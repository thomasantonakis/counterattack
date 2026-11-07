using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

public sealed class PossessionTimePlayModeTests
{
    private object fixture;
    private static Type Bridge => AppDomain.CurrentDomain.GetAssemblies()
        .Single(a => a.GetName().Name == "Assembly-CSharp-Editor").GetType("GoalkeeperBoxEntryFixture", true);

    [SetUp] public void SetUp() => fixture = Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator TearDown()
    {
        if (fixture != null) yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture, null);
        fixture = null;
    }

    [UnityTest]
    public IEnumerator PossessionTimeFollowsClockAndStoppageRules(
        [Values("turnover", "pauses", "halfBoundary", "extraActions", "realClock")] string scenario,
        [Values(true, false)] bool homeStarts)
        => (IEnumerator)Bridge.GetMethod("RunPossessionTime").Invoke(fixture, new object[] { scenario, homeStarts });

    [UnityTest]
    public IEnumerator OffTargetGoalKickSwitchesTimeBeforeNextCommit([Values(true, false)] bool homeStarts)
        => (IEnumerator)Bridge.GetMethod("RunPossessionTime").Invoke(fixture, new object[] { "goalKick", homeStarts });
}
