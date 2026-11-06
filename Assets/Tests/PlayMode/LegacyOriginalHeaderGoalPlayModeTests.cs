using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

public sealed class LegacyOriginalHeaderGoalPlayModeTests
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
    [UnityTest] public IEnumerator OriginalHeader_prep_Stay()
        => (IEnumerator)Bridge.GetMethod("RunOriginalHeaderGoalLegacy").Invoke(board, new object[] { "prep", false });
    [UnityTest] public IEnumerator OriginalHeader_prep_Rush()
        => (IEnumerator)Bridge.GetMethod("RunOriginalHeaderGoalLegacy").Invoke(board, new object[] { "prep", true });
    [UnityTest] public IEnumerator OriginalHeader_goal_Stay()
        => (IEnumerator)Bridge.GetMethod("RunOriginalHeaderGoalLegacy").Invoke(board, new object[] { "goal", false });
    [UnityTest] public IEnumerator OriginalHeader_goal_Rush()
        => (IEnumerator)Bridge.GetMethod("RunOriginalHeaderGoalLegacy").Invoke(board, new object[] { "goal", true });
    [UnityTest] public IEnumerator OriginalHeader_off_target_Stay()
        => (IEnumerator)Bridge.GetMethod("RunOriginalHeaderGoalLegacy").Invoke(board, new object[] { "off_target", false });
    [UnityTest] public IEnumerator OriginalHeader_off_target_Rush()
        => (IEnumerator)Bridge.GetMethod("RunOriginalHeaderGoalLegacy").Invoke(board, new object[] { "off_target", true });
    [UnityTest] public IEnumerator OriginalHeader_hold_Stay()
        => (IEnumerator)Bridge.GetMethod("RunOriginalHeaderGoalLegacy").Invoke(board, new object[] { "hold", false });
    [UnityTest] public IEnumerator OriginalHeader_hold_Rush()
        => (IEnumerator)Bridge.GetMethod("RunOriginalHeaderGoalLegacy").Invoke(board, new object[] { "hold", true });
    [UnityTest] public IEnumerator OriginalHeader_quick_throw_Stay()
        => (IEnumerator)Bridge.GetMethod("RunOriginalHeaderGoalLegacy").Invoke(board, new object[] { "quick_throw", false });
    [UnityTest] public IEnumerator OriginalHeader_quick_throw_Rush()
        => (IEnumerator)Bridge.GetMethod("RunOriginalHeaderGoalLegacy").Invoke(board, new object[] { "quick_throw", true });
    [UnityTest] public IEnumerator OriginalHeader_goal_kick_Stay()
        => (IEnumerator)Bridge.GetMethod("RunOriginalHeaderGoalLegacy").Invoke(board, new object[] { "goal_kick", false });
    [UnityTest] public IEnumerator OriginalHeader_goal_kick_Rush()
        => (IEnumerator)Bridge.GetMethod("RunOriginalHeaderGoalLegacy").Invoke(board, new object[] { "goal_kick", true });
    [UnityTest] public IEnumerator OriginalHeader_headed_away_Stay()
        => (IEnumerator)Bridge.GetMethod("RunOriginalHeaderGoalLegacy").Invoke(board, new object[] { "headed_away", false });
    [UnityTest] public IEnumerator OriginalHeader_headed_away_Rush()
        => (IEnumerator)Bridge.GetMethod("RunOriginalHeaderGoalLegacy").Invoke(board, new object[] { "headed_away", true });
    [UnityTest] public IEnumerator OriginalHeader_loose_Stay()
        => (IEnumerator)Bridge.GetMethod("RunOriginalHeaderGoalLegacy").Invoke(board, new object[] { "loose", false });
    [UnityTest] public IEnumerator OriginalHeader_loose_Rush()
        => (IEnumerator)Bridge.GetMethod("RunOriginalHeaderGoalLegacy").Invoke(board, new object[] { "loose", true });
    [UnityTest] public IEnumerator OriginalHeader_corner()
        => (IEnumerator)Bridge.GetMethod("RunOriginalHeaderGoalLegacy").Invoke(board, new object[] { "corner", false });
    [UnityTest] public IEnumerator OriginalHeader_spill()
        => (IEnumerator)Bridge.GetMethod("RunOriginalHeaderGoalLegacy").Invoke(board, new object[] { "spill", false });
    [UnityTest] public IEnumerator OriginalHeader_unfinished_own_goal()
        => (IEnumerator)Bridge.GetMethod("RunOriginalHeaderGoalLegacy").Invoke(board, new object[] { "unfinished_own_goal", true });
    [UnityTest] public IEnumerator OriginalHeader_unfinished_own_goal_Stay()
        => (IEnumerator)Bridge.GetMethod("RunOriginalHeaderGoalLegacy").Invoke(board, new object[] { "unfinished_own_goal", false });
}
