using System.Collections;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed partial class OnlineRoomActionSceneFixture
{
    public IEnumerator RunRoomLongPass(int side)
    {
        host=new OnlineRoomSession("room-long",false);
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode(OnlineRoomProofAuthoring.ScenePath,new LoadSceneParameters(LoadSceneMode.Single));
        board=Object.FindAnyObjectByType<OnlineGroundPassBoard>();
        if(side==-1) foreach(var index in System.Linq.Enumerable.Range(0,board.initialCoordinates.Length)) board.initialCoordinates[index].x*=-1;
        board.InitializeAuthority();
        board.match.homeTeamDirection=side==1?MatchManager.TeamAttackingDirection.LeftToRight:MatchManager.TeamAttackingDirection.RightToLeft;
        board.match.awayTeamDirection=side==1?MatchManager.TeamAttackingDirection.RightToLeft:MatchManager.TeamAttackingDirection.LeftToRight;
        var version=new OnlineCompatibility("room-long","rules","catalog");
        var protocol=new OnlineMatchProtocol("room-long","home","away","away",version);
        protocol.BindAuthenticatedAccount("home","home",version);protocol.BindAuthenticatedAccount("away","away",version);
        var authority=new OnlineGroundPassAuthority(board.match,protocol);
        using var pause=new OnlineSharedPause(protocol,board.match);
        long sequence=0;
        void Submit(string action,string connection="home")
        {
            var state=pause.Capture(authority);
            Assert.That(state.legalActions,Does.Contain(action),state.stage);
            Assert.That(authority.Submit(connection,new OnlineMatchCommand(state.matchId,++sequence,state.revision,state.decisionId,action)).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
        }
        board.tokens[0].highPass=4;
        var panel=board.match.movementPhaseManager.duelResolutionPanel;
        Assert.That(panel!=null && panel.HasSceneBindings,Is.True);panel.resultSeconds=.1f;
        Submit(OnlineGroundPassAuthority.LongStartAction);
        Submit(OnlineGroundPassAuthority.LongTargetAction(8*side,-6));
        Submit(OnlineGroundPassAuthority.LongTargetAction(8*side,-6));
        int seed=0;
        for(;seed<1000;seed++) {Random.InitState(seed);if(board.match.helperFunctions.DiceRoll()._roll==5)break;}
        Assert.That(seed,Is.LessThan(1000));Random.InitState(seed);
        Submit(OnlineGroundPassAuthority.LongRollAction);
        double deadline=Time.realtimeSinceStartupAsDouble+15;
        while(authority.Capture().stage!="long.keeper" && Time.realtimeSinceStartupAsDouble<deadline) yield return null;
        Assert.That(authority.Capture().stage,Is.EqualTo("long.keeper"));
        Submit(OnlineGroundPassAuthority.LongKeeperDeclineAction,"away");
        while(board.match.longBallManager.isActivated && Time.realtimeSinceStartupAsDouble<deadline) yield return null;
        Assert.That(board.match.longBallManager.isActivated,Is.False);
        Assert.That(board.match.ball.GetCurrentHex().coordinates,Is.EqualTo(new Vector3Int(8*side,0,-6)));
        Assert.That(board.match.TimingRollSequence,Is.EqualTo(1));Assert.That(board.match.totalCompletedActions,Is.EqualTo(1));
    }
}
