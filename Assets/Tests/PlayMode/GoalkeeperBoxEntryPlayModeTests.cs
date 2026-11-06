using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine.TestTools;

// Gameplay currently lives in the predefined Assembly-CSharp. An asmdef cannot
// reference that assembly. Keep this small reflection boundary until gameplay
// gains its own assembly; the editor bridge below is strongly typed gameplay code.
public sealed class GoalkeeperBoxEntryPlayModeTests
{
    private object fixture;
    private static Type Bridge => AppDomain.CurrentDomain.GetAssemblies()
        .Single(a => a.GetName().Name == "Assembly-CSharp-Editor").GetType("GoalkeeperBoxEntryFixture", true);

    public static IEnumerable<string> Cases
    {
        get
        {
            foreach (string direction in new[] { "Right", "Left" })
                foreach (string branch in new[] {
                    "ground", "ftp", "dribble", "reposition", "high", "long",
                    "own_box", "outside", "lost_foot", "lost_dive",
                    "tackle_stay", "tackle_reposition",
                    "high_inaccurate_inside", "high_inaccurate_outside",
                    "long_inaccurate_inside", "long_inaccurate_outside",
                    "ground_recovery", "ftp_recovery", "steal_recovery", "loose_recovery",
                    "header_recovery", "long_recovery", "long_direct", "loose_own_box",
                    "same_box_recovery", "tackle_scatter_inside", "tackle_scatter_outside",
                    "header_scatter_inside", "header_scatter_outside", "shot_scatter_inside" })
                    foreach (string choice in new[] { "skip", "move" })
                        yield return $"{direction}/{branch}/{choice}";
        }
    }

    [SetUp] public void SetUp() => fixture = Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator TearDown()
    {
        if (fixture != null) yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture, null);
        fixture = null;
    }

    [UnityTest]
    public IEnumerator BoxEntry([ValueSource(nameof(Cases))] string scenario)
        => (IEnumerator)Bridge.GetMethod("Run").Invoke(fixture, new object[] { scenario });

    [UnityTest]
    public IEnumerator InterruptedFixtureReleasesAsyncWaitersAndRestoresState()
        => (IEnumerator)Bridge.GetMethod("VerifyFailureCleanup").Invoke(fixture, null);
}
