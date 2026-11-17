using System;
using System.Collections;
using System.Reflection;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;
public sealed partial class OnlineFullRoomSceneFixture
{
    private IEnumerator RunOffTargetCamera(OnlineGroundPassBoard board,OnlineMatchProtocol protocol,OnlineGroundPassAuthority authority,string branch)
    {
        var match=board.match;var shot=match.shotManager;var shooter=shot.shooter;var origin=match.ball.GetCurrentHex();var start=match.ball.transform.position;
        Assert.That(shot.enabled,Is.False);Assert.That(match.ball.enabled,Is.False);
        int seed=0;
        for(;seed<10000;seed++)
        {
            UnityEngine.Random.InitState(seed);var first=match.helperFunctions.DiceRoll();int next=UnityEngine.Random.Range(1,7);
            if(first._roll==1 && !first._isJackPot && (next==3 || next==4))break;
        }
        Assert.That(seed,Is.LessThan(10000));UnityEngine.Random.InitState(seed);
        var state=authority.Capture();Assert.That(state.stage,Is.EqualTo("shot.roll"));
        Assert.That(authority.Submit("a",OnlineGroundPassMessage.Command(state,protocol.LastAcceptedSequence(OnlineTeam.Away)+1,OnlineGroundPassAuthority.ShotRollAction).ToCommand()).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
        float deadline=Time.realtimeSinceStartup+15;
        while(match.TimingRollSequence<2 || Vector3.Distance(start,match.ball.transform.position)<.1f)
        {Assert.That(Time.realtimeSinceStartup,Is.LessThan(deadline),"Real roll must reach the camera miss branch");yield return null;}
        var rolls=match.CaptureOnlineRolls();Assert.That(rolls.Length,Is.EqualTo(2));Assert.That(rolls[0].context,Is.EqualTo("shot_roll"));Assert.That(rolls[0].roll,Is.EqualTo(1));
        Assert.That(rolls[1].context,Is.EqualTo("shot_off_target_randomizer"));Assert.That(rolls[1].roll,Is.InRange(3,4));
        if(branch.EndsWith("-hold"))
        {
            var previous=match.ball.transform.position;float stationary=0;deadline=Time.realtimeSinceStartup+3;
            while(stationary<.1f)
            {
                Assert.That(Time.realtimeSinceStartup,Is.LessThan(deadline));yield return null;var current=match.ball.transform.position;
                stationary=(current-previous).sqrMagnitude<.00000001f?stationary+Time.unscaledDeltaTime:0;previous=current;
            }
        }
        state=pause.Capture(authority);
        if(branch=="flight")
        {
            Assert.That(state.ballMoving,Is.True,"Direct camera animation must use the live presentation lane");Assert.That(state.roomPose,Is.Not.Null);
            Assert.That(Vector3.Distance(new Vector3(state.roomPose.ballX,state.roomPose.ballY,state.roomPose.ballZ),match.ball.transform.position),Is.LessThan(.001f));
        }
        bool reset=branch.StartsWith("reset");
        if(branch.StartsWith("pause") || branch.StartsWith("hold"))
        {
            bool transport=branch.StartsWith("hold");
            if(transport)pause.SetConnectionHold(true);
            else Assert.That(pause.Submit("a",new OnlineMatchCommand(state.matchId,1,pause.Revision,"shared-pause",OnlineSharedPause.RequestAction)).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
            var frozen=match.ball.transform.position;float wait=branch.EndsWith("-hold")?2.3f:.7f;float end=Time.realtimeSinceStartup+wait;
            while(Time.realtimeSinceStartup<end)yield return null;
            Assert.That(Vector3.Distance(match.ball.transform.position,frozen),Is.LessThan(.001f),"Pause/connection hold freezes flight and camera hold timer");
            Assert.That(match.teamInAttack,Is.EqualTo(MatchManager.TeamInAttack.Away),"Restart cannot begin while held");
            Assert.That(pause.Capture(authority).legalActions,Is.Empty);
            if(transport)pause.SetConnectionHold(false);
            else Assert.That(pause.Submit("a",new OnlineMatchCommand(state.matchId,2,pause.Revision,"shared-pause",OnlineSharedPause.ResumeAction)).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
        }
        if(reset)
        {
            typeof(ShotManager).GetMethod("ResetShotProcess",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(shot,null);match.ball.PlaceAtCell(origin);
            var restored=match.ball.transform.position;float end=Time.realtimeSinceStartup+2.7f;while(Time.realtimeSinceStartup<end)yield return null;
            Assert.That(Vector3.Distance(match.ball.transform.position,restored),Is.LessThan(.001f),"Retired shot coroutine cannot overwrite the next ball position");
            Assert.That(match.teamInAttack,Is.EqualTo(MatchManager.TeamInAttack.Away),"Retired camera miss cannot award a late restart");
            Assert.That(shot.isActivated,Is.False);
        }
        else
        {
            deadline=Time.realtimeSinceStartup+15;
            OnlineGroundPassState restart;
            while(true)
            {
                restart=authority.Capture();
                if(match.teamInAttack==MatchManager.TeamInAttack.Home && match.finalThirdManager.isActivated && restart.stage.StartsWith("final-third.") && restart.legalActions.Length>0)break;
                Assert.That(Time.realtimeSinceStartup,Is.LessThan(deadline),"Miss must publish playable defending goal-kick Final Third choices");yield return null;
            }
            Assert.That(restart.stage,Does.StartWith("final-third."));Assert.That(restart.legalActions,Is.Not.Empty);
        }
        var player=match.gameData.stats.GetPlayerStats(shooter);Assert.That(player.shotsAttempted,Is.EqualTo(1));Assert.That(player.shotsOnTarget,Is.Zero);Assert.That(player.goals,Is.Zero);
        Assert.That(match.gameData.stats.homeTeamStats.totalGoals,Is.Zero);Assert.That(match.gameData.stats.awayTeamStats.totalGoals,Is.Zero);Assert.That(match.TimingRollSequence,Is.EqualTo(2));
    }
}
