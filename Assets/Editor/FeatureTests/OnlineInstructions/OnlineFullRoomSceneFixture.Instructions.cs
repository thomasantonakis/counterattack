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
    private IEnumerator RunOnlineInstructions(OnlineGroundPassBoard board,OnlineMatchProtocol protocol,MatchManager.GameData data,bool away,bool live)
    {
        GameDebugMonitor Monitor()=>board.gameObject.scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<GameDebugMonitor>(true)).Single();
        var monitor=Monitor();Assert.That(monitor.enabled,Is.False);Assert.That(monitor.instructionText,Is.Not.Null);
        var authority=new OnlineGroundPassAuthority(board.match,protocol);var state=authority.Capture();
        if(away){Assert.That(authority.Submit("h",OnlineGroundPassMessage.Command(state,1,OnlineGroundPassAuthority.KickoffConfirmAction).ToCommand()).Status,Is.EqualTo(OnlineCommandStatus.Accepted));state=authority.Capture();}
        if(live)
        {
            long homeSequence=away?1:0,awaySequence=0;float deadline=Time.realtimeSinceStartup+20;
            while(!(state.passActive && state.awaitingTarget))
            {
                Assert.That(Time.realtimeSinceStartup,Is.LessThan(deadline),"Real kickoff progresses to its mandatory pass");
                string action=state.stage=="kickoff.setup"?OnlineGroundPassAuthority.KickoffConfirmAction
                    :state.stage=="kickoff.taker"?state.legalActions.First(a=>a.StartsWith("kickoff.taker:")):null;
                if(action!=null)
                {
                    bool home=state.owner=="Home";long sequence=home?++homeSequence:++awaySequence;
                    Assert.That(authority.Submit(home?"h":"a",OnlineGroundPassMessage.Command(state,sequence,action).ToCommand()).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
                }
                yield return null;state=authority.Capture();
            }
        }
        string expected=monitor.GetCurrentInstructionSnapshotForLog()?.instructionText;Assert.That(expected,Is.Not.Null.And.Not.Empty,"The real disabled source managers still supply their kickoff instructions");
        if(!live)Assert.That(expected,Does.Contain(away?"Away":"Home").And.Contain("once").And.Not.Contain("twice"));
        int events=board.match.gameData.events.Count;authority.Capture();authority.Capture();Assert.That(board.match.gameData.events.Count,Is.EqualTo(events),"Reading instructions cannot log or analyze another action");
        board.Display(state,"Authority instructions");Assert.That(monitor.instructionText.text,Is.EqualTo(expected),"The authored instruction panel must render on the host");
        Assert.That(monitor.instructionText.gameObject.activeInHierarchy,Is.True);Assert.That(monitor.enabled,Is.False);Assert.That(monitor.IsAIThinking,Is.False);
        var wire=new OnlineGroundPassWire();OnlineGroundPassMessage decoded=null;
        foreach(var packet in OnlineGroundPassWire.Encode(new OnlineGroundPassMessage{kind="state",matchId=state.matchId,state=state}))decoded=wire.Receive(packet,packet.Length);
        pause.Dispose();pause=null;yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/MainMenu.unity",new LoadSceneParameters(LoadSceneMode.Single));session.Dispose();session=null;MatchManager.Instance=null;
        yield return Load(data,true);board=Object.FindAnyObjectByType<OnlineFullRoomBootstrap>().board;monitor=Monitor();
        var replica=new OnlineGroundPassReplica(state.matchId,board.tokens.Select(OnlineRollPublication.ActorId),OnlineFullRoomBootstrap.Catalog);Assert.That(replica.Apply(decoded.state),Is.True);
        var randomBefore=JsonUtility.ToJson(UnityEngine.Random.state);
        board.Display(replica.State,"Guest instructions");Assert.That(monitor.instructionText.text,Is.EqualTo(expected));Assert.That(monitor.instructionText.gameObject.activeInHierarchy,Is.True);
        var cellsBefore=board.tokens.Select(t=>t.GetCurrentHex()).ToArray();
        Assert.That(monitor.enabled,Is.False);Assert.That(monitor.IsAIThinking,Is.False);
        var held=replica.State;held.connectionHeld=true;board.Display(held,"Held");Assert.That(monitor.instructionText.text,Is.Empty);
        held.connectionHeld=false;held.isSharedPaused=true;board.Display(held,"Paused");Assert.That(monitor.instructionText.text,Is.Empty);
        board.Display(replica.State,"Recovered");Assert.That(monitor.instructionText.text,Is.EqualTo(expected));
        Assert.That(board.tokens.Select(t=>t.GetCurrentHex()),Is.EqualTo(cellsBefore));Assert.That(JsonUtility.ToJson(UnityEngine.Random.state),Is.EqualTo(randomBefore));Assert.That(board.match.gameData.events,Is.Empty);Assert.That(board.match.CaptureOnlineDecisionTiming(),Is.Null);Assert.That(board.authorityRoot.activeSelf,Is.False);
    }
}
