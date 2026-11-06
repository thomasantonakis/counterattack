using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunLongBallBoxLegacy(string branch)
    {
        Board(1); Invoke(grid, "LoadPrecomputedShootingPaths");
        attacker.name = attacker.playerName = "Ulisses"; attacker.highPass = 6;
        Place(attacker, Hex(-8, -4)); ball.PlaceAtCell(attacker.currentHex);
        defender.name = defender.playerName = "Delgado"; Place(defender, Hex(1, -10));
        var adjacent = Token("Abraham", false, false, 4); Place(adjacent, Hex(-8, -3));
        var poulsen = Token("Poulsen", false, false, 11); Place(poulsen, Hex(16, -4));
        var receiver = Token("Yaneva", true, false, 10); Place(receiver, Hex(10));
        keeper.gkPushManager = Add<GKPushManager>(); keeper.gkPushManager.Configure(grid, ball);
        Subscribe(longBall); Subscribe(movement); Subscribe(match.finalThirdManager);
        match.BroadcastSafeEndofMovementPhase(false, false);
        LegacyKey("L");
        var intended = Hex(10, -6);
        input.TryExecuteSyntheticHexClick(intended, "Legacy migration", out _);
        Assert.That(longBall.currentTargetHex, Is.SameAs(intended));
        input.TryExecuteSyntheticHexClick(intended, "Legacy migration", out _);
        Assert.That(longBall.isWaitingForAccuracyRoll, Is.True);
        Assert.That(longBall.GetInstructions(), Does.Contain("4+"));
        LongRoll("PerformAccuracyRoll", 1);
        yield return Until(() => longBall.isWaitingForDirectionRoll, "opposite-third inaccurate pass opens direction");
        LongRoll("PerformDirectionRoll", branch == "receiver_choice" ? 4 : 5); // N / NE die faces, translated from legacy indexes 3 / 4.
        yield return Until(() => longBall.isWaitingForDistanceRoll, "opposite-third scatter opens distance");
        LongRoll("PerformDistanceRoll", branch == "keeper_collect" ? 4 : 6);
        var landing = branch == "receiver_choice" ? Hex(10) : branch == "keeper_collect" ? Hex(14, -4) : Hex(16, -3);
        if (branch != "receiver_choice")
        {
            yield return Until(() => keeper.isActivated, "resolved box entry offers its free goalkeeper move");
            Assert.That(ball.GetCurrentHex(), Is.SameAs(landing));
            Assert.That(grid.highlightedHexes, Does.Contain(Hex(15, -1)));
            input.TryExecuteSyntheticHexClick(Hex(15, -1), "Legacy migration", out _);
            yield return Until(() => !keeper.isActivated && !keeper.IsResolvingMove && awayGK.currentHex == Hex(15, -1),
                "goalkeeper completes its free box move");
        }
        if (branch == "box_intercept_miss")
        {
            yield return Until(() => LongInterceptionPending, "Poulsen is offered the recalculated landing interception");
            var candidates = (List<HexCell>)typeof(LongBallManager).GetField("interceptingDefenders", Private).GetValue(longBall);
            Assert.That(candidates, Is.EquivalentTo(new[] { poulsen.currentHex }));
            var flow = (IEnumerator)typeof(LongBallManager).GetMethod("PerformInterceptionCheck", Private, null,
                new[] { typeof(HexCell), typeof(int?) }, null).Invoke(longBall, new object[] { landing, (int?)1 });
            longBall.StartCoroutine(flow);
        }
        yield return Until(() => longBall.isWaitingForDefLBMove, "long-ball goalkeeper pace response follows landing/free move/interception");
        Assert.That(ball.GetCurrentHex(), Is.SameAs(landing));
        Assert.That(LongInterceptionPending, Is.False);
        if (branch == "keeper_collect")
        {
            input.TryExecuteSyntheticHexClick(landing, "Legacy migration", out _);
            yield return Until(() => awayGK.currentHex == landing && match.LastTokenToTouchTheBallOnPurpose == awayGK
                && match.shotManager.isWaitingForSaveandHoldScenario, "goalkeeper pace collection enters Save-and-Hold");
            Assert.That(match.finalThirdManager.isActivated || match.shotManager.isActivated, Is.False);
            var stats = match.gameData.stats.GetPlayerStats(awayGK);
            Assert.That(stats.possessionWon, Is.EqualTo(1));
            Assert.That(stats.xRecoveries, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(match.gameData.stats.awayTeamStats.totalXRecoveries, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(match.gameData.stats.awayTeamStats.totalPossessionWon, Is.EqualTo(1));
            Assert.That(match.gameData.stats.GetPlayerStats(attacker).possessionLost, Is.EqualTo(1));
            yield break;
        }
        if (branch == "box_intercept_miss")
        {
            var stats = match.gameData.stats.GetPlayerStats(poulsen);
            Assert.That(stats.interceptionsAttempted, Is.EqualTo(1));
            Assert.That(stats.interceptionsMade, Is.EqualTo(0));
            Assert.That(stats.xRecoveries, Is.EqualTo(1f / 6f).Within(0.0001f));
        }
        LegacyKey("X");
        yield return Until(() => !longBall.isActivated && match.finalThirdManager.isActivated, "completed long ball offers its Final Third continuation");
        if (branch == "receiver_choice") Assert.That(match.LastTokenToTouchTheBallOnPurpose, Is.SameAs(receiver));
        yield return ForfeitLegacyFinalThirds();
        Assert.That(ground.isAvailable || ftp.isAvailable || high.isAvailable || longBall.isAvailable
            || ground.isActivated || ftp.isActivated || high.isActivated || longBall.isActivated, Is.False);
        if (branch == "receiver_choice")
        {
            Assert.That(match.attackHasPossession, Is.True);
            Assert.That(movement.isAvailable && !movement.isActivated && !movement.isCommitted, Is.True);
            Assert.That(match.shotManager.isAvailable && !match.shotManager.isActivated, Is.True);
            Assert.That(match.shotManager.GetInstructions(), Does.Contain("Press [S] to take a Snapshot"));
            Assert.That(match.shotManager.GetInstructions(), Does.Not.Contain("take a Shot"));
            Assert.That(match.shotManager.GetInstructions(), Does.Not.Contain("to Shoot"));
        }
        else
        {
            Assert.That(match.attackHasPossession, Is.False);
            Assert.That(movement.isActivated && movement.isCommitted && !movement.isAvailable, Is.True);
        }
    }
}
