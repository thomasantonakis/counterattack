using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace CounterAttack.Online
{
    public sealed partial class OnlineRoomMenu
    {
        public GameObject nominationPanel;
        public Button nominationButton,nominationBackButton,nominationCommitButton;
        public Dropdown emergencyKeeper;
        public Text nominationReason;
        private OnlineGroundPassState keeperState;
        private OnlineGroundPassTokenState[] nominees=Array.Empty<OnlineGroundPassTokenState>();
        private long keeperPublication=-1;private OnlineTeam keeperTeam;
        private bool LocalEmergency(OnlineGroundPassState state)=>board.localTeam==OnlineTeam.Home?state.homeEmergencyKeeper:state.awayEmergencyKeeper;
        private void WireNominationControls()
        {
            if(nominationPanel==null)return;
            nominationButton.onClick.AddListener(()=>{if(nominationButton.interactable)nominationPanel.SetActive(true);});
            nominationBackButton.onClick.AddListener(()=>nominationPanel.SetActive(false));
            nominationCommitButton.onClick.AddListener(()=>
            {
                if(!nominationCommitButton.interactable)return;var token=Selected(emergencyKeeper,nominees);if(token==null)return;
                board.controls.NominateKeeper(token.runtime.jerseyNumber);nominationPanel.SetActive(false);
            });
            board.controls.LocalEscapeHandler=()=>
            {
                if(nominationPanel.activeSelf){nominationPanel.SetActive(false);return true;}
                if(substitutionPanel!=null && substitutionPanel.activeSelf){substitutionPanel.SetActive(false);return true;}
                return false;
            };
        }
        private void DisplayNomination(OnlineGroundPassState state)
        {
            if(nominationPanel==null)return;keeperState=state;bool emergency=LocalEmergency(state);
            nominationButton.gameObject.SetActive(emergency);substitutionButton.gameObject.SetActive(!emergency);
            if(!state.isSharedPaused || !emergency)nominationPanel.SetActive(false);
            nominationReason.text=state.keeperActionReason;
            if(keeperPublication==state.publicationSequence && keeperTeam==board.localTeam)return;
            keeperPublication=state.publicationSequence;keeperTeam=board.localTeam;string previous=Selected(emergencyKeeper,nominees)?.id;
            nominees=state.tokens.Where(t=>t.runtime!=null && t.runtime.teamSide==board.localTeam.ToString() && t.runtime.isPlaying && t.onBoard && !t.runtime.isGoalKeeper && !t.runtime.isSentOff)
                .OrderBy(t=>t.runtime.jerseyNumber).ToArray();Options(emergencyKeeper,nominees,previous);
        }
        private void RefreshNominationButtons()
        {
            if(nominationPanel==null)return;
            bool ready=keeperState!=null && LocalEmergency(keeperState) && board.controls.CanNominateKeeper && nominees.Length>0;
            nominationButton.interactable=nominationCommitButton.interactable=ready;emergencyKeeper.interactable=nominees.Length>0;
        }
    }
}
