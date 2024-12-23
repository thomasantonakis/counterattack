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
    public bool isBooked { get; private set; } = false;
    public bool isInjured { get; private set; } = false;
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
        // TestBookAndInjureTokens();  // Test booking and injuring tokens
    }

    /// <summary>
    /// Method to handle booking the player with a yellow card.
    /// </summary>
    public void ReceiveYellowCard()
    {
        if (!isBooked)
        {
            isBooked = true;
            Debug.Log($"{name} has been booked with a yellow card.");
        }
        else
        {
            Debug.LogWarning($"{name} is already booked. Consider further actions (e.g., red card).");
        }
    }

    /// <summary>
    /// Method to handle when the player is injured.
    /// </summary>
    public void ReceiveInjury()
    {
        if (!isInjured)
        {
            isInjured = true;
            Debug.Log($"{name} has been injured.");
            // Optional: Disable token movement or remove the player from the field
            HandleInjury();
        }
        else
        {
            Debug.LogWarning($"{name} is already injured.");
        }
    }

    /// <summary>
    /// Handles the consequences of injury (e.g., disabling movement or substituting the player).
    /// </summary>
    private void HandleInjury()
    {
        // Example logic for injury (customize as needed)
        pace = Mathf.Max(0, pace - 1);
        dribbling = Mathf.Max(0, dribbling - 1);
        heading = Mathf.Max(0, heading - 1);
        highPass = Mathf.Max(0, highPass - 1);
        resilience = Mathf.Max(0, resilience - 1);
        shooting = Mathf.Max(0, shooting - 1);
        tackling = Mathf.Max(0, tackling - 1);
        Debug.Log($"{name}'s attributes after injury: Heading={heading}, Dribbling={dribbling}, Tackling={tackling}, Pace={pace}, HighPass={highPass}, Shooting={shooting}, Resilience={resilience}");
    }

    public void TestBookAndInjureTokens()
    {
        PlayerToken cafferata = FindPlayerTokenByNameOrID("2. Cafferata"); // Adjust this to your token identification logic
        PlayerToken nazef = FindPlayerTokenByNameOrID("6. Nazef");

        // Check if the tokens were found
        if (cafferata == null)
        {
            Debug.LogError("Token 2. Cafferata not found!");
            return;
        }

        if (nazef == null)
        {
            Debug.LogError("Token 6. Nazef not found!");
            return;
        }

        // Book Cafferata
        Debug.Log($"Booking {cafferata.name}...");
        cafferata.ReceiveYellowCard();
        Debug.Log($"{cafferata.name} is now booked: {cafferata.isBooked}");

        // Injure Nazef
        Debug.Log($"Injuring {nazef.name}...");
        nazef.ReceiveInjury();
        Debug.Log($"{nazef.name} is now injured: {nazef.isInjured}");
    }

    // Helper method to find a token (example implementation, adjust to your game's logic)
    private PlayerToken FindPlayerTokenByNameOrID(string nameOrID)
    {
        PlayerToken[] allTokens = FindObjectsOfType<PlayerToken>();
        foreach (var token in allTokens)
        {
            if (token.name == nameOrID) // Adjust based on your identifier
            {
                return token;
            }
        }
        return null;
    }
}
