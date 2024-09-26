using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;

public class LongBallManager : MonoBehaviour
{
    public Ball ball;
    public HexGrid hexGrid;
    public GroundBallManager groundBallManager;
    private bool isWaitingForAccuracyRoll = false; // Flag to check for accuracy roll
    private bool isWaitingForDirectionRoll = false; // Flag to check for Direction roll
    private bool isWaitingForDistanceRoll = false; // Flag to check for Distance roll
    private bool isWaitingForInterceptionRoll = false; // Flag to check for Interception Roll After Accuracy Result
    private HexCell currentTargetHex;
    private HexCell clickedHex;
    private HexCell lastClickedHex;
    private int directionIndex;
    private int distance;
    private HexCell finalHex;
    private Dictionary<HexCell, List<HexCell>> interceptionHexToDefendersMap = new Dictionary<HexCell, List<HexCell>>();
    private List<HexCell> interceptingDefenders;
    private List<HexCell> highlightedLongBallHexes = new List<HexCell>();


    // Step 1: Handle the input for starting the long pass (initial logic)
    void Update()
    {   
        // If waiting for accuracy roll
        if (isWaitingForAccuracyRoll && Input.GetKeyDown(KeyCode.D))
        {
            // Debug.Log("Accuracy roll triggered by D key.");
            PerformAccuracyRoll(); // Handle accuracy roll
        }
        else if (isWaitingForDirectionRoll && Input.GetKeyDown(KeyCode.D))
        {
            // Debug.Log("Direction roll triggered by D key.");
            PerformDirectionRoll(); // Handle direction roll
        }
        else if (isWaitingForDistanceRoll && Input.GetKeyDown(KeyCode.D))
        {
            // Debug.Log("Distance roll triggered by D key.");
            PerformDistanceRoll(); // Handle distance roll
        }
        else if (isWaitingForInterceptionRoll && Input.GetKeyDown(KeyCode.D))
        {
            // Debug.Log("Interception roll triggered by D key.");
            StartCoroutine(PerformInterceptionCheck(finalHex)); 
        }
    }
    
