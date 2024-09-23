using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;


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
    // Ground Pass Interceptions
    private List<HexCell> interceptionHexes = new List<HexCell>();  // List of interception hexes
    private List<HexCell> defendingHexes = new List<HexCell>();     // List of defenders responsible for each interception hex
    private int diceRollsPending = 0;                               // Number of pending dice rolls
    private HexCell currentDefenderHex = null;                      // The defender hex currently rolling the dice
    private bool passIsDangerous = false;      // To check if the pass is dangerous
    private bool isWaitingForDiceRoll = false; // To check if we are waiting for dice rolls
    
    void Start()
    {
        TestHexConversions();
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
        else if (isWaitingForDiceRoll && Input.GetKeyDown(KeyCode.D))
        {
            PerformDiceRoll();  // Trigger dice roll on "D" key press
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
            if (!isDragging && ball.IsBallSelected() && MatchManager.Instance.currentState == MatchManager.GameState.StandardPassAttempt)
            {
                // Handle ball movement or path highlighting
                HandleGroundBallPath();
            }
            if (!isDragging && ball.IsBallSelected() && MatchManager.Instance.currentState == MatchManager.GameState.LongBallAttempt)
            {
                // Handle ball movement or path highlighting
                HandleLongBallPath();
            }
            // Reset dragging state
            isDragging = false;
        }
    }

    void HandleGroundBallPath()
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
                else
                {
                    // Now handle the pass based on difficulty
                    HandlePassBasedOnDifficulty(clickedHex);
                }   
            }
        }
    }

    void HandlePassBasedOnDifficulty(HexCell clickedHex)
    {
        int difficulty = MatchManager.Instance.difficulty_level;  // Get current difficulty
        HexCell ballHex = ball.GetCurrentHex();
        if (ballHex == null)
        {
            Debug.LogError("Ball's current hex is null! Ensure the ball has been placed on the grid.");
            return;
        }
        // Convert the hex coordinates to cube coordinates
        Vector3Int ballCubeCoords = HexGridUtils.OffsetToCube(ballHex.coordinates.x, ballHex.coordinates.z);
        Vector3Int clickedCubeCoords = HexGridUtils.OffsetToCube(clickedHex.coordinates.x, clickedHex.coordinates.z);
        // Calculate the number of steps between the ball and the clicked hex
        int steps = HexGridUtils.GetHexDistance(ballCubeCoords, clickedCubeCoords);
        // Debug.Log($"Steps from {ballHex} to {clickedHex} hex: {steps}");
        // Reject the input if the number of steps exceeds 11
        if (steps > 11)
        {
            Debug.LogWarning("Target hex is too far! The maximum range is 11 steps.");
            return;
        }
        else
        {
            List<HexCell> pathHexes = CalculateThickPath(ball.GetCurrentHex(), clickedHex, ball.ballRadius);
            if (IsPassValid(clickedHex, pathHexes))  // Validate the pass
            {
                if (difficulty == 3)  // Difficult Level: No path visualization
                {
                    CheckForDangerousPath(clickedHex);  // Check if the pass is dangerous
                    currentTargetHex = clickedHex;  // Make sure the target hex is set
                    if (passIsDangerous)
                    {
                        Debug.Log("Dangerous pass detected. Waiting for dice rolls...");
                        diceRollsPending = defendingHexes.Count;  // Set number of dice rolls based on defenders involved
                        StartDiceRollSequence();
                    }
                    else
                    {
                        Debug.Log("Pass is not dangerous, moving ball.");
                        // Ensure currentTargetHex is set before movement
                        if (currentTargetHex == null)
                        {
                            Debug.LogError("currentTargetHex is null despite the pass being valid.");
                        }
                        StartCoroutine(HandleGroundBallMovement(clickedHex));  // Execute the pass with movement
                    }
                    ball.DeselectBall();
                }
                else if (difficulty == 2 || difficulty == 1)  // Medium & Easy levels
                {
                    HighlightGroundPathToHex(clickedHex);  // Highlight the path based on difficulty
                    if (clickedHex == currentTargetHex && clickedHex == lastClickedHex)
                    {
                        CheckForDangerousPath(clickedHex);  // Check if the pass is dangerous
                        if (passIsDangerous)
                        {
                            Debug.Log("Dangerous pass detected. Waiting for dice rolls...");
                            diceRollsPending = defendingHexes.Count;
                            StartDiceRollSequence();
                        }
                        else
                        {
                            Debug.Log("Pass is not dangerous, moving ball.");
                            // Ensure currentTargetHex is set before movement
                            if (currentTargetHex == null)
                            {
                                Debug.LogError("currentTargetHex is null despite the pass being valid.");
                            }
                            StartCoroutine(HandleGroundBallMovement(currentTargetHex));
                        }
                        ball.DeselectBall();
                    }
                    else
                    {
                        currentTargetHex = clickedHex;  // Set target for the first click
                        lastClickedHex = clickedHex;    // Track the last clicked hex
                    }
                }
            }
        }
    }

    bool IsPassValid(HexCell targetHex, List<HexCell> pathHexes)
    {
        // Check if the pass is blocked by any defense-occupied hex in the path
        foreach (HexCell hex in pathHexes)
        {
            if (hex.isDefenseOccupied)
            {
                Debug.LogWarning($"Invalid path! Hex at {hex.coordinates} is occupied by defense.");
                return false; // The path is blocked by a defender
            }
        }
        return true;
    }
    
    public void CheckForDangerousPath(HexCell targetHex)
    {
        HexCell ballHex = ball.GetCurrentHex();  // Get the current hex of the ball
        List<HexCell> pathHexes = CalculateThickPath(ballHex, targetHex, ball.ballRadius);
        ClearHighlightedHexes();
        // Remove the ball's current hex from the path
        pathHexes.Remove(ballHex);

        // Get defenders and their neighbors
        List<HexCell> defenderHexes = hexGrid.GetDefenderHexes();
        List<HexCell> defenderNeighbors = hexGrid.GetDefenderNeighbors(defenderHexes);
        
        // Initialize danger variables
        passIsDangerous = false;
        interceptionHexes.Clear();
        defendingHexes.Clear();
        // Track defenders that have already been processed
        HashSet<HexCell> alreadyProcessedDefenders = new HashSet<HexCell>();

        // Check if the path crosses any defender's ZOI
        foreach (HexCell hex in pathHexes)
        {
            // Get the neighbors of the hex and log them for debugging purposes
            HexCell[] neighbors = hex.GetNeighbors(hexGrid);
            // Check if a defender's neighbor is in the path
            if (defenderNeighbors.Contains(hex))
            {
                // HexCell defender = defenderHexes.Find(d => HexCell.GetNeighbors(hexGrid).Contains(hex));
                HexCell defender = defenderHexes.Find(d => d.GetNeighbors(hexGrid).Any(n => n == hex));

                // Only add the defender and interception hex if the defender hasn't already been processed
                if (defender != null && !alreadyProcessedDefenders.Contains(defender))
                {
                    interceptionHexes.Add(hex);  // Add the interceptable hex
                    defendingHexes.Add(defender);  // Add the defender responsible
                    alreadyProcessedDefenders.Add(defender);  // Mark this defender as processed
                    passIsDangerous = true;  // Mark the pass as dangerous
                    // Debug.Log($"Defender at {defender.coordinates} can intercept at {hex.coordinates}");
                    // Debug.Log($"Neighbors of hex ({hex.coordinates.x}, {hex.coordinates.z}): {string.Join(", ", neighbors.Select(n => n?.coordinates.ToString() ?? "null"))}");
                    Debug.Log($"Defender at {defender.coordinates} can intercept at {hex.coordinates}. Defender's ZOI: {string.Join(", ", defender.GetNeighbors(hexGrid).Select(n => n?.coordinates.ToString() ?? "null"))}");
                }
            }

            hex.HighlightHex("ballPath");  // Highlight the path
            highlightedHexes.Add(hex);
        }
    }

    void StartDiceRollSequence()
    {
        // Sort defendingHexes by distance from ballHex
        defendingHexes = defendingHexes.OrderBy(d => HexGridUtils.GetHexDistance(ball.GetCurrentHex().coordinates, d.coordinates)).ToList();
        // Start the dice roll process for each defender
        if (defendingHexes.Count > 0)
        {
            Debug.Log("Starting dice roll sequence...");
            isWaitingForDiceRoll = true;
            currentDefenderHex = defendingHexes[0];  // Start with the closest defender
        }
        else
        {
            Debug.LogWarning("No defenders found for interception.");
            StartCoroutine(HandleGroundBallMovement(currentTargetHex));  // No defenders, move ball to target
        }
    }

    void PerformDiceRoll()
    {
        if (currentDefenderHex != null)
        {
            // Roll the dice (1 to 6)
            int diceRoll = Random.Range(1, 7);
            Debug.Log($"Dice roll by defender at {currentDefenderHex.coordinates}: {diceRoll}");

            if (diceRoll == 10)
            {
                // Defender successfully intercepts the pass
                Debug.Log($"Pass intercepted by defender at {currentDefenderHex.coordinates}!");
                StartCoroutine(HandleGroundBallMovement(currentDefenderHex));  // Move the ball to the defender's hex
                isWaitingForDiceRoll = false;
                ResetDiceRolls();
            }
            else
            {
                Debug.Log($"Defender at {currentDefenderHex.coordinates} failed to intercept.");
                // Move to the next defender, if any
                defendingHexes.Remove(currentDefenderHex);
                if (defendingHexes.Count > 0)
                {
                    currentDefenderHex = defendingHexes[0];  // Move to the next defender
                }
                else
                {
                    // No more defenders to roll, pass is successful
                    Debug.Log("Pass successful! No more defenders to roll.");
                    // Ensure currentTargetHex is set before movement
                    if (currentTargetHex == null)
                    {
                        Debug.LogError("currentTargetHex is null despite the pass being valid.");
                    }
                    StartCoroutine(HandleGroundBallMovement(currentTargetHex));
                    isWaitingForDiceRoll = false;
                }
            }
        }
    }

    void ResetDiceRolls()
    {
        // Reset variables after the dice roll sequence
        defendingHexes.Clear();
        interceptionHexes.Clear();
        diceRollsPending = 0;
        currentDefenderHex = null;
    }
    void HandleLongBallPath()
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
                else {
                    if (clickedHex == currentTargetHex && clickedHex == lastClickedHex)
                    {
                        // Double click on the same hex: confirm the move
                        StartCoroutine(HandleAirBallMovement(currentTargetHex));
                        ball.DeselectBall();
                    }
                    else
                    {
                        // First or new click on a different hex: highlight the path
                        ClearHighlightedHexes();
                        // Highlight only the Target Hex
                        // TODO: Highlight 6 hexes the Target Hex
                        HighlightLongPassArea(clickedHex);
                        // clickedHex.HighlightHex("ballPath");
                        currentTargetHex = clickedHex;
                    }
                }
                lastClickedHex = clickedHex;  // Track the last clicked hex
            }
        }
    }

    private IEnumerator HandleGroundBallMovement(HexCell targetHex)
    {
        // Ensure the ball and targetHex are valid
        if (ball == null)
        {
            Debug.LogError("Ball reference is null!");
            yield break;
        }
        if (targetHex == null)
        {
            Debug.LogError("Target Hex is null in HandleGroundBallMovement!");
            Debug.LogError($"currentTargetHex: {currentTargetHex}, isWaitingForDiceRoll: {isWaitingForDiceRoll}");
            yield break;
        }
        // Set thegame status to StandardPassMoving
        MatchManager.Instance.currentState = MatchManager.GameState.StandardPassMoving;
        // Wait for the ball movement to complete
        yield return StartCoroutine(ball.MoveToCell(targetHex));
        // Set the game status to StandardPassCompleted
        MatchManager.Instance.currentState = MatchManager.GameState.StandardPassCompleted;
        // Now clear the highlights after the movement
        ClearHighlightedHexes();
        Debug.Log("Highlights cleared after ball movement.");
    }
    
    private IEnumerator HandleAirBallMovement(HexCell targetHex)
    {
        // Ensure the ball and targetHex are valid
        if (ball == null)
        {
            Debug.LogError("Ball reference is null!");
            yield break;
        }

        if (targetHex == null)
        {
            Debug.LogError("Target Hex is null in HandleAirBallMovement!");
            yield break;
        }
        // Set thegame status to StandardPassMoving
        MatchManager.Instance.currentState = MatchManager.GameState.LongPassMoving;
        // Wait for the ball movement to complete
        yield return StartCoroutine(ball.MoveToCell(targetHex));
        // Set the game status to StandardPassCompleted
        MatchManager.Instance.currentState = MatchManager.GameState.LongPassCompleted;
        // Now clear the highlights after the movement
        ClearHighlightedHexes();
        Debug.Log("Highlights cleared after ball movement.");
    }

    public void HighlightGroundPathToHex(HexCell targetHex)
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
        // Convert the hex coordinates to cube coordinates
        Vector3Int ballCubeCoords = HexGridUtils.OffsetToCube(ballHex.coordinates.x, ballHex.coordinates.z);
        Vector3Int clickedCubeCoords = HexGridUtils.OffsetToCube(targetHex.coordinates.x, targetHex.coordinates.z);
        // Calculate the number of steps between the ball and the clicked hex
        int steps = HexGridUtils.GetHexDistance(ballCubeCoords, clickedCubeCoords);
        Debug.Log($"Steps from {ballHex} to {targetHex} hex: {steps}");
        // Reject the input if the number of steps exceeds 11
        if (steps > 11)
        {
            Debug.LogWarning("Target hex is too far! The maximum range is 11 steps.");
            return;
        }
        // Step 1: Calculate the path between the ball and the target hex
        List<HexCell> pathHexes = CalculateThickPath(ballHex, targetHex, ballRadius);
        // Step 2: Get all the hexes occupied by defenders
        List<HexCell> defenderHexes = hexGrid.GetDefenderHexes();
        // Step 3: Get the neighbors of those defender hexes
        List<HexCell> defenderNeighbors = hexGrid.GetDefenderNeighbors(defenderHexes);
        
        // Get difficulty level from MatchManager
        int difficultyLevel = MatchManager.Instance.difficulty_level;
        // Step 4: Assume the path is valid initially
        bool isValidPath = true;
        // Step 5: Check if the path is dangerous
        bool isDangerous = hexGrid.IsPassDangerous(pathHexes, defenderNeighbors);
        foreach (HexCell hex in pathHexes)
        {
            // Skip defense-occupied hexes or invalid hexes from the path
            if (hex.isDefenseOccupied)
            {
                isValidPath = false;
                Debug.LogWarning($"Invalid path! Hex at {hex.coordinates} is blocked or too far.");
                break;
            }
        }
        // Medium Difficulty: Show valid paths and notify of dangerous passes
        // Easy Difficulty: Highlight all reachable hexes
        if (isValidPath) {
            if (difficultyLevel == 1)
            {
                // HighlightEasyMode(pathHexes, isDangerous);
                HighlightEasyMode(targetHex);
                // HighlightMediumMode(pathHexes, isDangerous);
            }
            if (difficultyLevel == 2)
            {
                HighlightMediumMode(pathHexes, isDangerous);
            }
            // Hard Difficulty: No path preview, just move if valid
            else if (difficultyLevel == 3)
            {
                    ball.MoveToCell(targetHex);  // Move immediately with no confirmation
                    Debug.Log("Path selected. Moving the ball.");
            }
        }
    }

    public void HighlightEasyMode(HexCell hoveredHex)
{
    // Retrieve the current hex where the ball is located
    HexCell ballHex = ball.GetCurrentHex();

    // Null check for ballHex to avoid further issues
    if (ballHex == null)
    {
        Debug.LogError("Ball's current hex is null! Ensure the ball has a valid hex.");
        return;
    }

    // Clear any previously highlighted hexes
    ClearHighlightedHexes();

    // Get hexes within valid range (11 hexes from the ball's position)
    List<HexCell> hexesInRange = HexGrid.GetHexesInRange(hexGrid, ballHex, 11);
    Debug.Log($"Hexes in range (11 steps): {string.Join(", ", hexesInRange.Select(h => h.coordinates.ToString()))}");

    // Check if hovered hex is within the valid range
    if (!hexesInRange.Contains(hoveredHex))
    {
        Debug.LogWarning($"Hovered hex ({hoveredHex.coordinates}) is out of range.");
        return;  // Exit if hovered hex is out of range
    }

    // Calculate the path from the ball's position to the hovered hex
    List<HexCell> pathHexes = CalculateThickPath(ballHex, hoveredHex, ball.ballRadius);

    Debug.Log($"Hovered hex ({hoveredHex.coordinates}) is within range. Calculated path: {string.Join(", ", pathHexes.Select(h => h.coordinates.ToString()))}");

    // Get defender hexes and their neighbors (for ZOI check)
    List<HexCell> defenderHexes = hexGrid.GetDefenderHexes();
    List<HexCell> defenderNeighbors = hexGrid.GetDefenderNeighbors(defenderHexes);

    Debug.Log($"Defender hexes: {string.Join(", ", defenderHexes.Select(h => h.coordinates.ToString()))}");
    Debug.Log($"Defender neighbors (ZOI): {string.Join(", ", defenderNeighbors.Select(h => h.coordinates.ToString()))}");

    // Check if the path is dangerous (intersects with a defender's ZOI)
    bool isDangerous = hexGrid.IsPassDangerous(pathHexes, defenderNeighbors);

    Debug.Log($"Is pass dangerous: {isDangerous}");

    // Highlight the path based on whether it's dangerous or not
    HighlightValidPath(pathHexes, isDangerous);
}



