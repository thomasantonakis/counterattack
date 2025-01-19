using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class LooseBallManager : MonoBehaviour
{
    public HexGrid hexGrid;
    public Ball ball;
    public OutOfBoundsManager outOfBoundsManager;
    public LongBallManager longBallManager;
    public GroundBallManager groundBallManager;
    public List<PlayerToken> defendersTriedToIntercept;

    public IEnumerator ResolveLooseBall(PlayerToken startingToken, string resolutionType)
    {
        Debug.Log($"Loose Ball Resolution triggered by {startingToken.name} with resolution type: {resolutionType}");

        // Step 1: Move the ball to the starting token's hex
        HexCell defenderHex = startingToken.GetCurrentHex();
        yield return StartCoroutine(groundBallManager.HandleGroundBallMovement(startingToken.GetCurrentHex()));
        // ball.SetCurrentHex(defenderHex);

        // Step 2: Roll for direction and distance
        // Wait for input to confirm the direction
        yield return StartCoroutine(WaitForInput(KeyCode.R)); 
        int directionRoll = 3; // 0-5 for hex directions
        // int directionRoll = Random.Range(0, 6); // 0-5 for hex directions
        string direction = longBallManager.TranslateRollToDirection(directionRoll);
        Debug.Log($"Rolled Direction: {direction}");
        yield return StartCoroutine(WaitForInput(KeyCode.R));
        int distanceRoll = 6; // Distance 1-6
        // int distanceRoll = Random.Range(1, 7); // Distance 1-6

        Debug.Log($"Loose Ball Direction: {direction}, Distance: {distanceRoll}");

        // Step 3: Calculate the final target hex
        HexCell finalHex = outOfBoundsManager.CalculateInaccurateTarget(defenderHex, directionRoll, distanceRoll);

        Debug.Log($"Loose Ball target hex: {finalHex.coordinates}");

        // Step 4: Get all hexes in the path from the defender's hex to the final hex
        List<HexCell> path = new List<HexCell>();
        HexCell currentHex = defenderHex;
        for (int i = 0; i < distanceRoll; i++)
        {
            HexCell nextHex = outOfBoundsManager.CalculateInaccurateTarget(defenderHex, directionRoll, i);
            path.Add(nextHex);
            // TODO: Maybe reverse the Path?
            currentHex = nextHex;
        }

        // Step 5: Check for interceptions or pickups along the path
        PlayerToken closestToken = null;  // Track the closest token for fallback pickup
        foreach (HexCell hex in path)
        {
            Debug.Log($"Checking hex {hex.coordinates} for tokens or interceptions...");

            // Step 5.1: Check if there is a token directly on this hex
            PlayerToken tokenOnHex = hex.GetOccupyingToken();
            if (tokenOnHex != null)
            {
                // Store the closest token for fallback pickup
                closestToken = tokenOnHex;
                Debug.Log($"{tokenOnHex.name} encountered on hex {hex.coordinates}. Tracking as fallback for ball pickup.");
                break; // Stop processing further hexes, as the ball can't go further
            }

            // Step 5.2: Check if there are defenders in ZOI of this hex
            foreach (HexCell neighbor in hex.GetNeighbors(hexGrid))
            {
                PlayerToken potentialInterceptor = neighbor.GetOccupyingToken();
                if (potentialInterceptor != null &&
                    potentialInterceptor != startingToken &&
                    !potentialInterceptor.isAttacker &&
                    !defendersTriedToIntercept.Contains(potentialInterceptor)) // Ensure the defender hasn't already tried
                {
                    Debug.Log($"{potentialInterceptor.name} is attempting to intercept the ball near {hex.coordinates}...");

                    // Roll for interception
                    // Step 5.3: Wait for interception roll
                    // Interception logic (e.g., wait for dice roll input)

                    yield return StartCoroutine(WaitForInterceptionRoll(potentialInterceptor, hex));
                    Debug.Log("Press [R] to roll for interception.");
                    // int interceptionRoll = Random.Range(1, 7); // Simulate dice roll
                    int interceptionRoll = 6; // Simulate dice roll
                    if (interceptionRoll == 6)
                    {
                        Debug.Log($"{potentialInterceptor.name} successfully intercepted the ball!");
                        // Move the ball to the interceptor's hex
                        ball.SetCurrentHex(potentialInterceptor.GetCurrentHex());
                        // Change possession
                        MatchManager.Instance.ChangePossession();
                        MatchManager.Instance.UpdatePossessionAfterPass(potentialInterceptor.GetCurrentHex());
                        yield break; // End ball movement
                    }
                    else
                    {
                        Debug.Log($"{potentialInterceptor.name} failed to intercept the ball.");
                        defendersTriedToIntercept.Add(potentialInterceptor); // Mark defender as having tried interception
                    }
                }
            }
        }

        // Step 5.3: If no interception succeeded, the closest token picks up the ball
        if (closestToken != null)
        {
            Debug.Log($"{closestToken.name} picks up the ball by default as all interceptions failed.");
            ball.SetCurrentHex(closestToken.GetCurrentHex());
            MatchManager.Instance.UpdatePossessionAfterPass(closestToken.GetCurrentHex());
        }
        else
        {
            Debug.Log($"Ball remains at {finalHex.coordinates} with no token.");
        }
    }

    private IEnumerator WaitForInput(KeyCode key)
    {
        Debug.Log($"Waiting for input: Press [{key}] to proceed.");
        while (!Input.GetKeyDown(key))
        {
            yield return null; // Wait until the key is pressed
        }
        yield return null;
        Debug.Log($"Input received: [{key}] pressed.");
    }
    private IEnumerator WaitForInterceptionRoll(PlayerToken potentialInterceptor, HexCell hex)
    {
        Debug.Log($"Waiting for input: Press [R] to proceed.");
        while (!Input.GetKeyDown(KeyCode.X))
        {
            yield return null; // Wait until the key is pressed
        }
        yield return null;
    }

}
