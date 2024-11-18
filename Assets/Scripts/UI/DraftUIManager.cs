using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;
using System.IO;
using System.Collections.Generic;
using TMPro;

public class DraftUIManager : MonoBehaviour
{
    public Button startGameButton;  // Reference to the Start Game button
    private DraftManager draftManager;  // Reference to the DraftManager
    public GameObject homeTeamPanel;
    public GameObject awayTeamPanel;

    void Start()
    {
        // Find the DraftManager script in the scene
        draftManager = FindObjectOfType<DraftManager>();

        // Initially disable the Start Game button
        startGameButton.interactable = false;
    }
    // Method to load the previous scene
    public void OnBackButtonPressed()
    {
        // Assuming you want to load a previous scene, change "PreviousSceneName" to the actual scene name
        SceneManager.LoadScene("CreateNewHSGameScene");
    }
    public void OnBackToMainButtonPressed()
    {
        // Assuming you want to load a previous scene, change "PreviousSceneName" to the actual scene name
        SceneManager.LoadScene("MainMenu");
    }

    // Method to load the game room scene
    // public void OnStartGameButtonPressed()
    // {
    //     // Assuming "GameRoomScene" is the name of the next scene
    //     SceneManager.LoadScene("Room");
    // }

    public void OnStartGameButtonPressed()
    {
        // Ensure DraftManager has loaded the current game settings
        if (draftManager.currentSettings == null)
        {
            Debug.LogError("Game settings not loaded in DraftManager.");
            return;
        }

        // Gather rosters
        var homeRoster = GatherRosterData(homeTeamPanel);
        var awayRoster = GatherRosterData(awayTeamPanel);

        // Ensure the file path is available
        if (string.IsNullOrEmpty(draftManager.mostRecentFilePath) || !File.Exists(draftManager.mostRecentFilePath))
        {
            Debug.LogError("Failed to locate the game settings file to update.");
            return;
        }

        string filePath = draftManager.mostRecentFilePath;
        string json = File.ReadAllText(filePath);
        var jsonData = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

        // Add rosters to the JSON data
        var rosters = new Dictionary<string, Dictionary<string, string>>
        {
            { "home", homeRoster },
            { "away", awayRoster }
        };

        jsonData["rosters"] = rosters;

        // Write updated JSON back to the file
        string updatedJson = JsonConvert.SerializeObject(jsonData, Formatting.Indented);
        File.WriteAllText(filePath, updatedJson);

        Debug.Log($"Rosters saved to JSON: {filePath}");

        // Proceed to the game scene
        // SceneManager.LoadScene("Room");
    }

    private Dictionary<string, string> GatherRosterData(GameObject teamPanel)
    {
        var rosterData = new Dictionary<string, string>();

        foreach (Transform slot in teamPanel.transform)
        {
            var contentWrapper = slot.Find("ContentWrapper");
            if (contentWrapper == null)
            {
                Debug.LogWarning($"ContentWrapper not found for slot {slot.name}");
                continue;
            }

            // Retrieve jersey number and player name
            var jerseyNumberText = contentWrapper.Find("Jersey#").GetComponent<TMP_Text>();
            var playerNameText = contentWrapper.Find("PlayerNameInSlot").GetComponent<TMP_Text>();

            if (jerseyNumberText != null && playerNameText != null && !string.IsNullOrEmpty(playerNameText.text))
            {
                string jerseyNumber = jerseyNumberText.text.Trim();
                string playerName = playerNameText.text.Trim();

                // Add to roster data
                rosterData[jerseyNumber] = playerName;
            }
        }

        return rosterData;
    }

    // Method to check if the draft is complete and enable the Start Game button
    public void CheckIfDraftIsComplete()
    {
        if (draftManager.draftPool.Count == 0)  // If all cards have been dealt
        {
            startGameButton.interactable = true;  // Enable the Start Game button
        }
    }
}
