using System;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

public interface IRoomAIProvider
{
    string Name { get; }

    RoomAIProviderDecision Decide(
        RoomAIDecisionSnapshot snapshot,
        IReadOnlyList<RoomActionCandidate> legalCandidates,
        RoomAIAttackPlan currentPlan,
        RoomAITacticalWeights weights,
        AIManager.CodexTacticianDifficulty difficulty);
}

public sealed class RoomAIProviderDecision
{
    public string providerName;
    public AIManager.CodexTacticianDifficulty difficulty;
    public RoomActionCandidate selectedCandidate;
    public RoomAITacticalEvaluation selectedEvaluation;
    public RoomAIAttackPlan plan;
    public List<RoomAITacticalEvaluation> rankedCandidates = new();
    public int candidateCount;
    public double durationMilliseconds;
    public string reasonSummary;
    public bool analysisComplete = true;
    public int legalCandidateCount;
}

/// <summary>
/// A pluggable Room AI provider which consumes legal actions produced by the
/// gameplay managers. It never invents actions or bypasses the rules engine.
/// </summary>
public sealed class CodexTacticianProvider : IRoomAIProvider
{
    public const string DisplayName = "Codex Tactician";
    public static readonly CodexTacticianProvider Instance = new();
    private static readonly (int x, int z)[] HumanLedAttackingKickoffLeftToRight =
    {
        default,
        (-16, 0),
        (-7, -9),
        (-7, 8),
        (-8, 3),
        (-8, -3),
        (-4, -4),
        (0, -7),
        (0, 6),
        (0, 4),
        (0, -4),
        (0, 10)
    };
    private static readonly (int x, int z)[] HumanLedDefendingKickoffRightToLeft =
    {
        default,
        (16, 0),
        (6, 8),
        (6, -7),
        (7, -5),
        (7, 4),
        (4, 2),
        (0, 9),
        (4, -1),
        (0, 5),
        (0, -5),
        (0, -8)
    };

    public string Name => DisplayName;

    private CodexTacticianProvider()
    {
    }

    public RoomAIProviderDecision Decide(
        RoomAIDecisionSnapshot snapshot,
        IReadOnlyList<RoomActionCandidate> legalCandidates,
        RoomAIAttackPlan currentPlan,
        RoomAITacticalWeights weights,
        AIManager.CodexTacticianDifficulty difficulty)
    {
        RoomAIProviderDecision result = null;
        var work = DecideIncrementally(snapshot, legalCandidates, currentPlan, weights, difficulty, decision => result = decision);
        while (work.MoveNext()) { }
        return result;
    }

    public IEnumerator DecideIncrementally(
        RoomAIDecisionSnapshot snapshot,
        IReadOnlyList<RoomActionCandidate> legalCandidates,
        RoomAIAttackPlan currentPlan,
        RoomAITacticalWeights weights,
        AIManager.CodexTacticianDifficulty difficulty, Action<RoomAIProviderDecision> completed)
    {
        Stopwatch timer = Stopwatch.StartNew();
        RoomAIProviderDecision decision = new()
        {
            providerName = Name,
            difficulty = difficulty,
            candidateCount = legalCandidates?.Count ?? 0
        };

        if (snapshot == null || legalCandidates == null || legalCandidates.Count == 0)
        {
            timer.Stop();
            decision.durationMilliseconds = timer.Elapsed.TotalMilliseconds;
            decision.reasonSummary = "No legal executable candidates were available.";
            completed(decision);
            yield break;
        }

        snapshot.providerWorkStage = "initial ranking";
        weights ??= new RoomAITacticalWeights();
        decision.plan = RoomAITacticalPlanner.UpdateAttackPlan(snapshot, currentPlan, weights);
        if (IsKickoffSetupState(snapshot.currentState) || snapshot.currentState == "KickOffTakerSelection")
            snapshot.kickoffPlan = BuildKickoffPlan(snapshot);
        decision.rankedCandidates = RoomAITacticalPlanner.RankCandidates(
            snapshot,
            legalCandidates,
            decision.plan,
            weights);

        yield return null;
        snapshot.providerWorkStage = "pre-lookahead policies / first lookahead step";
        ApplyUnsupportedPassSafety(snapshot, decision.rankedCandidates, weights);
        if (snapshot.splitProviderProfileStages) { snapshot.providerWorkStage = "unsupported pass safety"; yield return null; }
        ApplyLooseBallRecoveryPriority(snapshot, decision.rankedCandidates, weights);
        if (snapshot.splitProviderProfileStages) { snapshot.providerWorkStage = "loose-ball priority"; yield return null; }
        ApplyNutmegAndSetPiecePolicy(snapshot, decision.rankedCandidates);
        if (snapshot.splitProviderProfileStages) { snapshot.providerWorkStage = "nutmeg / set piece"; yield return null; }
        ApplyCarrierHoldPolicy(snapshot, decision.rankedCandidates);
        if (snapshot.splitProviderProfileStages) { snapshot.providerWorkStage = "carrier hold"; yield return null; }
        ApplyKickoffSetupPriority(snapshot, decision.rankedCandidates, weights);
        if (snapshot.splitProviderProfileStages) { snapshot.providerWorkStage = "kickoff setup"; yield return null; }
        ApplyKickoffTakerPriority(snapshot, decision.rankedCandidates);
        if (snapshot.splitProviderProfileStages) { snapshot.providerWorkStage = "kickoff taker"; yield return null; }
        snapshot.providerWorkStage = "kickoff opening candidate";
        var kickoffOpening = ApplyKickoffFirstActionPlanIncrementally(snapshot, decision.rankedCandidates);
        while (kickoffOpening.MoveNext()) yield return null;
        if (snapshot.splitProviderProfileStages) { snapshot.providerWorkStage = "kickoff first action"; yield return null; }
        ApplyDesperateKickoffRush(snapshot, decision.rankedCandidates, weights);
        if (snapshot.splitProviderProfileStages) { snapshot.providerWorkStage = "kickoff rush"; yield return null; }
        ApplyAerialReleaseContinuation(decision.plan?.aerialReleasePlan, snapshot, decision.rankedCandidates);
        if (snapshot.splitProviderProfileStages) { snapshot.providerWorkStage = "aerial release"; yield return null; }
        ApplyOptionalGoalkeeperMovementRestraint(snapshot, decision.rankedCandidates, weights);
        if (snapshot.splitProviderProfileStages) { snapshot.providerWorkStage = "goalkeeper restraint"; yield return null; }
        ApplyDefensivePressurePriority(snapshot, decision.rankedCandidates, weights);
        if (snapshot.splitProviderProfileStages) { snapshot.providerWorkStage = "defensive pressure"; yield return null; }
        ApplyRepeatedNoProgressMovementRestraint(snapshot, decision.rankedCandidates, weights);
        if (snapshot.splitProviderProfileStages) { snapshot.providerWorkStage = "repeated movement restraint"; yield return null; }

        if (snapshot.splitProviderProfileStages)
        {
            snapshot.providerWorkStage = "first lookahead step";
        }

        if (difficulty == AIManager.CodexTacticianDifficulty.Hard
            || difficulty == AIManager.CodexTacticianDifficulty.UltraHard)
        {
            var lookahead = ApplyHardLookaheadIncrementally(snapshot, decision.rankedCandidates, weights);
            while (lookahead.MoveNext())
            {
                yield return null;
                snapshot.providerWorkStage = "remaining lookahead";
            }
        }

        snapshot.providerWorkStage = "final policies and selection";
        ApplyTacticalSafetyConstraints(snapshot, decision.rankedCandidates);
        ApplyAdvancingReceptionPriority(decision.rankedCandidates);
        ApplyUnopposedDribbleProgress(decision.rankedCandidates);
        ApplyAttackingHighPassSupport(snapshot, decision.rankedCandidates);
        PreserveRemainingMovementAfterSelection(snapshot, decision.rankedCandidates);
        ApplySeparatedTwoForTwoOutlets(snapshot, decision.rankedCandidates);
        ApplyForfeitOpportunityCost(decision.rankedCandidates, weights);
        ApplyEarlyDefendingKickoffFrontLinePriority(snapshot, decision.rankedCandidates);

        decision.selectedEvaluation = SelectCandidate(decision.rankedCandidates, difficulty);
        decision.selectedCandidate = decision.selectedEvaluation?.candidate;
        if (decision.selectedEvaluation?.explanation?.isKickoffDecision == true
            && decision.selectedCandidate?.actionType == RoomActionType.GroundPass)
            snapshot.kickoffPlan = RoomAIKickoffPlanner.FromPassFacts(snapshot, decision.selectedEvaluation.explanation)
                ?? snapshot.kickoffPlan;
        else if (decision.selectedEvaluation?.explanation?.isKickoffDecision == true
            && snapshot.kickoffPlan?.isSetupProjection != true)
            snapshot.kickoffPlan = null; // Do not report an unchosen ground-pass opening as executed.
        decision.reasonSummary = BuildReasonSummary(decision.selectedEvaluation);
        timer.Stop();
        decision.durationMilliseconds = timer.Elapsed.TotalMilliseconds;
        completed(decision);
    }

    private static void ApplySeparatedTwoForTwoOutlets(RoomAIDecisionSnapshot snapshot, List<RoomAITacticalEvaluation> evaluations)
    {
        var match = MatchManager.Instance;
        var movement = match?.movementPhaseManager;
        var grid = match?.hexGrid;
        if (snapshot?.attackHasPossession != true || snapshot.expectedTeam != snapshot.teamInAttack
            || snapshot.currentState != "MovementPhase" || movement?.isMovementPhase2f2 != true
            || movement.isAwaitingHexDestination || movement.TwoForTwoMoveLimit - movement.attackersMovedIn2f2 < 2
            || grid == null) return;

        var eligible = new HashSet<PlayerToken>(movement.GetSelectableMovementTokensForDecisionPreview());
        var outlets = evaluations.Where(e => e?.candidate?.step == RoomDecisionStep.ChooseActor
            && e.candidate.actionType == RoomActionType.StartMovement && e.candidate.actor != null
            && eligible.Contains(e.candidate.actor) && e.safetyPriority == 0 && e.score > 0
            && e.explanation?.isSupportingPassLaneMove == true && e.explanation.supportsConcreteContinuation
            && e.explanation.groundPassRetentionProbability >= .5f
            && e.explanation.groundPassContinuationTargetHex != null
            && MatchesCoordinates(e.explanation.targetHex, e.explanation.groundPassContinuationTargetHex)).ToList();
        if (outlets.Count < 2) return;
        float baseline = RoomAITacticalSafety.CounterExposure(snapshot, null, null);
        var baseScores = outlets.ToDictionary(e => e, e => e.score);
        foreach (var first in outlets)
        {
            var a = first.explanation;
            var firstTarget = grid.GetHexCellAt(new UnityEngine.Vector3Int(a.targetHex.x, 0, a.targetHex.z), false);
            if (firstTarget == null) continue;
            RoomAITacticalEvaluation bestOther = null;
            int bestBonus = 0;
            int bestSharedPressureCost = 0;
            int bestFinishingBonus = 0;
            foreach (var second in outlets)
            {
                var b = second.explanation;
                if (second == first || first.candidate.actor == second.candidate.actor || first.tier != second.tier
                    || RoomAITacticalSafety.Distance(a.targetHex, b.targetHex) < 4) continue;
                var target = grid.GetHexCellAt(new UnityEngine.Vector3Int(b.targetHex.x, 0, b.targetHex.z), false);
                if (target == null || target == firstTarget) continue;
                // The original path must still work after the first player moves.
                // This conservative check does not invent a new path through the
                // vacated origin, and never changes live board occupancy.
                var path = HexGridUtils.FindPath(second.candidate.actor.GetCurrentHex(), target, grid);
                if (path == null || path.Count == 0 || path.Count > movement.TwoForTwoRange || path.Contains(firstTarget)) continue;
                float joint = RoomAITacticalSafety.CounterExposure(snapshot, a.actorTokenKey, a.targetHex,
                    b.actorTokenKey, b.targetHex) - baseline;
                float separatelyPriced = RoomAITacticalSafety.CounterExposure(snapshot, a.actorTokenKey, a.targetHex) - baseline
                    + RoomAITacticalSafety.CounterExposure(snapshot, b.actorTokenKey, b.targetHex) - baseline;
                int extraCoverCost = (int)Math.Ceiling(Math.Max(0f, joint - separatelyPriced) * 120f);
                // Both destinations must work as different outlets. A single
                // opponent who can plausibly challenge either next reception
                // makes the pair less complementary; do not assume a forced
                // tackle or discard either individually legal pass.
                int sharedPressureCost = (int)Math.Round(12f * RoomAITacticalSafety.SharedOutletChallengeExposure(snapshot,
                    a.targetHex, b.targetHex));
                bool finishingRoute = a.groundPassCreatesReachableShot || a.groundPassCreatesSecondMovementShot
                    || b.groundPassCreatesReachableShot || b.groundPassCreatesSecondMovementShot;
                int finishingBonus = finishingRoute ? 6 : 0;
                int bonus = Math.Min(24, baseScores[second] / 4) + finishingBonus - extraCoverCost - sharedPressureCost;
                if (bonus <= bestBonus) continue;
                bestBonus = bonus;
                bestOther = second;
                bestSharedPressureCost = sharedPressureCost;
                bestFinishingBonus = finishingBonus;
            }
            if (bestOther == null) continue;
            first.score += bestBonus;
            first.reasons.Add($"+{bestBonus} provisional separated 2f2 outlet: {bestOther.explanation.actorTokenKey} "
                + $"at ({bestOther.explanation.targetHex.x}, {bestOther.explanation.targetHex.z}); "
                + $"both moves fit the current budget and current paths, with combined cover cost and shared challenge penalty {bestSharedPressureCost}; "
                + (bestFinishingBonus > 0 ? "one forecast includes a reachable shot; " : "finishing route not established; ")
                + "reassess after the first move");
        }
        evaluations.Sort(CompareEvaluations);
    }

