using System;
using System.Collections.Generic;

namespace CounterAttack.Online
{
    public sealed partial class OnlineGroundPassAuthority
    {
        private void ReadDribbleDecision(OnlineGroundPassState state,List<string> actions)
        {
            var manager=match.movementPhaseManager;var phase=manager.OnlineDribbleStage;var actor=manager.OnlineDribbleActor;
            state.stage="dribble."+phase;state.selectedPlayer=TokenId(actor);
            state.owner=actor==null?"None":actor.isHomeTeam?"Home":"Away";
            state.awaitingRoll=phase=="steal";state.awaitingTarget=phase=="target";
            if(phase=="target")
            {
                foreach(var hex in manager.OnlineDribbleTargets)AddDribble(actions,MovementTargetAction(hex.coordinates.x,hex.coordinates.z),OnlineDribbleIntent.Move,hex);
                AddDribble(actions,"dribble.finish",OnlineDribbleIntent.Finish);
            }
            else if(phase=="nutmeg")AddDribble(actions,"dribble.decline-nutmeg",OnlineDribbleIntent.DeclineNutmeg);
            else if(phase=="steal")AddDribble(actions,"dribble.roll",OnlineDribbleIntent.Steal);
        }
        private void AddDribble(List<string> actions,string id,OnlineDribbleIntent intent,HexCell target=null)
            =>AddAdditional(actions,id,()=>{if(!match.movementPhaseManager.TryExecuteOnlineDribble(intent,target))
                throw new InvalidOperationException("Accepted dribble decision became unavailable");});
    }
}