    public void HandleLongBallProcess()
    {
        // Debug.Log("Processing long ball...");
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            clickedHex = hit.collider.GetComponent<HexCell>();
            if (clickedHex != null)
            { 
                Debug.Log($"Clicked hex: {clickedHex.coordinates}");
                HexCell ballHex = ball.GetCurrentHex();
                if (ballHex == null)
                {
                    Debug.LogError("Ball's current hex is null! Ensure the ball has been placed on the grid.");
                    return;
                }
                else
                {
                    // Now handle the pass based on difficulty
                    HandleLongBallBasedOnDifficulty(clickedHex);
                }   
            }
        }
    }

    private void HandleLongBallBasedOnDifficulty(HexCell clickedHex)
    {
        int difficulty = MatchManager.Instance.difficulty_level;  // Get current difficulty
        // Debug.Log("Hello from HandleLongBallBasedOnDifficulty");
        // Centralized target validation
        ClearHighlightedHexes();
        bool isValid = ValidateLongBallTarget(clickedHex);
        if (!isValid)
        {
            // Debug.LogWarning("Long Pass target is invalid");
            return; // Reject invalid targets
        }
        // Difficulty-based handling
        if (difficulty == 3) // Hard Mode: Immediate action
        {
            currentTargetHex = clickedHex;  // Assign the current target hex
            isWaitingForAccuracyRoll = true;  // Wait for accuracy roll
            Debug.Log("Waiting for accuracy roll... Please Press D key.");
            // A D is expected to roll for accuracy.
        }
        else if (difficulty == 2) // Medium Mode: Require confirmation with a second click
        {
            if (clickedHex == currentTargetHex && clickedHex == lastClickedHex)  // If it's the same hex clicked twice
            {
                Debug.Log("Long Ball confirmed by second click. Waiting for accuracy roll.");
                isWaitingForAccuracyRoll = true;  // Now ask for the accuracy roll
            }
            else
            {
                // First click: Set the target, highlight the path, and wait for confirmation
                currentTargetHex = clickedHex;
                lastClickedHex = clickedHex;  // Set this as the last clicked hex for confirmation
                ClearHighlightedHexes();

                // You can highlight the path here if you want to provide visual feedback in Medium/Easy modes
                HighlightLongPassArea(clickedHex);  // Optional: Visual feedback
                Debug.Log("First click registered. Click again to confirm the Long Ball.");
            }
        }
        else if (difficulty == 1) // Easy Mode: Require confirmation with a second click
        {
            if (clickedHex == currentTargetHex && clickedHex == lastClickedHex)  // If it's the same hex clicked twice
            {
                Debug.Log("Long Ball confirmed by second click. Waiting for accuracy roll.");
                isWaitingForAccuracyRoll = true;  // Now ask for the accuracy roll
            }
            else
            {
                // First click: Set the target, highlight the path, and wait for confirmation
                currentTargetHex = clickedHex;
                lastClickedHex = clickedHex;  // Set this as the last clicked hex for confirmation
                ClearHighlightedHexes();

                // You can highlight the path here if you want to provide visual feedback in Medium/Easy modes
                HighlightAllValidLongPassTargets();  // Optional: Visual feedback
                Debug.Log("First click registered. Click again to confirm the Long Ball.");
            }
        }
    }

    private bool ValidateLongBallTarget(HexCell targetHex)
    {
        HexCell ballHex = ball.GetCurrentHex();
        // Step 1: Ensure the ballHex and targetHex are valid
        if (ballHex == null || targetHex == null)
        {
            Debug.LogError("Ball or target hex is null!");
            return false;
        }
        // Step 2: Calculate the path between the ball and the target hex
        List<HexCell> pathHexes = groundBallManager.CalculateThickPath(ballHex, targetHex, ball.ballRadius);
        // Step 3: Check if the path is valid by ensuring no defense-occupied hexes block the path
        foreach (HexCell hex in pathHexes) // add here "and in the ball's neighbors"
        {
            if (hex.isDefenseOccupied && ballHex.GetNeighbors(hexGrid).Contains(hex))
            {
                Debug.Log($"Path blocked by defender at hex: {hex.coordinates}");
                return false; // Invalid path
            }
        }
        // Step 4: Get defenders and their ZOI
        List<HexCell> defenderHexes = hexGrid.GetDefenderHexes();
        List<HexCell> defenderNeighbors = hexGrid.GetDefenderNeighbors(defenderHexes);
        // add something here to exclude all defenderHexes and defenderNeighbors above from valid targets
        List<HexCell> attackerHexes = hexGrid.GetAttackerHexes();
        List<HexCell> invalidHexesinRangeOfAttackers = hexGrid.GetAttackerHexesinRange(attackerHexes, 5);
        // Maybe remove from defenderNeighbors all Hexes that exist in defenderHexes
        // Maybe remove from invalidHexesinRangeOfAttackers all Hexes that exist in attackerHexes
        // So that the below debugs make sense.
        if (defenderHexes.Contains(targetHex) || defenderNeighbors.Contains(targetHex))
        {
            Debug.Log("You cannot target a Long Ball on a Defender or in their ZOI");
            return false; // Invalid path
        }
        if (attackerHexes.Contains(targetHex) || invalidHexesinRangeOfAttackers.Contains(targetHex))
        {
            Debug.Log("You cannot target a Long Ball on an Attacker or 5 Hexes away from any Attacker");
            return false; // Invalid path
        }
        return true;
    }

    private void PerformAccuracyRoll()
    {
        // Placeholder for dice roll logic (will be expanded in later steps)
        // Debug.Log("Performing accuracy roll for Long Pass.");
        // Roll the dice (1 to 6)
        int diceRoll = Random.Range(1, 2); // Melina Mode
        // int diceRoll = Random.Range(1, 7);
        isWaitingForAccuracyRoll = false;
        if (diceRoll > 4)
        {
            Debug.Log($"Long Ball is accurate, passer roll: {diceRoll}");
            // Move the ball to the intended target
            StartCoroutine(HandleLongBallMovement(clickedHex));
            ResetLongPassRolls();  // Reset flags to finish long pass
        }
        else
        {
            Debug.Log($"Long Ball is NOT accurate, passer roll: {diceRoll}");
            isWaitingForDirectionRoll = true;
            Debug.Log("Waiting for Direction roll... Please Press D key.");
        }
    }

    private void PerformDirectionRoll()
    {
        // Debug.Log("Performing Direction roll to find Long Pass destination.");
        int diceRoll = Random.Range(0, 1); // Melina Mode
        // int diceRoll = Random.Range(0, 6);
        directionIndex = diceRoll;  // Set the direction index for future use
        int diceRollLabel = diceRoll + 1;
        string rolledDirection = TranslateRollToDirection(diceRoll);
        Debug.Log($"Rolled {diceRollLabel}: Moving in {rolledDirection} direction");
        isWaitingForDirectionRoll = false;
        isWaitingForDistanceRoll = true;
        Debug.Log("Waiting for Distance roll... Please Press D key.");
    }

    string TranslateRollToDirection(int direction)
    {
        switch (direction)
        {
          case 0:
            return "South";
          case 1:
            return "SouthWest";
          case 2:
            return "NorthWest";
          case 3:
            return "North";
          case 4:
            return "NorthEast";
          case 5:
            return "SouthEast";
          default:
            return "Invalid direction";  // This han
        }
    }

    void PerformDistanceRoll()
    {
        // Debug.Log("Performing Direction roll to find Long Pass destination.");
        int distanceRoll = Random.Range(3, 4); // Melina Mode
        // int distanceRoll = Random.Range(1, 7);
        isWaitingForDistanceRoll = false;
        Debug.Log($"Distance Roll: {distanceRoll} hexes away from target.");
        // Calculate the final target hex based on the direction and distance
        HexCell finalHex = CalculateInaccurateTarget(currentTargetHex, directionIndex, distanceRoll);
        // Check if the final hex is valid (not out of bounds or blocked)
        if (finalHex != null)
        {
            // Move the ball to the inaccurate final hex
            StartCoroutine(HandleLongBallMovement(finalHex));
        }
        else
        {
            Debug.LogWarning("Final target is invalid!");
        }
        ResetLongPassRolls();  // Reset flags to finish long pass
    }

    private void ResetLongPassRolls()
    {
        isWaitingForAccuracyRoll = false;
        isWaitingForDirectionRoll = false;
        isWaitingForDistanceRoll = false;
    }

    private HexCell CalculateInaccurateTarget(HexCell startHex, int directionIndex, int distance)
    {
        Vector3Int currentPosition = startHex.coordinates;  // Start from the current hex
        
        for (int i = 0; i < distance; i++)
        {
            // Use the GetDirectionVectors() method to get the correct direction for the current position
            Vector2Int[] directionVectors = hexGrid.GetHexCellAt(currentPosition).GetDirectionVectors();
            Vector2Int direction2D = directionVectors[directionIndex];
            // Move one step in the selected direction
            int newX = currentPosition.x + direction2D.x;
            int newZ = currentPosition.z + direction2D.y;
            // Update the current position
            currentPosition = new Vector3Int(newX, 0, newZ);
        }
        // Find the final hex based on the calculated position
        HexCell finalHex = hexGrid.GetHexCellAt(currentPosition);
        // Log the final hex for debugging
        if (finalHex != null)
        {
            Debug.Log($"Inaccurate final hex: ({finalHex.coordinates.x}, {finalHex.coordinates.z})");
        }
        else
        {
            Debug.LogWarning("Final hex is null or out of bounds!");
        }
        return finalHex;
    }

    private IEnumerator HandleLongBallMovement(HexCell targetHex)
    {
        if (targetHex == null)
        {
            Debug.LogError("Target Hex is null in HandleLongBallMovement!");
            yield break;
        }
        Vector3 startPosition = ball.transform.position;
        Vector3 targetPosition = targetHex.GetHexCenter();
        float travelDuration = 2.0f;  // Duration of the ball's flight
        float elapsedTime = 0;
        float height = 10f;// Height of the arc for the aerial trajectory
        // isMoving = true;

        while (elapsedTime < travelDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / travelDuration;
            // Lerp position along the straight line
            Vector3 flatPosition = Vector3.Lerp(startPosition, targetPosition, progress);
            // // Add the arc (use a sine curve to create the arc)
            flatPosition.y += height * Mathf.Sin(Mathf.PI * progress);
            // // Combine the flat position with the height offset to create the arc
            ball.transform.position = flatPosition;
            yield return null;  // Wait for the next frame
        }
        // isMoving = false;  // Stop the movement
        // Ensure the ball ends exactly on the target hex
        ball.PlaceAtCell(targetHex);
        Debug.Log($"Ball has reached its destination: {targetHex.coordinates}.");

        // Ball has landed, check for defender's ZOI interception
        CheckForLongBallInterception(targetHex);
        // Allow GK Movement
        // And Check Again
        // CheckForLongBallInterception(targetHex);
    }

    private void CheckForLongBallInterception(HexCell landingHex)
    {
        // Get all defenders and their ZOIs (neighbors)
        List<HexCell> defenderHexes = hexGrid.GetDefenderHexes();
        List<HexCell> defenderNeighbors = hexGrid.GetDefenderNeighbors(defenderHexes);
        // Initialize the interceptingDefenders list to avoid null reference
        interceptingDefenders = new List<HexCell>();

        // Check if the landing hex is in any defender's ZOI (neighbors)
        if (defenderNeighbors.Contains(landingHex))
        {
                // Log for debugging: Confirm the landing hex and defender neighbors
            Debug.Log($"Landing hex {landingHex.coordinates} is in defender ZOI. Checking eligible defenders...");

            // Get defenders who have the landing hex in their ZOI
            foreach (HexCell defender in defenderHexes)
            {
                HexCell[] neighbors = defender.GetNeighbors(hexGrid);
                // Debug.Log($"Defender at {defender.coordinates} has neighbors: {string.Join(", ", neighbors.Select(n => n?.coordinates.ToString() ?? "null"))}");

                if (neighbors.Contains(landingHex))
                {
                    Debug.Log($"Defender at {defender.coordinates} can intercept at {landingHex.coordinates}");
                    interceptingDefenders.Add(defender);  // Add the eligible defender to the list
                }
            }
            // Check if there are any intercepting defenders
            if (interceptingDefenders.Count > 0)
            {
                Debug.Log($"Found {interceptingDefenders.Count} defender(s) eligible for interception.");
                isWaitingForInterceptionRoll = true;
            }
            else
            {
                Debug.Log("No defenders eligible for interception. Ball lands without interception.");
            }
        }
        else
        {
            Debug.Log("Landing hex is not in any defender's ZOI. No interception needed.");
        }
    }


    /// Perform the dice roll sequence for each defender in the interception hex// Perform the dice roll sequence for each defender in the interception hex
    private IEnumerator PerformInterceptionCheck(HexCell landingHex)
    {
        if (interceptingDefenders == null || interceptingDefenders.Count == 0)
        {
            Debug.Log("No defenders available for interception.");
            yield break;
        }

        foreach (HexCell defenderHex in interceptingDefenders)
        {
            Debug.Log($"Checking interception for defender at {defenderHex.coordinates}");
            // Roll the dice (1 to 6)
            int diceRoll = Random.Range(6, 7); // Ensure proper range (1-6)
            Debug.Log($"Dice roll for defender at {defenderHex.coordinates}: {diceRoll}");

            if (diceRoll == 6)
            {
                Debug.Log($"Defender at {defenderHex.coordinates} successfully intercepted the ball!");
                isWaitingForInterceptionRoll = false;
                // Move the ball to the defender's hex and change possession
                StartCoroutine(ball.MoveToCell(defenderHex));
                MatchManager.Instance.ChangePossession();
                yield break;  // Stop the sequence once an interception is successful
            }
            else
            {
                Debug.Log($"Defender at {defenderHex.coordinates} failed to intercept the ball.");
            }
        }

        // If no defender intercepts, the ball stays at the original hex
        Debug.Log("No defenders intercepted. Ball remains at the landing hex.");
    }

    public void HighlightLongPassArea(HexCell targetHex)
    {
        ClearHighlightedHexes();
        if (targetHex == null)
        {
            Debug.LogError("Target hex is null in HighlightLongPassArea!");
            return;
        }
        // Initialize highlightedHexes to ensure it's ready for use
        highlightedLongBallHexes = new List<HexCell>();
        // Get hexes within a radius (e.g., 6 hexes) around the targetHex
        int radius = 5;  // You can tweak this value as needed
        List<HexCell> hexesInRange = HexGrid.GetHexesInRange(hexGrid, targetHex, radius);
        if (hexesInRange == null || hexesInRange.Count == 0)
        {
            Debug.LogError("No hexes found in range for highlighting.");
            return;
        }

        // Loop through the hexes and highlight each one
        foreach (HexCell hex in hexesInRange)
        {
            if (hex == null)
            {
                Debug.LogWarning("Encountered a null hex while highlighting, skipping this hex.");
                continue;  // Skip null hexes
            }

            if (hex.isOutOfBounds || hex.isDefenseOccupied)
            {
                Debug.LogWarning($"Hex {hex.coordinates} is out of bounds, skipping highlight.");
                continue;  // Skip out of bounds hexes
            }

            // Highlight hexes (use a specific color for Long Pass)
            hex.HighlightHex("longPass");  // Assuming HexHighlightReason.LongPass is defined for long pass highlights
            highlightedLongBallHexes.Add(hex);  // Track the highlighted hexes for later clearing

            Debug.Log($"Highlighted Hex at coordinates: ({hex.coordinates.x}, {hex.coordinates.z})");
        }

        // Log the highlighted hexes if needed (optional)
        Debug.Log($"Highlighted {hexesInRange.Count} hexes around the target for a Long Pass.");
    }

    public void HighlightAllValidLongPassTargets()
    {
        // Clear the previous highlights
        ClearHighlightedHexes();

        // Loop through all hexes on the grid
        foreach (HexCell hex in hexGrid.cells)
        {
            if (hex == null  || hex.isOutOfBounds) continue;  // Skip null hexes

            // Check if the hex is a valid target
            bool isValid = ValidateLongBallTarget(hex);

            if (isValid)
            {
                hex.HighlightHex("longPass");  // Highlight the valid hexes
                highlightedLongBallHexes.Add(hex);  // Track highlighted hexes for later clearing
            }
        }

        Debug.Log($"Successfully highlighted {highlightedLongBallHexes.Count} valid hexes for Long Pass.");
    }

    private void ClearHighlightedHexes()
    {
        foreach (HexCell hex in highlightedLongBallHexes)
        {
            hex.ResetHighlight();  // Assuming there's a method in HexCell to reset the highlight
        }
        highlightedLongBallHexes.Clear();  // Clear the list of highlighted hexes
    }


    // Implement other methods for handling the long pass
}
