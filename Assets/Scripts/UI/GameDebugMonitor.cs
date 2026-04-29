using TMPro;
using UnityEngine;
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

    private void OnEnable()
    {
        GameInputManager.OnKeyPress += OnKeyReceived;
    }

    private void OnDisable()
    {
        GameInputManager.OnKeyPress -= OnKeyReceived;
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
    }

    public void LinkRoomSceneComponents()
    {
        // Attempt to assign all managers
        gameInputManager = FindObjectOfType<GameInputManager>();
        groundBallManager = FindObjectOfType<GroundBallManager>();
        movementPhaseManager = FindObjectOfType<MovementPhaseManager>();
        highPassManager = FindObjectOfType<HighPassManager>();
        headerManager = FindObjectOfType<HeaderManager>();
        longBallManager = FindObjectOfType<LongBallManager>();
        firstTimePassManager = FindObjectOfType<FirstTimePassManager>();
        looseBallManager = FindObjectOfType<LooseBallManager>();
        outOfBoundsManager = FindObjectOfType<OutOfBoundsManager>();
        throwInManager = FindObjectOfType<ThrowInManager>();
        freeKickManager = FindObjectOfType<FreeKickManager>();
        shotManager = FindObjectOfType<ShotManager>();
        finalThirdManager = FindObjectOfType<FinalThirdManager>();
        goalFlowManager = FindObjectOfType<GoalFlowManager>();
        kickoffManager = FindObjectOfType<KickoffManager>();
        goalKeeperManager = FindObjectOfType<GoalKeeperManager>();
        hexgrid = FindObjectOfType<HexGrid>();

        // Track missing components
        List<string> missingComponents = new List<string>();

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
        builder.AppendLine(goalKeeperManager.GetDebugStatus());
        builder.AppendLine(finalThirdManager.GetDebugStatus());
        builder.AppendLine(looseBallManager.GetDebugStatus());
        builder.AppendLine(throwInManager != null ? throwInManager.GetDebugStatus() : "TI: (not linked)");
        builder.AppendLine(headerManager.GetDebugStatus());

        debugText.text = builder.ToString();
    }

    private void UpdateInstructionText()
    {
        instruction.Clear();

        List<string> activeInstructions = new List<string>();
        InstructionSide activeInstructionSide = InstructionSide.Neutral;

        void AddIfNotEmpty(string s, InstructionSide side)
        {
            if (!string.IsNullOrWhiteSpace(s))
            {
                if (activeInstructionSide == InstructionSide.Neutral && side != InstructionSide.Neutral)
                {
                    activeInstructionSide = side;
                }

                activeInstructions.Add(s);
            }
        }

        AddIfNotEmpty(finalThirdManager != null ? finalThirdManager.GetInstructions() : string.Empty, ResolveInstructionSide(finalThirdManager?.IsInstructionExpectingHomeTeam()));
        AddIfNotEmpty(movementPhaseManager != null ? movementPhaseManager.GetInstructions() : string.Empty, ResolveInstructionSide(movementPhaseManager?.IsInstructionExpectingHomeTeam()));
        AddIfNotEmpty(goalKeeperManager != null ? goalKeeperManager.GetInstructions() : string.Empty, ResolveInstructionSide(goalKeeperManager?.IsInstructionExpectingHomeTeam()));
        AddIfNotEmpty(looseBallManager != null ? looseBallManager.GetInstructions() : string.Empty, ResolveInstructionSide(looseBallManager?.IsInstructionExpectingHomeTeam()));
        AddIfNotEmpty(throwInManager != null ? throwInManager.GetInstructions() : string.Empty, ResolveInstructionSide(throwInManager?.IsInstructionExpectingHomeTeam()));
        AddIfNotEmpty(shotManager != null ? shotManager.GetInstructions() : string.Empty, ResolveInstructionSide(shotManager?.IsInstructionExpectingHomeTeam()));
        AddIfNotEmpty(groundBallManager != null ? groundBallManager.GetInstructions() : string.Empty, ResolveInstructionSide(groundBallManager?.IsInstructionExpectingHomeTeam()));
        AddIfNotEmpty(firstTimePassManager != null ? firstTimePassManager.GetInstructions() : string.Empty, ResolveInstructionSide(firstTimePassManager?.IsInstructionExpectingHomeTeam()));
        AddIfNotEmpty(freeKickManager != null ? freeKickManager.GetInstructions() : string.Empty, ResolveInstructionSide(freeKickManager?.IsInstructionExpectingHomeTeam()));
        AddIfNotEmpty(highPassManager != null ? highPassManager.GetInstructions() : string.Empty, ResolveInstructionSide(highPassManager?.IsInstructionExpectingHomeTeam()));
        AddIfNotEmpty(longBallManager != null ? longBallManager.GetInstructions() : string.Empty, ResolveInstructionSide(longBallManager?.IsInstructionExpectingHomeTeam()));
        AddIfNotEmpty(headerManager != null ? headerManager.GetInstructions() : string.Empty, ResolveInstructionSide(headerManager?.IsInstructionExpectingHomeTeam()));

        instruction.Append(string.Join(" / ", activeInstructions));
        if (instructionText != null)
        {
            instructionText.text = instruction.ToString();
            instructionText.color = ResolveInstructionColor(activeInstructionSide);
        }
    }

    private static InstructionSide ResolveInstructionSide(bool? expectsHomeTeam)
    {
        if (!expectsHomeTeam.HasValue)
        {
            return InstructionSide.Neutral;
        }

        return expectsHomeTeam.Value ? InstructionSide.Home : InstructionSide.Away;
    }

    private static Color ResolveInstructionColor(InstructionSide side)
    {
        if (MatchManager.Instance?.gameData?.gameSettings == null)
        {
            return NeutralInstructionColor;
        }

        return side switch
        {
            InstructionSide.Home => ResolveKitBodyColor(MatchManager.Instance.gameData.gameSettings.homeKit, NeutralInstructionColor),
            InstructionSide.Away => ResolveKitBodyColor(MatchManager.Instance.gameData.gameSettings.awayKit, NeutralInstructionColor),
            _ => NeutralInstructionColor,
        };
    }

    private static Color ResolveKitBodyColor(string kitIdOrAlias, Color fallbackColor)
    {
        if (string.IsNullOrWhiteSpace(kitIdOrAlias))
        {
            return fallbackColor;
        }

        TokenKitPreset preset = TokenKitCatalog.GetPresetByIdOrAlias(kitIdOrAlias);
        if (preset?.Style == null)
        {
            return fallbackColor;
        }

        return preset.Style.bodyColor;
    }
}
