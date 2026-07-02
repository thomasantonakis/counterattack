using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Text;
using System.Collections.Generic;

public class GameDebugMonitor : MonoBehaviour
{
    private static readonly bool LogFullRoomDecisionCandidates = false;

    private enum InstructionSide
    {
        Neutral,
        Home,
        Away,
    }

    public static GameDebugMonitor Instance;
    public MatchManager matchManager;
    public GameInputManager gameInputManager;
    public GroundBallManager groundBallManager;
    public MovementPhaseManager movementPhaseManager;
    public HighPassManager highPassManager;
    public HeaderManager headerManager;
    public LongBallManager longBallManager;
    public FirstTimePassManager firstTimePassManager;
    public LooseBallManager looseBallManager;
    public OutOfBoundsManager outOfBoundsManager;
    public ThrowInManager throwInManager;
    public FreeKickManager freeKickManager;
    public PenaltyKickManager penaltyKickManager;
    public PenaltyShootoutManager penaltyShootoutManager;
    public ShotManager shotManager;
    public FinalThirdManager finalThirdManager;
    public GoalFlowManager goalFlowManager;
    public KickoffManager kickoffManager;
    public GoalKeeperManager goalKeeperManager;
    public HexGrid hexgrid;

    [Header("UI Elements")]
    public TextMeshProUGUI debugText;
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI decisionMakingText;
    public RoomDecisionManager roomDecisionManager;

