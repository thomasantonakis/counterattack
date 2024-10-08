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
    
    // // Spawn positions for the players
    // private Vector3[] homeTeamPositions = new Vector3[]
    // {
    //     new Vector3(-5, 0.2f, 0), new Vector3(-6, 0.2f, 1), new Vector3(-7, 0.2f, -1),
    //     new Vector3(-8, 0.2f, 2), new Vector3(-9, 0.2f, -2), new Vector3(-10, 0.2f, 1),
    //     new Vector3(-11, 0.2f, -1), new Vector3(-12, 0.2f, 0), new Vector3(-13, 0.2f, 1),
    //     new Vector3(-14, 0.2f, -1)
    // };

    // private Vector3[] awayTeamPositions = new Vector3[]
    // {
    //     new Vector3(5, 0.2f, 0), new Vector3(6, 0.2f, 1), new Vector3(7, 0.2f, -1),
    //     new Vector3(8, 0.2f, 2), new Vector3(9, 0.2f, -2), new Vector3(10, 0.2f, 1),
    //     new Vector3(11, 0.2f, -1), new Vector3(12, 0.2f, 0), new Vector3(13, 0.2f, 1),
    //     new Vector3(14, 0.2f, -1)
    // };

    void Start()
    {
        // Start the coroutine to wait for the grid to initialize before creating teams
        StartCoroutine(InitializeTeamsAfterGridIsReady(10, 10));
    }

    private IEnumerator InitializeTeamsAfterGridIsReady(int homeTeamCount, int awayTeamCount)
    {
        // Wait until the HexGrid has finished creating cells
        yield return new WaitUntil(() => hexgrid != null && hexgrid.IsGridInitialized());  // Check if grid is ready

        // Now proceed to instantiate teams
        InstantiateTeams(homeTeamCount, awayTeamCount);
    }

    private void InstantiateTeams(int homeTeamCount, int awayTeamCount)
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
            player.name = $"{teamType}Player{i+2}";
            // Ensure this player token is assigned the correct layer
            player.layer = LayerMask.NameToLayer("Token");
            // Attach PlayerToken component and set the current hex
            PlayerToken token = player.GetComponent<PlayerToken>();
            // Log the hex before assigning it
            // Debug.Log($"Spawning player {player.name} at hex: {spawnHexes[i].name}");
            token.SetCurrentHex(spawnHexes[i]);  // This will dynamically set isAttacker based on the hex status
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
            numberText.fontSize = 3;  // Set font size, tweak as needed
            numberText.alignment = TextAlignmentOptions.Center;  // Center the text on top of the token
            numberText.GetComponent<MeshRenderer>().sortingOrder = 10;  // Ensure the number is rendered on top
        }
    }
}
