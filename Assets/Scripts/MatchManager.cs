using UnityEngine;
using System.Collections;

public class MatchManager : MonoBehaviour
{
    // Define the possible game states
    public enum GameState
    {
        KickOffSetup, // Free movements of Players in each own Half
        KickoffBlown, // Only a Standard Pass is available
        StandardPassAttempt, // Attacking Team calls a Standard Pass-11
        StandardPassMoving, // Ball is moving to either the intercepting Def or the Destination
        StandardPassCompleted,
        LongBallAttempt,
        LongPassMoving, // Ball is moving to either the intercepting Def or the Destination
        LongPassCompleted,
    }

    public GameState currentState; // Tracks the current state of the match

    // Singleton instance for easy access
    public static MatchManager Instance;
    public Ball ball;  // Reference to the ball
    public HexGrid hexGrid;  // Reference to the ball
    // public int difficulty_level = 1; // low
    // public int difficulty_level = 2; // medium
    public int difficulty_level; // high

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

    IEnumerator Start()
    {
        // Wait until the grid is fully initialized
        yield return new WaitUntil(() => hexGrid != null && hexGrid.IsGridInitialized());
        // Initialize the match in the KickOffSetup state
        currentState = GameState.KickOffSetup;
        Debug.Log("Game initialized in KickOffSetup state.");
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
        else if ( currentState == GameState.StandardPassCompleted ) // High diff and Something Else is selected
        {
            Debug.LogWarning("Movement Not Available. You have already called something else");
        }
        else // low diff
        {
            Debug.LogWarning("Movement Phase Activated");
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

    // Add other match-related methods here (like handling goals, score updates, etc.)
}