using System.Collections;
using NUnit.Framework;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunHighPassBoxSequenceLegacy()
    {
        var players = LoadLegacyRoster(); var header = match.shotManager.headerManager;
        Subscribe(high); Subscribe(header); Subscribe(movement); Subscribe(match.finalThirdManager);
        // This aerial sequence starts in open play; StartMatch offers kickoff's ground pass.
        match.BroadcastSafeEndofMovementPhase(countMovementAction: false, triggerFinalThird: false);
        var target = Hex(13, 1);
        LegacyKey("C"); Assert.That(high.isActivated, Is.True);
        input.TryExecuteSyntheticHexClick(target, "Legacy migration", out _);
        Assert.That(high.currentTargetHex, Is.SameAs(target));
        input.TryExecuteSyntheticHexClick(target, "Legacy migration", out _);
        yield return Until(() => high.isWaitingForDefenderSelection, "in-box high-pass target receives its sole eligible attacker before defender movement");
        Assert.That(players["Yaneva"].currentHex, Is.SameAs(target));
        input.TryExecuteSyntheticHexClick(players["Soares"].currentHex, "Legacy migration", out _);
        yield return Until(() => high.isWaitingForDefenderMove, "Soares selected for defensive high-pass movement");
        input.TryExecuteSyntheticHexClick(Hex(14, 1), "Legacy migration", out _);
        yield return Until(() => high.isWaitingForAccuracyRoll, "Soares movement ends before accuracy resolution");
        high.PerformAccuracyRoll(new RollInputOverride { hasOverride = true, roll = 6, isJackpot = false });
        yield return Until(() => keeper.isActivated, "accurate in-box landing offers the free goalkeeper hex after arrival", 120);
        Assert.That(ball.GetCurrentHex(), Is.SameAs(target));
        Assert.That(header.isActivated, Is.False);
        input.TryExecuteSyntheticHexClick(Hex(16, 1), "Legacy migration", out _);
        yield return Until(() => high.isWaitingForDefGKChallengeDecision, "free goalkeeper move finishes before the separate high-pass rush decision");
        Assert.That(players["Kuzmic"].currentHex, Is.SameAs(Hex(16, 1)));
        Assert.That(header.isActivated, Is.False);
        LegacyKey("X");
        yield return Until(() => match.finalThirdManager.isActivated && !high.isActivated,
            "forfeiting the separate rush hands the accurate landing to Final Third movement");
        Assert.That(header.isActivated, Is.False);
        yield return ForfeitLegacyFinalThirds();
        yield return Until(() => header.isActivated, "header begins only after goalkeeper and Final Third continuations finish");
        Assert.That(header.attEligibleToHead, Does.Contain(players["Yaneva"]));
        Assert.That(header.isWaitingForHeaderAtGoal, Is.True);
    }
}
