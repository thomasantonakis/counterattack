using System.Collections;
using System.Linq;
using NUnit.Framework;

public sealed partial class GoalkeeperBoxEntryFixture
{
    private IEnumerator PrepareLegacyBoxDribble(bool moveSupport, int goalkeeperZ)
    {
        var players = LoadLegacyRoster(); var yaneva = players["Yaneva"];
        AttachAuthoredPanels(); EnableForAuthoredPresentation(movement); EnableForAuthoredPresentation(loose);
        EnableForAuthoredPresentation(match.shotManager);
        Subscribe(ground); Subscribe(match.finalThirdManager);
        LegacyKey("P"); ClickPass(yaneva.currentHex); ClickPass(yaneva.currentHex);
        yield return Until(() => !ground.isActivated && ball.GetCurrentHex() == yaneva.currentHex, "box-continuation audit receives its kickoff pass");
        Assert.That(match.attackHasPossession && ftp.isAvailable && movement.isAvailable, Is.True);
        Assert.That(high.isAvailable || longBall.isAvailable || ground.isAvailable || movement.isActivated
            || ftp.isActivated || high.isActivated || longBall.isActivated, Is.False);
        yield return ForfeitLegacyFinalThirds(); LegacyKey("M");
        if (moveSupport)
        {
            var support = players["Toothnail"];
            input.TryExecuteSyntheticHexClick(support.currentHex, "Legacy migration", out _);
            input.TryExecuteSyntheticHexClick(Hex(13, 5), "Legacy migration", out _);
            yield return Until(() => support.currentHex == Hex(13, 5) && !movement.isPlayerMoving, "Toothnail reaches the later loose-ball path through actual movement");
        }
        input.TryExecuteSyntheticHexClick(yaneva.currentHex, "Legacy migration", out _);
        yield return LegacyDribbleStep(yaneva, Hex(11));
        input.TryExecuteSyntheticHexClick(Hex(12, 1), "Legacy migration", out _);
        yield return Until(() => keeper.isActivated, "actual dribble enters the box and offers the goalkeeper hex");
        input.TryExecuteSyntheticHexClick(Hex(16, goalkeeperZ), "Legacy migration", out _);
        yield return Until(() => !keeper.isActivated && !keeper.IsResolvingMove && movement.remainingDribblerPace == 4
            && movement.isAwaitingHexDestination, "box-entry goalkeeper movement returns control to the dribbler");
    }

    public IEnumerator RunDefensiveBoxLooseNoSnapshotLegacy()
    {
        yield return PrepareLegacyBoxDribble(true, 1);
        var players = match.playerTokenManager.allTokens.ToDictionary(t => t.playerName);
        var yaneva = players["Yaneva"]; var soares = players["Soares"]; var support = players["Toothnail"];
        yield return LegacyDribbleStep(yaneva, Hex(13, 1));
        LegacyKey("X"); LegacyKey("X");
        yield return Until(() => movement.isMovementPhaseDef && movement.attackersMoved == 4, "attacking pace and section forfeits reach defense");
        input.TryExecuteSyntheticHexClick(soares.currentHex, "Legacy migration", out _);
        input.TryExecuteSyntheticHexClick(Hex(13, 2), "Legacy migration", out _);
        yield return Until(() => movement.isWaitingForTackleDecision && !movement.isPlayerMoving, "Soares moves before the defensive box tackle");
        yield return RollLegacyTackleToScatter(6, 4); yield return RollLegacyNorthScatter(6);
        yield return Until(() => !loose.isActivated && !duelPanel.IsOpen && ball.GetCurrentHex() == support.currentHex,
            "defensive tackle scatter hits Toothnail and releases the continuous duel panel");
        Assert.That(match.PreviousTokenToTouchTheBallOnPurpose, Is.Null);
        Assert.That(movement.isMovementPhaseDef, Is.True);
        Assert.That(movement.movedTokens.Count, Is.EqualTo(3)); Assert.That(movement.MovedTokenEntries.Count, Is.EqualTo(3));
        Assert.That(movement.MovedTokenEntries.Count(e => e.section == MovementPhaseMovedTokenSection.DefMP), Is.EqualTo(1));
        Assert.That(movement.MovedTokenEntries.Any(e => e.section == MovementPhaseMovedTokenSection.DefMP && e.token == yaneva), Is.False);
        Assert.That(movement.attackersMoved, Is.EqualTo(4)); Assert.That(movement.defendersMoved, Is.EqualTo(1));
        foreach (var token in new[] { support, yaneva, soares }) Assert.That(movement.movedTokens, Does.Contain(token));
        Assert.That(movement.isWaitingForSnapshotDecision, Is.False);
        var vladoiu = players["Vladoiu"];
        input.TryExecuteSyntheticHexClick(vladoiu.currentHex, "Legacy migration", out _);
        input.TryExecuteSyntheticHexClick(Hex(-1, 1), "Legacy migration", out _);
        yield return Until(() => vladoiu.currentHex == Hex(-1, 1) && !movement.isPlayerMoving && movement.defendersMoved == 2,
            "another defender can continue after the attacker collects the defensive scatter");
        Assert.That(movement.isMovementPhaseDef, Is.True); Assert.That(movement.movedTokens.Count, Is.EqualTo(4));
        Assert.That(movement.attackersMoved, Is.EqualTo(4));
        foreach (var token in new[] { support, yaneva, soares, vladoiu }) Assert.That(movement.movedTokens, Does.Contain(token));
        LegacyKey("X"); LegacyKey("X");
        yield return Until(() => !movement.isActivated, "remaining defensive and two-for-two sections finish");
        yield return ForfeitLegacyFinalThirds();
        yield return Until(() => movement.isAvailable && ground.isAvailable && high.isAvailable && longBall.isAvailable,
            "completed movement restores the normal full action menu");
        Assert.That(ground.imposedDistance, Is.EqualTo(11));
        Assert.That(ftp.isAvailable || movement.isActivated || ground.isActivated || ftp.isActivated || high.isActivated || longBall.isActivated, Is.False);
    }

