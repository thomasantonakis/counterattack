using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("GameplayGoalAudio")]
public sealed class GameplayGoalAudioPlayModeTests
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
    public IEnumerator GoalFillsRemainingGainDuringCrossfade([Values(true, false)] bool homeScored) =>
        (IEnumerator)Bridge.GetMethod("RunGoalDuringCrossfade").Invoke(board, new object[] { homeScored });

    [UnityTest]
    public IEnumerator GoalAudioBoundaries([Values("duplicate", "paused", "muted", "restore", "disabled", "missing")] string boundary) =>
        (IEnumerator)Bridge.GetMethod("RunGoalAudioBoundary").Invoke(board, new object[] { boundary });

    [UnityTest]
    public IEnumerator ConfirmedGoalAudioUsesCreditedTeam([Values(true, false)] bool homeScored,
        [Values(1, -1)] int direction, [Values(false, true)] bool ownGoal) =>
        (IEnumerator)Bridge.GetMethod("RunConfirmedGoalAudio").Invoke(board, new object[] { homeScored, direction, ownGoal });
}
