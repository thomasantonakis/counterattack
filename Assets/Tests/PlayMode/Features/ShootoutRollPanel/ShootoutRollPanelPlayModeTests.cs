using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

public sealed class ShootoutRollPanelPlayModeTests
{
    private object board;
    private static Type Bridge => AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Assembly-CSharp-Editor").GetType("GoalkeeperBoxEntryFixture", true);
    [SetUp] public void SetUp() => board = Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator TearDown()
    {
        if (board != null) yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(board, null);
        board = null;
    }
    public static IEnumerable<string> Cases => new[] { "1/keeper", "-1/keeper", "1/offtarget", "-1/offtarget", "1/animation", "-1/animation" };
    [UnityTest] public IEnumerator ShootoutPanel([ValueSource(nameof(Cases))] string scenario)
    {
        var parts = scenario.Split('/');
        return (IEnumerator)Bridge.GetMethod("RunShootoutRollPanel").Invoke(board, new object[] { int.Parse(parts[0]), parts[1] });
    }
}
