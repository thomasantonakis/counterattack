using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class FirstTimePassManager : MonoBehaviour
{
    private const int FtpMaxDistance = 6;

    [Header("Dependencies")]
    public Ball ball;
    public HexGrid hexGrid;
    public MovementPhaseManager movementPhaseManager;
    public GoalKeeperManager goalKeeperManager;
    public FinalThirdManager finalThirdManager;
    public HelperFunctions helperFunctions;

    [Header("Runtime Items")]
    public bool isAvailable = false;
    public bool isActivated = false;
    public bool isAwaitingTargetSelection = false;
    public bool isWaitingForAttackerSelection = false;
    public bool isWaitingForAttackerMove = false;
    public bool isWaitingForDefenderSelection = false;
    public bool isWaitingForDefenderMove = false;
    public bool isWaitingForDiceRoll = false;

    [Header("Others")]
    public HexCell currentTargetHex = null;
    public PlayerToken selectedToken;

    private HexCell currentDefenderHex = null;
    private HexCell hoveredPreviewHex = null;
    private HexCell hoveredMovementHex = null;
    private string latestValidationInstruction = string.Empty;
    private bool currentTargetPreviewIsDangerous = false;
    private bool currentTargetPreviewHasConditionalGoalkeeperInteraction = false;
    private int currentTargetPreviewAttempts = 0;
    private List<GroundInterceptionCandidate> interceptionCandidates = new List<GroundInterceptionCandidate>();
    private readonly List<GroundBallPathInteraction> pathInteractions = new();
    private readonly HashSet<PlayerToken> attemptedOutfieldInterceptors = new();
    private GroundBallPathInteraction currentPathInteraction = null;
    private bool currentFtpGkWallAttempted = false;
    private bool currentFtpGkBoxMoveHandled = false;

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
        if (IsFinalThirdRunning() || !isActivated)
        {
            return;
        }

        if (isAwaitingTargetSelection)
        {
            HandleFTPBallPath(hex);
            return;
        }

        if (isWaitingForAttackerSelection)
        {
            HandleAttackerSelectionClick(token, hex);
            return;
        }

        if (isWaitingForDefenderSelection)
        {
            HandleDefenderSelectionClick(token, hex);
        }
    }

    private void OnHoverReceived(PlayerToken token, HexCell hex)
    {
        if (IsFinalThirdRunning() || !isActivated || MatchManager.Instance == null)
        {
            return;
        }

        int difficulty = MatchManager.Instance.difficulty_level;
        if (isAwaitingTargetSelection && (difficulty == 1 || difficulty == 2))
        {
            if (hoveredPreviewHex == hex)
            {
                return;
            }

            hoveredPreviewHex = hex;
            if (difficulty == 1)
            {
                UpdateEasyModeHoverPreview(hex);
            }
            else
            {
                UpdateMediumModeHoverPreview(hex);
            }

            return;
        }

        if (difficulty == 2
            && selectedToken != null
            && (isWaitingForAttackerMove || isWaitingForDefenderMove)
            && hoveredMovementHex != hex)
        {
            hoveredMovementHex = hex;
            UpdateMediumModeMovementHover(hex);
        }
    }

    private void OnKeyReceived(KeyPressData keyData)
    {
        if (keyData.isConsumed)
        {
            return;
        }

        if (IsFinalThirdRunning())
        {
            return;
        }

        if (isAvailable && !isActivated && keyData.key == KeyCode.F)
        {
            keyData.Consume(nameof(FirstTimePassManager));
            MatchManager.Instance.TriggerFTP();
            return;
        }

        if (!isActivated)
        {
            return;
        }

        bool hasRollOverride = RollInputOverride.TryParse(keyData, out RollInputOverride rollOverride);
        if (isWaitingForDiceRoll && (keyData.key == KeyCode.R || hasRollOverride))
        {
            keyData.Consume(nameof(FirstTimePassManager));
            PerformFTPInterceptionRolls(hasRollOverride ? rollOverride : null);
            return;
        }

        if (keyData.key != KeyCode.X)
        {
            return;
        }

        if (isWaitingForAttackerSelection)
        {
            keyData.Consume(nameof(FirstTimePassManager));
            SkipAttackerMovementPhase();
            return;
        }

        if (isWaitingForDefenderSelection)
        {
            keyData.Consume(nameof(FirstTimePassManager));
            SkipDefenderMovementPhase();
        }
    }

    public void ActivateFTP()
    {
        ball.SelectBall();
        Debug.Log("First Time pass attempt mode activated.");
        isActivated = true;
        isAvailable = false;
        isAwaitingTargetSelection = true;
        hoveredPreviewHex = null;
        hoveredMovementHex = null;
        latestValidationInstruction = string.Empty;
        ResetTargetPreviewState();
        ResetFTPInterceptionDiceRolls();

        if (MatchManager.Instance.difficulty_level == 3)
        {
            MatchManager.Instance.CommitToAction(MatchManager.MatchActionKind.FirstTimePass);
        }

        Debug.Log("FirstTimePassManager activated. Waiting for target selection...");
    }

    public void HandleFTPBallPath(HexCell clickedHex)
    {
        if (clickedHex == null)
        {
            return;
        }

        if (ball.GetCurrentHex() == null)
        {
            Debug.LogError("Ball's current hex is null! Ensure the ball has been placed on the grid.");
            return;
        }

        HandleFTPBasedOnDifficulty(clickedHex);
    }

    private void HandleFTPBasedOnDifficulty(HexCell clickedHex)
    {
        int difficulty = MatchManager.Instance.difficulty_level;
        GroundPassValidationResult validation = ValidateFTPTargetPath(clickedHex);
        List<GroundBallPathInteraction> previewInteractions = validation.IsValid
            ? BuildOrderedPathInteractions(clickedHex)
            : new List<GroundBallPathInteraction>();
        int previewAttempts = validation.IsValid
            ? GroundPassCommon.CountDangerousInteractions(previewInteractions)
            : 0;
        bool hasConditionalGoalkeeperInteraction = GroundPassCommon.HasConditionalGoalkeeperInteractionAfterBoxMove(previewInteractions);

        if (!validation.IsValid)
        {
            hexGrid.ClearHighlightedHexes();
            currentTargetHex = null;
            ResetTargetPreviewState();
            if (difficulty == 1 || difficulty == 3)
            {
                latestValidationInstruction = GroundPassCommon.GetValidationFailureInstruction(validation.FailureReason);
            }
            else
            {
                latestValidationInstruction = string.Empty;
            }

            return;
        }

        latestValidationInstruction = string.Empty;

        if (difficulty == 3)
        {
            currentTargetHex = clickedHex;
            currentTargetPreviewIsDangerous = previewAttempts > 0;
            currentTargetPreviewHasConditionalGoalkeeperInteraction = hasConditionalGoalkeeperInteraction;
            currentTargetPreviewAttempts = previewAttempts;
            ConfirmTargetSelection();
            return;
        }

        hexGrid.ClearHighlightedHexes();

        if (difficulty == 2)
        {
            if (currentTargetHex == null || clickedHex != currentTargetHex)
            {
                currentTargetHex = clickedHex;
                ResetTargetPreviewState();
                HighlightMediumModeTargets(clickedHex);
                string offsideWarning = GetFtpOffsideWarning(clickedHex);
                latestValidationInstruction = string.IsNullOrWhiteSpace(offsideWarning)
                    ? "Click the orange target again to confirm, or choose another valid target."
                    : $"{offsideWarning} Click the orange target again to confirm, or choose another valid target.";
                Debug.Log("First-Time Pass target selected. Click again to confirm or elsewhere to try another target.");
            }
            else
            {
                ConfirmTargetSelection();
            }

            return;
        }

        if (currentTargetHex == null || clickedHex != currentTargetHex)
        {
            currentTargetHex = clickedHex;
            currentTargetPreviewIsDangerous = previewAttempts > 0;
            currentTargetPreviewHasConditionalGoalkeeperInteraction = hasConditionalGoalkeeperInteraction;
            currentTargetPreviewAttempts = previewAttempts;
            hoveredPreviewHex = null;
            RenderEasyModeSelectedTargetPreview(validation);
        }
        else
        {
            ConfirmTargetSelection();
        }
    }

    private GroundPassValidationResult ValidateFTPTargetPath(HexCell targetHex)
    {
        return GroundPassCommon.ValidateStandardPassPath(hexGrid, ball, targetHex, FtpMaxDistance);
    }

    private string GetFtpOffsideWarning(HexCell targetHex)
    {
        MatchManager matchManager = MatchManager.Instance;
        return matchManager != null && matchManager.offsideManager != null
            ? matchManager.offsideManager.GetFtpOffsideWarning(targetHex)
            : string.Empty;
    }

    private bool IsDifficultyOneOffsideTokenOnTarget()
    {
        MatchManager matchManager = MatchManager.Instance;
        if (matchManager == null || matchManager.difficulty_level != 1 || currentTargetHex == null)
        {
            return false;
        }

        PlayerToken targetToken = currentTargetHex.GetOccupyingToken();
        return targetToken != null
            && matchManager.offsideManager != null
            && matchManager.offsideManager.IsTokenOffside(targetToken);
    }

    private bool IsDifficultyOneOffsideMoveOntoTarget(PlayerToken movingToken, HexCell destinationHex)
    {
        MatchManager matchManager = MatchManager.Instance;
        return matchManager != null
            && matchManager.difficulty_level == 1
            && movingToken != null
            && destinationHex != null
            && destinationHex == currentTargetHex
            && matchManager.offsideManager != null
            && matchManager.offsideManager.IsTokenOffside(movingToken);
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
                : $"Hover a target within {FtpMaxDistance} hexes, then click it to select.";
            return;
        }

        GroundPassValidationResult validation = ValidateFTPTargetPath(hoveredHex);
        hexGrid.ClearHighlightedHexes();
        HighlightCommittedTarget();

        if (!validation.IsValid)
        {
            latestValidationInstruction = GroundPassCommon.GetValidationFailureInstruction(validation.FailureReason);
            return;
        }

        HighlightMediumModeTargets(hoveredHex);
        string offsideWarning = GetFtpOffsideWarning(hoveredHex);
        if (!string.IsNullOrWhiteSpace(offsideWarning))
        {
            latestValidationInstruction = $"{offsideWarning} Click to select this target.";
            return;
        }

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
                ? GetEasyModeCommittedTargetInstruction()
                : $"Hover a target within {FtpMaxDistance} hexes to preview the First-Time Pass.";
            return;
        }

        if (hoveredHex == currentTargetHex)
        {
            RenderEasyModeSelectedTargetPreview();
            return;
        }

        GroundPassValidationResult validation = ValidateFTPTargetPath(hoveredHex);
        hexGrid.ClearHighlightedHexes();
        HighlightCommittedTarget();

        if (!validation.IsValid)
        {
            latestValidationInstruction = GroundPassCommon.GetValidationFailureInstruction(validation.FailureReason);
            return;
        }

        List<GroundBallPathInteraction> previewInteractions = BuildOrderedPathInteractions(hoveredHex);
        int previewAttempts = GroundPassCommon.CountDangerousInteractions(previewInteractions);
        bool hasConditionalGoalkeeperInteraction = GroundPassCommon.HasConditionalGoalkeeperInteractionAfterBoxMove(previewInteractions);
        bool previewIsDangerous = previewAttempts > 0;
        HighlightHoverPreviewPath(validation.PathHexes, hoveredHex, previewIsDangerous);
        HighlightCommittedTarget();
        string offsideWarning = GetFtpOffsideWarning(hoveredHex);
        latestValidationInstruction = string.IsNullOrWhiteSpace(offsideWarning)
            ? GetEasyModePreviewInstruction(previewIsDangerous, previewAttempts, hasConditionalGoalkeeperInteraction)
            : $"{offsideWarning} {GetEasyModePreviewInstruction(previewIsDangerous, previewAttempts, hasConditionalGoalkeeperInteraction)}";
    }

    private void RenderEasyModeSelectedTargetPreview(GroundPassValidationResult? knownValidation = null)
    {
        if (currentTargetHex == null)
        {
            return;
        }

        GroundPassValidationResult validation = knownValidation ?? ValidateFTPTargetPath(currentTargetHex);
        hexGrid.ClearHighlightedHexes();

        if (!validation.IsValid)
        {
            HighlightCommittedTarget();
            latestValidationInstruction = GroundPassCommon.GetValidationFailureInstruction(validation.FailureReason);
            return;
        }

        List<GroundBallPathInteraction> previewInteractions = BuildOrderedPathInteractions(currentTargetHex);
        currentTargetPreviewAttempts = GroundPassCommon.CountDangerousInteractions(previewInteractions);
        currentTargetPreviewIsDangerous = currentTargetPreviewAttempts > 0;
        currentTargetPreviewHasConditionalGoalkeeperInteraction = GroundPassCommon.HasConditionalGoalkeeperInteractionAfterBoxMove(previewInteractions);
        HighlightHoverPreviewPath(validation.PathHexes, currentTargetHex, currentTargetPreviewIsDangerous);
        string offsideWarning = GetFtpOffsideWarning(currentTargetHex);
        latestValidationInstruction = string.IsNullOrWhiteSpace(offsideWarning)
            ? GetEasyModeCommittedTargetInstruction()
            : $"{offsideWarning} {GetEasyModeCommittedTargetInstruction()}";
    }

    private void ConfirmTargetSelection()
    {
        if (currentTargetHex == null)
        {
            Debug.LogError("Cannot confirm an FTP target because no target hex is selected.");
            return;
        }

        Debug.Log("First-Time Pass target confirmed. Waiting for FTP movement phases.");
        MatchManager.Instance.EnsureOffsideManager()?.EvaluateAndStore("first_time_pass_target_confirmed");

        if (MatchManager.Instance.difficulty_level != 3)
        {
            MatchManager.Instance.CommitToAction(MatchManager.MatchActionKind.FirstTimePass);
        }

        isAwaitingTargetSelection = false;
        hoveredPreviewHex = null;
        hoveredMovementHex = null;
        latestValidationInstruction = string.Empty;
        MatchManager.Instance.gameData.gameLog.LogEvent(
            MatchManager.Instance.LastTokenToTouchTheBallOnPurpose,
            MatchManager.ActionType.PassAttempt
        );
        ball.DeselectBall();
        StartAttackerMovementPhase();
    }

    private void HandleAttackerSelectionClick(PlayerToken token, HexCell hex)
    {
        int difficulty = MatchManager.Instance.difficulty_level;
        if (isWaitingForAttackerMove && IsValidFtpMovementDestination(hex))
        {
            Debug.Log("Valid Hex to move the Attacker.");
            StartCoroutine(MoveSelectedAttackerToHex(hex));
            return;
        }

        if (difficulty == 3 && selectedToken != null)
        {
            Debug.LogWarning($"Attacker {selectedToken.name} is already selected. Click a reachable hex or press [X] to skip.");
            return;
        }

        if (token == null || !token.isAttacker)
        {
            Debug.LogWarning("Invalid token or not an attacker clicked. Please click on an attacker or press [X] to skip.");
            hexGrid.ClearHighlightedHexes();
            RestoreConfirmedFtpMovementHighlights();
            selectedToken = null;
            hoveredMovementHex = null;
            isWaitingForAttackerMove = false;
            return;
        }

        if (selectedToken == null || selectedToken != token)
        {
            Debug.Log($"Attacker {token.name} selected.");
            selectedToken = token;
            hoveredMovementHex = null;
            hexGrid.ClearHighlightedHexes();
            if (difficulty == 1)
            {
                movementPhaseManager.HighlightValidMovementHexes(selectedToken, 1);
            }
            RestoreConfirmedFtpMovementHighlights();
            isWaitingForAttackerMove = true;
        }
        else
        {
            Debug.Log($"Attacker {token.name} already selected. Click a highlighted Hex to move, click another attacker to switch, or press [X] to skip.");
        }
    }

    private void HandleDefenderSelectionClick(PlayerToken token, HexCell hex)
    {
        int difficulty = MatchManager.Instance.difficulty_level;
        if (isWaitingForDefenderMove && IsValidFtpMovementDestination(hex))
        {
            Debug.Log("Valid Hex to move the Defender.");
            StartCoroutine(MoveSelectedDefenderToHex(hex));
            return;
        }

        if (difficulty == 3 && selectedToken != null)
        {
            Debug.LogWarning($"Defender {selectedToken.name} is already selected. Click a reachable hex or press [X] to skip.");
            return;
        }

        if (token == null || token.isAttacker)
        {
            Debug.LogWarning("Invalid token or not a defender clicked. Please click on a defender or press [X] to skip.");
            hexGrid.ClearHighlightedHexes();
            RestoreConfirmedFtpMovementHighlights();
            selectedToken = null;
            hoveredMovementHex = null;
            isWaitingForDefenderMove = false;
            return;
        }

        if (selectedToken == null || selectedToken != token)
        {
            Debug.Log($"Defender {token.name} selected.");
            selectedToken = token;
            hoveredMovementHex = null;
            hexGrid.ClearHighlightedHexes();
            if (difficulty == 1)
            {
                HighlightFtpDefenderMovementHexes();
            }
            RestoreConfirmedFtpMovementHighlights();
            isWaitingForDefenderMove = true;
        }
        else
        {
            Debug.Log($"Defender {token.name} already selected. Click a highlighted Hex to move, click another defender to switch, or press [X] to skip.");
        }
    }

    private bool IsValidFtpMovementDestination(HexCell hex)
    {
        return hex != null
            && selectedToken != null
            && GetValidFtpMovementDestinations(selectedToken).Contains(hex);
    }

    private List<HexCell> GetValidFtpMovementDestinations(PlayerToken token)
    {
        HexCell currentHex = token != null ? token.GetCurrentHex() : null;
        if (currentHex == null)
        {
            return new List<HexCell>();
        }

        return HexGridUtils.GetReachableHexes(hexGrid, currentHex, 1).Item1
            .Where(hex => hex != null && !hex.isAttackOccupied && !hex.isDefenseOccupied && !hex.isOutOfBounds)
            .ToList();
    }

    private void UpdateMediumModeMovementHover(HexCell hoveredHex)
    {
        hexGrid.ClearHighlightedHexes();
        RestoreConfirmedFtpMovementHighlights();

        if (hoveredHex == null || !IsValidFtpMovementDestination(hoveredHex))
        {
            return;
        }

        hoveredHex.HighlightHex("MovementDestinationHover");
        if (!hexGrid.highlightedHexes.Contains(hoveredHex))
        {
            hexGrid.highlightedHexes.Add(hoveredHex);
        }
    }

    private void HighlightFtpDefenderMovementHexes()
    {
        hexGrid.ClearHighlightedHexes();
        RestoreConfirmedFtpMovementHighlights();

        foreach (HexCell hex in GetValidFtpMovementDestinations(selectedToken))
        {
            string highlightReason = GetFtpDefenderDestinationHighlightReason(hex);
            hex.HighlightHex(highlightReason);
            if (!hexGrid.highlightedHexes.Contains(hex))
            {
                hexGrid.highlightedHexes.Add(hex);
            }
        }
    }

    private string GetFtpDefenderDestinationHighlightReason(HexCell destinationHex)
    {
        HexCell ballHex = ball.GetCurrentHex();
        if (ballHex == null || currentTargetHex == null || destinationHex == null)
        {
            return "PaceAvailable";
        }

        List<HexCell> pathHexes = GroundPassCommon.CalculateThickPath(hexGrid, ballHex, currentTargetHex, ball.ballRadius);
        if (pathHexes.Contains(destinationHex))
        {
            return "dangerousPass";
        }

        HashSet<HexCell> relevantInterceptionHexes = new HashSet<HexCell>(
            GroundPassCommon.GetRelevantInterceptionHexes(pathHexes, currentTargetHex)
        );
        bool influencesPath = destinationHex
            .GetNeighbors(hexGrid)
            .Any(hex => hex != null && relevantInterceptionHexes.Contains(hex));

        return influencesPath ? "ballPath" : "PaceAvailable";
    }

    public void HighlightValidFTPPath(List<HexCell> pathHexes, bool isDangerous)
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

            hex.HighlightHex(hex == currentTargetHex ? "passTarget" : isDangerous ? "dangerousPass" : "ballPath");
            if (!hexGrid.highlightedHexes.Contains(hex))
            {
                hexGrid.highlightedHexes.Add(hex);
            }
        }
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

    private void RestoreConfirmedFtpMovementHighlights(bool includeMediumPath = false, bool mediumPathIsDangerous = false)
    {
        if (currentTargetHex == null || MatchManager.Instance == null)
        {
            return;
        }

        int difficulty = MatchManager.Instance.difficulty_level;
        if (difficulty == 1)
        {
            HighlightCurrentFtpPath(CalculateCurrentFtpPathDanger());
            return;
        }

        if (difficulty >= 2)
        {
            if (includeMediumPath)
            {
                HighlightCurrentFtpPath(mediumPathIsDangerous);
            }
            else
            {
                HighlightCommittedTarget();
            }
        }
    }

    private void HighlightCurrentFtpPath(bool isDangerous)
    {
        HexCell ballHex = ball != null ? ball.GetCurrentHex() : null;
        if (ballHex == null || currentTargetHex == null)
        {
            HighlightCommittedTarget();
            return;
        }

        List<HexCell> pathHexes = GroundPassCommon.CalculateThickPath(hexGrid, ballHex, currentTargetHex, ball.ballRadius);
        HighlightHoverPreviewPath(pathHexes, currentTargetHex, isDangerous);
    }

    private bool CalculateCurrentFtpPathDanger()
    {
        if (currentTargetHex == null)
        {
            return false;
        }

        return GroundPassCommon.CountDangerousInteractions(BuildPostMovementPathInteractions()) > 0;
    }

    private void HighlightFtpBallMovementPath(HexCell targetHex, bool hadInterceptionAttempt)
    {
        HexCell ballHex = ball != null ? ball.GetCurrentHex() : null;
        if (ballHex == null || targetHex == null)
        {
            return;
        }

        hexGrid.ClearHighlightedHexes();
        bool showIntendedPath = MatchManager.Instance != null
            && MatchManager.Instance.difficulty_level >= 2
            && currentTargetHex != null;
        HexCell highlightedPathTarget = showIntendedPath ? currentTargetHex : targetHex;
        List<HexCell> pathHexes = GroundPassCommon.CalculateThickPath(hexGrid, ballHex, highlightedPathTarget, ball.ballRadius);
        foreach (HexCell hex in pathHexes)
        {
            if (hex == null)
            {
                continue;
            }

            if (hex == highlightedPathTarget)
            {
                hex.HighlightHex(hex == currentTargetHex ? "passTargetCommitted" : "passTarget");
            }
            else
            {
                hex.HighlightHex(hadInterceptionAttempt ? "dangerousPass" : "ballPath");
            }

            if (!hexGrid.highlightedHexes.Contains(hex))
            {
                hexGrid.highlightedHexes.Add(hex);
            }
        }

        if (MatchManager.Instance != null
            && MatchManager.Instance.difficulty_level >= 2
            && currentTargetHex != null)
        {
            HighlightCommittedTarget();
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

    private string GetEasyModePreviewInstruction(bool isDangerous, int interceptionAttempts, bool hasConditionalGoalkeeperInteraction)
    {
        if (!isDangerous || interceptionAttempts <= 0)
        {
            if (hasConditionalGoalkeeperInteraction)
            {
                return "FTP preview enters the box: GK free move comes first, so current GK dive/interception threats are conditional and recalculate after that move. Click to lock this target.";
            }

            return "Safe FTP preview before the 1-hex moves. Click to lock this target.";
        }

        return $"Dangerous FTP preview before the 1-hex moves. {interceptionAttempts} current interception attempt{(interceptionAttempts == 1 ? string.Empty : "s")}; the path will be recalculated after the 1-hex moves.";
    }

    private string GetEasyModeCommittedTargetInstruction()
    {
        if (!currentTargetPreviewIsDangerous || currentTargetPreviewAttempts <= 0)
        {
            if (currentTargetPreviewHasConditionalGoalkeeperInteraction)
            {
                return "Selected FTP target enters the box: GK free move comes first, so current GK dive/interception threats are conditional and recalculate after that move. Click the orange target again to confirm, or hover another hex to preview.";
            }

            return "Selected FTP target is currently safe. Click the orange target again to confirm, or hover another hex to preview. The path will be recalculated after the 1-hex moves.";
        }

        return $"Selected FTP target is currently dangerous with {currentTargetPreviewAttempts} current interception attempt{(currentTargetPreviewAttempts == 1 ? string.Empty : "s")}. Click the orange target again to confirm, or hover another hex to preview. The path will be recalculated after the 1-hex moves.";
    }

    private void StartAttackerMovementPhase()
    {
        hexGrid.ClearHighlightedHexes();
        RestoreConfirmedFtpMovementHighlights();
        hoveredMovementHex = null;
        isWaitingForAttackerSelection = true;
        isWaitingForAttackerMove = false;
        isWaitingForDefenderSelection = false;
        isWaitingForDefenderMove = false;
        selectedToken = null;
        MatchManager.Instance.currentState = MatchManager.GameState.FirstTimePassAttackerMovement;
        Debug.Log("Attacker movement phase started. Move one attacker 1 hex or press [X] to skip.");
    }

    public void StartDefenderMovementPhase()
    {
        hexGrid.ClearHighlightedHexes();
        RestoreConfirmedFtpMovementHighlights();
        hoveredMovementHex = null;
        isWaitingForAttackerSelection = false;
        isWaitingForAttackerMove = false;
        isWaitingForDefenderSelection = true;
        isWaitingForDefenderMove = false;
        selectedToken = null;
        MatchManager.Instance.currentState = MatchManager.GameState.FirstTimePassDefenderMovement;
        Debug.Log("Defender movement phase started. Move one defender 1 hex or press [X] to skip.");
    }

    private void SkipAttackerMovementPhase()
    {
        if (IsDifficultyOneOffsideTokenOnTarget())
        {
            PlayerToken targetToken = currentTargetHex.GetOccupyingToken();
            latestValidationInstruction = $"{targetToken.name} is in an offside position and must be moved away from the First-Time Pass target.";
            Debug.LogWarning(latestValidationInstruction);
            return;
        }

        Debug.Log("Attacker FTP movement skipped.");
        latestValidationInstruction = string.Empty;
        hexGrid.ClearHighlightedHexes();
        RestoreConfirmedFtpMovementHighlights();
        hoveredMovementHex = null;
        selectedToken = null;
        isWaitingForAttackerSelection = false;
        isWaitingForAttackerMove = false;
        StartDefenderMovementPhase();
    }

    private void SkipDefenderMovementPhase()
    {
        Debug.Log("Defender FTP movement skipped.");
        hexGrid.ClearHighlightedHexes();
        RestoreConfirmedFtpMovementHighlights();
        hoveredMovementHex = null;
        selectedToken = null;
        isWaitingForDefenderSelection = false;
        isWaitingForDefenderMove = false;
        CompleteDefenderMovementPhase();
    }

    public void CompleteDefenderMovementPhase()
    {
        interceptionCandidates = BuildPostMovementInterceptionCandidates();
        pathInteractions.Clear();
        pathInteractions.AddRange(BuildPostMovementPathInteractions());

        if (pathInteractions.Count > 0)
        {
            Debug.Log($"Ball path interactions after FTP movement phases: {pathInteractions.Count}.");
            RestoreConfirmedFtpMovementHighlights(includeMediumPath: true, mediumPathIsDangerous: true);
            StartFTPInterceptionDiceRollSequence();
        }
        else
        {
            Debug.Log("No interception chance after FTP movement phases. Moving ball to target hex.");
            StartCoroutine(MovePassNotIntercepted(currentTargetHex));
        }
    }

    private List<GroundInterceptionCandidate> BuildPostMovementInterceptionCandidates()
    {
        HexCell ballHex = ball.GetCurrentHex();
        if (ballHex == null || currentTargetHex == null)
        {
            Debug.LogError("Cannot recalculate FTP interception candidates because the ball hex or target hex is null.");
            return new List<GroundInterceptionCandidate>();
        }

        List<HexCell> pathHexes = GroundPassCommon.CalculateThickPath(hexGrid, ballHex, currentTargetHex, ball.ballRadius);
        HashSet<HexCell> blockingDefenderHexes = new HashSet<HexCell>(
            pathHexes.Where(hex => hex != null && hex.isDefenseOccupied)
        );

        List<GroundInterceptionCandidate> candidates = GroundPassCommon.BuildOrderedInterceptionCandidates(
            hexGrid,
            ball,
            currentTargetHex,
            blockingDefenderHexes: blockingDefenderHexes
        )
        .Where(candidate => candidate != null && !IsCurrentTargetDefender(candidate.DefenderToken, candidate.DefenderHex))
        .ToList();

        if (candidates.Count > 0)
        {
            Debug.Log(
                $"FTP interception order: {string.Join(", ", candidates.Select(candidate => $"{candidate.DefenderToken.name} (blocking: {candidate.IsBlockingPath}, at {candidate.ClosestInterceptionHex.coordinates})"))}"
            );
        }

        return candidates;
    }

    private List<GroundBallPathInteraction> BuildOrderedPathInteractions(
        HexCell targetHex,
        int minPathIndex = 0,
        bool includeGoalkeeperBoxMove = true,
        IEnumerable<HexCell> blockingDefenderHexes = null)
    {
        return GroundPassCommon.BuildOrderedBallPathInteractions(
            hexGrid,
            ball,
            goalKeeperManager,
            targetHex,
            blockingDefenderHexes: blockingDefenderHexes,
            excludeOutfieldDefenders: attemptedOutfieldInterceptors,
            includeGoalkeeperWall: !currentFtpGkWallAttempted,
            includeGoalkeeperBoxMove: includeGoalkeeperBoxMove && !currentFtpGkBoxMoveHandled,
            minPathIndex: minPathIndex
        );
    }

    private List<GroundBallPathInteraction> BuildPostMovementPathInteractions(int minPathIndex = 0, bool includeGoalkeeperBoxMove = true)
    {
        HexCell ballHex = ball.GetCurrentHex();
        if (ballHex == null || currentTargetHex == null)
        {
            Debug.LogError("Cannot recalculate FTP path interactions because the ball hex or target hex is null.");
            return new List<GroundBallPathInteraction>();
        }

        List<HexCell> pathHexes = GroundPassCommon.CalculateThickPath(hexGrid, ballHex, currentTargetHex, ball.ballRadius);
        HashSet<HexCell> blockingDefenderHexes = new HashSet<HexCell>(
            pathHexes.Where(hex => hex != null && hex.isDefenseOccupied)
        );

        return BuildOrderedPathInteractions(currentTargetHex, minPathIndex, includeGoalkeeperBoxMove, blockingDefenderHexes)
            .Where(interaction => interaction != null && !IsCurrentTargetDefender(interaction.DefenderToken, interaction.DefenderHex))
            .ToList();
    }

    private bool IsCurrentTargetDefender(PlayerToken token, HexCell defenderHex)
    {
        if (currentTargetHex == null || token == null || defenderHex == null || defenderHex != currentTargetHex)
        {
            return false;
        }

        return currentTargetHex.GetOccupyingToken() == token && !token.isAttacker;
    }

    private IEnumerator MoveSelectedAttackerToHex(HexCell hex)
    {
        if (IsDifficultyOneOffsideMoveOntoTarget(selectedToken, hex))
        {
            latestValidationInstruction = $"{selectedToken.name} is in an offside position and cannot move onto the First-Time Pass target.";
            Debug.LogWarning(latestValidationInstruction);
            yield break;
        }

        hexGrid.ClearHighlightedHexes();
        RestoreConfirmedFtpMovementHighlights();
        latestValidationInstruction = string.Empty;
        hoveredMovementHex = null;
        isWaitingForAttackerMove = false;
        isWaitingForAttackerSelection = false;
        Debug.Log($"Moving {selectedToken.name} to hex {hex.coordinates}");
        yield return StartCoroutine(movementPhaseManager.MoveTokenToHex(
            targetHex: hex,
            token: selectedToken,
            isCalledDuringMovement: false,
            shouldCountForDistance: true,
            shouldCarryBall: false
        ));
        movementPhaseManager.isActivated = false;
        selectedToken = null;
        StartDefenderMovementPhase();
    }

    private IEnumerator MoveSelectedDefenderToHex(HexCell hex)
    {
        hexGrid.ClearHighlightedHexes();
        RestoreConfirmedFtpMovementHighlights();
        hoveredMovementHex = null;
        isWaitingForDefenderMove = false;
        isWaitingForDefenderSelection = false;
        Debug.Log($"Moving {selectedToken.name} to hex {hex.coordinates}");
        yield return StartCoroutine(movementPhaseManager.MoveTokenToHex(
            targetHex: hex,
            token: selectedToken,
            isCalledDuringMovement: false,
            shouldCountForDistance: true,
            shouldCarryBall: false
        ));
        movementPhaseManager.isActivated = false;
        selectedToken = null;
        CompleteDefenderMovementPhase();
    }

    private IEnumerator MovePassNotIntercepted(HexCell hex)
    {
        if (hex == null)
        {
            Debug.LogError("Cannot move an FTP that was not intercepted because the target hex is null.");
            yield break;
        }

        HighlightFtpBallMovementPath(hex, attemptedOutfieldInterceptors.Count > 0 || currentFtpGkWallAttempted);
        yield return StartCoroutine(HandleGroundBallMovement(hex, allowGKBoxMove: false));
        PlayerToken receiver = hex.GetOccupyingToken();
        if (receiver != null && receiver.isAttacker)
        {
            if (MatchManager.Instance.TryHandleOffsideCollection(receiver, "first_time_pass", hex))
            {
                yield break;
            }

            MatchManager.Instance.gameData.gameLog.LogEvent(
                MatchManager.Instance.LastTokenToTouchTheBallOnPurpose,
                MatchManager.ActionType.PassCompleted
            );
            MatchManager.Instance.SetLastToken(receiver);
            MatchManager.Instance.ClearOffsideForLegalCollection(receiver, "first_time_pass_completed");
        }
        else if (receiver != null && !receiver.isAttacker)
        {
            ResolveFtpTargetDefenderRecovery(receiver, hex);
            yield break;
        }
        else
        {
            MatchManager.Instance.SetHangingPass("ground", MatchManager.Instance.LastTokenToTouchTheBallOnPurpose);
        }

        MatchManager.Instance.UpdatePossessionAfterPass(hex);
        MatchManager.Instance.ResolveActionBeforeFinalThird(
            MatchManager.MatchActionKind.FirstTimePass,
            () =>
            {
                finalThirdManager.TriggerFinalThirdPhase();
                MatchManager.Instance.BroadcastEndofFirstTimePass();
            });
        CleanUpFTP();
    }

    private void ResolveFtpTargetDefenderRecovery(PlayerToken defenderToken, HexCell defenderHex)
    {
        if (defenderToken == null || defenderHex == null)
        {
            return;
        }

        PlayerToken passer = MatchManager.Instance.LastTokenToTouchTheBallOnPurpose;
        Debug.Log($"{defenderToken.name} recovers the First-Time Pass on the intended target at {defenderHex.coordinates} without an interception roll.");
        MatchManager.Instance.gameData.gameLog.LogExpectedRecovery(
            defenderToken,
            1f,
            passer,
            "ftp_target");
        MatchManager.Instance.gameData.gameLog.LogEvent(defenderToken, MatchManager.ActionType.InterceptionAttempt);
        MatchManager.Instance.gameData.gameLog.LogEvent(
            defenderToken,
            MatchManager.ActionType.InterceptionSuccess,
            recoveryType: "ftp",
            connectedToken: passer);
        MatchManager.Instance.SetLastToken(defenderToken);
        MatchManager.Instance.ClearOffsideForLegalCollection(defenderToken, "first_time_pass_target_defender_recovery");
        MatchManager.Instance.ChangePossession();
        MatchManager.Instance.currentState = MatchManager.GameState.LooseBallPickedUp;
        MatchManager.Instance.UpdatePossessionAfterPass(defenderHex);
        MatchManager.Instance.ResolveActionBeforeFinalThird(
            MatchManager.MatchActionKind.FirstTimePass,
            () => MatchManager.Instance.BroadcastDefensiveRecoveryOutcome(defenderToken, defenderHex));
        CleanUpFTP();
    }

    public void CleanUpFTP()
    {
        hexGrid.ClearHighlightedHexes();
        isActivated = false;
        isAwaitingTargetSelection = false;
        isWaitingForAttackerSelection = false;
        isWaitingForDefenderSelection = false;
        isWaitingForAttackerMove = false;
        isWaitingForDefenderMove = false;
        isWaitingForDiceRoll = false;
        selectedToken = null;
        currentTargetHex = null;
        hoveredPreviewHex = null;
        latestValidationInstruction = string.Empty;
        ResetTargetPreviewState();
        ResetFTPInterceptionDiceRolls();
    }

    private void StartFTPInterceptionDiceRollSequence()
    {
        if (pathInteractions.Count == 0)
        {
            Debug.LogWarning("No FTP path interactions available.");
            return;
        }

        ProcessCurrentFtpPathInteraction();
    }

    private void ProcessCurrentFtpPathInteraction()
    {
        if (pathInteractions.Count == 0)
        {
            Debug.Log("FTP successful! No more path interactions to resolve.");
            currentDefenderHex = null;
            StartCoroutine(MovePassNotIntercepted(currentTargetHex));
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
                Debug.Log($"Selected FTP defender for interception: {currentPathInteraction.DefenderToken.name}. Press [R] to roll.");
                isWaitingForDiceRoll = true;
                break;
        }
    }

    private IEnumerator ResolveGoalkeeperBoxMoveInteraction(GroundBallPathInteraction interaction)
    {
        isWaitingForDiceRoll = false;
        pathInteractions.Remove(interaction);
        currentFtpGkBoxMoveHandled = true;

        if (interaction != null
            && goalKeeperManager.TryStartGKMoveForPenaltyBox(interaction.InteractionHex.isInPenaltyBox, interaction.InteractionHex, out _))
        {
            yield return StartCoroutine(goalKeeperManager.HandleGKFreeMove());
        }

        pathInteractions.Clear();
        pathInteractions.AddRange(BuildPostMovementPathInteractions(minPathIndex: 0, includeGoalkeeperBoxMove: false));
        ProcessCurrentFtpPathInteraction();
    }

    private IEnumerator ResolveGoalkeeperDirectPickup(GroundBallPathInteraction interaction)
    {
        if (interaction?.DefenderToken == null || interaction.InteractionHex == null)
        {
            pathInteractions.Remove(interaction);
            ProcessCurrentFtpPathInteraction();
            yield break;
        }

        Debug.Log($"{interaction.DefenderToken.name} recovers the FTP directly at {interaction.InteractionHex.coordinates}.");
        MatchManager.Instance.ClearOffsideForLegalCollection(interaction.DefenderToken, "first_time_pass_goalkeeper_direct_pickup");
        yield return StartCoroutine(goalKeeperManager.ResolveGoalkeeperSaveAndHold(
            interaction.DefenderToken,
            interaction.InteractionHex,
            "direct"));
        CleanUpFTP();
    }

    private IEnumerator ResolveGoalkeeperWallSaveRoll(GroundBallPathInteraction interaction, RollInputOverride? rollOverride)
    {
        isWaitingForDiceRoll = false;
        currentFtpGkWallAttempted = true;
        if (interaction?.DefenderToken == null || interaction.InteractionHex == null)
        {
            pathInteractions.Remove(interaction);
            ProcessCurrentFtpPathInteraction();
            yield break;
        }

        PlayerToken goalkeeper = interaction.DefenderToken;
        GameplayDiceRollResult saveRoll = MatchManager.Instance.ResolveGameplayDiceRoll(
            "ftp_gk_wall_save",
            rollOverride,
            actor: goalkeeper,
            relatedToken: MatchManager.Instance.LastTokenToTouchTheBallOnPurpose,
            sourceHex: interaction.InteractionHex,
            targetHex: currentTargetHex,
            details: new Dictionary<string, string>
            {
                ["passType"] = "first_time_pass",
                ["goalkeeperSaving"] = goalkeeper.saving.ToString(),
                ["gkPenalty"] = interaction.GkPenalty.ToString()
            });
        bool isJackpot = saveRoll.isJackpot;
        int diceRoll = saveRoll.roll;
        int savingTotal = diceRoll + goalkeeper.saving + interaction.GkPenalty;

        MatchManager.Instance.gameData.gameLog.LogEvent(goalkeeper, MatchManager.ActionType.SaveAttempt);
        Debug.Log(isJackpot
            ? $"{goalkeeper.name} rolls a Jackpot for the FTP GK Wall save at {interaction.InteractionHex.coordinates}."
            : $"{goalkeeper.name} rolls {diceRoll} + Saving {goalkeeper.saving} + Penalty {interaction.GkPenalty} = {savingTotal} for the FTP GK Wall save.");

        if (isJackpot || diceRoll == 6 || savingTotal >= 10)
        {
            Debug.Log($"{goalkeeper.name} catches the First-Time Pass from the GK Wall.");
            MatchManager.Instance.ClearOffsideForLegalCollection(goalkeeper, "first_time_pass_goalkeeper_wall_save");
            yield return StartCoroutine(goalKeeperManager.ResolveGoalkeeperSaveAndHold(
                goalkeeper,
                interaction.InteractionHex,
                "gkWall"));
            CleanUpFTP();
            yield break;
        }

        Debug.Log($"{goalkeeper.name} failed the FTP GK Wall save. Play continues.");
        pathInteractions.Remove(interaction);
        ProcessCurrentFtpPathInteraction();
    }

    private void PerformFTPInterceptionRolls(int? rigRoll = null)
    {
        RollInputOverride? rollOverride = rigRoll.HasValue
            ? new RollInputOverride
            {
                hasOverride = true,
                roll = rigRoll.Value,
                isJackpot = false
            }
            : null;
        PerformFTPInterceptionRolls(rollOverride);
    }

    private void PerformFTPInterceptionRolls(RollInputOverride? rollOverride)
    {
        if (currentPathInteraction != null && currentPathInteraction.Type == GroundBallPathInteractionType.GoalkeeperWallSave)
        {
            StartCoroutine(ResolveGoalkeeperWallSaveRoll(currentPathInteraction, rollOverride));
            return;
        }

        if (currentDefenderHex == null || currentPathInteraction == null)
        {
            Debug.LogError("Cannot roll FTP interception because no current defender hex is set.");
            return;
        }

        GroundInterceptionCandidate currentCandidate = currentPathInteraction.InterceptionCandidate;

        if (currentCandidate == null || currentCandidate.DefenderToken == null)
        {
            Debug.LogError("No matching defender found for FTP interception rolls.");
            return;
        }

        PlayerToken defenderToken = currentCandidate.DefenderToken;
        int tackling = defenderToken.tackling;
        GameplayDiceRollResult interceptionRoll = MatchManager.Instance.ResolveGameplayDiceRoll(
            "ftp_interception",
            rollOverride,
            actor: defenderToken,
            relatedToken: MatchManager.Instance.LastTokenToTouchTheBallOnPurpose,
            sourceHex: currentDefenderHex,
            targetHex: currentTargetHex,
            details: new Dictionary<string, string>
            {
                ["passType"] = "first_time_pass",
                ["defenderTackling"] = tackling.ToString(),
                ["isBlockingPath"] = currentCandidate.IsBlockingPath.ToString()
            });
        int diceRoll = interceptionRoll.roll;

        Debug.Log($"Dice roll by {defenderToken.name} at {currentDefenderHex.coordinates}: {diceRoll}");
        MatchManager.Instance.gameData.gameLog.LogExpectedRecovery(
            defenderToken,
            ExpectedStatsCalculator.CalculateRecoveryProbability(defenderToken, currentCandidate.IsBlockingPath ? 5 : 6),
            MatchManager.Instance.LastTokenToTouchTheBallOnPurpose,
            "ftp"
        );
        MatchManager.Instance.gameData.gameLog.LogEvent(defenderToken, MatchManager.ActionType.InterceptionAttempt);

        isWaitingForDiceRoll = false;
        bool successfulInterception = currentCandidate.IsBlockingPath
            ? diceRoll >= 5 || diceRoll + tackling >= 10
            : diceRoll == 6 || diceRoll + tackling >= 10;

        if (successfulInterception)
        {
            Debug.Log($"Pass intercepted by {defenderToken.name} at {currentDefenderHex.coordinates}!");
            MatchManager.Instance.gameData.gameLog.LogEvent(
                defenderToken,
                MatchManager.ActionType.InterceptionSuccess,
                recoveryType: "ftp",
                connectedToken: MatchManager.Instance.LastTokenToTouchTheBallOnPurpose
            );
            MatchManager.Instance.SetLastToken(defenderToken);
            MatchManager.Instance.ClearOffsideForLegalCollection(defenderToken, "first_time_pass_interception");
            HexCell interceptionHex = currentDefenderHex;
            ResetFTPInterceptionDiceRolls();
            StartCoroutine(HandleBallInterception(interceptionHex));
            return;
        }

        Debug.Log($"{defenderToken.name} at {currentDefenderHex.coordinates} failed to intercept.");
        attemptedOutfieldInterceptors.Add(defenderToken);
        pathInteractions.Remove(currentPathInteraction);

        if (pathInteractions.Count > 0)
        {
            ProcessCurrentFtpPathInteraction();
            return;
        }

        Debug.Log("FTP successful! No more defenders to roll.");
        currentDefenderHex = null;
        StartCoroutine(MovePassNotIntercepted(currentTargetHex));
    }

    private int GetRollValueWithoutJackpot(RollInputOverride? rollOverride, int returnedRoll)
    {
        if (!rollOverride.HasValue || !rollOverride.Value.hasOverride)
        {
            return returnedRoll;
        }

        return rollOverride.Value.isJackpot ? 6 : rollOverride.Value.roll;
    }

    private IEnumerator HandleBallInterception(HexCell defenderHex)
    {
        HighlightFtpBallMovementPath(defenderHex, hadInterceptionAttempt: true);
        yield return StartCoroutine(HandleGroundBallMovement(defenderHex, allowGKBoxMove: false));
        PlayerToken recoveringToken = defenderHex != null ? defenderHex.GetOccupyingToken() : null;
        MatchManager.Instance.ChangePossession();
        MatchManager.Instance.currentState = MatchManager.GameState.LooseBallPickedUp;
        MatchManager.Instance.UpdatePossessionAfterPass(defenderHex);
        MatchManager.Instance.ResolveActionBeforeFinalThird(
            MatchManager.MatchActionKind.FirstTimePass,
            () => MatchManager.Instance.BroadcastDefensiveRecoveryOutcome(recoveringToken, defenderHex));
        CleanUpFTP();
    }

    private void ResetFTPInterceptionDiceRolls()
    {
        interceptionCandidates.Clear();
        pathInteractions.Clear();
        attemptedOutfieldInterceptors.Clear();
        currentDefenderHex = null;
        currentPathInteraction = null;
        isWaitingForDiceRoll = false;
        currentFtpGkWallAttempted = false;
        currentFtpGkBoxMoveHandled = false;
    }

    public IEnumerator HandleGroundBallMovement(HexCell targetHex, bool allowGKBoxMove = true)
    {
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

        yield return StartCoroutine(ball.MoveToCell(targetHex, null, allowGKBoxMove));
        ball.AdjustBallHeightBasedOnOccupancy();
        hexGrid.ClearHighlightedHexes();
        Debug.Log("Highlights cleared after ball movement.");
    }

    public List<HexCell> CalculateThickPath(HexCell startHex, HexCell endHex, float ballRadius)
    {
        return GroundPassCommon.CalculateThickPath(hexGrid, startHex, endHex, ballRadius);
    }

    public string GetDebugStatus()
    {
        StringBuilder sb = new();
        sb.Append("FTP: ");

        if (isActivated) sb.Append("isActivated, ");
        if (isAvailable) sb.Append("isAvailable, ");
        if (isAwaitingTargetSelection) sb.Append("isAwaitingTargetSelection, ");
        if (isWaitingForAttackerSelection) sb.Append("isWaitingForAttackerSelection, ");
        if (isWaitingForAttackerMove) sb.Append("isWaitingForAttackerMove, ");
        if (isWaitingForDefenderSelection) sb.Append("isWaitingForDefenderSelection, ");
        if (isWaitingForDefenderMove) sb.Append("isWaitingForDefenderMove, ");
        if (isWaitingForDiceRoll) sb.Append("isWaitingForDiceRoll, ");
        if (currentTargetHex != null) sb.Append($"currentTargetHex: {currentTargetHex.name}, ");

        if (sb.Length >= 2 && sb[^2] == ',') sb.Length -= 2;
        return sb.ToString();
    }

    public void PopulateRoomDecisionContext(RoomDecisionContext context)
    {
        if (context == null)
        {
            return;
        }

        if (isAvailable && !isActivated)
        {
            context.AddKeyActionCandidate(
                nameof(FirstTimePassManager),
                RoomActionType.FirstTimePass,
                RoomDecisionStep.ChooseActionType,
                "F",
                "Press [F] to play a First-Time Pass");
        }

        if (isActivated && isWaitingForDiceRoll)
        {
            context.AddKeyActionCandidate(
                nameof(FirstTimePassManager),
                RoomActionType.Roll,
                RoomDecisionStep.Roll,
                "R",
                "Press [R] to roll for the first-time-pass interaction");
            return;
        }

        if (isActivated && (isWaitingForAttackerSelection || isWaitingForDefenderSelection))
        {
            context.AddKeyActionCandidate(
                nameof(FirstTimePassManager),
                RoomActionType.Decline,
                RoomDecisionStep.InterruptionChoice,
                "X",
                "Press [X] to skip the 1-hex move",
                isForfeit: true);
            PopulateFtpMovementDecisionContext(context);
            return;
        }

        if (!isActivated || !isAwaitingTargetSelection)
        {
            return;
        }

        if (currentTargetHex != null)
        {
            context.AddActionSummary("Click the selected orange target to confirm");
            context.AddHexActionCandidate(
                nameof(FirstTimePassManager),
                RoomActionType.FirstTimePass,
                RoomDecisionStep.Confirm,
                currentTargetHex,
                $"Confirm first-time pass to hex {currentTargetHex.coordinates}");
            PlayerToken selectedReceiver = currentTargetHex.GetOccupyingToken();
            if (selectedReceiver != null && selectedReceiver.isAttacker)
            {
                context.AddTargetTokenActionCandidate(
                    nameof(FirstTimePassManager),
                    RoomActionType.FirstTimePass,
                    RoomDecisionStep.Confirm,
                    selectedReceiver,
                    $"Confirm first-time pass to {FormatTokenName(selectedReceiver)}");
            }
        }

        context.AddActionSummary("Click a valid first-time-pass target");
        PopulateValidFirstTimePassTargets(context);
    }

    private void PopulateValidFirstTimePassTargets(RoomDecisionContext context)
    {
        if (context == null || hexGrid == null || hexGrid.cells == null || ball == null || ball.GetCurrentHex() == null)
        {
            return;
        }

        foreach (HexCell hex in HexGrid.GetHexesInRange(hexGrid, ball.GetCurrentHex(), FtpMaxDistance))
        {
            if (!IsPlausibleFirstTimePassDecisionTarget(hex))
            {
                continue;
            }

            GroundPassValidationResult validation = ValidateFirstTimePassTargetForDecision(hex);
            if (!validation.IsValid)
            {
                continue;
            }

            PlayerToken targetToken = hex.GetOccupyingToken();
            if (targetToken != null && targetToken.isAttacker)
            {
                context.AddTargetTokenActionCandidate(
                    nameof(FirstTimePassManager),
                    RoomActionType.FirstTimePass,
                    RoomDecisionStep.ChooseTarget,
                    targetToken,
                    $"First-time pass to {FormatTokenName(targetToken)}");
                continue;
            }

            context.AddHexActionCandidate(
                nameof(FirstTimePassManager),
                RoomActionType.FirstTimePass,
                RoomDecisionStep.ChooseTarget,
                hex,
                $"First-time pass to hex {hex.coordinates}");
        }
    }

    private void PopulateFtpMovementDecisionContext(RoomDecisionContext context)
    {
        if (context == null)
        {
            return;
        }

        if (isWaitingForAttackerSelection)
        {
            PopulateFtpSideMovementDecisionContext(
                context,
                isAttackerSide: true,
                selectionSummary: "Click an attacker with a legal 1-hex FTP move");
            return;
        }

        if (isWaitingForDefenderSelection)
        {
            PopulateFtpSideMovementDecisionContext(
                context,
                isAttackerSide: false,
                selectionSummary: "Click a defender with a legal 1-hex FTP move");
        }
    }

    private void PopulateFtpSideMovementDecisionContext(
        RoomDecisionContext context,
        bool isAttackerSide,
        string selectionSummary)
    {
        int difficulty = MatchManager.Instance != null ? MatchManager.Instance.difficulty_level : 0;
        if (selectedToken == null)
        {
            context.AddActionSummary(selectionSummary);
            AddFtpMovementTokenCandidates(context, isAttackerSide, excludedToken: null);
            return;
        }

        string tokenName = FormatTokenName(selectedToken);
        context.AddActionSummary($"Click a legal 1-hex destination for {tokenName}");
        foreach (HexCell destination in GetValidFtpMovementDestinations(selectedToken))
        {
            context.AddHexActionCandidate(
                nameof(FirstTimePassManager),
                RoomActionType.SetupMove,
                RoomDecisionStep.ChooseTarget,
                destination,
                $"Move {tokenName} to hex {destination.coordinates}");
        }

        if (difficulty != 3)
        {
            context.AddActionSummary($"Click another {(isAttackerSide ? "attacker" : "defender")} to switch the FTP move");
            AddFtpMovementTokenCandidates(context, isAttackerSide, selectedToken);
        }
    }

    private void AddFtpMovementTokenCandidates(RoomDecisionContext context, bool isAttackerSide, PlayerToken excludedToken)
    {
        IEnumerable<HexCell> tokenHexes = isAttackerSide
            ? hexGrid != null ? hexGrid.GetAttackerHexes() : Enumerable.Empty<HexCell>()
            : hexGrid != null ? hexGrid.GetDefenderHexes() : Enumerable.Empty<HexCell>();

        foreach (HexCell tokenHex in tokenHexes)
        {
            PlayerToken token = tokenHex != null ? tokenHex.GetOccupyingToken() : null;
            if (token == null || token == excludedToken || token.isAttacker != isAttackerSide)
            {
                continue;
            }

            if (GetValidFtpMovementDestinations(token).Count > 0)
            {
                context.AddTokenActionCandidate(
                    nameof(FirstTimePassManager),
                    RoomActionType.SetupMove,
                    RoomDecisionStep.ChooseActor,
                    token,
                    $"Select {FormatTokenName(token)} for the 1-hex FTP move");
            }
        }
    }

    private static string FormatTokenName(PlayerToken token)
    {
        if (token == null)
        {
            return "the selected player";
        }

        return !string.IsNullOrWhiteSpace(token.playerName) ? token.playerName : token.name;
    }

    private bool IsPlausibleFirstTimePassDecisionTarget(HexCell hex)
    {
        if (hex == null || hex.isOutOfBounds || hex == ball.GetCurrentHex())
        {
            return false;
        }

        PlayerToken targetToken = hex.GetOccupyingToken();
        return targetToken == null || targetToken.isAttacker;
    }

    private GroundPassValidationResult ValidateFirstTimePassTargetForDecision(HexCell targetHex)
    {
        return GroundPassCommon.ValidateStandardPassPath(
            hexGrid,
            ball,
            targetHex,
            FtpMaxDistance,
            logFailures: false);
    }

    public string GetInstructions()
    {
        StringBuilder sb = new();
        MatchManager matchManager = MatchManager.Instance;
        if (goalKeeperManager.isActivated || finalThirdManager.isActivated)
        {
            return string.Empty;
        }

        if (isAvailable)
        {
            sb.Append("Press [F] to Play a First-Time Pass, ");
        }

        if (isActivated)
        {
            sb.Append("FTP: ");
        }

        if (isAwaitingTargetSelection)
        {
            int difficulty = matchManager != null ? matchManager.difficulty_level : 0;
            if (difficulty == 1)
            {
                if (!string.IsNullOrWhiteSpace(latestValidationInstruction))
                {
                    sb.Append($"{latestValidationInstruction} ");
                }
                else
                {
                    sb.Append($"Hover a target within {FtpMaxDistance} hexes to preview the First-Time Pass. ");
                }
            }
            else if (difficulty == 2)
            {
                if (!string.IsNullOrWhiteSpace(latestValidationInstruction))
                {
                    sb.Append($"{latestValidationInstruction} ");
                }
                else if (currentTargetHex == null)
                {
                    sb.Append($"Hover a target within {FtpMaxDistance} hexes, then click it to select, ");
                }
                else
                {
                    sb.Append("Click the orange target again to confirm the First-Time Pass, ");
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(latestValidationInstruction))
                {
                    sb.Append($"{latestValidationInstruction} ");
                }
                else
                {
                    sb.Append($"Click on a valid target within {FtpMaxDistance} hexes to lock the First-Time Pass target, ");
                }
            }
        }

        if (isWaitingForAttackerSelection)
        {
            if (!string.IsNullOrWhiteSpace(latestValidationInstruction))
            {
                sb.Append($"{latestValidationInstruction} ");
            }

            int difficulty = matchManager != null ? matchManager.difficulty_level : 0;
            if (selectedToken == null)
            {
                sb.Append(difficulty == 3
                    ? "Click on an Attacker to select them for a 1-hex move, or press [X] to skip, "
                    : difficulty == 2
                        ? "Click on an Attacker for a 1-hex move, or press [X] to skip, "
                        : "Click on an Attacker to show a 1-hex move, or press [X] to skip, ");
            }
            else
            {
                sb.Append(difficulty == 3
                    ? $"Click a reachable Hex to move {selectedToken.name}, or press [X] to skip, "
                    : difficulty == 2
                        ? $"Hover a valid destination to preview it orange, click a valid Hex to move {selectedToken.name}, click another attacker to switch player, or press [X] to skip, "
                        : $"Click on a highlighted Hex to move {selectedToken.name}, click another attacker to switch player, or press [X] to skip, ");
            }
        }

        if (isWaitingForDefenderSelection)
        {
            int difficulty = matchManager != null ? matchManager.difficulty_level : 0;
            if (selectedToken == null)
            {
                sb.Append(difficulty == 3
                    ? "Click on a Defender to select them for a 1-hex move, or press [X] to skip, "
                    : difficulty == 2
                        ? "Click on a Defender for a 1-hex move, or press [X] to skip, "
                        : "Click on a Defender to show a 1-hex move, or press [X] to skip, ");
            }
            else
            {
                sb.Append(difficulty == 3
                    ? $"Click a reachable Hex to move {selectedToken.name}, or press [X] to skip, "
                    : difficulty == 2
                        ? $"Hover a valid destination to preview it orange, click a valid Hex to move {selectedToken.name}, click another defender to switch player, or press [X] to skip, "
                        : $"Click on a highlighted Hex to move {selectedToken.name}; yellow gives no interception, blue gives a mild chance, purple gives an increased chance; click another defender to switch player, or press [X] to skip, ");
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
                PlayerToken currentDefender = currentDefenderHex != null ? currentDefenderHex.GetOccupyingToken() : null;
                if (currentDefender != null)
                {
                    sb.Append($"Press [R] to roll for interception with {currentDefender.name}, a roll of {GetCurrentInterceptionRollDescription()} is needed, ");
                }
            }
        }

        if (sb.Length >= 2 && sb[^2] == ',') sb.Length -= 2;
        return sb.ToString();
    }

    public bool? IsInstructionExpectingHomeTeam()
    {
        if (MatchManager.Instance == null || IsFinalThirdRunning() || (!isActivated && !isAvailable))
        {
            return null;
        }

        bool attackingTeamIsHome = MatchManager.Instance.teamInAttack == MatchManager.TeamInAttack.Home;
        if (!isActivated)
        {
            return attackingTeamIsHome;
        }

        if (isWaitingForDefenderSelection || isWaitingForDefenderMove || isWaitingForDiceRoll)
        {
            return !attackingTeamIsHome;
        }

        return attackingTeamIsHome;
    }

    private string GetCurrentInterceptionRollDescription()
    {
        GroundInterceptionCandidate currentCandidate = interceptionCandidates
            .FirstOrDefault(candidate => candidate.DefenderHex == currentDefenderHex);

        if (currentCandidate == null || currentCandidate.DefenderToken == null)
        {
            return "6";
        }

        if (currentCandidate.IsBlockingPath)
        {
            return currentCandidate.DefenderToken.tackling >= 6 ? "4+" : "5+";
        }

        return currentCandidate.DefenderToken.tackling switch
        {
            <= 4 => "6",
            6 => "4+",
            _ => "5+",
        };
    }

    private void ResetTargetPreviewState()
    {
        currentTargetPreviewIsDangerous = false;
        currentTargetPreviewHasConditionalGoalkeeperInteraction = false;
        currentTargetPreviewAttempts = 0;
    }

    private bool IsFinalThirdRunning()
    {
        return finalThirdManager != null && finalThirdManager.isActivated;
    }
}
