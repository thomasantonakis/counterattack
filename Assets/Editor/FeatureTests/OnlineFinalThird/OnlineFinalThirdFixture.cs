using System.Collections;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunOnlineFinalThird(int side,bool move)
    {
        var authority=OnlineGroundBoard(side,true,false,out var protocol,out _);
        Place(defender,Hex(-12*side,2));AttachAuthoredPanels();duelPanel.resultSeconds=.1f;
        using var pause=new OnlineSharedPause(protocol,match);
        void Submit(string connection,string action)
        {OnlineAccepted(authority,connection,protocol.LastAcceptedSequence(connection=="home"?OnlineTeam.Home:OnlineTeam.Away)+1,action);}
        attacker.highPass=4;
        Submit("home",OnlineGroundPassAuthority.LongStartAction);
        Submit("home",OnlineGroundPassAuthority.LongTargetAction(8*side,-6));
        Submit("home",OnlineGroundPassAuthority.LongTargetAction(8*side,-6));
        RigOnlineLongRoll(5);Submit("home",OnlineGroundPassAuthority.LongRollAction);
        yield return Until(()=>authority.Capture().stage=="long.keeper","real long-pass keeper response");
        Submit("away",OnlineGroundPassAuthority.LongKeeperDeclineAction);
        yield return Until(()=>authority.Capture().stage=="final-third.select","Final Third takes priority over automatic movement");
        Assert.That(authority.Capture().owner,Is.EqualTo("Home"));
        Assert.That(authority.Capture().legalActions,Does.Not.Contain(OnlineGroundPassAuthority.MovementForfeitAction));
        var prior=authority.Capture();
        Assert.That(authority.Submit("away",OnlineGroundCommand(prior,2,OnlineGroundPassAuthority.FinalThirdForfeitAction)).Status,Is.EqualTo(OnlineCommandStatus.WrongTeam));
        using(var guest=new OnlineRoomSession("f3-guest",true))
            Assert.That(match.finalThirdManager.TryExecuteOnlineFinalThird(OnlineFinalThirdIntent.Forfeit),Is.False);
        if(move)
        {
            Submit("home",OnlineGroundPassAuthority.FinalThirdPlayerAction(OnlineRollPublication.ActorId(homeGK)));
            var immediateTarget=match.finalThirdManager.GetMovementDestinationsForDecisionPreview(homeGK).First();
            Assert.That(match.finalThirdManager.TryExecuteOnlineFinalThird(OnlineFinalThirdIntent.Move,target:immediateTarget),Is.False,
                "a destination cannot overtake the coroutine's selection acknowledgement");
            yield return Until(()=>authority.Capture().stage=="final-third.target","Final Third coroutine acknowledges selected player");
            var target=homeGK.currentHex.GetNeighbors(grid).First(h=>h!=null && h.coordinates.x*side>homeGK.currentHex.coordinates.x*side
                && authority.Capture().legalActions.Contains(OnlineGroundPassAuthority.FinalThirdTargetAction(h.coordinates.x,h.coordinates.z)));
            var command=OnlineGroundCommand(authority.Capture(),protocol.LastAcceptedSequence(OnlineTeam.Home)+1,
                OnlineGroundPassAuthority.FinalThirdTargetAction(target.coordinates.x,target.coordinates.z));
            Assert.That(authority.Submit("home",command).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
            Assert.That(authority.Submit("home",command).Status,Is.EqualTo(OnlineCommandStatus.Duplicate));
            yield return Until(()=>movement.isPlayerMoving,"real Final Third movement starts");
            Assert.That(match.finalThirdManager.TryExecuteOnlineFinalThird(OnlineFinalThirdIntent.Forfeit),Is.False);
            pause.SetConnectionHold(true);var position=homeGK.transform.position;
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(Vector3.Distance(position,homeGK.transform.position),Is.LessThan(.001f));
            Assert.That(authority.Capture().legalActions,Is.Empty);
            pause.SetConnectionHold(false);
            yield return Until(()=>!movement.isPlayerMoving && homeGK.currentHex==target,"Final Third player arrives once");
        }
        else Submit("home",OnlineGroundPassAuthority.FinalThirdForfeitAction);
        yield return Until(()=>authority.Capture().stage=="final-third.select" && authority.Capture().owner=="Away","defending Final Third team owns next choice");
        Submit("away",OnlineGroundPassAuthority.FinalThirdForfeitAction);
        yield return Until(()=>!match.finalThirdManager.isActivated,"Final Third completes before ordinary movement resumes");
        var final=authority.Capture();Assert.That(final.stage,Does.StartWith("movement."));
        Assert.That(final.owner,Is.EqualTo("Home"));Assert.That(final.movementActive,Is.True);
        Assert.That(match.totalCompletedActions,Is.EqualTo(1));Assert.That(match.TimingRollSequence,Is.EqualTo(1));
        Assert.That(match.gameData.stats.GetPlayerStats(attacker).aerialPassesAttempted,Is.EqualTo(1));
        Assert.That(new OnlineGroundPassReplica(protocol.MatchId).Apply(final),Is.True);
    }
}
