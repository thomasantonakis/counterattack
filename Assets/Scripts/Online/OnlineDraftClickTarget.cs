using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;
namespace CounterAttack.Online
{
    public sealed class OnlineDraftClickTarget:MonoBehaviour,IPointerClickHandler,IBeginDragHandler,IDragHandler,IEndDragHandler,IDropHandler
    {
        public OnlineDraftView view;public bool card;public int index;public OnlineTeam team;
        public TMP_Text[] poolCells;
        public OnlineDraftCard PoolCard { get; private set; }
        private bool IsPoolRow => poolCells != null && poolCells.Length > 0;
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
        private Image[] poolBadges;
        internal void BindPoolCard(OnlineDraftCard candidate)
        {
            PoolCard = candidate;
            if (candidate == null) return;
            poolBadges ??= new Image[poolCells.Length];
            for (int column = 0; column < poolCells.Length; column++)
            {
                poolCells[column].text = OnlineDraftView.CellValue(candidate, column);
                if (column < 2 || column > 8) continue;
                poolBadges[column] ??= poolCells[column].transform.parent.GetComponentInChildren<Image>(true);
                var badge = poolBadges[column];
                if (badge != null) badge.color = view.source.GetAttributeColor(OnlineDraftView.StatValue(candidate, column));
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if(view==null || eventData==null || eventData.dragging || selection!=null || eventData.button!=PointerEventData.InputButton.Left)return;
            if(IsPoolRow){if(PoolCard!=null)view.SelectPoolCard(PoolCard.id,eventData.clickCount==2);}
            else if(card)view.SelectCard(index,eventData.clickCount==2);
            else view.SelectSlot(team,index);
        }
        public void OnBeginDrag(PointerEventData eventData)
        {
            if(view==null || eventData==null || eventData.button!=PointerEventData.InputButton.Left || selection!=null)return;
            var rect=GetComponent<RectTransform>();var group=GetComponent<CanvasGroup>();
            if(rect==null || group==null || IsPoolRow && (PoolCard==null || view.localPoolDragPreview==null)
                || !view.TryBeginDrag(this,out var pending))return;
            if(IsPoolRow)
            {
                var preview=view.localPoolDragPreview;
                OnlineDraftView.PresentPoolDragCard(preview, PoolCard);
                rect=(RectTransform)preview.transform;
                if(RectTransformUtility.ScreenPointToWorldPointInRectangle((RectTransform)rect.parent,eventData.position,eventData.pressEventCamera,out var start))
                    rect.position=start;
            }
            selection=pending;draggedRect=rect;dragGroup=group;originalParent=rect.parent;originalPosition=rect.anchoredPosition;originalAlpha=group.alpha;originalRaycasts=group.blocksRaycasts;
            RectTransformUtility.ScreenPointToWorldPointInRectangle(originalParent as RectTransform??rect,eventData.position,eventData.pressEventCamera,out var point);pointerOffset=rect.position-point;
            group.alpha=originalAlpha*.6f;group.blocksRaycasts=false;
            dragCanvas=rect.GetComponent<Canvas>();
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
            PlayerSlotDropHandler.TrackDropTarget(ref highlightedSlot, eventData);
            if(RectTransformUtility.ScreenPointToWorldPointInRectangle(originalParent as RectTransform??draggedRect,eventData.position,eventData.pressEventCamera,out var point))
            {
                draggedRect.position=point+pointerOffset;
                view.PublishDragPosition(selection,draggedRect,originalPosition);
            }
        }
        public void OnDrop(PointerEventData eventData)
        {
            if(card || IsPoolRow || view==null || eventData?.pointerDrag==null)return;
            var source=eventData.pointerDrag.GetComponent<OnlineDraftClickTarget>();if(source==null || source.selection==null)return;
            if(source.view!=view){source.Cancel();return;}
            var pending=source.selection;source.Restore();view.CompleteDrag(pending,team,index);
        }
        public void OnEndDrag(PointerEventData eventData)=>Cancel();
        internal bool CanPreviewDrop(OnlineDraftClickTarget target)
        {
            return selection!=null && target!=null && !target.card && !target.IsPoolRow && target.view==view
                && view.CanDrop(selection,target.team,target.index);
        }
        private void OnDisable()=>Cancel();
        private void Cancel(){var pending=selection;Restore();if(pending!=null && view!=null)view.CancelDrag(pending);}
        internal void Restore()
        {
            if(selection==null)return;selection=null;
            if(draggedRect!=null && draggedRect.parent==originalParent)draggedRect.anchoredPosition=originalPosition;
            if(dragGroup!=null){dragGroup.alpha=originalAlpha;dragGroup.blocksRaycasts=originalRaycasts;}
            if(dragCanvas!=null)
            {
                dragCanvas.sortingOrder=previousSortingOrder;
                dragCanvas.overrideSorting=previousOverrideSorting;
            }
            PlayerSlotDropHandler.ClearDropTarget(ref highlightedSlot);
            if(IsPoolRow && view!=null && view.localPoolDragPreview!=null)view.localPoolDragPreview.gameObject.SetActive(false);
            dragCanvas=null;
            draggedRect=null;dragGroup=null;originalParent=null;
        }
    }
}
