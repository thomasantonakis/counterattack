using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;
using UnityEngine.Assertions;

public struct AvailabilityCheckResult
{
    public bool passed;
    public List<string> failures;

    public AvailabilityCheckResult(bool passed, List<string> failures)
    {
        this.passed = passed;
        this.failures = failures;
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
    }

    private void Start()
    {
        StartTesting();
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

    private IEnumerator RunAllScenarios()
    {
        var scenarios = new List<IEnumerator>
        {
            Scenario_01_BasicKickoff(),
            Scenario_001_GroundBall_0001_Commitment(),
            Scenario_001_GroundBall_0002_Dangerous_pass_no_interception(),
            Scenario_001_GroundBall_0003_Dangerous_pass_intercepted_by_second_interceptor(),
            Scenario_001_GroundBall_0004_Pass_to_Player_FTP_No_interceptions(),
            Scenario_001_GroundBall_0005_Pass_to_Player_FTP_To_Player(),
            Scenario_002_GroundBall_0006_Swith_between_options_before_Committing(),
            // Add more scenarios here
        };

        for (; currentTestIndex < scenarios.Count; currentTestIndex++)
        {
            testFailed = false;
            Log($"\n==== Starting Test #{currentTestIndex + 1} ====");
            // 🔁 Scene switch to Dummy first (full teardown)
            yield return new WaitForSeconds(1f);

            // 🔄 Load Room scene fresh
            SceneManager.LoadScene("Room");

            yield return new WaitForSeconds(1f); // Optional buffer
            LinkRoomSceneComponents();
            yield return StartCoroutine(scenarios[currentTestIndex]);

            if (testFailed)
            {
                Log("❌ Test failed. Halting suite.");
                yield break; // Stop entire suite on failure
            }

            Log($"✅ Test #{currentTestIndex + 1} passed.\n");
            yield return new WaitForSeconds(0.5f); // Short pause between tests
            SceneManager.LoadScene("DummyLoader");
        }
        Log("🎉 ALL TESTS PASSED SUCCESSFULLY!");
    }

    private void LinkRoomSceneComponents()
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
        }
    }

    private IEnumerator Scenario_01_BasicKickoff()
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
    
    private IEnumerator Scenario_001_GroundBall_0001_Commitment()
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

    private IEnumerator Scenario_001_GroundBall_0002_Dangerous_pass_no_interception()
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
    
    private IEnumerator Scenario_001_GroundBall_0003_Dangerous_pass_intercepted_by_second_interceptor()
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
        var interceptor = PlayerToken.GetPlayerTokenByName("Gilbert");
        AssertTrue(
            MatchManager.Instance.LastTokenToTouchTheBallOnPurpose == interceptor,
            "Gilbert should be the last to touch the ball",
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
            MatchManager.Instance.gameData.stats.GetPlayerStats("Vladoiu").interceptionsAttempted == 1,
            "Vladoiu Should have 1 interception attempted",
            1,
            MatchManager.Instance.gameData.stats.GetPlayerStats("Vladoiu").interceptionsAttempted
        );
        AssertTrue(
            MatchManager.Instance.gameData.stats.GetPlayerStats("Gilbert").interceptionsAttempted == 1,
            "Gilbert Should have 1 interception attempted",
            1,
            MatchManager.Instance.gameData.stats.GetPlayerStats("Gilbert").interceptionsAttempted
        );
        AssertTrue(
            MatchManager.Instance.gameData.stats.GetPlayerStats("Vladoiu").interceptionsMade == 0,
            "Vladoiu Should have 0 interceptions made",
            1,
            MatchManager.Instance.gameData.stats.GetPlayerStats("Vladoiu").interceptionsMade
        );
        AssertTrue(
            MatchManager.Instance.gameData.stats.GetPlayerStats("Gilbert").interceptionsMade == 1,
            "Gilbert Should have 1 interception made",
            1,
            MatchManager.Instance.gameData.stats.GetPlayerStats("Gilbert").interceptionsMade
        );

        LogFooterofTest("Ground Ball - Dangerous Pass - No Interception");
    }

    private IEnumerator Scenario_001_GroundBall_0004_Pass_to_Player_FTP_No_interceptions()
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
    
    private IEnumerator Scenario_001_GroundBall_0005_Pass_to_Player_FTP_To_Player()
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
    
    private IEnumerator Scenario_002_GroundBall_0006_Swith_between_options_before_Committing()
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

    private void AssertTrue(bool condition, string message, object expected = null, object actual = null)
    {
        if (!condition)
        {
            string failMessage = $"❌ ASSERT FAILED: {message}";
            if (expected != null || actual != null) {failMessage += $" | Expected: {expected} | Actual: {actual}";}
            Debug.LogError(failMessage);
            File.AppendAllText(logFilePath, failMessage + "\n");
            testFailed = true;
            StopAllCoroutines(); // freeze test
        }
        else
        {
            string passMessage = $"✅ PASS: {message}";
            if (expected != null || actual != null) {passMessage += $" | Expected: {expected} | Actual: {actual}";}

            Debug.Log(passMessage);
            File.AppendAllText(logFilePath, passMessage + "\n");
        }
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
        if (!movementPhaseManager.isAvailable) failures.Add("MovementPhase should be available");
        if (highPassManager.isAvailable) failures.Add("HighPass should NOT be available");
        if (longBallManager.isAvailable) failures.Add("LongBall should NOT be available");
        if (groundBallManager.isAvailable) failures.Add("GroundBall should NOT be available");

        if (movementPhaseManager.isActivated) failures.Add("MovementPhase should not be activated");
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

        if (firstTimePassManager.isAvailable) failures.Add("FirstTimePass should NOT be available");
        if (!movementPhaseManager.isAvailable) failures.Add("MovementPhase should be available");
        if (highPassManager.isAvailable) failures.Add("HighPass should NOT be available");
        if (longBallManager.isAvailable) failures.Add("LongBall should NOT be available");
        if (groundBallManager.isAvailable) failures.Add("GroundBall should NOT be available");

        if (movementPhaseManager.isActivated) failures.Add("MovementPhase should not be activated");
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

        if (!movementPhaseManager.isAvailable) failures.Add("MovementPhase should be available");
        if (firstTimePassManager.isAvailable) failures.Add("FirstTimePass should not be available");
        if (highPassManager.isAvailable) failures.Add("HighPass should NOT be available");
        if (longBallManager.isAvailable) failures.Add("LongBall should NOT be available");
        if (groundBallManager.isAvailable) failures.Add("GroundBall should NOT be available");

        if (movementPhaseManager.isActivated) failures.Add("MovementPhase should not be activated");
        if (firstTimePassManager.isActivated) failures.Add("FirstTimePass should NOT be activated");
        if (groundBallManager.isActivated) failures.Add("GroundBall should not be activated");
        if (highPassManager.isActivated) failures.Add("HighPass should not be activated");
        if (longBallManager.isActivated) failures.Add("LongBall should not be activated");
        if (MatchManager.Instance.attackHasPossession) failures.Add("Attack has possession after ball movement");

        return new AvailabilityCheckResult(failures.Count == 0, failures);
    }
    
    private void Log(string message)
    {
        Debug.Log(message);
        File.AppendAllText(logFilePath, message + "\n");
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