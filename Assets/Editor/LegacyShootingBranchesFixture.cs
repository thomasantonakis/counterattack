using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using static LegacyShotBranchCatalog;

public sealed partial class GoalkeeperBoxEntryFixture
{
    private string nativeShotShooter = "Yaneva";
    private readonly Dictionary<string, int> shotBlocksAttempted = new(), shotBlocksMade = new(), scatterAttempts = new(), scatterRecoveries = new();
    private readonly Dictionary<string, float> scatterExpectedRecoveries = new();
    private bool expectedBlocked, expectedOnTarget, expectedOffTarget;
    private float? branchExpectedGoals;
    private static void CountBranch(Dictionary<string, int> values, string key) => values[key] = values.TryGetValue(key, out int n) ? n + 1 : 1;

    private IEnumerator PrepareNativeShootingBoard()
    {
        var players = LoadLegacyRoster();
        Place(players["Yaneva"], Hex(8)); Place(players["Kalla"], Hex(15, 1));
        Place(players["Paterson"], Hex(10)); Place(players["McNulty"], Hex(12));
        match.ClearLastTokenChain(); match.SetLastToken(players["Yaneva"]);
        ball.PlaceAtCell(players["Yaneva"].currentHex); match.UpdatePossessionAfterPass(ball.GetCurrentHex());
        var throwIn = CreateLegacyThrowInManager(); AttachAuthoredPanels();
        EnableForAuthoredPresentation(movement); EnableForAuthoredPresentation(match.shotManager); EnableForAuthoredPresentation(loose);
        Subscribe(match.freeKickManager); yield return AttachLegacyGoalFlow();
        var oob = Add<OutOfBoundsManager>(); oob.hexGrid = grid; oob.ball = ball; oob.freeKickManager = match.freeKickManager;
        oob.throwInManager = throwIn; oob.outOfBoundsPushManager = throwIn.outOfBoundsPushManager;
        oob.playerTokenManager = match.playerTokenManager; oob.finalThirdManager = match.finalThirdManager;
        match.shotManager.outOfBoundsManager = oob; loose.outOfBoundsManager = oob;
        match.BroadcastSafeEndofMovementPhase(false, false);
        Assert.That(match.shotManager.isAvailable && movement.isAvailable, Is.True);
        Assert.That(match.shotManager.GetInstructions(), Does.Contain("Press [S] to take a Shot"));
        LegacyKey("S"); Assert.That(match.shotManager.isWaitingForShotCommitConfirmation, Is.True);
        Assert.That(match.shotManager.GetInstructions(), Does.Contain("commit the Shot"));
        LegacyKey("S"); Assert.That(match.shotManager.isActivated && match.shotManager.isWaitingForTargetSelection, Is.True);
        Assert.That(match.shotManager.shotType, Is.EqualTo("fullPower"));
        input.TryExecuteSyntheticHexClick(Hex(19), "Legacy migration", out _);
        yield return Until(() => match.shotManager.isWaitingForBlockDiceRoll || match.shotManager.isWaitingForShotRoll || keeper.isActivated,
            "real aimed shot resolves its first blocker and box-entry offer");
        Assert.That(match.shotManager.targetHex, Is.SameAs(Hex(19)));
    }

    private float NativeShotExpectedGoal()
    {
        var shot = match.shotManager;
        var interactions = (IEnumerable)typeof(ShotManager).GetField("expectedGoalOutfieldBlockInteractions", Private).GetValue(shot);
        var blockers = new List<ExpectedStatsCalculator.ShotBlockerExpectation>();
        foreach (object entry in interactions)
            blockers.Add(new ExpectedStatsCalculator.ShotBlockerExpectation(LegacyShotField<PlayerToken>(entry, "defender"),
                LegacyShotField<int>(entry, "requiredNaturalRoll"), LegacyShotField<int?>(entry, "effectiveTackling")));
        var save = typeof(ShotManager).GetField("expectedGoalGkSaveInteraction", Private).GetValue(shot);
        return ExpectedStatsCalculator.CalculateShotGoalProbability(shot.shooter, 1, blockers,
            save == null ? null : LegacyShotField<PlayerToken>(save, "defender"), save == null ? 0 : LegacyShotField<int?>(save, "gkPenalty") ?? 0);
    }

