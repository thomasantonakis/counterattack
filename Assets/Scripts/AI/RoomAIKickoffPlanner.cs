using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>One opening: setup placement, a legal pass, actual collection cost,
/// then an evaluated continuation. Forecasts are rebuilt, not blindly executed.</summary>
public static class RoomAIKickoffPlanner
{
    private static readonly RoomHexCoordinates Centre = new() { x = 0, z = 0 };

    public static RoomAIKickoffPlan FromPassFacts(RoomAIDecisionSnapshot snapshot, RoomAICandidateExplanation facts)
    {
        if (snapshot == null || facts?.candidate?.actionType != RoomActionType.GroundPass
            || facts.candidate.isExecutableNow != true || facts.groundPassCollectorPickupDistance < 0
            || facts.groundPassRetentionProbability < 0f) return null;
        if (snapshot.kickoffOpeningCache.TryGetValue(facts.candidate, out var cached)) return cached;
        var receiver = Own(snapshot).FirstOrDefault(token => token.tokenKey == facts.groundPassCollectorTokenKey);
        var taker = ActualTaker(snapshot);
        var landing = facts.groundPassContinuationTargetHex ?? facts.targetHex ?? facts.targetTokenHex;
        if (receiver == null || taker == null || receiver == taker || landing == null) return null;
        var plan = Costed(snapshot, taker, receiver, landing, facts.groundPassCollectorPickupDistance);
        plan.firstPassEvaluated = true; // Manager supplied an executable, evaluated pass.
        plan.passRetention = facts.groundPassRetentionProbability;
        plan.continuationSurvival = facts.passRace?.continuationSurvival ?? -1f;
        plan.routeExpectedGoals = facts.groundPassRouteExpectedGoals;
        plan.firstActionReady = facts.passRace?.opponentCollectsFirst != true && plan.collectionFitsNextMovement;
        if (plan.collectionCost > 0)
        {
            plan.secondActionType = RoomActionType.StartMovement;
            plan.secondActionMoverTokenKey = receiver.tokenKey;
            plan.secondActionTargetHex = landing;
            plan.secondAction = $"Movement to collect at ({landing.x},{landing.z}); "
                + (plan.collectionFitsNextMovement ? $"{plan.remainingPace} Pace remains" : $"requires {plan.collectionPhases} movement phases");
            plan.continuationEvaluated = facts.passRace != null;
            if (!plan.collectionFitsNextMovement)
            {
                // The final pickup hex is not an executable first-move target
                // when collection requires multiple phases.
                plan.secondActionTargetHex = null;
                plan.continuationEvaluated = false;
                if (MatchesRuntime(snapshot) && receiver.pace > 0)
                {
                    var grid = MatchManager.Instance.hexGrid;
                    var route = HexGridUtils.FindPath(Hex(grid, receiver.currentHex), Hex(grid, landing), grid);
                    if (route.Count > 0)
                    {
                        plan.secondActionTargetHex = RoomHexCoordinates.FromHex(route[Math.Min(receiver.pace, route.Count) - 1]);
                        plan.continuationEvaluated = true;
                        plan.secondAction = $"Movement toward collection via ({plan.secondActionTargetHex.x},{plan.secondActionTargetHex.z}); "
                            + $"pickup still requires {plan.collectionPhases} movement phases";
                    }
                }
            }
        }
        else if (facts.groundPassImmediateSnapshotExpectedGoals > 0f)
        {
            plan.secondActionType = RoomActionType.Shot;
            plan.secondActionMoverTokenKey = receiver.tokenKey;
            plan.secondActionTargetHex = landing;
            plan.secondAction = $"Snapshot after reception at ({landing.x},{landing.z})";
            plan.continuationEvaluated = true;
        }
        else SetContinuation(snapshot, plan, receiver, facts.passRace);
        Score(plan);
        snapshot.kickoffOpeningCache[facts.candidate] = plan;
        return plan;
    }

