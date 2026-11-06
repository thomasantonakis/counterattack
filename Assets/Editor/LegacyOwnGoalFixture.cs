using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunLooseOwnGoalLegacy()
    {
        var players = LoadLegacyRoster(); var scorer = players["Cafferata"];
        // AddGoal updates its scoreboard dependency. Keep this component on an
        // inactive owned host so no UI Awake/Start creates or binds visuals.
        var statsHost = Child("Inactive scorer display dependency");
        match.matchStatsUI = statsHost.AddComponent<MatchStatsUI>();
        match.matchStatsUI.enabled = false;
        var goal = Add<GoalFlowManager>(); var kickoff = Add<KickoffManager>();
        match.goalFlowManager = goal; match.kickoffManager = kickoff;
        goal.hexGrid = grid; goal.playerTokenManager = match.playerTokenManager; goal.movementPhaseManager = movement;
        goal.groundBallManager = ground; goal.longBallManager = longBall; goal.kickoffManager = kickoff;
        kickoff.hexGrid = grid; kickoff.playerTokenManager = match.playerTokenManager; kickoff.ball = ball;
        kickoff.freeKickManager = match.freeKickManager; kickoff.movementPhaseManager = movement;
        kickoff.groundBallManager = ground; kickoff.headerManager = match.shotManager.headerManager;
        loose.goalFlowManager = goal;
        // All cells are built already; publish board readiness to the normal
        // GoalFlow initializer, without starting Room or loading a match save.
        Set(grid, "gridInitialized", true);
        goal.StartCoroutine((IEnumerator)Invoke(goal, "WaitUntilHexGridIsReady"));
        yield return Until(() => typeof(GoalFlowManager).GetField("resetFormationLeft", Private).GetValue(goal) != null,
            "normal goal-flow initialization resolves celebration and formation hexes");
        var ownGoalHex = Hex(-19);
        match.ClearLastTokenChain(); match.SetLastToken(scorer); ball.PlaceAtCell(ownGoalHex);
        int home = match.gameData.stats.homeTeamStats.totalGoals, away = match.gameData.stats.awayTeamStats.totalGoals;
        int playerGoals = match.gameData.stats.GetPlayerStats(scorer).goals;
        Assert.That((bool)Invoke(loose, "TryHandleLooseBallGoal", ownGoalHex, scorer), Is.True);
        Assert.That(goal.isActivated, Is.True);
        var waitClock = System.Diagnostics.Stopwatch.StartNew();
        while (goal.isActivated)
        {
            Assert.That(waitClock.Elapsed.TotalSeconds, Is.LessThan(60), "Own-goal celebration/reset must complete within one minute.");
            yield return null;
        }
        Assert.That(match.gameData.stats.homeTeamStats.totalGoals, Is.EqualTo(home));
        Assert.That(match.gameData.stats.awayTeamStats.totalGoals, Is.EqualTo(away + 1));
        Assert.That(match.gameData.stats.GetPlayerStats(scorer).goals, Is.EqualTo(playerGoals));
        Assert.That(match.awayScorers.Any(g => g != null && g.scorer.Contains("(OG)")), Is.True);
        Assert.That(match.teamInAttack, Is.EqualTo(MatchManager.TeamInAttack.Home));
        Assert.That(match.currentState, Is.EqualTo(MatchManager.GameState.PostGoalKickOffSetup));
    }
}