    [Header("Toggles")]
    public bool isVisible = true;
    private StringBuilder builder = new();
    private StringBuilder instruction = new();
    private static readonly Color NeutralInstructionColor = Color.white;
    private static readonly Color NeutralInstructionPanelColor = new Color(0f, 0f, 0f, 0.392f);
    private Image instructionPanelImage;
    private Image decisionMakingPanelImage;
    private GameObject decisionMakingPanelObject;
    private TokenKitInstructionPalette homeInstructionPalette;
    private TokenKitInstructionPalette awayInstructionPalette;
    private string cachedHomeKit = string.Empty;
    private string cachedAwayKit = string.Empty;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        GameInputManager.OnKeyPress += OnKeyReceived;
    }

    private void OnDisable()
    {
        GameInputManager.OnKeyPress -= OnKeyReceived;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnKeyReceived(KeyPressData key)
    {
        // Example: toggle monitor with Ctrl+Tab
        if (key.ctrl && key.key == KeyCode.Tab)
        {
            isVisible = !isVisible;
            Debug.Log($"🪧 GameDebugMonitor visibility toggled: {isVisible}");
        }

    }

    void Update()
    {
        UpdateInstructionText();
        if (isVisible)
        {
            UpdateDebugDisplay();
        }
        else
        {
            debugText.text = "";
        }
    }

    void Start()
    {
        LinkRoomSceneComponents();
        CacheInstructionPanelImage();
        RefreshInstructionPalettes();
    }

    public void LinkRoomSceneComponents()
    {
        // Attempt to assign all managers
        matchManager = FindAnyObjectByType<MatchManager>();
        gameInputManager = FindAnyObjectByType<GameInputManager>();
        groundBallManager = FindAnyObjectByType<GroundBallManager>();
        movementPhaseManager = FindAnyObjectByType<MovementPhaseManager>();
        highPassManager = FindAnyObjectByType<HighPassManager>();
        headerManager = FindAnyObjectByType<HeaderManager>();
        longBallManager = FindAnyObjectByType<LongBallManager>();
        firstTimePassManager = FindAnyObjectByType<FirstTimePassManager>();
        looseBallManager = FindAnyObjectByType<LooseBallManager>();
        outOfBoundsManager = FindAnyObjectByType<OutOfBoundsManager>();
        throwInManager = FindAnyObjectByType<ThrowInManager>();
        freeKickManager = FindAnyObjectByType<FreeKickManager>();
        penaltyKickManager = FindAnyObjectByType<PenaltyKickManager>();
        penaltyShootoutManager = FindAnyObjectByType<PenaltyShootoutManager>();
        shotManager = FindAnyObjectByType<ShotManager>();
        finalThirdManager = FindAnyObjectByType<FinalThirdManager>();
        goalFlowManager = FindAnyObjectByType<GoalFlowManager>();
        kickoffManager = FindAnyObjectByType<KickoffManager>();
        goalKeeperManager = FindAnyObjectByType<GoalKeeperManager>();
        hexgrid = FindAnyObjectByType<HexGrid>();

        // Track missing components
        List<string> missingComponents = new List<string>();

        if (matchManager == null) missingComponents.Add(nameof(matchManager));
        if (gameInputManager == null) missingComponents.Add(nameof(gameInputManager));
        if (groundBallManager == null) missingComponents.Add(nameof(groundBallManager));
        if (movementPhaseManager == null) missingComponents.Add(nameof(movementPhaseManager));
        if (highPassManager == null) missingComponents.Add(nameof(highPassManager));
        if (headerManager == null) missingComponents.Add(nameof(headerManager));
        if (longBallManager == null) missingComponents.Add(nameof(longBallManager));
        if (firstTimePassManager == null) missingComponents.Add(nameof(firstTimePassManager));
        if (looseBallManager == null) missingComponents.Add(nameof(looseBallManager));
        if (outOfBoundsManager == null) missingComponents.Add(nameof(outOfBoundsManager));
        if (throwInManager == null) missingComponents.Add(nameof(throwInManager));
        if (freeKickManager == null) missingComponents.Add(nameof(freeKickManager));
        if (penaltyKickManager == null) missingComponents.Add(nameof(penaltyKickManager));
        if (shotManager == null) missingComponents.Add(nameof(shotManager));
        if (finalThirdManager == null) missingComponents.Add(nameof(finalThirdManager));
        if (goalFlowManager == null) missingComponents.Add(nameof(goalFlowManager));
        if (kickoffManager == null) missingComponents.Add(nameof(kickoffManager));
        if (goalKeeperManager == null) missingComponents.Add(nameof(goalKeeperManager));
        if (hexgrid == null) missingComponents.Add(nameof(hexgrid));

        if (missingComponents.Count > 0)
        {
            string errorLog = "❌ Could not link the following scene components: " + string.Join(", ", missingComponents);
            Debug.LogError(errorLog);
        }
        else
        {
            Debug.Log("✅ All scene components successfully linked.");
        }
    }

    private void UpdateDebugDisplay()
    {
        if (MatchManager.Instance == null)
        {
            debugText.text = "";
            return;
        }

        builder.Clear();
        builder.AppendLine("<b>GAME STATUS</b>");

        // 👇 These methods will be defined by each subsystem
        builder.AppendLine(gameInputManager.GetDebugStatus());
        builder.AppendLine(MatchManager.Instance.GetDebugStatus());
        builder.AppendLine(movementPhaseManager.GetDebugStatus());
        builder.AppendLine(groundBallManager.GetDebugStatus());
        builder.AppendLine(firstTimePassManager.GetDebugStatus());
        builder.AppendLine(highPassManager.GetDebugStatus());
        builder.AppendLine(longBallManager.GetDebugStatus());
        builder.AppendLine(shotManager.GetDebugStatus());
        builder.AppendLine(freeKickManager.GetDebugStatus());
        builder.AppendLine(penaltyKickManager != null ? penaltyKickManager.GetDebugStatus() : "PK: (not linked)");
        builder.AppendLine(goalKeeperManager.GetDebugStatus());
        builder.AppendLine(finalThirdManager.GetDebugStatus());
        builder.AppendLine(looseBallManager.GetDebugStatus());
        builder.AppendLine(throwInManager != null ? throwInManager.GetDebugStatus() : "TI: (not linked)");
        builder.AppendLine(headerManager.GetDebugStatus());

        debugText.text = builder.ToString();
    }

    private void UpdateInstructionText()
    {
        GameplayInstructionSnapshot snapshot = BuildCurrentInstructionSnapshot(
            out InstructionSide activeInstructionSide,
            out bool shouldFlashInstruction);
        if (instructionText != null)
        {
            instructionText.text = snapshot != null ? snapshot.instructionText : string.Empty;
            ApplyInstructionPalette(activeInstructionSide, shouldFlashInstruction);
        }

        bool isSinglePlayerMatch = IsSinglePlayerMatch();
        RefreshDecisionMakingPanelVisibility(isSinglePlayerMatch);
        if (isSinglePlayerMatch && roomDecisionManager != null)
        {
            bool noDecisionNeeded = IsDecisionSuppressedByMovement(snapshot, out string noDecisionReason);
            roomDecisionManager.UpdateDecision(
                snapshot,
                BuildRoomDecisionContext(snapshot),
                MatchManager.Instance?.gameData?.gameSettings,
                noDecisionNeeded,
                noDecisionReason);
        }
        else
        {
            roomDecisionManager?.ClearDecisionState();
        }

        MatchManager.Instance?.RecordInstructionSnapshotIfChanged(snapshot);
    }

    public GameplayInstructionSnapshot GetCurrentInstructionSnapshotForLog()
    {
        return BuildCurrentInstructionSnapshot(out _, out _);
    }

    public GameplayRoomDecisionSnapshot GetRoomDecisionSnapshotForLog(GameplayInstructionSnapshot snapshot)
    {
        if (!IsSinglePlayerMatch())
        {
            return null;
        }

        if (snapshot == null || !snapshot.isAwaitingInput)
        {
            return null;
        }

        RoomDecisionContext context = BuildRoomDecisionContext(snapshot);
        if (context == null || context.actions.Count == 0)
        {
            return null;
        }

        GameplayRoomDecisionSnapshot decisionSnapshot = new GameplayRoomDecisionSnapshot
        {
            manager = context.manager,
            expectedTeam = context.expectedTeam,
            expectedInput = context.expectedInput,
            persona = AIManager.ResolveRoomPersona(
                MatchManager.Instance?.gameData?.gameSettings,
                context.expectedTeam).ToString(),
            candidateCount = context.actions.Count
        };

        Dictionary<string, int> candidateTypeCounts = new();
        foreach (RoomActionCandidate action in context.actions)
        {
            if (action == null)
            {
                continue;
            }

            string typeKey = $"{action.actionType}:{action.step}";
            candidateTypeCounts.TryGetValue(typeKey, out int typeCount);
            candidateTypeCounts[typeKey] = typeCount + 1;

            if (!string.IsNullOrWhiteSpace(action.key)
                && !decisionSnapshot.availableKeys.Contains(action.key))
            {
                decisionSnapshot.availableKeys.Add(action.key);
            }

            if (!string.IsNullOrWhiteSpace(action.key))
            {
                decisionSnapshot.keyCandidateCount++;
            }

            if (action.actor != null || action.targetToken != null)
            {
                decisionSnapshot.tokenCandidateCount++;
            }

            if (action.targetHex != null)
            {
                decisionSnapshot.hexCandidateCount++;
            }

            if (LogFullRoomDecisionCandidates)
            {
                GameplayRoomDecisionCandidate candidate = BuildRoomDecisionCandidate(action);
                if (candidate != null)
                {
                    decisionSnapshot.candidates.Add(candidate);
                }
            }
        }

        foreach (KeyValuePair<string, int> entry in candidateTypeCounts)
        {
            decisionSnapshot.candidateTypes.Add($"{entry.Key}:{entry.Value}");
        }

        return decisionSnapshot.candidateCount > 0 ? decisionSnapshot : null;
    }

    private static GameplayRoomDecisionCandidate BuildRoomDecisionCandidate(RoomActionCandidate action)
    {
        if (action == null)
        {
            return null;
        }

        return new GameplayRoomDecisionCandidate
        {
            id = action.id,
            manager = action.manager,
            actionType = action.actionType.ToString(),
            step = action.step.ToString(),
            label = action.label,
            key = action.key,
            actorTokenKey = MatchManager.GetStableTokenKey(action.actor),
            targetTokenKey = MatchManager.GetStableTokenKey(action.targetToken),
            targetHex = RoomHexCoordinates.FromHex(action.targetHex),
            isExecutableNow = action.isExecutableNow,
            isForfeit = action.isForfeit,
            executionCommand = action.executionCommand,
            reason = action.reason
        };
    }

    private GameplayInstructionSnapshot BuildCurrentInstructionSnapshot(
        out InstructionSide activeInstructionSide,
        out bool shouldFlashInstruction)
    {
        activeInstructionSide = InstructionSide.Neutral;
        shouldFlashInstruction = false;

        string goalFlowInstruction = goalFlowManager != null ? goalFlowManager.GetInstructions() : string.Empty;
        if (!string.IsNullOrWhiteSpace(goalFlowInstruction))
        {
            activeInstructionSide = ResolveInstructionSide(goalFlowManager.IsInstructionExpectingHomeTeam());
            shouldFlashInstruction = goalFlowManager.ShouldFlashInstructionColors();
            return CreateInstructionSnapshot(
                nameof(GoalFlowManager),
                goalFlowInstruction,
                activeInstructionSide,
                shouldFlashInstruction);
        }

        PenaltyShootoutManager activeShootoutManager = PenaltyShootoutManager.ActiveShootout != null
            ? PenaltyShootoutManager.ActiveShootout
            : penaltyShootoutManager != null ? penaltyShootoutManager : FindAnyObjectByType<PenaltyShootoutManager>();
        string shootoutInstruction = activeShootoutManager != null ? activeShootoutManager.GetInstructions() : string.Empty;
        if (!string.IsNullOrWhiteSpace(shootoutInstruction))
        {
            penaltyShootoutManager = activeShootoutManager;
            activeInstructionSide = ResolveInstructionSide(activeShootoutManager.IsInstructionExpectingHomeTeam());
            shouldFlashInstruction = activeShootoutManager.ShouldFlashInstructionColors();
            return CreateInstructionSnapshot(
                nameof(PenaltyShootoutManager),
                shootoutInstruction,
                activeInstructionSide,
                shouldFlashInstruction);
        }

        string matchInstruction = matchManager != null ? matchManager.GetInstructions() : string.Empty;
        if (!string.IsNullOrWhiteSpace(matchInstruction))
        {
            activeInstructionSide = ResolveInstructionSide(matchManager.IsInstructionExpectingHomeTeam());
            return CreateInstructionSnapshot(nameof(MatchManager), matchInstruction, activeInstructionSide, false);
        }

        instruction.Clear();
        List<string> activeInstructions = new List<string>();
        List<string> activeManagers = new List<string>();
        InstructionSide resolvedInstructionSide = InstructionSide.Neutral;

        AddInstructionIfNotEmpty(activeManagers, activeInstructions, ref resolvedInstructionSide, nameof(FinalThirdManager), finalThirdManager != null ? finalThirdManager.GetInstructions() : string.Empty, ResolveInstructionSide(finalThirdManager?.IsInstructionExpectingHomeTeam()));
        AddInstructionIfNotEmpty(activeManagers, activeInstructions, ref resolvedInstructionSide, nameof(MovementPhaseManager), movementPhaseManager != null ? movementPhaseManager.GetInstructions() : string.Empty, ResolveInstructionSide(movementPhaseManager?.IsInstructionExpectingHomeTeam()));
        AddInstructionIfNotEmpty(activeManagers, activeInstructions, ref resolvedInstructionSide, nameof(GoalKeeperManager), goalKeeperManager != null ? goalKeeperManager.GetInstructions() : string.Empty, ResolveInstructionSide(goalKeeperManager?.IsInstructionExpectingHomeTeam()));
        AddInstructionIfNotEmpty(activeManagers, activeInstructions, ref resolvedInstructionSide, nameof(LooseBallManager), looseBallManager != null ? looseBallManager.GetInstructions() : string.Empty, ResolveInstructionSide(looseBallManager?.IsInstructionExpectingHomeTeam()));
        AddInstructionIfNotEmpty(activeManagers, activeInstructions, ref resolvedInstructionSide, nameof(ThrowInManager), throwInManager != null ? throwInManager.GetInstructions() : string.Empty, ResolveInstructionSide(throwInManager?.IsInstructionExpectingHomeTeam()));
        AddInstructionIfNotEmpty(activeManagers, activeInstructions, ref resolvedInstructionSide, nameof(ShotManager), shotManager != null ? shotManager.GetInstructions() : string.Empty, ResolveInstructionSide(shotManager?.IsInstructionExpectingHomeTeam()));
        AddInstructionIfNotEmpty(activeManagers, activeInstructions, ref resolvedInstructionSide, nameof(KickoffManager), kickoffManager != null ? kickoffManager.GetInstructions() : string.Empty, ResolveInstructionSide(kickoffManager?.IsInstructionExpectingHomeTeam()));
        AddInstructionIfNotEmpty(activeManagers, activeInstructions, ref resolvedInstructionSide, nameof(GroundBallManager), groundBallManager != null ? groundBallManager.GetInstructions() : string.Empty, ResolveInstructionSide(groundBallManager?.IsInstructionExpectingHomeTeam()));
        AddInstructionIfNotEmpty(activeManagers, activeInstructions, ref resolvedInstructionSide, nameof(FirstTimePassManager), firstTimePassManager != null ? firstTimePassManager.GetInstructions() : string.Empty, ResolveInstructionSide(firstTimePassManager?.IsInstructionExpectingHomeTeam()));
        AddInstructionIfNotEmpty(activeManagers, activeInstructions, ref resolvedInstructionSide, nameof(FreeKickManager), freeKickManager != null ? freeKickManager.GetInstructions() : string.Empty, ResolveInstructionSide(freeKickManager?.IsInstructionExpectingHomeTeam()));
        AddInstructionIfNotEmpty(activeManagers, activeInstructions, ref resolvedInstructionSide, nameof(PenaltyKickManager), penaltyKickManager != null ? penaltyKickManager.GetInstructions() : string.Empty, ResolveInstructionSide(penaltyKickManager?.IsInstructionExpectingHomeTeam()));
        AddInstructionIfNotEmpty(activeManagers, activeInstructions, ref resolvedInstructionSide, nameof(HighPassManager), highPassManager != null ? highPassManager.GetInstructions() : string.Empty, ResolveInstructionSide(highPassManager?.IsInstructionExpectingHomeTeam()));
        AddInstructionIfNotEmpty(activeManagers, activeInstructions, ref resolvedInstructionSide, nameof(LongBallManager), longBallManager != null ? longBallManager.GetInstructions() : string.Empty, ResolveInstructionSide(longBallManager?.IsInstructionExpectingHomeTeam()));
        AddInstructionIfNotEmpty(activeManagers, activeInstructions, ref resolvedInstructionSide, nameof(HeaderManager), headerManager != null ? headerManager.GetInstructions() : string.Empty, ResolveInstructionSide(headerManager?.IsInstructionExpectingHomeTeam()));

        if (activeInstructions.Count == 0)
        {
            return null;
        }

        activeInstructionSide = resolvedInstructionSide;
        instruction.Append(string.Join(" / ", activeInstructions));
        return CreateInstructionSnapshot(
            string.Join(",", activeManagers),
            instruction.ToString(),
            activeInstructionSide,
            false);
    }

    private GameplayInstructionSnapshot CreateInstructionSnapshot(
        string managerName,
        string instructionTextValue,
        InstructionSide side,
        bool shouldFlashInstruction)
    {
        if (string.IsNullOrWhiteSpace(instructionTextValue))
        {
            return null;
        }

        string trimmedInstruction = instructionTextValue.Trim();
        List<string> expectedKeys = ExtractExpectedKeys(trimmedInstruction);
        GameplayInstructionSnapshot snapshot = new GameplayInstructionSnapshot
        {
            isAwaitingInput = true,
            manager = managerName,
            instructionText = trimmedInstruction,
            expectedTeam = FormatInstructionSide(side),
            instructionSide = FormatInstructionSide(side),
            expectedInput = InferExpectedInput(trimmedInstruction, expectedKeys),
            expectedKeys = expectedKeys,
            details = new Dictionary<string, string>
            {
                ["shouldFlash"] = shouldFlashInstruction.ToString()
            }
        };

        if (!string.IsNullOrWhiteSpace(managerName)
            && managerName.Contains(nameof(GroundBallManager))
            && groundBallManager != null)
        {
            groundBallManager.PopulateInstructionLogSnapshot(snapshot);
        }

        if (!string.IsNullOrWhiteSpace(managerName)
            && managerName.Contains(nameof(HighPassManager))
            && highPassManager != null)
        {
            highPassManager.PopulateInstructionLogSnapshot(snapshot);
        }

        return snapshot;
    }

    private RoomDecisionContext BuildRoomDecisionContext(GameplayInstructionSnapshot snapshot)
    {
        if (snapshot == null)
        {
            return null;
        }

        RoomDecisionContext context = new RoomDecisionContext
        {
            manager = snapshot.manager,
            expectedTeam = snapshot.expectedTeam,
            expectedInput = snapshot.expectedInput
        };

        if (!string.IsNullOrWhiteSpace(snapshot.manager)
            && snapshot.manager.Contains(nameof(GoalFlowManager))
            && goalFlowManager != null)
        {
            goalFlowManager.PopulateRoomDecisionContext(context);
        }

        if (!string.IsNullOrWhiteSpace(snapshot.manager)
            && snapshot.manager.Contains(nameof(PenaltyShootoutManager)))
        {
            PenaltyShootoutManager activeShootoutManager = PenaltyShootoutManager.ActiveShootout != null
                ? PenaltyShootoutManager.ActiveShootout
                : penaltyShootoutManager;
            if (activeShootoutManager != null)
            {
                activeShootoutManager.PopulateRoomDecisionContext(context);
            }
        }

        if (!string.IsNullOrWhiteSpace(snapshot.manager)
            && snapshot.manager.Contains(nameof(MatchManager))
            && matchManager != null)
        {
            matchManager.PopulateRoomDecisionContext(context);
        }

        if (!string.IsNullOrWhiteSpace(snapshot.manager)
            && snapshot.manager.Contains(nameof(MovementPhaseManager))
            && movementPhaseManager != null)
        {
            movementPhaseManager.PopulateRoomDecisionContext(context);
        }

        if (!string.IsNullOrWhiteSpace(snapshot.manager)
            && snapshot.manager.Contains(nameof(FinalThirdManager))
            && finalThirdManager != null)
        {
            finalThirdManager.PopulateRoomDecisionContext(context);
        }

        if (ShouldPopulateManagerContext(snapshot, nameof(GroundBallManager))
            && groundBallManager != null)
        {
            groundBallManager.PopulateRoomDecisionContext(context);
        }

        if (!string.IsNullOrWhiteSpace(snapshot.manager)
            && snapshot.manager.Contains(nameof(FirstTimePassManager))
            && firstTimePassManager != null)
        {
            firstTimePassManager.PopulateRoomDecisionContext(context);
        }

        if (ShouldPopulateManagerContext(snapshot, nameof(HighPassManager))
            && highPassManager != null)
        {
            highPassManager.PopulateRoomDecisionContext(context);
        }

        if (ShouldPopulateManagerContext(snapshot, nameof(LongBallManager))
            && longBallManager != null)
        {
            longBallManager.PopulateRoomDecisionContext(context);
        }

        if (ShouldPopulateManagerContext(snapshot, nameof(ShotManager))
            && shotManager != null)
        {
            shotManager.PopulateRoomDecisionContext(context);
        }

        if (!string.IsNullOrWhiteSpace(snapshot.manager)
            && snapshot.manager.Contains(nameof(KickoffManager))
            && kickoffManager != null)
        {
            kickoffManager.PopulateRoomDecisionContext(context);
        }

        if (!string.IsNullOrWhiteSpace(snapshot.manager)
            && snapshot.manager.Contains(nameof(GoalKeeperManager))
            && goalKeeperManager != null)
        {
            goalKeeperManager.PopulateRoomDecisionContext(context);
        }

        if (!string.IsNullOrWhiteSpace(snapshot.manager)
            && snapshot.manager.Contains(nameof(LooseBallManager))
            && looseBallManager != null)
        {
            looseBallManager.PopulateRoomDecisionContext(context);
        }

        if (!string.IsNullOrWhiteSpace(snapshot.manager)
            && snapshot.manager.Contains(nameof(ThrowInManager))
            && throwInManager != null)
        {
            throwInManager.PopulateRoomDecisionContext(context);
        }

        if (!string.IsNullOrWhiteSpace(snapshot.manager)
            && snapshot.manager.Contains(nameof(FreeKickManager))
            && freeKickManager != null)
        {
            freeKickManager.PopulateRoomDecisionContext(context);
        }

        if (!string.IsNullOrWhiteSpace(snapshot.manager)
            && snapshot.manager.Contains(nameof(PenaltyKickManager))
            && penaltyKickManager != null)
        {
            penaltyKickManager.PopulateRoomDecisionContext(context);
        }

        if (!string.IsNullOrWhiteSpace(snapshot.manager)
            && snapshot.manager.Contains(nameof(HeaderManager))
            && headerManager != null)
        {
            headerManager.PopulateRoomDecisionContext(context);
        }

        if (snapshot.expectedKeys != null)
        {
            foreach (string key in snapshot.expectedKeys)
            {
                context.AddKey(key);
            }
        }

        return context;
    }

    private bool ShouldPopulateManagerContext(GameplayInstructionSnapshot snapshot, string managerName)
    {
        if (snapshot != null
            && !string.IsNullOrWhiteSpace(snapshot.manager)
            && snapshot.manager.Contains(managerName))
        {
            return true;
        }

        if (freeKickManager == null || !freeKickManager.isWaitingForExecution)
        {
            return false;
        }

        if (managerName == nameof(GroundBallManager))
        {
            return groundBallManager != null && groundBallManager.isActivated;
        }

        if (managerName == nameof(HighPassManager))
        {
            return highPassManager != null && highPassManager.isActivated;
        }

        if (managerName == nameof(LongBallManager))
        {
            return longBallManager != null && longBallManager.isActivated;
        }

        if (managerName == nameof(ShotManager))
        {
            return shotManager != null
                && (shotManager.isActivated || shotManager.isWaitingForShotCommitConfirmation);
        }

        return false;
    }

    private bool IsDecisionSuppressedByMovement(GameplayInstructionSnapshot snapshot, out string reason)
    {
        if (snapshot != null
            && !string.IsNullOrWhiteSpace(snapshot.manager)
            && snapshot.manager.Contains(nameof(GoalFlowManager)))
        {
            reason = "goal flow transition";
            return true;
        }

        if (snapshot != null
            && !string.IsNullOrWhiteSpace(snapshot.manager)
            && snapshot.manager.Contains(nameof(PenaltyShootoutManager)))
        {
            reason = "penalty shootout transition";
            return true;
        }

        if (kickoffManager != null && kickoffManager.IsWaitingForMovementToComplete())
        {
            reason = "kick-off setup token is moving";
            return true;
        }

        Ball activeBall = MatchManager.Instance != null && MatchManager.Instance.ball != null
            ? MatchManager.Instance.ball
            : movementPhaseManager != null ? movementPhaseManager.ball : null;

        if (activeBall != null && activeBall.isMoving)
        {
            reason = "ball is moving";
            return true;
        }

        if (movementPhaseManager != null && movementPhaseManager.isPlayerMoving)
        {
            reason = "movement phase token is moving";
            return true;
        }

        List<string> pendingAerialTargetMaps = new List<string>();
        if (highPassManager != null
            && (highPassManager.IsWaitingForAvailableDecisionTargetOptions()
                || highPassManager.IsWaitingForDecisionTargetOptions()))
        {
            pendingAerialTargetMaps.Add("High Pass");
        }

        if (longBallManager != null
            && (longBallManager.IsWaitingForAvailableDecisionTargetOptions()
                || longBallManager.IsWaitingForDecisionTargetOptions()))
        {
            pendingAerialTargetMaps.Add("Long Ball");
        }

        if (pendingAerialTargetMaps.Count > 0)
        {
            reason = $"aerial target options are still calculating: {string.Join(", ", pendingAerialTargetMaps)}";
            return true;
        }

        reason = string.Empty;
        return false;
    }

    private static void AddInstructionIfNotEmpty(
        List<string> activeManagers,
        List<string> activeInstructions,
        ref InstructionSide resolvedInstructionSide,
        string managerName,
        string instruction,
        InstructionSide side)
    {
        if (string.IsNullOrWhiteSpace(instruction))
        {
            return;
        }

        if (resolvedInstructionSide == InstructionSide.Neutral && side != InstructionSide.Neutral)
        {
            resolvedInstructionSide = side;
        }

        activeManagers.Add(managerName);
        activeInstructions.Add(instruction);
    }

    private static InstructionSide ResolveInstructionSide(bool? expectsHomeTeam)
    {
        if (!expectsHomeTeam.HasValue)
        {
            return InstructionSide.Neutral;
        }

        return expectsHomeTeam.Value ? InstructionSide.Home : InstructionSide.Away;
    }

    private static string FormatInstructionSide(InstructionSide side)
    {
        return side switch
        {
            InstructionSide.Home => "Home",
            InstructionSide.Away => "Away",
            _ => "Neutral",
        };
    }

    private static List<string> ExtractExpectedKeys(string instructionTextValue)
    {
        List<string> keys = new List<string>();
        if (string.IsNullOrWhiteSpace(instructionTextValue))
        {
            return keys;
        }

        int searchIndex = 0;
        while (searchIndex < instructionTextValue.Length)
        {
            int startIndex = instructionTextValue.IndexOf('[', searchIndex);
            if (startIndex < 0)
            {
                break;
            }

            int endIndex = instructionTextValue.IndexOf(']', startIndex + 1);
            if (endIndex < 0)
            {
                break;
            }

            string key = instructionTextValue.Substring(startIndex + 1, endIndex - startIndex - 1).Trim();
            if (!string.IsNullOrWhiteSpace(key) && !keys.Contains(key))
            {
                keys.Add(key);
            }

            searchIndex = endIndex + 1;
        }

        string lowerInstruction = instructionTextValue.ToLowerInvariant();
        if ((lowerInstruction.Contains("press r") || lowerInstruction.Contains("click r"))
            && !keys.Contains("R"))
        {
            keys.Add("R");
        }

        return keys;
    }

    private static string InferExpectedInput(string instructionTextValue, List<string> expectedKeys)
    {
        if (string.IsNullOrWhiteSpace(instructionTextValue))
        {
            return "none";
        }

        string lowerInstruction = instructionTextValue.ToLowerInvariant();
        bool expectsKey = expectedKeys != null && expectedKeys.Count > 0;
        bool expectsTokenClick = lowerInstruction.Contains("click on a token")
            || lowerInstruction.Contains("click a token")
            || lowerInstruction.Contains("click on an attacker")
            || lowerInstruction.Contains("click an attacker")
            || lowerInstruction.Contains("click on a defender")
            || lowerInstruction.Contains("click a defender")
            || lowerInstruction.Contains("click on a player")
            || lowerInstruction.Contains("click a player");
        bool expectsHexClick = lowerInstruction.Contains("click on a hex")
            || lowerInstruction.Contains("click a hex")
            || lowerInstruction.Contains("click on an inbounds hex")
            || lowerInstruction.Contains("click an inbounds hex")
            || lowerInstruction.Contains("click a reachable hex")
            || lowerInstruction.Contains("click on an empty hex")
            || lowerInstruction.Contains("click an empty hex")
            || lowerInstruction.Contains("click on highlighted hex")
            || lowerInstruction.Contains("click a highlighted hex")
            || lowerInstruction.Contains("click this hex")
            || lowerInstruction.Contains("click it to select")
            || lowerInstruction.Contains("click again to confirm")
            || lowerInstruction.Contains("click the orange target")
            || lowerInstruction.Contains("click this orange target")
            || lowerInstruction.Contains("click the selected orange target")
            || lowerInstruction.Contains("click on a valid target")
            || lowerInstruction.Contains("click a valid target")
            || lowerInstruction.Contains("choose another valid target");
        bool expectsClick = expectsTokenClick
            || expectsHexClick
            || lowerInstruction.Contains("click on")
            || lowerInstruction.Contains("click the")
            || lowerInstruction.Contains("click this")
            || lowerInstruction.Contains("click a")
            || lowerInstruction.Contains("click another")
            || lowerInstruction.Contains("choose another");
        bool expectsHover = lowerInstruction.Contains("hover ");
        string clickInput = expectsTokenClick
            ? "click_token"
            : expectsHexClick
                ? "click_hex"
                : "click";

        if (expectsKey && (expectsClick || expectsHover))
        {
            return expectsClick ? $"key_or_{clickInput}" : "key_or_hover";
        }

        if (expectsKey)
        {
            return "key";
        }

        if (expectsHover && expectsClick)
        {
            return $"hover_or_{clickInput}";
        }

        if (expectsHover)
        {
            return "hover_hex";
        }

        if (expectsClick)
        {
            return clickInput;
        }

        return "unknown";
    }

    private void ApplyInstructionPalette(InstructionSide side, bool shouldFlash = false)
    {
        RefreshInstructionPalettes();

        TokenKitInstructionPalette palette = side switch
        {
            InstructionSide.Home => homeInstructionPalette,
            InstructionSide.Away => awayInstructionPalette,
            _ => new TokenKitInstructionPalette(NeutralInstructionPanelColor, NeutralInstructionColor),
        };

        bool useSwappedColors = shouldFlash && Mathf.FloorToInt(Time.unscaledTime * 6f) % 2 == 1;
        Color textColor = useSwappedColors ? palette.Primary : palette.Secondary;
        Color panelColor = useSwappedColors ? palette.Secondary : palette.Primary;
        panelColor.a = side == InstructionSide.Neutral ? NeutralInstructionPanelColor.a : 0.82f;

        if (instructionText != null)
        {
            instructionText.color = textColor;
        }

        if (instructionPanelImage != null)
        {
            instructionPanelImage.color = panelColor;
        }

        if (IsSinglePlayerMatch() && decisionMakingText != null)
        {
            decisionMakingText.color = textColor;
        }

        if (IsSinglePlayerMatch() && decisionMakingPanelImage != null)
        {
            decisionMakingPanelImage.color = panelColor;
        }
    }

    private bool IsSinglePlayerMatch()
    {
        return MatchManager.Instance?.gameData?.gameSettings != null
            && string.Equals(
                MatchManager.Instance.gameData.gameSettings.gameMode,
                ApplicationManager.SinglePlayerGameMode,
                System.StringComparison.OrdinalIgnoreCase);
    }

    private void RefreshDecisionMakingPanelVisibility(bool isSinglePlayerMatch)
    {
        if (decisionMakingPanelObject == null && decisionMakingPanelImage != null)
        {
            decisionMakingPanelObject = decisionMakingPanelImage.gameObject;
        }

        if (decisionMakingPanelObject != null && decisionMakingPanelObject.activeSelf != isSinglePlayerMatch)
        {
            decisionMakingPanelObject.SetActive(isSinglePlayerMatch);
        }
    }

    private void RefreshInstructionPalettes()
    {
        if (MatchManager.Instance?.gameData?.gameSettings == null)
        {
            homeInstructionPalette = new TokenKitInstructionPalette(NeutralInstructionPanelColor, NeutralInstructionColor);
            awayInstructionPalette = homeInstructionPalette;
            cachedHomeKit = string.Empty;
            cachedAwayKit = string.Empty;
            return;
        }

        string homeKit = MatchManager.Instance.gameData.gameSettings.homeKit ?? string.Empty;
        string awayKit = MatchManager.Instance.gameData.gameSettings.awayKit ?? string.Empty;
        if (homeKit == cachedHomeKit && awayKit == cachedAwayKit)
        {
            return;
        }

        homeInstructionPalette = TokenKitCatalog.ResolveInstructionPalette(homeKit, NeutralInstructionPanelColor, NeutralInstructionColor);
        awayInstructionPalette = TokenKitCatalog.ResolveInstructionPalette(awayKit, NeutralInstructionPanelColor, NeutralInstructionColor);
        cachedHomeKit = homeKit;
        cachedAwayKit = awayKit;
    }

    private void CacheInstructionPanelImage()
    {
        if (roomDecisionManager == null)
        {
            roomDecisionManager = FindAnyObjectByType<RoomDecisionManager>();
        }

        if (decisionMakingText == null && roomDecisionManager != null)
        {
            decisionMakingText = roomDecisionManager.decisionText;
        }

        if (instructionText != null)
        {
            instructionPanelImage = instructionText.GetComponentInParent<Image>();
            if (instructionPanelImage == null)
            {
                Debug.LogWarning("Instruction text has no parent Image for kit-colored instruction panel background.");
            }
        }

        if (decisionMakingText != null)
        {
            decisionMakingPanelImage = decisionMakingText.GetComponentInParent<Image>();
            if (decisionMakingPanelImage == null)
            {
                Debug.LogWarning("Decision making text has no parent Image for kit-colored decision panel background.");
            }
            else
            {
                decisionMakingPanelObject = decisionMakingPanelImage.gameObject;
                RefreshDecisionMakingPanelVisibility(IsSinglePlayerMatch());
            }
        }
    }
}
