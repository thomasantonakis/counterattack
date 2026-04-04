using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Linq;
using System.Threading.Tasks;

public class GroundBallManager : MonoBehaviour
{
    public enum PassValidationFailureReason
    {
        None,
        NullTarget,
        OutOfRange,
        BlockedByDefender,
        TargetOccupiedByDefender,
    }

    public readonly struct GroundPassValidationResult
    {
        public readonly bool IsValid;
        public readonly bool IsDangerous;
        public readonly List<HexCell> PathHexes;
        public readonly PassValidationFailureReason FailureReason;

        public GroundPassValidationResult(bool isValid, bool isDangerous, List<HexCell> pathHexes, PassValidationFailureReason failureReason)
        {
            IsValid = isValid;
            IsDangerous = isDangerous;
            PathHexes = pathHexes;
            FailureReason = failureReason;
        }
    }

    [Header("Dependencies")]
    public Ball ball;
    public HexGrid hexGrid;
    public FinalThirdManager finalThirdManager;
    public FirstTimePassManager firstTimePassManager;
    public GoalKeeperManager goalKeeperManager;
    public FreeKickManager freeKickManager;
    public HelperFunctions helperFunctions;
    [Header("Runtime Items")]
    public bool isAvailable = false;        // Check if the GroundBall is available as an action from the user.
    public bool isActivated = false;        // To check if the script is activated
    public bool isAwaitingTargetSelection = false; // To check if we are waiting for target selection
    // TODO: Formalize Short Pass as a first-class Ground Ball Pass mode instead of mutating this distance ad hoc.
    public int imposedDistance = 11;
    public bool isQuickThrow = false;
    public HexCell currentTargetHex = null;   // The currently selected target hex
    [SerializeField]
    public bool isWaitingForDiceRoll = false; // To check if we are waiting for dice rolls
    public bool passIsDangerous = false;      // To check if the pass is dangerous
    private HexCell currentDefenderHex = null;                      // The defender hex currently rolling the dice
    private HexCell hoveredPreviewHex = null;
    [SerializeField]
    public List<HexCell> defendingHexes = new List<HexCell>();     // List of defenders responsible for each interception hex
    [SerializeField]
    private List<HexCell> interceptionHexes = new List<HexCell>();  // List of interception hexes
    public int diceRollsPending = 0;          // Number of pending dice rolls
    private string latestValidationInstruction = string.Empty;

    private void OnEnable()
    {
        GameInputManager.OnClick += OnClickReceived;
        GameInputManager.OnHover += OnHoverReceived;
        GameInputManager.OnKeyPress += OnKeyReceived;
    }

    private void OnDisable()
    {
        GameInputManager.OnClick -= OnClickReceived;
        GameInputManager.OnHover -= OnHoverReceived;
        GameInputManager.OnKeyPress -= OnKeyReceived;
    }

    private void OnClickReceived(PlayerToken token, HexCell hex)
    {
        if (isAwaitingTargetSelection)
        {
            HandleGroundBallPath(hex);
        }
    }

    private void OnHoverReceived(PlayerToken token, HexCell hex)
    {
        if (!isActivated || !isAwaitingTargetSelection)
        {
            return;
        }

        if (MatchManager.Instance.difficulty_level != 1)
        {
            return;
        }

        if (hoveredPreviewHex == hex)
        {
            return;
        }

        hoveredPreviewHex = hex;
        UpdateEasyModeHoverPreview(hex);
    }

    private void OnKeyReceived(KeyPressData keyData)
    {
        if (isAvailable && !isActivated && !freeKickManager.isWaitingForExecution && keyData.key == KeyCode.P)
        {
            MatchManager.Instance.TriggerStandardPass();
            return;
        }
        if (isAvailable
            && !isActivated
            && MatchManager.Instance.currentState == MatchManager.GameState.QuickThrow&&
            keyData.key == KeyCode.Q
        )
        {
            MatchManager.Instance.TriggerStandardPass();
            isQuickThrow = true;
            CommitToThisAction();
            return;
        }
        if (isActivated)
        {
            if (isWaitingForDiceRoll && keyData.key == KeyCode.R)
            {
                // Check if waiting for dice rolls and the R key is pressed
                PerformGroundInterceptionDiceRoll();  // Trigger the dice roll when R is pressed
            }
        }
    }

