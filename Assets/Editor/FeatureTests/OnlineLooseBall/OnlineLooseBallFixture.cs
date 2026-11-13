using System.Collections;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunOnlineKeeperLooseBall(int side,bool automatic)
    {
        yield return RunOnlineHeadedBackPass(side,"spill");
        var version=OnlineGroundVersion();
        var protocol=new OnlineMatchProtocol("keeper-spill","home-account","away-account","away-account",version);
        protocol.BindAuthenticatedAccount("home-account","home",version);protocol.BindAuthenticatedAccount("away-account","away",version);
        var authority=new OnlineGroundPassAuthority(match,protocol);
        using var pause=new OnlineSharedPause(protocol,match);pause.Capture(authority);
        var state=authority.Capture();Assert.That(state.stage,Is.EqualTo("loose.direction"));Assert.That(state.owner,Is.EqualTo("Home"));
        Assert.That(state.legalActions,Is.EqualTo(new[]{OnlineGroundPassAuthority.LooseRollAction}));
        void Roll(int value)
        {
            RigOnlineLongRoll(value);
            if(automatic)match.AdvanceOnlineAuthorityTime(DecisionTimingConfig.AutomaticRollSeconds+.1f);
            else OnlineAccepted(authority,"home",protocol.LastAcceptedSequence(OnlineTeam.Home)+1,OnlineGroundPassAuthority.LooseRollAction);
        }
        Roll(4);yield return Until(()=>authority.Capture().stage=="loose.distance","keeper spill distance after direction");
        Roll(2);yield return Until(()=>!loose.isActivated,"keeper spill resumes open play",120);
        Assert.That(ball.GetCurrentHex(),Is.SameAs(Hex(-16*side,2)));
        Assert.That(match.TimingRollSequence,Is.EqualTo(3));
        Assert.That(match.CaptureOnlineRolls().Select(r=>r.context),Is.EqualTo(new[]{"headed_back_pass_catch","loose_ball_direction","loose_ball_distance"}));
        Assert.That(match.CaptureOnlineRolls().All(r=>r.actorId==OnlineRollPublication.ActorId(homeGK)),Is.True);
    }

    public IEnumerator RunOnlineHeaderLooseBall(int side,string branch)
    {
        var authority=OnlineGroundBoard(side,true,false,out var protocol,out var receiver);
        Place(defender,Hex(side,6));ball.PlaceAtCell(receiver.currentHex);
        var recovery=branch is "recover" or "miss" or "automatic" ? Token("Loose recovery",false,false,7):null;
        if(recovery!=null)Place(recovery,Hex(side+1,8));
        AttachAuthoredPanels();duelPanel.resultSeconds=.1f;
        var header=match.highPassManager.headerManager;header.headerChallengePanel.resultSeconds=.1f;
        receiver.heading=defender.heading=4;match.isClockRunning=true;
        using var pause=new OnlineSharedPause(protocol,match);pause.Capture(authority);
        void Submit(string connection,string action)=>OnlineAccepted(authority,connection,
            protocol.LastAcceptedSequence(connection=="home"?OnlineTeam.Home:OnlineTeam.Away)+1,action);
        yield return header.FindEligibleHeaderTokens(ball.GetCurrentHex());
        yield return Until(()=>header.isWaitingForDefenderSelection,"tied-header defender nomination");
        Submit("away",OnlineGroundPassAuthority.HeaderPlayerAction(OnlineRollPublication.ActorId(defender)));
        Submit("away",OnlineGroundPassAuthority.HeaderConfirmAction);
        yield return Until(()=>header.isWaitingForHeaderRoll && header.tokenRolling==receiver,"tie attacker roll");
        RigOnlineLongRoll(3);Submit("home",OnlineGroundPassAuthority.HeaderRollAction);
        yield return Until(()=>header.isWaitingForHeaderRoll && header.tokenRolling==defender,"tie defender roll");
        RigOnlineLongRoll(3);Submit("away",OnlineGroundPassAuthority.HeaderRollAction);
        yield return Until(()=>loose.isWaitingForDirectionRoll,"real tied-header loose-ball direction");
        var state=authority.Capture();Assert.That(state.stage,Is.EqualTo("loose.direction"));Assert.That(state.owner,Is.EqualTo("Away"));
        using(var guest=new OnlineRoomSession("loose-guest",true))
            Assert.That(loose.TryExecuteOnlineLooseBall(OnlineLooseBallIntent.Direction),Is.False);
        Assert.That(authority.Submit("home",OnlineGroundCommand(state,protocol.LastAcceptedSequence(OnlineTeam.Home)+1,"loose.roll")).Status,Is.EqualTo(OnlineCommandStatus.WrongTeam));
        RigOnlineLongRoll(4);
        if(branch=="automatic")match.AdvanceOnlineAuthorityTime(DecisionTimingConfig.AutomaticRollSeconds+.1f);
        else Submit("away","loose.roll");
        yield return Until(()=>authority.Capture().stage=="loose.distance","direction precedes distance");
        var late=OnlineGroundCommand(authority.Capture(),protocol.LastAcceptedSequence(OnlineTeam.Away)+1,"loose.roll");
        RigOnlineLongRoll(2);
        if(branch=="automatic")match.AdvanceOnlineAuthorityTime(DecisionTimingConfig.AutomaticRollSeconds+.1f);
        else Submit("away","loose.roll");
        Assert.That(authority.Capture().legalActions,Does.Not.Contain("loose.roll"));
        Assert.That(authority.Submit("away",late).Status,Is.Not.EqualTo(OnlineCommandStatus.Accepted));
        if(branch=="hold")yield return HoldOnlineHeaderContinuation(pause,protocol,()=>loose.isWaitingForDistanceRoll && loose.isActivated);
        if(branch is "cancel" or "replace")
        {
            var stoppedAt=ball.GetCurrentHex();loose.EndLooseBallPhase();
            if(branch=="replace")
            {
                loose.StartCoroutine(loose.ResolveLooseBall(attacker,LooseBallSourceType.GroundDeflection,allowGKBoxMove:false));
                yield return Until(()=>loose.isActivated && authority.Capture().stage=="loose.direction","replacement loose-ball phase survives old result");
                yield return new WaitForSecondsRealtime(.2f);
                Assert.That(loose.causingDeflection,Is.SameAs(attacker));Assert.That(authority.Capture().owner,Is.EqualTo("Home"));
                Assert.That(match.TimingRollSequence,Is.EqualTo(4));
                RigOnlineLongRoll(4);Submit("home","loose.roll");
                yield return Until(()=>authority.Capture().stage=="loose.distance","replacement direction completes independently");
                RigOnlineLongRoll(1);Submit("home","loose.roll");
                yield return Until(()=>!loose.isActivated,"replacement loose-ball completes");
                Assert.That(match.TimingRollSequence,Is.EqualTo(6));yield break;
            }
            yield return new WaitForSecondsRealtime(.3f);
            Assert.That(loose.isActivated,Is.False);Assert.That(ball.GetCurrentHex(),Is.SameAs(stoppedAt));
            Assert.That(match.TimingRollSequence,Is.EqualTo(4));yield break;
        }
        if(recovery!=null)
        {
            yield return Until(()=>authority.Capture().stage=="loose.recovery","landing recovery after scatter");
            Assert.That(loose.potentialInterceptor,Is.SameAs(recovery));RigOnlineLongRoll(branch=="miss"?1:6);
            if(branch=="automatic")match.AdvanceOnlineAuthorityTime(DecisionTimingConfig.AutomaticRollSeconds+.1f);
            else Submit("away","loose.roll");
        }
        yield return Until(()=>!loose.isActivated,"loose ball resolves collection or space",120);
        bool recovered=recovery!=null && branch!="miss";
        Assert.That(ball.GetCurrentHex(),Is.SameAs(recovered?recovery.currentHex:Hex(side,8)));
        Assert.That(match.TimingRollSequence,Is.EqualTo(recovery==null?4:5));
        Assert.That(match.CaptureOnlineRolls().Count(r=>r.context.StartsWith("loose_ball_")),Is.EqualTo(recovery==null?2:3));
        if(recovered)Assert.That(match.LastTokenToTouchTheBallOnPurpose,Is.SameAs(recovery));
    }
}
