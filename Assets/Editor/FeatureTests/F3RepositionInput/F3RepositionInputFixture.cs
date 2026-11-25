using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunRecoveryF3Ownership(int side, bool homeRecovers)
    {
        Board(side);
        if (homeRecovers) match.ChangePossession();
        var carrier = homeRecovers ? defender : attacker;
        var recovering = homeRecovers ? attacker : defender;
        Place(homeGK, Hex(0, -10)); Place(awayGK, Hex(0, 11));
        Place(carrier, Hex(12 * side, 7)); Place(recovering, Hex(12 * side, 9));
        var homeMover = Token("Home F3 mover", true, false, 3);
        var awayMover = Token("Away F3 mover", false, false, 3);
        // New fixture players inherit the actual possession side.
        homeMover.isAttacker = match.teamInAttack == MatchManager.TeamInAttack.Home;
        awayMover.isAttacker = !homeMover.isAttacker;
        Place(homeMover, Hex(-12 * side, 8)); Place(awayMover, Hex(-12 * side, -8));
        ball.PlaceAtCell(carrier.currentHex); match.SetLastToken(carrier);
        Subscribe(movement); Subscribe(ground); Subscribe(longBall); Subscribe(high); Subscribe(ftp); Subscribe(match.finalThirdManager);
        match.TriggerMovement();
        Assert.That(input.TryExecuteSyntheticHexClick(carrier.currentHex, "Recovery F3", out var error), Is.True, error);
        input.TryExecuteSyntheticHexClick(Hex(12 * side, 8), "Recovery F3", out _);
        yield return Until(() => (movement.isWaitingForNutmegDecision || movement.isWaitingForInterceptionDiceRoll) && !movement.isPlayerMoving, "dribble reaches its contest decision");
        if (movement.isWaitingForNutmegDecision) input.TryExecuteSyntheticKeyPress("X", "Decline nutmeg and allow steal", out _);
        yield return Until(() => movement.isWaitingForInterceptionDiceRoll, "dribble creates a real steal attempt");
        movement.StartCoroutine(movement.PerformBallInterceptionDiceRoll(6));
        yield return Until(() => match.finalThirdManager.isActivated && !movement.isActivated, "successful recovery reaches opposite F3");
        Assert.That(match.teamInAttack, Is.EqualTo(homeRecovers ? MatchManager.TeamInAttack.Home : MatchManager.TeamInAttack.Away));
        Assert.That(ball.GetCurrentHex(), Is.SameAs(recovering.currentHex));
        Assert.That(match.gameData.stats.GetPlayerStats(recovering).possessionWon, Is.EqualTo(1));
        AssertOrdinaryActionsUnavailable();
        match.RefreshAvailableActionsForCurrentState();
        AssertOrdinaryActionsUnavailable();
        foreach (string key in new[] { "P", "M", "L", "C", "F" })
            Assert.That(input.TryExecuteSyntheticKeyPress(key, "F3 owns input", out _), Is.False, key);
        // UI/AI commands use these entry points directly rather than hotkeys.
        match.TriggerStandardPass(); match.TriggerMovement(); match.TriggerHighPass();
        match.TriggerLongPass(); match.TriggerFTP(); match.TriggerQuickThrowPass();
        match.TriggerGoalkeeperKick();
        Assert.That(ground.isActivated || movement.isActivated || longBall.isActivated || high.isActivated || ftp.isActivated, Is.False);
        var mover = homeRecovers ? homeMover : awayMover;
        input.TryExecuteSyntheticHexClick(mover.currentHex, "F3 select", out _);
        Assert.That(grid.highlightedHexes, Is.Not.Empty);
        var targets = grid.highlightedHexes.ToArray();
        Invoke(ground, "OnHoverReceived", null, targets.First());
        Assert.That(grid.highlightedHexes, Is.EquivalentTo(targets));
        input.TryExecuteSyntheticHexClick(targets.First(), "F3 move", out _);
        yield return Until(() => !mover.IsMoving && mover.currentHex == targets.First(), "F3 move completes through its actual click handler");
        Assert.That(ball.GetCurrentHex(), Is.SameAs(recovering.currentHex));
        yield return ForfeitLegacyFinalThirds();
        Assert.That(movement.isAvailable && ground.isAvailable && longBall.isAvailable, Is.True);
        Assert.That(ground.imposedDistance, Is.EqualTo(6));
        Assert.That(input.TryExecuteSyntheticKeyPress("P", "After F3", out error), Is.True, error);
        var passTarget = Hex(12 * side, 10); // Clear of the former carrier's interception zone.
        var moverHex = mover.currentHex;
        input.TryExecuteSyntheticHexClick(passTarget, "Post-F3 pass preview", out _);
        input.TryExecuteSyntheticHexClick(passTarget, "Post-F3 pass confirm", out _);
        yield return Until(() => !ground.isActivated && ball.GetCurrentHex() == passTarget, "normal pass resumes after F3");
        Assert.That(mover.currentHex, Is.SameAs(moverHex), "Pass confirmation must never move the previously selected F3 player.");
    }

    private void AssertOrdinaryActionsUnavailable()
    {
        Assert.That(movement.isAvailable || ground.isAvailable || ftp.isAvailable || high.isAvailable || longBall.isAvailable || match.shotManager.isAvailable,
            Is.False, "F3 must reserve all ordinary attacking action choices.");
    }

    public IEnumerator RunRecoveryWithoutFinalThird(int side, bool emptyOppositeThird)
    {
        Board(side);
        Place(homeGK, Hex(0, -10)); Place(awayGK, Hex(0, 11)); Place(defender, Hex(0, 8));
        Place(attacker, emptyOppositeThird ? Hex(12 * side, 9) : Hex(0));
        ball.PlaceAtCell(attacker.currentHex); match.SetLastToken(attacker);
        Subscribe(ground);
        match.BroadcastDefensiveRecoveryOutcome(attacker, attacker.currentHex);
        Assert.That(match.finalThirdManager.isActivated, Is.False);
        Assert.That(movement.isAvailable && ground.isAvailable && longBall.isAvailable, Is.True);
        Assert.That(ground.imposedDistance, Is.EqualTo(6));
        Assert.That(input.TryExecuteSyntheticKeyPress("P", "No F3 continuation", out var error), Is.True, error);
        Assert.That(ground.isAwaitingTargetSelection, Is.True);
        yield break;
    }

    public IEnumerator RunStalePassF3Ownership(int side)
    {
        Board(side);
        Place(attacker, Hex(12 * side, 8)); ball.PlaceAtCell(attacker.currentHex); match.SetLastToken(attacker);
        var mover = Token("Selected F3 player", true, false, 3); Place(mover, Hex(-12 * side, 8));
        Subscribe(ground); Subscribe(match.finalThirdManager);
        match.BroadcastAnyOtherScenario();
        input.TryExecuteSyntheticKeyPress("P", "Pass before interruption", out _);
        Assert.That(ground.isAwaitingTargetSelection, Is.True);
        match.finalThirdManager.TriggerFinalThirdPhase();
        input.TryExecuteSyntheticHexClick(mover.currentHex, "F3 selection with stale pass", out _);
        var targets = grid.highlightedHexes.ToArray();
        Assert.That(targets, Is.Not.Empty, "A stale pass handler must not erase F3 selection highlights.");
        Invoke(ground, "OnHoverReceived", null, targets.First());
        Assert.That(grid.highlightedHexes, Is.EquivalentTo(targets));
        Assert.That(ground.currentTargetHex, Is.Null);
        yield return ForfeitLegacyFinalThirds();
        ground.CleanUpPass();
        match.finalThirdManager.TriggerFinalThirdPhase(true);
        // First end is followed by the second end; no intermediate action window.
        for (int i = 0; i < 2; i++) { input.TryExecuteSyntheticKeyPress("Return", "First F3 end", out _); yield return null; }
        Assert.That(match.finalThirdManager.isActivated, Is.True);
        AssertOrdinaryActionsUnavailable();
        yield return ForfeitLegacyFinalThirds();
    }

    public IEnumerator RunRepositionHoverOwnership(int side)
    {
        Board(side);
        Place(awayGK, Hex(12 * side));
        Subscribe(movement);
        match.currentState = MatchManager.GameState.MovementPhase;
        movement.isActivated = true; movement.isMovementPhaseDef = true;
        movement.selectedToken = awayGK;
        movement.HighlightValidMovementHexes(awayGK, awayGK.pace);
        yield return movement.MoveTokenToHex(Hex(11 * side, -1), awayGK, true, false);
        yield return Until(() => movement.isWaitingForTackleDecision, "defending move offers a tackle");
        input.TryExecuteSyntheticKeyPress("T", "Reposition tackle", out _);
        movement.PerformTackleDiceRoll(true, 2); movement.PerformTackleDiceRoll(false, 6);
        yield return Until(() => movement.isWaitingForReposition, "attacker wins the real tackle and chooses a reposition");
        var target = Hex(12 * side, -1);
        Assert.That(movement.repositionHexes, Does.Contain(target));
        Invoke(movement, "OnHoverReceived", null, target);
        Assert.That(grid.highlightedHexes, Does.Contain(target), "Selection feedback remains available before confirmation.");
        input.TryExecuteSyntheticHexClick(target, "Commit reposition", out _);
        yield return Until(() => movement.IsResolvingReposition, "reposition travel starts");
        for (int i = 0; i < 3; i++)
        {
            Invoke(movement, "OnHoverReceived", null, movement.repositionHexes.First(h => h != target));
            Assert.That(grid.highlightedHexes, Is.Empty, "Mouse movement must not restore destinations during travel.");
            yield return null;
        }
        yield return Until(() => keeper.isActivated && !attacker.IsMoving, "box entry opens nested goalkeeper decision");
        Assert.That(movement.IsResolvingReposition, Is.True, "The reservation covers the nested GK decision.");
        Assert.That(movement.isWaitingForReposition, Is.True);
        Invoke(movement, "OnHoverReceived", null, target);
        var position = attacker.currentHex;
        input.TryExecuteSyntheticHexClick(Hex(0), "Invalid destination during GK decision", out _);
        Assert.That(attacker.currentHex, Is.SameAs(position));
        yield return Choose(awayGK, false);
        yield return Until(() => !movement.isWaitingForReposition && !movement.IsResolvingReposition, "reposition completes after goalkeeper choice");
        Assert.That(attacker.currentHex, Is.SameAs(target));
        Assert.That(ball.GetCurrentHex(), Is.SameAs(target));
    }
}
