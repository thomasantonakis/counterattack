using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine.Rendering;
using Unity.VisualScripting;

public class FinalThirdManager : MonoBehaviour
{
    [Header("Dependencies")]
    public Ball ball;
    public HexGrid hexGrid;
    public PlayerTokenManager playerTokenManager;
    public MovementPhaseManager movementPhaseManager;
    public HighPassManager highPassManager;
    public HeaderManager headerManager;
    [Header("Flags")]
    public bool bothSides = false;
    public bool isActivated = false;
    [SerializeField]
    private string currentTeamMoving = null; // attack, defense
    [SerializeField]
    private PlayerToken selectedToken;
    [SerializeField]
    private bool isWaitingForTokenSelection = false;
    [SerializeField]
    private bool isWaitingForTargetHex = false;
    public bool forfeitWasPressed = false;
    public bool isWaitingForWhatToDo = false;
    [SerializeField]
    private bool thisIsTheSecond = false;
    [Header("Runtime Items")]
    [SerializeField]
    private List<PlayerToken> eligibleTokens;
    [SerializeField]
    private List<PlayerToken> currentMovableTokens;
    [SerializeField]
    private List<PlayerToken> movedTokens;
    [SerializeField]

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
        if (isActivated)
        {
            StartCoroutine(HandleMouseInput(token, hex));
        }
    }

    private void OnKeyReceived(KeyPressData keyData)
    {
        if (isActivated)
        {
            if (keyData.key == KeyCode.X)
            {
                keyData.isConsumed = true;
                ForfeitTurn();
            }
            if (isWaitingForWhatToDo && keyData.key == KeyCode.D)
            {
                DropBall();
            }
            if (isWaitingForWhatToDo && keyData.key == KeyCode.K)
            {
                GKKick();
            }
        }
    }


    public void TriggerFinalThirdPhase(bool bothSides = false)
    {
        isActivated = true;
        this.bothSides = bothSides;
        int f3Side = ball.GetCurrentHex().isInFinalThird; // 1 = Right F3, -1 = Left F3, 0 = No F3
        if (f3Side == 0)
        {
            isActivated = false;
            return; // No F3 triggered
        }

        if (bothSides) eligibleTokens = GetAllTokens(-f3Side);
        else eligibleTokens = GetAllTokens(f3Side);
        
        if (eligibleTokens.Count == 0)
        {
            isActivated = false;
            Debug.Log("No Tokens in the Final Third! Skipping!");
            return; // No Eligible Tokens
        }
        Debug.Log("Hello from FinalThird Manager!");
        movedTokens = new List<PlayerToken>();
        currentTeamMoving = "attack";
        StartCoroutine(HandleF3Movement());
    }

    private List<PlayerToken> GetAllTokens(int f3Side)
    {
        List<PlayerToken> initList = playerTokenManager.allTokens
            .Where(token => token.GetCurrentHex().isInFinalThird == -f3Side)  // Opposite F3
            .Where(token => !movementPhaseManager.stunnedTokens.Contains(token))
            .Where(token => !headerManager.attackerWillJump.Contains(token))
            .Where(token => !headerManager.defenderWillJump.Contains(token))
            .ToList();
        return initList;
    }
    
    private List<PlayerToken> GetCurrentTeamTokens()
    {
        // Debug.Log($"hello from GetCurrentTeamTokens, currentTeamMoving: {currentTeamMoving}");;
        if (currentTeamMoving == "attack")
            return eligibleTokens.Where(token => token.isAttacker).ToList();
        else
            return eligibleTokens.Where(token => !token.isAttacker).ToList();
    }

    public IEnumerator  HandleMouseInput(PlayerToken inputToken, HexCell inputCell)
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
        Debug.Log($"Hello from HandleF3Movement, currentTeamMoving: {currentTeamMoving}");
        isWaitingForTokenSelection = true;
        currentMovableTokens = GetCurrentTeamTokens();

        if (currentMovableTokens.Count == 0)  // <- Add this check
        {
            Debug.Log($"No movable tokens for {currentTeamMoving}. Skipping...");
            StartCoroutine(NextF3Phase());
            yield break;
        }
        forfeitWasPressed = false; // ✅ Allow listening for forfeit
        // // TODO: Make this more informative.
        // Debug.Log($"Final Third Moves - {currentTeamMoving} Team Moving, currentMovableTokens has {currentMovableTokens.Count} items");
        while (currentMovableTokens.Count > 0)
        {
            isWaitingForTargetHex = false;
            isWaitingForTokenSelection = true;
            Debug.Log($"Running F3");
            yield return new WaitUntil(() => selectedToken != null || forfeitWasPressed);
            // ✅ Exit immediately if forfeit is triggered
            if (forfeitWasPressed)
            {
                Debug.Log("Forfeit detected during F3 movement. Exiting movement phase.");
                break;
            }
            Debug.Log($"Selected token is now not null: {selectedToken}");
            isWaitingForTargetHex = true;
            isWaitingForTokenSelection = false;
            yield return new WaitUntil(() => !isWaitingForTargetHex || forfeitWasPressed);
            // ✅ Exit immediately if forfeit is triggered
            if (forfeitWasPressed)
            {
                Debug.Log("Forfeit detected while waiting for target hex. Exiting movement phase.");
                break;
            }
            Debug.Log($"isWaitingForTargetHex is now false");
        }
        forfeitWasPressed = false; // ✅ Reset flag after movement phase
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
            // TODO, check which Token was clicked and provide relevant Message
            Debug.Log("This is not an eligible Player");
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
        if (targetHex.isInPenaltyBox == 0 && selectedToken == ball.GetCurrentHex().GetOccupyingToken())
        {
            Debug.LogWarning("It would be best if the GoalKeeper does not walk out of the box with the ball in their hands to avoid a RED CARD!");
            yield break;
        }
        List<HexCell> gkZoi = ball.GetCurrentHex().GetNeighbors(hexGrid).ToList();
        if (bothSides && gkZoi.Contains(targetHex) && currentTeamMoving != "attack")
        {
            Debug.LogWarning("Invalid move! You cannot land on the Ball's ZOI as it is held by the  attacking GK.");
            yield break;
        }
        isWaitingForTargetHex = false;
        // Prevent duplicate movement
        PlayerToken movingToken = selectedToken;
        movedTokens.Add(selectedToken);
        // eligibleTokens.Remove(selectedToken);
        currentMovableTokens.Remove(selectedToken);
        selectedToken = null;
        yield return StartCoroutine(movementPhaseManager.MoveTokenToHex(targetHex, movingToken, false));
    }

    private IEnumerator NextF3Phase()
    {
        forfeitWasPressed = false;
        Debug.Log($"Hello from Nextf3, currentTeamMoving: {currentTeamMoving}");
        if (currentTeamMoving == "attack")
        {
            currentTeamMoving = "defense";
            // Debug.Log($"Starting HandleF3Movement, with currentTeamMoving: {currentTeamMoving}");
            StartCoroutine(HandleF3Movement());
        }
        else // defense just ended
        {
            if (bothSides)
            {
                Debug.Log("First F3 phase finished, waiting for second...");
                thisIsTheSecond = true;
                TriggerFinalThirdPhase(); // without both sides
            }
            else 
            {
                EndF3Phase();
                if (thisIsTheSecond)
                {
                    isWaitingForWhatToDo = true;
                    isActivated = true;
                    Debug.Log($"GK has to decide what to do: [D]rop the ball and play on? OR Play the GK [Kick] as a High pass enywhere except the opposite Final Third?");
                }
            }
        }
        yield break;
    }

    private void EndF3Phase()
    {
        forfeitWasPressed = false;
        eligibleTokens.Clear();
        currentMovableTokens.Clear();
        movedTokens.Clear();
        currentTeamMoving = null;
        Debug.Log("Final Third Phase Completed. Resuming gameplay.");
        isWaitingForTokenSelection = false;
        isActivated = false;
    }

    public void ForfeitTurn()
    {
        // yield return null;
        forfeitWasPressed = true;
        hexGrid.ClearHighlightedHexes();
        Debug.Log("Forfeiting Current F3 Moves.");
        isWaitingForTargetHex = false; // Avoid soft-locks
        isWaitingForTokenSelection = false; // Avoid soft-locks
        selectedToken = null;  // Clear selected token
        // Add remaining available tokens to the already moved ones.
        movedTokens.AddRange(currentMovableTokens);
        currentMovableTokens.Clear();
    }

    public void DropBall()
    {
        isWaitingForWhatToDo = false;
        isActivated = false;
        thisIsTheSecond = false;
        MatchManager.Instance.currentState = MatchManager.GameState.SuccessfulTackle; // Check this
        string gkWithBall = ball.GetCurrentHex().GetOccupyingToken().name;
        Debug.Log($"{gkWithBall} drops the ball at feet. Available things to do: Standard [P]ass / [M]ovement Phase / [C] High Pass / [L]ong Ball.");
    }
    
    public void GKKick()
    {
        isWaitingForWhatToDo = false;
        isActivated = false;
        thisIsTheSecond = false;
        MatchManager.Instance.currentState = MatchManager.GameState.GoalKick;
        string gkWithBall = ball.GetCurrentHex().GetOccupyingToken().name;
        Debug.Log($"{gkWithBall} will take a Gk High pass, Please click on any hex except from the oposite Final Third to target.");
    }

    private string GetTeamNameByCurrentTeamMoving()
    {
        if (MatchManager.Instance == null)
        {
            Debug.LogWarning("MatchManager instance is null!");
            return "Unknown Team";
        }

        if (currentTeamMoving == "attack")
        {
            // If we are in "attack" mode, get the attacking team's name
            if (MatchManager.Instance.teamInAttack == MatchManager.TeamInAttack.Home)
            {
                return MatchManager.Instance.gameData.gameSettings.homeTeamName;
            }
            else if (MatchManager.Instance.teamInAttack == MatchManager.TeamInAttack.Away)
            {
                return MatchManager.Instance.gameData.gameSettings.awayTeamName;
            }
        }
        else if (currentTeamMoving == "defense")
        {
            // If we are in "defense" mode, get the defending team's name
            if (MatchManager.Instance.teamInAttack == MatchManager.TeamInAttack.Home)
            {
                return MatchManager.Instance.gameData.gameSettings.awayTeamName;
            }
            else if (MatchManager.Instance.teamInAttack == MatchManager.TeamInAttack.Away)
            {
                return MatchManager.Instance.gameData.gameSettings.homeTeamName;
            }
        }
        
        Debug.LogWarning($"Unknown currentTeamMoving value: {currentTeamMoving}");
        return "Unknown Team";
    }

    public string GetDebugStatus()
    {
        StringBuilder sb = new();
        sb.Append("F3: ");

        if (isActivated) sb.Append("isActivated, ");
        if (bothSides) sb.Append("bothSides, ");
        if (isWaitingForTokenSelection) sb.Append("isWaitingForTokenSelection, ");
        if (isWaitingForTargetHex) sb.Append("isWaitingForTargetHex, ");
        if (isWaitingForWhatToDo) sb.Append("isWaitingForWhatToDo, ");
        if (thisIsTheSecond) sb.Append("thisIsTheSecond, ");
        if (!string.IsNullOrEmpty(currentTeamMoving) && (currentTeamMoving == "attack" || currentTeamMoving == "defense")) sb.Append($"currentTeamMoving: {GetTeamNameByCurrentTeamMoving()}, ");
        if (selectedToken != null) sb.Append($"selectedToken: {selectedToken.name}, ");
        
        if (sb.Length >= 2 && sb[^2] == ',') sb.Length -= 2; // Trim trailing comma
        return sb.ToString();
    }

    public string GetInstructions()
    {
        StringBuilder sb = new();
        if (isActivated)
        {
            sb.Append("F3: ");
            if (bothSides)
            {
                sb.Append("Both Sides will be played, ");
                if (!thisIsTheSecond)
                {
                    sb.Append("current side first: ");
                }
                else
                {
                    sb.Append("opposite side now: ");
                }
            }
        }
        if (isWaitingForTokenSelection) sb.Append($"Click on a Token from {GetTeamNameByCurrentTeamMoving()}, to select for movement, ");
        if (isWaitingForTargetHex) sb.Append($" or Click on a Hex to move {selectedToken.name} there, ");
        if (isActivated) sb.Append($"Press [X] to Forfeit {GetTeamNameByCurrentTeamMoving()}'s current F3 Move, ");
        if (isWaitingForWhatToDo) sb.Append($"Press [D] to Drop the ball, or [K] to take a Goal Kick, ");
        
        if (sb.Length >= 2 && sb[^2] == ',') sb.Length -= 2; // Safely trim trailing comma + space
        return sb.ToString();
    }

}
