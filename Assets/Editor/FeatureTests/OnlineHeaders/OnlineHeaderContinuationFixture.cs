using System.Collections;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;

public sealed partial class GoalkeeperBoxEntryFixture
{
    private IEnumerator HoldOnlineHeaderContinuation(OnlineSharedPause pause,OnlineMatchProtocol protocol,System.Func<bool> pending)
    {
        Assert.That(pause.Submit("home",new OnlineMatchCommand(protocol.MatchId,1,0,"shared-pause",OnlineSharedPause.RequestAction)).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
        pause.SetConnectionHold(true);yield return new WaitForSecondsRealtime(.25f);
        Assert.That(pending(),Is.True,"overlapping authority holds preserve this continuation");
        pause.SetConnectionHold(false);yield return new WaitForSecondsRealtime(.15f);
        Assert.That(pending(),Is.True,"shared pause outlives the connection hold");
        Assert.That(pause.Submit("home",new OnlineMatchCommand(protocol.MatchId,2,1,"shared-pause",OnlineSharedPause.ResumeAction)).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
    }
    public IEnumerator RunOnlineHeadedInterception(int side,string branch)
    {
        var authority=OnlineGroundBoard(side,true,false,out var protocol,out var receiver);
        Place(defender,Hex(side,9));ball.PlaceAtCell(receiver.currentHex);match.SetLastToken(receiver);
        AttachAuthoredPanels();duelPanel.resultSeconds=.1f;
        var header=match.highPassManager.headerManager;
        using var pause=new OnlineSharedPause(protocol,match);match.isClockRunning=true;
        void Submit(string connection,string action)=>OnlineAccepted(authority,connection,
            protocol.LastAcceptedSequence(connection=="home"?OnlineTeam.Home:OnlineTeam.Away)+1,action);
        yield return header.FindEligibleHeaderTokens(ball.GetCurrentHex());
        Submit("home",OnlineGroundPassAuthority.HeaderFreeAction);
        yield return Until(()=>header.isWaitingForHeaderTargetSelection,"free headed-pass targeting");
        Submit("home",OnlineGroundPassAuthority.HeaderTargetAction(0,9));
        yield return Until(()=>authority.Capture().stage=="header.interception","real landing interception");
        var state=authority.Capture();Assert.That(state.owner,Is.EqualTo("Away"));
        Assert.That(state.selectedPlayer,Is.EqualTo(OnlineRollPublication.ActorId(defender)));
        var late=OnlineGroundCommand(state,protocol.LastAcceptedSequence(OnlineTeam.Away)+1,OnlineGroundPassAuthority.HeaderInterceptionAction);
        RigOnlineLongRoll(branch=="miss"?1:6);
        if(branch=="automatic")match.AdvanceOnlineAuthorityTime(DecisionTimingConfig.AutomaticRollSeconds+.1f);
        else Submit("away",OnlineGroundPassAuthority.HeaderInterceptionAction);
        Assert.That(authority.Capture().legalActions,Does.Not.Contain(OnlineGroundPassAuthority.HeaderInterceptionAction));
        Assert.That(authority.Submit("away",late).Status,Is.Not.EqualTo(OnlineCommandStatus.Accepted));
        Assert.That(match.CaptureOnlineRolls()[0].actorId,Is.EqualTo(OnlineRollPublication.ActorId(defender)),"interception observation belongs to the rolling defender");
        if(branch=="hold")yield return HoldOnlineHeaderContinuation(pause,protocol,()=>header.isWaitingForInterceptionRoll && match.gameData.stats.GetPlayerStats(defender).interceptionsAttempted==0);
        if(branch=="cancel")
        {
            header.CleanUpHeader();yield return new WaitForSecondsRealtime(.3f);
            Assert.That(match.LastTokenToTouchTheBallOnPurpose,Is.SameAs(receiver));
            Assert.That(match.gameData.stats.GetPlayerStats(defender).interceptionsAttempted,Is.Zero);yield break;
        }
        yield return Until(()=>!header.isActivated,"landing interception resolves once");
        Assert.That(match.TimingRollSequence,Is.EqualTo(1));
        Assert.That(match.gameData.stats.GetPlayerStats(defender).interceptionsAttempted,Is.EqualTo(1));
        Assert.That(match.gameData.stats.GetPlayerStats(defender).interceptionsMade,Is.EqualTo(branch=="miss"?0:1));
        if(branch!="miss")Assert.That(match.LastTokenToTouchTheBallOnPurpose,Is.SameAs(defender));
    }

    public IEnumerator RunOnlineHeadedBackPass(int side,string branch)
    {
        var authority=OnlineGroundBoard(side,true,false,out var protocol,out _);
        Place(attacker,Hex(-13*side));ball.PlaceAtCell(attacker.currentHex);match.SetLastToken(attacker);
        AttachAuthoredPanels();duelPanel.resultSeconds=.1f;
        var header=match.highPassManager.headerManager;
        using var pause=new OnlineSharedPause(protocol,match);match.isClockRunning=true;
        void Submit(string action)=>OnlineAccepted(authority,"home",protocol.LastAcceptedSequence(OnlineTeam.Home)+1,action);
        yield return header.FindEligibleHeaderTokens(ball.GetCurrentHex());
        Submit(OnlineGroundPassAuthority.HeaderFreeAction);
        yield return Until(()=>header.isWaitingForHeaderTargetSelection,"free header to own keeper");
        Submit(OnlineGroundPassAuthority.HeaderTargetAction(homeGK.currentHex.coordinates.x,homeGK.currentHex.coordinates.z));
        yield return Until(()=>header.isWaitingForHeadedBackPassChoice,"real headed back-pass arrival");
        var choice=authority.Capture();Assert.That(choice.stage,Is.EqualTo("header.backpass.choice"));
        Assert.That(choice.owner,Is.EqualTo("Home"));
        Assert.That(authority.Submit("away",OnlineGroundCommand(choice,1,"header.backpass.hands")).Status,Is.EqualTo(OnlineCommandStatus.WrongTeam));
        Submit(branch=="feet"?"header.backpass.feet":"header.backpass.hands");
        if(branch=="feet")
        {
            yield return Until(()=>!header.isActivated,"keeper receives headed pass at feet");
            Assert.That(match.TimingRollSequence,Is.Zero);Assert.That(match.LastTokenToTouchTheBallOnPurpose,Is.SameAs(homeGK));yield break;
        }
        Assert.That(authority.Capture().stage,Is.EqualTo("header.backpass.roll"));
        var late=OnlineGroundCommand(authority.Capture(),protocol.LastAcceptedSequence(OnlineTeam.Home)+1,"header.backpass.roll");
        RigOnlineLongRoll(branch=="spill"?1:2);
        if(branch=="automatic")match.AdvanceOnlineAuthorityTime(DecisionTimingConfig.AutomaticRollSeconds+.1f);
        else Submit("header.backpass.roll");
        Assert.That(authority.Capture().legalActions,Does.Not.Contain("header.backpass.roll"));
        Assert.That(authority.Submit("home",late).Status,Is.Not.EqualTo(OnlineCommandStatus.Accepted));
        if(branch=="hold")yield return HoldOnlineHeaderContinuation(pause,protocol,()=>header.isWaitingForHeadedBackPassRoll && !match.shotManager.isWaitingForSaveandHoldScenario);
        if(branch=="cancel")
        {
            header.CleanUpHeader();yield return new WaitForSecondsRealtime(.3f);
            Assert.That(loose.isActivated || match.shotManager.isWaitingForSaveandHoldScenario,Is.False);yield break;
        }
        yield return Until(()=>branch=="spill"?loose.isWaitingForDirectionRoll:match.shotManager.isWaitingForSaveandHoldScenario,"headed catch outcome");
        Assert.That(match.TimingRollSequence,Is.EqualTo(1));
        Assert.That(match.CaptureOnlineRolls()[0].context,Is.EqualTo("headed_back_pass_catch"));
        Assert.That(match.CaptureOnlineRolls()[0].actorId,Is.EqualTo(OnlineRollPublication.ActorId(homeGK)));
        Assert.That(match.gameData.stats.GetPlayerStats(homeGK).attemptsSaved,Is.Zero);
    }
}
