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
            Scenario_001_GroundBall_0001_TooFarAway(),
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
        gameInputManager = FindObjectOfType<GameInputManager>();
        groundBallManager = FindObjectOfType<GroundBallManager>();
        if (gameInputManager == null || groundBallManager == null)
        {
            Debug.LogError("❌ Could not link scene components correctly.");
            testFailed = true;
            return;
        }
        // Debug.Log("✅ Room scene components successfully linked.");
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

        // ✅ STEP 2: Press Space to start match
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Space, 0.05f));
        AssertTrue(
            MatchManager.Instance.currentState == MatchManager.GameState.KickoffBlown
            , "KickOff state check after pressing Space"
            , MatchManager.GameState.KickoffBlown
            , MatchManager.Instance.currentState
        );

        // ✅ STEP 3: Press C (custom logic assumed)
        // yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.C, 0.2f));
        // AssertTrue(MatchManager.Instance != null, "MatchManager still active after pressing C");

        // // ✅ STEP 4: Click (10,6)
        // yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 6), 0.2f));
        // // Check some meaningful state change here
        // // AssertTrue(MatchManager.Instance.currentState != MatchManager.GameState.KickOffSetup, "Game state updated after first click");

        // // ✅ STEP 5: Click (10,6) again
        // yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 6), 0.2f));
        // // Add another relevant check

        // // ✅ STEP 6: Click (12,7)
        // yield return StartCoroutine(gameInputManager.DelayedClick(new Vector2Int(10, 6), 0.2f));
        // // Final state check
        LogFooterofTest("KICK OFF");
    }
    
    private IEnumerator Scenario_001_GroundBall_0001_TooFarAway()
    {
        yield return new WaitForSeconds(4f); // Allow scene to stabilize

        Log("▶️ Starting test scenario: 'Ground Ball - Too Far Away'");

        // ✅ STEP 1: Press 2
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Alpha2, 0f));
        AssertTrue(
            MatchManager.Instance.currentState == MatchManager.GameState.KickOffSetup
            , "KickOff state check after pressing 2"
            , MatchManager.GameState.KickOffSetup
            , MatchManager.Instance.currentState
        );

        // ✅ STEP 2: Press Space to start match
        yield return StartCoroutine(gameInputManager.DelayedKeyDataPress(KeyCode.Space, 0.05f));
        AssertTrue(
            MatchManager.Instance.currentState == MatchManager.GameState.KickOffSetup
            , "KickOff state check after pressing Space"
            , MatchManager.GameState.KickOffSetup
            , MatchManager.Instance.currentState
        );
        AssertTrue(
            groundBallManager.isAvailable
            , "GroundBallManager is available after pressing Space"
            , true
            , groundBallManager.isAvailable
        );
        LogFooterofTest("test");
        
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
            Log($"✅ {message} - TEST PASSED COMPLETELY 🎉\n\n");
        }
        else
        {
            Log($"❌ {message} - TEST FALED SOMEWHERE\n\n");
        }
    }

}