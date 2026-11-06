using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

public sealed class LegacyRestartPreparationPlayModeTests
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
    public static IEnumerable<string> Scenarios => new[] { "corner_attacker", "corner_defender", "goal_kick_clearance",
        "offside_spot", "free_kick_keeper", "corner_keeper", "penalty_no_final_third" };
    [UnityTest] public IEnumerator RestartPreparation([ValueSource(nameof(Scenarios))] string scenario)
        => (IEnumerator)Bridge.GetMethod("RunRestartPreparationLegacy").Invoke(board, new object[] { scenario });
}
