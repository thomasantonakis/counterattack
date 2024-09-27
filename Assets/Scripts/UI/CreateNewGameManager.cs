using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System;
using TMPro;  // Import TextMeshPro namespace

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
    public TMP_Dropdown draftDropdown;
    public TMP_Dropdown weatherDropdown;
    public TMP_Dropdown ballColorDropdown;
    public TMP_InputField homeTeamInputField;
    public TMP_InputField awayTeamInputField;
    public Button createGameButton;

    void Start()
    {
        createGameButton.onClick.AddListener(SaveGameSettingsToJson);

        // Update the text when the slider is moved
        halfDurationSlider.onValueChanged.AddListener(UpdateHalfDurationSliderText);
        playerAssistanceSlider.onValueChanged.AddListener(UpdatePlayerAssistanceSliderText);
        // Set the initial text based on the current slider value
        UpdateHalfDurationSliderText(halfDurationSlider.value);
        UpdatePlayerAssistanceSliderText(playerAssistanceSlider.value);
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

    public void SaveGameSettingsToJson()
    {
        // Create a GameSettings object and populate it from the UI input fields
        GameSettings settings = new GameSettings();
        settings.gameMode = gameModeDropdown.options[gameModeDropdown.value].text;
        settings.numberOfHalfs = int.Parse(numberOfHalvesDropdown.options[numberOfHalvesDropdown.value].text);
        settings.halfDuration = (int)halfDurationSlider.value;
        settings.tiebreaker = tiebreakerDropdown.options[tiebreakerDropdown.value].text;
        settings.playerAssistance = (int)playerAssistanceSlider.value;
        settings.matchType = matchTypeDropdown.options[matchTypeDropdown.value].text;
        settings.draft = draftDropdown.options[draftDropdown.value].text;
        settings.weatherConditions = weatherDropdown.options[weatherDropdown.value].text;
        settings.ballColor = ballColorDropdown.options[ballColorDropdown.value].text;
        settings.homeTeamName = homeTeamInputField.text;
        settings.awayTeamName = awayTeamInputField.text;

        // Convert to JSON
        string json = JsonUtility.ToJson(settings, true);

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
        // SceneManager.LoadScene("RoomScene");
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
    public string draft;
    public string weatherConditions;
    public string ballColor;
    public string homeTeamName;
    public string awayTeamName;
    // public string homeTeamKit;
    // public string awayTeamKit;
}
