using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

public class LongBallManager : MonoBehaviour
{
    [Header("Dependencies")]
    public Ball ball;
    public HexGrid hexGrid;
    public GroundBallManager groundBallManager;
    public MovementPhaseManager movementPhaseManager;
    public OutOfBoundsManager outOfBoundsManager;
    public LooseBallManager looseBallManager;
    public FinalThirdManager finalThirdManager;
    public GoalKeeperManager goalKeeperManager;
    public HelperFunctions helperFunctions;
    [Header("Runtime")]
    public bool isAvailable = false;
    [SerializeField]
    public bool isActivated = false;
    public bool isAwaitingTargetSelection = false;
    [SerializeField]
    private bool isDangerous = false;  // Flag for difficult pass
    public bool isWaitingForAccuracyRoll = false; // Flag to check for accuracy roll
    public bool isWaitingForDirectionRoll = false; // Flag to check for Direction roll
    public bool isWaitingForDistanceRoll = false; // Flag to check for Distance roll
    [SerializeField]
    private bool isWaitingForInterceptionRoll = false; // Flag to check for Interception Roll After Accuracy Result
    public bool isWaitingForDefLBMove = false;
    [Header("Important things")]
    public HexCell currentTargetHex;
    private int directionIndex;
    private HexCell finalHex;
    // private Dictionary<HexCell, List<HexCell>> interceptionHexToDefendersMap = new Dictionary<HexCell, List<HexCell>>();
    private List<HexCell> interceptingDefenders;

    private void OnEnable()
    {
        GameInputManager.OnClick += OnClickReceived;
        GameInputManager.OnKeyPress += OnKeyReceived;
    }

    private void OnDisable()
    {
        GameInputManager.OnClick -= OnClickReceived;
        GameInputManager.OnKeyPress -= OnKeyReceived;
    }

    private void OnClickReceived(PlayerToken token, HexCell hex)
    {
        if (isAwaitingTargetSelection)
        {
            HandleLongBallProcess(hex);
        }
        else if (isWaitingForDefLBMove)
        {
            if (hex != null && hexGrid.highlightedHexes.Contains(hex))
            {
                hexGrid.ClearHighlightedHexes();
                MoveGKForLB(hex);
            }
            else
            {
                Debug.LogWarning($"Cannot move GK there. Please click on a Highlighted Hex or Press [X] to forfeit GK Movement!");
            }
        }
    }

    private void OnKeyReceived(KeyPressData keyData)
    {
        // return;
        if (isAvailable && !isActivated && keyData.key == KeyCode.L)
        {
            MatchManager.Instance.TriggerLongPass();
        }
        if (isActivated)
        {
            if (isWaitingForAccuracyRoll && keyData.key == KeyCode.R)
            {
                PerformAccuracyRoll(); // Handle accuracy roll
            }
            else if (isWaitingForDirectionRoll && keyData.key == KeyCode.R)
            {
                PerformDirectionRoll(); // Handle direction roll
            }
            else if (isWaitingForDistanceRoll && keyData.key == KeyCode.R)
            {
                StartCoroutine(PerformDistanceRoll()); // Handle distance roll
            }
            else if (isWaitingForInterceptionRoll && keyData.key == KeyCode.R)
            {
                StartCoroutine(PerformInterceptionCheck(finalHex)); 
            }
            else if (isWaitingForDefLBMove && keyData.key == KeyCode.X)
            {
                hexGrid.ClearHighlightedHexes();
                Debug.Log($"GK chooses to not move for the Long Ball, moving on!");
                isWaitingForDefLBMove = false;
            }
        }
    }

    public void ActivateLongBall()
    {
        isActivated = true;
        isAvailable = false;
        isAwaitingTargetSelection = true;
        hexGrid.ClearHighlightedHexes(); 
        Debug.Log("Long Ball activated. Please select a target hex.");
    }
    
    public void HandleLongBallProcess(HexCell clickedHex)
    {
        if (clickedHex != null)
        { 
            Debug.Log($"Clicked hex: {clickedHex.coordinates}");
            HexCell ballHex = ball.GetCurrentHex();
            if (ballHex == null)
            {
                Debug.LogError("Ball's current hex is null! Ensure the ball has been placed on the grid.");
                return;
            }
            else
            {
                // Now handle the pass based on difficulty
                HandleLongBallBasedOnDifficulty(clickedHex);
            }   
        }
    }

