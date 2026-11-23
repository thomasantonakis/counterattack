using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("OnlineSeatOwnership")]
public sealed class OnlineSeatOwnershipPlayModeTests
{
    private object fixture;
    private static Type Bridge => AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Assembly-CSharp-Editor")
        .GetType("OnlineFullRoomSceneFixture", true);

    [SetUp]
    public void Setup() => fixture = Activator.CreateInstance(Bridge);

    [UnityTest]
    public IEnumerator NativeRoomEnforcesHostHomeAndGuestAway([Values(false, true)] bool homeKicksOff)
        => (IEnumerator)Bridge.GetMethod("RunNativeSeatOwnership").Invoke(fixture, new object[] { homeKicksOff });

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        if (fixture != null) yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture, null);
        fixture = null;
    }
}
