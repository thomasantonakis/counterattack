using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;  // Import TextMeshPro namespace


public class PlayerTokenManager : MonoBehaviour
{
    public GameObject redKitPrefab;
    public GameObject blueKitPrefab;
    public HexGrid hexgrid;
    public GameObject textPrefab; // A prefab for TextMeshPro object for jersey numbers (you'll create this prefab)
    
    // Spawn positions for the players
    private Vector3Int[] homeTeamPositions = new Vector3Int[]
    {
        new Vector3Int(0, 0, 0), new Vector3Int(6, 0, 6), new Vector3Int(8, 0, 8),
        new Vector3Int(12, 0, 12), new Vector3Int(4, 0, 4), new Vector3Int(10, 0, 10),
        new Vector3Int(-2, 0, -1), new Vector3Int(-4, 0, -4), new Vector3Int(-6, 0, -6),
        new Vector3Int(-8, 0, -8)
    };

    private Vector3Int[] awayTeamPositions = new Vector3Int[]
    {
        new Vector3Int(1, 0, 0), new Vector3Int(1, 0, -1), new Vector3Int(1, 0, 1),
        new Vector3Int(1, 0, 2), new Vector3Int(3, 0, 3), new Vector3Int(4, 0, 3),
        new Vector3Int(4, 0, 5), new Vector3Int(5, 0, 5), new Vector3Int(6, 0, 5),
        new Vector3Int(7, 0, 6)
    };

    void Start()
    {
        // Start the coroutine to wait for the grid to initialize before creating teams
        StartCoroutine(InitializeTeamsAfterGridIsReady(10, 10));
        var matchManager = FindObjectOfType<MatchManager>();
        if (matchManager == null)
        {
            Debug.LogError("MatchManager not found. Cannot subscribe to game settings load event.");
            return;
        }

        // Subscribe to the OnGameSettingsLoaded event
        matchManager.OnGameSettingsLoaded += InitializeTokens;
    }
    private void OnDestroy()
    {
        var matchManager = FindObjectOfType<MatchManager>();
        if (matchManager != null)
        {
            // Unsubscribe to avoid memory leaks
            matchManager.OnGameSettingsLoaded -= InitializeTokens;
        }
    }
    private void InitializeTokens()
    {
        Debug.Log("Game settings loaded. Initializing tokens...");
        var matchManager = FindObjectOfType<MatchManager>();
        if (matchManager == null || matchManager.gameData == null || matchManager.gameData.rosters == null)
        {
            Debug.LogError("MatchManager or rosters not found. Cannot initialize tokens.");
            return;
        }

        // Access the parsed home and away rosters directly
        var homeRoster = matchManager.gameData.rosters.home;
        var awayRoster = matchManager.gameData.rosters.away;

        Debug.Log("Initializing Home and Away Tokens:");
        foreach (var player in homeRoster)
        {
            Debug.Log($"Home {player.Key}: {player.Value.name}");
            // Call token creation methods as shown earlier
        }

        foreach (var player in awayRoster)
        {
            Debug.Log($"Away {player.Key}: {player.Value.name}");
            // Call token creation methods as shown earlier
        }
    }
    private IEnumerator InitializeTeamsAfterGridIsReady(int homeTeamCount, int awayTeamCount)
    {
        // Wait until the HexGrid has finished creating cells
        yield return new WaitUntil(() => hexgrid != null && hexgrid.IsGridInitialized());  // Check if grid is ready

        // Now proceed to instantiate teams
        InstantiateTeams(homeTeamPositions, awayTeamPositions);
        // InstantiateRandomTeams(homeTeamCount, awayTeamCount);
    }

