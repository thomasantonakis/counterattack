using System.Collections;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunOnlineKeeperFreeMove(int side,bool move)
    {
        var authority=OnlineGroundBoard(side,true,false,out var protocol,out var receiver);
        Place(attacker,Hex(10*side,4));Place(receiver,Hex(14*side,4));
        ball.PlaceAtCell(attacker.currentHex);match.SetLastToken(attacker);match.UpdatePossessionAfterPass(attacker.currentHex);
        AttachAuthoredPanels();using var pause=new OnlineSharedPause(protocol,match);pause.Capture(authority);
        void Submit(string connection,string action)=>OnlineAccepted(authority,connection,
            protocol.LastAcceptedSequence(connection=="home"?OnlineTeam.Home:OnlineTeam.Away)+1,action);
        Submit("home","ground.start");
        var target=OnlineGroundPassAuthority.TargetAction(14*side,4);Submit("home",target);Submit("home",target);
        yield return Until(()=>keeper.isActivated && grid.highlightedHexes.Count>0,"pass entering box offers real goalkeeper free move");
        var state=authority.Capture();Assert.That(state.stage,Is.EqualTo("keeper.move"));Assert.That(state.owner,Is.EqualTo("Away"));
        Assert.That(state.selectedPlayer,Is.EqualTo(OnlineRollPublication.ActorId(awayGK)));
        Assert.That(authority.Submit("home",OnlineGroundCommand(state,protocol.LastAcceptedSequence(OnlineTeam.Home)+1,"keeper.stay")).Status,Is.EqualTo(OnlineCommandStatus.WrongTeam));
        Assert.That(state.legalActions,Does.Not.Contain("ground.start"));
        Assert.That(keeper.TryExecuteOnlineGoalkeeper(OnlineGoalkeeperIntent.Move,target:attacker.currentHex),Is.False);
        using(var guest=new OnlineRoomSession("free-move-guest",true))
            Assert.That(keeper.TryExecuteOnlineGoalkeeper(OnlineGoalkeeperIntent.Stay),Is.False);
        var destination=grid.highlightedHexes.First(h=>h!=awayGK.currentHex && h.GetOccupyingToken()==null);
        var action=move?"keeper.move:"+destination.coordinates.x+":"+destination.coordinates.z:"keeper.stay";
        var command=OnlineGroundCommand(state,protocol.LastAcceptedSequence(OnlineTeam.Away)+1,action);
        Submit("away",action);
        Assert.That(authority.Submit("away",command).Status,Is.EqualTo(OnlineCommandStatus.Duplicate));
        if(move)
        {
            yield return Until(()=>keeper.IsResolvingMove,"owned keeper move starts");
            pause.SetConnectionHold(true);yield return null;var position=awayGK.transform.position;
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(Vector3.Distance(position,awayGK.transform.position),Is.LessThan(.001f));
            Assert.That(authority.Capture().legalActions,Is.Empty);pause.SetConnectionHold(false);
        }
        yield return Until(()=>!ground.isActivated,"pass resumes after keeper decision");
        Assert.That(ball.GetCurrentHex(),Is.SameAs(receiver.currentHex));
        if(move)Assert.That(awayGK.currentHex,Is.SameAs(destination));
        Assert.That(keeper.HasConsumedBoxMoveForDecisionPreview(side),Is.True);
        Assert.That(match.gameData.stats.GetPlayerStats(attacker).passesCompleted,Is.EqualTo(1));
        Assert.That(match.TimingRollSequence,Is.Zero);
    }

    public IEnumerator RunOnlineKeeperPickup(int side,string branch)
    {
        yield return RunOnlineHeadedBackPass(side,"feet");
        yield return FinishPickupFinalThirds();
        var version=OnlineGroundVersion();
        var protocol=new OnlineMatchProtocol("pickup-proof","home-account","away-account","away-account",version);
        protocol.BindAuthenticatedAccount("home-account","home",version);protocol.BindAuthenticatedAccount("away-account","away",version);
        var authority=new OnlineGroundPassAuthority(match,protocol);using var pause=new OnlineSharedPause(protocol,match);pause.Capture(authority);
        // Complete the real movement action. Its normal completion owns the
        // deferred pickup offer, before Final Third can resume.
        movement.ActivateMovementPhase();movement.CommitToAction();FinishPickupMovement();
        yield return Until(()=>keeper.IsWaitingForPickupChoice,"movement completion offers eligible headed-backpass keeper pickup");
        var state=authority.Capture();Assert.That(state.stage,Is.EqualTo("keeper.pickup"));Assert.That(state.owner,Is.EqualTo("Home"));
        Assert.That(state.selectedPlayer,Is.EqualTo(OnlineRollPublication.ActorId(homeGK)));
        bool hands=branch=="hands";
        var action=hands?"keeper.pickup.hands":"keeper.pickup.feet";
        var command=OnlineGroundCommand(state,1,action);
        Assert.That(authority.Submit("away",command).Status,Is.EqualTo(OnlineCommandStatus.WrongTeam));
        if(branch=="interrupt")
        {
            match.BeginGoalkeeperBallDelivery(GoalkeeperDeliveryKind.Kick,homeGK);
            Assert.That(keeper.IsWaitingForPickupChoice,Is.False);
            Assert.That(keeper.TryExecuteOnlineGoalkeeper(OnlineGoalkeeperIntent.Hands),Is.False);
            Assert.That(authority.Submit("home",command).Status,Is.Not.EqualTo(OnlineCommandStatus.Accepted));
            Assert.That(match.shotManager.isWaitingForSaveandHoldScenario,Is.False);
            Assert.That(match.TimingRollSequence,Is.Zero);yield break;
        }
        using(var guest=new OnlineRoomSession("pickup-guest",true))
            Assert.That(keeper.TryExecuteOnlineGoalkeeper(OnlineGoalkeeperIntent.Hands),Is.False);
        pause.SetConnectionHold(true);
        Assert.That(authority.Capture().legalActions,Is.Empty);
        Assert.That(authority.Submit("home",command).Status,Is.Not.EqualTo(OnlineCommandStatus.Accepted));
        pause.SetConnectionHold(false);
        OnlineAccepted(authority,"home",1,action);
        Assert.That(keeper.IsWaitingForPickupChoice,Is.False);
        Assert.That(match.shotManager.isWaitingForSaveandHoldScenario,Is.EqualTo(hands));
        Assert.That(match.CanGoalkeeperHandle(homeGK),Is.True);
        Assert.That(match.KeeperHandlingEligibility,Is.EqualTo(hands?GoalkeeperHandlingEligibility.Holding:GoalkeeperHandlingEligibility.TeammateHeader));
        Assert.That(match.TimingRollSequence,Is.Zero);
    }
}
