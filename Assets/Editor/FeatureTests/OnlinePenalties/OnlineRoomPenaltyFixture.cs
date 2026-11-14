using System.Collections;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
public sealed partial class OnlineRoomActionSceneFixture
{
    public IEnumerator RunRoomPenalty(int side)
    {
        host=new OnlineRoomSession("room-penalty",false);
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode(OnlineRoomProofAuthoring.ScenePath,new LoadSceneParameters(LoadSceneMode.Single));
        board=Object.FindAnyObjectByType<OnlineGroundPassBoard>();
        if(side==-1)for(int i=0;i<board.initialCoordinates.Length;i++)board.initialCoordinates[i].x*=-1;
        board.initialCoordinates[1]=new Vector3Int(8*side,0,10);
        board.InitializeAuthority();
        board.match.homeTeamDirection=side==1?MatchManager.TeamAttackingDirection.LeftToRight:MatchManager.TeamAttackingDirection.RightToLeft;
        board.match.awayTeamDirection=side==1?MatchManager.TeamAttackingDirection.RightToLeft:MatchManager.TeamAttackingDirection.LeftToRight;
        var version=new OnlineCompatibility("room-penalty","rules","catalog");
        var protocol=new OnlineMatchProtocol("room-penalty","home","away","away",version);
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
        var penalty=board.match.EnsurePenaltyKickManager();penalty.StartPenaltyPreparation();
        yield return Wait(()=>authority.Capture().stage=="penalty.kicker","Room required keeper placement");
        Submit("penalty.player:home:9");
        yield return Wait(()=>authority.Capture().stage=="penalty.select","Room taker placement");
        for(int i=0;i<3;i++)
        {
            Submit("penalty.confirm");
            if(i<2)yield return Wait(()=>authority.Capture().stage=="penalty.select","Room penalty setup team");
        }
        Assert.That(penalty.isActivated,Is.False);Assert.That(board.match.shotManager.shotType,Is.EqualTo("penalty"));
        Assert.That(board.match.ball.GetCurrentHex(),Is.SameAs(board.grid.GetHexCellAt(new Vector3Int(14*side,0,0))));
        Submit("shot.target:"+(19*side)+":0");
        yield return Wait(()=>authority.Capture().stage=="shot.roll","Room penalty aiming reaches owned shooter roll");
        Assert.That(authority.Capture().owner,Is.EqualTo("Home"));
    }
}
