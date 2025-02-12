using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class LooseBallManager : MonoBehaviour
{
    [Header("Dependencies")]
    public HexGrid hexGrid;
    public Ball ball;
    public OutOfBoundsManager outOfBoundsManager;
    public LongBallManager longBallManager;
    public GroundBallManager groundBallManager;
    public MovementPhaseManager movementPhaseManager;
    public HeaderManager headerManager;
    public FinalThirdManager finalThirdManager;
    [Header("Important Things")]
    public List<PlayerToken> defendersTriedToIntercept;
    public List<HexCell> path = new List<HexCell>();
    public PlayerToken causingDeflection;
    public PlayerToken ballHitThisToken;

    public string TranslateRollToDirection(int direction)
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
            return "Invalid direction";  // This should never Happen
        }
    }
    public IEnumerator ResolveLooseBall(PlayerToken startingToken, string resolutionType)
    {
        causingDeflection = startingToken;
        Debug.Log($"Loose Ball Resolution triggered by {startingToken.name} with resolution type: {resolutionType}");
        path.Clear();
        // Step 1: Move the ball to the starting token's hex
        HexCell defenderHex = startingToken.GetCurrentHex();
        yield return StartCoroutine(groundBallManager.HandleGroundBallMovement(startingToken.GetCurrentHex()));
        // ball.SetCurrentHex(defenderHex);
        List<int> spillDirections = new List<int>();
        if(resolutionType == "handling" && startingToken.IsGoalKeeper)
        {
            if (startingToken.currentHex.isInPenaltyBox == 0) Debug.LogError($"Something is wrong, {startingToken.name} does not seem to be in Penalty Area");
            else if( startingToken.currentHex.isInPenaltyBox == 1 )
            {
                // Right side of the pitch
                spillDirections = new List<int>{1,2};
            }
            else if( startingToken.currentHex.isInPenaltyBox == -1 )
            {
                // Left side of the pitch
                spillDirections = new List<int>{4,5};
            }
        }

        // Step 2: Roll for direction and distance
        // Wait for input to confirm the direction
        yield return StartCoroutine(WaitForInput(KeyCode.R)); 
        // int directionRoll = 0; // S
        // int directionRoll = 1; // SW
        // int directionRoll = 2; // NW
        int directionRoll = 3; // N
        // int directionRoll = 4; // NE
        // int directionRoll = 5; // SE
        // int directionRoll = Random.Range(0, 6); // 0-5 for hex directions

        if(resolutionType == "handling" && startingToken.IsGoalKeeper)
        {
            if (!spillDirections.Contains(directionRoll))
            {
                // HexCell lastInbound;
                // This should be a CornerKick, and we should break
                if (directionRoll == 2 || directionRoll == 3 || directionRoll == 4)
                {
                    // North Direction
                    if (startingToken.currentHex.isInPenaltyBox == 1)
                    {
                        // Right Side
                        yield return StartCoroutine(longBallManager.HandleLongBallMovement(hexGrid.GetHexCellAt(new Vector3Int(22, 0, 6)), true));
                        StartCoroutine(outOfBoundsManager.HandleGoalKickOrCorner(hexGrid.GetHexCellAt(new Vector3Int(18, 0, 6)), "RightGoalLine", "defendertouch"));
                    }
                    else
                    {
                        // Left Side
                        yield return StartCoroutine(longBallManager.HandleLongBallMovement(hexGrid.GetHexCellAt(new Vector3Int(-22, 0, 6)), true));
                        StartCoroutine(outOfBoundsManager.HandleGoalKickOrCorner(hexGrid.GetHexCellAt(new Vector3Int(-18, 0, 6)), "LeftGoalLine", "defendertouch"));
                    }
                }
                else
                {
                    // South Direction
                    if (startingToken.currentHex.isInPenaltyBox == 1)
                    {
                        // Right Side
                        yield return StartCoroutine(longBallManager.HandleLongBallMovement(hexGrid.GetHexCellAt(new Vector3Int(22, 0, -6)), true));
                        StartCoroutine(outOfBoundsManager.HandleGoalKickOrCorner(hexGrid.GetHexCellAt(new Vector3Int(18, 0, -6)), "RightGoalLine", "defendertouch"));
                    }
                    else
                    {
                        // Left Side
                        yield return StartCoroutine(longBallManager.HandleLongBallMovement(hexGrid.GetHexCellAt(new Vector3Int(-22, 0, -6)), true));
                        StartCoroutine(outOfBoundsManager.HandleGoalKickOrCorner(hexGrid.GetHexCellAt(new Vector3Int(-18, 0, -6)), "LeftGoalLine", "defendertouch"));
                    }
                }
                // Just decide where to put the ball and how to trigger the OutOfboundsManager to call the 
                yield break;
            }
        }
        string direction = TranslateRollToDirection(directionRoll);
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
        }
        Debug.Log($"Path: {string.Join(" -> ", path.Select((hex, index) => $"({index}): {hex.coordinates}"))}");

        // Step 5: Check for pickups along the path
        PlayerToken closestToken = null;  // Track the closest token for fallback pickup
        for (int i = 0; i < path.Count; i++)
        {
            HexCell hex = path[i];
            Debug.Log($"Checking hex {hex.coordinates} for tokens...");

            // Step 5.1: Check if there is a token directly on this hex
            PlayerToken tokenOnHex = hex.GetOccupyingToken();
            if (tokenOnHex != null)
            {
                //  If we are resolving a Header Loose ball if we encounter a token that has jumped we should ignore it.
                if (
                    resolutionType == "header" 
                    && (headerManager.defenderWillJump.Contains(tokenOnHex) || headerManager.attackerWillJump.Contains(tokenOnHex))
                )
                {
                    // If we reached the distance roll Hex 
                    // and we landed on a token that has jumped
                    // we extend the distance roll by 1 hex and we check again.
                    if (i == path.Count - 1)
                    {
                        HexCell additionalHex = outOfBoundsManager.CalculateInaccurateTarget(hex, directionRoll, 1);
                        path.Add(additionalHex);
                    }
                    continue;
                }
                else
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
        }

        // If the path is occupied by a jumped token, we need to expand it until we find a free Hex.
        // Up to here closestToken may or may not be null
        
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
                    !headerManager.defenderWillJump.Contains(potentialInterceptor) && // exclude defenders that are in the air // COLIN
                    // !movementPhaseManager.stunnedTokens.Contains(potentialInterceptor) && // exclude defenders that stunned from a nutmeg // COLIN
                    !defendersTriedToIntercept.Contains(potentialInterceptor)) // Ensure the defender hasn't already tried
                {
                    Debug.Log($"{potentialInterceptor.name} is attempting to intercept the ball near {hexround2.coordinates}...");

                    // Roll for interception
                    // Step 5.3: Wait for interception roll
                    // Interception logic (e.g., wait for dice roll input)

                    yield return StartCoroutine(WaitForInterceptionRoll(potentialInterceptor, hexround2));
                    Debug.Log("Press [R] to roll for interception.");
                    // int interceptionRoll = Random.Range(1, 7); // Simulate dice roll
                    int interceptionRoll = 1; // Simulate dice roll
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
                        movementPhaseManager.EndMovementPhase(true);
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
        if (closestToken != null) {path.Add(closestToken.GetCurrentHex());}
        // Step 5.3: If no interception succeeded move the ball to the last Hex of the Path
        yield return StartCoroutine(groundBallManager.HandleGroundBallMovement(path.Last()));
        // Check what is going on with where the ball went.
        // Ball ended up on a Token
        if (closestToken != null)
        {
            ballHitThisToken = closestToken;
            // TODO: resolve based on what created the Loose Ball.
            // Token with Ball is an Attacker
            if (closestToken.isAttacker)
            {
                if (resolutionType == "header")
                {
                    // TODO: ignore offside
                    MatchManager.Instance.currentState = MatchManager.GameState.HeaderCompletedToPlayer;
                    finalThirdManager.TriggerFinalThirdPhase();
                    Debug.Log("Available Options are: [M]ovement Phase, Short [P]ass, [L]ong Ball, [S]napshot");
                }
                else if (!movementPhaseManager.isMovementPhaseInProgress) // TODO: check if there is no Movement Phase going on, Allow Attacker Selection
                {
                    Debug.LogWarning("There is no movement Phase going on, Attacker must choose what to do!");
                    finalThirdManager.TriggerFinalThirdPhase();
                    Debug.Log("Available Options are: [M]ovement Phase, Short [P]ass, [L]ong Ball, [S]napshot");
                }
                else if (movementPhaseManager.isMovementPhaseInProgress)
                {
                    // There is a movement Phase going ON.
                    Debug.Log($"Ball hit {closestToken.name}, who is an attacker");
                    bool isSnapshotAvailable = movementPhaseManager.IsDribblerinOpponentPenaltyBox(closestToken);
                    if (isSnapshotAvailable)
                    {
                        Debug.Log($"{closestToken.name} found themselves with the ball in the opposition penalty Box. Press [S] to take a snapshot!");
                        movementPhaseManager.isWaitingForSnapshotDecision = true;
                        yield break;
                    }
                    else
                    {
                        movementPhaseManager.AdvanceMovementPhase();
                    }
                }
                else
                {
                    Debug.Log("Unknown Scenario");
                }
            }
            else
            {
                Debug.Log($"Ball hit {closestToken.name}, who is a defender");
                // Change possession to the defending team
                MatchManager.Instance.ChangePossession();  
                MatchManager.Instance.UpdatePossessionAfterPass(defenderHex);  // Update possession
                movementPhaseManager.EndMovementPhase(true); // Includes F3 check
                MatchManager.Instance.currentState = MatchManager.GameState.LooseBallPickedUp;
             }
        }
        else if (!path.Last().isOutOfBounds)
        {
            Debug.Log($"Ball did not hit anyone");
            if (resolutionType == "header")
            {
                MatchManager.Instance.currentState = MatchManager.GameState.HeaderCompletedToSpace;
                finalThirdManager.TriggerFinalThirdPhase();
                Debug.Log($"Header Resolved to a Loose Ball, Ball is not in Possesssion. {MatchManager.Instance.teamInAttack} Starts a movement Phase");
                MatchManager.Instance.currentState = MatchManager.GameState.MovementPhaseAttack;
            }
            else if (!movementPhaseManager.isMovementPhaseInProgress)
            {
                finalThirdManager.TriggerFinalThirdPhase();
                Debug.LogWarning($"Loose ball is not picked up by anyone.{MatchManager.Instance.teamInAttack} Starts a movement Phase");
                MatchManager.Instance.currentState = MatchManager.GameState.MovementPhaseAttack;
            }
            else if (movementPhaseManager.isMovementPhaseInProgress)
            {
                Debug.LogWarning($"Loose ball is not picked up by anyone. Current movement Phase continues.");
                movementPhaseManager.AdvanceMovementPhase();
            }
            else
            {
                Debug.Log("Unknown Scenario");
            }
        }
        else
        {
            Debug.Log($"Ball Went out of Bounds");
            if (movementPhaseManager.isMovementPhaseInProgress)
            {
                movementPhaseManager.EndMovementPhase(false);
            }
            outOfBoundsManager.HandleOutOfBounds(startingToken.GetCurrentHex(), directionRoll, "ground");
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

    public void EndLooseBallPhase()
    {
      defendersTriedToIntercept.Clear();
      causingDeflection = null;
      ballHitThisToken = null;
      path.Clear();
    }

}
