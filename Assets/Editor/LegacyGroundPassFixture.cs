using System.Collections;
using NUnit.Framework;
using UnityEngine;

// Pass-phase assertions migrated from legacy 002/002b/003/004. The common legacy
// kickoff UI startup is tracked separately; these cases begin from a real completed
// movement outcome and use the normal input/action/roll entry points.
public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunGroundPassLegacy(string branch)
    {
        Board(1);
        attacker.name = attacker.playerName = "Cafferata";
        defender.name = defender.playerName = "Paterson";
        Place(attacker, Hex(0)); ball.PlaceAtCell(attacker.currentHex);
        var receiver = Token("Delgado", true, false, 10);
        Place(receiver, Hex(10));
        // Keep the receiver onside using real defender positions. The seeded legacy
        // match had a full back line; the isolated board needs that second-last player.
        if (branch == "preview_confirm") Place(defender, Hex(11, 10));
        Subscribe(ground); Subscribe(match.finalThirdManager);
        if (branch == "forfeit_cycle") Subscribe(movement);
        match.BroadcastSafeEndofMovementPhase(countMovementAction: false, triggerFinalThird: false);
        Assert.That(ground.isAvailable, Is.True);
        Assert.That(ground.imposedDistance, Is.EqualTo(11));

        if (branch == "quick_throw")
        {
            Place(defender, Hex(3, 3));
            match.TriggerQuickThrowPass();
            Assert.That(match.currentState, Is.EqualTo(MatchManager.GameState.QuickThrow));
        }
        else
            Assert.That(input.TryExecuteSyntheticKeyPress("P", "Legacy migration", out string error), Is.True, error);
        Assert.That(ground.isActivated && ground.isAwaitingTargetSelection, Is.True);
        Assert.That(ground.isAvailable, Is.False);

        if (branch == "preview_confirm")
        {
            Assert.That(input.TryExecuteSyntheticHexClick(Hex(12, -6), "Legacy migration", out _), Is.False);
            Assert.That(ground.currentTargetHex, Is.Null);
            Assert.That(ground.isAwaitingTargetSelection, Is.True);
            ClickPass(Hex(10)); Assert.That(ground.currentTargetHex, Is.SameAs(Hex(10)));
            ClickPass(Hex(4, -4)); Assert.That(ground.currentTargetHex, Is.SameAs(Hex(4, -4)));
            ClickPass(Hex(10));
            Assert.That(ball.GetCurrentHex(), Is.SameAs(Hex(0)), "Preview/switching must not move the ball.");
            Assert.That(ground.isAwaitingTargetSelection, Is.True);
            ClickPass(Hex(10));
            Assert.That(ground.isAwaitingTargetSelection, Is.False);
            Assert.That(match.currentState, Is.EqualTo(MatchManager.GameState.StandardPass));
            AssertCommittedPassAvailability();
            Assert.That(match.finalThirdManager.isActivated, Is.False, "Final Third cannot start before the pass lands.");
            yield return Until(() => ball.GetCurrentHex() == Hex(10) && !ground.isActivated, "safe ground pass lands and cleans up");
            Assert.That(match.attackHasPossession, Is.True);
            Assert.That(movement.isAvailable && ftp.isAvailable, Is.True);
            Assert.That(ground.isAvailable || high.isAvailable || longBall.isAvailable, Is.False);
            Assert.That(movement.isActivated || ftp.isActivated || high.isActivated || longBall.isActivated, Is.False);
            Assert.That(match.finalThirdManager.isActivated, Is.True);
            yield return ForfeitLegacyFinalThirds();
            Assert.That(match.gameData.stats.GetPlayerStats(attacker).passesAttempted, Is.EqualTo(1));
            Assert.That(match.gameData.stats.GetPlayerStats(attacker).passesCompleted, Is.EqualTo(1));
            yield break;
        }

        if (branch == "quick_throw")
        {
            Assert.That(ground.isQuickThrow, Is.True);
            Assert.That(input.TryExecuteSyntheticHexClick(Hex(12, -6), "Legacy migration", out _), Is.False);
            Assert.That(ground.currentTargetHex, Is.Null);
            Assert.That(input.TryExecuteSyntheticHexClick(Hex(3, 3), "Legacy migration", out _), Is.False);
            Assert.That(ground.currentTargetHex, Is.Null);
            Assert.That(ground.isAwaitingTargetSelection, Is.True);
            ClickPass(Hex(4, 4)); Assert.That(ground.currentTargetHex, Is.SameAs(Hex(4, 4)));
            ClickPass(Hex(10)); Assert.That(ground.currentTargetHex, Is.SameAs(Hex(10)));
            Assert.That(ground.passIsDangerous, Is.False);
            ClickPass(Hex(3, 2));
            Assert.That(ground.currentTargetHex, Is.SameAs(Hex(3, 2)));
            // Medium-mode target-only previews superseded the legacy danger flag
            // expectation in bd4347b. Real danger must still resolve on confirmation.
            Assert.That(ground.passIsDangerous, Is.False);
            Assert.That(ground.isAwaitingTargetSelection, Is.True);
            ClickPass(Hex(3, 2));
            yield return Until(() => ground.isWaitingForDiceRoll, "quick throw confirms into target interception");
            Assert.That(ground.isAwaitingTargetSelection, Is.False);
            Assert.That(ground.passIsDangerous, Is.True);
            Assert.That(ground.diceRollsPending, Is.EqualTo(1));
            Assert.That(ground.defendingHexes, Does.Contain(defender.currentHex));
            yield break;
        }

        // An unobstructed vertical pass has three defenders' ZOIs in distance order.
        var first = Token("Gilbert", false, false, 4); Place(first, Hex(1, 1));
        Place(defender, Hex(-1, 3));
        var third = Token("Holm", false, false, 5); Place(third, Hex(1, 5));
        HexCell target = Hex(0, 6);
        ClickPass(target);
        Assert.That(ground.currentTargetHex, Is.SameAs(target));
        Assert.That(ground.passIsDangerous, Is.False, "Medium previews do not reveal interception calculations.");
        ClickPass(target);
        yield return Until(() => ground.isWaitingForDiceRoll, "dangerous ground pass opens interception rolls");
        Assert.That(ground.passIsDangerous, Is.True);
        Assert.That(ground.diceRollsPending, Is.EqualTo(3));
        Assert.That(ground.defendingHexes[0], Is.SameAs(first.currentHex));
        Assert.That(match.gameData.stats.GetPlayerStats(attacker).passesAttempted, Is.EqualTo(1));

        ground.PerformGroundInterceptionDiceRoll(1);
        yield return Until(() => ground.isWaitingForDiceRoll && GroundInteractionsRemaining == 2, "first interception fails before the second is offered");
        Assert.That(CurrentGroundDefender, Is.SameAs(defender.currentHex));
        if (branch == "second_interceptor")
        {
            ground.PerformGroundInterceptionDiceRoll(6);
            yield return Until(() => ball.GetCurrentHex() == defender.currentHex && !ground.isActivated, "second interceptor recovers and pass cleans up");
            Assert.That(match.teamInAttack, Is.EqualTo(MatchManager.TeamInAttack.Away));
            Assert.That(match.attackHasPossession, Is.True);
            Assert.That(match.LastTokenToTouchTheBallOnPurpose, Is.SameAs(defender));
            Assert.That(match.currentState, Is.EqualTo(MatchManager.GameState.AnyOtherScenario));
            Assert.That(movement.isAvailable && ground.isAvailable && longBall.isAvailable, Is.True);
            Assert.That(ftp.isAvailable || high.isAvailable, Is.False);
            Assert.That(movement.isActivated || ftp.isActivated || high.isActivated || longBall.isActivated, Is.False);
            Assert.That(ground.imposedDistance, Is.EqualTo(6));
            var stats = match.gameData.stats.GetPlayerStats(defender);
            Assert.That(stats.interceptionsAttempted, Is.EqualTo(1));
            Assert.That(stats.interceptionsMade, Is.EqualTo(1));
            Assert.That(stats.xRecoveries, Is.EqualTo(1f / 6f).Within(0.0001f));
            Assert.That(stats.possessionWon, Is.EqualTo(1));
            Assert.That(match.gameData.stats.GetPlayerStats(attacker).possessionLost, Is.EqualTo(1));
            Assert.That(match.gameData.stats.GetPlayerStats(third).interceptionsAttempted, Is.EqualTo(0));
        }
        else
        {
            ground.PerformGroundInterceptionDiceRoll(1);
            yield return Until(() => ground.isWaitingForDiceRoll && GroundInteractionsRemaining == 1, "second failed interception leaves exactly one roll");
            Assert.That(CurrentGroundDefender, Is.SameAs(third.currentHex));
            ground.PerformGroundInterceptionDiceRoll(1);
            yield return Until(() => ball.GetCurrentHex() == target && !ground.isActivated, "all failed interceptions leave the ball in space");
            Assert.That(ground.defendingHexes, Is.Empty);
            Assert.That(match.teamInAttack, Is.EqualTo(MatchManager.TeamInAttack.Home));
            Assert.That(match.attackHasPossession, Is.False);
            Assert.That(match.LastTokenToTouchTheBallOnPurpose, Is.SameAs(attacker));
            Assert.That(match.hangingPassType, Is.EqualTo("ground"));
            Assert.That(movement.isActivated, Is.True);
            Assert.That(movement.isAvailable || ground.isAvailable || ftp.isAvailable || high.isAvailable || longBall.isAvailable, Is.False);
            Assert.That(ftp.isActivated || high.isActivated || longBall.isActivated, Is.False);
            foreach (var player in new[] { first, defender, third })
                Assert.That(match.gameData.stats.GetPlayerStats(player).interceptionsAttempted, Is.EqualTo(1));
        }
        Assert.That(match.gameData.stats.GetPlayerStats(attacker).passesAttempted, Is.EqualTo(1));
        Assert.That(match.gameData.stats.GetPlayerStats(attacker).passesCompleted, Is.EqualTo(0));
        if (branch == "forfeit_cycle")
        {
            AssertFreshLegacyMovement();
            LegacyKey("X");
            yield return Until(() => movement.isMovementPhaseDef, "attack forfeit advances to defense");
            Assert.That(movement.attackersMoved, Is.EqualTo(4));
            LegacyKey("X");
            yield return Until(() => movement.isMovementPhase2f2, "defense forfeit advances to two-for-two");
            Assert.That(movement.defendersMoved, Is.EqualTo(5));
            LegacyKey("X");
            yield return Until(() => movement.isActivated && movement.isCommitted && movement.isMovementPhaseAttack,
                "two-for-two forfeit automatically starts the next clean attack phase");
            AssertFreshLegacyMovement();
            Assert.That(movement.movedTokens, Is.Empty);
        }
    }

    private void AssertFreshLegacyMovement()
    {
        Assert.That(movement.isActivated && movement.isCommitted && movement.isMovementPhaseAttack, Is.True);
        Assert.That(movement.isMovementPhaseDef || movement.isMovementPhase2f2, Is.False);
        Assert.That(movement.attackersMoved, Is.EqualTo(0));
        Assert.That(movement.defendersMoved, Is.EqualTo(0));
        Assert.That(movement.attackersMovedIn2f2, Is.EqualTo(0));
        Assert.That(match.attackHasPossession, Is.False);
    }

    private void ClickPass(HexCell target)
        => Assert.That(input.TryExecuteSyntheticHexClick(target, "Legacy migration", out string error), Is.True, error);

    // The path-interaction pipeline now owns the live queue. defendingHexes is
    // populated for target preview and retained until cleanup, so it cannot count
    // remaining rolls. Read state only; all advancement uses real rigged rolls.
    private int GroundInteractionsRemaining => ((ICollection)typeof(GroundBallManager)
        .GetField("pathInteractions", Private).GetValue(ground)).Count;
    private HexCell CurrentGroundDefender => (HexCell)typeof(GroundBallManager)
        .GetField("currentDefenderHex", Private).GetValue(ground);

    private void AssertCommittedPassAvailability()
    {
        Assert.That(movement.isAvailable || ground.isAvailable || ftp.isAvailable || high.isAvailable || longBall.isAvailable, Is.False);
        Assert.That(movement.isActivated || ftp.isActivated || high.isActivated || longBall.isActivated, Is.False);
    }

    private IEnumerator ForfeitLegacyFinalThirds()
    {
        for (int phase = 0; phase < 4 && match.finalThirdManager.isActivated; phase++)
        {
            Assert.That(input.TryExecuteSyntheticKeyPress("Return", "Legacy migration", out string error), Is.True, error);
            yield return null;
        }
        yield return Until(() => !match.finalThirdManager.isActivated, "Final Third forfeit continuation completes");
    }
}
