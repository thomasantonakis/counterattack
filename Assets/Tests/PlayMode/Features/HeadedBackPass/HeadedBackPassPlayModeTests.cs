using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

public sealed class HeadedBackPassPlayModeTests
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
    public IEnumerator DirectHeader([Values(1,-1)] int side, [Values(false,true)] bool corner,
        [Values("feet","hold","spill","star","cancel","cancel_roll")] string branch)
        => (IEnumerator)Bridge.GetMethod("RunHeadedBackPass").Invoke(board, new object[] {side,corner,branch});
    [UnityTest]
    public IEnumerator OutsideKeeperCannotChooseHands([Values(1,-1)] int side)
        => (IEnumerator)Bridge.GetMethod("RunOutsideHeadedBackPass").Invoke(board, new object[] {side});
}
