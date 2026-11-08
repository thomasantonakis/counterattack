using System.Collections;
using NUnit.Framework;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunLongBallLegacy(string branch)
    {
        if (branch is "receiver_choice" or "keeper_collect" or "box_intercept_miss")
        { yield return RunLongBallBoxLegacy(branch); yield break; }
        Board(1);
        attacker.name = attacker.playerName = "Ulisses"; attacker.highPass = 6;
        Place(attacker, Hex(-8, -4)); ball.PlaceAtCell(attacker.currentHex);
        defender.name = defender.playerName = "Delgado"; Place(defender, Hex(1, -10));
        var adjacent = Token("Abraham", false, false, 4); Place(adjacent, Hex(-8, -3));
        var poulsen = Token("Poulsen", false, false, 11); Place(poulsen, Hex(16, -4));
        Subscribe(longBall); Subscribe(movement); Subscribe(match.finalThirdManager);
        // Independent post-movement board from the legacy shared preparation.
        // Earlier native tests cover collection/movement/forfeit setup separately.
        match.BroadcastSafeEndofMovementPhase(false, false);
        match.difficulty_level = branch == "accurate_easy" ? 1 : branch == "hard_commit" ? 3 : 2;
        LegacyKey("L");
        Assert.That(longBall.isActivated && longBall.isAwaitingTargetSelection, Is.True);
        if (branch == "accurate_easy")
        {
            input.TryExecuteSyntheticHexClick(Hex(-8, 8), "Legacy migration", out _);
            Assert.That(longBall.currentTargetHex, Is.Null);
            Assert.That(longBall.isAwaitingTargetSelection, Is.True);
        }
        var target = Hex(6, -7);
        input.TryExecuteSyntheticHexClick(target, "Legacy migration", out _);
        Assert.That(longBall.currentTargetHex, Is.SameAs(target));
        if (branch != "hard_commit")
            input.TryExecuteSyntheticHexClick(target, "Legacy migration", out _);
        Assert.That(longBall.isAwaitingTargetSelection, Is.False);
        Assert.That(longBall.isWaitingForAccuracyRoll, Is.True);
        Assert.That(match.currentState, Is.EqualTo(MatchManager.GameState.LongBall));
        if (branch == "accurate_easy") Assert.That(longBall.GetInstructions(), Does.Contain("3+"));
        LongRoll("PerformAccuracyRoll", branch == "accurate_easy" ? 3 : 1);
        HexCell expectedLanding = target;
        if (branch != "accurate_easy")
        {
            yield return Until(() => longBall.isWaitingForDirectionRoll, "failed long-ball accuracy opens direction roll");
            if (branch == "hard_commit") yield break;
            // The old helper supplied a zero-based direction index (1 = SW).
            // The current roll entry point takes a die face, so SW is face 2.
            LongRoll("PerformDirectionRoll", 2);
            yield return Until(() => longBall.isWaitingForDistanceRoll, "long-ball direction opens distance roll");
            int distance = branch == "scatter_space" ? 1 : branch == "scatter_direct" ? 5 : 4;
            expectedLanding = distance == 1 ? Hex(5, -8) : distance == 5 ? Hex(1, -10) : Hex(2, -9);
            LongRoll("PerformDistanceRoll", distance);
        }
        yield return Until(() => longBall.isWaitingForDefLBMove, "Pace precedes every in-bounds landing recovery");
        Assert.That(LongInterceptionPending, Is.False);
        LegacyKey("X");
        if (branch is "scatter_interception" or "scatter_miss")
        {
            yield return Until(() => LongInterceptionPending, "resolved scatter destination offers Delgado's interception");
            Assert.That(ball.GetCurrentHex(), Is.SameAs(expectedLanding));
            var roll = (IEnumerator)typeof(LongBallManager).GetMethod("PerformInterceptionCheck", Private, null,
                new[] { typeof(HexCell), typeof(int?) }, null).Invoke(longBall,
                    new object[] { expectedLanding, (int?)(branch == "scatter_interception" ? 6 : 1) });
            longBall.StartCoroutine(roll);
        }
        if (branch is "scatter_interception" or "scatter_direct")
        {
            yield return Until(() => !longBall.isActivated && ball.GetCurrentHex() == defender.currentHex,
                "long-ball recovery completes and broadcasts available actions");
            Assert.That(match.LastTokenToTouchTheBallOnPurpose, Is.SameAs(defender));
            Assert.That(match.teamInAttack, Is.EqualTo(MatchManager.TeamInAttack.Away));
            Assert.That(match.currentState, Is.EqualTo(MatchManager.GameState.AnyOtherScenario));
            Assert.That(match.attackHasPossession, Is.True);
            Assert.That(ground.isAvailable && movement.isAvailable && longBall.isAvailable, Is.True);
            Assert.That(ftp.isAvailable || high.isAvailable || ftp.isActivated || high.isActivated || longBall.isActivated, Is.False);
            var stats = match.gameData.stats.GetPlayerStats(defender);
            Assert.That(stats.possessionWon, Is.EqualTo(1));
            if (branch == "scatter_interception")
            {
                Assert.That(stats.interceptionsAttempted, Is.EqualTo(1));
                Assert.That(stats.interceptionsMade, Is.EqualTo(1));
                Assert.That(stats.xRecoveries, Is.EqualTo(1f / 6f).Within(0.0001f));
            }
        }
        else
        {
            yield return Until(() => !longBall.isActivated && movement.isActivated && movement.isCommitted,
                "forfeited goalkeeper pace response automatically starts movement");
            Assert.That(ball.GetCurrentHex(), Is.SameAs(expectedLanding));
            Assert.That(match.hangingPassType, Is.EqualTo("aerial"));
            Assert.That(match.attackHasPossession, Is.False);
            Assert.That(movement.isAvailable || ground.isAvailable || ftp.isAvailable || high.isAvailable || longBall.isAvailable, Is.False);
            Assert.That(ground.isActivated || ftp.isActivated || high.isActivated || longBall.isActivated, Is.False);
        }
    }

    private bool LongInterceptionPending => (bool)typeof(LongBallManager)
        .GetField("isWaitingForInterceptionRoll", Private).GetValue(longBall);
}
