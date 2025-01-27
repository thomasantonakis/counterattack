using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System;
using TMPro;  // Import TextMeshPro namespace
using UnityEngine.SceneManagement;
using Newtonsoft.Json; // Now it will recognize JsonConvert

public class CreateNewGameManager : MonoBehaviour
{
    public TMP_Dropdown gameModeDropdown;
    public Slider halfDurationSlider;
    public TMP_Text halfDurationText;  // Text to show slider value
    public TMP_Dropdown numberOfHalvesDropdown;
    public TMP_Dropdown tiebreakerDropdown;
    public Slider playerAssistanceSlider;
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
    public TMP_InputField homeTeamInputField;
    public TMP_InputField awayTeamInputField;
    public Toggle includeTabletopiaToggle;
    public Toggle includeNonTabletopiaToggle;
    public Toggle includeInternationalsToggle;
    public Toggle includeTabletopiaGKToggle;
    public Toggle includeNonTabletopiaGKToggle;
    public Toggle includeInternationalsGKToggle;
    public Button createGameButton;

    void Start()
    {
        createGameButton.onClick.AddListener(SaveGameSettingsToJson);
        // Half Duration Set minimum and maximum values of the slider
        halfDurationSlider.minValue = 30;  // Minimum half duration
        halfDurationSlider.maxValue = 60;  // Maximum half duration
        halfDurationSlider.wholeNumbers = true;  // Restrict the slider to integer values
        halfDurationSlider.value = 45;  // Default to 45 minutes
        // Player Assistance Set minimum and maximum values of the slider
        playerAssistanceSlider.minValue = 1;  // Easy Mode
        playerAssistanceSlider.maxValue = 3;  // Hard Mode
        playerAssistanceSlider.wholeNumbers = true;
        playerAssistanceSlider.value = 2;  // Default to Medium
        includeTabletopiaToggle.isOn = true;
        includeNonTabletopiaToggle.isOn = false;
        includeInternationalsToggle.isOn = false;
        includeTabletopiaGKToggle.isOn = true;
        includeNonTabletopiaGKToggle.isOn = false;
        includeInternationalsGKToggle.isOn = false;

        // Update the text when the slider is moved
        halfDurationSlider.onValueChanged.AddListener(UpdateHalfDurationSliderText);
        playerAssistanceSlider.onValueChanged.AddListener(UpdatePlayerAssistanceSliderText);
        // Set the initial text based on the current slider value
        UpdateHalfDurationSliderText(halfDurationSlider.value);
        UpdatePlayerAssistanceSliderText(playerAssistanceSlider.value);
        
        // Example: Set default options for squad size at the start of the game
        SetDropDownOptions();
        // Subscribe to field changes, which dynamically adjusts other fields' options
        matchTypeDropdown.onValueChanged.AddListener(delegate { AdjustSquadSizeOptionsBasedOnMatchType(); });
        matchTypeDropdown.onValueChanged.AddListener(delegate { OnMatchTypeChanged(); });
        weatherDropdown.onValueChanged.AddListener(delegate { AdjustBallColorBasedOnWeather(); });
        // Add listeners to each checkbox
        includeTabletopiaToggle.onValueChanged.AddListener(delegate { ValidateCheckboxes(includeTabletopiaToggle); });
        includeNonTabletopiaToggle.onValueChanged.AddListener(delegate { ValidateCheckboxes(includeNonTabletopiaToggle); });
        includeInternationalsToggle.onValueChanged.AddListener(delegate { ValidateCheckboxes(includeInternationalsToggle); });
        includeTabletopiaGKToggle.onValueChanged.AddListener(delegate { ValidateCheckboxesGK(includeTabletopiaGKToggle); });
        includeNonTabletopiaGKToggle.onValueChanged.AddListener(delegate { ValidateCheckboxesGK(includeNonTabletopiaGKToggle); });
        includeInternationalsGKToggle.onValueChanged.AddListener(delegate { ValidateCheckboxesGK(includeInternationalsGKToggle); });
        // Initial setup
        AdjustSquadSizeOptionsBasedOnMatchType();
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
        List<string> gameModeOptions = new List<string> { "Single Player", "Hot Seat", "Multi Player" };  // Define options
        gameModeDropdown.ClearOptions();  // Clear any existing options
        gameModeDropdown.AddOptions(gameModeOptions);  // Add the default options
        List<string> numberOfHalvesOptions = new List<string> { "2", "1" };  // Define options
        numberOfHalvesDropdown.ClearOptions();  // Clear any existing options
        numberOfHalvesDropdown.AddOptions(numberOfHalvesOptions);  // Add the default options
        List<string> tiebreakerOptions = new List<string> { "Extra Time & Penalties", "Penalties", "Extra Time", "None"};  // Define options
        tiebreakerDropdown.ClearOptions();  // Clear any existing options
        tiebreakerDropdown.AddOptions(tiebreakerOptions);  // Add the default options
        List<string> matchTypeOptions = new List<string> { "Regular", "International"};  // Define options
        matchTypeDropdown.ClearOptions();  // Clear any existing options
        matchTypeDropdown.AddOptions(matchTypeOptions);  // Add the default options
        List<string> draftOptions = new List<string> { "Regular", "Free Regular", "Free"};  // Define options
        draftDropdown.ClearOptions();  // Clear any existing options
        draftDropdown.AddOptions(draftOptions);  // Add the default options
        List<string> gkDraftOptions = new List<string> { "Deal", "Free"};  // Define options
        gkDraftDropdown.ClearOptions();  // Clear any existing options
        gkDraftDropdown.AddOptions(gkDraftOptions);  // Add the default options
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
        // Clear current squad size options
        ballColorDropdown.ClearOptions();

        // Change options based on the selected match type
        if (weatherDropdown.value == 2)  // Example: value 0 represents "Snow"
        {
            ballColorDropdown.AddOptions(new List<string> { "Orange" });
        }
        else if (weatherDropdown.value == 1)  // Example: value 1 represents "international"
        {
            ballColorDropdown.AddOptions(new List<string> { "White", "Orange", "Yellow" }); // Fewer options for international matches
        }
        ballColorDropdown.RefreshShownValue();  // Force UI update to reflect changes
    }

