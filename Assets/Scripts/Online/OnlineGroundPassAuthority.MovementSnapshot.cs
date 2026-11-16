using System;
using System.Collections.Generic;
namespace CounterAttack.Online
{
    public sealed partial class OnlineGroundPassAuthority
    {
        private void ReadMovementSnapshotDecision(OnlineGroundPassState state,List<string> actions)
        {
            var manager=match.movementPhaseManager;var actor=manager.OnlineMovementSnapshotActor;
            state.stage="movement.snapshot";state.selectedPlayer=TokenId(actor);state.owner=actor==null?"None":actor.isHomeTeam?"Home":"Away";
            state.awaitingRoll=state.awaitingTarget=false;
            if(actor==null)return;
            if(manager.OnlineCanTakeMovementSnapshot)AddMovementSnapshot(actions,"movement.snapshot",OnlineMovementSnapshotIntent.Shoot);
            AddMovementSnapshot(actions,"movement.snapshot.decline",OnlineMovementSnapshotIntent.Continue);
        }
        private void AddMovementSnapshot(List<string> actions,string id,OnlineMovementSnapshotIntent intent)
            =>AddAdditional(actions,id,()=>{if(!match.movementPhaseManager.TryExecuteOnlineMovementSnapshot(intent))throw new InvalidOperationException("Accepted snapshot offer became unavailable");});
    }
}