    public IEnumerator RunNativeShootingBranch(int index)
    {
        var branch = BuildShootingBranchDefinitions()[index];
        yield return PrepareNativeShootingBoard();
        foreach (var step in branch.Steps)
        {
            UnityEngine.Debug.Log($"Native shooting {index + 1}: {step.Type} {step.PlayerName} {step.Roll}");
            yield return ExecuteNativeShootingStep(step, branch.Steps);
        }
        AssertNativeShootingStatistics(branch.Steps);
    }

    private IEnumerator DismissNativeShotResult()
    {
        yield return Until(() => duelPanel.IsComplete || loose.isWaitingForDirectionRoll, "shot result completes or expands into its continuous scatter");
        if (loose.isWaitingForDirectionRoll)
        {
            Assert.That(duelPanel.IsOpen && (duelPanel.scatterContinuationContent.activeSelf || duelPanel.looseBallContent.activeSelf), Is.True,  $"Actual scatter controls remain visible after any goalkeeper pitch handoff. Open={duelPanel.IsOpen}; inline={duelPanel.scatterContinuationContent.activeSelf}; standalone={duelPanel.looseBallContent.activeSelf}; title={duelPanel.title.text}");
            yield break;
        }
        LegacyKey("Escape"); yield return null;
    }

    private IEnumerator ExecuteNativeShootingStep(ShootingBranchStep step, List<ShootingBranchStep> steps)
    {
        var shot = match.shotManager;
        switch (step.Type)
        {
            case ShootingBranchStepType.ShotBlockRoll:
                yield return Until(() => shot.isWaitingForBlockDiceRoll, $"shot waits for {step.PlayerName} block");
                Assert.That(shot.isActivated && !shot.isWaitingForShotRoll && !shot.isWaitingForGKDiceRoll, Is.True);
                Assert.That(match.LastTokenToTouchTheBallOnPurpose.playerName, Is.EqualTo(nativeShotShooter));
                AssertNativeFreeKickDicePresentation();
                if (expectedOffTarget) branchExpectedGoals ??= NativeShotExpectedGoal();
                var block = CurrentLegacyShotInteraction(); var blocker = LegacyShotField<PlayerToken>(block, "defender");
                Assert.That(blocker.playerName, Is.EqualTo(step.PlayerName)); CountBranch(shotBlocksAttempted, step.PlayerName);
                if (step.Roll >= LegacyShotField<int>(block, "requiredNaturalRoll") || step.Roll + (LegacyShotField<int?>(block, "effectiveTackling") ?? blocker.tackling) >= 10)
                { CountBranch(shotBlocksMade, step.PlayerName); expectedBlocked = true; branchExpectedGoals ??= NativeShotExpectedGoal(); }
                shot.StartCoroutine(InvokeLegacyShotRoll("StartShotBlockRoll", step.Roll));
                yield return DismissNativeShotResult(); break;
            case ShootingBranchStepType.ShooterRoll:
                yield return Until(() => shot.isWaitingForShotRoll, "shot waits for the shooter roll");
                AssertNativeFreeKickDicePresentation();
                Assert.That(shot.isActivated, Is.True); Assert.That(shot.shooter.playerName, Does.Contain(step.PlayerName));
                Assert.That(match.LastTokenToTouchTheBallOnPurpose, Is.SameAs(shot.shooter));
                if (step.Roll == 1)
                {
                    expectedOffTarget = true;
                    // Free-kick blocker context is captured after power presentation,
                    // so evaluate its oracle at the first actual block instead.
                    if (shot.shotType != "freeKick") branchExpectedGoals ??= NativeShotExpectedGoal();
                }
                shot.StartCoroutine(shot.StartShotRoll(step.Roll));
                yield return Until(() => !shot.isWaitingForShotRoll, "actual shooter roll is consumed");
                yield return Until(() => duelPanel.IsComplete || shot.isWaitingForGKDiceRoll, "shooter presentation either completes or continues to its saving comparison");
                if (duelPanel.IsComplete)
                {
                    if (shot.shotType == "freeKick" && step.Roll != 1 && shot.totalShotPower >= 9)
                    { Assert.That(duelPanel.outcome.text, Does.Contain("Perfect height")); Assert.That(duelPanel.outcome.text, Does.Contain("blocks skipped")); }
                    LegacyKey("Escape"); yield return null;
                } break;
            case ShootingBranchStepType.GkSaveRoll:
                yield return Until(() => shot.isWaitingForGKDiceRoll, "shot waits for goalkeeper save");
                Assert.That(shot.isActivated, Is.True);
                AssertNativeFreeKickDicePresentation();
                var save = CurrentLegacyShotInteraction(); var gk = LegacyShotField<PlayerToken>(save, "defender");
                Assert.That(gk.playerName, Is.EqualTo(step.PlayerName));
                Assert.That(match.LastTokenToTouchTheBallOnPurpose.playerName, Is.EqualTo(nativeShotShooter));
                branchExpectedGoals ??= NativeShotExpectedGoal();
                int savePower = step.Roll + gk.saving + (LegacyShotField<int?>(save, "gkPenalty") ?? 0);
                if (savePower == shot.totalShotPower) expectedBlocked = true;
                else if (savePower > shot.totalShotPower) expectedOnTarget = true;
                shot.StartCoroutine(InvokeLegacyShotRoll("ResolveGKSavingAttempt", step.Roll, save));
                yield return DismissNativeShotResult(); break;
            case ShootingBranchStepType.HandlingRoll:
                yield return Until(() => shot.isWaitingforHandlingTest, "save opens Handling");
                if (shot.shotType == "freeKick") Assert.That(duelPanel.IsAwaitingRoll && duelPanel.title.text == "Free Kick Shot · Handling" && duelPanel.singlePlayer.die.IsAnimating, Is.True);
                Assert.That(match.LastTokenToTouchTheBallOnPurpose.playerName, Is.EqualTo(nativeShotShooter));
                shot.StartCoroutine(shot.ResolveHandlingTest(step.Roll)); yield return DismissNativeShotResult();
                bool hold = step.Roll < grid.GetDefendingGK().handling;
                yield return Until(() => hold ? shot.isWaitingForSaveandHoldScenario : loose.isActivated, "handling continues to actual hold or spill");
                Assert.That(match.LastTokenToTouchTheBallOnPurpose.playerName, Is.EqualTo("Kuzmic")); break;
            case ShootingBranchStepType.GoalkeeperMove:
                yield return Until(() => keeper.isActivated, "entry offers defending goalkeeper movement");
                Assert.That(shot.isActivated || loose.isActivated, Is.True);
                Assert.That(grid.GetDefendingGK().playerName, Is.EqualTo("Kuzmic"));
                var destination = Hex(step.Coordinates.x, step.Coordinates.y);
                Assert.That(grid.highlightedHexes.Contains(destination) || grid.GetDefendingGK().currentHex == destination, Is.True);
                Assert.That(keeper.GetInstructions(), Does.Contain("Defending GK"));
                if (shot.shotType == "freeKick") Assert.That(DuelResolutionPanelController.IsVisible, Is.False);
                if (grid.GetDefendingGK().currentHex == destination) LegacyKey("X");
                else input.TryExecuteSyntheticHexClick(destination, "Legacy migration", out _);
                yield return Until(() => !keeper.isActivated && !keeper.IsResolvingMove && !movement.isPlayerMoving, "keeper moves and releases the interrupted shooting flow");
                Assert.That(grid.GetDefendingGK().currentHex, Is.SameAs(destination)); break;
            case ShootingBranchStepType.LooseDirectionRoll:
                yield return Until(() => loose.isActivated && loose.isWaitingForDirectionRoll, "shot scatter waits for direction");
                Assert.That(match.LastTokenToTouchTheBallOnPurpose.playerName,
                    Is.EqualTo(steps.Take(steps.IndexOf(step)).Any(s => s.Type == ShootingBranchStepType.HandlingRoll) ? "Kuzmic" : nativeShotShooter));
                loose.PerformDirectionRoll(step.Roll);
                yield return Until(() => !loose.isWaitingForDirectionRoll, "scatter direction is consumed");
                Assert.That(loose.directionRoll, Is.EqualTo(step.Roll - 1)); break;
            case ShootingBranchStepType.LooseDistanceRoll:
                yield return Until(() => loose.isActivated && loose.isWaitingForDistanceRoll, "shot scatter waits for distance");
                loose.PerformDistanceRoll(step.Roll);
                yield return Until(() => !loose.isWaitingForDistanceRoll, "scatter distance is consumed");
                Assert.That(loose.distanceRoll, Is.EqualTo(step.Roll)); break;
            case ShootingBranchStepType.GoalkeeperWallSaveRoll:
                yield return Until(() => loose.isActivated && loose.isWaitingForInterceptionRoll, "keeper movement opens a real GK-wall saving attempt");
                Assert.That(loose.potentialInterceptor.playerName, Is.EqualTo("Kuzmic"));
                loose.PerformInterceptionRoll(step.Roll); yield return DismissNativeShotResult(); break;
            case ShootingBranchStepType.LooseInterceptionRoll:
                yield return Until(() => loose.isActivated && loose.isWaitingForInterceptionRoll, $"shot scatter offers {step.PlayerName} recovery");
                var interceptor = loose.potentialInterceptor; Assert.That(interceptor.playerName, Is.EqualTo(step.PlayerName));
                CountBranch(scatterAttempts, step.PlayerName);
                float probability = ExpectedStatsCalculator.CalculateRecoveryProbability(interceptor.IsGoalKeeper ? interceptor.saving : interceptor.tackling);
                scatterExpectedRecoveries[step.PlayerName] = (scatterExpectedRecoveries.TryGetValue(step.PlayerName, out float p) ? p : 0) + probability;
                if (step.Roll == 6 || step.Roll + (interceptor.IsGoalKeeper ? interceptor.saving : interceptor.tackling) >= 10) CountBranch(scatterRecoveries, step.PlayerName);
                loose.PerformInterceptionRoll(step.Roll); yield return DismissNativeShotResult(); break;
            default:
                yield return AssertNativeShootingOutcome(step, steps); break;
        }
    }

