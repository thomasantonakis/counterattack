using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Text;
using System.Collections.Generic;

public class GameDebugMonitor : MonoBehaviour
{
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

    [Header("Toggles")]
    public bool isVisible = true;
    private StringBuilder builder = new();
    private StringBuilder instruction = new();
    private static readonly Color NeutralInstructionColor = Color.white;
    private static readonly Color NeutralInstructionPanelColor = new Color(0f, 0f, 0f, 0.392f);
    private Image instructionPanelImage;
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

        MatchManager.Instance?.RecordInstructionSnapshotIfChanged(snapshot);
    }

    public GameplayInstructionSnapshot GetCurrentInstructionSnapshotForLog()
    {
        return BuildCurrentInstructionSnapshot(out _, out _);
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
        instructionText.color = useSwappedColors ? palette.Primary : palette.Secondary;

        if (instructionPanelImage != null)
        {
            Color panelColor = useSwappedColors ? palette.Secondary : palette.Primary;
            panelColor.a = side == InstructionSide.Neutral ? NeutralInstructionPanelColor.a : 0.82f;
            instructionPanelImage.color = panelColor;
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
        if (instructionText == null)
        {
            return;
        }

        instructionPanelImage = instructionText.GetComponentInParent<Image>();
        if (instructionPanelImage == null)
        {
            Debug.LogWarning("Instruction text has no parent Image for kit-colored instruction panel background.");
        }
    }
}
