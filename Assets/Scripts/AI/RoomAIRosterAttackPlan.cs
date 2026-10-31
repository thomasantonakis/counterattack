using System;
using System.Collections.Generic;
using System.Linq;

public enum RoomAIAttackingRoute { GroundCombination, RunningRelease, AerialRelease, DribbleLed }

[Serializable]
public sealed class RoomAIAttackingOption
{
    public RoomAIAttackingRoute route;
    public string creatorTokenKey;
    public string receiverTokenKey;
    public string supportTokenKey;
    public float suitability;
    public bool hasEvaluatedContinuation;
    public string preparation;
    public string continuation;
}

/// <summary>Roster strengths propose routes; live legal-action facts determine
/// whether those routes are ready. Suitability is not an xG/probability estimate.</summary>
[Serializable]
public sealed class RoomAIRosterAttackPlan
{
    public string team;
    public string rosterIdentity;
    public int playingOutfieldCount;
    public bool lacksStrongDribbler;
    public List<RoomAIAttackingOption> options = new();
    public RoomAIAerialReleasePlan aerial;
    public string rationale;

    public static RoomAIRosterAttackPlan Build(RoomAIDecisionSnapshot snapshot)
    {
        if (snapshot == null || snapshot.expectedTeam != snapshot.teamInAttack) return null;
        var own = EligiblePlayers(snapshot).OrderBy(token => token.tokenKey, StringComparer.Ordinal).ToList();
        if (own.Count == 0) return null;
        var result = new RoomAIRosterAttackPlan
        {
            team = snapshot.expectedTeam, playingOutfieldCount = own.Count,
            lacksStrongDribbler = own.All(token => token.dribbling < 5),
            rosterIdentity = string.Join(";", own.Select(token =>
                $"{token.tokenKey}:{token.pace},{token.dribbling},{token.heading},{token.highPass},{token.shooting},{token.resilience}"))
        };
        var carrier = snapshot.tokens.FirstOrDefault(token => token != null && token.isPlaying && !token.isSentOff
            && token.teamSide == snapshot.expectedTeam && RoomAITacticalSafety.Distance(token.currentHex, snapshot.ball) == 0);
        foreach (var shooter in own.Where(token => token != carrier))
        {
            var support = own.Where(token => token != carrier && token != shooter)
                .OrderByDescending(token => token.resilience).ThenByDescending(token => token.pace)
                .ThenBy(token => token.tokenKey, StringComparer.Ordinal).FirstOrDefault();
            result.options.Add(new RoomAIAttackingOption
            {
                route = RoomAIAttackingRoute.GroundCombination, creatorTokenKey = carrier?.tokenKey,
                receiverTokenKey = shooter.tokenKey, supportTokenKey = support?.tokenKey,
                suitability = .2f + shooter.shooting / 6f * .25f + shooter.pace / 6f * .2f,
                preparation = "Create a safer passing lane and nearby recovery support",
                continuation = "Ground pass or first-time combination; receiver movement then shot"
            });
        }
        foreach (var runner in own.Where(token => token != carrier && token.pace >= 5))
            result.options.Add(new RoomAIAttackingOption
            {
                route = RoomAIAttackingRoute.RunningRelease, creatorTokenKey = carrier?.tokenKey,
                receiverTokenKey = runner.tokenKey,
                suitability = .15f + runner.pace / 6f * .3f + runner.shooting / 6f * .2f,
                preparation = "Open a legal passing lane for a fast receiver; preserve collection Pace",
                continuation = "Pass into the receiver's path, collect, then move or snapshot"
            });
        result.aerial = RoomAIAerialReleasePlan.Build(snapshot);
        if (result.aerial != null)
            result.options.Add(new RoomAIAttackingOption
            {
                route = RoomAIAttackingRoute.AerialRelease, creatorTokenKey = result.aerial.distributorTokenKey,
                receiverTokenKey = result.aerial.runnerTokenKey, supportTokenKey = result.aerial.headerTokenKey,
                suitability = result.aerial.suitability,
                preparation = result.aerial.nextAction,
                continuation = "High pass to a suitable header; reassess offside, then headed release or finish"
            });
        foreach (var dribbler in own.Where(token => token.dribbling >= 5))
            result.options.Add(new RoomAIAttackingOption
            {
                route = RoomAIAttackingRoute.DribbleLed, creatorTokenKey = carrier?.tokenKey,
                receiverTokenKey = dribbler.tokenKey,
                suitability = dribbler.dribbling / 6f * .55f + dribbler.pace / 6f * .25f + dribbler.shooting / 6f * .2f,
                preparation = "Release the strong dribbler into space with passing and recovery support",
                continuation = "Carry through a safe route, then shoot or release a teammate"
            });
        foreach (var option in result.options)
        {
            var available = snapshot.candidateExplanations.Where(facts => Supports(option, facts)).ToList();
            option.hasEvaluatedContinuation = available.Count > 0;
            if (available.Count > 0)
                option.suitability += .2f * available.Max(facts => Math.Max(0f,
                    Math.Max(facts.groundPassRouteExpectedGoals, facts.firstTimePassCombinedExpectedGoals)));
        }
        result.options = result.options.OrderByDescending(option => option.hasEvaluatedContinuation)
            .ThenByDescending(option => option.suitability).ThenBy(option => option.route).ToList();
        result.rationale = $"Playing XI: {own.Count} outfield players; "
            + (result.lacksStrongDribbler ? "no Dribbling 5+ player; create passing/running alternatives" : "dribbling is one available route")
            + $". Available routes: {string.Join(", ", result.options.Select(option => option.route).Distinct())}. "
            + "Roster suitability proposes preparation; legal continuation and safety checks decide the action.";
        return result;
    }

