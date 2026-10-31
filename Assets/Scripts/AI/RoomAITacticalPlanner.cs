using System;
using System.Collections.Generic;
using System.Linq;

public enum RoomAIAttackPlanType
{
    None,
    FinishChance,
    CreateShot,
    WideAerialAttack,
    SafeDribble,
    ProgressPossession
}

public enum RoomAITacticalTier
{
    ImmediateFinish = 0,
    CreateFinish = 1,
    SafeProgression = 2,
    RetainPossession = 3,
    Neutral = 4,
    Risky = 5
}

[Serializable]
public sealed class RoomAIAttackPlan
{
    public RoomAIAttackPlanType planType;
    public string team;
    public string createdInState;
    public string rationale;
    public int revision;
    public RoomAIAerialReleasePlan aerialReleasePlan;
    public RoomAIRosterAttackPlan rosterAttackPlan;
}

public sealed class RoomAITacticalEvaluation
{
    public int safetyPriority;
    public int kickoffFrontLinePriority;
    public RoomActionCandidate candidate;
    public RoomAICandidateExplanation explanation;
    public RoomAITacticalTier tier;
    public int score;
    public int originalIndex;
    public readonly List<string> reasons = new();
}

[Serializable]
public sealed class RoomAITacticalWeights
{
    public int strongAttributeThreshold = 5;
    public int shotAction = 120;
    public int firstTimePassAction = 35;
    public int firstTimePassExtraPaceUnlock = 115;
    public int firstTimePassShotRouteExpectedGoalsScale = 800;
    public int firstTimePassRetentionScale = 70;
    public int highPassAction = 25;
    public int groundPassAction = 22;
    public int movementAction = 20;
    public int longBallAction = 12;
    public int committedContinuation = 10;
    public int forfeit = -200;
    public int forwardProgressPerColumn = 6;
    public int retreatPerColumn = -3;
    public int shootingPosition = 45;
    public int headingPosition = 30;
    public int walkBallIntoGoal = 0;
    public int thirdGoalkeeperWallHex = 24;
    public int safeFromNextMovementTackle = 20;
    public int opponentInTackleReach = -18;
    public int supportPerPlayer = 6;
    public int maximumSupportScore = 18;
    public int shootingAttribute = 5;
    public int expectedGoalsScale = 200;
    public int secondBookingProbabilityScale = 55;
    public int cornerHeaderAnchor = 28;
    public int cornerFinalCongestionPerHeader = 42;
    public int cornerAdjacentEliteMark = 38;
    public int cornerTwoHexCover = 10;
    public int setPieceHighPassFinishCorrection = 95;
    public int highPassAttribute = 4;
    public int headingAttribute = 5;
    public int paceAttribute = 2;
    public int dribblingAttribute = 3;
    public int receiverDribblingAttribute = 2;
    public int finishPlan = 80;
    public int wideAerialActionPlan = 35;
    public int wideAerialPositionPlan = 28;
    public int safeDribblePlan = 25;
    public int createShotPlan = 35;
    public int progressPossessionPlan = 18;
    [UnityEngine.Header("Codex Tactician - Possession Transitions")]
    public int looseBallCollect = 650;
    public int looseBallTargetBase = 500;
    public int looseBallTargetDistancePenalty = 50;
    public int looseBallProgressPerHex = 35;
    public int looseBallActorBase = 350;
    public int looseBallActorDistancePenalty = 30;
    public int unsupportedPassIntoSpace = -260;
    public int kickoffUnsupportedPassIntoSpace = -140;
    [UnityEngine.Header("Codex Tactician - Defensive Movement")]
    public int defensiveActorBase = 180;
    public int defensiveActorBallDistancePenalty = 18;
    public int defensivePressBase = 220;
    public int defensiveBallDistancePenalty = 28;
    public int defensiveMarkBase = 160;
    public int defensiveMarkDistancePenalty = 35;
    public int defensiveCrowdingFreeTeammates = 2;
    public int defensiveCrowdingPenaltyPerExtraTeammate = 24;
    public int defensivePlanActorRole = 180;
    public int defensivePlanExactDestination = 320;
    public int defensivePlanAdjacentPressure = 180;
    public int defensivePlanStopsShortPenalty = 260;
    [UnityEngine.Header("Codex Tactician - Action Comparison")]
    public int highPassReliabilityScale = 60;
    public int groundPassInterceptionPenalty = 45;
    public int groundPassRouteExpectedGoalsScale = 700;
    public int groundPassRetentionScale = 80;
    public int groundPassProvenCollector = 45;
    public int groundPassRemainingPacePerHex = 6;
    public int groundPassReachableShot = 85;
    public int groundPassDefensiveChallengePenalty = 24;
    public int groundPassImmediateReceiverChallengePenalty = 10;
    public int groundPassPrePickupChallengePenalty = 55;
    public int groundPassForcedLooseBallTurnoverPenalty = 700;
    public int safeTwoPhaseSpacePlan = 145;
    public int secondMovementShotPlan = 70;
    public int selfPassTackleEscape = 95;
    public int supportingPassLane = 55;
    public int reachableShootingPosition = 55;
    public int goalkeeperWallCoveragePerQuality = 35;
    public int goalkeeperUncoveredShotPathPenalty = 50;
    public int goalkeeperExpectedGoalsImprovementScale = 420;
    public int goalkeeperOwnGoalDistancePenalty = 70;
    public int goalkeeperRushDespiteOutfieldCoverPenalty = 120;
    public int longBallGoalkeeperClaim = 900;
    public int longBallGoalkeeperRecoverySupport = 260;
    public int longBallGoalkeeperSupportDistancePenalty = 45;
    public int longBallGoalkeeperDiveAtFeet = 240;
    public int longBallGoalkeeperFarPostSafety = 220;
    public int kickoffSecureGroundPass = 160;
    public int kickoffSpeculativeHighPass = -180;
    public int desperateKickoffDribblerRush = 300;
    public int desperateKickoffRushTurnoverRisk = 170;
    public int kickoffSetupConfirmBase = 140;
    public int kickoffSetupPenaltyScale = 1;
    public int kickoffSetupImprovementScale = 3;
    public int kickoffSetupActorUrgencyScale = 4;
    public int kickoffSetupAdequatePenalty = 80;
    public int kickoffSetupExceptionalImprovement = 20;
    public int attackingGoalkeeperOptionalMovePenalty = 220;
    public int optionalGoalkeeperMovementPenalty = 260;
    public int movementForfeitScoreMargin = 15;
    public int repeatedNoProgressMovementPenalty = 1200;
    [UnityEngine.Header("Codex Tactician - Tackle Risk")]
    public int tacklePossessionWinValue = 115;
    public int tackleThreatDenialScale = 240;
    public int tackleFoulCost = 85;
    public int tackleSendOffCost = 520;
    public int failedTackleRepositionXgCost = 260;
    public int failedTackleProgressCostPerColumn = 12;
    public int standGroundBaseValue = 22;
    public int successfulTackleNextActionScale = 1;
}

/// <summary>
/// Ranks actions which the gameplay managers have already declared legal.
/// This is intentionally a tactical evaluator, not a second rules engine.
/// Future forward-search and macro-action planners should consume the same
/// snapshot/evaluation boundary rather than invoking Unity input directly.
/// </summary>
public static class RoomAITacticalPlanner
{
    public static RoomAIAttackPlan UpdateAttackPlan(
        RoomAIDecisionSnapshot snapshot,
        RoomAIAttackPlan currentPlan,
        RoomAITacticalWeights weights)
    {
        weights ??= new RoomAITacticalWeights();
        if (!IsExpectedTeamAttacking(snapshot))
        {
            return new RoomAIAttackPlan
            {
                planType = RoomAIAttackPlanType.None,
                team = snapshot?.expectedTeam ?? string.Empty,
                createdInState = snapshot?.currentState ?? string.Empty,
                rationale = "Expected team is not the side attacking with possession.",
                revision = (currentPlan?.revision ?? 0) + 1
            };
        }

        var roster = RoomAIRosterAttackPlan.Build(snapshot);
        RoomAIAttackPlanType selectedPlan = SelectAttackPlanType(snapshot, weights);
        if (currentPlan != null
            && currentPlan.planType == selectedPlan
            && string.Equals(currentPlan.team, snapshot.expectedTeam, StringComparison.OrdinalIgnoreCase))
        {
            if (currentPlan.rosterAttackPlan?.rosterIdentity != roster?.rosterIdentity) currentPlan.revision++;
            currentPlan.rosterAttackPlan = roster;
            currentPlan.aerialReleasePlan = roster?.aerial;
            currentPlan.rationale = DescribePlanRationale(selectedPlan) + " " + roster?.rationale;
            return currentPlan;
        }

        return new RoomAIAttackPlan
        {
            planType = selectedPlan,
            team = snapshot.expectedTeam,
            createdInState = snapshot.currentState,
            rationale = DescribePlanRationale(selectedPlan) + " " + roster?.rationale,
            rosterAttackPlan = roster,
            aerialReleasePlan = roster?.aerial,
            revision = (currentPlan?.revision ?? 0) + 1
        };
    }

