using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("KitDropdownHeight")]
public sealed class KitDropdownHeightPlayModeTests
{
    private object fixture;
    private static Type Bridge => AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Assembly-CSharp-Editor").GetType("KitDropdownHeightFixture", true);
    [SetUp] public void SetUp() => fixture = Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator Cleanup() => (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture, null);
    [UnityTest] public IEnumerator BothSetupModesKeepTheSameAuthoredScrollWindow(
        [Values("Single Player", "Hot Seat")] string mode, [Values("home", "away", "homegk", "awaygk", "international")] string kind) =>
        (IEnumerator)Bridge.GetMethod("Verify").Invoke(fixture, new object[] { mode, kind });
}
