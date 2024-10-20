using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Linq;
using System.IO;
using TMPro;

public class DraftManager : MonoBehaviour
{
    public List<Player> allPlayers;  // Change the list to Player objects, not dictionaries
    public List<Player> selectedDeck;   // To hold shuffled players
    public List<Player> draftPool;   // To hold shuffled players
    public GameObject playerCardPrefab;
    public GameObject draftPanel;
    public GameObject homeTeamPanel;  // The panel where slots will be instantiated
    public GameObject awayTeamPanel;
    public GameObject playerSlotPrefab;  // Assign this in the Inspector
    private readonly int squadSize = 16;
    private int cardsAssignedThisRound = 0;
    private string currentTeamTurn;  // Track which team's turn it is
    private bool isHomeFirstInNextRound = true;  // Track which team starts first in each round


    void Start()
    {
        LoadPlayersFromCSV("outfield_players");  // Load players from the CSV
        CreateDraftPool();  // Create the draft pool
        CreateTeamSlots(homeTeamPanel);
        CreateTeamSlots(awayTeamPanel);
        PerformCoinFlip();
        DealNewDraftCards();  // Start the first draft round
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

    void CreateDraftPool()
    {
        // Initialize selectedDeck and draftPool
        selectedDeck = new List<Player>(allPlayers);

        // Shuffle the selectedDeck and limit it to squadSize * 2 cards
        ShuffleDeck(selectedDeck);
        selectedDeck = selectedDeck.GetRange(0, squadSize * 2);  // Limit to squadSize * 2 players

        // Set the draftPool to hold the entire selectedDeck initially
        draftPool = new List<Player>(selectedDeck);
    }
    void ShuffleDeck(List<Player> deck)
    {
        for (int i = 0; i < deck.Count; i++)
        {
            Player temp = deck[i];
            int randomIndex = Random.Range(i, deck.Count);
            deck[i] = deck[randomIndex];
            deck[randomIndex] = temp;
        }
    }

    // Simulate the coin flip
    private void PerformCoinFlip()
    {
        int coinFlip = Random.Range(0, 2);  // 0 = Tails (Away), 1 = Heads (Home)

        if (coinFlip == 1)
        {
            Debug.Log("Coin flip result: Heads, Home team picks first.");
            currentTeamTurn = "Home";
        }
        else
        {
            Debug.Log("Coin flip result: Tails, Away team picks first.");
            currentTeamTurn = "Away";
        }
        // Initialize first-round team based on the coin flip result
        isHomeFirstInNextRound = currentTeamTurn == "Home";
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
    public void CardAssignedToSlot(PlayerCard card)
    {
        // Remove the drafted player from the draft pool
        draftPool.Remove(card.assignedPlayer);
        // Increment cards assigned in this round
        cardsAssignedThisRound++;
        // Alternate the current team turn after each card assignment
        currentTeamTurn = currentTeamTurn == "Home" ? "Away" : "Home";

        // Alternate between teams after each card is assigned
        if (cardsAssignedThisRound >= 4 && draftPool.Count()>0)
        {
            // When 4 cards have been assigned, deal new ones
            DealNewDraftCards();
            cardsAssignedThisRound = 0;  // Reset for the next round
            isHomeFirstInNextRound = !isHomeFirstInNextRound;  // Alternate which team starts
            // Set current team for the next round's first pick
            currentTeamTurn = isHomeFirstInNextRound ? "Home" : "Away";
            Debug.Log($"New round started. {currentTeamTurn} picks first.");
        }
        else if (draftPool.Count == 0)
        {
            Debug.Log("No more cards to deal. Draft pool is empty.");
        }
    }

    public string GetCurrentTeamTurn()
    {
        Debug.Log($"Now it's {currentTeamTurn}'s turn.");
        return currentTeamTurn;
    }

    // Validate the target panel based on the current team's turn
    public bool IsValidTeamPanel(string rosterName)
    {
        // Allow only the current team's roster as a valid drop target
        bool panelWhereACardWasDropped = (currentTeamTurn == "Home" && rosterName == "HomeRoster") ||
               (currentTeamTurn == "Away" && rosterName == "AwayRoster");
        Debug.Log($"Dropped Card in {rosterName}, isValidTeamPanel: {panelWhereACardWasDropped}.");
        return panelWhereACardWasDropped;
    }

    void DealNewDraftCards()
    {
        // Debug.Log($"Starting round. Cards in draft pool: {draftPool.Count}");
        // Clear current draft cards from the panel
        foreach (Transform child in draftPanel.transform)
        {
            Destroy(child.gameObject);
        }
        // If there are no more cards to deal, do nothing
        if (draftPool.Count == 0)
        {
            Debug.Log("No more cards to deal. Draft pool is empty. Should not appear.");
            return;
        }
        
        // Make sure we still have enough cards in the draft pool
        int cardsToDeal = Mathf.Min(4, draftPool.Count);
        // Debug.Log($"Dealing {cardsToDeal} cards.");

        // Deal new cards
        for (int i = 0; i < cardsToDeal; i++)
        {
            Player nextPlayer = draftPool[i];
            GameObject newCard = Instantiate(playerCardPrefab, draftPanel.transform);
            PlayerCard playerCard = newCard.GetComponent<PlayerCard>();
            playerCard.UpdatePlayerCard(nextPlayer);
            // Debug.Log($"Dealt card for player: {nextPlayer.Name}");
        }

        // Remove the dealt cards from the draft pool
        draftPool.RemoveRange(0, cardsToDeal);
        // Debug.Log($"After round. Cards remaining in draft pool: {draftPool.Count}");

        // Reset the round
        cardsAssignedThisRound = 0;
    }

    public PlayerSlotDropHandler FindNextAvailableSlot(string rosterPanelName)
{
    // Get the roster panel (HomeRoster or AwayRoster) by name
    GameObject rosterPanel = GameObject.Find(rosterPanelName);

    if (rosterPanel == null)
    {
        Debug.LogError($"Roster panel '{rosterPanelName}' not found!");
        return null;
    }

    // Iterate through the child slots to find the next available slot
    foreach (Transform child in rosterPanel.transform)
    {
        PlayerSlotDropHandler slot = child.GetComponent<PlayerSlotDropHandler>();
        if (slot != null && !slot.IsSlotPopulated())  // Check if slot is not populated
        {
            return slot;  // Return the first available slot
        }
    }

    Debug.LogWarning($"No available slots found in {rosterPanelName}.");
    return null;  // No available slots found
}

    void CreateTeamSlots(GameObject rosterPanel)
    {
        for (int i = 1; i <= squadSize; i++)
        {
            // Instantiate a new slot
            GameObject newSlot = Instantiate(playerSlotPrefab, rosterPanel.transform);
            string rosterType = rosterPanel.name.Contains("Home") ? "Home" : "Away";
            newSlot.name = $"{rosterType}-{i}";  // This will name it "Home-1", "Away-1", etc.
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