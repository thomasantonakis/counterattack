using System;
using System.Collections.Generic;

namespace CounterAttack.Online
{
    public sealed partial class OnlineGroundPassAuthority
    {
        public const string MovementStartAction="movement.start",MovementForfeitAction="movement.forfeit";
        public static string MovementPlayerAction(string playerId)=>"movement.player:"+playerId;
        public static string MovementTargetAction(int x,int z)=>"movement.target:"+x+":"+z;
        private void ReadMovementDecision(OnlineGroundPassState state,List<string> actions)
        {
            var manager=match.movementPhaseManager;
            if(manager.OnlineFoulStage!=null){ReadFoulDecision(state,actions);return;}
            if(manager.OnlineGroundDuelStage!=null){ReadGroundDuelDecision(state,actions);return;}
            if(manager.OnlineDribbleStage!=null){ReadDribbleDecision(state,actions);return;}
            string phase=manager.isMovementPhaseDef?"defence":manager.isMovementPhase2f2?"two-for-two":"attack";
            state.owner=manager.IsInstructionExpectingHomeTeam()==true?"Home":"Away";
            state.stage="movement."+phase+".resolving";
            if(!manager.OnlineBasicMovementAvailable) return;
            void Command(string id,OnlineMovementIntent intent,PlayerToken player=null,HexCell target=null)=>AddAdditional(actions,id,()=>
            {if(!manager.TryExecuteOnlineMovement(intent,player,target))throw new InvalidOperationException("Accepted movement decision became unavailable");});
            if(manager.isAwaitingTokenSelection)
            {
                state.stage="movement."+phase+".select";
                foreach(var player in manager.GetSelectableMovementTokensForDecisionPreview())
                    Command(MovementPlayerAction(TokenId(player)),OnlineMovementIntent.SelectPlayer,player);
                Command(MovementForfeitAction,OnlineMovementIntent.Forfeit);
            }
            if(manager.isAwaitingHexDestination)
            {
                state.stage="movement."+phase+".target";
                foreach(var hex in match.hexGrid.highlightedHexes)
                    if(hex!=null && hex.GetOccupyingToken()==null && manager.IsHexValidForMovement(hex))
                        Command(MovementTargetAction(hex.coordinates.x,hex.coordinates.z),OnlineMovementIntent.Move,target:hex);
            }
        }
    }
}