    public static List<RoomAITacticalEvaluation> RankCandidates(
        RoomAIDecisionSnapshot snapshot,
        IReadOnlyList<RoomActionCandidate> candidates,
        RoomAIAttackPlan plan,
        RoomAITacticalWeights weights)
    {
        weights ??= new RoomAITacticalWeights();
        List<RoomAITacticalEvaluation> evaluations = new();
        if (candidates == null)
        {
            return evaluations;
        }

        for (int index = 0; index < candidates.Count; index++)
        {
            RoomActionCandidate candidate = candidates[index];
            if (candidate == null)
            {
                continue;
            }

            RoomAICandidateExplanation explanation = FindExplanation(snapshot, candidate);
            evaluations.Add(EvaluateCandidate(snapshot, candidate, explanation, plan, weights, index));
        }

        RoomAITacticalSafety.AssignSafetyPriorities(evaluations);
        evaluations.Sort(CompareEvaluations);
        return evaluations;
    }

    private static RoomAIAttackPlanType SelectAttackPlanType(
        RoomAIDecisionSnapshot snapshot,
        RoomAITacticalWeights weights)
    {
        if (HasLegalAction(snapshot, RoomActionType.Shot))
        {
            return RoomAIAttackPlanType.FinishChance;
        }

        if (HasImmediateShotCreationRoute(snapshot))
        {
            return RoomAIAttackPlanType.CreateShot;
        }

        RoomAIMatchupTacticalProfile matchup = snapshot.tacticalMatchup
            ?? RoomAITacticalProfileBuilder.Build(snapshot);
        bool aerialIsPrimary = matchup?.suggestedAttackingIdentity == RoomAIAttackingIdentity.AerialPrimary;
        bool aerialHasMatchupAdvantage = matchup?.hasHeadingAdvantageAgainstCentralDefenders == true;
        if (HasLegalAction(snapshot, RoomActionType.HighPass)
            && (aerialIsPrimary || aerialHasMatchupAdvantage))
        {
            return RoomAIAttackPlanType.WideAerialAttack;
        }

        if (HasLegalAction(snapshot, RoomActionType.StartMovement)
            && HasTeamPlayerWithAttribute(
                snapshot,
                token => !token.isGoalKeeper && !token.isSentOff && token.currentHex != null
                    && token.dribbling >= weights.strongAttributeThreshold))
        {
            return RoomAIAttackPlanType.SafeDribble;
        }

        return HasLegalAction(snapshot, RoomActionType.StartMovement)
            ? RoomAIAttackPlanType.CreateShot
            : RoomAIAttackPlanType.ProgressPossession;
    }

    private static RoomAITacticalEvaluation EvaluateCandidate(
        RoomAIDecisionSnapshot snapshot,
        RoomActionCandidate candidate,
        RoomAICandidateExplanation explanation,
        RoomAIAttackPlan plan,
        RoomAITacticalWeights weights,
        int originalIndex)
    {
        RoomAITacticalEvaluation evaluation = new()
        {
            candidate = candidate,
            explanation = explanation,
            originalIndex = originalIndex,
            tier = RoomAITacticalTier.Neutral
        };

        ApplyActionValue(evaluation, weights);
        ApplyPitchValue(snapshot, evaluation, weights);
        ApplyPlayerValue(evaluation, weights);
        ApplyPlanValue(evaluation, plan, weights);
        if (explanation?.goalkeeperActorDestinationPreviewed == true
            && explanation.goalkeeperActorBestContinuationIsForfeit)
        {
            AddScore(
                evaluation,
                -1000000,
                "best goalkeeper destination is to stay put; forfeit the phase before selecting the goalkeeper");
        }
        ResolveTier(evaluation);
        return evaluation;
    }

    private static void ApplyActionValue(
        RoomAITacticalEvaluation evaluation,
        RoomAITacticalWeights weights)
    {
        RoomActionCandidate candidate = evaluation.candidate;
        bool usePossessionContinuationValue = evaluation.explanation?.hasPossessionLevelPlan == true;
        switch (candidate.actionType)
        {
            case RoomActionType.Shot:
                AddScore(evaluation, weights.shotAction, "legal shot is available");
                break;
            case RoomActionType.ContinueWithoutShot when evaluation.explanation?.isSnapshotDecision == true:
                AddScore(evaluation, weights.shotAction, "continues toward a full shot after the Snapshot decision");
                break;
            case RoomActionType.FirstTimePass when !usePossessionContinuationValue:
                AddScore(evaluation, weights.firstTimePassAction, "first-time pass can create an immediate continuation");
                break;
            case RoomActionType.HighPass when !usePossessionContinuationValue:
            case RoomActionType.GoalkeeperKick when !usePossessionContinuationValue:
                AddScore(evaluation, weights.highPassAction, "high pass can create an aerial finish");
                break;
            case RoomActionType.GroundPass when !usePossessionContinuationValue:
                AddScore(evaluation, weights.groundPassAction, "ground pass progresses the possession structure");
                break;
            case RoomActionType.StartMovement when !usePossessionContinuationValue:
                AddScore(evaluation, weights.movementAction, "movement can improve several players before the next action");
                break;
            case RoomActionType.LongBall when !usePossessionContinuationValue:
                AddScore(evaluation, weights.longBallAction, "long ball offers direct progression with added turnover risk");
                break;
            case RoomActionType.Confirm:
            case RoomActionType.Roll:
                AddScore(evaluation, weights.committedContinuation, "continues the already committed action");
                break;
        }

        if (candidate.isForfeit
            && !(candidate.actionType == RoomActionType.ContinueWithoutShot
                && evaluation.explanation?.isSnapshotDecision == true)
            && !IsNeutralTacticalForfeit(candidate, evaluation.explanation))
        {
            AddScore(evaluation, weights.forfeit, "forfeits the current opportunity");
        }
        else if (candidate.isForfeit && IsNeutralTacticalForfeit(candidate, evaluation.explanation))
        {
            evaluation.reasons.Add("keeps the current shape instead of forcing an optional move");
        }
    }

