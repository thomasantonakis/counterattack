using System.Collections;
using System.Linq;
using NUnit.Framework;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunRepeatedFoulTackleLegacy(bool dangerousOriginalPosition)
    {
        yield return PrepareLegacyInjuredDribbler(dangerousOriginalPosition ? 6 : 1);
        var players = match.playerTokenManager.allTokens.ToDictionary(t => t.playerName);
        var dribbler = players["Yaneva"]; var paterson = players["Paterson"]; var gilbert = players["Gilbert"];
        Assert.That(paterson.isBooked, Is.EqualTo(dangerousOriginalPosition));
        LegacyKey("X");
        Assert.That(movement.attackersMoved, Is.EqualTo(4));
        input.TryExecuteSyntheticHexClick(paterson.currentHex, "Legacy migration", out _);
        Assert.That(movement.isWaitingForTackleDecisionWithoutMoving, Is.True);
        Assert.That(movement.isWaitingForTackleDecision, Is.False);
        if (!dangerousOriginalPosition)
        {
            // Preserve the intended lenient-advantage branch using a real move
            // to the front of the dribbler. The original behind-the-player
            // position now requires at least a yellow under the dangerous rule.
            input.TryExecuteSyntheticHexClick(Hex(6, 3), "Legacy migration", out _);
            yield return Until(() => movement.isWaitingForTackleDecision && !movement.isPlayerMoving,
                "Paterson moves into a legal front tackle position");
            Assert.That(movement.isWaitingForTackleDecisionWithoutMoving, Is.False);
        }
        LegacyKey("T");
        Assert.That(movement.movedTokens.Count, Is.EqualTo(2));
        Assert.That(movement.attackersMoved, Is.EqualTo(4));
        Assert.That(movement.defendersMoved, Is.EqualTo(0));
        Assert.That(movement.isWaitingForTackleRoll, Is.True);
        Assert.That(movement.selectedDefender, Is.SameAs(paterson));
        bool patersonDanger = dribbler.currentHex.IsDangerousTacklingPosition(match.homeTeamDirection, paterson.currentHex);
        movement.PerformTackleDiceRoll(true, 1);
        yield return AssertLegacyOutfieldDefenderPresentation();
        Assert.That(movement.defendersMoved, Is.EqualTo(0));
        movement.PerformTackleDiceRoll(false, new RollInputOverride { hasOverride = true, roll = 6, isJackpot = false });
        yield return Until(() => movement.isWaitingForYellowCardRoll, "tackle natural one opens booking without completing the defender move");
        Assert.That(movement.defendersMoved, Is.EqualTo(0));
        movement.PerformLeniencyTest(1);
        yield return Until(() => movement.isWaitingForInjuryRoll, "lenient tackle foul proceeds to resilience");
        Assert.That(movement.defendersMoved, Is.EqualTo(0));
        movement.PerformInjuryTest(1);
        yield return DismissLegacyDuel(false);
        if (dangerousOriginalPosition)
        {
            Assert.That(patersonDanger, Is.True);
            yield return Until(() => match.freeKickManager.isWaitingForKickerSelection || match.finalThirdManager.isActivated,
                "minimum dangerous-tackle yellow sends off the booked defender and forces the foul");
            Assert.That(paterson.isSentOff, Is.True);
            Assert.That(match.gameData.stats.GetPlayerStats(paterson).redCards, Is.EqualTo(1));
            Assert.That(movement.isWaitingForFoulDecision || movement.isWaitingForReposition || movement.isActivated, Is.False);
            yield return ForfeitLegacyFinalThirds();
            yield return Until(() => match.freeKickManager.isWaitingForKickerSelection, "forced free kick reaches actual kicker selection");
            Assert.That(match.freeKickManager.remainingDefenderMoves, Is.EqualTo(6));
            Assert.That(ball.GetCurrentHex(), Is.SameAs(Hex(5, 2)));
            Assert.That(match.gameData.stats.GetPlayerStats(dribbler).groundDuelsWon, Is.EqualTo(1));
            yield break;
        }
        Assert.That(patersonDanger, Is.False);
        yield return Until(() => movement.isWaitingForFoulDecision, "tackle foul preserves the advantage choice");
        Assert.That(movement.defendersMoved, Is.EqualTo(0));
        LegacyKey("A");
        yield return Until(() => movement.isWaitingForReposition, "tackle foul Play On grants attacker reposition");
        Assert.That(movement.defendersMoved, Is.EqualTo(0));
        Assert.That(movement.stunnedTokens.Count, Is.EqualTo(0));
        Assert.That(movement.movedTokens.Count, Is.EqualTo(2));
        Assert.That(movement.movedTokens, Does.Contain(paterson));
        input.TryExecuteSyntheticHexClick(Hex(6, 2), "Legacy migration", out _);
        yield return Until(() => movement.defendersMoved == 1 && movement.isAwaitingTokenSelection, "fouling defender's move completes after attacker reposition");
        Assert.That(movement.isWaitingForInterceptionDiceRoll, Is.False);
        input.TryExecuteSyntheticHexClick(gilbert.currentHex, "Legacy migration", out _);
        Assert.That(movement.isAwaitingHexDestination, Is.True);
        Assert.That(movement.isWaitingForTackleDecision || movement.isWaitingForTackleDecisionWithoutMoving, Is.False);
        input.TryExecuteSyntheticHexClick(Hex(5, 2), "Legacy migration", out _);
        yield return Until(() => movement.isWaitingForTackleDecision && !movement.isPlayerMoving, "Gilbert moves adjacent for a new tackle");
        Assert.That(movement.isAwaitingHexDestination || movement.isWaitingForTackleDecisionWithoutMoving, Is.False);
        LegacyKey("T");
        Assert.That(movement.isWaitingForTackleRoll, Is.True);
        Assert.That(match.currentState, Is.EqualTo(MatchManager.GameState.MovementPhase));
        Assert.That(match.LastTokenToTouchTheBallOnPurpose, Is.SameAs(dribbler));
        Assert.That(match.PreviousTokenToTouchTheBallOnPurpose, Is.SameAs(players["Cafferata"]));
        bool gilbertDanger = dribbler.currentHex.IsDangerousTacklingPosition(match.homeTeamDirection, gilbert.currentHex);
        movement.PerformTackleDiceRoll(true, new RollInputOverride { hasOverride = true, roll = 6, isJackpot = false });
        yield return AssertLegacyOutfieldDefenderPresentation();
        movement.PerformTackleDiceRoll(false, 2);
        yield return DismissLegacyDuel(false);
        yield return Until(() => movement.isWaitingForReposition, "successful new tackle offers the defender a reposition");
        LegacyKey("X");
        yield return Until(() => match.currentState == MatchManager.GameState.SuccessfulTackle && !movement.isActivated, "skipped defender reposition completes the successful tackle");
        Assert.That(match.LastTokenToTouchTheBallOnPurpose, Is.SameAs(gilbert));
        Assert.That(match.PreviousTokenToTouchTheBallOnPurpose, Is.Null);
        Assert.That(movement.isAvailable && ground.isAvailable && high.isAvailable && longBall.isAvailable, Is.True);
        Assert.That(ftp.isAvailable || ground.isActivated || ftp.isActivated || high.isActivated || longBall.isActivated, Is.False);
        var attackerStats = match.gameData.stats.GetPlayerStats(dribbler);
        var defenderStats = match.gameData.stats.GetPlayerStats(gilbert);
        Assert.That(attackerStats.groundDuelsInvolved, Is.EqualTo(2));
        Assert.That(defenderStats.groundDuelsInvolved, Is.EqualTo(1));
        Assert.That(defenderStats.groundDuelsWon, Is.EqualTo(1));
        Assert.That(defenderStats.tacklesMade, Is.EqualTo(1));
        Assert.That(attackerStats.dribblesMade, Is.EqualTo(1));
        Assert.That(defenderStats.possessionWon, Is.EqualTo(1));
        Assert.That(attackerStats.possessionLost, Is.EqualTo(1));
        // Independent exact probabilities for Dribbling 2 versus Tackling 1/3:
        // ordinary faces 1–5 have mass 1/6; numeric six/jackpot each 1/12;
        // ties weight 1/6 to xDribble and 5/6 to xTackle, fouls to xDribble.
        // These constants retain the original oracle without duplicating a dice resolver.
        float patersonXD = patersonDanger ? 59f / 96 : 511f / 864;
        float patersonXT = patersonDanger ? 37f / 96 : 353f / 864;
        float gilbertXD = gilbertDanger ? 389f / 864 : 107f / 288;
        float gilbertXT = gilbertDanger ? 475f / 864 : 181f / 288;
        Assert.That(attackerStats.xDribbles, Is.EqualTo(patersonXD + gilbertXD).Within(.0001f));
        Assert.That(defenderStats.xTackles, Is.EqualTo(gilbertXT).Within(.0001f));
        Assert.That(match.gameData.stats.awayTeamStats.totalXTackles, Is.EqualTo(patersonXT + gilbertXT).Within(.0001f));
        Assert.That(match.gameData.stats.homeTeamStats.totalXDribbles, Is.EqualTo(patersonXD + gilbertXD).Within(.0001f));
        Assert.That(match.gameData.stats.awayTeamStats.totalTacklesMade, Is.EqualTo(1));
        Assert.That(match.gameData.stats.homeTeamStats.totalDribblesMade, Is.EqualTo(1));
        Assert.That(attackerStats.groundDuelsWon, Is.EqualTo(1));
        Assert.That(match.gameData.stats.homeTeamStats.totalGroundDuelsWon, Is.EqualTo(1));
    }
}
