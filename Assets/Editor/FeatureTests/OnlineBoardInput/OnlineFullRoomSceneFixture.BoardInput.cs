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
    private IEnumerator RunBoardInput(OnlineGroundPassBoard board,OnlineMatchProtocol protocol,MatchManager.GameData data,string branch)
    {
        var authority=new OnlineGroundPassAuthority(board.match,protocol);
        Assert.That(board.boardInput,Is.Not.Null,"Input must exist in the saved scene");board.boardInput.samplePhysicalInput=false;
        OnlineGroundPassState current=pause.Capture(authority);
        void Publish()
        {
            current=pause.Capture(authority);var owner=current.owner=="Home"?OnlineTeam.Home:OnlineTeam.Away;
            board.localTeam=owner;board.controls.Configure(owner);board.Display(current,"Pitch fixture");
        }
        Publish();int requests=0;OnlineGroundPassMessage last=null;
        Action<OnlineGroundPassMessage> handle=message=>
        {
            requests++;last=message;var reader=new OnlineGroundPassWire();OnlineGroundPassMessage decoded=null;
            foreach(var packet in OnlineGroundPassWire.Encode(message))decoded=reader.Receive(packet,packet.Length);
            var result=authority.Submit(current.owner=="Home"?"h":"a",decoded.ToCommand());
            Assert.That(result.Status,Is.EqualTo(OnlineCommandStatus.Accepted));
            board.controls.Reply(new OnlineGroundPassMessage{status=result.Status.ToString()});
        };
        var player=board.tokens.First(token=>current.legalActions.Contains(OnlineGroundPassAuthority.KickoffPlayerAction(OnlineRollPublication.ActorId(token))));
        if(branch=="guards")
        {
            board.controls.Requested+=handle;
            board.controls.Configure(current.owner=="Home"?OnlineTeam.Away:OnlineTeam.Home);Assert.That(board.boardInput.TryToken(player),Is.False);
            Publish();board.controls.SetConnected(false);Assert.That(board.boardInput.TryToken(player),Is.False);board.controls.SetConnected(true);
            var held=OnlineGroundPassState.Copy(current);held.connectionHeld=true;board.Display(held,"Held");Assert.That(board.boardInput.TryToken(player),Is.False);
            held.connectionHeld=false;held.isSharedPaused=true;board.Display(held,"Paused");Assert.That(board.boardInput.TryToken(player),Is.False);
            Publish();Assert.That(board.boardInput.TryToken(null),Is.False);Assert.That(board.boardInput.TryHex(null),Is.False);
            var other=new GameObject("Foreign token fixture");other.SetActive(false);try{Assert.That(board.boardInput.TryToken(other.AddComponent<PlayerToken>()),Is.False);}finally{Object.DestroyImmediate(other);}
            Assert.That(requests,Is.Zero);board.controls.Requested-=handle;yield break;
        }
        if(branch=="guest")
        {
            string playerId=OnlineRollPublication.ActorId(player);
            pause.Dispose();pause=null;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/MainMenu.unity",new LoadSceneParameters(LoadSceneMode.Single));session.Dispose();session=null;MatchManager.Instance=null;
            yield return Load(data,true);board=Object.FindAnyObjectByType<OnlineFullRoomBootstrap>().board;board.boardInput.samplePhysicalInput=false;
            board.controls.Configure(current.owner=="Home"?OnlineTeam.Home:OnlineTeam.Away);
            var replica=new OnlineGroundPassReplica(current.matchId,board.tokens.Select(OnlineRollPublication.ActorId),OnlineFullRoomBootstrap.Catalog);
            Assert.That(replica.Apply(current),Is.True);board.Display(replica.State,"Guest pitch");
            player=board.tokens.Single(token=>OnlineRollPublication.ActorId(token)==playerId);
            var positions=board.tokens.Select(token=>token.transform.position).ToArray();var randomBefore=JsonUtility.ToJson(UnityEngine.Random.state);
            Action<OnlineGroundPassMessage> collect=message=>{requests++;last=message;};board.controls.Requested+=collect;
            Assert.That(board.boardInput.TryToken(player),Is.True);Assert.That(board.boardInput.TryToken(player),Is.False,"Pending command blocks double click");
            Assert.That(requests,Is.EqualTo(1));Assert.That(last.actionId,Is.EqualTo(OnlineGroundPassAuthority.KickoffPlayerAction(OnlineRollPublication.ActorId(player))));
            Assert.That(board.tokens.Select(token=>token.transform.position),Is.EqualTo(positions));Assert.That(board.authorityRoot.activeSelf,Is.False);
            Assert.That(board.match.kickoffManager.selectedToken,Is.Null);Assert.That(board.match.gameData.events,Is.Empty);Assert.That(board.match.TimingRollSequence,Is.Zero);
            Assert.That(JsonUtility.ToJson(UnityEngine.Random.state),Is.EqualTo(randomBefore));board.controls.Requested-=collect;yield break;
        }
        board.controls.Requested+=handle;
        try
        {
            if(branch=="ray")
            {
                Physics.SyncTransforms();var center=player.currentHex.GetHexCenter();
                Assert.That(board.boardInput.TryRay(new Ray(center+Vector3.up*50,Vector3.down)),Is.True,"Real physics hit must select the legal actor");
            }
            else Assert.That(board.boardInput.TryToken(player),Is.True);
            Assert.That(board.match.kickoffManager.selectedToken,Is.SameAs(player));Publish();
            var move=current.legalActions.First(action=>action.StartsWith("kickoff.move:",StringComparison.Ordinal));var xy=move.Split(':');var target=board.grid.GetHexCellAt(new Vector3Int(int.Parse(xy[1]),0,int.Parse(xy[2])));
            Assert.That(board.boardInput.TryHex(target),Is.True);float deadline=Time.realtimeSinceStartup+10;
            while(authority.Capture().stage=="kickoff.resolving" && Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(player.currentHex,Is.SameAs(target));Publish();Assert.That(board.boardInput.TryKey(KeyCode.Return),Is.True);
            Publish();Assert.That(board.boardInput.TryKey(KeyCode.KeypadEnter),Is.True);Publish();Assert.That(current.stage,Is.EqualTo("kickoff.taker"));
            var taker=board.tokens.First(token=>current.legalActions.Contains(OnlineGroundPassAuthority.KickoffTakerAction(OnlineRollPublication.ActorId(token))));
            Assert.That(board.boardInput.TryToken(taker),Is.True);deadline=Time.realtimeSinceStartup+10;
            while(authority.Capture().stage.StartsWith("kickoff.") && Time.realtimeSinceStartup<deadline)yield return null;
            Publish();Assert.That(current.passActive,Is.True);Assert.That(current.awaitingTarget,Is.True);Assert.That(board.match.ball.GetCurrentHex().occupyingToken,Is.SameAs(taker));
            Assert.That(requests,Is.EqualTo(5));Assert.That(board.match.TimingRollSequence,Is.Zero);
        }
        finally{board.controls.Requested-=handle;}
    }
}
