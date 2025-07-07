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
                selectedKicker = token;
                MatchManager.Instance.SetLastToken(selectedKicker);
                AdvanceToNextPhase(MatchManager.GameState.FreeKickDefineKicker);
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
            if (isCornerKick)
            {
                if (keyData.key == KeyCode.C)
                {
                    hexGrid.ClearHighlightedHexes(); 
                    highPassManager.ActivateHighPass();
                    MatchManager.Instance.CommitToAction();
                    highPassManager.isCornerKick = true;
                    isCornerKick = false;
                    isWaitingForExecution = false;
                }
                else if (keyData.key == KeyCode.P)
                {
                    hexGrid.ClearHighlightedHexes(); 
                    MatchManager.Instance.TriggerStandardPass();;
                    // groundBallManager.ActivateGroundBall();
                    MatchManager.Instance.CommitToAction();
                    groundBallManager.imposedDistance = 6;
                    isCornerKick = false;
                    isWaitingForExecution = false;
                }
            }
            else
            {
                if (keyData.key == KeyCode.L)
                {
                    hexGrid.ClearHighlightedHexes(); 
                    MatchManager.Instance.TriggerLongPass();
                    MatchManager.Instance.CommitToAction();
                    isWaitingForExecution = false;
                    isCornerKick = false;
                }
                else if (keyData.key == KeyCode.C)
                {
                    hexGrid.ClearHighlightedHexes();
                    highPassManager.ActivateHighPass(); 
                    highPassManager.isCornerKick = false;
                    isWaitingForExecution = false;
                    isCornerKick = false;
                }
                else if (keyData.key == KeyCode.P)
                {
                    hexGrid.ClearHighlightedHexes(); 
                    groundBallManager.ActivateGroundBall();
                    MatchManager.Instance.CommitToAction();
                    isWaitingForExecution = false;
                    isCornerKick = false;
                }
                else if (keyData.key == KeyCode.S)
                {
                    hexGrid.ClearHighlightedHexes();
                    Debug.Log("Free Kick Shoot triggered."); 
                    isWaitingForExecution = false;
                    isCornerKick = false;
                    // TODO: Implement Free Kick Shoot
                }
            }
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
        // Debug.Log("Calculating potential kickers...");
        HexCell ballHex = ball.GetCurrentHex();
        List<HexCell> nearbyHexes = HexGrid.GetHexesInRange(hexGrid, ballHex, 1);
        foreach (HexCell hex in nearbyHexes)
        {
            // Debug.Log($"Checking hex {hex.coordinates} for potential kickers. Is attack occupied? {hex.isAttackOccupied} By which Token? {hex.GetOccupyingToken()?.name}");
            if (hex.isAttackOccupied)
            {
                // Debug.Log($"Attacker found on the ball or around it: {hex.GetOccupyingToken().name}");
                potentialKickers.Add(hex.GetOccupyingToken());
            }
            else 
            {
              // Debug.Log($"No attacker on the ball or around it. Please select an attacker.");
            }
        }
    }

    private void CalculateDefendersThatNeedToMove()
      {
          shouldDefMoveTokens.Clear();
          HexCell ballHex = ball.GetCurrentHex();
          List<HexCell> nearbyHexes = HexGrid.GetHexesInRange(hexGrid, ballHex, 2);
          foreach (HexCell hex in nearbyHexes)
          {
              if (hex.isDefenseOccupied)
              {
                  shouldDefMoveTokens.Add(hex.GetOccupyingToken());
              }
          }
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
                    Debug.LogError("Target Hex is null!");
                }
            }
        }
        isWaitingForKickerSelection = false;
        hexGrid.ClearHighlightedHexes();
        Debug.Log("HandleKicker Selection: Calculating potential kickers...");
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
            .OrderBy(hex => HexGridUtils.GetHexDistance(hex.coordinates, new Vector3Int(0, 0, 0)))
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
                selectedKicker = token;
                AdvanceToNextPhase(matchManager.currentState);
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
            HexGridUtils.GetHexDistance(hex.coordinates, ball.GetCurrentHex().coordinates) <= 2)
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
                    matchManager.currentState = MatchManager.GameState.FreeKickDefineKicker;
                    ResetMoves();
                }
                break;
            case MatchManager.GameState.FreeKickAttMovement3:
                matchManager.currentState = MatchManager.GameState.FreeKickDefMovement3;
                StartCoroutine(HandleSetupPhase(MatchManager.GameState.FreeKickDefMovement3, 1));
                break;
            case MatchManager.GameState.FreeKickDefMovement3:
                matchManager.currentState = MatchManager.GameState.FreeKickDefineKicker;
                ResetMoves();
                isWaitingforMovement3 = false;
                isWaitingForSetupPhase = false;
                if (potentialKickers.Count > 1)
                {
                    Debug.Log($"Multiple potential kickers available. Select one from the {potentialKickers.Count} near the ball!");
                    isWaitingForFinalKickerSelection = true;
                }
                else
                {
                    selectedKicker = potentialKickers.FirstOrDefault();
                    Debug.Log($"{selectedKicker.name} selected as the kicker!");
                    MatchManager.Instance.SetLastToken(selectedKicker);
                    AdvanceToNextPhase(MatchManager.GameState.FreeKickDefineKicker);
                }
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
                        sb.Append($"Click another Attacking Token to move, or Click on a Hex to move {selectedToken.name} to, or Press [X] to this sequence part, ");
                    }
                    else
                    {
                        sb.Append("Click on an Attacking Token to move or Press [X] to this sequence part, ");
                    }
                    break;
                case MatchManager.GameState.FreeKickDef1:
                case MatchManager.GameState.FreeKickDef2:
                case MatchManager.GameState.FreeKickDef3:
                    if (selectedToken != null)
                    {
                        sb.Append($"Click another Defending Token to move, or Click on a Hex to move {selectedToken.name} to, or Press [X] to this sequence part, ");
                    }
                    else
                    {
                        sb.Append("Click on a Defending Token to move or Press [X] to this sequence part, ");
                    }
                    break;
            }
        }
        if (isWaitingforMovement3) sb.Append("isWaitingforMovement3, ");
        if (isWaitingForFinalKickerSelection) sb.Append("isWaitingForFinalKickerSelection, ");
        if (isWaitingForExecution) sb.Append("Short Standard [P]ass (6 Hexes), [C]ross (High Pass) up to 15 Hexes away or in the box on an attacker, no More movements, ");


        if (sb.Length >= 2 && sb[^2] == ',') sb.Length -= 2; // Trim trailing comma
        return sb.ToString();
    }

}
