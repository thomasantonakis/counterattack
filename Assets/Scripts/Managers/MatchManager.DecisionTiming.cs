using System;
using System.Collections.Generic;
using UnityEngine;

public partial class MatchManager
{
    private DecisionTimingService decisionTiming;
    private UnityEngine.Object timingRollManager;
    private bool executingTimingRoll;
    private int timingCompletedActions = -1;
    private HeaderManager timingHeader;
    private ThrowInManager timingThrowIn;
    private PenaltyShootoutManager timingShootout;
    public int TimingRollSequence { get; private set; }
    public DecisionTimingService DecisionTiming
    {
        get
        {
            if (decisionTiming == null)
            {
                decisionTiming = new DecisionTimingService();
                decisionTiming.Recorded += (eventName, owner, id) =>
                    RecordGameplayOutcome("decision_timing." + eventName, "decision_timing", eventName,
                        details: new Dictionary<string, string> { ["owner"] = owner, ["decisionId"] = id });
            }
            return decisionTiming;
        }
    }
    public static bool TimingResultBlocked => HeaderChallengePanelController.IsHoldingContinuation
        || DuelResolutionPanelController.IsHoldingContinuation || StoppageActionsPanelController.IsHoldingContinuation;

    public void RefreshDecisionTiming()
    {
        var timing = DecisionTiming;
        if (timingCompletedActions != totalCompletedActions)
        {
            timing.ClearUnresolvedChoices();
            timingCompletedActions = totalCompletedActions;
        }
        timing.Paused = isPauseMenuOpen || RoomInputOwnership.IntroActive || CoinTossPanelController.IsRoomVisible;
        timing.BlockingResult = HeaderChallengePanelController.IsMatchClockHeld
            || DuelResolutionPanelController.IsMatchClockHeld || StoppageActionsPanelController.IsMatchClockHeld;
        timing.AutomaticResult = TimingResultBlocked && !timing.BlockingResult;
        timing.ContinuationInProgress = movementPhaseManager?.isPlayerMoving == true
            || movementPhaseManager?.IsResolvingReposition == true;
        // Tactical allowances protect running match time. Built-in stoppages do not
        // need their own budget, but an interrupted live allowance is not refilled.
        timing.GameClockStopped = !isClockRunning || isHalfExpired || isMatchComplete
            || isHalfTimeFlowRunning || isPauseMenuOpen || RoomInputOwnership.IntroActive
            || CoinTossPanelController.IsRoomVisible || IsClockPausedForCurrentState();
        timing.BeginRegistration();
        var r = new DecisionTimingRegistration(this, timing);
        r.Scope(nameof(MatchManager), teamInAttack == TeamInAttack.Home);
        bool available = groundBallManager?.isAvailable == true || movementPhaseManager?.isAvailable == true
            || highPassManager?.isAvailable == true || longBallManager?.isAvailable == true || firstTimePassManager?.isAvailable == true;
        bool activated = groundBallManager?.isActivated == true || movementPhaseManager?.isActivated == true
            || highPassManager?.isActivated == true || longBallManager?.isActivated == true || firstTimePassManager?.isActivated == true
            || shotManager?.isActivated == true;
        bool preview = movementPhaseManager?.isActivated == true && !movementPhaseManager.isCommitted
            || groundBallManager?.isActivated == true && groundBallManager.isAwaitingTargetSelection
            || highPassManager?.isActivated == true && highPassManager.isWaitingForConfirmation
            || longBallManager?.isActivated == true && longBallManager.isAwaitingTargetSelection
            || firstTimePassManager?.isActivated == true && firstTimePassManager.isAwaitingTargetSelection
            || shotManager?.isWaitingForShotCommitConfirmation == true;
        bool sharedSelection = difficulty_level < 3 && currentCommittedActionKind == MatchActionKind.None
            && IsStableSaveState(currentState) && (preview || available && !activated)
            && shotManager?.headerManager?.isActivated != true
            && finalThirdManager?.isActivated != true && freeKickManager?.isActivated != true
            && movementPhaseManager?.goalKeeperManager?.IsWaitingForPickupChoice != true;
        r.SharedActionSelection = sharedSelection;
        r.Decision(currentState == GameState.GoalKick ? DecisionKind.GoalKickActionSelection : DecisionKind.SelectNextAction,
            (sharedSelection || available && !activated) && !isMatchComplete && !isHalfTimeFlowRunning);
        kickoffManager?.RegisterTiming(r);
        freeKickManager?.RegisterTiming(r);
        penaltyKickManager?.RegisterTiming(r);
        if (timingShootout == null) timingShootout = FindAnyObjectByType<PenaltyShootoutManager>();
        timingShootout?.RegisterTiming(r);
        movementPhaseManager?.RegisterTiming(r);
        groundBallManager?.RegisterTiming(r);
        firstTimePassManager?.RegisterTiming(r);
        highPassManager?.RegisterTiming(r);
        longBallManager?.RegisterTiming(r);
        if (timingHeader == null) timingHeader = FindAnyObjectByType<HeaderManager>();
        timingHeader?.RegisterTiming(r);
        shotManager?.RegisterTiming(r);
        looseBallManager?.RegisterTiming(r);
        if (timingThrowIn == null) timingThrowIn = FindAnyObjectByType<ThrowInManager>();
        timingThrowIn?.RegisterTiming(r);
        finalThirdManager?.RegisterTiming(r);
        movementPhaseManager?.goalKeeperManager?.RegisterTiming(r);
        r.Scope(nameof(MatchManager), null);
        r.Roll(this, "stoppage", isWaitingForExtraActionsRoll,
            () => isWaitingForExtraActionsRoll && (stoppageActionsPanel == null || !stoppageActionsPanel.HasSceneBindings || stoppageActionsPanel.IsAwaitingRoll), OnKeyReceived);
        timingRollManager = r.RollManager;
        r.ApplyRoll();
        timing.EndRegistration();
        timing.CurrentLabel = timing.Current?.kind == DecisionKind.SnapshotOrForfeitPace
            && movementPhaseManager?.isActivated == true
            ? movementPhaseManager.isWaitingForSnapshotDecision ? "Snapshot, move or forfeit pace" : "Move or forfeit pace"
            : null;
        ValidateTimingCoverage();
    }

