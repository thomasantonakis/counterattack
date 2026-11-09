using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("DecisionTimingReportedIssues")]
public sealed class DecisionTimingReportedIssuesPlayModeTests
{
    private object board;
    private static Type Bridge => AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Assembly-CSharp-Editor").GetType("GoalkeeperBoxEntryFixture", true);
    [SetUp] public void Setup() => board = Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator Cleanup() => (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(board, null);
    [UnityTest] public IEnumerator SnapshotConsumesOneMove([Values(1,-1)] int side, [Values(false,true)] bool exhausted, [Values(false,true)] bool shoot)
        => (IEnumerator)Bridge.GetMethod("RunSnapshotMovementAccounting").Invoke(board, new object[] {side,exhausted,shoot});
    [UnityTest] public IEnumerator FinalThirdMotion([Values(1,-1)] int side, [Values(false,true)] bool expired, [Values(false,true)] bool defense)
        => (IEnumerator)Bridge.GetMethod("RunFinalThirdMotionTiming").Invoke(board, new object[] {side,expired,defense});
    [UnityTest] public IEnumerator TacklePanel([Values(1,-1)] int side, [Values(false,true)] bool accept)
        => (IEnumerator)Bridge.GetMethod("RunTacklePendingMovementPanel").Invoke(board, new object[] {side,accept});
    [UnityTest] public IEnumerator PenaltyKeeperImmediately([Values(1,-1)] int side, [Values(false,true)] bool shootoutMode)
        => (IEnumerator)Bridge.GetMethod("RunImmediatePenaltyKeeper").Invoke(board, new object[] {side,shootoutMode});
}
