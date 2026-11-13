using System;
using System.Collections.Generic;

namespace CounterAttack.Online
{
    public sealed partial class OnlineGroundPassAuthority
    {
        public const string KeeperQuickAction="keeper.quick",KeeperFinalThirdAction="keeper.final-third",KeeperQuickStartAction="keeper.quick.start";
        private void ReadKeeperDistribution(OnlineGroundPassState state,List<string> actions)
        {
            var manager=match.shotManager;state.stage="keeper.distribution";
            state.selectedPlayer=TokenId(manager.OnlineDistributionKeeper);
            var home=manager.IsInstructionExpectingHomeTeam();state.owner=home==null?"None":home.Value?"Home":"Away";
            if(home==null || !manager.OnlineCanDistribute)return;
            void Command(string id,OnlineKeeperDistributionIntent intent)=>AddAdditional(actions,id,()=>
            {if(!manager.TryExecuteOnlineKeeperDistribution(intent))throw new InvalidOperationException("Accepted keeper distribution became unavailable");});
            Command(KeeperQuickAction,OnlineKeeperDistributionIntent.QuickThrow);
            Command(KeeperFinalThirdAction,OnlineKeeperDistributionIntent.FinalThird);
        }
    }
}
