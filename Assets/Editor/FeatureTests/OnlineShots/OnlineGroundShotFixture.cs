using System.Collections;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunOnlineShotPreviewInterruption(int side,bool finalThird)
    {
        var authority=OnlineGroundBoard(side,true,false,out var protocol,out var receiver);
        Invoke(grid,"LoadPrecomputedShootingPaths");Place(attacker,Hex(13*side));Place(receiver,Hex(14*side,-3));
        ball.PlaceAtCell(attacker.currentHex);match.SetLastToken(attacker);match.UpdatePossessionAfterPass(attacker.currentHex);
        match.RefreshAvailableActionsForCurrentState();AttachAuthoredPanels();
        var shot=match.shotManager;using var pause=new OnlineSharedPause(protocol,match);pause.Capture(authority);
        OnlineAccepted(authority,"home",1,OnlineGroundPassAuthority.ShotStartAction);
        var old=OnlineGroundCommand(authority.Capture(),2,OnlineGroundPassAuthority.ShotConfirmAction);
        if(finalThird)
        {
            match.finalThirdManager.TriggerFinalThirdPhase();
            yield return Until(()=>match.finalThirdManager.isActivated,"real Final Third interrupts uncommitted shot preview");
            Assert.That(authority.Capture().stage,Does.StartWith("final-third."));
        }
        else ball.PlaceAtCell(Hex(13*side,3));
        Assert.That(authority.Capture().legalActions,Does.Not.Contain(OnlineGroundPassAuthority.ShotConfirmAction));
        Assert.That(shot.TryExecuteOnlineShot(OnlineShotIntent.Confirm),Is.False);
        Assert.That(authority.Submit("home",old).Status,Is.Not.EqualTo(OnlineCommandStatus.Accepted));
        Assert.That(shot.isActivated,Is.False);Assert.That(match.TimingRollSequence,Is.Zero);
        Assert.That(match.gameData.stats.GetPlayerStats(attacker).shotsAttempted,Is.Zero);
    }

    public IEnumerator RunOnlineShotAfterPass(int side)
    {
        var authority=OnlineGroundBoard(side,true,false,out var protocol,out var receiver);
        Invoke(grid,"LoadPrecomputedShootingPaths");
        Place(attacker,Hex(13*side,-2));Place(receiver,Hex(13*side));receiver.shooting=3;
        ball.PlaceAtCell(attacker.currentHex);match.SetLastToken(attacker);match.UpdatePossessionAfterPass(attacker.currentHex);
        match.RefreshAvailableActionsForCurrentState();AttachAuthoredPanels();duelPanel.resultSeconds=.1f;
        using var pause=new OnlineSharedPause(protocol,match);pause.Capture(authority);
        void Submit(string connection,string action)=>OnlineAccepted(authority,connection,
            protocol.LastAcceptedSequence(connection=="home"?OnlineTeam.Home:OnlineTeam.Away)+1,action);
        Submit("home",OnlineGroundPassAuthority.StartAction);
        string target=OnlineGroundPassAuthority.TargetAction(13*side,0);Submit("home",target);Submit("home",target);
        yield return Until(()=>!ground.isActivated && match.LastTokenToTouchTheBallOnPurpose==receiver,"real pass collection before snapshot continuation");
        for(int i=0;i<8 && match.finalThirdManager.isActivated;i++)
        {
            yield return Until(()=>!match.finalThirdManager.isActivated || authority.Capture().legalActions.Contains(OnlineGroundPassAuthority.FinalThirdForfeitAction),"post-pass Final Third decision");
            if(!match.finalThirdManager.isActivated)break;
            var turn=authority.Capture();Submit(turn.owner=="Home"?"home":"away",OnlineGroundPassAuthority.FinalThirdForfeitAction);yield return null;
        }
        yield return Until(()=>!match.finalThirdManager.isActivated,"post-pass Final Third completion");
        Assert.That(ground.isAvailable,Is.False);Assert.That(match.shotManager.isAvailable && movement.isAvailable,Is.True);
        var state=authority.Capture();Assert.That(state.legalActions,Does.Contain(OnlineGroundPassAuthority.ShotStartAction));
        Assert.That(state.legalActions,Does.Contain(OnlineGroundPassAuthority.MovementStartAction));
        Assert.That(state.legalActions,Does.Not.Contain(OnlineGroundPassAuthority.StartAction));
        Submit("home",OnlineGroundPassAuthority.ShotStartAction);Submit("home",OnlineGroundPassAuthority.ShotConfirmAction);
        Assert.That(match.shotManager.shotType,Is.EqualTo("snapshot"));
        Assert.That(authority.Capture().stage,Is.EqualTo("shot.snapshot.select"));
    }

    public IEnumerator RunOnlineGroundShot(int side,bool snapshot,bool automatic)
    {
        var authority=OnlineGroundBoard(side,true,false,out var protocol,out _);
        Invoke(grid,"LoadPrecomputedShootingPaths");
        Place(attacker,Hex(13*side));Place(defender,Hex(8*side,10));Place(awayGK,Hex(16*side));
        ball.PlaceAtCell(attacker.currentHex);match.SetLastToken(attacker);match.UpdatePossessionAfterPass(attacker.currentHex);
        attacker.shooting=3;awayGK.saving=6;awayGK.handling=4;
        if(snapshot)match.BroadcastAnyOtherScenario();else match.BroadcastSafeEndofMovementPhase(countMovementAction:false,triggerFinalThird:false);
        AttachAuthoredPanels();duelPanel.resultSeconds=.1f;match.isClockRunning=true;
        var shot=match.shotManager;using var pause=new OnlineSharedPause(protocol,match);pause.Capture(authority);
        void Submit(string connection,string action)=>OnlineAccepted(authority,connection,
            protocol.LastAcceptedSequence(connection=="home"?OnlineTeam.Home:OnlineTeam.Away)+1,action);
        Assert.That(shot.isAvailable,Is.True,"real phase availability offers this shot");
        Assert.That(authority.Capture().legalActions,Does.Contain("shot.start"));Submit("home","shot.start");
        Assert.That(authority.Capture().stage,Is.EqualTo("shot.confirm"));
        if(!automatic)
        {
            Submit("home","shot.cancel");Assert.That(shot.isActivated,Is.False);
            Submit("home","shot.start");
        }
        Submit("home","shot.confirm");
        Assert.That(shot.shotType,Is.EqualTo(snapshot?"snapshot":"fullPower"));
        if(snapshot)
        {
            Assert.That(authority.Capture().stage,Is.EqualTo("shot.snapshot.select"));
            Submit("away","shot.player:"+OnlineRollPublication.ActorId(defender));
            Assert.That(authority.Capture().stage,Is.EqualTo("shot.snapshot.target"));
            Submit("away","shot.move:"+(8*side)+":9");
            yield return Until(()=>shot.isWaitingForTargetSelection,"real snapshot defender movement reaches aiming");
            Assert.That(defender.currentHex,Is.SameAs(Hex(8*side,9)));
        }
        Assert.That(authority.Capture().stage,Is.EqualTo("shot.target"));
        var invalid=OnlineGroundCommand(authority.Capture(),protocol.LastAcceptedSequence(OnlineTeam.Home)+1,"shot.target:0:0");
        Assert.That(authority.Submit("home",invalid).Status,Is.EqualTo(OnlineCommandStatus.IllegalAction));
        Submit("home","shot.target:"+(19*side)+":0");
        yield return Until(()=>authority.Capture().stage=="shot.roll","aimed shot reaches shooter roll");
        void Roll(string connection,string action,int face)
        {
            RigOnlineLongRoll(face);
            if(automatic)match.AdvanceOnlineAuthorityTime(DecisionTimingConfig.AutomaticRollSeconds+.1f);
            else Submit(connection,action);
        }
        var old=OnlineGroundCommand(authority.Capture(),protocol.LastAcceptedSequence(OnlineTeam.Home)+1,"shot.roll");
        Roll("home","shot.roll",2);
        Assert.That(shot.totalShotPower,Is.EqualTo(snapshot?4:5));
        Assert.That(authority.Submit("home",old).Status,Is.Not.EqualTo(OnlineCommandStatus.Accepted));
        yield return Until(()=>authority.Capture().stage=="shot.save","on-target shot reaches keeper");
        Assert.That(authority.Capture().owner,Is.EqualTo("Away"));Roll("away","shot.save",2);
        yield return Until(()=>authority.Capture().stage=="shot.handling","keeper save reaches handling");Roll("away","shot.handling",1);
        yield return Until(()=>authority.Capture().stage=="keeper.distribution","held ordinary shot reaches owned distribution");
        Assert.That(match.TimingRollSequence,Is.EqualTo(3));Assert.That(match.LastTokenToTouchTheBallOnPurpose,Is.SameAs(awayGK));
        Assert.That(match.gameData.stats.GetPlayerStats(attacker).shotsAttempted,Is.EqualTo(1));
        Assert.That(match.gameData.stats.GetPlayerStats(awayGK).attemptsSaved,Is.EqualTo(1));
        Assert.That(match.gameData.stats.homeTeamStats.totalGoals,Is.Zero);
        Submit("away",OnlineGroundPassAuthority.KeeperQuickAction);yield return null;
    }
}
