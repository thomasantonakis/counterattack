using System;
using System.Collections.Generic;
namespace CounterAttack.Online
{
    public sealed partial class OnlineGroundPassAuthority
    {
        private void ReadPenaltyDecision(OnlineGroundPassState state,List<string> actions)
        {
            var manager=match.penaltyKickManager;var phase=manager.OnlinePenaltyStage;
            state.stage="penalty."+phase;var home=manager.IsInstructionExpectingHomeTeam();state.owner=home==null?"None":home.Value?"Home":"Away";
            state.selectedPlayer=TokenId(manager.selectedToken??manager.selectedKicker);state.awaitingRoll=false;state.awaitingTarget=phase=="target";
            void Add(string id,OnlinePenaltyIntent intent,PlayerToken player=null,HexCell target=null)=>AddAdditional(actions,id,()=>{
                if(!manager.TryExecuteOnlinePenalty(intent,player,target))throw new InvalidOperationException("Accepted penalty setup became unavailable");});
            foreach(var player in manager.OnlinePenaltyPlayers)Add("penalty.player:"+TokenId(player),OnlinePenaltyIntent.Player,player);
            foreach(var hex in manager.OnlinePenaltyTargets)Add("penalty.target:"+hex.coordinates.x+":"+hex.coordinates.z,OnlinePenaltyIntent.Target,target:hex);
            if(manager.OnlinePenaltyCanConfirm)Add("penalty.confirm",OnlinePenaltyIntent.Confirm);
        }
    }
}
