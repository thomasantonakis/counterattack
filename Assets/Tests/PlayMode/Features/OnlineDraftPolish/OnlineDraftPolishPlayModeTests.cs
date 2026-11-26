using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("OnlineDraftPolish")]
public sealed class OnlineDraftPolishPlayModeTests
{
    private object fixture;
    private static Type Bridge => AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Assembly-CSharp-Editor")
        .GetType("OnlineDraftPolishSceneFixture", true);

    [SetUp]
    public void Setup() => fixture = Activator.CreateInstance(Bridge);

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        if (fixture != null) yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture, null);
        fixture = null;
    }

    [UnityTest]
    public IEnumerator DraftAndOpeningRollsPlayHotSeatCues([Values(true, false)] bool asHost)
        => (IEnumerator)Bridge.GetMethod("RunRollAudio").Invoke(fixture, new object[] { asHost });

    [UnityTest]
    public IEnumerator KeeperDribblingUsesTheCatalogAndCanBeSortedAndFiltered()
        => (IEnumerator)Bridge.GetMethod("RunKeeperRatings").Invoke(fixture, null);

    [UnityTest]
    public IEnumerator ExplicitDepartureCentersTheConnectionPanel([Values(true, false)] bool inDraft)
        => (IEnumerator)Bridge.GetMethod("RunDeparture").Invoke(fixture, new object[] { inDraft });
}
