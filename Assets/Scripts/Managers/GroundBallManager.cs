using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Linq;
using System.Threading.Tasks;

public class GroundBallManager : MonoBehaviour
{
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
    public bool isKickoffPass = false;
    public HexCell currentTargetHex = null;   // The currently selected target hex
    [SerializeField]
    public bool isWaitingForDiceRoll = false; // To check if we are waiting for dice rolls
    public bool passIsDangerous = false;      // To check if the pass is dangerous
    private bool passHasPathInteractions = false;
    private HexCell currentDefenderHex = null;                      // The defender hex currently rolling the dice
    private HexCell hoveredPreviewHex = null;
    private readonly List<GroundBallPathInteraction> pathInteractions = new();
    private readonly HashSet<PlayerToken> attemptedOutfieldInterceptors = new();
    private GroundBallPathInteraction currentPathInteraction = null;
    private bool currentPassGkWallAttempted = false;
    private bool currentPassGkBoxMoveHandled = false;
    private bool previewHasConditionalGoalkeeperInteraction = false;
    private PlayerToken currentPasser = null;
    [SerializeField]
    public List<HexCell> defendingHexes = new List<HexCell>();     // List of defenders responsible for each interception hex
    [SerializeField]
    private List<HexCell> interceptionHexes = new List<HexCell>();  // List of interception hexes
    public int diceRollsPending = 0;          // Number of pending dice rolls
    private string latestValidationInstruction = string.Empty;
    private PlayerToken pendingSetPieceTakerForCommit = null;

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

        MatchManager matchManager = MatchManager.Instance;
        if (matchManager == null || (matchManager.difficulty_level != 1 && matchManager.difficulty_level != 2))
        {
            return;
        }

        if (hoveredPreviewHex == hex)
        {
            return;
        }

