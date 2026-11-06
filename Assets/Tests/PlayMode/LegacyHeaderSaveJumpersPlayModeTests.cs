using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

public sealed class LegacyHeaderSaveJumpersPlayModeTests
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
    [UnityTest] public IEnumerator TiedSave_JumpedLandingOccupants_0()
        => (IEnumerator)Bridge.GetMethod("RunNativeHeaderSaveJumpers").Invoke(board, new object[] { false, 0 });
    [UnityTest] public IEnumerator TiedSave_JumpedLandingOccupants_1()
        => (IEnumerator)Bridge.GetMethod("RunNativeHeaderSaveJumpers").Invoke(board, new object[] { false, 1 });
    [UnityTest] public IEnumerator TiedSave_JumpedLandingOccupants_2()
        => (IEnumerator)Bridge.GetMethod("RunNativeHeaderSaveJumpers").Invoke(board, new object[] { false, 2 });
    [UnityTest] public IEnumerator HandlingSpill_JumpedLandingOccupants_0()
        => (IEnumerator)Bridge.GetMethod("RunNativeHeaderSaveJumpers").Invoke(board, new object[] { true, 0 });
    [UnityTest] public IEnumerator HandlingSpill_JumpedLandingOccupants_1()
        => (IEnumerator)Bridge.GetMethod("RunNativeHeaderSaveJumpers").Invoke(board, new object[] { true, 1 });
    [UnityTest] public IEnumerator HandlingSpill_JumpedLandingOccupants_2()
        => (IEnumerator)Bridge.GetMethod("RunNativeHeaderSaveJumpers").Invoke(board, new object[] { true, 2 });
}
