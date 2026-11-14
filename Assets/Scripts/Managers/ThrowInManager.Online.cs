using System;
using System.Collections;
using System.Linq;
public enum OnlineThrowInIntent { Taker, Movement, ThrowNow, Feet, Head, Target }
public partial class ThrowInManager
{
    private bool onlineSelectingThrower;
    public bool OnlineThrowInMoving=>onlineSelectingThrower;
    public string OnlineThrowInStage=>!isActivated?null
        :isRunningMandatoryMovement || isRunningOptionalMovement?(movementPhaseManager?.isActivated==true?null:"resolving")
        :onlineSelectingThrower || ball?.isMoving==true?"resolving":isWaitingForTakerSelection?"taker"
        :isWaitingForOptionalMovementDecision?"optional":isWaitingForThrowTypeSelection?"execution"
        :isWaitingForHeaderTarget?"target":"resolving";
    public PlayerToken[] OnlineThrowInTakers=>OnlineThrowInStage=="taker"?GetEligibleThrowersForDecision()
        .Where(t=>t!=null && t.isPlaying && !t.isSentOff && t.currentHex!=null).ToArray():Array.Empty<PlayerToken>();
    public HexCell[] OnlineThrowInTargets=>OnlineThrowInStage=="target"?GetAvailableHeaderThrowTargets().ToArray():Array.Empty<HexCell>();
    public bool OnlineThrowInCanHead=>OnlineThrowInStage=="execution" && GetAvailableHeaderThrowTargets().Count>0;
    public bool TryExecuteOnlineThrowIn(OnlineThrowInIntent intent,PlayerToken player=null,HexCell target=null)
    {
        var match=MatchManager.Instance;
        if(match==null || !match.IsOnlineAuthorityControlled || match.IsGameplayInputBlocked
            || !isActivated || onlineSelectingThrower || ball?.isMoving==true || finalThirdManager?.isActivated==true)return false;
        switch(intent)
        {
            case OnlineThrowInIntent.Taker:
                if(!OnlineThrowInTakers.Contains(player))return false;
                onlineSelectingThrower=true;StartCoroutine(SelectOnlineThrower(player));return true;
            case OnlineThrowInIntent.Movement:case OnlineThrowInIntent.ThrowNow:
                if(OnlineThrowInStage!="optional")return false;
                isWaitingForOptionalMovementDecision=false;
                if(intent==OnlineThrowInIntent.Movement)
                {RecordThrowInSelection("optional_movement_selected","M");StartCoroutine(RunOptionalMovementPhaseThenThrow());}
                else{RecordThrowInSelection("throw_now_selected","T");EnterThrowExecutionSelection();}
                return true;
            case OnlineThrowInIntent.Feet:
                if(OnlineThrowInStage!="execution")return false;
                RecordThrowInSelection("throw_to_feet_selected","P");HandleThrowToFeet();return true;
            case OnlineThrowInIntent.Head:
                if(!OnlineThrowInCanHead)return false;
                return ChooseHeaderThrow();
            case OnlineThrowInIntent.Target:
                if(!OnlineThrowInTargets.Contains(target))return false;
                OnClickReceived(null,target);return true;
            default:return false;
        }
    }
    private IEnumerator SelectOnlineThrower(PlayerToken player)
    {
        try{yield return HandleThrowerSelection(player);}
        finally{onlineSelectingThrower=false;}
    }
}
