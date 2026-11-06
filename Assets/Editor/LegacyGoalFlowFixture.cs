using System.Collections;
using NUnit.Framework;

public sealed partial class GoalkeeperBoxEntryFixture
{
    private IEnumerator AttachLegacyGoalFlow()
    {
        var statsHost = Child("Inactive scorer display dependency");
        match.matchStatsUI = statsHost.AddComponent<MatchStatsUI>(); match.matchStatsUI.enabled = false;
        var goal = Add<GoalFlowManager>(); var kickoff = Add<KickoffManager>();
        match.goalFlowManager = goal; match.kickoffManager = kickoff; input.kickoffManager = kickoff;
        goal.hexGrid = grid; goal.playerTokenManager = match.playerTokenManager; goal.movementPhaseManager = movement;
        goal.groundBallManager = ground; goal.longBallManager = longBall; goal.kickoffManager = kickoff;
        kickoff.hexGrid = grid; kickoff.playerTokenManager = match.playerTokenManager; kickoff.ball = ball;
        kickoff.freeKickManager = match.freeKickManager; kickoff.movementPhaseManager = movement;
        kickoff.groundBallManager = ground; kickoff.headerManager = match.shotManager.headerManager;
        movement.goalFlowManager = goal; loose.goalFlowManager = goal; match.shotManager.goalFlowManager = goal;
        match.shotManager.headerManager.goalFlowManager = goal;
        Set(grid, "gridInitialized", true);
        goal.StartCoroutine((IEnumerator)Invoke(goal, "WaitUntilHexGridIsReady"));
        yield return Until(() => typeof(GoalFlowManager).GetField("resetFormationLeft", Private).GetValue(goal) != null,
            "normal goal-flow initialization resolves the constructed board's formation and celebration hexes");
    }

    private IEnumerator WaitForLegacyGoalReset()
    {
        var clock = System.Diagnostics.Stopwatch.StartNew();
        while (match.goalFlowManager.isActivated)
        {
            Assert.That(clock.Elapsed.TotalSeconds, Is.LessThan(60), "Actual goal celebration and formation reset must complete within one active minute.");
            yield return null;
        }
    }
}
