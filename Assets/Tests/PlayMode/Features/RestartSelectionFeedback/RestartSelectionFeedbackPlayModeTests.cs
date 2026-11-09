using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("RestartSelectionFeedback")]
public sealed class RestartSelectionFeedbackPlayModeTests
{
    private object board;
    private static Type Bridge => AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Assembly-CSharp-Editor").GetType("GoalkeeperBoxEntryFixture", true);
    [SetUp] public void Setup() => board = Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator Cleanup() => (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(board, null);
    [UnityTest] public IEnumerator ThrowSelectionTiming([Values(1,2,3)] int difficulty, [Values(false,true)] bool head)
        => (IEnumerator)Bridge.GetMethod("RunThrowSelectionTimingAudit").Invoke(board, new object[] {difficulty,head});
    [UnityTest] public IEnumerator PostHeaderMovementTiming([Values(1,2,3)] int difficulty, [Values(false,true)] bool possession)
        => (IEnumerator)Bridge.GetMethod("RunPostHeaderMovementTimingAudit").Invoke(board, new object[] {difficulty,possession});
    [UnityTest] public IEnumerator CornerTargets([Values(1,-1)] int side, [Values(1,2,3)] int difficulty)
        => (IEnumerator)Bridge.GetMethod("RunCornerTargetFeedback").Invoke(board, new object[] {side,difficulty});
    [UnityTest] public IEnumerator FinalThirdHover([Values(1,-1)] int side, [Values(1,2,3)] int difficulty)
        => (IEnumerator)Bridge.GetMethod("RunFinalThirdHoverFeedback").Invoke(board, new object[] {side,difficulty});
}
