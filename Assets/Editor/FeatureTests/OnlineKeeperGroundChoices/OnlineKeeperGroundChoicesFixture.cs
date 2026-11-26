using System.Collections;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;
public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunOnlineKeeperGroundChoice(int side,string context,string choice)
    {
        bool attackingHome=!context.StartsWith("away-",System.StringComparison.Ordinal);if(!attackingHome)context=context.Substring(5);
        var authority=OnlineGroundBoard(attackingHome?side:-side,true,false,out var protocol,out _);
        if(!attackingHome)match.ChangePossession();
        var attackTeam=attackingHome?OnlineTeam.Home:OnlineTeam.Away;var defenceTeam=attackingHome?OnlineTeam.Away:OnlineTeam.Home;
        string attackConnection=attackingHome?"home":"away",defenceConnection=attackingHome?"away":"home";
        var carrier=attackingHome?attacker:defender;var defendingKeeper=attackingHome?awayGK:homeGK;var opposingToken=attackingHome?defender:attacker;
        bool wall=context=="wall";Place(carrier,wall?Hex(12*side,-3):Hex(11*side));
        Place(defendingKeeper,wall?Hex(12*side):Hex((context=="moving"?13:12)*side));Place(opposingToken,Hex(2*side,-10));
        ball.PlaceAtCell(carrier.currentHex);match.SetLastToken(carrier);match.UpdatePossessionAfterPass(carrier.currentHex);
        match.BroadcastSafeEndofMovementPhase(countMovementAction:false,triggerFinalThird:false);
        Subscribe(movement);AttachAuthoredPanels();duelPanel.resultSeconds=.1f;
        using var pause=new OnlineSharedPause(protocol,match);pause.Capture(authority);
        void Submit(string logicalActor,string action)
        {
            string actor=logicalActor=="home"?attackConnection:defenceConnection;var boundary=authority.Capture();
            var message=OnlineGroundPassMessage.Command(boundary,protocol.LastAcceptedSequence(logicalActor=="home"?attackTeam:defenceTeam)+1,action);
            var reader=new OnlineGroundPassWire();OnlineGroundPassMessage decoded=null;
            foreach(var packet in OnlineGroundPassWire.Encode(message))decoded=reader.Receive(packet,packet.Length);
            Assert.That(boundary.legalActions,Does.Contain(action));Assert.That(authority.Submit(actor,decoded.ToCommand()).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
        }
        Submit("home","movement.start");
        if(wall)
        {
            Submit("home","movement.player:"+OnlineRollPublication.ActorId(carrier));Submit("home","movement.target:"+(12*side)+":-2");
            yield return Until(()=>movement.isWaitingForGKWallDiveDecision,"actual dribble reaches the goalkeeper wall");
        }
        else
        {
            Submit("home","movement.forfeit");yield return Until(()=>movement.isMovementPhaseDef,"defending team movement turn");
            Submit("away","movement.player:"+OnlineRollPublication.ActorId(defendingKeeper));
            if(context=="moving")
            {
                Submit("away","movement.target:"+(12*side)+":0");
                yield return Until(()=>movement.isWaitingForTackleDecision && !movement.isPlayerMoving,"keeper moves beside an outside-box carrier");
            }
            else yield return Until(()=>movement.isWaitingForTackleDecisionWithoutMoving,"stationary keeper receives existing challenge choices");
        }
        var state=authority.Capture();Assert.That(state.stage,Is.EqualTo(wall?"duel.wall-choice":"duel.choice"));
        Assert.That(state.owner,Is.EqualTo(defenceTeam.ToString()));Assert.That(state.selectedPlayer,Is.EqualTo(OnlineRollPublication.ActorId(defendingKeeper)));
        Assert.That(state.legalActions,Does.Contain("duel.dive"));Assert.That(state.legalActions,Does.Contain("duel.stand"));
        Assert.That(state.legalActions.Contains("duel.tackle"),Is.EqualTo(!wall));
        bool dive=choice.StartsWith("dive",System.StringComparison.Ordinal);
        string action=dive?"duel.dive":choice=="foot"?"duel.tackle":"duel.stand";
        var readerState=new OnlineGroundPassWire();OnlineGroundPassMessage decodedState=null;
        foreach(var packet in OnlineGroundPassWire.Encode(new OnlineGroundPassMessage{kind="state",matchId=state.matchId,state=state}))decodedState=readerState.Receive(packet,packet.Length);
        var replica=new OnlineGroundPassReplica(state.matchId,state.tokens.Select(t=>t.id),state.catalogId);
        Assert.That(replica.Apply(decodedState.state),Is.True);Assert.That(replica.State.owner,Is.EqualTo(defenceTeam.ToString()));Assert.That(replica.State.legalActions,Is.EquivalentTo(state.legalActions));
        var request=OnlineGroundCommand(state,protocol.LastAcceptedSequence(defenceTeam)+1,action);
        var wrong=OnlineGroundCommand(state,protocol.LastAcceptedSequence(attackTeam)+1,action);
        Assert.That(authority.Submit(attackConnection,wrong).Status,Is.EqualTo(OnlineCommandStatus.WrongTeam));Assert.That(match.TimingRollSequence,Is.Zero);
        using(var guest=new OnlineRoomSession("keeper-choice-guest",true))
            Assert.That(movement.TryExecuteOnlineGroundDuel(dive?OnlineGroundDuelIntent.Dive:choice=="foot"?OnlineGroundDuelIntent.Tackle:OnlineGroundDuelIntent.Stand),Is.False);
        if(wall && choice=="stand")
        {
            Assert.That(pause.Submit(attackConnection,new OnlineMatchCommand(protocol.MatchId,1,pause.Revision,"shared-pause",OnlineSharedPause.RequestAction)).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
            pause.SetConnectionHold(true);Assert.That(movement.TryExecuteOnlineGroundDuel(OnlineGroundDuelIntent.Stand),Is.False);
            yield return new WaitForSecondsRealtime(.1f);Assert.That(movement.isWaitingForGKWallDiveDecision,Is.True);Assert.That(match.TimingRollSequence,Is.Zero);
            pause.SetConnectionHold(false);Assert.That(movement.TryExecuteOnlineGroundDuel(OnlineGroundDuelIntent.Stand),Is.False);
            Assert.That(pause.Submit(attackConnection,new OnlineMatchCommand(protocol.MatchId,2,pause.Revision,"shared-pause",OnlineSharedPause.ResumeAction)).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
            request=OnlineGroundCommand(authority.Capture(),protocol.LastAcceptedSequence(defenceTeam)+1,action);
        }
        Submit("away",action);Assert.That(authority.Submit(defenceConnection,request).Status,Is.EqualTo(OnlineCommandStatus.Duplicate));
        if(choice=="stand")
        {
            yield return Until(()=>!movement.isWaitingForGKWallDiveDecision && !movement.isWaitingForTackleDecision && !movement.isWaitingForTackleDecisionWithoutMoving,"declined keeper choice resumes its parent");
            Assert.That(match.TimingRollSequence,Is.Zero);Assert.That(match.LastTokenToTouchTheBallOnPurpose,Is.SameAs(carrier));
            if(!wall)Assert.That(movement.movedTokens,Does.Contain(defendingKeeper));yield break;
        }
        yield return Until(()=>authority.Capture().stage=="duel.roll","keeper choice opens the actual duel");
        Assert.That(authority.Capture().owner,Is.EqualTo(defenceTeam.ToString()));Assert.That(authority.Capture().selectedPlayer,Is.EqualTo(OnlineRollPublication.ActorId(defendingKeeper)));
        RigOnlineLongRoll(2);Submit("away","duel.roll");yield return Until(()=>match.TimingRollSequence==1,"keeper's real dice draw");
        Assert.That(match.gameData.stats.GetPlayerStats(defendingKeeper).groundDuelsInvolved,Is.EqualTo(1));
        Assert.That(defendingKeeper.isSentOff,Is.False,"A choice or first ordinary roll must not invent a handball outcome");
        if(!choice.Contains('-'))yield break;
        string outcome=choice.Split('-')[1];
        yield return Until(()=>authority.Capture().stage=="duel.roll" && authority.Capture().owner==attackTeam.ToString(),"the actual carrier roll follows the goalkeeper");
        RigOnlineLongRoll(outcome=="win"?1:outcome=="tie"?2:6);Submit("home","duel.roll");
        if(!wall && outcome!="lose")
        {
            yield return Until(()=>defendingKeeper.isSentOff && !movement.isWaitingForTackleRoll,"outside-box successful/tied hand dive is handball and straight red");
            Assert.That(ball.GetCurrentHex(),Is.SameAs(Hex(11*side)));Assert.That(match.gameData.stats.homeTeamStats.totalGoals,Is.Zero);
        }
        else if(wall && outcome=="win")
        {
            yield return Until(()=>authority.Capture().stage=="keeper.distribution","wall dive wins, completes recovery and offers distribution");
            Assert.That(match.LastTokenToTouchTheBallOnPurpose,Is.SameAs(defendingKeeper));Assert.That(match.teamInAttack.ToString(),Is.EqualTo(defenceTeam.ToString()));
        }
        else if(wall && outcome=="tie")
        {
            yield return Until(()=>authority.Capture().stage=="loose.direction","tied wall dive continues through real loose ball");
            Assert.That(loose.causingDeflection,Is.SameAs(defendingKeeper));Assert.That(defendingKeeper.isSentOff,Is.False);
        }
        else if(wall)
        {
            yield return Until(()=>!movement.isPlayerMoving && authority.Capture().stage=="dribble.target","lost wall dive releases the suspended dribble parent");
            Assert.That(ball.GetCurrentHex(),Is.SameAs(carrier.currentHex));Assert.That(match.LastTokenToTouchTheBallOnPurpose,Is.SameAs(carrier));
            Submit("home","dribble.finish");Assert.That(movement.movedTokens,Does.Contain(carrier));
        }
        else
        {
            yield return Until(()=>authority.Capture().stage=="duel.reposition","lost outside-box dive offers real carrier reposition");
            Submit("home","duel.stay");Assert.That(match.LastTokenToTouchTheBallOnPurpose,Is.SameAs(carrier));
        }
        Assert.That(match.TimingRollSequence,Is.EqualTo(2));
    }
}
