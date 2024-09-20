using UnityEngine;

public class MatchManager : MonoBehaviour
{
    // Define the possible game states
    public enum GameState
    {
        KickOffSetup, // Free movements of Players in each own Half
        KickoffBlown, // Generic state to show that we are accepting
        StandardPassAttempt, // Attacking Team calls a Standard Pass-11
        // Add other game states as needed
        // e.g., GoalScored, FreeKick, EndMatch, etc.
    }

    public GameState currentState; // Tracks the current state of the match

    // Singleton instance for easy access
    public static MatchManager Instance;

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
    }

    private void Start()
    {
        // Initialize the match in the KickOffSetup state
        currentState = GameState.KickOffSetup;
        Debug.Log("Game initialized in KickOffSetup state.");
    }

    private void Update()
    {
        // Handle state transitions or global inputs, like starting the match
        if (currentState == GameState.KickOffSetup && Input.GetKeyDown(KeyCode.Space))
        {
            StartMatch();
        }

        if (currentState == GameState.StandardPassAttempt)
        {
            // Here you could manage inputs like the player selecting a pass target (by clicking a hex)
            // You can also trigger transitions between game states based on player actions
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

    // Method to trigger the standard pass attempt mode (on key press, like "P")
    public void TriggerStandardPass()
    {
        if (currentState == GameState.KickoffBlown)
        {
            currentState = GameState.StandardPassAttempt;
            Debug.Log("Standard pass attempt mode activated.");
        }
        else
        {
            Debug.LogWarning("Cannot start pass attempt from current state: " + currentState);
        }
    }
    public void TriggerMovement()
    {
        // if (currentState == GameState.KickoffBlown)
        if (true)
        {
        }
        else
        {
        }
    }
    public void TriggerHighPass()
    {
        // if (currentState == GameState.KickoffBlown)
        if (true)
        {
        }
        else
        {
        }
    }
    public void TriggerLongPass()
    {
        // if (currentState == GameState.KickoffBlown)
        if (true)
        {
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

    // Add other match-related methods here (like handling goals, score updates, etc.)
}
