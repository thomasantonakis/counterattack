using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

public class ShotManager : MonoBehaviour
{
    private const string FreeKickShotType = "freeKick";
    private const string PenaltyShotType = "penalty";

    [Header("Dependencies")]
    public MovementPhaseManager movementPhaseManager;
    public GameInputManager gameInputManager;
    public GroundBallManager groundBallManager;
    public LooseBallManager looseBallManager;
    public FinalThirdManager finalThirdManager;
    public LongBallManager longBallManager;
    public OutOfBoundsManager outOfBoundsManager;
    public GoalKeeperManager goalKeeperManager;
    public GoalFlowManager goalFlowManager;
    public GKPushManager gkPushManager;
    public HelperFunctions helperFunctions;
    public HexGrid hexGrid;
    public Ball ball;
    [Header("Flags")]
    public bool isAvailable = false;
    public bool isActivated = false;  // Tracks if a shot is active
    public bool isWaitingforBlockerSelection = false;  // Tracks if we are waiting to select a blocker
    public bool isWaitingforBlockerMovement = false;  // Tracks if we are waiting for the selected blocker to move
    public bool isWaitingForTargetSelection = false;  // Tracks if we are waiting for shot target selection
    public bool isWaitingForBlockDiceRoll = false;  // Tracks we are in the Blocking Phase
    public bool isWaitingForShotRoll = false;  // Tracks we are in the Blocking Phase
    public bool isWaitingForGKDiceRoll = false;
    public bool isWaitingforHandlingTest = false;
    public bool isWaitingForSaveandHoldScenario = false;
    public bool gkWasOfferedMoveForBox = false;
    public bool isWaitingForSnapshotDecisionFromLoose = false;
    public bool isWaitingForShotCommitConfirmation = false;
    public string shotType; // "snapshot" or "fullPower"
    [Header("Important Runtime Items")]
    public PlayerToken shooter; // The token that is shooting
    public int totalShotPower;
    public int shooterRoll;
    public string boxPenalty;
    public string snapPenalty;
    public string difficultPenalty;
    public string shootingPenaltyInfo;
    private bool shooterRollWasJackpot;
    public PlayerToken tokenMoveforDeflection; // The token that is shooting
    public HexCell targetHex; // The CanShootTo hex selected by the attacker
    public HexCell saveHex; // The hex (if any) where the GK will make the save on.
    public HexCell currentDefenderBlockingHex; // The Hex of the defender currently attempting to intercept
    private List<HexCell> trajectoryPath; // The list of hexes the ball will travel through
    private List<ShotInteraction> interceptors = new(); // Defenders trying to interact with the shot
    private ShotInteraction currentShotInteraction;
    private readonly List<ShotInteraction> expectedGoalOutfieldBlockInteractions = new();
    private ShotInteraction expectedGoalGkSaveInteraction;
    private bool expectedGoalLogged;
    public List<PlayerToken> alreadyInterceptedDefs;

    // Header-at-goal specific
    public bool isHeaderAtGoal = false;
    private int headerAttackerTotalScore;
    private PlayerToken headerAttacker;
    private HexCell headerTargetHex;
    private int headerGkPenalty = 0;
    private readonly List<HexCell> shotCommitPreviewTargets = new();
    private readonly List<HexCell> shotCommitPreviewPath = new();
    private HexCell shotCommitPreviewOriginHex;
    private HexCell freeKickShotOriginHex;
    private HexCell penaltyShotOriginHex;
    private HexCell hoveredShotCommitPreviewTarget;
    private readonly List<HexCell> shotTargetSelectionTargets = new();
    private readonly List<HexCell> targetSelectionPreviewPath = new();
    private HexCell hoveredTargetSelectionPreviewTarget;
    private HexCell targetSelectionPreviewSaveHex;
    private MatchManager.MatchActionKind committedShotActionKind = MatchManager.MatchActionKind.None;
    private bool shotActionResolutionPending;
    private bool shotActionResolvedRecorded;
    private bool deferShotActionResolutionUntilExternalRestart;
    private bool snapshotActionRecorded;
    private bool pendingSnapshotEndedMovementPhase;
    private bool pendingSnapshotEndedMovementConsumesExtraAction;
    private HexCell hoveredSnapshotBlockerMovementHex;

    private enum ShotInteractionType
    {
        OutfieldBlock,
        GKSave
    }

    private sealed class ShotInteraction
    {
        public PlayerToken defender;
        public HexCell interactionHex;
        public ShotInteractionType type;
        public int pathIndex;
        public int requiredNaturalRoll;
        public int? gkPenalty;

        public bool IsGK => type == ShotInteractionType.GKSave;
    }

    private void ResetExpectedGoalContext()
    {
        expectedGoalOutfieldBlockInteractions.Clear();
        expectedGoalGkSaveInteraction = null;
        expectedGoalLogged = false;
    }

    private static ShotInteraction CloneShotInteraction(ShotInteraction interaction)
    {
        if (interaction == null)
        {
            return null;
        }

        return new ShotInteraction
        {
            defender = interaction.defender,
            interactionHex = interaction.interactionHex,
            type = interaction.type,
            pathIndex = interaction.pathIndex,
            requiredNaturalRoll = interaction.requiredNaturalRoll,
            gkPenalty = interaction.gkPenalty
        };
    }

    private void CaptureExpectedGoalInteractions(IEnumerable<ShotInteraction> interactions)
    {
        expectedGoalOutfieldBlockInteractions.Clear();
        expectedGoalGkSaveInteraction = null;

        if (interactions == null)
        {
            return;
        }

        foreach (ShotInteraction interaction in interactions)
        {
            if (interaction == null)
            {
                continue;
            }

            if (interaction.IsGK)
            {
                expectedGoalGkSaveInteraction = CloneShotInteraction(interaction);
            }
            else
            {
                expectedGoalOutfieldBlockInteractions.Add(CloneShotInteraction(interaction));
            }
        }
    }

    private void UpdateExpectedGoalGkInteraction(ShotInteraction interaction)
    {
        expectedGoalGkSaveInteraction = CloneShotInteraction(interaction);
    }

    private bool IsFreeKickShot()
    {
        return shotType == FreeKickShotType;
    }

    private bool IsPenaltyShot()
    {
        return shotType == PenaltyShotType;
    }

    private HexCell GetFreeKickShotOriginHex()
    {
        return freeKickShotOriginHex != null ? freeKickShotOriginHex : ball?.GetCurrentHex();
    }

    private HexCell GetShotOriginHex()
    {
        if (IsFreeKickShot())
        {
            return GetFreeKickShotOriginHex();
        }

        if (IsPenaltyShot())
        {
            return penaltyShotOriginHex != null ? penaltyShotOriginHex : shooter?.GetCurrentHex();
        }

        return shooter?.GetCurrentHex();
    }

    private HexCell GetShotCommitPreviewOriginHex()
    {
        if (shotCommitPreviewOriginHex != null)
        {
            return shotCommitPreviewOriginHex;
        }

        PlayerToken shootingToken = MatchManager.Instance.LastTokenToTouchTheBallOnPurpose;
        return shootingToken?.GetCurrentHex();
    }

    private bool IsValidShotOriginForAttackingDirection(HexCell originHex)
    {
        if (originHex == null || !originHex.CanShootFrom || originHex.ShootingPaths == null || originHex.ShootingPaths.Count == 0)
        {
            return false;
        }

        MatchManager.TeamAttackingDirection attackingDirection = MatchManager.Instance.teamInAttack == MatchManager.TeamInAttack.Home
            ? MatchManager.Instance.homeTeamDirection
            : MatchManager.Instance.awayTeamDirection;

        return (attackingDirection == MatchManager.TeamAttackingDirection.LeftToRight && originHex.coordinates.x > 0)
            || (attackingDirection == MatchManager.TeamAttackingDirection.RightToLeft && originHex.coordinates.x < 0);
    }

    public bool IsFreeKickShotAvailableFromBall()
    {
        return IsValidShotOriginForAttackingDirection(ball?.GetCurrentHex());
    }

    private void LogExpectedGoalForCurrentShot(string outcomeContext)
    {
        if (expectedGoalLogged || shooter == null || isHeaderAtGoal)
        {
            return;
        }

        int shootingPenalty = CalculateShootingPenalty(GetShotOriginHex());
        List<ExpectedStatsCalculator.ShotBlockerExpectation> blockExpectations = expectedGoalOutfieldBlockInteractions
            .Where(interaction => interaction?.defender != null)
            .Select(interaction => new ExpectedStatsCalculator.ShotBlockerExpectation(
                interaction.defender,
                interaction.requiredNaturalRoll))
            .ToList();

        PlayerToken savingGk = expectedGoalGkSaveInteraction?.defender;
        int savingPenalty = expectedGoalGkSaveInteraction?.gkPenalty ?? 0;
        float xGoals = ExpectedStatsCalculator.CalculateShotGoalProbability(
            shooter,
            shootingPenalty,
            blockExpectations,
            savingGk,
            savingPenalty);

        string context = string.IsNullOrWhiteSpace(outcomeContext)
            ? shotType
            : $"{shotType} {outcomeContext}";
        MatchManager.Instance.gameData.gameLog.LogExpectedGoal(shooter, xGoals, context);
        expectedGoalLogged = true;
    }

    void Update()
    {}

    private void OnEnable()
    {
        GameInputManager.OnClick += OnClickReceived;
        GameInputManager.OnHover += OnHoverReceived;
        GameInputManager.OnKeyPress += OnKeyReceived;
    }

    private void OnDisable()
    {
        GameInputManager.OnClick -= OnClickReceived;
        GameInputManager.OnHover -= OnHoverReceived;
        GameInputManager.OnKeyPress -= OnKeyReceived;
        ClearShotCommitPreview();
        ClearShotTargetSelectionHighlights();
    }

    private GKPushManager EnsureGKPushManager()
    {
        if (gkPushManager == null)
        {
            gkPushManager = FindAnyObjectByType<GKPushManager>();
        }

        if (gkPushManager == null)
        {
            gkPushManager = gameObject.AddComponent<GKPushManager>();
        }

        gkPushManager.Configure(hexGrid, ball);
        return gkPushManager;
    }

    private IEnumerator MoveGoalkeeperToSaveHex(PlayerToken gkToken, float? synchronizedDuration = null)
    {
        GKPushManager manager = EnsureGKPushManager();
        if (manager == null)
        {
            Debug.LogError("ShotManager could not resolve a GKPushManager.");
            yield break;
        }

        yield return StartCoroutine(manager.ResolveGKPush(gkToken, saveHex, synchronizedDuration));
    }

    private IEnumerator MoveBallAndGoalkeeperToSaveHex(PlayerToken gkToken)
    {
        int movementRoll = GetShotMovementRoll();
        float ballTravelDuration = ball != null ? ball.CalculateMoveDuration(saveHex, movementRoll) : 0f;
        RecordSnapshotBallStartedIfNeeded();
        Coroutine ballMovement = StartCoroutine(groundBallManager.HandleGroundBallMovement(saveHex, movementRoll, allowGKBoxMove: false));
        Coroutine goalkeeperMovement = StartCoroutine(MoveGoalkeeperToSaveHex(gkToken, ballTravelDuration));
        yield return ballMovement;
        yield return goalkeeperMovement;
    }

    private IEnumerator MovePenaltyGoalBallAndGoalkeeper(PlayerToken gkToken, int gkRoll)
    {
        HexCell goalkeeperDiveHex = GetPenaltyGoalkeeperDiveHex(gkToken, gkRoll);
        RecordSnapshotBallStartedIfNeeded();
        Coroutine ballMovement = StartCoroutine(groundBallManager.HandleGroundBallMovement(targetHex, GetShotMovementRoll(), allowGKBoxMove: false));
        Coroutine goalkeeperMovement = null;
        if (goalkeeperDiveHex != null && gkToken != null && gkToken.GetCurrentHex() != goalkeeperDiveHex)
        {
            goalkeeperMovement = StartCoroutine(movementPhaseManager.MoveTokenToHex(
                targetHex: goalkeeperDiveHex,
                token: gkToken,
                isCalledDuringMovement: false,
                shouldCountForDistance: false,
                shouldCarryBall: false));
        }

        yield return ballMovement;
        if (goalkeeperMovement != null)
        {
            yield return goalkeeperMovement;
        }
    }

    private HexCell GetPenaltyGoalkeeperDiveHex(PlayerToken gkToken, int gkRoll)
    {
        if (targetHex == null)
        {
            Debug.LogError("Cannot resolve penalty goalkeeper dive hex because targetHex is null.");
            return null;
        }

        int side = 0;
        HexCell gkHex = gkToken?.GetCurrentHex();
        if (gkHex != null)
        {
            side = Math.Sign(gkHex.coordinates.x);
        }

        if (side == 0)
        {
            side = Math.Sign(targetHex.coordinates.x);
        }

        if (side == 0)
        {
            Debug.LogError("Cannot resolve penalty goalkeeper dive side.");
            return null;
        }

        int diveDepth = gkRoll <= 2 ? 1 : gkRoll <= 4 ? 2 : 3;
        int diveZ = targetHex.coordinates.z >= 0 ? -diveDepth : diveDepth;
        HexCell diveHex = hexGrid.GetHexCellAt(new Vector3Int(18 * side, 0, diveZ));
        if (diveHex == null)
        {
            Debug.LogError($"Penalty goalkeeper dive hex ({18 * side}, 0, {diveZ}) could not be found.");
        }

        return diveHex;
    }

    private void OnClickReceived(PlayerToken token, HexCell hex)
    {
        if (!isActivated) return;
        _ = HandleClicksForSnapMovement(token, hex);
    }

    private void OnHoverReceived(PlayerToken token, HexCell hex)
    {
        if (isActivated && isWaitingforBlockerMovement && MatchManager.Instance != null && MatchManager.Instance.difficulty_level < 3)
        {
            HexCell nextHoveredBlockerHex = hexGrid.highlightedHexes.Contains(hex) ? hex : null;
            if (hoveredSnapshotBlockerMovementHex != nextHoveredBlockerHex)
            {
                hoveredSnapshotBlockerMovementHex = nextHoveredBlockerHex;
                RefreshSnapshotBlockerMovementHighlights();
            }

            return;
        }

        if (hoveredSnapshotBlockerMovementHex != null)
        {
            hoveredSnapshotBlockerMovementHex = null;
            RefreshSnapshotBlockerMovementHighlights();
        }

        if (isActivated && isWaitingForTargetSelection && MatchManager.Instance != null && MatchManager.Instance.difficulty_level < 3)
        {
            HexCell nextHoveredTarget = shotTargetSelectionTargets.Contains(hex) ? hex : null;
            if (hoveredTargetSelectionPreviewTarget == nextHoveredTarget)
            {
                return;
            }

            hoveredTargetSelectionPreviewTarget = nextHoveredTarget;
            if (MatchManager.Instance.difficulty_level == 1)
            {
                RefreshTargetSelectionPreviewPath();
            }
            else
            {
                RefreshTargetSelectionHoverOnly();
            }
            return;
        }

        if (hoveredTargetSelectionPreviewTarget != null || targetSelectionPreviewPath.Count > 0 || targetSelectionPreviewSaveHex != null)
        {
            hoveredTargetSelectionPreviewTarget = null;
            ClearTargetSelectionPreviewPath();
        }

        if (!CanShowShotCommitPreview())
        {
            if (hoveredShotCommitPreviewTarget != null || shotCommitPreviewPath.Count > 0)
            {
                hoveredShotCommitPreviewTarget = null;
                ClearShotCommitPreviewPath();
            }

            return;
        }

        HexCell nextHoveredCommitTarget = shotCommitPreviewTargets.Contains(hex) ? hex : null;
        if (hoveredShotCommitPreviewTarget == nextHoveredCommitTarget)
        {
            return;
        }

        hoveredShotCommitPreviewTarget = nextHoveredCommitTarget;
        RefreshShotCommitPreviewPath();
    }

