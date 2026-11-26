using System.Collections;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunOnlineAwayKeeperDistribution(int side)
    {
        var authority=OnlineGroundBoard(side,true,false,out var protocol,out _);
        match.ChangePossession();Place(defender,Hex(13*side));
        ball.PlaceAtCell(defender.currentHex);match.SetLastToken(defender);match.UpdatePossessionAfterPass(defender.currentHex);
        AttachAuthoredPanels();duelPanel.resultSeconds=.1f;
        var header=match.highPassManager.headerManager;
        using var pause=new OnlineSharedPause(protocol,match);
        void Submit(string action)=>OnlineAccepted(authority,"away",protocol.LastAcceptedSequence(OnlineTeam.Away)+1,action);
        yield return header.FindEligibleHeaderTokens(ball.GetCurrentHex());
        Assert.That(authority.Capture().owner,Is.EqualTo("Away"));Submit(OnlineGroundPassAuthority.HeaderFreeAction);
        yield return Until(()=>header.isWaitingForHeaderTargetSelection,"away free header");
        Submit(OnlineGroundPassAuthority.HeaderTargetAction(16*side,0));
        yield return Until(()=>header.isWaitingForHeadedBackPassChoice,"away headed back-pass choice");
        Submit(OnlineGroundPassAuthority.HeaderKeeperHandsAction);RigOnlineLongRoll(2);Submit(OnlineGroundPassAuthority.HeaderKeeperRollAction);
        yield return Until(()=>authority.Capture().stage=="keeper.distribution","away keeper catch distribution");
        var state=authority.Capture();Assert.That(state.owner,Is.EqualTo("Away"));
        Assert.That(state.selectedPlayer,Is.EqualTo(OnlineRollPublication.ActorId(awayGK)));
        Assert.That(authority.Submit("home",OnlineGroundCommand(state,1,OnlineGroundPassAuthority.KeeperQuickAction)).Status,Is.EqualTo(OnlineCommandStatus.WrongTeam));
        Submit(OnlineGroundPassAuthority.KeeperQuickAction);
        string target=OnlineGroundPassAuthority.TargetAction(13*side,0);Submit(target);Submit(target);
        yield return Until(()=>!ground.isActivated && match.LastTokenToTouchTheBallOnPurpose==defender,"away keeper quick throw collection");
        Assert.That(match.gameData.stats.GetPlayerStats(awayGK).passesCompleted,Is.EqualTo(1));
        Assert.That(match.TimingRollSequence,Is.EqualTo(1));
    }

    public IEnumerator RunOnlineKeeperDistribution(int side,string branch,bool held)
    {
        yield return RunOnlineHeadedBackPass(side,"catch");
        var mate=Token("Quick throw receiver",true,false,7);Place(mate,Hex(-12*side,-2));
        var version=OnlineGroundVersion();
        var protocol=new OnlineMatchProtocol("keeper-proof","home-account","away-account","away-account",version);
        protocol.BindAuthenticatedAccount("home-account","home",version);protocol.BindAuthenticatedAccount("away-account","away",version);
        var authority=new OnlineGroundPassAuthority(match,protocol);
        using var pause=new OnlineSharedPause(protocol,match);
        void Submit(string connection,string action)=>OnlineAccepted(authority,connection,
            protocol.LastAcceptedSequence(connection=="home"?OnlineTeam.Home:OnlineTeam.Away)+1,action);
        var state=pause.Capture(authority);Assert.That(state.stage,Is.EqualTo("keeper.distribution"));
        Assert.That(state.owner,Is.EqualTo("Home"));Assert.That(state.selectedPlayer,Is.EqualTo(OnlineRollPublication.ActorId(homeGK)));
        string action=branch=="quick"?"keeper.quick":"keeper.final-third";
        Assert.That(authority.Submit("away",OnlineGroundCommand(state,1,action)).Status,Is.EqualTo(OnlineCommandStatus.WrongTeam));
        using(var guest=new OnlineRoomSession("keeper-guest",true))
            Assert.That(match.shotManager.TryExecuteOnlineKeeperDistribution(OnlineKeeperDistributionIntent.QuickThrow),Is.False);
        if(held)
        {
            var old=OnlineGroundCommand(state,1,action);
            Assert.That(pause.Submit("home",new OnlineMatchCommand(protocol.MatchId,1,0,"shared-pause",OnlineSharedPause.RequestAction)).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
            pause.SetConnectionHold(true);yield return new WaitForSecondsRealtime(.1f);
            state=authority.Capture();Assert.That(state.stage,Is.EqualTo("blocked"));
            Assert.That(state.connectionHeld && state.isSharedPaused,Is.True);
            Assert.That(authority.Submit("home",old).Status,Is.Not.EqualTo(OnlineCommandStatus.Accepted));
            pause.SetConnectionHold(false);
            Assert.That(authority.Capture().stage,Is.EqualTo("blocked"));
            Assert.That(pause.Submit("home",new OnlineMatchCommand(protocol.MatchId,2,1,"shared-pause",OnlineSharedPause.ResumeAction)).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
            Assert.That(authority.Submit("home",old).Status,Is.Not.EqualTo(OnlineCommandStatus.Accepted));
            old=OnlineGroundCommand(authority.Capture(),1,action);
            ball.PlaceAtCell(Hex(-15*side,2));Assert.That(authority.Capture().legalActions,Is.Empty);
            Assert.That(authority.Submit("home",old).Status,Is.Not.EqualTo(OnlineCommandStatus.Accepted));
            ball.PlaceAtCell(homeGK.currentHex);
        }
        var accepted=OnlineGroundCommand(authority.Capture(),1,action);
        Assert.That(authority.Submit("home",accepted).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
        Assert.That(authority.Submit("home",accepted).Status,Is.EqualTo(OnlineCommandStatus.Duplicate));
        Assert.That(match.shotManager.isWaitingForSaveandHoldScenario,Is.False);
        if(branch=="quick")
        {
            // BroadcastQuickThrow already enters its committed target mode through the real match API.
            Assert.That(ground.isQuickThrow,Is.True);Assert.That(authority.Capture().stage,Is.EqualTo("target"));
            string target=OnlineGroundPassAuthority.TargetAction(mate.currentHex.coordinates.x,mate.currentHex.coordinates.z);
            Submit("home",target);Submit("home",target);
            yield return Until(()=>!ground.isActivated && match.LastTokenToTouchTheBallOnPurpose==mate,"quick throw collection");
            Assert.That(match.gameData.stats.GetPlayerStats(homeGK).passesCompleted,Is.EqualTo(1));
            Assert.That(match.TimingRollSequence,Is.EqualTo(1),"a clear quick throw consumes no additional roll");yield break;
        }
        for(int i=0;i<6 && authority.Capture().stage!="final-third.keeper-choice";i++)
        {
            yield return Until(()=>authority.Capture().legalActions.Length>0,"Final Third owned move or keeper choice");
            state=authority.Capture();if(state.stage=="final-third.keeper-choice")break;
            Assert.That(state.legalActions,Does.Contain(OnlineGroundPassAuthority.FinalThirdForfeitAction));
            Submit(state.owner=="Home"?"home":"away",OnlineGroundPassAuthority.FinalThirdForfeitAction);
            yield return null;
        }
        yield return Until(()=>authority.Capture().stage=="final-third.keeper-choice","Final Third reaches keeper distribution");
        Submit("home",branch=="kick"?OnlineGroundPassAuthority.FinalThirdKickAction:OnlineGroundPassAuthority.FinalThirdDropAction);
        if(branch=="kick")yield return Until(()=>match.highPassManager.isActivated,"keeper high-kick targeting");
        else yield return Until(()=>!match.finalThirdManager.isActivated,"keeper drops ball and resumes play");
    }
}
