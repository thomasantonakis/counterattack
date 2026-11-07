using System.Collections;
using NUnit.Framework;
using UnityEngine;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunPossessionTime(string scenario, bool homeStarts)
    {
        Board(1);
        match.teamInAttack = homeStarts ? MatchManager.TeamInAttack.Home : MatchManager.TeamInAttack.Away;
        match.homeTeamDirection = MatchManager.TeamAttackingDirection.LeftToRight;
        match.awayTeamDirection = MatchManager.TeamAttackingDirection.RightToLeft;
        match.currentState = MatchManager.GameState.AnyOtherScenario;
        match.gameData.stats = new MatchManager.Stats();
        match.useFastClock = false;
        match.isClockRunning = true;
        match.isHalfExpired = false;
        Set(match, "currentHalfRegulationSeconds", 0f);
        match.CommitToAction(MatchManager.MatchActionKind.StandardPass);
        var starter = match.gameData.stats.GetTeamStats(homeStarts);
        var opponent = match.gameData.stats.GetTeamStats(!homeStarts);

        if (scenario == "turnover")
        {
            Invoke(match, "AdvanceMatchTime", 10f);
            match.ChangePossession();
            match.attackHasPossession = false;
            Invoke(match, "AdvanceMatchTime", 20f);
            Assert.That(starter.totalPossessionSeconds, Is.EqualTo(10f), "The old team stops accumulating at recovery.");
            Assert.That(opponent.totalPossessionSeconds, Is.EqualTo(20f), "Recovery starts the new team's time before its next commitment.");
            match.RecordResolvedAction(MatchManager.MatchActionKind.StandardPass);
            match.CommitToAction(MatchManager.MatchActionKind.MovementPhase);
            Invoke(match, "AdvanceMatchTime", 15f);
            Assert.That(opponent.totalPossessionSeconds, Is.EqualTo(35f), "The next commitment must preserve time already earned after recovery.");
        }
        else if (scenario == "goalKick")
        {
            match.CommitToAction(MatchManager.MatchActionKind.Shot);
            Invoke(match, "AdvanceMatchTime", 10f);
            var outOfBounds = Add<OutOfBoundsManager>();
            outOfBounds.hexGrid = grid;
            outOfBounds.ball = ball;
            outOfBounds.finalThirdManager = match.finalThirdManager;
            outOfBounds.playerTokenManager = match.playerTokenManager;
            int side = homeStarts ? 1 : -1;
            outOfBounds.StartCoroutine(outOfBounds.HandleGoalKickOrCorner(
                Hex(18 * side), side > 0 ? "RightGoalLine" : "LeftGoalLine", "inaccuracy",
                homeStarts ? attacker : defender));
            Assert.That(match.teamInAttack, Is.EqualTo(homeStarts ? MatchManager.TeamInAttack.Away : MatchManager.TeamInAttack.Home));
            Invoke(match, "AdvanceMatchTime", 5f);
            Assert.That(starter.totalPossessionSeconds, Is.EqualTo(10f));
            Assert.That(opponent.totalPossessionSeconds, Is.EqualTo(5f), "Awarding the goal kick switches live time before another action is committed.");
            match.currentState = MatchManager.GameState.GoalKick;
            Invoke(match, "AdvanceMatchTime", 20f);
            Assert.That(opponent.totalPossessionSeconds, Is.EqualTo(5f), "Stopped goal-kick preparation must still exclude time.");
        }
        else if (scenario == "pauses")
        {
            match.SetPauseMenuOpen(true);
            Invoke(match, "AdvanceMatchTime", 10f);
            match.SetPauseMenuOpen(false);
            match.currentState = MatchManager.GameState.GoalKick;
            Invoke(match, "AdvanceMatchTime", 10f);
            match.currentState = MatchManager.GameState.AnyOtherScenario;
            match.PauseMatchClockForSetPiecePrep();
            Invoke(match, "AdvanceMatchTime", 10f);
            Assert.That(starter.totalPossessionSeconds, Is.Zero);
            match.ResumeMatchClockForLivePlay();
            Invoke(match, "AdvanceMatchTime", 2f);
            Assert.That(starter.totalPossessionSeconds, Is.EqualTo(2f));
        }
        else if (scenario == "halfBoundary")
        {
            match.gameData.gameSettings.halfDuration = 15;
            Set(match, "currentHalfRegulationSeconds", 899f);
            match.useFastClock = true; match.fastClockMultiplier = 60f;
            Invoke(match, "AdvanceMatchTime", 1f);
            Assert.That(starter.totalPossessionSeconds, Is.EqualTo(1f), "Only the remaining regulation second counts.");
            Assert.That(match.isHalfExpired, Is.True);
            Invoke(match, "AdvanceMatchTime", 10f);
            Assert.That(starter.totalPossessionSeconds, Is.EqualTo(1f));
            match.RecordResolvedAction(MatchManager.MatchActionKind.StandardPass);
            Assert.That(starter.totalPossessionSeconds, Is.EqualTo(1f), "An action begun in regulation is not an allocated extra action.");
        }
        else if (scenario == "extraActions")
        {
            match.isHalfExpired = true; match.isClockRunning = false;
            match.extraActionsDetermined = true;
            match.extraActionsTotal = match.extraActionsRemaining = 2;
            match.CommitToAction(MatchManager.MatchActionKind.StandardPass);
            match.ChangePossession();
            Invoke(match, "AdvanceMatchTime", 200f);
            Assert.That(starter.totalPossessionSeconds, Is.Zero, "Extra actions do not accumulate wall time.");
            match.RecordResolvedAction(MatchManager.MatchActionKind.StandardPass, false);
            Assert.That(starter.totalPossessionSeconds, Is.Zero, "A continuation must not consume or credit an extra action.");
            match.RecordResolvedAction(MatchManager.MatchActionKind.StandardPass);
            Assert.That(starter.totalPossessionSeconds, Is.EqualTo(60f));
            Assert.That(opponent.totalPossessionSeconds, Is.Zero);
            Assert.That(match.extraActionsRemaining, Is.EqualTo(1));
            match.CommitToAction(MatchManager.MatchActionKind.Snapshot);
            match.RecordSnapshotBallStarted();
            Assert.That(opponent.totalPossessionSeconds, Is.EqualTo(60f));
            Assert.That(match.extraActionsRemaining, Is.Zero);
            match.RecordResolvedAction(MatchManager.MatchActionKind.None);
            Assert.That(opponent.totalPossessionSeconds, Is.EqualTo(60f));
        }
        else if (scenario == "realClock")
        {
            match.useFastClock = true; match.fastClockMultiplier = 60f;
            yield return null;
            float expected = Time.unscaledDeltaTime * 60f;
            Invoke(match, "UpdateMatchClock");
            Assert.That(starter.totalPossessionSeconds, Is.EqualTo(expected).Within(.001f));
            Assert.That(opponent.totalPossessionSeconds, Is.Zero);
        }
        else Assert.Fail("Unknown possession scenario: " + scenario);
    }
}