    private void HandleLongBallBasedOnDifficulty(HexCell clickedHex)
    {
        int difficulty = MatchManager.Instance.difficulty_level;  // Get current difficulty
        // Debug.Log("Hello from HandleLongBallBasedOnDifficulty");
        // Centralized target validation
        hexGrid.ClearHighlightedHexes();
        var (isValid, isDangerous) = ValidateLongBallTarget(clickedHex);
        if (!isValid)
        {
            // Debug.LogWarning("Long Pass target is invalid");
            currentTargetHex = null;
            return; // Reject invalid targets
        }
        // Difficulty-based handling
        if (difficulty == 3) // Hard Mode: Immediate action
        {
            currentTargetHex = clickedHex;  // Assign the current target hex
            isWaitingForAccuracyRoll = true;  // Wait for accuracy roll
            Debug.Log("Waiting for accuracy roll... Please Press R key.");
        }
        else if (difficulty == 2) // Medium Mode: Require confirmation with a second click
        {
            if (clickedHex == currentTargetHex)  // If it's the same hex clicked twice
            {
                Debug.Log("Long Ball confirmed by second click. Waiting for accuracy roll.");
                MatchManager.Instance.gameData.gameLog.LogEvent(MatchManager.Instance.LastTokenToTouchTheBallOnPurpose, MatchManager.ActionType.AerialPassAttempt);
                isAwaitingTargetSelection = false;
                isWaitingForAccuracyRoll = true;  // Now ask for the accuracy roll
            }
            else
            {
                // First click: Set the target, highlight the path, and wait for confirmation
                currentTargetHex = clickedHex;
                hexGrid.ClearHighlightedHexes();

                // You can highlight the path here if you want to provide visual feedback in Medium/Easy modes
                HighlightLongPassArea(clickedHex);  // Optional: Visual feedback
                Debug.Log("First click registered. Click again to confirm the Long Ball.");
            }
        }
        else if (difficulty == 1) // Easy Mode: Require confirmation with a second click
        {
            if (clickedHex == currentTargetHex)  // If it's the same hex clicked twice
            {
                Debug.Log("Long Ball confirmed by second click. Waiting for accuracy roll.");
                isWaitingForAccuracyRoll = true;  // Now ask for the accuracy roll
            }
            else
            {
                // First click: Set the target, highlight the path, and wait for confirmation
                currentTargetHex = clickedHex;
                hexGrid.ClearHighlightedHexes();

                // You can highlight the path here if you want to provide visual feedback in Medium/Easy modes
                HighlightAllValidLongPassTargets();  // Takes a couple of seconds
                Debug.Log("First click registered. Click again to confirm the Long Ball.");
            }
        }
    }

    public (bool isValid, bool isDangerous) ValidateLongBallTarget(HexCell targetHex)
    {
        HexCell ballHex = ball.GetCurrentHex();
        // Step 1: Ensure the ballHex and targetHex are valid
        if (ballHex == null || targetHex == null)
        {
            Debug.LogError("Ball or target hex is null!");
            return (false, false);
        }
        // Step 2: Calculate the path between the ball and the target hex
        List<HexCell> pathHexes = groundBallManager.CalculateThickPath(ballHex, targetHex, ball.ballRadius);
        // Step 3: Check if the path is valid by ensuring no defense-occupied hexes block the path
        foreach (HexCell hex in pathHexes) // add here "and in the ball's neighbors"
        {
            if (hex.isDefenseOccupied && ballHex.GetNeighbors(hexGrid).Contains(hex))
            {
                Debug.Log($"Path blocked by defender at hex: {hex.coordinates}");
                return (false, false); // Invalid path
            }
        }
        // Step 4: Get defenders and their ZOI
        List<HexCell> defenderHexes = hexGrid.GetDefenderHexes();
        List<HexCell> defenderNeighbors = hexGrid.GetDefenderNeighbors(defenderHexes);
        // add something here to exclude all defenderHexes and defenderNeighbors above from valid targets
        List<HexCell> attackerHexes = hexGrid.GetAttackerHexes();
        List<HexCell> invalidHexesinRangeOfAttackers = hexGrid.GetAttackerHexesinRange(attackerHexes, 5);
        // Maybe remove from defenderNeighbors all Hexes that exist in defenderHexes
        // Maybe remove from invalidHexesinRangeOfAttackers all Hexes that exist in attackerHexes
        // So that the below debugs make sense.
        if (defenderHexes.Contains(targetHex) || defenderNeighbors.Contains(targetHex))
        {
            Debug.Log("You cannot target a Long Ball on a Defender or in their ZOI");
            return (false, false); // Invalid path
        }
        if (attackerHexes.Contains(targetHex) || invalidHexesinRangeOfAttackers.Contains(targetHex))
        {
            Debug.Log("You cannot target a Long Ball on an Attacker or 5 Hexes away from any Attacker");
            return (false, false); // Invalid path
        }
        if (targetHex.isInFinalThird * ballHex.isInFinalThird == -1)
        {
          return (true, true);
        }
        else return (true, false);
    }

