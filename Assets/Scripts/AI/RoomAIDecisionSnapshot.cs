using System.Collections;
using System.Collections.Generic;
using System.Linq;

public sealed class RoomAIDecisionSnapshot
{
    public GameplayInstructionSnapshot instruction;
    public RoomDecisionContext decisionContext;
    public RoomRuntimeSnapshot runtime;
    public string manager;
    public string expectedTeam;
    public string expectedInput;
    public string currentState;
    public string teamInAttack;
    public bool attackHasPossession;
    public string homeTeamDirection;
    public string awayTeamDirection;
    public RoomHexCoordinates ball;
    public RoomHexCoordinates committedAerialTarget;
    public RoomClockSnapshot clock;
    public int homeScore;
    public int awayScore;
    public int halfDurationMinutes = 45;
    public int regulationHalfCount = 2;
    public int freshAttackingMoveLimit = MovementPhaseManager.DefaultAttackingMoveLimit;
    public int defensiveMovesRemaining = MovementPhaseManager.DefaultDefendingMoveLimit;
    public bool repeatsNoProgressMovementState;
    public bool expectedOpponentCollection;
    public bool firstAerialOffsideExempt;
    public RoomAIMatchupTacticalProfile tacticalMatchup;
    public RoomAIDefensivePhasePlan defensivePhasePlan;
    internal readonly Dictionary<string, List<RoomHexCoordinates>> defensiveMovementDestinations = new();
    internal readonly Dictionary<string, float> defensiveThreatValues = new();
    internal PlayerToken preparedShooter;
    internal HexCell preparedShotOrigin;
    internal readonly List<(RoomHexCoordinates hex, float probability)> preparedRebounds = new();
    public RoomAIKickoffPlan kickoffPlan;
    internal readonly Dictionary<RoomActionCandidate, RoomAIKickoffPlan> kickoffOpeningCache = new();
    public readonly List<RoomAITokenSnapshot> tokens = new();
    public readonly List<RoomActionCandidate> legalActions = new();
    public readonly List<RoomAICandidateExplanation> candidateExplanations = new();
    internal bool hasCachedFirstTimePassTargets;
    internal readonly List<HexCell> cachedFirstTimePassTargets = new();
    internal Dictionary<string, RoomAIExpectedGoalsPreview> expectedGoalsCache = new();
    internal Dictionary<string, RoomAIReachabilityPreview> reachabilityCache = new();
    internal Dictionary<string, RoomAIPassRacePreview> passRaceCache = new();
    internal readonly Dictionary<string, RoomAIDefensiveResponsePreview> defensiveResponseCache = new();
    internal readonly Dictionary<string, RoomAIActionSequencePreview> aerialLandingCache = new();
    internal readonly Dictionary<string, Dictionary<HexCell, List<HexCell>>> defensiveResponsePrimaryReach = new();
    internal Dictionary<string, Dictionary<PlayerToken, HashSet<HexCell>>> passRaceDefensiveReachCache = new();
    internal readonly Dictionary<string, RoomAIFirstTimePassSetupFacts> firstTimePassSetupCache = new();
    internal readonly Dictionary<string, (float laneValue, float counterExposure)> supportingPassLaneBaselines = new();
    internal readonly Dictionary<string, bool> existingGroundOutletUsability = new();
    internal bool? existingGroundOutletBoardMatchesLive;
    internal List<HexCell> continuationShootingHexes;
    internal string providerWorkStage;
    public string ProviderWorkStage => providerWorkStage;
    public bool splitProviderProfileStages;
    internal int reachabilityCacheHits;
    internal int firstTimePassSetupCacheHits;
    internal readonly Dictionary<string, List<HexCell>> opponentChallengeReach = new();
    internal readonly Dictionary<PlayerToken, List<HexCell>> f3OpponentCollectionPaths = new();
    internal readonly Dictionary<(int x, int z), int> opponentChallengeCounts = new();
}

public sealed class RoomAIReachabilityPreview
{
    public List<HexCell> reachableHexes;
    public Dictionary<HexCell, (int distance, bool enteredZOI)> distances;
}

public sealed class RoomAIFirstTimePassSetupFacts
{
    public bool found;
    public bool extraPaceUnlocksShot;
    public string shooterTokenKey;
    public int shooterPace;
    public int shooterShooting;
    public RoomHexCoordinates setupHex;
    public RoomHexCoordinates shotHex;
    public int distanceBefore;
    public int distanceAfter;
    public float passRetention;
    public float reachShotProbability;
    public float rawExpectedGoals;
    public float routeExpectedGoals;
    public float combinedExpectedGoals;
    public RoomHexCoordinates rootTargetHex;
}

public sealed class RoomAIExpectedGoalsPreview
{
    public bool calculated;
    public float expectedGoals;
    public int shootingPenalty;
    public int blockerCount;
    public int goalkeeperPenalty;
}

public sealed class RoomAIDefensivePhasePlan
{
    public bool includesBoundedReceiverShotRoutes;
    public bool includesStaticKeeperFtpRelays;
    public readonly List<string> staticKeeperFtpRelayPairs = new();
    public readonly Dictionary<string, float> initialStaticKeeperFtpThreatByOutlet = new();
    public readonly Dictionary<string, float> initialKeeperFtpThreatAfterDefendingMoveByOutlet = new();
    public float initialThreatEstimate;
    public float remainingThreatEstimate;
    public readonly List<RoomAIDefensiveAssignment> assignments = new();
    public string primaryPressureTokenKey;
    public RoomHexCoordinates primaryPressureDestination;
    public string primaryPressureIntent;
    public string coverTokenKey;
    public RoomHexCoordinates coverDestination;
    public string coverIntent;
    public string outletDenialTokenKey;
    public RoomHexCoordinates outletDenialDestination;
    public string outletTargetTokenKey;
    public string defensiveLineTokenKey;
    public RoomHexCoordinates defensiveLineDestination;
    public bool legalAdjacencyAvailable;
}

public sealed class RoomAIDefensiveAssignment
{
    public string tokenKey;
    public string role;
    public float threatReduction;
    public int movePriority;
    public string targetTokenKey;
    public RoomHexCoordinates destination;
}

public sealed class RoomAICandidateExplanation
{
    // Cached facts are immutable after creation; presentation/choice identity is
    // rebound when the same target is offered again at the confirmation stage.
    public RoomAICandidateExplanation ForCandidate(RoomActionCandidate action)
    {
        var copy = (RoomAICandidateExplanation)MemberwiseClone();
        copy.candidate = action;
        copy.label = action.label;
        copy.manager = action.manager;
        copy.actionType = action.actionType;
        copy.step = action.step;
        copy.key = action.key;
        return copy;
    }

    public RoomActionCandidate candidate;
    public string label;
    public string manager;
    public RoomActionType actionType;
    public RoomDecisionStep step;
    public string key;
    public RoomHexCoordinates aerialPreparationHex;
    public string actorTokenKey;
    public RoomHexCoordinates actorHex;
    public string targetTokenKey;
    public RoomHexCoordinates targetTokenHex;
    public RoomHexCoordinates targetHex;
    public int actorDistanceToBall = -1;
    public int targetDistanceToBall = -1;
    public int forwardProgressDelta;
    public bool advancesTowardOpponentGoal;
    public bool targetCanShootFrom;
    public bool targetCanHeadFrom;
    public bool targetIsInOpponentGoal;
    public bool targetIsThirdGoalkeeperWallHex;
    public float walkInExpectedGoalProbability = -1f;
    public int targetDistanceToOpponentGoal = -1;
    public int nearestOpponentDistance = -1;
    public int opponentsWithinTackleReach;
    public int bestReachableOpponentTackling;
    public int receiverDribbling;
    public int tackleToDribblingMismatch;
    public int friendlySupportWithinRange;
    public bool targetIsInFinalThird;
    public int minimumStepsToOpponentPenaltyBox = -1;
    public int minimumStepsToShootingHex = -1;
    public bool receiverCanReachOpponentPenaltyBoxNextMovement;
    public bool receiverCanReachShootingHexNextMovement;
    public int groundPassInterceptionCandidates;
    public bool isGroundPassContinuation;
    public bool isSupportingPassLaneMove;
    public float supportingPassLaneValueGain;
    public string groundPassCollectorTokenKey;
    public RoomHexCoordinates groundPassContinuationTargetHex;
    public int groundPassCollectorPickupDistance = -1;
    public int groundPassCollectorRemainingPace = -1;
    public int groundPassDefensiveChallengeCount;
    public int groundPassPrePickupChallengeCount;
    public RoomAIPassRacePreview passRace;
    public RoomAIActionSequencePreview actionSequence;
    public bool groundPassIsSafeTwoPhaseSpacePlan;
    public bool groundPassCreatesSecondMovementShot;
    public bool groundPassCollectorNeedsSecondMovement;
    public bool groundPassCollectorIsPasser;
    public bool groundPassEscapesImmediateTacklePressure;
    public float groundPassRetentionProbability = -1f;
    public float groundPassImmediateSnapshotExpectedGoals = -1f;
    public float groundPassRouteExpectedGoals = -1f;
    public bool groundPassCreatesReachableShot;
    public bool isFirstTimePassShotSetup;
    public bool firstTimePassExtraPaceUnlocksShot;
    public string firstTimePassSetupShooterTokenKey;
    public int firstTimePassSetupShooterPace;
    public int firstTimePassSetupShooterShooting;
    public RoomHexCoordinates firstTimePassBestSetupHex;
    public RoomHexCoordinates firstTimePassReachableShotHex;
    public int firstTimePassShotDistanceBefore = -1;
    public int firstTimePassShotDistanceAfter = -1;
    public float firstTimePassPassRetentionProbability = -1f;
    public float firstTimePassReachShotProbability = -1f;
    public float firstTimePassRawShotExpectedGoals = -1f;
    public float firstTimePassShotRouteExpectedGoals = -1f;
    public float firstTimePassCombinedExpectedGoals = -1f;
    public bool hasSetPieceShotDefense;
    public bool isSetPieceSetup;
    public bool isCornerSetup;
    public bool isCornerFreeThreeMove;
    public bool isCornerDefenseMove;
    public float cornerDefenseGain;
    public bool setPieceActorIsAttacking;
    public int setPieceActorHeading;
    public int setPieceActorHeaderRank = -1;
    public bool targetIsCornerHeaderAnchor;
    public int adjacentVeryGoodAttackingHeaders;
    public int adjacentMarkedAttackerHeading;
    public int twoHexMarkedAttackerHeading;
    public int targetDistanceToSetPieceBall = -1;
    public bool isSetPieceHighPassPlan;
    public float setPieceHighPassAccuracyProbability = -1f;
    public float setPieceHeaderGoalProbability = -1f;
    public float setPieceInaccurateHighPassExpectedGoals = -1f;
    public int setPieceInaccuracyOutcomesEvaluated;
    public bool isRootActionPreview;
    public bool isKickoffDecision;
    public float highPassAccuracyProbability = -1f;
    public bool highPassTargetEvaluationComplete;
    public int highPassTargetsEvaluated;
    public RoomHexCoordinates highPassBestTargetHex;
    public string highPassBestCollectorTokenKey;
    public float highPassExpectedPossessionProbability = -1f;
    public float highPassTargetOutcomeValue = -1f;
    public bool highPassCollectorStartsOnTarget;
    public int highPassCollectorDistance = -1;
    public int highPassDefendersContesting;
    public bool isHighPassGoalkeeperDecision;
    public bool goalkeeperRushesForHighPass;
    public float defensiveExpectedGoalsAgainst = -1f;
    public float expectedGoals = -1f;
    public int shootingPenalty;
    public int shotBlockerCount;
    public int goalkeeperPenalty;
    public bool isSnapshotDecision;
    public bool timeAllowsFollowUpShot;
    public bool goalkeeperCanChallengeBeforeFullShot;
    public float fullShotExpectedGoals = -1f;
    public float estimatedBallRetentionProbability = 1f;
    public float estimatedPenaltyProbability;
    public float penaltyExpectedGoals = -1f;
    public bool isPenaltyFoulDecision;
    public bool isLateResultManagementDecision;
    public int lateResultScoreDifference;
    public int lateResultActionsRemaining;
    public bool likelyChallengerIsBooked;
    public bool likelyChallengeIsDangerous;
    public bool pursuesSecondBooking;
    public float secondBookingProbability;
    public int estimatedMovementPhasesToWalkIn = -1;
    public float estimatedWalkInExpectedGoals = -1f;
    public int forecastChallengeCount;
    public string forecastChallengeSequence;
    public bool isHeaderControlChoice;
    public bool isFreeHeaderChoice;
    public string bestHeaderControllerTokenKey;
    public int bestHeaderControllerDribbling;
    public int headerControlPenalty;
    public float headerControlSuccessProbability = -1f;
    public int headerControlContinuationValue;
    public bool isHeadedPassTarget;
    public bool headedPassTargetsPlayer;
    public bool headedPassTargetsSpace;
    public bool headedPassTargetIsOffside;
    public int headedPassInterceptors;
    public float headedPassInterceptionProbability = -1f;
    public float headedPassImmediateSnapshotExpectedGoals = -1f;
    public int headedPassSnapshotPenalty;
    public int headedPassSnapshotBlockers;
    public int headedPassSnapshotGoalkeeperPenalty;
    public string headedPassSpaceCollectorTokenKey;
    public RoomHexCoordinates headedPassSpaceSnapshotHex;
    public int headedPassSpacePickupDistance = -1;
    public int headedPassSpaceRemainingPace = -1;
    public float headedPassSpaceMovementTurnoverProbability = -1f;
    public float headedPassSpaceFoulProbability = -1f;
    public float headedPassSpaceReachSnapshotProbability = -1f;
    public float headedPassSpaceSnapshotExpectedGoals = -1f;
    public float headedPassSpaceRouteExpectedGoals = -1f;
    public int headedPassSpaceRoutesEvaluated;
    public bool isGoalkeeperPositioning;
    public int goalkeeperWallCoveredShotTargets;
    public int goalkeeperWallShotTargetCount;
    public int goalkeeperWallCoverageQuality;
    public float goalkeeperPositioningMaximumExpectedGoals = -1f;
    public float goalkeeperPositioningCurrentMaximumExpectedGoals = -1f;
    public RoomHexCoordinates goalkeeperPositioningWorstTargetHex;
    public string goalkeeperPositioningWorstThreatTokenKey;
    public int goalkeeperPositioningThreatCount;
    public int goalkeeperPositioningOffBallThreatCount;
    public int goalkeeperPositioningBlockerCount;
    public int goalkeeperPositioningCurrentOutfieldBlockerCount;
    public int goalkeeperPositioningGoalkeeperPenalty;
    public bool goalkeeperActorDestinationPreviewed;
    public bool goalkeeperActorBestContinuationIsForfeit;
    public RoomHexCoordinates goalkeeperActorBestDestinationHex;
    public bool isLongBallGoalkeeperRecovery;
    public string longBallRecoveryMode;
    public string longBallAttackingReceiverTokenKey;
    public string longBallRecoveringDefenderTokenKey;
    public int longBallAttackerMovementPhases = -1;
    public int longBallBestDefenderMovementPhases = -1;
    public int longBallGoalkeeperMovementPhases = -1;
    public int longBallGoalkeeperFreePace;
    public int longBallGoalkeeperRemainingMovementPhasesAfterFreeMove = -1;
    public bool longBallGoalkeeperCanReachBallWithFreeMove;
    public bool longBallDefenseClaimsFirst;
    public bool longBallDefenderTouchRecoveryChance;
    public bool longBallFarPostPassThreat;
    public int longBallGoalkeeperDistanceToLanding = -1;
    public int longBallGoalkeeperSupportDistance = -1;
    public RoomHexCoordinates longBallRecommendedGoalkeeperHex;
    public bool isRegularFinalThirdReorganization;
    public int currentSecondDefenderLineX;
    public int proposedSecondDefenderLineX;
    public int offsideLineRaise;
    public bool actorDefinesSecondDefenderLine;
    public int reachableOnsideOpponents;
    public int newlyOnsideOpponents;
    public int opponentsMadeOffside;
    public bool defensiveLineLeavesFullback;
    public bool defensiveLineCreatesGap;
    public bool goalkeeperPositioningWallApplies;
    public string goalkeeperPositioningThreatAction;
    public RoomHexCoordinates goalkeeperPositioningThreatOrigin;
    public int goalkeeperPositioningShootingPenalty;
    public int goalkeeperOwnGoalDistanceDelta;
    public float immediateSnapshotExpectedGoals = -1f;
    public float bestReachableSnapshotExpectedGoals = -1f;
    public float bestReachableSnapshotRawExpectedGoals = -1f;
    public float bestReachableSnapshotReachProbability = -1f;
    public float bestReachableSnapshotTurnoverProbability = -1f;
    public RoomHexCoordinates bestReachableSnapshotHex;
    public int reachableSnapshotHexesEvaluated;
    public string reachableSnapshotExpectedGoalsFrontier;
    public bool isFullPaceDribbleRoute;
    public readonly List<RoomHexCoordinates> fullPaceDribblePath = new();
    public RoomHexCoordinates fullPaceDribbleDestination;
    public int fullPaceDribbleSteps;
    public int fullPaceDribbleProgress;
    public int fullPaceDribbleEndpointPressure = -1;
    public float fullPaceDribbleReachProbability = -1f;
    public float fullPaceDribbleTurnoverProbability = -1f;
    public float fullPaceDribbleFoulProbability = -1f;
    public float fullPaceDribbleExpectedGoals = -1f;
    public int fullPaceDribbleUtility;
    public int fullPaceDribbleRoutesEvaluated;
    public RoomHexCoordinates freeHeaderBestTargetHex;
    public string freeHeaderBestReceiverTokenKey;
    public int freeHeaderTargetsEvaluated;
    public float freeHeaderExpectedPossessionProbability = -1f;
    public float freeHeaderExpectedGoals = -1f;
    public int freeHeaderExpectedValue;
    public int headerControlExpectedValue;
    public bool isTackleDecision;
    public bool tackleIsAttempted;
    public bool tackleIsFromBehind;
    public bool tackleDefenderIsBooked;
    public float tackleSuccessProbability = -1f;
    public float tackleFoulProbability = -1f;
    public float tackleSendOffProbability = -1f;
    public float tackleCardProbability = -1f;
    public float dribblerRetainsProbability = -1f;
    public float dribblerBestRepositionExpectedGoals = -1f;
    public int dribblerBestRepositionProgress;
    public RoomHexCoordinates dribblerBestRepositionHex;
    public bool isSuccessfulTackleReposition;
    public string plannedNextAction;
    public string plannedNextReceiverTokenKey;
    public RoomHexCoordinates plannedNextActionTargetHex;
    public float plannedNextActionValue = -1f;
    public float plannedNextActionExpectedGoals = -1f;
    public int plannedNextActionInterceptors;
    public bool isSnapshotDefensiveMove;
    public float snapshotDefenseMaximumExpectedGoals = -1f;
    public RoomHexCoordinates snapshotDefenseWorstTargetHex;
    public RoomHexCoordinates snapshotDefenseBestMoveHex;
    public int snapshotDefenseBlockerCount;
    public int snapshotDefenseGoalkeeperPenalty;
    public bool snapshotDefenseMovesGoalkeeper;
    public int snapshotDefenseNextStateValue;
    public string defensivePlanRole;
    public string defensivePlanIntent;
    public RoomHexCoordinates defensivePlanDestination;
    public bool defensiveLegalAdjacencyAvailable;
    public bool defensiveMoveEndsAdjacentToBall;
    public bool doubleTeamEvaluated;
    public float doubleTeamExtraTurnoverValue;
    public float doubleTeamExposureValue;
    public bool isAttackingOffBallMove;
    public bool isCommittedAerialDefense;
    public float committedAerialDefenseGain;
    public int committedAerialRecoverySteps;
    public bool supportsConcreteContinuation;
    public string attackingContinuation;
    public float attackingContinuationValue;
    public RoomAIPossessionChainSummary possessionChain;
    public bool hasPossessionLevelPlan;
    public float possessionPlanRetentionProbability = -1f;
    public float possessionPlanReceiverSurvivalProbability = -1f;
    public float possessionPlanTackleLossProbability = -1f;
    public float possessionPlanRecoveryProbability = -1f;
    public float possessionPlanEffectiveRetentionProbability = -1f;
    public bool possessionPlanSupportBlocksTackleWinnerExit;
    public string possessionPlanLikelyTackleWinnerTokenKey;
    public RoomHexCoordinates possessionPlanLikelyTackleWinnerExitHex;
    public string possessionPlanSupportTokenKey;
    public RoomHexCoordinates possessionPlanSupportDestinationHex;
    public int possessionPlanSupportSteps = -1;
    public float possessionPlanThreatExpectedGoals = -1f;
    public int possessionPlanValue;
    public string possessionPlanNextAction;
    public RoomAITokenSnapshot actorToken;
    public RoomAITokenSnapshot targetToken;
}

public sealed class RoomAITokenSnapshot
{
    public string tokenKey;
    public string teamSide;
    public int jerseyNumber;
    public string playerName;
    public RoomHexCoordinates currentHex;
    public bool isPlaying;
    public bool isGoalKeeper;
    public bool isAttacker;
    public bool wasSubbedOn;
    public bool isBooked;
    public bool isSentOff;
    public bool isInjured;
    public bool requiresSubstitution;
    public int pace;
    public int dribbling;
    public int highPass;
    public int resilience;
    public int heading;
    public int shooting;
    public int tackling;
    public int aerial;
    public int saving;
    public int handling;
}

public static class RoomAIDecisionSnapshotBuilder
{
    private const int SnapshotContinuationChallengeLookahead = 3;

    public static RoomAIDecisionSnapshot Build(
        GameplayInstructionSnapshot instruction,
        RoomDecisionContext decisionContext,
        IReadOnlyList<RoomActionCandidate> legalActions,
        bool includeCandidateExplanations = true)
    {
        RoomRuntimeSnapshot runtime = TryCreateRuntimeSnapshot();
        RoomAIDecisionSnapshot snapshot = new RoomAIDecisionSnapshot
        {
            instruction = instruction,
            decisionContext = decisionContext,
            runtime = runtime,
            manager = decisionContext != null ? decisionContext.manager : instruction?.manager,
            expectedTeam = decisionContext != null ? decisionContext.expectedTeam : instruction?.expectedTeam,
            expectedInput = decisionContext != null ? decisionContext.expectedInput : instruction?.expectedInput,
            currentState = runtime?.currentState ?? string.Empty,
            firstAerialOffsideExempt = runtime?.currentState == "GoalKick"
                || MatchManager.Instance?.highPassManager?.IsDirectGoalKick == true,
            teamInAttack = runtime?.teamInAttack ?? string.Empty,
            attackHasPossession = runtime != null && runtime.attackHasPossession,
            homeTeamDirection = runtime?.homeTeamDirection ?? string.Empty,
            awayTeamDirection = runtime?.awayTeamDirection ?? string.Empty,
            ball = runtime?.ball,
            committedAerialTarget = MatchManager.Instance?.highPassManager is { isWaitingForDefenderSelection: true } hp
                ? RoomHexCoordinates.FromHex(hp.currentTargetHex) : null,
            clock = runtime?.clock,
            homeScore = runtime?.stats?.homeTeamStats?.totalGoals ?? 0,
            awayScore = runtime?.stats?.awayTeamStats?.totalGoals ?? 0,
            halfDurationMinutes = MatchManager.Instance?.GetCurrentHalfDurationMinutesForAI() ?? 45,
            regulationHalfCount = MatchManager.Instance?.GetConfiguredHalfCountForAI() ?? 2,
            defensiveMovesRemaining = System.Math.Max(0, (MatchManager.Instance?.movementPhaseManager?.DefendingMoveLimit ?? 5)
                - (MatchManager.Instance?.movementPhaseManager?.defendersMoved ?? 0)),
            freshAttackingMoveLimit = MatchManager.Instance?.movementPhaseManager?.AttackingMoveLimit
                ?? MovementPhaseManager.DefaultAttackingMoveLimit
        };

        if (runtime?.tokens != null)
        {
            foreach (RoomTokenSnapshot token in runtime.tokens)
            {
                if (token != null)
                {
                    snapshot.tokens.Add(CreateTokenSnapshot(token));
                }
            }
        }

        if (legalActions != null)
        {
            foreach (RoomActionCandidate action in legalActions)
            {
                if (action != null)
                {
                    snapshot.legalActions.Add(action);
                }
            }
        }

        snapshot.tacticalMatchup = RoomAITacticalProfileBuilder.Build(snapshot);
        if (includeCandidateExplanations)
        {
            snapshot.defensivePhasePlan = BuildDefensivePhasePlan(snapshot);
            BuildCandidateExplanations(snapshot);
            ApplyDefensivePhasePlanFacts(snapshot);
        }
        return snapshot;
    }

    /// <summary>Cooperative main-thread analysis: one candidate or aerial target
    /// per step. The caller owns the frame budget and cancellation.</summary>
    public static IEnumerator BuildCandidateExplanationsIncrementally(
        RoomAIDecisionSnapshot snapshot, RoomAIHumanShadowCache cache)
    {
        cache ??= new RoomAIHumanShadowCache();
        var defense = BuildDefensivePhasePlanIncrementally(snapshot);
        while (defense.MoveNext()) yield return null;
        var header = snapshot.legalActions.Any(action => action.manager == nameof(HeaderManager))
            ? UnityEngine.Object.FindAnyObjectByType<HeaderManager>() : null;
        var lookup = BuildTokenLookup(snapshot.tokens);
        var boxes = CollectPitchFeatureHexes(snapshot, hex => IsOpponentPenaltyBox(snapshot, hex));
        var shooting = CollectPitchFeatureHexes(snapshot, hex => IsOpponentShootingHex(snapshot, hex));
        foreach (RoomActionCandidate action in snapshot.legalActions)
        {
            string key = RoomAIHumanShadowCache.CandidateKey(action);
            RoomAICandidateExplanation facts;
            if (cache.explanations.TryGetValue(key, out var cached))
                facts = cached.ForCandidate(action);
            else
            {
                facts = CreateCandidateExplanation(snapshot, action, lookup, boxes, shooting,
                    header, deferRegularHighPass: true);
                var carrier = MatchManager.Instance?.ball?.GetCurrentHex()?.GetOccupyingToken();
                if (IsAttackingSupportMove(snapshot, action, carrier)
                    && action.step == RoomDecisionStep.ChooseActor)
                {
                    var lane = PopulateBestSupportingPassLaneForActorIncrementally(snapshot, action, carrier,
                        MatchManager.Instance.ball.GetCurrentHex(), facts);
                    while (lane.MoveNext()) yield return null;
                }
                if (action.actionType is RoomActionType.HighPass or RoomActionType.GoalkeeperKick)
                {
                    var aerial = PopulateRegularHighPassOutcomeFactsIncrementally(snapshot, action,
                        facts, action.targetHex ?? action.targetToken?.GetCurrentHex());
                    while (aerial.MoveNext()) yield return null;
                }
                // An incomplete aerial-target precompute must be reconsidered.
                if (facts != null && (action.actionType != RoomActionType.HighPass
                    && action.actionType != RoomActionType.GoalkeeperKick
                    || facts.highPassTargetEvaluationComplete)) cache.explanations[key] = facts.ForCandidate(action);
            }
            if (facts != null) snapshot.candidateExplanations.Add(facts);
            yield return null;
        }
        var routes = PopulateFullPaceDribbleRouteFactsIncrementally(snapshot);
        while (routes.MoveNext()) yield return null;
        var keeperContinuation = PopulateGoalkeeperContinuationFactsIncrementally(snapshot);
        while (keeperContinuation.MoveNext()) yield return null;
        PopulateSnapshotContinuationComparison(snapshot);
        foreach (var facts in snapshot.candidateExplanations)
        {
            PopulatePossessionLevelPlanFacts(snapshot, new[] { facts });
            yield return null;
        }
        var continuations = PopulateAttackingContinuationFactsIncrementally(snapshot);
        while (continuations.MoveNext()) yield return null;
        ApplyDefensivePhasePlanFacts(snapshot);
    }

    private static RoomAIDefensivePhasePlan BuildDefensivePhasePlan(RoomAIDecisionSnapshot snapshot)
    {
        var work = BuildDefensivePhasePlanIncrementally(snapshot);
        while (work.MoveNext()) { }
        return snapshot.defensivePhasePlan;
    }

    private static IEnumerator BuildDefensivePhasePlanIncrementally(RoomAIDecisionSnapshot snapshot)
    {
        MatchManager match = MatchManager.Instance;
        HexGrid grid = match?.hexGrid;
        bool ftpDefense = snapshot?.manager == nameof(FirstTimePassManager)
            && match?.firstTimePassManager?.isWaitingForDefenderSelection == true;
        if (snapshot != null && snapshot.expectedTeam == snapshot.teamInAttack && !snapshot.attackHasPossession
            && snapshot.currentState == "MovementPhase")
            snapshot.expectedOpponentCollection = OpponentCanCollectBeforeAttacker(snapshot, null, null);
        if (snapshot == null || grid == null || snapshot.ball == null
            || snapshot.expectedTeam == snapshot.teamInAttack && !snapshot.expectedOpponentCollection
            || !ftpDefense && snapshot.currentState != "MovementPhase"
            || !snapshot.legalActions.Any(action => (action.actionType == RoomActionType.StartMovement || ftpDefense)
                && action.step is RoomDecisionStep.ChooseActor or RoomDecisionStep.ChooseTarget)) yield break;
        if (ftpDefense) snapshot.defensiveMovesRemaining = 1;
        var reachable = new Dictionary<string, List<RoomHexCoordinates>>();
        foreach (PlayerToken token in match.playerTokenManager.allTokens)
        {
            if (token == null || !token.isPlaying || token.IsGoalKeeper || token.GetCurrentHex() == null
                || (token.isHomeTeam ? "Home" : "Away") != snapshot.expectedTeam
                || !ftpDefense && (match.movementPhaseManager?.movedTokens.Contains(token) == true
                    || match.movementPhaseManager?.stunnedTokens.Contains(token) == true)
                || ftpDefense && !snapshot.legalActions.Any(action => (action.actor ?? action.targetToken) == token)) continue;
            reachable[GetTokenKey(token)] = GetCachedReachability(snapshot, grid, token.GetCurrentHex(), ftpDefense ? 1 : System.Math.Max(0, token.pace))
                .reachableHexes.Where(hex => hex != null && !hex.isOutOfBounds && !hex.isAttackOccupied && !hex.isDefenseOccupied)
                .Append(token.GetCurrentHex()).Select(RoomHexCoordinates.FromHex).ToList();
            yield return null;
        }
        var threatValues = new Dictionary<string, float>();
        if (match.shotManager != null)
            foreach (PlayerToken opponent in match.playerTokenManager.allTokens.Where(token => token != null
                && token.isPlaying && token.GetCurrentHex() != null && (token.isHomeTeam ? "Home" : "Away") != snapshot.expectedTeam))
            {
                match.shotManager.TryCalculateBestExpectedGoalsPreview(opponent, opponent.GetCurrentHex(), "snapshot",
                    out float xg, out _, out _, out _);
                float passSurvival = 0f;
                if (match.groundBallManager?.ball != null && HexGridUtils.GetHexStepDistance(match.ball.GetCurrentHex(), opponent.GetCurrentHex()) <= 11)
                {
                    var interceptions = GroundPassCommon.BuildOrderedInterceptionCandidates(grid, match.ball, opponent.GetCurrentHex());
                    passSurvival = 1f - ExpectedStatsCalculator.CalculateAtLeastOneRecoveryProbability(interceptions
                        .Where(item => item?.DefenderToken != null).Select(item => item.DefenderToken).Distinct().ToArray());
                }
                // A receiver just outside shooting range still threatens a
                // collection followed by movement/Snapshot. Include aerial and
                // relay access so a backward pass does not erase advanced threats.
                RoomAIPassRacePreview continuation = GetPassRace(snapshot, opponent, opponent.GetCurrentHex(), true);
                float nextThreat = System.Math.Max(System.Math.Max(0f, xg), continuation?.bestScoringRouteExpectedGoals ?? 0f);
                PlayerToken carrier = match.ball.GetCurrentHex().GetOccupyingToken();
                int deliveryDistance = HexGridUtils.GetHexStepDistance(match.ball.GetCurrentHex(), opponent.GetCurrentHex());
                if (carrier != null && carrier.isHomeTeam == opponent.isHomeTeam && deliveryDistance <= 15)
                {
                    float accuracy = System.Math.Max(0f, System.Math.Min(1f, (carrier.highPass - 1) / 6f));
                    bool contestedHeader = match.playerTokenManager.allTokens.Any(token => token != null && token.isPlaying
                        && token.isHomeTeam != opponent.isHomeTeam && token.GetCurrentHex() != null
                        && HexGridUtils.GetHexStepDistance(token.GetCurrentHex(), opponent.GetCurrentHex()) <= 3);
                    passSurvival = System.Math.Max(passSurvival, accuracy * (contestedHeader ? 0.5f : 1f));
                }
                bool hasRelay = snapshot.tokens.Any(token => token.isPlaying && !token.isGoalKeeper
                    && token.teamSide == (opponent.isHomeTeam ? "Home" : "Away") && token.currentHex != null
                    && GetTokenKey(opponent) != token.tokenKey && RoomAITacticalSafety.Distance(snapshot.ball, token.currentHex) <= 11
                    && RoomAITacticalSafety.Distance(token.currentHex, RoomHexCoordinates.FromHex(opponent.GetCurrentHex())) <= 11);
                if (hasRelay) passSurvival = System.Math.Max(passSurvival, 0.15f);
                threatValues[GetTokenKey(opponent)] = nextThreat * passSurvival;
                yield return null;
            }
        foreach (var entry in reachable) snapshot.defensiveMovementDestinations[entry.Key] = entry.Value;
        foreach (var entry in threatValues) snapshot.defensiveThreatValues[entry.Key] = entry.Value;
        var allocation = RoomAITacticalSafety.AssignDefenseIncrementally(snapshot, reachable, threatValues, plan => snapshot.defensivePhasePlan = plan);
        while (allocation.MoveNext()) yield return null;
    }

    public static void ApplyDefensivePhasePlanFacts(RoomAIDecisionSnapshot snapshot)
    {
        RoomAIDefensivePhasePlan plan = snapshot?.defensivePhasePlan;
        if (plan == null)
        {
            return;
        }
        foreach (RoomAICandidateExplanation explanation in snapshot.candidateExplanations)
        {
            if (explanation == null)
            {
                continue;
            }
            explanation.defensivePlanRole = string.Empty;
            explanation.defensivePlanIntent = string.Empty;
            explanation.defensivePlanDestination = null;
            explanation.defensiveLegalAdjacencyAvailable = false;
            explanation.defensiveMoveEndsAdjacentToBall = false;
            string actorKey = explanation.actorTokenKey;
            RoomAIDefensiveAssignment assignment = plan.assignments.FirstOrDefault(item => item.tokenKey == actorKey);
            if (assignment != null)
            {
                explanation.defensivePlanRole = assignment.role;
                explanation.defensivePlanDestination = assignment.destination;
                explanation.defensivePlanIntent = assignment.targetTokenKey ?? assignment.role;
                explanation.defensiveLegalAdjacencyAvailable = plan.legalAdjacencyAvailable;
                explanation.defensiveMoveEndsAdjacentToBall = explanation.targetDistanceToBall == 1;
                PopulateDoubleTeamComparison(snapshot, explanation);
                continue;
            }
            if (string.Equals(actorKey, plan.primaryPressureTokenKey, System.StringComparison.OrdinalIgnoreCase))
            {
                explanation.defensivePlanRole = "PrimaryPressure";
                explanation.defensivePlanDestination = plan.primaryPressureDestination;
                explanation.defensivePlanIntent = plan.primaryPressureIntent;
            }
            else if (string.Equals(actorKey, plan.coverTokenKey, System.StringComparison.OrdinalIgnoreCase))
            {
                explanation.defensivePlanRole = "Cover";
                explanation.defensivePlanDestination = plan.coverDestination;
                explanation.defensivePlanIntent = plan.coverIntent;
            }
            else if (string.Equals(actorKey, plan.outletDenialTokenKey, System.StringComparison.OrdinalIgnoreCase))
            {
                explanation.defensivePlanRole = "OutletDenial";
                explanation.defensivePlanDestination = plan.outletDenialDestination;
                explanation.defensivePlanIntent = string.IsNullOrWhiteSpace(plan.outletTargetTokenKey)
                    ? "DenyOutlet"
                    : $"DenyOutlet:{plan.outletTargetTokenKey}";
            }
            else if (string.Equals(actorKey, plan.defensiveLineTokenKey, System.StringComparison.OrdinalIgnoreCase))
            {
                explanation.defensivePlanRole = "DefensiveLine";
                explanation.defensivePlanDestination = plan.defensiveLineDestination;
                explanation.defensivePlanIntent = "HoldOffsideResponsibility";
            }
            explanation.defensiveLegalAdjacencyAvailable = plan.legalAdjacencyAvailable;
            explanation.defensiveMoveEndsAdjacentToBall = explanation.targetDistanceToBall == 1;
            PopulateDoubleTeamComparison(snapshot, explanation);
        }
    }

