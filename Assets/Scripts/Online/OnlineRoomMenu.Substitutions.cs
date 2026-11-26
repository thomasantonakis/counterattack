using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace CounterAttack.Online
{
    public sealed partial class OnlineRoomMenu
    {
        public GameObject substitutionPanel;
        public Button substitutionButton,substitutionBackButton,substitutionCommitButton;
        public Dropdown outgoingPlayer,incomingPlayer;
        public Text substitutionStatus;
        private OnlineGroundPassState rosterState;
        private long rosterPublication=-1;
        private OnlineTeam rosterTeam;
        private Text substitutionCaption;
        private int captionRemaining=-1;
        private OnlineGroundPassTokenState[] outgoingOptions=Array.Empty<OnlineGroundPassTokenState>(),incomingOptions=Array.Empty<OnlineGroundPassTokenState>();
        private void WireSubstitutionControls()
        {
            if(substitutionPanel==null)return;
            substitutionCaption=substitutionButton.GetComponentInChildren<Text>(true);
            board.controls.LocalEscapeHandler=()=>{if(!substitutionPanel.activeSelf)return false;substitutionPanel.SetActive(false);return true;};
            substitutionButton.onClick.AddListener(()=>{if(substitutionButton.interactable)substitutionPanel.SetActive(true);});
            substitutionBackButton.onClick.AddListener(()=>substitutionPanel.SetActive(false));
            substitutionCommitButton.onClick.AddListener(CommitSubstitution);
            outgoingPlayer.onValueChanged.AddListener(_=>RefreshIncoming());
        }
        private static OnlineGroundPassTokenState Selected(Dropdown dropdown,OnlineGroundPassTokenState[] options)
            =>dropdown.value>=0 && dropdown.value<options.Length?options[dropdown.value]:null;
        private static void Options(Dropdown dropdown,OnlineGroundPassTokenState[] options,string previous)
        {
            dropdown.ClearOptions();dropdown.AddOptions(options.Select(t=>$"{t.runtime.jerseyNumber}. {t.runtime.playerName}").ToList());
            dropdown.SetValueWithoutNotify(Math.Max(0,Array.FindIndex(options,t=>t.id==previous)));dropdown.RefreshShownValue();
        }
        private void DisplaySubstitutions(OnlineGroundPassState state)
        {
            if(substitutionPanel==null)return;rosterState=state;
            if(!state.isSharedPaused)substitutionPanel.SetActive(false);
            if(rosterPublication==state.publicationSequence && rosterTeam==board.localTeam)return;
            rosterPublication=state.publicationSequence;rosterTeam=board.localTeam;
            string previous=Selected(outgoingPlayer,outgoingOptions)?.id;
            bool sentOffReplacement=board.localTeam==OnlineTeam.Home?state.homeKeeperReplacement:state.awayKeeperReplacement;
            bool forcedKeeper=board.localTeam==OnlineTeam.Home?state.homeForcedKeeperSubstitution:state.awayForcedKeeperSubstitution;
            outgoingOptions=state.tokens.Where(t=>t.runtime!=null && t.runtime.teamSide==board.localTeam.ToString() && t.runtime.isPlaying && t.onBoard && !t.runtime.isSentOff)
                .Where(t=>sentOffReplacement?!t.runtime.isGoalKeeper:!forcedKeeper || t.runtime.isGoalKeeper && t.runtime.requiresSubstitution)
                .OrderBy(t=>t.runtime.jerseyNumber).ToArray();
            Options(outgoingPlayer,outgoingOptions,previous);RefreshIncoming();
        }
        private void RefreshIncoming()
        {
            if(rosterState==null)return;var selected=Selected(outgoingPlayer,outgoingOptions);string previous=Selected(incomingPlayer,incomingOptions)?.id;
            bool replacement=board.localTeam==OnlineTeam.Home?rosterState.homeKeeperReplacement:rosterState.awayKeeperReplacement;
            incomingOptions=rosterState.tokens.Where(t=>selected!=null && t.runtime!=null && t.runtime.teamSide==board.localTeam.ToString()
                && !t.runtime.isPlaying && !t.runtime.wasSubbedOff && !t.runtime.isSentOff && (replacement?t.runtime.isGoalKeeper:t.runtime.isGoalKeeper==selected.runtime.isGoalKeeper))
                .OrderBy(t=>t.runtime.jerseyNumber).ToArray();
            Options(incomingPlayer,incomingOptions,previous);RefreshSubstitutionButtons();
        }
        private void RefreshSubstitutionButtons()
        {
            if(substitutionPanel==null)return;
            int remaining=board.match.GetSubstitutionsRemaining(board.localTeam==OnlineTeam.Home);
            bool ready=remaining>0 && rosterState?.substitutionsAvailable==true && !rosterState.connectionHeld && board.controls.CanManageSubstitutions;
            if(substitutionCaption!=null && captionRemaining!=remaining){captionRemaining=remaining;substitutionCaption.text=$"Substitutions ({remaining} left)";}
            substitutionButton.interactable=ready;substitutionCommitButton.interactable=ready && outgoingOptions.Length>0 && incomingOptions.Length>0;
            outgoingPlayer.interactable=outgoingOptions.Length>0;incomingPlayer.interactable=incomingOptions.Length>0;
            substitutionStatus.text=rosterState?.connectionHeld==true?"Reconnecting. Your choices are kept.":remaining==0?"All substitutions have been used.":incomingOptions.Length==0?"No eligible substitutes.":"Choose a playing player and a substitute.";
        }
        private void CommitSubstitution()
        {
            if(!substitutionCommitButton.interactable)return;
            var outgoing=Selected(outgoingPlayer,outgoingOptions);var incoming=Selected(incomingPlayer,incomingOptions);
            if(outgoing==null || incoming==null)return;
            board.controls.Substitute(outgoing.runtime.jerseyNumber,incoming.runtime.jerseyNumber);substitutionPanel.SetActive(false);
        }
    }
}
