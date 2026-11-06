using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

public sealed class LegacyLongBallOobPlayModeTests
{
    private object board;
    private static Type Bridge => AppDomain.CurrentDomain.GetAssemblies()
        .Single(a => a.GetName().Name == "Assembly-CSharp-Editor").GetType("GoalkeeperBoxEntryFixture", true);
    public static IEnumerable<string> Scenarios => new[] { "prepared_ne3", "prepared_s3", "prepared_se6", "left_s_throw", "left_sw_corner", "left_box_nw_corner", "left_upper_nw_corner", "right_s_throw", "right_se_goal" };
    [SetUp] public void SetUp() => board = Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator TearDown()
    {
        if (board != null) yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(board, null);
        board = null;
    }
    [UnityTest]
    public IEnumerator LongBallOutOfBounds([ValueSource(nameof(Scenarios))] string scenario)
        => (IEnumerator)Bridge.GetMethod("RunLongBallOobLegacy").Invoke(board, new object[] { scenario });
}
