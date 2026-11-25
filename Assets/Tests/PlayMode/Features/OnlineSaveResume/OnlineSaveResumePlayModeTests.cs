using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("OnlineSaveResume")]
public sealed class OnlineSaveResumePlayModeTests
{
    private object fixture;

    [UnityTest]
    public IEnumerator HostSavesDuringGuestPauseAfterEitherSidePasses([Values(true, false)] bool homeAttacks)
        => RunCheckpoint(homeAttacks, afterMovement: false);

    [UnityTest]
    public IEnumerator HostSavesAndResumesAfterMovementWithPossession([Values(true, false)] bool homeAttacks)
        => RunCheckpoint(homeAttacks, afterMovement: true);

    [UnityTest]
    public IEnumerator HostSavesAndResumesAtLooseBallOpeningOrSettledStoppage(
        [Values(true, false)] bool homeAttacks, [Values("loose", "stoppage")] string boundary)
    {
        var bridge = AppDomain.CurrentDomain.GetAssemblies()
            .Single(assembly => assembly.GetName().Name == "Assembly-CSharp-Editor")
            .GetType("OnlineFullRoomSceneFixture", true);
        fixture = Activator.CreateInstance(bridge);
        return (IEnumerator)bridge.GetMethod("RunCheckpointAtNewBoundary").Invoke(fixture, new object[] { homeAttacks, boundary });
    }

    private IEnumerator RunCheckpoint(bool homeAttacks, bool afterMovement)
    {
        var bridge = AppDomain.CurrentDomain.GetAssemblies()
            .Single(assembly => assembly.GetName().Name == "Assembly-CSharp-Editor")
            .GetType("OnlineFullRoomSceneFixture", true);
        fixture = Activator.CreateInstance(bridge);
        return (IEnumerator)bridge.GetMethod("RunCheckpointPause").Invoke(fixture, new object[] { homeAttacks, afterMovement, false, false });
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        if (fixture != null) yield return (IEnumerator)fixture.GetType().GetMethod("Cleanup").Invoke(fixture, null);
        fixture = null;
    }
}
