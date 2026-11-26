using System;
using System.Linq;
using TMPro;
using UnityEngine;

namespace CounterAttack.Online
{
    public sealed partial class OnlineRoomMenu
    {
        [SerializeField] private PauseMenuManager roomPause;
        private TMP_Dropdown[] settingsFields;
        private (string value, string label)[][] settingOptions;
        private string[] displayedSettings;
        private bool settingsOpen;
        private bool hasPresentedPause;

        internal bool UsesRoomPause(OnlineGroundPassState state) => roomPause != null && !LocalEmergency(state);

        private void WireRoomPause()
        {
            if (roomPause == null) return;

            roomPause.resumeButton.onClick.AddListener(board.controls.Resume);
            roomPause.substitutionsButton.onClick.AddListener(() => substitutionButton.onClick.Invoke());
            roomPause.backToMainMenuButton.onClick.AddListener(Leave);
            roomPause.quitButton?.onClick.AddListener(() => roomPause.QuitConfirmation.ShowQuit(
                () => OnlineFrontendSession.Current?.Close()));
            roomPause.editSettings.onClick.AddListener(() =>
            {
                MenuAudioPlayer.Play(GameplayAudioPool.MenuAdvance);
                settingsOpen = true;
                RefreshRoomPause();
            });
            roomPause.editSettingsBackButton.onClick.AddListener(CloseRoomSettings);
            var previousEscape = board.controls.LocalEscapeHandler;
            board.controls.LocalEscapeHandler = () =>
            {
                if (roomPause.QuitConfirmation?.TryClose() == true) return true;
                if (!settingsOpen) return previousEscape?.Invoke() == true;
                CloseRoomSettings();
                return true;
            };

            settingsFields = new[] { roomPause.editTiebreakerDropdown, roomPause.editPlayerAssistanceDropdown,
                roomPause.editWeatherDropdown, roomPause.editFixtureLightingDropdown, roomPause.editBallColorDropdown,
                roomPause.editHomeKitDropdown, roomPause.editAwayKitDropdown, roomPause.editHomeGKKitDropdown,
                roomPause.editAwayGKKitDropdown };
            settingOptions = new (string value, string label)[settingsFields.Length][];
            for (int i = 0; i < settingsFields.Length; i++)
            {
                int index = i;
                settingsFields[i].onValueChanged.AddListener(value =>
                {
                    if (settingOptions[index] == null || value < 0 || value >= settingOptions[index].Length) return;
                    var options = settingOptions[index];
                    settingsFields[index].SetValueWithoutNotify(Math.Max(0,
                        Array.FindIndex(options, option => option.value == displayedSettings[index])));
                    settingsFields[index].RefreshShownValue();
                    board.controls.ChangeSetting((OnlineRoomSettings.Field)index, options[value].value);
                });
            }
        }

        private void CloseRoomSettings()
        {
            if (settingsOpen) MenuAudioPlayer.Play(GameplayAudioPool.MenuBack);
            settingsOpen = false;
            RefreshRoomPause();
        }

        private void PresentRoomPause(OnlineGroundPassState state)
        {
            if (roomPause == null) return;
            hasPresentedPause = state.isSharedPaused && UsesRoomPause(state);
            if (!hasPresentedPause) settingsOpen = false;
            var values = state.roomSettings;
            if (values != null && (displayedSettings == null || !displayedSettings.SequenceEqual(values)))
            {
                displayedSettings = (string[])values.Clone();
                for (int i = 0; i < settingsFields.Length; i++)
                {
                    var field = settingsFields[i];
                    settingOptions[i] = OnlineRoomSettings.Options((OnlineRoomSettings.Field)i, board.match.gameData.gameSettings).ToArray();
                    field.ClearOptions();
                    field.AddOptions(settingOptions[i].Select(option => option.label).ToList());
                    field.SetValueWithoutNotify(Math.Max(0, Array.FindIndex(settingOptions[i], option => option.value == values[i])));
                    field.RefreshShownValue();
                }
            }
            RefreshRoomPause();
        }

        private void RefreshRoomPause()
        {
            if (roomPause == null) return;
            bool visible = hasPresentedPause && !connectionHeld && !substitutionPanel.activeSelf && !nominationPanel.activeSelf;
            roomPause.pausePanel.SetActive(visible && !settingsOpen);
            roomPause.editSettingsPanel.SetActive(visible && settingsOpen);
            roomPause.resumeButton.interactable = resumeButton.interactable;
            roomPause.substitutionsButton.interactable = substitutionButton.interactable;
            roomPause.editSettings.interactable = displayedSettings != null;
            roomPause.pauseSaveStatusText.text = status.text;
            roomPause.logFileStatusText.text = board.controls.commandStatus.text;
            for (int i = 0; i < settingsFields.Length; i++)
            {
                settingsFields[i].interactable = board.controls.CanEditSettings && settingOptions[i]?.Length > 1
                    && ((OnlineRoomSettings.Field)i != OnlineRoomSettings.Field.Tiebreaker || board.match.CanChangeTiebreaker());
            }
        }
    }
}
