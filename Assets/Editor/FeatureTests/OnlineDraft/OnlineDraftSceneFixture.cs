using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed partial class OnlineDraftSceneFixture
{
    private OnlineSetupHostChannel host;
    private OnlineSetupGuestChannel guest;
    private readonly Queue<byte[]> toHost=new(),toGuest=new();
    private readonly Queue<int> rolls=new();
    private string hostAccount,guestAccount,previousMode,previousSave;
    private ApplicationManager app;private bool created,explicitSave;
    private readonly Random.State random=Random.state;
    private void Flush()
    {
        for(int i=0;i<4096 && (toHost.Count>0 || toGuest.Count>0);i++)
        {
            if(toGuest.Count>0){var p=toGuest.Dequeue();Assert.That(guest.Receive(hostAccount,p,p.Length),Is.True);}
            if(toHost.Count>0){var p=toHost.Dequeue();Assert.That(host.Receive(guestAccount,p,p.Length),Is.True);}
        }
        Assert.That(toHost,Is.Empty);Assert.That(toGuest,Is.Empty);
    }
    private OnlineDraftClient Client(OnlineTeam team)=>host.Draft.Team==team?host.Draft:guest.Draft;
    private void Pick()
    {var s=host.Draft.Capture();var roster=s.turn==OnlineTeam.Home?s.home:s.away;Assert.That(Client(s.turn).Request(OnlineDraftIntent.Pick,s.cards.First(card=>card!=null).id,System.Array.FindIndex(roster,c=>c==null)+1),Is.True);Flush();}
    public IEnumerator Run(bool hostAway,bool localHost,string branch)
    {
        Assert.That(OnlineFrontendSession.Current,Is.Null);
        app=ApplicationManager.Instance;created=app==null;if(created){ApplicationManager.EnsureInstanceExists();app=ApplicationManager.Instance;}
        previousMode=app.SelectedRoomGameMode;previousSave=app.LastSavedFileName;explicitSave=app.HasExplicitSaveContext;app.SetSelectedRoomGameMode(ApplicationManager.OnlineGameMode);
        hostAccount=hostAway?"away":"home";guestAccount=hostAway?"home":"away";
        var version=new OnlineCompatibility(OnlineSessionWire.Version,"draft-scene","catalog");
        var protocol=new OnlineMatchProtocol("draft-scene","home","away",hostAccount,version);protocol.BindAuthenticatedAccount(hostAccount,"host",version);
        host=new(protocol,"host",guestAccount,branch.StartsWith("drag-",System.StringComparison.Ordinal)?DragSettings(branch):OnlineSetupAgreementEditorTests.Valid(),()=>0);
        guest=new(protocol.MatchId,hostAccount,version,protocol.HostTeam,()=>0);
        host.EnableDraft(s=>new OnlineDraftAuthority(protocol,s,OnlineDraftCatalog.Load(),_=>0,()=>rolls.Dequeue()));
        host.Outgoing+=toGuest.Enqueue;guest.Outgoing+=toHost.Enqueue;host.Begin();Flush();
        host.Client.Confirm(host.Client.ViewSettings());Flush();guest.Client.Confirm(guest.Client.ViewSettings());Flush();host.Client.Confirm(host.Client.ViewSettings());Flush();
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Draft.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;
        var view=Object.FindAnyObjectByType<OnlineDraftView>();Assert.That(view,Is.Not.Null);
        var client=localHost?host.Draft:guest.Draft;view.Configure(client);yield return null;
        Assert.That(view.source.enabled || view.sourceUi.enabled,Is.False,"Guest/local menus cannot bootstrap independent saves or rules");
        Assert.That(view.source.allPlayers,Is.Null,"Local CSV/random startup remains inactive");
        if(branch.StartsWith("drag-",System.StringComparison.Ordinal)){yield return RunDrag(view,client,branch.Substring(5));yield break;}
        Assert.That(view.source.draftOrderPanel.visuals.activeInHierarchy,Is.True,"Roll projection must remain visible");
        Assert.That(view.homeSlots.Length,Is.EqualTo(18));Assert.That(view.awaySlots.Length,Is.EqualTo(18));Assert.That(view.cards.Length,Is.EqualTo(4));
        Assert.That(view.homeSlots.Count(s=>s.gameObject.activeInHierarchy),Is.EqualTo(16));
        rolls.Enqueue(6);rolls.Enqueue(1);
        if(client.Team==OnlineTeam.Home){Assert.That(view.actionButton.interactable,Is.True);view.actionButton.onClick.Invoke();Flush();Client(OnlineTeam.Away).Request(OnlineDraftIntent.Roll);Flush();}
        else{Assert.That(view.actionButton.interactable,Is.False);Client(OnlineTeam.Home).Request(OnlineDraftIntent.Roll);Flush();Assert.That(view.actionButton.interactable,Is.True);view.actionButton.onClick.Invoke();Flush();}
        Assert.That(view.source.draftOrderPanel.visuals.activeSelf,Is.False);Assert.That(view.cards.All(c=>c.gameObject.activeInHierarchy),Is.True);
        if (branch == "team-average")
        {
            for (int i = 0; i < 40 && host.Draft.Capture().phase == "pick"; i++) Pick();
            var drafted = client.Capture();
            Assert.That(drafted.phase, Is.EqualTo("review"));
            foreach (bool home in new[] { true, false })
            {
                var roster = home ? drafted.home : drafted.away;
                var panel = home ? view.source.homeAveragePanel : view.source.awayAveragePanel;
                string side = home ? "Home" : "Away";
                var team = roster.Where((card, index) => card != null && index != 0 && index != 11);
                var xi = roster.Skip(1).Take(10);
                Assert.That(panel.transform.Find($"{side}-TeamAverage/ContentWrapper/PaceInSlot").GetComponent<TMP_Text>().text,
                    Is.EqualTo(team.Average(card => card.pace).ToString("F1")));
                Assert.That(panel.transform.Find($"{side}-XI/ContentWrapper/PaceInSlot").GetComponent<TMP_Text>().text,
                    Is.EqualTo(xi.Average(card => card.pace).ToString("F1")));
            }
            yield break;
        }
        if(branch=="fixed-offer-positions")
        {
            Canvas.ForceUpdateCanvases();
            var positions=view.cards.Select(c=>((RectTransform)c.transform).anchoredPosition).ToArray();
            string[] offered=view.cards.Select(c=>c.assignedPlayer.Name).ToArray();
            var first=view.cards[0];
            var badge=first.paceValueText.transform.parent.Find(first.paceValueText.name+"Badge")
                .GetComponent<UnityEngine.UI.Image>();
            Assert.That(badge.sprite.name,Is.EqualTo("Disc"));
            Assert.That(badge.rectTransform.sizeDelta,Is.EqualTo(new Vector2(45,45)));
            Assert.That(badge.transform.GetSiblingIndex(),Is.LessThan(first.paceValueText.transform.GetSiblingIndex()));
            Assert.That(badge.color,Is.EqualTo(MatchStatsUI.GetInspectionAttributeColor(first.assignedPlayer.Pace)));
            var click=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
                { button=UnityEngine.EventSystems.PointerEventData.InputButton.Left, clickCount=2 };
            view.cards[1].GetComponent<OnlineDraftClickTarget>().OnPointerClick(click);Flush();
            yield return null;Canvas.ForceUpdateCanvases();
            Assert.That(client.Capture().home[1]?.name,Is.EqualTo(offered[1]));
            Assert.That(view.cards.Select(c=>((RectTransform)c.transform).anchoredPosition),Is.EqualTo(positions));
            Assert.That(view.cards[1].GetComponent<CanvasGroup>().alpha,Is.Zero);
            foreach(int index in new[]{0,2,3})Assert.That(view.cards[index].assignedPlayer.Name,Is.EqualTo(offered[index]));
            var remote=Client(OnlineTeam.Away);
            var candidate=remote.Capture().cards[2].id;
            remote.PublishDrag(candidate,0,.25f,.1f);Flush();
            Assert.That(((RectTransform)view.cards[2].transform).anchoredPosition,Is.Not.EqualTo(positions[2]));
            Assert.That(view.cards[2].GetComponent<Canvas>().overrideSorting,Is.True);
            Assert.That(client.Capture().away[1],Is.Null);
            remote.PublishDrag(null,0,0,0);Flush();
            Assert.That(((RectTransform)view.cards[2].transform).anchoredPosition,Is.EqualTo(positions[2]));
            Assert.That(view.cards[2].GetComponent<Canvas>().overrideSorting,Is.False);
            yield break;
        }
        if(branch=="ownership")
        {
            var before=client.Capture();view.SelectSlot(client.Team==OnlineTeam.Home?OnlineTeam.Away:OnlineTeam.Home,2);Flush();
            Assert.That(client.Capture().revision,Is.EqualTo(before.revision));
        }
        if(client.Team==OnlineTeam.Away)Pick();
        var beforePick=client.Capture();int offerIndex=System.Array.FindIndex(beforePick.cards,card=>card!=null);
        string chosen=beforePick.cards[offerIndex].id;view.SelectCard(offerIndex);view.SelectSlot(client.Team,2);Flush();
        var selected=client.Capture();var own=client.Team==OnlineTeam.Home?selected.home:selected.away;
        Assert.That(own[1].id,Is.EqualTo(chosen));
        var slots=client.Team==OnlineTeam.Home?view.homeSlots:view.awaySlots;
        Assert.That(slots[1].transform.Find("ContentWrapper/PlayerNameInSlot").GetComponent<TMP_Text>().text,Is.EqualTo(own[1].name));
        if(branch=="swap")
        {view.SelectSlot(client.Team,2);view.SelectSlot(client.Team,3);Flush();var after=client.Capture();Assert.That((client.Team==OnlineTeam.Home?after.home:after.away)[2].id,Is.EqualTo(chosen));}
        if(branch=="recovery")
        {
            guest.TransportLost();Assert.That(guest.Draft.Connected,Is.False);host.Begin();Flush();
            Assert.That(client.Connected,Is.True);Assert.That((client.Team==OnlineTeam.Home?client.Capture().home:client.Capture().away)[1].id,Is.EqualTo(chosen));
        }
        if(branch=="complete")
        {
            for(int i=0;i<40 && host.Draft.Capture().phase=="pick";i++)Pick();
            Assert.That(host.Draft.Capture().phase,Is.EqualTo("review"));Assert.That(view.actionButton.interactable,Is.True);
            view.actionButton.onClick.Invoke();Flush();var other=localHost?guest.Draft:host.Draft;other.Request(OnlineDraftIntent.Ready);Flush();
            if(localHost)view.actionButton.onClick.Invoke();else host.Draft.Request(OnlineDraftIntent.Start);Flush();
            Assert.That(client.Capture().phase,Is.EqualTo("opening-roll"));Assert.That(view.actionButton.interactable,Is.EqualTo(client.Team==OnlineTeam.Home));
        }
        Assert.That(app.LastSavedFileName,Is.EqualTo(previousSave));Assert.That(app.HasExplicitSaveContext,Is.EqualTo(explicitSave));
        Assert.That(Object.FindObjectsByType<OnlineDraftClickTarget>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length,Is.EqualTo(40),"Projection uses authored controls only");
    }
    public IEnumerator Cleanup()
    {
        if(app==null)yield break;
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/MainMenu.unity",new LoadSceneParameters(LoadSceneMode.Single));
        app.SetSelectedRoomGameMode(previousMode);if(explicitSave)app.SetActiveSaveFilePath(previousSave);else{app.ClearActiveSaveContext();app.LastSavedFileName=previousSave;}
        if(created)Object.Destroy(app.gameObject);Random.state=random;yield return null;
    }
}
