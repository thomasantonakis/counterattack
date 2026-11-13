using System.Collections;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunOnlineHeaderNominations(int side,bool unchallenged)
    {
        var authority=OnlineGroundBoard(side,true,false,out var protocol,out var receiver);
        var header=match.highPassManager.headerManager;
        PlayerToken second=null;
        if(!unchallenged){Place(defender,Hex(side,6));second=Token("Second jumper",true,false,8);Place(second,Hex(-side,6));}
        AttachAuthoredPanels();duelPanel.resultSeconds=.05f;header.headerChallengePanel.resultSeconds=.05f;attacker.highPass=4;
        receiver.heading=4;if(second!=null)second.heading=4;
        using var pause=new OnlineSharedPause(protocol,match);match.isClockRunning=true;
        void Submit(string connection,string action)=>OnlineAccepted(authority,connection,
            protocol.LastAcceptedSequence(connection=="home"?OnlineTeam.Home:OnlineTeam.Away)+1,action);
        Submit("home",OnlineGroundPassAuthority.HighStartAction);
        Submit("home",OnlineGroundPassAuthority.HighTargetAction(0,6));Submit("home",OnlineGroundPassAuthority.HighTargetAction(0,6));
        yield return Until(()=>authority.Capture().stage=="high.attacker","nomination preparation");
        Submit("home",OnlineGroundPassAuthority.HighSkipAction);Submit("away",OnlineGroundPassAuthority.HighSkipAction);
        RigOnlineLongRoll(5);Submit("home",OnlineGroundPassAuthority.HighRollAction);
        if(unchallenged)
        {
            yield return Until(()=>authority.Capture().stage=="header.control.choice","free header choice",120);
            Submit("home",OnlineGroundPassAuthority.HeaderFreeAction);
            yield return Until(()=>authority.Capture().stage=="header.target","free header targets");
            Assert.That(match.LastTokenToTouchTheBallOnPurpose,Is.SameAs(receiver));
            Assert.That(match.TimingRollSequence,Is.EqualTo(1),"unchallenged headed pass needs no challenge roll");
            Assert.That(match.gameData.stats.GetPlayerStats(attacker).aerialPassesCompleted,Is.EqualTo(1));
            yield break;
        }
        yield return Until(()=>authority.Capture().stage=="header.attackers","multiple attacking nominees",120);
        Assert.That(authority.Capture().legalActions,Does.Not.Contain(OnlineGroundPassAuthority.HeaderConfirmAction));
        Submit("home",OnlineGroundPassAuthority.HeaderPlayerAction(OnlineRollPublication.ActorId(receiver)));
        Submit("home",OnlineGroundPassAuthority.HeaderPlayerAction(OnlineRollPublication.ActorId(second)));
        Assert.That(authority.Capture().header.attackers.Length,Is.EqualTo(2));
        Submit("home",OnlineGroundPassAuthority.HeaderConfirmAction);
        yield return Until(()=>authority.Capture().stage=="header.defenders","defensive nominations after confirmation");
        Submit("away",OnlineGroundPassAuthority.HeaderPlayerAction(OnlineRollPublication.ActorId(defender)));
        Submit("away",OnlineGroundPassAuthority.HeaderConfirmAction);
        yield return Until(()=>authority.Capture().stage=="header.roll","first of successive same-team rolls");
        var first=header.tokenRolling;var old=OnlineGroundCommand(authority.Capture(),protocol.LastAcceptedSequence(OnlineTeam.Home)+1,OnlineGroundPassAuthority.HeaderRollAction);
        RigOnlineLongRoll(5);match.AdvanceOnlineAuthorityTime(DecisionTimingConfig.AutomaticRollSeconds+.1f);
        yield return Until(()=>header.isWaitingForHeaderRoll && header.tokenRolling!=first,"next same-team jumper");
        Assert.That(authority.Capture().owner,Is.EqualTo("Home"));
        Assert.That(authority.Submit("home",old).Status,Is.EqualTo(OnlineCommandStatus.StaleRevision));
        RigOnlineLongRoll(6);Submit("home",OnlineGroundPassAuthority.HeaderRollAction);
        yield return Until(()=>header.isWaitingForHeaderRoll && header.tokenRolling==defender,"defending jumper");
        RigOnlineLongRoll(1);Submit("away",OnlineGroundPassAuthority.HeaderRollAction);
        yield return Until(()=>authority.Capture().stage=="header.target","multiple-nominee result");
        Assert.That(authority.Capture().header.scores.Length,Is.EqualTo(3));
        Assert.That(match.TimingRollSequence,Is.EqualTo(4));
    }

    public IEnumerator RunOnlineHeaderCompletion(int side,bool defenseWins,bool automatic)
    {
        var authority=OnlineGroundBoard(side,true,false,out var protocol,out var receiver);
        Place(defender,Hex(side,6));
        var mate=Token("Header pass receiver",!defenseWins,false,7);Place(mate,Hex(0,10));
        AttachAuthoredPanels();duelPanel.resultSeconds=.05f;attacker.highPass=4;
        var header=match.highPassManager.headerManager;header.headerChallengePanel.resultSeconds=.1f;
        receiver.heading=defender.heading=4;match.isClockRunning=true;
        using var pause=new OnlineSharedPause(protocol,match);
        void Submit(string connection,string action)=>OnlineAccepted(authority,connection,
            protocol.LastAcceptedSequence(connection=="home"?OnlineTeam.Home:OnlineTeam.Away)+1,action);
        Submit("home",OnlineGroundPassAuthority.HighStartAction);
        Submit("home",OnlineGroundPassAuthority.HighTargetAction(0,6));Submit("home",OnlineGroundPassAuthority.HighTargetAction(0,6));
        yield return Until(()=>authority.Capture().stage=="high.attacker","header preparation");
        Submit("home",OnlineGroundPassAuthority.HighSkipAction);Submit("away",OnlineGroundPassAuthority.HighSkipAction);
        RigOnlineLongRoll(5);Submit("home",OnlineGroundPassAuthority.HighRollAction);
        yield return Until(()=>authority.Capture().stage=="header.defenders","header defensive nomination",120);
        var before=authority.Capture();
        string nomination=OnlineGroundPassAuthority.HeaderPlayerAction(OnlineRollPublication.ActorId(defender));
        Assert.That(authority.Submit("home",OnlineGroundCommand(before,protocol.LastAcceptedSequence(OnlineTeam.Home)+1,nomination)).Status,Is.EqualTo(OnlineCommandStatus.WrongTeam));
        var command=OnlineGroundCommand(before,protocol.LastAcceptedSequence(OnlineTeam.Away)+1,nomination);
        Assert.That(authority.Submit("away",command).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
        Assert.That(authority.Submit("away",command).Status,Is.EqualTo(OnlineCommandStatus.Duplicate));
        Assert.That(authority.Capture().decisionId,Is.Not.EqualTo(before.decisionId),"nomination invalidates earlier views");
        Submit("away",OnlineGroundPassAuthority.HeaderPlayerAction(OnlineRollPublication.ActorId(defender),true));
        Assert.That(header.defenderWillJump.Contains(defender),Is.False);
        Submit("away",nomination);Submit("away",OnlineGroundPassAuthority.HeaderConfirmAction);
        yield return Until(()=>authority.Capture().stage=="header.roll" && header.tokenRolling==receiver,"attacking header roll");
        var stale=OnlineGroundCommand(authority.Capture(),protocol.LastAcceptedSequence(OnlineTeam.Home)+1,OnlineGroundPassAuthority.HeaderRollAction);
        RigOnlineLongRoll(defenseWins?1:6);
        if(automatic)match.AdvanceOnlineAuthorityTime(DecisionTimingConfig.AutomaticRollSeconds+.1f);
        else Submit("home",OnlineGroundPassAuthority.HeaderRollAction);
        Assert.That(header.isWaitingForHeaderRoll,Is.False,"roll claims this row synchronously");
        Assert.That(authority.Submit("home",stale).Status,Is.Not.EqualTo(OnlineCommandStatus.Accepted));
        yield return Until(()=>authority.Capture().stage=="header.roll" && header.tokenRolling==defender,"defending header roll");
        RigOnlineLongRoll(defenseWins?6:1);Submit("away",OnlineGroundPassAuthority.HeaderRollAction);
        yield return Until(()=>authority.Capture().stage=="header.target","winning header target");
        var published=authority.Capture();
        Assert.That(published.header.scores.Length,Is.EqualTo(2));
        var replica=new OnlineGroundPassReplica(protocol.MatchId);Assert.That(replica.Apply(published),Is.True);
        Assert.That(replica.State.header.scores.Select(s=>s.roll),Is.EquivalentTo(new[]{1,6}));
        var winner=defenseWins?defender:receiver;string connection=defenseWins?"away":"home";
        Assert.That(match.LastTokenToTouchTheBallOnPurpose,Is.SameAs(winner));
        Assert.That(authority.Capture().owner,Is.EqualTo(defenseWins?"Away":"Home"));
        string target=OnlineGroundPassAuthority.HeaderTargetAction(0,10);
        Assert.That(authority.Capture().legalActions,Does.Contain(target));
        using(var guest=new OnlineRoomSession("header-guest",true))
            Assert.That(header.TryExecuteOnlineHeader(OnlineHeaderIntent.PassTarget,target:mate.currentHex),Is.False);
        Submit(connection,target);
        yield return Until(()=>!header.isActivated && ball.GetCurrentHex()==mate.currentHex,"headed pass collection and continuation",180);
        Assert.That(match.LastTokenToTouchTheBallOnPurpose,Is.SameAs(mate));
        Assert.That(match.TimingRollSequence,Is.EqualTo(3));
        Assert.That(match.CaptureOnlineRolls().Select(r=>r.context),Is.EqualTo(new[]{"high_pass_accuracy","header_challenge","header_challenge"}));
        Assert.That(match.gameData.stats.GetPlayerStats(winner).passesCompleted,Is.EqualTo(1));
    }

    public IEnumerator RunOnlineHeaderResultHold(int side,bool enabledOwner,bool cancel)
    {
        var authority=OnlineGroundBoard(side,true,false,out var protocol,out var receiver);
        Place(defender,Hex(side,6));AttachAuthoredPanels();duelPanel.resultSeconds=.1f;attacker.highPass=4;
        var header=match.highPassManager.headerManager;var panel=header.headerChallengePanel;panel.resultSeconds=.2f;
        if(enabledOwner)EnableForAuthoredPresentation(header);
        receiver.heading=defender.heading=4;
        using var pause=new OnlineSharedPause(protocol,match);
        void Submit(string connection,string action)=>OnlineAccepted(authority,connection,
            protocol.LastAcceptedSequence(connection=="home"?OnlineTeam.Home:OnlineTeam.Away)+1,action);
        Submit("home",OnlineGroundPassAuthority.HighStartAction);
        Submit("home",OnlineGroundPassAuthority.HighTargetAction(0,6));Submit("home",OnlineGroundPassAuthority.HighTargetAction(0,6));
        yield return Until(()=>authority.Capture().stage=="high.attacker","high-pass preparation before contested header");
        Submit("home",OnlineGroundPassAuthority.HighSkipAction);Submit("away",OnlineGroundPassAuthority.HighSkipAction);
        RigOnlineLongRoll(5);Submit("home",OnlineGroundPassAuthority.HighRollAction);
        yield return Until(()=>header.isWaitingForDefenderSelection,"real contested header nominations",120);
        Assert.That(authority.Capture().stage,Is.EqualTo("header.defenders"));
        Submit("away",OnlineGroundPassAuthority.HeaderPlayerAction(OnlineRollPublication.ActorId(defender)));
        Submit("away",OnlineGroundPassAuthority.HeaderConfirmAction);
        yield return Until(()=>header.isWaitingForHeaderRoll && header.tokenRolling==receiver,"attacker's real header roll");
        RigOnlineLongRoll(6);Submit("home",OnlineGroundPassAuthority.HeaderRollAction);
        yield return Until(()=>header.isWaitingForHeaderRoll && header.tokenRolling==defender,"defender's real header roll");
        RigOnlineLongRoll(1);Submit("away",OnlineGroundPassAuthority.HeaderRollAction);
        Assert.That(pause.Submit("home",new OnlineMatchCommand(protocol.MatchId,1,0,"shared-pause",OnlineSharedPause.RequestAction)).Status,
            Is.EqualTo(OnlineCommandStatus.Accepted));
        pause.SetConnectionHold(true);
        yield return new WaitForSecondsRealtime(.4f);
        Assert.That(panel.IsOpen,Is.True,"header result cannot close or reject its controlled owner during authority hold");
        Assert.That(header.isWaitingForHeaderTargetSelection,Is.False);
        Assert.That(match.gameData.stats.GetPlayerStats(attacker).aerialPassesCompleted,Is.Zero);
        panel.Close();Assert.That(panel.IsOpen,Is.True,"local close cannot advance a held result");
        var held=panel.IsResultHeld;
        HeaderChallengePanelController.ConsumeKey(new KeyPressData(KeyCode.Return,false,false,false));
        Assert.That(panel.IsResultHeld,Is.EqualTo(held));
        if(cancel)
        {
            header.CleanUpHeader();
            Assert.That(panel.IsOpen,Is.False,"manager cancellation can release a held presentation");
        }
        pause.SetConnectionHold(false);yield return new WaitForSecondsRealtime(.3f);
        Assert.That(panel.IsOpen,Is.EqualTo(!cancel),"shared pause survives connection recovery");
        Assert.That(pause.Submit("home",new OnlineMatchCommand(protocol.MatchId,2,1,"shared-pause",OnlineSharedPause.ResumeAction)).Status,
            Is.EqualTo(OnlineCommandStatus.Accepted));
        if(cancel)
        {
            yield return new WaitForSecondsRealtime(.3f);
            Assert.That(header.isWaitingForHeaderTargetSelection,Is.False);
            Assert.That(match.gameData.stats.GetPlayerStats(attacker).aerialPassesCompleted,Is.Zero);
            Assert.That(match.TimingRollSequence,Is.EqualTo(3));yield break;
        }
        yield return Until(()=>header.isWaitingForHeaderTargetSelection,"header result releases its winner continuation once");
        Assert.That(match.LastTokenToTouchTheBallOnPurpose,Is.SameAs(receiver));
        Assert.That(match.TimingRollSequence,Is.EqualTo(3));
        Assert.That(match.gameData.stats.GetPlayerStats(attacker).aerialPassesCompleted,Is.EqualTo(1));
    }
}
