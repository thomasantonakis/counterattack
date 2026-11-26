using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("OnlineGoalAudio")]
public sealed class OnlineGoalAudioPlayModeTests
{
    private object fixture;
    private static Type Bridge => AppDomain.CurrentDomain.GetAssemblies()
        .Single(assembly => assembly.GetName().Name == "Assembly-CSharp-Editor")
        .GetType("OnlineFullRoomSceneFixture", true);

    [SetUp]
    public void Setup() => fixture = Activator.CreateInstance(Bridge);

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        if (fixture != null) yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture, null);
        fixture = null;
    }

    [UnityTest]
    public IEnumerator GuestHearsEachNewGoalAtLocalVolume([Values(true, false)] bool homeScored) =>
        (IEnumerator)Bridge.GetMethod("RunGuestGoalAudio").Invoke(fixture, new object[] { homeScored });
}
