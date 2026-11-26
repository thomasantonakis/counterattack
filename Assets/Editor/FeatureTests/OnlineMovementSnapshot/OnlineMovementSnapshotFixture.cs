using System.Collections;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;
public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunOnlineMovementSnapshot(int side,string context,string choice)
    {
        bool attackingHome=!context.StartsWith("away-",System.StringComparison.Ordinal);if(!attackingHome)context=context.Substring(5);
        var authority=OnlineGroundBoard(attackingHome?side:-side,true,false,out var protocol,out _);
        if(!attackingHome)match.ChangePossession();
        var carrier=attackingHome?attacker:defender;var opposing=attackingHome?defender:attacker;var defendingKeeper=attackingHome?awayGK:homeGK;
        string attack=attackingHome?"home":"away",defence=attackingHome?"away":"home";
        var attackTeam=attackingHome?OnlineTeam.Home:OnlineTeam.Away;var defenceTeam=attackingHome?OnlineTeam.Away:OnlineTeam.Home;
        bool running=context=="running";carrier.pace=running?3:1;carrier.shooting=3;defendingKeeper.saving=6;defendingKeeper.handling=4;
        Place(carrier,Hex(12*side,-4));Place(opposing,Hex(2*side,-10));Place(defendingKeeper,Hex(16*side));
        ball.PlaceAtCell(carrier.currentHex);match.SetLastToken(carrier);match.UpdatePossessionAfterPass(carrier.currentHex);
        match.BroadcastSafeEndofMovementPhase(countMovementAction:false,triggerFinalThird:false);Invoke(grid,"LoadPrecomputedShootingPaths");
        Subscribe(movement);AttachAuthoredPanels();duelPanel.resultSeconds=.1f;
        using var pause=new OnlineSharedPause(protocol,match);pause.Capture(authority);
        void Submit(string actor,string action)
        {
            var boundary=authority.Capture();var message=OnlineGroundPassMessage.Command(boundary,protocol.LastAcceptedSequence(actor==attack?attackTeam:defenceTeam)+1,action);
            var reader=new OnlineGroundPassWire();OnlineGroundPassMessage decoded=null;
            foreach(var packet in OnlineGroundPassWire.Encode(message))decoded=reader.Receive(packet,packet.Length);
            Assert.That(boundary.legalActions,Does.Contain(action));Assert.That(authority.Submit(actor,decoded.ToCommand()).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
        }
        Submit(attack,"movement.start");Submit(attack,"movement.player:"+OnlineRollPublication.ActorId(carrier));Submit(attack,"movement.target:"+(12*side)+":-3");
        yield return Until(()=>movement.isWaitingForSnapshotDecision && !movement.isPlayerMoving,"real box dribble offers a snapshot");
        Assert.That(movement.isDribblerRunning,Is.EqualTo(running));var state=authority.Capture();
        Assert.That(state.stage,Is.EqualTo(running?"dribble.target":"movement.snapshot"));Assert.That(state.owner,Is.EqualTo(attackTeam.ToString()));
        Assert.That(state.legalActions,Does.Contain("movement.snapshot"));string decline=running?"dribble.finish":"movement.snapshot.decline";
        Assert.That(state.legalActions,Does.Contain(decline));bool shoot=choice is "shoot" or "hold" or "resolve";string action=shoot?"movement.snapshot":decline;
        var request=OnlineGroundCommand(state,protocol.LastAcceptedSequence(attackTeam)+1,action);
        Assert.That(authority.Submit(defence,OnlineGroundCommand(state,protocol.LastAcceptedSequence(defenceTeam)+1,action)).Status,Is.EqualTo(OnlineCommandStatus.WrongTeam));
        using(var guest=new OnlineRoomSession("movement-snapshot-guest",true))
            Assert.That(movement.TryExecuteOnlineMovementSnapshot(OnlineMovementSnapshotIntent.Shoot),Is.False);
        if(choice=="cancel")
        {
            movement.ResetMovementPhase();Assert.That(movement.TryExecuteOnlineMovementSnapshot(OnlineMovementSnapshotIntent.Shoot),Is.False);
            Assert.That(authority.Submit(attack,request).Status,Is.Not.EqualTo(OnlineCommandStatus.Accepted));
            Assert.That(match.gameData.stats.GetPlayerStats(carrier).shotsAttempted,Is.Zero);Assert.That(match.TimingRollSequence,Is.Zero);yield break;
        }
        if(choice=="hold")
        {
            Assert.That(pause.Submit(defence,new OnlineMatchCommand(protocol.MatchId,1,pause.Revision,"shared-pause",OnlineSharedPause.RequestAction)).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
            pause.SetConnectionHold(true);Assert.That(movement.TryExecuteOnlineMovementSnapshot(OnlineMovementSnapshotIntent.Shoot),Is.False);
            yield return new WaitForSecondsRealtime(.1f);Assert.That(movement.isWaitingForSnapshotDecision,Is.True);Assert.That(match.TimingRollSequence,Is.Zero);
            pause.SetConnectionHold(false);Assert.That(movement.TryExecuteOnlineMovementSnapshot(OnlineMovementSnapshotIntent.Shoot),Is.False);
            Assert.That(pause.Submit(defence,new OnlineMatchCommand(protocol.MatchId,2,pause.Revision,"shared-pause",OnlineSharedPause.ResumeAction)).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
            request=OnlineGroundCommand(authority.Capture(),protocol.LastAcceptedSequence(attackTeam)+1,action);
        }
        if(choice=="continue")
        {
            Submit(attack,"movement.target:"+(12*side)+":-2");
            yield return Until(()=>!movement.isPlayerMoving && carrier.currentHex==Hex(12*side,-2),"running player can continue past the offered snapshot");
            Assert.That(authority.Submit(attack,request).Status,Is.Not.EqualTo(OnlineCommandStatus.Accepted));
            Assert.That(authority.Capture().legalActions,Does.Contain("movement.snapshot"));
            request=OnlineGroundCommand(authority.Capture(),protocol.LastAcceptedSequence(attackTeam)+1,action);
        }
        Submit(attack,action);Assert.That(authority.Submit(attack,request).Status,Is.EqualTo(OnlineCommandStatus.Duplicate));
        if(shoot)
        {
            Assert.That(match.shotManager.isActivated,Is.True);Assert.That(match.shotManager.shotType,Is.EqualTo("snapshot"));
            Assert.That(match.shotManager.shooter,Is.SameAs(carrier));Assert.That(authority.Capture().stage,Is.EqualTo("shot.snapshot.select"));
            Assert.That(match.gameData.stats.GetPlayerStats(carrier).shotsAttempted,Is.EqualTo(1));
            Assert.That(movement.attackersMoved,Is.Zero,"Snapshot owns its pending mover until resolution");
        }
        else
        {
            yield return Until(()=>!movement.isWaitingForSnapshotDecision && movement.isAwaitingTokenSelection,"declined snapshot advances its movement parent");
            Assert.That(movement.attackersMoved,Is.EqualTo(1));Assert.That(match.shotManager.isActivated,Is.False);
            Assert.That(match.gameData.stats.GetPlayerStats(carrier).shotsAttempted,Is.Zero);
        }
        Assert.That(movement.movedTokens.Count(token=>token==carrier),Is.EqualTo(1));
        Assert.That(match.TimingRollSequence,Is.Zero);Assert.That(match.LastTokenToTouchTheBallOnPurpose,Is.SameAs(carrier));
        if(choice!="resolve")yield break;
        Submit(defence,"shot.player:"+OnlineRollPublication.ActorId(opposing));Submit(defence,"shot.move:"+(2*side)+":-9");
        yield return Until(()=>authority.Capture().stage=="shot.target","real snapshot blocker movement completes");
        Submit(attack,"shot.target:"+(19*side)+":0");yield return Until(()=>authority.Capture().stage=="shot.roll","snapshot aims through its parent movement phase");
        RigOnlineLongRoll(2);Submit(attack,"shot.roll");yield return Until(()=>authority.Capture().stage=="shot.save","snapshot reaches keeper save");
        RigOnlineLongRoll(2);Submit(defence,"shot.save");yield return Until(()=>authority.Capture().stage=="shot.handling","saved snapshot reaches handling");
        RigOnlineLongRoll(1);Submit(defence,"shot.handling");yield return Until(()=>authority.Capture().stage=="keeper.distribution","held snapshot changes possession and offers distribution");
        Assert.That(match.TimingRollSequence,Is.EqualTo(3));Assert.That(match.LastTokenToTouchTheBallOnPurpose,Is.SameAs(defendingKeeper));
        Assert.That(match.gameData.stats.GetPlayerStats(carrier).shotsAttempted,Is.EqualTo(1));Assert.That(match.gameData.stats.GetPlayerStats(defendingKeeper).attemptsSaved,Is.EqualTo(1));
        Assert.That(match.teamInAttack.ToString(),Is.EqualTo(defenceTeam.ToString()));Assert.That(movement.isActivated,Is.False);
    }
}