    private bool CanShowShotCommitPreview()
    {
        if (!isWaitingForShotCommitConfirmation
            || MatchManager.Instance == null
            || MatchManager.Instance.difficulty_level != 1)
        {
            return false;
        }

        bool isFreeKickExecutionPreview = MatchManager.Instance.currentState == MatchManager.GameState.FreeKickExecution;
        return isAvailable || isFreeKickExecutionPreview;
    }

    private void OnKeyReceived(KeyPressData keyData)
    {
        if (keyData.isConsumed) return;
        if (MatchManager.Instance != null && MatchManager.Instance.IsWaitingForExtraActionsRoll) return;
        if (finalThirdManager != null && finalThirdManager.isActivated) return;
        if (!isActivated
            && MatchManager.Instance != null
            && MatchManager.Instance.currentState == MatchManager.GameState.FreeKickExecution)
        {
            return;
        }
        if (keyData.key == KeyCode.S && IsSnapshotSuppressedByFinalExtraMovement())
        {
            keyData.isConsumed = true;
            isWaitingForSnapshotDecisionFromLoose = false;
            isWaitingForShotCommitConfirmation = false;
            ClearShotCommitPreview();
            Debug.Log("Shot input ignored because snapshots are not available during the final allowed stoppage movement phase.");
            return;
        }
        if (isAvailable && isWaitingForSnapshotDecisionFromLoose)
        {
            if (keyData.key == KeyCode.S)
            {
                isAvailable = false;
                isActivated = true;
                isWaitingForSnapshotDecisionFromLoose = false;
                isWaitingForShotCommitConfirmation = false;
                Debug.Log($"{MatchManager.Instance.LastTokenToTouchTheBallOnPurpose.name} decides to Snapshot!!!!");
                StartShotProcess(MatchManager.Instance.LastTokenToTouchTheBallOnPurpose, "snapshot");
                keyData.isConsumed = true;
                return;
            }
            if (keyData.key == KeyCode.X)
            {
                isAvailable = false;
                isActivated = false;
                isWaitingForSnapshotDecisionFromLoose = false;
                isWaitingForShotCommitConfirmation = false;
                if (movementPhaseManager.isActivated)
                {
                    Debug.Log($"Ball found itself (during MP) on {MatchManager.Instance.LastTokenToTouchTheBallOnPurpose.playerName} who decided to not take the Snapshot");
                    movementPhaseManager.AdvanceMovementPhase();
                }
                else
                {
                    // TODO: this should be "Any other Scenario"
                    Debug.LogWarning($"Ball found itself (NOT during MP) on {MatchManager.Instance.LastTokenToTouchTheBallOnPurpose.playerName} who decided to not take the Snapshot");
                }
                keyData.isConsumed = true;
                return;
            }
        }
        if (isAvailable && isWaitingForShotCommitConfirmation)
        {
            if (keyData.key == KeyCode.S)
            {
                keyData.isConsumed = true;
                isWaitingForShotCommitConfirmation = false;
                ClearShotCommitPreview();
                IdentifyShotType();
                return;
            }

            isWaitingForShotCommitConfirmation = false;
            ClearShotCommitPreview();
        }
        if (isAvailable && keyData.key == KeyCode.S)
        {
            keyData.isConsumed = true; // Consume the key event
            if (!CanStartShotFromCurrentPossession(MatchManager.Instance.LastTokenToTouchTheBallOnPurpose))
            {
                Debug.LogWarning("Shot input ignored because the ball is not held by the attacking team in a valid shooting position.");
                isAvailable = false;
                isWaitingForSnapshotDecisionFromLoose = false;
                isWaitingForShotCommitConfirmation = false;
                ClearShotCommitPreview();
                return;
            }

            if (MatchManager.Instance != null && !movementPhaseManager.isCommitted)
            {
                MatchManager.Instance.ClearNonShotActionPreviews();
            }

            if (ShouldRequireShotCommitConfirmation())
            {
                isWaitingForShotCommitConfirmation = true;
                ShowShotCommitPreviewTargets();
                Debug.Log("Shot selected. Press [S] again to commit.");
                return;
            }

            IdentifyShotType();
            return;
        }
        bool hasRollOverride = RollInputOverride.TryParse(keyData, out RollInputOverride rollOverride);
        if (isActivated && isWaitingForBlockDiceRoll && (keyData.key == KeyCode.R || hasRollOverride))
        {
          keyData.isConsumed = true; // Consume the key event
          StartCoroutine(StartShotBlockRoll(hasRollOverride ? (RollInputOverride?)rollOverride : null));  // Pass the stored list
          return;
        }
        else if (isActivated && !isHeaderAtGoal && isWaitingForGKDiceRoll && (keyData.key == KeyCode.R || hasRollOverride))
        {
          keyData.isConsumed = true; // Consume the key event
          if (interceptors.Count == 0 || !interceptors[0].IsGK)
          {
              Debug.LogWarning("GK roll requested, but the current shot interaction is not a GK save.");
              return;
          }
          StartCoroutine(ResolveGKSavingAttempt(interceptors[0], hasRollOverride ? (RollInputOverride?)rollOverride : null));
          return;
        }
        else if (isActivated && isHeaderAtGoal && isWaitingForGKDiceRoll && (keyData.key == KeyCode.R || hasRollOverride))
        {
            keyData.isConsumed = true;
            isWaitingForGKDiceRoll = false;
            PerformGKHeaderSave(hasRollOverride ? (RollInputOverride?)rollOverride : null);
        }
        else if (isActivated && isWaitingForShotRoll && (keyData.key == KeyCode.R || hasRollOverride))
        {
          keyData.isConsumed = true; // Consume the key event
          StartCoroutine(StartShotRoll(hasRollOverride ? (RollInputOverride?)rollOverride : null));
          return;
        }
        else if (isActivated && isWaitingforHandlingTest && (keyData.key == KeyCode.R || hasRollOverride))
        {
          keyData.isConsumed = true; // Consume the key event
          StartCoroutine(ResolveHandlingTest(hasRollOverride ? (RollInputOverride?)rollOverride : null));
          return;
        }
        else if (isActivated && isWaitingforBlockerSelection && keyData.key == KeyCode.X)
        {
          keyData.isConsumed = true; // Consume the key event
          StartDefenderMovementPhase();
          return;
        }
        else if (isActivated && isWaitingForTargetSelection && keyData.key == KeyCode.X)
        {
          keyData.isConsumed = true; // Consume the key event
          CompleteDefenderMovement();
          return;
        }
        else if (isWaitingForSaveandHoldScenario)
        {
          if (keyData.key == KeyCode.Q)
          {
            keyData.isConsumed = true;
            QuickThrow();
          }
          if (keyData.key == KeyCode.K)
          {
            keyData.isConsumed = true;
            ActivateFinalThirds();
          }
        }
    }

    private void IdentifyShotType()
    {
        if (MatchManager.Instance.currentState == MatchManager.GameState.EndOfMovementPhase)
        {
            StartShotProcess(MatchManager.Instance.LastTokenToTouchTheBallOnPurpose, "fullPower");
        }
        else 
        {
            StartShotProcess(MatchManager.Instance.LastTokenToTouchTheBallOnPurpose, "snapshot");
        }
    }

    public void EnterSaveAndHoldDecision()
    {
        isAvailable = false;
        isActivated = false;
        isWaitingforBlockerSelection = false;
        isWaitingForBlockDiceRoll = false;
        isWaitingForShotRoll = false;
        isWaitingForGKDiceRoll = false;
        isWaitingforHandlingTest = false;
        isWaitingForShotCommitConfirmation = false;
        isWaitingforBlockerMovement = false;
        isWaitingForTargetSelection = false;
        isWaitingForSnapshotDecisionFromLoose = false;
        isHeaderAtGoal = false;
        shooter = null;
        totalShotPower = 0;
        shooterRoll = 0;
        shooterRollWasJackpot = false;
        boxPenalty = null;
        snapPenalty = null;
        difficultPenalty = null;
        shootingPenaltyInfo = null;
        tokenMoveforDeflection = null;
        targetHex = null;
        saveHex = null;
        currentDefenderBlockingHex = null;
        trajectoryPath = null;
        interceptors.Clear();
        currentShotInteraction = null;
        alreadyInterceptedDefs?.Clear();
        headerAttackerTotalScore = 0;
        headerAttacker = null;
        headerTargetHex = null;
        headerGkPenalty = 0;
        ResetExpectedGoalContext();
        ClearShotCommitPreview();
        ClearShotTargetSelectionHighlights();
        isWaitingForSaveandHoldScenario = true;
    }

    private bool CanStartShotFromCurrentPossession(PlayerToken shootingToken)
    {
        if (shootingToken == null || MatchManager.Instance == null || ball == null)
        {
            return false;
        }

        HexCell ballHex = ball.GetCurrentHex();
        HexCell shooterHex = shootingToken.GetCurrentHex();
        if (ballHex == null || shooterHex == null || ballHex != shooterHex)
        {
            return false;
        }

        return MatchManager.Instance.attackHasPossession
            && ballHex.isAttackOccupied
            && shootingToken.isAttacker
            && IsValidShotOriginForAttackingDirection(shooterHex);
    }

    public void CommitToThisAction(MatchManager.MatchActionKind actionKind = MatchManager.MatchActionKind.Shot)
    {
        committedShotActionKind = actionKind;
        shotActionResolutionPending = actionKind == MatchManager.MatchActionKind.Shot;
        shotActionResolvedRecorded = false;
        deferShotActionResolutionUntilExternalRestart = false;
        snapshotActionRecorded = false;
        MatchManager.Instance.CommitToAction(actionKind);
    }

    private bool IsSnapshotSuppressedByFinalExtraMovement()
    {
        return MatchManager.Instance != null
            && MatchManager.Instance.IsFinalExtraMovementPhaseSnapshotSuppressed();
    }

    private void RecordSnapshotBallStartedIfNeeded()
    {
        if (snapshotActionRecorded || shotType != "snapshot")
        {
            return;
        }

        snapshotActionRecorded = true;
        MatchManager.Instance?.RecordSnapshotBallStarted();
    }

    private void RecordShotActionResolvedIfNeeded()
    {
        if (shotActionResolvedRecorded || !shotActionResolutionPending)
        {
            return;
        }

        if (deferShotActionResolutionUntilExternalRestart)
        {
            return;
        }

        CompleteShotActionResolution(null);
    }

    private bool CompleteShotActionResolution(Action continuation)
    {
        if (shotActionResolvedRecorded || !shotActionResolutionPending)
        {
            continuation?.Invoke();
            return false;
        }

        shotActionResolvedRecorded = true;
        shotActionResolutionPending = false;
        MatchManager.Instance?.ResolveActionBeforeFinalThird(MatchManager.MatchActionKind.Shot, continuation);
        return MatchManager.Instance != null && MatchManager.Instance.IsWaitingForExtraActionsRoll;
    }

    private void DeferShotActionResolutionUntilExternalRestart()
    {
        if (shotActionResolvedRecorded || !shotActionResolutionPending)
        {
            return;
        }

        deferShotActionResolutionUntilExternalRestart = true;
    }

    public bool HasDeferredShotActionResolution => deferShotActionResolutionUntilExternalRestart;

    public bool CompleteDeferredShotActionResolution(Action continuation = null)
    {
        if (!deferShotActionResolutionUntilExternalRestart)
        {
            return false;
        }

        deferShotActionResolutionUntilExternalRestart = false;
        Action resolvedContinuation = continuation;
        if (resolvedContinuation == null && MatchManager.Instance != null)
        {
            resolvedContinuation = MatchManager.Instance.RefreshAvailableActionsForCurrentState;
        }

        bool waitingForExtraRoll = CompleteShotActionResolution(resolvedContinuation);
        if (!waitingForExtraRoll)
        {
            RecordSnapshotEndedMovementPhaseIfNeeded();
        }

        committedShotActionKind = MatchManager.MatchActionKind.None;
        shotActionResolvedRecorded = false;
        return waitingForExtraRoll;
    }

    private void EndMovementPhaseForShotResolutionIfNeeded(bool clearStunnedTokens)
    {
        if (movementPhaseManager == null || !movementPhaseManager.isActivated)
        {
            return;
        }

        bool isSnapshot = shotType == "snapshot";
        bool consumesExtraAction = movementPhaseManager.committedMovementConsumesExtraAction;
        movementPhaseManager.EndMovementPhase(false);
        if (clearStunnedTokens)
        {
            movementPhaseManager.stunnedTokens.Clear();
        }

        if (isSnapshot)
        {
            pendingSnapshotEndedMovementPhase = true;
            pendingSnapshotEndedMovementConsumesExtraAction = consumesExtraAction;
        }
    }

    private void RecordSnapshotEndedMovementPhaseIfNeeded()
    {
        if (!pendingSnapshotEndedMovementPhase)
        {
            return;
        }

        if (deferShotActionResolutionUntilExternalRestart)
        {
            return;
        }

        bool consumesExtraAction = pendingSnapshotEndedMovementConsumesExtraAction;
        pendingSnapshotEndedMovementPhase = false;
        pendingSnapshotEndedMovementConsumesExtraAction = false;
        MatchManager.Instance?.ResolveActionBeforeFinalThird(
            MatchManager.MatchActionKind.MovementPhase,
            null,
            consumesExtraAction);
    }

    private bool ShouldRequireShotCommitConfirmation()
    {
        return MatchManager.Instance.difficulty_level < 3
            && !isActivated
            && !isWaitingForSnapshotDecisionFromLoose
            && !movementPhaseManager.isActivated
            && !movementPhaseManager.isWaitingForSnapshotDecision
            && movementPhaseManager.isAvailable;
    }

    private void ShowShotCommitPreviewTargets()
    {
        ClearShotCommitPreview();
        if (MatchManager.Instance.difficulty_level != 1)
        {
            return;
        }

        HexCell originHex = GetShotCommitPreviewOriginHex();
        if (originHex == null || originHex.ShootingPaths == null)
        {
            return;
        }

        foreach (HexCell canShootToHex in originHex.ShootingPaths.Keys)
        {
            canShootToHex.HighlightHex("CanShootFrom", 1);
            if (!hexGrid.highlightedHexes.Contains(canShootToHex))
            {
                hexGrid.highlightedHexes.Add(canShootToHex);
            }
            if (!shotCommitPreviewTargets.Contains(canShootToHex))
            {
                shotCommitPreviewTargets.Add(canShootToHex);
            }
            if (canShootToHex.transform.position.y == 0)
            {
                canShootToHex.transform.position += Vector3.up * 0.03f;
            }
        }
    }

    public void PreviewShotCommit()
    {
        shotCommitPreviewOriginHex = null;
        isWaitingForShotCommitConfirmation = true;
        ShowShotCommitPreviewTargets();
    }

