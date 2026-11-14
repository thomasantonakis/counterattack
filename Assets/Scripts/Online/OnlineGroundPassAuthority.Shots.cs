using System;
using System.Collections.Generic;

namespace CounterAttack.Online
{
    public sealed partial class OnlineGroundPassAuthority
    {
        public const string ShotSaveAction="shot.save",ShotHandlingAction="shot.handling";
        public const string ShotStartAction="shot.start",ShotConfirmAction="shot.confirm",ShotCancelAction="shot.cancel",ShotRollAction="shot.roll";
        public static string ShotTargetAction(int x,int z)=>"shot.target:"+x+":"+z;
        public static string ShotPlayerAction(string id)=>"shot.player:"+id;
        public static string ShotMoveAction(int x,int z)=>"shot.move:"+x+":"+z;
        private void ReadShotDecision(OnlineGroundPassState state,List<string> actions)
        {
            var manager=match.shotManager;var phase=manager.OnlineShotStage;
            state.stage="shot."+phase;state.selectedPlayer=TokenId(manager.OnlineShotActor);
            var home=manager.IsInstructionExpectingHomeTeam();state.owner=home==null?"None":home.Value?"Home":"Away";
            state.awaitingRoll=phase is "header.save" or "handling" or "roll" or "save";
            state.awaitingTarget=phase is "target" or "snapshot.target";
            if(manager.targetHex!=null){state.hasSelectedTarget=true;state.selectedX=manager.targetHex.coordinates.x;state.selectedZ=manager.targetHex.coordinates.z;}
            if(home==null)return;
            switch(phase)
            {
                case "confirm":
                    if(manager.OnlineCanSelectShot)AddShotCommand(actions,ShotConfirmAction,OnlineShotIntent.Confirm);
                    AddShotCommand(actions,ShotCancelAction,OnlineShotIntent.CancelPreview);break;
                case "target":foreach(var hex in manager.OnlineShotTargets)AddShotCommand(actions,ShotTargetAction(hex.coordinates.x,hex.coordinates.z),OnlineShotIntent.Target,target:hex);break;
                case "snapshot.select":case "snapshot.target":
                    foreach(var player in manager.OnlineSnapshotBlockers)AddShotCommand(actions,ShotPlayerAction(TokenId(player)),OnlineShotIntent.SelectBlocker,player);
                    if(phase=="snapshot.target")foreach(var hex in manager.OnlineSnapshotDestinations)AddShotCommand(actions,ShotMoveAction(hex.coordinates.x,hex.coordinates.z),OnlineShotIntent.MoveBlocker,target:hex);
                    break;
                case "roll":AddShotCommand(actions,ShotRollAction,OnlineShotIntent.Shoot);break;
                case "save":AddShotCommand(actions,ShotSaveAction,OnlineShotIntent.Save);break;
                case "header.save":AddShotCommand(actions,ShotSaveAction,OnlineShotIntent.HeaderSave);break;
                case "handling":AddShotCommand(actions,ShotHandlingAction,OnlineShotIntent.Handling);break;
            }
        }
        private void AddShotCommand(List<string> actions,string id,OnlineShotIntent intent,PlayerToken player=null,HexCell target=null)
            =>AddAdditional(actions,id,()=>{if(!match.shotManager.TryExecuteOnlineShot(intent,player,target))throw new InvalidOperationException("Accepted shot decision became unavailable");});
    }
}
