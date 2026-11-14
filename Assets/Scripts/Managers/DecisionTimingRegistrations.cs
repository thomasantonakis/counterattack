using UnityEngine;

public partial class GroundBallManager
{
    public void RegisterTiming(DecisionTimingRegistration r)
    {
        r.Scope(nameof(GroundBallManager), IsInstructionExpectingHomeTeam());
        r.Decision(isQuickThrow ? DecisionKind.GoalkeeperDistributionTarget : DecisionKind.StandardPassTarget, isActivated && isAwaitingTargetSelection);
        r.Roll(this, "interception", isActivated && isWaitingForDiceRoll, () => isActivated && isWaitingForDiceRoll, OnKeyReceived);
    }
}
public partial class FirstTimePassManager
{
    public void RegisterTiming(DecisionTimingRegistration r)
    {
        r.Scope(nameof(FirstTimePassManager), IsInstructionExpectingHomeTeam());
        r.Decision(DecisionKind.FirstTimePassTarget, isActivated && isAwaitingTargetSelection);
        r.Decision(DecisionKind.FirstTimePassAttackerMovement, isActivated && (isWaitingForAttackerSelection || isWaitingForAttackerMove), movementPhaseManager?.isPlayerMoving != true);
        r.Decision(DecisionKind.FirstTimePassDefenderMovement, isActivated && (isWaitingForDefenderSelection || isWaitingForDefenderMove), movementPhaseManager?.isPlayerMoving != true);
        if(MatchManager.Instance?.IsOnlineAuthorityControlled==true)
        {
            r.Roll(this,"interception",isActivated && isWaitingForDiceRoll,()=>isActivated && isWaitingForDiceRoll,
                ()=>{TryExecuteOnlineFtp(OnlineFtpIntent.Roll);});return;
        }
        r.Roll(this, "interception", isActivated && isWaitingForDiceRoll, () => isActivated && isWaitingForDiceRoll, OnKeyReceived);
    }
}
public partial class HighPassManager
{
    public void RegisterTiming(DecisionTimingRegistration r)
    {
        r.Scope(nameof(HighPassManager), IsInstructionExpectingHomeTeam());
        bool target = isActivated && isWaitingForConfirmation;
        r.Decision(IsDirectGoalKick ? DecisionKind.GoalKickTarget : isGoalkeeperKick ? DecisionKind.GoalkeeperDistributionTarget : DecisionKind.HighPassTarget, target);
        r.Decision(DecisionKind.HighPassAttackerMovement, isActivated && (isWaitingForAttackerSelection || isWaitingForAttackerMove), movementPhaseManager?.isPlayerMoving != true);
        r.Decision(DecisionKind.HighPassDefenderMovement, isActivated && (isWaitingForDefenderSelection || isWaitingForDefenderMove), movementPhaseManager?.isPlayerMoving != true);
        r.Decision(DecisionKind.HighPassGoalkeeperRush, isActivated && isWaitingForDefGKChallengeDecision);
        if (MatchManager.Instance?.IsOnlineAuthorityControlled == true)
        {
            void OnlineRoll(string phase,bool waiting,OnlineHighPassIntent intent)=>r.Roll(this,phase,waiting,
                ()=>isActivated && OnlineDecisionStage==phase,()=>{TryExecuteOnlineHighPass(intent);});
            OnlineRoll("accuracy",isWaitingForAccuracyRoll,OnlineHighPassIntent.Accuracy);
            OnlineRoll("direction",isWaitingForDirectionRoll,OnlineHighPassIntent.Direction);
            OnlineRoll("distance",isWaitingForDistanceRoll,OnlineHighPassIntent.Distance);
            return;
        }
        r.Roll(this, "accuracy", isWaitingForAccuracyRoll, () => isActivated && isWaitingForAccuracyRoll, OnKeyReceived);
        r.Roll(this, "direction", isWaitingForDirectionRoll, () => isActivated && isWaitingForDirectionRoll, OnKeyReceived);
        r.Roll(this, "distance", isWaitingForDistanceRoll, () => isActivated && isWaitingForDistanceRoll, OnKeyReceived);
    }
}
public partial class LongBallManager
{
    public void RegisterTiming(DecisionTimingRegistration r)
    {
        r.Scope(nameof(LongBallManager), IsInstructionExpectingHomeTeam());
        r.Decision(DecisionKind.LongBallTarget, isActivated && isAwaitingTargetSelection);
        r.Decision(DecisionKind.LongBallGoalkeeperRush, isActivated && isWaitingForDefLBMove);
        if (MatchManager.Instance?.IsOnlineAuthorityControlled == true)
        {
            void OnlineRoll(string phase, bool waiting, OnlineLongBallIntent intent) => r.Roll(this, phase, waiting,
                () => isActivated && OnlineDecisionStage == phase, () => { TryExecuteOnlineLongBall(intent); });
            OnlineRoll("accuracy", isWaitingForAccuracyRoll, OnlineLongBallIntent.Accuracy);
            OnlineRoll("direction", isWaitingForDirectionRoll, OnlineLongBallIntent.Direction);
            OnlineRoll("distance", isWaitingForDistanceRoll, OnlineLongBallIntent.Distance);
            OnlineRoll("interception", isWaitingForInterceptionRoll, OnlineLongBallIntent.Interception);
            return;
        }
        r.Roll(this, "accuracy", isWaitingForAccuracyRoll, () => isActivated && isWaitingForAccuracyRoll, OnKeyReceived);
        r.Roll(this, "direction", isWaitingForDirectionRoll, () => isActivated && isWaitingForDirectionRoll, OnKeyReceived);
        r.Roll(this, "distance", isWaitingForDistanceRoll, () => isActivated && isWaitingForDistanceRoll, OnKeyReceived);
        r.Roll(this, "interception", isWaitingForInterceptionRoll, () => isActivated && isWaitingForInterceptionRoll && !isResolvingLandingInterception, OnKeyReceived);
    }
}
public partial class HeaderManager
{
    public void RegisterTiming(DecisionTimingRegistration r)
    {
        r.Scope(nameof(HeaderManager), IsInstructionExpectingHomeTeam());
        r.Decision(DecisionKind.HighPassAttackingJumpers, isActivated && isWaitingForAttackerSelection);
        r.Decision(DecisionKind.HighPassDefensiveJumpers, isActivated && isWaitingForDefenderSelection);
        r.Decision(DecisionKind.HeaderOrBallControl, isActivated && (isWaitingForControlOrHeaderDecision || isWaitingForControlOrHeaderDecisionDef || iswaitingForChallengeWinnerSelection));
        r.Decision(DecisionKind.HeadedPassOrHeaderAtGoal, isActivated && isWaitingForHeaderAtGoal);
        r.Decision(DecisionKind.HeadedPassTarget, isActivated && isWaitingForHeaderTargetSelection);
        r.Decision(DecisionKind.GoalkeeperPickupOrLeave, isActivated && isWaitingForHeadedBackPassChoice);
        if(MatchManager.Instance?.IsOnlineAuthorityControlled==true)
            r.Roll(this,"header",isWaitingForHeaderRoll,()=>isActivated && isWaitingForHeaderRoll && tokenRolling!=null,
                ()=>{TryExecuteOnlineHeader(OnlineHeaderIntent.Roll);});
        else r.Roll(this, "header", isWaitingForHeaderRoll, () => isActivated && isWaitingForHeaderRoll, OnKeyReceived);
        if(MatchManager.Instance?.IsOnlineAuthorityControlled==true)
            r.Roll(this,"control",isWaitingForControlRoll,()=>isActivated && isWaitingForControlRoll,
                ()=>{TryExecuteOnlineHeaderControl(OnlineHeaderControlIntent.Roll);});
        else r.Roll(this, "control", isWaitingForControlRoll, () => isActivated && isWaitingForControlRoll, OnKeyReceived);
        if(MatchManager.Instance?.IsOnlineAuthorityControlled==true)
            r.Roll(this,"interception",OnlineHeaderStage=="interception",()=>isActivated && OnlineHeaderStage=="interception",
                ()=>{TryExecuteOnlineHeader(OnlineHeaderIntent.Interception);});
        else r.Roll(this, "interception", isWaitingForInterceptionRoll, () => isActivated && isWaitingForInterceptionRoll, OnKeyReceived);
        if(MatchManager.Instance?.IsOnlineAuthorityControlled==true)
            r.Roll(this,"backpass",OnlineHeaderStage=="backpass.roll",()=>isActivated && OnlineHeaderStage=="backpass.roll",
                ()=>{TryExecuteOnlineHeader(OnlineHeaderIntent.KeeperCatch);});
        else r.Roll(this, "backpass", isWaitingForHeadedBackPassRoll, () => isActivated && isWaitingForHeadedBackPassRoll, OnKeyReceived);
    }
}
public partial class ShotManager
{
    public void RegisterTiming(DecisionTimingRegistration r)
    {
        r.Scope(nameof(ShotManager), IsInstructionExpectingHomeTeam());
        r.Decision(DecisionKind.SnapshotOrForfeitPace, (isWaitingForSnapshotDecisionFromLoose
            && movementPhaseManager?.isActivated != true) || isWaitingForShotCommitConfirmation);
        r.Decision(DecisionKind.SnapshotDefenderMovement, isWaitingforBlockerSelection || isWaitingforBlockerMovement, !IsMovingSnapshotBlocker);
        r.Decision(shotType == "snapshot" ? DecisionKind.SnapshotGoalLineTarget : DecisionKind.ShotGoalLineTarget, isWaitingForTargetSelection);
        r.Decision(DecisionKind.QuickThrowOrDistribution, isWaitingForSaveandHoldScenario);
        r.Decision(DecisionKind.GoalkeeperPickupOrLeave, isWaitingForOutsideBoxGKShotDecision);
        if(MatchManager.Instance?.IsOnlineAuthorityControlled==true)
        {
            r.Roll(this,"block",OnlineShotStage=="block",()=>OnlineShotStage=="block",()=>{TryExecuteOnlineShot(OnlineShotIntent.Block);});
            r.Roll(this,"shot",OnlineShotStage=="roll",()=>OnlineShotStage=="roll",()=>{TryExecuteOnlineShot(OnlineShotIntent.Shoot);});
            r.Roll(this,"save",OnlineShotStage is "header.save" or "save",()=>OnlineShotStage is "header.save" or "save",
                ()=>{TryExecuteOnlineShot(isHeaderAtGoal?OnlineShotIntent.HeaderSave:OnlineShotIntent.Save);});
            r.Roll(this,"handling",OnlineShotStage=="handling",()=>OnlineShotStage=="handling",()=>{TryExecuteOnlineShot(OnlineShotIntent.Handling);});
            return;
        }
        r.Roll(this, "block", isWaitingForBlockDiceRoll, () => isWaitingForBlockDiceRoll, OnKeyReceived);
        r.Roll(this, "shot", isWaitingForShotRoll, () => isWaitingForShotRoll, OnKeyReceived);
        r.Roll(this, "save", isWaitingForGKDiceRoll, () => isWaitingForGKDiceRoll, OnKeyReceived);
        r.Roll(this, "handling", isWaitingforHandlingTest, () => isWaitingforHandlingTest, OnKeyReceived);
    }
}
public partial class LooseBallManager
{
    public void RegisterTiming(DecisionTimingRegistration r)
    {
        r.Scope(nameof(LooseBallManager), IsInstructionExpectingHomeTeam());
        if(MatchManager.Instance?.IsOnlineAuthorityControlled==true)
        {
            void OnlineRoll(string phase,OnlineLooseBallIntent intent)=>r.Roll(this,phase,OnlineDecisionStage==phase,
                ()=>isActivated && OnlineDecisionStage==phase,()=>{TryExecuteOnlineLooseBall(intent);});
            OnlineRoll("direction",OnlineLooseBallIntent.Direction);OnlineRoll("distance",OnlineLooseBallIntent.Distance);OnlineRoll("recovery",OnlineLooseBallIntent.Recovery);
            return;
        }
        r.Roll(this, "direction", isActivated && isWaitingForDirectionRoll, () => isActivated && isWaitingForDirectionRoll, OnKeyReceived);
        r.Roll(this, "distance", isActivated && isWaitingForDistanceRoll, () => isActivated && isWaitingForDistanceRoll, OnKeyReceived);
        r.Roll(this, "interception", isActivated && isWaitingForInterceptionRoll, () => isActivated && isWaitingForInterceptionRoll, OnKeyReceived);
    }
}
public partial class GoalKeeperManager
{
    public void RegisterTiming(DecisionTimingRegistration r)
    {
        r.Scope(nameof(GoalKeeperManager), IsInstructionExpectingHomeTeam());
        r.Decision(consumedBoxMovePenaltyBox != 0 ? DecisionKind.DefensiveBoxMovement : DecisionKind.GoalkeeperMovement, isActivated, !IsResolvingMove);
        r.Decision(DecisionKind.GoalkeeperPickupOrLeave, IsWaitingForPickupChoice, home: pickupKeeper != null ? pickupKeeper.isHomeTeam : null);
    }
}
public partial class ThrowInManager
{
    public void RegisterTiming(DecisionTimingRegistration r)
    {
        r.Scope(nameof(ThrowInManager), awardedTeam == MatchManager.TeamInAttack.Home);
        r.Decision(DecisionKind.ThrowInTakerSelection, isActivated && isWaitingForTakerSelection);
        r.Decision(DecisionKind.ThrowInMovementOrThrow, isActivated && isWaitingForOptionalMovementDecision);
        r.Decision(DecisionKind.ThrowInActionSelection, isActivated && isWaitingForThrowTypeSelection);
        r.Decision(DecisionKind.ThrowInTarget, isActivated && (isWaitingForGroundTarget || isWaitingForHeaderTarget));
    }
}
public partial class FreeKickManager
{
    public void RegisterTiming(DecisionTimingRegistration r)
    {
        if (!isActivated) return;
        var state = MatchManager.Instance.currentState;
        r.Scope(nameof(FreeKickManager), IsInstructionExpectingHomeTeam());
        r.Decision(isCornerKick ? DecisionKind.CornerTakerSelection : DecisionKind.FreeKickTakerSelection, isWaitingForKickerSelection, !IsResolvingSetup);
        bool gk = state == MatchManager.GameState.FreeKickAttGK || state == MatchManager.GameState.FreeKickDefGK1 || state == MatchManager.GameState.FreeKickDefGK2;
        bool defense = IsDefensiveSetupState(state);
        var kind = gk ? DecisionKind.FreeKickGoalkeeperMovement : isCornerKick
            ? defense ? DecisionKind.CornerDefensiveThreeMoves : DecisionKind.CornerAttackingThreeMoves
            : defense ? DecisionKind.FreeKickDefensiveSetup : DecisionKind.FreeKickAttackingSetup;
        r.Decision(kind, isWaitingForSetupPhase || isWaitingforMovement3, !IsResolvingSetup, state.ToString());
        r.Decision(DecisionKind.FreeKickFinalKickerSelection, isWaitingForFinalKickerSelection, !IsResolvingSetup);
        r.Decision(isCornerKick ? DecisionKind.CornerActionSelection : DecisionKind.FreeKickActionSelection, isWaitingForExecution && !IsExecutionChildManagerActive());
    }
}
public partial class PenaltyKickManager
{
    public void RegisterTiming(DecisionTimingRegistration r)
    {
        if (!isActivated) return;
        var state = MatchManager.Instance.currentState;
        r.Scope(nameof(PenaltyKickManager), IsInstructionExpectingHomeTeam());
        bool ready = !isMovingToken && !isMovingRequiredSpotToken;
        r.Decision(DecisionKind.PenaltyTakerSelection, isWaitingForKickerSelection, ready);
        r.Decision(state == MatchManager.GameState.PenaltyAtt ? DecisionKind.PenaltyAttackingSetup : DecisionKind.PenaltyDefensiveSetup, isWaitingForSetupPhase, ready, state.ToString());
    }
}
public partial class PenaltyShootoutManager
{
    public void RegisterTiming(DecisionTimingRegistration r)
    {
        r.Scope(nameof(PenaltyShootoutManager), null);
        r.Decision(DecisionKind.ShootoutOrderSelection, orderSelectionStarted && !orderSelectionCompleted);
    }
}
public partial class KickoffManager
{
    public void RegisterTiming(DecisionTimingRegistration r)
    {
        if (!isActivated) return;
        r.Scope(nameof(KickoffManager), IsInstructionExpectingHomeTeam());
        bool ready = !isMovingSetupToken && !isMovingKickoffTaker;
        r.Decision(DecisionKind.KickoffFormationSetup, flowPhase == KickoffFlowPhase.InitialSetup || flowPhase == KickoffFlowPhase.PostGoalSetup, ready);
        r.Decision(DecisionKind.KickoffTakerSelection, flowPhase == KickoffFlowPhase.TakerSelection, ready);
    }
}
public partial class FinalThirdManager
{
    public void RegisterTiming(DecisionTimingRegistration r)
    {
        if (!isActivated) return;
        r.Scope(nameof(FinalThirdManager), IsInstructionExpectingHomeTeam());
        bool goalKick = finalThirdContext == FinalThirdContext.OobGoalKick;
        bool defense = currentTeamMoving == "defense";
        r.Decision(goalKick ? defense ? DecisionKind.GoalKickDefensiveSetup : DecisionKind.GoalKickAttackingSetup
            : defense ? DecisionKind.FinalThirdDefensiveMovement : DecisionKind.FinalThirdAttackingMovement,
            isWaitingForTokenSelection || isWaitingForTargetHex || isMovingToken, phase: thisIsTheSecond.ToString());
        r.Decision(DecisionKind.QuickThrowOrDistribution, isWaitingForWhatToDo);
        r.Decision(DecisionKind.GoalKickActionSelection, isWaitingForSetPieceGoalKickChoice);
    }
}
public partial class MovementPhaseManager
{
    private bool? timingPhaseAttackIsHome;

