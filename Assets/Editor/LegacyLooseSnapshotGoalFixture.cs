using System.Collections;
using System.Linq;
using NUnit.Framework;

public sealed partial class GoalkeeperBoxEntryFixture
{
    private IEnumerator PrepareLegacyLooseSnapshot()
    {
        var p = LoadLegacyRoster(); var yaneva = p["Yaneva"]; var support = p["Toothnail"]; var soares = p["Soares"];
        AttachAuthoredPanels(); EnableForAuthoredPresentation(movement); EnableForAuthoredPresentation(loose);
        EnableForAuthoredPresentation(match.shotManager); Subscribe(ground); Subscribe(match.finalThirdManager);
        LegacyKey("P"); ClickPass(yaneva.currentHex); ClickPass(yaneva.currentHex);
        yield return Until(() => !ground.isActivated && ball.GetCurrentHex() == yaneva.currentHex, "kickoff pass reaches Yaneva before snapshot audit");
        Assert.That(match.attackHasPossession && movement.isAvailable && ftp.isAvailable, Is.True);
        yield return ForfeitLegacyFinalThirds(); LegacyKey("M");
        input.TryExecuteSyntheticHexClick(support.currentHex, "Legacy migration", out _);
        input.TryExecuteSyntheticHexClick(Hex(13, 5), "Legacy migration", out _);
        yield return Until(() => support.currentHex == Hex(13, 5) && !movement.isPlayerMoving, "first movement positions Toothnail on the later scatter path");
        LegacyKey("X"); Assert.That(movement.isMovementPhaseDef && movement.attackersMoved == 4, Is.True);
        input.TryExecuteSyntheticHexClick(soares.currentHex, "Legacy migration", out _);
        input.TryExecuteSyntheticHexClick(Hex(13, 1), "Legacy migration", out _);
        yield return Until(() => soares.currentHex == Hex(13, 1) && !movement.isPlayerMoving, "Soares prepares the box nutmeg victim through actual movement");
        LegacyKey("X"); Assert.That(movement.isMovementPhase2f2 && movement.defendersMoved == 5, Is.True);
        Assert.That(movement.attackersMoved, Is.EqualTo(4)); LegacyKey("X");
        yield return Until(() => match.finalThirdManager.isActivated, "first movement completes into Final Thirds");
        yield return ForfeitLegacyFinalThirds(); LegacyKey("M");
        input.TryExecuteSyntheticHexClick(yaneva.currentHex, "Legacy migration", out _);
        yield return LegacyDribbleStep(yaneva, Hex(11));
        input.TryExecuteSyntheticHexClick(Hex(12, 1), "Legacy migration", out _);
        yield return Until(() => keeper.isActivated, "second movement dribble enters the box");
        input.TryExecuteSyntheticHexClick(Hex(16, -1), "Legacy migration", out _);
        yield return Until(() => !keeper.isActivated && !keeper.IsResolvingMove && movement.isWaitingForNutmegDecision,
            "keeper movement resumes the actual adjacent Soares nutmeg choice");
        input.TryExecuteSyntheticHexClick(Hex(13, 1), "Legacy migration", out _);
        input.TryExecuteSyntheticHexClick(Hex(12, 1), "Legacy migration", out _);
        LegacyKey("N"); yield return Until(() => movement.isWaitingForTackleRoll, "accepted Soares nutmeg opens its rolls");
        movement.PerformTackleDiceRoll(true, 5); yield return AssertLegacyOutfieldDefenderPresentation();
        movement.PerformTackleDiceRoll(false, 4);
        yield return Until(() => loose.isActivated && loose.isWaitingForDirectionRoll, "nutmeg tie begins actual scatter without a result countdown");
        Assert.That(movement.isNutmegInProgress, Is.False);
        yield return RollLegacyNorthScatter(6);
        yield return Until(() => !loose.isActivated && match.shotManager.isWaitingForSnapshotDecisionFromLoose,
            "direct Toothnail collection offers the generic loose-ball snapshot");
        Assert.That(match.LastTokenToTouchTheBallOnPurpose, Is.SameAs(support));
        Assert.That(match.PreviousTokenToTouchTheBallOnPurpose, Is.Null);
        Assert.That(match.clearPreviousOnNextBallCollection, Is.False);
        Assert.That(movement.isMovementPhaseAttack, Is.True); Assert.That(movement.attackersMoved, Is.Zero);
        Assert.That(movement.movedTokens.Count, Is.EqualTo(1)); Assert.That(movement.movedTokens, Does.Contain(yaneva));
        Assert.That(movement.isWaitingForSnapshotDecision, Is.False);
    }