    private void PerformAccuracyRoll()
    {
        // Placeholder for dice roll logic (will be expanded in later steps)
        Debug.Log("Performing accuracy roll for Long Pass. Please Press R key.");
        // Roll the dice (1 to 6)
        var (returnedRoll, returnedJackpot) = helperFunctions.DiceRoll();
        // int diceRoll = returnedRoll;
        int diceRoll = 1; // Melina Mode
        Debug.Log($"Accuracy dice roll: {diceRoll}");
        isWaitingForAccuracyRoll = false;
        // Get the passer's highPass attribute
        PlayerToken attackerToken = ball.GetCurrentHex()?.GetOccupyingToken();
        if (attackerToken == null)
        {
            Debug.LogError("Error: No attacker token found on the ball's hex!");
            return;
        }

        int highPassAttribute = attackerToken.highPass;
        Debug.Log($"Passer: {attackerToken.name}, HighPass: {highPassAttribute}");
        // Adjust threshold based on difficulty
        int accuracyThreshold = isDangerous ? 10 : 9 ;
        int totalAccuracy = diceRoll + highPassAttribute;
        if (totalAccuracy >= accuracyThreshold)
        {
            Debug.Log($"Long Ball is accurate, passer roll: {diceRoll}");
            // Move the ball to the intended target
            StartCoroutine(HandleLongBallMovement(currentTargetHex));
            MatchManager.Instance.gameData.gameLog.LogEvent(MatchManager.Instance.LastTokenToTouchTheBallOnPurpose, MatchManager.ActionType.AerialPassTargeted);
            MatchManager.Instance.currentState = MatchManager.GameState.LongBallCompleted;
            ResetLongPassRolls();  // Reset flags to finish long pass
        }
        else
        {
            Debug.Log($"Long Ball is NOT accurate, passer roll: {diceRoll}");
            isWaitingForDirectionRoll = true;
            Debug.Log("Waiting for Direction roll... Please Press R key.");
        }
    }

    private void PerformDirectionRoll()
    {
        // Debug.Log("Performing Direction roll to find Long Pass destination.");
        int diceRoll = 2; // Melina Mode
        // int diceRoll = Random.Range(0, 6);
        directionIndex = diceRoll;  // Set the direction index for future use
        string rolledDirection = looseBallManager.TranslateRollToDirection(diceRoll);
        Debug.Log($"Rolled {directionIndex}: Moving in {rolledDirection} direction");
        isWaitingForDirectionRoll = false;
        isWaitingForDistanceRoll = true;
        Debug.Log("Waiting for Distance roll... Please Press R key.");
    }

    private IEnumerator PerformDistanceRoll()
    {
        // Debug.Log("Performing Direction roll to find Long Pass destination.");
        var (returnedRoll, returnedJackpot) = helperFunctions.DiceRoll();
        // int diceRoll = returnedRoll;
        int distanceRoll = 6; // Melina Mode
        isWaitingForDistanceRoll = false;
        Debug.Log($"Distance Roll: {distanceRoll} hexes away from target.");
        // Calculate the final inaccurate target hex
        HexCell inaccurateTargetHex = outOfBoundsManager.CalculateInaccurateTarget(currentTargetHex, directionIndex, distanceRoll);
        
        // Check if the final hex is valid (not out of bounds or blocked)
        if (inaccurateTargetHex != null)
        {
            // Move the ball to the inaccurate final hex
            Debug.Log($"Inaccurate target hex calculated: {inaccurateTargetHex.coordinates}");
            yield return StartCoroutine(HandleLongBallMovement(inaccurateTargetHex));
        }
        else
        {
           Debug.LogWarning("Final hex calculation failed.");
        }
        ResetLongPassRolls();  // Reset flags to finish long pass
    }