    private IEnumerator AssertNativeShootingOutcome(ShootingBranchStep step, List<ShootingBranchStep> steps)
    {
        var shot = match.shotManager;
        if (step.Type != ShootingBranchStepType.ExpectOffTarget) yield return ForfeitLegacyFinalThirds();
        switch (step.Type)
        {
            case ShootingBranchStepType.ExpectAnyOtherScenario:
                yield return Until(() => !loose.isActivated && !shot.isActivated && !keeper.isActivated, "shooting recovery completes");
                AssertLegacyMovementAnyOtherRecovery(match.playerTokenManager.allTokens.Single(t => t.playerName == step.PlayerName)); break;
            case ShootingBranchStepType.ExpectMovementToAttack:
                yield return Until(() => !loose.isActivated && movement.isActivated, "shot scatter into empty space starts committed movement");
                Assert.That(movement.isMovementPhaseAttack && !match.attackHasPossession, Is.True);
                Assert.That(ftp.isAvailable || movement.isAvailable || ground.isAvailable || high.isAvailable || longBall.isAvailable
                    || ftp.isActivated || ground.isActivated || high.isActivated || longBall.isActivated || shot.isActivated || keeper.isActivated, Is.False);
                Assert.That(ball.GetCurrentHex().GetOccupyingToken(), Is.Null);
                Assert.That(ball.GetCurrentHex().isAttackOccupied || ball.GetCurrentHex().isDefenseOccupied, Is.False);
                Assert.That(match.LastTokenToTouchTheBallOnPurpose.playerName, Is.EqualTo(steps.Any(s => s.Type == ShootingBranchStepType.HandlingRoll) ? "Kuzmic" : nativeShotShooter)); break;
            case ShootingBranchStepType.ExpectGoalkeeperRecoveryHold:
            case ShootingBranchStepType.ExpectSaveAndHold:
                yield return Until(() => shot.isWaitingForSaveandHoldScenario, "saved shot waits for goalkeeper distribution choice");
                Assert.That(shot.isActivated || loose.isActivated || keeper.isActivated || movement.isActivated, Is.False);
                Assert.That(match.LastTokenToTouchTheBallOnPurpose.playerName, Is.EqualTo("Kuzmic"));
                Assert.That(match.attackHasPossession && match.teamInAttack == MatchManager.TeamInAttack.Away, Is.True);
                Assert.That(movement.isAvailable || ground.isAvailable || ftp.isAvailable || high.isAvailable || longBall.isAvailable, Is.False);
                Assert.That(ball.GetCurrentHex(), Is.SameAs(match.LastTokenToTouchTheBallOnPurpose.currentHex)); break;
            case ShootingBranchStepType.ExpectCornerNorth:
            case ShootingBranchStepType.ExpectCornerSouth:
                if (!match.freeKickManager.isWaitingForKickerSelection)
                {
                    yield return Until(() => match.finalThirdManager.isActivated, "shot corner offers Final Thirds before kicker selection", 1200);
                    Assert.That(match.freeKickManager.isWaitingForKickerSelection, Is.False); yield return ForfeitLegacyFinalThirds();
                }
                yield return Until(() => match.freeKickManager.isActivated && match.freeKickManager.isWaitingForKickerSelection, "goalkeeper deflection produces the expected corner restart", 1200);
                Assert.That(shot.isActivated || loose.isActivated || keeper.isActivated || movement.isActivated, Is.False);
                Assert.That(ftp.isAvailable || ground.isAvailable || high.isAvailable || longBall.isAvailable || movement.isAvailable, Is.False);
                Assert.That(match.LastTokenToTouchTheBallOnPurpose.playerName, Is.EqualTo("Kuzmic"));
                Assert.That(ball.GetCurrentHex().coordinates.z, step.Type == ShootingBranchStepType.ExpectCornerNorth ? Is.GreaterThan(0) : Is.LessThan(0)); break;
            case ShootingBranchStepType.ExpectGoal:
            case ShootingBranchStepType.ExpectOwnGoal:
                yield return Until(() => match.goalFlowManager.isActivated, "shot or scatter reaches a goal through gameplay");
                Assert.That(shot.isActivated || loose.isActivated || keeper.isActivated || movement.isActivated, Is.False);
                Assert.That(match.LastTokenToTouchTheBallOnPurpose, Is.Not.Null);
                if (step.Type == ShootingBranchStepType.ExpectGoal) Assert.That(match.LastTokenToTouchTheBallOnPurpose.playerName, Is.EqualTo(nativeShotShooter));
                Assert.That(ball.GetCurrentHex().isInGoal, Is.Not.Zero);
                yield return WaitForLegacyGoalReset();
                Assert.That(shot.isActivated || loose.isActivated || keeper.isActivated, Is.False); break;
            case ShootingBranchStepType.ExpectOffTarget:
                yield return Until(() => !shot.isActivated && !loose.isActivated && !keeper.isActivated && !match.goalFlowManager.isActivated,
                    "original natural-one free-kick shot finishes off target");
                Assert.That(match.currentState, Is.EqualTo(MatchManager.GameState.WaitingForGoalKickFinalThirds));
                Assert.That(match.teamInAttack, Is.EqualTo(MatchManager.TeamInAttack.Away));
                Assert.That(match.LastTokenToTouchTheBallOnPurpose.playerName, Is.EqualTo("Kuzmic"),
                    "Goal-kick preparation establishes the new restart taker after the shooter owned the shot and failed-block inputs.");
                Assert.That(match.PreviousTokenToTouchTheBallOnPurpose, Is.Null); break;
            default: Assert.Fail($"Unhandled original shooting outcome {step.Type}"); break;
        }
    }

