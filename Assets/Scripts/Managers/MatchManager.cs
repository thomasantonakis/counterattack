using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;  // For JsonConvert

public class MatchManager : MonoBehaviour
{
    // Define the possible game states
    public enum GameState
    {
        KickOffSetup, // Free movements of Players in each own Half
        KickoffBlown, // Only a Standard Pass is available
        StandardPassAttempt, // Attacking Team calls a Standard Pass-11
        StandardPassMoving, // Ball is moving to either the intercepting Def or the Destination
        StandardPassCompletedToPlayer, // A standard Pass was not intercepted and is on an Attacker
        StandardPassCompletedToSpace, // A standard Pass was not intercepted and is on on a Free Hex
        LongBallAttempt,
        LongPassMoving,
        LongBallCompleted,
        WaitingForThrowInTaker, // An attacker must be chosen to take the throw in
        WaitingForCornerTaker, // An attacker must be chosen to take the Corner Kick
        WaitingForGoalKickFinalThirds, // Both Final Thirds Can Move
        LooseBallPickedUp, // Any type of Loose ball picked up by an outfielder
        MovementPhaseAttack,
        MovementPhaseDef,
        MovementPhase2f2,
        MovementPhaseEnded,
        SuccessfulTackle,
        HighPassAttempt,
        HighPassMoving,
        HighPassCompleted
    }
    public event Action OnGameSettingsLoaded;
    public event Action OnPlayersInstantiated;
    public enum TeamInAttack
    {
        Home,
        Away
    }
    public enum TeamAttackingDirection
    {
        LeftToRight,
        RightToLeft
    }
    public TeamAttackingDirection homeTeamDirection;
    public TeamAttackingDirection awayTeamDirection;

    public GameState currentState; // Tracks the current state of the match
    public TeamInAttack teamInAttack; // Tracks which team is in Attack
    public bool attackHasPossession; 
    // Singleton instance for easy access
    public static MatchManager Instance;
    public Ball ball;  // Reference to the ball
    public HexGrid hexGrid;  // Reference to the ball
    public GameData gameData;
    public int difficulty_level;

    // // Define other match-specific variables here (e.g., time, score, teams)
    // private int homeScore = 0;
    // private int awayScore = 0;

