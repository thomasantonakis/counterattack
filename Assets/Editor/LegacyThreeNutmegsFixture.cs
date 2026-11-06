using System.Collections;
using NUnit.Framework;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunThreeNutmegsLegacy()
    {
        var players = LoadLegacyRoster(); var dribbler = players["Yaneva"];
        Place(dribbler, Hex(5, 2)); ball.PlaceAtCell(dribbler.currentHex); match.SetLastToken(dribbler);
        AttachAuthoredPanels(); EnableForAuthoredPresentation(movement); EnableForAuthoredPresentation(loose);
        match.BroadcastSafeEndofMovementPhase(false, false); LegacyKey("M");
        input.TryExecuteSyntheticHexClick(dribbler.currentHex, "Legacy migration", out _);
        Assert.That(movement.isWaitingForNutmegDecisionWithoutMoving, Is.True);
        Assert.That(movement.isAwaitingTokenSelection && movement.isAwaitingHexDestination, Is.True);
        foreach (string instruction in new[] { "Click on a Nutmeggable Defender", "press [N]", "click on a Free Hex" })
            Assert.That(movement.GetInstructions(), Does.Contain(instruction));
        var victims = new[] { players["Paterson"], players["Gilbert"], players["Stewart"] };
        var destinations = new[] { Hex(3, 2), Hex(3, 4), Hex(4, 6) };
        for (int i = 0; i < victims.Length; i++)
        {
            if (i > 0) Assert.That(movement.isWaitingForNutmegDecision, Is.True);
            LegacyKey("N");
            Assert.That(movement.isWaitingForNutmegDecision || movement.isWaitingForNutmegDecisionWithoutMoving, Is.False);
            Assert.That(movement.isWaitingForTackleRoll && movement.isCommitted, Is.True);
            Assert.That(movement.nutmegVictim, Is.SameAs(victims[i]));
            Assert.That(ground.isAvailable || ftp.isAvailable || high.isAvailable || longBall.isAvailable, Is.False);
            yield return WinLegacyAttackerDuel();
            for (int j = 0; j <= i; j++) Assert.That(movement.stunnedTokens, Does.Contain(victims[j]));
            input.TryExecuteSyntheticHexClick(destinations[i], "Legacy migration", out _);
            int expectedPace = 4 - 2 * i;
            yield return Until(() => dribbler.currentHex == destinations[i] && !movement.isWaitingForReposition
                && movement.remainingDribblerPace == expectedPace, "successful nutmeg reposition consumes exactly two pace");
            Assert.That(movement.isNutmegInProgress, Is.False);
        }
        Assert.That(movement.isWaitingForInterceptionDiceRoll, Is.True);
        Assert.That(movement.eligibleDefenders, Is.EquivalentTo(new[] { players["McNulty"] }));
        yield return FailLegacyQueuedSteal();
        Assert.That(movement.remainingDribblerPace, Is.EqualTo(0));
        Assert.That(match.gameData.stats.GetPlayerStats(dribbler).dribblesMade, Is.EqualTo(3));
        Assert.That(match.gameData.stats.homeTeamStats.totalDribblesMade, Is.EqualTo(3));
        Assert.That(match.gameData.stats.awayTeamStats.totalTacklesMade, Is.EqualTo(0));
    }
}
