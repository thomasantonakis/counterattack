using System.Collections;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed partial class OnlineRoomActionSceneFixture
{
    public IEnumerator RunRoomMovement(int side)
    {
        host=new OnlineRoomSession("room-move",false);
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode(OnlineRoomProofAuthoring.ScenePath,new LoadSceneParameters(LoadSceneMode.Single));
        board=Object.FindAnyObjectByType<OnlineGroundPassBoard>();
        if(side==-1) for(int i=0;i<board.initialCoordinates.Length;i++) board.initialCoordinates[i].x*=-1;
        board.InitializeAuthority();
        board.match.homeTeamDirection=side==1?MatchManager.TeamAttackingDirection.LeftToRight:MatchManager.TeamAttackingDirection.RightToLeft;
        board.match.awayTeamDirection=side==1?MatchManager.TeamAttackingDirection.RightToLeft:MatchManager.TeamAttackingDirection.LeftToRight;
        var version=new OnlineCompatibility("room-move","rules","catalog");
        var protocol=new OnlineMatchProtocol("room-move","home","away","away",version);
        protocol.BindAuthenticatedAccount("home","home",version);protocol.BindAuthenticatedAccount("away","away",version);
        var authority=new OnlineGroundPassAuthority(board.match,protocol);
        using var pause=new OnlineSharedPause(protocol,board.match);
        var controls=board.controls;controls.Configure(OnlineTeam.Home);
        controls.Requested+=command=>
        {
            var reply=authority.Submit("home",command.ToCommand());Assert.That(reply.Status,Is.EqualTo(OnlineCommandStatus.Accepted));
            controls.Reply(new OnlineGroundPassMessage{status=reply.Status.ToString()});controls.Display(pause.Capture(authority));
        };
        void Click(string action)
        {
            var state=pause.Capture(authority);controls.Display(state);
            int index=System.Array.IndexOf(state.legalActions,action);Assert.That(index,Is.GreaterThanOrEqualTo(0),state.stage);
            controls.legalCommands.value=index;controls.commandButton.onClick.Invoke();
        }
        var receiver=board.tokens.Single(t=>t.isHomeTeam && t.jerseyNumber==10);
        Click(OnlineGroundPassAuthority.MovementStartAction);
        Click(OnlineGroundPassAuthority.MovementPlayerAction(OnlineRollPublication.ActorId(receiver)));
        var target=receiver.currentHex.GetNeighbors(board.grid).First(h=>h!=null && h.coordinates.z>receiver.currentHex.coordinates.z
            && authority.Capture().legalActions.Contains(OnlineGroundPassAuthority.MovementTargetAction(h.coordinates.x,h.coordinates.z)));
        Click(OnlineGroundPassAuthority.MovementTargetAction(target.coordinates.x,target.coordinates.z));
        double deadline=Time.realtimeSinceStartupAsDouble+10;
        while((board.match.movementPhaseManager.isPlayerMoving || receiver.currentHex!=target) && Time.realtimeSinceStartupAsDouble<deadline)yield return null;
        Assert.That(receiver.currentHex,Is.SameAs(target));Assert.That(board.match.TimingRollSequence,Is.Zero);
        var replica=new OnlineGroundPassReplica(protocol.MatchId);Assert.That(replica.Apply(authority.Capture()),Is.True);
        Assert.That(replica.State.tokens.Single(t=>t.id==OnlineRollPublication.ActorId(receiver)).z,Is.EqualTo(target.coordinates.z));
    }
}
