using System.Collections;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;

public sealed partial class GoalkeeperBoxEntryFixture
{
    private OnlineGroundPassState onlineFtpPrecedingPass;
    public IEnumerator RunOnlineFirstTimePass(int side,string branch)
    {
        var authority=OnlineGroundBoard(side,true,false,out var protocol,out var passer);
        Subscribe(ftp);AttachAuthoredPanels();duelPanel.resultSeconds=.1f;
        using var pause=new OnlineSharedPause(protocol,match);pause.Capture(authority);
        void Submit(string connection,string action)=>OnlineAccepted(authority,connection,
            protocol.LastAcceptedSequence(connection=="home"?OnlineTeam.Home:OnlineTeam.Away)+1,action);
        Submit("home","ground.start");Submit("home","ground.target:0:6");Submit("home","ground.target:0:6");
        yield return Until(()=>!ground.isActivated && match.LastTokenToTouchTheBallOnPurpose==passer,"real pass offers First Time Pass continuation");
        onlineFtpPrecedingPass=authority.Capture();
        Assert.That(ftp.isAvailable,Is.True);
        bool danger=branch is "miss" or "hit" or "automatic" or "cancel" or "hold";
        if(danger)Place(defender,Hex(side,3));
        Submit("home","ftp.start");
        Assert.That(authority.Capture().stage,Is.EqualTo("ftp.target"));
        Submit("home","ftp.target:0:0");
        Assert.That(ftp.currentTargetHex,Is.SameAs(attacker.currentHex));Assert.That(ftp.isAwaitingTargetSelection,Is.True);
        Submit("home","ftp.target:0:0");
        Assert.That(authority.Capture().stage,Is.EqualTo("ftp.attacker"));
        var state=authority.Capture();
        Assert.That(authority.Submit("away",OnlineGroundCommand(state,1,"ftp.skip")).Status,Is.EqualTo(OnlineCommandStatus.WrongTeam));
        if(branch=="move")
        {
            var support=Token("FTP support",true,false,7);Place(support,Hex(-4*side,-4));
            Submit("home","ftp.player:"+OnlineRollPublication.ActorId(support));
            Submit("home","ftp.move:"+(-4*side)+":-3");
            yield return Until(()=>authority.Capture().stage=="ftp.defender","attacker preparation move completes");
            Assert.That(support.currentHex,Is.SameAs(Hex(-4*side,-3)));
            Submit("away","ftp.player:"+OnlineRollPublication.ActorId(defender));
            Submit("away","ftp.move:"+(8*side)+":9");
        }
        else { Submit("home","ftp.skip");Assert.That(authority.Capture().owner,Is.EqualTo("Away"));Submit("away","ftp.skip"); }
        if(danger)
        {
            yield return Until(()=>authority.Capture().stage=="ftp.roll","real post-movement interception");
            state=authority.Capture();Assert.That(state.owner,Is.EqualTo("Away"));
            Assert.That(state.selectedPlayer,Is.EqualTo(OnlineRollPublication.ActorId(defender)));
            var late=OnlineGroundCommand(state,protocol.LastAcceptedSequence(OnlineTeam.Away)+1,"ftp.roll");
            RigOnlineLongRoll(branch=="hit"?6:1);
            if(branch=="automatic")match.AdvanceOnlineAuthorityTime(DecisionTimingConfig.AutomaticRollSeconds+.1f);
            else Submit("away","ftp.roll");
            Assert.That(authority.Submit("away",late).Status,Is.Not.EqualTo(OnlineCommandStatus.Accepted));
            if(branch=="cancel")
            {
                ftp.CleanUpFTP();yield return new WaitForSecondsRealtime(.3f);
                Assert.That(match.LastTokenToTouchTheBallOnPurpose,Is.SameAs(passer));
                Assert.That(match.gameData.stats.GetPlayerStats(passer).passesCompleted,Is.Zero);yield break;
            }
            if(branch=="hold")yield return HoldOnlineHeaderContinuation(pause,protocol,()=>ftp.isActivated && match.TimingRollSequence==1);
        }
        var receiver=branch=="hit"?defender:attacker;
        yield return Until(()=>!ftp.isActivated && match.LastTokenToTouchTheBallOnPurpose==receiver,"FTP resolves through real possession and collection");
        Assert.That(ball.GetCurrentHex(),Is.SameAs(receiver.currentHex));
        Assert.That(match.TimingRollSequence,Is.EqualTo(danger?1:0));
        Assert.That(match.gameData.stats.GetPlayerStats(passer).passesAttempted,Is.EqualTo(1));
        Assert.That(match.gameData.stats.GetPlayerStats(passer).passesCompleted,Is.EqualTo(branch=="hit"?0:1));
        if(danger)Assert.That(match.CaptureOnlineRolls()[0].actorId,Is.EqualTo(OnlineRollPublication.ActorId(defender)));
    }
}
