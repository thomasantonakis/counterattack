using UnityEngine;

public class PlayerToken : MonoBehaviour
{
    public bool isAttacker;   // Flag to identify if this is an attacker
    public bool isHomeTeam;  // Whether the token belongs to the home team
    public bool hasMoved = false;  // Tracks if the player has moved this turn

    private HexCell currentHex;   // Reference to the current hex this token occupies

    void Awake()
    {
        // Debug.Log($"{name}: PlayerToken Awake called");
    }
    // Get the current hex the token is on
    public HexCell GetCurrentHex()
    {
        if (currentHex == null)
        {
            // Debug.LogError($"Token {name} has no current hex assigned!");
        }
        else
        {
            // Debug.Log($"Token {name} is on Hex {currentHex.name}");
        }
        return currentHex;
    }

    // Set the hex where the token will be located
    public void SetCurrentHex(HexCell newHex)
    {
        if (currentHex != null)
        {
            // Clear the occupying token from the previous hex
            currentHex.occupyingToken = null;
        }

        if (newHex != null)
        {
            // Set the occupying token in the new hex
            newHex.occupyingToken = this;
            // Debug.Log($"Token {name} is on Hex {newHex.name}");
        }

        currentHex = newHex;  // Assign the new hex to the token
        UpdateTeamStatusBasedOnHex();  // Update isAttacker based on the hex status
    }

    // Update isAttacker based on the current hex state
    private void UpdateTeamStatusBasedOnHex()
    {
        if (currentHex.isAttackOccupied)
        {
            isAttacker = true;
        }
        else if (currentHex.isDefenseOccupied)
        {
            isAttacker = false;
        }
    }
}
