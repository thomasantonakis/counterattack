using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

public sealed class LongBallGoalkeeperPacePlayModeTests
{
    private object board;
    private static Type Bridge => AppDomain.CurrentDomain.GetAssemblies()
        .Single(a => a.GetName().Name == "Assembly-CSharp-Editor").GetType("GoalkeeperBoxEntryFixture", true);
    [SetUp] public void SetUp() => board = Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator TearDown()
    {
        if (board != null) yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(board, null);
        board = null;
    }
    [UnityTest]
    public IEnumerator Landing([Values(1,-1)] int side,
        [Values("direct","zoi","teammate","empty")] string recipient, [Values(false,true)] bool move)
        => (IEnumerator)Bridge.GetMethod("RunLongPaceLanding").Invoke(board, new object[] {side,recipient,move});
    [UnityTest]
    public IEnumerator BoxEntry([Values(1,-1)] int side, [Values(false,true)] bool entryMove,
        [Values(false,true)] bool paceMove)
        => (IEnumerator)Bridge.GetMethod("RunLongPaceBox").Invoke(board,new object[] {side,entryMove,paceMove});
    [UnityTest]
    public IEnumerator Recheck([Values(1,-1)] int side,
        [Values("claim_inside","claim_outside","leave_zoi","enter_zoi")] string branch)
        => (IEnumerator)Bridge.GetMethod("RunLongPaceRecheck").Invoke(board,new object[] {side,branch});
    [UnityTest]
    public IEnumerator Delivery([Values(1,-1)] int side,[Values(false,true)] bool inaccurate,
        [Values(false,true)] bool teammate)
        => (IEnumerator)Bridge.GetMethod("RunLongPaceDelivery").Invoke(board,new object[] {side,inaccurate,teammate});
    [UnityTest]
    public IEnumerator Boundary([Values(1,-1)] int side,
        [Values("zero","blocked","cancel","new_action","flight")] string branch)
        => (IEnumerator)Bridge.GetMethod("RunLongPaceBoundary").Invoke(board,new object[] {side,branch});

    [UnityTest]
    public IEnumerator Stoppage([Values(1,-1)] int side,
        [Values("oob","offside","half_gate","half","ended")] string branch)
        => (IEnumerator)Bridge.GetMethod("RunLongPaceStoppage").Invoke(board,new object[] {side,branch});

    [UnityTest]
    public IEnumerator RecoveryResultCanceled([Values(1,-1)] int side)
        => (IEnumerator)Bridge.GetMethod("RunLongPaceRollCancel").Invoke(board,new object[] {side});

}
