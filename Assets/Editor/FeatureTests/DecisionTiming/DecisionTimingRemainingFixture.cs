using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunTimingFoulFollowup(int side)
    {
        yield return PrepareLegacySoaresStealFoul(side);
        match.isClockRunning = true;
        match.AdvanceDecisionTiming(2);
        Assert.That(match.DecisionTiming.HasPendingRoll, Is.True);
        string cardId = match.DecisionTiming.RollId;
        Assert.That(match.DecisionTiming.RollRemaining, Is.EqualTo(3));
        movement.PerformLeniencyTest(1);
        yield return Until(() => movement.isWaitingForInjuryRoll, "foul card check continues to injury check");
        match.AdvanceDecisionTiming(2);
        Assert.That(match.DecisionTiming.RollId, Is.Not.EqualTo(cardId));
        Assert.That(match.DecisionTiming.RollRemaining, Is.EqualTo(3));
        movement.PerformInjuryTest(1);
        yield return Until(() => duelPanel.IsComplete, "foul results wait before advantage input");
        match.RefreshDecisionTiming(); Assert.That(match.DecisionTiming.BlockingResult, Is.False);
        yield return DismissLegacyDuel(false);
        yield return Until(() => movement.isWaitingForFoulDecision, "foul offers real advantage decision");
        ExpireTimingChoice(DecisionKind.AdvantageOrFreeKick, "Home");
        LegacyKey("A");
        yield return Until(() => !movement.isWaitingForFoulDecision && movement.isWaitingForSnapshotDecision,
            "legal advantage input remains available after expiry");
        match.RefreshDecisionTiming(); Assert.That(match.DecisionTiming.Current.kind, Is.EqualTo(DecisionKind.AttackingMovement));
    }

    private DecisionTimingState ExpireTimingChoice(DecisionKind kind, string owner)
    {
        match.RefreshDecisionTiming(); var state = match.DecisionTiming.Current;
        Assert.That(state, Is.Not.Null, kind.ToString());
        Assert.That(state.kind, Is.EqualTo(kind)); Assert.That(state.owner, Is.EqualTo(owner));
        match.AdvanceDecisionTiming(state.remaining);
        Assert.That(state.expired && match.DecisionTiming.HoldsMatchClock, Is.True);
        Assert.That(match.IsGameplayInputBlocked, Is.False);
        return state;
    }

    public IEnumerator RunTimingTargetReopening(int side, string action, bool expired)
    {
        Board(side); Place(attacker, Hex(0)); Place(defender, Hex(-10 * side, 8));
        ball.PlaceAtCell(attacker.currentHex); match.SetLastToken(attacker);
        match.difficulty_level = 2; match.isClockRunning = true;
        match.currentState = action == "F" ? MatchManager.GameState.EndOfStandardPass : MatchManager.GameState.EndOfMovementPhase;
        match.RefreshAvailableActionsForCurrentState();
        Subscribe(ground); Subscribe(high); Subscribe(longBall); Subscribe(ftp); Subscribe(movement);
        LegacyKey(action); match.RefreshDecisionTiming(); var original = match.DecisionTiming.Current;
        Assert.That(original, Is.Not.Null);
        match.AdvanceDecisionTiming(expired ? original.remaining : 4); float remaining = original.remaining;
        if (action == "C")
        {
            Assert.That(input.TryExecuteSyntheticKeyPress("M", "Timing rejected switch", out _), Is.False);
            match.RefreshDecisionTiming();
            Assert.That(match.DecisionTiming.Current.id, Is.EqualTo(original.id));
            Assert.That(match.DecisionTiming.Current.remaining, Is.EqualTo(remaining));
        }
        LegacyKey(action == "C" ? "P" : "M"); match.RefreshDecisionTiming();
        Assert.That(match.DecisionTiming.Current.kind, Is.EqualTo(DecisionKind.SelectNextAction));
        match.AdvanceDecisionTiming(3);
        LegacyKey(action); match.RefreshDecisionTiming();
        Assert.That(movement.isActivated, Is.False);
        Assert.That(match.DecisionTiming.Current.id, Is.EqualTo(original.id));
        Assert.That(match.DecisionTiming.Current.remaining, Is.EqualTo(Mathf.Max(0, remaining - 3)));
        Assert.That(match.DecisionTiming.HoldsMatchClock, Is.EqualTo(expired));
        Assert.That(match.IsGameplayInputBlocked, Is.False);
        yield break;
    }

    public IEnumerator RunTimingKeeperFollowup(int side, string choice)
    {
        match.isClockRunning = true;
        yield return PrepareHeadedBackPass(side, false);
        var header = match.shotManager.headerManager;
        yield return Until(() => header.isWaitingForHeadedBackPassChoice, "header arrives for keeper hands/feet choice");
        ExpireTimingChoice(DecisionKind.GoalkeeperPickupOrLeave, "Home");
        if (choice == "feet")
        {
            LegacyKey("F"); yield return Until(() => !header.isActivated, "legal feet input after expiry");
            yield return FinishPickupFinalThirds();
            movement.ActivateMovementPhase(); movement.CommitToAction(); FinishPickupMovement();
            Assert.That(keeper.IsWaitingForPickupChoice, Is.True);
            ExpireTimingChoice(DecisionKind.GoalkeeperPickupOrLeave, "Home");
        }
        LegacyKey("H");
        if (choice != "feet")
        {
            yield return Until(() => header.isWaitingForHeadedBackPassRoll, "keeper catch roll");
            match.AdvanceDecisionTiming(2);
            Assert.That(match.DecisionTiming.HasPendingRoll, Is.True);
            Assert.That(match.DecisionTiming.RollRemaining, Is.EqualTo(3));
            header.PerformHeadedBackPassRoll(new RollInputOverride {hasOverride=true,roll=2});
            yield return DismissLegacyDuel(false);
        }
        yield return Until(() => match.shotManager.isWaitingForSaveandHoldScenario, "keeper distribution decision");
        ExpireTimingChoice(DecisionKind.QuickThrowOrDistribution, "Home");
        LegacyKey(choice == "K" ? "K" : "Q");
        if (choice == "K")
        {
            yield return FinishPickupFinalThirds();
            Assert.That(match.finalThirdManager.isWaitingForWhatToDo, Is.True);
            match.RefreshDecisionTiming();
            LegacyKey("K");
            yield return Until(() => high.isActivated, "keeper kick target after final thirds");
            match.RefreshDecisionTiming();
            Assert.That(match.currentState, Is.EqualTo(MatchManager.GameState.GoalKick));
            Assert.That(match.DecisionTiming.GameClockStopped, Is.True);
            Assert.That(match.DecisionTiming.Current, Is.Null, "built-in goal-kick clock stop requires no allowance");
            yield break;
        }
        else Assert.That(ground.isActivated && ground.isQuickThrow, Is.True);
        ExpireTimingChoice(DecisionKind.GoalkeeperDistributionTarget, "Home");
    }

    public IEnumerator RunTimingShotFollowup(int side, bool snapshot)
    {
        Board(side); match.isClockRunning = true;
        Place(attacker, Hex(8 * side)); Place(defender, Hex(-10 * side, 8));
        Place(awayGK, Hex(16 * side)); attacker.shooting = 3; awayGK.saving = 10; awayGK.handling = 10;
        ball.PlaceAtCell(attacker.currentHex); match.SetLastToken(attacker);
        Invoke(grid, "LoadPrecomputedShootingPaths");
        AttachAuthoredPanels(); var shot = match.shotManager; EnableForAuthoredPresentation(shot);
        Subscribe(match.finalThirdManager);
        shot.StartShotProcess(attacker, snapshot ? "snapshot" : "fullPower");
        if (snapshot)
        {
            yield return Until(() => shot.isWaitingforBlockerSelection, "snapshot defender choice");
            input.TryExecuteSyntheticHexClick(defender.currentHex, "Timing snapshot defender", out _);
            var destination = shot.GetValidSnapshotBlockerDestinationsForDecisionPreview(defender).First();
            input.TryExecuteSyntheticHexClick(destination, "Timing snapshot movement", out _);
        }
        yield return Until(() => shot.isWaitingForTargetSelection, "real shot goal-line target choice");
        ExpireTimingChoice(snapshot ? DecisionKind.SnapshotGoalLineTarget : DecisionKind.ShotGoalLineTarget, "Home");
        input.TryExecuteSyntheticHexClick(Hex(19 * side), "Timing shot", out _);
        Assert.That(shot.targetHex, Is.SameAs(Hex(19 * side)));
        yield return Until(() => keeper.isActivated || shot.isWaitingForShotRoll, "target confirmation continues shot");
        if (keeper.isActivated) LegacyKey("X");
        yield return Until(() => shot.isWaitingForShotRoll, "shooter roll after keeper reaction");
        match.AdvanceDecisionTiming(2); Assert.That(match.DecisionTiming.HasPendingRoll, Is.True);
        Assert.That(match.DecisionTiming.RollRemaining, Is.EqualTo(3));
        shot.StartCoroutine(shot.StartShotRoll(2));
        yield return Until(() => shot.isWaitingForGKDiceRoll, "on-target shot reaches real saving roll");
        match.AdvanceDecisionTiming(2); Assert.That(match.DecisionTiming.HasPendingRoll, Is.True);
        Assert.That(match.DecisionTiming.RollRemaining, Is.EqualTo(3));
        shot.StartCoroutine(InvokeLegacyShotRoll("ResolveGKSavingAttempt", 6, CurrentLegacyShotInteraction()));
        yield return DismissNativeShotResult();
        yield return Until(() => shot.isWaitingforHandlingTest, "successful save opens handling roll");
        match.RefreshDecisionTiming(); Assert.That(match.DecisionTiming.HasPendingRoll, Is.True);
        shot.StartCoroutine(shot.ResolveHandlingTest(2));
        yield return DismissNativeShotResult();
        yield return Until(() => shot.isWaitingForSaveandHoldScenario, "held save reaches keeper distribution");
        ExpireTimingChoice(DecisionKind.QuickThrowOrDistribution, "Away");
        LegacyKey("Q");
        Assert.That(ground.isQuickThrow && ground.isActivated, Is.True);
        ExpireTimingChoice(DecisionKind.GoalkeeperDistributionTarget, "Away");
    }

    public IEnumerator RunTimingNonblockingScatter(int side, bool recovery, string context)
    {
        Board(side); match.isClockRunning = true;
        Place(attacker, Hex(0)); Place(defender, Hex(-10 * side, 8)); ball.PlaceAtCell(attacker.currentHex);
        match.SetLastToken(attacker);
        if (recovery)
        {
            var path = Enumerable.Range(1, 3).Select(n => loose.CalculateDirectionalTarget(Hex(0), 0, n)).ToArray();
            var destination = path[2].GetNeighbors(grid).First(h => h != null && !h.isOutOfBounds && !path.Contains(h));
            Place(defender, destination);
        }
        AttachAuthoredPanels(); EnableForAuthoredPresentation(loose);
        loose.StartCoroutine(loose.ResolveLooseBall(attacker, LooseBallSourceType.GroundDeflection, false,
            context == "shot" ? LooseBallPresentationContext.ShotResolution : LooseBallPresentationContext.GroundDuelResolution));
        yield return Until(() => loose.isWaitingForDirectionRoll, "scatter direction");
        loose.PerformDirectionRoll(1);
        yield return Until(() => loose.isWaitingForDistanceRoll, "scatter distance");
        loose.PerformDistanceRoll(3);
        yield return Until(() => ball.isMoving || loose.isWaitingForInterceptionRoll, "nonblocking scatter continues to travel or recovery");
        Assert.That(duelPanel.IsOpen, Is.True);
        match.RefreshDecisionTiming();
        Assert.That(match.DecisionTiming.BlockingResult, Is.False, "visible nonblocking scatter cannot hold timing");
        float before = match.gameData.stats.homeTeamStats.totalPossessionSeconds;
        Invoke(match, "AdvanceMatchTime", 2f);
        Assert.That(match.gameData.stats.homeTeamStats.totalPossessionSeconds, Is.GreaterThan(before));
        if (recovery)
        {
            yield return Until(() => loose.isWaitingForInterceptionRoll, "scatter reaches actual recovery roll");
            match.RefreshDecisionTiming();
            Assert.That(match.DecisionTiming.HasPendingRoll, Is.True);
            Assert.That(match.DecisionTiming.HoldsMatchClock, Is.False);
            loose.PerformInterceptionRoll(6);
            yield return Until(() => duelPanel.IsComplete, "recovery result reclaims a blocking review");
            match.RefreshDecisionTiming();
            Assert.That(match.DecisionTiming.BlockingResult, Is.False);
            LegacyKey("Return"); match.RefreshDecisionTiming();
            Assert.That(match.DecisionTiming.BlockingResult, Is.True);
            LegacyKey("Escape");
        }
        yield return Until(() => !loose.isActivated, "scatter completes without result dismissal");
        if (recovery) Assert.That(match.LastTokenToTouchTheBallOnPurpose, Is.SameAs(defender));
    }
}
