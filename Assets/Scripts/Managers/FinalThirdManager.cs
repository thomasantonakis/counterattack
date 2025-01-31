using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine.Rendering;
using Unity.VisualScripting;
public class FinalThirdManager : MonoBehaviour
{
    [Header("Dependencies")]
    public Ball ball;
    public HexGrid hexGrid;
    public PlayerTokenManager playerTokenManager;
    public MovementPhaseManager movementPhaseManager;
    [Header("Flags")]
    public bool bothSides = false;
    public bool isFinalThirdPhaseActive = false;
    [SerializeField]
    private bool isWaitingForTokenSelection = false;
    [SerializeField]
    private bool isWaitingForTargetHex = false;
    [Header("Runtime Items")]
    [SerializeField]
    private List<PlayerToken> eligibleTokens;
    [SerializeField]
    private List<PlayerToken> currentMovableTokens;
    [SerializeField]
    private List<PlayerToken> movedTokens;
    [SerializeField]
    private string currentTeamMoving; // attack, defense
    [SerializeField]
    private PlayerToken selectedToken;
    [SerializeField]
    private HexCell ballHex;

    public void TriggerFinalThirdPhase(bool bothSides = false)
    {
        Debug.Log("Hello from FinalThird Manager!");
        ballHex = ball.GetCurrentHex();
        this.bothSides = bothSides;
        int f3Side = ballHex.isInFinalThird; // 1 = Right F3, -1 = Left F3, 0 = No F3
        if (f3Side == 0)  return; // No F3 triggered
        eligibleTokens = GetAllTokens(f3Side);
        if (eligibleTokens.Count == 0) return; // No Eligible Tokens

        Debug.Log($"We should play final thirds on the {-f3Side}");
        isFinalThirdPhaseActive = true;
        movedTokens = new List<PlayerToken>();
        currentTeamMoving = "attack";
        if (bothSides)
        {
            StartCoroutine(HandleBothSidesF3(f3Side));
        }
        else
        {
            StartCoroutine(HandleF3Movement());
        }
    }

    private List<PlayerToken> GetAllTokens(int f3Side)
    {
        List<PlayerToken> initList = playerTokenManager.allTokens
            .Where(token => token.GetCurrentHex().isInFinalThird == -f3Side)  // Opposite F3
            .ToList();
        // TODO: Check if we can move stunned or Jumped Tokens.
        return initList;
    }
    private List<PlayerToken> GetCurrentTeamTokens()
    {
        if (currentTeamMoving == "attack")
            return eligibleTokens.Where(token => token.isAttacker).ToList();
        else
            return eligibleTokens.Where(token => !token.isAttacker).ToList();
    }

    public IEnumerator HandleMouseInput(PlayerToken inputToken, HexCell inputCell)
    {
        Debug.Log($"Hello from finalThirdManager.HandleMouseInput");
        if (
            inputToken != null // Clicked on a Token
            && (
                isWaitingForTokenSelection // Waiting for a Token to select
                // OR we had selected and thus we are waiting for a Hex
                // but we clicked a token that is not the selected
                || (selectedToken != inputToken && isWaitingForTargetHex)
            )
        )
        {
            Debug.Log($"Passing {inputToken} to HandleTokenSelectionForF3");
            HandleTokenSelectionForF3(inputToken);
        }
        if (
            isWaitingForTargetHex // We already have clicked a token
            && selectedToken != null // and we selected it
            && inputToken == null // and we now DID NOT click on a token
            && inputCell != null // We clicked on a Hex
        )
        {
            Debug.Log($"Passing {inputCell.name} to ConfirmTokenMove");
            yield return StartCoroutine(ConfirmTokenMove(inputCell));
        }
    }

    private IEnumerator HandleF3Movement()
    {
        isWaitingForTokenSelection = true;
        currentMovableTokens = GetCurrentTeamTokens();
        Debug.Log($"Final Third Moves - {currentTeamMoving} Team Moving"); // TODO: Make this more informative.
        while (currentMovableTokens.Count > 0)
        {
            isWaitingForTargetHex = false;
            isWaitingForTokenSelection = true;
            yield return new WaitUntil(() => selectedToken != null);
            isWaitingForTargetHex = true;
            isWaitingForTokenSelection = false;
            yield return new WaitUntil(() => !isWaitingForTargetHex);
        }
        Debug.Log($"{currentTeamMoving} Final Third Movement Phase Done.");
        StartCoroutine(NextF3Phase());
    }

