using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace CounterAttack.Online
{
    public sealed partial class OnlineGroundPassAuthority
    {
        private ThrowInManager throwInRules;
        private ThrowInManager ThrowInRules=>throwInRules!=null?throwInRules:throwInRules=UnityEngine.Object
            .FindObjectsByType<ThrowInManager>(FindObjectsSortMode.None).FirstOrDefault(m=>m.ball==match.ball && m.hexGrid==match.hexGrid);
        private void ReadThrowInDecision(OnlineGroundPassState state,List<string> actions)
        {
            var manager=ThrowInRules;var phase=manager.OnlineThrowInStage;
            state.stage="throw."+phase;state.owner=manager.awardedTeam==MatchManager.TeamInAttack.Home?"Home":"Away";
            state.selectedPlayer=TokenId(manager.selectedThrower);state.awaitingRoll=false;state.awaitingTarget=phase=="target";
            void Add(string id,OnlineThrowInIntent intent,PlayerToken player=null,HexCell target=null)=>AddAdditional(actions,id,()=>{
                if(!manager.TryExecuteOnlineThrowIn(intent,player,target))throw new InvalidOperationException("Accepted throw-in became unavailable");});
            foreach(var player in manager.OnlineThrowInTakers)Add("throw.player:"+TokenId(player),OnlineThrowInIntent.Taker,player);
            foreach(var hex in manager.OnlineThrowInTargets)Add("throw.target:"+hex.coordinates.x+":"+hex.coordinates.z,OnlineThrowInIntent.Target,target:hex);
            if(phase=="optional"){Add("throw.movement",OnlineThrowInIntent.Movement);Add("throw.now",OnlineThrowInIntent.ThrowNow);}
            if(phase=="execution")
            {Add("throw.feet",OnlineThrowInIntent.Feet);if(manager.OnlineThrowInCanHead)Add("throw.head",OnlineThrowInIntent.Head);}
        }
    }
}
