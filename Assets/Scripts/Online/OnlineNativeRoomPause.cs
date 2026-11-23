using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CounterAttack.Online
{
    // Binds the existing Room pause menu to the shared control lane.
    internal sealed partial class OnlineNativeRoomPause : MonoBehaviour
    {
        private OnlineGroundPassBoard board;
        private PauseMenuManager view;
        private TMP_Dropdown[] fields;
        private (string value, string label)[][] options;
        private string[] values;
        private OnlineGroundPassState state;
        private bool editing;
        private string terminalMessage;
        private SubstitutionMenuView substitutions;
        private SubstitutionDropdownRowView localRow;
        private OnlineGroundPassTokenState[] outgoing = Array.Empty<OnlineGroundPassTokenState>();
        private OnlineGroundPassTokenState[] incoming = Array.Empty<OnlineGroundPassTokenState>();
        private bool selectingPlayers;
        private bool IsHome => board.localTeam == OnlineTeam.Home;
        private bool Nomination => IsHome ? state?.homeEmergencyKeeper == true : state?.awayEmergencyKeeper == true;

        internal void Bind(OnlineGroundPassBoard source, PauseMenuManager pause, SubstitutionMenuView substitutionView)
        {
            board = source;
            view = pause;
            substitutions = substitutionView;
            board.controls.LocalEscapeHandler = CloseSubstitutionsOrSettings;
            view.resumeButton.onClick.AddListener(board.controls.Resume);
            view.backToMainMenuButton.onClick.AddListener(() =>
            {
                OnlineFrontendSession.Current?.Close();
                SceneManager.LoadScene("MainMenu");
            });
            view.quitButton.onClick.AddListener(() =>
            {
                OnlineFrontendSession.Current?.Close();
                Application.Quit();
            });
            view.editSettings.onClick.AddListener(() => { editing = true; Refresh(); });
            view.editSettingsBackButton.onClick.AddListener(() => CloseSettings());
            view.saveMatchButton.interactable = false;
            view.saveMatchAsButton.interactable = false;
            view.captureLogFileButton.interactable = false;
            view.substitutionsButton.interactable = false;
            if (substitutions != null)
            {
                localRow = substitutions.GetRows(IsHome).FirstOrDefault();
                foreach (bool home in new[] { true, false })
                    foreach (var row in substitutions.GetRows(home))
                        row.gameObject.SetActive(row == localRow);
                localRow?.outgoingDropdown.onValueChanged.AddListener(_ =>
                {
                    if (selectingPlayers) RefreshIncoming();
                });
                view.substitutionsButton.onClick.AddListener(() =>
                {
                    if (state?.keeperActionBlocked != true)
                    {
                        board.controls.OpenSubstitutionReview();
                        return;
                    }
                    selectingPlayers = true;
                    RefreshSubstitutions();
                    Refresh();
                });
                substitutions.confirmButton.onClick.AddListener(() =>
                {
                    if (state?.substitutionReview != null) board.controls.FinishSubstitutionReview(true);
                    else SubmitRosterChoice();
                });
                substitutions.backButton.onClick.AddListener(() => CloseSubstitutionsOrSettings());
                BindReviewRows();
            }
            view.pausePanel.SetActive(false);
            view.editSettingsPanel.SetActive(false);
            fields = new[] { view.editTiebreakerDropdown, view.editPlayerAssistanceDropdown,
                view.editWeatherDropdown, view.editFixtureLightingDropdown, view.editBallColorDropdown,
                view.editHomeKitDropdown, view.editAwayKitDropdown, view.editHomeGKKitDropdown, view.editAwayGKKitDropdown };
            options = new (string value, string label)[fields.Length][];
            for (int i = 0; i < fields.Length; i++)
            {
                int index = i;
                fields[index].onValueChanged.AddListener(value =>
                {
                    if (options[index] == null || value < 0 || value >= options[index].Length) return;
                    board.controls.ChangeSetting((OnlineRoomSettings.Field)index, options[index][value].value);
                    SelectPublishedValue(index);
                    Refresh();
                });
            }
        }

        internal void Display(OnlineGroundPassState publication)
        {
            state = publication;
            if (state.roomSettings != null && (values == null || !values.SequenceEqual(state.roomSettings)))
            {
                values = (string[])state.roomSettings.Clone();
                for (int i = 0; i < fields.Length; i++)
                {
                    options[i] = OnlineRoomSettings.Options((OnlineRoomSettings.Field)i, board.match.gameData.gameSettings).ToArray();
                    fields[i].ClearOptions();
                    fields[i].AddOptions(options[i].Select(option => option.label).ToList());
                    SelectPublishedValue(i);
                }
            }
            Refresh();
            if (state.substitutionReview != null) DisplaySubstitutionReview(state.substitutionReview);
            else if (selectingPlayers) RefreshSubstitutions();
        }

        internal void PresentClosedMatch(string message)
        {
            terminalMessage = message;
            editing = false;
            board.rollPanels.HideAfterDeparture();
            board.match.coinTossPanel?.HidePublishedToss();
            if (board.nativeShootout != null) board.nativeShootout.gameObject.SetActive(false);
            if (RoomInputOwnership.IntroActive) RoomMatchIntro.Instance.Skip();
            Refresh();
        }

        private void SelectPublishedValue(int index)
        {
            fields[index].SetValueWithoutNotify(Math.Max(0, Array.FindIndex(options[index], option => option.value == values[index])));
            fields[index].RefreshShownValue();
        }

        private bool CloseSettings()
        {
            if (!editing) return false;
            editing = false;
            Refresh();
            return true;
        }

        private bool CloseSubstitutionsOrSettings()
        {
            if (state?.substitutionReview != null && terminalMessage == null)
            {
                if (substitutions.backButton.interactable) board.controls.FinishSubstitutionReview(false);
                return true;
            }
            if (!selectingPlayers) return CloseSettings();
            selectingPlayers = false;
            Refresh();
            return true;
        }

        private void RefreshSubstitutions()
        {
            if (localRow == null || state?.tokens == null) return;
            bool replacement = IsHome ? state.homeKeeperReplacement : state.awayKeeperReplacement;
            bool forcedKeeper = IsHome ? state.homeForcedKeeperSubstitution : state.awayForcedKeeperSubstitution;
            foreach (var row in reviewRows) row.view.gameObject.SetActive(row.view == localRow);
            substitutions.backButton.interactable = true;
            string previous = Selected(localRow.outgoingDropdown, outgoing)?.id;
            outgoing = state.tokens.Where(token => token.runtime != null && token.runtime.teamSide == board.localTeam.ToString()
                && token.runtime.isPlaying && token.onBoard && !token.runtime.isSentOff)
                .Where(token => Nomination || replacement ? !token.runtime.isGoalKeeper
                    : !forcedKeeper || token.runtime.isGoalKeeper && token.runtime.requiresSubstitution)
                .OrderBy(token => token.runtime.jerseyNumber).ToArray();
            SetOptions(localRow.outgoingDropdown, outgoing, previous);
            localRow.outgoingDropdown.interactable = true;
            localRow.incomingDropdown.interactable = !Nomination;
            substitutions.titleText.text = Nomination ? "Nominate goalkeeper" : "Substitutions";
            var settings = board.match.gameData.gameSettings;
            substitutions.homeTeamTitleText.text = settings.homeTeamName;
            substitutions.awayTeamTitleText.text = settings.awayTeamName;
            substitutions.homeSubsRemainingText.text = $"{board.match.GetSubstitutionsRemaining(true)} substitutions left";
            substitutions.awaySubsRemainingText.text = $"{board.match.GetSubstitutionsRemaining(false)} substitutions left";
            foreach (bool home in new[] { true, false })
            {
                var roster = board.tokens.Where(token => token.isHomeTeam == home && !token.isSentOff)
                    .OrderBy(token => token.jerseyNumber).ToArray();
                DisplayRoster(substitutions.GetPlayingRows(home), roster.Where(token => token.isPlaying).ToArray());
                DisplayRoster(substitutions.GetBenchRows(home), roster.Where(token => !token.isPlaying).ToArray());
            }
            RefreshIncoming();
        }

        private void DisplayRoster(TMP_Text[] rows, PlayerToken[] roster)
        {
            for (int i = 0; i < rows.Length; i++)
            {
                if (rows[i] is not TextMeshProUGUI label) continue;
                var token = i < roster.Length ? roster[i] : null;
                label.gameObject.SetActive(token != null);
                if (token == null) continue;
                label.text = $"{token.jerseyNumber}. {token.playerName}" + (token.IsGoalKeeper ? " (GK)" : "");
                substitutions.SetRosterStatus(label, SubstitutionMenuManager.RosterStatusColor(token));
            }
        }

        private void RefreshIncoming()
        {
            var selected = Selected(localRow.outgoingDropdown, outgoing);
            bool replacement = IsHome ? state.homeKeeperReplacement : state.awayKeeperReplacement;
            string previous = Selected(localRow.incomingDropdown, incoming)?.id;
            incoming = selected == null || Nomination ? Array.Empty<OnlineGroundPassTokenState>()
                : state.tokens.Where(token => token.runtime != null && token.runtime.teamSide == board.localTeam.ToString()
                    && !token.runtime.isPlaying && !token.runtime.wasSubbedOff && !token.runtime.isSentOff
                    && token.runtime.isGoalKeeper == (replacement || selected.runtime.isGoalKeeper))
                    .OrderBy(token => token.runtime.jerseyNumber).ToArray();
            SetOptions(localRow.incomingDropdown, incoming, previous);
            substitutions.confirmButton.interactable = selected != null && (Nomination ? board.controls.CanNominateKeeper
                : incoming.Length > 0 && board.controls.CanManageSubstitutions);
        }

        private static OnlineGroundPassTokenState Selected(TMP_Dropdown dropdown, OnlineGroundPassTokenState[] candidates)
            => dropdown.value >= 0 && dropdown.value < candidates.Length ? candidates[dropdown.value] : null;

        private static void SetOptions(TMP_Dropdown dropdown, OnlineGroundPassTokenState[] candidates, string previous)
        {
            var labels = candidates.Select(token => $"{token.runtime.jerseyNumber}. {token.runtime.playerName}").ToList();
            if (!dropdown.options.Select(option => option.text).SequenceEqual(labels))
            {
                dropdown.ClearOptions();
                dropdown.AddOptions(labels);
            }
            dropdown.SetValueWithoutNotify(Math.Max(0, Array.FindIndex(candidates, token => token.id == previous)));
            dropdown.RefreshShownValue();
        }

        private void SubmitRosterChoice()
        {
            var selected = Selected(localRow.outgoingDropdown, outgoing);
            if (selected == null) return;
            if (Nomination) board.controls.NominateKeeper(selected.runtime.jerseyNumber);
            else if (Selected(localRow.incomingDropdown, incoming) is { } replacement)
                board.controls.Substitute(selected.runtime.jerseyNumber, replacement.runtime.jerseyNumber);
            CloseSubstitutionsOrSettings();
        }

        private void Refresh()
        {
            bool paused = state?.isSharedPaused == true;
            bool reconnecting = state?.connectionHeld == true;
            bool reviewing = state?.substitutionReview != null && terminalMessage == null;
            if (!paused || reconnecting || terminalMessage != null || reviewing)
            {
                editing = false;
                selectingPlayers = false;
            }
            if (substitutions != null) substitutions.gameObject.SetActive(selectingPlayers || reviewing);
            view.pausePanel.SetActive((paused || reconnecting || terminalMessage != null) && !editing && !selectingPlayers && !reviewing);
            view.editSettingsPanel.SetActive(editing);
            view.resumeButton.interactable = terminalMessage == null && board.controls.CanResume;
            view.editSettings.interactable = terminalMessage == null && paused && !reconnecting && values != null;
            view.substitutionsButton.interactable = localRow != null && terminalMessage == null
                && (Nomination ? board.controls.CanNominateKeeper
                    : board.match.GetSubstitutionsRemaining(IsHome) > 0 && (state?.keeperActionBlocked == true
                        ? state.substitutionsAvailable && board.controls.CanManageSubstitutions
                        : board.controls.CanOpenSubstitutionReview));
            if (view.substitutionsButton.GetComponentInChildren<TMP_Text>(true) is { } label)
                label.text = Nomination ? "Nominate goalkeeper" : "Substitutions";
            view.pauseSaveStatusText.text = terminalMessage ?? (reconnecting ? "Reconnecting. The match is held."
                : state?.keeperActionBlocked == true ? state.keeperActionReason
                : state?.pauseRequester == board.localTeam.ToString() ? "You paused the match."
                : "The other manager paused the match.");
            for (int i = 0; i < fields.Length; i++)
                fields[i].interactable = board.controls.CanEditSettings && options[i]?.Length > 1
                    && (i != (int)OnlineRoomSettings.Field.Tiebreaker || board.match.CanChangeTiebreaker());
        }
    }
}
