using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;


public class GameInputManager : MonoBehaviour
{
    public CameraController cameraController;  // Reference to the camera controller
    public GroundBallManager groundBallManager;
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
    // private List<HexCell> interceptionHexes = new List<HexCell>();  // List of interception hexes
    // private List<HexCell> defendingHexes = new List<HexCell>();     // List of defenders responsible for each interception hex
    // private int diceRollsPending = 0;                               // Number of pending dice rolls
    // private HexCell currentDefenderHex = null;                      // The defender hex currently rolling the dice
    // private bool passIsDangerous = false;      // To check if the pass is dangerous
    // private bool isWaitingForDiceRoll = false; // To check if we are waiting for dice rolls
    
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
                groundBallManager.HandleGroundBallPath();
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

    // void HandleGroundBallPath()
    // {
    //     Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
    //     if (Physics.Raycast(ray, out RaycastHit hit))
    //     {
    //         HexCell clickedHex = hit.collider.GetComponent<HexCell>();
    //         if (clickedHex != null)
    //         {
    //             HexCell ballHex = ball.GetCurrentHex();
    //             if (ballHex == null)
    //             {
    //                 Debug.LogError("Ball's current hex is null! Ensure the ball has been placed on the grid.");
    //                 return;
    //             }
    //             else
    //             {
    //                 // Now handle the pass based on difficulty
    //                 HandlePassBasedOnDifficulty(clickedHex);
    //             }   
    //         }
    //     }
    // }

    // void HandlePassBasedOnDifficulty(HexCell clickedHex)
    // {
    //     int difficulty = MatchManager.Instance.difficulty_level;  // Get current difficulty
        
    //     // This was not here.
    //     // List<HexCell> pathHexes = CalculateThickPath(ball.GetCurrentHex(), clickedHex, ball.ballRadius);
        
    //     // Centralized path validation and danger assessment
    //     var (isValid, isDangerous, pathHexes) = ValidatePath(clickedHex);
    //     if (!isValid)
    //     {
    //         // Debug.LogWarning("Invalid pass. Path rejected.");
    //         return; // Reject invalid paths
    //     }
    //     currentTargetHex = clickedHex;  // Assign the current target hex
    //     // Handle each difficulty's behavior
    //     if (difficulty == 3) // Hard Mode
    //     {
    //         CheckForDangerousPath(clickedHex);
    //         if (passIsDangerous)
    //         {
    //             diceRollsPending = defendingHexes.Count; // is this relevant here?
    //             Debug.Log($"Dangerous pass detected. Waiting for {diceRollsPending} dice rolls...");
    //             StartDiceRollSequence();
    //         }
    //         else
    //         {
    //             Debug.Log("Pass is not dangerous, moving ball.");
    //             StartCoroutine(HandleGroundBallMovement(clickedHex)); // Execute pass
    //         }
    //         ball.DeselectBall();
    //     }
    //     else if (difficulty == 2)
    //     {
    //         ClearHighlightedHexes();
    //         HighlightValidPath(pathHexes, isDangerous);
    //         CheckForDangerousPath(clickedHex);
    //         diceRollsPending = defendingHexes.Count; // is this relevant here?
    //         Debug.Log($"Dangerous pass detected. If you confirm there will be {diceRollsPending} dice rolls...");
    //         // Medium Mode: Wait for a second click for confirmation
    //         if (clickedHex == currentTargetHex && clickedHex == lastClickedHex)
    //         {
    //             CheckForDangerousPath(clickedHex);
    //             if (passIsDangerous)
    //             {
    //                 diceRollsPending = defendingHexes.Count; // is this relevant here?
    //                 Debug.Log($"Dangerous pass detected. Waiting for {diceRollsPending} dice rolls...");
    //                 StartDiceRollSequence();
    //             }
    //             else
    //             {
    //                 Debug.Log("Pass is not dangerous, moving ball.");
    //                 StartCoroutine(HandleGroundBallMovement(clickedHex)); // Execute pass
    //             }
    //             ball.DeselectBall();
    //         }
    //         else
    //         {
    //             ClearHighlightedHexes();
    //             HighlightValidPath(pathHexes, isDangerous);
    //             currentTargetHex = clickedHex;
    //             lastClickedHex = clickedHex;  // Set for confirmation click
    //         }
    //     }
    //     else if (difficulty == 1) // Easy Mode: Handle hover and clicks with immediate highlights
    //     {
    //         CheckForDangerousPath(clickedHex);
    //         diceRollsPending = defendingHexes.Count; // is this relevant here?
    //         Debug.Log($"Dangerous pass detected. If you confirm there will be {diceRollsPending} dice rolls...");
    //         if (clickedHex == currentTargetHex && clickedHex == lastClickedHex)
    //         {
    //             // Second click on the same hex: confirm the pass
    //             Debug.Log("Second click detected, confirming pass...");
    //             CheckForDangerousPath(clickedHex);
    //             if (passIsDangerous)
    //             {
    //                 diceRollsPending = defendingHexes.Count; // is this relevant here?
    //                 Debug.Log($"Dangerous pass detected. Waiting for {diceRollsPending} dice rolls...");
    //                 StartDiceRollSequence();
    //             }
    //             else
    //             {
    //                 Debug.Log("Pass is not dangerous, moving ball.");
    //                 StartCoroutine(HandleGroundBallMovement(clickedHex)); // Execute pass
    //             }
    //             ball.DeselectBall();
    //         }
    //         else
    //         {
    //             ClearHighlightedHexes();
    //             HighlightValidPath(pathHexes, isDangerous);
    //             currentTargetHex = clickedHex; // Set this as the current target hex
    //             lastClickedHex = clickedHex; // Track the last clicked hex
    //         }
    //     }
    // }
    
