using System.Collections;
using System.Linq;
using NUnit.Framework;

public sealed partial class GoalkeeperBoxEntryFixture
{
    private IEnumerator PrepareLegacyGoalLineDribbler()
    {
        yield return PrepareLegacyBoxDribble(false, -1);
        var yaneva = match.playerTokenManager.allTokens.Single(t => t.playerName == "Yaneva");
        foreach (var destination in new[] { Hex(13, 1), Hex(14, 2), Hex(15, 2), Hex(16, 3) })
        {
            yield return LegacyDribbleStep(yaneva, destination);
            Assert.That(movement.isWaitingForSnapshotDecision, Is.True);
            Assert.That(movement.attackersMoved, Is.Zero);
            if (movement.remainingDribblerPace > 0)
            {
                Assert.That(movement.isDribblerRunning && movement.isAwaitingHexDestination, Is.True);
                Assert.That(movement.movedTokens, Is.Empty);
            }
        }
        Assert.That(movement.remainingDribblerPace, Is.Zero);
        Assert.That(movement.isDribblerRunning || movement.isAwaitingHexDestination, Is.False);
        Assert.That(movement.movedTokens.Count, Is.EqualTo(1));
        LegacyKey("X");
        yield return Until(() => movement.attackersMoved == 1 && !movement.isWaitingForSnapshotDecision,
            "declined exhausted-pace snapshot registers Yaneva exactly once");
        Assert.That(movement.movedTokens.Count, Is.EqualTo(1));
        Assert.That(movement.isMovementPhaseAttack, Is.True);
        LegacyKey("X"); Assert.That(movement.isMovementPhaseDef, Is.True);
        LegacyKey("X"); Assert.That(movement.isMovementPhase2f2, Is.True);
        LegacyKey("X");
        yield return Until(() => match.finalThirdManager.isActivated, "completed movement offers Final Thirds before the second dribble");
        yield return ForfeitLegacyFinalThirds();
        LegacyKey("M"); input.TryExecuteSyntheticHexClick(yaneva.currentHex, "Legacy migration", out _);
        yield return LegacyDribbleStep(yaneva, Hex(17, 3));
        yield return LegacyDribbleStep(yaneva, Hex(18, 3));
    }

    public IEnumerator RunMovementIntoGoalLegacy(bool reposition)
    {
        yield return PrepareLegacyGoalLineDribbler(); yield return AttachLegacyGoalFlow();
        var players = match.playerTokenManager.allTokens.ToDictionary(t => t.playerName);
        var yaneva = players["Yaneva"];
        if (reposition)
        {
            LegacyKey("X"); LegacyKey("X"); Assert.That(movement.isMovementPhaseDef, Is.True);
            var poulsen = players["Poulsen"];
            input.TryExecuteSyntheticHexClick(poulsen.currentHex, "Legacy migration", out _);
            input.TryExecuteSyntheticHexClick(Hex(18, 2), "Legacy migration", out _);
            yield return Until(() => movement.isWaitingForTackleDecision && !movement.isPlayerMoving, "Poulsen challenges the dribbler beside the goal line");
            LegacyKey("T"); Assert.That(movement.isWaitingForTackleDecision, Is.False);
            Assert.That(movement.isWaitingForTackleRoll, Is.True);
            movement.PerformTackleDiceRoll(true, 4); yield return AssertLegacyOutfieldDefenderPresentation();
            movement.PerformTackleDiceRoll(false, 4); yield return DismissLegacyDuel(false);
            yield return Until(() => movement.isWaitingForReposition, "winning attacker can reposition into the goal");
        }
        input.TryExecuteSyntheticHexClick(reposition ? Hex(19, 2) : Hex(19, 3), "Legacy migration", out _);
        yield return Until(() => match.goalFlowManager.isActivated && !movement.isActivated, "actual dribble or reposition into a goal starts goal flow and ends movement");
        yield return WaitForLegacyGoalReset();
        Assert.That(match.gameData.stats.homeTeamStats.totalGoals, Is.EqualTo(1));
        Assert.That(match.gameData.stats.GetPlayerStats(yaneva).goals, Is.EqualTo(1));
        Assert.That(match.currentState, Is.EqualTo(MatchManager.GameState.PostGoalKickOffSetup));
        Assert.That(match.finalThirdManager.isActivated, Is.False);
    }
}
