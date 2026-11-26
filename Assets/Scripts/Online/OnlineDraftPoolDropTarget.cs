using UnityEngine;
using UnityEngine.EventSystems;

namespace CounterAttack.Online
{
    public sealed class OnlineDraftPoolDropTarget : MonoBehaviour, IDropHandler
    {
        public OnlineDraftView view;

        public void OnDrop(PointerEventData eventData) => OnlineDraftClickTarget.DropOnPool(view, eventData);
    }
}
