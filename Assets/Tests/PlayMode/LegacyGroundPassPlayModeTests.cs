using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

public sealed class LegacyGroundPassPlayModeTests
{
    private object board;
    private static Type Bridge => AppDomain.CurrentDomain.GetAssemblies()
        .Single(a => a.GetName().Name == "Assembly-CSharp-Editor").GetType("GoalkeeperBoxEntryFixture", true);
    public static IEnumerable<string> Scenarios => new[] { "preview_confirm", "quick_throw", "no_interception", "second_interceptor", "forfeit_cycle" };
    [SetUp] public void SetUp() => board = Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator TearDown()
    {
        if (board != null) yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(board, null);
        board = null;
    }
    [UnityTest]
    public IEnumerator GroundPass([ValueSource(nameof(Scenarios))] string scenario)
        => (IEnumerator)Bridge.GetMethod("RunGroundPassLegacy").Invoke(board, new object[] { scenario });
}
