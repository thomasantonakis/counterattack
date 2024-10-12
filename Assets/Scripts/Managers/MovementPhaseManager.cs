using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class MovementPhaseManager : MonoBehaviour
{
    private PlayerToken selectedToken;
    private PlayerToken selectedDefender;
    public HexGrid hexGrid;  // Reference to the HexGrid
    public Ball ball;
    public GroundBallManager groundBallManager;
    public int movementRange = 5;  // Maximum range of movement for a player
    private bool isWaitingForInterceptionDiceRoll = false;  // Whether we're waiting for a dice roll
    private bool isWaitingForTackleDecision = false;  // Whether we're waiting for a dice roll
    private bool isWaitingForTackleRoll = false;  // Whether we're waiting for a dice roll
    private bool tackleDefenderRolled = false;  // Whether we're waiting for a dice roll
    private bool tackleAttackerRolled = false;  // Whether we're waiting for a dice roll
    private int defenderDiceRoll;
    private int attackerDiceRoll;
    private List<PlayerToken> movedTokens = new List<PlayerToken>();  // To track moved tokens
    private int attackersMoved = 0;
    private int defendersMoved = 0;
    private int maxAttackerMoves = 4;  // Max moves allowed for attackers
    private int maxDefenderMoves = 5;  // Max moves allowed for defenders
    private int attackersMovedIn2f2 = 0;
    private int maxAttackerMovesIn2f2 = 2;
    private int movementRange2f2 = 2;  // Movement range limited to 2 hexes
    private List<HexCell> defenderHexesNearBall = new List<HexCell>();  // Defenders near the ball
    public bool isPlayerMoving = false;  // Tracks if a player is currently moving


    void Update()
    {
        // Check if waiting for dice rolls and the R key is pressed
        if (isWaitingForInterceptionDiceRoll && Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("R key detected for interception dice roll.");
            PerformBallInterceptionDiceRoll();  // Trigger the dice roll when R is pressed
        }
        // Handle tackle decision (either Tackle or No Tackle)
        else if (isWaitingForTackleDecision)
        {
            if (Input.GetKeyDown(KeyCode.N))  // No tackle
            {
                Debug.Log("No tackle chosen.");
                ResetTacklePhase();  // Reset tackle phase if no tackle is chosen
            }
            else if (Input.GetKeyDown(KeyCode.T))  // Tackle chosen
            {
                Debug.Log("Tackle chosen. Starting tackle dice rolls...");
                isWaitingForTackleDecision = false;
                StartTackleDiceRollSequence();  // Start the dice roll sequence for tackling
            }
        }
        // Check for defender or attacker dice roll
        else if (isWaitingForTackleRoll && Input.GetKeyDown(KeyCode.R))
        {
            if (!tackleDefenderRolled)
            {
                PerformTackleDiceRoll(isDefender: true);  // Defender rolls first
            }
            else if (!tackleAttackerRolled)
            {
                PerformTackleDiceRoll(isDefender: false);  // Attacker rolls second
            }
        }
    }

    // This method will be called when a player token is clicked
    public void HandleTokenSelection(PlayerToken token)
    {
        // Clear previous highlights
        hexGrid.ClearHighlightedHexes();

        // Ensure the token can move in this phase and hasn't already moved
        if (MatchManager.Instance.currentState == MatchManager.GameState.MovementPhaseAttack)
        {
            if (!token.isAttacker || movedTokens.Contains(token))
            {
                Debug.Log("Cannot move this token. Either it's not an attacker or it has already moved.");
                return;  // Reject defender clicks or already moved tokens
            }
        }
        else if (MatchManager.Instance.currentState == MatchManager.GameState.MovementPhaseDef)
        {
            if (token.isAttacker || movedTokens.Contains(token))
            {
                Debug.Log("Cannot move this token. Either it's not a defender or it has already moved.");
                return;  // Reject attacker clicks or already moved tokens
            }
        }
        else if (MatchManager.Instance.currentState == MatchManager.GameState.MovementPhase2f2)
        {
            // Only allow attackers who haven't moved yet in MovementPhaseAtt
            if (!token.isAttacker || movedTokens.Contains(token))
            {
                Debug.Log("This token has already moved or is not an attacker.");
                return;
            }

            // Limit the movement range to 2 hexes
            selectedToken = token;
            Debug.Log($"Selected Token for 2f2: {selectedToken.name}");

            // Highlight valid movement hexes for the selected token with a range of 2 hexes
            HighlightValidMovementHexes(selectedToken, movementRange2f2);
            return;
        }
        // Select the token
        selectedToken = token;
        // Highlight valid movement hexes for the selected token
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

        // Get valid movement hexes and their distance/ZOI data
        var (reachableHexes, distanceData) = HexGridUtils.GetReachableHexes(hexGrid, currentHex, movementRange);

        foreach (HexCell hex in reachableHexes)
        {
            if (!hex.isAttackOccupied && !hex.isDefenseOccupied)
            {
                hexGrid.highlightedHexes.Add(hex);  // Store the valid hex directly in HexGrid's highlightedHexes list

                // Retrieve the distance and ZOI data for the current hex
                var (hexDistance, enteredZOI) = distanceData[hex];

                // Highlight the hex based on ZOI entry and range
                if (hexDistance <= movementRange)
                {
                    hex.HighlightHex("PaceAvailable");  // Normal color for reachable hexes
                }
                else
                {
                    hex.HighlightHex("OutOfRange");  // Mark as out of range
                }
            }
        }
    }

    // Check if the clicked hex is a valid one
    public bool IsHexValidForMovement(HexCell hex)
    {
        bool isValid = hexGrid.highlightedHexes.Contains(hex);  // Check if the clicked hex is in the list of valid hexes
        Debug.Log($"IsHexValidForMovement called for {hex.name}: {isValid}");
        if (!isValid)
        {
            hexGrid.ClearHighlightedHexes();  // Clear the highlights if an invalid hex is clicked
            Debug.Log("Invalid hex clicked. All highlights cleared.");
        }
        return isValid;
    }

    public void MoveTokenToHex(HexCell targetHex, PlayerToken token = null)
    {
        PlayerToken movingToken = token ?? selectedToken;
        if (movingToken == null)
        {
            Debug.LogError("No token selected to move.");
            return;
        }

        // Find the path from the current hex to the target hex
        List<HexCell> path = HexGridUtils.FindPath(movingToken.GetCurrentHex(), targetHex, hexGrid);

        if (path == null || path.Count == 0)
        {
            Debug.LogError("No valid path found to the target hex.");
            return;
        }

        // Start the token movement across the hexes (this can be animated)
        StartCoroutine(MoveTokenAlongPath(movingToken, path));
        
        // Movement for MovementPhase2f2 (the special phase for two attackers)
        if (MatchManager.Instance.currentState == MatchManager.GameState.MovementPhase2f2)
        {
            attackersMovedIn2f2++;
            movedTokens.Add(movingToken);  // Track this token as moved

            if (attackersMovedIn2f2 >= maxAttackerMovesIn2f2)
            {
                Debug.Log("Last two attackers have moved in 2f2 phase. Ending Movement Phase.");
                EndMovementPhase();
            }
        }
        if (MatchManager.Instance.currentState == MatchManager.GameState.MovementPhaseDef)
        {
            defendersMoved++;
            movedTokens.Add(movingToken);  // Track this token as moved

            // Check if we should end the movement phase after defenders move
            if (defendersMoved >= maxDefenderMoves)
            {
                Debug.Log("All defenders have moved. Ready for Movement Phase 2f2.");
                MatchManager.Instance.StartMovementPhase2f2();
            }
        }
        else if (MatchManager.Instance.currentState == MatchManager.GameState.MovementPhaseAttack)
        {
            attackersMoved++;
            movedTokens.Add(movingToken);  // Track this token as moved

            // Check if we should transition to defender phase
            if (attackersMoved >= maxAttackerMoves)
            {
                Debug.Log("All attackers have moved. Switching to Defensive Movement Phase.");
                MatchManager.Instance.StartMovementPhaseDef();
            }
        }
    }

    // Coroutine to move the token one hex at a time
    private IEnumerator MoveTokenAlongPath(PlayerToken token, List<HexCell> path)
    {
        isPlayerMoving = true;  // Player starts moving
        // Get the current Y position of the token (to maintain it during the movement)
        float originalY = token.transform.position.y;
        HexCell previousHex = token.GetCurrentHex();
        if (previousHex != null)
        {
            // Debug.Log($"Token leaving hex: {previousHex.name}");
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
                if (MatchManager.Instance.currentState != MatchManager.GameState.HighPassDefenderMovement && ball.GetCurrentHex() == previousHex)
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
            if (MatchManager.Instance.currentState != MatchManager.GameState.HighPassDefenderMovement && ball.GetCurrentHex() == previousHex)
            {
                ball.SetCurrentHex(step);  // Update ball's hex to the current step
                ball.AdjustBallHeightBasedOnOccupancy();  // Adjust ball's height
            }

            previousHex = step;  // Set the previous hex to the current step for the next iteration
        }
        // Mark the final hex as occupied after the token reaches the destination
        HexCell finalHex = path[path.Count - 1];
        if (token.isAttacker)
        {
            finalHex.isAttackOccupied = true;  // Mark the target hex as occupied by an attacker
        }
        else
        {
            finalHex.isDefenseOccupied = true;  // Mark the target hex as occupied by a defender
        }
        // Debug.Log($"Token arrived at hex: {finalHex.name}");
        // Check if the defender has moved onto the ball's hex
        if (!token.isAttacker && ball.GetCurrentHex() == finalHex && !MatchManager.Instance.attackHasPossession)
        {
            // Defender picks up the loose ball
            Debug.Log("Defender has picked up the loose ball!");
            ball.SetCurrentHex(finalHex);  // Move the ball to the defender's hex
            MatchManager.Instance.ChangePossession();  // Change possession to the defender's team
            MatchManager.Instance.UpdatePossessionAfterPass(finalHex);  // Update possession
            MatchManager.Instance.currentState = MatchManager.GameState.LooseBallPickedUp;  // Update game state
            ResetMovementPhase();  // End the movement phase
        }
        else
        {
            // If the ball is not picked up directly, check for nearby interception or tackle possibility
            // Add the condition to check if we're in the MovementPhaseDef state
            if (MatchManager.Instance.currentState == MatchManager.GameState.MovementPhaseDef)
            {
                HexCell ballHex = ball.GetCurrentHex();
                if (!token.isAttacker && ballHex != null && ballHex != finalHex)
                {
                    HexCell[] neighbors = finalHex.GetNeighbors(hexGrid);

                    foreach (HexCell neighbor in neighbors)
                    {
                        if (neighbor == ballHex)
                        {
                            // If attackHasPossession is false, try interception
                            if (!MatchManager.Instance.attackHasPossession)
                            {
                                Debug.Log("Defender near the ball! Starting dice roll for interception.");
                                StartBallInterceptionDiceRollSequence(finalHex);  // Start the interception dice roll sequence
                            }
                            else
                            {
                                Debug.Log("Defender near the attacker with the ball. Waiting for tackle decision...Press [T]ackle or [N]o Tackle");
                                selectedDefender = token;  // Store the selected defender
                                isWaitingForTackleDecision = true;  // Activate tackle decision listener
                            }
                            break;
                        }
                    }
                }
            }
        }
        // Check if the player landed on the ball hex, adjust the ball height if necessary
        if (finalHex == ball.GetCurrentHex())
        {
            MatchManager.Instance.UpdatePossessionAfterPass(finalHex);
        }
        // Clear highlighted hexes after movement is completed
        hexGrid.ClearHighlightedHexes();
        ball.AdjustBallHeightBasedOnOccupancy();
        isPlayerMoving = false;  // Player finished moving
    }

    private void StartBallInterceptionDiceRollSequence(HexCell defenderHex)
    {
        HexCell ballHex = ball.GetCurrentHex();

        // Find all defenders near the ball
        // defenderHexesNearBall.Clear();
        List<HexCell> defenderHexesNearBall = new List<HexCell>();
        HexCell[] neighbors = ballHex.GetNeighbors(hexGrid);

        foreach (HexCell neighbor in neighbors)
        {
            if (neighbor.isDefenseOccupied)
            {
                defenderHexesNearBall.Add(neighbor);
            }
        }

        if (defenderHexesNearBall.Count > 0)
        {
            Debug.Log("Starting ball interception dice roll sequence... Press R to roll.");
            // currentDefenderHex = defenderHexesNearBall[0];  // Start with the first defender
            // Assign the correct defender token from the neighboring hexes
            // Use FindPlayerTokenOnHex to assign the correct defender
            selectedDefender = FindPlayerTokenOnHex(defenderHexesNearBall[0]);
            if (selectedDefender == null)
            {
                Debug.LogError("Failed to find PlayerToken on the defender hex.");
            }
            else
            {
                Debug.Log($"Selected defender for interception: {selectedDefender.name}");
                isWaitingForInterceptionDiceRoll = true;  // Activate the dice roll listener
            }
            Debug.Log($"isWaitingForInterceptionDiceRoll set to {isWaitingForInterceptionDiceRoll}");
        }
        else
        {
            Debug.LogWarning("No defenders near the ball for interception.");
        }
    }

    public void PerformBallInterceptionDiceRoll()
    {
        Debug.Log("PerformBallInterceptionDiceRoll Runs");
        if (selectedDefender != null)
        {
            // Roll the dice (1 to 6)
            int diceRoll = 6; // God Mode
            // int diceRoll = Random.Range(1, 7);
            Debug.Log($"Dice roll by defender at {selectedDefender.GetCurrentHex().coordinates}: {diceRoll}");
            isWaitingForInterceptionDiceRoll = false;

            if (diceRoll == 6)
            {
                // Defender successfully intercepts the ball
                Debug.Log($"Ball intercepted by defender at {selectedDefender.GetCurrentHex().coordinates}!");
                StartCoroutine(HandleBallInterception(selectedDefender.GetCurrentHex()));
                ResetBallInterceptionDiceRolls();
            }
            else
            {
                Debug.Log($"Defender at {selectedDefender.GetCurrentHex().coordinates} failed to intercept.");
                // Move to the next defender, if any
                defenderHexesNearBall.Remove(selectedDefender.GetCurrentHex());
                if (defenderHexesNearBall.Count > 0)
                {
                    selectedDefender = defenderHexesNearBall[0].GetComponentInChildren<PlayerToken>();  // Move to the next defender
                }
                else
                {
                    Debug.Log("No more defenders to roll.");
                }
            }
        }
        else
        {
            Debug.LogError("selectedDefender is null during dice roll.");
        }
    }

    private IEnumerator HandleBallInterception(HexCell defenderHex)
    {
        yield return StartCoroutine(groundBallManager.HandleGroundBallMovement(defenderHex));  // Move the ball to the defender's hex

        // Change possession to the defending team
        MatchManager.Instance.ChangePossession();  
        MatchManager.Instance.UpdatePossessionAfterPass(defenderHex);  // Update possession
        MatchManager.Instance.currentState = MatchManager.GameState.LooseBallPickedUp;
        ResetMovementPhase();  // Reset the movement phase after interception
    }

    private void PerformTackleDiceRoll(bool isDefender)
    {
        int diceRoll = Random.Range(1, 7);  // Roll a dice (1 to 6)
        
        // Rigged
        if (isDefender)
        {
            defenderDiceRoll = 5;
            tackleDefenderRolled = true;
            Debug.Log($"Defender rolled: {defenderDiceRoll}. Now it's the attacker's turn.");
        }
        else
        {
            attackerDiceRoll = 2;
            tackleAttackerRolled = true;
            Debug.Log($"Attacker rolled: {attackerDiceRoll}. Comparing results...");
            StartCoroutine(CompareTackleRolls());  // Compare the rolls after both rolls are complete
        }
        // Random
        // if (isDefender)
        // {
        //     defenderDiceRoll = diceRoll;
        //     tackleDefenderRolled = true;
        //     Debug.Log($"Defender rolled: {defenderDiceRoll}. Now it's the attacker's turn.");
        // }
        // else
        // {
        //     attackerDiceRoll = diceRoll;
        //     tackleAttackerRolled = true;
        //     Debug.Log($"Attacker rolled: {attackerDiceRoll}. Comparing results...");
        //     StartCoroutine(CompareTackleRolls());  // Compare the rolls after both rolls are complete
        // }
    }

    private IEnumerator CompareTackleRolls()
    {
        isWaitingForTackleRoll = false;  // Stop waiting for rolls

        if (defenderDiceRoll > attackerDiceRoll)
        {
            Debug.Log("Tackle successful! Defender wins possession of the ball.");
            ball.SetCurrentHex(selectedDefender.GetCurrentHex());
            yield return StartCoroutine(groundBallManager.HandleGroundBallMovement(selectedDefender.GetCurrentHex()));  // Move the ball to the defender's hex
            MatchManager.Instance.ChangePossession();  // Change possession to the defender's team
            MatchManager.Instance.UpdatePossessionAfterPass(selectedDefender.GetCurrentHex());  // Update possession
            ResetMovementPhase();  // End the movement phase after successful tackle
            MatchManager.Instance.currentState = MatchManager.GameState.SuccessfulTackle;
            Debug.Log("Movement phase ended due to successful tackle.");
        }
        else
        {
            Debug.Log("Tackle failed. Attacker retains possession.");
        }

        ResetTacklePhase();  // Reset tackle phase after handling results
    }

    private void StartTackleDiceRollSequence()
    {
        // Reset the tackle dice roll flags
        tackleDefenderRolled = false;
        tackleAttackerRolled = false;

        // Set flag to wait for dice rolls
        isWaitingForTackleRoll = true;

        Debug.Log("Press R to roll the dice for tackle. Defender rolls first.");
    }

    private void ResetTacklePhase()
    {
        isWaitingForTackleDecision = false;
        isWaitingForTackleRoll = false;
        tackleDefenderRolled = false;
        tackleAttackerRolled = false;
        Debug.Log("Tackle phase reset.");
    }


    private void ResetMovementPhase()
    {
        movedTokens.Clear();  // Reset the list of moved tokens
        attackersMoved = 0;    // Reset the number of attackers that have moved
        defendersMoved = 0;    // Reset the number of defenders that have moved
        attackersMovedIn2f2 = 0;  // Reset the 2f2 phase counter
        Debug.Log("Movement phase has been reset.");
    }

    private void ResetBallInterceptionDiceRolls()
    {
        defenderHexesNearBall.Clear();
        selectedDefender = null;
        isWaitingForInterceptionDiceRoll = false;
    }

    private void EndMovementPhase()
    {
        MatchManager.Instance.currentState = MatchManager.GameState.MovementPhaseEnded;  // Stop all movements
        ResetMovementPhase();  // Reset the moved tokens and phase counters
        Debug.Log("Movement phase is over.");
    }

    private PlayerToken FindPlayerTokenOnHex(HexCell hex)
    {
        Debug.Log($"Searching for PlayerToken on or around hex: {hex.name}");

        // Use GetOccupyingToken() to retrieve the token on this hex
        PlayerToken token = hex.GetOccupyingToken();

        if (token == null)
        {
            Debug.LogError($"No PlayerToken found on hex {hex.name}. Please ensure PlayerToken is correctly assigned.");
        }
        else
        {
            Debug.Log($"PlayerToken found: {token.name} on hex {hex.name}");
        }

        return token;
}

}
