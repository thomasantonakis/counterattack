using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("FixtureLighting")]
public sealed class FixtureLightingPlayModeTests
{
    private object rig;
    private static Type Bridge => AppDomain.CurrentDomain.GetAssemblies()
        .Single(a => a.GetName().Name == "Assembly-CSharp-Editor").GetType("FixtureLightingRuntimeFixture",true);
    [SetUp] public void SetUp() => rig = Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator TearDown()
    {
        if (rig != null) yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(rig,null);
        rig = null;
    }
    [UnityTest]
    public IEnumerator SavedSelectionAndLiveSettingsEvent([Values("MidAfternoon","Sunset","LateNight")] string selection)
        => (IEnumerator)Bridge.GetMethod("Run").Invoke(rig,new object[] {selection});

    [UnityTest]
    public IEnumerator IncomingSceneKeepsItsOwnEnvironment()
        => (IEnumerator)Bridge.GetMethod("RunSceneTransition").Invoke(rig,null);
}
