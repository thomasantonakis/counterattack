using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

public sealed class RulesViewerPlayModeTests
{
    private object fixture;
    private static Type Bridge => AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Assembly-CSharp-Editor").GetType("RulesViewerPlayModeFixture", true);
    [SetUp] public void SetUp() => fixture = Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator Cleanup() => (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture, null);
    [UnityTest] public IEnumerator MainMenuNavigationAndSearch() => (IEnumerator)Bridge.GetMethod("Run").Invoke(fixture,new object[] { "menu" });
    [UnityTest] public IEnumerator RoomKeepsLivePlayRunning() => (IEnumerator)Bridge.GetMethod("Run").Invoke(fixture,new object[] { "room-live" });
    [UnityTest] public IEnumerator RoomPreservesExistingPause() => (IEnumerator)Bridge.GetMethod("Run").Invoke(fixture,new object[] { "room-paused" });
}
