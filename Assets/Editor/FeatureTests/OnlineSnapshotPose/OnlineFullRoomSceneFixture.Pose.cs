using System.Collections;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
public sealed partial class OnlineFullRoomSceneFixture
{
    private IEnumerator RunSnapshotPose(OnlineGroundPassBoard board,OnlineMatchProtocol protocol,MatchManager.GameData data)
    {
        var token=board.tokens.Single(t=>t.isHomeTeam && t.jerseyNumber==2);
        var start=token.transform.position;
        board.match.ball.StartCoroutine(board.match.ball.MoveToCell(board.grid.GetHexCellAt(new Vector3Int(6,0,0)),allowGKBoxMove:false));
        board.match.movementPhaseManager.StartCoroutine(board.match.movementPhaseManager.MoveTokenToHex(board.grid.GetHexCellAt(new Vector3Int(0,0,8)),token,false,false,false));
        float deadline=Time.realtimeSinceStartup+5;
        while(Vector3.Distance(start,token.transform.position)<.02f && Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(Vector3.Distance(start,token.transform.position),Is.GreaterThan(.02f));
        pause.SetConnectionHold(true);
        var authority=new OnlineGroundPassAuthority(board.match,protocol);var state=pause.Capture(authority);
        Assert.That(state.ballMoving,Is.True);Assert.That(state.playerMoving,Is.True);
        var expectedBall=board.match.ball.transform.position;var expectedToken=token.transform.position;
        Assert.That(Vector3.Distance(expectedBall,board.match.ball.GetCurrentHex().GetHexCenter()),Is.GreaterThan(.02f));
        var expectedHex=token.GetCurrentHex().coordinates;
        var wire=new OnlineGroundPassWire();OnlineGroundPassMessage decoded=null;
        foreach(var packet in OnlineGroundPassWire.Encode(new OnlineGroundPassMessage{kind="state",matchId=state.matchId,state=state}))decoded=wire.Receive(packet,packet.Length);
        pause.Dispose();pause=null;
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/MainMenu.unity",new LoadSceneParameters(LoadSceneMode.Single));session.Dispose();session=null;MatchManager.Instance=null;
        yield return Load(data,true);board=Object.FindAnyObjectByType<OnlineFullRoomBootstrap>().board;
        var replica=new OnlineGroundPassReplica("full-room-scene",board.tokens.Select(OnlineRollPublication.ActorId),OnlineFullRoomBootstrap.Catalog);
        Assert.That(replica.Apply(decoded.state),Is.True);board.Display(replica.State,"Reconnect in flight");
        token=board.tokens.Single(t=>t.isHomeTeam && t.jerseyNumber==2);
        Assert.That(Vector3.Distance(board.displayedBall.position,expectedBall),Is.LessThan(.001f),"Full snapshots must retain the actual in-flight ball pose");
        Assert.That(Vector3.Distance(token.transform.position,expectedToken),Is.LessThan(.001f),"Full snapshots must retain the moving token pose");
        Assert.That(token.GetCurrentHex().coordinates,Is.EqualTo(expectedHex),"Presentation cannot change logical occupancy");
        board.Display(replica.State,"Duplicate timed snapshot");
        Assert.That(Vector3.Distance(board.displayedBall.position,expectedBall),Is.LessThan(.001f));
        Assert.That(board.authorityRoot.activeSelf,Is.False);Assert.That(board.match.gameData.events,Is.Empty);Assert.That(board.match.CaptureOnlineDecisionTiming(),Is.Null);
    }
}