    // public (bool isValid, bool isDangerous, List<HexCell> pathHexes) ValidatePath(HexCell targetHex)
    // {
    //     HexCell ballHex = ball.GetCurrentHex();
    //     // Step 1: Ensure the ballHex and targetHex are valid
    //     if (ballHex == null || targetHex == null)
    //     {
    //         Debug.LogError("Ball or target hex is null!");
    //         return (false, false, null);
    //     }
    //     // Step 2: Calculate the path between the ball and the target hex
    //     List<HexCell> pathHexes = CalculateThickPath(ballHex, targetHex, ball.ballRadius);
    //     // Get the distance in hex steps
    //     Vector3Int ballCubeCoords = HexGridUtils.OffsetToCube(ballHex.coordinates.x, ballHex.coordinates.z);
    //     Vector3Int targetCubeCoords = HexGridUtils.OffsetToCube(targetHex.coordinates.x, targetHex.coordinates.z);
    //     int distance = HexGridUtils.GetHexDistance(ballCubeCoords, targetCubeCoords);
    //     // Check the distance limit
    //     if (distance > 11)
    //     {
    //         Debug.LogWarning($"Pass is out of range. Maximum steps allowed: 11. Current steps: {distance}");
    //         return (false, false, pathHexes);
    //     }
    //     // Step 3: Check if the path is valid by ensuring no defense-occupied hexes block the path
    //     foreach (HexCell hex in pathHexes)
    //     {
    //         if (hex.isDefenseOccupied)
    //         {
    //             Debug.Log($"Path blocked by defender at hex: {hex.coordinates}");
    //             return (false, false, pathHexes); // Invalid path
    //         }
    //     }

    //     // Step 4: Get defenders and their ZOI
    //     List<HexCell> defenderHexes = hexGrid.GetDefenderHexes();
    //     List<HexCell> defenderNeighbors = hexGrid.GetDefenderNeighbors(defenderHexes);

    //     // Step 5: Determine if the path is dangerous by checking if it passes through any defender's ZOI
    //     bool isDangerous = hexGrid.IsPassDangerous(pathHexes, defenderNeighbors);

    //     // Debug.Log($"Path to {targetHex.coordinates}: Valid={true}, Dangerous={isDangerous}");

    //     return (true, isDangerous, pathHexes);
    // }

    // public void HighlightValidPath(List<HexCell> pathHexes, bool isDangerous)
    // {
    //     foreach (HexCell hex in pathHexes)
    //     {
    //         if (hex == null) continue; // to next hex (loop)
    //         hex.HighlightHex(isDangerous ? "dangerousPass" : "ballPath");
    //         highlightedHexes.Add(hex);  // Track the highlighted hexes
    //     }
    // }