    public void ActivateGroundBall(bool isFromQuickThrow = false)
    {
        // MatchManager.Instance.TriggerStandardPass();
        ball.SelectBall();
        Debug.Log("Standard pass attempt mode activated.");
        isActivated = true;
        isAvailable = false;  // Make it non available to avoid restarting this action again.
        isQuickThrow = isFromQuickThrow;
        if (MatchManager.Instance.difficulty_level == 3)
        {
            CommitToThisAction();
        }
        isAwaitingTargetSelection = true;
        latestValidationInstruction = string.Empty;
        Debug.Log("GroundBallManager activated. Waiting for target selection...");
    }

    public void CommitToThisAction()
    {
        if (isQuickThrow)
        {
            MatchManager.Instance.currentState = MatchManager.GameState.QuickThrow;
        }
        else
        {
            MatchManager.Instance.currentState = MatchManager.GameState.StandardPass;
        }
        MatchManager.Instance.CommitToAction();
    }
    
    public void HandleGroundBallPath(HexCell clickedHex, bool isGk = false)
    {
        if (clickedHex != null)
        {
            HexCell ballHex = ball.GetCurrentHex();
            if (ballHex == null)
            {
                Debug.LogError("Ball's current hex is null! Ensure the ball has been placed on the grid.");
                return;
            }
            else
            {
                // Now handle the pass based on difficulty
                HandleGroundPassBasedOnDifficulty(clickedHex);
            }   
        }
    }

    public void HandleGroundPassBasedOnDifficulty(HexCell clickedHex)
    {
        int difficulty = MatchManager.Instance.difficulty_level;  // Get current difficulty
        // Centralized path validation and danger assessment
        GroundPassValidationResult validation = ValidateGroundPassPath(clickedHex, imposedDistance);
        if (!validation.IsValid)
        {
            currentTargetHex = null;
            passIsDangerous = false;
            if (difficulty == 3)
            {
                latestValidationInstruction = GetValidationFailureInstruction(validation.FailureReason);
            }
            return; // Reject invalid paths
        }
        latestValidationInstruction = string.Empty;

        // Handle each difficulty's behavior
        if (difficulty == 3) // Hard Mode
        {
            currentTargetHex = clickedHex;
            isAwaitingTargetSelection = false;
            CommitToThisAction();
            LogGroundPassAttempt();
            PopulateGroundPathInterceptions(clickedHex, false);
            if (passIsDangerous)
            {
                diceRollsPending = defendingHexes.Count;
                Debug.Log($"Dangerous pass detected. Waiting for {diceRollsPending} dice rolls...");
                StartGroundPassInterceptionDiceRollSequence();
            }
            else
            {
                Debug.Log("Pass is not dangerous, moving ball.");
                _ = MoveTheBall(clickedHex);
            }
            ball.DeselectBall();
        }
        else if (difficulty == 2)
        {
            hexGrid.ClearHighlightedHexes();
            if (currentTargetHex == null || clickedHex != currentTargetHex)
            {
                currentTargetHex = clickedHex;
                PopulateGroundPathInterceptions(clickedHex);
                HighlightValidGroundPassPath(validation.PathHexes, validation.IsDangerous);
                diceRollsPending = defendingHexes.Count; // is this relevant here?
                if (diceRollsPending == 0) Debug.Log($"The Stanard pass cannot be intercepted. Click again to confirm or elsewhere to try another path.");
                else Debug.Log($"Dangerous pass detected. If you confirm there will be {diceRollsPending} dice rolls...");
            }
            // Medium Mode: Wait for a second click for confirmation
            else 
            {
                isAwaitingTargetSelection = false;
                CommitToThisAction();
                LogGroundPassAttempt();
                PopulateGroundPathInterceptions(clickedHex);
                if (passIsDangerous)
                {
                    diceRollsPending = defendingHexes.Count; // is this relevant here?
                    Debug.Log($"Dangerous pass detected. Waiting for {diceRollsPending} dice rolls...");
                    StartGroundPassInterceptionDiceRollSequence();
                }
                else
                {
                    Debug.Log("Pass is not dangerous, moving ball.");
                    // MoveTheBall(clickedHex); // Execute pass
                    _ = MoveTheBall(clickedHex); // Execute pass
                }
                ball.DeselectBall();  
            }
            
        }
        else if (difficulty == 1) // Easy Mode: Handle hover and clicks with immediate highlights
        {
            hexGrid.ClearHighlightedHexes();
            if (currentTargetHex == null || clickedHex != currentTargetHex)
            {
                currentTargetHex = clickedHex;
                PopulateGroundPathInterceptions(clickedHex, false);
                diceRollsPending = defendingHexes.Count;
                hoveredPreviewHex = null;
                HighlightCommittedTarget();
                latestValidationInstruction = GetEasyModeCommittedTargetInstruction(diceRollsPending);
            }
            else
            {
                isAwaitingTargetSelection = false;
                CommitToThisAction();
                LogGroundPassAttempt();
                PopulateGroundPathInterceptions(clickedHex, false);
                if (passIsDangerous)
                {
                    diceRollsPending = defendingHexes.Count;
                    Debug.Log($"Dangerous pass detected. Waiting for {diceRollsPending} dice rolls...");
                    StartGroundPassInterceptionDiceRollSequence();
                }
                else
                {
                    Debug.Log("Pass is not dangerous, moving ball.");
                    _ = MoveTheBall(clickedHex);
                }
                ball.DeselectBall();
            }
        }
    }

