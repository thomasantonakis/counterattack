using System.Collections;
using System.Linq;
using NUnit.Framework;
using static LegacyHeaderGoalCatalog;

public sealed partial class GoalkeeperBoxEntryFixture
{
    private IEnumerator PrepareNativeHeaderAtGoal(bool rush, bool original)
    {
        var p = LoadLegacyRoster(); var header = match.shotManager.headerManager;
        var throwIn = CreateLegacyThrowInManager(); AttachAuthoredPanels();
        EnableForAuthoredPresentation(high); EnableForAuthoredPresentation(header);
        EnableForAuthoredPresentation(match.shotManager); EnableForAuthoredPresentation(loose); EnableForAuthoredPresentation(movement);
        Subscribe(match.freeKickManager); yield return AttachLegacyGoalFlow();
        var oob = Add<OutOfBoundsManager>(); oob.hexGrid = grid; oob.ball = ball; oob.freeKickManager = match.freeKickManager;
        oob.throwInManager = throwIn; oob.outOfBoundsPushManager = throwIn.outOfBoundsPushManager;
        oob.playerTokenManager = match.playerTokenManager; oob.finalThirdManager = match.finalThirdManager;
        match.shotManager.outOfBoundsManager = oob; loose.outOfBoundsManager = oob; header.outOfBoundsManager = oob;
        LegacyKey("C"); Assert.That(high.isActivated, Is.True);
        input.TryExecuteSyntheticHexClick(Hex(13, 1), "Legacy migration", out _);
        Assert.That(high.currentTargetHex, Is.SameAs(Hex(13, 1))); Assert.That(high.eligibleAttackers.Count, Is.EqualTo(1));
        input.TryExecuteSyntheticHexClick(Hex(13, 1), "Legacy migration", out _);
        yield return Until(() => high.isWaitingForDefenderSelection, "sole high-pass attacker finishes its automatic approach");
        Assert.That(high.isWaitingForAttackerSelection || high.isWaitingForAttackerMove || high.isWaitingForConfirmation, Is.False);
        Assert.That(high.eligibleAttackers.Count, Is.EqualTo(1));
        var defender = p[original ? "Soares" : "Poulsen"];
        input.TryExecuteSyntheticHexClick(defender.currentHex, "Legacy migration", out _);
        yield return Until(() => high.isWaitingForDefenderMove, "defensive high-pass token selected");
        input.TryExecuteSyntheticHexClick(original ? Hex(14, 1) : Hex(18, 3), "Legacy migration", out _);
        yield return Until(() => high.isWaitingForAccuracyRoll, "defensive high-pass movement finishes before accuracy");
        Assert.That(high.isWaitingForAttackerSelection || high.isWaitingForAttackerMove || high.isWaitingForDefenderSelection || high.isWaitingForDefenderMove, Is.False);
        high.PerformAccuracyRoll(new RollInputOverride { hasOverride = true, roll = 6, isJackpot = false });
        yield return Until(() => keeper.isActivated, "accurate high-pass arrival offers goalkeeper box movement", 120);
        Assert.That(high.isActivated && !header.isActivated, Is.True);
        input.TryExecuteSyntheticHexClick(Hex(16, 1), "Legacy migration", out _);
        yield return Until(() => high.isWaitingForDefGKChallengeDecision && !keeper.isActivated, "box move completes before the separate aerial rush decision");
        if (rush) input.TryExecuteSyntheticHexClick(Hex(15, 2), "Legacy migration", out _); else LegacyKey("X");
        yield return Until(() => !high.isActivated && match.finalThirdManager.isActivated, "rush choice completes into Final Thirds");
        Assert.That(high.gkRushedOut, Is.EqualTo(rush)); yield return ForfeitLegacyFinalThirds();
        yield return Until(() => header.isActivated && header.isWaitingForHeaderAtGoal, "high-pass landing offers actual header-at-goal choice");
        Assert.That(match.finalThirdManager.isActivated || header.isWaitingForAttackerSelection, Is.False);
        Assert.That(header.attEligibleToHead, Does.Contain(p["Yaneva"])); Assert.That(header.attackerWillJump, Does.Contain(p["Yaneva"]));
        input.TryExecuteSyntheticHexClick(original ? Hex(19, 1) : Hex(19, -2), "Legacy migration", out _);
        yield return Until(() => header.isWaitingForDefenderSelection && !header.isWaitingForHeaderAtGoal, "headed-shot target opens defensive nominations");
        if (rush)
        {
            Assert.That(header.defenderWillJump, Does.Contain(p["Kuzmic"]));
            input.TryExecuteSyntheticHexClick(p["Kuzmic"].currentHex, "Legacy migration", out _);
            Assert.That(header.defenderWillJump, Does.Contain(p["Kuzmic"]), "The rushing goalkeeper cannot withdraw from the challenge.");
        }
        if (original)
        { input.TryExecuteSyntheticHexClick(p["Soares"].currentHex, "Legacy migration", out _); LegacyKey("Return"); }
        else LegacyKey("A");
        yield return Until(() => header.isWaitingForHeaderRoll && !header.isWaitingForDefenderSelection, "actual defensive nomination confirmation starts aerial rolls");
    }

