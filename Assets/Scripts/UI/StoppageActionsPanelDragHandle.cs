using UnityEngine;
using UnityEngine.EventSystems;

public sealed class StoppageActionsPanelDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public StoppageActionsPanelController owner;
    private Vector2 pointerOffset;
    public void OnBeginDrag(PointerEventData data)
    {
        if (data.button != PointerEventData.InputButton.Left || owner == null || !owner.IsOpen) return;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(owner.Panel.parent as RectTransform,
            data.position, data.pressEventCamera, out Vector2 point)) pointerOffset = owner.Panel.anchoredPosition - point;
    }
    public void OnDrag(PointerEventData data)
    {
        if (data.button != PointerEventData.InputButton.Left || owner == null || !owner.IsOpen) return;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(owner.Panel.parent as RectTransform,
            data.position, data.pressEventCamera, out Vector2 point)) owner.MovePanel(point + pointerOffset);
    }
    public void OnEndDrag(PointerEventData data)
    {
        if (data.button == PointerEventData.InputButton.Left && owner != null && owner.IsOpen) owner.MovePanel(owner.Panel.anchoredPosition);
    }
}
