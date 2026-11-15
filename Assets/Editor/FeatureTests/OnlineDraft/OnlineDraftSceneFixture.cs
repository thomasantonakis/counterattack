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
    {var s=host.Draft.Capture();var roster=s.turn==OnlineTeam.Home?s.home:s.away;Assert.That(Client(s.turn).Request(OnlineDraftIntent.Pick,s.cards[0].id,System.Array.FindIndex(roster,c=>c==null)+1),Is.True);Flush();}
    public IEnumerator Run(bool hostAway,bool localHost,string branch)
    {
        Assert.That(OnlineFrontendSession.Current,Is.Null);
        app=ApplicationManager.Instance;created=app==null;if(created){ApplicationManager.EnsureInstanceExists();app=ApplicationManager.Instance;}
        previousMode=app.SelectedRoomGameMode;previousSave=app.LastSavedFileName;explicitSave=app.HasExplicitSaveContext;app.SetSelectedRoomGameMode(ApplicationManager.OnlineGameMode);
        hostAccount=hostAway?"away":"home";guestAccount=hostAway?"home":"away";
        var version=new OnlineCompatibility(OnlineSessionWire.Version,"draft-scene","catalog");
        var protocol=new OnlineMatchProtocol("draft-scene","home","away",hostAccount,version);protocol.BindAuthenticatedAccount(hostAccount,"host",version);
        host=new(protocol,"host",guestAccount,OnlineSetupAgreementEditorTests.Valid(),()=>0);
        guest=new(protocol.MatchId,hostAccount,version,protocol.HostTeam,()=>0);
        host.EnableDraft(s=>new OnlineDraftAuthority(protocol,s,OnlineDraftCatalog.Load(),_=>0,()=>rolls.Dequeue()));
        host.Outgoing+=toGuest.Enqueue;guest.Outgoing+=toHost.Enqueue;host.Begin();Flush();
        host.Client.Confirm(host.Client.ViewSettings());Flush();guest.Client.Confirm(guest.Client.ViewSettings());Flush();host.Client.Confirm(host.Client.ViewSettings());Flush();
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Draft.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;
        var view=Object.FindAnyObjectByType<OnlineDraftView>();Assert.That(view,Is.Not.Null);
        var client=localHost?host.Draft:guest.Draft;view.Configure(client);yield return null;
        Assert.That(view.source.enabled || view.sourceUi.enabled,Is.False,"Guest/local menus cannot bootstrap independent saves or rules");
        Assert.That(view.source.allPlayers,Is.Null,"Local CSV/random startup remains inactive");
        Assert.That(view.source.draftOrderPanel.visuals.activeInHierarchy,Is.True,"Roll projection must remain visible");
        Assert.That(view.homeSlots.Length,Is.EqualTo(18));Assert.That(view.awaySlots.Length,Is.EqualTo(18));Assert.That(view.cards.Length,Is.EqualTo(4));
        Assert.That(view.homeSlots.Count(s=>s.gameObject.activeInHierarchy),Is.EqualTo(16));
        rolls.Enqueue(6);rolls.Enqueue(1);
        if(client.Team==OnlineTeam.Home){Assert.That(view.actionButton.interactable,Is.True);view.actionButton.onClick.Invoke();Flush();Client(OnlineTeam.Away).Request(OnlineDraftIntent.Roll);Flush();}
        else{Assert.That(view.actionButton.interactable,Is.False);Client(OnlineTeam.Home).Request(OnlineDraftIntent.Roll);Flush();Assert.That(view.actionButton.interactable,Is.True);view.actionButton.onClick.Invoke();Flush();}
        Assert.That(view.source.draftOrderPanel.visuals.activeSelf,Is.False);Assert.That(view.cards.All(c=>c.gameObject.activeInHierarchy),Is.True);
        if(branch=="ownership")
        {
            var before=client.Capture();view.SelectSlot(client.Team==OnlineTeam.Home?OnlineTeam.Away:OnlineTeam.Home,2);Flush();
            Assert.That(client.Capture().revision,Is.EqualTo(before.revision));
        }
        if(client.Team==OnlineTeam.Away)Pick();
        var beforePick=client.Capture();string chosen=beforePick.cards[0].id;view.SelectCard(0);view.SelectSlot(client.Team,2);Flush();
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
            Assert.That(client.Capture().phase,Is.EqualTo("room-ready"));Assert.That(view.actionButton.interactable,Is.False);
        }
        Assert.That(app.LastSavedFileName,Is.EqualTo(previousSave));Assert.That(app.HasExplicitSaveContext,Is.EqualTo(explicitSave));
        Assert.That(Object.FindObjectsByType<OnlineDraftClickTarget>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length,Is.EqualTo(40),"Projection uses authored controls only");
    }
    public IEnumerator Cleanup()
    {
        if(app==null)yield break;
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/MainMenu.unity",new LoadSceneParameters(LoadSceneMode.Single));
        app.SetSelectedRoomGameMode(previousMode);if(created)Object.Destroy(app.gameObject);Random.state=random;yield return null;
    }
}