    private static void ApplyPitchValue(
        RoomAIDecisionSnapshot snapshot,
        RoomAITacticalEvaluation evaluation,
        RoomAITacticalWeights weights)
    {
        RoomAICandidateExplanation explanation = evaluation.explanation;
        if (explanation == null)
        {
            evaluation.reasons.Add("no positional explanation is available");
            return;
        }

        if (explanation.isTackleDecision)
        {
            ApplyTackleDecisionValue(evaluation, weights);
            return;
        }

        ApplyDefensivePhasePlanValue(evaluation, weights);

        if (explanation.isPenaltyFoulDecision)
        {
            AddScore(
                evaluation,
                (int)Math.Round(explanation.penaltyExpectedGoals * weights.expectedGoalsScale),
                $"accepts a penalty with xG={explanation.penaltyExpectedGoals:0.000} "
                + "(full power, no blockers, goalkeeper saving penalty -2)");
            return;
        }

        if (explanation.isLateResultManagementDecision
            && explanation.lateResultScoreDifference > 0)
        {
            int urgency = System.Math.Max(1, 4 - explanation.lateResultActionsRemaining);
            if (evaluation.candidate.actionType == RoomActionType.LongBall)
            {
                AddScore(
                    evaluation,
                    70 * urgency,
                    $"protects a {explanation.lateResultScoreDifference}-goal lead by clearing the own half "
                    + $"with {explanation.lateResultActionsRemaining} stoppage action(s) remaining");
            }
            else if (evaluation.candidate.actionType == RoomActionType.QuickThrow)
            {
                AddScore(
                    evaluation,
                    -50 * urgency,
                    "quick restart exposes another turnover sequence while protecting a lead");
            }
            else
            {
                AddScore(
                    evaluation,
                    -30 * urgency,
                    "short continuation risks returning possession near goal while protecting a lead");
            }
        }
        else if (explanation.isLateResultManagementDecision
            && explanation.lateResultScoreDifference < 0)
        {
            evaluation.reasons.Add(
                $"team trails by {-explanation.lateResultScoreDifference}; do not apply result-protection clearance bias");
        }

        if (explanation.isSuccessfulTackleReposition
            && explanation.plannedNextActionValue >= 0f)
        {
            int continuationScore = (int)Math.Round(
                explanation.plannedNextActionValue * weights.successfulTackleNextActionScale);
            AddScore(
                evaluation,
                continuationScore,
                $"choose next action first: {explanation.plannedNextAction} to "
                + $"{FormatHex(explanation.plannedNextActionTargetHex)}"
                + (string.IsNullOrWhiteSpace(explanation.plannedNextReceiverTokenKey)
                    ? string.Empty
                    : $" for {explanation.plannedNextReceiverTokenKey}")
                + $"; value={explanation.plannedNextActionValue:0.0}, "
                + $"interceptors={explanation.plannedNextActionInterceptors}");
        }

        if (explanation.isSnapshotDefensiveMove)
        {
            if (explanation.snapshotDefenseMaximumExpectedGoals >= 0f)
            {
                int lexicographicValue = -(int)System.Math.Round(
                        explanation.snapshotDefenseMaximumExpectedGoals * 100000000f)
                    + (explanation.snapshotDefenseBlockerCount * 1000)
                    - (explanation.snapshotDefenseGoalkeeperPenalty * 100)
                    - (explanation.snapshotDefenseMovesGoalkeeper ? 20 : 0)
                    + System.Math.Max(-90, System.Math.Min(90, explanation.snapshotDefenseNextStateValue));
                AddScore(
                    evaluation,
                    lexicographicValue,
                    $"Snapshot desperation move leaves shooter max xG={explanation.snapshotDefenseMaximumExpectedGoals:0.000} "
                    + $"at {FormatHex(explanation.snapshotDefenseWorstTargetHex)}; best defender hex={FormatHex(explanation.snapshotDefenseBestMoveHex)}, "
                    + $"blockers={explanation.snapshotDefenseBlockerCount}, GK penalty={explanation.snapshotDefenseGoalkeeperPenalty}, "
                    + $"next-state={explanation.snapshotDefenseNextStateValue}");
            }

            return;
        }

        if (explanation.hasPossessionLevelPlan)
        {
            AddScore(
                evaluation,
                explanation.possessionPlanValue,
                $"possession plan: retention={explanation.possessionPlanRetentionProbability:0.000}, "
                + $"receiver survival={explanation.possessionPlanReceiverSurvivalProbability:0.000}, "
                + $"tackle loss={explanation.possessionPlanTackleLossProbability:0.000}, "
                + $"support recovery={explanation.possessionPlanRecoveryProbability:0.000}, "
                + $"effective retention={explanation.possessionPlanEffectiveRetentionProbability:0.000}, "
                + $"next threat xG={explanation.possessionPlanThreatExpectedGoals:0.000}, "
                + $"next={explanation.possessionPlanNextAction}"
                + (explanation.possessionPlanSupportBlocksTackleWinnerExit
                    ? $"; {explanation.possessionPlanSupportTokenKey} covers tackle winner "
                        + $"{explanation.possessionPlanLikelyTackleWinnerTokenKey}'s exit at "
                        + $"{FormatHex(explanation.possessionPlanSupportDestinationHex)}"
                    : string.Empty));
        }

        if (explanation.isRegularFinalThirdReorganization)
        {
            AddScore(
                evaluation,
                explanation.offsideLineRaise * 6,
                $"regular F3 raises second-defender offside line from x={explanation.currentSecondDefenderLineX} "
                + $"to x={explanation.proposedSecondDefenderLineX}");
            AddScore(
                evaluation,
                0,
                $"leaves {explanation.reachableOnsideOpponents} reachable opponent(s) onside");
            AddScore(evaluation, 0,
                $"Pass-time position only: {explanation.opponentsMadeOffside} beyond the ball/second-defender line; standing there is not an offence");
            if (explanation.defensiveLineLeavesFullback)
                AddScore(evaluation, -10000, "rejects advancing both centre-backs beyond the full-back holding the line");
            if (explanation.actorDefinesSecondDefenderLine)
            {
                AddScore(
                    evaluation,
                    30,
                    "moves the current second-deepest defender who defines the offside line");
            }
            else if (explanation.offsideLineRaise <= 0)
            {
                AddScore(
                    evaluation,
                    -30,
                    "regular F3 move neither raises the second-defender offside line nor moves its defining defender");
            }
            if (explanation.snapshotDefenseMovesGoalkeeper || explanation.actorToken?.isGoalKeeper == true)
            {
                AddScore(
                    evaluation,
                    -(80 + (System.Math.Max(0, explanation.goalkeeperOwnGoalDistanceDelta) * 40)),
                    $"regular F3 goalkeeper move is unnecessary and changes own-goal distance by {explanation.goalkeeperOwnGoalDistanceDelta}");
            }

            return;
        }

        if (explanation.isLongBallGoalkeeperRecovery)
        {
            ApplyLongBallGoalkeeperRecoveryValue(evaluation, weights);
            return;
        }

        if (explanation.isGoalkeeperPositioning)
        {
            if (explanation.goalkeeperPositioningMaximumExpectedGoals >= 0f)
            {
                AddScore(
                    evaluation,
                    -(int)System.Math.Round(
                        explanation.goalkeeperPositioningMaximumExpectedGoals * weights.expectedGoalsScale),
                    $"GK positioning limits the shooter's best Snapshot to xG="
                    + $"{explanation.goalkeeperPositioningMaximumExpectedGoals:0.000} at "
                    + $"{FormatHex(explanation.goalkeeperPositioningWorstTargetHex)} "
                    + $"({explanation.goalkeeperPositioningBlockerCount} blocker(s), "
                    + $"GK penalty {explanation.goalkeeperPositioningGoalkeeperPenalty})");
            }

            if (explanation.goalkeeperPositioningCurrentMaximumExpectedGoals >= 0f
                && explanation.goalkeeperPositioningMaximumExpectedGoals >= 0f)
            {
                float expectedGoalsImprovement = explanation.goalkeeperPositioningCurrentMaximumExpectedGoals
                    - explanation.goalkeeperPositioningMaximumExpectedGoals;
                AddScore(
                    evaluation,
                    (int)System.Math.Round(
                        expectedGoalsImprovement * weights.goalkeeperExpectedGoalsImprovementScale),
                    $"GK move changes worst live-threat xG from "
                    + $"{explanation.goalkeeperPositioningCurrentMaximumExpectedGoals:0.000} to "
                    + $"{explanation.goalkeeperPositioningMaximumExpectedGoals:0.000}");
            }

            if (explanation.goalkeeperOwnGoalDistanceDelta > 0)
            {
                AddScore(
                    evaluation,
                    -explanation.goalkeeperOwnGoalDistanceDelta * weights.goalkeeperOwnGoalDistancePenalty,
                    $"GK moves {explanation.goalkeeperOwnGoalDistanceDelta} hex(es) farther from own goal");
            }

            if (explanation.goalkeeperOwnGoalDistanceDelta > 0
                && explanation.goalkeeperPositioningCurrentOutfieldBlockerCount >= 2)
            {
                AddScore(
                    evaluation,
                    -weights.goalkeeperRushDespiteOutfieldCoverPenalty,
                    $"two or more outfield blockers already cover the current worst threat; preserve the goal and "
                    + $"{explanation.goalkeeperPositioningOffBallThreatCount} off-ball shooting threat(s)");
            }

            int uncoveredPaths = System.Math.Max(
                0,
                explanation.goalkeeperWallShotTargetCount - explanation.goalkeeperWallCoveredShotTargets);
            AddScore(
                evaluation,
                (explanation.goalkeeperWallCoverageQuality * weights.goalkeeperWallCoveragePerQuality)
                    - (uncoveredPaths * weights.goalkeeperUncoveredShotPathPenalty),
                $"GK wall covers {explanation.goalkeeperWallCoveredShotTargets}/{explanation.goalkeeperWallShotTargetCount} shooting paths "
                + $"with quality {explanation.goalkeeperWallCoverageQuality}");
            return;
        }

        if (explanation.bestReachableSnapshotExpectedGoals >= 0f)
        {
            AddScore(
                evaluation,
                (int)System.Math.Round(explanation.bestReachableSnapshotExpectedGoals * weights.expectedGoalsScale),
                $"dribble route xG={explanation.bestReachableSnapshotExpectedGoals:0.000} "
                + $"(raw={explanation.bestReachableSnapshotRawExpectedGoals:0.000}, "
                + $"reach={explanation.bestReachableSnapshotReachProbability:0.000}, "
                + $"turnover={explanation.bestReachableSnapshotTurnoverProbability:0.000}) "
                + $"at {FormatHex(explanation.bestReachableSnapshotHex)} across {explanation.reachableSnapshotHexesEvaluated} legal box hex(es)");
            if (explanation.immediateSnapshotExpectedGoals >= 0f)
            {
                AddScore(
                    evaluation,
                    (int)System.Math.Round(explanation.immediateSnapshotExpectedGoals * weights.expectedGoalsScale * 0.1f),
                    $"proposed next hex itself has Snapshot xG={explanation.immediateSnapshotExpectedGoals:0.000}");
            }
        }

        if (explanation.isFullPaceDribbleRoute)
        {
            AddScore(
                evaluation,
                explanation.fullPaceDribbleUtility,
                $"full-Pace dribble commits {explanation.fullPaceDribbleSteps} step(s) to "
                + $"{FormatHex(explanation.fullPaceDribbleDestination)}: progress={explanation.fullPaceDribbleProgress}, "
                + $"reach={explanation.fullPaceDribbleReachProbability:0.000}, "
                + $"turnover={explanation.fullPaceDribbleTurnoverProbability:0.000}, "
                + $"foul={explanation.fullPaceDribbleFoulProbability:0.000}, "
                + $"reachable challengers at endpoint={explanation.fullPaceDribbleEndpointPressure}, "
                + $"xG={explanation.fullPaceDribbleExpectedGoals:0.000}; "
                + $"evaluated {explanation.fullPaceDribbleRoutesEvaluated} route endpoints");
        }

        if (explanation.isFirstTimePassShotSetup)
        {
            AddScore(
                evaluation,
                (int)System.Math.Round(
                    explanation.firstTimePassCombinedExpectedGoals
                    * weights.firstTimePassShotRouteExpectedGoalsScale),
                $"FTP sequence sets {explanation.firstTimePassSetupShooterTokenKey} at "
                + $"{FormatHex(explanation.firstTimePassBestSetupHex)}, then reaches "
                + $"{FormatHex(explanation.firstTimePassReachableShotHex)}: pass retention="
                + $"{explanation.firstTimePassPassRetentionProbability:0.000}, movement reach="
                + $"{explanation.firstTimePassReachShotProbability:0.000}, raw Snapshot xG="
                + $"{explanation.firstTimePassRawShotExpectedGoals:0.000}, combined xG="
                + $"{explanation.firstTimePassCombinedExpectedGoals:0.000}");
            AddScore(
                evaluation,
                (int)System.Math.Round(
                    (explanation.firstTimePassPassRetentionProbability - 0.5f)
                    * weights.firstTimePassRetentionScale),
                $"FTP target survival={explanation.firstTimePassPassRetentionProbability:0.000}");
            if (explanation.firstTimePassExtraPaceUnlocksShot)
            {
                AddScore(
                    evaluation,
                    weights.firstTimePassExtraPaceUnlock,
                    $"FTP's free attacker hex converts a Shooting {explanation.firstTimePassSetupShooterShooting}, "
                    + $"Pace {explanation.firstTimePassSetupShooterPace} player from "
                    + $"{explanation.firstTimePassShotDistanceBefore} to "
                    + $"{explanation.firstTimePassShotDistanceAfter} step(s) from a shooting hex");
            }
        }

        if (explanation.highPassAccuracyProbability >= 0f)
        {
            int reliabilityAdjustment = (int)System.Math.Round(
                (explanation.highPassAccuracyProbability - 0.5f) * weights.highPassReliabilityScale);
            AddScore(
                evaluation,
                reliabilityAdjustment,
                $"high-pass accuracy={explanation.highPassAccuracyProbability:0.000}");
        }

        if (explanation.isGroundPassContinuation)
        {
            if (explanation.passRace != null)
                AddScore(evaluation, (int)System.Math.Round((explanation.passRace.continuationSurvival - 0.5f) * 300f),
                    $"collector={explanation.passRace.collectorTokenKey}, pickup={explanation.passRace.pickupCost}, phases={explanation.passRace.collectionPhases}, remaining Pace={explanation.passRace.remainingPace}; safe continuations={explanation.passRace.safeContinuationHexes}/{explanation.passRace.continuationHexes}, crossed ZOI={explanation.passRace.crossedZones}, opponent collects first={explanation.passRace.opponentCollectsFirst}, recovery={explanation.passRace.recoveryProbability:0.000}");
            if (explanation.groundPassRouteExpectedGoals >= 0f)
            {
                AddScore(
                    evaluation,
                    (int)Math.Round(
                        explanation.groundPassRouteExpectedGoals
                        * weights.groundPassRouteExpectedGoalsScale),
                    $"pass-to-Snapshot route via {explanation.groundPassCollectorTokenKey} has "
                    + $"retention={explanation.groundPassRetentionProbability:0.000}, "
                    + $"Snapshot xG={explanation.groundPassImmediateSnapshotExpectedGoals:0.000}, "
                    + $"complete route xG={explanation.groundPassRouteExpectedGoals:0.000} at "
                    + $"{FormatHex(explanation.groundPassContinuationTargetHex)}");
            }

            if (explanation.groundPassRetentionProbability >= 0f)
            {
                AddScore(
                    evaluation,
                    (int)Math.Round(
                        (explanation.groundPassRetentionProbability - 0.5f)
                        * weights.groundPassRetentionScale),
                    $"ground-pass retention={explanation.groundPassRetentionProbability:0.000}");
            }

            if (!string.IsNullOrWhiteSpace(explanation.groundPassCollectorTokenKey)
                && explanation.groundPassPrePickupChallengeCount == 0)
            {
                AddScore(
                    evaluation,
                    weights.groundPassProvenCollector,
                    $"proven collector {explanation.groundPassCollectorTokenKey} reaches the pass in "
                    + $"{explanation.groundPassCollectorPickupDistance} step(s)");
            }

            if (explanation.groundPassCollectorRemainingPace > 0)
            {
                AddScore(
                    evaluation,
                    explanation.groundPassCollectorRemainingPace * weights.groundPassRemainingPacePerHex,
                    $"collector retains {explanation.groundPassCollectorRemainingPace} Pace after pickup");
            }

            if (explanation.groundPassCreatesReachableShot
                && explanation.groundPassRouteExpectedGoals < 0f)
            {
                AddScore(
                    evaluation,
                    weights.groundPassReachableShot,
                    "pass sequence leaves the receiver able to reach a shooting hex");
            }

            if (explanation.groundPassDefensiveChallengeCount > 0)
            {
                int challengePenalty = explanation.groundPassCollectorPickupDistance > 0
                    ? weights.groundPassDefensiveChallengePenalty
                    : weights.groundPassImmediateReceiverChallengePenalty;
                AddScore(
                    evaluation,
                    -explanation.groundPassDefensiveChallengeCount * challengePenalty,
                    $"{explanation.groundPassDefensiveChallengeCount} defender(s) can challenge the receiver");
            }

            if (explanation.groundPassPrePickupChallengeCount > 0)
            {
                AddScore(
                    evaluation,
                    -explanation.groundPassPrePickupChallengeCount * weights.groundPassPrePickupChallengePenalty,
                    $"{explanation.groundPassPrePickupChallengeCount} defender(s) can reach the loose pass before collection");

                if (explanation.groundPassCollectorNeedsSecondMovement)
                {
                    AddScore(
                        evaluation,
                        -weights.groundPassForcedLooseBallTurnoverPenalty,
                        "delayed collector loses the stationary pass during the intervening defensive movement");
                }
            }

            if (explanation.groundPassIsSafeTwoPhaseSpacePlan)
            {
                AddScore(
                    evaluation,
                    weights.safeTwoPhaseSpacePlan,
                    $"Sam move: after pass survival {explanation.groundPassRetentionProbability:0.000}, "
                    + "defense cannot reach the loose ball before the planned collector");
            }

            if (explanation.groundPassCollectorNeedsSecondMovement)
            {
                evaluation.reasons.Add(
                    $"collector intentionally meets the ball in the second movement phase after "
                    + $"{explanation.groundPassCollectorPickupDistance} total step(s)");
            }

            if (explanation.groundPassCollectorIsPasser
                && explanation.groundPassEscapesImmediateTacklePressure)
            {
                AddScore(
                    evaluation,
                    weights.selfPassTackleEscape,
                    "passer releases into space and self-collects later, exchanging immediate tackle pressure for the calculated pass-interception risk");
            }

            if (explanation.groundPassCreatesSecondMovementShot)
            {
                AddScore(
                    evaluation,
                    weights.secondMovementShotPlan,
                    "collector can convert the safe pickup into a shooting position by the following movement phase");
            }

            if (explanation.isSupportingPassLaneMove)
            {
                AddScore(
                    evaluation,
                    weights.supportingPassLane,
                    $"movement creates the next ground-pass lane to {FormatHex(explanation.groundPassContinuationTargetHex)}");
            }
        }

        if (explanation.isRootActionPreview
            && explanation.isKickoffDecision
            && evaluation.candidate.actionType == RoomActionType.HighPass
            && (!explanation.highPassCollectorStartsOnTarget
                || explanation.highPassDefendersContesting > 0))
        {
            AddScore(
                evaluation,
                weights.kickoffSpeculativeHighPass,
                $"kickoff HP is speculative: collector distance={explanation.highPassCollectorDistance}, "
                + $"contesting defenders={explanation.highPassDefendersContesting}, "
                + $"retention={explanation.highPassExpectedPossessionProbability:0.000}");
        }

        if (explanation.isRootActionPreview
            && explanation.isKickoffDecision
            && evaluation.candidate.actionType == RoomActionType.GroundPass
            && explanation.targetToken != null
            && explanation.groundPassInterceptionCandidates == 0)
        {
            AddScore(
                evaluation,
                weights.kickoffSecureGroundPass,
                $"secure kickoff pass to {explanation.targetTokenKey} has no interception candidate");
        }

        if (evaluation.candidate.actionType == RoomActionType.HighPass
            && explanation.highPassTargetEvaluationComplete)
        {
            if (explanation.highPassTargetsEvaluated == 0
                || explanation.highPassBestTargetHex == null)
            {
                AddScore(evaluation, -500, "completed HP target evaluation found no viable collector/target outcome");
                evaluation.tier = RoomAITacticalTier.Risky;
            }
            else
            {
                int outcomeScore = (int)Math.Round(
                    (explanation.highPassTargetOutcomeValue - 100f) * 0.6f);
                AddScore(
                    evaluation,
                    outcomeScore,
                    $"HP target evaluation completed across {explanation.highPassTargetsEvaluated} target(s); "
                    + $"best={FormatHex(explanation.highPassBestTargetHex)} for {explanation.highPassBestCollectorTokenKey}, "
                    + $"possession={explanation.highPassExpectedPossessionProbability:0.000}, "
                    + $"outcome value={explanation.highPassTargetOutcomeValue:0.0}");
            }
        }

        bool defendingLooseBall = snapshot != null && !snapshot.attackHasPossession
            && (snapshot.expectedTeam != snapshot.teamInAttack || snapshot.expectedOpponentCollection)
            && evaluation.candidate.actionType == RoomActionType.StartMovement;
        if (!defendingLooseBall && explanation.receiverCanReachShootingHexNextMovement
            && (evaluation.candidate.actionType == RoomActionType.StartMovement
                || evaluation.candidate.actionType == RoomActionType.GroundPass))
        {
            AddScore(
                evaluation,
                weights.reachableShootingPosition,
                $"receiver can reach a shooting hex in {explanation.minimumStepsToShootingHex} step(s) next movement");
        }

        if (evaluation.candidate.actionType == RoomActionType.GroundPass
            && explanation.groundPassInterceptionCandidates > 0)
        {
            AddScore(
                evaluation,
                -explanation.groundPassInterceptionCandidates * weights.groundPassInterceptionPenalty,
                $"standard-pass path exposes {explanation.groundPassInterceptionCandidates} interception candidate(s)");
        }

        if (!defendingLooseBall && explanation.forwardProgressDelta > 0)
        {
            AddScore(
                evaluation,
                explanation.forwardProgressDelta * weights.forwardProgressPerColumn,
                $"advances {explanation.forwardProgressDelta} hex column(s)");
        }
        else if (!defendingLooseBall && explanation.forwardProgressDelta < 0)
        {
            AddScore(
                evaluation,
                -explanation.forwardProgressDelta * weights.retreatPerColumn,
                $"retreats {-explanation.forwardProgressDelta} hex column(s)");
        }

        bool setPieceDefenderMove = explanation.isSetPieceSetup && !explanation.setPieceActorIsAttacking;
        // An evaluated headed route already prices the resulting shot. Do not
        // give direct reception a second, generic shooting-position bonus.
        if (explanation.targetCanShootFrom && !setPieceDefenderMove && !defendingLooseBall
            && !HasEvaluatedHeadedShotRoute(explanation))
        {
            AddScore(evaluation, weights.shootingPosition, "target is a shooting position");
        }

        if (explanation.targetCanHeadFrom && !setPieceDefenderMove && !defendingLooseBall)
        {
            AddScore(evaluation, weights.headingPosition, "target is a heading position");
        }

        if (explanation.targetIsInOpponentGoal)
        {
            AddScore(
                evaluation,
                weights.walkBallIntoGoal,
                explanation.walkInExpectedGoalProbability >= 0f
                    ? $"walk-in xG={explanation.walkInExpectedGoalProbability:0.000} after the goalkeeper's best wall response"
                    : "ball carrier can step into the opponent goal");
        }

        if (explanation.targetIsThirdGoalkeeperWallHex)
        {
            AddScore(evaluation, weights.thirdGoalkeeperWallHex, "target is the penalized third goalkeeper-wall hex");
        }

        if (explanation.expectedGoals >= 0f)
        {
            string expectedGoalsLabel = explanation.targetIsInOpponentGoal
                ? "walk-in xG"
                : evaluation.candidate.actionType == RoomActionType.ContinueWithoutShot
                    && explanation.isSnapshotDecision
                        ? "retained full-shot xG"
                        : "shot xG";
            AddScore(
                evaluation,
                (int)Math.Round(explanation.expectedGoals * weights.expectedGoalsScale),
                $"{expectedGoalsLabel}={explanation.expectedGoals:0.000} "
                + $"(penalty={explanation.shootingPenalty}, blockers={explanation.shotBlockerCount}, gkPenalty={explanation.goalkeeperPenalty})");
        }

        if (explanation.isSetPieceHighPassPlan)
        {
            AddScore(
                evaluation,
                weights.setPieceHighPassFinishCorrection,
                $"set-piece HP accuracy={explanation.setPieceHighPassAccuracyProbability:0.000}, "
                + $"header-goal chance after accuracy={explanation.setPieceHeaderGoalProbability:0.000}, "
                + $"inaccuracy xG={explanation.setPieceInaccurateHighPassExpectedGoals:0.000} "
                + $"across {explanation.setPieceInaccuracyOutcomesEvaluated} direction-distance outcomes");
        }

        if (explanation.possessionChain != null)
        {
            evaluation.reasons.Add(
                $"possession-chain coverage={explanation.possessionChain.resolvedProbability:0.000}, "
                + $"unresolved={explanation.possessionChain.unresolvedProbability:0.000}");
        }

        if (explanation.isHeaderControlChoice && explanation.headerControlSuccessProbability >= 0f)
        {
            AddScore(
                evaluation,
                explanation.headerControlExpectedValue,
                $"ball control expected possession value={explanation.headerControlExpectedValue}");
            evaluation.reasons.Add(
                $"best ball-control option {explanation.bestHeaderControllerTokenKey} has "
                + $"Dribbling {explanation.bestHeaderControllerDribbling}, penalty {explanation.headerControlPenalty}, "
                + $"success={explanation.headerControlSuccessProbability:0.000}");
        }

        if (explanation.isFreeHeaderChoice)
        {
            AddScore(
                evaluation,
                explanation.freeHeaderExpectedValue,
                $"best free-header relay to {FormatHex(explanation.freeHeaderBestTargetHex)} "
                + $"for {explanation.freeHeaderBestReceiverTokenKey ?? "space"}: "
                + $"possession={explanation.freeHeaderExpectedPossessionProbability:0.000}, "
                + $"xG={explanation.freeHeaderExpectedGoals:0.000}, "
                + $"targets={explanation.freeHeaderTargetsEvaluated}");
        }

        if (explanation.isHeadedPassTarget)
        {
            evaluation.reasons.Add(explanation.headedPassTargetsPlayer
                ? explanation.headedPassTargetIsOffside
                    ? "headed pass targets a player whose collection would be called offside"
                    : "headed pass reaches a teammate directly and preserves the immediate Snapshot continuation"
                : $"headed pass enters space with {explanation.headedPassInterceptors} interceptor(s), "
                    + $"turnover probability={explanation.headedPassInterceptionProbability:0.000}; "
                    + (string.IsNullOrWhiteSpace(explanation.headedPassSpaceCollectorTokenKey)
                        ? "no collector is established; collection and next MP remain unresolved"
                        : $"candidate collector={explanation.headedPassSpaceCollectorTokenKey}, pickup={explanation.headedPassSpacePickupDistance}; "
                            + "reassess the next movement and opponent response"));
        }

        if (explanation.headedPassImmediateSnapshotExpectedGoals >= 0f)
        {
            AddScore(
                evaluation,
                (int)Math.Round(explanation.headedPassImmediateSnapshotExpectedGoals * weights.expectedGoalsScale),
                $"headed-pass receiver has immediate Snapshot xG={explanation.headedPassImmediateSnapshotExpectedGoals:0.000} "
                + $"(penalty={explanation.headedPassSnapshotPenalty}, blockers={explanation.headedPassSnapshotBlockers}, "
                + $"gkPenalty={explanation.headedPassSnapshotGoalkeeperPenalty})");
        }

        if (explanation.headedPassSpaceRouteExpectedGoals >= 0f)
        {
            AddScore(
                evaluation,
                (int)Math.Round(explanation.headedPassSpaceRouteExpectedGoals * weights.expectedGoalsScale),
                $"space route via {explanation.headedPassSpaceCollectorTokenKey} reaches "
                + $"{FormatHex(explanation.headedPassSpaceSnapshotHex)} with {explanation.headedPassSpaceRemainingPace} Pace after collection; "
                + $"reach={explanation.headedPassSpaceReachSnapshotProbability:0.000}, "
                + $"Snapshot xG={explanation.headedPassSpaceSnapshotExpectedGoals:0.000}, "
                + $"complete route xG={explanation.headedPassSpaceRouteExpectedGoals:0.000}");
        }

        if (explanation.isHighPassGoalkeeperDecision
            && explanation.defensiveExpectedGoalsAgainst >= 0f)
        {
            AddScore(
                evaluation,
                -(int)Math.Round(explanation.defensiveExpectedGoalsAgainst * weights.expectedGoalsScale),
                $"{(explanation.goalkeeperRushesForHighPass ? "rush" : "stay")} leaves "
                + $"opponent header xG={explanation.defensiveExpectedGoalsAgainst:0.000}");
        }

        if (evaluation.candidate.actionType == RoomActionType.ContinueWithoutShot
            && explanation.pursuesSecondBooking
            && explanation.secondBookingProbability > 0f)
        {
            AddScore(
                evaluation,
                (int)Math.Round(explanation.secondBookingProbability * weights.secondBookingProbabilityScale),
                $"preferred walk-in route has {explanation.secondBookingProbability:0.000} chance to draw a second booking");
        }

        if (evaluation.candidate.actionType == RoomActionType.ContinueWithoutShot
            && explanation.estimatedWalkInExpectedGoals >= 0f)
        {
            evaluation.reasons.Add(
                $"walk-in route xG={explanation.estimatedWalkInExpectedGoals:0.000} "
                + $"across {explanation.estimatedMovementPhasesToWalkIn} defensive phase(s)");
        }

        if (explanation.opponentsWithinTackleReach == 0 && explanation.targetHex != null)
        {
            AddScore(evaluation, weights.safeFromNextMovementTackle, "no opponent can reach tackling distance next movement");
        }
        else if (explanation.opponentsWithinTackleReach > 0)
        {
            AddScore(
                evaluation,
                weights.opponentInTackleReach * explanation.opponentsWithinTackleReach,
                $"{explanation.opponentsWithinTackleReach} opponent(s) can reach tackling distance");
        }

        if (explanation.friendlySupportWithinRange > 0)
        {
            int supportScore = Math.Min(
                weights.maximumSupportScore,
                explanation.friendlySupportWithinRange * weights.supportPerPlayer);
            AddScore(evaluation, supportScore, $"{explanation.friendlySupportWithinRange} teammate(s) can support the target");
        }

        ApplySetPiecePositionValue(evaluation, weights);
    }

