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

    private void Update()
    {
        if (isWaitingForExecution
            && MatchManager.Instance != null
            && MatchManager.Instance.currentState != MatchManager.GameState.FreeKickExecution)
        {
            FinishExecutionSelection();
        }
    }

    private void OnClickReceived(PlayerToken token, HexCell hex)
    {
        if (!isActivated) return;
        if (finalThirdManager.isActivated) return;
        if (isWaitingForKickerSelection)
        {
            if (token != null) StartCoroutine(HandleKickerSelection(token));
            else Debug.Log($"There is no Token on {hex.name}. Doing nothing!");
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
                    Debug.Log($"Clicked token during free kick setup: {token.name}");
                    StartCoroutine(movementPhaseManager.MoveTokenToHex(hex, selectedToken, false));
                    // yield return StartCoroutine(movementPhaseManager.MoveTokenToHex(hex));
                    return;
                }
                else
                Debug.LogWarning($"Hex {hex.name} is unoccupied. Please select a valid token.");
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
        if (keyData.isConsumed) return;
        if (isWaitingForKickerSelection)
        {
            if (keyData.key == KeyCode.X)
            {
                Debug.Log("Player pressed X to skip kicker selection.");
                StartCoroutine(HandleKickerSelection());  // Pass no token to skip
                return;
            }
        }
        if (isWaitingForSetupPhase)
        {
            if (keyData.key == KeyCode.X)
            {
                Debug.Log("Player attempts to forfeit the remaining moves for this phase.");
                selectedToken = null;  // Reset the selected token
                AttemptToAdvanceToNextPhase();
                return;
            }
        }
        if (isWaitingForExecution)
        {
            bool keyWasHandled = isCornerKick
                ? HandleCornerKickExecutionKey(keyData.key)
                : HandleFreeKickExecutionKey(keyData.key);

            if (keyWasHandled)
            {
                keyData.isConsumed = true;
            }
        }
    }

    private bool HandleCornerKickExecutionKey(KeyCode key)
    {
        if (key == KeyCode.C)
        {
            CancelShotPreview();
            hexGrid.ClearHighlightedHexes();
            MatchManager.Instance.TriggerHighPass(isCornerKick: true);
            FinishExecutionSelectionIfAutoCommitted();
            return true;
        }

        if (key == KeyCode.P)
        {
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
            CancelShotPreview();
            hexGrid.ClearHighlightedHexes();
            MatchManager.Instance.TriggerHighPass();
            FinishExecutionSelectionIfAutoCommitted();
            return true;
        }

        if (key == KeyCode.P)
        {
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

            // TODO: Replace the generic full-power shot handoff with the dedicated Free Kick shot resolver.
            MatchManager.Instance.shotManager.StartShotProcess(freeKickShooter, "fullPower");
            FinishExecutionSelection();
            return true;
        }

        hexGrid.ClearHighlightedHexes();
        MatchManager.Instance.shotManager.PreviewShotCommit();
        Debug.Log("Free Kick Shot selected. Press [S] again to commit, or press [P], [C], [L] to choose another option.");
        return true;
    }

    private bool IsFreeKickShotAvailable()
    {
        return MatchManager.Instance != null
            && MatchManager.Instance.shotManager != null
            && MatchManager.Instance.shotManager.isAvailable;
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

        if (!shooterHex.CanShootFrom)
        {
            Debug.LogWarning($"Shot is no longer available from {shooter.name}'s current hex.");
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

    public void StartFreeKickPreparation(HexCell cornerKickSpot = null)
    {
        isActivated = true;
        if (cornerKickSpot == null) Debug.Log("Starting Free Kick Preparation...");
        else
        {
            isCornerKick = true;
            spotkick = cornerKickSpot;
            Debug.Log("Starting Corner Kick Preparation...");
        }
        // matchManager.currentState = MatchManager.GameState.FreeKickKickerSelect;
        isWaitingForKickerSelection = true;
        remainingDefenderMoves = 6;
        attackerMovesUsed = 0;
        defenderMovesUsed = 0;
        CalculateDefendersThatNeedToMove();
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
        if (ball == null || hexGrid == null)
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
        if (ball == null || hexGrid == null)
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
        if (clickedToken == null && isCornerKick && potentialKickers.Count == 0)
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
                // TODO :this is quite optimistic.
                // There might be cases that an attacker might already be on the spotkick
                // maybe even a defender.
                yield return StartCoroutine(MoveTokenToHex(clickedToken, spotkick));
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
        // Transition to the first phase
        StartCoroutine(HandleSetupPhase(MatchManager.GameState.FreeKickAttGK, 1));
        yield break;  // Exit early since we already handled the token            
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
        }
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
        if (
            MatchManager.Instance.currentState.ToString().StartsWith("FreeKickDef")
            && !MatchManager.Instance.currentState.ToString().StartsWith("FreeKickDefG")
            && !MatchManager.Instance.currentState.ToString().StartsWith("FreeKickDefM")
        )
        {
            Debug.Log("We are in Defensive move, Cheking if we need to remove the token from the list of defenders that need to move.");
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
            defenderMovesUsed++;
            remainingDefenderMoves--;
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

    private void AttemptToAdvanceToNextPhase()
    {
        if (
            MatchManager.Instance.currentState.ToString().StartsWith("FreeKickDef")
            && !MatchManager.Instance.currentState.ToString().StartsWith("FreeKickDefG")
            && !MatchManager.Instance.currentState.ToString().StartsWith("FreeKickDefM")
            // if we let the forfeit happen, then 2 - movesUsed will be forfeited
            // so the remaining defender moves will be (remainingDefenderMoves - (2 - movesUsed))
            // This should be checked against the number of defenders that need to move.
            && remainingDefenderMoves - (2 - movesUsed) < shouldDefMoveTokens.Count
            // AND there are defenders close to the ball.
            && shouldDefMoveTokens.Count > 0
        )
        {
            // Debug.Log($"{MatchManager.Instance.currentState}");
            // Debug.Log($"{MatchManager.Instance.currentState.ToString().StartsWith("FreeKickDef")}");
            // Debug.Log($"{remainingDefenderMoves - (2 - movesUsed) <= shouldDefMoveTokens.Count}");
            // Debug.Log($"{shouldDefMoveTokens.Count == 0}");
            // Debug.Log($"{remainingDefenderMoves} - (2 - {movesUsed}) = {remainingDefenderMoves - (2 - movesUsed)} <= {shouldDefMoveTokens.Count}");
            Debug.LogWarning("You cannot forfeit current move, as the remaining moves will be less than the defenders that need to move.");
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
            MatchManager.Instance.currentState.ToString().StartsWith("FreeKickDef")
            && !MatchManager.Instance.currentState.ToString().StartsWith("FreeKickDefG")
            && !MatchManager.Instance.currentState.ToString().StartsWith("FreeKickDefM")
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

    private void BeginFinalKickerSelection()
    {
        matchManager.currentState = MatchManager.GameState.FreeKickDefineKicker;
        isWaitingforMovement3 = false;
        isWaitingForSetupPhase = false;
        ResetMoves();
        CalculatePotentialKickers();

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
        Debug.Log($"{selectedKicker.name} selected as the kicker!");
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

    private void AppendSetPieceObligations(StringBuilder sb)
    {
        List<PlayerToken> currentKickers = GetPotentialKickersAroundBall();
        List<PlayerToken> defendersTooClose = GetDefendersTooCloseToBall();
        bool setupStillRunning = isWaitingForKickerSelection
            || isWaitingForSetupPhase
            || isWaitingforMovement3
            || isWaitingForFinalKickerSelection;
        bool hasUnresolvedObligation = currentKickers.Count == 0 || defendersTooClose.Count > 0;

        if (!setupStillRunning && !hasUnresolvedObligation)
        {
            return;
        }

        sb.Append("Set-piece checks: ");
        if (currentKickers.Count == 0)
        {
            sb.Append("no attacker on/touching the ball; ");
        }
        else
        {
            sb.Append($"attacker on/touching ball: {FormatTokenNames(currentKickers)}; ");
        }

        if (defendersTooClose.Count == 0)
        {
            sb.Append("no defenders within 2 hexes, ");
            return;
        }

        sb.Append($"defenders within 2 hexes to move: {FormatTokenNames(defendersTooClose)}");
        if (isWaitingForSetupPhase)
        {
            sb.Append($"; defender setup moves left: {remainingDefenderMoves}");
        }
        sb.Append(", ");
    }

    private bool AppendExecutionPreviewInstruction(StringBuilder sb)
    {
        MatchManager activeMatchManager = MatchManager.Instance;

        if (groundBallManager != null && groundBallManager.isActivated && groundBallManager.isAwaitingTargetSelection)
        {
            string alternateOptions = isCornerKick ? "[C]" : "[C], [L], [S]";
            sb.Append($"Standard Pass selected; click a target Hex, or press {alternateOptions} to change option, ");
            return true;
        }

        if (highPassManager != null && highPassManager.isActivated && highPassManager.isWaitingForConfirmation)
        {
            string alternateOptions = isCornerKick ? "[P]" : "[P], [L], [S]";
            sb.Append($"High Pass selected; click a valid target Hex, or press {alternateOptions} to change option, ");
            return true;
        }

        if (!isCornerKick
            && activeMatchManager != null
            && activeMatchManager.longBallManager != null
            && activeMatchManager.longBallManager.isActivated
            && activeMatchManager.longBallManager.isAwaitingTargetSelection)
        {
            sb.Append("Long Ball selected; click a target Hex, or press [P], [C], [S] to change option, ");
            return true;
        }

        if (!isCornerKick
            && activeMatchManager != null
            && activeMatchManager.shotManager != null
            && activeMatchManager.shotManager.isWaitingForShotCommitConfirmation)
        {
            sb.Append("Shot selected; press [S] again to commit, or press [P], [C], [L] to change option, ");
            return true;
        }

        return false;
    }

    public string GetInstructions()
    {
        StringBuilder sb = new();
        sb.Append("FK: ");

        if (!isActivated) return "";
        if (isWaitingForKickerSelection) sb.Append("Click on an Attacker Token to Move to the Set Piece Spot as potential Kicker, ");
        if (isWaitingForSetupPhase)
        {
            switch (MatchManager.Instance.currentState)
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
                        sb.Append($"Click on a Hex to move {selectedToken.name} to, or Press [X] to leave them there, ");
                    }
                    else
                    {
                        sb.Append("Click on the Defending GK to move before the moving sequence or Press [X] to leave them there, ");
                    }
                    break;
                case MatchManager.GameState.FreeKickDefGK2:
                    if (selectedToken != null)
                    {
                        sb.Append($"Click on a Hex to move {selectedToken.name} to, or Press [X] to leave them there, ");
                    }
                    else
                    {
                        sb.Append("Click on the Defending GK to move just before the Kick or Press [X] to leave them there, ");
                    }
                    break;
                case MatchManager.GameState.FreeKickAtt1:
                case MatchManager.GameState.FreeKickAtt2:
                case MatchManager.GameState.FreeKickAtt3:
                    if (selectedToken != null)
                    {
                        sb.Append($"Click another Attacking Token to move, or Click on a Hex to move {selectedToken.name} to, or Press [X] to skip this sequence part, ");
                    }
                    else
                    {
                        sb.Append("Click on an Attacking Token to move or Press [X] to skip this sequence part, ");
                    }
                    break;
                case MatchManager.GameState.FreeKickDef1:
                case MatchManager.GameState.FreeKickDef2:
                case MatchManager.GameState.FreeKickDef3:
                    if (selectedToken != null)
                    {
                        sb.Append($"Click another Defending Token to move, or Click on a Hex to move {selectedToken.name} to, or Press [X] to skip this sequence part, ");
                    }
                    else
                    {
                        sb.Append("Click on a Defending Token to move or Press [X] to skip this sequence part, ");
                    }
                    break;
                case MatchManager.GameState.FreeKickAttMovement3:
                    if (selectedToken != null)
                    {
                        sb.Append($"Click a highlighted Hex to move {selectedToken.name} up to 3 hexes, click another attacker to switch, or Press [X] to skip this movement, ");
                    }
                    else
                    {
                        sb.Append("Click an Attacking Token to move up to 3 hexes, or Press [X] to skip this movement, ");
                    }
                    break;
                case MatchManager.GameState.FreeKickDefMovement3:
                    if (selectedToken != null)
                    {
                        sb.Append($"Click a highlighted Hex to move {selectedToken.name} up to 3 hexes, click another defender to switch, or Press [X] to skip this movement, ");
                    }
                    else
                    {
                        sb.Append("Click a Defending Token to move up to 3 hexes, or Press [X] to skip this movement, ");
                    }
                    break;
            }
        }
        AppendSetPieceObligations(sb);
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
                bool shotIsAvailable = MatchManager.Instance != null
                    && MatchManager.Instance.shotManager != null
                    && MatchManager.Instance.shotManager.isAvailable;
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
