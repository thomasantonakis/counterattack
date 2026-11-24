using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("OnlineRaisedGoalTargets")]
public sealed class OnlineRaisedGoalTargetsPlayModeTests
{
    private object fixture;

    [UnityTest]
    public IEnumerator GuestKeepsRaisedGoalTargetsWhileHostHovers(
        [Values("header", "shot-preview", "shot", "snapshot", "free-kick-preview", "free-kick", "penalty", "reposition", "walk")] string mode)
        => Run("RunRaisedGoalTargetPublication", mode);

    [UnityTest]
    public IEnumerator GuestShowsAndClearsTheCommittedShootoutTarget()
        => Run("RunCommittedShootoutTargetPublication");

    private IEnumerator Run(string method, params object[] arguments)
    {
        var bridge = AppDomain.CurrentDomain.GetAssemblies().Single(assembly => assembly.GetName().Name == "Assembly-CSharp-Editor")
            .GetType("OnlineFullRoomSceneFixture", true);
        fixture = Activator.CreateInstance(bridge);
        return (IEnumerator)bridge.GetMethod(method).Invoke(fixture, arguments);
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        if (fixture != null) yield return (IEnumerator)fixture.GetType().GetMethod("Cleanup").Invoke(fixture, null);
        fixture = null;
    }
}