    private static void ApplyLongBallGoalkeeperRecoveryValue(
        RoomAITacticalEvaluation evaluation,
        RoomAITacticalWeights weights)
    {
        RoomAICandidateExplanation explanation = evaluation.explanation;
        bool isClaim = evaluation.candidate.actionType == RoomActionType.LongBall;
        bool isMove = evaluation.candidate.actionType == RoomActionType.SetupMove;
        bool isDecline = evaluation.candidate.actionType == RoomActionType.Decline;

        if (string.Equals(explanation.longBallRecoveryMode, "ClaimLooseBall", StringComparison.Ordinal))
        {
            AddScore(
                evaluation,
                isClaim ? weights.longBallGoalkeeperClaim : -weights.longBallGoalkeeperClaim,
                explanation.longBallGoalkeeperCanReachBallWithFreeMove
                    ? $"Long Ball race: the GK's immediate free-Pace {explanation.longBallGoalkeeperFreePace} move "
                        + $"reaches the ball before the attacker, who needs {explanation.longBallAttackerMovementPhases} normal MP(s)"
                    : $"Long Ball race: after the immediate free-Pace {explanation.longBallGoalkeeperFreePace} move, "
                        + $"the GK needs {explanation.longBallGoalkeeperRemainingMovementPhasesAfterFreeMove} further MP(s) "
                        + $"and the attacker needs {explanation.longBallAttackerMovementPhases}");
            return;
        }

        if (string.Equals(explanation.longBallRecoveryMode, "SupportRecovery", StringComparison.Ordinal))
        {
            if (isMove)
            {
                int distanceError = System.Math.Abs(explanation.longBallGoalkeeperSupportDistance - 6);
                AddScore(
                    evaluation,
                    weights.longBallGoalkeeperRecoverySupport
                        - (distanceError * weights.longBallGoalkeeperSupportDistancePenalty),
                    $"support {explanation.longBallRecoveringDefenderTokenKey} from "
                    + $"{explanation.longBallGoalkeeperSupportDistance} hex(es) away after the Long Ball race");
            }
            else if (isDecline)
            {
                AddScore(evaluation, -40, "declining wastes the free GK support move before recovery");
            }

            return;
        }

        if (string.Equals(explanation.longBallRecoveryMode, "ForceDiveAtFeet", StringComparison.Ordinal))
        {
            if (isMove)
            {
                AddScore(
                    evaluation,
                    weights.longBallGoalkeeperDiveAtFeet
                        - (explanation.longBallGoalkeeperDistanceToLanding * 18)
                        - (System.Math.Max(0, explanation.goalkeeperOwnGoalDistanceDelta) * 12),
                    $"attacker wins the Long Ball race; close a weak dribbler from "
                    + $"{explanation.longBallGoalkeeperDistanceToLanding} hex(es)");
            }
            else if (isDecline)
            {
                AddScore(evaluation, -weights.longBallGoalkeeperDiveAtFeet, "declining concedes an uncontested box entry");
            }

            return;
        }

        if (isMove)
        {
            int uncoveredPaths = System.Math.Max(
                0,
                explanation.goalkeeperWallShotTargetCount - explanation.goalkeeperWallCoveredShotTargets);
            AddScore(
                evaluation,
                weights.longBallGoalkeeperFarPostSafety
                    + (explanation.goalkeeperWallCoverageQuality * weights.goalkeeperWallCoveragePerQuality)
                    - (uncoveredPaths * weights.goalkeeperUncoveredShotPathPenalty)
                    - (System.Math.Max(0, explanation.goalkeeperOwnGoalDistanceDelta)
                        * weights.goalkeeperOwnGoalDistancePenalty),
                $"attacker wins the Long Ball race with a far-post option; preserve "
                + $"{explanation.goalkeeperWallCoveredShotTargets}/{explanation.goalkeeperWallShotTargetCount} paths");
        }
        else if (isDecline)
        {
            AddScore(evaluation, 40, "stay deep rather than rush into a far-post passing threat");
        }
    }

