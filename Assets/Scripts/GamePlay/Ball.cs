using UnityEngine;
using System.Collections;

public class Ball : MonoBehaviour
{
    private HexCell currentCell;
    private HexCell targetCell;
    public bool isMoving = false;
    private float moveSpeed = 2f;  // Speed: 2 hexes per second
    private bool isBallSelected = false;  // Track if the ball is selected
    public HexGrid hexGrid;  // Reference to HexGrid to access grid cells
    [SerializeField] public float ballRadius = 0.6474f;

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

    public HexCell GetCurrentHex()
    {
        return currentCell;
    }

    private void OnMouseDown()
    {
        if (!isBallSelected)
        {
            Debug.Log("Ball selected! Now click on a hex to move it.");
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
