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
    private IEnumerator RunPublishedHighlights(OnlineGroundPassBoard board,OnlineMatchProtocol protocol,MatchManager.GameData data)
    {
        var authority=new OnlineGroundPassAuthority(board.match,protocol);var state=authority.Capture();
        var actor=state.legalActions.First(a=>a.StartsWith("kickoff.player:"));
        Assert.That(authority.Submit("h",OnlineGroundPassMessage.Command(state,1,actor).ToCommand()).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
        state=authority.Capture();Assert.That(state.stage,Is.EqualTo("kickoff.setup"));
        var action=state.legalActions.First(a=>a.StartsWith("kickoff.move:"));var parts=action.Split(':');int x=int.Parse(parts[1]),z=int.Parse(parts[2]);
        var target=board.grid.GetHexCellAt(new Vector3Int(x,0,z));
        var hostReason = target.HighlightReason;
        board.Display(state, "Authority display");
        Assert.That(target.HighlightReason, Is.EqualTo(hostReason));
        target.ResetHighlight();
        var material = target.hexRenderer.material;
        string fill = material.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
        var defaultColor = material.GetColor(fill);
        board.Display(state, "Authority display without a native preview");
        Assert.That(material.GetColor(fill), Is.EqualTo(defaultColor), "Legal destinations do not add a host fallback");
        pause.Dispose();pause=null;yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/MainMenu.unity",new LoadSceneParameters(LoadSceneMode.Single));session.Dispose();session=null;MatchManager.Instance=null;
        yield return Load(data,true);board=Object.FindAnyObjectByType<OnlineFullRoomBootstrap>().board;
        var replica=new OnlineGroundPassReplica(state.matchId,board.tokens.Select(OnlineRollPublication.ActorId),OnlineFullRoomBootstrap.Catalog);Assert.That(replica.Apply(state),Is.True);
        var randomBefore=JsonUtility.ToJson(UnityEngine.Random.state);var rulesBefore=JsonUtility.ToJson(replica.State);var cellsBefore=board.tokens.Select(t=>t.GetCurrentHex()).ToArray();
        int objects=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length;
        var display = replica.State;
        display.hexPreviews = System.Array.Empty<OnlineHexPreview>();
        board.Display(display, "Guest legal choices without gameplay previews");
        target = board.grid.GetHexCellAt(new Vector3Int(x, 0, z));
        Assert.That(target.HighlightReason, Is.Not.EqualTo("ReachOverlayAttacker"));
        var selected = board.tokens.Single(t => OnlineRollPublication.ActorId(t) == state.selectedPlayer).GetCurrentHex();
        Assert.That(selected.HighlightReason, Is.Not.EqualTo("MovementDestinationHover"));
        var eligible = state.tokens.First(t => t.onBoard && t.id != state.selectedPlayer && OnlineBoardActionPicker.Actor(state, t.id) != null);
        var eligibleHex = board.grid.GetHexCellAt(new Vector3Int(eligible.x, 0, eligible.z));
        Assert.That(eligibleHex.HighlightReason, Is.Not.EqualTo("PaceAvailable"));

        display.hasSelectedTarget = true;
        display.selectedX = x;
        display.selectedZ = z;
        board.Display(display, "Selected coordinates without a gameplay preview");
        Assert.That(target.HighlightReason, Is.Not.EqualTo("passTargetCommitted"));

        display.hexPreviews = new[] { new OnlineHexPreview
            { x = x, z = z, reason = "passTarget", height = target.transform.position.y } };
        board.Display(display, "Published gameplay preview");
        Assert.That(target.HighlightReason, Is.EqualTo("passTarget"));
        display.connectionHeld = true;
        board.Display(display, "Held");
        Assert.That(target.HighlightReason, Is.Not.EqualTo("passTarget"));
        display.connectionHeld = false;
        board.Display(display, "Recovered");
        Assert.That(target.HighlightReason, Is.EqualTo("passTarget"));
        board.boardInput.enabled = false;
        Assert.That(target.HighlightReason, Is.Not.EqualTo("passTarget"));
        board.boardInput.enabled = true;
        Assert.That(Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length,Is.EqualTo(objects));
        Assert.That(board.tokens.Select(t=>t.GetCurrentHex()),Is.EqualTo(cellsBefore));Assert.That(JsonUtility.ToJson(replica.State),Is.EqualTo(rulesBefore));
        Assert.That(JsonUtility.ToJson(UnityEngine.Random.state),Is.EqualTo(randomBefore));Assert.That(board.match.gameData.events,Is.Empty);Assert.That(board.match.CaptureOnlineDecisionTiming(),Is.Null);Assert.That(board.authorityRoot.activeSelf,Is.False);
    }
}