    public void AdvanceDecisionTiming(float seconds)
    {
        RefreshDecisionTiming();
        DecisionTiming.Advance(seconds);
    }

    // Both physical/AI roll handlers and elapsed-time authority enter here. The claimed
    // callback calls the existing manager handler, preserving overrides and continuations.
    public bool TryDispatchTimedRoll(UnityEngine.Object manager, KeyPressData input, Action<KeyPressData> command)
    {
        if (executingTimingRoll || input.isConsumed || (input.key != KeyCode.R && !RollInputOverride.TryParse(input, out _))) return false;
        RefreshDecisionTiming();
        if (!DecisionTiming.HasRollWait || timingRollManager != manager) return false;
        DecisionTiming.SubmitRoll(DecisionTiming.RollId, DecisionTiming.RollOwner,
            () => ExecuteTimingRoll(() => command(input)));
        return true;
    }
    public void ExecuteTimingRoll(Action command)
    {
        executingTimingRoll = true;
        try { command(); }
        finally
        {
            executingTimingRoll = false;
            // The roll may synchronously open a blocking result or the next roll.
            // Refresh before this frame's match-clock tick can credit possession.
            RefreshDecisionTiming();
        }
    }
    private string lastUnclassifiedTimingWait;
    private static readonly Dictionary<Type, System.Reflection.FieldInfo[]> TimingWaitFields = new();
    [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private void ValidateTimingCoverage()
    {
        if (DecisionTiming.GameClockStopped || DecisionTiming.Current != null || DecisionTiming.HasPendingRoll || TimingResultBlocked || DecisionTiming.Paused
            || movementPhaseManager?.isPlayerMoving == true || movementPhaseManager?.IsResolvingReposition == true
            || freeKickManager?.IsResolvingSetup == true || shotManager?.IsMovingSnapshotBlocker == true
            || movementPhaseManager?.goalKeeperManager?.IsResolvingMove == true)
        { lastUnclassifiedTimingWait = null; return; }
        MonoBehaviour[] managers = {groundBallManager, firstTimePassManager, highPassManager, longBallManager,
            movementPhaseManager, timingHeader, shotManager, looseBallManager, timingThrowIn, freeKickManager, penaltyKickManager};
        foreach (var manager in managers)
        {
            if (manager == null) continue;
            var type = manager.GetType();
            if (!TimingWaitFields.TryGetValue(type, out var fields))
            {
                fields = System.Array.FindAll(type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance),
                    f => f.FieldType == typeof(bool) && (f.Name.StartsWith("isWaiting", StringComparison.OrdinalIgnoreCase) || f.Name.StartsWith("isAwaiting", StringComparison.OrdinalIgnoreCase)));
                TimingWaitFields[type] = fields;
            }
            foreach (var field in fields)
            {
                if (!(bool)field.GetValue(manager)) continue;
                string wait = type.Name + "." + field.Name;
                if (lastUnclassifiedTimingWait != wait) Debug.LogWarning("[DecisionTiming] Awaiting-input state without a ready timing classification: " + wait);
                lastUnclassifiedTimingWait = wait;
                return;
            }
        }
        lastUnclassifiedTimingWait = null;
    }
}
