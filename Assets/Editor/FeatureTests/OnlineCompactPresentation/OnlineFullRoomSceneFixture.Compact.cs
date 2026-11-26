using System;
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
    private IEnumerator RunCompactPresentation(OnlineGroundPassBoard board,OnlineMatchProtocol protocol,MatchManager.GameData data,bool boundary)
    {
        var state=pause.Capture(new OnlineGroundPassAuthority(board.match,protocol));var position=board.match.ball.transform.position+new Vector3(.125f,0,.125f);
        var message=new OnlineGroundPassMessage{kind="presentation",matchId=state.matchId,peerNonce=Guid.NewGuid().ToString("N"),connectionGeneration=1,
            presentation=new OnlineGroundPassPresentation{matchId=state.matchId,publicationSequence=state.publicationSequence,frameSequence=1,ballX=position.x,ballY=position.y,ballZ=position.z,
                players=board.tokens.Select(t=>new OnlinePlayerPresentation{id=OnlineRollPublication.ActorId(t),x=t.transform.position.x,y=t.transform.position.y,z=t.transform.position.z}).ToArray()}};
        if(boundary)
        {
            string bench=OnlineRollPublication.ActorId(board.tokens.First(t=>!t.isPlaying));
            message.presentation.players.Single(p=>p.id==bench).x=999.5f;
        }
        var packets=OnlineGroundPassWire.Encode(message,true).ToArray();Assert.That(packets.Length,Is.EqualTo(1));Assert.That(packets[0].Length,Is.LessThanOrEqualTo(1170));
        var decoded=new OnlineGroundPassWire().Receive(packets[0],packets[0].Length);Assert.That(decoded.peerNonce,Is.EqualTo(message.peerNonce));
        pause.Dispose();pause=null;yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/MainMenu.unity",new LoadSceneParameters(LoadSceneMode.Single));session.Dispose();session=null;MatchManager.Instance=null;
        yield return Load(data,true);board=Object.FindAnyObjectByType<OnlineFullRoomBootstrap>().board;
        var replica=new OnlineGroundPassReplica(state.matchId,board.tokens.Select(OnlineRollPublication.ActorId),OnlineFullRoomBootstrap.Catalog);
        var before=JsonUtility.ToJson(UnityEngine.Random.state);Assert.That(replica.Apply(state),Is.True);board.Display(replica.State,"Compact pose guest");
        Assert.That(replica.ApplyPresentation(decoded.presentation),Is.True);board.DisplayPresentation(decoded.presentation);
        Assert.That(board.displayedBall.position,Is.EqualTo(position));Assert.That(board.authorityRoot.activeSelf,Is.False);Assert.That(board.match.gameData.events,Is.Empty);
        Assert.That(JsonUtility.ToJson(UnityEngine.Random.state),Is.EqualTo(before));
        foreach(var player in decoded.presentation.players)
        {var token=board.tokens.Single(t=>OnlineRollPublication.ActorId(t)==player.id);Assert.That(token.transform.position,Is.EqualTo(new Vector3(player.x,player.y,player.z)));}
    }
}
