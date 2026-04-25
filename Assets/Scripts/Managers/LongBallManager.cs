using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Text;
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
    public HeaderManager headerManager;
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
                _ = MoveGKForLB(hex);
            }
            else
            {
                Debug.LogWarning($"Cannot move GK there. Please click on a Highlighted Hex or Press [X] to forfeit GK Movement!");
            }
        }
    }

    private void OnKeyReceived(KeyPressData keyData)
    {
        if (keyData.isConsumed) return;
        // return;
        if (isAvailable && !isActivated && keyData.key == KeyCode.L)
        {
            MatchManager.Instance.TriggerLongPass();
            keyData.isConsumed = true;
            return;
        }
        if (isActivated)
        {
            if (isWaitingForAccuracyRoll && keyData.key == KeyCode.R)
            {
                PerformAccuracyRoll(); // Handle accuracy roll
                keyData.isConsumed = true;
            }
            else if (isWaitingForDirectionRoll && keyData.key == KeyCode.R)
            {
                PerformDirectionRoll(); // Handle direction roll
                keyData.isConsumed = true;
            }
            else if (isWaitingForDistanceRoll && keyData.key == KeyCode.R)
            {
                StartCoroutine(PerformDistanceRoll()); // Handle distance roll
                keyData.isConsumed = true;
            }
            else if (isWaitingForInterceptionRoll && keyData.key == KeyCode.R)
            {
                StartCoroutine(PerformInterceptionCheck(finalHex));
                keyData.isConsumed = true;
            }
            else if (isWaitingForDefLBMove && keyData.key == KeyCode.X)
            {
                hexGrid.ClearHighlightedHexes();
                Debug.Log($"GK chooses to not move for the Long Ball, moving on!");
                isWaitingForDefLBMove = false;
                keyData.isConsumed = true;
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

    public void CommitToThisAction()
    {
        MatchManager.Instance.currentState = MatchManager.GameState.LongBall;
        MatchManager.Instance.CommitToAction();
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
        var (isValid, requiresTenPlusAccuracy) = ValidateLongBallTarget(clickedHex);
        if (!isValid)
        {
            currentTargetHex = null;
            isDangerous = false;
            hexGrid.ClearHighlightedHexes();
            return; // Reject invalid targets
        }
        isDangerous = requiresTenPlusAccuracy;
        // Difficulty-based handling
        if (difficulty == 3) // Hard Mode: Immediate action
        {
            currentTargetHex = clickedHex;
            ConfirmLongBallTargetSelection(clickedHex);
        }
        else if (difficulty == 2 || difficulty == 1) // Confirm with second click
        {
            if (clickedHex == currentTargetHex)
            {
                ConfirmLongBallTargetSelection(clickedHex);
            }
            else
            {
                currentTargetHex = clickedHex;
                hexGrid.ClearHighlightedHexes();
                HighlightAllValidLongPassTargets();
                HighlightCommittedTarget();
                Debug.Log("First click registered. Click the orange target again to confirm the Long Ball.");
            }
        }
    }

    private void ConfirmLongBallTargetSelection(HexCell clickedHex)
    {
        currentTargetHex = clickedHex;
        hexGrid.ClearHighlightedHexes();
        HighlightCommittedTarget();
        isAwaitingTargetSelection = false;
        CommitToThisAction();
        MatchManager.Instance.gameData.gameLog.LogEvent(
            MatchManager.Instance.LastTokenToTouchTheBallOnPurpose,
            MatchManager.ActionType.AerialPassAttempt
        );
        isWaitingForAccuracyRoll = true;
        Debug.Log("Long Ball confirmed. Waiting for accuracy roll. Please Press R key.");
    }

    private void HighlightCommittedTarget()
    {
        if (currentTargetHex == null)
        {
            return;
        }

        currentTargetHex.HighlightHex("passTargetCommitted");
        if (!hexGrid.highlightedHexes.Contains(currentTargetHex))
        {
            hexGrid.highlightedHexes.Add(currentTargetHex);
        }
    }

    public (bool isValid, bool isDangerous) ValidateLongBallTarget(HexCell targetHex)
    {
        HexCell ballHex = ball.GetCurrentHex();
        if (ballHex == null || targetHex == null)
        {
            Debug.LogError("Ball or target hex is null!");
            return (false, false);
        }
        if (targetHex.isOutOfBounds)
        {
            Debug.Log("You cannot target a Long Ball out of bounds");
            return (false, false);
        }

        List<HexCell> pathHexes = groundBallManager.CalculateThickPath(ballHex, targetHex, ball.ballRadius);

        foreach (HexCell hex in pathHexes)
        {
            if (hex.isDefenseOccupied && ballHex.GetNeighbors(hexGrid).Contains(hex))
            {
                Debug.Log($"Path blocked by defender at hex: {hex.coordinates}");
                return (false, false);
            }
        }

        List<HexCell> defenderHexes = hexGrid.GetDefenderHexes();
        List<HexCell> defenderNeighbors = hexGrid.GetDefenderNeighbors(defenderHexes);
        List<HexCell> attackerHexes = hexGrid.GetAttackerHexes();
        List<HexCell> invalidHexesinRangeOfAttackers = hexGrid.GetAttackerHexesinRange(attackerHexes, 5);

        if (defenderHexes.Contains(targetHex) || defenderNeighbors.Contains(targetHex))
        {
            Debug.Log("You cannot target a Long Ball on a Defender or in their ZOI");
            return (false, false);
        }
        if (attackerHexes.Contains(targetHex) || invalidHexesinRangeOfAttackers.Contains(targetHex))
        {
            Debug.Log("You cannot target a Long Ball on an Attacker or 5 Hexes away from any Attacker");
            return (false, false);
        }
        if (targetHex.isInFinalThird * ballHex.isInFinalThird == -1)
        {
          return (true, true);
        }
        else return (true, false);
    }

    private void PerformAccuracyRoll(int? rigRoll = null)
    {
        // Placeholder for dice roll logic (will be expanded in later steps)
        Debug.Log("Performing accuracy roll for Long Pass. Please Press R key.");
        // Roll the dice (1 to 6)
        var (returnedRoll, returnedJackpot) = helperFunctions.DiceRoll();
        int diceRoll = rigRoll ?? returnedRoll;
        // int diceRoll = 1; // Melina Mode
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
            finalHex = currentTargetHex;
            StartCoroutine(HandleLongBallMovement(currentTargetHex));
            MatchManager.Instance.gameData.gameLog.LogEvent(MatchManager.Instance.LastTokenToTouchTheBallOnPurpose, MatchManager.ActionType.AerialPassTargeted);
            ResetLongPassRolls();  // Reset flags to finish long pass
        }
        else
        {
            Debug.Log($"Long Ball is NOT accurate, passer roll: {diceRoll}");
            isWaitingForDirectionRoll = true;
            Debug.Log("Waiting for Direction roll... Please Press R key.");
        }
    }

    private void PerformDirectionRoll(int? rigRoll = null)
    {
        // Debug.Log("Performing Direction roll to find Long Pass destination.");
        var (returnedRoll, returnedJackpot) = helperFunctions.DiceRoll();
        int directionRoll = rigRoll ?? Mathf.Clamp(returnedRoll - 1, 0, 5);
        directionIndex = directionRoll;  // Set the direction index for future use
        string rolledDirection = looseBallManager.TranslateRollToDirection(directionRoll);
        Debug.Log($"Rolled {directionIndex}: Moving in {rolledDirection} direction");
        isWaitingForDirectionRoll = false;
        isWaitingForDistanceRoll = true;
        Debug.Log("Waiting for Distance roll... Please Press R key.");
    }

    private IEnumerator PerformDistanceRoll(int? rigRoll = null)
    {
        // Debug.Log("Performing Direction roll to find Long Pass destination.");
        var (returnedRoll, returnedJackpot) = helperFunctions.DiceRoll();
        int distanceRoll = rigRoll ?? returnedRoll;
        // int distanceRoll = 6; // Melina Mode
        isWaitingForDistanceRoll = false;
        Debug.Log($"Distance Roll: {distanceRoll} hexes away from target.");
        // Calculate the final inaccurate target hex
        HexCell inaccurateTargetHex = outOfBoundsManager.CalculateInaccurateTarget(currentTargetHex, directionIndex, distanceRoll);
        
        // Check if the final hex is valid (not out of bounds or blocked)
        if (inaccurateTargetHex != null)
        {
            // Move the ball to the inaccurate final hex
            Debug.Log($"Inaccurate target hex calculated: {inaccurateTargetHex.coordinates}");
            finalHex = inaccurateTargetHex;
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
        finalHex = targetHex;
        Debug.Log($"Ball has reached its destination: {targetHex.coordinates}.");
        // After movement completes, check if the ball is out of bounds
        if (isFromHandling) yield break;
        if (targetHex.isOutOfBounds)
        {
            Debug.Log("Ball landed out of bounds!");
            outOfBoundsManager.HandleOutOfBounds(currentTargetHex, directionIndex, "inaccuracy");
            CleanUpLongBall();
        }
        else
        {
            if (!targetHex.isDefenseOccupied && goalKeeperManager.ShouldGKMove(targetHex))
            {
                yield return StartCoroutine(goalKeeperManager.HandleGKFreeMove());
                // TODO: Check if GK is already on the ball
            }
            if (targetHex.isDefenseOccupied)
            {
                yield return StartCoroutine(ResolveLongBallRecovery(targetHex.GetOccupyingToken(), targetHex));
                yield break;
            }
            else if (targetHex.isAttackOccupied)
            {
                yield return StartCoroutine(HandleGKLongBallMove());
                yield return StartCoroutine(FinalizeLongBallAfterDefensiveResponses(targetHex));
            }
            else
            {
                MatchManager.Instance.UpdatePossessionAfterPass(targetHex);
                bool waitingForInterception = CheckForLongBallInterception(targetHex);
                if (!waitingForInterception)
                {
                    yield return StartCoroutine(HandleGKLongBallMove());
                    yield return StartCoroutine(FinalizeLongBallAfterDefensiveResponses(targetHex));
                }
                yield break;
            }
        }
    }

    private IEnumerator FinalizeLongBallAfterDefensiveResponses(HexCell targetHex)
    {
        if (targetHex == null)
        {
            yield break;
        }

        Debug.Log("Ball landed within bounds.");

        if (targetHex.isDefenseOccupied)
        {
            yield return StartCoroutine(ResolveLongBallRecovery(targetHex.GetOccupyingToken(), targetHex));
            yield break;
        }

        if (targetHex.isAttackOccupied)
        {
            MatchManager.Instance.gameData.gameLog.LogEvent(MatchManager.Instance.LastTokenToTouchTheBallOnPurpose, MatchManager.ActionType.AerialPassCompleted);
            MatchManager.Instance.SetLastToken(targetHex.GetOccupyingToken());
            MatchManager.Instance.UpdatePossessionAfterPass(targetHex);
            MatchManager.Instance.BroadcastEndOfLongBall();
            finalThirdManager.TriggerFinalThirdPhase();
            CleanUpLongBall();
            yield break;
        }

        MatchManager.Instance.UpdatePossessionAfterPass(targetHex);
        MatchManager.Instance.SetHangingPass("aerial");
        MatchManager.Instance.BroadcastEndOfLongBall();
        finalThirdManager.TriggerFinalThirdPhase();
        CleanUpLongBall();
    }

    private IEnumerator ResolveLongBallRecovery(PlayerToken recoveringToken, HexCell recoveryHex)
    {
        if (recoveringToken == null || recoveryHex == null)
        {
            yield break;
        }

        MatchManager.Instance.gameData.gameLog.LogEvent(
            recoveringToken,
            MatchManager.ActionType.BallRecovery,
            recoveryType: "long",
            connectedToken: MatchManager.Instance.LastTokenToTouchTheBallOnPurpose
        );
        MatchManager.Instance.SetLastToken(recoveringToken);
        MatchManager.Instance.ChangePossession();
        MatchManager.Instance.UpdatePossessionAfterPass(recoveryHex);
        MatchManager.Instance.BroadcastAnyOtherScenario();
        finalThirdManager.TriggerFinalThirdPhase();
        CleanUpLongBall();
        yield break;
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

    private async Task MoveGKForLB(HexCell hex)
    {
        hexGrid.ClearHighlightedHexes();
        // await helperFunctions.StartCoroutineAndWait(movementPhaseManager.MoveTokenToHex(hex, hexGrid.GetDefendingGK(), false));
        await helperFunctions.StartCoroutineAndWait(movementPhaseManager.MoveTokenToHex(
            targetHex: hex
            , token: hexGrid.GetDefendingGK()
            , isCalledDuringMovement: false
            , shouldCountForDistance: true
            , shouldCarryBall: false
        ));
        isWaitingForDefLBMove = false;
        Debug.Log($"🧤 {hexGrid.GetDefendingGK().name} moved to {hex.name}");
    }

    private bool CheckForLongBallInterception(HexCell landingHex)
    {
        if (landingHex == null)
        {
            interceptingDefenders = new List<HexCell>();
            return false;
        }

        HashSet<HexCell> landingNeighbors = landingHex
            .GetNeighbors(hexGrid)
            .Where(hex => hex != null)
            .ToHashSet();

        interceptingDefenders = hexGrid
            .GetDefenderHexes()
            .Where(hex => hex != null)
            .Where(hex => IsEligibleLongBallInterceptor(hex.GetOccupyingToken()))
            .Where(hex => landingNeighbors.Contains(hex))
            .OrderBy(hex => HexGridUtils.GetHexStepDistance(landingHex, hex))
            .ThenBy(hex => hex.GetOccupyingToken().tackling)
            .ThenBy(hex => hex.GetOccupyingToken().playerName, System.StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (interceptingDefenders.Count > 0)
        {
            Debug.Log($"Found {interceptingDefenders.Count} defender(s) eligible for long-ball interception. Please Press R key.");
            isWaitingForInterceptionRoll = true;
            return true;
        }

        Debug.Log("Landing hex is not in any eligible defender ZOI. No interception needed.");
        return false;
    }

    private bool IsEligibleLongBallInterceptor(PlayerToken token)
    {
        return token != null
            && !movementPhaseManager.stunnedTokens.Contains(token)
            && !movementPhaseManager.stunnedforNext.Contains(token)
            && !headerManager.defenderWillJump.Contains(token);
    }

    private IEnumerator PerformInterceptionCheck(HexCell landingHex, int? rigRoll = null)
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
            int diceRoll = rigRoll ?? returnedRoll;
            // int diceRoll = 6; // Ensure proper range (1-6)
            Debug.Log($"Dice roll for defender {defenderToken.name} at {defenderHex.coordinates}: {diceRoll}");
            int totalInterceptionScore = diceRoll + defenderToken.tackling;
            Debug.Log($"Total interception score for defender {defenderToken.name}: {totalInterceptionScore}");
            MatchManager.Instance.gameData.gameLog.LogExpectedRecovery(
                defenderToken,
                ExpectedStatsCalculator.CalculateRecoveryProbability(defenderToken),
                MatchManager.Instance.LastTokenToTouchTheBallOnPurpose,
                "long");
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
                MatchManager.Instance.BroadcastAnyOtherScenario();
                finalThirdManager.TriggerFinalThirdPhase();
                CleanUpLongBall();
                yield break;  // Stop the sequence once an interception is successful
            }
            else
            {
                Debug.Log($"Defender at {defenderHex.coordinates} failed to intercept the ball.");
            }
        }

        // If no defender intercepts, the ball stays at the original hex
        Debug.Log("No defenders intercepted. Ball remains at the landing hex.");
        isWaitingForInterceptionRoll = false;
        yield return StartCoroutine(HandleGKLongBallMove());
        yield return StartCoroutine(FinalizeLongBallAfterDefensiveResponses(landingHex));
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
            if (hex == targetHex)
            {
                // Highlight hexes (use a specific color for Long Pass)
                hex.HighlightHex("passTarget");  // Assuming HexHighlightReason.LongPass is defined for long pass highlights
            }
            else
            {
                // Highlight hexes (use a specific color for Long Pass)
                hex.HighlightHex("highPass");  // Assuming HexHighlightReason.LongPass is defined for long pass highlights
            }
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
                if (hex == currentTargetHex)
                {
                    hex.HighlightHex("passTargetCommitted");
                }
                else if (isDangerous)
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

    public string GetDebugStatus()
    {
        StringBuilder sb = new();
        sb.Append("LONG: ");

        if (isActivated) sb.Append("isActivated, ");
        if (isAvailable) sb.Append("isAvailable, ");
        if (isAwaitingTargetSelection) sb.Append("isAwaitingTargetSelection, ");
        if (isDangerous) sb.Append("isDangerous, ");
        if (isWaitingForAccuracyRoll) sb.Append("isWaitingForAccuracyRoll, ");
        if (isWaitingForDirectionRoll) sb.Append("isWaitingForDirectionRoll, ");
        if (isWaitingForDistanceRoll) sb.Append("isWaitingForDistanceRoll, ");
        if (isWaitingForInterceptionRoll) sb.Append("isWaitingForInterceptionRoll, ");
        if (isWaitingForDefLBMove) sb.Append("isWaitingForDefLBMove, ");
        if (currentTargetHex != null) sb.Append($"currentTargetHex: {currentTargetHex.name}, ");

        if (sb.Length >= 2 && sb[^2] == ',') sb.Length -= 2; // Trim trailing comma
        return sb.ToString();
    }

    public string GetInstructions()
    {
        StringBuilder sb = new();
        PlayerToken lastToken = MatchManager.Instance.LastTokenToTouchTheBallOnPurpose;
        PlayerToken defendingGK = hexGrid != null ? hexGrid.GetDefendingGK() : null;
        if (goalKeeperManager.isActivated) return "";
        if (finalThirdManager.isActivated) return "";
        if (isAvailable) sb.Append("Press [L] to Play a Long Ball, ");
        if (isActivated) sb.Append("Long: ");
        if (isAwaitingTargetSelection) sb.Append($"Click on a Hex 6 or more Hexes away from the closest Attacker, ");
        if (isAwaitingTargetSelection && currentTargetHex != null) sb.Append($"or click the yellow Hex again to confirm, ");
        if (isWaitingForAccuracyRoll && lastToken != null) {sb.Append($"Press [R] to roll the accuracy check with {lastToken.name}, a roll of {(isDangerous ? 10 : 9) - lastToken.highPass}+ is needed, ");}
        if (isWaitingForDirectionRoll) {sb.Append($"Press [R] to roll for Inacuracy Direction, ");}
        if (isWaitingForDistanceRoll) {sb.Append($"Press [R] to roll for Inacuracy Distance, ");}
        if (isWaitingForDefLBMove) {sb.Append($"{(defendingGK != null ? defendingGK.name : "The defending GK")} can move according to their pace, click a highlighted hex to rush there, or Press [X] to not rush out, ");}

        if (sb.Length >= 2 && sb[^2] == ',') sb.Length -= 2; // Trim trailing comma
        return sb.ToString();
    }

    public bool? IsInstructionExpectingHomeTeam()
    {
        if (MatchManager.Instance == null || (!isActivated && !isAvailable))
        {
            return null;
        }

        bool attackingTeamIsHome = MatchManager.Instance.teamInAttack == MatchManager.TeamInAttack.Home;
        if (!isActivated)
        {
            return attackingTeamIsHome;
        }

        if (isWaitingForDefLBMove || isWaitingForInterceptionRoll)
        {
            return !attackingTeamIsHome;
        }

        return attackingTeamIsHome;
    }
}
