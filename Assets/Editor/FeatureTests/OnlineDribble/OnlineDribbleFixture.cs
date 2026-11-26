using System.Collections;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunOnlineDribble(int side,string branch)
    {
        var authority=OnlineGroundBoard(side,true,false,out var protocol,out _);
        bool danger=branch!="safe";
        if(danger)Place(defender,Hex(3*side));
        Subscribe(movement);AttachAuthoredPanels();duelPanel.resultSeconds=.1f;
        using var pause=new OnlineSharedPause(protocol,match);pause.Capture(authority);
        void Submit(string connection,string action)=>OnlineAccepted(authority,connection,
            protocol.LastAcceptedSequence(connection=="home"?OnlineTeam.Home:OnlineTeam.Away)+1,action);
        Submit("home","movement.start");Submit("home","movement.player:home:9");
        Submit("home","movement.target:"+side+":0");
        yield return Until(()=>!movement.isPlayerMoving && attacker.currentHex==Hex(side) && movement.isAwaitingHexDestination,"first real dribble step is ready for continuation");
        Assert.That(authority.Capture().stage,Is.EqualTo("dribble.target"));
        Assert.That(movement.remainingDribblerPace,Is.EqualTo(attacker.pace-1));
        using(var guest=new OnlineRoomSession("dribble-guest",true))
            Assert.That(movement.TryExecuteOnlineDribble(OnlineDribbleIntent.Move,target:Hex(2*side)),Is.False);
        Assert.That(movement.TryExecuteOnlineDribble(OnlineDribbleIntent.Move,target:Hex(4*side)),Is.False,"a running player cannot submit a multi-hex continuation");
        Submit("home","movement.target:"+(2*side)+":0");
        yield return Until(()=>!movement.isPlayerMoving && attacker.currentHex==Hex(2*side),"second real dribble step");
        Assert.That(ball.GetCurrentHex(),Is.SameAs(attacker.currentHex));
        if(danger)
        {
            yield return Until(()=>movement.isWaitingForNutmegDecision,"adjacent defender offers source nutmeg decision");
            Assert.That(authority.Capture().stage,Is.EqualTo("dribble.nutmeg"));Submit("home","dribble.decline-nutmeg");
            yield return Until(()=>movement.isWaitingForInterceptionDiceRoll,"declined nutmeg opens real steal");
            var state=authority.Capture();Assert.That(state.stage,Is.EqualTo("dribble.steal"));Assert.That(state.owner,Is.EqualTo("Away"));
            Assert.That(state.selectedPlayer,Is.EqualTo(OnlineRollPublication.ActorId(defender)));
            var late=OnlineGroundCommand(state,protocol.LastAcceptedSequence(OnlineTeam.Away)+1,"dribble.roll");
            Assert.That(authority.Submit("home",OnlineGroundCommand(state,protocol.LastAcceptedSequence(OnlineTeam.Home)+1,"dribble.roll")).Status,Is.EqualTo(OnlineCommandStatus.WrongTeam));
            RigOnlineLongRoll(branch=="hit"?6:2);
            if(branch=="automatic")match.AdvanceOnlineAuthorityTime(DecisionTimingConfig.AutomaticRollSeconds+.1f);
            else Submit("away","dribble.roll");
            Assert.That(authority.Submit("away",late).Status,Is.Not.EqualTo(OnlineCommandStatus.Accepted));
            if(branch=="cancel")
            {
                movement.ResetMovementPhase();yield return new WaitForSecondsRealtime(.3f);
                Assert.That(match.LastTokenToTouchTheBallOnPurpose,Is.SameAs(attacker));
                Assert.That(match.gameData.stats.GetPlayerStats(defender).interceptionsMade,Is.Zero);yield break;
            }
            if(branch=="hold")yield return HoldOnlineHeaderContinuation(pause,protocol,()=>movement.isActivated && match.TimingRollSequence==1 && match.LastTokenToTouchTheBallOnPurpose==attacker);
            if(branch=="hit")
            {
                yield return Until(()=>!movement.isActivated && ball.GetCurrentHex()==defender.currentHex,"successful steal completes real recovery");
                Assert.That(match.teamInAttack,Is.EqualTo(MatchManager.TeamInAttack.Away));
                Assert.That(match.gameData.stats.GetPlayerStats(defender).interceptionsMade,Is.EqualTo(1));
                Assert.That(match.LastTokenToTouchTheBallOnPurpose,Is.SameAs(defender));yield break;
            }
            yield return Until(()=>authority.Capture().stage=="dribble.target","failed steal resumes remaining pace");
            Assert.That(movement.defendersTriedToIntercept,Does.Contain(defender));
        }
        Submit("home","dribble.finish");
        yield return Until(()=>!movement.isDribblerRunning && movement.movedTokens.Contains(attacker),"pace forfeit registers the dribbler once");
        Assert.That(movement.attackersMoved,Is.EqualTo(1));
        Assert.That(match.TimingRollSequence,Is.EqualTo(danger?1:0));
        Assert.That(match.LastTokenToTouchTheBallOnPurpose,Is.SameAs(attacker));
    }
}
