using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("OnlineFullTimeReport")]
public sealed class OnlineFullTimeReportPlayModeTests
{
    private object fixture;

    [UnityTest]
    public IEnumerator AuthoredReportShowsTheFinalScore([Values("draw", "extra-time", "penalties")] string ending)
        => Run("RunNativeFullTimeReport", ending);

    [UnityTest]
    public IEnumerator ResumedMatchShowsInheritedHistoryAndStatsAtFullTime()
        => Run("RunCheckpointPause", true, false, true, true);

    [UnityTest]
    public IEnumerator MissingHistoryLeavesTheFinalScoreAndPlayersUsable()
        => Run("RunCheckpointPause", false, false, true, false);

    private IEnumerator Run(string method, params object[] arguments)
    {
        var bridge = AppDomain.CurrentDomain.GetAssemblies()
            .Single(assembly => assembly.GetName().Name == "Assembly-CSharp-Editor")
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
