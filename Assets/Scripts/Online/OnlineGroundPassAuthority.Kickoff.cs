using System;
using System.Collections.Generic;

namespace CounterAttack.Online
{
    public sealed partial class OnlineGroundPassAuthority
    {
        public const string KickoffConfirmAction="kickoff.confirm";
        public static string KickoffPlayerAction(string id)=>"kickoff.player:"+id;
        public static string KickoffMoveAction(int x,int z)=>"kickoff.move:"+x+":"+z;
        public static string KickoffTakerAction(string id)=>"kickoff.taker:"+id;
        private void ReadKickoffDecision(OnlineGroundPassState state,List<string> actions)
        {
            var manager=match.kickoffManager;var phase=manager.OnlineStage;
            state.stage="kickoff."+phase;state.owner=manager.OnlineOwner;
            state.selectedPlayer=TokenId(manager.selectedToken);
            state.awaitingRoll=false;state.awaitingTarget=phase=="setup" && manager.selectedToken!=null;
            if(phase=="setup")
            {
                foreach(var player in manager.OnlinePlayers)AddKickoff(actions,KickoffPlayerAction(TokenId(player)),OnlineKickoffIntent.Select,player);
                foreach(var hex in manager.OnlineDestinations)AddKickoff(actions,KickoffMoveAction(hex.coordinates.x,hex.coordinates.z),OnlineKickoffIntent.Move,target:hex);
                AddKickoff(actions,KickoffConfirmAction,OnlineKickoffIntent.Confirm);
            }
            else if(phase=="taker")foreach(var player in manager.OnlinePlayers)
                AddKickoff(actions,KickoffTakerAction(TokenId(player)),OnlineKickoffIntent.Taker,player);
        }
        private void AddKickoff(List<string> actions,string id,OnlineKickoffIntent intent,PlayerToken player=null,HexCell target=null)
            =>AddAdditional(actions,id,()=>{if(!match.kickoffManager.TryExecuteOnlineKickoff(intent,player,target))
                throw new InvalidOperationException("Accepted kickoff decision became unavailable");});
    }
}
