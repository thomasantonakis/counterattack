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
    public TMP_Dropdown playerDeckDropdown;
    public TMP_Dropdown homeKitDropdown;
    public TMP_Dropdown awayKitDropdown;
    public TMP_InputField homeTeamInputField;
    public TMP_InputField awayTeamInputField;
    public Toggle includeInternationalsToggle;
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
        includeInternationalsToggle.isOn = true;

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
        weatherDropdown.onValueChanged.AddListener(delegate { AdjustBallColorBasedOnWeather(); });
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
        List<string> playerDeckOptions = new List<string> { "Base Game", "Tabletopia", "All Extras" };  // Define options
        playerDeckDropdown.ClearOptions();  // Clear any existing options
        playerDeckDropdown.AddOptions(playerDeckOptions);  // Add the default options
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
        settings.referee = refereeDropdown.options[refereeDropdown.value].text;
        settings.playerAssistance = (int)playerAssistanceSlider.value;
        settings.weatherConditions = weatherDropdown.options[weatherDropdown.value].text;
        settings.ballColor = ballColorDropdown.options[ballColorDropdown.value].text;
        settings.homeTeamName = homeTeamInputField.text;
        settings.awayTeamName = awayTeamInputField.text;
        settings.playerDeck = playerDeckDropdown.options[playerDeckDropdown.value].text;
        settings.includeInternationals = includeInternationalsToggle.isOn;
        settings.homeKit = homeKitDropdown.options[homeKitDropdown.value].text;
        settings.awayKit = awayKitDropdown.options[awayKitDropdown.value].text;

        var gameData = new
        {
            gameSettings = settings  // Grouped under "gameSettings"
        };

        string json = JsonConvert.SerializeObject(gameData, Formatting.Indented);

        // // Convert to JSON
        // string json = JsonUtility.ToJson(settings, true);

        // Get current timestamp
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm");

        // Sanitize team names to avoid invalid characters in filenames
        string homeTeam = SanitizeFileName(homeTeamInputField.text);
        string awayTeam = SanitizeFileName(awayTeamInputField.text);
        string gameMode = SanitizeFileName(gameModeDropdown.options[gameModeDropdown.value].text);

        // Construct dynamic filename
        string fileName = $"{timestamp}__{gameMode}__{homeTeam}__{awayTeam}.json";

        // Path where you want to save the file
        string path = Path.Combine(Application.persistentDataPath, fileName);

        // Write the file
        File.WriteAllText(path, json);

        // Log where the file was saved
        Debug.Log($"Game settings saved to {path}");
        // Load the Room scene
        SceneManager.LoadScene("Room");
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
    public string playerDeck;
    public bool includeInternationals;
    public string homeTeamName;
    public string awayTeamName;
    public string homeKit;
    public string awayKit;
}
