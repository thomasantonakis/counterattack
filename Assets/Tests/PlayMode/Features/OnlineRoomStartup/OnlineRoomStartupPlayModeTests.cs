using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("OnlineRoomStartup")]
public sealed class OnlineRoomStartupPlayModeTests
{
    private object fixture;
    private static Type Bridge => AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Assembly-CSharp-Editor")
        .GetType("OnlineRoomStartupSceneFixture", true);
    [SetUp] public void Setup() => fixture = Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator Cleanup()
    { if (fixture != null) yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture, null); fixture = null; }
    [UnityTest] public IEnumerator NormallyLoadedGuestRoomHasNoAuthorityStartup()
        => (IEnumerator)Bridge.GetMethod("Run").Invoke(fixture, null);
}
