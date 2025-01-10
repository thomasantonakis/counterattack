using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine.Analytics;

public class MovementPhaseManager : MonoBehaviour
{
    public PlayerToken selectedToken;
    private PlayerToken selectedDefender;
    public HexGrid hexGrid;  // Reference to the HexGrid
    public Ball ball;
    public GroundBallManager groundBallManager;
    public HeaderManager headerManager;
    public FreeKickManager freeKickManager;
    public bool isWaitingForInterceptionDiceRoll = false;  // Whether we're waiting for a dice roll
    public bool isWaitingForTackleDecision = false;  // Whether we're waiting for a dice roll
    public bool isWaitingForTackleDecisionWithoutMoving = false; // Flag to check if waiting for tackle decision
    public bool isWaitingForTackleRoll = false;  // Whether we're waiting for a dice roll
    public bool isWaitingForReposition = false;  // Whether we're waiting for a dice roll
    private bool tackleAttackerRolled = false;  // Whether we're waiting for a dice roll
    private bool tackleDefenderRolled = false;  // Whether we're waiting for a dice roll
    private bool isWaitingForYellowCardRoll = false;  // Whether we're waiting for a dice roll
    private bool isWaitingForInjuryRoll = false;  // Whether we're waiting for a dice roll
    private bool isWaitingForFoulDecision = false;  // Whether we're waiting for a dice roll
    private int remainingDribblerPace; // Temporary variable for dribbler's pace
    private List<PlayerToken> defendersTriedToIntercept = new List<PlayerToken>(); // Temporary list of defenders
    private bool isDribblerRunning; // Flag to indicate ongoing dribbler movement
    private int defenderDiceRoll;
    private int attackerDiceRoll;
    [SerializeField]
    private List<PlayerToken> movedTokens = new List<PlayerToken>();  // To track moved tokens
    private List<PlayerToken> eligibleDefenders = new List<PlayerToken>();  // To track defenders eligible for interception
    [SerializeField]
    private int attackersMoved = 0;
    [SerializeField]
    private int defendersMoved = 0;
    [SerializeField]
    private int attackersMovedIn2f2 = 0;
    private int maxAttackerMoves = 4;  // Max moves allowed for attackers
    private int maxDefenderMoves = 5;  // Max moves allowed for defenders
    private int maxAttackerMovesIn2f2 = 2;
    private int movementRange2f2 = 2;  // Movement range limited to 2 hexes
    private List<HexCell> defenderHexesNearBall = new List<HexCell>();  // Defenders near the ball
    public bool isPlayerMoving = false;  // Tracks if a player is currently moving
    private const int FOUL_THRESHOLD = 1;  // Below this one is a foul
    private const int INTERCEPTION_THRESHOLD = 10;  // Below this one is a foul

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
        else if (isWaitingForTackleDecisionWithoutMoving)
        {
            if (Input.GetKeyDown(KeyCode.T))
            {
                // Defender chooses to tackle
                Debug.Log($"{selectedToken.name} initiates a tackle without moving. Starting Dice Rolls...");
                movedTokens.Add(selectedToken); // Mark defender as having moved
                isWaitingForTackleDecisionWithoutMoving = false; // Reset tackle decision flag
                isWaitingForTackleDecision = false;  // Reset tackle decision flag
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
            if (!token.isAttacker || movedTokens.Contains(token) || headerManager.attackerWillJump.Contains(token) || headerManager.defenderWillJump.Contains(token))
            {
                Debug.Log("Cannot move this token. Either it's not an attacker or it has already moved or is frozen due to previous header challenge.");
                return;  // Reject defender clicks or already moved tokens
            }
        }
        else if (MatchManager.Instance.currentState == MatchManager.GameState.MovementPhaseDef)
        {
            if (token.isAttacker || movedTokens.Contains(token) || headerManager.attackerWillJump.Contains(token) || headerManager.defenderWillJump.Contains(token))
            {
                Debug.Log("Cannot move this token. Either it's not a defender or it has already moved or is frozen due to previous header challenge.");
                return;  // Reject attacker clicks or already moved tokens
            }
        }
        else if (MatchManager.Instance.currentState == MatchManager.GameState.MovementPhase2f2)
        {
            // Only allow attackers who haven't moved yet in MovementPhaseAtt
            if (!token.isAttacker || movedTokens.Contains(token) || headerManager.attackerWillJump.Contains(token) || headerManager.defenderWillJump.Contains(token))
            {
                Debug.Log("This token has already moved or is not an attacker or it has already moved or is frozen due to previous header challenge.");
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
        if (MatchManager.Instance.currentState == MatchManager.GameState.MovementPhaseDef && !selectedToken.isAttacker && !movedTokens.Contains(selectedToken))
        {
            PlayerToken ballHolder = ball.GetCurrentHex()?.GetOccupyingToken();
            if (ballHolder != null && ballHolder.isAttacker)
            {
                // Check if defender is adjacent to the attacker with the ball
                HexCell[] neighbors = ballHolder.GetCurrentHex().GetNeighbors(hexGrid);
                if (neighbors.Contains(selectedToken.GetCurrentHex()))
                {
                    // Defender is adjacent, enable tackle decision
                    Debug.Log($"{selectedToken.name} is adjacent to {ballHolder.name}. Press 'T' to tackle or select a hex to move.");
                    isWaitingForTackleDecisionWithoutMoving = true;
                }

            }
        }
        if (token.IsDribbler)
        {
            Debug.Log($"{token.name} selected as dribbler. Starting dribble movement.");
            remainingDribblerPace = token.pace; // Initialize remaining pace
            defendersTriedToIntercept.Clear(); // Reset defenders list
            isDribblerRunning = true;
            HighlightValidMovementHexes(token, 1); // Highlight immediate neighbors
        }
        else
        {
            // Highlight valid movement hexes for the selected token
            HighlightValidMovementHexes(selectedToken, token.pace);
        }
    }
    
    // This method will highlight valid movement hexes for the selected token
    public void HighlightValidMovementHexes(PlayerToken token, int movementRange)
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
        // Debug.Log($"IsHexValidForMovement called for {hex.name}: {isValid}");
        if (!isValid)
        {
            hexGrid.ClearHighlightedHexes();  // Clear the highlights if an invalid hex is clicked
            Debug.Log("Invalid hex clicked. All highlights cleared.");
        }
        return isValid;
    }

    public IEnumerator MoveTokenToHex(HexCell targetHex, PlayerToken token = null)
    {
        PlayerToken movingToken = token ?? selectedToken;
        if (movingToken == null)
        {
            Debug.LogError("No token selected to move.");
            yield break;
        }

        // Find the path from the current hex to the target hex
        List<HexCell> path = HexGridUtils.FindPath(movingToken.GetCurrentHex(), targetHex, hexGrid);

        if (path == null || path.Count == 0)
        {
            Debug.LogError("No valid path found to the target hex.");
            yield break;
        }

        // Start the token movement across the hexes in the path
        yield return StartCoroutine(MoveTokenAlongPath(movingToken, path));
        if (movingToken.IsDribbler && isDribblerRunning)
        {
            remainingDribblerPace -= 1; // Reduce remaining dribbler pace
            Debug.Log($"{movingToken.name} has {remainingDribblerPace} remaining pace.");
            // Check for interceptions
            eligibleDefenders = GetEligibleDefendersForInterception(targetHex);
            eligibleDefenders = eligibleDefenders.Except(defendersTriedToIntercept).ToList(); // Remove defenders already tried
            if (eligibleDefenders.Count > 0)
            {
                Debug.Log($"Eligible defenders for interception: {string.Join(", ", eligibleDefenders.Select(d => d.name))}");
                defendersTriedToIntercept.AddRange(eligibleDefenders); // Mark as tried
                StartBallInterceptionDiceRollSequence(targetHex, eligibleDefenders);
            }
            else if (remainingDribblerPace > 0)
            {
                // Highlight valid movement hexes for the selected token
                HighlightValidMovementHexes(movingToken, 1);
            }
            else
            {
                Debug.Log($"{movingToken.name} has exhausted their pace. Dribbling complete.");
                isDribblerRunning = false;
                AdvanceMovementPhase(); // End dribbler's movement
            }
        }
        else
        {
          movedTokens.Add(movingToken);  // Track this token as moved
          AdvanceMovementPhase(); // Basic check to advance the movement phase
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
        token.SetCurrentHex(path.Last());  // Update the token's hex to the final step
        Debug.Log($"Token arrived at hex: {path.Last().name}");
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
                // If attackHasPossession is false, try interception
                if (!MatchManager.Instance.attackHasPossession)
                {
                    HexCell ballHex = ball.GetCurrentHex();
                    eligibleDefenders = GetEligibleDefendersForInterception(ballHex);
                    if (eligibleDefenders.Contains(token))
                    {
                        List<PlayerToken> defenderToIntercept = new List<PlayerToken>{token};
                        Debug.Log("Defender near the ball! Starting dice roll for interception.");
                        StartBallInterceptionDiceRollSequence(ballHex, defenderToIntercept);
                    }
                }
                else
                {
                    // Ensure the defender is adjacent to the attacker with the ball
                    HexCell ballHex = ball.GetCurrentHex();
                    HexCell[] ballNeighbors = ballHex.GetNeighbors(hexGrid);
                    if (ballNeighbors.Contains(token.GetCurrentHex()))
                    {
                        Debug.Log("Defender near the attacker with the ball. Waiting for tackle decision...Press [T]ackle or [N]o Tackle");
                        selectedDefender = token;  // Store the selected defender
                        isWaitingForTackleDecision = true;  // Activate tackle decision listener
                    }
                    else
                    {
                        Debug.Log("Defender is not close enough to tackle. No prompt shown.");
                    }
                    // TODO: 5th defender doesnt have time to decide for T ot N
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

    private void AdvanceMovementPhase()
    {
        if (!isWaitingForInterceptionDiceRoll
            && !isWaitingForTackleDecisionWithoutMoving
            && !isWaitingForTackleDecision
            && !isWaitingForTackleRoll
            && !isWaitingForReposition
        )
        {
            Debug.Log("AdvanceMovementPhase: Checking movement phase transitions.");
            // Check for the 2f2 special phase
            if (MatchManager.Instance.currentState == MatchManager.GameState.MovementPhase2f2)
            {
                attackersMovedIn2f2++;
                if (attackersMovedIn2f2 >= maxAttackerMovesIn2f2)
                {
                    Debug.Log("Last two attackers have moved in 2f2 phase. Ending Movement Phase.");
                    EndMovementPhase();
                    return;
                }
            }
            // Check for defender movement phase
            if (MatchManager.Instance.currentState == MatchManager.GameState.MovementPhaseDef)
            {
                defendersMoved++;
                if (defendersMoved >= maxDefenderMoves)
                {
                    Debug.Log("All defenders have moved. Ready for Movement Phase 2f2.");
                    MatchManager.Instance.StartMovementPhase2f2();
                    return;
                }
            }
            // Check for attacker movement phase
            if (MatchManager.Instance.currentState == MatchManager.GameState.MovementPhaseAttack)
            {
                attackersMoved++;
                if (attackersMoved >= maxAttackerMoves)
                {
                    Debug.Log("All attackers have moved. Switching to Defensive Movement Phase.");
                    MatchManager.Instance.StartMovementPhaseDef();
                    return;
                }
            }
        }
        else
        {
            Debug.LogWarning("AdvanceMovementPhase: Waiting for dice rolls or repositioning.");
        }
    }

    public void ForfeitTeamMovementPhase()
    {
        Debug.Log("Movement phase forfeited. No further moves for this team.");
        if (MatchManager.Instance.currentState == MatchManager.GameState.MovementPhase2f2)
        {
            EndMovementPhase();
        }
        if (MatchManager.Instance.currentState == MatchManager.GameState.MovementPhaseDef)
        {
            defendersMoved = maxDefenderMoves-1;
            AdvanceMovementPhase();  // Reset the movement phase
        }
        if (MatchManager.Instance.currentState == MatchManager.GameState.MovementPhaseAttack)
        {
            attackersMoved = maxAttackerMoves-1;
            AdvanceMovementPhase();  // Reset the movement phase
        }
    }
    
    private List<PlayerToken> GetEligibleDefendersForInterception(HexCell targetHex)
    {
        List<PlayerToken> eligibleDefenders = new List<PlayerToken>();
        HexCell[] neighbors = targetHex.GetNeighbors(hexGrid);

        foreach (HexCell neighbor in neighbors)
        {
            PlayerToken token = neighbor.GetOccupyingToken();
            if (token != null && neighbor.isDefenseOccupied && 
                !movedTokens.Contains(token) && 
                !headerManager.defenderWillJump.Contains(token) &&
                !headerManager.attackerWillJump.Contains(token) &&
                !eligibleDefenders.Contains(token)
            )  // Avoid duplicates
            {
                eligibleDefenders.Add(token);
            }
        }

        return eligibleDefenders;
    }

    private void StartBallInterceptionDiceRollSequence(HexCell targetHex, List<PlayerToken> eligibleDefenders)
    {
        if (eligibleDefenders == null || eligibleDefenders.Count == 0)
        {
            Debug.LogWarning("No eligible defenders for interception. Interception sequence aborted.");
            return;
        }
        selectedDefender = eligibleDefenders[0];
        if (selectedDefender == null)
        {
            Debug.LogError("Failed to assign a valid defender from the eligible defenders list.");
            return;
        }
        Debug.Log($"Selected defender for interception: {selectedDefender.name}. Press R to roll...");
        // Set the flag to wait for interception dice roll
        isWaitingForInterceptionDiceRoll = true;
    }

    public void PerformBallInterceptionDiceRoll()
    {
        Debug.Log("PerformBallInterceptionDiceRoll Runs");
        if (selectedDefender != null)
        {
            // Roll the dice (1 to 6)
            int diceRoll = 1; // God Mode
            // int diceRoll = Random.Range(1, 7);
            // Debug.Log($"Dice roll by defender at {selectedDefender.GetCurrentHex().coordinates}: {diceRoll}");
            isWaitingForInterceptionDiceRoll = false;

            // Get the defender's tackling attribute
            int defenderTackling = selectedDefender.tackling;
            Debug.Log($"Defender: {selectedDefender.name}, Tackling: {defenderTackling}, Dice Roll: {diceRoll}");

            // Check interception condition: either roll a 6 or roll + tackling >= 10
            if (diceRoll == 6 || (diceRoll + defenderTackling >= INTERCEPTION_THRESHOLD))
            {
                // Defender successfully intercepts the ball
                Debug.Log($"Ball intercepted by {selectedDefender.name} at {selectedDefender.GetCurrentHex().coordinates}!");
                StartCoroutine(HandleBallInterception(selectedDefender.GetCurrentHex()));
                ResetBallInterceptionDiceRolls();
            }
            else
            {
                Debug.Log($"{selectedDefender.name} at {selectedDefender.GetCurrentHex().coordinates} failed to intercept.");
                // Move to the next defender, if any
                eligibleDefenders.Remove(selectedDefender);
                if (eligibleDefenders.Count > 0)
                {
                    selectedDefender = eligibleDefenders[0];  // Move to the next defender
                    isWaitingForInterceptionDiceRoll = true;
                    Debug.Log($"Selected defender for interception: {selectedDefender.name}. Press R to roll...");
                    // PerformBallInterceptionDiceRoll();  // Roll for the next defender
                }
                else
                {
                    Debug.Log("No more defenders to roll.");
                    if (remainingDribblerPace > 0)
                    {
                        // Highlight valid movement hexes for the selected token
                        HighlightValidMovementHexes(selectedToken, 1);
                    }
                    else
                    {
                        Debug.Log($"{selectedToken.name} has no remaining pace. Ending dribble.");
                        AdvanceMovementPhase();
                    }
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
        
        // // Rigged
        if (isDefender)
        {
            defenderDiceRoll = 1;
            tackleDefenderRolled = true;
            Debug.Log($"Defender rolled: {defenderDiceRoll}. Now it's the attacker's turn.");
        }
        else
        {
            attackerDiceRoll = 6;
            tackleAttackerRolled = true;
            Debug.Log($"Attacker rolled: {attackerDiceRoll}. Comparing results...");
            StartCoroutine(CompareTackleRolls());  // Compare the rolls after both rolls are complete
        }
        // // Random
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
        ResetTacklePhase();  // Reset tackle phase after handling results
        // Ensure selected tokens are valid
        if (selectedDefender == null)
        {
            Debug.LogError("Error: Defender token is not set!");
            yield break;
        }
        PlayerToken attackerToken = ball.GetCurrentHex()?.GetOccupyingToken();
        if (attackerToken == null)
        {
            Debug.LogError("Error: No attacker token found on the ball's hex!");
            yield break;
        }
        // Retrieve the dribbling and tackling values
        int defenderTackling = selectedDefender.tackling;
        int attackerDribbling = attackerToken.dribbling;

        Debug.Log($"Defender Name: {selectedDefender.name} with tackling: {defenderTackling}, Attacker: {attackerToken.name} with Dribbling: {attackerDribbling}");
        if (defenderDiceRoll <= FOUL_THRESHOLD)
        {
            Debug.Log("Defender committed a foul.");
            yield return StartCoroutine(HandleFoulProcess(attackerToken, selectedDefender));
            yield break;  // End tackle resolution as the foul process takes over
        }
        else if (defenderDiceRoll + defenderTackling > attackerDiceRoll + attackerDribbling)
        {
            Debug.Log($"Tackle successful! {selectedDefender.name} roll({defenderDiceRoll})+Tackling({defenderTackling}) beats {attackerToken.name}'s roll({attackerDiceRoll})+Dribbling({attackerDribbling}) ad wins possession of the ball.");
            ball.SetCurrentHex(selectedDefender.GetCurrentHex());
            yield return StartCoroutine(groundBallManager.HandleGroundBallMovement(selectedDefender.GetCurrentHex()));  // Move the ball to the defender's hex
            MatchManager.Instance.ChangePossession();  // Change possession to the defender's team
            MatchManager.Instance.UpdatePossessionAfterPass(selectedDefender.GetCurrentHex());  // Update possession
            yield return StartCoroutine(HandlePostTackleReposition(selectedDefender, attackerToken));
            ResetMovementPhase();  // End the movement phase after successful tackle
            MatchManager.Instance.currentState = MatchManager.GameState.SuccessfulTackle;
            Debug.Log("Movement phase ended due to successful tackle.");
        }
        else if (defenderDiceRoll + defenderTackling <= attackerDiceRoll + attackerDribbling)
        {
            Debug.Log($"Tackle failed! {selectedDefender.name} roll({defenderDiceRoll})+Tackling({defenderTackling}) loses to {attackerToken.name}'s roll({attackerDiceRoll})+Dribbling({attackerDribbling}) and {attackerToken.name} retains possession of the ball.");
            yield return StartCoroutine(HandlePostTackleReposition(attackerToken, selectedDefender));
        }
        else // In case of a tie
        {
            Debug.Log("Tackle results in a tie. Loose ball situation.");
            // TODO: Handle loose ball situation
        }
 
    }

    private IEnumerator HandlePostTackleReposition(PlayerToken winner, PlayerToken loser)
    {
        Debug.Log($"{winner.name} won the tackle and is repositioning around {loser.name}. Click a Highlighted Hex to move there or Press X to stay put.");
        // Get the loser's hex and neighboring hexes
        HexCell loserHex = loser.GetCurrentHex();
        HexCell[] repositionHexesArray = loserHex.GetNeighbors(hexGrid);
        List<HexCell> repositionHexes = new List<HexCell>(repositionHexesArray); // Convert array to list
        repositionHexes.Where(hex => 
            hex != loserHex && !hex.isDefenseOccupied && !hex.isAttackOccupied).ToList();


        // Highlight repositioning options
        foreach (HexCell hex in repositionHexes)
        {
            if (!hex.isDefenseOccupied && !hex.isAttackOccupied)
            {
                hex.HighlightHex("reposition");
                hexGrid.highlightedHexes.Add(hex);
            }
        }

        isWaitingForReposition = true;

        while (isWaitingForReposition)
        {
            if (Input.GetKeyDown(KeyCode.X))
            {
                Debug.Log($"{winner.name} forfeits repositioning and stays at current position.");
                isWaitingForReposition = false;
                hexGrid.ClearHighlightedHexes();
                AdvanceMovementPhase(); // Skip repositioning and move to the next phase
                yield break;
            }
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    HexCell clickedHex = hit.collider.GetComponent<HexCell>();
                    if (clickedHex != null
                        && repositionHexes.Contains(clickedHex)
                        && clickedHex != loser.GetCurrentHex()
                        && !clickedHex.isDefenseOccupied
                        && !clickedHex.isAttackOccupied
                    )
                    {
                        Debug.Log($"{winner.name} repositioning to {clickedHex.coordinates}.");
                        HexCell winnerHex = winner.GetCurrentHex();
                        // the check seems to be redundant as before the call of this method, the Possession has been changed if needed
                        if (winner.isAttacker)
                        {
                            winnerHex.isAttackOccupied = false;
                            winnerHex.ResetHighlight();
                            clickedHex.isAttackOccupied = true;  // Mark the target hex as occupied by an attacker
                        }
                        else
                        {
                            winnerHex.isDefenseOccupied = false;
                            winnerHex.ResetHighlight();
                            clickedHex.isDefenseOccupied = true;  // Mark the target hex as occupied by an attacker
                        }
                        hexGrid.ClearHighlightedHexes();
                        yield return StartCoroutine(winner.JumpToHex(clickedHex)); // Move the token
                        isWaitingForReposition = false;
                        ball.PlaceAtCell(clickedHex); // Move the ball
                        clickedHex.HighlightHex("isAttackOccupied");
                        Debug.Log("Repositioning complete.");
                        eligibleDefenders = GetEligibleDefendersForInterception(clickedHex);
                        if (eligibleDefenders.Count > 0)
                        {
                            Debug.Log($"Eligible defenders for interception: {string.Join(", ", eligibleDefenders.Select(d => d.name))}");
                            StartBallInterceptionDiceRollSequence(clickedHex, eligibleDefenders);
                        }
                        else
                        {
                          // Repositioned in a safe Hex
                          AdvanceMovementPhase();
                        }
                    }
                }
            }
            yield return null;
        }
    }

    private IEnumerator HandleFoulProcess(PlayerToken attackerToken, PlayerToken defenderToken)
    {
        Debug.Log("Handling foul resolution process...");

        // Phase 1: Yellow card decision
        Debug.Log("Press 'R' to roll for a Booking.");
        isWaitingForYellowCardRoll = true;
        // Introduce a brief delay to ensure previous keypresses are ignored
        yield return null;
        while (isWaitingForYellowCardRoll)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                int roll = Random.Range(1, 7);  // Roll a dice (1 to 6)
                Debug.Log($"Yellow card roll: {roll}");
                if (roll >= MatchManager.Instance.refereeLeniency)
                {
                    Debug.Log($"Defender {defenderToken.name} receives a yellow card!");
                    defenderToken.ReceiveYellowCard();  // Assume a method exists to handle this
                }
                else
                {
                    Debug.Log($"Defender {defenderToken.name} escapes a yellow card.");
                }
                isWaitingForYellowCardRoll = false;
            }
            yield return null;  // Wait for the next frame
        }

        // Phase 2: Injury decision
        Debug.Log("Press 'R' to roll for attacker injury.");
        isWaitingForInjuryRoll = true;
        yield return null;   // Delay to avoid immediate triggering
        while (isWaitingForInjuryRoll)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                int roll = Random.Range(1, 7);  // Roll a dice (1 to 6)
                Debug.Log($"Injury roll: {roll}");
                if (roll >= attackerToken.resilience)
                {
                    Debug.Log($"Attacker {attackerToken.name} is injured!");
                    attackerToken.ReceiveInjury();  // Assume a method exists to handle this
                }
                else
                {
                    Debug.Log($"Attacker {attackerToken.name} avoids injury.");
                }
                isWaitingForInjuryRoll = false;
            }
            yield return null;  // Wait for the next frame
        }
        Debug.Log("Foul process completed.");

        // Phase 3: Foul decision (Play On or Take the Foul)
        Debug.Log("Press 'A' to Play On or 'Z' to Take the Foul.");
        isWaitingForFoulDecision = true;
        while (isWaitingForFoulDecision)
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                Debug.Log("Attacker chooses to play on. Reposition process starts.");
                isWaitingForFoulDecision = false;  // Cancel the decision phase
                yield return StartCoroutine(HandlePostTackleReposition(attackerToken, defenderToken));  // Start repositioning
            }
            else if (Input.GetKeyDown(KeyCode.Z))
            {
                Debug.Log("Attacker chooses to take the foul. Transitioning to Free Kick.");
                isWaitingForFoulDecision = false;  // Cancel the decision phase

                // End the movement phase and start the free kick process
                EndMovementPhase();  // End the movement phase
                freeKickManager.StartFreeKickPreparation();
            }
            yield return null;  // Wait for the next frame
        }

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

    public void ResetMovementPhase()
    {
        movedTokens.Clear();  // Reset the list of moved tokens
        attackersMoved = 0;    // Reset the number of attackers that have moved
        defendersMoved = 0;    // Reset the number of defenders that have moved
        attackersMovedIn2f2 = 0;  // Reset the 2f2 phase counter
        selectedToken = null;  // Reset the selected token
        Debug.Log("Movement phase has been reset.");
    }

    private void ResetBallInterceptionDiceRolls()
    {
        defenderHexesNearBall.Clear();
        selectedDefender = null;
        eligibleDefenders.Clear();
        isWaitingForInterceptionDiceRoll = false;
    }

    private void EndMovementPhase()
    {
        MatchManager.Instance.currentState = MatchManager.GameState.MovementPhaseEnded;  // Stop all movements
        ResetMovementPhase();  // Reset the moved tokens and phase counters
        headerManager.ResetHeader();  // Reset the header to free up unmovable players
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
