using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

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
    public bool hpmisWaitingForAccuracyRoll;
    public bool hpmIsWaitingForDirectionRoll;
    public bool hpmIsWaitingForDistanceRoll;
    public bool lbmAvailable;
    public bool lbmIsActivated;
    public bool lbmIsAwaitingTargetSelection;
    public bool lbmIsWaiitngForAccuracyRoll;
    public bool lbmIsWaitingForDirectionRoll;
    public bool lbmIsWaitingForDistanceRoll;
    public bool lbmIsWaitingForDefLBMove;
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
        hpmisWaitingForAccuracyRoll = hpm.isWaitingForAccuracyRoll;
        hpmIsWaitingForDirectionRoll = hpm.isWaitingForDirectionRoll;
        hpmIsWaitingForDistanceRoll = hpm.isWaitingForDistanceRoll;
        lbmAvailable = lbm.isAvailable;
        lbmIsActivated = lbm.isActivated;
        lbmIsWaiitngForAccuracyRoll = lbm.isWaitingForAccuracyRoll;
        lbmIsWaitingForDirectionRoll = lbm.isWaitingForDirectionRoll;
        lbmIsWaitingForDistanceRoll = lbm.isWaitingForDistanceRoll;
        lbmIsAwaitingTargetSelection = lbm.isAwaitingTargetSelection;
        lbmCurrentTargetHex = lbm.currentTargetHex;
        lbmIsWaitingForDefLBMove = lbm.isWaitingForDefLBMove;
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

        if (excludeFields?.Contains("hpmisWaitingForAccuracyRoll") != true && hpmisWaitingForAccuracyRoll != other.hpmisWaitingForAccuracyRoll)
        {
            mismatches.Add($"HighPassManager.isWaitingForAccuracyRoll mismatch: {hpmisWaitingForAccuracyRoll} vs {other.hpmisWaitingForAccuracyRoll}");
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
        
        if (excludeFields?.Contains("lbmIsWaiitngForAccuracyRoll") != true && lbmIsWaiitngForAccuracyRoll != other.lbmIsWaiitngForAccuracyRoll)
        {
            mismatches.Add($"LongBallManager.isWaitingForAccuracyRoll mismatch: {lbmIsWaiitngForAccuracyRoll} vs {other.lbmIsWaiitngForAccuracyRoll}");
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
        var scenarios = new List<IEnumerator>
        {
            Scenario_001_BasicKickoff(),
            Scenario_002_GroundBall_0001_Commitment(),
            Scenario_003_GroundBall_0002_Dangerous_pass_no_interception(),
            Scenario_004_GroundBall_0003_Dangerous_pass_intercepted_by_second_interceptor(),
            Scenario_005_GroundBall_0004_Pass_to_Player_FTP_No_interceptions(),
            Scenario_006_GroundBall_0005_Pass_to_Player_FTP_To_Player(),
            Scenario_007_GroundBall_0006_Swith_between_options_before_Committing(),
            Scenario_008_Stupid_Click_and_KeyPress_do_not_change_status(),
            Scenario_009_Movement_Phase_NO_interceptions_No_tackles(),
            Scenario_010_Movement_Phase_failed_interceptions_No_tackles(),
            Scenario_011_Movement_Phase_Successful_Interception(),
            Scenario_012_Movement_Phase_interception_Foul_take_foul(),
            Scenario_013_Movement_Phase_interception_Foul_Play_on(),
            Scenario_014_Movement_Phase_Check_reposition_interceptions(),
            Scenario_015_Movement_Phase_Check_NutmegWithoutMovement_tackle_Loose_Ball(),
            Scenario_016_Movement_Phase_Check_InterceptionFoul_Tackle_Foul_NewTackle_SuccessfulTackle(),
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
        }
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

        LogFooterofTest("MovementPhase With Successful Interception");
    }

    private IEnumerator Scenario_012_Movement_Phase_interception_Foul_take_foul()
    {        
        yield return new WaitForSeconds(1.5f); // Allow scene to stabilize
        Log("▶️ Starting test scenario: MovementPhase With Foul Taken on Interception");
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
        Log("Pressing X for Leniency Test");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.R, 0.1f));
        AssertTrue(
            !movementPhaseManager.isWaitingForFoulDecision,
            "MP Should NOT be waiting for a foul decision after Leniency Roll",
            true,
            movementPhaseManager.isWaitingForFoulDecision
        );
        Log("Pressing X for Resilience Test");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.R, 0.1f));
        yield return new WaitForSeconds(0.8f);
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

        LogFooterofTest("MovementPhase With Foul Taken on Interception");
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
        Log("Pressing X for Leniency Test");
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.R, 0.1f));
        AssertTrue(
            !movementPhaseManager.isWaitingForFoulDecision,
            "MP Should NOT be waiting for a foul decision after Leniency Roll",
            true,
            movementPhaseManager.isWaitingForFoulDecision
        );
        Log("Pressing X for Resilience Test");
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
            "MovementPhase Should NOT be waiting for Tackle Decision without moving before moving Paterson",
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
        yield return new WaitForSeconds(1.2f); // for the token to move
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

        LogFooterofTest("MovementPhase Check InterceptionFoul Tackle Foul NewTackle SuccessfulTackle");
    }

    // TODO: Movement Phase

    
    // TODO: Pass to player
    //    , move with dribbler next to defender (no nutmeg, no interception)
    //    , forfeit rest of movement
    //    , start MP with nutmeg without moving
    //    , reposition attacker
    //    , forfeit Attacking MP
    //    , bring defender for tackle
    //    , reposition attacker next to nutmegged (verify no interception)
    //    , bring third attacker for tackle
    //    , force loose ball to space
    //    , pick up with fourth defender verify end of movement
    // TODO: Pass to player
    //    , move dribler for nutmeg
    //    , reposition and continue moving
    //    , forfeit MPAtt
    //    , verify the defender cannot move
    //    , end MP
    // TODO: Pass to player
    //    , move dribbler for nutmeg, fail nutmeg, reposition defender, check Availability
    // TODO: Pass to player
    //    , move dribbler for nutmeg, loose ball to space with no interceptions.
    // TODO: Pass to player
    //    , move dribbler for nutmeg, loose ball to space with failed interceptions.
    // TODO: Pass to next to defenders, fail interceptions
    //    , move attacker to pickup ball and nutmeg.

    // TODO: Final Thirds
    // TODO: High Pass
    // TODO: Long Ball


    
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