    private void UpdateEasyModeHoverPreview(HexCell hoveredHex)
    {
        if (!isActivated || !isAwaitingTargetSelection || MatchManager.Instance.difficulty_level != 1)
        {
            return;
        }

        hexGrid.ClearHighlightedHexes();
        HighlightCommittedTarget();

        if (hoveredHex == null)
        {
            latestValidationInstruction = currentTargetHex != null
                ? GetEasyModeCommittedTargetInstruction(diceRollsPending)
                : $"Hover a target within {imposedDistance} hexes to preview the pass.";
            return;
        }

        GroundPassValidationResult validation = ValidateGroundPassPath(hoveredHex, imposedDistance);
        hexGrid.ClearHighlightedHexes();
        HighlightCommittedTarget();

        if (!validation.IsValid)
        {
            latestValidationInstruction = GetValidationFailureInstruction(validation.FailureReason);
            return;
        }

        PopulateGroundPathInterceptions(hoveredHex, false);
        int previewAttempts = defendingHexes.Count;
        HighlightHoverPreviewPath(validation.PathHexes, hoveredHex, validation.IsDangerous);
        latestValidationInstruction = GetEasyModePreviewInstruction(validation.IsDangerous, previewAttempts);
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

    private void HighlightHoverPreviewPath(List<HexCell> pathHexes, HexCell hoveredHex, bool isDangerous)
    {
        if (pathHexes == null)
        {
            return;
        }

        foreach (HexCell hex in pathHexes)
        {
            if (hex == null)
            {
                continue;
            }

            if (hex == hoveredHex)
            {
                hex.HighlightHex("passTarget");
            }
            else
            {
                hex.HighlightHex(isDangerous ? "dangerousPass" : "ballPath");
            }

            if (!hexGrid.highlightedHexes.Contains(hex))
            {
                hexGrid.highlightedHexes.Add(hex);
            }
        }
    }

    public GroundPassValidationResult ValidateGroundPassPath(HexCell targetHex, int distance)
    {
        hexGrid.ClearHighlightedHexes();
        HexCell ballHex = ball.GetCurrentHex();
        if (ballHex == null || targetHex == null)
        {
            Debug.LogError("Ball or target hex is null!");
            return new GroundPassValidationResult(false, false, null, PassValidationFailureReason.NullTarget);
        }

        List<HexCell> pathHexes = CalculateThickPath(ballHex, targetHex, ball.ballRadius);
        int distanceBetweenHexes = HexGridUtils.GetHexStepDistance(ballHex, targetHex);
        if (distanceBetweenHexes > distance)
        {
            Debug.LogWarning($"Pass is out of range. Maximum steps allowed: {distance}. Current steps: {distanceBetweenHexes}");
            return new GroundPassValidationResult(false, false, pathHexes, PassValidationFailureReason.OutOfRange);
        }

        if (isQuickThrow)
        {
            if (targetHex.isDefenseOccupied)
            {
                Debug.Log($"Quick throw target blocked by defender at hex: {targetHex.coordinates}");
                return new GroundPassValidationResult(false, false, pathHexes, PassValidationFailureReason.TargetOccupiedByDefender);
            }
        }
        else
        {
            foreach (HexCell hex in pathHexes)
            {
                if (hex.isDefenseOccupied)
                {
                    Debug.Log($"Path blocked by defender at hex: {hex.coordinates}");
                    return new GroundPassValidationResult(false, false, pathHexes, PassValidationFailureReason.BlockedByDefender);
                }
            }
        }

        List<HexCell> defenderHexes = hexGrid.GetDefenderHexes();
        List<HexCell> defenderNeighbors = hexGrid.GetDefenderNeighbors(defenderHexes);
        bool isDangerous = IsGroundPassDangerous(pathHexes, targetHex, defenderNeighbors);
        return new GroundPassValidationResult(true, isDangerous, pathHexes, PassValidationFailureReason.None);
    }

    private string GetValidationFailureInstruction(PassValidationFailureReason failureReason)
    {
        return failureReason switch
        {
            PassValidationFailureReason.OutOfRange => "Pass invalid: out of range.",
            PassValidationFailureReason.BlockedByDefender => "Pass invalid: blocked by defender.",
            PassValidationFailureReason.TargetOccupiedByDefender => "Pass invalid: target occupied by defender.",
            PassValidationFailureReason.NullTarget => "Pass invalid: no valid target selected.",
            _ => string.Empty,
        };
    }

    private string GetEasyModePreviewInstruction(bool isDangerous, int interceptionAttempts)
    {
        if (!isDangerous || interceptionAttempts == 0)
        {
            return "Safe pass preview. No interception attempts if you select this target.";
        }

        return $"Dangerous pass preview. {interceptionAttempts} interception attempt{(interceptionAttempts == 1 ? string.Empty : "s")} if you select this target.";
    }

    private string GetEasyModeCommittedTargetInstruction(int interceptionAttempts)
    {
        if (interceptionAttempts <= 0)
        {
            return "Selected target is safe. No interception attempts if you confirm. Click the orange target again to confirm, or hover another hex to preview.";
        }

        return $"Selected target will trigger {interceptionAttempts} interception attempt{(interceptionAttempts == 1 ? string.Empty : "s")} if you confirm. Click the orange target again to confirm, or hover another hex to preview.";
    }

    public void HighlightValidGroundPassPath(List<HexCell> pathHexes, bool isDangerous)
    {
        foreach (HexCell hex in pathHexes)
        {
            if (hex == null) continue; // to next hex (loop)
            hex.HighlightHex(hex == currentTargetHex ? "passTarget" : isDangerous ? "dangerousPass" : "ballPath");
            hexGrid.highlightedHexes.Add(hex);  // Track the highlighted hexes
        }
    }

    public void PopulateGroundPathInterceptions(HexCell targetHex, bool highlightPath = true)
    {
        HexCell ballHex = ball.GetCurrentHex();  // Get the current hex of the ball
        List<HexCell> pathHexes = CalculateThickPath(ballHex, targetHex, ball.ballRadius);
        string joined = string.Join(" -> ", pathHexes.Select(hex => hex.coordinates.ToString()));  
        Debug.Log($"Path: {joined}");
        hexGrid.ClearHighlightedHexes();

        // Initialize danger variables
        passIsDangerous = false;
        interceptionHexes.Clear();
        defendingHexes.Clear();

        foreach (HexCell hex in pathHexes)
        {
            if (highlightPath)
            {
                hex.HighlightHex("ballPath");
                hexGrid.highlightedHexes.Add(hex);
            }
        }

        List<GroundInterceptionCandidate> orderedInterceptors = BuildOrderedInterceptionCandidates(targetHex);
        foreach (GroundInterceptionCandidate candidate in orderedInterceptors)
        {
            string defenderName = candidate.DefenderToken.playerName;
            int defenderTackling = candidate.DefenderToken.tackling;
            int defenderJersey = candidate.DefenderToken.jerseyNumber;
            int requiredRoll = defenderTackling >= 4 ? 10 - defenderTackling : 6;
            string rollDescription = requiredRoll == 6 ? "6" : $"{requiredRoll}+";

            Debug.Log(
                $"{defenderJersey}. {defenderName} at {candidate.DefenderHex.coordinates} with a tackling of {defenderTackling} can intercept with a roll of {rollDescription} at {candidate.ClosestInterceptionHex.coordinates}. " +
                $"Closest interception hex is {candidate.ClosestInterceptionHex.coordinates} ({candidate.ClosestZoiDistanceFromBall} steps from the ball)."
            );

            interceptionHexes.Add(candidate.ClosestInterceptionHex);
            defendingHexes.Add(candidate.DefenderHex);
        }

        passIsDangerous = defendingHexes.Count > 0;
    }

    private async Task MoveTheBall(HexCell trgDestHex)
    {
        await helperFunctions.StartCoroutineAndWait(HandleGroundBallMovement(trgDestHex)); // Execute pass
        MatchManager.Instance.UpdatePossessionAfterPass(trgDestHex);
        finalThirdManager.TriggerFinalThirdPhase();
        MatchManager.Instance.BroadcastEndofGroundBallPass();
        Debug.Log($"Pass completed to {trgDestHex.coordinates}");
        if (trgDestHex.isAttackOccupied)
        {
            LogGroundPassSucess();
        }
        else
        {
            MatchManager.Instance.hangingPassType = "ground";
        }
        CleanUpPass();
    }

    public void LogGroundPassAttempt()
    {
        PlayerToken passer = MatchManager.Instance.LastTokenToTouchTheBallOnPurpose;
        MatchManager.Instance.gameData.gameLog.LogEvent(passer, MatchManager.ActionType.PassAttempt);
    }
    
    public void LogGroundPassSucess()
    {
        PlayerToken passer = MatchManager.Instance.LastTokenToTouchTheBallOnPurpose;
        MatchManager.Instance.gameData.gameLog.LogEvent(passer, MatchManager.ActionType.PassCompleted); // Log CompletedPass
        MatchManager.Instance.SetLastToken(currentTargetHex.GetOccupyingToken());
    }

    void StartGroundPassInterceptionDiceRollSequence()
    {
        Debug.Log($"Defenders with interception chances: {defendingHexes.Count}");
        if (defendingHexes.Count > 0)
        {
            // Start the dice roll process for each defender
            Debug.Log("Starting dice roll sequence... Press R key.");
            defendingHexes = BuildOrderedInterceptionCandidates(currentTargetHex, defendingHexes)
                .Select(candidate => candidate.DefenderHex)
                .ToList();
            currentDefenderHex = defendingHexes[0];  // Start with the closest defender
            isWaitingForDiceRoll = true;
        }
        else
        {
            Debug.LogWarning("No defenders in ZOI. This should never appear unless the path is clear.");
            return;
        }
    }

    public void PerformGroundInterceptionDiceRoll(int? rigroll = null)
    {
        if (currentDefenderHex != null)
        {
            // Roll the dice (1 to 6)
            // int diceRoll = 6; // God Mode
            // int diceRoll = 1; // Stupid Mode
            var (returnedRoll, returnedJackpot) = helperFunctions.DiceRoll();
            int diceRoll = rigroll ?? returnedRoll;
            // Retrieve the defender token
            PlayerToken defenderToken = currentDefenderHex.occupyingToken;
            if (defenderToken == null)
            {
                Debug.LogError($"No PlayerToken found on defender's hex at {currentDefenderHex.coordinates}. This should not happen.");
                return;
            }
            Debug.Log($"Dice roll by {defenderToken.name} at {currentDefenderHex.coordinates}: {diceRoll}");
            MatchManager.Instance.gameData.gameLog.LogEvent(defenderToken, MatchManager.ActionType.InterceptionAttempt);
            // Debug.Log($"Dice roll by defender at {currentDefenderHex.coordinates}: {diceRoll}");
            isWaitingForDiceRoll = false;
            // if (diceRoll == 6)
            if (diceRoll == 6 || diceRoll + defenderToken.tackling >= 10)
            {
                // Defender successfully intercepts the pass
                Debug.Log($"Pass intercepted by {defenderToken.name} at {currentDefenderHex.coordinates}!");
                MatchManager.Instance.gameData.gameLog.LogEvent(
                    defenderToken
                    , MatchManager.ActionType.InterceptionSuccess
                    , recoveryType: "standard"
                    , connectedToken: MatchManager.Instance.LastTokenToTouchTheBallOnPurpose
                );
                MatchManager.Instance.SetLastToken(defenderToken);
                StartCoroutine(HandleBallInterception(currentDefenderHex));
            }
            else
            {
                Debug.Log($"{defenderToken.name} at {currentDefenderHex.coordinates} failed to intercept.");
                // Move to the next defender, if any
                defendingHexes.Remove(currentDefenderHex);
                if (defendingHexes.Count > 0)
                {
                    currentDefenderHex = defendingHexes[0];  // Move to the next defender
                    Debug.Log("Starting next dice roll sequence... Press R key.");
                    isWaitingForDiceRoll = true; // Wait for the next dice roll
                }
                else
                {
                    // No more defenders to roll, pass is successful
                    Debug.Log("Pass successful! No more defenders to roll.");
                    // Ensure currentTargetHex is set before movement
                    if (currentTargetHex == null)
                    {
                        Debug.LogError("currentTargetHex is null despite the pass being valid.");
                    }
                    _ = MoveTheBall(currentTargetHex);
                }
            }
        }
    }

    private IEnumerator HandleBallInterception(HexCell defenderHex)
    {
        yield return StartCoroutine(HandleGroundBallMovement(defenderHex));  // Move the ball to the defender's hex
        // Call UpdatePossessionAfterPass after the ball has moved to the defender's hex
        MatchManager.Instance.ChangePossession();  // Possession is now changed to the other team
        MatchManager.Instance.UpdatePossessionAfterPass(defenderHex);  // Update possession after the ball has reached the defender's hex
        finalThirdManager.TriggerFinalThirdPhase();
        ResetGroundPassInterceptionDiceRolls();
        CleanUpPass();
        MatchManager.Instance.BroadcastAnyOtherScenario();
    }
    
    public void CleanUpPass()
    {
        hexGrid.ClearHighlightedHexes();
        isAvailable = false;
        isActivated = false;
        isAwaitingTargetSelection = false;
        currentTargetHex = null;  // Reset current target hex
        hoveredPreviewHex = null;
        latestValidationInstruction = string.Empty;
        // imposedDistance = 11;  // Reset imposed distance
        ResetGroundPassInterceptionDiceRolls();
        isQuickThrow = false;  // Reset quick throw state
    }

    void ResetGroundPassInterceptionDiceRolls()
    {
        // Reset variables after the dice roll sequence
        defendingHexes.Clear();
        interceptionHexes.Clear();
        diceRollsPending = 0;
        currentDefenderHex = null;
    }

    public IEnumerator HandleGroundBallMovement(HexCell targetHex, int? speed = null)
    {
        // Ensure the ball and targetHex are valid
        if (ball == null)
        {
            Debug.LogError("Ball reference is null in HandleGroundBallMovement!");
            yield break;
        }
        if (targetHex == null)
        {
            Debug.LogError("Target Hex is null in HandleGroundBallMovement!");
            Debug.LogError($"currentTargetHex: {currentTargetHex}, isWaitingForDiceRoll: {isWaitingForDiceRoll}");
            yield break;
        }
        // Set thegame status to StandardPassMoving
        // MatchManager.Instance.currentState = MatchManager.GameState.StandardPassMoving;
        // Wait for the ball movement to complete
        yield return StartCoroutine(ball.MoveToCell(targetHex, speed));
        // Adjust the ball's height based on occupancy (after movement is completed)
        ball.AdjustBallHeightBasedOnOccupancy();  // Ensure this method is public in Ball.cs
        // Now clear the highlights after the movement
        hexGrid.ClearHighlightedHexes();
        // Debug.Log("Highlights cleared after ball movement.");
        if (speed != null) yield break;
        // finalThirdManager.TriggerFinalThirdPhase();
    }

    private bool IsGroundPassDangerous(List<HexCell> pathHexes, HexCell targetHex, List<HexCell> defenderNeighbors)
    {
        List<HexCell> relevantInterceptionHexes = GetRelevantInterceptionHexes(pathHexes, targetHex);
        return relevantInterceptionHexes.Any(defenderNeighbors.Contains);
    }

    private List<HexCell> GetRelevantInterceptionHexes(List<HexCell> pathHexes, HexCell targetHex)
    {
        if (pathHexes == null)
        {
            return new List<HexCell>();
        }

        if (isQuickThrow)
        {
            return pathHexes.Where(hex => hex == targetHex).ToList();
        }

        return pathHexes
            .Where(hex => hex != null && !hex.isAttackOccupied)
            .ToList();
    }

    private List<GroundInterceptionCandidate> BuildOrderedInterceptionCandidates(HexCell targetHex, IEnumerable<HexCell> candidateDefenders = null)
    {
        HexCell ballHex = ball.GetCurrentHex();
        if (ballHex == null || targetHex == null)
        {
            return new List<GroundInterceptionCandidate>();
        }

        List<HexCell> pathHexes = CalculateThickPath(ballHex, targetHex, ball.ballRadius);
        List<HexCell> relevantInterceptionHexes = GetRelevantInterceptionHexes(pathHexes, targetHex);
        HashSet<HexCell> relevantInterceptionHexSet = new HashSet<HexCell>(relevantInterceptionHexes);

        IEnumerable<HexCell> defendersToEvaluate = candidateDefenders ?? hexGrid.GetDefenderHexes();
        List<GroundInterceptionCandidate> orderedCandidates = new List<GroundInterceptionCandidate>();

        foreach (HexCell defenderHex in defendersToEvaluate.Where(hex => hex != null))
        {
            PlayerToken defenderToken = defenderHex.occupyingToken;
            if (defenderToken == null)
            {
                continue;
            }

            List<HexCell> influencedHexes = defenderHex
                .GetNeighbors(hexGrid)
                .Where(hex => hex != null && relevantInterceptionHexSet.Contains(hex))
                .ToList();

            if (influencedHexes.Count == 0)
            {
                continue;
            }

            HexCell closestInterceptionHex = influencedHexes
                .OrderBy(hex => HexGridUtils.GetHexStepDistance(ballHex, hex))
                .ThenBy(hex => hex.coordinates.x)
                .ThenBy(hex => hex.coordinates.z)
                .First();

            orderedCandidates.Add(new GroundInterceptionCandidate
            {
                DefenderHex = defenderHex,
                DefenderToken = defenderToken,
                ClosestInterceptionHex = closestInterceptionHex,
                ClosestZoiDistanceFromBall = HexGridUtils.GetHexStepDistance(ballHex, closestInterceptionHex),
                DefenderDistanceFromBall = HexGridUtils.GetHexStepDistance(ballHex, defenderHex),
            });
        }

        return orderedCandidates
            .OrderBy(candidate => candidate.ClosestZoiDistanceFromBall)
            .ThenBy(candidate => candidate.DefenderDistanceFromBall)
            .ThenBy(candidate => candidate.DefenderToken.tackling)
            .ThenBy(candidate => candidate.DefenderToken.playerName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private sealed class GroundInterceptionCandidate
    {
        public HexCell DefenderHex;
        public PlayerToken DefenderToken;
        public HexCell ClosestInterceptionHex;
        public int ClosestZoiDistanceFromBall;
        public int DefenderDistanceFromBall;
    }

    public List<HexCell> CalculateThickPath(HexCell startHex, HexCell endHex, float ballRadius)
    {
        List<HexCell> path = new List<HexCell>();
        string logContent = $"Ball Radius: {ballRadius}\n";
        string startHexCoordinates = $"({startHex.coordinates.x}, {startHex.coordinates.z})";
        string endHexCoordinates = $"({endHex.coordinates.x}, {endHex.coordinates.z})";
        logContent += $"Starting Hex: {startHexCoordinates}, Target Hex: {endHexCoordinates}\n";

        // Get world positions of the start and end hex centers
        Vector3 startPos = startHex.GetHexCenter();
        Vector3 endPos = endHex.GetHexCenter();

        // Step 2: Get a list of candidate hexes based on the bounding box
        List<HexCell> candidateHexes = GetCandidateGroundPathHexes(startHex, endHex, ballRadius);

        // Step 3: Loop through the candidate hexes and check distances to the parallel lines
        foreach (HexCell candidateHex in candidateHexes)
        {
            Vector3 candidatePos = candidateHex.GetHexCenter();

            // Check the distance from the candidate hex to the main line
            float distanceToLine = DistanceFromPointToLine(candidatePos, startPos, endPos);

            if (distanceToLine <= ballRadius)
            {
                // Hex is within the thick path
                if (!path.Contains(candidateHex))
                {
                    path.Add(candidateHex);
                    logContent += $"Added Hex: ({candidateHex.coordinates.x}, {candidateHex.coordinates.z}), Distance to Line: {distanceToLine}, Radius: {ballRadius}\n";
                }
            }
            else
            {
                logContent += $"Not Added: ({candidateHex.coordinates.x}, {candidateHex.coordinates.z}), Distance: {distanceToLine} exceeds Ball Radius: {ballRadius}\n";
            }
        }
        path.Remove(startHex);
        // Log the final highlighted path to the file
        string highlightedPath = "Highlighted Path: ";
        foreach (HexCell hex in path)
        {
            highlightedPath += $"({hex.coordinates.x}, {hex.coordinates.z}), ";
        }
        highlightedPath = highlightedPath.TrimEnd(new char[] { ',', ' ' });
        logContent += highlightedPath;

        // Save the log to a file
        SaveLogToFile(logContent, startHexCoordinates, endHexCoordinates);

        return path;
    }

    public List<HexCell> GetCandidateGroundPathHexes(HexCell startHex, HexCell endHex, float ballRadius)
    {
        List<HexCell> candidates = new List<HexCell>();
        // Get the axial coordinates of the start and end hexes
        Vector3Int startCoords = startHex.coordinates;
        Vector3Int endCoords = endHex.coordinates;

        // Determine the bounds (min and max x and z)
        int minX = Mathf.Min(startCoords.x, endCoords.x) - 1;
        int maxX = Mathf.Max(startCoords.x, endCoords.x) + 1;
        int minZ = Mathf.Min(startCoords.z, endCoords.z) - 1;
        int maxZ = Mathf.Max(startCoords.z, endCoords.z) + 1;

        // Loop through all hexes in the bounding box
        for (int x = minX; x <= maxX; x++)
        {
            for (int z = minZ; z <= maxZ; z++)
            {
                Vector3Int coords = new Vector3Int(x, 0, z);
                HexCell hex = hexGrid.GetHexCellAt(coords);

                if (hex != null)
                {
                    candidates.Add(hex);
                }
            }
        }
        return candidates;
    }

    float DistanceFromPointToLine(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
    {
        Vector3 lineDirection = lineEnd - lineStart;
        float lineLength = lineDirection.magnitude;
        lineDirection.Normalize();

        // Project the point onto the line, clamping between the start and end of the line
        float projectedLength = Mathf.Clamp(Vector3.Dot(point - lineStart, lineDirection), 0, lineLength);

        // Calculate the closest point on the line
        Vector3 closestPoint = lineStart + lineDirection * projectedLength;

        // Return the distance from the point to the closest point on the line
        return Vector3.Distance(point, closestPoint);
    }

    void SaveLogToFile(string logText, string startHex, string endHex)
    {
        // // Define the file path (you can customize this path)
        // string filePath = Application.dataPath + $"/Logs/HexPath_{startHex}_to_{endHex}.txt";

        // // Ensure the directory exists
        // Directory.CreateDirectory(Path.GetDirectoryName(filePath));

        // // Write the log text to the file (overwrite mode)
        // using (StreamWriter writer = new StreamWriter(filePath))
        // {
        //     writer.WriteLine(logText);
        // }

        // Debug.Log($"Log saved to: {filePath}");
    }

    public string GetDebugStatus()
    {
        StringBuilder sb = new();
        sb.Append("GBM: ");

        if (isActivated) sb.Append("isActivated, ");
        if (isAvailable) sb.Append("isAvailable, ");
        if (isAwaitingTargetSelection) sb.Append("isAwaitingTargetSelection, ");
        if (isWaitingForDiceRoll) sb.Append("isWaitingForDiceRoll, ");
        if (currentTargetHex != null) sb.Append($"currentTargetHex: {currentTargetHex.name}, ");
        if (defendingHexes.Count != 0) sb.Append($"defendingHexes: {helperFunctions.PrintListNamesOneLine(defendingHexes)}, ");

        if (sb.Length >= 2 && sb[^2] == ',') sb.Length -= 2; // Trim trailing comma
        return sb.ToString();
    }

    public string GetInstructions()
    {
        StringBuilder sb = new();
        if (goalKeeperManager.isActivated) return "";
        if (finalThirdManager.isActivated) return "";
        if (freeKickManager.isWaitingForExecution) return "";
        if (isAvailable) sb.Append("Press [P] to Play a Standard Pass, ");
        if (isActivated)
        {
            sb.Append("SP: ");
        }
        if (isAwaitingTargetSelection)
        {
            if (MatchManager.Instance.difficulty_level == 3)
            {
                if (!string.IsNullOrWhiteSpace(latestValidationInstruction))
                {
                    sb.Append($"{latestValidationInstruction} ");
                }
                else
                {
                    sb.Append($"Click on a valid target within {imposedDistance} hexes to attempt a pass. ");
                }
            }
            else
            {
                if (MatchManager.Instance.difficulty_level == 1)
                {
                    if (!string.IsNullOrWhiteSpace(latestValidationInstruction))
                    {
                        sb.Append($"{latestValidationInstruction} ");
                    }
                    else
                    {
                        sb.Append($"Hover a target within {imposedDistance} hexes to preview the pass. ");
                    }

                if (currentTargetHex != null)
                {
                    sb.Append("Orange hex is the intended target. Click it again to confirm, or click another valid target to switch. ");
                    if (diceRollsPending > 0)
                    {
                        sb.Append($"Confirming this target will trigger {diceRollsPending} interception attempt{(diceRollsPending == 1 ? string.Empty : "s")}. ");
                    }
                    else
                    {
                        sb.Append("Confirming this target will not trigger any interception attempts. ");
                    }
                }
            }
            else
                {
                    sb.Append($"Click on a Hex up to {imposedDistance} Hexes away from {MatchManager.Instance.LastTokenToTouchTheBallOnPurpose.name}, ");
                    if (currentTargetHex != null) sb.Append($"or click the yellow Hex again to confirm, ");
                    if (currentTargetHex != null && diceRollsPending > 0) sb.Append($"there will be {diceRollsPending} attempts to intercept the pass, ");
                }
            }
        }
        if (isWaitingForDiceRoll)
        {
            string rollneeded = currentDefenderHex.GetOccupyingToken().tackling <= 4 ? "6" : currentDefenderHex.GetOccupyingToken().tackling == 6 ? "4+": "5+";
            sb.Append($"Press [R] to roll for interception with {currentDefenderHex.GetOccupyingToken().name}, a roll of {rollneeded} is needed, ");
        }

        if (sb.Length >= 2 && sb[^2] == ',') sb.Length -= 2; // Trim trailing comma
        return sb.ToString();
    }
}
