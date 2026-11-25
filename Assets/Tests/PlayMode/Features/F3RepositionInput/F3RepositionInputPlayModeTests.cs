using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("F3RepositionInput")]
public sealed class F3RepositionInputPlayModeTests
{
    private object fixture;
    private static Type Bridge => AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Assembly-CSharp-Editor").GetType("GoalkeeperBoxEntryFixture", true);
    [SetUp] public void SetUp() => fixture = Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator TearDown() => (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture, null);
    [UnityTest] public IEnumerator RecoveryReservesActionsUntilFinalThirdsFinish([Values(1, -1)] int side, [Values(true, false)] bool homeRecovers)
        => (IEnumerator)Bridge.GetMethod("RunRecoveryF3Ownership").Invoke(fixture, new object[] { side, homeRecovers });
    [UnityTest] public IEnumerator StalePassPreviewCannotConsumeFinalThirdClicksOrHover([Values(1, -1)] int side)
        => (IEnumerator)Bridge.GetMethod("RunStalePassF3Ownership").Invoke(fixture, new object[] { side });
    [UnityTest] public IEnumerator NoFinalThirdLeavesRecoveryActionsAvailable([Values(1, -1)] int side, [Values(true, false)] bool emptyOppositeThird)
        => (IEnumerator)Bridge.GetMethod("RunRecoveryWithoutFinalThird").Invoke(fixture, new object[] { side, emptyOppositeThird });
    [UnityTest] public IEnumerator RepositionTravelAndNestedKeeperDecisionKeepDestinationsCleared([Values(1, -1)] int side)
        => (IEnumerator)Bridge.GetMethod("RunRepositionHoverOwnership").Invoke(fixture, new object[] { side });
}
