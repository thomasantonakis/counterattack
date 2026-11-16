using System.Linq;

public enum OnlineGroundDuelIntent { Nutmeg, Victim, Tackle, Stand, Roll, Reposition, Stay, Dive }

public partial class MovementPhaseManager
{
    // The dribble's movement coroutine waits at the reached wall hex until the
    // keeper chooses/resolves. Its parent busy flag must not suppress that child input.
    private bool OnlineWallParentWaiting=>isGkWallDiveSequenceActive
        && (isWaitingForGKWallDiveDecision || goalkeeperChallengeMode==GoalkeeperChallengeMode.WallDive);
    public string OnlineGroundDuelStage=>!isActivated?null:OnlineDribbleBlocked || isPlayerMoving && !OnlineWallParentWaiting || IsResolvingReposition
        ?(isWaitingForTackleRoll || isWaitingForReposition || activeDuelPanel?.IsOpen==true?"resolving":null)
        :isWaitingForGKWallDiveDecision?"wall-choice":lookingForNutmegVictim?"victim":isWaitingForReposition?"reposition"
        :isWaitingForTackleRoll && (!tackleDefenderRolled || !tackleAttackerRolled)?"roll"
        :isWaitingForTackleDecision || OnlineStationaryKeeperChoice?"choice":isWaitingForNutmegDecisionWithoutMoving?"nutmeg"
        :activeDuelPanel?.IsOpen==true?"resolving":null;
    public PlayerToken OnlineGroundDuelActor=>isWaitingForGKWallDiveDecision?gkWallDiveGoalkeeper:isWaitingForReposition?repositionWinner
        :isWaitingForTackleRoll?(!tackleDefenderRolled?(isNutmegInProgress?nutmegVictim:selectedDefender):ball.GetCurrentHex()?.GetOccupyingToken())
        :OnlineStationaryKeeperChoice?selectedToken:isWaitingForTackleDecision?selectedDefender??selectedToken:ball.GetCurrentHex()?.GetOccupyingToken()??selectedToken;
    private bool OnlineStationaryKeeperChoice=>isWaitingForTackleDecisionWithoutMoving
        && IsOutsideBoxGoalkeeperChallengeCandidate(selectedToken,ball?.GetCurrentHex()?.GetOccupyingToken());
    public bool OnlineCanDive=>OnlineGroundDuelStage=="wall-choice" || OnlineGroundDuelStage=="choice"
        && (OnlineStationaryKeeperChoice || IsCurrentOutsideBoxGoalkeeperChallengeCandidate());
    public bool OnlineCanStationaryFootTackle=>OnlineBasicMovementAvailable && isWaitingForTackleDecisionWithoutMoving
        && isAwaitingHexDestination && selectedToken!=null && !selectedToken.isAttacker && !IsThrowInTackleWithoutMovingBlocked(selectedToken);
    public bool OnlineCanFootTackle=>OnlineCanStationaryFootTackle || OnlineGroundDuelStage=="choice"
        && (!OnlineStationaryKeeperChoice || !IsThrowInTackleWithoutMovingBlocked(selectedToken));
    public PlayerToken[] OnlineNutmegVictims=>lookingForNutmegVictim || isWaitingForNutmegDecisionWithoutMoving
        ?nutmeggableDefenders.Where(t=>t!=null && t.isPlaying && !t.isAttacker).ToArray():System.Array.Empty<PlayerToken>();
    public HexCell[] OnlineDuelRepositionTargets=>OnlineGroundDuelStage!="reposition"?System.Array.Empty<HexCell>()
        :repositionHexes.Where(h=>h!=null && h.GetOccupyingToken()==null && !h.isAttackOccupied && !h.isDefenseOccupied).ToArray();
    public bool OnlineCanStayAfterDuel=>isWaitingForReposition && !IsResolvingReposition && (!isNutmegInProgress || isSuccessfulTackleRepositionPending);

