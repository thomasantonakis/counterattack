using System;
using System.Collections;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;

public sealed partial class GoalkeeperBoxEntryFixture
{
    private void RigOnlineLongRoll(int face)
    {
        int seed=0;
        for(;seed<1000;seed++) { UnityEngine.Random.InitState(seed);if(helper.DiceRoll()._roll==face)break; }
        Assert.That(seed,Is.LessThan(1000));UnityEngine.Random.InitState(seed);
    }
    public IEnumerator RunOnlineLongPass(int side,bool accurate)
    {
        var authority=OnlineGroundBoard(side,true,false,out var protocol,out _);
        AttachAuthoredPanels();duelPanel.resultSeconds=.1f;
        using var pause=new OnlineSharedPause(protocol,match);
        attacker.highPass=4;
        var target=Hex(8*side,-6);var targetAction=OnlineGroundPassAuthority.LongTargetAction(8*side,-6);
        OnlineAccepted(authority,"home",1,OnlineGroundPassAuthority.LongStartAction);
        Assert.That(authority.Capture().stage,Is.EqualTo("long.target"));
        Assert.That(authority.Capture().legalActions,Does.Not.Contain(OnlineGroundPassAuthority.LongTargetAction(0,0)));
        var randomBefore=UnityEngine.Random.state;
        var preview=authority.Capture();
        var wire=new OnlineGroundPassWire();OnlineGroundPassMessage decoded=null;
        foreach(var packet in OnlineGroundPassWire.Encode(new OnlineGroundPassMessage{kind="state",matchId=preview.matchId,state=preview}))
        {Assert.That(packet.Length,Is.LessThanOrEqualTo(926));decoded=wire.Receive(packet,packet.Length);}
        Assert.That(decoded.state.legalActions,Is.EqualTo(preview.legalActions));
        Assert.That(new OnlineGroundPassReplica(protocol.MatchId).Apply(decoded.state),Is.True);
        Assert.That(authority.Submit("away",OnlineGroundCommand(preview,1,targetAction)).Status,Is.EqualTo(OnlineCommandStatus.WrongTeam));
        Assert.That(UnityEngine.Random.state,Is.EqualTo(randomBefore));
        OnlineAccepted(authority,"home",2,targetAction);
        Assert.That(ball.GetCurrentHex(),Is.SameAs(attacker.currentHex));
        Assert.That(match.gameData.stats.GetPlayerStats(attacker).aerialPassesAttempted,Is.Zero);
        OnlineAccepted(authority,"home",3,targetAction);
        Assert.That(longBall.currentTargetHex,Is.SameAs(target));
        Assert.That(match.gameData.stats.GetPlayerStats(attacker).aerialPassesAttempted,Is.EqualTo(1));
        RigOnlineLongRoll(accurate?5:1);
        var roll=OnlineGroundCommand(authority.Capture(),4,OnlineGroundPassAuthority.LongRollAction);
        Assert.That(authority.Submit("home",roll).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
        Assert.That(authority.Submit("home",roll).Status,Is.EqualTo(OnlineCommandStatus.Duplicate));
        yield return Until(()=>match.TimingRollSequence==1,"actual long accuracy draw");
        if(accurate)
        {
            yield return Until(()=>authority.Capture().ballMoving,"accurate long-pass flight begins");
            var flight=authority.Capture();
            Assert.That(flight.passActive && flight.hasSelectedTarget,Is.True);
            Assert.That(flight.stage,Is.EqualTo("long.resolving"));
            Assert.That(flight.selectedX,Is.EqualTo(8*side));Assert.That(flight.selectedZ,Is.EqualTo(-6));
            pause.SetConnectionHold(true);var position=ball.transform.position;
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(Vector3.Distance(position,ball.transform.position),Is.LessThan(.001f));
            Assert.That(match.TimingRollSequence,Is.EqualTo(1));
            pause.SetConnectionHold(false);
        }
        if(!accurate)
        {
            yield return Until(()=>authority.Capture().stage=="long.direction","real inaccurate direction decision");
            RigOnlineLongRoll(1);OnlineAccepted(authority,"home",5,OnlineGroundPassAuthority.LongRollAction);
            yield return Until(()=>authority.Capture().stage=="long.distance","real scatter distance decision");
            RigOnlineLongRoll(1);OnlineAccepted(authority,"home",6,OnlineGroundPassAuthority.LongRollAction);
        }
        yield return Until(()=>authority.Capture().stage=="long.keeper","mandatory goalkeeper Pace decision");
        var keeper=authority.Capture();Assert.That(keeper.owner,Is.EqualTo("Away"));
        var skip=OnlineGroundCommand(keeper,1,OnlineGroundPassAuthority.LongKeeperDeclineAction);
        Assert.That(authority.Submit("home",OnlineGroundCommand(keeper,7,OnlineGroundPassAuthority.LongKeeperDeclineAction)).Status,Is.EqualTo(OnlineCommandStatus.WrongTeam));
        pause.SetConnectionHold(true);
        Assert.That(authority.Submit("away",skip).Status,Is.Not.EqualTo(OnlineCommandStatus.Accepted));
        pause.SetConnectionHold(false);
        OnlineAccepted(authority,"away",1,OnlineGroundPassAuthority.LongKeeperDeclineAction);
        yield return Until(()=>!longBall.isActivated && !ball.isMoving,"long pass lands and continues");
        var final=authority.Capture();
        Assert.That(final.rollSequence,Is.EqualTo(accurate?1:3));
        Assert.That(final.rolls.Select(r=>r.context),Is.EqualTo(accurate?new[]{"long_ball_accuracy"}:new[]{"long_ball_accuracy","long_ball_direction","long_ball_distance"}));
        Assert.That(match.gameData.stats.GetPlayerStats(attacker).aerialPassesAttempted,Is.EqualTo(1));
        Assert.That(match.gameData.stats.GetPlayerStats(attacker).aerialPassesTargeted,Is.EqualTo(accurate?1:0));
        Assert.That(movement.isActivated,Is.True,"an aerial ball in space uses the existing automatic movement continuation");
        Assert.That(match.totalCompletedActions,Is.EqualTo(1));
        if(accurate) Assert.That(ball.GetCurrentHex(),Is.SameAs(target));
        var replica=new OnlineGroundPassReplica(protocol.MatchId,final.tokens.Select(t=>t.id),protocol.Compatibility.Catalog);
        Assert.That(replica.Apply(final),Is.True);
        Assert.That(replica.State.rolls.Length,Is.EqualTo(accurate?1:3));
        Assert.That(replica.State.tokens.Single(t=>t.id==OnlineRollPublication.ActorId(attacker)).aerialPassesAttempted,Is.EqualTo(1));
    }

    public IEnumerator RunOnlineLongTargetInvalidation()
    {
        var authority=OnlineGroundBoard(1,true,false,out _,out _);
        OnlineAccepted(authority,"home",1,OnlineGroundPassAuthority.LongStartAction);
        var action=OnlineGroundPassAuthority.LongTargetAction(8,-6);
        var before=authority.Capture();Assert.That(before.legalActions,Does.Contain(action));
        Place(defender,Hex(8,-6));
        var stale=OnlineGroundCommand(before,2,action);
        Assert.That(authority.Submit("home",stale).Status,Is.EqualTo(OnlineCommandStatus.StaleRevision));
        Assert.That(authority.Capture().legalActions,Does.Not.Contain(action));
        Assert.That(longBall.currentTargetHex,Is.Null);Assert.That(match.TimingRollSequence,Is.Zero);
        using(var guest=new OnlineRoomSession("long-guest",true))
            Assert.That(longBall.TryExecuteOnlineLongBall(OnlineLongBallIntent.Target,Hex(7,-6)),Is.False);
        yield break;
    }

    public IEnumerator RunOnlineLongKeeperResponse(int side,string response)
    {
        var authority=OnlineGroundBoard(side,true,false,out var protocol,out _);
        Place(awayGK,Hex(14*side,-4));
        AttachAuthoredPanels();duelPanel.resultSeconds=.1f;
        using var pause=new OnlineSharedPause(protocol,match);
        attacker.highPass=4;
        var target=Hex(11*side,-4);
        OnlineAccepted(authority,"home",1,OnlineGroundPassAuthority.LongStartAction);
        OnlineAccepted(authority,"home",2,OnlineGroundPassAuthority.LongTargetAction(11*side,-4));
        OnlineAccepted(authority,"home",3,OnlineGroundPassAuthority.LongTargetAction(11*side,-4));
        RigOnlineLongRoll(5);OnlineAccepted(authority,"home",4,OnlineGroundPassAuthority.LongRollAction);
        yield return Until(()=>authority.Capture().stage=="long.keeper","keeper response after real accurate flight");
        bool claim=response=="claim",intercept=response is "intercept" or "failed";
        var destination=claim?target:longBall.OnlineGoalkeeperDestinations.First(h=>h!=target && target.GetNeighbors(grid).Contains(h)==intercept);
        var action=claim?OnlineGroundPassAuthority.LongKeeperClaimAction:OnlineGroundPassAuthority.LongKeeperMoveAction(destination.coordinates.x,destination.coordinates.z);
        var command=OnlineGroundCommand(authority.Capture(),1,action);
        Assert.That(authority.Submit("away",command).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
        Assert.That(authority.Submit("away",command).Status,Is.EqualTo(OnlineCommandStatus.Duplicate));
        if(intercept)
        {
            yield return Until(()=>authority.Capture().stage=="long.interception","keeper Pace precedes actual landing interception");
            RigOnlineLongRoll(response=="intercept"?6:1);
            var roll=OnlineGroundCommand(authority.Capture(),2,OnlineGroundPassAuthority.LongRollAction);
            Assert.That(authority.Submit("away",roll).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
            Assert.That(authority.Submit("away",roll).Status,Is.EqualTo(OnlineCommandStatus.Duplicate));
        }
        yield return Until(()=>!longBall.isActivated && !movement.isPlayerMoving,"typed keeper move settles landing exactly once");
        Assert.That(awayGK.currentHex,Is.SameAs(destination));Assert.That(match.TimingRollSequence,Is.EqualTo(intercept?2:1));
        Assert.That(match.totalCompletedActions,Is.EqualTo(1));
        if(claim || response=="intercept")
        {
            Assert.That(match.teamInAttack,Is.EqualTo(MatchManager.TeamInAttack.Away));
            Assert.That(match.LastTokenToTouchTheBallOnPurpose,Is.SameAs(awayGK));
            Assert.That(match.gameData.stats.GetPlayerStats(awayGK).possessionWon,Is.EqualTo(1));
        }
        else Assert.That(match.LastTokenToTouchTheBallOnPurpose,Is.SameAs(attacker));
        if(intercept)
        {
            Assert.That(match.gameData.stats.GetPlayerStats(awayGK).interceptionsAttempted,Is.EqualTo(1));
            Assert.That(match.gameData.stats.GetPlayerStats(awayGK).interceptionsMade,Is.EqualTo(response=="intercept"?1:0));
        }
    }
}
