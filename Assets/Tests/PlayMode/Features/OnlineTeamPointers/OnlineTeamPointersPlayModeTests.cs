using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("OnlineTeamPointers")]
public sealed class OnlineTeamPointersPlayModeTests
{
    private object fixture;
    private static Type Bridge => AppDomain.CurrentDomain.GetAssemblies()
        .Single(assembly => assembly.GetName().Name == "Assembly-CSharp-Editor")
        .GetType("OnlineFullRoomSceneFixture", true);

    [SetUp]
    public void SetUp() => fixture = Activator.CreateInstance(Bridge);

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        if (fixture != null) yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture, null);
        fixture = null;
    }

    [UnityTest]
    public IEnumerator RemotePointerTracksPitchAcrossCameraPresetsWithoutGameplayInput([Values(false, true)] bool guest)
        => (IEnumerator)Bridge.GetMethod("Run").Invoke(fixture, new object[] { guest, 16, false, "team-pointers" });

    [UnityTest]
    public IEnumerator PitchHoverFeedsLocalPlayerInspection([Values(false, true)] bool guest)
        => (IEnumerator)Bridge.GetMethod("Run").Invoke(fixture, new object[] { guest, 16, false, "team-hover" });

    [UnityTest]
    public IEnumerator RoomIntroReservesEscapeOnBothRoles([Values(false, true)] bool guest)
        => (IEnumerator)Bridge.GetMethod("Run").Invoke(fixture, new object[] { guest, 16, false, "room-intro" });
}