    private static void ApplyTackleDecisionValue(
        RoomAITacticalEvaluation evaluation,
        RoomAITacticalWeights weights)
    {
        RoomAICandidateExplanation explanation = evaluation.explanation;
        float currentThreat = Math.Max(0f, explanation.defensiveExpectedGoalsAgainst);
        float repositionThreat = Math.Max(0f, explanation.dribblerBestRepositionExpectedGoals);
        float failedTackleGain = (repositionThreat * weights.failedTackleRepositionXgCost)
            + (Math.Max(0, explanation.dribblerBestRepositionProgress)
                * weights.failedTackleProgressCostPerColumn);

        float possessionBenefit = explanation.tackleSuccessProbability
            * (weights.tacklePossessionWinValue + (currentThreat * weights.tackleThreatDenialScale));
        float foulCost = explanation.tackleFoulProbability * weights.tackleFoulCost;
        float sendOffCost = explanation.tackleSendOffProbability * weights.tackleSendOffCost;
        float failedRepositionCost = explanation.dribblerRetainsProbability * failedTackleGain;
        int tackleScore = (int)Math.Round(possessionBenefit - foulCost - sendOffCost - failedRepositionCost);

        if (!explanation.tackleIsAttempted)
        {
            int standScore = weights.standGroundBaseValue
                - (int)Math.Round(currentThreat * weights.tackleThreatDenialScale * 0.45f);
            AddScore(
                evaluation,
                standScore,
                $"stand ground: carrier xG={currentThreat:0.000}; avoids foul/card risk and denies free reposition; "
                + $"containment value={standScore}, alternative tackle value={tackleScore}; win={explanation.tackleSuccessProbability:0.000}, "
                + $"foul={explanation.tackleFoulProbability:0.000}, card={explanation.tackleCardProbability:0.000}, send-off={explanation.tackleSendOffProbability:0.000}; "
                + $"threat removed on success={currentThreat:0.000}, possession benefit={possessionBenefit:0.0}, foul cost={foulCost:0.0}, "
                + $"send-off cost={sendOffCost:0.0}, reposition cost={failedRepositionCost:0.0}, reposition xG={repositionThreat:0.000}");
            return;
        }

        AddScore(
            evaluation,
            tackleScore,
            $"{(explanation.tackleIsFromBehind ? "from-behind " : string.Empty)}tackle EV: "
            + $"win={explanation.tackleSuccessProbability:0.000}, foul={explanation.tackleFoulProbability:0.000}, "
            + $"send-off={explanation.tackleSendOffProbability:0.000}, dribbler-retains={explanation.dribblerRetainsProbability:0.000}; "
            + $"card={explanation.tackleCardProbability:0.000}, possession benefit={possessionBenefit:0.0}, foul cost={foulCost:0.0}, send-off cost={sendOffCost:0.0}, reposition cost={failedRepositionCost:0.0}; "
            + $"best failed-tackle reposition={FormatHex(explanation.dribblerBestRepositionHex)}, "
            + $"xG={repositionThreat:0.000}, progress={explanation.dribblerBestRepositionProgress}");

        if (explanation.tackleIsFromBehind || explanation.tackleSendOffProbability >= 0.08f)
        {
            evaluation.tier = RoomAITacticalTier.Risky;
        }
    }

