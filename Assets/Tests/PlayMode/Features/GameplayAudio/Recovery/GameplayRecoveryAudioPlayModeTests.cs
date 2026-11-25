using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("GameplayRecoveryAudio")]
public sealed class GameplayRecoveryAudioPlayModeTests
{
    private object board;
    private static Type Bridge => AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Assembly-CSharp-Editor")
        .GetType("GoalkeeperBoxEntryFixture", true);
    [SetUp] public void SetUp() => board = Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator TearDown()
    { if (board != null) yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(board, null); board = null; }
    [UnityTest] public IEnumerator ConfirmedRecoveriesUseBothStands(
        [Values("standard", "ftp", "long", "headedpass", "loose", "header", "attempt", "teammate", "tackle_duplicate", "gk_duplicate")] string kind,
        [Values(true, false)] bool home, [Values(1, -1)] int direction) =>
        (IEnumerator)Bridge.GetMethod("RunRecoveryAudio").Invoke(board, new object[] { kind, home, direction });
    [UnityTest] public IEnumerator ActualPassInterceptionKeepsContinuation() =>
        (IEnumerator)Bridge.GetMethod("RunPhysicalRecoveryAudio").Invoke(board, null);
}