    // public void CheckForDangerousPath(HexCell targetHex)
    // {
    //     HexCell ballHex = ball.GetCurrentHex();  // Get the current hex of the ball
    //     List<HexCell> pathHexes = CalculateThickPath(ballHex, targetHex, ball.ballRadius);
    //     ClearHighlightedHexes();
    //     // Remove the ball's current hex from the path
    //     pathHexes.Remove(ballHex);

    //     // Get defenders and their neighbors
    //     List<HexCell> defenderHexes = hexGrid.GetDefenderHexes();
    //     List<HexCell> defenderNeighbors = hexGrid.GetDefenderNeighbors(defenderHexes);
        
    //     // Initialize danger variables
    //     passIsDangerous = false;
    //     interceptionHexes.Clear();
    //     defendingHexes.Clear();
    //     // Track defenders that have already been processed
    //     HashSet<HexCell> alreadyProcessedDefenders = new HashSet<HexCell>();

    //     // Check if the path crosses any defender's ZOI
    //     foreach (HexCell hex in pathHexes)
    //     {
    //         // Get the neighbors of the hex and log them for debugging purposes
    //         HexCell[] neighbors = hex.GetNeighbors(hexGrid);
    //         // Check if a defender's neighbor is in the path excluding Attacking occupied Hexes
    //         if (defenderNeighbors.Contains(hex) && !hex.isAttackOccupied)
    //         {
    //             HexCell defender = defenderHexes.Find(d => d.GetNeighbors(hexGrid).Any(n => n == hex));

    //             // Only add the defender and interception hex if the defender hasn't already been processed
    //             if (defender != null && !alreadyProcessedDefenders.Contains(defender))
    //             {
    //                 interceptionHexes.Add(hex);  // Add the interceptable hex
    //                 defendingHexes.Add(defender);  // Add the defender responsible
    //                 alreadyProcessedDefenders.Add(defender);  // Mark this defender as processed
    //                 passIsDangerous = true;  // Mark the pass as dangerous
    //                 // Debug.Log($"Defender at {defender.coordinates} can intercept at {hex.coordinates}");
    //                 // Debug.Log($"Neighbors of hex ({hex.coordinates.x}, {hex.coordinates.z}): {string.Join(", ", neighbors.Select(n => n?.coordinates.ToString() ?? "null"))}");
    //                 Debug.Log($"Defender at {defender.coordinates} can intercept at {hex.coordinates}. Defender's ZOI: {string.Join(", ", defender.GetNeighbors(hexGrid).Select(n => n?.coordinates.ToString() ?? "null"))}");
    //             }
    //         }

    //         hex.HighlightHex("ballPath");  // Highlight the path
    //         highlightedHexes.Add(hex);
    //     }
    // }
    // void StartDiceRollSequence()
    // {
    //     Debug.Log($"Defenders with interception chances: {defendingHexes.Count}");
    //     if (defendingHexes.Count > 0)
    //     {
    //         // Start the dice roll process for each defender
    //         Debug.Log("Starting dice roll sequence...");
    //         // Sort defendingHexes by distance from ballHex
    //         defendingHexes = defendingHexes.OrderBy(d => HexGridUtils.GetHexDistance(ball.GetCurrentHex().coordinates, d.coordinates)).ToList();
    //         currentDefenderHex = defendingHexes[0];  // Start with the closest defender
    //         isWaitingForDiceRoll = true;
    //     }
    //     else
    //     {
    //         Debug.LogWarning("No defenders in ZOI. This should never appear unless the path is clear.");
    //         StartCoroutine(HandleGroundBallMovement(currentTargetHex));  // Move ball to the target hex
    //         return;
    //     }
    // }

    // void PerformDiceRoll()
    // {
    //     if (currentDefenderHex != null)
    //     {
    //         // Roll the dice (1 to 6)
    //         int diceRoll = Random.Range(1, 7);
    //         Debug.Log($"Dice roll by defender at {currentDefenderHex.coordinates}: {diceRoll}");