    private IEnumerator RollNativeHeaderGoalChallenge(int attackerRoll, int defenderRoll, int? keeperRoll)
    {
        var header = match.shotManager.headerManager;
        string[] names = keeperRoll.HasValue ? new[] { "Yaneva", "Soares", "Kuzmic" } : new[] { "Yaneva", "Soares" };
        int[] rolls = keeperRoll.HasValue ? new[] { attackerRoll, defenderRoll, keeperRoll.Value } : new[] { attackerRoll, defenderRoll };
        for (int i = 0; i < names.Length; i++)
        {
            string expected = names[i];
            yield return Until(() => header.isWaitingForHeaderRoll && header.tokenRolling != null && header.tokenRolling.playerName == expected, $"headed-shot aerial roll belongs to {expected}");
            Assert.That(header.headerChallengePanel.IsOpen, Is.True);
            header.PerformHeaderRoll(new RollInputOverride { hasOverride = true, roll = rolls[i], isJackpot = false });
        }
        var panel = header.headerChallengePanel;
        yield return Until(() => (bool)typeof(HeaderChallengePanelController).GetField("complete", Private).GetValue(panel)
            || loose.isWaitingForDirectionRoll, "aerial result completes or transfers into its continuous scatter controls");
        if (!loose.isWaitingForDirectionRoll) yield return DismissLegacyHeaderResult();
    }

    private IEnumerator ResolveNativeHeaderSave(int roll)
    {
        var shot = match.shotManager;
        yield return Until(() => shot.isActivated && shot.isHeaderAtGoal && shot.isWaitingForGKDiceRoll, "non-jumping goalkeeper receives the actual headed-shot saving roll");
        Assert.That(duelPanel.IsOpen && duelPanel.IsAwaitingRoll, Is.True);
        Assert.That(HeaderChallengePanelController.IsVisible, Is.False);
        Assert.That(shot.isWaitingForShotRoll || shot.isWaitingForBlockDiceRoll, Is.False);
        shot.PerformGKHeaderSave(roll); yield return DismissNativeHeaderKeeperResult();
    }
    private IEnumerator ResolveNativeHeaderHandling(int roll)
    {
        yield return Until(() => match.shotManager.isWaitingforHandlingTest, "saving result releases a separate handling roll");
        Assert.That(duelPanel.IsOpen && duelPanel.IsAwaitingRoll, Is.True);
        match.shotManager.StartCoroutine(match.shotManager.ResolveHandlingTest(roll));
        yield return DismissNativeHeaderKeeperResult();
    }
    private IEnumerator DismissNativeHeaderKeeperResult()
    {
        yield return Until(() => duelPanel.IsComplete || loose.isWaitingForDirectionRoll, "headed-shot keeper result completes or expands into continuous scatter");
        if (loose.isWaitingForDirectionRoll)
        {
            Assert.That(duelPanel.IsOpen && duelPanel.scatterContinuationContent.activeSelf, Is.True);
            Assert.That(loose.IsInstructionExpectingHomeTeam(), Is.EqualTo(loose.causingDeflection.isHomeTeam)); yield break;
        }
        LegacyKey("Return"); Assert.That(duelPanel.IsResultHeld, Is.True);
        LegacyKey("Escape"); Assert.That(duelPanel.IsOpen, Is.False); yield return null;
    }
    private IEnumerator RollNativeHeaderScatter(int direction, int? distance)
    {
        yield return Until(() => loose.isActivated && loose.isWaitingForDirectionRoll, "header scatter waits for direction");
        loose.PerformDirectionRoll(direction);
        yield return Until(() => !loose.isWaitingForDirectionRoll, "header scatter direction consumed");
        if (!distance.HasValue) yield break;
        yield return Until(() => loose.isWaitingForDistanceRoll, "header scatter waits for distance");
        loose.PerformDistanceRoll(distance.Value);
        var panel = match.shotManager.headerManager.headerChallengePanel;
        yield return Until(() => !loose.isWaitingForDistanceRoll || panel.IsOpen
            && (bool)typeof(HeaderChallengePanelController).GetField("complete", Private).GetValue(panel), "header scatter distance resolves or presents its aerial result gate");
        if (panel.IsOpen && (bool)typeof(HeaderChallengePanelController).GetField("complete", Private).GetValue(panel)) yield return DismissLegacyHeaderResult();
        yield return Until(() => !loose.isWaitingForDistanceRoll, "actual header scatter distance is consumed before flight");
    }

