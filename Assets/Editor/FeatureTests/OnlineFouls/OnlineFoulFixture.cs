using System.Collections;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunOnlineFoul(int side,string branch)
    {
        var authority=OnlineGroundBoard(side,true,false,out var protocol,out _);
        int offset=branch=="penalty"?12:0;
        if(branch=="penalty")
        {
            Place(attacker,Hex(offset*side));ball.PlaceAtCell(attacker.currentHex);
            var penalty=Add<PenaltyKickManager>();match.penaltyKickManager=penalty;penalty.Configure(match,grid,ball,movement,match.shotManager);
        }
        Place(defender,Hex((offset+3)*side));Subscribe(movement);AttachAuthoredPanels();duelPanel.resultSeconds=.1f;
        attacker.resilience=4;match.refereeLeniency=3;
        if(branch=="sent-off")defender.ReceiveYellowCard();
        using var pause=new OnlineSharedPause(protocol,match);pause.Capture(authority);
        void Submit(string connection,string action)=>OnlineAccepted(authority,connection,
            protocol.LastAcceptedSequence(connection=="home"?OnlineTeam.Home:OnlineTeam.Away)+1,action);
        Submit("home","movement.start");Submit("home","movement.player:home:9");
        Submit("home","movement.target:"+((offset+1)*side)+":0");
        yield return Until(()=>authority.Capture().stage=="dribble.target","first dribble pace");
        Submit("home","movement.target:"+((offset+2)*side)+":0");
        yield return Until(()=>movement.isWaitingForNutmegDecision,"nutmeg offer");
        Submit("home","dribble.decline-nutmeg");
        yield return Until(()=>authority.Capture().stage=="dribble.steal","real defender steal");
        RigOnlineLongRoll(1);Submit("away","dribble.roll");
        yield return Until(()=>movement.isWaitingForYellowCardRoll,"real steal foul reaches leniency");
        var state=authority.Capture();Assert.That(state.stage,Is.EqualTo("foul.leniency"));
        Assert.That(state.owner,Is.EqualTo("Away"));Assert.That(state.selectedPlayer,Is.EqualTo("away:8"));
        Assert.That(authority.Submit("home",OnlineGroundCommand(state,protocol.LastAcceptedSequence(OnlineTeam.Home)+1,"foul.leniency")).Status,Is.EqualTo(OnlineCommandStatus.WrongTeam));
        using(var guest=new OnlineRoomSession("foul-guest",true))
            Assert.That(movement.TryExecuteOnlineFoul(OnlineFoulIntent.Leniency),Is.False);
        Assert.That(movement.TryExecuteOnlineFoul(OnlineFoulIntent.Injury),Is.False);
        var late=OnlineGroundCommand(state,protocol.LastAcceptedSequence(OnlineTeam.Away)+1,"foul.leniency");
        void Roll(string connection,string action,int face)
        {
            RigOnlineLongRoll(face);
            if(branch=="automatic")match.AdvanceOnlineAuthorityTime(DecisionTimingConfig.AutomaticRollSeconds+.1f);
            else Submit(connection,action);
        }
        Roll("away","foul.leniency",(branch=="booking" || branch=="sent-off")?5:2);
        Assert.That(authority.Submit("away",late).Status,Is.Not.EqualTo(OnlineCommandStatus.Accepted));
        if(branch=="cancel")
        {
            movement.ResetMovementPhase();yield return new WaitForSecondsRealtime(.3f);
            Assert.That(movement.isWaitingForInjuryRoll,Is.False,"cancelled foul cannot resurrect injury");
            Assert.That(movement.isWaitingForFoulDecision,Is.False);Assert.That(match.TimingRollSequence,Is.EqualTo(2));yield break;
        }
        yield return Until(()=>authority.Capture().stage=="foul.injury","attacker owns injury");
        state=authority.Capture();Assert.That(state.owner,Is.EqualTo("Home"));Assert.That(state.selectedPlayer,Is.EqualTo("home:9"));
        int pace=movement.remainingDribblerPace;
        Roll("home","foul.injury",branch=="injury"?5:2);
        if(branch=="hold")yield return HoldOnlineHeaderContinuation(pause,protocol,()=>!movement.isWaitingForFoulDecision && match.TimingRollSequence==3);
        if(branch=="sent-off")
        {
            Assert.That(defender.isSentOff,Is.True);
            Assert.That(movement.TryExecuteOnlineFoul(OnlineFoulIntent.Advantage),Is.False);
            yield return Until(()=>match.freeKickManager.isActivated,"second yellow forces awarded restart",120);
            Assert.That(authority.Capture().awayStats.totalRedCards,Is.EqualTo(1));yield break;
        }
        yield return Until(()=>authority.Capture().stage=="foul.choice","advantage or awarded restart after result");
        Assert.That(defender.isBooked,Is.EqualTo(branch=="booking"));
        Assert.That(attacker.isInjured,Is.EqualTo(branch=="injury"));
        Assert.That(movement.remainingDribblerPace,Is.EqualTo(pace-(branch=="injury"?1:0)));
        Assert.That(match.CaptureOnlineRolls().Select(r=>r.actorId),Is.EqualTo(new[]{"away:8","away:8","home:9"}));
        Assert.That(match.TimingRollSequence,Is.EqualTo(3));
        Assert.That(authority.Capture().awayStats.totalYellowCards,Is.EqualTo(branch=="booking"?1:0));
        Assert.That(authority.Capture().homeStats.totalInjuries,Is.EqualTo(branch=="injury"?1:0));
        Submit("home",branch=="take" || branch=="penalty"?"foul.take":"foul.advantage");
        if(branch=="penalty")
        {
            yield return Until(()=>match.penaltyKickManager.isActivated,"actual in-box steal foul awards a penalty");
            Assert.That(match.finalThirdManager.isActivated,Is.False);Assert.That(match.penaltyKickManager.penaltySpot,Is.SameAs(Hex(14*side)));yield break;
        }
        if(branch=="take")
        {
            for(int i=0;i<8 && match.freeKickManager.isActivated==false;i++)
            {
                yield return null;var turn=authority.Capture();
                if(turn.legalActions.Contains("final-third.forfeit"))Submit(turn.owner=="Home"?"home":"away","final-third.forfeit");
            }
            yield return Until(()=>match.freeKickManager.isActivated,"actual awarded free kick preparation",120);
            Assert.That(movement.isActivated,Is.False);
        }
        else
        {
            yield return Until(()=>authority.Capture().stage=="dribble.target","advantage resumes actual remaining pace");
            Assert.That(ball.GetCurrentHex(),Is.SameAs(attacker.currentHex));
            Assert.That(match.LastTokenToTouchTheBallOnPurpose,Is.SameAs(attacker));
        }
    }
}