    public void PreviewFreeKickShotCommit()
    {
        shotCommitPreviewOriginHex = ball?.GetCurrentHex();
        isWaitingForShotCommitConfirmation = true;
        ShowShotCommitPreviewTargets();
    }

    public void CancelShotCommitPreview()
    {
        isWaitingForShotCommitConfirmation = false;
        ClearShotCommitPreview();
        shotCommitPreviewOriginHex = null;
    }

    public void StartFreeKickShotProcess(PlayerToken shootingToken)
    {
        ClearShotCommitPreview();
        ClearTargetSelectionPreviewPath();
        hexGrid.ClearHighlightedHexes();
        isWaitingForShotCommitConfirmation = false;
        if (shootingToken == null)
        {
            Debug.LogError("Free Kick shooting token is NULL! Cannot proceed with shot.");
            return;
        }

        HexCell shooterHex = shootingToken.GetCurrentHex();
        if (shooterHex == null)
        {
            Debug.LogError($"Free Kick shooting token {shootingToken.name} is not on any hex! Cannot proceed with shot.");
            return;
        }

        HexCell originHex = ball?.GetCurrentHex();
        if (!IsValidShotOriginForAttackingDirection(originHex))
        {
            string originName = originHex != null ? originHex.coordinates.ToString() : "unknown";
            Debug.LogError($"Free Kick ball hex {originName} is not a valid shooting origin!");
            return;
        }

        CommitToThisAction(MatchManager.MatchActionKind.Shot);
        MatchManager.Instance.ClearLastTokenChain();
        MatchManager.Instance.SetLastToken(shootingToken);
        shooter = shootingToken;
        freeKickShotOriginHex = originHex;
        shotType = FreeKickShotType;
        isActivated = true;
        ResetExpectedGoalContext();
        MatchManager.Instance.gameData.gameLog.LogEvent(shooter, MatchManager.ActionType.ShotAttempt, shotType: FreeKickShotType);
        Debug.Log("Free Kick Shot initiated. Proceeding to target selection.");
        HandleTargetSelection();
    }

    public void StartPenaltyShotProcess(PlayerToken shootingToken, HexCell penaltySpot)
    {
        ClearShotCommitPreview();
        ClearTargetSelectionPreviewPath();
        hexGrid.ClearHighlightedHexes();
        isWaitingForShotCommitConfirmation = false;
        if (shootingToken == null)
        {
            Debug.LogError("Penalty shooting token is NULL! Cannot proceed with shot.");
            return;
        }

        if (penaltySpot == null)
        {
            Debug.LogError("Penalty spot is NULL! Cannot proceed with shot.");
            return;
        }

        if (shootingToken.GetCurrentHex() != penaltySpot || ball.GetCurrentHex() != penaltySpot)
        {
            Debug.LogError($"Penalty shot cannot start because {shootingToken.name} and the ball are not both on {penaltySpot.coordinates}.");
            return;
        }

        if (!IsValidShotOriginForAttackingDirection(penaltySpot))
        {
            Debug.LogError($"Penalty spot {penaltySpot.coordinates} is not a valid shooting origin.");
            return;
        }

        CommitToThisAction();
        MatchManager.Instance.ClearLastTokenChain();
        MatchManager.Instance.SetLastToken(shootingToken);
        shooter = shootingToken;
        penaltyShotOriginHex = penaltySpot;
        shotType = PenaltyShotType;
        isActivated = true;
        ResetExpectedGoalContext();
        MatchManager.Instance.gameData.gameLog.LogEvent(shooter, MatchManager.ActionType.ShotAttempt, shotType: PenaltyShotType);
        Debug.Log("Penalty Kick initiated. Proceeding to target selection.");
        HandleTargetSelection();
    }

    private void RefreshShotCommitPreviewPath()
    {
        ClearShotCommitPreviewPath();
        if (hoveredShotCommitPreviewTarget == null)
        {
            return;
        }

        HexCell originHex = GetShotCommitPreviewOriginHex();
        if (originHex == null
            || originHex.ShootingPaths == null
            || !originHex.ShootingPaths.TryGetValue(hoveredShotCommitPreviewTarget, out List<HexCell> previewPath))
        {
            return;
        }

        foreach (HexCell pathHex in previewPath)
        {
            if (pathHex == null || pathHex == hoveredShotCommitPreviewTarget)
            {
                continue;
            }

            pathHex.HighlightHex("ballPath");
            if (!hexGrid.highlightedHexes.Contains(pathHex))
            {
                hexGrid.highlightedHexes.Add(pathHex);
            }
            shotCommitPreviewPath.Add(pathHex);
        }

        hoveredShotCommitPreviewTarget.HighlightHex("CanShootFrom", 1);
    }

    private void ClearShotCommitPreviewPath()
    {
        foreach (HexCell pathHex in shotCommitPreviewPath)
        {
            if (pathHex == null) continue;
            pathHex.ResetHighlight();
            hexGrid.highlightedHexes.Remove(pathHex);
        }
        shotCommitPreviewPath.Clear();

        foreach (HexCell targetHex in shotCommitPreviewTargets)
        {
            if (targetHex == null) continue;
            targetHex.HighlightHex("CanShootFrom", 1);
            if (!hexGrid.highlightedHexes.Contains(targetHex))
            {
                hexGrid.highlightedHexes.Add(targetHex);
            }
        }
    }

    private void ClearShotCommitPreview()
    {
        ClearShotCommitPreviewPath();
        foreach (HexCell targetHex in shotCommitPreviewTargets)
        {
            if (targetHex == null) continue;
            targetHex.ResetHighlight();
            hexGrid.highlightedHexes.Remove(targetHex);
            if (targetHex.transform.position.y > 0.001f)
            {
                targetHex.transform.position -= Vector3.up * 0.03f;
            }
        }

        shotCommitPreviewTargets.Clear();
        hoveredShotCommitPreviewTarget = null;
    }

    private void RefreshTargetSelectionPreviewPath()
    {
        ClearTargetSelectionPreviewPath();
        if (hoveredTargetSelectionPreviewTarget == null || shooter == null)
        {
            return;
        }

        HexCell originHex = GetShotOriginHex();
        if (originHex == null
            || originHex.ShootingPaths == null
            || !originHex.ShootingPaths.TryGetValue(hoveredTargetSelectionPreviewTarget, out List<HexCell> previewPath))
        {
            return;
        }

        var (outfieldBlockAttempts, previewSaveHex) = GetShotPreviewInterceptionInfo(previewPath);
        bool hasDefensiveShotInteraction = outfieldBlockAttempts > 0 || previewSaveHex != null;
        string pathHighlight = hasDefensiveShotInteraction ? "dangerousPass" : "ballPath";
        foreach (HexCell pathHex in previewPath)
        {
            if (pathHex == null || pathHex == hoveredTargetSelectionPreviewTarget)
            {
                continue;
            }

            pathHex.HighlightHex(pathHighlight);
            if (!hexGrid.highlightedHexes.Contains(pathHex))
            {
                hexGrid.highlightedHexes.Add(pathHex);
            }
            targetSelectionPreviewPath.Add(pathHex);
        }

        if (previewSaveHex != null)
        {
            if (!hexGrid.highlightedHexes.Contains(previewSaveHex))
            {
                hexGrid.highlightedHexes.Add(previewSaveHex);
            }
            if (!targetSelectionPreviewPath.Contains(previewSaveHex))
            {
                targetSelectionPreviewPath.Add(previewSaveHex);
            }
            targetSelectionPreviewSaveHex = previewSaveHex;
        }

        hoveredTargetSelectionPreviewTarget.HighlightHex("ShotTargetHover");
    }

    private void RefreshTargetSelectionHoverOnly()
    {
        ClearTargetSelectionPreviewPath();
        if (hoveredTargetSelectionPreviewTarget == null)
        {
            return;
        }

        hoveredTargetSelectionPreviewTarget.HighlightHex("ShotTargetHover");
    }

    private (int outfieldBlockAttempts, HexCell previewSaveHex) GetShotPreviewInterceptionInfo(List<HexCell> path)
    {
        HexCell previewSaveHex = null;
        PlayerToken defendingGK = hexGrid.GetDefendingGK();
        HexCell gkHex = defendingGK?.GetCurrentHex();

        if (defendingGK != null && gkHex != null && !HasAlreadyInteracted(defendingGK))
        {
            previewSaveHex = GetClosestGKSaveHexOnPath(path, gkHex);
        }

        HashSet<PlayerToken> previewDefenders = new();
        foreach (HexCell pathHex in path)
        {
            if (pathHex == null || pathHex == gkHex) continue;

            if (pathHex.isDefenseOccupied)
            {
                PlayerToken defenderOnPath = pathHex.GetOccupyingToken();
                if (defenderOnPath != null && !HasAlreadyInteracted(defenderOnPath))
                {
                    previewDefenders.Add(defenderOnPath);
                }
            }
        }

        List<HexCell> defenderHexes = hexGrid.GetDefenderHexes();
        defenderHexes.Remove(gkHex);
        List<HexCell> defenderNeighbors = hexGrid.GetDefenderNeighbors(defenderHexes) ?? new List<HexCell>();
        foreach (HexCell pathHex in path)
        {
            if (pathHex == null || pathHex.isAttackOccupied || !defenderNeighbors.Contains(pathHex)) continue;

            foreach (HexCell neighbor in pathHex.GetNeighbors(hexGrid))
            {
                if (neighbor == null || neighbor.isAttackOccupied || !neighbor.isDefenseOccupied) continue;

                PlayerToken defenderInZOI = neighbor.GetOccupyingToken();
                if (defenderInZOI != null && defenderInZOI != defendingGK && !HasAlreadyInteracted(defenderInZOI))
                {
                    previewDefenders.Add(defenderInZOI);
                }
            }
        }

        return (previewDefenders.Count, previewSaveHex);
    }

    private string BuildShotTargetSelectionPreviewInstruction()
    {
        if (MatchManager.Instance == null || MatchManager.Instance.difficulty_level != 1)
        {
            return string.Empty;
        }

        if (hoveredTargetSelectionPreviewTarget == null)
        {
            return "Hover a target to preview blockers and the goalkeeper save. ";
        }

        HexCell originHex = GetShotOriginHex();
        if (originHex == null
            || originHex.ShootingPaths == null
            || !originHex.ShootingPaths.TryGetValue(hoveredTargetSelectionPreviewTarget, out List<HexCell> previewPath))
        {
            return string.Empty;
        }

        List<ShotInteraction> previewInteractions = BuildShotTargetPreviewInteractions(previewPath);
        List<ShotInteraction> outfieldBlocks = previewInteractions
            .Where(interaction => interaction != null && !interaction.IsGK)
            .ToList();
        ShotInteraction gkSave = previewInteractions.FirstOrDefault(interaction => interaction != null && interaction.IsGK);

        if (outfieldBlocks.Count == 0 && gkSave == null)
        {
            return "No defender block or goalkeeper save is currently previewed. ";
        }

        List<string> parts = new();
        if (outfieldBlocks.Count > 0)
        {
            string blockers = string.Join(", ", outfieldBlocks.Select(BuildPreviewBlockerInstruction));
            parts.Add($"Blockers: {blockers}");
        }

        if (gkSave != null)
        {
            parts.Add(BuildPreviewGkSaveInstruction(gkSave));
        }

        return $"{string.Join(". ", parts)}. ";
    }

    private List<ShotInteraction> BuildShotTargetPreviewInteractions(List<HexCell> path)
    {
        List<ShotInteraction> previewInteractions = new();
        if (path == null || path.Count == 0)
        {
            return previewInteractions;
        }

        PlayerToken defendingGK = hexGrid.GetDefendingGK();
        foreach (PlayerToken defender in hexGrid.GetDefenders())
        {
            if (defender == null
                || defender == defendingGK
                || defender.IsGoalKeeper
                || HasAlreadyInteracted(defender))
            {
                continue;
            }

            ShotInteraction interaction = BuildPreviewOutfieldBlockInteraction(defender, path);
            if (interaction != null)
            {
                previewInteractions.Add(interaction);
            }
        }

        if (defendingGK != null && !HasAlreadyInteracted(defendingGK))
        {
            HexCell gkHex = defendingGK.GetCurrentHex();
            HexCell candidateSaveHex = GetClosestGKSaveHexOnPath(path, gkHex);
            if (candidateSaveHex != null)
            {
                int saveDistance = HexGridUtils.GetHexStepDistance(gkHex, candidateSaveHex);
                int pathIndex = path.IndexOf(candidateSaveHex);
                previewInteractions.Add(new ShotInteraction
                {
                    defender = defendingGK,
                    interactionHex = candidateSaveHex,
                    type = ShotInteractionType.GKSave,
                    pathIndex = pathIndex >= 0 ? pathIndex : int.MaxValue,
                    requiredNaturalRoll = 0,
                    gkPenalty = CalculateGKSavePenalty(defendingGK, saveDistance)
                });
            }
        }

        return SortShotInteractions(previewInteractions);
    }

    private ShotInteraction BuildPreviewOutfieldBlockInteraction(PlayerToken defender, List<HexCell> path)
    {
        HexCell defenderHex = defender.GetCurrentHex();
        if (defenderHex == null)
        {
            return null;
        }

        for (int i = 0; i < path.Count; i++)
        {
            HexCell pathHex = path[i];
            if (pathHex != null && pathHex.GetOccupyingToken() == defender)
            {
                return new ShotInteraction
                {
                    defender = defender,
                    interactionHex = pathHex,
                    type = ShotInteractionType.OutfieldBlock,
                    pathIndex = i,
                    requiredNaturalRoll = 5,
                    gkPenalty = null
                };
            }
        }

        for (int i = 0; i < path.Count; i++)
        {
            HexCell pathHex = path[i];
            if (pathHex == null || pathHex.isAttackOccupied)
            {
                continue;
            }

            if (pathHex.GetNeighbors(hexGrid).Contains(defenderHex))
            {
                return new ShotInteraction
                {
                    defender = defender,
                    interactionHex = pathHex,
                    type = ShotInteractionType.OutfieldBlock,
                    pathIndex = i,
                    requiredNaturalRoll = 6,
                    gkPenalty = null
                };
            }
        }

        return null;
    }

    private string BuildPreviewBlockerInstruction(ShotInteraction interaction)
    {
        string blockType = interaction.requiredNaturalRoll == 5 ? "on-path" : "ZOI";
        int neededRoll = GetOutfieldBlockRequiredRoll(interaction);
        return $"{GetTokenInstructionName(interaction.defender)} ({blockType}, {BuildNeededRollInstruction(neededRoll, "")})";
    }

    private string BuildPreviewGkSaveInstruction(ShotInteraction gkSave)
    {
        string saveHexInfo = gkSave.interactionHex != null ? $" at {HexGridUtils.FormatHexCoordinates(gkSave.interactionHex)}" : string.Empty;
        return $"GK save: {GetTokenInstructionName(gkSave.defender)}{saveHexInfo} ({BuildSavingAttributeInstruction(gkSave.defender, gkSave.gkPenalty ?? 0)})";
    }

