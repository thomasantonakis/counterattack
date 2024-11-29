using UnityEngine;

public class PlayerToken : MonoBehaviour
{
    public bool isAttacker;   // Flag to identify if this is an attacker
    public bool isHomeTeam;  // Whether the token belongs to the home team
    public bool hasMoved = false;  // Tracks if the player has moved this turn
    private HexCell currentHex;   // Reference to the current hex this token occupies
    public string playerName;
    public int jerseyNumber;
    public int pace;
    public int dribbling;
    public int highPass;
    public int resilience;
    public int heading; // Only for outfielders
    public int shooting; // Only for outfielders
    public int tackling; // Only for outfielders
    public int aerial; // Only for goalkeepers
    public int saving; // Only for goalkeepers
    public int handling; // Only for goalkeepers

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

    // Method to set attributes from RosterPlayer
    public void InitializeAttributesFromRoster(MatchManager.RosterPlayer rosterPlayer, int jersey)
    {
        playerName = rosterPlayer.name;
        jerseyNumber = jersey;
        pace = rosterPlayer.pace;
        dribbling = rosterPlayer.dribbling;
        highPass = rosterPlayer.highPass;
        resilience = rosterPlayer.resilience;

        // Outfielder-specific attributes
        heading = rosterPlayer.heading;
        shooting = rosterPlayer.shooting;
        tackling = rosterPlayer.tackling;

        // Goalkeeper-specific attributes
        aerial = rosterPlayer.aerial;
        saving = rosterPlayer.saving;
        handling = rosterPlayer.handling;
        Debug.Log($"Initialized attributes for {playerName} (Jersey {jerseyNumber}): Pace: {pace}, Dribbling: {dribbling}, HighPass: {highPass}, Resilience: {resilience}");
    }
}