    private void ResetLongPassRolls()
    {
        isWaitingForAccuracyRoll = false;
        isWaitingForDirectionRoll = false;
        isWaitingForDistanceRoll = false;
    }

    public IEnumerator HandleLongBallMovement(HexCell targetHex, bool isFromHandling = false)
    {
        if (targetHex == null)
        {
            Debug.LogError("Target Hex is null in HandleLongBallMovement!");
            yield break;
        }
        Vector3 startPosition = ball.transform.position;
        Vector3 targetPosition = targetHex.GetHexCenter();
        float travelDuration = 2.0f;  // Duration of the ball's flight
        float elapsedTime = 0;
        float height = 10f;// Height of the arc for the aerial trajectory
        // isMoving = true;

        while (elapsedTime < travelDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / travelDuration;
            // Lerp position along the straight line
            Vector3 flatPosition = Vector3.Lerp(startPosition, targetPosition, progress);
            // // Add the arc (use a sine curve to create the arc)
            flatPosition.y += height * Mathf.Sin(Mathf.PI * progress);
            // // Combine the flat position with the height offset to create the arc
            ball.transform.position = flatPosition;
            yield return null;  // Wait for the next frame
        }
        // isMoving = false;  // Stop the movement
        // Ensure the ball ends exactly on the target hex
        ball.PlaceAtCell(targetHex);
        Debug.Log($"Ball has reached its destination: {targetHex.coordinates}.");
        // After movement completes, check if the ball is out of bounds
        if (isFromHandling) yield break;
        if (targetHex.isOutOfBounds)
        {
            Debug.Log("Ball landed out of bounds!");
            // Debug.Log($"Passing currentTargetHex to HandleOutOfBounds: {currentTargetHex.coordinates}");
            outOfBoundsManager.HandleOutOfBounds(targetHex, directionIndex, "inaccuracy");
            CleanUpLongBall();
        }
        else
        {
            if (goalKeeperManager.ShouldGKMove(targetHex))
            {
                yield return StartCoroutine(goalKeeperManager.HandleGKFreeMove());
                // TODO: Check if GK is already on the ball
            }
            yield return StartCoroutine(HandleGKLongBallMove());
            Debug.Log("Ball landed within bounds.");
            if (targetHex.isDefenseOccupied)
            {
                // Ball Landed directly on a Defender
                MatchManager.Instance.gameData.gameLog.LogEvent(targetHex.GetOccupyingToken()
                    , MatchManager.ActionType.BallRecovery
                    , recoveryType: "long"
                    , connectedToken: MatchManager.Instance.LastTokenToTouchTheBallOnPurpose
                );
                MatchManager.Instance.SetLastToken(targetHex.GetOccupyingToken());
                MatchManager.Instance.ChangePossession();
                MatchManager.Instance.UpdatePossessionAfterPass(targetHex);
                MatchManager.Instance.currentState = MatchManager.GameState.LooseBallPickedUp;
            }
            else if (targetHex.isAttackOccupied)
            {
                // Ball has landed on an attacker 
                MatchManager.Instance.UpdatePossessionAfterPass(targetHex);
                MatchManager.Instance.gameData.gameLog.LogEvent(MatchManager.Instance.LastTokenToTouchTheBallOnPurpose, MatchManager.ActionType.AerialPassCompleted);
                MatchManager.Instance.SetLastToken(targetHex.GetOccupyingToken());
                MatchManager.Instance.currentState = MatchManager.GameState.LongBallCompleted;
            }
            else {
                // Landed neither on Def or Attacker. Check for defender's ZOI interception
                CheckForLongBallInterception(targetHex);
                MatchManager.Instance.UpdatePossessionAfterPass(targetHex);
            }
            CleanUpLongBall();
            finalThirdManager.TriggerFinalThirdPhase();
        }
    }

    public IEnumerator HandleGKLongBallMove()
    {
        isWaitingForDefLBMove = true;
        PlayerToken defenderGK = hexGrid.GetDefendingGK();

        if (defenderGK == null)
        {
            Debug.LogError("No defending goalkeeper found!");
            yield break;
        }

        movementPhaseManager.HighlightValidMovementHexes(defenderGK, defenderGK.pace);

        if (hexGrid.highlightedHexes.Count == 0)
        {
            Debug.Log("GK has no valid move options. Skipping free move.");
            isWaitingForDefLBMove = false;
            yield break;
        }

        Debug.Log("🧤 GK Free Long Ball Move: Click on a highlighted hex to move, or press [X] to skip.");

        while (isWaitingForDefLBMove)
        {
            yield return null;
        }
    }

