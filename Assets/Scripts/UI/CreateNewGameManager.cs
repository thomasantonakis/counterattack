using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System;
using TMPro;  // Import TextMeshPro namespace
using UnityEngine.SceneManagement;
using Newtonsoft.Json; // Now it will recognize JsonConvert
using Newtonsoft.Json.Linq;
using UnityEngine.EventSystems;
using System.Linq;

public class CreateNewGameManager : MonoBehaviour
{
    private const string TieBreakerExtraTimeAndPenalties = "Extra Time & Penalties";
    private const string TieBreakerPenalties = "Penalties";
    private const string TieBreakerExtraTime = "Extra Time";
    private const string TieBreakerNone = "None";
    private const string MatchTypeInternational = "International";
    private const string DraftInternational = "International";
    private const string DraftArcade = "Arcade";
    private const string DraftSceneName = "Draft";
    private const string FreeDraftSceneName = "FreeDraft";
    private const string CreateLoadRoomSceneName = "CreateLoadRoom";
    private const string DraftProfileGreedy = "Greedy";
    private const string DraftProfileSophisticated = "Sophisticated";
    private const string RoomPersonaAskHuman = "AskHuman";
    private const string RoomPersonaRandom = "Random";
    private const string TeamControlHumanLabel = "P1 (Human)";
    private const string TeamControlCpuRandomLabel = "CPU-Random";
    private const string CurrentGameSettingsPlayerPrefsKey = "currentGameSettings";
    private const string CreateNewGameReturnSourcePlayerPrefsKey = "CreateNewGameReturnSource";
    private const float KitDropdownScrollSensitivity = 55f;
    private const int TeamNameCharacterLimit = 20;
    private static readonly string[] PlayerAssistanceLabels = { "Novice", "Intermediate", "Experienced" };

    public TMP_Dropdown gameModeDropdown;
    public Slider halfDurationSlider;
    public TMP_Text halfDurationText;  // Text to show slider value
    public TMP_Dropdown numberOfHalvesDropdown;
    public TMP_Dropdown tiebreakerDropdown;
    public Slider playerAssistanceSlider;
    public TMP_Dropdown playerAssistanceDropdown;
    public TMP_Text playerAssistanceText;  // Text to show slider value
    public TMP_Dropdown matchTypeDropdown;
    public TMP_Dropdown squadSizeDropdown;  // The dropdown for squad size
    public TMP_Dropdown draftDropdown;
    public TMP_Dropdown gkDraftDropdown;
    public TMP_Dropdown refereeDropdown;
    public TMP_Dropdown weatherDropdown;
    public TMP_Dropdown ballColorDropdown;
    // public TMP_Dropdown playerDeckDropdown;
    public TMP_Dropdown homeKitDropdown;
    public TMP_Dropdown awayKitDropdown;
    public TMP_Dropdown homeGKKitDropdown;
    public TMP_Dropdown awayGKKitDropdown;
    public TMP_Dropdown homeTeamControlDropdown;
    public TMP_Dropdown awayTeamControlDropdown;
    public RawImage homeKitPreviewImage;
    public TMP_Text homeKitPreviewNumberText;
    public RawImage awayKitPreviewImage;
    public TMP_Text awayKitPreviewNumberText;
    public RawImage homeGKKitPreviewImage;
    public TMP_Text homeGKKitPreviewNumberText;
    public RawImage awayGKKitPreviewImage;
    public TMP_Text awayGKKitPreviewNumberText;
    public TMP_Text kitValidationText;
    public TMP_Text kitSimilarityText;
    public TMP_InputField homeTeamInputField;
    public TMP_InputField awayTeamInputField;
    public TMP_Dropdown homeInternationalTeamDropdown;
    public TMP_Dropdown awayInternationalTeamDropdown;
    public Toggle includeTabletopiaToggle;
    public Toggle includeNonTabletopiaToggle;
    public Toggle includeInternationalsToggle;
    public Toggle includeTabletopiaGKToggle;
    public Toggle includeNonTabletopiaGKToggle;
    public Toggle includeInternationalsGKToggle;
    public Button createGameButton;
    public Button backToGameModeMenuButton;

    private const string DefaultHomeKitId = "088";
    private const string DefaultAwayKitId = "021";
    private const string DefaultHomeGKKitId = "gk2";
    private const string DefaultAwayGKKitId = "gk3";
    private const string PreviewSampleNumber = "10";
    private const string GKPreviewSampleNumber = "1";
    private const float PreviewPlainNumberFontSize = 34f;
    private const float PreviewVerticalNumberFontSize = 30f;

    private IReadOnlyList<TokenKitPreset> availableKitPresets;
    private IReadOnlyList<TokenKitPreset> availableGKKitPresets;
    private IReadOnlyList<TokenKitPreset> homeAvailableKitPresets;
    private IReadOnlyList<TokenKitPreset> awayAvailableKitPresets;
    private readonly List<string> internationalTeamNames = new List<string>();
    private readonly Dictionary<string, InternationalTeamKitOptions> internationalTeamKits = new Dictionary<string, InternationalTeamKitOptions>(StringComparer.OrdinalIgnoreCase);
    private TMP_Dropdown activeClosedKitDropdown;
    private bool isRefreshingInternationalTeamUi;
    private bool suppressKitSelectionChanged;
    private string lastValidHomeKitId = DefaultHomeKitId;
    private string lastValidAwayKitId = DefaultAwayKitId;
    private string lastValidHomeGKKitId = DefaultHomeGKKitId;
    private string lastValidAwayGKKitId = DefaultAwayGKKitId;
    private bool suppressRegularMatchSelectionSnapshot;
    private RegularMatchSelectionSnapshot regularMatchSelectionSnapshot;
    private bool isEditingExistingDraftSettings;
    private string existingDraftSettingsFilePath = string.Empty;
    private bool requestedCreateGameButtonEnabled = true;

    private void Update()
    {
        HandleCreateGameKeyboardNavigation();
        HandleClosedKitDropdownArrowKeys();
    }

    void Start()
    {
        createGameButton.onClick.AddListener(SaveGameSettingsToJson);
        // Half Duration Set minimum and maximum values of the slider
        halfDurationSlider.minValue = 15;  // Minimum half duration
        halfDurationSlider.maxValue = 60;  // Maximum half duration
        halfDurationSlider.wholeNumbers = true;  // Restrict the slider to integer values
        halfDurationSlider.value = 45;  // Default to 45 minutes
        ConfigurePlayerAssistanceDropdown();
        includeTabletopiaToggle.isOn = true;
        includeNonTabletopiaToggle.isOn = false;
        includeInternationalsToggle.isOn = false;
        includeTabletopiaGKToggle.isOn = true;
        includeNonTabletopiaGKToggle.isOn = false;
        includeInternationalsGKToggle.isOn = false;

        // Update the text when the slider is moved
        halfDurationSlider.onValueChanged.AddListener(UpdateHalfDurationSliderText);
        halfDurationSlider.onValueChanged.AddListener(delegate { RefreshTiebreakerOptions(); });
        numberOfHalvesDropdown.onValueChanged.AddListener(delegate { RefreshTiebreakerOptions(); });
        // Set the initial text based on the current slider value
        UpdateHalfDurationSliderText(halfDurationSlider.value);
        SetPlayerAssistanceDropdownValue(2);
        
        // Example: Set default options for squad size at the start of the game
        SetDropDownOptions();
        ConfigureTeamControlDropdowns();
        ConfigureBackToGameModeMenuButton();
        ConfigureTeamNameInputs();
        SetCreateGameButtonEnabled(true);
        // Subscribe to field changes, which dynamically adjusts other fields' options
        matchTypeDropdown.onValueChanged.AddListener(delegate { OnMatchTypeChanged(); });
        weatherDropdown.onValueChanged.AddListener(delegate { AdjustBallColorBasedOnWeather(); });
        homeTeamInputField.onSelect.AddListener(delegate { ClearActiveClosedKitDropdown(); });
        awayTeamInputField.onSelect.AddListener(delegate { ClearActiveClosedKitDropdown(); });
        // Add listeners to each checkbox
        includeTabletopiaToggle.onValueChanged.AddListener(delegate { ValidateCheckboxes(includeTabletopiaToggle); });
        includeNonTabletopiaToggle.onValueChanged.AddListener(delegate { ValidateCheckboxes(includeNonTabletopiaToggle); });
        includeInternationalsToggle.onValueChanged.AddListener(delegate { ValidateCheckboxes(includeInternationalsToggle); });
        includeTabletopiaGKToggle.onValueChanged.AddListener(delegate { ValidateCheckboxesGK(includeTabletopiaGKToggle); });
        includeNonTabletopiaGKToggle.onValueChanged.AddListener(delegate { ValidateCheckboxesGK(includeNonTabletopiaGKToggle); });
        includeInternationalsGKToggle.onValueChanged.AddListener(delegate { ValidateCheckboxesGK(includeInternationalsGKToggle); });
        // Initial setup
        AdjustSquadSizeOptionsBasedOnMatchType();
        InitializeInternationalTeamSelectionUi();
        InitializeKitSelectionUi();
        OnMatchTypeChanged();
        TryRestoreExistingSettingsFromDraftReturn();
    }

    private void ConfigureTeamControlDropdowns()
    {
        bool isSinglePlayerCreateMode = IsSinglePlayerCreateMode();
        ConfigureTeamControlDropdown(homeTeamControlDropdown, defaultToHuman: true, isSinglePlayerCreateMode);
        ConfigureTeamControlDropdown(awayTeamControlDropdown, defaultToHuman: false, isSinglePlayerCreateMode);
        RefreshTeamControlValidity(homeTeamControlDropdown);
    }

    private void ConfigureTeamControlDropdown(TMP_Dropdown dropdown, bool defaultToHuman, bool isSinglePlayerCreateMode)
    {
        if (dropdown == null)
        {
            return;
        }

        dropdown.gameObject.SetActive(isSinglePlayerCreateMode);
        dropdown.ClearOptions();
        dropdown.AddOptions(new List<string> { TeamControlHumanLabel, TeamControlCpuRandomLabel });
        dropdown.SetValueWithoutNotify(defaultToHuman ? 0 : 1);
        dropdown.RefreshShownValue();
        dropdown.onValueChanged.RemoveListener(OnTeamControlDropdownChanged);
        dropdown.onValueChanged.AddListener(OnTeamControlDropdownChanged);
    }

    private void OnTeamControlDropdownChanged(int _)
    {
        TMP_Dropdown changedDropdown = null;
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
        {
            if (EventSystem.current.currentSelectedGameObject == homeTeamControlDropdown?.gameObject)
            {
                changedDropdown = homeTeamControlDropdown;
            }
            else if (EventSystem.current.currentSelectedGameObject == awayTeamControlDropdown?.gameObject)
            {
                changedDropdown = awayTeamControlDropdown;
            }
        }

        RefreshTeamControlValidity(changedDropdown);
    }

    private void RefreshTeamControlValidity(TMP_Dropdown changedDropdown)
    {
        if (!IsSinglePlayerCreateMode() || homeTeamControlDropdown == null || awayTeamControlDropdown == null)
        {
            return;
        }

        if (!IsHumanControlled(homeTeamControlDropdown) || !IsHumanControlled(awayTeamControlDropdown))
        {
            return;
        }

        TMP_Dropdown dropdownToSwitch = changedDropdown == homeTeamControlDropdown
            ? awayTeamControlDropdown
            : homeTeamControlDropdown;
        SetTeamControlWithoutNotify(dropdownToSwitch, TeamControlCpuRandomLabel);
    }

    private void ConfigureTeamNameInputs()
    {
        ConfigureTeamNameInput(homeTeamInputField);
        ConfigureTeamNameInput(awayTeamInputField);
        RefreshCreateGameButtonState();
    }

    private void ConfigureTeamNameInput(TMP_InputField inputField)
    {
        if (inputField == null)
        {
            return;
        }

        inputField.characterLimit = TeamNameCharacterLimit;
        inputField.SetTextWithoutNotify(LimitTeamNameLength(inputField.text));
        inputField.onValueChanged.RemoveListener(OnTeamNameInputChanged);
        inputField.onValueChanged.AddListener(OnTeamNameInputChanged);
    }

    private void OnTeamNameInputChanged(string _)
    {
        RefreshCreateGameButtonState();
    }

    private void HandleCreateGameKeyboardNavigation()
    {
        if (homeTeamInputField == null || awayTeamInputField == null || EventSystem.current == null)
        {
            return;
        }

        if (!Input.GetKeyDown(KeyCode.Tab))
        {
            return;
        }

        if (EventSystem.current.currentSelectedGameObject != homeTeamInputField.gameObject)
        {
            return;
        }

        awayTeamInputField.Select();
        awayTeamInputField.ActivateInputField();
        awayTeamInputField.caretPosition = awayTeamInputField.text?.Length ?? 0;
    }

    // Update the displayed text for the slider value
    public void UpdateHalfDurationSliderText(float value)
    {
        halfDurationText.text = value.ToString(); // Show the slider's current value
    }
    public void UpdatePlayerAssistanceSliderText(float value)
    {
        playerAssistanceText.text = value.ToString(); // Show the slider's current value
    }

    private void ConfigurePlayerAssistanceDropdown()
    {
        if (playerAssistanceDropdown == null)
        {
            if (playerAssistanceSlider != null)
            {
                playerAssistanceSlider.minValue = 1;
                playerAssistanceSlider.maxValue = 3;
                playerAssistanceSlider.wholeNumbers = true;
                playerAssistanceSlider.value = 2;
                playerAssistanceSlider.onValueChanged.AddListener(UpdatePlayerAssistanceSliderText);
                UpdatePlayerAssistanceSliderText(playerAssistanceSlider.value);
            }

            return;
        }

        playerAssistanceDropdown.ClearOptions();
        playerAssistanceDropdown.AddOptions(PlayerAssistanceLabels.ToList());
        playerAssistanceDropdown.SetValueWithoutNotify(1);
        playerAssistanceDropdown.RefreshShownValue();

        if (playerAssistanceSlider != null)
        {
            playerAssistanceSlider.gameObject.SetActive(false);
        }

        if (playerAssistanceText != null)
        {
            playerAssistanceText.gameObject.SetActive(false);
        }
    }

