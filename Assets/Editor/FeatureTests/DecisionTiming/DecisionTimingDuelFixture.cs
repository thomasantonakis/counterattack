using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public sealed partial class GoalkeeperBoxEntryFixture
{
    private bool timingManualReview;
    private bool forceRepositionMovementGuard;

    public IEnumerator RunTimingDuelForfeitDuringMovementGuard()
    {
        forceRepositionMovementGuard = true;
        try { yield return RunTimingDuelInterruption(1, false, false, false, true); }
        finally { forceRepositionMovementGuard = false; }
    }

    public IEnumerator RunTimingDuelReview(int side, bool expired)
    {
        timingManualReview = true;
        yield return RunTimingDuelInterruption(side, false, expired, false, false);
    }
    public IEnumerator RunTimingDuelInterruption(int side, bool nutmeg, bool expired, bool decline, bool defenderWins)
    {
        Board(side);
        Place(attacker, Hex(0)); Place(defender, Hex(side));
        ball.PlaceAtCell(attacker.currentHex); match.SetLastToken(attacker);
        match.isClockRunning = true;
        AttachAuthoredPanels(); EnableForAuthoredPresentation(movement); EnableForAuthoredPresentation(loose);
        if (timingManualReview) duelPanel.resultSeconds = .1f;
        movement.ActivateMovementPhase();
        movement.CommitToAction();
        if (!nutmeg) movement.ForfeitTeamMovementPhase();
        match.RefreshDecisionTiming();
        var parent = match.DecisionTiming.Current;
        Assert.That(parent.kind, Is.EqualTo(nutmeg ? DecisionKind.AttackingMovement : DecisionKind.DefensiveMovement));
        Assert.That(parent.owner, Is.EqualTo(nutmeg ? "Home" : "Away"));
        match.AdvanceDecisionTiming(expired ? parent.remaining : 7);
        float remaining = parent.remaining;
        string parentId = parent.id;
        var actor = nutmeg ? attacker : defender;
        Assert.That(input.TryExecuteSyntheticHexClick(actor.currentHex, "Timing duel", out var error), Is.True, error);
        Assert.That(nutmeg ? movement.isWaitingForNutmegDecisionWithoutMoving : movement.isWaitingForTackleDecisionWithoutMoving, Is.True);
        match.RefreshDecisionTiming();
        var choice = match.DecisionTiming.Current;
        Assert.That(choice.kind, Is.EqualTo(nutmeg ? DecisionKind.StandaloneNutmegDecision : DecisionKind.StandaloneTackleOrStand));
        Assert.That(choice.id, Is.Not.EqualTo(parentId));
        Assert.That(choice.owner, Is.EqualTo(parent.owner));
        float choiceBefore = choice.remaining;
        match.AdvanceDecisionTiming(3);
        remaining = Mathf.Max(0, remaining - 3);
        Assert.That(parent.remaining, Is.EqualTo(remaining));
        Assert.That(choice.remaining, Is.EqualTo(choiceBefore - 3), "The choice and the deciding team phase both consume time.");
        if (decline)
        {
            // Choose a legal empty destination instead of challenging in place.
            var destination = actor.currentHex.GetNeighbors(grid).First(h => h != null && !h.isOutOfBounds
                && h.GetOccupyingToken() == null && !h.GetNeighbors(grid).Contains((nutmeg ? defender : attacker).currentHex));
            Assert.That(input.TryExecuteSyntheticHexClick(destination, "Timing decline", out error), Is.True, error);
            yield return Until(() => !movement.isPlayerMoving && actor.currentHex == destination, "declined challenge finishes its real move");
            match.RefreshDecisionTiming();
            Assert.That(match.DecisionTiming.Capture().decisions.Single(d => d.id == parentId).remaining, Is.EqualTo(remaining));
            Assert.That(movement.isWaitingForTackleRoll, Is.False);
            yield break;
        }
        LegacyKey(nutmeg ? "N" : "T");
        Assert.That(movement.isWaitingForTackleRoll, Is.True);
        match.AdvanceDecisionTiming(2);
        if (match.DecisionTiming.RollOwner == parent.owner) remaining = Mathf.Max(0, remaining - 2);
        Assert.That(parent.remaining, Is.EqualTo(remaining), "Roll waits charge only an existing allowance belonging to the roll owner.");
        movement.PerformTackleDiceRoll(true, new RollInputOverride { hasOverride = true, roll = defenderWins ? 6 : 2, isJackpot = false });
        yield return AssertLegacyOutfieldDefenderPresentation();
        match.RefreshDecisionTiming();
        match.AdvanceDecisionTiming(1);
        if (match.DecisionTiming.RollOwner == parent.owner) remaining = Mathf.Max(0, remaining - 1);
        Assert.That(parent.remaining, Is.EqualTo(remaining));
        movement.PerformTackleDiceRoll(false, new RollInputOverride { hasOverride = true, roll = defenderWins ? 2 : 6, isJackpot = false });
        if (timingManualReview)
        {
            yield return Until(() => duelPanel.IsComplete, "duel result ready for manual hold");
            LegacyKey("Return");
            Assert.That(duelPanel.IsResultHeld, Is.True);
            yield return new WaitForSecondsRealtime(.25f);
            float possession = match.gameData.stats.homeTeamStats.totalPossessionSeconds;
            match.AdvanceDecisionTiming(100);
            Invoke(match, "AdvanceMatchTime", 10f);
            Assert.That(duelPanel.IsOpen, Is.True);
            Assert.That(movement.isWaitingForReposition, Is.False);
            Assert.That(parent.remaining, Is.EqualTo(remaining));
            Assert.That(match.DecisionTiming.BlockingResult, Is.True);
            Assert.That(match.gameData.stats.homeTeamStats.totalPossessionSeconds, Is.EqualTo(possession));
            LegacyKey("Return");
            Assert.That(duelPanel.IsResultHeld, Is.False);
        }
        yield return DismissLegacyDuel(false);
        yield return Until(() => movement.isWaitingForReposition, "attacker wins and receives reposition input");
        match.RefreshDecisionTiming();
        var child = match.DecisionTiming.Current;
        Assert.That(child, Is.Not.Null);
        Assert.That(child.kind, Is.EqualTo(nutmeg ? DecisionKind.NutmegReposition : DecisionKind.TackleReposition));
        Assert.That(child.owner, Is.EqualTo(defenderWins ? "Away" : "Home"));
        Assert.That(match.DecisionTiming.Capture().decisions.Any(d => d.id == parentId && !d.inactive), Is.True,
            "Possession changing inside a duel must not replace the unfinished movement phase's owner or allowance.");
        Assert.That(match.DecisionTiming.HoldsMatchClock, Is.EqualTo(expired));
        Assert.That(child.id, Is.Not.EqualTo(parentId));
        match.AdvanceDecisionTiming(child.remaining);
        Assert.That(parent.remaining, Is.EqualTo(remaining), "Separately timed reposition must not charge its parent.");
        Assert.That(match.DecisionTiming.HoldsMatchClock, Is.True);
        if (nutmeg && !defenderWins)
        {
            var destinations = movement.repositionHexes;
            Assert.That(destinations, Is.Not.Empty);
            Assert.That(input.TryExecuteSyntheticHexClick(destinations[0], "Timing nutmeg reposition", out error), Is.True, error);
        }
        else if (forceRepositionMovementGuard)
        {
            movement.isPlayerMoving = true;
            try { LegacyKey("X"); }
            finally { movement.isPlayerMoving = false; }
        }
        else LegacyKey("X");
        yield return Until(() => !movement.isWaitingForReposition && !movement.IsResolvingReposition && !movement.isPlayerMoving, "legal reposition remains available after expiry");
        match.RefreshDecisionTiming();
        if (defenderWins)
        {
            yield return Until(() => !movement.isActivated && match.currentState == MatchManager.GameState.SuccessfulTackle, "turnover completes the movement phase");
            match.RefreshDecisionTiming();
            Assert.That(match.DecisionTiming.HoldsMatchClock, Is.False);
            Assert.That(match.teamInAttack, Is.EqualTo(MatchManager.TeamInAttack.Away));
            Assert.That(match.LastTokenToTouchTheBallOnPurpose, Is.SameAs(defender));
            movement.ActivateMovementPhase(); movement.CommitToAction(); match.RefreshDecisionTiming();
            Assert.That(match.DecisionTiming.Current.owner, Is.EqualTo("Away"));
            Assert.That(match.DecisionTiming.Current.id, Is.Not.EqualTo(parentId));
            Assert.That(match.DecisionTiming.Current.expired, Is.False);
            yield break;
        }
        var restored = match.DecisionTiming.Capture().decisions.Single(d => d.id == parentId);
        Assert.That(restored.remaining, Is.EqualTo(remaining));
        Assert.That(restored.expired, Is.EqualTo(expired));
        Assert.That(restored.inactive, Is.False, "The team phase continues after the interruption.");
        Assert.That(match.DecisionTiming.HoldsMatchClock, Is.EqualTo(expired), "Child expiry clears; expired parent stays latched.");
        Assert.That(match.teamInAttack, Is.EqualTo(MatchManager.TeamInAttack.Home));
        if (nutmeg) Assert.That(match.gameData.stats.GetPlayerStats(attacker).dribblesMade, Is.EqualTo(1));
    }
}
