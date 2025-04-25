using TMPro;
using UnityEngine;
using System.Text;
using System.Collections.Generic;

public class GameDebugMonitor : MonoBehaviour
{
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
    public FreeKickManager freeKickManager;
    public ShotManager shotManager;
    public FinalThirdManager finalThirdManager;
    public GoalFlowManager goalFlowManager;
    public KickoffManager kickoffManager;
    public GoalKeeperManager goalKeeperManager;
    public HexGrid hexgrid;

    [Header("UI Elements")]
    public TextMeshProUGUI debugText;

    [Header("Toggles")]
    public bool isVisible = true;
    private StringBuilder builder = new();

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

        // You can also add key combinations for custom debug commands here
    }

    void Update()
    {
        if (isVisible)
        {
            UpdateDisplay();
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

    private void UpdateDisplay()
    {
        builder.Clear();
        builder.AppendLine("<b>🔎 GAME STATUS</b>");

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

        debugText.text = builder.ToString();
    }
}