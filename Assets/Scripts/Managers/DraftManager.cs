using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.IO;
using TMPro;

public class DraftManager : MonoBehaviour
{
    public List<Player> allPlayers;  // Change the list to Player objects, not dictionaries
    public List<Player> draftPool;   // To hold shuffled players
    public int cardsPerRound = 4;    // Number of player cards shown per draft round
    public GameObject playerCardPrefab;
    public GameObject draftPanel;
    public int squadsize = 2;
    public GameObject playerSlotPrefab;  // Assign this in the Inspector
    public Transform homeTeamPanel;  // The panel where slots will be instantiated
    private int cardsAssigned = 0;

    void Start()
    {
        LoadPlayersFromCSV("outfield_players");  // Load players from the CSV
        ShuffleDraftPool();  // Shuffle the player pool for drafting
        StartDraftRound();   // Start the first draft round
        CreateTeamSlots();
    }

    void LoadPlayersFromCSV(string fileName)
    {
        allPlayers = new List<Player>();
        TextAsset csvFile = Resources.Load<TextAsset>(fileName);  // Load from Resources

        if (csvFile != null)
        {
            string[] lines = csvFile.text.Split('\n');  // Split the content by line

            // First line is the header, so we skip it
            for (int i = 1; i < lines.Length; i++)
            {
                string[] fields = lines[i].Split(',');

                // Log for debugging purposes
                // Debug.Log($"Parsing player: {fields[0]} from {fields[1]} with Pace {fields[2]}");

                // Create a dictionary from the CSV line
                Dictionary<string, string> playerData = new Dictionary<string, string>
                {
                    { "Name", fields[0] },
                    { "Nationality", fields[1] },
                    { "Pace", fields[2] },
                    { "Dribbling", fields[3] },
                    { "Heading", fields[4] },
                    { "HighPass", fields[5] },
                    { "Resilience", fields[6] },
                    { "Shooting", fields[7] },
                    { "Tackling", fields[8] },
                    { "Type", fields[9] }
                };

                // Create a Player object from the dictionary
                Player player = new Player(playerData);
                allPlayers.Add(player);  // Add the player to the list
            }
        }
        else
        {
            Debug.LogError($"CSV file not found in Resources: {fileName}");
        }
    }

    void ShuffleDraftPool()
    {
        draftPool = allPlayers.OrderBy(p => Random.value).Take(squadsize).ToList();  // Shuffle and take 18 players for the draft
    }

    void StartDraftRound()
    {
        // Display 4 cards (players) to draft in the UI
        List<Player> currentRoundPlayers = draftPool.Take(cardsPerRound).ToList();
        DisplayDraftCards(currentRoundPlayers);
    }

    public void DisplayDraftCards(List<Player> playersToShow)
    {
        foreach (Player player in playersToShow)
        {
            GameObject playerCard = Instantiate(playerCardPrefab, draftPanel.transform);  // Assuming playerCardPrefab is assigned in the inspector
            PlayerCard cardScript = playerCard.GetComponent<PlayerCard>();
            cardScript.UpdatePlayerCard(player);
        }
    }

    // Method to be called each time a card is assigned to a slot
    public void CardAssignedToSlot()
    {
        cardsAssigned++;

        // When 4 cards are assigned, deal the next 4 cards
        if (cardsAssigned == 4)
        {
            DealNewCards();  // Function to deal new cards
            cardsAssigned = 0;  // Reset the counter for the next round
        }
    }

    // Method to deal new cards
    private void DealNewCards()
    {
        // Destroy the old cards in the draft panel
        foreach (Transform child in draftPanel.transform)
        {
            Destroy(child.gameObject);  // Remove existing cards
        }

        // Now deal 4 new cards
        StartDraftRound();
    }

    void CreateTeamSlots()
    {
        for (int i = 1; i <= squadsize; i++)
        {
            // Instantiate a new slot
            GameObject newSlot = Instantiate(playerSlotPrefab, homeTeamPanel.transform);
            
            // Check if instantiation was successful
            if (newSlot == null)
            {
                Debug.LogError($"Failed to instantiate player slot #{i}");
                continue;
            }

            // Navigate to the ContentWrapper before accessing the text
            Transform contentWrapper = newSlot.transform.Find("ContentWrapper");
            if (contentWrapper == null)
            {
                Debug.LogError($"ContentWrapper not found in PlayerSlot prefab");
                continue;
            }

            // Set the jersey number in the slot (assuming the text is inside the ContentWrapper)
            contentWrapper.Find("Jersey#").GetComponent<TMP_Text>().text = i.ToString();

            // Debug.Log($"Instantiated player slot #{i} with jersey number {i}");
        }
    }

}