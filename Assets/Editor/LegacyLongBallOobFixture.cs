using System.Collections;
using NUnit.Framework;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunLongBallOobLegacy(string branch)
    {
        var players = LoadLegacyRoster();
        var oob = Add<OutOfBoundsManager>();
        var throwIn = Add<ThrowInManager>();
        var push = Add<OutOfBoundsPushManager>(); push.Configure(grid, ball);
        oob.ball = ball; oob.hexGrid = grid; oob.freeKickManager = match.freeKickManager;
        oob.throwInManager = throwIn; oob.outOfBoundsPushManager = push;
        oob.playerTokenManager = match.playerTokenManager; oob.finalThirdManager = match.finalThirdManager;
        throwIn.ball = ball; throwIn.hexGrid = grid; throwIn.movementPhaseManager = movement;
        throwIn.groundBallManager = ground; throwIn.headerManager = match.shotManager.headerManager;
        throwIn.finalThirdManager = match.finalThirdManager; throwIn.outOfBoundsPushManager = push;
        longBall.outOfBoundsManager = oob;
        Subscribe(longBall); Subscribe(movement); Subscribe(match.finalThirdManager); Subscribe(match.freeKickManager); Subscribe(throwIn);
        bool prepared = branch.StartsWith("prepared_");
        if (prepared)
        {
            Place(players["Ulisses"], Hex(-8, -4));
            Place(players["Abraham"], Hex(-8, -3)); Place(players["Poulsen"], Hex(16, -4));
            ball.PlaceAtCell(players["Ulisses"].currentHex); match.SetLastToken(players["Ulisses"]);
            match.BroadcastSafeEndofMovementPhase(false, false);
        }
        (int x, int z, int face, int distance, string restart, int rx, int rz) data = branch switch
        {
            "prepared_ne3" => (18, -12, 5, 3, "goal", 16, 0),
            "prepared_s3" => (18, -12, 1, 3, "throw", 18, -12),
            "prepared_se6" => (15, 4, 6, 6, "goal", 16, 0),
            "left_s_throw" => (-18, -12, 1, 6, "throw", -18, -12),
            "left_sw_corner" => (-18, -12, 2, 6, "corner", -18, -12),
            "left_box_nw_corner" => (-14, -5, 3, 6, "corner", -18, -12),
            "left_upper_nw_corner" => (-18, 7, 3, 6, "corner", -18, 12),
            "right_s_throw" => (18, -12, 1, 6, "throw", 18, -12),
            "right_se_goal" => (18, -12, 6, 6, "goal", 16, 0),
            _ => throw new System.ArgumentException(branch)
        };
        LegacyKey("L");
        Assert.That(longBall.isActivated && longBall.isAwaitingTargetSelection, Is.True);
        var target = Hex(data.x, data.z);
        input.TryExecuteSyntheticHexClick(target, "Legacy migration", out _);
        Assert.That(longBall.currentTargetHex, Is.SameAs(target));
        input.TryExecuteSyntheticHexClick(target, "Legacy migration", out _);
        Assert.That(longBall.isWaitingForAccuracyRoll, Is.True);
        LongRoll("PerformAccuracyRoll", 1);
        yield return Until(() => longBall.isWaitingForDirectionRoll, "inaccurate long ball opens direction");
        LongRoll("PerformDirectionRoll", data.face);
        yield return Until(() => longBall.isWaitingForDistanceRoll, "OOB branch direction opens distance");
        LongRoll("PerformDistanceRoll", data.distance);
        if (data.restart == "corner")
        {
            yield return Until(() => match.finalThirdManager.isActivated, "corner offers Final Thirds before kicker selection");
            Assert.That(match.freeKickManager.isWaitingForKickerSelection, Is.False);
            yield return ForfeitLegacyFinalThirds();
        }
        yield return Until(() => !longBall.isActivated && ball.GetCurrentHex() == Hex(data.rx, data.rz)
            && (data.restart == "throw" ? match.currentState == MatchManager.GameState.WaitingForThrowInTaker
                : data.restart == "goal" ? match.currentState == MatchManager.GameState.WaitingForGoalKickFinalThirds
                : match.freeKickManager.isActivated && match.freeKickManager.isCornerKick && match.freeKickManager.isWaitingForKickerSelection),
            $"{branch} awards {data.restart} at ({data.rx},{data.rz})");
        Assert.That(match.teamInAttack, Is.EqualTo(MatchManager.TeamInAttack.Away));
        Assert.That(ball.GetCurrentHex().isOutOfBounds, Is.False);
        if (data.restart == "corner")
        {
            Assert.That(movement.isAvailable || ground.isAvailable || ftp.isAvailable || high.isAvailable || longBall.isAvailable || match.shotManager.isAvailable, Is.False);
            Assert.That(match.gameData.stats.awayTeamStats.totalCorners, Is.EqualTo(1));
            Assert.That(match.gameData.stats.homeTeamStats.totalCorners, Is.EqualTo(0));
        }
        if (data.restart == "goal") Assert.That(match.freeKickManager.isActivated && match.freeKickManager.isCornerKick, Is.False);
    }
}
