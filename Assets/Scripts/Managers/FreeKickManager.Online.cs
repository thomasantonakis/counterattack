using System;
using System.Linq;
public enum OnlineSetPieceIntent { None, Player, Target, Skip, Ground, High, Long, Shot }
public partial class FreeKickManager
{
    public string OnlineSetPieceStage=>!isActivated?null:IsResolvingSetup?"resolving"
        :IsExecutionChildManagerActive() && !(matchManager.shotManager?.isWaitingForShotCommitConfirmation==true)?null
        :isWaitingForKickerSelection?"initial":isWaitingForFinalKickerSelection?"kicker"
        :isWaitingForSetupPhase?(selectedToken==null?"select":"target"):isWaitingForExecution?"execution":"resolving";
    public PlayerToken[] OnlineSetPiecePlayers=>OnlineSetPieceStage=="initial"?GetEligibleInitialKickersForDecision().Where(OnlineSetPiecePlayer).ToArray()
        :OnlineSetPieceStage=="kicker"?potentialKickers.Where(OnlineSetPiecePlayer).ToArray()
        :OnlineSetPieceStage is "select" or "target"?GetEligibleSetupTokensForDecision().Where(OnlineSetPiecePlayer).ToArray():Array.Empty<PlayerToken>();
    private static bool OnlineSetPiecePlayer(PlayerToken player)=>player!=null && player.isPlaying && !player.isSentOff && player.currentHex!=null;
    public HexCell[] OnlineSetPieceTargets=>OnlineSetPieceStage=="target"?GetLegalSetupDestinationsForDecision().Where(h=>h.GetOccupyingToken()==null).ToArray():Array.Empty<HexCell>();
    public bool OnlineSetPieceCanSkip=>OnlineSetPieceStage=="initial"?(!isCornerKick || GetPotentialKickersAroundBall().Count>0)
        :OnlineSetPieceStage is "select" or "target" && CanForfeitCurrentInstructionPhase();
    public bool OnlineSetPieceCanShoot=>OnlineSetPieceStage=="execution" && !isCornerKick && IsFreeKickShotAvailable();
    public bool TryExecuteOnlineSetPiece(OnlineSetPieceIntent intent,PlayerToken player=null,HexCell target=null)
    {
        if(matchManager==null || !matchManager.IsOnlineAuthorityControlled || matchManager.IsGameplayInputBlocked
            || finalThirdManager?.isActivated==true || OnlineSetPieceStage==null || IsResolvingSetup)return false;
        switch(intent)
        {
            case OnlineSetPieceIntent.Player:
                if(!OnlineSetPiecePlayers.Contains(player))return false;
                OnClickReceived(player,player.currentHex);return true;
            case OnlineSetPieceIntent.Target:
                if(!OnlineSetPieceTargets.Contains(target))return false;
                OnClickReceived(null,target);return true;
            case OnlineSetPieceIntent.Skip:
                if(!OnlineSetPieceCanSkip)return false;
                if(isWaitingForKickerSelection){isHandlingKickerSelection=true;StartCoroutine(HandleKickerSelection());}
                else{ClearSetupMoveHover();selectedToken=null;hexGrid.ClearHighlightedHexes();AttemptToAdvanceToNextPhase();}
                return true;
            case OnlineSetPieceIntent.Ground:case OnlineSetPieceIntent.High:case OnlineSetPieceIntent.Long:case OnlineSetPieceIntent.Shot:
                if(OnlineSetPieceStage!="execution" || intent==OnlineSetPieceIntent.Shot && !OnlineSetPieceCanShoot
                    || isCornerKick && intent is OnlineSetPieceIntent.Long or OnlineSetPieceIntent.Shot)return false;
                return isCornerKick?HandleCornerKickExecutionChoice(intent):HandleFreeKickExecutionChoice(intent);
            default:return false;
        }
    }
    private static OnlineSetPieceIntent SetPieceChoice(UnityEngine.KeyCode key)=>key switch
    {UnityEngine.KeyCode.P=>OnlineSetPieceIntent.Ground,UnityEngine.KeyCode.C=>OnlineSetPieceIntent.High,
     UnityEngine.KeyCode.L=>OnlineSetPieceIntent.Long,UnityEngine.KeyCode.S=>OnlineSetPieceIntent.Shot,_=>OnlineSetPieceIntent.None};
}
