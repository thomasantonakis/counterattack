using System;
using System.Collections.Generic;

namespace CounterAttack.Online
{
    public sealed partial class OnlineGroundPassAuthority
    {
        private GoalKeeperManager KeeperRules=>match.movementPhaseManager?.goalKeeperManager;
        private void ReadGoalkeeperDecision(OnlineGroundPassState state,List<string> actions)
        {
            var manager=KeeperRules;var phase=manager.OnlineStage;var actor=manager.OnlineActor;
            state.stage="keeper."+phase;state.selectedPlayer=TokenId(actor);
            state.owner=actor==null?"None":actor.isHomeTeam?"Home":"Away";
            state.awaitingRoll=false;state.awaitingTarget=phase=="move";
            if(actor==null)return;
            if(phase=="move")
            {
                AddGoalkeeper(actions,"keeper.stay",OnlineGoalkeeperIntent.Stay);
                foreach(var hex in manager.OnlineMoveTargets)AddGoalkeeper(actions,"keeper.move:"+hex.coordinates.x+":"+hex.coordinates.z,OnlineGoalkeeperIntent.Move,hex);
            }
            else if(phase=="pickup" && match.CanOfferGoalkeeperPickup(actor))
            {
                AddGoalkeeper(actions,"keeper.pickup.hands",OnlineGoalkeeperIntent.Hands);
                AddGoalkeeper(actions,"keeper.pickup.feet",OnlineGoalkeeperIntent.Feet);
            }
        }
        private void AddGoalkeeper(List<string> actions,string id,OnlineGoalkeeperIntent intent,HexCell target=null)
            =>AddAdditional(actions,id,()=>{if(!KeeperRules.TryExecuteOnlineGoalkeeper(intent,target))
                throw new InvalidOperationException("Accepted goalkeeper choice became unavailable");});
    }
}
