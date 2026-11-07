using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
public sealed class TiedPenaltyPanelPlayModeTests
{
    private object board;
    private static Type Bridge => AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Assembly-CSharp-Editor").GetType("GoalkeeperBoxEntryFixture", true);
    [SetUp] public void SetUp() => board = Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator TearDown()
    { if (board != null) yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(board, null); board = null; }
    public static IEnumerable<string> Cases => new[] { "1/normal", "-1/normal", "1/shootout", "-1/shootout" };
    [UnityTest] public IEnumerator TiedPenalty([ValueSource(nameof(Cases))] string scenario)
    { var p = scenario.Split('/'); return (IEnumerator)Bridge.GetMethod("RunTiedPenaltyPanel").Invoke(board, new object[] { int.Parse(p[0]), p[1] == "shootout" }); }
}
