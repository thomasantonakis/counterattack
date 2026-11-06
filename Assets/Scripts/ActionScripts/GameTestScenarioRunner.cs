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

    private enum ShootingBranchStepType
    {
        ShotBlockRoll,
        ShooterRoll,
        GkSaveRoll,
        HandlingRoll,
        LooseDirectionRoll,
        LooseDistanceRoll,
        LooseInterceptionRoll,
        GoalkeeperMove,
        ExpectAnyOtherScenario,
        ExpectMovementToAttack,
        ExpectSaveAndHold,
        ExpectCornerNorth,
        ExpectCornerSouth,
        ExpectGoal,
        ExpectOwnGoal,
        ExpectOffTarget
    }

    private sealed class ShootingBranchStep
    {
        public ShootingBranchStepType Type;
        public string PlayerName;
        public int Roll;
        public Vector2Int Coordinates;
    }

    private sealed class ShootingBranchDefinition
    {
        public ShootingBranchDefinition(string name, params ShootingBranchStep[] steps)
        {
            Name = name;
            Steps = steps.ToList();
        }

        public string Name { get; }
        public List<ShootingBranchStep> Steps { get; }
    }

    private sealed class FreeKickShotBranchDefinition
    {
        public FreeKickShotBranchDefinition(string name, params ShootingBranchStep[] steps)
        {
            Name = name;
            Steps = steps.ToList();
        }

        public string Name { get; }
        public List<ShootingBranchStep> Steps { get; }
    }

    private enum OobRestartExpectation
    {
        ThrowIn,
        CornerKick,
        GoalKick
    }

    private sealed class OobLongBallBranchDefinition
    {
        public OobLongBallBranchDefinition(
            string name,
            Vector2Int target,
            int accuracyRoll,
            int sheetDirectionRoll,
            int distanceRoll,
            OobRestartExpectation expectedRestart,
            Vector2Int expectedReentryHex)
        {
            Name = name;
            Target = target;
            AccuracyRoll = accuracyRoll;
            SheetDirectionRoll = sheetDirectionRoll;
            DistanceRoll = distanceRoll;
            ExpectedRestart = expectedRestart;
            ExpectedReentryHex = expectedReentryHex;
        }

        public string Name { get; }
        public Vector2Int Target { get; }
        public int AccuracyRoll { get; }
        public int SheetDirectionRoll { get; }
        public int DistanceRoll { get; }
        public OobRestartExpectation ExpectedRestart { get; }
        public Vector2Int ExpectedReentryHex { get; }
    }

    private enum HeaderAtGoalOutcomeExpectation
    {
        Goal,
        CornerKick,
        MovementPhase,
        SaveAndHold
    }

    private sealed class HeaderAtGoalBranchDefinition
    {
        public HeaderAtGoalBranchDefinition(
            string name,
            bool gkRush,
            int attackerHeaderRoll,
            int defenderHeaderRoll,
            int? goalkeeperHeaderOrAerialRoll,
            int? handlingRoll,
            int? looseDirectionRoll,
            int? looseDistanceRoll,
            HeaderAtGoalOutcomeExpectation expectedOutcome,
            Vector2Int? expectedBallHex = null,
            string looseInterceptionPlayerName = null,
            int? looseInterceptionRoll = null,
            string forbiddenLooseInterceptorName = null)
        {
            Name = name;
            GkRush = gkRush;
            AttackerHeaderRoll = attackerHeaderRoll;
            DefenderHeaderRoll = defenderHeaderRoll;
            GoalkeeperHeaderOrAerialRoll = goalkeeperHeaderOrAerialRoll;
            HandlingRoll = handlingRoll;
            LooseDirectionRoll = looseDirectionRoll;
            LooseDistanceRoll = looseDistanceRoll;
            ExpectedOutcome = expectedOutcome;
            ExpectedBallHex = expectedBallHex;
            LooseInterceptionPlayerName = looseInterceptionPlayerName;
            LooseInterceptionRoll = looseInterceptionRoll;
            ForbiddenLooseInterceptorName = forbiddenLooseInterceptorName;
        }

        public string Name { get; }
        public bool GkRush { get; }
        public int AttackerHeaderRoll { get; }
        public int DefenderHeaderRoll { get; }
        public int? GoalkeeperHeaderOrAerialRoll { get; }
        public int? HandlingRoll { get; }
        public int? LooseDirectionRoll { get; }
        public int? LooseDistanceRoll { get; }
        public HeaderAtGoalOutcomeExpectation ExpectedOutcome { get; }
        public Vector2Int? ExpectedBallHex { get; }
        public string LooseInterceptionPlayerName { get; }
        public int? LooseInterceptionRoll { get; }
        public string ForbiddenLooseInterceptorName { get; }
    }

    // private bool shouldRunTests = false;
    private bool shouldRunTests = true;
    [Tooltip("Runs only the Codex kickoff receiver/first-pass regression when the user starts Play Mode.")]
    public bool runCodexKickoffOnly;
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
    public ThrowInManager throwInManager;
    public FreeKickManager freeKickManager;
    public ShotManager shotManager;
    public FinalThirdManager finalThirdManager;
    public GoalFlowManager goalFlowManager;
    public KickoffManager kickoffManager;
    public GoalKeeperManager goalKeeperManager;
    public HexGrid hexgrid;
    public static GameTestScenarioRunner Instance;
    private Canvas testLogCanvas;
    private TextMeshProUGUI testLogText;
    private readonly Queue<string> onScreenLogLines = new();
    private const int MaxOnScreenLogLines = 15;
    private string currentScenarioName = string.Empty;
    private readonly Dictionary<string, int> expectedShootingShotBlockAttempts = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> expectedShootingShotBlocksMade = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> expectedShootingLooseInterceptionAttempts = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> expectedShootingLooseRecoveriesMade = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, float> expectedShootingLooseXRecoveries = new(StringComparer.OrdinalIgnoreCase);
    private float? expectedShootingXGoals;
    private bool expectedShootingShotBlocked;
    private bool expectedShootingShotOnTarget;
    private bool expectedShootingShotOffTarget;
    
    private void Awake()
    {
#if UNITY_EDITOR
        if (UnityEditor.SessionState.GetBool("CounterAttack.FrameworkTests", false)) return;
#endif
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
        testLogText.textWrappingMode = TextWrappingModes.Normal;
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

        TMP_Text anySceneText = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include)
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
#if UNITY_EDITOR
        if (UnityEditor.SessionState.GetBool("CounterAttack.FrameworkTests", false)) return;
