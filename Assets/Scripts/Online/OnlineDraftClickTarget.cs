using UnityEngine;
using UnityEngine.EventSystems;
namespace CounterAttack.Online
{
    public sealed class OnlineDraftClickTarget:MonoBehaviour,IPointerClickHandler
    {
        public OnlineDraftView view;public bool card;public int index;public OnlineTeam team;
        public void OnPointerClick(PointerEventData eventData)
        {if(eventData.button!=PointerEventData.InputButton.Left)return;if(card)view.SelectCard(index);else view.SelectSlot(team,index);}
    }
}