    private static void ApplySetPiecePositionValue(
        RoomAITacticalEvaluation evaluation,
        RoomAITacticalWeights weights)
    {
        RoomAICandidateExplanation explanation = evaluation.explanation;
        if (explanation == null || !explanation.isSetPieceSetup || !explanation.isCornerSetup)
        {
            return;
        }

        if (explanation.setPieceActorIsAttacking)
        {
            if (explanation.targetIsCornerHeaderAnchor
                && explanation.setPieceActorHeaderRank > 0
                && explanation.setPieceActorHeaderRank <= 3)
            {
                AddScore(
                    evaluation,
                    weights.cornerHeaderAnchor,
                    $"places top-{explanation.setPieceActorHeaderRank} header on the penalty-spot/wide corner structure");
            }

            if (explanation.isCornerFreeThreeMove
                && explanation.adjacentVeryGoodAttackingHeaders > 0)
            {
                AddScore(
                    evaluation,
                    explanation.adjacentVeryGoodAttackingHeaders * weights.cornerFinalCongestionPerHeader,
                    $"final 3-hex move creates {explanation.adjacentVeryGoodAttackingHeaders + 1}-attacker heading congestion");
            }
            return;
        }

        if (explanation.adjacentMarkedAttackerHeading >= weights.strongAttributeThreshold)
        {
            int matchupAdjustment = System.Math.Max(
                -2,
                System.Math.Min(2, explanation.setPieceActorHeading - explanation.adjacentMarkedAttackerHeading));
            AddScore(
                evaluation,
                weights.cornerAdjacentEliteMark + (matchupAdjustment * 5),
                $"marks Heading {explanation.adjacentMarkedAttackerHeading} attacker adjacently with Heading {explanation.setPieceActorHeading}");
        }
        else if (explanation.twoHexMarkedAttackerHeading >= weights.strongAttributeThreshold)
        {
            AddScore(
                evaluation,
                weights.cornerTwoHexCover,
                $"keeps two-hex cover on Heading {explanation.twoHexMarkedAttackerHeading} attacker, but would take the -1 distance penalty");
        }
    }

