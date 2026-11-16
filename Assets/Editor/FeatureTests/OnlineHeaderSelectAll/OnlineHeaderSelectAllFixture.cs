using System;
using System.Collections;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;
using Random=UnityEngine.Random;
public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunOnlineHeaderAll(int side,bool home,string branch)
    {
        var authority=OnlineGroundBoard(home?side:-side,true,false,out var protocol,out var receiver);var header=match.highPassManager.headerManager;
        var passer=home?attacker:Token("Away aerial passer",false,false,9);var firstAttack=home?receiver:defender;var firstDefence=home?defender:attacker;
        var secondAttack=Token("Second attacking jumper",home,false,7);var secondDefence=Token("Second defensive jumper",!home,false,7);
        var crowded=branch.StartsWith("crowded",StringComparison.Ordinal)?Token("Extra nomination candidate",branch=="crowded-attack"?home:!home,false,11):null;
        if(!home)match.ChangePossession();
        if(!home)Place(receiver,Hex(0,10));Place(firstDefence,Hex(side,6));Place(passer,Hex(0));Place(firstAttack,Hex(0,6));Place(secondAttack,Hex(-side,6));Place(secondDefence,Hex(0,7));
        if(crowded!=null)Place(crowded,Hex(-side,7));if(branch=="keeper")Place(home?awayGK:homeGK,Hex(0,5));
        ball.PlaceAtCell(passer.currentHex);match.SetLastToken(passer);match.UpdatePossessionAfterPass(passer.currentHex);match.BroadcastSafeEndofMovementPhase(countMovementAction:false,triggerFinalThird:false);
        firstAttack.heading=6;secondAttack.heading=3;firstDefence.heading=secondDefence.heading=2;passer.highPass=4;
        AttachAuthoredPanels();duelPanel.resultSeconds=.05f;header.headerChallengePanel.resultSeconds=.05f;using var pause=new OnlineSharedPause(protocol,match);
        string attack=home?"home":"away",defence=home?"away":"home";
        OnlineTeam Team(string c)=>c=="home"?OnlineTeam.Home:OnlineTeam.Away;
        OnlineMatchCommand Command(OnlineGroundPassState state,string connection,string action)=>OnlineGroundCommand(state,protocol.LastAcceptedSequence(Team(connection))+1,action);
        void Submit(string connection,string action)
        {
            var state=pause.Capture(authority);var wire=new OnlineGroundPassWire();OnlineGroundPassMessage decoded=null;foreach(var packet in OnlineGroundPassWire.Encode(OnlineGroundPassMessage.Command(state,protocol.LastAcceptedSequence(Team(connection))+1,action)))decoded=wire.Receive(packet,packet.Length);
            Assert.That(state.legalActions,Does.Contain(action));var request=decoded.ToCommand();Assert.That(authority.Submit(connection,request).Status,Is.EqualTo(OnlineCommandStatus.Accepted));Assert.That(authority.Submit(connection,request).Status,Is.EqualTo(OnlineCommandStatus.Duplicate));
        }
        void Bulk(string connection)
        {
            var state=pause.Capture(authority);Assert.That(state.legalActions,Does.Contain("header.all"));Assert.That(OnlineBoardActionPicker.Key(state,KeyCode.A),Is.EqualTo("header.all"));
            string other=connection=="home"?"away":"home";var rng=Random.state;Assert.That(authority.Submit(other,Command(state,other,"header.all")).Status,Is.EqualTo(OnlineCommandStatus.WrongTeam));
            var intent=(OnlineHeaderIntent)Enum.Parse(typeof(OnlineHeaderIntent),"SelectAll");using(var guest=new OnlineRoomSession("header-all-guest",true))Assert.That(header.TryExecuteOnlineHeader(intent),Is.False);
            pause.SetConnectionHold(true);Assert.That(pause.Capture(authority).legalActions,Is.Empty);Assert.That(header.TryExecuteOnlineHeader(intent),Is.False);pause.SetConnectionHold(false);Assert.That(Random.state,Is.EqualTo(rng));
            Submit(connection,"header.all");
        }
        Submit(attack,"high.start");Submit(attack,"high.target:0:6");Submit(attack,"high.target:0:6");yield return Until(()=>authority.Capture().stage=="high.attacker","actual aerial preparation");
        Submit(attack,"high.skip");Submit(defence,"high.skip");RigOnlineLongRoll(5);Submit(attack,"high.roll");
        yield return Until(()=>authority.Capture().stage=="header.attackers" || authority.Capture().legalActions.Contains("high.keeper.decline"),"aerial landing or legal keeper response",180);
        if(authority.Capture().legalActions.Contains("high.keeper.decline"))Submit(defence,"high.keeper.decline");
        yield return Until(()=>authority.Capture().stage=="header.attackers","real attacking nomination stage",180);
        if(branch=="crowded-attack")
        {
            var state=pause.Capture(authority);Assert.That(header.attEligibleToHead.Count,Is.EqualTo(3));Assert.That(state.legalActions,Does.Not.Contain("header.all"));Assert.That(OnlineBoardActionPicker.Key(state,KeyCode.A),Is.Null);Assert.That(authority.Submit(attack,Command(state,attack,"header.all")).Status,Is.EqualTo(OnlineCommandStatus.IllegalAction));
            Submit(attack,"header.nominate:"+OnlineRollPublication.ActorId(firstAttack));Submit(attack,"header.nominate:"+OnlineRollPublication.ActorId(secondAttack));Submit(attack,"header.confirm");
        }
        else Bulk(attack);
        yield return Until(()=>authority.Capture().stage=="header.defenders","real defensive nomination stage");Assert.That(header.attackerWillJump,Is.EquivalentTo(new[]{firstAttack,secondAttack}));
        if(branch=="crowded-defence")
        {
            var state=pause.Capture(authority);Assert.That(header.defEligibleToHead.Count(t=>!t.IsGoalKeeper),Is.EqualTo(3));Assert.That(state.legalActions,Does.Not.Contain("header.all"));Assert.That(OnlineBoardActionPicker.Key(state,KeyCode.A),Is.Null);Assert.That(authority.Submit(defence,Command(state,defence,"header.all")).Status,Is.EqualTo(OnlineCommandStatus.IllegalAction));
            Submit(defence,"header.nominate:"+OnlineRollPublication.ActorId(firstDefence));Submit(defence,"header.nominate:"+OnlineRollPublication.ActorId(secondDefence));Submit(defence,"header.confirm");
        }
        else Bulk(defence);
        yield return Until(()=>authority.Capture().stage=="header.roll","all nominations continue to actual rolls");
        int defenders=branch=="keeper"?3:2;Assert.That(header.defenderWillJump.Count,Is.EqualTo(defenders));Assert.That(header.defenderWillJump.Count(t=>!t.IsGoalKeeper),Is.EqualTo(2));
        var nominated=pause.Capture(authority);var stateWire=new OnlineGroundPassWire();OnlineGroundPassMessage encoded=null;foreach(var packet in OnlineGroundPassWire.Encode(new OnlineGroundPassMessage{kind="state",matchId=nominated.matchId,state=nominated}))encoded=stateWire.Receive(packet,packet.Length);var replica=new OnlineGroundPassReplica(nominated.matchId);Assert.That(replica.Apply(encoded.state),Is.True);Assert.That(replica.State.header.attackers.Length,Is.EqualTo(2));Assert.That(replica.State.header.defenders.Length,Is.EqualTo(defenders));
        for(int i=0;i<2+defenders;i++)
        {
            yield return Until(()=>authority.Capture().stage=="header.roll","next owned nominated roll");var roller=header.tokenRolling;RigOnlineLongRoll(roller==firstAttack?6:roller.isAttacker?4:1);Submit(roller.isHomeTeam?"home":"away","header.roll");
        }
        yield return Until(()=>authority.Capture().stage=="header.target","real nomination result reaches headed-pass continuation",180);
        var after=pause.Capture(authority);Assert.That(after.header.scores.Length,Is.EqualTo(2+defenders));Assert.That(match.TimingRollSequence,Is.EqualTo(3+defenders));Assert.That(match.LastTokenToTouchTheBallOnPurpose,Is.SameAs(firstAttack));Assert.That(after.owner,Is.EqualTo(Team(attack).ToString()));Assert.That(match.gameData.stats.GetPlayerStats(passer).aerialPassesCompleted,Is.EqualTo(1));
    }
}