    //         if (diceRoll == 10)
    //         {
    //             // Defender successfully intercepts the pass
    //             Debug.Log($"Pass intercepted by defender at {currentDefenderHex.coordinates}!");
    //             StartCoroutine(HandleGroundBallMovement(currentDefenderHex));  // Move the ball to the defender's hex
    //             isWaitingForDiceRoll = false;
    //             ResetDiceRolls();
    //         }
    //         else
    //         {
    //             Debug.Log($"Defender at {currentDefenderHex.coordinates} failed to intercept.");
    //             // Move to the next defender, if any
    //             defendingHexes.Remove(currentDefenderHex);
    //             if (defendingHexes.Count > 0)
    //             {
    //                 currentDefenderHex = defendingHexes[0];  // Move to the next defender
    //             }
    //             else
    //             {
    //                 // No more defenders to roll, pass is successful
    //                 Debug.Log("Pass successful! No more defenders to roll.");
    //                 // Ensure currentTargetHex is set before movement
    //                 if (currentTargetHex == null)
    //                 {
    //                     Debug.LogError("currentTargetHex is null despite the pass being valid.");
    //                 }
    //                 StartCoroutine(HandleGroundBallMovement(currentTargetHex));
    //                 isWaitingForDiceRoll = false;
    //             }
    //         }
    //     }
    // }

    // void ResetDiceRolls()
    // {
    //     // Reset variables after the dice roll sequence
    //     defendingHexes.Clear();
    //     interceptionHexes.Clear();
    //     diceRollsPending = 0;
    //     currentDefenderHex = null;
    // }
    
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
                        // ClearHighlightedHexes();
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

    // private IEnumerator HandleGroundBallMovement(HexCell targetHex)
    // {
    //     // Ensure the ball and targetHex are valid
    //     if (ball == null)
    //     {
    //         Debug.LogError("Ball reference is null in HandleGroundBallMovement!");
    //         yield break;
    //     }
    //     if (targetHex == null)
    //     {
    //         Debug.LogError("Target Hex is null in HandleGroundBallMovement!");
    //         Debug.LogError($"currentTargetHex: {currentTargetHex}, isWaitingForDiceRoll: {isWaitingForDiceRoll}");
    //         yield break;
    //     }
    //     // Set thegame status to StandardPassMoving
    //     MatchManager.Instance.currentState = MatchManager.GameState.StandardPassMoving;
    //     // Wait for the ball movement to complete
    //     yield return StartCoroutine(ball.MoveToCell(targetHex));
    //     // Set the game status to StandardPassCompleted
    //     MatchManager.Instance.currentState = MatchManager.GameState.StandardPassCompleted;
    //     // Now clear the highlights after the movement
    //     ClearHighlightedHexes();
    //     Debug.Log("Highlights cleared after ball movement.");
    // }
    
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
        // ClearHighlightedHexes();
        Debug.Log("Highlights cleared after ball movement.");
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

    // public List<HexCell> CalculateThickPath(HexCell startHex, HexCell endHex, float ballRadius)
    // {
    //     List<HexCell> path = new List<HexCell>();
    //     string logContent = $"Ball Radius: {ballRadius}\n";
    //     string startHexCoordinates = $"({startHex.coordinates.x}, {startHex.coordinates.z})";
    //     string endHexCoordinates = $"({endHex.coordinates.x}, {endHex.coordinates.z})";
    //     logContent += $"Starting Hex: {startHexCoordinates}, Target Hex: {endHexCoordinates}\n";

    //     // Get world positions of the start and end hex centers
    //     Vector3 startPos = startHex.GetHexCenter();
    //     Vector3 endPos = endHex.GetHexCenter();

    //     // Step 2: Get a list of candidate hexes based on the bounding box
    //     List<HexCell> candidateHexes = GetCandidateHexes(startHex, endHex, ballRadius);

    //     // Step 3: Loop through the candidate hexes and check distances to the parallel lines
    //     foreach (HexCell candidateHex in candidateHexes)
    //     {
    //         Vector3 candidatePos = candidateHex.GetHexCenter();

    //         // Check the distance from the candidate hex to the main line
    //         float distanceToLine = DistanceFromPointToLine(candidatePos, startPos, endPos);

