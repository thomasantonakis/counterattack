using System.Collections;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;
public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunOnlineOutsideKeeperShot(int side,string choice)
    {
        var authority=OnlineGroundBoard(side,true,false,out var protocol,out _);yield return AttachLegacyGoalFlow();Invoke(grid,"LoadPrecomputedShootingPaths");
        Place(attacker,Hex(8*side));Place(awayGK,Hex(10*side,choice=="eligible"?1:3));Place(defender,Hex(2*side,-10));
        ball.PlaceAtCell(attacker.currentHex);match.SetLastToken(attacker);match.UpdatePossessionAfterPass(attacker.currentHex);
        match.BroadcastSafeEndofMovementPhase(countMovementAction:false,triggerFinalThird:false);attacker.shooting=6;awayGK.saving=6;
        AttachAuthoredPanels();duelPanel.resultSeconds=.1f;var shot=match.shotManager;
        using var pause=new OnlineSharedPause(protocol,match);pause.Capture(authority);
        void Submit(string actor,string action)=>OnlineAccepted(authority,actor,protocol.LastAcceptedSequence(actor=="home"?OnlineTeam.Home:OnlineTeam.Away)+1,action);
        Submit("home","shot.start");Submit("home","shot.confirm");Submit("home","shot.target:"+(19*side)+":0");
        yield return Until(()=>shot.isWaitingForOutsideBoxGKShotDecision,"real shot path offers outside-box keeper choice");
        var state=authority.Capture();Assert.That(state.stage,Is.EqualTo("shot.keeper-choice"));Assert.That(state.owner,Is.EqualTo("Away"));
        Assert.That(state.selectedPlayer,Is.EqualTo(OnlineRollPublication.ActorId(awayGK)));Assert.That(state.awaitingRoll,Is.False);
        Assert.That(state.legalActions,Is.EquivalentTo(new[]{"shot.keeper.hands","shot.keeper.block"}));
        bool hands=choice is "hands" or "handball" or "hold";
        string action=hands?"shot.keeper.hands":"shot.keeper.block";
        var request=OnlineGroundCommand(state,protocol.LastAcceptedSequence(OnlineTeam.Away)+1,action);
        var wrong=OnlineGroundCommand(state,protocol.LastAcceptedSequence(OnlineTeam.Home)+1,action);
        Assert.That(authority.Submit("home",wrong).Status,Is.EqualTo(OnlineCommandStatus.WrongTeam));
        using(var guest=new OnlineRoomSession("outside-shot-guest",true))
            Assert.That(shot.TryExecuteOnlineShot(hands?OnlineShotIntent.OutsideHands:OnlineShotIntent.OutsideBlock),Is.False);
        Assert.That(match.TimingRollSequence,Is.Zero);
        if(choice=="cancel")
        {
            Invoke(shot,"ResetShotProcess");Assert.That(shot.TryExecuteOnlineShot(OnlineShotIntent.OutsideBlock),Is.False);
            Assert.That(authority.Submit("away",request).Status,Is.Not.EqualTo(OnlineCommandStatus.Accepted));
            Assert.That(authority.Capture().legalActions,Does.Not.Contain(action));Assert.That(match.TimingRollSequence,Is.Zero);yield break;
        }
        if(choice=="hold")
        {
            Assert.That(pause.Submit("home",new OnlineMatchCommand(protocol.MatchId,1,pause.Revision,"shared-pause",OnlineSharedPause.RequestAction)).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
            pause.SetConnectionHold(true);Assert.That(authority.Submit("away",request).Status,Is.EqualTo(OnlineCommandStatus.StaleRevision));
            var held=authority.Capture();var freshHeld=new OnlineMatchCommand(protocol.MatchId,protocol.LastAcceptedSequence(OnlineTeam.Away)+1,held.revision,request.DecisionId,action);
            Assert.That(authority.Submit("away",freshHeld).Status,Is.EqualTo(OnlineCommandStatus.NotReady));
            Assert.That(shot.TryExecuteOnlineShot(OnlineShotIntent.OutsideHands),Is.False);yield return new WaitForSecondsRealtime(.1f);
            pause.SetConnectionHold(false);Assert.That(shot.TryExecuteOnlineShot(OnlineShotIntent.OutsideHands),Is.False);
            Assert.That(pause.Submit("home",new OnlineMatchCommand(protocol.MatchId,2,pause.Revision,"shared-pause",OnlineSharedPause.ResumeAction)).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
            request=OnlineGroundCommand(authority.Capture(),protocol.LastAcceptedSequence(OnlineTeam.Away)+1,action);
        }
        Submit("away",action);Assert.That(shot.isWaitingForOutsideBoxGKShotDecision,Is.False);
        Assert.That(authority.Submit("away",request).Status,Is.EqualTo(OnlineCommandStatus.Duplicate));
        if(choice=="eligible")
        {
            yield return Until(()=>authority.Capture().stage=="shot.block","nearby goalkeeper receives an actual outfield block");
            RigOnlineLongRoll(1);Submit("away","shot.block");
            yield return Until(()=>match.TimingRollSequence==1,"keeper outfield block draws once");
        }
        yield return Until(()=>authority.Capture().stage=="shot.roll","choice continues to real shooter roll");
        Assert.That(match.TimingRollSequence,Is.EqualTo(choice=="eligible"?1:0));Assert.That(awayGK.isSentOff,Is.False);
        RigOnlineLongRoll(2);Submit("home","shot.roll");
        if(hands)
        {
            yield return Until(()=>authority.Capture().stage=="shot.save","outside-box hand choice reaches real keeper save");
            Assert.That(authority.Capture().owner,Is.EqualTo("Away"));Assert.That(keeper.IsGoalkeeperOwnPenaltyHex(awayGK,shot.saveHex),Is.False);
            if(choice=="handball")
            {
                var spot=shot.saveHex;RigOnlineLongRoll(6);Submit("away","shot.save");
                yield return Until(()=>awayGK.isSentOff && !shot.isActivated,"successful outside-box hand save receives the existing red-card restart");
                Assert.That(ball.GetCurrentHex(),Is.SameAs(spot));Assert.That(match.gameData.stats.homeTeamStats.totalGoals,Is.Zero);
                Assert.That(match.TimingRollSequence,Is.EqualTo(2));yield break;
            }
        }
        else
        {
            yield return Until(()=>!shot.isActivated,"declined or missed keeper block allows the real shot outcome",120);
            Assert.That(match.gameData.stats.homeTeamStats.totalGoals,Is.EqualTo(1));Assert.That(match.goalFlowManager.isActivated,Is.True);
        }
        Assert.That(match.TimingRollSequence,Is.EqualTo(choice=="eligible"?2:1));Assert.That(match.gameData.stats.GetPlayerStats(attacker).shotsAttempted,Is.EqualTo(1));
        Assert.That(match.gameData.stats.GetPlayerStats(awayGK).shotBlocksAttempted,Is.EqualTo(choice=="eligible"?1:0),"Only an eligible keeper receives an outfield block roll");
    }
}
