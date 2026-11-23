using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("OnlineScorers")]
public sealed class OnlineScorersPlayModeTests
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
    public IEnumerator BothTeamsScorersReachTheAuthoredGuestPanel()
        => (IEnumerator)Bridge.GetMethod("Run").Invoke(fixture, new object[] { false, 16, false, "scorers" });
}
