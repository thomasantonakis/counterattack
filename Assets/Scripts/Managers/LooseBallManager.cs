using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
public class LooseBallManager : MonoBehaviour
{
    public HexGrid hexGrid;
    public Ball ball;
    public OutOfBoundsManager outOfBoundsManager;
    public LongBallManager longBallManager;
    public GroundBallManager groundBallManager;
    public MovementPhaseManager movementPhaseManager;
    public List<PlayerToken> defendersTriedToIntercept;
    public List<HexCell> path = new List<HexCell>();

    public IEnumerator ResolveLooseBall(PlayerToken startingToken, string resolutionType)
    {
        Debug.Log($"Loose Ball Resolution triggered by {startingToken.name} with resolution type: {resolutionType}");
        path.Clear();
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
        // HexCell currentHex = defenderHex;
        for (int i = 0; i < distanceRoll; i++)
        {
            HexCell nextHex = outOfBoundsManager.CalculateInaccurateTarget(defenderHex, directionRoll, i+1);
            Debug.Log($"nextHex: {nextHex.coordinates}");
            path.Add(nextHex);
            // TODO: Maybe reverse the Path
            // currentHex = nextHex;
        }
        Debug.Log($"Path: {string.Join(" -> ", path.Select((hex, index) => $"({index}): {hex.coordinates}"))}");

        // Step 5: Check for interceptions or pickups along the path
        PlayerToken closestToken = null;  // Track the closest token for fallback pickup
        foreach (HexCell hex in path)
        {
            Debug.Log($"Checking hex {hex.coordinates} for tokens...");

            // Step 5.1: Check if there is a token directly on this hex
            PlayerToken tokenOnHex = hex.GetOccupyingToken();
            if (tokenOnHex != null)
            {
                // Store the closest token for fallback pickup
                closestToken = tokenOnHex;
                Debug.Log($"{tokenOnHex.name} encountered on hex {hex.coordinates}. Tracking as fallback for ball pickup.");
                int indexOfClosestHex = path.IndexOf(hex);
                Debug.Log($"{indexOfClosestHex}");
                if (indexOfClosestHex >= 0) // Ensure the hex exists in the path
                {
                    path.RemoveRange(indexOfClosestHex, path.Count - indexOfClosestHex);
                }
                Debug.Log($"Path: {string.Join(" -> ", path.Select((hex, index) => $"({index}): {hex.coordinates}"))}");
                break; // Stop processing further hexes, as the ball can't go further
            }
        }
        
        foreach (HexCell hexround2 in path)
        {
            // Step 5.2: Check if there are defenders in ZOI of this hex
            foreach (HexCell neighbor in hexround2.GetNeighbors(hexGrid))
            {
                PlayerToken potentialInterceptor = neighbor?.GetOccupyingToken();
                if (potentialInterceptor != null && // a token is there
                    potentialInterceptor != startingToken && // not the one who caused the loose ball
                    potentialInterceptor != closestToken && // not the one who is the fallback hit
                    !potentialInterceptor.isAttacker && // exclude all attackers
                    !defendersTriedToIntercept.Contains(potentialInterceptor)) // Ensure the defender hasn't already tried
                {
                    Debug.Log($"{potentialInterceptor.name} is attempting to intercept the ball near {hexround2.coordinates}...");

                    // Roll for interception
                    // Step 5.3: Wait for interception roll
                    // Interception logic (e.g., wait for dice roll input)

                    yield return StartCoroutine(WaitForInterceptionRoll(potentialInterceptor, hexround2));
                    Debug.Log("Press [R] to roll for interception.");
                    // int interceptionRoll = Random.Range(1, 7); // Simulate dice roll
                    int interceptionRoll = 5; // Simulate dice roll
                    if (interceptionRoll == 6 || potentialInterceptor.tackling + interceptionRoll >= 10)
                    {
                        Debug.Log($"{potentialInterceptor.name} successfully intercepted the ball!");
                        // Move the ball to the interceptor's hex
                        ball.SetCurrentHex(potentialInterceptor.GetCurrentHex());
                        // Change possession
                        yield return StartCoroutine(groundBallManager.HandleGroundBallMovement(neighbor));
                        // Change possession to the defending team
                        MatchManager.Instance.ChangePossession();  
                        MatchManager.Instance.UpdatePossessionAfterPass(defenderHex);  // Update possession
                        movementPhaseManager.EndMovementPhase();
                        MatchManager.Instance.currentState = MatchManager.GameState.LooseBallPickedUp;
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
        Debug.Log($"No more interception chances, Moving Ball to the last Hex of the Path");
        if (closestToken != null)
        {path.Add(closestToken.GetCurrentHex());}
        // Step 5.3: If no interception succeeded move the ball to the last Hex of the Path
        yield return StartCoroutine(groundBallManager.HandleGroundBallMovement(path.Last()));
        // Check what is going on with where the ball went.
        // Ball ended up on a Token
        if (closestToken != null)
        {
            // Token with Ball is an Attacker
            if (closestToken.isAttacker)
            {
                Debug.Log($"Ball hit {closestToken.name}, who is an attacker");
                movementPhaseManager.AdvanceMovementPhase();
            }
            else
            {
                Debug.Log($"Ball hit {closestToken.name}, who is a defender");
                // Change possession to the defending team
                MatchManager.Instance.ChangePossession();  
                MatchManager.Instance.UpdatePossessionAfterPass(defenderHex);  // Update possession
                movementPhaseManager.EndMovementPhase();
                MatchManager.Instance.currentState = MatchManager.GameState.LooseBallPickedUp;
            }
        }
        else if (!path.Last().isOutOfBounds)
        {
            Debug.Log($"Ball did not hit anyone");
            movementPhaseManager.AdvanceMovementPhase();
        }
        else
        {
            Debug.Log($"Ball Went out of Bounds");
            outOfBoundsManager.HandleOutOfBoundsFromInaccuracy(startingToken.GetCurrentHex(), directionRoll);
        }
        EndLooseBallPhase();
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
        while (!Input.GetKeyDown(KeyCode.R))
        {
            yield return null; // Wait until the key is pressed
        }
        yield return null;
    }

    private void EndLooseBallPhase()
    {
      defendersTriedToIntercept.Clear();
      path.Clear();
    }

}
