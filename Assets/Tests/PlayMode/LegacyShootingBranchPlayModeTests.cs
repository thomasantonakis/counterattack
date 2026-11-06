using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

public sealed class LegacyShootingBranchPlayModeTests
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
    [UnityTest] public IEnumerator ShootingBranch_01()
        => (IEnumerator)Bridge.GetMethod("RunNativeShootingBranch").Invoke(board, new object[] { 0 });
    [UnityTest] public IEnumerator ShootingBranch_02()
        => (IEnumerator)Bridge.GetMethod("RunNativeShootingBranch").Invoke(board, new object[] { 1 });
    [UnityTest] public IEnumerator ShootingBranch_03()
        => (IEnumerator)Bridge.GetMethod("RunNativeShootingBranch").Invoke(board, new object[] { 2 });
    [UnityTest] public IEnumerator ShootingBranch_04()
        => (IEnumerator)Bridge.GetMethod("RunNativeShootingBranch").Invoke(board, new object[] { 3 });
    [UnityTest] public IEnumerator ShootingBranch_05()
        => (IEnumerator)Bridge.GetMethod("RunNativeShootingBranch").Invoke(board, new object[] { 4 });
    [UnityTest] public IEnumerator ShootingBranch_06()
        => (IEnumerator)Bridge.GetMethod("RunNativeShootingBranch").Invoke(board, new object[] { 5 });
    [UnityTest] public IEnumerator ShootingBranch_07()
        => (IEnumerator)Bridge.GetMethod("RunNativeShootingBranch").Invoke(board, new object[] { 6 });
    [UnityTest] public IEnumerator ShootingBranch_08()
        => (IEnumerator)Bridge.GetMethod("RunNativeShootingBranch").Invoke(board, new object[] { 7 });
    [UnityTest] public IEnumerator ShootingBranch_09()
        => (IEnumerator)Bridge.GetMethod("RunNativeShootingBranch").Invoke(board, new object[] { 8 });
    [UnityTest] public IEnumerator ShootingBranch_10()
        => (IEnumerator)Bridge.GetMethod("RunNativeShootingBranch").Invoke(board, new object[] { 9 });
    [UnityTest] public IEnumerator ShootingBranch_11()
        => (IEnumerator)Bridge.GetMethod("RunNativeShootingBranch").Invoke(board, new object[] { 10 });
    [UnityTest] public IEnumerator ShootingBranch_12()
        => (IEnumerator)Bridge.GetMethod("RunNativeShootingBranch").Invoke(board, new object[] { 11 });
    [UnityTest] public IEnumerator ShootingBranch_13()
        => (IEnumerator)Bridge.GetMethod("RunNativeShootingBranch").Invoke(board, new object[] { 12 });
    [UnityTest] public IEnumerator ShootingBranch_14()
        => (IEnumerator)Bridge.GetMethod("RunNativeShootingBranch").Invoke(board, new object[] { 13 });
    [UnityTest] public IEnumerator ShootingBranch_15()
        => (IEnumerator)Bridge.GetMethod("RunNativeShootingBranch").Invoke(board, new object[] { 14 });
    [UnityTest] public IEnumerator ShootingBranch_16()
        => (IEnumerator)Bridge.GetMethod("RunNativeShootingBranch").Invoke(board, new object[] { 15 });
    [UnityTest] public IEnumerator ShootingBranch_17()
        => (IEnumerator)Bridge.GetMethod("RunNativeShootingBranch").Invoke(board, new object[] { 16 });
    [UnityTest] public IEnumerator ShootingBranch_18()
        => (IEnumerator)Bridge.GetMethod("RunNativeShootingBranch").Invoke(board, new object[] { 17 });
    [UnityTest] public IEnumerator ShootingBranch_19()
        => (IEnumerator)Bridge.GetMethod("RunNativeShootingBranch").Invoke(board, new object[] { 18 });
    [UnityTest] public IEnumerator ShootingBranch_20()
        => (IEnumerator)Bridge.GetMethod("RunNativeShootingBranch").Invoke(board, new object[] { 19 });
    [UnityTest] public IEnumerator ShootingBranch_21()
        => (IEnumerator)Bridge.GetMethod("RunNativeShootingBranch").Invoke(board, new object[] { 20 });
    [UnityTest] public IEnumerator ShootingBranch_22()
        => (IEnumerator)Bridge.GetMethod("RunNativeShootingBranch").Invoke(board, new object[] { 21 });
    [UnityTest] public IEnumerator ShootingBranch_23()
        => (IEnumerator)Bridge.GetMethod("RunNativeShootingBranch").Invoke(board, new object[] { 22 });
    [UnityTest] public IEnumerator ShootingBranch_24()
        => (IEnumerator)Bridge.GetMethod("RunNativeShootingBranch").Invoke(board, new object[] { 23 });
    [UnityTest] public IEnumerator ShootingBranch_25()
        => (IEnumerator)Bridge.GetMethod("RunNativeShootingBranch").Invoke(board, new object[] { 24 });
    [UnityTest] public IEnumerator ShootingBranch_26()
        => (IEnumerator)Bridge.GetMethod("RunNativeShootingBranch").Invoke(board, new object[] { 25 });
    [UnityTest] public IEnumerator ShootingBranch_27()
        => (IEnumerator)Bridge.GetMethod("RunNativeShootingBranch").Invoke(board, new object[] { 26 });
    [UnityTest] public IEnumerator ShootingBranch_28()
        => (IEnumerator)Bridge.GetMethod("RunNativeShootingBranch").Invoke(board, new object[] { 27 });
    [UnityTest] public IEnumerator ShootingBranch_29()
        => (IEnumerator)Bridge.GetMethod("RunNativeShootingBranch").Invoke(board, new object[] { 28 });
    [UnityTest] public IEnumerator ShootingBranch_30()
        => (IEnumerator)Bridge.GetMethod("RunNativeShootingBranch").Invoke(board, new object[] { 29 });
    [UnityTest] public IEnumerator ShootingBranch_31()
        => (IEnumerator)Bridge.GetMethod("RunNativeShootingBranch").Invoke(board, new object[] { 30 });
}
