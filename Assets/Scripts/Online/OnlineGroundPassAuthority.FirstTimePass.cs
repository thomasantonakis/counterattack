using System;
using System.Collections.Generic;

namespace CounterAttack.Online
{
    public sealed partial class OnlineGroundPassAuthority
    {
        private void ReadFirstTimePassDecision(OnlineGroundPassState state,List<string> actions)
        {
            var manager=match.firstTimePassManager;var phase=manager.OnlineFtpStage;
            state.stage="ftp."+phase;state.selectedPlayer=TokenId(manager.OnlineFtpActor);
            var home=manager.IsInstructionExpectingHomeTeam();state.owner=home==null?"None":home.Value?"Home":"Away";
            state.awaitingRoll=phase=="roll";state.awaitingTarget=phase=="target";
            state.hasSelectedTarget=manager.currentTargetHex!=null;
            if(state.hasSelectedTarget){state.selectedX=manager.currentTargetHex.coordinates.x;state.selectedZ=manager.currentTargetHex.coordinates.z;}
            if(home==null)return;
            if(phase=="target")foreach(var hex in manager.GetLegalFirstTimePassTargetsForDecisionPreview())
                AddFtp(actions,"ftp.target:"+hex.coordinates.x+":"+hex.coordinates.z,OnlineFtpIntent.Target,target:hex);
            else if(phase is "attacker" or "defender")
            {
                foreach(var player in manager.OnlineFtpPlayers)AddFtp(actions,"ftp.player:"+TokenId(player),OnlineFtpIntent.Select,player);
                foreach(var hex in manager.OnlineFtpDestinations)AddFtp(actions,"ftp.move:"+hex.coordinates.x+":"+hex.coordinates.z,OnlineFtpIntent.Move,target:hex);
                if(manager.OnlineCanSkipFtp)AddFtp(actions,"ftp.skip",OnlineFtpIntent.Skip);
            }
            else if(phase=="roll")AddFtp(actions,"ftp.roll",OnlineFtpIntent.Roll);
        }
        private void AddFtp(List<string> actions,string id,OnlineFtpIntent intent,PlayerToken player=null,HexCell target=null)
            =>AddAdditional(actions,id,()=>{if(!match.firstTimePassManager.TryExecuteOnlineFtp(intent,player,target))
                throw new InvalidOperationException("Accepted FTP decision became unavailable");});
    }
}