    private async void MoveGKForLB(HexCell hex)
    {
        hexGrid.ClearHighlightedHexes();
        await helperFunctions.StartCoroutineAndWait(movementPhaseManager.MoveTokenToHex(hex, hexGrid.GetDefendingGK(), false));
        Debug.Log($"🧤 {hexGrid.GetDefendingGK().name} moved to {hex.name}");
        isWaitingForDefLBMove = false;
    }

    private void CheckForLongBallInterception(HexCell landingHex)
    {
        // Get all defenders and their ZOIs (neighbors)
        List<HexCell> defenderHexes = hexGrid.GetDefenderHexes();
        List<HexCell> defenderNeighbors = hexGrid.GetDefenderNeighbors(defenderHexes);
        // Initialize the interceptingDefenders list to avoid null reference
        interceptingDefenders = new List<HexCell>();

        // Check if the landing hex is in any defender's ZOI (neighbors)
        if (defenderNeighbors.Contains(landingHex))
        {
                // Log for debugging: Confirm the landing hex and defender neighbors
            Debug.Log($"Landing hex {landingHex.coordinates} is in defender ZOI. Checking eligible defenders...");

            // Get defenders who have the landing hex in their ZOI
            foreach (HexCell defender in defenderHexes)
            {
                HexCell[] neighbors = defender.GetNeighbors(hexGrid);
                // Debug.Log($"Defender at {defender.coordinates} has neighbors: {string.Join(", ", neighbors.Select(n => n?.coordinates.ToString() ?? "null"))}");

                if (neighbors.Contains(landingHex))
                {
                    Debug.Log($"Defender at {defender.coordinates} can intercept at {landingHex.coordinates}");
                    interceptingDefenders.Add(defender);  // Add the eligible defender to the list
                }
            }
            // Check if there are any intercepting defenders
            if (interceptingDefenders.Count > 0)
            {
                Debug.Log($"Found {interceptingDefenders.Count} defender(s) eligible for interception. Please Press R key..");
                isWaitingForInterceptionRoll = true;
            }
            else
            {
                Debug.Log("No defenders eligible for interception. Ball lands without interception. Number one");
                MatchManager.Instance.currentState = MatchManager.GameState.LongBallCompleted;
                MatchManager.Instance.hangingPassType = "aerial";
            }
        }
        else
        {
            Debug.Log("Landing hex is not in any defender's ZOI. No interception needed. Number two ");
            // Long ball completed away from defenders
            MatchManager.Instance.currentState = MatchManager.GameState.LongBallCompleted;
            MatchManager.Instance.hangingPassType = "aerial";
        }
    }

    private IEnumerator PerformInterceptionCheck(HexCell landingHex)
    {
        if (interceptingDefenders == null || interceptingDefenders.Count == 0)
        {
            Debug.Log("No defenders available for interception.");
            yield break;
        }

        foreach (HexCell defenderHex in interceptingDefenders)
        {
            PlayerToken defenderToken = defenderHex.GetOccupyingToken();
            if (defenderToken == null)
            {
                Debug.LogWarning($"No valid token found at defender's hex {defenderHex.coordinates}.");
                continue;
            }
            Debug.Log($"Checking interception for defender at {defenderHex.coordinates}");
            // Roll the dice (1 to 6)
            var (returnedRoll, returnedJackpot) = helperFunctions.DiceRoll();
            // int diceRoll = returnedRoll;
            int diceRoll = 6; // Ensure proper range (1-6)
            Debug.Log($"Dice roll for defender {defenderToken.name} at {defenderHex.coordinates}: {diceRoll}");
            int totalInterceptionScore = diceRoll + defenderToken.tackling;
            Debug.Log($"Total interception score for defender {defenderToken.name}: {totalInterceptionScore}");
            MatchManager.Instance.gameData.gameLog.LogEvent(defenderToken, MatchManager.ActionType.InterceptionAttempt);

            if (diceRoll == 6 || totalInterceptionScore >= 10)
            {
                Debug.Log($"Defender at {defenderHex.coordinates} successfully intercepted the ball!");
                isWaitingForInterceptionRoll = false;
                MatchManager.Instance.gameData.gameLog.LogEvent(
                    defenderToken
                    , MatchManager.ActionType.InterceptionSuccess
                    , recoveryType: "long"
                    , connectedToken: MatchManager.Instance.LastTokenToTouchTheBallOnPurpose
                );
                MatchManager.Instance.SetLastToken(defenderToken);
                // Move the ball to the defender's hex and change possession
                yield return StartCoroutine(ball.MoveToCell(defenderHex));
                MatchManager.Instance.ChangePossession();
                MatchManager.Instance.UpdatePossessionAfterPass(defenderHex);
                MatchManager.Instance.currentState = MatchManager.GameState.LooseBallPickedUp;
                finalThirdManager.TriggerFinalThirdPhase();
                yield break;  // Stop the sequence once an interception is successful
            }
            else
            {
                Debug.Log($"Defender at {defenderHex.coordinates} failed to intercept the ball.");
            }
        }

        // If no defender intercepts, the ball stays at the original hex
        Debug.Log("No defenders intercepted. Ball remains at the landing hex.");
        MatchManager.Instance.currentState = MatchManager.GameState.LongBallCompleted;
        finalThirdManager.TriggerFinalThirdPhase();
    }