    private static void ApplyNutmegAndSetPiecePolicy(RoomAIDecisionSnapshot snapshot, List<RoomAITacticalEvaluation> evaluations)
    {
        var match = MatchManager.Instance;
        var movement = match?.movementPhaseManager;
        var carrier = match?.ball?.GetCurrentHex()?.GetOccupyingToken();
        foreach (var evaluation in evaluations)
        {
            var action = evaluation.candidate;
            if (action.actionType == RoomActionType.Nutmeg && carrier != null)
            {
                var victim = action.targetToken ?? movement?.nutmegVictim
                    ?? movement?.nutmeggableDefenders.OrderByDescending(token => token.tackling).FirstOrDefault();
                if (victim == null) continue;
                var duel = ExpectedStatsCalculator.CalculateGroundDuelExpectation(carrier, victim, 1);
                string direction = carrier.isHomeTeam ? snapshot.homeTeamDirection : snapshot.awayTeamDirection;
                int progress = (direction == "LeftToRight" ? 1 : -1) * carrier.GetCurrentHex().coordinates.x;
                bool escape = victim.pace > carrier.pace && carrier.dribbling >= 4 && progress >= 6;
                float retain = duel.attackerWinProbability + duel.tieProbability / 6f;
                float loss = duel.defenderWinProbability + duel.tieProbability * 5f / 6f;
                int value = (int)Math.Round(retain * (escape ? 650 : 140) + duel.defenderFoulProbability * 100 - loss * 500 - 180);
                // Replace the generic, fictitious certainty of possession.
                evaluation.score = value;
                evaluation.tier = value > 0 && escape ? RoomAITacticalTier.SafeProgression : RoomAITacticalTier.Risky;
                evaluation.reasons.Clear();
                evaluation.reasons.Add($"Nutmeg (+1 defending skill): retain={retain:0.000}, loss={loss:0.000}, foul={duel.defenderFoulProbability:0.000}; "
                    + $"faster-marker two-MP escape={escape}. Voluntary nutmegs require a substantial benefit.");
            }
            if (evaluation.explanation?.hasSetPieceShotDefense == true)
            {
                evaluation.score = (int)Math.Round((1f - evaluation.explanation.defensiveExpectedGoalsAgainst) * 20000f);
                evaluation.tier = RoomAITacticalTier.SafeProgression;
                evaluation.reasons.Add($"Free-kick wall leaves best direct-shot xG={evaluation.explanation.defensiveExpectedGoalsAgainst:0.000}");
            }
            if (action.manager == nameof(FreeKickManager) && evaluation.explanation?.isGoalkeeperPositioning == true)
                evaluation.score -= evaluation.explanation.goalkeeperOwnGoalDistanceDelta * 200;
            if (action.manager == nameof(FreeKickManager) && action.step == RoomDecisionStep.ChooseActor
                && action.actor != null && !action.actor.isAttacker)
            {
                var defender = action.actor;
                if (defender.IsGoalKeeper && snapshot.currentState != "FreeKickDefGK1"
                    && snapshot.currentState != "FreeKickDefGK2")
                {
                    evaluation.score = -100000;
                    evaluation.tier = RoomAITacticalTier.Risky;
                    evaluation.reasons.Clear();
                    evaluation.reasons.Add("defending goalkeeper already has a dedicated free move before and after the set-piece sequence");
                    continue;
                }
                if (defender.IsGoalKeeper || match?.freeKickManager == null) continue;
                evaluation.score = (match.freeKickManager.isCornerKick
                    ? defender.heading * 40 + defender.tackling * 10
                    : defender.tackling * 32 + defender.heading * 10)
                    - (match.freeKickManager.SetupMovedTokens.Contains(defender) ? 260 : 0);
                evaluation.reasons.Clear();
                evaluation.reasons.Add($"Set-piece defensive role: Tackling {defender.tackling}, Heading {defender.heading}; "
                    + $"already repositioned={match.freeKickManager.SetupMovedTokens.Contains(defender)}");
            }
            if (evaluation.explanation?.isCornerDefenseMove == true)
            {
                float gain = evaluation.explanation.cornerDefenseGain;
                evaluation.score = (int)Math.Round(gain * 5000f) - 12;
                evaluation.tier = gain > .001f ? RoomAITacticalTier.SafeProgression : RoomAITacticalTier.Risky;
                evaluation.reasons.Clear();
                evaluation.reasons.Add($"Corner receiver/header coverage gain={gain:0.000}; no dribbling threat credited to the taker");
            }
        }
        evaluations.Sort(CompareEvaluations);
    }

    private static void ApplyCarrierHoldPolicy(RoomAIDecisionSnapshot snapshot, List<RoomAITacticalEvaluation> evaluations)
    {
        var match = MatchManager.Instance;
        var movement = match?.movementPhaseManager;
        var carrier = match?.ball?.GetCurrentHex()?.GetOccupyingToken();
        if (movement?.isMovementPhaseAttack != true || carrier == null || carrier.highPass < 5
            || movement.movedTokens.Contains(carrier) || movement.TwoForTwoMoveLimit <= 0) return;
        int sign = (carrier.isHomeTeam ? snapshot.homeTeamDirection : snapshot.awayTeamDirection) == "LeftToRight" ? 1 : -1;
        if (sign * carrier.GetCurrentHex().coordinates.x < 8 || Math.Abs(carrier.GetCurrentHex().coordinates.z) < 6) return;
        var markers = match.playerTokenManager.allTokens.Where(token => token != null && token.isPlaying
            && token.isHomeTeam != carrier.isHomeTeam && token.GetCurrentHex() != null
            && HexGridUtils.GetHexStepDistance(token.GetCurrentHex(), carrier.GetCurrentHex()) == 1).ToList();
        if (markers.Count == 0) return;
        float survival = markers.Select(marker =>
        {
            var duel = ExpectedStatsCalculator.CalculateGroundDuelExpectation(carrier, marker);
            return duel.attackerWinProbability + duel.defenderFoulProbability + duel.tieProbability / 6f;
        }).Min();
        foreach (var evaluation in evaluations.Where(item => item.candidate.actor != null
            && item.candidate.actor != carrier && item.explanation?.supportsConcreteContinuation == true))
        {
            int value = (int)Math.Round(survival * 140f);
            evaluation.score += value;
            evaluation.reasons.Add($"+{value} conditional winger hold: preserve the carrier's move; "
                + $"if the marker stands, seek a 2f2 release; if tackled, survival={survival:0.000} before reposition/2f2. No forced opponent response assumed.");
        }
        evaluations.Sort(CompareEvaluations);
    }

    private static void ApplyUnopposedDribbleProgress(List<RoomAITacticalEvaluation> evaluations)
    {
        var safeRoutes = evaluations.Where(item => item.safetyPriority == 0
            && item.explanation?.isFullPaceDribbleRoute == true
            && item.explanation.fullPaceDribbleReachProbability >= 0.9999f
            && item.explanation.fullPaceDribbleTurnoverProbability <= 0.0001f
            && item.explanation.fullPaceDribbleEndpointPressure == 0
            && item.explanation.fullPaceDribbleExpectedGoals <= 0f
            && item.explanation.bestReachableSnapshotExpectedGoals <= 0f).ToList();
        if (safeRoutes.Count < 2) return;
        int bestProgress = safeRoutes.Max(item => item.explanation.fullPaceDribbleProgress);
        foreach (var item in safeRoutes.Where(item => item.explanation.fullPaceDribbleProgress < bestProgress))
        {
            item.safetyPriority = 1;
            item.reasons.Insert(0, $"An equally unopposed route advances {bestProgress} columns; this route advances "
                + $"{item.explanation.fullPaceDribbleProgress} without improving a finish");
        }
        evaluations.Sort(CompareEvaluations);
    }

    private static void ApplyAdvancingReceptionPriority(List<RoomAITacticalEvaluation> evaluations)
    {
        var passes = evaluations.Where(e => e.safetyPriority == 0 && !e.candidate.isForfeit
            && e.candidate.actionType == RoomActionType.GroundPass
            && e.candidate.step is RoomDecisionStep.ChooseTarget or RoomDecisionStep.Confirm
            && e.explanation?.isGroundPassContinuation == true
            && e.explanation.groundPassCollectorRemainingPace >= 0
            && e.explanation.groundPassRetentionProbability >= .65f
            && e.explanation.passRace?.opponentCollectsFirst != true).ToList();
        foreach (var pass in passes)
        {
            var current = pass.explanation;
            var advancing = passes.FirstOrDefault(other => other != pass
                && other.explanation.forwardProgressDelta >= Math.Max(1, current.forwardProgressDelta + 3)
                && other.explanation.groundPassRetentionProbability >= current.groundPassRetentionProbability - .05f
                && Math.Max(0f, other.explanation.groundPassRouteExpectedGoals)
                    >= Math.Max(0f, current.groundPassRouteExpectedGoals) - .01f);
            if (advancing == null) continue;
            pass.safetyPriority = 1;
            pass.reasons.Insert(0, $"An equally secure advancing reception offers {advancing.explanation.forwardProgressDelta} "
                + "columns of progress without sacrificing a material finishing route; retain the back pass as fallback");
        }
        evaluations.Sort(CompareEvaluations);
    }

    private static void ApplyRepeatedNoProgressMovementRestraint(
        RoomAIDecisionSnapshot snapshot,
        List<RoomAITacticalEvaluation> evaluations,
        RoomAITacticalWeights weights)
    {
        if (snapshot?.repeatsNoProgressMovementState != true || evaluations == null)
        {
            return;
        }

        foreach (RoomAITacticalEvaluation evaluation in evaluations)
        {
            RoomActionCandidate candidate = evaluation?.candidate;
            if (candidate == null
                || candidate.actionType != RoomActionType.StartMovement
                || candidate.step != RoomDecisionStep.ChooseActionType)
            {
                continue;
            }

            evaluation.score -= weights.repeatedNoProgressMovementPenalty;
            evaluation.tier = RoomAITacticalTier.Risky;
            evaluation.reasons.Insert(
                0,
                $"-{weights.repeatedNoProgressMovementPenalty} rejects another Movement Phase after the attacking side made no positional progress");
        }

        evaluations.Sort(CompareEvaluations);
    }