    private void Awake()
    {
        // Set up the singleton instance
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject); // Ensure there is only one MatchManager
        }
        // Initialize gameData if it's not set already
        gameData ??= new GameData();
    }

    IEnumerator Start()
    {
        LoadGameSettingsFromJson();
        if (gameData != null && gameData.gameSettings != null)
        {
            difficulty_level = gameData.gameSettings.playerAssistance;
        }
        // Wait until the grid is fully initialized
        yield return new WaitUntil(() => hexGrid != null && hexGrid.IsGridInitialized());
        // Initialize the match in the KickOffSetup state
        currentState = GameState.KickOffSetup;
        Debug.Log("Game initialized in KickOffSetup state.");
        // Initialize the attacking team and direction
        teamInAttack = TeamInAttack.Home;  // Home team starts with the ball
        homeTeamDirection = TeamAttackingDirection.LeftToRight;  // Set home team attacking direction to LeftToRight
        awayTeamDirection = TeamAttackingDirection.RightToLeft;  // Away team will attack in the opposite direction
    }

    private void Update()
    {
        // // Handle state transitions or global inputs, like starting the match
        // if (currentState == GameState.KickOffSetup && Input.GetKeyDown(KeyCode.Space))
        // {
        //     StartMatch();
        // }

        // if (currentState == GameState.StandardPassAttempt)
        // {
        //     // Here you could manage inputs like the player selecting a pass target (by clicking a hex)
        //     // You can also trigger transitions between game states based on player actions
        // }
    }

    // Call this when players are fully instantiated
    public void NotifyPlayersInstantiated()
    {
        if (OnPlayersInstantiated != null)
        {
            OnPlayersInstantiated.Invoke();
        }
    }

    // Example method to start the match
    public void StartMatch()
    {
        currentState = GameState.KickoffBlown;
        // Start the timer or wait for the next Action to be called to start it.
        Debug.Log("Match Kicked Off. Awaiting for Attacking Team to call an action");
        // Logic to start the game, such as showing the ball, enabling inputs, etc.
    }

    public void SwitchSides()
    {
        // Swap the attacking directions for both teams
        if (homeTeamDirection == TeamAttackingDirection.LeftToRight)
        {
            homeTeamDirection = TeamAttackingDirection.RightToLeft;
            awayTeamDirection = TeamAttackingDirection.LeftToRight;
        }
        else
        {
            homeTeamDirection = TeamAttackingDirection.LeftToRight;
            awayTeamDirection = TeamAttackingDirection.RightToLeft;
        }

        // Log the switch for debugging
        Debug.Log("Sides switched. Home team is now attacking " + homeTeamDirection + " and away team is attacking " + awayTeamDirection);
    }

    public void ChangePossession()
    {
        // Switch the team in attack
        if (teamInAttack == TeamInAttack.Home)
        {
            teamInAttack = TeamInAttack.Away;
        }
        else
        {
            teamInAttack = TeamInAttack.Home;
        }
        // Loop through all hexes and swap attacker/defender status
        foreach (HexCell hex in hexGrid.cells)
        {
            // Swap isAttackOccupied and isDefenseOccupied
            bool temp = hex.isAttackOccupied;
            hex.isAttackOccupied = hex.isDefenseOccupied;
            hex.isDefenseOccupied = temp;

            // Update the highlights during development
            if (hex.isAttackOccupied)
            {
                hex.HighlightHex("isAttackOccupied");  // Use a distinct color for attackers
            }
            else if (hex.isDefenseOccupied)
            {
                hex.HighlightHex("isDefenseOccupied");  // Use a distinct color for defenders
            }
            else
            {
                hex.ResetHighlight();  // Reset to normal if neither
            }
        }
        // Now update the PlayerTokens to reflect the new possession
        UpdatePlayerTokensAfterPossessionChange();

        Debug.Log($"Possession changed! {teamInAttack} now is the Attacking Team.");
    }

    public void UpdatePossessionAfterPass(HexCell ballHex)
    {
        List<HexCell> attackerHexes = hexGrid.GetAttackerHexes();

        // Check if the ball is on an attacker's hex
        if (attackerHexes.Contains(ballHex))
        {
            attackHasPossession = true;
            // Debug.Log("Attacking team retains possession.");
        }
        else
        {
            attackHasPossession = false;
            // Debug.Log("Attacking team lost possession.");
        }
    }

    // Method to trigger the standard pass attempt mode (on key press, like "P")
    public void TriggerStandardPass()
    {
        if (
            currentState == GameState.StandardPassMoving ||
            currentState == GameState.StandardPassAttempt ||
            // currentState == GameState.StandardPassCompleted || // Development Mode
            currentState == GameState.KickOffSetup
        ) // in not available
        {
            Debug.LogWarning("Cannot start pass attempt from current state: " + currentState);
        }
        else
        {
            currentState = GameState.StandardPassAttempt;
            ball.SelectBall();
            Debug.Log("Standard pass attempt mode activated.");
        }
    }
    public void TriggerMovement()
    {
        if (
            currentState == GameState.StandardPassMoving ||
            currentState == GameState.KickOffSetup ||
            currentState == GameState.KickoffBlown
        )  // Not available in current situation
        {
            Debug.LogWarning("Cannot start Movement Phase from current state: " + currentState);
        }
        // else if ( currentState != GameState.KickoffBlown ) // High diff and Something Else is selected
        // {
        //     Debug.LogWarning("Movement Not Available. You have already called something else");
        // }
        else // low diff
        {
            currentState = GameState.MovementPhaseAttack;
            Debug.Log("Attacking Movement Phase started.");
        }
    }

    public void StartMovementPhaseDef()
    {
        currentState = GameState.MovementPhaseDef;
        Debug.Log("Defensive Movement Phase Activated");
    }

    public void StartMovementPhase2f2()
    {
        currentState = GameState.MovementPhase2f2;
        Debug.Log("Movement Phase 2f2 Activated. Two attackers can move up to 2 hexes.");
    }

    public void TriggerHighPass()
    {
        if (true)
        {
            currentState = GameState.HighPassAttempt;
            ball.SelectBall();
            Debug.Log("High Pass attempt mode activated.");
        }
        else
        {
        }
    }
    public void TriggerLongPass()
    {
        if (true)
        {
            currentState = GameState.LongBallAttempt;
            ball.SelectBall();
            Debug.Log("Long ball attempt mode activated.");
        }
        else
        {
        }
    }
    public void TriggerShot()
    {
        // if (currentState == GameState.KickoffBlown)
        if (true)
        {
        }
        else
        {
        }
    }
    public void TriggerHeader()
    {
        // if (currentState == GameState.KickoffBlown)
        if (true)
        {
        }
        else
        {
        }
    }
    public void TriggerFTP()
    {
        // if (currentState == GameState.KickoffBlown)
        if (true)
        {
        }
        else
        {
        }
    }

    private void UpdatePlayerTokensAfterPossessionChange()
    {
        // Loop through all tokens in the game
        PlayerToken[] allTokens = FindObjectsOfType<PlayerToken>();  // Find all tokens in the scene

        foreach (PlayerToken token in allTokens)
        {
            // Determine if the token is now an attacker or a defender
            if ((teamInAttack == TeamInAttack.Home && token.isHomeTeam) ||
                (teamInAttack == TeamInAttack.Away && !token.isHomeTeam))
            {
                token.isAttacker = true;  // Set to attacker if the token's team is now in attack
            }
            else
            {
                token.isAttacker = false;  // Set to defender otherwise
            }
        }

        Debug.Log("Player tokens updated after possession change.");
    }

    // Add other match-related methods here (like handling goals, score updates, etc.)

    public void LoadGameSettingsFromJson()
    {
        // string path = Path.Combine(Application.persistentDataPath, "2024-09-29_02-48__Hot Seat__AS Roma__Aurora FC.json");
        string path = Path.Combine(Application.persistentDataPath, "2024-10-01_00-10__Hot Seat__AS Roma__Aurora FC.json");

        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            
            // Deserialize the JSON into the GameData class
            gameData = JsonConvert.DeserializeObject<GameData>(json);

            if (gameData != null && gameData.gameSettings != null)
            {
                Debug.Log($"Loaded Home Team: {gameData.gameSettings.homeTeamName}");
                Debug.Log($"Loaded Away Team: {gameData.gameSettings.awayTeamName}");
                Debug.Log($"Loaded Home Kit: {gameData.gameSettings.homeKit}");
                Debug.Log($"Loaded Away Kit: {gameData.gameSettings.awayKit}");
                // Trigger event or call LoadTeamNames after settings are loaded
                OnGameSettingsLoaded?.Invoke();
            }
            else
            {
                Debug.LogError("Failed to load game settings from the file!");
            }
        }
        else
        {
            Debug.LogWarning("Game settings file not found.");
        }
    }

    public class GameData
    {
        public GameSettings gameSettings;
    }

    public class GameSettings
    {
        public string gameMode;
        public string homeTeamName;
        public string awayTeamName;
        public string homeKit;
        public string awayKit;
        public int playerAssistance;
        // Add other game settings properties as needed
    }

}
