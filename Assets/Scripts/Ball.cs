using UnityEngine;
using System.Collections;

public class Ball : MonoBehaviour
{
    private HexCell currentCell;
    private HexCell targetCell;
    public bool isMoving = false;
    private float moveSpeed = 2f;  // Speed: 2 hexes per second
    // [SerializeField] private float moveSpeed = 0.5f;
    private bool isBallSelected = false;  // Track if the ball is selected
    public HexGrid hexGrid;  // Reference to HexGrid to access grid cells
    [SerializeField] public float ballRadius = 0.6474f;

    // Coroutine to ensure ball is placed after grid initialization
    IEnumerator Start()
    {
        // Wait until the grid is fully initialized
        yield return new WaitUntil(() => hexGrid != null && hexGrid.IsGridInitialized());

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
    }

    // Place the ball at the initial cell
    public void PlaceAtCell(HexCell cell)
    {
        if (cell == null)
        {
            Debug.LogError("Trying to place the ball at a null hex!");
            return;
        }

        currentCell = cell;  // Set the current hex to the given cell
        transform.position = cell.GetHexCenter();  // Place at the center of the hex
    }

    // Coroutine to move the ball to a new hex smoothly
    public IEnumerator MoveToCell(HexCell newHex)
    {
        if (newHex == null)
        {
            Debug.LogError("Target Hex is null in MoveToCell!");
            yield break;
        }

        targetCell = newHex;
        isMoving = true;

        // Move smoothly towards the target cell
        while (isMoving)
        {
            float step = moveSpeed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, targetCell.GetHexCenter(), step);

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

    // Method to get the ball's current hex
    public HexCell GetCurrentHex()
    {
        return currentCell;
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
    public void SelectBall()
    {
        isBallSelected = true;
    }
    // Call this to manually deselect the ball
    public void DeselectBall()
    {
        isBallSelected = false;
    }
}
