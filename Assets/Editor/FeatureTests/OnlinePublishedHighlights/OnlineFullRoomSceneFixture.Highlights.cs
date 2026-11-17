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
    private IEnumerator RunPublishedHighlights(OnlineGroundPassBoard board,OnlineMatchProtocol protocol,MatchManager.GameData data)
    {
        var authority=new OnlineGroundPassAuthority(board.match,protocol);var state=authority.Capture();
        var actor=state.legalActions.First(a=>a.StartsWith("kickoff.player:"));
        Assert.That(authority.Submit("h",OnlineGroundPassMessage.Command(state,1,actor).ToCommand()).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
        state=authority.Capture();Assert.That(state.stage,Is.EqualTo("kickoff.setup"));
        var action=state.legalActions.First(a=>a.StartsWith("kickoff.move:"));var parts=action.Split(':');int x=int.Parse(parts[1]),z=int.Parse(parts[2]);
        var target=board.grid.GetHexCellAt(new Vector3Int(x,0,z));
        var highlight=typeof(HexCell).GetField("currentHighlightReason",BindingFlags.Instance|BindingFlags.NonPublic);
        string Reason(HexCell cell)=>(string)highlight.GetValue(cell);
        string hostReason=Reason(target);board.Display(state,"Authority display");Assert.That(Reason(target),Is.EqualTo(hostReason),"Source host highlights retain their own rule presentation");
        pause.Dispose();pause=null;yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/MainMenu.unity",new LoadSceneParameters(LoadSceneMode.Single));session.Dispose();session=null;MatchManager.Instance=null;
        yield return Load(data,true);board=Object.FindAnyObjectByType<OnlineFullRoomBootstrap>().board;
        var replica=new OnlineGroundPassReplica(state.matchId,board.tokens.Select(OnlineRollPublication.ActorId),OnlineFullRoomBootstrap.Catalog);Assert.That(replica.Apply(state),Is.True);
        var randomBefore=JsonUtility.ToJson(UnityEngine.Random.state);var rulesBefore=JsonUtility.ToJson(replica.State);var cellsBefore=board.tokens.Select(t=>t.GetCurrentHex()).ToArray();
        int objects=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length;
        board.Display(replica.State,"Guest choices");target=board.grid.GetHexCellAt(new Vector3Int(x,0,z));
        Assert.That(Reason(target),Is.EqualTo("ReachOverlayAttacker"),"A guest sees a published legal destination on the actual pitch");
        var selected=board.tokens.Single(t=>OnlineRollPublication.ActorId(t)==state.selectedPlayer).GetCurrentHex();
        Assert.That(Reason(selected),Is.EqualTo("MovementDestinationHover"),"Current selection is distinct from other eligible players");
        var eligible=state.tokens.First(t=>t.onBoard && t.id!=state.selectedPlayer && OnlineBoardActionPicker.Actor(state,t.id)!=null);
        var eligibleHex=board.grid.GetHexCellAt(new Vector3Int(eligible.x,0,eligible.z));Assert.That(Reason(eligibleHex),Is.EqualTo("PaceAvailable"));
        board.Display(replica.State,"Repeated clock state");Assert.That(Reason(target),Is.EqualTo("ReachOverlayAttacker"));
        var display=replica.State;display.hasSelectedTarget=true;display.selectedX=x;display.selectedZ=z;board.Display(display,"Chosen target");Assert.That(Reason(target),Is.EqualTo("passTargetCommitted"));
        display.connectionHeld=true;board.Display(display,"Held");Assert.That(Reason(target),Is.Null);Assert.That(Reason(selected),Is.Null);
        display.connectionHeld=false;display.isSharedPaused=true;board.Display(display,"Paused");Assert.That(Reason(target),Is.Null);
        board.Display(replica.State,"Recovered");Assert.That(Reason(target),Is.EqualTo("ReachOverlayAttacker"));
        display=replica.State;display.legalActions=System.Array.Empty<string>();display.selectedPlayer=null;board.Display(display,"Choice withdrawn");Assert.That(Reason(target),Is.Null);Assert.That(Reason(eligibleHex),Is.Null);
        board.Display(replica.State,"Restore");board.boardInput.enabled=false;Assert.That(Reason(target),Is.Null,"Disabled input clears its own hints");board.boardInput.enabled=true;
        Assert.That(Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length,Is.EqualTo(objects));
        Assert.That(board.tokens.Select(t=>t.GetCurrentHex()),Is.EqualTo(cellsBefore));Assert.That(JsonUtility.ToJson(replica.State),Is.EqualTo(rulesBefore));
        Assert.That(JsonUtility.ToJson(UnityEngine.Random.state),Is.EqualTo(randomBefore));Assert.That(board.match.gameData.events,Is.Empty);Assert.That(board.match.CaptureOnlineDecisionTiming(),Is.Null);Assert.That(board.authorityRoot.activeSelf,Is.False);
    }
}
