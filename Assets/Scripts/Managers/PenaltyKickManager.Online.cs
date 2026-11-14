using System;
using System.Linq;
public enum OnlinePenaltyIntent { Player, Target, Confirm }
public partial class PenaltyKickManager
{
    public bool OnlinePenaltyMoving=>isMovingToken || isMovingRequiredSpotToken;
    public string OnlinePenaltyStage=>!isActivated?null:OnlinePenaltyMoving?"resolving"
        :isWaitingForKickerSelection?"kicker":isWaitingForSetupPhase?(selectedToken==null?"select":"target"):"resolving";
    public PlayerToken[] OnlinePenaltyPlayers=>OnlinePenaltyStage=="kicker"?GetEligiblePenaltyKickersForDecision().Where(OnlinePenaltyPlayer).ToArray()
        :OnlinePenaltyStage is "select" or "target"?GetEligibleSetupTokensForDecision().Where(OnlinePenaltyPlayer).ToArray():Array.Empty<PlayerToken>();
    private static bool OnlinePenaltyPlayer(PlayerToken player)=>player!=null && player.isPlaying && !player.isSentOff && player.currentHex!=null;
    public HexCell[] OnlinePenaltyTargets=>OnlinePenaltyStage=="target"?GetLegalPenaltySetupDestinationsForDecision().ToArray():Array.Empty<HexCell>();
    public bool OnlinePenaltyCanConfirm=>OnlinePenaltyStage is "select" or "target" && GetInvalidTokensForCurrentSetupTeam().Count==0 && RequiredSpotsAreReady();
    public bool TryExecuteOnlinePenalty(OnlinePenaltyIntent intent,PlayerToken player=null,HexCell target=null)
    {
        if(matchManager==null || !matchManager.IsOnlineAuthorityControlled || matchManager.IsGameplayInputBlocked || !isActivated || OnlinePenaltyMoving)return false;
        switch(intent)
        {
            case OnlinePenaltyIntent.Player:
                if(!OnlinePenaltyPlayers.Contains(player))return false;
                OnClickReceived(player,null);return true;
            case OnlinePenaltyIntent.Target:
                if(!OnlinePenaltyTargets.Contains(target))return false;
                OnClickReceived(null,target);return true;
            case OnlinePenaltyIntent.Confirm:
                if(!OnlinePenaltyCanConfirm)return false;
                AttemptToAdvanceSetupPhase();return true;
            default:return false;
        }
    }
}