    private void AssertNativeShootingStatistics(List<ShootingBranchStep> steps)
    {
        var stats = match.gameData.stats; var shooter = stats.GetPlayerStats(nativeShotShooter); var home = stats.homeTeamStats; var away = stats.awayTeamStats;
        Assert.That(shooter.shotsAttempted, Is.EqualTo(1)); Assert.That(home.totalShots, Is.EqualTo(1));
        Assert.That(shooter.shotsBlocked, Is.EqualTo(expectedBlocked ? 1 : 0)); Assert.That(home.totalShotsBlocked, Is.EqualTo(expectedBlocked ? 1 : 0));
        bool goalOnTarget = steps.Any(s => s.Type == ShootingBranchStepType.ExpectGoal) && !expectedBlocked;
        Assert.That(shooter.shotsOnTarget, Is.EqualTo(expectedOnTarget || goalOnTarget ? 1 : 0)); Assert.That(shooter.shotsOffTarget, Is.EqualTo(expectedOffTarget ? 1 : 0));
        Assert.That(home.totalGoals, Is.EqualTo(steps.Any(s => s.Type is ShootingBranchStepType.ExpectGoal or ShootingBranchStepType.ExpectOwnGoal) ? 1 : 0));
        Assert.That(home.totalCorners, Is.EqualTo(steps.Any(s => s.Type is ShootingBranchStepType.ExpectCornerNorth or ShootingBranchStepType.ExpectCornerSouth) ? 1 : 0));
        Assert.That(away.totalShotBlocksAttempted, Is.EqualTo(shotBlocksAttempted.Values.Sum())); Assert.That(away.totalShotBlocksMade, Is.EqualTo(shotBlocksMade.Values.Sum()));
        if (branchExpectedGoals.HasValue) Assert.That(shooter.xGoals, Is.EqualTo(branchExpectedGoals.Value).Within(.0001));
        else Assert.That(shooter.xGoals, Is.InRange(float.Epsilon, 1));
        Assert.That(home.totalXGoals, Is.EqualTo(shooter.xGoals).Within(.0001));
        string receiver = steps.LastOrDefault(s => s.Type is ShootingBranchStepType.ExpectAnyOtherScenario or ShootingBranchStepType.ExpectGoalkeeperRecoveryHold)?.PlayerName;
        bool autoPickup = receiver != null && !match.playerTokenManager.allTokens.Single(t => t.playerName == receiver).isHomeTeam && !scatterRecoveries.ContainsKey(receiver);
        var involved = shotBlocksAttempted.Keys.Concat(scatterAttempts.Keys).Concat(scatterExpectedRecoveries.Keys).Concat(autoPickup ? new[] { receiver } : Array.Empty<string>()).Distinct();
        foreach (var name in involved)
        {
            var player = stats.GetPlayerStats(name);
            Assert.That(player.shotBlocksAttempted, Is.EqualTo(shotBlocksAttempted.TryGetValue(name, out int b) ? b : 0), name);
            Assert.That(player.shotBlocksMade, Is.EqualTo(shotBlocksMade.TryGetValue(name, out int m) ? m : 0), name);
            Assert.That(player.interceptionsAttempted, Is.EqualTo(scatterAttempts.TryGetValue(name, out int i) ? i : 0), name);
            Assert.That(player.interceptionsMade, Is.Zero, name);
            Assert.That(player.xRecoveries, Is.EqualTo((scatterExpectedRecoveries.TryGetValue(name, out float x) ? x : 0) + (autoPickup && receiver == name ? 1 : 0)).Within(.0001), name);
            int recovered = (scatterRecoveries.TryGetValue(name, out int r) ? r : 0) + (autoPickup && receiver == name ? 1 : 0);
            if (recovered > 0) Assert.That(player.possessionWon, Is.GreaterThanOrEqualTo(recovered), name);
        }
        if (receiver != null && !match.playerTokenManager.allTokens.Single(t => t.playerName == receiver).isHomeTeam) Assert.That(stats.GetPlayerStats(receiver).possessionWon, Is.GreaterThanOrEqualTo(1));
        if (steps.Any(s => s.Type is ShootingBranchStepType.ExpectSaveAndHold or ShootingBranchStepType.ExpectGoalkeeperRecoveryHold)) Assert.That(stats.GetPlayerStats("Kuzmic").possessionWon, Is.GreaterThanOrEqualTo(1));
    }
}