    private static void PopulateDoubleTeamComparison(RoomAIDecisionSnapshot snapshot, RoomAICandidateExplanation facts)
    {
        facts.doubleTeamEvaluated = false;
        facts.doubleTeamExtraTurnoverValue = 0f;
        facts.doubleTeamExposureValue = 0f;
        if (!facts.defensiveMoveEndsAdjacentToBall || facts.defensivePlanRole == "PrimaryPressure") return;
        MatchManager match = MatchManager.Instance;
        var primary = match?.playerTokenManager?.allTokens.FirstOrDefault(token => token != null
            && GetTokenKey(token) == snapshot.defensivePhasePlan.primaryPressureTokenKey);
        var secondary = facts.candidate?.actor;
        var carrier = match?.ball?.GetCurrentHex()?.GetOccupyingToken();
        if (primary == null || secondary == null || carrier == null || match.shotManager == null) return;
        if (HexGridUtils.GetHexStepDistance(primary.GetCurrentHex(), carrier.GetCurrentHex()) != 1) return;
        facts.doubleTeamEvaluated = true;
        float primaryWin = ExpectedStatsCalculator.CalculateGroundDuelExpectationWithDefenderSkill(carrier, primary, primary.tackling).xTackles;
        float secondaryWin = ExpectedStatsCalculator.CalculateGroundDuelExpectationWithDefenderSkill(carrier, secondary, secondary.tackling).xTackles;
        match.shotManager.TryCalculateBestExpectedGoalsPreview(carrier, carrier.GetCurrentHex(), "snapshot", out float threat, out _, out _, out _);
        facts.doubleTeamExtraTurnoverValue = (1f - primaryWin) * secondaryWin * System.Math.Max(0f, threat);
        foreach (var opponent in match.playerTokenManager.allTokens.Where(token => token != null && token.isPlaying
            && token != carrier && token.isHomeTeam == carrier.isHomeTeam && token.GetCurrentHex() != null))
        {
            bool covered = match.playerTokenManager.allTokens.Any(token => token != null && token.isPlaying
                && token != secondary && token.isHomeTeam != carrier.isHomeTeam && token.GetCurrentHex() != null
                && HexGridUtils.GetHexStepDistance(token.GetCurrentHex(), opponent.GetCurrentHex()) <= 1);
            if (covered || HexGridUtils.GetHexStepDistance(carrier.GetCurrentHex(), opponent.GetCurrentHex()) > 11) continue;
            match.shotManager.TryCalculateBestExpectedGoalsPreview(opponent, opponent.GetCurrentHex(), "snapshot", out float xg, out _, out _, out _);
            facts.doubleTeamExposureValue = System.Math.Max(facts.doubleTeamExposureValue, System.Math.Max(0f, xg));
        }
    }


    private static void BuildCandidateExplanations(RoomAIDecisionSnapshot snapshot)
    {
        if (snapshot == null || snapshot.legalActions.Count == 0)
        {
            return;
        }

        Dictionary<string, RoomAITokenSnapshot> tokenLookup = BuildTokenLookup(snapshot.tokens);
        List<HexCell> opponentPenaltyBoxHexes = CollectPitchFeatureHexes(
            snapshot,
            hex => IsOpponentPenaltyBox(snapshot, hex));
        List<HexCell> opponentShootingHexes = CollectPitchFeatureHexes(
            snapshot,
            hex => IsOpponentShootingHex(snapshot, hex));
        HeaderManager headerManager = null;
        for (int index = 0; index < snapshot.legalActions.Count; index++)
        {
            if (string.Equals(
                snapshot.legalActions[index]?.manager,
                nameof(HeaderManager),
                System.StringComparison.Ordinal))
            {
                headerManager = UnityEngine.Object.FindAnyObjectByType<HeaderManager>();
                break;
            }
        }

        foreach (RoomActionCandidate action in snapshot.legalActions)
        {
            RoomAICandidateExplanation explanation = CreateCandidateExplanation(
                snapshot,
                action,
                tokenLookup,
                opponentPenaltyBoxHexes,
                opponentShootingHexes,
                headerManager);
            if (explanation != null)
            {
                snapshot.candidateExplanations.Add(explanation);
            }
        }

        PopulateFullPaceDribbleRouteFacts(snapshot);
        var keeperContinuation = PopulateGoalkeeperContinuationFactsIncrementally(snapshot);
        while (keeperContinuation.MoveNext()) { }

        PopulateSnapshotContinuationComparison(snapshot);
        PopulatePossessionLevelPlanFacts(snapshot);
        PopulateAttackingContinuationFacts(snapshot);
    }

    private static IEnumerator PopulateGoalkeeperContinuationFactsIncrementally(RoomAIDecisionSnapshot snapshot)
    {
        var match = MatchManager.Instance;
        if (snapshot.manager != nameof(GoalKeeperManager) || match?.shotManager == null
            || match.shotManager.isActivated || match.highPassManager?.isActivated == true) yield break;
        var carrier = match.ball?.GetCurrentHex()?.GetOccupyingToken();
        var keeper = match.hexGrid?.GetDefendingGK();
        if (carrier == null || keeper == null || carrier.isHomeTeam == keeper.isHomeTeam) yield break;
        int side = (carrier.isHomeTeam ? match.homeTeamDirection : match.awayTeamDirection)
            == MatchManager.TeamAttackingDirection.LeftToRight ? 1 : -1;
        int pace = match.movementPhaseManager?.selectedToken == carrier
            ? System.Math.Max(0, match.movementPhaseManager.remainingDribblerPace) : 0;
        var origins = GetCachedReachability(snapshot, match.hexGrid, carrier.GetCurrentHex(), pace).reachableHexes
            .Where(hex => hex.ShootingPaths?.Keys.Any(goal => goal != null && goal.isInGoal == side) == true)
            .OrderByDescending(hex => hex.coordinates.x * side).ThenBy(hex => System.Math.Abs(hex.coordinates.z))
            .Take(3).Append(carrier.GetCurrentHex()).Distinct()
            .Where(hex => hex.ShootingPaths?.Keys.Any(goal => goal != null && goal.isInGoal == side) == true).ToList();
        if (origins.Count == 0) yield break;
        var results = new Dictionary<HexCell, float>();
        foreach (var facts in snapshot.candidateExplanations.Where(facts => facts.isGoalkeeperPositioning))
        {
            var start = facts.candidate.targetHex ?? keeper.GetCurrentHex();
            if (!results.TryGetValue(start, out float worst))
            {
                worst = 0f;
                var vacated = new HashSet<HexCell> { keeper.GetCurrentHex() };
                foreach (var origin in origins)
                {
                    var paths = origin.ShootingPaths?.Where(pair => pair.Key != null && pair.Key.isInGoal == side)
                        .Select(pair => pair.Value).ToList();
                    if (paths == null || paths.Count == 0) continue;
                    float best = 1f;
                    var future = HexGridUtils.GetReachableHexes(match.hexGrid, start, ShotManager.SnapshotDefenderMovementRange, vacated)
                        .reachableHexes.Where(hex => !hex.isOutOfBounds && hex != origin
                            && (hex.GetOccupyingToken() == null || hex.GetOccupyingToken() == keeper))
                        .OrderByDescending(hex => paths.Count(path => path.Contains(hex) || path.Any(cell => HexGridUtils.GetHexStepDistance(cell, hex) == 1)))
                        .ThenBy(hex => HexGridUtils.GetHexStepDistance(start, hex)).Take(6).Append(start).Distinct();
                    foreach (var destination in future)
                    {
                        yield return null;
                        string key = $"box-then-snapshot|{origin.GetEntityId()}|{destination.GetEntityId()}";
                        if (!snapshot.expectedGoalsCache.TryGetValue(key, out var preview))
                        {
                            preview = new RoomAIExpectedGoalsPreview();
                            preview.calculated = match.shotManager.TryCalculateBestExpectedGoalsAfterGoalkeeperPositioning(
                                carrier, origin, keeper, destination, out preview.expectedGoals, out _, out _, out _, out _);
                            snapshot.expectedGoalsCache[key] = preview;
                        }
                        if (preview.calculated) best = System.Math.Min(best, preview.expectedGoals);
                    }
                    worst = System.Math.Max(worst, best);
                }
                results[start] = worst;
            }
            facts.goalkeeperPositioningMaximumExpectedGoals = worst;
            facts.goalkeeperPositioningThreatAction = "Box move then legal two-hex Snapshot response across bounded carrier endpoints";
        }
        if (results.TryGetValue(keeper.GetCurrentHex(), out float baseline))
            foreach (var facts in snapshot.candidateExplanations.Where(facts => facts.isGoalkeeperPositioning))
                facts.goalkeeperPositioningCurrentMaximumExpectedGoals = baseline;
    }

    private static void PopulateAttackingContinuationFacts(RoomAIDecisionSnapshot snapshot)
    {
        var work = PopulateAttackingContinuationFactsIncrementally(snapshot);
        while (work.MoveNext()) { }
    }

    private static IEnumerator PopulateAttackingContinuationFactsIncrementally(RoomAIDecisionSnapshot snapshot)
    {
        MatchManager match = MatchManager.Instance;
        if (match?.hexGrid == null || !snapshot.attackHasPossession || snapshot.expectedTeam != snapshot.teamInAttack) yield break;
        var carrier = match.ball?.GetCurrentHex()?.GetOccupyingToken() ?? match.LastTokenToTouchTheBallOnPurpose;
        if (carrier == null) yield break;
        if (!snapshot.candidateExplanations.Any(facts => facts.candidate?.actor != null
            && facts.candidate.actor != carrier && facts.candidate.actionType is RoomActionType.StartMovement or RoomActionType.FinalThird)) yield break;
        var preparation = BuildPreparedAttackSupport(snapshot);
        while (preparation.MoveNext()) yield return null;
        RoomAIAerialReleasePlan aerial = RoomAIAerialReleasePlan.Build(snapshot);
        float baselineExposure = RoomAITacticalSafety.CounterExposure(snapshot, null, null);
        foreach (var facts in snapshot.candidateExplanations)
        {
            yield return null;
            var action = facts.candidate;
            var actor = action?.actor;
            if (actor == null || actor == carrier || actor.IsGoalKeeper || actor.isHomeTeam != carrier.isHomeTeam
                || action.actionType != RoomActionType.StartMovement && action.actionType != RoomActionType.FinalThird
                || action.step != RoomDecisionStep.ChooseActor && action.step != RoomDecisionStep.ChooseTarget) continue;
            facts.isAttackingOffBallMove = true;
            HexCell destination = action.targetHex;
            if (destination == null && facts.groundPassContinuationTargetHex != null)
                destination = match.hexGrid.GetHexCellAt(new UnityEngine.Vector3Int(facts.groundPassContinuationTargetHex.x, 0, facts.groundPassContinuationTargetHex.z), false);
            if (facts.isSupportingPassLaneMove && destination != null)
            {
                if (facts.supportingPassLaneValueGain > 1f)
                {
                    facts.supportsConcreteContinuation = true;
                    facts.attackingContinuation = $"Improves Standard Pass outlet to {facts.actorTokenKey} at {destination.coordinates}: "
                        + $"retention={facts.groundPassRetentionProbability:0.000}, route xG={facts.groundPassRouteExpectedGoals:0.000}, "
                        + $"progress={facts.forwardProgressDelta}, continuation gain={facts.supportingPassLaneValueGain:0.0}; reassess after intervening moves";
                    facts.attackingContinuationValue = facts.supportingPassLaneValueGain / 120f;
                    if (action.step == RoomDecisionStep.ChooseActor) facts.targetHex = RoomHexCoordinates.FromHex(destination);
                }
            }
            float tackleRecoveryValue = RoomAITacticalSafety.RecoverySupportNetGain(snapshot, facts.actorToken,
                RoomHexCoordinates.FromHex(destination), facts.possessionPlanRecoveryProbability, baselineExposure);
            if (facts.possessionPlanSupportBlocksTackleWinnerExit && tackleRecoveryValue > facts.attackingContinuationValue
                && facts.possessionPlanSupportTokenKey == facts.actorTokenKey
                && RoomAITacticalSafety.Distance(RoomHexCoordinates.FromHex(destination), facts.possessionPlanSupportDestinationHex) <= 1)
            {
                facts.supportsConcreteContinuation = true;
                facts.attackingContinuation = $"Protects possession after a lost tackle: recovery={facts.possessionPlanRecoveryProbability:0.000}, "
                    + $"value after additional counterattack exposure={tackleRecoveryValue:0.000}";
                facts.attackingContinuationValue = tackleRecoveryValue;
            }
            var supportHexes = GetAttackingSupportDestinations(snapshot, action).ToList();
            if (!facts.supportsConcreteContinuation && snapshot.preparedShooter != null && actor != snapshot.preparedShooter)
            {
                float ReboundValue(HexCell hex) => snapshot.preparedRebounds.Sum(branch => branch.probability
                    * System.Math.Max(0f, 1f - RoomAITacticalSafety.Distance(RoomHexCoordinates.FromHex(hex), branch.hex) / (float)System.Math.Max(1, actor.pace)));
                float oldRebounds = ReboundValue(actor.GetCurrentHex());
                var preparationMove = supportHexes.Select(hex => new { hex,
                    value = baselineExposure - RoomAITacticalSafety.CounterExposure(snapshot, facts.actorTokenKey, RoomHexCoordinates.FromHex(hex))
                        + ReboundValue(hex) - oldRebounds }).OrderByDescending(item => item.value).FirstOrDefault();
                if (preparationMove?.value > .01f)
                {
                    facts.supportsConcreteContinuation = true;
                    facts.attackingContinuationValue = System.Math.Max(facts.attackingContinuationValue, preparationMove.value);
                    facts.attackingContinuation = $"Prepared Snapshot retained for {GetTokenKey(snapshot.preparedShooter)}; "
                        + $"reorganise the unit / cover uncertain rebound routes, gain={preparationMove.value:0.000}";
                    if (action.step == RoomDecisionStep.ChooseActor) facts.targetHex = RoomHexCoordinates.FromHex(preparationMove.hex);
                }
            }
            var recoveryMove = supportHexes.Select(hex => new { hex, gain = RoomAITacticalSafety.RecoverySupportNetGain(
                    snapshot, facts.actorToken, RoomHexCoordinates.FromHex(hex), RoomAITacticalSafety.RecoverySupportGain(
                        snapshot, facts.actorToken, RoomHexCoordinates.FromHex(hex)), baselineExposure) })
                .Where(item => item.gain > 0f).OrderByDescending(item => item.gain)
                .ThenBy(item => HexGridUtils.GetHexStepDistance(actor.GetCurrentHex(), item.hex)).FirstOrDefault();
            if (!facts.supportsConcreteContinuation && recoveryMove != null)
            {
                facts.supportsConcreteContinuation = true;
                facts.attackingContinuation = $"Recovery support within the next-action horizon: {facts.actorTokenKey}, "
                    + $"value after additional counterattack exposure={recoveryMove.gain:0.000}";
                facts.attackingContinuationValue = recoveryMove.gain;
                if (action.step == RoomDecisionStep.ChooseActor) facts.targetHex = RoomHexCoordinates.FromHex(recoveryMove.hex);
            }
            if (aerial == null || action.step != RoomDecisionStep.ChooseActor) continue;
            RoomHexCoordinates goal = facts.actorTokenKey == aerial.headerTokenKey ? aerial.contestHex
                : facts.actorTokenKey == aerial.runnerTokenKey ? aerial.runnerHex : null;
            if (goal == null) continue;
            HexCell best = supportHexes
                .OrderBy(hex => RoomAITacticalSafety.Distance(RoomHexCoordinates.FromHex(hex), goal))
                .ThenBy(hex => HexGridUtils.GetHexStepDistance(actor.GetCurrentHex(), hex)).FirstOrDefault();
            if (best != null)
            {
                facts.aerialPreparationHex = RoomHexCoordinates.FromHex(best);
                // Do not overwrite a proven ground/recovery destination with a
                // speculative aerial run. Both routes remain available.
                if (!facts.supportsConcreteContinuation) facts.targetHex = facts.aerialPreparationHex;
            }
        }
    }

    private static IEnumerator BuildPreparedAttackSupport(RoomAIDecisionSnapshot snapshot)
    {
        var match = MatchManager.Instance;
        var carrier = match?.ball?.GetCurrentHex()?.GetOccupyingToken();
        if (carrier == null || match.shotManager == null) yield break;
        float best = .15f;
        foreach (var shooter in match.playerTokenManager.allTokens.Where(token => token != null && token.isPlaying
            && token.isHomeTeam == carrier.isHomeTeam && !token.IsGoalKeeper && token.GetCurrentHex() != null && token.shooting >= 4))
        {
            yield return null;
            if (HexGridUtils.GetHexStepDistance(match.ball.GetCurrentHex(), shooter.GetCurrentHex()) > 11) continue;
            HexCell origin = shooter.GetCurrentHex();
            float value = 0f;
            if (origin.CanShootFrom)
                TryGetCachedExpectedGoalsPreview(snapshot, match.shotManager, shooter, origin, "snapshot", out value, out _, out _, out _);
            else continue; // A possible future dribble is not an established Snapshot.
            if (value <= best || origin == null) continue;
            if (shooter != carrier)
            {
                if (IsTokenOffsideAtHex(snapshot, shooter, shooter.GetCurrentHex())
                    || !GroundPassCommon.ValidateStandardPassPath(match.hexGrid, match.ball,
                        shooter.GetCurrentHex(), 11, logFailures: false).IsValid) continue;
                var interceptions = GroundPassCommon.BuildOrderedInterceptionCandidates(match.hexGrid, match.ball, shooter.GetCurrentHex());
                float retention = 1f - ExpectedStatsCalculator.CalculateAtLeastOneRecoveryProbability(interceptions
                    .Where(item => item?.DefenderToken != null).Select(item => item.DefenderToken).Distinct().ToArray());
                if (retention < .67f) continue;
                value *= retention;
            }
            if (value <= best) continue;
            best = value; snapshot.preparedShooter = shooter; snapshot.preparedShotOrigin = origin;
        }
        if (snapshot.preparedShooter == null) yield break;
        var board = match.playerTokenManager.allTokens.Where(token => token != null && token.isPlaying && token.GetCurrentHex() != null)
            .ToDictionary(token => token, token => token.GetCurrentHex());
        board[snapshot.preparedShooter] = snapshot.preparedShotOrigin;
        if (!match.shotManager.TryCalculateFutureShotResolutionPreview(snapshot.preparedShooter, snapshot.preparedShotOrigin,
            "snapshot", board, null, out var resolution)) yield break;
        // Marginal coverage over plausible save/block origins and rolled routes;
        // never pretend that one particular rebound hex is guaranteed.
        foreach (var rebound in resolution.rebounds.OrderByDescending(item => item.probability).Take(4))
        {
            for (int direction = 0; direction < 6; direction++)
            for (int distance = 1; distance <= 6; distance++)
            {
                var landing = match.looseBallManager.CalculateFinalInaccuracyTarget(rebound.origin, direction, distance).FinalHex;
                if (landing != null && !landing.isOutOfBounds && landing.isInGoal == 0)
                    snapshot.preparedRebounds.Add((RoomHexCoordinates.FromHex(landing), rebound.probability / 36f));
            }
            yield return null;
        }
    }

