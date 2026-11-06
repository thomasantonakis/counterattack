using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

public sealed class LegacyDuelPresentationPlayModeTests
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
    public IEnumerator AuthoredDuelPanelStartsClosedWithAllSceneBindings()
        => (IEnumerator)Bridge.GetMethod("RunDuelPanelBindingsLegacy").Invoke(board, null);

    public static IEnumerable<string> GoalkeeperCases => new[] { "prompt", "foot_tie", "foot_win",
        "Right/lost_foot", "Left/lost_foot", "Right/lost_dive", "Left/lost_dive" };
    [UnityTest]
    public IEnumerator GoalkeeperChallenge([ValueSource(nameof(GoalkeeperCases))] string scenario)
        => (IEnumerator)Bridge.GetMethod("RunGoalkeeperChallengeLegacy").Invoke(board, new object[] { scenario });
}
