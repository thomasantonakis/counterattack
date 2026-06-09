using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FreeKickManager : MonoBehaviour
{
    [Header("Dependencies")]
    public HexGrid hexGrid;
    public Ball ball;
    public MatchManager matchManager;
    public MovementPhaseManager movementPhaseManager;
    public HighPassManager highPassManager;
    public GroundBallManager groundBallManager;
    public FinalThirdManager finalThirdManager;
    [Header("Important Items")]
    public bool isActivated = false;
    public bool isWaitingForKickerSelection = false;
    public bool isWaitingForSetupPhase = false;
    public bool isWaitingforMovement3 = false;
    public bool isWaitingForFinalKickerSelection = false;
    public bool isWaitingForExecution = false;
    public bool isCornerKick = false;
    [SerializeField]
    private List<PlayerToken> shouldDefMoveTokens = new List<PlayerToken>();
    [SerializeField]
    private List<PlayerToken> potentialKickers = new List<PlayerToken>();
    [SerializeField]
    private int movesUsed;
    public int remainingDefenderMoves;
    public int attackerMovesUsed;
    public int defenderMovesUsed;
    public PlayerToken selectedKicker;
    public PlayerToken selectedToken;
    public HexCell targetHex;
    public HexCell spotkick;
    private HexCell hoveredSetupMoveHex;
    private bool isMovingSetupToken;

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

    private void Update()
    {
        if (isWaitingForExecution
            && MatchManager.Instance != null
            && MatchManager.Instance.currentState != MatchManager.GameState.FreeKickExecution)
        {
            FinishExecutionSelection();
        }
    }

    private void OnHoverReceived(PlayerToken token, HexCell hex)
    {
        if (!CanShowSetupMoveHover(hex))
        {
            ClearSetupMoveHover();
            return;
        }

        if (hoveredSetupMoveHex == hex)
        {
            return;
        }

        ClearSetupMoveHover();
        hoveredSetupMoveHex = hex;
        hoveredSetupMoveHex.HighlightHex("FreeKickSelectedMoveHover");
    }

    private void ClearSetupMoveHover()
    {
        if (hoveredSetupMoveHex == null)
        {
            return;
        }

        HexCell previousHover = hoveredSetupMoveHex;
        hoveredSetupMoveHex = null;
        previousHover.ResetHighlight();

        if (hexGrid != null && hexGrid.highlightedHexes.Contains(previousHover))
        {
            previousHover.HighlightHex("PaceAvailable");
        }
    }

    private void OnClickReceived(PlayerToken token, HexCell hex)
    {
        if (!isActivated) return;
        if (finalThirdManager.isActivated) return;
        if (isWaitingForKickerSelection)
        {
            if (token != null) StartCoroutine(HandleKickerSelection(token));
            else Debug.Log(hex != null
                ? $"There is no Token on {hex.name}. Doing nothing!"
                : "No token or valid hex clicked during kicker selection. Doing nothing!");
            return;
        }
        if (isWaitingForSetupPhase)
        {
            if (selectedToken != null)
            {
                if (token != null && token != selectedToken)
                {
                  Debug.Log($"New Clicked token during free kick setup: {token.name}");
                  HandleSetupTokenSelection(token);
                  return;
                }
                if (hex != null)
                {
                    if (!hex.isDefenseOccupied && !hex.isAttackOccupied)
                    {
                        Debug.Log($"Token {selectedToken.name} moving to Hex {hex.coordinates}");
                        StartCoroutine(HandleSetupHexSelection(hex));
                    }
                    else
                    {
                        Debug.LogWarning($"Hex {hex.coordinates} is occupied. Select an unoccupied Hex.");
                    }
                }
                else
                {
                    Debug.LogWarning("Please click on a valid Hex to move the selected token.");
                }
                return;
            }
            else if (token != null)
            {
                {
                    Debug.Log($"Clicked token during free kick setup: {token.name}");
                    HandleSetupTokenSelection(token);
                    return;
                }
            }
            else
            {
                if (isWaitingforMovement3)
                {
                    if (hex != null && selectedToken != null && hexGrid.highlightedHexes.Contains(hex))
                    {
                        Debug.Log($"Moving selected token {selectedToken.name} during free kick movement 3.");
                        StartCoroutine(movementPhaseManager.MoveTokenToHex(hex, selectedToken, false));
                    }
                    else
                    {
                        Debug.LogWarning("Please select a valid token, then click a highlighted Hex for free kick movement 3.");
                    }
                    return;
                }
                Debug.LogWarning(hex != null
                    ? $"Hex {hex.name} is unoccupied. Please select a valid token."
                    : "No token or valid hex clicked during free kick setup. Please select a valid token.");
                return;
            } 
        }
        if (isWaitingForFinalKickerSelection)
        {
            if (token != null && potentialKickers.Contains(token))
            {
                SelectKickerAndAdvance(token);
                return;
            }
        }
    }

    private void OnKeyReceived(KeyPressData keyData)
    {
        if (!isActivated) return;
        if (finalThirdManager.isActivated) return;
        if (isWaitingForKickerSelection)
        {
            if (keyData.key == KeyCode.X)
            {
                Debug.Log("Player pressed X to skip kicker selection.");
                keyData.Consume(nameof(FreeKickManager));
                StartCoroutine(HandleKickerSelection());  // Pass no token to skip
                return;
            }
        }
        if (isWaitingForSetupPhase)
        {
            if (keyData.key == KeyCode.X)
            {
                Debug.Log("Player attempts to forfeit the remaining moves for this phase.");
                ClearSetupMoveHover();
                selectedToken = null;  // Reset the selected token
                hexGrid.ClearHighlightedHexes();
                AttemptToAdvanceToNextPhase();
                keyData.Consume(nameof(FreeKickManager));
                return;
            }
        }
        if (keyData.isConsumed) return;
        if (isWaitingForExecution)
        {
            bool keyWasHandled = isCornerKick
                ? HandleCornerKickExecutionKey(keyData.key)
                : HandleFreeKickExecutionKey(keyData.key);

            if (keyWasHandled)
            {
                keyData.Consume(nameof(FreeKickManager));
            }
        }
    }

    private bool HandleCornerKickExecutionKey(KeyCode key)
    {
        if (key == KeyCode.C)
        {
            RecordSetPieceSelection("cross_selected", KeyCode.C);
            CancelShotPreview();
            hexGrid.ClearHighlightedHexes();
            MatchManager.Instance.TriggerHighPass(isCornerKick: true);
            FinishExecutionSelectionIfAutoCommitted();
            return true;
        }

        if (key == KeyCode.P)
        {
            RecordSetPieceSelection("short_pass_selected", KeyCode.P);
            CancelShotPreview();
            hexGrid.ClearHighlightedHexes();
            MatchManager.Instance.OfferShortGroundBallPass();
            MatchManager.Instance.TriggerStandardPass();
            FinishExecutionSelectionIfAutoCommitted();
            return true;
        }

        return false;
    }

    private bool HandleFreeKickExecutionKey(KeyCode key)
    {
        if (key == KeyCode.L)
        {
            RecordSetPieceSelection("long_ball_selected", KeyCode.L);
            CancelShotPreview();
            hexGrid.ClearHighlightedHexes();
            MatchManager.Instance.TriggerLongPass();
            if (ShouldAutoCommitExecutionChoice())
            {
                MatchManager.Instance.longBallManager.CommitToThisAction();
                FinishExecutionSelection();
            }
            return true;
        }

        if (key == KeyCode.C)
        {
            RecordSetPieceSelection("cross_selected", KeyCode.C);
            CancelShotPreview();
            hexGrid.ClearHighlightedHexes();
            MatchManager.Instance.TriggerHighPass();
            FinishExecutionSelectionIfAutoCommitted();
            return true;
        }

        if (key == KeyCode.P)
        {
            RecordSetPieceSelection("standard_pass_selected", KeyCode.P);
            CancelShotPreview();
            hexGrid.ClearHighlightedHexes();
            MatchManager.Instance.TriggerStandardPass();
            FinishExecutionSelectionIfAutoCommitted();
            return true;
        }

        if (key == KeyCode.S)
        {
            return HandleFreeKickShotSelection();
        }

        return false;
    }

    private bool HandleFreeKickShotSelection()
    {
        if (!IsFreeKickShotAvailable())
        {
            Debug.LogWarning("Shot is not available from this Free Kick.");
            return false;
        }

        if (ShouldAutoCommitExecutionChoice() || MatchManager.Instance.shotManager.isWaitingForShotCommitConfirmation)
        {
            PlayerToken freeKickShooter = selectedKicker != null
                ? selectedKicker
                : MatchManager.Instance.LastTokenToTouchTheBallOnPurpose;
            if (!CanStartFreeKickShot(freeKickShooter))
            {
                return true;
            }

            ClearNonShotExecutionPreviews();
            RecordSetPieceOutcome(
                "restart.committed",
                "shot_committed",
                freeKickShooter,
                MatchManager.Instance.ball?.GetCurrentHex(),
                new Dictionary<string, string> { ["key"] = KeyCode.S.ToString() });
            MatchManager.Instance.shotManager.StartFreeKickShotProcess(freeKickShooter);
            FinishExecutionSelection();
            return true;
        }

        RecordSetPieceSelection("shot_previewed", KeyCode.S);
        ClearNonShotExecutionPreviews();
        hexGrid.ClearHighlightedHexes();
        MatchManager.Instance.shotManager.PreviewFreeKickShotCommit();
        Debug.Log("Free Kick Shot selected. Press [S] again to commit, or press [P], [C], [L] to choose another option.");
        return true;
    }

    private bool IsFreeKickShotAvailable()
    {
        return MatchManager.Instance != null
            && MatchManager.Instance.shotManager != null
            && MatchManager.Instance.shotManager.IsFreeKickShotAvailableFromBall();
    }

    private bool CanStartFreeKickShot(PlayerToken shooter)
    {
        if (shooter == null)
        {
            Debug.LogError("Cannot start Free Kick Shot without a selected taker.");
            return false;
        }

        HexCell shooterHex = shooter.GetCurrentHex();
        if (shooterHex == null)
        {
            Debug.LogError($"Cannot start Free Kick Shot because {shooter.name} is not on a hex.");
            return false;
        }

        if (!IsFreeKickShotAvailable())
        {
            HexCell ballHex = ball != null ? ball.GetCurrentHex() : null;
            string ballHexName = ballHex != null ? ballHex.coordinates.ToString() : "unknown";
            Debug.LogWarning($"Shot is no longer available from the Free Kick ball hex {ballHexName}.");
            return false;
        }

        return true;
    }

    private bool ShouldAutoCommitExecutionChoice()
    {
        return MatchManager.Instance != null
            && MatchManager.Instance.difficulty_level == 3;
    }

    private void FinishExecutionSelectionIfAutoCommitted()
    {
        if (ShouldAutoCommitExecutionChoice())
        {
            FinishExecutionSelection();
        }
    }

    private void FinishExecutionSelection()
    {
        isActivated = false;
        isWaitingForExecution = false;
        isCornerKick = false;
        CancelShotPreview();
    }

    private void CancelShotPreview()
    {
        if (MatchManager.Instance != null && MatchManager.Instance.shotManager != null)
        {
            MatchManager.Instance.shotManager.CancelShotCommitPreview();
        }
    }

    private void ClearNonShotExecutionPreviews()
    {
        if (MatchManager.Instance == null)
        {
            return;
        }

        MatchManager.Instance.groundBallManager?.CleanUpPass();
        MatchManager.Instance.highPassManager?.CleanUpHighPass();
        MatchManager.Instance.longBallManager?.CleanUpLongBall();
    }

    public void StartFreeKickPreparation(HexCell cornerKickSpot = null)
    {
        isActivated = true;
        matchManager.PauseMatchClockForSetPiecePrep();
        if (cornerKickSpot == null) Debug.Log("Starting Free Kick Preparation...");
        else
        {
            isCornerKick = true;
            spotkick = cornerKickSpot;
            Debug.Log("Starting Corner Kick Preparation...");
        }
        matchManager.currentState = MatchManager.GameState.FreeKickKickerSelect;
        isWaitingForKickerSelection = true;
        remainingDefenderMoves = 6;
        attackerMovesUsed = 0;
        defenderMovesUsed = 0;
        CalculateDefendersThatNeedToMove();
        RecordSetPieceOutcome(
            "restart.started",
            "preparation_started",
            targetHex: CurrentSetPieceHex(),
            details: new Dictionary<string, string> { ["isCornerKick"] = isCornerKick.ToString() });
    }

    private void CalculatePotentialKickers()
    {
        potentialKickers.Clear();
        Debug.Log("HandleKicker Selection: Calculating potential kickers...");
        potentialKickers.AddRange(GetPotentialKickersAroundBall());
    }

    private void CalculateDefendersThatNeedToMove()
    {
        shouldDefMoveTokens.Clear();
        shouldDefMoveTokens.AddRange(GetDefendersTooCloseToBall());
    }

    private List<PlayerToken> GetPotentialKickersAroundBall()
    {
        List<PlayerToken> kickers = new();
        if (ball == null || hexGrid == null || hexGrid.cells == null)
        {
            return kickers;
        }

        HexCell ballHex = ball.GetCurrentHex();
        if (ballHex == null)
        {
            return kickers;
        }

        List<HexCell> nearbyHexes = HexGrid.GetHexesInRange(hexGrid, ballHex, 1);
        foreach (HexCell hex in nearbyHexes)
        {
            PlayerToken token = hex != null ? hex.GetOccupyingToken() : null;
            if (token != null && token.isAttacker && !kickers.Contains(token))
            {
                kickers.Add(token);
            }
        }

        return kickers;
    }

    private List<PlayerToken> GetDefendersTooCloseToBall()
    {
        List<PlayerToken> defenders = new();
        if (ball == null || hexGrid == null || hexGrid.cells == null)
        {
            return defenders;
        }

        HexCell ballHex = ball.GetCurrentHex();
        if (ballHex == null)
        {
            return defenders;
        }

        List<HexCell> nearbyHexes = HexGrid.GetHexesInRange(hexGrid, ballHex, 2);
        foreach (HexCell hex in nearbyHexes)
        {
            PlayerToken token = hex != null ? hex.GetOccupyingToken() : null;
            if (token != null && !token.isAttacker && !defenders.Contains(token))
            {
                defenders.Add(token);
            }
        }

        return defenders;
    }
    
    private IEnumerator HandleKickerSelection(PlayerToken clickedToken = null)
    {
        yield return null; // to separate the X from Kicker Selection
        if (clickedToken == null && isCornerKick && GetPotentialKickersAroundBall().Count == 0)
        {
            Debug.LogWarning("There are no attackers near the ball for the Corner Kick, please click on an Attacker to go take it!");
            yield break;
        }
        // If a token was pre-selected (e.g., passed from GIM), handle it immediately
        Debug.Log($"Handling kicker selection for {clickedToken?.name}...");
        if (clickedToken != null)
        {
            if (!clickedToken.isAttacker)
            {
                Debug.Log($"Click ignored: {clickedToken.name} is a defender and cannot be selected as the kicker.");
                yield break;
            }
            Debug.Log($"Selected {clickedToken.name} as the kicker.");
            if (isCornerKick)
            {
                bool placementSucceeded = false;
                yield return StartCoroutine(PlaceCornerKickTaker(clickedToken, success => placementSucceeded = success));
                if (!placementSucceeded)
                {
                    yield break;
                }
            }
            else
            {
                HexCell targetHex = GetClosestAvailableHexToBall();
                if (targetHex != null)
                {
                    yield return StartCoroutine(MoveTokenToHex(clickedToken, targetHex));
                }
                else
                {
                    // TODO: Decide how to resolve the rare set-piece case where every hex touching the fouled dribbler is occupied.
                    Debug.LogError("Target Hex is null!");
                }
            }
        }
        Debug.Log($"isWaitingForKickerSelection is now set to false");
        isWaitingForKickerSelection = false;
        hexGrid.ClearHighlightedHexes(); 
        CalculatePotentialKickers();
        CalculateDefendersThatNeedToMove();
        RecordSetPieceOutcome(
            "restart.taker_selected",
            clickedToken == null ? "taker_skipped" : "initial_taker_selected",
            clickedToken,
            clickedToken != null ? clickedToken.GetCurrentHex() : CurrentSetPieceHex(),
            new Dictionary<string, string> { ["phase"] = "initial_kicker_selection" });
        // Transition to the first phase
        StartCoroutine(HandleSetupPhase(MatchManager.GameState.FreeKickAttGK, 1));
        yield break;  // Exit early since we already handled the token            
    }

    private IEnumerator PlaceCornerKickTaker(PlayerToken selectedTaker, System.Action<bool> onComplete)
    {
        if (selectedTaker == null || spotkick == null)
        {
            Debug.LogWarning("Corner Kick taker placement failed because the selected taker or corner spot is missing.");
            onComplete?.Invoke(false);
            yield break;
        }

        PlayerToken spotOccupant = spotkick.GetOccupyingToken();
        if (spotOccupant == null || spotOccupant == selectedTaker)
        {
            if (selectedTaker.GetCurrentHex() != spotkick)
            {
                yield return StartCoroutine(MoveTokenToHex(selectedTaker, spotkick));
            }

            onComplete?.Invoke(true);
            yield break;
        }

        if (spotOccupant.isAttacker)
        {
            HexCell takerZoiHex = GetBestCornerSpotZoiHex(selectedTaker);
            if (takerZoiHex == null)
            {
                Debug.LogWarning($"Corner Kick taker placement failed: {spotkick.coordinates} is occupied by {spotOccupant.name} and no free ZOI hex is available for {selectedTaker.name}.");
                onComplete?.Invoke(false);
                yield break;
            }

            if (selectedTaker.GetCurrentHex() != takerZoiHex)
            {
                yield return StartCoroutine(MoveTokenToHex(selectedTaker, takerZoiHex));
            }

            Debug.Log($"{selectedTaker.name} moves to {takerZoiHex.coordinates} in the corner spot ZOI because {spotOccupant.name} already occupies the corner spot.");
            onComplete?.Invoke(true);
            yield break;
        }

        HexCell defenderZoiHex = GetBestCornerSpotZoiHex(spotOccupant);
        if (defenderZoiHex == null)
        {
            Debug.LogWarning($"Corner Kick taker placement failed: defender {spotOccupant.name} occupies {spotkick.coordinates}, but no free ZOI hex is available to clear the corner spot.");
            onComplete?.Invoke(false);
            yield break;
        }

        yield return StartCoroutine(MoveTokenToHex(spotOccupant, defenderZoiHex));
        Debug.Log($"{spotOccupant.name} clears the corner spot to {defenderZoiHex.coordinates}.");

        if (selectedTaker.GetCurrentHex() != spotkick)
        {
            yield return StartCoroutine(MoveTokenToHex(selectedTaker, spotkick));
        }

        onComplete?.Invoke(true);
    }

    private HexCell GetBestCornerSpotZoiHex(PlayerToken movingToken)
    {
        if (spotkick == null || hexGrid == null || movingToken == null)
        {
            return null;
        }

        HexCell currentHex = movingToken.GetCurrentHex();
        return spotkick.GetNeighbors(hexGrid)
            .Where(hex => hex != null && !hex.isOutOfBounds)
            .Where(hex =>
            {
                PlayerToken occupant = hex.GetOccupyingToken();
                bool occupiedByMovingToken = occupant == movingToken;
                return occupiedByMovingToken
                    || (occupant == null && !hex.isAttackOccupied && !hex.isDefenseOccupied);
            })
            .OrderBy(hex => currentHex != null ? HexGridUtils.GetHexStepDistance(currentHex.coordinates, hex.coordinates) : 0)
            .ThenBy(hex => HexGridUtils.GetHexStepDistance(hex.coordinates, new Vector3Int(0, 0, 0)))
            .ThenBy(hex => hex.coordinates.x)
            .ThenBy(hex => hex.coordinates.z)
            .FirstOrDefault();
    }

    private HexCell GetClosestAvailableHexToBall()
    {
        HexCell ballHex = ball.GetCurrentHex();
        HexCell[] neighbors = ballHex.GetNeighbors(hexGrid);

        return neighbors
            .Where(hex => !hex.isDefenseOccupied && !hex.isAttackOccupied && !hex.isOutOfBounds)
            .OrderBy(hex => HexGridUtils.GetHexStepDistance(hex.coordinates, new Vector3Int(0, 0, 0)))
            .FirstOrDefault();
    }

    private IEnumerator HandleSetupPhase(MatchManager.GameState phaseState, int maxMoves)
    {
        Debug.Log($"Starting {phaseState} phase with {maxMoves} moves allowed.");
        matchManager.currentState = phaseState;
        isWaitingForSetupPhase = true;
        movesUsed = 0;

        while (isWaitingForSetupPhase && movesUsed < maxMoves)
        {
            yield return null;
        }
        Debug.Log($"Setup phase {phaseState} completed with {movesUsed} moves.");
        AdvanceToNextPhase(phaseState);
    }

    private void HandleSetupTokenSelection(PlayerToken token)
    {
        if (MatchManager.Instance.currentState == MatchManager.GameState.FreeKickDefineKicker)
        {
            if (!token.isAttacker)
            {
                Debug.LogWarning($"Token {token.name} is not an attacker. Invalid Selection for a Kicker!");
                return;
            }
            else if (!potentialKickers.Contains(token))
            {
                Debug.LogWarning($"Token {token.name} is not close to the ball. Invalid Selection for a Kicker!");
                return;
            }
            else
            {
                Debug.LogWarning($"Token {token.name} Selected to take the Set Piece!");
                SelectKickerAndAdvance(token);
                return;
            }
        }
        // Debug.Log($"Selected token: {token.name} for current phase {MatchManager.Instance.currentState}");
        if (MatchManager.Instance.currentState.ToString().StartsWith("FreeKickAtt") && !token.isAttacker)
        {
            Debug.LogWarning($"Token {token.name} is not an attacker. Invalid selection for this phase.");
            return;
        }

        if (MatchManager.Instance.currentState.ToString().StartsWith("FreeKickDef") && token.isAttacker)
        {
            Debug.LogWarning($"Token {token.name} is not a defender. Invalid selection for this phase.");
            return;
        }
        if (
            (
                MatchManager.Instance.currentState.ToString().StartsWith("FreeKickAttG")
                || MatchManager.Instance.currentState.ToString().StartsWith("FreeKickDefG")
            )
            && !token.IsGoalKeeper
        )
        {
            if (MatchManager.Instance.currentState.ToString().StartsWith("FreeKickAttG"))
            {
                Debug.LogWarning($"Token {token.name} is not the Goalkeeper of the attacking team.");
            }
            else 
            {
                Debug.LogWarning($"Token {token.name} is not the Goalkeeper of the defending team.");
            }
            return;
        }
        if
        (
            matchManager.currentState.ToString().StartsWith("FreeKickDef") // During DefX state
            && !matchManager.currentState.ToString().StartsWith("FreeKickDefM")
            && !matchManager.currentState.ToString().StartsWith("FreeKickDefG")
            && !token.isAttacker // Clicked token is a defender
            && !shouldDefMoveTokens.Contains(token) // Clicked token is not in the list of defenders that need to move
            && shouldDefMoveTokens.Count >= remainingDefenderMoves // Defenders that need to move are as many as the remaining moves.
        )
        {
            Debug.LogWarning($"There are as many defenders that need to move ({shouldDefMoveTokens.Count}) as remaining defender moves ({remainingDefenderMoves}). Please select defender close to the ball");
            return;
        }
        if
        (
            matchManager.currentState.ToString().StartsWith("FreeKickAtt") // During AttX state
            && token.isAttacker // Clicked token is an attacker
            && potentialKickers.Contains(token) // Clicked token is in the list potential kickers.
            && potentialKickers.Count == 1 // There is only one attacker as a potential kicker
        )
        {
            Debug.LogWarning($"There must be at least one attacker on the ball or around it. Please select another attacker.");
            return;
        }
        if (matchManager.currentState == MatchManager.GameState.FreeKickAttMovement3 && token.isAttacker)
        {
            Debug.Log($"Attacker {token.name} selected for movement 3 hexes.");
            movementPhaseManager.HighlightValidMovementHexes(token, 3);
        }
        else if (matchManager.currentState == MatchManager.GameState.FreeKickDefMovement3 && !token.isAttacker)
        {
            Debug.Log($"Defender {token.name} selected for movement 3 hexes.");
            movementPhaseManager.HighlightValidMovementHexes(token, 3);
        }
        // Clear targetHex if switching tokens mid-selection
        targetHex = null;
        selectedToken = token;
        ClearSetupMoveHover();
        Debug.Log($"Token {token.name} selected for current phase {MatchManager.Instance.currentState}. Awaiting destination hex.");
    }

    private IEnumerator HandleSetupHexSelection(HexCell hex)
    {
        // Reject if no token is currently selected
        if (selectedToken == null)
        {
            Debug.LogWarning("No token selected. Please select a valid token first.");
            yield break;
        }
        // Check if the hex is valid
        if (hex.isDefenseOccupied || hex.isAttackOccupied)
        {
            Debug.LogWarning($"Hex {hex.coordinates} is occupied. Select a valid destination hex.");
            yield break;
        }

        // Validate hex for defenders in Def phases
        if (MatchManager.Instance.currentState.ToString().StartsWith("FreeKickDef") &&
            // TODO: Check if cube coordinates are needed as defender was not allowed to move to (-17,10) when corner is at (-18, -12)
            HexGridUtils.GetHexStepDistance(hex.coordinates, ball.GetCurrentHex().coordinates) <= 2)
        {
            Debug.LogWarning($"Hex {hex.coordinates} is too close to the ball. Choose another destination.");
            yield break;
        }
        // Validate Reachable Hexes in FreeKick***Movement3 phases
        if (
            (
                MatchManager.Instance.currentState == MatchManager.GameState.FreeKickAttMovement3
                || MatchManager.Instance.currentState == MatchManager.GameState.FreeKickDefMovement3
            )
            && (
                !hexGrid.highlightedHexes.Contains(hex)
                || hex.isDefenseOccupied
                || hex.isAttackOccupied
            )
        )
        {
            Debug.LogWarning($"{selectedToken.name} cannot reach {hex.name}");
            yield break;
        }
        isMovingSetupToken = true;
        ClearSetupMoveHover();
        targetHex = hex;
        Debug.Log($"Token {selectedToken.name} moving to hex {hex.coordinates}.");
        if 
        (
            MatchManager.Instance.currentState == MatchManager.GameState.FreeKickAttMovement3
            || MatchManager.Instance.currentState == MatchManager.GameState.FreeKickDefMovement3
        )
        {
            // Move like in MovementPhase
            yield return StartCoroutine(movementPhaseManager.MoveTokenToHex(hex, selectedToken, false));
        }
        else
        {
            // Jump to selected Hex!
            yield return StartCoroutine(MoveTokenToHex(selectedToken, hex));
        }
        isMovingSetupToken = false;
        if (IsDefensiveSetupState(MatchManager.Instance.currentState))
        {
            Debug.Log("We are in Defensive move, checking if we need to remove the token from the list of defenders that need to move.");
            if (selectedToken == null)
            {
                Debug.LogWarning("No token selected. Please select a valid token first.");
                yield break;
            }
            if (shouldDefMoveTokens.Contains(selectedToken))
            {
                Debug.Log($"Token {selectedToken.name} moved to hex {hex.coordinates}. Removing from list of defenders that need to move.");
                shouldDefMoveTokens.Remove(selectedToken);
            }
            else
            {
                Debug.LogWarning($"Token {selectedToken.name} moved to hex {hex.coordinates}. Not in the list of defenders that need to move.");
            }
            if (IsRegularDefensiveMoveState(MatchManager.Instance.currentState))
            {
                defenderMovesUsed++;
                remainingDefenderMoves--;
            }
        }
        else if (MatchManager.Instance.currentState.ToString().StartsWith("FreeKickAtt"))
        {
            Debug.Log("HandleSetupHexSelection: Calculating potential kickers...");
            CalculatePotentialKickers();
            if (!MatchManager.Instance.currentState.ToString().StartsWith("FreeKickAttG")) attackerMovesUsed++;
        }
        selectedToken = null;
        targetHex = null;
        movesUsed++;
        Debug.Log($"Move {movesUsed} just performed");
    }

    private bool CanShowSetupMoveHover(HexCell hex)
    {
        if (!isActivated
            || !isWaitingForSetupPhase
            || selectedToken == null
            || hex == null
            || finalThirdManager.isActivated
            || isMovingSetupToken
            || MatchManager.Instance == null
            || MatchManager.Instance.difficulty_level != 1)
        {
            return false;
        }

        if (hex.isDefenseOccupied || hex.isAttackOccupied)
        {
            return false;
        }

        MatchManager.GameState state = MatchManager.Instance.currentState;
        if (!IsSelectedTokenValidForSetupState(state))
        {
            return false;
        }

        if (state.ToString().StartsWith("FreeKickDef"))
        {
            HexCell ballHex = ball != null ? ball.GetCurrentHex() : null;
            if (ballHex == null || HexGridUtils.GetHexStepDistance(hex.coordinates, ballHex.coordinates) <= 2)
            {
                return false;
            }
        }

        if ((state == MatchManager.GameState.FreeKickAttMovement3
                || state == MatchManager.GameState.FreeKickDefMovement3)
            && !hexGrid.highlightedHexes.Contains(hex))
        {
            return false;
        }

        return true;
    }

    private bool IsSelectedTokenValidForSetupState(MatchManager.GameState state)
    {
        if (state.ToString().StartsWith("FreeKickAtt") && !selectedToken.isAttacker)
        {
            return false;
        }

        if (state.ToString().StartsWith("FreeKickDef") && selectedToken.isAttacker)
        {
            return false;
        }

        if ((state == MatchManager.GameState.FreeKickAttGK
                || state == MatchManager.GameState.FreeKickDefGK1
                || state == MatchManager.GameState.FreeKickDefGK2)
            && !selectedToken.IsGoalKeeper)
        {
            return false;
        }

        return true;
    }

    private void AttemptToAdvanceToNextPhase()
    {
        if (ShouldBlockDefensiveForfeit())
        {
            return;
        }
        else if
        (
            MatchManager.Instance.currentState == MatchManager.GameState.FreeKickKickerSelect
            && isCornerKick && potentialKickers.Count == 0
        )
        {
            Debug.LogWarning("You cannot forfeit the Kicker Selection during a Corner Kick. Click on an Attacker to go take it!");
            return;
        }
        else if (
            IsRegularDefensiveMoveState(MatchManager.Instance.currentState)
        )
        {
            Debug.Log("Forfeiting current Defensive move.");
            remainingDefenderMoves -= 2 - movesUsed;
            defenderMovesUsed += 2 - movesUsed;
            movesUsed = 2;
        }
        else if (
            MatchManager.Instance.currentState.ToString().StartsWith("FreeKickDefG")
            || MatchManager.Instance.currentState.ToString().StartsWith("FreeKickAttG")
        )
        {
            Debug.Log("Forfeiting current GK move.");
            movesUsed = 1;
        }
        else if (
            MatchManager.Instance.currentState.ToString().StartsWith("FreeKickAttM")
            || MatchManager.Instance.currentState.ToString().StartsWith("FreeKickDefM")
        )
        {
            Debug.Log("Forfeiting current Movement of 3 Hexes.");
            movesUsed = 1;
        }
        else if (MatchManager.Instance.currentState.ToString().StartsWith("FreeKickAtt3"))
        {
            Debug.Log("Forfeiting current Attacking move.");
            if (isCornerKick)
            {
                attackerMovesUsed += 2 - movesUsed;
                movesUsed = 2;
            }
            else
            {
                attackerMovesUsed += 3 - movesUsed;
                movesUsed = 3;
            }
        }
        else if (
            MatchManager.Instance.currentState.ToString().StartsWith("FreeKickAtt")
        )
        {
            Debug.Log("Forfeiting current Attacking move.");
            attackerMovesUsed += 2 - movesUsed;
            movesUsed = 2;
        }
        else Debug.LogError($"I do not know what to do from {MatchManager.Instance.currentState}");
        // AdvanceToNextPhase(MatchManager.Instance.currentState);
    }

    private bool IsDefensiveSetupState(MatchManager.GameState state)
    {
        return state == MatchManager.GameState.FreeKickDefGK1
            || state == MatchManager.GameState.FreeKickDefGK2
            || state == MatchManager.GameState.FreeKickDef1
            || state == MatchManager.GameState.FreeKickDef2
            || state == MatchManager.GameState.FreeKickDef3
            || state == MatchManager.GameState.FreeKickDefMovement3;
    }

    private bool IsRegularDefensiveMoveState(MatchManager.GameState state)
    {
        return state == MatchManager.GameState.FreeKickDef1
            || state == MatchManager.GameState.FreeKickDef2
            || state == MatchManager.GameState.FreeKickDef3;
    }

    private bool ShouldBlockDefensiveForfeit()
    {
        if (!WouldBlockDefensiveForfeit(out int remainingDefenderMovesIfForfeited, out List<PlayerToken> defendersTooClose))
        {
            return false;
        }

        Debug.LogWarning($"You cannot forfeit this defensive move. Remaining defender moves would be {remainingDefenderMovesIfForfeited}, but defenders within 2 hexes still need to move: {FormatTokenNames(defendersTooClose)}.");
        return true;
    }

    private bool WouldBlockDefensiveForfeit(out int remainingDefenderMovesIfForfeited, out List<PlayerToken> defendersTooClose)
    {
        remainingDefenderMovesIfForfeited = remainingDefenderMoves;
        defendersTooClose = new List<PlayerToken>();
        if (MatchManager.Instance == null)
        {
            return false;
        }

        MatchManager.GameState state = MatchManager.Instance.currentState;
        if (!IsDefensiveSetupState(state))
        {
            return false;
        }

        defendersTooClose = GetDefendersTooCloseToBall();
        if (defendersTooClose.Count == 0)
        {
            return false;
        }

        remainingDefenderMovesIfForfeited = GetRemainingDefenderMovesIfForfeitAccepted(state);
        return remainingDefenderMovesIfForfeited < defendersTooClose.Count;
    }

    private int GetRemainingDefenderMovesIfForfeitAccepted(MatchManager.GameState state)
    {
        if (IsRegularDefensiveMoveState(state))
        {
            return remainingDefenderMoves - (2 - movesUsed);
        }

        return remainingDefenderMoves;
    }

    private void BeginFinalKickerSelection()
    {
        matchManager.SetSubstitutionsAvailable(false, isCornerKick
            ? "Corner Kick final kicker selection"
            : "Free Kick final kicker selection");
        matchManager.currentState = MatchManager.GameState.FreeKickDefineKicker;
        isWaitingforMovement3 = false;
        isWaitingForSetupPhase = false;
        ResetMoves();
        CalculatePotentialKickers();
        matchManager.ResumeMatchClockForLivePlay();

        if (potentialKickers.Count > 1)
        {
            Debug.Log($"Multiple potential kickers available. Select one from the {potentialKickers.Count} near the ball!");
            isWaitingForFinalKickerSelection = true;
            return;
        }

        if (potentialKickers.Count == 1)
        {
            SelectKickerAndAdvance(potentialKickers[0]);
            return;
        }

        Debug.LogError("No attackers are on or touching the ball. Cannot define a set-piece kicker.");
        isWaitingForFinalKickerSelection = true;
    }

    private void SelectKickerAndAdvance(PlayerToken kicker)
    {
        if (kicker == null)
        {
            Debug.LogError("Cannot select a null set-piece kicker.");
            return;
        }

        selectedKicker = kicker;
        isWaitingForSetupPhase = false;
        isWaitingforMovement3 = false;
        isWaitingForFinalKickerSelection = false;
        MatchManager.Instance.ClearLastTokenChain();
        MatchManager.Instance.SetLastToken(selectedKicker);
        MatchManager.Instance.MarkSetPieceTakerForNextTouchExclusion(selectedKicker);
        Debug.Log($"{selectedKicker.name} selected as the kicker!");
        MatchManager.Instance.RecordActionSelection(
            CurrentSetPieceActionName(),
            selectedKicker,
            CurrentSetPieceHex(),
            new Dictionary<string, string> { ["selection"] = "final_kicker" });
        RecordSetPieceOutcome(
            "restart.taker_selected",
            "final_kicker_selected",
            selectedKicker,
            selectedKicker.GetCurrentHex(),
            new Dictionary<string, string> { ["phase"] = "final_kicker_selection" });
        AdvanceToNextPhase(MatchManager.GameState.FreeKickDefineKicker);
    }

    public void AdvanceToNextPhase(MatchManager.GameState currentPhase)
    {
        Debug.Log($"Advancing from {currentPhase} phase.");

        switch (currentPhase)
        {
            case MatchManager.GameState.FreeKickAttGK:
                MatchManager.Instance.currentState = MatchManager.GameState.FreeKickDefGK1;
                StartCoroutine(HandleSetupPhase(MatchManager.GameState.FreeKickDefGK1, 1));
                break;
            case MatchManager.GameState.FreeKickDefGK1:
                MatchManager.Instance.currentState = MatchManager.GameState.FreeKickAtt1;
                StartCoroutine(HandleSetupPhase(MatchManager.GameState.FreeKickAtt1, 2));
                break;
            case MatchManager.GameState.FreeKickAtt1:
                MatchManager.Instance.currentState = MatchManager.GameState.FreeKickDef1;
                StartCoroutine(HandleSetupPhase(MatchManager.GameState.FreeKickDef1, 2));
                break;
            case MatchManager.GameState.FreeKickDef1:
                MatchManager.Instance.currentState = MatchManager.GameState.FreeKickAtt2;
                StartCoroutine(HandleSetupPhase(MatchManager.GameState.FreeKickAtt2, 2));
                break;
            case MatchManager.GameState.FreeKickAtt2:
                MatchManager.Instance.currentState = MatchManager.GameState.FreeKickDef2;
                StartCoroutine(HandleSetupPhase(MatchManager.GameState.FreeKickDef2, 2));
                break;
            case MatchManager.GameState.FreeKickDef2:
                MatchManager.Instance.currentState = MatchManager.GameState.FreeKickAtt3;
                if (isCornerKick) StartCoroutine(HandleSetupPhase(MatchManager.GameState.FreeKickAtt3, 2));
                else StartCoroutine(HandleSetupPhase(MatchManager.GameState.FreeKickAtt3, 3));
                break;
            case MatchManager.GameState.FreeKickAtt3:
                MatchManager.Instance.currentState = MatchManager.GameState.FreeKickDef3;
                StartCoroutine(HandleSetupPhase(MatchManager.GameState.FreeKickDef3, 2));
                break;
            case MatchManager.GameState.FreeKickDef3:
                MatchManager.Instance.currentState = MatchManager.GameState.FreeKickDefGK2;
                StartCoroutine(HandleSetupPhase(MatchManager.GameState.FreeKickDefGK2, 1));
                break;
            case MatchManager.GameState.FreeKickDefGK2:
                if (isCornerKick)
                {
                    isWaitingforMovement3 = true;
                    matchManager.currentState = MatchManager.GameState.FreeKickAttMovement3;
                    matchManager.ResumeMatchClockForLivePlay();
                    StartCoroutine(HandleSetupPhase(MatchManager.GameState.FreeKickAttMovement3, 1));
                }
                else
                {
                    BeginFinalKickerSelection();
                }
                break;
            case MatchManager.GameState.FreeKickAttMovement3:
                matchManager.currentState = MatchManager.GameState.FreeKickDefMovement3;
                StartCoroutine(HandleSetupPhase(MatchManager.GameState.FreeKickDefMovement3, 1));
                break;
            case MatchManager.GameState.FreeKickDefMovement3:
                BeginFinalKickerSelection();
                break;
            case MatchManager.GameState.FreeKickDefineKicker:
                matchManager.currentState = MatchManager.GameState.FreeKickExecution;
                isWaitingForFinalKickerSelection = false;
                isWaitingForExecution = true;
                RecordSetPieceOutcome(
                    "restart.execution_ready",
                    "ready",
                    selectedKicker,
                    CurrentSetPieceHex());
                // FreeKickCleanup();
                if (isCornerKick)
                {
                    MatchManager.Instance.EnableCornerKickOptions();
                    Debug.Log("Corner Kick Preparation completed. Ready for execution.");
                    Debug.Log("Available Options are: Short Standard [P]ass (6 Hexes), [C]ross (High Pass) up to 15 Hexes away or in the box on an attacker, no More movements.)");
                }
                else
                {
                    MatchManager.Instance.EnableFreeKickOptions();
                    Debug.Log("Free Kick Preparation completed. Ready for execution.");
                    Debug.Log("Available Options are: Standard [P]ass, [L]ong Ball, [C]ross (High Pass), [S]hot!");
                }
                break;
        }
    }

    public bool ShouldPauseMatchClockForState(MatchManager.GameState state)
    {
        if (isCornerKick
            && (state == MatchManager.GameState.FreeKickAttMovement3
                || state == MatchManager.GameState.FreeKickDefMovement3))
        {
            return false;
        }

        return true;
    }

    private void FreeKickCleanup()
    {
        ResetMoves();
        isCornerKick = false;
        // selectedKicker = null; 
        spotkick = null;       
        potentialKickers.Clear();
        remainingDefenderMoves = 0;
    }

    private void ResetMoves()
    {
        remainingDefenderMoves = 6;
        attackerMovesUsed = 0;
        defenderMovesUsed = 0;
        movesUsed = 0;
        selectedToken = null;
        targetHex = null;
        shouldDefMoveTokens.Clear();
    }

    private string CurrentSetPieceActionName()
    {
        return isCornerKick ? "corner" : "free_kick";
    }

    private HexCell CurrentSetPieceHex()
    {
        return isCornerKick && spotkick != null ? spotkick : ball?.GetCurrentHex();
    }

    private PlayerToken CurrentSetPieceActor()
    {
        return selectedKicker != null ? selectedKicker : MatchManager.Instance?.LastTokenToTouchTheBallOnPurpose;
    }

    private void RecordSetPieceSelection(string outcome, KeyCode key)
    {
        MatchManager.Instance.RecordActionSelection(
            CurrentSetPieceActionName(),
            CurrentSetPieceActor(),
            CurrentSetPieceHex(),
            new Dictionary<string, string>
            {
                ["selection"] = outcome,
                ["key"] = key.ToString()
            });
        RecordSetPieceOutcome(
            "restart.option_selected",
            outcome,
            CurrentSetPieceActor(),
            CurrentSetPieceHex(),
            new Dictionary<string, string> { ["key"] = key.ToString() });
    }

    private void RecordSetPieceOutcome(
        string kind,
        string outcome,
        PlayerToken actor = null,
        HexCell targetHex = null,
        Dictionary<string, string> details = null)
    {
        if (MatchManager.Instance == null)
        {
            return;
        }

        Dictionary<string, string> eventDetails = details != null
            ? new Dictionary<string, string>(details)
            : new Dictionary<string, string>();
        eventDetails["restartType"] = CurrentSetPieceActionName();
        MatchManager.Instance.RecordGameplayOutcome(
            kind,
            CurrentSetPieceActionName(),
            outcome,
            actor: actor ?? CurrentSetPieceActor(),
            sourceHex: CurrentSetPieceHex(),
            targetHex: targetHex,
            details: eventDetails);
    }

    public IEnumerator MoveTokenToHex(PlayerToken token, HexCell targetHex)
    {
        hexGrid.ClearHighlightedHexes();
        HexCell tokenHex = token.GetCurrentHex();
        if (token.isAttacker)
        {
            tokenHex.isAttackOccupied = false;
            tokenHex.ResetHighlight();
            targetHex.isAttackOccupied = true;  // Mark the target hex as occupied by an attacker
        }
        else
        {
            tokenHex.isDefenseOccupied = false;
            tokenHex.ResetHighlight();
            targetHex.isDefenseOccupied = true;  // Mark the target hex as occupied by an attacker
        }
        yield return StartCoroutine(token.JumpToHex(targetHex));
        if (token.isAttacker)
        {
            targetHex.HighlightHex("isAttackOccupied");
        }
        else
        {
            targetHex.HighlightHex("isDefenseOccupied");
        }
        ball.AdjustBallHeightBasedOnOccupancy();
        yield return null;
    }

    public string GetDebugStatus()
    {
        StringBuilder sb = new();
        sb.Append("FK: ");

        if (isActivated) sb.Append("isActivated, ");
        if (isWaitingForKickerSelection) sb.Append("isWaitingForKickerSelection, ");
        if (isWaitingForSetupPhase) sb.Append("isWaitingForSetupPhase, ");
        if (isWaitingforMovement3) sb.Append("isWaitingforMovement3, ");
        if (isWaitingForFinalKickerSelection) sb.Append("isWaitingForFinalKickerSelection, ");
        if (isWaitingForExecution) sb.Append("isWaitingForExecution, ");
        if (isCornerKick) sb.Append("isCornerKick, ");
        if (targetHex != null) sb.Append($"targetHex: {targetHex.name}, ");

        if (sb.Length >= 2 && sb[^2] == ',') sb.Length -= 2; // Trim trailing comma
        return sb.ToString();
    }

    private static string FormatTokenNames(IEnumerable<PlayerToken> tokens)
    {
        List<string> names = tokens
            .Where(token => token != null)
            .Select(token => !string.IsNullOrWhiteSpace(token.playerName) ? token.playerName : token.name)
            .ToList();

        return names.Count > 0 ? string.Join(", ", names) : "none";
    }

    private bool IsRegularAttackingMoveState(MatchManager.GameState state)
    {
        return state == MatchManager.GameState.FreeKickAtt1
            || state == MatchManager.GameState.FreeKickAtt2
            || state == MatchManager.GameState.FreeKickAtt3;
    }

    private void AppendMoveStage(StringBuilder sb, MatchManager.GameState state)
    {
        if (IsRegularAttackingMoveState(state))
        {
            int maxAttackMoves = isCornerKick ? 6 : 7;
            int currentAttackMove = Mathf.Clamp(attackerMovesUsed + 1, 1, maxAttackMoves);
            sb.Append($"Attack Move {currentAttackMove} of {maxAttackMoves}. ");
        }
        else if (IsRegularDefensiveMoveState(state))
        {
            int currentDefenseMove = Mathf.Clamp(defenderMovesUsed + 1, 1, 6);
            sb.Append($"Defense Move {currentDefenseMove} of 6. ");
        }
    }

    private bool CanForfeitCurrentInstructionPhase()
    {
        return !WouldBlockDefensiveForfeit(out _, out _);
    }

    private string FormatSetupSkipInstruction(string fallbackInstruction)
    {
        return CanForfeitCurrentInstructionPhase()
            ? fallbackInstruction
            : "This phase cannot be skipped until the required defender is moved. ";
    }

    private void AppendSetPieceObligations(StringBuilder sb, MatchManager.GameState state)
    {
        List<PlayerToken> currentKickers = GetPotentialKickersAroundBall();
        List<PlayerToken> defendersTooClose = GetDefendersTooCloseToBall();

        if (IsRegularAttackingMoveState(state))
        {
            sb.Append($"\nSet Piece Checks: Potential Kicker(s): {FormatTokenNames(currentKickers)}. ");
            return;
        }

        if (!IsDefensiveSetupState(state))
        {
            return;
        }

        if (defendersTooClose.Count == 0)
        {
            return;
        }

        sb.Append($"\nSet Piece Checks: {FormatTokenNames(defendersTooClose)} MUST be moved away from the ball. ");
    }

    private bool AppendExecutionPreviewInstruction(StringBuilder sb)
    {
        MatchManager activeMatchManager = MatchManager.Instance;

        if (groundBallManager != null && groundBallManager.isActivated && groundBallManager.isAwaitingTargetSelection)
        {
            if (isCornerKick)
            {
                if (groundBallManager.currentTargetHex == null)
                {
                    sb.Append("Short Standard Pass selected; click a target Hex within 6 hexes, or press [C] to switch to Cross, ");
                }
                else
                {
                    sb.Append("Short Standard Pass target selected; click the same selected Hex again to confirm, click another target to change, or press [C] to switch to Cross, ");
                }
            }
            else
            {
                string alternateOptions = FormatFreeKickExecutionOptions(KeyCode.P);
                if (groundBallManager.currentTargetHex == null)
                {
                    sb.Append($"Standard Pass selected; click a target Hex, or press {alternateOptions} to change option, ");
                }
                else
                {
                    sb.Append($"Standard Pass target selected; click the same selected Hex again to confirm, click another target to change, or press {alternateOptions} to change option, ");
                }
            }
            return true;
        }

        if (highPassManager != null && highPassManager.isActivated && highPassManager.isWaitingForConfirmation)
        {
            if (isCornerKick)
            {
                if (highPassManager.currentTargetHex == null)
                {
                    sb.Append("Cross selected; click a valid attacker target Hex, or press [P] to switch to Short Standard Pass, ");
                }
                else
                {
                    sb.Append("Cross target selected; click the same selected Hex again to confirm, click another valid attacker target to change, or press [P] to switch to Short Standard Pass, ");
                }
                return true;
            }

            string alternateOptions = FormatFreeKickExecutionOptions(KeyCode.C);
            if (highPassManager.currentTargetHex == null)
            {
                sb.Append($"High Pass selected; click a valid target Hex, or press {alternateOptions} to change option, ");
            }
            else
            {
                sb.Append($"High Pass target selected; click the same selected Hex again to confirm, click another valid target to change, or press {alternateOptions} to change option, ");
            }
            return true;
        }

        if (!isCornerKick
            && activeMatchManager != null
            && activeMatchManager.longBallManager != null
            && activeMatchManager.longBallManager.isActivated
            && activeMatchManager.longBallManager.isAwaitingTargetSelection)
        {
            string alternateOptions = FormatFreeKickExecutionOptions(KeyCode.L);
            if (activeMatchManager.longBallManager.currentTargetHex == null)
            {
                sb.Append($"Long Ball selected; click a target Hex, or press {alternateOptions} to change option, ");
            }
            else
            {
                sb.Append($"Long Ball target selected; click the same selected Hex again to confirm, click another target to change, or press {alternateOptions} to change option, ");
            }
            return true;
        }

        if (!isCornerKick
            && activeMatchManager != null
            && activeMatchManager.shotManager != null
            && activeMatchManager.shotManager.isWaitingForShotCommitConfirmation)
        {
            sb.Append($"Shot selected; press [S] again to commit, or press {FormatFreeKickExecutionOptions(KeyCode.S)} to change option, ");
            return true;
        }

        return false;
    }

    private string FormatFreeKickExecutionOptions(KeyCode selectedOption)
    {
        List<string> options = new();
        if (selectedOption != KeyCode.P) options.Add("[P]");
        if (selectedOption != KeyCode.C) options.Add("[C]");
        if (selectedOption != KeyCode.L) options.Add("[L]");
        if (selectedOption != KeyCode.S && IsFreeKickShotAvailable()) options.Add("[S]");
        return string.Join(", ", options);
    }

    public string GetInstructions()
    {
        StringBuilder sb = new();
        sb.Append(isCornerKick ? "CK: " : "FK: ");

        if (!isActivated) return "";
        MatchManager activeMatchManager = MatchManager.Instance != null ? MatchManager.Instance : matchManager;
        if (activeMatchManager == null) return "";

        if (isWaitingForKickerSelection)
        {
            if (isCornerKick && GetPotentialKickersAroundBall().Count == 0)
            {
                sb.Append("Click on an Attacker Token to Move to the Set Piece Spot as potential Kicker. ");
            }
            else
            {
                sb.Append("Click on an Attacker Token to Move to the Set Piece Spot as potential Kicker, or Press [X] to forfeit option. ");
            }
        }
        if (isWaitingForSetupPhase)
        {
            AppendMoveStage(sb, activeMatchManager.currentState);
            switch (activeMatchManager.currentState)
            {
                case MatchManager.GameState.FreeKickAttGK:
                    if (selectedToken != null)
                    {
                        sb.Append($"Click on a Hex to move {selectedToken.name} to, or Press [X] to leave them there, ");
                    }
                    else
                    {
                        sb.Append("Click on the Attacking GK to adjust their position before the moving sequence or Press [X] to leave them there, ");
                    }
                    break;
                case MatchManager.GameState.FreeKickDefGK1:
                    if (selectedToken != null)
                    {
                        sb.Append(FormatSetupSkipInstruction($"Click on a Hex to move {selectedToken.name} to, or Press [X] to leave them there, "));
                    }
                    else
                    {
                        sb.Append(FormatSetupSkipInstruction("Click on the Defending GK to move before the moving sequence or Press [X] to leave them there, "));
                    }
                    break;
                case MatchManager.GameState.FreeKickDefGK2:
                    if (selectedToken != null)
                    {
                        sb.Append(FormatSetupSkipInstruction($"Click on a Hex to move {selectedToken.name} to, or Press [X] to leave them there, "));
                    }
                    else
                    {
                        sb.Append(FormatSetupSkipInstruction("Click on the Defending GK to move just before the Kick or Press [X] to leave them there, "));
                    }
                    break;
                case MatchManager.GameState.FreeKickAtt1:
                case MatchManager.GameState.FreeKickAtt2:
                case MatchManager.GameState.FreeKickAtt3:
                    if (selectedToken != null)
                    {
                        sb.Append($"Click another Attacking Token to move, or Click on a Hex to move {selectedToken.name} to, or Press [X] to skip this sequence part. ");
                    }
                    else
                    {
                        sb.Append("Click on an Attacking Token to move or Press [X] to skip this sequence part. ");
                    }
                    break;
                case MatchManager.GameState.FreeKickDef1:
                case MatchManager.GameState.FreeKickDef2:
                case MatchManager.GameState.FreeKickDef3:
                    if (selectedToken != null)
                    {
                        sb.Append(FormatSetupSkipInstruction($"Click another Defending Token to move, or Click on a Hex to move {selectedToken.name} to, or Press [X] to skip this sequence part. "));
                    }
                    else
                    {
                        sb.Append(FormatSetupSkipInstruction("Click on a Defending Token to move or Press [X] to skip this sequence part. "));
                    }
                    break;
                case MatchManager.GameState.FreeKickAttMovement3:
                    if (isCornerKick)
                    {
                        sb.Append("Click an Attacking Token to move up to 3 Hexes for FREE, or Press [X] to skip. ");
                    }
                    else if (selectedToken != null)
                    {
                        sb.Append($"Click a highlighted Hex to move {selectedToken.name} up to 3 hexes, click another attacker to switch, or Press [X] to skip this movement, ");
                    }
                    else
                    {
                        sb.Append("Click an Attacking Token to move up to 3 hexes, or Press [X] to skip this movement, ");
                    }
                    break;
                case MatchManager.GameState.FreeKickDefMovement3:
                    if (isCornerKick)
                    {
                        sb.Append(FormatSetupSkipInstruction("Click a Defending Token to move up to 3 Hexes for FREE, or Press [X] to skip. "));
                    }
                    else if (selectedToken != null)
                    {
                        sb.Append(FormatSetupSkipInstruction($"Click a highlighted Hex to move {selectedToken.name} up to 3 hexes, click another defender to switch, or Press [X] to skip this movement, "));
                    }
                    else
                    {
                        sb.Append(FormatSetupSkipInstruction("Click a Defending Token to move up to 3 hexes, or Press [X] to skip this movement, "));
                    }
                    break;
            }
        }
        AppendSetPieceObligations(sb, activeMatchManager.currentState);
        if (isWaitingForFinalKickerSelection) sb.Append($"Select the set-piece taker from attackers on/touching the ball: {FormatTokenNames(potentialKickers)}, ");
        if (isWaitingForExecution)
        {
            bool previewInstructionAppended = AppendExecutionPreviewInstruction(sb);
            if (!previewInstructionAppended && isCornerKick)
            {
                sb.Append("Short Standard [P]ass (6 Hexes), [C]ross (High Pass) up to 15 Hexes away or in the box on an attacker, no more movements, ");
            }
            else if (!previewInstructionAppended)
            {
                bool shotIsAvailable = IsFreeKickShotAvailable();
                if (shotIsAvailable)
                {
                    sb.Append("Standard [P]ass, High Pass [C], [L]ong Ball or [S]hot, ");
                }
                else
                {
                    sb.Append("Standard [P]ass, High Pass [C] or [L]ong Ball, ");
                }
            }
        }


        if (sb.Length >= 2 && sb[^2] == ',') sb.Length -= 2; // Trim trailing comma
        return sb.ToString();
    }

    public bool? IsInstructionExpectingHomeTeam()
    {
        if (!isActivated || MatchManager.Instance == null)
        {
            return null;
        }

        bool attackingTeamIsHome = MatchManager.Instance.teamInAttack == MatchManager.TeamInAttack.Home;

        if (isWaitingForKickerSelection || isWaitingForFinalKickerSelection || isWaitingForExecution)
        {
            return attackingTeamIsHome;
        }

        if (isWaitingForSetupPhase)
        {
            switch (MatchManager.Instance.currentState)
            {
                case MatchManager.GameState.FreeKickDefGK1:
                case MatchManager.GameState.FreeKickDefGK2:
                case MatchManager.GameState.FreeKickDef1:
                case MatchManager.GameState.FreeKickDef2:
                case MatchManager.GameState.FreeKickDef3:
                case MatchManager.GameState.FreeKickDefMovement3:
                    return !attackingTeamIsHome;
                default:
                    return attackingTeamIsHome;
            }
        }

        return attackingTeamIsHome;
    }

}
