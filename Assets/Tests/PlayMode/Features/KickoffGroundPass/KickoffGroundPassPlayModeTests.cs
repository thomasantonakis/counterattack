using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

public sealed class KickoffGroundPassPlayModeTests
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
    [UnityTest] public IEnumerator OpeningOffersOnlyGroundPass([Values(1,-1)] int side,
        [Values("Hotseat", "Single Player", "Multiplayer")] string mode)
        => (IEnumerator)Bridge.GetMethod("RunOpeningGroundPassOnly").Invoke(board, new object[] {side,mode});
    [UnityTest] public IEnumerator SeedWithoutSnapshotPreservesOpeningChoices([Values(false,true)] bool roomFixture,
        [Values(1,-1)] int side)
        => (IEnumerator)Bridge.GetMethod("RunOpeningFromSeedSource").Invoke(board, new object[] {roomFixture,side});
    [UnityTest] public IEnumerator OwnHalfPassHasNoDistanceCap([Values(1,-1)] int side,
        [Values(1,2,3)] int difficulty, [Values(0,1,2,4)] int restartHalf)
        => (IEnumerator)Bridge.GetMethod("RunKickoffOwnHalfPass").Invoke(board, new object[] {side,difficulty,restartHalf});
    [UnityTest] public IEnumerator SnapshotPreservesKickoffOptions([Values(false,true)] bool groundOnly)
        => (IEnumerator)Bridge.GetMethod("RunKickoffSnapshotOptions").Invoke(board, new object[] {groundOnly});
    [UnityTest] public IEnumerator OpponentHalfAndHalfwayUseNormalPassRules([Values(1,-1)] int side,
        [Values(1,2,3)] int difficulty, [Values(false,true)] bool halfway)
        => (IEnumerator)Bridge.GetMethod("RunKickoffNormalTarget").Invoke(board, new object[] {side,difficulty,halfway});
}
