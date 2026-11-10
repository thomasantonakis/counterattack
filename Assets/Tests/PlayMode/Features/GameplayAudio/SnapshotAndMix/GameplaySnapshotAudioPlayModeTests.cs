using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("GameplaySnapshotAudio")]
public sealed class GameplaySnapshotAudioPlayModeTests
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
    [UnityTest] public IEnumerator MovementSnapshotUsesActualOffer([Values(true, false)] bool home,
        [Values(1, -1)] int direction) =>
        (IEnumerator)Bridge.GetMethod("RunMovementSnapshotAudio").Invoke(board, new object[] { home, direction });
    [UnityTest] public IEnumerator SnapshotDeferralRemainsCurrent([Values("blocked", "stale", "gk", "muted", "paused")] string branch) =>
        (IEnumerator)Bridge.GetMethod("RunDeferredSnapshotAudio").Invoke(board, new object[] { branch });
    [UnityTest] public IEnumerator MatchTypeControlsEveryStandLayer([Values("International", "Club")] string matchType,
        [Values(true, false)] bool homeScored) =>
        (IEnumerator)Bridge.GetMethod("RunInternationalCrowdAudio").Invoke(board, new object[] { matchType, homeScored });
}
