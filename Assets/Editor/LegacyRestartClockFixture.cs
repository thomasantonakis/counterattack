using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;

public sealed partial class GoalkeeperBoxEntryFixture
{
    // Legacy 042a/042b: run the real clock update in PlayMode without activating
    // MatchManager.Awake/Start, which would bootstrap a user's saved match.
    public IEnumerator RunRestartClockLegacy(string scenario)
    {
        Board(1);
        string[] parts = scenario.Split('/');
        match.currentState = (MatchManager.GameState)Enum.Parse(typeof(MatchManager.GameState), parts[0]);
        match.freeKickManager.isCornerKick = parts[1] == "corner";
        bool advances = parts[2] == "live";
        match.currentHalf = 1; match.useFastClock = true; match.fastClockMultiplier = 60f;
        match.isClockRunning = true; match.isHalfExpired = false;
        match.extraActionsDetermined = false; match.extraActionsTotal = match.extraActionsRemaining = 0;
        Set(match, "currentHalfRegulationSeconds", 60f);
        Set(match, "isMatchComplete", false); Set(match, "isHalfTimeFlowRunning", false);
        float elapsedClockOpportunity = 0f;
        var waitClock = System.Diagnostics.Stopwatch.StartNew();
        while (elapsedClockOpportunity < 9f)
        {
            Assert.That(waitClock.Elapsed.TotalSeconds, Is.LessThan(8), "Clock update must have a bounded observation horizon.");
            yield return null;
            elapsedClockOpportunity += Time.unscaledDeltaTime * 60f;
            Invoke(match, "UpdateMatchClock");
        }
        float actual = (float)typeof(MatchManager).GetField("currentHalfRegulationSeconds", Private).GetValue(match);
        if (advances)
        {
            Assert.That(actual, Is.GreaterThan(63f), "Live restart phases must advance regulation time.");
            Assert.That(actual, Is.EqualTo(60f + elapsedClockOpportunity).Within(0.01f));
        }
        else Assert.That(actual, Is.EqualTo(60f).Within(0.01f), "Restart choices/preparation must leave regulation time frozen.");
    }
}
