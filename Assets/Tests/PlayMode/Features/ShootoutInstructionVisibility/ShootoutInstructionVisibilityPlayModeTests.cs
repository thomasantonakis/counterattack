using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
public sealed class ShootoutInstructionVisibilityPlayModeTests
{
    private object board;
    private static Type Bridge => AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Assembly-CSharp-Editor").GetType("GoalkeeperBoxEntryFixture", true);
    [SetUp] public void SetUp() => board = Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator TearDown()
    { if (board != null) yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(board, null); board = null; }
    public static IEnumerable<string> Cases => new[] { "shootout/prompt", "shootout/result", "normal/prompt", "normal/result" };
    [UnityTest] public IEnumerator InstructionTextWithOpenRollPanel([ValueSource(nameof(Cases))] string scenario)
    { var p = scenario.Split('/'); return (IEnumerator)Bridge.GetMethod("RunShootoutInstructionVisibility").Invoke(board, new object[] { p[0] == "shootout", p[1] == "result" }); }
}
