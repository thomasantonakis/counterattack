using System;
using System.Collections.Generic;

namespace CounterAttack.Online
{
    public sealed partial class OnlineGroundPassAuthority
    {
        private HeaderManager HeaderRules=>match.highPassManager?.headerManager ?? match.shotManager?.headerManager;
        public const string HighStartAction="high.start",HighRollAction="high.roll",HighSkipAction="high.skip",
            HighKeeperStayAction="high.keeper.stay",HighKeeperDeclineAction="high.keeper.decline",
            HeaderControlAction="header.control",HeaderControlRollAction="header.control.roll";
        public static string HighTargetAction(int x,int z)=>"high.target:"+x+":"+z;
        public static string HighPlayerAction(string playerId)=>"high.player:"+playerId;
        public static string HighMoveAction(int x,int z)=>"high.move:"+x+":"+z;
        public static string HighKeeperMoveAction(int x,int z)=>"high.keeper.move:"+x+":"+z;
        public static string HeaderControlPlayerAction(string playerId)=>"header.control.player:"+playerId;

        private void ReadHighPassDecision(OnlineGroundPassState state,List<string> actions)
        {
            var manager=match.highPassManager;string phase=manager.OnlineDecisionStage;
            state.stage="high."+phase;state.owner=manager.IsInstructionExpectingHomeTeam()==true?"Home":"Away";
            state.awaitingTarget=manager.isWaitingForConfirmation;state.awaitingRoll=phase is "accuracy" or "direction" or "distance";
            state.selectedPlayer=TokenId(manager.selectedToken);
            void Command(string id,OnlineHighPassIntent intent,PlayerToken player=null,HexCell target=null)=>AddAdditional(actions,id,()=>
            {if(!manager.TryExecuteOnlineHighPass(intent,player,target))throw new InvalidOperationException("Accepted high-pass decision became unavailable");});
            switch(phase)
            {
                case "target":foreach(var hex in manager.GetLegalOnlineHighPassTargets())Command(HighTargetAction(hex.coordinates.x,hex.coordinates.z),OnlineHighPassIntent.Target,target:hex);break;
                case "attacker":case "attacker-target":
                    if(manager.OnlineCanSkipAttacker)Command(HighSkipAction,OnlineHighPassIntent.SkipAttacker);
                    foreach(var player in manager.OnlineAttackers)Command(HighPlayerAction(TokenId(player)),OnlineHighPassIntent.SelectAttacker,player);
                    if(phase=="attacker-target")foreach(var hex in manager.OnlineActorDestinations)Command(HighMoveAction(hex.coordinates.x,hex.coordinates.z),OnlineHighPassIntent.MoveAttacker,target:hex);
                    break;
                case "defender":case "defender-target":
                    Command(HighSkipAction,OnlineHighPassIntent.SkipDefender);
                    foreach(var player in manager.OnlineDefenders)Command(HighPlayerAction(TokenId(player)),OnlineHighPassIntent.SelectDefender,player);
                    if(phase=="defender-target")foreach(var hex in manager.OnlineActorDestinations)Command(HighMoveAction(hex.coordinates.x,hex.coordinates.z),OnlineHighPassIntent.MoveDefender,target:hex);
                    break;
                case "accuracy":Command(HighRollAction,OnlineHighPassIntent.Accuracy);break;
                case "direction":Command(HighRollAction,OnlineHighPassIntent.Direction);break;
                case "distance":Command(HighRollAction,OnlineHighPassIntent.Distance);break;
                case "keeper":
                    Command(HighKeeperDeclineAction,OnlineHighPassIntent.KeeperDecline);
                    if(manager.OnlineCanKeeperStay)Command(HighKeeperStayAction,OnlineHighPassIntent.KeeperStay);
                    foreach(var hex in manager.gkReachableHexes)Command(HighKeeperMoveAction(hex.coordinates.x,hex.coordinates.z),OnlineHighPassIntent.KeeperMove,target:hex);
                    break;
            }
        }
        private void ReadHeaderControlDecision(OnlineGroundPassState state,List<string> actions,HeaderManager manager)
        {
            string phase=manager.OnlineControlStage;state.stage="header.control."+phase;
            state.owner=manager.IsInstructionExpectingHomeTeam()==true?"Home":"Away";state.awaitingRoll=phase=="roll";
            void Command(string id,OnlineHeaderControlIntent intent,PlayerToken player=null)=>AddAdditional(actions,id,()=>
            {if(!manager.TryExecuteOnlineHeaderControl(intent,player))throw new InvalidOperationException("Accepted header-control decision became unavailable");});
            if(phase=="choice")Command(HeaderControlAction,OnlineHeaderControlIntent.Control);
            else if(phase=="roll")Command(HeaderControlRollAction,OnlineHeaderControlIntent.Roll);
            else if(phase=="player")foreach(var player in manager.OnlineControlPlayers)Command(HeaderControlPlayerAction(TokenId(player)),OnlineHeaderControlIntent.SelectPlayer,player);
        }
    }
}
