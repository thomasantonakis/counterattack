using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Linq;

public class FinalThirdManager : MonoBehaviour
{
    private enum FinalThirdContext
    {
        None,
        SaveAndHoldK,
        OobGoalKick
    }

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
    [SerializeField]
    private bool isMovingToken = false;
    public bool forfeitWasPressed = false;
    public bool isWaitingForWhatToDo = false;
    public bool isWaitingForSetPieceGoalKickChoice = false;
    [SerializeField]
    private bool useThomasDropBallAutoMovementRule = false;
    [SerializeField]
    private bool thisIsTheSecond = false;
    [SerializeField]
    private FinalThirdContext finalThirdContext = FinalThirdContext.None;
    [SerializeField]
    private HexCell setPieceGoalKickSpot = null;
    [SerializeField]
    private PlayerToken setPieceGoalKickGoalkeeper = null;
    [Header("Runtime Items")]
    [SerializeField]
    private List<PlayerToken> eligibleTokens;
    [SerializeField]
    private List<PlayerToken> currentMovableTokens;
    [SerializeField]
    private List<PlayerToken> movedTokens;

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
        if (keyData.isConsumed) return;
        if (isActivated)
        {
            if (!isWaitingForWhatToDo && keyData.key == KeyCode.X)
            {
                if (isMovingToken)
                {
                    return;
                }

                keyData.isConsumed = true;
                ForfeitTurn();
            }
            if (isWaitingForWhatToDo && !isWaitingForSetPieceGoalKickChoice && keyData.key == KeyCode.D)
            {
                keyData.isConsumed = true;
                DropBall();
            }
            else if (isWaitingForWhatToDo && !isWaitingForSetPieceGoalKickChoice && keyData.key == KeyCode.K)
            {
                keyData.isConsumed = true;
                GKKick();
            }
        }
    }


    public void TriggerFinalThirdPhase(bool bothSides = false)
    {
        ClearSpecialFinalThirdContext();
        TriggerFinalThirdPhaseInternal(bothSides);
    }

    public void TriggerSaveAndHoldFinalThirds()
    {
        ClearSpecialFinalThirdContext();
        finalThirdContext = FinalThirdContext.SaveAndHoldK;
        TriggerFinalThirdPhaseInternal(true);
    }

    public void TriggerOobGoalKickFinalThirds(HexCell goalKickSpot, PlayerToken goalkeeper)
    {
        ClearSpecialFinalThirdContext();
        finalThirdContext = FinalThirdContext.OobGoalKick;
        setPieceGoalKickSpot = goalKickSpot;
        setPieceGoalKickGoalkeeper = goalkeeper;
        StartCoroutine(TriggerOobGoalKickFinalThirdsAfterGoalkeeperReady());
    }

    private IEnumerator TriggerOobGoalKickFinalThirdsAfterGoalkeeperReady()
    {
        if (!IsOobGoalKickSpotBlockedByNonGoalkeeper())
        {
            yield return StartCoroutine(MoveGoalkeeperToGoalKickSpotIfNeeded());
        }

        TriggerFinalThirdPhaseInternal(true);
    }

    private void TriggerFinalThirdPhaseInternal(bool bothSides = false)
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
            if (IsFirstOobGoalKickFinalThird() && !IsOobGoalKickSpotBlockedByNonGoalkeeper())
            {
                StartCoroutine(MoveGoalkeeperAndStartOppositeFinalThird());
                return;
            }

            if (thisIsTheSecond)
            {
                if (IsSaveAndHoldKContext() || IsOobGoalKickContext())
                {
                    Debug.Log("No Tokens in the opposite Final Third. Moving to the goalkeeper post-F3 decision.");
                    EnterGoalkeeperDecisionAfterDoubleFinalThirds();
                }
                else
                {
                    Debug.Log("No Tokens in the opposite Final Third. Ending generic Final Third phase.");
                    EndF3Phase();
                }
                return;
            }

            isActivated = false;
            Debug.Log("No Tokens in the Final Third! Skipping!");
            return; // No Eligible Tokens
        }
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

        if (IsFirstOobGoalKickFinalThird())
        {
            PlayerToken spotOccupant = setPieceGoalKickSpot != null ? setPieceGoalKickSpot.GetOccupyingToken() : null;
            if (spotOccupant != null
                && spotOccupant != setPieceGoalKickGoalkeeper
                && spotOccupant.GetCurrentHex() != null
                && spotOccupant.GetCurrentHex().isInFinalThird == -f3Side
                && !initList.Contains(spotOccupant))
            {
                initList.Add(spotOccupant);
            }
        }

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
        if (isMovingToken)
        {
            yield break;
        }

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
            HandleTokenSelectionForF3(inputToken);
        }
        if (
            isWaitingForTargetHex // We already have clicked a token
            && selectedToken != null // and we selected it
            && inputToken == null // and we now DID NOT click on a token
            && inputCell != null // We clicked on a Hex
        )
        {
            yield return StartCoroutine(ConfirmTokenMove(inputCell));
        }
    }

    private IEnumerator HandleF3Movement()
    {
        isWaitingForTokenSelection = true;
        currentMovableTokens = GetCurrentTeamTokens();
        if (IsFirstOobGoalKickFinalThird())
        {
            currentMovableTokens.Remove(setPieceGoalKickGoalkeeper);
        }
        ApplyOobGoalKickSpotBlockerMovableTokenRule();
        ApplyOobGoalKickDefenseClearanceMovableTokenRules();

        if (currentMovableTokens.Count == 0)  // <- Add this check
        {
            if (IsOobGoalKickSpotBlockedByCurrentTeam())
            {
                PlayerToken blocker = setPieceGoalKickSpot.GetOccupyingToken();
                currentMovableTokens.Add(blocker);
                if (!eligibleTokens.Contains(blocker))
                {
                    eligibleTokens.Add(blocker);
                }
            }
        }

        if (currentMovableTokens.Count == 0)
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
            yield return new WaitUntil(() => selectedToken != null || forfeitWasPressed);
            // ✅ Exit immediately if forfeit is triggered
            if (forfeitWasPressed)
            {
                Debug.Log("Forfeit detected during F3 movement. Exiting movement phase.");
                break;
            }
            isWaitingForTargetHex = true;
            isWaitingForTokenSelection = false;
            yield return new WaitUntil(() => (!isWaitingForTargetHex && !isMovingToken) || forfeitWasPressed);
            // ✅ Exit immediately if forfeit is triggered
            if (forfeitWasPressed)
            {
                Debug.Log("Forfeit detected while waiting for target hex. Exiting movement phase.");
                break;
            }
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
        if (token == setPieceGoalKickGoalkeeper && IsFirstOobGoalKickFinalThird())
        {
            Debug.Log("The goal-kick goalkeeper does not need a Final Third move before taking the Goal Kick.");
            selectedToken = null;
            isWaitingForTargetHex = false;
            isWaitingForTokenSelection = true;
            return;
        }
        if (!currentMovableTokens.Contains(token))
        {
            Debug.Log($"{token.name} is not currently movable in this Final Third step.");
            selectedToken = null;
            isWaitingForTargetHex = false;
            isWaitingForTokenSelection = true;
            return;
        }
        selectedToken = token;
        int movementRange = GetF3MovementRangeForToken(selectedToken);
        movementPhaseManager.HighlightValidMovementHexes(selectedToken, movementRange);
        ApplyContextualMovementHighlights(selectedToken);
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
        bool isNonGoalkeeperClearingGoalKickSpot = IsNonGoalkeeperClearingGoalKickSpot(selectedToken);
        if (targetHex.isInPenaltyBox == 0
            && selectedToken == ball.GetCurrentHex().GetOccupyingToken()
            && IsFirstSaveAndHoldKFinalThird()
            && !isNonGoalkeeperClearingGoalKickSpot)
        {
            Debug.LogWarning("It would be best if the GoalKeeper does not walk out of the box with the ball in their hands to avoid a RED CARD!");
            yield break;
        }
        if (IsFirstOobGoalKickFinalThird()
            && targetHex == setPieceGoalKickSpot
            && selectedToken != setPieceGoalKickGoalkeeper)
        {
            Debug.LogWarning("The Goal Kick spot must be left free for the goalkeeper.");
            yield break;
        }
        List<HexCell> gkZoi = ball.GetCurrentHex().GetNeighbors(hexGrid).ToList();
        if (IsSaveAndHoldCurrentSideDefenseTurn() && gkZoi.Contains(targetHex))
        {
            Debug.LogWarning("Invalid move! You cannot land on the Ball's ZOI as it is held by the  attacking GK.");
            yield break;
        }
        PlayerToken movingToken = selectedToken;
        bool startedOnGoalKickSpotAsNonGoalkeeper = IsNonGoalkeeperClearingGoalKickSpot(movingToken);
        bool shouldCarryBall = movingToken == ball.GetCurrentHex()?.GetOccupyingToken()
            && !startedOnGoalKickSpotAsNonGoalkeeper;

        movedTokens.Add(movingToken);
        currentMovableTokens.Remove(movingToken);
        isWaitingForTargetHex = false;
        isMovingToken = true;
        selectedToken = null;
        yield return StartCoroutine(movementPhaseManager.MoveTokenToHex(
            targetHex,
            movingToken,
            isCalledDuringMovement: false,
            shouldCountForDistance: true,
            shouldCarryBall: shouldCarryBall));

        if (startedOnGoalKickSpotAsNonGoalkeeper && !IsOobGoalKickSpotBlockedByNonGoalkeeper())
        {
            yield return StartCoroutine(MoveGoalkeeperToGoalKickSpotIfNeeded());
        }

        isMovingToken = false;
    }

    private IEnumerator NextF3Phase()
    {
        forfeitWasPressed = false;
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
                if (IsFirstOobGoalKickFinalThird())
                {
                    if (HasOobGoalKickDefensiveTokensInPenaltyBox())
                    {
                        Debug.LogWarning("Goal Kick side penalty box still has non-goalkeeper defensive tokens. Continuing defensive clearance before opposite-side Final Thirds.");
                        currentTeamMoving = "defense";
                        StartCoroutine(HandleF3Movement());
                        yield break;
                    }

                    if (IsOobGoalKickSpotBlockedByNonGoalkeeper())
                    {
                        PlayerToken blocker = setPieceGoalKickSpot.GetOccupyingToken();
                        Debug.LogWarning($"{blocker.name} must leave the Goal Kick spot before opposite-side Final Thirds can begin.");
                        currentTeamMoving = blocker.isAttacker ? "attack" : "defense";
                        if (!eligibleTokens.Contains(blocker))
                        {
                            eligibleTokens.Add(blocker);
                        }
                        StartCoroutine(HandleF3Movement());
                        yield break;
                    }

                    yield return StartCoroutine(MoveGoalkeeperToGoalKickSpotIfNeeded());
                }

                Debug.Log("First F3 phase finished, waiting for second...");
                thisIsTheSecond = true;
                TriggerFinalThirdPhaseInternal(); // without both sides
            }
            else 
            {
                if (thisIsTheSecond && (IsSaveAndHoldKContext() || IsOobGoalKickContext())) EnterGoalkeeperDecisionAfterDoubleFinalThirds();
                else EndF3Phase();
            }
        }
        yield break;
    }

    private void EnterGoalkeeperDecisionAfterDoubleFinalThirds()
    {
        bool isOobGoalKickChoice = IsOobGoalKickContext();
        if (isOobGoalKickChoice)
        {
            if (!EnsureSetPieceGoalKickGoalkeeperReady())
            {
                return;
            }

            PlayerToken goalkeeper = setPieceGoalKickGoalkeeper;
            EndF3Phase(preserveGoalKickContext: true);
            isWaitingForWhatToDo = false;
            isWaitingForSetPieceGoalKickChoice = false;
            isActivated = false;
            thisIsTheSecond = false;
            bothSides = false;
            MatchManager.Instance.BroadcastGoalKickRestartOptions(goalkeeper);
            ClearSetPieceGoalKickContext();
            Debug.Log("Goal Kick restart ready: MatchManager is offering [P] Standard Pass or [K] Goalkeeper Kick.");
            return;
        }

        EndF3Phase(preserveGoalKickContext: isOobGoalKickChoice);
        isWaitingForWhatToDo = true;
        isWaitingForSetPieceGoalKickChoice = isOobGoalKickChoice;
        isActivated = true;
        Debug.Log($"GK has to decide what to do: [D]rop the ball and play on? OR Play the GK [Kick] as a High pass enywhere except the opposite Final Third?");
    }

    private void EndF3Phase(bool preserveGoalKickContext = false)
    {
        forfeitWasPressed = false;
        eligibleTokens.Clear();
        currentMovableTokens.Clear();
        movedTokens.Clear();
        currentTeamMoving = null;
        Debug.Log("Final Third Phase Completed. Resuming gameplay.");
        isWaitingForTokenSelection = false;
        isWaitingForTargetHex = false;
        isMovingToken = false;
        isActivated = false;
        if (!preserveGoalKickContext && !bothSides && thisIsTheSecond)
        {
            ClearSpecialFinalThirdContext();
            isWaitingForSetPieceGoalKickChoice = false;
        }
        if (!preserveGoalKickContext)
        {
            thisIsTheSecond = false;
            this.bothSides = false;
        }
    }

    public void ForfeitTurn()
    {
        if (!CanForfeitCurrentF3Move(logWarnings: true))
        {
            return;
        }

        // yield return null;
        forfeitWasPressed = true;
        hexGrid.ClearHighlightedHexes();
        Debug.Log("Forfeiting Current F3 Moves.");
        isWaitingForTargetHex = false; // Avoid soft-locks
        isMovingToken = false;
        isWaitingForTokenSelection = false; // Avoid soft-locks
        selectedToken = null;  // Clear selected token
        // Add remaining available tokens to the already moved ones.
        movedTokens.AddRange(currentMovableTokens);
        currentMovableTokens.Clear();
    }

    private bool CanForfeitCurrentF3Move(bool logWarnings)
    {
        if (IsOobGoalKickSpotBlockedByCurrentTeam())
        {
            if (logWarnings)
            {
                PlayerToken blocker = setPieceGoalKickSpot.GetOccupyingToken();
                Debug.LogWarning($"{blocker.name} must move off the Goal Kick spot before this Final Third move can be forfeited.");
            }
            return false;
        }

        if (IsOobGoalKickDefenseClearanceTurn() && HasOobGoalKickDefensiveTokensInPenaltyBox())
        {
            if (logWarnings)
            {
                Debug.LogWarning("All non-goalkeeper defensive tokens must leave the Goal Kick side penalty box before this Final Third move can be forfeited.");
            }
            return false;
        }

        if (IsSaveAndHoldCurrentSideDefenseTurn() && HasDefenderInBallHoldingGoalkeeperZoi())
        {
            if (logWarnings)
            {
                Debug.LogWarning("Defenders must clear the goalkeeper's zone of influence before this Final Third move can be forfeited.");
            }
            return false;
        }

        return true;
    }

    private bool IsSaveAndHoldKContext()
    {
        return finalThirdContext == FinalThirdContext.SaveAndHoldK;
    }

    private bool IsOobGoalKickContext()
    {
        return finalThirdContext == FinalThirdContext.OobGoalKick;
    }

    private bool IsFirstSaveAndHoldKFinalThird()
    {
        return IsSaveAndHoldKContext()
            && bothSides
            && !thisIsTheSecond;
    }

    private bool IsFirstOobGoalKickFinalThird()
    {
        return IsOobGoalKickContext()
            && bothSides
            && !thisIsTheSecond
            && setPieceGoalKickSpot != null;
    }

    private bool IsSaveAndHoldCurrentSideDefenseTurn()
    {
        return IsFirstSaveAndHoldKFinalThird() && currentTeamMoving == "defense";
    }

    private bool IsOobGoalKickSpotBlockedByNonGoalkeeper()
    {
        if (!IsOobGoalKickContext() || setPieceGoalKickSpot == null)
        {
            return false;
        }

        PlayerToken occupant = setPieceGoalKickSpot.GetOccupyingToken();
        return occupant != null && occupant != setPieceGoalKickGoalkeeper;
    }

    private bool IsOobGoalKickSpotBlockedByCurrentTeam()
    {
        if (!IsOobGoalKickSpotBlockedByNonGoalkeeper())
        {
            return false;
        }

        PlayerToken occupant = setPieceGoalKickSpot.GetOccupyingToken();
        return currentTeamMoving == "attack"
            ? occupant.isAttacker
            : currentTeamMoving == "defense" && !occupant.isAttacker;
    }

    private bool IsNonGoalkeeperClearingGoalKickSpot(PlayerToken token)
    {
        return IsFirstOobGoalKickFinalThird()
            && token != null
            && token != setPieceGoalKickGoalkeeper
            && token.GetCurrentHex() == setPieceGoalKickSpot;
    }

    private bool IsOobGoalKickDefenseClearanceTurn()
    {
        return IsFirstOobGoalKickFinalThird() && currentTeamMoving == "defense";
    }

    private bool IsOobGoalKickCurrentSideDefenderToken(PlayerToken token)
    {
        if (!IsOobGoalKickDefenseClearanceTurn()
            || token == null
            || token == setPieceGoalKickGoalkeeper
            || token.isAttacker
            || setPieceGoalKickSpot == null)
        {
            return false;
        }

        HexCell tokenHex = token.GetCurrentHex();
        return tokenHex != null;
    }

    private bool IsOobGoalKickDefenseClearanceToken(PlayerToken token)
    {
        if (!IsOobGoalKickCurrentSideDefenderToken(token))
        {
            return false;
        }

        HexCell tokenHex = token.GetCurrentHex();
        return tokenHex != null
            && tokenHex.isInPenaltyBox == setPieceGoalKickSpot.isInPenaltyBox;
    }

    private bool HasOobGoalKickDefensiveTokensInPenaltyBox()
    {
        if (!IsOobGoalKickDefenseClearanceTurn())
        {
            return false;
        }

        return playerTokenManager.allTokens.Any(IsOobGoalKickDefenseClearanceToken);
    }

    private bool HasDefenderInBallHoldingGoalkeeperZoi()
    {
        HexCell ballHex = ball.GetCurrentHex();
        if (ballHex == null)
        {
            return false;
        }

        HashSet<HexCell> goalkeeperZoi = new(ballHex.GetNeighbors(hexGrid));
        return playerTokenManager.allTokens
            .Where(token => token != null && !token.isAttacker)
            .Select(token => token.GetCurrentHex())
            .Any(hex => hex != null && goalkeeperZoi.Contains(hex));
    }

    private void ApplyOobGoalKickDefenseClearanceMovableTokenRules()
    {
        if (!IsOobGoalKickDefenseClearanceTurn())
        {
            return;
        }

        List<PlayerToken> clearanceTokens = playerTokenManager.allTokens
            .Where(IsOobGoalKickDefenseClearanceToken)
            .ToList();

        if (clearanceTokens.Count == 0)
        {
            return;
        }

        foreach (PlayerToken clearanceToken in clearanceTokens)
        {
            if (!eligibleTokens.Contains(clearanceToken))
            {
                eligibleTokens.Add(clearanceToken);
            }

            movedTokens.Remove(clearanceToken);
            if (!currentMovableTokens.Contains(clearanceToken))
            {
                currentMovableTokens.Add(clearanceToken);
            }
        }
    }

    private void ApplyOobGoalKickSpotBlockerMovableTokenRule()
    {
        if (!IsOobGoalKickSpotBlockedByCurrentTeam())
        {
            return;
        }

        PlayerToken blocker = setPieceGoalKickSpot.GetOccupyingToken();
        if (blocker == null)
        {
            return;
        }

        if (!eligibleTokens.Contains(blocker))
        {
            eligibleTokens.Add(blocker);
        }

        if (!currentMovableTokens.Contains(blocker))
        {
            currentMovableTokens.Add(blocker);
        }

        movedTokens.Remove(blocker);
    }

    private int GetF3MovementRangeForToken(PlayerToken token)
    {
        if (IsOobGoalKickCurrentSideDefenderToken(token))
        {
            return GetMinimumOobGoalKickDefenderExitRange(token);
        }

        return 6;
    }

    private int GetMinimumOobGoalKickDefenderExitRange(PlayerToken token)
    {
        HexCell startHex = token != null ? token.GetCurrentHex() : null;
        if (startHex == null)
        {
            return 6;
        }

        for (int range = 6; range <= 30; range++)
        {
            var (reachableHexes, _) = HexGridUtils.GetReachableHexes(hexGrid, startHex, range);
            if (reachableHexes.Any(IsOobGoalKickDefenderExitDestination))
            {
                return range;
            }
        }

        Debug.LogWarning($"{token.name} has no reachable exit hex from the Goal Kick side penalty box. Keeping the normal F3 range.");
        return 6;
    }

    private void ApplyContextualMovementHighlights(PlayerToken token)
    {
        if (IsOobGoalKickCurrentSideDefenderToken(token))
        {
            FilterHighlightedHexes(IsOobGoalKickDefenderExitDestination);
            if (hexGrid.highlightedHexes.Count == 0)
            {
                Debug.LogWarning($"{token.name} has no legal Goal Kick clearance destination from {token.GetCurrentHex()?.coordinates.ToString() ?? "unknown hex"}.");
            }
            return;
        }

        if (IsFirstSaveAndHoldKFinalThird() && currentTeamMoving == "attack" && token == ball.GetCurrentHex()?.GetOccupyingToken())
        {
            FilterHighlightedHexes(hex => hex != null && !hex.isOutOfBounds && hex.isInPenaltyBox != 0);
            return;
        }

        if (IsSaveAndHoldCurrentSideDefenseTurn())
        {
            List<HexCell> gkZoi = ball.GetCurrentHex().GetNeighbors(hexGrid).ToList();
            FilterHighlightedHexes(hex => hex != null && !hex.isOutOfBounds && !gkZoi.Contains(hex));
            return;
        }

        FilterHighlightedHexes(hex => hex != null && !hex.isOutOfBounds);
    }

    private void FilterHighlightedHexes(System.Func<HexCell, bool> predicate)
    {
        List<HexCell> filteredHexes = hexGrid.highlightedHexes
            .Where(predicate)
            .ToList();
        hexGrid.ClearHighlightedHexes();
        foreach (HexCell hex in filteredHexes)
        {
            if (!hexGrid.highlightedHexes.Contains(hex))
            {
                hexGrid.highlightedHexes.Add(hex);
                hex.HighlightHex("PaceAvailable");
            }
        }
    }

    private bool IsOobGoalKickDefenderExitDestination(HexCell hex)
    {
        return hex != null
            && !hex.isOutOfBounds
            && (setPieceGoalKickSpot == null || hex.isInPenaltyBox != setPieceGoalKickSpot.isInPenaltyBox)
            && hex != setPieceGoalKickSpot
            && !hex.isAttackOccupied
            && !hex.isDefenseOccupied;
    }

    private IEnumerator MoveGoalkeeperAndStartOppositeFinalThird()
    {
        yield return StartCoroutine(MoveGoalkeeperToGoalKickSpotIfNeeded());
        Debug.Log("No Tokens in the Goal Kick side Final Third. Moving to opposite-side Final Thirds.");
        thisIsTheSecond = true;
        TriggerFinalThirdPhaseInternal();
    }

    private IEnumerator MoveGoalkeeperToGoalKickSpotIfNeeded()
    {
        if (setPieceGoalKickGoalkeeper == null || setPieceGoalKickSpot == null)
        {
            yield break;
        }

        if (setPieceGoalKickGoalkeeper.GetCurrentHex() == setPieceGoalKickSpot)
        {
            ball.PlaceAtCell(setPieceGoalKickSpot);
            MatchManager.Instance.SetLastToken(setPieceGoalKickGoalkeeper);
            yield break;
        }

        HexCell oldHex = setPieceGoalKickGoalkeeper.GetCurrentHex();
        if (oldHex != null && (oldHex.GetOccupyingToken() == null || oldHex.GetOccupyingToken() == setPieceGoalKickGoalkeeper))
        {
            oldHex.occupyingToken = null;
            oldHex.isAttackOccupied = false;
            oldHex.isDefenseOccupied = false;
            oldHex.ResetHighlight();
        }

        setPieceGoalKickSpot.occupyingToken = null;
        setPieceGoalKickSpot.isAttackOccupied = true;
        setPieceGoalKickSpot.isDefenseOccupied = false;
        setPieceGoalKickGoalkeeper.isAttacker = true;
        yield return StartCoroutine(setPieceGoalKickGoalkeeper.JumpToHex(setPieceGoalKickSpot));
        ball.PlaceAtCell(setPieceGoalKickSpot);
        setPieceGoalKickSpot.HighlightHex("isAttackOccupied");
        MatchManager.Instance.SetLastToken(setPieceGoalKickGoalkeeper);
        Debug.Log($"{setPieceGoalKickGoalkeeper.name} moves onto the cleared Goal Kick spot to take the Goal Kick.");
    }

    public void DropBall()
    {
        isWaitingForWhatToDo = false;
        isWaitingForSetPieceGoalKickChoice = false;
        isActivated = false;
        thisIsTheSecond = false;
        bothSides = false;
        string gkWithBall = ball.GetCurrentHex()?.GetOccupyingToken()?.name ?? "The goalkeeper";
        ClearSpecialFinalThirdContext();

        if (useThomasDropBallAutoMovementRule)
        {
            movementPhaseManager.ResetMovementPhase();
            movementPhaseManager.ActivateMovementPhase();
            movementPhaseManager.CommitToAction();
            Debug.Log($"{gkWithBall} drops the ball at feet and commits to Movement Phase.");
            return;
        }

        MatchManager.Instance.BroadcastSafeEndofMovementPhase();
        Debug.Log($"{gkWithBall} drops the ball at feet. Normal end-of-movement options are available.");
    }

    public void StandardGoalKickPass()
    {
        if (!EnsureSetPieceGoalKickGoalkeeperReady())
        {
            return;
        }

        isWaitingForWhatToDo = false;
        isWaitingForSetPieceGoalKickChoice = false;
        isActivated = false;
        thisIsTheSecond = false;
        bothSides = false;
        MatchManager.Instance.currentState = MatchManager.GameState.GoalKick;
        MatchManager.Instance.OfferStandardGroundBallPass();
        MatchManager.Instance.ClearLastTokenChain();
        MatchManager.Instance.SetLastToken(setPieceGoalKickGoalkeeper);
        string gkName = setPieceGoalKickGoalkeeper.name;
        Vector3Int kickCoordinates = ball.GetCurrentHex().coordinates;
        MatchManager.Instance.TriggerStandardPass(setPieceGoalKickGoalkeeper);
        ClearSetPieceGoalKickContext();
        Debug.Log($"{gkName} will take a Goal Kick Standard Pass from {kickCoordinates}.");
    }
    
    public void GKKick()
    {
        if (isWaitingForSetPieceGoalKickChoice && !EnsureSetPieceGoalKickGoalkeeperReady())
        {
            return;
        }

        isWaitingForWhatToDo = false;
        isWaitingForSetPieceGoalKickChoice = false;
        isActivated = false;
        thisIsTheSecond = false;
        bothSides = false;
        MatchManager.Instance.currentState = MatchManager.GameState.GoalKick;
        PlayerToken gkToken = ball.GetCurrentHex()?.GetOccupyingToken();
        if (gkToken == null)
        {
            Debug.LogError("Cannot take a Goal Kick because there is no goalkeeper on the ball hex.");
            return;
        }
        string gkWithBall = gkToken.name;
        MatchManager.Instance.currentState = MatchManager.GameState.GoalKick;
        MatchManager.Instance.TriggerGoalkeeperKick(gkToken, commitImmediately: true);
        ClearSetPieceGoalKickContext();
        Debug.Log($"{gkWithBall} will take a GK High Pass. Please click a valid target at least {highPassManager.minPassDistance} hexes away and outside the opposite Final Third.");
    }

    private bool EnsureSetPieceGoalKickGoalkeeperReady()
    {
        if (!IsOobGoalKickContext())
        {
            return true;
        }

        if (setPieceGoalKickGoalkeeper == null || setPieceGoalKickSpot == null)
        {
            Debug.LogError("Cannot take the Goal Kick because the set-piece goalkeeper or spot is missing.");
            return false;
        }

        if (setPieceGoalKickGoalkeeper.GetCurrentHex() != setPieceGoalKickSpot)
        {
            Debug.LogError("Cannot take the Goal Kick because the goalkeeper is not on the Goal Kick spot.");
            return false;
        }

        ball.PlaceAtCell(setPieceGoalKickSpot);
        MatchManager.Instance.ClearLastTokenChain();
        MatchManager.Instance.SetLastToken(setPieceGoalKickGoalkeeper);
        return true;
    }

    private void ClearSetPieceGoalKickContext()
    {
        ClearSpecialFinalThirdContext();
    }

    private void ClearSpecialFinalThirdContext()
    {
        finalThirdContext = FinalThirdContext.None;
        setPieceGoalKickSpot = null;
        setPieceGoalKickGoalkeeper = null;
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
        if (isMovingToken) sb.Append("isMovingToken, ");
        if (isWaitingForWhatToDo) sb.Append("isWaitingForWhatToDo, ");
        if (isWaitingForSetPieceGoalKickChoice) sb.Append("isWaitingForSetPieceGoalKickChoice, ");
        if (thisIsTheSecond) sb.Append("thisIsTheSecond, ");
        if (!string.IsNullOrEmpty(currentTeamMoving) && (currentTeamMoving == "attack" || currentTeamMoving == "defense")) sb.Append($"currentTeamMoving: {GetTeamNameByCurrentTeamMoving()}, ");
        if (selectedToken != null) sb.Append($"selectedToken: {selectedToken.name}, ");
        
        if (sb.Length >= 2 && sb[^2] == ',') sb.Length -= 2; // Trim trailing comma
        return sb.ToString();
    }

    public string GetInstructions()
    {
        StringBuilder sb = new();
        if (isActivated && !isWaitingForWhatToDo)
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
        if (isWaitingForTargetHex && selectedToken != null) sb.Append($" or Click on a Hex to move {selectedToken.name} there, ");
        if (isMovingToken) sb.Append("moving selected F3 token, ");
        if (isActivated && !isWaitingForWhatToDo && !isMovingToken && CanForfeitCurrentF3Move(logWarnings: false)) sb.Append($"Press [X] to Forfeit {GetTeamNameByCurrentTeamMoving()}'s current F3 Move, ");
        if (isWaitingForSetPieceGoalKickChoice) sb.Append($"Press [P] to take a Goal Kick Standard Pass, or [K] to take a Goalkeeper Kick, ");
        else if (isWaitingForWhatToDo) sb.Append($"Press [D] to Drop the ball and commit to Movement Phase, or [K] to take a Goal Kick, ");
        
        if (sb.Length >= 2 && sb[^2] == ',') sb.Length -= 2; // Safely trim trailing comma + space
        return sb.ToString();
    }

    public bool? IsInstructionExpectingHomeTeam()
    {
        if (!isActivated || MatchManager.Instance == null)
        {
            return null;
        }

        bool attackingTeamIsHome = MatchManager.Instance.teamInAttack == MatchManager.TeamInAttack.Home;
        return currentTeamMoving switch
        {
            "attack" => attackingTeamIsHome,
            "defense" => !attackingTeamIsHome,
            _ => isWaitingForWhatToDo ? attackingTeamIsHome : null,
        };
    }

}
