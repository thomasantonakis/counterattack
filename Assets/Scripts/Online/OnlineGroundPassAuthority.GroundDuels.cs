using System;
using System.Collections.Generic;

namespace CounterAttack.Online
{
    public sealed partial class OnlineGroundPassAuthority
    {
        private void ReadGroundDuelDecision(OnlineGroundPassState state,List<string> actions)
        {
            var manager=match.movementPhaseManager;var phase=manager.OnlineGroundDuelStage;var actor=manager.OnlineGroundDuelActor;
            state.stage="duel."+phase;state.selectedPlayer=TokenId(actor);state.owner=actor==null?"None":actor.isHomeTeam?"Home":"Away";
            state.awaitingRoll=phase=="roll";state.awaitingTarget=phase=="reposition";
            switch(phase)
            {
                case "nutmeg":AddGroundDuel(actions,"duel.nutmeg",OnlineGroundDuelIntent.Nutmeg);break;
                case "victim":foreach(var player in manager.OnlineNutmegVictims)AddGroundDuel(actions,"duel.victim:"+TokenId(player),OnlineGroundDuelIntent.Victim,player);break;
                case "choice":AddGroundDuel(actions,"duel.tackle",OnlineGroundDuelIntent.Tackle);AddGroundDuel(actions,"duel.stand",OnlineGroundDuelIntent.Stand);break;
                case "roll":AddGroundDuel(actions,"duel.roll",OnlineGroundDuelIntent.Roll);break;
                case "reposition":
                    foreach(var hex in manager.OnlineDuelRepositionTargets)AddGroundDuel(actions,"duel.target:"+hex.coordinates.x+":"+hex.coordinates.z,OnlineGroundDuelIntent.Reposition,target:hex);
                    if(manager.OnlineCanStayAfterDuel)AddGroundDuel(actions,"duel.stay",OnlineGroundDuelIntent.Stay);break;
            }
        }
        private void AddGroundDuel(List<string> actions,string id,OnlineGroundDuelIntent intent,PlayerToken player=null,HexCell target=null)
            =>AddAdditional(actions,id,()=>{if(!match.movementPhaseManager.TryExecuteOnlineGroundDuel(intent,player,target))
                throw new InvalidOperationException("Accepted ground duel became unavailable");});
    }
}
