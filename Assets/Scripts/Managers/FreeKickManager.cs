using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FreeKickManager : MonoBehaviour
{
    public HexGrid hexGrid;
    public Ball ball;
    public MatchManager matchManager;
    [SerializeField]
    private List<PlayerToken> shouldDefMoveTokens = new List<PlayerToken>();
    [SerializeField]
    private List<PlayerToken> potentialKickers = new List<PlayerToken>();
    [SerializeField]
    private int movesUsed;
    public int remainingDefenderMoves;
    public int attackerMovesUsed;
    public int defenderMovesUsed;
    public bool isWaitingForKickerSelection = false;
    public bool isWaitingForSetupPhase = false;
    private PlayerToken selectedKicker;
    public PlayerToken selectedToken;
    public HexCell targetHex;

    public void StartFreeKickPreparation()
    {
        Debug.Log("Starting Free Kick Preparation...");
        matchManager.currentState = MatchManager.GameState.FreeKickKickerSelect;
        isWaitingForKickerSelection = true;
        remainingDefenderMoves = 6;
        attackerMovesUsed = 0;
        defenderMovesUsed = 0;
        CalculateDefendersThatNeedToMove();
    }

    public void CalculatePotentialKickers()
    {
        potentialKickers.Clear();
        // Debug.Log("Calculating potential kickers...");
        HexCell ballHex = ball.GetCurrentHex();
        List<HexCell> nearbyHexes = HexGrid.GetHexesInRange(hexGrid, ballHex, 1);
        foreach (HexCell hex in nearbyHexes)
        {
            Debug.Log($"Checking hex {hex.coordinates} for potential kickers. Is attack occupied? {hex.isAttackOccupied} By which Token? {hex.GetOccupyingToken()?.name}");
            if (hex.isAttackOccupied)
            {
                Debug.Log($"Attacker found on the ball or around it: {hex.GetOccupyingToken().name}");
                potentialKickers.Add(hex.GetOccupyingToken());
            }
            else 
            {
              Debug.Log($"No attacker on the ball or around it. Please select an attacker.");
            }
        }
    }

    public void CalculateDefendersThatNeedToMove()
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
    
    public IEnumerator HandleKickerSelection(PlayerToken clickedToken = null)
    {
        // If a token was pre-selected (e.g., passed from GIM), handle it immediately
        Debug.Log($"Handling kicker selection for {clickedToken?.name}...");
        if (clickedToken != null)
        {
            Debug.Log($"Selected {clickedToken.name} as the kicker.");
            selectedKicker = clickedToken;
            HexCell targetHex = GetClosestAvailableHexToBall();
            if (targetHex != null)
            {
                yield return StartCoroutine(MoveTokenToHex(clickedToken, targetHex));
            }
        }
        isWaitingForKickerSelection = false;
        hexGrid.ClearHighlightedHexes();
        Debug.Log("HandleKicker Selection: Calculating potential kickers...");
        CalculatePotentialKickers();
        // Transition to the first phase
        StartCoroutine(HandleSetupPhase(MatchManager.GameState.FreeKickAtt1, 2));
        yield break;  // Exit early since we already handled the token            
    }

    private HexCell GetClosestAvailableHexToBall()
    {
        HexCell ballHex = ball.GetCurrentHex();
        HexCell[] neighbors = ballHex.GetNeighbors(hexGrid);

        return neighbors
            .Where(hex => !hex.isDefenseOccupied && !hex.isAttackOccupied)
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

    public void HandleSetupTokenSelection(PlayerToken token)
    {
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
        if
        (
            matchManager.currentState.ToString().StartsWith("FreeKickDef") // During DefX state
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
        // Clear targetHex if switching tokens mid-selection
        targetHex = null;
        selectedToken = token;
        Debug.Log($"Token {token.name} selected for current phase {MatchManager.Instance.currentState}. Awaiting destination hex.");
    }

    public IEnumerator HandleSetupHexSelection(HexCell hex)
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
            HexGridUtils.GetHexDistance(hex.coordinates, ball.GetCurrentHex().coordinates) <= 2)
        {
            Debug.LogWarning($"Hex {hex.coordinates} is too close to the ball. Choose another destination.");
            yield break;
        }
        targetHex = hex;
        Debug.Log($"Token {selectedToken.name} moving to hex {hex.coordinates}.");
        yield return StartCoroutine(MoveTokenToHex(selectedToken, hex));
        if (MatchManager.Instance.currentState.ToString().StartsWith("FreeKickDef"))
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
            attackerMovesUsed++;
        }
        // Clear selections for next move
        selectedToken = null;
        targetHex = null;
        movesUsed++;
        Debug.Log($"Move {movesUsed} just performed");
    }

    // private bool IsValidTokenForPhase(MatchManager.GameState phaseState, PlayerToken token)
    // {
    //     if (phaseState.ToString().StartsWith("FreeKickAtt"))
    //         return token.isAttacker;
    //     else if (phaseState.ToString().StartsWith("FreeKickDef"))
    //     {
    //         if (shouldDefMoveTokens.Contains(token))
    //             return true;
    //         if (!IsTokenViolatingDefenseRule(token))
    //             return true;
    //     }
    //     return false;
    // }

    // private HexCell GetValidTargetHexForToken(PlayerToken token)
    // {
    //     HexCell currentHex = token.GetCurrentHex();
    //     if (currentHex == null)
    //     {
    //         Debug.LogError($"Token {token.name} does not have a valid hex!");
    //         return null;
    //     }
    //     // Get all hexes within a reasonable range (e.g., the entire grid or a specific range)
    //     List<HexCell> reachableHexes = HexGrid.GetHexesInRange(hexGrid, currentHex, 48); // Adjust range as needed

    //     return reachableHexes
    //         .Where(hex => !hex.isDefenseOccupied && !hex.isAttackOccupied)
    //         .OrderBy(hex => HexGridUtils.GetHexDistance(currentHex.coordinates, hex.coordinates))
    //         .FirstOrDefault();
    //     }

    // private bool IsTokenViolatingDefenseRule(PlayerToken token)
    // {
    //     // Check if the token is null or not a defender
    //     if (token == null || token.isAttacker)
    //     {
    //         Debug.LogWarning($"Token {token?.name} is not a defender or is null. Ignoring for defense rule check.");
    //         return false;
    //     }

    //     HexCell tokenHex = token.GetCurrentHex();

    //     if (tokenHex == null)
    //     {
    //         Debug.LogError($"Token {token.name} does not occupy a valid hex!");
    //         return false;
    //     }
    //     // Check if the token is within 2 hexes of the ball
    //     HexCell ballHex = ball.GetCurrentHex();
    //     int distanceFromBall = HexGridUtils.GetHexDistance(tokenHex.coordinates, ballHex.coordinates);

    //     if (distanceFromBall <= 2)
    //     {
    //         Debug.Log($"Token {token.name} is within 2 hexes of the ball. Violating defense rule.");
    //         return true;
    //     }

    //     return false;
    // }

    public void AdvanceToNextPhase(MatchManager.GameState currentPhase)
    {
        Debug.Log($"Advancing from {currentPhase} phase.");

        switch (currentPhase)
        {
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
                StartCoroutine(HandleSetupPhase(MatchManager.GameState.FreeKickAtt3, 3));
                break;
            case MatchManager.GameState.FreeKickAtt3:
                MatchManager.Instance.currentState = MatchManager.GameState.FreeKickDef3;
                StartCoroutine(HandleSetupPhase(MatchManager.GameState.FreeKickDef3, 2));
                break;
            case MatchManager.GameState.FreeKickDef3:
                matchManager.currentState = MatchManager.GameState.FreeKickExecution;
                ResetMoves();
                Debug.Log("Free Kick Preparation completed. Ready for execution.");
                break;
        }
    }

    private void ResetMoves()
    {
        remainingDefenderMoves = 6;
        attackerMovesUsed = 0;
        defenderMovesUsed = 0;
        selectedToken = null;
        targetHex = null;
        shouldDefMoveTokens.Clear();
        potentialKickers.Clear();
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
        yield return null;
    }
}
