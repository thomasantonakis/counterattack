using System;
using System.Linq;
using CounterAttack.Online;
using UnityEngine;
public partial class PenaltyShootoutManager
{
    private void OnDestroy(){if(ActiveShootout==this)ActiveShootout=null;}
    private bool onlineHomeReady,onlineAwayReady,applyingOnlineOrder;
    private bool FullOnline=>OnlineRoomSession.Current?.HasInitialData==true;
    public bool OnlinePresentationMoving=>isActive && matchManager.currentState!=MatchManager.GameState.PenaltyShootoutKickExecution || winnerFlashInstructionActive;
    public bool OnlineOrderSelection=>orderSelectionStarted && !orderSelectionCompleted && !isActive && !isComplete;
    public OnlineShootoutSnapshot CaptureOnlineShootout()
    {
        if(!IsOrderSelectionOrShootoutStarted)return null;
        return new OnlineShootoutSnapshot{selecting=OnlineOrderSelection,active=isActive,complete=isComplete,homeReady=onlineHomeReady,awayReady=onlineAwayReady,
            homeOrder=homeOrder.Select(OnlineRollPublication.ActorId).ToArray(),awayOrder=awayOrder.Select(OnlineRollPublication.ActorId).ToArray(),
            homeEligible=GetEligiblePlayingTokens(true).Select(OnlineRollPublication.ActorId).ToArray(),awayEligible=GetEligiblePlayingTokens(false).Select(OnlineRollPublication.ActorId).ToArray(),
            homeScore=homeShootoutScore,awayScore=awayShootoutScore,homeAttempts=homeAttempts,awayAttempts=awayAttempts,
            homePreGoals=HomePreShootoutGoals,awayPreGoals=AwayPreShootoutGoals,homeResults=homeResults.ToArray(),awayResults=awayResults.ToArray(),
            suddenDeath=isSuddenDeath,winnerIsHome=winnerIsHome,shooter=OnlineRollPublication.ActorId(currentShooter),keeper=OnlineRollPublication.ActorId(currentDefendingGoalkeeper),
            homeFirst=FirstKickIsHome,rightGoal=UseRightGoal};
    }
    public bool RefreshOnlineOrders()
    {
        if(!FullOnline || OnlineRoomSession.IsGuest || !OnlineOrderSelection)return false;
        int count=Math.Min(GetEligiblePlayingTokens(true).Count,GetEligiblePlayingTokens(false).Count);
        System.Collections.Generic.List<PlayerToken> Reconcile(System.Collections.Generic.List<PlayerToken> order,bool home)
        {
            var eligible=GetEligiblePlayingTokens(home);
            return order.Where(eligible.Contains).Concat(BuildDefaultOrder(home)).Distinct().Take(count).ToList();
        }
        var nextHome=Reconcile(homeOrder,true);var nextAway=Reconcile(awayOrder,false);
        if(homeOrder.SequenceEqual(nextHome) && awayOrder.SequenceEqual(nextAway))return false;
        homeOrder=nextHome;awayOrder=nextAway;onlineHomeReady=onlineAwayReady=false;return true;
    }
    public bool TryExecuteOnlineOrder(bool home,string action)
    {
        if(!FullOnline || OnlineRoomSession.IsGuest || !matchManager.IsOnlineAuthorityControlled || matchManager.IsPauseMenuOpen || !OnlineOrderSelection)return false;
        var own=home?homeOrder:awayOrder;
        bool ValidOrder(System.Collections.Generic.List<PlayerToken> order,bool side)=>order.Count>0 && order.Distinct().Count()==order.Count
            && order.All(t=>t!=null && t.isHomeTeam==side && GetEligiblePlayingTokens(side).Contains(t));
        if(!ValidOrder(homeOrder,true) || !ValidOrder(awayOrder,false) || homeOrder.Count!=awayOrder.Count)return false;
        if(action=="shootout.ready")
        {
            if(home?onlineHomeReady:onlineAwayReady)return false;
            if(home)onlineHomeReady=true;else onlineAwayReady=true;
            if(onlineHomeReady && onlineAwayReady)
            {
                applyingOnlineOrder=true;
                try{BeginShootoutFromOrderPanel(homeOrder.ToList(),awayOrder.ToList());}
                finally{applyingOnlineOrder=false;}
            }
            return true;
        }
        var parts=action?.Split(':');
        if(parts?.Length!=3 || parts[0]!="shootout.swap" || !int.TryParse(parts[1],out int first) || !int.TryParse(parts[2],out int second)
            || first==second || action!="shootout.swap:"+first+":"+second)return false;
        int a=own.FindIndex(t=>t.jerseyNumber==first),b=own.FindIndex(t=>t.jerseyNumber==second);if(a<0 || b<0)return false;
        (own[a],own[b])=(own[b],own[a]);onlineHomeReady=onlineAwayReady=false;return true;
    }
    public void ApplyOnlineShootout(OnlineShootoutSnapshot state,PlayerToken[] roster)
    {
        if(!OnlineRoomSession.IsGuest)throw new InvalidOperationException("Only a guest projects shootout state");
        if(state==null)return;
        PlayerToken Find(string id)=>roster.FirstOrDefault(t=>OnlineRollPublication.ActorId(t)==id);
        homeOrder=state.homeOrder.Select(Find).ToList();awayOrder=state.awayOrder.Select(Find).ToList();
        onlineHomeReady=state.homeReady;onlineAwayReady=state.awayReady;orderSelectionStarted=state.selecting;orderSelectionCompleted=!state.selecting;
        isActive=state.active;isComplete=state.complete;homeShootoutScore=state.homeScore;awayShootoutScore=state.awayScore;
        homeAttempts=state.homeAttempts;awayAttempts=state.awayAttempts;homePreShootoutGoals=state.homePreGoals;awayPreShootoutGoals=state.awayPreGoals;preShootoutScoreCaptured=true;
        homeResults.Clear();homeResults.AddRange(state.homeResults);awayResults.Clear();awayResults.AddRange(state.awayResults);
        isSuddenDeath=state.suddenDeath;winnerIsHome=state.winnerIsHome;currentShooter=Find(state.shooter);currentDefendingGoalkeeper=Find(state.keeper);
        matchManager.gameData.penaltyShootoutSetup=new PenaltyShootoutSetup{homeTakesFirstPenalty=state.homeFirst,useRightGoal=state.rightGoal};
    }
}
