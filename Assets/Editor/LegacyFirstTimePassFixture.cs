using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunFirstTimePassLegacy(string branch)
    {
        if (branch == "end_options") { yield return RunFtpContinuationLegacy(); yield break; }
        Board(1);
        attacker.name = attacker.playerName = "Cafferata";
        defender.name = defender.playerName = "Delgado";
        Place(attacker, Hex(0)); Place(defender, Hex(1, -10));
        var passer = Token("André Noruega", true, false, 6); Place(passer, Hex(-6, -6));
        var support = Token("Ulisses", true, false, 7); Place(support, Hex(-8, -8));
        var receiver = Token("Relay receiver", true, false, 10); Place(receiver, Hex(-4, -4));
        ball.PlaceAtCell(attacker.currentHex);
        Subscribe(ground); Subscribe(ftp); Subscribe(movement); Subscribe(match.finalThirdManager);
        match.BroadcastSafeEndofMovementPhase(false, false);
        LegacyKey("P"); ClickPass(passer.currentHex); ClickPass(passer.currentHex);
        yield return Until(() => !ground.isActivated && ball.GetCurrentHex() == passer.currentHex, "initial ground relay completes");
        Assert.That(match.gameData.stats.GetPlayerStats(attacker).passesAttempted, Is.EqualTo(1));
        Assert.That(match.gameData.stats.GetPlayerStats(attacker).passesCompleted, Is.EqualTo(1));
        Assert.That(match.LastTokenToTouchTheBallOnPurpose, Is.SameAs(passer));
        Assert.That(ftp.isAvailable && movement.isAvailable, Is.True);
        Assert.That(high.isAvailable || longBall.isAvailable || ground.isAvailable, Is.False);
        if (branch == "reset_movement")
        {
            LegacyKey("M");
            Assert.That(movement.isActivated && movement.isAwaitingTokenSelection, Is.True);
            FtpClick(support.currentHex);
            Assert.That(movement.selectedToken, Is.SameAs(support));
            Assert.That(movement.isAwaitingHexDestination, Is.True);
            match.TriggerStandardPass();
            Assert.That(movement.isActivated || movement.isAwaitingTokenSelection
                || movement.isAwaitingHexDestination || movement.isCommitted, Is.False);
            Assert.That(movement.selectedToken, Is.Null);
            Assert.That(movement.movedTokens, Is.Empty);
            Assert.That(ground.isActivated && ground.isAwaitingTargetSelection, Is.True);
            yield break;
        }
        match.difficulty_level = branch == "easy_hover" ? 1 : branch == "hard_commit" ? 3 : 2;
        LegacyKey("F");
        AssertFtpPhase("target");
        Assert.That(movement.isAvailable, Is.EqualTo(branch != "hard_commit"));
        if (branch == "switch_options")
        {
            FtpClick(receiver.currentHex); AssertFtpPhase("target");
            Assert.That(movement.isAvailable, Is.True);
            LegacyKey("M");
            Assert.That(movement.isAvailable, Is.False);
            Assert.That(movement.isActivated && ftp.isAvailable, Is.True);
            LegacyKey("F"); AssertFtpPhase("target");
            Assert.That(movement.isAvailable, Is.True);
            Assert.That(ftp.currentTargetHex, Is.Null, "Switching back must clear the old FTP preview.");
        }

        HexCell target = branch is "to_player" or "easy_hover" ? receiver.currentHex
            : branch == "auto_recovery" ? Hex(0, -9)
            : branch is "to_space" or "zoi_recovery" ? Hex(-1, -9)
            : branch == "switch_options" ? Hex(-5, -5) : Hex(-5, -6);
        Assert.That(ftp.GetLegalFirstTimePassTargetsForDecisionPreview(), Does.Contain(target));
        if (branch == "easy_hover")
        {
            Invoke(ftp, "OnHoverReceived", null, Hex(6, 6));
            Assert.That(ftp.GetInstructions(), Does.Contain("out of range"));
            Place(defender, Hex(-4, -6));
            Invoke(ftp, "OnHoverReceived", null, Hex(-2, -6));
            Assert.That(ftp.GetInstructions(), Does.Contain("blocked by defender"));
            Place(defender, Hex(1, -10));
            Invoke(ftp, "OnHoverReceived", target.GetOccupyingToken(), target);
            Assert.That(grid.highlightedHexes, Does.Contain(target));
        }
        FtpClick(target);
        if (branch != "hard_commit")
        {
            AssertFtpPhase("target");
            Assert.That(ftp.currentTargetHex, Is.SameAs(target));
            if (branch == "easy_hover")
            {
                AssertOrange(target);
                Invoke(ftp, "OnHoverReceived", null, Hex(-5, -6));
                AssertOrange(target);
            }
            FtpClick(target);
        }
        AssertFtpPhase("attack");
        Assert.That(movement.isAvailable, Is.False);
        Assert.That(match.gameData.stats.GetPlayerStats(passer).passesAttempted, Is.EqualTo(1));
        if (branch is "to_space" or "switch_options")
        {
            FtpClick(support.currentHex); Assert.That(ftp.isWaitingForAttackerMove, Is.True);
            AssertFtpPhase("attack"); FtpClick(branch == "switch_options" ? Hex(-8, -7) : Hex(-7, -8));
        }
        else if (branch == "to_player")
        {
            FtpClick(passer.currentHex); Assert.That(ftp.isWaitingForAttackerMove, Is.True);
            FtpClick(Hex(-6, -7));
        }
        else if (branch is "auto_recovery" or "zoi_recovery")
        {
            FtpClick(attacker.currentHex); FtpClick(Hex(0, 1));
        }
        else LegacyKey("X");
        yield return Until(() => ftp.isWaitingForDefenderSelection, "FTP attacker movement hands control to defense");
        AssertFtpPhase("defense");
        Assert.That(ball.GetCurrentHex(), Is.SameAs(Hex(-6, -6)), "FTP prepass movement must not carry the ball.");
        if (branch is "to_space" or "switch_options")
        {
            FtpClick(defender.currentHex); Assert.That(ftp.isWaitingForDefenderMove, Is.True);
            FtpClick(Hex(1, -8)); AssertFtpPhase("defense");
            Assert.That(ftp.isWaitingForDefenderMove, Is.False, "Unreachable medium-mode move clears selection.");
            FtpClick(defender.currentHex); Assert.That(ftp.isWaitingForDefenderMove, Is.True);
            FtpClick(Hex(1, -9));
        }
        else if (branch == "to_player") { FtpClick(defender.currentHex); FtpClick(Hex(1, -9)); }
        else if (branch is "auto_recovery" or "zoi_recovery")
        {
            FtpClick(defender.currentHex); Assert.That(ftp.isWaitingForDefenderMove, Is.True);
            FtpClick(Hex(0, -9));
        }
        else LegacyKey("X");

        if (branch == "zoi_recovery")
        {
            yield return Until(() => ftp.isWaitingForDiceRoll, "new defender position recalculates FTP interception");
            Assert.That(((ICollection)typeof(FirstTimePassManager).GetField("interceptionCandidates", Private).GetValue(ftp)).Count, Is.EqualTo(1));
            Assert.That(ftp.GetInstructions(), Does.Contain("6"));
            typeof(FirstTimePassManager).GetMethod("PerformFTPInterceptionRolls", Private, null,
                new[] { typeof(int?) }, null).Invoke(ftp, new object[] { (int?)6 });
        }
        yield return Until(() => !ftp.isActivated && !ball.IsCellMoveInProgress, "FTP resolves its final reception and cleanup");
        if (branch is "auto_recovery" or "zoi_recovery")
        {
            Assert.That(ftp.isWaitingForDiceRoll, Is.False);
            Assert.That(ball.GetCurrentHex(), Is.SameAs(defender.currentHex));
            Assert.That(match.LastTokenToTouchTheBallOnPurpose, Is.SameAs(defender));
            Assert.That(match.teamInAttack, Is.EqualTo(MatchManager.TeamInAttack.Away));
            Assert.That(match.currentState, Is.EqualTo(MatchManager.GameState.AnyOtherScenario));
            Assert.That(match.attackHasPossession, Is.True);
            Assert.That(ground.isAvailable && movement.isAvailable && longBall.isAvailable, Is.True);
            Assert.That(ftp.isAvailable || high.isAvailable, Is.False);
            var stats = match.gameData.stats.GetPlayerStats(defender);
            Assert.That(stats.interceptionsAttempted, Is.EqualTo(1)); Assert.That(stats.interceptionsMade, Is.EqualTo(1));
            Assert.That(stats.possessionWon, Is.EqualTo(1));
            Assert.That(stats.xRecoveries, Is.EqualTo(branch == "auto_recovery" ? 1f : 1f / 6f).Within(0.0001f));
            Assert.That(match.gameData.stats.GetPlayerStats(passer).possessionLost, Is.EqualTo(1));
            yield break;
        }
        Assert.That(ball.GetCurrentHex(), Is.SameAs(target));
        Assert.That(movement.isActivated && movement.isCommitted, Is.True);
        Assert.That(movement.isAvailable || ftp.isAvailable || ground.isAvailable || high.isAvailable || longBall.isAvailable, Is.False);
        Assert.That(ftp.isActivated || ground.isActivated || high.isActivated || longBall.isActivated, Is.False);
        Assert.That(match.attackHasPossession, Is.EqualTo(target.GetOccupyingToken() != null));
        if (branch == "excluded_passer")
        {
            Assert.That(match.hangingPassType, Is.EqualTo("ground"));
            Assert.That(match.hangingPassExcludedCollector, Is.SameAs(passer));
            Assert.That(match.LastTokenToTouchTheBallOnPurpose, Is.SameAs(passer));
            var origin = passer.currentHex; FtpClick(origin);
            Assert.That(movement.selectedToken, Is.SameAs(passer));
            Assert.That(grid.highlightedHexes, Has.No.Member(target));
            Assert.That(movement.isBallPickable, Is.False);
            bool committed = movement.isCommitted;
            input.TryExecuteSyntheticHexClick(target, "Legacy migration", out _);
            yield return null;
            Assert.That(passer.currentHex, Is.SameAs(origin)); Assert.That(ball.GetCurrentHex(), Is.SameAs(target));
            Assert.That(movement.isCommitted, Is.EqualTo(committed));
        }
    }

    // FTP's legacy handler changes phase but does not call ConsumeCurrentClick.
    // Dispatch the real input and validate phase/selection/destination below,
    // exactly as the legacy scenarios did; do not infer rejection from that flag.
    private void FtpClick(HexCell target)
        => input.TryExecuteSyntheticHexClick(target, "Legacy migration", out _);

    private void LegacyKey(string key)
        => Assert.That(input.TryExecuteSyntheticKeyPress(key, "Legacy migration", out string error), Is.True, error);

    private void AssertFtpPhase(string phase)
    {
        Assert.That(ftp.isActivated, Is.True);
        Assert.That(ftp.isAvailable || ground.isAvailable || high.isAvailable || longBall.isAvailable, Is.False);
        Assert.That(movement.isActivated || ground.isActivated || high.isActivated || longBall.isActivated, Is.False);
        Assert.That(ftp.isAwaitingTargetSelection, Is.EqualTo(phase == "target"));
        Assert.That(ftp.isWaitingForAttackerSelection, Is.EqualTo(phase == "attack"));
        Assert.That(ftp.isWaitingForDefenderSelection, Is.EqualTo(phase == "defense"));
        Assert.That(ftp.isWaitingForDiceRoll, Is.False);
        Assert.That(match.attackHasPossession, Is.True);
        if (phase == "defense")
        {
            Assert.That(ftp.isWaitingForAttackerMove, Is.False);
            Assert.That(match.currentState, Is.EqualTo(MatchManager.GameState.FirstTimePassDefenderMovement));
        }
        else Assert.That(ftp.isWaitingForDefenderMove, Is.False);
    }

    private static void AssertOrange(HexCell target)
    {
        Color color = target.hexRenderer.material.color;
        Assert.That(color.r, Is.EqualTo(1f).Within(0.06f));
        Assert.That(color.g, Is.EqualTo(0.55f).Within(0.06f));
        Assert.That(color.b, Is.EqualTo(0f).Within(0.06f));
        Assert.That(color.a, Is.EqualTo(1f).Within(0.06f));
    }
}
