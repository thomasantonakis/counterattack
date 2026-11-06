using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

public sealed class LegacyMovementPlayModeTests
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
    public IEnumerator FourAttackFiveDefenseAndTwoForTwoComplete()
        => (IEnumerator)Bridge.GetMethod("RunSafeMovementLegacy").Invoke(board, null);

    [UnityTest]
    public IEnumerator DribblerForfeitRegistersOnceAndNextAttackerMoves()
        => (IEnumerator)Bridge.GetMethod("RunDribbleForfeitLegacy").Invoke(board, null);

    [UnityTest]
    public IEnumerator StealAfterDecliningNutmeg([Values(false, true)] bool succeeds)
        => (IEnumerator)Bridge.GetMethod("RunMovementStealLegacy").Invoke(board, new object[] { succeeds });
}
