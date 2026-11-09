using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public sealed partial class GoalkeeperBoxEntryFixture
{
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
        LegacyKey("M"); match.RefreshDecisionTiming();
        Assert.That(movement.isActivated, Is.True, "real action switch cancels target preview");
        Assert.That(match.DecisionTiming.Current.kind, Is.EqualTo(DecisionKind.AttackingMovement));
        match.AdvanceDecisionTiming(3);
        LegacyKey(action); match.RefreshDecisionTiming();
        Assert.That(movement.isActivated, Is.False);
        Assert.That(match.DecisionTiming.Current.id, Is.EqualTo(original.id));
        Assert.That(match.DecisionTiming.Current.remaining, Is.EqualTo(remaining));
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
        }
        else Assert.That(ground.isActivated && ground.isQuickThrow, Is.True);
        ExpireTimingChoice(DecisionKind.GoalkeeperDistributionTarget, "Home");
    }

    public IEnumerator RunTimingShotFollowup(int side, bool snapshot)
    {
        Board(side); match.isClockRunning = true;
        Place(attacker, Hex(14 * side)); Place(defender, Hex(-10 * side, 8));
        Place(awayGK, Hex(16 * side)); attacker.shooting = 3; awayGK.saving = 10; awayGK.handling = 10;
        ball.PlaceAtCell(attacker.currentHex); match.SetLastToken(attacker);
        Invoke(grid, "LoadPrecomputedShootingPaths");
        AttachAuthoredPanels(); var shot = match.shotManager; EnableForAuthoredPresentation(shot);
        Subscribe(match.finalThirdManager);
        shot.StartShotProcess(attacker, snapshot ? "snapshot" : "fullPower");
        if (snapshot)
        {
            yield return Until(() => shot.isWaitingforBlockerSelection, "snapshot defender choice");
            LegacyKey("X");
        }
        yield return Until(() => shot.isWaitingForTargetSelection, "real shot goal-line target choice");
        ExpireTimingChoice(snapshot ? DecisionKind.SnapshotGoalLineTarget : DecisionKind.ShotGoalLineTarget, "Home");
        Assert.That(input.TryExecuteSyntheticHexClick(Hex(19 * side), "Timing shot", out var error), Is.True, error);
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

    public IEnumerator RunTimingNonblockingScatter(int side)
    {
        Board(side); match.isClockRunning = true;
        Place(attacker, Hex(0)); Place(defender, Hex(-10 * side, 8)); ball.PlaceAtCell(attacker.currentHex);
        match.SetLastToken(attacker);
        AttachAuthoredPanels(); EnableForAuthoredPresentation(loose);
        loose.StartCoroutine(loose.ResolveLooseBall(attacker, LooseBallSourceType.GroundDeflection, false,
            LooseBallPresentationContext.ShotResolution));
        yield return Until(() => loose.isWaitingForDirectionRoll, "scatter direction");
        loose.PerformDirectionRoll(1);
        yield return Until(() => loose.isWaitingForDistanceRoll, "scatter distance");
        loose.PerformDistanceRoll(3);
        yield return Until(() => ball.isMoving, "nonblocking scatter result permits actual ball movement");
        Assert.That(duelPanel.IsOpen, Is.True);
        match.RefreshDecisionTiming();
        Assert.That(match.DecisionTiming.BlockingResult, Is.False, "visible nonblocking scatter cannot hold timing");
        float before = match.gameData.stats.homeTeamStats.totalPossessionSeconds;
        Invoke(match, "AdvanceMatchTime", 2f);
        Assert.That(match.gameData.stats.homeTeamStats.totalPossessionSeconds, Is.GreaterThan(before));
        yield return Until(() => !loose.isActivated, "scatter completes without result dismissal");
    }
}
