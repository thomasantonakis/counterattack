using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

public sealed class LegacyBoxLooseContinuationPlayModeTests
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
    [UnityTest] public IEnumerator DefensiveScatterHitDoesNotOfferSnapshotOrDuplicateMovedRows()
        => (IEnumerator)Bridge.GetMethod("RunDefensiveBoxLooseNoSnapshotLegacy").Invoke(board, null);
    [UnityTest] public IEnumerator NutmegTieBackUsesExhaustedPaceSnapshotAndResumesAttack()
        => (IEnumerator)Bridge.GetMethod("RunNutmegTieBackSnapshotLegacy").Invoke(board, null);
}
