using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

public sealed class LegacyMovementScatterPlayModeTests
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
    [UnityTest] public IEnumerator InjuredDribblerTackleScatter([Values(false, true)] bool intercept)
        => (IEnumerator)Bridge.GetMethod("RunInjuredTackleScatterLegacy").Invoke(board, new object[] { intercept });
    [UnityTest] public IEnumerator MissedInterceptionHitsAttackerThenNewTackleCausesThrowIn()
        => (IEnumerator)Bridge.GetMethod("RunTackleScatterThrowLegacy").Invoke(board, null);
    [UnityTest] public IEnumerator OwnPickupThenTwoMissedInterceptionsThenDefensivePickup()
        => (IEnumerator)Bridge.GetMethod("RunPickupThenMissedScatterLegacy").Invoke(board, null);
    [UnityTest] public IEnumerator LooseBallBoxEntryMovesKeeperBeforeFlightAndResumesDefense()
        => (IEnumerator)Bridge.GetMethod("RunMovementScatterBoxLegacy").Invoke(board, null);
}