    private void ClearTargetSelectionPreviewPath()
    {
        foreach (HexCell pathHex in targetSelectionPreviewPath)
        {
            if (pathHex == null) continue;
            pathHex.ResetHighlight();
            hexGrid.highlightedHexes.Remove(pathHex);
        }
        targetSelectionPreviewPath.Clear();
        targetSelectionPreviewSaveHex = null;

        foreach (HexCell targetHex in shotTargetSelectionTargets)
        {
            if (targetHex == null) continue;
            targetHex.HighlightHex("CanShootFrom", 1);
            if (!hexGrid.highlightedHexes.Contains(targetHex))
            {
                hexGrid.highlightedHexes.Add(targetHex);
            }
        }
    }

    private void ClearShotTargetSelectionHighlights()
    {
        ClearTargetSelectionPreviewPath();
        foreach (HexCell targetHex in shotTargetSelectionTargets)
        {
            if (targetHex == null) continue;
            targetHex.ResetHighlight();
            hexGrid.highlightedHexes.Remove(targetHex);
            if (targetHex.transform.position.y > 0.001f)
            {
                targetHex.transform.position -= Vector3.up * 0.03f;
            }
        }

        shotTargetSelectionTargets.Clear();
        hoveredTargetSelectionPreviewTarget = null;
    }
    
    public void StartShotProcess(PlayerToken shootingToken, string shotType)
    {
        ClearShotCommitPreview();
        ClearTargetSelectionPreviewPath();
        hexGrid.ClearHighlightedHexes();
        isWaitingForShotCommitConfirmation = false;
        if (shootingToken == null)
        {
            Debug.LogError("Shooting token is NULL! Cannot proceed with shot.");
            return;
        }

        HexCell shooterHex = shootingToken.GetCurrentHex();
        if (shooterHex == null)
        {
            Debug.LogError($"Shooting token {shootingToken.name} is not on any hex! Cannot proceed with shot.");
            return;
        }
        if (!shooterHex.CanShootFrom)
        {
            Debug.LogError($"Token {shootingToken.name} is on hex {shooterHex.coordinates}, but this hex is not a valid shooting hex!");
            return;
        }
        if (!CanStartShotFromCurrentPossession(shootingToken))
        {
            Debug.LogWarning($"Shot cancelled for {shootingToken.name}: current possession, ball hex, or attacking direction does not allow a shot from {shooterHex.coordinates}.");
            isActivated = false;
            isAvailable = false;
            isWaitingForSnapshotDecisionFromLoose = false;
            isWaitingForShotCommitConfirmation = false;
            return;
        }

        CommitToThisAction(shotType == "snapshot" ? MatchManager.MatchActionKind.None : MatchManager.MatchActionKind.Shot);
        shooter = shootingToken;
        this.shotType = shotType;
        isActivated = true;
        ResetExpectedGoalContext();

        if (shotType == "snapshot")
        {
            Debug.Log("Snapshot initiated. Allow one defender to move 2 hexes.");
            if (shooterHex.isInPenaltyBox == 0) MatchManager.Instance.gameData.gameLog.LogEvent(shooter, MatchManager.ActionType.ShotAttempt, shotType: "snapO");
            else MatchManager.Instance.gameData.gameLog.LogEvent(shooter, MatchManager.ActionType.ShotAttempt, shotType: "snap");
            StartDefenderMovementPhase();
        }
        else
        {
            Debug.Log("Full Power Shot initiated. Proceeding to target selection.");
            if (shooterHex.isInPenaltyBox == 0)
            {
                Debug.Log("Shooter is OUTSIDE the penalty box. -1 to shot power.");
                MatchManager.Instance.gameData.gameLog.LogEvent(shooter, MatchManager.ActionType.ShotAttempt, shotType: "shot");
                Debug.Log("Goalkeeper can move 1 hex");
                // TODO: Implement Goalkeeper movement for the Box in case of a Shot from outside the box
            }
            else
            {
                MatchManager.Instance.gameData.gameLog.LogEvent(shooter, MatchManager.ActionType.ShotAttempt, shotType: "shotO");
            }
            HandleTargetSelection();
        }
    }

    public async void ProcessHeaderAtGoal(PlayerToken attacker, int attackerTotalScore, HexCell headerTargetHex)
    {
        isActivated = true;
        isHeaderAtGoal = true;
        shooter = attacker;
        totalShotPower = attackerTotalScore;
        targetHex = headerTargetHex;
        shotType = "header";
        this.headerAttacker = attacker;
        this.headerAttackerTotalScore = attackerTotalScore;
        this.headerTargetHex = headerTargetHex;

        // Find the defending GK and their hex
        PlayerToken defendingGK = hexGrid.GetDefendingGK();
        HexCell gkHex = defendingGK.GetCurrentHex();

        // Find the saveHex closest to the GK, preferring the GK's own hex when it is on the path.
        // TODO: Retrieve the path from the JSON
        List<HexCell> path = ball.GetCurrentHex().HeadingPaths[headerTargetHex];
        saveHex = GetClosestGKSaveHexOnPath(path, gkHex);

        if (saveHex == null)
        {
            // No save possible, goal!
            Debug.Log("No saveHex found, header at goal is a GOAL!");
            MatchManager.Instance.gameData.gameLog.LogEvent(attacker, MatchManager.ActionType.GoalScored);
            // Animate ball to target, then trigger goal flow
            await helperFunctions.StartCoroutineAndWait(groundBallManager.HandleGroundBallMovement(headerTargetHex, allowGKBoxMove: false));
            goalFlowManager.StartGoalFlow(attacker);
            ResetShotProcess();
            return;
        }

        int saveDistance = HexGridUtils.GetHexStepDistance(gkHex, saveHex);
        headerGkPenalty = 0;
        if (saveDistance == 3) headerGkPenalty = -1;

        // GK can attempt a save
        Debug.Log($"GK {defendingGK.name} can attempt a save at {saveHex.coordinates} with penalty {headerGkPenalty}. Header power is {headerAttackerTotalScore}. Waiting for GK dice roll...");
        // StartCoroutine(PerformGKHeaderSave(defendingGK, headerGkPenalty));
        // Debug.Log($"Press [R] to roll for GK save attempt (header at goal).");
        isWaitingForGKDiceRoll = true;
    }

    public void PerformGKHeaderSave(int? rigRoll = null)
    {
        RollInputOverride? rollOverride = rigRoll.HasValue
            ? new RollInputOverride
            {
                hasOverride = true,
                roll = rigRoll.Value,
                isJackpot = false
            }
            : null;
        PerformGKHeaderSave(rollOverride);
    }

    public async void PerformGKHeaderSave(RollInputOverride? rollOverride)
    {
        // Roll for GK
        PlayerToken gkToken = hexGrid.GetDefendingGK();
        isWaitingForGKDiceRoll = false;
        var (returnedRoll, returnedJackpot) = helperFunctions.DiceRoll();
        bool isJackpot = IsJackpotRoll(rollOverride, returnedJackpot);
        int gkRoll = GetRollValueWithJackpot(rollOverride, returnedRoll);
        int totalSavingPower = isJackpot ? 50 : gkRoll + gkToken.saving + headerGkPenalty;
        if (totalSavingPower == 50) Debug.Log($"GK {gkToken.name} rolls A JACKPOT!!!");
        else Debug.Log($"GK {gkToken.name} rolls {gkRoll} + Saving: {gkToken.saving} + Penalty: {headerGkPenalty} = {totalSavingPower}");
        MatchManager.Instance.gameData.gameLog.LogEvent(
            gkToken,
            MatchManager.ActionType.SaveAttempt
        );

        // Compare with attacker's total
        if (totalSavingPower == headerAttackerTotalScore)
        {
            Debug.Log($"{gkToken.name} ties the attacker's header! Loose Ball situation initiated.");
            MatchManager.Instance.SetLastToken(headerAttacker);
            MatchManager.Instance.gameData.gameLog.LogEvent(
                headerAttacker,
                MatchManager.ActionType.ShotBlocked,
                connectedToken: gkToken
            );
            MatchManager.Instance.gameData.gameLog.LogEvent(
                gkToken
                , MatchManager.ActionType.SaveMade
                , saveType: "loose"
            );
            MatchManager.Instance.SetHangingPass("shot");
            await helperFunctions.StartCoroutineAndWait(groundBallManager.HandleGroundBallMovement(saveHex, allowGKBoxMove: false));
            await helperFunctions.StartCoroutineAndWait(MoveGoalkeeperToSaveHex(gkToken));
            StartCoroutine(looseBallManager.ResolveLooseBall(gkToken, LooseBallSourceType.GroundDeflection, allowGKBoxMove: false));
            ResetShotProcess();
        }
        else if (totalSavingPower > headerAttackerTotalScore)
        {
            Debug.Log($"{gkToken.name} saves the header! Will they hold the ball? Needs to roll lower than {gkToken.handling} to hold the ball. Press [R] to roll for Handling Test!");
            await helperFunctions.StartCoroutineAndWait(groundBallManager.HandleGroundBallMovement(saveHex, allowGKBoxMove: false));
            await helperFunctions.StartCoroutineAndWait(MoveGoalkeeperToSaveHex(gkToken));
            // MatchManager.Instance.ChangePossession();
            isWaitingforHandlingTest = true;
        }
        else // attacker wins
        {
            Debug.Log($"{headerAttacker.name} wins the header at goal! GOAL!");
            await helperFunctions.StartCoroutineAndWait(groundBallManager.HandleGroundBallMovement(headerTargetHex, headerAttackerTotalScore/2, allowGKBoxMove: false));
            MatchManager.Instance.gameData.gameLog.LogEvent(
                        headerAttacker
                        , MatchManager.ActionType.GoalScored
                    );
            goalFlowManager.StartGoalFlow(headerAttacker);
            ResetShotProcess();
        }
    }

    private async Task HandleClicksForSnapMovement(PlayerToken token, HexCell hex)
    {
        if (token != null && isWaitingforBlockerSelection)
        {
            Debug.Log($"PlayerToken {token.name} clicked, for Snapshot");

            // Attacker Phase: Ensure the token is an attacker
            if (!token.isAttacker)
            {
                SelectSnapshotBlocker(token);
            }
            else {
                Debug.LogWarning("Attacker clicked, while waiting for a defender to select.");
            }

            return;
        }

        if (CanSwitchSnapshotBlocker(token))
        {
            SelectSnapshotBlocker(token);
            return;
        }

        if (hex != null && isWaitingforBlockerMovement)
        {
            Debug.Log($"Hex clicked: {hex.name}");

            if (IsValidSnapshotBlockerDestination(hex))
            {
                if (tokenMoveforDeflection != null)
                {
                    Debug.Log($"Moving {tokenMoveforDeflection.name} to hex {hex.coordinates}");

                    // Move the selected token to the valid hex (use the highPassManager's selectedToken)
                    await helperFunctions.StartCoroutineAndWait(movementPhaseManager.MoveTokenToHex(hex, tokenMoveforDeflection, false));  // Pass the selected token
                    CompleteDefenderMovement();
                }
                else
                {
                    Debug.LogWarning("No token selected to move.");
                }
            }
            else
            {
                Debug.LogWarning("Clicked hex is not a valid movement target.");
                if (token == null)
                {
                    ClearSnapshotBlockerSelection();
                }
            }
        }
        else if (
            hex != null // we clicked a Hex
            && isWaitingForTargetSelection // We are indeed waiting for a targetSelection
            && shotTargetSelectionTargets.Contains(hex) // one of the target Hexes
        )
        {
            Debug.Log($"Valid selected Target Hex: {hex.name}");
            HandleTargetClick(hex);                    
        }
        else
        {
            Debug.LogWarning("No valid hex or token clicked.");
        }
    }

    private bool CanSwitchSnapshotBlocker(PlayerToken token)
    {
        return shotType == "snapshot"
            && MatchManager.Instance != null
            && MatchManager.Instance.difficulty_level < 3
            && isWaitingforBlockerMovement
            && token != null
            && !token.isAttacker
            && token != tokenMoveforDeflection;
    }

    private void SelectSnapshotBlocker(PlayerToken token)
    {
        if (tokenMoveforDeflection != null && tokenMoveforDeflection != token)
        {
            Debug.Log($"Switching Defender selection to {token.name}. Clearing previous highlights.");
            hexGrid.ClearHighlightedHexes();
        }

        Debug.Log($"Selecting defender {token.name}. Highlighting reachable hexes.");
        tokenMoveforDeflection = token;
        hoveredSnapshotBlockerMovementHex = null;
        movementPhaseManager.HighlightValidMovementHexes(token, 2, false);
        isWaitingforBlockerSelection = false;
        isWaitingforBlockerMovement = true;
    }

    private void RefreshSnapshotBlockerMovementHighlights()
    {
        if (tokenMoveforDeflection == null)
        {
            return;
        }

        movementPhaseManager.HighlightValidMovementHexes(tokenMoveforDeflection, 2, false);
        if (hoveredSnapshotBlockerMovementHex != null && hexGrid.highlightedHexes.Contains(hoveredSnapshotBlockerMovementHex))
        {
            hoveredSnapshotBlockerMovementHex.HighlightHex("MovementDestinationHover");
        }
    }

    private void ClearSnapshotBlockerSelection()
    {
        if (tokenMoveforDeflection != null)
        {
            Debug.Log($"Clearing selected snapshot blocker {tokenMoveforDeflection.name} after invalid destination click.");
        }

        tokenMoveforDeflection = null;
        hoveredSnapshotBlockerMovementHex = null;
        hexGrid.ClearHighlightedHexes();
        isWaitingforBlockerMovement = false;
        isWaitingforBlockerSelection = true;
    }

    private void StartDefenderMovementPhase()
    {
        isWaitingforBlockerSelection = true;
    }

    private bool IsValidSnapshotBlockerDestination(HexCell hex)
    {
        if (hex == null || tokenMoveforDeflection == null)
        {
            return false;
        }

        if (hex.isAttackOccupied || hex.isDefenseOccupied || hex.isOutOfBounds)
        {
            return false;
        }

        HexCell defenderHex = tokenMoveforDeflection.GetCurrentHex();
        if (defenderHex == null)
        {
            return false;
        }

        var (reachableHexes, _) = HexGridUtils.GetReachableHexes(hexGrid, defenderHex, 2);
        if (!reachableHexes.Contains(hex))
        {
            Debug.LogWarning($"{hex.name} is not within 2 hexes of selected snapshot blocker {tokenMoveforDeflection.name}.");
            return false;
        }

        return true;
    }

    public void CompleteDefenderMovement()
    {
        Debug.Log("Defender movement phase complete. Proceeding to target selection.");
        hoveredSnapshotBlockerMovementHex = null;
        isWaitingforBlockerMovement = false;
        HandleTargetSelection();
    }

    public void HandleTargetSelection()
    {
        Debug.Log("Highlighting CanShootTo hexes for target selection.");
        ClearShotTargetSelectionHighlights();
        HexCell originHex = GetShotOriginHex();
        if (originHex == null || originHex.ShootingPaths == null)
        {
            Debug.LogError("Cannot select a shot target because the shot origin has no shooting paths.");
            ResetShotProcess();
            return;
        }

        Dictionary<HexCell, List<HexCell>> shootingPaths = originHex.ShootingPaths;

        // Highlight and raise all CanShootTo hexes
        foreach (var canShootToHex in shootingPaths.Keys)
        {
            canShootToHex.HighlightHex("CanShootFrom", 1);
            if (!hexGrid.highlightedHexes.Contains(canShootToHex))
            {
                hexGrid.highlightedHexes.Add(canShootToHex);
            }
            if (!shotTargetSelectionTargets.Contains(canShootToHex))
            {
                shotTargetSelectionTargets.Add(canShootToHex);
            }
            if (canShootToHex.transform.position.y == 0)
            {
                canShootToHex.transform.position += Vector3.up * 0.03f; // Raise it above the plane
            }
        }

        Debug.Log("Waiting for target selection...");
        isWaitingForTargetSelection = true;
    }

