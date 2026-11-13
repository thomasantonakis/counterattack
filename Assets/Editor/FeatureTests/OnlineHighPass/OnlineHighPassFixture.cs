using System.Collections;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunOnlineHighAutomatic(int side)
    {
        var authority=OnlineGroundBoard(side,true,false,out var protocol,out _);
        AttachAuthoredPanels();duelPanel.resultSeconds=.1f;match.isClockRunning=true;attacker.highPass=4;
        using var pause=new OnlineSharedPause(protocol,match);
        void Submit(string connection,string action)=>OnlineAccepted(authority,connection,
            protocol.LastAcceptedSequence(connection=="home"?OnlineTeam.Home:OnlineTeam.Away)+1,action);
        Submit("home",OnlineGroundPassAuthority.HighStartAction);
        Submit("home",OnlineGroundPassAuthority.HighTargetAction(0,6));Submit("home",OnlineGroundPassAuthority.HighTargetAction(0,6));
        yield return Until(()=>authority.Capture().stage=="high.attacker","high-pass preparation before deadline");
        Submit("home",OnlineGroundPassAuthority.HighSkipAction);Submit("away",OnlineGroundPassAuthority.HighSkipAction);
        RigOnlineLongRoll(1);match.AdvanceOnlineAuthorityTime(DecisionTimingConfig.AutomaticRollSeconds+.1f);
        Assert.That(authority.Capture().legalActions,Does.Not.Contain(OnlineGroundPassAuthority.HighRollAction));
        yield return Until(()=>authority.Capture().stage=="high.direction","automatic accuracy reaches direction");
        var late=OnlineGroundCommand(authority.Capture(),protocol.LastAcceptedSequence(OnlineTeam.Home)+1,OnlineGroundPassAuthority.HighRollAction);
        RigOnlineLongRoll(1);match.AdvanceOnlineAuthorityTime(DecisionTimingConfig.AutomaticRollSeconds+.1f);
        Assert.That(authority.Capture().legalActions,Does.Not.Contain(OnlineGroundPassAuthority.HighRollAction));
        Assert.That(authority.Submit("home",late).Status,Is.Not.EqualTo(OnlineCommandStatus.Accepted));
        yield return Until(()=>authority.Capture().stage=="high.distance","automatic direction resolves once");
        Assert.That(match.TimingRollSequence,Is.EqualTo(2));
        Assert.That(match.CaptureOnlineRolls().Select(r=>r.context),Is.EqualTo(new[]{"high_pass_accuracy","high_pass_direction"}));
    }
    public IEnumerator RunOnlineHighInvalidatedTarget()
    {
        var authority=OnlineGroundBoard(1,true,false,out _,out _);
        OnlineAccepted(authority,"home",1,OnlineGroundPassAuthority.HighStartAction);
        string action=OnlineGroundPassAuthority.HighTargetAction(0,8);
        var prior=authority.Capture();Assert.That(prior.legalActions,Does.Contain(action));
        Place(defender,Hex(0,8));
        Assert.That(authority.Submit("home",OnlineGroundCommand(prior,2,action)).Status,Is.EqualTo(OnlineCommandStatus.StaleRevision));
        Assert.That(authority.Capture().legalActions,Does.Not.Contain(action));
        using(var guest=new OnlineRoomSession("high-guest",true))
            Assert.That(high.TryExecuteOnlineHighPass(OnlineHighPassIntent.Target,target:Hex(0,7)),Is.False);
        Assert.That(high.currentTargetHex,Is.Null);Assert.That(match.TimingRollSequence,Is.Zero);yield break;
    }

    public IEnumerator RunOnlineHighKeeper(int side,string response)
    {
        var authority=OnlineGroundBoard(side,true,false,out var protocol,out _);
        Place(awayGK,Hex((response=="stay"?2:4)*side,6));
        AttachAuthoredPanels();duelPanel.resultSeconds=.1f;attacker.highPass=4;
        using var pause=new OnlineSharedPause(protocol,match);
        void Submit(string connection,string action)=>OnlineAccepted(authority,connection,
            protocol.LastAcceptedSequence(connection=="home"?OnlineTeam.Home:OnlineTeam.Away)+1,action);
        Submit("home",OnlineGroundPassAuthority.HighStartAction);
        Submit("home",OnlineGroundPassAuthority.HighTargetAction(0,6));Submit("home",OnlineGroundPassAuthority.HighTargetAction(0,6));
        yield return Until(()=>authority.Capture().stage=="high.attacker","high-pass attacker preparation");
        Submit("home",OnlineGroundPassAuthority.HighSkipAction);Submit("away",OnlineGroundPassAuthority.HighSkipAction);
        RigOnlineLongRoll(5);Submit("home",OnlineGroundPassAuthority.HighRollAction);
        yield return Until(()=>authority.Capture().stage=="high.keeper","defending keeper owns high-pass challenge",120);
        var state=authority.Capture();Assert.That(state.owner,Is.EqualTo("Away"));
        Assert.That(authority.Submit("home",OnlineGroundCommand(state,protocol.LastAcceptedSequence(OnlineTeam.Home)+1,
            OnlineGroundPassAuthority.HighKeeperDeclineAction)).Status,Is.EqualTo(OnlineCommandStatus.WrongTeam));
        HexCell destination=null;
        string action=response=="stay"?OnlineGroundPassAuthority.HighKeeperStayAction:OnlineGroundPassAuthority.HighKeeperDeclineAction;
        if(response=="move")
        {
            destination=high.gkReachableHexes.First(h=>h!=awayGK.currentHex && h.GetOccupyingToken()==null);
            action=OnlineGroundPassAuthority.HighKeeperMoveAction(destination.coordinates.x,destination.coordinates.z);
        }
        var command=OnlineGroundCommand(state,protocol.LastAcceptedSequence(OnlineTeam.Away)+1,action);
        Assert.That(authority.Submit("away",command).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
        Assert.That(authority.Submit("away",command).Status,Is.EqualTo(OnlineCommandStatus.Duplicate));
        if(response=="move")
        {
            Assert.That(high.TryExecuteOnlineHighPass(OnlineHighPassIntent.KeeperDecline),Is.False);
            yield return Until(()=>awayGK.currentHex==destination && !movement.isPlayerMoving,"keeper reaches accepted challenge hex");
        }
        yield return Until(()=>match.shotManager.headerManager.isActivated,"header ownership follows keeper response");
        Assert.That(match.TimingRollSequence,Is.EqualTo(1));
        Assert.That(high.gkRushedOut,Is.EqualTo(response!="decline"));
        if(response!="decline")Assert.That(match.shotManager.headerManager.defenderWillJump,Does.Contain(awayGK));
        Assert.That(match.gameData.stats.GetPlayerStats(attacker).aerialPassesAttempted,Is.EqualTo(1));
    }

    public IEnumerator RunOnlineHighPass(int side,bool scatter)
    {
        var authority=OnlineGroundBoard(side,true,false,out var protocol,out var receiver);
        AttachAuthoredPanels();duelPanel.resultSeconds=.1f;match.isClockRunning=true;attacker.highPass=4;
        using var pause=new OnlineSharedPause(protocol,match);
        void Submit(string connection,string action)=>OnlineAccepted(authority,connection,
            protocol.LastAcceptedSequence(connection=="home"?OnlineTeam.Home:OnlineTeam.Away)+1,action);
        Submit("home",OnlineGroundPassAuthority.HighStartAction);
        Assert.That(authority.Capture().stage,Is.EqualTo("high.target"));
        Assert.That(authority.Capture().legalActions,Does.Not.Contain(OnlineGroundPassAuthority.HighTargetAction(0,0)));
        Submit("home",OnlineGroundPassAuthority.HighTargetAction(0,6));
        Assert.That(ball.GetCurrentHex(),Is.SameAs(attacker.currentHex));
        Submit("home",OnlineGroundPassAuthority.HighTargetAction(0,6));
        yield return Until(()=>authority.Capture().stage=="high.attacker","attacking high-pass preparation");
        Submit("home",OnlineGroundPassAuthority.HighSkipAction);
        Assert.That(authority.Capture().owner,Is.EqualTo("Away"));
        Submit("away",OnlineGroundPassAuthority.HighSkipAction);
        RigOnlineLongRoll(scatter?1:5);
        var roll=OnlineGroundCommand(authority.Capture(),protocol.LastAcceptedSequence(OnlineTeam.Home)+1,OnlineGroundPassAuthority.HighRollAction);
        Assert.That(authority.Submit("home",roll).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
        Assert.That(authority.Submit("home",roll).Status,Is.EqualTo(OnlineCommandStatus.Duplicate));
        if(scatter)
        {
            yield return Until(()=>authority.Capture().stage=="high.direction","real high-pass inaccuracy direction");
            RigOnlineLongRoll(1);Submit("home",OnlineGroundPassAuthority.HighRollAction);
            yield return Until(()=>authority.Capture().stage=="high.distance","real high-pass scatter distance");
            RigOnlineLongRoll(1);Submit("home",OnlineGroundPassAuthority.HighRollAction);
        }
        yield return Until(()=>high.OnlineFlightInProgress,"real high-pass flight starts");
        pause.SetConnectionHold(true);var position=ball.transform.position;
        var request=new OnlineMatchCommand(protocol.MatchId,1,0,"shared-pause",OnlineSharedPause.RequestAction);
        Assert.That(pause.Submit("home",request).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
        yield return new WaitForSecondsRealtime(.2f);
        Assert.That(Vector3.Distance(position,ball.transform.position),Is.LessThan(.001f),"frame-based flight cannot advance through a hold");
        pause.SetConnectionHold(false);yield return new WaitForSecondsRealtime(.1f);
        Assert.That(Vector3.Distance(position,ball.transform.position),Is.LessThan(.001f),"requester pause survives recovery");
        Assert.That(pause.Submit("home",new OnlineMatchCommand(protocol.MatchId,2,1,"shared-pause",OnlineSharedPause.ResumeAction)).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
        yield return Until(()=>authority.Capture().stage=="header.control.choice","high pass offers real landing control",120);
        Assert.That(match.totalCompletedActions,Is.EqualTo(1),"the aerial action resolves before landing control");
        var landing=authority.Capture();var wire=new OnlineGroundPassWire();OnlineGroundPassMessage decoded=null;
        foreach(var packet in OnlineGroundPassWire.Encode(new OnlineGroundPassMessage{kind="state",matchId=landing.matchId,state=landing}))decoded=wire.Receive(packet,packet.Length);
        Assert.That(new OnlineGroundPassReplica(protocol.MatchId).Apply(decoded.state),Is.True);
        Submit("home",OnlineGroundPassAuthority.HeaderControlAction);
        Assert.That(authority.Capture().stage,Is.EqualTo("header.control.roll"));
        var control=OnlineGroundCommand(authority.Capture(),protocol.LastAcceptedSequence(OnlineTeam.Home)+1,OnlineGroundPassAuthority.HeaderControlRollAction);
        Assert.That(authority.Submit("away",OnlineGroundCommand(authority.Capture(),protocol.LastAcceptedSequence(OnlineTeam.Away)+1,OnlineGroundPassAuthority.HeaderControlRollAction)).Status,Is.EqualTo(OnlineCommandStatus.WrongTeam));
        RigOnlineLongRoll(6);
        if(scatter)
        {
            match.AdvanceOnlineAuthorityTime(DecisionTimingConfig.AutomaticRollSeconds+.1f);
            Assert.That(authority.Submit("home",control).Status,Is.Not.EqualTo(OnlineCommandStatus.Accepted));
        }
        else
        {
            Assert.That(authority.Submit("home",control).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
            Assert.That(authority.Submit("home",control).Status,Is.EqualTo(OnlineCommandStatus.Duplicate));
        }
        yield return Until(()=>!match.shotManager.headerManager.isActivated,"actual controlled ball and continuation settle");
        var final=authority.Capture();Assert.That(final.passActive,Is.False);Assert.That(final.lastTouch,Is.EqualTo(OnlineRollPublication.ActorId(receiver)));
        Assert.That(ball.GetCurrentHex(),Is.SameAs(receiver.currentHex));Assert.That(final.rollSequence,Is.EqualTo(scatter?4:2));
        var stats=match.gameData.stats.GetPlayerStats(attacker);
        Assert.That(stats.aerialPassesAttempted,Is.EqualTo(1));Assert.That(stats.aerialPassesTargeted,Is.EqualTo(scatter?0:1));Assert.That(stats.aerialPassesCompleted,Is.EqualTo(1));
        Assert.That(match.totalCompletedActions,Is.EqualTo(2),"existing rules record ball control as its own resolved action");
        var replica=new OnlineGroundPassReplica(protocol.MatchId);Assert.That(replica.Apply(final),Is.True);
        Assert.That(replica.State.tokens.Single(t=>t.id==OnlineRollPublication.ActorId(attacker)).aerialPassesCompleted,Is.EqualTo(1));
    }

    public IEnumerator RunOnlineHighPreparation(int side,bool singleEligible)
    {
        var authority=OnlineGroundBoard(side,true,false,out var protocol,out var receiver);
        if(!singleEligible)Place(Token("Second eligible high-pass attacker",true,false,11),Hex(side,7));
        using var pause=new OnlineSharedPause(protocol,match);
        void Submit(string connection,string action)=>OnlineAccepted(authority,connection,
            protocol.LastAcceptedSequence(connection=="home"?OnlineTeam.Home:OnlineTeam.Away)+1,action);
        Submit("home",OnlineGroundPassAuthority.HighStartAction);
        Submit("home",OnlineGroundPassAuthority.HighTargetAction(0,8));Submit("home",OnlineGroundPassAuthority.HighTargetAction(0,8));
        if(!singleEligible)
        {
            yield return Until(()=>authority.Capture().stage=="high.attacker","mandatory high-pass attacker movement");
            Assert.That(authority.Capture().legalActions,Does.Not.Contain(OnlineGroundPassAuthority.HighSkipAction));
            Submit("home",OnlineGroundPassAuthority.HighPlayerAction(OnlineRollPublication.ActorId(receiver)));
        }
        yield return Until(()=>authority.Capture().stage=="high.defender","mandatory attacker reaches selected target");
        Assert.That(receiver.currentHex,Is.SameAs(Hex(0,8)));
        Submit("away",OnlineGroundPassAuthority.HighPlayerAction(OnlineRollPublication.ActorId(defender)));
        var destination=defender.currentHex.GetNeighbors(grid).First(h=>h!=null && authority.Capture().legalActions.Contains(OnlineGroundPassAuthority.HighMoveAction(h.coordinates.x,h.coordinates.z)));
        Submit("away",OnlineGroundPassAuthority.HighMoveAction(destination.coordinates.x,destination.coordinates.z));
        yield return Until(()=>authority.Capture().stage=="high.accuracy","defender high-pass movement completes");
        Assert.That(defender.currentHex,Is.SameAs(destination));Assert.That(match.TimingRollSequence,Is.Zero);
        Assert.That(match.gameData.stats.GetPlayerStats(attacker).aerialPassesAttempted,Is.Zero);
    }

    public IEnumerator RunOnlineHighCancelledFlight(int side)
    {
        var authority=OnlineGroundBoard(side,true,false,out var protocol,out _);
        AttachAuthoredPanels();duelPanel.resultSeconds=.1f;attacker.highPass=4;
        using var pause=new OnlineSharedPause(protocol,match);
        void Submit(string connection,string action)=>OnlineAccepted(authority,connection,
            protocol.LastAcceptedSequence(connection=="home"?OnlineTeam.Home:OnlineTeam.Away)+1,action);
        Submit("home",OnlineGroundPassAuthority.HighStartAction);
        Submit("home",OnlineGroundPassAuthority.HighTargetAction(0,6));Submit("home",OnlineGroundPassAuthority.HighTargetAction(0,6));
        yield return Until(()=>authority.Capture().stage=="high.attacker","high pass attacker phase");
        Submit("home",OnlineGroundPassAuthority.HighSkipAction);Submit("away",OnlineGroundPassAuthority.HighSkipAction);
        RigOnlineLongRoll(5);Submit("home",OnlineGroundPassAuthority.HighRollAction);
        yield return Until(()=>high.OnlineFlightInProgress,"cancellable high-pass flight");
        high.CleanUpHighPass();var position=ball.transform.position;
        for(int frame=0;frame<100;frame++)yield return null;
        Assert.That(Vector3.Distance(position,ball.transform.position),Is.LessThan(.001f));Assert.That(high.OnlineFlightInProgress,Is.False);
        Assert.That(match.shotManager.headerManager.isActivated,Is.False);Assert.That(match.TimingRollSequence,Is.EqualTo(1));
        Assert.That(match.gameData.stats.GetPlayerStats(attacker).aerialPassesCompleted,Is.Zero);
    }
}
