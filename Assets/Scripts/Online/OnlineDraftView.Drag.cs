using System;
using UnityEngine;
namespace CounterAttack.Online
{
    internal sealed class OnlineDraftDragSelection
    {
        internal readonly OnlineDraftView view;
        internal readonly OnlineDraftClient client;
        internal readonly long connection;
        internal readonly bool card,goalkeeper;
        internal readonly int index;
        internal readonly string id;
        internal OnlineDraftDragSelection(OnlineDraftView view,OnlineDraftClient client,long connection,bool card,int index,OnlineDraftCard player)
        {this.view=view;this.client=client;this.connection=connection;this.card=card;this.index=index;id=player.id;goalkeeper=player.goalkeeper;}
    }
    public sealed partial class OnlineDraftView
    {
        private OnlineDraftState rendered;
        private OnlineDraftDragSelection activeDrag;
        private RectTransform remoteDragRect;
        public PlayerCard freeDraftDragPreview;
        private Vector2 remoteDragOrigin;
        private Canvas remoteDragCanvas;
        private bool remoteDragSorting;
        private int remoteDragSortingOrder;
        private long remoteDragRevision;
        private double nextDragFrame;

        internal void PublishDragPosition(OnlineDraftDragSelection selection,RectTransform card,Vector2 origin)
        {
            if(Time.unscaledTimeAsDouble<nextDragFrame || card.parent is not RectTransform parent)return;
            var size=parent.rect.size;
            if(size.x<=0 || size.y<=0)return;
            nextDragFrame=Time.unscaledTimeAsDouble+1.0/15;
            var offset=card.anchoredPosition-origin;
            client.PublishDrag(selection.id,selection.card?0:selection.index,Mathf.Clamp(offset.x/size.x,-2,2),Mathf.Clamp(offset.y/size.y,-2,2),selection.card?selection.index:0);
        }

        private void RenderRemoteDrag(OnlineDraftDragFrame frame)
        {
            if(rendered==null)return;
            if(frame.cardId==null){ClearRemoteDrag();return;}
            if(frame.cardId!=null && frame.rosterSlot==0 && rendered.freeSelection && freeDraftDragPreview!=null)
            {
                localCatalog??=System.Linq.Enumerable.ToDictionary(OnlineDraftCatalog.Load(),card=>card.id);
                if(!localCatalog.TryGetValue(frame.cardId,out var offered) || frame.offerIndex>=cards.Length)return;
                var source=(RectTransform)cards[frame.offerIndex].transform;
                var sourcePanel=(RectTransform)source.parent;
                var preview=(RectTransform)freeDraftDragPreview.transform;
                if(!freeDraftDragPreview.gameObject.activeSelf || previewedOffer!=frame.cardId)
                {
                    freeDraftDragPreview.UpdatePlayerCard(Player(offered));
                    previewedOffer=frame.cardId;
                    freeDraftDragPreview.gameObject.SetActive(true);
                }
                preview.position=source.position+sourcePanel.TransformVector(Vector2.Scale(sourcePanel.rect.size,new Vector2(frame.x,frame.y)));
                remoteDragRevision=frame.revision;
                return;
            }
            RectTransform target=null;
            if(frame.cardId!=null)
            {
                if(frame.rosterSlot>0)
                {
                    var slots=client.Team==OnlineTeam.Home?awaySlots:homeSlots;
                    if(frame.rosterSlot<=slots.Length)target=slots[frame.rosterSlot-1].transform as RectTransform;
                }
                else
                {
                    int index=Array.FindIndex(DisplayedCards(rendered),offer=>offer?.id==frame.cardId);
                    if(index>=0 && index<cards.Length)target=cards[index].transform as RectTransform;
                }
            }
            if(target!=remoteDragRect)ClearRemoteDrag();
            if(target==null || target.parent is not RectTransform parent)return;
            if(remoteDragRect==null)
            {
                remoteDragOrigin=target.anchoredPosition;
                remoteDragCanvas=target.GetComponent<Canvas>();
                if(remoteDragCanvas!=null)
                {
                    remoteDragSorting=remoteDragCanvas.overrideSorting;
                    remoteDragSortingOrder=remoteDragCanvas.sortingOrder;
                    remoteDragCanvas.overrideSorting=true;
                    remoteDragCanvas.sortingOrder=Mathf.Max(remoteDragSortingOrder,remoteDragCanvas.rootCanvas.sortingOrder+19);
                }
            }
            remoteDragRect=target;
            remoteDragRevision=frame.revision;
            target.anchoredPosition=remoteDragOrigin+Vector2.Scale(parent.rect.size,new Vector2(frame.x,frame.y));
        }