    public void HandleTargetClick(HexCell clickedTargethex)
    {
        ClearTargetSelectionPreviewPath();
        targetHex = clickedTargethex;
        Debug.Log($"Target hex {targetHex.coordinates} selected. Preparing trajectory.");
        HexCell originHex = GetShotOriginHex();
        if (originHex == null || originHex.ShootingPaths == null)
        {
            Debug.LogError("Cannot resolve shot target because the shot origin has no shooting paths.");
            ResetShotProcess();
            return;
        }

        Dictionary<HexCell, List<HexCell>> shootingPaths = originHex.ShootingPaths;
        foreach (var canShootToHex in shootingPaths.Keys)
        {
            canShootToHex.ResetHighlight();
            if (canShootToHex.transform.position.y >= 0)
            {
              canShootToHex.transform.position -= Vector3.up * 0.03f; // Sink it below the plane
            }
        }
        trajectoryPath = originHex.ShootingPaths[targetHex];
        if (MatchManager.Instance == null || MatchManager.Instance.difficulty_level != 2)
        {
            HighlightTrajectoryPath();
        }
        isWaitingForTargetSelection = false;
        shotTargetSelectionTargets.Clear();
        if (IsFreeKickShot() || IsPenaltyShot())
        {
            StartCoroutine(StartFreeKickShooterRollPhase());
        }
        else
        {
            StartCoroutine(StartInterceptionPhase());
        }
    }

    private void HighlightTrajectoryPath()
    {
        foreach (HexCell hex in trajectoryPath)
        {
            hex.HighlightHex("ballPath");
            hexGrid.highlightedHexes.Add(hex);
        }
    }

    private IEnumerator StartInterceptionPhase()
    {
        Debug.Log("Starting interception phase.");
        alreadyInterceptedDefs ??= new List<PlayerToken>();
        interceptors = GatherInterceptors(trajectoryPath);
        CaptureExpectedGoalInteractions(interceptors);
        Debug.Log($"Shot interactions found: {interceptors.Count}");
        StartCoroutine(OfferBlockRoll());
        yield return null;

    }

    private IEnumerator StartFreeKickShooterRollPhase()
    {
        Debug.Log(IsPenaltyShot()
            ? "Penalty Kick target selected. Shooter must roll before the GK save attempt."
            : "Free Kick Shot target selected. Shooter must roll before block attempts.");
        alreadyInterceptedDefs ??= new List<PlayerToken>();
        alreadyInterceptedDefs.Clear();
        interceptors.Clear();
        currentShotInteraction = null;
        currentDefenderBlockingHex = null;
        saveHex = null;
        CaptureExpectedGoalInteractions(interceptors);
        isWaitingForShotRoll = true;
        yield return null;
    }

    private List<ShotInteraction> GatherInterceptors(List<HexCell> path)
    {
        List<ShotInteraction> shotInteractions = new();
        if (path == null || path.Count == 0)
        {
            return shotInteractions;
        }

        PlayerToken defendingGK = hexGrid.GetDefendingGK();
        foreach (PlayerToken defender in hexGrid.GetDefenders())
        {
            if (defender == null
                || defender == defendingGK
                || defender.IsGoalKeeper
                || HasAlreadyInteracted(defender))
            {
                continue;
            }

            ShotInteraction outfieldInteraction = BuildOutfieldBlockInteraction(defender, path);
            if (outfieldInteraction != null)
            {
                shotInteractions.Add(outfieldInteraction);
            }
        }

        ShotInteraction gkInteraction = BuildGKSaveInteraction(path);
        if (gkInteraction != null)
        {
            shotInteractions.Add(gkInteraction);
        }

        return SortShotInteractions(shotInteractions);
    }

    private List<ShotInteraction> GatherOutfieldBlockInteractions(List<HexCell> path)
    {
        List<ShotInteraction> shotInteractions = new();
        if (path == null || path.Count == 0)
        {
            return shotInteractions;
        }

        PlayerToken defendingGK = hexGrid.GetDefendingGK();
        foreach (PlayerToken defender in hexGrid.GetDefenders())
        {
            if (defender == null
                || defender == defendingGK
                || defender.IsGoalKeeper
                || HasAlreadyInteracted(defender))
            {
                continue;
            }

            ShotInteraction outfieldInteraction = BuildOutfieldBlockInteraction(defender, path);
            if (outfieldInteraction != null)
            {
                shotInteractions.Add(outfieldInteraction);
            }
        }

        return SortShotInteractions(shotInteractions);
    }

    private ShotInteraction BuildOutfieldBlockInteraction(PlayerToken defender, List<HexCell> path)
    {
        HexCell defenderHex = defender.GetCurrentHex();
        if (defenderHex == null)
        {
            return null;
        }

        for (int i = 0; i < path.Count; i++)
        {
            HexCell pathHex = path[i];
            if (pathHex != null && pathHex.GetOccupyingToken() == defender)
            {
                Debug.Log($"Path blocked by defender {defender.name} at shot hex {pathHex.coordinates}.");
                return new ShotInteraction
                {
                    defender = defender,
                    interactionHex = pathHex,
                    type = ShotInteractionType.OutfieldBlock,
                    pathIndex = i,
                    requiredNaturalRoll = 5,
                    gkPenalty = null
                };
            }
        }

        for (int i = 0; i < path.Count; i++)
        {
            HexCell pathHex = path[i];
            if (pathHex == null || pathHex.isAttackOccupied)
            {
                continue;
            }

            if (pathHex.GetNeighbors(hexGrid).Contains(defenderHex))
            {
                Debug.Log($"Defender {defender.name} can block through ZOI at shot hex {pathHex.coordinates}.");
                return new ShotInteraction
                {
                    defender = defender,
                    interactionHex = pathHex,
                    type = ShotInteractionType.OutfieldBlock,
                    pathIndex = i,
                    requiredNaturalRoll = 6,
                    gkPenalty = null
                };
            }
        }

        return null;
    }

    private ShotInteraction BuildGKSaveInteraction(List<HexCell> path)
    {
        PlayerToken defendingGK = hexGrid.GetDefendingGK();
        HexCell gkHex = defendingGK?.GetCurrentHex();
        if (defendingGK == null || gkHex == null || HasAlreadyInteracted(defendingGK))
        {
            return null;
        }

        HexCell candidateSaveHex = GetClosestGKSaveHexOnPath(path, gkHex);
        if (candidateSaveHex == null)
        {
            return null;
        }

        int saveDistance = HexGridUtils.GetHexStepDistance(gkHex, candidateSaveHex);
        int gkPenalty = CalculateGKSavePenalty(defendingGK, saveDistance);
        int pathIndex = path.IndexOf(candidateSaveHex);

        Debug.Log($"Goalkeeper {defendingGK.name} can attempt a save at {candidateSaveHex.coordinates} with penalty {gkPenalty}");
        return new ShotInteraction
        {
            defender = defendingGK,
            interactionHex = candidateSaveHex,
            type = ShotInteractionType.GKSave,
            pathIndex = pathIndex >= 0 ? pathIndex : int.MaxValue,
            requiredNaturalRoll = 0,
            gkPenalty = gkPenalty
        };
    }

    private int CalculateGKSavePenalty(PlayerToken defendingGK, int saveDistance)
    {
        if (defendingGK == null)
        {
            return 0;
        }

        if (IsPenaltyShot())
        {
            return -2;
        }

        bool gkWasSnapshotDefenderMove = shotType == "snapshot" && tokenMoveforDeflection == defendingGK;
        if (gkWasSnapshotDefenderMove)
        {
            if (saveDistance == 2) return -1;
            if (saveDistance == 3) return -2;
            return 0;
        }

        return saveDistance == 3 ? -1 : 0;
    }

    private HexCell GetClosestGKSaveHexOnPath(List<HexCell> path, HexCell gkHex)
    {
        if (path == null || gkHex == null)
        {
            return null;
        }

        PlayerToken defendingGK = hexGrid.GetDefendingGK();
        if (goalKeeperManager != null && defendingGK != null)
        {
            return goalKeeperManager.FindClosestGoalkeeperWallHexOnPath(path, defendingGK, includeGoalkeeperHex: true);
        }

        List<HexCell> saveableHexes = hexGrid.GetSavableHexes();
        return path
            .Select((hex, index) => new { hex, index })
            .Where(entry => entry.hex != null && saveableHexes.Contains(entry.hex))
            .OrderBy(entry => HexGridUtils.GetHexStepDistance(gkHex, entry.hex))
            .ThenBy(entry => entry.hex == gkHex ? 0 : 1)
            .ThenBy(entry => entry.index)
            .Select(entry => entry.hex)
            .FirstOrDefault();
    }

    private List<ShotInteraction> SortShotInteractions(List<ShotInteraction> interactions)
    {
        HexCell originHex = GetShotOriginHex();
        return interactions
            .Where(interaction => interaction?.defender != null && interaction.interactionHex != null)
            .OrderBy(interaction => originHex != null
                ? HexGridUtils.GetHexStepDistance(originHex, interaction.interactionHex)
                : interaction.pathIndex)
            .ThenBy(interaction => interaction.IsGK ? 1 : 0)
            .ThenBy(interaction => interaction.IsGK ? int.MaxValue : interaction.defender.tackling)
            .ThenBy(interaction => GetTokenSortName(interaction.defender), StringComparer.Ordinal)
            .ThenBy(interaction => interaction.pathIndex)
            .ToList();
    }

    private string GetTokenSortName(PlayerToken token)
    {
        if (token == null)
        {
            return string.Empty;
        }

        return !string.IsNullOrWhiteSpace(token.playerName) ? token.playerName : token.name;
    }

    private bool HasAlreadyInteracted(PlayerToken token)
    {
        return token != null && alreadyInterceptedDefs != null && alreadyInterceptedDefs.Contains(token);
    }

    private IEnumerator OfferBlockRoll()
    {
        if (ShouldOfferGKBoxMoveBeforeNextInteraction())
        {
            if (IsFreeKickShot())
            {
                yield return StartCoroutine(OfferFreeKickGKBoxMoveOnly());
            }
            else
            {
                yield return StartCoroutine(OfferGKBoxMoveAndRefreshGKInteraction());
            }
        }

        if (interceptors.Count == 0)
        {
            Debug.Log($"No more defenders to Deflect! The {shooter.name} may [R]oll! Good Luck!.");
            currentShotInteraction = null;
            currentDefenderBlockingHex = null;
            isWaitingForShotRoll = true;
            yield break;
        }

        currentShotInteraction = interceptors[0];
        currentDefenderBlockingHex = currentShotInteraction.interactionHex;

        if (currentShotInteraction.IsGK)
        {
            Debug.Log("The GK is next up. Shooter must Roll to shoot. Setting isWaitingForShotRoll to true.");
            saveHex = currentShotInteraction.interactionHex;
            isWaitingForShotRoll = true;
        }
        else
        {
            int tacklingRollNeeded = 10 - currentShotInteraction.defender.tackling;
            int finalRollNeeded = Math.Min(tacklingRollNeeded, currentShotInteraction.requiredNaturalRoll);
            string blockType = currentShotInteraction.requiredNaturalRoll == 5 ? "on-path" : "ZOI";
            Debug.Log($"{currentShotInteraction.defender.name} attempts an {blockType} shot block at {currentShotInteraction.interactionHex.coordinates}, needs a {finalRollNeeded}+ to deflect. [R]oll!");
            isWaitingForBlockDiceRoll = true;
        }
    }

    private bool ShouldOfferGKBoxMoveBeforeNextInteraction()
    {
        if (gkWasOfferedMoveForBox || shooter == null || trajectoryPath == null || trajectoryPath.Count == 0)
        {
            return false;
        }

        HexCell originHex = GetShotOriginHex();
        if (originHex == null || originHex.isInPenaltyBox != 0)
        {
            return false;
        }

        int firstPenaltyBoxPathIndex = GetFirstDefendingPenaltyBoxPathIndex();
        if (firstPenaltyBoxPathIndex < 0)
        {
            return false;
        }

        return !interceptors.Any(interaction => interaction != null && interaction.pathIndex < firstPenaltyBoxPathIndex);
    }

    private int GetFirstDefendingPenaltyBoxPathIndex()
    {
        int targetPenaltyBox = targetHex != null && targetHex.isInGoal != 0
            ? targetHex.isInGoal
            : Math.Sign(targetHex != null ? targetHex.coordinates.x : 0);

        for (int i = 0; i < trajectoryPath.Count; i++)
        {
            HexCell pathHex = trajectoryPath[i];
            if (pathHex == null || pathHex.isInPenaltyBox == 0)
            {
                continue;
            }

            if (targetPenaltyBox == 0 || pathHex.isInPenaltyBox == targetPenaltyBox)
            {
                return i;
            }
        }

        return -1;
    }

    private IEnumerator OfferGKBoxMoveAndRefreshGKInteraction()
    {
        int firstPenaltyBoxPathIndex = GetFirstDefendingPenaltyBoxPathIndex();
        HexCell firstPenaltyBoxHex = firstPenaltyBoxPathIndex >= 0 ? trajectoryPath[firstPenaltyBoxPathIndex] : null;
        Debug.Log($"Shot has logically entered the penalty box at {firstPenaltyBoxHex?.coordinates}. GK gets the ball-in-box free move.");

        gkWasOfferedMoveForBox = true;
        goalKeeperManager.isActivated = true;
        yield return StartCoroutine(goalKeeperManager.HandleGKFreeMove());
        goalKeeperManager.isActivated = false;

        RefreshGKInteractionOnly();
    }

    private IEnumerator OfferFreeKickGKBoxMoveOnly()
    {
        int firstPenaltyBoxPathIndex = GetFirstDefendingPenaltyBoxPathIndex();
        HexCell firstPenaltyBoxHex = firstPenaltyBoxPathIndex >= 0 ? trajectoryPath[firstPenaltyBoxPathIndex] : null;
        Debug.Log($"Free Kick Shot has logically entered the penalty box at {firstPenaltyBoxHex?.coordinates}. GK gets the ball-in-box free move before remaining outfield blocks.");

        gkWasOfferedMoveForBox = true;
        goalKeeperManager.isActivated = true;
        yield return StartCoroutine(goalKeeperManager.HandleGKFreeMove());
        goalKeeperManager.isActivated = false;
    }

    private IEnumerator StartFreeKickGKPhase()
    {
        currentShotInteraction = null;
        currentDefenderBlockingHex = null;
        interceptors.Clear();

        if (ShouldOfferGKBoxMoveBeforeNextInteraction())
        {
            yield return StartCoroutine(OfferGKBoxMoveAndRefreshGKInteraction());
        }
        else
        {
            RefreshGKInteractionOnly();
        }

        ShotInteraction gkInteraction = interceptors.FirstOrDefault(interaction => interaction != null && interaction.IsGK);
        if (gkInteraction == null)
        {
            yield return StartCoroutine(ResolveShotGoal());
            yield break;
        }

        currentShotInteraction = gkInteraction;
        currentDefenderBlockingHex = gkInteraction.interactionHex;
        saveHex = gkInteraction.interactionHex;
        Debug.Log($"Free Kick Shot has reached GK {gkInteraction.defender.name}. Press [R] to roll for the save.");
        isWaitingForGKDiceRoll = true;
    }