    public static RoomAIKickoffPlan Build(RoomAIDecisionSnapshot snapshot, RoomAITokenSnapshot proposedTaker)
    {
        if (snapshot?.ball == null || proposedTaker == null) return null;
        bool setup = snapshot.currentState is "KickOffSetup" or "PostGoalKickOffSetup" or "KickOffTakerSelection";
        var evaluated = snapshot.candidateExplanations.Select(facts => FromPassFacts(snapshot, facts))
            .Where(plan => plan != null).ToList();
        if (evaluated.Count > 0) return SelectOpening(evaluated);
        // Do not invent tactical facts for a live target stage that has not been
        // analysed yet (e.g. Human Shadow's incremental analysis).
        if (!setup && snapshot.legalActions.Any(action => action.actionType == RoomActionType.GroundPass)) return null;

        bool runtime = MatchesRuntime(snapshot);
        var match = runtime ? MatchManager.Instance : null;
        int sign = snapshot.teamInAttack == "Home"
            ? snapshot.homeTeamDirection == "LeftToRight" ? 1 : -1
            : snapshot.awayTeamDirection == "LeftToRight" ? 1 : -1;
        int pressPace = snapshot.tokens.Where(token => token != null && token.isPlaying && !token.isGoalKeeper
            && token.teamSide != snapshot.teamInAttack).Select(token => token.pace).DefaultIfEmpty(5).Max();
        int safeX = -sign * Math.Max(4, Math.Min(8, pressPace + 1));
        // Bounded setup search; shape is still scored separately. Current
        // positions and useful placements compete, not a named receiver.
        var receivers = Own(snapshot).Where(token => token != proposedTaker)
            .OrderByDescending(token => token.highPass + token.pace + token.dribbling + token.shooting)
            .ThenBy(token => token.tokenKey, StringComparer.Ordinal).ToList();
        var plans = new List<RoomAIKickoffPlan>();
        foreach (var receiver in receivers)
        {
            var positions = new List<RoomHexCoordinates> { receiver.currentHex };
            if (setup)
                positions.Add(new RoomHexCoordinates { x = safeX, z = receiver.currentHex.z < 0 ? -2 : 2 });
            foreach (var target in positions.GroupBy(hex => (hex.x, hex.z)).Select(group => group.First()))
            {
                if (target.x * sign > 0 || Distance(Centre, target) == 0 || Distance(Centre, target) > 11
                    || snapshot.tokens.Any(token => token != null && token.isPlaying && token != receiver
                        && Distance(token.currentHex, target) == 0)) continue;
                bool needsSetup = setup && Distance(receiver.currentHex, target) > 0;
                var plan = Costed(snapshot, proposedTaker, receiver, target, needsSetup ? -1 : 0);
                plan.isSetupProjection = setup;
                plan.requiresReceiverSetup = needsSetup;
                plan.receiverSetupHex = target;
                plan.takerSetupHex = new RoomHexCoordinates { x = 0, z = 0 };
                if (runtime)
                {
                    var liveReceiver = match.playerTokenManager.allTokens.First(token => Key(token) == receiver.tokenKey);
                    var destination = Hex(match.hexGrid, target);
                    plan.firstPassEvaluated = TryPassRetention(snapshot, Centre, target, out float retention);
                    plan.passRetention = retention;
                    var race = RoomAIDecisionSnapshotBuilder.GetPassRace(snapshot, liveReceiver, destination, true);
                    plan.continuationSurvival = race?.continuationSurvival ?? -1f;
                    plan.routeExpectedGoals = race?.routeExpectedGoals ?? -1f;
                    SetContinuation(snapshot, plan, receiver, race);
                }
                // Collection becomes zero only once the setup actually places
                // the receiver on the landing hex. Unknown costs stay unknown.
                plan.firstActionReady = plan.firstPassEvaluated && plan.passRetention >= .5f
                    && plan.continuationEvaluated && !needsSetup
                    && !snapshot.tokens.Any(token => token != null && token.isPlaying && token != proposedTaker
                        && Distance(token.currentHex, Centre) == 0);
                Score(plan);
                plans.Add(plan);
            }
        }
        return SelectOpening(plans);
    }

    public static RoomAIKickoffPlan Costed(RoomAIDecisionSnapshot snapshot, RoomAITokenSnapshot taker,
        RoomAITokenSnapshot receiver, RoomHexCoordinates landing, int cost)
    {
        int pace = Math.Max(0, receiver.pace);
        int phases = cost < 0 ? 0 : cost == 0 ? 0 : (cost + Math.Max(1, pace) - 1) / Math.Max(1, pace);
        int consumed = cost > 0 ? 1 : 0;
        return new RoomAIKickoffPlan
        {
            team = snapshot.teamInAttack, takerTokenKey = taker.tokenKey, receiverTokenKey = receiver.tokenKey,
            landingHex = landing, collectionCost = cost, collectionPhases = phases, collectionMovesConsumed = consumed,
            collectionFitsNextMovement = cost >= 0 && cost <= pace,
            remainingPace = cost < 0 ? -1 : cost == 0 ? pace : Math.Max(0, phases * pace - cost),
            remainingPaceThisMovement = cost < 0 ? -1 : Math.Max(0, pace - cost),
            remainingAttackingMoves = cost < 0 ? -1 : Math.Max(0, snapshot.freshAttackingMoveLimit - consumed),
            secondAction = "Reassess after reception: no supported second action evaluated yet"
        };
    }