        hoveredPreviewHex = hex;
        if (matchManager.difficulty_level == 1)
        {
            UpdateEasyModeHoverPreview(hex);
        }
        else
        {
            UpdateMediumModeHoverPreview(hex);
        }
    }

    private void OnKeyReceived(KeyPressData keyData)
    {
        if (keyData.isConsumed) return;
        if (isAvailable && !isActivated && !freeKickManager.isWaitingForExecution && keyData.key == KeyCode.P)
        {
            MatchManager.Instance.TriggerStandardPass();
            keyData.isConsumed = true;
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
            keyData.isConsumed = true;
            return;
        }
        if (isActivated)
        {
            bool hasRollOverride = RollInputOverride.TryParse(keyData, out RollInputOverride rollOverride);
            if (isWaitingForDiceRoll && (keyData.key == KeyCode.R || hasRollOverride))
            {
                // Check if waiting for dice rolls and the R key is pressed
                PerformGroundInterceptionDiceRoll(hasRollOverride ? rollOverride : null);  // Trigger the dice roll when R is pressed
                keyData.isConsumed = true;
                return;
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

    public void ActivateKickoffGroundBall(PlayerToken kickoffTaker)
    {
        CleanUpPass();
        isKickoffPass = true;
        currentPasser = kickoffTaker;
        if (kickoffTaker != null)
        {
            SetPendingSetPieceTakerForCommit(kickoffTaker);
        }

        ActivateGroundBall();
        CommitToThisAction();
    }

    public void CommitToThisAction()
    {
        CaptureCurrentPasser();
        if (pendingSetPieceTakerForCommit != null)
        {
            MatchManager.Instance.MarkSetPieceTakerForNextTouchExclusion(pendingSetPieceTakerForCommit);
            pendingSetPieceTakerForCommit = null;
        }

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
            passHasPathInteractions = false;
            previewHasConditionalGoalkeeperInteraction = false;
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
            if (passHasPathInteractions)
            {
                diceRollsPending = GroundPassCommon.CountDangerousInteractions(BuildOrderedPathInteractions(clickedHex));
                Debug.Log(passIsDangerous
                    ? $"Dangerous pass detected. Waiting for {diceRollsPending} dice rolls..."
                    : "Pass enters the box and will offer the defending GK free move before continuing.");
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
                passIsDangerous = false;
                passHasPathInteractions = false;
                previewHasConditionalGoalkeeperInteraction = false;
                diceRollsPending = 0;
                ResetGroundPassInterceptionDiceRolls();
                HighlightMediumModeTargets(clickedHex);
                latestValidationInstruction = "Click the orange target again to confirm, or choose another valid target.";
                Debug.Log("Standard pass target selected. Click again to confirm or elsewhere to try another target.");
            }
            // Medium Mode: Wait for a second click for confirmation
            else 
            {
                isAwaitingTargetSelection = false;
                CommitToThisAction();
                LogGroundPassAttempt();
                PopulateGroundPathInterceptions(clickedHex);
                if (passHasPathInteractions)
                {
                    diceRollsPending = GroundPassCommon.CountDangerousInteractions(BuildOrderedPathInteractions(clickedHex));
                    Debug.Log(passIsDangerous
                        ? $"Dangerous pass detected. Waiting for {diceRollsPending} dice rolls..."
                        : "Pass enters the box and will offer the defending GK free move before continuing.");
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
                hoveredPreviewHex = null;
                RenderEasyModeSelectedTargetPreview(validation);
            }
            else
            {
                isAwaitingTargetSelection = false;
                CommitToThisAction();
                LogGroundPassAttempt();
                PopulateGroundPathInterceptions(clickedHex);
                if (passHasPathInteractions)
                {
                    diceRollsPending = GroundPassCommon.CountDangerousInteractions(BuildOrderedPathInteractions(clickedHex));
                    Debug.Log(passIsDangerous
                        ? $"Dangerous pass detected. Waiting for {diceRollsPending} dice rolls..."
                        : "Pass enters the box and will offer the defending GK free move before continuing.");
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

    private void UpdateMediumModeHoverPreview(HexCell hoveredHex)
    {
        if (!isActivated || !isAwaitingTargetSelection || MatchManager.Instance.difficulty_level != 2)
        {
            return;
        }

        hexGrid.ClearHighlightedHexes();

        if (hoveredHex == null)
        {
            HighlightCommittedTarget();
            latestValidationInstruction = currentTargetHex != null
                ? "Click the orange target again to confirm, or choose another valid target."
                : $"Hover a target within {imposedDistance} hexes, then click it to select.";
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

        HighlightMediumModeTargets(hoveredHex);
        latestValidationInstruction = hoveredHex == currentTargetHex
            ? "Click the orange target again to confirm, or choose another valid target."
            : currentTargetHex != null
                ? "Click this orange target to switch selection, or click the selected orange target again to confirm."
                : "Click this orange target to select it.";
    }

    private void HighlightMediumModeTargets(HexCell hoveredHex)
    {
        HighlightCommittedTarget();

        if (hoveredHex == null || hoveredHex == currentTargetHex)
        {
            return;
        }

        hoveredHex.HighlightHex("passTargetCommitted");
        if (!hexGrid.highlightedHexes.Contains(hoveredHex))
        {
            hexGrid.highlightedHexes.Add(hoveredHex);
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
                ? GetEasyModeCommittedTargetInstruction(diceRollsPending, previewHasConditionalGoalkeeperInteraction)
                : $"Hover a target within {imposedDistance} hexes to preview the pass.";
            return;
        }

        if (hoveredHex == currentTargetHex)
        {
            RenderEasyModeSelectedTargetPreview();
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
        List<GroundBallPathInteraction> previewInteractions = BuildOrderedPathInteractions(hoveredHex);
        int previewAttempts = GroundPassCommon.CountDangerousInteractions(previewInteractions);
        previewHasConditionalGoalkeeperInteraction = GroundPassCommon.HasConditionalGoalkeeperInteractionAfterBoxMove(previewInteractions);
        bool previewIsDangerous = passIsDangerous;
        HighlightHoverPreviewPath(validation.PathHexes, hoveredHex, previewIsDangerous);
        HighlightCommittedTarget();
        latestValidationInstruction = GetEasyModePreviewInstruction(previewIsDangerous, previewAttempts, previewHasConditionalGoalkeeperInteraction);
    }

    private void RenderEasyModeSelectedTargetPreview(GroundPassValidationResult? knownValidation = null)
    {
        if (currentTargetHex == null)
        {
            return;
        }

        GroundPassValidationResult validation = knownValidation ?? ValidateGroundPassPath(currentTargetHex, imposedDistance);
        hexGrid.ClearHighlightedHexes();

        if (!validation.IsValid)
        {
            HighlightCommittedTarget();
            latestValidationInstruction = GetValidationFailureInstruction(validation.FailureReason);
            return;
        }

        PopulateGroundPathInterceptions(currentTargetHex, false);
        List<GroundBallPathInteraction> previewInteractions = BuildOrderedPathInteractions(currentTargetHex);
        diceRollsPending = GroundPassCommon.CountDangerousInteractions(previewInteractions);
        previewHasConditionalGoalkeeperInteraction = GroundPassCommon.HasConditionalGoalkeeperInteractionAfterBoxMove(previewInteractions);
        HighlightHoverPreviewPath(validation.PathHexes, currentTargetHex, passIsDangerous);
        latestValidationInstruction = GetEasyModeCommittedTargetInstruction(diceRollsPending, previewHasConditionalGoalkeeperInteraction);
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

            if (hex == currentTargetHex)
            {
                hex.HighlightHex("passTargetCommitted");
            }
            else if (hex == hoveredHex)
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
        PlayerToken targetToken = targetHex != null ? targetHex.GetOccupyingToken() : null;
        if (targetToken != null
            && MatchManager.Instance != null
            && (!MatchManager.Instance.CanTokenCollectHangingPass(targetToken)
                || targetToken == pendingSetPieceTakerForCommit))
        {
            Debug.LogWarning($"{targetToken.name} cannot be the next player to touch the ball after taking the set piece.");
            return new GroundPassValidationResult(false, false, null, PassValidationFailureReason.TargetExcludedFromNextTouch);
        }

        bool useEasyOwnHalfKickoffRules = IsEasyOwnHalfKickoffPassTarget(targetHex);
        return GroundPassCommon.ValidateStandardPassPath(
            hexGrid,
            ball,
            targetHex,
            distance,
            isQuickThrow,
            ignoreMaxDistance: useEasyOwnHalfKickoffRules,
            suppressInterceptions: useEasyOwnHalfKickoffRules
        );
    }

    private string GetValidationFailureInstruction(PassValidationFailureReason failureReason)
    {
        return GroundPassCommon.GetValidationFailureInstruction(failureReason);
    }

    private string GetEasyModePreviewInstruction(bool isDangerous, int interceptionAttempts, bool hasConditionalGoalkeeperInteraction)
    {
        if (!isDangerous || interceptionAttempts == 0)
        {
            if (hasConditionalGoalkeeperInteraction)
            {
                return currentTargetHex != null
                    ? "Preview target enters the box: GK free move comes first, so current GK dive/interception threats are conditional and recalculate after that move. Click this hex to switch target, or click the orange target to confirm the current selection."
                    : "Preview target enters the box: GK free move comes first, so current GK dive/interception threats are conditional and recalculate after that move. Click this hex to select it.";
            }

            return currentTargetHex != null
                ? "Preview target is safe. Click this hex to switch target, or click the orange target to confirm the current selection."
                : "Preview target is safe. Click this hex to select it.";
        }

        return currentTargetHex != null
            ? $"Preview target: {interceptionAttempts} interception attempt{(interceptionAttempts == 1 ? string.Empty : "s")} if selected. Click this hex to switch target, or click the orange target to confirm the current selection."
            : $"Preview target: {interceptionAttempts} interception attempt{(interceptionAttempts == 1 ? string.Empty : "s")} if selected. Click this hex to select it.";
    }

    private string GetEasyModeCommittedTargetInstruction(int interceptionAttempts, bool hasConditionalGoalkeeperInteraction = false)
    {
        if (interceptionAttempts <= 0)
        {
            if (hasConditionalGoalkeeperInteraction)
            {
                return "Selected target enters the box: GK free move comes first, so current GK dive/interception threats are conditional and recalculate after that move. Click again to confirm, or hover another valid hex to preview.";
            }

            return "Selected target is safe. Click again to confirm, or hover another valid hex to preview.";
        }

        return $"Selected target: {interceptionAttempts} interception attempt{(interceptionAttempts == 1 ? string.Empty : "s")} if confirmed. Click again to confirm, or hover another valid hex to preview.";
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
        passHasPathInteractions = false;
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

        List<GroundBallPathInteraction> orderedInteractions = BuildOrderedPathInteractions(targetHex);
        foreach (GroundBallPathInteraction interaction in orderedInteractions)
        {
            if (interaction.Type == GroundBallPathInteractionType.GoalkeeperBoxMove)
            {
                Debug.Log($"Defending GK free box move will be offered when the pass reaches {interaction.InteractionHex.coordinates}.");
                continue;
            }

            if (interaction.Type == GroundBallPathInteractionType.GoalkeeperDirectPickup)
            {
                Debug.Log($"{interaction.DefenderToken.name} is directly on the pass path at {interaction.InteractionHex.coordinates} and can recover the ball.");
                interceptionHexes.Add(interaction.InteractionHex);
                defendingHexes.Add(interaction.DefenderHex);
                continue;
            }

            if (interaction.Type == GroundBallPathInteractionType.GoalkeeperWallSave)
            {
                Debug.Log($"{interaction.DefenderToken.name} can dive from the GK Wall at {interaction.InteractionHex.coordinates} with penalty {interaction.GkPenalty}.");
                interceptionHexes.Add(interaction.InteractionHex);
                defendingHexes.Add(interaction.DefenderHex);
                continue;
            }

            GroundInterceptionCandidate candidate = interaction.InterceptionCandidate;
            string defenderName = candidate.DefenderToken.playerName;
            int defenderTackling = candidate.DefenderToken.tackling;
            int defenderJersey = candidate.DefenderToken.jerseyNumber;
            int requiredRoll = defenderTackling >= 4 ? 10 - defenderTackling : 6;
            string rollDescription = requiredRoll == 6 ? "6" : $"{requiredRoll}+";

            Debug.Log(
                $"{defenderJersey}. {defenderName} at {candidate.DefenderHex.coordinates} with a tackling of {defenderTackling} can intercept with a roll of {rollDescription} at {candidate.ClosestInterceptionHex.coordinates}. " +
                $"Closest interception hex is {candidate.ClosestInterceptionHex.coordinates} ({candidate.ClosestInterceptionDistanceFromBall} steps from the ball)."
            );

            interceptionHexes.Add(candidate.ClosestInterceptionHex);
            defendingHexes.Add(candidate.DefenderHex);
        }

        passHasPathInteractions = orderedInteractions.Count > 0;
        previewHasConditionalGoalkeeperInteraction = GroundPassCommon.HasConditionalGoalkeeperInteractionAfterBoxMove(orderedInteractions);
        passIsDangerous = GroundPassCommon.CountDangerousInteractions(orderedInteractions) > 0;
    }

    private async Task MoveTheBall(HexCell trgDestHex)
    {
        await helperFunctions.StartCoroutineAndWait(HandleGroundBallMovement(trgDestHex, allowGKBoxMove: false)); // Execute pass
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
            MatchManager.Instance.SetHangingPass("ground");
        }
        CleanUpPass();
    }

    public void LogGroundPassAttempt()
    {
        PlayerToken passer = ResolveCurrentPasser();
        if (passer == null)
        {
            Debug.LogWarning("Ground pass attempt was not logged because no passer token is available.");
            return;
        }

        MatchManager.Instance.gameData.gameLog.LogEvent(passer, MatchManager.ActionType.PassAttempt);
    }
    
    public void LogGroundPassSucess()
    {
        PlayerToken passer = ResolveCurrentPasser();
        if (passer == null)
        {
            Debug.LogWarning("Ground pass completion was not logged because no passer token is available.");
        }
        else
        {
            MatchManager.Instance.gameData.gameLog.LogEvent(passer, MatchManager.ActionType.PassCompleted); // Log CompletedPass
        }

        PlayerToken receiver = currentTargetHex != null ? currentTargetHex.GetOccupyingToken() : null;
        if (receiver != null)
        {
            MatchManager.Instance.SetLastToken(receiver);
        }
        else
        {
            string targetCoordinates = currentTargetHex != null ? currentTargetHex.coordinates.ToString() : "null";
            Debug.LogWarning($"Ground pass completed to {targetCoordinates}, but no receiving token was found.");
        }
    }

    private void CaptureCurrentPasser()
    {
        if (currentPasser != null)
        {
            return;
        }

        currentPasser = MatchManager.Instance != null
            ? MatchManager.Instance.LastTokenToTouchTheBallOnPurpose
            : null;

        if (currentPasser == null)
        {
            currentPasser = ball != null ? ball.GetCurrentHex()?.GetOccupyingToken() : null;
        }
    }

    private PlayerToken ResolveCurrentPasser()
    {
        CaptureCurrentPasser();
        return currentPasser;
    }

    void StartGroundPassInterceptionDiceRollSequence()
    {
        pathInteractions.Clear();
        pathInteractions.AddRange(BuildOrderedPathInteractions(currentTargetHex));
        Debug.Log($"Ball path interactions: {pathInteractions.Count}");
        if (pathInteractions.Count > 0)
        {
            Debug.Log("Starting ball path interaction sequence...");
            ProcessCurrentPathInteraction();
        }
        else
        {
            Debug.LogWarning("No path interactions are available. Completing pass.");
            _ = MoveTheBall(currentTargetHex);
        }
    }

    public void PerformGroundInterceptionDiceRoll(int? rigroll = null)
    {
        RollInputOverride? rollOverride = rigroll.HasValue
            ? new RollInputOverride
            {
                hasOverride = true,
                roll = rigroll.Value,
                isJackpot = false
            }
            : null;
        PerformGroundInterceptionDiceRoll(rollOverride);
    }

    public void PerformGroundInterceptionDiceRoll(RollInputOverride? rollOverride)
    {
        if (currentPathInteraction != null && currentPathInteraction.Type == GroundBallPathInteractionType.GoalkeeperWallSave)
        {
            StartCoroutine(ResolveGoalkeeperWallSaveRoll(currentPathInteraction, rollOverride));
            return;
        }

        if (currentDefenderHex == null || currentPathInteraction == null)
        {
            return;
        }

        // Roll the dice (1 to 6)
        // int diceRoll = 6; // God Mode
        // int diceRoll = 1; // Stupid Mode
        var (returnedRoll, returnedJackpot) = helperFunctions.DiceRoll();
        int diceRoll = GetRollValueWithoutJackpot(rollOverride, returnedRoll);
        // Retrieve the defender token
        PlayerToken defenderToken = currentDefenderHex.occupyingToken;
        if (defenderToken == null)
        {
            Debug.LogError($"No PlayerToken found on defender's hex at {currentDefenderHex.coordinates}. This should not happen.");
            return;
        }
        Debug.Log($"Dice roll by {defenderToken.name} at {currentDefenderHex.coordinates}: {diceRoll}");
        MatchManager.Instance.gameData.gameLog.LogExpectedRecovery(
            defenderToken,
            ExpectedStatsCalculator.CalculateRecoveryProbability(defenderToken),
            MatchManager.Instance.LastTokenToTouchTheBallOnPurpose,
            "standard");
        MatchManager.Instance.gameData.gameLog.LogEvent(defenderToken, MatchManager.ActionType.InterceptionAttempt);
        // Debug.Log($"Dice roll by defender at {currentDefenderHex.coordinates}: {diceRoll}");
        isWaitingForDiceRoll = false;
        // if (diceRoll == 6)
        if (diceRoll == 6 || diceRoll + defenderToken.tackling >= 10)
        {
            ResolveGroundInterceptionSuccess(defenderToken);
            return;
        }

        Debug.Log($"{defenderToken.name} at {currentDefenderHex.coordinates} failed to intercept.");
        AdvanceToNextInterceptorOrCompletePass();
    }

    private int GetRollValueWithoutJackpot(RollInputOverride? rollOverride, int returnedRoll)
    {
        if (!rollOverride.HasValue || !rollOverride.Value.hasOverride)
        {
            return returnedRoll;
        }

        return rollOverride.Value.isJackpot ? 6 : rollOverride.Value.roll;
    }

    private void ResolveGroundInterceptionSuccess(PlayerToken defenderToken)
    {
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

    private void AdvanceToNextInterceptorOrCompletePass()
    {
        if (currentPathInteraction != null && currentPathInteraction.Type == GroundBallPathInteractionType.OutfieldInterception)
        {
            attemptedOutfieldInterceptors.Add(currentPathInteraction.DefenderToken);
        }

        pathInteractions.Remove(currentPathInteraction);
        currentPathInteraction = null;
        currentDefenderHex = null;
        if (pathInteractions.Count > 0)
        {
            ProcessCurrentPathInteraction();
            return;
        }

        // No more defenders to roll, pass is successful
        Debug.Log("Pass successful! No more defenders to roll.");
        // Ensure currentTargetHex is set before movement
        if (currentTargetHex == null)
        {
            Debug.LogError("currentTargetHex is null despite the pass being valid.");
            return;
        }
        _ = MoveTheBall(currentTargetHex);
    }

    private IEnumerator HandleBallInterception(HexCell defenderHex)
    {
        yield return StartCoroutine(HandleGroundBallMovement(defenderHex, allowGKBoxMove: false));  // Move the ball to the defender's hex
        PlayerToken recoveringToken = defenderHex != null ? defenderHex.GetOccupyingToken() : null;
        // Call UpdatePossessionAfterPass after the ball has moved to the defender's hex
        MatchManager.Instance.ChangePossession();  // Possession is now changed to the other team
        MatchManager.Instance.UpdatePossessionAfterPass(defenderHex);  // Update possession after the ball has reached the defender's hex
        ResetGroundPassInterceptionDiceRolls();
        CleanUpPass();
        MatchManager.Instance.BroadcastDefensiveRecoveryOutcome(recoveringToken, defenderHex);
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
        isKickoffPass = false;
        currentPasser = null;
        pendingSetPieceTakerForCommit = null;
    }

    public void SetPendingSetPieceTakerForCommit(PlayerToken taker)
    {
        pendingSetPieceTakerForCommit = taker;
    }

    void ResetGroundPassInterceptionDiceRolls()
    {
        // Reset variables after the dice roll sequence
        defendingHexes.Clear();
        interceptionHexes.Clear();
        pathInteractions.Clear();
        attemptedOutfieldInterceptors.Clear();
        diceRollsPending = 0;
        passHasPathInteractions = false;
        currentDefenderHex = null;
        currentPathInteraction = null;
        currentPassGkWallAttempted = false;
        currentPassGkBoxMoveHandled = false;
        previewHasConditionalGoalkeeperInteraction = false;
    }

    public IEnumerator HandleGroundBallMovement(HexCell targetHex, int? speed = null, bool allowGKBoxMove = true)
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
        yield return StartCoroutine(ball.MoveToCell(targetHex, speed, allowGKBoxMove));
        // Adjust the ball's height based on occupancy (after movement is completed)
        ball.AdjustBallHeightBasedOnOccupancy();  // Ensure this method is public in Ball.cs
        // Now clear the highlights after the movement
        hexGrid.ClearHighlightedHexes();
        // Debug.Log("Highlights cleared after ball movement.");
        if (speed != null) yield break;
        // finalThirdManager.TriggerFinalThirdPhase();
    }

    private List<GroundInterceptionCandidate> BuildOrderedInterceptionCandidates(HexCell targetHex, IEnumerable<HexCell> candidateDefenders = null)
    {
        return GroundPassCommon.BuildOrderedInterceptionCandidates(
            hexGrid,
            ball,
            targetHex,
            candidateDefenders,
            isQuickThrow
        );
    }

    private List<GroundBallPathInteraction> BuildOrderedPathInteractions(
        HexCell targetHex,
        int minPathIndex = 0,
        bool includeGoalkeeperBoxMove = true)
    {
        if (IsEasyOwnHalfKickoffPassTarget(targetHex))
        {
            return new List<GroundBallPathInteraction>();
        }

        return GroundPassCommon.BuildOrderedBallPathInteractions(
            hexGrid,
            ball,
            goalKeeperManager,
            targetHex,
            isQuickThrow: isQuickThrow,
            excludeOutfieldDefenders: attemptedOutfieldInterceptors,
            includeGoalkeeperWall: !currentPassGkWallAttempted,
            includeGoalkeeperBoxMove: includeGoalkeeperBoxMove && !currentPassGkBoxMoveHandled,
            minPathIndex: minPathIndex
        );
    }

    private bool IsEasyOwnHalfKickoffPassTarget(HexCell targetHex)
    {
        MatchManager matchManager = MatchManager.Instance;
        if (!isKickoffPass || targetHex == null || matchManager == null || matchManager.difficulty_level != 1)
        {
            return false;
        }

        MatchManager.TeamAttackingDirection attackingDirection = matchManager.teamInAttack == MatchManager.TeamInAttack.Home
            ? matchManager.homeTeamDirection
            : matchManager.awayTeamDirection;

        return attackingDirection == MatchManager.TeamAttackingDirection.LeftToRight
            ? targetHex.coordinates.x < 0
            : targetHex.coordinates.x > 0;
    }

    private void ProcessCurrentPathInteraction()
    {
        if (pathInteractions.Count == 0)
        {
            Debug.Log("Pass successful! No more path interactions to resolve.");
            _ = MoveTheBall(currentTargetHex);
            return;
        }

        currentPathInteraction = pathInteractions[0];
        currentDefenderHex = currentPathInteraction.DefenderHex;

        switch (currentPathInteraction.Type)
        {
            case GroundBallPathInteractionType.GoalkeeperBoxMove:
                StartCoroutine(ResolveGoalkeeperBoxMoveInteraction(currentPathInteraction));
                break;
            case GroundBallPathInteractionType.GoalkeeperDirectPickup:
                StartCoroutine(ResolveGoalkeeperDirectPickup(currentPathInteraction));
                break;
            case GroundBallPathInteractionType.GoalkeeperWallSave:
                int neededRoll = Mathf.Max(1, 10 - currentPathInteraction.DefenderToken.saving - currentPathInteraction.GkPenalty);
                Debug.Log($"{currentPathInteraction.DefenderToken.name} can dive for a GK Wall save at {currentPathInteraction.InteractionHex.coordinates}. Needs {neededRoll}+ with Saving {currentPathInteraction.DefenderToken.saving} and penalty {currentPathInteraction.GkPenalty}, or natural 6/Jackpot. Press [R] to roll.");
                isWaitingForDiceRoll = true;
                break;
            case GroundBallPathInteractionType.OutfieldInterception:
                Debug.Log($"Selected defender for interception: {currentPathInteraction.DefenderToken.name}. Press [R] to roll.");
                isWaitingForDiceRoll = true;
                break;
        }
    }

    private IEnumerator ResolveGoalkeeperBoxMoveInteraction(GroundBallPathInteraction interaction)
    {
        isWaitingForDiceRoll = false;
        pathInteractions.Remove(interaction);
        currentPassGkBoxMoveHandled = true;

        if (interaction != null
            && goalKeeperManager.TryStartGKMoveForPenaltyBox(interaction.InteractionHex.isInPenaltyBox, interaction.InteractionHex, out _))
        {
            yield return StartCoroutine(goalKeeperManager.HandleGKFreeMove());
        }

        pathInteractions.Clear();
        pathInteractions.AddRange(BuildOrderedPathInteractions(currentTargetHex, minPathIndex: 0, includeGoalkeeperBoxMove: false));
        ProcessCurrentPathInteraction();
    }

    private IEnumerator ResolveGoalkeeperDirectPickup(GroundBallPathInteraction interaction)
    {
        if (interaction?.DefenderToken == null || interaction.InteractionHex == null)
        {
            AdvanceToNextInterceptorOrCompletePass();
            yield break;
        }

        Debug.Log($"{interaction.DefenderToken.name} recovers the pass directly at {interaction.InteractionHex.coordinates}.");
        yield return StartCoroutine(goalKeeperManager.ResolveGoalkeeperSaveAndHold(
            interaction.DefenderToken,
            interaction.InteractionHex,
            "direct"));
        CleanUpPass();
    }

    private IEnumerator ResolveGoalkeeperWallSaveRoll(GroundBallPathInteraction interaction, RollInputOverride? rollOverride)
    {
        isWaitingForDiceRoll = false;
        currentPassGkWallAttempted = true;
        if (interaction?.DefenderToken == null || interaction.InteractionHex == null)
        {
            AdvanceToNextInterceptorOrCompletePass();
            yield break;
        }

        PlayerToken goalkeeper = interaction.DefenderToken;
        var (returnedRoll, returnedJackpot) = helperFunctions.DiceRoll();
        bool isJackpot = rollOverride.HasValue && rollOverride.Value.hasOverride
            ? rollOverride.Value.isJackpot
            : returnedJackpot;
        int diceRoll = rollOverride.HasValue && rollOverride.Value.hasOverride
            ? rollOverride.Value.isJackpot ? 6 : rollOverride.Value.roll
            : returnedRoll;
        int savingTotal = diceRoll + goalkeeper.saving + interaction.GkPenalty;

        MatchManager.Instance.gameData.gameLog.LogEvent(goalkeeper, MatchManager.ActionType.SaveAttempt);
        Debug.Log(isJackpot
            ? $"{goalkeeper.name} rolls a Jackpot for the GK Wall save at {interaction.InteractionHex.coordinates}."
            : $"{goalkeeper.name} rolls {diceRoll} + Saving {goalkeeper.saving} + Penalty {interaction.GkPenalty} = {savingTotal} for the GK Wall save.");

        if (isJackpot || diceRoll == 6 || savingTotal >= 10)
        {
            Debug.Log($"{goalkeeper.name} catches the ground pass from the GK Wall.");
            yield return StartCoroutine(goalKeeperManager.ResolveGoalkeeperSaveAndHold(
                goalkeeper,
                interaction.InteractionHex,
                "gkWall"));
            CleanUpPass();
            yield break;
        }

        Debug.Log($"{goalkeeper.name} failed the GK Wall save. Play continues.");
        AdvanceToNextInterceptorOrCompletePass();
    }

    public List<HexCell> CalculateThickPath(HexCell startHex, HexCell endHex, float ballRadius)
    {
        return GroundPassCommon.CalculateThickPath(hexGrid, startHex, endHex, ballRadius);
    }

    public List<HexCell> GetCandidateGroundPathHexes(HexCell startHex, HexCell endHex, float ballRadius)
    {
        return GroundPassCommon.GetCandidateGroundPathHexes(hexGrid, startHex, endHex);
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
        MatchManager matchManager = MatchManager.Instance;
        if (isActivated)
        {
            sb.Append("SP: ");
        }
        if (isAwaitingTargetSelection)
        {
            if (matchManager == null)
            {
                return sb.ToString();
            }

            if (matchManager.difficulty_level == 3)
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
                    else if (currentTargetHex != null)
                    {
                        sb.Append($"{GetEasyModeCommittedTargetInstruction(diceRollsPending, previewHasConditionalGoalkeeperInteraction)} ");
                    }
                    else
                    {
                        sb.Append($"Hover a target within {imposedDistance} hexes to preview the pass. ");
                    }
            }
            else
                {
                    if (!string.IsNullOrWhiteSpace(latestValidationInstruction))
                    {
                        sb.Append($"{latestValidationInstruction} ");
                    }
                    else if (currentTargetHex != null)
                    {
                        sb.Append("Click the orange target again to confirm, or choose another valid target. ");
                    }
                    else
                    {
                        sb.Append($"Hover a target within {imposedDistance} hexes, then click it to select. ");
                    }
                }
            }
        }
        if (isWaitingForDiceRoll)
        {
            if (currentPathInteraction != null && currentPathInteraction.Type == GroundBallPathInteractionType.GoalkeeperWallSave)
            {
                PlayerToken gkToken = currentPathInteraction.DefenderToken;
                int neededRoll = Mathf.Max(1, 10 - gkToken.saving - currentPathInteraction.GkPenalty);
                string penaltyText = currentPathInteraction.GkPenalty == 0
                    ? ""
                    : $", dive penalty {currentPathInteraction.GkPenalty}";
                sb.Append($"Press [R] to roll a GK dive with {gkToken.name}: needs {neededRoll}+ with Saving {gkToken.saving}{penaltyText}, or natural 6/Jackpot, ");
            }
            else
            {
                string rollneeded = currentDefenderHex.GetOccupyingToken().tackling <= 4 ? "6" : currentDefenderHex.GetOccupyingToken().tackling == 6 ? "4+": "5+";
                sb.Append($"Press [R] to roll for interception with {currentDefenderHex.GetOccupyingToken().name}, a roll of {rollneeded} is needed, ");
            }
        }

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
        return isWaitingForDiceRoll ? !attackingTeamIsHome : attackingTeamIsHome;
    }
}