    public void HandleTokenSelectionForF3(PlayerToken token)
    {
        hexGrid.ClearHighlightedHexes();
        if (token == null)
        {
            Debug.Log("This is NOT a Token");
            selectedToken = null; 
            isWaitingForTargetHex = false;
            isWaitingForTokenSelection = true;
            return;
        }
        if (currentMovableTokens == null)
        {
            Debug.LogError("currentMovableTokens is null! Has HandleF3Movement() started?");
            selectedToken = null; 
            isWaitingForTargetHex = false;
            isWaitingForTokenSelection = true;
            return;
        }
        if (!eligibleTokens.Contains(token))
        {
            Debug.Log("This is not an eligible Player"); // TODO, check which Token was clicked and provide relevant Message
            selectedToken = null; 
            isWaitingForTargetHex = false;
            isWaitingForTokenSelection = true;
            return;
        }
        if (!token.isAttacker && currentTeamMoving == "attack")
        {
            Debug.Log($"{token.name} is a defender and we are currently in the Attacking part of the F3 Move");
            selectedToken = null; 
            isWaitingForTargetHex = false;
            isWaitingForTokenSelection = true;
            return;            
        }
        if (token.isAttacker && currentTeamMoving != "attack")
        {
            Debug.Log($"{token.name} is an attacker and we are currently in the Defensive part of the F3 Move");
            selectedToken = null; 
            isWaitingForTargetHex = false;
            isWaitingForTokenSelection = true;
            return;            
        }
        if (movedTokens.Contains(token))
        {
            Debug.Log("This Token has already moved in current F3 Move");
            selectedToken = null;
            isWaitingForTargetHex = false;
            isWaitingForTokenSelection = true;
            return;
        }
        selectedToken = token;
        // Highlight valid movement hexes (6 hexes)
        movementPhaseManager.HighlightValidMovementHexes(selectedToken, 6);
        Debug.Log($"{token.name} selected for F3 move.");
        isWaitingForTokenSelection = false;
        isWaitingForTargetHex = true;

    }

    public IEnumerator ConfirmTokenMove(HexCell targetHex)
    {
        if (!hexGrid.highlightedHexes.Contains(targetHex))
        {
            Debug.LogWarning("Invalid move! Selected hex is not in the highlighted movement options.");
            yield break;
        }
        List<HexCell> gkZoi = ballHex.GetNeighbors(hexGrid).ToList();
        if (bothSides && gkZoi.Contains(targetHex) && currentTeamMoving != "attack")
        {
            Debug.LogWarning("Invalid move! You cannot land on the Ball's ZOI as it is held by the  attacking GK.");
            yield break;
        }
        // TODO: Check if the clicked token is one of hghlighted reachable Hexes
        isWaitingForTargetHex = false;
        // Prevent duplicate movement**
        PlayerToken movingToken = selectedToken;
        movedTokens.Add(selectedToken);
        // eligibleTokens.Remove(selectedToken);
        currentMovableTokens.Remove(selectedToken);
        selectedToken = null;
        yield return StartCoroutine(movementPhaseManager.MoveTokenToHex(targetHex, movingToken, false));
    }

    private IEnumerator NextF3Phase()
    {
        Debug.Log("Hello from Nextf3");
        if (currentTeamMoving == "attack") // Attack finished, now defense
        {
            currentTeamMoving = "defense";
            StartCoroutine(HandleF3Movement());
        }
        else
        {
            EndF3Phase();
        }
        yield break;
    }

    private void EndF3Phase()
    {
        isFinalThirdPhaseActive = false;
        eligibleTokens.Clear();
        currentMovableTokens.Clear();
        movedTokens.Clear();
        currentTeamMoving = null;
        isWaitingForTokenSelection = false;
        ballHex = null;
        Debug.Log("Final Third Phase Completed. Resuming gameplay.");
    }

    public void ForfeitTurn()
    {
        hexGrid.ClearHighlightedHexes();
        Debug.Log("Forfeiting Current F3 Moves.");
        isWaitingForTargetHex = false; // Avoid soft-locks
        isWaitingForTokenSelection = false; // Avoid soft-locks
        selectedToken = null;  // Clear selected token
        // Add remaining available tokens to the already moved ones.
        movedTokens.AddRange(currentMovableTokens);
        currentMovableTokens.Clear();
        // NextF3Phase();
        StartCoroutine(NextF3Phase());

    }

    private IEnumerator HandleBothSidesF3(int f3Side)
    {
        eligibleTokens = GetAllTokens(-f3Side); // Ball Side
        if (eligibleTokens.Count == 0) yield break; // No Eligible Tokens

        isFinalThirdPhaseActive = true;
        movedTokens = new List<PlayerToken>();
        currentTeamMoving = "attack";
        yield return StartCoroutine(HandleF3Movement());

        eligibleTokens = GetAllTokens(f3Side); // Other side than ball's side
        if (eligibleTokens.Count == 0) yield break; // No Eligible Tokens

        currentTeamMoving = "attack";
        yield return StartCoroutine(HandleF3Movement());
    }
}
