using System;
using System.Collections.Generic;

namespace CounterAttack.Online
{
    public sealed partial class OnlineGroundPassAuthority
    {
        public const string FinalThirdForfeitAction="final-third.forfeit",FinalThirdDropAction="final-third.drop",
            FinalThirdKickAction="final-third.kick",FinalThirdPassAction="final-third.pass";
        public static string FinalThirdPlayerAction(string playerId)=>"final-third.player:"+playerId;
        public static string FinalThirdTargetAction(int x,int z)=>"final-third.target:"+x+":"+z;

        private void ReadFinalThirdDecision(OnlineGroundPassState state,List<string> actions)
        {
            var manager=match.finalThirdManager;
            state.stage="final-third."+manager.OnlineDecisionStage;
            state.selectedPlayer=TokenId(manager.OnlineSelectedPlayer);
            var home=manager.IsInstructionExpectingHomeTeam();
            state.owner=home.HasValue?(home.Value?"Home":"Away"):"None";
            if(!home.HasValue)return;
            void Command(string id,OnlineFinalThirdIntent intent,PlayerToken player=null,HexCell target=null)=>AddAdditional(actions,id,()=>
            {if(!manager.TryExecuteOnlineFinalThird(intent,player,target))throw new InvalidOperationException("Accepted Final Third decision became unavailable");});
            if(manager.OnlineCanForfeit)Command(FinalThirdForfeitAction,OnlineFinalThirdIntent.Forfeit);
            switch(manager.OnlineDecisionStage)
            {
                case "select":
                    foreach(var player in manager.OnlineSelectablePlayers)Command(FinalThirdPlayerAction(TokenId(player)),OnlineFinalThirdIntent.SelectPlayer,player);
                    break;
                case "target":
                    foreach(var hex in manager.GetMovementDestinationsForDecisionPreview(manager.OnlineSelectedPlayer))
                        if(match.hexGrid.highlightedHexes.Contains(hex))Command(FinalThirdTargetAction(hex.coordinates.x,hex.coordinates.z),OnlineFinalThirdIntent.Move,target:hex);
                    break;
                case "keeper-choice":
                    Command(FinalThirdKickAction,OnlineFinalThirdIntent.GoalkeeperKick);
                    if(manager.isWaitingForSetPieceGoalKickChoice)Command(FinalThirdPassAction,OnlineFinalThirdIntent.GoalKickPass);
                    else Command(FinalThirdDropAction,OnlineFinalThirdIntent.DropBall);
                    break;
            }
        }
    }
}
