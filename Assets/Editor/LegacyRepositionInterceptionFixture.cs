using System.Collections;
using NUnit.Framework;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunRepositionInterceptionsLegacy()
    {
        var players = LoadLegacyRoster(); var dribbler = players["Yaneva"];
        var paterson = players["Paterson"]; var gilbert = players["Gilbert"];
        var mcnulty = players["McNulty"]; var stewart = players["Stewart"];
        AttachAuthoredPanels(); EnableForAuthoredPresentation(movement); EnableForAuthoredPresentation(loose);
        Subscribe(ground); Subscribe(match.finalThirdManager);
        LegacyKey("P"); ClickPass(dribbler.currentHex); ClickPass(dribbler.currentHex);
        yield return Until(() => !ground.isActivated && ball.GetCurrentHex() == dribbler.currentHex, "reposition audit receives the kickoff pass");
        yield return ForfeitLegacyFinalThirds(); LegacyKey("M");
        input.TryExecuteSyntheticHexClick(dribbler.currentHex, "Legacy migration", out _);
        foreach (var step in new[] { Hex(9), Hex(8, 1), Hex(7, 1), Hex(6, 2), Hex(5, 2) })
            yield return LegacyDribbleStep(dribbler, step);
        yield return Until(() => movement.isWaitingForInterceptionDiceRoll, "initial low-pace dribble offers the adjacent defender steal");
        yield return FailLegacyQueuedSteal();
        LegacyKey("X"); LegacyKey("X");
        yield return Until(() => movement.isMovementPhaseDef, "dribbler/attack forfeits enter the defensive section");
        input.TryExecuteSyntheticHexClick(paterson.currentHex, "Legacy migration", out _);
        Assert.That(movement.isWaitingForTackleDecisionWithoutMoving, Is.True);
        Assert.That(movement.isWaitingForTackleDecision, Is.False);
        input.TryExecuteSyntheticHexClick(Hex(6, 4), "Legacy migration", out _);
        yield return Until(() => movement.defendersMoved == 1 && movement.movedTokens.Contains(paterson), "Paterson moves away instead of tackling in place");
        input.TryExecuteSyntheticHexClick(gilbert.currentHex, "Legacy migration", out _);
        Assert.That(movement.isWaitingForTackleDecisionWithoutMoving || movement.isWaitingForTackleDecision, Is.False);
        input.TryExecuteSyntheticHexClick(Hex(5, 3), "Legacy migration", out _);
        yield return Until(() => movement.movedTokens.Contains(gilbert) && movement.isWaitingForTackleDecision, "Gilbert moves adjacent and offers tackle");
        Assert.That(movement.isWaitingForTackleDecisionWithoutMoving, Is.False);
        LegacyKey("T");
        Assert.That(movement.isWaitingForTackleDecision, Is.False);
        Assert.That(movement.isWaitingForTackleRoll, Is.True);
        Assert.That(movement.selectedDefender, Is.SameAs(gilbert));
        yield return WinLegacyAttackerDuel();
        input.TryExecuteSyntheticHexClick(Hex(5, 4), "Legacy migration", out _);
        yield return Until(() => movement.isWaitingForInterceptionDiceRoll, "attacker reposition offers surrounding steals");
        Assert.That(movement.eligibleDefenders, Is.EquivalentTo(new[] { stewart, mcnulty }));
        yield return FailLegacyQueuedSteal(); yield return FailLegacyQueuedSteal();
        yield return Until(() => movement.defendersMoved == 2 && movement.isAwaitingTokenSelection, "Gilbert's tackle/reposition resolves one defender move");
        input.TryExecuteSyntheticHexClick(mcnulty.currentHex, "Legacy migration", out _);
        Assert.That(movement.isWaitingForTackleDecisionWithoutMoving, Is.True);
        Assert.That(movement.isWaitingForTackleDecision, Is.False);
        LegacyKey("T");
        Assert.That(movement.movedTokens, Does.Contain(mcnulty));
        Assert.That(movement.isWaitingForTackleDecisionWithoutMoving, Is.False);
        Assert.That(movement.isWaitingForTackleRoll, Is.True);
        string instructions = movement.GetInstructions();
        Assert.That(instructions, Does.Contain("Press [R] to roll with McNulty for the tackle"));
        foreach (string stale in new[] { "Click on a Defender", "Click on a Free Hex", "Click on a Token" })
            Assert.That(instructions, Does.Not.Contain(stale));
        yield return WinLegacyAttackerDuel();
        input.TryExecuteSyntheticHexClick(Hex(4, 6), "Legacy migration", out _);
        yield return Until(() => movement.isWaitingForInterceptionDiceRoll, "second reposition offers Stewart's steal");
        Assert.That(movement.selectedDefender, Is.SameAs(stewart));
        yield return FailLegacyQueuedSteal();
        yield return Until(() => movement.defendersMoved == 3 && movement.isAwaitingTokenSelection, "McNulty's stationary tackle consumes exactly one move");
        input.TryExecuteSyntheticHexClick(stewart.currentHex, "Legacy migration", out _);
        Assert.That(movement.isWaitingForTackleDecisionWithoutMoving, Is.True);
        Assert.That(movement.isWaitingForTackleDecision, Is.False);
        input.TryExecuteSyntheticHexClick(Hex(0, 7), "Legacy migration", out _);
        yield return Until(() => movement.defendersMoved == 4 && movement.movedTokens.Contains(stewart), "Stewart declines the stationary tackle by moving away");
        Assert.That(movement.isWaitingForTackleDecisionWithoutMoving || movement.isWaitingForTackleDecision, Is.False);
        LegacyKey("X"); Assert.That(movement.defendersMoved, Is.EqualTo(5));
        yield return MoveLegacySelectedToken(players["André Noruega"], Hex(-8, -6));
        Assert.That(movement.attackersMovedIn2f2, Is.EqualTo(1));
        yield return MoveLegacySelectedToken(players["Murphy"], Hex(10, 12), true);
        yield return ForfeitLegacyFinalThirds();
        Assert.That(match.attackHasPossession, Is.True);
        Assert.That(movement.isAvailable && ground.isAvailable && high.isAvailable && longBall.isAvailable, Is.True);
        Assert.That(ground.imposedDistance, Is.EqualTo(11));
        Assert.That(ftp.isAvailable || movement.isActivated || ground.isActivated || ftp.isActivated || high.isActivated || longBall.isActivated, Is.False);
    }

    private IEnumerator FailLegacyQueuedSteal()
    {
        var stealer = movement.selectedDefender;
        Assert.That(movement.isWaitingForInterceptionDiceRoll, Is.True);
        movement.StartCoroutine(movement.PerformBallInterceptionDiceRoll(2));
        yield return Until(() => movement.defendersTriedToIntercept.Contains(stealer)
            || !movement.isWaitingForInterceptionDiceRoll && movement.isAwaitingTokenSelection,
            "failed steal completes its presentation and advances actual continuation");
    }

    private IEnumerator WinLegacyAttackerDuel()
    {
        movement.PerformTackleDiceRoll(true, 2);
        yield return AssertLegacyOutfieldDefenderPresentation();
        if (movement.isNutmegInProgress)
            Assert.That(movement.nutmegVictim, Is.Not.Null, "An earlier duel continuation must not clear the active nutmeg victim.");
        movement.PerformTackleDiceRoll(false, new RollInputOverride { hasOverride = true, roll = 6, isJackpot = false });
        yield return DismissLegacyDuel(false);
        yield return Until(() => movement.isWaitingForReposition, "attacker wins the actual tackle and receives reposition");
    }
}
