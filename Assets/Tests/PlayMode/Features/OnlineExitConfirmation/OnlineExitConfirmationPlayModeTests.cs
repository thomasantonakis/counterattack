using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("OnlineExitConfirmation")]
public sealed class OnlineExitConfirmationPlayModeTests
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
    public IEnumerator LobbyExitUsesMainMenuConfirmation() =>
        (IEnumerator)Bridge.GetMethod("RunOnlineMenuExitConfirmation").Invoke(fixture, null);

    [UnityTest]
    public IEnumerator PauseExitFollowsOfflineSaveAndLeaveBehavior(
        [Values("leave-new", "leave-existing", "leave-without-saving", "leave-unavailable")] string boundary) =>
        (IEnumerator)Bridge.GetMethod("RunOnlinePauseExitConfirmation").Invoke(fixture, new object[] { boundary });
}
