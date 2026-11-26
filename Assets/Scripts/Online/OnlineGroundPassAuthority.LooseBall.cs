using System;
using System.Collections.Generic;

namespace CounterAttack.Online
{
    public sealed partial class OnlineGroundPassAuthority
    {
        public const string LooseRollAction="loose.roll";
        private void ReadLooseBallDecision(OnlineGroundPassState state,List<string> actions)
        {
            var manager=match.looseBallManager;var phase=manager.OnlineDecisionStage;
            state.stage="loose."+phase;state.selectedPlayer=TokenId(manager.OnlineRollActor);
            var home=manager.IsInstructionExpectingHomeTeam();state.owner=home==null?"None":home.Value?"Home":"Away";
            state.awaitingRoll=phase!="resolving";
            if(home==null || phase=="resolving")return;
            var intent=phase=="direction"?OnlineLooseBallIntent.Direction:phase=="distance"?OnlineLooseBallIntent.Distance:OnlineLooseBallIntent.Recovery;
            AddAdditional(actions,LooseRollAction,()=>
            {if(!manager.TryExecuteOnlineLooseBall(intent))throw new InvalidOperationException("Accepted loose-ball roll became unavailable");});
        }
    }
}
