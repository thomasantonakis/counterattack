using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("OnlineGuestShootoutPresentation")]
public sealed class OnlineGuestShootoutPresentationPlayModeTests
{
    private object fixture;

    [UnityTest]
    public IEnumerator GuestPreviewsTheShooterAndSavingGoalkeeper() => Run("portraits");

    [UnityTest]
    public IEnumerator GuestRollPanelKeepsItsDraggedPositionAcrossKicks() => Run("drag");

    private IEnumerator Run(string check)
    {
        var bridge = AppDomain.CurrentDomain.GetAssemblies().Single(assembly => assembly.GetName().Name == "Assembly-CSharp-Editor")
            .GetType("OnlineFullRoomSceneFixture", true);
        fixture = Activator.CreateInstance(bridge);
        return (IEnumerator)bridge.GetMethod("RunGuestShootoutPresentation").Invoke(fixture, new object[] { check });
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        if (fixture != null) yield return (IEnumerator)fixture.GetType().GetMethod("Cleanup").Invoke(fixture, null);
        fixture = null;
    }
}
