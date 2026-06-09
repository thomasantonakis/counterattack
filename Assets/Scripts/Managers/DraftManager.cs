using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Linq;
using System.IO;
using TMPro;
using Newtonsoft.Json; // Now it will recognize JsonConvert
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DraftManager : MonoBehaviour
{
    private const string ProtectedRoomFixtureSaveFileName = "gv10-dHYf-vRVz-oLwz_2024-11-26_00-28__Single Player__Inverness Caledonian Thistle__Aurora F.C..json";
    private const string FreeDraftSceneName = "FreeDraft";
    private const string MatchTypeInternational = "International";
    private const string DraftInternational = "International";
    private const string WorldCupPlayerType = "World Cup";
    private static readonly string[] FreeDraftFilterOrder =
    {
        "name",
        "nationality",
        "pace",
        "dribbling",
        "headingAerial",
        "highPass",
        "resilience",
        "shootingSaving",
        "tacklingHandling",
        "type"
    };

    [Header("Dependencies")]
    public GameSettings currentSettings; // Class-level variable
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
    [Header("Runtime Items")]
    private int squadSize;
    private bool useTableTopia = true; 
    private bool useNonTableTopia = false; 
    private bool useInternationals = false; 
    private bool useTableTopiaGK = true; 
    private bool useNonTableTopiaGK = false; 
    private bool useInternationalsGK = false; 
    private int cardsAssignedThisRound = 0;
    private int currentBatchNumber = 0;
    private int totalBatchCount = 0;
    private int currentBatchSize = 0;
    private string currentTeamTurn;  // Track which team's turn it is
    private string firstBatchStarter;
    private string currentBatchStarter;
    private bool isHomeFirstInNextRound = true;  // Track which team starts first in each round
    private bool isFreeDraftScene;
    private FreeDraftPhase freeDraftPhase = FreeDraftPhase.Setup;
    private Transform freeDraftContent;
    private Transform freeDraftPreviewRow;
    private TMP_Text freeDraftTitleText;
    private DraftUIManager draftUIManager;
    private readonly Dictionary<string, string> freeDraftFilters = new Dictionary<string, string>();
    private readonly Dictionary<string, FreeDraftTableFilterField> freeDraftFilterFields = new Dictionary<string, FreeDraftTableFilterField>();
    private string freeDraftSortKey;
    private bool freeDraftSortAscending;
    private bool freeDraftHasUserSort;
    public TMP_Text refereeText; // Reference to the TMP_Text for the referee
    public TMP_Text homeTeamName; // Reference to the TMP_Text for the Home Team
    public TMP_Text awayTeamName; // Reference to the TMP_Text for the Away Team

    private enum FreeDraftPhase
    {
        Setup,
        Goalkeepers,
        Outfielders,
        Complete
    }

    void Start()
    {
        isFreeDraftScene = SceneManager.GetActiveScene().name == FreeDraftSceneName;
        if (isFreeDraftScene)
        {
            StartFreeDraftFlow();
            return;
        }

        // Draft scene flow:
        // 1. Load the game-settings JSON produced by CreateNewHSGameScene.
        // 2. Filter/shuffle the outfielder and goalkeeper pools from the setup toggles.
        // 3. Create roster slots for both teams and auto-assign the two GKs per side.
        // 4. Flip who picks first, then deal 4 outfield cards at a time until squads are full.
        LoadGameSettings();
        ApplySettingsToDraft();
        LoadPlayersFromCSV("outfield_players");  // Load players from the CSV
        CreateDraftPool();  // Create the draft pool
        LoadGKFromCSV("goalkeepers");  // Load players from the CSV
        FilterAndShuffleGoalkeepers();
        CreateTeamSlots(homeTeamPanel, homeAveragePanel);
        CreateTeamSlots(awayTeamPanel, awayAveragePanel);
        if (IsInternationalDraftMode())
        {
            PrepopulateInternationalRosters();
            return;
        }

        AssignGoalkeepersToSlots();  // Assign GKs before dealing player cards
        PerformCoinFlip();
        DealNewDraftCards();  // Start the first draft round
    }

    private void StartFreeDraftFlow()
    {
        LoadGameSettings();
        ApplySettingsToDraft();
        LoadPlayersFromCSV("outfield_players");
        LoadGKFromCSV("goalkeepers");
        CreateFreeDraftPools();
        CreateTeamSlots(homeTeamPanel, homeAveragePanel);
        CreateTeamSlots(awayTeamPanel, awayAveragePanel);
        BindFreeDraftTable();
        PerformCoinFlip();

        if (currentSettings != null && currentSettings.gkDraft == "Deal")
        {
            AssignGoalkeepersToSlots();
            BeginFreeDraftOutfielderPhase();
            return;
        }

        BeginFreeDraftGoalkeeperPhase();
    }

    private void LoadGameSettings()
    {
        ApplicationManager.EnsureInstanceExists();
        string folderPath = ApplicationManager.Instance.GetSaveFolderPath();
        string settingsFilePath = ResolveGameSettingsFilePath(folderPath);
        if (string.IsNullOrEmpty(settingsFilePath))
        {
            Debug.LogError("No game settings files found in the persistent data path!");
            return;
        }

        ApplicationManager.Instance.SetActiveSaveFilePath(settingsFilePath);
        Debug.Log($"Loading draft settings from: {settingsFilePath}");

        string json = File.ReadAllText(settingsFilePath);

        // Parse the "gameSettings" node into the currentSettings object
        var root = JsonConvert.DeserializeObject<RootGameSettings>(json);

        if (root != null && root.gameSettings != null)
        {
            currentSettings = root.gameSettings; // Populate currentSettings from the nested node
            Debug.Log("Game settings loaded successfully!");
            Debug.Log($"Loaded Settings: {JsonConvert.SerializeObject(currentSettings, Formatting.Indented)}");
        }
        else
        {
            Debug.LogError("Failed to parse game settings from JSON file.");
        }
    }

    private string ResolveGameSettingsFilePath(string folderPath)
    {
        // Prefer the exact file selected in the previous scene before falling back to discovery.
        string exactFilePath = ApplicationManager.Instance.HasExplicitSaveContext
            ? ApplicationManager.Instance.GetLastSavedFilePath()
            : string.Empty;
        if (!string.IsNullOrEmpty(exactFilePath) && File.Exists(exactFilePath) && !IsProtectedRoomFixturePath(exactFilePath))
        {
            return exactFilePath;
        }
        if (!string.IsNullOrEmpty(exactFilePath) && IsProtectedRoomFixturePath(exactFilePath))
        {
            Debug.LogWarning($"Draft ignored protected Room fixture from explicit save context: {exactFilePath}");
        }

        string playerPrefsPath = PlayerPrefs.GetString("currentGameSettings", string.Empty);
        if (!string.IsNullOrEmpty(playerPrefsPath))
        {
            string resolvedPlayerPrefsPath = Path.IsPathRooted(playerPrefsPath)
                ? playerPrefsPath
                : Path.Combine(folderPath, playerPrefsPath);

            if (File.Exists(resolvedPlayerPrefsPath) && !IsProtectedRoomFixturePath(resolvedPlayerPrefsPath))
            {
                return resolvedPlayerPrefsPath;
            }
            if (File.Exists(resolvedPlayerPrefsPath) && IsProtectedRoomFixturePath(resolvedPlayerPrefsPath))
            {
                Debug.LogWarning($"Draft ignored protected Room fixture from PlayerPrefs: {resolvedPlayerPrefsPath}");
            }
        }

        // TODO: Replace this newest-file fallback with explicit save-slot selection when Load Game is implemented.
        string[] files = Directory.GetFiles(folderPath, "*.json");
        string newestNonProtectedFile = files
            .Where(path => !IsProtectedRoomFixturePath(path))
            .OrderByDescending(File.GetCreationTime)
            .FirstOrDefault();

        if (string.IsNullOrEmpty(newestNonProtectedFile))
        {
            return string.Empty;
        }

        return newestNonProtectedFile;
    }

    private bool IsProtectedRoomFixturePath(string filePath)
    {
        return !string.IsNullOrEmpty(filePath) &&
               string.Equals(
                   Path.GetFileName(filePath),
                   ProtectedRoomFixtureSaveFileName,
                   System.StringComparison.OrdinalIgnoreCase);
    }

    private void ApplySettingsToDraft()
    {
        if (currentSettings == null)
        {
            Debug.LogError("Cannot apply settings because they are null!");
            return;
        }

        // Parse squad size from settings
        if (int.TryParse(currentSettings.squadSize, out int parsedSquadSize))
        {
            squadSize = parsedSquadSize;
            Debug.Log($"Squad size applied: {squadSize}");
        }
        else
        {
            Debug.LogWarning("Invalid squad size in settings. Defaulting to 16.");
            squadSize = 16;
        }
        // Apply the referee setting
        refereeText.text = $"{currentSettings.referee}";
        homeTeamName.text = $"{currentSettings.homeTeamName}";
        awayTeamName.text = $"{currentSettings.awayTeamName}";

        // Apply the 'includeInternationals' setting
        useTableTopia = currentSettings.includeTabletopia;
        useNonTableTopia = currentSettings.includeNonTabletopia;
        useInternationals = currentSettings.includeInternationals;
        useTableTopiaGK = currentSettings.includeTabletopiaGK;
        useNonTableTopiaGK = currentSettings.includeNonTabletopiaGK;
        useInternationalsGK = currentSettings.includeInternationalsGK;
        Debug.Log($"Outfielders: Include TableTopia: {useTableTopia}, NonTableTopia: {useNonTableTopia}, internationals: {useInternationals}");
        Debug.Log($"GKs: Include TableTopia: {useTableTopiaGK}, NonTableTopia: {useNonTableTopiaGK}, internationals: {useInternationalsGK}");
    }

    void LoadPlayersFromCSV(string fileName)
    {
        allPlayers = new List<Player>();
        TextAsset csvFile = Resources.Load<TextAsset>(fileName);  // Load from Resources

        if (csvFile != null)
        {
            string[] lines = csvFile.text.Split(new[] { "\r\n", "\n" }, System.StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length <= 1)
            {
                return;
            }

            string[] headers = lines[0].Split(',').Select(header => header.Trim()).ToArray();

            for (int i = 1; i < lines.Length; i++)
            {
                Dictionary<string, string> playerData = new Dictionary<string, string>
                {
                    { "Name", GetCsvField(lines[i], headers, "Name") },
                    { "Nationality", GetCsvField(lines[i], headers, "Nationality") },
                    { "Pace", GetCsvField(lines[i], headers, "Pace") },
                    { "Dribbling", GetCsvField(lines[i], headers, "Dribbling") },
                    { "Heading", GetCsvField(lines[i], headers, "Heading") },
                    { "HighPass", GetCsvField(lines[i], headers, "High Pass") },
                    { "Resilience", GetCsvField(lines[i], headers, "Resilience") },
                    { "Shooting", GetCsvField(lines[i], headers, "Shooting") },
                    { "Tackling", GetCsvField(lines[i], headers, "Tackling") },
                    { "Type", GetCsvField(lines[i], headers, "Type") },
                    { "sqno", GetCsvField(lines[i], headers, "sqno") }
                };

                Player player = new Player(playerData);
                allPlayers.Add(player);  // Add the player to the list
            }
            Debug.Log($"Total players in allPlayers: {allPlayers.Count}");
            // var uniqueTypes = allPlayers.Select(player => player.Type?.Trim()).Distinct().ToList();
            // Debug.Log($"Unique Types: {string.Join(", ", uniqueTypes)}");
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
            string[] lines = csvFile.text.Split(new[] { "\r\n", "\n" }, System.StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length <= 1)
            {
                return;
            }

            string[] headers = lines[0].Split(',').Select(header => header.Trim()).ToArray();

            for (int i = 1; i < lines.Length; i++)
            {
                Dictionary<string, string> gkData = new Dictionary<string, string>
                {
                    { "Name", GetCsvField(lines[i], headers, "Name") },
                    { "Nationality", GetCsvField(lines[i], headers, "Nationality") },
                    { "Aerial", GetCsvField(lines[i], headers, "Aerial") },
                    { "Dribbling", GetCsvField(lines[i], headers, "Dribbling") },
                    { "Pace", GetCsvField(lines[i], headers, "Pace") },
                    { "Resilience", GetCsvField(lines[i], headers, "Resilience") },
                    { "Saving", GetCsvField(lines[i], headers, "Saving") },
                    { "Handling", GetCsvField(lines[i], headers, "Handling") },
                    { "HighPass", GetCsvField(lines[i], headers, "High Pass") },
                    { "Type", GetCsvField(lines[i], headers, "Type") },
                    { "sqno", GetCsvField(lines[i], headers, "sqno") }
                };

                Goalkeeper goalkeeper = new Goalkeeper(gkData);
                allGks.Add(goalkeeper);  // Add the player to the list
            }
        }
        else
        {
            Debug.LogError($"CSV file not found in Resources: {fileName}");
        }
    }

    private string GetCsvField(string line, string[] headers, string headerName)
    {
        int index = System.Array.FindIndex(headers, header => string.Equals(header, headerName, System.StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            return string.Empty;
        }

        string[] fields = line.Split(',');
        return index < fields.Length ? fields[index].Trim() : string.Empty;
    }

    void CreateDraftPool()
    {
        // Filter players based on the three boolean settings
        selectedDeck = allPlayers.Where(IsOutfielderAllowedBySettings).ToList();
        Debug.Log($"Filtered players based on settings. Total players in selectedDeck: {selectedDeck.Count}");

        // Shuffle the selectedDeck and limit it to squadSize * 2 cards
        ShuffleDeck(selectedDeck);
        draftPool = selectedDeck.GetRange(0, (squadSize-2) * 2);  // Limit to squadSize * 2 players - excluding GKs
        totalBatchCount = Mathf.CeilToInt(draftPool.Count / 4f);
        Debug.Log($"Total players in draftpool: {draftPool.Count}");
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
        // Filter players based on the three boolean settings
        selectedGks = allGks.Where(IsGoalkeeperAllowedBySettings).ToList();
        Debug.Log($"Filtered GKs based on settings. Total players in selectedDeck: {selectedGks.Count}");
        // Shuffle the selectedGks list
        ShuffleGKDeck(selectedGks);
    }

    private bool IsOutfielderAllowedBySettings(Player player)
    {
        if (player == null || string.IsNullOrEmpty(player.Type))
        {
            return true;
        }

        string type = player.Type.Trim();
        if (!useInternationals && type.Equals("World Cup", System.StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!useTableTopia && type.Equals("TableTopia", System.StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!useNonTableTopia && type.Equals("Not TableTopia", System.StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private bool IsGoalkeeperAllowedBySettings(Goalkeeper goalkeeper)
    {
        if (goalkeeper == null || string.IsNullOrEmpty(goalkeeper.Type))
        {
            return true;
        }

        string type = goalkeeper.Type.Trim();
        if (!useInternationalsGK && type.Equals("World Cup", System.StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!useTableTopiaGK && type.Equals("TableTopia", System.StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!useNonTableTopiaGK && type.Equals("Not TableTopia", System.StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
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
        if (selectedGks == null || selectedGks.Count < 4)
        {
            int goalkeeperCount = selectedGks != null ? selectedGks.Count : 0;
            Debug.LogError($"Cannot deal goalkeepers. Need 4 filtered GKs, found {goalkeeperCount}.");
            return;
        }

        ShuffleGKDeck(selectedGks);
        List<Goalkeeper> homeGoalkeepers = GetGoalkeeperDealOrder(selectedGks.Take(2)).ToList();
        List<Goalkeeper> awayGoalkeepers = GetGoalkeeperDealOrder(selectedGks.Skip(2).Take(2)).ToList();

        AssignGoalkeeperToSlot(homeGoalkeepers[0], "Home-1");
        AssignGoalkeeperToSlot(homeGoalkeepers[1], "Home-12");
        AssignGoalkeeperToSlot(awayGoalkeepers[0], "Away-1");
        AssignGoalkeeperToSlot(awayGoalkeepers[1], "Away-12");

        foreach (Goalkeeper goalkeeper in homeGoalkeepers.Concat(awayGoalkeepers))
        {
            selectedGks.Remove(goalkeeper);
        }
    }

    private bool IsInternationalDraftMode()
    {
        if (currentSettings == null)
        {
            return false;
        }

        return string.Equals(currentSettings.matchType, MatchTypeInternational, System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(currentSettings.draft, DraftInternational, System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(currentSettings.gkDraft, DraftInternational, System.StringComparison.OrdinalIgnoreCase);
    }

    private void PrepopulateInternationalRosters()
    {
        draftPool = new List<Player>();
        selectedDeck = new List<Player>();
        selectedGks = new List<Goalkeeper>();
        currentBatchNumber = 0;
        totalBatchCount = 0;
        currentBatchSize = 0;
        cardsAssignedThisRound = 0;
        currentTeamTurn = string.Empty;
        firstBatchStarter = string.Empty;
        currentBatchStarter = string.Empty;

        if (draftPanel != null)
        {
            draftPanel.SetActive(false);
        }

        PrepopulateInternationalTeamRoster("Home", currentSettings.homeTeamName, homeTeamPanel);
        PrepopulateInternationalTeamRoster("Away", currentSettings.awayTeamName, awayTeamPanel);
        RefreshRosterAverages();

        DraftUIManager uiManager = GetDraftUIManager();
        if (uiManager != null)
        {
            uiManager.CheckIfDraftIsComplete();
        }

        Debug.Log($"International rosters prepopulated for {currentSettings.homeTeamName} vs {currentSettings.awayTeamName}.");
    }

    private void PrepopulateInternationalTeamRoster(string rosterPrefix, string teamName, GameObject teamPanel)
    {
        if (teamPanel == null)
        {
            Debug.LogError($"Cannot prepopulate {rosterPrefix} international roster because the team panel is missing.");
            return;
        }

        List<Goalkeeper> goalkeepers = allGks
            .Where(goalkeeper => IsWorldCupTeamMember(goalkeeper.Type, goalkeeper.Country, teamName))
            .ToList();
        List<Goalkeeper> orderedGoalkeepers = GetGoalkeeperDealOrder(goalkeepers);
        if (orderedGoalkeepers.Count != 2)
        {
            Debug.LogWarning($"{teamName} should have exactly two World Cup goalkeepers, found {orderedGoalkeepers.Count}.");
        }

        if (orderedGoalkeepers.Count > 0)
        {
            AssignGoalkeeperToSlot(orderedGoalkeepers[0], $"{rosterPrefix}-1");
        }

        if (orderedGoalkeepers.Count > 1)
        {
            AssignGoalkeeperToSlot(orderedGoalkeepers[1], $"{rosterPrefix}-12");
        }

        List<Player> outfielders = allPlayers
            .Where(player => IsWorldCupTeamMember(player.Type, player.Country, teamName))
            .OrderBy(player => player.SquadNumber <= 0)
            .ThenBy(player => player.SquadNumber <= 0 ? int.MaxValue : player.SquadNumber)
            .ThenBy(player => player.Name)
            .ToList();
        AssignInternationalOutfieldersToSlots(outfielders, rosterPrefix, teamPanel);
    }

    private bool IsWorldCupTeamMember(string type, string nationality, string teamName)
    {
        return string.Equals(type?.Trim(), WorldCupPlayerType, System.StringComparison.OrdinalIgnoreCase)
            && string.Equals(nationality?.Trim(), teamName?.Trim(), System.StringComparison.OrdinalIgnoreCase);
    }

    private void AssignInternationalOutfieldersToSlots(List<Player> outfielders, string rosterPrefix, GameObject teamPanel)
    {
        HashSet<int> assignedJerseyNumbers = new HashSet<int>();
        HashSet<Player> assignedPlayers = new HashSet<Player>();
        List<int> freeOutfieldJerseyNumbers = GetFreeOutfieldJerseyNumbers(teamPanel);

        foreach (Player player in outfielders.Where(player => IsValidOutfieldJerseyNumber(player.SquadNumber)))
        {
            if (assignedJerseyNumbers.Contains(player.SquadNumber) || !freeOutfieldJerseyNumbers.Contains(player.SquadNumber))
            {
                Debug.LogWarning($"{player.Name} has duplicate or unavailable sqno {player.SquadNumber}; assigning a free outfield number instead.");
                continue;
            }

            if (AssignPlayerToSlot(player, $"{rosterPrefix}-{player.SquadNumber}"))
            {
                assignedJerseyNumbers.Add(player.SquadNumber);
                assignedPlayers.Add(player);
                freeOutfieldJerseyNumbers.Remove(player.SquadNumber);
            }
        }

        foreach (Player player in outfielders.Where(player => !assignedPlayers.Contains(player)))
        {
            if (freeOutfieldJerseyNumbers.Count == 0)
            {
                Debug.LogWarning($"No free outfield jersey number remains for {player.Name}.");
                return;
            }

            int jerseyNumber = freeOutfieldJerseyNumbers[0];
            if (AssignPlayerToSlot(player, $"{rosterPrefix}-{jerseyNumber}"))
            {
                freeOutfieldJerseyNumbers.RemoveAt(0);
            }
        }
    }

    private List<int> GetFreeOutfieldJerseyNumbers(GameObject teamPanel)
    {
        List<int> jerseyNumbers = new List<int>();
        foreach (Transform child in teamPanel.transform)
        {
            PlayerSlotDropHandler slot = child.GetComponent<PlayerSlotDropHandler>();
            if (slot == null || slot.IsGoalkeeperRosterSlot())
            {
                continue;
            }

            int jerseyNumber = slot.GetJerseyNumber();
            if (IsValidOutfieldJerseyNumber(jerseyNumber))
            {
                jerseyNumbers.Add(jerseyNumber);
            }
        }

        jerseyNumbers.Sort();
        return jerseyNumbers;
    }

    private bool IsValidOutfieldJerseyNumber(int jerseyNumber)
    {
        return jerseyNumber >= 2 && jerseyNumber <= squadSize && jerseyNumber != 12;
    }

    private bool AssignPlayerToSlot(Player player, string slotName)
    {
        GameObject slot = GameObject.Find(slotName);
        if (slot == null)
        {
            Debug.LogError($"Slot {slotName} not found!");
            return false;
        }

        PlayerSlotDropHandler slotHandler = slot.GetComponent<PlayerSlotDropHandler>();
        if (slotHandler == null)
        {
            return false;
        }

        slotHandler.UpdatePlayerSlot(player);
        return true;
    }

    private List<Goalkeeper> GetGoalkeeperDealOrder(IEnumerable<Goalkeeper> goalkeepers)
    {
        return goalkeepers
            .OrderByDescending(gk => gk.Saving)
            .ThenByDescending(gk => gk.Handling)
            .ThenByDescending(gk => gk.Pace)
            .ThenByDescending(gk => gk.Aerial)
            .ThenByDescending(gk => gk.Name)
            .ToList();
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
    // TODO: Replace this hidden random draw with a visual pre-draft flow that follows normal football procedure:
    // show the coin toss, let the winner choose kick-off or sides, and make the "who picks first" outcome explicit in the UI.
    {
        // The coin flip determines who gets first pick in batch 1.
        // Later 4-card batches alternate the starting side automatically.
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
        firstBatchStarter = currentTeamTurn;
        currentBatchStarter = currentTeamTurn;
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
        // draftPool tracks the undealt outfielders that remain after the current 4-card batch.
        // This extra remove is harmless if the player was already removed during DealNewDraftCards.
        draftPool.Remove(card.assignedPlayer);
        // Increment cards assigned in this round
        cardsAssignedThisRound++;
        // Alternate the current team turn after each card assignment
        currentTeamTurn = currentTeamTurn == "Home" ? "Away" : "Home";

        // A draft round is exactly 4 face-up cards. Once all 4 are assigned:
        // - deal the next 4 cards if any remain
        // - alternate who gets first pick in the next round
        // This mirrors the tabletop rule:
        // batch 1 starter also starts batches 3, 5, 7...
        // other side starts batches 2, 4, 6, 8...
        if (cardsAssignedThisRound >= 4 && draftPool.Count()>0)
        {
            // When 4 cards have been assigned, deal new ones
            DealNewDraftCards();
            cardsAssignedThisRound = 0;  // Reset for the next round
            isHomeFirstInNextRound = !isHomeFirstInNextRound;  // Alternate which team starts
            // Set current team for the next round's first pick
            currentTeamTurn = isHomeFirstInNextRound ? "Home" : "Away";
            currentBatchStarter = currentTeamTurn;
            Debug.Log($"New round started. {currentTeamTurn} picks first.");
        }
        else if (cardsAssignedThisRound >= 4)
        {
            // Check if the draft is complete
            DraftUIManager draftUIManager = FindAnyObjectByType<DraftUIManager>();
            draftUIManager.CheckIfDraftIsComplete();  // Enable the Start Game button if the draft is complete
        }
        if (draftPool.Count == 0)
        {
            Debug.Log("No more cards to deal. Draft pool is empty.");
        }

        DraftUIManager liveDraftUi = FindAnyObjectByType<DraftUIManager>();
        if (liveDraftUi != null)
        {
            liveDraftUi.RefreshDraftStateUI();
        }
    }

    public string GetCurrentTeamTurn()
    {
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
        // Clear the previous 4-card batch from the center panel before dealing the next one.
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
        
        // The regular draft always reveals up to 4 cards. Near the end we deal whatever is left.
        int cardsToDeal = Mathf.Min(4, draftPool.Count);
        currentBatchNumber++;
        currentBatchSize = cardsToDeal;

        // Deal new cards
        for (int i = 0; i < cardsToDeal; i++)
        {
            Player nextPlayer = draftPool[i];
            GameObject newCard = Instantiate(playerCardPrefab, draftPanel.transform);
            PlayerCard playerCard = newCard.GetComponent<PlayerCard>();
            playerCard.UpdatePlayerCard(nextPlayer);
            // Debug.Log($"Dealt card for player: {nextPlayer.Name}");
        }

        // Remove the dealt cards immediately so draftPool now represents undealt cards only.
        draftPool.RemoveRange(0, cardsToDeal);

        // Reset the round
        cardsAssignedThisRound = 0;
        DraftUIManager draftUIManager = FindAnyObjectByType<DraftUIManager>();
        if (draftUIManager != null)
        {
            draftUIManager.RefreshDraftStateUI();
        }
    }

    public int GetCurrentBatchNumber()
    {
        return currentBatchNumber;
    }

    public int GetTotalBatchCount()
    {
        return totalBatchCount;
    }

    public int GetRemainingSelectionsInCurrentBatch()
    {
        return Mathf.Max(0, currentBatchSize - cardsAssignedThisRound);
    }

    public string GetFirstBatchStarter()
    {
        return firstBatchStarter;
    }

    public string GetCurrentBatchStarter()
    {
        return currentBatchStarter;
    }

    public bool IsDraftComplete()
    {
        if (isFreeDraftScene)
        {
            return freeDraftPhase == FreeDraftPhase.Complete;
        }

        if (IsInternationalDraftMode())
        {
            return homeTeamPanel != null
                && awayTeamPanel != null
                && AreRostersFull();
        }

        return draftPool != null && draftPool.Count == 0 && cardsAssignedThisRound >= currentBatchSize;
    }

    public bool IsFreeDraftMode()
    {
        return isFreeDraftScene;
    }

    public string GetFreeDraftPhaseName()
    {
        switch (freeDraftPhase)
        {
            case FreeDraftPhase.Goalkeepers:
                return "Goalkeepers";
            case FreeDraftPhase.Outfielders:
                return "Outfielders";
            case FreeDraftPhase.Complete:
                return "Complete";
            default:
                return "Setup";
        }
    }

    public string GetFreeDraftProgressText()
    {
        if (!isFreeDraftScene)
        {
            return string.Empty;
        }

        if (freeDraftPhase == FreeDraftPhase.Goalkeepers)
        {
            int remainingGoalkeepers = CountEmptyGoalkeeperSlots(homeTeamPanel.transform) + CountEmptyGoalkeeperSlots(awayTeamPanel.transform);
            string label = remainingGoalkeepers == 1 ? "GK pick" : "GK picks";
            return $"{remainingGoalkeepers} {label} remaining";
        }

        if (freeDraftPhase == FreeDraftPhase.Outfielders)
        {
            int remainingOutfielders = CountEmptyOutfieldSlots(homeTeamPanel.transform) + CountEmptyOutfieldSlots(awayTeamPanel.transform);
            string label = remainingOutfielders == 1 ? "outfielder pick" : "outfielder picks";
            return $"{remainingOutfielders} {label} remaining";
        }

        if (freeDraftPhase == FreeDraftPhase.Complete)
        {
            return "Both rosters are full";
        }

        return "Preparing draft pools";
    }

    public bool AssignFreeDraftCandidateToNextSlot(FreeDraftTableRowDragHandler row)
    {
        if (row == null || !isFreeDraftScene)
        {
            return false;
        }

        string rosterPanelName = currentTeamTurn == "Home" ? "HomeRoster" : "AwayRoster";
        PlayerSlotDropHandler nextSlot = row.IsGoalkeeper
            ? FindNextAvailableGoalkeeperSlot(rosterPanelName)
            : FindNextAvailableOutfieldSlot(rosterPanelName, 0);

        return nextSlot != null && AssignFreeDraftCandidateToSlot(row, nextSlot);
    }

    public bool AssignFreeDraftCandidateToSlot(FreeDraftTableRowDragHandler row, PlayerSlotDropHandler targetSlot)
    {
        if (row == null)
        {
            return false;
        }

        return AssignFreeDraftCandidateToSlot(row.CandidateName, row.IsGoalkeeper, targetSlot);
    }

    private bool AssignFreeDraftCandidateToSlot(string candidateName, bool isGoalkeeper, PlayerSlotDropHandler targetSlot)
    {
        if (!isFreeDraftScene || targetSlot == null)
        {
            return false;
        }

        bool expectedGoalkeeper = freeDraftPhase == FreeDraftPhase.Goalkeepers;
        if (isGoalkeeper != expectedGoalkeeper)
        {
            Debug.LogWarning($"Cannot draft '{candidateName}' during {freeDraftPhase} phase.");
            return false;
        }

        string rosterName = targetSlot.transform.parent != null ? targetSlot.transform.parent.name : string.Empty;
        if (!IsValidTeamPanel(rosterName))
        {
            Debug.LogWarning($"Invalid FreeDraft drop: {rosterName} is not the active roster for {currentTeamTurn}.");
            return false;
        }

        PlayerSlotDropHandler destinationSlot = isGoalkeeper
            ? ResolveGoalkeeperDestinationSlot(targetSlot)
            : ResolveOutfielderDestinationSlot(targetSlot);

        if (destinationSlot == null)
        {
            Debug.LogWarning($"No valid destination slot found for '{candidateName}'.");
            return false;
        }

        if (isGoalkeeper)
        {
            Goalkeeper goalkeeper = selectedGks.FirstOrDefault(gk => gk.Name == candidateName);
            if (goalkeeper == null)
            {
                Debug.LogWarning($"Goalkeeper '{candidateName}' is not in the remaining FreeDraft GK pool.");
                return false;
            }

            destinationSlot.UpdateGoalkeeperSlot(goalkeeper);
            selectedGks.Remove(goalkeeper);
        }
        else
        {
            Player player = draftPool.FirstOrDefault(candidate => candidate.Name == candidateName);
            if (player == null)
            {
                Debug.LogWarning($"Player '{candidateName}' is not in the remaining FreeDraft outfielder pool.");
                return false;
            }

            destinationSlot.UpdatePlayerSlot(player);
            draftPool.Remove(player);
        }

        CompleteFreeDraftPick();
        return true;
    }

    public void RefreshRosterAverages()
    {
        UpdateTeamAverages(homeTeamPanel.transform, homeAveragePanel.transform);
        UpdateTeamAverages(awayTeamPanel.transform, awayAveragePanel.transform);
    }

    private void CreateFreeDraftPools()
    {
        selectedGks = allGks
            .Where(IsGoalkeeperAllowedBySettings)
            .OrderByDescending(gk => gk.Saving)
            .ThenByDescending(gk => gk.Handling)
            .ThenByDescending(gk => gk.Pace)
            .ThenByDescending(gk => gk.Aerial)
            .ThenByDescending(gk => gk.Name)
            .ToList();

        selectedDeck = allPlayers
            .Where(IsOutfielderAllowedBySettings)
            .OrderByDescending(GetOutfielderTotal)
            .ThenByDescending(player => player.Shooting)
            .ThenByDescending(player => player.Pace)
            .ThenByDescending(player => player.Dribbling)
            .ThenByDescending(player => player.Tackling)
            .ThenByDescending(player => player.Name)
            .ToList();

        draftPool = new List<Player>(selectedDeck);
        Debug.Log($"FreeDraft pools built. GKs: {selectedGks.Count}, Outfielders: {draftPool.Count}");
    }

    private int GetOutfielderTotal(Player player)
    {
        return player.Shooting + player.Dribbling + player.Pace + player.Tackling + player.Heading;
    }

    private void BindFreeDraftTable()
    {
        Transform tableRoot = draftPanel != null ? draftPanel.transform.Find("FreeDraftTable") : null;
        freeDraftContent = tableRoot != null ? tableRoot.Find("ScrollView/Viewport/Content") : null;
        freeDraftPreviewRow = freeDraftContent != null ? freeDraftContent.Find("PreviewRow") : null;
        Transform titleTransform = tableRoot != null ? tableRoot.Find("TableTitle") : null;
        freeDraftTitleText = titleTransform != null ? titleTransform.GetComponent<TMP_Text>() : null;
        Transform headerRow = tableRoot != null ? tableRoot.Find("HeaderRow") : null;
        Transform filterRow = tableRoot != null ? tableRoot.Find("FilterRow") : null;

        if (freeDraftPreviewRow == null)
        {
            Debug.LogError("FreeDraft table PreviewRow was not found. The table must be authored in FreeDraft.unity.");
            return;
        }

        freeDraftPreviewRow.gameObject.SetActive(false);
        BindFreeDraftHeaderControls(headerRow);
        BindFreeDraftFilterControls(filterRow);
        ClearFreeDraftRows();
    }

    private void BindFreeDraftHeaderControls(Transform headerRow)
    {
        if (headerRow == null)
        {
            Debug.LogError("FreeDraft HeaderRow was not found. The table must be authored in FreeDraft.unity.");
            return;
        }

        BindFreeDraftHeaderButton(headerRow, "NameHeader", "name");
        BindFreeDraftHeaderButton(headerRow, "NatHeader", "nationality");
        BindFreeDraftHeaderButton(headerRow, "PaceHeader", "pace");
        BindFreeDraftHeaderButton(headerRow, "DrHeader", "dribbling");
        BindFreeDraftHeaderButton(headerRow, "H/AHeader", "headingAerial");
        BindFreeDraftHeaderButton(headerRow, "HPHeader", "highPass");
        BindFreeDraftHeaderButton(headerRow, "ResHeader", "resilience");
        BindFreeDraftHeaderButton(headerRow, "Sh/SvHeader", "shootingSaving");
        BindFreeDraftHeaderButton(headerRow, "Tac/HanHeader", "tacklingHandling");
        BindFreeDraftHeaderButton(headerRow, "TypeHeader", "type");
    }

    private void BindFreeDraftHeaderButton(Transform headerRow, string headerName, string columnKey)
    {
        Transform header = FindDirectChildByName(headerRow, headerName);
        if (header == null)
        {
            Debug.LogWarning($"FreeDraft header '{headerName}' was not found.");
            return;
        }

        Button button = header.GetComponent<Button>();
        if (button == null)
        {
            button = header.gameObject.AddComponent<Button>();
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => ToggleFreeDraftSort(columnKey));
    }

    private void BindFreeDraftFilterControls(Transform filterRow)
    {
        if (filterRow == null)
        {
            Debug.LogError("FreeDraft FilterRow was not found. The table must be authored in FreeDraft.unity.");
            return;
        }

        freeDraftFilterFields.Clear();
        BindFreeDraftFilterField(filterRow, "NameFilter", "name", false);
        BindFreeDraftFilterField(filterRow, "NatFilter", "nationality", false);
        BindFreeDraftFilterField(filterRow, "PaceFilter", "pace", true);
        BindFreeDraftFilterField(filterRow, "DrFilter", "dribbling", true);
        BindFreeDraftFilterField(filterRow, "H/AFilter", "headingAerial", true);
        BindFreeDraftFilterField(filterRow, "HPFilter", "highPass", true);
        BindFreeDraftFilterField(filterRow, "ResFilter", "resilience", true);
        BindFreeDraftFilterField(filterRow, "Sh/SvFilter", "shootingSaving", true);
        BindFreeDraftFilterField(filterRow, "Tac/HanFilter", "tacklingHandling", true);
        BindFreeDraftFilterField(filterRow, "TypeFilter", "type", false);
    }

    private void BindFreeDraftFilterField(Transform filterRow, string filterName, string columnKey, bool numeric)
    {
        Transform filter = FindDirectChildByName(filterRow, filterName);
        if (filter == null)
        {
            Debug.LogWarning($"FreeDraft filter '{filterName}' was not found.");
            return;
        }

        FreeDraftTableFilterField filterField = filter.GetComponent<FreeDraftTableFilterField>();
        if (filterField == null)
        {
            filterField = filter.gameObject.AddComponent<FreeDraftTableFilterField>();
        }

        string defaultValue = numeric ? ">=1" : string.Empty;
        string placeholder = numeric ? ">=1" : "filter";
        filterField.Configure(this, columnKey, numeric, defaultValue, placeholder);
        freeDraftFilterFields[columnKey] = filterField;
    }

    private void BeginFreeDraftGoalkeeperPhase()
    {
        freeDraftPhase = FreeDraftPhase.Goalkeepers;
        ResetFreeDraftTableControls();
        RenderFreeDraftTable();
        RefreshDraftUI();
    }

    private void BeginFreeDraftOutfielderPhase()
    {
        freeDraftPhase = FreeDraftPhase.Outfielders;
        ResetFreeDraftTableControls();
        RenderFreeDraftTable();
        RefreshDraftUI();
    }

    private void CompleteFreeDraft()
    {
        freeDraftPhase = FreeDraftPhase.Complete;
        ClearFreeDraftRows();
        if (freeDraftTitleText != null)
        {
            freeDraftTitleText.text = "Free Draft Complete";
        }

        RefreshDraftUI();
        DraftUIManager uiManager = GetDraftUIManager();
        if (uiManager != null)
        {
            uiManager.CheckIfDraftIsComplete();
        }
    }

    private void CompleteFreeDraftPick()
    {
        RefreshRosterAverages();
        currentTeamTurn = currentTeamTurn == "Home" ? "Away" : "Home";

        if (freeDraftPhase == FreeDraftPhase.Goalkeepers && AreAllGoalkeeperSlotsFilled())
        {
            BeginFreeDraftOutfielderPhase();
            return;
        }

        if (freeDraftPhase == FreeDraftPhase.Outfielders && AreRostersFull())
        {
            CompleteFreeDraft();
            return;
        }

        RenderFreeDraftTable();
        RefreshDraftUI();
    }

    public void UpdateFreeDraftFilter(string columnKey, string filterValue)
    {
        freeDraftFilters[columnKey] = filterValue ?? string.Empty;
        RenderFreeDraftTable();
    }

    public void FocusAdjacentFreeDraftFilter(string columnKey, int direction)
    {
        if (FreeDraftFilterOrder.Length == 0)
        {
            return;
        }

        int currentIndex = System.Array.IndexOf(FreeDraftFilterOrder, columnKey);
        if (currentIndex < 0)
        {
            currentIndex = 0;
        }

        int normalizedDirection = direction < 0 ? -1 : 1;
        int nextIndex = (currentIndex + normalizedDirection + FreeDraftFilterOrder.Length) % FreeDraftFilterOrder.Length;
        string nextColumnKey = FreeDraftFilterOrder[nextIndex];
        if (freeDraftFilterFields.TryGetValue(nextColumnKey, out FreeDraftTableFilterField filterField))
        {
            filterField.FocusInput();
        }
    }

    private void ToggleFreeDraftSort(string columnKey)
    {
        if (!freeDraftHasUserSort || freeDraftSortKey != columnKey)
        {
            freeDraftSortKey = columnKey;
            freeDraftSortAscending = false;
            freeDraftHasUserSort = true;
        }
        else if (!freeDraftSortAscending)
        {
            freeDraftSortAscending = true;
        }
        else
        {
            freeDraftSortKey = string.Empty;
            freeDraftSortAscending = false;
            freeDraftHasUserSort = false;
        }

        RenderFreeDraftTable();
    }

    private void ResetFreeDraftTableControls()
    {
        freeDraftHasUserSort = false;
        freeDraftSortKey = string.Empty;
        freeDraftSortAscending = false;

        SetFreeDraftFilterDefault("name", string.Empty);
        SetFreeDraftFilterDefault("nationality", string.Empty);
        SetFreeDraftFilterDefault("pace", ">=1");
        SetFreeDraftFilterDefault("dribbling", ">=1");
        SetFreeDraftFilterDefault("headingAerial", ">=1");
        SetFreeDraftFilterDefault("highPass", ">=1");
        SetFreeDraftFilterDefault("resilience", ">=1");
        SetFreeDraftFilterDefault("shootingSaving", ">=1");
        SetFreeDraftFilterDefault("tacklingHandling", ">=1");
        SetFreeDraftFilterDefault("type", string.Empty);

        foreach (KeyValuePair<string, FreeDraftTableFilterField> field in freeDraftFilterFields)
        {
            if (freeDraftFilters.TryGetValue(field.Key, out string filterValue))
            {
                field.Value.SetValueWithoutNotify(filterValue);
            }
        }
    }

    private void SetFreeDraftFilterDefault(string columnKey, string filterValue)
    {
        freeDraftFilters[columnKey] = filterValue;
    }

    private List<FreeDraftCandidateView> GetVisibleFreeDraftCandidates()
    {
        IEnumerable<FreeDraftCandidateView> candidates = freeDraftPhase == FreeDraftPhase.Goalkeepers
            ? selectedGks.Select(FreeDraftCandidateView.FromGoalkeeper)
            : draftPool.Select(FreeDraftCandidateView.FromPlayer);

        candidates = candidates.Where(CandidateMatchesFreeDraftFilters);

        if (!freeDraftHasUserSort || string.IsNullOrEmpty(freeDraftSortKey))
        {
            return SortFreeDraftCandidatesByDefault(candidates).ToList();
        }

        return SortFreeDraftCandidates(candidates, freeDraftSortKey, freeDraftSortAscending).ToList();
    }

    private bool CandidateMatchesFreeDraftFilters(FreeDraftCandidateView candidate)
    {
        return MatchesTextFilter(candidate.Name, GetFreeDraftFilter("name")) &&
               MatchesTextFilter(AbbreviateNationality(candidate.Nationality), GetFreeDraftFilter("nationality")) &&
               MatchesTextFilter(candidate.Type, GetFreeDraftFilter("type")) &&
               MatchesNumericFilter(candidate.Pace, GetFreeDraftFilter("pace")) &&
               MatchesNumericFilter(candidate.Dribbling, GetFreeDraftFilter("dribbling")) &&
               MatchesNumericFilter(candidate.HeadingOrAerial, GetFreeDraftFilter("headingAerial")) &&
               MatchesNumericFilter(candidate.HighPass, GetFreeDraftFilter("highPass")) &&
               MatchesNumericFilter(candidate.Resilience, GetFreeDraftFilter("resilience")) &&
               MatchesNumericFilter(candidate.ShootingOrSaving, GetFreeDraftFilter("shootingSaving")) &&
               MatchesNumericFilter(candidate.TacklingOrHandling, GetFreeDraftFilter("tacklingHandling"));
    }

    private string GetFreeDraftFilter(string columnKey)
    {
        return freeDraftFilters.TryGetValue(columnKey, out string filterValue) ? filterValue : string.Empty;
    }

    private bool MatchesTextFilter(string candidateValue, string filterValue)
    {
        if (string.IsNullOrWhiteSpace(filterValue))
        {
            return true;
        }

        return (candidateValue ?? string.Empty).IndexOf(filterValue.Trim(), System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private bool MatchesNumericFilter(int candidateValue, string filterValue)
    {
        if (string.IsNullOrWhiteSpace(filterValue))
        {
            return true;
        }

        string trimmed = filterValue.Trim().Replace(" ", string.Empty);
        if (trimmed.StartsWith(">=", System.StringComparison.Ordinal))
        {
            return int.TryParse(trimmed.Substring(2), out int minimumInclusive) && candidateValue >= minimumInclusive;
        }

        if (trimmed.StartsWith("<=", System.StringComparison.Ordinal))
        {
            return int.TryParse(trimmed.Substring(2), out int maximumInclusive) && candidateValue <= maximumInclusive;
        }

        if (trimmed.StartsWith(">", System.StringComparison.Ordinal))
        {
            return int.TryParse(trimmed.Substring(1), out int minimumExclusive) && candidateValue > minimumExclusive;
        }

        if (trimmed.StartsWith("<", System.StringComparison.Ordinal))
        {
            return int.TryParse(trimmed.Substring(1), out int maximumExclusive) && candidateValue < maximumExclusive;
        }

        if (trimmed.StartsWith("=", System.StringComparison.Ordinal))
        {
            return int.TryParse(trimmed.Substring(1), out int exactValue) && candidateValue == exactValue;
        }

        return int.TryParse(trimmed, out int exactValueWithoutOperator) && candidateValue == exactValueWithoutOperator;
    }

    private IEnumerable<FreeDraftCandidateView> SortFreeDraftCandidates(
        IEnumerable<FreeDraftCandidateView> candidates,
        string columnKey,
        bool ascending)
    {
        if (IsFreeDraftTextColumn(columnKey))
        {
            return ascending
                ? candidates.OrderBy(candidate => GetFreeDraftTextSortValue(candidate, columnKey), System.StringComparer.OrdinalIgnoreCase)
                    .ThenBy(candidate => candidate.Name, System.StringComparer.OrdinalIgnoreCase)
                : candidates.OrderByDescending(candidate => GetFreeDraftTextSortValue(candidate, columnKey), System.StringComparer.OrdinalIgnoreCase)
                    .ThenByDescending(candidate => candidate.Name, System.StringComparer.OrdinalIgnoreCase);
        }

        return ascending
            ? candidates.OrderBy(candidate => GetFreeDraftNumericSortValue(candidate, columnKey))
                .ThenBy(candidate => candidate.Name, System.StringComparer.OrdinalIgnoreCase)
            : candidates.OrderByDescending(candidate => GetFreeDraftNumericSortValue(candidate, columnKey))
                .ThenByDescending(candidate => candidate.Name, System.StringComparer.OrdinalIgnoreCase);
    }

    private bool IsFreeDraftTextColumn(string columnKey)
    {
        return columnKey == "name" || columnKey == "nationality" || columnKey == "type";
    }

    private string GetFreeDraftTextSortValue(FreeDraftCandidateView candidate, string columnKey)
    {
        switch (columnKey)
        {
            case "nationality":
                return AbbreviateNationality(candidate.Nationality);
            case "type":
                return candidate.Type;
            default:
                return candidate.Name;
        }
    }

    private int GetFreeDraftNumericSortValue(FreeDraftCandidateView candidate, string columnKey)
    {
        switch (columnKey)
        {
            case "pace":
                return candidate.Pace;
            case "dribbling":
                return candidate.Dribbling;
            case "headingAerial":
                return candidate.HeadingOrAerial;
            case "highPass":
                return candidate.HighPass;
            case "resilience":
                return candidate.Resilience;
            case "shootingSaving":
                return candidate.ShootingOrSaving;
            case "tacklingHandling":
                return candidate.TacklingOrHandling;
            default:
                return 0;
        }
    }

    private IEnumerable<FreeDraftCandidateView> SortFreeDraftCandidatesByDefault(IEnumerable<FreeDraftCandidateView> candidates)
    {
        if (freeDraftPhase == FreeDraftPhase.Goalkeepers)
        {
            return candidates
                .OrderByDescending(candidate => candidate.ShootingOrSaving)
                .ThenByDescending(candidate => candidate.TacklingOrHandling)
                .ThenByDescending(candidate => candidate.Pace)
                .ThenByDescending(candidate => candidate.HeadingOrAerial)
                .ThenByDescending(candidate => candidate.Name, System.StringComparer.OrdinalIgnoreCase);
        }

        return candidates
            .OrderByDescending(GetFreeDraftOutfielderDefaultTotal)
            .ThenByDescending(candidate => candidate.ShootingOrSaving)
            .ThenByDescending(candidate => candidate.Pace)
            .ThenByDescending(candidate => candidate.Dribbling)
            .ThenByDescending(candidate => candidate.TacklingOrHandling)
            .ThenByDescending(candidate => candidate.Name, System.StringComparer.OrdinalIgnoreCase);
    }

    private int GetFreeDraftOutfielderDefaultTotal(FreeDraftCandidateView candidate)
    {
        return candidate.Pace +
               candidate.Dribbling +
               candidate.HeadingOrAerial +
               candidate.TacklingOrHandling +
               candidate.ShootingOrSaving;
    }

    private void RenderFreeDraftTable()
    {
        if (freeDraftContent == null || freeDraftPreviewRow == null)
        {
            return;
        }

        ClearFreeDraftRows();

        if (freeDraftTitleText != null)
        {
            freeDraftTitleText.text = freeDraftPhase == FreeDraftPhase.Goalkeepers
                ? "Free Draft Goalkeepers"
                : "Free Draft Outfielders";
        }

        UpdateFreeDraftColumnHeaders();

        List<FreeDraftCandidateView> candidates = GetVisibleFreeDraftCandidates();

        RectTransform previewRect = freeDraftPreviewRow as RectTransform;
        float rowHeight = previewRect != null && Mathf.Abs(previewRect.sizeDelta.y) > 0f
            ? Mathf.Abs(previewRect.sizeDelta.y)
            : 30f;
        Vector2 previewPosition = previewRect != null ? previewRect.anchoredPosition : new Vector2(0f, -15f);

        for (int index = 0; index < candidates.Count; index++)
        {
            FreeDraftCandidateView candidate = candidates[index];
            GameObject rowObject = Instantiate(freeDraftPreviewRow.gameObject, freeDraftContent, false);
            rowObject.name = $"{(candidate.IsGoalkeeper ? "GK" : "Player")}Row-{candidate.Name}";
            rowObject.SetActive(true);

            RectTransform rowRect = rowObject.GetComponent<RectTransform>();
            if (rowRect != null)
            {
                rowRect.anchoredPosition = new Vector2(previewPosition.x, previewPosition.y - rowHeight * index);
            }

            SetFreeDraftRowCell(rowObject.transform, "NameCell", candidate.Name);
            SetFreeDraftRowCell(rowObject.transform, "NatCell", AbbreviateNationality(candidate.Nationality));
            SetFreeDraftRowCell(rowObject.transform, "PaceCell", candidate.Pace.ToString());
            SetFreeDraftRowCell(rowObject.transform, "DrCell", candidate.Dribbling.ToString());
            SetFreeDraftRowCell(rowObject.transform, "H/ACell", candidate.HeadingOrAerial.ToString());
            SetFreeDraftRowCell(rowObject.transform, "HPCell", candidate.HighPass.ToString());
            SetFreeDraftRowCell(rowObject.transform, "ResCell", candidate.Resilience.ToString());
            SetFreeDraftRowCell(rowObject.transform, "Sh/SvCell", candidate.ShootingOrSaving.ToString());
            SetFreeDraftRowCell(rowObject.transform, "Tac/HanCell", candidate.TacklingOrHandling.ToString());
            SetFreeDraftRowCell(rowObject.transform, "TypeCell", candidate.Type);

            FreeDraftTableRowDragHandler dragHandler = rowObject.GetComponent<FreeDraftTableRowDragHandler>();
            if (dragHandler == null)
            {
                dragHandler = rowObject.AddComponent<FreeDraftTableRowDragHandler>();
            }
            dragHandler.Configure(this, candidate.Name, candidate.IsGoalkeeper);
        }

        RectTransform contentRect = freeDraftContent as RectTransform;
        if (contentRect != null)
        {
            contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, Mathf.Max(rowHeight, rowHeight * candidates.Count));
        }
    }

    private void ClearFreeDraftRows()
    {
        if (freeDraftContent == null || freeDraftPreviewRow == null)
        {
            return;
        }

        for (int i = freeDraftContent.childCount - 1; i >= 0; i--)
        {
            Transform child = freeDraftContent.GetChild(i);
            if (child != freeDraftPreviewRow)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
        }
    }

    private void UpdateFreeDraftColumnHeaders()
    {
        Transform tableRoot = draftPanel != null ? draftPanel.transform.Find("FreeDraftTable") : null;
        Transform headerRow = tableRoot != null ? tableRoot.Find("HeaderRow") : null;
        if (headerRow == null)
        {
            return;
        }

        bool showingGoalkeepers = freeDraftPhase == FreeDraftPhase.Goalkeepers;
        SetHeaderText(headerRow, "NameHeader", FormatHeaderLabel("name", "Name"));
        SetHeaderText(headerRow, "NatHeader", FormatHeaderLabel("nationality", "Nat"));
        SetHeaderText(headerRow, "PaceHeader", FormatHeaderLabel("pace", "Pace"));
        SetHeaderText(headerRow, "DrHeader", FormatHeaderLabel("dribbling", "Dr"));
        SetHeaderText(headerRow, "H/AHeader", FormatHeaderLabel("headingAerial", showingGoalkeepers ? "Aerial" : "Heading"));
        SetHeaderText(headerRow, "HPHeader", FormatHeaderLabel("highPass", "HP"));
        SetHeaderText(headerRow, "ResHeader", FormatHeaderLabel("resilience", "Res"));
        SetHeaderText(headerRow, "Sh/SvHeader", FormatHeaderLabel("shootingSaving", showingGoalkeepers ? "Saving" : "Shooting"));
        SetHeaderText(headerRow, "Tac/HanHeader", FormatHeaderLabel("tacklingHandling", showingGoalkeepers ? "Handling" : "Tackling"));
        SetHeaderText(headerRow, "TypeHeader", FormatHeaderLabel("type", "Type"));
    }

    private string FormatHeaderLabel(string columnKey, string label)
    {
        if (!freeDraftHasUserSort || freeDraftSortKey != columnKey)
        {
            return label;
        }

        return freeDraftSortAscending ? $"{label} ^" : $"{label} v";
    }

    private void SetHeaderText(Transform headerRow, string headerName, string label)
    {
        Transform header = FindDirectChildByName(headerRow, headerName);
        TMP_Text text = header != null ? header.GetComponentInChildren<TMP_Text>() : null;
        if (text != null)
        {
            text.text = label;
        }
    }

    private void SetFreeDraftRowCell(Transform row, string cellName, string value)
    {
        Transform cell = FindDirectChildByName(row, cellName);
        TMP_Text cellText = cell != null ? cell.GetComponentInChildren<TMP_Text>() : null;
        if (cellText != null)
        {
            cellText.text = value;
        }
    }

    private Transform FindDirectChildByName(Transform parent, string childName)
    {
        if (parent == null)
        {
            return null;
        }

        foreach (Transform child in parent)
        {
            if (child.name == childName)
            {
                return child;
            }
        }

        return null;
    }

    private string AbbreviateNationality(string nationality)
    {
        if (string.IsNullOrWhiteSpace(nationality))
        {
            return string.Empty;
        }

        string trimmed = nationality.Trim();
        return trimmed.Length <= 3 ? trimmed.ToUpperInvariant() : trimmed.Substring(0, 3).ToUpperInvariant();
    }

    private PlayerSlotDropHandler ResolveGoalkeeperDestinationSlot(PlayerSlotDropHandler targetSlot)
    {
        if (targetSlot.IsGoalkeeperRosterSlot() && !targetSlot.IsSlotPopulated())
        {
            return targetSlot;
        }

        return FindNextAvailableGoalkeeperSlot(targetSlot.transform.parent.name);
    }

    private PlayerSlotDropHandler ResolveOutfielderDestinationSlot(PlayerSlotDropHandler targetSlot)
    {
        if (targetSlot.IsGoalkeeperRosterSlot())
        {
            Debug.LogWarning($"Invalid FreeDraft drop: cannot place an outfielder in goalkeeper slot {targetSlot.name}.");
            return null;
        }

        if (!targetSlot.IsSlotPopulated())
        {
            return targetSlot;
        }

        return FindNextAvailableOutfieldSlot(targetSlot.transform.parent.name, targetSlot.transform.GetSiblingIndex() + 1);
    }

    private PlayerSlotDropHandler FindNextAvailableGoalkeeperSlot(string rosterPanelName)
    {
        GameObject rosterPanel = GameObject.Find(rosterPanelName);
        if (rosterPanel == null)
        {
            return null;
        }

        foreach (Transform child in rosterPanel.transform)
        {
            PlayerSlotDropHandler slot = child.GetComponent<PlayerSlotDropHandler>();
            if (slot != null && slot.IsGoalkeeperRosterSlot() && !slot.IsSlotPopulated())
            {
                return slot;
            }
        }

        return null;
    }

    private PlayerSlotDropHandler FindNextAvailableOutfieldSlot(string rosterPanelName, int startIndex)
    {
        GameObject rosterPanel = GameObject.Find(rosterPanelName);
        if (rosterPanel == null)
        {
            return null;
        }

        int childCount = rosterPanel.transform.childCount;
        for (int offset = 0; offset < childCount; offset++)
        {
            int childIndex = (Mathf.Max(0, startIndex) + offset) % childCount;
            Transform child = rosterPanel.transform.GetChild(childIndex);
            PlayerSlotDropHandler slot = child.GetComponent<PlayerSlotDropHandler>();
            if (slot != null && !slot.IsGoalkeeperRosterSlot() && !slot.IsSlotPopulated())
            {
                return slot;
            }
        }

        return null;
    }

    private bool AreAllGoalkeeperSlotsFilled()
    {
        return CountEmptyGoalkeeperSlots(homeTeamPanel.transform) == 0 &&
               CountEmptyGoalkeeperSlots(awayTeamPanel.transform) == 0;
    }

    private bool AreRostersFull()
    {
        return CountEmptyRosterSlots(homeTeamPanel.transform) == 0 &&
               CountEmptyRosterSlots(awayTeamPanel.transform) == 0;
    }

    private int CountEmptyGoalkeeperSlots(Transform rosterPanel)
    {
        int emptyCount = 0;
        foreach (Transform child in rosterPanel)
        {
            PlayerSlotDropHandler slot = child.GetComponent<PlayerSlotDropHandler>();
            if (slot != null && slot.IsGoalkeeperRosterSlot() && !slot.IsSlotPopulated())
            {
                emptyCount++;
            }
        }

        return emptyCount;
    }

    private int CountEmptyOutfieldSlots(Transform rosterPanel)
    {
        int emptyCount = 0;
        foreach (Transform child in rosterPanel)
        {
            PlayerSlotDropHandler slot = child.GetComponent<PlayerSlotDropHandler>();
            if (slot != null && !slot.IsGoalkeeperRosterSlot() && !slot.IsSlotPopulated())
            {
                emptyCount++;
            }
        }

        return emptyCount;
    }

    private int CountEmptyRosterSlots(Transform rosterPanel)
    {
        int emptyCount = 0;
        foreach (Transform child in rosterPanel)
        {
            PlayerSlotDropHandler slot = child.GetComponent<PlayerSlotDropHandler>();
            if (slot != null && !slot.IsSlotPopulated())
            {
                emptyCount++;
            }
        }

        return emptyCount;
    }

    private DraftUIManager GetDraftUIManager()
    {
        if (draftUIManager == null)
        {
            draftUIManager = FindAnyObjectByType<DraftUIManager>();
        }

        return draftUIManager;
    }

    private void RefreshDraftUI()
    {
        DraftUIManager uiManager = GetDraftUIManager();
        if (uiManager != null)
        {
            uiManager.RefreshDraftStateUI();
        }
    }

    private struct FreeDraftCandidateView
    {
        public string Name;
        public string Nationality;
        public int Pace;
        public int Dribbling;
        public int HeadingOrAerial;
        public int HighPass;
        public int Resilience;
        public int ShootingOrSaving;
        public int TacklingOrHandling;
        public string Type;
        public bool IsGoalkeeper;

        public static FreeDraftCandidateView FromGoalkeeper(Goalkeeper goalkeeper)
        {
            return new FreeDraftCandidateView
            {
                Name = goalkeeper.Name,
                Nationality = goalkeeper.Country,
                Pace = goalkeeper.Pace,
                Dribbling = goalkeeper.Dribbling,
                HeadingOrAerial = goalkeeper.Aerial,
                HighPass = goalkeeper.HighPass,
                Resilience = goalkeeper.Resilience,
                ShootingOrSaving = goalkeeper.Saving,
                TacklingOrHandling = goalkeeper.Handling,
                Type = goalkeeper.Type,
                IsGoalkeeper = true
            };
        }

        public static FreeDraftCandidateView FromPlayer(Player player)
        {
            return new FreeDraftCandidateView
            {
                Name = player.Name,
                Nationality = player.Country,
                Pace = player.Pace,
                Dribbling = player.Dribbling,
                HeadingOrAerial = player.Heading,
                HighPass = player.HighPass,
                Resilience = player.Resilience,
                ShootingOrSaving = player.Shooting,
                TacklingOrHandling = player.Tackling,
                Type = player.Type,
                IsGoalkeeper = false
            };
        }
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

                // Team Average should include every non-goalkeeper squad slot, including larger benches.
                // That keeps 18-player squads honest by counting slots 17 and 18 as well.
                if (i >= 2 && i <= squadSize && i != 12)
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

// Helper class to match the JSON structure
[System.Serializable]
public class RootGameSettings
{
    public GameSettings gameSettings; // This matches the "gameSettings" node in the JSON
}
