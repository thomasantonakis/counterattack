using System.Collections;
using NUnit.Framework;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunDribbleForfeitLegacy()
    {
        var players = LoadLegacyRoster(); var dribbler = players["Yaneva"]; var noruega = players["André Noruega"];
        Subscribe(ground); Subscribe(movement); Subscribe(match.finalThirdManager);
        LegacyKey("P"); ClickPass(dribbler.currentHex); ClickPass(dribbler.currentHex);
        yield return Until(() => !ground.isActivated && ball.GetCurrentHex() == dribbler.currentHex, "forfeit audit ground pass reaches Yaneva");
        Assert.That(movement.isAvailable && ftp.isAvailable, Is.True);
        Assert.That(ground.isAvailable || high.isAvailable || longBall.isAvailable, Is.False);
        yield return ForfeitLegacyFinalThirds(); LegacyKey("M");
        input.TryExecuteSyntheticHexClick(dribbler.currentHex, "Legacy migration", out _);
        yield return LegacyDribbleStep(dribbler, Hex(10, 1));
        yield return LegacyDribbleStep(dribbler, Hex(10, 2));
        Assert.That(movement.isDribblerRunning && movement.remainingDribblerPace > 0, Is.True);
        LegacyKey("X");
        yield return Until(() => movement.movedTokens.Contains(dribbler) && movement.isAwaitingTokenSelection, "forfeit registers the dribbler and returns token selection");
        Assert.That(movement.attackersMoved, Is.EqualTo(1));
        Assert.That(movement.isDribblerRunning, Is.False);
        yield return MoveLegacySelectedToken(noruega, Hex(-8, -6));
        Assert.That(movement.movedTokens, Does.Contain(dribbler));
        Assert.That(movement.movedTokens, Does.Contain(noruega));
        Assert.That(movement.attackersMoved, Is.EqualTo(2));
    }
}
