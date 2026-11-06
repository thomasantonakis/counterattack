using System;
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

// SessionState survives the domain reload on entry to framework PlayMode tests.
// Register globally so running tests from the Test Runner UI also suppresses the legacy runner.
[InitializeOnLoad]
public sealed class FrameworkTestExecutionGate : ICallbacks
{
    private static readonly TestRunnerApi Api;
    static FrameworkTestExecutionGate()
    {
        Api = ScriptableObject.CreateInstance<TestRunnerApi>();
        Api.RegisterCallbacks(new FrameworkTestExecutionGate());
    }
    private const string DirectoryKey = "CounterAttack.FrameworkResultDirectory";
    public void RunStarted(ITestAdaptor testsToRun)
    {
        SessionState.SetBool("CounterAttack.FrameworkTests", true);
        string directory = Path.Combine("TestResults", "GameplayRegression", "Runs", DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ"));
        Directory.CreateDirectory(directory);
        SessionState.SetString(DirectoryKey, directory);
    }
    public void RunFinished(ITestResultAdaptor result)
    {
        SessionState.SetBool("CounterAttack.FrameworkTests", false);
        string directory = SessionState.GetString(DirectoryKey, string.Empty);
        if (!string.IsNullOrEmpty(directory))
            TestRunnerApi.SaveResultToFile(result, Path.Combine(directory, "results.xml"));
    }
    public void TestStarted(ITestAdaptor test)
    {
        string directory = SessionState.GetString(DirectoryKey, string.Empty);
        if (!test.IsSuite && !string.IsNullOrEmpty(directory))
            File.WriteAllText(Path.Combine(directory, "current-test.txt"), test.FullName);
    }
    public void TestFinished(ITestResultAdaptor result)
    {
        string directory = SessionState.GetString(DirectoryKey, string.Empty);
        if (result.Test.IsSuite || string.IsNullOrEmpty(directory)) return;
        File.AppendAllText(Path.Combine(directory, "progress.jsonl"), JsonUtility.ToJson(new CaseResult
        {
            name = result.Test.FullName, status = result.TestStatus.ToString(), message = result.Message, stackTrace = result.StackTrace
        }) + "\n");
    }
    [Serializable] private sealed class CaseResult { public string name; public string status; public string message; public string stackTrace; }
}