    private int GetSelectedPlayerAssistanceValue()
    {
        if (playerAssistanceDropdown != null)
        {
            return Mathf.Clamp(playerAssistanceDropdown.value + 1, 1, 3);
        }

        return playerAssistanceSlider != null
            ? Mathf.Clamp(Mathf.RoundToInt(playerAssistanceSlider.value), 1, 3)
            : 2;
    }

    private void SetPlayerAssistanceDropdownValue(int playerAssistance)
    {
        int clampedValue = Mathf.Clamp(playerAssistance, 1, 3);
        if (playerAssistanceDropdown != null)
        {
            playerAssistanceDropdown.SetValueWithoutNotify(clampedValue - 1);
            playerAssistanceDropdown.RefreshShownValue();
        }

        if (playerAssistanceSlider != null)
        {
            playerAssistanceSlider.SetValueWithoutNotify(clampedValue);
            UpdatePlayerAssistanceSliderText(clampedValue);
        }
    }

    private void ValidateCheckboxes(Toggle changedToggle)
    {
        // Ensure at least one checkbox remains checked
        if (!includeTabletopiaToggle.isOn && !includeNonTabletopiaToggle.isOn && !includeInternationalsToggle.isOn)
        {
            // Revert the last changed checkbox to ON
            changedToggle.isOn = true;

            Debug.Log("At least one checkbox must be selected.");
        }
    }
    private void ValidateCheckboxesGK(Toggle changedToggle)
    {
        // Ensure at least one checkbox remains checked
        if (!includeTabletopiaGKToggle.isOn && !includeNonTabletopiaGKToggle.isOn && !includeInternationalsGKToggle.isOn)
        {
            // Revert the last changed checkbox to ON
            changedToggle.isOn = true;

            Debug.Log("At least one checkbox must be selected.");
        }

        // Ensure that includeNonTabletopiaToggle is not the only one selected
        if (includeNonTabletopiaGKToggle.isOn && !includeTabletopiaGKToggle.isOn && !includeInternationalsGKToggle.isOn)
        {
            // Revert the last changed checkbox to OFF
            changedToggle.isOn = true;

            Debug.Log("includeNonTabletopiaToggle cannot be the only selected option.");
        }
    }

    // Set the default squad size options when the scene loads
    void SetDropDownOptions()
    {
        bool isSinglePlayerCreateMode = IsSinglePlayerCreateMode();
        List<string> gameModeOptions = isSinglePlayerCreateMode
            ? new List<string> { ApplicationManager.SinglePlayerGameMode }
            : new List<string> { "Single Player", "Hot Seat", "Multi Player" };  // Define options
        gameModeDropdown.ClearOptions();  // Clear any existing options
        gameModeDropdown.AddOptions(gameModeOptions);  // Add the default options
        string requestedGameMode = isSinglePlayerCreateMode ? ApplicationManager.SinglePlayerGameMode : GetRequestedCreateLoadGameMode();
        int gameModeIndex = gameModeOptions.FindIndex(option => string.Equals(option, requestedGameMode, StringComparison.OrdinalIgnoreCase));
        if (gameModeIndex < 0)
        {
            string fallback = isSinglePlayerCreateMode ? ApplicationManager.SinglePlayerGameMode : ApplicationManager.HotSeatGameMode;
            gameModeIndex = gameModeOptions.FindIndex(option => string.Equals(option, fallback, StringComparison.OrdinalIgnoreCase));
        }

        gameModeDropdown.SetValueWithoutNotify(Mathf.Max(0, gameModeIndex));
        gameModeDropdown.interactable = !isSinglePlayerCreateMode;
        gameModeDropdown.RefreshShownValue();
        RefreshGameModeLabelForCreateMode();
        List<string> numberOfHalvesOptions = new List<string> { "2", "1" };  // Define options
        numberOfHalvesDropdown.ClearOptions();  // Clear any existing options
        numberOfHalvesDropdown.AddOptions(numberOfHalvesOptions);  // Add the default options
        List<string> tiebreakerOptions = new List<string> { "Extra Time & Penalties", "Penalties", "Extra Time", "None"};  // Define options
        tiebreakerDropdown.ClearOptions();  // Clear any existing options
        tiebreakerDropdown.AddOptions(tiebreakerOptions);  // Add the default options
        List<string> matchTypeOptions = new List<string> { "Regular", "International"};  // Define options
        matchTypeDropdown.ClearOptions();  // Clear any existing options
        matchTypeDropdown.AddOptions(matchTypeOptions);  // Add the default options
        SetRegularDraftDropdownOptions();
        List<string> defaultSquadSizes = new List<string> { "16", "18" };  // Define options
        squadSizeDropdown.ClearOptions();  // Clear any existing options
        squadSizeDropdown.AddOptions(defaultSquadSizes);  // Add the default options
        List<string> weatherOptions = new List<string> { "Clear", "Rain", "Snow" };  // Define options
        weatherDropdown.ClearOptions();  // Clear any existing options
        weatherDropdown.AddOptions(weatherOptions);  // Add the default options
        List<string> ballColorOptions = new List<string> { "White", "Orange", "Yellow" };  // Define options
        ballColorDropdown.ClearOptions();  // Clear any existing options
        ballColorDropdown.AddOptions(ballColorOptions);  // Add the default options
        List<string> refereeOptions = new List<string> { "Random", "Webster - 2", "Castolo - 3", "Bakker - 4", "Read - 5" };  // Define options
        refereeDropdown.ClearOptions();  // Clear any existing options
        refereeDropdown.AddOptions(refereeOptions);  // Add the default options
        RefreshTiebreakerOptions();
    }

    private void SetRegularDraftDropdownOptions()
    {
        SetDropdownOptionsPreservingSelection(draftDropdown, new List<string> { "Regular", "Free Regular", DraftArcade }, "Regular");
        SetDropdownOptionsPreservingSelection(gkDraftDropdown, new List<string> { "Deal", "Free", DraftArcade }, "Deal");
        if (draftDropdown != null)
        {
            draftDropdown.interactable = true;
        }

        if (gkDraftDropdown != null)
        {
            gkDraftDropdown.interactable = true;
        }
    }

    private void SetInternationalDraftDropdownOptions()
    {
        SetDropdownOptionsPreservingSelection(draftDropdown, new List<string> { DraftInternational }, DraftInternational);
        SetDropdownOptionsPreservingSelection(gkDraftDropdown, new List<string> { DraftInternational }, DraftInternational);
        if (draftDropdown != null)
        {
            draftDropdown.interactable = false;
        }

        if (gkDraftDropdown != null)
        {
            gkDraftDropdown.interactable = false;
        }
    }

    private void SetDropdownOptionsPreservingSelection(TMP_Dropdown dropdown, List<string> options, string fallback)
    {
        if (dropdown == null)
        {
            return;
        }

        string currentSelection = dropdown.options.Count > 0 && dropdown.value >= 0 && dropdown.value < dropdown.options.Count
            ? dropdown.options[dropdown.value].text
            : fallback;
        int selectedIndex = options.FindIndex(option => string.Equals(option, currentSelection, StringComparison.OrdinalIgnoreCase));
        if (selectedIndex < 0)
        {
            selectedIndex = options.FindIndex(option => string.Equals(option, fallback, StringComparison.OrdinalIgnoreCase));
        }

        dropdown.ClearOptions();
        dropdown.AddOptions(options);
        dropdown.SetValueWithoutNotify(Mathf.Max(0, selectedIndex));
        dropdown.RefreshShownValue();
    }

    private void RefreshTiebreakerOptions()
    {
        if (tiebreakerDropdown == null || halfDurationSlider == null || numberOfHalvesDropdown == null || numberOfHalvesDropdown.options.Count == 0)
        {
            return;
        }

        string selectedTiebreaker = tiebreakerDropdown.options.Count > 0
            ? tiebreakerDropdown.options[Mathf.Clamp(tiebreakerDropdown.value, 0, tiebreakerDropdown.options.Count - 1)].text
            : TieBreakerNone;

        int numberOfHalves = int.Parse(numberOfHalvesDropdown.options[numberOfHalvesDropdown.value].text);
        bool fullLengthTwoHalfMatch = Mathf.RoundToInt(halfDurationSlider.value) == 45 && numberOfHalves == 2;
        List<string> tiebreakerOptions = fullLengthTwoHalfMatch
            ? new List<string> { TieBreakerExtraTimeAndPenalties, TieBreakerPenalties, TieBreakerExtraTime, TieBreakerNone }
            : new List<string> { TieBreakerPenalties, TieBreakerNone };

        int selectedIndex = tiebreakerOptions.IndexOf(selectedTiebreaker);
        if (selectedIndex < 0)
        {
            selectedIndex = tiebreakerOptions.IndexOf(TieBreakerNone);
        }

        tiebreakerDropdown.ClearOptions();
        tiebreakerDropdown.AddOptions(tiebreakerOptions);
        tiebreakerDropdown.value = Mathf.Max(0, selectedIndex);
        tiebreakerDropdown.RefreshShownValue();
    }

    private void InitializeKitSelectionUi()
    {
        ReloadKitSelectionUi();
    }

    private void InitializeInternationalTeamSelectionUi()
    {
        EnsureInternationalTeamDropdowns();
        LoadInternationalTeamOptions();
        PopulateInternationalTeamDropdown(homeInternationalTeamDropdown, "France");
        PopulateInternationalTeamDropdown(awayInternationalTeamDropdown, "Argentina");

        if (homeInternationalTeamDropdown != null)
        {
            homeInternationalTeamDropdown.onValueChanged.RemoveListener(OnHomeInternationalTeamChanged);
            homeInternationalTeamDropdown.onValueChanged.AddListener(OnHomeInternationalTeamChanged);
        }

        if (awayInternationalTeamDropdown != null)
        {
            awayInternationalTeamDropdown.onValueChanged.RemoveListener(OnAwayInternationalTeamChanged);
            awayInternationalTeamDropdown.onValueChanged.AddListener(OnAwayInternationalTeamChanged);
        }
    }

    private void EnsureInternationalTeamDropdowns()
    {
        if (homeInternationalTeamDropdown == null)
        {
            homeInternationalTeamDropdown = CreateInternationalTeamDropdown("Home International Team Dropdown", homeTeamInputField);
        }

        if (awayInternationalTeamDropdown == null)
        {
            awayInternationalTeamDropdown = CreateInternationalTeamDropdown("Away International Team Dropdown", awayTeamInputField);
        }
    }

    private TMP_Dropdown CreateInternationalTeamDropdown(string objectName, TMP_InputField sourceInput)
    {
        TMP_Dropdown sourceDropdown = matchTypeDropdown != null ? matchTypeDropdown : homeKitDropdown;
        if (sourceDropdown == null || sourceInput == null)
        {
            return null;
        }

        TMP_Dropdown dropdown = Instantiate(sourceDropdown, sourceInput.transform.parent);
        dropdown.name = objectName;
        RectTransform dropdownTransform = dropdown.transform as RectTransform;
        RectTransform sourceTransform = sourceInput.transform as RectTransform;
        if (dropdownTransform != null && sourceTransform != null)
        {
            dropdownTransform.anchorMin = sourceTransform.anchorMin;
            dropdownTransform.anchorMax = sourceTransform.anchorMax;
            dropdownTransform.pivot = sourceTransform.pivot;
            dropdownTransform.anchoredPosition = sourceTransform.anchoredPosition;
            dropdownTransform.sizeDelta = sourceTransform.sizeDelta;
            dropdownTransform.localScale = sourceTransform.localScale;
        }

        dropdown.onValueChanged.RemoveAllListeners();
        dropdown.gameObject.SetActive(false);
        return dropdown;
    }

    private void LoadInternationalTeamOptions()
    {
        internationalTeamNames.Clear();
        internationalTeamKits.Clear();

        foreach (string nationality in LoadWorldCupNationalities())
        {
            if (!string.IsNullOrWhiteSpace(nationality) && !internationalTeamNames.Contains(nationality))
            {
                internationalTeamNames.Add(nationality);
            }
        }

        internationalTeamNames.Sort(StringComparer.OrdinalIgnoreCase);

        if (internationalTeamNames.Count == 0)
        {
            internationalTeamNames.AddRange(new[]
            {
                "Argentina", "Belgium", "Brazil", "England", "France", "Germany",
                "Greece", "Ireland", "Italy", "Ivory Coast", "Japan", "Mexico",
                "Netherlands", "Nigeria", "Portugal", "Scotland", "Spain", "USA"
            });
        }
    }

    private IEnumerable<string> LoadWorldCupNationalities()
    {
        TextAsset playersCsv = Resources.Load<TextAsset>("outfield_players");
        if (playersCsv == null)
        {
            yield break;
        }

        string[] lines = playersCsv.text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length <= 1)
        {
            yield break;
        }

        string[] headers = lines[0].Split(',');
        int nationalityIndex = Array.FindIndex(headers, header => string.Equals(header.Trim(), "Nationality", StringComparison.OrdinalIgnoreCase));
        int typeIndex = Array.FindIndex(headers, header => string.Equals(header.Trim(), "Type", StringComparison.OrdinalIgnoreCase));
        if (nationalityIndex < 0 || typeIndex < 0)
        {
            yield break;
        }