    //         if (distanceToLine <= ballRadius)
    //         {
    //             // Hex is within the thick path
    //             if (!path.Contains(candidateHex))
    //             {
    //                 path.Add(candidateHex);
    //                 logContent += $"Added Hex: ({candidateHex.coordinates.x}, {candidateHex.coordinates.z}), Distance to Line: {distanceToLine}, Radius: {ballRadius}\n";
    //             }
    //         }
    //         else
    //         {
    //             logContent += $"Not Added: ({candidateHex.coordinates.x}, {candidateHex.coordinates.z}), Distance: {distanceToLine} exceeds Ball Radius: {ballRadius}\n";
    //         }
    //     }
    //     path.Remove(startHex);
    //     // Log the final highlighted path to the file
    //     string highlightedPath = "Highlighted Path: ";
    //     foreach (HexCell hex in path)
    //     {
    //         highlightedPath += $"({hex.coordinates.x}, {hex.coordinates.z}), ";
    //     }
    //     highlightedPath = highlightedPath.TrimEnd(new char[] { ',', ' ' });
    //     logContent += highlightedPath;

    //     // Save the log to a file
    //     SaveLogToFile(logContent, startHexCoordinates, endHexCoordinates);

    //     return path;
    // }

    // public List<HexCell> GetCandidateHexes(HexCell startHex, HexCell endHex, float ballRadius)
    // {
    //     List<HexCell> candidates = new List<HexCell>();
    //     // Get the axial coordinates of the start and end hexes
    //     Vector3Int startCoords = startHex.coordinates;
    //     Vector3Int endCoords = endHex.coordinates;

    //     // Determine the bounds (min and max x and z)
    //     int minX = Mathf.Min(startCoords.x, endCoords.x) - 1;
    //     int maxX = Mathf.Max(startCoords.x, endCoords.x) + 1;
    //     int minZ = Mathf.Min(startCoords.z, endCoords.z) - 1;
    //     int maxZ = Mathf.Max(startCoords.z, endCoords.z) + 1;

    //     // Loop through all hexes in the bounding box
    //     for (int x = minX; x <= maxX; x++)
    //     {
    //         for (int z = minZ; z <= maxZ; z++)
    //         {
    //             Vector3Int coords = new Vector3Int(x, 0, z);
    //             HexCell hex = hexGrid.GetHexCellAt(coords);

    //             if (hex != null)
    //             {
    //                 candidates.Add(hex);
    //             }
    //         }
    //     }
    //     return candidates;
    // }

    // float DistanceFromPointToLine(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
    // {
    //     Vector3 lineDirection = lineEnd - lineStart;
    //     float lineLength = lineDirection.magnitude;
    //     lineDirection.Normalize();

    //     // Project the point onto the line, clamping between the start and end of the line
    //     float projectedLength = Mathf.Clamp(Vector3.Dot(point - lineStart, lineDirection), 0, lineLength);

    //     // Calculate the closest point on the line
    //     Vector3 closestPoint = lineStart + lineDirection * projectedLength;

    //     // Return the distance from the point to the closest point on the line
    //     return Vector3.Distance(point, closestPoint);
    // }

    public void ClearHighlightedHexes()
    {
        foreach (HexCell hex in highlightedHexes)
        {
            hex.ResetHighlight();  // Assuming there's a method in HexCell to reset the highlight
        }
        highlightedHexes.Clear();  // Clear the list of highlighted hexes
    }

    // void SaveLogToFile(string logText, string startHex, string endHex)
    // {
    //     // // Define the file path (you can customize this path)
    //     // string filePath = Application.dataPath + $"/Logs/HexPath_{startHex}_to_{endHex}.txt";

    //     // // Ensure the directory exists
    //     // Directory.CreateDirectory(Path.GetDirectoryName(filePath));

    //     // // Write the log text to the file (overwrite mode)
    //     // using (StreamWriter writer = new StreamWriter(filePath))
    //     // {
    //     //     writer.WriteLine(logText);
    //     // }

    //     // Debug.Log($"Log saved to: {filePath}");
    // }

    public void TestHexConversions()
    {
        Vector3Int cubeCoords = new Vector3Int(3, -3, 0); // Example cube coordinates
        Vector2Int offsetCoords = HexGridUtils.CubeToOffset(cubeCoords);  // Convert cube to offset (even-q)
        Vector3Int convertedBackToCube = HexGridUtils.OffsetToCube(offsetCoords.x, offsetCoords.y);  // Convert offset back to cube

        Debug.Log($"Cube: {cubeCoords}, Offset: {offsetCoords}, Converted Back to Cube: {convertedBackToCube}");
    }

}
