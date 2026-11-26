using System;
using System.Collections;
using System.Linq;
using CounterAttack.Online;
using Newtonsoft.Json;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object=UnityEngine.Object;
public sealed partial class OnlineDraftSceneFixture
{
    private GameSettings DragSettings(string branch)
    {
        var settings=OnlineSetupAgreementEditorTests.Valid();
        if(branch=="drag-gk-card")settings.gkDraft="Free";
        if(branch=="drag-arcade-card")settings.draft="Arcade";
        if(branch=="drag-free-card")settings.draft="Free Regular";
        if(branch=="drag-large")settings.squadSize="18";
        return settings;
    }
    private void PickAnyDragCard()
    {
        var s=host.Draft.Capture();string id=s.freeSelection?s.available[0]:s.cards.First(card=>card!=null).id;var roster=s.turn==OnlineTeam.Home?s.home:s.away;
        Assert.That(Client(s.turn).Request(OnlineDraftIntent.Pick,id,Array.FindIndex(roster,c=>c==null)+1),Is.True);Flush();
    }
    private IEnumerator RunDrag(OnlineDraftView view,OnlineDraftClient client,string branch)
    {
        Assert.That(EventSystem.current,Is.Not.Null,"Authored Draft needs an active EventSystem");
        Assert.That(view.homeSlots.All(s=>!s.enabled && s.GetComponentsInChildren<PlayerSlotDragHandler>(true).All(d=>!d.enabled)),Is.True,"Offline drop/drag handlers remain inactive");
        Assert.That(view.awaySlots.All(s=>!s.enabled && s.GetComponentsInChildren<PlayerSlotDragHandler>(true).All(d=>!d.enabled)),Is.True);
        Assert.That(view.cards.SelectMany(c=>c.GetComponentsInChildren<PlayerCardDragHandler>(true)).All(d=>!d.enabled),Is.True);
        if(branch!="roll-blocked" && client.Capture().phase=="roll")
        {rolls.Enqueue(6);rolls.Enqueue(1);Assert.That(Client(OnlineTeam.Home).Request(OnlineDraftIntent.Roll),Is.True);Flush();Assert.That(Client(OnlineTeam.Away).Request(OnlineDraftIntent.Roll),Is.True);Flush();}
        var slots=client.Team==OnlineTeam.Home?view.homeSlots:view.awaySlots;var foreign=client.Team==OnlineTeam.Home?view.awaySlots:view.homeSlots;
        bool pool=branch is "card" or "gk-card" or "arcade-card" or "free-card" or "last-card" or "pending";
        bool needsPlayer=branch is "roster" or "kind" or "large";
        if(branch=="last-card")
        {
            for(int i=0;i<64;i++)
            {
                var s=client.Capture();var ownRoster=client.Team==OnlineTeam.Home?s.home:s.away;var otherRoster=client.Team==OnlineTeam.Home?s.away:s.home;
                if(s.phase=="pick" && s.turn==client.Team && ownRoster.Count(c=>c==null)==1 && otherRoster.All(c=>c!=null))break;PickAnyDragCard();
            }
            Assert.That(client.Capture().phase,Is.EqualTo("pick"));
        }
        if((pool || needsPlayer) && !client.Capture().repeatable && client.Capture().turn!=client.Team)PickAnyDragCard();
        if(needsPlayer){view.SelectCard(Math.Max(0,Array.FindIndex(client.Capture().cards,card=>card!=null)));view.SelectSlot(client.Team,2);Flush();}
        if(branch=="ready")
        {
            for(int i=0;i<64 && host.Draft.Capture().phase=="pick";i++)PickAnyDragCard();Assert.That(client.Capture().phase,Is.EqualTo("review"));
            Assert.That(Client(OnlineTeam.Home).Request(OnlineDraftIntent.Ready),Is.True);Flush();Assert.That(Client(OnlineTeam.Away).Request(OnlineDraftIntent.Ready),Is.True);Flush();
            Assert.That(client.Capture().homeReady && client.Capture().awayReady,Is.True);
        }
        yield return null;
        var before=client.Capture();var own=client.Team==OnlineTeam.Home?before.home:before.away;
        int offerIndex=Math.Max(0,Array.FindIndex(before.cards,card=>card!=null));
        bool poolRow=pool && before.freeSelection;
        var start=poolRow?view.poolRows[0]:pool?view.cards[offerIndex].GetComponent<OnlineDraftClickTarget>():slots[branch is "roster" or "kind" or "large" or "ready" or "empty"?1:0].GetComponent<OnlineDraftClickTarget>();
        int destination=branch=="last-card"?Array.FindIndex(own,c=>c==null)+1:branch=="large"?18:branch=="keeper"?12:pool?(branch=="gk-card"?1:2):branch is "roster" or "ready"?3:branch=="same"?1:branch=="kind"?1:12;
        var target=(branch=="foreign"?foreign:slots)[destination-1].GetComponent<OnlineDraftClickTarget>();
        var rect=start.GetComponent<RectTransform>();var original=rect.anchoredPosition;var parent=rect.parent;int sibling=rect.GetSiblingIndex();var group=start.GetComponent<CanvasGroup>();Assert.That(group,Is.Not.Null,"Use the authored drag canvas group");float alpha=group.alpha;bool raycasts=group.blocksRaycasts;
        int hierarchy=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length;var randomBefore=UnityEngine.Random.state;int requests=0;OnlineDraftRequest request=null;
        Action<OnlineDraftRequest> observe=r=>{requests++;request=r;};client.Requested+=observe;
        var canvas=start.GetComponentInParent<Canvas>();var data=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,pointerDrag=start.gameObject,position=RectTransformUtility.WorldToScreenPoint(canvas.worldCamera,rect.position),dragging=true,pointerPressRaycast=new RaycastResult{gameObject=start.gameObject,module=canvas.GetComponent<GraphicRaycaster>()}};
        bool invalidBegin=branch is "empty" or "roll-blocked";
        string chosen=pool?(before.freeSelection?before.available[0]:before.cards[offerIndex].id):own[start.index-1]?.id;
        if (poolRow) Assert.That(start.poolCells[2].text, Is.EqualTo(start.PoolCard.pace.ToString()));
        else if (pool)
        {
            var offered = before.cards[offerIndex];
            var pace = view.cards[offerIndex].paceValueText;
            Assert.That(pace.text, Is.EqualTo(offered.pace.ToString()));
            var circle = pace.transform.parent.Find(pace.name + "Badge");
            Assert.That(circle, Is.Not.Null, "The Draft offer reuses its authored rating circle");
            Assert.That(circle.GetComponent<Image>().color, Is.EqualTo(MatchStatsUI.GetInspectionAttributeColor(offered.pace)));
            Assert.That(circle.GetSiblingIndex(), Is.LessThan(pace.transform.GetSiblingIndex()));
        }
        try
        {
            Assert.That(ExecuteEvents.Execute(start.gameObject,data,ExecuteEvents.beginDragHandler),Is.True,"Authored online control must handle BeginDrag");
            Assert.That(group.blocksRaycasts,Is.EqualTo(invalidBegin?raycasts:false));
            var border = target.transform.Find("Online Drop Border");
            Assert.That(border, Is.Not.Null, "The four-sided drop border is authored on the slot prefab");
            Assert.That(border.childCount, Is.EqualTo(4));
            data.pointerCurrentRaycast = new RaycastResult { gameObject = target.gameObject };
            data.position+=new Vector2(80,40);data.delta=new Vector2(80,40);Assert.That(ExecuteEvents.Execute(start.gameObject,data,ExecuteEvents.dragHandler),Is.True);
            if (pool || branch is "roster" or "keeper" or "ready" or "large")
                Assert.That(border.gameObject.activeSelf, Is.True);
            if (invalidBegin || branch is "foreign" or "same" or "kind")
                Assert.That(border.gameObject.activeSelf, Is.False);
            if(poolRow)Assert.That(view.poolDragGhost.gameObject.activeInHierarchy,Is.True,"The authored row preview follows the drag");
            else if(!invalidBegin)Assert.That(Vector2.Distance(rect.anchoredPosition,original),Is.GreaterThan(1),"The existing authored card follows the drag");
            if(branch=="disconnect"){if(ReferenceEquals(client,host.Draft))host.TransportLost();else guest.TransportLost();}
            if(branch=="reconnect")
            {
                // Deliver the in-flight drag on its original generation before
                // rotating the handshake. Drop must still reject the old gesture.
                Flush();
                host.Begin();
                Flush();
                Assert.That(client.Connected,Is.True);
            }
            var dropBaseline=client.Capture();
            if(branch!="cancel")Assert.That(ExecuteEvents.Execute(target.gameObject,data,ExecuteEvents.dropHandler),Is.True);
            if(branch=="pending")
            {
                Assert.That(client.Pending,Is.True);ExecuteEvents.Execute(start.gameObject,data,ExecuteEvents.beginDragHandler);ExecuteEvents.Execute(target.gameObject,data,ExecuteEvents.dropHandler);Assert.That(requests,Is.EqualTo(1),"A pending guest intention cannot be sent twice");
            }
            bool ended=ExecuteEvents.Execute(start.gameObject,data,ExecuteEvents.endDragHandler);Assert.That(ended || !start.gameObject.activeInHierarchy,Is.True,"An inactive final card must already be restored by its disable path");Flush();
            bool accepted=pool || branch is "roster" or "keeper" or "ready" or "large";
            Assert.That(requests,Is.EqualTo(accepted?1:0));var after=client.Capture();var result=client.Team==OnlineTeam.Home?after.home:after.away;
            if(accepted)
            {
                Assert.That(client.LastReply,Is.EqualTo(OnlineCommandStatus.Accepted));Assert.That(request.intent,Is.EqualTo(pool?OnlineDraftIntent.Pick:OnlineDraftIntent.Swap));
                Assert.That(result[destination-1]?.id,Is.EqualTo(chosen));
                Assert.That(slots[destination-1].transform.Find("ContentWrapper/PlayerNameInSlot").GetComponent<TMP_Text>().text,Is.EqualTo(result[destination-1].name));
                if(!pool)Assert.That(result[start.index-1]?.id,Is.EqualTo(own[destination-1]?.id));
                if(branch=="ready")Assert.That(after.homeReady || after.awayReady,Is.False,"Roster changes require both confirmations again");
                if(branch=="last-card"){Assert.That(after.phase,Is.EqualTo("review"));Assert.That(start.gameObject.activeInHierarchy,Is.False);}
            }
            else Assert.That(JsonConvert.SerializeObject(after),Is.EqualTo(JsonConvert.SerializeObject(dropBaseline)),"Rejected or cancelled drag cannot mutate shared state");
            if(branch=="disconnect")
            {
                host.Begin();Flush();Assert.That(client.Connected,Is.True);ExecuteEvents.Execute(target.gameObject,data,ExecuteEvents.dropHandler);Flush();Assert.That(requests,Is.Zero,"A cancelled drag must not replay after reconnect");
            }
            Assert.That(JsonConvert.SerializeObject(host.Draft.Capture()),Is.EqualTo(JsonConvert.SerializeObject(guest.Draft.Capture())));
            Assert.That(rect.parent,Is.SameAs(parent));Assert.That(rect.GetSiblingIndex(),Is.EqualTo(sibling));Assert.That(Vector2.Distance(rect.anchoredPosition,original),Is.LessThan(.01));
            bool emptyOffer = pool && !after.freeSelection && after.cards.ElementAtOrDefault(offerIndex) == null;
            Assert.That(group.alpha,Is.EqualTo(emptyOffer ? 0 : alpha), "A picked offer leaves an empty slot in the same position");
            Assert.That(group.blocksRaycasts,Is.EqualTo(!emptyOffer && raycasts));
            Assert.That(Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length,Is.EqualTo(hierarchy),"Drag reuses authored objects without runtime placeholders");Assert.That(UnityEngine.Random.state,Is.EqualTo(randomBefore));
            Assert.That(border.gameObject.activeSelf, Is.False);
            if(poolRow)Assert.That(view.poolDragGhost.gameObject.activeSelf,Is.False);
            Assert.That(app.LastSavedFileName,Is.EqualTo(previousSave));Assert.That(app.HasExplicitSaveContext,Is.EqualTo(explicitSave));
        }
        finally{client.Requested-=observe;ExecuteEvents.Execute(start.gameObject,data,ExecuteEvents.endDragHandler);}
    }
}
