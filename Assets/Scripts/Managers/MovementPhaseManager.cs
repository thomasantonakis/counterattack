using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class MovementPhaseManager : MonoBehaviour
{
    private PlayerToken selectedToken;
    public HexGrid hexGrid;  // Reference to the HexGrid
    public Ball ball;
    public int movementRange = 5;  // Maximum range of movement for a player

    // This method will be called when a player token is clicked
    public void HandleTokenSelection(PlayerToken token)
    {
        // Clear previous highlights
        hexGrid.ClearHighlightedHexes();
        if (selectedToken != null)
        {
            // If a token is already selected, deselect it first
            DeselectToken();
        }

        selectedToken = token;
        Debug.Log($"Selected Token: {selectedToken.name}");

        // Highlight valid movement hexes
        HighlightValidMovementHexes(selectedToken, movementRange);
    }

    // This method will highlight valid movement hexes for the selected token
    private void HighlightValidMovementHexes(PlayerToken token, int movementRange)
    {
        HexCell currentHex = token.GetCurrentHex();  // Get the hex the token is currently on
        if (currentHex == null)
        {
            Debug.LogError("Selected token does not have a valid hex!");
            return;
        }

        // Clear any previously highlighted hexes before highlighting new ones
        hexGrid.ClearHighlightedHexes();
        // Get valid movement hexes (based on movement range and occupation status)
        List<HexCell> validHexes = HexGrid.GetHexesInRange(hexGrid, currentHex, movementRange);

        foreach (HexCell hex in validHexes)
        {
            // Check if the hex is occupied
            if (!hex.isAttackOccupied && !hex.isDefenseOccupied)
            {
                hexGrid.highlightedHexes.Add(hex);  // Store the valid hex directly in HexGrid's highlightedHexes list
                // If the hex is not occupied, highlight it as available for movement
                hex.HighlightHex("PaceAvailable");  // Use your existing highlighting logic
            }
        }
    }

    // Check if the clicked hex is a valid one
    public bool IsHexValidForMovement(HexCell hex)
    {
        bool isValid = hexGrid.highlightedHexes.Contains(hex);  // Check if the clicked hex is in the list of valid hexes
        Debug.Log($"IsHexValidForMovement called for {hex.name}: {isValid}");
        // If the hex is not valid, clear all highlights
        if (!isValid)
        {
            hexGrid.ClearHighlightedHexes();  // Clear the highlights if an invalid hex is clicked
            Debug.Log("Invalid hex clicked. All highlights cleared.");
        }
        return isValid;
    }

    public void MoveTokenToHex(HexCell targetHex)
    {
        if (selectedToken == null)
        {
            Debug.LogError("No token selected to move.");
            return;
        }

        // Find the path from the current hex to the target hex
        List<HexCell> path = HexGridUtils.FindPath(selectedToken.GetCurrentHex(), targetHex, hexGrid);

        if (path == null || path.Count == 0)
        {
            Debug.LogError("No valid path found to the target hex.");
            return;
        }


        // Start the token movement across the hexes (this can be animated)
        StartCoroutine(MoveTokenAlongPath(selectedToken, path));
    }

    // Coroutine to move the token one hex at a time
    private IEnumerator MoveTokenAlongPath(PlayerToken token, List<HexCell> path)
    // private IEnumerator MoveTokenAlongPath(PlayerToken token, List<HexCell> path)
    {
        // Get the current Y position of the token (to maintain it during the movement)
        float originalY = token.transform.position.y;
        HexCell previousHex = token.GetCurrentHex();
        if (previousHex != null)
        {
            // Debug.Log($"Token leaving hex: {previousHex.name}");
            // Update the previous hex to no longer be occupied
            previousHex.isAttackOccupied = false;
            previousHex.isDefenseOccupied = false;
            previousHex.ResetHighlight();
        }
        else
        {
            Debug.LogError("Previous hex is null. Token might not be assigned to a valid hex.");
        }
        // Loop through each hex in the path
        foreach (HexCell step in path)
        {
            Vector3 startPosition = token.transform.position;  // Starting position for the current hex
            Vector3 targetPosition = new Vector3(step.GetHexCenter().x, originalY, step.GetHexCenter().z);  // Target position for the next hex
            float t = 0;  // Timer for smooth transition
            float moveDuration = 0.3f;  // Duration of the movement between hexes
            // Smoothly move the token between hexes
            while (t < 1f)
            {
                t += Time.deltaTime / moveDuration;
                token.transform.position = Vector3.Lerp(startPosition, targetPosition, t);  // Interpolate the position
                // If the player is carrying the ball, move the ball alongside the player
                if (ball.GetCurrentHex() == previousHex)
                {
                    // Move the ball alongside the player, keeping the correct Y offset
                    Vector3 ballPosition = new Vector3(token.transform.position.x, ball.playerHeightOffset, token.transform.position.z);
                    ball.transform.position = ballPosition;  // Move the ball along with the token
                }
                yield return null;  // Wait for the next frame
            }
            // Update the token's hex after reaching the next hex
            token.SetCurrentHex(step);

            // If the player is carrying the ball, move the ball along with the player
            if (ball.GetCurrentHex() == previousHex)
            {
                ball.SetCurrentHex(step);  // Update ball's hex to the current step
                ball.AdjustBallHeightBasedOnOccupancy();  // Adjust ball's height
            }

            previousHex = step;  // Set the previous hex to the current step for the next iteration
        }
        // // If the player is carrying the ball (ball is on their previous hex), move the ball
        // if (ball.GetCurrentHex() == previousHex)
        // {
        //     ball.SetCurrentHex(targetHex);  // Update the ball's hex to the new player hex
        //     ball.AdjustBallHeightBasedOnOccupancy();  // Adjust the ball's height automatically
        // }
        // Mark the final hex as occupied after the token reaches the destination
        HexCell finalHex = path[path.Count - 1];
        // Set the token's new position to the final hex
        // token.SetCurrentHex(finalHex);
        finalHex.isAttackOccupied = true;  // Mark the target hex as occupied
        // Debug.Log($"Token arrived at hex: {finalHex.name}");
        // Check if the player landed on the ball hex, adjust the ball height if necessary
        if (finalHex == ball.GetCurrentHex())
        {
            ball.AdjustBallHeightBasedOnOccupancy();  // Adjust ball's position based on occupancy
        }
        // Clear highlighted hexes after movement is completed
        hexGrid.ClearHighlightedHexes();
    }


    // This method will deselect the currently selected token
    private void DeselectToken()
    {
        selectedToken = null;
        hexGrid.ClearHighlightedHexes();  // Clear any highlighted hexes when deselecting a token
    }
}