    /// <summary>
    /// A hanging or loose ball is the immediate tactical objective. Without this
    /// provider-level correction, attractive shooting-position movement can win
    /// every recovery phase while possession remains unresolved, producing a
    /// legal but non-progressing loop. The rules layer still decides which actors,
    /// destinations, and collect commands are legal; this only ranks those options.
    /// </summary>
    private static void ApplyLooseBallRecoveryPriority(
        RoomAIDecisionSnapshot snapshot,
        List<RoomAITacticalEvaluation> evaluations,
        RoomAITacticalWeights weights)
    {
        if (snapshot == null
            || snapshot.attackHasPossession
            || snapshot.expectedOpponentCollection
            || evaluations == null
            || !string.Equals(snapshot.currentState, "MovementPhase", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        foreach (RoomAITacticalEvaluation evaluation in evaluations)
        {
            RoomActionCandidate candidate = evaluation?.candidate;
            RoomAICandidateExplanation explanation = evaluation?.explanation;
            if (candidate == null
                || explanation == null
                || candidate.actionType != RoomActionType.StartMovement)
            {
                continue;
            }

            int recoveryScore = 0;
            if (candidate.step == RoomDecisionStep.InterruptionChoice
                && string.Equals(candidate.key, "V", StringComparison.OrdinalIgnoreCase))
            {
                recoveryScore = weights.looseBallCollect;
            }
            else if (candidate.step == RoomDecisionStep.ChooseTarget
                && explanation.targetDistanceToBall >= 0)
            {
                recoveryScore = Math.Max(
                    0,
                    weights.looseBallTargetBase
                    - (explanation.targetDistanceToBall * weights.looseBallTargetDistancePenalty));
                if (explanation.actorDistanceToBall >= 0)
                {
                    recoveryScore += Math.Max(
                        0,
                        explanation.actorDistanceToBall - explanation.targetDistanceToBall)
                        * weights.looseBallProgressPerHex;
                }
            }
            else if (candidate.step == RoomDecisionStep.ChooseActor
                && explanation.actorDistanceToBall >= 0)
            {
                recoveryScore = Math.Max(
                    0,
                    weights.looseBallActorBase
                    - (explanation.actorDistanceToBall * weights.looseBallActorDistancePenalty));
            }

            if (recoveryScore <= 0)
            {
                continue;
            }

            // Recover the unowned ball; an unrelated hypothetical GK shooting
            // wall must not subsidise a distant two-phase collection attempt.
            evaluation.score = recoveryScore;
            evaluation.reasons.Clear();
            if (explanation.passRace != null)
            {
                evaluation.score += (int)Math.Round(explanation.passRace.continuationSurvival * 250f)
                    - explanation.passRace.pickupCost * 15;
                evaluation.reasons.Insert(0, $"Loose-ball race: {explanation.passRace.collectorTokenKey}, pickup cost {explanation.passRace.pickupCost}, remaining Pace {explanation.passRace.remainingPace}, survival={explanation.passRace.continuationSurvival:0.000}");
            }
            // While the ball is loose, recovery is the immediate objective. All
            // legal movement options share this tier so distance-to-ball, not an
            // unrelated attacking-position tier, decides between them.
            evaluation.tier = RoomAITacticalTier.ImmediateFinish;
            evaluation.reasons.Insert(
                0,
                $"+{recoveryScore} prioritizes legal loose-ball recovery");
        }

        evaluations.Sort(CompareEvaluations);
    }

    private static void ApplyUnsupportedPassSafety(
        RoomAIDecisionSnapshot snapshot,
        List<RoomAITacticalEvaluation> evaluations,
        RoomAITacticalWeights weights)
    {
        if (snapshot == null || evaluations == null)
        {
            return;
        }

        bool isKickoff = snapshot.currentState?.IndexOf("kickoff", StringComparison.OrdinalIgnoreCase) >= 0;
        foreach (RoomAITacticalEvaluation evaluation in evaluations)
        {
            RoomActionCandidate candidate = evaluation?.candidate;
            RoomAICandidateExplanation explanation = evaluation?.explanation;
            if (candidate == null
                || explanation == null
                || candidate.step is not (RoomDecisionStep.ChooseTarget or RoomDecisionStep.Confirm)
                || !IsPassAction(candidate.actionType)
                || explanation.targetHex == null
                || explanation.targetTokenHex != null
                || !string.IsNullOrWhiteSpace(explanation.targetTokenKey))
            {
                continue;
            }

            bool groundDistribution = candidate.actionType is RoomActionType.GroundPass or RoomActionType.QuickThrow;
            bool hasProvenCollector = groundDistribution
                ? !string.IsNullOrWhiteSpace(explanation.groundPassCollectorTokenKey)
                    && explanation.passRace != null && !explanation.passRace.opponentCollectsFirst
                : explanation.friendlySupportWithinRange > 0
                    || !string.IsNullOrWhiteSpace(explanation.headedPassSpaceCollectorTokenKey)
                    || !string.IsNullOrWhiteSpace(explanation.highPassBestCollectorTokenKey)
                    || explanation.possessionChain?.attackRetainsPossessionProbability > 0.25f;
            if (hasProvenCollector || explanation.isSetPieceHighPassPlan)
            {
                continue;
            }

            int penalty = weights.unsupportedPassIntoSpace
                + (isKickoff ? weights.kickoffUnsupportedPassIntoSpace : 0);
            evaluation.score += penalty;
            evaluation.tier = RoomAITacticalTier.Risky;
            evaluation.reasons.Insert(
                0,
                $"{penalty} rejects an unsupported pass into empty space"
                + (isKickoff ? " from kickoff" : string.Empty));
        }

        evaluations.Sort(CompareEvaluations);
    }

    private static void ApplyDefensivePressurePriority(
        RoomAIDecisionSnapshot snapshot,
        List<RoomAITacticalEvaluation> evaluations,
        RoomAITacticalWeights weights)
    {
        if (snapshot == null
            || string.IsNullOrWhiteSpace(snapshot.expectedTeam)
            || string.Equals(snapshot.expectedTeam, "Neutral", StringComparison.OrdinalIgnoreCase)
            || string.Equals(snapshot.expectedTeam, snapshot.teamInAttack, StringComparison.OrdinalIgnoreCase)
                && !snapshot.expectedOpponentCollection
            || IsKickoffSetupState(snapshot.currentState)
            || evaluations == null)
        {
            return;
        }

        foreach (RoomAITacticalEvaluation evaluation in evaluations)
        {
            RoomActionCandidate candidate = evaluation?.candidate;
            RoomAICandidateExplanation explanation = evaluation?.explanation;
            if (candidate == null
                || explanation == null
                || explanation.isGoalkeeperPositioning
                || explanation.isRegularFinalThirdReorganization
                || explanation.isTackleDecision
                || explanation.isSuccessfulTackleReposition
                || explanation.isCornerDefenseMove
                || explanation.isCommittedAerialDefense
                || !IsMovementCandidate(candidate))
            {
                continue;
            }

            int defensiveScore = 0;
            bool assignedRole = snapshot.defensivePhasePlan != null && !string.IsNullOrEmpty(explanation.defensivePlanRole);
            if (assignedRole && snapshot.defensivePhasePlan.includesBoundedReceiverShotRoutes)
                evaluation.reasons.Add("Defensive estimate includes up to six reachable receiver-to-shot routes per threat; other routes and later replies remain unexpanded");
            if (assignedRole && snapshot.defensivePhasePlan.includesStaticKeeperFtpRelays)
                evaluation.reasons.Add("Keeper pickup estimate includes Quick Throw to FTP relays with a bounded one-hex defending reply (relative threat weight 0.6); attacking FTP move, F3, goalkeeper-wall interactions and later routes remain unexpanded");
            if (candidate.step == RoomDecisionStep.ChooseActor && explanation.actorDistanceToBall >= 0)
            {
                defensiveScore = weights.defensiveActorBase
                    - ((assignedRole && explanation.actorHex != null && explanation.defensivePlanDestination != null
                        ? RoomAITacticalSafety.Distance(explanation.actorHex, explanation.defensivePlanDestination)
                        : explanation.actorDistanceToBall) * weights.defensiveActorBallDistancePenalty);
                defensiveScore += explanation.defensivePlanRole switch
                {
                    "PrimaryPressure" => 320,
                    "RecoveryCover" => 0,
                    "Cover" => 180,
                    "OutletDenial" => 140,
                    "SecondaryShooter" => 140,
                    "WeakFlank" => 100,
                    "DefensiveLine" => 80,
                    _ => 0
                };
            }
            else if (candidate.step == RoomDecisionStep.ChooseTarget)
            {
                if (explanation.targetDistanceToBall >= 0 && (!assignedRole || explanation.defensivePlanRole == "PrimaryPressure"))
                {
                    defensiveScore += weights.defensivePressBase
                        - (explanation.targetDistanceToBall * weights.defensiveBallDistancePenalty);
                }

                if (explanation.nearestOpponentDistance >= 0)
                {
                    defensiveScore += Math.Max(
                        0,
                        weights.defensiveMarkBase
                        - (Math.Abs(explanation.nearestOpponentDistance - 1)
                            * weights.defensiveMarkDistancePenalty));
                }

                int nearbyTeammates = CountNearbyTeammates(
                    snapshot,
                    explanation.targetHex,
                    explanation.actorTokenKey,
                    2);
                int crowdedTeammates = Math.Max(
                    0,
                    nearbyTeammates - weights.defensiveCrowdingFreeTeammates);
                defensiveScore -= crowdedTeammates * weights.defensiveCrowdingPenaltyPerExtraTeammate;

                if (MatchesCoordinates(explanation.targetHex, explanation.defensivePlanDestination))
                {
                    defensiveScore += explanation.defensivePlanRole switch
                    {
                        "PrimaryPressure" => 420,
                        "RecoveryCover" => 0,
                        "Cover" => 240,
                        "OutletDenial" => 210,
                        "SecondaryShooter" => 210,
                        "WeakFlank" => 180,
                        "DefensiveLine" => 120,
                        _ => 0
                    };
                }

                if (explanation.defensiveMoveEndsAdjacentToBall
                    && (snapshot.defensivePhasePlan == null || explanation.defensivePlanRole == "PrimaryPressure"))
                {
                    defensiveScore += 260;
                }
                else if (string.Equals(
                        explanation.defensivePlanRole,
                        "PrimaryPressure",
                        StringComparison.Ordinal)
                    && explanation.defensiveLegalAdjacencyAvailable
                    && explanation.targetDistanceToBall == 2)
                {
                    defensiveScore -= 360;
                }
            }

            var assignment = snapshot.defensivePhasePlan?.assignments.FirstOrDefault(item => item.tokenKey == explanation.actorTokenKey);
            if (assignment != null && (assignment.threatReduction > 0f || assignment.role == "EmergencyRecovery")
                && (candidate.step == RoomDecisionStep.ChooseActor || MatchesCoordinates(explanation.targetHex, assignment.destination)))
            {
                int coordinated = (int)Math.Round(assignment.threatReduction * 5000f) + Math.Max(0, 5 - assignment.movePriority) * 180;
                defensiveScore += coordinated;
                evaluation.tier = RoomAITacticalTier.SafeProgression;
                evaluation.reasons.Add($"+{coordinated} coordinated {assignment.role}: threat reduction={assignment.threatReduction:0.000}, move priority={assignment.movePriority + 1}");
            }
            evaluation.score += defensiveScore;
            evaluation.tier = defensiveScore > 0
                ? RoomAITacticalTier.SafeProgression
                : defensiveScore < 0
                    ? RoomAITacticalTier.Risky
                    : RoomAITacticalTier.Neutral;
            evaluation.reasons.Insert(
                0,
                $"{(defensiveScore >= 0 ? "+" : string.Empty)}{defensiveScore} defensive pressure/marking shape");
        }

        evaluations.Sort(CompareEvaluations);
    }

    private static bool MatchesCoordinates(
        RoomHexCoordinates left,
        RoomHexCoordinates right)
    {
        return left != null && right != null && left.x == right.x && left.z == right.z;
    }

    private static void ApplyOptionalGoalkeeperMovementRestraint(
        RoomAIDecisionSnapshot snapshot,
        List<RoomAITacticalEvaluation> evaluations,
        RoomAITacticalWeights weights)
    {
        if (snapshot == null
            || evaluations == null)
        {
            return;
        }

        foreach (RoomAITacticalEvaluation evaluation in evaluations)
        {
            RoomAICandidateExplanation explanation = evaluation?.explanation;
            RoomActionCandidate candidate = evaluation?.candidate;
            if (candidate == null
                || explanation?.actorToken?.isGoalKeeper != true
                || (!string.Equals(candidate.manager, nameof(MovementPhaseManager), StringComparison.Ordinal)
                    && !string.Equals(candidate.manager, nameof(FinalThirdManager), StringComparison.Ordinal)))
            {
                continue;
            }

            bool attackingTeamDecision = string.Equals(
                snapshot.expectedTeam,
                snapshot.teamInAttack,
                StringComparison.OrdinalIgnoreCase);
            if (RoomAITacticalSafety.IsGoalkeeperRecovery(snapshot, explanation)) continue;
            if (!snapshot.attackHasPossession && explanation.passRace is { collectionPhases: 1, opponentCollectsFirst: false }
                && !evaluations.Any(other => other != evaluation && other.candidate?.step == candidate.step
                    && other.explanation?.actorToken is { isGoalKeeper: false }
                    && other.explanation.passRace is { collectionPhases: 1, opponentCollectsFirst: false }))
                continue;
            if (attackingTeamDecision && candidate.step == RoomDecisionStep.ChooseActor)
            {
                evaluation.score -= weights.attackingGoalkeeperOptionalMovePenalty * 4;
                evaluation.tier = RoomAITacticalTier.Risky;
                evaluation.reasons.Insert(
                    0,
                    "attacking movement excludes the goalkeeper because no previewed sweeper/support continuation exists");
                continue;
            }

            float improvement = explanation.goalkeeperPositioningCurrentMaximumExpectedGoals >= 0f
                && explanation.goalkeeperPositioningMaximumExpectedGoals >= 0f
                    ? explanation.goalkeeperPositioningCurrentMaximumExpectedGoals
                        - explanation.goalkeeperPositioningMaximumExpectedGoals
                    : 0f;
            bool actorSelectionEmergency = candidate.step == RoomDecisionStep.ChooseActor
                && explanation.goalkeeperPositioningCurrentMaximumExpectedGoals >= 0.45f
                && explanation.goalkeeperPositioningCurrentOutfieldBlockerCount < 2;
            bool exceptionalEmergency = actorSelectionEmergency
                || (improvement >= 0.10f
                && explanation.goalkeeperOwnGoalDistanceDelta <= 0
                && explanation.goalkeeperPositioningCurrentOutfieldBlockerCount < 2);
            if (exceptionalEmergency)
            {
                continue;
            }

            int penalty = string.Equals(
                    snapshot.expectedTeam,
                    snapshot.teamInAttack,
                    StringComparison.OrdinalIgnoreCase)
                ? weights.attackingGoalkeeperOptionalMovePenalty
                : weights.optionalGoalkeeperMovementPenalty;
            evaluation.score -= penalty;
            evaluation.tier = RoomAITacticalTier.Risky;
            evaluation.reasons.Insert(
                0,
                $"-{penalty} keeps the goalkeeper near goal because this optional move does not materially reduce "
                + "the opponent's best live shooting outcome");
        }

        evaluations.Sort(CompareEvaluations);
    }

    private static void ApplyKickoffSetupPriority(
        RoomAIDecisionSnapshot snapshot,
        List<RoomAITacticalEvaluation> evaluations,
        RoomAITacticalWeights weights)
    {
        if (snapshot == null || evaluations == null || !IsKickoffSetupState(snapshot.currentState))
        {
            return;
        }

        int currentPenalty = CalculateKickoffFormationPenalty(snapshot, null, null);
        int bestImprovement = 0;
        foreach (RoomAITacticalEvaluation evaluation in evaluations)
        {
            RoomActionCandidate candidate = evaluation?.candidate;
            RoomAICandidateExplanation explanation = evaluation?.explanation;
            if (candidate?.actionType != RoomActionType.SetupMove
                || explanation?.actorToken == null
                || explanation.targetHex == null)
            {
                continue;
            }

            int projectedPenalty = CalculateKickoffFormationPenalty(
                snapshot,
                explanation.actorToken.tokenKey,
                explanation.targetHex);
            bestImprovement = Math.Max(bestImprovement, currentPenalty - projectedPenalty);
        }

        bool frontLineReady = snapshot.expectedTeam != snapshot.teamInAttack || snapshot.tokens
            .Where(token => token.isPlaying && token.teamSide == snapshot.expectedTeam && token.jerseyNumber is 7 or 9 or 10 or 11)
            .All(token => token.currentHex?.x == 0 || token.tokenKey == snapshot.kickoffPlan?.receiverTokenKey
                || token.tokenKey == snapshot.kickoffPlan?.takerTokenKey);
        bool formationAcceptable = currentPenalty <= 24 && frontLineReady;
        bool noImprovingMove = bestImprovement <= 0;
        foreach (RoomAITacticalEvaluation evaluation in evaluations)
        {
            RoomActionCandidate candidate = evaluation?.candidate;
            RoomAICandidateExplanation explanation = evaluation?.explanation;
            if (candidate == null || explanation == null)
            {
                continue;
            }

            bool confirmsSetup = candidate.actionType == RoomActionType.Confirm
                || ((candidate.step == RoomDecisionStep.Confirm
                        || candidate.step == RoomDecisionStep.InterruptionChoice)
                    && (string.Equals(candidate.key, "Space", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(candidate.key, "Enter", StringComparison.OrdinalIgnoreCase)));
            if (confirmsSetup)
            {
                bool budgetReached = string.Equals(
                    candidate.setupTerminalReason,
                    "move budget reached",
                    StringComparison.Ordinal);
                bool needsOpening = snapshot.expectedTeam == snapshot.teamInAttack && snapshot.kickoffPlan != null;
                bool openingReady = !needsOpening || snapshot.kickoffPlan.firstActionReady;
                bool shouldConfirm = formationAcceptable && openingReady || noImprovingMove || budgetReached;
                int confirmationScore = shouldConfirm
                    ? weights.kickoffSetupConfirmBase + 100000
                    : -100000;
                evaluation.score += confirmationScore;
                evaluation.tier = shouldConfirm
                    ? RoomAITacticalTier.SafeProgression
                    : RoomAITacticalTier.Risky;
                candidate.setupFormationPenaltyBefore = currentPenalty;
                candidate.setupFormationPenaltyAfter = currentPenalty;
                candidate.setupTerminalReason = budgetReached
                    ? "move budget reached"
                        : formationAcceptable && openingReady
                        ? "formation acceptable"
                        : noImprovingMove
                            ? "no unvisited legal move improves the formation"
                            : "improving move or opening preparation remains";
                evaluation.reasons.Insert(
                    0,
                    $"{Signed(confirmationScore)} confirmation: {candidate.setupTerminalReason}; formation penalty={currentPenalty}");
                if (needsOpening) evaluation.reasons.Add("Opening readiness=" + openingReady + "; " + snapshot.kickoffPlan.rationale);
                continue;
            }

            if (candidate.actionType != RoomActionType.SetupMove || explanation.actorToken == null)
            {
                continue;
            }

            if (candidate.step == RoomDecisionStep.ChooseTarget && explanation.targetHex != null)
            {
                int projectedPenalty = CalculateKickoffFormationPenalty(
                    snapshot,
                    explanation.actorToken.tokenKey,
                    explanation.targetHex);
                int improvement = currentPenalty - projectedPenalty;
                int moveScore = improvement > 0
                    ? 50000 + (improvement * weights.kickoffSetupImprovementScale)
                    : -100000 + (improvement * weights.kickoffSetupImprovementScale);
                evaluation.score += moveScore;
                evaluation.tier = improvement > 0
                    ? RoomAITacticalTier.SafeProgression
                    : improvement < 0
                        ? RoomAITacticalTier.Risky
                        : RoomAITacticalTier.Neutral;
                candidate.setupFormationPenaltyBefore = currentPenalty;
                candidate.setupFormationPenaltyAfter = projectedPenalty;
                evaluation.reasons.Insert(
                    0,
                    $"{Signed(moveScore)} pre-kickoff shape changes formation penalty "
                    + $"from {currentPenalty} to {projectedPenalty}");
                continue;
            }

            if (candidate.step == RoomDecisionStep.ChooseActor)
            {
                int urgency = CalculateKickoffTokenPenalty(snapshot, explanation.actorToken, explanation.actorToken.currentHex);
                int actorScore = urgency * weights.kickoffSetupActorUrgencyScale;
                evaluation.score += actorScore;
                evaluation.tier = urgency > 0
                    ? RoomAITacticalTier.SafeProgression
                    : RoomAITacticalTier.Neutral;
                evaluation.reasons.Insert(
                    0,
                    $"{Signed(actorScore)} setup urgency={urgency} for {explanation.actorToken.tokenKey}");
            }
        }

        evaluations.Sort(CompareEvaluations);
    }

    private static bool IsKickoffSetupState(string state)
    {
        return string.Equals(state, "KickOffSetup", StringComparison.OrdinalIgnoreCase)
            || string.Equals(state, "PostGoalKickOffSetup", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsEarlyDefendingKickoffSetup(RoomAIDecisionSnapshot snapshot)
    {
        return IsKickoffSetupState(snapshot.currentState)
            && snapshot.clock != null && snapshot.clock.currentHalf == 1
            && snapshot.clock.currentHalfRegulationSeconds >= 0f
            && snapshot.clock.currentHalfRegulationSeconds < 5 * 60
            && !snapshot.clock.isHalfExpired && !snapshot.clock.isMatchComplete
            && snapshot.expectedTeam is "Home" or "Away"
            && snapshot.teamInAttack is "Home" or "Away"
            && snapshot.expectedTeam != snapshot.teamInAttack;
    }

    private static void ApplyEarlyDefendingKickoffFrontLinePriority(
        RoomAIDecisionSnapshot snapshot, List<RoomAITacticalEvaluation> evaluations)
    {
        // This is an opening press, not a possession/reception decision. Compare
        // legal placements geometrically after genuine safety constraints, so
        // generic tackle avoidance cannot strand a forward behind halfway.
        if (!IsEarlyDefendingKickoffSetup(snapshot)) return;

        var frontLine = snapshot.tokens.Where(token => token != null && token.isPlaying
            && !token.isSentOff && !token.isGoalKeeper && token.currentHex != null
            && token.teamSide == snapshot.expectedTeam && token.jerseyNumber is 7 or 9 or 10 or 11).ToList();
        if (frontLine.Count == 0) return;

        var placements = evaluations.Select(evaluation =>
        {
            var facts = evaluation.explanation;
            bool moves = evaluation.candidate.manager == nameof(KickoffManager)
                && evaluation.candidate.actionType == RoomActionType.SetupMove
                && evaluation.candidate.step == RoomDecisionStep.ChooseTarget && facts?.targetHex != null;
            int flankChanges = 0, distance = 0, shapePenalty = 0;
            foreach (var token in frontLine)
            {
                var position = moves && facts.actorToken?.tokenKey == token.tokenKey
                    ? facts.targetHex : token.currentHex;
                if (token.jerseyNumber is 7 or 11 && token.currentHex.z != 0
                    && position.z * token.currentHex.z < 0)
                    flankChanges++;
                distance += Math.Abs(position.x);
                if (TryGetHumanLedKickoffTarget(snapshot, token, out var target))
                    shapePenalty += HexDistance(position, target);
            }
            return (evaluation, flankChanges, distance, shapePenalty);
        }).OrderBy(item => item.flankChanges).ThenBy(item => item.distance)
            .ThenBy(item => item.shapePenalty).ToList();

        int priority = 0;
        for (int index = 0; index < placements.Count; index++)
        {
            var placement = placements[index];
            if (index > 0)
            {
                var previous = placements[index - 1];
                if ((placement.flankChanges, placement.distance, placement.shapePenalty)
                    != (previous.flankChanges, previous.distance, previous.shapePenalty))
                    priority++;
            }
            placement.evaluation.kickoffFrontLinePriority = priority;
            placement.evaluation.reasons.Insert(0,
                $"Early defending kickoff: front four prioritize halfway; projected line distance={placement.distance}, "
                + $"flank changes={placement.flankChanges}, front shape penalty={placement.shapePenalty}; "
                + "receiver tackle-avoidance/support bonuses do not override the opening press");
        }
        evaluations.Sort(CompareEvaluations);
    }

    private static int CalculateKickoffFormationPenalty(
        RoomAIDecisionSnapshot snapshot,
        string movedTokenKey,
        RoomHexCoordinates movedHex)
    {
        if (snapshot?.tokens == null)
        {
            return 0;
        }

        int penalty = 0;
        foreach (RoomAITokenSnapshot token in snapshot.tokens)
        {
            if (token == null || !token.isPlaying || token.currentHex == null)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(snapshot.expectedTeam)
                && !string.Equals(snapshot.expectedTeam, "Neutral", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(token.teamSide, snapshot.expectedTeam, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            RoomHexCoordinates tokenHex = string.Equals(token.tokenKey, movedTokenKey, StringComparison.OrdinalIgnoreCase)
                ? movedHex
                : token.currentHex;
            penalty += CalculateKickoffTokenPenalty(snapshot, token, tokenHex);
        }

        var opening = snapshot.kickoffPlan;
        if (opening?.firstPassEvaluated == true && opening.firstActionReady && opening.landingHex != null
            && opening.team == snapshot.expectedTeam && snapshot.expectedTeam == snapshot.teamInAttack)
        {
            // A ready direct reception is no longer ready if this setup move
            // takes the receiver away from its committed landing cell.
            if (opening.collectionCost == 0 && movedTokenKey == opening.receiverTokenKey && movedHex != null
                && !MatchesCoordinates(movedHex, opening.landingHex)) penalty += 96;
            penalty += (int)Math.Ceiling(8f * RoomAITacticalSafety.KickoffOpeningRecoveryExposure(snapshot,
                movedTokenKey, movedHex));
            bool leftToRight = snapshot.expectedTeam == "Home"
                ? snapshot.homeTeamDirection == "LeftToRight" : snapshot.awayTeamDirection == "LeftToRight";
            int sign = leftToRight ? 1 : -1;
            foreach (var back in snapshot.tokens.Where(t => t != null && t.isPlaying && !t.isSentOff
                         && t.teamSide == snapshot.expectedTeam && t.currentHex != null && t.jerseyNumber is 4 or 5
                         && t.tokenKey != opening.receiverTokenKey))
            {
                var at = back.tokenKey == movedTokenKey && movedHex != null ? movedHex : back.currentHex;
                // Keep the unit connected to the actual planned receiver. A
                // centre-back more than one movement behind that receiver has
                // a real recovery gap; preserve existing shape safeguards too.
                int depthGap = Math.Max(0, sign * (opening.landingHex.x - at.x) - Math.Max(1, back.pace));
                penalty += depthGap * 16;
            }
        }

        return penalty;
    }

    private static int CalculateKickoffTokenPenalty(
        RoomAIDecisionSnapshot snapshot,
        RoomAITokenSnapshot token,
        RoomHexCoordinates hex)
    {
        if (snapshot == null || token == null || hex == null)
        {
            return 0;
        }

        if (!TryGetHumanLedKickoffTarget(snapshot, token, out RoomHexCoordinates target))
        {
            return 0;
        }

        return HexDistance(hex, target) * 12;
    }

    private static bool TryGetHumanLedKickoffTarget(
        RoomAIDecisionSnapshot snapshot,
        RoomAITokenSnapshot token,
        out RoomHexCoordinates target)
    {
        target = null;
        var opening = snapshot?.kickoffPlan;
        if (opening?.isSetupProjection == true && token?.teamSide == opening.team)
        {
            if (token.tokenKey == opening.receiverTokenKey) { target = opening.receiverSetupHex; return target != null; }
            if (token.tokenKey == opening.takerTokenKey) { target = opening.takerSetupHex; return target != null; }
        }
        if (snapshot == null || token == null || token.jerseyNumber < 1 || token.jerseyNumber > 11)
        {
            return false;
        }

        bool isAttackingTeam = string.Equals(
            token.teamSide,
            snapshot.teamInAttack,
            StringComparison.OrdinalIgnoreCase);
        string direction = string.Equals(token.teamSide, "Home", StringComparison.OrdinalIgnoreCase)
            ? snapshot.homeTeamDirection
            : snapshot.awayTeamDirection;
        bool attacksRightToLeft = string.Equals(direction, "RightToLeft", StringComparison.OrdinalIgnoreCase);

        // These are the two shapes demonstrated in the saved human-controlled
        // kick-off: the kicking side commits its front line to halfway and raises
        // the supporting lines; the defending side pushes up to deny easy relays.
        // X is mirrored when a team attacks the opposite goal, while Z preserves
        // the jersey's established side of the pitch.
        (int x, int z) canonical = isAttackingTeam
            ? GetAttackingKickoffTarget(snapshot, token)
            : GetDefendingKickoffTarget(snapshot, token);
        // Keep setup targets stable across individual moves. Receiver safety is based on
        // the defending squad's plausible completed press (pace/tackling), not transient
        // left/right occupancy while either side is still arranging itself.
        bool canonicalAttacksRightToLeft = !isAttackingTeam;
        int x = attacksRightToLeft == canonicalAttacksRightToLeft
            ? canonical.x
            : -canonical.x;
        target = new RoomHexCoordinates { x = x, z = canonical.z };
        return true;
    }

    private static (int x, int z) GetAttackingKickoffTarget(
        RoomAIDecisionSnapshot snapshot,
        RoomAITokenSnapshot token)
    {
        RoomAITokenSnapshot kicker = snapshot.kickoffPlan?.team == token.teamSide
            ? snapshot.tokens.FirstOrDefault(player => player.tokenKey == snapshot.kickoffPlan.takerTokenKey)
            : SelectKickoffTaker(snapshot, token.teamSide);
        RoomAITokenSnapshot receiver = snapshot.kickoffPlan?.team == token.teamSide
            ? snapshot.tokens.FirstOrDefault(player => player.tokenKey == snapshot.kickoffPlan.receiverTokenKey)
            : SelectKickoffReceiver(snapshot, token.teamSide, kicker);
        int maximumPressingPace = snapshot.tokens
            .Where(opponent => opponent != null
                && opponent.isPlaying
                && !string.Equals(opponent.teamSide, token.teamSide, StringComparison.OrdinalIgnoreCase)
                && !opponent.isGoalKeeper
                && opponent.jerseyNumber is 6 or 7 or 8 or 9 or 10 or 11)
            .Select(opponent => opponent.pace)
            .DefaultIfEmpty(5)
            .Max();
        int safeReceiverX = -Math.Max(4, Math.Min(8, maximumPressingPace + 1));
        bool pushForMultiGoalRecovery = IsMultiGoalRecoveryWindow(snapshot, token.teamSide);

        if (token == kicker)
        {
            return (0, 0);
        }

        if (token == receiver)
        {
            int receiverZ = token.jerseyNumber == 4 ? -2
                : token.jerseyNumber == 5 ? 2
                : token.jerseyNumber == 6 ? -1
                : 1;
            return (safeReceiverX, receiverZ);
        }

        bool receiverIsCentreBack = receiver?.jerseyNumber is 4 or 5;
        int Side(int fallback) => token.currentHex?.z > 0 ? 1 : token.currentHex?.z < 0 ? -1 : fallback;
        (int x, int z) target = token.jerseyNumber switch
        {
            1 => (-16, 0),
            2 => (safeReceiverX + 2, Side(-1) * 8),
            3 => (safeReceiverX + 2, Side(1) * 8),
            4 => (receiverIsCentreBack ? safeReceiverX : safeReceiverX - 2, -3),
            5 => (receiverIsCentreBack ? safeReceiverX : safeReceiverX - 2, 3),
            6 => (-2, -2),
            8 => (-2, 2),
            9 => (0, 4),
            10 => (0, -4),
            7 => (0, Side(-1) * 9),
            11 => (0, Side(1) * 9),
            _ => HumanLedAttackingKickoffLeftToRight[token.jerseyNumber]
        };
        if (pushForMultiGoalRecovery)
        {
            int advance = token.jerseyNumber == 1 ? 0 : 2;
            target.x = Math.Min(0, target.x + advance);
        }
        return target;
    }

    private static (int x, int z) GetDefendingKickoffTarget(
        RoomAIDecisionSnapshot snapshot,
        RoomAITokenSnapshot token)
    {
        int pressHeight = CalculateKickoffPressHeight(snapshot, token.teamSide);
        int Side(int fallback) => token.currentHex?.z > 0 ? 1 : token.currentHex?.z < 0 ? -1 : fallback;
        if (token.jerseyNumber is 6 or 8)
        {
            // Keep the midfield within mutual support distance. Width is derived
            // from the slower midfielder's reach, not a named player's hex.
            int supportingPace = snapshot.tokens.Where(player => player.isPlaying && player.teamSide == token.teamSide
                    && player.jerseyNumber is 6 or 8).Select(player => player.pace).DefaultIfEmpty(4).Min();
            int width = Math.Max(1, Math.Min(2, supportingPace / 2));
            return (pressHeight / 2, Side(token.jerseyNumber == 6 ? -1 : 1) * width);
        }
        return token.jerseyNumber switch
        {
            1 => (16, 0),
            2 => (pressHeight - 1, Side(1) * 8),
            3 => (pressHeight - 1, Side(-1) * 8),
            4 => (pressHeight, -3),
            5 => (pressHeight, 3),
            7 => (0, Side(1) * 8),
            9 => (0, 5),
            10 => (0, -5),
            11 => (0, Side(-1) * 8),
            _ => HumanLedDefendingKickoffRightToLeft[token.jerseyNumber]
        };
    }

    private static int CalculateKickoffPressHeight(RoomAIDecisionSnapshot snapshot, string teamSide)
    {
        List<RoomAITokenSnapshot> pressers = snapshot.tokens
            .Where(candidate => candidate != null
                && candidate.isPlaying
                && string.Equals(candidate.teamSide, teamSide, StringComparison.OrdinalIgnoreCase)
                && candidate.jerseyNumber is 6 or 7 or 8 or 9 or 10 or 11)
            .ToList();
        float averagePace = pressers.Count == 0 ? 4f : (float)pressers.Average(candidate => candidate.pace);
        float averageTackling = pressers.Count == 0 ? 3f : (float)pressers.Average(candidate => candidate.tackling);
        return averagePace + averageTackling >= 9f ? 6 : 8;
    }

    private static RoomAITokenSnapshot SelectKickoffTaker(
        RoomAIDecisionSnapshot snapshot,
        string teamSide)
    {
        var eligible = snapshot.tokens
            .Where(candidate => candidate != null
                && candidate.isPlaying
                && !candidate.isSentOff && !candidate.isGoalKeeper && candidate.currentHex != null
                && string.Equals(candidate.teamSide, teamSide, StringComparison.OrdinalIgnoreCase)
                ).ToList();
        var central = eligible.Where(candidate => candidate.jerseyNumber is 6 or 8).ToList();
        return (central.Count > 0 ? central : eligible)
            .OrderBy(candidate => (candidate.shooting * 3)
                + (candidate.dribbling * 2)
                + candidate.highPass
                + candidate.pace)
            .ThenBy(candidate => candidate.jerseyNumber)
            .FirstOrDefault();
    }

    public static RoomAIKickoffPlan BuildKickoffPlan(RoomAIDecisionSnapshot snapshot)
    {
        if (snapshot == null || snapshot.expectedTeam != snapshot.teamInAttack && snapshot.expectedTeam != "Neutral") return null;
        var taker = !IsKickoffSetupState(snapshot.currentState) && snapshot.currentState != "KickOffTakerSelection"
            ? snapshot.tokens.FirstOrDefault(token => token != null && token.isPlaying && !token.isGoalKeeper
                && token.teamSide == snapshot.teamInAttack && HexDistance(token.currentHex, snapshot.ball) == 0)
            : SelectKickoffTaker(snapshot, snapshot.teamInAttack);
        var kickoff = MatchManager.Instance?.kickoffManager;
        return IsKickoffSetupState(snapshot.currentState) && kickoff != null
            ? kickoff.GetAutomatedOpeningPlan(snapshot.teamInAttack, () => RoomAIKickoffPlanner.Build(snapshot, taker))
            : RoomAIKickoffPlanner.Build(snapshot, taker);
    }

    private static void ApplyKickoffFirstActionPlan(RoomAIDecisionSnapshot snapshot, List<RoomAITacticalEvaluation> evaluations)
    {
        if (!evaluations.Any(item => item.explanation?.isKickoffDecision == true)) return;
        snapshot.kickoffPlan = BuildKickoffPlan(snapshot);
        var plan = snapshot.kickoffPlan;
        if (plan == null) return;
        var costed = evaluations.Where(item => item.candidate.actionType == RoomActionType.GroundPass)
            .Select(item => new { item, opening = RoomAIKickoffPlanner.FromPassFacts(snapshot, item.explanation) })
            .Where(pair => pair.opening != null).ToList();
        var direct = costed.Where(pair => pair.opening.collectionCost == 0)
            .OrderByDescending(pair => pair.opening.value).FirstOrDefault();
        foreach (var pair in costed)
        {
            var item = pair.item;
            var opening = pair.opening;
            int costPenalty = opening.collectionMovesConsumed * 50 + opening.collectionCost * 8
                + System.Math.Max(0, opening.collectionPhases - 1) * 150;
            item.score -= costPenalty;
            item.reasons.Insert(0, "Kickoff opening: " + opening.rationale);
            if (opening.collectionCost > 0 && direct != null && opening.value <= direct.opening.value)
            {
                item.safetyPriority = System.Math.Max(1, item.safetyPriority);
                item.tier = RoomAITacticalTier.Risky;
                item.reasons.Insert(0, "Kickoff retrieval consumes a movement and receiver Pace without increasing continuation value");
            }
        }
        evaluations.Sort(CompareEvaluations);
    }

    private static IEnumerator ApplyKickoffFirstActionPlanIncrementally(
        RoomAIDecisionSnapshot snapshot, List<RoomAITacticalEvaluation> evaluations)
    {
        if (!evaluations.Any(item => item.explanation?.isKickoffDecision == true)) yield break;
        // Build the same opening facts and populate the existing per-candidate
        // cache before choosing the opening. Each path forecast can then yield
        // to the caller's frame budget instead of processing the whole target
        // board in one uninterruptible provider step.
        foreach (var item in evaluations)
        {
            RoomAIKickoffPlanner.FromPassFacts(snapshot, item.explanation);
            yield return null;
        }
        ApplyKickoffFirstActionPlan(snapshot, evaluations);
    }

    private static RoomAITokenSnapshot SelectKickoffReceiver(
        RoomAIDecisionSnapshot snapshot,
        string teamSide,
        RoomAITokenSnapshot kicker)
    {
        return snapshot.tokens
            .Where(candidate => candidate != null
                && candidate.isPlaying
                && candidate != kicker
                && string.Equals(candidate.teamSide, teamSide, StringComparison.OrdinalIgnoreCase)
                && candidate.jerseyNumber is 4 or 5 or 6 or 8)
            .OrderByDescending(candidate => candidate.highPass)
            .ThenByDescending(candidate => candidate.pace)
            .ThenBy(candidate => candidate.jerseyNumber)
            .FirstOrDefault();
    }

    private static bool IsMultiGoalRecoveryWindow(
        RoomAIDecisionSnapshot snapshot,
        string teamSide)
    {
        if (snapshot?.clock == null || snapshot.halfDurationMinutes <= 0)
        {
            return false;
        }

        bool home = string.Equals(teamSide, "Home", StringComparison.OrdinalIgnoreCase);
        int deficit = home
            ? snapshot.awayScore - snapshot.homeScore
            : snapshot.homeScore - snapshot.awayScore;
        int laterRegulationHalves = Math.Max(
            0,
            snapshot.regulationHalfCount - snapshot.clock.currentHalf);
        float secondsRemaining = (snapshot.halfDurationMinutes * 60f)
            - snapshot.clock.currentHalfRegulationSeconds
            + (laterRegulationHalves * snapshot.halfDurationMinutes * 60f);
        return deficit >= 2 && secondsRemaining > 10f * 60f;
    }

    private static void ApplyKickoffTakerPriority(
        RoomAIDecisionSnapshot snapshot,
        List<RoomAITacticalEvaluation> evaluations)
    {
        if (snapshot == null
            || evaluations == null
            || !string.Equals(snapshot.currentState, "KickOffTakerSelection", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        RoomAITokenSnapshot kicker = snapshot.kickoffPlan != null
            ? snapshot.tokens.FirstOrDefault(token => token.tokenKey == snapshot.kickoffPlan.takerTokenKey)
            : SelectKickoffTaker(snapshot, snapshot.teamInAttack);
        foreach (RoomAITacticalEvaluation evaluation in evaluations)
        {
            RoomAICandidateExplanation explanation = evaluation?.explanation;
            if (explanation?.actorToken == null || kicker == null)
            {
                continue;
            }

            if (string.Equals(explanation.actorToken.tokenKey, kicker.tokenKey, StringComparison.OrdinalIgnoreCase))
            {
                evaluation.score += 500;
                evaluation.tier = RoomAITacticalTier.SafeProgression;
                evaluation.reasons.Insert(0, "+500 selects the less attack-useful central midfielder as kickoff taker");
            }
            else if (explanation.actorToken.jerseyNumber is 4 or 5 or 6 or 8)
            {
                evaluation.score -= 180;
                evaluation.reasons.Insert(0, "-180 preserves the designated central receiver; kickoff taker cannot collect the ball");
            }
        }

        evaluations.Sort(CompareEvaluations);
    }

    private static void ApplyDesperateKickoffRush(
        RoomAIDecisionSnapshot snapshot,
        List<RoomAITacticalEvaluation> evaluations,
        RoomAITacticalWeights weights)
    {
        if (snapshot == null
            || evaluations == null
            || snapshot.clock == null
            || snapshot.currentState?.IndexOf("kickoff", StringComparison.OrdinalIgnoreCase) < 0)
        {
            return;
        }

        bool homeAttacks = string.Equals(snapshot.teamInAttack, "Home", StringComparison.OrdinalIgnoreCase);
        int deficit = homeAttacks
            ? snapshot.awayScore - snapshot.homeScore
            : snapshot.homeScore - snapshot.awayScore;
        int actionsRemaining = snapshot.clock.extraActionsRemaining;
        bool oneGoalLateSequence = deficit == 1
            && snapshot.clock.currentHalf >= snapshot.regulationHalfCount
            && snapshot.clock.extraActionsDetermined
            && actionsRemaining >= 3
            && actionsRemaining <= 5;
        bool multiGoalRecovery = IsMultiGoalRecoveryWindow(snapshot, snapshot.teamInAttack);
        bool extremeDesperation = oneGoalLateSequence || multiGoalRecovery;
        if (!extremeDesperation)
        {
            return;
        }

        RoomAITokenSnapshot numberTen = snapshot.tokens.FirstOrDefault(token => token != null
            && token.isPlaying
            && token.jerseyNumber == 10
            && string.Equals(token.teamSide, snapshot.teamInAttack, StringComparison.OrdinalIgnoreCase));
        if (numberTen == null || numberTen.dribbling < 5)
        {
            return;
        }

        foreach (RoomAITacticalEvaluation evaluation in evaluations)
        {
            RoomAICandidateExplanation explanation = evaluation?.explanation;
            RoomActionCandidate candidate = evaluation?.candidate;
            if (candidate == null
                || explanation == null
                || candidate.actionType != RoomActionType.GroundPass
                || !string.Equals(
                    explanation.targetTokenKey,
                    numberTen.tokenKey,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            evaluation.score += weights.desperateKickoffDribblerRush;
            evaluation.score -= weights.desperateKickoffRushTurnoverRisk;
            evaluation.tier = RoomAITacticalTier.SafeProgression;
            evaluation.reasons.Insert(
                0,
                $"+{weights.desperateKickoffDribblerRush} emergency kickoff to dribbling {numberTen.dribbling} number 10 "
                + $"while trailing by {deficit} with {actionsRemaining} action(s) left; "
                + $"-{weights.desperateKickoffRushTurnoverRisk} immediate central turnover exposure");
        }

        evaluations.Sort(CompareEvaluations);
    }

    private static int KickoffProgress(
        RoomAIDecisionSnapshot snapshot,
        RoomAITokenSnapshot token,
        RoomHexCoordinates hex)
    {
        if (snapshot == null || token == null || hex == null)
        {
            return 0;
        }

        string direction = string.Equals(token.teamSide, "Home", StringComparison.OrdinalIgnoreCase)
            ? snapshot.homeTeamDirection
            : snapshot.awayTeamDirection;
        return string.Equals(direction, "RightToLeft", StringComparison.OrdinalIgnoreCase)
            ? 19 - hex.x
            : hex.x + 19;
    }

    private static int HexDistance(RoomHexCoordinates left, RoomHexCoordinates right)
    {
        if (left == null || right == null)
        {
            return int.MaxValue;
        }

        int dx = left.x - right.x;
        int dz = left.z - right.z;
        return (Math.Abs(dx) + Math.Abs(dz) + Math.Abs(dx + dz)) / 2;
    }

    private static string Signed(int value)
    {
        return value >= 0 ? $"+{value}" : value.ToString();
    }

    private static bool IsPassAction(RoomActionType actionType)
    {
        return actionType == RoomActionType.GroundPass
            || actionType == RoomActionType.QuickThrow
            || actionType == RoomActionType.HighPass
            || actionType == RoomActionType.LongBall
            || actionType == RoomActionType.FirstTimePass
            || actionType == RoomActionType.GoalkeeperKick
            || actionType == RoomActionType.Kickoff;
    }

    private static bool IsMovementCandidate(RoomActionCandidate candidate)
    {
        return candidate.actionType == RoomActionType.StartMovement
            || candidate.actionType == RoomActionType.FinalThird
            || candidate.actionType == RoomActionType.SetupMove
            || string.Equals(candidate.manager, "FinalThirdManager", StringComparison.Ordinal)
            || string.Equals(candidate.manager, "MovementPhaseManager", StringComparison.Ordinal);
    }

    private static int CountNearbyTeammates(
        RoomAIDecisionSnapshot snapshot,
        RoomHexCoordinates target,
        string actorTokenKey,
        int radius)
    {
        if (snapshot?.tokens == null || target == null)
        {
            return 0;
        }

        int count = 0;
        foreach (RoomAITokenSnapshot token in snapshot.tokens)
        {
            if (token == null
                || !token.isPlaying
                || token.currentHex == null
                || string.Equals(token.tokenKey, actorTokenKey, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(token.teamSide, snapshot.expectedTeam, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            int distance = RoomAITacticalSafety.Distance(token.currentHex, target);
            if (distance <= radius)
            {
                count++;
            }
        }

        return count;
    }

    private static IEnumerator ApplyHardLookaheadIncrementally(RoomAIDecisionSnapshot snapshot, List<RoomAITacticalEvaluation> evaluations,
        RoomAITacticalWeights weights)
    {
        if (evaluations == null)
        {
            yield break;
        }

        int lookaheadCount = Math.Min(12, evaluations.Count);
        for (int index = 0; index < lookaheadCount; index++)
        {
            RoomAITacticalEvaluation evaluation = evaluations[index];
            RoomAICandidateExplanation explanation = evaluation?.explanation;
            if (explanation == null)
            {
                continue;
            }

            yield return null;
            int lookaheadScore = 0;
            if (index < RoomAIActionSequence.CandidateLimit)
                explanation.actionSequence ??= RoomAIActionSequence.Evaluate(snapshot, explanation);
            // Only the first action is staged. Subsequent choices are re-evaluated
            // against the actual board after chance events/opponent responses.
            if (explanation.candidate.step == RoomDecisionStep.ChooseActionType
                && explanation.actionSequence?.steps.FirstOrDefault()?.action == "High pass")
            {
                explanation.highPassBestTargetHex = explanation.actionSequence.steps[0].ballHex;
                explanation.targetHex = explanation.highPassBestTargetHex;
                explanation.highPassBestCollectorTokenKey = explanation.actionSequence.steps
                    .FirstOrDefault(s => s.action.StartsWith("Win header", StringComparison.Ordinal) || s.action == "Collect")?.actorTokenKey;
            }
            RoomAIPossessionChainSummary continuation = explanation.actionSequence?.outcomes ?? explanation.possessionChain;
            if (explanation.actionSequence != null)
            {
                // Supersede the endpoint/partial-route threat already credited
                // by the cheap shortlist. Otherwise its much larger base bonus
                // can drown out a sequence which proves that shot unreachable.
                if (explanation.hasPossessionLevelPlan)
                    lookaheadScore += (int)Math.Round((continuation.goalProbability - explanation.possessionPlanThreatExpectedGoals) * 900f);
                if (explanation.hasPossessionLevelPlan)
                {
                    // Use the same later horizon for both the possession and
                    // chance components. A direct pass or header can have a
                    // very different outcome after collection and challenge,
                    // just as an aerial pass can after its scatter branches.
                    float retained = continuation.goalProbability + continuation.attackRetainsPossessionProbability
                        + continuation.attackingRestartProbability * .65f;
                    lookaheadScore += (int)Math.Round((retained - explanation.possessionPlanEffectiveRetentionProbability) * 420f);
                }
                if (explanation.isGroundPassContinuation && explanation.groundPassRouteExpectedGoals >= 0f)
                    lookaheadScore += (int)Math.Round((continuation.goalProbability - explanation.groundPassRouteExpectedGoals)
                        * weights.groundPassRouteExpectedGoalsScale);
            }
            if (continuation != null)
            {
                RoomAIPossessionChainSummary chain = continuation;
                lookaheadScore += (int)Math.Round(chain.goalProbability * 180f);
                lookaheadScore += (int)Math.Round(chain.attackRetainsPossessionProbability * 35f);
                lookaheadScore += (int)Math.Round(chain.attackingRestartProbability * 18f);
                lookaheadScore -= (int)Math.Round(chain.defenseWinsPossessionProbability * 70f);
                lookaheadScore -= (int)Math.Round(chain.defendingRestartProbability * 28f);
                lookaheadScore -= (int)Math.Round(chain.unresolvedProbability * 8f);
            }

            if (explanation.actionSequence == null && explanation.expectedGoals >= 0f)
            {
                lookaheadScore += (int)Math.Round(explanation.expectedGoals * 45f);
            }

            if (explanation.actionSequence == null && explanation.estimatedBallRetentionProbability >= 0f)
            {
                lookaheadScore += (int)Math.Round((explanation.estimatedBallRetentionProbability - 0.5f) * 20f);
            }

            lookaheadScore += Math.Min(12, explanation.friendlySupportWithinRange * 3);
            lookaheadScore -= Math.Min(20, explanation.opponentsWithinTackleReach * 8);

            evaluation.score += lookaheadScore;
            evaluation.reasons.Add($"{(lookaheadScore >= 0 ? "+" : string.Empty)}{lookaheadScore} hard lookahead risk/reward");
            if (explanation.actionSequence != null)
                evaluation.reasons.Add($"Connected sequence: {string.Join(" -> ", explanation.actionSequence.steps.Select(s => s.action))}; "
                    + $"goal {continuation.goalProbability:F3}, unresolved {continuation.unresolvedProbability:F3}, "
                    + $"{explanation.actionSequence.alternativesEvaluated} bounded alternatives");
            if (explanation.actionSequence != null && explanation.candidate.actionType is RoomActionType.HighPass or RoomActionType.GoalkeeperKick)
                evaluation.reasons.Add("HP attacking and defending free moves are not expanded in this sequence; the static contest forecast must be reassessed");
            if (explanation.actionSequence?.secondary != null)
                evaluation.reasons.Add($"Secondary outcomes: {explanation.actionSequence.secondary.rebounds.Count} rebound branches; "
                    + $"{explanation.actionSequence.secondary.tackleExits.Count} legal tackle exits; "
                    + $"worst-exit steal-back {explanation.actionSequence.secondary.postTackleRecoveryProbability:F3}");
        }

        evaluations.Sort(CompareEvaluations);
    }

    /// <summary>
    /// Tactical tiers are intentionally compared before raw scores, but an optional
    /// Neutral forfeit must not use that rule to beat every useful movement which
    /// happened to be classified Risky. Put the forfeit into the best worthwhile
    /// move's tier when the move has a clear numerical advantage; genuinely poor
    /// movement still loses to keeping the current shape.
    /// </summary>
    private static void ApplyForfeitOpportunityCost(
        List<RoomAITacticalEvaluation> evaluations,
        RoomAITacticalWeights weights)
    {
        if (evaluations == null || evaluations.Count == 0)
        {
            return;
        }

        List<RoomAITacticalEvaluation> movementOptions = evaluations
            .Where(evaluation => evaluation?.candidate != null
                && evaluation.safetyPriority == 0
                && !evaluation.candidate.isForfeit
                && (evaluation.candidate.actionType == RoomActionType.StartMovement
                    || evaluation.candidate.actionType == RoomActionType.FinalThird))
            .ToList();
        if (movementOptions.Count == 0)
        {
            return;
        }

        RoomAITacticalEvaluation bestMovementByScore = movementOptions
            .OrderByDescending(evaluation => evaluation.score)
            .ThenBy(evaluation => evaluation.originalIndex)
            .First();
        foreach (RoomAITacticalEvaluation forfeit in evaluations.Where(
                     evaluation => evaluation?.candidate?.isForfeit == true
                         && (string.Equals(evaluation.candidate.manager, nameof(MovementPhaseManager), StringComparison.Ordinal)
                             || string.Equals(evaluation.candidate.manager, nameof(FinalThirdManager), StringComparison.Ordinal))))
        {
            if (bestMovementByScore.score < forfeit.score + weights.movementForfeitScoreMargin
                || bestMovementByScore.tier.CompareTo(forfeit.tier) <= 0)
            {
                continue;
            }

            forfeit.tier = bestMovementByScore.tier;
            forfeit.reasons.Insert(
                0,
                $"forfeit yields to a worthwhile movement scoring {bestMovementByScore.score} "
                + $"versus {forfeit.score}");
        }

        evaluations.Sort(CompareEvaluations);
    }

    private static void PreserveRemainingMovementAfterSelection(
        RoomAIDecisionSnapshot snapshot, List<RoomAITacticalEvaluation> evaluations)
    {
        var match = MatchManager.Instance;
        var movement = match?.movementPhaseManager;
        if (snapshot?.currentState != "MovementPhase" || match == null || match.difficulty_level < 3
            || movement == null || !movement.isAwaitingHexDestination || movement.isDribblerRunning
            || movement.selectedToken == null || movement.selectedToken.IsDribbler
            || movement.selectedToken.IsGoalKeeper || !snapshot.attackHasPossession
            || snapshot.expectedTeam != snapshot.teamInAttack) return;
        var forfeit = evaluations.FirstOrDefault(e => e?.candidate?.isForfeit == true
            && e.candidate.manager == nameof(MovementPhaseManager) && e.candidate.key == "X");
        if (forfeit == null || evaluations.Any(e => e?.candidate?.step == RoomDecisionStep.ChooseTarget
            && e.safetyPriority == 0 && e.explanation?.supportsConcreteContinuation == true)) return;
        foreach (var item in evaluations.Where(e => e?.explanation?.isAttackingOffBallMove == true
                     && e.candidate?.step == RoomDecisionStep.ChooseTarget))
            RoomAITacticalSafety.EvaluateDefensiveLine(snapshot, item.explanation);
        var fallback = evaluations.Where(e => e?.candidate?.actionType == RoomActionType.StartMovement
                && e.candidate.step == RoomDecisionStep.ChooseTarget && e.candidate.targetHex != null
                && e.explanation?.isAttackingOffBallMove == true && !e.explanation.supportsConcreteContinuation
                && !e.explanation.defensiveLineCreatesGap && !e.explanation.defensiveLineLeavesFullback
                && RoomAITacticalSafety.Distance(e.explanation.actorHex, e.explanation.targetHex) is > 0 and <= 2)
            .OrderBy(e => RoomAITacticalSafety.Distance(e.explanation.actorHex, e.explanation.targetHex))
            .ThenByDescending(e => e.score).FirstOrDefault();
        if (fallback == null) return;
        fallback.safetyPriority = 0;
        fallback.tier = forfeit.tier;
        fallback.score = Math.Max(fallback.score, forfeit.score + 1);
        fallback.reasons.Insert(0, "Small legal move avoids forfeiting the remaining phase after actor selection; no stronger continuation was identified");
        evaluations.Sort(CompareEvaluations);
    }

    private static void ApplyAttackingHighPassSupport(
        RoomAIDecisionSnapshot snapshot, List<RoomAITacticalEvaluation> evaluations)
    {
        var hp = MatchManager.Instance?.highPassManager;
        var actor = hp?.selectedToken;
        var landing = hp?.currentTargetHex;
        if (snapshot?.currentState != "HighPass" || hp == null || !hp.isWaitingForAttackerSelection
            || !hp.isWaitingForAttackerMove || hp.lockedAttacker == null || actor == null
            || actor.IsGoalKeeper || landing == null || actor.GetCurrentHex() == null
            || HexGridUtils.GetHexStepDistance(actor.GetCurrentHex(), landing) <= 6) return;
        bool leftToRight = actor.isHomeTeam ? snapshot.homeTeamDirection == "LeftToRight"
            : snapshot.awayTeamDirection == "LeftToRight";
        var opponents = snapshot.tokens.Where(t => t != null && t.isPlaying && !t.isSentOff
            && t.currentHex != null && t.teamSide != snapshot.expectedTeam).ToList();
        var columns = opponents.Select(t => t.currentHex.x).OrderBy(x => x).ToList();
        if (columns.Count < 2) return;
        int line = leftToRight ? Math.Max(0, Math.Max(landing.coordinates.x, columns[columns.Count - 2]))
            : Math.Min(0, Math.Min(landing.coordinates.x, columns[1]));
        var candidates = evaluations.Where(e => e?.candidate?.manager == nameof(HighPassManager)
                && e.candidate.actionType == RoomActionType.SetupMove && e.candidate.step == RoomDecisionStep.ChooseTarget
                && e.candidate.actor == actor && e.candidate.targetHex != null && e.safetyPriority == 0
                && HexGridUtils.GetHexStepDistance(e.candidate.targetHex, landing)
                    < HexGridUtils.GetHexStepDistance(actor.GetCurrentHex(), landing)
                && (leftToRight ? e.candidate.targetHex.coordinates.x <= line : e.candidate.targetHex.coordinates.x >= line))
            .ToList();
        RoomAITacticalEvaluation support = null;
        HexCell preparedLanding = null;
        int bestCost = int.MaxValue;
        foreach (var candidate in candidates)
        {
            var destination = candidate.candidate.targetHex;
            var vacated = new HashSet<HexCell> { actor.GetCurrentHex() };
            var reach = HexGridUtils.GetReachableHexes(hp.hexGrid, destination, actor.pace, vacated).Item2;
            var delivery = reach.Where(pair => !pair.Key.isOutOfBounds
                    && (!pair.Key.isAttackOccupied && !pair.Key.isDefenseOccupied || pair.Key == actor.GetCurrentHex())
                    && HexGridUtils.GetHexStepDistance(pair.Key, landing) <= 6
                    && !opponents.Any(t => RoomAITacticalSafety.Distance(t.currentHex,
                        RoomHexCoordinates.FromHex(pair.Key)) == 1))
                .OrderBy(pair => pair.Value.distance).FirstOrDefault();
            if (delivery.Key == null || delivery.Value.distance > bestCost
                || delivery.Value.distance == bestCost && support != null && candidate.score <= support.score) continue;
            support = candidate; preparedLanding = delivery.Key; bestCost = delivery.Value.distance;
        }
        if (support == null) return;
        support.tier = RoomAITacticalTier.SafeProgression;
        support.score += 120;
        support.reasons.Insert(0, $"Prepares a headed-pass landing at ({preparedLanding.coordinates.x}, {preparedLanding.coordinates.z}) "
            + $"with {bestCost} collection step(s); reassess offside and interception after the defender's free move");
        evaluations.Sort(CompareEvaluations);
    }

    private static RoomAITacticalEvaluation SelectCandidate(
        List<RoomAITacticalEvaluation> rankedCandidates,
        AIManager.CodexTacticianDifficulty difficulty)
    {
        if (rankedCandidates == null || rankedCandidates.Count == 0)
        {
            return null;
        }

        if (difficulty == AIManager.CodexTacticianDifficulty.Hard
            || difficulty == AIManager.CodexTacticianDifficulty.UltraHard)
        {
            return rankedCandidates[0];
        }

        int maximumPoolSize = difficulty == AIManager.CodexTacticianDifficulty.Medium ? 4 : 8;
        int safeCount = rankedCandidates.TakeWhile(item => item.safetyPriority == rankedCandidates[0].safetyPriority
            && item.kickoffFrontLinePriority == rankedCandidates[0].kickoffFrontLinePriority).Count();
        int poolSize = Math.Min(safeCount, maximumPoolSize);
        int nonRiskyCount = 0;
        while (nonRiskyCount < poolSize && rankedCandidates[nonRiskyCount].tier != RoomAITacticalTier.Risky)
        {
            nonRiskyCount++;
        }

        if (nonRiskyCount > 0)
        {
            poolSize = nonRiskyCount;
        }

        if (difficulty == AIManager.CodexTacticianDifficulty.Medium && UnityEngine.Random.value < 0.65f)
        {
            return rankedCandidates[0];
        }

        int totalWeight = poolSize * (poolSize + 1) / 2;
        int roll = UnityEngine.Random.Range(0, totalWeight);
        for (int index = 0; index < poolSize; index++)
        {
            int weight = poolSize - index;
            if (roll < weight)
            {
                return rankedCandidates[index];
            }

            roll -= weight;
        }

        return rankedCandidates[0];
    }

    private static int CompareEvaluations(RoomAITacticalEvaluation left, RoomAITacticalEvaluation right)
    {
        int goalkeeperSafety = RoomAITacticalSafety.CompareGoalkeeperSafety(left, right);
        if (goalkeeperSafety != 0) return goalkeeperSafety;
        int kickoffFrontLine = left.kickoffFrontLinePriority.CompareTo(right.kickoffFrontLinePriority);
        if (kickoffFrontLine != 0) return kickoffFrontLine;
        int tierComparison = left.tier.CompareTo(right.tier);
        if (tierComparison != 0)
        {
            return tierComparison;
        }

        int scoreComparison = right.score.CompareTo(left.score);
        return scoreComparison != 0
            ? scoreComparison
            : left.originalIndex.CompareTo(right.originalIndex);
    }

    private static void ApplyTacticalSafetyConstraints(RoomAIDecisionSnapshot snapshot, List<RoomAITacticalEvaluation> evaluations)
    {
        RoomAITacticalSafety.AssignSafetyPriorities(evaluations);
        RoomAITacticalSafety.ConstrainGoalkeeperChoices(snapshot, evaluations);
        RoomAITacticalSafety.ProtectPossession(evaluations);
        bool hasAerialImprovement = evaluations.Any(e => e.explanation?.isCommittedAerialDefense == true
            && e.explanation.committedAerialDefenseGain > .001f);
        foreach (var evaluation in evaluations)
        {
            var facts = evaluation.explanation;
            if (facts == null) continue;
            if (facts.isCommittedAerialDefense)
            {
                evaluation.score = (int)Math.Round(facts.committedAerialDefenseGain * 5000f) - 12;
                evaluation.tier = facts.committedAerialDefenseGain > .001f
                    ? RoomAITacticalTier.SafeProgression : RoomAITacticalTier.Risky;
                evaluation.reasons.Clear();
                evaluation.reasons.Add($"Committed HP landing/header/relay danger reduction={facts.committedAerialDefenseGain:0.000}; "
                    + "approaching the kicker cannot revoke this pass or create an ordinary tackle");
                if (!hasAerialImprovement && facts.committedAerialDefenseGain >= -.0001f
                    && facts.committedAerialRecoverySteps > 0)
                {
                    evaluation.score = facts.committedAerialRecoverySteps * 10;
                    evaluation.tier = RoomAITacticalTier.SafeProgression;
                    evaluation.reasons.Add("No landing intervention improves the chance: recover toward goal without worsening the danger");
                }
            }
            if (snapshot.expectedTeam != snapshot.teamInAttack && !facts.isCommittedAerialDefense
                && evaluation.candidate.actionType is (RoomActionType.StartMovement or RoomActionType.FinalThird or RoomActionType.SetupMove))
                RoomAITacticalSafety.EvaluateDefensiveLine(snapshot, facts);
            if (facts.actorToken?.isGoalKeeper == true
                && evaluation.candidate.actionType is RoomActionType.StartMovement or RoomActionType.FinalThird)
            {
                bool carriesBall = snapshot.attackHasPossession && snapshot.expectedTeam == snapshot.teamInAttack
                    && RoomAITacticalSafety.Distance(facts.actorHex, snapshot.ball) == 0;
                float defensiveGain = facts.goalkeeperPositioningCurrentMaximumExpectedGoals >= 0f
                    && facts.goalkeeperPositioningMaximumExpectedGoals >= 0f
                    ? facts.goalkeeperPositioningCurrentMaximumExpectedGoals - facts.goalkeeperPositioningMaximumExpectedGoals : 0f;
                if (carriesBall && facts.opponentsWithinTackleReach > 0)
                {
                    evaluation.score -= 700 * facts.opponentsWithinTackleReach;
                    evaluation.safetyPriority = Math.Max(1, evaluation.safetyPriority);
                    evaluation.reasons.Insert(0, "Keeper possession remains inside an opponent's tackle reach; prefer a safe distribution or escape");
                }
                else if (!carriesBall && snapshot.attackHasPossession && defensiveGain < .10f
                    && !RoomAITacticalSafety.IsGoalkeeperRecovery(snapshot, facts))
                {
                    evaluation.score -= 800;
                    evaluation.safetyPriority = Math.Max(2, evaluation.safetyPriority);
                    evaluation.reasons.Insert(0, "Optional keeper move has no demonstrated defensive benefit; preserve goal coverage");
                }
            }
            if (facts.isAttackingOffBallMove && facts.supportsConcreteContinuation && !string.IsNullOrWhiteSpace(facts.attackingContinuation))
            {
                // Moving a support player does not earn possession again. Rank
                // the change from their existing outlet/coverage, not the entire
                // hypothetical pass as if it had already happened.
                evaluation.score = (int)Math.Round(facts.attackingContinuationValue * 120f) - 12;
                evaluation.reasons.Clear();
                evaluation.reasons.Insert(0, facts.attackingContinuation);
                evaluation.reasons.Insert(1, $"Marginal support value={evaluation.score}; phase move cost included");
                if (evaluation.score <= 0)
                {
                    evaluation.tier = RoomAITacticalTier.Risky;
                    evaluation.safetyPriority = Math.Max(1, evaluation.safetyPriority);
                    evaluation.reasons.Add("Marginal support gain does not repay the phase move; preserve the opportunity instead of clustering");
                }
            }
            if (facts.isAttackingOffBallMove && !facts.supportsConcreteContinuation)
            {
                evaluation.safetyPriority = System.Math.Max(1, evaluation.safetyPriority);
                evaluation.tier = RoomAITacticalTier.Risky;
                evaluation.reasons.Insert(0, "Off-ball move has no improved passing, aerial, finishing or recovery continuation");
            }
            bool isDistribution = evaluation.candidate.actionType is RoomActionType.GroundPass
                or RoomActionType.QuickThrow or RoomActionType.HighPass or RoomActionType.GoalkeeperKick or RoomActionType.Header;
            if (facts.passRace != null && (isDistribution && facts.passRace.opponentCollectsFirst
                || (snapshot.currentState == "QuickThrow" || evaluation.candidate.actionType == RoomActionType.QuickThrow)
                    && facts.passRace.safeContinuationHexes == 0 && facts.passRace.routeExpectedGoals <= 0f))
            {
                evaluation.safetyPriority = System.Math.Max(1, evaluation.safetyPriority);
                evaluation.tier = RoomAITacticalTier.Risky;
                evaluation.reasons.Insert(0, "Distribution race has no safe continuation before opponent recovery");
            }
            bool openingForwardPlacement = IsEarlyDefendingKickoffSetup(snapshot)
                && evaluation.candidate.manager == nameof(KickoffManager)
                && evaluation.candidate.actionType == RoomActionType.SetupMove
                && facts.actorToken is { isGoalKeeper: false, jerseyNumber: 7 or 9 or 10 or 11 };
            // Opening setup can reposition the whole defensive unit. A forward
            // starting behind the backs must still reach the opening press line;
            // the live ball's temporary anchor-gap penalty would strand them.
            if (!openingForwardPlacement && (facts.defensiveLineLeavesFullback || facts.defensiveLineCreatesGap))
            {
                evaluation.safetyPriority = System.Math.Max(1, evaluation.safetyPriority);
                evaluation.reasons.Insert(0, "Preserve defensive cover: this advance isolates the remaining anchor and opens a recovery gap");
            }
            if (snapshot.expectedTeam != snapshot.teamInAttack && facts.defensivePlanRole == "DefensiveLine"
                && (evaluation.candidate.step == RoomDecisionStep.ChooseTarget && !MatchesCoordinates(facts.targetHex, facts.defensivePlanDestination)
                    || evaluation.candidate.step == RoomDecisionStep.ChooseActor && MatchesCoordinates(facts.actorHex, facts.defensivePlanDestination)))
            {
                evaluation.safetyPriority = System.Math.Max(1, evaluation.safetyPriority);
                evaluation.reasons.Insert(0, "Preserves the assigned centre-back's line responsibility");
            }
            if (snapshot.expectedTeam != snapshot.teamInAttack && snapshot.attackHasPossession
                && snapshot.defensivePhasePlan != null && evaluation.candidate.actionType == RoomActionType.StartMovement
                && (facts.defensivePlanRole == "HoldShape"
                    || evaluation.candidate.step == RoomDecisionStep.ChooseActor
                        && facts.defensivePlanRole != "PrimaryPressure"
                        && facts.defensivePlanDestination != null
                        && MatchesCoordinates(facts.actorHex, facts.defensivePlanDestination)
                    || evaluation.candidate.step == RoomDecisionStep.ChooseTarget && facts.defensivePlanDestination != null
                        && !MatchesCoordinates(facts.targetHex, facts.defensivePlanDestination)
                        && !(facts.doubleTeamEvaluated && facts.doubleTeamExtraTurnoverValue > facts.doubleTeamExposureValue)))
            {
                evaluation.safetyPriority = System.Math.Max(1, evaluation.safetyPriority);
                evaluation.reasons.Insert(0, "Defensive unit preserves its assigned coverage instead of adding another ball chase");
                if (evaluation.candidate.step == RoomDecisionStep.ChooseActor
                    && MatchesCoordinates(facts.actorHex, facts.defensivePlanDestination))
                    evaluation.reasons.Insert(0, "Assigned coverage is already satisfied at the actor's current hex; no movement is needed");
            }
            if (snapshot.expectedTeam != snapshot.teamInAttack && snapshot.attackHasPossession
                && snapshot.defensivePhasePlan != null && facts.defensivePlanRole != "PrimaryPressure"
                && facts.defensiveMoveEndsAdjacentToBall && snapshot.tokens.Any(token => token.isPlaying
                    && token.teamSide == snapshot.expectedTeam && token.tokenKey != facts.actorTokenKey
                    && RoomAITacticalSafety.Distance(token.currentHex, snapshot.ball) == 1)
                && (!facts.doubleTeamEvaluated || facts.doubleTeamExtraTurnoverValue <= facts.doubleTeamExposureValue))
            {
                // Require positive evidence for a second presser. Without a verified
                // turnover advantage, protecting the assigned outlet has priority.
                evaluation.safetyPriority = System.Math.Max(1, evaluation.safetyPriority);
                evaluation.reasons.Insert(0, $"Single-presser assignment: extra turnover value={facts.doubleTeamExtraTurnoverValue:0.000}, exposed threat={facts.doubleTeamExposureValue:0.000}; protect the assigned outlet");
            }
        }
        evaluations.Sort(CompareEvaluations);
    }

    private static void ApplyAerialReleaseContinuation(RoomAIAerialReleasePlan plan, RoomAIDecisionSnapshot snapshot,
        List<RoomAITacticalEvaluation> evaluations)
    {
        if (plan == null || plan.sequenceRetention <= 0f) return;
        foreach (var item in evaluations)
        {
            var facts = item.explanation;
            if (facts == null) continue;
            RoomHexCoordinates goal = facts.actorTokenKey == plan.runnerTokenKey ? plan.runnerHex
                : facts.actorTokenKey == plan.headerTokenKey ? plan.contestHex : null;
            if (facts.isAttackingOffBallMove && goal != null && facts.actorHex != null && facts.targetHex != null)
            {
                int gain = RoomAITacticalSafety.Distance(facts.actorHex, goal) - RoomAITacticalSafety.Distance(facts.targetHex, goal);
                if (gain <= 0 || facts.opponentsWithinTackleReach > 0 && facts.actorTokenKey == plan.headerTokenKey) continue;
                var grid = MatchManager.Instance?.hexGrid;
                var actor = item.candidate.actor;
                var at = grid?.GetHexCellAt(new Vector3Int(facts.targetHex.x, 0, facts.targetHex.z), false);
                if (grid == null || actor?.GetCurrentHex() == null || at == null) continue;
                var vacated = new HashSet<HexCell> { actor.GetCurrentHex() };
                int futureRange = facts.actorTokenKey == plan.headerTokenKey ? 3 : actor.pace;
                var contest = grid.GetHexCellAt(new Vector3Int(goal.x, 0, goal.z), false);
                var locked = contest?.GetOccupyingToken();
                bool attackerOnLanding = contest == at || locked != null && locked != actor && locked.isHomeTeam == actor.isHomeTeam;
                // If the initial HP target is empty, the free move must reach
                // it; the two-hex nomination radius only applies when another
                // attacker is already locked on the landing.
                int contestRange = facts.actorTokenKey == plan.headerTokenKey && attackerOnLanding
                    ? HeaderManager.HEADER_SELECTION_RANGE : 0;
                if (RoomAITacticalSafety.Distance(facts.targetHex, goal) > futureRange + contestRange
                    || !RoomAIDecisionSnapshotBuilder.GetCachedReachability(snapshot, grid, actor.GetCurrentHex(), actor.pace)
                        .reachableHexes.Contains(at)) continue;
                bool reachesContinuation = HexGridUtils.GetReachableHexes(grid, at, futureRange, vacated).reachableHexes
                    .Any(hex => hex != null && !hex.isOutOfBounds
                        && (!hex.isAttackOccupied && !hex.isDefenseOccupied || hex == actor.GetCurrentHex())
                        && RoomAITacticalSafety.Distance(RoomHexCoordinates.FromHex(hex), goal) <= contestRange);
                if (!reachesContinuation) continue;
                facts.supportsConcreteContinuation = true;
                facts.attackingContinuation = $"Aerial release: {plan.headerTokenKey} Heading {plan.headerHeading} contests at ({plan.contestHex.x}, {plan.contestHex.z}); headed pass to {plan.runnerTokenKey} Pace {plan.runnerPace}, reassess offside at the headed action";
                facts.attackingContinuationValue = plan.sequenceRetention * gain;
                item.score += (int)Math.Round(120f * facts.attackingContinuationValue);
                item.reasons.Insert(0, facts.attackingContinuation);
            }
            if (item.candidate.actionType is RoomActionType.HighPass or RoomActionType.GoalkeeperKick && facts.highPassBestCollectorTokenKey == plan.headerTokenKey
                && RoomAITacticalSafety.Distance(facts.highPassBestTargetHex, plan.contestHex) <= 1
                && plan.runnerMovesRequired == 0)
            {
                item.score += (int)Math.Round(plan.sequenceRetention * 300f);
                item.reasons.Insert(0, "High Pass begins the prepared headed-release sequence; offside will be checked again at the header");
            }
            if (facts.isHeadedPassTarget && !facts.headedPassTargetIsOffside
                && (facts.targetTokenKey == plan.runnerTokenKey || facts.headedPassSpaceCollectorTokenKey == plan.runnerTokenKey))
            {
                float continuation = System.Math.Max(facts.headedPassImmediateSnapshotExpectedGoals, facts.headedPassSpaceRouteExpectedGoals);
                if (continuation > 0f) item.score += (int)Math.Round(plan.sequenceRetention * continuation * 300f);
                item.reasons.Insert(0, "Prepared aerial release reaches the fast runner after current offside reassessment");
            }
        }
        evaluations.Sort(CompareEvaluations);
    }

    private static string BuildReasonSummary(RoomAITacticalEvaluation evaluation)
    {
        if (evaluation == null)
        {
            return "No candidate was selected.";
        }

        int reasonCount = Math.Min(3, evaluation.reasons.Count);
        string reasons = reasonCount > 0
            ? string.Join("; ", evaluation.reasons.GetRange(0, reasonCount))
            : "highest-ranked legal action";
        return $"tier={evaluation.tier}, score={evaluation.score}: {reasons}";
    }
}
