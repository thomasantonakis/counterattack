using System.Collections;
using System.Linq;
using NUnit.Framework;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunNutmegFoulLegacy(string branch)
    {
        var players = LoadLegacyRoster(); var dribbler = players["Yaneva"];
        var victim = players["Paterson"];
        Place(players["Nazef"], Hex(8, 6)); Place(victim, Hex(3, 4));
        // Reach the old two-pace-left audit state by actual dribbling, rather
        // than assigning the remaining-pace result expected by the assertions.
        Place(dribbler, Hex(8, 2)); ball.PlaceAtCell(dribbler.currentHex); match.SetLastToken(dribbler);
        dribbler.resilience = 3; match.refereeLeniency = 3;
        AttachAuthoredPanels(); EnableForAuthoredPresentation(movement); EnableForAuthoredPresentation(loose);
        Subscribe(match.finalThirdManager);
        match.BroadcastSafeEndofMovementPhase(false, false); LegacyKey("M");
        input.TryExecuteSyntheticHexClick(dribbler.currentHex, "Legacy migration", out _);
        foreach (var step in new[] { Hex(7, 2), Hex(6, 3), Hex(5, 3), Hex(4, 4) })
            yield return LegacyDribbleStep(dribbler, step);
        Assert.That(movement.remainingDribblerPace, Is.EqualTo(2));
        Assert.That(movement.isWaitingForNutmegDecision, Is.True);
        LegacyKey("N"); input.TryExecuteSyntheticHexClick(victim.currentHex, "Legacy migration", out _);
        Assert.That(movement.eligibleDefenders.Count, Is.EqualTo(2));
        var secondStealer = movement.eligibleDefenders[1]; var origin = dribbler.currentHex;
        bool injured = branch is "injury" or "post";
        yield return ResolveLegacyNutmegStealFoul(injured);
        yield return Until(() => movement.isWaitingForInterceptionDiceRoll, "Play On advances to the remaining pre-nutmeg steal");
        Assert.That(movement.selectedDefender, Is.SameAs(secondStealer));
        Assert.That(movement.nutmegVictim, Is.SameAs(victim));
        Assert.That(dribbler.currentHex, Is.SameAs(origin));
        if (branch == "recover")
        {
            movement.StartCoroutine(movement.PerformBallInterceptionDiceRoll(6));
            yield return Until(() => match.currentState == MatchManager.GameState.AnyOtherScenario && !movement.isActivated,
                "successful queued steal completes movement");
            Assert.That(ball.GetCurrentHex(), Is.SameAs(secondStealer.currentHex));
            Assert.That(match.LastTokenToTouchTheBallOnPurpose, Is.SameAs(secondStealer));
            Assert.That(secondStealer.isAttacker && !dribbler.isAttacker, Is.True);
            yield break;
        }
        if (branch == "red")
        {
            yield return ResolveLegacyNutmegStealFoul(false, true);
            yield return Until(() => secondStealer.isSentOff && !movement.isWaitingForTackleRoll && !movement.isWaitingForReposition,
                "second yellow forces the foul instead of the reserved nutmeg");
            Assert.That(movement.isWaitingForFoulDecision, Is.False);
            yield break;
        }
        if (branch == "miss") movement.StartCoroutine(movement.PerformBallInterceptionDiceRoll(2));
        else yield return ResolveLegacyNutmegStealFoul(false);
        yield return Until(() => movement.isWaitingForTackleRoll, "exhausting the steal queue starts the preserved nutmeg duel");
        Assert.That(movement.nutmegVictim, Is.SameAs(victim));
        Assert.That(movement.remainingDribblerPace, Is.EqualTo(injured ? 1 : 2));
        movement.PerformTackleDiceRoll(true, 2);
        yield return AssertLegacyOutfieldDefenderPresentation();
        movement.PerformTackleDiceRoll(false, new RollInputOverride { hasOverride = true, roll = 6, isJackpot = true });
        yield return DismissLegacyDuel(false);
        yield return Until(() => movement.isWaitingForReposition, "nutmeg win permits mandatory reposition with one pace remaining");
        var destination = movement.repositionHexes.First();
        PlayerToken landingStealer = null;
        if (branch == "post")
        {
            landingStealer = match.playerTokenManager.allTokens.First(t => t.isPlaying && !t.isAttacker && !t.IsGoalKeeper
                && t != victim && !movement.defendersTriedToIntercept.Contains(t));
            var adjacent = destination.GetNeighbors(grid).First(h => h != null && !h.isOutOfBounds
                && h != origin && !h.isAttackOccupied && !h.isDefenseOccupied);
            Place(landingStealer, adjacent);
            Assert.That(adjacent.isDefenseOccupied, Is.True);
        }
        input.TryExecuteSyntheticHexClick(destination, "Legacy migration", out _);
        yield return Until(() => dribbler.currentHex == destination && !movement.isWaitingForReposition
            && movement.movedTokens.Contains(dribbler), "nutmeg reposition finishes without negative pace or a softlock");
        Assert.That(movement.remainingDribblerPace, Is.EqualTo(0));
        if (branch == "post")
        {
            yield return Until(() => movement.isWaitingForInterceptionDiceRoll, "fresh landing defender receives a steal even at zero remaining pace");
            Assert.That(movement.selectedDefender, Is.SameAs(landingStealer));
            yield return ResolveLegacyNutmegStealFoul(false);
            yield return Until(() => !movement.isDribblerRunning, "landing steal foul followed by Play On finishes the exhausted turn");
            Assert.That(movement.isWaitingForReposition, Is.False);
        }
    }

    private IEnumerator ResolveLegacyNutmegStealFoul(bool injury, bool red = false)
    {
        var stealer = movement.selectedDefender;
        if (red && !stealer.isBooked) stealer.ReceiveYellowCard();
        movement.StartCoroutine(movement.PerformBallInterceptionDiceRoll(1));
        yield return Until(() => movement.isWaitingForYellowCardRoll, "nutmeg queued steal foul opens leniency");
        movement.PerformLeniencyTest(red ? 6 : 1);
        yield return Until(() => movement.isWaitingForInjuryRoll, "nutmeg queued foul opens resilience");
        movement.PerformInjuryTest(injury ? 6 : 1);
        yield return DismissLegacyDuel(false);
        if (red) yield break;
        yield return Until(() => movement.isWaitingForFoulDecision, "non-forced nutmeg steal foul offers Play On");
        LegacyKey("A");
        Assert.That(movement.isWaitingForReposition, Is.False, "A steal foul gives no tackle-winner reposition.");
    }
}
