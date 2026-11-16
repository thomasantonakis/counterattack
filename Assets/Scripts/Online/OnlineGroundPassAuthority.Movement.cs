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
            if(manager.OnlineMovementSnapshotStage=="exhausted"){ReadMovementSnapshotDecision(state,actions);return;}
            if(manager.OnlineDribbleStage!=null){ReadDribbleDecision(state,actions);return;}
            string phase=manager.isMovementPhaseDef?"defence":manager.isMovementPhase2f2?"two-for-two":"attack";
            state.owner=manager.IsInstructionExpectingHomeTeam()==true?"Home":"Away";
            state.stage="movement."+phase+".resolving";
            if(!manager.OnlineBasicMovementAvailable) return;
            if(manager.isAwaitingTokenSelection)
            {
                state.stage="movement."+phase+".select";
                AddMovementSelections(actions,manager);
            }
            if(manager.isAwaitingHexDestination)
            {
                state.stage="movement."+phase+".target";
                state.awaitingTarget=AddMovementTargets(actions,manager)>0;
                if(manager.OnlineCanStationaryFootTackle)AddGroundDuel(actions,"duel.tackle",OnlineGroundDuelIntent.Tackle);
            }
        }
        private void AddMovementSelections(List<string> actions,MovementPhaseManager manager,bool includeForfeit=true)
        {
            if(manager.OnlineCanSelectMovementPlayer)
                foreach(var player in manager.GetSelectableMovementTokensForDecisionPreview())
                    AddAdditional(actions,MovementPlayerAction(TokenId(player)),()=>
                    {if(!manager.TryExecuteOnlineMovement(OnlineMovementIntent.SelectPlayer,player))throw new InvalidOperationException("Accepted player selection became unavailable");});
            if(includeForfeit && manager.OnlineCanForfeitMovement)AddAdditional(actions,MovementForfeitAction,()=>
                {if(!manager.TryExecuteOnlineMovement(OnlineMovementIntent.Forfeit))throw new InvalidOperationException("Accepted movement forfeit became unavailable");});
        }
        private int AddMovementTargets(List<string> actions,MovementPhaseManager manager)
        {
            int count=0;if(!manager.OnlineBasicMovementAvailable || !manager.isAwaitingHexDestination)return count;
            foreach(var hex in match.hexGrid.highlightedHexes)
                if(hex!=null && hex.GetOccupyingToken()==null && manager.IsHexValidForMovement(hex))
                {
                    var destination=hex;AddAdditional(actions,MovementTargetAction(hex.coordinates.x,hex.coordinates.z),()=>
                    {if(!manager.TryExecuteOnlineMovement(OnlineMovementIntent.Move,target:destination))throw new InvalidOperationException("Accepted movement destination became unavailable");});count++;
                }
            return count;
        }
    }
}