    private IEnumerator StartPenaltyGKPhase()
    {
        currentShotInteraction = null;
        currentDefenderBlockingHex = null;
        interceptors.Clear();
        RefreshGKInteractionOnly();

        ShotInteraction gkInteraction = interceptors.FirstOrDefault(interaction => interaction != null && interaction.IsGK);
        if (gkInteraction == null)
        {
            yield return StartCoroutine(ResolveShotGoal());
            yield break;
        }

        currentShotInteraction = gkInteraction;
        currentDefenderBlockingHex = gkInteraction.interactionHex;
        saveHex = gkInteraction.interactionHex;
        Debug.Log($"Penalty Kick has reached GK {gkInteraction.defender.name}. Press [R] to roll for the save.");
        isWaitingForGKDiceRoll = true;
    }

    private void RefreshGKInteractionOnly()
    {
        interceptors.RemoveAll(interaction => interaction != null && interaction.IsGK);

        ShotInteraction updatedGKInteraction = BuildGKSaveInteraction(trajectoryPath);
        if (updatedGKInteraction != null)
        {
            interceptors.Add(updatedGKInteraction);
            saveHex = updatedGKInteraction.interactionHex;
            UpdateExpectedGoalGkInteraction(updatedGKInteraction);
        }
        else
        {
            saveHex = null;
            UpdateExpectedGoalGkInteraction(null);
            Debug.Log("GK has no save interaction after the ball-in-box free move.");
        }

        interceptors = SortShotInteractions(interceptors);
    }

    private IEnumerator StartShotBlockRoll(int? rigRoll = null)
    {
        RollInputOverride? rollOverride = rigRoll.HasValue
            ? new RollInputOverride
            {
                hasOverride = true,
                roll = rigRoll.Value,
                isJackpot = false
            }
            : null;
        yield return StartCoroutine(StartShotBlockRoll(rollOverride));
    }

    private IEnumerator StartShotBlockRoll(RollInputOverride? rollOverride)
    {
        yield return null; // Wait for next frame
        RecordSnapshotBallStartedIfNeeded();
        ShotInteraction currentDefenderEntry = currentShotInteraction;
        if (currentDefenderEntry != null && currentDefenderBlockingHex != null)
        {
            if (currentDefenderEntry.defender != null)
            {
                // Retrieve defender attributes
                PlayerToken defenderToken = currentDefenderEntry.defender;
                int tackling = defenderToken.tackling;
                string defenderName = defenderToken.name;

                if (currentDefenderEntry.IsGK)
                {
                    Debug.Log($"GK {defenderName} is attempting a save at {currentDefenderBlockingHex.coordinates}.");
                    Debug.Log("Shooter must roll first! Press [R] to roll for the shot.");
                    isWaitingForBlockDiceRoll = false;
                    isWaitingForShotRoll = true;  // Wait for the shooter to roll
                    yield break;  // Exit the coroutine here to wait for the shot roll
                }

                // Roll the dice
                var (returnedRoll, returnedJackpot) = helperFunctions.DiceRoll();
                bool isJackpot = IsJackpotRoll(rollOverride, returnedJackpot);
                int diceRoll = GetRollValueWithoutJackpot(rollOverride, returnedRoll);
                
                if (isJackpot) Debug.Log($"{defenderName} rolls A JACKPOT for the block at {currentDefenderBlockingHex.coordinates}!");
                else Debug.Log($"Dice roll by {defenderName} at {currentDefenderBlockingHex.coordinates}: {diceRoll}");
                MatchManager.Instance.gameData.gameLog.LogEvent(defenderToken, MatchManager.ActionType.ShotBlockAttempt);
                isWaitingForBlockDiceRoll = false;
                // Calculate interception conditions
                int requiredRoll = currentDefenderEntry.requiredNaturalRoll;
                bool successfulInterception = isJackpot || diceRoll >= requiredRoll || diceRoll + tackling >= 10;
                if (successfulInterception)
                {
                    hexGrid.ClearHighlightedHexes();
                    Debug.Log($"Shot blocked by {defenderName}! Loose Ball from {currentDefenderBlockingHex.coordinates}!");
                    LogExpectedGoalForCurrentShot("blocked");
                    MatchManager.Instance.gameData.gameLog.LogEvent(
                        MatchManager.Instance.LastTokenToTouchTheBallOnPurpose
                        , MatchManager.ActionType.ShotBlocked
                        , connectedToken: defenderToken
                    );
                    MatchManager.Instance.gameData.gameLog.LogEvent(
                        defenderToken
                        , MatchManager.ActionType.ShotBlockMade
                        , connectedToken: MatchManager.Instance.LastTokenToTouchTheBallOnPurpose
                    );
                    MatchManager.Instance.SetHangingPass("shot"); // TODO: WHY?
                    DeferShotActionResolutionUntilExternalRestart();
                    yield return StartCoroutine(looseBallManager.ResolveLooseBall(defenderToken, LooseBallSourceType.GroundDeflection, allowGKBoxMove: false));
                    ResetShotProcess();
                }
                else
                {
                    Debug.Log($"{defenderName} at {currentDefenderBlockingHex.coordinates} failed to block.");
                    // Remove this defender and move to the next
                    interceptors.Remove(currentDefenderEntry);
                    alreadyInterceptedDefs.Add(currentDefenderEntry.defender);
                    currentShotInteraction = null;
                    currentDefenderBlockingHex = null;

                    if (IsFreeKickShot() && interceptors.Count == 0)
                    {
                        if (shooterRoll == 1)
                        {
                            yield return StartCoroutine(ResolveShotOffTarget());
                        }
                        else
                        {
                            yield return StartCoroutine(StartFreeKickGKPhase());
                        }
                    }
                    else if (interceptors.Count > 0 || totalShotPower == 0)
                    {
                        // There may be more defenders, a GK box-entry checkpoint, or the final shooter roll.
                        StartCoroutine(OfferBlockRoll());
                    }
                    else
                    {
                        if (shooterRoll == 1)
                        {
                            yield return StartCoroutine(ResolveShotOffTarget());
                        }
                        else
                        {
                            Debug.Log($"{shooter.name} Shot roll: {shooterRoll}, that's a GOAL!!");
                            RecordSnapshotBallStartedIfNeeded();
                            LogExpectedGoalForCurrentShot("goal");
                            LogGoalScoredForCurrentShot(MatchManager.Instance.LastTokenToTouchTheBallOnPurpose);
                            DeferShotActionResolutionUntilExternalRestart();
                            yield return StartCoroutine(groundBallManager.HandleGroundBallMovement(targetHex, GetShotMovementRoll(), allowGKBoxMove: false));
                            EndMovementPhaseForShotResolutionIfNeeded(clearStunnedTokens: true);
                            goalFlowManager.StartGoalFlow(shooter);
                            ResetShotProcess();
                        }
                    }
                }
            }
        }
        yield return null;
    }

    public IEnumerator StartShotRoll(int? rigRoll = null)
    {
        RollInputOverride? rollOverride = rigRoll.HasValue
            ? new RollInputOverride
            {
                hasOverride = true,
                roll = rigRoll.Value,
                isJackpot = false
            }
            : null;
        yield return StartCoroutine(StartShotRoll(rollOverride));
    }

    public IEnumerator StartShotRoll(RollInputOverride? rollOverride)
    {
        Debug.Log("Hello from the StartShotRoll");
        
        var (returnedRoll, returnedJackpot) = helperFunctions.DiceRoll();
        bool isJackpot = IsJackpotRoll(rollOverride, returnedJackpot);
        shooterRollWasJackpot = isJackpot;
        shooterRoll = GetRollValueWithJackpot(rollOverride, returnedRoll);
        // shooterRoll = 2;
        isWaitingForShotRoll = false;
        HexCell originHex = GetShotOriginHex();
        int shootingPenalty = CalculateShootingPenalty(originHex);
        boxPenalty = originHex != null && originHex.isInPenaltyBox == 0 ? ", -1 outside the Penalty Box" : "";
        snapPenalty = shotType == "snapshot" ? ", -1 for taking a Snapshot" : "";
        difficultPenalty = originHex != null && originHex.isDifficultShotPosition ? ", -1 difficult shooting position" : "";
        shootingPenaltyInfo = BuildShootingPenaltyInfo(originHex, shootingPenalty);
        totalShotPower = isJackpot ? 50 : shooterRoll + shooter.shooting - shootingPenalty;

        if (IsFreeKickShot())
        {
            yield return StartCoroutine(ContinueFreeKickShotAfterShooterRoll());
            yield break;
        }

        if (IsPenaltyShot())
        {
            yield return StartCoroutine(ContinuePenaltyShotAfterShooterRoll());
            yield break;
        }

        if (shooterRoll == 1)
        {
            yield return StartCoroutine(ResolveShotOffTarget());
            yield break;
        }

        if (interceptors.Count > 0 && interceptors[0].IsGK) // Check if the GK is next
        {
            Debug.Log($"Goalkeeper {interceptors[0].defender.name} now attempts a save. Press [R] to roll");
            isWaitingForGKDiceRoll = true;
        }
        else // There's NO GOALKEEPER or more defenders! Shooter is attempting to put it on target. 
            {
                Debug.Log($"{shooter.name} Shot roll: {shooterRoll} + Shooting: {shooter.shooting}{shootingPenaltyInfo}= {totalShotPower}");
                Debug.Log($"Get IN!! {shooter.name}, buries it to the top corner! Goal!!!");
                RecordSnapshotBallStartedIfNeeded();
                LogExpectedGoalForCurrentShot("goal");
                LogGoalScoredForCurrentShot(MatchManager.Instance.LastTokenToTouchTheBallOnPurpose);
            DeferShotActionResolutionUntilExternalRestart();
            EndMovementPhaseForShotResolutionIfNeeded(clearStunnedTokens: true);
            RecordSnapshotBallStartedIfNeeded();
            yield return StartCoroutine(groundBallManager.HandleGroundBallMovement(targetHex, GetShotMovementRoll(), allowGKBoxMove: false));
            goalFlowManager.StartGoalFlow(shooter);
            ResetShotProcess();
        } 
    }

    private IEnumerator ContinueFreeKickShotAfterShooterRoll()
    {
        Debug.Log($"{shooter.name} Free Kick Shot roll: {shooterRoll} + Shooting: {shooter.shooting}{shootingPenaltyInfo} = {totalShotPower}");

        if (shooterRoll == 1)
        {
            Debug.Log("Free Kick Shot roll is a natural 1. Outfield blocks may still happen; if none succeeds, the shot is off target.");
            interceptors = GatherOutfieldBlockInteractions(trajectoryPath);
            CaptureExpectedGoalInteractions(interceptors);
            if (interceptors.Count > 0)
            {
                yield return StartCoroutine(OfferBlockRoll());
            }
            else
            {
                yield return StartCoroutine(ResolveShotOffTarget());
            }
            yield break;
        }

        if (totalShotPower >= 9)
        {
            Debug.Log("Free Kick Shot has the perfect height. Outfield defender blocks are skipped.");
            interceptors.Clear();
            CaptureExpectedGoalInteractions(interceptors);
            yield return StartCoroutine(StartFreeKickGKPhase());
            yield break;
        }

        interceptors = GatherOutfieldBlockInteractions(trajectoryPath);
        CaptureExpectedGoalInteractions(interceptors);
        if (interceptors.Count > 0)
        {
            yield return StartCoroutine(OfferBlockRoll());
        }
        else
        {
            yield return StartCoroutine(StartFreeKickGKPhase());
        }
    }

    private IEnumerator ContinuePenaltyShotAfterShooterRoll()
    {
        Debug.Log($"{shooter.name} Penalty Kick roll: {shooterRoll} + Shooting: {shooter.shooting}{shootingPenaltyInfo} = {totalShotPower}");

        if (shooterRoll == 1)
        {
            yield return StartCoroutine(ResolveShotOffTarget());
            yield break;
        }

        yield return StartCoroutine(StartPenaltyGKPhase());
    }

    private int CalculateShootingPenalty(HexCell shooterHex)
    {
        int penalty = 0;
        if (IsPenaltyShot())
        {
            return penalty;
        }

        if (IsFreeKickShot())
        {
            if (shooterHex != null && shooterHex.isInPenaltyBox == 0) penalty++;
            return penalty;
        }

        if (shotType == "snapshot") penalty++;
        if (shooterHex != null && shooterHex.isInPenaltyBox == 0) penalty++;
        if (shooterHex != null && shooterHex.isDifficultShotPosition) penalty++;

        return Mathf.Min(penalty, 2);
    }

    private string BuildShootingPenaltyInfo(HexCell shooterHex, int appliedPenalty)
    {
        if (appliedPenalty <= 0)
        {
            return "";
        }

        if (IsPenaltyShot())
        {
            return "";
        }

        if (IsFreeKickShot())
        {
            return $", shooting penalties: -{appliedPenalty} (outside box)";
        }

        List<string> reasons = new();
        if (shotType == "snapshot") reasons.Add("Snapshot");
        if (shooterHex != null && shooterHex.isInPenaltyBox == 0) reasons.Add("outside box");
        if (shooterHex != null && shooterHex.isDifficultShotPosition) reasons.Add("difficult position");

        string capInfo = reasons.Count > appliedPenalty ? "; capped at -2" : "";
        return $", shooting penalties: -{appliedPenalty} ({string.Join(", ", reasons)}{capInfo})";
    }

    private List<string> GetShootingPenaltyReasons(HexCell shooterHex)
    {
        List<string> reasons = new();
        if (IsPenaltyShot())
        {
            return reasons;
        }

        if (IsFreeKickShot())
        {
            if (shooterHex != null && shooterHex.isInPenaltyBox == 0) reasons.Add("outside the box");
            return reasons;
        }

        if (shotType == "snapshot") reasons.Add("snapshot");
        if (shooterHex != null && shooterHex.isInPenaltyBox == 0) reasons.Add("outside the box");
        if (shooterHex != null && shooterHex.isDifficultShotPosition) reasons.Add("difficult shot position");
        return reasons;
    }

    private string BuildShootingModifierInstruction(HexCell shooterHex)
    {
        int appliedPenalty = CalculateShootingPenalty(shooterHex);
        if (appliedPenalty <= 0)
        {
            return "";
        }

        List<string> reasons = GetShootingPenaltyReasons(shooterHex);
        string reasonInfo = string.Join(" ", reasons.Select(reason => $"-1 for {reason}"));
        string capInfo = reasons.Count > appliedPenalty ? "; capped at -2" : "";
        return $" ({reasonInfo}{capInfo})";
    }

    private string GetTokenInstructionName(PlayerToken token)
    {
        if (token == null)
        {
            return "Unknown";
        }

        string displayName = !string.IsNullOrWhiteSpace(token.playerName) ? token.playerName : token.name;
        return token.jerseyNumber > 0 ? $"{token.jerseyNumber}.{displayName}" : displayName;
    }

