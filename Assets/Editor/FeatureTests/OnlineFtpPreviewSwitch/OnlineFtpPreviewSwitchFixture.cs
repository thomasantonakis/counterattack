using System.Collections;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;
public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunOnlineFtpPreviewSwitch(int side,bool home,string branch)
    {
        var authority=OnlineGroundBoard(home?side:-side,true,false,out var protocol,out var originalReceiver);
        var receiver=home?originalReceiver:match.playerTokenManager.allTokens.Single(t=>!t.isHomeTeam && t.jerseyNumber==6);
        if(!home){Place(originalReceiver,Hex(8*side,-8));match.ChangePossession();}
        var carrier=home?attacker:defender;var opponent=home?defender:attacker;
        Place(opponent,Hex(8*side,10));Place(carrier,Hex(0));Place(receiver,Hex(0,6));
        int assistance=branch=="experienced"?3:2;
        match.difficulty_level=2;match.gameData.gameSettings.playerAssistance=2;
        ball.PlaceAtCell(carrier.currentHex);match.SetLastToken(carrier);match.UpdatePossessionAfterPass(carrier.currentHex);
        Subscribe(ftp);Subscribe(movement);AttachAuthoredPanels();
        match.BroadcastSafeEndofMovementPhase(countMovementAction:false,triggerFinalThird:false);
        string attack=home?"home":"away",defence=home?"away":"home";var team=home?OnlineTeam.Home:OnlineTeam.Away;
        using var pause=new OnlineSharedPause(protocol,match);
        void Submit(string action)=>OnlineAccepted(authority,attack,protocol.LastAcceptedSequence(team)+1,action);
        Submit("ground.start");Submit("ground.target:0:6");Submit("ground.target:0:6");
        yield return Until(()=>!ground.isActivated && match.LastTokenToTouchTheBallOnPurpose==receiver,"real preceding pass offers FTP");
        Assert.That(ftp.isAvailable,Is.True);
        match.difficulty_level=assistance;match.gameData.gameSettings.playerAssistance=assistance;Submit("ftp.start");
        if(branch!="unselected" && branch!="experienced")Submit("ftp.target:0:0");
        var state=pause.Capture(authority);
        if(branch=="experienced")
        {
            Assert.That(state.stage,Is.EqualTo("ftp.target"));Assert.That(state.legalActions,Does.Not.Contain("movement.start"));
            Assert.That(OnlineBoardActionPicker.Key(state,KeyCode.M),Is.Null);Assert.That(movement.isAvailable,Is.False);yield break;
        }
        if(branch=="committed")
        {
            Submit("ftp.target:0:0");state=pause.Capture(authority);
            Assert.That(state.stage,Is.EqualTo("ftp.attacker"));Assert.That(state.legalActions,Does.Not.Contain("movement.start"));
            Assert.That(OnlineBoardActionPicker.Key(state,KeyCode.M),Is.Null);Assert.That(ftp.isActivated,Is.True);yield break;
        }
        Assert.That(state.stage,Is.EqualTo("ftp.target"));Assert.That(ftp.isAwaitingTargetSelection,Is.True);
        Assert.That(state.legalActions,Does.Contain("movement.start"),"Uncommitted FTP must offer the advertised movement switch");
        Assert.That(OnlineBoardActionPicker.Key(state,KeyCode.M),Is.EqualTo("movement.start"));
        var oldConfirm=OnlineGroundCommand(state,protocol.LastAcceptedSequence(team)+2,"ftp.target:0:0");
        if(branch=="hold")
        {
            pause.SetConnectionHold(true);var held=pause.Capture(authority);
            Assert.That(held.legalActions,Is.Empty);Assert.That(OnlineBoardActionPicker.Key(held,KeyCode.M),Is.Null);
            Assert.That(authority.Submit(attack,OnlineGroundCommand(state,protocol.LastAcceptedSequence(team)+1,"movement.start")).Status,Is.Not.EqualTo(OnlineCommandStatus.Accepted));
            Assert.That(ftp.isActivated,Is.True);pause.SetConnectionHold(false);state=pause.Capture(authority);
            Assert.That(OnlineBoardActionPicker.Key(state,KeyCode.M),Is.EqualTo("movement.start"));
        }
        long sequence=protocol.LastAcceptedSequence(team)+1;
        Assert.That(authority.Submit(defence,OnlineGroundCommand(state,1,"movement.start")).Status,Is.EqualTo(OnlineCommandStatus.WrongTeam));
        var command=OnlineGroundPassMessage.Command(state,sequence,OnlineBoardActionPicker.Key(state,KeyCode.M));
        var wire=new OnlineGroundPassWire();OnlineGroundPassMessage decoded=null;
        foreach(var packet in OnlineGroundPassWire.Encode(command))decoded=wire.Receive(packet,packet.Length);
        Assert.That(authority.Submit(attack,decoded.ToCommand()).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
        Assert.That(ftp.isActivated,Is.False);Assert.That(ftp.currentTargetHex,Is.Null);Assert.That(movement.isActivated,Is.True);Assert.That(movement.isCommitted,Is.False);
        Assert.That(ball.GetCurrentHex(),Is.SameAs(receiver.currentHex));Assert.That(match.LastTokenToTouchTheBallOnPurpose,Is.SameAs(receiver));
        Assert.That(match.gameData.stats.GetPlayerStats(carrier).passesAttempted,Is.EqualTo(1));Assert.That(match.TimingRollSequence,Is.Zero);
        Assert.That(authority.Submit(attack,oldConfirm).Status,Is.EqualTo(OnlineCommandStatus.StaleRevision),"Old FTP preview cannot complete after switching");
    }
}
