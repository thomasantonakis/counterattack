using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

public sealed class LegacyLongBallPlayModeTests
{
    private object board;
    private static Type Bridge => AppDomain.CurrentDomain.GetAssemblies()
        .Single(a => a.GetName().Name == "Assembly-CSharp-Editor").GetType("GoalkeeperBoxEntryFixture", true);
    public static IEnumerable<string> Scenarios => new[] { "accurate_easy", "hard_commit", "scatter_space", "scatter_interception", "scatter_miss", "scatter_direct",
        "receiver_choice", "keeper_collect", "box_intercept_miss" };
    [SetUp] public void SetUp() => board = Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator TearDown()
    {
        if (board != null) yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(board, null);
        board = null;
    }
    [UnityTest]
    public IEnumerator LongBall([ValueSource(nameof(Scenarios))] string scenario)
        => (IEnumerator)Bridge.GetMethod("RunLongBallLegacy").Invoke(board, new object[] { scenario });
}