    public IEnumerator RunLooseSnapshotGoalLegacy(bool takeSnapshot)
    {
        yield return PrepareLegacyLooseSnapshot();
        var p = match.playerTokenManager.allTokens.ToDictionary(t => t.playerName); var shot = match.shotManager;
        if (!takeSnapshot)
        {
            LegacyKey("X"); yield return Until(() => !shot.isActivated && movement.attackersMoved == 1 && movement.isAwaitingTokenSelection,
                "declined generic snapshot resumes attack and counts the exhausted dribbler once");
            Assert.That(match.LastTokenToTouchTheBallOnPurpose, Is.SameAs(p["Toothnail"]));
            Assert.That(movement.isMovementPhaseAttack, Is.True);
            Assert.That(movement.movedTokens.Count, Is.EqualTo(1)); Assert.That(movement.movedTokens, Does.Contain(p["Yaneva"]));
            input.TryExecuteSyntheticHexClick(p["Nazef"].currentHex, "Legacy migration", out _);
            input.TryExecuteSyntheticHexClick(Hex(0, 6), "Legacy migration", out _);
            yield return Until(() => p["Nazef"].currentHex == Hex(0, 6) && !movement.isPlayerMoving, "Nazef can continue after the declined snapshot");
            LegacyKey("X"); Assert.That(movement.attackersMoved, Is.EqualTo(4)); Assert.That(movement.movedTokens.Count, Is.EqualTo(2));
            Assert.That(movement.isMovementPhaseDef && movement.isAwaitingTokenSelection, Is.True);
            input.TryExecuteSyntheticHexClick(p["Soares"].currentHex, "Legacy migration", out _);
            Assert.That(movement.isAwaitingTokenSelection, Is.True);
            input.TryExecuteSyntheticHexClick(Hex(13, -2), "Legacy migration", out _);
            yield return Until(() => p["Soares"].currentHex == Hex(13, -2) && !movement.isPlayerMoving, "Soares continues the defensive movement section");
            LegacyKey("X"); LegacyKey("X");
            yield return Until(() => match.finalThirdManager.isActivated, "completed continuation reaches Final Thirds");
            yield return ForfeitLegacyFinalThirds(); LegacyKey("S");
            Assert.That(shot.isWaitingForShotCommitConfirmation, Is.True, "The original endpoint declares a difficulty-two shot preview.");
            LegacyKey("S");
            yield return Until(() => shot.isActivated && shot.isWaitingForTargetSelection, "original 024b endpoint can declare a full-power shot with Toothnail");
            yield break;
        }
        yield return AttachLegacyGoalFlow(); LegacyKey("S");
        Assert.That(shot.isActivated && shot.isWaitingforBlockerSelection, Is.True);
        input.TryExecuteSyntheticHexClick(p["Soares"].currentHex, "Legacy migration", out _);
        Assert.That(shot.isWaitingforBlockerMovement, Is.True);
        input.TryExecuteSyntheticHexClick(Hex(11, 1), "Legacy migration", out _);
        yield return Until(() => shot.isWaitingForTargetSelection && !shot.isWaitingforBlockerMovement && !shot.isWaitingforBlockerSelection,
            "snapshot defensive reaction finishes before target selection");
        input.TryExecuteSyntheticHexClick(Hex(19, 2), "Legacy migration", out _);
        yield return Until(() => shot.isWaitingForShotRoll && !shot.isWaitingForTargetSelection, "snapshot target resolves the real shooting path");
        shot.StartCoroutine(shot.StartShotRoll(6));
        yield return Until(() => match.goalFlowManager.isActivated && !shot.isActivated && !movement.isActivated,
            "rigged snapshot scores through real shot resolution");
        yield return WaitForLegacyGoalReset();
        Assert.That(match.currentState, Is.EqualTo(MatchManager.GameState.PostGoalKickOffSetup));
        Assert.That(match.finalThirdManager.isActivated, Is.False);
        Assert.That(match.PreviousTokenToTouchTheBallOnPurpose, Is.Null);
        Assert.That(match.gameData.stats.GetPlayerStats(p["Yaneva"]).assists, Is.Zero);
        Assert.That(match.gameData.stats.homeTeamStats.totalAssists, Is.Zero);
        Assert.That(match.gameData.stats.GetPlayerStats(p["Toothnail"]).goals, Is.EqualTo(1));
    }
}