    public static void PopulatePossessionLevelPlanFacts(RoomAIDecisionSnapshot snapshot,
        IEnumerable<RoomAICandidateExplanation> candidates = null)
    {
        if (snapshot == null || !snapshot.attackHasPossession
            || !string.Equals(snapshot.expectedTeam, snapshot.teamInAttack, System.StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        foreach (RoomAICandidateExplanation explanation in candidates ?? snapshot.candidateExplanations)
        {
            if (explanation?.candidate == null
                || explanation.candidate.actionType is RoomActionType.Nutmeg or RoomActionType.FreeKick or RoomActionType.GoalkeeperSave or RoomActionType.Decline
                    or RoomActionType.Confirm
                    or RoomActionType.Roll
                    or RoomActionType.SetupMove)
            {
                continue;
            }
            if (explanation.actorToken?.isGoalKeeper == true
                && RoomAITacticalSafety.Distance(explanation.actorHex, snapshot.ball) != 0)
                continue; // Moving a keeper off the ball does not create possession.

            float retention = explanation.possessionChain != null
                ? explanation.possessionChain.goalProbability
                    + explanation.possessionChain.attackRetainsPossessionProbability
                    + (explanation.possessionChain.attackingRestartProbability * 0.65f)
                : FirstDefinedProbability(
                    explanation.groundPassRetentionProbability,
                    explanation.highPassExpectedPossessionProbability,
                    explanation.estimatedBallRetentionProbability,
                    explanation.fullPaceDribbleReachProbability);
            retention = UnityEngine.Mathf.Clamp01(retention);
            float tackleLoss = explanation.opponentsWithinTackleReach <= 0
                ? 0f
                : UnityEngine.Mathf.Clamp01(
                    0.5f + (explanation.tackleToDribblingMismatch * 0.08f)
                    + ((explanation.opponentsWithinTackleReach - 1) * 0.12f));
            float survival = 1f - tackleLoss;
            PopulatePossessionTackleSupportPlan(snapshot, explanation);
            int supportCount = string.IsNullOrWhiteSpace(explanation.possessionPlanSupportTokenKey) ? 0 : 1;
            bool supportBlocksWinnerExit = explanation.possessionPlanSupportBlocksTackleWinnerExit;
            float recovery = supportBlocksWinnerExit
                ? UnityEngine.Mathf.Clamp01(
                    0.18f
                    + (System.Math.Max(0, 2 - explanation.possessionPlanSupportSteps) * 0.06f)
                    - ((explanation.opponentsWithinTackleReach - 1) * 0.08f))
                : 0f;
            float effectiveRetention = UnityEngine.Mathf.Clamp01(
                retention * (survival + (tackleLoss * recovery)));
            float threat = System.Math.Max(
                0f,
                System.Math.Max(
                    explanation.expectedGoals,
                    System.Math.Max(
                        explanation.groundPassRouteExpectedGoals,
                        System.Math.Max(
                            explanation.firstTimePassCombinedExpectedGoals,
                            System.Math.Max(
                                explanation.fullPaceDribbleExpectedGoals,
                                explanation.highPassTargetOutcomeValue > 0f
                                    ? explanation.highPassTargetOutcomeValue / 1000f
                                    : -1f)))));
            int value = (int)System.Math.Round(
                (effectiveRetention * 420f)
                + (survival * 140f)
                + (threat * 900f)
                + (explanation.forwardProgressDelta * 14f)
                + (supportCount * 16f)
                + (supportBlocksWinnerExit ? recovery * 120f : 0f)
                - (explanation.opponentsWithinTackleReach * 42f));

            explanation.hasPossessionLevelPlan = true;
            explanation.possessionPlanRetentionProbability = retention;
            explanation.possessionPlanReceiverSurvivalProbability = survival;
            explanation.possessionPlanTackleLossProbability = tackleLoss;
            explanation.possessionPlanRecoveryProbability = recovery;
            explanation.possessionPlanEffectiveRetentionProbability = effectiveRetention;
            explanation.possessionPlanSupportBlocksTackleWinnerExit = supportBlocksWinnerExit;
            explanation.possessionPlanThreatExpectedGoals = threat;
            explanation.possessionPlanValue = value;
            explanation.possessionPlanNextAction = !string.IsNullOrWhiteSpace(explanation.plannedNextAction)
                ? explanation.plannedNextAction
                : explanation.firstTimePassCombinedExpectedGoals > 0f
                    ? "movement then Snapshot"
                    : explanation.groundPassCreatesReachableShot
                        ? "receiver movement then Snapshot"
                        : explanation.targetCanShootFrom
                            ? "Snapshot"
                            : explanation.targetCanHeadFrom
                                ? "Header"
                                : explanation.advancesTowardOpponentGoal
                                    ? "continue progression"
                                    : "retain shape";
        }
    }

    private static void PopulatePossessionTackleSupportPlan(
        RoomAIDecisionSnapshot snapshot,
        RoomAICandidateExplanation explanation)
    {
        if (snapshot?.tokens == null
            || explanation == null
            || explanation.opponentsWithinTackleReach <= 0)
        {
            return;
        }

        RoomHexCoordinates receiverHex = explanation.targetHex
            ?? explanation.targetTokenHex
            ?? explanation.highPassBestTargetHex
            ?? explanation.groundPassContinuationTargetHex
            ?? explanation.fullPaceDribbleDestination;
        if (receiverHex == null)
        {
            return;
        }

        RoomAITokenSnapshot receiver = ResolvePossessionReceiver(snapshot, explanation, receiverHex);
        string attackingTeam = snapshot.expectedTeam;
        RoomAITokenSnapshot tackleWinner = snapshot.tokens
            .Where(token => token != null
                && token.isPlaying
                && token.currentHex != null
                && !string.Equals(token.teamSide, attackingTeam, System.StringComparison.OrdinalIgnoreCase)
                && CalculateSnapshotHexDistance(token.currentHex, receiverHex) <= System.Math.Max(0, token.pace) + 1)
            .OrderByDescending(token => token.tackling)
            .ThenBy(token => CalculateSnapshotHexDistance(token.currentHex, receiverHex))
            .ThenBy(token => token.tokenKey, System.StringComparer.Ordinal)
            .FirstOrDefault();
        if (tackleWinner == null)
        {
            return;
        }

        RoomHexCoordinates exitHex = FindLikelyTackleWinnerExit(snapshot, receiverHex, tackleWinner.teamSide);
        if (exitHex == null)
        {
            return;
        }

        RoomAITokenSnapshot supporter = snapshot.tokens
            .Where(token => token != null
                && token.isPlaying
                && !token.isGoalKeeper
                && token.currentHex != null
                && string.Equals(token.teamSide, attackingTeam, System.StringComparison.OrdinalIgnoreCase)
                && !string.Equals(token.tokenKey, receiver?.tokenKey, System.StringComparison.OrdinalIgnoreCase)
                && CalculateSnapshotHexDistance(token.currentHex, exitHex) <= System.Math.Max(0, token.pace))
            .OrderBy(token => CalculateSnapshotHexDistance(token.currentHex, exitHex))
            .ThenByDescending(token => token.tackling)
            .ThenByDescending(token => token.resilience)
            .ThenBy(token => token.tokenKey, System.StringComparer.Ordinal)
            .FirstOrDefault();

        explanation.possessionPlanLikelyTackleWinnerTokenKey = tackleWinner.tokenKey;
        explanation.possessionPlanLikelyTackleWinnerExitHex = CloneCoordinates(exitHex);
        if (supporter == null)
        {
            return;
        }

        explanation.possessionPlanSupportTokenKey = supporter.tokenKey;
        explanation.possessionPlanSupportDestinationHex = CloneCoordinates(exitHex);
        explanation.possessionPlanSupportSteps = CalculateSnapshotHexDistance(supporter.currentHex, exitHex);
        explanation.possessionPlanSupportBlocksTackleWinnerExit = true;
    }

    private static RoomAITokenSnapshot ResolvePossessionReceiver(
        RoomAIDecisionSnapshot snapshot,
        RoomAICandidateExplanation explanation,
        RoomHexCoordinates receiverHex)
    {
        string receiverKey = !string.IsNullOrWhiteSpace(explanation.targetTokenKey)
            ? explanation.targetTokenKey
            : !string.IsNullOrWhiteSpace(explanation.groundPassCollectorTokenKey)
                ? explanation.groundPassCollectorTokenKey
                : !string.IsNullOrWhiteSpace(explanation.highPassBestCollectorTokenKey)
                    ? explanation.highPassBestCollectorTokenKey
                    : explanation.actorTokenKey;
        return snapshot.tokens.FirstOrDefault(token => token != null
                && string.Equals(token.tokenKey, receiverKey, System.StringComparison.OrdinalIgnoreCase))
            ?? snapshot.tokens.FirstOrDefault(token => token != null
                && string.Equals(token.teamSide, snapshot.expectedTeam, System.StringComparison.OrdinalIgnoreCase)
                && CalculateSnapshotHexDistance(token.currentHex, receiverHex) == 0);
    }

    private static RoomHexCoordinates FindLikelyTackleWinnerExit(
        RoomAIDecisionSnapshot snapshot,
        RoomHexCoordinates turnoverHex,
        string tackleWinnerTeam)
    {
        HexGrid grid = MatchManager.Instance?.hexGrid;
        HexCell turnoverCell = grid?.GetHexCellAt(
            new UnityEngine.Vector3Int(turnoverHex.x, 0, turnoverHex.z),
            false);
        IEnumerable<RoomHexCoordinates> exits = turnoverCell != null
            ? turnoverCell.GetNeighbors(grid)
                .Where(hex => hex != null && !hex.isOutOfBounds)
                .Select(RoomHexCoordinates.FromHex)
            : CreateAdjacentCoordinates(turnoverHex);
        bool leftToRight = string.Equals(tackleWinnerTeam, "Home", System.StringComparison.OrdinalIgnoreCase)
            ? string.Equals(snapshot.homeTeamDirection, "LeftToRight", System.StringComparison.OrdinalIgnoreCase)
            : string.Equals(snapshot.awayTeamDirection, "LeftToRight", System.StringComparison.OrdinalIgnoreCase);
        return exits
            .Where(hex => hex != null && !IsOccupiedSnapshotHex(snapshot, hex))
            .OrderByDescending(hex => leftToRight ? hex.x - turnoverHex.x : turnoverHex.x - hex.x)
            .ThenBy(hex => System.Math.Abs(hex.z))
            .FirstOrDefault();
    }

    private static IEnumerable<RoomHexCoordinates> CreateAdjacentCoordinates(RoomHexCoordinates center)
    {
        bool evenColumn = (center.x & 1) == 0;
        yield return new RoomHexCoordinates { x = center.x + 1, z = center.z };
        yield return new RoomHexCoordinates { x = center.x - 1, z = center.z };
        yield return new RoomHexCoordinates { x = center.x, z = center.z + 1 };
        yield return new RoomHexCoordinates { x = center.x, z = center.z - 1 };
        yield return new RoomHexCoordinates { x = center.x + 1, z = center.z + (evenColumn ? -1 : 1) };
        yield return new RoomHexCoordinates { x = center.x - 1, z = center.z + (evenColumn ? -1 : 1) };
    }

    private static bool IsOccupiedSnapshotHex(RoomAIDecisionSnapshot snapshot, RoomHexCoordinates hex)
    {
        return snapshot.tokens.Any(token => token?.isPlaying == true
            && CalculateSnapshotHexDistance(token.currentHex, hex) == 0);
    }

    private static int CalculateSnapshotHexDistance(RoomHexCoordinates from, RoomHexCoordinates to)
    {
        if (from == null || to == null)
        {
            return int.MaxValue;
        }

        return HexGridUtils.GetHexStepDistance(
            new UnityEngine.Vector3Int(from.x, 0, from.z),
            new UnityEngine.Vector3Int(to.x, 0, to.z));
    }

    private static RoomHexCoordinates CloneCoordinates(RoomHexCoordinates source)
    {
        return source == null ? null : new RoomHexCoordinates { x = source.x, z = source.z };
    }

    private static float FirstDefinedProbability(params float[] probabilities)
    {
        foreach (float probability in probabilities)
        {
            if (probability >= 0f)
            {
                return probability;
            }
        }
        return 0.5f;
    }

    private static void PopulateSnapshotContinuationComparison(RoomAIDecisionSnapshot snapshot)
    {
        if (snapshot == null
            || string.IsNullOrWhiteSpace(snapshot.manager)
            || !snapshot.manager.Contains(nameof(MovementPhaseManager)))
        {
            return;
        }

        RoomAICandidateExplanation snapshotShot = null;
        RoomAICandidateExplanation continuation = null;
        foreach (RoomAICandidateExplanation explanation in snapshot.candidateExplanations)
        {
            if (explanation?.candidate?.actionType == RoomActionType.Shot)
            {
                snapshotShot = explanation;
            }
            else if (explanation?.candidate?.actionType == RoomActionType.ContinueWithoutShot)
            {
                continuation = explanation;
            }
        }

        if (snapshotShot == null || continuation == null || snapshotShot.expectedGoals < 0f)
        {
            return;
        }

        MatchManager matchManager = MatchManager.Instance;
        MovementPhaseManager movement = matchManager?.movementPhaseManager;
        PlayerToken ballCarrier = movement?.selectedToken ?? matchManager?.LastTokenToTouchTheBallOnPurpose;
        HexCell ballHex = matchManager?.ball?.GetCurrentHex();
        if (matchManager == null || ballCarrier == null || ballHex == null)
        {
            return;
        }

        const float fullShotExpectedGoalsBonus = 0.16f;
        float fullShotExpectedGoals = System.Math.Min(
            1f,
            snapshotShot.expectedGoals + fullShotExpectedGoalsBonus);
        float retentionProbability = EstimateBallRetentionProbability(
            matchManager,
            movement?.goalKeeperManager,
            ballCarrier,
            ballHex,
            out bool goalkeeperCanChallenge,
            out float foulProbability,
            out bool likelyChallengerIsBooked,
            out bool likelyChallengeIsDangerous,
            out int forecastChallengeCount,
            out string forecastChallengeSequence);
        float penaltyExpectedGoals = CalculateBestPenaltyExpectedGoals(
            matchManager.hexGrid,
            movement?.goalKeeperManager,
            ballHex);
        float penaltyProbability = penaltyExpectedGoals >= 0f ? foulProbability : 0f;
        bool timeAllowsFollowUpShot = snapshot.clock == null
            || !snapshot.clock.isHalfExpired
            || !snapshot.clock.extraActionsDetermined
            || snapshot.clock.extraActionsRemaining > 1;
        float directShotContinuationExpectedGoals = timeAllowsFollowUpShot
            ? System.Math.Min(
                1f,
                (fullShotExpectedGoals * retentionProbability)
                    + (penaltyExpectedGoals * penaltyProbability))
            : 0f;
        float walkInExpectedGoals = EstimateBestMultiPhaseWalkInExpectedGoals(
            snapshot,
            matchManager,
            movement,
            ballCarrier,
            ballHex,
            retentionProbability,
            out int movementPhasesToWalkIn);
        bool walkInRouteHasTime = HasTimeForWalkInRoute(snapshot.clock, movementPhasesToWalkIn);
        if (!walkInRouteHasTime)
        {
            walkInExpectedGoals = 0f;
        }

        bool prefersWalkInRoute = walkInExpectedGoals >= 0f
            && walkInExpectedGoals > directShotContinuationExpectedGoals
            && walkInExpectedGoals > snapshotShot.expectedGoals;
        bool pursuesSecondBooking = prefersWalkInRoute
            && ballCarrier.dribbling >= 4
            && likelyChallengerIsBooked
            && movementPhasesToWalkIn > 0;
        float bookingOnFoulProbability = likelyChallengeIsDangerous
            ? 1f
            : CalculateBookingProbability(matchManager.refereeLeniency);
        float secondBookingProbability = pursuesSecondBooking
            ? foulProbability * bookingOnFoulProbability
            : 0f;
        float continuationExpectedGoals = System.Math.Max(
            directShotContinuationExpectedGoals,
            walkInExpectedGoals);

        snapshotShot.isSnapshotDecision = true;
        snapshotShot.timeAllowsFollowUpShot = timeAllowsFollowUpShot;
        snapshotShot.goalkeeperCanChallengeBeforeFullShot = goalkeeperCanChallenge;
        snapshotShot.fullShotExpectedGoals = fullShotExpectedGoals;
        snapshotShot.estimatedBallRetentionProbability = retentionProbability;
        snapshotShot.estimatedPenaltyProbability = penaltyProbability;
        snapshotShot.penaltyExpectedGoals = penaltyExpectedGoals;
        snapshotShot.likelyChallengerIsBooked = likelyChallengerIsBooked;
        snapshotShot.likelyChallengeIsDangerous = likelyChallengeIsDangerous;
        snapshotShot.pursuesSecondBooking = pursuesSecondBooking;
        snapshotShot.secondBookingProbability = secondBookingProbability;
        snapshotShot.estimatedMovementPhasesToWalkIn = movementPhasesToWalkIn;
        snapshotShot.estimatedWalkInExpectedGoals = walkInExpectedGoals;
        snapshotShot.forecastChallengeCount = forecastChallengeCount;
        snapshotShot.forecastChallengeSequence = forecastChallengeSequence;

        continuation.isSnapshotDecision = true;
        continuation.timeAllowsFollowUpShot = timeAllowsFollowUpShot;
        continuation.goalkeeperCanChallengeBeforeFullShot = goalkeeperCanChallenge;
        continuation.fullShotExpectedGoals = fullShotExpectedGoals;
        continuation.estimatedBallRetentionProbability = retentionProbability;
        continuation.estimatedPenaltyProbability = penaltyProbability;
        continuation.penaltyExpectedGoals = penaltyExpectedGoals;
        continuation.likelyChallengerIsBooked = likelyChallengerIsBooked;
        continuation.likelyChallengeIsDangerous = likelyChallengeIsDangerous;
        continuation.pursuesSecondBooking = pursuesSecondBooking;
        continuation.secondBookingProbability = secondBookingProbability;
        continuation.estimatedMovementPhasesToWalkIn = movementPhasesToWalkIn;
        continuation.estimatedWalkInExpectedGoals = walkInExpectedGoals;
        continuation.forecastChallengeCount = forecastChallengeCount;
        continuation.forecastChallengeSequence = forecastChallengeSequence;
        continuation.expectedGoals = continuationExpectedGoals;
    }

    private static float EstimateBestMultiPhaseWalkInExpectedGoals(
        RoomAIDecisionSnapshot snapshot,
        MatchManager matchManager,
        MovementPhaseManager movement,
        PlayerToken ballCarrier,
        HexCell ballHex,
        float perDefensivePhaseRetention,
        out int movementPhasesToWalkIn)
    {
        movementPhasesToWalkIn = -1;
        if (snapshot == null
            || matchManager?.hexGrid?.cells == null
            || ballCarrier == null
            || ballHex == null
            || ballCarrier.pace <= 0)
        {
            return -1f;
        }

        float bestExpectedGoals = -1f;
        foreach (HexCell goalHex in matchManager.hexGrid.cells)
        {
            if (!IsOpponentGoalHex(snapshot, goalHex))
            {
                continue;
            }

            List<HexCell> path = HexGridUtils.FindPath(ballHex, goalHex, matchManager.hexGrid);
            if (path == null || path.Count == 0)
            {
                continue;
            }

            RoomAICandidateExplanation walkPreview = new RoomAICandidateExplanation
            {
                targetIsInOpponentGoal = true
            };
            PopulateWalkInExpectedGoalProbability(
                snapshot,
                new RoomActionCandidate { actor = ballCarrier },
                walkPreview,
                goalHex);
            if (walkPreview.walkInExpectedGoalProbability < 0f)
            {
                continue;
            }

            int currentMovementPace = movement != null && movement.isDribblerRunning
                ? System.Math.Max(0, movement.remainingDribblerPace)
                : 0;
            int stepsAfterCurrentMovement = System.Math.Max(0, path.Count - currentMovementPace);
            int defensivePhases = stepsAfterCurrentMovement == 0
                ? 0
                : (stepsAfterCurrentMovement + ballCarrier.pace - 1) / ballCarrier.pace;
            float routeExpectedGoals = walkPreview.walkInExpectedGoalProbability
                * (float)System.Math.Pow(perDefensivePhaseRetention, defensivePhases);
            if (routeExpectedGoals > bestExpectedGoals)
            {
                bestExpectedGoals = routeExpectedGoals;
                movementPhasesToWalkIn = defensivePhases;
            }
        }

        return bestExpectedGoals;
    }

    private static bool HasTimeForWalkInRoute(RoomClockSnapshot clock, int movementPhasesToWalkIn)
    {
        if (movementPhasesToWalkIn < 0 || clock == null || !clock.isHalfExpired || !clock.extraActionsDetermined)
        {
            return movementPhasesToWalkIn >= 0;
        }

        return clock.extraActionsRemaining > movementPhasesToWalkIn;
    }

    private sealed class ChallengeForecast
    {
        public float shotRetention = 1f;
        public float foulProbability;
        public bool firstChallengerIsBooked;
        public bool firstChallengeIsDangerous;
        public int challengeCount;
        public string sequence = string.Empty;
    }

    private static float EstimateBallRetentionProbability(
        MatchManager matchManager,
        GoalKeeperManager goalKeeperManager,
        PlayerToken ballCarrier,
        HexCell ballHex,
        out bool goalkeeperCanChallenge,
        out float foulProbability,
        out bool likelyChallengerIsBooked,
        out bool likelyChallengeIsDangerous,
        out int forecastChallengeCount,
        out string forecastChallengeSequence)
    {
        goalkeeperCanChallenge = false;
        foulProbability = 0f;
        likelyChallengerIsBooked = false;
        likelyChallengeIsDangerous = false;
        forecastChallengeCount = 0;
        forecastChallengeSequence = string.Empty;
        if (matchManager?.hexGrid == null || ballCarrier == null || ballHex == null)
        {
            return 0f;
        }

        List<PlayerToken> outfieldDefenders = new List<PlayerToken>();
        PlayerToken defendingGoalkeeper = matchManager.hexGrid.GetDefendingGK();
        float bestGoalkeeperRetention = 1f;
        float bestGoalkeeperFoulProbability = 0f;
        foreach (PlayerToken defender in matchManager.hexGrid.GetDefenders())
        {
            HexCell defenderHex = defender?.GetCurrentHex();
            if (defender == null || defenderHex == null)
            {
                continue;
            }

            if (defender == defendingGoalkeeper
                && TryCalculateReachableGoalkeeperWallRetention(
                    matchManager.hexGrid,
                    goalKeeperManager,
                    ballCarrier,
                    defender,
                    ballHex,
                    out float goalkeeperRetention,
                    out float goalkeeperFoulProbability))
            {
                goalkeeperCanChallenge = true;
                if (goalkeeperRetention < bestGoalkeeperRetention)
                {
                    bestGoalkeeperRetention = goalkeeperRetention;
                    bestGoalkeeperFoulProbability = goalkeeperFoulProbability;
                }
                continue;
            }

            if (defender != defendingGoalkeeper)
            {
                outfieldDefenders.Add(defender);
            }
        }

        ChallengeForecast outfieldForecast = ForecastOrderedOutfieldChallenges(
            matchManager,
            ballCarrier,
            ballHex,
            outfieldDefenders,
            maxChallenges: SnapshotContinuationChallengeLookahead);
        float retentionProbability = outfieldForecast.shotRetention;
        foulProbability = outfieldForecast.foulProbability;
        likelyChallengerIsBooked = outfieldForecast.firstChallengerIsBooked;
        likelyChallengeIsDangerous = outfieldForecast.firstChallengeIsDangerous;
        forecastChallengeCount = outfieldForecast.challengeCount;
        forecastChallengeSequence = outfieldForecast.sequence;

        if (goalkeeperCanChallenge && bestGoalkeeperRetention < retentionProbability)
        {
            retentionProbability = bestGoalkeeperRetention;
            foulProbability = bestGoalkeeperFoulProbability;
            likelyChallengerIsBooked = defendingGoalkeeper != null && defendingGoalkeeper.isBooked;
            likelyChallengeIsDangerous = false;
            forecastChallengeCount = 1;
            forecastChallengeSequence = $"{DisplayName(defendingGoalkeeper)} goalkeeper challenge";
        }

        return retentionProbability;
    }

    private static ChallengeForecast ForecastOrderedOutfieldChallenges(
        MatchManager matchManager,
        PlayerToken ballCarrier,
        HexCell ballHex,
        List<PlayerToken> defenders,
        int maxChallenges)
    {
        if (matchManager?.hexGrid == null
            || ballCarrier == null
            || ballHex == null
            || defenders == null
            || defenders.Count == 0
            || maxChallenges <= 0)
        {
            return new ChallengeForecast();
        }

        int usableCount = System.Math.Min(defenders.Count, 30);
        if (usableCount < defenders.Count)
        {
            defenders.RemoveRange(usableCount, defenders.Count - usableCount);
        }

        int availableMask = (1 << usableCount) - 1;
        Dictionary<string, ChallengeForecast> memo = new Dictionary<string, ChallengeForecast>();
        return ForecastDefensiveChoice(
            matchManager,
            ballCarrier,
            ballHex,
            defenders,
            availableMask,
            maxChallenges,
            memo);
    }

    private static ChallengeForecast ForecastDefensiveChoice(
        MatchManager matchManager,
        PlayerToken ballCarrier,
        HexCell carrierHex,
        IReadOnlyList<PlayerToken> defenders,
        int availableMask,
        int remainingChallenges,
        Dictionary<string, ChallengeForecast> memo)
    {
        if (carrierHex == null || availableMask == 0 || remainingChallenges <= 0)
        {
            return new ChallengeForecast();
        }

        string memoKey = $"{carrierHex.coordinates.x}:{carrierHex.coordinates.z}:{availableMask}:{remainingChallenges}";
        if (memo.TryGetValue(memoKey, out ChallengeForecast cached))
        {
            return cached;
        }

        ChallengeForecast defenseBest = null;
        for (int defenderIndex = 0; defenderIndex < defenders.Count; defenderIndex++)
        {
            int defenderBit = 1 << defenderIndex;
            if ((availableMask & defenderBit) == 0)
            {
                continue;
            }

            PlayerToken defender = defenders[defenderIndex];
            foreach (HexCell tackleHex in GetReachableTackleHexes(
                matchManager.hexGrid,
                ballCarrier,
                carrierHex,
                defender))
            {
                int foulThreshold = IsDangerousTackle(
                    matchManager,
                    ballCarrier,
                    carrierHex,
                    tackleHex)
                        ? 2
                        : 1;
                ExpectedStatsCalculator.GroundDuelExpectation duel =
                    ExpectedStatsCalculator.CalculateGroundDuelExpectationWithDefenderSkill(
                        ballCarrier,
                        defender,
                        defender.tackling,
                        defenderBonusMalus: 0,
                        defenderFoulThreshold: foulThreshold);

                ChallengeForecast attackBest = null;
                foreach (HexCell repositionHex in GetAttackerRepositionHexes(
                    matchManager.hexGrid,
                    ballCarrier,
                    carrierHex,
                    tackleHex))
                {
                    ChallengeForecast child = ForecastDefensiveChoice(
                        matchManager,
                        ballCarrier,
                        repositionHex,
                        defenders,
                        availableMask & ~defenderBit,
                        remainingChallenges - 1,
                        memo);
                    ChallengeForecast branch = new ChallengeForecast
                    {
                        shotRetention = duel.dribbleContinuationProbability * child.shotRetention,
                        foulProbability = duel.defenderFoulProbability
                            + (duel.dribbleContinuationProbability * child.foulProbability),
                        firstChallengerIsBooked = defender.isBooked,
                        firstChallengeIsDangerous = foulThreshold == 2,
                        challengeCount = 1 + child.challengeCount,
                        sequence = FormatChallengeStep(defender, tackleHex, repositionHex, child.sequence)
                    };

                    if (attackBest == null
                        || branch.shotRetention > attackBest.shotRetention
                        || (ApproximatelyEqual(branch.shotRetention, attackBest.shotRetention)
                            && branch.foulProbability > attackBest.foulProbability))
                    {
                        attackBest = branch;
                    }
                }

                if (attackBest != null
                    && (defenseBest == null
                        || attackBest.shotRetention < defenseBest.shotRetention
                        || (ApproximatelyEqual(attackBest.shotRetention, defenseBest.shotRetention)
                            && attackBest.foulProbability < defenseBest.foulProbability)))
                {
                    defenseBest = attackBest;
                }
            }
        }

        defenseBest ??= new ChallengeForecast();
        memo[memoKey] = defenseBest;
        return defenseBest;
    }

    private static IEnumerable<HexCell> GetReachableTackleHexes(
        HexGrid hexGrid,
        PlayerToken ballCarrier,
        HexCell carrierHex,
        PlayerToken defender)
    {
        HexCell defenderHex = defender?.GetCurrentHex();
        if (hexGrid == null || carrierHex == null || defenderHex == null || defender == null)
        {
            yield break;
        }

        foreach (HexCell candidate in carrierHex.GetNeighbors(hexGrid))
        {
            if (candidate == null || candidate.isOutOfBounds)
            {
                continue;
            }

            PlayerToken occupant = candidate.GetOccupyingToken();
            if (occupant != null && occupant != defender && occupant != ballCarrier)
            {
                continue;
            }

            if (candidate == defenderHex)
            {
                yield return candidate;
                continue;
            }

            List<HexCell> path = HexGridUtils.FindPath(defenderHex, candidate, hexGrid);
            if (path != null && path.Count > 0 && path.Count <= defender.pace)
            {
                yield return candidate;
            }
        }
    }

    private static IEnumerable<HexCell> GetAttackerRepositionHexes(
        HexGrid hexGrid,
        PlayerToken ballCarrier,
        HexCell carrierHex,
        HexCell tackleHex)
    {
        if (hexGrid == null || carrierHex == null || tackleHex == null)
        {
            yield break;
        }

        // Staying put is always offered with [X] after a normal survived tackle.
        yield return carrierHex;
        foreach (HexCell candidate in tackleHex.GetNeighbors(hexGrid))
        {
            if (candidate == null || candidate == carrierHex || candidate.isOutOfBounds)
            {
                continue;
            }

            PlayerToken occupant = candidate.GetOccupyingToken();
            if (occupant == null || occupant == ballCarrier)
            {
                yield return candidate;
            }
        }
    }

    private static bool IsDangerousTackle(
        MatchManager matchManager,
        PlayerToken ballCarrier,
        HexCell carrierHex,
        HexCell tackleHex)
    {
        if (matchManager == null || ballCarrier == null || carrierHex == null || tackleHex == null)
        {
            return false;
        }

        MatchManager.TeamAttackingDirection direction = ballCarrier.isHomeTeam
            ? matchManager.homeTeamDirection
            : matchManager.awayTeamDirection;
        return carrierHex.IsDangerousTacklingPosition(direction, tackleHex);
    }

    private static string FormatChallengeStep(
        PlayerToken defender,
        HexCell tackleHex,
        HexCell repositionHex,
        string childSequence)
    {
        string step = $"{DisplayName(defender)} at {tackleHex.coordinates} -> dribbler {repositionHex.coordinates}";
        return string.IsNullOrWhiteSpace(childSequence) ? step : $"{step}; {childSequence}";
    }

    private static string DisplayName(PlayerToken token)
    {
        if (token == null)
        {
            return "unknown";
        }

        return !string.IsNullOrWhiteSpace(token.playerName) ? token.playerName : token.name;
    }

    private static bool ApproximatelyEqual(float left, float right)
    {
        return System.Math.Abs(left - right) < 0.0001f;
    }

    private static float CalculateBookingProbability(int refereeLeniency)
    {
        int successfulFaces = System.Math.Max(0, System.Math.Min(6, 7 - refereeLeniency));
        return successfulFaces / 6f;
    }

    private static bool TryCalculateReachableGoalkeeperWallRetention(
        HexGrid hexGrid,
        GoalKeeperManager goalKeeperManager,
        PlayerToken ballCarrier,
        PlayerToken goalkeeper,
        HexCell ballHex,
        out float retentionProbability,
        out float foulProbability)
    {
        retentionProbability = 1f;
        foulProbability = 0f;
        HexCell goalkeeperHex = goalkeeper?.GetCurrentHex();
        if (hexGrid?.cells == null
            || goalKeeperManager == null
            || ballCarrier == null
            || goalkeeperHex == null
            || ballHex == null)
        {
            return false;
        }

        bool found = false;
        foreach (HexCell goalkeeperDestination in hexGrid.cells)
        {
            if (goalkeeperDestination == null
                || !goalKeeperManager.IsGoalkeeperOwnPenaltyHex(goalkeeper, goalkeeperDestination)
                || (goalkeeperDestination.GetOccupyingToken() != null
                    && goalkeeperDestination.GetOccupyingToken() != goalkeeper)
                || HexGridUtils.GetHexStepDistance(goalkeeperHex, goalkeeperDestination) > goalkeeper.pace
                || goalkeeperDestination.coordinates.x != ballHex.coordinates.x)
            {
                continue;
            }

            int wallDistance = HexGridUtils.GetHexStepDistance(goalkeeperDestination, ballHex);
            if (wallDistance < 1 || wallDistance > 3)
            {
                continue;
            }

            int savingPenalty = wallDistance == 3 ? -1 : 0;
            ExpectedStatsCalculator.GroundDuelExpectation duel =
                ExpectedStatsCalculator.CalculateGroundDuelExpectationWithDefenderSkill(
                    ballCarrier,
                    goalkeeper,
                    goalkeeper.saving,
                    savingPenalty);
            if (duel.dribbleContinuationProbability < retentionProbability)
            {
                retentionProbability = duel.dribbleContinuationProbability;
                foulProbability = duel.defenderFoulProbability;
            }
            found = true;
        }

        return found;
    }

    private static float CalculateBestPenaltyExpectedGoals(
        HexGrid hexGrid,
        GoalKeeperManager goalKeeperManager,
        HexCell ballHex)
    {
        PlayerToken goalkeeper = hexGrid?.GetDefendingGK();
        if (hexGrid == null
            || goalKeeperManager == null
            || ballHex == null
            || goalkeeper == null
            || !goalKeeperManager.IsGoalkeeperOwnPenaltyHex(goalkeeper, ballHex))
        {
            return -1f;
        }

        PlayerToken penaltyTaker = null;
        foreach (PlayerToken attacker in hexGrid.GetAttackers())
        {
            if (attacker != null
                && (penaltyTaker == null || attacker.shooting > penaltyTaker.shooting))
            {
                penaltyTaker = attacker;
            }
        }

        return penaltyTaker != null
            ? ExpectedStatsCalculator.CalculateShotGoalProbability(
                penaltyTaker,
                shootingPenalty: 0,
                blockAttempts: null,
                goalkeeper: goalkeeper,
                goalkeeperPenalty: -2)
            : -1f;
    }

    private static Dictionary<string, RoomAITokenSnapshot> BuildTokenLookup(List<RoomAITokenSnapshot> tokens)
    {
        Dictionary<string, RoomAITokenSnapshot> lookup = new Dictionary<string, RoomAITokenSnapshot>();
        if (tokens == null)
        {
            return lookup;
        }

        foreach (RoomAITokenSnapshot token in tokens)
        {
            if (token != null && !string.IsNullOrWhiteSpace(token.tokenKey) && !lookup.ContainsKey(token.tokenKey))
            {
                lookup[token.tokenKey] = token;
            }
        }

        return lookup;
    }

    private static RoomAICandidateExplanation CreateCandidateExplanation(
        RoomAIDecisionSnapshot snapshot,
        RoomActionCandidate action,
        Dictionary<string, RoomAITokenSnapshot> tokenLookup,
        IReadOnlyList<HexCell> opponentPenaltyBoxHexes,
        IReadOnlyList<HexCell> opponentShootingHexes,
        HeaderManager headerManager,
        bool deferRegularHighPass = false)
    {
        if (action == null)
        {
            return null;
        }

        string actorTokenKey = GetTokenKey(action.actor);
        string targetTokenKey = GetTokenKey(action.targetToken);
        RoomHexCoordinates actorHex = ResolveTokenHex(actorTokenKey, action.actor, tokenLookup);
        RoomHexCoordinates targetTokenHex = ResolveTokenHex(targetTokenKey, action.targetToken, tokenLookup);
        RoomHexCoordinates targetHex = RoomHexCoordinates.FromHex(action.targetHex);
        HexCell tacticalTargetHex = action.targetHex ?? action.targetToken?.GetCurrentHex();
        RoomHexCoordinates progressStartHex = actorHex ?? snapshot.ball;
        RoomHexCoordinates progressTargetHex = targetHex ?? targetTokenHex;
        int forwardProgressDelta = CalculateForwardProgressDelta(snapshot, progressStartHex, progressTargetHex);
        RoomAITokenSnapshot actorToken = ResolveTokenSnapshot(actorTokenKey, tokenLookup);
        RoomAITokenSnapshot targetToken = ResolveTokenSnapshot(targetTokenKey, tokenLookup);
        RoomAITokenSnapshot evaluatedToken = targetToken ?? actorToken;
        int nearestOpponentDistance = CalculateNearestOpponentDistance(snapshot, progressTargetHex);
        int minimumStepsToOpponentPenaltyBox = CalculateMinimumDistanceToPitchFeature(
            tacticalTargetHex,
            opponentPenaltyBoxHexes);
        int minimumStepsToShootingHex = CalculateMinimumDistanceToPitchFeature(
            tacticalTargetHex,
            opponentShootingHexes);
        int receiverPace = evaluatedToken?.pace ?? 0;
        int bestReachableOpponentTackling = FindBestReachableOpponentTackling(snapshot, progressTargetHex);
        int receiverDribbling = evaluatedToken?.dribbling ?? 0;
        bool isBallCarrierMovementTarget = IsBallCarrierMovementTarget(action);

        RoomAICandidateExplanation explanation = new RoomAICandidateExplanation
        {
            candidate = action,
            label = action.label,
            manager = action.manager,
            actionType = action.actionType,
            step = action.step,
            key = action.key,
            actorTokenKey = actorTokenKey,
            actorHex = actorHex,
            targetTokenKey = targetTokenKey,
            targetTokenHex = targetTokenHex,
            targetHex = targetHex,
            actorDistanceToBall = CalculateHexDistance(actorHex, snapshot.ball),
            targetDistanceToBall = CalculateHexDistance(progressTargetHex, snapshot.ball),
            forwardProgressDelta = forwardProgressDelta,
            advancesTowardOpponentGoal = forwardProgressDelta > 0,
            targetCanShootFrom = IsOpponentShootingHex(snapshot, tacticalTargetHex),
            targetCanHeadFrom = IsOpponentHeadingHex(snapshot, tacticalTargetHex),
            targetIsInOpponentGoal = isBallCarrierMovementTarget
                && IsOpponentGoalHex(snapshot, tacticalTargetHex),
            targetIsThirdGoalkeeperWallHex = isBallCarrierMovementTarget
                && IsThirdGoalkeeperWallHex(tacticalTargetHex),
            targetDistanceToOpponentGoal = ResolveTargetDistanceToOpponentGoal(snapshot, tacticalTargetHex),
            nearestOpponentDistance = nearestOpponentDistance,
            opponentsWithinTackleReach = CountOpponentsWithinTackleReach(snapshot, progressTargetHex),
            bestReachableOpponentTackling = bestReachableOpponentTackling,
            receiverDribbling = receiverDribbling,
            tackleToDribblingMismatch = bestReachableOpponentTackling - receiverDribbling,
            friendlySupportWithinRange = CountFriendlySupportWithinRange(snapshot, progressTargetHex, evaluatedToken),
            targetIsInFinalThird = tacticalTargetHex != null && tacticalTargetHex.isInFinalThird != 0,
            minimumStepsToOpponentPenaltyBox = minimumStepsToOpponentPenaltyBox,
            minimumStepsToShootingHex = minimumStepsToShootingHex,
            receiverCanReachOpponentPenaltyBoxNextMovement = receiverPace > 0
                && minimumStepsToOpponentPenaltyBox >= 0
                && minimumStepsToOpponentPenaltyBox <= receiverPace,
            receiverCanReachShootingHexNextMovement = receiverPace > 0
                && minimumStepsToShootingHex >= 0
                && minimumStepsToShootingHex <= receiverPace,
            groundPassInterceptionCandidates = CountGroundPassInterceptionCandidates(action, tacticalTargetHex),
            actorToken = actorToken,
            targetToken = targetToken,
            isKickoffDecision = snapshot.currentState?.IndexOf(
                "kick",
                System.StringComparison.OrdinalIgnoreCase) >= 0
        };

        PopulateRootActionPreview(
            snapshot,
            action,
            explanation,
            tokenLookup,
            opponentPenaltyBoxHexes,
            opponentShootingHexes);
        PopulateFirstTimePassExtraPaceFacts(
            snapshot,
            action,
            explanation,
            tacticalTargetHex,
            opponentShootingHexes);
        PopulateFoulDecisionFacts(action, explanation);
        PopulateLateResultManagementFacts(snapshot, action, explanation);
        if (!deferRegularHighPass)
            PopulateRegularHighPassOutcomeFacts(snapshot, action, explanation, tacticalTargetHex);
        PopulateGoalkeeperPositioningFacts(snapshot, action, explanation);
        PopulateLongBallGoalkeeperRecoveryFacts(snapshot, action, explanation);
        PopulateDribbleSnapshotRouteFacts(snapshot, action, explanation, tacticalTargetHex);
        PopulateTackleDecisionFacts(snapshot, action, explanation);
        PopulateSuccessfulTackleRepositionPlan(snapshot, action, explanation, tacticalTargetHex);
        PopulateSnapshotDefensiveMoveFacts(snapshot, action, explanation);
        PopulateSetPieceSetupFacts(snapshot, action, explanation, tacticalTargetHex);
        PopulateRegularFinalThirdFacts(snapshot, action, explanation, tacticalTargetHex);
        PopulateSetPieceHighPassExpectedGoals(snapshot, action, explanation, tacticalTargetHex);
        PopulateHighPassGoalkeeperDecision(snapshot, action, explanation, tacticalTargetHex);
        PopulateWalkInExpectedGoalProbability(snapshot, action, explanation, tacticalTargetHex);
        PopulateShotExpectedGoals(action, explanation);
        PopulateCommittedAerialDefense(snapshot, action, explanation);
        if (!(deferRegularHighPass && action.step == RoomDecisionStep.ChooseActor
            && IsAttackingSupportMove(snapshot, action, MatchManager.Instance?.ball?.GetCurrentHex()?.GetOccupyingToken())))
            PopulateGroundPassContinuationFacts(snapshot, action, explanation, tacticalTargetHex);
        if (!snapshot.attackHasPossession && snapshot.expectedTeam == snapshot.teamInAttack
            && action.actionType == RoomActionType.StartMovement && snapshot.currentState == "MovementPhase")
        {
            PlayerToken collector = action.actor ?? MatchManager.Instance?.movementPhaseManager?.selectedToken;
            if (collector != null) explanation.passRace = GetPassRace(snapshot, collector,
                MatchManager.Instance?.ball?.GetCurrentHex(), false);
        }
        PopulateHeaderPossessionChainFacts(
            snapshot,
            action,
            explanation,
            tacticalTargetHex,
            headerManager,
            opponentShootingHexes);
        return explanation;
    }

    private static void PopulateCommittedAerialDefense(RoomAIDecisionSnapshot snapshot,
        RoomActionCandidate action, RoomAICandidateExplanation facts)
    {
        var grid = MatchManager.Instance?.hexGrid;
        if (snapshot.committedAerialTarget == null || snapshot.expectedTeam == snapshot.teamInAttack
            || action.manager != nameof(HighPassManager) || action.actor == null
            || facts.actorToken == null || grid == null
            || action.step is not (RoomDecisionStep.ChooseActor or RoomDecisionStep.ChooseTarget)) return;
        // A blocked actor must not fall back to generic pressure scoring simply
        // because it has no legal destination to populate below.
        facts.isCommittedAerialDefense = true;
        var destinations = action.step == RoomDecisionStep.ChooseTarget
            ? new[] { action.targetHex }.AsEnumerable()
            : GetCachedReachability(snapshot, grid, action.actor.GetCurrentHex(), HighPassManager.DefenderMoveRange).reachableHexes;
        float baseline = RoomAITacticalSafety.CommittedAerialDanger(snapshot);
        var center = RoomAITacticalSafety.GoalCenter(snapshot);
        var best = destinations.Where(hex => hex != null && !hex.isOutOfBounds
                && !hex.isAttackOccupied && !hex.isDefenseOccupied)
            .Select(hex => new { hex, gain = baseline - RoomAITacticalSafety.CommittedAerialDanger(
                snapshot, facts.actorTokenKey, RoomHexCoordinates.FromHex(hex)) })
            .OrderByDescending(item => item.gain)
            .ThenBy(item => RoomAITacticalSafety.Distance(RoomHexCoordinates.FromHex(item.hex), center))
            .ThenBy(item => HexGridUtils.GetHexStepDistance(action.actor.GetCurrentHex(), item.hex)).FirstOrDefault();
        if (best == null) return;
        facts.committedAerialDefenseGain = best.gain;
        facts.targetHex = RoomHexCoordinates.FromHex(best.hex);
        if (center != null)
            facts.committedAerialRecoverySteps = System.Math.Max(0, RoomAITacticalSafety.Distance(facts.actorHex, center)
                - RoomAITacticalSafety.Distance(facts.targetHex, center));
    }

    private static void PopulateFirstTimePassExtraPaceFacts(
        RoomAIDecisionSnapshot snapshot,
        RoomActionCandidate action,
        RoomAICandidateExplanation explanation,
        HexCell requestedTarget,
        IReadOnlyList<HexCell> opponentShootingHexes)
    {
        MatchManager matchManager = MatchManager.Instance;
        FirstTimePassManager firstTimePass = matchManager?.firstTimePassManager;
        HexGrid hexGrid = matchManager?.hexGrid;
        ShotManager shotManager = matchManager?.shotManager;
        MovementPhaseManager movement = matchManager?.movementPhaseManager;
        HexCell ballHex = matchManager?.ball?.GetCurrentHex();
        PlayerToken passer = ballHex?.GetOccupyingToken() ?? matchManager?.LastTokenToTouchTheBallOnPurpose;
        bool isFirstTimePassChoice = action?.actionType == RoomActionType.FirstTimePass;
        bool isFirstTimePassMove = action?.actionType == RoomActionType.SetupMove
            && string.Equals(action.manager, nameof(FirstTimePassManager), System.StringComparison.Ordinal);
        if (snapshot == null
            || action == null
            || explanation == null
            || firstTimePass == null
            || hexGrid == null
            || shotManager == null
            || movement == null
            || matchManager.groundBallManager?.ball == null
            || ballHex == null
            || passer == null
            || opponentShootingHexes == null
            || (!isFirstTimePassChoice && !isFirstTimePassMove)
            || (isFirstTimePassMove && firstTimePass.isWaitingForDefenderSelection))
        {
            return;
        }

        List<HexCell> passTargets = new List<HexCell>();
        HexCell committedTarget = isFirstTimePassMove ? firstTimePass.currentTargetHex : requestedTarget;
        if (committedTarget != null)
        {
            passTargets.Add(committedTarget);
        }
        else if (action.step == RoomDecisionStep.ChooseActionType)
        {
            if (!snapshot.hasCachedFirstTimePassTargets)
            {
                snapshot.cachedFirstTimePassTargets.AddRange(
                    firstTimePass.GetLegalFirstTimePassTargetsForDecisionPreview());
                snapshot.hasCachedFirstTimePassTargets = true;
            }
            passTargets.AddRange(snapshot.cachedFirstTimePassTargets);
        }

        if (passTargets.Count == 0)
        {
            return;
        }

        string setupCacheKey = BuildFirstTimePassSetupCacheKey(
            action,
            requestedTarget,
            committedTarget,
            passTargets);
        if (snapshot.firstTimePassSetupCache.TryGetValue(
                setupCacheKey,
                out RoomAIFirstTimePassSetupFacts cachedSetup))
        {
            snapshot.firstTimePassSetupCacheHits++;
            ApplyFirstTimePassSetupFacts(explanation, cachedSetup, action.step);
            return;
        }

        List<PlayerToken> setupShooters = new List<PlayerToken>();
        if (isFirstTimePassMove && action.step == RoomDecisionStep.ChooseTarget)
        {
            PlayerToken selected = action.actor ?? firstTimePass.selectedToken;
            if (selected != null)
            {
                setupShooters.Add(selected);
            }
        }
        else if (isFirstTimePassMove && action.step == RoomDecisionStep.ChooseActor)
        {
            if (action.actor != null)
            {
                setupShooters.Add(action.actor);
            }
        }
        else
        {
            setupShooters.AddRange(hexGrid.GetAttackers().Where(token => token != null && token.isPlaying));
        }

        float bestCombinedExpectedGoals = -1f;
        float bestRouteExpectedGoals = -1f;
        float bestRawExpectedGoals = -1f;
        float bestReachProbability = -1f;
        float bestPassRetention = -1f;
        PlayerToken bestShooter = null;
        HexCell bestSetupHex = null;
        HexCell bestShotHex = null;
        HexCell bestPassTarget = null;
        int bestDistanceBefore = -1;
        int bestDistanceAfter = -1;
        bool bestUnlocksShot = false;

        List<(PlayerToken shooter, HexCell setupHex, HexCell shotHex, int distanceBefore,
            int distanceAfter, float reachProbability, float rawExpectedGoals,
            float routeExpectedGoals, bool unlocksShot)> setupPreviews = new();
        foreach (PlayerToken shooter in setupShooters)
        {
            HexCell shooterHex = shooter?.GetCurrentHex();
            if (shooter == null || shooterHex == null || shooter.IsGoalKeeper || shooter.pace <= 0)
            {
                continue;
            }

            int distanceBefore = CalculateMinimumDistanceToPitchFeature(shooterHex, opponentShootingHexes);
            if (distanceBefore < 0 || distanceBefore > shooter.pace + 1)
            {
                continue;
            }

            IReadOnlyList<HexCell> setupHexes = isFirstTimePassMove
                && action.step == RoomDecisionStep.ChooseTarget
                && requestedTarget != null
                    ? new[] { requestedTarget }
                    : firstTimePass.GetValidFtpMovementDestinationsForDecision(shooter);
            foreach (HexCell setupHex in setupHexes)
            {
                if (setupHex == null)
                {
                    continue;
                }

                int distanceAfter = CalculateMinimumDistanceToPitchFeature(setupHex, opponentShootingHexes);
                if (distanceAfter < 0 || distanceAfter > shooter.pace || distanceAfter >= distanceBefore)
                {
                    continue;
                }

                HexCell bestPreviewShotHex = null;
                float bestPreviewReach = -1f;
                float bestPreviewRawXg = -1f;
                float bestPreviewRouteXg = -1f;
                foreach (HexCell shotHex in opponentShootingHexes)
                {
                    if (shotHex == null
                        || HexGridUtils.GetHexStepDistance(setupHex, shotHex) > shooter.pace
                        || (shotHex.GetOccupyingToken() != null && shotHex.GetOccupyingToken() != shooter))
                    {
                        continue;
                    }

                    List<HexCell> route = new List<HexCell>();
                    if (!ReferenceEquals(setupHex, shotHex))
                    {
                        HashSet<HexCell> vacated = new HashSet<HexCell> { shooterHex };
                        route = HexGridUtils.FindPath(setupHex, shotHex, hexGrid, vacated);
                        if (route == null || route.Count == 0 || route.Count > shooter.pace)
                        {
                            continue;
                        }
                    }

                    CalculateMovementRouteProbabilities(
                        movement,
                        route,
                        out _,
                        out _,
                        out float reachProbability);
                    if (!TryGetCachedExpectedGoalsPreview(
                            snapshot,
                            shotManager,
                            shooter,
                            shotHex,
                            "snapshot",
                            out float rawExpectedGoals,
                            out _,
                            out _,
                            out _))
                    {
                        continue;
                    }

                    float routeExpectedGoals = reachProbability * rawExpectedGoals;
                    if (routeExpectedGoals <= bestPreviewRouteXg)
                    {
                        continue;
                    }

                    bestPreviewShotHex = shotHex;
                    bestPreviewReach = reachProbability;
                    bestPreviewRawXg = rawExpectedGoals;
                    bestPreviewRouteXg = routeExpectedGoals;
                }

                if (bestPreviewShotHex != null)
                {
                    setupPreviews.Add((
                        shooter,
                        setupHex,
                        bestPreviewShotHex,
                        distanceBefore,
                        distanceAfter,
                        bestPreviewReach,
                        bestPreviewRawXg,
                        bestPreviewRouteXg,
                        distanceBefore == shooter.pace + 1));
                }
            }
        }

        foreach (HexCell passTarget in passTargets)
        {
            if (passTarget == null || passTarget.isOutOfBounds)
            {
                continue;
            }

            PlayerToken currentReceiver = passTarget.GetOccupyingToken();
            if (currentReceiver == passer)
            {
                continue;
            }

            List<GroundInterceptionCandidate> interceptors = GroundPassCommon.BuildOrderedInterceptionCandidates(
                hexGrid,
                matchManager.groundBallManager.ball,
                passTarget);
            float passTurnoverProbability = ExpectedStatsCalculator.CalculateAtLeastOneRecoveryProbability(
                interceptors
                    .Where(candidate => candidate?.DefenderToken != null)
                    .Select(candidate => candidate.DefenderToken)
                    .Distinct()
                    .ToArray());
            float passRetention = System.Math.Max(0f, System.Math.Min(1f, 1f - passTurnoverProbability));
            foreach (var preview in setupPreviews)
            {
                bool setupCollectsPass = ReferenceEquals(preview.setupHex, passTarget);
                if (preview.shooter == passer)
                {
                    // The FTP passer is explicitly excluded from collecting their own pass.
                    continue;
                }

                // A teammate receiving the ball does not give the setup mover
                // possession. No unmodelled relay may connect their shooting route.
                if (currentReceiver != null && currentReceiver != preview.shooter)
                {
                    continue;
                }

                if (IsTokenOffsideAtHex(snapshot, preview.shooter, preview.setupHex))
                {
                    continue;
                }

                var vacated = new HashSet<HexCell> { preview.shooter.GetCurrentHex() };
                var pickup = setupCollectsPass ? new List<HexCell>()
                    : HexGridUtils.FindPath(preview.setupHex, passTarget, hexGrid, vacated);
                if (!setupCollectsPass && pickup.Count == 0) continue;
                var shotRoute = passTarget == preview.shotHex ? new List<HexCell>()
                    : HexGridUtils.FindPath(passTarget, preview.shotHex, hexGrid, vacated);
                if (passTarget != preview.shotHex && shotRoute.Count == 0
                    || pickup.Count + shotRoute.Count > preview.shooter.pace) continue;
                // Only movement after collection crosses zones with the ball.
                var dribble = new List<HexCell>();
                if (!setupCollectsPass) dribble.Add(passTarget);
                dribble.AddRange(shotRoute);
                CalculateMovementRouteProbabilities(movement, dribble, out _, out _, out float actualReach);
                float actualRouteXg = actualReach * preview.rawExpectedGoals;
                float combinedExpectedGoals = passRetention * actualRouteXg;
                bool better = combinedExpectedGoals > bestCombinedExpectedGoals + 0.0001f
                    || (ApproximatelyEqual(combinedExpectedGoals, bestCombinedExpectedGoals)
                        && preview.unlocksShot && !bestUnlocksShot)
                    || (ApproximatelyEqual(combinedExpectedGoals, bestCombinedExpectedGoals)
                        && preview.unlocksShot == bestUnlocksShot
                        && preview.shooter.shooting > (bestShooter?.shooting ?? -1));
                if (!better)
                {
                    continue;
                }

                bestCombinedExpectedGoals = combinedExpectedGoals;
                bestRouteExpectedGoals = actualRouteXg;
                bestRawExpectedGoals = preview.rawExpectedGoals;
                bestReachProbability = actualReach;
                bestPassRetention = passRetention;
                bestShooter = preview.shooter;
                bestSetupHex = preview.setupHex;
                bestShotHex = preview.shotHex;
                bestPassTarget = passTarget;
                bestDistanceBefore = preview.distanceBefore;
                bestDistanceAfter = preview.distanceAfter;
                bestUnlocksShot = preview.unlocksShot;
            }
        }

        if (bestShooter == null)
        {
            snapshot.firstTimePassSetupCache[setupCacheKey] = new RoomAIFirstTimePassSetupFacts();
            return;
        }

        explanation.isFirstTimePassShotSetup = true;
        explanation.firstTimePassExtraPaceUnlocksShot = bestUnlocksShot;
        explanation.firstTimePassSetupShooterTokenKey = GetTokenKey(bestShooter);
        explanation.firstTimePassSetupShooterPace = bestShooter.pace;
        explanation.firstTimePassSetupShooterShooting = bestShooter.shooting;
        explanation.firstTimePassBestSetupHex = RoomHexCoordinates.FromHex(bestSetupHex);
        explanation.firstTimePassReachableShotHex = RoomHexCoordinates.FromHex(bestShotHex);
        explanation.firstTimePassShotDistanceBefore = bestDistanceBefore;
        explanation.firstTimePassShotDistanceAfter = bestDistanceAfter;
        explanation.firstTimePassPassRetentionProbability = bestPassRetention;
        explanation.firstTimePassReachShotProbability = bestReachProbability;
        explanation.firstTimePassRawShotExpectedGoals = bestRawExpectedGoals;
        explanation.firstTimePassShotRouteExpectedGoals = bestRouteExpectedGoals;
        explanation.firstTimePassCombinedExpectedGoals = bestCombinedExpectedGoals;
        if (action.step == RoomDecisionStep.ChooseActionType && bestPassTarget != null)
        {
            explanation.targetHex = RoomHexCoordinates.FromHex(bestPassTarget);
        }
        snapshot.firstTimePassSetupCache[setupCacheKey] = CaptureFirstTimePassSetupFacts(explanation);
    }

    private static string BuildFirstTimePassSetupCacheKey(
        RoomActionCandidate action,
        HexCell requestedTarget,
        HexCell committedTarget,
        IReadOnlyList<HexCell> passTargets)
    {
        string actor = GetTokenKey(action?.actor);
        string requested = requestedTarget == null
            ? "-"
            : $"{requestedTarget.coordinates.x},{requestedTarget.coordinates.z}";
        string committed = committedTarget == null
            ? "-"
            : $"{committedTarget.coordinates.x},{committedTarget.coordinates.z}";
        string targets = string.Join(
            ";",
            passTargets.Where(hex => hex != null)
                .Select(hex => $"{hex.coordinates.x},{hex.coordinates.z}"));
        return $"{action?.actionType}|{action?.step}|{actor}|{requested}|{committed}|{targets}";
    }

    private static RoomAIFirstTimePassSetupFacts CaptureFirstTimePassSetupFacts(
        RoomAICandidateExplanation explanation)
    {
        return new RoomAIFirstTimePassSetupFacts
        {
            found = explanation.isFirstTimePassShotSetup,
            extraPaceUnlocksShot = explanation.firstTimePassExtraPaceUnlocksShot,
            shooterTokenKey = explanation.firstTimePassSetupShooterTokenKey,
            shooterPace = explanation.firstTimePassSetupShooterPace,
            shooterShooting = explanation.firstTimePassSetupShooterShooting,
            setupHex = explanation.firstTimePassBestSetupHex,
            shotHex = explanation.firstTimePassReachableShotHex,
            distanceBefore = explanation.firstTimePassShotDistanceBefore,
            distanceAfter = explanation.firstTimePassShotDistanceAfter,
            passRetention = explanation.firstTimePassPassRetentionProbability,
            reachShotProbability = explanation.firstTimePassReachShotProbability,
            rawExpectedGoals = explanation.firstTimePassRawShotExpectedGoals,
            routeExpectedGoals = explanation.firstTimePassShotRouteExpectedGoals,
            combinedExpectedGoals = explanation.firstTimePassCombinedExpectedGoals,
            rootTargetHex = explanation.targetHex
        };
    }

    private static void ApplyFirstTimePassSetupFacts(
        RoomAICandidateExplanation explanation,
        RoomAIFirstTimePassSetupFacts facts,
        RoomDecisionStep step)
    {
        if (explanation == null || facts?.found != true)
        {
            return;
        }

        explanation.isFirstTimePassShotSetup = true;
        explanation.firstTimePassExtraPaceUnlocksShot = facts.extraPaceUnlocksShot;
        explanation.firstTimePassSetupShooterTokenKey = facts.shooterTokenKey;
        explanation.firstTimePassSetupShooterPace = facts.shooterPace;
        explanation.firstTimePassSetupShooterShooting = facts.shooterShooting;
        explanation.firstTimePassBestSetupHex = facts.setupHex;
        explanation.firstTimePassReachableShotHex = facts.shotHex;
        explanation.firstTimePassShotDistanceBefore = facts.distanceBefore;
        explanation.firstTimePassShotDistanceAfter = facts.distanceAfter;
        explanation.firstTimePassPassRetentionProbability = facts.passRetention;
        explanation.firstTimePassReachShotProbability = facts.reachShotProbability;
        explanation.firstTimePassRawShotExpectedGoals = facts.rawExpectedGoals;
        explanation.firstTimePassShotRouteExpectedGoals = facts.routeExpectedGoals;
        explanation.firstTimePassCombinedExpectedGoals = facts.combinedExpectedGoals;
        if (step == RoomDecisionStep.ChooseActionType)
        {
            explanation.targetHex = facts.rootTargetHex;
        }
    }

    private static void PopulateFoulDecisionFacts(
        RoomActionCandidate action,
        RoomAICandidateExplanation explanation)
    {
        MatchManager matchManager = MatchManager.Instance;
        MovementPhaseManager movement = matchManager?.movementPhaseManager;
        if (action == null
            || explanation == null
            || action.actionType != RoomActionType.Foul
            || movement?.PendingFoulIsPenalty != true)
        {
            return;
        }

        float penaltyExpectedGoals = CalculateBestPenaltyExpectedGoals(
            matchManager.hexGrid,
            movement.goalKeeperManager,
            matchManager.ball?.GetCurrentHex());
        if (penaltyExpectedGoals < 0f)
        {
            return;
        }

        explanation.isPenaltyFoulDecision = true;
        explanation.penaltyExpectedGoals = penaltyExpectedGoals;
        explanation.expectedGoals = penaltyExpectedGoals;
        explanation.shootingPenalty = 0;
        explanation.shotBlockerCount = 0;
        explanation.goalkeeperPenalty = -2;
    }

    private static void PopulateLateResultManagementFacts(
        RoomAIDecisionSnapshot snapshot,
        RoomActionCandidate action,
        RoomAICandidateExplanation explanation)
    {
        if (snapshot?.clock == null
            || action == null
            || explanation == null
            || !snapshot.attackHasPossession
            || !snapshot.clock.isHalfExpired
            || !snapshot.clock.extraActionsDetermined
            || snapshot.clock.extraActionsRemaining > 3
            || snapshot.ball == null)
        {
            return;
        }

        string direction = ResolveExpectedTeamDirection(snapshot);
        bool ballInOwnHalf = string.Equals(direction, "LeftToRight", System.StringComparison.OrdinalIgnoreCase)
            ? snapshot.ball.x < 0
            : string.Equals(direction, "RightToLeft", System.StringComparison.OrdinalIgnoreCase)
                && snapshot.ball.x > 0;
        if (!ballInOwnHalf)
        {
            return;
        }

        bool isResultManagementAction = action.actionType == RoomActionType.LongBall
            || action.actionType == RoomActionType.GroundPass
            || action.actionType == RoomActionType.StartMovement
            || action.actionType == RoomActionType.FirstTimePass
            || action.actionType == RoomActionType.QuickThrow;
        if (!isResultManagementAction)
        {
            return;
        }

        bool expectedTeamIsHome = string.Equals(
            snapshot.expectedTeam,
            "Home",
            System.StringComparison.OrdinalIgnoreCase);
        bool expectedTeamIsAway = string.Equals(
            snapshot.expectedTeam,
            "Away",
            System.StringComparison.OrdinalIgnoreCase);
        if (!expectedTeamIsHome && !expectedTeamIsAway)
        {
            return;
        }

        explanation.isLateResultManagementDecision = true;
        explanation.lateResultScoreDifference = expectedTeamIsHome
            ? snapshot.homeScore - snapshot.awayScore
            : snapshot.awayScore - snapshot.homeScore;
        explanation.lateResultActionsRemaining = snapshot.clock.extraActionsRemaining;
    }

    private static void PopulateRootActionPreview(
        RoomAIDecisionSnapshot snapshot,
        RoomActionCandidate action,
        RoomAICandidateExplanation explanation,
        Dictionary<string, RoomAITokenSnapshot> tokenLookup,
        IReadOnlyList<HexCell> opponentPenaltyBoxHexes,
        IReadOnlyList<HexCell> opponentShootingHexes)
    {
        if (snapshot == null
            || action == null
            || explanation == null
            || action.step != RoomDecisionStep.ChooseActionType)
        {
            return;
        }

        MatchManager matchManager = MatchManager.Instance;
        HexCell ballHex = matchManager?.ball?.GetCurrentHex();
        PlayerToken ballCarrier = ballHex?.GetOccupyingToken() ?? matchManager?.LastTokenToTouchTheBallOnPurpose;
        if (ballHex == null || ballCarrier == null)
        {
            return;
        }

        explanation.isRootActionPreview = true;
        string ballCarrierKey = GetTokenKey(ballCarrier);
        explanation.actorTokenKey = ballCarrierKey;
        explanation.actorHex = RoomHexCoordinates.FromHex(ballCarrier.GetCurrentHex());
        explanation.actorToken = ResolveTokenSnapshot(ballCarrierKey, tokenLookup);

        if (action.actionType == RoomActionType.QuickThrow)
        {
            PopulateQuickThrowRacePreview(snapshot, action, explanation);
            return;
        }

        if (action.actionType == RoomActionType.HighPass)
        {
            explanation.highPassAccuracyProbability = System.Math.Max(
                0f,
                System.Math.Min(1f, (ballCarrier.highPass - 1) / 6f));
            return;
        }

        if (action.actionType == RoomActionType.StartMovement)
        {
            explanation.minimumStepsToOpponentPenaltyBox = CalculateMinimumDistanceToPitchFeature(
                ballHex,
                opponentPenaltyBoxHexes);
            explanation.minimumStepsToShootingHex = CalculateMinimumDistanceToPitchFeature(
                ballHex,
                opponentShootingHexes);
            explanation.receiverCanReachOpponentPenaltyBoxNextMovement = ballCarrier.pace > 0
                && explanation.minimumStepsToOpponentPenaltyBox >= 0
                && explanation.minimumStepsToOpponentPenaltyBox <= ballCarrier.pace;
            explanation.receiverCanReachShootingHexNextMovement = ballCarrier.pace > 0
                && explanation.minimumStepsToShootingHex >= 0
                && explanation.minimumStepsToShootingHex <= ballCarrier.pace;
            return;
        }

        if (action.actionType != RoomActionType.GroundPass
            || matchManager.groundBallManager == null)
        {
            return;
        }

        HexCell bestTarget = null;
        PlayerToken bestReceiver = null;
        int bestScore = int.MinValue;
        foreach (HexCell target in matchManager.groundBallManager.GetLegalGroundPassTargetsForDecisionPreview())
        {
            if (target == null)
            {
                continue;
            }

            PlayerToken receiver = target.GetOccupyingToken();
            bool friendlyReceiver = receiver != null && receiver.isAttacker;
            int progress = CalculateForwardProgressDelta(
                snapshot,
                RoomHexCoordinates.FromHex(ballHex),
                RoomHexCoordinates.FromHex(target));
            int reachableShotDistance = receiver != null
                ? CalculateMinimumDistanceToPitchFeature(target, opponentShootingHexes)
                : -1;
            bool canCreateShotNextMovement = receiver != null
                && receiver.pace > 0
                && reachableShotDistance >= 0
                && reachableShotDistance <= receiver.pace;
            int futureTacklePressure = receiver != null
                ? CountOpponentsWithinTackleReach(snapshot, RoomHexCoordinates.FromHex(target))
                : 0;
            int interceptionCandidates = CountGroundPassInterceptionCandidates(action, target);
            float immediateRouteExpectedGoals = -1f;
            if (friendlyReceiver
                && IsOpponentShootingHex(snapshot, target)
                && matchManager.shotManager != null
                && matchManager.shotManager.TryCalculateBestExpectedGoalsPreview(
                    receiver,
                    target,
                    "snapshot",
                    out float receiverExpectedGoals,
                    out _,
                    out _,
                    out _))
            {
                List<GroundInterceptionCandidate> interceptors = GroundPassCommon.BuildOrderedInterceptionCandidates(
                    matchManager.hexGrid,
                    matchManager.groundBallManager.ball,
                    target);
                float turnoverProbability = ExpectedStatsCalculator.CalculateAtLeastOneRecoveryProbability(
                    interceptors
                        .Where(candidate => candidate?.DefenderToken != null)
                        .Select(candidate => candidate.DefenderToken)
                        .Distinct()
                        .ToArray());
                immediateRouteExpectedGoals = (1f - turnoverProbability) * receiverExpectedGoals;
            }
            int score = (friendlyReceiver ? 180 : 0)
                + (IsOpponentShootingHex(snapshot, target) ? 500 : 0)
                + (IsOpponentHeadingHex(snapshot, target) ? 80 : 0)
                + (canCreateShotNextMovement ? 220 : 0)
                + (immediateRouteExpectedGoals >= 0f
                    ? (int)System.Math.Round(immediateRouteExpectedGoals * 700f)
                    : 0)
                + (progress * 12)
                - (interceptionCandidates * 90)
                - (futureTacklePressure * 55);
            if (score > bestScore)
            {
                bestScore = score;
                bestTarget = target;
                bestReceiver = receiver;
            }
        }

        if (bestTarget == null)
        {
            return;
        }

        string receiverKey = GetTokenKey(bestReceiver);
        RoomHexCoordinates bestCoordinates = RoomHexCoordinates.FromHex(bestTarget);
        explanation.targetHex = bestCoordinates;
        explanation.targetTokenKey = receiverKey;
        explanation.targetTokenHex = bestReceiver != null ? bestCoordinates : null;
        explanation.targetToken = ResolveTokenSnapshot(receiverKey, tokenLookup);
        explanation.forwardProgressDelta = CalculateForwardProgressDelta(
            snapshot,
            RoomHexCoordinates.FromHex(ballHex),
            bestCoordinates);
        explanation.advancesTowardOpponentGoal = explanation.forwardProgressDelta > 0;
        explanation.targetCanShootFrom = IsOpponentShootingHex(snapshot, bestTarget);
        explanation.targetCanHeadFrom = IsOpponentHeadingHex(snapshot, bestTarget);
        explanation.targetDistanceToOpponentGoal = ResolveTargetDistanceToOpponentGoal(snapshot, bestTarget);
        explanation.groundPassInterceptionCandidates = CountGroundPassInterceptionCandidates(action, bestTarget);
        explanation.friendlySupportWithinRange = CountFriendlySupportWithinRange(
            snapshot,
            bestCoordinates,
            explanation.targetToken);
        explanation.minimumStepsToOpponentPenaltyBox = CalculateMinimumDistanceToPitchFeature(
            bestTarget,
            opponentPenaltyBoxHexes);
        explanation.minimumStepsToShootingHex = CalculateMinimumDistanceToPitchFeature(
            bestTarget,
            opponentShootingHexes);
        int bestReceiverPace = bestReceiver != null ? bestReceiver.pace : 0;
        explanation.receiverCanReachOpponentPenaltyBoxNextMovement = bestReceiverPace > 0
            && explanation.minimumStepsToOpponentPenaltyBox >= 0
            && explanation.minimumStepsToOpponentPenaltyBox <= bestReceiverPace;
        explanation.receiverCanReachShootingHexNextMovement = bestReceiverPace > 0
            && explanation.minimumStepsToShootingHex >= 0
            && explanation.minimumStepsToShootingHex <= bestReceiverPace;
    }

    /// <summary>
    /// Values the short sequence which the human-shadow notes repeatedly describe:
    /// create a passing lane, complete the pass, then use the receiver's immediate
    /// Snapshot (or their reachable next-movement shot). This remains a preview of
    /// legal actions; the live managers still validate and execute every step.
    /// </summary>
    private static void PopulateQuickThrowRacePreview(RoomAIDecisionSnapshot snapshot,
        RoomActionCandidate action, RoomAICandidateExplanation explanation)
    {
        MatchManager match = MatchManager.Instance;
        if (match?.groundBallManager == null) return;
        var targets = match.groundBallManager.GetLegalGroundPassTargetsForDecisionPreview()
            .OrderByDescending(hex => hex.GetOccupyingToken()?.isAttacker == true)
            .ThenBy(hex => snapshot.tokens.Where(token => token.isPlaying && token.teamSide == snapshot.expectedTeam
                && !token.isGoalKeeper).Select(token => RoomAITacticalSafety.Distance(token.currentHex, RoomHexCoordinates.FromHex(hex)))
                .DefaultIfEmpty(100).Min()).Take(48);
        RoomAICandidateExplanation best = null;
        float bestValue = float.NegativeInfinity;
        foreach (HexCell target in targets)
        {
            var preview = new RoomAICandidateExplanation();
            var pass = new RoomActionCandidate { actionType = RoomActionType.QuickThrow, step = RoomDecisionStep.ChooseTarget,
                manager = nameof(GroundBallManager), targetHex = target };
            PopulateGroundPassContinuationFacts(snapshot, pass, preview, target);
            if (preview.passRace == null) continue;
            bool safe = preview.passRace.safeContinuationHexes > 0 || preview.passRace.routeExpectedGoals > 0f;
            float value = (safe ? 2f : 0f) + preview.groundPassRetentionProbability
                + System.Math.Max(0f, preview.groundPassRouteExpectedGoals) * 2f;
            if (value <= bestValue) continue;
            bestValue = value; best = preview;
        }
        explanation.isGroundPassContinuation = true;
        explanation.passRace = best?.passRace ?? new RoomAIPassRacePreview { opponentCollectsFirst = true };
        explanation.groundPassRetentionProbability = best?.groundPassRetentionProbability ?? 0f;
        explanation.groundPassRouteExpectedGoals = best?.groundPassRouteExpectedGoals ?? -1f;
        explanation.groundPassCollectorTokenKey = best?.groundPassCollectorTokenKey;
        explanation.groundPassCollectorPickupDistance = best?.groundPassCollectorPickupDistance ?? -1;
        explanation.groundPassCollectorRemainingPace = best?.groundPassCollectorRemainingPace ?? 0;
        explanation.groundPassContinuationTargetHex = best?.groundPassContinuationTargetHex;
        explanation.targetHex = best?.groundPassContinuationTargetHex;
    }

    private static void PopulateGroundPassContinuationFacts(
        RoomAIDecisionSnapshot snapshot,
        RoomActionCandidate action,
        RoomAICandidateExplanation explanation,
        HexCell requestedTarget)
    {
        MatchManager matchManager = MatchManager.Instance;
        HexGrid hexGrid = matchManager?.hexGrid;
        HexCell ballHex = matchManager?.ball?.GetCurrentHex();
        PlayerToken passer = ballHex?.GetOccupyingToken() ?? matchManager?.LastTokenToTouchTheBallOnPurpose;
        if (snapshot == null
            || action == null
            || explanation == null
            || hexGrid == null
            || matchManager.groundBallManager?.ball == null
            || ballHex == null
            || passer == null)
        {
            return;
        }

        bool isGroundPass = action.actionType == RoomActionType.GroundPass || action.actionType == RoomActionType.QuickThrow;
        explanation.isKickoffDecision |= matchManager.groundBallManager.isKickoffPass;
        bool isAttackingMovement = IsAttackingSupportMove(snapshot, action, passer);
        if (!isGroundPass && !isAttackingMovement)
        {
            return;
        }

        HexCell target = requestedTarget;
        if (target == null && explanation.targetHex != null)
        {
            target = hexGrid.GetHexCellAt(
                new UnityEngine.Vector3Int(explanation.targetHex.x, 0, explanation.targetHex.z),
                false);
        }

        if (isAttackingMovement && action.step == RoomDecisionStep.ChooseActor)
        {
            PopulateBestSupportingPassLaneForActor(snapshot, action, passer, ballHex, explanation);
            return;
        }

        if (target == null)
        {
            return;
        }

        PlayerToken receiver;
        int pickupDistance;
        bool supportingMove = isAttackingMovement;
        bool isQuickThrow = isGroundPass && (action.actionType == RoomActionType.QuickThrow
            || matchManager.groundBallManager.isQuickThrow);
        if (supportingMove)
        {
            receiver = action.actor;
            pickupDistance = 0;
        }
        else
        {
            receiver = FindBestGroundPassCollector(matchManager, snapshot, passer, target, out pickupDistance, isQuickThrow);
        }

        if (!TryEvaluateGroundPassContinuation(
            snapshot,
            passer,
            receiver,
            ballHex,
            target,
            pickupDistance,
            supportingMove,
            out float retention,
            out float immediateExpectedGoals,
            out float routeExpectedGoals,
            out int interceptionCandidates,
            out int defensiveChallenges,
            out int prePickupChallenges,
            out int remainingPace,
            out bool createsReachableShot,
            out bool createsSecondMovementShot,
            isQuickThrow))
        {
            return;
        }

        explanation.isGroundPassContinuation = true;
        explanation.isSupportingPassLaneMove = supportingMove;
        explanation.groundPassCollectorTokenKey = GetTokenKey(receiver);
        explanation.groundPassContinuationTargetHex = RoomHexCoordinates.FromHex(target);
        explanation.groundPassCollectorPickupDistance = pickupDistance;
        explanation.groundPassCollectorRemainingPace = remainingPace;
        explanation.groundPassDefensiveChallengeCount = defensiveChallenges;
        explanation.groundPassPrePickupChallengeCount = prePickupChallenges;
        explanation.groundPassRetentionProbability = retention;
        explanation.groundPassImmediateSnapshotExpectedGoals = immediateExpectedGoals;
        explanation.groundPassRouteExpectedGoals = routeExpectedGoals;
        explanation.groundPassCreatesReachableShot = createsReachableShot;
        explanation.groundPassCreatesSecondMovementShot = createsSecondMovementShot;
        explanation.groundPassCollectorNeedsSecondMovement = pickupDistance > receiver.pace;
        explanation.groundPassCollectorIsPasser = receiver == passer;
        explanation.groundPassEscapesImmediateTacklePressure = receiver == passer
            && CountOpponentsWithinTackleReach(snapshot, RoomHexCoordinates.FromHex(ballHex)) > 0
            && prePickupChallenges == 0;
        explanation.groundPassIsSafeTwoPhaseSpacePlan = pickupDistance > 0
            && prePickupChallenges == 0
            && retention >= 0.45f
            && (createsReachableShot || createsSecondMovementShot || receiver == passer);
        explanation.groundPassInterceptionCandidates = interceptionCandidates;
        explanation.estimatedBallRetentionProbability = retention;
        explanation.passRace = GetPassRace(snapshot, receiver, target, supportingMove);
        if (supportingMove)
            explanation.supportingPassLaneValueGain = SupportingPassLaneGain(snapshot, receiver, passer, ballHex,
                target, retention, routeExpectedGoals, createsReachableShot, createsSecondMovementShot, defensiveChallenges);
    }

    private static bool IsAttackingSupportMove(RoomAIDecisionSnapshot snapshot, RoomActionCandidate action, PlayerToken carrier)
    {
        return snapshot?.attackHasPossession == true
            && string.Equals(snapshot.expectedTeam, snapshot.teamInAttack, System.StringComparison.OrdinalIgnoreCase)
            && carrier != null && action?.actor != null && action.actor != carrier
            && !action.actor.IsGoalKeeper && action.actor.isHomeTeam == carrier.isHomeTeam
            && action.step is RoomDecisionStep.ChooseActor or RoomDecisionStep.ChooseTarget
            && (action.actionType == RoomActionType.StartMovement && action.manager == nameof(MovementPhaseManager)
                || action.actionType == RoomActionType.FinalThird && action.manager == nameof(FinalThirdManager));
    }

    private static IEnumerable<HexCell> GetAttackingSupportDestinations(RoomAIDecisionSnapshot snapshot, RoomActionCandidate action)
    {
        if (action.step == RoomDecisionStep.ChooseTarget)
            return action.targetHex != null ? new[] { action.targetHex } : System.Array.Empty<HexCell>();
        var match = MatchManager.Instance;
        if (action.actionType == RoomActionType.FinalThird)
            return match?.finalThirdManager?.GetMovementDestinationsForDecisionPreview(action.actor)
                ?? System.Array.Empty<HexCell>();
        if (match?.hexGrid == null || action.actor?.GetCurrentHex() == null)
            return System.Array.Empty<HexCell>();
        int range = match.movementPhaseManager?.isMovementPhase2f2 == true
            ? match.movementPhaseManager.TwoForTwoRange : action.actor.pace;
        return GetCachedReachability(snapshot, match.hexGrid, action.actor.GetCurrentHex(), range).reachableHexes
            // Goal-line targets are reachable in the geometric path map, but
            // only the ball carrier can walk into goal. An off-ball receiver
            // cannot be selected on the strength of an illegal placement.
            .Where(hex => hex != null && !hex.isOutOfBounds
                && (match.movementPhaseManager?.CanEnterMovementGoalHexForDecisionPreview(action.actor, hex) ?? hex.isInGoal == 0)
                && !hex.isAttackOccupied && !hex.isDefenseOccupied);
    }

    private static void PopulateBestSupportingPassLaneForActor(
        RoomAIDecisionSnapshot snapshot,
        RoomActionCandidate action,
        PlayerToken passer,
        HexCell ballHex,
        RoomAICandidateExplanation explanation)
    {
        var work = PopulateBestSupportingPassLaneForActorIncrementally(snapshot, action, passer, ballHex, explanation);
        while (work.MoveNext()) { }
    }

    private static IEnumerator PopulateBestSupportingPassLaneForActorIncrementally(
        RoomAIDecisionSnapshot snapshot,
        RoomActionCandidate action,
        PlayerToken passer,
        HexCell ballHex,
        RoomAICandidateExplanation explanation)
    {
        MatchManager matchManager = MatchManager.Instance;
        HexGrid hexGrid = matchManager?.hexGrid;
        PlayerToken receiver = action?.actor;
        HexCell receiverHex = receiver?.GetCurrentHex();
        if (snapshot == null
            || receiver == null
            || passer == null
            || ballHex == null
            || explanation == null
            || hexGrid == null
            || receiverHex == null)
        {
            yield break;
        }

        HexCell bestTarget = null;
        float bestRetention = -1f;
        float bestImmediateExpectedGoals = -1f;
        float bestRouteExpectedGoals = -1f;
        int bestInterceptors = 0;
        int bestChallenges = 0;
        int bestPrePickupChallenges = 0;
        bool bestCreatesShot = false;
        bool bestCreatesSecondMovementShot = false;
        float bestGain = 1f;
        foreach (HexCell target in GetAttackingSupportDestinations(snapshot, action))
        {
            yield return null;
            if (target == null
                || (!ReferenceEquals(target, receiverHex) && (target.isAttackOccupied || target.isDefenseOccupied))
                || HexGridUtils.GetHexStepDistance(ballHex, target) > 11)
            {
                continue;
            }

            if (!TryEvaluateGroundPassContinuation(
                snapshot,
                passer,
                receiver,
                ballHex,
                target,
                0,
                true,
                out float retention,
                out float immediateExpectedGoals,
                out float routeExpectedGoals,
                out int interceptionCandidates,
                out int defensiveChallenges,
                out int prePickupChallenges,
                out _,
                out bool createsShot,
                out bool createsSecondMovementShot))
            {
                continue;
            }

            float gain = SupportingPassLaneGain(snapshot, receiver, passer, ballHex, target, retention,
                routeExpectedGoals, createsShot, createsSecondMovementShot, defensiveChallenges);
            if (gain <= bestGain)
            {
                continue;
            }

            bestTarget = target;
            bestGain = gain;
            bestCreatesShot = createsShot;
            bestCreatesSecondMovementShot = createsSecondMovementShot;
            bestRetention = retention;
            bestImmediateExpectedGoals = immediateExpectedGoals;
            bestRouteExpectedGoals = routeExpectedGoals;
            bestInterceptors = interceptionCandidates;
            bestChallenges = defensiveChallenges;
            bestPrePickupChallenges = prePickupChallenges;
        }

        if (bestTarget == null)
        {
            yield break;
        }

        explanation.isGroundPassContinuation = true;
        explanation.isSupportingPassLaneMove = true;
        explanation.supportingPassLaneValueGain = bestGain;
        explanation.groundPassCollectorTokenKey = GetTokenKey(receiver);
        explanation.groundPassContinuationTargetHex = RoomHexCoordinates.FromHex(bestTarget);
        explanation.groundPassCollectorPickupDistance = 0;
        explanation.groundPassCollectorRemainingPace = receiver.pace;
        explanation.groundPassDefensiveChallengeCount = bestChallenges;
        explanation.groundPassPrePickupChallengeCount = bestPrePickupChallenges;
        explanation.groundPassRetentionProbability = bestRetention;
        explanation.groundPassImmediateSnapshotExpectedGoals = bestImmediateExpectedGoals;
        explanation.groundPassRouteExpectedGoals = bestRouteExpectedGoals;
        explanation.groundPassCreatesReachableShot = bestCreatesShot;
        explanation.groundPassCreatesSecondMovementShot = bestCreatesSecondMovementShot;
        explanation.groundPassInterceptionCandidates = bestInterceptors;
        explanation.estimatedBallRetentionProbability = bestRetention;
        explanation.passRace = GetPassRace(snapshot, receiver, bestTarget, true);
        // Actor selection and destination selection describe the same endpoint.
        explanation.targetHex = RoomHexCoordinates.FromHex(bestTarget);
        explanation.forwardProgressDelta = CalculateForwardProgressDelta(snapshot, explanation.actorHex, explanation.targetHex);
        explanation.advancesTowardOpponentGoal = explanation.forwardProgressDelta > 0;
        explanation.opponentsWithinTackleReach = CountOpponentsWithinTackleReach(snapshot, explanation.targetHex);
        explanation.bestReachableOpponentTackling = FindBestReachableOpponentTackling(snapshot, explanation.targetHex);
        explanation.tackleToDribblingMismatch = explanation.bestReachableOpponentTackling - receiver.dribbling;
        explanation.targetCanShootFrom = IsOpponentShootingHex(snapshot, bestTarget);
        explanation.targetCanHeadFrom = IsOpponentHeadingHex(snapshot, bestTarget);
    }

    private static float SupportingPassLaneGain(RoomAIDecisionSnapshot snapshot, PlayerToken receiver, PlayerToken passer,
        HexCell ballHex, HexCell target, float retention, float routeXg, bool createsShot, bool createsSecondMovementShot,
        int challenges)
    {
        if (retention < .5f && routeXg <= 0f) return 0f;
        HexCell origin = receiver.GetCurrentHex();
        string actorKey = GetTokenKey(receiver);
        if (!snapshot.supportingPassLaneBaselines.TryGetValue(actorKey, out var baseline))
        {
            bool currentLane = TryEvaluateGroundPassContinuation(snapshot, passer, receiver, ballHex, origin, 0, true,
                out float oldRetention, out _, out float oldXg, out _, out int oldChallenges, out _, out _,
                out bool oldShot, out bool oldSecondShot);
            baseline = (currentLane ? ContinuationLaneValue(oldRetention, oldXg, oldShot, oldSecondShot, oldChallenges) : 0f,
                RoomAITacticalSafety.CounterExposure(snapshot, null, null));
            snapshot.supportingPassLaneBaselines[actorKey] = baseline;
        }
        var at = RoomHexCoordinates.FromHex(target);
        int progress = CalculateForwardProgressDelta(snapshot, RoomHexCoordinates.FromHex(origin), at);
        int centralGain = System.Math.Abs(origin.coordinates.z) - System.Math.Abs(target.coordinates.z);
        float exposure = RoomAITacticalSafety.CounterExposure(snapshot, actorKey, at) - baseline.counterExposure;
        float crowding = RoomAITacticalSafety.OutletCrowdingCost(snapshot, actorKey, at)
            - RoomAITacticalSafety.OutletCrowdingCost(snapshot, actorKey, RoomHexCoordinates.FromHex(origin));
        // Reward the resulting legal outlet. Pace spent and path length have no
        // value of their own; a real delivery/shot advantage can justify width.
        return ContinuationLaneValue(retention, routeXg, createsShot, createsSecondMovementShot, challenges) - baseline.laneValue
            + retention * (progress * 6f + centralGain * 2f) - exposure * 120f - crowding;
    }

    private static float ContinuationLaneValue(float retention, float routeXg, bool createsShot, bool createsSecondMovementShot,
        int challenges)
    {
        return System.Math.Max(0f, routeXg) * 1000f + System.Math.Max(0f, retention) * 80f
            + (createsShot ? 120f : createsSecondMovementShot ? 60f : 0f) - challenges * 20f;
    }

    private static bool TryEvaluateGroundPassContinuation(
        RoomAIDecisionSnapshot snapshot,
        PlayerToken passer,
        PlayerToken receiver,
        HexCell ballHex,
        HexCell target,
        int pickupDistance,
        bool receiverWillMoveToTargetBeforePass,
        out float retentionProbability,
        out float immediateExpectedGoals,
        out float routeExpectedGoals,
        out int interceptionCandidateCount,
        out int defensiveChallengeCount,
        out int prePickupChallengeCount,
        out int remainingPace,
        out bool createsReachableShot,
        out bool createsSecondMovementShot,
        bool isQuickThrow = false)
    {
        retentionProbability = -1f;
        immediateExpectedGoals = -1f;
        routeExpectedGoals = -1f;
        interceptionCandidateCount = 0;
        defensiveChallengeCount = 0;
        prePickupChallengeCount = 0;
        remainingPace = -1;
        createsReachableShot = false;
        createsSecondMovementShot = false;

        MatchManager matchManager = MatchManager.Instance;
        HexGrid hexGrid = matchManager?.hexGrid;
        if (snapshot == null
            || passer == null
            || receiver == null
            || ballHex == null
            || target == null
            || hexGrid == null
            || receiver.isHomeTeam != passer.isHomeTeam
            || HexGridUtils.GetHexStepDistance(ballHex, target) > 11)
        {
            return false;
        }

        if (!isQuickThrow && (receiverWillMoveToTargetBeforePass
            ? IsTokenOffsideAtHex(snapshot, receiver, target)
            : matchManager.offsideManager?.IsTokenOffside(receiver) == true))
        {
            return false;
        }

        List<HexCell> path = GroundPassCommon.CalculateThickPath(
            hexGrid,
            ballHex,
            target,
            matchManager.groundBallManager.ball.ballRadius);
        IEnumerable<PlayerToken> allTokens = matchManager.playerTokenManager != null
            ? matchManager.playerTokenManager.allTokens
            : UnityEngine.Object.FindObjectsByType<PlayerToken>();
        if (allTokens.Any(token => token != null
            && token.isPlaying
            && token.isHomeTeam != passer.isHomeTeam
            && token.GetCurrentHex() != null
            && (isQuickThrow ? token.GetCurrentHex() == target : path.Contains(token.GetCurrentHex()))))
        {
            return false;
        }

        List<GroundInterceptionCandidate> interceptors = GroundPassCommon.BuildOrderedInterceptionCandidates(
            hexGrid,
            matchManager.groundBallManager.ball,
            target,
            isQuickThrow: isQuickThrow);
        interceptionCandidateCount = interceptors.Count;
        float turnoverProbability = ExpectedStatsCalculator.CalculateAtLeastOneRecoveryProbability(
            interceptors
                .Where(candidate => candidate?.DefenderToken != null)
                .Select(candidate => candidate.DefenderToken)
                .Distinct()
                .ToArray());
        retentionProbability = System.Math.Max(0f, System.Math.Min(1f, 1f - turnoverProbability));
        int collectionPaceBudget = pickupDistance > receiver.pace
            ? receiver.pace * 2
            : receiver.pace;
        remainingPace = System.Math.Max(0, collectionPaceBudget - System.Math.Max(0, pickupDistance));

        foreach (RoomAITokenSnapshot token in snapshot.tokens)
        {
            if (token == null
                || !token.isPlaying
                || token.currentHex == null
                || string.Equals(token.teamSide, receiver.isHomeTeam ? "Home" : "Away", System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            int distance = CalculateHexDistance(token.currentHex, RoomHexCoordinates.FromHex(target));
            if (distance >= 0 && distance <= token.pace + 1)
            {
                defensiveChallengeCount++;
            }

            if (pickupDistance > receiver.pace && distance >= 0 && distance <= token.pace)
            {
                prePickupChallengeCount++;
            }
        }

        // A collector who needs a second attacking movement cannot protect this
        // stationary ball during the intervening defensive movement. If a defender
        // can reach it, this is a forced turnover, not a high-retention "Sam move".
        if (pickupDistance > receiver.pace && prePickupChallengeCount > 0)
        {
            retentionProbability = 0f;
        }

        float passSurvival = retentionProbability;
        RoomAIPassRacePreview race = GetPassRace(snapshot, receiver, target, receiverWillMoveToTargetBeforePass);
        if (race != null)
        {
            remainingPace = race.remainingPace;
            retentionProbability *= race.pickupSurvival;
            if (race.opponentCollectsFirst) prePickupChallengeCount = System.Math.Max(1, prePickupChallengeCount);
        }

        if (IsOpponentShootingHex(snapshot, target)
            && matchManager.shotManager != null
            && matchManager.shotManager.TryCalculateBestExpectedGoalsPreview(
                receiver,
                target,
                "snapshot",
                out float expectedGoals,
                out _,
                out _,
                out _))
        {
            immediateExpectedGoals = expectedGoals;
            routeExpectedGoals = retentionProbability * expectedGoals;
            createsReachableShot = true;
            return true;
        }

        int minimumShotDistance = int.MaxValue;
        // The pitch's shooting cells do not change between target candidates
        // in one decision. Preserve the same complete set while avoiding a
        // fresh full-grid scan for each possible pass destination.
        if (snapshot.continuationShootingHexes == null)
        {
            snapshot.continuationShootingHexes = new List<HexCell>();
            foreach (HexCell candidate in hexGrid.cells)
                if (candidate != null && IsOpponentShootingHex(snapshot, candidate))
                    snapshot.continuationShootingHexes.Add(candidate);
        }
        foreach (HexCell candidate in snapshot.continuationShootingHexes)
            minimumShotDistance = System.Math.Min(minimumShotDistance, HexGridUtils.GetHexStepDistance(target, candidate));

        if (race != null)
        {
            retentionProbability *= race.continuationSurvival;
            routeExpectedGoals = race.routeExpectedGoals < 0f ? -1f : passSurvival * race.routeExpectedGoals;
        }
        createsReachableShot = race?.routeExpectedGoals > 0f
            || race == null && minimumShotDistance != int.MaxValue && minimumShotDistance <= remainingPace;
        createsSecondMovementShot = !createsReachableShot
            && minimumShotDistance != int.MaxValue
            && minimumShotDistance <= remainingPace + receiver.pace;
        return true;
    }

    public static RoomAIPassRacePreview GetPassRace(RoomAIDecisionSnapshot snapshot, PlayerToken collector,
        HexCell landing, bool alreadyOnLanding)
    {
        if (snapshot == null || collector == null || landing == null) return null;
        string key = $"{GetTokenKey(collector)}|{landing.coordinates.x}:{landing.coordinates.z}|{alreadyOnLanding}";
        if (!snapshot.passRaceCache.TryGetValue(key, out RoomAIPassRacePreview result))
        {
            result = RoomAIPassRace.Evaluate(snapshot, collector, landing, alreadyOnLanding);
            snapshot.passRaceCache[key] = result;
        }
        return result;
    }

    private static PlayerToken FindBestGroundPassCollector(MatchManager matchManager,
        RoomAIDecisionSnapshot snapshot, PlayerToken passer, HexCell target, out int pickupDistance,
        bool isQuickThrow = false)
    {
        pickupDistance = -1;
        if (matchManager?.hexGrid == null || passer == null || target == null) return null;
        PlayerToken occupant = target.GetOccupyingToken();
        if (occupant != null && occupant.isHomeTeam == passer.isHomeTeam)
        { pickupDistance = 0; return occupant; }
        // Collection timing dominates speculative shooting and self-pass bonuses.
        var collectors = matchManager.playerTokenManager.allTokens
            .Where(token => token != null && token.isPlaying && token.isHomeTeam == passer.isHomeTeam
                && token.GetCurrentHex() != null && matchManager.CanTokenCollectHangingPass(token)
                && (isQuickThrow || matchManager.offsideManager?.IsTokenOffside(token) != true))
            .Select(token => new { token, distance = target.isOutOfBounds
                ? HexGridUtils.FindPath(token.GetCurrentHex(), target, matchManager.hexGrid).Count
                : GetCachedReachability(snapshot, matchManager.hexGrid, token.GetCurrentHex(), token.pace * 2)
                    .distances.TryGetValue(target, out var reach) ? reach.distance : -1 })
            .Where(item => item.distance > 0 && item.distance <= item.token.pace * 2)
            .OrderBy(item => (item.distance + System.Math.Max(1, item.token.pace) - 1) / System.Math.Max(1, item.token.pace))
            .ThenBy(item => item.distance)
            .ThenBy(item => item.token.IsGoalKeeper)
            .ThenByDescending(item => item.token.dribbling)
            .ThenBy(item => GetTokenKey(item.token), System.StringComparer.Ordinal)
            .FirstOrDefault();
        if (collectors == null) return null;
        pickupDistance = collectors.distance;
        return collectors.token;
    }

    private static bool IsTokenOffsideAtHex(
        RoomAIDecisionSnapshot snapshot,
        PlayerToken receiver,
        HexCell proposedHex)
    {
        if (snapshot == null || receiver == null || proposedHex == null)
        {
            return false;
        }

        bool leftToRight = receiver.isHomeTeam
            ? string.Equals(snapshot.homeTeamDirection, "LeftToRight", System.StringComparison.OrdinalIgnoreCase)
            : string.Equals(snapshot.awayTeamDirection, "LeftToRight", System.StringComparison.OrdinalIgnoreCase);
        List<int> defenderColumns = snapshot.tokens
            .Where(token => token != null
                && token.isPlaying
                && token.currentHex != null
                && !string.Equals(token.teamSide, receiver.isHomeTeam ? "Home" : "Away", System.StringComparison.OrdinalIgnoreCase))
            .Select(token => token.currentHex.x)
            .OrderBy(value => value)
            .ToList();
        if (defenderColumns.Count < 2)
        {
            return false;
        }

        int secondDefenderX = leftToRight
            ? defenderColumns[defenderColumns.Count - 2]
            : defenderColumns[1];
        int ballX = snapshot.ball?.x ?? 0;
        int offsideLineX = leftToRight
            ? UnityEngine.Mathf.Max(0, UnityEngine.Mathf.Max(ballX, secondDefenderX))
            : UnityEngine.Mathf.Min(0, UnityEngine.Mathf.Min(ballX, secondDefenderX));
        return leftToRight
            ? proposedHex.coordinates.x > offsideLineX
            : proposedHex.coordinates.x < offsideLineX;
    }

    private static void PopulateRegularHighPassOutcomeFacts(
        RoomAIDecisionSnapshot snapshot,
        RoomActionCandidate action,
        RoomAICandidateExplanation explanation,
        HexCell requestedTarget)
    {
        var work = PopulateRegularHighPassOutcomeFactsIncrementally(snapshot, action, explanation, requestedTarget);
        while (work.MoveNext()) { }
    }

    private static IEnumerator PopulateRegularHighPassOutcomeFactsIncrementally(
        RoomAIDecisionSnapshot snapshot,
        RoomActionCandidate action,
        RoomAICandidateExplanation explanation,
        HexCell requestedTarget)
    {
        if (snapshot == null
            || action == null
            || explanation == null
            || action.actionType != RoomActionType.HighPass && action.actionType != RoomActionType.GoalkeeperKick)
        {
            yield break;
        }

        MatchManager matchManager = MatchManager.Instance;
        HighPassManager highPass = matchManager?.highPassManager;
        PlayerToken passer = matchManager?.ball?.GetCurrentHex()?.GetOccupyingToken()
            ?? matchManager?.LastTokenToTouchTheBallOnPurpose;
        if (matchManager?.hexGrid == null || highPass == null || passer == null)
        {
            yield break;
        }

        explanation.highPassAccuracyProbability = System.Math.Max(
            0f,
            System.Math.Min(1f, (passer.highPass - 1) / 6f));
        explanation.highPassTargetEvaluationComplete = highPass.IsAvailableTargetEvaluationComplete;

        IReadOnlyList<HexCell> evaluatedTargets = requestedTarget != null
            ? new[] { requestedTarget }
            : highPass.GetEvaluatedDecisionTargets();
        if (requestedTarget == null && !explanation.highPassTargetEvaluationComplete)
        {
            yield break;
        }

        List<PlayerToken> allTokens = (matchManager.playerTokenManager != null
            ? matchManager.playerTokenManager.allTokens
            : (IEnumerable<PlayerToken>)UnityEngine.Object.FindObjectsByType<PlayerToken>())
            .Where(token => token != null && token.isPlaying && token.GetCurrentHex() != null)
            .ToList();
        List<PlayerToken> teammates = allTokens
            .Where(token => token != null
                && token != passer
                && token.isPlaying
                && token.isHomeTeam == passer.isHomeTeam
                && token.GetCurrentHex() != null
                && highPass.CanAttackerPrepareHighPassTargetForDecisionPreview(token)
                && (snapshot.firstAerialOffsideExempt || matchManager.offsideManager?.IsTokenOffside(token) != true))
            .ToList();

        float bestValue = float.MinValue;
        float bestPossession = -1f;
        HexCell bestTarget = null;
        PlayerToken bestCollector = null;
        RoomAIPassRacePreview bestRace = null;
        foreach (HexCell target in evaluatedTargets)
        {
            yield return null;
            if (target == null || target.isOutOfBounds)
            {
                continue;
            }

            explanation.highPassTargetsEvaluated++;
            // A free landing requires an actual unblocked three-hex HP move;
            // geometric proximity alone can nominate an unreachable receiver.
            PlayerToken collector = teammates
                .Where(token => CanPrepareHighPassLanding(snapshot, highPass, token, target))
                .OrderByDescending(token => token.GetCurrentHex() == target)
                .ThenByDescending(token => token.heading)
                .ThenByDescending(token => token.dribbling)
                .ThenBy(token => HexGridUtils.GetHexStepDistance(token.GetCurrentHex(), target))
                .FirstOrDefault();
            if (collector == null)
            {
                continue;
            }

            int collectorDistance = collector.GetCurrentHex() == target ? 0
                : GetCachedReachability(snapshot, matchManager.hexGrid, collector.GetCurrentHex(), HighPassManager.AttackerMoveRange)
                    .distances[target].distance;
            int support = teammates.Count(token =>
                token != collector
                && HexGridUtils.GetHexStepDistance(token.GetCurrentHex(), target) <= 3);
            var defenders = allTokens.Where(token => token.isHomeTeam != passer.isHomeTeam
                && matchManager.CanTokenCollectHangingPass(token)).ToList();
            int contestingDefenders = defenders.Count(token =>
                HexGridUtils.GetHexStepDistance(token.GetCurrentHex(), target) <= HeaderManager.HEADER_SELECTION_RANGE);
            // Only one defender can use the HP free move. Count one additional
            // outfield contestant if a legal route reaches header range after
            // the attacker's landing; do not award each nearby defender a move.
            if (defenders.Any(token => CanEnterHighPassHeaderContest(snapshot, token, target))) contestingDefenders++;
            float inaccurateRecovery = System.Math.Min(0.35f, 0.08f + (support * 0.07f));
            float accurateCollectionProbability = System.Math.Max(
                0.15f,
                System.Math.Min(
                    0.92f,
                    0.46f
                    + (collector.heading * 0.055f)
                    + (collector.dribbling * 0.025f)
                    - (collectorDistance * 0.12f)
                    - (contestingDefenders * 0.10f)));
            float possessionProbability = (explanation.highPassAccuracyProbability * accurateCollectionProbability)
                + ((1f - explanation.highPassAccuracyProbability) * inaccurateRecovery);
            int progress = CalculateForwardProgressDelta(
                snapshot,
                RoomHexCoordinates.FromHex(matchManager.ball.GetCurrentHex()),
                RoomHexCoordinates.FromHex(target));
            float value = (possessionProbability * 100f)
                + (progress * 7f)
                + (collector.heading * (target.CanHeadFrom ? 9f : 2f))
                + (collector.dribbling * 3f)
                + (support * 5f)
                - (contestingDefenders * 16f)
                + (collectorDistance == 0 && target.CanShootFrom ? 70f : 0f)
                + (collectorDistance == 0 && target.CanHeadFrom ? 45f : 0f);
            if (value <= bestValue)
            {
                continue;
            }

            // Collection survival can only reduce this upper bound. Skip the
            // expensive race when even perfect survival cannot beat the incumbent.
            RoomAIPassRacePreview postCollection = GetPassRace(snapshot, collector, target, true);
            if (postCollection != null)
            {
                value -= possessionProbability * (1f - postCollection.continuationSurvival) * 100f;
                possessionProbability *= postCollection.continuationSurvival;
            }
            if (value <= bestValue) continue;

            bestValue = value;
            bestPossession = possessionProbability;
            bestTarget = target;
            bestCollector = collector;
            bestRace = postCollection;
            explanation.highPassCollectorStartsOnTarget = collectorDistance == 0;
            explanation.highPassCollectorDistance = collectorDistance;
            explanation.highPassDefendersContesting = contestingDefenders;
        }

        if (bestTarget == null)
        {
            yield break;
        }

        explanation.highPassBestTargetHex = RoomHexCoordinates.FromHex(bestTarget);
        explanation.highPassBestCollectorTokenKey = GetTokenKey(bestCollector);
        explanation.passRace = bestRace;
        explanation.highPassExpectedPossessionProbability = bestPossession;
        explanation.highPassTargetOutcomeValue = bestValue;
        if (requestedTarget == null)
        {
            explanation.targetHex = explanation.highPassBestTargetHex;
            explanation.targetTokenKey = explanation.highPassBestCollectorTokenKey;
            explanation.targetTokenHex = RoomHexCoordinates.FromHex(bestCollector?.GetCurrentHex());
            explanation.targetToken = ResolveTokenSnapshot(
                explanation.highPassBestCollectorTokenKey,
                BuildTokenLookup(snapshot.tokens));
            explanation.forwardProgressDelta = CalculateForwardProgressDelta(
                snapshot,
                RoomHexCoordinates.FromHex(matchManager.ball.GetCurrentHex()),
                explanation.highPassBestTargetHex);
            explanation.advancesTowardOpponentGoal = explanation.forwardProgressDelta > 0;
            explanation.targetCanShootFrom = explanation.highPassCollectorStartsOnTarget
                && IsOpponentShootingHex(snapshot, bestTarget);
            explanation.targetCanHeadFrom = explanation.highPassCollectorStartsOnTarget
                && IsOpponentHeadingHex(snapshot, bestTarget);
        }
    }

    private static bool CanPrepareHighPassLanding(RoomAIDecisionSnapshot snapshot, HighPassManager highPass,
        PlayerToken token, HexCell target)
    {
        var grid = MatchManager.Instance?.hexGrid;
        if (snapshot == null || highPass == null || token?.GetCurrentHex() == null || target == null
            || grid == null || !highPass.CanAttackerPrepareHighPassTargetForDecisionPreview(token)) return false;
        if (token.GetCurrentHex() == target) return true;
        if (target.isAttackOccupied || target.isDefenseOccupied) return false;
        return GetCachedReachability(snapshot, grid, token.GetCurrentHex(), HighPassManager.AttackerMoveRange)
            .reachableHexes.Contains(target);
    }

    private static bool CanEnterHighPassHeaderContest(RoomAIDecisionSnapshot snapshot, PlayerToken defender, HexCell target)
    {
        var grid = MatchManager.Instance?.hexGrid;
        var start = defender?.GetCurrentHex();
        if (snapshot == null || grid == null || start == null || target == null || defender.IsGoalKeeper) return false;
        int distance = HexGridUtils.GetHexStepDistance(start, target);
        if (distance <= HeaderManager.HEADER_SELECTION_RANGE
            || distance > HeaderManager.HEADER_SELECTION_RANGE + HighPassManager.DefenderMoveRange) return false;
        var reach = GetCachedReachability(snapshot, grid, start, HighPassManager.DefenderMoveRange);
        foreach (var hex in reach.reachableHexes)
        {
            if (hex == null || hex == target || hex.isOutOfBounds || hex.isInGoal != 0
                || hex.isAttackOccupied || hex.isDefenseOccupied
                || HexGridUtils.GetHexStepDistance(hex, target) > HeaderManager.HEADER_SELECTION_RANGE) continue;
            // This defender starts at least three steps from the landing. A
            // three-step path to a different destination cannot pass through it.
            return true;
        }
        return false;
    }

    private static void PopulateRegularFinalThirdFacts(RoomAIDecisionSnapshot snapshot,
        RoomActionCandidate action, RoomAICandidateExplanation explanation, HexCell proposedHex)
    {
        if (snapshot == null || action == null || explanation == null || explanation.isSetPieceSetup
            || action.actionType != RoomActionType.FinalThird
            || (snapshot.expectedTeam == snapshot.teamInAttack
                && !OpponentCanCollectBeforeAttacker(snapshot, action, proposedHex))) return;
        explanation.isRegularFinalThirdReorganization = true;
        RoomAITacticalSafety.EvaluateDefensiveLine(snapshot, explanation);
        if (explanation.actorToken?.isGoalKeeper == true && proposedHex != null)
            explanation.goalkeeperOwnGoalDistanceDelta = CalculateDistanceToOwnGoalCenter(snapshot, proposedHex)
                - CalculateDistanceToOwnGoalCenter(snapshot, action.actor?.GetCurrentHex());
    }

    private static bool OpponentCanCollectBeforeAttacker(RoomAIDecisionSnapshot snapshot,
        RoomActionCandidate action, HexCell proposedHex)
    {
        var match = MatchManager.Instance;
        var ball = match?.ball?.GetCurrentHex();
        var grid = match?.hexGrid;
        if (snapshot.attackHasPossession || ball == null || grid == null
            || match.playerTokenManager?.allTokens == null || ball.GetOccupyingToken() != null) return false;
        var actor = action?.actor ?? action?.targetToken;
        var players = match.playerTokenManager.allTokens.Where(t => t != null && t.isPlaying && !t.isSentOff
            && t.GetCurrentHex() != null && match.CanTokenCollectHangingPass(t)).ToList();
        bool Own(PlayerToken token) => (token.isHomeTeam ? "Home" : "Away") == snapshot.expectedTeam;
        // If a teammate could geometrically reach the ball in the first
        // attacking movement, do not assume that the opponent collects first.
        if (players.Where(Own).Any(t => HexGridUtils.GetHexStepDistance(
                t == actor && proposedHex != null ? proposedHex : t.GetCurrentHex(), ball) <= t.pace)) return false;
        foreach (var opponent in players.Where(t => !Own(t)))
        {
            if (match.movementPhaseManager?.stunnedTokens.Contains(opponent) == true
                || match.movementPhaseManager?.stunnedforNext.Contains(opponent) == true
                || match.movementPhaseManager?.headerManager?.attackerWillJump.Contains(opponent) == true
                || match.movementPhaseManager?.headerManager?.defenderWillJump.Contains(opponent) == true) continue;
            if (HexGridUtils.GetHexStepDistance(opponent.GetCurrentHex(), ball) > opponent.pace) continue;
            if (!snapshot.f3OpponentCollectionPaths.TryGetValue(opponent, out var path))
            {
                path = HexGridUtils.FindPath(opponent.GetCurrentHex(), ball, grid);
                snapshot.f3OpponentCollectionPaths[opponent] = path;
            }
            if (path == null || path.Count == 0 || path.Count > opponent.pace) continue;
            // Do not assume a path remains open if the proposed F3 placement
            // itself occupies it. A later full response can then resolve it.
            if (proposedHex != null && path.Contains(proposedHex)) continue;
            return true;
        }
        return false;
    }

    private static int CalculateDistanceToOwnGoalCenter(RoomAIDecisionSnapshot snapshot, HexCell origin)
    {
        HexGrid grid = MatchManager.Instance?.hexGrid;
        if (snapshot == null || origin == null || grid?.cells == null)
        {
            return 0;
        }

        int ownGoalSide = -ResolveExpectedTeamAttackingSide(snapshot);
        int best = int.MaxValue;
        foreach (HexCell hex in grid.cells)
        {
            if (hex != null && hex.isInGoal == ownGoalSide)
            {
                best = System.Math.Min(best, HexGridUtils.GetHexStepDistance(origin, hex));
            }
        }

        return best == int.MaxValue ? 0 : best;
    }

    private sealed class DribbleRoutePreview
    {
        public readonly List<HexCell> path = new();
        public HexCell destination;
        public int progress;
        public float reachProbability;
        public float turnoverProbability;
        public float foulProbability;
        public float expectedGoals = -1f;
        public int utility;
        public int pressure;
    }

    private sealed class DribbleEndpointPreview
    {
        public int pressure;
        public int support;
        public float expectedGoals = -1f;
    }

    private sealed class DribbleRouteLabel
    {
        public readonly List<HexCell> path = new();
        public readonly HashSet<HexCell> visited = new();
        public HexCell current;
    }

    private const int MaxDribbleLabelsPerState = 3;
    private const int MaxDribbleRouteExpansions = 4096;

    /// <summary>
    /// The rules engine requests dribble input one adjacent hex at a time. Tactical
    /// evaluation must nevertheless choose the destination and route over all of
    /// the remaining Pace, otherwise equally-valued adjacent clicks can oscillate.
    /// </summary>
    private static void PopulateFullPaceDribbleRouteFacts(RoomAIDecisionSnapshot snapshot)
    {
        var work = PopulateFullPaceDribbleRouteFactsIncrementally(snapshot);
        while (work.MoveNext()) { }
    }

    private static IEnumerator PopulateFullPaceDribbleRouteFactsIncrementally(RoomAIDecisionSnapshot snapshot)
    {
        MatchManager matchManager = MatchManager.Instance;
        MovementPhaseManager movement = matchManager?.movementPhaseManager;
        HexGrid grid = matchManager?.hexGrid;
        PlayerToken dribbler = movement?.selectedToken;
        HexCell start = dribbler?.GetCurrentHex();
        int remainingPace = movement != null ? System.Math.Max(0, movement.remainingDribblerPace) : 0;
        if (snapshot == null
            || grid == null
            || movement == null
            || !(movement.isDribblerRunning || dribbler?.IsDribbler == true && movement.isAwaitingHexDestination)
            || dribbler == null
            || start == null
            || remainingPace <= 0)
        {
            yield break;
        }

        Dictionary<HexCell, RoomAICandidateExplanation> firstStepExplanations =
            new Dictionary<HexCell, RoomAICandidateExplanation>();
        foreach (RoomAICandidateExplanation explanation in snapshot.candidateExplanations)
        {
            if (explanation?.candidate?.actionType == RoomActionType.StartMovement
                && explanation.candidate.step == RoomDecisionStep.ChooseTarget
                && explanation.candidate.targetHex != null)
            {
                firstStepExplanations[explanation.candidate.targetHex] = explanation;
            }
        }

        if (firstStepExplanations.Count == 0)
        {
            yield break;
        }

        Dictionary<HexCell, DribbleRoutePreview> bestByFirstStep =
            new Dictionary<HexCell, DribbleRoutePreview>();
        Dictionary<(HexCell firstStep, HexCell destination, int steps), List<DribbleRoutePreview>> labelsByState =
            new Dictionary<(HexCell firstStep, HexCell destination, int steps), List<DribbleRoutePreview>>();
        Queue<DribbleRouteLabel> frontier = new Queue<DribbleRouteLabel>();
        int routesEvaluated = 0;
        var endpoints = new Dictionary<HexCell, DribbleEndpointPreview>();

        foreach (HexCell firstStep in firstStepExplanations.Keys)
        {
            if (firstStep == null
                || firstStep.isOutOfBounds
                || (firstStep.GetOccupyingToken() != null && firstStep.GetOccupyingToken() != dribbler))
            {
                continue;
            }

            DribbleRouteLabel initial = new DribbleRouteLabel { current = firstStep };
            initial.path.Add(firstStep);
            initial.visited.Add(start);
            initial.visited.Add(firstStep);
            frontier.Enqueue(initial);
        }

        while (frontier.Count > 0 && routesEvaluated < MaxDribbleRouteExpansions)
        {
            DribbleRouteLabel label = frontier.Dequeue();
            routesEvaluated++;
            yield return null;
            DribbleRoutePreview preview = EvaluateDribbleRoute(
                snapshot,
                movement,
                dribbler,
                start,
                label.path,
                endpoints);
            if (preview == null)
            {
                continue;
            }

            HexCell firstStep = label.path[0];
            if (!bestByFirstStep.TryGetValue(firstStep, out DribbleRoutePreview incumbent)
                || IsBetterDribbleRoute(preview, incumbent))
            {
                // Every retained partial route is a genuine outcome: the dribbler
                // may stop here and forfeit unused Pace when another step is worse.
                bestByFirstStep[firstStep] = preview;
            }

            var stateKey = (firstStep, label.current, label.path.Count);
            if (!TryRetainDribbleLabel(labelsByState, stateKey, preview))
            {
                continue;
            }

            if (label.path.Count >= remainingPace)
            {
                continue;
            }

            foreach (HexCell next in label.current.GetNeighbors(grid))
            {
                if (next == null
                    || next.isOutOfBounds
                    || label.visited.Contains(next)
                    || (next.GetOccupyingToken() != null && next.GetOccupyingToken() != dribbler))
                {
                    continue;
                }

                DribbleRouteLabel nextLabel = new DribbleRouteLabel { current = next };
                nextLabel.path.AddRange(label.path);
                nextLabel.path.Add(next);
                nextLabel.visited.UnionWith(label.visited);
                nextLabel.visited.Add(next);
                frontier.Enqueue(nextLabel);
            }
        }

        foreach (KeyValuePair<HexCell, RoomAICandidateExplanation> entry in firstStepExplanations)
        {
            if (!bestByFirstStep.TryGetValue(entry.Key, out DribbleRoutePreview preview))
            {
                continue;
            }

            RoomAICandidateExplanation explanation = entry.Value;
            explanation.isFullPaceDribbleRoute = true;
            explanation.fullPaceDribbleDestination = RoomHexCoordinates.FromHex(preview.destination);
            explanation.fullPaceDribbleSteps = preview.path.Count;
            explanation.fullPaceDribbleProgress = preview.progress;
            explanation.fullPaceDribbleEndpointPressure = preview.pressure;
            explanation.fullPaceDribbleReachProbability = preview.reachProbability;
            explanation.fullPaceDribbleTurnoverProbability = preview.turnoverProbability;
            explanation.fullPaceDribbleFoulProbability = preview.foulProbability;
            explanation.fullPaceDribbleExpectedGoals = preview.expectedGoals;
            explanation.fullPaceDribbleUtility = preview.utility;
            explanation.fullPaceDribbleRoutesEvaluated = routesEvaluated;
            explanation.fullPaceDribblePath.Clear();
            explanation.fullPaceDribblePath.AddRange(
                preview.path.Select(RoomHexCoordinates.FromHex));
        }
    }

    private static bool TryRetainDribbleLabel(
        Dictionary<(HexCell firstStep, HexCell destination, int steps), List<DribbleRoutePreview>> labelsByState,
        (HexCell firstStep, HexCell destination, int steps) stateKey,
        DribbleRoutePreview candidate)
    {
        if (!labelsByState.TryGetValue(stateKey, out List<DribbleRoutePreview> labels))
        {
            labels = new List<DribbleRoutePreview>();
            labelsByState[stateKey] = labels;
        }

        labels.Add(candidate);
        labels.Sort((left, right) => IsBetterDribbleRoute(left, right) ? -1 : IsBetterDribbleRoute(right, left) ? 1 : 0);
        bool retained = labels.IndexOf(candidate) < MaxDribbleLabelsPerState;
        if (labels.Count > MaxDribbleLabelsPerState)
        {
            labels.RemoveRange(MaxDribbleLabelsPerState, labels.Count - MaxDribbleLabelsPerState);
        }

        return retained;
    }

    private static DribbleRoutePreview EvaluateDribbleRoute(
        RoomAIDecisionSnapshot snapshot,
        MovementPhaseManager movement,
        PlayerToken dribbler,
        HexCell start,
        IReadOnlyList<HexCell> path,
        Dictionary<HexCell, DribbleEndpointPreview> endpoints)
    {
        if (path == null || path.Count == 0)
        {
            return null;
        }

        HexCell destination = path[path.Count - 1];
        CalculateMovementRouteProbabilities(
            movement,
            path,
            out float turnoverProbability,
            out float foulProbability,
            out float reachProbability);
        int progress = CalculateForwardProgressDelta(
            snapshot,
            RoomHexCoordinates.FromHex(start),
            RoomHexCoordinates.FromHex(destination));
        // Hundreds of paths can end on the same hex. Endpoint pressure, support
        // and shooting quality depend on the board, not the path taken there.
        if (!endpoints.TryGetValue(destination, out var endpoint))
        {
            endpoint = new DribbleEndpointPreview();
            var coordinates = RoomHexCoordinates.FromHex(destination);
            endpoint.pressure = CountOpponentsWithinTackleReach(snapshot, coordinates);
            var token = snapshot.tokens.FirstOrDefault(item => item.tokenKey == GetTokenKey(dribbler));
            endpoint.support = CountFriendlySupportWithinRange(snapshot, coordinates, token);
            if (destination.CanShootFrom)
                TryGetCachedExpectedGoalsPreview(snapshot, MatchManager.Instance?.shotManager, dribbler,
                    destination, "snapshot", out endpoint.expectedGoals, out _, out _, out _);
            endpoints[destination] = endpoint;
        }
        int pressure = endpoint.pressure;
        int support = endpoint.support;
        float expectedGoals = endpoint.expectedGoals;

        int utility = (int)System.Math.Round(
            (reachProbability * 100f)
            - (turnoverProbability * 220f)
            + (foulProbability * 35f)
            + (progress * 12f)
            + (System.Math.Min(3, support) * 5f)
            - (pressure * 28f)
            + (System.Math.Max(0f, expectedGoals) * 500f));
        DribbleRoutePreview preview = new DribbleRoutePreview
        {
            destination = destination,
            progress = progress,
            reachProbability = reachProbability,
            turnoverProbability = turnoverProbability,
            foulProbability = foulProbability,
            expectedGoals = expectedGoals,
            utility = utility,
            pressure = pressure
        };
        preview.path.AddRange(path);
        return preview;
    }

    private static bool IsBetterDribbleRoute(
        DribbleRoutePreview candidate,
        DribbleRoutePreview incumbent)
    {
        // Extra support is a tie-breaker when both routes are unopposed. It must
        // not spend a forward column without buying safety or a better finish.
        bool equallySafe = candidate.turnoverProbability <= 0.0001f && incumbent.turnoverProbability <= 0.0001f
            && candidate.reachProbability >= 0.9999f && incumbent.reachProbability >= 0.9999f
            && candidate.pressure == 0 && incumbent.pressure == 0
            && System.Math.Abs(System.Math.Max(0f, candidate.expectedGoals) - System.Math.Max(0f, incumbent.expectedGoals)) < 0.01f;
        if (equallySafe && candidate.progress != incumbent.progress)
            return candidate.progress > incumbent.progress;
        if (candidate.utility != incumbent.utility)
        {
            return candidate.utility > incumbent.utility;
        }

        if (System.Math.Abs(candidate.turnoverProbability - incumbent.turnoverProbability) > 0.0001f)
        {
            return candidate.turnoverProbability < incumbent.turnoverProbability;
        }

        return candidate.path.Count < incumbent.path.Count;
    }

    private static void PopulateDribbleSnapshotRouteFacts(
        RoomAIDecisionSnapshot snapshot,
        RoomActionCandidate action,
        RoomAICandidateExplanation explanation,
        HexCell proposedHex)
    {
        if (!IsBallCarrierMovementTarget(action)
            || snapshot == null
            || explanation == null
            || proposedHex == null)
        {
            return;
        }

        MatchManager matchManager = MatchManager.Instance;
        MovementPhaseManager movement = matchManager?.movementPhaseManager;
        ShotManager shotManager = matchManager?.shotManager;
        PlayerToken shooter = movement?.selectedToken;
        if (matchManager?.hexGrid == null || movement == null || shotManager == null || shooter == null)
        {
            return;
        }

        int paceAfterProposedStep = System.Math.Max(0, movement.remainingDribblerPace - 1);
        RoomAIReachabilityPreview reachablePreview = GetCachedReachability(
            snapshot,
            matchManager.hexGrid,
            proposedHex,
            paceAfterProposedStep);
        List<HexCell> reachableHexes = reachablePreview.reachableHexes;
        HashSet<HexCell> destinations = new HashSet<HexCell>(reachableHexes ?? new List<HexCell>())
        {
            proposedHex
        };
        List<(HexCell hex, float rawXg, float reachProbability, float routeXg)> frontier =
            new List<(HexCell hex, float rawXg, float reachProbability, float routeXg)>();
        HexCell shooterHex = shooter.GetCurrentHex();
        foreach (HexCell destination in destinations)
        {
            if (destination == null
                || destination.isOutOfBounds
                || (!ReferenceEquals(destination, proposedHex)
                    && (destination.isAttackOccupied || destination.isDefenseOccupied))
                || !IsOpponentPenaltyBox(snapshot, destination))
            {
                continue;
            }

            if (!TryGetCachedExpectedGoalsPreview(
                    snapshot,
                    shotManager,
                    shooter,
                    destination,
                    "snapshot",
                    out float expectedGoals,
                    out _,
                    out _,
                    out _))
            {
                continue;
            }

            List<HexCell> route = new List<HexCell> { proposedHex };
            if (!ReferenceEquals(destination, proposedHex))
            {
                HashSet<HexCell> vacated = shooterHex != null
                    ? new HashSet<HexCell> { shooterHex }
                    : new HashSet<HexCell>();
                List<HexCell> remainder = HexGridUtils.FindPath(
                    proposedHex,
                    destination,
                    matchManager.hexGrid,
                    vacated);
                if (remainder == null || remainder.Count == 0 || remainder.Count > paceAfterProposedStep)
                {
                    continue;
                }

                route.AddRange(remainder);
            }

            CalculateMovementRouteProbabilities(
                movement,
                route,
                out float turnoverProbability,
                out _,
                out float reachProbability);
            float routeExpectedGoals = reachProbability * expectedGoals;
            frontier.Add((destination, expectedGoals, reachProbability, routeExpectedGoals));
            if (ReferenceEquals(destination, proposedHex))
            {
                explanation.immediateSnapshotExpectedGoals = routeExpectedGoals;
            }

            if (routeExpectedGoals > explanation.bestReachableSnapshotExpectedGoals)
            {
                explanation.bestReachableSnapshotExpectedGoals = routeExpectedGoals;
                explanation.bestReachableSnapshotRawExpectedGoals = expectedGoals;
                explanation.bestReachableSnapshotReachProbability = reachProbability;
                explanation.bestReachableSnapshotTurnoverProbability = turnoverProbability;
                explanation.bestReachableSnapshotHex = RoomHexCoordinates.FromHex(destination);
            }
        }

        frontier = frontier
            .OrderByDescending(entry => entry.routeXg)
            .ThenBy(entry => entry.hex.coordinates.x)
            .ThenBy(entry => entry.hex.coordinates.z)
            .ToList();
        explanation.reachableSnapshotHexesEvaluated = frontier.Count;
        explanation.reachableSnapshotExpectedGoalsFrontier = string.Join(
            ", ",
            frontier.Take(16).Select(entry =>
                $"{entry.hex.coordinates}:route={entry.routeXg:0.000},raw={entry.rawXg:0.000},reach={entry.reachProbability:0.000}"));
    }

    private static void PopulateTackleDecisionFacts(
        RoomAIDecisionSnapshot snapshot,
        RoomActionCandidate action,
        RoomAICandidateExplanation explanation)
    {
        if (snapshot == null
            || action == null
            || explanation == null
            || !string.Equals(action.manager, nameof(MovementPhaseManager), System.StringComparison.Ordinal)
            || action.step != RoomDecisionStep.InterruptionChoice
            || (action.actionType != RoomActionType.Tackle
                && action.actionType != RoomActionType.Decline)
            || (action.executionCommand != "tackle"
                && action.executionCommand != "no_tackle"))
        {
            return;
        }

        MatchManager matchManager = MatchManager.Instance;
        MovementPhaseManager movement = matchManager?.movementPhaseManager;
        PlayerToken defender = movement?.selectedDefender ?? movement?.selectedToken;
        HexCell carrierHex = matchManager?.ball?.GetCurrentHex();
        PlayerToken dribbler = carrierHex?.GetOccupyingToken()
            ?? matchManager?.LastTokenToTouchTheBallOnPurpose;
        HexCell defenderHex = defender?.GetCurrentHex();
        if (matchManager?.hexGrid == null
            || movement == null
            || defender == null
            || dribbler == null
            || carrierHex == null
            || defenderHex == null)
        {
            return;
        }

        MatchManager.TeamAttackingDirection direction = dribbler.isHomeTeam
            ? matchManager.homeTeamDirection
            : matchManager.awayTeamDirection;
        bool fromBehind = carrierHex.IsDangerousTacklingPosition(direction, defenderHex);
        int foulThreshold = fromBehind ? 2 : 1;
        ExpectedStatsCalculator.GroundDuelExpectation duel =
            ExpectedStatsCalculator.CalculateGroundDuelExpectationWithDefenderSkill(
                dribbler,
                defender,
                defender.tackling,
                defenderBonusMalus: 0,
                defenderFoulThreshold: foulThreshold);

        float leniencyCardProbability = CalculateBookingProbability(matchManager.refereeLeniency);
        float sendOffWhenFoul = defender.isBooked
            ? (fromBehind ? 1f : leniencyCardProbability)
            : fromBehind ? leniencyCardProbability : 0f;

        explanation.isTackleDecision = true;
        explanation.tackleIsAttempted = action.actionType == RoomActionType.Tackle;
        explanation.tackleIsFromBehind = fromBehind;
        explanation.tackleDefenderIsBooked = defender.isBooked;
        explanation.tackleSuccessProbability = duel.xTackles;
        explanation.tackleFoulProbability = duel.defenderFoulProbability;
        explanation.tackleSendOffProbability = duel.defenderFoulProbability * sendOffWhenFoul;
        explanation.tackleCardProbability = duel.defenderFoulProbability * (fromBehind ? 1f : leniencyCardProbability);
        explanation.dribblerRetainsProbability = duel.dribbleContinuationProbability;

        ShotManager shotManager = matchManager.shotManager;
        float currentExpectedGoals = -1f;
        if (shotManager != null)
        {
            shotManager.TryCalculateBestExpectedGoalsPreview(
                dribbler,
                carrierHex,
                "snapshot",
                out currentExpectedGoals,
                out _,
                out _,
                out _);
        }

        explanation.defensiveExpectedGoalsAgainst = currentExpectedGoals;
        int bestProgress = 0;
        float bestExpectedGoals = currentExpectedGoals;
        HexCell bestReposition = carrierHex;
        foreach (HexCell repositionHex in GetAttackerRepositionHexes(
            matchManager.hexGrid,
            dribbler,
            carrierHex,
            defenderHex))
        {
            if (repositionHex == null)
            {
                continue;
            }

            int progress = CalculateForwardProgressDelta(
                snapshot,
                RoomHexCoordinates.FromHex(carrierHex),
                RoomHexCoordinates.FromHex(repositionHex));
            float expectedGoals = -1f;
            if (shotManager != null)
            {
                shotManager.TryCalculateBestExpectedGoalsPreview(
                    dribbler,
                    repositionHex,
                    "snapshot",
                    out expectedGoals,
                    out _,
                    out _,
                    out _);
            }

            bool better = expectedGoals > bestExpectedGoals + 0.0001f
                || (ApproximatelyEqual(expectedGoals, bestExpectedGoals) && progress > bestProgress);
            if (better)
            {
                bestExpectedGoals = expectedGoals;
                bestProgress = progress;
                bestReposition = repositionHex;
            }
        }

        explanation.dribblerBestRepositionExpectedGoals = bestExpectedGoals;
        explanation.dribblerBestRepositionProgress = bestProgress;
        explanation.dribblerBestRepositionHex = RoomHexCoordinates.FromHex(bestReposition);
    }

    private static void PopulateSuccessfulTackleRepositionPlan(
        RoomAIDecisionSnapshot snapshot,
        RoomActionCandidate action,
        RoomAICandidateExplanation explanation,
        HexCell requestedHex)
    {
        MatchManager matchManager = MatchManager.Instance;
        MovementPhaseManager movement = matchManager?.movementPhaseManager;
        if (snapshot == null
            || action == null
            || explanation == null
            || movement == null
            || !movement.IsSuccessfulTackleRepositionPendingForDecision
            || !string.Equals(action.manager, nameof(MovementPhaseManager), System.StringComparison.Ordinal))
        {
            return;
        }

        bool movesToHex = action.step == RoomDecisionStep.ChooseTarget
            && action.actionType == RoomActionType.SetupMove;
        bool staysPut = action.step == RoomDecisionStep.InterruptionChoice
            && action.actionType == RoomActionType.Decline
            && action.isForfeit;
        if (!movesToHex && !staysPut)
        {
            return;
        }

        PlayerToken winner = movement.repositionWinner;
        HexCell proposedHex = requestedHex ?? winner?.GetCurrentHex();
        if (winner == null || proposedHex == null || matchManager.hexGrid == null)
        {
            return;
        }

        explanation.isSuccessfulTackleReposition = true;
        explanation.plannedNextAction = "Movement";
        explanation.plannedNextActionValue = 20f;
        explanation.plannedNextActionTargetHex = RoomHexCoordinates.FromHex(proposedHex);

        ShotManager shotManager = matchManager.shotManager;
        if (shotManager != null
            && shotManager.TryCalculateBestExpectedGoalsPreview(
                winner,
                proposedHex,
                "shot",
                out float shotExpectedGoals,
                out _,
                out _,
                out _))
        {
            float shotValue = shotExpectedGoals * 300f;
            if (shotValue > explanation.plannedNextActionValue)
            {
                explanation.plannedNextAction = "Shot";
                explanation.plannedNextActionValue = shotValue;
                explanation.plannedNextActionExpectedGoals = shotExpectedGoals;
                explanation.plannedNextActionTargetHex = RoomHexCoordinates.FromHex(proposedHex);
            }
        }

        IEnumerable<PlayerToken> tokens = matchManager.playerTokenManager != null
            ? matchManager.playerTokenManager.allTokens
            : UnityEngine.Object.FindObjectsByType<PlayerToken>();
        foreach (PlayerToken receiver in tokens)
        {
            HexCell receiverHex = receiver?.GetCurrentHex();
            if (receiver == null
                || receiver == winner
                || !receiver.isPlaying
                || receiver.isHomeTeam != winner.isHomeTeam
                || receiverHex == null
                || HexGridUtils.GetHexStepDistance(proposedHex, receiverHex) > 11
                || IsReceiverOffsideFromProposedBallHex(matchManager, winner, receiver, proposedHex, tokens)
                || !matchManager.CanTokenCollectHangingPass(receiver))
            {
                continue;
            }

            List<HexCell> path = GroundPassCommon.CalculateThickPath(
                matchManager.hexGrid,
                proposedHex,
                receiverHex,
                matchManager.ball != null ? matchManager.ball.ballRadius : 0f);
            bool blocked = false;
            int interceptors = 0;
            foreach (PlayerToken opponent in tokens)
            {
                HexCell opponentHex = opponent?.GetCurrentHex();
                if (opponent == null
                    || !opponent.isPlaying
                    || opponent.isHomeTeam == winner.isHomeTeam
                    || opponentHex == null)
                {
                    continue;
                }

                if (path.Contains(opponentHex))
                {
                    blocked = true;
                    break;
                }

                if (path.Exists(pathHex => pathHex != null
                    && HexGridUtils.GetHexStepDistance(opponentHex, pathHex) <= 1))
                {
                    interceptors++;
                }
            }

            if (blocked)
            {
                continue;
            }

            int progress = CalculateForwardProgressDelta(
                snapshot,
                RoomHexCoordinates.FromHex(proposedHex),
                RoomHexCoordinates.FromHex(receiverHex));
            float passValue = 55f
                + (progress * 10f)
                + (receiver.dribbling * 4f)
                + (receiver.shooting * 3f)
                - (interceptors * 45f);
            if (IsOpponentShootingHex(snapshot, receiverHex))
            {
                passValue += 90f;
            }

            if (passValue <= explanation.plannedNextActionValue)
            {
                continue;
            }

            explanation.plannedNextAction = "StandardPass";
            explanation.plannedNextActionValue = passValue;
            explanation.plannedNextReceiverTokenKey = GetTokenKey(receiver);
            explanation.plannedNextActionTargetHex = RoomHexCoordinates.FromHex(receiverHex);
            explanation.plannedNextActionInterceptors = interceptors;
        }
    }

    private static bool IsReceiverOffsideFromProposedBallHex(
        MatchManager matchManager,
        PlayerToken passer,
        PlayerToken receiver,
        HexCell proposedBallHex,
        IEnumerable<PlayerToken> allTokens)
    {
        HexCell receiverHex = receiver?.GetCurrentHex();
        if (matchManager == null
            || passer == null
            || receiverHex == null
            || proposedBallHex == null
            || allTokens == null)
        {
            return false;
        }

        MatchManager.TeamAttackingDirection direction = passer.isHomeTeam
            ? matchManager.homeTeamDirection
            : matchManager.awayTeamDirection;
        List<int> defenderColumns = allTokens
            .Where(token => token != null
                && token.isPlaying
                && token.isHomeTeam != passer.isHomeTeam
                && token.GetCurrentHex() != null)
            .Select(token => token.GetCurrentHex().coordinates.x)
            .ToList();
        if (defenderColumns.Count < 2)
        {
            return false;
        }

        defenderColumns.Sort();
        int secondDefenderX = direction == MatchManager.TeamAttackingDirection.LeftToRight
            ? defenderColumns[defenderColumns.Count - 2]
            : defenderColumns[1];
        int ballX = proposedBallHex.coordinates.x;
        int offsideLineX = direction == MatchManager.TeamAttackingDirection.LeftToRight
            ? UnityEngine.Mathf.Max(0, UnityEngine.Mathf.Max(ballX, secondDefenderX))
            : UnityEngine.Mathf.Min(0, UnityEngine.Mathf.Min(ballX, secondDefenderX));
        return direction == MatchManager.TeamAttackingDirection.LeftToRight
            ? receiverHex.coordinates.x > offsideLineX
            : receiverHex.coordinates.x < offsideLineX;
    }

    private static void PopulateSnapshotDefensiveMoveFacts(
        RoomAIDecisionSnapshot snapshot,
        RoomActionCandidate action,
        RoomAICandidateExplanation explanation)
    {
        if (action == null
            || explanation == null
            || action.actionType != RoomActionType.Block
            || !string.Equals(action.manager, nameof(ShotManager), System.StringComparison.Ordinal))
        {
            return;
        }

        ShotManager shotManager = MatchManager.Instance?.shotManager;
        PlayerToken defender = action.actor ?? shotManager?.tokenMoveforDeflection;
        if (shotManager == null || defender == null)
        {
            return;
        }

        explanation.isSnapshotDefensiveMove = true;
        explanation.snapshotDefenseMovesGoalkeeper = defender.IsGoalKeeper;
        IEnumerable<HexCell> destinations = action.targetHex != null
            ? new[] { action.targetHex }
            : shotManager.GetValidSnapshotBlockerDestinationsForDecisionPreview(defender);
        foreach (HexCell destination in destinations)
        {
            if (!shotManager.TryCalculateBestExpectedGoalsAfterSnapshotDefenderMove(
                    defender,
                    destination,
                    out float maximumExpectedGoals,
                    out HexCell worstTarget,
                    out int shootingPenalty,
                    out int blockers,
                    out int goalkeeperPenalty))
            {
                continue;
            }

            HexCell shooterHex = MatchManager.Instance?.LastTokenToTouchTheBallOnPurpose?.GetCurrentHex();
            int nextStateValue = shooterHex == null
                ? 0
                : 12 - (HexGridUtils.GetHexStepDistance(destination, shooterHex) * 3);

            if (explanation.snapshotDefenseMaximumExpectedGoals >= 0f
                && !IsBetterSnapshotDefenseOutcome(
                    maximumExpectedGoals,
                    blockers,
                    goalkeeperPenalty,
                    nextStateValue,
                    explanation.snapshotDefenseMaximumExpectedGoals,
                    explanation.snapshotDefenseBlockerCount,
                    explanation.snapshotDefenseGoalkeeperPenalty,
                    explanation.snapshotDefenseNextStateValue))
            {
                continue;
            }

            explanation.snapshotDefenseMaximumExpectedGoals = maximumExpectedGoals;
            explanation.snapshotDefenseWorstTargetHex = RoomHexCoordinates.FromHex(worstTarget);
            explanation.snapshotDefenseBestMoveHex = RoomHexCoordinates.FromHex(destination);
            explanation.snapshotDefenseBlockerCount = blockers;
            explanation.snapshotDefenseGoalkeeperPenalty = goalkeeperPenalty;
            explanation.snapshotDefenseNextStateValue = nextStateValue;
            PlayerToken anticipatedShooter = MatchManager.Instance?.LastTokenToTouchTheBallOnPurpose;
            PlayerToken goalkeeper = MatchManager.Instance?.hexGrid?.GetDefendingGK();
            explanation.goalkeeperPositioningWorstThreatTokenKey = GetTokenKey(anticipatedShooter);
            explanation.goalkeeperPositioningThreatOrigin = RoomHexCoordinates.FromHex(shooterHex);
            explanation.goalkeeperPositioningThreatAction = "Declared Snapshot";
            explanation.goalkeeperPositioningShootingPenalty = shootingPenalty;
            explanation.goalkeeperPositioningGoalkeeperPenalty = goalkeeperPenalty;
            explanation.goalkeeperPositioningWallApplies = goalkeeper != null && shotManager.GoalkeeperWallAppliesForPreview(
                goalkeeper, defender == goalkeeper ? destination : goalkeeper.GetCurrentHex(), shooterHex, worstTarget);
        }
    }

    private static bool IsBetterSnapshotDefenseOutcome(
        float candidateExpectedGoals,
        int candidateBlockers,
        int candidateGoalkeeperPenalty,
        int candidateNextStateValue,
        float incumbentExpectedGoals,
        int incumbentBlockers,
        int incumbentGoalkeeperPenalty,
        int incumbentNextStateValue)
    {
        const float xgTolerance = 0.000001f;
        if (System.Math.Abs(candidateExpectedGoals - incumbentExpectedGoals) > xgTolerance)
        {
            return candidateExpectedGoals < incumbentExpectedGoals;
        }
        if (candidateBlockers != incumbentBlockers)
        {
            return candidateBlockers > incumbentBlockers;
        }
        if (candidateGoalkeeperPenalty != incumbentGoalkeeperPenalty)
        {
            return candidateGoalkeeperPenalty < incumbentGoalkeeperPenalty;
        }
        return candidateNextStateValue > incumbentNextStateValue;
    }

    private static void PopulateGoalkeeperPositioningFacts(
        RoomAIDecisionSnapshot snapshot,
        RoomActionCandidate action,
        RoomAICandidateExplanation explanation)
    {
        PlayerToken goalkeeper = action?.actor;
        if (explanation == null
            || goalkeeper == null
            || !goalkeeper.IsGoalKeeper
            || (action.actionType != RoomActionType.GoalkeeperSave
                && action.actionType != RoomActionType.FinalThird
                && action.actionType != RoomActionType.StartMovement
                && action.actionType != RoomActionType.Decline
                && action.actionType != RoomActionType.FreeKick
                && !(action.actionType == RoomActionType.SetupMove
                    && (string.Equals(action.manager, nameof(LongBallManager), System.StringComparison.Ordinal)
                        || string.Equals(action.manager, nameof(FreeKickManager), System.StringComparison.Ordinal)))))
        {
            return;
        }

        explanation.isGoalkeeperPositioning = true;
        MatchManager matchManager = MatchManager.Instance;
        GoalKeeperManager goalkeeperManager = matchManager?.movementPhaseManager?.goalKeeperManager
            ?? UnityEngine.Object.FindAnyObjectByType<GoalKeeperManager>();
        HexCell proposedHex = action.targetHex ?? goalkeeper.GetCurrentHex();
        ShotManager shotManager = matchManager?.shotManager;
        HexCell currentGoalkeeperHex = goalkeeper.GetCurrentHex();
        if (matchManager == null
            || goalkeeperManager == null
            || proposedHex == null)
        {
            return;
        }

        explanation.goalkeeperOwnGoalDistanceDelta = CalculateDistanceToOwnGoalCenter(snapshot, proposedHex)
            - CalculateDistanceToOwnGoalCenter(snapshot, currentGoalkeeperHex);

        MatchManager.TeamAttackingDirection goalkeeperDirection = goalkeeper.isHomeTeam
            ? matchManager.homeTeamDirection
            : matchManager.awayTeamDirection;
        int goalkeeperOwnGoalSide = goalkeeperDirection == MatchManager.TeamAttackingDirection.LeftToRight
            ? -1
            : 1;

        PlayerToken ballCarrier = matchManager?.LastTokenToTouchTheBallOnPurpose;
        HexCell liveOrigin = matchManager.ball?.GetCurrentHex();
        List<PlayerToken> opponents = (matchManager?.playerTokenManager?.allTokens
                ?? UnityEngine.Object.FindObjectsByType<PlayerToken>().ToList())
            .Where(token => token != null
                && token.isPlaying
                && token.isHomeTeam != goalkeeper.isHomeTeam
                && token.isAttacker
                && HasShootingPathToGoalSide(token.GetCurrentHex(), goalkeeperOwnGoalSide)
                && (token == ballCarrier || liveOrigin != null
                    && HexGridUtils.GetHexStepDistance(liveOrigin, token.GetCurrentHex()) <= 11))
            .ToList();
        HexCell worstThreatHex = null;
        if (shotManager?.isActivated == true && shotManager.targetHex != null)
            opponents = opponents.Where(token => token == shotManager.shooter).ToList();
        foreach (PlayerToken threat in opponents)
        {
            HexCell threatHex = threat.GetCurrentHex();
            explanation.goalkeeperPositioningThreatCount++;
            if (threat != ballCarrier)
            {
                explanation.goalkeeperPositioningOffBallThreatCount++;
            }

            if (shotManager != null
                && shotManager.TryCalculateBestExpectedGoalsAfterGoalkeeperPositioning(
                    threat,
                    threatHex,
                    goalkeeper,
                    proposedHex,
                    out float proposedMaximumExpectedGoals,
                    out HexCell proposedWorstTarget,
                    out int proposedShootingPenalty,
                    out int proposedBlockerCount,
                    out int proposedGoalkeeperPenalty)
                && proposedMaximumExpectedGoals > explanation.goalkeeperPositioningMaximumExpectedGoals)
            {
                explanation.goalkeeperPositioningMaximumExpectedGoals = proposedMaximumExpectedGoals;
                explanation.goalkeeperPositioningWorstTargetHex = RoomHexCoordinates.FromHex(proposedWorstTarget);
                explanation.goalkeeperPositioningWorstThreatTokenKey = GetTokenKey(threat);
                explanation.goalkeeperPositioningBlockerCount = proposedBlockerCount;
                explanation.goalkeeperPositioningGoalkeeperPenalty = proposedGoalkeeperPenalty;
                explanation.goalkeeperPositioningShootingPenalty = proposedShootingPenalty;
                explanation.goalkeeperPositioningThreatOrigin = RoomHexCoordinates.FromHex(threatHex);
                explanation.goalkeeperPositioningThreatAction = threat == ballCarrier ? "Current carrier Snapshot" : "One-pass receiver Snapshot";
                explanation.goalkeeperPositioningWallApplies = shotManager.GoalkeeperWallAppliesForPreview(goalkeeper, proposedHex, threatHex, proposedWorstTarget);
                worstThreatHex = threatHex;
            }

            if (shotManager != null
                && currentGoalkeeperHex != null
                && shotManager.TryCalculateBestExpectedGoalsAfterGoalkeeperPositioning(
                    threat,
                    threatHex,
                    goalkeeper,
                    currentGoalkeeperHex,
                    out float currentMaximumExpectedGoals,
                    out _,
                    out _,
                    out int currentBlockerCount,
                    out _))
            {
                if (threat == ballCarrier)
                {
                    // The immediate shot-cover question is about the player on the
                    // ball.  Keep this separate from the worst xG across secondary
                    // threats so a lurking back-post player does not erase the fact
                    // that two outfield defenders already cover the current shot.
                    explanation.goalkeeperPositioningCurrentOutfieldBlockerCount = currentBlockerCount;
                }

                if (currentMaximumExpectedGoals > explanation.goalkeeperPositioningCurrentMaximumExpectedGoals)
                {
                    explanation.goalkeeperPositioningCurrentMaximumExpectedGoals = currentMaximumExpectedGoals;
                    if (ballCarrier == null)
                    {
                        explanation.goalkeeperPositioningCurrentOutfieldBlockerCount = currentBlockerCount;
                    }
                }
            }
        }

        // During a pass, the landing point is the next shot origin even while
        // the intended collector is still elsewhere on the board.
        HexCell landing = snapshot.instruction?.currentTargetHex != null
            ? matchManager.hexGrid.GetHexCellAt(new UnityEngine.Vector3Int(snapshot.instruction.currentTargetHex.x, 0, snapshot.instruction.currentTargetHex.z), false)
            : matchManager.ball?.GetCurrentHex();
        if (landing != null && shotManager != null && !(shotManager.isActivated && shotManager.targetHex != null))
        {
            var collectors = matchManager.playerTokenManager.allTokens.Where(token => token != null && token.isPlaying
                    && token.isHomeTeam != goalkeeper.isHomeTeam && token.GetCurrentHex() != null)
                .Select(token => new { token, distance = HexGridUtils.GetHexStepDistance(token.GetCurrentHex(), landing) })
                .Where(item => item.distance <= item.token.pace).OrderBy(item => item.distance)
                .ThenByDescending(item => item.token.pace).Take(1);
            foreach (var collector in collectors)
            {
                EvaluateAnticipatedGoalkeeperThreat(snapshot, explanation, goalkeeper, proposedHex,
                    currentGoalkeeperHex, collector.token, landing, "Snapshot after collection", ref worstThreatHex);
                foreach (PlayerToken receiver in opponents.Where(token => token != collector.token
                    && HexGridUtils.GetHexStepDistance(landing, token.GetCurrentHex()) <= 11))
                {
                    var passPath = GroundPassCommon.CalculateThickPath(matchManager.hexGrid, landing,
                        receiver.GetCurrentHex(), matchManager.ball.ballRadius);
                    if (passPath.Any(hex => hex.GetOccupyingToken() != null
                        && hex.GetOccupyingToken().isHomeTeam == goalkeeper.isHomeTeam)) continue;
                    EvaluateAnticipatedGoalkeeperThreat(snapshot, explanation, goalkeeper, proposedHex,
                        currentGoalkeeperHex, receiver, receiver.GetCurrentHex(), "One-pass continuation after collection", ref worstThreatHex);
                }
                int remaining = System.Math.Max(0, collector.token.pace - collector.distance);
                foreach (HexCell goal in matchManager.hexGrid.cells)
                {
                    if (goal == null || goal.isInGoal != goalkeeperOwnGoalSide) continue;
                    var route = HexGridUtils.FindPath(landing, goal, matchManager.hexGrid,
                        new HashSet<HexCell> { collector.token.GetCurrentHex(), currentGoalkeeperHex });
                    if (route.Count == 0 || route.Count > remaining) continue;
                    CalculateMovementRouteProbabilities(matchManager.movementPhaseManager, route,
                        out _, out _, out float survival);
                    bool gkCanReach = route.Any(hex => HexGridUtils.GetHexStepDistance(proposedHex, hex) <= 1);
                    float duelSurvival = 1f - ExpectedStatsCalculator.CalculateGroundDuelExpectationWithDefenderSkill(
                        collector.token, goalkeeper, goalkeeper.saving).xTackles;
                    float value = route.Contains(proposedHex) ? 0f : gkCanReach ? survival * duelSurvival : survival;
                    float currentValue = route.Contains(currentGoalkeeperHex) ? 0f
                        : route.Any(hex => HexGridUtils.GetHexStepDistance(currentGoalkeeperHex, hex) <= 1) ? survival * duelSurvival : survival;
                    explanation.goalkeeperPositioningCurrentMaximumExpectedGoals = System.Math.Max(
                        explanation.goalkeeperPositioningCurrentMaximumExpectedGoals, currentValue);
                    if (value > explanation.goalkeeperPositioningMaximumExpectedGoals)
                    {
                        explanation.goalkeeperPositioningMaximumExpectedGoals = value;
                        explanation.goalkeeperPositioningWorstThreatTokenKey = GetTokenKey(collector.token);
                        explanation.goalkeeperPositioningWorstTargetHex = RoomHexCoordinates.FromHex(goal);
                        explanation.goalkeeperPositioningThreatOrigin = RoomHexCoordinates.FromHex(landing);
                        explanation.goalkeeperPositioningThreatAction = "Walk into goal after collection";
                        explanation.goalkeeperPositioningWallApplies = false;
                        explanation.goalkeeperPositioningGoalkeeperPenalty = 0;
                        worstThreatHex = landing;
                    }
                }
            }
        }

        HexCell representativeThreatHex = worstThreatHex
            ?? ballCarrier?.GetCurrentHex()
            ?? opponents.FirstOrDefault()?.GetCurrentHex();
        if (representativeThreatHex?.ShootingPaths == null)
        {
            if (action.step == RoomDecisionStep.ChooseActor
                && action.actionType == RoomActionType.StartMovement)
            {
                PopulateGoalkeeperActorDestinationPreview(
                    snapshot,
                    action,
                    explanation,
                    goalkeeper,
                    currentGoalkeeperHex,
                    goalkeeperManager);
            }
            return;
        }

        foreach (KeyValuePair<HexCell, List<HexCell>> shotPath in representativeThreatHex.ShootingPaths)
        {
            if (shotPath.Key == null
                || shotPath.Value == null
                || shotPath.Key.isInGoal != goalkeeperOwnGoalSide)
            {
                continue;
            }

            explanation.goalkeeperWallShotTargetCount++;
            int bestDistance = int.MaxValue;
            foreach (HexCell pathHex in shotPath.Value)
            {
                if (pathHex == null
                    || pathHex.coordinates.x != proposedHex.coordinates.x
                    || !goalkeeperManager.IsGoalkeeperOwnPenaltyHex(goalkeeper, pathHex))
                {
                    continue;
                }

                int distance = HexGridUtils.GetHexStepDistance(proposedHex, pathHex);
                if (distance <= 3)
                {
                    bestDistance = System.Math.Min(bestDistance, distance);
                }
            }

            if (bestDistance == int.MaxValue)
            {
                continue;
            }

            explanation.goalkeeperWallCoveredShotTargets++;
            explanation.goalkeeperWallCoverageQuality += bestDistance == 3 ? 2 : 3;
        }

        if (action.step == RoomDecisionStep.ChooseActor
            && action.actionType == RoomActionType.StartMovement)
        {
            PopulateGoalkeeperActorDestinationPreview(
                snapshot,
                action,
                explanation,
                goalkeeper,
                currentGoalkeeperHex,
                goalkeeperManager);
        }
    }

    private static void EvaluateAnticipatedGoalkeeperThreat(RoomAIDecisionSnapshot snapshot,
        RoomAICandidateExplanation facts, PlayerToken goalkeeper, HexCell proposed, HexCell current,
        PlayerToken shooter, HexCell origin, string action, ref HexCell worstOrigin)
    {
        ShotManager shots = MatchManager.Instance?.shotManager;
        if (shots == null || origin == null) return;
        if (shots.TryCalculateBestExpectedGoalsAfterGoalkeeperPositioning(shooter, origin, goalkeeper, proposed,
            out float xg, out HexCell target, out int penalty, out int blockers, out int saveModifier)
            && xg > facts.goalkeeperPositioningMaximumExpectedGoals)
        {
            facts.goalkeeperPositioningMaximumExpectedGoals = xg;
            facts.goalkeeperPositioningWorstThreatTokenKey = GetTokenKey(shooter);
            facts.goalkeeperPositioningWorstTargetHex = RoomHexCoordinates.FromHex(target);
            facts.goalkeeperPositioningThreatOrigin = RoomHexCoordinates.FromHex(origin);
            facts.goalkeeperPositioningThreatAction = action;
            facts.goalkeeperPositioningBlockerCount = blockers;
            facts.goalkeeperPositioningShootingPenalty = penalty;
            facts.goalkeeperPositioningGoalkeeperPenalty = saveModifier;
            facts.goalkeeperPositioningWallApplies = shots.GoalkeeperWallAppliesForPreview(goalkeeper, proposed, origin, target);
            worstOrigin = origin;
        }
        if (current != null && shots.TryCalculateBestExpectedGoalsAfterGoalkeeperPositioning(shooter, origin, goalkeeper, current,
            out float currentXg, out _, out _, out _, out _))
            facts.goalkeeperPositioningCurrentMaximumExpectedGoals = System.Math.Max(facts.goalkeeperPositioningCurrentMaximumExpectedGoals, currentXg);
    }

    private static void PopulateLongBallGoalkeeperRecoveryFacts(
        RoomAIDecisionSnapshot snapshot,
        RoomActionCandidate action,
        RoomAICandidateExplanation explanation)
    {
        if (snapshot == null
            || action == null
            || explanation == null
            || !string.Equals(action.manager, nameof(LongBallManager), System.StringComparison.Ordinal)
            || (action.actionType != RoomActionType.SetupMove
                && action.actionType != RoomActionType.Decline
                && action.actionType != RoomActionType.LongBall))
        {
            return;
        }

        MatchManager matchManager = MatchManager.Instance;
        LongBallManager longBallManager = matchManager?.longBallManager;
        HexGrid grid = matchManager?.hexGrid;
        HexCell landingHex = longBallManager?.GetLongBallLandingHexForDecision();
        PlayerToken goalkeeper = grid?.GetDefendingGK();
        HexCell goalkeeperHex = goalkeeper?.GetCurrentHex();
        if (landingHex == null || goalkeeper == null || goalkeeperHex == null)
        {
            return;
        }

        if (action.actor == null)
        {
            PopulateGoalkeeperPositioningFacts(
                snapshot,
                new RoomActionCandidate
                {
                    manager = action.manager,
                    actionType = action.actionType,
                    step = action.step,
                    actor = goalkeeper,
                    targetHex = action.targetHex,
                    isExecutableNow = action.isExecutableNow
                },
                explanation);
        }

        List<PlayerToken> tokens = (matchManager.playerTokenManager?.allTokens
                ?? UnityEngine.Object.FindObjectsByType<PlayerToken>().ToList())
            .Where(token => token != null && token.isPlaying && token.GetCurrentHex() != null)
            .ToList();
        PlayerToken receiver = tokens
            .Where(token => token.isHomeTeam != goalkeeper.isHomeTeam
                && !token.IsGoalKeeper
                && token.pace >= 4
                && token.shooting >= 4)
            .Select(token => new
            {
                token,
                distance = HexGridUtils.GetHexStepDistance(token.GetCurrentHex(), landingHex)
            })
            .Where(item => item.distance <= item.token.pace * 2)
            .OrderBy(item => MovementPhasesToReach(item.distance, item.token.pace))
            .ThenByDescending(item => item.token.shooting)
            .ThenByDescending(item => item.token.pace)
            .Select(item => item.token)
            .FirstOrDefault();
        if (receiver == null)
        {
            return;
        }

        int receiverDistance = HexGridUtils.GetHexStepDistance(receiver.GetCurrentHex(), landingHex);
        int attackerPhases = MovementPhasesToReach(receiverDistance, receiver.pace);
        PlayerToken recoveringDefender = tokens
            .Where(token => token.isHomeTeam == goalkeeper.isHomeTeam && !token.IsGoalKeeper)
            .OrderBy(token => MovementPhasesToReach(
                HexGridUtils.GetHexStepDistance(token.GetCurrentHex(), landingHex), token.pace))
            .ThenBy(token => HexGridUtils.GetHexStepDistance(token.GetCurrentHex(), landingHex))
            .ThenByDescending(token => token.tackling)
            .FirstOrDefault();
        int defenderDistance = recoveringDefender != null
            ? HexGridUtils.GetHexStepDistance(recoveringDefender.GetCurrentHex(), landingHex)
            : int.MaxValue;
        int defenderPhases = recoveringDefender != null
            ? MovementPhasesToReach(defenderDistance, recoveringDefender.pace)
            : int.MaxValue;
        int goalkeeperDistance = HexGridUtils.GetHexStepDistance(goalkeeperHex, landingHex);
        int goalkeeperFreePace = System.Math.Max(0, goalkeeper.pace);
        int goalkeeperDistanceAfterFreeMove = System.Math.Max(0, goalkeeperDistance - goalkeeperFreePace);
        int goalkeeperPhasesAfterFreeMove = MovementPhasesToReach(
            goalkeeperDistanceAfterFreeMove,
            goalkeeper.pace);

        bool defenderClaimsFirst = defenderPhases < attackerPhases;
        bool goalkeeperClaimsFirst = goalkeeperPhasesAfterFreeMove < attackerPhases;
        bool touchRecoveryChance = recoveringDefender != null
            && !defenderClaimsFirst
            && defenderDistance <= recoveringDefender.pace + 1
            && (recoveringDefender.tackling >= 4 || recoveringDefender.resilience >= 4);

        MatchManager.TeamAttackingDirection goalkeeperDirection = goalkeeper.isHomeTeam
            ? matchManager.homeTeamDirection
            : matchManager.awayTeamDirection;
        int ownGoalSide = goalkeeperDirection == MatchManager.TeamAttackingDirection.LeftToRight ? -1 : 1;
        bool farPostPassThreat = tokens.Any(token => token != receiver
            && token.isHomeTeam != goalkeeper.isHomeTeam
            && !token.IsGoalKeeper
            && token.shooting >= 4
            && HexGridUtils.GetHexStepDistance(token.GetCurrentHex(), landingHex) <= 11
            && HasShootingPathToGoalSide(token.GetCurrentHex(), ownGoalSide));

        HexCell proposedHex = action.targetHex ?? goalkeeperHex;
        explanation.isLongBallGoalkeeperRecovery = true;
        explanation.longBallAttackingReceiverTokenKey = GetTokenKey(receiver);
        explanation.longBallRecoveringDefenderTokenKey = GetTokenKey(recoveringDefender);
        explanation.longBallAttackerMovementPhases = attackerPhases;
        explanation.longBallBestDefenderMovementPhases = defenderPhases == int.MaxValue ? -1 : defenderPhases;
        // The Long Ball interruption gives the GK one full Pace move immediately,
        // before either side receives a normal movement phase.  Store both the
        // allowance and the later phases separately so the tactical decision
        // cannot accidentally count the free move as an ordinary MP.
        explanation.longBallGoalkeeperFreePace = goalkeeperFreePace;
        explanation.longBallGoalkeeperCanReachBallWithFreeMove = goalkeeperDistanceAfterFreeMove == 0;
        explanation.longBallGoalkeeperRemainingMovementPhasesAfterFreeMove = goalkeeperPhasesAfterFreeMove;
        explanation.longBallGoalkeeperMovementPhases = goalkeeperPhasesAfterFreeMove;
        explanation.longBallDefenseClaimsFirst = defenderClaimsFirst || goalkeeperClaimsFirst;
        explanation.longBallDefenderTouchRecoveryChance = touchRecoveryChance;
        explanation.longBallFarPostPassThreat = farPostPassThreat;
        explanation.longBallGoalkeeperDistanceToLanding = HexGridUtils.GetHexStepDistance(proposedHex, landingHex);
        explanation.longBallGoalkeeperSupportDistance = recoveringDefender != null
            ? HexGridUtils.GetHexStepDistance(proposedHex, landingHex)
            : -1;
        explanation.longBallRecommendedGoalkeeperHex = RoomHexCoordinates.FromHex(proposedHex);

        if (goalkeeperClaimsFirst)
        {
            explanation.longBallRecoveryMode = "ClaimLooseBall";
        }
        else if (defenderClaimsFirst || touchRecoveryChance)
        {
            explanation.longBallRecoveryMode = "SupportRecovery";
        }
        else if (!farPostPassThreat && receiver.dribbling <= 3)
        {
            explanation.longBallRecoveryMode = "ForceDiveAtFeet";
        }
        else
        {
            explanation.longBallRecoveryMode = "CoverGoalAndFarPost";
        }
    }

    private static int MovementPhasesToReach(int distance, int pace)
    {
        if (distance <= 0)
        {
            return 0;
        }

        return pace > 0 ? (distance + pace - 1) / pace : int.MaxValue;
    }

    private static void PopulateGoalkeeperActorDestinationPreview(
        RoomAIDecisionSnapshot snapshot,
        RoomActionCandidate actorAction,
        RoomAICandidateExplanation actorExplanation,
        PlayerToken goalkeeper,
        HexCell currentHex,
        GoalKeeperManager goalkeeperManager)
    {
        HexGrid grid = MatchManager.Instance?.hexGrid;
        if (grid == null || currentHex == null || goalkeeperManager == null)
        {
            return;
        }

        List<HexCell> destinations = GetCachedReachability(
                snapshot,
                grid,
                currentHex,
                System.Math.Max(0, goalkeeper.pace)).reachableHexes
            .Where(hex => hex != null
                && !hex.isOutOfBounds
                && goalkeeperManager.IsGoalkeeperOwnPenaltyHex(goalkeeper, hex)
                && ((!hex.isAttackOccupied && !hex.isDefenseOccupied) || hex == currentHex))
            .Append(currentHex)
            .Distinct()
            .ToList();
        if (destinations.Count == 0)
        {
            return;
        }

        RoomAICandidateExplanation best = null;
        HexCell bestHex = currentHex;
        foreach (HexCell destination in destinations)
        {
            RoomActionCandidate destinationAction = new RoomActionCandidate
            {
                manager = actorAction.manager,
                actionType = actorAction.actionType,
                step = RoomDecisionStep.ChooseTarget,
                actor = goalkeeper,
                targetHex = destination,
                isExecutableNow = true
            };
            RoomAICandidateExplanation preview = new RoomAICandidateExplanation();
            PopulateGoalkeeperPositioningFacts(snapshot, destinationAction, preview);
            if (best == null
                || preview.goalkeeperPositioningMaximumExpectedGoals
                    < best.goalkeeperPositioningMaximumExpectedGoals - 0.0001f
                || (System.Math.Abs(preview.goalkeeperPositioningMaximumExpectedGoals
                        - best.goalkeeperPositioningMaximumExpectedGoals) <= 0.0001f
                    && preview.goalkeeperWallCoverageQuality > best.goalkeeperWallCoverageQuality)
                || (System.Math.Abs(preview.goalkeeperPositioningMaximumExpectedGoals
                        - best.goalkeeperPositioningMaximumExpectedGoals) <= 0.0001f
                    && preview.goalkeeperWallCoverageQuality == best.goalkeeperWallCoverageQuality
                    && preview.goalkeeperOwnGoalDistanceDelta < best.goalkeeperOwnGoalDistanceDelta))
            {
                best = preview;
                bestHex = destination;
            }
        }

        if (best == null)
        {
            return;
        }

        actorExplanation.goalkeeperActorDestinationPreviewed = true;
        actorExplanation.goalkeeperActorBestDestinationHex = RoomHexCoordinates.FromHex(bestHex);
        bool improvesExpectedGoals = best.goalkeeperPositioningMaximumExpectedGoals >= 0f
            && actorExplanation.goalkeeperPositioningCurrentMaximumExpectedGoals >= 0f
            && best.goalkeeperPositioningMaximumExpectedGoals
                < actorExplanation.goalkeeperPositioningCurrentMaximumExpectedGoals - 0.005f;
        bool improvesCoverage = best.goalkeeperWallCoverageQuality
            > actorExplanation.goalkeeperWallCoverageQuality;
        actorExplanation.goalkeeperActorBestContinuationIsForfeit = !improvesExpectedGoals
            && !improvesCoverage;

        actorExplanation.goalkeeperPositioningMaximumExpectedGoals
            = best.goalkeeperPositioningMaximumExpectedGoals;
        actorExplanation.goalkeeperPositioningWorstTargetHex
            = best.goalkeeperPositioningWorstTargetHex;
        actorExplanation.goalkeeperPositioningWorstThreatTokenKey
            = best.goalkeeperPositioningWorstThreatTokenKey;
        actorExplanation.goalkeeperPositioningThreatOrigin = best.goalkeeperPositioningThreatOrigin;
        actorExplanation.goalkeeperPositioningThreatAction = best.goalkeeperPositioningThreatAction;
        actorExplanation.goalkeeperPositioningWallApplies = best.goalkeeperPositioningWallApplies;
        actorExplanation.goalkeeperPositioningShootingPenalty = best.goalkeeperPositioningShootingPenalty;
        actorExplanation.goalkeeperPositioningBlockerCount
            = best.goalkeeperPositioningBlockerCount;
        actorExplanation.goalkeeperPositioningGoalkeeperPenalty
            = best.goalkeeperPositioningGoalkeeperPenalty;
        actorExplanation.goalkeeperWallCoveredShotTargets
            = best.goalkeeperWallCoveredShotTargets;
        actorExplanation.goalkeeperWallShotTargetCount
            = best.goalkeeperWallShotTargetCount;
        actorExplanation.goalkeeperWallCoverageQuality
            = best.goalkeeperWallCoverageQuality;
        actorExplanation.goalkeeperOwnGoalDistanceDelta
            = best.goalkeeperOwnGoalDistanceDelta;
    }

    private static bool HasShootingPathToGoalSide(HexCell threatHex, int goalSide)
    {
        return threatHex?.ShootingPaths != null
            && threatHex.ShootingPaths.Any(path =>
                path.Key != null
                && path.Key.isInGoal == goalSide
                && path.Value != null
                && path.Value.Count > 0);
    }

    private static void PopulateHeaderPossessionChainFacts(
        RoomAIDecisionSnapshot snapshot,
        RoomActionCandidate action,
        RoomAICandidateExplanation explanation,
        HexCell targetHex,
        HeaderManager headerManager,
        IReadOnlyList<HexCell> opponentShootingHexes)
    {
        if (snapshot == null
            || action == null
            || explanation == null
            || !string.Equals(action.manager, nameof(HeaderManager), System.StringComparison.Ordinal))
        {
            return;
        }

        if (headerManager == null)
        {
            return;
        }

        bool isControlParticipantChoice = action.step == RoomDecisionStep.ChooseActor
            && action.actor != null
            && (headerManager.attackControlBall || headerManager.defenseBallControl);
        if (isControlParticipantChoice)
        {
            int penalty = headerManager.GetControlPenaltyForPreview(action.actor);
            float successProbability = ExpectedStatsCalculator.CalculateAttributeCheckSuccessProbability(
                action.actor.dribbling,
                penalty);
            explanation.isHeaderControlChoice = true;
            explanation.bestHeaderControllerTokenKey = GetTokenKey(action.actor);
            explanation.bestHeaderControllerDribbling = action.actor.dribbling;
            explanation.headerControlPenalty = penalty;
            explanation.headerControlSuccessProbability = successProbability;
            explanation.headerControlContinuationValue = CalculateHeaderControlContinuationValue(
                snapshot,
                action.actor,
                opponentShootingHexes,
                headerManager.defenseBallControl);
            explanation.headerControlExpectedValue = (int)System.Math.Round(
                (successProbability * explanation.headerControlContinuationValue)
                - ((1f - successProbability) * 170f));
            explanation.possessionChain = new RoomAIPossessionChain("Selected player controls High Pass")
                .AddTerminal(
                    "Ball control succeeds",
                    successProbability,
                    headerManager.defenseBallControl
                        ? RoomAIPossessionOutcomeKind.DefenseWinsPossession
                        : RoomAIPossessionOutcomeKind.AttackRetainsPossession)
                .AddTerminal(
                    "Ball control fails",
                    1f - successProbability,
                    RoomAIPossessionOutcomeKind.Unresolved)
                .Evaluate();
            return;
        }

        bool isControlChoice = action.step == RoomDecisionStep.InterruptionChoice
            && action.actionType == RoomActionType.SelectToken
            && string.Equals(action.key, "B", System.StringComparison.OrdinalIgnoreCase);
        bool isFreeHeaderChoice = action.step == RoomDecisionStep.InterruptionChoice
            && action.actionType == RoomActionType.Header
            && string.Equals(action.key, "H", System.StringComparison.OrdinalIgnoreCase);
        if (isControlChoice || isFreeHeaderChoice)
        {
            explanation.isHeaderControlChoice = isControlChoice;
            explanation.isFreeHeaderChoice = isFreeHeaderChoice;
            if (isControlChoice)
            {
                PopulateHeaderControlChoice(
                    snapshot,
                    headerManager,
                    explanation,
                    opponentShootingHexes);
            }
            else
            {
                PopulateFreeHeaderChoicePreview(
                    snapshot,
                    headerManager,
                    explanation,
                    opponentShootingHexes);
            }

            return;
        }

        if (action.actionType != RoomActionType.Header
            || action.step != RoomDecisionStep.ChooseTarget
            || targetHex == null)
        {
            return;
        }

        explanation.isHeadedPassTarget = true;
        PlayerToken receiver = action.targetToken ?? targetHex.GetOccupyingToken();
        explanation.headedPassTargetsPlayer = receiver != null;
        explanation.headedPassTargetsSpace = receiver == null;

        if (receiver != null)
        {
            // ShouldBlockCollection controls beginner input assistance only;
            // a stored offence is called on collection at every difficulty.
            bool isOffside = MatchManager.Instance?.offsideManager?.IsTokenOffside(receiver) == true;
            explanation.headedPassTargetIsOffside = isOffside;
            explanation.headedPassInterceptors = 0;
            explanation.headedPassInterceptionProbability = 0f;
            if (!isOffside
                && targetHex.CanShootFrom
                && headerManager.shotManager != null
                && headerManager.shotManager.TryCalculateBestExpectedGoalsPreview(
                    receiver,
                    targetHex,
                    "snapshot",
                    out float snapshotExpectedGoals,
                    out int snapshotPenalty,
                    out int snapshotBlockers,
                    out int snapshotGoalkeeperPenalty))
            {
                explanation.headedPassImmediateSnapshotExpectedGoals = snapshotExpectedGoals;
                explanation.headedPassSnapshotPenalty = snapshotPenalty;
                explanation.headedPassSnapshotBlockers = snapshotBlockers;
                explanation.headedPassSnapshotGoalkeeperPenalty = snapshotGoalkeeperPenalty;
            }

            RoomAIPossessionChain directPassChain = new RoomAIPossessionChain("Headed pass to player");
            if (isOffside)
            {
                directPassChain.AddTerminal(
                    "Receiver is called offside",
                    1f,
                    RoomAIPossessionOutcomeKind.DefendingRestart);
            }
            else if (explanation.headedPassImmediateSnapshotExpectedGoals >= 0f)
            {
                directPassChain.AddContinuation(
                    "Receiver takes the immediate Snapshot",
                    1f,
                    new RoomAIPossessionChain("Immediate Snapshot after headed pass")
                        .AddTerminal(
                            "Snapshot scores",
                            explanation.headedPassImmediateSnapshotExpectedGoals,
                            RoomAIPossessionOutcomeKind.Goal));
            }
            else
            {
                directPassChain.AddTerminal(
                    "Receiver controls the headed pass",
                    1f,
                    RoomAIPossessionOutcomeKind.AttackRetainsPossession);
            }

            explanation.possessionChain = directPassChain.Evaluate();

            return;
        }

        IReadOnlyList<PlayerToken> interceptors = headerManager.GetHeaderPassInterceptorsForPreview(targetHex);
        float interceptionProbability = ExpectedStatsCalculator.CalculateAtLeastOneRecoveryProbability(interceptors);
        explanation.headedPassInterceptors = interceptors.Count;
        explanation.headedPassInterceptionProbability = interceptionProbability;
        HeaderSpaceSnapshotRoute route = FindBestHeaderSpaceSnapshotRoute(
            snapshot,
            headerManager,
            targetHex,
            opponentShootingHexes,
            out int routesEvaluated);
        explanation.headedPassSpaceRoutesEvaluated = routesEvaluated;
        RoomAIPossessionChain chain = new RoomAIPossessionChain("Headed pass into space")
            .AddTerminal(
                "A defender intercepts the headed pass",
                interceptionProbability,
                RoomAIPossessionOutcomeKind.DefenseWinsPossession);
        if (route != null)
        {
            explanation.headedPassSpaceCollectorTokenKey = GetTokenKey(route.collector);
            explanation.headedPassSpaceSnapshotHex = RoomHexCoordinates.FromHex(route.snapshotHex);
            explanation.headedPassSpacePickupDistance = route.pickupDistance;
            explanation.headedPassSpaceRemainingPace = route.remainingPace;
            explanation.headedPassSpaceMovementTurnoverProbability = route.movementTurnoverProbability;
            explanation.headedPassSpaceFoulProbability = route.foulProbability;
            explanation.headedPassSpaceReachSnapshotProbability = route.reachSnapshotProbability;
            explanation.headedPassSpaceSnapshotExpectedGoals = route.snapshotExpectedGoals;
            explanation.headedPassSpaceRouteExpectedGoals = (1f - interceptionProbability)
                * route.reachSnapshotProbability
                * route.snapshotExpectedGoals;

            RoomAIPossessionChain snapshotChain = new RoomAIPossessionChain("Snapshot after collecting headed pass")
                .AddTerminal(
                    "Snapshot scores",
                    route.snapshotExpectedGoals,
                    RoomAIPossessionOutcomeKind.Goal);
            RoomAIPossessionChain movementChain = new RoomAIPossessionChain("Collect and carry into Snapshot position")
                .AddTerminal(
                    "Defender steals during the dribble",
                    route.movementTurnoverProbability,
                    RoomAIPossessionOutcomeKind.DefenseWinsPossession)
                .AddTerminal(
                    "Defender fouls the collector",
                    route.foulProbability,
                    RoomAIPossessionOutcomeKind.AttackingRestart)
                .AddContinuation(
                    "Collector reaches Snapshot position",
                    route.reachSnapshotProbability,
                    snapshotChain);
            chain.AddContinuation(
                "Headed pass survives and collection route begins",
                1f - interceptionProbability,
                movementChain);
        }
        else if (TryFindHeaderSpacePossessionCollector(
            snapshot,
            headerManager,
            targetHex,
            out PlayerToken collector,
            out int pickupDistance,
            out float pickupReachProbability))
        {
            explanation.headedPassSpaceCollectorTokenKey = GetTokenKey(collector);
            explanation.headedPassSpacePickupDistance = pickupDistance;
            explanation.headedPassSpaceReachSnapshotProbability = pickupReachProbability;
            chain.AddContinuation(
                "Headed pass survives for a reachable teammate",
                1f - interceptionProbability,
                new RoomAIPossessionChain("Collect headed pass; defensive response remains unexpanded")
                    .AddTerminal(
                        "Teammate reaches the ball; next defensive movement is unexpanded",
                        pickupReachProbability,
                        RoomAIPossessionOutcomeKind.Unresolved)
                    .AddTerminal(
                        "Defender steals during collection movement",
                        1f - pickupReachProbability,
                        RoomAIPossessionOutcomeKind.DefenseWinsPossession));
        }

        explanation.possessionChain = chain.Evaluate();
    }

    private static bool TryFindHeaderSpacePossessionCollector(
        RoomAIDecisionSnapshot snapshot,
        HeaderManager headerManager,
        HexCell targetHex,
        out PlayerToken bestCollector,
        out int bestPickupDistance,
        out float bestReachProbability)
    {
        bestCollector = null;
        bestPickupDistance = -1;
        bestReachProbability = -1f;
        MatchManager matchManager = MatchManager.Instance;
        HexGrid grid = matchManager?.hexGrid;
        MovementPhaseManager movement = matchManager?.movementPhaseManager;
        if (headerManager == null || targetHex == null || grid == null || movement == null)
        {
            return false;
        }

        foreach (PlayerToken collector in grid.GetAttackers())
        {
            HexCell collectorHex = collector?.GetCurrentHex();
            if (collector == null
                || collectorHex == null
                || collector.pace <= 0
                || headerManager.attackerWillJump.Contains(collector)
                || headerManager.defenderWillJump.Contains(collector)
                || movement.stunnedTokens.Contains(collector)
                || movement.stunnedforNext.Contains(collector)
                || matchManager.offsideManager?.IsTokenOffside(collector) == true
                || !matchManager.CanTokenCollectHangingPass(collector))
            {
                continue;
            }

            Dictionary<HexCell, (int distance, bool enteredZOI)> distances = GetCachedReachability(
                snapshot,
                grid,
                collectorHex,
                collector.pace).distances;
            if (!distances.TryGetValue(targetHex, out (int distance, bool enteredZOI) pickup))
            {
                continue;
            }

            List<HexCell> route = targetHex == collectorHex
                ? new List<HexCell>()
                : HexGridUtils.FindPath(
                    collectorHex,
                    targetHex,
                    grid,
                    new HashSet<HexCell> { collectorHex });
            if (targetHex != collectorHex && (route == null || route.Count == 0 || route.Count > collector.pace))
            {
                continue;
            }

            // The approach is an off-ball move. Dribble interception begins
            // only after collection, not along the route to the loose ball.
            float reachProbability = 1f;
            bool better = reachProbability > bestReachProbability + 0.0001f
                || (System.Math.Abs(reachProbability - bestReachProbability) < 0.0001f
                    && (bestPickupDistance < 0 || pickup.distance < bestPickupDistance));
            if (better)
            {
                bestCollector = collector;
                bestPickupDistance = pickup.distance;
                bestReachProbability = reachProbability;
            }
        }

        return bestCollector != null;
    }

    private sealed class HeaderSpaceSnapshotRoute
    {
        public PlayerToken collector;
        public HexCell snapshotHex;
        public int pickupDistance;
        public int remainingPace;
        public float movementTurnoverProbability;
        public float foulProbability;
        public float reachSnapshotProbability;
        public float snapshotExpectedGoals;
        public float routeExpectedGoals;
    }

    private static HeaderSpaceSnapshotRoute FindBestHeaderSpaceSnapshotRoute(
        RoomAIDecisionSnapshot snapshot,
        HeaderManager headerManager,
        HexCell ballTargetHex,
        IReadOnlyList<HexCell> opponentShootingHexes,
        out int routesEvaluated)
    {
        routesEvaluated = 0;
        MatchManager matchManager = MatchManager.Instance;
        HexGrid hexGrid = matchManager?.hexGrid;
        MovementPhaseManager movement = matchManager?.movementPhaseManager;
        ShotManager shotManager = headerManager?.shotManager ?? matchManager?.shotManager;
        if (snapshot == null
            || hexGrid == null
            || movement == null
            || shotManager == null
            || ballTargetHex == null
            || opponentShootingHexes == null)
        {
            return null;
        }

        HeaderSpaceSnapshotRoute bestRoute = null;
        foreach (PlayerToken collector in hexGrid.GetAttackers())
        {
            HexCell collectorHex = collector?.GetCurrentHex();
            if (collector == null
                || collectorHex == null
                || collector.pace <= 0
                || headerManager.attackerWillJump.Contains(collector)
                || headerManager.defenderWillJump.Contains(collector)
                || movement.stunnedTokens.Contains(collector)
                || movement.stunnedforNext.Contains(collector)
                || matchManager.offsideManager?.IsTokenOffside(collector) == true
                || !matchManager.CanTokenCollectHangingPass(collector))
            {
                continue;
            }

            Dictionary<HexCell, (int distance, bool enteredZOI)> pickupDistances = GetCachedReachability(
                snapshot,
                hexGrid,
                collectorHex,
                collector.pace).distances;
            if (!pickupDistances.TryGetValue(ballTargetHex, out (int distance, bool enteredZOI) pickupData))
            {
                continue;
            }

            int remainingPace = System.Math.Max(0, collector.pace - pickupData.distance);
            HashSet<HexCell> vacatedHexes = new HashSet<HexCell> { collectorHex };
            foreach (HexCell snapshotHex in opponentShootingHexes)
            {
                if (snapshotHex == null
                    || !IsOpponentPenaltyBox(snapshot, snapshotHex)
                    || HexGridUtils.GetHexStepDistance(ballTargetHex, snapshotHex) > remainingPace
                    || snapshotHex.ShootingPaths == null
                    || snapshotHex.ShootingPaths.Count == 0)
                {
                    continue;
                }

                List<HexCell> dribblePath = snapshotHex == ballTargetHex
                    ? new List<HexCell>()
                    : HexGridUtils.FindPath(ballTargetHex, snapshotHex, hexGrid, vacatedHexes);
                if (snapshotHex != ballTargetHex
                    && (dribblePath.Count == 0 || dribblePath.Count > remainingPace))
                {
                    continue;
                }

                routesEvaluated++;

                CalculateMovementRouteProbabilities(
                    movement,
                    dribblePath,
                    out float movementTurnoverProbability,
                    out float foulProbability,
                    out float reachSnapshotProbability);
                if (!shotManager.TryCalculateBestExpectedGoalsPreview(
                    collector,
                    snapshotHex,
                    "snapshot",
                    out float snapshotExpectedGoals,
                    out _,
                    out _,
                    out _))
                {
                    continue;
                }

                float routeExpectedGoals = reachSnapshotProbability * snapshotExpectedGoals;
                if (bestRoute != null
                    && (routeExpectedGoals < bestRoute.routeExpectedGoals
                        || (System.Math.Abs(routeExpectedGoals - bestRoute.routeExpectedGoals) < 0.0001f
                            && movementTurnoverProbability >= bestRoute.movementTurnoverProbability)))
                {
                    continue;
                }

                bestRoute = new HeaderSpaceSnapshotRoute
                {
                    collector = collector,
                    snapshotHex = snapshotHex,
                    pickupDistance = pickupData.distance,
                    remainingPace = remainingPace,
                    movementTurnoverProbability = movementTurnoverProbability,
                    foulProbability = foulProbability,
                    reachSnapshotProbability = reachSnapshotProbability,
                    snapshotExpectedGoals = snapshotExpectedGoals,
                    routeExpectedGoals = routeExpectedGoals
                };
            }
        }

        return bestRoute;
    }

    private static void CalculateMovementRouteProbabilities(
        MovementPhaseManager movement,
        IReadOnlyList<HexCell> dribblePath,
        out float turnoverProbability,
        out float foulProbability,
        out float continuationProbability)
    {
        turnoverProbability = 0f;
        foulProbability = 0f;
        continuationProbability = 1f;
        if (movement == null || dribblePath == null || dribblePath.Count == 0)
        {
            return;
        }

        HashSet<PlayerToken> defendersAlreadyTried = new HashSet<PlayerToken>();
        for (int pathIndex = 0; pathIndex < dribblePath.Count && continuationProbability > 0f; pathIndex++)
        {
            IReadOnlyList<PlayerToken> interceptors = movement.GetMovementInterceptorsForPreview(
                dribblePath[pathIndex],
                defendersAlreadyTried);
            for (int defenderIndex = 0; defenderIndex < interceptors.Count; defenderIndex++)
            {
                PlayerToken defender = interceptors[defenderIndex];
                if (defender == null || !defendersAlreadyTried.Add(defender))
                {
                    continue;
                }

                ExpectedStatsCalculator.MovementInterceptionExpectation attempt =
                    ExpectedStatsCalculator.CalculateMovementInterceptionExpectation(defender);
                float attemptReachProbability = continuationProbability;
                turnoverProbability += attemptReachProbability * attempt.recoveryProbability;
                foulProbability += attemptReachProbability * attempt.foulProbability;
                continuationProbability = attemptReachProbability * attempt.continuationProbability;
            }
        }
    }

    private static void PopulateHeaderControlChoice(
        RoomAIDecisionSnapshot snapshot,
        HeaderManager headerManager,
        RoomAICandidateExplanation explanation,
        IReadOnlyList<HexCell> opponentShootingHexes)
    {
        bool defenseControls = headerManager.isWaitingForControlOrHeaderDecisionDef;
        IReadOnlyList<PlayerToken> candidates = ResolveHeaderControlCandidates(headerManager, defenseControls);
        PlayerToken bestController = null;
        int bestPenalty = 0;
        float bestSuccessProbability = -1f;

        for (int index = 0; index < candidates.Count; index++)
        {
            PlayerToken candidate = candidates[index];
            if (candidate == null)
            {
                continue;
            }

            int penalty = headerManager.GetControlPenaltyForPreview(candidate);
            float successProbability = ExpectedStatsCalculator.CalculateAttributeCheckSuccessProbability(
                candidate.dribbling,
                penalty);
            if (successProbability > bestSuccessProbability)
            {
                bestController = candidate;
                bestPenalty = penalty;
                bestSuccessProbability = successProbability;
            }
        }

        if (bestController == null)
        {
            return;
        }

        explanation.bestHeaderControllerTokenKey = GetTokenKey(bestController);
        explanation.bestHeaderControllerDribbling = bestController.dribbling;
        explanation.headerControlPenalty = bestPenalty;
        explanation.headerControlSuccessProbability = bestSuccessProbability;
        explanation.headerControlContinuationValue = CalculateHeaderControlContinuationValue(
            snapshot,
            bestController,
            opponentShootingHexes,
            defenseControls);
        // A failed control releases the ball. Value control as the deliberate
        // choice to retain it with this exact jumper, not as a generic H/B coin.
        explanation.headerControlExpectedValue = (int)System.Math.Round(
            (bestSuccessProbability * explanation.headerControlContinuationValue)
            - ((1f - bestSuccessProbability) * 170f));
        explanation.possessionChain = new RoomAIPossessionChain("Bring High Pass under control")
            .AddTerminal(
                "Ball control succeeds",
                bestSuccessProbability,
                defenseControls
                    ? RoomAIPossessionOutcomeKind.DefenseWinsPossession
                    : RoomAIPossessionOutcomeKind.AttackRetainsPossession)
            .Evaluate();
    }

    private static int CalculateHeaderControlContinuationValue(
        RoomAIDecisionSnapshot snapshot,
        PlayerToken controller,
        IReadOnlyList<HexCell> opponentShootingHexes,
        bool defenseControls)
    {
        if (controller == null || defenseControls)
        {
            return 100;
        }

        HexCell controllerHex = controller.GetCurrentHex();
        int value = 100;
        if (controllerHex == null)
        {
            return value;
        }

        if (controllerHex.CanShootFrom
            && MatchManager.Instance?.shotManager != null
            && MatchManager.Instance.shotManager.TryCalculateBestExpectedGoalsPreview(
                controller,
                controllerHex,
                "snapshot",
                out float expectedGoals,
                out _,
                out _,
                out _))
        {
            value += (int)System.Math.Round(expectedGoals * 400f);
        }
        else
        {
            int stepsToShot = CalculateMinimumDistanceToPitchFeature(
                controllerHex,
                opponentShootingHexes);
            if (stepsToShot >= 0 && stepsToShot <= controller.pace)
            {
                value += 55;
            }
        }

        int progress = CalculateForwardProgressDelta(
            snapshot,
            snapshot?.ball,
            RoomHexCoordinates.FromHex(controllerHex));
        value += System.Math.Max(0, progress) * 5;
        return value;
    }

    private static void PopulateFreeHeaderChoicePreview(
        RoomAIDecisionSnapshot snapshot,
        HeaderManager headerManager,
        RoomAICandidateExplanation explanation,
        IReadOnlyList<HexCell> opponentShootingHexes)
    {
        IReadOnlyList<HexCell> legalTargets = headerManager.GetLegalFreeHeaderTargetsForDecisionPreview();
        int bestValue = int.MinValue;
        RoomAICandidateExplanation best = null;
        for (int index = 0; index < legalTargets.Count; index++)
        {
            HexCell target = legalTargets[index];
            RoomActionCandidate previewAction = new RoomActionCandidate
            {
                manager = nameof(HeaderManager),
                actionType = RoomActionType.Header,
                step = RoomDecisionStep.ChooseTarget,
                targetHex = target,
                targetToken = target?.GetOccupyingToken()
            };
            RoomAICandidateExplanation preview = new RoomAICandidateExplanation
            {
                candidate = previewAction,
                targetHex = RoomHexCoordinates.FromHex(target)
            };
            PopulateHeaderPossessionChainFacts(
                snapshot,
                previewAction,
                preview,
                target,
                headerManager,
                opponentShootingHexes);

            RoomAIPossessionChainSummary chain = preview.possessionChain;
            float possession = chain?.attackRetainsPossessionProbability ?? 0f;
            float goals = chain?.goalProbability ?? 0f;
            float turnover = chain?.defenseWinsPossessionProbability ?? 0f;
            float unresolved = chain?.unresolvedProbability ?? 0f;
            // Unresolved space is not assumed safe. It receives only a small
            // continuation value and therefore cannot beat a reachable teammate
            // or shot route merely because no outcome was modelled.
            int value = (int)System.Math.Round(
                (possession * 100f)
                + (goals * 500f)
                - (turnover * 180f)
                + (unresolved * 10f));
            if (preview.headedPassImmediateSnapshotExpectedGoals >= 0f)
            {
                value += (int)System.Math.Round(preview.headedPassImmediateSnapshotExpectedGoals * 400f);
            }
            if (preview.headedPassSpaceRouteExpectedGoals >= 0f)
            {
                value += (int)System.Math.Round(preview.headedPassSpaceRouteExpectedGoals * 400f);
            }

            if (best == null || value > bestValue)
            {
                best = preview;
                bestValue = value;
            }
        }

        explanation.freeHeaderTargetsEvaluated = legalTargets.Count;
        if (best == null)
        {
            explanation.freeHeaderExpectedValue = -250;
            return;
        }

        explanation.freeHeaderBestTargetHex = best.targetHex;
        explanation.freeHeaderBestReceiverTokenKey = GetTokenKey(best.candidate.targetToken);
        explanation.freeHeaderExpectedPossessionProbability =
            best.possessionChain?.attackRetainsPossessionProbability ?? 0f;
        explanation.freeHeaderExpectedGoals = System.Math.Max(
            best.headedPassImmediateSnapshotExpectedGoals,
            best.headedPassSpaceRouteExpectedGoals);
        explanation.freeHeaderExpectedValue = bestValue;
    }

    private static IReadOnlyList<PlayerToken> ResolveHeaderControlCandidates(
        HeaderManager headerManager,
        bool defenseControls)
    {
        if (defenseControls)
        {
            return headerManager.defenderWillJump.Count > 0
                ? headerManager.defenderWillJump
                : headerManager.defEligibleToHead;
        }

        return headerManager.attackerWillJump.Count > 0
            ? headerManager.attackerWillJump
            : headerManager.attEligibleToHead;
    }

    private static void PopulateSetPieceHighPassExpectedGoals(
        RoomAIDecisionSnapshot snapshot,
        RoomActionCandidate action,
        RoomAICandidateExplanation explanation,
        HexCell requestedTarget)
    {
        MatchManager matchManager = MatchManager.Instance;
        FreeKickManager freeKick = matchManager?.freeKickManager;
        HighPassManager highPass = matchManager?.highPassManager;
        HexGrid hexGrid = matchManager?.hexGrid;
        bool isCornerKick = freeKick?.isCornerKick == true || highPass?.isCornerKick == true;
        bool isCommittedFreeKickHighPass = highPass?.isActivated == true
            && freeKick?.selectedKicker != null
            && freeKick.selectedKicker == matchManager?.LastTokenToTouchTheBallOnPurpose
            && freeKick.selectedKicker == matchManager?.setPieceTakerExcludedFromNextTouch;
        bool isActiveSetPiece = freeKick?.isActivated == true
            || (highPass?.isActivated == true && isCornerKick)
            || isCommittedFreeKickHighPass;
        if (snapshot == null
            || action == null
            || explanation == null
            || action.actionType != RoomActionType.HighPass
            || freeKick == null
            || !isActiveSetPiece
            || hexGrid?.cells == null)
        {
            return;
        }

        PlayerToken passer = freeKick.selectedKicker ?? matchManager.LastTokenToTouchTheBallOnPurpose;
        HexCell ballHex = freeKick.ball?.GetCurrentHex();
        if (passer == null || ballHex == null)
        {
            return;
        }

        float accuracyProbability = System.Math.Max(0f, System.Math.Min(1f, (passer.highPass - 1) / 6f));
        float bestHeaderGoalProbability = -1f;
        float bestInaccurateExpectedGoals = -1f;
        int bestInaccuracyOutcomesEvaluated = 0;
        Dictionary<HexCell, float> landingExpectedGoals = new Dictionary<HexCell, float>();
        if (requestedTarget != null)
        {
            if (IsLegalSetPieceHighPassPreviewTarget(freeKick, isCornerKick, ballHex, requestedTarget, passer))
            {
                bestHeaderGoalProbability = CalculateHeaderGoalProbabilityAtTarget(
                    hexGrid,
                    requestedTarget,
                    passer,
                    ballHex);
                bestInaccurateExpectedGoals = CalculateInaccurateHighPassExpectedGoals(
                    matchManager.looseBallManager,
                    hexGrid,
                    requestedTarget,
                    passer,
                    ballHex,
                    landingExpectedGoals,
                    out bestInaccuracyOutcomesEvaluated);
            }
        }
        else
        {
            foreach (HexCell target in hexGrid.cells)
            {
                if (!IsLegalSetPieceHighPassPreviewTarget(freeKick, isCornerKick, ballHex, target, passer))
                {
                    continue;
                }

                float headerGoalProbability = CalculateHeaderGoalProbabilityAtTarget(
                    hexGrid,
                    target,
                    passer,
                    ballHex);
                float inaccurateExpectedGoals = CalculateInaccurateHighPassExpectedGoals(
                    matchManager.looseBallManager,
                    hexGrid,
                    target,
                    passer,
                    ballHex,
                    landingExpectedGoals,
                    out int outcomesEvaluated);
                float combinedExpectedGoals = (accuracyProbability * headerGoalProbability)
                    + ((1f - accuracyProbability) * inaccurateExpectedGoals);
                float currentBestCombinedExpectedGoals = bestHeaderGoalProbability < 0f
                    ? -1f
                    : (accuracyProbability * bestHeaderGoalProbability)
                        + ((1f - accuracyProbability) * System.Math.Max(0f, bestInaccurateExpectedGoals));
                if (combinedExpectedGoals > currentBestCombinedExpectedGoals)
                {
                    bestHeaderGoalProbability = headerGoalProbability;
                    bestInaccurateExpectedGoals = inaccurateExpectedGoals;
                    bestInaccuracyOutcomesEvaluated = outcomesEvaluated;
                }
            }
        }

        if (bestHeaderGoalProbability < 0f)
        {
            return;
        }

        explanation.isSetPieceHighPassPlan = true;
        explanation.setPieceHighPassAccuracyProbability = accuracyProbability;
        explanation.setPieceHeaderGoalProbability = bestHeaderGoalProbability;
        explanation.setPieceInaccurateHighPassExpectedGoals = System.Math.Max(0f, bestInaccurateExpectedGoals);
        explanation.setPieceInaccuracyOutcomesEvaluated = bestInaccuracyOutcomesEvaluated;
        explanation.expectedGoals = (accuracyProbability * bestHeaderGoalProbability)
            + ((1f - accuracyProbability) * explanation.setPieceInaccurateHighPassExpectedGoals);
        explanation.possessionChain = BuildPartialSetPieceHighPassChain(
            accuracyProbability,
            bestHeaderGoalProbability,
            explanation.setPieceInaccurateHighPassExpectedGoals).Evaluate();
    }

    private static RoomAIPossessionChain BuildPartialSetPieceHighPassChain(
        float accuracyProbability,
        float accurateHeaderGoalProbability,
        float inaccurateHeaderGoalProbability)
    {
        float clampedAccuracy = System.Math.Max(0f, System.Math.Min(1f, accuracyProbability));
        float clampedAccurateGoal = System.Math.Max(0f, System.Math.Min(1f, accurateHeaderGoalProbability));
        float clampedInaccurateGoal = System.Math.Max(0f, System.Math.Min(1f, inaccurateHeaderGoalProbability));

        RoomAIPossessionChain accurateBranch = new RoomAIPossessionChain("Accurate High Pass")
            .AddTerminal("Immediate headed goal", clampedAccurateGoal, RoomAIPossessionOutcomeKind.Goal);
        RoomAIPossessionChain inaccurateBranch = new RoomAIPossessionChain("Inaccurate High Pass")
            .AddTerminal("Immediate headed goal after scatter", clampedInaccurateGoal, RoomAIPossessionOutcomeKind.Goal);

        return new RoomAIPossessionChain("Set-piece High Pass possession chain")
            .AddContinuation("Accuracy succeeds", clampedAccuracy, accurateBranch)
            .AddContinuation("Accuracy fails", 1f - clampedAccuracy, inaccurateBranch);
    }

    private static float CalculateInaccurateHighPassExpectedGoals(
        LooseBallManager looseBallManager,
        HexGrid hexGrid,
        HexCell intendedTarget,
        PlayerToken passer,
        HexCell passOrigin,
        Dictionary<HexCell, float> landingExpectedGoals,
        out int outcomesEvaluated)
    {
        outcomesEvaluated = 0;
        if (looseBallManager == null || hexGrid == null || intendedTarget == null || passer == null)
        {
            return 0f;
        }

        float totalExpectedGoals = 0f;
        for (int direction = 0; direction < 6; direction++)
        {
            for (int distance = 1; distance <= 6; distance++)
            {
                outcomesEvaluated++;
                HexCell landingHex = looseBallManager.CalculateFinalInaccuracyTarget(
                    intendedTarget,
                    direction,
                    distance).FinalHex;
                if (landingHex == null || landingHex.isOutOfBounds)
                {
                    continue;
                }

                if (!landingExpectedGoals.TryGetValue(landingHex, out float landingExpectedGoal))
                {
                    landingExpectedGoal = CalculateHeaderGoalProbabilityAtTarget(
                        hexGrid,
                        landingHex,
                        passer,
                        passOrigin);
                    landingExpectedGoals[landingHex] = landingExpectedGoal;
                }

                totalExpectedGoals += landingExpectedGoal;
            }
        }

        return outcomesEvaluated > 0 ? totalExpectedGoals / outcomesEvaluated : 0f;
    }

    private static void PopulateHighPassGoalkeeperDecision(
        RoomAIDecisionSnapshot snapshot,
        RoomActionCandidate action,
        RoomAICandidateExplanation explanation,
        HexCell requestedTarget)
    {
        MatchManager matchManager = MatchManager.Instance;
        HighPassManager highPass = matchManager?.highPassManager;
        HexGrid hexGrid = matchManager?.hexGrid;
        if (snapshot == null
            || action == null
            || explanation == null
            || highPass == null
            || !highPass.isActivated
            || !highPass.isWaitingForDefGKChallengeDecision
            || !string.Equals(action.manager, nameof(HighPassManager), System.StringComparison.Ordinal)
            || (action.actionType != RoomActionType.GoalkeeperSave
                && action.actionType != RoomActionType.Decline)
            || hexGrid == null)
        {
            return;
        }

        HexCell landingHex = highPass.finalTargetHex ?? matchManager.ball?.GetCurrentHex();
        PlayerToken passer = matchManager.LastTokenToTouchTheBallOnPurpose;
        PlayerToken goalkeeper = hexGrid.GetDefendingGK();
        if (landingHex == null || passer == null || goalkeeper == null)
        {
            return;
        }

        bool rushes = action.actionType == RoomActionType.GoalkeeperSave;
        HexCell goalkeeperDecisionHex = rushes
            ? requestedTarget ?? goalkeeper.GetCurrentHex()
            : null;
        explanation.isHighPassGoalkeeperDecision = true;
        explanation.goalkeeperRushesForHighPass = rushes;
        explanation.defensiveExpectedGoalsAgainst = CalculateHeaderGoalProbabilityAtTarget(
            hexGrid,
            landingHex,
            passer,
            passOrigin: null,
            forceGoalkeeperRush: rushes,
            forcedGoalkeeperHex: goalkeeperDecisionHex);
    }

    private static bool IsLegalSetPieceHighPassPreviewTarget(
        FreeKickManager freeKick,
        bool isCornerKick,
        HexCell ballHex,
        HexCell target,
        PlayerToken passer)
    {
        if (freeKick == null
            || ballHex == null
            || target == null
            || target.isOutOfBounds
            || !target.isAttackOccupied
            || target.GetOccupyingToken() == passer)
        {
            return false;
        }

        int distance = HexGridUtils.GetHexStepDistance(ballHex, target);
        if (isCornerKick)
        {
            if (ballHex.isInFinalThird * target.isInPenaltyBox != 1 && distance > 15)
            {
                return false;
            }
        }
        else if (distance < 3 || distance > 15)
        {
            return false;
        }

        List<HexCell> thickPath = freeKick.groundBallManager?.CalculateThickPath(
            ballHex,
            target,
            freeKick.ball != null ? freeKick.ball.ballRadius : 0f);
        if (thickPath != null)
        {
            foreach (HexCell pathHex in thickPath)
            {
                if (pathHex != null
                    && pathHex.isDefenseOccupied
                    && ballHex.GetNeighbors(freeKick.hexGrid).Contains(pathHex))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static float CalculateHeaderGoalProbabilityAtTarget(
        HexGrid hexGrid,
        HexCell target,
        PlayerToken passer,
        HexCell passOrigin = null,
        bool? forceGoalkeeperRush = null,
        HexCell forcedGoalkeeperHex = null)
    {
        if (hexGrid == null
            || target == null
            || !target.CanHeadFrom
            || target.HeadingPaths == null
            || target.HeadingPaths.Count == 0)
        {
            return 0f;
        }

        List<PlayerToken> attackers = new List<PlayerToken>();
        List<PlayerToken> defenders = new List<PlayerToken>();
        PlayerToken goalkeeper = hexGrid.GetDefendingGK();
        foreach (HexCell nearbyHex in HexGrid.GetHexesInRange(hexGrid, target, 2))
        {
            PlayerToken token = nearbyHex?.GetOccupyingToken();
            if (token == null || token == passer)
            {
                continue;
            }

            if (token.isAttacker)
            {
                attackers.Add(token);
            }
            else if (token != goalkeeper)
            {
                defenders.Add(token);
            }
        }

        SortByEffectiveHeaderDescending(attackers, target);
        SortByEffectiveHeaderDescending(defenders, target);
        if (attackers.Count > 2) attackers.RemoveRange(2, attackers.Count - 2);
        if (defenders.Count > 2) defenders.RemoveRange(2, defenders.Count - 2);
        if (attackers.Count == 0)
        {
            return 0f;
        }

        List<ExpectedStatsCalculator.AerialContestant> contestants = new List<ExpectedStatsCalculator.AerialContestant>();
        foreach (PlayerToken attacker in attackers)
        {
            contestants.Add(new ExpectedStatsCalculator.AerialContestant(
                attacker,
                EffectiveHeaderAttribute(attacker, target)));
        }
        foreach (PlayerToken defender in defenders)
        {
            contestants.Add(new ExpectedStatsCalculator.AerialContestant(
                defender,
                EffectiveHeaderAttribute(defender, target)));
        }
        float bestGoalTargetProbability = 0f;
        foreach (KeyValuePair<HexCell, List<HexCell>> headingPath in target.HeadingPaths)
        {
            HexCell goalTarget = headingPath.Key;
            if (goalTarget == null || goalTarget.isInGoal == 0)
            {
                continue;
            }

            float probability = CalculateBestGoalkeeperHeaderDefense(
                hexGrid,
                target,
                headingPath.Value,
                passOrigin,
                goalkeeper,
                attackers,
                contestants,
                forceGoalkeeperRush,
                forcedGoalkeeperHex);
            bestGoalTargetProbability = System.Math.Max(bestGoalTargetProbability, probability);
        }

        return System.Math.Min(1f, bestGoalTargetProbability);
    }

    private static float CalculateBestGoalkeeperHeaderDefense(
        HexGrid hexGrid,
        HexCell headerTarget,
        List<HexCell> goalPath,
        HexCell passOrigin,
        PlayerToken goalkeeper,
        List<PlayerToken> attackers,
        List<ExpectedStatsCalculator.AerialContestant> outfieldContestants,
        bool? forceGoalkeeperRush,
        HexCell forcedGoalkeeperHex)
    {
        if (goalkeeper?.GetCurrentHex() == null)
        {
            return CalculateHeaderGoalForGoalkeeperChoice(
                attackers,
                outfieldContestants,
                null,
                null,
                0);
        }

        List<HexCell> baseGoalkeeperHexes = GetGoalkeeperPostAccuracyBaseHexes(
            hexGrid,
            goalkeeper,
            passOrigin,
            headerTarget);
        float bestDefense = 1f;
        foreach (HexCell baseHex in baseGoalkeeperHexes)
        {
            if (forceGoalkeeperRush != true)
            {
                ResolveGoalkeeperSaveOnHeaderPath(
                    hexGrid,
                    goalkeeper,
                    baseHex,
                    goalPath,
                    out int? goalkeeperSaving,
                    out int goalkeeperPenalty);
                bestDefense = System.Math.Min(
                    bestDefense,
                    CalculateHeaderGoalForGoalkeeperChoice(
                        attackers,
                        outfieldContestants,
                        null,
                        goalkeeperSaving,
                        goalkeeperPenalty));
            }

            if (forceGoalkeeperRush == false || headerTarget.isInPenaltyBox == 0)
            {
                continue;
            }

            foreach (HexCell rushHex in GetGoalkeeperRushHexes(
                hexGrid,
                goalkeeper,
                baseHex,
                headerTarget,
                forcedGoalkeeperHex))
            {
                int rushDistanceToTarget = HexGridUtils.GetHexStepDistance(rushHex, headerTarget);
                ExpectedStatsCalculator.AerialContestant goalkeeperContestant =
                    new ExpectedStatsCalculator.AerialContestant(
                        goalkeeper,
                        goalkeeper.aerial + (rushDistanceToTarget > 1 ? -1 : 0));
                bestDefense = System.Math.Min(
                    bestDefense,
                    CalculateHeaderGoalForGoalkeeperChoice(
                        attackers,
                        outfieldContestants,
                        goalkeeperContestant,
                        null,
                        0));
            }
        }

        return bestDefense;
    }

    private static List<HexCell> GetGoalkeeperPostAccuracyBaseHexes(
        HexGrid hexGrid,
        PlayerToken goalkeeper,
        HexCell passOrigin,
        HexCell headerTarget)
    {
        HexCell goalkeeperHex = goalkeeper?.GetCurrentHex();
        List<HexCell> positions = new List<HexCell>();
        if (goalkeeperHex == null)
        {
            return positions;
        }

        positions.Add(goalkeeperHex);
        bool receivesBoxEntryMove = passOrigin != null
            && headerTarget != null
            && headerTarget.isInPenaltyBox != 0
            && passOrigin.isInPenaltyBox * headerTarget.isInPenaltyBox != 1;
        if (!receivesBoxEntryMove)
        {
            return positions;
        }

        foreach (HexCell candidate in HexGridUtils.GetReachableHexes(hexGrid, goalkeeperHex, 1).Item1)
        {
            if (candidate != null
                && !candidate.isOutOfBounds
                && (candidate.GetOccupyingToken() == null || candidate.GetOccupyingToken() == goalkeeper)
                && !positions.Contains(candidate))
            {
                positions.Add(candidate);
            }
        }

        return positions;
    }

    private static IEnumerable<HexCell> GetGoalkeeperRushHexes(
        HexGrid hexGrid,
        PlayerToken goalkeeper,
        HexCell baseHex,
        HexCell headerTarget,
        HexCell forcedGoalkeeperHex)
    {
        if (forcedGoalkeeperHex != null)
        {
            if (HexGridUtils.GetHexStepDistance(forcedGoalkeeperHex, headerTarget) <= 2)
            {
                yield return forcedGoalkeeperHex;
            }
            yield break;
        }

        List<HexCell> reachable = HexGridUtils.GetReachableHexes(hexGrid, baseHex, 3).Item1;
        if (!reachable.Contains(baseHex))
        {
            reachable.Add(baseHex);
        }

        foreach (HexCell candidate in reachable)
        {
            if (candidate == null
                || candidate.isOutOfBounds
                || HexGridUtils.GetHexStepDistance(candidate, headerTarget) > 2)
            {
                continue;
            }

            PlayerToken occupant = candidate.GetOccupyingToken();
            if (occupant == null || occupant == goalkeeper)
            {
                yield return candidate;
            }
        }
    }

    private static void ResolveGoalkeeperSaveOnHeaderPath(
        HexGrid hexGrid,
        PlayerToken goalkeeper,
        HexCell goalkeeperHex,
        List<HexCell> goalPath,
        out int? goalkeeperSaving,
        out int goalkeeperPenalty)
    {
        goalkeeperSaving = null;
        goalkeeperPenalty = 0;
        if (hexGrid == null || goalkeeper == null || goalkeeperHex == null || goalPath == null)
        {
            return;
        }

        HexCell saveHex = null;
        List<HexCell> saveableHexes = hexGrid.GetSavableHexes();
        foreach (HexCell pathHex in goalPath)
        {
            if (pathHex != null
                && saveableHexes.Contains(pathHex)
                && pathHex.coordinates.x == goalkeeperHex.coordinates.x
                && HexGridUtils.GetHexStepDistance(goalkeeperHex, pathHex) <= 3
                && MatchManager.Instance?.movementPhaseManager?.goalKeeperManager?.IsGoalkeeperOwnPenaltyHex(goalkeeper, pathHex) == true
                && (saveHex == null
                    || HexGridUtils.GetHexStepDistance(goalkeeperHex, pathHex)
                        < HexGridUtils.GetHexStepDistance(goalkeeperHex, saveHex)))
            {
                saveHex = pathHex;
            }
        }

        if (saveHex == null)
        {
            return;
        }

        goalkeeperSaving = goalkeeper.saving;
        goalkeeperPenalty = HexGridUtils.GetHexStepDistance(goalkeeperHex, saveHex) == 3 ? -1 : 0;
    }

    private static float CalculateHeaderGoalForGoalkeeperChoice(
        List<PlayerToken> attackers,
        List<ExpectedStatsCalculator.AerialContestant> outfieldContestants,
        ExpectedStatsCalculator.AerialContestant? goalkeeperContestant,
        int? goalkeeperSaving,
        int goalkeeperPenalty)
    {
        List<ExpectedStatsCalculator.AerialContestant> contestants =
            new List<ExpectedStatsCalculator.AerialContestant>(outfieldContestants);
        if (goalkeeperContestant.HasValue)
        {
            contestants.Add(goalkeeperContestant.Value);
        }

        ExpectedStatsCalculator.AerialContestant[] contestantArray = contestants.ToArray();
        float probability = 0f;
        foreach (PlayerToken attacker in attackers)
        {
            ExpectedStatsCalculator.AerialContestant shooter = contestants.Find(
                contestant => contestant.token == attacker);
            probability += ExpectedStatsCalculator.CalculateHeaderGoalProbability(
                shooter,
                contestantArray,
                shooterNaturalRollMustBeGreaterThanOne: true,
                keeperSaving: goalkeeperSaving,
                keeperPenalty: goalkeeperPenalty);
        }

        return System.Math.Min(1f, probability);
    }

    private static void SortByEffectiveHeaderDescending(List<PlayerToken> tokens, HexCell target)
    {
        tokens.Sort((left, right) =>
        {
            int attributeComparison = EffectiveHeaderAttribute(right, target)
                .CompareTo(EffectiveHeaderAttribute(left, target));
            return attributeComparison != 0
                ? attributeComparison
                : string.CompareOrdinal(DisplayName(left), DisplayName(right));
        });
    }

    private static int EffectiveHeaderAttribute(PlayerToken token, HexCell target)
    {
        if (token == null || target == null || token.GetCurrentHex() == null)
        {
            return 0;
        }

        int attribute = token.IsGoalKeeper ? token.aerial : token.heading;
        return HexGridUtils.GetHexStepDistance(target, token.GetCurrentHex()) > 1
            ? attribute - 1
            : attribute;
    }

    private static void PopulateSetPieceSetupFacts(
        RoomAIDecisionSnapshot snapshot,
        RoomActionCandidate action,
        RoomAICandidateExplanation explanation,
        HexCell targetHex)
    {
        FreeKickManager freeKick = MatchManager.Instance?.freeKickManager;
        if (snapshot == null
            || action == null
            || explanation == null
            || targetHex == null
            || freeKick == null
            || !freeKick.isActivated
            || action.actionType != RoomActionType.SetupMove
            || !string.Equals(action.manager, nameof(FreeKickManager), System.StringComparison.Ordinal))
        {
            return;
        }

        PlayerToken actor = action.actor ?? freeKick.selectedToken;
        if (actor == null)
        {
            return;
        }

        explanation.isSetPieceSetup = true;
        explanation.isCornerSetup = freeKick.isCornerKick;
        explanation.isCornerFreeThreeMove = freeKick.isCornerKick
            && (string.Equals(snapshot.currentState, MatchManager.GameState.FreeKickAttMovement3.ToString(), System.StringComparison.Ordinal)
                || string.Equals(snapshot.currentState, MatchManager.GameState.FreeKickDefMovement3.ToString(), System.StringComparison.Ordinal));
        explanation.setPieceActorIsAttacking = actor.isAttacker;
        explanation.setPieceActorHeading = actor.heading;
        if (freeKick.isCornerKick && !actor.isAttacker && !actor.IsGoalKeeper)
        {
            explanation.isCornerDefenseMove = true;
            explanation.cornerDefenseGain = EstimateCornerDanger(snapshot, null, null)
                - EstimateCornerDanger(snapshot, GetTokenKey(actor), RoomHexCoordinates.FromHex(targetHex));
        }
        explanation.targetDistanceToSetPieceBall = freeKick.ball?.GetCurrentHex() != null
            ? HexGridUtils.GetHexStepDistance(freeKick.ball.GetCurrentHex(), targetHex)
            : -1;

        RoomAITeamTacticalProfile actingTeamProfile = snapshot.tacticalMatchup?.ownTeam;
        if (actingTeamProfile != null)
        {
            explanation.setPieceActorHeaderRank = FindHeaderRank(actingTeamProfile, actor);
        }

        if (!actor.isAttacker && !freeKick.isCornerKick)
        {
            var shooter = MatchManager.Instance.playerTokenManager.allTokens
                .Where(token => token != null && token.isPlaying && token.isAttacker && token.GetCurrentHex() != null
                    && HexGridUtils.GetHexStepDistance(token.GetCurrentHex(), freeKick.ball.GetCurrentHex()) <= 1)
                .OrderByDescending(token => token.shooting).FirstOrDefault();
            if (shooter != null && MatchManager.Instance.shotManager.TryCalculateFreeKickDefensePreview(shooter, actor, targetHex, out float xg))
            {
                explanation.defensiveExpectedGoalsAgainst = xg;
                explanation.hasSetPieceShotDefense = true;
            }
        }
        int attackingGoalSide = ResolveExpectedTeamAttackingSide(snapshot);
        explanation.targetIsCornerHeaderAnchor = freeKick.isCornerKick
            && actor.isAttacker
            && attackingGoalSide != 0
            && targetHex.coordinates.x == attackingGoalSide * 14
            && (System.Math.Abs(targetHex.coordinates.z) == 0
                || System.Math.Abs(targetHex.coordinates.z) == 3
                || System.Math.Abs(targetHex.coordinates.z) == 4);

        foreach (PlayerToken token in MatchManager.Instance.hexGrid.GetAttackers())
        {
            if (token == null || token == actor || token.GetCurrentHex() == null)
            {
                continue;
            }

            int distance = HexGridUtils.GetHexStepDistance(targetHex, token.GetCurrentHex());
            if (distance == 1 && token.heading >= 5)
            {
                explanation.adjacentVeryGoodAttackingHeaders++;
            }

            if (!actor.isAttacker && distance == 1)
            {
                explanation.adjacentMarkedAttackerHeading = System.Math.Max(
                    explanation.adjacentMarkedAttackerHeading,
                    token.heading);
            }
            else if (!actor.isAttacker && distance == 2)
            {
                explanation.twoHexMarkedAttackerHeading = System.Math.Max(
                    explanation.twoHexMarkedAttackerHeading,
                    token.heading);
            }
        }
    }

    private static float EstimateCornerDanger(RoomAIDecisionSnapshot snapshot, string movedKey, RoomHexCoordinates destination)
    {
        int goalSide = -ResolveExpectedTeamAttackingSide(snapshot);
        var defenders = snapshot.tokens.Where(token => token.isPlaying && !token.isGoalKeeper
            && token.teamSide == snapshot.expectedTeam && token.currentHex != null).ToList();
        float danger = 0f;
        foreach (var attacker in snapshot.tokens.Where(token => token.isPlaying && !token.isGoalKeeper
            && token.teamSide != snapshot.expectedTeam && token.currentHex != null))
        {
            HexCell origin = MatchManager.Instance.hexGrid.GetHexCellAt(
                new UnityEngine.Vector3Int(attacker.currentHex.x, 0, attacker.currentHex.z), false);
            bool header = origin?.HeadingPaths?.Keys.Any(goal => goal != null && goal.isInGoal == goalSide) == true;
            bool shot = origin?.ShootingPaths?.Keys.Any(goal => goal != null && goal.isInGoal == goalSide) == true;
            // The corner taker cannot dribble. Price actual receivers/headers,
            // rather than rewarding pursuit of the corner flag itself.
            if (!header && !shot) continue;
            float threat = (header ? attacker.heading * attacker.heading : 0f)
                + (shot ? attacker.shooting * attacker.shooting * .35f : 0f);
            float control = 0f;
            foreach (var defender in defenders)
            {
                var at = defender.tokenKey == movedKey && destination != null ? destination : defender.currentHex;
                int distance = RoomAITacticalSafety.Distance(at, attacker.currentHex);
                if (distance > 2) continue;
                float quality = header ? defender.heading / 6f : defender.tackling / 6f;
                float goalSideBonus = goalSide * (at.x - attacker.currentHex.x) >= 0 ? 1f : .8f;
                control = System.Math.Max(control, quality * goalSideBonus * (distance <= 1 ? .85f : .5f));
            }
            danger += threat / 36f * (1f - control);
        }
        return danger;
    }

    private static int FindHeaderRank(RoomAITeamTacticalProfile profile, PlayerToken actor)
    {
        if (profile?.outfieldPlayers == null || actor == null)
        {
            return -1;
        }

        int rank = 1;
        foreach (RoomAITokenSnapshot player in profile.outfieldPlayers)
        {
            if (player != null && player.heading > actor.heading)
            {
                rank++;
            }
        }

        return rank;
    }

    private static void PopulateWalkInExpectedGoalProbability(
        RoomAIDecisionSnapshot snapshot,
        RoomActionCandidate action,
        RoomAICandidateExplanation explanation,
        HexCell targetHex)
    {
        if (snapshot == null
            || action == null
            || explanation == null
            || !explanation.targetIsInOpponentGoal
            || targetHex == null)
        {
            return;
        }

        MatchManager matchManager = MatchManager.Instance;
        HexGrid hexGrid = matchManager?.hexGrid;
        GoalKeeperManager goalkeeperManager = matchManager?.movementPhaseManager?.goalKeeperManager;
        PlayerToken dribbler = action.actor ?? matchManager?.movementPhaseManager?.selectedToken;
        PlayerToken goalkeeper = hexGrid?.GetDefendingGK();
        HexCell startHex = dribbler?.GetCurrentHex();
        HexCell goalkeeperHex = goalkeeper?.GetCurrentHex();
        if (hexGrid == null || dribbler == null || startHex == null)
        {
            return;
        }

        List<HexCell> path = HexGridUtils.FindPath(startHex, targetHex, hexGrid);
        if (path == null || path.Count == 0)
        {
            return;
        }

        explanation.walkInExpectedGoalProbability = 1f;
        if (goalkeeper == null || goalkeeperHex == null || goalkeeperManager == null)
        {
            explanation.expectedGoals = explanation.walkInExpectedGoalProbability;
            return;
        }

        bool entersPenaltyBox = !IsOpponentPenaltyBox(snapshot, startHex);
        int firstPenaltyPathIndex = -1;
        for (int index = 0; index < path.Count; index++)
        {
            if (IsOpponentPenaltyBox(snapshot, path[index]))
            {
                firstPenaltyPathIndex = index;
                break;
            }
        }

        float bestGoalkeeperStopPlan = 1f;
        bool wallChallengeFound = false;
        int bestWallPenalty = 0;
        foreach (HexCell possibleGoalkeeperHex in EnumerateGoalkeeperPositionsForWalkIn(
            hexGrid,
            goalkeeperManager,
            goalkeeper,
            goalkeeperHex,
            entersPenaltyBox && firstPenaltyPathIndex >= 0))
        {
            int pathStart = firstPenaltyPathIndex >= 0 ? firstPenaltyPathIndex : 0;
            for (int index = pathStart; index < path.Count; index++)
            {
                HexCell step = path[index];
                if (step == null || step.coordinates.x != possibleGoalkeeperHex.coordinates.x)
                {
                    continue;
                }

                int wallDistance = HexGridUtils.GetHexStepDistance(possibleGoalkeeperHex, step);
                if (wallDistance < 1 || wallDistance > 3)
                {
                    continue;
                }

                int goalkeeperPenalty = wallDistance == 3 ? -1 : 0;
                ExpectedStatsCalculator.GroundDuelExpectation duel =
                    ExpectedStatsCalculator.CalculateGroundDuelExpectationWithDefenderSkill(
                        dribbler,
                        goalkeeper,
                        goalkeeper.saving,
                        goalkeeperPenalty);
                if (!wallChallengeFound || duel.dribbleContinuationProbability < bestGoalkeeperStopPlan)
                {
                    wallChallengeFound = true;
                    bestGoalkeeperStopPlan = duel.dribbleContinuationProbability;
                    bestWallPenalty = goalkeeperPenalty;
                }
                break;
            }
        }

        if (wallChallengeFound)
        {
            explanation.walkInExpectedGoalProbability = bestGoalkeeperStopPlan;
            explanation.targetIsThirdGoalkeeperWallHex = bestWallPenalty == -1;
            explanation.goalkeeperPenalty = bestWallPenalty;
        }
        explanation.expectedGoals = explanation.walkInExpectedGoalProbability;
    }

    private static IEnumerable<HexCell> EnumerateGoalkeeperPositionsForWalkIn(
        HexGrid hexGrid,
        GoalKeeperManager goalkeeperManager,
        PlayerToken goalkeeper,
        HexCell goalkeeperHex,
        bool receivesFreePenaltyBoxMove)
    {
        if (!receivesFreePenaltyBoxMove || hexGrid?.cells == null)
        {
            yield return goalkeeperHex;
            yield break;
        }

        foreach (HexCell candidate in hexGrid.cells)
        {
            if (candidate == null
                || !goalkeeperManager.IsGoalkeeperOwnPenaltyHex(goalkeeper, candidate)
                || HexGridUtils.GetHexStepDistance(goalkeeperHex, candidate) > goalkeeper.pace)
            {
                continue;
            }

            PlayerToken occupant = candidate.GetOccupyingToken();
            if (occupant == null || occupant == goalkeeper)
            {
                yield return candidate;
            }
        }
    }

    private static void PopulateShotExpectedGoals(
        RoomActionCandidate action,
        RoomAICandidateExplanation explanation)
    {
        if (action?.actionType != RoomActionType.Shot
            || explanation == null
            || string.Equals(action.manager, nameof(HeaderManager), System.StringComparison.Ordinal))
        {
            return;
        }

        ShotManager shotManager = MatchManager.Instance?.shotManager;
        bool calculated = false;
        if (shotManager != null && action.targetHex != null)
        {
            calculated = shotManager.TryCalculateExpectedGoalsPreview(
                action.targetHex,
                out explanation.expectedGoals,
                out explanation.shootingPenalty,
                out explanation.shotBlockerCount,
                out explanation.goalkeeperPenalty);
        }
        else if (shotManager != null)
        {
            calculated = shotManager.TryCalculateBestExpectedGoalsPreview(
                out explanation.expectedGoals,
                out explanation.shootingPenalty,
                out explanation.shotBlockerCount,
                out explanation.goalkeeperPenalty);
        }

        if (!calculated)
        {
            explanation.expectedGoals = -1f;
        }
    }

    private static bool TryGetCachedExpectedGoalsPreview(
        RoomAIDecisionSnapshot snapshot,
        ShotManager shotManager,
        PlayerToken shooter,
        HexCell shooterHex,
        string shotType,
        out float expectedGoals,
        out int shootingPenalty,
        out int blockerCount,
        out int goalkeeperPenalty)
    {
        expectedGoals = -1f;
        shootingPenalty = 0;
        blockerCount = 0;
        goalkeeperPenalty = 0;
        if (snapshot == null || shotManager == null || shooter == null || shooterHex == null)
        {
            return false;
        }

        string key = $"{GetTokenKey(shooter)}|{shooterHex.coordinates.x},{shooterHex.coordinates.z}|{shotType}";
        if (!snapshot.expectedGoalsCache.TryGetValue(key, out RoomAIExpectedGoalsPreview preview))
        {
            preview = new RoomAIExpectedGoalsPreview();
            preview.calculated = shotManager.TryCalculateBestExpectedGoalsPreview(
                shooter,
                shooterHex,
                shotType,
                out preview.expectedGoals,
                out preview.shootingPenalty,
                out preview.blockerCount,
                out preview.goalkeeperPenalty);
            snapshot.expectedGoalsCache[key] = preview;
        }

        expectedGoals = preview.expectedGoals;
        shootingPenalty = preview.shootingPenalty;
        blockerCount = preview.blockerCount;
        goalkeeperPenalty = preview.goalkeeperPenalty;
        return preview.calculated;
    }

    public static RoomAIReachabilityPreview GetCachedReachability(
        RoomAIDecisionSnapshot snapshot,
        HexGrid grid,
        HexCell start,
        int range)
    {
        if (snapshot == null || grid == null || start == null)
        {
            return new RoomAIReachabilityPreview
            {
                reachableHexes = new List<HexCell>(),
                distances = new Dictionary<HexCell, (int distance, bool enteredZOI)>()
            };
        }

        string key = $"{start.coordinates.x},{start.coordinates.z}|{System.Math.Max(0, range)}";
        if (snapshot.reachabilityCache.TryGetValue(key, out RoomAIReachabilityPreview cached))
        {
            snapshot.reachabilityCacheHits++;
            return cached;
        }

        var (reachableHexes, distances) = HexGridUtils.GetReachableHexes(
            grid,
            start,
            System.Math.Max(0, range));
        RoomAIReachabilityPreview preview = new RoomAIReachabilityPreview
        {
            reachableHexes = reachableHexes ?? new List<HexCell>(),
            distances = distances
                ?? new Dictionary<HexCell, (int distance, bool enteredZOI)>()
        };
        snapshot.reachabilityCache[key] = preview;
        return preview;
    }

    private static int CalculateMinimumDistanceToPitchFeature(
        HexCell origin,
        IReadOnlyList<HexCell> featureHexes)
    {
        if (origin == null || featureHexes == null)
        {
            return -1;
        }

        int minimum = int.MaxValue;
        foreach (HexCell hex in featureHexes)
        {
            if (hex == null)
            {
                continue;
            }

            minimum = System.Math.Min(minimum, HexGridUtils.GetHexStepDistance(origin, hex));
        }

        return minimum == int.MaxValue ? -1 : minimum;
    }

    private static List<HexCell> CollectPitchFeatureHexes(
        RoomAIDecisionSnapshot snapshot,
        System.Predicate<HexCell> predicate)
    {
        List<HexCell> matches = new List<HexCell>();
        HexGrid grid = MatchManager.Instance?.hexGrid;
        if (snapshot == null || predicate == null || grid?.cells == null)
        {
            return matches;
        }

        foreach (HexCell hex in grid.cells)
        {
            if (hex != null && !hex.isOutOfBounds && predicate(hex))
            {
                matches.Add(hex);
            }
        }

        return matches;
    }

    private static bool IsOpponentPenaltyBox(RoomAIDecisionSnapshot snapshot, HexCell hex)
    {
        int opponentSide = ResolveExpectedTeamAttackingSide(snapshot);
        return hex != null && opponentSide != 0 && hex.isInPenaltyBox == opponentSide;
    }

    private static bool IsOpponentShootingHex(RoomAIDecisionSnapshot snapshot, HexCell hex)
    {
        int opponentSide = ResolveExpectedTeamAttackingSide(snapshot);
        return hex != null
            && opponentSide != 0
            && hex.CanShootFrom
            && System.Math.Sign(hex.coordinates.x) == opponentSide;
    }

    private static bool IsOpponentHeadingHex(RoomAIDecisionSnapshot snapshot, HexCell hex)
    {
        int opponentSide = ResolveExpectedTeamAttackingSide(snapshot);
        return hex != null
            && opponentSide != 0
            && hex.CanHeadFrom
            && System.Math.Sign(hex.coordinates.x) == opponentSide;
    }

    private static bool IsOpponentGoalHex(RoomAIDecisionSnapshot snapshot, HexCell hex)
    {
        int opponentSide = ResolveExpectedTeamAttackingSide(snapshot);
        return hex != null && opponentSide != 0 && hex.isInGoal == opponentSide;
    }

    private static bool IsBallCarrierMovementTarget(RoomActionCandidate action)
    {
        MatchManager matchManager = MatchManager.Instance;
        PlayerToken selectedToken = matchManager?.movementPhaseManager?.selectedToken;
        return action?.actionType == RoomActionType.StartMovement
            && action.step == RoomDecisionStep.ChooseTarget
            && selectedToken != null
            && matchManager?.ball?.GetCurrentHex()?.GetOccupyingToken() == selectedToken;
    }

    private static bool IsThirdGoalkeeperWallHex(HexCell hex)
    {
        MatchManager matchManager = MatchManager.Instance;
        GoalKeeperManager goalKeeperManager = matchManager?.movementPhaseManager?.goalKeeperManager;
        PlayerToken goalkeeper = matchManager?.hexGrid?.GetDefendingGK();
        return hex != null
            && goalkeeper != null
            && goalKeeperManager != null
            && goalKeeperManager.GetGoalkeeperWallHexes(goalkeeper, includeGoalkeeperHex: false).Contains(hex)
            && goalKeeperManager.CalculateGoalkeeperWallPenalty(goalkeeper, hex) == -1;
    }

    private static int ResolveExpectedTeamAttackingSide(RoomAIDecisionSnapshot snapshot)
    {
        string direction = ResolveExpectedTeamDirection(snapshot);
        if (string.Equals(direction, "LeftToRight", System.StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        return string.Equals(direction, "RightToLeft", System.StringComparison.OrdinalIgnoreCase) ? -1 : 0;
    }

    private static int CountGroundPassInterceptionCandidates(RoomActionCandidate action, HexCell targetHex)
    {
        MatchManager matchManager = MatchManager.Instance;
        if (action == null
            || action.actionType != RoomActionType.GroundPass
            || targetHex == null
            || matchManager?.hexGrid == null
            || matchManager.groundBallManager?.ball == null)
        {
            return 0;
        }

        return GroundPassCommon.BuildOrderedInterceptionCandidates(
            matchManager.hexGrid,
            matchManager.groundBallManager.ball,
            targetHex).Count;
    }

    private static RoomAITokenSnapshot ResolveTokenSnapshot(
        string tokenKey,
        Dictionary<string, RoomAITokenSnapshot> tokenLookup)
    {
        if (string.IsNullOrWhiteSpace(tokenKey) || tokenLookup == null)
        {
            return null;
        }

        tokenLookup.TryGetValue(tokenKey, out RoomAITokenSnapshot token);
        return token;
    }

    private static int ResolveTargetDistanceToOpponentGoal(RoomAIDecisionSnapshot snapshot, HexCell targetHex)
    {
        if (snapshot == null || targetHex == null)
        {
            return -1;
        }

        string direction = ResolveExpectedTeamDirection(snapshot);
        int distance = string.Equals(direction, "RightToLeft", System.StringComparison.OrdinalIgnoreCase)
            ? targetHex.DistanceToLeftGoal
            : string.Equals(direction, "LeftToRight", System.StringComparison.OrdinalIgnoreCase)
                ? targetHex.DistanceToRightGoal
                : int.MaxValue;
        return distance == int.MaxValue ? -1 : distance;
    }

    private static int CalculateNearestOpponentDistance(
        RoomAIDecisionSnapshot snapshot,
        RoomHexCoordinates targetHex)
    {
        if (snapshot == null || targetHex == null)
        {
            return -1;
        }

        int nearestDistance = int.MaxValue;
        foreach (RoomAITokenSnapshot token in snapshot.tokens)
        {
            if (!IsPlayingOpponent(snapshot, token))
            {
                continue;
            }

            int distance = CalculateHexDistance(token.currentHex, targetHex);
            if (distance >= 0 && distance < nearestDistance)
            {
                nearestDistance = distance;
            }
        }

        return nearestDistance == int.MaxValue ? -1 : nearestDistance;
    }

    private static int CountOpponentsWithinTackleReach(
        RoomAIDecisionSnapshot snapshot,
        RoomHexCoordinates targetHex)
    {
        if (snapshot == null || targetHex == null)
        {
            return 0;
        }

        int count = 0;
        if (snapshot.opponentChallengeCounts.TryGetValue((targetHex.x, targetHex.z), out int cached)) return cached;
        HexGrid grid = MatchManager.Instance?.hexGrid;
        UnityEngine.Vector3Int targetCoordinates = new UnityEngine.Vector3Int(targetHex.x, 0, targetHex.z);
        foreach (RoomAITokenSnapshot token in snapshot.tokens)
        {
            if (!IsPlayingOpponent(snapshot, token))
            {
                continue;
            }

            int distance = CalculateHexDistance(token.currentHex, targetHex);
            if (distance >= 0 && distance <= token.pace + 1)
            {
                if (grid?.cells == null)
                {
                    count++;
                    continue;
                }
                if (!snapshot.opponentChallengeReach.TryGetValue(token.tokenKey, out var reachable))
                {
                    var start = grid.GetHexCellAt(new UnityEngine.Vector3Int(token.currentHex.x, 0, token.currentHex.z), false);
                    if (start == null) continue;
                    // The carrier's original hex is empty after its hypothetical
                    // move. Do not let that stale occupancy block a real chase.
                    var vacated = new HashSet<HexCell>();
                    if (MatchManager.Instance?.ball?.GetCurrentHex() is HexCell origin) vacated.Add(origin);
                    reachable = HexGridUtils.GetReachableHexes(grid, start, token.pace, vacated).reachableHexes;
                    snapshot.opponentChallengeReach[token.tokenKey] = reachable;
                }
                if (reachable.Any(hex => HexGridUtils.GetHexStepDistance(hex.coordinates, targetCoordinates) <= 1)) count++;
            }
        }

        snapshot.opponentChallengeCounts[(targetHex.x, targetHex.z)] = count;
        return count;
    }

    private static int FindBestReachableOpponentTackling(
        RoomAIDecisionSnapshot snapshot,
        RoomHexCoordinates targetHex)
    {
        if (snapshot == null || targetHex == null)
        {
            return 0;
        }

        int bestTackling = 0;
        foreach (RoomAITokenSnapshot token in snapshot.tokens)
        {
            if (!IsPlayingOpponent(snapshot, token))
            {
                continue;
            }

            int distance = CalculateHexDistance(token.currentHex, targetHex);
            if (distance >= 0 && distance <= token.pace + 1)
            {
                bestTackling = System.Math.Max(bestTackling, token.tackling);
            }
        }

        return bestTackling;
    }

    private static int CountFriendlySupportWithinRange(
        RoomAIDecisionSnapshot snapshot,
        RoomHexCoordinates targetHex,
        RoomAITokenSnapshot evaluatedToken)
    {
        if (snapshot == null || targetHex == null)
        {
            return 0;
        }

        int count = 0;
        foreach (RoomAITokenSnapshot token in snapshot.tokens)
        {
            if (token == null
                || token == evaluatedToken
                || !token.isPlaying
                || !string.Equals(token.teamSide, snapshot.expectedTeam, System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            int distance = CalculateHexDistance(token.currentHex, targetHex);
            if (distance >= 0 && distance <= token.pace)
            {
                count++;
            }
        }

        return count;
    }

    private static bool IsPlayingOpponent(RoomAIDecisionSnapshot snapshot, RoomAITokenSnapshot token)
    {
        return snapshot != null
            && token != null
            && token.isPlaying
            && !string.IsNullOrWhiteSpace(token.teamSide)
            && !string.Equals(token.teamSide, snapshot.expectedTeam, System.StringComparison.OrdinalIgnoreCase);
    }

    private static string GetTokenKey(PlayerToken token)
    {
        return token != null ? MatchManager.GetStableTokenKey(token) : string.Empty;
    }

    private static RoomHexCoordinates ResolveTokenHex(
        string tokenKey,
        PlayerToken token,
        Dictionary<string, RoomAITokenSnapshot> tokenLookup)
    {
        if (!string.IsNullOrWhiteSpace(tokenKey)
            && tokenLookup != null
            && tokenLookup.TryGetValue(tokenKey, out RoomAITokenSnapshot tokenSnapshot))
        {
            return tokenSnapshot.currentHex;
        }

        return token != null ? RoomHexCoordinates.FromHex(token.GetCurrentHex()) : null;
    }

    private static int CalculateHexDistance(RoomHexCoordinates from, RoomHexCoordinates to)
    {
        if (from == null || to == null)
        {
            return -1;
        }

        return RoomAITacticalSafety.Distance(from, to);
    }

    private static int CalculateForwardProgressDelta(
        RoomAIDecisionSnapshot snapshot,
        RoomHexCoordinates from,
        RoomHexCoordinates to)
    {
        if (snapshot == null || from == null || to == null)
        {
            return 0;
        }

        string direction = ResolveExpectedTeamDirection(snapshot);
        if (string.Equals(direction, "RightToLeft", System.StringComparison.OrdinalIgnoreCase))
        {
            return from.x - to.x;
        }

        if (string.Equals(direction, "LeftToRight", System.StringComparison.OrdinalIgnoreCase))
        {
            return to.x - from.x;
        }

        return 0;
    }

    private static string ResolveExpectedTeamDirection(RoomAIDecisionSnapshot snapshot)
    {
        if (snapshot == null)
        {
            return string.Empty;
        }

        if (string.Equals(snapshot.expectedTeam, "Home", System.StringComparison.OrdinalIgnoreCase))
        {
            return snapshot.homeTeamDirection;
        }

        if (string.Equals(snapshot.expectedTeam, "Away", System.StringComparison.OrdinalIgnoreCase))
        {
            return snapshot.awayTeamDirection;
        }

        if (string.Equals(snapshot.teamInAttack, "Home", System.StringComparison.OrdinalIgnoreCase))
        {
            return snapshot.homeTeamDirection;
        }

        if (string.Equals(snapshot.teamInAttack, "Away", System.StringComparison.OrdinalIgnoreCase))
        {
            return snapshot.awayTeamDirection;
        }

        return string.Empty;
    }

    private static RoomRuntimeSnapshot TryCreateRuntimeSnapshot()
    {
        MatchManager matchManager = MatchManager.Instance;
        if (matchManager == null)
        {
            return null;
        }

        try
        {
            return matchManager.CreateRuntimeSnapshot();
        }
        catch (System.Exception exception)
        {
            UnityEngine.Debug.LogWarning($"Room AI decision snapshot could not include runtime state: {exception.Message}");
            return null;
        }
    }

    private static RoomAITokenSnapshot CreateTokenSnapshot(RoomTokenSnapshot token)
    {
        return new RoomAITokenSnapshot
        {
            tokenKey = token.tokenKey,
            teamSide = token.teamSide,
            jerseyNumber = token.jerseyNumber,
            playerName = token.playerName,
            currentHex = token.currentHex,
            isPlaying = token.isPlaying,
            isGoalKeeper = token.isGoalKeeper,
            isAttacker = token.isAttacker,
            wasSubbedOn = token.wasSubbedOn,
            isBooked = token.isBooked,
            isSentOff = token.isSentOff,
            isInjured = token.isInjured,
            requiresSubstitution = token.requiresSubstitution,
            pace = token.pace,
            dribbling = token.dribbling,
            highPass = token.highPass,
            resilience = token.resilience,
            heading = token.heading,
            shooting = token.shooting,
            tackling = token.tackling,
            aerial = token.aerial,
            saving = token.saving,
            handling = token.handling
        };
    }
}
