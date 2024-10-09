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
            Debug.LogError($"Token {name} has no current hex assigned!");
        }
        else
        {
            Debug.Log($"Token {name} is on Hex {currentHex.name}");
        }
        return currentHex;
    }

    // Set the hex where the token will be located
    public void SetCurrentHex(HexCell hex)
    {
        if (hex == null)
        {
            Debug.LogError("Hex is null, cannot set current hex for token.");
            return;
        }
        currentHex = hex;
        // Debug.Log($"Token {name} assigned to Hex {currentHex.name}");
        UpdateTeamStatusBasedOnHex();  // Update isAttacker based on the hex status
    }

    // Move the token to the new hex
    public void MoveToHex(HexCell hex)
    {
        SetCurrentHex(hex);
        transform.position = hex.transform.position;  // Move the GameObject to the hex's position
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