    private void HighlightLongPassArea(HexCell targetHex)
    {
        hexGrid.ClearHighlightedHexes();
        if (targetHex == null)
        {
            Debug.LogError("Target hex is null in HighlightLongPassArea!");
            return;
        }
        // Initialize highlightedHexes to ensure it's ready for use
       hexGrid.highlightedHexes = new List<HexCell>();
        // Get hexes within a radius (e.g., 6 hexes) around the targetHex
        int radius = 5;  // You can tweak this value as needed
        List<HexCell> hexesInRange = HexGrid.GetHexesInRange(hexGrid, targetHex, radius);
        if (hexesInRange == null || hexesInRange.Count == 0)
        {
            Debug.LogError("No hexes found in range for highlighting.");
            return;
        }

        // Loop through the hexes and highlight each one
        foreach (HexCell hex in hexesInRange)
        {
            if (hex == null)
            {
                Debug.LogWarning("Encountered a null hex while highlighting, skipping this hex.");
                continue;  // Skip null hexes
            }

            if (hex.isOutOfBounds || hex.isDefenseOccupied)
            {
                Debug.LogWarning($"Hex {hex.coordinates} is out of bounds, skipping highlight.");
                continue;  // Skip out of bounds hexes
            }

            // Highlight hexes (use a specific color for Long Pass)
            hex.HighlightHex("longPass");  // Assuming HexHighlightReason.LongPass is defined for long pass highlights
           hexGrid.highlightedHexes.Add(hex);  // Track the highlighted hexes for later clearing

            Debug.Log($"Highlighted Hex at coordinates: ({hex.coordinates.x}, {hex.coordinates.z})");
        }

        // Log the highlighted hexes if needed (optional)
        Debug.Log($"Highlighted {hexesInRange.Count} hexes around the target for a Long Pass.");
    }

    private void HighlightAllValidLongPassTargets()
    {
        // Clear the previous highlights
        hexGrid.ClearHighlightedHexes();

        // Loop through all hexes on the grid
        foreach (HexCell hex in hexGrid.cells)
        {
            if (hex == null  || hex.isOutOfBounds) continue;  // Skip null hexes

            // Check if the hex is a valid target
            var (isValid, isDangerous) = ValidateLongBallTarget(hex);

            if (isValid)
            {
                if (isDangerous)
                {
                    hex.HighlightHex("longPassDifficult");  // Highlight the Difficult hexes
                }
                else
                {
                    hex.HighlightHex("longPass"); // Highlight the valid hexes
                }
               hexGrid.highlightedHexes.Add(hex);  // Track highlighted hexes for later clearing
            }
        }

        Debug.Log($"Successfully highlighted {hexGrid.highlightedHexes.Count} valid hexes for Long Pass.");
    }

    public void CleanUpLongBall()
    {
        isActivated = false;
        isAwaitingTargetSelection = false;
        isDangerous = false;
        isWaitingForAccuracyRoll = false;
        isWaitingForDirectionRoll = false;
        isWaitingForDistanceRoll = false;
        isWaitingForInterceptionRoll = false;
        isWaitingForDefLBMove = false;
        currentTargetHex = null;
    }

}
