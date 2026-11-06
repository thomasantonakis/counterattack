using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

public sealed class LegacyTackleContinuationPlayModeTests
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
    public IEnumerator RepositionInterceptionsAndStationaryDefenderTackle()
        => (IEnumerator)Bridge.GetMethod("RunRepositionInterceptionsLegacy").Invoke(board, null);
    [UnityTest]
    public IEnumerator ThreeNutmegsConsumeTwoPaceEachAndRecordTeamStats()
        => (IEnumerator)Bridge.GetMethod("RunThreeNutmegsLegacy").Invoke(board, null);
    [UnityTest]
    public IEnumerator InjuryPlayOnThenLostStationaryNutmegTransfersPossession()
        => (IEnumerator)Bridge.GetMethod("RunLostNutmegAfterFoulLegacy").Invoke(board, null);
    [UnityTest]
    public IEnumerator StealFoulThenTackleFoulThenSuccessfulTackleRecordsExactStats([Values(false, true)] bool dangerousOriginalPosition)
        => (IEnumerator)Bridge.GetMethod("RunRepeatedFoulTackleLegacy").Invoke(board, new object[] { dangerousOriginalPosition });
}
