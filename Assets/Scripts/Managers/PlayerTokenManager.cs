using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;  // Import TextMeshPro namespace

public class PlayerTokenManager : MonoBehaviour
{
    [Header("Dependencies")]
    public GameObject redKitPrefab;
    public GameObject blueKitPrefab;
    public HexGrid hexgrid;
    public GameObject textPrefab; // A prefab for TextMeshPro object for jersey numbers (you'll create this prefab)
    public List<PlayerToken> allTokens = new List<PlayerToken>();
    public List<PlayerToken> benchTokens = new List<PlayerToken>();
    // Spawn positions for the players
    private Vector3Int[] homeTeamPositions = new Vector3Int[]
    {
        new Vector3Int(-16, 0, 0), // 1
        new Vector3Int(0, 0, 0),
        new Vector3Int(6, 0, 6),
        new Vector3Int(8, 0, 8),
        new Vector3Int(12, 0, 12),
        new Vector3Int(4, 0, 4),
        new Vector3Int(10, 0, 10),
        new Vector3Int(10, 0, 0),
        new Vector3Int(-4, 0, -4),
        new Vector3Int(-6, 0, -6),
        new Vector3Int(-8, 0, -8)
    };

    private Vector3Int[] awayTeamPositions = new Vector3Int[]
    {
        new Vector3Int(16, 0, 0), // 1
        new Vector3Int(-12, 0, 0), // 2
        new Vector3Int(1, 0, -10),
        new Vector3Int(1, 0, 10),
        new Vector3Int(1, 0, 2),
        new Vector3Int(3, 0, 3),
        new Vector3Int(4, 0, 3),
        new Vector3Int(4, 0, 5),
        new Vector3Int(5, 0, 5),
        new Vector3Int(14, 0, 0), // 10
        new Vector3Int(18, 0, 0), // 11
    };

    void Start()
    {
        // Start the coroutine to wait for the grid to initialize before creating teams
        StartCoroutine(InitializeTeamsAfterGridIsReady(10, 10));
        var matchManager = FindAnyObjectByType<MatchManager>();
        if (matchManager == null)
        {
            Debug.LogError("MatchManager not found. Cannot subscribe to game settings load event.");
            return;
        }

        // Subscribe to the OnGameSettingsLoaded event
        // matchManager.OnGameSettingsLoaded += InitializeTokens;
    }
    // private void OnDestroy()
    // {
    //     var matchManager = FindAnyObjectByType<MatchManager>();
    //     if (matchManager != null)
    //     {
    //         // Unsubscribe to avoid memory leaks
    //         matchManager.OnGameSettingsLoaded -= InitializeTokens;
    //     }
    // }
    // private void InitializeTokens()
    // {
    //     Debug.Log("Game settings loaded. Initializing tokens...");
    //     var matchManager = FindAnyObjectByType<MatchManager>();
    //     if (matchManager == null || matchManager.gameData == null || matchManager.gameData.rosters == null)
    //     {
    //         Debug.LogError("MatchManager or rosters not found. Cannot initialize tokens.");
    //         return;
    //     }

    //     // Access the parsed home and away rosters directly
    //     var homeRoster = matchManager.gameData.rosters.home;
    //     var awayRoster = matchManager.gameData.rosters.away;

    //     // Debug.Log("Initializing Home and Away Tokens:");
    //     // foreach (var player in homeRoster)
    //     // {
    //     //     Debug.Log($"Home {player.Key}: {player.Value.name}");
    //     //     // Call token creation methods as shown earlier
    //     // }

