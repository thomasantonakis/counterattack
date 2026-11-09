using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("DecisionTimingLiveClock")]
public sealed class DecisionTimingLiveClockPlayModeTests
{
    private object board;
    private static Type Bridge => AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Assembly-CSharp-Editor").GetType("GoalkeeperBoxEntryFixture", true);
    [SetUp] public void SetUp() => board = Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator Cleanup() => (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(board, null);
    [UnityTest] public IEnumerator SharedPreview([Values(1,2)] int difficulty, [Values(false,true)] bool expired)
        => (IEnumerator)Bridge.GetMethod("RunLiveTimingPreview").Invoke(board, new object[] {difficulty,expired});
    [UnityTest] public IEnumerator CommittedMovement([Values(1,2,3)] int difficulty, [Values(false,true)] bool expired)
        => (IEnumerator)Bridge.GetMethod("RunLiveTimingMovement").Invoke(board, new object[] {difficulty,expired});
    [UnityTest] public IEnumerator AccuracyReview([Values(1,-1)] int side, [Values(false,true)] bool automatic)
        => (IEnumerator)Bridge.GetMethod("RunLiveTimingAccuracy").Invoke(board, new object[] {side,automatic});
    [UnityTest] public IEnumerator SnapshotUsesPhase([Values(1,-1)] int side)
        => (IEnumerator)Bridge.GetMethod("RunLiveTimingSnapshot").Invoke(board, new object[] {side,false});
    [UnityTest] public IEnumerator SnapshotDeclarationPanel([Values(1,-1)] int side)
        => (IEnumerator)Bridge.GetMethod("RunLiveTimingSnapshot").Invoke(board, new object[] {side,true});
    [UnityTest] public IEnumerator DifficultyThreeCommit([Values("P","C","L","F","S")] string action)
        => (IEnumerator)Bridge.GetMethod("RunLiveTimingDifficultyThree").Invoke(board, new object[] {action});
}