    public IEnumerator RunNativeHeaderGoalBranch(int index)
    {
        var branch = BuildHeaderAtGoalBranchDefinitions()[index];
        yield return PrepareNativeHeaderAtGoal(branch.GkRush, false);
        yield return RollNativeHeaderGoalChallenge(branch.AttackerHeaderRoll, branch.DefenderHeaderRoll,
            branch.GkRush ? branch.GoalkeeperHeaderOrAerialRoll : null);
        if (!branch.GkRush && branch.GoalkeeperHeaderOrAerialRoll.HasValue) yield return ResolveNativeHeaderSave(branch.GoalkeeperHeaderOrAerialRoll.Value);
        if (branch.HandlingRoll.HasValue) yield return ResolveNativeHeaderHandling(branch.HandlingRoll.Value);
        if (branch.LooseDirectionRoll.HasValue) yield return RollNativeHeaderScatter(branch.LooseDirectionRoll.Value, branch.LooseDistanceRoll);
        if (branch.ForbiddenLooseInterceptorName != null)
        {
            var clock = System.Diagnostics.Stopwatch.StartNew();
            while (loose.isActivated && !loose.isWaitingForInterceptionRoll)
            { Assert.That(clock.Elapsed.TotalSeconds, Is.LessThan(8)); yield return null; }
            if (loose.isWaitingForInterceptionRoll) Assert.That(loose.potentialInterceptor.playerName, Is.Not.EqualTo(branch.ForbiddenLooseInterceptorName));
        }
        if (branch.LooseInterceptionRoll.HasValue)
        {
            yield return Until(() => loose.isWaitingForInterceptionRoll, "header spill offers its expected recovering token");
            Assert.That(loose.potentialInterceptor.playerName, Is.EqualTo(branch.LooseInterceptionPlayerName));
            loose.PerformInterceptionRoll(branch.LooseInterceptionRoll.Value); yield return DismissNativeHeaderKeeperResult();
        }
        switch (branch.ExpectedOutcome)
        {
            case HeaderAtGoalOutcomeExpectation.Goal:
                yield return Until(() => match.goalFlowManager.isActivated, "headed shot or its scatter produces the expected goal");
                Assert.That(match.LastTokenToTouchTheBallOnPurpose.playerName, Is.EqualTo("Yaneva"));
                yield return WaitForLegacyGoalReset();
                Assert.That(match.LastTokenToTouchTheBallOnPurpose, Is.Null, "Post-goal kickoff setup clears the previous action's touch chain.");
                Assert.That(match.gameData.stats.homeTeamStats.totalGoals, Is.EqualTo(1), "Preserve the original team-goal contract; personal attribution is recorded for rules review.");
                Assert.That(match.currentState, Is.EqualTo(MatchManager.GameState.PostGoalKickOffSetup));
                Assert.That(match.goalFlowManager.LastCompletedGoalSide, Is.EqualTo(1));
                var players = match.playerTokenManager.allTokens.ToDictionary(t => t.playerName);
                Assert.That(players["Yaneva"].currentHex.coordinates.x, Is.LessThan(0)); Assert.That(players["Kuzmic"].currentHex.coordinates.x, Is.GreaterThan(0)); break;
            case HeaderAtGoalOutcomeExpectation.CornerKick:
                yield return Until(() => match.finalThirdManager.isActivated, "headed-shot corner offers Final Thirds before setup", 1200);
                Assert.That(match.freeKickManager.isWaitingForKickerSelection, Is.False); yield return ForfeitLegacyFinalThirds();
                AssertNativeRestartAvailability();
                yield return Until(() => match.freeKickManager.isActivated && match.freeKickManager.isCornerKick && match.freeKickManager.isWaitingForKickerSelection,
                    "header corner reaches actual kicker selection after Final Thirds");
                Assert.That(movement.isAvailable || ground.isAvailable || ftp.isAvailable || high.isAvailable || longBall.isAvailable || match.shotManager.isAvailable, Is.False);
                Assert.That(match.gameData.stats.homeTeamStats.totalCorners, Is.EqualTo(1)); Assert.That(match.gameData.stats.awayTeamStats.totalCorners, Is.Zero); break;
            case HeaderAtGoalOutcomeExpectation.MovementPhase:
                yield return Until(() => movement.isActivated || match.finalThirdManager.isActivated, "headed-shot loose ball continues into movement or Final Thirds"); break;
            case HeaderAtGoalOutcomeExpectation.SaveAndHold:
                yield return Until(() => match.shotManager.isWaitingForSaveandHoldScenario, "headed shot is held for goalkeeper distribution"); break;
        }
        if (branch.ExpectedBallHex.HasValue) Assert.That(ball.GetCurrentHex(), Is.SameAs(Hex(branch.ExpectedBallHex.Value.x, branch.ExpectedBallHex.Value.y)));
    }
}
