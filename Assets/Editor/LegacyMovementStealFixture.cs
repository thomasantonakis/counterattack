using System.Collections;
using NUnit.Framework;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunMovementStealLegacy(bool succeeds)
    {
        Board(1); Invoke(grid, "LoadPrecomputedShootingPaths");
        attacker.name = attacker.playerName = "Cafferata"; Place(attacker, Hex(0)); ball.PlaceAtCell(attacker.currentHex);
        defender.name = defender.playerName = "Soares"; Place(defender, Hex(14));
        var dribbler = Token("Yaneva", true, false, 10); dribbler.pace = 6; Place(dribbler, Hex(10));
        Subscribe(ground); Subscribe(movement); Subscribe(match.finalThirdManager);
        match.BroadcastSafeEndofMovementPhase(false, false);
        LegacyKey("P"); ClickPass(dribbler.currentHex); ClickPass(dribbler.currentHex);
        yield return Until(() => !ground.isActivated && ball.GetCurrentHex() == dribbler.currentHex, "ground reception enables the legacy dribble");
        yield return ForfeitLegacyFinalThirds();
        LegacyKey("M");
        input.TryExecuteSyntheticHexClick(dribbler.currentHex, "Legacy migration", out _);
        Assert.That(movement.selectedToken, Is.SameAs(dribbler));
        yield return LegacyDribbleStep(dribbler, Hex(11));
        input.TryExecuteSyntheticHexClick(Hex(12, 1), "Legacy migration", out _);
        yield return Until(() => keeper.isActivated, "second pace enters the defending box");
        Assert.That(grid.highlightedHexes, Does.Contain(Hex(16, -1)));
        Assert.That(input.TryExecuteSyntheticHexClick(Hex(16, -1), "Legacy migration", out string error), Is.True, error);
        yield return Until(() => !keeper.isActivated && !keeper.IsResolvingMove && !movement.isPlayerMoving
            && movement.remainingDribblerPace == 4 && movement.isAwaitingHexDestination,
            "goalkeeper move completes before the next dribble pace");
        Assert.That(awayGK.currentHex, Is.SameAs(Hex(16, -1)));
        yield return LegacyDribbleStep(dribbler, Hex(13));
        yield return Until(() => movement.isWaitingForNutmegDecision, "adjacent Soares offers a nutmeg choice");
        Assert.That(movement.nutmeggableDefenders, Is.EquivalentTo(new[] { defender }));
        LegacyKey("X");
        yield return Until(() => movement.isWaitingForInterceptionDiceRoll, "declining the nutmeg offers Soares his steal");
        Assert.That(movement.eligibleDefenders, Is.EquivalentTo(new[] { defender }));
        movement.StartCoroutine(movement.PerformBallInterceptionDiceRoll(succeeds ? 6 : 2));
        if (succeeds)
        {
            yield return Until(() => !movement.isActivated && ball.GetCurrentHex() == defender.currentHex,
                "successful steal attracts the ball and completes movement");
            Assert.That(match.currentState, Is.EqualTo(MatchManager.GameState.AnyOtherScenario));
            Assert.That(match.teamInAttack, Is.EqualTo(MatchManager.TeamInAttack.Away));
            Assert.That(match.attackHasPossession, Is.True);
            Assert.That(ground.isAvailable && movement.isAvailable && longBall.isAvailable, Is.True);
            Assert.That(ftp.isAvailable || high.isAvailable || ftp.isActivated || high.isActivated || longBall.isActivated, Is.False);
            var stats = match.gameData.stats.GetPlayerStats(defender);
            Assert.That(stats.interceptionsAttempted, Is.EqualTo(1)); Assert.That(stats.interceptionsMade, Is.EqualTo(1));
            Assert.That(stats.xRecoveries, Is.EqualTo(1f / 6f).Within(0.0001f));
            Assert.That(match.gameData.stats.awayTeamStats.totalXRecoveries, Is.EqualTo(1f / 6f).Within(0.0001f));
            Assert.That(ground.imposedDistance, Is.EqualTo(6));
            yield return ForfeitLegacyFinalThirds();
            LegacyKey("P"); ground.CommitToThisAction();
            Assert.That(ground.imposedDistance, Is.EqualTo(6), "Post-steal pass commitment preserves its short range.");
        }
        else
        {
            yield return Until(() => movement.defendersTriedToIntercept.Contains(defender)
                && !movement.isWaitingForInterceptionDiceRoll, "failed steal is recorded before continuing the dribble");
            yield return LegacyDribbleStep(dribbler, Hex(14, 1));
            yield return LegacyDribbleStep(dribbler, Hex(14, 2));
            yield return LegacyDribbleStep(dribbler, Hex(14, 3));
            yield return Until(() => movement.isWaitingForSnapshotDecision && !movement.isDribblerRunning,
                "exhausted dribble pace finishes before its final Snapshot decision");
            LegacyKey("X");
            yield return Until(() => movement.attackersMoved == 1 && movement.isMovementPhaseAttack,
                "declining the final Snapshot registers the completed dribbler");
            Assert.That(movement.attackersMoved, Is.EqualTo(1));
            Assert.That(movement.isMovementPhaseAttack, Is.True);
            LegacyKey("X");
            yield return Until(() => movement.isMovementPhaseDef, "forfeiting the remaining attackers enters defense");
            Assert.That(movement.attackersMoved, Is.EqualTo(4));
            Assert.That(movement.isMovementPhaseDef, Is.True);
        }
    }

    private IEnumerator LegacyDribbleStep(PlayerToken dribbler, HexCell destination)
    {
        int paceBefore = movement.remainingDribblerPace;
        input.TryExecuteSyntheticHexClick(destination, "Legacy migration", out _);
        yield return Until(() => dribbler.currentHex == destination && !movement.isPlayerMoving
            && movement.remainingDribblerPace == paceBefore - 1,
            $"legacy dribbler reaches {destination.coordinates}");
        Assert.That(ball.GetCurrentHex(), Is.SameAs(destination));
    }
}
