using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunKickoffLegacy(string branch)
    {
        Board(1); Place(attacker, Hex(0)); ball.PlaceAtCell(attacker.currentHex);
        var kickoff = Add<KickoffManager>(); match.kickoffManager = kickoff; input.kickoffManager = kickoff;
        kickoff.ball = ball; kickoff.hexGrid = grid; kickoff.playerTokenManager = match.playerTokenManager;
        kickoff.freeKickManager = match.freeKickManager; kickoff.groundBallManager = ground;
        kickoff.movementPhaseManager = movement; kickoff.headerManager = match.shotManager.headerManager;
        Subscribe(kickoff); Subscribe(ground);
        match.currentState = MatchManager.GameState.KickOffSetup;
        kickoff.StartPreKickoffPhase();
        var initial = CaptureLegacyStatus();
        input.TryExecuteSyntheticKeyPress("2", "Legacy migration", out _);
        Assert.That(initial.IsEqualTo(CaptureLegacyStatus(), out string reason), Is.True, reason);
        Assert.That(match.currentState, Is.EqualTo(MatchManager.GameState.KickOffSetup));
        Assert.That(ground.isAvailable || ground.isActivated || movement.isAvailable || movement.isActivated, Is.False);
        // d276dfb changed initial confirmation from Space to Enter. Exercise the
        // real current confirmations rather than modifying outcome flags.
        LegacyKey("Return");
        Assert.That(match.currentState, Is.EqualTo(MatchManager.GameState.KickOffSetup));
        LegacyKey("Return");
        Assert.That(match.currentState, Is.EqualTo(MatchManager.GameState.KickoffBlown));
        Assert.That(ground.isAvailable, Is.True);
        Assert.That(ground.isActivated || movement.isAvailable || movement.isActivated, Is.False);
        LegacyKey("P");
        Assert.That(ground.isActivated && ground.isAwaitingTargetSelection, Is.True);
        Assert.That(ground.isAvailable || movement.isAvailable || movement.isActivated, Is.False);
        if (branch == "ignored_input")
        {
            var before = CaptureLegacyStatus();
            input.TryExecuteSyntheticHexClick(Hex(12, -6), "Legacy migration", out _);
            Assert.That(before.IsEqualTo(CaptureLegacyStatus(), out reason), Is.True, reason);
            foreach (var target in new[] { Hex(11, -6), Hex(10, -6) })
            {
                input.TryExecuteSyntheticHexClick(target, "Legacy migration", out _);
                Assert.That(before.IsEqualTo(CaptureLegacyStatus(), out reason,
                    new HashSet<string> { "gbmCurrentTargetHex" }), Is.True, reason);
            }
        }
        yield break;
    }

    // Pure snapshot data helper from the legacy suite; no runner method executes.
    private GameStatusSnapshot CaptureLegacyStatus()
        => new(ground, ftp, high, longBall, movement, match.finalThirdManager,
            match.shotManager.headerManager, loose, null, match.freeKickManager,
            match.shotManager, keeper, null, match.kickoffManager, grid, input);
}
