using System.Collections;
using NUnit.Framework;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunDefensiveControlLegacy(string branch)
    {
        var players = LoadLegacyRoster(); var header = match.shotManager.headerManager;
        Subscribe(high); Subscribe(movement); Subscribe(match.finalThirdManager); Subscribe(header);
        LegacyKey("C");
        input.TryExecuteSyntheticHexClick(Hex(6, 8), "Legacy migration", out _);
        Assert.That(high.currentTargetHex, Is.SameAs(Hex(6, 8)));
        Assert.That(high.eligibleAttackers.Count, Is.EqualTo(2));
        input.TryExecuteSyntheticHexClick(Hex(6, 8), "Legacy migration", out _);
        yield return Until(() => high.isWaitingForAttackerSelection, "defensive landing high pass confirms its two-receiver target");
        Assert.That(high.lockedAttacker, Is.Null);
        Assert.That(high.intendedTargetHex, Is.SameAs(Hex(6, 8)));
        input.TryExecuteSyntheticHexClick(players["Kalla"].currentHex, "Legacy migration", out _);
        yield return Until(() => players["Kalla"].currentHex == Hex(6, 8) && high.isWaitingForDefenderSelection,
            "selected receiver moves onto the committed high-pass target");
        AssertHighPassMovementSelection(false, false);
        input.TryExecuteSyntheticHexClick(players["Vladoiu"].currentHex, "Legacy migration", out _);
        AssertHighPassMovementSelection(false, true);
        input.TryExecuteSyntheticHexClick(Hex(1, 3), "Legacy migration", out _);
        yield return Until(() => players["Vladoiu"].currentHex == Hex(1, 3) && high.isWaitingForAccuracyRoll,
            "defensive high-pass movement ends before accuracy");
        high.PerformAccuracyRoll(1);
        yield return Until(() => high.isWaitingForDirectionRoll, "failed defensive-landing high pass opens scatter");
        Assert.That(grid.highlightedHexes, Has.No.Member(high.currentTargetHex));
        high.PerformDirectionRoll(2);
        yield return Until(() => high.isWaitingForDistanceRoll, "southwest high-pass direction resolves");
        Assert.That(high.isWaitingForDirectionRoll || high.isWaitingForAccuracyRoll, Is.False);
        high.PerformDistanceRoll(3);
        yield return Until(() => !high.isActivated && header.isWaitingForControlOrHeaderDecisionDef,
            "high pass lands among defenders and offers Header or Ball Control", 120);
        Assert.That(header.isActivated, Is.True);
        Assert.That(high.isWaitingForDistanceRoll, Is.False);
        if (branch == "landing") yield break;
        AttachAuthoredPanels(); EnableForAuthoredPresentation(header); EnableForAuthoredPresentation(loose);
        var mcnulty = players["McNulty"];
        LegacyKey(branch == "head" ? "H" : "B");
        yield return Until(() => header.iswaitingForChallengeWinnerSelection,
            "defensive free header or control selects its actual eligible winner");
        Assert.That(header.isWaitingForControlOrHeaderDecisionDef, Is.False);
        Assert.That(header.defenderWillJump, Is.Empty);
        input.TryExecuteSyntheticHexClick(mcnulty.currentHex, "Legacy migration", out _);
        if (branch == "head")
        {
            yield return Until(() => header.isWaitingForHeaderTargetSelection, "unchallenged McNulty header opens target selection");
            Assert.That(header.defenderWillJump, Is.EqualTo(new[] { mcnulty }));
            input.TryExecuteSyntheticHexClick(players["Marell"].currentHex, "Legacy migration", out _);
            yield return Until(() => !header.isActivated && ball.GetCurrentHex() == players["Marell"].currentHex,
                "defensive free header reaches Marell");
            yield return ForfeitLegacyFinalThirds();
            Assert.That(match.teamInAttack, Is.EqualTo(MatchManager.TeamInAttack.Away));
            Assert.That(match.attackHasPossession, Is.True);
            Assert.That(match.LastTokenToTouchTheBallOnPurpose, Is.SameAs(players["Marell"]));
            Assert.That(movement.isAvailable && ftp.isAvailable && longBall.isAvailable, Is.True);
            Assert.That(ground.isAvailable || high.isAvailable || ground.isActivated || high.isActivated
                || movement.isActivated || ftp.isActivated || longBall.isActivated, Is.False);
            yield break;
        }
        yield return Until(() => header.isWaitingForControlRoll, "McNulty is ready for the actual control check");
        Assert.That(header.challengeWinner, Is.SameAs(mcnulty));
        if (branch == "control") yield break;
        _ = header.PerformControlRoll(branch == "success" ? 6 : 1);
        Assert.That(header.isWaitingForControlRoll, Is.False);
        if (branch == "success")
        {
            yield return DismissLegacyDuel(false);
            yield return Until(() => !header.isActivated && ball.GetCurrentHex() == mcnulty.currentHex,
                "successful defensive control attracts the ball to McNulty");
            yield return ForfeitLegacyFinalThirds();
            Assert.That(match.LastTokenToTouchTheBallOnPurpose, Is.SameAs(mcnulty));
            Assert.That(match.teamInAttack, Is.EqualTo(MatchManager.TeamInAttack.Away));
            Assert.That(movement.isAvailable && ground.isAvailable && high.isAvailable && longBall.isAvailable, Is.True);
            Assert.That(ftp.isAvailable || movement.isActivated || ground.isActivated || ftp.isActivated
                || high.isActivated || longBall.isActivated, Is.False);
            yield break;
        }
        yield return Until(() => !header.isActivated && loose.isWaitingForDirectionRoll,
            "failed control transfers directly into the loose-ball direction gate");
        Assert.That(loose.isActivated && duelPanel.IsOpen, Is.True);
        loose.PerformDirectionRoll(4);
        yield return Until(() => loose.isWaitingForDistanceRoll, "northward failed-control scatter awaits distance");
        Assert.That(loose.isWaitingForDirectionRoll, Is.False);
        loose.PerformDistanceRoll(4);
        yield return DismissLegacyDuel(false);
        yield return Until(() => loose.isWaitingForInterceptionRoll, "Kalla gets the loose-ball recovery offer after McNulty fails control");
        Assert.That(loose.potentialInterceptor, Is.SameAs(players["Kalla"]));
        Assert.That(loose.isWaitingForDistanceRoll, Is.False);
        if (branch == "offer") yield break;
        loose.PerformInterceptionRoll(branch == "recover" ? 6 : 1);
        yield return DismissLegacyDuel(false);
        yield return Until(() => !loose.isActivated, "loose ball finishes Kalla's recovery attempt");
        yield return ForfeitLegacyFinalThirds();
        if (branch == "recover")
        {
            Assert.That(ball.GetCurrentHex(), Is.SameAs(players["Kalla"].currentHex));
            Assert.That(match.LastTokenToTouchTheBallOnPurpose, Is.SameAs(players["Kalla"]));
            Assert.That(match.teamInAttack, Is.EqualTo(MatchManager.TeamInAttack.Home));
            Assert.That(match.attackHasPossession, Is.True);
            Assert.That(movement.isAvailable && ground.isAvailable && longBall.isAvailable, Is.True);
            Assert.That(ground.imposedDistance, Is.EqualTo(6));
            Assert.That(ftp.isAvailable || high.isAvailable || movement.isActivated || ground.isActivated
                || ftp.isActivated || high.isActivated || longBall.isActivated, Is.False);
            yield break;
        }
        Assert.That(ball.GetCurrentHex(), Is.SameAs(Hex(5, 9)));
        AssertLegacyHeaderSpaceContinuation();
        Assert.That(movement.isAwaitingTokenSelection, Is.True);
        input.TryExecuteSyntheticHexClick(mcnulty.currentHex, "Legacy migration", out _);
        Assert.That(movement.isAwaitingTokenSelection && movement.isAwaitingHexDestination && movement.isBallPickable, Is.True);
        LegacyKey("V");
        yield return Until(() => movement.isDribblerRunning && movement.remainingDribblerPace == 1
            && mcnulty.currentHex == ball.GetCurrentHex(), "McNulty collects the failed recovery with exactly one pace remaining");
    }
}
