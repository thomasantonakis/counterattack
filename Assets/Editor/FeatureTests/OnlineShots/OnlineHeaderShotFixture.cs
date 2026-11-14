using System.Collections;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunOnlineHeaderShot(int side,string branch)
    {
        var authority=OnlineGroundBoard(side,true,false,out var protocol,out _);
        Invoke(grid,"LoadPrecomputedShootingPaths");
        Place(attacker,Hex(13*side));Place(defender,Hex(13*side,1));Place(awayGK,Hex(16*side,3));
        ball.PlaceAtCell(attacker.currentHex);match.SetLastToken(attacker);
        attacker.heading=4;defender.heading=0;awayGK.saving=4;awayGK.handling=4;
        AttachAuthoredPanels();duelPanel.resultSeconds=.1f;
        var header=match.highPassManager.headerManager;header.headerChallengePanel.resultSeconds=.1f;
        var shot=match.shotManager;match.isClockRunning=true;
        using var pause=new OnlineSharedPause(protocol,match);pause.Capture(authority);
        void Submit(string connection,string action)=>OnlineAccepted(authority,connection,
            protocol.LastAcceptedSequence(connection=="home"?OnlineTeam.Home:OnlineTeam.Away)+1,action);
        header.StartCoroutine(header.FindEligibleHeaderTokens(ball.GetCurrentHex()));
        yield return Until(()=>authority.Capture().stage=="header.goal-choice","real header goal-line choice");
        Submit("home",OnlineGroundPassAuthority.HeaderGoalAction(19*side,0));
        yield return Until(()=>header.isWaitingForDefenderSelection,"headed-shot defensive nomination");
        Submit("away",OnlineGroundPassAuthority.HeaderPlayerAction(OnlineRollPublication.ActorId(defender)));
        Submit("away",OnlineGroundPassAuthority.HeaderConfirmAction);
        yield return Until(()=>header.isWaitingForHeaderRoll && header.tokenRolling==attacker,"headed-shot attacking roll");
        RigOnlineLongRoll(3);Submit("home",OnlineGroundPassAuthority.HeaderRollAction);
        yield return Until(()=>header.isWaitingForHeaderRoll && header.tokenRolling==defender,"headed-shot defender roll");
        RigOnlineLongRoll(1);Submit("away",OnlineGroundPassAuthority.HeaderRollAction);
        yield return Until(()=>shot.isWaitingForGKDiceRoll,"winning headed shot reaches keeper save");
        var state=authority.Capture();Assert.That(state.stage,Is.EqualTo("shot.header.save"));Assert.That(state.owner,Is.EqualTo("Away"));
        using(var guest=new OnlineRoomSession("shot-guest",true))
            Assert.That(shot.TryExecuteOnlineShot(OnlineShotIntent.HeaderSave),Is.False);
        Assert.That(authority.Submit("home",OnlineGroundCommand(state,protocol.LastAcceptedSequence(OnlineTeam.Home)+1,"shot.save")).Status,Is.EqualTo(OnlineCommandStatus.WrongTeam));
        // The closest save hex on this real heading path is two steps from the keeper: no distance penalty.
        Assert.That(shot.totalShotPower,Is.EqualTo(7));
        Assert.That(HexGridUtils.GetHexStepDistance(awayGK.currentHex,shot.saveHex),Is.EqualTo(2));
        RigOnlineLongRoll(branch=="tie"?3:6);
        if(branch=="automatic")match.AdvanceOnlineAuthorityTime(DecisionTimingConfig.AutomaticRollSeconds+.1f);
        else Submit("away","shot.save");
        Assert.That(authority.Capture().legalActions,Does.Not.Contain("shot.save"));
        if(branch=="cancel")
        {
            Invoke(shot,"ResetShotProcess");yield return new WaitForSecondsRealtime(.3f);
            Assert.That(shot.isWaitingforHandlingTest || loose.isActivated,Is.False);
            Assert.That(match.gameData.stats.homeTeamStats.totalGoals,Is.Zero);yield break;
        }
        if(branch=="pause")yield return HoldOnlineHeaderContinuation(pause,protocol,()=>shot.isActivated && !shot.isWaitingforHandlingTest && !loose.isActivated);
        if(branch=="tie")
        {
            yield return Until(()=>authority.Capture().stage=="loose.direction","tied headed save enters owned rebound");
            Assert.That(match.TimingRollSequence,Is.EqualTo(3));
            RigOnlineLongRoll(4);Submit("away",OnlineGroundPassAuthority.LooseRollAction);
            yield return Until(()=>authority.Capture().stage=="loose.distance","headed-save rebound distance");
            RigOnlineLongRoll(2);Submit("away",OnlineGroundPassAuthority.LooseRollAction);
            // Finish the real async save/rebound owner before fixture teardown.
            yield return Until(()=>!loose.isActivated && !shot.isActivated,"headed-save rebound completes its shot continuation",120);
            Assert.That(match.TimingRollSequence,Is.EqualTo(5));
            Assert.That(ball.GetCurrentHex(),Is.SameAs(Hex(16*side,3)));
            Assert.That(match.gameData.stats.homeTeamStats.totalGoals,Is.Zero);yield break;
        }
        yield return Until(()=>authority.Capture().stage=="shot.handling","saved header offers handling");
        RigOnlineLongRoll(branch=="spill"?6:1);
        if(branch=="automatic")match.AdvanceOnlineAuthorityTime(DecisionTimingConfig.AutomaticRollSeconds+.1f);
        else Submit("away","shot.handling");
        if(branch=="cancel-handling")
        {
            Invoke(shot,"ResetShotProcess");yield return new WaitForSecondsRealtime(.3f);
            Assert.That(shot.isWaitingForSaveandHoldScenario || loose.isActivated,Is.False);
            Assert.That(match.LastTokenToTouchTheBallOnPurpose,Is.SameAs(attacker));yield break;
        }
        if(branch=="pause-handling")yield return HoldOnlineHeaderContinuation(pause,protocol,()=>shot.isActivated && !shot.isWaitingForSaveandHoldScenario && !loose.isActivated);
        yield return Until(()=>branch=="spill"?authority.Capture().stage=="loose.direction":authority.Capture().stage=="keeper.distribution","header handling continuation");
        Assert.That(match.TimingRollSequence,Is.EqualTo(4));
        var roll=match.CaptureOnlineRolls().Last();Assert.That(roll.context,Is.EqualTo("goalkeeper_handling"));
        Assert.That(roll.actorId,Is.EqualTo(OnlineRollPublication.ActorId(awayGK)));
        Assert.That(roll.jackpotEnabled,Is.False,"handling is a numeric hold/spill roll");
        Assert.That(match.gameData.stats.homeTeamStats.totalGoals,Is.Zero);
        if(branch!="spill")
        {
            Assert.That(match.LastTokenToTouchTheBallOnPurpose,Is.SameAs(awayGK));
            Assert.That(authority.Capture().owner,Is.EqualTo("Away"));
            Submit("away",OnlineGroundPassAuthority.KeeperQuickAction);
            yield return null;Assert.That(ground.isQuickThrow,Is.True,"held header resumes owned distribution");
        }
    }
}
