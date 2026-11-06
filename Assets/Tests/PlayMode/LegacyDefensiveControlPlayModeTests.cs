using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

public sealed class LegacyDefensiveControlPlayModeTests
{
    private object board;
    private static Type Bridge => AppDomain.CurrentDomain.GetAssemblies()
        .Single(a => a.GetName().Name == "Assembly-CSharp-Editor").GetType("GoalkeeperBoxEntryFixture", true);
    public static IEnumerable<string> Scenarios => new[] { "landing", "head", "control", "offer", "recover", "miss", "success" };
    [SetUp] public void SetUp() => board = Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator TearDown()
    {
        if (board != null) yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(board, null);
        board = null;
    }
    [UnityTest]
    public IEnumerator DefensiveHighPassControl([ValueSource(nameof(Scenarios))] string scenario)
        => (IEnumerator)Bridge.GetMethod("RunDefensiveControlLegacy").Invoke(board, new object[] { scenario });
}
