using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

public sealed class LegacyThrowInPlayModeTests
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
    public IEnumerator ThrowExecution([Values("feet", "single", "easy", "hard")] string scenario)
        => (IEnumerator)Bridge.GetMethod("RunThrowInLegacy").Invoke(board, new object[] { scenario });
    [UnityTest]
    public IEnumerator ThrowInDisplacementAndRestrictedMovement()
        => (IEnumerator)Bridge.GetMethod("RunThrowInRestrictedLegacy").Invoke(board, null);
}
