using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System;
using System.IO;

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

    private bool isPaused = false;
    private bool isSaveAvailable = false;
    private string saveUnavailableReason = string.Empty;
    private SubstitutionMenuManager substitutionMenuManager;

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
        Transform panelParent = pausePanel != null && pausePanel.transform.parent != null
            ? pausePanel.transform.parent
            : transform;

        if (editSettingsPanel == null)
        {
            editSettingsPanel = CreateRuntimeEditSettingsPanel(panelParent);
        }

        Transform panelTransform = editSettingsPanel.transform;
        captureLogFileButton ??= FindChildComponent<Button>(panelTransform, "CaptureLogFileButton")
            ?? CreateRuntimeButton(panelTransform, "CaptureLogFileButton", "Capture Logfile");
        editSettingsBackButton ??= FindChildComponent<Button>(panelTransform, "EditSettingsBackButton")
            ?? CreateRuntimeButton(panelTransform, "EditSettingsBackButton", "Back");
        logFileStatusText ??= FindChildComponent<TextMeshProUGUI>(panelTransform, "LogFileStatusText")
            ?? CreateRuntimeStatusText(panelTransform);

        editSettingsPanel.SetActive(false);
    }

    private void ConfigureEditSettingsUi()
    {
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

    private GameObject CreateRuntimeEditSettingsPanel(Transform parent)
    {
        GameObject panel = new("EditSettingsPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(VerticalLayoutGroup));
        panel.transform.SetParent(parent, worldPositionStays: false);

        RectTransform rect = panel.transform as RectTransform;
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }

        Image image = panel.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.55f);
        image.raycastTarget = true;

        VerticalLayoutGroup layout = panel.GetComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 30f;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        return panel;
    }

    private Button CreateRuntimeButton(Transform parent, string buttonName, string label)
    {
        Button button;
        if (resumeButton != null)
        {
            button = Instantiate(resumeButton, parent);
            button.onClick.RemoveAllListeners();
        }
        else
        {
            GameObject buttonObject = new(buttonName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, worldPositionStays: false);
            Image image = buttonObject.GetComponent<Image>();
            image.color = Color.white;
            button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;

            TextMeshProUGUI labelText = CreateRuntimeButtonLabel(buttonObject.transform);
            labelText.text = label;
        }

        button.name = buttonName;
        RectTransform rect = button.transform as RectTransform;
        if (rect != null)
        {
            rect.sizeDelta = new Vector2(300f, 56f);
        }

        SetButtonText(button, label);
        return button;
    }

    private TextMeshProUGUI CreateRuntimeButtonLabel(Transform parent)
    {
        GameObject textObject = new("Text (TMP)", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, worldPositionStays: false);
        RectTransform rect = textObject.transform as RectTransform;
        if (rect != null)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 24f;
        text.color = new Color(0.196f, 0.196f, 0.196f, 1f);
        if (TMP_Settings.defaultFontAsset != null)
        {
            text.font = TMP_Settings.defaultFontAsset;
        }

        return text;
    }

    private TextMeshProUGUI CreateRuntimeStatusText(Transform parent)
    {
        GameObject textObject = new("LogFileStatusText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, worldPositionStays: false);
        RectTransform rect = textObject.transform as RectTransform;
        if (rect != null)
        {
            rect.sizeDelta = new Vector2(1000f, 120f);
        }

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 20f;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
        text.color = MutedTextColor;
        if (TMP_Settings.defaultFontAsset != null)
        {
            text.font = TMP_Settings.defaultFontAsset;
        }

        return text;
    }

    private static void SetButtonText(Button button, string text)
    {
        TMP_Text label = button != null ? button.GetComponentInChildren<TMP_Text>(includeInactive: true) : null;
        if (label != null)
        {
            label.text = text;
        }
    }
}
