using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

public class GroundBallManager : MonoBehaviour
{
    [Header("Dependencies")]
    public Ball ball;
    public HexGrid hexGrid;
    public FinalThirdManager finalThirdManager;
    [Header("Runtime Items")]
    private HexCell currentTargetHex = null;   // The currently selected target hex
    private HexCell lastClickedHex = null;     // The last hex that was clicked
    private bool isWaitingForDiceRoll = false; // To check if we are waiting for dice rolls
    private bool passIsDangerous = false;      // To check if the pass is dangerous
    private HexCell currentDefenderHex = null;                      // The defender hex currently rolling the dice
    private List<HexCell> defendingHexes = new List<HexCell>();     // List of defenders responsible for each interception hex
    private List<HexCell> interceptionHexes = new List<HexCell>();  // List of interception hexes
    private int diceRollsPending = 0;          // Number of pending dice rolls

    void Update()
    {
        // Check if waiting for dice rolls and the R key is pressed
        if (isWaitingForDiceRoll && Input.GetKeyDown(KeyCode.R))
        {
            PerformGroundInterceptionDiceRoll();  // Trigger the dice roll when D is pressed
        }
    }
    
    private async Task StartCoroutineAndWait(IEnumerator coroutine)
    {
        bool isDone = false;
        StartCoroutine(WrapCoroutine(coroutine, () => isDone = true));
        await Task.Run(() => { while (!isDone) { } }); // Wait until coroutine completes
    }

    private IEnumerator WrapCoroutine(IEnumerator coroutine, System.Action onComplete)
    {
        yield return StartCoroutine(coroutine);
        onComplete?.Invoke();
    }

