using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

public sealed class GoalkeeperPickupPlayModeTests
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
    public IEnumerator HeaderFeetDefersFinalThirds([Values(1,-1)] int side)
        => (IEnumerator)Bridge.GetMethod("RunPickupReproduction").Invoke(board, new object[] {side});
    [UnityTest]
    public IEnumerator DeferredPickup([Values(1,-1)] int side,
        [Values("direct", "corner", "outside", "nearby")] string arrival,
        [Values("Q", "K", "feet", "reset")] string choice)
        => (IEnumerator)Bridge.GetMethod("RunDeferredPickup").Invoke(board, new object[] {side,arrival,choice});
    [UnityTest]
    [Category("GoalkeeperPickupFollowup")]
    public IEnumerator LostHeaderAtGoalReleasesTargetBeforeContinuation()
        => (IEnumerator)Bridge.GetMethod("RunLostHeaderTargetCleanup").Invoke(board, null);
    [UnityTest]
    [Category("GoalkeeperPickupFollowup")]
    public IEnumerator OutsideEndDefersUntilLaterPhase([Values(1,-1)] int side, [Values(false,true)] bool opponent)
        => (IEnumerator)Bridge.GetMethod("RunOutsidePhaseDeferral").Invoke(board, new object[] {side,opponent});
    [UnityTest]
    [Category("GoalkeeperPickupFollowup")]
    public IEnumerator DirectHeaderCatchShowsPercentage([Values(1,-1)] int side)
        => (IEnumerator)Bridge.GetMethod("RunDirectHeaderCatchPercentage").Invoke(board, new object[] {side});
}
