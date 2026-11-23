using UnityEngine;
using UnityEngine.EventSystems;
namespace CounterAttack.Online
{
    public sealed class OnlineDraftClickTarget:MonoBehaviour,IPointerClickHandler,IBeginDragHandler,IDragHandler,IEndDragHandler,IDropHandler
    {
        public OnlineDraftView view;public bool card;public int index;public OnlineTeam team;
        private OnlineDraftDragSelection selection;
        private RectTransform draggedRect;
        private CanvasGroup dragGroup;
        private Transform originalParent;
        private Vector2 originalPosition;
        private Vector3 pointerOffset;
        private float originalAlpha;
        private bool originalRaycasts;
        private Canvas dragCanvas;
        private bool previousOverrideSorting;
        private int previousSortingOrder;
        private PlayerSlotDropHandler highlightedSlot;
        public void OnPointerClick(PointerEventData eventData)
        {
            if(view==null || eventData==null || eventData.dragging || selection!=null || eventData.button!=PointerEventData.InputButton.Left)return;
            if(card)view.SelectCard(index,eventData.clickCount==2);
            else view.SelectSlot(team,index);
        }
        public void OnBeginDrag(PointerEventData eventData)
        {
            if(view==null || eventData==null || eventData.button!=PointerEventData.InputButton.Left || selection!=null)return;
            var rect=GetComponent<RectTransform>();var group=GetComponent<CanvasGroup>();
            if(rect==null || group==null || !view.TryBeginDrag(card,team,index,out var pending))return;
            selection=pending;draggedRect=rect;dragGroup=group;originalParent=rect.parent;originalPosition=rect.anchoredPosition;originalAlpha=group.alpha;originalRaycasts=group.blocksRaycasts;
            RectTransformUtility.ScreenPointToWorldPointInRectangle(originalParent as RectTransform??rect,eventData.position,eventData.pressEventCamera,out var point);pointerOffset=rect.position-point;
            group.alpha=originalAlpha*.6f;group.blocksRaycasts=false;
            dragCanvas=GetComponent<Canvas>();
            if(dragCanvas!=null)
            {
                previousOverrideSorting=dragCanvas.overrideSorting;
                previousSortingOrder=dragCanvas.sortingOrder;
                dragCanvas.overrideSorting=true;
                dragCanvas.sortingOrder=Mathf.Max(previousSortingOrder,dragCanvas.rootCanvas.sortingOrder+20);
            }
            view.PublishDragPosition(selection,rect,originalPosition);
        }
        public void OnDrag(PointerEventData eventData)
        {
            if(selection==null)return;
            if(view==null || !view.DragIsCurrent(selection) || eventData==null){Cancel();return;}
            var target=eventData.pointerCurrentRaycast.gameObject?.GetComponentInParent<PlayerSlotDropHandler>();
            if(target!=highlightedSlot)
            {
                highlightedSlot?.ClearDropHighlight();
                highlightedSlot=target;
                highlightedSlot?.OnPointerEnter(eventData);
            }
            if(RectTransformUtility.ScreenPointToWorldPointInRectangle(originalParent as RectTransform??draggedRect,eventData.position,eventData.pressEventCamera,out var point))
            {
                draggedRect.position=point+pointerOffset;
                view.PublishDragPosition(selection,draggedRect,originalPosition);
            }
        }
        public void OnDrop(PointerEventData eventData)
        {
            if(card || view==null || eventData?.pointerDrag==null)return;
            var source=eventData.pointerDrag.GetComponent<OnlineDraftClickTarget>();if(source==null || source.selection==null)return;
            if(source.view!=view){source.Cancel();return;}
            var pending=source.selection;source.Restore();view.CompleteDrag(pending,team,index);
        }
        public void OnEndDrag(PointerEventData eventData)=>Cancel();
        internal bool CanPreviewDrop(OnlineDraftClickTarget target)
        {
            return selection!=null && target!=null && !target.card && target.view==view
                && target.team==view.Client.Team && target.index!=(!card?index:0)
                && (target.index is 1 or 12)==selection.goalkeeper;
        }
        private void OnDisable()=>Cancel();
        private void Cancel(){var pending=selection;Restore();if(pending!=null && view!=null)view.CancelDrag(pending);}
        private void Restore()
        {
            if(selection==null)return;selection=null;
            if(draggedRect!=null && draggedRect.parent==originalParent)draggedRect.anchoredPosition=originalPosition;
            if(dragGroup!=null){dragGroup.alpha=originalAlpha;dragGroup.blocksRaycasts=originalRaycasts;}
            if(dragCanvas!=null)
            {
                dragCanvas.sortingOrder=previousSortingOrder;
                dragCanvas.overrideSorting=previousOverrideSorting;
            }
            highlightedSlot?.ClearDropHighlight();
            highlightedSlot=null;
            dragCanvas=null;
            draggedRect=null;dragGroup=null;originalParent=null;
        }
    }
}
