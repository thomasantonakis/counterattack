using System.Collections;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
public sealed partial class OnlineRoomActionSceneFixture
{
    public IEnumerator RunRoomThrowIn(int side,bool head)
    {
        host=new OnlineRoomSession("room-throw-in",false);
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode(OnlineRoomProofAuthoring.ScenePath,new LoadSceneParameters(LoadSceneMode.Single));
        board=Object.FindAnyObjectByType<OnlineGroundPassBoard>();
        if(side==-1)for(int i=0;i<board.initialCoordinates.Length;i++)board.initialCoordinates[i].x*=-1;
        board.initialCoordinates[0]=new Vector3Int(0,0,11);board.initialCoordinates[4]=new Vector3Int(0,0,8);
        board.initialCoordinates[1]=new Vector3Int(8*side,0,10);
        board.InitializeAuthority();
        board.match.homeTeamDirection=side==1?MatchManager.TeamAttackingDirection.LeftToRight:MatchManager.TeamAttackingDirection.RightToLeft;
        board.match.awayTeamDirection=side==1?MatchManager.TeamAttackingDirection.RightToLeft:MatchManager.TeamAttackingDirection.LeftToRight;
        var version=new OnlineCompatibility("room-throw-in","rules","catalog");
        var protocol=new OnlineMatchProtocol("room-throw-in","home","away","away",version);
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
        var throwIn=Object.FindObjectsByType<ThrowInManager>(FindObjectsSortMode.None).Single(m=>m.ball==board.match.ball);
        var spot=board.grid.GetHexCellAt(new Vector3Int(0,0,12));board.match.ball.PlaceAtCell(spot);
        throwIn.StartThrowInPreparation(spot,MatchManager.TeamInAttack.Home,false);Submit("throw.player:home:9");
        for(int i=0;i<16 && !throwIn.isWaitingForOptionalMovementDecision;i++)
        {
            yield return Wait(()=>throwIn.isWaitingForOptionalMovementDecision || authority.Capture().legalActions.Any(a=>a=="movement.forfeit" || a=="final-third.forfeit"),"Room protected throw-in movement");
            if(throwIn.isWaitingForOptionalMovementDecision)break;
            var actions=authority.Capture().legalActions;
            Assert.That(actions,Does.Not.Contain("movement.player:home:9"));
            Submit(actions.Contains("final-third.forfeit")?"final-third.forfeit":"movement.forfeit");yield return null;
        }
        Assert.That(throwIn.isWaitingForOptionalMovementDecision,Is.True);Submit("throw.now");
        yield return Wait(()=>throwIn.isWaitingForThrowTypeSelection,"Room throw type selection");Submit(head?"throw.head":"throw.feet");
        if(head)
        {
            yield return Wait(()=>board.match.highPassManager.headerManager.isActivated,"Room throw reaches header manager");
            Assert.That(authority.Capture().stage,Does.StartWith("header."));
        }
        else
        {
            Submit("ground.target:0:8");Submit("ground.target:0:8");
            yield return Wait(()=>!board.match.groundBallManager.isActivated && board.match.LastTokenToTouchTheBallOnPurpose==board.tokens.Single(t=>t.isHomeTeam && t.jerseyNumber==10),"Room throw completes to feet");
            Assert.That(authority.Capture().tokens.Single(t=>t.id=="home:9").passesCompleted,Is.EqualTo(1));
        }
        Assert.That(throwIn.isActivated,Is.False);Assert.That(board.match.ball.GetCurrentHex(),Is.SameAs(board.grid.GetHexCellAt(new Vector3Int(0,0,8))));
    }
}
