using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;

public class LongBallManager : MonoBehaviour
{
    public Ball ball;
    public HexGrid hexGrid;
    private bool isWaitingForAccuracyRoll = false; // Flag to check for accuracy roll
    private bool isWaitingForDirectionRoll = false; // Flag to check for Direction roll
    private bool isWaitingForDistanceRoll = false; // Flag to check for Distance roll
    // private bool isAccuracySuccessful = false;    // Track if the pass was accurate
    private HexCell currentTargetHex;
    private HexCell clickedHex;
    private int directionIndex;
    private int distance;


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
        bool isValid = ValidateLongBallTarget(clickedHex);
        if (!isValid)
        {
            Debug.LogWarning("Long Pass target is invalid");
            return; // Reject invalid targets
        }
        currentTargetHex = clickedHex;  // Assign the current target hex
        // Generic behavior
        isWaitingForAccuracyRoll = true;
        Debug.Log("Waiting for accuracy roll... Please Press D key.");
        // A D is expected to roll for accuracy.
        
        
        
        // // Handle each difficulty's behavior
        // if (difficulty == 3) // Hard Mode
        // {
        // }
    }

    private bool ValidateLongBallTarget(HexCell hexCell)
    {
        return true;
    }

    private void PerformAccuracyRoll()
    {
        // Placeholder for dice roll logic (will be expanded in later steps)
        // Debug.Log("Performing accuracy roll for Long Pass.");
        // Roll the dice (1 to 6)
        int diceRoll = Random.Range(1, 2);
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
        int diceRoll = Random.Range(1, 2);
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
        int distanceRoll = Random.Range(6, 7);
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
        Debug.Log($"Moving ball to target hex: {targetHex.coordinates}");
        yield break;
    }

    // public void HighlightLongPassArea(HexCell targetHex)
    // {
    //     // Get hexes within a radius (e.g., 6 hexes) around the targetHex
    //     int radius = 5;  // You can tweak this value as needed
    //     List<HexCell> hexesInRange = HexGrid.GetHexesInRange(hexGrid, targetHex, radius);

    //     // Loop through the hexes and highlight each one
    //     foreach (HexCell hex in hexesInRange)
    //     {
    //         // Highlight hexes (pass a specific color for Long Pass)
    //         hex.HighlightHex("longPass");  // Assuming HexHighlightReason.LongPass is defined for long pass highlights
    //         highlightedHexes.Add(hex);  // Track the highlighted hexes for later clearing
    //     }

    //     // Log the highlighted hexes if needed (optional)
    //     Debug.Log($"Highlighted {hexesInRange.Count} hexes around the target for a Long Pass.");
    // }

    // Implement other methods for handling the long pass
}
