using System;
using System.Collections.Generic;
namespace CounterAttack.Online
{
    public sealed partial class OnlineGroundPassAuthority
    {
        private void ReadFoulDecision(OnlineGroundPassState state,List<string> actions)
        {
            var manager=match.movementPhaseManager;var phase=manager.OnlineFoulStage;var actor=manager.OnlineFoulActor;
            state.stage="foul."+phase;state.selectedPlayer=TokenId(actor);state.owner=actor==null?"None":actor.isHomeTeam?"Home":"Away";
            state.awaitingRoll=phase=="leniency" || phase=="injury";
            void Add(string id,OnlineFoulIntent intent)=>AddAdditional(actions,id,()=>{
                if(!manager.TryExecuteOnlineFoul(intent))throw new InvalidOperationException("Accepted foul decision became unavailable");});
            if(phase=="leniency")Add("foul.leniency",OnlineFoulIntent.Leniency);
            if(phase=="injury")Add("foul.injury",OnlineFoulIntent.Injury);
            if(phase=="choice")
            {
                if(manager.OnlineCanPlayAdvantage)Add("foul.advantage",OnlineFoulIntent.Advantage);
                Add("foul.take",OnlineFoulIntent.Take);
            }
        }
    }
}