    public static RoomAIKickoffPlan SelectOpening(IEnumerable<RoomAIKickoffPlan> plans)
    {
        var options = plans.ToList();
        var direct = options.Where(plan => plan.collectionCost == 0)
            .OrderByDescending(plan => plan.value).FirstOrDefault();
        return options.Where(plan => plan.collectionCost <= 0 || direct == null || plan.value > direct.value)
            .OrderByDescending(plan => plan.firstPassEvaluated)
            .ThenByDescending(plan => plan.value).ThenBy(plan => plan.requiresReceiverSetup)
            .ThenBy(plan => plan.receiverTokenKey, StringComparer.Ordinal).FirstOrDefault();
    }

    private static void SetContinuation(RoomAIDecisionSnapshot snapshot, RoomAIKickoffPlan plan,
        RoomAITokenSnapshot receiver, RoomAIPassRacePreview race)
    {
        if (race?.routeExpectedGoals > 0f && race.bestContinuationHex != null
            && Distance(plan.landingHex, race.bestContinuationHex) > 0)
        {
            plan.secondActionType = RoomActionType.StartMovement;
            plan.secondActionMoverTokenKey = receiver.tokenKey;
            plan.secondActionTargetHex = race.bestContinuationHex;
            plan.secondAction = $"Movement to ({race.bestContinuationHex.x},{race.bestContinuationHex.z}), then snapshot";
            plan.continuationEvaluated = true;
            return;
        }
        if (!MatchesRuntime(snapshot)) return;
        // EndOfStandardPass offers movement/snapshot, not another ground pass.
        // Find a legal off-ball move which creates a new lane, improves retention
        // or makes a snapshot available after the subsequent pass.
        var match = MatchManager.Instance;
        float bestValue = 0f;
        foreach (var actor in Own(snapshot).Where(token => token != receiver && token.pace > 0))
        {
            var origin = actor.tokenKey == plan.takerTokenKey ? Centre : actor.currentHex;
            bool oldLane = TryPassRetention(snapshot, plan.landingHex, origin, out float oldRetention);
            var live = match.playerTokenManager.allTokens.First(token => Key(token) == actor.tokenKey);
            float oldXg = -1f;
            match.shotManager?.TryCalculateBestExpectedGoalsPreview(live, Hex(match.hexGrid, origin), "snapshot", out oldXg, out _, out _, out _);
            var vacated = new HashSet<HexCell> { live.GetCurrentHex() };
            var moves = HexGridUtils.GetReachableHexes(match.hexGrid, Hex(match.hexGrid, origin), actor.pace, vacated);
            int sign = receiver.teamSide == "Home"
                ? snapshot.homeTeamDirection == "LeftToRight" ? 1 : -1
                : snapshot.awayTeamDirection == "LeftToRight" ? 1 : -1;
            int line = snapshot.tokens.Where(token => token != null && token.isPlaying && !token.isSentOff
                    && token.currentHex != null && token.teamSide != receiver.teamSide)
                .Select(token => sign * token.currentHex.x).OrderByDescending(x => x).Skip(1).FirstOrDefault();
            int effectiveLine = Math.Max(0, Math.Max(line, sign * plan.landingHex.x));
            var endpoints = moves.reachableHexes.Where(hex => hex != null && !hex.isOutOfBounds
                    && !hex.isAttackOccupied && !hex.isDefenseOccupied
                    && Distance(origin, RoomHexCoordinates.FromHex(hex)) > 0 && sign * hex.coordinates.x <= effectiveLine)
                .OrderByDescending(hex => hex.ShootingPaths?.Keys.Any(goal => goal.isInGoal == sign) == true)
                .ThenBy(hex => moves.Item2[hex].distance)
                .ThenBy(hex => Distance(plan.landingHex, RoomHexCoordinates.FromHex(hex))).Take(6);
            foreach (var endpoint in endpoints)
            {
                var target = RoomHexCoordinates.FromHex(endpoint);
                if (!TryPassRetention(snapshot, plan.landingHex, target, out float retention) || retention < .5f) continue;
                float xg = -1f;
                match.shotManager?.TryCalculateBestExpectedGoalsPreview(live, endpoint, "snapshot", out xg, out _, out _, out _);
                float threatGain = Math.Max(0f, xg) * retention
                    - (oldLane ? Math.Max(0f, oldXg) * Math.Max(0f, oldRetention) : 0f);
                bool improves = !oldLane || retention > oldRetention + .05f || threatGain > .01f;
                if (!improves) continue;
                float value = (!oldLane ? 100f : (retention - oldRetention) * 100f)
                    + threatGain * 300f - moves.Item2[endpoint].distance;
                if (value <= bestValue) continue;
                bestValue = value;
                plan.secondActionType = RoomActionType.StartMovement;
                plan.secondActionMoverTokenKey = actor.tokenKey;
                plan.secondActionTargetHex = target;
                plan.followingActionType = RoomActionType.GroundPass;
                plan.followingActionTargetHex = target;
                plan.followingActionReceiverTokenKey = actor.tokenKey;
                plan.secondAction = $"Movement: {actor.tokenKey} to ({target.x},{target.z}) "
                    + $"to {(!oldLane ? "create a passing lane" : xg > 0f ? "prepare a snapshot receiver" : "improve pass retention")}; "
                    + $"after MP, reassess the pass (forecast retention {retention:0.000})";
                plan.continuationEvaluated = true;
            }
        }
    }

