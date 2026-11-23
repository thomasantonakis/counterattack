using System.Collections;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
public sealed partial class OnlineDraftSceneFixture
{
    public IEnumerator RunVariant(string mode,bool localHome)
    {
        Assert.That(OnlineFrontendSession.Current,Is.Null);
        app=ApplicationManager.Instance;created=app==null;if(created){ApplicationManager.EnsureInstanceExists();app=ApplicationManager.Instance;}
        previousMode=app.SelectedRoomGameMode;previousSave=app.LastSavedFileName;explicitSave=app.HasExplicitSaveContext;app.SetSelectedRoomGameMode(ApplicationManager.OnlineGameMode);
        hostAccount="away";guestAccount="home";
        var settings=OnlineSetupAgreementEditorTests.Valid();
        if(mode is "FreeGK" or "FullMixed"){settings.gkDraft="Free";if(mode=="FullMixed")settings.draft="Free Regular";}
        else if(mode=="International"){settings.matchType="International";settings.draft=settings.gkDraft="International";settings.squadSize="18";settings.homeTeamName="England";settings.awayTeamName="Germany";}
        else settings.draft=mode=="FreeDragDifferentPage"?"Free Regular":mode;
        var version=new OnlineCompatibility(OnlineSessionWire.Version,"draft-variant-scene","catalog");
        var protocol=new OnlineMatchProtocol("draft-variant-scene","home","away",hostAccount,version);protocol.BindAuthenticatedAccount(hostAccount,"host",version);
        host=new(protocol,"host",guestAccount,settings,()=>0);guest=new(protocol.MatchId,hostAccount,version,protocol.HostTeam,()=>0);
        host.EnableDraft(s=>new OnlineDraftAuthority(protocol,s,OnlineDraftCatalog.Load(),_=>0,()=>rolls.Dequeue()));
        host.Outgoing+=toGuest.Enqueue;guest.Outgoing+=toHost.Enqueue;host.Begin();Flush();
        host.Client.Confirm(host.Client.ViewSettings());Flush();guest.Client.Confirm(guest.Client.ViewSettings());Flush();host.Client.Confirm(host.Client.ViewSettings());Flush();
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Draft.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;
        var view=Object.FindAnyObjectByType<OnlineDraftView>();var client=localHome?guest.Draft:host.Draft;view.Configure(client);yield return null;
        Assert.That(view.source.enabled || view.sourceUi.enabled,Is.False);
        Assert.That(view.previousPageButton,Is.Not.Null);Assert.That(view.nextPageButton,Is.Not.Null);
        if(mode=="International")
        {
            Assert.That(view.homeSlots.Count(s=>s.gameObject.activeInHierarchy),Is.EqualTo(18));Assert.That(view.cards.Any(c=>c.gameObject.activeInHierarchy),Is.False);
            Assert.That(client.Capture().phase,Is.EqualTo("review"));view.actionButton.onClick.Invoke();Flush();
            var state=client.Capture();Assert.That(localHome?state.homeReady:state.awayReady,Is.True);
        }
        else
        {
            if(client.Capture().phase=="roll")
            {
                rolls.Enqueue(6);rolls.Enqueue(1);
                for(int i=0;i<2;i++)
                {var s=client.Capture();if(s.turn==client.Team)view.actionButton.onClick.Invoke();else Client(s.turn).Request(OnlineDraftIntent.Roll);Flush();}
            }
            var state=client.Capture();Assert.That(state.freeSelection,Is.True);
            if(mode=="FreeDragDifferentPage")
            {
                var sender=Client(state.turn);
                Assert.That(sender,Is.Not.SameAs(client));
                string idOffPage=state.available[1];
                view.ChangePage(1);
                string visibleName=view.cards[0].assignedPlayer.Name;
                sender.PublishDrag(idOffPage,0,.2f,.3f,1);Flush();
                Assert.That(view.freeDraftDragPreview.gameObject.activeInHierarchy,Is.True);
                Assert.That(view.freeDraftDragPreview.assignedPlayer.Name,Is.EqualTo(OnlineDraftCatalog.Load().Single(card=>card.id==idOffPage).name));
                Assert.That(view.cards[0].assignedPlayer.Name,Is.EqualTo(visibleName));
                sender.PublishDrag(null,0,0,0);Flush();
                Assert.That(view.freeDraftDragPreview.gameObject.activeSelf,Is.False);
                yield break;
            }
            if(!state.repeatable && state.turn!=client.Team)
            {bool keeper=state.selection=="goalkeepers";Client(state.turn).Request(OnlineDraftIntent.Pick,state.available[0],keeper?1:2);Flush();}
            state=client.Capture();long revision=state.revision;string first=view.cards[0].assignedPlayer.Name;
            view.nextPageButton.onClick.Invoke();Assert.That(client.Capture().revision,Is.EqualTo(revision),"Browsing never sends a rules command");
            Assert.That(view.cards[0].assignedPlayer.Name,Is.Not.EqualTo(first));string id=state.available[4];
            bool gk=state.selection=="goalkeepers";view.SelectCard(0);view.SelectSlot(client.Team,gk?1:2);Flush();
            var after=client.Capture();Assert.That((localHome?after.home:after.away)[gk?0:1].id,Is.EqualTo(id));
            if(gk)Assert.That(view.cards[0].playerNameText.text,Does.StartWith("GK"));
            if(mode=="FullMixed")
            {
                var catalog=OnlineDraftCatalog.Load().ToDictionary(c=>c.id);
                bool checkedNewPool=false;
                for(int step=0;step<100 && client.Capture().phase!="review";step++)
                {
                    var s=client.Capture();
                    if(s.phase=="roll")
                    {
                        rolls.Enqueue(6);rolls.Enqueue(1);
                        for(int i=0;i<2;i++){var rollState=client.Capture();if(rollState.turn==client.Team)view.actionButton.onClick.Invoke();else Client(rollState.turn).Request(OnlineDraftIntent.Roll);Flush();}
                        var picking=client.Capture();Assert.That(view.cards[0].assignedPlayer.Name,Is.EqualTo(catalog[picking.available[0]].name),"A new pool begins on its first page");checkedNewPool=true;continue;
                    }
                    var roster=s.turn==OnlineTeam.Home?s.home:s.away;bool keeper=s.selection=="goalkeepers";
                    int slot=Enumerable.Range(0,roster.Length).First(i=>(i is 0 or 11)==keeper && roster[i]==null)+1;
                    if(s.turn==client.Team){view.SelectCard(0);view.SelectSlot(client.Team,slot);}
                    else Client(s.turn).Request(OnlineDraftIntent.Pick,s.available[0],slot);
                    Flush();
                }
                Assert.That(checkedNewPool,Is.True);Assert.That(client.Capture().phase,Is.EqualTo("review"));
            }
        }
        Assert.That(app.LastSavedFileName,Is.EqualTo(previousSave));Assert.That(app.HasExplicitSaveContext,Is.EqualTo(explicitSave));
    }
}
