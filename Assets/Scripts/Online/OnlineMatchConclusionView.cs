using UnityEngine;
using UnityEngine.UI;
namespace CounterAttack.Online
{
    public sealed class OnlineMatchConclusionView:MonoBehaviour
    {
        public OnlineGroundPassBoard board;
        public Text title,homeName,awayName,score,result,summary;
        public Button leave;
        private void Awake()=>leave.onClick.AddListener(board.menu.Leave);
        public static bool CanDisplay(OnlineGroundPassState state)=>state!=null && state.catalogId==OnlineFullRoomBootstrap.Catalog && state.matchComplete
            && state.gameState==MatchManager.GameState.MatchEnded.ToString() && state.coin==null && state.shootout==null;
        public void Display(OnlineGroundPassState state)
        {
            bool visible=CanDisplay(state);gameObject.SetActive(visible);if(!visible)return;
            var settings=board.match.gameData.gameSettings;var home=state.homeStats;var away=state.awayStats;
            title.text=state.half>2?"Full time after extra time":"Full time";homeName.text=settings.homeTeamName;awayName.text=settings.awayTeamName;
            score.text=home.totalGoals+" - "+away.totalGoals;
            result.text=home.totalGoals==away.totalGoals?"Match drawn":(home.totalGoals>away.totalGoals?settings.homeTeamName:settings.awayTeamName)+" win";
            summary.text="Shots: "+home.totalShots+" - "+away.totalShots+"\nOn target: "+home.totalShotsOnTarget+" - "+away.totalShotsOnTarget+
                "\nCompleted passes: "+home.totalPassesCompleted+" - "+away.totalPassesCompleted+
                "\nYellow cards: "+home.totalYellowCards+" - "+away.totalYellowCards+"\nRed cards: "+home.totalRedCards+" - "+away.totalRedCards;
            leave.interactable=true;
        }
    }
}
