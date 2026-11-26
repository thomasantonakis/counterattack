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
        internal readonly OnlineDraftClickTarget source;
        internal readonly bool fromPool;
        internal OnlineDraftDragSelection(OnlineDraftView view,OnlineDraftClient client,long connection,OnlineDraftClickTarget source,OnlineDraftCard player)
        {
            this.view=view;this.client=client;this.connection=connection;this.source=source;
            fromPool=source.PoolCard!=null;card=source.card || fromPool;index=source.index;id=player.id;goalkeeper=player.goalkeeper;
        }
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
            if(Time.unscaledTimeAsDouble<nextDragFrame)return;
            var parent=(RectTransform)card.parent;
            Vector2 offset=card.anchoredPosition-origin;
            int offerIndex=selection.card?selection.index:0;
            if(selection.fromPool)
            {
                // The first card is a fixed shared reference even while the pool table is displayed.
                var anchor=(RectTransform)cards[0].transform;
                parent=(RectTransform)anchor.parent;
                offset=parent.InverseTransformVector(card.position-anchor.position);
                offerIndex=0;
            }
            var size=parent.rect.size;
            if(size.x<=0 || size.y<=0)return;
            nextDragFrame=Time.unscaledTimeAsDouble+1.0/15;
            client.PublishDrag(selection.id,selection.card?0:selection.index,Mathf.Clamp(offset.x/size.x,-2,2),Mathf.Clamp(offset.y/size.y,-2,2),offerIndex);
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
                    int index=Array.FindIndex(rendered.cards,offer=>offer?.id==frame.cardId);
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
        internal bool TryBeginDrag(OnlineDraftClickTarget target,out OnlineDraftDragSelection selection)
        {
            selection=null;var s=rendered;
            if(!online || activeDrag!=null || client==null || !client.Connected || client.Pending || s==null || s.phase is not ("pick" or "review"))return false;
            OnlineDraftCard player;
            if(target.PoolCard!=null)
            {
                if(!CanBrowsePool() || !System.Array.Exists(s.available,id=>id==target.PoolCard.id))return false;
                player=target.PoolCard;
            }
            else if(target.card)
            {
                if(!CanPick(s) || s.freeSelection || target.index<0 || target.index>=s.cards.Length || s.cards[target.index]==null)return false;
                player=s.cards[target.index];
            }
            else
            {
                var roster=OwnRoster(s);
                if(target.team!=client.Team || target.index<1 || target.index>roster.Length || roster[target.index-1]==null)return false;
                player=roster[target.index-1];
            }
            nextDragFrame=0;
            selection=activeDrag=new OnlineDraftDragSelection(this,client,s.connectionRevision,target,player);selected=null;selectedSlot=0;Render();return true;
        }
        internal bool DragIsCurrent(OnlineDraftDragSelection selection)
        {
            var s=rendered;
            if(selection==null || !ReferenceEquals(activeDrag,selection) || selection.view!=this || !ReferenceEquals(selection.client,client)
                || client==null || !client.Connected || client.Pending || s==null || s.connectionRevision!=selection.connection || s.phase is not ("pick" or "review"))return false;
            if(selection.card)
            {
                if(!CanPick(s))return false;
                if(selection.fromPool)return s.freeSelection && selection.goalkeeper==(s.selection=="goalkeepers")
                    && System.Array.Exists(s.available,id=>id==selection.id);
                return !s.freeSelection && selection.index>=0 && selection.index<s.cards.Length && s.cards[selection.index]?.id==selection.id;
            }
            var roster=client.Team==OnlineTeam.Home?s.home:s.away;
            return selection.index>=1 && selection.index<=roster.Length && roster[selection.index-1]?.id==selection.id;
        }
        internal bool CompleteDrag(OnlineDraftDragSelection selection,OnlineTeam team,int slot)
        {
            bool canDrop=CanDrop(selection,team,slot);
            if(ReferenceEquals(activeDrag,selection)){activeDrag=null;client?.PublishDrag(null,0,0,0);}
            if(!canDrop){RefreshAfterDrag();return false;}
            bool sent=selection.card?client.Request(OnlineDraftIntent.Pick,selection.id,slot)
                :client.Request(OnlineDraftIntent.Swap,null,selection.index,slot);
            if(!sent)RefreshAfterDrag();return sent;
        }
        internal bool CanDrop(OnlineDraftDragSelection selection,OnlineTeam team,int slot)
        {
            if(!DragIsCurrent(selection) || team!=client.Team || slot<1 || slot>OwnRoster(rendered).Length)return false;
            if(selection.fromPool)return CanPlacePoolCard(rendered,team,slot,selection.id);
            return GoalkeeperSlot(slot)==selection.goalkeeper && (selection.card || slot!=selection.index);
        }

        private void InvalidateLocalDrag()
        {
            if(activeDrag==null)return;
            activeDrag.source.Restore();
            activeDrag=null;
            client?.PublishDrag(null,0,0,0);
        }
        internal void CancelDrag(OnlineDraftDragSelection selection)
        {if(!ReferenceEquals(activeDrag,selection))return;activeDrag=null;client?.PublishDrag(null,0,0,0);RefreshAfterDrag();}
        private static bool GoalkeeperSlot(int slot)=>slot is 1 or 12;
        private void RefreshAfterDrag()
        {if(isActiveAndEnabled && source!=null && sourceUi!=null && actionButton!=null)Render();}
    }
}
