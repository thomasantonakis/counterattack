using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("OnlineGroundPass")]
public sealed class OnlineGroundPassPlayModeTests
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
    [UnityTest] public IEnumerator RealGroundPassOutcome([Values(1, -1)] int side,
        [Values("safe", "intercept", "failed_interception")] string branch)
        => (IEnumerator)Bridge.GetMethod("RunOnlineGroundOutcome").Invoke(board, new object[] { side, branch });
    [UnityTest] public IEnumerator UnauthorizedAndStaleCommandsCannotMutate([Values(1, -1)] int side,
        [Values(false, true)] bool hostAway)
        => (IEnumerator)Bridge.GetMethod("RunOnlineGroundRejections").Invoke(board, new object[] { side, hostAway });
    [UnityTest] public IEnumerator ReconnectRetainsPreviewAndReceipt([Values(1, -1)] int side)
        => (IEnumerator)Bridge.GetMethod("RunOnlineGroundReconnect").Invoke(board, new object[] { side });
    [UnityTest] public IEnumerator PauseRevokesOldConfirmation([Values(1, -1)] int side)
        => (IEnumerator)Bridge.GetMethod("RunOnlineGroundPause").Invoke(board, new object[] { side });
}
