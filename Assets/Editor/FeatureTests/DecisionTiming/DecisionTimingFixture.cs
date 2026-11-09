using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunDecisionTimingPass(int side, string action)
    {
        Board(side);
        match.difficulty_level = 3;
        match.isClockRunning = true;
        Place(attacker, Hex(0)); ball.PlaceAtCell(attacker.currentHex);
        Place(defender, Hex(-10 * side, 7));
        match.LastTokenToTouchTheBallOnPurpose = attacker;
        match.currentState = action == "F" ? MatchManager.GameState.EndOfStandardPass : MatchManager.GameState.EndOfMovementPhase;
        match.RefreshAvailableActionsForCurrentState();
        Subscribe(ground); Subscribe(high); Subscribe(longBall); Subscribe(ftp);
        LegacyKey(action);
        match.RefreshDecisionTiming();
        var state = match.DecisionTiming.Current;
        Assert.That(state, Is.Not.Null, action);
        var expected = action == "P" ? DecisionKind.StandardPassTarget : action == "C" ? DecisionKind.HighPassTarget
            : action == "L" ? DecisionKind.LongBallTarget : DecisionKind.FirstTimePassTarget;
        Assert.That(state.kind, Is.EqualTo(expected));
        Assert.That(state.owner, Is.EqualTo("Home"));
        match.AdvanceDecisionTiming(3);
        string id = state.id;
        // An invalid off-pitch click exercises the real target validation path.
        var manager = action == "P" ? (object)ground : action == "C" ? high : action == "L" ? longBall : ftp;
        var outOfBounds = grid.cells.Cast<HexCell>().First(h => h != null && h.isOutOfBounds);
        Invoke(manager, "OnClickReceived", null, outOfBounds);
        match.RefreshDecisionTiming();
        Assert.That(match.DecisionTiming.Current.id, Is.EqualTo(id));
        Assert.That(match.DecisionTiming.Current.remaining, Is.EqualTo(state.remaining));
        match.useFastClock = true; match.fastClockMultiplier = 60;
        match.isClockRunning = true;
        match.AdvanceDecisionTiming(state.remaining);
        float possession = match.gameData.stats.homeTeamStats.totalPossessionSeconds;
        Invoke(match, "AdvanceMatchTime", 10f);
        Assert.That(match.gameData.stats.homeTeamStats.totalPossessionSeconds, Is.EqualTo(possession));
        Assert.That(match.DecisionTiming.HoldsMatchClock, Is.True);
        Assert.That(match.IsGameplayInputBlocked, Is.False);
        yield break;
    }

    public IEnumerator RunDecisionTimingMovement(int side)
    {
        Board(side); Place(attacker, Hex(0)); ball.PlaceAtCell(attacker.currentHex);
        match.isClockRunning = true;
        movement.ActivateMovementPhase();
        movement.CommitToAction();
        match.RefreshDecisionTiming();
        var phase = match.DecisionTiming.Current;
        Assert.That(phase.kind, Is.EqualTo(DecisionKind.AttackingMovement));
        match.AdvanceDecisionTiming(phase.remaining);
        // Real token selection must leave an expired whole-team phase latched.
        Invoke(movement, "OnClickReceived", attacker, attacker.currentHex);
        match.RefreshDecisionTiming();
        Assert.That(match.DecisionTiming.Current.id, Is.EqualTo(phase.id));
        Assert.That(match.DecisionTiming.HoldsMatchClock, Is.True);
        movement.ForfeitTeamMovementPhase();
        match.RefreshDecisionTiming();
        Assert.That(match.DecisionTiming.Current.kind, Is.EqualTo(DecisionKind.DefensiveMovement));
        Assert.That(match.DecisionTiming.HoldsMatchClock, Is.False);
        yield break;
    }

    public IEnumerator RunDecisionTimingAutomaticStoppage()
    {
        Board(1); Subscribe(match);
        Invoke(match, "EnterExtraActionsRollPrompt");
        match.RefreshDecisionTiming();
        Assert.That(match.DecisionTiming.HasPendingRoll, Is.True);
        Assert.That(match.DecisionTiming.HoldsMatchClock, Is.False);
        match.AdvanceDecisionTiming(4);
        Assert.That(match.IsWaitingForExtraActionsRoll, Is.True);
        match.SetPauseMenuOpen(true); match.AdvanceDecisionTiming(100);
        Assert.That(match.IsWaitingForExtraActionsRoll, Is.True);
        match.SetPauseMenuOpen(false); match.AdvanceDecisionTiming(1);
        Assert.That(match.IsWaitingForExtraActionsRoll, Is.False);
        Assert.That(match.TimingRollSequence, Is.EqualTo(1));
        match.AdvanceDecisionTiming(10);
        Assert.That(match.TimingRollSequence, Is.EqualTo(1));
        yield break;
    }
    public IEnumerator RunDecisionTimingRestart(int side, string restart)
    {
        Board(side); Place(attacker, Hex(0)); ball.PlaceAtCell(attacker.currentHex);

        if (restart == "free" || restart == "corner")
        {
            match.freeKickManager.StartFreeKickPreparation(restart == "corner" ? Hex(18 * side, 12) : null);

        }
        else if (restart == "penalty")
        {
            var penalty = Add<PenaltyKickManager>(); match.penaltyKickManager = penalty;
            penalty.Configure(match, grid, ball, movement, match.shotManager); penalty.StartPenaltyPreparation();

        }
        else
        {
            var throwIn = Add<ThrowInManager>(); throwIn.ball = ball; throwIn.hexGrid = grid;
            throwIn.StartThrowInPreparation(Hex(0, 12), MatchManager.TeamInAttack.Home, false);

        }
        match.RefreshDecisionTiming();
        Assert.That(match.DecisionTiming.GameClockStopped, Is.True, restart);
        Assert.That(match.DecisionTiming.Current, Is.Null, "Built-in restart stoppages need no allowance.");
        match.AdvanceDecisionTiming(1000);
        Assert.That(match.DecisionTiming.Capture().decisions, Is.Empty);
        Assert.That(match.IsGameplayInputBlocked, Is.False);
        yield break;
    }
    public IEnumerator RunDecisionTimingAccuracy(int side, bool automatic)
    {
        Board(side); Place(attacker, Hex(0)); ball.PlaceAtCell(attacker.currentHex);
        Place(defender, Hex(-10 * side, 8));
        match.isClockRunning = true; match.difficulty_level = 3;
        attacker.highPass = 10;
        AttachAuthoredPanels(); EnableForAuthoredPresentation(longBall);
        match.currentState = MatchManager.GameState.EndOfMovementPhase;
        match.RefreshAvailableActionsForCurrentState();
        LegacyKey("L");
        Invoke(longBall, "OnClickReceived", null, Hex(8 * side, 3));
        yield return Until(() => longBall.isWaitingForAccuracyRoll, "actual long-ball target reaches accuracy roll");
        match.RefreshDecisionTiming();
        Assert.That(match.DecisionTiming.HasPendingRoll, Is.True);
        Assert.That(match.DecisionTiming.HoldsMatchClock, Is.False);
        string id = match.DecisionTiming.RollId;
        // Rig the existing dice source without replacing rule evaluation or continuation.
        int seed = 1;
        for (; seed < 100; seed++)
        {
            Random.InitState(seed);
            if (helper.DiceRoll() == (2, false)) break;
        }
        Assert.That(seed, Is.LessThan(100)); Random.InitState(seed);
        if (automatic) match.AdvanceDecisionTiming(DecisionTimingConfig.AutomaticRollSeconds);
        else LegacyKey("R");
        Assert.That(match.DecisionTiming.SubmitRoll(id, "Home"), Is.False, "The old roll cannot be submitted twice.");
        Assert.That(match.TimingRollSequence, Is.EqualTo(1));
        Assert.That(match.DecisionTiming.BlockingResult, Is.False, "ordinary results keep live time running until Enter hold");
        float possessionBeforeReview = match.gameData.stats.homeTeamStats.totalPossessionSeconds;
        Invoke(match, "AdvanceMatchTime", 3f);
        Assert.That(match.gameData.stats.homeTeamStats.totalPossessionSeconds, Is.GreaterThan(possessionBeforeReview));
        yield return Until(() => !longBall.isWaitingForAccuracyRoll, "accuracy continuation");
        Assert.That(longBall.isWaitingForDirectionRoll, Is.False, "The rigged successful accuracy must not scatter.");
    }
    public IEnumerator RunAdditionalTimingDecision(int side, string family)
    {
        Board(side); match.isClockRunning = true;
        Place(attacker, Hex(0)); ball.PlaceAtCell(attacker.currentHex);
        Place(defender, Hex(-10 * side, 8));
        DecisionKind expected;
        bool home = true;
        if (family == "ftp" || family == "high")
        {
            var receiver = Token("Timing receiver", true, false, 10); Place(receiver, Hex(4 * side));
            match.currentState = family == "ftp" ? MatchManager.GameState.EndOfStandardPass : MatchManager.GameState.EndOfMovementPhase;
            match.RefreshAvailableActionsForCurrentState(); Subscribe(ftp); Subscribe(high);
            LegacyKey(family == "ftp" ? "F" : "C");
            var manager = family == "ftp" ? (object)ftp : high;
            Invoke(manager, "OnClickReceived", receiver, receiver.currentHex);
            Invoke(manager, "OnClickReceived", receiver, receiver.currentHex);
            expected = family == "ftp" ? DecisionKind.FirstTimePassAttackerMovement : DecisionKind.HighPassAttackerMovement;
            match.RefreshDecisionTiming(); Assert.That(match.DecisionTiming.Current.kind, Is.EqualTo(expected));
            match.AdvanceDecisionTiming(4);
            LegacyKey("X");
            yield return Until(() => family == "ftp" ? ftp.isWaitingForDefenderSelection : high.isWaitingForDefenderSelection, "defender gets pass movement");
            expected = family == "ftp" ? DecisionKind.FirstTimePassDefenderMovement : DecisionKind.HighPassDefenderMovement;
            home = false;
        }
        else if (family == "keeper")
        {
            Assert.That(keeper.ShouldGKMoveForPenaltyBox(Hex(14 * side).isInPenaltyBox, Hex(14 * side)), Is.True);
            expected = DecisionKind.DefensiveBoxMovement; home = false;
        }
        else if (family == "header")
        {
            ball.PlaceAtCell(Hex(2 * side));
            yield return match.shotManager.headerManager.FindEligibleHeaderTokens(ball.GetCurrentHex());
            expected = DecisionKind.HeaderOrBallControl;
        }
        else if (family == "shot")
        {
            Invoke(grid, "LoadPrecomputedShootingPaths"); Place(attacker, Hex(8 * side)); ball.PlaceAtCell(attacker.currentHex);
            match.shotManager.StartShotProcess(attacker, "snapshot");
            yield return Until(() => match.shotManager.isWaitingforBlockerSelection, "snapshot defender input");
            expected = DecisionKind.SnapshotDefenderMovement; home = false;
        }
        else
        {
            Place(attacker, Hex(14 * side)); ball.PlaceAtCell(attacker.currentHex);
            match.finalThirdManager.TriggerFinalThirdPhase();
            expected = DecisionKind.FinalThirdAttackingMovement;
        }
        match.RefreshDecisionTiming();
        Assert.That(match.DecisionTiming.Current, Is.Not.Null, family);
        Assert.That(match.DecisionTiming.Current.kind, Is.EqualTo(expected));
        Assert.That(match.DecisionTiming.Current.owner, Is.EqualTo(home ? "Home" : "Away"));
        match.AdvanceDecisionTiming(match.DecisionTiming.Current.remaining);
        Assert.That(match.DecisionTiming.HoldsMatchClock, Is.True);
        Assert.That(match.IsGameplayInputBlocked, Is.False);
        if (family == "ftp" || family == "high" || family == "keeper")
        {
            LegacyKey("X");
            match.RefreshDecisionTiming();
            Assert.That(match.DecisionTiming.Current?.kind, Is.Not.EqualTo(expected), "expiry must still allow the real forfeit continuation");
        }
    }
}
