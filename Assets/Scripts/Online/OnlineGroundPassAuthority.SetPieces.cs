using System;
using System.Collections.Generic;
namespace CounterAttack.Online
{
    public sealed partial class OnlineGroundPassAuthority
    {
        private void ReadSetPieceDecision(OnlineGroundPassState state,List<string> actions)
        {
            var manager=match.freeKickManager;var phase=manager.OnlineSetPieceStage;
            state.stage="set-piece."+phase;var home=manager.IsInstructionExpectingHomeTeam();state.owner=home==null?"None":home.Value?"Home":"Away";
            state.selectedPlayer=TokenId(manager.selectedToken??manager.selectedKicker);state.awaitingRoll=false;state.awaitingTarget=phase=="target";
            void Add(string id,OnlineSetPieceIntent intent,PlayerToken player=null,HexCell target=null)=>AddAdditional(actions,id,()=>{
                if(!manager.TryExecuteOnlineSetPiece(intent,player,target))throw new InvalidOperationException("Accepted set piece became unavailable");});
            foreach(var player in manager.OnlineSetPiecePlayers)Add("set-piece.player:"+TokenId(player),OnlineSetPieceIntent.Player,player);
            foreach(var hex in manager.OnlineSetPieceTargets)Add("set-piece.target:"+hex.coordinates.x+":"+hex.coordinates.z,OnlineSetPieceIntent.Target,target:hex);
            if(manager.OnlineSetPieceCanSkip)Add("set-piece.skip",OnlineSetPieceIntent.Skip);
            if(phase=="execution")
            {
                Add("set-piece.ground",OnlineSetPieceIntent.Ground);Add("set-piece.high",OnlineSetPieceIntent.High);
                if(!manager.isCornerKick)Add("set-piece.long",OnlineSetPieceIntent.Long);
                if(manager.OnlineSetPieceCanShoot)Add("set-piece.shot",OnlineSetPieceIntent.Shot);
            }
        }
    }
}
