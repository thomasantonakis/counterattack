using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

public sealed class LegacyKeeperRemainingPlayModeTests
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
    public static IEnumerable<string> Shots => new[] { "block", "contact", "failed_save", "free_kick" };
    public static IEnumerable<string> Fouls => new[] { "hand_win", "hand_tie", "normal_feet", "normal_hands",
        "danger_feet", "danger_hands_red", "normal_feet_yellow", "stale_touch", "second_injury" };
    [UnityTest] public IEnumerator OutsideKeeperShot([ValueSource(nameof(Shots))] string scenario)
        => (IEnumerator)Bridge.GetMethod("RunOutsideKeeperShotLegacy").Invoke(board, new object[] { scenario });
    [UnityTest] public IEnumerator KeeperFoul([ValueSource(nameof(Fouls))] string scenario)
        => (IEnumerator)Bridge.GetMethod("RunKeeperFoulLegacy").Invoke(board, new object[] { scenario });
}
