using System.Collections;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
public sealed partial class OnlineRoomActionSceneFixture
{
    public IEnumerator RunRoomSetPiece(int side)
    {
        host=new OnlineRoomSession("room-set-piece",false);
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode(OnlineRoomProofAuthoring.ScenePath,new LoadSceneParameters(LoadSceneMode.Single));
        board=Object.FindAnyObjectByType<OnlineGroundPassBoard>();
        if(side==-1)for(int i=0;i<board.initialCoordinates.Length;i++)board.initialCoordinates[i].x*=-1;
        board.initialCoordinates[1]=new Vector3Int(8*side,0,10);
        board.InitializeAuthority();
        board.match.homeTeamDirection=side==1?MatchManager.TeamAttackingDirection.LeftToRight:MatchManager.TeamAttackingDirection.RightToLeft;
        board.match.awayTeamDirection=side==1?MatchManager.TeamAttackingDirection.RightToLeft:MatchManager.TeamAttackingDirection.LeftToRight;
        var version=new OnlineCompatibility("room-set-piece","rules","catalog");
        var protocol=new OnlineMatchProtocol("room-set-piece","home","away","away",version);
        protocol.BindAuthenticatedAccount("home","home",version);protocol.BindAuthenticatedAccount("away","away",version);
        var authority=new OnlineGroundPassAuthority(board.match,protocol);
        using var pause=new OnlineSharedPause(protocol,board.match);
        void Submit(string action)
        {
            var state=pause.Capture(authority);Assert.That(state.legalActions,Does.Contain(action),state.stage);
            var team=state.owner=="Home"?OnlineTeam.Home:OnlineTeam.Away;
            Assert.That(authority.Submit(team==OnlineTeam.Home?"home":"away",new OnlineMatchCommand(state.matchId,protocol.LastAcceptedSequence(team)+1,state.revision,state.decisionId,action)).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
        }
        IEnumerator Wait(System.Func<bool> ready,string reason)
        {
            double deadline=Time.realtimeSinceStartupAsDouble+15;
            while(!ready() && Time.realtimeSinceStartupAsDouble<deadline)yield return null;
            Assert.That(ready(),Is.True,reason);
        }
        var fk=board.match.freeKickManager;fk.StartFreeKickPreparation();Submit("set-piece.skip");
        yield return Wait(()=>fk.isWaitingForSetupPhase,"Room initial setup");
        for(int i=0;i<16 && !fk.isWaitingForExecution;i++)
        {
            var state=board.match.currentState;
            if(fk.isWaitingForFinalKickerSelection)Submit("set-piece.player:home:9");
            else Submit("set-piece.skip");
            yield return Wait(()=>board.match.currentState!=state,"Room setup advances");
        }
        Assert.That(fk.isWaitingForExecution,Is.True);Submit("set-piece.ground");
        yield return Wait(()=>board.match.groundBallManager.isAwaitingTargetSelection,"Room free kick ground target");
        Submit("ground.target:0:6");Submit("ground.target:0:6");
        yield return Wait(()=>!board.match.groundBallManager.isActivated && board.match.LastTokenToTouchTheBallOnPurpose==board.tokens.Single(t=>t.isHomeTeam && t.jerseyNumber==10),"Room awarded restart completes pass");
        var final=authority.Capture();Assert.That(final.tokens.Single(t=>t.id=="home:9").passesCompleted,Is.EqualTo(1));
        Assert.That(final.ballZ,Is.EqualTo(6));
    }
}
