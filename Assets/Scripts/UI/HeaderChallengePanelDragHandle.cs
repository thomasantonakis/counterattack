using UnityEngine;
using UnityEngine.EventSystems;

public sealed class HeaderChallengePanelDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public HeaderChallengePanelController owner;
    private Vector2 offset;
    public void OnBeginDrag(PointerEventData data)
    {
        if (data.button != PointerEventData.InputButton.Left || owner == null || !owner.IsOpen) return;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(owner.panel.parent as RectTransform,
            data.position, data.pressEventCamera, out var point)) offset = owner.panel.anchoredPosition - point;
    }
    public void OnDrag(PointerEventData data)
    {
        if (data.button != PointerEventData.InputButton.Left || owner == null || !owner.IsOpen) return;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(owner.panel.parent as RectTransform,
            data.position, data.pressEventCamera, out var point)) owner.MovePanel(point + offset);
    }
    public void OnEndDrag(PointerEventData data)
    {
        if (data.button == PointerEventData.InputButton.Left && owner != null && owner.IsOpen) owner.MovePanel(owner.panel.anchoredPosition);
    }
}