    //     // foreach (var player in awayRoster)
    //     // {
    //     //     Debug.Log($"Away {player.Key}: {player.Value.name}");
    //     //     // Call token creation methods as shown earlier
    //     // }
    // }
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
        string homeGkKit = MatchManager.Instance.gameData.gameSettings.homeGKKit;
        string awayGkKit = MatchManager.Instance.gameData.gameSettings.awayGKKit;
        TokenStyleDefinition homeTokenStyle = TokenKitCatalog.ResolveStyle(homeKit);
        TokenStyleDefinition awayTokenStyle = TokenKitCatalog.ResolveStyle(awayKit);
        TokenStyleDefinition homeGkTokenStyle = TokenKitCatalog.ResolveStyle(string.IsNullOrWhiteSpace(homeGkKit) ? homeKit : homeGkKit);
        TokenStyleDefinition awayGkTokenStyle = TokenKitCatalog.ResolveStyle(string.IsNullOrWhiteSpace(awayGkKit) ? awayKit : awayGkKit);
        GameObject tokenBasePrefab = GetTokenBasePrefab();
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
        MatchManager.Instance?.BeginGameplayEventLoggingSuppression();
        try
        {
            CreateTeam(tokenBasePrefab, "Home", homeTeamHexes, homeTokenStyle, homeGkTokenStyle);
            CreateTeam(tokenBasePrefab, "Away", awayTeamHexes, awayTokenStyle, awayGkTokenStyle);
        }
        finally
        {
            MatchManager.Instance?.EndGameplayEventLoggingSuppression();
        }

