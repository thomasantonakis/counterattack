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
    private string latestValidationInstruction = string.Empty;
    private bool currentTargetPreviewIsDangerous = false;
    private int currentTargetPreviewAttempts = 0;
    private List<GroundInterceptionCandidate> interceptionCandidates = new List<GroundInterceptionCandidate>();

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
        if (!isActivated)
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
        if (!isActivated || !isAwaitingTargetSelection || MatchManager.Instance.difficulty_level != 1)
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
        if (keyData.isConsumed)
        {
            return;
        }

        if (isAvailable && !isActivated && keyData.key == KeyCode.F)
        {
            keyData.isConsumed = true;
            MatchManager.Instance.TriggerFTP();
            return;
        }

        if (!isActivated)
        {
            return;
        }

        if (isWaitingForDiceRoll && keyData.key == KeyCode.R)
        {
            keyData.isConsumed = true;
            PerformFTPInterceptionRolls();
            return;
        }

        if (keyData.key != KeyCode.X)
        {
            return;
        }

        if (isWaitingForAttackerSelection)
        {
            keyData.isConsumed = true;
            SkipAttackerMovementPhase();
            return;
        }

        if (isWaitingForDefenderSelection)
        {
            keyData.isConsumed = true;
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
        latestValidationInstruction = string.Empty;
        ResetTargetPreviewState();
        ResetFTPInterceptionDiceRolls();

        if (MatchManager.Instance.difficulty_level == 3)
        {
            MatchManager.Instance.CommitToAction();
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
        int previewAttempts = validation.IsValid
            ? GroundPassCommon.BuildOrderedInterceptionCandidates(hexGrid, ball, clickedHex).Count
            : 0;

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
            currentTargetPreviewIsDangerous = validation.IsDangerous;
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
                currentTargetPreviewIsDangerous = validation.IsDangerous;
                currentTargetPreviewAttempts = previewAttempts;
                HighlightValidFTPPath(validation.PathHexes, validation.IsDangerous);
                Debug.Log($"First click registered. Click again to confirm the First-Time Pass. Current path is {(validation.IsDangerous ? "dangerous" : "safe")} before the 1-hex moves.");
            }
            else
            {
                HighlightValidFTPPath(validation.PathHexes, validation.IsDangerous);
                ConfirmTargetSelection();
            }

            return;
        }

        if (currentTargetHex == null || clickedHex != currentTargetHex)
        {
            currentTargetHex = clickedHex;
            currentTargetPreviewIsDangerous = validation.IsDangerous;
            currentTargetPreviewAttempts = previewAttempts;
            hoveredPreviewHex = null;
            hexGrid.ClearHighlightedHexes();
            HighlightCommittedTarget();
            latestValidationInstruction = GetEasyModeCommittedTargetInstruction();
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

        GroundPassValidationResult validation = ValidateFTPTargetPath(hoveredHex);
        hexGrid.ClearHighlightedHexes();
        HighlightCommittedTarget();

        if (!validation.IsValid)
        {
            latestValidationInstruction = GroundPassCommon.GetValidationFailureInstruction(validation.FailureReason);
            return;
        }

        int previewAttempts = GroundPassCommon.BuildOrderedInterceptionCandidates(hexGrid, ball, hoveredHex).Count;
        HighlightHoverPreviewPath(validation.PathHexes, hoveredHex, validation.IsDangerous);
        HighlightCommittedTarget();
        latestValidationInstruction = GetEasyModePreviewInstruction(validation.IsDangerous, previewAttempts);
    }

    private void ConfirmTargetSelection()
    {
        if (currentTargetHex == null)
        {
            Debug.LogError("Cannot confirm an FTP target because no target hex is selected.");
            return;
        }

        Debug.Log("First-Time Pass target confirmed. Waiting for FTP movement phases.");

        if (MatchManager.Instance.difficulty_level != 3)
        {
            MatchManager.Instance.CommitToAction();
        }

        isAwaitingTargetSelection = false;
        hoveredPreviewHex = null;
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
        if (isWaitingForAttackerMove && hex != null && hexGrid.highlightedHexes.Contains(hex))
        {
            Debug.Log("Valid Hex to move the Attacker.");
            StartCoroutine(MoveSelectedAttackerToHex(hex));
            return;
        }

        if (token == null || !token.isAttacker)
        {
            Debug.LogWarning("Invalid token or not an attacker clicked. Please click on an attacker or press [X] to skip.");
            hexGrid.ClearHighlightedHexes();
            selectedToken = null;
            isWaitingForAttackerMove = false;
            return;
        }

        if (selectedToken == null || selectedToken != token)
        {
            Debug.Log($"Attacker {token.name} selected.");
            selectedToken = token;
            hexGrid.ClearHighlightedHexes();
            movementPhaseManager.HighlightValidMovementHexes(selectedToken, 1);
            isWaitingForAttackerMove = true;
        }
        else
        {
            Debug.Log($"Attacker {token.name} already selected. Click a highlighted Hex to move, click another attacker to switch, or press [X] to skip.");
        }
    }

    private void HandleDefenderSelectionClick(PlayerToken token, HexCell hex)
    {
        if (isWaitingForDefenderMove && hex != null && hexGrid.highlightedHexes.Contains(hex))
        {
            Debug.Log("Valid Hex to move the Defender.");
            StartCoroutine(MoveSelectedDefenderToHex(hex));
            return;
        }

        if (token == null || token.isAttacker)
        {
            Debug.LogWarning("Invalid token or not a defender clicked. Please click on a defender or press [X] to skip.");
            hexGrid.ClearHighlightedHexes();
            selectedToken = null;
            isWaitingForDefenderMove = false;
            return;
        }

        if (selectedToken == null || selectedToken != token)
        {
            Debug.Log($"Defender {token.name} selected.");
            selectedToken = token;
            hexGrid.ClearHighlightedHexes();
            movementPhaseManager.HighlightValidMovementHexes(selectedToken, 1);
            isWaitingForDefenderMove = true;
        }
        else
        {
            Debug.Log($"Defender {token.name} already selected. Click a highlighted Hex to move, click another defender to switch, or press [X] to skip.");
        }
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

    private string GetEasyModePreviewInstruction(bool isDangerous, int interceptionAttempts)
    {
        if (!isDangerous || interceptionAttempts <= 0)
        {
            return "Safe FTP preview before the 1-hex moves. Click to lock this target.";
        }

        return $"Dangerous FTP preview before the 1-hex moves. {interceptionAttempts} current interception attempt{(interceptionAttempts == 1 ? string.Empty : "s")}; the path will be recalculated after the 1-hex moves.";
    }

    private string GetEasyModeCommittedTargetInstruction()
    {
        if (!currentTargetPreviewIsDangerous || currentTargetPreviewAttempts <= 0)
        {
            return "Selected FTP target is currently safe. Click the orange target again to confirm, or hover another hex to preview. The path will be recalculated after the 1-hex moves.";
        }

        return $"Selected FTP target is currently dangerous with {currentTargetPreviewAttempts} current interception attempt{(currentTargetPreviewAttempts == 1 ? string.Empty : "s")}. Click the orange target again to confirm, or hover another hex to preview. The path will be recalculated after the 1-hex moves.";
    }

    private void StartAttackerMovementPhase()
    {
        hexGrid.ClearHighlightedHexes();
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
        Debug.Log("Attacker FTP movement skipped.");
        hexGrid.ClearHighlightedHexes();
        selectedToken = null;
        isWaitingForAttackerSelection = false;
        isWaitingForAttackerMove = false;
        StartDefenderMovementPhase();
    }

    private void SkipDefenderMovementPhase()
    {
        Debug.Log("Defender FTP movement skipped.");
        hexGrid.ClearHighlightedHexes();
        selectedToken = null;
        isWaitingForDefenderSelection = false;
        isWaitingForDefenderMove = false;
        CompleteDefenderMovementPhase();
    }

    public void CompleteDefenderMovementPhase()
    {
        interceptionCandidates = BuildPostMovementInterceptionCandidates();

        if (interceptionCandidates.Count > 0)
        {
            Debug.Log($"Interception chance after FTP movement phases: {interceptionCandidates.Count} defenders can intercept the pass.");
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
        );

        if (candidates.Count > 0)
        {
            Debug.Log(
                $"FTP interception order: {string.Join(", ", candidates.Select(candidate => $"{candidate.DefenderToken.name} (blocking: {candidate.IsBlockingPath}, at {candidate.ClosestInterceptionHex.coordinates})"))}"
            );
        }

        return candidates;
    }

    private IEnumerator MoveSelectedAttackerToHex(HexCell hex)
    {
        hexGrid.ClearHighlightedHexes();
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

        if (hex.isAttackOccupied)
        {
            MatchManager.Instance.gameData.gameLog.LogEvent(
                MatchManager.Instance.LastTokenToTouchTheBallOnPurpose,
                MatchManager.ActionType.PassCompleted
            );
            MatchManager.Instance.SetLastToken(hex.GetOccupyingToken());
        }
        else
        {
            MatchManager.Instance.SetHangingPass("ground", MatchManager.Instance.LastTokenToTouchTheBallOnPurpose);
        }

        yield return StartCoroutine(HandleGroundBallMovement(hex));
        MatchManager.Instance.UpdatePossessionAfterPass(hex);
        finalThirdManager.TriggerFinalThirdPhase();
        MatchManager.Instance.BroadcastEndofFirstTimePass();
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
        if (interceptionCandidates.Count == 0)
        {
            Debug.LogWarning("No defenders available for FTP interception rolls.");
            return;
        }

        currentDefenderHex = interceptionCandidates[0].DefenderHex;
        isWaitingForDiceRoll = true;
        Debug.Log("Starting FTP interception dice roll sequence... Press [R] to roll.");
    }

    private void PerformFTPInterceptionRolls(int? rigRoll = null)
    {
        if (currentDefenderHex == null)
        {
            Debug.LogError("Cannot roll FTP interception because no current defender hex is set.");
            return;
        }

        GroundInterceptionCandidate currentCandidate = interceptionCandidates
            .FirstOrDefault(candidate => candidate.DefenderHex == currentDefenderHex);

        if (currentCandidate == null || currentCandidate.DefenderToken == null)
        {
            Debug.LogError("No matching defender found for FTP interception rolls.");
            return;
        }

        PlayerToken defenderToken = currentCandidate.DefenderToken;
        int tackling = defenderToken.tackling;
        var (returnedRoll, returnedJackpot) = helperFunctions.DiceRoll();
        int diceRoll = rigRoll ?? returnedRoll;

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
            HexCell interceptionHex = currentDefenderHex;
            ResetFTPInterceptionDiceRolls();
            CleanUpFTP();
            StartCoroutine(HandleBallInterception(interceptionHex));
            return;
        }

        Debug.Log($"{defenderToken.name} at {currentDefenderHex.coordinates} failed to intercept.");
        interceptionCandidates.Remove(currentCandidate);

        if (interceptionCandidates.Count > 0)
        {
            currentDefenderHex = interceptionCandidates[0].DefenderHex;
            isWaitingForDiceRoll = true;
            Debug.Log("Starting next FTP interception roll... Press [R] to roll.");
            return;
        }

        Debug.Log("FTP successful! No more defenders to roll.");
        currentDefenderHex = null;
        StartCoroutine(MovePassNotIntercepted(currentTargetHex));
    }

    private IEnumerator HandleBallInterception(HexCell defenderHex)
    {
        yield return StartCoroutine(HandleGroundBallMovement(defenderHex));
        MatchManager.Instance.ChangePossession();
        MatchManager.Instance.currentState = MatchManager.GameState.LooseBallPickedUp;
        MatchManager.Instance.UpdatePossessionAfterPass(defenderHex);
        finalThirdManager.TriggerFinalThirdPhase();
        MatchManager.Instance.BroadcastAnyOtherScenario();
    }

    private void ResetFTPInterceptionDiceRolls()
    {
        interceptionCandidates.Clear();
        currentDefenderHex = null;
        isWaitingForDiceRoll = false;
    }

    public IEnumerator HandleGroundBallMovement(HexCell targetHex)
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

        yield return StartCoroutine(ball.MoveToCell(targetHex));
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

    public string GetInstructions()
    {
        StringBuilder sb = new();
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
            int difficulty = MatchManager.Instance.difficulty_level;
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
                    sb.Append($"Click on a Hex up to {FtpMaxDistance} hexes away from {MatchManager.Instance.LastTokenToTouchTheBallOnPurpose.name}, ");
                }
                else
                {
                    sb.Append("Click the highlighted target again to confirm the First-Time Pass, ");
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
            if (selectedToken == null)
            {
                sb.Append("Click on an Attacker to show a 1-hex move, or press [X] to skip, ");
            }
            else
            {
                sb.Append($"Click on a highlighted Hex to move {selectedToken.name}, click another attacker to switch player, or press [X] to skip, ");
            }
        }

        if (isWaitingForDefenderSelection)
        {
            if (selectedToken == null)
            {
                sb.Append("Click on a Defender to show a 1-hex move, or press [X] to skip, ");
            }
            else
            {
                sb.Append($"Click on a highlighted Hex to move {selectedToken.name}, click another defender to switch player, or press [X] to skip, ");
            }
        }

        if (isWaitingForDiceRoll)
        {
            PlayerToken currentDefender = currentDefenderHex != null ? currentDefenderHex.GetOccupyingToken() : null;
            if (currentDefender != null)
            {
                sb.Append($"Press [R] to roll for interception with {currentDefender.name}, a roll of {GetCurrentInterceptionRollDescription()} is needed, ");
            }
        }

        if (sb.Length >= 2 && sb[^2] == ',') sb.Length -= 2;
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
        currentTargetPreviewAttempts = 0;
    }
}
