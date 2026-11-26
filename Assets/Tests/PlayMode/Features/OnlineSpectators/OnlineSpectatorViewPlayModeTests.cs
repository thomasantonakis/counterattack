using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("OnlineSpectatorView")]
public sealed class OnlineSpectatorViewPlayModeTests
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
        if (fixture != null)
            yield return (IEnumerator)Bridge.GetMethod("CleanupSpectatorView").Invoke(fixture, null);
        fixture = null;
    }

    [UnityTest]
    public IEnumerator FollowsHostAndProjectsRoomWithoutManagerControls([Values(false, true)] bool late)
        => (IEnumerator)Bridge.GetMethod("RunSpectatorView").Invoke(fixture, new object[] { late });
}
