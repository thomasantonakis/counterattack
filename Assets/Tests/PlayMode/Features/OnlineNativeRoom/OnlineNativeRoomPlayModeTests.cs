using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("OnlineNativeRoom")]
public sealed class OnlineNativeRoomPlayModeTests
{
    private object fixture;
    private static Type Bridge => AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Assembly-CSharp-Editor")
        .GetType("OnlineFullRoomSceneFixture", true);

    [SetUp]
    public void Setup() => fixture = Activator.CreateInstance(Bridge);

    [UnityTest]
    public IEnumerator BothRolesStartInAuthoredRoom([Values(false, true)] bool guest)
        => (IEnumerator)Bridge.GetMethod("RunNativeRoom").Invoke(fixture, new object[] { guest });

    [UnityTest]
    public IEnumerator OfflineModesKeepTheirAuthoredRoom([Values("Hot Seat", "Single Player")] string gameMode)
        => (IEnumerator)Bridge.GetMethod("RunOfflineNativeRoom").Invoke(fixture, new object[] { gameMode });

    [UnityTest]
    public IEnumerator HostAndGuestUseTheAuthoredPanels([Values("movement", "duel", "header", "stoppage")] string presentation)
        => (IEnumerator)Bridge.GetMethod("RunNativeProjection").Invoke(fixture, new object[] { presentation });

    [UnityTest]
    public IEnumerator HostResetsToSecondHalfAfterTheWhistle()
        => (IEnumerator)Bridge.GetMethod("RunNativeProjection").Invoke(fixture, new object[] { "half-time" });

    [UnityTest]
    public IEnumerator TeamColorsMatchCommandOwnershipAndLocalUiStaysLocal()
        => (IEnumerator)Bridge.GetMethod("RunNativeInputOwnership").Invoke(fixture, null);

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        if (fixture != null) yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture, null);
        fixture = null;
    }
}
