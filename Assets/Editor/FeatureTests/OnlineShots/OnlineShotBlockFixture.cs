using System.Collections;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunOnlineShotBlock(int side,string branch)
    {
        var authority=OnlineGroundBoard(side,true,false,out var protocol,out _);
        Invoke(grid,"LoadPrecomputedShootingPaths");Place(attacker,Hex(13*side));Place(awayGK,Hex(16*side));
        var target=Hex(19*side);var blockingHex=attacker.currentHex.ShootingPaths[target].First(h=>h.coordinates.x==15*side);
        Place(defender,blockingHex);ball.PlaceAtCell(attacker.currentHex);match.SetLastToken(attacker);match.UpdatePossessionAfterPass(attacker.currentHex);
        attacker.shooting=3;defender.tackling=4;awayGK.saving=6;awayGK.handling=4;
        match.RefreshAvailableActionsForCurrentState();AttachAuthoredPanels();duelPanel.resultSeconds=.1f;match.isClockRunning=true;
        var shot=match.shotManager;using var pause=new OnlineSharedPause(protocol,match);pause.Capture(authority);
        void Submit(string connection,string action)=>OnlineAccepted(authority,connection,
            protocol.LastAcceptedSequence(connection=="home"?OnlineTeam.Home:OnlineTeam.Away)+1,action);
        Submit("home",OnlineGroundPassAuthority.ShotStartAction);Submit("home",OnlineGroundPassAuthority.ShotConfirmAction);
        Submit("home",OnlineGroundPassAuthority.ShotTargetAction(19*side,0));
        yield return Until(()=>shot.isWaitingForBlockDiceRoll,"real outfield shot block before shooter roll");
        var state=authority.Capture();Assert.That(state.stage,Is.EqualTo("shot.block"));Assert.That(state.owner,Is.EqualTo("Away"));
        Assert.That(state.selectedPlayer,Is.EqualTo(OnlineRollPublication.ActorId(defender)));
        bool blocked=branch is "block" or "hold";
        var late=OnlineGroundCommand(state,protocol.LastAcceptedSequence(OnlineTeam.Away)+1,"shot.block");
        RigOnlineLongRoll(blocked?6:1);
        if(branch=="automatic")match.AdvanceOnlineAuthorityTime(DecisionTimingConfig.AutomaticRollSeconds+.1f);
        else Submit("away","shot.block");
        Assert.That(authority.Capture().legalActions,Does.Not.Contain("shot.block"),"block admission claims the one-frame roll before its dice draw");
        Assert.That(authority.Submit("away",late).Status,Is.Not.EqualTo(OnlineCommandStatus.Accepted));
        if(branch=="cancel")
        {
            Invoke(shot,"ResetShotProcess");yield return new WaitForSecondsRealtime(.2f);
            Assert.That(match.TimingRollSequence,Is.Zero);Assert.That(match.gameData.stats.GetPlayerStats(defender).shotBlocksAttempted,Is.Zero);yield break;
        }
        yield return Until(()=>match.TimingRollSequence==1,"accepted shot block draws once");
        Assert.That(match.CaptureOnlineRolls()[0].context,Is.EqualTo("shot_block"));
        Assert.That(match.CaptureOnlineRolls()[0].actorId,Is.EqualTo(OnlineRollPublication.ActorId(defender)));
        if(branch=="hold")yield return HoldOnlineHeaderContinuation(pause,protocol,()=>match.TimingRollSequence==1 && (shot.isActivated || loose.isActivated));
        if(blocked)
        {
            yield return Until(()=>authority.Capture().stage=="loose.direction","blocked shot enters real loose ball");
            RigOnlineLongRoll(4);Submit("away",OnlineGroundPassAuthority.LooseRollAction);
            yield return Until(()=>authority.Capture().stage=="loose.distance","blocked-shot scatter distance");
            RigOnlineLongRoll(2);Submit("away",OnlineGroundPassAuthority.LooseRollAction);
            yield return Until(()=>!shot.isActivated && !loose.isActivated,"blocked shot completes its scatter",120);
            Assert.That(match.TimingRollSequence,Is.EqualTo(3));
            Assert.That(match.gameData.stats.GetPlayerStats(defender).shotBlocksMade,Is.EqualTo(1));
            Assert.That(match.gameData.stats.GetPlayerStats(attacker).shotsBlocked,Is.EqualTo(1));
        }
        else
        {
            yield return Until(()=>authority.Capture().stage=="shot.roll","failed block continues to shooter");
            void Roll(string connection,string action,int face)
            {
                RigOnlineLongRoll(face);
                if(branch=="automatic")match.AdvanceOnlineAuthorityTime(DecisionTimingConfig.AutomaticRollSeconds+.1f);
                else Submit(connection,action);
            }
            Roll("home",OnlineGroundPassAuthority.ShotRollAction,2);
            yield return Until(()=>authority.Capture().stage=="shot.save","shot after failed block reaches keeper");Roll("away",OnlineGroundPassAuthority.ShotSaveAction,2);
            yield return Until(()=>authority.Capture().stage=="shot.handling","shot after failed block reaches handling");Roll("away",OnlineGroundPassAuthority.ShotHandlingAction,1);
            yield return Until(()=>authority.Capture().stage=="keeper.distribution","failed-block shot completes save/hold");
            Assert.That(match.TimingRollSequence,Is.EqualTo(4));Assert.That(match.gameData.stats.GetPlayerStats(defender).shotBlocksMade,Is.Zero);
            Submit("away",OnlineGroundPassAuthority.KeeperQuickAction);yield return null;
        }
        Assert.That(match.gameData.stats.GetPlayerStats(defender).shotBlocksAttempted,Is.EqualTo(1));
        Assert.That(match.gameData.stats.GetPlayerStats(attacker).shotsAttempted,Is.EqualTo(1));
        Assert.That(match.gameData.stats.homeTeamStats.totalGoals,Is.Zero);
    }
}
