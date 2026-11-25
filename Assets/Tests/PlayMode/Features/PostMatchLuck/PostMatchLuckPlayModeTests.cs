using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("PostMatchLuck")]
public sealed class PostMatchLuckPlayModeTests
{
    private object fixture;
    private static Type Bridge => AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Assembly-CSharp-Editor").GetType("GoalkeeperBoxEntryFixture", true);
    [SetUp] public void SetUp() => fixture = Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator TearDown() => (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture, null);
    [UnityTest] public IEnumerator CapturePreservesRandomSequenceAndPreRollInputsAcrossSaveReload([Values(1, -1)] int side, [Values(true, false)] bool home)
        => (IEnumerator)Bridge.GetMethod("RunLuckCapture").Invoke(fixture, new object[] { side, home });
    [UnityTest] public IEnumerator RealAerialRollsShareOneCompleteContest([Values(1, -1)] int side)
        => (IEnumerator)Bridge.GetMethod("RunLuckAerialCapture").Invoke(fixture, new object[] { side });
    [UnityTest] public IEnumerator RealTackleKeepsModifiersAndExcludesBothRiggedRolls([Values(1, -1)] int side)
        => (IEnumerator)Bridge.GetMethod("RunLuckGroundCapture").Invoke(fixture, new object[] { side });
    [UnityTest] public IEnumerator HeaderAndLooseRecoveryRollsCaptureTheCorrectPlayerAndAttribute([Values(1, -1)] int side, [Values(true, false)] bool headerPass)
        => (IEnumerator)Bridge.GetMethod("RunLuckRecoveryCapture").Invoke(fixture, new object[] { side, headerPass });
}
