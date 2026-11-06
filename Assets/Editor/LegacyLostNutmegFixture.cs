using System.Collections;
using System.Linq;
using NUnit.Framework;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunLostNutmegAfterFoulLegacy()
    {
        yield return PrepareLegacyInjuredDribbler();
        var players = match.playerTokenManager.allTokens.ToDictionary(t => t.playerName);
        var dribbler = players["Yaneva"]; var victim = players["Paterson"];
        LegacyKey("X"); LegacyKey("X"); LegacyKey("X");
        yield return Until(() => movement.isAvailable && !movement.isActivated, "remaining sections forfeit to normal action choices");
        yield return ForfeitLegacyFinalThirds(); LegacyKey("M");
        input.TryExecuteSyntheticHexClick(dribbler.currentHex, "Legacy migration", out _);
        Assert.That(movement.isWaitingForNutmegDecisionWithoutMoving, Is.True);
        Assert.That(movement.isAwaitingTokenSelection && movement.isAwaitingHexDestination, Is.True);
        LegacyKey("N");
        Assert.That(movement.isCommitted && movement.isWaitingForTackleRoll, Is.True);
        Assert.That(movement.isWaitingForNutmegDecisionWithoutMoving, Is.False);
        Assert.That(ground.isAvailable || ftp.isAvailable || high.isAvailable || longBall.isAvailable, Is.False);
        movement.PerformTackleDiceRoll(true, new RollInputOverride { hasOverride = true, roll = 6, isJackpot = false });
        yield return AssertLegacyOutfieldDefenderPresentation();
        movement.PerformTackleDiceRoll(false, 2);
        yield return DismissLegacyDuel(false);
        yield return Until(() => movement.isWaitingForReposition, "losing the stationary nutmeg transfers its reposition to Paterson");
        Assert.That(movement.stunnedforNext, Does.Contain(dribbler));
        input.TryExecuteSyntheticHexClick(Hex(6, 2), "Legacy migration", out _);
        yield return Until(() => match.currentState == MatchManager.GameState.SuccessfulTackle && !movement.isActivated,
            "winning defender repositions and broadcasts the successful tackle continuation");
        Assert.That(match.LastTokenToTouchTheBallOnPurpose, Is.SameAs(victim));
        Assert.That(movement.isAvailable && ground.isAvailable && high.isAvailable && longBall.isAvailable, Is.True);
        Assert.That(ftp.isAvailable || ground.isActivated || ftp.isActivated || high.isActivated || longBall.isActivated, Is.False);
    }
    private IEnumerator PrepareLegacyInjuredDribbler(int initialLeniencyRoll = 6)
    {
        var players = LoadLegacyRoster(); var dribbler = players["Yaneva"]; var victim = players["Paterson"];
        AttachAuthoredPanels(); EnableForAuthoredPresentation(movement); EnableForAuthoredPresentation(loose);
        Subscribe(ground); Subscribe(match.finalThirdManager);
        LegacyKey("P"); ClickPass(dribbler.currentHex); ClickPass(dribbler.currentHex);
        yield return Until(() => !ground.isActivated && ball.GetCurrentHex() == dribbler.currentHex, "lost-nutmeg audit receives the kickoff pass");
        yield return ForfeitLegacyFinalThirds(); LegacyKey("M");
        input.TryExecuteSyntheticHexClick(dribbler.currentHex, "Legacy migration", out _);
        foreach (var step in new[] { Hex(9), Hex(8, 1), Hex(7, 1), Hex(6, 2), Hex(5, 2) })
            yield return LegacyDribbleStep(dribbler, step);
        yield return Until(() => movement.isWaitingForInterceptionDiceRoll, "last-pace dribble offers Paterson's steal");
        Assert.That(movement.selectedDefender, Is.SameAs(victim));
        movement.StartCoroutine(movement.PerformBallInterceptionDiceRoll(1));
        yield return Until(() => movement.isWaitingForYellowCardRoll, "steal natural one opens booking");
        movement.PerformLeniencyTest(initialLeniencyRoll);
        yield return Until(() => movement.isWaitingForInjuryRoll, "booked defender proceeds to injury check");
        movement.PerformInjuryTest(6);
        yield return DismissLegacyDuel(false);
        yield return Until(() => movement.isWaitingForFoulDecision, "first injury and booking retain Play On");
        LegacyKey("A");
        yield return Until(() => movement.attackersMoved == 1 && movement.isAwaitingTokenSelection, "injury exhausts the last pace and registers the dribbler once");
        Assert.That(movement.movedTokens.Count, Is.EqualTo(1));
    }

}
