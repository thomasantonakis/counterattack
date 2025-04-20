using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using System.Threading.Tasks;
using UnityEngine.Analytics;
using System;
using UnityEditor.Experimental.GraphView;
using System.Text.RegularExpressions;

public class MovementPhaseManager : MonoBehaviour
{
    [Header("Dependencies")]
    public GroundBallManager groundBallManager;
    public HeaderManager headerManager;
    public FreeKickManager freeKickManager;
    public LooseBallManager looseBallManager;
    public FinalThirdManager finalThirdManager;
    public GoalKeeperManager goalKeeperManager;
    public GoalFlowManager goalFlowManager;
    public ShotManager shotManager;
    public HighPassManager highPassManager;
    public HelperFunctions helperFunctions;
    public HexGrid hexGrid;  // Reference to the HexGrid
    public Ball ball;
    public HexCell ballHex;
    [Header("Tokens Involved")]
    public PlayerToken selectedToken;
    [SerializeField]
    private PlayerToken selectedDefender;
    public PlayerToken nutmegVictim;
    [Header("Boolean Flags")]
    public bool isAvailable = false;
    public bool isActivated = false;
    public bool isMovementPhaseAttack = false;
    public bool isMovementPhaseDef = false;
    public bool isMovementPhase2f2 = false;
    public bool isAwaitingTokenSelection = false;  // Flag to indicate if waiting for token selection
    public bool isAwaitingHexDestination = false;  // Flag to indicate if waiting for the selected token's destinatiion
    public bool isBallPickable = false;
    public bool isDribblerRunning; // Flag to indicate ongoing dribbler movement
    public bool isPlayerMoving = false;  // Tracks if a player is currently moving
    public bool tokenPickedUpBall = false; // Flag to check if waiting for Nutmeg decision
    public bool isWaitingForInterceptionDiceRoll = false;  // Whether we're waiting for a dice roll
    public bool isWaitingForTackleDecision = false;  // Whether we're waiting for a dice roll
    public bool isWaitingForTackleDecisionWithoutMoving = false; // Flag to check if waiting for tackle decision
    public bool isWaitingForTackleRoll = false;  // Whether we're waiting for a dice roll
    public bool isWaitingForSnapshotDecision = false;
    public bool isWaitingForReposition = false;  // Whether we're waiting for a dice roll
    private bool tackleAttackerRolled = false;  // Whether we're waiting for a dice roll
    private bool tackleDefenderRolled = false;  // Whether we're waiting for a dice roll
    public bool isWaitingForYellowCardRoll = false;  // Whether we're waiting for a dice roll
    public bool isWaitingForInjuryRoll = false;  // Whether we're waiting for a dice roll
    public bool isWaitingForFoulDecision = false;  // Whether we're waiting for a dice roll
    public bool isWaitingForNutmegDecision = false;
    public bool isWaitingForNutmegDecisionWithoutMoving = false;
    public bool lookingForNutmegVictim = false;
    public bool isNutmegInProgress = false;
    [Header("Informative Runtime Items")]
    public PlayerToken repositionWinner; // The token that won the tackle duel
    public PlayerToken repositionLoser; // The token that lost the tackle duel
    public int remainingDribblerPace; // Temporary variable for dribbler's pace
    [SerializeField]
    private List<PlayerToken> movedTokens = new List<PlayerToken>();  // To track moved tokens
    [SerializeField]
    private List<PlayerToken> eligibleDefenders = new List<PlayerToken>();  // To track defenders eligible for interception
    public List<PlayerToken> nutmeggableDefenders = new List<PlayerToken>(); // Temporary list of defenders tha can be nutmegged
    [SerializeField]
    private List<PlayerToken> defendersTriedToIntercept = new List<PlayerToken>(); // Temporary list of defenders
    public List<PlayerToken> stunnedTokens = new List<PlayerToken>(); // Temporary list of defenders
    public List<PlayerToken> stunnedforNext = new List<PlayerToken>(); // Temporary list of defenders
    public List<HexCell> repositionHexes = new List<HexCell>();
    private int defenderDiceRoll;
    private int attackerDiceRoll;
    [SerializeField]
    private int attackersMoved = 0;
    [SerializeField]
    private int defendersMoved = 0;
    [SerializeField]
    private int attackersMovedIn2f2 = 0;
    private int maxAttackerMoves = 4;  // Max moves allowed for attackers
    private int maxDefenderMoves = 5;  // Max moves allowed for defenders
    private int maxAttackerMovesIn2f2 = 2;
    private int movementRange2f2 = 2;  // Movement range limited to 2 hexes
    private List<HexCell> defenderHexesNearBall = new List<HexCell>();  // Defenders near the ball
    private const int FOUL_THRESHOLD = 1;  // Below this one is a foul
    private const int INTERCEPTION_THRESHOLD = 10;  // Below this one is a foul

    private async Task StartCoroutineAndWait(IEnumerator coroutine)
    {
        bool isDone = false;
        StartCoroutine(WrapCoroutine(coroutine, () => isDone = true));
        await Task.Run(() => { while (!isDone) { } }); // Wait until coroutine completes
    }

