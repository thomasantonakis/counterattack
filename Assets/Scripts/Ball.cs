using UnityEngine;

public class Ball : MonoBehaviour
{
    private HexCell currentCell;
    private HexCell targetCell;
    private bool isMoving = false;
    private float moveSpeed = 2f;  // 2 hexes per second
    private bool isBallSelected = false;  // Track if the ball is selected

    public void PlaceAtCell(HexCell cell)
    {
        currentCell = cell;
        transform.position = cell.transform.position;  // Start at the center of the hex
    }

    void Update()
    {
        if (isMoving)
        {
            MoveTowardsTarget();
        }
    }

    public void MoveToCell(HexCell destination)
    {
        targetCell = destination;
        isMoving = true;
        isBallSelected = false;  // Deselect the ball after setting the destination
        Debug.Log($"Moving ball to: {destination.coordinates}");  // Debug log for ball movement
    }

    void MoveTowardsTarget()
    {
        if (targetCell != null)
        {
            // Move the ball from current position to the target hex
            transform.position = Vector3.MoveTowards(transform.position, targetCell.transform.position, moveSpeed * Time.deltaTime);
            // Check if the ball has reached the target cell
            if (Vector3.Distance(transform.position, targetCell.transform.position) < 0.01f)
            {
                currentCell = targetCell;  // Update the current cell
                targetCell = null;
                isMoving = false;  // Stop moving
                Debug.Log("Ball has reached the target.");
            }
        }
    }

    private void OnMouseDown()
    {
        if (!isBallSelected)
        {
            Debug.Log("Ball selected! Now click on a hex to move it.");
            isBallSelected = true;  // Mark ball as selected when clicked
        }
        // // Move the ball to a test destination (for now, move to the top-right hex)
        // HexCell destination = FindObjectOfType<HexGrid>().GetHexCellAt(new Vector3Int(1, 0, 0));  // Test destination
        // if (destination != null)
        // {
        //     MoveToCell(destination);  // Move the ball to the destination hex
        // }
    }

    // Check if the ball is currently selected
    public bool IsBallSelected()
    {
        return isBallSelected;
    }

    // Call this to manually deselect the ball (optional)
    public void DeselectBall()
    {
        isBallSelected = false;
    }

}
