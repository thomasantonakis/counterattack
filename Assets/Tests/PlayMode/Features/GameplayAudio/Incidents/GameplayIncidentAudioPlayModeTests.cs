using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("GameplayIncidentAudio")]
public sealed class GameplayIncidentAudioPlayModeTests
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
    private IEnumerator Run(string method, params object[] args) => (IEnumerator)Bridge.GetMethod(method).Invoke(board, args);

    [UnityTest] public IEnumerator ConfirmedIncident(
        [Values("tackle", "nutmeg", "keep", "steal", "foul", "off_target", "block", "save", "kickoff", "half_time", "full_time", "pending")] string action,
        [Values(true, false)] bool home, [Values(1, -1)] int direction) => Run("RunIncidentAudio", action, home, direction);
    [UnityTest] public IEnumerator SubstitutionsAfterResume([Values(true, false)] bool home,
        [Values(true, false)] bool injury, [Values(true, false)] bool multiple) => Run("RunIncidentSubstitution", home, injury, multiple);
    [UnityTest] public IEnumerator PreparationCrossfadesUntilExecution([Values(true, false)] bool home,
        [Values(1, -1)] int direction) => Run("RunPreparationAudio", home, direction);
    [UnityTest] public IEnumerator SnapshotAvailabilityPlaysOnce([Values(true, false)] bool home) => Run("RunSnapshotAudio", home);
    [UnityTest] public IEnumerator ActualStealContinuation([Values(true, false)] bool succeeds) => Run("RunStealAudio", succeeds);
    [UnityTest] public IEnumerator RecoveryFoulUsesPenalisedStand([Values(true, false)] bool home) => Run("RunRecoveryFoulAudio", home);
    [UnityTest] public IEnumerator OutsideHandballUsesPenalisedStand([Values(true, false)] bool tie) => Run("RunOutsideHandballAudio", tie);
    [UnityTest] public IEnumerator InaccuracyRewardsRestartStand([Values("throw", "corner", "goal_kick")] string restart,
        [Values(1, -1)] int direction) => Run("RunOutOfBoundsAudio", restart, direction);
}
