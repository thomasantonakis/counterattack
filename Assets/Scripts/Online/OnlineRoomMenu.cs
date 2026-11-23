using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace CounterAttack.Online
{
    public sealed partial class OnlineRoomMenu:MonoBehaviour
    {
        public OnlineGroundPassBoard board;
        public Button pauseButton,resumeButton,leaveButton;
        public Text status,settings;
        private bool connectionHeld;
        private void Awake()
        {
            pauseButton.onClick.AddListener(board.controls.Pause);
            resumeButton.onClick.AddListener(board.controls.Resume);
            leaveButton.onClick.AddListener(Leave);
            WireSubstitutionControls();
            WireNominationControls();
            WireRoomPause();
            WireAuthoredSubstitutions();
        }
        public void Display(OnlineGroundPassState state)
        {
            connectionHeld=state.connectionHeld;
            bool own=state.pauseRequester==board.localTeam.ToString();
            status.text=connectionHeld?"Reconnecting. The match is held.":own?"You paused the match. Resume when ready.":"The other manager paused the match. They can resume it.";
            if(!connectionHeld && state.keeperActionBlocked)status.text=state.keeperActionReason;
            var data=board.match.gameData.gameSettings;
            settings.text=$"{data.homeTeamName}  â€”  {data.awayTeamName}\nWeather: {data.weatherConditions}\nBall: {data.ballColor}\nAssistance: {AssistanceLabel(data.playerAssistance)}\nTiebreaker: {data.tiebreaker}";
            DisplaySubstitutions(state);
            DisplayNomination(state);
            PresentRoomPause(state);
            RefreshButtons();
        }
        private static string AssistanceLabel(int value)=>value==1?"Novice":value==2?"Intermediate":"Experienced";
        private void Update()=>RefreshButtons();
        private void RefreshButtons()
        {
            pauseButton.interactable=!connectionHeld && board.controls.pauseButton.interactable;
            resumeButton.interactable=!connectionHeld && board.controls.resumeButton.interactable;
            RefreshSubstitutionButtons();
            RefreshNominationButtons();
            RefreshRoomPause();
            RefreshAuthoredSubstitutions();
        }
        public void Leave()
        {
            if(OnlineFrontendSession.Current!=null)OnlineFrontendSession.Current.Close();
            else OnlineRoomSession.Current?.Dispose();
            SceneManager.LoadScene("MainMenu");
        }
    }
}
