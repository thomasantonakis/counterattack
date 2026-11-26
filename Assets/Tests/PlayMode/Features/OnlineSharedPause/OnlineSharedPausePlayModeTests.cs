using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("OnlineSharedPause")]
public sealed class OnlineSharedPausePlayModeTests
{
    private object fixture;
    private static Type Bridge => AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Assembly-CSharp-Editor")
        .GetType("GoalkeeperBoxEntryFixture", true);
    [SetUp] public void SetUp() => fixture = Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator TearDown()
    { if (fixture != null) yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture, null); fixture = null; }
    [UnityTest] public IEnumerator RequesterOwnsPauseAcrossReconnect([Values(1,-1)] int side,
        [Values(false,true)] bool requesterHome, [Values(false,true)] bool hostAway)
        => (IEnumerator)Bridge.GetMethod("RunSharedPauseOwnership").Invoke(fixture, new object[] { side, requesterHome, hostAway });
    [UnityTest] public IEnumerator InFlightBallFreezesAndResumesOnce([Values(1,-1)] int side)
        => (IEnumerator)Bridge.GetMethod("RunSharedPauseMovingBall").Invoke(fixture, new object[] { side });
    [UnityTest] public IEnumerator PausedInterceptionCannotDrawOrReplayARoll([Values(1,-1)] int side)
        => (IEnumerator)Bridge.GetMethod("RunSharedPausePendingRoll").Invoke(fixture, new object[] { side });
}

[Category("OnlineSharedPause")]
public sealed class OnlineSharedPausePresentationPlayModeTests
{
    [UnityTest] public IEnumerator AuthoredRequesterAndPassivePauseViewsAreIndependentOfStats()
    {
        var bridge = AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Assembly-CSharp-Editor")
            .GetType("OnlineSharedPauseSceneFixture", true);
        return (IEnumerator)bridge.GetMethod("Run").Invoke(Activator.CreateInstance(bridge), null);
    }
}
