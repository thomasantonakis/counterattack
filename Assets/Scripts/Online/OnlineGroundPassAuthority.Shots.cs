using System;
using System.Collections.Generic;

namespace CounterAttack.Online
{
    public sealed partial class OnlineGroundPassAuthority
    {
        public const string ShotSaveAction="shot.save",ShotHandlingAction="shot.handling";
        private void ReadShotDecision(OnlineGroundPassState state,List<string> actions)
        {
            var manager=match.shotManager;var phase=manager.OnlineShotStage;
            state.stage="shot."+phase;state.selectedPlayer=TokenId(manager.OnlineShotActor);
            var home=manager.IsInstructionExpectingHomeTeam();state.owner=home==null?"None":home.Value?"Home":"Away";
            state.awaitingRoll=phase is "header.save" or "handling";
            if(home==null || !state.awaitingRoll)return;
            var intent=phase=="header.save"?OnlineShotIntent.HeaderSave:OnlineShotIntent.Handling;
            AddAdditional(actions,phase=="header.save"?ShotSaveAction:ShotHandlingAction,()=>
            {if(!manager.TryExecuteOnlineShot(intent))throw new InvalidOperationException("Accepted shot roll became unavailable");});
        }
    }
}
