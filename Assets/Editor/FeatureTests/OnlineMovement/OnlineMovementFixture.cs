using System.Collections;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunOnlineMovement(int side,bool afterLongPass)
    {
        var authority=OnlineGroundBoard(side,true,false,out var protocol,out var receiver);
        AttachAuthoredPanels();duelPanel.resultSeconds=.1f;
        using var pause=new OnlineSharedPause(protocol,match);
        void Submit(string connection,string action)
        {
            var team=connection=="home"?OnlineTeam.Home:OnlineTeam.Away;
            OnlineAccepted(authority,connection,protocol.LastAcceptedSequence(team)+1,action);
        }
        if(afterLongPass)
        {
            attacker.highPass=4;
            Submit("home",OnlineGroundPassAuthority.LongStartAction);
            // Keep this movement feature outside a Final Third interruption;
            // that separately owned phase has its own upcoming online adapter.
            Submit("home",OnlineGroundPassAuthority.LongTargetAction(6*side,-6));
            Submit("home",OnlineGroundPassAuthority.LongTargetAction(6*side,-6));
            RigOnlineLongRoll(5);Submit("home",OnlineGroundPassAuthority.LongRollAction);
            yield return Until(()=>authority.Capture().stage=="long.keeper","long pass reaches keeper response");
            Submit("away",OnlineGroundPassAuthority.LongKeeperDeclineAction);
            yield return Until(()=>!longBall.isActivated,"long pass enters automatic movement");
        }
        else Submit("home",OnlineGroundPassAuthority.MovementStartAction);
        Assert.That(authority.Capture().movementActive,Is.True);
        Assert.That(authority.Capture().owner,Is.EqualTo("Home"));
        var beforeSelection=authority.Capture();
        var choose=OnlineGroundPassAuthority.MovementPlayerAction(OnlineRollPublication.ActorId(receiver));
        Assert.That(authority.Submit("away",OnlineGroundCommand(beforeSelection,protocol.LastAcceptedSequence(OnlineTeam.Away)+1,choose)).Status,Is.EqualTo(OnlineCommandStatus.WrongTeam));
        Submit("home",choose);
        Assert.That(authority.Capture().selectedPlayer,Is.EqualTo(OnlineRollPublication.ActorId(receiver)));
        var destination=receiver.currentHex.GetNeighbors(grid).First(h=>h!=null && h.GetOccupyingToken()==null
            && authority.Capture().legalActions.Contains(OnlineGroundPassAuthority.MovementTargetAction(h.coordinates.x,h.coordinates.z)));
        var command=OnlineGroundCommand(authority.Capture(),protocol.LastAcceptedSequence(OnlineTeam.Home)+1,
            OnlineGroundPassAuthority.MovementTargetAction(destination.coordinates.x,destination.coordinates.z));
        Assert.That(authority.Submit("home",command).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
        Assert.That(authority.Submit("home",command).Status,Is.EqualTo(OnlineCommandStatus.Duplicate));
        yield return Until(()=>movement.isPlayerMoving,"real player movement starts");
        pause.SetConnectionHold(true);var position=receiver.transform.position;
        yield return new WaitForSecondsRealtime(.2f);
        Assert.That(Vector3.Distance(position,receiver.transform.position),Is.LessThan(.001f));
        Assert.That(authority.Capture().legalActions,Is.Empty);
        pause.SetConnectionHold(false);
        yield return Until(()=>!movement.isPlayerMoving && receiver.currentHex==destination,"player arrives once");
        Assert.That(movement.movedTokens,Does.Contain(receiver));
        Assert.That(authority.Capture().tokens.Single(t=>t.id==OnlineRollPublication.ActorId(receiver)).x,Is.EqualTo(destination.coordinates.x));
        Submit("home",OnlineGroundPassAuthority.MovementForfeitAction);
        Assert.That(authority.Capture().owner,Is.EqualTo("Away"));
        Submit("away",OnlineGroundPassAuthority.MovementForfeitAction);
        Assert.That(authority.Capture().owner,Is.EqualTo("Home"));
        Submit("home",OnlineGroundPassAuthority.MovementForfeitAction);
        Assert.That(movement.isActivated,Is.EqualTo(afterLongPass),
            "a ball still in space starts the next movement under the existing possession rule");
        Assert.That(match.totalCompletedActions,Is.EqualTo(afterLongPass?2:1));
        Assert.That(match.TimingRollSequence,Is.EqualTo(afterLongPass?1:0));
        var final=authority.Capture();var replica=new OnlineGroundPassReplica(protocol.MatchId);
        Assert.That(replica.Apply(final),Is.True);Assert.That(replica.State.movementActive,Is.EqualTo(afterLongPass));
    }

    public IEnumerator RunOnlineMovementInvalidTargets()
    {
        var authority=OnlineGroundBoard(1,true,false,out _,out var receiver);
        OnlineAccepted(authority,"home",1,OnlineGroundPassAuthority.MovementStartAction);
        OnlineAccepted(authority,"home",2,OnlineGroundPassAuthority.MovementPlayerAction(OnlineRollPublication.ActorId(receiver)));
        var state=authority.Capture();var origin=receiver.currentHex;
        Assert.That(authority.Submit("home",OnlineGroundCommand(state,3,OnlineGroundPassAuthority.MovementTargetAction(999,999))).Status,Is.EqualTo(OnlineCommandStatus.IllegalAction));
        Assert.That(movement.TryExecuteOnlineMovement(OnlineMovementIntent.Move,target:attacker.currentHex),Is.False);
        using(var guest=new OnlineRoomSession("movement-guest",true))
            Assert.That(movement.TryExecuteOnlineMovement(OnlineMovementIntent.Forfeit),Is.False);
        Assert.That(receiver.currentHex,Is.SameAs(origin));Assert.That(match.TimingRollSequence,Is.Zero);
        yield break;
    }
}