    private static void ApplyDefensivePhasePlanValue(
        RoomAITacticalEvaluation evaluation,
        RoomAITacticalWeights weights)
    {
        RoomAICandidateExplanation explanation = evaluation.explanation;
        if (string.IsNullOrWhiteSpace(explanation?.defensivePlanRole))
        {
            return;
        }

        AddScore(
            evaluation,
            weights.defensivePlanActorRole,
            $"assigned defensive phase role={explanation.defensivePlanRole}"
                + (string.IsNullOrWhiteSpace(explanation.defensivePlanIntent)
                    ? string.Empty
                    : $" ({explanation.defensivePlanIntent})"));

        if (SameHex(explanation.targetHex, explanation.defensivePlanDestination))
        {
            AddScore(
                evaluation,
                weights.defensivePlanExactDestination,
                $"executes committed {explanation.defensivePlanRole} destination "
                    + FormatHex(explanation.defensivePlanDestination));
        }

        if (!string.Equals(
                explanation.defensivePlanRole,
                "PrimaryPressure",
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (explanation.defensiveMoveEndsAdjacentToBall)
        {
            AddScore(
                evaluation,
                weights.defensivePlanAdjacentPressure,
                "ends adjacent to the ball carrier and closes tackle/touch actions");
        }
        else if (explanation.defensiveLegalAdjacencyAvailable
            && explanation.targetDistanceToBall == 2)
        {
            AddScore(
                evaluation,
                -weights.defensivePlanStopsShortPenalty,
                "stops one hex short although legal adjacency can close meaningful actions");
        }
    }

    private static bool SameHex(RoomHexCoordinates left, RoomHexCoordinates right)
    {
        return left != null
            && right != null
            && left.x == right.x
            && left.z == right.z;
    }

    private static bool HasEvaluatedHeadedShotRoute(RoomAICandidateExplanation explanation)
    {
        return explanation?.isHeadedPassTarget == true
            && (explanation.headedPassImmediateSnapshotExpectedGoals >= 0f
                || explanation.headedPassSpaceRouteExpectedGoals >= 0f);
    }

    private static void ApplyPlayerValue(
        RoomAITacticalEvaluation evaluation,
        RoomAITacticalWeights weights)
    {
        RoomAICandidateExplanation explanation = evaluation.explanation;
        if (explanation == null)
        {
            return;
        }

        if (explanation.isGoalkeeperPositioning
            || explanation.isRegularFinalThirdReorganization
            || explanation.isSnapshotDefensiveMove)
        {
            return;
        }

        RoomAITokenSnapshot actingToken = explanation.targetToken ?? explanation.actorToken;
        if (actingToken == null)
        {
            return;
        }

        switch (evaluation.candidate.actionType)
        {
            case RoomActionType.Shot:
                AddScore(evaluation, actingToken.shooting * weights.shootingAttribute, $"player shooting={actingToken.shooting}");
                break;
            case RoomActionType.HighPass:
                AddScore(evaluation, actingToken.highPass * weights.highPassAttribute, $"player high pass={actingToken.highPass}");
                break;
            case RoomActionType.Header:
                if (explanation.isHeaderControlChoice)
                {
                    AddScore(
                        evaluation,
                        actingToken.dribbling * weights.dribblingAttribute,
                        $"ball controller dribbling={actingToken.dribbling}");
                }
                else if (!explanation.isHeadedPassTarget)
                {
                    AddScore(evaluation, actingToken.heading * weights.headingAttribute, $"player heading={actingToken.heading}");
                }
                break;
            case RoomActionType.StartMovement:
            case RoomActionType.SelectHex:
            case RoomActionType.SelectToken:
                AddScore(evaluation, actingToken.pace * weights.paceAttribute, $"player pace={actingToken.pace}");
                AddScore(evaluation, actingToken.dribbling * weights.dribblingAttribute, $"player dribbling={actingToken.dribbling}");
                break;
            default:
                AddScore(evaluation, actingToken.dribbling * weights.receiverDribblingAttribute, $"receiver dribbling={actingToken.dribbling}");
                break;
        }
    }

    private static void ApplyPlanValue(
        RoomAITacticalEvaluation evaluation,
        RoomAIAttackPlan plan,
        RoomAITacticalWeights weights)
    {
        if (plan == null)
        {
            return;
        }

        if (evaluation.explanation?.isRegularFinalThirdReorganization == true
            || evaluation.explanation?.isSnapshotDefensiveMove == true)
        {
            return;
        }

        if (plan.rosterAttackPlan?.options.Count > 0)
        {
            // Assess every compatible route, not just the currently highest
            // roster suitability. Never stack bonuses for synonymous plans.
            var matches = plan.rosterAttackPlan.options.FindAll(option =>
                RoomAIRosterAttackPlan.Supports(option, evaluation.explanation));
            if (matches.Count > 0)
                AddScore(evaluation, weights.progressPossessionPlan,
                    "playing-XI routes with concrete continuation: "
                    + string.Join(", ", matches.ConvertAll(option => option.route.ToString()).Distinct()));
            return;
        }

        switch (plan.planType)
        {
            case RoomAIAttackPlanType.FinishChance when evaluation.candidate.actionType == RoomActionType.Shot:
                AddScore(evaluation, weights.finishPlan, "matches finish-chance plan");
                break;
            case RoomAIAttackPlanType.FinishChance when evaluation.explanation?.isSetPieceHighPassPlan == true:
                AddScore(evaluation, weights.finishPlan, "set-piece header route matches finish-chance plan");
                break;
            case RoomAIAttackPlanType.FinishChance
                when evaluation.candidate.actionType == RoomActionType.ContinueWithoutShot
                    && evaluation.explanation?.isSnapshotDecision == true:
                AddScore(evaluation, weights.finishPlan, "protects a higher-value full-shot continuation");
                break;
            case RoomAIAttackPlanType.WideAerialAttack
                when evaluation.candidate.actionType == RoomActionType.HighPass
                    && evaluation.explanation?.hasPossessionLevelPlan != true:
                AddScore(evaluation, weights.wideAerialActionPlan, "matches wide aerial plan");
                break;
            case RoomAIAttackPlanType.WideAerialAttack when evaluation.explanation?.targetCanHeadFrom == true:
                AddScore(evaluation, weights.wideAerialPositionPlan, "positions a player in a heading zone");
                break;
            case RoomAIAttackPlanType.SafeDribble
                when evaluation.explanation?.opponentsWithinTackleReach == 0
                    && evaluation.explanation?.hasPossessionLevelPlan != true:
                AddScore(evaluation, weights.safeDribblePlan, "matches safe-dribble plan");
                break;
            case RoomAIAttackPlanType.CreateShot when evaluation.explanation?.targetCanShootFrom == true
                && !HasEvaluatedHeadedShotRoute(evaluation.explanation):
                AddScore(evaluation, weights.createShotPlan, "matches shot-creation plan");
                break;
            case RoomAIAttackPlanType.CreateShot
                when evaluation.candidate.actionType == RoomActionType.StartMovement
                    && evaluation.explanation?.receiverCanReachShootingHexNextMovement == true:
                AddScore(evaluation, weights.createShotPlan, "movement can create a shot with the current ball carrier");
                break;
            case RoomAIAttackPlanType.ProgressPossession
                when evaluation.explanation?.advancesTowardOpponentGoal == true
                    && evaluation.explanation?.hasPossessionLevelPlan != true:
                AddScore(evaluation, weights.progressPossessionPlan, "matches possession-progression plan");
                break;
        }
    }

    private static void ResolveTier(RoomAITacticalEvaluation evaluation)
    {
        if (evaluation.explanation?.isTackleDecision == true)
        {
            bool emergencyThreat = evaluation.explanation.defensiveExpectedGoalsAgainst >= 0.35f;
            if (evaluation.explanation.tackleIsAttempted
                && !emergencyThreat
                && (evaluation.explanation.tackleIsFromBehind
                    || evaluation.explanation.tackleSendOffProbability >= 0.08f))
            {
                evaluation.tier = RoomAITacticalTier.Risky;
            }
            else
            {
                evaluation.tier = evaluation.score > 0
                    ? RoomAITacticalTier.SafeProgression
                    : RoomAITacticalTier.Neutral;
            }

            return;
        }

        if (evaluation.explanation?.isPenaltyFoulDecision == true)
        {
            evaluation.tier = RoomAITacticalTier.ImmediateFinish;
            return;
        }

        if (evaluation.explanation?.isLateResultManagementDecision == true
            && evaluation.explanation.lateResultScoreDifference > 0)
        {
            evaluation.tier = evaluation.candidate.actionType == RoomActionType.LongBall
                ? RoomAITacticalTier.SafeProgression
                : evaluation.candidate.actionType == RoomActionType.QuickThrow
                    ? RoomAITacticalTier.Risky
                    : RoomAITacticalTier.Neutral;
            return;
        }

        if (evaluation.explanation?.isRegularFinalThirdReorganization == true)
        {
            evaluation.tier = evaluation.explanation.defensiveLineLeavesFullback || evaluation.explanation.newlyOnsideOpponents > 0
                ? RoomAITacticalTier.Risky
                : evaluation.explanation.offsideLineRaise > 0
                ? RoomAITacticalTier.SafeProgression
                : evaluation.explanation.reachableOnsideOpponents > 0
                    ? RoomAITacticalTier.RetainPossession
                    : RoomAITacticalTier.Neutral;
            return;
        }

        if (evaluation.explanation?.isRootActionPreview == true
            && evaluation.candidate.actionType is RoomActionType.HighPass or RoomActionType.GoalkeeperKick)
        {
            evaluation.tier = evaluation.explanation.highPassCollectorStartsOnTarget
                && evaluation.explanation.highPassExpectedPossessionProbability >= 0.55f
                && (evaluation.explanation.targetCanShootFrom || evaluation.explanation.targetCanHeadFrom)
                    ? RoomAITacticalTier.CreateFinish
                    : evaluation.explanation.highPassExpectedPossessionProbability >= 0.55f
                        ? RoomAITacticalTier.SafeProgression
                        : RoomAITacticalTier.RetainPossession;
            return;
        }

        if (evaluation.explanation?.isGroundPassContinuation == true
            && evaluation.explanation.groundPassCollectorNeedsSecondMovement
            && evaluation.explanation.groundPassPrePickupChallengeCount > 0)
        {
            evaluation.tier = RoomAITacticalTier.Risky;
            return;
        }

        if (evaluation.explanation?.isSupportingPassLaneMove == true
            && (evaluation.explanation.groundPassRetentionProbability < .5f
                || evaluation.explanation.passRace?.continuationSurvival < .5f))
        {
            evaluation.tier = RoomAITacticalTier.Risky;
            return;
        }

        if (evaluation.explanation?.isGroundPassContinuation == true
            && evaluation.explanation.groundPassRouteExpectedGoals >= .1f)
        {
            evaluation.tier = evaluation.candidate.actionType == RoomActionType.GroundPass
                    ? RoomAITacticalTier.ImmediateFinish
                    : RoomAITacticalTier.CreateFinish;
            return;
        }

        if (evaluation.explanation?.isFirstTimePassShotSetup == true
            && evaluation.explanation.firstTimePassCombinedExpectedGoals >= 0f)
        {
            evaluation.tier = RoomAITacticalTier.CreateFinish;
            return;
        }

        if (evaluation.explanation?.isRootActionPreview == true
            && evaluation.candidate.actionType == RoomActionType.GroundPass
            && evaluation.explanation.receiverCanReachShootingHexNextMovement)
        {
            evaluation.tier = RoomAITacticalTier.CreateFinish;
            return;
        }

        if (evaluation.explanation?.isGroundPassContinuation == true
            && evaluation.explanation.groundPassCreatesReachableShot)
        {
            evaluation.tier = RoomAITacticalTier.CreateFinish;
            return;
        }

        if (evaluation.explanation?.targetIsInOpponentGoal == true
            || evaluation.explanation?.isSetPieceHighPassPlan == true
            || evaluation.candidate.actionType == RoomActionType.Shot
            || (evaluation.candidate.actionType == RoomActionType.ContinueWithoutShot
                && evaluation.explanation?.isSnapshotDecision == true))
        {
            evaluation.tier = RoomAITacticalTier.ImmediateFinish;
        }
        else if (evaluation.explanation?.headedPassSpaceRouteExpectedGoals >= 0f
            || evaluation.explanation?.targetCanShootFrom == true
            || evaluation.explanation?.targetCanHeadFrom == true)
        {
            evaluation.tier = RoomAITacticalTier.CreateFinish;
        }
        else if (evaluation.explanation?.advancesTowardOpponentGoal == true
            && evaluation.explanation.opponentsWithinTackleReach == 0)
        {
            evaluation.tier = RoomAITacticalTier.SafeProgression;
        }
        else if (evaluation.explanation != null
            && (evaluation.explanation.targetHex != null || evaluation.explanation.targetTokenHex != null)
            && evaluation.explanation.opponentsWithinTackleReach == 0)
        {
            evaluation.tier = RoomAITacticalTier.RetainPossession;
        }
        else if (evaluation.explanation?.opponentsWithinTackleReach > 1)
        {
            evaluation.tier = RoomAITacticalTier.Risky;
        }
    }

    private static string FormatHex(RoomHexCoordinates coordinates)
    {
        return coordinates == null ? "an unknown hex" : $"({coordinates.x}, {coordinates.z})";
    }

    private static bool HasRootActionChoice(RoomAIDecisionSnapshot snapshot)
    {
        if (snapshot?.legalActions == null)
        {
            return false;
        }

        foreach (RoomActionCandidate action in snapshot.legalActions)
        {
            if (action?.step == RoomDecisionStep.ChooseActionType)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasImmediateShotCreationRoute(RoomAIDecisionSnapshot snapshot)
    {
        if (snapshot?.candidateExplanations == null)
        {
            return false;
        }

        foreach (RoomAICandidateExplanation explanation in snapshot.candidateExplanations)
        {
            if (explanation == null)
            {
                continue;
            }

            if (explanation.actionType == RoomActionType.GroundPass && explanation.targetCanShootFrom)
            {
                return true;
            }

            if (explanation.actionType == RoomActionType.StartMovement
                && explanation.receiverCanReachShootingHexNextMovement)
            {
                return true;
            }

            if (explanation.isFirstTimePassShotSetup)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsNeutralTacticalForfeit(
        RoomActionCandidate candidate,
        RoomAICandidateExplanation explanation)
    {
        return candidate != null
            && (string.Equals(candidate.manager, nameof(FinalThirdManager), StringComparison.Ordinal)
                || explanation?.isGoalkeeperPositioning == true
                || explanation?.isSuccessfulTackleReposition == true);
    }

    private static int CompareEvaluations(RoomAITacticalEvaluation left, RoomAITacticalEvaluation right)
    {
        int goalkeeperSafety = RoomAITacticalSafety.CompareGoalkeeperSafety(left, right);
        if (goalkeeperSafety != 0) return goalkeeperSafety;
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

    private static bool IsExpectedTeamAttacking(RoomAIDecisionSnapshot snapshot)
    {
        return snapshot != null
            && snapshot.attackHasPossession
            && !string.IsNullOrWhiteSpace(snapshot.expectedTeam)
            && string.Equals(snapshot.expectedTeam, snapshot.teamInAttack, StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasLegalAction(RoomAIDecisionSnapshot snapshot, RoomActionType actionType)
    {
        if (snapshot == null)
        {
            return false;
        }

        foreach (RoomActionCandidate action in snapshot.legalActions)
        {
            if (action != null && action.actionType == actionType && action.isExecutableNow)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasTeamPlayerWithAttribute(
        RoomAIDecisionSnapshot snapshot,
        Predicate<RoomAITokenSnapshot> predicate)
    {
        if (snapshot == null || predicate == null)
        {
            return false;
        }

        foreach (RoomAITokenSnapshot token in snapshot.tokens)
        {
            if (token != null
                && token.isPlaying
                && string.Equals(token.teamSide, snapshot.expectedTeam, StringComparison.OrdinalIgnoreCase)
                && predicate(token))
            {
                return true;
            }
        }

        return false;
    }

    private static RoomAICandidateExplanation FindExplanation(
        RoomAIDecisionSnapshot snapshot,
        RoomActionCandidate candidate)
    {
        if (snapshot == null || candidate == null)
        {
            return null;
        }

        foreach (RoomAICandidateExplanation explanation in snapshot.candidateExplanations)
        {
            if (explanation?.candidate == candidate)
            {
                return explanation;
            }
        }

        return null;
    }

    private static string DescribePlanRationale(RoomAIAttackPlanType planType)
    {
        return planType switch
        {
            RoomAIAttackPlanType.FinishChance => "A legal shot is already available.",
            RoomAIAttackPlanType.CreateShot => "Use movement to construct a stronger shooting position.",
            RoomAIAttackPlanType.WideAerialAttack => "The aerial route is justified by shooter scarcity or a heading advantage against the central defenders.",
            RoomAIAttackPlanType.SafeDribble => "The team has a strong dribbler who can progress through safer space.",
            RoomAIAttackPlanType.ProgressPossession => "Advance possession while preserving future options.",
            _ => "No attacking plan is active."
        };
    }

    private static void AddScore(RoomAITacticalEvaluation evaluation, int value, string reason)
    {
        evaluation.score += value;
        if (!string.IsNullOrWhiteSpace(reason))
        {
            evaluation.reasons.Add($"{(value >= 0 ? "+" : string.Empty)}{value} {reason}");
        }
    }
}