        for (int i = 1; i < lines.Length; i++)
        {
            string[] columns = lines[i].Split(',');
            if (columns.Length <= Mathf.Max(nationalityIndex, typeIndex))
            {
                continue;
            }

            if (string.Equals(columns[typeIndex].Trim(), "World Cup", StringComparison.OrdinalIgnoreCase))
            {
                yield return columns[nationalityIndex].Trim();
            }
        }
    }

    private void PopulateInternationalTeamDropdown(TMP_Dropdown dropdown, string defaultTeam)
    {
        if (dropdown == null)
        {
            return;
        }

        dropdown.ClearOptions();
        dropdown.AddOptions(internationalTeamNames);
        int selectedIndex = internationalTeamNames.FindIndex(team => string.Equals(team, defaultTeam, StringComparison.OrdinalIgnoreCase));
        dropdown.SetValueWithoutNotify(selectedIndex >= 0 ? selectedIndex : 0);
        dropdown.RefreshShownValue();
    }

    public void ReloadKitSelectionUi()
    {
        string currentHomeKitId = GetSelectedKitPresetId(homeKitDropdown);
        string currentAwayKitId = GetSelectedKitPresetId(awayKitDropdown);
        string currentHomeGKKitId = GetSelectedKitPresetId(homeGKKitDropdown);
        string currentAwayGKKitId = GetSelectedKitPresetId(awayGKKitDropdown);

        homeKitDropdown.onValueChanged.RemoveListener(OnHomeKitChanged);
        awayKitDropdown.onValueChanged.RemoveListener(OnAwayKitChanged);
        if (homeGKKitDropdown != null)
        {
            homeGKKitDropdown.onValueChanged.RemoveListener(OnHomeGKKitChanged);
        }
        if (awayGKKitDropdown != null)
        {
            awayGKKitDropdown.onValueChanged.RemoveListener(OnAwayGKKitChanged);
        }

        List<TokenKitPreset> allPresets = TokenKitCatalog.GetAllPresets().ToList();
        availableKitPresets = allPresets
            .Where(preset => preset != null && !IsGoalkeeperKitPreset(preset))
            .ToList();
        availableGKKitPresets = allPresets
            .Where(IsGoalkeeperKitPreset)
            .ToList();
        RefreshInternationalTeamKitOptions();
        PopulateKitDropdown(homeKitDropdown, string.IsNullOrWhiteSpace(currentHomeKitId) ? DefaultHomeKitId : currentHomeKitId);
        PopulateKitDropdown(awayKitDropdown, string.IsNullOrWhiteSpace(currentAwayKitId) ? DefaultAwayKitId : currentAwayKitId);
        PopulateKitDropdown(homeGKKitDropdown, string.IsNullOrWhiteSpace(currentHomeGKKitId) ? DefaultHomeGKKitId : currentHomeGKKitId);
        PopulateKitDropdown(awayGKKitDropdown, string.IsNullOrWhiteSpace(currentAwayGKKitId) ? DefaultAwayGKKitId : currentAwayGKKitId);

        homeKitDropdown.onValueChanged.AddListener(OnHomeKitChanged);
        awayKitDropdown.onValueChanged.AddListener(OnAwayKitChanged);
        if (homeGKKitDropdown != null)
        {
            homeGKKitDropdown.onValueChanged.AddListener(OnHomeGKKitChanged);
        }
        if (awayGKKitDropdown != null)
        {
            awayGKKitDropdown.onValueChanged.AddListener(OnAwayGKKitChanged);
        }

        ConfigureKitDropdownNavigation(homeKitDropdown);
        ConfigureKitDropdownNavigation(awayKitDropdown);
        ConfigureKitDropdownNavigation(homeGKKitDropdown);
        ConfigureKitDropdownNavigation(awayGKKitDropdown);
        RepairGoalkeeperKitSelections(null);
        RememberCurrentValidKitSelections();
        UpdateKitPreviews();
        UpdateKitValidationForCurrentSelection();
    }

    private void PopulateKitDropdown(TMP_Dropdown dropdown, string defaultPresetId)
    {
        if (dropdown == null)
        {
            return;
        }

        IReadOnlyList<TokenKitPreset> presets = GetPresetListForDropdown(dropdown);
        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        foreach (TokenKitPreset preset in presets)
        {
            options.Add(new TMP_Dropdown.OptionData(preset.DisplayName));
        }

        dropdown.ClearOptions();
        dropdown.AddOptions(options);

        int defaultIndex = FindPresetIndex(defaultPresetId, presets);
        dropdown.SetValueWithoutNotify(defaultIndex >= 0 ? defaultIndex : 0);
        dropdown.RefreshShownValue();
    }

    private void RefreshInternationalTeamKitOptions()
    {
        if (!IsInternationalMatchSelected())
        {
            homeAvailableKitPresets = availableKitPresets;
            awayAvailableKitPresets = availableKitPresets;
            return;
        }

        homeAvailableKitPresets = GetInternationalTeamKitPresets(GetSelectedInternationalTeamName(homeInternationalTeamDropdown));
        awayAvailableKitPresets = GetInternationalTeamKitPresets(GetSelectedInternationalTeamName(awayInternationalTeamDropdown));
    }

    private IReadOnlyList<TokenKitPreset> GetInternationalTeamKitPresets(string teamName)
    {
        InternationalTeamKitOptions options = GetInternationalTeamKitOptions(teamName);
        if (options.AllKits.Count > 0)
        {
            return options.AllKits;
        }

        return availableKitPresets ?? Array.Empty<TokenKitPreset>();
    }

    private InternationalTeamKitOptions GetInternationalTeamKitOptions(string teamName)
    {
        if (string.IsNullOrWhiteSpace(teamName))
        {
            return InternationalTeamKitOptions.Empty;
        }

        if (internationalTeamKits.TryGetValue(teamName, out InternationalTeamKitOptions cachedOptions))
        {
            return cachedOptions;
        }

        TokenKitPreset homeKit = FindInternationalTeamKit(teamName, false);
        TokenKitPreset awayKit = FindInternationalTeamKit(teamName, true);
        List<TokenKitPreset> allKits = new List<TokenKitPreset>();
        if (homeKit != null)
        {
            allKits.Add(homeKit);
        }

        if (awayKit != null && allKits.All(kit => kit.Id != awayKit.Id))
        {
            allKits.Add(awayKit);
        }

        InternationalTeamKitOptions options = new InternationalTeamKitOptions(homeKit, awayKit, allKits);
        internationalTeamKits[teamName] = options;
        return options;
    }

    private TokenKitPreset FindInternationalTeamKit(string teamName, bool away)
    {
        string expectedName = away ? $"{teamName} Away" : teamName;
        foreach (TokenKitPreset preset in availableKitPresets ?? Array.Empty<TokenKitPreset>())
        {
            if (preset != null && string.Equals(preset.DisplayName, expectedName, StringComparison.OrdinalIgnoreCase))
            {
                return preset;
            }
        }

        return null;
    }

    private int FindPresetIndex(string presetIdOrAlias, IReadOnlyList<TokenKitPreset> presets)
    {
        TokenKitPreset preset = TokenKitCatalog.GetPresetByIdOrAlias(presetIdOrAlias);
        if (preset == null || presets == null)
        {
            return -1;
        }

        for (int i = 0; i < presets.Count; i++)
        {
            if (presets[i].Id == preset.Id)
            {
                return i;
            }
        }

        return -1;
    }

    private TokenKitPreset GetSelectedKitPreset(TMP_Dropdown dropdown)
    {
        IReadOnlyList<TokenKitPreset> presets = GetPresetListForDropdown(dropdown);
        if (dropdown == null || presets == null || presets.Count == 0)
        {
            return null;
        }

        int clampedIndex = Mathf.Clamp(dropdown.value, 0, presets.Count - 1);
        return presets[clampedIndex];
    }

    private string GetSelectedKitPresetId(TMP_Dropdown dropdown)
    {
        return GetSelectedKitPreset(dropdown)?.Id ?? string.Empty;
    }

    private IReadOnlyList<TokenKitPreset> GetPresetListForDropdown(TMP_Dropdown dropdown)
    {
        if (dropdown == homeGKKitDropdown || dropdown == awayGKKitDropdown)
        {
            return availableGKKitPresets ?? Array.Empty<TokenKitPreset>();
        }

        if (dropdown == homeKitDropdown)
        {
            return homeAvailableKitPresets ?? availableKitPresets ?? Array.Empty<TokenKitPreset>();
        }

        if (dropdown == awayKitDropdown)
        {
            return awayAvailableKitPresets ?? availableKitPresets ?? Array.Empty<TokenKitPreset>();
        }

        return availableKitPresets ?? Array.Empty<TokenKitPreset>();
    }

    private static bool IsGoalkeeperKitPreset(TokenKitPreset preset)
    {
        return preset != null && preset.Id.StartsWith("gk", StringComparison.OrdinalIgnoreCase);
    }

    private void OnHomeKitChanged(int _)
    {
        activeClosedKitDropdown = homeKitDropdown;
        HandleKitSelectionChanged(homeKitDropdown);
    }

    private void OnAwayKitChanged(int _)
    {
        activeClosedKitDropdown = awayKitDropdown;
        HandleKitSelectionChanged(awayKitDropdown);
    }

    private void OnHomeGKKitChanged(int _)
    {
        activeClosedKitDropdown = homeGKKitDropdown;
        HandleKitSelectionChanged(homeGKKitDropdown);
    }

    private void OnAwayGKKitChanged(int _)
    {
        activeClosedKitDropdown = awayGKKitDropdown;
        HandleKitSelectionChanged(awayGKKitDropdown);
    }

    private void HandleKitSelectionChanged(TMP_Dropdown changedDropdown)
    {
        if (suppressKitSelectionChanged)
        {
            return;
        }

        if (!TryAcceptKitSelectionChange(changedDropdown))
        {
            return;
        }

        UpdateKitPreviews();
        UpdateKitValidationForCurrentSelection();
    }

    private bool TryAcceptKitSelectionChange(TMP_Dropdown changedDropdown)
    {
        TokenKitPreset homePreset = GetSelectedKitPreset(homeKitDropdown);
        TokenKitPreset awayPreset = GetSelectedKitPreset(awayKitDropdown);
        TokenKitPreset homeGkPreset = GetSelectedKitPreset(homeGKKitDropdown);
        TokenKitPreset awayGkPreset = GetSelectedKitPreset(awayGKKitDropdown);

        if (changedDropdown == homeKitDropdown || changedDropdown == awayKitDropdown)
        {
            if (IsInternationalMatchSelected())
            {
                ApplyInternationalOutfieldKitPairForManualChange(changedDropdown);
                return FinishAcceptedKitSelection();
            }

            TokenKitSimilarityBreakdown outfieldSimilarity = TokenKitCatalog.GetSimilarityBreakdown(homePreset?.Id, awayPreset?.Id);
            if (outfieldSimilarity.IsClash)
            {
                RejectKitSelectionChange(
                    changedDropdown,
                    changedDropdown == homeKitDropdown ? lastValidHomeKitId : lastValidAwayKitId,
                    BuildKitClashMessage(homePreset?.DisplayName ?? "Home kit", awayPreset?.DisplayName ?? "Away kit", outfieldSimilarity));
                return false;
            }

            RepairGoalkeeperKitSelections(changedDropdown);
            return FinishAcceptedKitSelection();
        }

        if (changedDropdown == homeGKKitDropdown)
        {
            string directClashMessage = BuildFirstGkKitClashMessage(homeGkPreset, null, homePreset, awayPreset);
            if (!string.IsNullOrWhiteSpace(directClashMessage))
            {
                RejectKitSelectionChange(changedDropdown, lastValidHomeGKKitId, directClashMessage);
                return false;
            }

            RepairGoalkeeperKitSelections(changedDropdown);
            return FinishAcceptedKitSelection();
        }

        if (changedDropdown == awayGKKitDropdown)
        {
            string directClashMessage = BuildFirstGkKitClashMessage(null, awayGkPreset, homePreset, awayPreset);
            if (!string.IsNullOrWhiteSpace(directClashMessage))
            {
                RejectKitSelectionChange(changedDropdown, lastValidAwayGKKitId, directClashMessage);
                return false;
            }

            RepairGoalkeeperKitSelections(changedDropdown);
            return FinishAcceptedKitSelection();
        }

        RepairGoalkeeperKitSelections(null);
        return FinishAcceptedKitSelection();
    }

    private bool FinishAcceptedKitSelection()
    {
        TokenKitSimilarityBreakdown outfieldSimilarity = TokenKitCatalog.GetSimilarityBreakdown(GetSelectedKitPresetId(homeKitDropdown), GetSelectedKitPresetId(awayKitDropdown));
        string validationMessage = GetKitValidationMessage(outfieldSimilarity);
        if (!string.IsNullOrWhiteSpace(validationMessage))
        {
            RejectKitSelectionChangeAndRestoreLastValidSet(validationMessage);
            return false;
        }

        RememberCurrentValidKitSelections();
        return true;
    }

    private void RejectKitSelectionChange(TMP_Dropdown dropdown, string fallbackPresetId, string message)
    {
        suppressKitSelectionChanged = true;
        try
        {
            SetKitDropdownSelectionWithoutNotify(dropdown, fallbackPresetId);
        }
        finally
        {
            suppressKitSelectionChanged = false;
        }

        UpdateKitPreviews();
        UpdateKitSimilarityScore();
        UpdateKitValidation(message);
        SetCreateGameButtonEnabled(true);
    }

    private void RejectKitSelectionChangeAndRestoreLastValidSet(string message)
    {
        suppressKitSelectionChanged = true;
        try
        {
            SetKitDropdownSelectionWithoutNotify(homeKitDropdown, lastValidHomeKitId);
            SetKitDropdownSelectionWithoutNotify(awayKitDropdown, lastValidAwayKitId);
            SetKitDropdownSelectionWithoutNotify(homeGKKitDropdown, lastValidHomeGKKitId);
            SetKitDropdownSelectionWithoutNotify(awayGKKitDropdown, lastValidAwayGKKitId);
        }
        finally
        {
            suppressKitSelectionChanged = false;
        }

        UpdateKitPreviews();
        UpdateKitSimilarityScore();
        UpdateKitValidation(message);
        SetCreateGameButtonEnabled(true);
    }

    private void RepairGoalkeeperKitSelections(TMP_Dropdown changedDropdown)
    {
        TokenKitPreset homePreset = GetSelectedKitPreset(homeKitDropdown);
        TokenKitPreset awayPreset = GetSelectedKitPreset(awayKitDropdown);
        TokenKitPreset homeGkPreset = GetSelectedKitPreset(homeGKKitDropdown);
        TokenKitPreset awayGkPreset = GetSelectedKitPreset(awayGKKitDropdown);

        suppressKitSelectionChanged = true;
        try
        {
            bool homeGkWasChanged = changedDropdown == homeGKKitDropdown;
            bool awayGkWasChanged = changedDropdown == awayGKKitDropdown;

            if (!homeGkWasChanged
                && !IsGoalkeeperKitCompatible(homeGkPreset, homePreset, awayPreset, awayGkPreset))
            {
                TokenKitPreset replacement = FindCompatibleGoalkeeperKit(homePreset, awayPreset, awayGkPreset);
                if (replacement != null)
                {
                    SetKitDropdownSelectionWithoutNotify(homeGKKitDropdown, replacement.Id);
                    homeGkPreset = replacement;
                }
            }

            if (!awayGkWasChanged
                && !IsGoalkeeperKitCompatible(awayGkPreset, homePreset, awayPreset, homeGkPreset))
            {
                TokenKitPreset replacement = FindCompatibleGoalkeeperKit(homePreset, awayPreset, homeGkPreset);
                if (replacement != null)
                {
                    SetKitDropdownSelectionWithoutNotify(awayGKKitDropdown, replacement.Id);
                    awayGkPreset = replacement;
                }
            }

            if (homeGkWasChanged
                && !IsGoalkeeperKitCompatible(awayGkPreset, homePreset, awayPreset, homeGkPreset))
            {
                TokenKitPreset replacement = FindCompatibleGoalkeeperKit(homePreset, awayPreset, homeGkPreset);
                if (replacement != null)
                {
                    SetKitDropdownSelectionWithoutNotify(awayGKKitDropdown, replacement.Id);
                }
            }
            else if (awayGkWasChanged
                && !IsGoalkeeperKitCompatible(homeGkPreset, homePreset, awayPreset, awayGkPreset))
            {
                TokenKitPreset replacement = FindCompatibleGoalkeeperKit(homePreset, awayPreset, awayGkPreset);
                if (replacement != null)
                {
                    SetKitDropdownSelectionWithoutNotify(homeGKKitDropdown, replacement.Id);
                    homeGkPreset = replacement;
                }
            }

            if (!homeGkWasChanged
                && !awayGkWasChanged
                && (!IsGoalkeeperKitCompatible(homeGkPreset, homePreset, awayPreset, awayGkPreset)
                    || !IsGoalkeeperKitCompatible(awayGkPreset, homePreset, awayPreset, homeGkPreset)))
            {
                TokenKitPreset homeReplacement = FindCompatibleGoalkeeperKit(homePreset, awayPreset, null);
                TokenKitPreset awayReplacement = FindCompatibleGoalkeeperKit(homePreset, awayPreset, homeReplacement);
                if (homeReplacement != null && awayReplacement != null)
                {
                    SetKitDropdownSelectionWithoutNotify(homeGKKitDropdown, homeReplacement.Id);
                    SetKitDropdownSelectionWithoutNotify(awayGKKitDropdown, awayReplacement.Id);
                }
            }
        }
        finally
        {
            suppressKitSelectionChanged = false;
        }
    }

    private TokenKitPreset FindCompatibleGoalkeeperKit(
        TokenKitPreset homePreset,
        TokenKitPreset awayPreset,
        TokenKitPreset otherGkPreset)
    {
        foreach (TokenKitPreset candidate in availableGKKitPresets ?? Array.Empty<TokenKitPreset>())
        {
            if (IsGoalkeeperKitCompatible(candidate, homePreset, awayPreset, otherGkPreset))
            {
                return candidate;
            }
        }

        return null;
    }

    private bool IsGoalkeeperKitCompatible(
        TokenKitPreset candidate,
        TokenKitPreset homePreset,
        TokenKitPreset awayPreset,
        TokenKitPreset otherGkPreset)
    {
        if (candidate == null)
        {
            return false;
        }

        if (otherGkPreset != null
            && string.Equals(candidate.Id, otherGkPreset.Id, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return !KitsClash(candidate, homePreset)
            && !KitsClash(candidate, awayPreset)
            && !KitsClash(candidate, otherGkPreset);
    }

    private static bool KitsClash(TokenKitPreset first, TokenKitPreset second)
    {
        if (first == null || second == null)
        {
            return false;
        }

        return TokenKitCatalog.GetSimilarityBreakdown(first.Id, second.Id).IsClash;
    }

    private void SetKitDropdownSelectionWithoutNotify(TMP_Dropdown dropdown, string presetId)
    {
        if (dropdown == null)
        {
            return;
        }

        int presetIndex = FindPresetIndex(presetId, GetPresetListForDropdown(dropdown));
        dropdown.SetValueWithoutNotify(presetIndex >= 0 ? presetIndex : 0);
        dropdown.RefreshShownValue();
    }

    private void RememberCurrentValidKitSelections()
    {
        TokenKitSimilarityBreakdown outfieldSimilarity = TokenKitCatalog.GetSimilarityBreakdown(GetSelectedKitPresetId(homeKitDropdown), GetSelectedKitPresetId(awayKitDropdown));
        if (!string.IsNullOrWhiteSpace(GetKitValidationMessage(outfieldSimilarity)))
        {
            return;
        }

        lastValidHomeKitId = GetSelectedKitPresetId(homeKitDropdown);
        lastValidAwayKitId = GetSelectedKitPresetId(awayKitDropdown);
        lastValidHomeGKKitId = GetSelectedKitPresetId(homeGKKitDropdown);
        lastValidAwayGKKitId = GetSelectedKitPresetId(awayGKKitDropdown);
    }

    private void OnHomeInternationalTeamChanged(int _)
    {
        if (isRefreshingInternationalTeamUi)
        {
            return;
        }

        HandleInternationalTeamChanged(homeInternationalTeamDropdown, awayInternationalTeamDropdown);
    }

    private void OnAwayInternationalTeamChanged(int _)
    {
        if (isRefreshingInternationalTeamUi)
        {
            return;
        }

        HandleInternationalTeamChanged(awayInternationalTeamDropdown, homeInternationalTeamDropdown);
    }

    private void HandleInternationalTeamChanged(TMP_Dropdown changedDropdown, TMP_Dropdown otherDropdown)
    {
        isRefreshingInternationalTeamUi = true;
        try
        {
            EnsureDifferentInternationalTeams(changedDropdown, otherDropdown);
            SyncInternationalTeamInputFields();
            RefreshInternationalTeamKitOptions();
            ForceInternationalOutfieldKitsToTeamDefaults();
        }
        finally
        {
            isRefreshingInternationalTeamUi = false;
        }

        UpdateKitPreviews();
        UpdateKitValidationForCurrentSelection();
    }

    private void EnsureDifferentInternationalTeams(TMP_Dropdown changedDropdown, TMP_Dropdown otherDropdown)
    {
        if (changedDropdown == null || otherDropdown == null || internationalTeamNames.Count <= 1)
        {
            return;
        }

        string changedTeam = GetSelectedInternationalTeamName(changedDropdown);
        string otherTeam = GetSelectedInternationalTeamName(otherDropdown);
        if (!string.Equals(changedTeam, otherTeam, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        int nextIndex = Mathf.Clamp(otherDropdown.value, 0, internationalTeamNames.Count - 1);
        for (int offset = 1; offset < internationalTeamNames.Count; offset++)
        {
            int candidateIndex = (nextIndex + offset) % internationalTeamNames.Count;
            if (!string.Equals(internationalTeamNames[candidateIndex], changedTeam, StringComparison.OrdinalIgnoreCase))
            {
                otherDropdown.SetValueWithoutNotify(candidateIndex);
                otherDropdown.RefreshShownValue();
                return;
            }
        }
    }

    private void ForceInternationalOutfieldKitsToTeamDefaults()
    {
        if (!IsInternationalMatchSelected())
        {
            return;
        }

        RefreshInternationalTeamKitOptions();
        InternationalTeamKitOptions homeOptions = GetInternationalTeamKitOptions(GetSelectedInternationalTeamName(homeInternationalTeamDropdown));
        InternationalTeamKitOptions awayOptions = GetInternationalTeamKitOptions(GetSelectedInternationalTeamName(awayInternationalTeamDropdown));

        suppressKitSelectionChanged = true;
        try
        {
            ApplyInternationalOutfieldKitPair(homeOptions, awayOptions);
            RepairGoalkeeperKitSelections(null);
            RememberCurrentValidKitSelections();
        }
        finally
        {
            suppressKitSelectionChanged = false;
        }
    }

    private void ApplyInternationalOutfieldKitPair(InternationalTeamKitOptions homeOptions, InternationalTeamKitOptions awayOptions)
    {
        if (TryApplyInternationalOutfieldKitPair(homeOptions.HomeKit, awayOptions.HomeKit)
            || TryApplyInternationalOutfieldKitPair(homeOptions.HomeKit, awayOptions.AwayKit)
            || TryApplyInternationalOutfieldKitPair(homeOptions.AwayKit, awayOptions.HomeKit)
            || TryApplyInternationalOutfieldKitPair(homeOptions.AwayKit, awayOptions.AwayKit))
        {
            return;
        }

        PopulateKitDropdown(homeKitDropdown, GetPreferredInternationalKitId(homeOptions));
        PopulateKitDropdown(awayKitDropdown, GetPreferredInternationalKitId(awayOptions));
        Debug.LogError(
            $"No compatible international kit pairing found for {GetSelectedInternationalTeamName(homeInternationalTeamDropdown)} vs {GetSelectedInternationalTeamName(awayInternationalTeamDropdown)}.");
    }

    private void ApplyInternationalOutfieldKitPairForManualChange(TMP_Dropdown changedDropdown)
    {
        RefreshInternationalTeamKitOptions();
        InternationalTeamKitOptions homeOptions = GetInternationalTeamKitOptions(GetSelectedInternationalTeamName(homeInternationalTeamDropdown));
        InternationalTeamKitOptions awayOptions = GetInternationalTeamKitOptions(GetSelectedInternationalTeamName(awayInternationalTeamDropdown));
        TokenKitPreset selectedPreset = GetSelectedKitPreset(changedDropdown);

        suppressKitSelectionChanged = true;
        try
        {
            if (changedDropdown == homeKitDropdown)
            {
                if (TryApplyInternationalOutfieldKitPair(selectedPreset, awayOptions.HomeKit)
                    || TryApplyInternationalOutfieldKitPair(selectedPreset, awayOptions.AwayKit))
                {
                    return;
                }
            }
            else if (changedDropdown == awayKitDropdown)
            {
                if (TryApplyInternationalOutfieldKitPair(homeOptions.HomeKit, selectedPreset)
                    || TryApplyInternationalOutfieldKitPair(homeOptions.AwayKit, selectedPreset))
                {
                    return;
                }
            }
        }
        finally
        {
            suppressKitSelectionChanged = false;
        }
    }

    private bool TryApplyInternationalOutfieldKitPair(TokenKitPreset homePreset, TokenKitPreset awayPreset)
    {
        if (homePreset == null || awayPreset == null)
        {
            return false;
        }

        TokenKitSimilarityBreakdown similarity = TokenKitCatalog.GetSimilarityBreakdown(homePreset.Id, awayPreset.Id);
        if (similarity.IsClash)
        {
            return false;
        }

        PopulateKitDropdown(homeKitDropdown, homePreset.Id);
        PopulateKitDropdown(awayKitDropdown, awayPreset.Id);
        return true;
    }

    private static string GetPreferredInternationalKitId(InternationalTeamKitOptions options)
    {
        return options.HomeKit?.Id
            ?? options.AwayKit?.Id
            ?? string.Empty;
    }

    private void UpdateKitPreviews()
    {
        UpdateKitPreview(homeKitPreviewImage, homeKitPreviewNumberText, GetSelectedKitPreset(homeKitDropdown));
        UpdateKitPreview(awayKitPreviewImage, awayKitPreviewNumberText, GetSelectedKitPreset(awayKitDropdown));
        UpdateKitPreview(homeGKKitPreviewImage, homeGKKitPreviewNumberText, GetSelectedKitPreset(homeGKKitDropdown), GKPreviewSampleNumber);
        UpdateKitPreview(awayGKKitPreviewImage, awayGKKitPreviewNumberText, GetSelectedKitPreset(awayGKKitDropdown), GKPreviewSampleNumber);
    }

    private void UpdateKitSimilarityScore()
    {
        string homeKitId = GetSelectedKitPresetId(homeKitDropdown);
        string awayKitId = GetSelectedKitPresetId(awayKitDropdown);
        TokenKitSimilarityBreakdown similarity = TokenKitCatalog.GetSimilarityBreakdown(homeKitId, awayKitId);
        UpdateKitSimilarityScore(similarity);
    }

    private void UpdateKitValidationForCurrentSelection()
    {
        string homeKitId = GetSelectedKitPresetId(homeKitDropdown);
        string awayKitId = GetSelectedKitPresetId(awayKitDropdown);
        UpdateKitValidationForCurrentSelection(TokenKitCatalog.GetSimilarityBreakdown(homeKitId, awayKitId));
    }

    private void UpdateKitValidationForCurrentSelection(TokenKitSimilarityBreakdown similarity)
    {
        UpdateKitSimilarityScore(similarity);

        string validationMessage = GetKitValidationMessage(similarity);
        if (!string.IsNullOrWhiteSpace(validationMessage))
        {
            UpdateKitValidation(validationMessage);
            SetCreateGameButtonEnabled(false);
            return;
        }

        UpdateKitValidation(string.Empty);
        SetCreateGameButtonEnabled(true);
    }

    private void UpdateKitSimilarityScore(TokenKitSimilarityBreakdown similarity)
    {
        if (kitSimilarityText == null)
        {
            return;
        }

        kitSimilarityText.text = $"Kit similarity index: {similarity.OverallScore:0.#}%  Body {similarity.BodyColorSimilarity:0.#}%  Top {similarity.TopFaceSimilarity:0.#}%";
        // kitSimilarityText.gameObject.SetActive(true);
    }

    private string BuildKitClashMessage(string attemptedPresetName, string otherPresetName, TokenKitSimilarityBreakdown similarity)
    {
        if (similarity.BodyColorSimilarity > TokenKitCatalog.KitComponentClashThreshold
            && similarity.TopFaceSimilarity > TokenKitCatalog.KitComponentClashThreshold)
        {
            return $"{attemptedPresetName} is too similar to {otherPresetName}: body and top-face clash.";
        }

        if (similarity.TopFaceSimilarity > TokenKitCatalog.KitComponentClashThreshold)
        {
            return $"{attemptedPresetName} is too similar to {otherPresetName} from above.";
        }

        if (similarity.BodyColorSimilarity > TokenKitCatalog.KitComponentClashThreshold)
        {
            return $"{attemptedPresetName} is too similar to {otherPresetName}: body colors clash.";
        }

        if (similarity.OverallScore >= TokenKitCatalog.ClashThreshold)
        {
            return $"{attemptedPresetName} is too similar to {otherPresetName}: weighted similarity is too high.";
        }

        return $"{attemptedPresetName} is too similar to {otherPresetName}.";
    }

    private string GetKitValidationMessage(TokenKitSimilarityBreakdown outfieldSimilarity)
    {
        if (IsInternationalMatchSelected()
            && string.Equals(GetSelectedInternationalTeamName(homeInternationalTeamDropdown), GetSelectedInternationalTeamName(awayInternationalTeamDropdown), StringComparison.OrdinalIgnoreCase))
        {
            return "Home and away international teams cannot be the same.";
        }

        TokenKitPreset homePreset = GetSelectedKitPreset(homeKitDropdown);
        TokenKitPreset awayPreset = GetSelectedKitPreset(awayKitDropdown);
        TokenKitPreset homeGkPreset = GetSelectedKitPreset(homeGKKitDropdown);
        TokenKitPreset awayGkPreset = GetSelectedKitPreset(awayGKKitDropdown);

        if (outfieldSimilarity.IsClash)
        {
            return BuildKitClashMessage(homePreset?.DisplayName ?? "Home kit", awayPreset?.DisplayName ?? "Away kit", outfieldSimilarity);
        }

        if (homeGkPreset != null && awayGkPreset != null
            && string.Equals(homeGkPreset.Id, awayGkPreset.Id, StringComparison.OrdinalIgnoreCase))
        {
            return "Home and away goalkeepers cannot use the same kit.";
        }

        string gkClashMessage = BuildFirstGkKitClashMessage(
            homeGkPreset,
            awayGkPreset,
            homePreset,
            awayPreset);
        if (!string.IsNullOrWhiteSpace(gkClashMessage))
        {
            return gkClashMessage;
        }

        return string.Empty;
    }

    private string BuildFirstGkKitClashMessage(
        TokenKitPreset homeGkPreset,
        TokenKitPreset awayGkPreset,
        TokenKitPreset homePreset,
        TokenKitPreset awayPreset)
    {
        string message = BuildGkKitClashMessage(homeGkPreset, awayGkPreset, "Home GK", "Away GK");
        if (!string.IsNullOrWhiteSpace(message)) return message;

        message = BuildGkKitClashMessage(homeGkPreset, homePreset, "Home GK", "Home outfield");
        if (!string.IsNullOrWhiteSpace(message)) return message;

        message = BuildGkKitClashMessage(homeGkPreset, awayPreset, "Home GK", "Away outfield");
        if (!string.IsNullOrWhiteSpace(message)) return message;

        message = BuildGkKitClashMessage(awayGkPreset, homePreset, "Away GK", "Home outfield");
        if (!string.IsNullOrWhiteSpace(message)) return message;

        return BuildGkKitClashMessage(awayGkPreset, awayPreset, "Away GK", "Away outfield");
    }

    private string BuildGkKitClashMessage(TokenKitPreset gkPreset, TokenKitPreset otherPreset, string gkLabel, string otherLabel)
    {
        if (gkPreset == null || otherPreset == null)
        {
            return string.Empty;
        }

        TokenKitSimilarityBreakdown similarity = TokenKitCatalog.GetSimilarityBreakdown(gkPreset.Id, otherPreset.Id);
        if (!similarity.IsClash)
        {
            return string.Empty;
        }

        return BuildKitClashMessage(
            $"{gkLabel} ({gkPreset.DisplayName})",
            $"{otherLabel} ({otherPreset.DisplayName})",
            similarity);
    }

    private void ConfigureKitDropdownNavigation(TMP_Dropdown dropdown)
    {
        if (dropdown == null)
        {
            return;
        }

        ScrollRect templateScrollRect = dropdown.template != null
            ? dropdown.template.GetComponentInChildren<ScrollRect>(true)
            : null;
        if (templateScrollRect != null)
        {
            templateScrollRect.scrollSensitivity = KitDropdownScrollSensitivity;
        }

        Transform item = dropdown.template != null ? dropdown.template.Find("Viewport/Content/Item") : null;
        if (item == null)
        {
            return;
        }

        Selectable selectable = item.GetComponent<Selectable>();
        if (selectable == null)
        {
            return;
        }

        ColorBlock colors = selectable.colors;
        colors.normalColor = new Color(1f, 1f, 1f, 0f);
        colors.highlightedColor = new Color(1f, 1f, 1f, 0f);
        colors.selectedColor = new Color(1f, 1f, 1f, 0f);
        colors.pressedColor = new Color(1f, 1f, 1f, 0.18f);
        colors.fadeDuration = 0f;
        selectable.colors = colors;

        KitDropdownHoverPainter painter = item.GetComponent<KitDropdownHoverPainter>();
        if (painter == null)
        {
            painter = item.gameObject.AddComponent<KitDropdownHoverPainter>();
        }

        painter.Configure(dropdown, this);

        KitDropdownKeyboardStepper stepper = dropdown.GetComponent<KitDropdownKeyboardStepper>();
        if (stepper == null)
        {
            stepper = dropdown.gameObject.AddComponent<KitDropdownKeyboardStepper>();
        }

        stepper.Configure(dropdown, this);

        Navigation navigation = dropdown.navigation;
        navigation.mode = Navigation.Mode.None;
        dropdown.navigation = navigation;
    }

    private void NotifyKitDropdownFocused(TMP_Dropdown dropdown)
    {
        if (dropdown == homeKitDropdown
            || dropdown == awayKitDropdown
            || dropdown == homeGKKitDropdown
            || dropdown == awayGKKitDropdown)
        {
            activeClosedKitDropdown = dropdown;
        }
    }

    private void ClearActiveClosedKitDropdown()
    {
        activeClosedKitDropdown = null;
    }

    private void HandleClosedKitDropdownArrowKeys()
    {
        if (activeClosedKitDropdown == null
            || EventSystem.current == null
            || IsKitDropdownListOpen(activeClosedKitDropdown))
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            StepKitDropdown(activeClosedKitDropdown, 1);
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            StepKitDropdown(activeClosedKitDropdown, -1);
        }
    }

    private void StepKitDropdown(TMP_Dropdown dropdown, int direction)
    {
        if (dropdown == null || dropdown.options.Count == 0)
        {
            return;
        }

        int nextValue = Mathf.Clamp(dropdown.value + direction, 0, dropdown.options.Count - 1);
        if (nextValue == dropdown.value)
        {
            return;
        }

        dropdown.value = nextValue;
        dropdown.RefreshShownValue();
        EventSystem.current.SetSelectedGameObject(dropdown.gameObject);
    }

    private static bool IsKitDropdownListOpen(TMP_Dropdown dropdown)
    {
        if (dropdown == null)
        {
            return false;
        }

        string dropdownListName = $"{dropdown.name} Dropdown List";
        Canvas rootCanvas = dropdown.GetComponentInParent<Canvas>();
        if (rootCanvas == null)
        {
            return GameObject.Find(dropdownListName) != null;
        }

        Transform[] children = rootCanvas.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child != null && child.name == dropdownListName)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsInternationalMatchSelected()
    {
        return matchTypeDropdown != null
            && matchTypeDropdown.options.Count > 0
            && matchTypeDropdown.value >= 0
            && matchTypeDropdown.value < matchTypeDropdown.options.Count
            && string.Equals(matchTypeDropdown.options[matchTypeDropdown.value].text, MatchTypeInternational, StringComparison.OrdinalIgnoreCase);
    }

    private string GetSelectedInternationalTeamName(TMP_Dropdown dropdown)
    {
        if (dropdown == null || dropdown.options.Count == 0)
        {
            return string.Empty;
        }

        int index = Mathf.Clamp(dropdown.value, 0, dropdown.options.Count - 1);
        return dropdown.options[index].text;
    }

    private string GetHomeTeamNameForSettings()
    {
        string teamName = IsInternationalMatchSelected()
            ? GetSelectedInternationalTeamName(homeInternationalTeamDropdown)
            : homeTeamInputField != null ? homeTeamInputField.text : string.Empty;
        return NormalizeTeamNameForSettings(teamName);
    }

    private string GetAwayTeamNameForSettings()
    {
        string teamName = IsInternationalMatchSelected()
            ? GetSelectedInternationalTeamName(awayInternationalTeamDropdown)
            : awayTeamInputField != null ? awayTeamInputField.text : string.Empty;
        return NormalizeTeamNameForSettings(teamName);
    }

    private void SyncInternationalTeamInputFields()
    {
        if (!IsInternationalMatchSelected())
        {
            return;
        }

        if (homeTeamInputField != null)
        {
            homeTeamInputField.SetTextWithoutNotify(GetSelectedInternationalTeamName(homeInternationalTeamDropdown));
        }

        if (awayTeamInputField != null)
        {
            awayTeamInputField.SetTextWithoutNotify(GetSelectedInternationalTeamName(awayInternationalTeamDropdown));
        }

        RefreshCreateGameButtonState();
    }

    private void SetInternationalTeamSelectionVisible(bool visible)
    {
        if (homeTeamInputField != null)
        {
            homeTeamInputField.interactable = !visible;
            homeTeamInputField.gameObject.SetActive(!visible);
        }

        if (awayTeamInputField != null)
        {
            awayTeamInputField.interactable = !visible;
            awayTeamInputField.gameObject.SetActive(!visible);
        }

        if (homeInternationalTeamDropdown != null)
        {
            homeInternationalTeamDropdown.gameObject.SetActive(visible);
        }

        if (awayInternationalTeamDropdown != null)
        {
            awayInternationalTeamDropdown.gameObject.SetActive(visible);
        }
    }

    private void UpdateKitPreview(RawImage previewImage, TMP_Text previewNumberText, TokenKitPreset preset, string sampleNumber = PreviewSampleNumber)
    {
        if (preset == null)
        {
            return;
        }

        int sampleJersey = int.TryParse(sampleNumber, out int parsedSampleNumber) ? parsedSampleNumber : 0;
        TokenFaceUiRenderer.Render(
            previewImage,
            previewNumberText,
            preset.Style,
            sampleJersey,
            PreviewPlainNumberFontSize,
            PreviewVerticalNumberFontSize);
    }

    private TokenKitPreset FindKitPresetByDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return null;
        }

        foreach (TokenKitPreset preset in availableKitPresets ?? Array.Empty<TokenKitPreset>())
        {
            if (preset != null && string.Equals(preset.DisplayName, displayName, StringComparison.Ordinal))
            {
                return preset;
            }
        }

        foreach (TokenKitPreset preset in availableGKKitPresets ?? Array.Empty<TokenKitPreset>())
        {
            if (preset != null && string.Equals(preset.DisplayName, displayName, StringComparison.Ordinal))
            {
                return preset;
            }
        }

        return null;
    }

    private static Color GetReadableTextColor(Color background)
    {
        float luminance = (0.2126f * background.r) + (0.7152f * background.g) + (0.0722f * background.b);
        return luminance >= 0.5f ? Color.black : Color.white;
    }

    private static bool HasReadableContrast(Color background, Color foreground)
    {
        float backgroundLuminance = (0.2126f * background.r) + (0.7152f * background.g) + (0.0722f * background.b);
        float foregroundLuminance = (0.2126f * foreground.r) + (0.7152f * foreground.g) + (0.0722f * foreground.b);
        return Mathf.Abs(backgroundLuminance - foregroundLuminance) >= 0.42f;
    }

    private void UpdateKitValidation(string message)
    {
        if (kitValidationText == null)
        {
            return;
        }

        kitValidationText.text = message;
        kitValidationText.gameObject.SetActive(!string.IsNullOrWhiteSpace(message));
    }

    private void SetCreateGameButtonEnabled(bool enabled)
    {
        requestedCreateGameButtonEnabled = enabled;
        RefreshCreateGameButtonState();
    }

    private void RefreshCreateGameButtonState()
    {
        if (createGameButton == null)
        {
            return;
        }

        createGameButton.interactable = requestedCreateGameButtonEnabled
            && AreTeamNamesValid();
    }

    private bool AreTeamNamesValid()
    {
        return TryGetValidatedTeamNames(out _, out _, out _);
    }

    private bool TryGetValidatedTeamNames(out string homeTeamName, out string awayTeamName, out string validationMessage)
    {
        homeTeamName = GetHomeTeamNameForSettings();
        awayTeamName = GetAwayTeamNameForSettings();
        validationMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(homeTeamName) || string.IsNullOrWhiteSpace(awayTeamName))
        {
            validationMessage = "Both team names are required.";
            return false;
        }

        if (string.Equals(homeTeamName, awayTeamName, StringComparison.OrdinalIgnoreCase))
        {
            validationMessage = "Team names must be different.";
            return false;
        }

        return true;
    }

    private static string NormalizeTeamNameForSettings(string teamName)
    {
        return LimitTeamNameLength((teamName ?? string.Empty).Trim());
    }

    private static string LimitTeamNameLength(string teamName)
    {
        if (string.IsNullOrEmpty(teamName) || teamName.Length <= TeamNameCharacterLimit)
        {
            return teamName ?? string.Empty;
        }

        return teamName.Substring(0, TeamNameCharacterLimit);
    }

    public void BackToCreateLoadRoomMenu()
    {
        PlayerPrefs.DeleteKey(CreateNewGameReturnSourcePlayerPrefsKey);
        PlayerPrefs.Save();
        ApplicationManager.EnsureInstanceExists();
        ApplicationManager.Instance.SetSelectedRoomGameMode(GetSelectedCreateMenuGameMode());
        SceneManager.LoadScene(CreateLoadRoomSceneName);
    }

    public void BackToHotSeatMenu()
    {
        BackToCreateLoadRoomMenu();
    }

    private string GetRequestedCreateLoadGameMode()
    {
        ApplicationManager.EnsureInstanceExists();
        string requestedGameMode = ApplicationManager.Instance.SelectedRoomGameMode;
        return string.IsNullOrWhiteSpace(requestedGameMode)
            ? ApplicationManager.HotSeatGameMode
            : requestedGameMode.Trim();
    }

    private string GetSelectedCreateMenuGameMode()
    {
        if (IsSinglePlayerCreateMode())
        {
            return ApplicationManager.SinglePlayerGameMode;
        }

        if (gameModeDropdown != null
            && gameModeDropdown.options.Count > 0
            && gameModeDropdown.value >= 0
            && gameModeDropdown.value < gameModeDropdown.options.Count)
        {
            return gameModeDropdown.options[gameModeDropdown.value].text;
        }

        return GetRequestedCreateLoadGameMode();
    }

    private bool IsSinglePlayerCreateMode()
    {
        return string.Equals(GetRequestedCreateLoadGameMode(), ApplicationManager.SinglePlayerGameMode, StringComparison.OrdinalIgnoreCase);
    }

    private string GetSelectedGameModeForSettings()
    {
        return IsSinglePlayerCreateMode()
            ? ApplicationManager.SinglePlayerGameMode
            : GetSelectedCreateMenuGameMode();
    }

    private string GetSelectedOpponentDraftProfile()
    {
        return DraftProfileSophisticated;
    }

    private void RefreshGameModeLabelForCreateMode()
    {
        GameObject labelObject = GameObject.Find("Game Mode Label");
        TMP_Text label = labelObject != null ? labelObject.GetComponent<TMP_Text>() : null;
        if (label != null)
        {
            label.text = "Game Mode";
        }
    }

    private void ConfigureBackToGameModeMenuButton()
    {
        Button backButton = ResolveBackToGameModeMenuButton();
        if (backButton == null)
        {
            return;
        }

        TMP_Text label = backButton.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.text = $"Back to {GetSelectedCreateMenuGameMode()} Menu";
        }
    }

    private Button ResolveBackToGameModeMenuButton()
    {
        if (backToGameModeMenuButton != null)
        {
            return backToGameModeMenuButton;
        }

        GameObject buttonObject = GameObject.Find("Back to Game Mode Menu")
            ?? GameObject.Find("Back to Hot Seat Menu");
        if (buttonObject != null)
        {
            backToGameModeMenuButton = buttonObject.GetComponent<Button>();
        }

        return backToGameModeMenuButton;
    }

    private void TryRestoreExistingSettingsFromDraftReturn()
    {
        string returnSource = PlayerPrefs.GetString(CreateNewGameReturnSourcePlayerPrefsKey, string.Empty);
        if (!IsDraftReturnSource(returnSource))
        {
            return;
        }

        PlayerPrefs.DeleteKey(CreateNewGameReturnSourcePlayerPrefsKey);
        PlayerPrefs.Save();

        string settingsFilePath = ResolveExistingGameSettingsFilePath();
        if (string.IsNullOrWhiteSpace(settingsFilePath))
        {
            Debug.LogWarning($"CreateNewHSGameScene was opened from {returnSource}, but no existing game-settings JSON was found.");
            return;
        }

        GameSettings restoredSettings = LoadGameSettingsFromJson(settingsFilePath);
        if (restoredSettings == null)
        {
            Debug.LogWarning($"CreateNewHSGameScene could not restore game settings from: {settingsFilePath}");
            return;
        }

        existingDraftSettingsFilePath = settingsFilePath;
        isEditingExistingDraftSettings = true;

        ApplicationManager.EnsureInstanceExists();
        ApplicationManager.Instance.SetActiveSaveFilePath(settingsFilePath);
        PlayerPrefs.SetString(CurrentGameSettingsPlayerPrefsKey, settingsFilePath);
        PlayerPrefs.Save();

        ApplyExistingSettingsToUi(restoredSettings);
        Debug.Log($"Restored create-game settings from existing draft save: {settingsFilePath}");
    }

    private static bool IsDraftReturnSource(string returnSource)
    {
        return string.Equals(returnSource, DraftSceneName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(returnSource, FreeDraftSceneName, StringComparison.OrdinalIgnoreCase);
    }

    private string ResolveExistingGameSettingsFilePath()
    {
        ApplicationManager.EnsureInstanceExists();

        string explicitFilePath = ApplicationManager.Instance.GetLastSavedFilePath();
        if (!string.IsNullOrWhiteSpace(explicitFilePath) && File.Exists(explicitFilePath))
        {
            return explicitFilePath;
        }

        string playerPrefsPath = PlayerPrefs.GetString(CurrentGameSettingsPlayerPrefsKey, string.Empty);
        if (string.IsNullOrWhiteSpace(playerPrefsPath))
        {
            return string.Empty;
        }

        string resolvedPlayerPrefsPath = Path.IsPathRooted(playerPrefsPath)
            ? playerPrefsPath
            : Path.Combine(ApplicationManager.Instance.GetSaveFolderPath(), playerPrefsPath);

        return File.Exists(resolvedPlayerPrefsPath)
            ? resolvedPlayerPrefsPath
            : string.Empty;
    }

    private GameSettings LoadGameSettingsFromJson(string settingsFilePath)
    {
        try
        {
            JObject root = JObject.Parse(File.ReadAllText(settingsFilePath));
            JToken settingsToken = root["gameSettings"];
            return settingsToken?.ToObject<GameSettings>();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to read create-game settings from '{settingsFilePath}': {ex.Message}");
            return null;
        }
    }

    private void ApplyExistingSettingsToUi(GameSettings settings)
    {
        if (settings == null)
        {
            return;
        }

        string restoredGameModeSelection = IsSinglePlayerCreateMode()
            ? ApplicationManager.SinglePlayerGameMode
            : settings.gameMode;
        string restoredGameModeFallback = IsSinglePlayerCreateMode()
            ? ApplicationManager.SinglePlayerGameMode
            : GetRequestedCreateLoadGameMode();
        SelectDropdownOption(gameModeDropdown, restoredGameModeSelection, restoredGameModeFallback);
        ApplyExistingTeamControlSettings(settings);
        ConfigureBackToGameModeMenuButton();
        RefreshGameModeLabelForCreateMode();
        SetCreateGameButtonEnabled(true);

        if (halfDurationSlider != null)
        {
            halfDurationSlider.SetValueWithoutNotify(Mathf.Clamp(settings.halfDuration, halfDurationSlider.minValue, halfDurationSlider.maxValue));
            UpdateHalfDurationSliderText(halfDurationSlider.value);
        }

        SetPlayerAssistanceDropdownValue(settings.playerAssistance);

        SelectDropdownOption(numberOfHalvesDropdown, settings.numberOfHalfs.ToString(), "2");
        RefreshTiebreakerOptions();
        SelectDropdownOption(tiebreakerDropdown, settings.tiebreaker, TieBreakerNone);
        SelectDropdownOption(matchTypeDropdown, settings.matchType, "Regular");

        SetInputFieldTextWithoutNotify(homeTeamInputField, settings.homeTeamName);
        SetInputFieldTextWithoutNotify(awayTeamInputField, settings.awayTeamName);
        RefreshCreateGameButtonState();

        if (IsInternationalSettings(settings))
        {
            SelectDropdownOption(homeInternationalTeamDropdown, settings.homeTeamName);
            SelectDropdownOption(awayInternationalTeamDropdown, settings.awayTeamName);
        }

        suppressRegularMatchSelectionSnapshot = true;
        try
        {
            OnMatchTypeChanged();
        }
        finally
        {
            suppressRegularMatchSelectionSnapshot = false;
        }
        SelectDropdownOption(squadSizeDropdown, settings.squadSize, IsInternationalMatchSelected() ? "18" : "16");

        if (IsInternationalMatchSelected())
        {
            SyncInternationalTeamInputFields();
            SetInternationalDraftDropdownOptions();
        }
        else
        {
            ApplyExistingRosterSourceToggles(settings);
            SelectDropdownOption(draftDropdown, NormalizeDraftSelection(settings.draft), "Regular");
            SelectDropdownOption(gkDraftDropdown, settings.gkDraft, "Deal");
        }

        SelectDropdownOption(refereeDropdown, settings.referee, "Random");
        SelectDropdownOption(weatherDropdown, settings.weatherConditions, "Clear");
        AdjustBallColorBasedOnWeather();
        string restoredBallColor = string.Equals(GetSelectedDropdownText(weatherDropdown), "Snow", StringComparison.OrdinalIgnoreCase)
            ? "Orange"
            : settings.ballColor;
        SelectDropdownOption(ballColorDropdown, restoredBallColor, "White");

        ApplySavedKitSelections(settings);
    }

    private void ApplyExistingTeamControlSettings(GameSettings settings)
    {
        if (!IsSinglePlayerCreateMode() || settings == null)
        {
            return;
        }

        bool homeCpuControlled = IsExplicitCpuControl(settings.homeTeamControl)
            || IsCpuControlled(settings.homeRoomPersona);
        bool awayCpuControlled = IsExplicitCpuControl(settings.awayTeamControl)
            || IsCpuControlled(settings.awayRoomPersona)
            || (string.IsNullOrWhiteSpace(settings.awayTeamControl)
                && string.IsNullOrWhiteSpace(settings.awayRoomPersona)
                && !string.IsNullOrWhiteSpace(settings.awayDraftPersona));

        SetTeamControlWithoutNotify(homeTeamControlDropdown, homeCpuControlled ? TeamControlCpuRandomLabel : TeamControlHumanLabel);
        SetTeamControlWithoutNotify(awayTeamControlDropdown, awayCpuControlled ? TeamControlCpuRandomLabel : TeamControlHumanLabel);
        RefreshTeamControlValidity(null);
    }

    private static bool IsInternationalSettings(GameSettings settings)
    {
        return settings != null
            && string.Equals(settings.matchType, MatchTypeInternational, StringComparison.OrdinalIgnoreCase);
    }

    private void ApplyExistingRosterSourceToggles(GameSettings settings)
    {
        SetToggleWithoutNotify(includeTabletopiaToggle, settings.includeTabletopia);
        SetToggleWithoutNotify(includeNonTabletopiaToggle, settings.includeNonTabletopia);
        SetToggleWithoutNotify(includeInternationalsToggle, settings.includeInternationals);
        SetToggleWithoutNotify(includeTabletopiaGKToggle, settings.includeTabletopiaGK);
        SetToggleWithoutNotify(includeNonTabletopiaGKToggle, settings.includeNonTabletopiaGK);
        SetToggleWithoutNotify(includeInternationalsGKToggle, settings.includeInternationalsGK);

        if (!IsToggleOn(includeTabletopiaToggle) && !IsToggleOn(includeNonTabletopiaToggle) && !IsToggleOn(includeInternationalsToggle))
        {
            SetToggleWithoutNotify(includeTabletopiaToggle, true);
        }

        if (!IsToggleOn(includeTabletopiaGKToggle) && !IsToggleOn(includeNonTabletopiaGKToggle) && !IsToggleOn(includeInternationalsGKToggle))
        {
            SetToggleWithoutNotify(includeTabletopiaGKToggle, true);
        }
    }

    private void ApplySavedKitSelections(GameSettings settings)
    {
        RefreshInternationalTeamKitOptions();
        suppressKitSelectionChanged = true;
        try
        {
            PopulateKitDropdown(homeKitDropdown, string.IsNullOrWhiteSpace(settings.homeKit) ? DefaultHomeKitId : settings.homeKit);
            PopulateKitDropdown(awayKitDropdown, string.IsNullOrWhiteSpace(settings.awayKit) ? DefaultAwayKitId : settings.awayKit);
            PopulateKitDropdown(homeGKKitDropdown, string.IsNullOrWhiteSpace(settings.homeGKKit) ? DefaultHomeGKKitId : settings.homeGKKit);
            PopulateKitDropdown(awayGKKitDropdown, string.IsNullOrWhiteSpace(settings.awayGKKit) ? DefaultAwayGKKitId : settings.awayGKKit);
            RepairGoalkeeperKitSelections(null);
            RememberCurrentValidKitSelections();
        }
        finally
        {
            suppressKitSelectionChanged = false;
        }

        UpdateKitPreviews();
        UpdateKitValidationForCurrentSelection();
    }

    private static void SelectDropdownOption(TMP_Dropdown dropdown, string selectedValue, string fallbackValue = null)
    {
        if (dropdown == null || dropdown.options.Count == 0)
        {
            return;
        }

        int selectedIndex = FindDropdownOptionIndex(dropdown, selectedValue);
        if (selectedIndex < 0 && !string.IsNullOrWhiteSpace(fallbackValue))
        {
            selectedIndex = FindDropdownOptionIndex(dropdown, fallbackValue);
        }

        dropdown.SetValueWithoutNotify(selectedIndex >= 0 ? selectedIndex : 0);
        dropdown.RefreshShownValue();
    }

    private static string NormalizeDraftSelection(string draftSelection)
    {
        return string.Equals(draftSelection, "Free", StringComparison.OrdinalIgnoreCase)
            ? DraftArcade
            : draftSelection;
    }

    private static int FindDropdownOptionIndex(TMP_Dropdown dropdown, string selectedValue)
    {
        if (dropdown == null || string.IsNullOrWhiteSpace(selectedValue))
        {
            return -1;
        }

        for (int i = 0; i < dropdown.options.Count; i++)
        {
            if (string.Equals(dropdown.options[i].text, selectedValue, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private static string GetSelectedDropdownText(TMP_Dropdown dropdown)
    {
        if (dropdown == null || dropdown.options.Count == 0)
        {
            return string.Empty;
        }

        int index = Mathf.Clamp(dropdown.value, 0, dropdown.options.Count - 1);
        return dropdown.options[index].text;
    }

    private static void SetInputFieldTextWithoutNotify(TMP_InputField inputField, string value)
    {
        if (inputField == null)
        {
            return;
        }

        inputField.characterLimit = TeamNameCharacterLimit;
        inputField.SetTextWithoutNotify(LimitTeamNameLength(value ?? string.Empty));
    }

    private static void SetToggleWithoutNotify(Toggle toggle, bool isOn)
    {
        if (toggle == null)
        {
            return;
        }

        toggle.SetIsOnWithoutNotify(isOn);
    }

    private static bool IsToggleOn(Toggle toggle)
    {
        return toggle != null && toggle.isOn;
    }

    private void CaptureRegularMatchSelectionSnapshot()
    {
        if (suppressRegularMatchSelectionSnapshot
            || homeTeamInputField == null
            || !homeTeamInputField.gameObject.activeSelf)
        {
            return;
        }

        regularMatchSelectionSnapshot = new RegularMatchSelectionSnapshot
        {
            homeTeamName = homeTeamInputField.text,
            awayTeamName = awayTeamInputField != null ? awayTeamInputField.text : string.Empty,
            squadSize = GetSelectedDropdownText(squadSizeDropdown),
            draft = GetSelectedDropdownText(draftDropdown),
            gkDraft = GetSelectedDropdownText(gkDraftDropdown),
            homeKit = GetSelectedKitPresetId(homeKitDropdown),
            awayKit = GetSelectedKitPresetId(awayKitDropdown),
            homeGKKit = GetSelectedKitPresetId(homeGKKitDropdown),
            awayGKKit = GetSelectedKitPresetId(awayGKKitDropdown),
            includeTabletopia = IsToggleOn(includeTabletopiaToggle),
            includeNonTabletopia = IsToggleOn(includeNonTabletopiaToggle),
            includeInternationals = IsToggleOn(includeInternationalsToggle),
            includeTabletopiaGK = IsToggleOn(includeTabletopiaGKToggle),
            includeNonTabletopiaGK = IsToggleOn(includeNonTabletopiaGKToggle),
            includeInternationalsGK = IsToggleOn(includeInternationalsGKToggle),
            hasValue = true
        };
    }

    private void RestoreRegularMatchSelectionSnapshot()
    {
        if (regularMatchSelectionSnapshot == null || !regularMatchSelectionSnapshot.hasValue)
        {
            return;
        }

        SetInputFieldTextWithoutNotify(homeTeamInputField, regularMatchSelectionSnapshot.homeTeamName);
        SetInputFieldTextWithoutNotify(awayTeamInputField, regularMatchSelectionSnapshot.awayTeamName);
        RefreshCreateGameButtonState();
        SelectDropdownOption(squadSizeDropdown, regularMatchSelectionSnapshot.squadSize, "16");
        SelectDropdownOption(draftDropdown, regularMatchSelectionSnapshot.draft, "Regular");
        SelectDropdownOption(gkDraftDropdown, regularMatchSelectionSnapshot.gkDraft, "Deal");

        SetToggleWithoutNotify(includeTabletopiaToggle, regularMatchSelectionSnapshot.includeTabletopia);
        SetToggleWithoutNotify(includeNonTabletopiaToggle, regularMatchSelectionSnapshot.includeNonTabletopia);
        SetToggleWithoutNotify(includeInternationalsToggle, regularMatchSelectionSnapshot.includeInternationals);
        SetToggleWithoutNotify(includeTabletopiaGKToggle, regularMatchSelectionSnapshot.includeTabletopiaGK);
        SetToggleWithoutNotify(includeNonTabletopiaGKToggle, regularMatchSelectionSnapshot.includeNonTabletopiaGK);
        SetToggleWithoutNotify(includeInternationalsGKToggle, regularMatchSelectionSnapshot.includeInternationalsGK);

        PopulateKitDropdown(homeKitDropdown, regularMatchSelectionSnapshot.homeKit);
        PopulateKitDropdown(awayKitDropdown, regularMatchSelectionSnapshot.awayKit);
        PopulateKitDropdown(homeGKKitDropdown, regularMatchSelectionSnapshot.homeGKKit);
        PopulateKitDropdown(awayGKKitDropdown, regularMatchSelectionSnapshot.awayGKKit);
        RepairGoalkeeperKitSelections(null);
        RememberCurrentValidKitSelections();
    }

    // Adjust the squad size dropdown based on match type selection
    void AdjustSquadSizeOptionsBasedOnMatchType()
    {
        // Clear current squad size options
        squadSizeDropdown.ClearOptions();

        // Change options based on the selected match type
        if (matchTypeDropdown.value == 0)  // Example: value 0 represents "regular"
        {
            squadSizeDropdown.AddOptions(new List<string> { "16", "18" });
        }
        else if (matchTypeDropdown.value == 1)  // Example: value 1 represents "international"
        {
            squadSizeDropdown.AddOptions(new List<string> { "18" }); // Fewer options for international matches
        }
        squadSizeDropdown.RefreshShownValue();  // Force UI update to reflect changes
    }
    void AdjustBallColorBasedOnWeather()
    {
        string previouslySelectedColor = ballColorDropdown.options.Count > 0
            ? ballColorDropdown.options[ballColorDropdown.value].text
            : "White";

        ballColorDropdown.ClearOptions();

        if (weatherDropdown.value == 2)  // Snow
        {
            ballColorDropdown.AddOptions(new List<string> { "Orange" });
        }
        else
        {
            ballColorDropdown.AddOptions(new List<string> { "White", "Orange", "Yellow" });
        }

        int restoredIndex = ballColorDropdown.options.FindIndex(option => option.text == previouslySelectedColor);
        ballColorDropdown.SetValueWithoutNotify(restoredIndex >= 0 ? restoredIndex : 0);
        ballColorDropdown.RefreshShownValue();
    }

    // Update checkboxes and squad size options when match type changes
    void OnMatchTypeChanged()
    {
        bool isInternationalMatch = matchTypeDropdown.value == 1;
        if (isInternationalMatch)
        {
            CaptureRegularMatchSelectionSnapshot();
        }

        AdjustSquadSizeOptionsBasedOnMatchType();  // Update squad size dropdown

        if (isInternationalMatch)  // International
        {
            isRefreshingInternationalTeamUi = true;
            try
            {
                EnsureDifferentInternationalTeams(homeInternationalTeamDropdown, awayInternationalTeamDropdown);
                SetInternationalTeamSelectionVisible(true);
                SyncInternationalTeamInputFields();
                ForceInternationalOutfieldKitsToTeamDefaults();
            }
            finally
            {
                isRefreshingInternationalTeamUi = false;
            }

            UpdateKitPreviews();
            UpdateKitValidationForCurrentSelection();
            SetInternationalDraftDropdownOptions();

            // Update checkboxes
            includeInternationalsToggle.isOn = true;
            includeInternationalsToggle.interactable = false;

            includeTabletopiaToggle.isOn = false;
            includeTabletopiaToggle.interactable = false;

            includeNonTabletopiaToggle.isOn = false;
            includeNonTabletopiaToggle.interactable = false;
            // Update GK checkboxes
            includeInternationalsGKToggle.isOn = true;
            includeInternationalsGKToggle.interactable = false;

            includeTabletopiaGKToggle.isOn = false;
            includeTabletopiaGKToggle.interactable = false;

            includeNonTabletopiaGKToggle.isOn = false;
            includeNonTabletopiaGKToggle.interactable = false;

            Debug.Log("Match type is International. Adjusted checkboxes and squad size.");
        }
        else
        {
            string currentHomeKitId = GetSelectedKitPresetId(homeKitDropdown);
            string currentAwayKitId = GetSelectedKitPresetId(awayKitDropdown);
            SetInternationalTeamSelectionVisible(false);
            homeAvailableKitPresets = availableKitPresets;
            awayAvailableKitPresets = availableKitPresets;
            PopulateKitDropdown(homeKitDropdown, currentHomeKitId);
            PopulateKitDropdown(awayKitDropdown, currentAwayKitId);
            UpdateKitPreviews();
            UpdateKitValidationForCurrentSelection();
            SetRegularDraftDropdownOptions();
            RestoreRegularMatchSelectionSnapshot();
            UpdateKitPreviews();
            UpdateKitValidationForCurrentSelection();

            // Regular or other match types - make all checkboxes interactive again
            includeTabletopiaToggle.interactable = true;
            includeNonTabletopiaToggle.interactable = true;
            includeInternationalsToggle.interactable = true;
            includeTabletopiaGKToggle.interactable = true;
            includeNonTabletopiaGKToggle.interactable = true;
            includeInternationalsGKToggle.interactable = true;

            Debug.Log("Match type is Regular or other. Reset checkboxes.");
        }
    }

    public void SaveGameSettingsToJson()
    {
        if (!TryGetValidatedTeamNames(out string homeTeamName, out string awayTeamName, out string teamValidationMessage))
        {
            RefreshCreateGameButtonState();
            Debug.LogWarning($"Create game blocked because team names are invalid: {teamValidationMessage}");
            return;
        }

        TokenKitSimilarityBreakdown currentSimilarity = TokenKitCatalog.GetSimilarityBreakdown(GetSelectedKitPresetId(homeKitDropdown), GetSelectedKitPresetId(awayKitDropdown));
        string kitValidationMessage = GetKitValidationMessage(currentSimilarity);
        if (!string.IsNullOrWhiteSpace(kitValidationMessage))
        {
            UpdateKitValidationForCurrentSelection(currentSimilarity);
            Debug.LogWarning($"Create game blocked because kit selection is invalid: {kitValidationMessage}");
            return;
        }

        // Create a GameSettings object and populate it from the UI input fields
        GameSettings settings = new GameSettings();
        settings.gameMode = GetSelectedGameModeForSettings();
        settings.halfDuration = (int)halfDurationSlider.value;
        settings.numberOfHalfs = int.Parse(numberOfHalvesDropdown.options[numberOfHalvesDropdown.value].text);
        RefreshTiebreakerOptions();
        settings.tiebreaker = tiebreakerDropdown.options[tiebreakerDropdown.value].text;
        settings.matchType = matchTypeDropdown.options[matchTypeDropdown.value].text;
        settings.squadSize = squadSizeDropdown.options[squadSizeDropdown.value].text;
        settings.draft = draftDropdown.options[draftDropdown.value].text;
        settings.gkDraft = gkDraftDropdown.options[gkDraftDropdown.value].text;
        string selectedReferee = refereeDropdown.options[refereeDropdown.value].text;
        // Check if the selected referee is "Random"
        if (selectedReferee == "Random")
        {
            // Create a list of referees excluding "Random"
            List<string> nonRandomReferees = new List<string> { "Webster - 2", "Castolo - 3", "Bakker - 4", "Read - 5" };
            // Randomly select one of the non-random referees
            System.Random rng = new System.Random();
            selectedReferee = nonRandomReferees[rng.Next(nonRandomReferees.Count)];
            Debug.Log($"Random referee chosen: {selectedReferee}");
        }
        settings.referee = selectedReferee;
        settings.playerAssistance = GetSelectedPlayerAssistanceValue();
        settings.weatherConditions = weatherDropdown.options[weatherDropdown.value].text;
        settings.ballColor = ballColorDropdown.options[ballColorDropdown.value].text;
        settings.homeTeamName = homeTeamName;
        settings.awayTeamName = awayTeamName;
        RefreshTeamControlValidity(null);
        bool homeCpuControlled = IsSinglePlayerCreateMode() && IsCpuControlled(homeTeamControlDropdown);
        bool awayCpuControlled = IsSinglePlayerCreateMode() && IsCpuControlled(awayTeamControlDropdown);
        settings.homeTeamControl = IsSinglePlayerCreateMode() ? GetTeamControlText(homeTeamControlDropdown) : TeamControlHumanLabel;
        settings.awayTeamControl = IsSinglePlayerCreateMode() ? GetTeamControlText(awayTeamControlDropdown) : TeamControlHumanLabel;
        bool usesRegularDraft = string.Equals(settings.draft, "Regular", StringComparison.OrdinalIgnoreCase);
        settings.homeDraftPersona = ShouldApplyCpuDraftPersona(homeCpuControlled, usesRegularDraft) ? DraftProfileSophisticated : string.Empty;
        settings.awayDraftPersona = ShouldApplyCpuDraftPersona(awayCpuControlled, usesRegularDraft) ? DraftProfileSophisticated : string.Empty;
        settings.defaultDraftPersona = IsSinglePlayerCreateMode() ? string.Empty : DraftProfileSophisticated;
        settings.homeRoomPersona = homeCpuControlled ? RoomPersonaRandom : RoomPersonaAskHuman;
        settings.awayRoomPersona = awayCpuControlled ? RoomPersonaRandom : RoomPersonaAskHuman;
        settings.defaultRoomPersona = homeCpuControlled && awayCpuControlled ? RoomPersonaRandom : RoomPersonaAskHuman;
        settings.includeTabletopia = includeTabletopiaToggle.isOn;
        settings.includeNonTabletopia = includeNonTabletopiaToggle.isOn;
        settings.includeInternationals = includeInternationalsToggle.isOn;
        settings.includeTabletopiaGK = includeTabletopiaGKToggle.isOn;
        settings.includeNonTabletopiaGK = includeNonTabletopiaGKToggle.isOn;
        settings.includeInternationalsGK = includeInternationalsGKToggle.isOn;
        TokenKitPreset selectedHomeKit = GetSelectedKitPreset(homeKitDropdown);
        TokenKitPreset selectedAwayKit = GetSelectedKitPreset(awayKitDropdown);
        TokenKitPreset selectedHomeGKKit = GetSelectedKitPreset(homeGKKitDropdown);
        TokenKitPreset selectedAwayGKKit = GetSelectedKitPreset(awayGKKitDropdown);
        settings.homeKit = selectedHomeKit?.DisplayName ?? GetSelectedKitPresetId(homeKitDropdown);
        settings.awayKit = selectedAwayKit?.DisplayName ?? GetSelectedKitPresetId(awayKitDropdown);
        settings.homeGKKit = selectedHomeGKKit?.DisplayName ?? GetSelectedKitPresetId(homeGKKitDropdown);
        settings.awayGKKit = selectedAwayGKKit?.DisplayName ?? GetSelectedKitPresetId(awayGKKitDropdown);
        Debug.Log($"[CreateNewGameManager] Selected kits: Home '{settings.homeKit}' (id '{selectedHomeKit?.Id ?? string.Empty}'), Away '{settings.awayKit}' (id '{selectedAwayKit?.Id ?? string.Empty}'), Home GK '{settings.homeGKKit}' (id '{selectedHomeGKKit?.Id ?? string.Empty}'), Away GK '{settings.awayGKKit}' (id '{selectedAwayGKKit?.Id ?? string.Empty}').");

        string path = ResolveGameSettingsSavePath(settings, out bool updatedExistingSave);
        string json = updatedExistingSave
            ? BuildUpdatedExistingGameDataJson(path, settings)
            : BuildNewGameDataJson(settings);

        // Write the file
        File.WriteAllText(path, json);
        // Persist the exact save reference so Draft/Room keep mutating the same JSON file.
        ApplicationManager.Instance.SetActiveSaveFilePath(path);
        PlayerPrefs.SetString(CurrentGameSettingsPlayerPrefsKey, path);
        PlayerPrefs.Save();

        // Log where the file was saved
        Debug.Log(updatedExistingSave
            ? $"Existing draft game settings updated at {path}"
            : $"Game settings saved to {path}");

        // Check the draft and gkDraft settings to determine the scene to load
        if ((settings.draft == "Regular" && settings.gkDraft == "Deal")
            || (settings.draft == DraftInternational && settings.gkDraft == DraftInternational))
        {
            Debug.Log("Loading the Regular Draft Scene...");
            SceneManager.LoadScene("Draft"); // Load the current Draft scene
        }
        else
        {
            Debug.Log("Non-regular draft selected. Loading the Free Draft Scene...");
            SceneManager.LoadScene("FreeDraft");
        }
    }

    private static bool ShouldApplyCpuDraftPersona(bool isCpuControlled, bool usesRegularDraft)
    {
        return isCpuControlled && usesRegularDraft;
    }

    private static bool IsCpuControlled(string roomPersona)
    {
        return string.Equals(roomPersona, RoomPersonaRandom, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsExplicitCpuControl(string teamControl)
    {
        return string.Equals(teamControl, TeamControlCpuRandomLabel, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsHumanControlled(TMP_Dropdown dropdown)
    {
        return string.Equals(GetTeamControlText(dropdown), TeamControlHumanLabel, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCpuControlled(TMP_Dropdown dropdown)
    {
        return string.Equals(GetTeamControlText(dropdown), TeamControlCpuRandomLabel, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetTeamControlText(TMP_Dropdown dropdown)
    {
        if (dropdown == null || dropdown.options.Count == 0 || dropdown.value < 0 || dropdown.value >= dropdown.options.Count)
        {
            return TeamControlHumanLabel;
        }

        return dropdown.options[dropdown.value].text;
    }

    private static void SetTeamControlWithoutNotify(TMP_Dropdown dropdown, string value)
    {
        if (dropdown == null)
        {
            return;
        }

        int index = dropdown.options.FindIndex(option => string.Equals(option.text, value, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            index = string.Equals(value, TeamControlCpuRandomLabel, StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        }

        dropdown.SetValueWithoutNotify(Mathf.Clamp(index, 0, Mathf.Max(0, dropdown.options.Count - 1)));
        dropdown.RefreshShownValue();
    }

    private string ResolveGameSettingsSavePath(GameSettings settings, out bool updatedExistingSave)
    {
        ApplicationManager.EnsureInstanceExists();
        updatedExistingSave = isEditingExistingDraftSettings
            && !string.IsNullOrWhiteSpace(existingDraftSettingsFilePath)
            && File.Exists(existingDraftSettingsFilePath)
            && !RoomSaveService.IsProtectedSavePath(existingDraftSettingsFilePath);

        if (updatedExistingSave)
        {
            return existingDraftSettingsFilePath;
        }

        return BuildNewGameSettingsFilePath(settings);
    }

    private string BuildNewGameSettingsFilePath(GameSettings settings)
    {
        // Generate random alphanumeric prefix
        string randomPrefix = GenerateRandomString(16);
        // Get current timestamp
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm");

        // Sanitize team names to avoid invalid characters in filenames
        string homeTeam = SanitizeFileName(settings.homeTeamName);
        string awayTeam = SanitizeFileName(settings.awayTeamName);
        string gameMode = SanitizeFileName(settings.gameMode);

        // Construct dynamic filename
        string fileName = $"{randomPrefix}_{timestamp}__{gameMode}__{homeTeam}__{awayTeam}.json";

        return Path.Combine(ApplicationManager.Instance.GetSaveFolderPath(), fileName);
    }

    private string BuildNewGameDataJson(GameSettings settings)
    {
        string nowUtc = DateTime.UtcNow.ToString("o");
        var gameData = new
        {
            saveSchemaVersion = RoomSaveService.SaveSchemaVersion,
            eventLogSchemaVersion = GameplayEvent.CurrentSchemaVersion,
            createdUtc = nowUtc,
            lastSavedUtc = nowUtc,
            gameSettings = settings,  // Grouped under "gameSettings"
            events = new List<GameplayEvent>(),
            runtimeSnapshot = (object)null
        };

        return JsonConvert.SerializeObject(gameData, Formatting.Indented);
    }

    private string BuildUpdatedExistingGameDataJson(string path, GameSettings settings)
    {
        try
        {
            DateTime nowUtc = DateTime.UtcNow;
            JObject root = JObject.Parse(File.ReadAllText(path));
            string createdUtc = root.Value<string>("createdUtc");
            if (string.IsNullOrWhiteSpace(createdUtc))
            {
                createdUtc = File.GetCreationTimeUtc(path).ToString("o");
            }

            root["saveSchemaVersion"] = RoomSaveService.SaveSchemaVersion;
            root["eventLogSchemaVersion"] = GameplayEvent.CurrentSchemaVersion;
            root["createdUtc"] = createdUtc;
            root["lastSavedUtc"] = nowUtc.ToString("o");
            root["gameSettings"] = JToken.FromObject(settings);

            if (root["events"] == null)
            {
                root["events"] = new JArray();
            }

            if (root["runtimeSnapshot"] == null)
            {
                root["runtimeSnapshot"] = JValue.CreateNull();
            }

            return root.ToString(Formatting.Indented);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to update existing draft save '{path}'. Rebuilding game settings JSON: {ex.Message}");
            return BuildNewGameDataJson(settings);
        }
    }

    // Helper function to sanitize file names by removing invalid characters
    private string SanitizeFileName(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        // Replace invalid characters with underscores
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            input = input.Replace(c, '_');
        }
        return input;
    }

    // Generate a random alphanumeric string of format xxxx-xxxx-xxxx-xxxx
    private string GenerateRandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmopqrstuvwxyz0123456789";
        string randomString = "";
        for (int i = 0; i < length; i++)
        {
            if (i > 0 && i % 4 == 0) randomString += "-";
            randomString += chars[UnityEngine.Random.Range(0, chars.Length)];
        }
        return randomString;
    }

    private sealed class InternationalTeamKitOptions
    {
        public static readonly InternationalTeamKitOptions Empty = new InternationalTeamKitOptions(null, null, new List<TokenKitPreset>());

        public TokenKitPreset HomeKit { get; }
        public TokenKitPreset AwayKit { get; }
        public IReadOnlyList<TokenKitPreset> AllKits { get; }

        public InternationalTeamKitOptions(TokenKitPreset homeKit, TokenKitPreset awayKit, IReadOnlyList<TokenKitPreset> allKits)
        {
            HomeKit = homeKit;
            AwayKit = awayKit;
            AllKits = allKits ?? Array.Empty<TokenKitPreset>();
        }
    }

    private sealed class KitDropdownHoverPainter : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        private TMP_Dropdown dropdown;
        private CreateNewGameManager owner;
        private Graphic targetGraphic;
        private TMP_Text label;
        private Color baseGraphicColor = new Color(1f, 1f, 1f, 0f);
        private Color baseLabelColor = Color.black;

        public void Configure(TMP_Dropdown sourceDropdown, CreateNewGameManager sourceOwner)
        {
            dropdown = sourceDropdown;
            owner = sourceOwner;
            ResolveReferences();
            CaptureBaseColors();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            ApplyHoverColors();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            RestoreBaseColors();
        }

        public void OnSelect(BaseEventData eventData)
        {
            ApplyHoverColors();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            RestoreBaseColors();
        }

        private void ApplyHoverColors()
        {
            ResolveReferences();
            if (owner == null || label == null)
            {
                return;
            }

            TokenKitPreset preset = owner.FindKitPresetByDisplayName(label.text);
            if (preset?.Style == null)
            {
                return;
            }

            TokenKitInstructionPalette palette = TokenKitCatalog.GetInstructionPalette(preset.Style);
            Color backgroundColor = Color.Lerp(palette.Primary, palette.Secondary, 0.25f);
            backgroundColor.a = 0.90f;

            if (targetGraphic != null)
            {
                targetGraphic.color = backgroundColor;
            }

            label.color = HasReadableContrast(backgroundColor, palette.Secondary)
                ? palette.Secondary
                : GetReadableTextColor(backgroundColor);
        }

        private void RestoreBaseColors()
        {
            ResolveReferences();
            if (targetGraphic != null)
            {
                targetGraphic.color = baseGraphicColor;
            }

            if (label != null)
            {
                label.color = baseLabelColor;
            }
        }

        private void ResolveReferences()
        {
            if (targetGraphic == null)
            {
                Selectable selectable = GetComponent<Selectable>();
                targetGraphic = selectable != null ? selectable.targetGraphic : GetComponent<Graphic>();
            }

            if (label == null)
            {
                label = GetComponentInChildren<TMP_Text>(true);
            }

            if (owner == null && dropdown != null)
            {
                owner = dropdown.GetComponentInParent<CreateNewGameManager>();
            }
        }

        private void CaptureBaseColors()
        {
            ResolveReferences();
            baseGraphicColor = targetGraphic != null ? targetGraphic.color : new Color(1f, 1f, 1f, 0f);
            baseLabelColor = label != null ? label.color : Color.black;
        }
    }

    private sealed class KitDropdownKeyboardStepper : MonoBehaviour, ISelectHandler, IPointerClickHandler, ISubmitHandler
    {
        private TMP_Dropdown dropdown;
        private CreateNewGameManager owner;

        public void Configure(TMP_Dropdown sourceDropdown, CreateNewGameManager sourceOwner)
        {
            dropdown = sourceDropdown;
            owner = sourceOwner;
        }

        public void OnSelect(BaseEventData eventData)
        {
            owner?.NotifyKitDropdownFocused(dropdown);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            owner?.NotifyKitDropdownFocused(dropdown);
            StartCoroutine(SelectAfterDropdownCloses());
        }

        public void OnSubmit(BaseEventData eventData)
        {
            owner?.NotifyKitDropdownFocused(dropdown);
            StartCoroutine(SelectAfterDropdownCloses());
        }

        private IEnumerator SelectAfterDropdownCloses()
        {
            yield return null;
            while (IsKitDropdownListOpen(dropdown))
            {
                yield return null;
            }

            if (dropdown != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(dropdown.gameObject);
            }
        }

    }

    private sealed class RegularMatchSelectionSnapshot
    {
        public bool hasValue;
        public string homeTeamName;
        public string awayTeamName;
        public string squadSize;
        public string draft;
        public string gkDraft;
        public string homeKit;
        public string awayKit;
        public string homeGKKit;
        public string awayGKKit;
        public bool includeTabletopia;
        public bool includeNonTabletopia;
        public bool includeInternationals;
        public bool includeTabletopiaGK;
        public bool includeNonTabletopiaGK;
        public bool includeInternationalsGK;
    }
}

[System.Serializable]
public class GameSettings
{
    public string gameMode;
    public int halfDuration;
    public int numberOfHalfs;
    public string tiebreaker;
    public int playerAssistance;
    public string matchType;
    public string squadSize;
    public string draft;
    public string gkDraft;
    public string referee;
    public string weatherConditions;
    public string ballColor;
    // public string playerDeck;
    public bool includeTabletopia;
    public bool includeNonTabletopia;
    public bool includeInternationals;
    public bool includeTabletopiaGK;
    public bool includeNonTabletopiaGK;
    public bool includeInternationalsGK;
    public string homeTeamName;
    public string awayTeamName;
    public string homeTeamControl;
    public string awayTeamControl;
    public string homeKit;
    public string awayKit;
    public string homeGKKit;
    public string awayGKKit;
    public string homeDraftPersona;
    public string awayDraftPersona;
    public string defaultDraftPersona;
    public string homeRoomPersona;
    public string awayRoomPersona;
    public string defaultRoomPersona;
}
