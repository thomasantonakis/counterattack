using UnityEngine;
using System.Collections;

public class Ball : MonoBehaviour
{
    private HexCell currentCell;
    private HexCell targetCell;
    private bool isMoving = false;
    private float moveSpeed = 2f;  // Speed: 2 hexes per second
    private bool isBallSelected = false;  // Track if the ball is selected
    public HexGrid hexGrid;  // Reference to HexGrid to access grid cells


    // Coroutine to ensure ball is placed after grid initialization
    IEnumerator Start()
    {
        // Wait until the grid is fully initialized
        yield return new WaitUntil(() => hexGrid != null && hexGrid.IsGridInitialized());
        // Debug.Log("Ball Start method is executing.");

        // Now the grid is initialized, place the ball at the initial hex
        HexCell startingHex = hexGrid.GetHexCellAt(new Vector3Int(0, 0, 0));

        if (startingHex != null)
        {
            // PlaceAtCell(startingHex, hexGrid.gridCenter);
            PlaceAtCell(startingHex);
            Debug.Log("Ball placed at initial hex: " + startingHex.coordinates);
        }
        else
        {
            Debug.LogError("Starting hex is null!");
        }
    }
    // Place the ball at the initial cell
    // public void PlaceAtCell(HexCell cell, Vector3 gridCenter)
    public void PlaceAtCell(HexCell cell)
    {
        if (cell == null)
        {
            Debug.LogError("Trying to place the ball at a null hex!");
            return;
        }

        currentCell = cell;  // Set the current hex to the given cell
        // transform.position = cell.transform.position + gridCenter;  // Adjust position with gridCenter
        transform.position = cell.transform.position;  // Adjust position with gridCenter
    }

    // Update method to handle smooth movement
    void Update()
    {
        if (isMoving)
        {
            MoveTowardsTarget();
        }
    }

    // Method to get the ball's current hex
    public HexCell GetCurrentHex()
    {
        return currentCell;
    }

    // Method to move the ball to a new hex smoothly
    public void MoveToCell(HexCell newHex)
    {
        if (newHex == null)
        {
            Debug.LogError("Target Hex is null in MoveToCell!");
            return;
        }

        // Set the target cell for smooth movement
        targetCell = newHex;
        isMoving = true;  // Start the movement
    }

    // Handles the movement towards the target hex
    void MoveTowardsTarget()
    {
        if (targetCell != null)
        {
            // Move the ball smoothly towards the target cell
            transform.position = Vector3.MoveTowards(transform.position, targetCell.transform.position, moveSpeed * Time.deltaTime);

            // Check if the ball has reached the target cell
            if (Vector3.Distance(transform.position, targetCell.transform.position) < 0.01f)
            {
                currentCell = targetCell;  // Update the current cell once the ball arrives
                targetCell = null;  // Clear the target cell
                isMoving = false;  // Stop movement
                Debug.Log("Ball has reached the target.");
            }
        }
    }

    // OnMouseDown event for selecting the ball
    private void OnMouseDown()
    {
        if (!isBallSelected)
        {
            Debug.Log("Ball selected! Now click on a hex to move it.");
            isBallSelected = true;  // Mark ball as selected when clicked
        }
    }

    // Check if the ball is currently selected
    public bool IsBallSelected()
    {
        return isBallSelected;
    }

    // Call this to manually deselect the ball
    public void DeselectBall()
    {
        isBallSelected = false;
    }
}
