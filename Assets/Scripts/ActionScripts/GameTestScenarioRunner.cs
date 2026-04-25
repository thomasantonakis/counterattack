using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using System.Text;
using System.Linq;
using System.Reflection;

public struct AvailabilityCheckResult
{
    public bool passed;
    public List<string> failures;

    public bool IsSuccess => passed;

    public AvailabilityCheckResult(bool passed, List<string> failures)
    {
        this.passed = passed;
        this.failures = failures;
    }

    public string GetFailureReport()
    {
        return ToString();
    }

    public override string ToString()
    {
        return passed
            ? "✅ All availability flags are correct."
            : "❌ Failures: " + string.Join(", ", failures);
    }
}

public class GameStatusSnapshot
{
    public bool gbmAvailable;
    public bool gbmisActivated;
    public bool gbmIsAwaitingTargetSelection;
    public bool gbmIsWaitingForDiceRoll;
    public HexCell gbmCurrentTargetHex;
    public bool ftpAvailable;
    public bool ftpIsActivated;
    public bool ftpIsAwaitingTargetSelection;
    public bool ftpIsWaitingforAttackerSelection;
    public bool ftpIsWaitingforDefenderSelection;
    public bool ftpIsWaitingforDiceRoll;
    public HexCell ftpCurrentTargetHex;
    public bool hpmAvailable;
    public bool hpmIsActivated;
    public bool hpmIsAwaitingTargetSelection;
    public bool hpmIsWaitingforAttackerSelection;
    public bool hpmIsWaitingforDefenderSelection;
    public HexCell hpmCurrentTargetHex;
    public bool hpmIsWaitingForAccuracyRoll;
    public bool hpmIsWaitingForDirectionRoll;
    public bool hpmIsWaitingForDistanceRoll;
    public bool lbmAvailable;
    public bool lbmIsActivated;
    public bool lbmIsAwaitingTargetSelection;
    public bool lbmIsWaitingForAccuracyRoll;
    public bool lbmIsWaitingForDirectionRoll;
    public bool lbmIsWaitingForDistanceRoll;
    public bool lbmIsWaitingForDefLBMove;
    public bool looseIsActivated;
    public bool looseIsWaitingForDirectionRoll;
    public bool looseIsWaitingForDistanceRoll;
    public bool looseIsWaitingForInterceptionRoll;
    public HexCell lbmCurrentTargetHex;

    public GameStatusSnapshot(
        GroundBallManager gbm, FirstTimePassManager ftp, HighPassManager hpm, LongBallManager lbm, MovementPhaseManager mpm
        , FinalThirdManager ftm, HeaderManager hdm, LooseBallManager loose, OutOfBoundsManager obob, FreeKickManager fkm
        , ShotManager shot, GoalKeeperManager gkm, GoalFlowManager gfm, KickoffManager kom, HexGrid hg, GameInputManager gim
    )
    {
        gbmAvailable = gbm.isAvailable;
        gbmisActivated = gbm.isActivated;
        gbmIsAwaitingTargetSelection = gbm.isAwaitingTargetSelection; 
        gbmIsWaitingForDiceRoll = gbm.isWaitingForDiceRoll;
        gbmCurrentTargetHex = gbm.currentTargetHex;
        ftpAvailable = ftp.isAvailable;
        ftpIsActivated = ftp.isActivated;
        ftpIsAwaitingTargetSelection = ftp.isAwaitingTargetSelection;
        ftpIsWaitingforAttackerSelection = ftp.isWaitingForAttackerSelection;
        ftpIsWaitingforDefenderSelection = ftp.isWaitingForDefenderSelection;
        ftpIsWaitingforDiceRoll = ftp.isWaitingForDiceRoll;
        ftpCurrentTargetHex = ftp.currentTargetHex;
        hpmAvailable = hpm.isAvailable;
        hpmIsActivated = hpm.isActivated;
        hpmIsAwaitingTargetSelection = hpm.isWaitingForConfirmation;
        hpmIsWaitingforAttackerSelection = hpm.isWaitingForAttackerSelection;
        hpmIsWaitingforDefenderSelection = hpm.isWaitingForDefenderSelection;
        hpmCurrentTargetHex = hpm.currentTargetHex;
        hpmIsWaitingForAccuracyRoll = hpm.isWaitingForAccuracyRoll;
        hpmIsWaitingForDirectionRoll = hpm.isWaitingForDirectionRoll;
        hpmIsWaitingForDistanceRoll = hpm.isWaitingForDistanceRoll;
        lbmAvailable = lbm.isAvailable;
        lbmIsActivated = lbm.isActivated;
        lbmIsWaitingForAccuracyRoll = lbm.isWaitingForAccuracyRoll;
        lbmIsWaitingForDirectionRoll = lbm.isWaitingForDirectionRoll;
        lbmIsWaitingForDistanceRoll = lbm.isWaitingForDistanceRoll;
        lbmIsAwaitingTargetSelection = lbm.isAwaitingTargetSelection;
        lbmCurrentTargetHex = lbm.currentTargetHex;
        lbmIsWaitingForDefLBMove = lbm.isWaitingForDefLBMove;
        looseIsActivated = loose.isActivated;
        looseIsWaitingForDirectionRoll = loose.isWaitingForDirectionRoll;
        looseIsWaitingForDistanceRoll = loose.isWaitingForDistanceRoll;
        looseIsWaitingForInterceptionRoll = loose.isWaitingForInterceptionRoll;
    }

    public bool IsEqualTo(GameStatusSnapshot other, out string reason, HashSet<string> excludeFields = null)
    {
        List<string> mismatches = new List<string>();

        if (excludeFields?.Contains("gbmAvailable") != true && gbmAvailable != other.gbmAvailable)
        {
            mismatches.Add($"GroundBallManager.isAvailable mismatch: {gbmAvailable} vs {other.gbmAvailable}");
        }

        if (excludeFields?.Contains("gbmisActivated") != true && gbmisActivated != other.gbmisActivated)
        {
            mismatches.Add($"GroundBallManager.isActivated mismatch: {gbmisActivated} vs {other.gbmisActivated}");
        }

        if (excludeFields?.Contains("gbmIsAwaitingTargetSelection") != true && gbmIsAwaitingTargetSelection != other.gbmIsAwaitingTargetSelection)
        {
            mismatches.Add($"GroundBallManager.isAwaitingTargetSelection mismatch: {gbmIsAwaitingTargetSelection} vs {other.gbmIsAwaitingTargetSelection}");
        }

        if (excludeFields?.Contains("gbmIsWaitingForDiceRoll") != true && gbmIsWaitingForDiceRoll != other.gbmIsWaitingForDiceRoll)
        {
            mismatches.Add($"GroundBallManager.isWaitingForDiceRoll mismatch: {gbmIsWaitingForDiceRoll} vs {other.gbmIsWaitingForDiceRoll}");
        }

        if (excludeFields?.Contains("gbmCurrentTargetHex") != true && gbmCurrentTargetHex != other.gbmCurrentTargetHex)
        {
            mismatches.Add($"GroundBallManager.currentTargetHex mismatch: {gbmCurrentTargetHex?.name} vs {other.gbmCurrentTargetHex?.name}");
        }

        if (excludeFields?.Contains("ftpAvailable") != true && ftpAvailable != other.ftpAvailable)
        {
            mismatches.Add($"FirstTimePassManager.isAvailable mismatch: {ftpAvailable} vs {other.ftpAvailable}");
        }

        if (excludeFields?.Contains("ftpIsActivated") != true && ftpIsActivated != other.ftpIsActivated)
        {
            mismatches.Add($"FirstTimePassManager.isActivated mismatch: {ftpIsActivated} vs {other.ftpIsActivated}");
        }

        if (excludeFields?.Contains("ftpIsAwaitingTargetSelection") != true && ftpIsAwaitingTargetSelection != other.ftpIsAwaitingTargetSelection)
        {
            mismatches.Add($"FirstTimePassManager.isAwaitingTargetSelection mismatch: {ftpIsAwaitingTargetSelection} vs {other.ftpIsAwaitingTargetSelection}");
        }

        if (excludeFields?.Contains("ftpIsWaitingforAttackerSelection") != true && ftpIsWaitingforAttackerSelection != other.ftpIsWaitingforAttackerSelection)
        {
            mismatches.Add($"FirstTimePassManager.isWaitingforAttackerSelection mismatch: {ftpIsWaitingforAttackerSelection} vs {other.ftpIsWaitingforAttackerSelection}");
        }

        if (excludeFields?.Contains("ftpIsWaitingforDefenderSelection") != true && ftpIsWaitingforDefenderSelection != other.ftpIsWaitingforDefenderSelection)
        {
            mismatches.Add($"FirstTimePassManager.isWaitingforDefenderSelection mismatch: {ftpIsWaitingforDefenderSelection} vs {other.ftpIsWaitingforDefenderSelection}");
        }

        if (excludeFields?.Contains("ftpIsWaitingforDiceRoll") != true && ftpIsWaitingforDiceRoll != other.ftpIsWaitingforDiceRoll)
        {
            mismatches.Add($"FirstTimePassManager.isWaitingforDiceRoll mismatch: {ftpIsWaitingforDiceRoll} vs {other.ftpIsWaitingforDiceRoll}");
        }

        if (excludeFields?.Contains("ftpCurrentTargetHex") != true && ftpCurrentTargetHex != other.ftpCurrentTargetHex)
        {
            mismatches.Add($"FirstTimePassManager.currentTargetHex mismatch: {ftpCurrentTargetHex?.name} vs {other.ftpCurrentTargetHex?.name}");
        }
        
        if (excludeFields?.Contains("hpmAvailable") != true && hpmAvailable != other.hpmAvailable)
        {
            mismatches.Add($"HighPassManager.isAvailable mismatch: {hpmAvailable} vs {other.hpmAvailable}");
        }
        
        if (excludeFields?.Contains("hpmIsActivated") != true && hpmIsActivated != other.hpmIsActivated)
        {
            mismatches.Add($"HighPassManager.isActivated mismatch: {hpmIsActivated} vs {other.hpmIsActivated}");
        }
        
        if (excludeFields?.Contains("hpmIsAwaitingTargetSelection") != true && hpmIsAwaitingTargetSelection != other.hpmIsAwaitingTargetSelection)
        {
            mismatches.Add($"HighPassManager.IsAwaitingTargetSelection mismatch: {hpmIsAwaitingTargetSelection} vs {other.hpmIsAwaitingTargetSelection}");
        }
        
        if (excludeFields?.Contains("hpmIsWaitingforAttackerSelection") != true && hpmIsWaitingforAttackerSelection != other.hpmIsWaitingforAttackerSelection)
        {
            mismatches.Add($"HighPassManager.IsWaitingforAttackerSelection mismatch: {hpmIsWaitingforAttackerSelection} vs {other.hpmIsWaitingforAttackerSelection}");
        }
        
        if (excludeFields?.Contains("hpmIsWaitingforDefenderSelection") != true && hpmIsWaitingforDefenderSelection != other.hpmIsWaitingforDefenderSelection)
        {
            mismatches.Add($"HighPassManager.IsWaitingforDefenderSelection mismatch: {hpmIsWaitingforDefenderSelection} vs {other.hpmIsWaitingforDefenderSelection}");
        }

        if (excludeFields?.Contains("hpmCurrentTargetHex") != true && hpmCurrentTargetHex != other.hpmCurrentTargetHex)
        {
            mismatches.Add($"HighPassManager.currentTargetHex mismatch: {hpmCurrentTargetHex?.name} vs {other.hpmCurrentTargetHex?.name}");
        }

        if (excludeFields?.Contains("hpmIsWaitingForAccuracyRoll") != true && hpmIsWaitingForAccuracyRoll != other.hpmIsWaitingForAccuracyRoll)
        {
            mismatches.Add($"HighPassManager.isWaitingForAccuracyRoll mismatch: {hpmIsWaitingForAccuracyRoll} vs {other.hpmIsWaitingForAccuracyRoll}");
        }
        
        if (excludeFields?.Contains("hpmIsWaitingForDirectionRoll") != true && hpmIsWaitingForDirectionRoll != other.hpmIsWaitingForDirectionRoll)
        {
            mismatches.Add($"HighPassManager.IsWaitingForDirectionRoll mismatch: {hpmIsWaitingForDirectionRoll} vs {other.hpmIsWaitingForDirectionRoll}");
        }
        
        if (excludeFields?.Contains("hpmIsWaitingForDistanceRoll") != true && hpmIsWaitingForDistanceRoll != other.hpmIsWaitingForDistanceRoll)
        {
            mismatches.Add($"HighPassManager.IsWaitingForDistanceRoll mismatch: {hpmIsWaitingForDistanceRoll} vs {other.hpmIsWaitingForDistanceRoll}");
        }
        
        if (excludeFields?.Contains("lbmAvailable") != true && lbmAvailable != other.lbmAvailable)
        {
            mismatches.Add($"LongBallManager.isAvailable mismatch: {lbmAvailable} vs {other.lbmAvailable}");
        }
        
        if (excludeFields?.Contains("lbmIsActivated") != true && lbmIsActivated != other.lbmIsActivated)
        {
            mismatches.Add($"LongBallManager.isActivated mismatch: {lbmIsActivated} vs {other.lbmIsActivated}");
        }
        
        if (excludeFields?.Contains("lbmIsWaitingForAccuracyRoll") != true && lbmIsWaitingForAccuracyRoll != other.lbmIsWaitingForAccuracyRoll)
        {
            mismatches.Add($"LongBallManager.isWaitingForAccuracyRoll mismatch: {lbmIsWaitingForAccuracyRoll} vs {other.lbmIsWaitingForAccuracyRoll}");
        }
        
        if (excludeFields?.Contains("lbmIsWaitingForDirectionRoll") != true && lbmIsWaitingForDirectionRoll != other.lbmIsWaitingForDirectionRoll)
        {
            mismatches.Add($"LongBallManager.IsWaitingForDirectionRoll mismatch: {lbmIsWaitingForDirectionRoll} vs {other.lbmIsWaitingForDirectionRoll}");
        }
        
        if (excludeFields?.Contains("lbmIsWaitingForDistanceRoll") != true && lbmIsWaitingForDistanceRoll != other.lbmIsWaitingForDistanceRoll)
        {
            mismatches.Add($"LongBallManager.IsWaitingForDistanceRoll mismatch: {lbmIsWaitingForDistanceRoll} vs {other.lbmIsWaitingForDistanceRoll}");
        }
        
        if (excludeFields?.Contains("lbmIsAwaitingTargetSelection") != true && lbmIsAwaitingTargetSelection != other.lbmIsAwaitingTargetSelection)
        {
            mismatches.Add($"LongBallManager.isAwaitingTargetSelection mismatch: {lbmIsAwaitingTargetSelection} vs {other.lbmIsAwaitingTargetSelection}");
        }
        
        if (excludeFields?.Contains("lbmIsWaitingForDefLBMove") != true && lbmIsWaitingForDefLBMove != other.lbmIsWaitingForDefLBMove)
        {
            mismatches.Add($"LongBallManager.IsWaitingForDefLBMove mismatch: {lbmIsWaitingForDefLBMove} vs {other.lbmIsWaitingForDefLBMove}");
        }
        
        if (excludeFields?.Contains("lbmCurrentTargetHex") != true && lbmCurrentTargetHex != other.lbmCurrentTargetHex)
        {
            mismatches.Add($"LongBallManager.currentTargetHex mismatch: {lbmCurrentTargetHex?.name} vs {other.lbmCurrentTargetHex?.name}");
        }
        if (excludeFields?.Contains("looseIsActivated") != true && looseIsActivated != other.looseIsActivated)
        {
            mismatches.Add($"LooseBallManager.IsActivated mismatch: {looseIsActivated} vs {other.looseIsActivated}");
        }
        if (excludeFields?.Contains("looseIsWaitingForDirectionRoll") != true && looseIsWaitingForDirectionRoll != other.looseIsWaitingForDirectionRoll)
        {
            mismatches.Add($"LooseBallManager.IsWaitingForDirectionRoll mismatch: {looseIsWaitingForDirectionRoll} vs {other.looseIsWaitingForDirectionRoll}");
        }
        if (excludeFields?.Contains("looseIsWaitingForDistanceRoll") != true && looseIsWaitingForDistanceRoll != other.looseIsWaitingForDistanceRoll)
        {
            mismatches.Add($"LooseBallManager.IsWaitingForDistanceRoll mismatch: {looseIsWaitingForDistanceRoll} vs {other.looseIsWaitingForDistanceRoll}");
        }
        if (excludeFields?.Contains("looseIsWaitingForInterceptionRoll") != true && looseIsWaitingForInterceptionRoll != other.looseIsWaitingForInterceptionRoll)
        {
            mismatches.Add($"LooseBallManager.IsWaitingForInterceptionRoll mismatch: {looseIsWaitingForInterceptionRoll} vs {other.looseIsWaitingForInterceptionRoll}");
        }
        
        if (mismatches.Count > 0)
        {
            reason = string.Join("\n", mismatches);
            return false;
        }

        reason = null;
        return true;
    }
}

public class GameTestScenarioRunner : MonoBehaviour
{
    private readonly struct ScenarioDefinition
    {
        public ScenarioDefinition(string name, Func<IEnumerator> run)
        {
            Name = name;
            Run = run;
        }

        public string Name { get; }
        public Func<IEnumerator> Run { get; }
    }

    private sealed class FtpInterceptionMoveSetup
    {
        public HexCell targetHex;
        public HexCell attackerHex;
        public PlayerToken attackerToken;
        public HexCell attackerDestinationHex;
        public HexCell defenderHex;
        public PlayerToken defenderToken;
        public HexCell defenderDestinationHex;
        public bool isBlockingPath;
    }

    private sealed class FtpBlockedHoverSetup
    {
        public HexCell targetHex;
        public PlayerToken defenderToken;
        public HexCell originalDefenderHex;
        public HexCell blockingHex;
    }

    // private bool shouldRunTests = false;
    private bool shouldRunTests = true;
    private string logFilePath;
    private bool testFailed = false;
    private static int currentTestIndex = 0;
    private static bool hasInitializedLogFile = false;
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
    public static GameTestScenarioRunner Instance;
    private GameStatusSnapshot savedSnapshot;
    private Canvas testLogCanvas;
    private TextMeshProUGUI testLogText;
    private readonly Queue<string> onScreenLogLines = new();
    private const int MaxOnScreenLogLines = 15;
    private string currentScenarioName = string.Empty;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("⚠️ Duplicate GameTestScenarioRunner detected. Destroying the new one.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureOnScreenLogOverlay();
    }

    private void Start()
    {
        StartTesting();
    }

    private void OnDestroy()
    {
        DetachTestInputLogging();
    }

    private void EnsureOnScreenLogOverlay()
    {
        if (testLogCanvas != null && testLogText != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject("GameTestLogOverlay");
        DontDestroyOnLoad(canvasObject);

        testLogCanvas = canvasObject.AddComponent<Canvas>();
        testLogCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        testLogCanvas.sortingOrder = 5000;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 1f;

        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject panelObject = new GameObject("Panel");
        panelObject.transform.SetParent(canvasObject.transform, false);
        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.7f);

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(20f, -450f);
        panelRect.sizeDelta = new Vector2(780f, 320f);

        GameObject textObject = new GameObject("Text");
        textObject.transform.SetParent(panelObject.transform, false);
        testLogText = textObject.AddComponent<TextMeshProUGUI>();
        testLogText.font = ResolveOverlayFontAsset();
        testLogText.fontSize = 20f;
        testLogText.enableWordWrapping = true;
        testLogText.overflowMode = TextOverflowModes.Overflow;
        testLogText.color = Color.white;
        testLogText.alignment = TextAlignmentOptions.TopLeft;
        testLogText.richText = false;
        testLogText.text = "Test log overlay ready.";

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(12f, 12f);
        textRect.offsetMax = new Vector2(-12f, -12f);
    }

    private static TMP_FontAsset ResolveOverlayFontAsset()
    {
        if (TMP_Settings.defaultFontAsset != null)
        {
            return TMP_Settings.defaultFontAsset;
        }

        TMP_Text anySceneText = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(text => text != null && text.font != null);
        if (anySceneText != null)
        {
            return anySceneText.font;
        }

        return null;
    }

    private void AppendOnScreenLog(string message)
    {
        EnsureOnScreenLogOverlay();

        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        foreach (string rawLine in message.Split('\n'))
        {
            string line = rawLine.TrimEnd('\r');
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            line = SanitizeForOnScreenLog(line);
            onScreenLogLines.Enqueue(line);
        }

        while (onScreenLogLines.Count > MaxOnScreenLogLines)
        {
            onScreenLogLines.Dequeue();
        }

        testLogText.text = string.Join("\n", onScreenLogLines);
    }

    private static string SanitizeForOnScreenLog(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return string.Empty;
        }

        string sanitized = message
            .Replace("▶️", "> ")
            .Replace("▶", "> ")
            .Replace("✅", "[PASS]")
            .Replace("❌", "[FAIL]")
            .Replace("⚠️", "[WARN]")
            .Replace("⚠", "[WARN]")
            .Replace("🎉", string.Empty);

        StringBuilder asciiOnly = new StringBuilder(sanitized.Length);
        foreach (char c in sanitized)
        {
            asciiOnly.Append(c <= 127 ? c : ' ');
        }

        return asciiOnly.ToString();
    }

    private void StartTesting()
    {
        if (shouldRunTests)
        {
            logFilePath = Path.Combine(Application.dataPath, "TestResults.txt");
            if (!hasInitializedLogFile)
            {
                File.WriteAllText(logFilePath, "===== TEST LOG START =====\n");
                hasInitializedLogFile = true;
            }
            else
            {
                File.AppendAllText(logFilePath, $"\n\n===== CONTINUING TESTS at {System.DateTime.Now} =====\n");
            }
            StartCoroutine(RunAllScenarios());
        }
        else
        {
            Debug.Log("Tests are disabled. Set shouldRunTests to true to enable.");
            return;
        }
    }

    private GameStatusSnapshot SaveGameStatusSnapshot()
    {
        savedSnapshot = new GameStatusSnapshot(
              groundBallManager
            , firstTimePassManager
            , highPassManager
            , longBallManager
            , movementPhaseManager
            , finalThirdManager
            , headerManager
            , looseBallManager
            , outOfBoundsManager
            , freeKickManager
            , shotManager
            , goalKeeperManager
            , goalFlowManager
            , kickoffManager
            , hexgrid
            , gameInputManager
        );
        Log("📸 Game state snapshot saved");
        return savedSnapshot;
    }

    private GameStatusSnapshot GetCurrentSnapshot()
    {
        return new GameStatusSnapshot(
            groundBallManager,
            firstTimePassManager,
            highPassManager,
            longBallManager,
            movementPhaseManager,
            finalThirdManager,
            headerManager,
            looseBallManager,
            outOfBoundsManager,
            freeKickManager,
            shotManager,
            goalKeeperManager,
            goalFlowManager,
            kickoffManager,
            hexgrid,
            gameInputManager
        );
    }

    private IEnumerator RunAllScenarios()
    {
        var scenarios = new List<ScenarioDefinition>();
        bool runManualStatsPreviewOnly = false;
        bool runFtpAuditOnly = false;
        bool runLongBallAuditOnly = false;
        bool runFromCurrentFailureOnly = false;

        if (runManualStatsPreviewOnly)
        {
            scenarios.Add(new ScenarioDefinition(nameof(Scenario_001_Stats_UI_Preview), Scenario_001_Stats_UI_Preview));
        }
        else if (runFtpAuditOnly)
        {
            scenarios.AddRange(new[]
            {
                new ScenarioDefinition(nameof(Scenario_005_GroundBall_0004_Pass_to_Player_FTP_No_interceptions), Scenario_005_GroundBall_0004_Pass_to_Player_FTP_No_interceptions),
                new ScenarioDefinition(nameof(Scenario_006_GroundBall_0005_Pass_to_Player_FTP_To_Player), Scenario_006_GroundBall_0005_Pass_to_Player_FTP_To_Player),
                new ScenarioDefinition(nameof(Scenario_007_GroundBall_0006_Swith_between_options_before_Committing), Scenario_007_GroundBall_0006_Swith_between_options_before_Committing),
                new ScenarioDefinition(nameof(Scenario_007a_FirstTimePass_Difficulty1_Hover_Preview_And_Commitment), Scenario_007a_FirstTimePass_Difficulty1_Hover_Preview_And_Commitment),
                new ScenarioDefinition(nameof(Scenario_007b_FirstTimePass_Difficulty3_Commits_On_F_And_First_Click), Scenario_007b_FirstTimePass_Difficulty3_Commits_On_F_And_First_Click),
                new ScenarioDefinition(nameof(Scenario_007c_FirstTimePass_Defender_Path_Block_Intercepts_On_5), Scenario_007c_FirstTimePass_Defender_Path_Block_Intercepts_On_5),
                new ScenarioDefinition(nameof(Scenario_007d_FirstTimePass_Defender_ZOI_Recalculation_Intercepts_On_6), Scenario_007d_FirstTimePass_Defender_ZOI_Recalculation_Intercepts_On_6),
                new ScenarioDefinition(nameof(Scenario_007e_FirstTimePass_Passer_Cannot_Reclaim_FTP_To_Space), Scenario_007e_FirstTimePass_Passer_Cannot_Reclaim_FTP_To_Space),
            });
        }
        else if (runLongBallAuditOnly)
        {
            scenarios.AddRange(new[]
            {
                new ScenarioDefinition(nameof(Scenario_030a_LongBall_Difficulty1_InvalidTarget_And_AccurateThreshold), Scenario_030a_LongBall_Difficulty1_InvalidTarget_And_AccurateThreshold),
                new ScenarioDefinition(nameof(Scenario_030b_LongBall_Difficulty3_Commits_On_First_Click), Scenario_030b_LongBall_Difficulty3_Commits_On_First_Click),
                new ScenarioDefinition(nameof(Scenario_030c_LongBall_Inaccurate_No_Interception_GK_Forfeit_AutoMovement), Scenario_030c_LongBall_Inaccurate_No_Interception_GK_Forfeit_AutoMovement),
                new ScenarioDefinition(nameof(Scenario_030d_LongBall_Inaccurate_Delgado_Interception_Success_Broadcasts_AnyOtherScenario), Scenario_030d_LongBall_Inaccurate_Delgado_Interception_Success_Broadcasts_AnyOtherScenario),
                new ScenarioDefinition(nameof(Scenario_030e_LongBall_Inaccurate_Delgado_Interception_Fails_GK_Forfeit_AutoMovement), Scenario_030e_LongBall_Inaccurate_Delgado_Interception_Fails_GK_Forfeit_AutoMovement),
                new ScenarioDefinition(nameof(Scenario_030f_LongBall_Inaccurate_Lands_On_Delgado_Broadcasts_AnyOtherScenario), Scenario_030f_LongBall_Inaccurate_Lands_On_Delgado_Broadcasts_AnyOtherScenario),
                new ScenarioDefinition(nameof(Scenario_031a_LongBall_OppositeF3_Inaccurate_On_Yaneva_Offers_Snapshot_Or_Movement), Scenario_031a_LongBall_OppositeF3_Inaccurate_On_Yaneva_Offers_Snapshot_Or_Movement),
                new ScenarioDefinition(nameof(Scenario_031b_LongBall_Box_GK_Free_Move_Then_Kuzmic_Recovery_Broadcasts_AnyOtherScenario), Scenario_031b_LongBall_Box_GK_Free_Move_Then_Kuzmic_Recovery_Broadcasts_AnyOtherScenario),
                new ScenarioDefinition(nameof(Scenario_031c_LongBall_Box_Poulsen_Interception_Fails_GK_Forfeit_EndOfLongBall), Scenario_031c_LongBall_Box_Poulsen_Interception_Fails_GK_Forfeit_EndOfLongBall),
                new ScenarioDefinition(nameof(Scenario_031d_LongBall_CornerTarget_Inaccurate_NorthEast3_Is_GoalKick), Scenario_031d_LongBall_CornerTarget_Inaccurate_NorthEast3_Is_GoalKick),
                new ScenarioDefinition(nameof(Scenario_031e_LongBall_CornerTarget_Inaccurate_South3_Is_ThrowIn), Scenario_031e_LongBall_CornerTarget_Inaccurate_South3_Is_ThrowIn),
                new ScenarioDefinition(nameof(Scenario_031f_LongBall_To_15_4_Inaccurate_SouthEast6_Is_GoalKick_Not_Goal), Scenario_031f_LongBall_To_15_4_Inaccurate_SouthEast6_Is_GoalKick_Not_Goal),
            });
        }
        else
        {
            scenarios.AddRange(runFromCurrentFailureOnly ? new[]
            {
            new ScenarioDefinition(nameof(Scenario_020_Movement_Phase_Check_Tackle_loose_interception_missed_hit_attacker_new_tackle_throw_in), Scenario_020_Movement_Phase_Check_Tackle_loose_interception_missed_hit_attacker_new_tackle_throw_in),
            new ScenarioDefinition(nameof(Scenario_021_Movement_Phase_PickUp_continue_move_looseball_two_missed_interceptions), Scenario_021_Movement_Phase_PickUp_continue_move_looseball_two_missed_interceptions),
            new ScenarioDefinition(nameof(Scenario_022_Movement_Phase_Loose_ball_gets_in_pen_box_check_keeper_move), Scenario_022_Movement_Phase_Loose_ball_gets_in_pen_box_check_keeper_move),
            new ScenarioDefinition(nameof(Scenario_023_Movement_Phase_DriblingBox_TackleLoose_ball_on_attacker_NO_Snapshot_end_MP), Scenario_023_Movement_Phase_DriblingBox_TackleLoose_ball_on_attacker_NO_Snapshot_end_MP),
            new ScenarioDefinition(nameof(Scenario_024_Movement_Phase_DriblingBox_Nutmeg_Loose_ball_on_attacker_Snapshot_goal), Scenario_024_Movement_Phase_DriblingBox_Nutmeg_Loose_ball_on_attacker_Snapshot_goal),
            new ScenarioDefinition(nameof(Scenario_024b_Movement_Phase_DriblingBox_Nutmeg_Loose_ball_on_attacker_No_Snapshot_end_MP_SHOT_GOAL), Scenario_024b_Movement_Phase_DriblingBox_Nutmeg_Loose_ball_on_attacker_No_Snapshot_end_MP_SHOT_GOAL),
            new ScenarioDefinition(nameof(Scenario_025a_Movement_Phase_Dribling_into_goal), Scenario_025a_Movement_Phase_Dribling_into_goal),
            new ScenarioDefinition(nameof(Scenario_025b_Movement_Phase_Reposition_into_goal), Scenario_025b_Movement_Phase_Reposition_into_goal),
            } : new[]
            {
            // Ground Ball regression suite
            new ScenarioDefinition(nameof(Scenario_002_GroundBall_0001_Commitment), Scenario_002_GroundBall_0001_Commitment),
            new ScenarioDefinition(nameof(Scenario_002b_GroundBall_0001b_QuickThrow_Commitment), Scenario_002b_GroundBall_0001b_QuickThrow_Commitment),
            new ScenarioDefinition(nameof(Scenario_003_GroundBall_0002_Dangerous_pass_no_interception), Scenario_003_GroundBall_0002_Dangerous_pass_no_interception),
            new ScenarioDefinition(nameof(Scenario_004_GroundBall_0003_Dangerous_pass_intercepted_by_second_interceptor), Scenario_004_GroundBall_0003_Dangerous_pass_intercepted_by_second_interceptor),
            new ScenarioDefinition(nameof(Scenario_005_GroundBall_0004_Pass_to_Player_FTP_No_interceptions), Scenario_005_GroundBall_0004_Pass_to_Player_FTP_No_interceptions),
            new ScenarioDefinition(nameof(Scenario_006_GroundBall_0005_Pass_to_Player_FTP_To_Player), Scenario_006_GroundBall_0005_Pass_to_Player_FTP_To_Player),
            new ScenarioDefinition(nameof(Scenario_007_GroundBall_0006_Swith_between_options_before_Committing), Scenario_007_GroundBall_0006_Swith_between_options_before_Committing),
            new ScenarioDefinition(nameof(Scenario_007a_FirstTimePass_Difficulty1_Hover_Preview_And_Commitment), Scenario_007a_FirstTimePass_Difficulty1_Hover_Preview_And_Commitment),
            new ScenarioDefinition(nameof(Scenario_007b_FirstTimePass_Difficulty3_Commits_On_F_And_First_Click), Scenario_007b_FirstTimePass_Difficulty3_Commits_On_F_And_First_Click),
            new ScenarioDefinition(nameof(Scenario_007c_FirstTimePass_Defender_Path_Block_Intercepts_On_5), Scenario_007c_FirstTimePass_Defender_Path_Block_Intercepts_On_5),
            new ScenarioDefinition(nameof(Scenario_007d_FirstTimePass_Defender_ZOI_Recalculation_Intercepts_On_6), Scenario_007d_FirstTimePass_Defender_ZOI_Recalculation_Intercepts_On_6),
            new ScenarioDefinition(nameof(Scenario_007e_FirstTimePass_Passer_Cannot_Reclaim_FTP_To_Space), Scenario_007e_FirstTimePass_Passer_Cannot_Reclaim_FTP_To_Space),
            new ScenarioDefinition(nameof(Scenario_008_Stupid_Click_and_KeyPress_do_not_change_status), Scenario_008_Stupid_Click_and_KeyPress_do_not_change_status),
            new ScenarioDefinition(nameof(Scenario_008b_Movement_Phase_Reset_When_Switching_Action_Before_Commit), Scenario_008b_Movement_Phase_Reset_When_Switching_Action_Before_Commit),
            new ScenarioDefinition(nameof(Scenario_009_Movement_Phase_NO_interceptions_No_tackles), Scenario_009_Movement_Phase_NO_interceptions_No_tackles),
            new ScenarioDefinition(nameof(Scenario_010_Movement_Phase_failed_interceptions_No_tackles), Scenario_010_Movement_Phase_failed_interceptions_No_tackles),
            new ScenarioDefinition(nameof(Scenario_011_Movement_Phase_Successful_Interception), Scenario_011_Movement_Phase_Successful_Interception),
            new ScenarioDefinition("Scenario_012_Movement_Phase_interception_Foul_take_foul(false, false)", () => Scenario_012_Movement_Phase_interception_Foul_take_foul(false, false)),
            new ScenarioDefinition("Scenario_012_Movement_Phase_interception_Foul_take_foul(true, false)", () => Scenario_012_Movement_Phase_interception_Foul_take_foul(true, false)),
            new ScenarioDefinition("Scenario_012_Movement_Phase_interception_Foul_take_foul(false, true)", () => Scenario_012_Movement_Phase_interception_Foul_take_foul(false, true)),
            new ScenarioDefinition("Scenario_012_Movement_Phase_interception_Foul_take_foul(true, true)", () => Scenario_012_Movement_Phase_interception_Foul_take_foul(true, true)),
            new ScenarioDefinition(nameof(Scenario_013_Movement_Phase_interception_Foul_Play_on), Scenario_013_Movement_Phase_interception_Foul_Play_on),
            new ScenarioDefinition(nameof(Scenario_014_Movement_Phase_Check_reposition_interceptions), Scenario_014_Movement_Phase_Check_reposition_interceptions),
            new ScenarioDefinition(nameof(Scenario_015_Movement_Phase_Check_NutmegWithoutMovement_tackle_Loose_Ball), Scenario_015_Movement_Phase_Check_NutmegWithoutMovement_tackle_Loose_Ball),
            new ScenarioDefinition(nameof(Scenario_016_Movement_Phase_Check_InterceptionFoul_Tackle_Foul_NewTackle_SuccessfulTackle), Scenario_016_Movement_Phase_Check_InterceptionFoul_Tackle_Foul_NewTackle_SuccessfulTackle),
            new ScenarioDefinition(nameof(Scenario_017_Movement_Phase_Check_InterceptionFoul_NutmegLost), Scenario_017_Movement_Phase_Check_InterceptionFoul_NutmegLost),
            new ScenarioDefinition(nameof(Scenario_017b_Movement_Phase_Dribbler_Forfeit_Remaining_Pace), Scenario_017b_Movement_Phase_Dribbler_Forfeit_Remaining_Pace),
            new ScenarioDefinition(nameof(Scenario_017c_Movement_Phase_Mixed_Nutmeggable_And_Stealable_Defenders), Scenario_017c_Movement_Phase_Mixed_Nutmeggable_And_Stealable_Defenders),
            new ScenarioDefinition(nameof(Scenario_017d_Movement_Phase_Multiple_Nutmeggable_Defenders_Reject_Nutmeg), Scenario_017d_Movement_Phase_Multiple_Nutmeggable_Defenders_Reject_Nutmeg),
            new ScenarioDefinition(nameof(Scenario_017e_Movement_Phase_Multiple_Nutmeggable_Defenders_Select_Victim), Scenario_017e_Movement_Phase_Multiple_Nutmeggable_Defenders_Select_Victim),
            new ScenarioDefinition(nameof(Scenario_017e_b_Movement_Phase_Multiple_Nutmeggable_Defenders_Select_Victim_Offers_Other_Steals), Scenario_017e_b_Movement_Phase_Multiple_Nutmeggable_Defenders_Select_Victim_Offers_Other_Steals),
            new ScenarioDefinition(nameof(Scenario_017f_Movement_Phase_Same_Defender_Steals_Once_Per_Section_Per_Dribbler), Scenario_017f_Movement_Phase_Same_Defender_Steals_Once_Per_Section_Per_Dribbler),
            new ScenarioDefinition("Scenario_017g_Movement_Phase_Successful_Tackle_Reposition_Triggers_Other_Attacker_Steal(false)", () => Scenario_017g_Movement_Phase_Successful_Tackle_Reposition_Triggers_Other_Attacker_Steal(false)),
            new ScenarioDefinition("Scenario_017g_Movement_Phase_Successful_Tackle_Reposition_Triggers_Other_Attacker_Steal(true)", () => Scenario_017g_Movement_Phase_Successful_Tackle_Reposition_Triggers_Other_Attacker_Steal(true)),
            new ScenarioDefinition(nameof(Scenario_018_Movement_Phase_Check_Tackle_loose_interception), Scenario_018_Movement_Phase_Check_Tackle_loose_interception),
            new ScenarioDefinition(nameof(Scenario_019_Movement_Phase_Check_Tackle_loose_interception_missed_hit_defender), Scenario_019_Movement_Phase_Check_Tackle_loose_interception_missed_hit_defender),
            new ScenarioDefinition(nameof(Scenario_020_Movement_Phase_Check_Tackle_loose_interception_missed_hit_attacker_new_tackle_throw_in), Scenario_020_Movement_Phase_Check_Tackle_loose_interception_missed_hit_attacker_new_tackle_throw_in),
            new ScenarioDefinition(nameof(Scenario_021_Movement_Phase_PickUp_continue_move_looseball_two_missed_interceptions), Scenario_021_Movement_Phase_PickUp_continue_move_looseball_two_missed_interceptions),
            new ScenarioDefinition(nameof(Scenario_022_Movement_Phase_Loose_ball_gets_in_pen_box_check_keeper_move), Scenario_022_Movement_Phase_Loose_ball_gets_in_pen_box_check_keeper_move),
            new ScenarioDefinition(nameof(Scenario_023_Movement_Phase_DriblingBox_TackleLoose_ball_on_attacker_NO_Snapshot_end_MP), Scenario_023_Movement_Phase_DriblingBox_TackleLoose_ball_on_attacker_NO_Snapshot_end_MP),
            new ScenarioDefinition(nameof(Scenario_024_Movement_Phase_DriblingBox_Nutmeg_Loose_ball_on_attacker_Snapshot_goal), Scenario_024_Movement_Phase_DriblingBox_Nutmeg_Loose_ball_on_attacker_Snapshot_goal),
            new ScenarioDefinition(nameof(Scenario_024b_Movement_Phase_DriblingBox_Nutmeg_Loose_ball_on_attacker_No_Snapshot_end_MP_SHOT_GOAL), Scenario_024b_Movement_Phase_DriblingBox_Nutmeg_Loose_ball_on_attacker_No_Snapshot_end_MP_SHOT_GOAL),
            new ScenarioDefinition(nameof(Scenario_025a_Movement_Phase_Dribling_into_goal), Scenario_025a_Movement_Phase_Dribling_into_goal),
            new ScenarioDefinition(nameof(Scenario_025b_Movement_Phase_Reposition_into_goal), Scenario_025b_Movement_Phase_Reposition_into_goal),
            new ScenarioDefinition(nameof(Scenario_030a_LongBall_Difficulty1_InvalidTarget_And_AccurateThreshold), Scenario_030a_LongBall_Difficulty1_InvalidTarget_And_AccurateThreshold),
            new ScenarioDefinition(nameof(Scenario_030b_LongBall_Difficulty3_Commits_On_First_Click), Scenario_030b_LongBall_Difficulty3_Commits_On_First_Click),
            new ScenarioDefinition(nameof(Scenario_030c_LongBall_Inaccurate_No_Interception_GK_Forfeit_AutoMovement), Scenario_030c_LongBall_Inaccurate_No_Interception_GK_Forfeit_AutoMovement),
            new ScenarioDefinition(nameof(Scenario_030d_LongBall_Inaccurate_Delgado_Interception_Success_Broadcasts_AnyOtherScenario), Scenario_030d_LongBall_Inaccurate_Delgado_Interception_Success_Broadcasts_AnyOtherScenario),
            new ScenarioDefinition(nameof(Scenario_030e_LongBall_Inaccurate_Delgado_Interception_Fails_GK_Forfeit_AutoMovement), Scenario_030e_LongBall_Inaccurate_Delgado_Interception_Fails_GK_Forfeit_AutoMovement),
            new ScenarioDefinition(nameof(Scenario_030f_LongBall_Inaccurate_Lands_On_Delgado_Broadcasts_AnyOtherScenario), Scenario_030f_LongBall_Inaccurate_Lands_On_Delgado_Broadcasts_AnyOtherScenario),
            new ScenarioDefinition(nameof(Scenario_031a_LongBall_OppositeF3_Inaccurate_On_Yaneva_Offers_Snapshot_Or_Movement), Scenario_031a_LongBall_OppositeF3_Inaccurate_On_Yaneva_Offers_Snapshot_Or_Movement),
            new ScenarioDefinition(nameof(Scenario_031b_LongBall_Box_GK_Free_Move_Then_Kuzmic_Recovery_Broadcasts_AnyOtherScenario), Scenario_031b_LongBall_Box_GK_Free_Move_Then_Kuzmic_Recovery_Broadcasts_AnyOtherScenario),
            new ScenarioDefinition(nameof(Scenario_031c_LongBall_Box_Poulsen_Interception_Fails_GK_Forfeit_EndOfLongBall), Scenario_031c_LongBall_Box_Poulsen_Interception_Fails_GK_Forfeit_EndOfLongBall),
            new ScenarioDefinition(nameof(Scenario_031d_LongBall_CornerTarget_Inaccurate_NorthEast3_Is_GoalKick), Scenario_031d_LongBall_CornerTarget_Inaccurate_NorthEast3_Is_GoalKick),
            new ScenarioDefinition(nameof(Scenario_031e_LongBall_CornerTarget_Inaccurate_South3_Is_ThrowIn), Scenario_031e_LongBall_CornerTarget_Inaccurate_South3_Is_ThrowIn),
            new ScenarioDefinition(nameof(Scenario_031f_LongBall_To_15_4_Inaccurate_SouthEast6_Is_GoalKick_Not_Goal), Scenario_031f_LongBall_To_15_4_Inaccurate_SouthEast6_Is_GoalKick_Not_Goal),
              
            // // // // // // Scenario_026_HighPass_onAttacker_MoveAtt_moveDef_AccurateHP(),
            // Scenario_027_HighPass_on_Attacker_MoveAtt_moveDef_Accurate_HP_BC(),
            // // // // // // Scenario_027_HighPass_onAttacker_MoveAtt_moveDef_INAccurateHP(),
            // Scenario_027a_Decide_on_attWillJump(),
            // // // // Scenario_027_a_b_HP_on_1att_def_Not_challenging(),
            // Scenario_027_a_c_HP_on_1att_def_Not_challenging_att_head(),
            // Scenario_027_a_c_HP_on_1att_def_Not_challenging_att_head(true),
            // Scenario_027_a_d_HP_on_1att_def_Not_challenging_att_BC(),
            // Scenario_027_a_d_HP_on_1att_def_Not_challenging_att_BC(true),
            // // // // // // Scenario_027b_Decide_on_DefWillJump(),
            // Scenario_027c_4PlayerJump_AttackWins(),
            // Scenario_027d_4PlayerJump_Defense_Wins_to_player(),
            // Scenario_027e_4PlayerJump_Defense_Wins_to_space(),
            // // // // // // Scenario_027g_4PlayerJump_LooseBall_From_Stewart_Space(),
            // Scenario_027g_a_4PlayerJump_LooseBall_OnDefender_interception(),
            // Scenario_027g_b_4PlayerJump_LooseBall_OnDefender_NO_interception(),
            // Scenario_027h_4PlayerJump_LooseBall_OnDefender(),
            // Scenario_027i_4PlayerJump_LooseBall_OnAttacker(),
            // Scenario_027j_4PlayerJump_LooseBall_OnJumpedToken(),
            // Scenario_027j_4PlayerJump_LooseBall_OnJumpedToken(true),
            // // // // Scenario_028_Inaccurate_on_Defenders(),
            // Scenario_028a_Defense_Heads(),
            // // // // // // // Scenario_028a_Defense_BCs(),
            // Scenario_028b_a_a_Defense_Ball_Controls_McNulty_fails_INterception(),
            // Scenario_028b_a_a_Defense_Ball_Controls_McNulty_fails_NO_interception(),
            // Scenario_028b_a_Defense_Ball_Controls_McNulty_BC(),
            // // // // // Scenario_029_HeaderAtGoal_prep(true),
            // // // // // Scenario_029_HeaderAtGoal_prep(),
            // Scenario_029_HeaderAtGoal_GOAL(true), // ✅
            // Scenario_029_HesorryaderAtGoal_GOAL(), // ✅
            // Scenario_029_HeaderAtGoal_OFF_TARGET(true), // ✅
            // Scenario_029_HeaderAtGoal_OFF_TARGET(), // ✅
            // // // // // // // Scenario_029_HeaderAtGoal_Saved_by_GK(true),
            // // // // // // // Scenario_029_HeaderAtGoal_Saved_by_GK(),
            // Scenario_029_HeaderAtGoal_Saved_by_GK_QThrow(true), // ✅ // TODO: Check Quickthrow logic
            // Scenario_029_HeaderAtGoal_Saved_by_GK_QThrow(),
            // Scenario_029_HeaderAtGoal_Saved_by_GK_GoalKick(true), // ✅
            // Scenario_029_HeaderAtGoal_Saved_by_GK_GoalKick(),
            // // // // // // Scenario_029_HeaderAtGoal_Saved_by_GK_Corner(true), // Impossible Scenario
            // Scenario_029_HeaderAtGoal_Saved_by_GK_Corner(),
            // Scenario_029_HeaderAtGoal_Saved_by_GK_LooseBall(true), // ✅
            // Scenario_029_HeaderAtGoal_Saved_by_GK_LooseBall(),
            // Scenario_029_HeaderAtGoal_Headed_Away(true),
            // Scenario_029_HeaderAtGoal_Headed_Away(),
            // Scenario_029_HeaderAtGoal_LooseBall(),
            // Scenario_029_HeaderAtGoal_LooseBall_OWN_GOAL(),
            // Add more scenarios here
            });
        }

        for (; currentTestIndex < scenarios.Count; currentTestIndex++)
        {
            testFailed = false;
            ScenarioDefinition scenario = scenarios[currentTestIndex];
            currentScenarioName = scenario.Name;
            LogScenarioBoundary(isStart: true, currentTestIndex + 1, currentScenarioName, status: null);
            // 🔁 Scene switch to Dummy first (full teardown)
            yield return new WaitForSeconds(1f);

            // 🔄 Load Room scene fresh
            SceneManager.LoadScene("Room");

            yield return new WaitForSeconds(1f); // Optional buffer
            LinkRoomSceneComponents();
            yield return StartCoroutine(scenario.Run());

            if (testFailed)
            {
                LogScenarioBoundary(isStart: false, currentTestIndex + 1, currentScenarioName, "FAILED");
                Log("❌ Test failed. Halting suite.");
                yield break; // Stop entire suite on failure
            }

            LogScenarioBoundary(isStart: false, currentTestIndex + 1, currentScenarioName, "PASSED");
            yield return new WaitForSeconds(0.5f); // Short pause between tests
            SceneManager.LoadScene("DummyLoader");
        }
        Log("🎉 ALL TESTS PASSED SUCCESSFULLY!");
    }

    private IEnumerator Scenario_001_Stats_UI_Preview()
    {
        yield return new WaitForSeconds(2f);

        Log("Preparing manual stats UI preview in Room scene.");

        PlayerToken yaneva = RequirePlayerToken("Yaneva");
        PlayerToken baas = RequirePlayerToken("Cafferata");
        PlayerToken yugar = RequirePlayerToken("Kalla");
        PlayerToken delgado = RequirePlayerToken("Delgado");
        PlayerToken soares = RequirePlayerToken("Soares");
        PlayerToken mcNulty = RequirePlayerToken("McNulty");
        PlayerToken kuzmic = RequirePlayerToken("Kuzmic");
        PlayerToken poulsen = RequirePlayerToken("Poulsen");
        if (new[] { yaneva, baas, yugar, delgado, soares, mcNulty, kuzmic, poulsen }.Any(token => token == null))
        {
            yield break;
        }

        MatchManager matchManager = MatchManager.Instance;
        AssertTrue(matchManager != null, "MatchManager should exist for stats preview.");
        AssertTrue(matchManager.gameData != null, "MatchManager gameData should exist for stats preview.");
        if (matchManager == null || matchManager.gameData == null)
        {
            yield break;
        }

        Log("Logging Yaneva: 2 goals, 1 assist, sub off.");
        matchManager.gameData.gameLog.LogEvent(yaneva, MatchManager.ActionType.GoalScored);
        matchManager.gameData.gameLog.LogEvent(yaneva, MatchManager.ActionType.GoalScored);
        matchManager.gameData.gameLog.LogEvent(yaneva, MatchManager.ActionType.AssistProvided);

        Log("Logging Baas: yellow card, injury, sub on, sub off.");
        baas.ReceiveYellowCard();
        baas.ReceiveInjury();
        matchManager.gameData.gameLog.LogEvent(baas, MatchManager.ActionType.YellowCardShown, connectedToken: delgado);
        matchManager.gameData.gameLog.LogEvent(baas, MatchManager.ActionType.Injured, connectedToken: delgado);

        Log("Logging Yugar: sub on.");
        matchManager.RecordSubstitutionEvent(null, baas.playerName);
        matchManager.RecordSubstitutionEvent(baas.playerName, yugar.playerName);
        matchManager.gameData.stats.homeTeamStats.totalSubstiutions += 2;

        Log("Logging Delgado: 2 assists.");
        matchManager.gameData.gameLog.LogEvent(delgado, MatchManager.ActionType.AssistProvided);
        matchManager.gameData.gameLog.LogEvent(delgado, MatchManager.ActionType.AssistProvided);

        Log("Logging Soares: yellow card and goal.");
        soares.ReceiveYellowCard();
        matchManager.gameData.gameLog.LogEvent(soares, MatchManager.ActionType.YellowCardShown, connectedToken: yaneva);
        matchManager.gameData.gameLog.LogEvent(soares, MatchManager.ActionType.GoalScored);

        Log("Logging McNulty: goal.");
        matchManager.gameData.gameLog.LogEvent(mcNulty, MatchManager.ActionType.GoalScored);

        // Provide harmless opposing references so log strings stay readable.
        matchManager.PreviousTokenToTouchTheBallOnPurpose = null;
        matchManager.LastTokenToTouchTheBallOnPurpose = yaneva;

        Log($"Preview tokens loaded: home {yaneva.playerName}, {baas.playerName}, {yugar.playerName}; away {delgado.playerName}, {soares.playerName}, {mcNulty.playerName}.");
        Log($"Bench anchors present for preview context: {kuzmic.playerName}, {poulsen.playerName}.");
        Log("Stats UI preview armed. Waiting 10 seconds for visual inspection.");
        yield return new WaitForSeconds(10f);
    }

    private PlayerToken RequirePlayerToken(string playerName)
    {
        PlayerToken token = PlayerToken.GetPlayerTokenByName(playerName);
        AssertTrue(token != null, $"PlayerToken '{playerName}' should exist for the stats preview test.");
        return token;
    }

    private HexCell RequireHex(HexCell hex, string message)
    {
        AssertTrue(hex != null, message);
        return hex;
    }

    private IEnumerable<HexCell> GetAllInBoundsHexesOrdered(HexCell referenceHex = null)
    {
        referenceHex ??= firstTimePassManager != null && firstTimePassManager.ball != null
            ? firstTimePassManager.ball.GetCurrentHex()
            : null;

        return hexgrid.cells
            .Cast<HexCell>()
            .Where(cell => cell != null && !cell.isOutOfBounds)
            .OrderBy(cell => referenceHex != null ? HexGridUtils.GetHexStepDistance(referenceHex, cell) : 0)
            .ThenBy(cell => cell.coordinates.x)
            .ThenBy(cell => cell.coordinates.z);
    }

    private GroundPassValidationResult ValidateFtpTarget(HexCell targetHex)
    {
        return GroundPassCommon.ValidateStandardPassPath(hexgrid, firstTimePassManager.ball, targetHex, 6);
    }

    private HexCell FindFirstFtpTarget(Func<HexCell, GroundPassValidationResult, bool> predicate)
    {
        foreach (HexCell candidate in GetAllInBoundsHexesOrdered())
        {
            if (candidate == null || candidate == firstTimePassManager.ball.GetCurrentHex())
            {
                continue;
            }

            GroundPassValidationResult validation = ValidateFtpTarget(candidate);
            if (predicate(candidate, validation))
            {
                return candidate;
            }
        }

        return null;
    }

    private HexCell FindFirstSafeFtpTarget(HexCell excludedHex = null)
    {
        return FindFirstFtpTarget((candidate, validation) =>
            candidate != excludedHex
            && validation.IsValid
            && GroundPassCommon.BuildOrderedInterceptionCandidates(hexgrid, firstTimePassManager.ball, candidate).Count == 0);
    }

    private HexCell FindFirstSafeFtpSpaceTargetReachableByPasser(PlayerToken passer)
    {
        if (passer == null || passer.GetCurrentHex() == null)
        {
            return null;
        }

        var (reachableHexes, _) = HexGridUtils.GetReachableHexes(hexgrid, passer.GetCurrentHex(), passer.pace);
        HashSet<HexCell> reachableSet = reachableHexes != null
            ? new HashSet<HexCell>(reachableHexes)
            : new HashSet<HexCell>();

        return FindFirstFtpTarget((candidate, validation) =>
            validation.IsValid
            && !candidate.isAttackOccupied
            && reachableSet.Contains(candidate)
            && GroundPassCommon.BuildOrderedInterceptionCandidates(hexgrid, firstTimePassManager.ball, candidate).Count == 0);
    }

    private HexCell FindFirstOutOfRangeFtpTarget()
    {
        return FindFirstFtpTarget((candidate, validation) =>
            validation.FailureReason == PassValidationFailureReason.OutOfRange);
    }

    private FtpBlockedHoverSetup FindFtpBlockedHoverSetup(HexCell excludedTarget = null)
    {
        HexCell ballHex = firstTimePassManager.ball.GetCurrentHex();
        if (ballHex == null)
        {
            return null;
        }

        List<HexCell> defenderHexes = hexgrid.GetDefenderHexes()
            .Where(hex => hex != null && hex.GetOccupyingToken() != null)
            .OrderBy(hex => hex.coordinates.x)
            .ThenBy(hex => hex.coordinates.z)
            .ToList();

        if (defenderHexes.Count == 0)
        {
            return null;
        }

        foreach (HexCell targetHex in GetAllInBoundsHexesOrdered(ballHex))
        {
            if (targetHex == null || targetHex == excludedTarget || targetHex == ballHex)
            {
                continue;
            }

            GroundPassValidationResult validation = ValidateFtpTarget(targetHex);
            if (!validation.IsValid || validation.PathHexes == null)
            {
                continue;
            }

            List<HexCell> candidateBlockingHexes = validation.PathHexes
                .Where(hex =>
                    hex != null &&
                    hex != ballHex &&
                    hex != targetHex &&
                    !hex.isAttackOccupied &&
                    !hex.isDefenseOccupied)
                .OrderBy(hex => HexGridUtils.GetHexStepDistance(ballHex, hex))
                .ThenBy(hex => hex.coordinates.x)
                .ThenBy(hex => hex.coordinates.z)
                .ToList();

            if (candidateBlockingHexes.Count == 0)
            {
                continue;
            }

            HexCell chosenBlockingHex = candidateBlockingHexes[0];
            HexCell originalDefenderHex = defenderHexes[0];
            PlayerToken defenderToken = originalDefenderHex.GetOccupyingToken();
            if (defenderToken == null)
            {
                continue;
            }

            return new FtpBlockedHoverSetup
            {
                targetHex = targetHex,
                defenderToken = defenderToken,
                originalDefenderHex = originalDefenderHex,
                blockingHex = chosenBlockingHex,
            };
        }

        return null;
    }

    private List<HexCell> GetLegalSingleHexMoves(PlayerToken token)
    {
        if (token == null)
        {
            return new List<HexCell>();
        }

        movementPhaseManager.HighlightValidMovementHexes(token, 1, false);
        List<HexCell> legalDestinations = hexgrid.highlightedHexes
            .Where(hex => hex != null)
            .OrderBy(hex => hex.coordinates.x)
            .ThenBy(hex => hex.coordinates.z)
            .ToList();
        hexgrid.ClearHighlightedHexes();
        return legalDestinations;
    }

    private FtpInterceptionMoveSetup FindFtpInterceptionMoveSetup(bool requireBlockingPath)
    {
        HexCell ballHex = firstTimePassManager.ball.GetCurrentHex();
        if (ballHex == null)
        {
            return null;
        }

        foreach (HexCell targetHex in GetAllInBoundsHexesOrdered(ballHex))
        {
            if (targetHex == ballHex)
            {
                continue;
            }

            GroundPassValidationResult validation = ValidateFtpTarget(targetHex);
            if (!validation.IsValid)
            {
                continue;
            }

            if (GroundPassCommon.BuildOrderedInterceptionCandidates(hexgrid, firstTimePassManager.ball, targetHex).Count != 0)
            {
                continue;
            }

            List<(PlayerToken token, HexCell originalHex, HexCell destinationHex)> attackerMoveOptions = new()
            {
                (null, null, null)
            };

            foreach (HexCell attackerHex in hexgrid.GetAttackerHexes().OrderBy(hex => hex.coordinates.x).ThenBy(hex => hex.coordinates.z))
            {
                PlayerToken attackerToken = attackerHex?.GetOccupyingToken();
                if (attackerToken == null)
                {
                    continue;
                }

                foreach (HexCell attackerDestinationHex in GetLegalSingleHexMoves(attackerToken))
                {
                    attackerMoveOptions.Add((attackerToken, attackerHex, attackerDestinationHex));
                }
            }

            foreach ((PlayerToken attackerToken, HexCell attackerHex, HexCell attackerDestinationHex) in attackerMoveOptions)
            {
                if (attackerToken != null && attackerDestinationHex != null)
                {
                    SetTokenHexForTest(attackerToken, attackerDestinationHex);
                }

                try
                {
                    List<HexCell> pathHexes = GroundPassCommon.CalculateThickPath(hexgrid, ballHex, targetHex, firstTimePassManager.ball.ballRadius);
                    List<HexCell> relevantInterceptionHexes = GroundPassCommon.GetRelevantInterceptionHexes(pathHexes, targetHex);

                    if (GroundPassCommon.BuildOrderedInterceptionCandidates(hexgrid, firstTimePassManager.ball, targetHex).Count != 0)
                    {
                        continue;
                    }

                    foreach (HexCell defenderHex in hexgrid.GetDefenderHexes().OrderBy(hex => hex.coordinates.x).ThenBy(hex => hex.coordinates.z))
                    {
                        PlayerToken defenderToken = defenderHex?.GetOccupyingToken();
                        if (defenderToken == null || defenderToken.tackling > 4)
                        {
                            continue;
                        }

                        foreach (HexCell destinationHex in GetLegalSingleHexMoves(defenderToken))
                        {
                            bool isBlockingPath = pathHexes.Contains(destinationHex);
                            bool isZoIOnly = !isBlockingPath && destinationHex
                                .GetNeighbors(hexgrid)
                                .Any(neighbor => neighbor != null && relevantInterceptionHexes.Contains(neighbor));

                            if (requireBlockingPath != isBlockingPath)
                            {
                                continue;
                            }

                            if (!isBlockingPath && !isZoIOnly)
                            {
                                continue;
                            }

                            return new FtpInterceptionMoveSetup
                            {
                                targetHex = targetHex,
                                attackerHex = attackerHex,
                                attackerToken = attackerToken,
                                attackerDestinationHex = attackerDestinationHex,
                                defenderHex = defenderHex,
                                defenderToken = defenderToken,
                                defenderDestinationHex = destinationHex,
                                isBlockingPath = isBlockingPath,
                            };
                        }
                    }
                }
                finally
                {
                    if (attackerToken != null && attackerHex != null)
                    {
                        SetTokenHexForTest(attackerToken, attackerHex);
                    }
                }
            }
        }

        return null;
    }

    private void SimulateFirstTimePassHover(HexCell hex)
    {
        MethodInfo hoverMethod = typeof(FirstTimePassManager).GetMethod("OnHoverReceived", BindingFlags.Instance | BindingFlags.NonPublic);
        AssertTrue(hoverMethod != null, "FirstTimePassManager private hover handler should exist for the FTP hover tests.");
        hoverMethod?.Invoke(firstTimePassManager, new object[] { hex?.occupyingToken, hex });
    }

    private void PerformRiggedFirstTimePassInterceptionRoll(int rigRoll)
    {
        MethodInfo interceptionMethod = typeof(FirstTimePassManager).GetMethod("PerformFTPInterceptionRolls", BindingFlags.Instance | BindingFlags.NonPublic);
        AssertTrue(interceptionMethod != null, "FirstTimePassManager private interception roll method should exist for the FTP tests.");
        interceptionMethod?.Invoke(firstTimePassManager, new object[] { (int?)rigRoll });
    }

    private void PerformRiggedLongBallAccuracyRoll(int rigRoll)
    {
        MethodInfo accuracyMethod = typeof(LongBallManager).GetMethod("PerformAccuracyRoll", BindingFlags.Instance | BindingFlags.NonPublic);
        AssertTrue(accuracyMethod != null, "LongBallManager private accuracy roll method should exist for the Long Ball tests.");
        accuracyMethod?.Invoke(longBallManager, new object[] { (int?)rigRoll });
    }

    private void PerformRiggedLongBallDirectionRoll(int rigRoll)
    {
        MethodInfo directionMethod = typeof(LongBallManager).GetMethod("PerformDirectionRoll", BindingFlags.Instance | BindingFlags.NonPublic);
        AssertTrue(directionMethod != null, "LongBallManager private direction roll method should exist for the Long Ball tests.");
        directionMethod?.Invoke(longBallManager, new object[] { (int?)rigRoll });
    }

    private IEnumerator PerformRiggedLongBallDistanceRoll(int rigRoll)
    {
        MethodInfo distanceMethod = typeof(LongBallManager).GetMethod("PerformDistanceRoll", BindingFlags.Instance | BindingFlags.NonPublic);
        AssertTrue(distanceMethod != null, "LongBallManager private distance roll method should exist for the Long Ball tests.");
        IEnumerator coroutine = distanceMethod?.Invoke(longBallManager, new object[] { (int?)rigRoll }) as IEnumerator;
        AssertTrue(coroutine != null, "Long Ball distance roll should return a coroutine for the tests.");
        if (coroutine != null)
        {
            yield return StartCoroutine(coroutine);
        }
    }

    private void StartRiggedLongBallDistanceRollAsync(int rigRoll)
    {
        MethodInfo distanceMethod = typeof(LongBallManager).GetMethod("PerformDistanceRoll", BindingFlags.Instance | BindingFlags.NonPublic);
        AssertTrue(distanceMethod != null, "LongBallManager private distance roll method should exist for the Long Ball tests.");
        IEnumerator coroutine = distanceMethod?.Invoke(longBallManager, new object[] { (int?)rigRoll }) as IEnumerator;
        AssertTrue(coroutine != null, "Long Ball distance roll should return a coroutine for the tests.");
        if (coroutine != null)
        {
            StartCoroutine(coroutine);
        }
    }

    private IEnumerator PerformRiggedLongBallInterceptionRoll(int rigRoll)
    {
        MethodInfo interceptionMethod = typeof(LongBallManager).GetMethod("PerformInterceptionCheck", BindingFlags.Instance | BindingFlags.NonPublic);
        AssertTrue(interceptionMethod != null, "LongBallManager private interception roll method should exist for the Long Ball tests.");
        FieldInfo finalHexField = typeof(LongBallManager).GetField("finalHex", BindingFlags.Instance | BindingFlags.NonPublic);
        AssertTrue(finalHexField != null, "LongBallManager private final hex field should exist for the Long Ball tests.");
        HexCell landingHex = finalHexField?.GetValue(longBallManager) as HexCell;
        IEnumerator coroutine = interceptionMethod?.Invoke(longBallManager, new object[] { landingHex, (int?)rigRoll }) as IEnumerator;
        AssertTrue(coroutine != null, "Long Ball interception roll should return a coroutine for the tests.");
        if (coroutine != null)
        {
            yield return StartCoroutine(coroutine);
        }
    }

    private void StartRiggedLongBallInterceptionRollAsync(int rigRoll)
    {
        MethodInfo interceptionMethod = typeof(LongBallManager).GetMethod("PerformInterceptionCheck", BindingFlags.Instance | BindingFlags.NonPublic);
        AssertTrue(interceptionMethod != null, "LongBallManager private interception roll method should exist for the Long Ball tests.");
        FieldInfo finalHexField = typeof(LongBallManager).GetField("finalHex", BindingFlags.Instance | BindingFlags.NonPublic);
        AssertTrue(finalHexField != null, "LongBallManager private final hex field should exist for the Long Ball tests.");
        HexCell landingHex = finalHexField?.GetValue(longBallManager) as HexCell;
        IEnumerator coroutine = interceptionMethod?.Invoke(longBallManager, new object[] { landingHex, (int?)rigRoll }) as IEnumerator;
        AssertTrue(coroutine != null, "Long Ball interception roll should return a coroutine for the tests.");
        if (coroutine != null)
        {
            StartCoroutine(coroutine);
        }
    }

    private int GetLongBallInterceptionCandidateCount()
    {
        FieldInfo interceptingDefendersField = typeof(LongBallManager).GetField("interceptingDefenders", BindingFlags.Instance | BindingFlags.NonPublic);
        AssertTrue(interceptingDefendersField != null, "LongBallManager private interception defender list should exist for the Long Ball tests.");
        List<HexCell> interceptingDefenders = interceptingDefendersField?.GetValue(longBallManager) as List<HexCell>;
        return interceptingDefenders?.Count ?? 0;
    }

    private bool IsWaitingForLongBallInterceptionRoll()
    {
        FieldInfo interceptionFlagField = typeof(LongBallManager).GetField("isWaitingForInterceptionRoll", BindingFlags.Instance | BindingFlags.NonPublic);
        AssertTrue(interceptionFlagField != null, "LongBallManager private interception waiting flag should exist for the Long Ball tests.");
        return interceptionFlagField != null && (bool)interceptionFlagField.GetValue(longBallManager);
    }

    private string GetFirstLongBallInterceptionCandidateName()
    {
        FieldInfo interceptingDefendersField = typeof(LongBallManager).GetField("interceptingDefenders", BindingFlags.Instance | BindingFlags.NonPublic);
        AssertTrue(interceptingDefendersField != null, "LongBallManager private interception defender list should exist for the Long Ball tests.");
        List<HexCell> interceptingDefenders = interceptingDefendersField?.GetValue(longBallManager) as List<HexCell>;
        return interceptingDefenders?.FirstOrDefault()?.GetOccupyingToken()?.playerName;
    }

    private IEnumerator StartPreparedLongBallToTarget(int difficulty, Vector2Int targetCoordinates, string expectedAccuracyThreshold)
    {
        yield return StartCoroutine(PrepareManualLongBallBoardState(difficulty));

        Log("Pressing L - Start Long Ball");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.L, 0.1f));
        AssertTrue(longBallManager.isActivated, "Long Ball should be activated after pressing L.");
        AssertTrue(longBallManager.isAwaitingTargetSelection, "Long Ball should be waiting for target selection.");

        HexCell targetHex = RequireHex(
            hexgrid.GetHexCellAt(new Vector3Int(targetCoordinates.x, 0, targetCoordinates.y)),
            $"Long Ball target {targetCoordinates} should exist.");

        Log($"Clicking {targetCoordinates} - Select Long Ball target");
        yield return StartCoroutine(gameInputManager.DelayedClick(targetCoordinates, 0.1f));

        if (difficulty == 3)
        {
            AssertTrue(!longBallManager.isAwaitingTargetSelection, "Difficulty 3 Long Ball should commit on the first valid click.");
            AssertTrue(longBallManager.isWaitingForAccuracyRoll, "Difficulty 3 Long Ball should wait for accuracy immediately after the first valid click.");
            if (!string.IsNullOrWhiteSpace(expectedAccuracyThreshold))
            {
                AssertTrue(
                    longBallManager.GetInstructions().Contains(expectedAccuracyThreshold),
                    $"Long Ball instructions should show an accuracy threshold of {expectedAccuracyThreshold}.",
                    true,
                    longBallManager.GetInstructions()
                );
            }
        }
        else
        {
            AssertTrue(longBallManager.currentTargetHex == targetHex, "Long Ball should accept the selected target.", targetHex, longBallManager.currentTargetHex);
            Log($"Clicking {targetCoordinates} again - Confirm Long Ball target");
            yield return StartCoroutine(gameInputManager.DelayedClick(targetCoordinates, 0.1f));
            AssertTrue(longBallManager.isWaitingForAccuracyRoll, "Long Ball should be waiting for the accuracy roll after target confirmation.");
            if (!string.IsNullOrWhiteSpace(expectedAccuracyThreshold))
            {
                AssertTrue(
                    longBallManager.GetInstructions().Contains(expectedAccuracyThreshold),
                    $"Long Ball instructions should show an accuracy threshold of {expectedAccuracyThreshold}.",
                    true,
                    longBallManager.GetInstructions()
                );
            }
        }
    }

    private IEnumerator ForfeitActiveFinalThirds(int maxForfeits = 3)
    {
        int forfeitsUsed = 0;
        while (finalThirdManager.isActivated && forfeitsUsed < maxForfeits)
        {
            Log("Pressing X - Forfeit current Final Third");
            yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
            yield return new WaitForSeconds(0.2f);
            forfeitsUsed++;
        }
        AssertTrue(!finalThirdManager.isActivated, "Final Third should no longer be active after forfeiting all pending Long Ball F3 moves.");
    }

    private bool IsEligibleLongBallInterceptor(PlayerToken token)
    {
        return token != null
            && !movementPhaseManager.stunnedTokens.Contains(token)
            && !movementPhaseManager.stunnedforNext.Contains(token)
            && !headerManager.defenderWillJump.Contains(token);
    }

    private bool LandingHexHasEligibleLongBallInterceptors(HexCell landingHex)
    {
        if (landingHex == null)
        {
            return false;
        }

        List<HexCell> eligibleDefenderHexes = hexgrid.GetDefenderHexes()
            .Where(hex => hex != null && IsEligibleLongBallInterceptor(hex.GetOccupyingToken()))
            .ToList();

        return GroundPassCommon.BuildOrderedInterceptionCandidates(
                hexgrid,
                longBallManager.ball,
                landingHex,
                candidateDefenders: eligibleDefenderHexes,
                isQuickThrow: true
            )
            .Count > 0;
    }

    private HexCell FindFirstCleanLongBallTarget(bool requireDangerous)
    {
        HexCell ballHex = longBallManager.ball.GetCurrentHex();
        foreach (HexCell candidate in GetAllInBoundsHexesOrdered(ballHex))
        {
            if (candidate == null || candidate == ballHex || candidate.isInPenaltyBox != 0)
            {
                continue;
            }

            var (isValid, isDangerous) = longBallManager.ValidateLongBallTarget(candidate);
            if (!isValid || isDangerous != requireDangerous)
            {
                continue;
            }

            if (LandingHexHasEligibleLongBallInterceptors(candidate))
            {
                continue;
            }

            return candidate;
        }

        return null;
    }

    private (HexCell intendedTarget, HexCell finalTarget, int directionRoll, int distanceRoll)? FindLongBallInaccuracySetup(
        Func<HexCell, bool> finalTargetPredicate,
        bool? requireDangerous = null)
    {
        HexCell ballHex = longBallManager.ball.GetCurrentHex();
        foreach (HexCell intendedTarget in GetAllInBoundsHexesOrdered(ballHex))
        {
            if (intendedTarget == null || intendedTarget == ballHex)
            {
                continue;
            }

            var (isValid, isDangerous) = longBallManager.ValidateLongBallTarget(intendedTarget);
            if (!isValid)
            {
                continue;
            }

            if (requireDangerous.HasValue && isDangerous != requireDangerous.Value)
            {
                continue;
            }

            for (int directionRoll = 0; directionRoll < 6; directionRoll++)
            {
                for (int distanceRoll = 1; distanceRoll <= 6; distanceRoll++)
                {
                    HexCell finalTarget = outOfBoundsManager.CalculateInaccurateTarget(intendedTarget, directionRoll, distanceRoll);
                    if (finalTargetPredicate(finalTarget))
                    {
                        return (intendedTarget, finalTarget, directionRoll, distanceRoll);
                    }
                }
            }
        }

        return null;
    }

    private int GetFirstTimePassInterceptionCandidateCount()
    {
        FieldInfo interceptionCandidatesField = typeof(FirstTimePassManager).GetField("interceptionCandidates", BindingFlags.Instance | BindingFlags.NonPublic);
        AssertTrue(interceptionCandidatesField != null, "FirstTimePassManager private interception candidate list should exist for the FTP tests.");
        List<GroundInterceptionCandidate> candidates = interceptionCandidatesField?.GetValue(firstTimePassManager) as List<GroundInterceptionCandidate>;
        return candidates?.Count ?? 0;
    }

    private static Vector2Int ToClickCoordinates(HexCell hex)
    {
        return new Vector2Int(hex.coordinates.x, hex.coordinates.z);
    }

    private void SetTokenHexForTest(PlayerToken token, HexCell destinationHex)
    {
        if (token == null || destinationHex == null)
        {
            return;
        }

        HexCell previousHex = token.GetCurrentHex();
        if (previousHex != null)
        {
            previousHex.isAttackOccupied = false;
            previousHex.isDefenseOccupied = false;
            if (previousHex.occupyingToken == token)
            {
                previousHex.occupyingToken = null;
            }
        }

        bool wasAttacker = token.isAttacker;
        token.SetCurrentHex(destinationHex);
        destinationHex.isAttackOccupied = wasAttacker;
        destinationHex.isDefenseOccupied = !wasAttacker;
        destinationHex.occupyingToken = token;

        Vector3 destinationPosition = destinationHex.GetHexCenter();
        float tokenHeight = token.transform.position.y > 0.01f ? token.transform.position.y : 0.2f;
        token.transform.position = new Vector3(destinationPosition.x, tokenHeight, destinationPosition.z);
    }

    private IEnumerator WaitForFtpDefenderMovementPhase(float timeoutSeconds = 2f)
    {
        float elapsedSeconds = 0f;
        while (elapsedSeconds < timeoutSeconds)
        {
            if (firstTimePassManager != null && firstTimePassManager.isWaitingForDefenderSelection)
            {
                yield break;
            }

            yield return null;
            elapsedSeconds += Time.deltaTime;
        }
    }

    private IEnumerator WaitForCondition(Func<bool> condition, float timeoutSeconds, string failureMessage)
    {
        float elapsedSeconds = 0f;
        while (elapsedSeconds < timeoutSeconds)
        {
            if (condition())
            {
                yield break;
            }

            yield return null;
            elapsedSeconds += Time.deltaTime;
        }

        AssertTrue(false, failureMessage);
    }

    private string GetFinalThirdCurrentTeamMoving()
    {
        if (finalThirdManager == null)
        {
            return null;
        }

        FieldInfo currentTeamField = typeof(FinalThirdManager).GetField("currentTeamMoving", BindingFlags.Instance | BindingFlags.NonPublic);
        return currentTeamField?.GetValue(finalThirdManager) as string;
    }

    private void AssertColorApproximately(Color actual, Color expected, float tolerance, string message)
    {
        bool withinTolerance =
            Mathf.Abs(actual.r - expected.r) <= tolerance &&
            Mathf.Abs(actual.g - expected.g) <= tolerance &&
            Mathf.Abs(actual.b - expected.b) <= tolerance &&
            Mathf.Abs(actual.a - expected.a) <= tolerance;

        AssertTrue(withinTolerance, message, expected, actual);
    }

    private IEnumerator PrepareFtpAvailabilityFromKickoff(int difficulty)
    {
        yield return new WaitForSeconds(3f);

        MatchManager.Instance.difficulty_level = difficulty;
        if (MatchManager.Instance.gameData != null && MatchManager.Instance.gameData.gameSettings != null)
        {
            MatchManager.Instance.gameData.gameSettings.playerAssistance = difficulty;
        }
        Log($"Setting difficulty to {difficulty}");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Space, 0.05f));
        Log("Pressing Space");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.P, 0.05f));
        Log("Pressing P");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(-6, -6), 0.2f));
        Log("Clicking (-6, -6)");

        if (difficulty != 3)
        {
            yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(-6, -6), 0.2f));
            Log("Clicking (-6, -6) again");
        }

        Log("Wait for the ball to move");
        float timeoutSeconds = difficulty == 3 ? 8f : 5f;
        float elapsedSeconds = 0f;
        AvailabilityCheckResult availabilityCheck = AssertCorrectAvailabilityAfterGBToPlayer();
        while (!availabilityCheck.passed && elapsedSeconds < timeoutSeconds)
        {
            yield return new WaitForSeconds(0.25f);
            elapsedSeconds += 0.25f;
            availabilityCheck = AssertCorrectAvailabilityAfterGBToPlayer();
        }

        AssertTrue(
            availabilityCheck.passed,
            "Action Availability after Pass to Player",
            true,
            availabilityCheck.ToString()
        );
    }

    private IEnumerator PrepareLongBallAvailabilityFromKickoff(int difficulty)
    {
        yield return new WaitForSeconds(3f);

        MatchManager.Instance.difficulty_level = difficulty;
        if (MatchManager.Instance.gameData != null && MatchManager.Instance.gameData.gameSettings != null)
        {
            MatchManager.Instance.gameData.gameSettings.playerAssistance = difficulty;
        }
        Log($"Setting difficulty to {difficulty}");

        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Space, 0.05f));
        Log("Pressing Space");

        AssertTrue(longBallManager.isAvailable, "Long Ball should be available after kickoff.");

        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.L, 0.05f));
        Log("Pressing L");

        AssertTrue(longBallManager.isActivated, "Long Ball should be activated after pressing L.");
        AssertTrue(longBallManager.isAwaitingTargetSelection, "Long Ball should be waiting for target selection after pressing L.");
        AssertTrue(!longBallManager.isAvailable, "Long Ball should no longer be available after activation.");
    }

    private IEnumerator PrepareManualLongBallBoardState(int difficulty)
    {
        yield return new WaitForSeconds(3f);

        MatchManager.Instance.difficulty_level = difficulty;
        if (MatchManager.Instance.gameData != null && MatchManager.Instance.gameData.gameSettings != null)
        {
            MatchManager.Instance.gameData.gameSettings.playerAssistance = difficulty;
        }
        Log($"Setting difficulty to {difficulty}");

        Log("Pressing Space");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Space, 0.05f));
        Log("Pressing P");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.P, 0.05f));
        Log("Clicking (-8, -4) - GBM target");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(-8, -4), 0.1f));
        if (difficulty != 3)
        {
            Log("Clicking (-8, -4) again - Confirm GBM");
            yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(-8, -4), 0.1f));
        }

        yield return StartCoroutine(WaitForCondition(
            () => longBallManager.ball.GetCurrentHex() == hexgrid.GetHexCellAt(new Vector3Int(-8, 0, -4)) && (finalThirdManager.isActivated || movementPhaseManager.isActivated),
            6f,
            "Kickoff GBM should complete and place the ball on (-8,-4)."));

        Log("Pressing X - Forfeit Att F3");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        yield return StartCoroutine(WaitForCondition(
            () => finalThirdManager.isActivated && GetFinalThirdCurrentTeamMoving() == "defense",
            2f,
            "Defensive Final Third should be active after forfeiting attack F3."));

        Log("Clicking (18, 0) - Select Poulsen");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(18, 0), 0.1f));
        yield return new WaitForSeconds(0.15f);
        Log("Clicking (16, -4) - Move Poulsen");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(16, -4), 0.1f));
        yield return StartCoroutine(WaitForCondition(
            () =>
                RequirePlayerToken("Poulsen").GetCurrentHex() == hexgrid.GetHexCellAt(new Vector3Int(16, 0, -4))
                && !movementPhaseManager.isPlayerMoving,
            3f,
            "Poulsen should finish the defensive Final Third move to (16,-4)."));
        Log("Pressing X - Forfeit rest of Def F3");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        yield return StartCoroutine(WaitForCondition(
            () =>
                movementPhaseManager.isActivated
                && movementPhaseManager.isMovementPhaseAttack
                && movementPhaseManager.isAwaitingTokenSelection
                && !movementPhaseManager.isPlayerMoving
                && !finalThirdManager.isActivated,
            2f,
            "Movement Phase attack turn should start after final-third forfeits."));

        PlayerToken ulisses = RequirePlayerToken("Ulisses");
        Log("Clicking (-8, -8) - Select Ulisses");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(-8, -8), 0.1f));
        yield return StartCoroutine(WaitForCondition(
            () => movementPhaseManager.selectedToken == ulisses && movementPhaseManager.isAwaitingHexDestination,
            2f,
            "Ulisses should be selected and waiting for a movement destination."));
        Log("Clicking (-8, -4) - Move Ulisses onto the ball");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(-8, -4), 0.1f));
        yield return StartCoroutine(WaitForCondition(
            () =>
                MatchManager.Instance.LastTokenToTouchTheBallOnPurpose == ulisses
                && ulisses.GetCurrentHex() == hexgrid.GetHexCellAt(new Vector3Int(-8, 0, -4))
                && longBallManager.ball.GetCurrentHex() == ulisses.GetCurrentHex(),
            3f,
            "Ulisses should collect the ball on (-8,-4) during Long Ball prep."));

        if (movementPhaseManager.isDribblerRunning)
        {
            Log("Pressing X - Forfeit Ulisses remaining pace");
            yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
            yield return StartCoroutine(WaitForCondition(
                () => movementPhaseManager.isMovementPhaseAttack && !movementPhaseManager.isDribblerRunning,
                2f,
                "Ulisses should stop dribbler-running after forfeiting remaining pace."));
        }

        Log("Pressing X - Forfeit rest of Att MP");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        yield return StartCoroutine(WaitForCondition(
            () => movementPhaseManager.isActivated && movementPhaseManager.isMovementPhaseDef && movementPhaseManager.isAwaitingTokenSelection,
            2f,
            "Defensive Movement Phase should start after forfeiting attack MP."));

        PlayerToken abraham = RequirePlayerToken("Abraham");
        Log("Clicking (-12, 0) - Select Abraham");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(-12, 0), 0.1f));
        yield return StartCoroutine(WaitForCondition(
            () => movementPhaseManager.selectedToken == abraham && movementPhaseManager.isAwaitingHexDestination,
            2f,
            "Abraham should be selected and waiting for a movement destination."));
        Log("Clicking (-8, -3) - Move Abraham");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(-8, -3), 0.1f));
        yield return StartCoroutine(WaitForCondition(
            () =>
                abraham.GetCurrentHex() == hexgrid.GetHexCellAt(new Vector3Int(-8, 0, -3))
                && !movementPhaseManager.isPlayerMoving,
            3f,
            "Abraham should finish moving to (-8,-3)."));

        if (movementPhaseManager.isWaitingForTackleDecision)
        {
            Log("Pressing N - Abraham stands without tackling");
            yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.N, 0.1f));
            yield return StartCoroutine(WaitForCondition(
                () => movementPhaseManager.isMovementPhase2f2 || movementPhaseManager.isMovementPhaseDef,
                2f,
                "Movement Phase should continue after declining Abraham's tackle."));
        }

        if (movementPhaseManager.isMovementPhaseDef)
        {
            Log("Pressing X - Forfeit rest of Def MP");
            yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        }

        yield return StartCoroutine(WaitForCondition(
            () => movementPhaseManager.isMovementPhase2f2 && !movementPhaseManager.isPlayerMoving,
            3f,
            "Movement Phase should be in 2f2."));
        Log("Pressing X - Forfeit 2f2");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        yield return StartCoroutine(WaitForCondition(
            () => finalThirdManager.isActivated,
            2f,
            "Final Third should be active after 2f2 ends."));

        Log("Pressing X - Forfeit Att F3");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        yield return StartCoroutine(WaitForCondition(
            () => finalThirdManager.isActivated && GetFinalThirdCurrentTeamMoving() == "defense",
            2f,
            "Defensive Final Third should start after forfeiting attack F3."));
        Log("Pressing X - Forfeit Def F3");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        yield return StartCoroutine(WaitForCondition(
            () => !finalThirdManager.isActivated,
            2f,
            "Final Third should end after both sides forfeit."));

        AvailabilityCheckResult availabilityCheck = AssertCorrectAvailabilityAfterMovementComplete();
        AssertTrue(
            availabilityCheck.passed,
            "Action Availability after manual Long Ball prep is correct",
            true,
            availabilityCheck.ToString()
        );

        AssertTrue(longBallManager.ball.GetCurrentHex() == hexgrid.GetHexCellAt(new Vector3Int(-8, 0, -4)), "Ball should be on (-8,-4) after Long Ball prep.", hexgrid.GetHexCellAt(new Vector3Int(-8, 0, -4)), longBallManager.ball.GetCurrentHex());
        AssertTrue(MatchManager.Instance.LastTokenToTouchTheBallOnPurpose == RequirePlayerToken("Ulisses"), "Ulisses should be the last token after Long Ball prep.");
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
            File.AppendAllText(logFilePath, errorLog + "\n");
            testFailed = true;
        }
        else
        {
            Debug.Log("✅ All scene components successfully linked.");
            AttachTestInputLogging();
        }
    }

    private void AttachTestInputLogging()
    {
        DetachTestInputLogging();
        GameInputManager.OnClick += HandleTestInputClickLogged;
        GameInputManager.OnKeyPress += HandleTestInputKeyLogged;
    }

    private void DetachTestInputLogging()
    {
        GameInputManager.OnClick -= HandleTestInputClickLogged;
        GameInputManager.OnKeyPress -= HandleTestInputKeyLogged;
    }

    private void HandleTestInputClickLogged(PlayerToken token, HexCell hex)
    {
        if (!shouldRunTests || string.IsNullOrWhiteSpace(logFilePath) || string.IsNullOrWhiteSpace(currentScenarioName))
        {
            return;
        }

        string clickedHex = hex != null ? $"{hex.name} @ {hex.coordinates}" : "null";
        string clickedToken = token != null ? token.name : "None";
        Log($"INPUT click -> hex: {clickedHex}, token: {clickedToken}");
    }

    private void HandleTestInputKeyLogged(KeyPressData keyData)
    {
        if (!shouldRunTests || string.IsNullOrWhiteSpace(logFilePath) || string.IsNullOrWhiteSpace(currentScenarioName) || keyData == null)
        {
            return;
        }

        Log($"INPUT key -> {FormatKeyChord(keyData)} | Consumed: {keyData.isConsumed}");
    }

    private static string FormatKeyChord(KeyPressData keyData)
    {
        if (keyData == null)
        {
            return "None";
        }

        StringBuilder builder = new();
        if (keyData.ctrl) builder.Append("Ctrl+");
        if (keyData.alt) builder.Append("Alt+");
        if (keyData.shift) builder.Append("Shift+");
        builder.Append(keyData.key);
        return builder.ToString();
    }

    private void LogScenarioBoundary(bool isStart, int testNumber, string scenarioName, string status)
    {
        string boundaryKind = isStart ? "START" : "END";
        string suffix = string.IsNullOrWhiteSpace(status) ? string.Empty : $" [{status}]";
        Log($"\n==== {boundaryKind} Test #{testNumber}: {scenarioName}{suffix} ====");
    }

    private IEnumerator Scenario_001_BasicKickoff()
    {
        yield return new WaitForSeconds(2f); // Allow scene to stabilize

        Log("▶️ Starting test scenario: 'Kick Off'");


        // ✅ STEP 1: Press 2
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Alpha2, 0f));
        Log("Pressing 2");
        AssertTrue(
            MatchManager.Instance.currentState == MatchManager.GameState.KickOffSetup
            , "KickOff state check after pressing 2"
            , MatchManager.GameState.KickOffSetup
            , MatchManager.Instance.currentState
        );
        AssertTrue(
            groundBallManager.isActivated == false
            , "GBM is not activated after pressing 2"
            , false
            , groundBallManager.isActivated
        );
        AssertTrue(
            movementPhaseManager.isActivated == false
            , "MPM is not activated after pressing 2"
            , false
            , movementPhaseManager.isActivated
        );
        AssertTrue(
            groundBallManager.isAvailable == false
            , "GBM is not Availabls after pressing 2"
            , false
            , groundBallManager.isAvailable
        );
        AssertTrue(
            movementPhaseManager.isAvailable == false
            , "MPM is not Available after pressing 2"
            , false
            , movementPhaseManager.isAvailable
        );

        // ✅ STEP 2: Press Space to start match
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Space, 0.05f));
        Log("Pressing Space");
        AssertTrue(
            MatchManager.Instance.currentState == MatchManager.GameState.KickoffBlown
            , "KickOff state check after pressing Space"
            , MatchManager.GameState.KickoffBlown
            , MatchManager.Instance.currentState
        );
        AssertTrue(
            groundBallManager.isActivated == false
            , "GBM is not activated after pressing Space"
            , false
            , groundBallManager.isActivated
        );
        AssertTrue(
            movementPhaseManager.isActivated == false
            , "MPM is not activated after pressing Space"
            , false
            , movementPhaseManager.isActivated
        );
        AssertTrue(
            groundBallManager.isAvailable == true
            , "GBM is not Available after pressing Space"
            , true
            , groundBallManager.isAvailable
        );
        AssertTrue(
            movementPhaseManager.isAvailable == false
            , "MPM is not Available after pressing Space"
            , false
            , movementPhaseManager.isAvailable
        );

        // ✅ STEP 3: Press P (custom logic assumed)
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.P, 0.2f));
        Log("Pressing P");
        AssertTrue(
            groundBallManager.isActivated == true
            , "GBM is activated after pressing P"
            , true
            , groundBallManager.isActivated
        );
        AssertTrue(
            movementPhaseManager.isActivated == false
            , "MPM is not activated after pressing P"
            , false
            , movementPhaseManager.isActivated
        );
        AssertTrue(
            groundBallManager.isAvailable == false
            , "GBM is not Available after pressing P"
            , false
            , groundBallManager.isAvailable
        );
        AssertTrue(
            movementPhaseManager.isAvailable == false
            , "MPM is not Available after pressing P"
            , false
            , movementPhaseManager.isAvailable
        );
        AssertTrue(
            groundBallManager.isAwaitingTargetSelection == true
            , "GBM is waiting target selection after pressing P"
            , true
            , groundBallManager.isAwaitingTargetSelection
        );

        LogFooterofTest("KICK OFF");
    }
    
    private IEnumerator Scenario_002_GroundBall_0001_Commitment()
    {
        yield return new WaitForSeconds(3f); // Allow scene to stabilize

        Log("▶️ Starting test scenario: 'Ground Ball - Commitment'");

        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Alpha2, 0f));
        Log("Pressing 2");
        AssertTrue(
            MatchManager.Instance.currentState == MatchManager.GameState.KickOffSetup
            , "KickOff state check after pressing 2"
            , MatchManager.GameState.KickOffSetup
            , MatchManager.Instance.currentState
        );
        AssertTrue(
            groundBallManager.isActivated == false
            , "GBM is not activated after pressing 2"
            , false
            , groundBallManager.isActivated
        );
        AssertTrue(
            movementPhaseManager.isActivated == false
            , "MPM is not activated after pressing 2"
            , false
            , movementPhaseManager.isActivated
        );
        AssertTrue(
            groundBallManager.isAvailable == false
            , "GBM is not Availabls after pressing 2"
            , false
            , groundBallManager.isAvailable
        );
        AssertTrue(
            movementPhaseManager.isAvailable == false
            , "MPM is not Available after pressing 2"
            , false
            , movementPhaseManager.isAvailable
        );

        // ✅ STEP 2: Press Space to start match
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Space, 0.05f));
        Log("Pressing Space");
        AssertTrue(
            MatchManager.Instance.currentState == MatchManager.GameState.KickoffBlown
            , "KickOff state check after pressing Space"
            , MatchManager.GameState.KickoffBlown
            , MatchManager.Instance.currentState
        );
        AssertTrue(
            groundBallManager.isActivated == false
            , "GBM is not activated after pressing Space"
            , false
            , groundBallManager.isActivated
        );
        AssertTrue(
            movementPhaseManager.isActivated == false
            , "MPM is not activated after pressing Space"
            , false
            , movementPhaseManager.isActivated
        );
        AssertTrue(
            groundBallManager.isAvailable == true
            , "GBM is not Available after pressing Space"
            , true
            , groundBallManager.isAvailable
        );
        AssertTrue(
            movementPhaseManager.isAvailable == false
            , "MPM is not Available after pressing Space"
            , false
            , movementPhaseManager.isAvailable
        );

        // ✅ STEP 3: Press P (custom logic assumed)
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.P, 0.2f));
        Log("Pressing P");
        AssertTrue(
            groundBallManager.isActivated == true
            , "GBM is activated after pressing P"
            , true
            , groundBallManager.isActivated
        );
        AssertTrue(
            movementPhaseManager.isActivated == false
            , "MPM is not activated after pressing P"
            , false
            , movementPhaseManager.isActivated
        );
        AssertTrue(
            groundBallManager.isAvailable == false
            , "GBM is not Available after pressing P"
            , false
            , groundBallManager.isAvailable
        );
        AssertTrue(
            movementPhaseManager.isAvailable == false
            , "MPM is not Available after pressing P"
            , false
            , movementPhaseManager.isAvailable
        );
        AssertTrue(
            groundBallManager.isAwaitingTargetSelection == true
            , "GBM is waiting target selection after pressing P"
            , true
            , groundBallManager.isAwaitingTargetSelection
        );

        // ✅ STEP 4: Click (12, -6)
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(12, -6), 0.2f));
        Log("Clicking (12, -6)");
        AssertTrue(
            groundBallManager.isActivated == true
            , "GBM is activated after Clicking on (12, -6) which is too far away"
            , true
            , groundBallManager.isActivated
        );
        AssertTrue(
            groundBallManager.isAvailable == false
            , "GBM is not Available after Clicking on (12, -6) which is too far away"
            , false
            , groundBallManager.isAvailable
        );
        AssertTrue(
            groundBallManager.currentTargetHex == null
            , "GBM has a null target after Clicking on (12, -6) which is too far away"
            , null
            , groundBallManager.currentTargetHex
        );
        AssertTrue(
            groundBallManager.isAwaitingTargetSelection
            , "GGBM is waiting target selection after Clicking on (12, -6) which is too far away"
            , true
            , groundBallManager.isAwaitingTargetSelection
        );

        // ✅ STEP 5: Click (10,0)
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.2f));
        Log("Clicking (10, 0)");
        AssertTrue(
            groundBallManager.isActivated == true
            , "GBM is activated after Clicking on (10, 0) which is passable"
            , true
            , groundBallManager.isActivated
        );
        AssertTrue(
            groundBallManager.isAvailable == false
            , "GBM is not Available after Clicking on (10, 0) which is passable"
            , false
            , groundBallManager.isAvailable
        );
        AssertTrue(
            groundBallManager.currentTargetHex != null
            , "GBM has a valid target after Clicking on (10, 0) which is passable"
            , hexgrid.GetHexCellAt(new Vector3Int(10, 0, 0))
            , groundBallManager.currentTargetHex
        );
        AssertTrue(
            groundBallManager.isAwaitingTargetSelection
            , "GBM is waiting target selection after Clicking on (10, 0) which is passable"
            , true
            , groundBallManager.isAwaitingTargetSelection
        );

        // ✅ STEP 6: Click (12, -6) again
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(12, -6), 0.2f));
        Log("Clicking (12, -6) again");
        AssertTrue(
            groundBallManager.isActivated == true
            , "GBM is activated after Clicking again on (12, -6) which is too far away"
            , true
            , groundBallManager.isActivated
        );
        AssertTrue(
            groundBallManager.isAvailable == false
            , "GBM is not Available after Clicking again on (12, -6) which is too far away"
            , false
            , groundBallManager.isAvailable
        );
        AssertTrue(
            groundBallManager.currentTargetHex == null
            , "GBM has a null target after Clicking again on (12, -6) which is too far away"
            , null
            , groundBallManager.currentTargetHex
        );
        AssertTrue(
            groundBallManager.isAwaitingTargetSelection
            , "GBM is waiting target selection after Clicking again on (12, -6) which is too far away"
            , true
            , groundBallManager.isAwaitingTargetSelection
        );

        // ✅ STEP 7: Click (10,0) again
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.2f));
        Log("Clicking (10, 0) again");
        AssertTrue(
            groundBallManager.isActivated == true
            , "GBM is activated after Clicking on (10, 0) which is passable"
            , true
            , groundBallManager.isActivated
        );
        AssertTrue(
            groundBallManager.isAvailable == false
            , "GBM is not Available after Clicking on (10, 0) which is passable"
            , false
            , groundBallManager.isAvailable
        );
        AssertTrue(
            groundBallManager.currentTargetHex != null
            , "GBM has a valid target after Clicking on (10, 0) which is passable"
            , hexgrid.GetHexCellAt(new Vector3Int(10, 0, 0))
            , groundBallManager.currentTargetHex
        );
        AssertTrue(
            groundBallManager.isAwaitingTargetSelection
            , "GBM is waiting target selection after Clicking again on (10, 0) which is passable"
            , true
            , groundBallManager.isAwaitingTargetSelection
        );

        // ✅ STEP 8: Switch Valid target to  (4, -4) again
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(4, -4), 0.2f));
        Log("Clicking (4, -4)");
        AssertTrue(
            groundBallManager.isActivated == true
            , "GBM is activated after Clicking on (4, -4) which is passable"
            , true
            , groundBallManager.isActivated
        );
        AssertTrue(
            groundBallManager.isAvailable == false
            , "GBM is not Available after Clicking on (4, -4) which is passable"
            , false
            , groundBallManager.isAvailable
        );
        AssertTrue(
            groundBallManager.currentTargetHex != null
            , "GBM has a valid target after Clicking on (4, -4) which is passable"
            , hexgrid.GetHexCellAt(new Vector3Int(4, 0, -4))
            , groundBallManager.currentTargetHex
        );
        AssertTrue(
            groundBallManager.isAwaitingTargetSelection
            , "GBM is waiting target selection after Clicking again on (4, -4) which is passable"
            , true
            , groundBallManager.isAwaitingTargetSelection
        );

        // ✅ STEP 9: Click (10,0) again
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.2f));
        Log("Clicking (10, 0) again");
        AssertTrue(
            groundBallManager.isActivated == true
            , "GBM is activated after reClicking on (10, 0) which is passable"
            , true
            , groundBallManager.isActivated
        );
        AssertTrue(
            groundBallManager.isAvailable == false
            , "GBM is not Available after reClicking on (10, 0) which is passable"
            , false
            , groundBallManager.isAvailable
        );
        AssertTrue(
            groundBallManager.currentTargetHex != null
            , "GBM has a valid target after reClicking on (10, 0) which is passable"
            , hexgrid.GetHexCellAt(new Vector3Int(10, 0, 0))
            , groundBallManager.currentTargetHex
        );
        AssertTrue(
            groundBallManager.isAwaitingTargetSelection
            , "GBM is waiting target selection after reClicking again on (10, 0) which is passable"
            , true
            , groundBallManager.isAwaitingTargetSelection
        );

        // ✅ STEP 10: Click (10, 0) to confirm Pass
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.2f));
        Log("Clicking (10, 0) to confirm Pass");
        AssertTrue(
            !finalThirdManager.isActivated
            , "Final Third Manager is Still Inactive"
            , false
            , finalThirdManager.isActivated
        );
        AssertTrue(
            groundBallManager.isActivated == true
            , "GBM is activated after Clicking again on (10, 0) to confirm Pass"
            , true
            , groundBallManager.isActivated
        );
        AssertTrue(
            groundBallManager.isAvailable == false
            , "GBM is not Available after Clicking again on (10, 0) to confirm Pass"
            , false
            , groundBallManager.isAvailable
        );
        AssertTrue(
            groundBallManager.currentTargetHex != null
            , "GBM has a valid target after Clicking again on (10, 0) to confirm Pass"
            , hexgrid.GetHexCellAt(new Vector3Int(10, 0, 0))
            , groundBallManager.currentTargetHex
        );
        AssertTrue(
            !groundBallManager.isAwaitingTargetSelection
            , "GBM is NOT waiting target selection after Clicking again on (10, 0) which is passable"
            , false
            , groundBallManager.isAwaitingTargetSelection
        );

        yield return new WaitForSeconds(3f);
        Log("Wait for the ball to move");
        AvailabilityCheckResult availabilityCheck = AssertCorrectAvailabilityAfterGBToPlayer();
        AssertTrue(
            availabilityCheck.passed,
            "Action Availability after GB to Player is correct",
            true,
            availabilityCheck.ToString()
        );
        AssertTrue(
            finalThirdManager.isActivated
            , "Final Third Manager is activated after the pass"
            , true
            , finalThirdManager.isActivated
        );
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.2f));
        Log("Pressing X To forfeit Final Third");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.2f));
        Log("Pressing X To forfeit Defense Final Third");
        yield return new WaitForSeconds(0.25f);
        AssertTrue(
            !finalThirdManager.isActivated
            , "Final Third Manager is Done and closed"
            , false
            , finalThirdManager.isActivated
        );


        LogFooterofTest("Ground Ball - Invalid, Switch and Commitment to no interceptions");
        
    }

    private IEnumerator Scenario_002b_GroundBall_0001b_QuickThrow_Commitment()
    {
        yield return new WaitForSeconds(3f); // Allow scene to stabilize

        Log("▶️ Starting test scenario: 'Quick Throw - Commitment and Target Validation'");

        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Alpha2, 0f));
        Log("Pressing 2");
        AssertTrue(
            MatchManager.Instance.currentState == MatchManager.GameState.KickOffSetup
            , "KickOff state check after pressing 2"
            , MatchManager.GameState.KickOffSetup
            , MatchManager.Instance.currentState
        );

        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Space, 0.05f));
        Log("Pressing Space");
        AssertTrue(
            MatchManager.Instance.currentState == MatchManager.GameState.KickoffBlown
            , "KickOff state check after pressing Space"
            , MatchManager.GameState.KickoffBlown
            , MatchManager.Instance.currentState
        );
        AssertTrue(
            groundBallManager.isAvailable
            , "GBM is Available after pressing Space"
            , true
            , groundBallManager.isAvailable
        );

        MatchManager.Instance.currentState = MatchManager.GameState.QuickThrow;
        Log("Forcing MatchManager into QuickThrow mode for isolated GBM validation");

        groundBallManager.ActivateGroundBall(true);
        Log("Activating Ground Ball directly as a forced Quick Throw");
        AssertTrue(
            groundBallManager.isActivated
            , "GBM is activated after forcing Quick Throw mode"
            , true
            , groundBallManager.isActivated
        );
        AssertTrue(
            groundBallManager.isQuickThrow
            , "GBM is handling a Quick Throw after forcing Quick Throw mode"
            , true
            , groundBallManager.isQuickThrow
        );
        AssertTrue(
            groundBallManager.isAwaitingTargetSelection
            , "GBM is waiting target selection after forcing Quick Throw mode"
            , true
            , groundBallManager.isAwaitingTargetSelection
        );
        AssertTrue(
            MatchManager.Instance.currentState == MatchManager.GameState.QuickThrow
            , "MatchManager remains in QuickThrow state after forcing Quick Throw mode"
            , MatchManager.GameState.QuickThrow
            , MatchManager.Instance.currentState
        );

        // Too far away is still rejected in a Quick Throw
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(12, -6), 0.2f));
        Log("Clicking (12, -6)");
        AssertTrue(
            groundBallManager.currentTargetHex == null
            , "QT has a null target after Clicking on (12, -6) which is too far away"
            , null
            , groundBallManager.currentTargetHex
        );
        AssertTrue(
            groundBallManager.isAwaitingTargetSelection
            , "QT is still waiting target selection after Clicking on (12, -6) which is too far away"
            , true
            , groundBallManager.isAwaitingTargetSelection
        );

        // Targeting a defender directly must be rejected in a Quick Throw
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(3, 3), 0.2f));
        Log("Clicking (3, 3) which is Gilbert's hex");
        AssertTrue(
            groundBallManager.currentTargetHex == null
            , "QT has a null target after Clicking on defender-occupied target (3, 3)"
            , null
            , groundBallManager.currentTargetHex
        );
        AssertTrue(
            groundBallManager.isAwaitingTargetSelection
            , "QT is still waiting target selection after Clicking on defender-occupied target (3, 3)"
            , true
            , groundBallManager.isAwaitingTargetSelection
        );

        // Quick Throw ignores occupied/path ZOI issues except on the target hex.
        // This target is useful to prove that QT accepts targets that Standard Pass would treat more strictly.
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(4, 4), 0.2f));
        Log("Clicking (4, 4)");
        AssertTrue(
            groundBallManager.currentTargetHex != null
            , "QT has a valid target after Clicking on (4, 4)"
            , hexgrid.GetHexCellAt(new Vector3Int(4, 0, 4))
            , groundBallManager.currentTargetHex
        );
        AssertTrue(
            groundBallManager.isAwaitingTargetSelection
            , "QT is waiting target selection after Clicking on (4, 4)"
            , true
            , groundBallManager.isAwaitingTargetSelection
        );

        // A clearly safe target remains valid and not dangerous.
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.2f));
        Log("Clicking (10, 0)");
        AssertTrue(
            groundBallManager.currentTargetHex != null
            , "QT has a valid target after Clicking on (10, 0)"
            , hexgrid.GetHexCellAt(new Vector3Int(10, 0, 0))
            , groundBallManager.currentTargetHex
        );
        AssertTrue(
            !groundBallManager.passIsDangerous
            , "QT pass to (10, 0) is not considered dangerous"
            , false
            , groundBallManager.passIsDangerous
        );

        // Dangerous in Quick Throw means the target hex itself is influenced by defenders.
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(3, 2), 0.2f));
        Log("Clicking (3, 2)");
        AssertTrue(
            groundBallManager.currentTargetHex != null
            , "QT has a valid target after Clicking on (3, 2)"
            , hexgrid.GetHexCellAt(new Vector3Int(3, 0, 2))
            , groundBallManager.currentTargetHex
        );
        AssertTrue(
            groundBallManager.passIsDangerous
            , "QT pass to (3, 2) is considered dangerous because the target hex is influenced by defenders"
            , true
            , groundBallManager.passIsDangerous
        );

        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(3, 2), 0.2f));
        Log("Clicking (3, 2) again to confirm Quick Throw");
        AssertTrue(
            !groundBallManager.isAwaitingTargetSelection
            , "QT is no longer waiting for target selection after confirming (3, 2)"
            , false
            , groundBallManager.isAwaitingTargetSelection
        );
        AssertTrue(
            groundBallManager.isWaitingForDiceRoll
            , "QT is now waiting for interception dice roll(s)"
            , true
            , groundBallManager.isWaitingForDiceRoll
        );
        AssertTrue(
            groundBallManager.diceRollsPending > 0
            , "QT has at least one interceptor because the target hex is influenced"
            , true
            , groundBallManager.diceRollsPending > 0
        );

        LogFooterofTest("Quick Throw - Commitment and Target Validation");
    }

    private IEnumerator Scenario_003_GroundBall_0002_Dangerous_pass_no_interception()
    {
        yield return new WaitForSeconds(3f); // Allow scene to stabilize

        Log("▶️ Starting test scenario: 'Ground Ball - Dangerous Pass - No Interception'");

        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Alpha2, 0.05f));
        Log("Pressing 2");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Space, 0.05f));
        Log("Pressing Space");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.P, 0.05f));
        Log("Pressing P");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(0, 6), 0.2f));
        Log("Clicking (0, 6)");
        AssertTrue(
            groundBallManager.currentTargetHex != null
            , "GBM has a valid target after Clicking on (0, 6) which is passable, but dangerous"
            , hexgrid.GetHexCellAt(new Vector3Int(0, 0, 6))
            , groundBallManager.currentTargetHex
        );
        AssertTrue(
            groundBallManager.passIsDangerous
            , "GBM pass to (0, 6) is indeed considered dangerous"
            , true
            , groundBallManager.passIsDangerous
        );
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(3, 2), 0.2f));
        Log("Clicking (3, 2)");
        AssertTrue(
            groundBallManager.currentTargetHex != null
            , "GBM has a valid target after Clicking on (3, 2) which is passable, but dangerous"
            , hexgrid.GetHexCellAt(new Vector3Int(3, 0, 2))
            , groundBallManager.currentTargetHex
        );
        AssertTrue(
            groundBallManager.passIsDangerous
            , "GBM pass to (3, 2) is indeed considered dangerous"
            , true
            , groundBallManager.passIsDangerous
        );

        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(3, 2), 0.2f));
        Log("Clicking (3, 2) again");
        AssertTrue(
            groundBallManager.currentTargetHex != null
            , "GBM has a valid target after Confirming pass to (3, 2) which is passable, but dangerous"
            , hexgrid.GetHexCellAt(new Vector3Int(3, 0, 2))
            , groundBallManager.currentTargetHex
        );
        AssertTrue(
            groundBallManager.passIsDangerous
            , "GBM pass to (3, 2) is indeed considered dangerous"
            , true
            , groundBallManager.passIsDangerous
        );
        AssertTrue(
            !groundBallManager.isAwaitingTargetSelection
            , "GBM pass to (3, 2) is indeed considered dangerous"
            , false
            , !groundBallManager.isAwaitingTargetSelection
        );
        AssertTrue(
            groundBallManager.isWaitingForDiceRoll
            , "GBM is now waiting for a dice roll"
            , true
            , groundBallManager.isWaitingForDiceRoll
        );
        AssertTrue(
            groundBallManager.diceRollsPending == 3
            , "GBM is now waiting for 3 dice rolls"
            , 3
            , groundBallManager.diceRollsPending
        );
        AssertTrue(
            MatchManager.Instance.gameData.stats.GetPlayerStats("Cafferata").passesAttempted == 1,
            "Cafferata Should have 1 pass attempted",
            1,
            MatchManager.Instance.gameData.stats.GetPlayerStats("Cafferata").passesAttempted
        );
        yield return new WaitForSeconds(0.5f);
        groundBallManager.PerformGroundInterceptionDiceRoll(1);
        Log("Performing dice roll 1 for the first interceptor");
        AssertTrue(
            groundBallManager.defendingHexes.Count == 2
            , "GBM is now waiting for 2 dice rolls"
            , 2
            , groundBallManager.defendingHexes.Count
        );
        yield return new WaitForSeconds(0.5f);
        groundBallManager.PerformGroundInterceptionDiceRoll(1);
        Log("Performing dice roll 1 for the second interceptor");
        AssertTrue(
            groundBallManager.defendingHexes.Count == 1
            , "GBM is now waiting for 1 dice rolls"
            , 1
            , groundBallManager.defendingHexes.Count
        );
        yield return new WaitForSeconds(0.5f);
        groundBallManager.PerformGroundInterceptionDiceRoll(1);
        Log("Performing dice roll 1 for the third interceptor");
        AssertTrue(
            groundBallManager.defendingHexes.Count == 0
            , "GBM is cleaned up"
            , 0
            , groundBallManager.defendingHexes.Count
        );
        yield return new WaitForSeconds(2f);
        Log("Wait for the ball to move");
        AssertTrue(
            MatchManager.Instance.gameData.stats.GetPlayerStats("Cafferata").passesAttempted == 1,
            "Cafferata Should have 1 pass attempted",
            1,
            MatchManager.Instance.gameData.stats.GetPlayerStats("Cafferata").passesAttempted
        );
        AssertTrue(
            MatchManager.Instance.gameData.stats.GetPlayerStats("Cafferata").passesCompleted == 0,
            "Cafferata Should have 0 pass completed",
            0,
            MatchManager.Instance.gameData.stats.GetPlayerStats("Cafferata").passesCompleted
        );
        AssertTrue(
            groundBallManager.isActivated == false
            , "GBM is deactivated after ball movement"
            , false
            , groundBallManager.isActivated
        );
        AssertTrue(
            MatchManager.Instance.teamInAttack == MatchManager.TeamInAttack.Home
            , "home team is in attack after ball movement"
            , MatchManager.TeamInAttack.Home
            , MatchManager.Instance.teamInAttack
        );
        AssertTrue(
            MatchManager.Instance.attackHasPossession == false
            , "Attack has no possession after ball movement"
            , false
            , MatchManager.Instance.attackHasPossession
        );
        AvailabilityCheckResult availabilityCheck = AssertCorrectAvailabilityAfterGBToSpace();
        AssertTrue(
            availabilityCheck.passed,
            "Action Availability after GB to Space is correct",
            true,
            availabilityCheck.ToString()
        );

        var passer = PlayerToken.GetPlayerTokenByName("Cafferata");
        AssertTrue(
            MatchManager.Instance.LastTokenToTouchTheBallOnPurpose == passer,
            "Cafferata should be the last to touch the ball",
            passer,
            MatchManager.Instance.LastTokenToTouchTheBallOnPurpose
        );
        AssertTrue(
            MatchManager.Instance.hangingPassType == "ground",
            "There is a hanging pass and it is a ground pass",
            "ground",
            MatchManager.Instance.hangingPassType
        );

        LogFooterofTest("Ground Ball - Dangerous Pass - No Interception");
    }
    
    private IEnumerator Scenario_004_GroundBall_0003_Dangerous_pass_intercepted_by_second_interceptor()
    {
        yield return new WaitForSeconds(3f); // Allow scene to stabilize

        Log("▶️ Starting test scenario: 'Ground Ball - Dangerous Pass - No Interception'");

        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Alpha2, 0.05f));
        Log("Pressing 2");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Space, 0.05f));
        Log("Pressing Space");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.P, 0.05f));
        Log("Pressing P");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(3, 2), 0.2f));
        Log("Clicking (3, 2)");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(3, 2), 0.2f));
        Log("Clicking (3, 2) again");
        yield return new WaitForSeconds(0.5f);
        groundBallManager.PerformGroundInterceptionDiceRoll(1);
        Log("Performing dice roll 1 for the first interceptor");
        yield return new WaitForSeconds(0.5f);
        groundBallManager.PerformGroundInterceptionDiceRoll(6);
        Log("Performing dice roll 6 for the second interceptor");
        yield return new WaitForSeconds(2f);
        Log("Wait for the ball to move");
        AvailabilityCheckResult availabilityCheck = AssertCorrectAvailabilityAnyOtherScenario();
        AssertTrue(
            availabilityCheck.passed,
            "Action Availability after Interception (Any Other Scenario)",
            true,
            availabilityCheck.ToString()
        );
        AssertTrue(
            MatchManager.Instance.teamInAttack == MatchManager.TeamInAttack.Away
            , "home team is in attack after ball movement"
            , MatchManager.TeamInAttack.Away
            , MatchManager.Instance.teamInAttack
        );
        AssertTrue(
            MatchManager.Instance.attackHasPossession
            , "Attack has no possession after ball movement"
            , true
            , MatchManager.Instance.attackHasPossession
        );
        var interceptor = PlayerToken.GetPlayerTokenByName("Paterson");
        AssertTrue(
            MatchManager.Instance.LastTokenToTouchTheBallOnPurpose == interceptor,
            "Paterson should be the last to touch the ball",
            interceptor,
            MatchManager.Instance.LastTokenToTouchTheBallOnPurpose
        );
        // int passes = MatchManager.Instance.gameData.stats.GetPlayerStats("Cafferata").passesAttempted;
        AssertTrue(
            MatchManager.Instance.gameData.stats.GetPlayerStats("Cafferata").passesAttempted == 1,
            "Cafferata Should have 1 pass attempted",
            1,
            MatchManager.Instance.gameData.stats.GetPlayerStats("Cafferata").passesAttempted
        );
        AssertTrue(
            MatchManager.Instance.gameData.stats.GetPlayerStats("Cafferata").passesCompleted == 0,
            "Cafferata Should have 0 pass completed",
            0,
            MatchManager.Instance.gameData.stats.GetPlayerStats("Cafferata").passesCompleted
        );
        AssertTrue(
            MatchManager.Instance.gameData.stats.GetPlayerStats("Paterson").interceptionsAttempted == 1,
            "Paterson Should have 1 interception attempted",
            1,
            MatchManager.Instance.gameData.stats.GetPlayerStats("Paterson").interceptionsAttempted
        );
        AssertTrue(
            MatchManager.Instance.gameData.stats.GetPlayerStats("Paterson").interceptionsMade == 1,
            "Paterson Should have 1 interception made",
            1,
            MatchManager.Instance.gameData.stats.GetPlayerStats("Paterson").interceptionsMade
        );
        float patersonExpectedXRecovery = CalculateExpectedRecoveryFromTackling(interceptor.tackling);
        AssertApproximately(
            MatchManager.Instance.gameData.stats.GetPlayerStats("Paterson").xRecoveries,
            patersonExpectedXRecovery,
            0.0001f,
            "Paterson should record xRecovery for the actual ground-pass interception attempt");
        AssertTrue(
            MatchManager.Instance.gameData.stats.GetPlayerStats("Paterson").possessionWon == 1,
            "Paterson should have 1 possession won from the interception",
            1,
            MatchManager.Instance.gameData.stats.GetPlayerStats("Paterson").possessionWon
        );
        AssertTrue(
            MatchManager.Instance.gameData.stats.GetPlayerStats("Cafferata").possessionLost == 1,
            "Cafferata should have 1 possession lost from the intercepted pass",
            1,
            MatchManager.Instance.gameData.stats.GetPlayerStats("Cafferata").possessionLost
        );

        LogFooterofTest("Ground Ball - Dangerous Pass - No Interception");
    }

    private IEnumerator Scenario_005_GroundBall_0004_Pass_to_Player_FTP_No_interceptions()
    {
        yield return new WaitForSeconds(3f); // Allow scene to stabilize

        Log("▶️ Starting test scenario: 'Ground Ball - Pass to Player - FTP with No Interceptions'");

        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Alpha2, 0.05f));
        Log("Pressing 2");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Space, 0.05f));
        Log("Pressing Space");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.P, 0.05f));
        Log("Pressing P");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(-6, -6), 0.2f));
        Log("Clicking (-6, -6)");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(-6, -6), 0.2f));
        Log("Clicking (-6, -6) again");
        yield return new WaitForSeconds(3f); // for the ball to move
        Log("Wait for the ball to move");
        AvailabilityCheckResult availabilityCheck = AssertCorrectAvailabilityAfterGBToPlayer();
        AssertTrue(
            availabilityCheck.passed,
            "Action Availability after Pass to Player",
            true,
            availabilityCheck.ToString()
        );
        AssertTrue(
            MatchManager.Instance.gameData.stats.GetPlayerStats("Cafferata").passesAttempted == 1,
            "Cafferata Should have 1 pass attempted",
            1,
            MatchManager.Instance.gameData.stats.GetPlayerStats("Cafferata").passesAttempted
        );
        AssertTrue(
            MatchManager.Instance.gameData.stats.GetPlayerStats("Cafferata").passesCompleted == 1,
            "Cafferata Should have 1 pass completed",
            0,
            MatchManager.Instance.gameData.stats.GetPlayerStats("Cafferata").passesCompleted
        );
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.F, 0.5f));
        Log("Pressing F");
        AvailabilityCheckResult availabilityFTPInit = AssertCorrectWaitinginFTPInitialization();
        AssertTrue(
            availabilityFTPInit.passed,
            "FTP subsystem waiting status at Initialization",
            true,
            availabilityFTPInit.ToString()
        );
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(-1, -9), 0.2f));
        Log("Clicking (-1, -9)");
        AvailabilityCheckResult availabilitysecondFTPInit = AssertCorrectWaitinginFTPInitialization();
        AssertTrue(
            availabilitysecondFTPInit.passed,
            "FTP subsystem waiting status at after Target CLick",
            true,
            availabilitysecondFTPInit.ToString()
        );
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(-1, -9), 0.2f));
        Log("Clicking (-1, -9) again to Lock Target");
        AvailabilityCheckResult availabilityFTPTargetLocked = AssertCorrectWaitinginFTPAttackerMovementPhase();
        AssertTrue(
            availabilityFTPTargetLocked.passed,
            "FTP subsystem waiting status at after Target Confirmation",
            true,
            availabilityFTPTargetLocked.ToString()
        );
        AssertTrue(
            MatchManager.Instance.gameData.stats.GetPlayerStats("André Noruega").passesAttempted == 1,
            "Noruega Should have 1 pass attempted",
            1,
            MatchManager.Instance.gameData.stats.GetPlayerStats("André Noruega").passesAttempted
        );
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(-8, -8), 0.2f));
        Log("Clicking (-8, -8) ON Ulisses");
        AvailabilityCheckResult availabilityFTPTargetLocked2 = AssertCorrectWaitinginFTPAttackerMovementPhase();
        AssertTrue(
            availabilityFTPTargetLocked2.passed,
            "FTP subsystem waiting status at after Selecting a Valid Attacker",
            true,
            availabilityFTPTargetLocked2.ToString()
        );
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(-7, -8), 0.2f));
        Log("Clicking (-7, -8) again to Move Ulisses");
        yield return new WaitForSeconds(1f);
        AvailabilityCheckResult availabilityFTPDefense = AssertCorrectWaitinginFTPDefenderMovementPhase();
        AssertTrue(
            availabilityFTPDefense.passed,
            "FTP subsystem waiting status at after Selecting a Valid Attacker Destination",
            true,
            availabilityFTPDefense.ToString()
        );
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(1, -10), 0.2f));
        Log("Clicking (1, -10) Select Delgado");
        AvailabilityCheckResult availabilityFTPDefense1 = AssertCorrectWaitinginFTPDefenderMovementPhase();
        AssertTrue(
            availabilityFTPDefense1.passed,
            "FTP subsystem waiting status at after Defender Selection",
            true,
            availabilityFTPDefense1.ToString()
        );
        AssertTrue(
            firstTimePassManager.isWaitingForDefenderMove,
            "FTP subsystem waiting status for Move at after Defender Selection",
            true,
            firstTimePassManager.isWaitingForDefenderMove
        );
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(1, -8), 0.2f));
        Log("Clicking (1, -8) Impossible Move");
        AvailabilityCheckResult availabilityFTPDefense2 = AssertCorrectWaitinginFTPDefenderMovementPhase();
        AssertTrue(
            availabilityFTPDefense2.passed,
            "FTP subsystem waiting status at after Target Unreachable Destination Hex Clicked",
            true,
            availabilityFTPDefense2.ToString()
        );
        AssertTrue(
            !firstTimePassManager.isWaitingForDefenderMove,
            "FTP subsystem waiting status for Move at after Defender Selection",
            false,
            firstTimePassManager.isWaitingForDefenderMove
        );
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(1, -10), 0.2f));
        Log("Clicking (1, -10) Reselect Delgado");
        AvailabilityCheckResult availabilityFTPDefense3 = AssertCorrectWaitinginFTPDefenderMovementPhase();
        AssertTrue(
            availabilityFTPDefense3.passed,
            "FTP subsystem waiting status at after Once again Defender Selection",
            true,
            availabilityFTPDefense3.ToString()
        );
        AssertTrue(
            firstTimePassManager.isWaitingForDefenderMove,
            "FTP subsystem waiting status for Move at Once again Defender Selection",
            true,
            firstTimePassManager.isWaitingForDefenderMove
        );
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(1, -9), 0.2f));
        Log("Clicking (1, -9) Move Delgado");
        yield return new WaitForSeconds(1f);
        yield return new WaitForSeconds(3f);
        Log("Wait for the ball to move");
        AvailabilityCheckResult ftpballMoved = AssertCorrectAvailabilityAfterFTPToSpace();
        AssertTrue(
            ftpballMoved.passed,
            "FTP subsystem waiting status at after After Moving Delgado",
            true,
            ftpballMoved.ToString()
        );

        LogFooterofTest("Ground Ball - Pass to Player - FTP with No Interceptions'");
    }
    
    private IEnumerator Scenario_006_GroundBall_0005_Pass_to_Player_FTP_To_Player()
    {
        yield return new WaitForSeconds(3f); // Allow scene to stabilize

        Log("▶️ Starting test scenario: 'Ground Ball - Pass to Player - FTP To Player'");

        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Alpha2, 0.05f));
        Log("Pressing 2");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Space, 0.05f));
        Log("Pressing Space");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.P, 0.05f));
        Log("Pressing P");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(-6, -6), 0.2f));
        Log("Clicking (-6, -6)");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(-6, -6), 0.2f));
        Log("Clicking (-6, -6) again");
        yield return new WaitForSeconds(3f); // for the ball to move
        Log("Wait for the ball to move");
        AvailabilityCheckResult availabilityAfterGbToPlayer = AssertCorrectAvailabilityAfterGBToPlayer();
        AssertTrue(
            availabilityAfterGbToPlayer.passed,
            "Action Availability after kickoff Ground Ball to Player before FTP To Player",
            true,
            availabilityAfterGbToPlayer.ToString()
        );
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.F, 0.5f));
        Log("Pressing F");
        AvailabilityCheckResult availabilityFTPInit = AssertCorrectWaitinginFTPInitialization();
        AssertTrue(
            availabilityFTPInit.passed,
            "FTP subsystem waiting status at Initialization",
            true,
            availabilityFTPInit.ToString()
        );
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(-4, -4), 0.2f));
        Log("Clicking (-4, -4)");
        AvailabilityCheckResult availabilitysecondFTPInit = AssertCorrectWaitinginFTPInitialization();
        AssertTrue(
            availabilitysecondFTPInit.passed,
            "FTP subsystem waiting status at after Target CLick",
            true,
            availabilitysecondFTPInit.ToString()
        );
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(-4, -4), 0.2f));
        Log("Clicking (-4, -4) again to Lock Target");
        AvailabilityCheckResult availabilityFTPTargetLocked = AssertCorrectWaitinginFTPAttackerMovementPhase();
        AssertTrue(
            availabilityFTPTargetLocked.passed,
            "FTP subsystem waiting status at after Target Confirmation",
            true,
            availabilityFTPTargetLocked.ToString()
        );
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(-6, -6), 0.5f));
        Log("Clicking (-6, -6) On Noruega (passer)");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(-6, -7), 0.2f));
        Log("Clicking (-6, -7) Move Noruega (passer)");
        yield return new WaitForSeconds(1f);
        AvailabilityCheckResult availabilityFTPDefense = AssertCorrectWaitinginFTPDefenderMovementPhase();
        AssertTrue(
            availabilityFTPDefense.passed,
            "FTP subsystem waiting status at after Selecting a Valid Attacker Destination",
            true,
            availabilityFTPDefense.ToString()
        );
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(1, -10), 0.2f));
        Log("Clicking (1, -10) Select Delgado");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(1, -9), 0.2f));
        Log("Clicking (1, -9) Move Delgado");
        yield return new WaitForSeconds(3f);
        Log("Wait for the ball to move");
        AvailabilityCheckResult ftpballMoved = AssertCorrectAvailabilityAfterFTPToPlayer();
        AssertTrue(
            ftpballMoved.passed,
            "FTP subsystem waiting status at after After Moving Delgado",
            true,
            ftpballMoved.ToString()
        );

        LogFooterofTest("Ground Ball - Pass to Player - FTP To Player");
    }
    
    private IEnumerator Scenario_007_GroundBall_0006_Swith_between_options_before_Committing()
    {
        yield return new WaitForSeconds(3f); // Allow scene to stabilize

        Log("▶️ Starting test scenario: 'Ground ball to Player, FTP - M - FTP Commitment'");

        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Alpha2, 0.05f));
        Log("Pressing 2");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Space, 0.05f));
        Log("Pressing Space");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.P, 0.05f));
        Log("Pressing P");
        AssertTrue(
            !movementPhaseManager.isAvailable
            , "MPM is not Available after pressing P form kick Off "
            , false
            , movementPhaseManager.isAvailable
        );
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(-6, -6), 0.2f));
        Log("Clicking (-6, -6)");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(-6, -6), 0.2f));
        Log("Clicking (-6, -6) again");
        yield return new WaitForSeconds(3f); // for the ball to move
        Log("Wait for the ball to move");
        AvailabilityCheckResult availabilityAfterGbToPlayer = AssertCorrectAvailabilityAfterGBToPlayer();
        AssertTrue(
            availabilityAfterGbToPlayer.passed,
            "Action Availability after kickoff Ground Ball to Player before FTP option switching",
            true,
            availabilityAfterGbToPlayer.ToString()
        );
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.F, 0.5f));
        Log("Pressing F");
        AvailabilityCheckResult availabilityFTPInit = AssertCorrectWaitinginFTPInitialization();
        AssertTrue(
            availabilityFTPInit.passed,
            "FTP subsystem waiting status at Initialization",
            true,
            availabilityFTPInit.ToString()
        );
        AssertTrue(
            movementPhaseManager.isAvailable
            , "MPM is Available after Selecting FTP and not committing"
            , true
            , movementPhaseManager.isAvailable
        );
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(-4, -4), 0.2f));
        Log("Clicking (-4, -4)");
        AvailabilityCheckResult availabilitysecondFTPInit = AssertCorrectWaitinginFTPInitialization();
        AssertTrue(
            availabilitysecondFTPInit.passed,
            "FTP subsystem waiting status at after Target CLick",
            true,
            availabilitysecondFTPInit.ToString()
        );
        AssertTrue(
            movementPhaseManager.isAvailable
            , "MPM is Available after Selecting FTP target and still not committing"
            , true
            , movementPhaseManager.isAvailable
        );
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.M, 0.5f));
        Log("Switch Selection to Movement");
        AssertTrue(
            !movementPhaseManager.isAvailable
            , "MPM is not Available after Selecting it by changing or MPM"
            , false
            , movementPhaseManager.isAvailable
        );
        AssertTrue(
            firstTimePassManager.isAvailable
            , "FTP SHould be available while MP was selected"
            , true
            , firstTimePassManager.isAvailable
        );
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.F, 0.5f));
        Log("Switch Selection Back to FTP");
        yield return new WaitForSeconds(1f);
        AssertTrue(
            movementPhaseManager.isAvailable
            , "MPM is Available after Re Selecting FTP target"
            , true
            , movementPhaseManager.isAvailable
        );
        AvailabilityCheckResult availabilitysecondFTPInit2 = AssertCorrectWaitinginFTPInitialization();
        AssertTrue(
            availabilitysecondFTPInit2.passed,
            "FTP subsystem waiting status at after Target CLick",
            true,
            availabilitysecondFTPInit2.ToString()
        );
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(-5, -5), 0.2f));
        Log("Clicking (-5, -5) to select target");
        AvailabilityCheckResult availabilitysecondFTPInit1 = AssertCorrectWaitinginFTPInitialization();
        AssertTrue(
            availabilitysecondFTPInit1.passed,
            "FTP subsystem waiting status at after Target CLick",
            true,
            availabilitysecondFTPInit1.ToString()
        );
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(-5, -5), 0.2f));
        Log("Clicking (-5, -5) To confirm the target");
        AvailabilityCheckResult availabilitysecondFTPTargetSelected = AssertCorrectWaitinginFTPAttackerMovementPhase();
        AssertTrue(
            availabilitysecondFTPTargetSelected.passed,
            "FTP subsystem waiting status at after Target CLick",
            true,
            availabilitysecondFTPTargetSelected.ToString()
        );
        AssertTrue(
            !movementPhaseManager.isAvailable
            , "MPM is NOT Available after Confirming FTP target"
            , false
            , movementPhaseManager.isAvailable
        );
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(-8, -8), 0.2f));
        Log("Clicking (-8, -8) On Ulisses");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(-8, -7), 0.2f));
        Log("Clicking (-8, -7) Move Ulisses");
        yield return new WaitForSeconds(1f);
        AvailabilityCheckResult availabilityFTPDefense = AssertCorrectWaitinginFTPDefenderMovementPhase();
        AssertTrue(
            availabilityFTPDefense.passed,
            "FTP subsystem waiting status at after Selecting a Valid Attacker Destination",
            true,
            availabilityFTPDefense.ToString()
        );
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(1, -10), 0.2f));
        Log("Clicking (1, -10) Select Delgado");
        AvailabilityCheckResult availabilityFTPDefense3 = AssertCorrectWaitinginFTPDefenderMovementPhase();
        AssertTrue(
            availabilityFTPDefense3.passed,
            "FTP subsystem waiting status at after Once again Defender Selection",
            true,
            availabilityFTPDefense3.ToString()
        );
        AssertTrue(
            firstTimePassManager.isWaitingForDefenderMove,
            "FTP subsystem waiting status for Move at Once again Defender Selection",
            true,
            firstTimePassManager.isWaitingForDefenderMove
        );
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(1, -9), 0.2f));
        Log("Clicking (1, -9) Move Delgado");
        yield return new WaitForSeconds(1f);
        yield return new WaitForSeconds(3f);
        Log("Wait for the ball to move");
        AvailabilityCheckResult ftpballMoved = AssertCorrectAvailabilityAfterFTPToSpace();
        AssertTrue(
            ftpballMoved.passed,
            "FTP subsystem waiting status at after After Moving Delgado",
            true,
            ftpballMoved.ToString()
        );

        LogFooterofTest("Ground ball to Player, FTP - M - FTP Commitment");

    }

    private IEnumerator Scenario_007a_FirstTimePass_Difficulty1_Hover_Preview_And_Commitment()
    {
        Log("▶️ Starting test scenario: 'FTP Difficulty 1 Hover Preview And Commitment'");
        yield return StartCoroutine(PrepareFtpAvailabilityFromKickoff(1));

        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.F, 0.2f));
        Log("Pressing F");
        AvailabilityCheckResult ftpInitialization = AssertCorrectWaitinginFTPInitialization();
        AssertTrue(
            ftpInitialization.passed,
            "FTP subsystem waiting status at Initialization on difficulty 1",
            true,
            ftpInitialization.ToString()
        );

        HexCell outOfRangeHex = RequireHex(FindFirstOutOfRangeFtpTarget(), "Difficulty 1 FTP test should find an out-of-range hover target.");
        HexCell committedTargetHex = RequireHex(FindFirstSafeFtpTarget(), "Difficulty 1 FTP test should find a safe FTP target.");
        HexCell alternateTargetHex = RequireHex(FindFirstSafeFtpTarget(committedTargetHex), "Difficulty 1 FTP test should find an alternate safe FTP target.");
        FtpBlockedHoverSetup blockedHoverSetup = FindFtpBlockedHoverSetup(committedTargetHex);
        AssertTrue(blockedHoverSetup != null, "Difficulty 1 FTP test should be able to create a blocked hover target.");
        if (blockedHoverSetup == null)
        {
            yield break;
        }

        SimulateFirstTimePassHover(outOfRangeHex);
        yield return null;
        AssertTrue(
            firstTimePassManager.GetInstructions().Contains("out of range"),
            "FTP hover should explain the out-of-range validation failure",
            true,
            firstTimePassManager.GetInstructions()
        );

        SetTokenHexForTest(blockedHoverSetup.defenderToken, blockedHoverSetup.blockingHex);
        SimulateFirstTimePassHover(blockedHoverSetup.targetHex);
        yield return null;
        AssertTrue(
            firstTimePassManager.GetInstructions().Contains("blocked by defender"),
            "FTP hover should explain the blocked-by-defender validation failure",
            true,
            firstTimePassManager.GetInstructions()
        );
        SetTokenHexForTest(blockedHoverSetup.defenderToken, blockedHoverSetup.originalDefenderHex);

        SimulateFirstTimePassHover(committedTargetHex);
        yield return null;
        AssertTrue(
            hexgrid.highlightedHexes.Contains(committedTargetHex),
            "FTP hover should highlight the hovered safe target in difficulty 1",
            true,
            hexgrid.highlightedHexes.Contains(committedTargetHex)
        );

        yield return StartCoroutine(gameInputManager.DelayedClick(ToClickCoordinates(committedTargetHex), 0.2f));
        Log($"Clicking {committedTargetHex.coordinates} to commit the FTP target");
        AssertTrue(
            firstTimePassManager.currentTargetHex == committedTargetHex,
            "FTP should lock the clicked target after the first click in difficulty 1",
            committedTargetHex,
            firstTimePassManager.currentTargetHex
        );
        AssertColorApproximately(
            committedTargetHex.hexRenderer.material.color,
            new Color(1f, 0.55f, 0f, 1f),
            0.06f,
            "FTP committed target should remain orange in difficulty 1.");

        SimulateFirstTimePassHover(alternateTargetHex);
        yield return null;
        AssertColorApproximately(
            committedTargetHex.hexRenderer.material.color,
            new Color(1f, 0.55f, 0f, 1f),
            0.06f,
            "FTP committed target should stay orange while previewing another path in difficulty 1.");

        yield return StartCoroutine(gameInputManager.DelayedClick(ToClickCoordinates(committedTargetHex), 0.2f));
        Log($"Clicking {committedTargetHex.coordinates} again to confirm the FTP target");
        AvailabilityCheckResult ftpAttackerPhase = AssertCorrectWaitinginFTPAttackerMovementPhase();
        AssertTrue(
            ftpAttackerPhase.passed,
            "FTP should enter attacker movement after confirming the target in difficulty 1",
            true,
            ftpAttackerPhase.ToString()
        );

        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        Log("Pressing X to skip attacker movement");
        yield return StartCoroutine(WaitForFtpDefenderMovementPhase());
        AvailabilityCheckResult ftpDefenderPhase = AssertCorrectWaitinginFTPDefenderMovementPhase();
        AssertTrue(
            ftpDefenderPhase.passed,
            "FTP should enter defender movement after skipping the attacker move",
            true,
            ftpDefenderPhase.ToString()
        );

        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        Log("Pressing X to skip defender movement");
        yield return new WaitForSeconds(3f);

        AvailabilityCheckResult finalAvailability = committedTargetHex.isAttackOccupied
            ? AssertCorrectAvailabilityAfterFTPToPlayer()
            : AssertCorrectAvailabilityAfterFTPToSpace();
        AssertTrue(
            finalAvailability.passed,
            "FTP should resolve cleanly after both movement phases are skipped in difficulty 1",
            true,
            finalAvailability.ToString()
        );

        LogFooterofTest("FTP Difficulty 1 Hover Preview And Commitment");
    }

    private IEnumerator Scenario_007b_FirstTimePass_Difficulty3_Commits_On_F_And_First_Click()
    {
        Log("▶️ Starting test scenario: 'FTP Difficulty 3 Commits On F And First Click'");
        yield return StartCoroutine(PrepareFtpAvailabilityFromKickoff(3));

        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.F, 0.2f));
        Log("Pressing F");
        AssertTrue(
            firstTimePassManager.isActivated,
            "FTP should activate immediately on F in difficulty 3",
            true,
            firstTimePassManager.isActivated
        );
        AssertTrue(
            firstTimePassManager.isAwaitingTargetSelection,
            "FTP should be awaiting a target immediately after F in difficulty 3",
            true,
            firstTimePassManager.isAwaitingTargetSelection
        );
        AssertTrue(
            !movementPhaseManager.isAvailable,
            "Movement Phase should not remain selectable after FTP commits on F in difficulty 3",
            false,
            movementPhaseManager.isAvailable
        );

        HexCell safeTargetHex = RequireHex(FindFirstSafeFtpTarget(), "Difficulty 3 FTP test should find a safe FTP target.");
        yield return StartCoroutine(gameInputManager.DelayedClick(ToClickCoordinates(safeTargetHex), 0.2f));
        Log($"Clicking {safeTargetHex.coordinates} once to confirm the FTP target in difficulty 3");

        AvailabilityCheckResult ftpAttackerPhase = AssertCorrectWaitinginFTPAttackerMovementPhase();
        AssertTrue(
            ftpAttackerPhase.passed,
            "Difficulty 3 FTP should advance to attacker movement on the first valid target click",
            true,
            ftpAttackerPhase.ToString()
        );

        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        Log("Pressing X to skip attacker movement");
        AvailabilityCheckResult ftpDefenderPhase = AssertCorrectWaitinginFTPDefenderMovementPhase();
        AssertTrue(
            ftpDefenderPhase.passed,
            "FTP should enter defender movement after skipping the attacker move in difficulty 3",
            true,
            ftpDefenderPhase.ToString()
        );

        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        Log("Pressing X to skip defender movement");
        yield return new WaitForSeconds(3f);

        AvailabilityCheckResult finalAvailability = safeTargetHex.isAttackOccupied
            ? AssertCorrectAvailabilityAfterFTPToPlayer()
            : AssertCorrectAvailabilityAfterFTPToSpace();
        AssertTrue(
            finalAvailability.passed,
            "Difficulty 3 FTP should resolve cleanly after the first-click confirmation flow",
            true,
            finalAvailability.ToString()
        );

        LogFooterofTest("FTP Difficulty 3 Commits On F And First Click");
    }

    private IEnumerator Scenario_007c_FirstTimePass_Defender_Path_Block_Intercepts_On_5()
    {
        Log("▶️ Starting test scenario: 'FTP Defender Path Block Intercepts On 5+'");
        yield return StartCoroutine(PrepareFtpAvailabilityFromKickoff(2));

        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.F, 0.2f));
        Log("Pressing F");
        AvailabilityCheckResult ftpInitialization = AssertCorrectWaitinginFTPInitialization();
        AssertTrue(
            ftpInitialization.passed,
            "FTP subsystem waiting status at Initialization before the blocker interception test",
            true,
            ftpInitialization.ToString()
        );

        PlayerToken attackerToken = RequirePlayerToken("Cafferata");
        PlayerToken defenderToken = RequirePlayerToken("Delgado");
        HexCell ftpTargetHex = RequireHex(
            hexgrid.GetHexCellAt(new Vector3Int(0, 0, -9)),
            "FTP blocker interception test should find target hex (0, -9).");
        HexCell attackerSourceHex = RequireHex(attackerToken.GetCurrentHex(), "Cafferata should be on the pitch for the FTP blocker interception test.");
        HexCell attackerDestinationHex = RequireHex(
            hexgrid.GetHexCellAt(new Vector3Int(0, 0, 1)),
            "FTP blocker interception test should find Cafferata's destination hex (0, 1).");
        HexCell defenderSourceHex = RequireHex(defenderToken.GetCurrentHex(), "Delgado should be on the pitch for the FTP blocker interception test.");
        HexCell defenderDestinationHex = RequireHex(
            hexgrid.GetHexCellAt(new Vector3Int(0, 0, -9)),
            "FTP blocker interception test should find Delgado's destination hex (0, -9).");

        AssertTrue(
            attackerSourceHex.coordinates == new Vector3Int(0, 0, 0),
            "Cafferata should start from (0, 0) in the FTP blocker interception test.",
            new Vector3Int(0, 0, 0),
            attackerSourceHex.coordinates
        );
        AssertTrue(
            defenderSourceHex.coordinates == new Vector3Int(1, 0, -10),
            "Delgado should start from (1, -10) in the FTP blocker interception test.",
            new Vector3Int(1, 0, -10),
            defenderSourceHex.coordinates
        );

        yield return StartCoroutine(gameInputManager.DelayedClick(ToClickCoordinates(ftpTargetHex), 0.2f));
        Log($"Clicking {ftpTargetHex.coordinates} to select the FTP target");
        yield return StartCoroutine(gameInputManager.DelayedClick(ToClickCoordinates(ftpTargetHex), 0.2f));
        Log($"Clicking {ftpTargetHex.coordinates} again to confirm the FTP target");
        AvailabilityCheckResult ftpAttackerPhase = AssertCorrectWaitinginFTPAttackerMovementPhase();
        AssertTrue(
            ftpAttackerPhase.passed,
            "FTP should enter attacker movement after confirming the target for the blocker interception test",
            true,
            ftpAttackerPhase.ToString()
        );

        PlayerToken ftpPasser = MatchManager.Instance.LastTokenToTouchTheBallOnPurpose;
        AssertTrue(ftpPasser != null, "FTP blocker interception test should have a tracked passer.");
        MatchManager.PlayerStats defenderStatsBefore = MatchManager.Instance.gameData.stats.GetPlayerStats(defenderToken.playerName);
        MatchManager.PlayerStats passerStatsBefore = MatchManager.Instance.gameData.stats.GetPlayerStats(ftpPasser.playerName);
        int interceptionsAttemptedBefore = defenderStatsBefore.interceptionsAttempted;
        int interceptionsMadeBefore = defenderStatsBefore.interceptionsMade;
        int possessionWonBefore = defenderStatsBefore.possessionWon;
        int passerPossessionLostBefore = passerStatsBefore.possessionLost;
        float xRecoveryBefore = defenderStatsBefore.xRecoveries;

        yield return StartCoroutine(gameInputManager.DelayedClick(ToClickCoordinates(attackerSourceHex), 0.2f));
        Log($"Clicking {attackerSourceHex.coordinates} to select {attackerToken.playerName}");
        yield return StartCoroutine(gameInputManager.DelayedClick(ToClickCoordinates(attackerDestinationHex), 0.2f));
        Log($"Clicking {attackerDestinationHex.coordinates} to move {attackerToken.playerName} before the blocker interception");
        yield return StartCoroutine(WaitForFtpDefenderMovementPhase());

        AvailabilityCheckResult ftpDefenderPhase = AssertCorrectWaitinginFTPDefenderMovementPhase();
        AssertTrue(
            ftpDefenderPhase.passed,
            "FTP should enter defender movement before the blocker interception test",
            true,
            ftpDefenderPhase.ToString()
        );

        yield return StartCoroutine(gameInputManager.DelayedClick(ToClickCoordinates(defenderSourceHex), 0.2f));
        Log($"Clicking {defenderSourceHex.coordinates} to select {defenderToken.playerName}");
        AssertTrue(
            firstTimePassManager.isWaitingForDefenderMove,
            "FTP should be waiting for the selected defender to move",
            true,
            firstTimePassManager.isWaitingForDefenderMove
        );

        yield return StartCoroutine(gameInputManager.DelayedClick(ToClickCoordinates(defenderDestinationHex), 0.2f));
        Log($"Clicking {defenderDestinationHex.coordinates} to move {defenderToken.playerName} onto the FTP path");
        yield return new WaitForSeconds(1f);

        AssertTrue(
            firstTimePassManager.isWaitingForDiceRoll,
            "FTP should enter the interception roll phase after a defender moves onto the path",
            true,
            firstTimePassManager.isWaitingForDiceRoll
        );
        AssertTrue(
            GetFirstTimePassInterceptionCandidateCount() == 1,
            "FTP blocker interception test should produce exactly one defender entry in the interception list",
            1,
            GetFirstTimePassInterceptionCandidateCount()
        );
        AssertTrue(
            firstTimePassManager.GetInstructions().Contains("5+"),
            "A low-tackling defender blocking the FTP path should get a 5+ interception instruction",
            true,
            firstTimePassManager.GetInstructions()
        );

        PerformRiggedFirstTimePassInterceptionRoll(5);
        yield return new WaitForSeconds(3f);

        AvailabilityCheckResult anyOtherAvailability = AssertCorrectAvailabilityAnyOtherScenario();
        AssertTrue(
            anyOtherAvailability.passed,
            "FTP blocker interception should hand play to AnyOtherScenario",
            true,
            anyOtherAvailability.ToString()
        );

        MatchManager.PlayerStats defenderStatsAfter = MatchManager.Instance.gameData.stats.GetPlayerStats(defenderToken.playerName);
        MatchManager.PlayerStats passerStatsAfter = MatchManager.Instance.gameData.stats.GetPlayerStats(ftpPasser.playerName);
        AssertTrue(
            defenderStatsAfter.interceptionsAttempted == interceptionsAttemptedBefore + 1,
            "The blocking defender should log exactly one interception attempt",
            interceptionsAttemptedBefore + 1,
            defenderStatsAfter.interceptionsAttempted
        );
        AssertTrue(
            defenderStatsAfter.interceptionsMade == interceptionsMadeBefore + 1,
            "The blocking defender should log exactly one successful interception",
            interceptionsMadeBefore + 1,
            defenderStatsAfter.interceptionsMade
        );
        AssertTrue(
            defenderStatsAfter.possessionWon == possessionWonBefore + 1,
            "The blocking defender should log one possession won from the FTP interception",
            possessionWonBefore + 1,
            defenderStatsAfter.possessionWon
        );
        AssertTrue(
            passerStatsAfter.possessionLost == passerPossessionLostBefore + 1,
            "The FTP passer should log one possession lost from the successful blocker interception",
            passerPossessionLostBefore + 1,
            passerStatsAfter.possessionLost
        );
        AssertApproximately(
            defenderStatsAfter.xRecoveries,
            xRecoveryBefore + CalculateExpectedRecoveryFromTackling(defenderToken.tackling, 5),
            0.0001f,
            "The blocking defender should log xRecovery using the FTP 5+ interception rule");

        LogFooterofTest("FTP Defender Path Block Intercepts On 5+");
    }

    private IEnumerator Scenario_007d_FirstTimePass_Defender_ZOI_Recalculation_Intercepts_On_6()
    {
        Log("▶️ Starting test scenario: 'FTP Defender ZOI Recalculation Intercepts On 6'");
        yield return StartCoroutine(PrepareFtpAvailabilityFromKickoff(2));

        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.F, 0.2f));
        Log("Pressing F");
        AvailabilityCheckResult ftpInitialization = AssertCorrectWaitinginFTPInitialization();
        AssertTrue(
            ftpInitialization.passed,
            "FTP subsystem waiting status at Initialization before the ZOI interception test",
            true,
            ftpInitialization.ToString()
        );

        PlayerToken attackerToken = RequirePlayerToken("Cafferata");
        PlayerToken defenderToken = RequirePlayerToken("Delgado");
        HexCell ftpTargetHex = RequireHex(
            hexgrid.GetHexCellAt(new Vector3Int(-1, 0, -9)),
            "FTP ZOI interception test should find target hex (-1, -9).");
        HexCell attackerSourceHex = RequireHex(attackerToken.GetCurrentHex(), "Cafferata should be on the pitch for the FTP ZOI interception test.");
        HexCell attackerDestinationHex = RequireHex(
            hexgrid.GetHexCellAt(new Vector3Int(0, 0, 1)),
            "FTP ZOI interception test should find Cafferata's destination hex (0, 1).");
        HexCell defenderSourceHex = RequireHex(defenderToken.GetCurrentHex(), "Delgado should be on the pitch for the FTP ZOI interception test.");
        HexCell defenderDestinationHex = RequireHex(
            hexgrid.GetHexCellAt(new Vector3Int(0, 0, -9)),
            "FTP ZOI interception test should find Delgado's destination hex (0, -9).");

        AssertTrue(
            attackerSourceHex.coordinates == new Vector3Int(0, 0, 0),
            "Cafferata should start from (0, 0) in the FTP ZOI interception test.",
            new Vector3Int(0, 0, 0),
            attackerSourceHex.coordinates
        );
        AssertTrue(
            defenderSourceHex.coordinates == new Vector3Int(1, 0, -10),
            "Delgado should start from (1, -10) in the FTP ZOI interception test.",
            new Vector3Int(1, 0, -10),
            defenderSourceHex.coordinates
        );

        yield return StartCoroutine(gameInputManager.DelayedClick(ToClickCoordinates(ftpTargetHex), 0.2f));
        Log($"Clicking {ftpTargetHex.coordinates} to select the FTP target");
        yield return StartCoroutine(gameInputManager.DelayedClick(ToClickCoordinates(ftpTargetHex), 0.2f));
        Log($"Clicking {ftpTargetHex.coordinates} again to confirm the FTP target");
        AvailabilityCheckResult ftpAttackerPhase = AssertCorrectWaitinginFTPAttackerMovementPhase();
        AssertTrue(
            ftpAttackerPhase.passed,
            "FTP should enter attacker movement after confirming the target for the ZOI interception test",
            true,
            ftpAttackerPhase.ToString()
        );

        PlayerToken ftpPasser = MatchManager.Instance.LastTokenToTouchTheBallOnPurpose;
        AssertTrue(ftpPasser != null, "FTP ZOI interception test should have a tracked passer.");
        MatchManager.PlayerStats defenderStatsBefore = MatchManager.Instance.gameData.stats.GetPlayerStats(defenderToken.playerName);
        MatchManager.PlayerStats passerStatsBefore = MatchManager.Instance.gameData.stats.GetPlayerStats(ftpPasser.playerName);
        int interceptionsAttemptedBefore = defenderStatsBefore.interceptionsAttempted;
        int interceptionsMadeBefore = defenderStatsBefore.interceptionsMade;
        int passerPossessionLostBefore = passerStatsBefore.possessionLost;
        float xRecoveryBefore = defenderStatsBefore.xRecoveries;

        yield return StartCoroutine(gameInputManager.DelayedClick(ToClickCoordinates(attackerSourceHex), 0.2f));
        Log($"Clicking {attackerSourceHex.coordinates} to select {attackerToken.playerName}");
        yield return StartCoroutine(gameInputManager.DelayedClick(ToClickCoordinates(attackerDestinationHex), 0.2f));
        Log($"Clicking {attackerDestinationHex.coordinates} to move {attackerToken.playerName} before the ZOI interception");
        yield return StartCoroutine(WaitForFtpDefenderMovementPhase());

        AvailabilityCheckResult ftpDefenderPhase = AssertCorrectWaitinginFTPDefenderMovementPhase();
        AssertTrue(
            ftpDefenderPhase.passed,
            "FTP should enter defender movement before the ZOI interception test",
            true,
            ftpDefenderPhase.ToString()
        );

        yield return StartCoroutine(gameInputManager.DelayedClick(ToClickCoordinates(defenderSourceHex), 0.2f));
        Log($"Clicking {defenderSourceHex.coordinates} to select {defenderToken.playerName}");
        yield return StartCoroutine(gameInputManager.DelayedClick(ToClickCoordinates(defenderDestinationHex), 0.2f));
        Log($"Clicking {defenderDestinationHex.coordinates} to move {defenderToken.playerName} into FTP interception range");
        yield return new WaitForSeconds(1f);

        AssertTrue(
            firstTimePassManager.isWaitingForDiceRoll,
            "FTP should enter the interception roll phase after a defender moves into ZOI influence",
            true,
            firstTimePassManager.isWaitingForDiceRoll
        );
        AssertTrue(
            GetFirstTimePassInterceptionCandidateCount() == 1,
            "FTP ZOI interception test should produce exactly one defender entry in the interception list",
            1,
            GetFirstTimePassInterceptionCandidateCount()
        );
        AssertTrue(
            firstTimePassManager.GetInstructions().Contains("6"),
            "A low-tackling defender entering ZOI influence should keep the standard 6 interception instruction",
            true,
            firstTimePassManager.GetInstructions()
        );

        PerformRiggedFirstTimePassInterceptionRoll(6);
        yield return new WaitForSeconds(3f);

        AvailabilityCheckResult anyOtherAvailability = AssertCorrectAvailabilityAnyOtherScenario();
        AssertTrue(
            anyOtherAvailability.passed,
            "FTP ZOI interception should hand play to AnyOtherScenario",
            true,
            anyOtherAvailability.ToString()
        );

        MatchManager.PlayerStats defenderStatsAfter = MatchManager.Instance.gameData.stats.GetPlayerStats(defenderToken.playerName);
        MatchManager.PlayerStats passerStatsAfter = MatchManager.Instance.gameData.stats.GetPlayerStats(ftpPasser.playerName);
        AssertTrue(
            defenderStatsAfter.interceptionsAttempted == interceptionsAttemptedBefore + 1,
            "The ZOI defender should log exactly one interception attempt",
            interceptionsAttemptedBefore + 1,
            defenderStatsAfter.interceptionsAttempted
        );
        AssertTrue(
            defenderStatsAfter.interceptionsMade == interceptionsMadeBefore + 1,
            "The ZOI defender should log exactly one successful interception",
            interceptionsMadeBefore + 1,
            defenderStatsAfter.interceptionsMade
        );
        AssertTrue(
            passerStatsAfter.possessionLost == passerPossessionLostBefore + 1,
            "The FTP passer should log one possession lost from the successful ZOI interception",
            passerPossessionLostBefore + 1,
            passerStatsAfter.possessionLost
        );
        AssertApproximately(
            defenderStatsAfter.xRecoveries,
            xRecoveryBefore + CalculateExpectedRecoveryFromTackling(defenderToken.tackling),
            0.0001f,
            "The ZOI defender should log xRecovery using the standard FTP interception rule");

        LogFooterofTest("FTP Defender ZOI Recalculation Intercepts On 6");
    }

    private IEnumerator Scenario_007e_FirstTimePass_Passer_Cannot_Reclaim_FTP_To_Space()
    {
        Log("▶️ Starting test scenario: 'FTP Passer Cannot Reclaim FTP To Space'");
        yield return StartCoroutine(PrepareFtpAvailabilityFromKickoff(2));

        PlayerToken ftpPasser = RequirePlayerToken("André Noruega");
        HexCell passerHexBeforeFtp = RequireHex(ftpPasser.GetCurrentHex(), "The FTP passer should start on a valid hex.");
        HexCell ftpTargetHex = RequireHex(
            FindFirstSafeFtpSpaceTargetReachableByPasser(ftpPasser),
            "FTP reclaim test should find a safe empty FTP target that the passer could otherwise reach in Movement Phase.");

        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.F, 0.2f));
        Log("Pressing F");
        AvailabilityCheckResult ftpInitialization = AssertCorrectWaitinginFTPInitialization();
        AssertTrue(
            ftpInitialization.passed,
            "FTP subsystem waiting status at Initialization before the excluded-collector test",
            true,
            ftpInitialization.ToString()
        );

        yield return StartCoroutine(gameInputManager.DelayedClick(ToClickCoordinates(ftpTargetHex), 0.2f));
        Log($"Clicking {ftpTargetHex.coordinates} to select the FTP target");
        yield return StartCoroutine(gameInputManager.DelayedClick(ToClickCoordinates(ftpTargetHex), 0.2f));
        Log($"Clicking {ftpTargetHex.coordinates} again to confirm the FTP target");
        AvailabilityCheckResult ftpAttackerPhase = AssertCorrectWaitinginFTPAttackerMovementPhase();
        AssertTrue(
            ftpAttackerPhase.passed,
            "FTP should enter attacker movement before the excluded-collector test",
            true,
            ftpAttackerPhase.ToString()
        );

        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        Log("Pressing X to skip attacker movement");
        yield return StartCoroutine(WaitForFtpDefenderMovementPhase());
        AvailabilityCheckResult ftpDefenderPhase = AssertCorrectWaitinginFTPDefenderMovementPhase();
        AssertTrue(
            ftpDefenderPhase.passed,
            "FTP should enter defender movement before the excluded-collector test",
            true,
            ftpDefenderPhase.ToString()
        );

        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        Log("Pressing X to skip defender movement");
        yield return new WaitForSeconds(3f);

        AvailabilityCheckResult ftpToSpaceAvailability = AssertCorrectAvailabilityAfterFTPToSpace();
        AssertTrue(
            ftpToSpaceAvailability.passed,
            "FTP to space should hand play to Movement Phase before the excluded-collector test",
            true,
            ftpToSpaceAvailability.ToString()
        );
        AssertTrue(
            MatchManager.Instance.hangingPassType == "ground",
            "FTP to space should leave a hanging ground pass",
            "ground",
            MatchManager.Instance.hangingPassType
        );
        AssertTrue(
            MatchManager.Instance.hangingPassExcludedCollector == ftpPasser,
            "FTP to space should exclude the FTP passer from collecting the hanging pass",
            ftpPasser,
            MatchManager.Instance.hangingPassExcludedCollector
        );
        AssertTrue(
            MatchManager.Instance.LastTokenToTouchTheBallOnPurpose == ftpPasser,
            "FTP to space should keep the passer as the last token to touch the ball on purpose",
            ftpPasser,
            MatchManager.Instance.LastTokenToTouchTheBallOnPurpose
        );

        HexCell ballHexAfterFtp = RequireHex(firstTimePassManager.ball.GetCurrentHex(), "The ball should still be on a valid hex after FTP to space.");
        AssertTrue(
            ballHexAfterFtp == ftpTargetHex,
            "FTP to space should leave the ball on the selected target hex",
            ftpTargetHex,
            ballHexAfterFtp
        );

        yield return StartCoroutine(gameInputManager.DelayedClick(ToClickCoordinates(passerHexBeforeFtp), 0.2f));
        Log($"Clicking {passerHexBeforeFtp.coordinates} to select {ftpPasser.playerName} after the FTP to space");
        AssertTrue(
            movementPhaseManager.selectedToken == ftpPasser,
            "Movement Phase should select the FTP passer for the excluded-collector test",
            ftpPasser,
            movementPhaseManager.selectedToken
        );
        AssertTrue(
            !hexgrid.highlightedHexes.Contains(ballHexAfterFtp),
            "The FTP passer should not have the hanging ball hex highlighted as a legal destination",
            false,
            hexgrid.highlightedHexes.Contains(ballHexAfterFtp)
        );
        AssertTrue(
            !movementPhaseManager.isBallPickable,
            "Movement Phase should not mark the hanging FTP ball as pickable for the excluded passer",
            false,
            movementPhaseManager.isBallPickable
        );
        bool committedStateBeforeBlockedClick = movementPhaseManager.isCommitted;

        yield return StartCoroutine(gameInputManager.DelayedClick(ToClickCoordinates(ballHexAfterFtp), 0.2f));
        Log($"Clicking {ballHexAfterFtp.coordinates} with the excluded FTP passer");
        yield return new WaitForSeconds(0.25f);

        AssertTrue(
            ftpPasser.GetCurrentHex() == passerHexBeforeFtp,
            "The excluded FTP passer should stay on the original hex after clicking the hanging ball",
            passerHexBeforeFtp,
            ftpPasser.GetCurrentHex()
        );
        AssertTrue(
            firstTimePassManager.ball.GetCurrentHex() == ballHexAfterFtp,
            "The hanging FTP ball should remain in place when the excluded passer clicks it",
            ballHexAfterFtp,
            firstTimePassManager.ball.GetCurrentHex()
        );
        AssertTrue(
            movementPhaseManager.isCommitted == committedStateBeforeBlockedClick,
            "Clicking the hanging FTP ball with the excluded passer should not change the existing Movement Phase commitment state",
            committedStateBeforeBlockedClick,
            movementPhaseManager.isCommitted
        );

        LogFooterofTest("FTP Passer Cannot Reclaim FTP To Space");
    }

    private IEnumerator Scenario_008_Stupid_Click_and_KeyPress_do_not_change_status()
    {
        yield return new WaitForSeconds(2f); // Allow scene to stabilize
        Log("▶️ Starting test scenario: Stupid Click and Key Press do not change status");
        // ✅ STEP 1: Press 2
        savedSnapshot = SaveGameStatusSnapshot();
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Alpha2, 0f));
        Log("Pressing 2");
        GameStatusSnapshot currentSnapshot = GetCurrentSnapshot();
        bool isSame = savedSnapshot.IsEqualTo(currentSnapshot, out string mismatchReason, new HashSet<string> {});
        AssertTrue(
            isSame,
            "Snapshot should match the expected game state",
            "Snapshots match",
            isSame ? "Snapshots match" : mismatchReason
        );
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Space, 0.1f));
        Log("Pressing Space");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.P, 0.1f));
        Log("Pressing P - Game is in Ground Ball Mode");
        savedSnapshot = SaveGameStatusSnapshot();
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(12, -6), 0.5f));
        Log("Clicking (12, -6)");
        GameStatusSnapshot currentSnapshot2 = GetCurrentSnapshot();
        bool isSame2 = savedSnapshot.IsEqualTo(currentSnapshot2, out string mismatchReason2, new HashSet<string> {});
        AssertTrue(
            isSame2,
            "Snapshot should match the `Pressing P - Game is in Ground Ball Mode` game state",
            "Snapshots match",
            isSame2 ? "Snapshots match" : mismatchReason2
        );
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(11, -6), 0.5f));
        Log("Clicking (11, -6)");
        GameStatusSnapshot currentSnapshot3 = GetCurrentSnapshot();
        bool isSame3 = savedSnapshot.IsEqualTo(currentSnapshot3, out string mismatchReason3, new HashSet<string> {"gbmCurrentTargetHex"});
        AssertTrue(
            isSame3,
            "Snapshot should match the `Pressing P - Game is in Ground Ball Mode` game state",
            "Snapshots match",
            isSame3 ? "Snapshots match" : mismatchReason3
        );
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, -6), 0.5f));
        Log("Clicking (10, -6)");
        groundBallManager.currentTargetHex = hexgrid.GetHexCellAt(new Vector3Int(0, 0, 0));
        GameStatusSnapshot currentSnapshot4 = GetCurrentSnapshot();
        bool isSame4 = savedSnapshot.IsEqualTo(currentSnapshot4, out string mismatchReason4, new HashSet<string> {"gbmCurrentTargetHex"});
        AssertTrue(
            isSame4,
            "Snapshot should match the `Pressing P - Game is in Ground Ball Mode` game state",
            "Snapshots match",
            isSame4 ? "Snapshots match" : mismatchReason4
        );
        LogFooterofTest("Stupid Click and Key Press do not change status");
    }

    private IEnumerator Scenario_008b_Movement_Phase_Reset_When_Switching_Action_Before_Commit()
    {
        yield return new WaitForSeconds(1.5f); // Allow scene to stabilize
        Log("▶️ Starting test scenario: MovementPhase reset when switching action before commitment");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Alpha2, 0.1f));
        Log("Pressing 2");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Space, 0.1f));
        Log("Pressing Space");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.P, 0.1f));
        Log("Pressing P");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        Log("Clicking (10, 0)");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        Log("Clicking (10, 0) again");
        yield return new WaitForSeconds(3f);
        Log("Wait for the ball to move");
        AvailabilityCheckResult availabilityCheck = AssertCorrectAvailabilityAfterGBToPlayer();
        AssertTrue(
            availabilityCheck.passed,
            "Action Availability after GB to Player",
            true,
            availabilityCheck.ToString()
        );
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        Log("Pressing X - Forfeit Attack FinalThird");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        Log("Pressing X - Forfeit Defense FinalThird");
        yield return new WaitForSeconds(0.2f);

        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.M, 0.1f));
        Log("Pressing M - Start Movement Phase");
        AssertTrue(
            movementPhaseManager.isActivated,
            "MP should be activated after pressing M",
            true,
            movementPhaseManager.isActivated
        );
        AssertTrue(
            movementPhaseManager.isAwaitingTokenSelection,
            "MP should be awaiting token selection",
            true,
            movementPhaseManager.isAwaitingTokenSelection
        );

        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(-8, -8), 0.5f));
        Log("Clicking (-8, -8), 11.Ulisses");
        AssertTrue(
            movementPhaseManager.selectedToken == PlayerToken.GetPlayerTokenByName("Ulisses"),
            "MP selected token should be Ulisses before switching action",
            PlayerToken.GetPlayerTokenByName("Ulisses"),
            movementPhaseManager.selectedToken
        );
        AssertTrue(
            movementPhaseManager.isAwaitingHexDestination,
            "MP should be awaiting hex destination after selecting Ulisses",
            true,
            movementPhaseManager.isAwaitingHexDestination
        );

        MatchManager.Instance.TriggerStandardPass();
        Log("Calling MatchManager.TriggerStandardPass() before MP commitment");
        yield return new WaitForSeconds(0.2f);

        AssertTrue(
            !movementPhaseManager.isActivated,
            "MP should no longer be activated after switching action",
            false,
            movementPhaseManager.isActivated
        );
        AssertTrue(
            movementPhaseManager.selectedToken == null,
            "MP selected token should be cleared after switching action"
        );
        AssertTrue(
            !movementPhaseManager.isAwaitingTokenSelection,
            "MP should not be awaiting token selection after switching action",
            false,
            movementPhaseManager.isAwaitingTokenSelection
        );
        AssertTrue(
            !movementPhaseManager.isAwaitingHexDestination,
            "MP should not be awaiting hex destination after switching action",
            false,
            movementPhaseManager.isAwaitingHexDestination
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Count == 0,
            "MP should have no moved tokens after switching action",
            0,
            movementPhaseManager.movedTokens.Count
        );
        AssertTrue(
            !movementPhaseManager.isCommitted,
            "MP should not be committed after switching action",
            false,
            movementPhaseManager.isCommitted
        );
        AssertTrue(
            groundBallManager.isActivated,
            "GBM should be activated after switching to Standard Pass",
            true,
            groundBallManager.isActivated
        );
        AssertTrue(
            groundBallManager.isAwaitingTargetSelection,
            "GBM should be awaiting target selection after switching to Standard Pass",
            true,
            groundBallManager.isAwaitingTargetSelection
        );

        LogFooterofTest("MovementPhase reset when switching action before commitment");
    }
    
    private IEnumerator Scenario_009_Movement_Phase_NO_interceptions_No_tackles()
    {
        yield return new WaitForSeconds(1.5f); // Allow scene to stabilize
        Log("▶️ Starting test scenario: MovementPhase No Interceptions, No Tackles");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Alpha2, 0.1f));
        Log("Pressing 2");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Space, 0.1f));
        Log("Pressing Space");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.P, 0.1f));
        Log("Pressing P - Game is in Movement Phase");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        Log("Clicking (10, 0)");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        Log("Clicking (10, 0) again");
        yield return new WaitForSeconds(3f); // for the ball to move
        Log("Wait for the ball to move");
        AvailabilityCheckResult availabilityCheck = AssertCorrectAvailabilityAfterGBToPlayer();
        AssertTrue(
            availabilityCheck.passed,
            "Action Availability after Pass to Player",
            true,
            availabilityCheck.ToString()
        );
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        Log("Pressing X - Forfeit Attack FinalThird");
        AssertTrue(
            finalThirdManager.isActivated,
            "Final Thirds should be Active now",
            true,
            finalThirdManager.isActivated
        );
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        Log("Pressing X - Forfeit Defense FinalThird");
        yield return null ; // for the token to move
        AssertTrue(
            !finalThirdManager.isActivated,
            "Final Thirds should be inactive now",
            false,
            finalThirdManager.isActivated
        );
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.M, 0.1f));
        Log("Pressing M - Game is in Movement Phase");
        AssertTrue(
            movementPhaseManager.isAwaitingTokenSelection,
            "MP SHould be waiting for Token Selection after M",
            true,
            movementPhaseManager.isAwaitingTokenSelection
        );
        AssertTrue(
            !movementPhaseManager.isAwaitingHexDestination,
            "MP Should NOT be waiting for Hex Destination after M",
            false,
            movementPhaseManager.isAwaitingHexDestination
        );
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(-8, -8), 0.5f));
        Log("Clicking (-8, -8), 11.Ulisses");
        AssertTrue(
            movementPhaseManager.isAwaitingTokenSelection,
            "MP Should be waiting for Token Selection after Clicking on a Token",
            true,
            movementPhaseManager.isAwaitingTokenSelection
        );
        AssertTrue(
            movementPhaseManager.isAwaitingHexDestination,
            "MP Should be waiting for Hex Destination after Clicking on a Ulisses",
            true,
            movementPhaseManager.isAwaitingHexDestination
        );
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(-3, -8), 0.5f));
        Log("Clicking (-3, -8), Move Ulisses, a commitment should be made");
        yield return new WaitForSeconds(3f); // for the token to move
        AvailabilityCheckResult mpcommitment = AssertCorrectAvailabilityAfterMovementCommitment();
        AssertTrue(
            mpcommitment.passed,
            "MovementPhase Commitment Check Status Availability",
            true,
            mpcommitment.ToString()
        );
        AssertTrue(
            movementPhaseManager.attackersMoved == 1,
            "MP - 1 attcker moved",
            1,
            movementPhaseManager.attackersMoved
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Count == 1,
            "MovementPhase Should have 1 after Ulisses's movement",
            1,
            movementPhaseManager.movedTokens.Count
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Contains(PlayerToken.GetPlayerTokenByName("Ulisses")),
            "MovementPhase Should have 1 after Ulisses's movement",
            true,
            movementPhaseManager.movedTokens.Contains(PlayerToken.GetPlayerTokenByName("Ulisses"))
        );
        AssertTrue(
            !movementPhaseManager.isAwaitingHexDestination,
            "MP Should NOT be waiting for Hex Destination after Moving Ulisses",
            false,
            movementPhaseManager.isAwaitingHexDestination
        );
        // savedSnapshot = SaveGameStatusSnapshot();
        // Log("Saving Game Status Snapshot");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(-4, -4), 0.5f));
        Log("Clicking (-4, -4), 9.Pavlovic");
        AssertTrue(
            movementPhaseManager.isAwaitingHexDestination,
            "MP Should be waiting for Hex Destination after Clicking on Pavlovic",
            true,
            movementPhaseManager.isAwaitingHexDestination
        );
        AssertTrue(
            movementPhaseManager.attackersMoved == 1,
            "MP - 1 attcker moved",
            1,
            movementPhaseManager.attackersMoved
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Count == 1,
            "MovementPhase Should have 1 after Ulisses's movement",
            1,
            movementPhaseManager.movedTokens.Count
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Contains(PlayerToken.GetPlayerTokenByName("Ulisses")),
            "MovementPhase Should have 1 after Ulisses's movement",
            true,
            movementPhaseManager.movedTokens.Contains(PlayerToken.GetPlayerTokenByName("Ulisses"))
        );
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(0, -3), 0.5f));
        Log("Clicking (0, -3), Move 9.Pavlovic");
        yield return new WaitForSeconds(3f); // for the token to move
        AssertTrue(
            movementPhaseManager.attackersMoved == 2,
            "MP - 2 attcker moved",
            2,
            movementPhaseManager.attackersMoved
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Count == 2,
            "MovementPhase Should have 1 after Pavlovic's movement",
            2,
            movementPhaseManager.movedTokens.Count
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Contains(PlayerToken.GetPlayerTokenByName("Ulisses")),
            "MovementPhase Should have 1 after Ulisses's movement",
            true,
            movementPhaseManager.movedTokens.Contains(PlayerToken.GetPlayerTokenByName("Ulisses"))
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Contains(PlayerToken.GetPlayerTokenByName("Pavlovic")),
            "MovementPhase Should have 1 after Pavlovic's movement",
            true,
            movementPhaseManager.movedTokens.Contains(PlayerToken.GetPlayerTokenByName("Pavlovic"))
        );
        AssertTrue(
            !movementPhaseManager.isAwaitingHexDestination,
            "MP Should NOT be waiting for Hex Destination after Moving Pavlovic",
            false,
            movementPhaseManager.isAwaitingHexDestination
        );
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(0, 0), 0.5f));
        Log("Clicking (0, 0), 2.Cafferata");
        AssertTrue(
            movementPhaseManager.isAwaitingHexDestination,
            "MP Should be waiting for Hex Destination after Clicking on Cafferata",
            true,
            movementPhaseManager.isAwaitingHexDestination
        );
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(5, 0), 0.5f));
        Log("Clicking (5, 0), Move 2.Cafferata");
        yield return new WaitForSeconds(3f); // for the token to move
        AssertTrue(
            movementPhaseManager.attackersMoved == 3,
            "MP - 3 attcker moved",
            3,
            movementPhaseManager.attackersMoved
        );
        AssertTrue(
            !movementPhaseManager.isAwaitingHexDestination,
            "MP Should NOT be waiting for Hex Destination after Moving Cafferata",
            false,
            movementPhaseManager.isAwaitingHexDestination
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Contains(PlayerToken.GetPlayerTokenByName("Ulisses")),
            "MovementPhase Should have 1 after Ulisses's movement",
            true,
            movementPhaseManager.movedTokens.Contains(PlayerToken.GetPlayerTokenByName("Ulisses"))
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Contains(PlayerToken.GetPlayerTokenByName("Pavlovic")),
            "MovementPhase Should have 1 after Pavlovic's movement",
            true,
            movementPhaseManager.movedTokens.Contains(PlayerToken.GetPlayerTokenByName("Pavlovic"))
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Contains(PlayerToken.GetPlayerTokenByName("Cafferata")),
            "MovementPhase Should have 1 after Cafferata's movement",
            true,
            movementPhaseManager.movedTokens.Contains(PlayerToken.GetPlayerTokenByName("Cafferata"))
        );
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(4, 4), 0.5f));
        Log("Clicking (4, 4), 6.Nazef");
        AssertTrue(
            movementPhaseManager.isAwaitingHexDestination,
            "MP Should be waiting for Hex Destination after Clicking on Nazef",
            true,
            movementPhaseManager.isAwaitingHexDestination
        );
        AssertTrue(
            movementPhaseManager.isMovementPhaseAttack,
            "MP - Attacking Movement Phase",
            true,
            movementPhaseManager.isMovementPhaseAttack
        );
        AssertTrue(
            !movementPhaseManager.isMovementPhaseDef,
            "MP - Attacking Movement Phase",
            false,
            movementPhaseManager.isMovementPhaseDef
        );
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(0, 6), 0.5f));
        Log("Clicking (0, 6), Move 6.Nazef");
        yield return new WaitForSeconds(3f); // for the token to move
        AssertTrue(
            movementPhaseManager.attackersMoved == 4,
            "MP - 4 attcker moved",
            4,
            movementPhaseManager.attackersMoved
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Contains(PlayerToken.GetPlayerTokenByName("Ulisses")),
            "MovementPhase Should have 1 after Ulisses's movement",
            true,
            movementPhaseManager.movedTokens.Contains(PlayerToken.GetPlayerTokenByName("Ulisses"))
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Contains(PlayerToken.GetPlayerTokenByName("Pavlovic")),
            "MovementPhase Should have 1 after Pavlovic's movement",
            true,
            movementPhaseManager.movedTokens.Contains(PlayerToken.GetPlayerTokenByName("Pavlovic"))
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Contains(PlayerToken.GetPlayerTokenByName("Cafferata")),
            "MovementPhase Should have 1 after Cafferata's movement",
            true,
            movementPhaseManager.movedTokens.Contains(PlayerToken.GetPlayerTokenByName("Cafferata"))
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Contains(PlayerToken.GetPlayerTokenByName("Nazef")),
            "MovementPhase Should have 1 after Nazef's movement",
            true,
            movementPhaseManager.movedTokens.Contains(PlayerToken.GetPlayerTokenByName("Nazef"))
        );
        AssertTrue(
            movementPhaseManager.defendersMoved == 0,
            "MP - 0 defenders moved",
            0,
            movementPhaseManager.defendersMoved
        );
        AssertTrue(
            movementPhaseManager.isMovementPhaseDef,
            "MP - Defensive Movement Phase after 4 moves",
            true,
            movementPhaseManager.isMovementPhaseDef
        );
        AssertTrue(
            !movementPhaseManager.isMovementPhaseAttack,
            "MP - Not Attacking Movement Phase after 4 moves",
            false,
            movementPhaseManager.isMovementPhaseAttack
        );
        AssertTrue(
            movementPhaseManager.isAwaitingTokenSelection,
            "MP Should be waiting for Token Selection after Attacking Movement Phase",
            true,
            movementPhaseManager.isAwaitingTokenSelection
        );
        AssertTrue(
            !movementPhaseManager.isAwaitingHexDestination,
            "MP Should NOT be waiting for Hex Destination after Attacking Movement Phase",
            false,
            movementPhaseManager.isAwaitingHexDestination
        );

        // Def1
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(1, 2), 0.5f));
        Log("Clicking (1, 2), 5.Vladoiu");
        AssertTrue(
            movementPhaseManager.isAwaitingTokenSelection,
            "MP SHould be waiting for Token Selection after Attacking Movement Phase",
            true,
            movementPhaseManager.isAwaitingTokenSelection
        );
        AssertTrue(
            movementPhaseManager.isAwaitingHexDestination,
            "MP Should be waiting for Hex Destination after Clicking on Vladoiu",
            true,
            movementPhaseManager.isAwaitingHexDestination
        );
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(-4, 5), 0.5f));
        Log("Clicking (-4, 5), Move 5.Vladoiu");
        yield return new WaitForSeconds(3f); // for the token to move
        AssertTrue(
            movementPhaseManager.isAwaitingTokenSelection,
            "MP SHould be waiting for Token Selection after Attacking Movement Phase",
            true,
            movementPhaseManager.isAwaitingTokenSelection
        );
        AssertTrue(
            !movementPhaseManager.isAwaitingHexDestination,
            "MP Should NOT be waiting for Hex Destination after moving Vladoiu",
            false,
            movementPhaseManager.isAwaitingHexDestination
        );
        AssertTrue(
            movementPhaseManager.defendersMoved == 1,
            "MP - 1 defenders moved",
            1,
            movementPhaseManager.defendersMoved
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Contains(PlayerToken.GetPlayerTokenByName("Ulisses")),
            "MovementPhase Should have 1 after Ulisses's movement",
            true,
            movementPhaseManager.movedTokens.Contains(PlayerToken.GetPlayerTokenByName("Ulisses"))
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Contains(PlayerToken.GetPlayerTokenByName("Pavlovic")),
            "MovementPhase Should have 1 after Pavlovic's movement",
            true,
            movementPhaseManager.movedTokens.Contains(PlayerToken.GetPlayerTokenByName("Pavlovic"))
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Contains(PlayerToken.GetPlayerTokenByName("Cafferata")),
            "MovementPhase Should have 1 after Cafferata's movement",
            true,
            movementPhaseManager.movedTokens.Contains(PlayerToken.GetPlayerTokenByName("Cafferata"))
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Contains(PlayerToken.GetPlayerTokenByName("Nazef")),
            "MovementPhase Should have 1 after Nazef's movement",
            true,
            movementPhaseManager.movedTokens.Contains(PlayerToken.GetPlayerTokenByName("Nazef"))
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Contains(PlayerToken.GetPlayerTokenByName("Vladoiu")),
            "MovementPhase Should have 1 after Vladoiu's movement",
            true,
            movementPhaseManager.movedTokens.Contains(PlayerToken.GetPlayerTokenByName("Vladoiu"))
        );
        // Def2
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(1, 10), 0.5f));
        Log("Clicking (1, 10), 4.Marell");
        AssertTrue(
            movementPhaseManager.isAwaitingHexDestination,
            "MP Should be waiting for Hex Destination after Clicking on Marell",
            true,
            movementPhaseManager.isAwaitingHexDestination
        );
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(5, 10), 0.5f));
        Log("Clicking (5, 10), Move 4.Marell");
        yield return new WaitForSeconds(3f); // for the token to move
        AssertTrue(
            movementPhaseManager.defendersMoved == 2,
            "MP - 2 defenders moved",
            2,
            movementPhaseManager.defendersMoved
        );
        AssertTrue(
            !movementPhaseManager.isAwaitingHexDestination,
            "MP Should NOT be waiting for Hex Destination after Moving Marell",
            false,
            movementPhaseManager.isAwaitingHexDestination
        );
        // Def3
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(14, 0), 0.5f));
        Log("Clicking (14, 0), 10. Soares");
        AssertTrue(
            movementPhaseManager.isAwaitingHexDestination,
            "MP Should be waiting for Hex Destination after Clicking on Soares",
            true,
            movementPhaseManager.isAwaitingHexDestination
        );
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(14, 5), 0.5f));
        Log("Clicking (14, 5), Move 10. Soares");
        yield return new WaitForSeconds(3f); // for the token to move
        AssertTrue(
            movementPhaseManager.defendersMoved == 3,
            "MP - 3 defenders moved",
            3,
            movementPhaseManager.defendersMoved
        );
        AssertTrue(
            !movementPhaseManager.isAwaitingHexDestination,
            "MP Should NOT be waiting for Hex Destination after Moving Soares",
            false,
            movementPhaseManager.isAwaitingHexDestination
        );
        // Def4
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(18, 0), 0.5f));
        Log("Clicking (18, 0), 11.Poulsen");
        AssertTrue(
            movementPhaseManager.isAwaitingHexDestination,
            "MP Should be waiting for Hex Destination after Clicking on Poulsen",
            true,
            movementPhaseManager.isAwaitingHexDestination
        );
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(14, 0), 0.5f));
        Log("Clicking (14, 0), Move 11.Poulsen");
        yield return new WaitForSeconds(3f); // for the token to move
        AssertTrue(
            movementPhaseManager.defendersMoved == 4,
            "MP - 4 defenders moved",
            4,
            movementPhaseManager.defendersMoved
        );
        AssertTrue(
            !movementPhaseManager.isAwaitingHexDestination,
            "MP Should NOT be waiting for Hex Destination after Moving Poulsen",
            false,
            movementPhaseManager.isAwaitingHexDestination
        );
        // Def5
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(1, -10), 0.5f));
        Log("Clicking (1, -10), 3.Delgado");
        AssertTrue(
            movementPhaseManager.isAwaitingHexDestination,
            "MP Should be waiting for Hex Destination after Clicking on Delgado",
            true,
            movementPhaseManager.isAwaitingHexDestination
        );
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(6, -9), 0.5f));
        Log("Clicking (6, -9), Move 3.Delgado");
        yield return new WaitForSeconds(3f); // for the token to move
        AssertTrue(
            movementPhaseManager.defendersMoved == 5,
            "MP - 4 defenders moved",
            5,
            movementPhaseManager.defendersMoved
        );
        AssertTrue(
            !movementPhaseManager.isAwaitingHexDestination,
            "MP Should NOT be waiting for Hex Destination after Moving Delgado",
            false,
            movementPhaseManager.isAwaitingHexDestination
        );

        // 2f1
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(-6, -6), 0.5f));
        Log("Clicking (-6, -6), 10.Noruega");
        AssertTrue(
            movementPhaseManager.isAwaitingHexDestination,
            "MP Should be waiting for Hex Destination after Clicking on Noruega",
            true,
            movementPhaseManager.isAwaitingHexDestination
        );
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(-8, -6), 0.5f));
        Log("Clicking (-8, -6), Move 10.Noruega");
        yield return new WaitForSeconds(3f); // for the token to move
        AssertTrue(
            movementPhaseManager.attackersMovedIn2f2 == 1,
            "MP - 1 2f2 moved",
            1,
            movementPhaseManager.attackersMovedIn2f2
        );
        AssertTrue(
            !movementPhaseManager.isAwaitingHexDestination,
            "MP Should NOT be waiting for Hex Destination after Moving Noruega",
            false,
            movementPhaseManager.isAwaitingHexDestination
        );
        // 2f2
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(12, 12), 0.5f));
        Log("Clicking (12, 12), 5.Murphy");
        AssertTrue(
            movementPhaseManager.isAwaitingHexDestination,
            "MP Should be waiting for Hex Destination after Clicking on Muprhy",
            true,
            movementPhaseManager.isAwaitingHexDestination
        );
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 12), 0.5f));
        Log("Clicking (10, 12), Move 5.Murphy");
        yield return new WaitForSeconds(3f); // for the token to move
        AssertTrue(
            finalThirdManager.isActivated,
            "Final Thirds should be Active now after MP ending in F3",
            true,
            finalThirdManager.isActivated
        );

        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        Log("Pressing X - Forfeit Attack FinalThird");
        AssertTrue(
            finalThirdManager.isActivated,
            "Final Thirds should be Active now after MP ending in F3",
            true,
            finalThirdManager.isActivated
        );
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        Log("Pressing X - Forfeit Defense FinalThird");
        yield return null ; // for the token to move
        AssertTrue(
            !finalThirdManager.isActivated,
            "Final Thirds should be inactive now after MP ending in F3",
            false,
            finalThirdManager.isActivated
        );
        
        AvailabilityCheckResult mpcomplete = AssertCorrectAvailabilityAfterMovementComplete();
        AssertTrue(
            mpcomplete.passed,
            "MovementPhase Complete Check Status Availability",
            true,
            mpcomplete.ToString()
        );
        
        // GameStatusSnapshot currentSnapshot = GetCurrentSnapshot();
        // bool isSame = savedSnapshot.IsEqualTo(currentSnapshot, out string mismatchReason, new HashSet<string> {});
        // AssertTrue(
        //     isSame,
        //     "Snapshot should match the expected game state",
        //     "Snapshots match",
        //     isSame ? "Snapshots match" : mismatchReason
        // );

        LogFooterofTest("MovementPhase No Interceptions, No Tackles");
    }
    
    private IEnumerator Scenario_010_Movement_Phase_failed_interceptions_No_tackles()
    {
        yield return new WaitForSeconds(1.5f); // Allow scene to stabilize
        Log("▶️ Starting test scenario: MovementPhase With failed Interceptions, No Tackles");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Alpha2, 0.1f));
        Log("Pressing 2");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Space, 0.1f));
        Log("Pressing Space");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.P, 0.1f));
        Log("Pressing P - Game is in Movement Phase");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        Log("Clicking (10, 0)");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        Log("Clicking (10, 0) again");
        yield return new WaitForSeconds(3f); // for the ball to move
        Log("Wait for the ball to move");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        Log("Pressing X - Forfeit Attack FinalThird");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        Log("Pressing X - Forfeit Defense FinalThird");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.M, 0.1f));
        Log("Pressing M - Game is in Movement Phase");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        Log("Clicking (10, 0) Select Yaneva");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(11, 0), 0.5f));
        Log("Clicking (11, 0) Move Yaneva 1 pace");
        yield return new WaitForSeconds(1.2f); // for the token to move
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(12, 1), 0.5f));
        Log("Clicking (12, 1) Move Yaneva 2nd pace");
        yield return new WaitForSeconds(1.2f); // for the token to move
        Log("Clicking (16, -1) Move GK for Box entrance");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(16, -1), 0.5f));
        yield return new WaitForSeconds(1.2f); // for the token to move
        Log("Clicking (13, 0) Move Yaneva 3rd pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(13, 0), 0.5f));
        yield return new WaitForSeconds(1.9f); // for the token to move
        AssertTrue(
            movementPhaseManager.isWaitingForNutmegDecision,
            "MP Should be waiting for nutmeg decision when Yaneva moves next to Soares",
            true,
            movementPhaseManager.isWaitingForNutmegDecision
        );
        AssertTrue(
            movementPhaseManager.nutmeggableDefenders.Count == 1,
            "MP Nutmeggable defenders should contain 1",
            1,
            movementPhaseManager.nutmeggableDefenders.Count
        );
        var defender = PlayerToken.GetPlayerTokenByName("Soares");
        AssertTrue(
            movementPhaseManager.nutmeggableDefenders.Contains(defender),
            "MP Nutmeggable defenders should contain Soares",
            PlayerToken.GetPlayerTokenByName("Soares"),
            defender
        );
        Log("Pressing X to not Nutmeg Soares");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.9f));
        yield return new WaitForSeconds(0.1f); // for the token to move
        AssertTrue(
            movementPhaseManager.isWaitingForInterceptionDiceRoll,
            "MP Should be waiting for Interception Roll after Rejected nutmeg",
            true,
            movementPhaseManager.isWaitingForInterceptionDiceRoll
        );
        AssertTrue(
            movementPhaseManager.eligibleDefenders.Count == 1,
            "MP eligibleDefenders should contain 1",
            1,
            movementPhaseManager.eligibleDefenders.Count
        );
        var interceptor = PlayerToken.GetPlayerTokenByName("Soares");
        AssertTrue(
            movementPhaseManager.eligibleDefenders.Contains(interceptor),
            "MP eligibleDefenders should contain Soares",
            PlayerToken.GetPlayerTokenByName("Soares"),
            interceptor
        );
        Log("Pressing R to roll and Soares fails to steal the ball");
        StartCoroutine(movementPhaseManager.PerformBallInterceptionDiceRoll(2));
        yield return new WaitForSeconds(0.5f);
        AssertTrue(
            movementPhaseManager.defendersTriedToIntercept.Contains(interceptor),
            "MP defendersTriedToIntercept should contain Soares",
            PlayerToken.GetPlayerTokenByName("Soares"),
            interceptor
        );
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(14, 1), 0.5f));
        Log("Clicking (14, 1) Move Yaneva 4th pace");
        yield return new WaitForSeconds(1.2f); // for the token to move
        Log("Clicking (14, 2) Move Yaneva 5th pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(14, 2), 0.5f));
        yield return new WaitForSeconds(1.2f); // for the token to move
        Log("Clicking (14, 3) Move Yaneva 6th pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(14, 3), 0.5f));
        yield return new WaitForSeconds(1.2f); // for the token to move
        Log("Pressing X to NOT SNAPSHOT after exhausting pace");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        yield return new WaitForSeconds(0.1f); // for the token to move
        AssertTrue(
            movementPhaseManager.attackersMoved == 1,
            "MP - 1 attacker moved",
            1,
            movementPhaseManager.attackersMoved
        );
        AssertTrue(
            movementPhaseManager.isMovementPhaseAttack,
            "MP - In Def Att phase",
            true,
            movementPhaseManager.isMovementPhaseAttack
        );
        Log("Pressing X to Forfeit Att Movement Phase");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        AssertTrue(
            movementPhaseManager.attackersMoved == 4,
            "MP - 4 attacker moved due to forfeit",
            4,
            movementPhaseManager.attackersMoved
        );
        AssertTrue(
            movementPhaseManager.isMovementPhaseDef,
            "MP - In Def MP phase",
            true,
            movementPhaseManager.isMovementPhaseDef
        );

        LogFooterofTest("MovementPhase With failed Interceptions, No Tackles");
    }

    private IEnumerator Scenario_011_Movement_Phase_Successful_Interception()
    {
        yield return new WaitForSeconds(1.5f); // Allow scene to stabilize
        Log("▶️ Starting test scenario: MovementPhase With Successful Interception");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Alpha2, 0.1f));
        Log("Pressing 2");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Space, 0.1f));
        Log("Pressing Space");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.P, 0.1f));
        Log("Pressing P - Game is in Movement Phase");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        Log("Clicking (10, 0)");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        Log("Clicking (10, 0) again");
        yield return new WaitForSeconds(3f); // for the ball to move
        Log("Wait for the ball to move");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        Log("Pressing X - Forfeit Attack FinalThird");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        Log("Pressing X - Forfeit Defense FinalThird");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.M, 0.1f));
        Log("Pressing M - Game is in Movement Phase");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        Log("Clicking (10, 0) Select Yaneva");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(11, 0), 0.5f));
        Log("Clicking (11, 0) Move Yaneva 1 pace");
        yield return new WaitForSeconds(1.2f); // for the token to move
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(12, 1), 0.5f));
        Log("Clicking (12, 1) Move Yaneva 2nd pace");
        yield return new WaitForSeconds(1.2f); // for the token to move
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(16, -1), 0.5f));
        yield return new WaitForSeconds(1.2f); // for the token to move
        Log("Clicking (16, -1) Move GK for Box entrance");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(13, 0), 0.5f));
        Log("Clicking (13, 0) Move Yaneva 3rd pace");
        yield return new WaitForSeconds(1.2f); // for the token to move
        AssertTrue(
            movementPhaseManager.isWaitingForNutmegDecision,
            "MP Should be waiting for nutmeg decision when Yaneva moves next to Soares",
            true,
            movementPhaseManager.isWaitingForNutmegDecision
        );
        AssertTrue(
            movementPhaseManager.nutmeggableDefenders.Count == 1,
            "MP Nutmeggable defenders should contain 1",
            1,
            movementPhaseManager.nutmeggableDefenders.Count
        );
        var defender = PlayerToken.GetPlayerTokenByName("Soares");
        AssertTrue(
            movementPhaseManager.nutmeggableDefenders.Contains(defender),
            "MP Nutmeggable defenders should contain Soares",
            PlayerToken.GetPlayerTokenByName("Soares"),
            defender
        );
        Log("Pressing X to not Nutmeg Soares");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        yield return new WaitForSeconds(0.1f); // for the token to move
        AssertTrue(
            movementPhaseManager.isWaitingForInterceptionDiceRoll,
            "MP Should be waiting for Interception Roll after Rejected nutmeg",
            true,
            movementPhaseManager.isWaitingForInterceptionDiceRoll
        );
        AssertTrue(
            movementPhaseManager.eligibleDefenders.Count == 1,
            "MP eligibleDefenders should contain 1",
            1,
            movementPhaseManager.eligibleDefenders.Count
        );
        var interceptor = PlayerToken.GetPlayerTokenByName("Soares");
        AssertTrue(
            movementPhaseManager.eligibleDefenders.Contains(interceptor),
            "MP eligibleDefenders should contain Soares",
            PlayerToken.GetPlayerTokenByName("Soares"),
            interceptor
        );
        Log("Pressing R to roll and Soares steals the ball");
        StartCoroutine(movementPhaseManager.PerformBallInterceptionDiceRoll(6));
        yield return new WaitForSeconds(0.5f);
        AvailabilityCheckResult availabilityCheck = AssertCorrectAvailabilityAnyOtherScenario();
        AssertTrue(
            availabilityCheck.passed,
            "Action Availability after Interception (Any Other Scenario)",
            true,
            availabilityCheck.ToString()
        );
        float soaresExpectedXRecovery = CalculateExpectedRecoveryFromTackling(interceptor.tackling);
        AssertTrue(
            MatchManager.Instance.gameData.stats.GetPlayerStats("Soares").interceptionsAttempted == 1,
            "Soares should have 1 interception attempted",
            1,
            MatchManager.Instance.gameData.stats.GetPlayerStats("Soares").interceptionsAttempted
        );
        AssertTrue(
            MatchManager.Instance.gameData.stats.GetPlayerStats("Soares").interceptionsMade == 1,
            "Soares should have 1 interception made",
            1,
            MatchManager.Instance.gameData.stats.GetPlayerStats("Soares").interceptionsMade
        );
        AssertApproximately(
            MatchManager.Instance.gameData.stats.GetPlayerStats("Soares").xRecoveries,
            soaresExpectedXRecovery,
            0.0001f,
            "Soares should record xRecovery for the movement-phase steal attempt");
        AssertApproximately(
            MatchManager.Instance.gameData.stats.awayTeamStats.totalXRecoveries,
            soaresExpectedXRecovery,
            0.0001f,
            "Away team xRecoveries should roll up Soares' movement steal expectation");

        LogFooterofTest("MovementPhase With Successful Interception");
    }

    private IEnumerator Scenario_012_Movement_Phase_interception_Foul_take_foul(bool alreadyBooked, bool alreadyInjured)
    {        
        yield return new WaitForSeconds(1.5f); // Allow scene to stabilize
        Log($"▶️ Starting test scenario: MovementPhase With Foul Taken on Interception [alreadyBooked={alreadyBooked}, alreadyInjured={alreadyInjured}]");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Alpha2, 0.1f));
        Log("Pressing 2");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Space, 0.1f));
        Log("Pressing Space");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.P, 0.1f));
        Log("Pressing P - Game is in Movement Phase");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        Log("Clicking (10, 0)");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        Log("Clicking (10, 0) again");
        yield return new WaitForSeconds(3f); // for the ball to move
        Log("Wait for the ball to move");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        Log("Pressing X - Forfeit Attack FinalThird");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        Log("Pressing X - Forfeit Defense FinalThird");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.M, 0.1f));
        Log("Pressing M - Game is in Movement Phase");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        Log("Clicking (10, 0) Select Yaneva");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(11, 0), 0.5f));
        Log("Clicking (11, 0) Move Yaneva 1 pace");
        yield return new WaitForSeconds(1.2f); // for the token to move
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(12, 1), 0.5f));
        Log("Clicking (12, 1) Move Yaneva 2nd pace");
        yield return new WaitForSeconds(1.2f); // for the token to move
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(16, -1), 0.5f));
        yield return new WaitForSeconds(1.2f); // for the token to move
        Log("Clicking (16, -1) Move GK for Box entrance");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(13, 0), 0.5f));
        Log("Clicking (13, 0) Move Yaneva 3rd pace");
        yield return new WaitForSeconds(1.2f); // for the token to move
        AssertTrue(
            movementPhaseManager.isWaitingForNutmegDecision,
            "MP Should be waiting for nutmeg decision when Yaneva moves next to Soares",
            true,
            movementPhaseManager.isWaitingForNutmegDecision
        );
        AssertTrue(
            movementPhaseManager.nutmeggableDefenders.Count == 1,
            "MP Nutmeggable defenders should contain 1",
            1,
            movementPhaseManager.nutmeggableDefenders.Count
        );
        var defender = PlayerToken.GetPlayerTokenByName("Soares");
        AssertTrue(
            movementPhaseManager.nutmeggableDefenders.Contains(defender),
            "MP Nutmeggable defenders should contain Soares",
            PlayerToken.GetPlayerTokenByName("Soares"),
            defender
        );
        Log("Pressing X to not Nutmeg Soares");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        yield return new WaitForSeconds(0.1f); // for the token to move
        AssertTrue(
            movementPhaseManager.isWaitingForInterceptionDiceRoll,
            "MP Should be waiting for Interception Roll after Rejected nutmeg",
            true,
            movementPhaseManager.isWaitingForInterceptionDiceRoll
        );
        AssertTrue(
            movementPhaseManager.eligibleDefenders.Count == 1,
            "MP eligibleDefenders should contain 1",
            1,
            movementPhaseManager.eligibleDefenders.Count
        );
        var interceptor = PlayerToken.GetPlayerTokenByName("Soares");
        AssertTrue(
            movementPhaseManager.eligibleDefenders.Contains(interceptor),
            "MP eligibleDefenders should contain Soares",
            PlayerToken.GetPlayerTokenByName("Soares"),
            interceptor
        );
        Log("Pressing R to roll and Soares and he fouls!");
        StartCoroutine(movementPhaseManager.PerformBallInterceptionDiceRoll(1));
        yield return new WaitForSeconds(0.5f);
        if (alreadyBooked)
        {
            Log("Pre-booking Soares before the leniency roll");
            interceptor.ReceiveYellowCard();
        }
        if (alreadyInjured)
        {
            Log("Pre-injuring Yaneva before the injury roll");
            PlayerToken.GetPlayerTokenByName("Yaneva").ReceiveInjury();
        }
        Log("Calling PerformLeniencyTest(6)");
        movementPhaseManager.PerformLeniencyTest(6);
        yield return new WaitForSeconds(0.2f);
        if (alreadyBooked)
        {
            AssertTrue(
                MatchManager.Instance.gameData.stats.GetPlayerStats("Soares").redCards == 1,
                "Soares should log a red card when a steal foul produces a second yellow",
                1,
                MatchManager.Instance.gameData.stats.GetPlayerStats("Soares").redCards
            );
        }
        else
        {
            AssertTrue(
                MatchManager.Instance.gameData.stats.GetPlayerStats("Soares").yellowCards == 1,
                "Soares should log a yellow card when a steal foul booking is shown",
                1,
                MatchManager.Instance.gameData.stats.GetPlayerStats("Soares").yellowCards
            );
        }
        AssertTrue(
            !movementPhaseManager.isWaitingForFoulDecision,
            "MP Should NOT be waiting for a foul decision after Leniency Roll",
            true,
            movementPhaseManager.isWaitingForFoulDecision
        );
        Log("Calling PerformInjuryTest(6)");
        movementPhaseManager.PerformInjuryTest(6);
        yield return new WaitForSeconds(0.8f);
        if (!alreadyInjured)
        {
            AssertTrue(
                MatchManager.Instance.gameData.stats.GetPlayerStats("Yaneva").injuries == 1,
                "Yaneva should log an injury when a steal foul injures her",
                1,
                MatchManager.Instance.gameData.stats.GetPlayerStats("Yaneva").injuries
            );
        }

        bool playOnShouldBeBlocked = alreadyBooked || alreadyInjured;
        if (playOnShouldBeBlocked)
        {
            AssertTrue(
                !movementPhaseManager.isWaitingForFoulDecision,
                "MP Should NOT offer a foul decision when Play On is unavailable",
                false,
                movementPhaseManager.isWaitingForFoulDecision
            );
            AssertTrue(
                freeKickManager.isWaitingForKickerSelection,
                "FreeKickManager should start immediately when Play On is unavailable",
                true,
                freeKickManager.isWaitingForKickerSelection
            );
            AssertTrue(
                !movementPhaseManager.isActivated,
                "MP should have died after the forced free kick",
                false,
                movementPhaseManager.isActivated
            );
        }
        else
        {
            AssertTrue(
                movementPhaseManager.isWaitingForFoulDecision,
                "MP Should be waiting for a foul decision after Foul Rolls",
                true,
                movementPhaseManager.isWaitingForFoulDecision
            );
            Log("Pressing F - to take foul");
            yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.F, 0.1f));
            yield return new WaitForSeconds(0.5f);
            AssertTrue(
                !movementPhaseManager.isWaitingForFoulDecision,
                "MP Should NOT wait for a foul decision after Foul Decision",
                false,
                movementPhaseManager.isWaitingForFoulDecision
            );
            AssertTrue(
                freeKickManager.isWaitingForKickerSelection,
                "FreeKickManager should be waiting for Kicker Selection",
                true,
                freeKickManager.isWaitingForKickerSelection
            );
            AssertTrue(
                freeKickManager.remainingDefenderMoves == 6,
                "FreeKickManager should be waiting for Kicker Selection",
                6,
                freeKickManager.remainingDefenderMoves
            );
            AssertTrue(
                !movementPhaseManager.isActivated,
                "MP Should have died after FK taken",
                false,
                movementPhaseManager.isActivated
            );
        }

        LogFooterofTest($"MovementPhase With Foul Taken on Interception [alreadyBooked={alreadyBooked}, alreadyInjured={alreadyInjured}]");
    }

    private IEnumerator Scenario_013_Movement_Phase_interception_Foul_Play_on()
    {        
        yield return new WaitForSeconds(1.5f); // Allow scene to stabilize
        Log("▶️ Starting test scenario: MovementPhase With Fouled Interception and Play On");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Alpha2, 0.1f));
        Log("Pressing 2");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Space, 0.1f));
        Log("Pressing Space");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.P, 0.1f));
        Log("Pressing P - Game is in Movement Phase");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        Log("Clicking (10, 0)");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        Log("Clicking (10, 0) again");
        yield return new WaitForSeconds(3f); // for the ball to move
        Log("Wait for the ball to move");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        Log("Pressing X - Forfeit Attack FinalThird");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        Log("Pressing X - Forfeit Defense FinalThird");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.M, 0.1f));
        Log("Pressing M - Game is in Movement Phase");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        Log("Clicking (10, 0) Select Yaneva");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(11, 0), 0.5f));
        Log("Clicking (11, 0) Move Yaneva 1 pace");
        yield return new WaitForSeconds(1.2f); // for the token to move
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(12, 1), 0.5f));
        Log("Clicking (12, 1) Move Yaneva 2nd pace");
        yield return new WaitForSeconds(1.2f); // for the token to move
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(16, -1), 0.5f));
        yield return new WaitForSeconds(1.2f); // for the token to move
        Log("Clicking (16, -1) Move GK for Box entrance");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(13, 0), 0.5f));
        Log("Clicking (13, 0) Move Yaneva 3rd pace");
        yield return new WaitForSeconds(1.2f); // for the token to move
        AssertTrue(
            movementPhaseManager.isWaitingForNutmegDecision,
            "MP Should be waiting for nutmeg decision when Yaneva moves next to Soares",
            true,
            movementPhaseManager.isWaitingForNutmegDecision
        );
        AssertTrue(
            movementPhaseManager.nutmeggableDefenders.Count == 1,
            "MP Nutmeggable defenders should contain 1",
            1,
            movementPhaseManager.nutmeggableDefenders.Count
        );
        var defender = PlayerToken.GetPlayerTokenByName("Soares");
        AssertTrue(
            movementPhaseManager.nutmeggableDefenders.Contains(defender),
            "MP Nutmeggable defenders should contain Soares",
            PlayerToken.GetPlayerTokenByName("Soares"),
            defender
        );
        Log("Pressing X to not Nutmeg Soares");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        yield return new WaitForSeconds(0.1f); // for the token to move
        AssertTrue(
            movementPhaseManager.isWaitingForInterceptionDiceRoll,
            "MP Should be waiting for Interception Roll after Rejected nutmeg",
            true,
            movementPhaseManager.isWaitingForInterceptionDiceRoll
        );
        AssertTrue(
            movementPhaseManager.eligibleDefenders.Count == 1,
            "MP eligibleDefenders should contain 1",
            1,
            movementPhaseManager.eligibleDefenders.Count
        );
        var interceptor = PlayerToken.GetPlayerTokenByName("Soares");
        AssertTrue(
            movementPhaseManager.eligibleDefenders.Contains(interceptor),
            "MP eligibleDefenders should contain Soares",
            PlayerToken.GetPlayerTokenByName("Soares"),
            interceptor
        );
        Log("Pressing R to roll and Soares and he fouls!");
        StartCoroutine(movementPhaseManager.PerformBallInterceptionDiceRoll(1));
        Log("Pressing R for Leniency Test");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.R, 0.1f));
        AssertTrue(
            !movementPhaseManager.isWaitingForFoulDecision,
            "MP Should NOT be waiting for a foul decision after Leniency Roll",
            true,
            movementPhaseManager.isWaitingForFoulDecision
        );
        Log("Pressing R for Resilience Test");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.R, 0.1f));
        yield return new WaitForSeconds(0.5f);
        AssertTrue(
            movementPhaseManager.isWaitingForFoulDecision,
            "MP Should be waiting for a foul decision after Foul Rolls",
            true,
            movementPhaseManager.isWaitingForFoulDecision
        );
        Log("Pressing A - to Play ON");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.A, 0.1f));
        yield return new WaitForSeconds(0.5f);
        AssertTrue(
            !movementPhaseManager.isWaitingForFoulDecision,
            "MP Should NOT wait for a foul decision after Play On",
            false,
            movementPhaseManager.isWaitingForFoulDecision
        );
        AssertTrue(
            !movementPhaseManager.isWaitingForReposition,
            "MP Should NOT Be Waiting for a reposition after Play On, it was just an interception",
            false,
            movementPhaseManager.isWaitingForReposition
        );
        Log("Clicking (15, 0) Reposition Yaneva");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(15, 0), 0.5f));
        yield return new WaitForSeconds(1.2f); // for the token to move
        AssertTrue(
            !movementPhaseManager.isWaitingForReposition,
            "MP Should NOT Be Waiting for a reposition after Reposition Yaneva",
            false,
            movementPhaseManager.isWaitingForReposition
        );
        AssertTrue(
            movementPhaseManager.isWaitingForSnapshotDecision,
            "MP Should Be Waiting for Snapshot after Reposition Yaneva",
            true,
            movementPhaseManager.isWaitingForSnapshotDecision
        );
        yield return new WaitForSeconds(1.2f); // for the token to move

        LogFooterofTest("MovementPhase With Fouled Interception and Play On");
    }

    private IEnumerator Scenario_014_Movement_Phase_Check_reposition_interceptions()
    {
        yield return new WaitForSeconds(1.5f); // Allow scene to stabilize
        Log("▶️ Starting test scenario: MovementPhase Check Reposition Interceptions");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Alpha2, 0.1f));
        Log("Pressing 2");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Space, 0.1f));
        Log("Pressing Space");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.P, 0.1f));
        Log("Pressing P - Game is in Movement Phase");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        Log("Clicking (10, 0)");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        Log("Clicking (10, 0) again");
        yield return new WaitForSeconds(3f); // for the ball to move
        Log("Wait for the ball to move");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        Log("Pressing X - Forfeit Attack FinalThird");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        Log("Pressing X - Forfeit Defense FinalThird");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.M, 0.1f));
        Log("Pressing M - Game is in Movement Phase");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        Log("Clicking (10, 0) Select Yaneva");
        Log("Clicking (9, 0) Move Yaneva 1 pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(9, 0), 0.5f));
        yield return new WaitForSeconds(1.2f); // for the token to move
        Log("Clicking (8, 1) Move Yaneva 2nd pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(8, 1), 0.5f));
        yield return new WaitForSeconds(1.2f); // for the token to move
        Log("Clicking (7, 1) Move Yaneva 3rd pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(7, 1), 0.5f));
        yield return new WaitForSeconds(1.2f); // for the token to move
        Log("Clicking (6, 2) Move Yaneva 4th pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(6, 2), 0.5f));
        yield return new WaitForSeconds(1.2f); // for the token to move
        Log("Clicking (5, 2) Move Yaneva 5th pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(5, 2), 0.5f));
        yield return new WaitForSeconds(1.2f); // for the token to move
        Log("Pressing R to roll and Soares and he fails!");
        StartCoroutine(movementPhaseManager.PerformBallInterceptionDiceRoll(2));
        Log("Pressing X to Forfeit Rest of Yaneva Pace");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        Log("Pressing X to Forfeit Rest of Attack Movement Phase");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        Log("Clicking (4, 3) Select Paterson");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(4, 3), 0.5f));
        AssertTrue(
            movementPhaseManager.isWaitingForTackleDecisionWithoutMoving,
            "MovementPhase Should be waiting for Tackle Decision without moving before moving Paterson",
            true,
            movementPhaseManager.isWaitingForTackleDecisionWithoutMoving
        );
        AssertTrue(
            !movementPhaseManager.isWaitingForTackleDecision,
            "MovementPhase Should NOT be waiting for Tackle Decision before moving Paterson",
            false,
            movementPhaseManager.isWaitingForTackleDecision
        );
        Log("Clicking (6, 4) Move Paterson");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(6, 4), 0.5f));
        yield return new WaitForSeconds(1.2f); // for the token to move
        AssertTrue(
            movementPhaseManager.defendersMoved == 1,
            "MovementPhase Defenders Moved should be 1 after moving Paterson",
            1,
            movementPhaseManager.defendersMoved
        );
        var paterson = PlayerToken.GetPlayerTokenByName("Paterson");
        AssertTrue(
            movementPhaseManager.movedTokens.Contains(paterson),
            "MovementPhase Defenders Moved should be 1 after moving Paterson",
            true,
            movementPhaseManager.movedTokens.Contains(paterson)
        );
        Log("Clicking (3, 3) Select Gilbert");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(3, 3), 0.5f));
        AssertTrue(
            !movementPhaseManager.isWaitingForTackleDecisionWithoutMoving,
            "MovementPhase Should NOT be waiting for Tackle Decision without moving after selecting Gilbert",
            false,
            movementPhaseManager.isWaitingForTackleDecisionWithoutMoving
        );
        AssertTrue(
            !movementPhaseManager.isWaitingForTackleDecision,
            "MovementPhase Should NOT be waiting for Tackle Decision after selecting Gilbert",
            false,
            movementPhaseManager.isWaitingForTackleDecision
        );
        Log("Clicking (5, 3) Move Gilbert");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(5, 3), 0.5f));
        yield return new WaitForSeconds(1.2f); // for the token to move
        var gilbert = PlayerToken.GetPlayerTokenByName("Gilbert");
        AssertTrue(
            movementPhaseManager.movedTokens.Contains(gilbert),
            "MovementPhase movedTokens should contain Gilbert",
            true,
            movementPhaseManager.movedTokens.Contains(gilbert)
        );
        AssertTrue(
            movementPhaseManager.isWaitingForTackleDecision,
            "MovementPhase Should be waiting for Tackle Decision after moving Gilbert",
            true,
            movementPhaseManager.isWaitingForTackleDecision
        );
        AssertTrue(
            !movementPhaseManager.isWaitingForTackleDecisionWithoutMoving,
            "MovementPhase Should NOT be waiting for Tackle Decision without moving",
            false,
            movementPhaseManager.isWaitingForTackleDecisionWithoutMoving
        );
        Log("Pressing T to Tackle Yaneva with Gilbert");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.T, 0.1f));
        AssertTrue(
            !movementPhaseManager.isWaitingForTackleDecision,
            "MovementPhase Should NOT be waiting for Tackle Decision after calling Tackle",
            false,
            movementPhaseManager.isWaitingForTackleDecision
        );
        AssertTrue(
            movementPhaseManager.isWaitingForTackleRoll,
            "MovementPhase Should be waiting for Tackle Rolls after calling Tackle",
            true,
            movementPhaseManager.isWaitingForTackleRoll
        );
        var tackler = PlayerToken.GetPlayerTokenByName("Gilbert");
        AssertTrue(
            movementPhaseManager.selectedDefender == tackler,
            "MovementPhase Should be waiting for Tackle Rolls",
            tackler.name,
            movementPhaseManager.selectedDefender.name
        );
        movementPhaseManager.PerformTackleDiceRoll(isDefender: true, 2);
        yield return new WaitForSeconds(0.2f);
        movementPhaseManager.PerformTackleDiceRoll(isDefender: false, 6);
        yield return new WaitForSeconds(0.2f);
        AssertTrue(
            movementPhaseManager.isWaitingForReposition,
            "MovementPhase Should be waiting for Reposition after Tackle Rolls",
            true,
            movementPhaseManager.isWaitingForReposition
        );
        Log("Clicking (5, 4) Reposition Yaneva");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(5, 4), 0.5f));
        yield return new WaitForSeconds(2.0f);
        AssertTrue(
            movementPhaseManager.isWaitingForInterceptionDiceRoll,
            "MovementPhase Should be waiting for Interception Rolls after Reposition Yaneva",
            true,
            movementPhaseManager.isWaitingForInterceptionDiceRoll
        );
        AssertTrue(
            movementPhaseManager.eligibleDefenders.Count == 2,
            "MP eligibleDefenders should contain 2",
            2,
            movementPhaseManager.eligibleDefenders.Count
        );
        yield return new WaitForSeconds(1.0f);
        Log("Pressing R to roll and Stewart and he fails!");
        StartCoroutine(movementPhaseManager.PerformBallInterceptionDiceRoll(2));
        yield return new WaitForSeconds(0.5f);
        Log("Pressing R to roll and McNulty and he fails!");
        StartCoroutine(movementPhaseManager.PerformBallInterceptionDiceRoll(2));
        yield return new WaitForSeconds(0.5f);
        AssertTrue(
            movementPhaseManager.defendersMoved == 2,
            "MovementPhase Defenders Moved should be 2 after moving Gilbert and resolving his tackling",
            2,
            movementPhaseManager.defendersMoved
        );
        Log("Clicking (5, 5) Select McNulty");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(5, 5), 0.5f));
        yield return new WaitForSeconds(0.5f);
        AssertTrue(
            movementPhaseManager.isWaitingForTackleDecisionWithoutMoving,
            "MovementPhase Should be waiting for Tackle Decision without moving when we select McNulty",
            true,
            movementPhaseManager.isWaitingForTackleDecisionWithoutMoving
        );
        AssertTrue(
            !movementPhaseManager.isWaitingForTackleDecision,
            "MovementPhase Should NOT be waiting for Tackle Decision without moving when we select McNulty",
            false,
            movementPhaseManager.isWaitingForTackleDecision
        );
        Log("Pressing T to Tackle Yaneva with McNulty");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.T, 0.1f));
        yield return new WaitForSeconds(0.5f);
        var mcnulty = PlayerToken.GetPlayerTokenByName("McNulty");
        AssertTrue(
            movementPhaseManager.movedTokens.Contains(mcnulty),
            "MovementPhase movedTokens should contain McNulty",
            true,
            movementPhaseManager.movedTokens.Contains(mcnulty)
        );
        AssertTrue(
            !movementPhaseManager.isWaitingForTackleDecisionWithoutMoving,
            "MovementPhase Should be waiting for Tackle Decision without moving after calling Tackle",
            false,
            movementPhaseManager.isWaitingForTackleDecisionWithoutMoving
        );
        AssertTrue(
            movementPhaseManager.isWaitingForTackleRoll,
            "MovementPhase Should be waiting for Tackle Rolls after calling Tackle with McNulty",
            true,
            movementPhaseManager.isWaitingForTackleRoll
        );
        movementPhaseManager.PerformTackleDiceRoll(isDefender: true, 2);
        yield return new WaitForSeconds(0.2f);
        movementPhaseManager.PerformTackleDiceRoll(isDefender: false, 6);
        yield return new WaitForSeconds(0.2f);
        AssertTrue(
            movementPhaseManager.isWaitingForReposition,
            "MovementPhase Should be waiting for Reposition after Tackle Rolls",
            true,
            movementPhaseManager.isWaitingForReposition
        );
        Log("Clicking (4, 6) Reposition Yaneva");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(4, 6), 0.5f));
        yield return new WaitForSeconds(2.0f);
        Log("Pressing R to roll and Stewart and he fails!");
        StartCoroutine(movementPhaseManager.PerformBallInterceptionDiceRoll(2));
        yield return new WaitForSeconds(1.0f);
        AssertTrue(
            movementPhaseManager.defendersMoved == 3,
            "MovementPhase Defenders Moved should be 3 after moving McNulty and resolving his tackling",
            3,
            movementPhaseManager.defendersMoved
        );
        Log("Clicking (4, 5) Select Stewart");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(4, 5), 0.5f));
        yield return new WaitForSeconds(0.5f);
        AssertTrue(
            movementPhaseManager.isWaitingForTackleDecisionWithoutMoving,
            "MovementPhase Should be waiting for Tackle Decision without moving when we select Stewart",
            true,
            movementPhaseManager.isWaitingForTackleDecisionWithoutMoving
        );
        AssertTrue(
            !movementPhaseManager.isWaitingForTackleDecision,
            "MovementPhase Should NOT be waiting for Tackle Decision without moving when we select Stewart",
            false,
            movementPhaseManager.isWaitingForTackleDecision
        );
        Log("Clicking (0, 7) Move Stewart away");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(0, 7), 0.5f));
        yield return new WaitForSeconds(1.5f);
        AssertTrue(
            !movementPhaseManager.isWaitingForTackleDecisionWithoutMoving,
            "MovementPhase Should be waiting for Tackle Decision without moving when we move Stewart away",
            false,
            movementPhaseManager.isWaitingForTackleDecisionWithoutMoving
        );
        AssertTrue(
            !movementPhaseManager.isWaitingForTackleDecision,
            "MovementPhase Should NOT be waiting for Tackle Decision without moving when we move Stewart away",
            false,
            movementPhaseManager.isWaitingForTackleDecision
        );
        AssertTrue(
            movementPhaseManager.defendersMoved == 4,
            "MovementPhase Defenders Moved should be 4 after moving Stewart",
            4,
            movementPhaseManager.defendersMoved
        );
        Log("Pressing X to Forfeit Rest of Defense Movement Phase");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        AssertTrue(
            movementPhaseManager.defendersMoved == 5,
            "MovementPhase Defenders Moved should be 5 after forfeting 5th move",
            5,
            movementPhaseManager.defendersMoved
        );
        // 2f2
        Log("Clicking (-6, -6), 10.Noruega");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(-6, -6), 0.5f));
        AssertTrue(
            movementPhaseManager.isAwaitingHexDestination,
            "MP Should be waiting for Hex Destination after Clicking on Noruega",
            true,
            movementPhaseManager.isAwaitingHexDestination
        );
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(-8, -6), 0.5f));
        Log("Clicking (-8, -6), Move 10.Noruega");
        yield return new WaitForSeconds(3f); // for the token to move
        AssertTrue(
            movementPhaseManager.attackersMovedIn2f2 == 1,
            "MP - 1 2f2 moved",
            1,
            movementPhaseManager.attackersMovedIn2f2
        );
        AssertTrue(
            !movementPhaseManager.isAwaitingHexDestination,
            "MP Should NOT be waiting for Hex Destination after Moving Noruega",
            false,
            movementPhaseManager.isAwaitingHexDestination
        );
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(12, 12), 0.5f));
        Log("Clicking (12, 12), 5.Murphy");
        AssertTrue(
            movementPhaseManager.isAwaitingHexDestination,
            "MP Should be waiting for Hex Destination after Clicking on Muprhy",
            true,
            movementPhaseManager.isAwaitingHexDestination
        );
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 12), 0.5f));
        Log("Clicking (10, 12), Move 5.Murphy");
        yield return new WaitForSeconds(3f); // for the token to move
        AvailabilityCheckResult mpcomplete = AssertCorrectAvailabilityAfterMovementComplete();
        AssertTrue(
            mpcomplete.passed,
            "MovementPhase Complete Check Status Availability",
            true,
            mpcomplete.ToString()
        );

        LogFooterofTest("MovementPhase Check Reposition Interceptions");
    }

    private IEnumerator Scenario_015_Movement_Phase_Check_NutmegWithoutMovement_tackle_Loose_Ball()
    {
        yield return new WaitForSeconds(1.5f); // Allow scene to stabilize
        Log("▶️ Starting test scenario: MovementPhase Check NutmegWithoutMovement And then 2 more successful nutmegs");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Alpha2, 0.1f));
        Log("Pressing 2");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Space, 0.1f));
        Log("Pressing Space");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.P, 0.1f));
        Log("Pressing P - Game is in Movement Phase");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        Log("Clicking (10, 0)");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        Log("Clicking (10, 0) again");
        yield return new WaitForSeconds(3f); // for the ball to move
        Log("Wait for the ball to move");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        Log("Pressing X - Forfeit Attack FinalThird");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        Log("Pressing X - Forfeit Defense FinalThird");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.M, 0.1f));
        Log("Pressing M - Game is in Movement Phase");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        Log("Clicking (10, 0) Select Yaneva");
        Log("Clicking (9, 0) Move Yaneva 1 pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(9, 0), 0.5f));
        yield return new WaitForSeconds(1.2f); // for the token to move
        Log("Clicking (8, 1) Move Yaneva 2nd pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(8, 1), 0.5f));
        yield return new WaitForSeconds(1.2f); // for the token to move
        Log("Clicking (7, 1) Move Yaneva 3rd pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(7, 1), 0.5f));
        yield return new WaitForSeconds(1.2f); // for the token to move
        Log("Clicking (6, 2) Move Yaneva 4th pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(6, 2), 0.5f));
        yield return new WaitForSeconds(1.2f); // for the token to move
        Log("Clicking (5, 2) Move Yaneva 5th pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(5, 2), 0.5f));
        yield return new WaitForSeconds(1.2f); // for the token to move
        Log("Pressing R to roll and Paterson and he fails!");
        StartCoroutine(movementPhaseManager.PerformBallInterceptionDiceRoll(2));
        Log("Pressing X to Forfeit Rest of Yaneva Pace");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        Log("Pressing X to Forfeit Rest of Attack Movement Phase");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        Log("Pressing X to Forfeit Defensive Movement Phase");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        Log("Pressing X to Forfeit 2f2 Attack Movement Phase");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        yield return new WaitForSeconds(0.5f); // for the token to move
        AvailabilityCheckResult mpcomplete = AssertCorrectAvailabilityAfterMovementComplete();
        AssertTrue(
            mpcomplete.passed,
            "MovementPhase Complete Check Status Availability",
            true,
            mpcomplete.ToString()
        );
        Log("Pressing M to Start New Movement Phase");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.M, 0.8f));
        Log("Clicking (5, 2) Select Yaneva");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(5, 2), 0.5f));
        AssertTrue(
            movementPhaseManager.isWaitingForNutmegDecisionWithoutMoving,
            "MovementPhase Should be waiting for Nutmeg Decision without moving before moving Yaneva",
            true,
            movementPhaseManager.isWaitingForNutmegDecisionWithoutMoving
        );
        AssertTrue(
            movementPhaseManager.isAwaitingTokenSelection,
            "MovementPhase Should be waiting for Another token selection after selecting Yaneva",
            false,
            movementPhaseManager.isAwaitingTokenSelection
        );
        AssertTrue(
            movementPhaseManager.isAwaitingHexDestination,
            "MovementPhase Should be waiting Hex Destination after selecting Yaneva",
            false,
            movementPhaseManager.isAwaitingHexDestination
        );
        Log("Pressing N to Nutmeg Paterson");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.N, 0.1f));
        yield return new WaitForSeconds(0.5f); // for the token to move
        AvailabilityCheckResult mpcommitment = AssertCorrectAvailabilityAfterMovementCommitment();
        AssertTrue(
            mpcommitment.passed,
            "MovementPhase Commitment Check Status Availability",
            true,
            mpcommitment.ToString()
        );
        AssertTrue(
            !movementPhaseManager.isWaitingForNutmegDecisionWithoutMoving,
            "MovementPhase Should NOT be waiting for Nutmeg Decision after calling Nutmeg",
            false,
            movementPhaseManager.isWaitingForNutmegDecisionWithoutMoving
        );
        // AssertTrue(
        //     movementPhaseManager.isAwaitingTokenSelection,
        //     "TODO: This must be OFF: MovementPhase Should be waiting for Another token selection after selecting Yaneva",
        //     false,
        //     movementPhaseManager.isAwaitingTokenSelection
        // );
        // AssertTrue(
        //     movementPhaseManager.isAwaitingHexDestination,
        //     "MovementPhase Should be waiting Hex Destination after selecting Yaneva",
        //     false,
        //     movementPhaseManager.isAwaitingHexDestination
        // );
        yield return new WaitForSeconds(1.0f); // for victim identification and nutmeg process
        AssertTrue(
            movementPhaseManager.isWaitingForTackleRoll,
            "MovementPhase Should be waiting for takling Rolls after calling Nutmeg",
            true,
            movementPhaseManager.isWaitingForTackleRoll
        );
        movementPhaseManager.PerformTackleDiceRoll(isDefender: true, 2);
        yield return new WaitForSeconds(0.2f);
        movementPhaseManager.PerformTackleDiceRoll(isDefender: false, 6);
        yield return new WaitForSeconds(0.2f);
        AssertTrue(
            movementPhaseManager.isWaitingForReposition,
            "MovementPhase Should be waiting for Reposition after Tackle Rolls",
            true,
            movementPhaseManager.isWaitingForReposition
        );
        var paterson = PlayerToken.GetPlayerTokenByName("Paterson");
        AssertTrue(
            movementPhaseManager.stunnedTokens.Contains(paterson),
            "MovementPhase Paterson should be now stunned",
            true,
            movementPhaseManager.stunnedTokens.Contains(paterson)
        );
        Log("Clicking (3, 2) Reposition Yaneva after Nutmeg on Paterson");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(3, 2), 0.5f));
        yield return new WaitForSeconds(1.2f); // for the token to move
        AssertTrue(
            movementPhaseManager.isWaitingForNutmegDecision,
            "MovementPhase Should be waiting for Nutmeg Decision after Reposition Yaneva from paterson",
            true,
            movementPhaseManager.isWaitingForNutmegDecision
        );
        Log("Pressing N to Nutmeg Gilbert");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.N, 0.1f));
        yield return new WaitForSeconds(0.5f);
        AssertTrue(
            !movementPhaseManager.isWaitingForNutmegDecision,
            "MovementPhase Should NOT be waiting for Nutmeg Decision after Call for Nutmeg on Gilbert",
            false,
            movementPhaseManager.isWaitingForNutmegDecision
        );
        AssertTrue(
            movementPhaseManager.isWaitingForTackleRoll,
            "MovementPhase Should be waiting for Tackle Rolls after Call for Nutmeg on Gilbert",
            true,
            movementPhaseManager.isWaitingForTackleRoll
        );
        yield return new WaitForSeconds(0.5f);
        movementPhaseManager.PerformTackleDiceRoll(isDefender: true, 2);
        yield return new WaitForSeconds(0.2f);
        movementPhaseManager.PerformTackleDiceRoll(isDefender: false, 6);
        yield return new WaitForSeconds(0.2f);
        AssertTrue(
            movementPhaseManager.isWaitingForReposition,
            "MovementPhase Should be waiting for Reposition after Tackle Rolls",
            true,
            movementPhaseManager.isWaitingForReposition
        );
        var gilbert = PlayerToken.GetPlayerTokenByName("Gilbert");
        AssertTrue(
            movementPhaseManager.stunnedTokens.Contains(gilbert),
            "MovementPhase Gilbert should be now stunned",
            true,
            movementPhaseManager.stunnedTokens.Contains(gilbert)
        );
        AssertTrue(
            movementPhaseManager.stunnedTokens.Contains(paterson),
            "MovementPhase Paterson should be now stunned",
            true,
            movementPhaseManager.stunnedTokens.Contains(paterson)
        );
        Log("Clicking (3, 4) Reposition Yaneva after Nutmeg on Paterson");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(3, 4), 0.5f));
        float repositionTimeout = 3f;
        while (movementPhaseManager.isWaitingForReposition && repositionTimeout > 0f)
        {
            repositionTimeout -= Time.deltaTime;
            yield return null;
        }
        yield return new WaitForSeconds(0.2f);
        AssertTrue(
            !movementPhaseManager.isWaitingForReposition,
            "MovementPhase Should NOT still be waiting for Reposition after clicking (3, 4)",
            false,
            movementPhaseManager.isWaitingForReposition
        );
        AssertTrue(
            movementPhaseManager.isWaitingForNutmegDecision,
            "MovementPhase Should be waiting for Nutmeg Decision after Reposition Yaneva from Gilbert",
            true,
            movementPhaseManager.isWaitingForNutmegDecision
        );
        Log("Pressing N to Nutmeg Stewart");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.N, 0.1f));
        yield return new WaitForSeconds(0.5f);
        AssertTrue(
            !movementPhaseManager.isWaitingForNutmegDecision,
            "MovementPhase Should NOT be waiting for Nutmeg Decision after Call for Nutmeg on Gilbert",
            false,
            movementPhaseManager.isWaitingForNutmegDecision
        );
        AssertTrue(
            movementPhaseManager.isWaitingForTackleRoll,
            "MovementPhase Should be waiting for Tackle Rolls after Call for Nutmeg on Gilbert",
            true,
            movementPhaseManager.isWaitingForTackleRoll
        );
        yield return new WaitForSeconds(0.5f);
        movementPhaseManager.PerformTackleDiceRoll(isDefender: true, 2);
        yield return new WaitForSeconds(0.2f);
        movementPhaseManager.PerformTackleDiceRoll(isDefender: false, 6);
        yield return new WaitForSeconds(0.2f);
        AssertTrue(
            movementPhaseManager.isWaitingForReposition,
            "MovementPhase Should be waiting for Reposition after Tackle Rolls",
            true,
            movementPhaseManager.isWaitingForReposition
        );
        var stewart = PlayerToken.GetPlayerTokenByName("Stewart");
        AssertTrue(
            movementPhaseManager.stunnedTokens.Contains(stewart),
            "MovementPhase Stewart should be now stunned",
            true,
            movementPhaseManager.stunnedTokens.Contains(stewart)
        );
        AssertTrue(
            movementPhaseManager.stunnedTokens.Contains(gilbert),
            "MovementPhase Gilbert should be now stunned",
            true,
            movementPhaseManager.stunnedTokens.Contains(gilbert)
        );
        AssertTrue(
            movementPhaseManager.stunnedTokens.Contains(paterson),
            "MovementPhase Paterson should be now stunned",
            true,
            movementPhaseManager.stunnedTokens.Contains(paterson)
        );
        Log("Clicking (4, 6) Reposition Yaneva after Nutmeg on Paterson");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(4, 6), 0.5f));
        yield return new WaitForSeconds(1.9f); // for the token to move
        AssertTrue(
            movementPhaseManager.isWaitingForInterceptionDiceRoll,
            "MP Should be waiting for Interception Roll after Rejected nutmeg",
            true,
            movementPhaseManager.isWaitingForInterceptionDiceRoll
        );
        AssertTrue(
            movementPhaseManager.eligibleDefenders.Count == 1,
            "MP eligibleDefenders should contain 1",
            1,
            movementPhaseManager.eligibleDefenders.Count
        );
        var mcNulty = PlayerToken.GetPlayerTokenByName("McNulty");
        AssertTrue(
            movementPhaseManager.eligibleDefenders.Contains(mcNulty),
            "MP eligibleDefenders should contain Soares",
            PlayerToken.GetPlayerTokenByName("Soares"),
            mcNulty
        );
        yield return new WaitForSeconds(1.0f); // for the token to move
        Log("Pressing R to roll and McNulty and he fails!");
        StartCoroutine(movementPhaseManager.PerformBallInterceptionDiceRoll(2));
        AssertTrue(
            movementPhaseManager.remainingDribblerPace == 0,
            "MovementPhase Yaneva should have 0 remaining pace after three successful nutmegs",
            true,
            movementPhaseManager.remainingDribblerPace == 0
        );        

        LogFooterofTest("MovementPhase Check NutmegWithoutMovement And then 2 more successful nutmegs");
    }

    private IEnumerator Scenario_016_Movement_Phase_Check_InterceptionFoul_Tackle_Foul_NewTackle_SuccessfulTackle()
    {
        yield return new WaitForSeconds(1.5f); // Allow scene to stabilize
        Log("▶️ Starting test scenario: MovementPhase Check InterceptionFoul Tackle Foul NewTackle SuccessfulTackle");
        Log("Pressing 2");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Alpha2, 0.1f));
        Log("Pressing Space");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Space, 0.1f));
        Log("Pressing P - Game is in Movement Phase");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.P, 0.1f));
        Log("Clicking (10, 0)");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        Log("Clicking (10, 0) again");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        Log("Wait for the ball to move");
        yield return new WaitForSeconds(3f); // for the ball to move
        Log("Pressing X - Forfeit Attack FinalThird");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        Log("Pressing X - Forfeit Defense FinalThird");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        Log("Pressing M - Game is in Movement Phase");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.M, 0.1f));
        Log("Clicking (10, 0) Select Yaneva");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        Log("Clicking (9, 0) Move Yaneva 1st Pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(9, 0), 0.5f));
        yield return new WaitForSeconds(0.8f); // for the ball to move
        Log("Clicking (8, 1) Move Yaneva 2nd Pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(8, 1), 0.5f));
        yield return new WaitForSeconds(0.8f); // for the ball to move
        Log("Clicking (7, 1) Move Yaneva 3rd Pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(7, 1), 0.5f));
        yield return new WaitForSeconds(0.8f); // for the ball to move
        Log("Clicking (6, 2) Move Yaneva 4th Pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(6, 2), 0.5f));
        yield return new WaitForSeconds(0.8f); // for the ball to move
        Log("Clicking (5, 2) Move Yaneva 5th Pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(5, 2), 0.5f));
        yield return new WaitForSeconds(0.8f); // for the ball to move
        Log("Pressing R to roll and Paterson and he fouls!");
        StartCoroutine(movementPhaseManager.PerformBallInterceptionDiceRoll(1));
        yield return new WaitForSeconds(0.6f); // for the ball to move
        Log("Pressing R to roll for a card on Paterson, Yellow!");
        movementPhaseManager.PerformLeniencyTest(6);
        yield return new WaitForSeconds(0.6f); // for the ball to move
        Log("Pressing R to roll for an injury on Yaneva, oh, she's injured!");
        movementPhaseManager.PerformInjuryTest(6);
        yield return new WaitForSeconds(0.6f); // for the ball to move
        Log("Pressing A - to play on");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.A, 0.1f));
        yield return new WaitForSeconds(0.6f); // for the ball to move
        AssertTrue(
            movementPhaseManager.movedTokens.Count == 1,
            "MovementPhase Should have 1 after Yaneva's movement",
            1,
            movementPhaseManager.movedTokens.Count
        );
        AssertTrue(
            movementPhaseManager.attackersMoved == 1,
            "MovementPhase Should be 1 after Yaneva's injury",
            1,
            movementPhaseManager.attackersMoved
        );
        yield return new WaitForSeconds(0.6f); // for the ball to move
        Log("Pressing X - Forfeit Attack MovementPhase");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        yield return new WaitForSeconds(0.6f); // for the ball to move
        AssertTrue(
            movementPhaseManager.attackersMoved == 4,
            "MovementPhase Should be 4 after movement forfeiting",
            4,
            movementPhaseManager.attackersMoved
        );
        Log("Clicking (4, 3) Select Paterson");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(4, 3), 0.5f));
        AssertTrue(
            movementPhaseManager.isWaitingForTackleDecisionWithoutMoving,
            "MovementPhase Should be waiting for Tackle Decision without moving before moving Paterson",
            true,
            movementPhaseManager.isWaitingForTackleDecisionWithoutMoving
        );
        AssertTrue(
            !movementPhaseManager.isWaitingForTackleDecision,
            "MovementPhase Should NOT be waiting for Tackle Decision before moving Paterson",
            false,
            movementPhaseManager.isWaitingForTackleDecision
        );
        Log("Pressing T - Tackle Yaneva with Paterson");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.T, 0.3f));
        yield return new WaitForSeconds(0.5f); // for the ball to move
        AssertTrue(
            movementPhaseManager.movedTokens.Count == 2,
            "MovementPhase Should have 2 after Paterson's call for tackle",
            1,
            movementPhaseManager.movedTokens.Count
        );
        AssertTrue(
            movementPhaseManager.attackersMoved == 4,
            "MovementPhase Should be 4 after movement forfeiting",
            4,
            movementPhaseManager.attackersMoved
        );
        AssertTrue(
            movementPhaseManager.defendersMoved == 0,
            "MovementPhase Should be 0 as Paterson's tackle is not resolved yet",
            0,
            movementPhaseManager.defendersMoved
        );
        AssertTrue(
            movementPhaseManager.isWaitingForTackleRoll,
            "MovementPhase Should be waiting for Tackle Rolls after calling Tackle",
            true,
            movementPhaseManager.isWaitingForTackleRoll
        );
        AssertTrue(
            movementPhaseManager.selectedDefender == PlayerToken.GetPlayerTokenByName("Paterson"),
            "MovementPhase Should be waiting for Tackle Rolls",
            PlayerToken.GetPlayerTokenByName("Paterson").name,
            movementPhaseManager.selectedDefender.name
        );
        movementPhaseManager.PerformTackleDiceRoll(isDefender: true, 1);
        yield return new WaitForSeconds(0.2f);
        AssertTrue(
            movementPhaseManager.defendersMoved == 0,
            "MovementPhase Should be 0 as Paterson's tackle is not resolved yet",
            0,
            movementPhaseManager.defendersMoved
        );
        movementPhaseManager.PerformTackleDiceRoll(isDefender: false, 6);
        yield return new WaitForSeconds(0.2f);
        AssertTrue(
            movementPhaseManager.defendersMoved == 0,
            "MovementPhase Should be 0 as Paterson's tackle is not resolved yet",
            0,
            movementPhaseManager.defendersMoved
        );
        AssertTrue(
            movementPhaseManager.isWaitingForYellowCardRoll,
            "MovementPhase Should be waiting for Yellow Card Rolls after Tackle Rolls",
            true,
            movementPhaseManager.isWaitingForYellowCardRoll
        );
        yield return new WaitForSeconds(0.6f);
        Log("Pressing R to roll for a card on Paterson, no Yellow!");
        movementPhaseManager.PerformLeniencyTest(1);
        yield return new WaitForSeconds(0.6f);
        AssertTrue(
            movementPhaseManager.defendersMoved == 0,
            "MovementPhase Should be 0 as Paterson's tackle is not resolved yet",
            0,
            movementPhaseManager.defendersMoved
        );
        AssertTrue(
            movementPhaseManager.isWaitingForInjuryRoll,
            "MovementPhase Should be waiting for Injury Rolls after Tackle Rolls",
            true,
            movementPhaseManager.isWaitingForInjuryRoll
        );
        yield return new WaitForSeconds(0.6f);
        Log("Pressing R to roll for an injury on Yaneva, NO injury!");
        movementPhaseManager.PerformInjuryTest(1);
        yield return new WaitForSeconds(0.6f);
        AssertTrue(
            movementPhaseManager.defendersMoved == 0,
            "MovementPhase Should be 0 as Paterson's tackle is not resolved yet",
            0,
            movementPhaseManager.defendersMoved
        );
        AssertTrue(
            movementPhaseManager.isWaitingForFoulDecision,
            "MovementPhase Should be waiting for Foul Decision after Tackle Rolls",
            true,
            movementPhaseManager.isWaitingForFoulDecision
        );
        AssertTrue(
            movementPhaseManager.defendersMoved == 0,
            "MovementPhase Should be 0 as Paterson's tackle is not resolved yet",
            0,
            movementPhaseManager.defendersMoved
        );
        Log("Pressing A - Play on");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.A, 0.3f));
        yield return new WaitForSeconds(0.6f);
        AssertTrue(
            movementPhaseManager.defendersMoved == 0,
            "MovementPhase Should be 0 as Paterson's tackle is not resolved yet",
            0,
            movementPhaseManager.defendersMoved
        );
        AssertTrue(
            movementPhaseManager.isWaitingForReposition,
            "MovementPhase Should be waiting for Reposition after Tackle Rolls",
            true,
            movementPhaseManager.isWaitingForReposition
        );
        AssertTrue(
            movementPhaseManager.stunnedTokens.Count == 0,
            "MovementPhase Should NOT have any stunned tokens after Tackle",
            true,
            movementPhaseManager.stunnedTokens.Count == 0
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Count == 2,
            "MovementPhase Should have 2 moved token after Tackle",
            2,
            movementPhaseManager.movedTokens.Count
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Contains(PlayerToken.GetPlayerTokenByName("Paterson")),
            "MovementPhase Should have Paterson moved after Tackle",
            true,
            movementPhaseManager.movedTokens.Contains(PlayerToken.GetPlayerTokenByName("Paterson"))
        );
        Log("Clicking (4, 2) Reposition Yaneva after Tackle");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(4, 2), 0.5f));
        yield return new WaitForSeconds(1.8f);
        AssertTrue(
            movementPhaseManager.defendersMoved == 1,
            "MovementPhase Should be 1 after Paterson's tackle",
            1,
            movementPhaseManager.defendersMoved
        );
        AssertTrue(
            !movementPhaseManager.isWaitingForInterceptionDiceRoll,
            "MP Should NOT be waiting for Interception Roll after Reposition Yaneva",
            false,
            movementPhaseManager.isWaitingForInterceptionDiceRoll
        );
        AssertTrue(
            movementPhaseManager.isAwaitingTokenSelection,
            "MP Should be waiting for Another token selection after Reposition Yaneva",
            true,
            movementPhaseManager.isAwaitingTokenSelection
        );
        Log("Clicking (3, 3) Select Gilbert");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(3, 3), 0.5f));
        yield return new WaitForSeconds(0.8f);
        AssertTrue(
            movementPhaseManager.isAwaitingHexDestination,
            "MP Should be waiting for Hex Destination after Selecting Gilbert",
            true,
            movementPhaseManager.isAwaitingHexDestination
        );
        AssertTrue(
            !movementPhaseManager.isWaitingForTackleDecision,
            "MP Should NOT be waiting for Tackle Decision after Selecting Gilbert",
            false,
            movementPhaseManager.isWaitingForTackleDecision
        );
        AssertTrue(
            !movementPhaseManager.isWaitingForTackleDecisionWithoutMoving,
            "MP Should be NOT waiting for Tackle Decision without moving after Selecting Gilbert",
            false,
            movementPhaseManager.isWaitingForTackleDecisionWithoutMoving
        );
        Log("Clicking (3, 2) Move Gilbert for the tackle");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(3, 2), 0.5f));
        yield return new WaitForSeconds(0.8f);
        AssertTrue(
            !movementPhaseManager.isAwaitingHexDestination,
            "MP Should NOT be waiting for Hex Destination after Moving Gilbert",
            false,
            movementPhaseManager.isAwaitingHexDestination
        );
        AssertTrue(
            movementPhaseManager.isWaitingForTackleDecision,
            "MP Should be waiting for Tackle Decision after Moving Gilbert",
            true,
            movementPhaseManager.isWaitingForTackleDecision
        );
        AssertTrue(
            !movementPhaseManager.isWaitingForTackleDecisionWithoutMoving,
            "MP Should be NOT waiting for Tackle Decision without moving after Moving Gilbert",
            false,
            movementPhaseManager.isWaitingForTackleDecisionWithoutMoving
        );
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.T, 0.3f));
        yield return new WaitForSeconds(0.5f); // for the ball to move
        AssertTrue(
            movementPhaseManager.isWaitingForTackleRoll,
            "MP Should be waiting for Tackle Rolls after Selecting Gilbert",
            true,
            movementPhaseManager.isWaitingForTackleRoll
        );
        AssertTrue(
            MatchManager.Instance.currentState == MatchManager.GameState.MovementPhase,
            "MP Should be in MovementPhase after Selecting Gilbert",
            true,
            MatchManager.Instance.currentState
        );
        AssertTrue(
            MatchManager.Instance.LastTokenToTouchTheBallOnPurpose == PlayerToken.GetPlayerTokenByName("Yaneva"),
            "LastTokenToTouchTheBallOnPurpose should be Yaneva",
            PlayerToken.GetPlayerTokenByName("Yaneva").playerName,
            MatchManager.Instance.LastTokenToTouchTheBallOnPurpose.playerName
        );
        AssertTrue(
            MatchManager.Instance.PreviousTokenToTouchTheBallOnPurpose == PlayerToken.GetPlayerTokenByName("Cafferata"),
            "LastTokenToTouchTheBallOnPurpose should be Cafferata",
            PlayerToken.GetPlayerTokenByName("Cafferata").playerName,
            MatchManager.Instance.PreviousTokenToTouchTheBallOnPurpose.playerName
        );
        movementPhaseManager.PerformTackleDiceRoll(isDefender: true, 6);
        yield return new WaitForSeconds(0.2f);
        movementPhaseManager.PerformTackleDiceRoll(isDefender: false, 2);
        yield return new WaitForSeconds(0.2f);
        Log("Pressing X to Forfeit Reposition of Gilbert");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.3f));
        yield return new WaitForSeconds(0.5f); // for the ball to move
        AssertTrue(
            MatchManager.Instance.currentState == MatchManager.GameState.SuccessfulTackle,
            "MP Should be in MovementPhase after the successful tackle",
            true,
            MatchManager.Instance.currentState
        );
        AssertTrue(
            MatchManager.Instance.LastTokenToTouchTheBallOnPurpose == PlayerToken.GetPlayerTokenByName("Gilbert"),
            "LastTokenToTouchTheBallOnPurpose should be Gilbert",
            PlayerToken.GetPlayerTokenByName("Gilbert").playerName,
            MatchManager.Instance.LastTokenToTouchTheBallOnPurpose.playerName
        );
        AssertTrue(
            MatchManager.Instance.PreviousTokenToTouchTheBallOnPurpose == null,
            "LastTokenToTouchTheBallOnPurpose should be NULL"
        );
        AvailabilityCheckResult successfulTackle = AssertCorrectAvailabilityAfterSuccessfulTackle();
        AssertTrue(
            successfulTackle.passed,
            "Availability after successful tackle",
            true,
            successfulTackle.ToString()
        );
        var yaneva = PlayerToken.GetPlayerTokenByName("Yaneva");
        var gilbert = PlayerToken.GetPlayerTokenByName("Gilbert");
        var paterson = PlayerToken.GetPlayerTokenByName("Paterson");
        var dribblerTeamStats = MatchManager.Instance.gameData.stats.GetTeamStats(yaneva.isHomeTeam);
        var tacklerTeamStats = MatchManager.Instance.gameData.stats.GetTeamStats(gilbert.isHomeTeam);
        (float patersonXD, float patersonXT) = CalculateExpectedGroundDuel(yaneva.dribbling, paterson.tackling);
        (float gilbertXD, float gilbertXT) = CalculateExpectedGroundDuel(yaneva.dribbling, gilbert.tackling);
        AssertTrue(
            MatchManager.Instance.gameData.stats.GetPlayerStats("Yaneva").groundDuelsInvolved == 2,
            "Yaneva should have 2 ground duels involved",
            2,
            MatchManager.Instance.gameData.stats.GetPlayerStats("Yaneva").groundDuelsInvolved
        );
        AssertTrue(
            MatchManager.Instance.gameData.stats.GetPlayerStats("Gilbert").groundDuelsInvolved == 1,
            "Gilbert should have 1 ground duel involved",
            1,
            MatchManager.Instance.gameData.stats.GetPlayerStats("Gilbert").groundDuelsInvolved
        );
        AssertTrue(
            MatchManager.Instance.gameData.stats.GetPlayerStats("Gilbert").groundDuelsWon == 1,
            "Gilbert should have 1 ground duel won",
            1,
            MatchManager.Instance.gameData.stats.GetPlayerStats("Gilbert").groundDuelsWon
        );
        AssertTrue(
            MatchManager.Instance.gameData.stats.GetPlayerStats("Gilbert").possessionWon == 1,
            "Gilbert should have 1 possession won from the successful tackle",
            1,
            MatchManager.Instance.gameData.stats.GetPlayerStats("Gilbert").possessionWon
        );
        AssertTrue(
            MatchManager.Instance.gameData.stats.GetPlayerStats("Yaneva").possessionLost == 1,
            "Yaneva should have 1 possession lost from the successful tackle",
            1,
            MatchManager.Instance.gameData.stats.GetPlayerStats("Yaneva").possessionLost
        );
        AssertApproximately(
            MatchManager.Instance.gameData.stats.GetPlayerStats("Yaneva").xDribbles,
            patersonXD + gilbertXD,
            0.0001f,
            "Yaneva should record xDribbles for both tackle duels she faced in the scenario");
        AssertApproximately(
            MatchManager.Instance.gameData.stats.GetPlayerStats("Gilbert").xTackles,
            gilbertXT,
            0.0001f,
            "Gilbert should record xTackles when the tackle duel starts");
        AssertApproximately(
            tacklerTeamStats.totalXTackles,
            patersonXT + gilbertXT,
            0.0001f,
            "The tacklers' team xTackles should roll up the Paterson and Gilbert tackle duel expectations");
        AssertApproximately(
            dribblerTeamStats.totalXDribbles,
            patersonXD + gilbertXD,
            0.0001f,
            "The dribbler's team xDribbles should roll up Yaneva's tackle duel expectations");

        LogFooterofTest("MovementPhase Check InterceptionFoul Tackle Foul NewTackle SuccessfulTackle");
    }

    private IEnumerator Scenario_017_Movement_Phase_Check_InterceptionFoul_NutmegLost()
    {
        yield return new WaitForSeconds(1.5f); // Allow scene to stabilize
        Log("▶️ Starting test scenario: MovementPhase Check InterceptionFoul Lost Nutmeg");
        Log("Pressing 2");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Alpha2, 0.1f));
        Log("Pressing Space");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Space, 0.1f));
        Log("Pressing P - Game is in Movement Phase");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.P, 0.1f));
        Log("Clicking (10, 0)");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        Log("Clicking (10, 0) again");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        Log("Wait for the ball to move");
        yield return new WaitForSeconds(3f); // for the ball to move
        Log("Pressing X - Forfeit Attack FinalThird");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        Log("Pressing X - Forfeit Defense FinalThird");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        Log("Pressing M - Game is in Movement Phase");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.M, 0.1f));
        Log("Clicking (10, 0) Select Yaneva");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        Log("Clicking (9, 0) Move Yaneva 1st Pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(9, 0), 0.5f));
        yield return new WaitForSeconds(0.8f); // for the ball to move
        Log("Clicking (8, 1) Move Yaneva 2nd Pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(8, 1), 0.5f));
        yield return new WaitForSeconds(0.8f); // for the ball to move
        Log("Clicking (7, 1) Move Yaneva 3rd Pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(7, 1), 0.5f));
        yield return new WaitForSeconds(0.8f); // for the ball to move
        Log("Clicking (6, 2) Move Yaneva 4th Pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(6, 2), 0.5f));
        yield return new WaitForSeconds(0.8f); // for the ball to move
        Log("Clicking (5, 2) Move Yaneva 5th Pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(5, 2), 0.5f));
        yield return new WaitForSeconds(0.8f); // for the ball to move
        Log("Pressing R to roll and Paterson and he fouls!");
        StartCoroutine(movementPhaseManager.PerformBallInterceptionDiceRoll(1));
        yield return new WaitForSeconds(0.6f); // for the ball to move
        Log("Pressing R to roll for a card on Paterson, Yellow!");
        movementPhaseManager.PerformLeniencyTest(6);
        yield return new WaitForSeconds(0.6f); // for the ball to move
        Log("Pressing R to roll for an injury on Yaneva, oh, she's injured!");
        movementPhaseManager.PerformInjuryTest(6);
        yield return new WaitForSeconds(0.6f); // for the ball to move
        Log("Pressing A - to play on");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.A, 0.1f));
        yield return new WaitForSeconds(0.6f); // for the ball to move
        AssertTrue(
            movementPhaseManager.movedTokens.Count == 1,
            "MovementPhase Should have 1 after Yaneva's movement",
            1,
            movementPhaseManager.movedTokens.Count
        );
        AssertTrue(
            movementPhaseManager.attackersMoved == 1,
            "MovementPhase Should be 1 after Yaneva's injury",
            1,
            movementPhaseManager.attackersMoved
        );
        yield return new WaitForSeconds(0.6f); // for the ball to move
        Log("Pressing X - Forfeit Attack MovementPhase");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        Log("Pressing X - Forfeit Defense MovementPhase");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.5f));
        Log("Pressing X - Forfeit 2f2 MovementPhase");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.5f));
        AvailabilityCheckResult mpcomplete = AssertCorrectAvailabilityAfterMovementComplete();
        AssertTrue(
            mpcomplete.passed,
            "MovementPhase Complete Check Status Availability",
            true,
            mpcomplete.ToString()
        );
        Log("Pressing M - New Movement Phase");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.M, 0.5f));
        Log("Clicking (5, 2) Select Yaneva");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(5, 2), 0.5f));
        AssertTrue(
            movementPhaseManager.isWaitingForNutmegDecisionWithoutMoving,
            "MovementPhase Should be waiting for Nutmeg Decision without moving before moving Yaneva",
            true,
            movementPhaseManager.isWaitingForNutmegDecisionWithoutMoving
        );
        AssertTrue(
            movementPhaseManager.isAwaitingTokenSelection,
            "MovementPhase Should be waiting for Another token selection after selecting Yaneva",
            false,
            movementPhaseManager.isAwaitingTokenSelection
        );
        AssertTrue(
            movementPhaseManager.isAwaitingHexDestination,
            "MovementPhase Should be waiting Hex Destination after selecting Yaneva",
            false,
            movementPhaseManager.isAwaitingHexDestination
        );
        Log("Pressing N to Nutmeg Paterson");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.N, 0.1f));
        yield return new WaitForSeconds(0.5f); // for the token to move
        AvailabilityCheckResult mpcommitment = AssertCorrectAvailabilityAfterMovementCommitment();
        AssertTrue(
            mpcommitment.passed,
            "MovementPhase Commitment Check Status Availability",
            true,
            mpcommitment.ToString()
        );
        AssertTrue(
            !movementPhaseManager.isWaitingForNutmegDecisionWithoutMoving,
            "MovementPhase Should NOT be waiting for Nutmeg Decision after calling Nutmeg",
            false,
            movementPhaseManager.isWaitingForNutmegDecisionWithoutMoving
        );
        AssertTrue(
            movementPhaseManager.isWaitingForTackleRoll,
            "MovementPhase Should be waiting for the tackle rolls",
            true,
            movementPhaseManager.isWaitingForTackleRoll
        );
        movementPhaseManager.PerformTackleDiceRoll(isDefender: true, 6);
        yield return new WaitForSeconds(0.2f);
        movementPhaseManager.PerformTackleDiceRoll(isDefender: false, 2);
        yield return new WaitForSeconds(1.2f);
        AssertTrue(
            movementPhaseManager.stunnedforNext.Contains(PlayerToken.GetPlayerTokenByName("Yaneva")),
            "Yaneva should be stunned for next",
            true,
            movementPhaseManager.stunnedforNext.Contains(PlayerToken.GetPlayerTokenByName("Yaneva"))
        );
        AssertTrue(
            movementPhaseManager.isWaitingForReposition,
            "Tackle Resolves with reposition",
            true,
            movementPhaseManager.isWaitingForReposition
        );
        yield return new WaitForSeconds(0.8f);
        Log("Clicking (6, 2) Reposition Paterson");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(6, 2), 0.5f));
        yield return new WaitForSeconds(1.2f);
        AssertTrue(
            MatchManager.Instance.LastTokenToTouchTheBallOnPurpose == PlayerToken.GetPlayerTokenByName("Paterson"),
            "Paterson should be the last to touch the ball",
            PlayerToken.GetPlayerTokenByName("Paterson").playerName,
            MatchManager.Instance.LastTokenToTouchTheBallOnPurpose.playerName
        );
        AvailabilityCheckResult successfulTackle = AssertCorrectAvailabilityAfterSuccessfulTackle();
        AssertTrue(
            successfulTackle.passed,
            "Availability after successful tackle",
            true,
            successfulTackle.ToString()
        );

        LogFooterofTest("MovementPhase Check InterceptionFoul Lost Nutmeg");
    }

    private IEnumerator Scenario_017b_Movement_Phase_Dribbler_Forfeit_Remaining_Pace()
    {
        yield return new WaitForSeconds(1.5f); // Allow scene to stabilize
        Log("▶ Starting test scenario: MovementPhase Dribbler Forfeits Remaining Pace");
        Log("Pressing 2");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Alpha2, 0.1f));
        Log("Pressing Space");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Space, 0.1f));
        Log("Pressing P - Standard Pass");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.P, 0.1f));
        Log("Clicking (10, 0)");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        Log("Clicking (10, 0) again");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        Log("Wait for the ball to move");
        yield return new WaitForSeconds(3f);

        AvailabilityCheckResult availabilityCheck = AssertCorrectAvailabilityAfterGBToPlayer();
        AssertTrue(
            availabilityCheck.passed,
            "Action Availability after GB to Player is correct",
            true,
            availabilityCheck.ToString()
        );

        Log("Pressing X - Forfeit Attack FinalThird");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        Log("Pressing X - Forfeit Defense FinalThird");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        Log("Pressing M - Start Movement Phase");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.M, 0.1f));
        yield return new WaitForSeconds(0.2f);

        Log("Clicking (10, 0) - Select Yaneva");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        Log("Clicking (10, 1) - Move Yaneva 1st Pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 1), 0.5f));
        yield return new WaitForSeconds(1.2f);
        Log("Clicking (10, 2) - Move Yaneva 2nd Pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 2), 0.5f));
        yield return new WaitForSeconds(1.2f);

        AssertTrue(
            movementPhaseManager.isDribblerRunning,
            "MovementPhase should still have Yaneva running before forfeiting remaining pace",
            true,
            movementPhaseManager.isDribblerRunning
        );
        AssertTrue(
            movementPhaseManager.remainingDribblerPace > 0,
            "MovementPhase should still have remaining dribbler pace before forfeiting",
            true,
            movementPhaseManager.remainingDribblerPace
        );

        Log("Pressing X - Forfeit Yaneva remaining pace");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        yield return new WaitForSeconds(0.3f);

        PlayerToken yaneva = PlayerToken.GetPlayerTokenByName("Yaneva");
        AssertTrue(
            movementPhaseManager.movedTokens.Contains(yaneva),
            "MovementPhase moved tokens should contain Yaneva after forfeiting remaining pace",
            true,
            movementPhaseManager.movedTokens.Contains(yaneva)
        );
        AssertTrue(
            movementPhaseManager.attackersMoved == 1,
            "MovementPhase attackers moved should be 1 after Yaneva forfeits remaining pace",
            1,
            movementPhaseManager.attackersMoved
        );
        AssertTrue(
            !movementPhaseManager.isDribblerRunning,
            "MovementPhase should no longer have an active dribbler after forfeiting remaining pace",
            false,
            movementPhaseManager.isDribblerRunning
        );
        AssertTrue(
            movementPhaseManager.isAwaitingTokenSelection,
            "MovementPhase should be ready for another attacker selection after Yaneva forfeits remaining pace",
            true,
            movementPhaseManager.isAwaitingTokenSelection
        );

        Log("Clicking (-6, -6) - Select Noruega");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(-6, -6), 0.5f));
        AssertTrue(
            movementPhaseManager.isAwaitingHexDestination,
            "MovementPhase should be waiting for Noruega destination",
            true,
            movementPhaseManager.isAwaitingHexDestination
        );
        Log("Clicking (-8, -6) - Move Noruega 2 paces");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(-8, -6), 0.5f));
        yield return new WaitForSeconds(3f);

        PlayerToken noruega = PlayerToken.GetPlayerTokenByName("André Noruega");
        AssertTrue(
            movementPhaseManager.movedTokens.Contains(noruega),
            "MovementPhase moved tokens should contain Noruega after his move",
            true,
            movementPhaseManager.movedTokens.Contains(noruega)
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Contains(yaneva),
            "MovementPhase moved tokens should still contain Yaneva after Noruega moves",
            true,
            movementPhaseManager.movedTokens.Contains(yaneva)
        );
        AssertTrue(
            movementPhaseManager.attackersMoved == 2,
            "MovementPhase attackers moved should be 2 after Yaneva forfeits and Noruega moves",
            2,
            movementPhaseManager.attackersMoved
        );

        LogFooterofTest("MovementPhase Dribbler Forfeits Remaining Pace");
    }

    private IEnumerator Scenario_017c_Movement_Phase_Mixed_Nutmeggable_And_Stealable_Defenders()
    {
        yield return new WaitForSeconds(1.5f);
        Log("> Starting test scenario: MovementPhase Mixed Nutmeggable And Stealable Defenders");
        Log("Pressing 2");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Alpha2, 0.1f));
        Log("Pressing Space");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Space, 0.1f));
        Log("Pressing P - Standard Pass");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.P, 0.1f));
        Log("Clicking (10, 0)");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        Log("Clicking (10, 0) again");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        yield return new WaitForSeconds(3f);
        Log("Pressing X - Forfeit Attack FinalThird");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        Log("Pressing X - Forfeit Defense FinalThird");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        Log("Pressing M - Start Movement Phase");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.M, 0.1f));

        Log("Move Yaneva to (7, 3)");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(9, 0), 0.5f));
        yield return new WaitForSeconds(1.0f);
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(8, 1), 0.5f));
        yield return new WaitForSeconds(1.0f);
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(7, 1), 0.5f));
        yield return new WaitForSeconds(1.0f);
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(6, 2), 0.5f));
        yield return new WaitForSeconds(1.0f);
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(7, 3), 0.5f));
        yield return new WaitForSeconds(1.2f);
        Log("Pressing X - Forfeit Yaneva remaining pace");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));

        Log("Move Nazef to (8, 6)");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(4, 4), 0.5f));
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(8, 6), 0.5f));
        yield return new WaitForSeconds(2.2f);

        Log("Pressing X - Forfeit Att MP");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));

        Log("Move Paterson to (4, 4)");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(4, 3), 0.5f));
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(4, 4), 0.5f));
        yield return new WaitForSeconds(1.0f);
        Log("Move Vladoiu to (3, 4)");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(1, 2), 0.5f));
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(3, 4), 0.5f));
        yield return new WaitForSeconds(1.2f);
        Log("Move McNulty to (4, 6)");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(5, 5), 0.5f));
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(4, 6), 0.5f));
        yield return new WaitForSeconds(1.0f);
        Log("Move Gilbert to (3, 5)");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(3, 3), 0.5f));
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(3, 5), 0.5f));
        yield return new WaitForSeconds(1.0f);

        Log("Pressing X - Forfeit Def MP");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        Log("Pressing X - Forfeit 2f2");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        Log("Pressing M - Start New Movement Phase");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.M, 0.1f));

        Log("Move Yaneva to (5, 4)");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(6, 2), 0.5f));
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(6, 3), 0.5f));
        yield return new WaitForSeconds(1.0f);
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(6, 4), 0.5f));
        yield return new WaitForSeconds(1.0f);
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(5, 4), 0.5f));
        yield return new WaitForSeconds(1.2f);

        PlayerToken paterson = PlayerToken.GetPlayerTokenByName("Paterson");
        PlayerToken stewart = PlayerToken.GetPlayerTokenByName("Stewart");
        AssertTrue(
            movementPhaseManager.isWaitingForNutmegDecision,
            "MovementPhase should be waiting for a nutmeg decision in the mixed-defender setup",
            true,
            movementPhaseManager.isWaitingForNutmegDecision
        );
        AssertTrue(
            movementPhaseManager.nutmeggableDefenders.Contains(paterson),
            "Paterson should be nutmeggable in the mixed-defender setup",
            true,
            movementPhaseManager.nutmeggableDefenders.Contains(paterson)
        );
        AssertTrue(
            !movementPhaseManager.nutmeggableDefenders.Contains(stewart),
            "Stewart should not be nutmeggable in the mixed-defender setup",
            false,
            movementPhaseManager.nutmeggableDefenders.Contains(stewart)
        );

        LogFooterofTest("MovementPhase Mixed Nutmeggable And Stealable Defenders");
    }

    private IEnumerator Scenario_017d_Movement_Phase_Multiple_Nutmeggable_Defenders_Reject_Nutmeg()
    {
        yield return new WaitForSeconds(1.5f);
        Log("> Starting test scenario: MovementPhase Multiple Nutmeggable Defenders Reject Nutmeg");
        Log("Pressing 2");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Alpha2, 0.1f));
        Log("Pressing Space");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Space, 0.1f));
        Log("Pressing P - Standard Pass");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.P, 0.1f));
        Log("Clicking (10, 0)");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        Log("Clicking (10, 0) again");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        yield return new WaitForSeconds(3f);
        Log("Pressing X - Forfeit Attack FinalThird");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        Log("Pressing X - Forfeit Defense FinalThird");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        Log("Pressing M - Start Movement Phase");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.M, 0.1f));

        Log("Move Yaneva to (6, 3)");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(9, 0), 0.5f));
        yield return new WaitForSeconds(1.0f);
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(8, 1), 0.5f));
        yield return new WaitForSeconds(1.0f);
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(7, 1), 0.5f));
        yield return new WaitForSeconds(1.0f);
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(6, 2), 0.5f));
        yield return new WaitForSeconds(1.0f);
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(6, 3), 0.5f));
        yield return new WaitForSeconds(1.2f);
        Log("Pressing X - Forfeit Yaneva remaining pace");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));

        Log("Move Nazef to (8, 6)");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(4, 4), 0.5f));
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(8, 6), 0.5f));
        yield return new WaitForSeconds(1.2f);

        Log("Pressing X - Forfeit Att MP");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        Log("Move Paterson to (3, 4)");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(4, 3), 0.5f));
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(3, 4), 0.5f));
        yield return new WaitForSeconds(1.0f);
        Log("Pressing X - Forfeit Def MP");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        Log("Pressing X - Forfeit 2f2");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        Log("Pressing M - Start New Movement Phase");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.M, 0.1f));

        Log("Move Yaneva to (4, 4) via (5, 3)");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(6, 3), 0.5f));
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(5, 3), 0.5f));
        yield return new WaitForSeconds(1.0f);
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(4, 4), 0.5f));
        yield return new WaitForSeconds(1.2f);

        PlayerToken gilbert = PlayerToken.GetPlayerTokenByName("Gilbert");
        PlayerToken paterson = PlayerToken.GetPlayerTokenByName("Paterson");
        PlayerToken stewart = PlayerToken.GetPlayerTokenByName("Stewart");
        AssertTrue(
            movementPhaseManager.isWaitingForNutmegDecision,
            "MovementPhase should be waiting for nutmeg decision with three nutmeggable defenders",
            true,
            movementPhaseManager.isWaitingForNutmegDecision
        );
        AssertTrue(
            movementPhaseManager.nutmeggableDefenders.Count == 3,
            "MovementPhase should identify 3 nutmeggable defenders",
            3,
            movementPhaseManager.nutmeggableDefenders.Count
        );
        AssertTrue(movementPhaseManager.nutmeggableDefenders.Contains(gilbert), "MovementPhase nutmeggable defenders should contain Gilbert");
        AssertTrue(movementPhaseManager.nutmeggableDefenders.Contains(paterson), "MovementPhase nutmeggable defenders should contain Paterson");
        AssertTrue(movementPhaseManager.nutmeggableDefenders.Contains(stewart), "MovementPhase nutmeggable defenders should contain Stewart");

        Log("Pressing X - Reject Nutmeg");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        yield return new WaitForSeconds(0.3f);

        AssertTrue(
            movementPhaseManager.isWaitingForInterceptionDiceRoll,
            "MovementPhase should be waiting for interception rolls after rejecting nutmeg",
            true,
            movementPhaseManager.isWaitingForInterceptionDiceRoll
        );
        AssertTrue(
            movementPhaseManager.eligibleDefenders.Count == 3,
            "MovementPhase should have 3 eligible defenders after rejecting nutmeg",
            3,
            movementPhaseManager.eligibleDefenders.Count
        );
        AssertTrue(
            movementPhaseManager.eligibleDefenders[0] == gilbert
            && movementPhaseManager.eligibleDefenders[1] == paterson
            && movementPhaseManager.eligibleDefenders[2] == stewart,
            "MovementPhase eligible defenders should follow current deterministic neighbor order Gilbert, Paterson, Stewart",
            "Gilbert, Paterson, Stewart",
            string.Join(", ", movementPhaseManager.eligibleDefenders.Select(token => token.playerName))
        );

        LogFooterofTest("MovementPhase Multiple Nutmeggable Defenders Reject Nutmeg");
    }

    private IEnumerator Scenario_017e_Movement_Phase_Multiple_Nutmeggable_Defenders_Select_Victim()
    {
        yield return StartCoroutine(Scenario_017d_Movement_Phase_Multiple_Nutmeggable_Defenders_Reject_Nutmeg_SetupOnly());

        PlayerToken gilbert = PlayerToken.GetPlayerTokenByName("Gilbert");
        PlayerToken paterson = PlayerToken.GetPlayerTokenByName("Paterson");
        PlayerToken stewart = PlayerToken.GetPlayerTokenByName("Stewart");

        AssertTrue(
            movementPhaseManager.isWaitingForNutmegDecision,
            "MovementPhase should be waiting for nutmeg decision before selecting a victim",
            true,
            movementPhaseManager.isWaitingForNutmegDecision
        );

        Log("Pressing N - Enter nutmeg victim selection");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.N, 0.1f));
        yield return new WaitForSeconds(0.3f);
        AssertTrue(
            movementPhaseManager.lookingForNutmegVictim,
            "MovementPhase should be looking for a nutmeg victim after pressing N",
            true,
            movementPhaseManager.lookingForNutmegVictim
        );

        Log("Clicking (3, 4) - Select Paterson as nutmeg victim");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(3, 4), 0.5f));
        yield return new WaitForSeconds(0.3f);
        AssertTrue(
            movementPhaseManager.nutmegVictim == paterson,
            "MovementPhase should lock Paterson as the nutmeg victim",
            paterson,
            movementPhaseManager.nutmegVictim
        );
        AssertTrue(
            movementPhaseManager.isWaitingForInterceptionDiceRoll,
            "MovementPhase should offer the other nutmeggable defenders steal attempts before the nutmeg challenge starts",
            true,
            movementPhaseManager.isWaitingForInterceptionDiceRoll
        );
        AssertTrue(
            !movementPhaseManager.isWaitingForTackleRoll,
            "MovementPhase should not start the nutmeg tackle rolls until the other nutmeggable defenders finish their steal attempts",
            false,
            movementPhaseManager.isWaitingForTackleRoll
        );
        AssertTrue(
            movementPhaseManager.eligibleDefenders.Count == 2,
            "MovementPhase should offer two non-selected nutmeggable defenders as pre-nutmeg stealers",
            2,
            movementPhaseManager.eligibleDefenders.Count
        );
        AssertTrue(
            movementPhaseManager.selectedDefender == gilbert,
            "MovementPhase should offer Gilbert first for the pre-nutmeg steal sequence",
            gilbert,
            movementPhaseManager.selectedDefender
        );

        Log("Roll Gilbert fail on pre-nutmeg steal");
        yield return StartCoroutine(movementPhaseManager.PerformBallInterceptionDiceRoll(2));
        yield return new WaitForSeconds(0.2f);
        AssertTrue(
            movementPhaseManager.isWaitingForInterceptionDiceRoll && movementPhaseManager.selectedDefender == stewart,
            "MovementPhase should move to Stewart after Gilbert fails the pre-nutmeg steal",
            true,
            movementPhaseManager.isWaitingForInterceptionDiceRoll && movementPhaseManager.selectedDefender == stewart
        );

        Log("Roll Stewart fail on pre-nutmeg steal");
        yield return StartCoroutine(movementPhaseManager.PerformBallInterceptionDiceRoll(2));
        yield return new WaitForSeconds(0.3f);
        AssertTrue(
            movementPhaseManager.isWaitingForTackleRoll,
            "MovementPhase should start the nutmeg tackle rolls after the other nutmeggable defenders fail their steal attempts",
            true,
            movementPhaseManager.isWaitingForTackleRoll
        );
        AssertTrue(
            !movementPhaseManager.isWaitingForInterceptionDiceRoll,
            "MovementPhase should finish the pre-nutmeg steal sequence before starting the nutmeg challenge",
            false,
            movementPhaseManager.isWaitingForInterceptionDiceRoll
        );

        Log("Roll defender fail, attacker win on nutmeg");
        movementPhaseManager.PerformTackleDiceRoll(isDefender: true, 2);
        yield return new WaitForSeconds(0.2f);
        movementPhaseManager.PerformTackleDiceRoll(isDefender: false, 6);
        yield return new WaitForSeconds(0.5f);

        AssertTrue(
            movementPhaseManager.isWaitingForReposition,
            "MovementPhase should be waiting for reposition after successful nutmeg",
            true,
            movementPhaseManager.isWaitingForReposition
        );
        AssertTrue(
            movementPhaseManager.stunnedTokens.Contains(paterson),
            "Paterson should be stunned after losing the nutmeg",
            true,
            movementPhaseManager.stunnedTokens.Contains(paterson)
        );
        AssertTrue(
            !movementPhaseManager.stunnedTokens.Contains(gilbert) && !movementPhaseManager.stunnedTokens.Contains(stewart),
            "Only the selected nutmeg victim should be stunned by the nutmeg resolution",
            true,
            !movementPhaseManager.stunnedTokens.Contains(gilbert) && !movementPhaseManager.stunnedTokens.Contains(stewart)
        );

        LogFooterofTest("MovementPhase Multiple Nutmeggable Defenders Select Victim");
    }

    private IEnumerator Scenario_017e_b_Movement_Phase_Multiple_Nutmeggable_Defenders_Select_Victim_Offers_Other_Steals()
    {
        yield return StartCoroutine(Scenario_017d_Movement_Phase_Multiple_Nutmeggable_Defenders_Reject_Nutmeg_SetupOnly());

        PlayerToken gilbert = PlayerToken.GetPlayerTokenByName("Gilbert");
        PlayerToken paterson = PlayerToken.GetPlayerTokenByName("Paterson");
        PlayerToken stewart = PlayerToken.GetPlayerTokenByName("Stewart");

        AssertTrue(
            movementPhaseManager.isWaitingForNutmegDecision,
            "MovementPhase should be waiting for nutmeg decision before selecting a victim",
            true,
            movementPhaseManager.isWaitingForNutmegDecision
        );

        Log("Pressing N - Enter nutmeg victim selection");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.N, 0.1f));
        yield return new WaitForSeconds(0.3f);
        AssertTrue(
            movementPhaseManager.lookingForNutmegVictim,
            "MovementPhase should be looking for a nutmeg victim after pressing N",
            true,
            movementPhaseManager.lookingForNutmegVictim
        );

        Log("Clicking (3, 4) - Select Paterson as nutmeg victim");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(3, 4), 0.5f));
        yield return new WaitForSeconds(0.3f);

        AssertTrue(
            movementPhaseManager.nutmegVictim == paterson,
            "MovementPhase should lock Paterson as the nutmeg victim before offering the other steals",
            paterson,
            movementPhaseManager.nutmegVictim
        );
        AssertTrue(
            movementPhaseManager.isWaitingForInterceptionDiceRoll,
            "MovementPhase should offer interception rolls from the other nutmeggable defenders before the nutmeg challenge starts",
            true,
            movementPhaseManager.isWaitingForInterceptionDiceRoll
        );
        AssertTrue(
            !movementPhaseManager.isWaitingForTackleRoll,
            "MovementPhase should not start the nutmeg tackle roll until the other nutmeggable defenders finish their steal attempts",
            false,
            movementPhaseManager.isWaitingForTackleRoll
        );
        AssertTrue(
            movementPhaseManager.eligibleDefenders.Count == 2,
            "MovementPhase should offer only the two non-selected nutmeggable defenders as stealers",
            2,
            movementPhaseManager.eligibleDefenders.Count
        );
        AssertTrue(
            movementPhaseManager.eligibleDefenders.Contains(gilbert)
            && movementPhaseManager.eligibleDefenders.Contains(stewart)
            && !movementPhaseManager.eligibleDefenders.Contains(paterson),
            "MovementPhase should offer Gilbert and Stewart, but not the selected victim Paterson, as pre-nutmeg stealers",
            "Gilbert, Stewart",
            string.Join(", ", movementPhaseManager.eligibleDefenders.Select(token => token.playerName))
        );
        AssertTrue(
            movementPhaseManager.selectedDefender == gilbert,
            "MovementPhase should offer Gilbert first based on current deterministic neighbor order once Paterson is reserved as the nutmeg victim",
            gilbert,
            movementPhaseManager.selectedDefender
        );

        LogFooterofTest("MovementPhase Multiple Nutmeggable Defenders Select Victim Offers Other Steals");
    }

    private IEnumerator Scenario_017d_Movement_Phase_Multiple_Nutmeggable_Defenders_Reject_Nutmeg_SetupOnly()
    {
        yield return new WaitForSeconds(1.5f);
        Log("> Setup helper: Multiple Nutmeggable Defenders");
        Log("Pressing 2");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Alpha2, 0.1f));
        Log("Pressing Space");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Space, 0.1f));
        Log("Pressing P - Standard Pass");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.P, 0.1f));
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        yield return new WaitForSeconds(3f);
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.M, 0.1f));

        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(9, 0), 0.5f));
        yield return new WaitForSeconds(1.0f);
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(8, 1), 0.5f));
        yield return new WaitForSeconds(1.0f);
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(7, 1), 0.5f));
        yield return new WaitForSeconds(1.0f);
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(6, 2), 0.5f));
        yield return new WaitForSeconds(1.0f);
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(6, 3), 0.5f));
        yield return new WaitForSeconds(1.2f);
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));

        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(4, 4), 0.5f));
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(8, 6), 0.5f));
        yield return new WaitForSeconds(1.2f);

        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(4, 3), 0.5f));
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(3, 4), 0.5f));
        yield return new WaitForSeconds(1.0f);
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.M, 0.1f));
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(6, 3), 0.5f));
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(5, 3), 0.5f));
        yield return new WaitForSeconds(1.0f);
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(4, 4), 0.5f));
        yield return new WaitForSeconds(1.2f);
    }

    private IEnumerator Scenario_017f_Movement_Phase_Same_Defender_Steals_Once_Per_Section_Per_Dribbler()
    {
        yield return new WaitForSeconds(1.5f);
        Log("> Starting test scenario: MovementPhase Same Defender Steals Once Per Section Per Dribbler");
        Log("Pressing 2");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Alpha2, 0.1f));
        Log("Pressing Space");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Space, 0.1f));
        Log("Pressing P - Standard Pass");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.P, 0.1f));
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        yield return new WaitForSeconds(3f);
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.M, 0.1f));

        Log("Move Yaneva to (8, 4)");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(9, 0), 0.5f));
        yield return new WaitForSeconds(1.0f);
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(8, 1), 0.5f));
        yield return new WaitForSeconds(1.0f);
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(8, 2), 0.5f));
        yield return new WaitForSeconds(1.0f);
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(8, 3), 0.5f));
        yield return new WaitForSeconds(1.0f);
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(8, 4), 0.5f));
        yield return new WaitForSeconds(1.2f);
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));

        Log("Move Paterson to (8, 5) and tackle Yaneva");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(4, 3), 0.5f));
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(8, 5), 0.5f));
        yield return new WaitForSeconds(1.5f);
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.T, 0.3f));
        yield return new WaitForSeconds(0.3f);
        movementPhaseManager.PerformTackleDiceRoll(isDefender: true, 5);
        yield return new WaitForSeconds(0.2f);
        movementPhaseManager.PerformTackleDiceRoll(isDefender: false, 3);
        yield return new WaitForSeconds(0.5f);
        AssertTrue(looseBallManager.isActivated, "LooseBall should be active after the tied tackle", true, looseBallManager.isActivated);

        Log("Rig loose ball north 6 to Toothnail");
        looseBallManager.PerformDirectionRoll(4);
        yield return new WaitForSeconds(0.2f);
        looseBallManager.PerformDistanceRoll(6);
        yield return new WaitForSeconds(3f);

        Log("Pressing X - Forfeit remaining Def MP");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        Log("Move Toothnail to (8, 7) and then (8, 6)");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(8, 8), 0.5f));
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(8, 7), 0.5f));
        yield return new WaitForSeconds(1.5f);
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(8, 6), 0.5f));
        yield return new WaitForSeconds(1.2f);

        PlayerToken paterson = PlayerToken.GetPlayerTokenByName("Paterson");
        AssertTrue(
            movementPhaseManager.isWaitingForInterceptionDiceRoll,
            "MovementPhase should be waiting for a new steal attempt on the different dribbler in 2f2",
            true,
            movementPhaseManager.isWaitingForInterceptionDiceRoll
        );
        AssertTrue(
            movementPhaseManager.selectedDefender == paterson,
            "Paterson should be allowed to steal again on a different dribbler within the same Movement Phase",
            paterson,
            movementPhaseManager.selectedDefender
        );

        LogFooterofTest("MovementPhase Same Defender Steals Once Per Section Per Dribbler");
    }

    private IEnumerator Scenario_017g_Movement_Phase_Successful_Tackle_Reposition_Triggers_Other_Attacker_Steal(bool stealSucceeds)
    {
        yield return new WaitForSeconds(1.5f);
        Log($"> Starting test scenario: MovementPhase Successful Tackle Reposition Triggers Other Attacker Steal ({(stealSucceeds ? "steal succeeds" : "steal fails")})");
        Log("Pressing 2");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Alpha2, 0.1f));
        Log("Pressing Space");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Space, 0.1f));
        Log("Pressing P - Ground pass to space at (7, 3)");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.P, 0.1f));
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(7, 3), 0.5f));
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(7, 3), 0.5f));
        Log("Attempting interception with 1 defender dice");
        groundBallManager.PerformGroundInterceptionDiceRoll(1);
        yield return new WaitForSeconds(3f);
        AssertTrue(
            MatchManager.Instance.hangingPassType == "ground",
            "The pass to space should leave a hanging ground pass",
            "ground",
            MatchManager.Instance.hangingPassType
        );

        Log("Select Yaneva and pick up the ball with V");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        AssertTrue(
            movementPhaseManager.isBallPickable,
            "Yaneva should be able to pick up the hanging pass",
            true,
            movementPhaseManager.isBallPickable
        );
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.V, 0.1f));
        yield return new WaitForSeconds(2.0f);

        PlayerToken yaneva = PlayerToken.GetPlayerTokenByName("Yaneva");
        AssertTrue(
            MatchManager.Instance.LastTokenToTouchTheBallOnPurpose == yaneva,
            "Picking up the hanging pass should set Yaneva as last token",
            yaneva,
            MatchManager.Instance.LastTokenToTouchTheBallOnPurpose
        );
        AssertTrue(
            MatchManager.Instance.PreviousTokenToTouchTheBallOnPurpose == PlayerToken.GetPlayerTokenByName("Cafferata"),
            "Picking up the hanging pass should complete Cafferata's pass",
            PlayerToken.GetPlayerTokenByName("Cafferata"),
            MatchManager.Instance.PreviousTokenToTouchTheBallOnPurpose
        );
        AssertTrue(
            MatchManager.Instance.gameData.stats.GetPlayerStats("Cafferata").passesCompleted == 1,
            "Picking up the hanging ground pass should credit Cafferata with a completed pass",
            1,
            MatchManager.Instance.gameData.stats.GetPlayerStats("Cafferata").passesCompleted
        );
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));

        Log("Move Nazef to (7, 5)");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(4, 4), 0.5f));
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(7, 5), 0.5f));
        yield return new WaitForSeconds(1.2f);
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));

        Log("Move Paterson to Yaneva and win tackle");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(4, 3), 0.5f));
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(7, 2), 0.5f));
        yield return new WaitForSeconds(1.2f);
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.T, 0.3f));
        yield return new WaitForSeconds(0.3f);
        movementPhaseManager.PerformTackleDiceRoll(isDefender: true, 6);
        yield return new WaitForSeconds(0.2f);
        movementPhaseManager.PerformTackleDiceRoll(isDefender: false, 2);
        yield return new WaitForSeconds(0.6f);
        AssertTrue(
            movementPhaseManager.isWaitingForReposition,
            "MovementPhase should be waiting for defender reposition after successful tackle",
            true,
            movementPhaseManager.isWaitingForReposition
        );
        Log("Clicking (6, 5) - Reposition Paterson into another attacker's ZOI");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(7, 4), 0.5f));
        yield return new WaitForSeconds(1.5f);

        PlayerToken nazef = PlayerToken.GetPlayerTokenByName("Nazef");
        AssertTrue(
            movementPhaseManager.isWaitingForInterceptionDiceRoll && movementPhaseManager.selectedDefender == nazef,
            "Repositioning a tackle winner into another attacker's ZOI should prompt that attacker to steal",
            nazef,
            movementPhaseManager.selectedDefender
        );

        Log($"Rigging Nazef's steal roll to {(stealSucceeds ? "6 (success)" : "1 (failure)")}");
        yield return StartCoroutine(movementPhaseManager.PerformBallInterceptionDiceRoll(stealSucceeds ? 6 : 1));
        yield return new WaitForSeconds(1.5f);

        if (stealSucceeds)
        {
            AssertTrue(
                MatchManager.Instance.currentState == MatchManager.GameState.AnyOtherScenario,
                "Successful post-tackle steal should switch the game to AnyOtherScenario",
                MatchManager.GameState.AnyOtherScenario,
                MatchManager.Instance.currentState
            );
            AssertTrue(
                MatchManager.Instance.LastTokenToTouchTheBallOnPurpose == nazef,
                "Nazef should become last token after the successful post-tackle steal",
                nazef,
                MatchManager.Instance.LastTokenToTouchTheBallOnPurpose
            );
            AvailabilityCheckResult availabilityCheck = AssertCorrectAvailabilityAnyOtherScenario();
            AssertTrue(
                availabilityCheck.IsSuccess,
                "Availability after successful post-tackle steal",
                true,
                availabilityCheck.GetFailureReport()
            );
        }
        else
        {
            PlayerToken paterson = PlayerToken.GetPlayerTokenByName("Paterson");
            AssertTrue(
                MatchManager.Instance.currentState == MatchManager.GameState.SuccessfulTackle,
                "Failed post-tackle steal should preserve the SuccessfulTackle outcome",
                MatchManager.GameState.SuccessfulTackle,
                MatchManager.Instance.currentState
            );
            AssertTrue(
                MatchManager.Instance.LastTokenToTouchTheBallOnPurpose == paterson,
                "Paterson should remain last token after the failed post-tackle steal",
                paterson,
                MatchManager.Instance.LastTokenToTouchTheBallOnPurpose
            );
            AvailabilityCheckResult successfulTackle = AssertCorrectAvailabilityAfterSuccessfulTackle();
            AssertTrue(
                successfulTackle.IsSuccess,
                "Availability after failed post-tackle steal",
                true,
                successfulTackle.GetFailureReport()
            );
        }

        LogFooterofTest($"MovementPhase Successful Tackle Reposition Triggers Other Attacker Steal ({(stealSucceeds ? "success" : "failure")})");
    }

    private IEnumerator Scenario_018_Movement_Phase_Check_Tackle_loose_interception()
    {
        yield return new WaitForSeconds(1.5f); // Allow scene to stabilize
        Log("▶️ Starting test scenario: MovementPhase Check Tackle Loose Ball Interception");
        Log("Pressing 2");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Alpha2, 0.1f));
        Log("Pressing Space");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Space, 0.1f));
        Log("Pressing P - Game is in Movement Phase");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.P, 0.1f));
        Log("Clicking (10, 0)");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        Log("Clicking (10, 0) again");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        Log("Wait for the ball to move");
        yield return new WaitForSeconds(3f); // for the ball to move
        Log("Pressing X - Forfeit Attack FinalThird");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        Log("Pressing X - Forfeit Defense FinalThird");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        Log("Pressing M - Game is in Movement Phase");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.M, 0.1f));
        Log("Clicking (10, 0) Select Yaneva");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        Log("Clicking (9, 0) Move Yaneva 1st Pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(9, 0), 0.5f));
        yield return new WaitForSeconds(0.8f); // for the ball to move
        Log("Clicking (8, 1) Move Yaneva 2nd Pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(8, 1), 0.5f));
        yield return new WaitForSeconds(0.8f); // for the ball to move
        Log("Clicking (7, 1) Move Yaneva 3rd Pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(7, 1), 0.5f));
        yield return new WaitForSeconds(0.8f); // for the ball to move
        Log("Clicking (6, 2) Move Yaneva 4th Pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(6, 2), 0.5f));
        yield return new WaitForSeconds(0.8f); // for the ball to move
        Log("Clicking (5, 2) Move Yaneva 5th Pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(5, 2), 0.5f));
        yield return new WaitForSeconds(0.8f); // for the ball to move
        Log("Pressing R to roll and Paterson and he fouls!");
        StartCoroutine(movementPhaseManager.PerformBallInterceptionDiceRoll(1));
        yield return new WaitForSeconds(0.6f); // for the ball to move
        Log("Pressing R to roll for a card on Paterson, Yellow!");
        movementPhaseManager.PerformLeniencyTest(6);
        yield return new WaitForSeconds(0.6f); // for the ball to move
        Log("Pressing R to roll for an injury on Yaneva, oh, she's injured!");
        movementPhaseManager.PerformInjuryTest(6);
        yield return new WaitForSeconds(0.6f); // for the ball to move
        Log("Pressing A - to play on");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.A, 0.1f));
        yield return new WaitForSeconds(0.6f); // for the ball to move
        AssertTrue(
            movementPhaseManager.movedTokens.Count == 1,
            "MovementPhase Should have 1 after Yaneva's movement",
            1,
            movementPhaseManager.movedTokens.Count
        );
        AssertTrue(
            movementPhaseManager.attackersMoved == 1,
            "MovementPhase Should be 1 after Yaneva's injury",
            1,
            movementPhaseManager.attackersMoved
        );
        yield return new WaitForSeconds(0.6f); // for the ball to move
        Log("Pressing X - Forfeit Attack MovementPhase");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        Log("Clicking (4, 3) Select Paterson");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(4, 3), 0.5f));
        AssertTrue(
            movementPhaseManager.isWaitingForTackleDecisionWithoutMoving,
            "MovementPhase Should be waiting for Tackle Decision without moving before moving Paterson",
            true,
            movementPhaseManager.isWaitingForTackleDecisionWithoutMoving
        );
        AssertTrue(
            !movementPhaseManager.isWaitingForTackleDecision,
            "MovementPhase Should NOT be waiting for Tackle Decision without moving before moving Paterson",
            false,
            movementPhaseManager.isWaitingForTackleDecision
        );
        Log("Clicking (5, 3) Move Paterson");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(5, 3), 0.5f));
        yield return new WaitForSeconds(1.8f);
        AssertTrue(
            !movementPhaseManager.isWaitingForTackleDecisionWithoutMoving,
            "MovementPhase Should NOT be waiting for Tackle Decision without moving after moving Paterson",
            false,
            movementPhaseManager.isWaitingForTackleDecisionWithoutMoving
        );
        AssertTrue(
            movementPhaseManager.isWaitingForTackleDecision,
            "MovementPhase Should be waiting for Tackle Decision without moving after moving Paterson",
            true,
            movementPhaseManager.isWaitingForTackleDecision
        );
        Log("Pressing T - Tackle Yaneva");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.T, 0.5f));
        yield return new WaitForSeconds(0.6f);
        movementPhaseManager.PerformTackleDiceRoll(isDefender: true, 5);
        yield return new WaitForSeconds(0.2f);
        movementPhaseManager.PerformTackleDiceRoll(isDefender: false, 4);
        yield return new WaitForSeconds(1.2f);
        AssertTrue(
            looseBallManager.isActivated,
            "Loose Ball Manager Should be activated after the loose ball caused by Paterson's tackle",
            true,
            looseBallManager.isActivated
        );
        AssertTrue(
            looseBallManager.isWaitingForDirectionRoll,
            "Loose Ball Manager Should be waiting for a Direction Roll after the loose ball caused by Paterson's tackle",
            true,
            looseBallManager.isWaitingForDirectionRoll
        );
        yield return new WaitForSeconds(0.5f);
        Log("Pressing R - Direction Roll North");
        looseBallManager.PerformDirectionRoll(4);
        yield return new WaitForSeconds(0.1f);
        AssertTrue(
            !looseBallManager.isWaitingForDirectionRoll,
            "Loose Ball Manager Should NOT be waiting for a Direction Roll after Direction Roll",
            false,
            looseBallManager.isWaitingForDirectionRoll
        );
        AssertTrue(
            looseBallManager.isWaitingForDistanceRoll,
            "Loose Ball Manager Should be waiting for a Distance Roll after Direction Roll",
            true,
            looseBallManager.isWaitingForDistanceRoll
        );
        yield return new WaitForSeconds(0.5f);
        Log("Pressing R - Distance Roll 6");
        looseBallManager.PerformDistanceRoll(6);
        yield return new WaitForSeconds(0.5f);
        AssertTrue(
            !looseBallManager.isWaitingForDirectionRoll,
            "Loose Ball Manager Should NOT be waiting for a Direction Roll after Distance Roll",
            false,
            looseBallManager.isWaitingForDirectionRoll
        );
        AssertTrue(
            !looseBallManager.isWaitingForDistanceRoll,
            "Loose Ball Manager NOT Should be waiting for a Distance Roll after Direction Roll",
            false,
            looseBallManager.isWaitingForDistanceRoll
        );
        AssertTrue(
            looseBallManager.isWaitingForInterceptionRoll,
            "Loose Ball Manager Should be waiting for an Interception Roll after Direction Roll",
            true,
            looseBallManager.isWaitingForInterceptionRoll
        );
        AssertTrue(
            looseBallManager.potentialInterceptor == PlayerToken.GetPlayerTokenByName("Stewart"),
            "Loose ball should be waiting for an interception from ",
            PlayerToken.GetPlayerTokenByName("Stewart"),
            looseBallManager.potentialInterceptor
        );
        yield return new WaitForSeconds(0.5f);
        Log("Pressing R - Interception Roll 6");
        looseBallManager.PerformInterceptionRoll(6);
        yield return new WaitForSeconds(2.5f); // wait for the ball to move
        yield return new WaitForSeconds(0.5f); // wait for calculations
        AvailabilityCheckResult availabilityCheck = AssertCorrectAvailabilityAnyOtherScenario();
        AssertTrue(
            availabilityCheck.passed,
            "Action Availability after Interception (Any Other Scenario)",
            true,
            availabilityCheck.ToString()
        );
        AssertTrue(
            MatchManager.Instance.LastTokenToTouchTheBallOnPurpose == PlayerToken.GetPlayerTokenByName("Stewart"),
            "Stewart should be the LastTokenToTouchTheBallOnPurpose",
            PlayerToken.GetPlayerTokenByName("Stewart").playerName,
            MatchManager.Instance.LastTokenToTouchTheBallOnPurpose.playerName
        );
        // AssertTrue(
        //     false,
        //     "break"
        // );

        LogFooterofTest("MovementPhase Check Tackle Loose Ball Interception");
    }

    private IEnumerator Scenario_019_Movement_Phase_Check_Tackle_loose_interception_missed_hit_defender()
    {
        yield return new WaitForSeconds(1.5f); // Allow scene to stabilize
        Log("▶️ Starting test scenario: MovementPhase Check Tackle Loose Ball Missed Interceptio - Hit Defender");
        Log("Pressing 2");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Alpha2, 0.1f));
        Log("Pressing Space");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Space, 0.1f));
        Log("Pressing P - Game is in Movement Phase");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.P, 0.1f));
        Log("Clicking (10, 0)");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        Log("Clicking (10, 0) again");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        Log("Wait for the ball to move");
        yield return new WaitForSeconds(3f); // for the ball to move
        Log("Pressing X - Forfeit Attack FinalThird");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        Log("Pressing X - Forfeit Defense FinalThird");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        Log("Pressing M - Game is in Movement Phase");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.M, 0.1f));
        Log("Clicking (10, 0) Select Yaneva");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        Log("Clicking (9, 0) Move Yaneva 1st Pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(9, 0), 0.5f));
        yield return new WaitForSeconds(0.8f); // for the ball to move
        Log("Clicking (8, 1) Move Yaneva 2nd Pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(8, 1), 0.5f));
        yield return new WaitForSeconds(0.8f); // for the ball to move
        Log("Clicking (7, 1) Move Yaneva 3rd Pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(7, 1), 0.5f));
        yield return new WaitForSeconds(0.8f); // for the ball to move
        Log("Clicking (6, 2) Move Yaneva 4th Pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(6, 2), 0.5f));
        yield return new WaitForSeconds(0.8f); // for the ball to move
        Log("Clicking (5, 2) Move Yaneva 5th Pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(5, 2), 0.5f));
        yield return new WaitForSeconds(0.8f); // for the ball to move
        Log("Pressing R to roll and Paterson and he fouls!");
        StartCoroutine(movementPhaseManager.PerformBallInterceptionDiceRoll(1));
        yield return new WaitForSeconds(0.6f); // for the ball to move
        Log("Pressing R to roll for a card on Paterson, Yellow!");
        movementPhaseManager.PerformLeniencyTest(6);
        yield return new WaitForSeconds(0.6f); // for the ball to move
        Log("Pressing R to roll for an injury on Yaneva, oh, she's injured!");
        movementPhaseManager.PerformInjuryTest(6);
        yield return new WaitForSeconds(0.6f); // for the ball to move
        Log("Pressing A - to play on");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.A, 0.1f));
        yield return new WaitForSeconds(0.6f); // for the ball to move
        AssertTrue(
            movementPhaseManager.movedTokens.Count == 1,
            "MovementPhase Should have 1 after Yaneva's movement",
            1,
            movementPhaseManager.movedTokens.Count
        );
        AssertTrue(
            movementPhaseManager.attackersMoved == 1,
            "MovementPhase Should be 1 after Yaneva's injury",
            1,
            movementPhaseManager.attackersMoved
        );
        yield return new WaitForSeconds(0.6f); // for the ball to move
        Log("Pressing X - Forfeit Attack MovementPhase");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        Log("Clicking (4, 3) Select Paterson");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(4, 3), 0.5f));
        AssertTrue(
            movementPhaseManager.isWaitingForTackleDecisionWithoutMoving,
            "MovementPhase Should be waiting for Tackle Decision without moving before moving Paterson",
            true,
            movementPhaseManager.isWaitingForTackleDecisionWithoutMoving
        );
        AssertTrue(
            !movementPhaseManager.isWaitingForTackleDecision,
            "MovementPhase Should NOT be waiting for Tackle Decision without moving before moving Paterson",
            false,
            movementPhaseManager.isWaitingForTackleDecision
        );
        Log("Clicking (5, 3) Move Paterson");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(5, 3), 0.5f));
        yield return new WaitForSeconds(1.8f);
        AssertTrue(
            !movementPhaseManager.isWaitingForTackleDecisionWithoutMoving,
            "MovementPhase Should NOT be waiting for Tackle Decision without moving after moving Paterson",
            false,
            movementPhaseManager.isWaitingForTackleDecisionWithoutMoving
        );
        AssertTrue(
            movementPhaseManager.isWaitingForTackleDecision,
            "MovementPhase Should be waiting for Tackle Decision without moving after moving Paterson",
            true,
            movementPhaseManager.isWaitingForTackleDecision
        );
        Log("Pressing T - Tackle Yaneva");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.T, 0.5f));
        yield return new WaitForSeconds(0.6f);
        movementPhaseManager.PerformTackleDiceRoll(isDefender: true, 5);
        yield return new WaitForSeconds(0.2f);
        movementPhaseManager.PerformTackleDiceRoll(isDefender: false, 4);
        yield return new WaitForSeconds(1.2f);
        AssertTrue(
            looseBallManager.isActivated,
            "Loose Ball Manager Should be activated after the loose ball caused by Paterson's tackle",
            true,
            looseBallManager.isActivated
        );
        AssertTrue(
            looseBallManager.isWaitingForDirectionRoll,
            "Loose Ball Manager Should be waiting for a Direction Roll after the loose ball caused by Paterson's tackle",
            true,
            looseBallManager.isWaitingForDirectionRoll
        );
        yield return new WaitForSeconds(0.5f);
        Log("Pressing R - Direction Roll North");
        looseBallManager.PerformDirectionRoll(4);
        yield return new WaitForSeconds(0.1f);
        AssertTrue(
            !looseBallManager.isWaitingForDirectionRoll,
            "Loose Ball Manager Should NOT be waiting for a Direction Roll after Direction Roll",
            false,
            looseBallManager.isWaitingForDirectionRoll
        );
        AssertTrue(
            looseBallManager.isWaitingForDistanceRoll,
            "Loose Ball Manager Should be waiting for a Distance Roll after Direction Roll",
            true,
            looseBallManager.isWaitingForDistanceRoll
        );
        yield return new WaitForSeconds(0.5f);
        Log("Pressing R - Distance Roll 6");
        looseBallManager.PerformDistanceRoll(6);
        yield return new WaitForSeconds(0.5f);
        AssertTrue(
            !looseBallManager.isWaitingForDirectionRoll,
            "Loose Ball Manager Should NOT be waiting for a Direction Roll after Distance Roll",
            false,
            looseBallManager.isWaitingForDirectionRoll
        );
        AssertTrue(
            !looseBallManager.isWaitingForDistanceRoll,
            "Loose Ball Manager NOT Should be waiting for a Distance Roll after Direction Roll",
            false,
            looseBallManager.isWaitingForDistanceRoll
        );
        AssertTrue(
            looseBallManager.isWaitingForInterceptionRoll,
            "Loose Ball Manager Should be waiting for an Interception Roll after Direction Roll",
            true,
            looseBallManager.isWaitingForInterceptionRoll
        );
        AssertTrue(
            looseBallManager.potentialInterceptor == PlayerToken.GetPlayerTokenByName("Stewart"),
            "Loose ball should be waiting for an interception from ",
            PlayerToken.GetPlayerTokenByName("Stewart"),
            looseBallManager.potentialInterceptor
        );
        yield return new WaitForSeconds(0.5f);
        Log("Pressing R - Interception Roll Missed - Should move on McNulty");
        looseBallManager.PerformInterceptionRoll(1);
        yield return new WaitForSeconds(0.5f); 
        yield return new WaitForSeconds(2.5f); // wait for the ball to move
        AvailabilityCheckResult availabilityCheck = AssertCorrectAvailabilityAnyOtherScenario();
        AssertTrue(
            availabilityCheck.passed,
            "Action Availability after Interception (Any Other Scenario)",
            true,
            availabilityCheck.ToString()
        );
        AssertTrue(
            MatchManager.Instance.LastTokenToTouchTheBallOnPurpose == PlayerToken.GetPlayerTokenByName("McNulty"),
            "McNulty should be the LastTokenToTouchTheBallOnPurpose",
            PlayerToken.GetPlayerTokenByName("McNulty").playerName,
            MatchManager.Instance.LastTokenToTouchTheBallOnPurpose.playerName
        );
        // AssertTrue(
        //     false,
        //     "break"
        // );

        LogFooterofTest("MovementPhase Check Tackle Loose Ball Missed Interceptio - Hit Defender");
    }

    private IEnumerator Scenario_020_Movement_Phase_Check_Tackle_loose_interception_missed_hit_attacker_new_tackle_throw_in()
    {
        yield return new WaitForSeconds(1.5f); // Allow scene to stabilize
        Log("▶️ Starting test scenario: MovementPhase Check Tackle Loose Ball Missed Interceptio - Hit Attacker, new tackle loose ball, throw in");
        Log("Pressing 2");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Alpha2, 0.1f));
        Log("Pressing Space");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Space, 0.1f));
        Log("Pressing P - Game is in Movement Phase");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.P, 0.1f));
        Log("Clicking (10, 0)");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        Log("Clicking (10, 0) again");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        Log("Wait for the ball to move");
        yield return new WaitForSeconds(3f); // for the ball to move
        Log("Pressing X - Forfeit Attack FinalThird");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        Log("Pressing X - Forfeit Defense FinalThird");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        Log("Pressing M - Game is in Movement Phase");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.M, 0.1f));
        Log("Clicking (10, 0) Select Yaneva");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        Log("Clicking (9, 0) Move Yaneva 1st Pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(9, 0), 0.5f));
        AssertTrue(
            !movementPhaseManager.isAwaitingTokenSelection,
            "Defensive Movement DOES NOT expect a token to continue after moving Yaneva 1",
            true,
            movementPhaseManager.isAwaitingTokenSelection
        );
        yield return new WaitForSeconds(0.8f); // for the ball to move
        Log("Clicking (8, 1) Move Yaneva 2nd Pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(8, 1), 0.5f));
        yield return new WaitForSeconds(0.8f); // for the ball to move
        AssertTrue(
            !movementPhaseManager.isAwaitingTokenSelection,
            "Defensive Movement DOES NOT expect a token to continue after moving Yaneva 2",
            true,
            movementPhaseManager.isAwaitingTokenSelection
        );
        Log("Clicking (7, 1) Move Yaneva 3rd Pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(7, 1), 0.5f));
        yield return new WaitForSeconds(0.8f); // for the ball to move
        AssertTrue(
            !movementPhaseManager.isAwaitingTokenSelection,
            "Defensive Movement DOES NOT expect a token to continue after moving Yaneva 3",
            true,
            movementPhaseManager.isAwaitingTokenSelection
        );
        Log("Clicking (6, 2) Move Yaneva 4th Pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(6, 2), 0.5f));
        yield return new WaitForSeconds(0.6f); // for the ball to move
        AssertTrue(
            !movementPhaseManager.isAwaitingTokenSelection,
            "Defensive Movement DOES NOT expect a token to continue after moving Yaneva 4",
            true,
            movementPhaseManager.isAwaitingTokenSelection
        );
        yield return new WaitForSeconds(0.6f); // for the ball to move
        Log("Pressing X - Forfeit Yaneva's Pace");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.4f));
        yield return new WaitForSeconds(0.6f); // for the ball to move
        AssertTrue(
            movementPhaseManager.isAwaitingTokenSelection,
            "Defensive Movement DOES expect a token to continue after forfeiting Yaneva",
            true,
            movementPhaseManager.isAwaitingTokenSelection
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Contains(PlayerToken.GetPlayerTokenByName("Yaneva")),
            "MovementPhase Should moved tokens should contain Yaneva after resolving their movement",
            true,
            movementPhaseManager.movedTokens.Contains(PlayerToken.GetPlayerTokenByName("Yaneva"))
        );
        Log("Pressing X - Forfeit Attack MovementPhase");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        Log("Clicking (4, 3) Select Paterson");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(4, 3), 0.5f));
        AssertTrue(
            !movementPhaseManager.isWaitingForTackleDecisionWithoutMoving,
            "MovementPhase Should NOT be waiting for Tackle Decision without moving before moving Paterson",
            false,
            movementPhaseManager.isWaitingForTackleDecisionWithoutMoving
        );
        AssertTrue(
            !movementPhaseManager.isWaitingForTackleDecision,
            "MovementPhase Should NOT be waiting for Tackle Decision without moving before moving Paterson",
            false,
            movementPhaseManager.isWaitingForTackleDecision
        );
        Log("Clicking (6, 3) Move Paterson");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(6, 3), 0.5f));
        yield return new WaitForSeconds(1.8f);
        AssertTrue(
            !movementPhaseManager.isWaitingForTackleDecisionWithoutMoving,
            "MovementPhase Should NOT be waiting for Tackle Decision without moving after moving Paterson",
            false,
            movementPhaseManager.isWaitingForTackleDecisionWithoutMoving
        );
        AssertTrue(
            movementPhaseManager.isWaitingForTackleDecision,
            "MovementPhase Should be waiting for Tackle Decision without moving after moving Paterson",
            true,
            movementPhaseManager.isWaitingForTackleDecision
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Contains(PlayerToken.GetPlayerTokenByName("Paterson")),
            "MovementPhase Should moved tokens should contain Paterson after resolving their movement",
            true,
            movementPhaseManager.movedTokens.Contains(PlayerToken.GetPlayerTokenByName("Paterson"))
        );
        AssertTrue(
            movementPhaseManager.defendersMoved == 0,
            "MP - 0 defenders moved. Stewart's movement is not resolved yet",
            0,
            movementPhaseManager.defendersMoved
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Count == 2,
            "MovementPhase Should have 1 after Yaneva. Paterson's movement is not resolved yet",
            2,
            movementPhaseManager.movedTokens.Count
        );
        yield return new WaitForSeconds(1.6f);
        Log("Pressing T - Tackle Yaneva");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.T, 0.5f));
        yield return new WaitForSeconds(0.6f);
        movementPhaseManager.PerformTackleDiceRoll(isDefender: true, 6);
        yield return new WaitForSeconds(0.2f);
        movementPhaseManager.PerformTackleDiceRoll(isDefender: false, 4);
        yield return new WaitForSeconds(1.2f);
        AssertTrue(
            looseBallManager.isActivated,
            "Loose Ball Manager Should be activated after the loose ball caused by Paterson's tackle",
            true,
            looseBallManager.isActivated
        );
        AssertTrue(
            looseBallManager.isWaitingForDirectionRoll,
            "Loose Ball Manager Should be waiting for a Direction Roll after the loose ball caused by Paterson's tackle",
            true,
            looseBallManager.isWaitingForDirectionRoll
        );
        yield return new WaitForSeconds(0.5f);
        Log("Pressing R - Direction Roll North");
        looseBallManager.PerformDirectionRoll(4);
        yield return new WaitForSeconds(0.1f);
        AssertTrue(
            !looseBallManager.isWaitingForDirectionRoll,
            "Loose Ball Manager Should NOT be waiting for a Direction Roll after Direction Roll",
            false,
            looseBallManager.isWaitingForDirectionRoll
        );
        AssertTrue(
            looseBallManager.isWaitingForDistanceRoll,
            "Loose Ball Manager Should be waiting for a Distance Roll after Direction Roll",
            true,
            looseBallManager.isWaitingForDistanceRoll
        );
        yield return new WaitForSeconds(0.5f);
        Log("Pressing R - Distance Roll 6");
        looseBallManager.PerformDistanceRoll(6);
        yield return new WaitForSeconds(0.5f);
        AssertTrue(
            !looseBallManager.isWaitingForDirectionRoll,
            "Loose Ball Manager Should NOT be waiting for a Direction Roll after Distance Roll",
            false,
            looseBallManager.isWaitingForDirectionRoll
        );
        AssertTrue(
            !looseBallManager.isWaitingForDistanceRoll,
            "Loose Ball Manager NOT Should be waiting for a Distance Roll after Direction Roll",
            false,
            looseBallManager.isWaitingForDistanceRoll
        );
        AssertTrue(
            looseBallManager.isWaitingForInterceptionRoll,
            "Loose Ball Manager Should be waiting for an Interception Roll after Direction Roll",
            true,
            looseBallManager.isWaitingForInterceptionRoll
        );
        AssertTrue(
            looseBallManager.potentialInterceptor == PlayerToken.GetPlayerTokenByName("McNulty"),
            "Loose ball should be waiting for an interception from McNulty",
            PlayerToken.GetPlayerTokenByName("McNulty"),
            looseBallManager.potentialInterceptor
        );
        yield return new WaitForSeconds(0.5f);
        Log("Pressing R - Interception Roll Missed - Should move on Kalla");
        looseBallManager.PerformInterceptionRoll(1);
        yield return new WaitForSeconds(0.5f); 
        yield return new WaitForSeconds(2.5f); // wait for the ball to move
        AssertTrue(
            MatchManager.Instance.LastTokenToTouchTheBallOnPurpose == PlayerToken.GetPlayerTokenByName("Kalla"),
            "Kalla should be the LastTokenToTouchTheBallOnPurpose",
            PlayerToken.GetPlayerTokenByName("Kalla").playerName,
            MatchManager.Instance.LastTokenToTouchTheBallOnPurpose.playerName
        );
        AssertTrue(
            MatchManager.Instance.PreviousTokenToTouchTheBallOnPurpose == null,
            "A defender-caused loose ball that hits an attacker should clear the previous token to avoid assists"
        );
        AssertTrue(
            movementPhaseManager.defendersMoved == 1,
            "MP - 1 defenders moved",
            1,
            movementPhaseManager.defendersMoved
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Count == 2,
            "MovementPhase Should have 2 after Yaneva and Stewart's movement",
            2,
            movementPhaseManager.movedTokens.Count
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Contains(PlayerToken.GetPlayerTokenByName("Yaneva")),
            "MovementPhase Should moved tokens should contain Yaneva after resolving their movement",
            true,
            movementPhaseManager.movedTokens.Contains(PlayerToken.GetPlayerTokenByName("Yaneva"))
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Contains(PlayerToken.GetPlayerTokenByName("Paterson")),
            "MovementPhase Should moved tokens should contain Paterson after resolving their movement",
            true,
            movementPhaseManager.movedTokens.Contains(PlayerToken.GetPlayerTokenByName("Paterson"))
        );
        AssertTrue(
            movementPhaseManager.isMovementPhaseDef,
            "Defensive Movement continues",
            true,
            movementPhaseManager.isMovementPhaseDef
        );
        AssertTrue(
            movementPhaseManager.isAwaitingTokenSelection,
            "Defensive Movement expects a token to continue",
            true,
            movementPhaseManager.isAwaitingTokenSelection
        );
        AssertTrue(
            !movementPhaseManager.isAwaitingHexDestination,
            "Defensive Movement DOES NOT EXPECT a hex to continue",
            false,
            movementPhaseManager.isAwaitingHexDestination
        );
        Log("Clicking (4, 5) Select Stewart");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(4, 5), 1.5f));
        AssertTrue(
            movementPhaseManager.isMovementPhaseDef,
            "Defensive Movement continues after clicking on Stewart",
            true,
            movementPhaseManager.isMovementPhaseDef
        );
        AssertTrue(
            movementPhaseManager.isAwaitingTokenSelection,
            "Defensive Movement expects a token to continue after clicking on Stewart",
            true,
            movementPhaseManager.isAwaitingTokenSelection
        );
        AssertTrue(
            movementPhaseManager.isAwaitingHexDestination,
            "Defensive Movement expects a hex to continue after clicking on Stewart",
            false,
            movementPhaseManager.isAwaitingHexDestination
        );
        Log("Clicking (6, 7) Select Stewart");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(6, 7), 0.5f));
        yield return new WaitForSeconds(1.5f); 
        AssertTrue(
            movementPhaseManager.isWaitingForTackleDecision,
            "Defensive Movement expects a hex to continue after moving Stewart",
            true,
            movementPhaseManager.isAwaitingHexDestination
        );
        AssertTrue(
            !movementPhaseManager.isAwaitingHexDestination,
            "Defensive Movement expects a hex to continue after moving Stewart",
            false,
            movementPhaseManager.isAwaitingHexDestination
        );
        AssertTrue(
            !movementPhaseManager.isAwaitingTokenSelection,
            "Defensive Movement DOES NOT expect a token to continue after moving Stewart",
            true,
            movementPhaseManager.isAwaitingTokenSelection
        );
        yield return new WaitForSeconds(0.6f);
        Log("Pressing T - Tackle McNulty");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.T, 0.5f));
        yield return new WaitForSeconds(0.6f);
        movementPhaseManager.PerformTackleDiceRoll(isDefender: true, 4);
        yield return new WaitForSeconds(0.2f);
        movementPhaseManager.PerformTackleDiceRoll(isDefender: false, 4);
        yield return new WaitForSeconds(1.2f);
        AssertTrue(
            looseBallManager.isActivated,
            "Loose Ball Manager Should be activated after the loose ball caused by Stewart's tackle",
            true,
            looseBallManager.isActivated
        );
        AssertTrue(
            looseBallManager.isWaitingForDirectionRoll,
            "Loose Ball Manager Should be waiting for a Direction Roll after the loose ball caused by Stewart's tackle",
            true,
            looseBallManager.isWaitingForDirectionRoll
        );
        yield return new WaitForSeconds(0.5f);
        Log("Pressing R - Direction Roll North");
        looseBallManager.PerformDirectionRoll(4);
        yield return new WaitForSeconds(0.1f);
        AssertTrue(
            !looseBallManager.isWaitingForDirectionRoll,
            "Loose Ball Manager Should NOT be waiting for a Direction Roll after Direction Roll",
            false,
            looseBallManager.isWaitingForDirectionRoll
        );
        AssertTrue(
            looseBallManager.isWaitingForDistanceRoll,
            "Loose Ball Manager Should be waiting for a Distance Roll after Direction Roll",
            true,
            looseBallManager.isWaitingForDistanceRoll
        );
        yield return new WaitForSeconds(0.5f);
        Log("Pressing R - Distance Roll 6");
        looseBallManager.PerformDistanceRoll(6);
        yield return new WaitForSeconds(2.5f);
        AssertTrue(
            MatchManager.Instance.currentState == MatchManager.GameState.WaitingForThrowInTaker,
            "Ball went out for a throw in",
            MatchManager.GameState.WaitingForThrowInTaker,
            MatchManager.Instance.currentState
        );

        LogFooterofTest("MovementPhase Check Tackle Loose Ball Missed Interceptio - Hit Attacker, new tackle loose ball, throw in");
    }

    private IEnumerator Scenario_021_Movement_Phase_PickUp_continue_move_looseball_two_missed_interceptions()
    {
        yield return new WaitForSeconds(1.5f); // Allow scene to stabilize
        Log("▶️ Starting test scenario: MovementPhase Check Pick Up Ball, continue Dribble, tackle Loose, 2 missed interceptions");
        Log("Pressing 2");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Alpha2, 0.1f));
        Log("Pressing Space");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Space, 0.1f));
        Log("Pressing P - Game is in Movement Phase");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.P, 0.1f));
        Log("Clicking (6, 0)");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(6, 0), 0.5f));
        Log("Clicking (6, 0) again");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(6, 0), 0.5f));
        Log("Wait for the ball to move");
        yield return new WaitForSeconds(3f); // for the ball to move
        AvailabilityCheckResult availabilityCheck = AssertCorrectAvailabilityAfterGBToSpace();
        AssertTrue(
            availabilityCheck.passed,
            "Action Availability after GB to Space is correct",
            true,
            availabilityCheck.ToString()
        );
        Log("Clicking (10, 0) - Select Yaneva");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        Log("Pressing V - Pick Up the ball with Yaneva");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.V, 0.1f));
        yield return new WaitForSeconds(2f);
        AssertTrue(
            movementPhaseManager.isDribblerRunning,
            "MP should realize that the ball is picked up",
            true,
            movementPhaseManager.isDribblerRunning
        );
        AssertTrue(
            movementPhaseManager.remainingDribblerPace == 2,
            "MP should realize that Yaneva has 2 more pace available to run",
            2,
            movementPhaseManager.remainingDribblerPace
        );
        AssertTrue(
            movementPhaseManager.isAwaitingHexDestination,
            "MP should be waiting for a destination again",
            true,
            movementPhaseManager.isAwaitingHexDestination
        );
        AssertTrue(
            !movementPhaseManager.isAwaitingTokenSelection,
            "MP should NOT be waiting for TOKEN, either forfeit pace or move",
            false,
            movementPhaseManager.isAwaitingTokenSelection
        );
        Log("Pressing X - Forfeit remaining Pace of Yaneva");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.8f));
        yield return new WaitForSeconds(0.6f);
        AssertTrue(
            movementPhaseManager.isAwaitingTokenSelection,
            "MP should be waiting for TOKEN, after forfeiting remaining Pace",
            false,
            movementPhaseManager.isAwaitingTokenSelection
        );
        AssertTrue(
            movementPhaseManager.attackersMoved == 1,
            "MP - 1 attackers moved",
            1,
            movementPhaseManager.defendersMoved
        );
        Log("Pressing X - Forfeit Att Movement Phase");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.3f));
        yield return new WaitForSeconds(0.5f);
        AssertTrue(
            movementPhaseManager.attackersMoved == 4,
            "MP - 1 attackers moved",
            4,
            movementPhaseManager.defendersMoved
        );
        Log("Clicking (4, 3) - Select Paterson");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(4, 3), 0.5f));
        Log("Clicking (6, 1) - Move Paterson");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(6, 1), 0.5f));
        yield return new WaitForSeconds(2f);
        Log("Pressing T - Tackle on Taneva");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.T, 0.3f));
        yield return new WaitForSeconds(0.5f);
        movementPhaseManager.PerformTackleDiceRoll(isDefender: true, 6);
        yield return new WaitForSeconds(0.2f);
        movementPhaseManager.PerformTackleDiceRoll(isDefender: false, 4);
        yield return new WaitForSeconds(1.2f);
        AssertTrue(
            looseBallManager.isActivated,
            "Loose Ball Manager Should be activated after the loose ball caused by Paterson's tackle",
            true,
            looseBallManager.isActivated
        );
        AssertTrue(
            looseBallManager.isWaitingForDirectionRoll,
            "Loose Ball Manager Should be waiting for a Direction Roll after the loose ball caused by Paterson's tackle",
            true,
            looseBallManager.isWaitingForDirectionRoll
        );
        yield return new WaitForSeconds(0.5f);
        Log("Pressing R - Direction Roll North West");
        looseBallManager.PerformDirectionRoll(3);
        yield return new WaitForSeconds(0.1f);
        AssertTrue(
            !looseBallManager.isWaitingForDirectionRoll,
            "Loose Ball Manager Should NOT be waiting for a Direction Roll after Direction Roll",
            false,
            looseBallManager.isWaitingForDirectionRoll
        );
        AssertTrue(
            looseBallManager.isWaitingForDistanceRoll,
            "Loose Ball Manager Should be waiting for a Distance Roll after Direction Roll",
            true,
            looseBallManager.isWaitingForDistanceRoll
        );
        yield return new WaitForSeconds(0.5f);
        Log("Pressing R - Distance Roll 6");
        looseBallManager.PerformDistanceRoll(6);
        yield return new WaitForSeconds(0.5f);
        AssertTrue(
            !looseBallManager.isWaitingForDirectionRoll,
            "Loose Ball Manager Should NOT be waiting for a Direction Roll after Distance Roll",
            false,
            looseBallManager.isWaitingForDirectionRoll
        );
        AssertTrue(
            !looseBallManager.isWaitingForDistanceRoll,
            "Loose Ball Manager NOT Should be waiting for a Distance Roll after Direction Roll",
            false,
            looseBallManager.isWaitingForDistanceRoll
        );
        AssertTrue(
            looseBallManager.isWaitingForInterceptionRoll,
            "Loose Ball Manager Should be waiting for an Interception Roll after Direction Roll",
            true,
            looseBallManager.isWaitingForInterceptionRoll
        );
        AssertTrue(
            looseBallManager.potentialInterceptor == PlayerToken.GetPlayerTokenByName("Gilbert"),
            "Loose ball should be waiting for an interception from Gilbert",
            PlayerToken.GetPlayerTokenByName("Gilbert"),
            looseBallManager.potentialInterceptor
        );
        yield return new WaitForSeconds(0.5f);
        Log("Pressing R - Interception Roll Missed - Interceptor should be Vladoiu");
        looseBallManager.PerformInterceptionRoll(1);
        yield return new WaitForSeconds(0.5f); 
        AssertTrue(
            !looseBallManager.isWaitingForDirectionRoll,
            "Loose Ball Manager Should NOT be waiting for a Direction Roll after Distance Roll",
            false,
            looseBallManager.isWaitingForDirectionRoll
        );
        AssertTrue(
            !looseBallManager.isWaitingForDistanceRoll,
            "Loose Ball Manager NOT Should be waiting for a Distance Roll after Direction Roll",
            false,
            looseBallManager.isWaitingForDistanceRoll
        );
        AssertTrue(
            looseBallManager.isWaitingForInterceptionRoll,
            "Loose Ball Manager Should be waiting for an Interception Roll after Direction Roll",
            true,
            looseBallManager.isWaitingForInterceptionRoll
        );
        AssertTrue(
            looseBallManager.potentialInterceptor == PlayerToken.GetPlayerTokenByName("Vladoiu"),
            "Loose ball should be waiting for an interception from Vladoiu",
            PlayerToken.GetPlayerTokenByName("Vladoiu"),
            looseBallManager.potentialInterceptor
        );
        yield return new WaitForSeconds(0.5f);
        Log("Pressing R - Interception Roll Missed - Move the ball to space");
        looseBallManager.PerformInterceptionRoll(1);
        yield return new WaitForSeconds(2.5f);
        AssertTrue(
            !looseBallManager.isActivated,
            "Loose ball should be cleaned up",
            false,
            looseBallManager.isActivated
        );
        AssertTrue(
            movementPhaseManager.isMovementPhaseDef,
            "MP should continue from Defensive Movement",
            false,
            movementPhaseManager.isMovementPhaseDef
        );
        AssertTrue(
            movementPhaseManager.isAwaitingTokenSelection,
            "Defensive Movement expects a token to continue",
            true,
            movementPhaseManager.isAwaitingTokenSelection
        );
        AssertTrue(
            !movementPhaseManager.isAwaitingHexDestination,
            "Defensive Movement DOES NOT EXPECT a hex to continue",
            false,
            movementPhaseManager.isAwaitingHexDestination
        );
        AssertTrue(
            movementPhaseManager.defendersMoved == 1,
            "MP - 1 defenders moved",
            1,
            movementPhaseManager.defendersMoved
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Count == 2,
            "MovementPhase Should have 2 after Yaneva and Stewart's movement",
            2,
            movementPhaseManager.movedTokens.Count
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Contains(PlayerToken.GetPlayerTokenByName("Yaneva")),
            "MovementPhase Should moved tokens should contain Yaneva after resolving their movement",
            true,
            movementPhaseManager.movedTokens.Contains(PlayerToken.GetPlayerTokenByName("Yaneva"))
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Contains(PlayerToken.GetPlayerTokenByName("Paterson")),
            "MovementPhase Should moved tokens should contain Paterson after resolving their movement",
            true,
            movementPhaseManager.movedTokens.Contains(PlayerToken.GetPlayerTokenByName("Paterson"))
        );
        // AssertTrue(
        //     false,
        //     "Break"
        // );
        Log("Clicking (5, 5) - Select McNulty");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(5, 5), 0.5f));
        AssertTrue(
            movementPhaseManager.isBallPickable,
            "MovementPhase Should identify that the ball is pickable by McNulty",
            true,
            movementPhaseManager.isBallPickable
        );
        yield return new WaitForSeconds(0.5f);
        Log("Clicking (1, 10) - Select Marell");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(1, 10), 0.5f));
        yield return new WaitForSeconds(0.5f);
        AssertTrue(
            !movementPhaseManager.isBallPickable,
            "MovementPhase Should identify that the ball is pickable by Marell",
            false,
            movementPhaseManager.isBallPickable
        );
        yield return new WaitForSeconds(1.5f);
        Log("Clicking (5, 5) - Select McNulty");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(5, 5), 0.5f));
        AssertTrue(
            movementPhaseManager.isBallPickable,
            "MovementPhase Should identify that the ball is pickable by McNulty",
            true,
            movementPhaseManager.isBallPickable
        );
        yield return new WaitForSeconds(0.5f);
        Log("Pressing V - McNulty to Pick Up Ball");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.V, 0.3f));
        yield return new WaitForSeconds(2.5f);
        availabilityCheck = AssertCorrectAvailabilityAnyOtherScenario();
        AssertTrue(
            availabilityCheck.passed,
            "Action Availability after Interception (Any Other Scenario)",
            true,
            availabilityCheck.ToString()
        );
        AssertTrue(
            MatchManager.Instance.teamInAttack == MatchManager.TeamInAttack.Away
            , "home team is in attack after ball movement"
            , MatchManager.TeamInAttack.Away
            , MatchManager.Instance.teamInAttack
        );
        AssertTrue(
            MatchManager.Instance.attackHasPossession
            , "Attack has no possession after ball movement"
            , true
            , MatchManager.Instance.attackHasPossession
        );
        AssertTrue(
            MatchManager.Instance.LastTokenToTouchTheBallOnPurpose == PlayerToken.GetPlayerTokenByName("McNulty"),
            "McNulty should be the last to touch the ball",
            PlayerToken.GetPlayerTokenByName("McNulty"),
            MatchManager.Instance.LastTokenToTouchTheBallOnPurpose
        );
        AssertTrue(
            MatchManager.Instance.PreviousTokenToTouchTheBallOnPurpose == null,
            "Picking up a defender-caused loose ball from space should clear the previous token chain"
        );
        AssertTrue(
            !MatchManager.Instance.clearPreviousOnNextBallCollection,
            "The loose-ball pickup ownership reset should be consumed once the ball is collected",
            false,
            MatchManager.Instance.clearPreviousOnNextBallCollection
        );

        LogFooterofTest("MovementPhase Check Pick Up Ball, continue Dribble, tackle Loose, 2 missed interceptions");
    }

    private IEnumerator Scenario_022_Movement_Phase_Loose_ball_gets_in_pen_box_check_keeper_move()
    {
        yield return new WaitForSeconds(1.5f); // Allow scene to stabilize
        Log("▶️ Starting test scenario: MovementPhase Loose Ball, sent in the Penalty Box");
        Log("Pressing 2");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Alpha2, 0.1f));
        Log("Pressing Space");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Space, 0.1f));
        Log("Pressing P - Game is in Movement Phase");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.P, 0.1f));
        Log("Clicking (10, 0)");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        Log("Clicking (10, 0) again");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        Log("Wait for the ball to move");
        yield return new WaitForSeconds(3f); // for the ball to move
        AvailabilityCheckResult availabilityCheck = AssertCorrectAvailabilityAfterGBToPlayer();
        AssertTrue(
            availabilityCheck.passed,
            "Action Availability after GB to Player is correct",
            true,
            availabilityCheck.ToString()
        );
        Log("Pressing X - Forfeit Att F3");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        yield return new WaitForSeconds(0.2f);
        Log("Pressing X - Forfeit Def F3");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        yield return new WaitForSeconds(0.2f);
        Log("Pressing M - Start Movement Phase");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.M, 0.1f));
        yield return new WaitForSeconds(0.2f);
        Log("Pressing M - Forfeit Att MP");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        yield return new WaitForSeconds(0.2f);
        Log("Clicking (14, 0) - Select Soares");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(14, 0), 0.5f));
        Log("Clicking (11, 0) - Move Soares");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(11, 0), 0.5f));
        yield return new WaitForSeconds(3f); // for the ball to move
        Log("Pressing T - Tackle Yaneva with Soares");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.T, 0.1f));
        yield return new WaitForSeconds(0.5f);
        movementPhaseManager.PerformTackleDiceRoll(isDefender: true, 6);
        yield return new WaitForSeconds(0.2f);
        movementPhaseManager.PerformTackleDiceRoll(isDefender: false, 4);
        yield return new WaitForSeconds(1.2f);
        AssertTrue(
            looseBallManager.isActivated,
            "Loose Ball Manager Should be activated after the loose ball caused by Paterson's tackle",
            true,
            looseBallManager.isActivated
        );
        AssertTrue(
            looseBallManager.isWaitingForDirectionRoll,
            "Loose Ball Manager Should be waiting for a Direction Roll after the loose ball caused by Paterson's tackle",
            true,
            looseBallManager.isWaitingForDirectionRoll
        );
        yield return new WaitForSeconds(0.5f);
        Log("Pressing R - Direction Roll North East");
        looseBallManager.PerformDirectionRoll(5);
        yield return new WaitForSeconds(0.1f);
        AssertTrue(
            !looseBallManager.isWaitingForDirectionRoll,
            "Loose Ball Manager Should NOT be waiting for a Direction Roll after Direction Roll",
            false,
            looseBallManager.isWaitingForDirectionRoll
        );
        AssertTrue(
            looseBallManager.isWaitingForDistanceRoll,
            "Loose Ball Manager Should be waiting for a Distance Roll after Direction Roll",
            true,
            looseBallManager.isWaitingForDistanceRoll
        );
        yield return new WaitForSeconds(0.5f);
        Log("Pressing R - Distance Roll 6");
        looseBallManager.PerformDistanceRoll(6);
        yield return new WaitForSeconds(0.5f);
        AssertTrue(
            !looseBallManager.isWaitingForDirectionRoll,
            "Loose Ball Manager Should NOT be waiting for a Direction Roll after Distance Roll",
            false,
            looseBallManager.isWaitingForDirectionRoll
        );
        AssertTrue(
            !looseBallManager.isWaitingForDistanceRoll,
            "Loose Ball Manager NOT Should be waiting for a Distance Roll after Direction Roll",
            false,
            looseBallManager.isWaitingForDistanceRoll
        );
        AssertTrue(
            looseBallManager.isActivated,
            "Loose Ball Manager Should Still be active",
            true,
            looseBallManager.isActivated
        );
        yield return new WaitForSeconds(2.5f);
        AssertTrue(
            goalKeeperManager.isActivated,
            "GK Manager Should be active",
            true,
            goalKeeperManager.isActivated
        );
        Log("Clicking (16, -1) Move GK for Box entrance");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(16, -1), 0.5f));
        yield return new WaitForSeconds(1.2f); // for the token to move
        AssertTrue(
            !goalKeeperManager.isActivated,
            "GK Manager Should NOT be active any more",
            false,
            goalKeeperManager.isActivated
        );
        yield return new WaitForSeconds(2.2f); // for the token to move
        AssertTrue(
            !looseBallManager.isActivated,
            "Loose Ball Should NOT be active any more",
            false,
            looseBallManager.isActivated
        );
        AssertTrue(
            MatchManager.Instance.clearPreviousOnNextBallCollection,
            "A defender-caused loose ball left in space should keep the next-pickup ownership reset armed",
            true,
            MatchManager.Instance.clearPreviousOnNextBallCollection
        );
        AssertTrue(
            movementPhaseManager.isMovementPhaseDef,
            "MP Should still be at Defensive part",
            true,
            movementPhaseManager.isMovementPhaseDef
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Count == 2,
            "MP Moved Tokens should have 2",
            2,
            movementPhaseManager.movedTokens.Count
        );
        AssertTrue(
            movementPhaseManager.defendersMoved == 1,
            "MP Defenders Moved should have 1",
            true,
            movementPhaseManager.defendersMoved
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Contains(PlayerToken.GetPlayerTokenByName("Soares")),
            "MP Moved Tokens should Contain Soares",
            true,
            movementPhaseManager.defendersMoved
        );
        AssertTrue(
            MatchManager.Instance.currentState == MatchManager.GameState.MovementPhase,
            "MM should be at Movement Phase",
            true,
            MatchManager.Instance.currentState
        );

        LogFooterofTest("MovementPhase Loose Ball, sent in the Penalty Box");
    }
    
    private IEnumerator Scenario_023_Movement_Phase_DriblingBox_TackleLoose_ball_on_attacker_NO_Snapshot_end_MP()
    {
        yield return new WaitForSeconds(1.5f); // Allow scene to stabilize
        Log("▶️ Starting test scenario: MovementPhase DribbleBox Tackle, LB, ball on attacker, no snapshot");
        Log("Pressing 2");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Alpha2, 0.1f));
        Log("Pressing Space");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Space, 0.1f));
        Log("Pressing P - Game is in Movement Phase");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.P, 0.1f));
        Log("Clicking (10, 0)");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        Log("Clicking (10, 0) again");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        Log("Wait for the ball to move");
        yield return new WaitForSeconds(3f); // for the ball to move
        AvailabilityCheckResult availabilityCheck = AssertCorrectAvailabilityAfterGBToPlayer();
        AssertTrue(
            availabilityCheck.passed,
            "Action Availability after GB to Player is correct",
            true,
            availabilityCheck.ToString()
        );
        Log("Pressing X - Forfeit Att F3");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        yield return new WaitForSeconds(0.2f);
        Log("Pressing X - Forfeit Def F3");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        yield return new WaitForSeconds(0.2f);
        Log("Pressing M - Start Movement Phase");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.M, 0.1f));
        yield return new WaitForSeconds(0.2f);
        Log("Clicking (8, 8) - Select Toothnail");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(8, 8), 0.5f));
        Log("Clicking (13, 5) - Move Toothnail");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(13, 5), 0.5f));
        yield return new WaitForSeconds(2.8f);
        Log("Clicking (10, 0) - Select Yaneva");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        Log("Clicking (11, 0) - Move Yaneva 1st Pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(11, 0), 0.5f));
        yield return new WaitForSeconds(0.8f);
        Log("Clicking (12, 1) - Move Yaneva 2nd Pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(12, 1), 0.5f));
        yield return new WaitForSeconds(0.8f);
        AssertTrue(
            goalKeeperManager.isActivated,
            "GK Manager Should be active for Yaneva entering the box with the ball",
            false,
            goalKeeperManager.isActivated
        );
        Log("Clicking (16, 1) - Move GK for ");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(16, 1), 0.5f));
        yield return new WaitForSeconds(0.8f);
        AssertTrue(
            !goalKeeperManager.isActivated,
            "GK Manager Should NOT be active any more",
            false,
            goalKeeperManager.isActivated
        );
        Log("Clicking (13, 1) - Move Yaneva 3rd Pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(13, 1), 0.5f));
        yield return new WaitForSeconds(0.8f);
        Log("Pressing X - Forfeit Yaneva's remaining Pace");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.5f));
        yield return new WaitForSeconds(0.5f);
        // AssertTrue(
        //     false,
        //     "Break"
        // );
        Log("Pressing X - Forfeit Att MP");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.5f));
        yield return new WaitForSeconds(0.2f);
        AssertTrue(
            movementPhaseManager.attackersMoved == 4,
            "MP Defenders Moved should have 4 as the Att MP is forfeited",
            4,
            movementPhaseManager.attackersMoved
        );
        Log("Clicking (14, 0) - Select Soares");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(14, 0), 0.5f));
        Log("Clicking (13, 2) - Move Soares for Tackle");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(13, 2), 0.5f));
        yield return new WaitForSeconds(0.8f);
        Log("Pressing T - Tackle Yaneva with Soares");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.T, 0.1f));
        yield return new WaitForSeconds(0.5f);
        movementPhaseManager.PerformTackleDiceRoll(isDefender: true, 6);
        yield return new WaitForSeconds(0.2f);
        movementPhaseManager.PerformTackleDiceRoll(isDefender: false, 4);
        yield return new WaitForSeconds(1.2f);
        AssertTrue(
            looseBallManager.isActivated,
            "Loose Ball Manager Should be activated after the loose ball caused by Soares's tackle",
            true,
            looseBallManager.isActivated
        );
        AssertTrue(
            looseBallManager.isWaitingForDirectionRoll,
            "Loose Ball Manager Should be waiting for a Direction Roll after the loose ball caused by Soares's tackle",
            true,
            looseBallManager.isWaitingForDirectionRoll
        );
        yield return new WaitForSeconds(0.5f);
        Log("Pressing R - Direction Roll North");
        looseBallManager.PerformDirectionRoll(4);
        yield return new WaitForSeconds(0.1f);
        AssertTrue(
            !looseBallManager.isWaitingForDirectionRoll,
            "Loose Ball Manager Should NOT be waiting for a Direction Roll after Direction Roll",
            false,
            looseBallManager.isWaitingForDirectionRoll
        );
        AssertTrue(
            looseBallManager.isWaitingForDistanceRoll,
            "Loose Ball Manager Should be waiting for a Distance Roll after Direction Roll",
            true,
            looseBallManager.isWaitingForDistanceRoll
        );
        yield return new WaitForSeconds(0.5f);
        Log("Pressing R - Distance Roll 6");
        looseBallManager.PerformDistanceRoll(6);
        yield return new WaitForSeconds(3.5f);
        AssertTrue(
            !looseBallManager.isActivated,
            "Loose Ball Manager Should not be activated any more",
            false,
            looseBallManager.isActivated
        );
        AssertTrue(
            MatchManager.Instance.PreviousTokenToTouchTheBallOnPurpose == null,
            "A defender-caused loose ball that hits an attacker should clear the previous token in defensive MP"
        );
        AssertTrue(
            movementPhaseManager.isMovementPhaseDef,
            "MP Should still be at Defensive part",
            true,
            movementPhaseManager.isMovementPhaseDef
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Count == 3,
            "MP Moved Tokens should have 3",
            true,
            movementPhaseManager.movedTokens.Count
        );
        AssertTrue(
            movementPhaseManager.attackersMoved == 4,
            "MP Defenders Moved should have 4 as the Att MP is forfeited",
            4,
            movementPhaseManager.attackersMoved
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Contains(PlayerToken.GetPlayerTokenByName("Toothnail")),
            "MP Moved Tokens should Contain Toothnail"
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Contains(PlayerToken.GetPlayerTokenByName("Yaneva")),
            "MP Moved Tokens should Contain Yaneva"
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Contains(PlayerToken.GetPlayerTokenByName("Soares")),
            "MP Moved Tokens should Contain Soares"
        );
        AssertTrue(
            !movementPhaseManager.isWaitingForSnapshotDecision,
            "MP Should be waiting for a Snapshot decision, as we are in Def MP",
            false,
            movementPhaseManager.isWaitingForSnapshotDecision
        );
        AssertTrue(
            movementPhaseManager.defendersMoved == 1,
            "MP Defenders Moved should have 1",
            1,
            movementPhaseManager.defendersMoved
        );
        Log("Clicking (1, 2) - Select Vladoiu");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(1, 2), 0.5f));
        Log("Clicking (-1, 1) - Move Vladoiu");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(-1, 1), 0.5f));
        yield return new WaitForSeconds(1.2f);
        AssertTrue(
            movementPhaseManager.isMovementPhaseDef,
            "MP Should still be at Defensive part",
            true,
            movementPhaseManager.isMovementPhaseDef
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Count == 4,
            "MP Moved Tokens should have 4",
            true,
            movementPhaseManager.movedTokens.Count
        );
        AssertTrue(
            movementPhaseManager.attackersMoved == 4,
            "MP Defenders Moved should have 4 as the Att MP is forfeited",
            4,
            movementPhaseManager.attackersMoved
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Contains(PlayerToken.GetPlayerTokenByName("Toothnail")),
            "MP Moved Tokens should Contain Toothnail"
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Contains(PlayerToken.GetPlayerTokenByName("Yaneva")),
            "MP Moved Tokens should Contain Yaneva"
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Contains(PlayerToken.GetPlayerTokenByName("Soares")),
            "MP Moved Tokens should Contain Soares"
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Contains(PlayerToken.GetPlayerTokenByName("Vladoiu")),
            "MP Moved Tokens should Contain Vladoiu"
        );
        AssertTrue(
            movementPhaseManager.defendersMoved == 2,
            "MP Defenders Moved should have 2",
            2,
            movementPhaseManager.defendersMoved
        );
        Log("Pressing X - Forfeit Def MP");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.5f));
        Log("Pressing X - Forfeit 2f2 MP");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.5f));
        AvailabilityCheckResult mpcomplete = AssertCorrectAvailabilityAfterMovementComplete();
        AssertTrue(
            mpcomplete.passed,
            "MovementPhase Complete Check Status Availability",
            true,
            mpcomplete.ToString()
        );

        LogFooterofTest("MovementPhase DribbleBox Tackle, LB, ball on attacker, no snapshot");
    }
    
    private IEnumerator Scenario_024_Movement_Phase_DriblingBox_Nutmeg_Loose_ball_on_attacker_Snapshot_goal()
    {
        yield return new WaitForSeconds(1.5f); // Allow scene to stabilize
        Log("▶️ Starting test scenario: MovementPhase DribbleBox Tackle, LB, ball on attacker, snapshot GOAL!");
        Log("Pressing 2");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Alpha2, 0.1f));
        Log("Pressing Space");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Space, 0.1f));
        Log("Pressing P - Game is in Movement Phase");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.P, 0.1f));
        Log("Clicking (10, 0)");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        Log("Clicking (10, 0) again");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        Log("Wait for the ball to move");
        yield return new WaitForSeconds(3f); // for the ball to move
        AvailabilityCheckResult availabilityCheck = AssertCorrectAvailabilityAfterGBToPlayer();
        AssertTrue(
            availabilityCheck.passed,
            "Action Availability after GB to Player is correct",
            true,
            availabilityCheck.ToString()
        );
        Log("Pressing X - Forfeit Att F3");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        yield return new WaitForSeconds(0.2f);
        Log("Pressing X - Forfeit Def F3");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        yield return new WaitForSeconds(0.2f);
        Log("Pressing M - Start Movement Phase");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.M, 0.1f));
        yield return new WaitForSeconds(0.2f);
        Log("Clicking (8, 8) - Select Toothnail");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(8, 8), 0.5f));
        Log("Clicking (13, 5) - Move Toothnail");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(13, 5), 0.5f));
        yield return new WaitForSeconds(2.8f);
        Log("Pressing X - Forfeit Att MP");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.5f));
        yield return new WaitForSeconds(0.2f);
        AssertTrue(
            movementPhaseManager.attackersMoved == 4,
            "MP Defenders Moved should have 4 as the Att MP is forfeited",
            4,
            movementPhaseManager.attackersMoved
        );
        AssertTrue(
            movementPhaseManager.isMovementPhaseDef,
            "MP must have passed in Def MP",
            true,
            movementPhaseManager.isMovementPhaseDef
        );
        Log("Clicking (14, 0) - Select Soares");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(14, 0), 0.5f));
        Log("Clicking (13, 1) - Move Soares");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(13, 1), 0.5f));
        yield return new WaitForSeconds(0.8f);
        Log("Pressing X - Forfeit Def MP");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.5f));
        yield return new WaitForSeconds(0.2f);
        AssertTrue(
            movementPhaseManager.attackersMoved == 4,
            "MP Defenders Moved should have 4 as the Att MP is forfeited",
            4,
            movementPhaseManager.attackersMoved
        );
        AssertTrue(
            movementPhaseManager.defendersMoved == 5 ,
            "MP Defenders Moved should have 5 as the Att MP is forfeited",
            5,
            movementPhaseManager.defendersMoved
        );
        AssertTrue(
            movementPhaseManager.isMovementPhase2f2,
            "MP must have passed in MovementPhase2f2 MP",
            true,
            movementPhaseManager.isMovementPhase2f2
        );
        Log("Pressing X - Forfeit 2f2 MP");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.5f));
        yield return new WaitForSeconds(0.2f);
        Log("Pressing X - Forfeit Att F3");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        yield return new WaitForSeconds(0.2f);
        Log("Pressing X - Forfeit Def F3");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        yield return new WaitForSeconds(0.2f);
        Log("Pressing M - Start Movement Phase");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.M, 0.1f));
        yield return new WaitForSeconds(0.2f);
        Log("Clicking (10, 0) - Select Yaneva");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        Log("Clicking (11, 0) - Move Yaneva 1st Pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(11, 0), 0.5f));
        yield return new WaitForSeconds(0.8f);
        Log("Clicking (12, 1) - Move Yaneva 2nd Pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(12, 1), 0.5f));
        yield return new WaitForSeconds(0.8f);
        AssertTrue(
            goalKeeperManager.isActivated,
            "GK Manager Should be active for Yaneva entering the box with the ball",
            false,
            goalKeeperManager.isActivated
        );
        Log("Clicking (16, -1) - Move GK for Box");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(16, -1), 0.5f));
        yield return new WaitForSeconds(0.8f);
        AssertTrue(
            !goalKeeperManager.isActivated,
            "GK Manager Should NOT be active any more",
            false,
            goalKeeperManager.isActivated
        );
        Log("Clicking (13, 1) - Move Yaneva 3rd Pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(13, 1), 0.5f));
        yield return new WaitForSeconds(0.8f);
        Log("Clicking (12, 1) - Move Yaneva 4th Pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(12, 1), 0.5f));
        yield return new WaitForSeconds(0.8f);
        Log("Pressing N - Nutmeg Soares with Yaneva");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.N, 0.1f));
        yield return new WaitForSeconds(0.5f);
        movementPhaseManager.PerformTackleDiceRoll(isDefender: true, 5);
        yield return new WaitForSeconds(0.2f);
        movementPhaseManager.PerformTackleDiceRoll(isDefender: false, 4);
        yield return new WaitForSeconds(1.2f);
        AssertTrue(
            !movementPhaseManager.isNutmegInProgress,
            "Movement Phase should NO longer be in Nutmeg phase",
            false,
            movementPhaseManager.isNutmegInProgress
        );
        AssertTrue(
            looseBallManager.isActivated,
            "Loose Ball Manager Should be activated after the loose ball caused by Soares's tackle",
            true,
            looseBallManager.isActivated
        );
        AssertTrue(
            looseBallManager.isWaitingForDirectionRoll,
            "Loose Ball Manager Should be waiting for a Direction Roll after the loose ball caused by Soares's tackle",
            true,
            looseBallManager.isWaitingForDirectionRoll
        );
        yield return new WaitForSeconds(0.5f);
        Log("Pressing R - Direction Roll North");
        looseBallManager.PerformDirectionRoll(4);
        yield return new WaitForSeconds(0.1f);
        AssertTrue(
            !looseBallManager.isWaitingForDirectionRoll,
            "Loose Ball Manager Should NOT be waiting for a Direction Roll after Direction Roll",
            false,
            looseBallManager.isWaitingForDirectionRoll
        );
        AssertTrue(
            looseBallManager.isWaitingForDistanceRoll,
            "Loose Ball Manager Should be waiting for a Distance Roll after Direction Roll",
            true,
            looseBallManager.isWaitingForDistanceRoll
        );
        yield return new WaitForSeconds(0.5f);
        Log("Pressing R - Distance Roll 6");
        looseBallManager.PerformDistanceRoll(6);
        yield return new WaitForSeconds(3.5f);
        AssertTrue(
            !looseBallManager.isActivated,
            "Loose Ball Manager Should not be activated any more",
            false,
            looseBallManager.isActivated
        );
        AssertTrue(
            MatchManager.Instance.PreviousTokenToTouchTheBallOnPurpose == null,
            "A defender-caused loose ball that hits an attacker should clear the previous token before snapshot handling"
        );
        AssertTrue(
            MatchManager.Instance.LastTokenToTouchTheBallOnPurpose == PlayerToken.GetPlayerTokenByName("Toothnail"),
            "A defender-caused loose ball that hits Toothnail should make Toothnail the last token to touch the ball on purpose",
            PlayerToken.GetPlayerTokenByName("Toothnail"),
            MatchManager.Instance.LastTokenToTouchTheBallOnPurpose
        );
        AssertTrue(
            !MatchManager.Instance.clearPreviousOnNextBallCollection,
            "An immediate loose-ball hit on Toothnail should consume any pending next-pickup ownership reset",
            false,
            MatchManager.Instance.clearPreviousOnNextBallCollection
        );
        AssertTrue(
            movementPhaseManager.isMovementPhaseAttack,
            "MP Should still be at Attacking part",
            true,
            movementPhaseManager.isMovementPhaseAttack
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Count == 1,
            "MP Moved Tokens should have 1",
            1,
            movementPhaseManager.movedTokens.Count
        );
        AssertTrue(
            movementPhaseManager.attackersMoved == 0,
            "MP Attackers Moved shouldbe 0 sas ithis is not yet resolved",
            4,
            movementPhaseManager.attackersMoved
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Contains(PlayerToken.GetPlayerTokenByName("Yaneva")),
            "MP Moved Tokens should Contain Yaneva"
        );
        AssertTrue(
            !movementPhaseManager.isWaitingForSnapshotDecision,
            // TODO: During the attacking team’s player movements in a Movement Phase if a player has 
            // or takes the ball in the opposition’s penalty area OR immediately when a Loose Ball hits
            // an attacking player in the opposition’s penalty area or outside the box within shooting distance.
            // Immediately following a pass, whether inside or outside the penalty area.
            "MP Should Not be waiting for a Snapshot decision, as this is the Shot Manager's responsiility now",
            true,
            movementPhaseManager.isWaitingForSnapshotDecision
        );
        AssertTrue(
            shotManager.isWaitingForSnapshotDecisionFromLoose,
            "Shot Should be waiting for a Snapshot decision",
            true,
            shotManager.isWaitingForSnapshotDecisionFromLoose
        );
        Log("Pressing S - Snapshot with Toothnail!");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.S, 0.5f));
        AssertTrue(
            shotManager.isActivated
            , "Shot Manager is activated"
        );
        AssertTrue(
            shotManager.isWaitingforBlockerSelection
            , "Shot Manager waiting for blocker Selection"
        );
        Log("Clicking (13, 1) - Select Soares");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(13, 1), 0.5f));
        AssertTrue(
            shotManager.isWaitingforBlockerMovement
            , "Shot Manager waiting for blocker Target"
        );
        Log("Clicking (11, 1) - Move Soares for Snapshot Block");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(11, 1), 0.5f));
        yield return new WaitForSeconds(1.5f);
        AssertTrue(
            shotManager.isActivated
            , "Shot Manager is activated"
        );
        AssertTrue(
            !shotManager.isWaitingforBlockerSelection
            , "Shot Manager NOT waiting for blocker Selection"
        );
        AssertTrue(
            !shotManager.isWaitingforBlockerMovement
            , "Shot Manager NOT waiting for blocker target"
        );
        AssertTrue(
            shotManager.isWaitingForTargetSelection
            , "Shot Manager waiting for target selection"
        );
        Log("Clicking (19, 2) - Select target of Snapshot");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(19, 2), 0.5f));
        yield return new WaitForSeconds(2.5f);
        AssertTrue(
            !shotManager.isWaitingForTargetSelection
            , "Shot Manager NOT waiting for target selection"
        );
        AssertTrue(
            shotManager.isWaitingForShotRoll
            , "Shot Manager waiting for Shot Roll"
        );
        Log("Pressing R - Roll shot with Toothnail!");
        StartCoroutine(shotManager.StartShotRoll(6));
        yield return new WaitForSeconds(0.5f);
        AssertTrue(
            !shotManager.isActivated
            , "Shot Manager is no longer activated"
        );
        AssertTrue(
            !movementPhaseManager.isActivated
            , "MP Manager is no longer activated"
        );
        AssertTrue(
            goalFlowManager.isActivated
            , "GoalFlow is activated"
        );
        while (goalFlowManager.isActivated) yield return null;
        AssertTrue(
            !goalFlowManager.isActivated
            , "GoalFlow is no longer activated"
        );
        var toothnail = PlayerToken.GetPlayerTokenByName("Toothnail");
        var yaneva = PlayerToken.GetPlayerTokenByName("Yaneva");
        var homeTeamStatsAfterLooseGoal = MatchManager.Instance.gameData.stats.GetTeamStats(yaneva.isHomeTeam);
        AssertTrue(
            MatchManager.Instance.PreviousTokenToTouchTheBallOnPurpose == null,
            "A Toothnail goal coming from a defender-caused loose ball should not restore a previous-token assist chain"
        );
        AssertTrue(
            MatchManager.Instance.gameData.stats.GetPlayerStats(yaneva.playerName).assists == 0,
            "Yaneva should not be credited with an assist when Toothnail scores from a defender-caused loose ball",
            0,
            MatchManager.Instance.gameData.stats.GetPlayerStats(yaneva.playerName).assists
        );
        AssertTrue(
            homeTeamStatsAfterLooseGoal.totalAssists == 0,
            "The home team should not record an assist when Toothnail scores from a defender-caused loose ball",
            0,
            homeTeamStatsAfterLooseGoal.totalAssists
        );
        AssertTrue(
            MatchManager.Instance.gameData.stats.GetPlayerStats(toothnail.playerName).goals == 1,
            "Toothnail should still be credited with the goal",
            1,
            MatchManager.Instance.gameData.stats.GetPlayerStats(toothnail.playerName).goals
        );
        // AssertTrue(
        //     false,
        //     "Break"
        // );

        LogFooterofTest("MovementPhase DribbleBox Tackle, LB, ball on attacker, snapshot GOAL!");
    }

    private IEnumerator Scenario_024b_Movement_Phase_DriblingBox_Nutmeg_Loose_ball_on_attacker_No_Snapshot_end_MP_SHOT_GOAL()
    {
        yield return new WaitForSeconds(1.5f); // Allow scene to stabilize
        Log("▶️ Starting test scenario: MovementPhase DribbleBox Nutmeg, LB, ball on attacker, NO snapshot, end MP, SHOT GOAL!");
        Log("Pressing 2");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Alpha2, 0.1f));
        Log("Pressing Space");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Space, 0.1f));
        Log("Pressing P - Game is in Movement Phase");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.P, 0.1f));
        Log("Clicking (10, 0)");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        Log("Clicking (10, 0) again");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        Log("Wait for the ball to move");
        yield return new WaitForSeconds(3f); // for the ball to move
        AvailabilityCheckResult availabilityCheck = AssertCorrectAvailabilityAfterGBToPlayer();
        AssertTrue(
            availabilityCheck.passed,
            "Action Availability after GB to Player is correct",
            true,
            availabilityCheck.ToString()
        );
        Log("Pressing X - Forfeit Att F3");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        yield return new WaitForSeconds(0.2f);
        Log("Pressing X - Forfeit Def F3");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        yield return new WaitForSeconds(0.2f);
        Log("Pressing M - Start Movement Phase");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.M, 0.1f));
        yield return new WaitForSeconds(0.2f);
        Log("Clicking (8, 8) - Select Toothnail");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(8, 8), 0.5f));
        Log("Clicking (13, 5) - Move Toothnail");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(13, 5), 0.5f));
        yield return new WaitForSeconds(2.8f);
        Log("Pressing X - Forfeit Att MP");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.5f));
        yield return new WaitForSeconds(0.2f);
        AssertTrue(
            movementPhaseManager.attackersMoved == 4,
            "MP Defenders Moved should have 4 as the Att MP is forfeited",
            4,
            movementPhaseManager.attackersMoved
        );
        AssertTrue(
            movementPhaseManager.isMovementPhaseDef,
            "MP must have passed in Def MP",
            true,
            movementPhaseManager.isMovementPhaseDef
        );
        Log("Clicking (14, 0) - Select Soares");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(14, 0), 0.5f));
        Log("Clicking (13, 1) - Move Soares");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(13, 1), 0.5f));
        yield return new WaitForSeconds(0.8f);
        Log("Pressing X - Forfeit Def MP");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.5f));
        yield return new WaitForSeconds(0.2f);
        AssertTrue(
            movementPhaseManager.attackersMoved == 4,
            "MP Defenders Moved should have 4 as the Att MP is forfeited",
            4,
            movementPhaseManager.attackersMoved
        );
        AssertTrue(
            movementPhaseManager.defendersMoved == 5 ,
            "MP Defenders Moved should have 5 as the Att MP is forfeited",
            5,
            movementPhaseManager.defendersMoved
        );
        AssertTrue(
            movementPhaseManager.isMovementPhase2f2,
            "MP must have passed in MovementPhase2f2 MP",
            true,
            movementPhaseManager.isMovementPhase2f2
        );
        Log("Pressing X - Forfeit 2f2 MP");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.5f));
        yield return new WaitForSeconds(0.2f);
        Log("Pressing X - Forfeit Att F3");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        yield return new WaitForSeconds(0.2f);
        Log("Pressing X - Forfeit Def F3");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        yield return new WaitForSeconds(0.2f);
        Log("Pressing M - Start Movement Phase");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.M, 0.1f));
        yield return new WaitForSeconds(0.2f);
        Log("Clicking (10, 0) - Select Yaneva");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        Log("Clicking (11, 0) - Move Yaneva 1st Pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(11, 0), 0.5f));
        yield return new WaitForSeconds(0.8f);
        Log("Clicking (12, 1) - Move Yaneva 2nd Pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(12, 1), 0.5f));
        yield return new WaitForSeconds(0.8f);
        AssertTrue(
            goalKeeperManager.isActivated,
            "GK Manager Should be active for Yaneva entering the box with the ball",
            false,
            goalKeeperManager.isActivated
        );
        Log("Clicking (16, -1) - Move GK for Box");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(16, -1), 0.5f));
        yield return new WaitForSeconds(0.8f);
        AssertTrue(
            !goalKeeperManager.isActivated,
            "GK Manager Should NOT be active any more",
            false,
            goalKeeperManager.isActivated
        );
        Log("Clicking (13, 1) - Move Yaneva 3rd Pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(13, 1), 0.5f));
        yield return new WaitForSeconds(0.8f);
        Log("Clicking (12, 1) - Move Yaneva 4th Pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(12, 1), 0.5f));
        yield return new WaitForSeconds(0.8f);
        Log("Pressing N - Nutmeg Soares with Yaneva");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.N, 0.1f));
        yield return new WaitForSeconds(0.5f);
        movementPhaseManager.PerformTackleDiceRoll(isDefender: true, 5);
        yield return new WaitForSeconds(0.2f);
        movementPhaseManager.PerformTackleDiceRoll(isDefender: false, 4);
        yield return new WaitForSeconds(1.2f);
        AssertTrue(
            !movementPhaseManager.isNutmegInProgress,
            "Movement Phase should NO longer be in Nutmeg phase",
            false,
            movementPhaseManager.isNutmegInProgress
        );
        AssertTrue(
            looseBallManager.isActivated,
            "Loose Ball Manager Should be activated after the loose ball caused by Soares's tackle",
            true,
            looseBallManager.isActivated
        );
        AssertTrue(
            looseBallManager.isWaitingForDirectionRoll,
            "Loose Ball Manager Should be waiting for a Direction Roll after the loose ball caused by Soares's tackle",
            true,
            looseBallManager.isWaitingForDirectionRoll
        );
        yield return new WaitForSeconds(0.5f);
        Log("Pressing R - Direction Roll North");
        looseBallManager.PerformDirectionRoll(4);
        yield return new WaitForSeconds(0.1f);
        AssertTrue(
            !looseBallManager.isWaitingForDirectionRoll,
            "Loose Ball Manager Should NOT be waiting for a Direction Roll after Direction Roll",
            false,
            looseBallManager.isWaitingForDirectionRoll
        );
        AssertTrue(
            looseBallManager.isWaitingForDistanceRoll,
            "Loose Ball Manager Should be waiting for a Distance Roll after Direction Roll",
            true,
            looseBallManager.isWaitingForDistanceRoll
        );
        yield return new WaitForSeconds(0.5f);
        Log("Pressing R - Distance Roll 6");
        looseBallManager.PerformDistanceRoll(6);
        yield return new WaitForSeconds(3.5f);
        AssertTrue(
            !looseBallManager.isActivated,
            "Loose Ball Manager Should not be activated any more",
            false,
            looseBallManager.isActivated
        );
        AssertTrue(
            MatchManager.Instance.PreviousTokenToTouchTheBallOnPurpose == null,
            "A defender-caused loose ball that hits an attacker should clear the previous token before continuing the attack"
        );
        AssertTrue(
            MatchManager.Instance.LastTokenToTouchTheBallOnPurpose == PlayerToken.GetPlayerTokenByName("Toothnail"),
            "A defender-caused loose ball that hits Toothnail should make Toothnail the last token to touch the ball on purpose",
            PlayerToken.GetPlayerTokenByName("Toothnail"),
            MatchManager.Instance.LastTokenToTouchTheBallOnPurpose
        );
        AssertTrue(
            !MatchManager.Instance.clearPreviousOnNextBallCollection,
            "An immediate loose-ball hit on Toothnail should consume any pending next-pickup ownership reset",
            false,
            MatchManager.Instance.clearPreviousOnNextBallCollection
        );
        AssertTrue(
            movementPhaseManager.isMovementPhaseAttack,
            "MP Should still be at Attacking part",
            true,
            movementPhaseManager.isMovementPhaseAttack
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Count == 1,
            "MP Moved Tokens should have 1",
            1,
            movementPhaseManager.movedTokens.Count
        );
        AssertTrue(
            movementPhaseManager.attackersMoved == 0,
            "MP Attackers Moved shouldbe 0 sas ithis is not yet resolved",
            4,
            movementPhaseManager.attackersMoved
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Contains(PlayerToken.GetPlayerTokenByName("Yaneva")),
            "MP Moved Tokens should Contain Yaneva"
        );
        AssertTrue(
            !movementPhaseManager.isWaitingForSnapshotDecision,
            // TODO: During the attacking team’s player movements in a Movement Phase if a player has 
            // or takes the ball in the opposition’s penalty area OR immediately when a Loose Ball hits
            // an attacking player in the opposition’s penalty area or outside the box within shooting distance.
            // Immediately following a pass, whether inside or outside the penalty area.
            "MP Should Not be waiting for a Snapshot decision, as this is the Shot Manager's responsiility now",
            true,
            movementPhaseManager.isWaitingForSnapshotDecision
        );
        AssertTrue(
            shotManager.isWaitingForSnapshotDecisionFromLoose,
            "Shot Should be waiting for a Snapshot decision",
            true,
            shotManager.isWaitingForSnapshotDecisionFromLoose
        );
        Log("Pressing X - NO Snapshot with Toothnail!");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.5f));
        yield return new WaitForSeconds(0.8f);
        AssertTrue(
            !shotManager.isActivated
            , "Shot Manager is no longer activated"
        );
        AssertTrue(
            movementPhaseManager.attackersMoved == 1,
            "MP Defenders Moved should have 1 as the Att MP is continuing",
            1,
            movementPhaseManager.attackersMoved
        );
        AssertTrue(
            MatchManager.Instance.LastTokenToTouchTheBallOnPurpose == PlayerToken.GetPlayerTokenByName("Toothnail"),
            "LastTokenToTouchTheBallOnPurpose should be Toothnail",
            PlayerToken.GetPlayerTokenByName("Yaneva").playerName,
            MatchManager.Instance.LastTokenToTouchTheBallOnPurpose.playerName
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Contains(PlayerToken.GetPlayerTokenByName("Yaneva")),
            "Yaneva be in te moved tokens",
            true,
            movementPhaseManager.movedTokens.Contains(PlayerToken.GetPlayerTokenByName("Yaneva"))
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Count == 1,
            "moved tokens contains only one token Taneva",
            1,
            movementPhaseManager.movedTokens.Count
        );
        AssertTrue(
            movementPhaseManager.isAwaitingTokenSelection,
            "MP shoulbd be waiting for an atacking token to move",
            true,
            movementPhaseManager.isAwaitingTokenSelection
        );
        AssertTrue(
            movementPhaseManager.isMovementPhaseAttack,
            "MP shoulbd be waiting for an atacking phase",
            true,
            movementPhaseManager.isMovementPhaseAttack
        );
        Log("Clicking (4, 4) - Select Nazef");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(4, 4), 0.5f));
        Log("Clicking (0, 6) - Move Nazef");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(0, 6), 0.5f));
        yield return new WaitForSeconds(1.2f);
        Log("Pressing X - Forfeit Att MP");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.5f));
        yield return new WaitForSeconds(0.8f);
        AssertTrue(
            movementPhaseManager.attackersMoved == 4,
            "MP Attackers Moved should have 4 as the Att MP is fofeit",
            4,
            movementPhaseManager.attackersMoved
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Count == 2,
            "moved tokens contains only one token Nazef & Yaneva",
            2,
            movementPhaseManager.movedTokens.Count
        );
        AssertTrue(
            movementPhaseManager.isMovementPhaseDef,
            "MP shoulbd be waiting for an defending phase",
            true,
            movementPhaseManager.isMovementPhaseDef
        );
        AssertTrue(
            movementPhaseManager.isAwaitingTokenSelection,
            "MP shoulbd be waiting for an defending token to move",
            true,
            movementPhaseManager.isAwaitingTokenSelection
        );
        Log("Clicking (13, 1) - Select Nazef");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(13, 1), 0.5f));
        AssertTrue(
            movementPhaseManager.isAwaitingTokenSelection,
            "MP shoulbd be waiting for an defending token to move",
            true,
            movementPhaseManager.isAwaitingTokenSelection
        );
        Log("Clicking (13, -2) - Move Nazef");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(13, -2), 0.5f));
        yield return new WaitForSeconds(1.2f);
        Log("Pressing X - Forfeit Def MP");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.5f));
        yield return new WaitForSeconds(0.8f);
        Log("Pressing X - Forfeit 2f2 MP");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.5f));
        yield return new WaitForSeconds(0.8f);
        Log("Pressing X - Forfeit Att F3");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.5f));
        yield return new WaitForSeconds(0.8f);
        Log("Pressing X - Forfeit Def F3");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.5f));
        yield return new WaitForSeconds(0.8f);
        Log("Pressing S - Declare Shot with Toothnail");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.S, 0.5f));
        yield return new WaitForSeconds(0.8f);
        

        LogFooterofTest("MovementPhase DribbleBox Nutmeg, LB, ball on attacker, NO snapshot, end MP, SHOT GOAL!");
    }

    private IEnumerator Scenario_025a_Movement_Phase_Dribling_into_goal()
    {
        yield return new WaitForSeconds(1.5f); // Allow scene to stabilize
        Log("▶️ Starting test scenario: MovementPhase Dribble IN GOAL!");
        Log("Pressing 2");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Alpha2, 0.1f));
        AssertTrue(
            MatchManager.Instance.currentState == MatchManager.GameState.KickOffSetup,
            "Game is in KickOff Setup",
            MatchManager.GameState.KickOffSetup,
            MatchManager.Instance.currentState
        );
        Log("Pressing Space");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Space, 0.1f));
        AssertTrue(
            MatchManager.Instance.currentState == MatchManager.GameState.KickoffBlown,
            "Game is in KickoffBlown",
            MatchManager.GameState.KickoffBlown,
            MatchManager.Instance.currentState
        );
        Log("Pressing P - Pass to Yaneva");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.P, 0.1f));
        AssertTrue(
            groundBallManager.isActivated,
            "GBM should be activated",
            false,
            groundBallManager.isActivated
        );
        AssertTrue(
            groundBallManager.isAwaitingTargetSelection,
            "GBM should be waiting for a target",
            false,
            groundBallManager.isAwaitingTargetSelection
        );
        AssertTrue(
            groundBallManager.currentTargetHex == null,
            "GBM should be waiting for a target, but there is no target yet",
            false,
            groundBallManager.isAwaitingTargetSelection
        );
        Log("Clicking (10, 0)");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        AssertTrue(
            groundBallManager.isActivated,
            "GBM should be activated",
            false,
            groundBallManager.isActivated
        );
        AssertTrue(
            groundBallManager.isAwaitingTargetSelection,
            "GBM should be waiting for a target",
            false,
            groundBallManager.isAwaitingTargetSelection
        );
        AssertTrue(
            groundBallManager.currentTargetHex != null,
            "GBM should be waiting for a target, but there a target was clicked"
        );
        Log("Clicking (10, 0) again");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        Log("Wait for the ball to move");
        yield return new WaitForSeconds(3f); // for the ball to move
        AvailabilityCheckResult availabilityCheck = AssertCorrectAvailabilityAfterGBToPlayer();
        AssertTrue(
            availabilityCheck.passed,
            "Action Availability after GB to Player is correct",
            true,
            availabilityCheck.ToString()
        );
        AssertTrue(
            finalThirdManager.isActivated,
            "F3 should be activated",
            true,
            finalThirdManager.isActivated
        );
        Log("Pressing X - Forfeit Att F3");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        yield return new WaitForSeconds(0.2f);
        AssertTrue(
            finalThirdManager.isActivated,
            "F3 should be activated",
            true,
            finalThirdManager.isActivated
        );
        Log("Pressing X - Forfeit Def F3");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        yield return new WaitForSeconds(0.2f);
        AssertTrue(
            !finalThirdManager.isActivated,
            "F3 should no longer be activated",
            false,
            finalThirdManager.isActivated
        );
        AssertTrue(
            availabilityCheck.passed,
            "Action Availability after GB to Player is correct",
            true,
            availabilityCheck.ToString()
        );
        Log("Pressing M - Start Movement Phase");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.M, 0.1f));
        yield return new WaitForSeconds(0.2f);
        AssertTrue(
            movementPhaseManager.isActivated,
            "MP should be activated",
            true,
            movementPhaseManager.isActivated
        );
        AssertTrue(
            movementPhaseManager.isAwaitingTokenSelection,
            "MP should be waiting for token selection",
            true,
            movementPhaseManager.isAwaitingTokenSelection
        );
        AssertTrue(
            !movementPhaseManager.isAwaitingHexDestination,
            "MP should NOT be waiting for Hex selection",
            false,
            movementPhaseManager.isAwaitingHexDestination
        );
        Log("Clicking (10, 0) - Select Yaneva");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        AssertTrue(
            movementPhaseManager.isAwaitingTokenSelection,
            "MP should be waiting for token selection",
            true,
            movementPhaseManager.isAwaitingTokenSelection
        );
        AssertTrue(
            movementPhaseManager.isAwaitingHexDestination,
            "MP should be waiting for Hex selection",
            true,
            movementPhaseManager.isAwaitingHexDestination
        );
        Log("Clicking (11, 0) - Move Yaneva 1st Pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(11, 0), 0.5f));
        yield return new WaitForSeconds(0.8f);
        AssertTrue(
            !movementPhaseManager.isAwaitingTokenSelection,
            "MP should NOT be waiting for token selection, as dribbler is running",
            false,
            movementPhaseManager.isAwaitingTokenSelection
        );
        AssertTrue(
            movementPhaseManager.isDribblerRunning,
            "MP Dribbler should be running",
            true,
            movementPhaseManager.isDribblerRunning
        );
        AssertTrue(
            movementPhaseManager.isAwaitingHexDestination,
            "MP should be waiting for Hex selection",
            true,
            movementPhaseManager.isAwaitingHexDestination
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Count == 0,
            "MP Yaneva should be considered as running",
            0,
            movementPhaseManager.movedTokens.Count == 0
        );
        AssertTrue(
            movementPhaseManager.attackersMoved == 0,
            "MP Yaneva should be considered as running but MP has not advanced",
            0,
            movementPhaseManager.attackersMoved
        );
        AssertTrue(
            movementPhaseManager.remainingDribblerPace == 5,
            "MP Yaneva has remaining pace 5",
            5,
            movementPhaseManager.remainingDribblerPace
        );
        Log("Clicking (12, 1) - Move Yaneva 2nd Pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(12, 1), 0.5f));
        yield return new WaitForSeconds(0.8f);
        AssertTrue(
            goalKeeperManager.isActivated,
            "GK Manager Should be active for Yaneva entering the box with the ball",
            false,
            goalKeeperManager.isActivated
        );
        Log("Clicking (16, -1) - Move GK for Box");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(16, -1), 0.5f));
        yield return new WaitForSeconds(0.8f);
        AssertTrue(
            !goalKeeperManager.isActivated,
            "GK Manager Should NOT be active any more",
            false,
            goalKeeperManager.isActivated
        );
        AssertTrue(
            !movementPhaseManager.isAwaitingTokenSelection,
            "MP should NOT be waiting for token selection, as dribbler is running",
            false,
            movementPhaseManager.isAwaitingTokenSelection
        );
        AssertTrue(
            movementPhaseManager.isDribblerRunning,
            "MP Dribbler should be running",
            true,
            movementPhaseManager.isDribblerRunning
        );
        AssertTrue(
            movementPhaseManager.isAwaitingHexDestination,
            "MP should be waiting for Hex selection",
            true,
            movementPhaseManager.isAwaitingHexDestination
        );
        AssertTrue(
            movementPhaseManager.isWaitingForSnapshotDecision,
            "MP should be waiting for Snapshot decision",
            true,
            movementPhaseManager.isWaitingForSnapshotDecision
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Count == 0,
            "MP Yaneva should be considered as running",
            0,
            movementPhaseManager.movedTokens.Count == 0
        );
        AssertTrue(
            movementPhaseManager.attackersMoved == 0,
            "MP Yaneva should be considered as running but MP has not advanced",
            0,
            movementPhaseManager.attackersMoved
        );
        AssertTrue(
            movementPhaseManager.remainingDribblerPace == 4,
            "MP Yaneva has remaining pace 4",
            4,
            movementPhaseManager.remainingDribblerPace
        );
        Log("Clicking (13, 1) - Move Yaneva 3rd Pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(13, 1), 0.5f));
        yield return new WaitForSeconds(0.8f);
        AssertTrue(
            !movementPhaseManager.isAwaitingTokenSelection,
            "MP should NOT be waiting for token selection, as dribbler is running",
            false,
            movementPhaseManager.isAwaitingTokenSelection
        );
        AssertTrue(
            movementPhaseManager.isWaitingForSnapshotDecision,
            "MP should be waiting for Snapshot decision",
            true,
            movementPhaseManager.isWaitingForSnapshotDecision
        );
        AssertTrue(
            movementPhaseManager.isDribblerRunning,
            "MP Dribbler should be running",
            true,
            movementPhaseManager.isDribblerRunning
        );
        AssertTrue(
            movementPhaseManager.isAwaitingHexDestination,
            "MP should be waiting for Hex selection",
            true,
            movementPhaseManager.isAwaitingHexDestination
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Count == 0,
            "MP Yaneva should be considered as running",
            0,
            movementPhaseManager.movedTokens.Count == 0
        );
        AssertTrue(
            movementPhaseManager.attackersMoved == 0,
            "MP Yaneva should be considered as running but MP has not advanced",
            0,
            movementPhaseManager.attackersMoved
        );
        AssertTrue(
            movementPhaseManager.remainingDribblerPace == 3,
            "MP Yaneva has remaining pace 3",
            3,
            movementPhaseManager.remainingDribblerPace
        );
        Log("Clicking (14, 2) - Move Yaneva 4th Pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(14, 2), 0.5f));
        yield return new WaitForSeconds(0.8f);
        AssertTrue(
            !movementPhaseManager.isAwaitingTokenSelection,
            "MP should NOT be waiting for token selection, as dribbler is running",
            false,
            movementPhaseManager.isAwaitingTokenSelection
        );
        AssertTrue(
            movementPhaseManager.isDribblerRunning,
            "MP Dribbler should be running",
            true,
            movementPhaseManager.isDribblerRunning
        );
        AssertTrue(
            movementPhaseManager.isAwaitingHexDestination,
            "MP should be waiting for Hex selection",
            true,
            movementPhaseManager.isAwaitingHexDestination
        );
        AssertTrue(
            movementPhaseManager.isWaitingForSnapshotDecision,
            "MP should be waiting for Snapshot decision",
            true,
            movementPhaseManager.isWaitingForSnapshotDecision
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Count == 0,
            "MP Yaneva should be considered as running",
            0,
            movementPhaseManager.movedTokens.Count == 0
        );
        AssertTrue(
            movementPhaseManager.attackersMoved == 0,
            "MP Yaneva should be considered as running but MP has not advanced",
            0,
            movementPhaseManager.attackersMoved
        );
        AssertTrue(
            movementPhaseManager.remainingDribblerPace == 2,
            "MP Yaneva has remaining pace 2",
            2,
            movementPhaseManager.remainingDribblerPace
        );
        Log("Clicking (15, 2) - Move Yaneva 5th Pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(15, 2), 0.5f));
        yield return new WaitForSeconds(0.8f);
        AssertTrue(
            !movementPhaseManager.isAwaitingTokenSelection,
            "MP should NOT be waiting for token selection, as dribbler is running",
            false,
            movementPhaseManager.isAwaitingTokenSelection
        );
        AssertTrue(
            movementPhaseManager.isDribblerRunning,
            "MP Dribbler should be running",
            true,
            movementPhaseManager.isDribblerRunning
        );
        AssertTrue(
            movementPhaseManager.isWaitingForSnapshotDecision,
            "MP should be waiting for Snapshot decision",
            true,
            movementPhaseManager.isWaitingForSnapshotDecision
        );
        AssertTrue(
            movementPhaseManager.isAwaitingHexDestination,
            "MP should be waiting for Hex selection",
            true,
            movementPhaseManager.isAwaitingHexDestination
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Count == 0,
            "MP Yaneva should be considered as running",
            0,
            movementPhaseManager.movedTokens.Count == 0
        );
        AssertTrue(
            movementPhaseManager.attackersMoved == 0,
            "MP Yaneva should be considered as running but MP has not advanced",
            0,
            movementPhaseManager.attackersMoved
        );
        AssertTrue(
            movementPhaseManager.remainingDribblerPace == 1,
            "MP Yaneva has remaining pace 1",
            1,
            movementPhaseManager.remainingDribblerPace
        );
        Log("Clicking (16, 3) - Move Yaneva 6th Pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(16, 3), 0.5f));
        yield return new WaitForSeconds(0.8f);
        AssertTrue(
            !movementPhaseManager.isAwaitingTokenSelection,
            "MP should NOT be waiting for token selection, as dribbler is running",
            false,
            movementPhaseManager.isAwaitingTokenSelection
        );
        AssertTrue(
            !movementPhaseManager.isDribblerRunning,
            "MP Dribbler should NOT be running after 6th pace",
            false,
            movementPhaseManager.isDribblerRunning
        );
        AssertTrue(
            !movementPhaseManager.isAwaitingHexDestination,
            "MP should NOT be waiting for Hex selection after 6th pace",
            true,
            movementPhaseManager.isAwaitingHexDestination
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Count == 1,
            "MP Yaneva's movement should be considered as done",
            1,
            movementPhaseManager.movedTokens.Count == 1
        );
        AssertTrue(
            movementPhaseManager.attackersMoved == 0,
            "MP Yaneva should be considered as running but MP has not advanced",
            0,
            movementPhaseManager.attackersMoved
        );
        AssertTrue(
            movementPhaseManager.remainingDribblerPace == 0,
            "MP Yaneva has remaining pace 0",
            0,
            movementPhaseManager.remainingDribblerPace
        );
        AssertTrue(
            movementPhaseManager.isWaitingForSnapshotDecision,
            "MP should be waiting for Snapshot decision",
            true,
            movementPhaseManager.isWaitingForSnapshotDecision
        );
        Log("Pressing X - No Snapshot");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        yield return new WaitForSeconds(0.2f);
        AssertTrue(
            !movementPhaseManager.isWaitingForSnapshotDecision,
            "MP should NOT be waiting for Snapshot decision",
            false,
            movementPhaseManager.isWaitingForSnapshotDecision
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Count == 1,
            "MP Yaneva should NOT be considered as running",
            1,
            movementPhaseManager.movedTokens.Count == 1
        );
        AssertTrue(
            movementPhaseManager.attackersMoved == 1,
            "MP Yaneva's movement has been resolved",
            1,
            movementPhaseManager.attackersMoved
        );
        AssertTrue(
            movementPhaseManager.isMovementPhaseAttack,
            "MP should be in attacking part still",
            1,
            movementPhaseManager.isMovementPhaseAttack
        );
        yield return new WaitForSeconds(0.8f);
        Log("Pressing X - Forfeit Att MP");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        yield return new WaitForSeconds(0.2f);
        AssertTrue(
            movementPhaseManager.isMovementPhaseDef,
            "MP should be in defensive part now",
            1,
            movementPhaseManager.isMovementPhaseDef
        );
        Log("Pressing X - Forfeit Def MP");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        yield return new WaitForSeconds(0.2f);
        AssertTrue(
            movementPhaseManager.isMovementPhase2f2,
            "MP should be in 2f2 part now",
            1,
            movementPhaseManager.isMovementPhase2f2
        );
        Log("Pressing X - Forfeit 2f2 MP");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        yield return new WaitForSeconds(0.2f);
        AssertTrue(
            finalThirdManager.isActivated,
            "F3 should be activated",
            true,
            finalThirdManager.isActivated
        );
        Log("Pressing X - Forfeit Att F3");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        yield return new WaitForSeconds(0.2f);
        AssertTrue(
            finalThirdManager.isActivated,
            "F3 should be activated",
            true,
            finalThirdManager.isActivated
        );
        Log("Pressing X - Forfeit Def F3");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        yield return new WaitForSeconds(0.2f);
        AssertTrue(
            !finalThirdManager.isActivated,
            "F3 should no longer be activated",
            false,
            finalThirdManager.isActivated
        );

        Log("Pressing M - Start Movement Phase");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.M, 0.1f));
        yield return new WaitForSeconds(0.2f);
        Log("Clicking (16, 3) - Select Yaneva");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(16, 3), 0.5f));
        yield return new WaitForSeconds(0.8f);
        Log("Clicking (17, 3) - Move Yaneva 1st Pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(17, 3), 0.5f));
        yield return new WaitForSeconds(0.8f);
        Log("Clicking (18, 3) - Move Yaneva 2nd Pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(18, 3), 0.5f));
        yield return new WaitForSeconds(0.8f);
        Log("Clicking (19, 3) - Move Yaneva 3rd Pace IN GOAL");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(19, 3), 0.5f));
        yield return new WaitForSeconds(0.8f);
        AssertTrue(
            !movementPhaseManager.isActivated
            , "MP Manager is no longer activated"
        );
        AssertTrue(
            goalFlowManager.isActivated
            , "GoalFlow is activated"
        );
        while (goalFlowManager.isActivated) yield return null;
        AssertTrue(
            !goalFlowManager.isActivated
            , "GoalFlow is no longer activated"
        );

        LogFooterofTest("MovementPhase Dribble IN GOAL!");
    }
    
    private IEnumerator Scenario_025b_Movement_Phase_Reposition_into_goal()
    {
        yield return new WaitForSeconds(1.5f); // Allow scene to stabilize
        Log("▶️ Starting test scenario: MovementPhase Reposition IN GOAL!");
        Log("Pressing 2");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Alpha2, 0.1f));
        AssertTrue(
            MatchManager.Instance.currentState == MatchManager.GameState.KickOffSetup,
            "Game is in KickOff Setup",
            MatchManager.GameState.KickOffSetup,
            MatchManager.Instance.currentState
        );
        Log("Pressing Space");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Space, 0.1f));
        AssertTrue(
            MatchManager.Instance.currentState == MatchManager.GameState.KickoffBlown,
            "Game is in KickoffBlown",
            MatchManager.GameState.KickoffBlown,
            MatchManager.Instance.currentState
        );
        Log("Pressing P - Pass to Yaneva");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.P, 0.1f));
        AssertTrue(
            groundBallManager.isActivated,
            "GBM should be activated",
            false,
            groundBallManager.isActivated
        );
        AssertTrue(
            groundBallManager.isAwaitingTargetSelection,
            "GBM should be waiting for a target",
            false,
            groundBallManager.isAwaitingTargetSelection
        );
        AssertTrue(
            groundBallManager.currentTargetHex == null,
            "GBM should be waiting for a target, but there is no target yet",
            false,
            groundBallManager.isAwaitingTargetSelection
        );
        Log("Clicking (10, 0)");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        AssertTrue(
            groundBallManager.isActivated,
            "GBM should be activated",
            false,
            groundBallManager.isActivated
        );
        AssertTrue(
            groundBallManager.isAwaitingTargetSelection,
            "GBM should be waiting for a target",
            false,
            groundBallManager.isAwaitingTargetSelection
        );
        AssertTrue(
            groundBallManager.currentTargetHex != null,
            "GBM should be waiting for a target, but there a target was clicked"
        );
        Log("Clicking (10, 0) again");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        Log("Wait for the ball to move");
        yield return new WaitForSeconds(3f); // for the ball to move
        AvailabilityCheckResult availabilityCheck = AssertCorrectAvailabilityAfterGBToPlayer();
        AssertTrue(
            availabilityCheck.passed,
            "Action Availability after GB to Player is correct",
            true,
            availabilityCheck.ToString()
        );
        AssertTrue(
            finalThirdManager.isActivated,
            "F3 should be activated",
            true,
            finalThirdManager.isActivated
        );
        Log("Pressing X - Forfeit Att F3");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        yield return new WaitForSeconds(0.2f);
        AssertTrue(
            finalThirdManager.isActivated,
            "F3 should be activated",
            true,
            finalThirdManager.isActivated
        );
        Log("Pressing X - Forfeit Def F3");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        yield return new WaitForSeconds(0.2f);
        AssertTrue(
            !finalThirdManager.isActivated,
            "F3 should no longer be activated",
            false,
            finalThirdManager.isActivated
        );
        AssertTrue(
            availabilityCheck.passed,
            "Action Availability after GB to Player is correct",
            true,
            availabilityCheck.ToString()
        );
        Log("Pressing M - Start Movement Phase");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.M, 0.1f));
        yield return new WaitForSeconds(0.2f);
        AssertTrue(
            movementPhaseManager.isActivated,
            "MP should be activated",
            true,
            movementPhaseManager.isActivated
        );
        AssertTrue(
            movementPhaseManager.isAwaitingTokenSelection,
            "MP should be waiting for token selection",
            true,
            movementPhaseManager.isAwaitingTokenSelection
        );
        AssertTrue(
            !movementPhaseManager.isAwaitingHexDestination,
            "MP should NOT be waiting for Hex selection",
            false,
            movementPhaseManager.isAwaitingHexDestination
        );
        Log("Clicking (10, 0) - Select Yaneva");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
        AssertTrue(
            movementPhaseManager.isAwaitingTokenSelection,
            "MP should be waiting for token selection",
            true,
            movementPhaseManager.isAwaitingTokenSelection
        );
        AssertTrue(
            movementPhaseManager.isAwaitingHexDestination,
            "MP should be waiting for Hex selection",
            true,
            movementPhaseManager.isAwaitingHexDestination
        );
        Log("Clicking (11, 0) - Move Yaneva 1st Pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(11, 0), 0.5f));
        yield return new WaitForSeconds(0.8f);
        AssertTrue(
            !movementPhaseManager.isAwaitingTokenSelection,
            "MP should NOT be waiting for token selection, as dribbler is running",
            false,
            movementPhaseManager.isAwaitingTokenSelection
        );
        AssertTrue(
            movementPhaseManager.isDribblerRunning,
            "MP Dribbler should be running",
            true,
            movementPhaseManager.isDribblerRunning
        );
        AssertTrue(
            movementPhaseManager.isAwaitingHexDestination,
            "MP should be waiting for Hex selection",
            true,
            movementPhaseManager.isAwaitingHexDestination
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Count == 0,
            "MP Yaneva should be considered as running",
            0,
            movementPhaseManager.movedTokens.Count == 0
        );
        AssertTrue(
            movementPhaseManager.attackersMoved == 0,
            "MP Yaneva should be considered as running but MP has not advanced",
            0,
            movementPhaseManager.attackersMoved
        );
        AssertTrue(
            movementPhaseManager.remainingDribblerPace == 5,
            "MP Yaneva has remaining pace 5",
            5,
            movementPhaseManager.remainingDribblerPace
        );
        Log("Clicking (12, 1) - Move Yaneva 2nd Pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(12, 1), 0.5f));
        yield return new WaitForSeconds(0.8f);
        AssertTrue(
            goalKeeperManager.isActivated,
            "GK Manager Should be active for Yaneva entering the box with the ball",
            false,
            goalKeeperManager.isActivated
        );
        Log("Clicking (16, -1) - Move GK for Box");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(16, -1), 0.5f));
        yield return new WaitForSeconds(0.8f);
        AssertTrue(
            !goalKeeperManager.isActivated,
            "GK Manager Should NOT be active any more",
            false,
            goalKeeperManager.isActivated
        );
        AssertTrue(
            !movementPhaseManager.isAwaitingTokenSelection,
            "MP should NOT be waiting for token selection, as dribbler is running",
            false,
            movementPhaseManager.isAwaitingTokenSelection
        );
        AssertTrue(
            movementPhaseManager.isDribblerRunning,
            "MP Dribbler should be running",
            true,
            movementPhaseManager.isDribblerRunning
        );
        AssertTrue(
            movementPhaseManager.isAwaitingHexDestination,
            "MP should be waiting for Hex selection",
            true,
            movementPhaseManager.isAwaitingHexDestination
        );
        AssertTrue(
            movementPhaseManager.isWaitingForSnapshotDecision,
            "MP should be waiting for Snapshot decision",
            true,
            movementPhaseManager.isWaitingForSnapshotDecision
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Count == 0,
            "MP Yaneva should be considered as running",
            0,
            movementPhaseManager.movedTokens.Count == 0
        );
        AssertTrue(
            movementPhaseManager.attackersMoved == 0,
            "MP Yaneva should be considered as running but MP has not advanced",
            0,
            movementPhaseManager.attackersMoved
        );
        AssertTrue(
            movementPhaseManager.remainingDribblerPace == 4,
            "MP Yaneva has remaining pace 4",
            4,
            movementPhaseManager.remainingDribblerPace
        );
        Log("Clicking (13, 1) - Move Yaneva 3rd Pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(13, 1), 0.5f));
        yield return new WaitForSeconds(0.8f);
        AssertTrue(
            !movementPhaseManager.isAwaitingTokenSelection,
            "MP should NOT be waiting for token selection, as dribbler is running",
            false,
            movementPhaseManager.isAwaitingTokenSelection
        );
        AssertTrue(
            movementPhaseManager.isWaitingForSnapshotDecision,
            "MP should be waiting for Snapshot decision",
            true,
            movementPhaseManager.isWaitingForSnapshotDecision
        );
        AssertTrue(
            movementPhaseManager.isDribblerRunning,
            "MP Dribbler should be running",
            true,
            movementPhaseManager.isDribblerRunning
        );
        AssertTrue(
            movementPhaseManager.isAwaitingHexDestination,
            "MP should be waiting for Hex selection",
            true,
            movementPhaseManager.isAwaitingHexDestination
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Count == 0,
            "MP Yaneva should be considered as running",
            0,
            movementPhaseManager.movedTokens.Count == 0
        );
        AssertTrue(
            movementPhaseManager.attackersMoved == 0,
            "MP Yaneva should be considered as running but MP has not advanced",
            0,
            movementPhaseManager.attackersMoved
        );
        AssertTrue(
            movementPhaseManager.remainingDribblerPace == 3,
            "MP Yaneva has remaining pace 3",
            3,
            movementPhaseManager.remainingDribblerPace
        );
        Log("Clicking (14, 2) - Move Yaneva 4th Pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(14, 2), 0.5f));
        yield return new WaitForSeconds(0.8f);
        AssertTrue(
            !movementPhaseManager.isAwaitingTokenSelection,
            "MP should NOT be waiting for token selection, as dribbler is running",
            false,
            movementPhaseManager.isAwaitingTokenSelection
        );
        AssertTrue(
            movementPhaseManager.isDribblerRunning,
            "MP Dribbler should be running",
            true,
            movementPhaseManager.isDribblerRunning
        );
        AssertTrue(
            movementPhaseManager.isAwaitingHexDestination,
            "MP should be waiting for Hex selection",
            true,
            movementPhaseManager.isAwaitingHexDestination
        );
        AssertTrue(
            movementPhaseManager.isWaitingForSnapshotDecision,
            "MP should be waiting for Snapshot decision",
            true,
            movementPhaseManager.isWaitingForSnapshotDecision
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Count == 0,
            "MP Yaneva should be considered as running",
            0,
            movementPhaseManager.movedTokens.Count == 0
        );
        AssertTrue(
            movementPhaseManager.attackersMoved == 0,
            "MP Yaneva should be considered as running but MP has not advanced",
            0,
            movementPhaseManager.attackersMoved
        );
        AssertTrue(
            movementPhaseManager.remainingDribblerPace == 2,
            "MP Yaneva has remaining pace 2",
            2,
            movementPhaseManager.remainingDribblerPace
        );
        Log("Clicking (15, 2) - Move Yaneva 5th Pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(15, 2), 0.5f));
        yield return new WaitForSeconds(0.8f);
        AssertTrue(
            !movementPhaseManager.isAwaitingTokenSelection,
            "MP should NOT be waiting for token selection, as dribbler is running",
            false,
            movementPhaseManager.isAwaitingTokenSelection
        );
        AssertTrue(
            movementPhaseManager.isDribblerRunning,
            "MP Dribbler should be running",
            true,
            movementPhaseManager.isDribblerRunning
        );
        AssertTrue(
            movementPhaseManager.isWaitingForSnapshotDecision,
            "MP should be waiting for Snapshot decision",
            true,
            movementPhaseManager.isWaitingForSnapshotDecision
        );
        AssertTrue(
            movementPhaseManager.isAwaitingHexDestination,
            "MP should be waiting for Hex selection",
            true,
            movementPhaseManager.isAwaitingHexDestination
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Count == 0,
            "MP Yaneva should be considered as running",
            0,
            movementPhaseManager.movedTokens.Count == 0
        );
        AssertTrue(
            movementPhaseManager.attackersMoved == 0,
            "MP Yaneva should be considered as running but MP has not advanced",
            0,
            movementPhaseManager.attackersMoved
        );
        AssertTrue(
            movementPhaseManager.remainingDribblerPace == 1,
            "MP Yaneva has remaining pace 1",
            1,
            movementPhaseManager.remainingDribblerPace
        );
        Log("Clicking (16, 3) - Move Yaneva 6th Pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(16, 3), 0.5f));
        yield return new WaitForSeconds(0.8f);
        AssertTrue(
            !movementPhaseManager.isAwaitingTokenSelection,
            "MP should NOT be waiting for token selection, as dribbler is running",
            false,
            movementPhaseManager.isAwaitingTokenSelection
        );
        AssertTrue(
            !movementPhaseManager.isDribblerRunning,
            "MP Dribbler should NOT be running after 6th pace",
            false,
            movementPhaseManager.isDribblerRunning
        );
        AssertTrue(
            !movementPhaseManager.isAwaitingHexDestination,
            "MP should NOT be waiting for Hex selection after 6th pace",
            true,
            movementPhaseManager.isAwaitingHexDestination
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Count == 1,
            "MP Yaneva's movement should be considered as done",
            1,
            movementPhaseManager.movedTokens.Count == 1
        );
        AssertTrue(
            movementPhaseManager.attackersMoved == 0,
            "MP Yaneva should be considered as running but MP has not advanced",
            0,
            movementPhaseManager.attackersMoved
        );
        AssertTrue(
            movementPhaseManager.remainingDribblerPace == 0,
            "MP Yaneva has remaining pace 0",
            0,
            movementPhaseManager.remainingDribblerPace
        );
        AssertTrue(
            movementPhaseManager.isWaitingForSnapshotDecision,
            "MP should be waiting for Snapshot decision",
            true,
            movementPhaseManager.isWaitingForSnapshotDecision
        );
        Log("Pressing X - No Snapshot");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        yield return new WaitForSeconds(0.2f);
        AssertTrue(
            !movementPhaseManager.isWaitingForSnapshotDecision,
            "MP should NOT be waiting for Snapshot decision",
            false,
            movementPhaseManager.isWaitingForSnapshotDecision
        );
        AssertTrue(
            movementPhaseManager.movedTokens.Count == 1,
            "MP Yaneva should NOT be considered as running",
            1,
            movementPhaseManager.movedTokens.Count == 1
        );
        AssertTrue(
            movementPhaseManager.attackersMoved == 1,
            "MP Yaneva's movement has been resolved",
            1,
            movementPhaseManager.attackersMoved
        );
        AssertTrue(
            movementPhaseManager.isMovementPhaseAttack,
            "MP should be in attacking part still",
            1,
            movementPhaseManager.isMovementPhaseAttack
        );
        yield return new WaitForSeconds(0.8f);
        Log("Pressing X - Forfeit Att MP");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        yield return new WaitForSeconds(0.2f);
        AssertTrue(
            movementPhaseManager.isMovementPhaseDef,
            "MP should be in defensive part now",
            1,
            movementPhaseManager.isMovementPhaseDef
        );
        Log("Pressing X - Forfeit Def MP");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        yield return new WaitForSeconds(0.2f);
        AssertTrue(
            movementPhaseManager.isMovementPhase2f2,
            "MP should be in 2f2 part now",
            1,
            movementPhaseManager.isMovementPhase2f2
        );
        Log("Pressing X - Forfeit 2f2 MP");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        yield return new WaitForSeconds(0.2f);
        AssertTrue(
            finalThirdManager.isActivated,
            "F3 should be activated",
            true,
            finalThirdManager.isActivated
        );
        Log("Pressing X - Forfeit Att F3");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        yield return new WaitForSeconds(0.2f);
        AssertTrue(
            finalThirdManager.isActivated,
            "F3 should be activated",
            true,
            finalThirdManager.isActivated
        );
        Log("Pressing X - Forfeit Def F3");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        yield return new WaitForSeconds(0.2f);
        AssertTrue(
            !finalThirdManager.isActivated,
            "F3 should no longer be activated",
            false,
            finalThirdManager.isActivated
        );
        Log("Pressing M - Start Movement Phase");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.M, 0.1f));
        yield return new WaitForSeconds(0.2f);
        Log("Clicking (16, 3) - Select Yaneva");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(16, 3), 0.5f));
        yield return new WaitForSeconds(0.8f);
        Log("Clicking (17, 3) - Move Yaneva 1st Pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(17, 3), 0.5f));
        yield return new WaitForSeconds(0.8f);
        Log("Clicking (18, 3) - Move Yaneva 2nd Pace");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(18, 3), 0.5f));
        yield return new WaitForSeconds(0.8f);
        Log("Pressing X - Forfeit Yaneva's Snapshot and remaining Pace");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        yield return new WaitForSeconds(0.2f);
        Log("Pressing X - Forfeit Rest of Att MP");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        yield return new WaitForSeconds(0.2f);
        AssertTrue(
            movementPhaseManager.isMovementPhaseDef,
            "MP should be in defensive part now",
            true,
            movementPhaseManager.isMovementPhaseDef
        );
        Log("Clicking (18, 0) - Select Poulsen");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(18, 0), 0.5f));
        yield return new WaitForSeconds(0.8f);
        Log("Clicking (18, 2) - Move Yansen");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(18, 2), 0.5f));
        yield return new WaitForSeconds(0.8f);
        AssertTrue(
            movementPhaseManager.isWaitingForTackleDecision,
            "MP should be in tackle decision from Poulsen",
            true,
            movementPhaseManager.isWaitingForTackleDecision
        );
        Log("Pressing T - Tackle");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.T, 0.1f));
        yield return new WaitForSeconds(0.2f);
        AssertTrue(
            !movementPhaseManager.isWaitingForTackleDecision,
            "MP should no longer be in tackle decision from Poulsen",
            false,
            movementPhaseManager.isWaitingForTackleDecision
        );
        AssertTrue(
            movementPhaseManager.isWaitingForTackleRoll,
            "MP should be wating for tackle rolls",
            false,
            movementPhaseManager.isWaitingForTackleRoll
        );
        movementPhaseManager.PerformTackleDiceRoll(isDefender: true, 4);
        yield return new WaitForSeconds(0.2f);
        movementPhaseManager.PerformTackleDiceRoll(isDefender: false, 4);
        yield return new WaitForSeconds(0.2f);
        AssertTrue(
            movementPhaseManager.isWaitingForReposition,
            "MovementPhase Should be waiting for Reposition after Tackle Rolls",
            true,
            movementPhaseManager.isWaitingForReposition
        );
        Log("Clicking (19, 2) - Reposition Yaneva IN GOAL");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(19, 2), 0.5f));
        yield return new WaitForSeconds(1.8f);
        AssertTrue(
            !movementPhaseManager.isActivated
            , "MP Manager is no longer activated"
        );
        AssertTrue(
            goalFlowManager.isActivated
            , "GoalFlow is activated"
        );
        while (goalFlowManager.isActivated) yield return null;
        AssertTrue(
            !goalFlowManager.isActivated
            , "GoalFlow is no longer activated"
        );

        LogFooterofTest("MovementPhase Reposition IN GOAL!");

    }
    
    // Duplicate This one for header at goal
    private IEnumerator Scenario_026_HighPass_onAttacker_MoveAtt_moveDef_AccurateHP()
    {
      yield return new WaitForSeconds(1.5f); // Allow scene to stabilize
      Log("▶️ Starting test scenario: High Pass on Attacker, Attacking and Defensive moves before Accurate Pass.");
      Log("Pressing 2");
      yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Alpha2, 0.1f));
      AssertTrue(
          MatchManager.Instance.currentState == MatchManager.GameState.KickOffSetup,
          "Game is in KickOff Setup",
          MatchManager.GameState.KickOffSetup,
          MatchManager.Instance.currentState
      );
      Log("Pressing Space");
      yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Space, 0.1f));
      AssertTrue(
          MatchManager.Instance.currentState == MatchManager.GameState.KickoffBlown,
          "Game is in KickoffBlown",
          MatchManager.GameState.KickoffBlown,
          MatchManager.Instance.currentState
      );
      Log("Pressing C - Call a HighPass");
      yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.C, 0.1f));
      Log("Click On (13, 1) - Intitial HP Target");
      yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(13, 1), 0.5f));
      AssertTrue(
          highPassManager.eligibleAttackers.Count == 1,
          "HP target is has 1 eligible Attacker",
          1,
          highPassManager.eligibleAttackers.Count
      );
      AssertTrue(
          highPassManager.currentTargetHex == hexgrid.GetHexCellAt(new Vector3Int(13, 0, 1)),
          "HP target is the key pressed",
          hexgrid.GetHexCellAt(new Vector3Int(13, 0, 1)),
          highPassManager.currentTargetHex
      );
      yield return new WaitForSeconds(0.5f);
      Log("Click On (7, 5) - Intitial HP Target");
      yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(7, 5), 0.5f));
      AssertTrue(
          highPassManager.eligibleAttackers.Count == 3,
          "HP target is has 3 eligible Attacker",
          3,
          highPassManager.eligibleAttackers.Count
      );
      AssertTrue(
          highPassManager.isWaitingForConfirmation,
          "HP target is waiting for target confirmation",
          true,
          highPassManager.isWaitingForConfirmation
      );
      AssertTrue(
          highPassManager.currentTargetHex == hexgrid.GetHexCellAt(new Vector3Int(7, 0, 5)),
          "HP target is the key pressed",
          hexgrid.GetHexCellAt(new Vector3Int(7, 0, 5)),
          highPassManager.currentTargetHex
      );
      yield return new WaitForSeconds(0.5f);
      Log("Click On (-6, 3) - Intitial HP Target");
      yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(-6, 3), 0.5f));
      AssertTrue(
          highPassManager.eligibleAttackers.Count == 0,
          "HP target is has no eligible Attacker",
          0,
          highPassManager.eligibleAttackers.Count
      );
      AssertTrue(
          highPassManager.isWaitingForConfirmation,
          "HP target is waiting for target confirmation",
          true,
          highPassManager.isWaitingForConfirmation
      );
      AssertTrue(
          highPassManager.currentTargetHex == null,
          "HP target is cleared"
      );
      yield return new WaitForSeconds(0.5f);
      Log("Click On (10, 0) - Intitial HP Target");
      yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
      AssertTrue(
          highPassManager.eligibleAttackers.Count == 1,
          "HP target is has 1 eligible Attacker",
          1,
          highPassManager.eligibleAttackers.Count
      );
      AssertTrue(
          highPassManager.isWaitingForConfirmation,
          "HP target is waiting for target confirmation",
          true,
          highPassManager.isWaitingForConfirmation
      );
      AssertTrue(
          highPassManager.currentTargetHex == hexgrid.GetHexCellAt(new Vector3Int(10, 0, 0)),
          "HP target is the key pressed",
          hexgrid.GetHexCellAt(new Vector3Int(10, 0, 0)),
          highPassManager.currentTargetHex
      );
      Log("Click On (10, 0) - Confirm HP Target on Yaneva");
      yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
      AssertTrue(
          !highPassManager.isWaitingForConfirmation,
          "HP target is NO LONGER waiting for target confirmation",
          false,
          highPassManager.isWaitingForConfirmation
      );
      AssertTrue(
          highPassManager.eligibleAttackers.Count == 1,
          "HP target is has 1 eligible Attacker",
          1,
          highPassManager.eligibleAttackers.Count
      );
      AssertTrue(
          highPassManager.isWaitingForAttackerSelection,
          "HP target is waiting for Attacker selection",
          true,
          highPassManager.isWaitingForAttackerSelection
      );
      Log("Click On (-12, 0) - Click on a Defender");
      yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(-12, 0), 0.5f));
      AssertTrue(
          highPassManager.isWaitingForAttackerSelection,
          "HP target is waiting for Attacker selection",
          true,
          highPassManager.isWaitingForAttackerSelection
      );
      AssertTrue(
          !highPassManager.isWaitingForAttackerMove,
          "HP target is NOT waiting for Attacker move",
          false,
          highPassManager.isWaitingForAttackerMove
      );
      Log("Click On (-4, -4) - Click on an Attacker");
      yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(-4, -4), 0.5f));
      AssertTrue(
          highPassManager.isWaitingForAttackerSelection,
          "HP target is waiting for Attacker selection",
          true,
          highPassManager.isWaitingForAttackerSelection
      );
      AssertTrue(
          highPassManager.isWaitingForAttackerMove,
          "HP target is waiting for Attacker move",
          true,
          highPassManager.isWaitingForAttackerMove
      );
      Log("Click On (10, 0) - Click on the locked Attacker");
      yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 0), 0.5f));
      AssertTrue(
          highPassManager.isWaitingForAttackerSelection,
          "HP target is waiting for Attacker selection",
          true,
          highPassManager.isWaitingForAttackerSelection
      );
      AssertTrue(
          !highPassManager.isWaitingForAttackerMove,
          "HP target is NOT waiting for Attacker move",
          false,
          highPassManager.isWaitingForAttackerMove
      );
      Log("Click On (-4, -4) - Click on an Attacker");
      yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(-4, -4), 0.5f));
      AssertTrue(
          highPassManager.isWaitingForAttackerSelection,
          "HP target is waiting for Attacker selection",
          true,
          highPassManager.isWaitingForAttackerSelection
      );
      AssertTrue(
          highPassManager.isWaitingForAttackerMove,
          "HP target is waiting for Attacker move",
          true,
          highPassManager.isWaitingForAttackerMove
      );
      Log("Click On (0, 8) - Click on an enpty Hex, not in Highlights");
      yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(0, 8), 0.5f));
      AssertTrue(
          highPassManager.isWaitingForAttackerSelection,
          "HP target is waiting for Attacker selection",
          true,
          highPassManager.isWaitingForAttackerSelection
      );
      AssertTrue(
          !highPassManager.isWaitingForAttackerMove,
          "HP target is waiting for Attacker move",
          false,
          highPassManager.isWaitingForAttackerMove
      );
      Log("Click On (-4, -4) - Click on an Attacker");
      yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(-4, -4), 0.5f));
      AssertTrue(
          highPassManager.isWaitingForAttackerSelection,
          "HP target is waiting for Attacker selection",
          true,
          highPassManager.isWaitingForAttackerSelection
      );
      AssertTrue(
          highPassManager.isWaitingForAttackerMove,
          "HP target is waiting for Attacker move",
          true,
          highPassManager.isWaitingForAttackerMove
      );
      Log("Click On (-1, -5) - Click on an valid Hex");
      yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(-1, -5), 0.5f));
      yield return new WaitForSeconds(1.5f);
      AssertTrue(
          !highPassManager.isWaitingForAttackerSelection,
          "HP target is waiting for Attacker selection",
          false,
          highPassManager.isWaitingForAttackerSelection
      );
      AssertTrue(
          !highPassManager.isWaitingForAttackerMove,
          "HP target is waiting for Attacker move",
          false,
          highPassManager.isWaitingForAttackerMove
      );
      AssertTrue(
          highPassManager.isWaitingForDefenderSelection,
          "HP target is waiting for Defender selection",
          true,
          highPassManager.isWaitingForDefenderSelection
      );
      AssertTrue(
          !highPassManager.isWaitingForDefenderMove,
          "HP target is waiting for Defender move",
          false,
          highPassManager.isWaitingForDefenderMove
      );
      Log("Click On (4, 5) - Click on an valid Hex");
      yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(4, 5), 0.5f));
      yield return new WaitForSeconds(1);
      AssertTrue(
          !highPassManager.isWaitingForAttackerSelection,
          "HP target is waiting for Attacker selection",
          false,
          highPassManager.isWaitingForAttackerSelection
      );
      AssertTrue(
          !highPassManager.isWaitingForAttackerMove,
          "HP target is waiting for Attacker move",
          false,
          highPassManager.isWaitingForAttackerMove
      );
      AssertTrue(
          highPassManager.isWaitingForDefenderSelection,
          "HP target is waiting for Defender selection",
          true,
          highPassManager.isWaitingForDefenderSelection
      );
      AssertTrue(
          highPassManager.isWaitingForDefenderMove,
          "HP target is waiting for Defender move",
          true,
          highPassManager.isWaitingForDefenderMove
      );
      Log("Click On (1, 2) - Click on an valid Defender");
      yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(1, 2), 0.5f));
      AssertTrue(
          !highPassManager.isWaitingForAttackerSelection,
          "HP target is waiting for Attacker selection",
          false,
          highPassManager.isWaitingForAttackerSelection
      );
      AssertTrue(
          !highPassManager.isWaitingForAttackerMove,
          "HP target is waiting for Attacker move",
          false,
          highPassManager.isWaitingForAttackerMove
      );
      AssertTrue(
          highPassManager.isWaitingForDefenderSelection,
          "HP target is waiting for Defender selection",
          true,
          highPassManager.isWaitingForDefenderSelection
      );
      AssertTrue(
          highPassManager.isWaitingForDefenderMove,
          "HP target is waiting for Defender move",
          true,
          highPassManager.isWaitingForDefenderMove
      );
      Log("Click On (-8, -8) - Click on an Attacker");
      yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(-8, -8), 0.5f));
      AssertTrue(
          !highPassManager.isWaitingForAttackerSelection,
          "HP target is waiting for Attacker selection",
          false,
          highPassManager.isWaitingForAttackerSelection
      );
      AssertTrue(
          !highPassManager.isWaitingForAttackerMove,
          "HP target is waiting for Attacker move",
          false,
          highPassManager.isWaitingForAttackerMove
      );
      AssertTrue(
          highPassManager.isWaitingForDefenderSelection,
          "HP target is waiting for Defender selection",
          true,
          highPassManager.isWaitingForDefenderSelection
      );
      AssertTrue(
          !highPassManager.isWaitingForDefenderMove,
          "HP target is waiting for Defender move",
          false,
          highPassManager.isWaitingForDefenderMove
      );
      Log("Click On (14, 0) - Click on an valid Defender");
      yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(14, 0), 0.5f));
      AssertTrue(
          !highPassManager.isWaitingForAttackerSelection,
          "HP target is waiting for Attacker selection",
          false,
          highPassManager.isWaitingForAttackerSelection
      );
      AssertTrue(
          !highPassManager.isWaitingForAttackerMove,
          "HP target is waiting for Attacker move",
          false,
          highPassManager.isWaitingForAttackerMove
      );
      AssertTrue(
          highPassManager.isWaitingForDefenderSelection,
          "HP target is waiting for Defender selection",
          true,
          highPassManager.isWaitingForDefenderSelection
      );
      AssertTrue(
          highPassManager.isWaitingForDefenderMove,
          "HP target is waiting for Defender move",
          true,
          highPassManager.isWaitingForDefenderMove
      );
      AssertTrue(
          !highPassManager.isWaitingForAccuracyRoll,
          "HP target is NOT waiting for Accuracy Roll yet",
          false,
          highPassManager.isWaitingForAccuracyRoll
      );
      Log("Click On (14, 1) - Click on an valid Hex");
      yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(14, 1), 0.5f));
      yield return new WaitForSeconds(1);
      AssertTrue(
          !highPassManager.isWaitingForAttackerSelection,
          "HP target is waiting for Attacker selection",
          false,
          highPassManager.isWaitingForAttackerSelection
      );
      AssertTrue(
          !highPassManager.isWaitingForAttackerMove,
          "HP target is waiting for Attacker move",
          false,
          highPassManager.isWaitingForAttackerMove
      );
      AssertTrue(
          !highPassManager.isWaitingForDefenderSelection,
          "HP target is waiting for Defender selection",
          false,
          highPassManager.isWaitingForDefenderSelection
      );
      AssertTrue(
          !highPassManager.isWaitingForDefenderMove,
          "HP target is waiting for Defender move",
          false,
          highPassManager.isWaitingForDefenderMove
      );
      AssertTrue(
          highPassManager.isWaitingForAccuracyRoll,
          "HP target is NOT waiting for Accuracy Roll yet",
          true,
          highPassManager.isWaitingForAccuracyRoll
      );
      highPassManager.PerformAccuracyRoll(6);
      yield return new WaitForSeconds(5);
      AssertTrue(
          !highPassManager.isActivated,
          "HP should be done",
          false,
          highPassManager.isActivated
      );
      AssertTrue(
          headerManager.isActivated,
          "header Manager should be activated",
          true,
          headerManager.isActivated
      );
      AssertTrue(
          headerManager.isWaitingForControlOrHeaderDecision,
          "header Manager should isWaitingForControlOrHeaderDecision",
          true,
          headerManager.isWaitingForControlOrHeaderDecision
      );
      Log("Pressing X - Forfeit Att F3");
      yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
      yield return new WaitForSeconds(0.2f);
      Log("Pressing X - Forfeit Def F3");
      yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
      yield return new WaitForSeconds(0.2f);

      LogFooterofTest("High Pass on Attacker, Attacking and Defensive moves before Accurate Pass.");
    }

    private IEnumerator Scenario_027_HighPass_on_Attacker_MoveAtt_moveDef_Accurate_HP_BC()
    {
        Log("▶️ Starting test scenario: High Pass on Attacker, Attacking and Defensive moves before Accurate Pass Defense cannot challenge.");
        yield return Scenario_026_HighPass_onAttacker_MoveAtt_moveDef_AccurateHP();
        yield return new WaitForSeconds(0.5f);
        Log("Pressing B - Ball Control");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.B, 0.1f));
        yield return new WaitForSeconds(0.5f);
        AssertTrue(
            !headerManager.isWaitingForControlOrHeaderDecision,
            "header Manager should NOT isWaitingForControlOrHeaderDecision",
            false,
            headerManager.isWaitingForControlOrHeaderDecision
        );
        AssertTrue(
            headerManager.isWaitingForControlRoll,
            "header Manager should isWaitingForControlRoll",
            true,
            headerManager.isWaitingForControlRoll
        );
        AssertTrue(
            headerManager.challengeWinner == PlayerToken.GetPlayerTokenByName("Yaneva"),
            "Yaneva should be the winner",
            PlayerToken.GetPlayerTokenByName("Yaneva"),
            headerManager.challengeWinner
        );
        // AssertTrue(false, "break");
        LogFooterofTest("High Pass on Attacker, Attacking and Defensive moves before Accurate Pass Defense cannot challenge");
    }

    private IEnumerator Scenario_027_HighPass_onAttacker_MoveAtt_moveDef_INAccurateHP()
    {
        yield return new WaitForSeconds(1.5f); // Allow scene to stabilize
        Log("▶️ Starting test scenario: High Pass on Attacker, Move Passer, defender and INAccurate HP.");
        Log("Pressing 2");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Alpha2, 0.1f));
        AssertTrue(
            MatchManager.Instance.currentState == MatchManager.GameState.KickOffSetup,
            "Game is in KickOff Setup",
            MatchManager.GameState.KickOffSetup,
            MatchManager.Instance.currentState
        );
        Log("Pressing Space");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Space, 0.1f));
        AssertTrue(
            MatchManager.Instance.currentState == MatchManager.GameState.KickoffBlown,
            "Game is in KickoffBlown",
            MatchManager.GameState.KickoffBlown,
            MatchManager.Instance.currentState
        );
        Log("Pressing C - Call a HighPass");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.C, 0.1f));
        Log("Click On (4, 4) - Intitial HP Target");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(4, 4), 0.5f));
        AssertTrue(
            highPassManager.eligibleAttackers.Count == 1,
            "HP target is has 1 eligible Attacker",
            1,
            highPassManager.eligibleAttackers.Count
        );
        AssertTrue(
            highPassManager.currentTargetHex == hexgrid.GetHexCellAt(new Vector3Int (4, 0, 4)),
            "HP target is the key pressed",
            hexgrid.GetHexCellAt(new Vector3Int (4, 0, 4)),
            highPassManager.currentTargetHex
        );
        Log("Click On (4, 4) - Confirm HP Target");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(4, 4), 0.5f));
        // AssertTrue(
        //     highPassManager.eligibleAttackers.Count == 2,
        //     "HP target is has 2 eligible Attacker",
        //     2,
        //     highPassManager.eligibleAttackers.Count
        // );
        AssertTrue(
            highPassManager.currentTargetHex == hexgrid.GetHexCellAt(new Vector3Int (4, 0, 4)),
            "HP target 4, 4 Nazef",
            hexgrid.GetHexCellAt(new Vector3Int (4, 0, 4)),
            highPassManager.currentTargetHex
        );
        AssertTrue(
            highPassManager.intendedTargetHex == hexgrid.GetHexCellAt(new Vector3Int (4, 0, 4)),
            "HP confrimed target is 4, 4 Nazef",
            hexgrid.GetHexCellAt(new Vector3Int (4, 0, 4)),
            highPassManager.intendedTargetHex
        );
        AssertTrue(
            highPassManager.isWaitingForAttackerSelection,
            "HP is wating for attacker to be selected",
            true,
            highPassManager.isWaitingForAttackerSelection
        );
        AssertTrue(
            highPassManager.lockedAttacker == PlayerToken.GetPlayerTokenByName("Nazef"),
            "HP has locked Nazef",
            PlayerToken.GetPlayerTokenByName("Nazef"),
            highPassManager.lockedAttacker
        );
        Log("Click On (0, 0) - Select Cafferata");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(0, 0), 0.5f));
        AssertTrue(
            highPassManager.isWaitingForAttackerSelection,
            "HP is wating for attacker to be selected",
            true,
            highPassManager.isWaitingForAttackerSelection
        );
        AssertTrue(
            highPassManager.isWaitingForAttackerMove,
            "HP is wating for attacker to move",
            true,
            highPassManager.isWaitingForAttackerMove
        );
        Log("Click On (-1, -1) - Select Cafferata");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(-1, -1), 0.5f));
        yield return new WaitForSeconds(1f);
        AssertTrue(
            !highPassManager.isWaitingForAttackerSelection,
            "HP is NOT wating for attacker to be selected",
            false,
            highPassManager.isWaitingForAttackerSelection
        );
        AssertTrue(
            !highPassManager.isWaitingForAttackerMove,
            "HP is NOT wating for attacker to move",
            false,
            highPassManager.isWaitingForAttackerMove
        );
        AssertTrue(
            highPassManager.isWaitingForDefenderSelection,
            "HP is wating for defender to be selected",
            true,
            highPassManager.isWaitingForDefenderSelection
        );
        AssertTrue(
            !highPassManager.isWaitingForDefenderMove,
            "HP is NOT wating for defender to move",
            false,
            highPassManager.isWaitingForDefenderMove
        );
        Log("Click On (1, 2) - Select Vladoiu");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(1, 2), 0.5f));
        AssertTrue(
            !highPassManager.isWaitingForAttackerSelection,
            "HP is NOT wating for attacker to be selected",
            false,
            highPassManager.isWaitingForAttackerSelection
        );
        AssertTrue(
            !highPassManager.isWaitingForAttackerMove,
            "HP is NOT wating for attacker to move",
            false,
            highPassManager.isWaitingForAttackerMove
        );
        AssertTrue(
            highPassManager.isWaitingForDefenderSelection,
            "HP is wating for defender to be selected",
            true,
            highPassManager.isWaitingForDefenderSelection
        );
        AssertTrue(
            highPassManager.isWaitingForDefenderMove,
            "HP is wating for defender to move",
            false,
            highPassManager.isWaitingForDefenderMove
        );
        Log("Click On (1, 3) - Move Vladoiu");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(1, 3), 0.5f));
        yield return new WaitForSeconds(1.5f);
        AssertTrue(
            !highPassManager.isWaitingForAttackerSelection,
            "HP is NOT wating for attacker to be selected",
            false,
            highPassManager.isWaitingForAttackerSelection
        );
        AssertTrue(
            !highPassManager.isWaitingForAttackerMove,
            "HP is NOT wating for attacker to move",
            false,
            highPassManager.isWaitingForAttackerMove
        );
        AssertTrue(
            !highPassManager.isWaitingForDefenderSelection,
            "HP is NOT wating for defender to be selected",
            false,
            highPassManager.isWaitingForDefenderSelection
        );
        AssertTrue(
            !highPassManager.isWaitingForDefenderMove,
            "HP is NOT wating for defender to move",
            false,
            highPassManager.isWaitingForDefenderMove
        );
        AssertTrue(
            highPassManager.isWaitingForAccuracyRoll,
            "HP is waiting for accuracy Roll",
            true,
            highPassManager.isWaitingForAccuracyRoll
        );
        Log("Pressing R for Accuracy");
        highPassManager.PerformAccuracyRoll(1);
        yield return new WaitForSeconds(0.2f);
        AssertTrue(
            !highPassManager.isWaitingForAccuracyRoll,
            "HP is NO longer waiting for accuracy Roll",
            false,
            highPassManager.isWaitingForAccuracyRoll
        );
        AssertTrue(
            highPassManager.isWaitingForDirectionRoll,
            "HP is waiting for direction Roll",
            true,
            highPassManager.isWaitingForDirectionRoll
        );
        yield return new WaitForSeconds(0.5f);
        highPassManager.PerformDirectionRoll(5);
        yield return new WaitForSeconds(0.2f);
        AssertTrue(
            !highPassManager.isWaitingForDirectionRoll,
            "HP is NO longer waiting for direction Roll",
            false,
            highPassManager.isWaitingForDirectionRoll
        );
        AssertTrue(
            highPassManager.isWaitingForDistanceRoll,
            "HP is waiting for distance Roll",
            true,
            highPassManager.isWaitingForDistanceRoll
        );
        yield return new WaitForSeconds(0.5f);
        highPassManager.PerformDistanceRoll(1);
        yield return new WaitForSeconds(0.2f);
        AssertTrue(
            !highPassManager.isWaitingForDistanceRoll,
            "HP is NO Longer waiting for distance Roll",
            true,
            highPassManager.isWaitingForDistanceRoll
        );
        yield return new WaitForSeconds(3.5f);
        AssertTrue(
            !highPassManager.isActivated,
            "HP is NO activated",
            true,
            highPassManager.isActivated
        );
        AssertTrue(
            headerManager.isActivated,
            "Header Manager is activated",
            true,
            headerManager.isActivated
        );
        // yield return new WaitForSeconds(0.2f);
        // AssertTrue(
        //     finalThirdManager.isActivated,
        //     "F3 should be activated",
        //     true,
        //     finalThirdManager.isActivated
        // );
        // Log("Pressing X - Forfeit Def F3");
        // yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        // yield return new WaitForSeconds(0.2f);
        AssertTrue(
            !finalThirdManager.isActivated,
            "F3 should no longer be activated",
            false,
            finalThirdManager.isActivated
        );


        LogFooterofTest("High Pass on Attacker, Move Passer, defender and INAccurate HP.");
    }

    private IEnumerator Scenario_027a_Decide_on_attWillJump(bool addKalla = false)
    {
        yield return Scenario_027_HighPass_onAttacker_MoveAtt_moveDef_INAccurateHP();

        Log("▶️ Starting test scenario: hasEligibleAtt & hasEligibleDef: Decide on who will jump");
        AssertTrue(
            headerManager.isActivated,
            "Header Manager is activated",
            true,
            headerManager.isActivated
        );
        AssertTrue(
            headerManager.isWaitingForAttackerSelection,
            "Header Manager Should be waiting for Attacker selection",
            true,
            headerManager.isWaitingForAttackerSelection
        );
        AssertTrue(
            headerManager.attEligibleToHead.Contains(PlayerToken.GetPlayerTokenByName("Nazef")),
            "Header Manager Nazef is Eligible"
        );
        AssertTrue(
            headerManager.attEligibleToHead.Contains(PlayerToken.GetPlayerTokenByName("Kalla")),
            "Header Manager Kalla is Eligible"
        );
        AssertTrue(
            headerManager.defEligibleToHead.Count == 4,
            "Header Manager defEligibleToHead has Tokens (Gilbert, Paterson, Stewart, McNulty)",
            4,
            headerManager.defEligibleToHead.Count
        );
        Log("Click On (-4, -4) - Nominate Pavlovic, LOL");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(-4, -4), 0.5f));
        yield return new WaitForSeconds(0.2f);
        AssertTrue(
            headerManager.isWaitingForAttackerSelection,
            "Header Manager Should be waiting for Attacker selection",
            true,
            headerManager.isWaitingForAttackerSelection
        );
        AssertTrue(
            headerManager.attEligibleToHead.Contains(PlayerToken.GetPlayerTokenByName("Nazef")),
            "Header Manager Nazef is Eligible"
        );
        AssertTrue(
            headerManager.attEligibleToHead.Contains(PlayerToken.GetPlayerTokenByName("Kalla")),
            "Header Manager Kalla is Eligible"
        );
        AssertTrue(
            headerManager.defEligibleToHead.Count == 4,
            "Header Manager defEligibleToHead has Tokens (Gilbert, Paterson, Stewart, McNulty)",
            4,
            headerManager.defEligibleToHead.Count
        );
        Log("Click On (4, 4) - Nominate Nazef");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(4, 4), 0.5f));
        yield return new WaitForSeconds(0.2f);
        AssertTrue(
            headerManager.isWaitingForAttackerSelection,
            "Header Manager Should be waiting for Attacker selection",
            true,
            headerManager.isWaitingForAttackerSelection
        );
        AssertTrue(
            headerManager.attEligibleToHead.Contains(PlayerToken.GetPlayerTokenByName("Nazef")),
            "Header Manager Nazef is Eligible"
        );
        AssertTrue(
            headerManager.attackerWillJump.Contains(PlayerToken.GetPlayerTokenByName("Nazef")),
            "Header Manager Nazef is Eligible"
        );
        AssertTrue(
            headerManager.attEligibleToHead.Contains(PlayerToken.GetPlayerTokenByName("Kalla")),
            "Header Manager Kalla is Nominated to Jump"
        );
        AssertTrue(
            headerManager.defEligibleToHead.Count == 4,
            "Header Manager defEligibleToHead has Tokens (Gilbert, Paterson, Stewart, McNulty)",
            4,
            headerManager.defEligibleToHead.Count
        );
        Log("Click On (4, 4) - DeNominate Nazef");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(4, 4), 0.5f));
        yield return new WaitForSeconds(0.2f);
        AssertTrue(
            headerManager.isWaitingForAttackerSelection,
            "Header Manager Should be waiting for Attacker selection",
            true,
            headerManager.isWaitingForAttackerSelection
        );
        AssertTrue(
            headerManager.attEligibleToHead.Contains(PlayerToken.GetPlayerTokenByName("Nazef")),
            "Header Manager Nazef is Eligible"
        );
        AssertTrue(
            headerManager.attEligibleToHead.Contains(PlayerToken.GetPlayerTokenByName("Kalla")),
            "Header Manager Kalla is Eligible"
        );
        AssertTrue(
            !headerManager.attackerWillJump.Contains(PlayerToken.GetPlayerTokenByName("Nazef")),
            "Header Manager Nazef is NOT Nominated to Jump"
        );
        AssertTrue(
            headerManager.defEligibleToHead.Count == 4,
            "Header Manager defEligibleToHead has Tokens (Gilbert, Paterson, Stewart, McNulty)",
            4,
            headerManager.defEligibleToHead.Count
        );
        Log("Click On (4, 4) - Re Nominate Nazef");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(4, 4), 0.5f));
        yield return new WaitForSeconds(0.2f);
        AssertTrue(
            headerManager.isWaitingForAttackerSelection,
            "Header Manager Should be waiting for Attacker selection",
            true,
            headerManager.isWaitingForAttackerSelection
        );
        AssertTrue(
            headerManager.attEligibleToHead.Contains(PlayerToken.GetPlayerTokenByName("Nazef")),
            "Header Manager Nazef is Eligible"
        );
        AssertTrue(
            headerManager.attackerWillJump.Contains(PlayerToken.GetPlayerTokenByName("Nazef")),
            "Header Manager Nazef is Eligible"
        );
        AssertTrue(
            headerManager.attEligibleToHead.Contains(PlayerToken.GetPlayerTokenByName("Kalla")),
            "Header Manager Kalla is Nominated to Jump"
        );
        AssertTrue(
            headerManager.defEligibleToHead.Count == 4,
            "Header Manager defEligibleToHead has Tokens (Gilbert, Paterson, Stewart, McNulty)",
            4,
            headerManager.defEligibleToHead.Count
        );
        if (addKalla)
        {
            Log("Click On (6, 6) - Nominate Kalla too");
            yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(6, 6), 0.5f));
            yield return new WaitForSeconds(0.2f);
        }
        AssertTrue(
            headerManager.isWaitingForAttackerSelection,
            "Header Manager Should be waiting for Attacker selection",
            true,
            headerManager.isWaitingForAttackerSelection
        );
        AssertTrue(
            !headerManager.isWaitingForDefenderSelection,
            "Header Manager Should NOT be waiting for defender selection as Attack has not confirmed",
            true,
            headerManager.isWaitingForDefenderSelection
        );
        AssertTrue(
            headerManager.attEligibleToHead.Contains(PlayerToken.GetPlayerTokenByName("Nazef")),
            "Header Manager Nazef is Eligible"
        );
        AssertTrue(
            headerManager.attEligibleToHead.Contains(PlayerToken.GetPlayerTokenByName("Kalla")),
            "Header Manager Kalla isEligible"
        );
        AssertTrue(
            headerManager.attackerWillJump.Contains(PlayerToken.GetPlayerTokenByName("Nazef")),
            "Header Manager Nazef is Nominated"
        );
        if (addKalla)
        {
        AssertTrue(
            headerManager.attackerWillJump.Contains(PlayerToken.GetPlayerTokenByName("Kalla")),
            "Header Manager Kalla is Nominated to jump"
        );
        }
        AssertTrue(
            headerManager.defEligibleToHead.Count == 4,
            "Header Manager defEligibleToHead has Tokens (Gilbert, Paterson, Stewart, McNulty)",
            4,
            headerManager.defEligibleToHead.Count
        );
        Log("Pressing Enter - Confirm Attackers");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.KeypadEnter, 0.1f));
        yield return new WaitForSeconds(0.2f);
        AssertTrue(
            !headerManager.isWaitingForAttackerSelection,
            "Header Manager Should NO LONGER be waiting for Attacker selection",
            false,
            headerManager.isWaitingForAttackerSelection
        );
        AssertTrue(
            headerManager.isWaitingForDefenderSelection,
            "Header Manager Should now be waiting for defender selection",
            true,
            headerManager.isWaitingForDefenderSelection
        );

        LogFooterofTest("hasEligibleAtt & hasEligibleDef: Decide on who will jump");
    }

    private IEnumerator Scenario_027_a_b_HP_on_1att_def_Not_challenging(bool addKalla)
    {
        yield return new WaitForSeconds(1.5f); // Allow scene to stabilize
        Log("▶️ Starting test scenario: High Pass on Attacker, Move Passer, defender and INAccurate HP. Attack challenges with 2 players. Defense Does not jump with any of the 4 eligible");
        yield return StartCoroutine(Scenario_027a_Decide_on_attWillJump(addKalla));
        Log("Press Enter - Defense not challenging");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.KeypadEnter, 0.5f));
        yield return new WaitForSeconds(0.5f);
        AssertTrue(
            headerManager.isWaitingForControlOrHeaderDecision,
            "Header should be waiting for choice between header or BC",
            true,
            headerManager.isWaitingForControlOrHeaderDecision
        );

        LogFooterofTest("High Pass on Attacker, Move Passer, defender and INAccurate HP. Attack challenges with 2 players. Defense Does not jump with any of the 4 eligible");
    }

    private IEnumerator Scenario_027_a_c_HP_on_1att_def_Not_challenging_att_head(bool addKalla = false)
    {
        yield return new WaitForSeconds(1.5f); // Allow scene to stabilize
        Log("▶️ Starting test scenario: High Pass on Attacker, Move Passer, defender and INAccurate HP. Attack challenges with 2 players. Defense Does not jump with any of the 4 , Attack Heads");
        yield return StartCoroutine(Scenario_027_a_b_HP_on_1att_def_Not_challenging(addKalla));
        Log("Press H - Attack decides to Head");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.H, 0.5f));
        yield return new WaitForSeconds(0.5f);
        AssertTrue(
            !headerManager.isWaitingForControlOrHeaderDecision,
            "Header should NOT be waiting for choice between header or BC",
            false,
            headerManager.isWaitingForControlOrHeaderDecision
        );
        AssertTrue(
            headerManager.isWaitingForHeaderTargetSelection,
            "Header should be waiting for header Target",
            true,
            headerManager.isWaitingForHeaderTargetSelection
        );
        if (addKalla)
        {
            AssertTrue(
                headerManager.attackerWillJump.Count == 2,
                "Header both Kalla and Nazef should be jumping",
                2,
                headerManager.attackerWillJump.Count
            );
            AssertTrue(
                headerManager.challengeWinner == PlayerToken.GetPlayerTokenByName("Nazef"),
                "Header Auto selection of Nazef for header",
                PlayerToken.GetPlayerTokenByName("Nazef"),
                headerManager.challengeWinner
            );
        }
        else
        {
            AssertTrue(
                headerManager.attackerWillJump.Count == 1,
                "Header only Nazef should be jumping",
                1,
                headerManager.attackerWillJump.Count
            );
            AssertTrue(
                headerManager.challengeWinner == PlayerToken.GetPlayerTokenByName("Nazef"),
                "Header Auto selection of Nazef for header",
                PlayerToken.GetPlayerTokenByName("Nazef"),
                headerManager.challengeWinner
            );
        }

        LogFooterofTest("High Pass on Attacker, Move Passer, defender and INAccurate HP. Attack challenges with 2 players. Defense Does not jump with any of the 4 , Attack Heads");
    }

    private IEnumerator Scenario_027_a_d_HP_on_1att_def_Not_challenging_att_BC(bool addKalla = false)
    {
        yield return new WaitForSeconds(1.5f); // Allow scene to stabilize
        Log("▶️ Starting test scenario: High Pass on Attacker, Move Passer, defender and INAccurate HP. Attack challenges with 2 players. Defense Does not jump with any of the 4 , Attack BC with Nazef");
        yield return StartCoroutine(Scenario_027_a_b_HP_on_1att_def_Not_challenging(addKalla));
        Log("Press B - Attack decides to Ball Control");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.B, 0.5f));
        yield return new WaitForSeconds(0.5f);
        AssertTrue(
            !headerManager.isWaitingForControlOrHeaderDecision,
            "Header should NOT be waiting for choice between header or BC",
            false,
            headerManager.isWaitingForControlOrHeaderDecision
        );
        if (addKalla)
        {
            AssertTrue(
                headerManager.iswaitingForChallengeWinnerSelection,
                "Header waiting for who will contol ball",
                true,
                headerManager.iswaitingForChallengeWinnerSelection
            );
            AssertTrue(
                headerManager.attackerWillJump.Count == 2,
                "Header both Kalla and Nazef should be jumping",
                2,
                headerManager.attackerWillJump.Count
            );
            AssertTrue(
                headerManager.iswaitingForChallengeWinnerSelection,
                "Header waiting for who will contol ball",
                true,
                headerManager.iswaitingForChallengeWinnerSelection
            );
            Log("Click On (4, 4) - Nazef will attempt to Control");
            yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(4, 4), 0.5f));
            yield return new WaitForSeconds(0.2f);
        }
        else
        {
            yield return new WaitForSeconds(1.2f);
            AssertTrue(
                headerManager.attackerWillJump.Count == 1,
                "Header only Nazef should be jumping",
                1,
                headerManager.attackerWillJump.Count
            );
            AssertTrue(
                headerManager.challengeWinner == PlayerToken.GetPlayerTokenByName("Nazef"),
                "Header Auto selection of Nazef for header",
                PlayerToken.GetPlayerTokenByName("Nazef"),
                headerManager.challengeWinner
            );
        }
        AssertTrue(
            !headerManager.iswaitingForChallengeWinnerSelection,
            "Header SHOULD NOT BE waiting for who will contol ball as Nazef was declared or autoselected",
            false,
            headerManager.iswaitingForChallengeWinnerSelection
        );
        AssertTrue(
            headerManager.isWaitingForControlRoll,
            "Header waiting for Ball Control Roll",
            true,
            headerManager.isWaitingForControlRoll
        );

        LogFooterofTest("High Pass on Attacker, Move Passer, defender and INAccurate HP. Attack challenges with 2 players. Defense Does not jump with any of the 4 , Attack BC with Nazef");
    }
    
    private IEnumerator Scenario_027b_Decide_on_DefWillJump(bool addKalla = false)
    {
        yield return Scenario_027a_Decide_on_attWillJump(addKalla);

        Log("▶️ Starting test scenario: hasEligibleAtt & hasEligibleDef: Decide on who will jump (DEF)");
        Log("Click On (3, 3) - Nominate Gilbert");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(3, 3), 0.5f));
        yield return new WaitForSeconds(0.2f);
        AssertTrue(
            !headerManager.isWaitingForAttackerSelection,
            "Header Manager Should NOT be waiting for Attacker selection",
            false,
            headerManager.isWaitingForAttackerSelection
        );
        AssertTrue(
            headerManager.isWaitingForDefenderSelection,
            "Header Manager Should be waiting for defender selection",
            true,
            headerManager.isWaitingForDefenderSelection
        );
        AssertTrue(
            headerManager.attackerWillJump.Contains(PlayerToken.GetPlayerTokenByName("Nazef")),
            "Header Manager Nazef is Nominated to jump"
        );
        if (addKalla)
        {
            AssertTrue(
                headerManager.attackerWillJump.Contains(PlayerToken.GetPlayerTokenByName("Kalla")),
                "Header Manager Kalla is Nominated to jump"
            );
        }
        AssertTrue(
            headerManager.defenderWillJump.Contains(PlayerToken.GetPlayerTokenByName("Gilbert")),
            "Header Manager Kalla is Nominated to jump"
        );
        AssertTrue(
            headerManager.defEligibleToHead.Count == 4,
            "Header Manager defEligibleToHead has Tokens (Gilbert, Paterson, Stewart, McNulty)",
            4,
            headerManager.defEligibleToHead.Count
        );
        Log("Click On (1, 3) - Reject Nominate Vladoiu");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(1, 3), 0.5f));
        yield return new WaitForSeconds(0.2f);
        AssertTrue(
            !headerManager.isWaitingForAttackerSelection,
            "Header Manager Should NOT be waiting for Attacker selection",
            false,
            headerManager.isWaitingForAttackerSelection
        );
        AssertTrue(
            headerManager.isWaitingForDefenderSelection,
            "Header Manager Should be waiting for defender selection",
            true,
            headerManager.isWaitingForDefenderSelection
        );
        AssertTrue(
            headerManager.attackerWillJump.Contains(PlayerToken.GetPlayerTokenByName("Nazef")),
            "Header Manager Nazef is Nominated to jump"
        );
        if (addKalla)
        {
            AssertTrue(
                headerManager.attackerWillJump.Contains(PlayerToken.GetPlayerTokenByName("Kalla")),
                "Header Manager Kalla is Nominated to jump"
            );
        }
        AssertTrue(
            headerManager.defenderWillJump.Contains(PlayerToken.GetPlayerTokenByName("Gilbert")),
            "Header Manager Gilbert is Nominated to jump"
        );
        AssertTrue(
            !headerManager.defenderWillJump.Contains(PlayerToken.GetPlayerTokenByName("Vladoiu")),
            "Header Manager Vladoiu's nomination was rejected"
        );
        AssertTrue(
            headerManager.defEligibleToHead.Count == 4,
            "Header Manager defEligibleToHead has Tokens (Gilbert, Paterson, Stewart, McNulty)",
            4,
            headerManager.defEligibleToHead.Count
        );
        Log("Click On (3, 3) - DeNominate Gilbert");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(3, 3), 0.5f));
        yield return new WaitForSeconds(0.2f);
        AssertTrue(
            !headerManager.isWaitingForAttackerSelection,
            "Header Manager Should NOT be waiting for Attacker selection",
            false,
            headerManager.isWaitingForAttackerSelection
        );
        AssertTrue(
            headerManager.isWaitingForDefenderSelection,
            "Header Manager Should be waiting for defender selection",
            true,
            headerManager.isWaitingForDefenderSelection
        );
        AssertTrue(
            headerManager.attackerWillJump.Contains(PlayerToken.GetPlayerTokenByName("Nazef")),
            "Header Manager Nazef is Nominated to jump"
        );
        if (addKalla)
        {
            AssertTrue(
                headerManager.attackerWillJump.Contains(PlayerToken.GetPlayerTokenByName("Kalla")),
                "Header Manager Kalla is Nominated to jump"
            );
        }
        AssertTrue(
            !headerManager.defenderWillJump.Contains(PlayerToken.GetPlayerTokenByName("Gilbert")),
            "Header Manager Gilbert is deNominated to jump"
        );
        AssertTrue(
            !headerManager.defenderWillJump.Contains(PlayerToken.GetPlayerTokenByName("Vladoiu")),
            "Header Manager Vladoiu's nomination was rejected"
        );
        AssertTrue(
            headerManager.defEligibleToHead.Count == 4,
            "Header Manager defEligibleToHead has Tokens (Gilbert, Paterson, Stewart, McNulty)",
            4,
            headerManager.defEligibleToHead.Count
        );
        Log("Click On (5, 5) - Nominate McNulty");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(5, 5), 0.5f));
        yield return new WaitForSeconds(0.2f);
        AssertTrue(
            !headerManager.isWaitingForAttackerSelection,
            "Header Manager Should NOT be waiting for Attacker selection",
            false,
            headerManager.isWaitingForAttackerSelection
        );
        AssertTrue(
            headerManager.isWaitingForDefenderSelection,
            "Header Manager Should be waiting for defender selection",
            true,
            headerManager.isWaitingForDefenderSelection
        );
        AssertTrue(
            headerManager.attackerWillJump.Contains(PlayerToken.GetPlayerTokenByName("Nazef")),
            "Header Manager Nazef is Nominated to jump"
        );
        if (addKalla)
        {
            AssertTrue(
                headerManager.attackerWillJump.Contains(PlayerToken.GetPlayerTokenByName("Kalla")),
                "Header Manager Kalla is Nominated to jump"
            );
        }
        AssertTrue(
            headerManager.defenderWillJump.Contains(PlayerToken.GetPlayerTokenByName("McNulty")),
            "Header Manager McNulty is Nominated to jump"
        );
        AssertTrue(
            !headerManager.defenderWillJump.Contains(PlayerToken.GetPlayerTokenByName("Vladoiu")),
            "Header Manager Vladoiu's nomination was rejected"
        );
        AssertTrue(
            headerManager.defEligibleToHead.Count == 4,
            "Header Manager defEligibleToHead has Tokens (Gilbert, Paterson, Stewart, McNulty)",
            4,
            headerManager.defEligibleToHead.Count
        );
        Log("Click On (4, 5) - Nominate Stewart");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(4, 5), 0.5f));
        yield return new WaitForSeconds(0.2f);
        AssertTrue(
            !headerManager.isWaitingForAttackerSelection,
            "Header Manager Should NOT be waiting for Attacker selection",
            false,
            headerManager.isWaitingForAttackerSelection
        );
        AssertTrue(
            headerManager.isWaitingForDefenderSelection,
            "Header Manager Should be waiting for defender selection",
            true,
            headerManager.isWaitingForDefenderSelection
        );
        AssertTrue(
            headerManager.attackerWillJump.Contains(PlayerToken.GetPlayerTokenByName("Nazef")),
            "Header Manager Nazef is Nominated to jump"
        );
        if (addKalla)
        {
            AssertTrue(
                headerManager.attackerWillJump.Contains(PlayerToken.GetPlayerTokenByName("Kalla")),
                "Header Manager Kalla is Nominated to jump"
            );
        }
        AssertTrue(
            headerManager.defenderWillJump.Contains(PlayerToken.GetPlayerTokenByName("McNulty")),
            "Header Manager McNulty is Nominated to jump"
        );
        AssertTrue(
            headerManager.defenderWillJump.Contains(PlayerToken.GetPlayerTokenByName("Stewart")),
            "Header Manager Stewart is Nominated to jump"
        );
        AssertTrue(
            !headerManager.defenderWillJump.Contains(PlayerToken.GetPlayerTokenByName("Vladoiu")),
            "Header Manager Vladoiu's nomination was rejected"
        );
        AssertTrue(
            headerManager.defEligibleToHead.Count == 4,
            "Header Manager defEligibleToHead has Tokens (Gilbert, Paterson, Stewart, McNulty)",
            4,
            headerManager.defEligibleToHead.Count
        );
        Log("Pressing Enter - Confirm Defenders");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.KeypadEnter, 0.1f));
        yield return new WaitForSeconds(0.3f);
        AssertTrue(
            !headerManager.isWaitingForAttackerSelection,
            "Header Manager Should NO LONGER be waiting for Attacker selection",
            false,
            headerManager.isWaitingForAttackerSelection
        );
        AssertTrue(
            !headerManager.isWaitingForDefenderSelection,
            "Header Manager Should NO LONGER be waiting for defender selection",
            false,
            headerManager.isWaitingForDefenderSelection
        );
        AssertTrue(
            headerManager.isWaitingForHeaderRoll,
            "Header Manager Should be waiting for HeaderRolls",
            true,
            headerManager.isWaitingForHeaderRoll
        );
        yield return new WaitForSeconds(0.8f);
        
        LogFooterofTest("hasEligibleAtt & hasEligibleDef: Decide on who will jump (DEF)");
    }

    private IEnumerator Scenario_027c_4PlayerJump_AttackWins()
    {
        yield return Scenario_027b_Decide_on_DefWillJump();

        Log("▶️ Starting test scenario: 4 players Jump - Attack Wins, 2 failed Interceptions header to space");
        Log("Rolling 1st Attacker");
        headerManager.PerformHeaderRoll(6);
        yield return new WaitForSeconds(0.5f);
        // Log("Rolling 2nd Attacker");
        // headerManager.PerformHeaderRoll(4);
        // yield return new WaitForSeconds(0.5f);
        Log("Rolling 1st Defender");
        headerManager.PerformHeaderRoll(2);
        yield return new WaitForSeconds(0.5f);
        Log("Rolling 2nd Defender");
        headerManager.PerformHeaderRoll(2);
        yield return new WaitForSeconds(0.5f);
        AssertTrue(
            headerManager.isWaitingForHeaderTargetSelection,
            "Header Manager Should be waiting for HeaderTargetSelection",
            true,
            headerManager.isWaitingForHeaderTargetSelection
        );
        Log("Click On (2, 3) - Send ball next to Vladoiu");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(2, 3), 0.5f));
        yield return new WaitForSeconds(2.2f); // ball moving
        AssertTrue(
            headerManager.isWaitingForInterceptionRoll,
            "Header Manager Should be waiting for InterceptionRoll",
            true,
            headerManager.isWaitingForInterceptionRoll
        );
        Log("Rolling Vladoiu's Interception");
        headerManager.PerformInterceptionRoll(2);
        yield return new WaitForSeconds(2.2f);
        AssertTrue(
            headerManager.isWaitingForInterceptionRoll,
            "Header Manager Should be waiting for InterceptionRoll",
            true,
            headerManager.isWaitingForInterceptionRoll
        );
        Log("Rolling Gilbert's Interception");
        headerManager.PerformInterceptionRoll(2);
        yield return new WaitForSeconds(2.2f);

        LogFooterofTest("4 players Jump - Attack Wins, 2 failed Interceptions header to space");
    }

    private IEnumerator Scenario_027d_4PlayerJump_Defense_Wins_to_player()
    {
        yield return Scenario_027b_Decide_on_DefWillJump();

        Log("▶️ Starting test scenario: 4 players Jump - Defense Wins, Header To Player");
        Log("Rolling 1st Attacker");
        headerManager.PerformHeaderRoll(1);
        yield return new WaitForSeconds(0.5f);
        // Log("Rolling 2nd Attacker");
        // headerManager.PerformHeaderRoll(1);
        // yield return new WaitForSeconds(0.5f);
        Log("Rolling 1st Defender");
        headerManager.PerformHeaderRoll(6);
        yield return new WaitForSeconds(0.5f);
        Log("Rolling 2nd Defender");
        headerManager.PerformHeaderRoll(6);
        yield return new WaitForSeconds(0.5f);
        AssertTrue(
            headerManager.isWaitingForHeaderTargetSelection,
            "Header Manager Should be waiting for HeaderTargetSelection",
            true,
            headerManager.isWaitingForHeaderTargetSelection
        );
        AssertTrue(
            MatchManager.Instance.LastTokenToTouchTheBallOnPurpose == PlayerToken.GetPlayerTokenByName("McNulty"),
            "Header Manager should have killed itself by now",
            PlayerToken.GetPlayerTokenByName("McNulty").name,
            MatchManager.Instance.LastTokenToTouchTheBallOnPurpose.name
        );
        Log("Click On (1, 3) - Send ball to Vladoiu");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(1, 3), 0.5f));
        yield return new WaitForSeconds(2.2f); // ball moving
        AssertTrue(
            !headerManager.isActivated,
            "Header Manager should have killed itself by now",
            false,
            headerManager.isActivated
        );
        AssertTrue(
            MatchManager.Instance.LastTokenToTouchTheBallOnPurpose == PlayerToken.GetPlayerTokenByName("Vladoiu"),
            "Vladoiu Last one to touch teh ball",
            PlayerToken.GetPlayerTokenByName("Vladoiu").name,
            MatchManager.Instance.LastTokenToTouchTheBallOnPurpose.name
        );
        AvailabilityCheckResult availabilityCheck = AssertCorrectAvailabilityAfterHeadToPlayer();
        AssertTrue(
            availabilityCheck.passed,
            "Action Availability after Head to Player",
            true,
            availabilityCheck.ToString()
        );

        LogFooterofTest("4 players Jump - Defense Wins, Header To Player");
    }
    
    private IEnumerator Scenario_027e_4PlayerJump_Defense_Wins_to_space()
    {
        yield return Scenario_027b_Decide_on_DefWillJump();

        Log("▶️ Starting test scenario: 4 players Jump - Defense Wins, Header To Player");
        Log("Rolling 1st Attacker");
        headerManager.PerformHeaderRoll(1);
        yield return new WaitForSeconds(0.5f);
        // Log("Rolling 2nd Attacker");
        // headerManager.PerformHeaderRoll(1);
        // yield return new WaitForSeconds(0.5f);
        Log("Rolling 1st Defender");
        headerManager.PerformHeaderRoll(6);
        yield return new WaitForSeconds(0.5f);
        Log("Rolling 2nd Defender");
        headerManager.PerformHeaderRoll(6);
        yield return new WaitForSeconds(0.5f);
        AssertTrue(
            headerManager.isWaitingForHeaderTargetSelection,
            "Header Manager Should be waiting for HeaderTargetSelection",
            true,
            headerManager.isWaitingForHeaderTargetSelection
        );
        AssertTrue(
            MatchManager.Instance.LastTokenToTouchTheBallOnPurpose == PlayerToken.GetPlayerTokenByName("McNulty"),
            "Header Manager should have killed itself by now",
            PlayerToken.GetPlayerTokenByName("McNulty").name,
            MatchManager.Instance.LastTokenToTouchTheBallOnPurpose.name
        );
        Log("Click On (11, 3) - Send ball to space");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(11, 3), 0.5f));
        yield return new WaitForSeconds(3.2f); // ball moving
        AssertTrue(
            !headerManager.isActivated,
            "Header Manager should have killed itself by now",
            false,
            headerManager.isActivated
        );
        AssertTrue(
            MatchManager.Instance.LastTokenToTouchTheBallOnPurpose == PlayerToken.GetPlayerTokenByName("McNulty"),
            "Last Player to touch the ball should still be McNulty",
            PlayerToken.GetPlayerTokenByName("McNulty").name,
            MatchManager.Instance.LastTokenToTouchTheBallOnPurpose.name
        );
        AvailabilityCheckResult availabilityCheck = AssertCorrectAvailabilityAfterGBToSpace();
        AssertTrue(
            availabilityCheck.passed,
            "Action Availability after Interception (Any Other Scenario)",
            true,
            availabilityCheck.ToString()
        );

        LogFooterofTest("4 players Jump - Defense Wins, Header To Player");
    }

    private IEnumerator Scenario_027f_4PlayerJump_LooseBall(bool addKalla = false)
    {
        yield return Scenario_027b_Decide_on_DefWillJump(addKalla);

        Log("▶️ Starting test scenario: 4 players Jump - Loose Ball from header");
        Log("Rolling 1st Attacker - Nazef");
        headerManager.PerformHeaderRoll(3);
        yield return new WaitForSeconds(0.5f);
        if (addKalla)
        {
            Log("Rolling 2nd Attacker - Kalla");
            headerManager.PerformHeaderRoll(2);
            yield return new WaitForSeconds(0.5f);
        }
        Log("Rolling 1st Defender - McNulty");
        headerManager.PerformHeaderRoll(1);
        yield return new WaitForSeconds(0.5f);
        Log("Rolling 2nd Defender - Stewart");
        headerManager.PerformHeaderRoll(4);
        yield return new WaitForSeconds(1.9f);
        AssertTrue(
            !headerManager.isActivated,
            "Header Manager Should have killed itself by now",
            false,
            headerManager.isActivated
        );
        AssertTrue(
            looseBallManager.isActivated,
            "Loose ball Should be awake by now",
            true,
            looseBallManager.isActivated
        );
        AssertTrue(
            looseBallManager.isWaitingForDirectionRoll,
            "Loose ball Should be waiting for a direction Roll",
            true,
            looseBallManager.isWaitingForDirectionRoll
        );
        AssertTrue(
            MatchManager.Instance.LastTokenToTouchTheBallOnPurpose == PlayerToken.GetPlayerTokenByName("Cafferata"),
            "Last player to touch the ball is still the HPasser",
            PlayerToken.GetPlayerTokenByName("Cafferata").name,
            MatchManager.Instance.LastTokenToTouchTheBallOnPurpose.name
        );

        LogFooterofTest("4 players Jump - Loose Ball from header");
    }

    private IEnumerator Scenario_027g_4PlayerJump_LooseBall_From_Stewart_Space()
    {
        yield return Scenario_027f_4PlayerJump_LooseBall();
        Log("▶️ Starting test scenario: 4 players Jump - Loose Ball from Stewart to Space");
        AssertTrue(
            looseBallManager.isWaitingForDirectionRoll,
            "Loose ball Should be waiting for a direction Roll",
            true,
            looseBallManager.isWaitingForDirectionRoll
        );
        Log("Rolling for Direction - SouthWest");
        looseBallManager.PerformDirectionRoll(2);
        yield return new WaitForSeconds(0.5f);
        AssertTrue(
            !looseBallManager.isWaitingForDirectionRoll,
            "Loose ball Should NOT be waiting for a direction Roll",
            false,
            looseBallManager.isWaitingForDirectionRoll
        );
        AssertTrue(
            looseBallManager.isWaitingForDistanceRoll,
            "Loose ball Should be waiting for a distance Roll",
            true,
            looseBallManager.isWaitingForDistanceRoll
        );
        Log("Rolling for Distance - 4");
        yield return new WaitForSeconds(0.5f);
        looseBallManager.PerformDistanceRoll(4);
        AssertTrue(
            !looseBallManager.isWaitingForDistanceRoll,
            "Loose ball Should NOT be waiting for a distance Roll",
            false,
            looseBallManager.isWaitingForDistanceRoll
        );
        yield return new WaitForSeconds(2f);
        AssertTrue(
            !looseBallManager.isWaitingForDistanceRoll,
            "Loose ball Should NOT be waiting for a distance Roll",
            true,
            looseBallManager.isWaitingForDistanceRoll
        );
        AssertTrue(
            looseBallManager.isWaitingForInterceptionRoll,
            "Loose ball Should be waiting for a interception Roll",
            true,
            looseBallManager.isWaitingForInterceptionRoll
        );
        AssertTrue(
            looseBallManager.potentialInterceptor == PlayerToken.GetPlayerTokenByName("Vladoiu"),
            "Loose ball Should be waiting for a interception Roll from Vladoiu",
            PlayerToken.GetPlayerTokenByName("Vladoiu"),
            looseBallManager.potentialInterceptor
        );
        AssertTrue(
            MatchManager.Instance.LastTokenToTouchTheBallOnPurpose == PlayerToken.GetPlayerTokenByName("Cafferata"),
            "Last player to touch the ball is still the HPasser",
            PlayerToken.GetPlayerTokenByName("Cafferata").name,
            MatchManager.Instance.LastTokenToTouchTheBallOnPurpose.name
        );

        LogFooterofTest("4 players Jump - Loose Ball from Stewart To Space");
    }

    private IEnumerator Scenario_027g_a_4PlayerJump_LooseBall_OnDefender_interception()
    {
        yield return Scenario_027g_4PlayerJump_LooseBall_From_Stewart_Space();
        looseBallManager.PerformInterceptionRoll(6);
        yield return new WaitForSeconds(2.5f);
        AssertTrue(
            !looseBallManager.isWaitingForInterceptionRoll,
            "Loose ball Should NOT be waiting for a interception Roll",
            false,
            looseBallManager.isWaitingForInterceptionRoll
        );
        AssertTrue(
            MatchManager.Instance.teamInAttack == MatchManager.TeamInAttack.Away
            , "Away team is in attack after ball movement"
            , MatchManager.TeamInAttack.Away
            , MatchManager.Instance.teamInAttack
        );
        AvailabilityCheckResult availabilityCheck = AssertCorrectAvailabilityAnyOtherScenario();
        AssertTrue(
            availabilityCheck.passed,
            "Action Availability after Interception (Any Other Scenario)",
            true,
            availabilityCheck.ToString()
        );
        AssertTrue(
            MatchManager.Instance.LastTokenToTouchTheBallOnPurpose == PlayerToken.GetPlayerTokenByName("Vladoiu"),
            "Last player to touch the ball is Now the interceptor",
            PlayerToken.GetPlayerTokenByName("Vladoiu").name,
            MatchManager.Instance.LastTokenToTouchTheBallOnPurpose.name
        );
        Log("▶️ Starting test scenario: 4 players Jump - Loose Ball from Stewart to Space Intercepted by Vladoiu");

        LogFooterofTest("4 players Jump - Loose Ball from Stewart To Space Intercepted by Vladoiu");
    }
    
    private IEnumerator Scenario_027g_b_4PlayerJump_LooseBall_OnDefender_NO_interception()
    {
        yield return Scenario_027g_4PlayerJump_LooseBall_From_Stewart_Space();
        looseBallManager.PerformInterceptionRoll(1);
        yield return new WaitForSeconds(2.5f);
        AssertTrue(
            !looseBallManager.isWaitingForInterceptionRoll,
            "Loose ball Should NOT be waiting for a interception Roll",
            false,
            looseBallManager.isWaitingForInterceptionRoll
        );
        AssertTrue(
            !looseBallManager.isActivated,
            "Loose ball Should have killed itself by now",
            false,
            looseBallManager.isActivated
        );
        AssertTrue(
            MatchManager.Instance.teamInAttack == MatchManager.TeamInAttack.Home
            , "home team is in attack after ball movement"
            , MatchManager.TeamInAttack.Home
            , MatchManager.Instance.teamInAttack
        );
        AvailabilityCheckResult availabilityCheck = AssertCorrectAvailabilityAfterGBToSpace();
        AssertTrue(
            availabilityCheck.passed,
            "Action Availability after Interception (Any Other Scenario)",
            true,
            availabilityCheck.ToString()
        );
        AssertTrue(
            MatchManager.Instance.LastTokenToTouchTheBallOnPurpose == PlayerToken.GetPlayerTokenByName("Cafferata"),
            "Last player to touch the ball is still the HPasser",
            PlayerToken.GetPlayerTokenByName("Cafferata").name,
            MatchManager.Instance.LastTokenToTouchTheBallOnPurpose.name
        );
        Log("▶️ Starting test scenario: 4 players Jump - Loose Ball from Stewart to Space No Interception");

        LogFooterofTest("4 players Jump - Loose Ball from Stewart To Space No Interception");
    }

    private IEnumerator Scenario_027h_4PlayerJump_LooseBall_OnDefender()
    {
        yield return Scenario_027f_4PlayerJump_LooseBall();
        AssertTrue(
            looseBallManager.isWaitingForDirectionRoll,
            "Loose ball Should be waiting for a direction Roll",
            true,
            looseBallManager.isWaitingForDirectionRoll
        );
        Log("Rolling for Direction - South");
        looseBallManager.PerformDirectionRoll(1);
        yield return new WaitForSeconds(0.5f);
        AssertTrue(
            !looseBallManager.isWaitingForDirectionRoll,
            "Loose ball Should NOT be waiting for a direction Roll",
            false,
            looseBallManager.isWaitingForDirectionRoll
        );
        AssertTrue(
            looseBallManager.isWaitingForDistanceRoll,
            "Loose ball Should be waiting for a distance Roll",
            true,
            looseBallManager.isWaitingForDistanceRoll
        );
        yield return new WaitForSeconds(0.5f);
        looseBallManager.PerformDistanceRoll(2);
        Log("Rolling for Distance - 2");
        yield return new WaitForSeconds(2f);
        AssertTrue(
            !looseBallManager.isWaitingForDistanceRoll,
            "Loose ball Should NOT be waiting for a distance Roll",
            false,
            looseBallManager.isWaitingForDistanceRoll
        );
        AssertTrue(
            MatchManager.Instance.teamInAttack == MatchManager.TeamInAttack.Away
            , "Away team is in attack after ball movement"
            , MatchManager.TeamInAttack.Away
            , MatchManager.Instance.teamInAttack
        );
        AvailabilityCheckResult availabilityCheck = AssertCorrectAvailabilityAnyOtherScenario();
        AssertTrue(
            availabilityCheck.passed,
            "Action Availability after Interception (Any Other Scenario)",
            true,
            availabilityCheck.ToString()
        );
        AssertTrue(
            MatchManager.Instance.LastTokenToTouchTheBallOnPurpose == PlayerToken.GetPlayerTokenByName("Paterson"),
            "Last player to touch the ball is the Tokem with the ball.",
            PlayerToken.GetPlayerTokenByName("Paterson").name,
            MatchManager.Instance.LastTokenToTouchTheBallOnPurpose.name
        );

        Log("▶️ Starting test scenario: 4 players Jump - Loose Ball from header On Defender");
        LogFooterofTest("4 players Jump - Loose Ball from header On Defender");
    }

    private IEnumerator Scenario_027i_4PlayerJump_LooseBall_OnAttacker()
    {
        yield return Scenario_027f_4PlayerJump_LooseBall();
        AssertTrue(
            looseBallManager.isWaitingForDirectionRoll,
            "Loose ball Should be waiting for a direction Roll",
            true,
            looseBallManager.isWaitingForDirectionRoll
        );
        Log("Rolling for Direction - NorthEast - 5");
        looseBallManager.PerformDirectionRoll(5);
        AssertTrue(
            !looseBallManager.isWaitingForDirectionRoll,
            "Loose ball Should NOT be waiting for a direction Roll",
            false,
            looseBallManager.isWaitingForDirectionRoll
        );
        yield return new WaitForSeconds(0.5f);
        AssertTrue(
            looseBallManager.isWaitingForDistanceRoll,
            "Loose ball Should be waiting for a distance Roll",
            true,
            looseBallManager.isWaitingForDistanceRoll
        );
        yield return new WaitForSeconds(0.5f);
        Log("Rolling for Distance - 2");
        looseBallManager.PerformDistanceRoll(2);
        AssertTrue(
            !looseBallManager.isWaitingForDistanceRoll,
            "Loose ball Should NOT be waiting for a distance Roll",
            false,
            looseBallManager.isWaitingForDistanceRoll
        );
        yield return new WaitForSeconds(2f);

        Log("▶️ Starting test scenario: 4 players Jump - Loose Ball from header On Attacker");
        LogFooterofTest("4 players Jump - Loose Ball from header On Attacker");
    }
    
    private IEnumerator Scenario_027j_4PlayerJump_LooseBall_OnJumpedToken(bool addKalla = false)
    {
        Log("▶️ Starting test scenario: 4 players Jump - Loose Ball from header On Jumped Token");
        yield return Scenario_027f_4PlayerJump_LooseBall(addKalla);
        AssertTrue(
            looseBallManager.isWaitingForDirectionRoll,
            "Loose ball Should be waiting for a direction Roll",
            true,
            looseBallManager.isWaitingForDirectionRoll
        );
        yield return new WaitForSeconds(0.5f);
        Log("Rolling for Direction - NorthEast 5");
        looseBallManager.PerformDirectionRoll(5);
        yield return new WaitForSeconds(0.5f);
        AssertTrue(
            !looseBallManager.isWaitingForDirectionRoll,
            "Loose ball Should NOT be waiting for a direction Roll",
            false,
            looseBallManager.isWaitingForDirectionRoll
        );
        AssertTrue(
            looseBallManager.isWaitingForDistanceRoll,
            "Loose ball Should be waiting for a distance Roll",
            true,
            looseBallManager.isWaitingForDistanceRoll
        );
        yield return new WaitForSeconds(0.5f);
        Log("Rolling for Distance - 1");
        looseBallManager.PerformDistanceRoll(1);
        yield return new WaitForSeconds(0.5f);
        AssertTrue(
            !looseBallManager.isWaitingForDistanceRoll,
            "Loose ball Should NOT be waiting for a distance Roll",
            false,
            looseBallManager.isWaitingForDistanceRoll
        );
        yield return new WaitForSeconds(2f);
        // AssertTrue(false, "break");

        
        LogFooterofTest("4 players Jump - Loose Ball from header On Jumped Token");
    }

    private IEnumerator Scenario_028_Inaccurate_on_Defenders()
    {
        yield return new WaitForSeconds(1.5f); // Allow scene to stabilize
        Log("▶️ Starting test scenario: High Pass on Attacker, INAccurate HP on Defenders.");
        Log("Pressing 2");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Alpha2, 0.1f));
        AssertTrue(
            MatchManager.Instance.currentState == MatchManager.GameState.KickOffSetup,
            "Game is in KickOff Setup",
            MatchManager.GameState.KickOffSetup,
            MatchManager.Instance.currentState
        );
        Log("Pressing Space");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Space, 0.1f));
        AssertTrue(
            MatchManager.Instance.currentState == MatchManager.GameState.KickoffBlown,
            "Game is in KickoffBlown",
            MatchManager.GameState.KickoffBlown,
            MatchManager.Instance.currentState
        );
        Log("Pressing C - Call a HighPass");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.C, 0.1f));
        Log("Click On (6, 8) - Intitial HP Target");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(6, 8), 0.5f));
        AssertTrue(
            highPassManager.eligibleAttackers.Count == 2,
            "HP target is has 2 eligible Attacker",
            2,
            highPassManager.eligibleAttackers.Count
        );
        AssertTrue(
            highPassManager.currentTargetHex == hexgrid.GetHexCellAt(new Vector3Int (6, 0, 8)),
            "HP target is the key pressed",
            hexgrid.GetHexCellAt(new Vector3Int (6, 0, 8)),
            highPassManager.currentTargetHex
        );
        Log("Click On (6, 8) - Confirm HP Target");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(6, 8), 0.5f));
        AssertTrue(
            highPassManager.currentTargetHex == hexgrid.GetHexCellAt(new Vector3Int (6, 0, 8)),
            "HP target 8, 8 Toothnail",
            hexgrid.GetHexCellAt(new Vector3Int (6, 0, 8)),
            highPassManager.currentTargetHex
        );
        AssertTrue(
            highPassManager.intendedTargetHex == hexgrid.GetHexCellAt(new Vector3Int (6, 0, 8)),
            "HP confrimed target is 8, 8 Toothnail",
            hexgrid.GetHexCellAt(new Vector3Int (6, 0, 8)),
            highPassManager.intendedTargetHex
        );
        AssertTrue(
            highPassManager.isWaitingForAttackerSelection,
            "HP is wating for attacker to be selected",
            true,
            highPassManager.isWaitingForAttackerSelection
        );
        AssertTrue(
            highPassManager.lockedAttacker == null,
            "HP has noone locked"
        );
        Log("Click On (6, 6) - Select Kalla to go on Target");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(6, 6), 0.5f));
        yield return new WaitForSeconds(1);
        AssertTrue(
            !highPassManager.isWaitingForAttackerSelection,
            "HP is NO LONGER wating for attacker to be selected",
            true,
            highPassManager.isWaitingForAttackerSelection
        );
        AssertTrue(
            !highPassManager.isWaitingForAttackerMove,
            "HP is NOT wating for attacker to move",
            false,
            highPassManager.isWaitingForAttackerMove
        );
        AssertTrue(
            highPassManager.isWaitingForDefenderSelection,
            "HP is wating for defender to be selected",
            true,
            highPassManager.isWaitingForDefenderSelection
        );
        AssertTrue(
            !highPassManager.isWaitingForDefenderMove,
            "HP is NOT wating for defender to move",
            false,
            highPassManager.isWaitingForDefenderMove
        );
        Log("Click On (1, 2) - Select Vladoiu");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(1, 2), 0.5f));
        AssertTrue(
            !highPassManager.isWaitingForAttackerSelection,
            "HP is NOT wating for attacker to be selected",
            false,
            highPassManager.isWaitingForAttackerSelection
        );
        AssertTrue(
            !highPassManager.isWaitingForAttackerMove,
            "HP is NOT wating for attacker to move",
            false,
            highPassManager.isWaitingForAttackerMove
        );
        AssertTrue(
            highPassManager.isWaitingForDefenderSelection,
            "HP is wating for defender to be selected",
            true,
            highPassManager.isWaitingForDefenderSelection
        );
        AssertTrue(
            highPassManager.isWaitingForDefenderMove,
            "HP is wating for defender to move",
            false,
            highPassManager.isWaitingForDefenderMove
        );
        Log("Click On (1, 3) - Move Vladoiu");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(1, 3), 0.5f));
        yield return new WaitForSeconds(1.5f);
        AssertTrue(
            !highPassManager.isWaitingForAttackerSelection,
            "HP is NOT wating for attacker to be selected",
            false,
            highPassManager.isWaitingForAttackerSelection
        );
        AssertTrue(
            !highPassManager.isWaitingForAttackerMove,
            "HP is NOT wating for attacker to move",
            false,
            highPassManager.isWaitingForAttackerMove
        );
        AssertTrue(
            !highPassManager.isWaitingForDefenderSelection,
            "HP is NOT wating for defender to be selected",
            false,
            highPassManager.isWaitingForDefenderSelection
        );
        AssertTrue(
            !highPassManager.isWaitingForDefenderMove,
            "HP is NOT wating for defender to move",
            false,
            highPassManager.isWaitingForDefenderMove
        );
        AssertTrue(
            highPassManager.isWaitingForAccuracyRoll,
            "HP is waiting for accuracy Roll",
            true,
            highPassManager.isWaitingForAccuracyRoll
        );
        Log("Pressing R for Accuracy");
        highPassManager.PerformAccuracyRoll(1);
        yield return new WaitForSeconds(0.2f);
        AssertTrue(
            !highPassManager.isWaitingForAccuracyRoll,
            "HP is NO longer waiting for accuracy Roll",
            false,
            highPassManager.isWaitingForAccuracyRoll
        );
        AssertTrue(
            highPassManager.isWaitingForDirectionRoll,
            "HP is waiting for direction Roll",
            true,
            highPassManager.isWaitingForDirectionRoll
        );
        yield return new WaitForSeconds(0.5f);
        Log("Pressing R for Direction 3 - SouthWest");
        highPassManager.PerformDirectionRoll(2);
        yield return new WaitForSeconds(0.2f);
        AssertTrue(
            !highPassManager.isWaitingForDirectionRoll,
            "HP is NO longer waiting for direction Roll",
            false,
            highPassManager.isWaitingForDirectionRoll
        );
        AssertTrue(
            highPassManager.isWaitingForDistanceRoll,
            "HP is waiting for distance Roll",
            true,
            highPassManager.isWaitingForDistanceRoll
        );
        yield return new WaitForSeconds(0.5f);
        Log("Pressing R for Distance 3");
        highPassManager.PerformDistanceRoll(3);
        yield return new WaitForSeconds(0.2f);
        AssertTrue(
            !highPassManager.isWaitingForDistanceRoll,
            "HP is NO Longer waiting for distance Roll",
            true,
            highPassManager.isWaitingForDistanceRoll
        );
        yield return new WaitForSeconds(3.5f);
        AssertTrue(
            !highPassManager.isActivated,
            "HP is NO activated",
            false,
            highPassManager.isActivated
        );
        AssertTrue(
            headerManager.isActivated,
            "Header Manager is activated",
            true,
            headerManager.isActivated
        );
        AssertTrue(
            headerManager.isWaitingForControlOrHeaderDecisionDef,
            "Header Manager Should be waiting for Defensive BC or H Decision",
            true,
            headerManager.isWaitingForControlOrHeaderDecisionDef
        );

        LogFooterofTest("High Pass on Attacker, INAccurate HP on Defenders.");
    }
    
    private IEnumerator Scenario_028a_Defense_Heads()
    {
        Log("▶️ Starting test scenario: High Pass on Attacker, INAccurate HP on Defenders who decide to Head.");
        yield return Scenario_028_Inaccurate_on_Defenders();
        yield return new WaitForSeconds(0.5f);
        Log("Pressing H");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.H, 0.1f));
        AssertTrue(
            !headerManager.isWaitingForControlOrHeaderDecisionDef,
            "Header Manager Should NO LONGER be waiting for Defensive BC or H Decision",
            false,
            headerManager.isWaitingForControlOrHeaderDecisionDef
        );
        AssertTrue(
            headerManager.iswaitingForChallengeWinnerSelection,
            "Header Manager Should be waiting for Challenge Winner Selection",
            true,
            headerManager.iswaitingForChallengeWinnerSelection
        );
        Log("Click On (5, 5) - Head with McnNulty");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(5, 5), 0.5f));
        yield return new WaitForSeconds(0.5f);
        AssertTrue(
            headerManager.isWaitingForHeaderTargetSelection,
            "Header Manager Should be waiting for Header Target Selection",
            true,
            headerManager.isWaitingForHeaderTargetSelection
        );
        AssertTrue(
            headerManager.defenderWillJump.Count == 1,
            "Header Manager defenderWillJump Should be Containing only McNulty",
            1,
            headerManager.defenderWillJump.Count
        );
        AssertTrue(
            headerManager.defenderWillJump.Contains(PlayerToken.GetPlayerTokenByName("McNulty")),
            "Header Manager McNulty should be Jumping"
        );
        yield return new WaitForSeconds(0.5f);
        Log("Click On (1, 10) - Head the ball to Marell");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(1, 10), 0.5f));
        yield return new WaitForSeconds(1.5f);
        AvailabilityCheckResult availabilityCheck = AssertCorrectAvailabilityAfterHeadToPlayer();
        AssertTrue(
            availabilityCheck.passed,
            "Action Availability after Header to Player",
            true,
            availabilityCheck.ToString()
        );
        AssertTrue(
            MatchManager.Instance.attackHasPossession,
            "match Manager should know that attack is in possession",
            true,
            MatchManager.Instance.attackHasPossession
        );
        AssertTrue(
            MatchManager.Instance.teamInAttack == MatchManager.TeamInAttack.Away,
            "Match Manager should know that Away is in Attack",
            MatchManager.TeamInAttack.Away,
            MatchManager.Instance.teamInAttack
        );

        LogFooterofTest("High Pass on Attacker, INAccurate HP on Defenders who decide to Head.");
    }
    
    private IEnumerator Scenario_028b_Defense_Ball_Controls_McNulty()
    {
        Log("▶️ Starting test scenario: High Pass on Attacker, INAccurate HP on Defenders who decide to BC.");
        yield return Scenario_028_Inaccurate_on_Defenders();
        yield return new WaitForSeconds(0.5f);
        Log("Pressing B");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.B, 0.1f));
        AssertTrue(
            headerManager.iswaitingForChallengeWinnerSelection,
            "Header Manager Should be waiting for Challenge winner to use for Dribbling Roll",
            true,
            headerManager.iswaitingForChallengeWinnerSelection
        );
        AssertTrue(
            headerManager.defenderWillJump.Count == 0,
            "Header Manager defenderWillJump Should be empty",
            1,
            headerManager.defenderWillJump.Count
        );
        yield return new WaitForSeconds(0.5f);
        Log("Click On (5, 5) - Ball Control with McNulty");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(5, 5), 0.5f));
        yield return new WaitForSeconds(0.5f);
        AssertTrue(
            headerManager.isWaitingForControlRoll,
            "Header Manager Should be waiting for Control Roll",
            true,
            headerManager.isWaitingForControlRoll
        );

        LogFooterofTest("High Pass on Attacker, INAccurate HP on Defenders who decide to BC.");
    }

    private IEnumerator Scenario_028b_a_Defense_Ball_Controls_McNulty_fails()
    {
        Log("▶️ Starting test scenario: High Pass on Attacker, INAccurate HP on Defenders who decide to BC, McNulty Fails.");
        yield return Scenario_028b_Defense_Ball_Controls_McNulty();
        yield return new WaitForSeconds(0.5f);
        Log("Pressing R - McNulty Fails the Control Attempt");
        headerManager.PerformControlRoll(1);
        yield return new WaitForSeconds(1.5f);
        AssertTrue(
            !headerManager.isWaitingForControlRoll,
            "Header Manager Should NO LONGER be waiting for Control Roll",
            false,
            headerManager.isWaitingForControlRoll
        );
        yield return new WaitForSeconds(0.5f);
        AssertTrue(
            !headerManager.isActivated,
            "Header Manager Should NO LONGER be even Available",
            false,
            headerManager.isActivated
        );
        AssertTrue(
            looseBallManager.isActivated,
            "Loose Ball Manager Should be Available",
            true,
            looseBallManager.isActivated
        );
        AssertTrue(
            looseBallManager.isWaitingForDirectionRoll,
            "Loose Ball Manager Should be Waiting for Direction Roll",
            true,
            looseBallManager.isWaitingForDirectionRoll
        );
        yield return new WaitForSeconds(0.5f);
        Log("Rolling for Direction - 6 - North");
        looseBallManager.PerformDirectionRoll(4);
        yield return new WaitForSeconds(0.5f);
        AssertTrue(
            !looseBallManager.isWaitingForDirectionRoll,
            "Loose Ball Manager Should NOT be Waiting for Direction Roll",
            false,
            looseBallManager.isWaitingForDirectionRoll
        );
        AssertTrue(
            looseBallManager.isWaitingForDistanceRoll,
            "Loose Ball Manager Should be Waiting for Distance Roll",
            true,
            looseBallManager.isWaitingForDistanceRoll
        );
        yield return new WaitForSeconds(0.5f);
        Log("Rolling for Distance - 4");
        looseBallManager.PerformDistanceRoll(4);
        yield return new WaitForSeconds(0.5f);
        AssertTrue(
            !looseBallManager.isWaitingForDistanceRoll,
            "Loose Ball Manager Should NOT be Waiting for Distance Roll",
            false,
            looseBallManager.isWaitingForDistanceRoll
        );
        AssertTrue(
            looseBallManager.isWaitingForInterceptionRoll,
            "Loose Ball Manager Should be Waiting for Interception Roll",
            true,
            looseBallManager.isWaitingForInterceptionRoll
        );
        AssertTrue(
            looseBallManager.potentialInterceptor == PlayerToken.GetPlayerTokenByName("Kalla"),
            "Loose Ball Manager Kalla is waiting for a roll",
            PlayerToken.GetPlayerTokenByName("Kalla"),
            looseBallManager.potentialInterceptor
        );

        LogFooterofTest("High Pass on Attacker, INAccurate HP on Defenders who decide to BC, McNulty Fails.");
    }

    private IEnumerator Scenario_028b_a_a_Defense_Ball_Controls_McNulty_fails_INterception()
    {
        Log("▶️ Starting test scenario: High Pass on Attacker, INAccurate HP on Defenders who decide to BC, McNulty Fails, Kalla Intercepts");
        yield return Scenario_028b_a_Defense_Ball_Controls_McNulty_fails();
        yield return new WaitForSeconds(0.5f);
        looseBallManager.PerformInterceptionRoll(6);
        yield return new WaitForSeconds(1.5f); // Wait for ball to move
        AvailabilityCheckResult availabilityCheck = AssertCorrectAvailabilityAnyOtherScenario();
        AssertTrue(
            availabilityCheck.passed,
            "Action Availability after Interception (Any Other Scenario)",
            true,
            availabilityCheck.ToString()
        );

        LogFooterofTest("High Pass on Attacker, INAccurate HP on Defenders who decide to BC, McNulty Fails, Kalla Intercepts");
    }
    
    private IEnumerator Scenario_028b_a_a_Defense_Ball_Controls_McNulty_fails_NO_interception()
    {
        Log("▶️ Starting test scenario: High Pass on Attacker, INAccurate HP on Defenders who decide to BC, McNulty Fails, Kalla fails to Intercept");
        yield return Scenario_028b_a_Defense_Ball_Controls_McNulty_fails();
        yield return new WaitForSeconds(0.5f);
        looseBallManager.PerformInterceptionRoll(1);
        yield return new WaitForSeconds(2.5f); // Wait for ball to move
        AvailabilityCheckResult availabilityCheck = AssertCorrectAvailabilityAfterGBToSpace();
        AssertTrue(
            availabilityCheck.passed,
            "Action Availability after Interception (Any Other Scenario)",
            true,
            availabilityCheck.ToString()
        );
        AssertTrue(
            movementPhaseManager.isAwaitingTokenSelection,
            "MP should be waiting for a token selection",
            true,
            movementPhaseManager.isAwaitingTokenSelection
        );
        yield return new WaitForSeconds(0.5f);
        Log("Click On (5, 5) - Select McNulty");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(5, 5), 0.5f));
        AssertTrue(
            movementPhaseManager.isAwaitingTokenSelection,
            "MP should be waiting for a token selection",
            true,
            movementPhaseManager.isAwaitingTokenSelection
        );
        AssertTrue(
            movementPhaseManager.isAwaitingHexDestination,
            "MP should be waiting for a HexDestination",
            true,
            movementPhaseManager.isAwaitingHexDestination
        );
        AssertTrue(
            movementPhaseManager.isBallPickable,
            "MP Ball should be pickable",
            true,
            movementPhaseManager.isBallPickable
        );
        Log("Pressing V - Pick Up ball with McNulty");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.V, 0.1f));
        yield return new WaitForSeconds(2.5f); // Wait for McN to Move
        AssertTrue(
            movementPhaseManager.isDribblerRunning,
            "MP Dribbler is Dribbling",
            true,
            movementPhaseManager.isDribblerRunning
        );
        AssertTrue(
            movementPhaseManager.remainingDribblerPace == 1,
            "MP McNulty has 1 more pace available",
            1,
            movementPhaseManager.remainingDribblerPace
        );


        LogFooterofTest("High Pass on Attacker, INAccurate HP on Defenders who decide to BC, McNulty Fails, Kalla fails to Intercept");
    }

    private IEnumerator Scenario_028b_a_Defense_Ball_Controls_McNulty_BC()
    {
        Log("▶️ Starting test scenario: High Pass on Attacker, INAccurate HP on Defenders who decide to BC, McNulty Controls.");
        yield return Scenario_028b_Defense_Ball_Controls_McNulty();
        yield return new WaitForSeconds(0.5f);
        Log("Pressing R - McNulty Fails the Control Attempt");
        headerManager.PerformControlRoll(6);
        yield return new WaitForSeconds(1.5f); 
        AvailabilityCheckResult successfulTackle = AssertCorrectAvailabilityAfterSuccessfulTackle();
        AssertTrue(
            successfulTackle.passed,
            "Availability after successful tackle",
            true,
            successfulTackle.ToString()
        );

        LogFooterofTest("High Pass on Attacker, INAccurate HP on Defenders who decide to BC, McNulty Controls.");

    }
    
    private IEnumerator Scenario_029_HeaderAtGoal_prep(bool gkRush = false)
    {
        yield return new WaitForSeconds(1.5f); // Allow scene to stabilize
        Log("▶️ Starting test scenario: High Pass towards Attacker to head at Goal");
        Log("Pressing 2");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Alpha2, 0.1f));
        AssertTrue(
            MatchManager.Instance.currentState == MatchManager.GameState.KickOffSetup,
            "Game is in KickOff Setup",
            MatchManager.GameState.KickOffSetup,
            MatchManager.Instance.currentState
        );
        Log("Pressing Space");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Space, 0.1f));
        AssertTrue(
            MatchManager.Instance.currentState == MatchManager.GameState.KickoffBlown,
            "Game is in KickoffBlown",
            MatchManager.GameState.KickoffBlown,
            MatchManager.Instance.currentState
        );
        Log("Pressing C - Call a HighPass");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.C, 0.1f));
        Log("Click On (13, 1) - Intitial HP Target");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(13, 1), 0.5f));
        AssertTrue(
            highPassManager.eligibleAttackers.Count == 1,
            "HP target is has 1 eligible Attacker",
            1,
            highPassManager.eligibleAttackers.Count
        );
        AssertTrue(
            highPassManager.currentTargetHex == hexgrid.GetHexCellAt(new Vector3Int(13, 0, 1)),
            "HP target is the key pressed",
            hexgrid.GetHexCellAt(new Vector3Int(13, 0, 1)),
            highPassManager.currentTargetHex
        );
        Log("Click On (13, 1) - Confirm HP Target");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(13, 1), 0.5f));
        yield return new WaitForSeconds(1.0f);
        AssertTrue(
            !highPassManager.isWaitingForConfirmation,
            "HP target is NO LONGER waiting for target confirmation",
            false,
            highPassManager.isWaitingForConfirmation
        );
        AssertTrue(
            highPassManager.eligibleAttackers.Count == 1,
            "HP target is has 1 eligible Attacker",
            1,
            highPassManager.eligibleAttackers.Count
        );
        AssertTrue(
            !highPassManager.isWaitingForAttackerSelection,
            "HP target is waiting for Attacker selection",
            false,
            highPassManager.isWaitingForAttackerSelection
        );
        Log("Click On (14, 0) - Click on a Defender");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(14, 0), 0.5f));
        yield return new WaitForSeconds(0.5f);
        AssertTrue(
            !highPassManager.isWaitingForAttackerSelection,
            "HP target is waiting for Attacker selection",
            false,
            highPassManager.isWaitingForAttackerSelection
        );
        AssertTrue(
            !highPassManager.isWaitingForAttackerMove,
            "HP target is waiting for Attacker move",
            false,
            highPassManager.isWaitingForAttackerMove
        );
        AssertTrue(
            highPassManager.isWaitingForDefenderSelection,
            "HP target is waiting for Defender selection",
            true,
            highPassManager.isWaitingForDefenderSelection
        );
        AssertTrue(
            highPassManager.isWaitingForDefenderMove,
            "HP target is waiting for Defender move",
            true,
            highPassManager.isWaitingForDefenderMove
        );
        Log("Click On (14, 1) - Click on an valid Hex");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(14, 1), 0.5f));
        yield return new WaitForSeconds(1);
        AssertTrue(
            !highPassManager.isWaitingForAttackerSelection,
            "HP target is waiting for Attacker selection",
            false,
            highPassManager.isWaitingForAttackerSelection
        );
        AssertTrue(
            !highPassManager.isWaitingForAttackerMove,
            "HP target is waiting for Attacker move",
            false,
            highPassManager.isWaitingForAttackerMove
        );
        AssertTrue(
            !highPassManager.isWaitingForDefenderSelection,
            "HP target is waiting for Defender selection",
            false,
            highPassManager.isWaitingForDefenderSelection
        );
        AssertTrue(
            !highPassManager.isWaitingForDefenderMove,
            "HP target is waiting for Defender move",
            false,
            highPassManager.isWaitingForDefenderMove
        );
        AssertTrue(
            highPassManager.isWaitingForAccuracyRoll,
            "HP target is NOT waiting for Accuracy Roll yet",
            true,
            highPassManager.isWaitingForAccuracyRoll
        );
        highPassManager.PerformAccuracyRoll(6);
        yield return new WaitForSeconds(3);
        AssertTrue(
            highPassManager.isActivated,
            "HP should NOT be done",
            true,
            highPassManager.isActivated
        );
        AssertTrue(
            !headerManager.isActivated,
            "header Manager should be activated",
            false,
            headerManager.isActivated
        );
        AssertTrue(
            goalKeeperManager.isActivated,
            "GK Manager should be activated for the ball entering the box",
            true,
            goalKeeperManager.isActivated
        );
        Log("Click On (16, 1) - Click on an valid Hex");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(16, 1), 0.5f));
        yield return new WaitForSeconds(1); // Move GK for Box
        AssertTrue(
            !goalKeeperManager.isActivated,
            "GK Manager should be done with GK Movement",
            false,
            goalKeeperManager.isActivated
        );
        AssertTrue(
            highPassManager.isWaitingForDefGKChallengeDecision,
            "HP Manager should now be waiting for a GK decision to Rushout",
            true,
            highPassManager.isWaitingForDefGKChallengeDecision
        );
        if (gkRush)
        {
          Log("Click On (15, 2) - Click on an valid Hex");
          yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(15, 2), 0.5f));
          yield return new WaitForSeconds(1); // Move GK for HighPass
          AssertTrue(
              highPassManager.gkRushedOut,
              "HP Manager know that GK rushed out",
              true,
              highPassManager.gkRushedOut
          );
        }
        else
        {
            Log("Press X - Do not Rush Out");
            yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.5f));
            yield return new WaitForSeconds(1); // Move GK for HighPass
            AssertTrue(
                !highPassManager.gkRushedOut,
                "HP Manager know that GK DID NOT rush out",
                false,
                highPassManager.gkRushedOut
            );  
        }
        AssertTrue(
            !highPassManager.isActivated,
            "HP Manager should be done now",
            false,
            highPassManager.isActivated
        );
        AssertTrue(
            finalThirdManager.isActivated,
            "Final third Manager should be waiting",
            true,
            finalThirdManager.isActivated
        );
        Log("Pressing X - Forfeit Att F3");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        yield return new WaitForSeconds(0.2f);
        Log("Pressing X - Forfeit Def F3");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        yield return new WaitForSeconds(0.2f);
        AssertTrue(
            headerManager.isActivated,
            "Head should be Active",
            true,
            headerManager.isActivated
        );
        AssertTrue(
            !finalThirdManager.isActivated,
            "Final third Manager should be done by now",
            false,
            finalThirdManager.isActivated
        );
        AssertTrue(
            !headerManager.isWaitingForAttackerSelection,
            "Header Manager Should NOT be waiting for Attacker selection as there is only one",
            false,
            headerManager.isWaitingForAttackerSelection
        );
        AssertTrue(
            headerManager.isWaitingForHeaderAtGoal,
            "Header Manager Should be waiting for Attack to chose if they are going to try and score",
            true,
            headerManager.isWaitingForHeaderAtGoal
        );
        AssertTrue(
            headerManager.attEligibleToHead.Contains(PlayerToken.GetPlayerTokenByName("Yaneva")),
            "Header Manager Yaneva is Eligible"
        );
        AssertTrue(
            headerManager.attackerWillJump.Contains(PlayerToken.GetPlayerTokenByName("Yaneva")),
            "Header Manager Yaneva is Automatically nominated"
        );
        Log("Click On (19, 1) - Declaring header At Goal");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(19, 1), 0.5f));
        yield return new WaitForSeconds(0.2f);
        AssertTrue(
            !headerManager.isWaitingForHeaderAtGoal,
            "Header Manager Should NO LONGER be waiting for GOAL decision, as it was already declared",
            false,
            headerManager.isWaitingForHeaderAtGoal
        );
        AssertTrue(
            headerManager.isWaitingForDefenderSelection,
            "Header Manager Should be waiting for More defenders to challenge",
            true,
            headerManager.isWaitingForDefenderSelection
        );
        if (gkRush)
        {
          AssertTrue(
            headerManager.defenderWillJump.Contains(PlayerToken.GetPlayerTokenByName("Kuzmic")),
            "GK is already in defensive challengers"
          );
          Log("Click On (15, 2) - Attempt to Deselect Kuzmic");
          yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(15, 2), 0.5f));
          AssertTrue(
            headerManager.defenderWillJump.Contains(PlayerToken.GetPlayerTokenByName("Kuzmic")),
            "Defensive GK was not denominated"
          );
            AssertTrue(
                highPassManager.gkRushedOut,
                "HP Manager know that GK rushed out",
                true,
                highPassManager.gkRushedOut
            );
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
            AssertTrue(
                !highPassManager.gkRushedOut,
                "HP Manager know that GK DID NOT rush out",
                false,
                highPassManager.gkRushedOut
            );  
        }
        AssertTrue(
            headerManager.isWaitingForDefenderSelection,
            "Header Manager Should be waiting for More defenders to challenge",
            true,
            headerManager.isWaitingForDefenderSelection
        );
        Log("Click On (14, 1) - Nominate Soares to also Jump");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(14, 1), 0.5f));
        yield return new WaitForSeconds(0.2f);
        Log("Pressing Enter to confirm the Defensive Selection");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.KeypadEnter, 0.1f));
        yield return new WaitForSeconds(0.2f);
        AssertTrue(
            !headerManager.isWaitingForDefenderSelection,
            "Header Manager Should NO LONGER be waiting for More defenders to challenge",
            false,
            headerManager.isWaitingForDefenderSelection
        );
        AssertTrue(
            headerManager.isWaitingForHeaderRoll,
            "Header Manager Should be waiting for Header Rolls",
            true,
            headerManager.isWaitingForHeaderRoll
        );

        LogFooterofTest("High Pass towards Attacker to head at Goal");
    }

    private IEnumerator Scenario_029_HeaderAtGoal_GOAL(bool gkRush = false)
    {
        Log("▶️ Starting test scenario: High Pass towards Attacker to head at Goal AND GOAL");
        yield return Scenario_029_HeaderAtGoal_prep(gkRush);
        Log("Press R to Roll a Header for Yaneva");
        headerManager.PerformHeaderRoll(6);
        yield return new WaitForSeconds(0.2f);
        Log("Press R to Roll a Header for Soares");
        headerManager.PerformHeaderRoll(1);
        yield return new WaitForSeconds(0.2f);
        if (gkRush)
        {
            Log("Press R to Roll a Header for Kuzmic");
            headerManager.PerformHeaderRoll(1);
            yield return new WaitForSeconds(0.4f);
            AssertTrue(
                goalFlowManager.isActivated
                , "GoalFlow is activated"
            );
            while (goalFlowManager.isActivated) yield return null;
            AssertTrue(
                !goalFlowManager.isActivated
                , "GoalFlow is no longer activated"
            );
        }
        else
        {
            AssertTrue(
                !headerManager.isActivated,
                "Header Manager is dead",
                false,
                headerManager.isActivated
            );
            AssertTrue(
                shotManager.isActivated,
                "Shot Manager is activated for the Goal",
                true,
                shotManager.isActivated
            );
            AssertTrue(
                shotManager.isWaitingForGKDiceRoll,
                "Shot Manager is waiting for GK Dice Roll",
                true,
                shotManager.isWaitingForGKDiceRoll
            );
            shotManager.PerformGKHeaderSave(2);
            yield return new WaitForSeconds(1.2f);
            AssertTrue(
                goalFlowManager.isActivated
                , "GoalFlow is activated"
            );
            while (goalFlowManager.isActivated) yield return null;
            AssertTrue(
                !goalFlowManager.isActivated
                , "GoalFlow is no longer activated"
            );
            
        }
        AssertTrue(
            MatchManager.Instance.gameData.stats.GetPlayerStats("Cafferata").assists == 1,
            "Cafferata should be credited with exactly one assist for the headed goal",
            1,
            MatchManager.Instance.gameData.stats.GetPlayerStats("Cafferata").assists
        );
        AssertTrue(
            MatchManager.Instance.gameData.stats.GetTeamStats(PlayerToken.GetPlayerTokenByName("Cafferata").isHomeTeam).totalAssists == 1,
            "The assisting team should record exactly one assist for the headed goal",
            1,
            MatchManager.Instance.gameData.stats.GetTeamStats(PlayerToken.GetPlayerTokenByName("Cafferata").isHomeTeam).totalAssists
        );
        LogFooterofTest("High Pass towards Attacker to head at Goal AND GOAL");
    }
    
    private IEnumerator Scenario_029_HeaderAtGoal_OFF_TARGET(bool gkRush = false)
    {
        Log("▶️ Starting test scenario: High Pass towards Attacker to head at Goal AND OFF target");
        if (gkRush) yield return Scenario_029_HeaderAtGoal_prep(true);
        else yield return Scenario_029_HeaderAtGoal_prep();
        PlayerToken.GetPlayerTokenByName("Yaneva").heading = 6;
        Log("Press R to Roll a Header for Yaneva");
        headerManager.PerformHeaderRoll(1);
        yield return new WaitForSeconds(0.2f);
        Log("Press R to Roll a Header for Soares");
        headerManager.PerformHeaderRoll(1);
        yield return new WaitForSeconds(0.2f);
        if (gkRush)
        {
            Log("Press R to Roll a Header for Kuzmic");
            headerManager.PerformHeaderRoll(1);
        }
        yield return new WaitForSeconds(5.2f);
        AssertTrue(
            MatchManager.Instance.currentState == MatchManager.GameState.WaitingForGoalKickFinalThirds,
            "Match Manager shound understand that it is a GoalKick",
            MatchManager.GameState.WaitingForGoalKickFinalThirds,
            MatchManager.Instance.currentState
        );
            
        // Check what to test from here
        // AssertTrue(false, "break");
        LogFooterofTest("High Pass towards Attacker to head at Goal AND OFF target");
    }
    
    private IEnumerator Scenario_029_HeaderAtGoal_Saved_by_GK(bool gkRush = false, bool handled = true)
    {
        Log("▶️ Starting test scenario: High Pass towards Attacker to head at Goal AND saved by GK");
        yield return Scenario_029_HeaderAtGoal_prep(gkRush);
        Log("Press R to Roll a Header for Yaneva");
        headerManager.PerformHeaderRoll(2);
        yield return new WaitForSeconds(0.2f);
        Log("Press R to Roll a Header for Soares");
        headerManager.PerformHeaderRoll(1);
        yield return new WaitForSeconds(0.2f);
        if (gkRush)
        {
            Log("Press R to Roll a Header for Kuzmic");
            headerManager.PerformHeaderRoll(6);
            yield return new WaitForSeconds(1.5f);
            AssertTrue(
                shotManager.isWaitingForSaveandHoldScenario,
                "Shot Manager is now handling the Save and Hold Choice",
                true,
                shotManager.isWaitingForSaveandHoldScenario
            );
            AssertTrue(
                !headerManager.isActivated,
                "Header Manager is dead",
                true,
                headerManager.isActivated
            );
        }
        else
        {
            AssertTrue(
                shotManager.isWaitingForGKDiceRoll,
                "Shot Manager is Waiting for GK Dice Roll",
                true,
                shotManager.isWaitingForGKDiceRoll
            );
            AssertTrue(
                !headerManager.isActivated,
                "Header Manager is dead",
                true,
                headerManager.isActivated
            );
            yield return new WaitForSeconds(0.5f);
            Log("Press R to Roll a Save with Kuzmic");
            shotManager.PerformGKHeaderSave(6);
            yield return new WaitForSeconds(1.5f);
            AssertTrue(
                !shotManager.isWaitingForGKDiceRoll,
                "Shot Manager is no longer Waiting for GK Dice Roll",
                false,
                shotManager.isWaitingForGKDiceRoll
            );
            AssertTrue(
                shotManager.isWaitingforHandlingTest,
                "Shot Manager is Waiting for Handling Test",
                true,
                shotManager.isWaitingforHandlingTest
            );
            yield return new WaitForSeconds(0.5f);
            if (handled)
            {
                StartCoroutine(shotManager.ResolveHandlingTest(1));
                AssertTrue(
                    shotManager.isWaitingForSaveandHoldScenario
                    ,"Shot Manager is waiting for Save and Hold Scenario"
                    , true
                    , shotManager.isWaitingForSaveandHoldScenario
                );
            }
            else
            {
                StartCoroutine(shotManager.ResolveHandlingTest(6));
                yield return new WaitForSeconds(0.5f);
                AssertTrue(
                    !shotManager.isWaitingforHandlingTest,
                    "Shot Manager is no longer Waiting for Handling Test",
                    true,
                    shotManager.isWaitingforHandlingTest
                );
            }
            
        }
        LogFooterofTest("High Pass towards Attacker to head at Goal AND saved by GK");
    }
    
    private IEnumerator Scenario_029_HeaderAtGoal_Saved_by_GK_QThrow(bool gkRush = false)
    {
        Log("▶️ Starting test scenario: High Pass towards Attacker to head at Goal AND saved by GK, GK chooses Quick Throw");
        yield return Scenario_029_HeaderAtGoal_Saved_by_GK(gkRush);
        Log("Pressing Q to Play a Quick Throw");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Q, 0.1f));
        yield return new WaitForSeconds(0.5f);
        AssertTrue(
            !shotManager.isActivated
            , "Shot Manager killed itself"
            , true
            , shotManager.isActivated
        );
        AssertTrue(
            groundBallManager.isActivated
            , "Ground Ball Manager is Activated"
            , true
            , groundBallManager.isActivated
        );
        AssertTrue(
            MatchManager.Instance.currentState == MatchManager.GameState.QuickThrow
            , "Matchmanager should know that we are in a quick throw in order to help GBM handle it"
            , MatchManager.GameState.QuickThrow
            , MatchManager.Instance.currentState
        );
        AssertTrue(
            groundBallManager.isQuickThrow
            , "Ground Ball Manager is handling a Quick Throw"
            , true
            , groundBallManager.isQuickThrow
        );

        LogFooterofTest("High Pass towards Attacker to head at Goal AND saved by GK, GK chooses Quick Throw");
    }
    
    private IEnumerator Scenario_029_HeaderAtGoal_Saved_by_GK_GoalKick(bool gkRush = false)
    {
        Log("▶️ Starting test scenario: High Pass towards Attacker to head at Goal AND saved by GK, GK chooses Goal Kick");
        yield return Scenario_029_HeaderAtGoal_Saved_by_GK(gkRush);
        Log("Pressing Enter to confirm the Defensive Selection");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.K, 0.1f));
        AssertTrue(
            !shotManager.isWaitingForSaveandHoldScenario
            , "header Manager is NOT waiting for Save and Hold decision"
            , true
            , shotManager.isWaitingForSaveandHoldScenario
        );
        AssertTrue(
            finalThirdManager.isActivated
            , "F3 Manager is Activated"
            , true
            , finalThirdManager.isActivated
        );
        AssertTrue(
            finalThirdManager.bothSides
            , "F3 Manager is Activated for both sides"
            , true
            , finalThirdManager.bothSides
        );
        LogFooterofTest("High Pass towards Attacker to head at Goal AND saved by  GK, GK chooses Goal Kick");
    }

    private IEnumerator Scenario_029_HeaderAtGoal_Saved_by_GK_Corner(bool gkRush = false)
    {
        Log("▶️ Starting test scenario: High Pass towards Attacker to head at Goal AND saved but not handled by GK, Corner");
        yield return Scenario_029_HeaderAtGoal_Saved_by_GK(gkRush, false);
        AssertTrue(
            looseBallManager.isActivated
            , "Loose Ball Manager is Activated"
            , true
            , looseBallManager.isActivated
        );
        AssertTrue(
            looseBallManager.isWaitingForDirectionRoll
            , "Loose Ball Manager is Waiting for Direction Roll"
            , true
            , looseBallManager.isWaitingForDirectionRoll
        );
        Log("Rolling for Direction - 4 - North - Corner");
        looseBallManager.PerformDirectionRoll(4);
        yield return new WaitForSeconds(0.5f);
        AssertTrue(
            !looseBallManager.isWaitingForDirectionRoll
            , "Loose Ball Manager is NOT Waiting for Direction Roll"
            , false
            , looseBallManager.isWaitingForDirectionRoll
        );
        AssertTrue(
            !looseBallManager.isWaitingForDistanceRoll
            , "Loose Ball Manager is Not Waiting for Distance Roll"
            , false
            , looseBallManager.isWaitingForDistanceRoll
        );
        yield return new WaitForSeconds(4.5f);
        AssertTrue(
            !looseBallManager.isActivated
            , "Loose Ball Manager is Not Activated"
            , false
            , looseBallManager.isActivated
        );
        AssertTrue(
            freeKickManager.isActivated
            , "Free Kick Manager is Activated"
            , true
            , freeKickManager.isActivated
        );
        AssertTrue(
            freeKickManager.isWaitingForKickerSelection
            , "Free Kick Manager is Waiting for Kicker Selection"
            , true
            , freeKickManager.isWaitingForKickerSelection
        );
        // TODO: Check Corner Kick Flow
        Log("Click On (-6, -6) - Nominate Noruega as the Corner Kicker");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(-6, -6), 0.1f));
        yield return new WaitForSeconds(2.5f);
        AssertTrue(
            !freeKickManager.isWaitingForKickerSelection
            , "Free Kick Manager is NOT Waiting for Kicker Selection"
            , false
            , freeKickManager.isWaitingForKickerSelection
        );
        AssertTrue(
            freeKickManager.isWaitingForSetupPhase
            , "Free Kick Manager is Waiting for Setup Phase"
            , true
            , freeKickManager.isWaitingForSetupPhase
        );
        AssertTrue(
            false,
            "Break"
        );
    LogFooterofTest("High Pass towards Attacker to head at Goal AND saved but not handled by GK, Corner");
    }

    private IEnumerator Scenario_029_HeaderAtGoal_Saved_by_GK_LooseBall(bool gkRush = false)
    {
        Log("▶️ Starting test scenario: High Pass towards Attacker to head at Goal AND saved but not handled by GK, Loose Ball");
        yield return Scenario_029_HeaderAtGoal_Saved_by_GK(gkRush, false);
        AssertTrue(
            looseBallManager.isActivated
            , "Loose Ball Manager is Activated"
            , true
            , looseBallManager.isActivated
        );
        AssertTrue(
            looseBallManager.isWaitingForDirectionRoll
            , "Loose Ball Manager is Waiting for Direction Roll"
            , true
            , looseBallManager.isWaitingForDirectionRoll
        );
        Log("Rolling for Direction - 4 - NorthWest - Play On");
        looseBallManager.PerformDirectionRoll(3);
        yield return new WaitForSeconds(0.5f);
        AssertTrue(
            !looseBallManager.isWaitingForDirectionRoll
            , "Loose Ball Manager is NOT Waiting for Direction Roll"
            , false
            , looseBallManager.isWaitingForDirectionRoll
        );
        AssertTrue(
            looseBallManager.isWaitingForDistanceRoll
            , "Loose Ball Manager is Waiting for Distance Roll"
            , true
            , looseBallManager.isWaitingForDistanceRoll
        );
        LogFooterofTest("High Pass towards Attacker to head at Goal AND saved but not handled by GK, Loose Ball");
    }
    
    private IEnumerator Scenario_029_HeaderAtGoal_Headed_Away(bool gkRush = false)
    {
        Log("▶️ Starting test scenario: High Pass towards Attacker to head at Goal AND headed away by Def");
        yield return Scenario_029_HeaderAtGoal_prep(gkRush);
        Log("Press R to Roll a Header for Yaneva");
        headerManager.PerformHeaderRoll(2);
        yield return new WaitForSeconds(0.2f);
        Log("Press R to Roll a Header for Soares");
        headerManager.PerformHeaderRoll(6);
        yield return new WaitForSeconds(0.2f);
        if (gkRush)
        {
            Log("Press R to Roll a Header for Kuzmic");
            headerManager.PerformHeaderRoll(2);
            yield return new WaitForSeconds(0.5f);
        }
        // AssertTrue(false, "break");
        AssertTrue(
            headerManager.isWaitingForHeaderTargetSelection,
            "Header Manager Should be waiting for Header Target Selection",
            true,
            headerManager.isWaitingForHeaderTargetSelection
        );
        LogFooterofTest("High Pass towards Attacker to head at Goal AND headed away by Def");
    }
    
    private IEnumerator Scenario_029_HeaderAtGoal_LooseBall(bool gkRush = false)
    {
        Log("▶️ Starting test scenario: High Pass towards Attacker to head at Goal AND Looseball");
        yield return Scenario_029_HeaderAtGoal_prep(gkRush);
        Log("Press R to Roll a Header for Yaneva");
        headerManager.PerformHeaderRoll(5);
        yield return new WaitForSeconds(0.2f);
        Log("Press R to Roll a Header for Soares");
        headerManager.PerformHeaderRoll(5);
        yield return new WaitForSeconds(0.2f);
        if (gkRush)
        {
            Log("Press R to Roll a Header for Kuzmic");
            headerManager.PerformHeaderRoll(2);
            yield return new WaitForSeconds(0.5f);
        }
        yield return new WaitForSeconds(1.5f);
        AssertTrue(
            looseBallManager.isActivated,
            "Loose Ball Manager Should be Activated",
            true,
            looseBallManager.isActivated
        );
        AssertTrue(
            looseBallManager.isWaitingForDirectionRoll,
            "Loose Ball Manager Should be Waiting for Direction Roll",
            true,
            looseBallManager.isWaitingForDirectionRoll
        );
        Log("Rolling for Direction - 1 - South");
        looseBallManager.PerformDirectionRoll(1);
        yield return new WaitForSeconds(0.5f);
        AssertTrue(
            !looseBallManager.isWaitingForDirectionRoll,
            "Loose Ball Manager Should NOT be Waiting for Direction Roll",
            false,
            looseBallManager.isWaitingForDirectionRoll
        );
        AssertTrue(
            looseBallManager.isWaitingForDistanceRoll,
            "Loose Ball Manager Should be Waiting for Distance Roll",
            true,
            looseBallManager.isWaitingForDistanceRoll
        );
        yield return new WaitForSeconds(0.5f);
        Log("Rolling for Distance - 4");
        looseBallManager.PerformDistanceRoll(4);
        yield return new WaitForSeconds(0.5f);
        AssertTrue(
            !looseBallManager.isWaitingForDistanceRoll,
            "Loose Ball Manager Should NOT be Waiting for Distance Roll",
            false,
            looseBallManager.isWaitingForDistanceRoll
        );
        yield return new WaitForSeconds(3.5f);
        AssertTrue(
            !looseBallManager.isActivated,
            "Loose Ball Manager Should NOT be Activated",
            false,
            looseBallManager.isActivated
        );
        // AssertTrue(false, "break");
        LogFooterofTest("High Pass towards Attacker to head at Goal AND Looseball");
    }
    
    private IEnumerator Scenario_029_HeaderAtGoal_LooseBall_OWN_GOAL(bool gkRush = false)
    {
        Log("▶️ Starting test scenario: High Pass towards Attacker to head at Goal AND Loose Ball for Own GOAL");
        yield return Scenario_029_HeaderAtGoal_prep(gkRush);
        Log("Press R to Roll a Header for Yaneva");
        headerManager.PerformHeaderRoll(1);
        yield return new WaitForSeconds(0.2f);
        Log("Press R to Roll a Header for Soares");
        headerManager.PerformHeaderRoll(1);
        yield return new WaitForSeconds(0.2f);
        Log("Press R to Roll a Header for Kuzmic");
        headerManager.PerformHeaderRoll(1);
        yield return new WaitForSeconds(0.5f);
        // AssertTrue(false, "break");
        LogFooterofTest("High Pass towards Attacker to head at Goal AND Loose Ball for Own GOAL");
    }

        // AssertTrue(
  //     false,
  //     "Break"
  // );

  // TODO: Movement Phase
  // TODO: OwnGoal from Tackle Loose Ball
  // TODO: Pass to player
  //    , move dribbler for nutmeg, fail nutmeg, reposition defender, check Availability
  // TODO: Pass to player
  //    , move dribbler for nutmeg, Loose ball on nutmeg, on dribbler, check Availability
  // TODO: Pass to player
  //    , move dribbler for nutmeg, Loose ball on nutmeg, on not moved attacker, check Availability
  // TODO: Pass to player
  //    , move dribbler for nutmeg, Loose ball on nutmeg, on defender, check Availability
  // TODO: Pass to player
  //    , move dribbler for nutmeg, Loose ball on nutmeg, to space with interceptions, check Availability
  // TODO: Pass to player
  //    , move dribbler for nutmeg, Loose ball on nutmeg, to space without interceptions, check Availability



  // TODO: Pass to player
  //    , move dribler for nutmeg
  //    , reposition and continue moving
  //    , forfeit MPAtt
  //    , verify the defender cannot move
  //    , end MP
  // TODO: Pass to player
  //    , move dribbler for nutmeg, loose ball to space with no interceptions.
  // TODO: Pass to player
  //    , move dribbler for nutmeg, loose ball to space with failed interceptions.
  // TODO: Pass to next to defenders, fail interceptions
  //    , move attacker to pickup ball and nutmeg.

  // TODO: Final Thirds
  // TODO: High Pass
  // TODO: Long Ball

    private IEnumerator Scenario_030a_LongBall_Difficulty1_InvalidTarget_And_AccurateThreshold()
    {
        yield return StartCoroutine(PrepareManualLongBallBoardState(1));

        Log("Pressing L - Start Long Ball");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.L, 0.1f));
        AssertTrue(longBallManager.isActivated, "Long Ball should be activated.");
        AssertTrue(longBallManager.isAwaitingTargetSelection, "Long Ball should be waiting for target selection.");

        Log("Clicking (-8, 8) - Invalid target blocked by Abraham touching Ulisses");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(-8, 8), 0.1f));
        AssertTrue(longBallManager.currentTargetHex == null, "Long Ball should reject (-8,8) as blocked by an adjacent defender.");
        AssertTrue(longBallManager.isAwaitingTargetSelection, "Long Ball should still be awaiting target selection after rejecting (-8,8).");

        HexCell intendedTarget = RequireHex(hexgrid.GetHexCellAt(new Vector3Int(6, 0, -7)), "Long Ball test target (6,-7) should exist.");
        Log("Clicking (6, -7) - Select valid Long Ball target");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(6, -7), 0.1f));
        AssertTrue(longBallManager.currentTargetHex == intendedTarget, "Long Ball should accept (6,-7) as the selected target.", intendedTarget, longBallManager.currentTargetHex);

        Log("Clicking (6, -7) again - Confirm Long Ball target");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(6, -7), 0.1f));
        AssertTrue(longBallManager.isWaitingForAccuracyRoll, "Long Ball should be waiting for the accuracy roll after confirmation.");
        AssertTrue(
            longBallManager.GetInstructions().Contains("3+"),
            "Long Ball instructions should show a 3+ accuracy threshold for Ulisses.",
            true,
            longBallManager.GetInstructions()
        );

        Log("Rigging accurate Long Ball roll to 3");
        PerformRiggedLongBallAccuracyRoll(3);
        yield return new WaitForSeconds(2.3f);

        if (longBallManager.isWaitingForDefLBMove)
        {
            Log("Pressing X - Forfeit GK pace move");
            yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        }

        yield return new WaitForSeconds(0.3f);

        AvailabilityCheckResult availabilityCheck = AssertCorrectAvailabilityAfterGBToSpace();
        AssertTrue(availabilityCheck.passed, "Accurate Long Ball to space should auto-commit MP for attack.", true, availabilityCheck.ToString());
        AssertTrue(MatchManager.Instance.hangingPassType == "aerial", "Accurate Long Ball to space should leave an aerial hanging pass.", "aerial", MatchManager.Instance.hangingPassType);
        AssertTrue(longBallManager.ball.GetCurrentHex() == intendedTarget, "Ball should finish on the accurate long-ball target.", intendedTarget, longBallManager.ball.GetCurrentHex());

        LogFooterofTest("Long Ball Difficulty 1 Invalid Target And Accurate Threshold");
    }

    private IEnumerator Scenario_030b_LongBall_Difficulty3_Commits_On_First_Click()
    {
        yield return StartCoroutine(PrepareManualLongBallBoardState(3));

        Log("Pressing L - Start Long Ball");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.L, 0.1f));
        Log("Clicking (6, -7) - Difficulty 3 first valid target");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(6, -7), 0.1f));

        AssertTrue(!longBallManager.isAwaitingTargetSelection, "Difficulty 3 Long Ball should commit on the first valid click.");
        AssertTrue(longBallManager.isWaitingForAccuracyRoll, "Difficulty 3 Long Ball should wait for accuracy immediately after the first click.");
        AssertTrue(MatchManager.Instance.currentState == MatchManager.GameState.LongBall, "MatchManager should be in LongBall state after the first valid click.", MatchManager.GameState.LongBall, MatchManager.Instance.currentState);

        Log("Rigging inaccurate Long Ball roll to 1");
        PerformRiggedLongBallAccuracyRoll(1);
        AssertTrue(longBallManager.isWaitingForDirectionRoll, "After a failed accuracy roll, Long Ball should wait for the direction roll.");

        LogFooterofTest("Long Ball Difficulty 3 First Click Commitment");
    }

    private IEnumerator Scenario_030c_LongBall_Inaccurate_No_Interception_GK_Forfeit_AutoMovement()
    {
        yield return StartCoroutine(PrepareManualLongBallBoardState(2));

        Log("Pressing L - Start Long Ball");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.L, 0.1f));
        Log("Clicking (6, -7) - Select Long Ball target");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(6, -7), 0.1f));
        Log("Clicking (6, -7) again - Confirm Long Ball target");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(6, -7), 0.1f));

        Log("Rigging inaccurate Long Ball roll to 1");
        PerformRiggedLongBallAccuracyRoll(1);
        Log("Rigging Long Ball direction to SouthWest (1)");
        PerformRiggedLongBallDirectionRoll(1);
        AssertTrue(longBallManager.isWaitingForDistanceRoll, "Long Ball should wait for the distance roll after the direction roll.");

        Log("Rigging Long Ball distance to 1 - No interceptions");
        StartRiggedLongBallDistanceRollAsync(1);
        yield return StartCoroutine(WaitForCondition(
            () => longBallManager.isWaitingForDefLBMove,
            4f,
            "GK pace move should be offered after an inaccurate Long Ball with no defender interception."));

        Log("Pressing X - Forfeit GK pace move");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        yield return new WaitForSeconds(0.3f);

        AvailabilityCheckResult availabilityCheck = AssertCorrectAvailabilityAfterGBToSpace();
        AssertTrue(availabilityCheck.passed, "After inaccurate Long Ball with no interceptions, MP should be auto-committed for attack.", true, availabilityCheck.ToString());

        LogFooterofTest("Long Ball Inaccurate No Interception GK Forfeit AutoMovement");
    }

    private IEnumerator Scenario_030d_LongBall_Inaccurate_Delgado_Interception_Success_Broadcasts_AnyOtherScenario()
    {
        yield return StartCoroutine(PrepareManualLongBallBoardState(2));

        Log("Pressing L - Start Long Ball");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.L, 0.1f));
        Log("Clicking (6, -7) - Select Long Ball target");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(6, -7), 0.1f));
        Log("Clicking (6, -7) again - Confirm Long Ball target");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(6, -7), 0.1f));

        Log("Rigging inaccurate Long Ball roll to 1");
        PerformRiggedLongBallAccuracyRoll(1);
        Log("Rigging Long Ball direction to SouthWest (1)");
        PerformRiggedLongBallDirectionRoll(1);
        Log("Rigging Long Ball distance to 4 - Offer Delgado interception");
        StartRiggedLongBallDistanceRollAsync(4);
        yield return StartCoroutine(WaitForCondition(
            () => IsWaitingForLongBallInterceptionRoll(),
            4f,
            "Long Ball should be waiting for Delgado interception."));

        PlayerToken delgado = RequirePlayerToken("Delgado");
        MatchManager.PlayerStats delgadoStatsBefore = MatchManager.Instance.gameData.stats.GetPlayerStats(delgado.playerName);
        float xRecoveryBefore = delgadoStatsBefore.xRecoveries;
        int interceptionsAttemptedBefore = delgadoStatsBefore.interceptionsAttempted;
        int interceptionsMadeBefore = delgadoStatsBefore.interceptionsMade;
        int possessionWonBefore = delgadoStatsBefore.possessionWon;

        Log("Rigging Delgado interception roll to 6");
        yield return StartCoroutine(PerformRiggedLongBallInterceptionRoll(6));
        yield return new WaitForSeconds(0.3f);

        AvailabilityCheckResult anyOtherAvailability = AssertCorrectAvailabilityAnyOtherScenario();
        AssertTrue(anyOtherAvailability.passed, "Successful Delgado interception on a Long Ball should broadcast AnyOtherScenario.", true, anyOtherAvailability.ToString());
        AssertTrue(MatchManager.Instance.LastTokenToTouchTheBallOnPurpose == delgado, "Delgado should be the last token after the successful long-ball interception.", delgado, MatchManager.Instance.LastTokenToTouchTheBallOnPurpose);

        MatchManager.PlayerStats delgadoStatsAfter = MatchManager.Instance.gameData.stats.GetPlayerStats(delgado.playerName);
        AssertTrue(delgadoStatsAfter.interceptionsAttempted == interceptionsAttemptedBefore + 1, "Delgado should log one interception attempt.", interceptionsAttemptedBefore + 1, delgadoStatsAfter.interceptionsAttempted);
        AssertTrue(delgadoStatsAfter.interceptionsMade == interceptionsMadeBefore + 1, "Delgado should log one successful interception.", interceptionsMadeBefore + 1, delgadoStatsAfter.interceptionsMade);
        AssertTrue(delgadoStatsAfter.possessionWon == possessionWonBefore + 1, "Delgado should log one possession won from the long-ball interception.", possessionWonBefore + 1, delgadoStatsAfter.possessionWon);
        AssertApproximately(delgadoStatsAfter.xRecoveries, xRecoveryBefore + CalculateExpectedRecoveryFromTackling(delgado.tackling), 0.0001f, "Delgado xRecovery should use the standard interception rule on the long ball.");

        LogFooterofTest("Long Ball Inaccurate Delgado Interception Success");
    }

    private IEnumerator Scenario_030e_LongBall_Inaccurate_Delgado_Interception_Fails_GK_Forfeit_AutoMovement()
    {
        yield return StartCoroutine(PrepareManualLongBallBoardState(2));

        Log("Pressing L - Start Long Ball");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.L, 0.1f));
        Log("Clicking (6, -7) - Select Long Ball target");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(6, -7), 0.1f));
        Log("Clicking (6, -7) again - Confirm Long Ball target");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(6, -7), 0.1f));

        Log("Rigging inaccurate Long Ball roll to 1");
        PerformRiggedLongBallAccuracyRoll(1);
        Log("Rigging Long Ball direction to SouthWest (1)");
        PerformRiggedLongBallDirectionRoll(1);
        Log("Rigging Long Ball distance to 4 - Offer Delgado interception");
        StartRiggedLongBallDistanceRollAsync(4);
        yield return StartCoroutine(WaitForCondition(
            () => IsWaitingForLongBallInterceptionRoll(),
            4f,
            "Long Ball should be waiting for Delgado interception."));

        Log("Rigging Delgado interception roll to 1 - Fail");
        StartRiggedLongBallInterceptionRollAsync(1);
        yield return StartCoroutine(WaitForCondition(
            () => longBallManager.isWaitingForDefLBMove,
            4f,
            "After Delgado fails the interception, GK pace move should be offered."));

        Log("Pressing X - Forfeit GK pace move");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        yield return new WaitForSeconds(0.3f);

        AvailabilityCheckResult failedInterceptionAvailability = AssertCorrectAvailabilityAfterGBToSpace();
        AssertTrue(failedInterceptionAvailability.passed, "After failed Delgado interception and forfeited GK move, MP should be auto-committed for attack.", true, failedInterceptionAvailability.ToString());

        LogFooterofTest("Long Ball Inaccurate Delgado Interception Fails");
    }

    private IEnumerator Scenario_030f_LongBall_Inaccurate_Lands_On_Delgado_Broadcasts_AnyOtherScenario()
    {
        yield return StartCoroutine(PrepareManualLongBallBoardState(2));

        Log("Pressing L - Start Long Ball");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.L, 0.1f));
        Log("Clicking (6, -7) - Select Long Ball target");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(6, -7), 0.1f));
        Log("Clicking (6, -7) again - Confirm Long Ball target");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(6, -7), 0.1f));

        Log("Rigging inaccurate Long Ball roll to 1");
        PerformRiggedLongBallAccuracyRoll(1);
        Log("Rigging Long Ball direction to SouthWest (1)");
        PerformRiggedLongBallDirectionRoll(1);

        PlayerToken delgado = RequirePlayerToken("Delgado");
        MatchManager.PlayerStats delgadoStatsBefore = MatchManager.Instance.gameData.stats.GetPlayerStats(delgado.playerName);
        int possessionWonBefore = delgadoStatsBefore.possessionWon;

        Log("Rigging Long Ball distance to 5 - Ball lands directly on Delgado");
        yield return StartCoroutine(PerformRiggedLongBallDistanceRoll(5));
        yield return new WaitForSeconds(0.3f);

        AvailabilityCheckResult directDefenderAvailability = AssertCorrectAvailabilityAnyOtherScenario();
        AssertTrue(directDefenderAvailability.passed, "Direct Long Ball recovery by Delgado should broadcast AnyOtherScenario.", true, directDefenderAvailability.ToString());
        AssertTrue(MatchManager.Instance.LastTokenToTouchTheBallOnPurpose == delgado, "Delgado should be the last token after direct Long Ball recovery.", delgado, MatchManager.Instance.LastTokenToTouchTheBallOnPurpose);

        MatchManager.PlayerStats delgadoStatsAfter = MatchManager.Instance.gameData.stats.GetPlayerStats(delgado.playerName);
        AssertTrue(delgadoStatsAfter.possessionWon == possessionWonBefore + 1, "Delgado should record one possession won from direct long-ball recovery.", possessionWonBefore + 1, delgadoStatsAfter.possessionWon);

        LogFooterofTest("Long Ball Inaccurate Lands On Delgado");
    }

    private IEnumerator Scenario_031a_LongBall_OppositeF3_Inaccurate_On_Yaneva_Offers_Snapshot_Or_Movement()
    {
        yield return StartCoroutine(StartPreparedLongBallToTarget(2, new Vector2Int(10, -6), "4+"));

        Log("Rigging inaccurate Long Ball roll to 1");
        PerformRiggedLongBallAccuracyRoll(1);
        AssertTrue(longBallManager.isWaitingForDirectionRoll, "After a failed opposite-F3 accuracy roll, Long Ball should wait for the direction roll.");

        Log("Rigging Long Ball direction to North (3)");
        PerformRiggedLongBallDirectionRoll(3);
        AssertTrue(longBallManager.isWaitingForDistanceRoll, "Long Ball should wait for the distance roll after the North direction roll.");

        Log("Rigging Long Ball distance to 6 - Ball should land on Yaneva");
        StartRiggedLongBallDistanceRollAsync(6);

        PlayerToken yaneva = RequirePlayerToken("Yaneva");
        yield return StartCoroutine(WaitForCondition(
            () => longBallManager.ball.GetCurrentHex() == yaneva.GetCurrentHex() && longBallManager.isWaitingForDefLBMove,
            4f,
            "The inaccurate long ball should land on Yaneva and offer the defending GK pace move."));

        Log("Pressing X - Forfeit GK pace move");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        yield return new WaitForSeconds(0.3f);

        AssertTrue(MatchManager.Instance.LastTokenToTouchTheBallOnPurpose == yaneva, "Yaneva should be the last token after the inaccurate long ball lands on her.", yaneva, MatchManager.Instance.LastTokenToTouchTheBallOnPurpose);
        AssertTrue(finalThirdManager.isActivated, "Final Third should be offered after the inaccurate long ball resolves on Yaneva.");

        yield return StartCoroutine(ForfeitActiveFinalThirds());

        AvailabilityCheckResult postLongBallChoice = AssertCorrectAvailabilityAfterLongBallSnapshotChoice();
        AssertTrue(
            postLongBallChoice.passed,
            "After the opposite-F3 inaccurate long ball lands on Yaneva, the attack should get Snapshot or Movement.",
            true,
            postLongBallChoice.ToString()
        );

        LogFooterofTest("Long Ball Opposite F3 Inaccurate On Yaneva");
    }

    private IEnumerator Scenario_031b_LongBall_Box_GK_Free_Move_Then_Kuzmic_Recovery_Broadcasts_AnyOtherScenario()
    {
        yield return StartCoroutine(StartPreparedLongBallToTarget(2, new Vector2Int(10, -6), "4+"));

        PlayerToken kuzmic = RequirePlayerToken("Kuzmic");
        PlayerToken passer = RequirePlayerToken("Ulisses");
        MatchManager.PlayerStats kuzmicStatsBefore = MatchManager.Instance.gameData.stats.GetPlayerStats(kuzmic.playerName);
        MatchManager.PlayerStats passerStatsBefore = MatchManager.Instance.gameData.stats.GetPlayerStats(passer.playerName);
        int possessionWonBefore = kuzmicStatsBefore.possessionWon;
        int passerPossessionLostBefore = passerStatsBefore.possessionLost;

        Log("Rigging inaccurate Long Ball roll to 1");
        PerformRiggedLongBallAccuracyRoll(1);
        Log("Rigging Long Ball direction to NorthEast (4)");
        PerformRiggedLongBallDirectionRoll(4);
        Log("Rigging Long Ball distance to 4 - Ball should enter the defensive box");
        StartRiggedLongBallDistanceRollAsync(4);
        yield return StartCoroutine(WaitForCondition(
            () => goalKeeperManager.isActivated,
            4f,
            "Defending GK should be offered the 1-hex free move when the long ball lands in the defensive box."));

        Log("Clicking (15, -1) - Move the defending GK for the free box move");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(15, -1), 0.1f));
        yield return StartCoroutine(WaitForCondition(
            () =>
                kuzmic.GetCurrentHex() == hexgrid.GetHexCellAt(new Vector3Int(15, 0, -1))
                && !goalKeeperManager.isActivated
                && !movementPhaseManager.isPlayerMoving,
            4f,
            "Kuzmic should complete the free box move to (15,-1)."));
        yield return StartCoroutine(WaitForCondition(
            () => longBallManager.isWaitingForDefLBMove,
            4f,
            "After the free GK move, Kuzmic should be offered the pace move."));

        AssertTrue(!IsWaitingForLongBallInterceptionRoll(), "No interception should be offered in the defensive-box Kuzmic recovery case.");

        HexCell ballHex = RequireHex(longBallManager.ball.GetCurrentHex(), "The ball should be on a valid hex before Kuzmic's pace recovery.");
        Log($"Clicking {ballHex.coordinates} - Move Kuzmic to collect the long ball");
        yield return StartCoroutine(gameInputManager.DelayedClick(ToClickCoordinates(ballHex), 0.1f));
        yield return StartCoroutine(WaitForCondition(
            () =>
                kuzmic.GetCurrentHex() == ballHex
                && MatchManager.Instance.LastTokenToTouchTheBallOnPurpose == kuzmic
                && finalThirdManager.isActivated,
            4f,
            "Final Third should be offered after Kuzmic recovers the long ball."));
        yield return StartCoroutine(ForfeitActiveFinalThirds());

        AvailabilityCheckResult anyOtherAvailability = AssertCorrectAvailabilityAnyOtherScenario();
        AssertTrue(
            anyOtherAvailability.passed,
            "Kuzmic's defensive-box long-ball recovery should broadcast AnyOtherScenario after Final Thirds.",
            true,
            anyOtherAvailability.ToString()
        );
        AssertTrue(MatchManager.Instance.LastTokenToTouchTheBallOnPurpose == kuzmic, "Kuzmic should be the last token after recovering the long ball.", kuzmic, MatchManager.Instance.LastTokenToTouchTheBallOnPurpose);

        MatchManager.PlayerStats kuzmicStatsAfter = MatchManager.Instance.gameData.stats.GetPlayerStats(kuzmic.playerName);
        MatchManager.PlayerStats passerStatsAfter = MatchManager.Instance.gameData.stats.GetPlayerStats(passer.playerName);
        AssertTrue(kuzmicStatsAfter.possessionWon == possessionWonBefore + 1, "Kuzmic should log one possession won from the long-ball recovery.", possessionWonBefore + 1, kuzmicStatsAfter.possessionWon);
        AssertTrue(passerStatsAfter.possessionLost == passerPossessionLostBefore + 1, "Ulisses should log one possession lost from Kuzmic's long-ball recovery.", passerPossessionLostBefore + 1, passerStatsAfter.possessionLost);

        LogFooterofTest("Long Ball Box GK Free Move Then Kuzmic Recovery");
    }

    private IEnumerator Scenario_031c_LongBall_Box_Poulsen_Interception_Fails_GK_Forfeit_EndOfLongBall()
    {
        yield return StartCoroutine(StartPreparedLongBallToTarget(2, new Vector2Int(10, -6), "4+"));

        PlayerToken poulsen = RequirePlayerToken("Poulsen");
        MatchManager.PlayerStats poulsenStatsBefore = MatchManager.Instance.gameData.stats.GetPlayerStats(poulsen.playerName);
        int interceptionsAttemptedBefore = poulsenStatsBefore.interceptionsAttempted;
        int interceptionsMadeBefore = poulsenStatsBefore.interceptionsMade;
        float xRecoveryBefore = poulsenStatsBefore.xRecoveries;

        Log("Rigging inaccurate Long Ball roll to 1");
        PerformRiggedLongBallAccuracyRoll(1);
        Log("Rigging Long Ball direction to NorthEast (4)");
        PerformRiggedLongBallDirectionRoll(4);
        Log("Rigging Long Ball distance to 6 - Ball should enter the defensive box and offer Poulsen an interception");
        StartRiggedLongBallDistanceRollAsync(6);
        yield return StartCoroutine(WaitForCondition(
            () => goalKeeperManager.isActivated,
            4f,
            "Defending GK should be offered the 1-hex free move before the Poulsen interception case."));

        Log("Clicking (15, -1) - Move the defending GK for the free box move");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(15, -1), 0.1f));
        yield return StartCoroutine(WaitForCondition(
            () =>
            {
                PlayerToken defendingGk = hexgrid.GetDefendingGK();
                HexCell gkDestination = hexgrid.GetHexCellAt(new Vector3Int(15, 0, -1));
                return defendingGk != null
                    && gkDestination != null
                    && defendingGk.GetCurrentHex() == gkDestination
                    && !goalKeeperManager.isActivated
                    && !movementPhaseManager.isPlayerMoving;
            },
            4f,
            "The defending GK should complete the free move to (15,-1)."));
        yield return StartCoroutine(WaitForCondition(
            () => IsWaitingForLongBallInterceptionRoll(),
            4f,
            "After the free GK move, Poulsen should be offered a long-ball interception."));

        AssertTrue(GetLongBallInterceptionCandidateCount() == 1, "Exactly one defender should be in the long-ball interception list for the Poulsen case.", 1, GetLongBallInterceptionCandidateCount());
        AssertTrue(GetFirstLongBallInterceptionCandidateName() == poulsen.playerName, "Poulsen should be the defender offered the interception.", poulsen.playerName, GetFirstLongBallInterceptionCandidateName());

        Log("Rigging Poulsen interception roll to 1 - Fail");
        StartRiggedLongBallInterceptionRollAsync(1);
        yield return StartCoroutine(WaitForCondition(
            () => longBallManager.isWaitingForDefLBMove,
            4f,
            "After Poulsen fails the interception, GK pace move should be offered."));

        MatchManager.PlayerStats poulsenStatsAfterInterception = MatchManager.Instance.gameData.stats.GetPlayerStats(poulsen.playerName);
        AssertTrue(poulsenStatsAfterInterception.interceptionsAttempted == interceptionsAttemptedBefore + 1, "Poulsen should log one interception attempt.", interceptionsAttemptedBefore + 1, poulsenStatsAfterInterception.interceptionsAttempted);
        AssertTrue(poulsenStatsAfterInterception.interceptionsMade == interceptionsMadeBefore, "Poulsen should not log a made interception after failing the roll.", interceptionsMadeBefore, poulsenStatsAfterInterception.interceptionsMade);
        AssertApproximately(poulsenStatsAfterInterception.xRecoveries, xRecoveryBefore + CalculateExpectedRecoveryFromTackling(poulsen.tackling), 0.0001f, "Poulsen should still log xRecovery on the failed long-ball interception attempt.");

        Log("Pressing X - Forfeit GK pace move");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
        yield return new WaitForSeconds(0.3f);

        AssertTrue(finalThirdManager.isActivated, "Final Third should be offered after the inaccurate long ball survives the failed Poulsen interception and GK pace forfeit.");
        yield return StartCoroutine(ForfeitActiveFinalThirds());

        AvailabilityCheckResult postLongBallAvailability = AssertCorrectAvailabilityAfterGBToSpace();
        AssertTrue(
            postLongBallAvailability.passed,
            "After the failed Poulsen interception and forfeited GK pace move, the attack should auto-commit to Movement Phase.",
            true,
            postLongBallAvailability.ToString()
        );

        LogFooterofTest("Long Ball Box Poulsen Interception Fails GK Forfeit");
    }

    private IEnumerator Scenario_031d_LongBall_CornerTarget_Inaccurate_NorthEast3_Is_GoalKick()
    {
        yield return StartCoroutine(StartPreparedLongBallToTarget(2, new Vector2Int(18, -12), null));

        Log("Rigging inaccurate Long Ball roll to 1");
        PerformRiggedLongBallAccuracyRoll(1);
        AssertTrue(longBallManager.isWaitingForDirectionRoll, "After the failed long-ball accuracy roll, the manager should wait for the direction roll.");

        Log("Rigging Long Ball direction to NorthEast (4)");
        PerformRiggedLongBallDirectionRoll(4);
        AssertTrue(longBallManager.isWaitingForDistanceRoll, "Long Ball should wait for the distance roll after the NorthEast direction roll.");

        Log("Rigging Long Ball distance to 3 - Should resolve to Goal Kick");
        yield return StartCoroutine(PerformRiggedLongBallDistanceRoll(3));
        yield return StartCoroutine(WaitForCondition(
            () =>
                MatchManager.Instance.currentState == MatchManager.GameState.WaitingForGoalKickFinalThirds
                && longBallManager.ball.GetCurrentHex() == hexgrid.GetHexCellAt(new Vector3Int(16, 0, 0))
                && !longBallManager.isActivated,
            5f,
            "Long Ball inaccurate NorthEast 3 from the corner target should resolve through OOB as a Goal Kick."));

        AssertTrue(
            MatchManager.Instance.currentState == MatchManager.GameState.WaitingForGoalKickFinalThirds,
            "Long Ball inaccurate NorthEast 3 from the corner target should be recognized as a Goal Kick.",
            MatchManager.GameState.WaitingForGoalKickFinalThirds,
            MatchManager.Instance.currentState
        );
        AssertTrue(
            longBallManager.ball.GetCurrentHex() == hexgrid.GetHexCellAt(new Vector3Int(16, 0, 0)),
            "Goal Kick resolution should place the ball on the defending goal-kick spot.",
            hexgrid.GetHexCellAt(new Vector3Int(16, 0, 0)),
            longBallManager.ball.GetCurrentHex()
        );

        LogFooterofTest("Long Ball Corner Target Inaccurate NorthEast3 Is GoalKick");
    }

    private IEnumerator Scenario_031e_LongBall_CornerTarget_Inaccurate_South3_Is_ThrowIn()
    {
        yield return StartCoroutine(StartPreparedLongBallToTarget(2, new Vector2Int(18, -12), null));

        Log("Rigging inaccurate Long Ball roll to 1");
        PerformRiggedLongBallAccuracyRoll(1);
        AssertTrue(longBallManager.isWaitingForDirectionRoll, "After the failed long-ball accuracy roll, the manager should wait for the direction roll.");

        Log("Rigging Long Ball direction to South (0)");
        PerformRiggedLongBallDirectionRoll(0);
        AssertTrue(longBallManager.isWaitingForDistanceRoll, "Long Ball should wait for the distance roll after the South direction roll.");

        Log("Rigging Long Ball distance to 3 - Should resolve to Throw-In");
        yield return StartCoroutine(PerformRiggedLongBallDistanceRoll(3));
        yield return StartCoroutine(WaitForCondition(
            () =>
                MatchManager.Instance.currentState == MatchManager.GameState.WaitingForThrowInTaker
                && longBallManager.ball.GetCurrentHex() != null
                && !longBallManager.ball.GetCurrentHex().isOutOfBounds
                && !longBallManager.isActivated,
            5f,
            "Long Ball inaccurate South 3 from the corner target should resolve through OOB as a Throw-In."));

        AssertTrue(
            MatchManager.Instance.currentState == MatchManager.GameState.WaitingForThrowInTaker,
            "Long Ball inaccurate South 3 from the corner target should be recognized as a Throw-In.",
            MatchManager.GameState.WaitingForThrowInTaker,
            MatchManager.Instance.currentState
        );
        AssertTrue(
            longBallManager.ball.GetCurrentHex() != null && !longBallManager.ball.GetCurrentHex().isOutOfBounds,
            "Throw-In resolution should return the ball to an in-bounds throw-in spot.",
            true,
            longBallManager.ball.GetCurrentHex()
        );

        LogFooterofTest("Long Ball Corner Target Inaccurate South3 Is ThrowIn");
    }

    private IEnumerator Scenario_031f_LongBall_To_15_4_Inaccurate_SouthEast6_Is_GoalKick_Not_Goal()
    {
        yield return StartCoroutine(StartPreparedLongBallToTarget(2, new Vector2Int(15, 4), null));

        Log("Rigging inaccurate Long Ball roll to 1");
        PerformRiggedLongBallAccuracyRoll(1);
        AssertTrue(longBallManager.isWaitingForDirectionRoll, "After the failed long-ball accuracy roll, the manager should wait for the direction roll.");

        Log("Rigging Long Ball direction to SouthEast (5)");
        PerformRiggedLongBallDirectionRoll(5);
        AssertTrue(longBallManager.isWaitingForDistanceRoll, "Long Ball should wait for the distance roll after the SouthEast direction roll.");

        Log("Rigging Long Ball distance to 6 - Should resolve to Goal Kick and not a Goal");
        yield return StartCoroutine(PerformRiggedLongBallDistanceRoll(6));
        yield return StartCoroutine(WaitForCondition(
            () =>
                MatchManager.Instance.currentState == MatchManager.GameState.WaitingForGoalKickFinalThirds
                && longBallManager.ball.GetCurrentHex() == hexgrid.GetHexCellAt(new Vector3Int(16, 0, 0))
                && !longBallManager.isActivated,
            5f,
            "Long Ball to (15,4) with SouthEast 6 inaccuracy should resolve through OOB as a Goal Kick."));

        AssertTrue(
            MatchManager.Instance.currentState == MatchManager.GameState.WaitingForGoalKickFinalThirds,
            "Long Ball to (15,4) with SouthEast 6 inaccuracy should be recognized as a Goal Kick, not a Goal.",
            MatchManager.GameState.WaitingForGoalKickFinalThirds,
            MatchManager.Instance.currentState
        );
        AssertTrue(
            longBallManager.ball.GetCurrentHex() == hexgrid.GetHexCellAt(new Vector3Int(16, 0, 0)),
            "Goal Kick resolution should place the ball on the defending goal-kick spot rather than treating it as a goal.",
            hexgrid.GetHexCellAt(new Vector3Int(16, 0, 0)),
            longBallManager.ball.GetCurrentHex()
        );

        LogFooterofTest("Long Ball To 15 4 Inaccurate SouthEast6 Is GoalKick");
    }



  private void AssertTrue(bool condition, string message, object expected = null, object actual = null)
  {
    if (!condition)
    {
      string failMessage = $"❌ ASSERT FAILED: {message}";
      if (expected != null || actual != null) { failMessage += $" | Expected: {expected} | Actual: {actual}"; }
      Debug.LogError(failMessage);
      File.AppendAllText(logFilePath, failMessage + "\n");
      testFailed = true;
      StopAllCoroutines(); // freeze test
    }
    else
    {
      string passMessage = $"✅ PASS: {message}";
      if (expected != null || actual != null) { passMessage += $" | Expected: {expected} | Actual: {actual}"; }

      Debug.Log(passMessage);
      File.AppendAllText(logFilePath, passMessage + "\n");
    }
  }

  private void AssertApproximately(float actual, float expected, float tolerance, string message)
  {
    AssertTrue(
        Mathf.Abs(actual - expected) <= tolerance,
        message,
        expected,
        actual);
  }

    private static float CalculateExpectedRecoveryFromTackling(int tackling, int minimumNaturalRollForSuccess = 6)
    {
        int successfulRolls = 0;
        for (int roll = 1; roll <= 6; roll++)
        {
            if (roll >= minimumNaturalRollForSuccess || roll + tackling >= 10)
            {
                successfulRolls++;
            }
        }

        return successfulRolls / 6f;
    }

    private static (float xDribbles, float xTackles) CalculateExpectedGroundDuel(
        int attackerDribbling,
        int defenderTackling,
        int defenderBonusMalus = 0)
    {
        (int effectiveRoll, float probability)[] outcomes =
        {
            (1, 1f / 6f),
            (2, 1f / 6f),
            (3, 1f / 6f),
            (4, 1f / 6f),
            (5, 1f / 6f),
            (6, 1f / 12f),
            (50, 1f / 12f),
        };

        float attackerWins = 0f;
        float defenderWins = 0f;
        float ties = 0f;

        foreach ((int defenderRoll, float defenderProbability) in outcomes)
        {
            foreach ((int attackerRoll, float attackerProbability) in outcomes)
            {
                float branchProbability = defenderProbability * attackerProbability;

                if (defenderRoll <= 1)
                {
                    continue;
                }

                int defenderTotal = defenderRoll == 50
                    ? 50
                    : defenderTackling + defenderBonusMalus + defenderRoll;
                int attackerTotal = attackerRoll == 50
                    ? 50
                    : attackerDribbling + attackerRoll;

                if (defenderTotal > attackerTotal)
                {
                    defenderWins += branchProbability;
                }
                else if (defenderTotal < attackerTotal)
                {
                    attackerWins += branchProbability;
                }
                else
                {
                    ties += branchProbability;
                }
            }
        }

        return (attackerWins + (ties / 6f), defenderWins + ((ties * 5f) / 6f));
    }

    private AvailabilityCheckResult AssertCorrectAvailabilityAfterGBToPlayer()
    {
        List<string> failures = new();

        if (!firstTimePassManager.isAvailable) failures.Add("FirstTimePass should be available");
        if (!movementPhaseManager.isAvailable) failures.Add("MovementPhase should be available");
        if (highPassManager.isAvailable) failures.Add("HighPass should NOT be available");
        if (longBallManager.isAvailable) failures.Add("LongBall should NOT be available");
        if (groundBallManager.isAvailable) failures.Add("GroundBall should NOT be available");

        if (movementPhaseManager.isActivated) failures.Add("MovementPhase should not be activated");
        if (firstTimePassManager.isActivated) failures.Add("FirstTimePass should not be activated");
        if (groundBallManager.isActivated) failures.Add("GroundBall should not be activated");
        if (highPassManager.isActivated) failures.Add("HighPass should not be activated");
        if (longBallManager.isActivated) failures.Add("LongBall should not be activated");
        if (!MatchManager.Instance.attackHasPossession) failures.Add("Attack has no possession after ball movement");

        return new AvailabilityCheckResult(failures.Count == 0, failures);
    }
    
    private AvailabilityCheckResult AssertCorrectAvailabilityAfterGBToSpace()
    {
        List<string> failures = new();

        if (firstTimePassManager.isAvailable) failures.Add("FirstTimePass should not be available");
        if (movementPhaseManager.isAvailable) failures.Add("MovementPhase should NOT be available");
        if (highPassManager.isAvailable) failures.Add("HighPass should NOT be available");
        if (longBallManager.isAvailable) failures.Add("LongBall should NOT be available");
        if (groundBallManager.isAvailable) failures.Add("GroundBall should NOT be available");

        if (!movementPhaseManager.isActivated) failures.Add("MovementPhase should be activated");
        if (firstTimePassManager.isActivated) failures.Add("FirstTimePass should not be activated");
        if (groundBallManager.isActivated) failures.Add("GroundBall should not be activated");
        if (highPassManager.isActivated) failures.Add("HighPass should not be activated");
        if (longBallManager.isActivated) failures.Add("LongBall should not be activated");
        if (MatchManager.Instance.attackHasPossession) failures.Add("Attack has possession after ball movement");

        return new AvailabilityCheckResult(failures.Count == 0, failures);
    }
    
    private AvailabilityCheckResult AssertCorrectAvailabilityAnyOtherScenario()
    {
        List<string> failures = new();

        if (firstTimePassManager.isAvailable) failures.Add("FirstTimePass should not be available");
        if (!movementPhaseManager.isAvailable) failures.Add("MovementPhase should be available");
        if (highPassManager.isAvailable) failures.Add("HighPass should not be available");
        if (!longBallManager.isAvailable) failures.Add("LongBall should be available");
        if (!groundBallManager.isAvailable) failures.Add("GroundBall should be available");

        if (movementPhaseManager.isActivated) failures.Add("MovementPhase should not be activated");
        if (firstTimePassManager.isActivated) failures.Add("FirstTimePass should not be activated");
        if (groundBallManager.isActivated) failures.Add("GroundBall should not be activated");
        if (highPassManager.isActivated) failures.Add("HighPass should not be activated");
        if (longBallManager.isActivated) failures.Add("LongBall should not be activated");
        if (!MatchManager.Instance.attackHasPossession) failures.Add("Attack has no possession after ball movement");
        if (groundBallManager.imposedDistance != 6) failures.Add($"GroundBall imposed distance should be 6 in AnyOtherScenario, but was {groundBallManager.imposedDistance}");

        return new AvailabilityCheckResult(failures.Count == 0, failures);
    }

    private AvailabilityCheckResult AssertCorrectWaitinginFTPInitialization()
    {
        List<string> failures = new();

        if (firstTimePassManager.isAvailable) failures.Add("FirstTimePass should not be available");
        if (!movementPhaseManager.isAvailable) failures.Add("MovementPhase should be available");
        if (groundBallManager.isAvailable) failures.Add("GroundBall should NOT be available");
        if (highPassManager.isAvailable) failures.Add("HighPass should NOT be available");
        if (longBallManager.isAvailable) failures.Add("LongBall should NOT be available");

        if (!firstTimePassManager.isAwaitingTargetSelection) failures.Add("FirstTimePass should be waiting for target selection");
        if (firstTimePassManager.isWaitingForAttackerSelection) failures.Add("FirstTimePass should not be waiting for attacker selection");
        if (firstTimePassManager.isWaitingForAttackerMove) failures.Add("FirstTimePass should not be waiting for attacker move");
        if (firstTimePassManager.isWaitingForDefenderSelection) failures.Add("FirstTimePass should not be waiting for defender selection");
        if (firstTimePassManager.isWaitingForDefenderMove) failures.Add("FirstTimePass should not be waiting for defender move");
        if (firstTimePassManager.isWaitingForDiceRoll) failures.Add("FirstTimePass should not be waiting for dice roll");

        if (movementPhaseManager.isActivated) failures.Add("MovementPhase should not be activated");
        if (!firstTimePassManager.isActivated) failures.Add("FirstTimePass should be activated");
        if (groundBallManager.isActivated) failures.Add("GroundBall should not be activated");
        if (highPassManager.isActivated) failures.Add("HighPass should not be activated");
        if (longBallManager.isActivated) failures.Add("LongBall should not be activated");
        if (!MatchManager.Instance.attackHasPossession) failures.Add("Attack has no possession after ball movement");

        return new AvailabilityCheckResult(failures.Count == 0, failures);
    }
    
    private AvailabilityCheckResult AssertCorrectWaitinginFTPAttackerMovementPhase()
    {
        List<string> failures = new();

        if (firstTimePassManager.isAvailable) failures.Add("FirstTimePass should NOT be available");
        if (movementPhaseManager.isAvailable) failures.Add("MovementPhase should NOTbe available");
        if (groundBallManager.isAvailable) failures.Add("GroundBall should NOT  be available");
        if (highPassManager.isAvailable) failures.Add("HighPass should NOTbe available");
        if (longBallManager.isAvailable) failures.Add("LongBall should NOT be available");

        if (firstTimePassManager.isAwaitingTargetSelection) failures.Add("FirstTimePass should be NOT waiting for target selection");
        if (!firstTimePassManager.isWaitingForAttackerSelection) failures.Add("FirstTimePass should be waiting for attacker selection");
        if (firstTimePassManager.isWaitingForDefenderSelection) failures.Add("FirstTimePass should not be waiting for defender selection");
        if (firstTimePassManager.isWaitingForDefenderMove) failures.Add("FirstTimePass should not be waiting for defender move");
        if (firstTimePassManager.isWaitingForDiceRoll) failures.Add("FirstTimePass should not be waiting for dice roll");

        if (movementPhaseManager.isActivated) failures.Add("MovementPhase should not be activated");
        if (!firstTimePassManager.isActivated) failures.Add("FirstTimePass should not be activated");
        if (groundBallManager.isActivated) failures.Add("GroundBall should not be activated");
        if (highPassManager.isActivated) failures.Add("HighPass should not be activated");
        if (longBallManager.isActivated) failures.Add("LongBall should not be activated");
        if (!MatchManager.Instance.attackHasPossession) failures.Add("Attack has no possession after ball movement");

        return new AvailabilityCheckResult(failures.Count == 0, failures);
    }
    
    private AvailabilityCheckResult AssertCorrectWaitinginFTPDefenderMovementPhase()
    {
        List<string> failures = new();

        if (firstTimePassManager.isAvailable) failures.Add("FirstTimePass should NOT be available");
        if (movementPhaseManager.isAvailable) failures.Add("MovementPhase should NOTbe available");
        if (groundBallManager.isAvailable) failures.Add("GroundBall should NOT  be available");
        if (highPassManager.isAvailable) failures.Add("HighPass should NOTbe available");
        if (longBallManager.isAvailable) failures.Add("LongBall should NOT be available");

        if (firstTimePassManager.isAwaitingTargetSelection) failures.Add("FirstTimePass should be NOT waiting for target selection");
        if (firstTimePassManager.isWaitingForAttackerSelection) failures.Add("FirstTimePass should NOT be waiting for attacker selection");
        if (firstTimePassManager.isWaitingForAttackerMove) failures.Add("FirstTimePass should NOT be waiting for defender move");
        if (!firstTimePassManager.isWaitingForDefenderSelection) failures.Add("FirstTimePass should be waiting for defender selection");
        if (firstTimePassManager.isWaitingForDiceRoll) failures.Add("FirstTimePass should NOT be waiting for dice roll");
        if (MatchManager.Instance.currentState != MatchManager.GameState.FirstTimePassDefenderMovement) failures.Add($"MatchManager should be in {MatchManager.GameState.FirstTimePassDefenderMovement}");

        if (movementPhaseManager.isActivated) failures.Add("MovementPhase should not be activated");
        if (!firstTimePassManager.isActivated) failures.Add("FirstTimePass should be activated");
        if (groundBallManager.isActivated) failures.Add("GroundBall should not be activated");
        if (highPassManager.isActivated) failures.Add("HighPass should not be activated");
        if (longBallManager.isActivated) failures.Add("LongBall should not be activated");
        if (!MatchManager.Instance.attackHasPossession) failures.Add("Attack has no possession after ball movement");

        return new AvailabilityCheckResult(failures.Count == 0, failures);
    }
    
    private AvailabilityCheckResult AssertCorrectAvailabilityAfterFTPToPlayer()
    {
        List<string> failures = new();

        if (movementPhaseManager.isAvailable) failures.Add("MovementPhase should NOT be available");
        if (firstTimePassManager.isAvailable) failures.Add("FirstTimePass should NOT be available");
        if (highPassManager.isAvailable) failures.Add("HighPass should NOT be available");
        if (longBallManager.isAvailable) failures.Add("LongBall should NOT be available");
        if (groundBallManager.isAvailable) failures.Add("GroundBall should NOT be available");

        if (!movementPhaseManager.isActivated) failures.Add("MovementPhase should be activated");
        if (firstTimePassManager.isActivated) failures.Add("FirstTimePass should NOT be activated");
        if (groundBallManager.isActivated) failures.Add("GroundBall should not be activated");
        if (highPassManager.isActivated) failures.Add("HighPass should not be activated");
        if (longBallManager.isActivated) failures.Add("LongBall should not be activated");
        if (!MatchManager.Instance.attackHasPossession) failures.Add("Attack has no possession after ball movement");

        return new AvailabilityCheckResult(failures.Count == 0, failures);
    }
    
    private AvailabilityCheckResult AssertCorrectAvailabilityAfterFTPToSpace()
    {
        List<string> failures = new();

        if (movementPhaseManager.isAvailable) failures.Add("MovementPhase should NOT be available");
        if (firstTimePassManager.isAvailable) failures.Add("FirstTimePass should not be available");
        if (highPassManager.isAvailable) failures.Add("HighPass should NOT be available");
        if (longBallManager.isAvailable) failures.Add("LongBall should NOT be available");
        if (groundBallManager.isAvailable) failures.Add("GroundBall should NOT be available");

        if (!movementPhaseManager.isActivated) failures.Add("MovementPhase should be activated");
        if (firstTimePassManager.isActivated) failures.Add("FirstTimePass should NOT be activated");
        if (groundBallManager.isActivated) failures.Add("GroundBall should not be activated");
        if (highPassManager.isActivated) failures.Add("HighPass should not be activated");
        if (longBallManager.isActivated) failures.Add("LongBall should not be activated");
        if (MatchManager.Instance.attackHasPossession) failures.Add("Attack has possession after ball movement");

        return new AvailabilityCheckResult(failures.Count == 0, failures);
    }

    private AvailabilityCheckResult AssertCorrectAvailabilityAfterLongBallSnapshotChoice()
    {
        List<string> failures = new();

        if (!movementPhaseManager.isAvailable) failures.Add("MovementPhase should be available");
        if (movementPhaseManager.isActivated) failures.Add("MovementPhase should NOT be activated");

        if (!shotManager.isAvailable) failures.Add("ShotManager should be available for a Snapshot choice");
        if (shotManager.isActivated) failures.Add("ShotManager should NOT be activated");

        if (firstTimePassManager.isAvailable) failures.Add("FirstTimePass should NOT be available");
        if (groundBallManager.isAvailable) failures.Add("GroundBall should NOT be available");
        if (highPassManager.isAvailable) failures.Add("HighPass should NOT be available");
        if (longBallManager.isAvailable) failures.Add("LongBall should NOT be available");

        if (firstTimePassManager.isActivated) failures.Add("FirstTimePass should NOT be activated");
        if (groundBallManager.isActivated) failures.Add("GroundBall should NOT be activated");
        if (highPassManager.isActivated) failures.Add("HighPass should NOT be activated");
        if (longBallManager.isActivated) failures.Add("LongBall should NOT be activated");

        if (!MatchManager.Instance.attackHasPossession) failures.Add("Attack should have possession after long ball lands on an attacker");

        return new AvailabilityCheckResult(failures.Count == 0, failures);
    }

    private AvailabilityCheckResult AssertCorrectAvailabilityAfterMovementCommitment()
    {
        List<string> failures = new();
        if (movementPhaseManager.isAvailable) failures.Add("MovementPhase should NOT be available");
        if (!movementPhaseManager.isActivated) failures.Add("MovementPhase should be activated");
        if (groundBallManager.isAvailable) failures.Add("GroundBall should NOT be available");
        if (groundBallManager.isActivated) failures.Add("GroundBall should NOT be activated");
        if (firstTimePassManager.isAvailable) failures.Add("FirstTimePass should NOT be available");
        if (firstTimePassManager.isActivated) failures.Add("FirstTimePass should NOT be activated");
        if (highPassManager.isAvailable) failures.Add("HighPass should NOT be available");
        if (highPassManager.isActivated) failures.Add("HighPass should NOT be activated");
        if (longBallManager.isAvailable) failures.Add("LongBall should NOT be available");
        if (longBallManager.isActivated) failures.Add("LongBall should NOT be activated");
        return new AvailabilityCheckResult(failures.Count == 0, failures);
    }
    
    private AvailabilityCheckResult AssertCorrectAvailabilityAfterMovementComplete()
    {
        List<string> failures = new();
        if (MatchManager.Instance.attackHasPossession)
        {
            if (!movementPhaseManager.isAvailable) failures.Add("MovementPhase should be available");
            if (movementPhaseManager.isActivated) failures.Add("MovementPhase should NOT be activated");
            if (!groundBallManager.isAvailable) failures.Add("GroundBall should be available");
            if (groundBallManager.isActivated) failures.Add("GroundBall should NOT be activated");
            if (groundBallManager.imposedDistance != 11) failures.Add($"GroundBall imposed distance should be 11 after Movement Phase completion, but was {groundBallManager.imposedDistance}");
            if (firstTimePassManager.isAvailable) failures.Add("FirstTimePass should NOT be available");
            if (firstTimePassManager.isActivated) failures.Add("FirstTimePass should NOT be activated");
            if (!highPassManager.isAvailable) failures.Add("HighPass should be available");
            if (highPassManager.isActivated) failures.Add("HighPass should NOT be activated");
            if (!longBallManager.isAvailable) failures.Add("LongBall should be available");
            if (longBallManager.isActivated) failures.Add("LongBall should NOT be activated");
        }
        else
        {
            if (movementPhaseManager.isAvailable) failures.Add("MovementPhase should NOT be available");
            if (firstTimePassManager.isAvailable) failures.Add("FirstTimePass should not be available");
            if (highPassManager.isAvailable) failures.Add("HighPass should NOT be available");
            if (longBallManager.isAvailable) failures.Add("LongBall should NOT be available");
            if (groundBallManager.isAvailable) failures.Add("GroundBall should NOT be available");

            if (!movementPhaseManager.isActivated) failures.Add("MovementPhase should be activated");
            if (firstTimePassManager.isActivated) failures.Add("FirstTimePass should NOT be activated");
            if (groundBallManager.isActivated) failures.Add("GroundBall should not be activated");
            if (highPassManager.isActivated) failures.Add("HighPass should not be activated");
            if (longBallManager.isActivated) failures.Add("LongBall should not be activated");
        }
        return new AvailabilityCheckResult(failures.Count == 0, failures);
    }
    
    private AvailabilityCheckResult AssertCorrectAvailabilityAfterSuccessfulTackle()
    {
        List<string> failures = new();
        if (!movementPhaseManager.isAvailable) failures.Add("MovementPhase should be available");
        if (movementPhaseManager.isActivated) failures.Add("MovementPhase should NOT be activated");
        
        if (!groundBallManager.isAvailable) failures.Add("GroundBall should be available");
        if (groundBallManager.isActivated) failures.Add("GroundBall should NOT be activated");
        
        if (firstTimePassManager.isAvailable) failures.Add("FirstTimePass should NOT be available");
        if (firstTimePassManager.isActivated) failures.Add("FirstTimePass should NOT be activated");
        
        if (!highPassManager.isAvailable) failures.Add("HighPass should be available");
        if (highPassManager.isActivated) failures.Add("HighPass should NOT be activated");
        
        if (!longBallManager.isAvailable) failures.Add("LongBall should be available");
        if (longBallManager.isActivated) failures.Add("LongBall should NOT be activated");
        return new AvailabilityCheckResult(failures.Count == 0, failures);
    }
    
    private AvailabilityCheckResult AssertCorrectAvailabilityAfterHeadToPlayer()
    {
        List<string> failures = new();
        if (!movementPhaseManager.isAvailable) failures.Add("MovementPhase should be available");
        if (movementPhaseManager.isActivated) failures.Add("MovementPhase should NOT be activated");
        
        if (groundBallManager.isAvailable) failures.Add("GroundBall should NOT be available");
        if (groundBallManager.isActivated) failures.Add("GroundBall should NOT be activated");
        
        if (!firstTimePassManager.isAvailable) failures.Add("FirstTimePass should be available");
        if (firstTimePassManager.isActivated) failures.Add("FirstTimePass should NOT be activated");
        
        if (highPassManager.isAvailable) failures.Add("HighPass should NOT be available");
        if (highPassManager.isActivated) failures.Add("HighPass should NOT be activated");
        
        if (!longBallManager.isAvailable) failures.Add("LongBall should be available");
        if (longBallManager.isActivated) failures.Add("LongBall should NOT be activated");
        return new AvailabilityCheckResult(failures.Count == 0, failures);
    }
    
    private AvailabilityCheckResult AssertCorrectAvailabilityFreeKickTaken(HexCell foulpos)
    {
        List<string> failures = new();
        if (movementPhaseManager.isAvailable) failures.Add("MovementPhase should NOT be available");
        if (movementPhaseManager.isActivated) failures.Add("MovementPhase should NOT be activated");
        
        if (!groundBallManager.isAvailable) failures.Add("GroundBall should be available");
        if (groundBallManager.isActivated) failures.Add("GroundBall should NOT be activated");
        
        if (firstTimePassManager.isAvailable) failures.Add("FirstTimePass should NOT be available");
        if (firstTimePassManager.isActivated) failures.Add("FirstTimePass should NOT be activated");
        
        if (!highPassManager.isAvailable) failures.Add("HighPass should be available");
        if (highPassManager.isActivated) failures.Add("HighPass should NOT be activated");
        
        if (!longBallManager.isAvailable) failures.Add("LongBall should be available");
        if (longBallManager.isActivated) failures.Add("LongBall should NOT be activated");
        if (foulpos.CanShootFrom && !shotManager.isAvailable) failures.Add("Based on Foul position, wrong Shot availability");
        return new AvailabilityCheckResult(failures.Count == 0, failures);
    }
    
    private void Log(string message)
    {
        Debug.Log("LOG: " + message);
        File.AppendAllText(logFilePath, message + "\n");
        AppendOnScreenLog(message);
    }

    private void LogFooterofTest(string message)
    {
        if (!testFailed)
        {
            Log($"\n✅ {message} - TEST PASSED COMPLETELY 🎉\n");
        }
        else
        {
            Log($"\n❌ {message} - TEST FAILED SOMEWHERE\n");
        }
    }

}
