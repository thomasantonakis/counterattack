using UnityEngine;
using System.Collections.Generic;

public class MovementPhaseManager : MonoBehaviour
{
    private PlayerToken selectedToken;
    public HexGrid hexGrid;  // Reference to the HexGrid
    public int movementRange = 5;  // Maximum range of movement for a player

    // This method will be called when a player token is clicked
    public void HandleTokenSelection(PlayerToken token)
    {
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

        // Get valid movement hexes (based on movement range and occupation status)
        List<HexCell> validHexes = HexGrid.GetHexesInRange(hexGrid, currentHex, movementRange);

        foreach (HexCell hex in validHexes)
        {
            // Check if the hex is occupied
            if (!hex.isAttackOccupied && !hex.isDefenseOccupied)
            {
                // If the hex is not occupied, highlight it as available for movement
                hex.HighlightHex("PaceAvailable");  // Use your existing highlighting logic
            }
        }
    }

    // This method will deselect the currently selected token
    private void DeselectToken()
    {
        selectedToken = null;
        hexGrid.ClearHighlightedHexes();  // Clear any highlighted hexes when deselecting a token
    }
}
