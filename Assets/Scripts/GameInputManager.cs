using UnityEngine;
using System.Collections.Generic;
using System.IO;

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
    

    void Start(){}

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
        float ballRadius = ball.ballRadius;  // Get the ball's radius
        ClearHighlightedHexes();
        List<HexCell> pathHexes = CalculateHexPathWithRadius(ballHex, targetHex, ballRadius);  // Calculate the path between the ball and the target hex
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

    public List<HexCell> CalculateHexPathWithRadius(HexCell startHex, HexCell endHex, float ballRadius)
    {
        List<HexCell> path = new List<HexCell>();
        string logContent = $"Ball Radius: {ballRadius}\n";
        string startHexCoordinates = $"({startHex.coordinates.x}, {startHex.coordinates.z})";
        string endHexCoordinates = $"({endHex.coordinates.x}, {endHex.coordinates.z})";
        logContent += $"Starting Hex: {startHexCoordinates}, Target Hex: {endHexCoordinates}\n";

        // Calculate the straight-line path between start and end
        List<HexCell> initialPath = CalculateHexPath(startHex, endHex);

        // First, add the main path
        foreach (HexCell hex in initialPath)
        {
            if (!path.Contains(hex))
            {
                path.Add(hex);
                logContent += $"Added Hex: ({hex.coordinates.x}, {hex.coordinates.z}) from main path\n";
            }
        }

        // Convert hex coordinates to world space for the distance calculation
        Vector3 startPos = startHex.GetHexCenter();
        Vector3 endPos = endHex.GetHexCenter();

        // Now explore neighbors of the hexes in the initial path
        Queue<HexCell> hexQueue = new Queue<HexCell>(initialPath); // Add all hexes in the initial path to the queue for neighbor exploration

        while (hexQueue.Count > 0)
        {
            HexCell currentHex = hexQueue.Dequeue();
            logContent += $"Finding Neighbours of ({currentHex.coordinates.x}, {currentHex.coordinates.z})\n";

            // Get the neighbors of the current hex
            foreach (HexCell neighbor in currentHex.GetNeighbors(hexGrid))
            {
                if (neighbor == null || path.Contains(neighbor)) // Skip invalid or already added hexes
                {
                    if (neighbor != null && path.Contains(neighbor))
                    {
                        logContent += $"Exists in path ({neighbor.coordinates.x}, {neighbor.coordinates.z}), skipping\n";
                    }
                    continue;
                }

                // Calculate the distance from the neighbor's center to the line (startPos to endPos)
                float distanceToLine = DistanceFromPointToLine(neighbor.GetHexCenter(), startPos, endPos);

                // If the distance to the line is within the ball radius, add the neighbor to the path
                if (distanceToLine <= ballRadius)
                {
                    path.Add(neighbor);  // Add neighbor to the path
                    hexQueue.Enqueue(neighbor);  // Enqueue the neighbor for further exploration
                    logContent += $"Added Hex: ({neighbor.coordinates.x}, {neighbor.coordinates.z}), Distance to Line: {distanceToLine}, Radius: {ballRadius}\n";
                }
                else
                {
                    logContent += $"Not Added: ({neighbor.coordinates.x}, {neighbor.coordinates.z}), Distance: {distanceToLine} exceeds Ball Radius: {ballRadius}\n";
                }
            }
        }

        // Log the final highlighted path to the file
        string highlightedPath = "Highlighted Path: ";
        foreach (HexCell hex in path)
        {
            highlightedPath += $"({hex.coordinates.x}, {hex.coordinates.z}), ";
        }
        highlightedPath = highlightedPath.TrimEnd(new char[] { ',', ' ' });
        logContent += highlightedPath;

        // Save the log to a file
        SaveLogToFile(logContent, startHexCoordinates, endHexCoordinates);

        return path;
    }

    public List<HexCell> CalculateHexPath(HexCell startHex, HexCell endHex)
    {
        List<HexCell> path = new List<HexCell>();

        Vector3Int startCoords = startHex.coordinates;
        Vector3Int endCoords = endHex.coordinates;

        int steps = Mathf.Max(Mathf.Abs(endCoords.x - startCoords.x), Mathf.Abs(endCoords.z - startCoords.z));

        for (int i = 0; i <= steps; i++)
        {
            float t = steps == 0 ? 0 : (float)i / steps;
            
            // Call the axial interpolation and rounding from HexGridUtils
            Vector3 interpolatedAxial = HexGridUtils.AxialLerp(startCoords, endCoords, t);
            Vector3Int roundedAxial = HexGridUtils.AxialRound(interpolatedAxial);

            HexCell hex = hexGrid.GetHexCellAt(roundedAxial);
            if (hex != null && !path.Contains(hex))
            {
                path.Add(hex); // Add the hex to the path
            }
        }

        return path;
    }


    float DistanceFromPointToLine(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
    {
        Vector3 lineDirection = lineEnd - lineStart;
        float lineLength = lineDirection.magnitude;
        lineDirection.Normalize();

        // Project the point onto the line, clamping between the start and end of the line
        float projectedLength = Mathf.Clamp(Vector3.Dot(point - lineStart, lineDirection), 0, lineLength);

        // Calculate the closest point on the line
        Vector3 closestPoint = lineStart + lineDirection * projectedLength;

        // Return the distance from the point to the closest point on the line
        return Vector3.Distance(point, closestPoint);
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

    void SaveLogToFile(string logText, string startHex, string endHex)
    {
        // Define the file path (you can customize this path)
        string filePath = Application.dataPath + $"/Logs/HexPath_{startHex}_to_{endHex}.txt";

        // Ensure the directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(filePath));

        // Write the log text to the file (append mode)
        using (StreamWriter writer = new StreamWriter(filePath, true))
        {
            writer.WriteLine(logText);
        }

        Debug.Log($"Log saved to: {filePath}");
    }
}
