using System.Collections;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed partial class OnlineRoomActionSceneFixture
{
    public IEnumerator RunRoomFirstTimePass(int side)
    {
        host=new OnlineRoomSession("room-ftp",false);
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode(OnlineRoomProofAuthoring.ScenePath,new LoadSceneParameters(LoadSceneMode.Single));
        board=Object.FindAnyObjectByType<OnlineGroundPassBoard>();
        if(side==-1)for(int i=0;i<board.initialCoordinates.Length;i++)board.initialCoordinates[i].x*=-1;
        board.initialCoordinates[1]=new Vector3Int(8*side,0,10);
        board.InitializeAuthority();
        board.match.homeTeamDirection=side==1?MatchManager.TeamAttackingDirection.LeftToRight:MatchManager.TeamAttackingDirection.RightToLeft;
        board.match.awayTeamDirection=side==1?MatchManager.TeamAttackingDirection.RightToLeft:MatchManager.TeamAttackingDirection.LeftToRight;
        var version=new OnlineCompatibility("room-ftp","rules","catalog");
        var protocol=new OnlineMatchProtocol("room-ftp","home","away","away",version);
        protocol.BindAuthenticatedAccount("home","home",version);protocol.BindAuthenticatedAccount("away","away",version);
        var authority=new OnlineGroundPassAuthority(board.match,protocol);
        using var pause=new OnlineSharedPause(protocol,board.match);
        void Submit(string connection,string action)
        {
            var state=pause.Capture(authority);Assert.That(state.legalActions,Does.Contain(action),state.stage);
            var team=connection=="home"?OnlineTeam.Home:OnlineTeam.Away;
            Assert.That(authority.Submit(connection,new OnlineMatchCommand(state.matchId,protocol.LastAcceptedSequence(team)+1,state.revision,state.decisionId,action)).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
        }
        IEnumerator Wait(System.Func<bool> ready,string reason)
        {
            double deadline=Time.realtimeSinceStartupAsDouble+15;
            while(!ready() && Time.realtimeSinceStartupAsDouble<deadline)yield return null;
            Assert.That(ready(),Is.True,reason);
        }
        var ftp=board.match.firstTimePassManager;
        Submit("home","ground.start");Submit("home","ground.target:0:6");Submit("home","ground.target:0:6");
        yield return Wait(()=>!board.match.groundBallManager.isActivated && ftp.isAvailable,"Room pass offers FTP");
        Submit("home","ftp.start");
        var target=board.grid.GetHexCellAt(new Vector3Int(0,0,0));
        Assert.That(ftp.TryExecuteOnlineFtp(OnlineFtpIntent.Target,target:board.grid.GetHexCellAt(new Vector3Int(18,0,10))),Is.False);
        host.Dispose();
        try
        {
            using var guest=new OnlineRoomSession("inert-ftp-guest",true);
            Assert.That(ftp.TryExecuteOnlineFtp(OnlineFtpIntent.Target,target:target),Is.False);
        }
        finally { host=new OnlineRoomSession("room-ftp",false); }
        Submit("home","ftp.target:0:0");Submit("home","ftp.target:0:0");
        Submit("home","ftp.skip");Submit("away","ftp.skip");
        yield return Wait(()=>!ftp.isActivated && board.match.LastTokenToTouchTheBallOnPurpose==board.tokens[0],"Room FTP returns to original passer");
        Assert.That(board.match.ball.GetCurrentHex(),Is.SameAs(target));
        Assert.That(board.match.TimingRollSequence,Is.Zero);
        var final=authority.Capture();var replica=new OnlineGroundPassReplica(protocol.MatchId);
        Assert.That(replica.Apply(final),Is.True);Assert.That(final.tokens.Single(t=>t.id=="home:10").passesCompleted,Is.EqualTo(1));
    }
}
