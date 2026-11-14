using System.Linq;

public enum OnlineGroundDuelIntent { Nutmeg, Victim, Tackle, Stand, Roll, Reposition, Stay }

public partial class MovementPhaseManager
{
    public string OnlineGroundDuelStage=>!isActivated?null:OnlineDribbleBlocked || isPlayerMoving || IsResolvingReposition
        ?(isWaitingForTackleRoll || isWaitingForReposition || activeDuelPanel?.IsOpen==true?"resolving":null)
        :lookingForNutmegVictim?"victim":isWaitingForReposition?"reposition"
        :isWaitingForTackleRoll && (!tackleDefenderRolled || !tackleAttackerRolled)?"roll"
        :isWaitingForTackleDecision?"choice":isWaitingForNutmegDecisionWithoutMoving?"nutmeg"
        :activeDuelPanel?.IsOpen==true?"resolving":null;
    public PlayerToken OnlineGroundDuelActor=>isWaitingForReposition?repositionWinner
        :isWaitingForTackleRoll?(!tackleDefenderRolled?(isNutmegInProgress?nutmegVictim:selectedDefender):ball.GetCurrentHex()?.GetOccupyingToken())
        :isWaitingForTackleDecision?selectedDefender??selectedToken:ball.GetCurrentHex()?.GetOccupyingToken()??selectedToken;
    public PlayerToken[] OnlineNutmegVictims=>lookingForNutmegVictim?nutmeggableDefenders.Where(t=>t!=null && t.isPlaying && !t.isAttacker).ToArray():System.Array.Empty<PlayerToken>();
    public HexCell[] OnlineDuelRepositionTargets=>OnlineGroundDuelStage!="reposition"?System.Array.Empty<HexCell>()
        :repositionHexes.Where(h=>h!=null && h.GetOccupyingToken()==null && !h.isAttackOccupied && !h.isDefenseOccupied).ToArray();
    public bool OnlineCanStayAfterDuel=>isWaitingForReposition && !IsResolvingReposition && (!isNutmegInProgress || isSuccessfulTackleRepositionPending);

    public bool TryExecuteOnlineGroundDuel(OnlineGroundDuelIntent intent,PlayerToken player=null,HexCell target=null)
    {
        var match=MatchManager.Instance;
        if(match==null || !match.IsOnlineAuthorityControlled || match.IsGameplayInputBlocked || !isActivated
            || OnlineDribbleBlocked || isPlayerMoving || IsResolvingReposition)return false;
        switch(intent)
        {
            case OnlineGroundDuelIntent.Nutmeg:
                if(!isWaitingForNutmegDecision && !isWaitingForNutmegDecisionWithoutMoving)return false;
                RecordNutmegChoiceSelected("online","attempt_nutmeg",IsInstructionExpectingHomeTeam());
                StartChosenNutmeg();return true;
            case OnlineGroundDuelIntent.Victim:
                if(!OnlineNutmegVictims.Contains(player))return false;
                HandleNutmegVictimSelection(player);return true;
            case OnlineGroundDuelIntent.Tackle:
                if(OnlineGroundDuelStage!="choice")return false;
                if(IsCurrentOutsideBoxGoalkeeperChallengeCandidate())goalkeeperChallengeMode=GoalkeeperChallengeMode.OutsideBoxFootTackle;
                isWaitingForTackleDecision=false;StartTackleDiceRollSequence();return true;
            case OnlineGroundDuelIntent.Stand:
                if(OnlineGroundDuelStage!="choice")return false;
                isWaitingForTackleDecision=false;ResetTacklePhase();AdvanceMovementPhase();return true;
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