    private int GetShotMovementRoll()
    {
        return shooterRollWasJackpot ? 8 : shooterRoll;
    }

    private string BuildShooterInstructionInfo(bool includeRoll)
    {
        if (shooter == null)
        {
            return "Unknown shooter";
        }

        HexCell originHex = GetShotOriginHex();
        string shootingInfo = $"Shooting: {shooter.shooting}{BuildShootingModifierInstruction(originHex)}";
        if (!includeRoll || shooterRoll <= 0)
        {
            return $"{GetTokenInstructionName(shooter)} ({shootingInfo})";
        }

        string rollInfo = shooterRollWasJackpot ? "Jackpot" : shooterRoll.ToString();
        return $"{GetTokenInstructionName(shooter)} ({shootingInfo} + Roll: {rollInfo} = {totalShotPower})";
    }

    private int GetOutfieldBlockRequiredRoll(ShotInteraction interaction)
    {
        if (interaction == null || interaction.defender == null)
        {
            return 6;
        }

        int tacklingRollNeeded = 10 - interaction.defender.tackling;
        return Mathf.Max(1, Math.Min(tacklingRollNeeded, interaction.requiredNaturalRoll));
    }

    private int GetGKSavingRequiredRoll(PlayerToken gkToken, int savingPenalty, int attackPower)
    {
        if (gkToken == null)
        {
            return 7;
        }

        return attackPower + 1 - gkToken.saving - savingPenalty;
    }

    private int GetGKTieRoll(PlayerToken gkToken, int savingPenalty, int attackPower)
    {
        if (gkToken == null)
        {
            return 7;
        }

        return attackPower - gkToken.saving - savingPenalty;
    }

    private string BuildNeededRollInstruction(int neededRoll, string outcome)
    {
        if (neededRoll <= 6)
        {
            return $"A roll of {Mathf.Max(1, neededRoll)}+ is needed{outcome}";
        }

        return $"A Jackpot is needed{outcome}";
    }

    private string BuildGKTieInstruction(PlayerToken gkToken, int savingPenalty, int attackPower)
    {
        int tieRoll = GetGKTieRoll(gkToken, savingPenalty, attackPower);
        return tieRoll >= 1 && tieRoll <= 6 ? $" ({tieRoll} ties)" : "";
    }

    private string BuildBlockRollInstruction()
    {
        ShotInteraction interaction = currentShotInteraction;
        if (interaction == null || interaction.IsGK || interaction.defender == null)
        {
            return "Click R to Roll for the block";
        }

        int neededRoll = GetOutfieldBlockRequiredRoll(interaction);
        return $"Click R to Roll for a block with {GetTokenInstructionName(interaction.defender)} (Tackling: {interaction.defender.tackling}). {BuildNeededRollInstruction(neededRoll, "")}";
    }

    private string BuildShotRollInstruction()
    {
        string shotLabel = shotType == "snapshot"
            ? "Snapshot"
            : IsPenaltyShot()
                ? "Penalty Kick"
            : IsFreeKickShot()
                ? "Free Kick Shot"
                : "Shot";
        return $"Click R to Roll for the {shotLabel} with {BuildShooterInstructionInfo(false)}";
    }

    private string BuildSavingAttributeInstruction(PlayerToken gkToken, int savingPenalty)
    {
        if (gkToken == null)
        {
            return "Saving: ?";
        }

        return savingPenalty == 0
            ? $"Saving: {gkToken.saving}"
            : $"Saving: {gkToken.saving}, save penalty: {savingPenalty}";
    }

    private string BuildGKSaveRollInstruction()
    {
        ShotInteraction gkInteraction = currentShotInteraction != null && currentShotInteraction.IsGK
            ? currentShotInteraction
            : interceptors.FirstOrDefault(interaction => interaction != null && interaction.IsGK);
        PlayerToken gkToken = gkInteraction?.defender ?? hexGrid.GetDefendingGK();
        int savingPenalty = gkInteraction?.gkPenalty ?? 0;
        int neededRoll = GetGKSavingRequiredRoll(gkToken, savingPenalty, totalShotPower);
        string tieInfo = BuildGKTieInstruction(gkToken, savingPenalty, totalShotPower);
        return $"Click R to Roll for a save with {GetTokenInstructionName(gkToken)} ({BuildSavingAttributeInstruction(gkToken, savingPenalty)}). {BuildShooterInstructionInfo(true)}. {BuildNeededRollInstruction(neededRoll, " to save")}{tieInfo}";
    }

    private string BuildHeaderGKSaveRollInstruction()
    {
        PlayerToken defendingGK = hexGrid.GetDefendingGK();
        int neededRoll = GetGKSavingRequiredRoll(defendingGK, headerGkPenalty, headerAttackerTotalScore);
        string tieInfo = BuildGKTieInstruction(defendingGK, headerGkPenalty, headerAttackerTotalScore);
        string headerInfo = headerAttacker != null
            ? $"{GetTokenInstructionName(headerAttacker)} header power: {headerAttackerTotalScore}"
            : $"Header power: {headerAttackerTotalScore}";
        return $"Click R to Roll for a save with {GetTokenInstructionName(defendingGK)} ({BuildSavingAttributeInstruction(defendingGK, headerGkPenalty)}). {headerInfo}. {BuildNeededRollInstruction(neededRoll, " to save")}{tieInfo}";
    }

    private IEnumerator ResolveShotGoal()
    {
        Debug.Log($"{shooter.name} Shot roll: {shooterRoll} + Shooting: {shooter.shooting}{shootingPenaltyInfo} = {totalShotPower}");
        Debug.Log($"Get IN!! {shooter.name}, buries it to the top corner! Goal!!!");
        RecordSnapshotBallStartedIfNeeded();
        LogExpectedGoalForCurrentShot("goal");
        LogGoalScoredForCurrentShot(MatchManager.Instance.LastTokenToTouchTheBallOnPurpose);
        DeferShotActionResolutionUntilExternalRestart();
        yield return StartCoroutine(groundBallManager.HandleGroundBallMovement(targetHex, GetShotMovementRoll(), allowGKBoxMove: false));
        EndMovementPhaseForShotResolutionIfNeeded(clearStunnedTokens: true);
        goalFlowManager.StartGoalFlow(shooter);
        ResetShotProcess();
    }

    private void LogGoalScoredForCurrentShot(PlayerToken scorer)
    {
        if (scorer == null)
        {
            Debug.LogError("Cannot log shot goal because scorer is null.");
            return;
        }

        if (IsPenaltyShot())
        {
            MatchManager.Instance.MarkNextGoalAsPenalty();
        }

        MatchManager.Instance.gameData.gameLog.LogEvent(scorer, MatchManager.ActionType.GoalScored);
    }

    private IEnumerator ResolveShotOffTarget()
    {
        Debug.Log($"{shooter.name} rolled a {shooterRoll}, this means the Shot is OFF target! GoalKick awarded.");
        LogExpectedGoalForCurrentShot("off target");
        MatchManager.Instance.gameData.gameLog.LogEvent(
            MatchManager.Instance.LastTokenToTouchTheBallOnPurpose
            , MatchManager.ActionType.ShotOffTarget
        );
        RecordSnapshotBallStartedIfNeeded();
        DeferShotActionResolutionUntilExternalRestart();
        yield return StartCoroutine(ShootOffTargetRandomizer());
        EndMovementPhaseForShotResolutionIfNeeded(clearStunnedTokens: true);
        ResetShotProcess();
    }

    private IEnumerator ResolveGKSavingAttempt(ShotInteraction gkEntry, int? rigRoll = null)
    {
        RollInputOverride? rollOverride = rigRoll.HasValue
            ? new RollInputOverride
            {
                hasOverride = true,
                roll = rigRoll.Value,
                isJackpot = false
            }
            : null;
        yield return StartCoroutine(ResolveGKSavingAttempt(gkEntry, rollOverride));
    }

    private IEnumerator ResolveGKSavingAttempt(ShotInteraction gkEntry, RollInputOverride? rollOverride)
    {
        isWaitingForGKDiceRoll = false;
        yield return null;
        if (gkEntry == null || !gkEntry.IsGK)
        {
            Debug.LogError("ResolveGKSavingAttempt called without a GK shot interaction.");
            yield break;
        }

        PlayerToken gkToken = gkEntry.defender;
        saveHex = gkEntry.interactionHex;
        if (shooterRoll == 1)
        {
            yield return StartCoroutine(ResolveShotOffTarget());
            yield break;
        }

        var (returnedRoll, returnedJackpot) = helperFunctions.DiceRoll();
        bool isJackpot = IsJackpotRoll(rollOverride, returnedJackpot);
        int gkRoll = GetRollValueWithJackpot(rollOverride, returnedRoll);
        int gkPenalty = gkEntry.gkPenalty ?? 0;
        int totalSavingPower = isJackpot ? 50 : gkRoll + gkToken.saving + gkPenalty;
        // int totalSavingPower = gkRoll + gkToken.saving + gkPenalty;
        // int totalSavingPower = 6;
        if (isJackpot) Debug.Log($"GK {gkToken.name} rolls A JACKPOT!!!");
        else Debug.Log($"GK {gkToken.name} rolls {gkRoll} + Saving: {gkToken.saving} + Penalty: {gkPenalty} = {totalSavingPower}");
        MatchManager.Instance.gameData.gameLog.LogEvent(
            gkToken
            , MatchManager.ActionType.SaveAttempt
        );

        hexGrid.ClearHighlightedHexes();
        alreadyInterceptedDefs.Add(gkToken);

        if (totalSavingPower == totalShotPower)
        {
            yield return null;
            Debug.Log($"{gkToken.name} ties the attacker's roll!! Loose Ball situation initiated.");
            LogExpectedGoalForCurrentShot("blocked");
            MatchManager.Instance.gameData.gameLog.LogEvent(
                MatchManager.Instance.LastTokenToTouchTheBallOnPurpose
                , MatchManager.ActionType.ShotBlocked
                , connectedToken: gkToken
            );
            MatchManager.Instance.gameData.gameLog.LogEvent(
                gkToken
                , MatchManager.ActionType.SaveMade
                , saveType: "loose"
            );
            MatchManager.Instance.SetHangingPass("shot");
            yield return StartCoroutine(MoveBallAndGoalkeeperToSaveHex(gkToken));
            if (IsPenaltyShot())
            {
                looseBallManager.MarkNextLooseBallGoalAsPenalty();
            }
            DeferShotActionResolutionUntilExternalRestart();
            yield return StartCoroutine(looseBallManager.ResolveLooseBall(gkToken, LooseBallSourceType.GroundDeflection, allowGKBoxMove: false));
            ResetShotProcess();
        }
        else if (totalSavingPower > totalShotPower)
        {
            LogExpectedGoalForCurrentShot("on target");
            MatchManager.Instance.gameData.gameLog.LogEvent(
                MatchManager.Instance.LastTokenToTouchTheBallOnPurpose
                , MatchManager.ActionType.ShotOnTarget
            );
            yield return StartCoroutine(MoveBallAndGoalkeeperToSaveHex(gkToken));
            EndMovementPhaseForShotResolutionIfNeeded(clearStunnedTokens: false);
            // yield return null;
            Debug.Log($"{gkToken.name} saves the shot! Will they hold the ball? {gkToken} needs to roll lower than {gkToken.handling} to hold the ball. Press [R] to roll for Handling Test!");
            isWaitingforHandlingTest = true;
            yield break;
        }
        else if (totalSavingPower < totalShotPower)
        {
            interceptors.Remove(gkEntry);
            currentShotInteraction = null;
            currentDefenderBlockingHex = null;
            if (interceptors.Count > 0)
            {
                // There are more defenders to block, Run through them
                StartCoroutine(OfferBlockRoll());
            }
            else
            {
                Debug.Log($"{shooter.name} Shot roll: {shooterRoll} + Shooting: {shooter.shooting}{shootingPenaltyInfo} = {totalShotPower}");
                Debug.Log($"Get IN!! {shooter.name}, buries it to the top corner! Goal!!!");
                RecordSnapshotBallStartedIfNeeded();
                LogExpectedGoalForCurrentShot("goal");
                LogGoalScoredForCurrentShot(MatchManager.Instance.LastTokenToTouchTheBallOnPurpose);
                DeferShotActionResolutionUntilExternalRestart();
                if (IsPenaltyShot())
                {
                    yield return StartCoroutine(MovePenaltyGoalBallAndGoalkeeper(gkToken, gkRoll));
                }
                else
                {
                    RecordSnapshotBallStartedIfNeeded();
                    yield return StartCoroutine(groundBallManager.HandleGroundBallMovement(targetHex, GetShotMovementRoll(), allowGKBoxMove: false));
                }
                EndMovementPhaseForShotResolutionIfNeeded(clearStunnedTokens: true);
                goalFlowManager.StartGoalFlow(shooter);
                ResetShotProcess();
            }
        }
        yield return null;
    }

    public IEnumerator ResolveHandlingTest(int? rigRoll = null)
    {
        RollInputOverride? rollOverride = rigRoll.HasValue
            ? new RollInputOverride
            {
                hasOverride = true,
                roll = rigRoll.Value,
                isJackpot = false
            }
            : null;
        yield return StartCoroutine(ResolveHandlingTest(rollOverride));
    }

    public IEnumerator ResolveHandlingTest(RollInputOverride? rollOverride)
    {
        PlayerToken gkToken = hexGrid.GetDefendingGK();
        var (returnedRoll, returnedJackpot) = helperFunctions.DiceRoll();
        int gkRoll = GetRollValueWithoutJackpot(rollOverride, returnedRoll);
        isWaitingforHandlingTest = false;
        // Handling Test
        if (gkRoll < gkToken.handling)
        {
            MatchManager.Instance.gameData.gameLog.LogEvent(
                gkToken
                , MatchManager.ActionType.SaveMade
                , saveType: "held"
            );
            MatchManager.Instance.gameData.gameLog.LogEvent(
                gkToken
                , MatchManager.ActionType.BallRecovery
                , connectedToken: MatchManager.Instance.LastTokenToTouchTheBallOnPurpose
                , recoveryType: "shot"
            );
            MatchManager.Instance.ChangePossession();
            MatchManager.Instance.SetLastToken(gkToken);
            Debug.Log($"{gkToken.name} rolled {gkRoll} and holds the ball.");
            EndMovementPhaseForShotResolutionIfNeeded(clearStunnedTokens: false);
            bool waitingForExtraRoll = CompleteShotActionResolution(null);
            if (!waitingForExtraRoll
                && MatchManager.Instance != null
                && MatchManager.Instance.TryEnterExtraActionsRollBeforeRestart(null))
            {
                waitingForExtraRoll = true;
            }

            if (waitingForExtraRoll)
            {
                while (MatchManager.Instance != null && MatchManager.Instance.IsWaitingForExtraActionsRoll)
                {
                    yield return null;
                }

                if (MatchManager.Instance == null
                    || MatchManager.Instance.currentState == MatchManager.GameState.HalfTime
                    || MatchManager.Instance.currentState == MatchManager.GameState.MatchEnded)
                {
                    yield break;
                }
            }

            EnterSaveAndHoldDecision();
            Debug.Log($"{gkToken.name} held the shot. Press [Q]uickThrow, or [K] to activate Final Thirds");
            while (isWaitingForSaveandHoldScenario)
            {
                yield return null;  // Wait for the next frame
            }
        }
        else 
        {
            Debug.Log($"{gkToken.name} rolled {gkRoll} and can't hold it!");
            DeferShotActionResolutionUntilExternalRestart();
            yield return StartCoroutine(looseBallManager.ResolveLooseBall(gkToken, LooseBallSourceType.GoalkeeperHandlingSpill, allowGKBoxMove: false));
        }
        ResetShotProcess();
    }