    public static IEnumerable<RoomAITokenSnapshot> EligiblePlayers(RoomAIDecisionSnapshot snapshot) =>
        snapshot.tokens.Where(token => token != null && token.isPlaying && !token.isSentOff && !token.isGoalKeeper
            && token.currentHex != null && token.teamSide == snapshot.expectedTeam);

    public static bool Supports(RoomAIAttackingOption option, RoomAICandidateExplanation facts)
    {
        if (facts == null || facts.candidate?.isExecutableNow != true) return false;
        bool participant = facts.actorTokenKey == option.receiverTokenKey || facts.targetTokenKey == option.receiverTokenKey
            || facts.groundPassCollectorTokenKey == option.receiverTokenKey
            || facts.firstTimePassSetupShooterTokenKey == option.receiverTokenKey;
        switch (option.route)
        {
            case RoomAIAttackingRoute.GroundCombination:
            case RoomAIAttackingRoute.RunningRelease:
                return participant && (facts.supportsConcreteContinuation && facts.isSupportingPassLaneMove
                    || facts.isGroundPassContinuation && facts.groundPassRetentionProbability >= .5f
                        && facts.passRace?.opponentCollectsFirst != true
                        && (facts.groundPassCreatesReachableShot || facts.groundPassCreatesSecondMovementShot)
                    || facts.isFirstTimePassShotSetup && facts.firstTimePassCombinedExpectedGoals > 0f);
            case RoomAIAttackingRoute.AerialRelease:
                return facts.highPassTargetEvaluationComplete && facts.highPassBestCollectorTokenKey == option.supportTokenKey
                    && facts.highPassExpectedPossessionProbability >= .5f && facts.passRace?.opponentCollectsFirst != true;
            case RoomAIAttackingRoute.DribbleLed:
                return participant && (facts.fullPaceDribbleReachProbability >= .5f
                    || facts.isGroundPassContinuation && facts.groundPassRetentionProbability >= .5f
                        && facts.passRace?.opponentCollectsFirst != true && facts.passRace?.safeContinuationHexes > 0);
            default: return false;
        }
    }
}