    public IEnumerator RunNutmegTieBackSnapshotLegacy()
    {
        yield return PrepareLegacyBoxDribble(false, -1);
        var players = match.playerTokenManager.allTokens.ToDictionary(t => t.playerName);
        var yaneva = players["Yaneva"];
        yield return LegacyDribbleStep(yaneva, Hex(13));
        yield return Until(() => movement.isWaitingForNutmegDecision, "in-box Yaneva can attempt Soares nutmeg");
        LegacyKey("N");
        yield return Until(() => movement.isWaitingForTackleRoll, "accepted nutmeg opens the actual duel");
        Assert.That(movement.nutmegVictim, Is.SameAs(players["Soares"]));
        movement.PerformTackleDiceRoll(true, 5); yield return AssertLegacyOutfieldDefenderPresentation();
        movement.PerformTackleDiceRoll(false, 4);
        yield return Until(() => loose.isActivated && loose.isWaitingForDirectionRoll, "nutmeg tie transfers into continuous scatter controls");
        Assert.That(loose.PresentationContext, Is.EqualTo(LooseBallPresentationContext.GroundDuelResolution));
        Assert.That(duelPanel.IsOpen && !duelPanel.IsComplete && duelPanel.scatterContinuationContent.activeSelf, Is.True);
        loose.PerformDirectionRoll(3);
        yield return Until(() => loose.isWaitingForDistanceRoll, "one-hex northwest return direction reaches its distance roll");
        loose.PerformDistanceRoll(1);
        yield return Until(() => !loose.isWaitingForDistanceRoll, "continuous tackle scatter consumes distance without a completed-result countdown");
        Assert.That(duelPanel.IsComplete, Is.False, "Distance continues into flight without a completed-result gate.");
        yield return Until(() => !loose.isActivated && movement.isWaitingForSnapshotDecision, "return to the same dribbler uses movement's exhausted-pace snapshot handling");
        Assert.That(match.LastTokenToTouchTheBallOnPurpose, Is.SameAs(yaneva));
        Assert.That(movement.movedTokens, Does.Contain(yaneva));
        Assert.That(match.shotManager.isWaitingForSnapshotDecisionFromLoose, Is.False);
        LegacyKey("X");
        yield return Until(() => movement.attackersMoved == 1 && movement.isAwaitingTokenSelection, "declined tie-back snapshot registers the dribbler once and resumes attack");
        Assert.That(movement.isMovementPhaseAttack, Is.True);
    }
}