    public bool TryExecuteOnlineGroundDuel(OnlineGroundDuelIntent intent,PlayerToken player=null,HexCell target=null)
    {
        var match=MatchManager.Instance;
        if(match==null || !match.IsOnlineAuthorityControlled || match.IsGameplayInputBlocked || !isActivated
            || OnlineDribbleBlocked || isPlayerMoving && !OnlineWallParentWaiting || IsResolvingReposition)return false;
        switch(intent)
        {
            case OnlineGroundDuelIntent.Nutmeg:
                if(!isWaitingForNutmegDecision && !isWaitingForNutmegDecisionWithoutMoving)return false;
                RecordNutmegChoiceSelected("online","attempt_nutmeg",IsInstructionExpectingHomeTeam());
                StartChosenNutmeg();return true;
            case OnlineGroundDuelIntent.Victim:
                if(!OnlineNutmegVictims.Contains(player))return false;
                if(lookingForNutmegVictim){HandleNutmegVictimSelection(player);return true;}
                if(isWaitingForNutmegDecisionWithoutMoving)
                {RecordNutmegChoiceSelected("online","attempt_nutmeg",IsInstructionExpectingHomeTeam());CommitToNutmeg(player);return true;}
                return false;
            case OnlineGroundDuelIntent.Tackle:
                if(!OnlineCanFootTackle)return false;
                PrepareOnlineStationaryTackleChoice();
                if(IsCurrentOutsideBoxGoalkeeperChallengeCandidate())goalkeeperChallengeMode=GoalkeeperChallengeMode.OutsideBoxFootTackle;
                isWaitingForTackleDecision=false;StartTackleDiceRollSequence();return true;
            case OnlineGroundDuelIntent.Stand:
                if(OnlineGroundDuelStage=="wall-choice")
                {isWaitingForGKWallDiveDecision=false;isGkWallDiveSequenceActive=false;return true;}
                if(OnlineGroundDuelStage!="choice")return false;
                if(OnlineStationaryKeeperChoice){PrepareOnlineStationaryTackleChoice();AdvanceMovementPhase();return true;}
                isWaitingForTackleDecision=false;ResetTacklePhase();AdvanceMovementPhase();return true;
            case OnlineGroundDuelIntent.Dive:
                if(!OnlineCanDive)return false;
                if(OnlineGroundDuelStage=="wall-choice")
                {
                    isWaitingForGKWallDiveDecision=false;isGkWallDiveInProgress=true;
                    goalkeeperChallengeMode=GoalkeeperChallengeMode.WallDive;selectedDefender=gkWallDiveGoalkeeper;
                }
                else
                {
                    PrepareOnlineStationaryTackleChoice();isWaitingForTackleDecision=false;
                    goalkeeperChallengeMode=GoalkeeperChallengeMode.OutsideBoxDive;isGkWallDiveInProgress=true;
                    gkWallDiveGoalkeeper=selectedDefender;gkWallDiveHex=ball.GetCurrentHex()?.GetOccupyingToken()?.GetCurrentHex();gkWallDiveSavingPenalty=0;
                }
                StartTackleDiceRollSequence();return true;
            case OnlineGroundDuelIntent.Roll:
                if(OnlineGroundDuelStage!="roll")return false;
                PerformTackleDiceRollInternal(!tackleDefenderRolled,(RollInputOverride?)null);return true;
            case OnlineGroundDuelIntent.Reposition:
                if(!OnlineDuelRepositionTargets.Contains(target))return false;
                _=AsyncRepositionTokenToHex(target);return true;
            case OnlineGroundDuelIntent.Stay:
                if(!OnlineCanStayAfterDuel)return false;
                isWaitingForReposition=false;hexGrid.ClearHighlightedHexes();
                if(!isSuccessfulTackleRepositionPending)AdvanceMovementPhase();return true;
            default:return false;
        }
    }
    private void PrepareOnlineStationaryTackleChoice()
    {
        if(!isWaitingForTackleDecisionWithoutMoving)return;
        selectedDefender=selectedToken;RegisterMovedToken(selectedToken);isAwaitingHexDestination=false;
        isWaitingForTackleDecisionWithoutMoving=false;isWaitingForTackleDecision=false;hexGrid.ClearHighlightedHexes();
    }
    private void StartChosenNutmeg()
    {
        CommitToAction();
        if(isWaitingForNutmegDecisionWithoutMoving)
        {
            hexGrid.ClearHighlightedHexes();isWaitingForNutmegDecisionWithoutMoving=false;isDribblerRunning=true;
        }
        isWaitingForNutmegDecision=false;isWaitingForSnapshotDecision=false;
        StartNutmegVictimIdentification();
    }
}