    private int GetRollValueWithoutJackpot(RollInputOverride? rollOverride, int returnedRoll)
    {
        if (!rollOverride.HasValue || !rollOverride.Value.hasOverride)
        {
            return returnedRoll;
        }

        return rollOverride.Value.isJackpot ? 6 : rollOverride.Value.roll;
    }

    private int GetRollValueWithJackpot(RollInputOverride? rollOverride, int returnedRoll)
    {
        if (!rollOverride.HasValue || !rollOverride.Value.hasOverride)
        {
            return returnedRoll;
        }

        return rollOverride.Value.isJackpot ? 50 : rollOverride.Value.roll;
    }

    private bool IsJackpotRoll(RollInputOverride? rollOverride, bool returnedJackpot)
    {
        if (!rollOverride.HasValue || !rollOverride.Value.hasOverride)
        {
            return returnedJackpot;
        }

        return rollOverride.Value.isJackpot;
    }

    private void QuickThrow()
    {
        isWaitingForSaveandHoldScenario = false;
        Debug.Log("QuickThrow Scenario chosen, NOBODY MOVES! Click Hex to select target for GK's throw");
        ResetShotProcess();
        MatchManager.Instance.BroadcastQuickThrow();
    }
    private void ActivateFinalThirds()
    {
        isWaitingForSaveandHoldScenario = false;  // Cancel the decision phase
        Debug.Log("GK Decided to activate F3 Moves");
        MatchManager.Instance.BroadcastActivateFinalThirdsAfterSave();
        finalThirdManager.TriggerSaveAndHoldFinalThirds();
    }

    private void ResetShotProcess()
    {
        RecordShotActionResolvedIfNeeded();
        RecordSnapshotEndedMovementPhaseIfNeeded();
        MatchManager.Instance?.ClearPendingShotGoalTimeLabel();
        isActivated = false;
        isWaitingforBlockerSelection = false;
        isWaitingForBlockDiceRoll = false;
        isWaitingForShotRoll = false;
        isWaitingForGKDiceRoll = false;
        isWaitingforHandlingTest = false;
        isWaitingForSaveandHoldScenario = false;
        isWaitingForShotCommitConfirmation = false;
        isWaitingforBlockerMovement = false;
        isWaitingForTargetSelection = false;
        ResetExpectedGoalContext();
        ClearShotTargetSelectionHighlights();
        shooter = null;
        freeKickShotOriginHex = null;
        penaltyShotOriginHex = null;
        shotCommitPreviewOriginHex = null;
        targetHex = null;
        trajectoryPath = null;
        interceptors.Clear();
        alreadyInterceptedDefs ??= new List<PlayerToken>();
        alreadyInterceptedDefs.Clear();
        currentShotInteraction = null;
        totalShotPower = 0;
        shooterRoll = 0;
        shooterRollWasJackpot = false;
        boxPenalty = "";
        snapPenalty = "";
        difficultPenalty = "";
        shootingPenaltyInfo = "";
        tokenMoveforDeflection = null;
        saveHex = null;
        currentDefenderBlockingHex = null;
        gkWasOfferedMoveForBox = false;
        shotType = null;
        if (!deferShotActionResolutionUntilExternalRestart)
        {
            committedShotActionKind = MatchManager.MatchActionKind.None;
            shotActionResolutionPending = false;
            shotActionResolvedRecorded = false;
            pendingSnapshotEndedMovementPhase = false;
            pendingSnapshotEndedMovementConsumesExtraAction = false;
        }
        snapshotActionRecorded = false;
        isHeaderAtGoal = false;
        headerAttackerTotalScore = 0;
        headerAttacker = null;
        headerTargetHex = null;
        headerGkPenalty = 0;
    }

    private IEnumerator ShootOffTargetRandomizer()
    {
        var (returnedRoll, returnedJackpot) = helperFunctions.DiceRoll();
        int diceRoll = returnedRoll;
        // int diceRoll = 6;
        switch (diceRoll)
        {
            case 1:
            case 2:
                yield return StartCoroutine(FailedLob());
                break;
            case 3:
            case 4:
                Debug.Log("Camera");
                yield return StartCoroutine(oGamosTouKaragkiozi());
                break;
            case 5:
            case 6:
                yield return StartCoroutine(NextToBar());
                break;
            default:
                yield return StartCoroutine(NextToBar());
                Debug.Log("Value is something else");
                break;
        }
    }

    private IEnumerator FailedLob()
    {
        HexCell originHex = GetShotOriginHex();
        int goalSide = GetAttackingGoalSide();
        int targetX = 22 * goalSide;
        float slope = (float)(targetHex.coordinates.z - originHex.coordinates.z) /
                  (targetHex.coordinates.x - originHex.coordinates.x);
        int intercept = targetHex.coordinates.z - Mathf.RoundToInt(slope * targetHex.coordinates.x);
        int intersectionZ = Mathf.RoundToInt(slope * targetX + intercept);
        yield return StartCoroutine(longBallManager.HandleLongBallMovement(GetOffTargetHex(targetX, intersectionZ), true));
        HexCell lastInboundsHex = GetOffTargetHex(18 * goalSide, Mathf.RoundToInt(slope * (18 * goalSide) + intercept));
        yield return StartCoroutine(ResolveOffTargetGoalKick(lastInboundsHex, goalSide));
    }
    
    private IEnumerator NextToBar()
    {
        HexCell originHex = GetShotOriginHex();
        int goalSide = GetAttackingGoalSide();
        int targetZ = originHex.coordinates.z >= 0 ? 4 : -5;
        HexCell finalHex = GetOffTargetHex(19 * goalSide, targetZ);
        yield return StartCoroutine(groundBallManager.HandleGroundBallMovement(finalHex, 4, allowGKBoxMove: false));
        HexCell lastInboundsHex = GetOffTargetHex(18 * goalSide, targetZ);
        yield return StartCoroutine(ResolveOffTargetGoalKick(lastInboundsHex, goalSide));
    }

    private IEnumerator oGamosTouKaragkiozi()
    {
        hexGrid.ClearHighlightedHexes();
        HexCell originHex = GetShotOriginHex();
        // Step 1: Get Camera Position & Forward Direction
        Transform camTransform = Camera.main.transform;
        Vector3 cameraPosition = camTransform.position;
        Vector3 cameraForward = camTransform.forward.normalized;

        // Step 2: Define Close-Up Target Position (In Front of Camera)
        float closeUpDistance = 1f; // Distance in front of the camera where the ball will stop
        Vector3 closeUpPosition = cameraPosition + (cameraForward * closeUpDistance);

        // Step 3: Move the Ball Towards the Camera (Fast)
        float moveDuration = 0.4f; // Ball flies toward the camera in 0.4 seconds
        float elapsedTime = 0f;
        Vector3 startPos = originHex.GetHexCenter();

        while (elapsedTime < moveDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / moveDuration;
            ball.transform.position = Vector3.Lerp(startPos, closeUpPosition, progress);
            yield return null; // Wait for the next frame
        }

        // Step 4: Hold Ball Near Camera for Dramatic Pause
        yield return new WaitForSeconds(2f);

        // Step 5: Move Ball behind the goal line.
        int goalSide = GetAttackingGoalSide();
        int finalX = 22 * goalSide;
        HexCell finalHex = GetOffTargetHex(finalX, 0);

        if (finalHex != null)
        {
            ball.transform.position = finalHex.GetHexCenter();
            ball.PlaceAtCell(finalHex);
            HexCell lastInboundsHex = GetOffTargetHex(18 * goalSide, 0);
            yield return StartCoroutine(ResolveOffTargetGoalKick(lastInboundsHex, goalSide));
        }
        else
        {
            Debug.LogWarning($"Final hex at ({finalX}, 0, 0) is null!");
        }
    }

    private int GetAttackingGoalSide()
    {
        MatchManager.TeamAttackingDirection attackingDirection = MatchManager.Instance.teamInAttack == MatchManager.TeamInAttack.Home
            ? MatchManager.Instance.homeTeamDirection
            : MatchManager.Instance.awayTeamDirection;

        return attackingDirection == MatchManager.TeamAttackingDirection.LeftToRight ? 1 : -1;
    }

    private HexCell GetOffTargetHex(int x, int z)
    {
        int clampedZ = Mathf.Clamp(z, -hexGrid.GridHeight / 2, hexGrid.GridHeight / 2 - 1);
        return hexGrid.GetHexCellAt(new Vector3Int(x, 0, clampedZ));
    }

    private IEnumerator ResolveOffTargetGoalKick(HexCell lastInboundsHex, int goalSide)
    {
        OutOfBoundsManager manager = EnsureOutOfBoundsManager();
        if (manager == null || lastInboundsHex == null)
        {
            Debug.LogError("Shot off-target could not start GoalKick flow because OutOfBoundsManager or last inbounds hex is missing.");
            yield break;
        }

        string outOfBoundsSide = goalSide > 0 ? "RightGoalLine" : "LeftGoalLine";
        PlayerToken lastTouchToken = MatchManager.Instance.LastTokenToTouchTheBallOnPurpose ?? shooter;
        yield return StartCoroutine(manager.HandleGoalKickOrCorner(lastInboundsHex, outOfBoundsSide, "inaccuracy", lastTouchToken));
    }

    private OutOfBoundsManager EnsureOutOfBoundsManager()
    {
        if (outOfBoundsManager == null)
        {
            outOfBoundsManager = FindAnyObjectByType<OutOfBoundsManager>();
        }

        return outOfBoundsManager;
    }

    public string GetDebugStatus()
    {
        StringBuilder sb = new();
        sb.Append("Shot: ");

        if (isActivated) sb.Append("isActivated, ");
        if (isAvailable) sb.Append("isAvailable, ");
        if (isWaitingforBlockerSelection) sb.Append("isWaitingforBlockerSelection, ");
        if (isWaitingforBlockerMovement) sb.Append("isWaitingforBlockerMovement, ");
        if (isWaitingForTargetSelection) sb.Append("isWaitingForTargetSelection, ");
        if (isWaitingForBlockDiceRoll) sb.Append("isWaitingForBlockDiceRoll, ");
        if (isWaitingForShotRoll) sb.Append("isWaitingForShotRoll, ");
        if (isWaitingForGKDiceRoll) sb.Append("isWaitingForGKDiceRoll, ");
        if (isWaitingforHandlingTest) sb.Append("isWaitingforHandlingTest, ");
        if (isWaitingForSaveandHoldScenario) sb.Append("isWaitingForSaveandHoldScenario, ");
        if (gkWasOfferedMoveForBox) sb.Append("gkWasOfferedMoveForBox, ");
        if (isWaitingForShotCommitConfirmation) sb.Append("isWaitingForShotCommitConfirmation, ");
        if (isHeaderAtGoal) sb.Append("isHeaderAtGoal, ");
        if (!string.IsNullOrEmpty(shotType)) sb.Append($"shotType: {shotType}, ");
        if (shooter != null) sb.Append($"shooter: {shooter.name}, ");
        if (totalShotPower != 0) sb.Append($"totalShotPower: {totalShotPower}, ");
        if (targetHex != null) sb.Append($"targetHex: {targetHex.name}, ");
        if (saveHex != null) sb.Append($"saveHex: {saveHex.name}, ");
        if (isHeaderAtGoal && headerAttacker != null) sb.Append($"headerAttacker: {headerAttacker.name}, ");
        if (isHeaderAtGoal) sb.Append($"headerPower: {headerAttackerTotalScore}, headerGkPenalty: {headerGkPenalty}, ");

        if (sb.Length >= 2 && sb[^2] == ',') sb.Length -= 2; // Trim trailing comma
        return sb.ToString();
    }

    public string GetInstructions()
    {
        StringBuilder sb = new();
        if (MatchManager.Instance != null && MatchManager.Instance.IsWaitingForExtraActionsRoll) return "";
        if (finalThirdManager != null && finalThirdManager.isActivated) return "";
        if (goalKeeperManager != null && goalKeeperManager.isActivated) return "";
        if (!isActivated
            && MatchManager.Instance != null
            && MatchManager.Instance.currentState == MatchManager.GameState.FreeKickExecution)
        {
            return "";
        }
        bool snapshotSuppressed = IsSnapshotSuppressedByFinalExtraMovement();
        if (!snapshotSuppressed && isAvailable && isWaitingForSnapshotDecisionFromLoose) sb.Append("Press [S] to Snapshot directly from there, or [X] no continue without shoooting, ");
        if (!snapshotSuppressed && isAvailable && isWaitingForShotCommitConfirmation) sb.Append("Press [S] again to commit the Shot, ");
        else if (!snapshotSuppressed && isAvailable && !isWaitingForSnapshotDecisionFromLoose) sb.Append("Press [S] to Shoot, ");
        if (isActivated) sb.Append("Shot: ");
        if (isWaitingforBlockerSelection) sb.Append($"Click on a defender to move 2 Hexes in an attempt to block the Snapshot, ");
        if (isWaitingforBlockerMovement) sb.Append($"Click on a Highlighted Hex to move the blocker there, ");
        if (isWaitingForTargetSelection)
        {
            sb.Append("Click on a Hex in the Goal to target the Shot there, ");
            sb.Append(BuildShotTargetSelectionPreviewInstruction());
        }
        if (isWaitingForBlockDiceRoll) sb.Append($"{BuildBlockRollInstruction()}, ");
        if (isWaitingForShotRoll) sb.Append($"{BuildShotRollInstruction()}, ");
        if (isHeaderAtGoal && isWaitingForGKDiceRoll)
        {
            sb.Append($"{BuildHeaderGKSaveRollInstruction()}, ");
        }
        else if (isWaitingForGKDiceRoll)
        {
            sb.Append($"{BuildGKSaveRollInstruction()}, ");
        }
        if (isWaitingforHandlingTest) sb.Append($"Click R to Roll for the Handling Test, ");
        if (isWaitingForSaveandHoldScenario) sb.Append($"Press [Q]uick Throw or [K] to Activate Final thirds, ");

        if (sb.Length >= 2 && sb[^2] == ',') sb.Length -= 2; // Safely trim trailing comma + space
        return sb.ToString();
    }

    public bool? IsInstructionExpectingHomeTeam()
    {
        if (MatchManager.Instance == null || (!isActivated && !isAvailable && !isWaitingForSaveandHoldScenario))
        {
            return null;
        }

        bool attackingTeamIsHome = MatchManager.Instance.teamInAttack == MatchManager.TeamInAttack.Home;
        if (isWaitingForSaveandHoldScenario)
        {
            return attackingTeamIsHome;
        }

        if (!isActivated)
        {
            return attackingTeamIsHome;
        }

        if (isWaitingforBlockerSelection
            || isWaitingforBlockerMovement
            || isWaitingForBlockDiceRoll
            || isWaitingForGKDiceRoll
            || isWaitingforHandlingTest)
        {
            return !attackingTeamIsHome;
        }

        return attackingTeamIsHome;
    }
}
