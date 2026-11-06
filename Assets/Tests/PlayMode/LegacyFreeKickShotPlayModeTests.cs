using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

public sealed class LegacyFreeKickShotPlayModeTests
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
    [UnityTest] public IEnumerator OriginalFreeKickSheetSetupKeepsPhaseOwnershipAndOffersShot()
        => (IEnumerator)Bridge.GetMethod("RunNativeFreeKickSheetPreparation").Invoke(board, null);
    [UnityTest] public IEnumerator FreeKickShotBranch_01()
        => (IEnumerator)Bridge.GetMethod("RunNativeFreeKickShotBranch").Invoke(board, new object[] { 0 });
    [UnityTest] public IEnumerator FreeKickShotBranch_02()
        => (IEnumerator)Bridge.GetMethod("RunNativeFreeKickShotBranch").Invoke(board, new object[] { 1 });
    [UnityTest] public IEnumerator FreeKickShotBranch_03()
        => (IEnumerator)Bridge.GetMethod("RunNativeFreeKickShotBranch").Invoke(board, new object[] { 2 });
    [UnityTest] public IEnumerator FreeKickShotBranch_04()
        => (IEnumerator)Bridge.GetMethod("RunNativeFreeKickShotBranch").Invoke(board, new object[] { 3 });
    [UnityTest] public IEnumerator FreeKickShotBranch_05()
        => (IEnumerator)Bridge.GetMethod("RunNativeFreeKickShotBranch").Invoke(board, new object[] { 4 });
    [UnityTest] public IEnumerator FreeKickShotBranch_06()
        => (IEnumerator)Bridge.GetMethod("RunNativeFreeKickShotBranch").Invoke(board, new object[] { 5 });
    [UnityTest] public IEnumerator FreeKickShotBranch_07()
        => (IEnumerator)Bridge.GetMethod("RunNativeFreeKickShotBranch").Invoke(board, new object[] { 6 });
    [UnityTest] public IEnumerator FreeKickShotBranch_08()
        => (IEnumerator)Bridge.GetMethod("RunNativeFreeKickShotBranch").Invoke(board, new object[] { 7 });
    [UnityTest] public IEnumerator FreeKickShotBranch_09()
        => (IEnumerator)Bridge.GetMethod("RunNativeFreeKickShotBranch").Invoke(board, new object[] { 8 });
    [UnityTest] public IEnumerator FreeKickShotBranch_10()
        => (IEnumerator)Bridge.GetMethod("RunNativeFreeKickShotBranch").Invoke(board, new object[] { 9 });
    [UnityTest] public IEnumerator FreeKickShotBranch_11()
        => (IEnumerator)Bridge.GetMethod("RunNativeFreeKickShotBranch").Invoke(board, new object[] { 10 });
    [UnityTest] public IEnumerator FreeKickShotBranch_12()
        => (IEnumerator)Bridge.GetMethod("RunNativeFreeKickShotBranch").Invoke(board, new object[] { 11 });
}
