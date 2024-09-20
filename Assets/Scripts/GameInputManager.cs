using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.IO;

public class GameInputManager : MonoBehaviour
{
    public CameraController cameraController;  // Reference to the camera controller
    public Ball ball;  // Reference to the ball
    public HexGrid hexGrid;  // Add a reference to the HexGrid
    public MatchManager matchManager;

    // List to store highlighted hexes
    private List<HexCell> highlightedHexes = new List<HexCell>();
    private HexCell currentTargetHex = null;   // The currently selected target hex
    private HexCell lastClickedHex = null;     // The last hex that was clicked

    // Variables to track mouse movement for dragging
    private Vector3 mouseDownPosition;  // Where the mouse button was pressed
    private bool isDragging = false;    // Whether a drag is happening
    public float dragThreshold = 10f;   // Sensitivity to detect dragging vs. clicking (in pixels)
    

    void Start(){
    }

    void Update()
    {
        // Always handle camera movement with the keyboard, regardless of mouse input
        cameraController.HandleCameraInput();
        HandleMouseInput();
        // Handle game-specific inputs based on the current match state
        if (MatchManager.Instance.currentState == MatchManager.GameState.KickOffSetup && Input.GetKeyDown(KeyCode.Space))
        {
            MatchManager.Instance.StartMatch();
        }
        // Listen for input to trigger a standard pass
        if (Input.GetKeyDown(KeyCode.P))
        {
            MatchManager.Instance.TriggerStandardPass();
        }
        else if (Input.GetKeyDown(KeyCode.M))
        {
            MatchManager.Instance.TriggerMovement();
        }
        else if (Input.GetKeyDown(KeyCode.C))
        {
            MatchManager.Instance.TriggerHighPass();
        }
        else if (Input.GetKeyDown(KeyCode.L))
        {
            MatchManager.Instance.TriggerLongPass();
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            MatchManager.Instance.TriggerShot();
        }
        else if (Input.GetKeyDown(KeyCode.H))
        {
            MatchManager.Instance.TriggerHeader();
        }
        else if (Input.GetKeyDown(KeyCode.F))
        {
            MatchManager.Instance.TriggerFTP();
        }

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
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            HexCell clickedHex = hit.collider.GetComponent<HexCell>();
            if (clickedHex != null)
            {
                HexCell ballHex = ball.GetCurrentHex();
                if (ballHex == null)
                {
                    Debug.LogError("Ball's current hex is null! Ensure the ball has been placed on the grid.");
                    return;
                }
                // Calculate the number of steps between the ball and the clicked hex
                int steps = HexGridUtils.GetStepsBetweenHexes(ballHex, clickedHex);
                Debug.Log($"Steps from ball to target hex: {steps}");

                // Reject the input if the number of steps exceeds 11
                if (steps > 11)
                {
                    Debug.LogWarning("Target hex is too far! The maximum range is 11 steps.");
                    return;
                }
                else {
                    if (clickedHex == currentTargetHex && clickedHex == lastClickedHex)
                    {
                        // Double click on the same hex: confirm the move
                        StartCoroutine(ClearHighlightsAfterMove(currentTargetHex));
                        ball.DeselectBall();
                    }
                    else
                    {
                        // First or new click on a different hex: highlight the path
                        ClearHighlightedHexes();
                        HighlightPathToHex(clickedHex);
                        currentTargetHex = clickedHex;
                    }
                }

                lastClickedHex = clickedHex;  // Track the last clicked hex
            }
        }
    }

    private IEnumerator ClearHighlightsAfterMove(HexCell targetHex)
    {
        // Ensure the ball and targetHex are valid
        if (ball == null)
        {
            Debug.LogError("Ball reference is null!");
            yield break;
        }

        if (targetHex == null)
        {
            Debug.LogError("Target Hex is null in ClearHighlightsAfterMove!");
            yield break;
        }

        // Wait for the ball movement to complete
        yield return StartCoroutine(ball.MoveToCell(targetHex));

        // Now clear the highlights after the movement
        ClearHighlightedHexes();
        Debug.Log("Highlights cleared after ball movement.");
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
        List<HexCell> pathHexes = CalculateThickPath(ballHex, targetHex, ballRadius);  // Calculate the path between the ball and the target hex
        // Prepare a string to hold the coordinates for logging
        string hexCoordinatesLog = "Highlighted Path: ";
        foreach (HexCell hex in pathHexes)
        {
            if (hex == null)
            {
                Debug.LogError("A hex in the path is null! Check the path calculation.");
                continue;
            }
            hex.HighlightHex("ballPath");    // Assuming there's a method in HexCell to highlight the hex
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

    public List<HexCell> CalculateThickPath(HexCell startHex, HexCell endHex, float ballRadius)
    {
        List<HexCell> path = new List<HexCell>();
        string logContent = $"Ball Radius: {ballRadius}\n";
        string startHexCoordinates = $"({startHex.coordinates.x}, {startHex.coordinates.z})";
        string endHexCoordinates = $"({endHex.coordinates.x}, {endHex.coordinates.z})";
        logContent += $"Starting Hex: {startHexCoordinates}, Target Hex: {endHexCoordinates}\n";

        // Get world positions of the start and end hex centers
        Vector3 startPos = startHex.GetHexCenter();
        Vector3 endPos = endHex.GetHexCenter();

        // Step 2: Get a list of candidate hexes based on the bounding box
        List<HexCell> candidateHexes = GetCandidateHexes(startHex, endHex, ballRadius);

        // Step 3: Loop through the candidate hexes and check distances to the parallel lines
        foreach (HexCell candidateHex in candidateHexes)
        {
            Vector3 candidatePos = candidateHex.GetHexCenter();

            // Check the distance from the candidate hex to the main line
            float distanceToLine = DistanceFromPointToLine(candidatePos, startPos, endPos);

            if (distanceToLine <= ballRadius)
            {
                // Hex is within the thick path
                if (!path.Contains(candidateHex))
                {
                    path.Add(candidateHex);
                    logContent += $"Added Hex: ({candidateHex.coordinates.x}, {candidateHex.coordinates.z}), Distance to Line: {distanceToLine}, Radius: {ballRadius}\n";
                }
            }
            else
            {
                logContent += $"Not Added: ({candidateHex.coordinates.x}, {candidateHex.coordinates.z}), Distance: {distanceToLine} exceeds Ball Radius: {ballRadius}\n";
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

    public List<HexCell> GetCandidateHexes(HexCell startHex, HexCell endHex, float ballRadius)
    {
        List<HexCell> candidates = new List<HexCell>();

        // Get the axial coordinates of the start and end hexes
        Vector3Int startCoords = startHex.coordinates;
        Vector3Int endCoords = endHex.coordinates;

        // Determine the bounds (min and max x and z)
        int minX = Mathf.Min(startCoords.x, endCoords.x) - 1;
        int maxX = Mathf.Max(startCoords.x, endCoords.x) + 1;
        int minZ = Mathf.Min(startCoords.z, endCoords.z) - 1;
        int maxZ = Mathf.Max(startCoords.z, endCoords.z) + 1;

        // Loop through all hexes in the bounding box
        for (int x = minX; x <= maxX; x++)
        {
            for (int z = minZ; z <= maxZ; z++)
            {
                Vector3Int coords = new Vector3Int(x, 0, z);
                HexCell hex = hexGrid.GetHexCellAt(coords);

                if (hex != null)
                {
                    candidates.Add(hex);
                }
            }
        }

        return candidates;
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

        // Write the log text to the file (overwrite mode)
        using (StreamWriter writer = new StreamWriter(filePath))
        {
            writer.WriteLine(logText);
        }

        Debug.Log($"Log saved to: {filePath}");
    }
}