private void HighlightValidPath(List<HexCell> pathHexes, bool isDangerous)
{
    string hexCoordinatesLog = "Highlighted Path: ";

    foreach (HexCell hex in pathHexes)
    {
        if (hex == null)
        {
            Debug.LogError("A hex in the path is null! Check the path calculation.");
            continue;
        }

        // Highlight based on danger status
        if (isDangerous)
        {
            hex.HighlightHex("dangerousPass");  // Orange for dangerous pass
        }
        else
        {
            hex.HighlightHex("ballPath");  // Regular path highlight
        }

        // Track highlighted hexes for future clearing
        highlightedHexes.Add(hex);

        // Log the highlighted hexes for debugging
        hexCoordinatesLog += $"({hex.coordinates.x}, {hex.coordinates.z}), ";
    }

    // Remove the last comma and log the final path
    hexCoordinatesLog = hexCoordinatesLog.TrimEnd(',', ' ');
    Debug.Log(hexCoordinatesLog);
}

    // Handles Medium Mode: Highlights only valid paths and warns of danger
    void HighlightMediumMode(List<HexCell> pathHexes, bool isDangerous)
    {
        string hexCoordinatesLog = "Highlighted Path: ";
        foreach (HexCell hex in pathHexes)
        {
            if (hex == null)
            {
                Debug.LogError("A hex in the path is null! Check the path calculation.");
                continue;
            }

            // Highlight hex based on danger status
            if (isDangerous)
            {
                hex.HighlightHex("dangerousPass");  // Orange for dangerous pass
            }
            else
            {
                hex.HighlightHex("ballPath");  // Regular path highlight
            }

            // Track highlighted hexes
            highlightedHexes.Add(hex);  
            hexCoordinatesLog += $"({hex.coordinates.x}, {hex.coordinates.z}), ";
        }

        // Log the path coordinates for debugging purposes
        hexCoordinatesLog = hexCoordinatesLog.TrimEnd(new char[] { ',', ' ' });
        Debug.Log(hexCoordinatesLog);
    }
    
    public void HighlightLongPassArea(HexCell targetHex)
    {
        // Get hexes within a radius (e.g., 6 hexes) around the targetHex
        int radius = 5;  // You can tweak this value as needed
        List<HexCell> hexesInRange = HexGrid.GetHexesInRange(hexGrid, targetHex, radius);

        // Loop through the hexes and highlight each one
        foreach (HexCell hex in hexesInRange)
        {
            // Highlight hexes (pass a specific color for Long Pass)
            hex.HighlightHex("longPass");  // Assuming HexHighlightReason.LongPass is defined for long pass highlights
            highlightedHexes.Add(hex);  // Track the highlighted hexes for later clearing
        }

        // Log the highlighted hexes if needed (optional)
        Debug.Log($"Highlighted {hexesInRange.Count} hexes around the target for a Long Pass.");
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
        path.Remove(startHex);
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
        // // Define the file path (you can customize this path)
        // string filePath = Application.dataPath + $"/Logs/HexPath_{startHex}_to_{endHex}.txt";

        // // Ensure the directory exists
        // Directory.CreateDirectory(Path.GetDirectoryName(filePath));

        // // Write the log text to the file (overwrite mode)
        // using (StreamWriter writer = new StreamWriter(filePath))
        // {
        //     writer.WriteLine(logText);
        // }

        // Debug.Log($"Log saved to: {filePath}");
    }

    public void TestHexConversions()
    {
        Vector3Int cubeCoords = new Vector3Int(3, -3, 0); // Example cube coordinates
        Vector2Int offsetCoords = HexGridUtils.CubeToOffset(cubeCoords);  // Convert cube to offset (even-q)
        Vector3Int convertedBackToCube = HexGridUtils.OffsetToCube(offsetCoords.x, offsetCoords.y);  // Convert offset back to cube

        Debug.Log($"Cube: {cubeCoords}, Offset: {offsetCoords}, Converted Back to Cube: {convertedBackToCube}");
    }

}
