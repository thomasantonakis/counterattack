using System;
using System.Collections.Generic;

namespace CounterAttack.Online
{
    public sealed partial class OnlineGroundPassAuthority
    {
        public const string LongStartAction="long.start", LongRollAction="long.roll",
            LongKeeperDeclineAction="long.keeper.decline",LongKeeperClaimAction="long.keeper.claim";
        private readonly Dictionary<string,Action> additionalActions=new(StringComparer.Ordinal);
        public static string LongTargetAction(int x,int z)=>"long.target:"+x+":"+z;
        public static string LongKeeperMoveAction(int x,int z)=>"long.keeper.move:"+x+":"+z;
        private void AddAdditional(List<string> actions,string id,Action execute)
        { actions.Add(id);additionalActions.Add(id,execute); }

        private void ReadLongPassDecision(OnlineGroundPassState state,List<string> actions,string attacking)
        {
            var manager=match.longBallManager;
            state.passActive=true;state.awaitingTarget=manager.isAwaitingTargetSelection;
            var selected=manager.currentTargetHex;
            state.hasSelectedTarget=selected!=null;state.selectedX=selected==null?0:selected.coordinates.x;state.selectedZ=selected==null?0:selected.coordinates.z;
            var phase=manager.OnlineDecisionStage;state.stage="long."+phase;
            state.owner=manager.IsInstructionExpectingHomeTeam()==true?"Home":"Away";
            void Command(string id,OnlineLongBallIntent intent,HexCell hex=null)=>AddAdditional(actions,id,()=>
            { if(!manager.TryExecuteOnlineLongBall(intent,hex)) throw new InvalidOperationException("Accepted long-pass decision became unavailable"); });
            switch(phase)
            {
                case "target":
                    foreach(var hex in manager.GetLegalOnlineLongPassTargets())
                        Command(LongTargetAction(hex.coordinates.x,hex.coordinates.z),OnlineLongBallIntent.Target,hex);
                    break;
                case "accuracy":Command(LongRollAction,OnlineLongBallIntent.Accuracy);break;
                case "direction":Command(LongRollAction,OnlineLongBallIntent.Direction);break;
                case "distance":Command(LongRollAction,OnlineLongBallIntent.Distance);break;
                case "interception":Command(LongRollAction,OnlineLongBallIntent.Interception);break;
                case "keeper":
                    Command(LongKeeperDeclineAction,OnlineLongBallIntent.GoalkeeperDecline);
                    if(manager.OnlineGoalkeeperCanClaim) Command(LongKeeperClaimAction,OnlineLongBallIntent.GoalkeeperClaim);
                    foreach(var hex in manager.OnlineGoalkeeperDestinations)
                        Command(LongKeeperMoveAction(hex.coordinates.x,hex.coordinates.z),OnlineLongBallIntent.GoalkeeperMove,hex);
                    break;
            }
            state.awaitingRoll=phase is "accuracy" or "direction" or "distance" or "interception";
        }
    }
}
