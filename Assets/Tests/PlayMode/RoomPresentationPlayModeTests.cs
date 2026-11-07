using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

public sealed class RoomPresentationPlayModeTests
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
    public IEnumerator RoomPreviewDoubleClickInspectsDisplayedPlayer([Values(false, true)] bool goalkeeper)
        => (IEnumerator)Bridge.GetMethod("RunPreviewDoubleClickInspection").Invoke(board, new object[] { goalkeeper });
    [UnityTest]
    public IEnumerator RoomPreviewDoubleClicksAndDragsDoNothing()
        => (IEnumerator)Bridge.GetMethod("RunRoomPreviewIgnoresDraftPointers").Invoke(board, null);
    [UnityTest]
    public IEnumerator RoomShutdownDoesNotRecreateDestroyedApplicationContext()
        => (IEnumerator)Bridge.GetMethod("RunRoomShutdownDoesNotRecreateApplication").Invoke(board, null);
    [UnityTest]
    public IEnumerator AcceptedAndRejectedForfeitsRetainOnlyRealPresentationState([Values(-1, 1)] int direction, [Values(false, true)] bool rejectDefense)
        => (IEnumerator)Bridge.GetMethod("RunMovementForfeitPresentation").Invoke(board, new object[] { direction, rejectDefense });
    [UnityTest]
    public IEnumerator StoppingDribblerPacePreservesCompletedFacesWithoutForfeitMarks()
        => (IEnumerator)Bridge.GetMethod("RunDribblerPresentationForfeit").Invoke(board, null);
    [UnityTest]
    public IEnumerator SpaceInspectsBothCardTypesFromPitchAndLineup([Values(false, true)] bool goalkeeper, [Values(false, true)] bool lineup)
        => (IEnumerator)Bridge.GetMethod("RunInspectionCardPresentation").Invoke(board, new object[] { goalkeeper, lineup });
    [UnityTest]
    public IEnumerator InspectionBlocksPhysicalActionsWhileMovementAndCpuContinue()
        => (IEnumerator)Bridge.GetMethod("RunInspectionContinuesMovementAndCpuInput").Invoke(board, null);
    [UnityTest]
    public IEnumerator SlotsTrackSelectionCompletionPauseSubphasesForfeitAndReset()
        => (IEnumerator)Bridge.GetMethod("RunMovedSlotPresentation").Invoke(board, null);
    [UnityTest]
    public IEnumerator IntroReadinessIgnoresOffPitchBenchPlayers()
        => (IEnumerator)Bridge.GetMethod("RunIntroReadinessWithBench").Invoke(board, null);
    [UnityTest]
    public IEnumerator CameraIntroWeatherOverlaysAndCardProduceSafeAuthoredFrames()
        => (IEnumerator)Bridge.GetMethod("RunRoomVisualFrames").Invoke(board, null);
    [UnityTest]
    public IEnumerator IntroRestoresHudCameraAndKickoff([Values("complete", "button", "escape", "missing", "load")] string mode)
        => (IEnumerator)Bridge.GetMethod("RunIntroLifecycle").Invoke(board, new object[] { mode });
}
