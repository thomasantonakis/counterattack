using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerTokenManager : MonoBehaviour
{
    public GameObject redKitPrefab;
    public GameObject blueKitPrefab;
    public HexGrid hexgrid;
    
    
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
    }
    void CreateTeam(GameObject kitPrefab, string teamType, List<HexCell> spawnHexes)
    {
        for (int i = 0; i < spawnHexes.Count; i++)  // Assuming each hex in spawnHexes corresponds to a player
        {
            Vector3 hexCenter = spawnHexes[i].GetHexCenter();
            Vector3 playerPosition = new Vector3(hexCenter.x, 0.2f, hexCenter.z);  // Position snapped to the hex center, y set to -0.2
            GameObject player = Instantiate(kitPrefab, playerPosition, Quaternion.identity);
            player.name = $"{teamType}Player{i+2}";
        }
    }

    // void CreateTeam(GameObject kitPrefab, string teamType, Vector3[] positions)
    // {
    //     for (int i = 0; i < 10; i++)  // Assuming 10 players per team
    //     {
    //         GameObject player = Instantiate(kitPrefab, positions[i], Quaternion.identity);
    //         // Set player positions, name them, and assign them to the team
    //         player.name = $"{teamType}Player{i+1}";
    //     }
    // }
}
