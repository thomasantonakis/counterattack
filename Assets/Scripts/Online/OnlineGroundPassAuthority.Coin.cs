using System;
using System.Collections.Generic;
namespace CounterAttack.Online
{
    public sealed partial class OnlineGroundPassAuthority
    {
        private void ReadCoinDecision(OnlineGroundPassState state,List<string> actions)
        {
            var coin=state.coin;state.stage="coin."+coin.stage;state.owner="None";state.awaitingTarget=false;state.awaitingRoll=false;
            if(state.isSharedPaused || state.connectionHeld || match.IsGameplayInputBlocked)return;
            void Add(string action)=>AddAdditional(actions,action,()=>{if(!match.coinTossPanel.TryExecuteOnline(action))throw new InvalidOperationException("Accepted coin toss became unavailable");});
            if(coin.stage is "home" or "away"){state.owner=coin.stage=="home"?"Home":"Away";state.awaitingRoll=true;Add("coin.roll");}
            else if(coin.stage=="choice")
            {
                state.owner=coin.choosingHome?"Home":"Away";if(coin.canChooseKickoff)Add("coin.kickoff");Add("coin.left");Add("coin.right");
            }
        }
    }
}
