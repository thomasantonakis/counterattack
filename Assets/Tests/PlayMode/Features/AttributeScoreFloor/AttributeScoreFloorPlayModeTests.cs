using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

public sealed class AttributeScoreFloorPlayModeTests
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
    public IEnumerator SnapshotFloorsCurrentShooting([Values(false,true)] bool injured,
        [Values(false,true)] bool jackpot, [Values(1,-1)] int side)
        => (IEnumerator)Bridge.GetMethod("RunScoreFloorSnapshot").Invoke(board, new object[] { injured, jackpot, side });

    [UnityTest]
    public IEnumerator SavingZeroTiesHeaderDespiteDistancePenalty([Values(false,true)] bool injured, [Values(1,-1)] int side)
        => (IEnumerator)Bridge.GetMethod("RunScoreFloorHeaderSave").Invoke(board, new object[] { injured, side });

    [UnityTest]
    public IEnumerator BallControlFloorsInjuredDribblingAtDistanceTwo([Values(1,-1)] int side)
        => (IEnumerator)Bridge.GetMethod("RunScoreFloorControl").Invoke(board, new object[] { side });
}
