using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("OnlineDraftInteractions")]
public sealed class OnlineDraftInteractionsPlayModeTests
{
    private object fixture;
    private static Type Bridge => AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Assembly-CSharp-Editor")
        .GetType("OnlineDraftSceneFixture", true);

    [SetUp]
    public void Setup() => fixture = Activator.CreateInstance(Bridge);

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        if (fixture != null) yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture, null);
        fixture = null;
    }

    [UnityTest]
    public IEnumerator ManagersUseTheirAuthoredPoolControls(
        [Values("InteractionsGK", "InteractionsOutfield", "InteractionsArcade")] string mode,
        [Values(true, false)] bool localHome)
        => (IEnumerator)Bridge.GetMethod("RunVariant").Invoke(fixture, new object[] { mode, localHome });

    [UnityTest]
    public IEnumerator SpectatorJoinsKeeperRollAndFollowsArcadeOutfieldReadOnly()
        => (IEnumerator)Bridge.GetMethod("RunSpectatorPool").Invoke(fixture, null);

    [UnityTest]
    public IEnumerator PoolRowsAndPageControlsFitBetweenTheRosters([Values(true, false)] bool localHome)
        => (IEnumerator)Bridge.GetMethod("RunVariant").Invoke(fixture, new object[] { "PoolLayout", localHome });
}
