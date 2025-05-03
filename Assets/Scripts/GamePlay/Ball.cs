using UnityEngine;
using System.Collections;
// using System.Diagnostics;

public class Ball : MonoBehaviour
{
    [SerializeField]
    private HexCell currentCell;
    private HexCell targetCell;
    public bool isMoving = false;
    private bool isBallSelected = false;  // Track if the ball is selected
    public HexGrid hexGrid;  // Reference to HexGrid to access grid cells
    public GoalKeeperManager goalKeeperManager;
    [SerializeField] public float ballRadius = 0.6474f;
    private bool playersInstantiated = false;  // New flag to track when players are ready
    public float groundHeightOffset = 0.2f;  // Height when ball is on the ground
    public float playerHeightOffset = 0.5f;  // Height when ball is on a player

    IEnumerator Start()
    {
        // Wait until the grid is fully initialized
        yield return new WaitUntil(() => hexGrid != null && hexGrid.IsGridInitialized());
        // Subscribe to the event to know when players are instantiated
        MatchManager.Instance.OnPlayersInstantiated += PlayersReady;  // Subscribe to event

        // Wait for the players to be instantiated
        yield return new WaitUntil(() => playersInstantiated);
        // Now the grid is initialized, place the ball at the initial hex
        HexCell startingHex = hexGrid.GetHexCellAt(new Vector3Int(0, 0, 0));

        if (startingHex != null)
        {
            PlaceAtCell(startingHex);
            Debug.Log("Ball placed at initial hex: " + startingHex.coordinates);
        }
        else
        {
            Debug.LogError("Starting hex is null!");
        }
        AdjustBallHeightBasedOnOccupancy();
    }

    // This method is called when players are ready
    private void PlayersReady()
    {
        playersInstantiated = true;  // Set the flag to true when players are ready
    }

    // Adjust the ball height based on player occupancy
    public void AdjustBallHeightBasedOnOccupancy()
    {
        if (currentCell == null)
        {
            return; // No current cell to check
        }

        // Check if the hex is occupied by a player (attacker or defender)
        float yOffset = groundHeightOffset;  // Default height on the ground
        // Debug.Log("Ball Cell isAttackOccupied: " + currentCell.isAttackOccupied + " or  isDefenseOccupied: " + currentCell.isDefenseOccupied);
        if (currentCell.isAttackOccupied || currentCell.isDefenseOccupied)
        {
            yOffset = playerHeightOffset;  // Lift the ball when it's on a player token
        }

        // Set the ball's position
        Vector3 newPosition = new Vector3(currentCell.GetHexCenter().x, yOffset, currentCell.GetHexCenter().z);
        transform.position = newPosition;
        // Debug.Log("Ball position adjusted to: " + yOffset);

    }

    public void PlaceAtCell(HexCell cell)
    {
        if (cell == null)
        {
            Debug.LogError("Trying to place the ball at a null hex!");
            return;
        }

        currentCell = cell;  // Set the current hex to the given cell
        // transform.position = cell.GetHexCenter();  // Place at the center of the hex
        AdjustBallHeightBasedOnOccupancy();
    }

    public IEnumerator MoveToCell(HexCell newHex, int? roll = null)
    {
        if (newHex == null)
        {
            Debug.LogError("Target Hex is null in MoveToCell!");
            yield break;
        }
        // Check if target is out-of-bounds
        bool isOutOfBounds = newHex.isOutOfBounds;
        // Adjust speed based on shot roll
        float baseSpeed = 3.0f;  // Default ball speed
        float speedMultiplier = 3.5f;  // Scaling factor for shot power influence
        float adjustedSpeed = roll.HasValue ? baseSpeed + (roll.Value * speedMultiplier) : baseSpeed;

        targetCell = newHex;
        isMoving = true;
        int previousPenaltyBoxStatus = hexGrid.CheckPenaltyBox(transform.position);
        bool isFromGoal = GetCurrentHex().isInGoal != 0;
        // Move smoothly towards the target cell
        while (isMoving)
        {
            float step = adjustedSpeed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, targetCell.GetHexCenter(), step);
            // ✅ Detect transition into penalty box
            int currentPenaltyBoxStatus = hexGrid.CheckPenaltyBox(transform.position);
            if (previousPenaltyBoxStatus == 0 && currentPenaltyBoxStatus != 0)
            {
                Debug.Log("⚽ Ball just entered the penalty box! Checking if GK should move.");
                Vector3Int currentHexCoords = hexGrid.WorldToHexCoords(transform.position);
                HexCell currentHex = hexGrid.GetHexCellAt(currentHexCoords);
                if (currentHex == null)
                {
                    Debug.LogError("Ball is in an invalid hex cell!");
                    yield break;
                }
                // ✅ Check if GK should move
                if (
                    goalKeeperManager.ShouldGKMove(currentHex) // check if we're eligible for move
                    && roll == null // this is not called from Shot Manager
                    && !isFromGoal
                )
                {
                    Debug.Log("🛑 GK move triggered! Pausing ball.");
                    isMoving = false;
                    // ✅ Store paused position before GK move
                    Vector3 pausedPosition = transform.position;
                    yield return StartCoroutine(goalKeeperManager.HandleGKFreeMove());
                    // ✅ Restore ball's paused position after GK move
                    transform.position = pausedPosition;
                    isMoving = true; // Resume movement
                }
            }
            // Update previous status
            previousPenaltyBoxStatus = currentPenaltyBoxStatus;
            // Check if the ball has reached the target
            if (Vector3.Distance(transform.position, targetCell.GetHexCenter()) < 0.001f)
            {
                transform.position = targetCell.GetHexCenter();  // Snap exactly to the hex center
                isMoving = false;  // Stop the movement
            }

            yield return null;  // Wait for the next frame
        }
        currentCell = targetCell;  // Update current cell
        targetCell = null;         // Clear target cell
    }

    public HexCell GetCurrentHex()
    {
        return currentCell;
    }
    
    public HexCell SetCurrentHex(HexCell newHex)
    {
        return currentCell = newHex;
    }

    private void OnMouseDown()
    {
        if (!isBallSelected)
        {
            // Debug.Log("Ball selected! Now click on a hex to move it.");
            isBallSelected = true;  // Mark ball as selected when clicked
        }
    }

    public bool IsBallSelected()
    {
        return isBallSelected;
    }

    public void SelectBall()
    {
        isBallSelected = true;
    }
    
    public void DeselectBall()
    {
        isBallSelected = false;
    }

}
