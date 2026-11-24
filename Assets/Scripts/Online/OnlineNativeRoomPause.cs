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
        private bool namingSave;
        private string saveMessage;
        private Guid? selectedSave;
        private Guid durableMatchId = Guid.NewGuid();
        private Func<string> saveUnavailable;
        private Action<string> commitSave;
        private string terminalMessage;
        private SubstitutionMenuView substitutions;
        private SubstitutionDropdownRowView localRow;
        private OnlineGroundPassTokenState[] outgoing = Array.Empty<OnlineGroundPassTokenState>();
        private OnlineGroundPassTokenState[] incoming = Array.Empty<OnlineGroundPassTokenState>();
        private bool selectingPlayers;
        private string defaultSubstitutionConfirmLabel;
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
            view.saveMatchButton.onClick.AddListener(() => commitSave?.Invoke(null));
            view.saveMatchAsButton.onClick.AddListener(OpenSaveAs);
            view.saveAsSaveButton.onClick.AddListener(() => commitSave?.Invoke(view.saveNameInput.text.Trim()));
            view.saveAsCancelButton.onClick.AddListener(CloseSaveAs);
            view.captureLogFileButton.interactable = false;
            view.substitutionsButton.interactable = false;
            if (substitutions != null)
            {
                defaultSubstitutionConfirmLabel = substitutions.confirmButton.GetComponentInChildren<TMP_Text>(true)?.text;
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

        internal void BindCheckpointHost(OnlineMatchProtocol protocol, OnlineSharedPause pause,
            OnlineGroundPassAuthority authority, string guestAccount, OnlineCheckpointStore store, OnlineCompatibility compatibility)
        {
            string Guard()
            {
                var publication = state;
                return RoomSaveService.CanCaptureOnline(board.match, protocol, pause, publication, out string reason)
                    ? null : reason;
            }
            saveUnavailable = Guard;
            commitSave = requestedName =>
            {
                // Capture and write synchronously on the same authority thread as command admission.
                var current = pause.Capture(authority);
                if (!RoomSaveService.CanCaptureOnline(board.match, protocol, pause, current, out var reason))
                {
                    ShowSaveMessage(reason);
                    return;
                }
                if (requestedName == null && !selectedSave.HasValue)
                {
                    OpenSaveAs();
                    return;
                }
                if (requestedName != null && (requestedName.Length == 0 || requestedName.Length > 100))
                {
                    ShowSaveMessage("Enter a save name of up to 100 characters.");
                    return;
                }
                try
                {
                    var match = board.match;
                    match.EnsureGameplayEventLogInitialized();
                    var savedUtc = DateTime.UtcNow;
                    var snapshot = match.CreateRuntimeSnapshot();
                    snapshot.savedAtUtc = savedUtc.ToString("O");
                    var data = OnlineRoomLaunchData.Copy(match.gameData);
                    data.runtimeSnapshot = snapshot;
                    data.sourceSavePath = null;
                    data.lastSavedUtc = snapshot.savedAtUtc;
                    var checkpoint = new OnlineCheckpoint
                    {
                        matchId = durableMatchId,
                        checkpointId = Guid.NewGuid(),
                        savedUtc = savedUtc,
                        displayName = requestedName ?? store.Load(selectedSave.Value).displayName,
                        hostAccount = protocol.HostAccountId,
                        guestAccount = guestAccount,
                        protocol = compatibility.Protocol,
                        rules = compatibility.Rules,
                        catalog = compatibility.Catalog,
                        match = data
                    };
                    selectedSave = store.Save(checkpoint, requestedName == null ? selectedSave : null);
                    CloseSaveAs();
                    ShowSaveMessage($"Saved {checkpoint.displayName}.");
                }
                catch (Exception error) when (error is System.IO.IOException or UnauthorizedAccessException
                    or Newtonsoft.Json.JsonException or ArgumentException)
                {
                    Debug.LogWarning($"Online checkpoint write failed: {error.GetType().Name}");
                    ShowSaveMessage("Could not save the match. Try again.");
                }
            };
        }

        internal void UnbindCheckpointHost()
        {
            saveUnavailable = null;
            commitSave = null;
            CloseSaveAs();
        }

        private void OpenSaveAs()
        {
            if (saveUnavailable == null) return;
            if (saveUnavailable() is { } reason)
            {
                ShowSaveMessage(reason);
                return;
            }
            var settings = board.match.gameData.gameSettings;
            namingSave = true;
            view.saveNameInput.SetTextWithoutNotify($"{settings.homeTeamName} vs {settings.awayTeamName}");
            view.savePreviewText.text = "Save a new online checkpoint on this device.";
            view.saveModalStatusText.text = "";
            view.overwriteConfirmPanel.SetActive(false);
            Refresh();
            view.saveNameInput.ActivateInputField();
        }

        private void CloseSaveAs()
        {
            namingSave = false;
            Refresh();
        }

        private void ShowSaveMessage(string message)
        {
            if (namingSave) view.saveModalStatusText.text = message;
            else
            {
                saveMessage = message;
                Refresh();
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
            if (namingSave)
            {
                CloseSaveAs();
                return true;
            }
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
            if (substitutions.confirmButton.GetComponentInChildren<TMP_Text>(true) is { } confirmLabel)
                confirmLabel.text = defaultSubstitutionConfirmLabel;
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
                var roster = board.tokens.Where(token => token.isHomeTeam == home)
                    .OrderBy(token => token.jerseyNumber).ToArray();
                DisplayRoster(substitutions.GetPlayingRows(home), roster.Where(token => token.isPlaying || token.isSentOff).ToArray());
                DisplayRoster(substitutions.GetBenchRows(home), roster.Where(token => !token.isPlaying && !token.isSentOff).ToArray());
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
                if (token.isSentOff) label.text += " · Sent off";
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
                namingSave = false;
                saveMessage = null;
            }
            if (substitutions != null) substitutions.gameObject.SetActive(selectingPlayers || reviewing);
            bool showPause = (paused || reconnecting || terminalMessage != null) && !editing && !selectingPlayers && !reviewing && !namingSave;
            if (showPause && !view.pausePanel.activeSelf)
                view.pausePanel.transform.SetAsLastSibling();
            view.pausePanel.SetActive(showPause);
            view.saveAsOverlay.SetActive(namingSave);
            view.editSettingsPanel.SetActive(editing);
            string checkpointReason = saveUnavailable?.Invoke();
            bool canSave = showPause && saveUnavailable != null && checkpointReason == null && terminalMessage == null;
            view.saveMatchButton.interactable = canSave && selectedSave.HasValue;
            view.saveMatchAsButton.interactable = canSave;
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
                : saveMessage ?? checkpointReason ??
                (state?.pauseRequester == board.localTeam.ToString() ? "You paused the match."
                : "The other manager paused the match."));
            for (int i = 0; i < fields.Length; i++)
                fields[i].interactable = board.controls.CanEditSettings && options[i]?.Length > 1
                    && (i != (int)OnlineRoomSettings.Field.Tiebreaker || board.match.CanChangeTiebreaker());
        }
    }
}
