using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;
using UnityEngine.Assertions;

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
        yield return new WaitForSeconds(4f); // Allow scene to stabilize

        Log("▶️ Starting test scenario: 'Ground Ball - Commitment'");

        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Alpha2, 0f));
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
        AssertTrue(
            !groundBallManager.isActivated
            , "GBM is deactivated after ball movement"
            , false
            , groundBallManager.isActivated
        );

        LogFooterofTest("Ground Ball - Invalid, Switch and Commitment to no interceptions");
        
    }

    private IEnumerator Scenario_001_GroundBall_0002_Dangerous_pass_no_interception()
    {
        yield return new WaitForSeconds(4f); // Allow scene to stabilize

        Log("▶️ Starting test scenario: 'Ground Ball - Dangerous Pass - No Interception'");

        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Alpha2, 0.05f));
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Space, 0.05f));
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.P, 0.05f));

        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(0, 6), 0.2f));
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

        yield return new WaitForSeconds(0.5f);
        groundBallManager.PerformGroundInterceptionDiceRoll(1);
        AssertTrue(
            groundBallManager.defendingHexes.Count == 2
            , "GBM is now waiting for 2 dice rolls"
            , 2
            , groundBallManager.defendingHexes.Count
        );
        yield return new WaitForSeconds(0.5f);
        groundBallManager.PerformGroundInterceptionDiceRoll(1);
        AssertTrue(
            groundBallManager.defendingHexes.Count == 1
            , "GBM is now waiting for 1 dice rolls"
            , 1
            , groundBallManager.defendingHexes.Count
        );
        yield return new WaitForSeconds(0.5f);
        groundBallManager.PerformGroundInterceptionDiceRoll(1);
        AssertTrue(
            groundBallManager.defendingHexes.Count == 0
            , "GBM is cleaned up"
            , 0
            , groundBallManager.defendingHexes.Count
        );
        yield return new WaitForSeconds(2f);
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
        var passer = PlayerToken.GetPlayerTokenByName("Cafferata");
        AssertTrue(
            MatchManager.Instance.LastTokenToTouchTheBallOnPurpose == passer,
            "Cafferata should be the last to touch the ball",
            passer,
            MatchManager.Instance.LastTokenToTouchTheBallOnPurpose
        );
        AssertTrue(
            MatchManager.Instance.currentState == MatchManager.GameState.StandardPassCompletedToSpace,
            "MatchManager is in StandardPassCompletedToSpace state",
            MatchManager.GameState.StandardPassCompletedToSpace,
            MatchManager.Instance.currentState
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
        yield return new WaitForSeconds(4f); // Allow scene to stabilize

        Log("▶️ Starting test scenario: 'Ground Ball - Dangerous Pass - No Interception'");

        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Alpha2, 0.05f));
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Space, 0.05f));
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.P, 0.05f));
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(3, 2), 0.2f));
        yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(3, 2), 0.2f));
        yield return new WaitForSeconds(0.5f);
        groundBallManager.PerformGroundInterceptionDiceRoll(1);
        yield return new WaitForSeconds(0.5f);
        groundBallManager.PerformGroundInterceptionDiceRoll(6);
        yield return new WaitForSeconds(2f);
        AssertTrue(
            groundBallManager.isActivated == false
            , "GBM is deactivated after ball movement"
            , false
            , groundBallManager.isActivated
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
            Log($"\n❌ {message} - TEST FALED SOMEWHERE\n");
        }
    }

}