using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

public sealed class LegacyHeaderGoalBranchPlayModeTests
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
    [UnityTest] public IEnumerator HeaderGoalBranch_01()
        => (IEnumerator)Bridge.GetMethod("RunNativeHeaderGoalBranch").Invoke(board, new object[] { 0 });
    [UnityTest] public IEnumerator HeaderGoalBranch_02()
        => (IEnumerator)Bridge.GetMethod("RunNativeHeaderGoalBranch").Invoke(board, new object[] { 1 });
    [UnityTest] public IEnumerator HeaderGoalBranch_03()
        => (IEnumerator)Bridge.GetMethod("RunNativeHeaderGoalBranch").Invoke(board, new object[] { 2 });
    [UnityTest] public IEnumerator HeaderGoalBranch_04()
        => (IEnumerator)Bridge.GetMethod("RunNativeHeaderGoalBranch").Invoke(board, new object[] { 3 });
    [UnityTest] public IEnumerator HeaderGoalBranch_05()
        => (IEnumerator)Bridge.GetMethod("RunNativeHeaderGoalBranch").Invoke(board, new object[] { 4 });
    [UnityTest] public IEnumerator HeaderGoalBranch_06()
        => (IEnumerator)Bridge.GetMethod("RunNativeHeaderGoalBranch").Invoke(board, new object[] { 5 });
    [UnityTest] public IEnumerator HeaderGoalBranch_07()
        => (IEnumerator)Bridge.GetMethod("RunNativeHeaderGoalBranch").Invoke(board, new object[] { 6 });
    [UnityTest] public IEnumerator HeaderGoalBranch_08()
        => (IEnumerator)Bridge.GetMethod("RunNativeHeaderGoalBranch").Invoke(board, new object[] { 7 });
    [UnityTest] public IEnumerator HeaderGoalBranch_09()
        => (IEnumerator)Bridge.GetMethod("RunNativeHeaderGoalBranch").Invoke(board, new object[] { 8 });
    [UnityTest] public IEnumerator HeaderGoalBranch_10()
        => (IEnumerator)Bridge.GetMethod("RunNativeHeaderGoalBranch").Invoke(board, new object[] { 9 });
    [UnityTest] public IEnumerator HeaderGoalBranch_11()
        => (IEnumerator)Bridge.GetMethod("RunNativeHeaderGoalBranch").Invoke(board, new object[] { 10 });
    [UnityTest] public IEnumerator HeaderGoalBranch_12()
        => (IEnumerator)Bridge.GetMethod("RunNativeHeaderGoalBranch").Invoke(board, new object[] { 11 });
    [UnityTest] public IEnumerator HeaderGoalBranch_13()
        => (IEnumerator)Bridge.GetMethod("RunNativeHeaderGoalBranch").Invoke(board, new object[] { 12 });
}