    private IEnumerator WrapCoroutine(IEnumerator coroutine, System.Action onComplete)
    {
        yield return StartCoroutine(coroutine);
        onComplete?.Invoke();
    }
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
        // When do we need a expect a Token Selection?
        if (
            token != null // A token was indeed inferred from the click
            && !isPlayerMoving // Wait for animations to stop
            && !isDribblerRunning // The Dribbler has not started moving
            && (
                selectedToken == null // MovementPhase does not have a selected Token.
                || !isDribblerRunning //  We Should not be able to reset the selected Token while the Dribbler is running.
            )
            && !lookingForNutmegVictim // Do not handle a Token when looking for a victim
            && !isWaitingForNutmegDecision // Do not handle a Token when waiting for Nutmeg Decision
            && !isWaitingForNutmegDecisionWithoutMoving // Do not handle a Token when waiting for Nutmeg Decision without moving
        )
        {
            Debug.Log($"Passing {token.name} to HandleTokenSelection");
            HandleTokenSelection(token);  // Select the token first
        }
        else if (
            // While either we are waiting to nutmeg without moving
            isWaitingForNutmegDecisionWithoutMoving
            // Or we are waiting to nutmeg while the dribbler has already started moving
            || (isDribblerRunning && isWaitingForNutmegDecision)
        )
        {
            // We clicked on a Nutmeggable Defender
            if (nutmeggableDefenders.Contains(token))
            {
                // Start the Nutmeg with the Selected nutmeggable Token
                Debug.LogWarning("While waiting for a Nutmeg Decision, a nutmeggable Defender was clicked");
                isWaitingForSnapshotDecision = false;
                isWaitingForNutmegDecision = false;
                isWaitingForNutmegDecisionWithoutMoving = false;
                nutmegVictim = token;
                isDribblerRunning = true;
                hexGrid.ClearHighlightedHexes();
                Debug.Log($"Selected {token.name} to nutmeg. Proceeding with nutmeg.");
                lookingForNutmegVictim = false;
                StartNutmegProcess();
            }
            else
            {
                Debug.LogWarning("While waiting for a Nutmeg Decision, a nutmeggable Defender was not clicked");
                if (IsHexValidForMovement(hex))
                {
                    AsyncMoveTokenToHexWhileWaitingForNutmegDecision(hex);
                }
            }
        }
        else if (lookingForNutmegVictim)
        {
            // Nutmeg was selected with the Keyboard, and more than one nutmeggable Defender exists
            Debug.Log($"Passing {token.name} to HandleNutmegVictimSelection");
            HandleNutmegVictimSelection(token);
        }
        else if (isWaitingForReposition)
        {
            if (hex != null
                && repositionHexes.Contains(hex)
                && hex != repositionLoser.GetCurrentHex()
                && !hex.isDefenseOccupied
                && !hex.isAttackOccupied
            )
            {
                AsyncRepositionTokenToHex(hex);
            }
        }
        else
        // We did not infer a Token (clicked on an Hex (or the ball on it) where there is no Token)
        // Clicked on a NOT OCCUPIED HEX
        {
            if (!isWaitingForNutmegDecision && !isWaitingForNutmegDecisionWithoutMoving)
            {
                // If the hex is not occupied, check if it's valid for movement
                if (IsHexValidForMovement(hex))
                {
                    AsyncMoveTokenToHexWithTempCheck(hex);
                }
            }
        }
    }

  private void OnKeyReceived(KeyPressData keyData)
    {
        if (isAvailable && !isActivated && keyData.key == KeyCode.M)
        {
            ActivateMovementPhase();
            return;
        }
        if (isWaitingForInterceptionDiceRoll && keyData.key == KeyCode.R)
        {
            Debug.Log("R key detected for interception dice roll.");
            StartCoroutine(PerformBallInterceptionDiceRoll());
        }
        else if (isWaitingForTackleDecision)
        {
            if (keyData.key == KeyCode.N)  // No tackle
            {
                Debug.Log("No tackle chosen.");
                ResetTacklePhase();  // Reset tackle phase if no tackle is chosen
                AdvanceMovementPhase();
            }
            else if (keyData.key == KeyCode.T)  // Tackle chosen
            {
                Debug.Log("Tackle chosen. Starting tackle dice rolls...");
                isWaitingForTackleDecision = false;
                StartTackleDiceRollSequence();  // Start the dice roll sequence for tackling
            }
        }
        else if (isWaitingForTackleDecisionWithoutMoving)
        {
            if (keyData.key == KeyCode.T)
            {
                // Defender chooses to tackle
                Debug.Log($"{selectedToken.name} initiates a tackle without moving. Starting Dice Rolls...");
                movedTokens.Add(selectedToken); // Mark defender as having moved
                isWaitingForTackleDecisionWithoutMoving = false; // Reset tackle decision flag
                isWaitingForTackleDecision = false;  // Reset tackle decision flag
                hexGrid.ClearHighlightedHexes();
                StartTackleDiceRollSequence();  // Start the dice roll sequence for tackling
            }
        }
        else if (isWaitingForTackleRoll && keyData.key == KeyCode.R)
        {
            if (!tackleDefenderRolled)
            {
                PerformTackleDiceRoll(isDefender: true);  // Defender rolls first
            }
            else if (!tackleAttackerRolled)
            {
                PerformTackleDiceRoll(isDefender: false);  // Attacker rolls second
            }
        }
        else if (isWaitingForNutmegDecision && isWaitingForNutmegDecisionWithoutMoving && keyData.key == KeyCode.X)
        {
            ForfeitTeamMovementPhase();
        }
        else if (isBallPickable && keyData.key == KeyCode.V)
        {
            AsyncMoveTokenToHexToPickUpBall();
        }
        else if (isWaitingForNutmegDecision)
        {
            if (keyData.key == KeyCode.N)
            {
                isWaitingForNutmegDecision = false;
                isWaitingForSnapshotDecision = false;
                Debug.Log($"Starting Nutmeg Process.");
                StartNutmegVictimIdentification();
            }
            else if (keyData.key == KeyCode.X)
            {
                isWaitingForNutmegDecision = false;
                Debug.Log($"Reject Nutmeg. Check for interceptions.");
                ContinueFromRejectedNutmeg();
            }
        }
        else if (isWaitingForNutmegDecisionWithoutMoving)
        {
            if (keyData.key == KeyCode.N)
            {
                isWaitingForNutmegDecisionWithoutMoving = false;
                isWaitingForSnapshotDecision = false;
                Debug.Log($"Starting Nutmeg Process.");
                StartNutmegVictimIdentification();
            }
        }
        else if (isWaitingForSnapshotDecision)
        {
            if (keyData.key == KeyCode.S)
            {
                isWaitingForSnapshotDecision = false;
                Debug.Log($"{selectedToken.name} decides to Snapshot!!!!");
                shotManager.StartShotProcess(selectedToken, "snapshot");
            }
            if (keyData.key == KeyCode.X)
            {
                isWaitingForSnapshotDecision = false;
                Debug.Log($"Attacker decides not to shoot..");
            }
        }
        else if (isWaitingForReposition)
        {
            if (keyData.key == KeyCode.X)
            {
                if (isNutmegInProgress)
                {
                    Debug.Log($"{repositionWinner.name} just nutmegged {repositionLoser.name}, they have to reposition.");
                }
                else if (MatchManager.Instance.currentState != MatchManager.GameState.MovementPhaseDef)
                {
                    Debug.Log($"{repositionWinner.name} forfeits repositioning and stays at current position.");
                    isWaitingForReposition = false;
                    hexGrid.ClearHighlightedHexes();
                    // selectedToken = winner;
                    DribblerMoved1HexOrReposition();
                    return;
                }
                else
                {
                    AdvanceMovementPhase();
                }
            }
        }
        else if (isWaitingForYellowCardRoll)
        {
            if (keyData.key == KeyCode.R)
            {
                PerformLeniencyTest();
            }
        }
        else if (isWaitingForInjuryRoll)
        {
            if (keyData.key == KeyCode.R)
            {
                PerformInjuryTest();
            }
        }
        else if (isWaitingForFoulDecision)
        {
            if (keyData.key == KeyCode.A)
            {
                PlayAdvantage();
            }
            else if (keyData.key == KeyCode.Z)
            {
                TakeFreeKick();
            }
        }
    }

    private async void AsyncMoveTokenToHexWhileWaitingForNutmegDecision(HexCell hex)
    {
        // Turning off wait for Nutmeg decision flags.
        if (isWaitingForTackleDecisionWithoutMoving) isWaitingForTackleDecisionWithoutMoving = false;
        if (isWaitingForNutmegDecisionWithoutMoving) isWaitingForNutmegDecisionWithoutMoving = false;
        if (isWaitingForSnapshotDecision) isWaitingForSnapshotDecision = false;
        Debug.Log($"Passing {hex.name} to MoveTokenToHex");
        await StartCoroutineAndWait(MoveTokenToHex(hex));  // Move the selected token to the hex
    }
    private async void AsyncMoveTokenToHexToPickUpBall()
    {
        tokenPickedUpBall = true;
        remainingDribblerPace = selectedToken.pace;
        await StartCoroutineAndWait(MoveTokenToHex(ball.GetCurrentHex()));
        isBallPickable = false;
    }

    private async void AsyncMoveTokenToHexWithTempCheck(HexCell hex)
    {
        isWaitingForSnapshotDecision = false;
        bool temp_check = ball.GetCurrentHex() == hex && selectedToken != null;
        if (temp_check)
        {
            tokenPickedUpBall = true;
        }
        Debug.Log($"Passing {hex.name} to MoveTokenToHex");
        await StartCoroutineAndWait(MoveTokenToHex(hex));  // Move the selected token to the hex
        if (temp_check)
        {
            isBallPickable = false;
        }
    }

    private void ActivateMovementPhase()
    {
        isActivated = true;
        isAvailable = false;
        isMovementPhaseAttack = true;
        isAwaitingTokenSelection = true;
    }
    // This method will be called when a player token is clicked
    public void HandleTokenSelection(PlayerToken token)
    {
        // Clear previous highlights
        hexGrid.ClearHighlightedHexes();

        // Ensure the token can move in this phase and hasn't already moved
        if (isMovementPhaseAttack)
        {
            if (
                !token.isAttacker
                || movedTokens.Contains(token)
                || headerManager.attackerWillJump.Contains(token)
                || headerManager.defenderWillJump.Contains(token)
                || stunnedTokens.Contains(token)
            )
            {
                Debug.Log("MPAtt: Cannot select this token to move. Either it's not an attacker or it has already moved or is frozen due to previous header challenge.");
                return;  // Reject defender clicks or already moved tokens
            }
        }
        else if (isMovementPhaseDef)
        {
            if (
                token.isAttacker
                || movedTokens.Contains(token)
                || headerManager.attackerWillJump.Contains(token)
                || headerManager.defenderWillJump.Contains(token)
                || stunnedTokens.Contains(token)
            )
            {
                Debug.Log("MPDef: Cannot select this token to move. Either it's not a defender or it has already moved or is frozen due to previous header challenge.");
                return;  // Reject attacker clicks or already moved tokens
            }
        }
        else if (isMovementPhase2f2)
        {
            // Only allow attackers who haven't moved yet in MovementPhaseAtt
            if (!token.isAttacker || movedTokens.Contains(token) || headerManager.attackerWillJump.Contains(token) || headerManager.defenderWillJump.Contains(token))
            {
                Debug.Log("MP2f2:Cannot select this token to move. Either it's not an attacker or it has already moved or is frozen due to previous header challenge.");
                return;
            }
            // // Limit the movement range to 2 hexes
            // selectedToken = token;
            // Debug.Log($"Selected Token for 2f2: {selectedToken.name}");

            // // Highlight valid movement hexes for the selected token with a range of 2 hexes
            // HighlightValidMovementHexes(selectedToken, movementRange2f2);
            // return;
        }
        // Select the token
        selectedToken = token;
        // The selected token is a Defender that has not moved.
        if (isMovementPhaseDef && !selectedToken.isAttacker && !movedTokens.Contains(selectedToken))
        {
            Debug.Log($"A Valid Defender was selected: {selectedToken.name}");
            HighlightValidMovementHexes(selectedToken, token.pace);
            PlayerToken ballHolder = ball.GetCurrentHex()?.GetOccupyingToken();
            if (ballHolder != null && ballHolder.isAttacker)
            {
                // Check if defender is adjacent to the attacker with the ball
                HexCell[] neighbors = ballHolder.GetCurrentHex().GetNeighbors(hexGrid);
                if (neighbors.Contains(selectedToken.GetCurrentHex()))
                {
                    // Defender is adjacent, enable tackle decision
                    Debug.Log($"{selectedToken.name} is adjacent to {ballHolder.name}. Press 'T' to tackle or select a hex to move.");
                    isWaitingForTackleDecisionWithoutMoving = true;
                }
            }
        }
        if (
            (
                isMovementPhaseAttack || isMovementPhase2f2
            ) 
            && selectedToken.isAttacker
            && !movedTokens.Contains(selectedToken)
        )
        {
            if (token.IsDribbler && !isDribblerRunning)
            {
                Debug.Log($"{token.name} selected as dribbler. Starting dribble movement.");
                if (!tokenPickedUpBall)
                {
                    if (isMovementPhase2f2)
                    {
                        remainingDribblerPace = movementRange2f2;
                    }
                    else
                    {
                        remainingDribblerPace = token.pace; // Initialize remaining pace
                    }
                    Debug.Log($"Setting remaining Pace to {token.name}'s Pace: {remainingDribblerPace}");
                } 

                defendersTriedToIntercept.Clear(); // Reset defenders list
                HighlightValidMovementHexes(token, 1); // Highlight immediate neighbors
                nutmeggableDefenders = GetNutmeggableDefenders(token, hexGrid);
                if (remainingDribblerPace >=2 && nutmeggableDefenders.Count() > 0)
                {
                    Debug.Log($"{selectedToken.name} is next to at least one Nutmeggable Defender. Select a nutmeggable Defender, or Press [N] to attempt a Nutmeg OR Select a Hex to Move.");
                    isWaitingForNutmegDecisionWithoutMoving = true;
                }
            }
            else if (isDribblerRunning)
            {
                Debug.LogWarning("Dribbler is Running and has remaining Pace, please first Forfeit the rest of the Pace [X]");
                // HighlightValidMovementHexes(selectedToken, 1);
            }
            else if (isMovementPhaseAttack)
            { HighlightValidMovementHexes(selectedToken, token.pace); }
            // Highlight valid movement hexes for the selected token
            else if (isMovementPhase2f2)
            { HighlightValidMovementHexes(selectedToken, movementRange2f2); }
            else { Debug.LogError("Unknown Match State"); }
        }

    }
    
    // This method will highlight valid movement hexes for the selected token
    public void HighlightValidMovementHexes(PlayerToken token, int movementRange)
    {
        HexCell currentHex = token.GetCurrentHex();  // Get the hex the token is currently on
        if (currentHex == null)
        {
            Debug.LogError("Selected token does not have a valid hex!");
            return;
        }

        // Clear any previously highlighted hexes before highlighting new ones
        hexGrid.ClearHighlightedHexes();

        // Get valid movement hexes and their distance/ZOI data
        var (reachableHexes, distanceData) = HexGridUtils.GetReachableHexes(hexGrid, currentHex, movementRange);
        ballHex = ball.GetCurrentHex();
        // Check if ball hex is reachable
        if (reachableHexes.Contains(ballHex) && !token.isAttacker)
        {
            Debug.Log("Ball is reachable, recalculating Reachable without using ballHex");
            // Temporarily mark the ball hex as occupied and recalculate
            ballHex.isDefenseOccupied = true;
            var (reachableWithoutBall, _) = HexGridUtils.GetReachableHexes(hexGrid, currentHex, movementRange);
            ballHex.isDefenseOccupied = false;

            // Add the ball hex back to reachable hexes
            reachableWithoutBall.Add(ballHex);
            reachableHexes = reachableWithoutBall;
        }

        foreach (HexCell hex in reachableHexes)
        {
            if (!hex.isAttackOccupied && !hex.isDefenseOccupied)
            {
                hexGrid.highlightedHexes.Add(hex);  // Store the valid hex directly in HexGrid's highlightedHexes list

                // Retrieve the distance and ZOI data for the current hex
                var (hexDistance, enteredZOI) = distanceData[hex];

                // Highlight the hex based on ZOI entry and range
                if (hexDistance <= movementRange)
                {
                    // if (
                    //     token.GetCurrentHex() == ballHex // token is on the ball
                    //     && hex.isInGoal != 0 // the hex is in a goal
                    //     // the hex is in the attaking part of the dribbler
                    // )
                    {
                        hex.HighlightHex("PaceAvailable");  // Normal color for reachable hexes
                    }
                }
            }
        }
        isBallPickable = reachableHexes.Contains(ballHex) && !token.IsDribbler;
        // Slight change as it seems that clicking on the ballHex while moving for FTP causes an error
        // isBallPickable = reachableHexes.Contains(ballHex) && !selectedToken.IsDribbler;
    }

    // Check if the clicked hex is a valid one
    public bool IsHexValidForMovement(HexCell hex)
    {
        bool isValid = hexGrid.highlightedHexes.Contains(hex);  // Check if the clicked hex is in the list of valid hexes
        // Debug.Log($"IsHexValidForMovement called for {hex.name}: {isValid}");
        if (!isValid)
        {
            if (isDribblerRunning)
            {
                Debug.Log($"{selectedToken.name} is running with the ball. Please click on a valid Hex Or Forfeit remaining Pace [X].");
            }
            else
            {
                hexGrid.ClearHighlightedHexes();  // Clear the highlights if an invalid hex is clicked
                Debug.Log("Invalid hex clicked. All highlights cleared.");
            }
        }
        return isValid;
    }

    public IEnumerator MoveTokenToHex(HexCell targetHex, PlayerToken token = null, bool isCalledDuringMovement = true, bool shouldCountForDistance = true)
    {
        PlayerToken movingToken = token ?? selectedToken;
        if (movingToken == null)
        {
            Debug.LogError("No token selected to move.");
            yield break;
        }
        isActivated = true;

        // Find the path from the current hex to the target hex
        List<HexCell> path;
        HexCell ballHex = ball.GetCurrentHex();
        // If We call this method during HP or FTP moves, find the shortest path 
        if (targetHex != ballHex && isCalledDuringMovement)
        {
            ballHex.isDefenseOccupied = true;
            path = HexGridUtils.FindPath(movingToken.GetCurrentHex(), targetHex, hexGrid);
            ballHex.isDefenseOccupied = false;
        }
        else
        {
            path = HexGridUtils.FindPath(movingToken.GetCurrentHex(), targetHex, hexGrid);
        }

        if (path == null || path.Count == 0)
        {
            if (isCalledDuringMovement) Debug.LogError("No valid path found to the target hex.");
            else Debug.LogWarning("No movement asked");
            yield break;
        }
        
        if (movingToken.IsDribbler && !isDribblerRunning && isCalledDuringMovement)
        {
            Debug.Log("isDribblerRunning set to true");
            isDribblerRunning = true;
        }
        // Start the token movement across the hexes in the path
        yield return StartCoroutine(MoveTokenAlongPath(movingToken, path, shouldCountForDistance));
        if (!isCalledDuringMovement) {yield break;}
        if (targetHex == ballHex)
        {
            Debug.Log("isDribblerRunning set to true because someone picked up the ball.");
            isDribblerRunning = true;
        }
        ResolveMovement(targetHex, path);
    }

    public void ResolveMovement(HexCell targetHex, List<HexCell> path)
    {
        bool isMovingTokenAttacker = selectedToken.isAttacker;
        Debug.Log($"isMovingTokenAttacker: {isMovingTokenAttacker}");

        // Defender Lands on the Ball Hex
        if (!isMovingTokenAttacker && ball.GetCurrentHex() == targetHex && !MatchManager.Instance.attackHasPossession)
        {
            Debug.LogWarning("Defender picks up the loose ball");
            Debug.Log("Defender has picked up the loose ball!");
            movedTokens.Add(selectedToken);
            ball.SetCurrentHex(targetHex);  // Move the ball to the defender's hex
            MatchManager.Instance.ChangePossession();  // Change possession to the defender's team
            MatchManager.Instance.UpdatePossessionAfterPass(targetHex);  // Update possession
            EndMovementPhase();
            MatchManager.Instance.currentState = MatchManager.GameState.LooseBallPickedUp;  // Update game state
            remainingDribblerPace = 0;
        }
        // Defender does not land on Ball Hex
        else if (!isMovingTokenAttacker && isMovementPhaseDef)
        {
            Debug.LogWarning("Defender does not land on Ball Hex");
            movedTokens.Add(selectedToken);
            // If the ball is not picked up directly, check for nearby interception or tackle possibility
            // Add the condition to check if we're in the MovementPhaseDef state
            // Ensure the defender is adjacent to the attacker with the ball
            // HexCell ballHex = ball.GetCurrentHex();
            HexCell[] ballNeighbors = ballHex.GetNeighbors(hexGrid);
            // Defender lands next to the ball.
            if (ballNeighbors.Contains(selectedToken.GetCurrentHex()))
            {
                if (MatchManager.Instance.attackHasPossession)
                {
                    Debug.LogWarning("Defender lands next to the dribbler Prompt for Tackle or not.");
                    Debug.Log("Defender near the attacker with the ball. Waiting for tackle decision...Press [T]ackle or [N]o Tackle");
                    selectedDefender = selectedToken;  // Store the selected defender
                    isWaitingForTackleDecision = true;  // Activate tackle decision listener
                }
                // Defender lands next to the ball, Intercept.
                else
                {
                    Debug.LogWarning("Defender lands next to the unpossessed ball. Intercept the ball");
                    Debug.Log(" Defender near the ball, need to try and steal ball.");
                    // Check for interceptions
                    eligibleDefenders.Clear();
                    eligibleDefenders.Add(selectedToken);
                    Debug.Log($"{selectedToken.name} can intercept");
                    defendersTriedToIntercept.AddRange(eligibleDefenders); // Mark as tried
                    StartBallInterceptionDiceRollSequence(targetHex, eligibleDefenders);
                }
            }
            else
            {
                Debug.LogWarning("Defender is not close enough to tackle. No prompt shown.");
                AdvanceMovementPhase();
            }
        }
        // Not a defensive movement Phase
        else if (!isMovementPhaseDef)
        {
            Debug.LogWarning("We are not in a Defensive Movement Phase");
            Debug.Log($"Hello from the Attacking MP, Selected Token: {selectedToken.name}, IsDribbler: {selectedToken.IsDribbler}, remainingDribblerPace: {remainingDribblerPace}");
            // Non Dribbler Moved
            if (!selectedToken.IsDribbler)
            {
                Debug.LogWarning("The selected Token is not the dribbler");
                Debug.Log("Non Dribbler Moved.");
                if (MatchManager.Instance.currentState != MatchManager.GameState.LooseBallPickedUp)
                {
                    movedTokens.Add(selectedToken);
                }
                AdvanceMovementPhase(); // Basic check to advance the movement phase
            }
            else if (selectedToken.IsDribbler)
            // If attackHasPossession is false Check for nutmeg Option otherwise go for interception
            {
                Debug.LogWarning("The selected Token is the dribbler");
                Debug.Log("Hello, this is a dribbler dribbling");
                remainingDribblerPace -= path.Count; // Reduce remaining dribbler pace
                DribblerMoved1HexOrReposition();
            }
            else { Debug.LogError("How did we end up here?");}
        }
    }

    public void DribblerMoved1HexOrReposition()
    {
        Debug.Log($"BallHex: {ballHex.name}, {ballHex.isInGoal}");
        Debug.Log($"Ball's currentHex: {ball.GetCurrentHex().name}, {ball.GetCurrentHex().isInGoal}");
        if (ball.GetCurrentHex().isInGoal != 0)
        {
          Debug.Log($"{selectedToken.name} walked or repositioned in the goal! It's a GOAL!!!!");
          goalFlowManager.StartGoalFlow(ball.GetCurrentHex().GetOccupyingToken());
          // TRIGGER The GOAL CELEBRATION
          // LOG The GOAL
          return;
        }
                  
        if(isDribblerRunning)
        {
            HighlightValidMovementHexes(selectedToken, 1);
            nutmeggableDefenders = GetNutmeggableDefenders(selectedToken, hexGrid);
            Debug.Log($"remainingDribblerPace: {remainingDribblerPace}, nutmeggableDefenders: {nutmeggableDefenders.Count}");
            if (remainingDribblerPace >= 2 && nutmeggableDefenders.Count > 0)
            {
                Debug.LogWarning("Nutmeg(s) Are available to the dribbler");
                Debug.Log($"{selectedToken.name} is next to at least one Nutmeggable Defender. Click a Nutmeggable Defender or Press [N] to attempt a Nutmeg, or [X] to allow interceptions.");
                isWaitingForNutmegDecision = true;
                return;
            }
            else
            {
                Debug.LogWarning("For some reason the nutmeg was not available");
                nutmeggableDefenders.Clear();
                ContinueFromRejectedNutmeg();
                return;
            }
        }
        else
        {
            Debug.LogWarning("How did we end up here? DribblerMoved1HexOrReposition when isDribblerRunning = False");
            // this appeared after a reposition of the attacker.
        }
    }

    public void ContinueFromRejectedNutmeg()
    {
        if(isDribblerRunning)
        {
            Debug.LogWarning("ContinueFromRejectedNutmeg: Checking for Interceptions");
            nutmeggableDefenders.Clear();
            Debug.Log($"{selectedToken.name} Rejected the Nutmeg option, forcing interceptions from adjacent defenders.");
            HexCell ballHex = ball.GetCurrentHex();
            eligibleDefenders = GetEligibleDefendersForInterception(ballHex);
            Debug.Log($"eligibleDefenders.Count: {eligibleDefenders.Count}");
            if (eligibleDefenders.Count > 0)
            {
                Debug.LogWarning("There are possible Interceptions");
                Debug.Log($"Eligible defenders for interception: {string.Join(", ", eligibleDefenders.Select(d => d.name))}");
                StartBallInterceptionDiceRollSequence(selectedToken.GetCurrentHex(), eligibleDefenders);
                return; // Exit after handling interception
            }
            else
            {
                Debug.LogWarning("No interceptions Available, let the dribbler move if available");
                ContinueDribblerMovement();
            }
        }
        else
        {
            Debug.LogError("How did we end up here? ContinueFromRejectedNutmeg when isDribblerRunning = False");
        }
    }

    private void ContinueDribblerMovement()
    {
        nutmeggableDefenders = GetNutmeggableDefenders(selectedToken, hexGrid); // TODO: What the fuck is this doing here?
        bool isDribblerinOppPenBox = IsDribblerinOpponentPenaltyBox(selectedToken);
        if (isDribblerinOppPenBox)
        {
            Debug.Log($"{selectedToken.name} is in the opponent penalty Box. Press [S] to take a snapshot!");
            isWaitingForSnapshotDecision = true;
        }
        // Offer a Snapshot option
        // Highlight neighbors if more pace is available
        if (remainingDribblerPace > 0)
        {
            Debug.LogWarning($"Dribbler has {remainingDribblerPace} remaining Pace, Highlighting 1 Hex");
            Debug.Log("More Pace is available");
            HighlightValidMovementHexes(selectedToken, 1);
        }
        else
        {
            Debug.LogWarning($"Dribbler has 0 remaining Pace, Adding to Moved and moving Forward.");
            Debug.Log($"{selectedToken.name} has exhausted their pace. Dribbling complete.");
            Debug.Log($"isDribblerRunning set to false;");
            isDribblerRunning = false;
            hexGrid.ClearHighlightedHexes();
            if (!movedTokens.Contains(selectedToken))
            {
                movedTokens.Add(selectedToken);
            }
            AdvanceMovementPhase();
        }
    }

    // Coroutine to move the token one hex at a time
    private IEnumerator MoveTokenAlongPath(PlayerToken token, List<HexCell> path, bool shouldCountForDistance = true)
    {
        isPlayerMoving = true;  // Player starts moving
        // Get the current Y position of the token (to maintain it during the movement)
        float originalY = token.transform.position.y;
        HexCell previousHex = token.GetCurrentHex();
        if (previousHex != null)
        {
            // Debug.Log($"Token leaving hex: {previousHex.name}");
            previousHex.isAttackOccupied = false;
            previousHex.isDefenseOccupied = false;
            previousHex.ResetHighlight();
        }
        else
        {
            Debug.LogError("Previous hex is null. Token might not be assigned to a valid hex.");
        }
        // Loop through each hex in the path
        foreach (HexCell step in path)
        {
            Vector3 startPosition = token.transform.position;  // Starting position for the current hex
            Vector3 targetPosition = new Vector3(step.GetHexCenter().x, originalY, step.GetHexCenter().z);  // Target position for the next hex
            float t = 0;  // Timer for smooth transition
            float moveDuration = 0.3f;  // Duration of the movement between hexes
            // Smoothly move the token between hexes
            while (t < 1f)
            {
                t += Time.deltaTime / moveDuration;
                token.transform.position = Vector3.Lerp(startPosition, targetPosition, t);  // Interpolate the position
                // If the player is carrying the ball, move the ball alongside the player
                if (!highPassManager.isWaitingForDefenderSelection && !highPassManager.isWaitingForDefenderMove && ball.GetCurrentHex() == previousHex)
                {
                    // Move the ball alongside the player, keeping the correct Y offset
                    Vector3 ballPosition = new Vector3(token.transform.position.x, ball.playerHeightOffset, token.transform.position.z);
                    ball.transform.position = ballPosition;  // Move the ball along with the token
                }
                yield return null;  // Wait for the next frame
            }
            // Update the token's hex after reaching the next hex
            token.SetCurrentHex(step);
            // If the player is carrying the ball, move the ball along with the player
            if (!highPassManager.isWaitingForDefenderSelection && !highPassManager.isWaitingForDefenderMove && ball.GetCurrentHex() == previousHex)
            {
                ball.SetCurrentHex(step);  // Update ball's hex to the current step
                Debug.Log($"Ball is at: {ball.GetCurrentHex().name}");
                ball.AdjustBallHeightBasedOnOccupancy();  // Adjust ball's height
            }
            ball.AdjustBallHeightBasedOnOccupancy();
            // 🛑 CHECK IF THE BALL ENTERED THE PENALTY BOX
            if (previousHex.isInPenaltyBox == 0 && step.isInPenaltyBox != 0 && token.IsDribbler && goalKeeperManager.ShouldGKMove(step))
            {
                Debug.Log("⚽ Ball entered penalty box during dribble! Offering GK a free move.");
                if (goalKeeperManager.ShouldGKMove(step))
                {
                    yield return StartCoroutine(goalKeeperManager.HandleGKFreeMove());
                }
            }
            previousHex = step;  // Set the previous hex to the current step for the next iteration
        }
        token.SetCurrentHex(path.Last());  // Update the token's hex to the final step
        Debug.Log($"Token arrived at hex: {path.Last().name}");
        // Mark the final hex as occupied after the token reaches the destination
        HexCell finalHex = path[path.Count - 1];
        if (token.isAttacker) finalHex.isAttackOccupied = true;  // Mark the target hex as occupied by an attacker
        else finalHex.isDefenseOccupied = true;  // Mark the target hex as occupied by a defender
        // Check if the player landed on the ball hex, adjust the ball height if necessary
        if (finalHex == ball.GetCurrentHex())
        {
            MatchManager.Instance.UpdatePossessionAfterPass(finalHex);
            isBallPickable = false;
        }
        if (shouldCountForDistance) {
            MatchManager.Instance.gameData.gameLog.LogEvent(
                token
                , MatchManager.ActionType.Move
                , value: path.Count
            );
        }
        // Clear highlighted hexes after movement is completed
        hexGrid.ClearHighlightedHexes();
        finalHex.ResetHighlight();
        ball.AdjustBallHeightBasedOnOccupancy();
        isPlayerMoving = false;  // Player finished moving
    }

    public void AdvanceMovementPhase()
    {
        if (!isWaitingForInterceptionDiceRoll
            && !isWaitingForTackleDecisionWithoutMoving
            && !isWaitingForTackleDecision
            && !isWaitingForTackleRoll
            && !isWaitingForReposition
            && !isWaitingForNutmegDecision
        )
        {
            Debug.Log("AdvanceMovementPhase: Clearing defendersTriedToIntercept.");
            defendersTriedToIntercept.Clear();
            remainingDribblerPace = 0;
            // Check for the 2f2 special phase
            if (isMovementPhase2f2)
            {
                attackersMovedIn2f2++;
                if (attackersMovedIn2f2 >= maxAttackerMovesIn2f2)
                {
                    Debug.Log("Last two attackers have moved in 2f2 phase. Ending Movement Phase.");
                    EndMovementPhase();
                    return;
                }
            }
            // Check for defender movement phase
            if (isMovementPhaseDef)
            {
                defendersMoved++;
                if (defendersMoved >= maxDefenderMoves)
                {
                    Debug.Log("All defenders have moved. Ready for Movement Phase 2f2.");
                    selectedToken = null;
                    hexGrid.ClearHighlightedHexes();
                    isMovementPhaseDef = false;
                    isMovementPhase2f2 = true;
                    return;
                }
            }
            // Check for attacker movement phase
            if (isMovementPhaseAttack)
            {
                attackersMoved++;
                if (attackersMoved >= maxAttackerMoves)
                {
                    Debug.Log("All attackers have moved. Switching to Defensive Movement Phase.");
                    selectedToken = null;
                    hexGrid.ClearHighlightedHexes();
                    isMovementPhaseAttack = false;
                    isMovementPhaseDef = true;
                    return;
                }
            }
        }
        else
        {
            Debug.LogWarning("AdvanceMovementPhase: Waiting for dice rolls or repositioning.");
        }
    }

    public void ForfeitTeamMovementPhase()
    {
        Debug.Log("Trying to Forfeit Movement.");
        if (isMovementPhase2f2)
        {
            if (isDribblerRunning)
            {
                Debug.Log($"{selectedToken.name} does not want to run with the ball any more.");
                isDribblerRunning = false;
                hexGrid.ClearHighlightedHexes();
                movedTokens.Add(selectedToken);
                nutmeggableDefenders.Clear();
                AdvanceMovementPhase(); // End dribbler's movement
            }
            else 
            {
                Debug.Log($"No more tokens moving in 2f2 phase. Ending Movement Phase.");
                EndMovementPhase();
            }
        }
        if (isMovementPhaseDef)
        {
            Debug.Log($"No more defenders wish to move. Attack gets 2f2 move.");
            defendersMoved = maxDefenderMoves-1;
            AdvanceMovementPhase();  // Reset the movement phase
        }
        if (isMovementPhaseAttack)
        {
            if (isDribblerRunning)
            {
                Debug.Log($"{selectedToken.name} does not want to run with the ball any more.");
                isDribblerRunning = false;
                hexGrid.ClearHighlightedHexes();
                movedTokens.Add(selectedToken);
                AdvanceMovementPhase(); // End dribbler's movement
            }
            else
            {
                Debug.Log($"No more Attackers wish to move. Defence's turn to move up to 5 tokens.");
                attackersMoved = maxAttackerMoves-1;
                AdvanceMovementPhase();  // Reset the movement phase
            }
        }
    }
    
    private List<PlayerToken> GetEligibleDefendersForInterception(HexCell targetHex)
    {
        Debug.Log("Calculating Eligible Defenders for Interception");
        List<PlayerToken> eligibleDefs = new List<PlayerToken>();
        HexCell[] neighbors = targetHex.GetNeighbors(hexGrid);
        foreach (HexCell neighbor in neighbors)
        {
            PlayerToken token = neighbor.GetOccupyingToken();
            if (token != null && // null check
                !eligibleDefs.Contains(token) && // Avoid duplicates
                neighbor.isDefenseOccupied && // only defenders
                !defendersTriedToIntercept.Contains(token) && // has not already tried to intercept during the dribblers movement
                !headerManager.defenderWillJump.Contains(token) && // has not jumped in previous Header Challenge
                !stunnedTokens.Contains(token) && // ignore nutmegged defenders may cause problems in Loose ball.
                !stunnedforNext.Contains(token) &&
                !headerManager.attackerWillJump.Contains(token) && // wtf is this? we are looking for defenders.
                // if we are in MPDef, get only not moved, if in MP2f2, get all
                (!movedTokens.Contains(token) || MatchManager.Instance.currentState == MatchManager.GameState.MovementPhase2f2)
            )
            {
                eligibleDefs.Add(token);
            }
        }

        return eligibleDefs;
    }

    private void StartBallInterceptionDiceRollSequence(HexCell targetHex, List<PlayerToken> eligibleDefenders)
    {
        if (eligibleDefenders == null || eligibleDefenders.Count == 0)
        {
            Debug.LogError("No eligible defenders for interception. Interception sequence aborted.");
            return;
        }
        selectedDefender = eligibleDefenders[0];
        if (selectedDefender == null)
        {
            Debug.LogError("Failed to assign a valid defender from the eligible defenders list.");
            return;
        }
        Debug.Log($"Selected defender for interception: {selectedDefender.name}. Press R to roll...");
        // Set the flag to wait for interception dice roll
        isWaitingForInterceptionDiceRoll = true;
    }

    public IEnumerator PerformBallInterceptionDiceRoll(int? rigroll = null)
    {
        Debug.Log("PerformBallInterceptionDiceRoll Runs");
        if (selectedDefender != null)
        {
            // Roll the dice (1 to 6)
            var (returnedRoll, returnedJackpot) = helperFunctions.DiceRoll();
            int diceRoll = rigroll ?? returnedRoll;
            // Debug.Log($"Dice roll by defender at {selectedDefender.GetCurrentHex().coordinates}: {diceRoll}");
            isWaitingForInterceptionDiceRoll = false;

            // Get the defender's tackling attribute
            int defenderTackling = selectedDefender.tackling;
            Debug.Log($"Defender: {selectedDefender.name}, Tackling: {defenderTackling}, Dice Roll: {diceRoll}");

            // Check if there was a foul
            if (isDribblerRunning && diceRoll <= FOUL_THRESHOLD)
            {
                Debug.Log("Defender committed a foul.");
                eligibleDefenders.Remove(selectedDefender);
                defendersTriedToIntercept.Add(selectedDefender);
                yield return StartCoroutine(HandleFoulProcess(selectedToken, selectedDefender));
            }
            // Check interception condition: either roll a 6 or roll + tackling >= 10
            else if (diceRoll == 6 || (diceRoll + defenderTackling >= INTERCEPTION_THRESHOLD))
            {
                // Defender successfully intercepts the ball
                Debug.Log($"Ball intercepted by {selectedDefender.name} at {selectedDefender.GetCurrentHex().coordinates}!");
                StartCoroutine(HandleBallInterception(selectedDefender.GetCurrentHex()));
                ResetBallInterceptionDiceRolls();
            }
            else
            {
                Debug.Log($"{selectedDefender.name} at {selectedDefender.GetCurrentHex().coordinates} failed to intercept.");
                // Move to the next defender, if any
                eligibleDefenders.Remove(selectedDefender);
                defendersTriedToIntercept.Add(selectedDefender);
                if (eligibleDefenders.Count > 0)
                {
                    selectedDefender = eligibleDefenders[0];  // Move to the next defender
                    isWaitingForInterceptionDiceRoll = true;
                    Debug.Log($"Selected defender for interception: {selectedDefender.name}. Press R to roll...");
                }
                else if (isDribblerRunning)
                {
                    Debug.Log("No more defenders to roll.");
                    // HandleTokenSelection(selectedToken);
                    ContinueDribblerMovement();
                }
                else
                {
                    AdvanceMovementPhase();
                }
            }
        }
        else
        {
            Debug.LogError("selectedDefender is null during dice roll.");
        }
    }

    private IEnumerator HandleBallInterception(HexCell defenderHex)
    {
        yield return StartCoroutine(groundBallManager.HandleGroundBallMovement(defenderHex));  // Move the ball to the defender's hex

        // Change possession to the defending team
        MatchManager.Instance.ChangePossession();  
        MatchManager.Instance.UpdatePossessionAfterPass(defenderHex);  // Update possession
        EndMovementPhase(true);
        MatchManager.Instance.currentState = MatchManager.GameState.LooseBallPickedUp;
    }

    private void PerformTackleDiceRoll(bool isDefender, int? rigroll = null)
    {
        var (returnedRoll, returnedJackpot) = helperFunctions.DiceRoll();
        int roll = returnedJackpot ? 50 :returnedRoll;
        int diceRoll = rigroll ?? roll;
        
        // Random
        if (isDefender)
        {
            defenderDiceRoll = diceRoll;
            tackleDefenderRolled = true;
            if (returnedJackpot) Debug.Log($"Defender rolled A JACKPOT!!!");
            else Debug.Log($"Defender rolled: {defenderDiceRoll}. Now it's the attacker's turn.");
        }
        else
        {
            attackerDiceRoll = diceRoll;
            tackleAttackerRolled = true;
            if (returnedJackpot) Debug.Log($"Defender rolled A JACKPOT!!!");
            else Debug.Log($"Attacker rolled: {attackerDiceRoll}. Comparing results...");
            StartCoroutine(CompareTackleRolls());  // Compare the rolls after both rolls are complete
        }
    }

    private IEnumerator CompareTackleRolls()
    {
        ResetTacklePhase();  // Reset tackle phase after handling results
        // Ensure selected tokens are valid
        if (isNutmegInProgress)
        {
            Debug.Log("This is a nutmeg attempt");
            selectedDefender = nutmegVictim;
        }
        if (selectedDefender == null)
        {
            Debug.LogError("Error: Defender token is not set!");
            yield break;
        }
        PlayerToken attackerToken = ball.GetCurrentHex()?.GetOccupyingToken();
        if (attackerToken == null)
        {
            Debug.LogError("Error: No attacker token found on the ball's hex!");
            yield break;
        }
        // Retrieve the dribbling and tackling values
        int defenderTackling = selectedDefender.tackling;
        int attackerDribbling = attackerToken.dribbling;
        int defenderTotalScore = defenderDiceRoll == 50 ? defenderDiceRoll : selectedDefender.tackling + defenderDiceRoll + (isNutmegInProgress ? 1 : 0);
        int attackerTotalScore = attackerDiceRoll == 50 ? attackerDiceRoll : attackerToken.dribbling + attackerDiceRoll;

        Debug.Log($"Defender Name: {selectedDefender.name} with tackling: {defenderTackling}, Attacker: {attackerToken.name} with Dribbling: {attackerDribbling}");
        if (defenderDiceRoll <= FOUL_THRESHOLD)
        {
            Debug.Log("Defender committed a foul.");
            yield return StartCoroutine(HandleFoulProcess(attackerToken, selectedDefender));
            yield break;  // End tackle resolution as the foul process takes over
        }
        else if (defenderTotalScore > attackerTotalScore)
        {
            if (defenderTotalScore == 50) Debug.Log($"Tackle failed! {selectedDefender.name} Rolled a JACKPOT!! Attacker {attackerToken.name}'s Roll({attackerDiceRoll})+Dribbling({attackerDribbling}) = {attackerTotalScore} is left helpless and loses possession of the ball.");
            else Debug.Log($"Tackle failed! {selectedDefender.name} Roll({defenderDiceRoll})+Tackling({defenderTackling})" + (isNutmegInProgress ? "+Nutmeg bonus(1)" : "")+ $"={defenderTotalScore} beats {attackerToken.name}'s Roll({attackerDiceRoll})+Dribbling({attackerDribbling}) = {attackerTotalScore}, and wins possession of the ball.");
            if(isNutmegInProgress)
            {
                Debug.Log($"{attackerToken.name} will be stunned in next Movement Phase");
                stunnedforNext.Add(attackerToken);
            }
            ball.SetCurrentHex(selectedDefender.GetCurrentHex());
            yield return StartCoroutine(groundBallManager.HandleGroundBallMovement(selectedDefender.GetCurrentHex()));  // Move the ball to the defender's hex
            MatchManager.Instance.ChangePossession();  // Change possession to the defender's team
            MatchManager.Instance.UpdatePossessionAfterPass(selectedDefender.GetCurrentHex());  // Update possession
            Debug.Log($"{selectedDefender.name} can reposition!");
            repositionWinner = selectedDefender;
            repositionLoser = attackerToken;
            yield return StartCoroutine(HandlePostTackleReposition());
            if (isNutmegInProgress)
            {
                nutmegVictim = null;
                isNutmegInProgress = false;
            }
            EndMovementPhase(true);
            MatchManager.Instance.currentState = MatchManager.GameState.SuccessfulTackle;
            Debug.Log("Movement phase ended due to successful tackle.");
        }
        else if (defenderTotalScore < attackerTotalScore)
        {
            // Defender Loses and gets stunned
            if (attackerTotalScore == 50) Debug.Log($"{attackerToken.name} rolled A JACKPOT!!! {selectedDefender.name} Roll({defenderDiceRoll})+Tackling({defenderTackling})" + (isNutmegInProgress ? "+Nutmeg bonus(1)" : "")+ $"={defenderTotalScore}. {attackerToken.name} retains possession of the ball.");
            else Debug.Log($"Tackle failed! {selectedDefender.name} Roll({defenderDiceRoll})+Tackling({defenderTackling})" + (isNutmegInProgress ? "+Nutmeg bonus(1)" : "")+ $"={defenderTotalScore} loses to {attackerToken.name}'s Roll({attackerDiceRoll})+Dribbling({attackerDribbling}) = {attackerTotalScore}, who retains possession of the ball.");
            yield return StartCoroutine(PrepareAttackerReposition(attackerToken));
        }
        else if (defenderTotalScore == attackerTotalScore)
        {
            if (defenderTotalScore == 50) Debug.Log("Oh, my! A DOUBLE JACKPOT!! Loose Ball situation from the defender");
            else Debug.Log($"{selectedDefender.name} Roll({defenderDiceRoll})+Tackling({defenderTackling})" + (isNutmegInProgress ? "+Nutmeg bonus(1)" : "")+ $"={defenderTotalScore} is equal to {attackerToken.name}'s Roll({attackerDiceRoll})+Dribbling({attackerDribbling}) = {attackerTotalScore}. Tackle results in a tie. Loose ball situation from the defender Hex.");
            isDribblerRunning = false;
            isNutmegInProgress = false;
            nutmegVictim = null;
            remainingDribblerPace = 0;
            // TODO: in case of a Loose Ball, is the attacker stunned? I think not!
            StartCoroutine(looseBallManager.ResolveLooseBall(selectedDefender, "ground"));
        }
 
    }

    private IEnumerator PrepareAttackerReposition(PlayerToken attackerToken)
    {
        if (MatchManager.Instance.currentState == MatchManager.GameState.MovementPhase2f2)
        {
            Debug.Log($"{selectedDefender.name} will be stunned in the next Movement Phase");
            stunnedforNext.Add(selectedDefender);
        }
        else if (MatchManager.Instance.currentState == MatchManager.GameState.MovementPhaseAttack)
        {
            Debug.Log($"{selectedDefender.name} will be stunned in the Defensive Part of the Movement Phase");
            stunnedTokens.Add(selectedDefender);
        }
        Debug.Log($"{attackerToken.name} can reposition!");
        if (isNutmegInProgress)
        {
            Debug.Log($"Reducing {attackerToken.name}'s remaining Pace by 2 due to the Nutmeg");
            // TODO: Add log for two paces
            remainingDribblerPace -=2;
            remainingDribblerPace = Mathf.Max(remainingDribblerPace, 0);
            // Clamp remainingDribblerPaceto 0 and adjust isDribblerRunning accordingly.
            if (remainingDribblerPace == 0) {isDribblerRunning = false;}
        }
        repositionWinner = attackerToken;
        repositionLoser = selectedDefender;
        yield return StartCoroutine(HandlePostTackleReposition());
        if (isNutmegInProgress)
        {
            nutmegVictim = null;
            isNutmegInProgress = false;
        }
    }
    
    private List<HexCell> FindRepositionHexes()
    {
        HexCell loserHex = repositionLoser.GetCurrentHex();
        HexCell[] repositionHexesArray = loserHex.GetNeighbors(hexGrid);
        repositionHexes = new List<HexCell>(repositionHexesArray); // Convert array to list
        repositionHexes.Where(hex => 
            hex != loserHex && !hex.isDefenseOccupied && !hex.isAttackOccupied).ToList();
        return repositionHexes;
    }

    private IEnumerator HandlePostTackleReposition()
    {
        Debug.Log($"{repositionWinner.name} won the tackle and is repositioning around {repositionLoser.name}. Click a Highlighted Hex to move there or Press X to stay put.");
        // Get the loser's hex and neighboring hexes
        FindRepositionHexes();
        if (isNutmegInProgress)
        {
            Debug.Log("Leaving as repositionable Hexes only the nutmeggableHexes");
            HexCell winnerHex = repositionWinner.GetCurrentHex();
            HexCell[] nutmegHexesArray = winnerHex.GetNeighbors(hexGrid);
            List<HexCell> nutmegUnavailable = new List<HexCell>(nutmegHexesArray); // Convert array to list
            repositionHexes.RemoveAll(thomas => nutmegUnavailable.Contains(thomas));
        }
        // Highlight repositioning options
        foreach (HexCell hex in repositionHexes)
        {
            if (!hex.isDefenseOccupied && !hex.isAttackOccupied)
            {
                hex.HighlightHex("reposition");
                hexGrid.highlightedHexes.Add(hex);
            }
        }
        isWaitingForReposition = true;
        while (isWaitingForReposition)
        {
            yield return null;
        }
    }

    private async void AsyncRepositionTokenToHex(HexCell hex)
    {
        Debug.Log($"{repositionWinner.name} repositioning to {hex.coordinates}.");
        HexCell winnerHex = repositionWinner.GetCurrentHex();
        // the check seems to be redundant as before the call of this method, the Possession has been changed if needed
        if (repositionWinner.isAttacker)
        {
            winnerHex.isAttackOccupied = false;
            winnerHex.ResetHighlight();
            hex.isAttackOccupied = true;  // Mark the target hex as occupied by an attacker
        }
        else
        {
            winnerHex.isDefenseOccupied = false;
            winnerHex.ResetHighlight();
            hex.isDefenseOccupied = true;  // Mark the target hex as occupied by an attacker
        }
        foreach (HexCell needToresetHighlightHex in hexGrid.highlightedHexes) needToresetHighlightHex.ResetHighlight();
        hexGrid.ClearHighlightedHexes();
        await StartCoroutineAndWait(repositionWinner.JumpToHex(hex)); // Move the token
        ball.PlaceAtCell(hex); // Move the ball
        // clickedHex.ResetHighlight();
        // clickedHex.HighlightHex("isAttackOccupied");
        Debug.Log("Repositioning complete.");
        isWaitingForReposition = false;
        if (winnerHex.isInPenaltyBox == 0 && hex.isInPenaltyBox != 0 && repositionWinner.IsDribbler)
        {
            Debug.Log("⚽ Ball entered penalty box during a reposition! Offering GK a free move.");
            if (goalKeeperManager.ShouldGKMove(hex))
            {
                await StartCoroutineAndWait(goalKeeperManager.HandleGKFreeMove());
            }
        }
        DribblerMoved1HexOrReposition();
    }

    private IEnumerator HandleFoulProcess(PlayerToken attackerToken, PlayerToken defenderToken)
    {
        Debug.Log("Handling foul resolution process...");
        // Phase 1: Yellow card decision
        Debug.Log("Press 'R' to roll for a Booking.");
        isWaitingForYellowCardRoll = true;
        // Introduce a brief delay to ensure previous keypresses are ignored
        yield return null;
        while (isWaitingForYellowCardRoll)
        {
            yield return null;  // Wait for the next frame
        }
        // Phase 2: Injury decision
        Debug.Log("Press 'R' to roll for attacker injury.");
        isWaitingForInjuryRoll = true;
        yield return null;   // Delay to avoid immediate triggering
        while (isWaitingForInjuryRoll)
        {
            yield return null;  // Wait for the next frame
        }
        Debug.Log("Foul process completed.");
        yield return null;   // Delay to avoid immediate triggering
        // Phase 3: Foul decision (Play On or Take the Foul)
        Debug.Log("Press 'A' to Play On or 'Z' to Take the Foul.");
        isWaitingForFoulDecision = true;
        while (isWaitingForFoulDecision)
        {
            yield return null;  // Wait for the next frame
        }
    }

    private async void PlayAdvantage()
    {
        isWaitingForFoulDecision = false;
        PlayerToken attackerToken = MatchManager.Instance.LastTokenToTouchTheBallOnPurpose;
        await StartCoroutineAndWait(PrepareAttackerReposition(attackerToken));
        AdvanceMovementPhase();
    }

    private void TakeFreeKick()
    {
        Debug.Log("Attacker chooses to take the foul. Transitioning to Free Kick.");
        isWaitingForFoulDecision = false;  // Cancel the decision phase
        // End the movement phase and start the free kick process
        EndMovementPhase();  // End the movement phase
        freeKickManager.StartFreeKickPreparation();
    }

    public void PerformLeniencyTest(int? rigroll = null)
    {
        var (returnedRoll, returnedJackpot) = helperFunctions.DiceRoll();
        int roll = rigroll ?? returnedRoll;
        Debug.Log($"Yellow card roll: {roll}");
        if (roll >= MatchManager.Instance.refereeLeniency)
        {
            Debug.Log($"Defender {selectedDefender.name} receives a yellow card!");
            selectedDefender.ReceiveYellowCard();  // Assume a method exists to handle this
        }
        else
        {
            Debug.Log($"Defender {selectedDefender.name} escapes a yellow card.");
        }
        isWaitingForYellowCardRoll = false;
    }
    public void PerformInjuryTest(int? rigroll = null)
    {
        var (returnedRoll, returnedJackpot) = helperFunctions.DiceRoll();
        int roll = rigroll ?? returnedRoll;
        Debug.Log($"Injury roll: {roll}");
        // PlayerToken attackerToken = ball.GetCurrentHex()?.GetOccupyingToken();
        PlayerToken attackerToken = MatchManager.Instance.LastTokenToTouchTheBallOnPurpose;
        if (roll >= attackerToken.resilience)
        {
            Debug.Log($"Attacker {attackerToken.name} is injured!");
            attackerToken.ReceiveInjury();  // Assume a method exists to handle this
            remainingDribblerPace -= 1;
        }
        else
        {
            Debug.Log($"Attacker {attackerToken.name} avoids injury.");
        }
        isWaitingForInjuryRoll = false;
    }

    private void StartTackleDiceRollSequence()
    {
        // Reset the tackle dice roll flags
        tackleDefenderRolled = false;
        tackleAttackerRolled = false;
        // Set flag to wait for dice rolls
        isWaitingForTackleRoll = true;
        Debug.Log("Press R to roll the dice for tackle. Defender rolls first.");
    }

    private void ResetTacklePhase()
    {
        isWaitingForTackleDecision = false;
        isWaitingForTackleRoll = false;
        tackleDefenderRolled = false;
        tackleAttackerRolled = false;
        Debug.Log("Tackle phase reset.");
    }

    public void ResetMovementPhase()
    {
        movedTokens.Clear();  // Reset the list of moved tokens
        attackersMoved = 0;    // Reset the number of attackers that have moved
        defendersMoved = 0;    // Reset the number of defenders that have moved
        attackersMovedIn2f2 = 0;  // Reset the 2f2 phase counter
        selectedToken = null;  // Reset the selected token
        selectedDefender = null;
        isBallPickable = false;
        isDribblerRunning = false;
        tokenPickedUpBall = false;
        isActivated = false;
        isMovementPhaseAttack = false;  // Reset the movement phase state
        isMovementPhaseDef = false;  // Reset the movement phase state
        isMovementPhase2f2 = false;  // Reset the movement phase state
        isAwaitingTokenSelection = false;  // Reset the token selection state
        isAwaitingHexDestination = false;  // Reset the hex destination state
        Debug.Log("Movement phase has been reset.");
    }

    private void ResetBallInterceptionDiceRolls()
    {
        defenderHexesNearBall.Clear();
        selectedDefender = null;
        eligibleDefenders.Clear();
        isWaitingForInterceptionDiceRoll = false;
    }

    public void EndMovementPhase(bool triggerF3 = true)
    {
        MatchManager.Instance.currentState = MatchManager.GameState.MovementPhaseEnded;  // Stop all movements
        // TODO: Broadcast Ennd of MovementPhase
        ResetMovementPhase();  // Reset the moved tokens and phase counters
        nutmeggableDefenders.Clear();
        stunnedTokens.Clear();
        stunnedTokens.AddRange(stunnedforNext);
        stunnedforNext.Clear();
        headerManager.ResetHeader();  // Reset the header to free up unmovable players
        isActivated = false;
        Debug.Log("Movement phase is over.");
        if (triggerF3) finalThirdManager.TriggerFinalThirdPhase();
    }

    public List<PlayerToken> GetNutmeggableDefenders(PlayerToken dribbler, HexGrid hexGrid)
    {
        Debug.Log("Checking for nutmeggable Defenders");
        List<PlayerToken> nutmeggableList = new List<PlayerToken>();

        if (dribbler == null || !dribbler.IsDribbler)
        {
            Debug.Log("Nutmeg is not available: Dribbler is null, not active.");
            return nutmeggableList;
        }

        HexCell dribblerHex = dribbler.GetCurrentHex();
        if (dribblerHex == null)
        {
            Debug.LogError("Dribbler's current hex is null.");
            return nutmeggableList;
        }

        // Get all neighbors of the dribbler
        HexCell[] dribblerNeighbors = dribblerHex.GetNeighbors(hexGrid);

        // Identify defenders in the dribbler's neighbors
        foreach (HexCell neighborHex in dribblerNeighbors)
        {
            PlayerToken defender = neighborHex?.GetOccupyingToken();

            // Skip null or non-defender tokens
            if (
                defender == null // ignore unoccupied Hexes
                || defender.isAttacker // Ignore Defenders
                || stunnedTokens.Contains(defender) // Ignored stunned Defenders
                || stunnedforNext.Contains(defender) // Ignore Stunned defenders for next
            )
            {
                continue;
            }

            // Check if there's at least one valid hex beyond the defender for the dribbler to land
            bool validNutmegHexFound = false;
            foreach (HexCell defenderNeighborHex in neighborHex.GetNeighbors(hexGrid))
            {
                // A valid landing hex must:
                // 1. Be in bounds
                // 2. Not be occupied
                // 3. Not be in the dribbler's neighbors
                if (defenderNeighborHex != null &&
                    !defenderNeighborHex.isAttackOccupied &&
                    !defenderNeighborHex.isDefenseOccupied &&
                    !dribblerNeighbors.Contains(defenderNeighborHex))
                {
                    validNutmegHexFound = true;
                    break; // No need to check further; at least one valid hex is sufficient
                }
            }

            if (validNutmegHexFound)
            {
                nutmeggableList.Add(defender);
            }
        }
        if (nutmeggableList.Count() > 0) {Debug.Log($"Nutmeg options: {string.Join(", ", nutmeggableList.Select(d => d.name))}");}
        return nutmeggableList;
    }

    public void StartNutmegVictimIdentification()
    {
        Debug.Log("Starting Nutmeg Victim Identification...");
        lookingForNutmegVictim = true ;
        if (nutmeggableDefenders.Count == 1)
        {
            nutmegVictim = nutmeggableDefenders[0];
            Debug.Log($"Only one nutmeggable defender: {nutmegVictim.name}. Proceeding with nutmeg.");
            lookingForNutmegVictim = false;
            StartNutmegProcess();
            return;
        }
        // HighlightNutmeggableDefenders();
        Debug.Log($"Multiple nutmeggable defenders found: {nutmeggableDefenders.Count}. Waiting for player selection.");
    }
    
    public void HandleNutmegVictimSelection(PlayerToken potentialVictim)
    {
        if (potentialVictim == null)
        {
            Debug.LogError("Victim Provided is not a token");
            return;
        }
        else if (potentialVictim.isAttacker)
        {
            Debug.LogError("This is an Attacker, please click on one of the nutmeggable defenders");
            return;
        }
        else if (!nutmeggableDefenders.Contains(potentialVictim))
        {
            Debug.LogError("This is not a nutmeggable Defender, please click on one of the nutmeggable defenders");
            return;
        }
        else
        {
            Debug.Log($"{potentialVictim.name} has been selected as the nutmeg victim. Proceeding with nutmeg.");
            lookingForNutmegVictim = false;
            nutmegVictim = potentialVictim;
            // hexGrid.ClearHighlightedHexes(); // In case we highlight Nutmeggable Defenders.
            // TODO: Highlight Nutmeggable Defenders Hexes
            StartNutmegProcess();
        }
    }
    
    private void HighlightNutmeggableDefenders()
    {
        foreach (PlayerToken nutmegdef in nutmeggableDefenders)
        {
            HexCell nutmegHex = nutmegdef.GetCurrentHex();
            if (nutmegHex == null) continue; // to next hex (loop)
            nutmegHex.HighlightHex("nutmeggableDef");
            hexGrid.highlightedHexes.Add(nutmegHex);  // Track the highlighted hexes
        }
    }

    public void StartNutmegProcess()
    {
        isNutmegInProgress = true;
        isWaitingForTackleRoll = true;
        StartTackleDiceRollSequence();
    }
    

    public bool IsDribblerinOpponentPenaltyBox(PlayerToken token)
    {
        bool DribberIsInOpponentPenaltyBox = false;
        MatchManager.TeamAttackingDirection attackingDirection;
        if (MatchManager.Instance.teamInAttack == MatchManager.TeamInAttack.Home)
        {
            attackingDirection = MatchManager.Instance.homeTeamDirection;
        }
        else
        {
            attackingDirection = MatchManager.Instance.awayTeamDirection;
        }
        // If dribbler is in opponent's Penalty Box!
        if (
            (
                attackingDirection == MatchManager.TeamAttackingDirection.LeftToRight // Attackers shoot to the Right
                && ballHex.isInPenaltyBox == 1 // In Right PenaltyBox
                && token.GetCurrentHex().coordinates.x > 0 // Dribbler is in the right half of pitch
            )
            ||
            (
                attackingDirection == MatchManager.TeamAttackingDirection.RightToLeft // Attackers shoot to the Left
                && ballHex.isInPenaltyBox == -1 // In Left PenaltyBox
                && token.GetCurrentHex().coordinates.x < 0 // Dribbler is in the left half of pitch
            )
        )
        {
          DribberIsInOpponentPenaltyBox = true;
        }
        return DribberIsInOpponentPenaltyBox;
    }

}
