using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("OnlineSharedRollPanel")]
public sealed class OnlineSharedRollPanelPlayModeTests
{
    private object fixture;
    private static Type Bridge => AppDomain.CurrentDomain.GetAssemblies()
        .Single(assembly => assembly.GetName().Name == "Assembly-CSharp-Editor")
        .GetType("OnlineFullRoomSceneFixture", true);

    [SetUp]
    public void SetUp() => fixture = Activator.CreateInstance(Bridge);

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        if (fixture != null) yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture, null);
        fixture = null;
    }

    [UnityTest]
    public IEnumerator StoppagePanelReplicatesAndWaitsForBothManagersOrTimeout([Values(false, true)] bool timeout)
        => (IEnumerator)Bridge.GetMethod("Run").Invoke(fixture,
            new object[] { false, 16, false, timeout ? "shared-roll:timeout" : "shared-roll:both" });
    [UnityTest]
    public IEnumerator SingleRollAndHeaderUseTheOriginalGuestPanels([Values(false, true)] bool header)
        => (IEnumerator)Bridge.GetMethod("Run").Invoke(fixture,
            new object[] { false, 16, false, header ? "shared-header-panel" : "shared-duel-panel" });
}
