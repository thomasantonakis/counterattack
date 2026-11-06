using System.Collections;
using NUnit.Framework;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunHighPassMovementLegacy(bool ballControl)
    {
        var players = LoadLegacyRoster(); var header = match.shotManager.headerManager;
        Subscribe(high); Subscribe(movement); Subscribe(match.finalThirdManager); Subscribe(header);
        LegacyKey("C");
        input.TryExecuteSyntheticHexClick(Hex(13, 1), "Legacy migration", out _);
        Assert.That(high.eligibleAttackers.Count, Is.EqualTo(1));
        Assert.That(high.currentTargetHex, Is.SameAs(Hex(13, 1)));
        input.TryExecuteSyntheticHexClick(Hex(7, 5), "Legacy migration", out _);
        Assert.That(high.eligibleAttackers.Count, Is.EqualTo(3));
        Assert.That(high.isWaitingForConfirmation, Is.True);
        Assert.That(high.currentTargetHex, Is.SameAs(Hex(7, 5)));
        input.TryExecuteSyntheticHexClick(Hex(-6, 3), "Legacy migration", out _);
        Assert.That(high.eligibleAttackers.Count, Is.EqualTo(0));
        Assert.That(high.isWaitingForConfirmation, Is.True);
        Assert.That(high.currentTargetHex, Is.Null);
        input.TryExecuteSyntheticHexClick(players["Yaneva"].currentHex, "Legacy migration", out _);
        Assert.That(high.eligibleAttackers.Count, Is.EqualTo(1));
        Assert.That(high.isWaitingForConfirmation, Is.True);
        Assert.That(high.currentTargetHex, Is.SameAs(players["Yaneva"].currentHex));
        input.TryExecuteSyntheticHexClick(players["Yaneva"].currentHex, "Legacy migration", out _);
        yield return Until(() => high.isWaitingForAttackerSelection, "confirmed high pass opens attacker movement");
        Assert.That(high.isWaitingForConfirmation, Is.False);
        Assert.That(high.lockedAttacker, Is.SameAs(players["Yaneva"]));
        input.TryExecuteSyntheticHexClick(players["Abraham"].currentHex, "Legacy migration", out _);
        AssertHighPassMovementSelection(true, false);
        input.TryExecuteSyntheticHexClick(players["Pavlovic"].currentHex, "Legacy migration", out _);
        AssertHighPassMovementSelection(true, true);
        input.TryExecuteSyntheticHexClick(players["Yaneva"].currentHex, "Legacy migration", out _);
        AssertHighPassMovementSelection(true, false);
        input.TryExecuteSyntheticHexClick(players["Pavlovic"].currentHex, "Legacy migration", out _);
        AssertHighPassMovementSelection(true, true);
        input.TryExecuteSyntheticHexClick(Hex(0, 8), "Legacy migration", out _);
        AssertHighPassMovementSelection(true, false);
        input.TryExecuteSyntheticHexClick(players["Pavlovic"].currentHex, "Legacy migration", out _);
        AssertHighPassMovementSelection(true, true);
        input.TryExecuteSyntheticHexClick(Hex(-1, -5), "Legacy migration", out _);
        yield return Until(() => players["Pavlovic"].currentHex == Hex(-1, -5) && high.isWaitingForDefenderSelection,
            "valid attacker movement hands high-pass input to defense");
        AssertHighPassMovementSelection(false, false);
        input.TryExecuteSyntheticHexClick(players["Stewart"].currentHex, "Legacy migration", out _);
        AssertHighPassMovementSelection(false, true);
        input.TryExecuteSyntheticHexClick(players["Vladoiu"].currentHex, "Legacy migration", out _);
        AssertHighPassMovementSelection(false, true);
        input.TryExecuteSyntheticHexClick(players["Ulisses"].currentHex, "Legacy migration", out _);
        AssertHighPassMovementSelection(false, false);
        input.TryExecuteSyntheticHexClick(players["Soares"].currentHex, "Legacy migration", out _);
        AssertHighPassMovementSelection(false, true);
        Assert.That(high.isWaitingForAccuracyRoll, Is.False);
        input.TryExecuteSyntheticHexClick(Hex(14, 1), "Legacy migration", out _);
        yield return Until(() => players["Soares"].currentHex == Hex(14, 1) && high.isWaitingForAccuracyRoll,
            "valid defender movement opens high-pass accuracy");
        Assert.That(high.isWaitingForAttackerSelection || high.isWaitingForAttackerMove
            || high.isWaitingForDefenderSelection || high.isWaitingForDefenderMove, Is.False);
        high.PerformAccuracyRoll(new RollInputOverride { hasOverride = true, roll = 6, isJackpot = false });
        yield return Until(() => !high.isActivated && ball.GetCurrentHex() == players["Yaneva"].currentHex,
            "accurate high pass reaches its locked attacker and releases the phase", 120);
        yield return ForfeitLegacyFinalThirds();
        yield return Until(() => header.isActivated && header.isWaitingForControlOrHeaderDecision,
            "unchallenged accurate landing offers Header or Ball Control after Final Thirds");
        if (ballControl)
        {
            LegacyKey("B");
            yield return Until(() => header.isWaitingForControlRoll, "unchallenged attacker chooses Ball Control");
            Assert.That(header.isWaitingForControlOrHeaderDecision, Is.False);
            Assert.That(header.challengeWinner, Is.SameAs(players["Yaneva"]));
        }
    }

    private void AssertHighPassMovementSelection(bool attacking, bool selected)
    {
        Assert.That(high.isWaitingForAttackerSelection, Is.EqualTo(attacking));
        Assert.That(high.isWaitingForAttackerMove, Is.EqualTo(attacking && selected));
        Assert.That(high.isWaitingForDefenderSelection, Is.EqualTo(!attacking));
        Assert.That(high.isWaitingForDefenderMove, Is.EqualTo(!attacking && selected));
    }
}
