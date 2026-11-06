using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

public sealed class LegacyNutmegSelectionPlayModeTests
{
    private object board;
    private static Type Bridge => AppDomain.CurrentDomain.GetAssemblies()
        .Single(a => a.GetName().Name == "Assembly-CSharp-Editor").GetType("GoalkeeperBoxEntryFixture", true);
    public static IEnumerable<string> Scenarios => new[] { "reject", "select",
        "stationary/1/key", "stationary/1/click", "stationary/2/key", "stationary/2/click", "stationary/3/key", "stationary/3/click" };
    [SetUp] public void SetUp() => board = Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator TearDown()
    {
        if (board != null) yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(board, null);
        board = null;
    }
    [UnityTest]
    public IEnumerator NutmegSelection([ValueSource(nameof(Scenarios))] string scenario)
        => (IEnumerator)Bridge.GetMethod("RunNutmegSelectionLegacy").Invoke(board, new object[] { scenario });
    [UnityTest]
    public IEnumerator NutmegStealFoulContinuation([Values("miss", "foul", "injury", "post", "recover", "red")] string scenario)
        => (IEnumerator)Bridge.GetMethod("RunNutmegFoulLegacy").Invoke(board, new object[] { scenario });
    [UnityTest]
    public IEnumerator MixedAdjacentDefendersOnlyOfferNutmegsWithLandingSpace()
        => (IEnumerator)Bridge.GetMethod("RunMixedNutmegLegacy").Invoke(board, null);
}