    // Update checkboxes and squad size options when match type changes
    void OnMatchTypeChanged()
    {
        AdjustSquadSizeOptionsBasedOnMatchType();  // Update squad size dropdown

        if (matchTypeDropdown.value == 1)  // International
        {
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
        // Create a GameSettings object and populate it from the UI input fields
        GameSettings settings = new GameSettings();
        settings.gameMode = gameModeDropdown.options[gameModeDropdown.value].text;
        settings.halfDuration = (int)halfDurationSlider.value;
        settings.numberOfHalfs = int.Parse(numberOfHalvesDropdown.options[numberOfHalvesDropdown.value].text);
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
        settings.playerAssistance = (int)playerAssistanceSlider.value;
        settings.weatherConditions = weatherDropdown.options[weatherDropdown.value].text;
        settings.ballColor = ballColorDropdown.options[ballColorDropdown.value].text;
        settings.homeTeamName = homeTeamInputField.text;
        settings.awayTeamName = awayTeamInputField.text;
        settings.includeTabletopia = includeTabletopiaToggle.isOn;
        settings.includeNonTabletopia = includeNonTabletopiaToggle.isOn;
        settings.includeInternationals = includeInternationalsToggle.isOn;
        settings.includeTabletopiaGK = includeTabletopiaGKToggle.isOn;
        settings.includeNonTabletopiaGK = includeNonTabletopiaGKToggle.isOn;
        settings.includeInternationalsGK = includeInternationalsGKToggle.isOn;
        settings.homeKit = homeKitDropdown.options[homeKitDropdown.value].text;
        settings.awayKit = awayKitDropdown.options[awayKitDropdown.value].text;

        var gameData = new
        {
            gameSettings = settings  // Grouped under "gameSettings"
        };
        string json = JsonConvert.SerializeObject(gameData, Formatting.Indented);

        // Generate random alphanumeric prefix
        string randomPrefix = GenerateRandomString(16);
        // Get current timestamp
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm");

        // Sanitize team names to avoid invalid characters in filenames
        string homeTeam = SanitizeFileName(homeTeamInputField.text);
        string awayTeam = SanitizeFileName(awayTeamInputField.text);
        string gameMode = SanitizeFileName(gameModeDropdown.options[gameModeDropdown.value].text);

        // Construct dynamic filename
        string fileName = $"{randomPrefix}_{timestamp}__{gameMode}__{homeTeam}__{awayTeam}.json";

        // Path where you want to save the file
        // string path = Path.Combine(Application.persistentDataPath, fileName);
        string path = Path.Combine(ApplicationManager.Instance.GetSaveFolderPath(), fileName);

        // Write the file
        File.WriteAllText(path, json);

        // Log where the file was saved
        Debug.Log($"Game settings saved to {path}");
        // Load the Room scene

        // Check the draft and gkDraft settings to determine the scene to load
        if (settings.draft == "Regular" && settings.gkDraft == "Deal")
        {
            Debug.Log("Loading the Regular Draft Scene...");
            PlayerPrefs.SetString("currentGameSettings", fileName); // Save the file name for the Draft scene to use
            SceneManager.LoadScene("Draft"); // Load the current Draft scene
        }
        else
        {
            Debug.Log("Non-regular draft selected. Loading the Free Draft Scene...");
            PlayerPrefs.SetString("currentGameSettings", fileName); // Save the file name for the future Free Draft scene
            SceneManager.LoadScene("FreeDraft"); // Placeholder for a new scene
        }
    }

    // Helper function to sanitize file names by removing invalid characters
    private string SanitizeFileName(string input)
    {
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
    public string homeKit;
    public string awayKit;
}
