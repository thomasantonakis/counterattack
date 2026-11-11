using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("OnlineGroundPassNetwork")]
public sealed class OnlineGroundPassNetworkPlayModeTests
{
    private object fixture;
    private static Type Bridge => AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Assembly-CSharp-Editor")
        .GetType("OnlineGroundPassSceneFixture", true);
    [SetUp] public void SetUp() => fixture = Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator TearDown()
    { if (fixture != null) yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture, null); fixture = null; }
    [UnityTest] public IEnumerator AuthoredGuestHasNoRunningGameplay() => (IEnumerator)Bridge.GetMethod("Run").Invoke(fixture, new object[] { false });
    [UnityTest] public IEnumerator RealPassAcrossWireUpdatesFreshGuestWithoutReroll() => (IEnumerator)Bridge.GetMethod("Run").Invoke(fixture, new object[] { true });
}
