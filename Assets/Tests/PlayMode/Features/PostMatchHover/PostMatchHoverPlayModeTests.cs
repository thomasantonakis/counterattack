using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("PostMatchHover")]
public sealed class PostMatchHoverPlayModeTests
{
    private object fixture;
    private static Type Bridge => AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Assembly-CSharp-Editor").GetType("GoalkeeperBoxEntryFixture", true);
    [SetUp] public void SetUp() => fixture = Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator TearDown() => (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture, null);
    [UnityTest] public IEnumerator FullTimeSuppressesPitchAndLineupNamesAndRestoresInspection([Values(false, true)] bool lineup)
        => (IEnumerator)Bridge.GetMethod("RunPostMatchHover").Invoke(fixture, new object[] { lineup });
}