    public void RegisterTiming(DecisionTimingRegistration r)
    {
        if (!isActivated) return;
        // A recovery changes teamInAttack before repositioning finishes. The
        // unfinished phase still belongs to its original team until reset.
        bool phaseAttackHome = timingPhaseAttackIsHome ??= r.AttackHome;
        bool? owner = IsInstructionExpectingHomeTeam();
        r.Scope(nameof(MovementPhaseManager), owner);
        bool challenge = isWaitingForTackleDecision || isWaitingForTackleDecisionWithoutMoving || isWaitingForNutmegDecision || isWaitingForNutmegDecisionWithoutMoving || lookingForNutmegVictim;
        bool child = isWaitingForReposition || isWaitingForFoulDecision || isWaitingForGKWallDiveDecision;
        bool ready = !child && !IsResolvingReposition;
        // Every live dribble step offers move/forfeit, even outside snapshot range.
        bool paceChoice = (isWaitingForSnapshotDecision || isDribblerRunning && isAwaitingHexDestination)
            && !isPlayerMoving && !challenge && !child && !IsResolvingReposition
            && goalKeeperManager?.isActivated != true && looseBallManager?.isActivated != true
            && shotManager?.isActivated != true && !isWaitingForInterceptionDiceRoll
            && !isWaitingForTackleRoll && !isWaitingForYellowCardRoll && !isWaitingForInjuryRoll;
        // Keep the team's entire phase registered through token moves, dice and nested decisions.
        r.Decision(DecisionKind.AttackingMovement, isMovementPhaseAttack, ready && (!challenge || owner == phaseAttackHome), home: phaseAttackHome, concurrentWithCurrent: challenge || paceChoice);
        r.Decision(DecisionKind.DefensiveMovement, isMovementPhaseDef, ready && (!challenge || owner != phaseAttackHome), home: !phaseAttackHome, concurrentWithCurrent: challenge || paceChoice);
        r.Decision(DecisionKind.AttackingTwoForTwo, isMovementPhase2f2, ready && (!challenge || owner == phaseAttackHome), home: phaseAttackHome, concurrentWithCurrent: challenge || paceChoice);
        r.Decision(isWaitingForNutmegDecision || isWaitingForNutmegDecisionWithoutMoving || lookingForNutmegVictim ? DecisionKind.StandaloneNutmegDecision : DecisionKind.StandaloneTackleOrStand, challenge);
        r.Decision(isNutmegInProgress ? DecisionKind.NutmegReposition : DecisionKind.TackleReposition, isWaitingForReposition, !IsResolvingReposition);
        r.Decision(DecisionKind.SnapshotOrForfeitPace, paceChoice);
        r.Decision(DecisionKind.AdvantageOrFreeKick, isWaitingForFoulDecision);
        r.Decision(DecisionKind.GoalkeeperPickupOrLeave, isWaitingForGKWallDiveDecision);
        if(MatchManager.Instance?.IsOnlineAuthorityControlled==true)
            r.Roll(this,"interception",isWaitingForInterceptionDiceRoll,()=>isActivated && isWaitingForInterceptionDiceRoll,
                ()=>{TryExecuteOnlineDribble(OnlineDribbleIntent.Steal);});
        else r.Roll(this, "interception", isWaitingForInterceptionDiceRoll, () => isActivated && isWaitingForInterceptionDiceRoll, OnKeyReceived);
        if(MatchManager.Instance?.IsOnlineAuthorityControlled==true)
            r.Roll(this,"tackle",isWaitingForTackleRoll,()=>isActivated && OnlineGroundDuelStage=="roll",
                ()=>{TryExecuteOnlineGroundDuel(OnlineGroundDuelIntent.Roll);});
        else r.Roll(this, "tackle", isWaitingForTackleRoll, () => isActivated && isWaitingForTackleRoll, OnKeyReceived);
        if(MatchManager.Instance?.IsOnlineAuthorityControlled==true)
        {
            r.Roll(this,"card",isWaitingForYellowCardRoll,()=>OnlineFoulStage=="leniency",()=>{TryExecuteOnlineFoul(OnlineFoulIntent.Leniency);});
            r.Roll(this,"injury",isWaitingForInjuryRoll,()=>OnlineFoulStage=="injury",()=>{TryExecuteOnlineFoul(OnlineFoulIntent.Injury);});
        }
        else
        {
        r.Roll(this, "card", isWaitingForYellowCardRoll, () => isActivated && isWaitingForYellowCardRoll, OnKeyReceived);
        r.Roll(this, "injury", isWaitingForInjuryRoll, () => isActivated && isWaitingForInjuryRoll, OnKeyReceived);
        }
    }
}
