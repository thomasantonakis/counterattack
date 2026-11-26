using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("OnlineDraftDeparture")]
public sealed class OnlineDraftDeparturePlayModeTests
{
    private object fixture;

    [UnityTest]
    public IEnumerator HostCanSaveAfterGuestLeaves() =>
        Run("RunCheckpointAtNewBoundary", true, "guest-left");

    [UnityTest]
    public IEnumerator GuestSeesPausedRoomAfterHostLeaves() => Run("RunDepartedGuestRoom");

    private IEnumerator Run(string method, params object[] arguments)
    {
        var bridge = AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Assembly-CSharp-Editor")
            .GetType("OnlineFullRoomSceneFixture", true);
        fixture = Activator.CreateInstance(bridge);
        return (IEnumerator)bridge.GetMethod(method).Invoke(fixture, arguments);
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        if (fixture != null) yield return (IEnumerator)fixture.GetType().GetMethod("Cleanup").Invoke(fixture, null);
        fixture = null;
    }
}
