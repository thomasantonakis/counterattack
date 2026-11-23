using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("OnlineSharedSubstitutionReview")]
public sealed class OnlineSharedSubstitutionReviewPlayModeTests
{
    private object fixture;
    private static Type Bridge => AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Assembly-CSharp-Editor")
        .GetType("OnlineFullRoomSceneFixture", true);

    [SetUp]
    public void Setup() => fixture = Activator.CreateInstance(Bridge);

    [UnityTest]
    public IEnumerator BothManagersSeeAndConfirmOrders([Values("solo", "both", "cancel")] string scenario)
        => (IEnumerator)Bridge.GetMethod("RunNativeSubstitutionReview").Invoke(fixture, new object[] { scenario });

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        if (fixture != null) yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture, null);
        fixture = null;
    }
}