    private void InstantiateTeams(Vector3Int[] home, Vector3Int[] away)
    {
        // Load GameSettings data from the MatchManager
        string homeKit = MatchManager.Instance.gameData.gameSettings.homeKit;
        string awayKit = MatchManager.Instance.gameData.gameSettings.awayKit;
        List<HexCell> homeTeamHexes = new List<HexCell>();
        List<HexCell> awayTeamHexes = new List<HexCell>();
        foreach (Vector3Int vector in home)
        {
            HexCell hex = hexgrid.GetHexCellAt(vector); 
            hex.isAttackOccupied = true;  // Mark as defender
            hex.HighlightHex("isAttackOccupied");
            homeTeamHexes.Add(hex);
        } 
        foreach (Vector3Int vector in away)
        {
            HexCell hex = hexgrid.GetHexCellAt(vector); 
            hex.isDefenseOccupied = true;  // Mark as defender
            hex.HighlightHex("isDefenseOccupied");
            awayTeamHexes.Add(hex);
        } 
        if (homeKit == "R&W")
        {
            CreateTeam(redKitPrefab, "Home", homeTeamHexes);
        }
        else if (homeKit == "Bluw")
        {
            CreateTeam(blueKitPrefab, "Home", homeTeamHexes);
        }
        // Do the same for Away team
        if (awayKit == "R&W")
        {
            CreateTeam(redKitPrefab, "Away", awayTeamHexes);
        }
        else if (awayKit == "Blue")
        {
            CreateTeam(blueKitPrefab, "Away", awayTeamHexes);
        }
        // After players are instantiated
        MatchManager.Instance.NotifyPlayersInstantiated();  // Notify that players are instantiated
    }
    private void InstantiateRandomTeams(int homeTeamCount, int awayTeamCount)
    {
        // Load GameSettings data from the MatchManager
        string homeKit = MatchManager.Instance.gameData.gameSettings.homeKit;
        string awayKit = MatchManager.Instance.gameData.gameSettings.awayKit;
        // Create an empty list of HexCells
        List<HexCell> potentialSpawns = new List<HexCell>();
        // Gather inbound hexes for potential spawning locations
        foreach (HexCell hex in hexgrid.cells)
        {
            if (!hex.isOutOfBounds && !hex.isAttackOccupied && !hex.isDefenseOccupied)  // Skip occupied or out-of-bounds hexes
            {
                potentialSpawns.Add(hex);
            }
        }
        // Home Team: Manually place one player on the kickoff hex
        HexCell kickoffHex = hexgrid.GetHexCellAt(new Vector3Int(0, 0, 0)); 
        kickoffHex.isAttackOccupied = true;  // Mark as attacker
        kickoffHex.HighlightHex("isAttackOccupied");
        List<HexCell> homeTeamHexes = new List<HexCell> { kickoffHex };
        // Randomly place the remaining 9 Home team players
        for (int i = 0; i < homeTeamCount - 1 && potentialSpawns.Count > 0; i++)
        {
            int randomIndex = Random.Range(0, potentialSpawns.Count);
            HexCell homeHex = potentialSpawns[randomIndex];
            homeHex.isAttackOccupied = true;  // Mark as attacker
            homeHex.HighlightHex("isAttackOccupied");
            homeTeamHexes.Add(homeHex);  // Add to home team hexes
            potentialSpawns.RemoveAt(randomIndex);
        }
        // Away Team: Randomly place 10 players, avoiding attacker hexes
        List<HexCell> awayTeamHexes = new List<HexCell>();
        for (int i = 0; i < awayTeamCount && potentialSpawns.Count > 0; i++)
        {
            int randomIndex = Random.Range(0, potentialSpawns.Count);
            HexCell awayHex = potentialSpawns[randomIndex];
            awayHex.isDefenseOccupied = true;  // Mark as defender
            awayHex.HighlightHex("isDefenseOccupied");
            awayTeamHexes.Add(awayHex);  // Add to away team hexes
            potentialSpawns.RemoveAt(randomIndex);
        }

        // // Load the correct prefab based on the kit
        if (homeKit == "R&W")
        {
            CreateTeam(redKitPrefab, "Home", homeTeamHexes);
        }
        else if (homeKit == "Bluw")
        {
            CreateTeam(blueKitPrefab, "Home", homeTeamHexes);
        }
        // Do the same for Away team
        if (awayKit == "R&W")
        {
            CreateTeam(redKitPrefab, "Away", awayTeamHexes);
        }
        else if (awayKit == "Blue")
        {
            CreateTeam(blueKitPrefab, "Away", awayTeamHexes);
        }
        // After players are instantiated
        MatchManager.Instance.NotifyPlayersInstantiated();  // Notify that players are instantiated
    }
    void CreateTeam(GameObject kitPrefab, string teamType, List<HexCell> spawnHexes)
    {
        // Find or create the "Player Tokens" parent object in the scene
        GameObject parentObject = GameObject.Find("Player Tokens");
        // If the parent object doesn't exist, create it
        if (parentObject == null)
        {
            Debug.Log("Parent object not found, creating a new 'Player Tokens' object.");
            parentObject = new GameObject("Player Tokens");
        }
        if (textPrefab == null)
        {
            Debug.LogError("Text prefab is not assigned! Please assign the TextMeshPro prefab.");
            return;  // Prevent further execution
        }
        // Load settings from Match Manager on the lineups
        var matchManager = FindObjectOfType<MatchManager>();
        if (matchManager == null || matchManager.gameData == null || matchManager.gameData.rosters == null)
        {
            Debug.LogError("MatchManager or rosters not found. Cannot initialize tokens.");
            return;
        }

        // Access the parsed home and away rosters directly
        var homeRoster = matchManager.gameData.rosters.home;
        var awayRoster = matchManager.gameData.rosters.away;
        // Debug.Log("Initializing Home and Away Tokens:");
        // foreach (var player in homeRoster)
        // {
        //     Debug.Log($"Home {player.Key}: {player.Value.name}");
        //     // Call token creation methods as shown earlier
        // }

        for (int i = 0; i < spawnHexes.Count; i++)  // Assuming each hex in spawnHexes corresponds to a player
        {
            if (spawnHexes[i] == null)
            {
                Debug.LogError($"Hex at index {i} is null!");
                continue;
            }

            // Debug.Log($"Spawning player at hex: {spawnHexes[i].name}");
            Vector3 hexCenter = spawnHexes[i].GetHexCenter();
            Vector3 playerPosition = new Vector3(hexCenter.x, 0.2f, hexCenter.z);  // Position snapped to the hex center, y set to -0.2
            GameObject player = Instantiate(kitPrefab, playerPosition, Quaternion.identity, parentObject.transform);
            // player.name = $"{teamType}Player{i+2}";
            // Set GameObject name based on roster and jersey number
            string jerseyNumber = (i + 2).ToString();
            string playerName = teamType == "Home"
                ? homeRoster.ContainsKey(jerseyNumber) ? homeRoster[jerseyNumber].name : "Unknown"
                : awayRoster.ContainsKey(jerseyNumber) ? awayRoster[jerseyNumber].name : "Unknown";

            player.name = $"{jerseyNumber}. {playerName}";
            // Ensure this player token is assigned the correct layer
            player.layer = LayerMask.NameToLayer("Token");
            // Attach PlayerToken component and set the current hex
            PlayerToken token = player.GetComponent<PlayerToken>();
            if (token == null)
            {
                token = player.AddComponent<PlayerToken>();
            }
            MatchManager.RosterPlayer rosterPlayer = teamType == "Home"
                ? homeRoster.ContainsKey(jerseyNumber) ? homeRoster[jerseyNumber] : null
                : awayRoster.ContainsKey(jerseyNumber) ? awayRoster[jerseyNumber] : null;

            if (rosterPlayer == null)
            {
                Debug.LogWarning($"RosterPlayer not found for jersey {jerseyNumber} in {teamType} roster.");
                continue;  // Skip this token if no roster data is found
            }
            token.InitializeAttributesFromRoster(rosterPlayer, int.Parse(jerseyNumber));
            // Log the hex before assigning it
            // Debug.Log($"Spawning player {player.name} at hex: {spawnHexes[i].name}");
            token.SetCurrentHex(spawnHexes[i]);  // This will dynamically set isAttacker based on the hex status
            token.isHomeTeam = teamType == "Home";  // Set isHomeTeam based on team type
            // After assignment, confirm it was assigned
            // Debug.Log($"{player.name} assigned hex: {token.GetCurrentHex()?.name}");
            // Instantiate the TextMeshPro object for the jersey number
            GameObject numberTextObj = Instantiate(textPrefab, playerPosition, Quaternion.identity, player.transform);  // Make the text a child of the player
            if (numberTextObj == null)
            {
                Debug.LogError("Failed to instantiate the TextMeshPro object for jersey numbers.");
                continue;
            }
            // Adjust position of the text slightly above the player token
            numberTextObj.transform.position = new Vector3(playerPosition.x, 0.41f, playerPosition.z);  // Adjust Y position to sit on top
            // Rotate the text to face upwards
            numberTextObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);  // Rotate the text to lay flat, facing upwards

            // Get the TextMeshPro component and assign the jersey number
            TextMeshPro numberText = numberTextObj.GetComponent<TextMeshPro>();
            if (numberText == null)
            {
                Debug.LogError("Failed to get TextMeshPro component from instantiated jersey number prefab.");
                continue;
            }
            numberText.text = (i + 2).ToString();  // Assign jersey numbers starting from 2
            // TODO: add a '.' after 6 and 9.
            numberText.fontSize = 3;  // Set font size, tweak as needed
            numberText.alignment = TextAlignmentOptions.Center;  // Center the text on top of the token
            numberText.GetComponent<MeshRenderer>().sortingOrder = 10;  // Ensure the number is rendered on top
        }
    }
}
