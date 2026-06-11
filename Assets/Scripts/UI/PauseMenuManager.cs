using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class PauseMenuManager : MonoBehaviour
{
    private static readonly Color DangerColor = new(0.85f, 0.24f, 0.18f, 1f);
    private static readonly Color MutedTextColor = new(0.7f, 0.74f, 0.82f, 1f);

    public GameObject pausePanel;  // Reference to the Pause Panel
    public Button resumeButton;
    public Button saveMatchButton;
    public Button saveMatchAsButton;
    public Button substitutionsButton;
    public Button editSettings;
    public Button backToMainMenuButton;
    public Button quitButton;

    [Header("Save UI")]
    public TextMeshProUGUI pauseSaveStatusText;
    public GameObject saveAsOverlay;
    public TMP_InputField saveNameInput;
    public TextMeshProUGUI savePreviewText;
    public TextMeshProUGUI saveModalStatusText;
    public GameObject overwriteConfirmPanel;
    public Button saveAsSaveButton;
    public Button saveAsCancelButton;
    public Button overwriteReplaceButton;
    public Button overwriteCancelButton;

    [Header("Edit Settings UI")]
    public GameObject editSettingsPanel;
    public Button captureLogFileButton;
    public Button editSettingsBackButton;
    public TextMeshProUGUI logFileStatusText;
    public TMP_Dropdown editTiebreakerDropdown;
    public TMP_Dropdown editPlayerAssistanceDropdown;
    public TMP_Dropdown editWeatherDropdown;
    public TMP_Dropdown editBallColorDropdown;
    public TMP_Dropdown editHomeKitDropdown;
    public TMP_Dropdown editAwayKitDropdown;
    public TMP_Dropdown editHomeGKKitDropdown;
    public TMP_Dropdown editAwayGKKitDropdown;

    private bool isPaused = false;
    private bool isSaveAvailable = false;
    private string saveUnavailableReason = string.Empty;
    private SubstitutionMenuManager substitutionMenuManager;
    private bool suppressEditSettingsEvents = false;
    private IReadOnlyList<TokenKitPreset> editOutfieldKitPresets = Array.Empty<TokenKitPreset>();
    private IReadOnlyList<TokenKitPreset> editHomeKitPresets = Array.Empty<TokenKitPreset>();
    private IReadOnlyList<TokenKitPreset> editAwayKitPresets = Array.Empty<TokenKitPreset>();
    private IReadOnlyList<TokenKitPreset> editGkKitPresets = Array.Empty<TokenKitPreset>();

    void Awake()
    {
        // Set up button listeners
        resumeButton.onClick.AddListener(OnResumeButtonClicked);
        saveMatchButton.onClick.AddListener(OnSaveMatchButtonClicked);
        saveMatchAsButton.onClick.AddListener(OnSaveMatchAsButtonClicked);
        editSettings.onClick.AddListener(OnEditSettingsButtonClicked);
        backToMainMenuButton.onClick.AddListener(OnBackToMainMenuButtonClicked);
        quitButton.onClick.AddListener(OnQuitButtonClicked);
        substitutionMenuManager = GetComponent<SubstitutionMenuManager>();
        if (substitutionMenuManager == null)
        {
            substitutionMenuManager = gameObject.AddComponent<SubstitutionMenuManager>();
        }
        substitutionMenuManager.Configure(this, pausePanel, substitutionsButton, resumeButton);
        // Hide the pause menu on awake
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
        else
        {
            Debug.LogError("Pause panel reference is missing!");
        }

        EndGamePanelManager.EnsureScenePanel();
        BindSaveUiReferences();
        ConfigureSaveUi();
        BindEditSettingsUiReferences();
        EnsureEditSettingsUi();
        ConfigureEditSettingsUi();
    }

    void Update()
    {
        // Toggle the pause menu when the Escape key is pressed
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            if (editSettingsPanel != null && editSettingsPanel.activeSelf)
            {
                CloseEditSettingsPanel();
                return;
            }

            if (substitutionMenuManager != null && substitutionMenuManager.IsOpen)
            {
                substitutionMenuManager.CloseToPauseMenu();
                return;
            }

            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }

        if (isPaused)
        {
            RefreshSaveButtonAvailability();
        }
    }

    private void OnResumeButtonClicked()
    {
        LogUiClick("pause_menu", "resume_button", "resume_match");
        ResumeGame();
    }

    private void OnSaveMatchButtonClicked()
    {
        RefreshSaveButtonAvailability();
        LogUiClick(
            "pause_menu",
            "save_match_button",
            "save_in_place",
            ("saveAvailable", isSaveAvailable),
            ("saveUnavailableReason", saveUnavailableReason));
        SaveMatch();
    }

    private void OnSaveMatchAsButtonClicked()
    {
        RefreshSaveButtonAvailability();
        LogUiClick(
            "pause_menu",
            "save_match_as_button",
            "open_save_as",
            ("saveAvailable", isSaveAvailable),
            ("saveUnavailableReason", saveUnavailableReason));
        SaveMatchAs();
    }

    private void OnEditSettingsButtonClicked()
    {
        LogUiClick("pause_menu", "edit_settings_button", "open_edit_settings");
        EditSettings();
    }

    private void OnBackToMainMenuButtonClicked()
    {
        LogUiClick("pause_menu", "back_to_main_menu_button", "back_to_main_menu");
        BackToMainMenuInGame();
    }

    private void OnQuitButtonClicked()
    {
        LogUiClick("pause_menu", "quit_button", "quit_game");
        QuitMatch();
    }

    private void OnSaveAsSaveButtonClicked()
    {
        LogUiClick(
            "save_as_panel",
            "save_button",
            "save_as",
            ("requestedName", GetRequestedSaveAsName()),
            ("overwrite", false));
        TrySaveAs(overwrite: false);
    }

    private void OnSaveAsCancelButtonClicked()
    {
        LogUiClick(
            "save_as_panel",
            "cancel_button",
            "cancel_save_as",
            ("requestedName", GetRequestedSaveAsName()));
        CancelSaveAs();
    }

    private void OnOverwriteReplaceButtonClicked()
    {
        LogUiClick(
            "save_as_overwrite_panel",
            "replace_button",
            "confirm_overwrite",
            ("requestedName", GetRequestedSaveAsName()),
            ("overwrite", true));
        TrySaveAs(overwrite: true);
    }

    private void OnOverwriteCancelButtonClicked()
    {
        LogUiClick(
            "save_as_overwrite_panel",
            "cancel_button",
            "cancel_overwrite",
            ("requestedName", GetRequestedSaveAsName()));
        SetOverwriteConfirmActive(false);
    }

    private void OnCaptureLogFileButtonClicked()
    {
        LogUiClick("edit_settings_panel", "capture_logfile_button", "capture_logfile");
        CaptureLogFile();
    }

    private void OnEditSettingsBackButtonClicked()
    {
        LogUiClick("edit_settings_panel", "back_button", "back_to_pause_menu");
        CloseEditSettingsPanel();
    }

    private void LogUiClick(
        string panel,
        string control,
        string action,
        params (string Key, object Value)[] details)
    {
        MatchManager.Instance?.RecordUiClick(panel, control, action, "clicked", details);
    }

    public void PauseGame()
    {
        // Debug to see if the panel is being set active
        // Debug.Log("Pausing game and showing pause panel");
        // Display the pause menu
        if (editSettingsPanel != null)
        {
            editSettingsPanel.SetActive(false);
        }

        pausePanel.SetActive(true);
        MatchManager.Instance?.SetPauseMenuOpen(true);
        RefreshSaveButtonAvailability();
        ClearPauseSaveStatus();
        substitutionMenuManager?.RefreshOpenButtonState();
        // Debug to confirm visibility
        // Debug.Log($"Pause panel active status: {pausePanel.activeSelf}");
        isPaused = true;
    }

    public void OpenSubstitutionsForForcedSubstitution()
    {
        PauseGame();
        substitutionMenuManager?.OpenSubstitutionMenu();
    }

    public void ResumeGame()
    {
        if (MatchManager.Instance != null && MatchManager.Instance.HasPlayingTokensRequiringSubstitution())
        {
            Debug.LogWarning("Cannot resume while a double-injured player remains on the pitch.");
            PauseGame();
            substitutionMenuManager?.OpenSubstitutionMenu();
            return;
        }

        // Debug to see if the panel is being hidden
        // Debug.Log("Resuming game and hiding pause panel");
        // Hide the pause menu
        ClearPauseSaveStatus();
        ClearLogFileStatus();
        if (editSettingsPanel != null)
        {
            editSettingsPanel.SetActive(false);
        }

        pausePanel.SetActive(false);
        if (substitutionMenuManager != null && substitutionMenuManager.IsOpen)
        {
            substitutionMenuManager.CloseToPauseMenu();
            pausePanel.SetActive(false);
        }
        MatchManager.Instance?.SetPauseMenuOpen(false);
        // Debug to confirm visibility
        // Debug.Log($"Pause panel active status: {pausePanel.activeSelf}");
        isPaused = false;
    }

    public void SaveMatch()
    {
        RefreshSaveButtonAvailability();
        if (!isSaveAvailable)
        {
            ShowPauseSaveStatus(saveUnavailableReason, isError: true);
            return;
        }

        if (RoomSaveService.SaveInPlace(MatchManager.Instance, out string savedPath, out string message))
        {
            Debug.Log(message);
            ShowPauseSaveStatus(message, isError: false);
            ResumeGame();
            return;
        }

        Debug.LogWarning(message);
        ShowPauseSaveStatus(message, isError: true);
    }

    public void SaveMatchAs()
    {
        RefreshSaveButtonAvailability();
        if (!isSaveAvailable)
        {
            ShowPauseSaveStatus(saveUnavailableReason, isError: true);
            return;
        }

        if (!EnsureSaveAsPanelReady())
        {
            ShowPauseSaveStatus("Save Game As UI is not configured in the scene.", isError: true);
            return;
        }

        saveNameInput.SetTextWithoutNotify(BuildDefaultSaveAsName());
        UpdateSaveAsPreview(saveNameInput.text);
        saveModalStatusText.text = string.Empty;
        SetOverwriteConfirmActive(false);
        saveAsOverlay.SetActive(true);
    }

    private void TrySaveAs(bool overwrite)
    {
        RefreshSaveButtonAvailability();
        if (!isSaveAvailable)
        {
            if (saveModalStatusText != null)
            {
                saveModalStatusText.color = DangerColor;
                saveModalStatusText.text = saveUnavailableReason;
            }
            else
            {
                ShowPauseSaveStatus(saveUnavailableReason, isError: true);
            }

            return;
        }

        if (!EnsureSaveAsPanelReady())
        {
            ShowPauseSaveStatus("Save Game As UI is not configured in the scene.", isError: true);
            return;
        }

        string requestedName = saveNameInput != null ? saveNameInput.text : string.Empty;
        if (!overwrite && RoomSaveService.SaveFileExistsForName(requestedName))
        {
            SetOverwriteConfirmActive(true);
            saveModalStatusText.color = DangerColor;
            saveModalStatusText.text = "A save with this name already exists.";
            return;
        }

        if (RoomSaveService.SaveAs(MatchManager.Instance, requestedName, overwrite, out string savedPath, out string message))
        {
            Debug.Log(message);
            saveAsOverlay.SetActive(false);
            ShowPauseSaveStatus(message, isError: false);
            ResumeGame();
            return;
        }

        Debug.LogWarning(message);
        saveModalStatusText.color = DangerColor;
        saveModalStatusText.text = message;
        RefreshSaveButtonAvailability();
    }

    private void CancelSaveAs()
    {
        if (saveAsOverlay != null)
        {
            saveAsOverlay.SetActive(false);
        }
    }

    private string GetRequestedSaveAsName()
    {
        return saveNameInput != null ? saveNameInput.text : string.Empty;
    }

    private string BuildDefaultSaveAsName()
    {
        MatchManager matchManager = MatchManager.Instance;
        string home = matchManager?.gameData?.gameSettings?.homeTeamName ?? "Home";
        string away = matchManager?.gameData?.gameSettings?.awayTeamName ?? "Away";
        return $"{home} vs {away} {DateTime.Now:yyyy-MM-dd HH-mm}";
    }

    private void UpdateSaveAsPreview(string requestedName)
    {
        if (savePreviewText == null)
        {
            return;
        }

        SetOverwriteConfirmActive(false);
        string path = RoomSaveService.BuildSaveAsPath(requestedName);
        if (string.IsNullOrWhiteSpace(path))
        {
            savePreviewText.text = "Filename preview: -";
            return;
        }

        string fileName = Path.GetFileName(path);
        string existsText = File.Exists(path) ? " (exists)" : string.Empty;
        savePreviewText.text = $"Filename preview: {fileName}{existsText}";
    }

    private void BindSaveUiReferences()
    {
        if (pausePanel != null && pauseSaveStatusText == null)
        {
            pauseSaveStatusText = FindChildComponent<TextMeshProUGUI>(pausePanel.transform, "SaveStatusText");
        }

        if (saveAsOverlay == null)
        {
            saveAsOverlay = FindChildObject(transform.root, "SaveAsOverlay");
        }

        if (saveAsOverlay == null)
        {
            return;
        }

        Transform overlayTransform = saveAsOverlay.transform;
        saveNameInput ??= FindChildComponent<TMP_InputField>(overlayTransform, "SaveNameInput");
        savePreviewText ??= FindChildComponent<TextMeshProUGUI>(overlayTransform, "FilenamePreview");
        saveModalStatusText ??= FindChildComponent<TextMeshProUGUI>(overlayTransform, "SaveStatus");
        overwriteConfirmPanel ??= FindChildObject(overlayTransform, "OverwriteConfirm");
        saveAsSaveButton ??= FindChildComponent<Button>(overlayTransform, "SaveButton");
        saveAsCancelButton ??= FindChildComponent<Button>(overlayTransform, "CancelButton");
        overwriteReplaceButton ??= FindChildComponent<Button>(overlayTransform, "ReplaceButton");
        overwriteCancelButton ??= FindChildComponent<Button>(overlayTransform, "CancelOverwriteButton");
    }

    private void BindEditSettingsUiReferences()
    {
        if (editSettingsPanel == null)
        {
            editSettingsPanel = FindChildObject(transform.root, "EditSettingsPanel");
        }

        if (editSettingsPanel == null)
        {
            return;
        }

        Transform panelTransform = editSettingsPanel.transform;
        captureLogFileButton ??= FindChildComponent<Button>(panelTransform, "CaptureLogFileButton");
        editSettingsBackButton ??= FindChildComponent<Button>(panelTransform, "EditSettingsBackButton");
        logFileStatusText ??= FindChildComponent<TextMeshProUGUI>(panelTransform, "LogFileStatusText");
        editTiebreakerDropdown ??= FindChildComponent<TMP_Dropdown>(panelTransform, "EditTiebreakerDropdown");
        editPlayerAssistanceDropdown ??= FindChildComponent<TMP_Dropdown>(panelTransform, "EditPlayerAssistanceDropdown");
        editWeatherDropdown ??= FindChildComponent<TMP_Dropdown>(panelTransform, "EditWeatherDropdown");
        editBallColorDropdown ??= FindChildComponent<TMP_Dropdown>(panelTransform, "EditBallColorDropdown");
        editHomeKitDropdown ??= FindChildComponent<TMP_Dropdown>(panelTransform, "EditHomeKitDropdown");
        editAwayKitDropdown ??= FindChildComponent<TMP_Dropdown>(panelTransform, "EditAwayKitDropdown");
        editHomeGKKitDropdown ??= FindChildComponent<TMP_Dropdown>(panelTransform, "EditHomeGKKitDropdown");
        editAwayGKKitDropdown ??= FindChildComponent<TMP_Dropdown>(panelTransform, "EditAwayGKKitDropdown");
    }

    private void ConfigureSaveUi()
    {
        if (saveNameInput != null)
        {
            saveNameInput.onValueChanged.AddListener(UpdateSaveAsPreview);
        }

        if (saveAsSaveButton != null)
        {
            saveAsSaveButton.onClick.AddListener(OnSaveAsSaveButtonClicked);
        }

        if (saveAsCancelButton != null)
        {
            saveAsCancelButton.onClick.AddListener(OnSaveAsCancelButtonClicked);
        }

        if (overwriteReplaceButton != null)
        {
            overwriteReplaceButton.onClick.AddListener(OnOverwriteReplaceButtonClicked);
        }

        if (overwriteCancelButton != null)
        {
            overwriteCancelButton.onClick.AddListener(OnOverwriteCancelButtonClicked);
        }

        SetOverwriteConfirmActive(false);

        if (saveAsOverlay != null)
        {
            saveAsOverlay.SetActive(false);
        }

        RefreshSaveButtonAvailability();
    }

    private void EnsureEditSettingsUi()
    {
        BindEditSettingsUiReferences();

        if (!HasEditSettingsUiReferences(out string missingReferences))
        {
            Debug.LogError($"Edit Settings UI is missing scene-authored references: {missingReferences}. Run CounterAttack/Room/Ensure Pause Menu Edit Settings Panel in edit mode.");
        }

        if (editSettingsPanel != null)
        {
            editSettingsPanel.SetActive(false);
        }
    }

    private bool HasEditSettingsUiReferences(out string missingReferences)
    {
        List<string> missing = new();
        AddMissingReference(missing, editSettingsPanel, nameof(editSettingsPanel));
        AddMissingReference(missing, captureLogFileButton, nameof(captureLogFileButton));
        AddMissingReference(missing, editSettingsBackButton, nameof(editSettingsBackButton));
        AddMissingReference(missing, logFileStatusText, nameof(logFileStatusText));
        AddMissingReference(missing, editTiebreakerDropdown, nameof(editTiebreakerDropdown));
        AddMissingReference(missing, editPlayerAssistanceDropdown, nameof(editPlayerAssistanceDropdown));
        AddMissingReference(missing, editWeatherDropdown, nameof(editWeatherDropdown));
        AddMissingReference(missing, editBallColorDropdown, nameof(editBallColorDropdown));
        AddMissingReference(missing, editHomeKitDropdown, nameof(editHomeKitDropdown));
        AddMissingReference(missing, editAwayKitDropdown, nameof(editAwayKitDropdown));
        AddMissingReference(missing, editHomeGKKitDropdown, nameof(editHomeGKKitDropdown));
        AddMissingReference(missing, editAwayGKKitDropdown, nameof(editAwayGKKitDropdown));

        missingReferences = string.Join(", ", missing);
        return missing.Count == 0;
    }

    private static void AddMissingReference<T>(List<string> missing, T reference, string fieldName) where T : UnityEngine.Object
    {
        if (reference == null)
        {
            missing.Add(fieldName);
        }
    }

    private void ConfigureEditSettingsUi()
    {
        ConfigureEditSettingsDropdownListeners();

        if (captureLogFileButton != null)
        {
            captureLogFileButton.onClick.AddListener(OnCaptureLogFileButtonClicked);
        }

        if (editSettingsBackButton != null)
        {
            editSettingsBackButton.onClick.AddListener(OnEditSettingsBackButtonClicked);
        }

        ClearLogFileStatus();
    }

    private void ConfigureEditSettingsDropdownListeners()
    {
        ConfigureEditSettingsDropdownListener(editTiebreakerDropdown);
        ConfigureEditSettingsDropdownListener(editPlayerAssistanceDropdown);
        ConfigureEditSettingsDropdownListener(editWeatherDropdown);
        ConfigureEditSettingsDropdownListener(editBallColorDropdown);
        ConfigureEditSettingsDropdownListener(editHomeKitDropdown);
        ConfigureEditSettingsDropdownListener(editAwayKitDropdown);
        ConfigureEditSettingsDropdownListener(editHomeGKKitDropdown);
        ConfigureEditSettingsDropdownListener(editAwayGKKitDropdown);
    }

    private void ConfigureEditSettingsDropdownListener(TMP_Dropdown dropdown)
    {
        if (dropdown == null)
        {
            return;
        }

        dropdown.onValueChanged.RemoveListener(OnEditSettingsDropdownChanged);
        dropdown.onValueChanged.AddListener(OnEditSettingsDropdownChanged);
    }

    private void ShowPauseSaveStatus(string message, bool isError)
    {
        BindSaveUiReferences();
        if (pauseSaveStatusText == null)
        {
            return;
        }

        pauseSaveStatusText.color = isError ? DangerColor : MutedTextColor;
        pauseSaveStatusText.text = message;
    }

    private void ClearPauseSaveStatus()
    {
        BindSaveUiReferences();
        if (pauseSaveStatusText != null)
        {
            pauseSaveStatusText.text = string.Empty;
        }
    }

    private void ShowLogFileStatus(string message, bool isError)
    {
        BindEditSettingsUiReferences();
        if (logFileStatusText == null)
        {
            return;
        }

        logFileStatusText.color = isError ? DangerColor : MutedTextColor;
        logFileStatusText.text = message;
    }

    private void ClearLogFileStatus()
    {
        BindEditSettingsUiReferences();
        if (logFileStatusText != null)
        {
            logFileStatusText.text = string.Empty;
        }
    }

    private void RefreshSaveButtonAvailability()
    {
        MatchManager matchManager = MatchManager.Instance;
        isSaveAvailable = matchManager != null && matchManager.CanCreateRuntimeSnapshot(out saveUnavailableReason);
        if (matchManager == null)
        {
            saveUnavailableReason = "MatchManager is not available.";
        }

        if (saveMatchButton != null)
        {
            saveMatchButton.interactable = isSaveAvailable;
        }

        if (saveMatchAsButton != null)
        {
            saveMatchAsButton.interactable = isSaveAvailable;
        }
    }

    private void SetOverwriteConfirmActive(bool isActive)
    {
        if (overwriteConfirmPanel != null)
        {
            overwriteConfirmPanel.SetActive(isActive);
        }

        if (saveAsSaveButton != null)
        {
            saveAsSaveButton.gameObject.SetActive(!isActive);
        }

        if (saveAsCancelButton != null)
        {
            saveAsCancelButton.gameObject.SetActive(!isActive);
        }
    }

    private bool EnsureSaveAsPanelReady()
    {
        BindSaveUiReferences();
        bool ready = saveAsOverlay != null
            && saveNameInput != null
            && savePreviewText != null
            && saveModalStatusText != null
            && overwriteConfirmPanel != null
            && saveAsSaveButton != null
            && saveAsCancelButton != null
            && overwriteReplaceButton != null
            && overwriteCancelButton != null;

        if (!ready)
        {
            Debug.LogError("Save Game As UI is missing scene-authored references. Check SaveAsOverlay under Canvas.");
        }

        return ready;
    }

    private void RefreshEditSettingsControls()
    {
        MatchManager matchManager = MatchManager.Instance;
        MatchManager.GameSettings settings = matchManager?.gameData?.gameSettings;
        if (settings == null)
        {
            ShowLogFileStatus("Match settings are not available.", isError: true);
            return;
        }

        suppressEditSettingsEvents = true;
        try
        {
            RefreshEditKitPresetLists(settings);
            PopulateEditDropdown(editTiebreakerDropdown, GetTiebreakerOptions(settings), settings.tiebreaker);
            PopulateEditDropdown(editPlayerAssistanceDropdown, new List<string> { "1", "2", "3" }, settings.playerAssistance.ToString());
            PopulateEditDropdown(editWeatherDropdown, new List<string> { "Clear", "Rain", "Snow" }, settings.weatherConditions);
            string normalizedBallColor = GetValidBallColorForWeather(settings.weatherConditions, settings.ballColor);
            PopulateEditDropdown(editBallColorDropdown, GetBallColorOptions(settings.weatherConditions), normalizedBallColor);
            PopulateEditKitDropdown(editHomeKitDropdown, editHomeKitPresets, settings.homeKit);
            PopulateEditKitDropdown(editAwayKitDropdown, editAwayKitPresets, settings.awayKit);
            PopulateEditKitDropdown(editHomeGKKitDropdown, editGkKitPresets, settings.homeGKKit);
            PopulateEditKitDropdown(editAwayGKKitDropdown, editGkKitPresets, settings.awayGKKit);

            if (!string.Equals(settings.ballColor, normalizedBallColor, StringComparison.OrdinalIgnoreCase))
            {
                settings.ballColor = normalizedBallColor;
                matchManager?.ApplyLiveGameSettingsChanges();
            }

            bool canChangeTiebreaker = matchManager == null || matchManager.CanChangeTiebreaker();
            if (editTiebreakerDropdown != null)
            {
                editTiebreakerDropdown.interactable = canChangeTiebreaker;
            }

            if (!canChangeTiebreaker)
            {
                ShowLogFileStatus("Tiebreaker is locked after full regulation time has ended.", isError: false);
            }
        }
        finally
        {
            suppressEditSettingsEvents = false;
        }

        ValidateEditKitSelection(out string validationMessage);
        if (!string.IsNullOrWhiteSpace(validationMessage))
        {
            ShowLogFileStatus(validationMessage, isError: true);
        }
    }

    private void RefreshEditKitPresetLists(MatchManager.GameSettings settings)
    {
        List<TokenKitPreset> allPresets = TokenKitCatalog.GetAllPresets().Where(preset => preset != null).ToList();
        editOutfieldKitPresets = allPresets
            .Where(preset => !IsGoalkeeperKitPreset(preset))
            .ToList();
        editGkKitPresets = allPresets
            .Where(IsGoalkeeperKitPreset)
            .ToList();

        if (IsInternationalMatch(settings))
        {
            editHomeKitPresets = GetInternationalTeamKitPresets(settings.homeTeamName);
            editAwayKitPresets = GetInternationalTeamKitPresets(settings.awayTeamName);
        }
        else
        {
            editHomeKitPresets = editOutfieldKitPresets;
            editAwayKitPresets = editOutfieldKitPresets;
        }
    }

    private static bool IsGoalkeeperKitPreset(TokenKitPreset preset)
    {
        return preset != null && preset.Id.StartsWith("gk", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsInternationalMatch(MatchManager.GameSettings settings)
    {
        return settings != null
            && string.Equals(settings.matchType, "International", StringComparison.OrdinalIgnoreCase);
    }

    private IReadOnlyList<TokenKitPreset> GetInternationalTeamKitPresets(string teamName)
    {
        if (string.IsNullOrWhiteSpace(teamName))
        {
            return editOutfieldKitPresets;
        }

        List<TokenKitPreset> teamKits = editOutfieldKitPresets
            .Where(preset => string.Equals(preset.DisplayName, teamName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(preset.DisplayName, $"{teamName} Away", StringComparison.OrdinalIgnoreCase))
            .ToList();

        return teamKits.Count > 0 ? teamKits : editOutfieldKitPresets;
    }

    private static List<string> GetTiebreakerOptions(MatchManager.GameSettings settings)
    {
        bool fullLengthTwoHalfMatch = settings != null
            && Mathf.Clamp(settings.halfDuration, 15, 60) == 45
            && Mathf.Clamp(settings.numberOfHalfs, 1, 2) == 2;

        return fullLengthTwoHalfMatch
            ? new List<string> { "Extra Time & Penalties", "Penalties", "Extra Time", "None" }
            : new List<string> { "Penalties", "None" };
    }

    private static List<string> GetBallColorOptions(string weather)
    {
        return string.Equals(weather, "Snow", StringComparison.OrdinalIgnoreCase)
            ? new List<string> { "Orange" }
            : new List<string> { "White", "Orange", "Yellow" };
    }

    private static string GetValidBallColorForWeather(string weather, string selectedColor)
    {
        List<string> options = GetBallColorOptions(weather);
        int selectedIndex = options.FindIndex(option => string.Equals(option, selectedColor, StringComparison.OrdinalIgnoreCase));
        return options[selectedIndex >= 0 ? selectedIndex : 0];
    }

    private static void PopulateEditDropdown(TMP_Dropdown dropdown, List<string> options, string selectedValue)
    {
        if (dropdown == null)
        {
            return;
        }

        dropdown.ClearOptions();
        dropdown.AddOptions(options);
        int selectedIndex = options.FindIndex(option => string.Equals(option, selectedValue, StringComparison.OrdinalIgnoreCase));
        dropdown.SetValueWithoutNotify(selectedIndex >= 0 ? selectedIndex : 0);
        dropdown.RefreshShownValue();
    }

    private static void PopulateEditKitDropdown(TMP_Dropdown dropdown, IReadOnlyList<TokenKitPreset> presets, string selectedKit)
    {
        if (dropdown == null)
        {
            return;
        }

        List<string> options = presets?.Select(preset => preset.DisplayName).ToList() ?? new List<string>();
        dropdown.ClearOptions();
        dropdown.AddOptions(options);
        int selectedIndex = FindEditKitIndex(presets, selectedKit);
        dropdown.SetValueWithoutNotify(selectedIndex >= 0 ? selectedIndex : 0);
        dropdown.RefreshShownValue();
    }

    private static int FindEditKitIndex(IReadOnlyList<TokenKitPreset> presets, string selectedKit)
    {
        if (presets == null || presets.Count == 0 || string.IsNullOrWhiteSpace(selectedKit))
        {
            return -1;
        }

        TokenKitPreset selectedPreset = TokenKitCatalog.GetPresetByIdOrAlias(selectedKit);
        for (int i = 0; i < presets.Count; i++)
        {
            TokenKitPreset preset = presets[i];
            if (preset == null)
            {
                continue;
            }

            if (selectedPreset != null && string.Equals(preset.Id, selectedPreset.Id, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }

            if (string.Equals(preset.DisplayName, selectedKit, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private void OnEditSettingsDropdownChanged(int _)
    {
        if (suppressEditSettingsEvents)
        {
            return;
        }

        ApplyEditSettingsFromControls();
    }

    private void ApplyEditSettingsFromControls()
    {
        MatchManager matchManager = MatchManager.Instance;
        MatchManager.GameSettings settings = matchManager?.gameData?.gameSettings;
        if (settings == null)
        {
            ShowLogFileStatus("Match settings are not available.", isError: true);
            return;
        }

        suppressEditSettingsEvents = true;
        try
        {
            string selectedWeather = GetDropdownText(editWeatherDropdown);
            string selectedBallColor = GetValidBallColorForWeather(selectedWeather, GetDropdownText(editBallColorDropdown));
            PopulateEditDropdown(editBallColorDropdown, GetBallColorOptions(selectedWeather), selectedBallColor);
            RefreshEditKitPresetLists(settings);
        }
        finally
        {
            suppressEditSettingsEvents = false;
        }

        if (!ValidateEditKitSelection(out string validationMessage))
        {
            ShowLogFileStatus(validationMessage, isError: true);
            return;
        }

        bool tiebreakerLocked = matchManager != null && !matchManager.CanChangeTiebreaker();
        if (!tiebreakerLocked)
        {
            settings.tiebreaker = GetDropdownText(editTiebreakerDropdown);
        }

        if (int.TryParse(GetDropdownText(editPlayerAssistanceDropdown), out int playerAssistance))
        {
            settings.playerAssistance = Mathf.Clamp(playerAssistance, 1, 3);
        }

        string appliedWeather = GetDropdownText(editWeatherDropdown);
        settings.weatherConditions = appliedWeather;
        settings.ballColor = GetValidBallColorForWeather(appliedWeather, GetDropdownText(editBallColorDropdown));
        settings.homeKit = GetSelectedEditKitDisplayName(editHomeKitDropdown, editHomeKitPresets);
        settings.awayKit = GetSelectedEditKitDisplayName(editAwayKitDropdown, editAwayKitPresets);
        settings.homeGKKit = GetSelectedEditKitDisplayName(editHomeGKKitDropdown, editGkKitPresets);
        settings.awayGKKit = GetSelectedEditKitDisplayName(editAwayGKKitDropdown, editGkKitPresets);

        matchManager?.ApplyLiveGameSettingsChanges();
        ShowLogFileStatus(
            tiebreakerLocked
                ? "Settings updated. Tiebreaker remains locked after full regulation time."
                : "Settings updated.",
            isError: false);
    }

    private bool ValidateEditKitSelection(out string validationMessage)
    {
        validationMessage = string.Empty;
        TokenKitPreset home = GetSelectedEditKitPreset(editHomeKitDropdown, editHomeKitPresets);
        TokenKitPreset away = GetSelectedEditKitPreset(editAwayKitDropdown, editAwayKitPresets);
        TokenKitPreset homeGk = GetSelectedEditKitPreset(editHomeGKKitDropdown, editGkKitPresets);
        TokenKitPreset awayGk = GetSelectedEditKitPreset(editAwayGKKitDropdown, editGkKitPresets);

        validationMessage = BuildFirstKitClashMessage(home, away, homeGk, awayGk);
        return string.IsNullOrWhiteSpace(validationMessage);
    }

    private string BuildFirstKitClashMessage(TokenKitPreset home, TokenKitPreset away, TokenKitPreset homeGk, TokenKitPreset awayGk)
    {
        string message = BuildKitClashMessage(home, away, "Home", "Away");
        if (!string.IsNullOrWhiteSpace(message)) return message;

        if (homeGk != null && awayGk != null && string.Equals(homeGk.Id, awayGk.Id, StringComparison.OrdinalIgnoreCase))
        {
            return "Home and away goalkeepers cannot use the same kit.";
        }

        message = BuildKitClashMessage(homeGk, awayGk, "Home GK", "Away GK");
        if (!string.IsNullOrWhiteSpace(message)) return message;
        message = BuildKitClashMessage(homeGk, home, "Home GK", "Home outfield");
        if (!string.IsNullOrWhiteSpace(message)) return message;
        message = BuildKitClashMessage(homeGk, away, "Home GK", "Away outfield");
        if (!string.IsNullOrWhiteSpace(message)) return message;
        message = BuildKitClashMessage(awayGk, home, "Away GK", "Home outfield");
        if (!string.IsNullOrWhiteSpace(message)) return message;
        return BuildKitClashMessage(awayGk, away, "Away GK", "Away outfield");
    }

    private static string BuildKitClashMessage(TokenKitPreset first, TokenKitPreset second, string firstLabel, string secondLabel)
    {
        if (first == null || second == null)
        {
            return string.Empty;
        }

        TokenKitSimilarityBreakdown similarity = TokenKitCatalog.GetSimilarityBreakdown(first.Id, second.Id);
        if (!similarity.IsClash)
        {
            return string.Empty;
        }

        return $"{firstLabel} ({first.DisplayName}) is too similar to {secondLabel} ({second.DisplayName}).";
    }

    private static string GetDropdownText(TMP_Dropdown dropdown)
    {
        if (dropdown == null || dropdown.options.Count == 0)
        {
            return string.Empty;
        }

        int index = Mathf.Clamp(dropdown.value, 0, dropdown.options.Count - 1);
        return dropdown.options[index].text;
    }

    private static TokenKitPreset GetSelectedEditKitPreset(TMP_Dropdown dropdown, IReadOnlyList<TokenKitPreset> presets)
    {
        if (dropdown == null || presets == null || presets.Count == 0)
        {
            return null;
        }

        int index = Mathf.Clamp(dropdown.value, 0, presets.Count - 1);
        return presets[index];
    }

    private static string GetSelectedEditKitDisplayName(TMP_Dropdown dropdown, IReadOnlyList<TokenKitPreset> presets)
    {
        return GetSelectedEditKitPreset(dropdown, presets)?.DisplayName ?? GetDropdownText(dropdown);
    }

    private static T FindChildComponent<T>(Transform root, string childName) where T : Component
    {
        if (root == null)
        {
            return null;
        }

        foreach (T component in root.GetComponentsInChildren<T>(includeInactive: true))
        {
            if (component.name == childName)
            {
                return component;
            }
        }

        return null;
    }

    private static GameObject FindChildObject(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        foreach (Transform child in root.GetComponentsInChildren<Transform>(includeInactive: true))
        {
            if (child.name == childName)
            {
                return child.gameObject;
            }
        }

        return null;
    }

    public void EditSettings()
    {
        BindEditSettingsUiReferences();
        EnsureEditSettingsUi();
        if (!HasEditSettingsUiReferences(out string missingReferences))
        {
            ShowPauseSaveStatus($"Edit Settings UI is not configured in the scene: {missingReferences}.", isError: true);
            return;
        }

        ClearPauseSaveStatus();
        ClearLogFileStatus();

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        if (editSettingsPanel != null)
        {
            editSettingsPanel.SetActive(true);
        }

        RefreshEditSettingsControls();

        MatchManager.Instance?.SetPauseMenuOpen(true);
        isPaused = true;
        Debug.Log("Entering Edit Settings Mode.");
    }

    public void CloseEditSettingsPanel()
    {
        if (editSettingsPanel != null)
        {
            editSettingsPanel.SetActive(false);
        }

        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }

        RefreshSaveButtonAvailability();
        substitutionMenuManager?.RefreshOpenButtonState();
    }

    public void CaptureLogFile()
    {
        MatchManager matchManager = MatchManager.Instance;
        if (matchManager == null)
        {
            ShowLogFileStatus("MatchManager is not available.", isError: true);
            return;
        }

        if (matchManager.CaptureLiveLogFile(
            openFolder: true,
            out string captureFolderPath,
            out string capturedFilePath,
            out string message))
        {
            string capturedText = string.IsNullOrWhiteSpace(capturedFilePath)
                ? captureFolderPath
                : capturedFilePath;
            ShowLogFileStatus($"{message}\n{capturedText}", isError: false);
            Debug.Log(message);
            return;
        }

        Debug.LogWarning(message);
        ShowLogFileStatus(message, isError: true);
    }

    public void BackToMainMenuInGame()
    {
        // Implement your logic for "Save Match As" functionality
        Debug.Log("Back To Main Menu Scene!");
        MatchManager.Instance?.SetPauseMenuOpen(false);
        SceneManager.LoadScene("MainMenu");  // Adjust to your actual Main Menu scene name
    }

    public void QuitMatch()
    {
        // Implement your logic for quitting the match (return to main menu)
        Debug.Log("Game Quit!");
        Application.Quit(); // This will close the application
    }
}
