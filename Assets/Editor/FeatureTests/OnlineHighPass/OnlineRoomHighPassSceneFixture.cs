using System.Collections;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed partial class OnlineRoomActionSceneFixture
{
    public IEnumerator RunRoomHighPass(int side)
    {
        host=new OnlineRoomSession("room-high",false);
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode(OnlineRoomProofAuthoring.ScenePath,new LoadSceneParameters(LoadSceneMode.Single));
        board=Object.FindAnyObjectByType<OnlineGroundPassBoard>();
        if(side==-1)for(int i=0;i<board.initialCoordinates.Length;i++)board.initialCoordinates[i].x*=-1;
        board.InitializeAuthority();
        board.match.homeTeamDirection=side==1?MatchManager.TeamAttackingDirection.LeftToRight:MatchManager.TeamAttackingDirection.RightToLeft;
        board.match.awayTeamDirection=side==1?MatchManager.TeamAttackingDirection.RightToLeft:MatchManager.TeamAttackingDirection.LeftToRight;
        var version=new OnlineCompatibility("room-high","rules","catalog");
        var protocol=new OnlineMatchProtocol("room-high","home","away","away",version);
        protocol.BindAuthenticatedAccount("home","home",version);protocol.BindAuthenticatedAccount("away","away",version);
        var authority=new OnlineGroundPassAuthority(board.match,protocol);
        using var pause=new OnlineSharedPause(protocol,board.match);
        void Submit(string connection,string action)
        {
            var state=pause.Capture(authority);Assert.That(state.legalActions,Does.Contain(action),state.stage);
            var team=connection=="home"?OnlineTeam.Home:OnlineTeam.Away;
            Assert.That(authority.Submit(connection,new OnlineMatchCommand(state.matchId,protocol.LastAcceptedSequence(team)+1,state.revision,state.decisionId,action)).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
        }
        IEnumerator Wait(string stage)
        {
            double deadline=Time.realtimeSinceStartupAsDouble+20;
            while(authority.Capture().stage!=stage && Time.realtimeSinceStartupAsDouble<deadline)yield return null;
            Assert.That(authority.Capture().stage,Is.EqualTo(stage));
        }
        void Rig(int face)
        {
            int seed=0;for(;seed<1000;seed++){Random.InitState(seed);if(board.match.helperFunctions.DiceRoll()._roll==face)break;}
            Assert.That(seed,Is.LessThan(1000));Random.InitState(seed);
        }
        board.tokens[0].highPass=4;board.match.movementPhaseManager.duelResolutionPanel.resultSeconds=.1f;
        Submit("home",OnlineGroundPassAuthority.HighStartAction);
        Submit("home",OnlineGroundPassAuthority.HighTargetAction(0,6));Submit("home",OnlineGroundPassAuthority.HighTargetAction(0,6));
        yield return Wait("high.attacker");Submit("home",OnlineGroundPassAuthority.HighSkipAction);Submit("away",OnlineGroundPassAuthority.HighSkipAction);
        Rig(5);Submit("home",OnlineGroundPassAuthority.HighRollAction);
        yield return Wait("header.control.choice");Submit("home",OnlineGroundPassAuthority.HeaderControlAction);
        Rig(6);Submit("home",OnlineGroundPassAuthority.HeaderControlRollAction);
        double completed=Time.realtimeSinceStartupAsDouble+10;
        while(board.match.highPassManager.headerManager.isActivated && Time.realtimeSinceStartupAsDouble<completed)yield return null;
        Assert.That(board.match.highPassManager.headerManager.isActivated,Is.False);
        Assert.That(board.match.LastTokenToTouchTheBallOnPurpose.jerseyNumber,Is.EqualTo(10));
        Assert.That(board.match.TimingRollSequence,Is.EqualTo(2));Assert.That(board.match.totalCompletedActions,Is.EqualTo(2));
        Assert.That(authority.Capture().tokens.Single(t=>t.id=="home:9").aerialPassesCompleted,Is.EqualTo(1));
    }
}
