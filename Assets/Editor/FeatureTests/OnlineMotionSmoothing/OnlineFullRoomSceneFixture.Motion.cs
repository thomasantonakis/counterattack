using System.Collections;
using System.Linq;
using System.Reflection;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
public sealed partial class OnlineFullRoomSceneFixture
{
    private IEnumerator RunMotionSmoothing(OnlineGroundPassBoard board,OnlineMatchProtocol protocol,MatchManager.GameData data,bool automatic)
    {
        var queue=typeof(OnlineGroundPassBoard).GetMethod("QueuePresentation");
        var advance=typeof(OnlineGroundPassBoard).GetMethod("AdvancePresentation",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
        Assert.That(queue,Is.Not.Null,"Streamed guest poses need an interpolation entry point");Assert.That(advance,Is.Not.Null);
        void Queue(OnlineGroundPassPresentation frame)=>queue.Invoke(board,new object[]{frame});
        void Advance(float seconds)=>advance.Invoke(board,new object[]{seconds});
        var state=pause.Capture(new OnlineGroundPassAuthority(board.match,protocol));
        var initial=board.displayedBall.position;var frame=new OnlineGroundPassPresentation{matchId=state.matchId,publicationSequence=state.publicationSequence,frameSequence=1,ballX=initial.x+2,ballY=initial.y+1,ballZ=initial.z+2,
            players=board.tokens.Select(t=>new OnlinePlayerPresentation{id=OnlineRollPublication.ActorId(t),x=t.transform.position.x+2,y=t.transform.position.y+.5f,z=t.transform.position.z+2}).ToArray()};
        Queue(frame);Advance(.05f);Assert.That(board.displayedBall.position,Is.EqualTo(initial),"Host transforms stay owned by gameplay");
        pause.Dispose();pause=null;yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/MainMenu.unity",new LoadSceneParameters(LoadSceneMode.Single));session.Dispose();session=null;MatchManager.Instance=null;
        yield return Load(data,true);board=Object.FindAnyObjectByType<OnlineFullRoomBootstrap>().board;
        var replica=new OnlineGroundPassReplica(state.matchId,board.tokens.Select(OnlineRollPublication.ActorId),OnlineFullRoomBootstrap.Catalog);
        Assert.That(replica.Apply(state),Is.True);board.Display(replica.State,"Motion fixture");
        var randomBefore=JsonUtility.ToJson(UnityEngine.Random.state);var rulesBefore=JsonUtility.ToJson(replica.State);
        var cellsBefore=board.tokens.Select(t=>t.GetCurrentHex()).ToArray();var positions=board.displayedTokens.Select(t=>t.position).ToArray();initial=board.displayedBall.position;
        var target=new Vector3(frame.ballX,frame.ballY,frame.ballZ);
        Assert.That(replica.ApplyPresentation(frame),Is.True);Queue(frame);
        Assert.That(board.displayedBall.position,Is.EqualTo(initial),"Receiving a stream frame alone does not teleport");
        Advance(.025f);Assert.That(Vector3.Distance(board.displayedBall.position,Vector3.Lerp(initial,target,.5f)),Is.LessThan(.001f));
        for(int i=0;i<board.tokens.Length;i++){var p=frame.players.Single(p=>p.id==OnlineRollPublication.ActorId(board.tokens[i]));Assert.That(Vector3.Distance(board.displayedTokens[i].position,Vector3.Lerp(positions[i],new Vector3(p.x,p.y,p.z),.5f)),Is.LessThan(.001f));}
        Assert.That(replica.ApplyPresentation(frame),Is.True);Queue(frame);Advance(.025f);
        Assert.That(Vector3.Distance(board.displayedBall.position,target),Is.LessThan(.001f),"A duplicated packet cannot restart smoothing");
        Advance(.5f);Assert.That(board.displayedBall.position,Is.EqualTo(target),"Missing packets do not extrapolate");
        frame.frameSequence=2;frame.ballX+=2;frame.ballZ+=2;Assert.That(replica.ApplyPresentation(frame),Is.True);Queue(frame);Advance(.01f);
        var mid=board.displayedBall.position;
        frame.frameSequence=3;frame.ballX+=2;Assert.That(replica.ApplyPresentation(frame),Is.True);Queue(frame);
        Assert.That(board.displayedBall.position,Is.EqualTo(mid),"Replacement begins at the current displayed pose");Advance(.025f);
        Assert.That(Vector3.Distance(board.displayedBall.position,Vector3.Lerp(mid,new Vector3(frame.ballX,frame.ballY,frame.ballZ),.5f)),Is.LessThan(.001f));
        board.Display(replica.State,"Reliable correction");var corrected=board.displayedBall.position;Advance(.5f);
        Assert.That(board.displayedBall.position,Is.EqualTo(corrected),"A reliable correction cancels an old stream target");
        var held=replica.State;held.connectionHeld=true;board.Display(held,"Connection hold");corrected=board.displayedBall.position;
        frame.frameSequence=4;Queue(frame);Advance(.5f);Assert.That(board.displayedBall.position,Is.EqualTo(corrected),"Held guest cannot animate queued motion");
        held.connectionHeld=false;held.isSharedPaused=true;board.Display(held,"Shared pause");frame.frameSequence=5;Queue(frame);Advance(.5f);
        Assert.That(board.displayedBall.position,Is.EqualTo(corrected),"Shared pause freezes presentation");
        held.isSharedPaused=false;board.Display(held,"Recovered");frame.frameSequence=6;Queue(frame);Advance(.05f);
        Assert.That(Vector3.Distance(board.displayedBall.position,new Vector3(frame.ballX,frame.ballY,frame.ballZ)),Is.LessThan(.001f));
        if(automatic)
        {
            frame.frameSequence=7;frame.ballX+=2;Assert.That(replica.ApplyPresentation(frame),Is.True);Queue(frame);
            var goal=new Vector3(frame.ballX,frame.ballY,frame.ballZ);float deadline=Time.realtimeSinceStartup+2;
            while(Vector3.Distance(board.displayedBall.position,goal)>.001f && Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(Vector3.Distance(board.displayedBall.position,goal),Is.LessThan(.001f),"The enabled board Update advances queued frames without a gameplay manager");
        }
        Assert.That(board.tokens.Select(t=>t.GetCurrentHex()),Is.EqualTo(cellsBefore));Assert.That(JsonUtility.ToJson(replica.State),Is.EqualTo(rulesBefore));
        Assert.That(JsonUtility.ToJson(UnityEngine.Random.state),Is.EqualTo(randomBefore));Assert.That(board.match.gameData.events,Is.Empty);Assert.That(board.match.CaptureOnlineDecisionTiming(),Is.Null);Assert.That(board.authorityRoot.activeSelf,Is.False);
    }
}