#endif
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

    private void AddShootingBranchScenarios(List<ScenarioDefinition> scenarios, int firstBranchIndex = 1)
    {
        int shootingBranchIndex = 1;
        foreach (ShootingBranchDefinition branch in BuildShootingBranchDefinitions())
        {
            if (shootingBranchIndex < firstBranchIndex)
            {
                shootingBranchIndex++;
                continue;
            }

            ShootingBranchDefinition capturedBranch = branch;
            scenarios.Add(new ScenarioDefinition(
                $"Scenario_034_{shootingBranchIndex:00}_Shooting_{BuildScenarioSafeName(capturedBranch.Name)}",
                () => Scenario_034_Shooting_Branch(capturedBranch)));
            shootingBranchIndex++;
        }
    }

    private void AddFreeKickShotBranchScenarios(List<ScenarioDefinition> scenarios)
    {
        Debug.Log("All twelve free-kick shot branches migrated to Unity Test Runner: LegacyFreeKickShot.");
    }

    private void AddHeaderAtGoalBranchScenarios(List<ScenarioDefinition> scenarios, int firstBranchIndex = 1)
    {
        int branchIndex = 1;
        foreach (HeaderAtGoalBranchDefinition branch in BuildHeaderAtGoalBranchDefinitions())
        {
            if (branchIndex < firstBranchIndex)
            {
                branchIndex++;
                continue;
            }

            HeaderAtGoalBranchDefinition capturedBranch = branch;
            scenarios.Add(new ScenarioDefinition(
                $"Scenario_038_{branchIndex:00}_HeaderAtGoal_{BuildScenarioSafeName(capturedBranch.Name)}",
                () => Scenario_038_HeaderAtGoal_Branch(capturedBranch)));
            branchIndex++;
        }

    }

    private void AddThrowInAuditScenarios(List<ScenarioDefinition> scenarios)
    {
        Debug.Log("Throw-in audit scenarios migrated to Unity Test Runner: LegacyThrowIn.");
    }

    private void AddSetPiecePolicyScenarios(List<ScenarioDefinition> scenarios)
    {
    }

    private void AddGkWallAuditScenarios(List<ScenarioDefinition> scenarios)
    {
        Debug.Log("Goalkeeper wall and outside-box audits migrated to Unity Test Runner.");
    }

    private void AddRestartClockAndSetPieceScenarios(List<ScenarioDefinition> scenarios)
    {
        Debug.Log("Restart clock and preparation audits migrated to Unity Test Runner.");
    }

    private IEnumerator RunAllScenarios()
    {
        var scenarios = new List<ScenarioDefinition>();
        bool runNutmegStealFoulAuditOnly = true;
        bool runManualStatsPreviewOnly = false;
        bool runLongBallAuditOnly = false;
        bool runHighPassAuditOnly = false;
        bool runHighPassHeaderAuditOnly = false;
        bool runShootingAuditOnly = false;
        bool runFreeKickPrepOnly = false;
        bool runFreeKickShotAuditOnly = false;
        bool runOobAuditOnly = false;
        bool runHeaderAtGoalAuditOnly = false;
        bool runThrowInAuditOnly = false;
        bool runSetPiecePolicyOnly = false;
        bool runGkWallAuditOnly = false;
        bool runReleasedOobAndHeaderAtGoalOnly = true;
        bool runFromCurrentFailureOnly = false;

        if (runCodexKickoffOnly)
        {
        }
        else if (runNutmegStealFoulAuditOnly)
        {
            Debug.Log("Nutmeg steal/foul audits migrated to Unity Test Runner: LegacyNutmegSelection.");
        }
        else if (runReleasedOobAndHeaderAtGoalOnly)
        {
            AddRestartClockAndSetPieceScenarios(scenarios);
            AddHeaderAtGoalBranchScenarios(scenarios);
        }
        else if (runManualStatsPreviewOnly)
        {
        }
        else if (runLongBallAuditOnly)
        {
            Debug.Log("Long-ball audit scenarios migrated to Unity Test Runner: LegacyLongBall and LegacyLongBallOob.");
        }
        else if (runHighPassAuditOnly)
        {
            Debug.Log("High-pass target audit scenarios migrated to Unity Test Runner: LegacyHighPassSelection.");
        }
        else if (runHighPassHeaderAuditOnly)
        {
            Debug.Log("Audit scenarios migrated to Unity Test Runner.");
            AddHeaderAtGoalBranchScenarios(scenarios);
        }
        else if (runShootingAuditOnly)
        {
            AddShootingBranchScenarios(scenarios);
        }
        else if (runFreeKickPrepOnly)
        {
        }
        else if (runFreeKickShotAuditOnly)
        {
            AddFreeKickShotBranchScenarios(scenarios);
        }
        else if (runOobAuditOnly)
        {
        }
        else if (runHeaderAtGoalAuditOnly)
        {
            AddHeaderAtGoalBranchScenarios(scenarios);
        }
        else if (runThrowInAuditOnly)
        {
            AddThrowInAuditScenarios(scenarios);
        }
        else if (runSetPiecePolicyOnly)
        {
            AddSetPiecePolicyScenarios(scenarios);
        }
        else if (runGkWallAuditOnly)
        {
            AddGkWallAuditScenarios(scenarios);
        }
        else
        {
            // Explicit scenarios are migrated; only unretired generated matrices follow.
            AddHeaderAtGoalBranchScenarios(scenarios);

            AddShootingBranchScenarios(scenarios);
            if (runFromCurrentFailureOnly)
            {
                scenarios.Clear();
                AddShootingBranchScenarios(scenarios, firstBranchIndex: 13);
            }
            AddFreeKickShotBranchScenarios(scenarios);
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

    private readonly struct OutsideBoxGoalkeeperChallengeSetup
    {
        public OutsideBoxGoalkeeperChallengeSetup(
            PlayerToken dribbler,
            PlayerToken goalkeeper,
            HexCell dribblerHex,
            HexCell goalkeeperStartHex,
            HexCell goalkeeperChallengeHex,
            HexCell boxRepositionHex)
        {
            Dribbler = dribbler;
            Goalkeeper = goalkeeper;
            DribblerHex = dribblerHex;
            GoalkeeperStartHex = goalkeeperStartHex;
            GoalkeeperChallengeHex = goalkeeperChallengeHex;
            BoxRepositionHex = boxRepositionHex;
        }

        public PlayerToken Dribbler { get; }
        public PlayerToken Goalkeeper { get; }
        public HexCell DribblerHex { get; }
        public HexCell GoalkeeperStartHex { get; }
        public HexCell GoalkeeperChallengeHex { get; }
        public HexCell BoxRepositionHex { get; }
    }

    private readonly struct OutsideBoxGoalkeeperShotSetup
    {
        public OutsideBoxGoalkeeperShotSetup(
            PlayerToken shooter,
            PlayerToken goalkeeper,
            PlayerToken blocker,
            HexCell shooterHex,
            HexCell goalkeeperHex,
            HexCell targetHex,
            List<HexCell> path)
        {
            Shooter = shooter;
            Goalkeeper = goalkeeper;
            Blocker = blocker;
            ShooterHex = shooterHex;
            GoalkeeperHex = goalkeeperHex;
            TargetHex = targetHex;
            Path = path;
        }

        public PlayerToken Shooter { get; }
        public PlayerToken Goalkeeper { get; }
        public PlayerToken Blocker { get; }
        public HexCell ShooterHex { get; }
        public HexCell GoalkeeperHex { get; }
        public HexCell TargetHex { get; }
        public List<HexCell> Path { get; }
    }

    private OutsideBoxGoalkeeperChallengeSetup PrepareOutsideBoxGoalkeeperChallengeBoard(
        bool dangerousPosition = false, bool reverseDirection = false)
    {
        MatchManager.Instance.difficulty_level = 2;
        if (MatchManager.Instance.gameData != null && MatchManager.Instance.gameData.gameSettings != null)
        {
            MatchManager.Instance.gameData.gameSettings.playerAssistance = 2;
        }

        MatchManager.Instance.homeTeamDirection = reverseDirection
            ? MatchManager.TeamAttackingDirection.RightToLeft : MatchManager.TeamAttackingDirection.LeftToRight;
        MatchManager.Instance.awayTeamDirection = reverseDirection
            ? MatchManager.TeamAttackingDirection.LeftToRight : MatchManager.TeamAttackingDirection.RightToLeft;
        EnsureTeamInAttackForTest(MatchManager.TeamInAttack.Home);
        MatchManager.Instance.attackHasPossession = true;

        PlayerToken dribbler = RequirePlayerToken("Yaneva");
        PlayerToken goalkeeper = RequirePlayerToken("Kuzmic");
        int side = reverseDirection ? -1 : 1;
        HexCell dribblerHex = RequireHex(hexgrid.GetHexCellAt(new Vector3Int(11 * side, 0, 0)), "Outside-box GK test dribbler hex should exist.");
        HexCell goalkeeperStartHex = RequireHex(hexgrid.GetHexCellAt(new Vector3Int((dangerousPosition ? 8 : 13) * side, 0, 0)), "Outside-box GK test GK start hex should exist.");
        HexCell goalkeeperChallengeHex = dangerousPosition
            ? RequireHex(dribblerHex.GetNeighbors(hexgrid).FirstOrDefault(hex => hex != null
                && dribblerHex.IsDangerousTacklingPosition(MatchManager.Instance.homeTeamDirection, hex)), "A cached dangerous tackle hex should exist behind the dribbler.")
            : RequireHex(hexgrid.GetHexCellAt(new Vector3Int(12 * side, 0, 0)), "Outside-box GK test GK challenge hex should exist.");

        MoveTokensOutOfFinalThirdForRestartTest(side, dribbler, goalkeeper);
        ClearHexForGkWallScenario(dribblerHex);
        ClearHexForGkWallScenario(goalkeeperStartHex);
        ClearHexForGkWallScenario(goalkeeperChallengeHex);

        PlaceTokenForScenario(dribbler, dribblerHex, asAttacker: true);
        PlaceTokenForScenario(goalkeeper, goalkeeperStartHex, asAttacker: false);
        groundBallManager.ball.PlaceAtCell(dribblerHex);
        MatchManager.Instance.LastTokenToTouchTheBallOnPurpose = dribbler;
        MatchManager.Instance.PreviousTokenToTouchTheBallOnPurpose = null;
        MatchManager.Instance.UpdatePossessionAfterPass(dribblerHex);

        movementPhaseManager.ResetMovementPhase();
        movementPhaseManager.isActivated = true;
        movementPhaseManager.isAvailable = false;
        movementPhaseManager.isMovementPhaseDef = true;
        movementPhaseManager.isAwaitingTokenSelection = true;
        movementPhaseManager.CommitToAction();

        finalThirdManager.isActivated = false;
        freeKickManager.isActivated = false;
        looseBallManager.isActivated = false;
        goalKeeperManager.isActivated = false;
        shotManager.isActivated = false;

        AssertTrue(goalkeeper.IsGoalKeeper, "Outside-box GK tests should use Kuzmic as the defending goalkeeper.");
        AssertTrue(dribbler.IsDribbler, "Outside-box GK tests should put Yaneva on the ball as dribbler.");
        AssertTrue(!goalKeeperManager.IsGoalkeeperOwnPenaltyHex(goalkeeper, dribblerHex), "Dribbler should be outside the defending goalkeeper's own penalty box.");
        AssertTrue(dribblerHex.IsDangerousTacklingPosition(MatchManager.Instance.homeTeamDirection, goalkeeperChallengeHex) == dangerousPosition,
            "The selected tackle hex must match the scenario's position-based danger setting.");

        return new OutsideBoxGoalkeeperChallengeSetup(
            dribbler,
            goalkeeper,
            dribblerHex,
            goalkeeperStartHex,
            goalkeeperChallengeHex,
            goalkeeperStartHex);
    }

    private OutsideBoxGoalkeeperShotSetup PrepareOutsideBoxGoalkeeperShotBoard(
        Vector3Int shooterCoordinates,
        Vector3Int goalkeeperCoordinates,
        Vector3Int targetCoordinates,
        IReadOnlyList<Vector3Int> pathCoordinates,
        string shotType = "fullPower",
        string blockerName = null,
        Vector3Int? blockerCoordinates = null)
    {
        ConfigureRightBoxGkWallAuditState();
        MatchManager.Instance.attackHasPossession = true;
        MatchManager.Instance.currentState = MatchManager.GameState.EndOfMovementPhase;

        PlayerToken shooter = RequirePlayerToken("Yaneva");
        PlayerToken goalkeeper = RequirePlayerToken("Kuzmic");
        PlayerToken blocker = !string.IsNullOrWhiteSpace(blockerName)
            ? RequirePlayerToken(blockerName)
            : null;

        HexCell shooterHex = RequireHex(hexgrid.GetHexCellAt(shooterCoordinates), $"Outside-box GK shot shooter hex {shooterCoordinates} should exist.");
        HexCell goalkeeperHex = RequireHex(hexgrid.GetHexCellAt(goalkeeperCoordinates), $"Outside-box GK shot goalkeeper hex {goalkeeperCoordinates} should exist.");
        HexCell targetHex = RequireHex(hexgrid.GetHexCellAt(targetCoordinates), $"Outside-box GK shot target hex {targetCoordinates} should exist.");
        List<HexCell> path = pathCoordinates
            .Select(coordinates => RequireHex(hexgrid.GetHexCellAt(coordinates), $"Outside-box GK shot path hex {coordinates} should exist."))
            .ToList();
        HexCell blockerHex = blocker != null && blockerCoordinates.HasValue
            ? RequireHex(hexgrid.GetHexCellAt(blockerCoordinates.Value), $"Outside-box GK shot blocker hex {blockerCoordinates.Value} should exist.")
            : null;

        List<HexCell> reservedHexes = new() { shooterHex, goalkeeperHex, targetHex };
        reservedHexes.AddRange(path);
        if (blockerHex != null)
        {
            reservedHexes.Add(blockerHex);
        }

        MoveTokensAwayFromOutsideBoxShotHexes(reservedHexes, shooter, goalkeeper, blocker);
        PlaceTokenForScenario(shooter, shooterHex, asAttacker: true);
        PlaceTokenForScenario(goalkeeper, goalkeeperHex, asAttacker: false);
        if (blocker != null && blockerHex != null)
        {
            PlaceTokenForScenario(blocker, blockerHex, asAttacker: false);
        }

        groundBallManager.ball.PlaceAtCell(shooterHex);
        MatchManager.Instance.LastTokenToTouchTheBallOnPurpose = shooter;
        MatchManager.Instance.PreviousTokenToTouchTheBallOnPurpose = null;
        MatchManager.Instance.UpdatePossessionAfterPass(shooterHex);

        movementPhaseManager.ResetMovementPhase();
        movementPhaseManager.isActivated = false;
        finalThirdManager.isActivated = false;
        freeKickManager.isActivated = false;
        looseBallManager.isActivated = false;
        goalKeeperManager.isActivated = false;

        ResetShotManagerForOutsideBoxShotTest();
        shotManager.shooter = shooter;
        shotManager.shotType = shotType;
        shotManager.isActivated = true;
        shotManager.targetHex = targetHex;
        shotManager.saveHex = null;
        shotManager.totalShotPower = 0;
        shotManager.shooterRoll = 0;
        shotManager.alreadyInterceptedDefs = new List<PlayerToken>();
        SetPrivateInstanceField(shotManager, "trajectoryPath", path);

        AssertTrue(goalkeeper.IsGoalKeeper, "Outside-box GK shot tests should use Kuzmic as the defending goalkeeper.");
        AssertTrue(!goalKeeperManager.IsGoalkeeperOwnPenaltyHex(goalkeeper, goalkeeperHex), "Outside-box GK shot test goalkeeper should start outside their own penalty box.", "outside own box", HexCoordinates(goalkeeperHex));

        return new OutsideBoxGoalkeeperShotSetup(
            shooter,
            goalkeeper,
            blocker,
            shooterHex,
            goalkeeperHex,
            targetHex,
            path);
    }

    private void ResetShotManagerForOutsideBoxShotTest()
    {
        MethodInfo resetMethod = GetPrivateInstanceMethod(typeof(ShotManager), "ResetShotProcess");
        AssertTrue(resetMethod != null, "ShotManager reset method should exist for outside-box GK shot tests.");
        resetMethod?.Invoke(shotManager, Array.Empty<object>());
    }

    private void MoveTokensAwayFromOutsideBoxShotHexes(IEnumerable<HexCell> importantHexes, params PlayerToken[] exceptions)
    {
        HashSet<PlayerToken> exceptionTokens = new(exceptions.Where(token => token != null));
        HashSet<HexCell> reservedHexes = new(importantHexes.Where(hex => hex != null));
        foreach (HexCell hex in importantHexes.Where(hex => hex != null))
        {
            foreach (HexCell neighbor in hex.GetNeighbors(hexgrid))
            {
                if (neighbor != null)
                {
                    reservedHexes.Add(neighbor);
                }
            }
        }

        foreach (PlayerToken token in FindObjectsByType<PlayerToken>())
        {
            if (token == null || exceptionTokens.Contains(token))
            {
                continue;
            }

            HexCell currentHex = token.GetCurrentHex();
            if (currentHex == null || !reservedHexes.Contains(currentHex))
            {
                continue;
            }

            bool wasAttacker = token.isAttacker;
            HexCell fallbackHex = GetAllInBoundsHexesOrdered(currentHex)
                .FirstOrDefault(candidate => candidate != null
                    && !reservedHexes.Contains(candidate)
                    && candidate.isInGoal == 0
                    && !candidate.isAttackOccupied
                    && !candidate.isDefenseOccupied
                    && candidate.GetOccupyingToken() == null);
            AssertTrue(fallbackHex != null, $"Outside-box GK shot setup should find a fallback hex for {token.name}.");
            if (fallbackHex != null)
            {
                PlaceTokenForScenario(token, fallbackHex, wasAttacker);
            }
        }
    }

    private IEnumerator StartOutsideBoxGoalkeeperShotInterception(OutsideBoxGoalkeeperShotSetup setup)
    {
        SetPrivateInstanceField(shotManager, "trajectoryPath", setup.Path);
        IEnumerator coroutine = InvokePrivateShotCoroutine("StartInterceptionPhase", Array.Empty<object>());
        AssertTrue(coroutine != null, "Shot interception phase should be available for outside-box GK shot tests.");
        if (coroutine != null)
        {
            StartCoroutine(coroutine);
        }

        yield return null;
    }

    private IEnumerator MoveGoalkeeperNextToOutsideBoxDribbler(OutsideBoxGoalkeeperChallengeSetup setup)
    {
        Log("Clicking GK start - Select Kuzmic for outside-box GK challenge");
        yield return StartCoroutine(gameInputManager.DelayedClick(ToClickCoordinates(setup.GoalkeeperStartHex), 0.1f));
        Log("Clicking GK challenge hex - Move Kuzmic next to Yaneva");
        yield return StartCoroutine(gameInputManager.DelayedClick(ToClickCoordinates(setup.GoalkeeperChallengeHex), 0.1f));
        yield return StartCoroutine(WaitForCondition(
            () => movementPhaseManager.isWaitingForTackleDecision,
            2f,
            "Outside-box GK challenge should wait for a tackle/dive/stand decision after the GK moves adjacent."));

        AssertTrue(movementPhaseManager.selectedDefender == setup.Goalkeeper, "Outside-box GK challenge should select Kuzmic as the defender.", setup.Goalkeeper, movementPhaseManager.selectedDefender);
        AssertTrue(setup.Goalkeeper.GetCurrentHex() == setup.GoalkeeperChallengeHex, "Kuzmic should move onto the challenge hex.", setup.GoalkeeperChallengeHex, setup.Goalkeeper.GetCurrentHex());
    }

    private IEnumerator StartOutsideBoxGoalkeeperDive(OutsideBoxGoalkeeperChallengeSetup setup)
    {
        yield return StartCoroutine(MoveGoalkeeperNextToOutsideBoxDribbler(setup));
        Log("Pressing D - Kuzmic dives outside the box");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.D, 0.1f));
        yield return StartCoroutine(WaitForCondition(
            () => movementPhaseManager.isWaitingForTackleRoll,
            2f,
            "Outside-box GK dive should start tackle roll resolution."));
    }

    private IEnumerator StartOutsideBoxGoalkeeperFootTackle(OutsideBoxGoalkeeperChallengeSetup setup)
    {
        yield return StartCoroutine(MoveGoalkeeperNextToOutsideBoxDribbler(setup));
        Log("Pressing T - Kuzmic chooses foot tackle outside the box");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.T, 0.1f));
        yield return StartCoroutine(WaitForCondition(
            () => movementPhaseManager.isWaitingForTackleRoll,
            2f,
            "Outside-box GK foot tackle should start tackle rolls."));
        string rollInstructions = movementPhaseManager.GetInstructions();
        AssertTrue(rollInstructions.Contains("Tackling: 0"), "Outside-box GK foot tackle roll prompt should use Tackling 0.", "Tackling: 0", rollInstructions);
    }

    private static RollInputOverride JackpotRollOverride()
    {
        return new RollInputOverride
        {
            hasOverride = true,
            roll = 6,
            isJackpot = true
        };
    }

    private HexCell GetRestartTestCornerSpot()
    {
        Vector3Int[] candidateCoordinates =
        {
            new Vector3Int(18, 0, 12),
            new Vector3Int(18, 0, -12),
            new Vector3Int(-18, 0, 12),
            new Vector3Int(-18, 0, -12),
        };

        foreach (Vector3Int coordinates in candidateCoordinates)
        {
            HexCell candidate = hexgrid.GetHexCellAt(coordinates);
            if (candidate != null)
            {
                return candidate;
            }
        }

        AssertTrue(false, "Restart tests should find at least one corner spot.");
        return null;
    }

    private HexCell GetEmptyRestartTestHex(HexCell referenceHex, params HexCell[] excludedHexes)
    {
        HashSet<HexCell> excluded = new(excludedHexes.Where(hex => hex != null));
        HexCell destination = GetAllInBoundsHexesOrdered(referenceHex)
            .FirstOrDefault(hex => hex != null
                && !excluded.Contains(hex)
                && !hex.isAttackOccupied
                && !hex.isDefenseOccupied
                && hex.GetOccupyingToken() == null);

        AssertTrue(destination != null, "Restart test should find an empty in-bounds hex.");
        return destination;
    }

    private bool IsInCornerSpotZoi(HexCell cornerSpot, HexCell checkedHex)
    {
        return cornerSpot != null
            && checkedHex != null
            && cornerSpot.GetNeighbors(hexgrid).Contains(checkedHex);
    }

    private PlayerToken RequireAttackingGoalkeeperForRestartTest()
    {
        PlayerToken goalkeeper = FindObjectsByType<PlayerToken>(FindObjectsInactive.Include)
            .FirstOrDefault(token => token != null
                && token.isPlaying
                && token.IsGoalKeeper
                && token.isAttacker);
        AssertTrue(goalkeeper != null, "Restart test should find the current attacking goalkeeper.");
        return goalkeeper;
    }

    private PlayerToken RequireAttackingOutfielderForRestartTest(params PlayerToken[] excludedTokens)
    {
        HashSet<PlayerToken> excluded = new(excludedTokens.Where(token => token != null));
        PlayerToken outfielder = FindObjectsByType<PlayerToken>(FindObjectsInactive.Include)
            .FirstOrDefault(token => token != null
                && token.isPlaying
                && !token.IsGoalKeeper
                && token.isAttacker
                && !excluded.Contains(token));
        AssertTrue(outfielder != null, "Restart test should find a current attacking outfielder.");
        return outfielder;
    }

    private void MoveTokensOutOfFinalThirdForRestartTest(int finalThirdSide, params PlayerToken[] exceptions)
    {
        HashSet<PlayerToken> exceptionSet = new(exceptions.Where(token => token != null));
        foreach (PlayerToken token in FindObjectsByType<PlayerToken>())
        {
            if (token == null || exceptionSet.Contains(token) || token.GetCurrentHex()?.isInFinalThird != finalThirdSide)
            {
                continue;
            }

            bool wasAttacker = token.isAttacker;
            HexCell destination = GetAllInBoundsHexesOrdered(token.GetCurrentHex())
                .FirstOrDefault(hex => hex != null
                    && hex.isInFinalThird == 0
                    && hex.isInPenaltyBox == 0
                    && !hex.isAttackOccupied
                    && !hex.isDefenseOccupied
                    && hex.GetOccupyingToken() == null);
            AssertTrue(destination != null, $"Restart test should find a neutral destination for {token.name}.");
            PlaceTokenForScenario(token, destination, wasAttacker);
        }
    }

    private void ConfigureRightBoxGkWallAuditState()
    {
        MatchManager.Instance.difficulty_level = 3;
        if (MatchManager.Instance.gameData != null && MatchManager.Instance.gameData.gameSettings != null)
        {
            MatchManager.Instance.gameData.gameSettings.playerAssistance = 3;
        }

        MatchManager.Instance.homeTeamDirection = MatchManager.TeamAttackingDirection.LeftToRight;
        MatchManager.Instance.awayTeamDirection = MatchManager.TeamAttackingDirection.RightToLeft;
        EnsureTeamInAttackForTest(MatchManager.TeamInAttack.Home);
    }

    private void PlaceTokenForGkWallScenario(PlayerToken token, HexCell destinationHex, bool asAttacker)
    {
        if (token == null || destinationHex == null)
        {
            return;
        }

        token.isAttacker = asAttacker;
        SetTokenHexForThrowInTest(token, destinationHex);
    }

    private void ClearHexForGkWallScenario(HexCell hex)
    {
        if (hex == null)
        {
            return;
        }

        PlayerToken occupyingToken = hex.GetOccupyingToken();
        if (occupyingToken != null)
        {
            HexCell fallbackHex = GetAllInBoundsHexesOrdered(hex)
                .FirstOrDefault(candidate => candidate != null
                    && candidate != hex
                    && !candidate.isAttackOccupied
                    && !candidate.isDefenseOccupied);
            AssertTrue(fallbackHex != null, "GK Wall audit should find an empty fallback hex.");
            if (fallbackHex != null)
            {
                SetTokenHexForTest(occupyingToken, fallbackHex);
            }
        }

        hex.occupyingToken = null;
        hex.isAttackOccupied = false;
        hex.isDefenseOccupied = false;
    }

    private PlayerToken RequirePlayerToken(string playerName)
    {
        PlayerToken token = PlayerToken.GetPlayerTokenByName(playerName);
        AssertTrue(token != null, $"PlayerToken '{playerName}' should exist for the stats preview test.");
        return token;
    }

    private PlayerToken RequirePlayerTokenMatching(string playerNameFragment)
    {
        PlayerToken token = PlayerToken.GetPlayerTokenByName(playerNameFragment);
        if (token == null && !string.IsNullOrWhiteSpace(playerNameFragment))
        {
            token = FindObjectsByType<PlayerToken>()
                .FirstOrDefault(candidate => NamesMatch(GetTokenTestName(candidate), playerNameFragment));
        }

        AssertTrue(token != null, $"PlayerToken containing '{playerNameFragment}' should exist for the stats test.");
        return token;
    }

    private HexCell RequireHex(HexCell hex, string message)
    {
        AssertTrue(hex != null, message);
        return hex;
    }

    private void PlaceTokenForScenario(PlayerToken token, HexCell hex, bool asAttacker)
    {
        if (token == null || hex == null)
        {
            return;
        }

        HexCell oldHex = token.GetCurrentHex();
        if (oldHex != null && oldHex.occupyingToken == token)
        {
            oldHex.occupyingToken = null;
            oldHex.isAttackOccupied = false;
            oldHex.isDefenseOccupied = false;
        }

        if (hex.occupyingToken != null && hex.occupyingToken != token)
        {
            hex.occupyingToken.SetCurrentHex(null);
        }

        hex.occupyingToken = null;
        hex.isAttackOccupied = asAttacker;
        hex.isDefenseOccupied = !asAttacker;
        token.SetCurrentHex(hex);
        token.isAttacker = asAttacker;
        token.transform.position = hex.GetHexCenter();
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

    private void SimulateHighPassHover(HexCell hex)
    {
        MethodInfo hoverMethod = typeof(HighPassManager).GetMethod("OnHoverReceived", BindingFlags.Instance | BindingFlags.NonPublic);
        AssertTrue(hoverMethod != null, "HighPassManager private hover handler should exist for the HP hover tests.");
        hoverMethod?.Invoke(highPassManager, new object[] { hex?.occupyingToken, hex });
    }

    private static MethodInfo GetPrivateInstanceMethod(Type ownerType, string methodName, params Type[] parameterTypes)
    {
        return ownerType.GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            parameterTypes,
            null);
    }

    private void SetPrivateInstanceField(object target, string fieldName, object value)
    {
        AssertTrue(target != null, $"Target object should exist before setting private field '{fieldName}'.");
        if (target == null)
        {
            return;
        }

        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        AssertTrue(field != null, $"{target.GetType().Name}.{fieldName} private field should exist for the test.");
        field?.SetValue(target, value);
    }

    private T GetPrivateInstanceField<T>(object target, string fieldName)
    {
        AssertTrue(target != null, $"Target object should exist before reading private field '{fieldName}'.");
        if (target == null)
        {
            return default;
        }

        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        AssertTrue(field != null, $"{target.GetType().Name}.{fieldName} private field should exist for the test.");
        object value = field?.GetValue(target);
        return value is T typedValue ? typedValue : default;
    }

    private void PerformRiggedFirstTimePassInterceptionRoll(int rigRoll)
    {
        MethodInfo interceptionMethod = GetPrivateInstanceMethod(
            typeof(FirstTimePassManager),
            "PerformFTPInterceptionRolls",
            typeof(int?));
        AssertTrue(interceptionMethod != null, "FirstTimePassManager private interception roll method should exist for the FTP tests.");
        interceptionMethod?.Invoke(firstTimePassManager, new object[] { (int?)rigRoll });
    }

    private void PerformRiggedLongBallAccuracyRoll(int rigRoll)
    {
        MethodInfo accuracyMethod = GetPrivateInstanceMethod(
            typeof(LongBallManager),
            "PerformAccuracyRoll",
            typeof(int?));
        AssertTrue(accuracyMethod != null, "LongBallManager private accuracy roll method should exist for the Long Ball tests.");
        accuracyMethod?.Invoke(longBallManager, new object[] { (int?)rigRoll });
    }

    private void PerformRiggedLongBallDirectionRoll(int rigRoll)
    {
        MethodInfo directionMethod = GetPrivateInstanceMethod(
            typeof(LongBallManager),
            "PerformDirectionRoll",
            typeof(int?));
        AssertTrue(directionMethod != null, "LongBallManager private direction roll method should exist for the Long Ball tests.");
        directionMethod?.Invoke(longBallManager, new object[] { (int?)rigRoll });
    }

    private IEnumerator PerformRiggedLongBallDistanceRoll(int rigRoll)
    {
        MethodInfo distanceMethod = GetPrivateInstanceMethod(
            typeof(LongBallManager),
            "PerformDistanceRoll",
            typeof(int?));
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
        MethodInfo distanceMethod = GetPrivateInstanceMethod(
            typeof(LongBallManager),
            "PerformDistanceRoll",
            typeof(int?));
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
        MethodInfo interceptionMethod = GetPrivateInstanceMethod(
            typeof(LongBallManager),
            "PerformInterceptionCheck",
            typeof(HexCell),
            typeof(int?));
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
        MethodInfo interceptionMethod = GetPrivateInstanceMethod(
            typeof(LongBallManager),
            "PerformInterceptionCheck",
            typeof(HexCell),
            typeof(int?));
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

    private IEnumerator StartKickoffLongBallToTarget(int difficulty, Vector2Int targetCoordinates)
    {
        yield return StartCoroutine(PrepareLongBallAvailabilityFromKickoff(difficulty));

        HexCell targetHex = RequireHex(
            hexgrid.GetHexCellAt(new Vector3Int(targetCoordinates.x, 0, targetCoordinates.y)),
            $"Kickoff Long Ball target {targetCoordinates} should exist.");

        Log($"Clicking {targetCoordinates} - Select kickoff Long Ball target");
        yield return StartCoroutine(gameInputManager.DelayedClick(targetCoordinates, 0.1f));

        if (difficulty == 3)
        {
            AssertTrue(!longBallManager.isAwaitingTargetSelection, "Difficulty 3 kickoff Long Ball should commit on the first valid click.");
            AssertTrue(longBallManager.isWaitingForAccuracyRoll, "Difficulty 3 kickoff Long Ball should wait for accuracy after the target click.");
            yield break;
        }

        AssertTrue(longBallManager.currentTargetHex == targetHex, "Kickoff Long Ball should accept the selected target.", targetHex, longBallManager.currentTargetHex);
        Log($"Clicking {targetCoordinates} again - Confirm kickoff Long Ball target");
        yield return StartCoroutine(gameInputManager.DelayedClick(targetCoordinates, 0.1f));
        AssertTrue(longBallManager.isWaitingForAccuracyRoll, "Kickoff Long Ball should be waiting for accuracy after target confirmation.");
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

    private IEnumerator ResolvePendingFoulFinalThirdsBeforeFreeKick(string context)
    {
        yield return StartCoroutine(WaitForCondition(
            () => finalThirdManager.isActivated || freeKickManager.isWaitingForKickerSelection,
            3f,
            $"{context}: Final Thirds should either be active or the Free Kick should already be waiting for kicker selection."));

        if (finalThirdManager.isActivated)
        {
            Log($"{context}: Pressing X - Forfeit Attack FinalThird");
            yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
            yield return new WaitForSeconds(0.2f);

            if (finalThirdManager.isActivated)
            {
                Log($"{context}: Pressing X - Forfeit Defense FinalThird");
                yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
                yield return new WaitForSeconds(0.2f);
            }
        }

        yield return StartCoroutine(WaitForCondition(
            () => !finalThirdManager.isActivated && freeKickManager.isWaitingForKickerSelection,
            3f,
            $"{context}: FreeKickManager should wait for kicker selection after Final Thirds finish."));
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

    private IEnumerator PrepareThrowInAuditBoard(int difficulty)
    {
        yield return new WaitForSeconds(2f);

        MatchManager.Instance.difficulty_level = difficulty;
        if (MatchManager.Instance.gameData != null && MatchManager.Instance.gameData.gameSettings != null)
        {
            MatchManager.Instance.gameData.gameSettings.playerAssistance = difficulty;
        }

        EnsureTeamInAttackForTest(MatchManager.TeamInAttack.Home);
    }

    private void EnsureTeamInAttackForTest(MatchManager.TeamInAttack team)
    {
        if (MatchManager.Instance.teamInAttack != team)
        {
            MatchManager.Instance.ChangePossession();
        }
    }

    private void MoveHomeAttackersOutsideThrowRangeForTest(HexCell throwHex, params PlayerToken[] exceptions)
    {
        HashSet<PlayerToken> excludedTokens = new HashSet<PlayerToken>(exceptions.Where(token => token != null));
        List<HexCell> fallbackHexes = GetAllInBoundsHexesOrdered()
            .Where(hex => hex != null
                && HexGridUtils.GetHexStepDistance(throwHex, hex) > 6
                && !hex.isAttackOccupied
                && !hex.isDefenseOccupied)
            .ToList();

        int fallbackIndex = 0;
        foreach (PlayerToken token in FindObjectsByType<PlayerToken>()
            .Where(candidate => candidate != null && candidate.isHomeTeam && candidate.isAttacker && !excludedTokens.Contains(candidate)))
        {
            AssertTrue(fallbackIndex < fallbackHexes.Count, "Throw-in audit should find enough empty distant hexes for home attackers.");
            if (fallbackIndex >= fallbackHexes.Count)
            {
                return;
            }

            SetTokenHexForTest(token, fallbackHexes[fallbackIndex]);
            fallbackIndex++;
        }
    }

    private void SetTokenHexForThrowInTest(PlayerToken token, HexCell destinationHex)
    {
        PlayerToken occupyingToken = destinationHex?.GetOccupyingToken();
        if (occupyingToken != null && occupyingToken != token)
        {
            HexCell fallbackHex = GetAllInBoundsHexesOrdered(destinationHex)
                .FirstOrDefault(hex => hex != null
                    && hex != destinationHex
                    && !hex.isAttackOccupied
                    && !hex.isDefenseOccupied);
            AssertTrue(fallbackHex != null, "Throw-in audit should find an empty hex to clear a setup destination.");
            if (fallbackHex == null)
            {
                return;
            }

            SetTokenHexForTest(occupyingToken, fallbackHex);
        }

        SetTokenHexForTest(token, destinationHex);
    }

    private void ArmThrowInExecutionForTest(PlayerToken thrower, HexCell throwHex)
    {
        throwInManager.isActivated = true;
        throwInManager.isWaitingForTakerSelection = false;
        throwInManager.isRunningMandatoryMovement = false;
        throwInManager.isWaitingForOptionalMovementDecision = false;
        throwInManager.isRunningOptionalMovement = false;
        throwInManager.isWaitingForThrowTypeSelection = true;
        throwInManager.isWaitingForGroundTarget = false;
        throwInManager.isWaitingForHeaderTarget = false;
        throwInManager.throwInHex = throwHex;
        throwInManager.selectedThrower = thrower;
        throwInManager.currentHeaderThrowTarget = null;
        throwInManager.awardedTeam = MatchManager.TeamInAttack.Home;
        throwInManager.ball.PlaceAtCell(throwHex);
        MatchManager.Instance.SetLastToken(thrower);
        throwInManager.EnterThrowExecutionSelection();
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

    private IEnumerator PrepareHighPassAvailabilityFromKickoff(int difficulty)
    {
        yield return new WaitForSeconds(3f);

        MatchManager.Instance.difficulty_level = difficulty;
        if (MatchManager.Instance.gameData != null && MatchManager.Instance.gameData.gameSettings != null)
        {
            MatchManager.Instance.gameData.gameSettings.playerAssistance = difficulty;
        }
        Log($"Setting difficulty to {difficulty}");

        highPassManager.minPassDistance = 6;
        Log($"Setting High Pass minimum distance to {highPassManager.minPassDistance}");

        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Space, 0.05f));
        Log("Pressing Space");

        AssertTrue(
            MatchManager.Instance.currentState == MatchManager.GameState.KickoffBlown,
            "Game should be in KickoffBlown after kickoff.",
            MatchManager.GameState.KickoffBlown,
            MatchManager.Instance.currentState
        );
        AssertTrue(highPassManager.isAvailable, "High Pass should be available in the kickoff test harness.");
    }

    private bool ValidateHighPassTargetForTest(HexCell targetHex)
    {
        MethodInfo validationMethod = typeof(HighPassManager).GetMethod("ValidateHighPassTarget", BindingFlags.Instance | BindingFlags.NonPublic);
        AssertTrue(validationMethod != null, "HighPassManager private target validator should exist for the High Pass tests.");
        return validationMethod != null && (bool)validationMethod.Invoke(highPassManager, new object[] { targetHex, false });
    }

    private HexCell FindFirstValidHighPassTarget(Func<HexCell, int, bool> predicate, HexCell excludedHex = null)
    {
        HexCell ballHex = highPassManager.ball.GetCurrentHex();
        foreach (HexCell candidate in GetAllInBoundsHexesOrdered(ballHex))
        {
            if (candidate == null || candidate == ballHex || candidate == excludedHex)
            {
                continue;
            }

            int distance = HexGridUtils.GetHexStepDistance(ballHex, candidate);
            if (!predicate(candidate, distance))
            {
                continue;
            }

            if (ValidateHighPassTargetForTest(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private HexCell FindFirstValidHighPassTargetAtMinimumDistance(HexCell excludedHex = null)
    {
        HexCell exactMinimumTarget = FindFirstValidHighPassTarget(
            (_, distance) => distance == highPassManager.minPassDistance,
            excludedHex
        );

        return exactMinimumTarget ?? FindFirstValidHighPassTarget(
            (_, distance) => distance >= highPassManager.minPassDistance && distance <= 15,
            excludedHex
        );
    }

    private HexCell FindFirstTooCloseHighPassTarget()
    {
        HexCell ballHex = highPassManager.ball.GetCurrentHex();
        return GetAllInBoundsHexesOrdered(ballHex)
            .FirstOrDefault(candidate =>
                candidate != null
                && candidate != ballHex
                && !candidate.isAttackOccupied
                && !candidate.isDefenseOccupied
                && HexGridUtils.GetHexStepDistance(ballHex, candidate) > 0
                && HexGridUtils.GetHexStepDistance(ballHex, candidate) < highPassManager.minPassDistance);
    }

    private HexCell FindFirstDefenderOccupiedHighPassTarget()
    {
        HexCell ballHex = highPassManager.ball.GetCurrentHex();
        return hexgrid.GetDefenderHexes()
            .Where(hex => hex != null && hex.GetOccupyingToken() != null)
            .OrderBy(hex => HexGridUtils.GetHexStepDistance(ballHex, hex))
            .ThenBy(hex => hex.coordinates.x)
            .ThenBy(hex => hex.coordinates.z)
            .FirstOrDefault(hex =>
            {
                int distance = HexGridUtils.GetHexStepDistance(ballHex, hex);
                return distance >= highPassManager.minPassDistance && distance <= 15;
            });
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

    private static ShootingBranchStep ShotBlock(string playerName, int roll)
    {
        return new ShootingBranchStep { Type = ShootingBranchStepType.ShotBlockRoll, PlayerName = playerName, Roll = roll };
    }

    private static ShootingBranchStep ShooterRoll(string playerName, int roll)
    {
        return new ShootingBranchStep { Type = ShootingBranchStepType.ShooterRoll, PlayerName = playerName, Roll = roll };
    }

    private static ShootingBranchStep GkSave(string playerName, int roll)
    {
        return new ShootingBranchStep { Type = ShootingBranchStepType.GkSaveRoll, PlayerName = playerName, Roll = roll };
    }

    private static ShootingBranchStep Handling(int roll)
    {
        return new ShootingBranchStep { Type = ShootingBranchStepType.HandlingRoll, Roll = roll };
    }

    private static ShootingBranchStep LooseDirection(int roll)
    {
        return new ShootingBranchStep { Type = ShootingBranchStepType.LooseDirectionRoll, Roll = roll };
    }

    private static ShootingBranchStep LooseDistance(int roll)
    {
        return new ShootingBranchStep { Type = ShootingBranchStepType.LooseDistanceRoll, Roll = roll };
    }

    private static ShootingBranchStep LooseInterception(string playerName, int roll)
    {
        return new ShootingBranchStep { Type = ShootingBranchStepType.LooseInterceptionRoll, PlayerName = playerName, Roll = roll };
    }

    private static ShootingBranchStep GkMove(int x, int z)
    {
        return new ShootingBranchStep { Type = ShootingBranchStepType.GoalkeeperMove, Coordinates = new Vector2Int(x, z) };
    }

    private static ShootingBranchStep ExpectAnyOther(string playerName = null)
    {
        return new ShootingBranchStep { Type = ShootingBranchStepType.ExpectAnyOtherScenario, PlayerName = playerName };
    }

    private static ShootingBranchStep ExpectMovementToAttack()
    {
        return new ShootingBranchStep { Type = ShootingBranchStepType.ExpectMovementToAttack };
    }

    private static ShootingBranchStep ExpectSaveAndHold()
    {
        return new ShootingBranchStep { Type = ShootingBranchStepType.ExpectSaveAndHold };
    }

    private static ShootingBranchStep ExpectCornerNorth()
    {
        return new ShootingBranchStep { Type = ShootingBranchStepType.ExpectCornerNorth };
    }

    private static ShootingBranchStep ExpectCornerSouth()
    {
        return new ShootingBranchStep { Type = ShootingBranchStepType.ExpectCornerSouth };
    }

    private static ShootingBranchStep ExpectGoal()
    {
        return new ShootingBranchStep { Type = ShootingBranchStepType.ExpectGoal };
    }

    private static ShootingBranchStep ExpectOwnGoal()
    {
        return new ShootingBranchStep { Type = ShootingBranchStepType.ExpectOwnGoal };
    }

    private static ShootingBranchStep ExpectOffTarget()
    {
        return new ShootingBranchStep { Type = ShootingBranchStepType.ExpectOffTarget };
    }

    private List<ShootingBranchDefinition> BuildShootingBranchDefinitions()
    {
        ShootingBranchStep[] missedPatersonMissedMcNultySavedBase =
        {
            ShotBlock("Paterson", 4),
            GkMove(15, -1),
            ShotBlock("McNulty", 4),
            ShotBlock("Soares", 3),
            ShooterRoll("Yaneva", 4),
            GkSave("Kuzmic", 2)
        };

        ShootingBranchStep[] looseBallByKuzmicBase =
        {
            ShotBlock("Paterson", 4),
            GkMove(15, -1),
            ShotBlock("McNulty", 4),
            ShotBlock("Soares", 3),
            ShooterRoll("Yaneva", 5),
            GkSave("Kuzmic", 2)
        };

        ShootingBranchStep[] poulsenGoalLineBase =
        {
            ShotBlock("Paterson", 4),
            GkMove(15, -1),
            ShotBlock("McNulty", 4),
            ShotBlock("Soares", 3),
            ShooterRoll("Yaneva", 6),
            GkSave("Kuzmic", 2)
        };

        List<ShootingBranchDefinition> branches = new()
        {
            new ShootingBranchDefinition(
                "Blocked by Paterson Picked up by Gilbert",
                ShotBlock("Paterson", 6),
                LooseDirection(3),
                LooseDistance(6),
                LooseInterception("Gilbert", 6),
                ExpectAnyOther("Gilbert")),

            new ShootingBranchDefinition(
                "Blocked by Paterson missed by Gilbert",
                ShotBlock("Paterson", 6),
                LooseDirection(3),
                LooseDistance(6),
                LooseInterception("Gilbert", 3),
                ExpectMovementToAttack()),

            new ShootingBranchDefinition(
                "Blocked by Paterson missed by Paterson",
                ShotBlock("Paterson", 6),
                LooseDirection(1),
                LooseDistance(1),
                LooseInterception("Paterson", 3),
                ExpectMovementToAttack()),

            new ShootingBranchDefinition(
                "Blocked by Paterson into the Box McNulty misses",
                ShotBlock("Paterson", 6),
                LooseDirection(6),
                LooseDistance(5),
                LooseInterception("McNulty", 3),
                GkMove(15, -1),
                ExpectMovementToAttack()),

            new ShootingBranchDefinition(
                "Blocked by Paterson into the Box McNulty picks",
                ShotBlock("Paterson", 6),
                LooseDirection(6),
                LooseDistance(5),
                LooseInterception("McNulty", 6),
                GkMove(15, -1),
                ExpectAnyOther("McNulty")),

            new ShootingBranchDefinition(
                "Missed by Paterson Blocked by McNulty Paterson Picks",
                ShotBlock("Paterson", 4),
                GkMove(15, -1),
                ShotBlock("McNulty", 6),
                LooseDirection(2),
                LooseDistance(6),
                LooseInterception("Paterson", 6),
                ExpectAnyOther("Paterson")),

            new ShootingBranchDefinition(
                "Missed by Paterson Blocked by McNulty Paterson Misses",
                ShotBlock("Paterson", 4),
                GkMove(15, -1),
                ShotBlock("McNulty", 6),
                LooseDirection(2),
                LooseDistance(6),
                LooseInterception("Paterson", 4),
                ExpectMovementToAttack()),

            new ShootingBranchDefinition(
                "Missed by Paterson Blocked by McNulty McNulty Picks",
                ShotBlock("Paterson", 4),
                GkMove(15, -1),
                ShotBlock("McNulty", 6),
                LooseDirection(1),
                LooseDistance(1),
                LooseInterception("McNulty", 6),
                ExpectAnyOther("McNulty")),

            new ShootingBranchDefinition(
                "Missed by Paterson Blocked by McNulty McNulty Misses",
                ShotBlock("Paterson", 4),
                GkMove(15, -1),
                ShotBlock("McNulty", 6),
                LooseDirection(1),
                LooseDistance(1),
                LooseInterception("McNulty", 2),
                ExpectMovementToAttack()),

            new ShootingBranchDefinition(
                "Missed by Paterson Blocked by McNulty Soares Picks",
                ShotBlock("Paterson", 4),
                GkMove(15, -1),
                ShotBlock("McNulty", 6),
                LooseDirection(6),
                LooseDistance(3),
                LooseInterception("Soares", 6),
                ExpectAnyOther("Soares")),

            new ShootingBranchDefinition(
                "Missed by Paterson Blocked by McNulty Kuzmic Picks",
                ShotBlock("Paterson", 4),
                GkMove(15, -1),
                ShotBlock("McNulty", 6),
                LooseDirection(6),
                LooseDistance(3),
                LooseInterception("Soares", 2),
                LooseInterception("Kuzmic", 6),
                ExpectAnyOther("Kuzmic")),

            new ShootingBranchDefinition(
                "Missed by Paterson Blocked by McNulty Kuzmic Misses",
                ShotBlock("Paterson", 4),
                GkMove(15, -1),
                ShotBlock("McNulty", 6),
                LooseDirection(6),
                LooseDistance(3),
                LooseInterception("Soares", 2),
                LooseInterception("Kuzmic", 2),
                ExpectMovementToAttack()),

            new ShootingBranchDefinition(
                "Missed by Paterson McNulty Blocked by Soares McNulty Picks",
                ShotBlock("Paterson", 4),
                GkMove(15, -1),
                ShotBlock("McNulty", 4),
                ShotBlock("Soares", 6),
                LooseDirection(2),
                LooseDistance(6),
                LooseInterception("McNulty", 6),
                ExpectAnyOther("McNulty")),

            new ShootingBranchDefinition(
                "Missed by Paterson McNulty Blocked by Soares McNulty Misses",
                ShotBlock("Paterson", 4),
                GkMove(15, -1),
                ShotBlock("McNulty", 4),
                ShotBlock("Soares", 6),
                LooseDirection(2),
                LooseDistance(6),
                LooseInterception("McNulty", 4),
                ExpectMovementToAttack()),

            new ShootingBranchDefinition(
                "Missed by Paterson McNulty Blocked by Soares Kuzmic Picks",
                ShotBlock("Paterson", 4),
                GkMove(15, -1),
                ShotBlock("McNulty", 4),
                ShotBlock("Soares", 6),
                LooseDirection(6),
                LooseDistance(6),
                ExpectAnyOther("Kuzmic")),

            new ShootingBranchDefinition(
                "Missed by Paterson McNulty Blocked by Soares Kuzmic Misses",
                ShotBlock("Paterson", 4),
                GkMove(15, -1),
                ShotBlock("McNulty", 4),
                ShotBlock("Soares", 6),
                LooseDirection(5),
                LooseDistance(4),
                LooseInterception("Kuzmic", 2),
                ExpectMovementToAttack()),

            new ShootingBranchDefinition(
                "Missed by Paterson McNulty Blocked by Soares Kuzmic Misses Own Goal",
                ShotBlock("Paterson", 4),
                GkMove(15, -1),
                ShotBlock("McNulty", 4),
                ShotBlock("Soares", 6),
                LooseDirection(5),
                LooseDistance(5),
                LooseInterception("Kuzmic", 2),
                ExpectOwnGoal()),

            new ShootingBranchDefinition(
                "Saved by Kuzmic and held",
                missedPatersonMissedMcNultySavedBase.Concat(new[]
                {
                    Handling(1),
                    ExpectSaveAndHold()
                }).ToArray()),

            new ShootingBranchDefinition(
                "Saved by Kuzmic and Corner N",
                missedPatersonMissedMcNultySavedBase.Concat(new[]
                {
                    Handling(6),
                    LooseDirection(4),
                    ExpectCornerNorth()
                }).ToArray()),

            new ShootingBranchDefinition(
                "Saved by Kuzmic and Corner S",
                missedPatersonMissedMcNultySavedBase.Concat(new[]
                {
                    Handling(6),
                    LooseDirection(6),
                    ExpectCornerSouth()
                }).ToArray()),

            new ShootingBranchDefinition(
                "Saved by Kuzmic and Spill Soares Hits",
                missedPatersonMissedMcNultySavedBase.Concat(new[]
                {
                    Handling(6),
                    LooseDirection(2),
                    LooseDistance(6),
                    ExpectAnyOther("Soares")
                }).ToArray()),

            new ShootingBranchDefinition(
                "Saved by Kuzmic and Spill Soares Misses",
                missedPatersonMissedMcNultySavedBase.Concat(new[]
                {
                    Handling(6),
                    LooseDirection(3),
                    LooseDistance(4),
                    LooseInterception("Soares", 3),
                    ExpectMovementToAttack()
                }).ToArray()),

            new ShootingBranchDefinition(
                "Loose Ball by Kuzmic Poulsen Misses",
                looseBallByKuzmicBase.Concat(new[]
                {
                    LooseDirection(6),
                    LooseDistance(6),
                    LooseInterception("Poulsen", 2),
                    ExpectGoal()
                }).ToArray()),

            new ShootingBranchDefinition(
                "Loose Ball by Kuzmic Poulsen Intercepts",
                looseBallByKuzmicBase.Concat(new[]
                {
                    LooseDirection(6),
                    LooseDistance(6),
                    LooseInterception("Poulsen", 6),
                    ExpectAnyOther("Poulsen")
                }).ToArray()),

            new ShootingBranchDefinition(
                "Loose Ball by Kuzmic Kalla Picks",
                looseBallByKuzmicBase.Concat(new[]
                {
                    LooseDirection(4),
                    LooseDistance(1),
                    ExpectAnyOther("Kalla")
                }).ToArray()),

            new ShootingBranchDefinition(
                "Loose Ball by Kuzmic Soares Picks",
                looseBallByKuzmicBase.Concat(new[]
                {
                    LooseDirection(3),
                    LooseDistance(4),
                    LooseInterception("Soares", 6),
                    ExpectAnyOther("Soares")
                }).ToArray()),

            new ShootingBranchDefinition(
                "Loose Ball by Kuzmic Soares Misses",
                looseBallByKuzmicBase.Concat(new[]
                {
                    LooseDirection(3),
                    LooseDistance(4),
                    LooseInterception("Soares", 2),
                    ExpectMovementToAttack()
                }).ToArray()),

            new ShootingBranchDefinition(
                "Poulsen Goal",
                poulsenGoalLineBase.Concat(new[]
                {
                    ShotBlock("Poulsen", 1),
                    ExpectGoal()
                }).ToArray()),

            new ShootingBranchDefinition(
                "Poulsen Own Goal",
                poulsenGoalLineBase.Concat(new[]
                {
                    ShotBlock("Poulsen", 6),
                    LooseDirection(6),
                    LooseDistance(2),
                    ExpectOwnGoal()
                }).ToArray()),

            new ShootingBranchDefinition(
                "Poulsen Blocks Kuzmic Picks Up",
                poulsenGoalLineBase.Concat(new[]
                {
                    ShotBlock("Poulsen", 6),
                    LooseDirection(2),
                    LooseDistance(6),
                    LooseInterception("Kuzmic", 6),
                    ExpectAnyOther("Kuzmic")
                }).ToArray()),

            new ShootingBranchDefinition(
                "Poulsen Blocks Kuzmic Misses",
                poulsenGoalLineBase.Concat(new[]
                {
                    ShotBlock("Poulsen", 6),
                    LooseDirection(2),
                    LooseDistance(6),
                    LooseInterception("Kuzmic", 2),
                    ExpectMovementToAttack()
                }).ToArray()),
        };

        return branches;
    }

    private List<FreeKickShotBranchDefinition> BuildFreeKickShotBranchDefinitions()
    {
        ShootingBranchStep[] failedBlocksBase =
        {
            ShooterRoll("Noruega", 5),
            GkMove(17, -1),
            ShotBlock("Soares", 1)
        };

        return new List<FreeKickShotBranchDefinition>
        {
            new FreeKickShotBranchDefinition(
                "Natural one missed blocks off target",
                ShooterRoll("Noruega", 1),
                GkMove(17, -1),
                ShotBlock("Soares", 1),
                ExpectOffTarget()),

            new FreeKickShotBranchDefinition(
                "Noruega 5 failed blocks Kuzmic save roll 2 goal",
                failedBlocksBase.Concat(new[]
                {
                    GkSave("Kuzmic", 2),
                    ExpectGoal()
                }).ToArray()),

            new FreeKickShotBranchDefinition(
                "Noruega 5 failed blocks Kuzmic save roll 4 handling hold",
                failedBlocksBase.Concat(new[]
                {
                    GkSave("Kuzmic", 4),
                    Handling(1),
                    ExpectSaveAndHold()
                }).ToArray()),

            new FreeKickShotBranchDefinition(
                "Noruega 5 failed blocks Kuzmic save roll 4 handling spill corner south",
                failedBlocksBase.Concat(new[]
                {
                    GkSave("Kuzmic", 4),
                    Handling(6),
                    LooseDirection(1),
                    ExpectCornerSouth()
                }).ToArray()),

            new FreeKickShotBranchDefinition(
                "Noruega 5 failed blocks Kuzmic save roll 4 spill Vladoiu hit",
                failedBlocksBase.Concat(new[]
                {
                    GkSave("Kuzmic", 4),
                    Handling(6),
                    LooseDirection(2),
                    LooseDistance(5),
                    ExpectAnyOther("Vladoiu")
                }).ToArray()),

            new FreeKickShotBranchDefinition(
                "Noruega 5 failed blocks Kuzmic save roll 4 spill Vladoiu intercepts",
                failedBlocksBase.Concat(new[]
                {
                    GkSave("Kuzmic", 4),
                    Handling(6),
                    LooseDirection(2),
                    LooseDistance(2),
                    LooseInterception("Vladoiu", 6),
                    ExpectAnyOther("Vladoiu")
                }).ToArray()),

            new FreeKickShotBranchDefinition(
                "Noruega 5 failed blocks Kuzmic save roll 4 spill Vladoiu misses",
                failedBlocksBase.Concat(new[]
                {
                    GkSave("Kuzmic", 4),
                    Handling(6),
                    LooseDirection(2),
                    LooseDistance(2),
                    LooseInterception("Vladoiu", 5),
                    ExpectMovementToAttack()
                }).ToArray()),

            new FreeKickShotBranchDefinition(
                "Noruega 5 failed blocks Kuzmic tie loose direction 1 distance 1 Kuzmic misses",
                failedBlocksBase.Concat(new[]
                {
                    GkSave("Kuzmic", 3),
                    LooseDirection(1),
                    LooseDistance(1),
                    LooseInterception("Kuzmic", 1),
                    ExpectMovementToAttack()
                }).ToArray()),

            new FreeKickShotBranchDefinition(
                "Noruega 5 failed blocks Kuzmic tie loose direction 1 distance 2 movement",
                failedBlocksBase.Concat(new[]
                {
                    GkSave("Kuzmic", 3),
                    LooseDirection(1),
                    LooseDistance(2),
                    ExpectMovementToAttack()
                }).ToArray()),

            new FreeKickShotBranchDefinition(
                "Noruega 5 failed blocks Kuzmic tie loose direction 6 distance 4 goal",
                failedBlocksBase.Concat(new[]
                {
                    GkSave("Kuzmic", 3),
                    LooseDirection(6),
                    LooseDistance(4),
                    ExpectGoal()
                }).ToArray()),

            new FreeKickShotBranchDefinition(
                "Noruega 5 failed blocks Kuzmic tie loose Soares misses movement",
                failedBlocksBase.Concat(new[]
                {
                    GkSave("Kuzmic", 3),
                    LooseDirection(3),
                    LooseDistance(6),
                    LooseInterception("Soares", 4),
                    ExpectMovementToAttack()
                }).ToArray()),

            new FreeKickShotBranchDefinition(
                "Noruega 6 perfect height skips blocks Kuzmic save roll 3 goal",
                ShooterRoll("Noruega", 6),
                GkMove(17, -1),
                GkSave("Kuzmic", 3),
                ExpectGoal())
        };
    }

    private List<OobLongBallBranchDefinition> BuildOobLongBallBranchDefinitions()
    {
        return new List<OobLongBallBranchDefinition>
        {
            new OobLongBallBranchDefinition("Left bottom direction 1 throw in away", new Vector2Int(-18, -12), 1, 1, 6, OobRestartExpectation.ThrowIn, new Vector2Int(-18, -12)),
            new OobLongBallBranchDefinition("Left bottom direction 2 corner away", new Vector2Int(-18, -12), 1, 2, 6, OobRestartExpectation.CornerKick, new Vector2Int(-18, -12)),
            new OobLongBallBranchDefinition("Left lower box direction 3 corner away", new Vector2Int(-14, -5), 1, 3, 6, OobRestartExpectation.CornerKick, new Vector2Int(-18, -12)),
            new OobLongBallBranchDefinition("Left upper touchline direction 3 corner away", new Vector2Int(-18, 7), 1, 3, 6, OobRestartExpectation.CornerKick, new Vector2Int(-18, 12)),
            new OobLongBallBranchDefinition("Right bottom direction 1 throw in away", new Vector2Int(18, -12), 1, 1, 6, OobRestartExpectation.ThrowIn, new Vector2Int(18, -12)),
            new OobLongBallBranchDefinition("Right bottom direction 6 goal kick away", new Vector2Int(18, -12), 1, 6, 6, OobRestartExpectation.GoalKick, new Vector2Int(16, 0)),
        };
    }

    private List<HeaderAtGoalBranchDefinition> BuildHeaderAtGoalBranchDefinitions()
    {
        return new List<HeaderAtGoalBranchDefinition>
        {
            new HeaderAtGoalBranchDefinition("No rush save roll 2 goal", false, 6, 4, 2, null, null, null, HeaderAtGoalOutcomeExpectation.Goal),
            new HeaderAtGoalBranchDefinition("No rush save tie direction 5 distance 5 goal", false, 6, 4, 4, null, 5, 5, HeaderAtGoalOutcomeExpectation.Goal),
            new HeaderAtGoalBranchDefinition("No rush save tie direction 6 distance 5 goal", false, 6, 4, 4, null, 6, 5, HeaderAtGoalOutcomeExpectation.Goal),
            new HeaderAtGoalBranchDefinition("No rush save handling spill direction 5 corner north", false, 6, 4, 6, 6, 5, null, HeaderAtGoalOutcomeExpectation.CornerKick, new Vector2Int(18, 12)),
            new HeaderAtGoalBranchDefinition("No rush save handling spill direction 1 corner south", false, 6, 4, 6, 6, 1, null, HeaderAtGoalOutcomeExpectation.CornerKick, new Vector2Int(18, -12)),
            new HeaderAtGoalBranchDefinition("No rush save handling spill direction 2 distance 1 Kuzmic misses movement", false, 6, 4, 6, 6, 2, 1, HeaderAtGoalOutcomeExpectation.MovementPhase, looseInterceptionPlayerName: "Kuzmic", looseInterceptionRoll: 2),
            new HeaderAtGoalBranchDefinition("No rush save handling hold", false, 6, 4, 6, 1, null, null, HeaderAtGoalOutcomeExpectation.SaveAndHold, new Vector2Int(16, 0)),
            new HeaderAtGoalBranchDefinition("Rush aerial 6 GK wins challenge save and hold no handling", true, 6, 4, 6, null, null, null, HeaderAtGoalOutcomeExpectation.SaveAndHold, new Vector2Int(15, 2)),
            new HeaderAtGoalBranchDefinition("Rush aerial 5 direction 6 distance 6 goal", true, 6, 4, 5, null, 6, 6, HeaderAtGoalOutcomeExpectation.Goal),
            new HeaderAtGoalBranchDefinition("Rush aerial 5 direction 5 distance 6 corner north", true, 6, 4, 5, null, 5, 6, HeaderAtGoalOutcomeExpectation.CornerKick, new Vector2Int(18, 12)),
            new HeaderAtGoalBranchDefinition("Rush aerial 5 direction 2 distance 1 movement no Kuzmic interception", true, 6, 4, 5, null, 2, 1, HeaderAtGoalOutcomeExpectation.MovementPhase, forbiddenLooseInterceptorName: "Kuzmic"),
            new HeaderAtGoalBranchDefinition("Rush aerial 5 direction 2 distance 2 movement", true, 6, 4, 5, null, 2, 2, HeaderAtGoalOutcomeExpectation.MovementPhase, new Vector2Int(12, 1)),
            new HeaderAtGoalBranchDefinition("Rush aerial 4 goal", true, 6, 4, 4, null, null, null, HeaderAtGoalOutcomeExpectation.Goal),
        };
    }

    private void ResetShootingStatsExpectations()
    {
        expectedShootingShotBlockAttempts.Clear();
        expectedShootingShotBlocksMade.Clear();
        expectedShootingLooseInterceptionAttempts.Clear();
        expectedShootingLooseRecoveriesMade.Clear();
        expectedShootingLooseXRecoveries.Clear();
        expectedShootingXGoals = null;
        expectedShootingShotBlocked = false;
        expectedShootingShotOnTarget = false;
        expectedShootingShotOffTarget = false;
    }

    private static void IncrementExpectedCount(Dictionary<string, int> counts, string playerName, int amount = 1)
    {
        if (string.IsNullOrWhiteSpace(playerName))
        {
            return;
        }

        counts.TryGetValue(playerName, out int current);
        counts[playerName] = current + amount;
    }

    private static void IncrementExpectedFloat(Dictionary<string, float> values, string playerName, float amount)
    {
        if (string.IsNullOrWhiteSpace(playerName))
        {
            return;
        }

        values.TryGetValue(playerName, out float current);
        values[playerName] = current + amount;
    }

    private void RecordShootingShotBlockAttempt(PlayerToken blocker, int requiredNaturalRoll, int rigRoll, int? effectiveTackling = null)
    {
        string blockerName = GetTokenTestName(blocker);
        IncrementExpectedCount(expectedShootingShotBlockAttempts, blockerName);

        if (IsShootingBlockRollSuccessful(blocker, requiredNaturalRoll, rigRoll, effectiveTackling))
        {
            IncrementExpectedCount(expectedShootingShotBlocksMade, blockerName);
            expectedShootingShotBlocked = true;
            expectedShootingXGoals ??= CalculateCurrentShootingExpectedGoal();
        }
    }

    private void RecordShootingLooseInterceptionAttempt(PlayerToken interceptor, int rigRoll)
    {
        string interceptorName = GetTokenTestName(interceptor);
        IncrementExpectedCount(expectedShootingLooseInterceptionAttempts, interceptorName);
        IncrementExpectedFloat(
            expectedShootingLooseXRecoveries,
            interceptorName,
            ExpectedStatsCalculator.CalculateRecoveryProbability(interceptor.IsGoalKeeper ? interceptor.saving : interceptor.tackling));

        if (IsLooseRecoveryRollSuccessful(interceptor, rigRoll))
        {
            IncrementExpectedCount(expectedShootingLooseRecoveriesMade, interceptorName);
        }
    }

    private void RecordShootingGkSaveExpectation(object gkInteraction, int rigRoll)
    {
        expectedShootingXGoals ??= CalculateCurrentShootingExpectedGoal();

        PlayerToken gk = GetShotInteractionDefender(gkInteraction);
        int gkPenalty = GetShotInteractionNullableInt(gkInteraction, "gkPenalty") ?? 0;
        int shotPower = shotManager != null ? shotManager.totalShotPower : 0;
        int savePower = gk != null ? rigRoll + gk.saving + gkPenalty : rigRoll + gkPenalty;

        if (savePower == shotPower)
        {
            expectedShootingShotBlocked = true;
        }
        else if (savePower > shotPower)
        {
            expectedShootingShotOnTarget = true;
        }
    }

    private static bool IsShootingBlockRollSuccessful(PlayerToken blocker, int requiredNaturalRoll, int roll, int? effectiveTackling = null)
    {
        int tackling = effectiveTackling ?? blocker?.tackling ?? 0;
        return blocker != null && (roll >= requiredNaturalRoll || roll + tackling >= 10);
    }

    private static bool IsLooseRecoveryRollSuccessful(PlayerToken interceptor, int roll)
    {
        return interceptor != null && (roll == 6 || roll + (interceptor.IsGoalKeeper ? interceptor.saving : interceptor.tackling) >= 10);
    }

    private float CalculateCurrentShootingExpectedGoal()
    {
        PlayerToken currentShooter = shotManager != null ? shotManager.shooter : null;
        if (currentShooter == null)
        {
            currentShooter = RequirePlayerToken("Yaneva");
        }

        int shootingPenalty = CalculateShootingPenaltyForStatsTest(currentShooter, shotManager != null ? shotManager.shotType : null);
        List<ExpectedStatsCalculator.ShotBlockerExpectation> blockExpectations = ReadExpectedShotBlockersFromShotManager();
        object gkInteraction = ReadExpectedGoalGkInteractionFromShotManager();
        PlayerToken savingGk = GetShotInteractionDefender(gkInteraction);
        int savingPenalty = GetShotInteractionNullableInt(gkInteraction, "gkPenalty") ?? 0;

        return ExpectedStatsCalculator.CalculateShotGoalProbability(
            currentShooter,
            shootingPenalty,
            blockExpectations,
            savingGk,
            savingPenalty);
    }

    private int CalculateShootingPenaltyForStatsTest(PlayerToken currentShooter, string currentShotType)
    {
        int penalty = 0;
        HexCell shooterHex = currentShooter != null ? currentShooter.GetCurrentHex() : null;
        if (currentShotType == "snapshot") penalty++;
        if (shooterHex != null && shooterHex.isInPenaltyBox == 0) penalty++;
        if (shooterHex != null && shooterHex.isDifficultShotPosition) penalty++;
        return Mathf.Min(penalty, 2);
    }

    private List<ExpectedStatsCalculator.ShotBlockerExpectation> ReadExpectedShotBlockersFromShotManager()
    {
        List<ExpectedStatsCalculator.ShotBlockerExpectation> blockExpectations = new();
        FieldInfo field = typeof(ShotManager).GetField("expectedGoalOutfieldBlockInteractions", BindingFlags.Instance | BindingFlags.NonPublic);
        AssertTrue(field != null, "ShotManager expected-goal blocker context should exist for Shooting stats tests.");
        if (field?.GetValue(shotManager) is not System.Collections.IEnumerable interactions)
        {
            return blockExpectations;
        }

        foreach (object interaction in interactions)
        {
            PlayerToken defender = GetShotInteractionDefender(interaction);
            if (defender == null)
            {
                continue;
            }

            blockExpectations.Add(new ExpectedStatsCalculator.ShotBlockerExpectation(
                defender,
                GetShotInteractionInt(interaction, "requiredNaturalRoll", 6),
                GetShotInteractionNullableInt(interaction, "effectiveTackling")));
        }

        return blockExpectations;
    }

    private object ReadExpectedGoalGkInteractionFromShotManager()
    {
        FieldInfo field = typeof(ShotManager).GetField("expectedGoalGkSaveInteraction", BindingFlags.Instance | BindingFlags.NonPublic);
        AssertTrue(field != null, "ShotManager expected-goal GK context should exist for Shooting stats tests.");
        return field?.GetValue(shotManager);
    }

    private void AssertShootingBranchStats(ShootingBranchDefinition branch)
    {
        AssertShotBranchStats($"Shooting branch '{branch.Name}' stats", branch.Steps, "Yaneva");
    }

    private void AssertFreeKickShotBranchStats(FreeKickShotBranchDefinition branch)
    {
        AssertShotBranchStats($"Free Kick Shot branch '{branch.Name}' stats", branch.Steps, "Noruega");
    }

    private void AssertShotBranchStats(string context, IEnumerable<ShootingBranchStep> steps, string expectedShooterName)
    {
        List<ShootingBranchStep> branchSteps = steps?.ToList() ?? new List<ShootingBranchStep>();
        PlayerToken shooterToken = RequirePlayerTokenMatching(expectedShooterName);
        MatchManager.PlayerStats shooterStats = MatchManager.Instance.gameData.stats.GetPlayerStats(shooterToken.playerName);
        MatchManager.TeamStats shooterTeamStats = MatchManager.Instance.gameData.stats.GetTeamStats(shooterToken.isHomeTeam);
        MatchManager.TeamStats defendingTeamStats = MatchManager.Instance.gameData.stats.GetTeamStats(!shooterToken.isHomeTeam);

        int expectedGoals = branchSteps.Any(step => step.Type == ShootingBranchStepType.ExpectGoal || step.Type == ShootingBranchStepType.ExpectOwnGoal) ? 1 : 0;
        int expectedCorners = branchSteps.Any(IsShootingCornerExpectation) ? 1 : 0;
        int expectedBlockedShots = expectedShootingShotBlocked ? 1 : 0;
        int expectedShotsOnTarget = expectedShootingShotOnTarget ? 1 : 0;
        int expectedShotsOffTarget = expectedShootingShotOffTarget ? 1 : 0;
        int expectedShotBlockAttempts = expectedShootingShotBlockAttempts.Values.Sum();
        int expectedShotBlocksMade = expectedShootingShotBlocksMade.Values.Sum();

        AssertTrue(shooterStats.shotsAttempted == 1, $"{context}: {expectedShooterName} should have exactly one shot attempt.", 1, shooterStats.shotsAttempted);
        AssertTrue(shooterTeamStats.totalShots == 1, $"{context}: shooting team should have exactly one shot attempt.", 1, shooterTeamStats.totalShots);
        AssertTrue(shooterStats.shotsBlocked == expectedBlockedShots, $"{context}: {expectedShooterName} blocked-shot count should match the resolved shot outcome.", expectedBlockedShots, shooterStats.shotsBlocked);
        AssertTrue(shooterTeamStats.totalShotsBlocked == expectedBlockedShots, $"{context}: team blocked-shot total should match the resolved shot outcome.", expectedBlockedShots, shooterTeamStats.totalShotsBlocked);
        AssertTrue(shooterStats.shotsOnTarget == expectedShotsOnTarget, $"{context}: {expectedShooterName} shot-on-target count should match the resolved shot outcome.", expectedShotsOnTarget, shooterStats.shotsOnTarget);
        AssertTrue(shooterStats.shotsOffTarget == expectedShotsOffTarget, $"{context}: {expectedShooterName} off-target count should match the resolved shot outcome.", expectedShotsOffTarget, shooterStats.shotsOffTarget);
        AssertTrue(shooterTeamStats.totalGoals == expectedGoals, $"{context}: shooting team goal total should match the outcome.", expectedGoals, shooterTeamStats.totalGoals);
        AssertTrue(shooterTeamStats.totalCorners == expectedCorners, $"{context}: shooting team corner total should match the outcome.", expectedCorners, shooterTeamStats.totalCorners);
        AssertTrue(defendingTeamStats.totalShotBlocksAttempted == expectedShotBlockAttempts, $"{context}: defensive team shot-block attempts should not be counted as recoveries.", expectedShotBlockAttempts, defendingTeamStats.totalShotBlocksAttempted);
        AssertTrue(defendingTeamStats.totalShotBlocksMade == expectedShotBlocksMade, $"{context}: defensive team made shot blocks should match successful block rolls.", expectedShotBlocksMade, defendingTeamStats.totalShotBlocksMade);

        if (expectedShootingXGoals.HasValue)
        {
            AssertApproximately(shooterStats.xGoals, expectedShootingXGoals.Value, 0.0001f, $"{context}: {expectedShooterName} xG should use the final shot-resolution formula.");
        }
        else
        {
            AssertTrue(shooterStats.xGoals > 0f && shooterStats.xGoals <= 1f, $"{context}: {expectedShooterName} should have one bounded xG entry.", "(0,1]", shooterStats.xGoals);
        }
        AssertApproximately(shooterTeamStats.totalXGoals, shooterStats.xGoals, 0.0001f, $"{context}: team xG should roll up {expectedShooterName}'s xG.");

        string expectedReceiverName = branchSteps.LastOrDefault(step => step.Type == ShootingBranchStepType.ExpectAnyOtherScenario)?.PlayerName;
        PlayerToken expectedReceiver = !string.IsNullOrWhiteSpace(expectedReceiverName) ? RequirePlayerToken(expectedReceiverName) : null;
        bool isAutomaticDefensivePickup = expectedReceiver != null
            && expectedReceiver.isHomeTeam != shooterToken.isHomeTeam
            && !expectedShootingLooseRecoveriesMade.ContainsKey(expectedReceiverName);

        HashSet<string> involvedPlayers = new(StringComparer.OrdinalIgnoreCase);
        foreach (string playerName in expectedShootingShotBlockAttempts.Keys) involvedPlayers.Add(playerName);
        foreach (string playerName in expectedShootingLooseInterceptionAttempts.Keys) involvedPlayers.Add(playerName);
        foreach (string playerName in expectedShootingLooseXRecoveries.Keys) involvedPlayers.Add(playerName);
        if (isAutomaticDefensivePickup) involvedPlayers.Add(expectedReceiverName);

        foreach (string playerName in involvedPlayers)
        {
            MatchManager.PlayerStats playerStats = MatchManager.Instance.gameData.stats.GetPlayerStats(playerName);
            int expectedPlayerShotBlockAttempts = expectedShootingShotBlockAttempts.TryGetValue(playerName, out int shotBlockAttempts) ? shotBlockAttempts : 0;
            int expectedPlayerShotBlocksMade = expectedShootingShotBlocksMade.TryGetValue(playerName, out int shotBlocksMade) ? shotBlocksMade : 0;
            int expectedPlayerLooseInterceptionAttempts = expectedShootingLooseInterceptionAttempts.TryGetValue(playerName, out int looseAttempts) ? looseAttempts : 0;
            int expectedPlayerLooseRecoveriesMade = expectedShootingLooseRecoveriesMade.TryGetValue(playerName, out int looseRecoveries) ? looseRecoveries : 0;
            float expectedPlayerXRecoveries = expectedShootingLooseXRecoveries.TryGetValue(playerName, out float xRecoveries) ? xRecoveries : 0f;
            if (isAutomaticDefensivePickup && string.Equals(playerName, expectedReceiverName, StringComparison.OrdinalIgnoreCase))
            {
                expectedPlayerXRecoveries += 1f;
                expectedPlayerLooseRecoveriesMade++;
            }

            AssertTrue(playerStats.shotBlocksAttempted == expectedPlayerShotBlockAttempts, $"{context}: {playerName} shot-block attempts should be tracked separately from interceptions.", expectedPlayerShotBlockAttempts, playerStats.shotBlocksAttempted);
            AssertTrue(playerStats.shotBlocksMade == expectedPlayerShotBlocksMade, $"{context}: {playerName} made shot blocks should match successful block rolls.", expectedPlayerShotBlocksMade, playerStats.shotBlocksMade);
            AssertTrue(playerStats.interceptionsAttempted == expectedPlayerLooseInterceptionAttempts, $"{context}: {playerName} interception attempts should come only from loose-ball interception rolls.", expectedPlayerLooseInterceptionAttempts, playerStats.interceptionsAttempted);
            AssertTrue(playerStats.interceptionsMade == 0, $"{context}: {playerName} should not receive made interceptions from shot blocks or loose-ball recoveries.", 0, playerStats.interceptionsMade);
            AssertApproximately(playerStats.xRecoveries, expectedPlayerXRecoveries, 0.0001f, $"{context}: {playerName} xRecoveries should match attempted interceptions plus guaranteed defensive pickups.");
            if (expectedPlayerLooseRecoveriesMade > 0)
            {
                AssertTrue(playerStats.possessionWon >= expectedPlayerLooseRecoveriesMade, $"{context}: {playerName} should record a recovery made after winning a loose-ball interception.", $">= {expectedPlayerLooseRecoveriesMade}", playerStats.possessionWon);
            }
        }

        if (!string.IsNullOrWhiteSpace(expectedReceiverName))
        {
            PlayerToken receiver = RequirePlayerToken(expectedReceiverName);
            if (receiver != null && receiver.isHomeTeam != shooterToken.isHomeTeam)
            {
                MatchManager.PlayerStats receiverStats = MatchManager.Instance.gameData.stats.GetPlayerStats(receiver.playerName);
                AssertTrue(receiverStats.possessionWon >= 1, $"{context}: defensive receiver {expectedReceiverName} should record a recovery made.", ">= 1", receiverStats.possessionWon);
            }
        }

        if (branchSteps.Any(step => step.Type == ShootingBranchStepType.ExpectSaveAndHold))
        {
            MatchManager.PlayerStats gkStats = MatchManager.Instance.gameData.stats.GetPlayerStats("Kuzmic");
            AssertTrue(gkStats.possessionWon >= 1, $"{context}: Kuzmic should record a recovery made when holding the save.", ">= 1", gkStats.possessionWon);
        }
    }

    private static string BuildScenarioSafeName(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
        {
            return "Unnamed";
        }

        StringBuilder builder = new(rawName.Length);
        bool previousWasSeparator = false;
        foreach (char c in rawName)
        {
            if (char.IsLetterOrDigit(c))
            {
                builder.Append(c);
                previousWasSeparator = false;
            }
            else if (!previousWasSeparator)
            {
                builder.Append('_');
                previousWasSeparator = true;
            }
        }

        return builder.ToString().Trim('_');
    }

    private IEnumerator Scenario_034_Shooting_Branch(ShootingBranchDefinition branch)
    {
        Log($"Starting Shooting branch from sheet: {branch.Name}");
        ResetShootingStatsExpectations();
        yield return StartCoroutine(PrepareShootingBranchFromSheet());

        foreach (ShootingBranchStep step in branch.Steps)
        {
            yield return StartCoroutine(ExecuteShootingBranchStep(branch, step));
            if (testFailed)
            {
                yield break;
            }
        }

        AssertShootingBranchStats(branch);
        LogFooterofTest($"Shooting - {branch.Name}");
    }

    private IEnumerator PrepareShootingBranchFromSheet()
    {
        yield return new WaitForSeconds(1.5f);

        MatchManager.Instance.difficulty_level = 2;
        if (MatchManager.Instance.gameData != null && MatchManager.Instance.gameData.gameSettings != null)
        {
            MatchManager.Instance.gameData.gameSettings.playerAssistance = 2;
        }

        Log("Setting difficulty to 2 for the shared Shooting sheet preparation.");

        yield return StartCoroutine(PressShootingSheetKey("Camera preset 2", KeyCode.Alpha2));
        yield return StartCoroutine(PressShootingSheetKey("KickOff", KeyCode.Space));
        AssertTrue(
            MatchManager.Instance.currentState == MatchManager.GameState.KickoffBlown,
            "Game should be in KickoffBlown after the Shooting prep kickoff.",
            MatchManager.GameState.KickoffBlown,
            MatchManager.Instance.currentState
        );

        yield return StartCoroutine(PressShootingSheetKey("StandardPass", KeyCode.P));
        yield return StartCoroutine(ClickShootingSheetHex("Target (8,0)", new Vector2Int(8, 0)));
        yield return StartCoroutine(ClickShootingSheetHex("ConfirmTarget (8,0)", new Vector2Int(8, 0)));
        yield return StartCoroutine(WaitForCondition(
            () => groundBallManager.ball.GetCurrentHex() == hexgrid.GetHexCellAt(new Vector3Int(8, 0, 0)) && !groundBallManager.isActivated,
            6f,
            "Shooting prep Standard Pass should finish with the ball on (8,0)."));

        yield return StartCoroutine(ForfeitShootingFinalThirds());

        yield return StartCoroutine(PressShootingSheetKey("Call MP", KeyCode.M));
        yield return StartCoroutine(WaitForCondition(
            () => movementPhaseManager.isActivated && movementPhaseManager.isMovementPhaseAttack,
            2f,
            "Shooting prep should start attacking Movement Phase."));

        yield return StartCoroutine(ClickShootingSheetHex("Click Yaneva (10,0)", new Vector2Int(10, 0)));
        AssertTrue(movementPhaseManager.selectedToken == RequirePlayerToken("Yaneva"), "Yaneva should be selected before picking up the ball.");
        yield return StartCoroutine(PressShootingSheetKey("Move Yaneva / pick up ball", KeyCode.V));
        yield return StartCoroutine(WaitForTokenOnHex("Yaneva", new Vector2Int(8, 0), "Yaneva should pick up the ball on (8,0)."));
        yield return StartCoroutine(PressShootingSheetKey("Forfeit Yaneva's Pace", KeyCode.X));

        yield return StartCoroutine(ClickShootingSheetHex("Click Kalla (6,6)", new Vector2Int(6, 6)));
        yield return StartCoroutine(MoveShootingToken("Kalla", new Vector2Int(11, 3)));
        yield return StartCoroutine(PressShootingSheetKey("Forfeit AttMP", KeyCode.X));
        yield return StartCoroutine(WaitForCondition(
            () => movementPhaseManager.isMovementPhaseDef,
            2f,
            "Shooting prep should advance to defensive Movement Phase."));

        yield return StartCoroutine(ClickShootingSheetHex("Click Paterson (4,3)", new Vector2Int(4, 3)));
        yield return StartCoroutine(MoveShootingToken("Paterson", new Vector2Int(9, 1)));
        yield return StartCoroutine(ClickShootingSheetHex("Click McNulty (5,5)", new Vector2Int(5, 5)));
        yield return StartCoroutine(MoveShootingToken("McNulty", new Vector2Int(9, 2)));
        yield return StartCoroutine(PressShootingSheetKey("Forfeit DefMP", KeyCode.X));
        yield return StartCoroutine(PressShootingSheetKey("Forfeit 2f2", KeyCode.X));

        yield return StartCoroutine(ForfeitShootingFinalThirds());

        yield return StartCoroutine(PressShootingSheetKey("Call MP", KeyCode.M));
        yield return StartCoroutine(WaitForCondition(
            () => movementPhaseManager.isActivated && movementPhaseManager.isMovementPhaseAttack,
            2f,
            "Second Shooting prep Movement Phase should start for attack."));

        yield return StartCoroutine(ClickShootingSheetHex("Click Kalla (11,3)", new Vector2Int(11, 3)));
        yield return StartCoroutine(MoveShootingToken("Kalla", new Vector2Int(15, 1)));
        yield return StartCoroutine(PressShootingSheetKey("Forfeit AttMP", KeyCode.X));
        yield return StartCoroutine(WaitForCondition(
            () => movementPhaseManager.isMovementPhaseDef,
            2f,
            "Second Shooting prep should advance to defense."));

        yield return StartCoroutine(ClickShootingSheetHex("Click Paterson (9,1)", new Vector2Int(9, 1)));
        yield return StartCoroutine(MoveShootingToken("Paterson", new Vector2Int(10, 0)));
        yield return StartCoroutine(ClickShootingSheetHex("Click McNulty (9,2)", new Vector2Int(9, 2)));
        yield return StartCoroutine(MoveShootingToken("McNulty", new Vector2Int(12, 0)));
        yield return StartCoroutine(PressShootingSheetKey("Forfeit DefMP", KeyCode.X));
        yield return StartCoroutine(PressShootingSheetKey("Forfeit 2f2", KeyCode.X));

        yield return StartCoroutine(ForfeitShootingFinalThirds());

        yield return StartCoroutine(WaitForCondition(
            () => shotManager.isAvailable && movementPhaseManager.isAvailable,
            3f,
            "Shot and Movement Phase should be available at the end of the Shooting prep."));
        AssertTrue(MatchManager.Instance.LastTokenToTouchTheBallOnPurpose == RequirePlayerToken("Yaneva"), "Yaneva should be the shooter after the shared Shooting prep.");
        AssertTrue(RequirePlayerToken("Yaneva").GetCurrentHex() == hexgrid.GetHexCellAt(new Vector3Int(8, 0, 0)), "Yaneva should shoot from (8,0).");
        string shotOfferInstructions = shotManager.GetInstructions();
        AssertTrue(
            shotOfferInstructions.Contains("Press [S] to take a Shot"),
            "Full-power shot offer instruction should label [S] as a Shot.",
            "Press [S] to take a Shot",
            shotOfferInstructions);

        yield return StartCoroutine(PressShootingSheetKey("Call Shot", KeyCode.S));
        AssertTrue(shotManager.isWaitingForShotCommitConfirmation, "Difficulty 2 Shooting prep should require S confirmation for the shot.");
        string shotCommitInstructions = shotManager.GetInstructions();
        AssertTrue(
            shotCommitInstructions.Contains("commit the Shot"),
            "Full-power shot confirmation instruction should label the pending action as a Shot.",
            "commit the Shot",
            shotCommitInstructions);
        yield return StartCoroutine(PressShootingSheetKey("Confirm Shot", KeyCode.S));
        AssertTrue(shotManager.isActivated, "ShotManager should activate after confirming the shot.");
        AssertTrue(shotManager.shotType == "fullPower", "The shared Shooting prep should create a full-power shot.", "fullPower", shotManager.shotType);
        AssertTrue(shotManager.isWaitingForTargetSelection, "ShotManager should wait for target selection after the shot is confirmed.");

        yield return StartCoroutine(ClickShootingSheetHex("Call Shot Target (19,0)", new Vector2Int(19, 0)));
        yield return StartCoroutine(WaitForCondition(
            () => shotManager.isWaitingForBlockDiceRoll || shotManager.isWaitingForShotRoll || goalKeeperManager.isActivated,
            3f,
            "Shooting prep should enter the first shot interaction after choosing target (19,0)."));
        AssertTrue(shotManager.targetHex == hexgrid.GetHexCellAt(new Vector3Int(19, 0, 0)), "Shot target should be (19,0).");
    }

    private IEnumerator PrepareFreeKickShotExecutionFromSheet()
    {
        yield return StartCoroutine(PrepareFreeKickShotAvailableFromSheet());
        AssertFreeKickExecutionReady("Free Kick Shot branch should begin at FK execution selection.");

        yield return StartCoroutine(PressShootingSheetKey("Noruega selects Shot preview", KeyCode.S));
        yield return StartCoroutine(WaitForCondition(
            () => shotManager.isWaitingForShotCommitConfirmation,
            2f,
            "Free Kick Shot should wait for second [S] confirmation at difficulty 2."));
        AssertTrue(!shotManager.isActivated, "Free Kick Shot should not activate before the second [S] confirmation.");

        yield return StartCoroutine(PressShootingSheetKey("Noruega confirms Shot", KeyCode.S));
        yield return StartCoroutine(WaitForCondition(
            () => shotManager.isActivated && shotManager.isWaitingForTargetSelection,
            3f,
            "Free Kick Shot should wait for target selection after confirmation."));

        yield return StartCoroutine(ClickShootingSheetHex("FK Shot target (19,-1)", new Vector2Int(19, -1)));
        yield return StartCoroutine(WaitForCondition(
            () => shotManager.isActivated && shotManager.isWaitingForShotRoll,
            3f,
            "Free Kick Shot should wait for Noruega's shot roll after target selection."));

        AssertFreeKickShotPathSetup();
        AssertFreeKickDicePresentation();
    }

    private void AssertFreeKickShotPathSetup()
    {
        List<HexCell> path = GetShotTrajectoryPath();
        AssertTrue(path != null && path.Count > 0, "Free Kick Shot should have a trajectory path after aiming at (19,-1).");
        if (path == null)
        {
            return;
        }

        HexCell soaresHex = hexgrid.GetHexCellAt(new Vector3Int(14, 0, 0));
        AssertTrue(path.Contains(soaresHex), "Free Kick Shot path should contain Soares' on-path block hex (14,0).", "(14,0)", PathCoordinates(path));
        AssertTrue(RequirePlayerToken("Soares").GetCurrentHex() == soaresHex, "Soares should be on the FK shot path at (14,0).", "(14,0)", HexCoordinates(RequirePlayerToken("Soares").GetCurrentHex()));
        AssertTrue(RequirePlayerToken("Kuzmic").GetCurrentHex() == hexgrid.GetHexCellAt(new Vector3Int(18, 0, 0)), "Kuzmic should start the FK shot resolution at (18,0).", "(18,0)", HexCoordinates(RequirePlayerToken("Kuzmic").GetCurrentHex()));
    }

    private IEnumerator ExecuteFreeKickShotBranchStep(FreeKickShotBranchDefinition branch, ShootingBranchStep step)
    {
        if (IsShootingOutcomeExpectation(step.Type))
        {
            yield return StartCoroutine(ForfeitShootingFinalThirdsBeforeOutcomeCheck(branch.Name));
        }

        switch (step.Type)
        {
            case ShootingBranchStepType.ShotBlockRoll:
                yield return StartCoroutine(PerformRiggedShotBlockRoll(step.PlayerName, step.Roll));
                break;
            case ShootingBranchStepType.ShooterRoll:
                yield return StartCoroutine(PerformRiggedShooterRoll(step.PlayerName, step.Roll));
                break;
            case ShootingBranchStepType.GkSaveRoll:
                yield return StartCoroutine(PerformRiggedShotGkSaveRoll(step.PlayerName, step.Roll));
                break;
            case ShootingBranchStepType.HandlingRoll:
                yield return StartCoroutine(PerformRiggedShotHandlingRoll(step.Roll));
                break;
            case ShootingBranchStepType.LooseDirectionRoll:
                yield return StartCoroutine(PerformRiggedLooseDirectionRoll(step.Roll));
                break;
            case ShootingBranchStepType.LooseDistanceRoll:
                yield return StartCoroutine(PerformRiggedLooseDistanceRoll(step.Roll));
                break;
            case ShootingBranchStepType.LooseInterceptionRoll:
                yield return StartCoroutine(PerformRiggedLooseInterceptionRoll(step.PlayerName, step.Roll));
                break;
            case ShootingBranchStepType.GoalkeeperMove:
                yield return StartCoroutine(MoveShootingGoalkeeperIfWaiting(step.Coordinates, false));
                break;
            case ShootingBranchStepType.ExpectAnyOtherScenario:
                yield return StartCoroutine(AssertFreeKickShotAnyOtherScenario(branch, step.PlayerName));
                break;
            case ShootingBranchStepType.ExpectMovementToAttack:
                yield return StartCoroutine(AssertFreeKickShotMovementToAttack(branch));
                break;
            case ShootingBranchStepType.ExpectSaveAndHold:
                yield return StartCoroutine(AssertFreeKickShotSaveAndHold(branch));
                break;
            case ShootingBranchStepType.ExpectCornerNorth:
                yield return StartCoroutine(AssertFreeKickShotCorner(branch, true));
                break;
            case ShootingBranchStepType.ExpectCornerSouth:
                yield return StartCoroutine(AssertFreeKickShotCorner(branch, false));
                break;
            case ShootingBranchStepType.ExpectGoal:
                yield return StartCoroutine(AssertFreeKickShotGoal(branch));
                break;
            case ShootingBranchStepType.ExpectOffTarget:
                yield return StartCoroutine(AssertFreeKickShotOffTarget(branch));
                break;
            default:
                AssertTrue(false, $"Unhandled Free Kick Shot branch step in {branch.Name}: {step.Type}");
                break;
        }
    }

    private IEnumerator AssertFreeKickShotAnyOtherScenario(FreeKickShotBranchDefinition branch, string expectedReceiverName)
    {
        yield return StartCoroutine(WaitForCondition(
            () => !looseBallManager.isActivated && !shotManager.isActivated && !goalKeeperManager.isActivated,
            6f,
            $"Free Kick Shot branch '{branch.Name}' should finish resolving before AnyOtherScenario availability is checked."));

        AvailabilityCheckResult availabilityCheck = AssertCorrectAvailabilityAnyOtherScenario();
        AssertTrue(availabilityCheck.passed, $"Free Kick Shot branch '{branch.Name}' should end in AnyOtherScenario availability.", true, availabilityCheck.ToString());

        if (!string.IsNullOrWhiteSpace(expectedReceiverName))
        {
            AssertShootingLastTouch(expectedReceiverName, $"Free Kick Shot branch '{branch.Name}' AnyOtherScenario outcome");
            AssertShootingBallOnToken(expectedReceiverName, $"Free Kick Shot branch '{branch.Name}' AnyOtherScenario outcome");
        }
    }

    private IEnumerator AssertFreeKickShotMovementToAttack(FreeKickShotBranchDefinition branch)
    {
        yield return StartCoroutine(WaitForCondition(
            () => !looseBallManager.isActivated && movementPhaseManager.isActivated,
            6f,
            $"Free Kick Shot branch '{branch.Name}' should resolve into attacking Movement Phase."));

        AssertShootingManagerState($"Free Kick Shot branch '{branch.Name}' Movement-to-attack outcome", expectMovementActive: true);
        AssertShootingBallInEmptySpace($"Free Kick Shot branch '{branch.Name}' Movement-to-attack outcome");
        AvailabilityCheckResult availabilityCheck = AssertCorrectAvailabilityAfterGBToSpace();
        AssertTrue(availabilityCheck.passed, $"Free Kick Shot branch '{branch.Name}' should end with Movement Phase committed for attack.", true, availabilityCheck.ToString());
    }

    private IEnumerator AssertFreeKickShotSaveAndHold(FreeKickShotBranchDefinition branch)
    {
        yield return StartCoroutine(WaitForCondition(
            () => shotManager.isWaitingForSaveandHoldScenario,
            4f,
            $"Free Kick Shot branch '{branch.Name}' should wait for Save-and-Hold choice."));

        AssertShootingManagerState($"Free Kick Shot branch '{branch.Name}' Save-and-Hold outcome");
        AssertShootingLastTouch("Kuzmic", $"Free Kick Shot branch '{branch.Name}' Save-and-Hold outcome");
        AssertShootingBallOnToken("Kuzmic", $"Free Kick Shot branch '{branch.Name}' Save-and-Hold outcome");
    }

    private IEnumerator AssertFreeKickShotCorner(FreeKickShotBranchDefinition branch, bool north)
    {
        yield return StartCoroutine(WaitForCondition(
            () => finalThirdManager.isActivated,
            6f,
            $"Free Kick Shot branch '{branch.Name}' should offer Final Thirds before corner setup."));
        AssertTrue(!freeKickManager.isWaitingForKickerSelection, $"Free Kick Shot branch '{branch.Name}' should not wait for corner kicker selection while Final Thirds are active.");
        yield return StartCoroutine(ForfeitShootingFinalThirdsBeforeOutcomeCheck(branch.Name));
        AssertNoLiveActionAvailabilityDuringSetPieceRestart($"Free Kick Shot branch '{branch.Name}' after corner Final Thirds");

        yield return StartCoroutine(WaitForCondition(
            () => freeKickManager.isActivated && freeKickManager.isWaitingForKickerSelection,
            6f,
            $"Free Kick Shot branch '{branch.Name}' should resolve to corner setup after Final Thirds."));

        AssertShootingManagerState($"Free Kick Shot branch '{branch.Name}' corner outcome", expectFreeKickActive: true);
        AssertNoLiveActionAvailabilityDuringSetPieceRestart($"Free Kick Shot branch '{branch.Name}' corner setup");
        AssertShootingLastTouch("Kuzmic", $"Free Kick Shot branch '{branch.Name}' corner outcome");
        HexCell ballHex = freeKickManager.ball.GetCurrentHex();
        AssertTrue(ballHex != null, $"Free Kick Shot branch '{branch.Name}' corner should place the ball on a corner spot.");
        if (ballHex != null)
        {
            AssertTrue(north ? ballHex.coordinates.z > 0 : ballHex.coordinates.z < 0, $"Free Kick Shot branch '{branch.Name}' should resolve to the {(north ? "north" : "south")} corner.", north ? "z > 0" : "z < 0", ballHex.coordinates);
        }
    }

    private IEnumerator AssertFreeKickShotGoal(FreeKickShotBranchDefinition branch)
    {
        yield return StartCoroutine(WaitForCondition(
            () => goalFlowManager.isActivated,
            6f,
            $"Free Kick Shot branch '{branch.Name}' should activate GoalFlow."));

        AssertShootingManagerState($"Free Kick Shot branch '{branch.Name}' goal outcome", expectGoalFlowActive: true);
        AssertShootingLastTouch("Noruega", $"Free Kick Shot branch '{branch.Name}' goal outcome");
        AssertShootingBallInGoal($"Free Kick Shot branch '{branch.Name}' goal outcome");
        while (goalFlowManager.isActivated)
        {
            yield return null;
        }
    }

    private IEnumerator AssertFreeKickShotOffTarget(FreeKickShotBranchDefinition branch)
    {
        yield return StartCoroutine(WaitForCondition(
            () => !shotManager.isActivated && !looseBallManager.isActivated && !goalKeeperManager.isActivated && !goalFlowManager.isActivated,
            6f,
            $"Free Kick Shot branch '{branch.Name}' should finish as an off-target shot."));

        AssertShootingManagerState($"Free Kick Shot branch '{branch.Name}' off-target outcome");
        AssertShootingLastTouch("Noruega", $"Free Kick Shot branch '{branch.Name}' off-target outcome");
    }

    private IEnumerator PrepareFreeKickShotAvailableFromSheet()
    {
        yield return new WaitForSeconds(1.5f);

        MatchManager.Instance.difficulty_level = 2;
        if (MatchManager.Instance.gameData != null && MatchManager.Instance.gameData.gameSettings != null)
        {
            MatchManager.Instance.gameData.gameSettings.playerAssistance = 2;
        }

        Log("Setting difficulty to 2 for the Free Kick prep script.");
        yield return StartCoroutine(PressShootingSheetKey("Camera preset 2", KeyCode.Alpha2));
        yield return StartCoroutine(PressShootingSheetKey("KickOff", KeyCode.Space));
        yield return StartCoroutine(PressShootingSheetKey("StandardPass", KeyCode.P));
        yield return StartCoroutine(ClickShootingSheetHex("Target (8,0)", new Vector2Int(8, 0)));
        yield return StartCoroutine(ClickShootingSheetHex("ConfirmTarget (8,0)", new Vector2Int(8, 0)));
        yield return StartCoroutine(WaitForCondition(
            () => groundBallManager.ball.GetCurrentHex() == hexgrid.GetHexCellAt(new Vector3Int(8, 0, 0)) && !groundBallManager.isActivated,
            6f,
            "Free Kick prep Standard Pass should finish with the ball on (8,0)."));

        yield return StartCoroutine(ForfeitShootingFinalThirds());

        yield return StartCoroutine(PressShootingSheetKey("Call MP", KeyCode.M));
        yield return StartCoroutine(WaitForCondition(
            () => movementPhaseManager.isActivated && movementPhaseManager.isMovementPhaseAttack,
            2f,
            "Free Kick prep should start attacking Movement Phase."));

        yield return StartCoroutine(ClickShootingSheetHex("Click Yaneva (10,0)", new Vector2Int(10, 0)));
        AssertTrue(movementPhaseManager.selectedToken == RequirePlayerToken("Yaneva"), "Yaneva should be selected before picking up the ball.");
        yield return StartCoroutine(PressShootingSheetKey("Move Yaneva / pick up ball", KeyCode.V));
        yield return StartCoroutine(WaitForTokenOnHex("Yaneva", new Vector2Int(8, 0), "Yaneva should pick up the ball on (8,0)."));
        yield return StartCoroutine(PressShootingSheetKey("Forfeit Yaneva's Pace", KeyCode.X));

        yield return StartCoroutine(SelectAndMoveFreeKickPrepToken("Kalla", new Vector2Int(6, 6), new Vector2Int(11, 3)));
        yield return StartCoroutine(PressShootingSheetKey("Forfeit AttMP", KeyCode.X));
        yield return StartCoroutine(WaitForCondition(
            () => movementPhaseManager.isMovementPhaseDef,
            2f,
            "Free Kick prep should advance to defensive Movement Phase."));

        yield return StartCoroutine(SelectAndMoveFreeKickPrepToken("Paterson", new Vector2Int(4, 3), new Vector2Int(9, 0)));
        yield return StartCoroutine(WaitForCondition(
            () => movementPhaseManager.isWaitingForTackleDecision,
            2f,
            "Paterson should be able to tackle Yaneva after moving to (9,0)."));
        yield return StartCoroutine(PressShootingSheetKey("Tackle", KeyCode.T));
        yield return StartCoroutine(WaitForCondition(
            () => movementPhaseManager.isWaitingForTackleRoll,
            2f,
            "Movement Phase should wait for tackle rolls after pressing T."));

        Log("Tackle Roll: rigging Paterson to 1.");
        movementPhaseManager.PerformTackleDiceRoll(isDefender: true, 1);
        AssertDuelDefenderRollPresentation();
        yield return new WaitForSeconds(0.2f);
        Log("Dribble Roll: rigging Yaneva to 6.");
        movementPhaseManager.PerformTackleDiceRoll(isDefender: false, 6);
        yield return WaitForDuelResultPresentation();
        yield return StartCoroutine(WaitForCondition(
            () => movementPhaseManager.isWaitingForYellowCardRoll,
            2f,
            "Movement Phase should wait for the leniency test after Paterson fouls."));

        Log("Leniency Test: rigging 6.");
        movementPhaseManager.PerformLeniencyTest(6);
        yield return StartCoroutine(WaitForCondition(
            () => movementPhaseManager.isWaitingForInjuryRoll,
            2f,
            "Movement Phase should wait for the injury test after the leniency test."));

        Log("Injury Test: rigging 2.");
        movementPhaseManager.PerformInjuryTest(2);
        yield return WaitForDuelResultPresentation();
        yield return StartCoroutine(WaitForCondition(
            () => movementPhaseManager.isWaitingForFoulDecision,
            2f,
            "Movement Phase should offer Play On or Take Free Kick after leniency and injury tests."));

        yield return StartCoroutine(PressShootingSheetKey("Take Foul", KeyCode.F));
        yield return StartCoroutine(WaitForCondition(
            () => finalThirdManager.isActivated,
            2f,
            "Final Thirds should resolve before FreeKickManager starts kicker selection."));
        AssertTrue(!freeKickManager.isWaitingForKickerSelection, "FreeKickManager should not listen for kicker selection while Final Thirds are active.");
        yield return StartCoroutine(ForfeitShootingFinalThirds());
        yield return StartCoroutine(WaitForCondition(
            () => freeKickManager.isActivated && freeKickManager.isWaitingForKickerSelection,
            4f,
            "FreeKickManager should wait for kicker selection after Final Thirds finish."));
        AssertTrue(
            MatchManager.Instance.currentState == MatchManager.GameState.FreeKickKickerSelect,
            "FreeKickManager should still be in the initial FreeKick state while waiting for the potential kicker.",
            MatchManager.GameState.FreeKickKickerSelect,
            MatchManager.Instance.currentState);

        yield return StartCoroutine(ClickShootingSheetHex("Noruega Kicker (-6,-6)", new Vector2Int(-6, -6)));
        yield return StartCoroutine(WaitForFreeKickState(MatchManager.GameState.FreeKickAttGK, "Free Kick should advance to attacking GK adjustment."));

        yield return StartCoroutine(AssertFreeKickOppositeTeamClickIgnored("FreeKickAttGK", new Vector2Int(16, 0), MatchManager.GameState.FreeKickAttGK));
        yield return StartCoroutine(ClickShootingSheetHex("AttGK Select (-16,0)", new Vector2Int(-16, 0)));
        yield return StartCoroutine(ClickShootingSheetHex("AttGK Move (-14,0)", new Vector2Int(-14, 0)));
        yield return StartCoroutine(WaitForFreeKickState(MatchManager.GameState.FreeKickDefGK1, "Free Kick should advance to first defending GK adjustment."));

        yield return StartCoroutine(AssertFreeKickOppositeTeamClickIgnored("FreeKickDefGK1", new Vector2Int(-14, 0), MatchManager.GameState.FreeKickDefGK1));
        yield return StartCoroutine(PressShootingSheetKey("DefGK Forfeit", KeyCode.X));
        yield return StartCoroutine(WaitForFreeKickState(MatchManager.GameState.FreeKickAtt1, "Free Kick should advance to first attacking setup phase."));

        yield return StartCoroutine(AssertFreeKickOppositeTeamClickIgnored("FreeKickAtt1", new Vector2Int(1, 10), MatchManager.GameState.FreeKickAtt1));
        yield return StartCoroutine(SelectAndMoveFreeKickPrepToken("Jansen", new Vector2Int(10, 10), new Vector2Int(14, 2)));
        yield return StartCoroutine(SelectAndMoveFreeKickPrepToken("Toothnail", new Vector2Int(8, 8), new Vector2Int(14, -1)));
        yield return StartCoroutine(WaitForFreeKickState(MatchManager.GameState.FreeKickDef1, "Free Kick should advance to first defending setup phase."));

        yield return StartCoroutine(AssertFreeKickOppositeTeamClickIgnored("FreeKickDef1", new Vector2Int(12, 12), MatchManager.GameState.FreeKickDef1));
        yield return StartCoroutine(SelectAndMoveFreeKickPrepToken("Marell", new Vector2Int(1, 10), new Vector2Int(14, 3)));
        yield return StartCoroutine(SelectAndMoveFreeKickPrepToken("Vladoiu", new Vector2Int(1, 2), new Vector2Int(14, -2)));
        yield return StartCoroutine(WaitForFreeKickState(MatchManager.GameState.FreeKickAtt2, "Free Kick should advance to second attacking setup phase."));

        yield return StartCoroutine(AssertFreeKickOppositeTeamClickIgnored("FreeKickAtt2", new Vector2Int(18, 0), MatchManager.GameState.FreeKickAtt2));
        yield return StartCoroutine(SelectAndMoveFreeKickPrepToken("Murphy", new Vector2Int(12, 12), new Vector2Int(14, -3)));
        yield return StartCoroutine(SelectAndMoveFreeKickPrepToken("Cafferata", new Vector2Int(0, 0), new Vector2Int(14, -6)));
        yield return StartCoroutine(WaitForFreeKickState(MatchManager.GameState.FreeKickDef2, "Free Kick should advance to second defending setup phase."));

        yield return StartCoroutine(AssertFreeKickOppositeTeamClickIgnored("FreeKickDef2", new Vector2Int(-4, -4), MatchManager.GameState.FreeKickDef2));
        yield return StartCoroutine(SelectAndMoveFreeKickPrepToken("Poulsen", new Vector2Int(18, 0), new Vector2Int(14, 6)));
        yield return StartCoroutine(PressShootingSheetKey("Defense Forfeit", KeyCode.X));
        yield return StartCoroutine(WaitForFreeKickState(MatchManager.GameState.FreeKickAtt3, "Free Kick should advance to third attacking setup phase."));

        yield return StartCoroutine(AssertFreeKickOppositeTeamClickIgnored("FreeKickAtt3", new Vector2Int(1, -10), MatchManager.GameState.FreeKickAtt3));
        yield return StartCoroutine(SelectAndMoveFreeKickPrepToken("Pavlovic", new Vector2Int(-4, -4), new Vector2Int(13, -2)));
        yield return StartCoroutine(PressShootingSheetKey("Attack Forfeit", KeyCode.X));
        yield return StartCoroutine(WaitForFreeKickState(MatchManager.GameState.FreeKickDef3, "Free Kick should advance to third defending setup phase."));

        yield return StartCoroutine(AssertFreeKickOppositeTeamClickIgnored("FreeKickDef3", new Vector2Int(13, -2), MatchManager.GameState.FreeKickDef3));
        yield return StartCoroutine(SelectAndMoveFreeKickPrepToken("Delgado", new Vector2Int(1, -10), new Vector2Int(1, -8)));
        yield return StartCoroutine(AssertFinalDefensiveForfeitBlockedByPaterson());

        yield return StartCoroutine(SelectAndMoveFreeKickPrepToken("Paterson", new Vector2Int(9, 0), new Vector2Int(-1, 7)));
        yield return StartCoroutine(WaitForFreeKickState(MatchManager.GameState.FreeKickDefGK2, "Free Kick should advance to second defending GK adjustment."));

        yield return StartCoroutine(AssertFreeKickOppositeTeamClickIgnored("FreeKickDefGK2", new Vector2Int(-14, 0), MatchManager.GameState.FreeKickDefGK2));
        yield return StartCoroutine(ClickShootingSheetHex("DefGK Select (16,0)", new Vector2Int(16, 0)));
        yield return StartCoroutine(ClickShootingSheetHex("DefGK Move (18,0)", new Vector2Int(18, 0)));
        yield return StartCoroutine(WaitForCondition(
            () => MatchManager.Instance.currentState == MatchManager.GameState.FreeKickDefineKicker
                && freeKickManager.isWaitingForFinalKickerSelection,
            3f,
            "Free Kick should wait for final kicker selection after the second defending GK adjustment."));
        AssertFreeKickFinalKickerSelectionReady("Free Kick should wait for final kicker selection after setup completes.");

        yield return StartCoroutine(AssertFreeKickInvalidFinalKickerClickIgnored("Final kicker selection", new Vector2Int(-1, 7)));
        yield return StartCoroutine(ClickShootingSheetHex("Select Kicker (7,-1)", new Vector2Int(7, -1)));
        yield return StartCoroutine(WaitForCondition(
            () => MatchManager.Instance.currentState == MatchManager.GameState.FreeKickExecution
                && freeKickManager.isWaitingForExecution,
            3f,
            "Free Kick should enter execution after selecting the kicker."));
        AssertFreeKickExecutionReady("Free Kick should enter execution after selecting the kicker.");

        HexCell foulHex = hexgrid.GetHexCellAt(new Vector3Int(8, 0, 0));
        AssertTrue(groundBallManager.ball.GetCurrentHex() == foulHex, "Free Kick ball should still be on the foul hex (8,0).", foulHex, groundBallManager.ball.GetCurrentHex());
        AssertTrue(foulHex != null && foulHex.CanShootFrom, "Foul hex (8,0) should be a CanShootFrom hex.");
        AssertTrue(shotManager.isAvailable, "Shot should be available from the Free Kick ball position.");
        AssertTrue(
            string.Equals(freeKickManager.GetInstructions(), "FK: Standard [P]ass, High Pass [C], [L]ong Ball or [S]hot", StringComparison.Ordinal),
            "Free Kick instructions should be the only execution prompt and should offer [S]hot.",
            "FK: Standard [P]ass, High Pass [C], [L]ong Ball or [S]hot",
            freeKickManager.GetInstructions());
        AssertTrue(string.IsNullOrEmpty(longBallManager.GetInstructions()), "Long Ball instructions should stay silent while FK is waiting for execution.", "", longBallManager.GetInstructions());
    }

    private IEnumerator SelectAndMoveFreeKickPrepToken(string playerName, Vector2Int selectCoordinates, Vector2Int destination)
    {
        yield return StartCoroutine(ClickShootingSheetHex($"{playerName} Select ({selectCoordinates.x},{selectCoordinates.y})", selectCoordinates));
        yield return StartCoroutine(MoveShootingToken(playerName, destination));
    }

    private IEnumerator WaitForFreeKickState(MatchManager.GameState expectedState, string failureMessage)
    {
        yield return StartCoroutine(WaitForCondition(
            () => MatchManager.Instance.currentState == expectedState && freeKickManager.isWaitingForSetupPhase,
            3f,
            failureMessage));
        AssertTrue(MatchManager.Instance.currentState == expectedState, $"FK phase should be {expectedState}.", expectedState, MatchManager.Instance.currentState);
        AssertTrue(freeKickManager.isActivated, $"{expectedState}: FreeKickManager should be active.");
        AssertTrue(freeKickManager.isWaitingForSetupPhase, $"{expectedState}: FreeKickManager should be waiting for setup clicks.");
        AssertTrue(!freeKickManager.isWaitingForFinalKickerSelection, $"{expectedState}: final kicker selection should not be active yet.");
        AssertTrue(!freeKickManager.isWaitingForExecution, $"{expectedState}: execution should not be active yet.");
    }

    private IEnumerator AssertFreeKickOppositeTeamClickIgnored(string phaseLabel, Vector2Int coordinates, MatchManager.GameState expectedState)
    {
        PlayerToken previousSelectedToken = freeKickManager.selectedToken;
        HexCell previousTargetHex = freeKickManager.targetHex;
        string previousInstructions = freeKickManager.GetInstructions();

        yield return StartCoroutine(ClickShootingSheetHex($"{phaseLabel} opposite-team click ({coordinates.x},{coordinates.y})", coordinates));

        AssertTrue(MatchManager.Instance.currentState == expectedState, $"{phaseLabel}: opposite-team click should not change the FK phase.", expectedState, MatchManager.Instance.currentState);
        AssertTrue(freeKickManager.selectedToken == previousSelectedToken, $"{phaseLabel}: opposite-team click should not select a token.", TokenName(previousSelectedToken), TokenName(freeKickManager.selectedToken));
        AssertTrue(freeKickManager.targetHex == previousTargetHex, $"{phaseLabel}: opposite-team click should not set a target hex.", HexCoordinates(previousTargetHex), HexCoordinates(freeKickManager.targetHex));
        AssertTrue(string.Equals(freeKickManager.GetInstructions(), previousInstructions, StringComparison.Ordinal), $"{phaseLabel}: opposite-team click should leave FK instructions unchanged.", previousInstructions, freeKickManager.GetInstructions());
    }

    private IEnumerator AssertFinalDefensiveForfeitBlockedByPaterson()
    {
        PlayerToken paterson = RequirePlayerToken("Paterson");
        HexCell patersonHex = paterson != null ? paterson.GetCurrentHex() : null;
        HexCell ballHex = groundBallManager.ball.GetCurrentHex();
        int patersonDistance = patersonHex != null && ballHex != null ? HexGridUtils.GetHexStepDistance(ballHex, patersonHex) : int.MaxValue;

        AssertTrue(MatchManager.Instance.currentState == MatchManager.GameState.FreeKickDef3, "Paterson forfeit guard should run in the final defensive FK phase.", MatchManager.GameState.FreeKickDef3, MatchManager.Instance.currentState);
        AssertTrue(patersonHex == hexgrid.GetHexCellAt(new Vector3Int(9, 0, 0)), "Paterson should still be on (9,0) before the blocked defensive forfeit.", "(9,0)", HexCoordinates(patersonHex));
        AssertTrue(patersonDistance <= 2, "Paterson should still be close enough to the ball to block defensive forfeit.", "<= 2", patersonDistance);

        yield return StartCoroutine(PressShootingSheetKey("Defense Forfeit blocked by Paterson", KeyCode.X));
        yield return new WaitForSeconds(0.3f);

        AssertTrue(MatchManager.Instance.currentState == MatchManager.GameState.FreeKickDef3, "Defense forfeit should be blocked while Paterson still has to move.", MatchManager.GameState.FreeKickDef3, MatchManager.Instance.currentState);
        AssertTrue(freeKickManager.isWaitingForSetupPhase, "Defense forfeit block should keep FK setup listening for the required defender move.");
        AssertTrue(paterson.GetCurrentHex() == patersonHex, "Blocked defensive forfeit should not move Paterson.", HexCoordinates(patersonHex), HexCoordinates(paterson.GetCurrentHex()));
    }

    private IEnumerator AssertFreeKickInvalidFinalKickerClickIgnored(string phaseLabel, Vector2Int coordinates)
    {
        PlayerToken previousSelectedKicker = freeKickManager.selectedKicker;
        string previousInstructions = freeKickManager.GetInstructions();

        yield return StartCoroutine(ClickShootingSheetHex($"{phaseLabel} invalid kicker click ({coordinates.x},{coordinates.y})", coordinates));

        AssertFreeKickFinalKickerSelectionReady($"{phaseLabel}: invalid kicker click should keep final kicker selection active.");
        AssertTrue(freeKickManager.selectedKicker == previousSelectedKicker, $"{phaseLabel}: invalid kicker click should not replace the selected kicker.", TokenName(previousSelectedKicker), TokenName(freeKickManager.selectedKicker));
        AssertTrue(string.Equals(freeKickManager.GetInstructions(), previousInstructions, StringComparison.Ordinal), $"{phaseLabel}: invalid kicker click should leave FK instructions unchanged.", previousInstructions, freeKickManager.GetInstructions());
    }

    private void AssertFreeKickFinalKickerSelectionReady(string message)
    {
        AssertTrue(MatchManager.Instance.currentState == MatchManager.GameState.FreeKickDefineKicker, message, MatchManager.GameState.FreeKickDefineKicker, MatchManager.Instance.currentState);
        AssertTrue(freeKickManager.isActivated, "FreeKickManager should remain active during final kicker selection.");
        AssertTrue(freeKickManager.isWaitingForFinalKickerSelection, "FreeKickManager should listen only for final kicker selection.");
        AssertTrue(!freeKickManager.isWaitingForSetupPhase, "FreeKickManager setup phase should be complete before final kicker selection.");
        AssertTrue(!freeKickManager.isWaitingForExecution, "FreeKickManager execution should not start before final kicker selection.");
    }

    private void AssertFreeKickExecutionReady(string message)
    {
        AssertTrue(MatchManager.Instance.currentState == MatchManager.GameState.FreeKickExecution, message, MatchManager.GameState.FreeKickExecution, MatchManager.Instance.currentState);
        AssertTrue(freeKickManager.isActivated, "FreeKickManager should remain active during FK execution selection.");
        AssertTrue(freeKickManager.isWaitingForExecution, "FreeKickManager should listen for FK execution keys.");
        AssertTrue(!freeKickManager.isWaitingForSetupPhase, "FreeKickManager setup phase should be complete before execution.");
        AssertTrue(!freeKickManager.isWaitingForFinalKickerSelection, "FreeKickManager final kicker selection should be complete before execution.");
    }

    private static string TokenName(PlayerToken token)
    {
        return token != null ? token.name : "<none>";
    }

    private static string HexCoordinates(HexCell hex)
    {
        return hex != null ? hex.coordinates.ToString() : "<none>";
    }

    private List<HexCell> GetShotTrajectoryPath()
    {
        FieldInfo field = typeof(ShotManager).GetField("trajectoryPath", BindingFlags.Instance | BindingFlags.NonPublic);
        AssertTrue(field != null, "ShotManager trajectoryPath field should exist for Free Kick Shot branch tests.");
        return field?.GetValue(shotManager) as List<HexCell>;
    }

    private static string PathCoordinates(IEnumerable<HexCell> path)
    {
        return path != null
            ? string.Join(", ", path.Where(hex => hex != null).Select(hex => hex.coordinates.ToString()))
            : "<none>";
    }

    private IEnumerator ExecuteShootingBranchStep(ShootingBranchDefinition branch, ShootingBranchStep step)
    {
        if (IsShootingOutcomeExpectation(step.Type))
        {
            yield return StartCoroutine(ForfeitShootingFinalThirdsBeforeOutcomeCheck(branch.Name));
        }

        yield return StartCoroutine(WaitForShootingStepReady(branch, step));
        AssertShootingStepPreconditions(branch, step);

        switch (step.Type)
        {
            case ShootingBranchStepType.ShotBlockRoll:
                yield return StartCoroutine(PerformRiggedShotBlockRoll(step.PlayerName, step.Roll));
                break;
            case ShootingBranchStepType.ShooterRoll:
                yield return StartCoroutine(PerformRiggedShooterRoll(step.PlayerName, step.Roll));
                break;
            case ShootingBranchStepType.GkSaveRoll:
                yield return StartCoroutine(PerformRiggedShotGkSaveRoll(step.PlayerName, step.Roll));
                break;
            case ShootingBranchStepType.HandlingRoll:
                yield return StartCoroutine(PerformRiggedShotHandlingRoll(step.Roll));
                break;
            case ShootingBranchStepType.LooseDirectionRoll:
                yield return StartCoroutine(PerformRiggedLooseDirectionRoll(step.Roll));
                break;
            case ShootingBranchStepType.LooseDistanceRoll:
                yield return StartCoroutine(PerformRiggedLooseDistanceRoll(step.Roll));
                break;
            case ShootingBranchStepType.LooseInterceptionRoll:
                yield return StartCoroutine(PerformRiggedLooseInterceptionRoll(step.PlayerName, step.Roll));
                break;
            case ShootingBranchStepType.GoalkeeperMove:
                yield return StartCoroutine(MoveShootingGoalkeeperIfWaiting(step.Coordinates, false));
                break;
            case ShootingBranchStepType.ExpectAnyOtherScenario:
                yield return StartCoroutine(AssertShootingAnyOtherScenario(branch, step.PlayerName));
                break;
            case ShootingBranchStepType.ExpectMovementToAttack:
                yield return StartCoroutine(AssertShootingMovementToAttack(branch));
                break;
            case ShootingBranchStepType.ExpectSaveAndHold:
                yield return StartCoroutine(AssertShootingSaveAndHold(branch));
                break;
            case ShootingBranchStepType.ExpectCornerNorth:
                yield return StartCoroutine(AssertShootingCorner(branch, true));
                break;
            case ShootingBranchStepType.ExpectCornerSouth:
                yield return StartCoroutine(AssertShootingCorner(branch, false));
                break;
            case ShootingBranchStepType.ExpectGoal:
                yield return StartCoroutine(AssertShootingGoal(branch));
                break;
            case ShootingBranchStepType.ExpectOwnGoal:
                yield return StartCoroutine(AssertShootingOwnGoal(branch));
                break;
            default:
                AssertTrue(false, $"Unhandled Shooting branch step in {branch.Name}: {step.Type}");
                break;
        }

        AssertShootingStepPostconditions(branch, step);
    }

    private static bool IsShootingOutcomeExpectation(ShootingBranchStepType stepType)
    {
        return stepType == ShootingBranchStepType.ExpectAnyOtherScenario
            || stepType == ShootingBranchStepType.ExpectMovementToAttack
            || stepType == ShootingBranchStepType.ExpectSaveAndHold
            || stepType == ShootingBranchStepType.ExpectCornerNorth
            || stepType == ShootingBranchStepType.ExpectCornerSouth
            || stepType == ShootingBranchStepType.ExpectGoal
            || stepType == ShootingBranchStepType.ExpectOwnGoal
            || stepType == ShootingBranchStepType.ExpectOffTarget;
    }

    private static bool IsShootingCornerExpectation(ShootingBranchStep step)
    {
        return step != null
            && (step.Type == ShootingBranchStepType.ExpectCornerNorth
                || step.Type == ShootingBranchStepType.ExpectCornerSouth);
    }

    private static ShootingBranchStep GetNextShootingStep(ShootingBranchDefinition branch, ShootingBranchStep step)
    {
        int stepIndex = branch.Steps.IndexOf(step);
        return stepIndex >= 0 && stepIndex + 1 < branch.Steps.Count
            ? branch.Steps[stepIndex + 1]
            : null;
    }

    private static bool HasPriorShootingStep(ShootingBranchDefinition branch, ShootingBranchStep step, ShootingBranchStepType stepType)
    {
        int stepIndex = branch.Steps.IndexOf(step);
        if (stepIndex <= 0)
        {
            return false;
        }

        for (int i = 0; i < stepIndex; i++)
        {
            if (branch.Steps[i].Type == stepType)
            {
                return true;
            }
        }

        return false;
    }

    private static string GetExpectedShootingLooseBallLastTouch(ShootingBranchDefinition branch, ShootingBranchStep step)
    {
        return HasPriorShootingStep(branch, step, ShootingBranchStepType.HandlingRoll)
            ? "Kuzmic"
            : "Yaneva";
    }

    private IEnumerator WaitForShootingStepReady(ShootingBranchDefinition branch, ShootingBranchStep step)
    {
        switch (step.Type)
        {
            case ShootingBranchStepType.ShotBlockRoll:
                yield return StartCoroutine(WaitForCondition(
                    () => shotManager.isWaitingForBlockDiceRoll,
                    4f,
                    $"Shooting branch '{branch.Name}' should be waiting for {step.PlayerName}'s shot block roll."));
                break;
            case ShootingBranchStepType.ShooterRoll:
                yield return StartCoroutine(WaitForCondition(
                    () => shotManager.isWaitingForShotRoll,
                    4f,
                    $"Shooting branch '{branch.Name}' should be waiting for {step.PlayerName}'s shot roll."));
                break;
            case ShootingBranchStepType.GkSaveRoll:
                yield return StartCoroutine(WaitForCondition(
                    () => shotManager.isWaitingForGKDiceRoll,
                    4f,
                    $"Shooting branch '{branch.Name}' should be waiting for {step.PlayerName}'s GK save roll."));
                break;
            case ShootingBranchStepType.HandlingRoll:
                yield return StartCoroutine(WaitForCondition(
                    () => shotManager.isWaitingforHandlingTest,
                    4f,
                    $"Shooting branch '{branch.Name}' should be waiting for GK handling."));
                break;
            case ShootingBranchStepType.LooseDirectionRoll:
                yield return StartCoroutine(WaitForCondition(
                    () => looseBallManager.isActivated && looseBallManager.isWaitingForDirectionRoll,
                    4f,
                    $"Shooting branch '{branch.Name}' should be waiting for loose-ball direction."));
                break;
            case ShootingBranchStepType.LooseDistanceRoll:
                yield return StartCoroutine(WaitForCondition(
                    () => looseBallManager.isActivated && looseBallManager.isWaitingForDistanceRoll,
                    4f,
                    $"Shooting branch '{branch.Name}' should be waiting for loose-ball distance."));
                break;
            case ShootingBranchStepType.LooseInterceptionRoll:
                yield return StartCoroutine(WaitForCondition(
                    () => looseBallManager.isActivated && looseBallManager.isWaitingForInterceptionRoll,
                    4f,
                    $"Shooting branch '{branch.Name}' should be waiting for {step.PlayerName}'s loose-ball interception."));
                break;
            case ShootingBranchStepType.GoalkeeperMove:
                yield return StartCoroutine(WaitForCondition(
                    () => goalKeeperManager.isActivated,
                    4f,
                    $"Shooting branch '{branch.Name}' should wake the defending GK for a free move to ({step.Coordinates.x},{step.Coordinates.y})."));
                break;
        }
    }

    private string GetShootingStateSummary()
    {
        return string.Join(" | ", new[]
        {
            shotManager != null ? shotManager.GetDebugStatus() : "Shot: <null>",
            looseBallManager != null ? looseBallManager.GetDebugStatus() : "Loose: <null>",
            goalKeeperManager != null ? goalKeeperManager.GetDebugStatus() : "GK: <null>",
            movementPhaseManager != null ? movementPhaseManager.GetDebugStatus() : "Movement: <null>",
            finalThirdManager != null ? finalThirdManager.GetDebugStatus() : "F3: <null>",
            freeKickManager != null ? freeKickManager.GetDebugStatus() : "FreeKick: <null>",
            goalFlowManager != null ? $"GoalFlow: isActivated={goalFlowManager.isActivated}" : "GoalFlow: <null>",
            MatchManager.Instance != null ? MatchManager.Instance.GetDebugStatus() : "Match: <null>",
            $"LTTBOP={GetTokenTestName(MatchManager.Instance?.LastTokenToTouchTheBallOnPurpose) ?? "<null>"}",
            $"Ball={groundBallManager?.ball?.GetCurrentHex()?.coordinates.ToString() ?? "<null>"}"
        });
    }

    private HexCell GetShootingBallHex()
    {
        return groundBallManager != null && groundBallManager.ball != null
            ? groundBallManager.ball.GetCurrentHex()
            : null;
    }

    private AvailabilityCheckResult CheckShootingManagerState(
        string context,
        bool expectShotActive = false,
        bool expectLooseActive = false,
        bool expectGkActive = false,
        bool expectMovementActive = false,
        bool expectFinalThirdActive = false,
        bool expectFreeKickActive = false,
        bool expectGoalFlowActive = false)
    {
        List<string> failures = new();

        if (shotManager.isActivated != expectShotActive) failures.Add($"ShotManager.isActivated should be {expectShotActive}");
        if (looseBallManager.isActivated != expectLooseActive) failures.Add($"LooseBallManager.isActivated should be {expectLooseActive}");
        if (goalKeeperManager.isActivated != expectGkActive) failures.Add($"GoalKeeperManager.isActivated should be {expectGkActive}");
        if (movementPhaseManager.isActivated != expectMovementActive) failures.Add($"MovementPhaseManager.isActivated should be {expectMovementActive}");
        if (finalThirdManager.isActivated != expectFinalThirdActive) failures.Add($"FinalThirdManager.isActivated should be {expectFinalThirdActive}");
        if (freeKickManager.isActivated != expectFreeKickActive) failures.Add($"FreeKickManager.isActivated should be {expectFreeKickActive}");
        if (goalFlowManager.isActivated != expectGoalFlowActive) failures.Add($"GoalFlowManager.isActivated should be {expectGoalFlowActive}");

        if (groundBallManager.isActivated) failures.Add("GroundBallManager should not be activated during Shooting branch resolution");
        if (firstTimePassManager.isActivated) failures.Add("FirstTimePassManager should not be activated during Shooting branch resolution");
        if (highPassManager.isActivated) failures.Add("HighPassManager should not be activated during Shooting branch resolution");
        if (longBallManager.isActivated) failures.Add("LongBallManager should not be activated during Shooting branch resolution");
        if (headerManager.isActivated) failures.Add("HeaderManager should not be activated during Shooting branch resolution");

        if (failures.Count > 0)
        {
            failures.Add($"{context} state: {GetShootingStateSummary()}");
        }

        return new AvailabilityCheckResult(failures.Count == 0, failures);
    }

    private void AssertShootingManagerState(
        string context,
        bool expectShotActive = false,
        bool expectLooseActive = false,
        bool expectGkActive = false,
        bool expectMovementActive = false,
        bool expectFinalThirdActive = false,
        bool expectFreeKickActive = false,
        bool expectGoalFlowActive = false)
    {
        AvailabilityCheckResult result = CheckShootingManagerState(
            context,
            expectShotActive,
            expectLooseActive,
            expectGkActive,
            expectMovementActive,
            expectFinalThirdActive,
            expectFreeKickActive,
            expectGoalFlowActive);
        AssertTrue(result.passed, $"{context}: unexpected Shooting manager state.", true, result.ToString());
    }

    private void AssertNoLiveActionAvailabilityDuringSetPieceRestart(string context)
    {
        AssertTrue(!movementPhaseManager.isAvailable, $"{context}: Movement Phase should not be available during set-piece restart setup.", false, movementPhaseManager.isAvailable);
        AssertTrue(!groundBallManager.isAvailable, $"{context}: Standard Pass should not be available during set-piece restart setup.", false, groundBallManager.isAvailable);
        AssertTrue(!firstTimePassManager.isAvailable, $"{context}: First Time Pass should not be available during set-piece restart setup.", false, firstTimePassManager.isAvailable);
        AssertTrue(!highPassManager.isAvailable, $"{context}: High Pass should not be available during set-piece restart setup.", false, highPassManager.isAvailable);
        AssertTrue(!longBallManager.isAvailable, $"{context}: Long Ball should not be available during set-piece restart setup.", false, longBallManager.isAvailable);
        AssertTrue(!shotManager.isAvailable, $"{context}: Shot should not be available during set-piece restart setup.", false, shotManager.isAvailable);
    }

    private void AssertShootingLastTouch(string expectedPlayerName, string context)
    {
        string actual = GetTokenTestName(MatchManager.Instance?.LastTokenToTouchTheBallOnPurpose);
        AssertTrue(
            NamesMatch(actual, expectedPlayerName),
            $"{context}: LastTokenToTouchTheBallOnPurpose should be {expectedPlayerName}.",
            expectedPlayerName,
            actual
        );
    }

    private void AssertShootingBallOnToken(string expectedPlayerName, string context)
    {
        PlayerToken expectedToken = RequirePlayerToken(expectedPlayerName);
        HexCell expectedHex = expectedToken != null ? expectedToken.GetCurrentHex() : null;
        HexCell actualHex = GetShootingBallHex();
        AssertTrue(
            expectedHex != null && actualHex == expectedHex,
            $"{context}: ball should be on {expectedPlayerName}'s hex.",
            expectedHex != null ? expectedHex.coordinates.ToString() : "<null>",
            actualHex != null ? actualHex.coordinates.ToString() : "<null>"
        );
    }

    private void AssertShootingBallInGoal(string context)
    {
        HexCell ballHex = GetShootingBallHex();
        AssertTrue(
            ballHex != null && ballHex.isInGoal != 0,
            $"{context}: ball should be on a goal hex.",
            "goal hex",
            ballHex != null ? ballHex.coordinates.ToString() : "<null>"
        );
    }

    private void AssertShootingBallInEmptySpace(string context)
    {
        HexCell ballHex = GetShootingBallHex();
        AssertTrue(ballHex != null, $"{context}: ball should have a resting hex after the loose-ball miss.");
        if (ballHex == null)
        {
            return;
        }

        AssertTrue(
            !ballHex.isAttackOccupied && !ballHex.isDefenseOccupied,
            $"{context}: Movement-to-attack should start from an empty loose-ball hex.",
            "empty hex",
            $"{ballHex.coordinates}; attackOccupied={ballHex.isAttackOccupied}; defenseOccupied={ballHex.isDefenseOccupied}"
        );
    }

    private void AssertShootingStepPreconditions(ShootingBranchDefinition branch, ShootingBranchStep step)
    {
        if (IsShootingOutcomeExpectation(step.Type))
        {
            return;
        }

        string context = $"Before Shooting branch '{branch.Name}' step {step.Type}";
        switch (step.Type)
        {
            case ShootingBranchStepType.ShotBlockRoll:
                AssertShootingManagerState(context, expectShotActive: true);
                AssertTrue(shotManager.isWaitingForBlockDiceRoll, $"{context}: ShotManager should be waiting for a block roll.", true, shotManager.GetDebugStatus());
                AssertTrue(!shotManager.isWaitingForShotRoll, $"{context}: ShotManager should not be waiting for the shooter roll yet.", false, shotManager.isWaitingForShotRoll);
                AssertTrue(!shotManager.isWaitingForGKDiceRoll, $"{context}: ShotManager should not be waiting for a GK save roll.", false, shotManager.isWaitingForGKDiceRoll);
                AssertTrue(NamesMatch(GetCurrentShotInteractionDefenderName(), step.PlayerName), $"{context}: wrong current shot blocker.", step.PlayerName, GetCurrentShotInteractionDefenderName());
                AssertShootingLastTouch("Yaneva", context);
                break;
            case ShootingBranchStepType.ShooterRoll:
                AssertShootingManagerState(context, expectShotActive: true);
                AssertTrue(shotManager.isWaitingForShotRoll, $"{context}: ShotManager should be waiting for the shooter roll.", true, shotManager.GetDebugStatus());
                AssertTrue(NamesMatch(GetTokenTestName(shotManager.shooter), step.PlayerName), $"{context}: wrong shooting token.", step.PlayerName, GetTokenTestName(shotManager.shooter));
                AssertShootingLastTouch(step.PlayerName, context);
                break;
            case ShootingBranchStepType.GkSaveRoll:
                AssertShootingManagerState(context, expectShotActive: true);
                AssertTrue(shotManager.isWaitingForGKDiceRoll, $"{context}: ShotManager should be waiting for the GK save roll.", true, shotManager.GetDebugStatus());
                AssertTrue(NamesMatch(GetCurrentShotInteractionDefenderName(), step.PlayerName), $"{context}: wrong current saving GK.", step.PlayerName, GetCurrentShotInteractionDefenderName());
                AssertShootingLastTouch("Yaneva", context);
                break;
            case ShootingBranchStepType.HandlingRoll:
                AssertShootingManagerState(context, expectShotActive: true);
                AssertTrue(shotManager.isWaitingforHandlingTest, $"{context}: ShotManager should be waiting for the handling test.", true, shotManager.GetDebugStatus());
                AssertShootingLastTouch("Yaneva", context);
                break;
            case ShootingBranchStepType.LooseDirectionRoll:
                AssertShootingManagerState(context, expectLooseActive: true);
                AssertTrue(looseBallManager.isWaitingForDirectionRoll, $"{context}: LooseBallManager should be waiting for the direction roll.", true, looseBallManager.GetDebugStatus());
                AssertShootingLastTouch(GetExpectedShootingLooseBallLastTouch(branch, step), context);
                break;
            case ShootingBranchStepType.LooseDistanceRoll:
                AssertShootingManagerState(context, expectLooseActive: true);
                AssertTrue(looseBallManager.isWaitingForDistanceRoll, $"{context}: LooseBallManager should be waiting for the distance roll.", true, looseBallManager.GetDebugStatus());
                AssertShootingLastTouch(GetExpectedShootingLooseBallLastTouch(branch, step), context);
                break;
            case ShootingBranchStepType.LooseInterceptionRoll:
                AssertShootingManagerState(context, expectLooseActive: true);
                AssertTrue(looseBallManager.isWaitingForInterceptionRoll, $"{context}: LooseBallManager should be waiting for an interception roll.", true, looseBallManager.GetDebugStatus());
                AssertTrue(NamesMatch(GetCurrentLooseInterceptorName(), step.PlayerName), $"{context}: wrong loose-ball interceptor.", step.PlayerName, GetCurrentLooseInterceptorName());
                break;
            case ShootingBranchStepType.GoalkeeperMove:
                AssertTrue(goalKeeperManager.isActivated, $"{context}: GoalKeeperManager should be activated for the defending GK free move.", true, GetShootingStateSummary());
                AssertTrue(shotManager.isActivated || looseBallManager.isActivated, $"{context}: either ShotManager or LooseBallManager should still own the flow while GK moves.", true, GetShootingStateSummary());
                PlayerToken defendingGk = hexgrid.GetDefendingGK();
                AssertTrue(NamesMatch(GetTokenTestName(defendingGk), "Kuzmic"), $"{context}: defending GK should be Kuzmic.", "Kuzmic", GetTokenTestName(defendingGk));
                HexCell destinationHex = hexgrid.GetHexCellAt(new Vector3Int(step.Coordinates.x, 0, step.Coordinates.y));
                AssertTrue(destinationHex != null, $"{context}: GK destination hex should exist.", true, step.Coordinates);
                if (destinationHex != null)
                {
                    bool destinationHighlighted = hexgrid.highlightedHexes.Contains(destinationHex);
                    bool alreadyAtDestination = defendingGk != null && defendingGk.GetCurrentHex() == destinationHex;
                    AssertTrue(
                        destinationHighlighted || alreadyAtDestination,
                        $"{context}: GK destination should be highlighted unless Kuzmic is already there.",
                        destinationHex.coordinates,
                        $"Kuzmic={defendingGk?.GetCurrentHex()?.coordinates.ToString() ?? "<null>"}; highlighted={string.Join(", ", hexgrid.highlightedHexes.Select(hex => hex.coordinates.ToString()))}");
                }
                AssertTrue(goalKeeperManager.GetInstructions().Contains("Defending GK"), $"{context}: GK instructions should be active.", true, goalKeeperManager.GetInstructions());
                break;
        }
    }

    private void AssertShootingStepPostconditions(ShootingBranchDefinition branch, ShootingBranchStep step)
    {
        if (IsShootingOutcomeExpectation(step.Type))
        {
            return;
        }

        string context = $"After Shooting branch '{branch.Name}' step {step.Type}";
        switch (step.Type)
        {
            case ShootingBranchStepType.LooseDirectionRoll:
                AssertTrue(looseBallManager.directionRoll == step.Roll - 1, $"{context}: loose-ball direction roll should be stored as zero-based value.", step.Roll - 1, looseBallManager.directionRoll);
                if (IsShootingCornerExpectation(GetNextShootingStep(branch, step)))
                {
                    AssertShootingLastTouch("Kuzmic", context);
                    break;
                }

                AssertShootingManagerState(context, expectLooseActive: true);
                AssertTrue(looseBallManager.isWaitingForDistanceRoll, $"{context}: LooseBallManager should advance to distance roll.", true, looseBallManager.GetDebugStatus());
                AssertShootingLastTouch(GetExpectedShootingLooseBallLastTouch(branch, step), context);
                break;
            case ShootingBranchStepType.LooseDistanceRoll:
                AssertTrue(looseBallManager.distanceRoll == step.Roll, $"{context}: loose-ball distance roll should be stored.", step.Roll, looseBallManager.distanceRoll);
                break;
            case ShootingBranchStepType.GoalkeeperMove:
                PlayerToken defendingGk = RequirePlayerToken("Kuzmic");
                HexCell expectedHex = hexgrid.GetHexCellAt(new Vector3Int(step.Coordinates.x, 0, step.Coordinates.y));
                AssertTrue(!goalKeeperManager.isActivated, $"{context}: GoalKeeperManager should deactivate after the GK move.", false, goalKeeperManager.GetDebugStatus());
                AssertTrue(defendingGk.GetCurrentHex() == expectedHex, $"{context}: Kuzmic should finish the GK move on the scripted hex.", expectedHex, defendingGk.GetCurrentHex());
                AssertTrue(!movementPhaseManager.isPlayerMoving, $"{context}: no player movement should remain in progress after GK move.", false, movementPhaseManager.isPlayerMoving);
                break;
            case ShootingBranchStepType.HandlingRoll:
                PlayerToken handlingGk = hexgrid.GetDefendingGK();
                bool handlingShouldHold = handlingGk != null && step.Roll < handlingGk.handling;
                AssertTrue(
                    handlingShouldHold ? shotManager.isWaitingForSaveandHoldScenario : looseBallManager.isActivated,
                    $"{context}: handling roll should resolve to the expected hold/spill state.",
                    true,
                    GetShootingStateSummary());
                AssertShootingLastTouch("Kuzmic", context);
                break;
        }
    }

    private IEnumerator PressShootingSheetKey(string label, KeyCode key, float delay = 0.1f)
    {
        Log($"{label}: pressing {key}");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(key, delay));
        yield return new WaitForSeconds(0.15f);
    }

    private IEnumerator ClickShootingSheetHex(string label, Vector2Int coordinates, float delay = 0.1f)
    {
        Log($"{label}: clicking ({coordinates.x}, {coordinates.y})");
        yield return StartCoroutine(gameInputManager.DelayedClick(coordinates, delay));
        yield return new WaitForSeconds(0.15f);
    }

    private IEnumerator MoveShootingToken(string playerName, Vector2Int destination)
    {
        yield return StartCoroutine(ClickShootingSheetHex($"Move {playerName} to ({destination.x},{destination.y})", destination));
        yield return StartCoroutine(WaitForTokenOnHex(playerName, destination, $"{playerName} should finish moving to ({destination.x},{destination.y})."));
    }

    private IEnumerator WaitForTokenOnHex(string playerName, Vector2Int coordinates, string failureMessage)
    {
        PlayerToken token = RequirePlayerToken(playerName);
        HexCell expectedHex = hexgrid.GetHexCellAt(new Vector3Int(coordinates.x, 0, coordinates.y));
        yield return StartCoroutine(WaitForCondition(
            () => token.GetCurrentHex() == expectedHex && !movementPhaseManager.isPlayerMoving,
            4f,
            failureMessage));
    }

    private IEnumerator ForfeitShootingFinalThirds()
    {
        float elapsedSeconds = 0f;
        while (!finalThirdManager.isActivated
            && !movementPhaseManager.isAvailable
            && !movementPhaseManager.isActivated
            && elapsedSeconds < 1.5f)
        {
            yield return null;
            elapsedSeconds += Time.deltaTime;
        }

        if (!finalThirdManager.isActivated)
        {
            yield return new WaitForSeconds(0.2f);
            yield break;
        }

        yield return StartCoroutine(ForfeitActiveShootingFinalThirds("prep"));
    }

    private IEnumerator ForfeitShootingFinalThirdsBeforeOutcomeCheck(string branchName)
    {
        float elapsedSeconds = 0f;
        while (!finalThirdManager.isActivated
            && (looseBallManager.isActivated || shotManager.isActivated || goalKeeperManager.isActivated)
            && elapsedSeconds < 6f)
        {
            yield return null;
            elapsedSeconds += Time.deltaTime;
        }

        elapsedSeconds = 0f;
        while (!finalThirdManager.isActivated
            && !movementPhaseManager.isActivated
            && !freeKickManager.isActivated
            && !goalFlowManager.isActivated
            && !shotManager.isWaitingForSaveandHoldScenario
            && elapsedSeconds < 1f)
        {
            yield return null;
            elapsedSeconds += Time.deltaTime;
        }

        if (!finalThirdManager.isActivated)
        {
            yield break;
        }

        Log($"Forfeiting Final Thirds before Shooting branch '{branchName}' outcome check.");
        yield return StartCoroutine(ForfeitActiveShootingFinalThirds("outcome"));
    }

    private IEnumerator ForfeitActiveShootingFinalThirds(string phaseLabel)
    {
        if (!finalThirdManager.isActivated)
        {
            yield break;
        }

        yield return StartCoroutine(PressShootingSheetKey("Forfeit AttF3", KeyCode.X));
        yield return new WaitForSeconds(0.2f);
        if (finalThirdManager.isActivated)
        {
            yield return StartCoroutine(PressShootingSheetKey("Forfeit DefF3", KeyCode.X));
        }

        yield return StartCoroutine(WaitForCondition(
            () => !finalThirdManager.isActivated,
            3f,
            $"Final Third should finish after both sides forfeit during Shooting {phaseLabel}."));
    }

    private IEnumerator MoveShootingGoalkeeperIfWaiting(Vector2Int destination, bool allowAlreadyResolved)
    {
        if (!goalKeeperManager.isActivated)
        {
            float elapsedSeconds = 0f;
            while (!goalKeeperManager.isActivated && elapsedSeconds < 2f)
            {
                yield return null;
                elapsedSeconds += Time.deltaTime;
            }

            if (goalKeeperManager.isActivated)
            {
                Log($"GK move to ({destination.x},{destination.y}) became available after waiting.");
            }
        }

        if (!goalKeeperManager.isActivated)
        {
            if (allowAlreadyResolved)
            {
                Log($"GK move to ({destination.x},{destination.y}) was already resolved or not required.");
                yield break;
            }

            yield return StartCoroutine(WaitForCondition(
                () => goalKeeperManager.isActivated,
                2f,
                "Defending GK should be waiting for a shooting free move."));
        }

        if (!goalKeeperManager.isActivated)
        {
            yield break;
        }

        PlayerToken defendingGk = hexgrid.GetDefendingGK();
        HexCell destinationHex = hexgrid.GetHexCellAt(new Vector3Int(destination.x, 0, destination.y));
        if (shotManager.shotType == "freeKick")
            AssertTrue(!DuelResolutionPanelController.IsVisible,
                "Free Kick result must release the pitch before the GK box move.");
        if (defendingGk != null && destinationHex != null && defendingGk.GetCurrentHex() == destinationHex)
        {
            Log($"Defending GK is already on ({destination.x},{destination.y}) for the Shooting branch; forfeiting the redundant GK move.");
            yield return StartCoroutine(PressShootingSheetKey("Forfeit GK move already on target", KeyCode.X));
            yield return StartCoroutine(WaitForCondition(
                () => !goalKeeperManager.isActivated && !movementPhaseManager.isPlayerMoving,
                4f,
                "Defending GK should complete the shooting free move by staying on the scripted hex."));
            yield break;
        }

        Log($"Moving defending GK to ({destination.x},{destination.y}) for the Shooting branch.");
        yield return StartCoroutine(gameInputManager.DelayedClick(destination, 0.1f));
        yield return StartCoroutine(WaitForCondition(
            () => !goalKeeperManager.isActivated && !movementPhaseManager.isPlayerMoving,
            4f,
            "Defending GK should complete the shooting free move."));
    }

    private IEnumerator PerformRiggedShotBlockRoll(string expectedDefenderName, int rigRoll)
    {
        yield return StartCoroutine(WaitForCondition(
            () => shotManager.isWaitingForBlockDiceRoll,
            3f,
            $"Shot should be waiting for {expectedDefenderName}'s block roll."));

        AssertFreeKickDicePresentation();

        string currentDefenderName = GetCurrentShotInteractionDefenderName();
        AssertTrue(
            NamesMatch(currentDefenderName, expectedDefenderName),
            $"Expected {expectedDefenderName} to be the next shot blocker.",
            expectedDefenderName,
            currentDefenderName
        );

        object currentInteraction = GetCurrentShotInteractionObject();
        PlayerToken blocker = GetShotInteractionDefender(currentInteraction);
        int requiredNaturalRoll = GetShotInteractionInt(currentInteraction, "requiredNaturalRoll", 6);
        int? effectiveTackling = GetShotInteractionNullableInt(currentInteraction, "effectiveTackling");
        RecordShootingShotBlockAttempt(blocker, requiredNaturalRoll, rigRoll, effectiveTackling);

        Log($"Rigging {expectedDefenderName} shot block roll to {rigRoll}");
        IEnumerator coroutine = InvokePrivateShotCoroutine("StartShotBlockRoll", new object[] { (int?)rigRoll }, typeof(int?));
        AssertTrue(coroutine != null, "Shot block roll coroutine should be available for the Shooting branch.");
        if (coroutine != null)
        {
            StartCoroutine(coroutine);
            yield return StartCoroutine(WaitForCondition(
                () => !shotManager.isWaitingForBlockDiceRoll || !NamesMatch(GetCurrentShotInteractionDefenderName(), expectedDefenderName),
                3f,
                $"Shot block roll for {expectedDefenderName} should be consumed."));
        }

        yield return new WaitForSeconds(0.2f);
        yield return StartCoroutine(DismissCompletedShotPresentation());
    }

    private IEnumerator PerformRiggedShooterRoll(string expectedShooterName, int rigRoll)
    {
        yield return StartCoroutine(WaitForCondition(
            () => shotManager.isWaitingForShotRoll,
            3f,
            $"Shot should be waiting for {expectedShooterName}'s shot roll."));

        AssertFreeKickDicePresentation();

        string shooterName = GetTokenTestName(shotManager.shooter);
        AssertTrue(
            NamesMatch(shooterName, expectedShooterName),
            $"Expected {expectedShooterName} to be the shooter.",
            expectedShooterName,
            shooterName
        );

        Log($"Rigging {expectedShooterName} shot roll to {rigRoll}");
        if (rigRoll == 1)
        {
            expectedShootingShotOffTarget = true;
        }
        StartCoroutine(shotManager.StartShotRoll(rigRoll));
        yield return StartCoroutine(WaitForCondition(
            () => !shotManager.isWaitingForShotRoll,
            3f,
            $"Shot roll for {expectedShooterName} should be consumed."));
        if (rigRoll == 1)
        {
            expectedShootingXGoals ??= CalculateCurrentShootingExpectedGoal();
        }
        yield return new WaitForSeconds(0.2f);
        if (shotManager.shotType == "freeKick")
        {
            var panel = DuelResolutionPanelController.Instance;
            AssertTrue(panel != null && panel.IsComplete,
                "Free Kick shot power must be displayed before blockers or the GK move start.");
            if (panel != null && rigRoll != 1 && shotManager.totalShotPower >= 9)
                AssertTrue(panel.outcome.text.Contains("Perfect height") && panel.outcome.text.Contains("blocks skipped"),
                    "Power 9+ must explain that all outfield blocks are skipped.");
        }
        yield return StartCoroutine(DismissCompletedShotPresentation());
    }

    private IEnumerator PerformRiggedShotGkSaveRoll(string expectedGoalkeeperName, int rigRoll)
    {
        yield return StartCoroutine(WaitForCondition(
            () => shotManager.isWaitingForGKDiceRoll,
            3f,
            $"Shot should be waiting for {expectedGoalkeeperName}'s save roll."));

        AssertFreeKickDicePresentation();

        string currentDefenderName = GetCurrentShotInteractionDefenderName();
        AssertTrue(
            NamesMatch(currentDefenderName, expectedGoalkeeperName),
            $"Expected {expectedGoalkeeperName} to be the saving goalkeeper.",
            expectedGoalkeeperName,
            currentDefenderName
        );

        object currentInteraction = GetCurrentShotInteractionObject();
        RecordShootingGkSaveExpectation(currentInteraction, rigRoll);
        Log($"Rigging {expectedGoalkeeperName} save roll to {rigRoll}");
        IEnumerator coroutine = InvokePrivateShotCoroutine("ResolveGKSavingAttempt", new[] { currentInteraction, (object)(int?)rigRoll }, currentInteraction?.GetType(), typeof(int?));
        AssertTrue(coroutine != null, "GK save roll coroutine should be available for the Shooting branch.");
        if (coroutine != null)
        {
            StartCoroutine(coroutine);
        }

        yield return new WaitForSeconds(0.2f);
        yield return StartCoroutine(DismissCompletedShotPresentation());
    }

    private IEnumerator PerformRiggedShotHandlingRoll(int rigRoll)
    {
        yield return StartCoroutine(WaitForCondition(
            () => shotManager.isWaitingforHandlingTest,
            3f,
            "Shot should be waiting for the GK handling test."));

        if (shotManager.shotType == "freeKick")
        {
            var panel = DuelResolutionPanelController.Instance;
            AssertTrue(panel != null && panel.IsAwaitingRoll && panel.title.text == "Free Kick Shot · Handling"
                && panel.singlePlayer.die.IsAnimating,
                "Free Kick saves must show the goalkeeper's animated Handling panel.");
        }

        Log($"Rigging GK handling roll to {rigRoll}");
        StartCoroutine(shotManager.ResolveHandlingTest(rigRoll));
        yield return new WaitForSeconds(0.2f);
        yield return StartCoroutine(DismissCompletedShotPresentation());

        PlayerToken defendingGk = hexgrid.GetDefendingGK();
        bool shouldHold = defendingGk != null && rigRoll < defendingGk.handling;
        yield return StartCoroutine(WaitForCondition(
            () => shouldHold
                ? shotManager.isWaitingForSaveandHoldScenario
                : looseBallManager.isActivated || !shotManager.isActivated,
            4f,
            "Handling test should resolve to Save-and-Hold or a loose ball."));
    }

    private IEnumerator PerformRiggedLooseDirectionRoll(int rigRoll)
    {
        yield return StartCoroutine(WaitForCondition(
            () => looseBallManager.isActivated && looseBallManager.isWaitingForDirectionRoll,
            4f,
            "LooseBallManager should be waiting for a direction roll."));

        Log($"Rigging loose-ball direction roll to {rigRoll}");
        looseBallManager.PerformDirectionRoll(rigRoll);
        yield return new WaitForSeconds(0.2f);
        yield return StartCoroutine(DismissCompletedShotPresentation());
    }

    private IEnumerator PerformRiggedLooseDistanceRoll(int rigRoll)
    {
        yield return StartCoroutine(WaitForCondition(
            () => looseBallManager.isActivated && looseBallManager.isWaitingForDistanceRoll,
            4f,
            "LooseBallManager should be waiting for a distance roll."));

        Log($"Rigging loose-ball distance roll to {rigRoll}");
        looseBallManager.PerformDistanceRoll(rigRoll);
        yield return new WaitForSeconds(0.2f);
        yield return StartCoroutine(DismissCompletedShotPresentation());
    }

    private IEnumerator PerformRiggedLooseInterceptionRoll(string expectedDefenderName, int rigRoll)
    {
        float elapsedSeconds = 0f;
        while (looseBallManager.isActivated
            && !looseBallManager.isWaitingForInterceptionRoll
            && elapsedSeconds < 4f)
        {
            yield return null;
            elapsedSeconds += Time.deltaTime;
        }

        if (!looseBallManager.isWaitingForInterceptionRoll)
        {
            AssertTrue(
                false,
                $"LooseBallManager should be waiting for {expectedDefenderName}'s interception roll.",
                true,
                looseBallManager.isWaitingForInterceptionRoll
            );
            yield break;
        }

        string currentDefenderName = GetCurrentLooseInterceptorName();
        PlayerToken currentInterceptor = looseBallManager.potentialInterceptor;
        AssertTrue(
            NamesMatch(currentDefenderName, expectedDefenderName),
            $"Expected {expectedDefenderName} to be the next loose-ball interceptor.",
            expectedDefenderName,
            currentDefenderName
        );

        RecordShootingLooseInterceptionAttempt(currentInterceptor, rigRoll);
        Log($"Rigging {expectedDefenderName} loose-ball interception roll to {rigRoll}");
        looseBallManager.PerformInterceptionRoll(rigRoll);
        yield return new WaitForSeconds(0.3f);
        yield return StartCoroutine(DismissCompletedShotPresentation());
    }

    private void AssertFreeKickDicePresentation()
    {
        if (shotManager.shotType != "freeKick") return;
        var panel = DuelResolutionPanelController.Instance;
        AssertTrue(panel != null && panel.IsOpen && panel.IsAwaitingRoll && panel.playerColumns.activeSelf,
            "Free Kick rolls must use the authored two-column shot panel.");
        if (panel == null || !panel.IsOpen) return;
        bool attackLeft = (shotManager.shooter.isHomeTeam ? MatchManager.Instance.homeTeamDirection
            : MatchManager.Instance.awayTeamDirection) == MatchManager.TeamAttackingDirection.LeftToRight;
        var attack = attackLeft ? panel.leftPlayer : panel.rightPlayer;
        var defense = attackLeft ? panel.rightPlayer : panel.leftPlayer;
        AssertTrue(attack.playerName.text.Contains(shotManager.shooter.playerName) && attack.modifier.text.Contains("xGoals"),
            "Free Kick attacker and xGoals must follow the team's attack direction.");
        if (shotManager.isWaitingForShotRoll)
            AssertTrue(attack.die.IsAnimating && !defense.die.IsAnimating,
                "The Free Kick shooter rolls before the wall and goalkeeper.");
        else
        {
            AssertTrue(!attack.die.IsAnimating && attack.total.text.Contains($"Total: {shotManager.totalShotPower}")
                && defense.die.IsAnimating,
                "Block and Saving panels must preserve the authoritative Free Kick shot power.");
            AssertTrue(defense.playerName.text.Contains(GetCurrentShotInteractionDefenderName()),
                "The defending die must belong to the actual pending blocker or goalkeeper.");
        }
    }

    private IEnumerator DismissCompletedShotPresentation()
    {
        var shared = DuelResolutionPanelController.Instance;
        if (shared != null && shared.IsComplete)
        {
            yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Escape, 0.1f));
            yield return null;
        }
        var header = HeaderChallengePanelController.Instance;
        if (header != null && header.IsOpen && HeaderChallengePanelController.IsHoldingContinuation)
        {
            yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Escape, 0.1f));
            yield return null;
        }
    }

    private IEnumerator AssertShootingAnyOtherScenario(ShootingBranchDefinition branch, string expectedReceiverName)
    {
        yield return StartCoroutine(WaitForCondition(
            () => !looseBallManager.isActivated && !shotManager.isActivated && !goalKeeperManager.isActivated,
            6f,
            $"Shooting branch '{branch.Name}' should finish resolving before AnyOtherScenario availability is checked."));

        string context = $"Shooting branch '{branch.Name}' AnyOtherScenario outcome";
        AssertShootingManagerState(context);

        AvailabilityCheckResult availabilityCheck = AssertCorrectAvailabilityAnyOtherScenario();
        AssertTrue(
            availabilityCheck.passed,
            $"Shooting branch '{branch.Name}' should end in AnyOtherScenario availability.",
            true,
            availabilityCheck.ToString()
        );

        if (!string.IsNullOrWhiteSpace(expectedReceiverName))
        {
            string actualReceiverName = GetTokenTestName(MatchManager.Instance.LastTokenToTouchTheBallOnPurpose);
            AssertTrue(
                NamesMatch(actualReceiverName, expectedReceiverName),
                $"Shooting branch '{branch.Name}' should end with {expectedReceiverName} as the last touch.",
                expectedReceiverName,
                actualReceiverName
            );
            AssertShootingBallOnToken(expectedReceiverName, context);
        }
    }

    private IEnumerator AssertShootingMovementToAttack(ShootingBranchDefinition branch)
    {
        yield return StartCoroutine(WaitForCondition(
            () => !looseBallManager.isActivated && movementPhaseManager.isActivated,
            6f,
            $"Shooting branch '{branch.Name}' should resolve into attacking Movement Phase."));

        string context = $"Shooting branch '{branch.Name}' Movement-to-attack outcome";
        AssertShootingManagerState(context, expectMovementActive: true);
        AssertShootingLastTouch(GetExpectedShootingLooseBallLastTouch(branch, branch.Steps.Last()), context);
        AssertShootingBallInEmptySpace(context);

        AvailabilityCheckResult availabilityCheck = AssertCorrectAvailabilityAfterGBToSpace();
        AssertTrue(
            availabilityCheck.passed,
            $"Shooting branch '{branch.Name}' should end with Movement Phase committed for attack.",
            true,
            availabilityCheck.ToString()
        );
    }

    private IEnumerator AssertShootingSaveAndHold(ShootingBranchDefinition branch)
    {
        yield return StartCoroutine(WaitForCondition(
            () => shotManager.isWaitingForSaveandHoldScenario,
            4f,
            $"Shooting branch '{branch.Name}' should wait for Save-and-Hold choice."));

        string context = $"Shooting branch '{branch.Name}' Save-and-Hold outcome";
        AssertShootingManagerState(context);
        AssertTrue(shotManager.isWaitingForSaveandHoldScenario, $"Shooting branch '{branch.Name}' should be in Save-and-Hold.");
        AssertShootingLastTouch("Kuzmic", context);
        AssertShootingBallOnToken("Kuzmic", context);
    }

    private IEnumerator AssertShootingCorner(ShootingBranchDefinition branch, bool north)
    {
        yield return StartCoroutine(WaitForCondition(
            () => freeKickManager.isActivated && freeKickManager.isWaitingForKickerSelection,
            6f,
            $"Shooting branch '{branch.Name}' should resolve to a corner."));

        string context = $"Shooting branch '{branch.Name}' {(north ? "north" : "south")} corner outcome";
        AssertShootingManagerState(context, expectFreeKickActive: true);
        AssertNoLiveActionAvailabilityDuringSetPieceRestart(context);
        AssertTrue(
            freeKickManager.isWaitingForKickerSelection,
            $"{context}: FreeKickManager should wait for the corner kicker selection.",
            true,
            freeKickManager.GetDebugStatus());
        AssertShootingLastTouch("Kuzmic", context);
        HexCell ballHex = freeKickManager.ball.GetCurrentHex();
        AssertTrue(ballHex != null, $"Shooting branch '{branch.Name}' corner should place the ball on a corner spot.");
        if (ballHex != null)
        {
            AssertTrue(
                north ? ballHex.coordinates.z > 0 : ballHex.coordinates.z < 0,
                $"Shooting branch '{branch.Name}' should resolve to the {(north ? "north" : "south")} corner.",
                north ? "z > 0" : "z < 0",
                ballHex.coordinates
            );
        }
    }

    private IEnumerator AssertShootingGoal(ShootingBranchDefinition branch)
    {
        yield return StartCoroutine(WaitForCondition(
            () => goalFlowManager.isActivated,
            6f,
            $"Shooting branch '{branch.Name}' should activate GoalFlow."));

        string context = $"Shooting branch '{branch.Name}' goal outcome";
        AssertShootingManagerState(context, expectGoalFlowActive: true);
        AssertTrue(goalFlowManager.isActivated, $"Shooting branch '{branch.Name}' should be in GoalFlow.");
        AssertShootingLastTouch("Yaneva", context);
        AssertShootingBallInGoal(context);
        while (goalFlowManager.isActivated)
        {
            yield return null;
        }
        AssertTrue(!shotManager.isActivated && !looseBallManager.isActivated && !goalKeeperManager.isActivated, $"{context}: Shooting flow managers should remain inactive after GoalFlow closes.", true, GetShootingStateSummary());
    }

    private IEnumerator AssertShootingOwnGoal(ShootingBranchDefinition branch)
    {
        yield return StartCoroutine(WaitForCondition(
            () => goalFlowManager.isActivated,
            6f,
            $"Shooting branch '{branch.Name}' should activate GoalFlow for an own-goal resolution."));

        string context = $"Shooting branch '{branch.Name}' own-goal outcome";
        AssertShootingManagerState(context, expectGoalFlowActive: true);
        AssertTrue(goalFlowManager.isActivated, $"Shooting branch '{branch.Name}' should be in GoalFlow for the own-goal resolution.");
        AssertTrue(MatchManager.Instance.LastTokenToTouchTheBallOnPurpose != null, $"{context}: own-goal resolution should keep a scoring token.", true, GetTokenTestName(MatchManager.Instance.LastTokenToTouchTheBallOnPurpose));
        AssertShootingBallInGoal(context);
        while (goalFlowManager.isActivated)
        {
            yield return null;
        }
        AssertTrue(!shotManager.isActivated && !looseBallManager.isActivated && !goalKeeperManager.isActivated, $"{context}: Shooting flow managers should remain inactive after GoalFlow closes.", true, GetShootingStateSummary());
    }

    private IEnumerator InvokePrivateShotCoroutine(string methodName, object[] parameters, params Type[] parameterTypes)
    {
        MethodInfo method = typeof(ShotManager)
            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .FirstOrDefault(candidate =>
            {
                if (candidate.Name != methodName)
                {
                    return false;
                }

                ParameterInfo[] methodParameters = candidate.GetParameters();
                if (methodParameters.Length != parameterTypes.Length)
                {
                    return false;
                }

                for (int i = 0; i < parameterTypes.Length; i++)
                {
                    if (parameterTypes[i] != null && methodParameters[i].ParameterType != parameterTypes[i])
                    {
                        return false;
                    }
                }

                return true;
            });

        AssertTrue(method != null, $"ShotManager private coroutine '{methodName}' should exist for the Shooting branch tests.");
        return method?.Invoke(shotManager, parameters) as IEnumerator;
    }

    private object InvokePrivateShotMethod(string methodName, object[] parameters, params Type[] parameterTypes)
    {
        MethodInfo method = typeof(ShotManager)
            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .FirstOrDefault(candidate =>
            {
                if (candidate.Name != methodName)
                {
                    return false;
                }

                ParameterInfo[] methodParameters = candidate.GetParameters();
                if (methodParameters.Length != parameterTypes.Length)
                {
                    return false;
                }

                for (int i = 0; i < parameterTypes.Length; i++)
                {
                    if (parameterTypes[i] != null && methodParameters[i].ParameterType != parameterTypes[i])
                    {
                        return false;
                    }
                }

                return true;
            });

        AssertTrue(method != null, $"ShotManager private method '{methodName}' should exist for the Shooting branch tests.");
        return method?.Invoke(shotManager, parameters);
    }

    private object GetCurrentShotInteractionObject()
    {
        FieldInfo field = typeof(ShotManager).GetField("currentShotInteraction", BindingFlags.Instance | BindingFlags.NonPublic);
        AssertTrue(field != null, "ShotManager currentShotInteraction field should exist for the Shooting branch tests.");
        return field?.GetValue(shotManager);
    }

    private string GetCurrentShotInteractionDefenderName()
    {
        object interaction = GetCurrentShotInteractionObject();
        return GetTokenTestName(GetShotInteractionDefender(interaction));
    }

    private PlayerToken GetShotInteractionDefender(object interaction)
    {
        if (interaction == null)
        {
            return null;
        }

        FieldInfo defenderField = interaction.GetType().GetField("defender", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return defenderField?.GetValue(interaction) as PlayerToken;
    }

    private HexCell GetShotInteractionHex(object interaction)
    {
        if (interaction == null)
        {
            return null;
        }

        FieldInfo field = interaction.GetType().GetField("interactionHex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return field?.GetValue(interaction) as HexCell;
    }

    private int GetShotInteractionInt(object interaction, string fieldName, int fallback)
    {
        if (interaction == null)
        {
            return fallback;
        }

        FieldInfo field = interaction.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field == null)
        {
            return fallback;
        }

        object value = field.GetValue(interaction);
        return value is int intValue ? intValue : fallback;
    }

    private int? GetShotInteractionNullableInt(object interaction, string fieldName)
    {
        if (interaction == null)
        {
            return null;
        }

        FieldInfo field = interaction.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field == null)
        {
            return null;
        }

        object value = field.GetValue(interaction);
        return value is int intValue ? intValue : value as int?;
    }

    private string GetCurrentLooseInterceptorName()
    {
        return GetTokenTestName(looseBallManager.potentialInterceptor);
    }

    private static string GetTokenTestName(PlayerToken token)
    {
        if (token == null)
        {
            return null;
        }

        return !string.IsNullOrWhiteSpace(token.playerName) ? token.playerName : token.name;
    }

    private static bool NamesMatch(string actualName, string expectedName)
    {
        if (string.IsNullOrWhiteSpace(expectedName))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(actualName))
        {
            return false;
        }

        return string.Equals(actualName, expectedName, StringComparison.OrdinalIgnoreCase)
            || actualName.IndexOf(expectedName, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public void LinkRoomSceneComponents()
    {
        // Attempt to assign all managers
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
        shotManager = FindAnyObjectByType<ShotManager>();
        finalThirdManager = FindAnyObjectByType<FinalThirdManager>();
        goalFlowManager = FindAnyObjectByType<GoalFlowManager>();
        kickoffManager = FindAnyObjectByType<KickoffManager>();
        goalKeeperManager = FindAnyObjectByType<GoalKeeperManager>();
        hexgrid = FindAnyObjectByType<HexGrid>();

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

    private void AssertEarlyDefendingKickoffFrontLineChoices(MatchManager match)
    {
        string defendingTeam = match.teamInAttack == MatchManager.TeamInAttack.Home ? "Away" : "Home";
        var legal = kickoffManager.BuildAutomatedSetupCandidates(defendingTeam);
        foreach (int jersey in new[] { 7, 11, 9, 10 })
        {
            var actor = match.playerTokenManager.allTokens.First(token => token != null && token.isPlaying
                && token.isHomeTeam == (defendingTeam == "Home") && token.jerseyNumber == jersey);
            int flank = System.Math.Sign(actor.GetCurrentHex().coordinates.z);
            var choices = legal.Where(action => action.actor == actor || action.actionType == RoomActionType.Confirm).ToList();
            bool hasLegalLine = choices.Any(action => action.actor == actor && action.targetHex != null
                && action.targetHex.coordinates.x == 0
                && (jersey is not (7 or 11) || flank == 0 || action.targetHex.coordinates.z * flank >= 0));
            AssertTrue(hasLegalLine, $"Opening fixture must offer a legal halfway destination for defending No. {jersey}.");
            var context = new RoomDecisionContext { manager = nameof(KickoffManager), expectedTeam = defendingTeam,
                expectedInput = "hex" };
            var snapshot = RoomAIDecisionSnapshotBuilder.Build(null, context, choices);
            var decision = CodexTacticianProvider.Instance.Decide(snapshot, choices, null,
                new RoomAITacticalWeights(), AIManager.CodexTacticianDifficulty.UltraHard);
            AssertTrue(decision.selectedCandidate?.actor == actor && decision.selectedCandidate.targetHex != null
                && decision.selectedCandidate.targetHex.coordinates.x == 0,
                $"Early defending kickoff No. {jersey} should choose halfway over generic receiver-protection bonuses.");
            if (jersey is 7 or 11)
                AssertTrue(flank == 0 || decision.selectedCandidate.targetHex.coordinates.z * flank >= 0,
                    $"Defending winger No. {jersey} must preserve the established flank.");
        }
        // Analysis only: no setup commands are executed and no move budget is
        // consumed before the existing attacking kickoff/first-pass scenario.
    }



    private void AssertDuelDefenderRollPresentation()
    {
        var panel = movementPhaseManager.duelResolutionPanel;
        if (panel == null || !panel.IsOpen) return; // GK duels are outside this prototype.
        var attacker = MatchManager.Instance.LastTokenToTouchTheBallOnPurpose;
        bool left = (attacker.isHomeTeam ? MatchManager.Instance.homeTeamDirection : MatchManager.Instance.awayTeamDirection)
            == MatchManager.TeamAttackingDirection.LeftToRight;
        AssertTrue(panel.DribblerOnLeft == left, "Duel columns must follow current attacking direction.");
        var dribbler = left ? panel.leftPlayer : panel.rightPlayer;
        var tackler = left ? panel.rightPlayer : panel.leftPlayer;
        AssertTrue(dribbler.attribute.text.Contains("xDr") && tackler.attribute.text.Contains("xTk"),
            "Duel attributes must show the initial expected-stat values.");
        AssertTrue(panel.leniency.column.anchorMin.x == (left ? .5f : 0f)
                && panel.injury.column.anchorMin.x == (left ? 0f : .5f),
            "Leniency must be under the tackler and injury under the dribbler.");
        AssertTrue(!tackler.die.IsAnimating && dribbler.die.IsAnimating,
            "Tackler result must freeze before the dribbler's die starts.");
        AssertTrue(movementPhaseManager.GetInstructions().Contains(dribbler.guidance.text),
            "Instruction box and panel must share the same required-roll guidance.");
        bool paused = MatchManager.Instance.IsPauseMenuOpen;
        gameInputManager.SimulateKeyDataPress(KeyCode.Escape);
        AssertTrue(panel.IsOpen && !panel.IsComplete, "Escape must not skip remaining duel rolls.");
        AssertTrue(MatchManager.Instance.IsPauseMenuOpen == paused, "Early Escape must not open Pause.");
        gameInputManager.SimulateKeyDataPress(KeyCode.Return);
        AssertTrue(!panel.IsResultHeld && !DuelResolutionPanelController.IsMatchClockHeld,
            "Enter must not hold the result before all required rolls finish.");
    }

    private bool checkedDuelAutomaticDismissal;

    // Exercise the real result gate before existing assertions inspect follow-up gameplay.
    private IEnumerator WaitForDuelResultPresentation()
    {
        var panel = movementPhaseManager.duelResolutionPanel;
        if (panel == null || !panel.IsOpen) yield break;
        // Injury resolution's outer coroutine starts the countdown on its next frame.
        yield return null;
        yield return null;
        if (!panel.IsComplete) yield break; // A foul still needs its two checks.
        AssertTrue(!movementPhaseManager.isWaitingForReposition && !movementPhaseManager.isWaitingForFoulDecision,
            "Duel result must hold repositioning and Play On/Take Foul until dismissal.");
        gameInputManager.SimulateKeyDataPress(KeyCode.A);
        gameInputManager.SimulateKeyDataPress(KeyCode.F);
        AssertTrue(panel.IsOpen && panel.IsComplete, "A/F must not bypass the completed duel panel.");
        if (!checkedDuelAutomaticDismissal)
        {
            checkedDuelAutomaticDismissal = true;
            gameInputManager.SimulateKeyDataPress(KeyCode.Return);
            AssertTrue(panel.IsResultHeld && DuelResolutionPanelController.IsMatchClockHeld,
                "Enter should hold the completed result and match clock.");
            string heldClock = MatchManager.Instance.GetClockDisplayText();
            string heldFooter = panel.footer.text;
            yield return new WaitForSecondsRealtime(1.2f);
            AssertTrue(panel.IsOpen && panel.IsResultHeld && panel.footer.text == heldFooter,
                "The held result must stay visible without advancing its countdown.");
            AssertTrue(MatchManager.Instance.GetClockDisplayText() == heldClock,
                "The match clock must not advance while the result is held.");
            gameInputManager.SimulateKeyDataPress(KeyCode.Return);
            AssertTrue(!panel.IsResultHeld && !DuelResolutionPanelController.IsMatchClockHeld,
                "Enter again should release the hold and resume the countdown.");
            float started = Time.unscaledTime;
            yield return WaitForCondition(() => !panel.IsOpen, panel.resultSeconds + 1f,
                "Duel result should close automatically after its countdown.");
            AssertTrue(Time.unscaledTime - started >= panel.resultSeconds - .15f,
                "The completed result should remain readable for five seconds.");
        }
        else
        {
            bool wasPaused = MatchManager.Instance.IsPauseMenuOpen;
            gameInputManager.SimulateKeyDataPress(KeyCode.Escape);
            AssertTrue(!panel.IsOpen, "Escape must close a completed duel result.");
            AssertTrue(!DuelResolutionPanelController.IsMatchClockHeld, "Closing the panel must release its clock hold.");
            AssertTrue(MatchManager.Instance.IsPauseMenuOpen == wasPaused,
                "Closing a completed duel result must not toggle Pause.");
        }
        yield return null; // Resume the manager's existing continuation.
    }



    private IEnumerator ResolveNutmegAuditStealFoul(bool injury, bool red = false)
    {
        PlayerToken defender = movementPhaseManager.selectedDefender;
        if (red && !defender.isBooked) defender.ReceiveYellowCard();
        StartCoroutine(movementPhaseManager.PerformBallInterceptionDiceRoll(1));
        yield return WaitForCondition(() => movementPhaseManager.isWaitingForYellowCardRoll, 4f,
            "Steal foul should open leniency checks.");
        movementPhaseManager.PerformLeniencyTest(red ? 6 : 1);
        yield return WaitForCondition(() => movementPhaseManager.isWaitingForInjuryRoll, 4f,
            "Steal foul should open injury checks.");
        movementPhaseManager.PerformInjuryTest(injury ? 6 : 1);
        yield return WaitForDuelResultPresentation();
        if (red) yield break;
        yield return WaitForCondition(() => movementPhaseManager.isWaitingForFoulDecision, 4f,
            "A non-forced steal foul must offer Play On.");
        yield return gameInputManager.DelayedKeyDataPress(KeyCode.A, .1f);
        AssertTrue(!movementPhaseManager.isWaitingForReposition,
            "Steal foul followed by Play On must never offer reposition.");
    }



    private bool IsOobBranchOutcomeReached(OobLongBallBranchDefinition branch)
    {
        HexCell expectedHex = hexgrid.GetHexCellAt(new Vector3Int(branch.ExpectedReentryHex.x, 0, branch.ExpectedReentryHex.y));
        HexCell ballHex = longBallManager.ball.GetCurrentHex();
        bool isOnExpectedHex = expectedHex != null && ballHex == expectedHex;

        return branch.ExpectedRestart switch
        {
            OobRestartExpectation.ThrowIn =>
                MatchManager.Instance.currentState == MatchManager.GameState.WaitingForThrowInTaker
                && isOnExpectedHex
                && !longBallManager.isActivated,
            OobRestartExpectation.CornerKick =>
                freeKickManager.isActivated
                && freeKickManager.isCornerKick
                && freeKickManager.isWaitingForKickerSelection
                && isOnExpectedHex
                && !longBallManager.isActivated,
            OobRestartExpectation.GoalKick =>
                MatchManager.Instance.currentState == MatchManager.GameState.WaitingForGoalKickFinalThirds
                && isOnExpectedHex
                && !longBallManager.isActivated,
            _ => false
        };
    }

    private void AssertOobBranchOutcome(OobLongBallBranchDefinition branch)
    {
        HexCell expectedHex = hexgrid.GetHexCellAt(new Vector3Int(branch.ExpectedReentryHex.x, 0, branch.ExpectedReentryHex.y));
        HexCell ballHex = longBallManager.ball.GetCurrentHex();
        AssertTrue(expectedHex != null, $"OOB branch '{branch.Name}' expected re-entry hex should exist.", true, branch.ExpectedReentryHex);
        AssertTrue(ballHex == expectedHex, $"OOB branch '{branch.Name}' should place the ball on the expected re-entry hex.", expectedHex, ballHex);
        AssertTrue(MatchManager.Instance.teamInAttack == MatchManager.TeamInAttack.Away, $"OOB branch '{branch.Name}' should award the restart to Away.", MatchManager.TeamInAttack.Away, MatchManager.Instance.teamInAttack);

        switch (branch.ExpectedRestart)
        {
            case OobRestartExpectation.ThrowIn:
                AssertTrue(MatchManager.Instance.currentState == MatchManager.GameState.WaitingForThrowInTaker, $"OOB branch '{branch.Name}' should wait for throw-in taker.", MatchManager.GameState.WaitingForThrowInTaker, MatchManager.Instance.currentState);
                break;
            case OobRestartExpectation.CornerKick:
                AssertTrue(freeKickManager.isActivated && freeKickManager.isCornerKick, $"OOB branch '{branch.Name}' should activate Corner Kick mode.", true, freeKickManager.GetDebugStatus());
                AssertTrue(freeKickManager.isWaitingForKickerSelection, $"OOB branch '{branch.Name}' should wait for corner kicker selection.", true, freeKickManager.GetDebugStatus());
                AssertNoLiveActionAvailabilityDuringSetPieceRestart($"OOB branch '{branch.Name}' corner setup");
                AssertOobBranchCornerLogged(branch);
                break;
            case OobRestartExpectation.GoalKick:
                AssertTrue(MatchManager.Instance.currentState == MatchManager.GameState.WaitingForGoalKickFinalThirds, $"OOB branch '{branch.Name}' should wait for goal-kick Final Thirds.", MatchManager.GameState.WaitingForGoalKickFinalThirds, MatchManager.Instance.currentState);
                AssertTrue(!freeKickManager.isActivated || !freeKickManager.isCornerKick, $"OOB branch '{branch.Name}' should not activate generic Corner/Free Kick setup for a Goal Kick.", false, freeKickManager.GetDebugStatus());
                break;
        }
    }

    private void AssertOobBranchCornerLogged(OobLongBallBranchDefinition branch)
    {
        MatchManager.TeamStats awayTeamStats = MatchManager.Instance.gameData.stats.GetTeamStats(false);
        MatchManager.TeamStats homeTeamStats = MatchManager.Instance.gameData.stats.GetTeamStats(true);

        AssertTrue(
            awayTeamStats.totalCorners == 1,
            $"OOB branch '{branch.Name}' should log exactly one corner for Away.",
            1,
            awayTeamStats.totalCorners);
        AssertTrue(
            homeTeamStats.totalCorners == 0,
            $"OOB branch '{branch.Name}' should not log a corner for Home.",
            0,
            homeTeamStats.totalCorners);
    }


    private IEnumerator PrepareDirectHeaderAuditState()
    {
        yield return StartCoroutine(PrepareHighPassAvailabilityFromKickoff(2));

        highPassManager.isAvailable = false;
        highPassManager.isActivated = false;
        longBallManager.isAvailable = false;
        groundBallManager.isAvailable = false;
        firstTimePassManager.isAvailable = false;
        movementPhaseManager.isAvailable = false;

        headerManager.ResetHeader();
        headerManager.isActivated = true;
        headerManager.isAvailable = false;
        headerManager.isWaitingForAttackerSelection = false;
        headerManager.isWaitingForDefenderSelection = false;
        headerManager.isWaitingForControlOrHeaderDecision = false;
        headerManager.isWaitingForControlOrHeaderDecisionDef = false;
        headerManager.iswaitingForChallengeWinnerSelection = false;
        headerManager.isWaitingForHeaderTargetSelection = false;
        headerManager.isWaitingForHeaderAtGoal = false;
        headerManager.headerAtGoalDeclared = false;
        headerManager.attackFreeHeader = false;
        headerManager.attackControlBall = false;
        headerManager.defenseWonFreeHeader = false;
        headerManager.defenseBallControl = false;

        PlayerToken passer = RequirePlayerToken("Cafferata");
        if (headerManager.ball.GetCurrentHex() == null && passer.GetCurrentHex() != null)
        {
            headerManager.ball.PlaceAtCell(passer.GetCurrentHex());
        }

        MatchManager.Instance.SetLastToken(passer);
        MatchManager.Instance.currentState = MatchManager.GameState.HeaderGeneric;
    }

    private static string GetHeaderSortNameForTest(PlayerToken token)
    {
        if (token == null) return "";
        return string.IsNullOrWhiteSpace(token.playerName) ? token.name : token.playerName;
    }

    private static int GetHeaderAttributeForTest(PlayerToken token)
    {
        if (token == null) return 0;
        return token.IsGoalKeeper ? token.aerial : token.heading;
    }

    private List<PlayerToken> BuildExpectedHeaderOrderForTest(IEnumerable<PlayerToken> tokens, bool keepGoalKeeperLast)
    {
        HexCell ballHex = headerManager.ball.GetCurrentHex();
        return tokens
            .Where(token => token != null)
            .Distinct()
            .OrderBy(token => keepGoalKeeperLast && token.IsGoalKeeper ? 1 : 0)
            .ThenBy(token => HexGridUtils.GetHexStepDistance(ballHex, token.GetCurrentHex()))
            .ThenByDescending(GetHeaderAttributeForTest)
            .ThenBy(GetHeaderSortNameForTest)
            .ToList();
    }



    private IEnumerator Scenario_038_HeaderAtGoal_Branch(HeaderAtGoalBranchDefinition branch)
    {
        Log($"Starting Header at Goal branch: {branch.Name}");
        yield return StartCoroutine(PrepareScriptedHeaderAtGoal(branch.GkRush));

        yield return StartCoroutine(PerformHeaderAtGoalRoll("Yaneva", branch.AttackerHeaderRoll));
        yield return StartCoroutine(PerformHeaderAtGoalRoll("Soares", branch.DefenderHeaderRoll));

        if (branch.GoalkeeperHeaderOrAerialRoll.HasValue)
        {
            if (branch.GkRush)
            {
                yield return StartCoroutine(PerformHeaderAtGoalRoll("Kuzmic", branch.GoalkeeperHeaderOrAerialRoll.Value));
            }
            else
            {
                yield return StartCoroutine(PerformHeaderAtGoalGkSave(branch.GoalkeeperHeaderOrAerialRoll.Value));
            }
        }

        if (branch.HandlingRoll.HasValue)
        {
            yield return StartCoroutine(PerformHeaderAtGoalHandling(branch.HandlingRoll.Value));
        }

        if (branch.LooseDirectionRoll.HasValue)
        {
            yield return StartCoroutine(PerformRiggedLooseDirectionRoll(branch.LooseDirectionRoll.Value));
        }

        if (branch.LooseDistanceRoll.HasValue)
        {
            yield return StartCoroutine(PerformRiggedLooseDistanceRoll(branch.LooseDistanceRoll.Value));
        }

        if (!string.IsNullOrWhiteSpace(branch.ForbiddenLooseInterceptorName))
        {
            yield return StartCoroutine(AssertLooseInterceptorNotOffered(branch.ForbiddenLooseInterceptorName));
        }

        if (!string.IsNullOrWhiteSpace(branch.LooseInterceptionPlayerName) && branch.LooseInterceptionRoll.HasValue)
        {
            yield return StartCoroutine(PerformRiggedLooseInterceptionRoll(branch.LooseInterceptionPlayerName, branch.LooseInterceptionRoll.Value));
        }

        yield return StartCoroutine(AssertHeaderAtGoalOutcome(branch));
        LogFooterofTest($"Header at Goal {branch.Name}");
    }

    private IEnumerator PrepareScriptedHeaderAtGoal(bool gkRush)
    {
        yield return StartCoroutine(PrepareHighPassAvailabilityFromKickoff(2));

        Log("Pressing C - Start scripted High Pass");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.C, 0.1f));
        AssertTrue(highPassManager.isActivated, "Scripted High Pass should activate after pressing C.");

        Log("Clicking (13, 1) - Select scripted High Pass target");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(13, 1), 0.1f));
        AssertTrue(highPassManager.currentTargetHex == hexgrid.GetHexCellAt(new Vector3Int(13, 0, 1)), "Scripted High Pass should select (13,1).", hexgrid.GetHexCellAt(new Vector3Int(13, 0, 1)), highPassManager.currentTargetHex);

        Log("Clicking (13, 1) again - Confirm scripted High Pass target");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(13, 1), 0.1f));
        yield return StartCoroutine(WaitForCondition(
            () => highPassManager.isWaitingForDefenderSelection,
            2f,
            "Scripted High Pass should wait for defender HP movement."));

        Log("Clicking (18, 0) - Select scripted defending token");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(18, 0), 0.1f));
        yield return StartCoroutine(WaitForCondition(
            () => highPassManager.isWaitingForDefenderMove,
            2f,
            "Scripted High Pass should wait for the selected defender move."));

        Log("Clicking (18, 3) - Move scripted defending token");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(18, 3), 0.1f));
        yield return StartCoroutine(WaitForCondition(
            () => highPassManager.isWaitingForAccuracyRoll,
            4f,
            "Scripted High Pass should wait for accuracy after defender movement."));

        Log("Rigging scripted High Pass accuracy roll to 6");
        highPassManager.PerformAccuracyRoll(6);
        yield return StartCoroutine(WaitForCondition(
            () => goalKeeperManager.isActivated,
            5f,
            "Scripted High Pass into the box should offer the defending GK box move."));

        Log("Clicking (16, 1) - Move defending GK for box entry");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(16, 1), 0.1f));
        yield return StartCoroutine(WaitForCondition(
            () => highPassManager.isWaitingForDefGKChallengeDecision,
            4f,
            "Scripted High Pass should offer the GK rush decision after the box move."));

        if (gkRush)
        {
            Log("Clicking (15, 2) - Rush defending GK");
            yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(15, 2), 0.1f));
            yield return StartCoroutine(WaitForCondition(
                () => finalThirdManager.isActivated && !highPassManager.isActivated,
                4f,
                "Scripted High Pass should trigger Final Third after GK rush."));
        }
        else
        {
            Log("Pressing X - Forfeit defending GK rush");
            yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.X, 0.1f));
            yield return StartCoroutine(WaitForCondition(
                () => finalThirdManager.isActivated && !highPassManager.isActivated,
                4f,
                "Scripted High Pass should trigger Final Third after GK rush forfeit."));
        }

        yield return StartCoroutine(ForfeitActiveFinalThirds());
        yield return StartCoroutine(WaitForCondition(
            () => headerManager.isActivated && headerManager.isWaitingForHeaderAtGoal,
            5f,
            "Scripted High Pass should activate Header and offer header-at-goal after F3."));

        Log("Clicking (19, -2) - Declare headed shot target");
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(19, -2), 0.1f));
        yield return StartCoroutine(WaitForCondition(
            () => headerManager.isWaitingForDefenderSelection,
            3f,
            "Header should wait for defensive challenges after headed shot target selection."));

        Log("Pressing A - Defense challenges with all eligible defenders");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.A, 0.1f));
        yield return StartCoroutine(WaitForCondition(
            () => headerManager.isWaitingForHeaderRoll,
            3f,
            "Header should wait for challenge rolls after defense selects all challengers."));
    }

    private IEnumerator PerformHeaderAtGoalRoll(string expectedTokenName, int rigRoll)
    {
        yield return StartCoroutine(WaitForCondition(
            () => headerManager.isWaitingForHeaderRoll,
            3f,
            $"Header at Goal should wait for {expectedTokenName}'s header/aerial roll."));

        AssertTrue(
            NamesMatch(GetTokenTestName(headerManager.tokenRolling), expectedTokenName),
            $"Header at Goal should roll with {expectedTokenName}.",
            expectedTokenName,
            GetTokenTestName(headerManager.tokenRolling));

        Log($"Rigging Header at Goal roll for {expectedTokenName} to {rigRoll}");
        headerManager.PerformHeaderRoll(rigRoll);
        yield return new WaitForSeconds(0.25f);
        var headerPanel = HeaderChallengePanelController.Instance;
        if (headerPanel != null && headerPanel.IsOpen && HeaderChallengePanelController.IsHoldingContinuation)
        {
            // The challenge's completed result owns input until dismissal.
            yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Escape, 0.1f));
            yield return null;
        }
    }

    private IEnumerator PerformHeaderAtGoalGkSave(int rigRoll)
    {
        yield return StartCoroutine(WaitForCondition(
            () => shotManager.isActivated && shotManager.isHeaderAtGoal && shotManager.isWaitingForGKDiceRoll,
            4f,
            "Header at Goal should wait for Kuzmic's saving roll."));

        Log($"Rigging Kuzmic header save roll to {rigRoll}");
        var panel = DuelResolutionPanelController.Instance;
        AssertTrue(panel != null && panel.IsOpen && panel.IsAwaitingRoll,
            "Non-jumping GK Saving should use the authored single-roll panel.");
        AssertTrue(!HeaderChallengePanelController.IsVisible,
            "The aerial challenge must release input before the Saving roll.");
        AssertTrue(!shotManager.isWaitingForShotRoll && !shotManager.isWaitingForBlockDiceRoll,
            "A header save must not introduce shooter or outfield block rolls.");
        shotManager.PerformGKHeaderSave(rigRoll);
        yield return StartCoroutine(DismissHeaderGoalkeeperResult(panel));
    }

    private IEnumerator PerformHeaderAtGoalHandling(int rigRoll)
    {
        yield return StartCoroutine(WaitForCondition(
            () => shotManager.isWaitingforHandlingTest,
            4f,
            "Header at Goal should wait for Kuzmic's handling roll."));

        Log($"Rigging Kuzmic handling roll to {rigRoll}");
        var panel = DuelResolutionPanelController.Instance;
        AssertTrue(panel != null && panel.IsOpen && panel.IsAwaitingRoll,
            "Header Handling should use a separate single-roll check after Saving dismissal.");
        StartCoroutine(shotManager.ResolveHandlingTest(rigRoll));
        yield return StartCoroutine(DismissHeaderGoalkeeperResult(panel));
    }

    private IEnumerator DismissHeaderGoalkeeperResult(DuelResolutionPanelController panel)
    {
        yield return StartCoroutine(WaitForCondition(() => panel != null
                && (panel.IsComplete || looseBallManager.isWaitingForDirectionRoll),
            4f, "Goalkeeper result should remain visible or expand into its loose-ball continuation."));
        if (looseBallManager.isWaitingForDirectionRoll && panel != null)
        {
            AssertTrue(panel.IsOpen && panel.scatterContinuationContent.activeSelf,
                "Tied Saving or failed Handling must retain its result above the loose-ball expansion.");
            AssertTrue(looseBallManager.IsInstructionExpectingHomeTeam() == looseBallManager.causingDeflection.isHomeTeam,
                "The deflecting defender's team must own loose-ball scatter input.");
            yield break;
        }
        if (panel == null || !panel.IsComplete) yield break;
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Return, 0.1f));
        AssertTrue(panel.IsResultHeld, "Enter should hold the goalkeeper result.");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Escape, 0.1f));
        AssertTrue(!panel.IsOpen, "Escape should release the goalkeeper result and input ownership.");
        yield return new WaitForSeconds(0.4f);
    }

    private IEnumerator AssertLooseInterceptorNotOffered(string forbiddenPlayerName)
    {
        float elapsedSeconds = 0f;
        while (looseBallManager.isActivated
            && !looseBallManager.isWaitingForInterceptionRoll
            && !movementPhaseManager.isActivated
            && !finalThirdManager.isActivated
            && !freeKickManager.isActivated
            && !goalFlowManager.isActivated
            && elapsedSeconds < 1.5f)
        {
            yield return null;
            elapsedSeconds += Time.deltaTime;
        }

        if (looseBallManager.isWaitingForInterceptionRoll)
        {
            string currentInterceptorName = GetCurrentLooseInterceptorName();
            AssertTrue(
                !NamesMatch(currentInterceptorName, forbiddenPlayerName),
                $"Header at Goal should not offer {forbiddenPlayerName} a loose-ball interception after they jumped in the header challenge.",
                $"not {forbiddenPlayerName}",
                currentInterceptorName);
        }
    }

    private IEnumerator AssertHeaderAtGoalOutcome(HeaderAtGoalBranchDefinition branch)
    {
        switch (branch.ExpectedOutcome)
        {
            case HeaderAtGoalOutcomeExpectation.Goal:
                yield return StartCoroutine(WaitForCondition(
                    () => goalFlowManager.isActivated,
                    6f,
                    $"Header at Goal branch '{branch.Name}' should activate GoalFlow."));
                while (goalFlowManager.isActivated) yield return null;
                AssertHeaderAtGoalResetAfterRightSideGoal(branch);
                break;

            case HeaderAtGoalOutcomeExpectation.CornerKick:
                yield return StartCoroutine(WaitForCondition(
                    () => finalThirdManager.isActivated,
                    6f,
                    $"Header at Goal branch '{branch.Name}' should offer Final Thirds before corner setup."));
                AssertTrue(!freeKickManager.isWaitingForKickerSelection, $"Header at Goal branch '{branch.Name}' should not wait for corner kicker selection while Final Thirds are active.");
                yield return StartCoroutine(ForfeitActiveFinalThirds());
                AssertNoLiveActionAvailabilityDuringSetPieceRestart($"Header at Goal branch '{branch.Name}' after corner Final Thirds");
                yield return StartCoroutine(WaitForCondition(
                    () => freeKickManager.isActivated && freeKickManager.isCornerKick && freeKickManager.isWaitingForKickerSelection,
                    6f,
                    $"Header at Goal branch '{branch.Name}' should resolve to corner setup after Final Thirds."));
                AssertNoLiveActionAvailabilityDuringSetPieceRestart($"Header at Goal branch '{branch.Name}' corner setup");
                AssertHeaderAtGoalExpectedBallHex(branch);
                AssertHeaderAtGoalCornerLogged(branch);
                break;

            case HeaderAtGoalOutcomeExpectation.MovementPhase:
                yield return StartCoroutine(WaitForCondition(
                    () => movementPhaseManager.isActivated || finalThirdManager.isActivated,
                    6f,
                    $"Header at Goal branch '{branch.Name}' should resolve toward movement for Home."));
                if (branch.ExpectedBallHex.HasValue)
                {
                    AssertHeaderAtGoalExpectedBallHex(branch);
                }
                break;

            case HeaderAtGoalOutcomeExpectation.SaveAndHold:
                yield return StartCoroutine(WaitForCondition(
                    () => shotManager.isWaitingForSaveandHoldScenario,
                    6f,
                    $"Header at Goal branch '{branch.Name}' should resolve to Save and Hold for Away."));
                AssertHeaderAtGoalExpectedBallHex(branch);
                break;
        }
    }

    private void AssertHeaderAtGoalExpectedBallHex(HeaderAtGoalBranchDefinition branch)
    {
        if (!branch.ExpectedBallHex.HasValue)
        {
            return;
        }

        Vector2Int expectedCoordinates = branch.ExpectedBallHex.Value;
        HexCell expectedHex = hexgrid.GetHexCellAt(new Vector3Int(expectedCoordinates.x, 0, expectedCoordinates.y));
        HexCell ballHex = headerManager.ball.GetCurrentHex();
        AssertTrue(expectedHex != null, $"Header at Goal branch '{branch.Name}' expected ball hex should exist.", true, expectedCoordinates);
        AssertTrue(ballHex == expectedHex, $"Header at Goal branch '{branch.Name}' should place the ball on the expected hex.", expectedHex, ballHex);
    }

    private void AssertHeaderAtGoalCornerLogged(HeaderAtGoalBranchDefinition branch)
    {
        PlayerToken yaneva = RequirePlayerToken("Yaneva");
        MatchManager.TeamStats attackingTeamStats = MatchManager.Instance.gameData.stats.GetTeamStats(yaneva.isHomeTeam);
        MatchManager.TeamStats defendingTeamStats = MatchManager.Instance.gameData.stats.GetTeamStats(!yaneva.isHomeTeam);

        AssertTrue(
            attackingTeamStats.totalCorners == 1,
            $"Header at Goal branch '{branch.Name}' should log exactly one corner for the attacking team.",
            1,
            attackingTeamStats.totalCorners);
        AssertTrue(
            defendingTeamStats.totalCorners == 0,
            $"Header at Goal branch '{branch.Name}' should not log a corner for the defending team.",
            0,
            defendingTeamStats.totalCorners);
    }

    private void AssertHeaderAtGoalResetAfterRightSideGoal(HeaderAtGoalBranchDefinition branch)
    {
        PlayerToken yaneva = RequirePlayerToken("Yaneva");
        AssertTrue(
            MatchManager.Instance.LastTokenToTouchTheBallOnPurpose == yaneva,
            $"Header at Goal branch '{branch.Name}' should credit the loose-ball goal to Yaneva.",
            yaneva,
            MatchManager.Instance.LastTokenToTouchTheBallOnPurpose);
        AssertTrue(
            goalFlowManager.LastCompletedGoalSide == 1,
            $"Header at Goal branch '{branch.Name}' should resolve as a right-side goal.",
            1,
            goalFlowManager.LastCompletedGoalSide);

        PlayerToken kuzmic = RequirePlayerToken("Kuzmic");
        HexCell yanevaHex = yaneva.GetCurrentHex();
        HexCell kuzmicHex = kuzmic.GetCurrentHex();

        AssertTrue(
            yanevaHex != null && yanevaHex.coordinates.x < 0,
            $"Header at Goal branch '{branch.Name}' should reset Home attackers to the left half after scoring at the right goal.",
            "x < 0",
            yanevaHex != null ? yanevaHex.coordinates : "null");
        AssertTrue(
            kuzmicHex != null && kuzmicHex.coordinates.x > 0,
            $"Header at Goal branch '{branch.Name}' should reset Away defenders to the right half after conceding at the right goal.",
            "x > 0",
            kuzmicHex != null ? kuzmicHex.coordinates : "null");
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

    private int CaptureScenarioTackleFoulThreshold()
    {
        var match = MatchManager.Instance;
        var attacker = movementPhaseManager.ball.GetCurrentHex()?.GetOccupyingToken();
        var defender = movementPhaseManager.selectedDefender;
        if (attacker == null || defender == null) return 1;
        var direction = attacker.isHomeTeam ? match.homeTeamDirection : match.awayTeamDirection;
        return attacker.GetCurrentHex().IsDangerousTacklingPosition(direction, defender.GetCurrentHex()) ? 2 : 1;
    }

    private static (float xDribbles, float xTackles) CalculateExpectedGroundDuel(
        int attackerDribbling,
        int defenderTackling,
        int defenderBonusMalus = 0,
        int defenderFoulThreshold = 1)
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
        float fouls = 0f;

        foreach ((int defenderRoll, float defenderProbability) in outcomes)
        {
            foreach ((int attackerRoll, float attackerProbability) in outcomes)
            {
                float branchProbability = defenderProbability * attackerProbability;

                if (defenderRoll <= defenderFoulThreshold)
                {
                    fouls += branchProbability;
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

        return (attackerWins + (ties / 6f) + fouls, defenderWins + ((ties * 5f) / 6f));
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
        string shotInstructions = shotManager.GetInstructions();
        if (!shotInstructions.Contains("Press [S] to take a Snapshot")) failures.Add($"ShotManager instruction should offer a Snapshot, but was '{shotInstructions}'");
        if (shotInstructions.Contains("take a Shot") || shotInstructions.Contains("to Shoot")) failures.Add($"ShotManager instruction should not label the Snapshot choice as a Shot, but was '{shotInstructions}'");

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
