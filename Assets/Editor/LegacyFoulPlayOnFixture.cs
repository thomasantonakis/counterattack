using System.Collections;
using NUnit.Framework;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunFoulPlayOnLegacy()
    {
        yield return PrepareLegacySoaresStealFoul();
        // The legacy scenario used random checks. Fixed non-booking/non-injury
        // rolls preserve its Play On branch while making it independently repeatable.
        movement.PerformLeniencyTest(1);
        Assert.That(movement.isWaitingForFoulDecision, Is.False);
        yield return Until(() => movement.isWaitingForInjuryRoll, "leniency completes before resilience check");
        movement.PerformInjuryTest(1);
        yield return DismissLegacyDuel(false);
        yield return Until(() => movement.isWaitingForFoulDecision, "dismissed foul result exposes Play On / Take Foul");
        LegacyKey("A");
        yield return Until(() => !movement.isWaitingForFoulDecision && !movement.isWaitingForReposition
            && movement.isWaitingForSnapshotDecision, "Play On resumes dribbling with the box Snapshot option");
        input.TryExecuteSyntheticHexClick(Hex(15), "Legacy migration", out _);
        yield return null;
        Assert.That(movement.isWaitingForReposition, Is.False, "A steal foul grants no tackle-winner reposition.");
        Assert.That(movement.isWaitingForSnapshotDecision, Is.True);
    }
    private IEnumerator PrepareLegacySoaresStealFoul(int side = 1)
    {
        var players = LoadLegacyRoster(side);
        var dribbler = players["Yaneva"];
        var soares = players["Soares"];
        AttachAuthoredPanels(); EnableForAuthoredPresentation(movement); EnableForAuthoredPresentation(loose);
        Subscribe(ground); Subscribe(match.finalThirdManager);
        LegacyKey("P"); ClickPass(dribbler.currentHex); ClickPass(dribbler.currentHex);
        yield return Until(() => !ground.isActivated && ball.GetCurrentHex() == dribbler.currentHex, "ground reception enables the foul/advantage dribble");
        yield return ForfeitLegacyFinalThirds(); LegacyKey("M");
        input.TryExecuteSyntheticHexClick(dribbler.currentHex, "Legacy migration", out _);
        Assert.That(movement.selectedToken, Is.SameAs(dribbler));
        yield return LegacyDribbleStep(dribbler, Hex(11 * side));
        input.TryExecuteSyntheticHexClick(Hex(12 * side, 1), "Legacy migration", out _);
        yield return Until(() => keeper.isActivated, "second pace offers the box-entry goalkeeper move");
        input.TryExecuteSyntheticHexClick(Hex(16 * side, -1), "Legacy migration", out _);
        yield return Until(() => !keeper.isActivated && !keeper.IsResolvingMove
            && movement.remainingDribblerPace == 4 && movement.isAwaitingHexDestination, "dribbler regains control after the nested goalkeeper move");
        yield return LegacyDribbleStep(dribbler, Hex(13 * side));
        yield return Until(() => movement.isWaitingForNutmegDecision, "Soares offers the nutmeg choice");
        Assert.That(movement.nutmeggableDefenders, Is.EquivalentTo(new[] { soares }));
        LegacyKey("X");
        yield return Until(() => movement.isWaitingForInterceptionDiceRoll, "declining nutmeg offers Soares his steal");
        Assert.That(movement.eligibleDefenders, Is.EquivalentTo(new[] { soares }));
        movement.StartCoroutine(movement.PerformBallInterceptionDiceRoll(1));
        yield return Until(() => movement.isWaitingForYellowCardRoll, "natural one enters actual foul checks");
    }

}
