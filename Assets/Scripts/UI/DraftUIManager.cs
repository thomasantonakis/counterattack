using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;
using System.IO;
using System.Collections.Generic;
using TMPro;
using System.Linq;

public class DraftUIManager : MonoBehaviour
{
    public Button startGameButton;  // Reference to the Start Game button
    private DraftManager draftManager;  // Reference to the DraftManager
    public GameObject homeTeamPanel;
    public GameObject awayTeamPanel;
    [Header("Draft State UI")]
    public TMP_Text draftTurnText;
    public TMP_Text draftBatchText;
    public TMP_Text draftMetaText;

    void Start()
    {
        // Find the DraftManager script in the scene
        draftManager = FindObjectOfType<DraftManager>();

        // The match can only start after every outfield slot has been filled by the draft.
        startGameButton.interactable = false;
    }

    public void RefreshDraftStateUI()
    {
        if (draftTurnText == null || draftBatchText == null || draftMetaText == null)
        {
            Debug.LogWarning("Draft state UI references are not assigned in Draft.scene.");
            return;
        }

        if (draftManager == null)
        {
            draftManager = FindObjectOfType<DraftManager>();
        }

        if (draftManager == null)
        {
            return;
        }

        int currentBatch = draftManager.GetCurrentBatchNumber();
        int totalBatches = draftManager.GetTotalBatchCount();

        if (draftManager.IsDraftComplete())
        {
            draftTurnText.text = "DRAFT COMPLETE";
            draftBatchText.text = $"Batch {currentBatch} of {totalBatches}";
            draftMetaText.text = "All cards assigned. Start Game is now available.";
            return;
        }

        string currentTurn = draftManager.GetCurrentTeamTurn();
        if (string.IsNullOrEmpty(currentTurn))
        {
            return;
        }

        // Keep the messaging framed around the current decision:
        // who picks now, where we are in the 4-card cycle, and who started this batch.
        int picksLeftInBatch = draftManager.GetRemainingSelectionsInCurrentBatch();
        string pickLabel = picksLeftInBatch == 1 ? "pick" : "picks";
        string batchStarter = draftManager.GetCurrentBatchStarter();

        draftTurnText.text = $"{currentTurn.ToUpperInvariant()} PICKS NOW";
        draftBatchText.text = $"Batch {currentBatch} of {totalBatches} • {picksLeftInBatch} {pickLabel} left";

        if (currentBatch <= 1)
        {
            draftMetaText.text = $"Coin toss: {draftManager.GetFirstBatchStarter()} starts batch 1.";
        }
        else
        {
            draftMetaText.text = $"{batchStarter} started this batch. Batch starters alternate every 4 picks.";
        }
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
    public void OnStartGameButtonPressed()
    {
        // Ensure DraftManager has loaded the current game settings
        if (draftManager.currentSettings == null)
        {
            Debug.LogError("Game settings not loaded in DraftManager.");
            return;
        }

        // Persist the final drafted lineups back into the same JSON that created this draft.
        var homeRoster = GatherRosterData(homeTeamPanel);
        var awayRoster = GatherRosterData(awayTeamPanel);
        string filePath = ApplicationManager.Instance.GetLastSavedFilePath();

        // Ensure the file path is available
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            Debug.LogError("Failed to locate the game settings file to update.");
            return;
        }

        string json = File.ReadAllText(filePath);
        var jsonData = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

        // Add rosters to JSON
        var rosters = new
        {
            home = homeRoster,
            away = awayRoster
        };

        jsonData["rosters"] = rosters;

        // Write updated JSON back to the file
        string updatedJson = JsonConvert.SerializeObject(jsonData, Formatting.Indented);
        File.WriteAllText(filePath, updatedJson);

        Debug.Log($"Rosters saved to JSON: {filePath}");

        // Proceed to the game scene
        SceneManager.LoadScene("Room");
    }

    private Dictionary<string, Dictionary<string, object>> GatherRosterData(GameObject teamPanel)
    {
        var rosterData = new Dictionary<string, Dictionary<string, object>>();

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

                // Check if the slot is for a goalkeeper
                if (jerseyNumber == "1" || jerseyNumber == "12")
                {
                    // Find the goalkeeper in the allGks list
                    var goalkeeper = draftManager.allGks.FirstOrDefault(gk => gk.Name == playerName);

                    if (goalkeeper != null)
                    {
                        // Add goalkeeper data to the roster
                        rosterData[jerseyNumber] = new Dictionary<string, object>
                        {
                            { "name", playerName },
                            { "aerial", goalkeeper.Aerial },
                            { "saving", goalkeeper.Saving },
                            { "handling", goalkeeper.Handling },
                            { "pace", goalkeeper.Pace },
                            { "dribbling", goalkeeper.Dribbling },
                            { "highPass", goalkeeper.HighPass },
                            { "resilience", goalkeeper.Resilience }
                        };
                    }
                    else
                    {
                        Debug.LogWarning($"Goalkeeper '{playerName}' not found in allGks list.");
                    }
                }
                else
                {
                    // Find the player in the allPlayers list
                    var player = draftManager.allPlayers.FirstOrDefault(p => p.Name == playerName);

                    if (player != null)
                    {
                        // Add player data to the roster
                        rosterData[jerseyNumber] = new Dictionary<string, object>
                        {
                            { "name", playerName },
                            { "pace", player.Pace },
                            { "dribbling", player.Dribbling },
                            { "heading", player.Heading },
                            { "highPass", player.HighPass },
                            { "resilience", player.Resilience },
                            { "shooting", player.Shooting },
                            { "tackling", player.Tackling }
                        };
                    }
                    else
                    {
                        Debug.LogWarning($"Player '{playerName}' not found in allPlayers list.");
                    }
                }
            }
        }

        return rosterData;
    }

    // Method to check if the draft is complete and enable the Start Game button
    public void CheckIfDraftIsComplete()
    {
        // In the regular hot-seat flow, the draft is complete once there are no undealt outfielders left.
        if (draftManager.draftPool.Count == 0)  // If all cards have been dealt
        {
            startGameButton.interactable = true;  // Enable the Start Game button
        }

        RefreshDraftStateUI();
    }
}
