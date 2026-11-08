using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("MatchEventBanners")]
public sealed class MatchEventBannerPlayModeTests
{
    private object fixture;
    private static Type Bridge => AppDomain.CurrentDomain.GetAssemblies()
        .Single(a => a.GetName().Name == "Assembly-CSharp-Editor").GetType("MatchEventBannerRuntimeFixture", true);

    [SetUp] public void SetUp() => fixture = Activator.CreateInstance(Bridge);

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (fixture != null) yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture, null);
        fixture = null;
    }

    [UnityTest]
    public IEnumerator LiveQueueUsesUnscaledTimeAndUnsubscribesWhenDisabled()
        => (IEnumerator)Bridge.GetMethod("Run").Invoke(fixture, null);
}
