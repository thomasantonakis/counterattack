using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("GameplayDiceAudio")]
public sealed class GameplayDiceAudioPlayModeTests
{
    private object fixture;
    private static Type Bridge => AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Assembly-CSharp-Editor")
        .GetType("GoalkeeperBoxEntryFixture", true);
    [SetUp] public void SetUp() => fixture = Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator Cleanup()
    { if (fixture != null) yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture, null); fixture = null; }
    [UnityTest] public IEnumerator RequestedRollUsesGameAudioWithoutChangingOutcome(
        [Values(false, true)] bool rigged, [Values("normal", "muted", "paused", "missing")] string boundary) =>
        (IEnumerator)Bridge.GetMethod("RunDiceAudio").Invoke(fixture, new object[] { rigged, boundary });

    [UnityTest] public IEnumerator WigglePlaysOncePerActiveRollWaitAndStopsWhenItEnds() =>
        (IEnumerator)Bridge.GetMethod("RunDiceWiggleWait").Invoke(fixture, null);
}