    public void HandleGroundBallPath(HexCell clickedHex, int distance, bool isGk = false)
    {
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
                HandleGroundPassBasedOnDifficulty(clickedHex, distance, isGk);
            }   
        }
    }

    public async void HandleGroundPassBasedOnDifficulty(HexCell clickedHex, int distance, bool isGk = false)
    {
        int difficulty = MatchManager.Instance.difficulty_level;  // Get current difficulty
        // Centralized path validation and danger assessment
        var (isValid, isDangerous, pathHexes) = ValidateGroundPassPath(clickedHex, distance, isGk);
        if (!isValid)
        {
            // Debug.LogWarning("Invalid pass. Path rejected.");
            return; // Reject invalid paths
        }
        currentTargetHex = clickedHex;  // Assign the current target hex
        // Handle each difficulty's behavior
        if (difficulty == 3) // Hard Mode
        {
            PopulateGroundPathInterceptions(clickedHex, isGk);
            if (passIsDangerous)
            {
                diceRollsPending = defendingHexes.Count; // is this relevant here?
                Debug.Log($"Dangerous pass detected. Waiting for {diceRollsPending} dice rolls...");
                StartGroundPassInterceptionDiceRollSequence();
            }
            else
            {
                Debug.Log("Pass is not dangerous, moving ball.");
                await StartCoroutineAndWait(HandleGroundBallMovement(clickedHex)); // Execute pass
                finalThirdManager.TriggerFinalThirdPhase();
                MatchManager.Instance.UpdatePossessionAfterPass(clickedHex);
                if (clickedHex.isAttackOccupied)
                {
                    MatchManager.Instance.currentState = MatchManager.GameState.StandardPassCompletedToPlayer;
                }
                else {
                    MatchManager.Instance.currentState = MatchManager.GameState.StandardPassCompletedToSpace;
                }
            }
            ball.DeselectBall();
        }
        else if (difficulty == 2)
        {
            hexGrid.ClearHighlightedHexes();
            HighlightValidGroundPassPath(pathHexes, isDangerous);
            PopulateGroundPathInterceptions(clickedHex, isGk);
            diceRollsPending = defendingHexes.Count; // is this relevant here?
            if (diceRollsPending == 0)
            {
                Debug.Log($"The Stanard pass cannot be intercepted. Click again to confirm or elsewhere to try another path.");
            }
            else 
            {
                Debug.Log($"Dangerous pass detected. If you confirm there will be {diceRollsPending} dice rolls...");
            }
            // Medium Mode: Wait for a second click for confirmation
            if (clickedHex == currentTargetHex && clickedHex == lastClickedHex)
            {
                PopulateGroundPathInterceptions(clickedHex, isGk);
                if (passIsDangerous)
                {
                    diceRollsPending = defendingHexes.Count; // is this relevant here?
                    Debug.Log($"Dangerous pass detected. Waiting for {diceRollsPending} dice rolls...");
                    StartGroundPassInterceptionDiceRollSequence();
                }
                else
                {
                    Debug.Log("Pass is not dangerous, moving ball.");
                    await StartCoroutineAndWait(HandleGroundBallMovement(clickedHex)); // Execute pass
                    finalThirdManager.TriggerFinalThirdPhase();
                    MatchManager.Instance.UpdatePossessionAfterPass(clickedHex);
                    if (clickedHex.isAttackOccupied)
                    {
                        MatchManager.Instance.currentState = MatchManager.GameState.StandardPassCompletedToPlayer;
                    }
                    else {
                        MatchManager.Instance.currentState = MatchManager.GameState.StandardPassCompletedToSpace;
                    }
                }
                ball.DeselectBall();
            }
            else
            {
                hexGrid.ClearHighlightedHexes();
                HighlightValidGroundPassPath(pathHexes, isDangerous);
                currentTargetHex = clickedHex;
                lastClickedHex = clickedHex;  // Set for confirmation click
            }
        }
        else if (difficulty == 1) // Easy Mode: Handle hover and clicks with immediate highlights
        {
            PopulateGroundPathInterceptions(clickedHex, isGk);
            diceRollsPending = defendingHexes.Count; // is this relevant here?
            Debug.Log($"Dangerous pass detected. If you confirm there will be {diceRollsPending} dice rolls...");
            if (clickedHex == currentTargetHex && clickedHex == lastClickedHex)
            {
                // Second click on the same hex: confirm the pass
                Debug.Log("Second click detected, confirming pass...");
                PopulateGroundPathInterceptions(clickedHex, isGk);
                if (passIsDangerous)
                {
                    diceRollsPending = defendingHexes.Count; // is this relevant here?
                    Debug.Log($"Dangerous pass detected. Waiting for {diceRollsPending} dice rolls...");
                    StartGroundPassInterceptionDiceRollSequence();
                }
                else
                {
                    Debug.Log("Pass is not dangerous, moving ball.");
                    await StartCoroutineAndWait(HandleGroundBallMovement(clickedHex)); // Execute pass
                    finalThirdManager.TriggerFinalThirdPhase();
                    MatchManager.Instance.UpdatePossessionAfterPass(clickedHex);
                    if (clickedHex.isAttackOccupied)
                    {
                        MatchManager.Instance.currentState = MatchManager.GameState.StandardPassCompletedToPlayer;
                    }
                    else {
                        MatchManager.Instance.currentState = MatchManager.GameState.StandardPassCompletedToSpace;
                    }
                }
                ball.DeselectBall();
            }
            else
            {
                hexGrid.ClearHighlightedHexes();
                HighlightValidGroundPassPath(pathHexes, isDangerous);
                currentTargetHex = clickedHex; // Set this as the current target hex
                lastClickedHex = clickedHex; // Track the last clicked hex
            }
        }
    }

    public (bool isValid, bool isDangerous, List<HexCell> pathHexes) ValidateGroundPassPath(HexCell targetHex, int distance, bool isGk = false)
    {
        // TODO: 0.0 -> 2.-9 seems valid
        hexGrid.ClearHighlightedHexes();
        HexCell ballHex = ball.GetCurrentHex();
        // Step 1: Ensure the ballHex and targetHex are valid
        if (ballHex == null || targetHex == null)
        {
            Debug.LogError("Ball or target hex is null!");
            return (false, false, null);
        }
        // Step 2: Calculate the path between the ball and the target hex
        List<HexCell> pathHexes = CalculateThickPath(ballHex, targetHex, ball.ballRadius);
        // Get the distance in hex steps
        Vector3Int ballCubeCoords = HexGridUtils.OffsetToCube(ballHex.coordinates.x, ballHex.coordinates.z);
        Vector3Int targetCubeCoords = HexGridUtils.OffsetToCube(targetHex.coordinates.x, targetHex.coordinates.z);
        int distanceBetweenHexes = HexGridUtils.GetHexDistance(ballCubeCoords, targetCubeCoords);
        // Check the distance limit
        if (distanceBetweenHexes > distance)
        {
            Debug.LogWarning($"Pass is out of range. Maximum steps allowed: {distance}. Current steps: {distanceBetweenHexes}");
            return (false, false, pathHexes);
        }
        // Step 3: Check if the path is valid by ensuring no defense-occupied hexes block the path
        if (!isGk)
        {
            foreach (HexCell hex in pathHexes)
            {
                if (hex.isDefenseOccupied)
                {
                    Debug.Log($"Path blocked by defender at hex: {hex.coordinates}");
                    return (false, false, pathHexes); // Invalid path
                }
            }
        }

        // Step 4: Get defenders and their ZOI
        List<HexCell> defenderHexes = hexGrid.GetDefenderHexes();
        List<HexCell> defenderNeighbors = hexGrid.GetDefenderNeighbors(defenderHexes);

        // Step 5: Determine if the path is dangerous by checking if it passes through any defender's ZOI
        bool isDangerous = hexGrid.IsPassDangerous(pathHexes, defenderNeighbors, isGk);

        // Debug.Log($"Path to {targetHex.coordinates}: Valid={true}, Dangerous={isDangerous}");

        return (true, isDangerous, pathHexes);
    }

    public void HighlightValidGroundPassPath(List<HexCell> pathHexes, bool isDangerous)
    {
        foreach (HexCell hex in pathHexes)
        {
            if (hex == null) continue; // to next hex (loop)
            hex.HighlightHex(isDangerous ? "dangerousPass" : "ballPath");
            hexGrid.highlightedHexes.Add(hex);  // Track the highlighted hexes
        }
    }

    public void PopulateGroundPathInterceptions(HexCell targetHex, bool isGk = false)
    {
        HexCell ballHex = ball.GetCurrentHex();  // Get the current hex of the ball
        List<HexCell> pathHexes = CalculateThickPath(ballHex, targetHex, ball.ballRadius);
        string joined = string.Join(" -> ", pathHexes.Select(hex => hex.coordinates.ToString()));  
        Debug.Log($"Path: {joined}");
        hexGrid.ClearHighlightedHexes();
        // Remove the ball's current hex from the path
        pathHexes.Remove(ballHex);

        // Get defenders and their neighbors
        List<HexCell> defenderHexes = hexGrid.GetDefenderHexes();
        List<HexCell> defenderNeighbors = hexGrid.GetDefenderNeighbors(defenderHexes);
        string joineddefenderNeighbors = string.Join(" -> ", defenderNeighbors.Select(hex => hex.coordinates.ToString()));  
        Debug.Log($"All Neighbors: {joineddefenderNeighbors}");
        
        // Initialize danger variables
        passIsDangerous = false;
        interceptionHexes.Clear();
        defendingHexes.Clear();
        // Track defenders that have already been processed
        HashSet<HexCell> alreadyProcessedDefenders = new HashSet<HexCell>();

        // Check if the path crosses any defender's ZOI
        foreach (HexCell hex in pathHexes)
        {
            // Debug.Log($"Checking hex: {hex.coordinates}");
            if (isGk && hex != pathHexes.Last())  // Skip TO the last hex if the target is the GK
            {
              continue;
            }
            // Get the neighbors of the hex and log them for debugging purposes
            HexCell[] neighbors = hex.GetNeighbors(hexGrid);
            string neighborCoords = string.Join(", ", neighbors.Select(n => n?.coordinates.ToString() ?? "null"));
            // Debug.Log($"Neighbors: {neighborCoords}"); // Correct!

            // Debug.Log($"{defenderNeighbors.Contains(hex)}");
            // Check if a defender's neighbor is in the path excluding Attacking occupied Hexes
            if (defenderNeighbors.Contains(hex) && !hex.isAttackOccupied)
            {
                List<HexCell> defendersForThisHex = defenderHexes.Where(d => d.GetNeighbors(hexGrid).Contains(hex)).ToList();
                // Process all such defenders
                foreach (HexCell defenderHex in defendersForThisHex)
                {
                    // Only add the defender and interception hex if the defender hasn't already been processed
                    if (defenderHex != null && !alreadyProcessedDefenders.Contains(defenderHex))
                    {
                        PlayerToken defenderToken = defenderHex.occupyingToken; // Get the token
                        if (defenderToken != null)
                        {
                            string defenderName = defenderToken.playerName;
                            int defenderTackling = defenderToken.tackling;
                            int defenderJersey = defenderToken.jerseyNumber;

                            // Calculate required roll
                            int requiredRoll = defenderTackling >= 4 ? 10 - defenderTackling : 6;
                            string rollDescription = requiredRoll == 6 ? "6" : $"{requiredRoll}+";

                            Debug.Log(
                                $"{defenderJersey}. {defenderName} at {defenderHex.coordinates} with a tackling of {defenderTackling} can intercept with a roll of {rollDescription} at {hex.coordinates}. " +
                                $"Defender's ZOI: {string.Join(", ", defenderHex.GetNeighbors(hexGrid).Select(n => n?.coordinates.ToString() ?? "null"))}"
                            );
                            interceptionHexes.Add(hex);  // Add the interceptable hex
                            defendingHexes.Add(defenderHex);  // Add the defender responsible
                            alreadyProcessedDefenders.Add(defenderHex);  // Mark this defender as processed
                            passIsDangerous = true;  // Mark the pass as dangerous
                            // Debug.Log($"Defender at {defender.coordinates} can intercept at {hex.coordinates}. Defender's ZOI: {string.Join(", ", defender.GetNeighbors(hexGrid).Select(n => n?.coordinates.ToString() ?? "null"))}");
                        }
                    }
                }
            }
            hex.HighlightHex("ballPath");  // Highlight the path
            hexGrid.highlightedHexes.Add(hex);
        }
    }

    void StartGroundPassInterceptionDiceRollSequence()
    {
        Debug.Log($"Defenders with interception chances: {defendingHexes.Count}");
        if (defendingHexes.Count > 0)
        {
            // Start the dice roll process for each defender
            Debug.Log("Starting dice roll sequence... Press R key.");
            // Sort defendingHexes by distance from ballHex
            defendingHexes = defendingHexes.OrderBy(d => HexGridUtils.GetHexDistance(ball.GetCurrentHex().coordinates, d.coordinates)).ToList();
            currentDefenderHex = defendingHexes[0];  // Start with the closest defender
            isWaitingForDiceRoll = true;
        }
        else
        {
            Debug.LogWarning("No defenders in ZOI. This should never appear unless the path is clear.");
            return;
        }
    }

    public async void PerformGroundInterceptionDiceRoll()
    {
        if (currentDefenderHex != null)
        {
            // Roll the dice (1 to 6)
            // int diceRoll = 6; // God Mode
            // int diceRoll = 5; // Stupid Mode
            int diceRoll = Random.Range(1, 7);
            // Retrieve the defender token
            PlayerToken defenderToken = currentDefenderHex.occupyingToken;
            if (defenderToken == null)
            {
                Debug.LogError($"No PlayerToken found on defender's hex at {currentDefenderHex.coordinates}. This should not happen.");
                return;
            }
            Debug.Log($"Dice roll by {defenderToken.jerseyNumber}. {defenderToken.playerName} at {currentDefenderHex.coordinates}: {diceRoll}");
            // Debug.Log($"Dice roll by defender at {currentDefenderHex.coordinates}: {diceRoll}");
            isWaitingForDiceRoll = false;
            // if (diceRoll == 6)
            if (diceRoll == 6 || diceRoll + defenderToken.tackling >= 10)
            {
                // Defender successfully intercepts the pass
                Debug.Log($"Pass intercepted by {defenderToken.jerseyNumber}. {defenderToken.playerName} at {currentDefenderHex.coordinates}!");
                StartCoroutine(HandleBallInterception(currentDefenderHex));
                ResetGroundPassInterceptionDiceRolls();
            }
            else
            {
                Debug.Log($"{defenderToken.jerseyNumber}. {defenderToken.playerName} at {currentDefenderHex.coordinates} failed to intercept.");
                // Move to the next defender, if any
                defendingHexes.Remove(currentDefenderHex);
                if (defendingHexes.Count > 0)
                {
                    currentDefenderHex = defendingHexes[0];  // Move to the next defender
                    Debug.Log("Starting next dice roll sequence... Press R key.");
                    isWaitingForDiceRoll = true; // Wait for the next dice roll
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
                    await StartCoroutineAndWait(HandleGroundBallMovement(currentTargetHex)); // Execute pass
                    finalThirdManager.TriggerFinalThirdPhase();
                    MatchManager.Instance.UpdatePossessionAfterPass(currentTargetHex);
                    if (currentTargetHex.isAttackOccupied)
                    {
                        MatchManager.Instance.currentState = MatchManager.GameState.StandardPassCompletedToPlayer;
                    }
                    else {
                        MatchManager.Instance.currentState = MatchManager.GameState.StandardPassCompletedToSpace;
                    }
                }
            }
        }
    }

    // Create a new coroutine to handle ball movement and update possession after the ball moves
    private IEnumerator HandleBallInterception(HexCell defenderHex)
    {
        yield return StartCoroutine(HandleGroundBallMovement(defenderHex));  // Move the ball to the defender's hex
        // Call UpdatePossessionAfterPass after the ball has moved to the defender's hex
        MatchManager.Instance.ChangePossession();  // Possession is now changed to the other team
        MatchManager.Instance.currentState = MatchManager.GameState.LooseBallPickedUp;
        MatchManager.Instance.UpdatePossessionAfterPass(defenderHex);  // Update possession after the ball has reached the defender's hex
        finalThirdManager.TriggerFinalThirdPhase();
    }

    void ResetGroundPassInterceptionDiceRolls()
    {
        // Reset variables after the dice roll sequence
        defendingHexes.Clear();
        interceptionHexes.Clear();
        diceRollsPending = 0;
        currentDefenderHex = null;
    }

    public IEnumerator HandleGroundBallMovement(HexCell targetHex, int? speed = null)
    {
        // Ensure the ball and targetHex are valid
        if (ball == null)
        {
            Debug.LogError("Ball reference is null in HandleGroundBallMovement!");
            yield break;
        }
        if (targetHex == null)
        {
            Debug.LogError("Target Hex is null in HandleGroundBallMovement!");
            Debug.LogError($"currentTargetHex: {currentTargetHex}, isWaitingForDiceRoll: {isWaitingForDiceRoll}");
            yield break;
        }
        // Set thegame status to StandardPassMoving
        // MatchManager.Instance.currentState = MatchManager.GameState.StandardPassMoving;
        // Wait for the ball movement to complete
        yield return StartCoroutine(ball.MoveToCell(targetHex, speed));
        // Adjust the ball's height based on occupancy (after movement is completed)
        ball.AdjustBallHeightBasedOnOccupancy();  // Ensure this method is public in Ball.cs
        // Now clear the highlights after the movement
        hexGrid.ClearHighlightedHexes();
        Debug.Log("Highlights cleared after ball movement.");
        if (speed != null) yield break;
        // finalThirdManager.TriggerFinalThirdPhase();
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
        List<HexCell> candidateHexes = GetCandidateGroundPathHexes(startHex, endHex, ballRadius);

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

    public List<HexCell> GetCandidateGroundPathHexes(HexCell startHex, HexCell endHex, float ballRadius)
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
}