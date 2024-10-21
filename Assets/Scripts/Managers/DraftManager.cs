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
    public List<Goalkeeper> allGks;  // Change the list to Player objects, not dictionaries
    public List<Goalkeeper> selectedGks;  // Change the list to Player objects, not dictionaries
    public GameObject draftPanel;
    public GameObject homeTeamPanel;  // The panel where slots will be instantiated
    public GameObject awayTeamPanel;
    public GameObject homeAveragePanel;
    public GameObject awayAveragePanel;
    public GameObject playerSlotPrefab;  // Assign this in the Inspector
    private readonly int squadSize = 16;
    private int cardsAssignedThisRound = 0;
    private string currentTeamTurn;  // Track which team's turn it is
    private bool isHomeFirstInNextRound = true;  // Track which team starts first in each round


    void Start()
    {
        LoadPlayersFromCSV("outfield_players");  // Load players from the CSV
        CreateDraftPool();  // Create the draft pool
        LoadGKFromCSV("goalkeepers");  // Load players from the CSV
        FilterAndShuffleGoalkeepers();
        CreateTeamSlots(homeTeamPanel, homeAveragePanel);
        CreateTeamSlots(awayTeamPanel, awayAveragePanel);
        AssignGoalkeepersToSlots();  // Assign GKs before dealing player cards
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
    void LoadGKFromCSV(string fileName)
    {
        allGks = new List<Goalkeeper>();
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
                Dictionary<string, string> gkData = new Dictionary<string, string>
                {
                    { "Name", fields[0] },
                    { "Nationality", fields[1] },
                    { "Aerial", fields[2] },
                    { "Dribbling", fields[3] },
                    { "Pace", fields[4] },
                    { "Resilience", fields[5] },
                    { "Saving", fields[6] },
                    { "Handling", fields[7] },
                    { "HighPass", fields[8] },
                    { "Type", fields[9] }
                };

                // Create a Goalkeeper object from the dictionary
                Goalkeeper goalkeeper = new Goalkeeper(gkData);
                allGks.Add(goalkeeper);  // Add the player to the list
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
        selectedDeck = selectedDeck.GetRange(0, (squadSize-2) * 2);  // Limit to squadSize * 2 players - excluding GKs

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

    private void FilterAndShuffleGoalkeepers()
    {
        // Copy allGks into selectedGks (no filter for now)
        selectedGks = new List<Goalkeeper>(allGks);

        // Shuffle the selectedGks list
        ShuffleGKDeck(selectedGks);
    }

    private void ShuffleGKDeck(List<Goalkeeper> deck)
    {
        for (int i = 0; i < deck.Count; i++)
        {
            Goalkeeper temp = deck[i];
            int randomIndex = Random.Range(i, deck.Count);
            deck[i] = deck[randomIndex];
            deck[randomIndex] = temp;
        }
    }

    private void AssignGoalkeepersToSlots()
    {
        // Sort the first two goalkeepers for Home by Saving, Handling, and Pace
        var topTwoHome = selectedGks.Take(2).OrderByDescending(gk => gk.Saving)
                                        .ThenByDescending(gk => gk.Handling)
                                        .ThenByDescending(gk => gk.Pace)
                                        .ToList();
                                        
        // Assign the first one to Home-1 and the second one to Home-12
        AssignGoalkeeperToSlot(topTwoHome[0], "Home-1");
        AssignGoalkeeperToSlot(topTwoHome[1], "Home-12");

        // Remove them from selectedGks
        selectedGks.RemoveRange(0, 2);

        // Sort the next two goalkeepers for Away
        var topTwoAway = selectedGks.Take(2).OrderByDescending(gk => gk.Saving)
                                        .ThenByDescending(gk => gk.Handling)
                                        .ThenByDescending(gk => gk.Pace)
                                        .ToList();
                                        
        // Assign the first one to Away-1 and the second one to Away-12
        AssignGoalkeeperToSlot(topTwoAway[0], "Away-1");
        AssignGoalkeeperToSlot(topTwoAway[1], "Away-12");

        // Remove them from selectedGks
        selectedGks.RemoveRange(0, 2);
    }

    // Helper method to assign goalkeeper to the correct slot
    private void AssignGoalkeeperToSlot(Goalkeeper gk, string slotName)
    {
        GameObject slot = GameObject.Find(slotName);
        if (slot == null)
        {
            Debug.LogError($"Slot {slotName} not found!");
            return;
        }

        PlayerSlotDropHandler slotHandler = slot.GetComponent<PlayerSlotDropHandler>();
        if (slotHandler != null)
        {
            slotHandler.UpdateGoalkeeperSlot(gk);
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
        // Convert the GameObjects to Transforms and update averages
        UpdateTeamAverages(homeTeamPanel.transform, homeAveragePanel.transform);
        UpdateTeamAverages(awayTeamPanel.transform, awayAveragePanel.transform);
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

    void CreateTeamSlots(GameObject rosterPanel, GameObject averagePanel)
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
            // Dynamically adjust labels for Goalkeeper slots (1 and 12)
            if (i == 1 || i == 12)
            {
                // Change labels for goalkeeper stats
                contentWrapper.Find("HeadingInSlot").GetComponent<TMP_Text>().text = "AerialInSlot";
                contentWrapper.Find("ShootingInSlot").GetComponent<TMP_Text>().text = "SavingInSlot";
                contentWrapper.Find("TacklingInSlot").GetComponent<TMP_Text>().text = "HandlingInSlot";
            }

            // Debug.Log($"Instantiated player slot #{i} with jersey number {i}");
        }
        
        CreateAverageSlot(averagePanel, rosterPanel.name.Contains("Home") ? "Home" : "Away");
    }

    void CreateAverageSlot(GameObject averagePanel, string teamType)
    {
        // Create the "Starting XI" slot
        GameObject startingXISlot = Instantiate(playerSlotPrefab, averagePanel.transform);
        startingXISlot.name = $"{teamType}-XI";

        // Navigate to the ContentWrapper and set the appropriate text
        Transform contentWrapperXI = startingXISlot.transform.Find("ContentWrapper");
        if (contentWrapperXI != null)
        {
            contentWrapperXI.Find("Jersey#").GetComponent<TMP_Text>().text = "XI";
            contentWrapperXI.Find("PlayerNameInSlot").GetComponent<TMP_Text>().text = "Starting XI";
        }
        else
        {
            Debug.LogError("ContentWrapper not found in Starting XI slot prefab");
        }

        // Create the "Team Average" slot
        GameObject teamAverageSlot = Instantiate(playerSlotPrefab, averagePanel.transform);
        teamAverageSlot.name = $"{teamType}-TeamAverage";

        Transform contentWrapperAvg = teamAverageSlot.transform.Find("ContentWrapper");
        if (contentWrapperAvg != null)
        {
            contentWrapperAvg.Find("Jersey#").GetComponent<TMP_Text>().text = "";
            contentWrapperAvg.Find("PlayerNameInSlot").GetComponent<TMP_Text>().text = "Team Average";
        }
        else
        {
            Debug.LogError("ContentWrapper not found in Team Average slot prefab");
        }
    }


    public void UpdateTeamAverages(Transform rosterPanel, Transform averagePanel)
    {
        // Debug.Log("Updating Slot with Card");
        // The sums and counts for calculating averages
        int startXICount = 0, teamAverageCount = 0;
        int startXISumPace = 0, teamAverageSumPace = 0;
        int startXISumDribbling = 0, teamAverageSumDribbling = 0;
        int startXISumHeading = 0, teamAverageSumHeading = 0;
        int startXISumHighPass = 0, teamAverageSumHighPass = 0;
        int startXISumResilience = 0, teamAverageSumResilience = 0;
        int startXISumShooting = 0, teamAverageSumShooting = 0;
        int startXISumTackling = 0, teamAverageSumTackling = 0;

        // Loop through the roster panel and calculate averages
        for (int i = 1; i <= squadSize; i++)
        {
            // Debug.Log($"Get from {rosterPanel}, slot number {i}");
            Transform slot = rosterPanel.GetChild(i-1);
            PlayerSlotDropHandler slotHandler = slot.GetComponent<PlayerSlotDropHandler>();
            // Find the ContentWrapper first

            if (slotHandler != null && slotHandler.IsSlotPopulated())
            {
                Transform contentWrapper = slot.Find("ContentWrapper");
                if (contentWrapper == null)
                {
                    Debug.LogError($"ContentWrapper not found in roster slot '{slot.name}'");
                    continue;
                }
                // Debug.Log("Updating Slot with Card");
                // Retrieve the attributes from the slot (Pace, Dribbling, etc.)
                TMP_Text paceText = contentWrapper.Find("PaceInSlot").GetComponent<TMP_Text>();
                TMP_Text dribblingText = contentWrapper.Find("DribblingInSlot").GetComponent<TMP_Text>();
                TMP_Text headingText = contentWrapper.Find("HeadingInSlot").GetComponent<TMP_Text>();
                TMP_Text highPassText = contentWrapper.Find("HighPassInSlot").GetComponent<TMP_Text>();
                TMP_Text resilienceText = contentWrapper.Find("ResilienceInSlot").GetComponent<TMP_Text>();
                TMP_Text shootingText = contentWrapper.Find("ShootingInSlot").GetComponent<TMP_Text>();
                TMP_Text tacklingText = contentWrapper.Find("TacklingInSlot").GetComponent<TMP_Text>();

                int pace, dribbling, heading, highpass, resilience, shooting, tackling;
                // Try to parse each attribute, if parsing fails, set it to 0
                pace = int.TryParse(paceText.text, out pace) ? pace : 0;
                dribbling = int.TryParse(dribblingText.text, out dribbling) ? dribbling : 0;
                heading = int.TryParse(headingText.text, out heading) ? heading : 0;
                highpass = int.TryParse(highPassText.text, out highpass) ? highpass : 0;
                resilience = int.TryParse(resilienceText.text, out resilience) ? resilience : 0;
                shooting = int.TryParse(shootingText.text, out shooting) ? shooting : 0;
                tackling = int.TryParse(tacklingText.text, out tackling) ? tackling : 0;

                // For Starting XI (slots 2 to 11)
                if (i >= 2 && i <= 11)
                {
                    startXISumPace += pace;
                    startXISumDribbling += dribbling;
                    startXISumHeading += heading;
                    startXISumHighPass += highpass;
                    startXISumResilience += resilience;
                    startXISumShooting += shooting;
                    startXISumTackling += tackling;
                    startXICount++;
                }

                // For Team Average (slots 2 to 11 and 13 to 16)
                if ((i >= 2 && i <= 11) || (i >= 13 && i <= 16))
                {
                    teamAverageSumPace += pace;
                    teamAverageSumDribbling += dribbling;
                    teamAverageSumHeading += heading;
                    teamAverageSumHighPass += highpass;
                    teamAverageSumResilience += resilience;
                    teamAverageSumShooting += shooting;
                    teamAverageSumTackling += tackling;
                    teamAverageCount++;
                }
            }
        }

        // Calculate the averages, ensuring no division by zero
        float avgPaceXI = startXICount > 0 ? (float)startXISumPace / startXICount : 0;
        float avgDribblingXI = startXICount > 0 ? (float)startXISumDribbling / startXICount : 0;
        float avgHeadingXI = startXICount > 0 ? (float)startXISumHeading / startXICount : 0;
        float avgHighPassXI = startXICount > 0 ? (float)startXISumHighPass / startXICount : 0;
        float avgResilienceXI = startXICount > 0 ? (float)startXISumResilience / startXICount : 0;
        float avgShootingXI = startXICount > 0 ? (float)startXISumShooting / startXICount : 0;
        float avgTacklingXI = startXICount > 0 ? (float)startXISumTackling / startXICount : 0;

        float avgPaceTeam = teamAverageCount > 0 ? (float)teamAverageSumPace / teamAverageCount : 0;
        float avgDribblingTeam = teamAverageCount > 0 ? (float)teamAverageSumDribbling / teamAverageCount : 0;
        float avgHeadingTeam = teamAverageCount > 0 ? (float)teamAverageSumHeading / teamAverageCount : 0;
        float avgHighPassTeam = teamAverageCount > 0 ? (float)teamAverageSumHighPass / teamAverageCount : 0;
        float avgResilienceTeam = teamAverageCount > 0 ? (float)teamAverageSumResilience / teamAverageCount : 0;
        float avgShootingTeam = teamAverageCount > 0 ? (float)teamAverageSumShooting / teamAverageCount : 0;
        float avgTacklingTeam = teamAverageCount > 0 ? (float)teamAverageSumTackling / teamAverageCount : 0;


        // Update the UI for Starting XI and Team Average slots
        Transform startingXISlot = averagePanel.Find("Away-XI") ?? averagePanel.Find("Home-XI");
        Transform teamAverageSlot = averagePanel.Find("Away-TeamAverage") ?? averagePanel.Find("Home-TeamAverage");

        if (startingXISlot != null)
        {
            Transform contentWrapperXI = startingXISlot.Find("ContentWrapper");

            if (contentWrapperXI == null)
            {
                Debug.LogError("ContentWrapper not found in Team Average slot");
                return;
            }
            // Update text with 1 decimal point and apply color coding
            contentWrapperXI.Find("PaceInSlot").GetComponent<TMP_Text>().text = avgPaceXI.ToString("F1");
            contentWrapperXI.Find("PaceInSlot").GetComponent<TMP_Text>().color = GetAttributeColor(avgPaceXI);

            contentWrapperXI.Find("DribblingInSlot").GetComponent<TMP_Text>().text = avgDribblingXI.ToString("F1");
            contentWrapperXI.Find("DribblingInSlot").GetComponent<TMP_Text>().color = GetAttributeColor(avgDribblingXI);

            contentWrapperXI.Find("HeadingInSlot").GetComponent<TMP_Text>().text = avgHeadingXI.ToString("F1");
            contentWrapperXI.Find("HeadingInSlot").GetComponent<TMP_Text>().color = GetAttributeColor(avgHeadingXI);

            contentWrapperXI.Find("HighPassInSlot").GetComponent<TMP_Text>().text = avgHighPassXI.ToString("F1");
            contentWrapperXI.Find("HighPassInSlot").GetComponent<TMP_Text>().color = GetAttributeColor(avgHighPassXI);

            contentWrapperXI.Find("ResilienceInSlot").GetComponent<TMP_Text>().text = avgResilienceXI.ToString("F1");
            contentWrapperXI.Find("ResilienceInSlot").GetComponent<TMP_Text>().color = GetAttributeColor(avgResilienceXI);

            contentWrapperXI.Find("ShootingInSlot").GetComponent<TMP_Text>().text = avgShootingXI.ToString("F1");
            contentWrapperXI.Find("ShootingInSlot").GetComponent<TMP_Text>().color = GetAttributeColor(avgShootingXI);

            contentWrapperXI.Find("TacklingInSlot").GetComponent<TMP_Text>().text = avgTacklingXI.ToString("F1");
            contentWrapperXI.Find("TacklingInSlot").GetComponent<TMP_Text>().color = GetAttributeColor(avgTacklingXI);
        }
        else
        {
            Debug.LogError($"{rosterPanel.name}-XI not found");
        }

        if (teamAverageSlot != null)
        {
            Transform contentWrapperAvg = teamAverageSlot.Find("ContentWrapper");

            if (contentWrapperAvg == null)
            {
                Debug.LogError("ContentWrapper not found in Team Average slot");
                return;
            }
            // Update text with 1 decimal point and apply color coding
            contentWrapperAvg.Find("PaceInSlot").GetComponent<TMP_Text>().text = avgPaceTeam.ToString("F1");
            contentWrapperAvg.Find("PaceInSlot").GetComponent<TMP_Text>().color = GetAttributeColor(avgPaceTeam);

            contentWrapperAvg.Find("DribblingInSlot").GetComponent<TMP_Text>().text = avgDribblingTeam.ToString("F1");
            contentWrapperAvg.Find("DribblingInSlot").GetComponent<TMP_Text>().color = GetAttributeColor(avgDribblingTeam);

            contentWrapperAvg.Find("HeadingInSlot").GetComponent<TMP_Text>().text = avgHeadingTeam.ToString("F1");
            contentWrapperAvg.Find("HeadingInSlot").GetComponent<TMP_Text>().color = GetAttributeColor(avgHeadingTeam);

            contentWrapperAvg.Find("HighPassInSlot").GetComponent<TMP_Text>().text = avgHighPassTeam.ToString("F1");
            contentWrapperAvg.Find("HighPassInSlot").GetComponent<TMP_Text>().color = GetAttributeColor(avgHighPassTeam);

            contentWrapperAvg.Find("ResilienceInSlot").GetComponent<TMP_Text>().text = avgResilienceTeam.ToString("F1");
            contentWrapperAvg.Find("ResilienceInSlot").GetComponent<TMP_Text>().color = GetAttributeColor(avgResilienceTeam);

            contentWrapperAvg.Find("ShootingInSlot").GetComponent<TMP_Text>().text = avgShootingTeam.ToString("F1");
            contentWrapperAvg.Find("ShootingInSlot").GetComponent<TMP_Text>().color = GetAttributeColor(avgShootingTeam);

            contentWrapperAvg.Find("TacklingInSlot").GetComponent<TMP_Text>().text = avgTacklingTeam.ToString("F1");
            contentWrapperAvg.Find("TacklingInSlot").GetComponent<TMP_Text>().color = GetAttributeColor(avgTacklingTeam);
        }
        else
        {
            Debug.LogError($"{rosterPanel.name}-TeamAverage not found");
        }
    }

    private Color GetAttributeColor(float value)
    {
        if (value >= 5f)
        {
            return new Color(0f, 0.5f, 0f);  // Dark Green
        }
        else if (value >= 3f)
        {
            return new Color(0.8f, 0.4f, 0f);  // Dark Orange
        }
        else
        {
            return new Color(0.5f, 0f, 0f);  // Dark Red
        }
    }

}