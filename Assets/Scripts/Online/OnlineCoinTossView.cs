using UnityEngine;
using UnityEngine.UI;
namespace CounterAttack.Online
{
    public sealed class OnlineCoinTossView:MonoBehaviour
    {
        public OnlineGroundPassBoard board;
        public Text title,status,homeName,awayName;
        public DuelDieView homeDie,awayDie;
        public Button roll,kickoff,left,right,pause,leave;
        private OnlineGroundPassState state;
        private string key;
        private void Awake()
        {
            roll.onClick.AddListener(()=>board.controls.SubmitAction("coin.roll"));kickoff.onClick.AddListener(()=>board.controls.SubmitAction("coin.kickoff"));
            left.onClick.AddListener(()=>board.controls.SubmitAction("coin.left"));right.onClick.AddListener(()=>board.controls.SubmitAction("coin.right"));
            pause.onClick.AddListener(board.controls.Pause);leave.onClick.AddListener(board.menu.Leave);
        }
        public void Display(OnlineGroundPassState publication)
        {
            state=publication;var coin=state?.coin;gameObject.SetActive(coin!=null);if(coin==null)return;
            var settings=board.match.gameData.gameSettings;homeName.text=settings.homeTeamName;awayName.text=settings.awayTeamName;
            bool penalties=coin.purpose==CoinTossPurpose.PenaltyShootout.ToString();title.text=penalties?"Penalty shootout coin toss":coin.purpose==CoinTossPurpose.ExtraTime.ToString()?"Extra-time coin toss":"Opening coin toss";
            left.GetComponentInChildren<Text>(true).text=penalties?"Left goal":"Left to right";right.GetComponentInChildren<Text>(true).text=penalties?"Right goal":"Right to left";
            kickoff.GetComponentInChildren<Text>(true).text=penalties?"Take first penalty":"Take kickoff";
            string next=coin.stage+":"+coin.homeRoll+":"+coin.awayRoll;
            if(key!=next)
            {
                if(coin.homeRoll>0)homeDie.Resolve(coin.homeRoll,false);else if(coin.stage=="home")homeDie.Begin(false);else homeDie.Clear();
                if(coin.awayRoll>0)awayDie.Resolve(coin.awayRoll,false);else if(coin.stage=="away")awayDie.Begin(false);else awayDie.Clear();key=next;
            }
            status.text=state.connectionHeld?"Reconnecting. The coin toss is held.":state.isSharedPaused?"The coin toss is paused.":coin.stage=="result"?
                (coin.homeRoll==coin.awayRoll?"A tie. Both managers will roll again.":"Result confirmed. The winner chooses next."):
                (state.owner==board.localTeam.ToString()?"Your choice":"Waiting for the other manager");Refresh();
        }
        private void Update()=>Refresh();
        private void Refresh()
        {
            roll.interactable=board.controls.CanSubmitAction("coin.roll");kickoff.interactable=board.controls.CanSubmitAction("coin.kickoff");
            left.interactable=board.controls.CanSubmitAction("coin.left");right.interactable=board.controls.CanSubmitAction("coin.right");
            homeDie.enabled=awayDie.enabled=state?.connectionHeld!=true && state?.isSharedPaused!=true;
            pause.interactable=board.controls.pauseButton.interactable && state?.connectionHeld!=true;
        }
    }
}