        private void ClearRemoteDrag()
        {
            if(remoteDragRect!=null)remoteDragRect.anchoredPosition=remoteDragOrigin;
            if(remoteDragCanvas!=null)
            {
                remoteDragCanvas.sortingOrder=remoteDragSortingOrder;
                remoteDragCanvas.overrideSorting=remoteDragSorting;
            }
            remoteDragCanvas=null;
            remoteDragRect=null;
            if(freeDraftDragPreview!=null)freeDraftDragPreview.gameObject.SetActive(false);
            previewedOffer=null;
        }
        private string previewedOffer;
        internal bool TryBeginDrag(bool card,OnlineTeam team,int index,out OnlineDraftDragSelection selection)
        {
            selection=null;var s=rendered;
            if(!online || activeDrag!=null || client==null || !client.Connected || client.Pending || s==null || s.phase is not ("pick" or "review"))return false;
            OnlineDraftCard player;
            if(card)
            {
                if(s.phase!="pick" || !s.repeatable && s.turn!=client.Team)return false;
                var visible=DisplayedCards(s);if(index<0 || index>=visible.Length || visible[index]==null)return false;player=visible[index];
            }
            else
            {
                var roster=client.Team==OnlineTeam.Home?s.home:s.away;
                if(team!=client.Team || index<1 || index>roster.Length || roster[index-1]==null)return false;player=roster[index-1];
            }
            nextDragFrame=0;
            selection=activeDrag=new OnlineDraftDragSelection(this,client,s.connectionRevision,card,index,player);selected=null;selectedSlot=0;Render();return true;
        }
        internal bool DragIsCurrent(OnlineDraftDragSelection selection)
        {
            var s=rendered;
            if(selection==null || !ReferenceEquals(activeDrag,selection) || selection.view!=this || !ReferenceEquals(selection.client,client)
                || client==null || !client.Connected || client.Pending || s==null || s.connectionRevision!=selection.connection || s.phase is not ("pick" or "review"))return false;
            if(selection.card)
            {
                if(s.phase!="pick" || !s.repeatable && s.turn!=client.Team)return false;
                var visible=DisplayedCards(s);return selection.index>=0 && selection.index<visible.Length && visible[selection.index]?.id==selection.id;
            }
            var roster=client.Team==OnlineTeam.Home?s.home:s.away;
            return selection.index>=1 && selection.index<=roster.Length && roster[selection.index-1]?.id==selection.id;
        }
        internal bool CompleteDrag(OnlineDraftDragSelection selection,OnlineTeam team,int slot)
        {
            bool current=DragIsCurrent(selection);
            if(ReferenceEquals(activeDrag,selection)){activeDrag=null;client?.PublishDrag(null,0,0,0);}
            if(!current || team!=client.Team){RefreshAfterDrag();return false;}
            var roster=client.Team==OnlineTeam.Home?rendered.home:rendered.away;
            if(slot<1 || slot>roster.Length || !selection.card && (slot==selection.index || GoalkeeperSlot(slot)!=GoalkeeperSlot(selection.index))
                || selection.card && !selection.goalkeeper && GoalkeeperSlot(slot)){RefreshAfterDrag();return false;}
            // Picks keep the authority's existing GK/occupied-slot placement policy.
            bool sent=selection.card?client.Request(OnlineDraftIntent.Pick,selection.id,slot)
                :client.Request(OnlineDraftIntent.Swap,null,selection.index,slot);
            if(!sent)RefreshAfterDrag();return sent;
        }
        internal void CancelDrag(OnlineDraftDragSelection selection)
        {if(!ReferenceEquals(activeDrag,selection))return;activeDrag=null;client?.PublishDrag(null,0,0,0);RefreshAfterDrag();}
        private static bool GoalkeeperSlot(int slot)=>slot is 1 or 12;
        private void RefreshAfterDrag()
        {if(isActiveAndEnabled && source!=null && sourceUi!=null && actionButton!=null)Render();}
    }
}