    private static bool TryPassRetention(RoomAIDecisionSnapshot snapshot, RoomHexCoordinates origin,
        RoomHexCoordinates target, out float retention)
    {
        retention = -1f;
        var match = MatchManager.Instance;
        if (match?.hexGrid == null || origin == null || target == null || Distance(origin, target) is 0 or > 11) return false;
        var from = Hex(match.hexGrid, origin); var to = Hex(match.hexGrid, target);
        if (from == null || to == null || to.isOutOfBounds) return false;
        var path = GroundPassCommon.CalculateThickPath(match.hexGrid, from, to, match.ball.ballRadius);
        var opponents = snapshot.tokens.Where(token => token != null && token.isPlaying && !token.isSentOff
            && token.currentHex != null && token.teamSide != snapshot.teamInAttack).ToList();
        if (opponents.Any(token => path.Any(hex => Distance(token.currentHex, RoomHexCoordinates.FromHex(hex)) == 0))) return false;
        var interceptors = match.playerTokenManager.allTokens.Where(token => token != null && token.isPlaying
            && (token.isHomeTeam ? "Home" : "Away") != snapshot.teamInAttack
            && token.GetCurrentHex() != null && path.Any(hex => HexGridUtils.GetHexStepDistance(token.GetCurrentHex(), hex) == 1));
        retention = 1f - ExpectedStatsCalculator.CalculateAtLeastOneRecoveryProbability(interceptors.Distinct().ToArray());
        return true;
    }

    private static void Score(RoomAIKickoffPlan plan)
    {
        // Value uses evaluated retention/threat, not the receiver's High Pass
        // attribute as evidence that a high-pass continuation is legal.
        plan.value = Math.Max(0f, plan.passRetention) * 200f + Math.Max(0f, plan.continuationSurvival) * 100f
            + Math.Max(0f, plan.routeExpectedGoals) * 900f + (plan.continuationEvaluated ? 40f : 0f)
            - Math.Max(0, plan.collectionCost) * 8f - plan.collectionMovesConsumed * 50f
            - Math.Max(0, plan.collectionPhases - 1) * 150f;
        plan.rationale = plan.requiresReceiverSetup
            ? $"Place {plan.receiverTokenKey} at ({plan.landingHex.x},{plan.landingHex.z}) before kickoff; retrieval costs remain unknown until arranged."
            : $"Collection cost {plan.collectionCost}, phases {plan.collectionPhases}, remaining Pace {plan.remainingPace}, "
                + $"remaining Pace in the next movement {plan.remainingPaceThisMovement}, "
                + $"remaining first-phase attacking moves {plan.remainingAttackingMoves}; next {plan.secondAction}.";
    }

    private static RoomAITokenSnapshot ActualTaker(RoomAIDecisionSnapshot snapshot) =>
        Own(snapshot).FirstOrDefault(token => Distance(token.currentHex, snapshot.ball) == 0);
    private static IEnumerable<RoomAITokenSnapshot> Own(RoomAIDecisionSnapshot snapshot) => snapshot.tokens
        .Where(token => token != null && token.isPlaying && !token.isSentOff && !token.isGoalKeeper
            && token.currentHex != null && token.teamSide == snapshot.teamInAttack);
    private static bool MatchesRuntime(RoomAIDecisionSnapshot snapshot)
    {
        var match = MatchManager.Instance;
        return match?.hexGrid != null && match.ball != null && match.playerTokenManager != null
            && snapshot.tokens.Where(token => token != null && token.isPlaying && token.currentHex != null)
                .All(token => match.playerTokenManager.allTokens.Any(live => live != null && live.isPlaying
                    && Key(live) == token.tokenKey && live.pace == token.pace && live.dribbling == token.dribbling
                    && live.highPass == token.highPass && live.heading == token.heading && live.shooting == token.shooting
                    && live.tackling == token.tackling && live.resilience == token.resilience
                    && Distance(RoomHexCoordinates.FromHex(live.GetCurrentHex()), token.currentHex) == 0));
    }
    private static string Key(PlayerToken token) => (token.isHomeTeam ? "Home:" : "Away:") + token.jerseyNumber;
    private static HexCell Hex(HexGrid grid, RoomHexCoordinates hex) =>
        grid.GetHexCellAt(new Vector3Int(hex.x, 0, hex.z), false);
    private static int Distance(RoomHexCoordinates a, RoomHexCoordinates b) => RoomAITacticalSafety.Distance(a, b);
}
