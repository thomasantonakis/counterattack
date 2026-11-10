using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("GameplayFoulAudioTiming")]
public sealed class GameplayFoulAudioTimingPlayModeTests
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
    [UnityTest] public IEnumerator TackleFoulSoundsBeforeDribblerRoll(
        [Values("normal1", "normal2", "danger1", "danger2", "danger3", "nutmeg1", "nutmeg2", "jackpot")] string scenario,
        [Values(true, false)] bool penalisedHome, [Values(1, -1)] int direction) =>
        (IEnumerator)Bridge.GetMethod("RunEarlyTackleFoulAudio").Invoke(board, new object[] { scenario, penalisedHome, direction });

    [UnityTest] public IEnumerator CardReactionBelongsToFouledStand(
        [Values("yellow", "second_yellow", "straight_red", "no_card")] string card,
        [Values(true, false)] bool penalisedHome, [Values(1, -1)] int direction) =>
        (IEnumerator)Bridge.GetMethod("RunCardAudioAfterFoul").Invoke(board, new object[] { card, penalisedHome, direction });
}
