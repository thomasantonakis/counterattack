using UnityEngine;
using System.Collections.Generic;

public class GameInputManager : MonoBehaviour
{
    public CameraController cameraController;  // Reference to the camera controller
    public Ball ball;  // Reference to the ball
    public HexGrid hexGrid;  // Add a reference to the HexGrid


    // List to store highlighted hexes
    private List<HexCell> highlightedHexes = new List<HexCell>();
    private HexCell currentTargetHex = null;   // The currently selected target hex
    private HexCell lastClickedHex = null;     // The last hex that was clicked

    // Variables to track mouse movement for dragging
    private Vector3 mouseDownPosition;  // Where the mouse button was pressed
    private bool isDragging = false;    // Whether a drag is happening
    public float dragThreshold = 10f;   // Sensitivity to detect dragging vs. clicking (in pixels)

    void Start()
    {

    }
    void Update()
    {
        // Always handle camera movement with the keyboard, regardless of mouse input
        cameraController.HandleCameraInput();

        HandleMouseInput();
    }

    void HandleMouseInput()
    {
        // When the left mouse button is pressed down
        if (Input.GetMouseButtonDown(0))
        {
            mouseDownPosition = Input.mousePosition;  // Store the initial mouse position
            isDragging = false;  // Reset dragging flag
        }

        // While the left mouse button is held down
        if (Input.GetMouseButton(0))
        {
            // Check if mouse movement exceeds the drag threshold
            if (!isDragging && Vector3.Distance(mouseDownPosition, Input.mousePosition) > dragThreshold)
            {
                isDragging = true;  // Consider this a drag
            }

            // If dragging, handle camera movement
            if (isDragging)
            {
                cameraController.HandleCameraInput();  // Move the camera
            }
        }

        // When the left mouse button is released
        if (Input.GetMouseButtonUp(0))
        {
            if (!isDragging && ball.IsBallSelected())
            {
                // Handle ball movement or path highlighting
                HandleBallPath();
            }
            // Reset dragging state
            isDragging = false;
        }
    }

    void HandleBallPath()
    {
        // Cast a ray to detect the clicked hex
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            HexCell clickedHex = hit.collider.GetComponent<HexCell>();
            if (clickedHex != null)
            {
                // Get ball's current hex and handle null cases
                HexCell ballHex = ball.GetCurrentHex();
                if (ballHex == null)
                {
                    Debug.LogError("Ball's current hex is null! Ensure the ball has been placed on the grid.");
                    return;
                }
                if (clickedHex == currentTargetHex && clickedHex == lastClickedHex)
                {
                    // Double click on the same hex: confirm the move
                    ball.MoveToCell(currentTargetHex);  // Start ball movement with animation
                    ClearHighlightedHexes();            // Clear highlights
                    ball.DeselectBall();                // Deselect the ball after the move
                }
                else
                {
                    // First or new click on a different hex: highlight the path
                    ClearHighlightedHexes();            // Clear previous highlights
                    HighlightPathToHex(clickedHex);     // Highlight new path
                    currentTargetHex = clickedHex;      // Set the new target hex
                }
                // Update the last clicked hex for tracking double-clicks
                lastClickedHex = clickedHex;
            }
        }
    }

    void HighlightPathToHex(HexCell targetHex)
    {
        HexCell ballHex = ball.GetCurrentHex();  // Get the current hex of the ball
        // Null check for ballHex
        if (ballHex == null)
        {
            Debug.LogError("Ball's current hex is null! Ensure the ball has a valid hex.");
            return;
        }
        ClearHighlightedHexes();
        List<HexCell> pathHexes = CalculateHexPath(ballHex, targetHex);  // Calculate the path between the ball and the target hex
        // Prepare a string to hold the coordinates for logging
        string hexCoordinatesLog = "Highlighted Path: ";
        foreach (HexCell hex in pathHexes)
        {
            if (hex == null)
            {
                Debug.LogError("A hex in the path is null! Check the path calculation.");
                continue;
            }
            hex.HighlightHex();    // Assuming there's a method in HexCell to highlight the hex
            highlightedHexes.Add(hex);  // Keep track of highlighted hexes
            // Append the hex coordinates to the log string
            hexCoordinatesLog += $"({hex.coordinates.x}, {hex.coordinates.z}), ";
        }
        // Remove the trailing comma and space, and log the coordinates in one line
        if (hexCoordinatesLog.Length > 0)
        {
            hexCoordinatesLog = hexCoordinatesLog.TrimEnd(new char[] { ',', ' ' });
            Debug.Log(hexCoordinatesLog);
        }

    }

    List<HexCell> CalculateHexPath(HexCell startHex, HexCell endHex)
    {
        List<HexCell> path = new List<HexCell>();

        // Ensure both start and end hexes are valid
        if (startHex == null || endHex == null)
        {
            Debug.LogError("Start or End hex is null in path calculation!");
            return path;
        }

        Vector3Int startCoords = startHex.coordinates;
        Vector3Int endCoords = endHex.coordinates;
        // Add start hex to the path
        path.Add(startHex);
        // Calculate the straight-line path (Bresenham-like line algorithm for hexes)
        int dx = endCoords.x - startCoords.x;
        int dz = endCoords.z - startCoords.z;
        int steps = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dz));
        float stepX = dx / (float)steps;
        float stepZ = dz / (float)steps;
        float currentX = startCoords.x;
        float currentZ = startCoords.z;
        // Walk through the line from start to end hex
        for (int i = 1; i <= steps; i++)
        {
            currentX += stepX;
            currentZ += stepZ;

            // Round to the nearest hex cell coordinates
            Vector3Int roundedCoords = new Vector3Int(Mathf.RoundToInt(currentX), 0, Mathf.RoundToInt(currentZ));
            HexCell nextHex = hexGrid.GetHexCellAt(roundedCoords);

            if (nextHex != null && !path.Contains(nextHex))
            {
                path.Add(nextHex);
            }
        }
        return path;
    }

    void ClearHighlightedHexes()
    {
        foreach (HexCell hex in highlightedHexes)
        {
            hex.ResetHighlight();  // Assuming there's a method in HexCell to reset the highlight
        }
        highlightedHexes.Clear();  // Clear the list of highlighted hexes
    }

    bool IsPointerOverToken()
    {
        return false;  // Placeholder until tokens are implemented
    }
}
