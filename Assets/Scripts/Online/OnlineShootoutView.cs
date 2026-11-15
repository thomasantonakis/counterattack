using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace CounterAttack.Online
{
    public sealed class OnlineShootoutView:MonoBehaviour
    {
        public OnlineGroundPassBoard board;
        public Text homeOrder,awayOrder,status;
        public Dropdown ownPlayer;
        public Button earlier,later,confirm,pause,leave;
        private OnlineGroundPassState state;
        private string[] own=Array.Empty<string>();
        private string orderKey;
        private void Awake()
        {
            earlier.onClick.AddListener(()=>Swap(-1));later.onClick.AddListener(()=>Swap(1));confirm.onClick.AddListener(board.controls.ConfirmShootout);
            pause.onClick.AddListener(board.controls.Pause);leave.onClick.AddListener(board.menu.Leave);ownPlayer.onValueChanged.AddListener(_=>Refresh());
        }
        public void Display(OnlineGroundPassState publication)
        {
            state=publication;var shootout=state?.shootout;gameObject.SetActive(shootout?.selecting==true || shootout?.complete==true);
            if(shootout==null)return;
            string Name(string id)=>state.tokens.FirstOrDefault(t=>t.id==id)?.runtime?.playerName??id;
            string Column(string[] ids,bool ready)=>string.Join("\n",ids.Select((id,i)=>(i+1)+". "+Name(id)))+"\n\n"+(ready?"Confirmed":"Choosing order");
            homeOrder.text=Column(shootout.homeOrder,shootout.homeReady);awayOrder.text=Column(shootout.awayOrder,shootout.awayReady);
            own=board.localTeam==OnlineTeam.Home?shootout.homeOrder:shootout.awayOrder;
            string key=board.localTeam+":"+string.Join("/",own);
            if(key!=orderKey)
            {
                string prior=ownPlayer.value>=0 && ownPlayer.value<ownPlayer.options.Count?ownPlayer.options[ownPlayer.value].text:null;
                var options=own.Select(id=>id.Split(':')[1]+". "+Name(id)).ToList();ownPlayer.ClearOptions();ownPlayer.AddOptions(options);
                ownPlayer.SetValueWithoutNotify(Math.Max(0,options.IndexOf(prior)));orderKey=key;
            }
            status.text=state.connectionHeld?"Reconnecting. Orders and confirmations are kept.":state.isSharedPaused?"The match is paused.":shootout.selecting?"Reorder your own takers, then confirm. Both managers must confirm.":
                "Penalties: "+shootout.homeScore+" - "+shootout.awayScore+"\n"+string.Join(" ",shootout.homeResults.Select(r=>r?"G":"M"))+"  |  "+string.Join(" ",shootout.awayResults.Select(r=>r?"G":"M"))+
                (shootout.complete?"\n"+(shootout.winnerIsHome?"Home":"Away")+" win on penalties.":"\n"+(shootout.homeFirst?"Home":"Away")+" take the first kick.");
            Refresh();
        }
        private void Update()=>Refresh();
        private void Refresh()
        {
            bool edit=board.controls.CanManageShootout;
            ownPlayer.gameObject.SetActive(state?.shootout?.selecting==true);
            earlier.gameObject.SetActive(state?.shootout?.selecting==true);later.gameObject.SetActive(state?.shootout?.selecting==true);confirm.gameObject.SetActive(state?.shootout?.selecting==true);
            ownPlayer.interactable=edit;earlier.interactable=edit && ownPlayer.value>0;later.interactable=edit && ownPlayer.value<own.Length-1;
            confirm.interactable=edit && !(board.localTeam==OnlineTeam.Home?state.shootout.homeReady:state.shootout.awayReady);
            pause.interactable=board.controls.pauseButton.interactable && state?.connectionHeld!=true;
        }
        private void Swap(int offset)
        {
            int a=ownPlayer.value,b=a+offset;if(!board.controls.CanManageShootout || a<0 || a>=own.Length || b<0 || b>=own.Length)return;
            board.controls.SwapShootout(int.Parse(own[a].Split(':')[1]),int.Parse(own[b].Split(':')[1]));
        }
    }
}