        // After players are instantiated
        MatchManager.Instance.NotifyPlayersInstantiated();  // Notify that players are instantiated
    }
    private void InstantiateRandomTeams(int homeTeamCount, int awayTeamCount)
    {
        // Load GameSettings data from the MatchManager
        string homeKit = MatchManager.Instance.gameData.gameSettings.homeKit;
        string awayKit = MatchManager.Instance.gameData.gameSettings.awayKit;
        string homeGkKit = MatchManager.Instance.gameData.gameSettings.homeGKKit;
        string awayGkKit = MatchManager.Instance.gameData.gameSettings.awayGKKit;
        TokenStyleDefinition homeTokenStyle = TokenKitCatalog.ResolveStyle(homeKit);
        TokenStyleDefinition awayTokenStyle = TokenKitCatalog.ResolveStyle(awayKit);
        TokenStyleDefinition homeGkTokenStyle = TokenKitCatalog.ResolveStyle(string.IsNullOrWhiteSpace(homeGkKit) ? homeKit : homeGkKit);
        TokenStyleDefinition awayGkTokenStyle = TokenKitCatalog.ResolveStyle(string.IsNullOrWhiteSpace(awayGkKit) ? awayKit : awayGkKit);
        GameObject tokenBasePrefab = GetTokenBasePrefab();
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
        MatchManager.Instance?.BeginGameplayEventLoggingSuppression();
        try
        {
            CreateTeam(tokenBasePrefab, "Home", homeTeamHexes, homeTokenStyle, homeGkTokenStyle);
            CreateTeam(tokenBasePrefab, "Away", awayTeamHexes, awayTokenStyle, awayGkTokenStyle);
        }
        finally
        {
            MatchManager.Instance?.EndGameplayEventLoggingSuppression();
        }

        // After players are instantiated
        MatchManager.Instance.NotifyPlayersInstantiated();  // Notify that players are instantiated
    }
    void CreateTeam(GameObject kitPrefab, string teamType, List<HexCell> spawnHexes, TokenStyleDefinition tokenStyle, TokenStyleDefinition goalkeeperTokenStyle)
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
        var matchManager = FindAnyObjectByType<MatchManager>();
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

            Vector3 hexCenter = spawnHexes[i].GetHexCenter();
            Vector3 playerPosition = new Vector3(hexCenter.x, 0.2f, hexCenter.z);
            string jerseyNumber = (i + 1).ToString();
            MatchManager.RosterPlayer rosterPlayer = teamType == "Home"
                ? homeRoster.ContainsKey(jerseyNumber) ? homeRoster[jerseyNumber] : null
                : awayRoster.ContainsKey(jerseyNumber) ? awayRoster[jerseyNumber] : null;

            if (rosterPlayer == null)
            {
                Debug.LogWarning($"RosterPlayer not found for jersey {jerseyNumber} in {teamType} roster.");
                continue;  // Skip this token if no roster data is found
            }

            TokenStyleDefinition resolvedTokenStyle = IsGoalkeeperRosterPlayer(rosterPlayer, int.Parse(jerseyNumber))
                ? goalkeeperTokenStyle
                : tokenStyle;
            PlayerToken token = CreateTokenObject(
                kitPrefab,
                parentObject.transform,
                teamType,
                rosterPlayer,
                int.Parse(jerseyNumber),
                playerPosition,
                resolvedTokenStyle);
            token.SetCurrentHex(spawnHexes[i]);  // This will dynamically set isAttacker based on the hex status
            token.MarkAsStarter();
            allTokens.Add(token);
        }

        Dictionary<string, MatchManager.RosterPlayer> teamRoster = teamType == "Home" ? homeRoster : awayRoster;
        CreateBenchTokens(kitPrefab, parentObject.transform, teamType, teamRoster, spawnHexes.Count, tokenStyle, goalkeeperTokenStyle);
    }

    private void CreateBenchTokens(
        GameObject kitPrefab,
        Transform parentTransform,
        string teamType,
        Dictionary<string, MatchManager.RosterPlayer> roster,
        int starterCount,
        TokenStyleDefinition tokenStyle,
        TokenStyleDefinition goalkeeperTokenStyle)
    {
        bool isHomeTeam = teamType == "Home";
        int squadSize = GetConfiguredSquadSize(roster);
        int benchIndex = 0;
        foreach (var entry in roster
            .Select(pair => new
            {
                JerseyText = pair.Key,
                Player = pair.Value,
                ParsedJersey = int.TryParse(pair.Key, out int parsedJersey) ? parsedJersey : int.MaxValue
            })
            .Where(entry => entry.ParsedJersey > starterCount
                && entry.ParsedJersey <= squadSize
                && entry.ParsedJersey < int.MaxValue)
            .OrderBy(entry => entry.ParsedJersey))
        {
            Vector3 benchPosition = GetBenchTokenPosition(isHomeTeam, benchIndex);
            TokenStyleDefinition resolvedTokenStyle = IsGoalkeeperRosterPlayer(entry.Player, entry.ParsedJersey)
                ? goalkeeperTokenStyle
                : tokenStyle;
            PlayerToken token = CreateTokenObject(
                kitPrefab,
                parentTransform,
                teamType,
                entry.Player,
                entry.ParsedJersey,
                benchPosition,
                resolvedTokenStyle);
            token.isAttacker = false;
            token.MarkAsBench();
            benchTokens.Add(token);
            benchIndex++;
        }
    }

    private int GetConfiguredSquadSize(Dictionary<string, MatchManager.RosterPlayer> roster)
    {
        int configuredSquadSize = MatchManager.Instance?.gameData?.gameSettings?.squadSize ?? 0;
        if (configuredSquadSize > 0)
        {
            return configuredSquadSize;
        }

        return roster
            .Select(pair => int.TryParse(pair.Key, out int parsedJersey) ? parsedJersey : 0)
            .DefaultIfEmpty(11)
            .Max();
    }

    private static bool IsGoalkeeperRosterPlayer(MatchManager.RosterPlayer rosterPlayer, int jerseyNumber)
    {
        return jerseyNumber == 1
            || jerseyNumber == 12
            || rosterPlayer.aerial > 0
            || rosterPlayer.saving > 0
            || rosterPlayer.handling > 0;
    }

    private Vector3 GetBenchTokenPosition(bool isHomeTeam, int benchIndex)
    {
        float x = isHomeTeam ? -4f - benchIndex : 4f + benchIndex;
        return new Vector3(x, 0.2f, -14f);
    }

    public Vector3 GetBenchTokenPositionForRestore(bool isHomeTeam, int benchIndex)
    {
        return GetBenchTokenPosition(isHomeTeam, benchIndex);
    }

    private PlayerToken CreateTokenObject(
        GameObject kitPrefab,
        Transform parentTransform,
        string teamType,
        MatchManager.RosterPlayer rosterPlayer,
        int jerseyNumber,
        Vector3 playerPosition,
        TokenStyleDefinition tokenStyle)
    {
        GameObject player = Instantiate(kitPrefab, playerPosition, Quaternion.identity, parentTransform);
        player.name = $"{jerseyNumber}. {rosterPlayer.name}";
        player.layer = LayerMask.NameToLayer("Token");

        PlayerToken token = player.GetComponent<PlayerToken>();
        if (token == null)
        {
            token = player.AddComponent<PlayerToken>();
        }

        token.InitializeAttributesFromRoster(rosterPlayer, jerseyNumber);
        token.isHomeTeam = teamType == "Home";

        PlayerTokenVisuals visuals = player.GetComponent<PlayerTokenVisuals>();
        if (visuals == null)
        {
            visuals = player.AddComponent<PlayerTokenVisuals>();
        }
        visuals.ApplyStyle(tokenStyle);
        AddNumberText(player, playerPosition, jerseyNumber, visuals, tokenStyle);
        return token;
    }

    private void AddNumberText(
        GameObject player,
        Vector3 playerPosition,
        int jerseyNumber,
        PlayerTokenVisuals visuals,
        TokenStyleDefinition tokenStyle)
    {
        GameObject numberTextObj = Instantiate(textPrefab, playerPosition, Quaternion.identity, player.transform);
        if (numberTextObj == null)
        {
            Debug.LogError("Failed to instantiate the TextMeshPro object for jersey numbers.");
            return;
        }

        numberTextObj.transform.localPosition = new Vector3(0f, 1.06f, 0f);
        numberTextObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        TextMeshPro numberText = numberTextObj.GetComponent<TextMeshPro>();
        if (numberText == null)
        {
            Debug.LogError("Failed to get TextMeshPro component from instantiated jersey number prefab.");
            return;
        }

        numberText.text = (jerseyNumber == 6 || jerseyNumber == 9) ? $"<u>{jerseyNumber}</u>" : jerseyNumber.ToString();
        numberText.fontSize = 3;
        numberText.alignment = TextAlignmentOptions.Center;
        numberText.GetComponent<MeshRenderer>().sortingOrder = 10;
        visuals.ApplyNumberStyle(numberText, tokenStyle);
    }

    public List<PlayerToken> GetPlayingTokens(bool isHomeTeam)
    {
        return allTokens
            .Where(token => token != null && token.isPlaying && token.isHomeTeam == isHomeTeam)
            .ToList();
    }

    public List<PlayerToken> GetAvailableBenchTokens(bool isHomeTeam)
    {
        return benchTokens
            .Where(token => token != null
                && token.gameObject.activeSelf
                && !token.isPlaying
                && !token.wasSubbedOff
                && token.isHomeTeam == isHomeTeam)
            .ToList();
    }

    public void MoveBenchTokenToActive(PlayerToken token)
    {
        if (token == null)
        {
            return;
        }

        benchTokens.Remove(token);
        if (!allTokens.Contains(token))
        {
            allTokens.Add(token);
        }
        token.MarkSubbedOn();
    }

    public void RemoveActiveToken(PlayerToken token)
    {
        if (token == null)
        {
            return;
        }

        allTokens.Remove(token);
        benchTokens.Remove(token);
        token.MarkSubbedOff();
    }

    public void MoveActiveTokenToBenchSlot(PlayerToken token, Vector3 benchPosition)
    {
        if (token == null)
        {
            return;
        }

        allTokens.Remove(token);
        if (!benchTokens.Contains(token))
        {
            benchTokens.Add(token);
        }

        token.isAttacker = false;
        token.MarkSubbedOffToBench(benchPosition);
    }

    private GameObject GetTokenBasePrefab()
    {
        if (blueKitPrefab != null)
        {
            return blueKitPrefab;
        }

        if (redKitPrefab != null)
        {
            return redKitPrefab;
        }

        Debug.LogError("No token base prefab is assigned on PlayerTokenManager.");
        return null;
    }

}
