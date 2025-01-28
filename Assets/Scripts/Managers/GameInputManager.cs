using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using Unity.VisualScripting;


public class GameInputManager : MonoBehaviour
{
    [Header("Dependencies")]
    public CameraController cameraController;  
    public GroundBallManager groundBallManager;
    public FirstTimePassManager firstTimePassManager;
    public LongBallManager longBallManager;
    public HighPassManager highPassManager;
    public MovementPhaseManager movementPhaseManager;
    public LooseBallManager looseBallManager;
    public HeaderManager headerManager;
    public FreeKickManager freeKickManager;
    public ShotManager shotManager;
    public Ball ball;  
    public HexGrid hexGrid; 
    public MatchManager matchManager;
    [Header("Layers")]
    public LayerMask tokenLayerMask;  // Layer for player tokens
    public LayerMask hexLayerMask;    // Layer for hex grid

    private Vector3 mouseDownPosition;  
    private bool isDragging = false;    
    public float dragThreshold = 10f;   

    void Start()
    {

    }

    void Update()
    {
        cameraController.HandleCameraInput();
        HandleMouseInput();

        if (MatchManager.Instance.currentState == MatchManager.GameState.KickOffSetup && Input.GetKeyDown(KeyCode.Space))
        {
            MatchManager.Instance.StartMatch();
        }

        HandleSpecialInputs();
    }

    void HandleSpecialInputs()
    {
        // Handle Next Action Selection
        if (
            Input.GetKeyDown(KeyCode.P)
            // && MatchManager.Instance.currentState == MatchManager.GameState.SuccessfulTackle
        )
        {
            hexGrid.ClearHighlightedHexes(); 
            MatchManager.Instance.TriggerStandardPass();
        }
        else if (
            Input.GetKeyDown(KeyCode.M)
            && (
                MatchManager.Instance.currentState == MatchManager.GameState.LongBallCompleted
                || true
            )
        )
        {
            MatchManager.Instance.TriggerMovement();
        }
        else if (
            Input.GetKeyDown(KeyCode.C)
            // && MatchManager.Instance.currentState == MatchManager.GameState.SuccessfulTackle
        )
        {
            hexGrid.ClearHighlightedHexes(); 
            MatchManager.Instance.TriggerHighPass();
        }
        else if (Input.GetKeyDown(KeyCode.L))
        {
            hexGrid.ClearHighlightedHexes(); 
            MatchManager.Instance.TriggerLongPass();
        }
        else if (Input.GetKeyDown(KeyCode.F))
        {
            hexGrid.ClearHighlightedHexes(); 
            MatchManager.Instance.TriggerFTP();
        }
        // MovementPhase input handling
        if
        (
            !movementPhaseManager.isPlayerMoving &&
            !movementPhaseManager.isWaitingForTackleRoll &&
            !movementPhaseManager.isWaitingForTackleDecision &&
            !movementPhaseManager.isWaitingForInterceptionDiceRoll &&
            !movementPhaseManager.isWaitingForReposition &&
            !movementPhaseManager.isWaitingForInjuryRoll &&
            !movementPhaseManager.isWaitingForYellowCardRoll &&
            !movementPhaseManager.isWaitingForFoulDecision &&
            (
                MatchManager.Instance.currentState == MatchManager.GameState.MovementPhaseAttack || 
                MatchManager.Instance.currentState == MatchManager.GameState.MovementPhaseDef ||
                MatchManager.Instance.currentState == MatchManager.GameState.MovementPhase2f2
            )
        )
        {
            if (Input.GetKeyDown(KeyCode.X))
            {
                if (!movementPhaseManager.isWaitingForNutmegDecision 
                    && !movementPhaseManager.isWaitingForNutmegDecisionWithoutMoving
                )
                {
                    movementPhaseManager.ForfeitTeamMovementPhase();
                }
            }
            StartCoroutine(HandleMouseInputForMovement());
        }
        if (
                MatchManager.Instance.currentState == MatchManager.GameState.HighPassAttackerMovement || 
                MatchManager.Instance.currentState == MatchManager.GameState.HighPassDefenderMovement
        )
        {
            StartCoroutine(HandleMouseInputForHighPassMovement());
        }
        if (
                MatchManager.Instance.currentState == MatchManager.GameState.SnapshotPhase
        )
        {
            if (movementPhaseManager.isWaitingForSnapshotDecision)
            {
                if (Input.GetKeyDown(KeyCode.S))
                {
                    movementPhaseManager.isWaitingForSnapshotDecision = false;
                    Debug.Log($"Attacker decides to Snapshot!!!!");
                    shotManager.StartShotProcess(looseBallManager.ballHitThisToken, "snapshot");
                    looseBallManager.EndLooseBallPhase();
                }
                if (Input.GetKeyDown(KeyCode.X))
                {
                    movementPhaseManager.isWaitingForSnapshotDecision = false;
                    Debug.Log($"Attacker decides not to shoot..");
                    if (movementPhaseManager.isMovementPhaseInProgress) movementPhaseManager.AdvanceMovementPhase();
                }
            }
            StartCoroutine(HandleMouseInputForSnapMovement());
        }
        if (
                MatchManager.Instance.currentState == MatchManager.GameState.FirstTimePassAttackerMovement ||
                MatchManager.Instance.currentState == MatchManager.GameState.FirstTimePassDefenderMovement
        )
        {
            StartCoroutine(HandleMouseInputForFTPMovement());
        }
        if (MatchManager.Instance.currentState == MatchManager.GameState.HeaderAttackerSelection)
        {
            HandleAttackerHeaderSelectionInput();
        }
        else if (MatchManager.Instance.currentState == MatchManager.GameState.HeaderDefenderSelection)
        {
            HandleDefenderHeaderSelectionInput();
        }
        if (MatchManager.Instance.currentState.ToString().StartsWith("FreeKick"))
        {
            if (freeKickManager.isWaitingForKickerSelection)
            {
                HandleFreeKickKickerSelection();
            }
            else if (freeKickManager.isWaitingForSetupPhase)
            {
                HandleFreeKickSetupPhaseInput();
            }
            else if (freeKickManager.isWaitingForExecution)
            {
                if (Input.GetKeyDown(KeyCode.L))
                {
                    hexGrid.ClearHighlightedHexes(); 
                    MatchManager.Instance.TriggerLongPass();
                }
                else if (Input.GetKeyDown(KeyCode.C))
                {
                    hexGrid.ClearHighlightedHexes(); 
                    MatchManager.Instance.TriggerHighPass();
                }
                else if (Input.GetKeyDown(KeyCode.P))
                {
                    hexGrid.ClearHighlightedHexes(); 
                    MatchManager.Instance.TriggerStandardPass();
                }
                else if (Input.GetKeyDown(KeyCode.S))
                {
                    hexGrid.ClearHighlightedHexes();
                    Debug.Log("Free Kick Shoot triggered."); 
                    // TODO: Implement Free Kick Shoot
                }
            }
        }
    }

    void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            mouseDownPosition = Input.mousePosition;
            isDragging = false;
        }
        
        if (Input.GetMouseButton(0))
        {
            if (!isDragging && Vector3.Distance(mouseDownPosition, Input.mousePosition) > dragThreshold)
            {
                isDragging = true;
            }

            if (isDragging)
            {
                cameraController.HandleCameraInput();
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (!isDragging)
            {
                HandleClick();
            }
            isDragging = false;
        }
    }

    // This method handles the click logic for tokens and hexes
    void HandleClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // Check if we hit a token first
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, tokenLayerMask))
        {
            Ray hexRay = new Ray(hit.point + Vector3.up * 0.1f, Vector3.down);
            RaycastHit hexHit;

            // Raycast down to detect the hex beneath the token
            if (Physics.Raycast(hexRay, out hexHit, Mathf.Infinity, hexLayerMask))
            {
                HexCell clickedHex = hexHit.collider.GetComponent<HexCell>();
                if (clickedHex != null)
                {
                    HandleHexClick(clickedHex);
                }
            }
        }
        else if (Physics.Raycast(ray, out hit, Mathf.Infinity, hexLayerMask))
        {
            HexCell clickedHex = hit.collider.GetComponent<HexCell>();
            if (clickedHex != null)
            {
                HandleHexClick(clickedHex);
            }
        }
    }

    private void HandleHexClick(HexCell hex)
    {
        // Debug.Log($"Hex clicked: {hex.name}");

        // Check for Ground Ball and Long Ball state handling
        if (ball.IsBallSelected() && MatchManager.Instance.currentState == MatchManager.GameState.StandardPassAttempt)
        {
            groundBallManager.HandleGroundBallPath(hex, 11, true); // Normal Standard Pass
        }
        else if (ball.IsBallSelected() && MatchManager.Instance.currentState == MatchManager.GameState.FirstTimePassAttempt)
        {
            firstTimePassManager.HandleFTPBallPath(hex);
        }
        else if (ball.IsBallSelected() && MatchManager.Instance.currentState == MatchManager.GameState.LongBallAttempt)
        {
            longBallManager.HandleLongBallProcess(hex);
        }
        else if (ball.IsBallSelected() && MatchManager.Instance.currentState == MatchManager.GameState.HighPassAttempt)
        {
            highPassManager.HandleHighPassProcess(hex);
        }
    }

    public IEnumerator HandleMouseInputForMovement()
    {
        if (Input.GetMouseButtonDown(0))  // Only respond to left mouse click (not every frame)
        {
            Debug.Log("HandleMouseInputForMovement called on click");

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                Debug.Log("HandleMouseInputForMovement: Raycast hit something");

                Ball clickedBall = hit.collider.GetComponent<Ball>();
                HexCell clickedBallHex = null;
                PlayerToken clickedBallToken = null;
                if (clickedBall != null)
                {
                    clickedBallHex = clickedBall.GetCurrentHex(); 
                    clickedBallToken = clickedBallHex?.GetOccupyingToken();
                    if (clickedBallToken != null) {Debug.Log($"Raycast hit the Ball, and {clickedBallToken.name} controls it, on {clickedBallHex.name}");}
                    else {Debug.Log($"Raycast hit the Ball, on {clickedBallHex.name}");}
                }
                PlayerToken clickedToken = hit.collider.GetComponent<PlayerToken>();
                HexCell clickedTokenHex = null;
                if (clickedToken != null)
                {
                    clickedTokenHex = clickedToken.GetCurrentHex();
                    Debug.Log($"Raycast hit {clickedToken.name} on {clickedTokenHex.name}.");
                }
                HexCell clickedHex = hit.collider.GetComponent<HexCell>();
                PlayerToken tokenOnClickedHex = null;
                if (clickedHex != null)
                {
                    tokenOnClickedHex = clickedHex.GetOccupyingToken();
                    if (tokenOnClickedHex != null)
                    {
                        Debug.Log($"Raycast hit {clickedHex.name}, where {tokenOnClickedHex.name} is on");
                    }
                    else 
                    {
                        Debug.Log($"Raycast hit {clickedHex.name}, which is not occupied ");
                    }
                }
                PlayerToken inferredToken = clickedBallToken ?? clickedToken ?? tokenOnClickedHex ?? null;
                HexCell inferredHexCell = clickedBallHex ?? clickedTokenHex ?? clickedHex ?? null;
                Debug.Log($"Inferred Clicked Token: {inferredToken?.name}");
                Debug.Log($"Inferred Clicked Hex: {inferredHexCell.name}");

                // When do we need a expect a Token Selection?
                if (
                    inferredToken != null // A token was indeed inferred from the click
                    && !movementPhaseManager.isPlayerMoving // Wait for anumations to stop
                    && !movementPhaseManager.isDribblerRunning // The Dribbler has not started moving
                    && (
                        movementPhaseManager.selectedToken == null // MovementPhase does not have a selected Token.
                        || !movementPhaseManager.isDribblerRunning //  We Should not be able to reset the selected Token while the Dribbler is running.
                    )
                    && !movementPhaseManager.lookingForNutmegVictim // Do not handle a Token when looking for a victim
                    && !movementPhaseManager.isWaitingForNutmegDecision // Do not handle a Token when waiting for Nutmeg Decision
                    && !movementPhaseManager.isWaitingForNutmegDecisionWithoutMoving // Do not handle a Token when waiting for Nutmeg Decision without moving
                )
                {
                    Debug.Log($"Passing {inferredToken.name} to HandleTokenSelection");
                    movementPhaseManager.HandleTokenSelection(inferredToken);  // Select the token first
                    yield return null;
                }
                else if (
                    // While either we are waiting to nutmeg without moving
                    movementPhaseManager.isWaitingForNutmegDecisionWithoutMoving
                    // Or we are waiting to nutmeg while the dribbler has already started moving
                    || (movementPhaseManager.isDribblerRunning && movementPhaseManager.isWaitingForNutmegDecision)
                )
                {
                    // We clicked on a Nutmeggable Defender
                    if (movementPhaseManager.nutmeggableDefenders.Contains(inferredToken))
                    {
                        // Start the Nutmeg with the Selected nutmeggable Token
                        Debug.LogWarning("While waiting for a Nutmeg Decision, a nutmeggable Defender was clicked");
                        movementPhaseManager.isWaitingForSnapshotDecision = false;
                        movementPhaseManager.isWaitingForNutmegDecision = false;
                        movementPhaseManager.isWaitingForNutmegDecisionWithoutMoving = false;
                        movementPhaseManager.nutmegVictim = inferredToken;
                        movementPhaseManager.isDribblerRunning = true;
                        hexGrid.ClearHighlightedHexes();
                        Debug.Log($"Selected {inferredToken.name} to nutmeg. Proceeding with nutmeg.");
                        movementPhaseManager.lookingForNutmegVictim = false;
                        movementPhaseManager.StartNutmegProcess();
                        yield return null;
                    }
                    else
                    {
                        Debug.LogWarning("While waiting for a Nutmeg Decision, a nutmeggable Defender was not clicked");
                        if (movementPhaseManager.IsHexValidForMovement(inferredHexCell))
                        {
                            // Turning off wait for Nutmeg decision flags.
                            if (movementPhaseManager.isWaitingForTackleDecisionWithoutMoving)
                            {
                                movementPhaseManager.isWaitingForTackleDecisionWithoutMoving = false;
                            }
                            if (movementPhaseManager.isWaitingForNutmegDecisionWithoutMoving)
                            {
                                movementPhaseManager.isWaitingForNutmegDecisionWithoutMoving = false;
                            }
                            if (movementPhaseManager.isWaitingForSnapshotDecision)
                            {
                                movementPhaseManager.isWaitingForSnapshotDecision = false;
                            }
                            Debug.Log($"Passing {inferredHexCell.name} to MoveTokenToHex");
                            yield return StartCoroutine(movementPhaseManager.MoveTokenToHex(inferredHexCell));  // Move the selected token to the hex
                        }
                    }
                }
                else if (movementPhaseManager.lookingForNutmegVictim)
                {
                    // Nutmeg was selected with the Keyboard, and more than one nutmeggable Defender exists
                    Debug.Log($"Passing {inferredToken.name} to HandleNutmegVictimSelection");
                    movementPhaseManager.HandleNutmegVictimSelection(inferredToken);
                    yield return null;
                }
                else
                // We did not infer a Token (clicked on an Hex (or the ball on it) where there is no Token)
                // Clicked on a NOT OCCUPIED HEX
                {
                    if (!movementPhaseManager.isWaitingForNutmegDecision && !movementPhaseManager.isWaitingForNutmegDecisionWithoutMoving)
                    {
                        // If the hex is not occupied, check if it's valid for movement
                        if (movementPhaseManager.IsHexValidForMovement(inferredHexCell))
                        {
                            movementPhaseManager.isWaitingForSnapshotDecision = false;
                            bool temp_check = ball.GetCurrentHex() == inferredHexCell && movementPhaseManager.selectedToken != null;
                            if (temp_check)
                            {
                                movementPhaseManager.tokenPickedUpBall = true;
                            }
                            Debug.Log($"Passing {inferredHexCell.name} to MoveTokenToHex");
                            yield return StartCoroutine(movementPhaseManager.MoveTokenToHex(inferredHexCell));  // Move the selected token to the hex
                            if (temp_check)
                            {
                                movementPhaseManager.isBallPickable = false;
                            }
                        }
                    }
                }
            }
        }
        else if (movementPhaseManager.isBallPickable && Input.GetKeyDown(KeyCode.V))
        {
            movementPhaseManager.tokenPickedUpBall = true;
            movementPhaseManager.remainingDribblerPace = movementPhaseManager.selectedToken.pace;
            yield return StartCoroutine(movementPhaseManager.MoveTokenToHex(ball.GetCurrentHex()));
            movementPhaseManager.isBallPickable = false;
        }
        else if (movementPhaseManager.isWaitingForNutmegDecision)
        {
            if (Input.GetKeyDown(KeyCode.N))
            {
                movementPhaseManager.isWaitingForNutmegDecision = false;
                movementPhaseManager.isWaitingForSnapshotDecision = false;
                Debug.Log($"Starting Nutmeg Process.");
                movementPhaseManager.StartNutmegVictimIdentification();
            }
            else if (Input.GetKeyDown(KeyCode.X))
            {
                movementPhaseManager.isWaitingForNutmegDecision = false;
                Debug.Log($"Reject Nutmeg. Check for interceptions.");
                movementPhaseManager.ContinueFromRejectedNutmeg();
            }
        }
        else if (movementPhaseManager.isWaitingForNutmegDecisionWithoutMoving)
        {
            if (Input.GetKeyDown(KeyCode.N))
            {
                movementPhaseManager.isWaitingForNutmegDecisionWithoutMoving = false;
                movementPhaseManager.isWaitingForSnapshotDecision = false;
                Debug.Log($"Starting Nutmeg Process.");
                movementPhaseManager.StartNutmegVictimIdentification();
            }
        }
        else if (movementPhaseManager.isWaitingForSnapshotDecision)
        {
            if (Input.GetKeyDown(KeyCode.S))
            {
                movementPhaseManager.isWaitingForSnapshotDecision = false;
                Debug.Log($"Attacker decides to Snapshot!!!!");
                shotManager.StartShotProcess(movementPhaseManager.selectedToken, "snapshot");
            }
            if (Input.GetKeyDown(KeyCode.X))
            {
                movementPhaseManager.isWaitingForSnapshotDecision = false;
                Debug.Log($"Attacker decides not to shoot..");
            }
        }
    }

    public IEnumerator HandleMouseInputForHighPassMovement()
    {
        if (Input.GetMouseButtonDown(0))  // Only respond to left mouse click (not every frame)
        {
            Debug.Log("HandleMouseInputForHighPassMovement called on click");

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                Debug.Log("Raycast hit something");

                // Check if a player token was clicked
                PlayerToken token = hit.collider.GetComponent<PlayerToken>();
                if (token != null)
                {
                    Debug.Log($"PlayerToken {token.name} clicked");

                    // Attacker Phase: Ensure the token is an attacker
                    if (MatchManager.Instance.currentState == MatchManager.GameState.HighPassAttackerMovement && token.isAttacker)
                    {
                        // ** Targeting a Player
                        if (highPassManager.lockedAttacker != null)
                        { 
                            // Trying to move the locked Player Reject
                            if (token == highPassManager.lockedAttacker)
                            {
                                Debug.LogWarning($"This attacker {token.name} is locked and cannot be moved.");
                                // Clear previous highlights if locked attacker is clicked
                                hexGrid.ClearHighlightedHexes();
                                highPassManager.selectedToken = null;  // Reset selected token
                                yield return null;  // Exit to avoid selecting a locked attacker
                            }
                            else
                            {
                                // Trying to move anyone BUT the locked Player, Accept, Highlight and wait for click on Hex
                                Debug.Log($"Selecting attacker {token.name}. Highlighting reachable hexes.");
                                highPassManager.selectedToken = token;  // Set selected token
                                movementPhaseManager.HighlightValidMovementHexes(token, 3);  // Highlight reachable hexes within 3 moves
                                yield return null;
                            }
                        }
                        // ** Targeting a Hex Near One or more Players
                        // **Check if there are multiple eligible attackers**
                        else
                        {
                            if (highPassManager.eligibleAttackers != null && highPassManager.eligibleAttackers.Contains(token))
                            {
                                Debug.Log($"Eligible attacker {token.name} selected. Moving to the target hex.");
                                yield return StartCoroutine(movementPhaseManager.MoveTokenToHex(highPassManager.currentTargetHex, token, false));  // Move attacker to target hex
                                highPassManager.StartDefenderMovementPhase();  // Transition to defender phase
                                yield return null;  // Exit after attacker has moved
                            }
                            else if (highPassManager.eligibleAttackers != null && !highPassManager.eligibleAttackers.Contains(token))
                            {
                                Debug.LogWarning($"Ineligible attacker {token.name} clicked. Rejecting.");
                                hexGrid.ClearHighlightedHexes();
                                highPassManager.selectedToken = null;
                                yield return null;  // Exit after rejecting the ineligible attacker
                            }
                        }
                    }
                    // Defender Phase: Ensure the token is a defender
                    else if (MatchManager.Instance.currentState == MatchManager.GameState.HighPassDefenderMovement && !token.isAttacker)
                    {
                        if (highPassManager.selectedToken != null && highPassManager.selectedToken != token)
                        {
                            Debug.Log($"Switching defender selection to {token.name}. Clearing previous highlights.");
                            hexGrid.ClearHighlightedHexes();  // Clear the previous highlights
                        }

                        highPassManager.selectedToken = token;  // Set the selected defender token
                        movementPhaseManager.HighlightValidMovementHexes(token, 3);  // Highlight reachable hexes within 3 moves
                        yield return null;  // Ensure no further processing for this click
                    }
                }

                // Check if a valid hex was clicked (for movement)
                HexCell clickedHex = hit.collider.GetComponent<HexCell>();
                if (clickedHex != null)
                {
                    Debug.Log($"Hex clicked: {clickedHex.name}");

                    // Ensure the hex is within the highlighted valid movement hexes
                    if (hexGrid.highlightedHexes.Contains(clickedHex))
                    {
                        if (highPassManager.selectedToken != null)
                        {
                            Debug.Log($"Moving {highPassManager.selectedToken.name} to hex {clickedHex.coordinates}");

                            // Move the selected token to the valid hex (use the highPassManager's selectedToken)
                            yield return StartCoroutine(movementPhaseManager.MoveTokenToHex(clickedHex, highPassManager.selectedToken, false));  // Pass the selected token
                            highPassManager.selectedToken = null;  // Reset after movement

                            if (MatchManager.Instance.currentState == MatchManager.GameState.HighPassAttackerMovement)
                            {
                                highPassManager.StartDefenderMovementPhase();  // Transition to defender movement after attacker moves
                            }
                            else if (MatchManager.Instance.currentState == MatchManager.GameState.HighPassDefenderMovement)
                            {
                                highPassManager.isWaitingForAccuracyRoll = true;
                                Debug.Log("Waiting for accuracy roll... Please Press R key.");
                            }
                        }
                        else
                        {
                            Debug.LogWarning("No token selected to move.");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Clicked hex is not a valid movement target.");
                    }
                }
                else
                {
                    Debug.LogWarning("No valid hex or token clicked.");
                }
            }
        }
    }

    public IEnumerator HandleMouseInputForFTPMovement()
    {
        if (Input.GetMouseButtonDown(0))  // Only respond to left mouse click (not every frame)
        {
            Debug.Log("HandleMouseInputForFTPMovement called on click");

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                Debug.Log("Raycast hit something");

                // Check if a player token was clicked
                PlayerToken token = hit.collider.GetComponent<PlayerToken>();
                if (token != null)
                {
                    Debug.Log($"PlayerToken {token.name} clicked, for FTP");

                    // Attacker Phase: Ensure the token is an attacker
                    if (MatchManager.Instance.currentState == MatchManager.GameState.FirstTimePassAttackerMovement && token.isAttacker)
                    {
                        // Trying to move an Attacker: Accept, Highlight and wait for click on Hex
                        if (firstTimePassManager.selectedToken != null && firstTimePassManager.selectedToken != token)
                        {
                            Debug.Log($"Switching Attacker selection to {token.name}. Clearing previous highlights.");
                            hexGrid.ClearHighlightedHexes();  // Clear the previous highlights
                        }

                        Debug.Log($"Selecting attacker {token.name}. Highlighting reachable hexes.");
                        firstTimePassManager.selectedToken = token;  // Set selected token
                        movementPhaseManager.HighlightValidMovementHexes(token, 1);  // Highlight reachable hexes within 3 moves
                        yield return null;
                    }
                    // Defender Phase: Ensure the token is a defender
                    else if (MatchManager.Instance.currentState == MatchManager.GameState.FirstTimePassDefenderMovement && !token.isAttacker)
                    {
                        if (firstTimePassManager.selectedToken != null && firstTimePassManager.selectedToken != token)
                        {
                            Debug.Log($"Switching defender selection to {token.name}. Clearing previous highlights.");
                            hexGrid.ClearHighlightedHexes();  // Clear the previous highlights
                        }

                        firstTimePassManager.selectedToken = token;  // Set the selected defender token
                        movementPhaseManager.HighlightValidMovementHexes(token, 1);  // Highlight reachable hexes within 3 moves
                        firstTimePassManager.CompleteDefenderMovementPhase();
                        yield return null;  // Ensure no further processing for this click
                    }
                }

                // Check if a valid hex was clicked (for movement)
                HexCell clickedHex = hit.collider.GetComponent<HexCell>();
                if (clickedHex != null)
                {
                    Debug.Log($"Hex clicked: {clickedHex.name}");

                    // Ensure the hex is within the highlighted valid movement hexes
                    if (hexGrid.highlightedHexes.Contains(clickedHex))
                    {
                        if (firstTimePassManager.selectedToken != null)
                        {
                            Debug.Log($"Moving {firstTimePassManager.selectedToken.name} to hex {clickedHex.coordinates}");

                            // Move the selected token to the valid hex (use the highPassManager's selectedToken)
                            yield return StartCoroutine(movementPhaseManager.MoveTokenToHex(clickedHex, firstTimePassManager.selectedToken, false));  // Pass the selected token
                            firstTimePassManager.selectedToken = null;  // Reset after movement

                            if (MatchManager.Instance.currentState == MatchManager.GameState.FirstTimePassAttackerMovement)
                            {
                                firstTimePassManager.StartDefenderMovementPhase();  // Transition to defender movement after attacker moves
                            }
                            else if (MatchManager.Instance.currentState == MatchManager.GameState.FirstTimePassDefenderMovement)
                            {
                                StartCoroutine(firstTimePassManager.CompleteDefenderMovementPhase());
                            }
                        }
                        else
                        {
                            Debug.LogWarning("No token selected to move.");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Clicked hex is not a valid movement target.");
                    }
                }
                else
                {
                    Debug.LogWarning("No valid hex or token clicked.");
                }
            }
        }
    }
    
    public IEnumerator HandleMouseInputForSnapMovement()
    {
        if (Input.GetMouseButtonDown(0))  // Only respond to left mouse click (not every frame)
        {
            Debug.Log("HandleMouseInputForSnapMovement called on click");

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                Debug.Log("Raycast hit something");

                // Check if a player token was clicked
                PlayerToken token = hit.collider.GetComponent<PlayerToken>();
                HexCell clickedHex = hit.collider.GetComponent<HexCell>();
                PlayerToken tokenOnHex = clickedHex?.GetOccupyingToken();
                PlayerToken inferredToken = token ?? tokenOnHex ?? null;

                if (inferredToken != null && shotManager.isWaitingforBlockerSelection)
                {
                    Debug.Log($"PlayerToken {inferredToken.name} clicked, for Snapshot");

                    // Attacker Phase: Ensure the token is an attacker
                    if (!inferredToken.isAttacker)
                    {
                        // Trying to move an Attacker: Accept, Highlight and wait for click on Hex
                        if (shotManager.tokenMoveforDeflection != null && shotManager.tokenMoveforDeflection != inferredToken)
                        {
                            Debug.Log($"Switching Defender selection to {inferredToken.name}. Clearing previous highlights.");
                            hexGrid.ClearHighlightedHexes();  // Clear the previous highlights
                        }

                        Debug.Log($"Selecting defender {inferredToken.name}. Highlighting reachable hexes.");
                        shotManager.tokenMoveforDeflection = inferredToken;  // Set selected token
                        movementPhaseManager.HighlightValidMovementHexes(inferredToken, 2);  // Highlight reachable hexes within 3 moves
                        shotManager.isWaitingforBlockerSelection = false;
                        shotManager.isWaitingforBlockerMovement = true;
                        yield return null;
                    }
                    else {
                        Debug.LogWarning("Attacker clicked, while waiting for a defender to select.");
                    }
                }

                if (clickedHex != null && shotManager.isWaitingforBlockerMovement)
                {
                    Debug.Log($"Hex clicked: {clickedHex.name}");

                    // Ensure the hex is within the highlighted valid movement hexes
                    if (
                        hexGrid.highlightedHexes.Contains(clickedHex)
                        && !clickedHex.isAttackOccupied
                        && !clickedHex.isDefenseOccupied
                        && !clickedHex.isOutOfBounds
                    )
                    {
                        if (shotManager.tokenMoveforDeflection != null)
                        {
                            Debug.Log($"Moving {shotManager.tokenMoveforDeflection.name} to hex {clickedHex.coordinates}");

                            // Move the selected token to the valid hex (use the highPassManager's selectedToken)
                            yield return StartCoroutine(movementPhaseManager.MoveTokenToHex(clickedHex, shotManager.tokenMoveforDeflection, false));  // Pass the selected token
                            shotManager.CompleteDefenderMovement();
                        }
                        else
                        {
                            Debug.LogWarning("No token selected to move.");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Clicked hex is not a valid movement target.");
                    }
                }
                else if (
                    clickedHex != null // we clicked a Hex
                    && shotManager.isWaitingForTargetSelection // We are indeed waiting for a targetSelection
                    && hexGrid.highlightedHexes.Contains(clickedHex) // one of the target Hexes
                )
                {
                    Debug.Log($"Valid selected Target Hex: {clickedHex.name}");
                    shotManager.HandleTargetClick(clickedHex);                    
                }
                else
                {
                    Debug.LogWarning("No valid hex or token clicked.");
                }
            }
        }
    }

    private void HandleAttackerHeaderSelectionInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                PlayerToken token = hit.collider.GetComponent<PlayerToken>();
                if (token != null && token.isAttacker && headerManager.attEligibleToHead.Contains(token) && !headerManager.attackerWillJump.Contains(token))
                {
                    StartCoroutine(headerManager.HandleAttackerHeaderSelection(token));
                }
                else
                {
                    Debug.Log("Invalid attacker selected for header challenge.");
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            headerManager.ConfirmAttackerHeaderSelection();
        }
        if (Input.GetKeyDown(KeyCode.A))
        {
            headerManager.SelectAllAvailableAttackers();
        }
    }

    private void HandleDefenderHeaderSelectionInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                PlayerToken token = hit.collider.GetComponent<PlayerToken>();
                if (token != null && !token.isAttacker && headerManager.defEligibleToHead.Contains(token) && !headerManager.defenderWillJump.Contains(token))
                {
                    StartCoroutine(headerManager.HandleDefenderHeaderSelection(token));
                }
                else
                {
                    Debug.Log("Invalid defender selected for header challenge.");
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            headerManager.ConfirmDefenderHeaderSelection();
        }
        if (Input.GetKeyDown(KeyCode.A))
        {
            headerManager.SelectAllAvailableDefenders();
        }
    }

    private void HandleFreeKickKickerSelection()
    {
        if (MatchManager.Instance.currentState == MatchManager.GameState.FreeKickKickerSelect
            && freeKickManager.isWaitingForKickerSelection)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    Debug.Log($"Raycast hit: {hit.collider.name}");

                    // Check if the ray hit a PlayerToken directly
                    PlayerToken clickedToken = hit.collider.GetComponent<PlayerToken>();
                    if (clickedToken != null)
                    {
                        if (clickedToken.isAttacker)
                        {
                            Debug.Log($"Selected {clickedToken.name} as the kicker.");
                            StartCoroutine(freeKickManager.HandleKickerSelection(clickedToken));
                            return;  // End processing for this click
                        }
                        else
                        {
                            Debug.Log($"Click ignored: {clickedToken.name} is a defender and cannot be selected as the kicker.");
                        }
                        return;  // End processing for this click
                    }

                    // If the ray did not hit a PlayerToken, check for a HexCell
                    HexCell clickedHex = hit.collider.GetComponent<HexCell>();
                    if (clickedHex != null)
                    {
                        Debug.Log($"Hex clicked: {clickedHex.coordinates}");

                        // Check if the hex has a token on it
                        PlayerToken occupyingToken = clickedHex.GetOccupyingToken();
                        if (occupyingToken != null)
                        {
                            if (occupyingToken.isAttacker)
                            {
                                Debug.Log($"Selected {occupyingToken.name} as the kicker.");
                                StartCoroutine(freeKickManager.HandleKickerSelection(occupyingToken));
                            }
                            else
                            {
                                Debug.Log($"Click ignored: {occupyingToken.name} is a defender and cannot be selected as the kicker.");
                            }
                        }
                        else
                        {
                            Debug.Log("Click ignored: Please select an attacker occupying a hex.");
                        }
                    }
                    else
                    {
                        Debug.Log("Raycast did not hit a valid hex or token.");
                    }
                }
                else
                {
                    Debug.Log("Raycast did not hit any collider.");
                }
            }
            else if (Input.GetKeyDown(KeyCode.X))
            {
                Debug.Log("Player pressed X to skip kicker selection.");
                StartCoroutine(freeKickManager.HandleKickerSelection());  // Pass no token to skip
            }
        }
    }

    private void HandleFreeKickSetupPhaseInput()
    {
        if (freeKickManager.isWaitingForSetupPhase)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    Debug.Log($"Raycast hit: {hit.collider.name}");
                    // Check if a token is already selected
                    if (freeKickManager.selectedToken != null)
                    {
                        // Handle destination Hex selection
                        HexCell clickedDestinationHex = hit.collider.GetComponent<HexCell>();
                        PlayerToken newClickedToken = hit.collider.GetComponent<PlayerToken>();
                        if (newClickedToken != null && newClickedToken != freeKickManager.selectedToken)
                        {
                          Debug.Log($"New Clicked token during free kick setup: {newClickedToken.name}");
                          freeKickManager.HandleSetupTokenSelection(newClickedToken);
                          return;
                        }
                        if (clickedDestinationHex != null)
                        {
                            if (!clickedDestinationHex.isDefenseOccupied && !clickedDestinationHex.isAttackOccupied)
                            {
                                Debug.Log($"Token {freeKickManager.selectedToken.name} moving to Hex {clickedDestinationHex.coordinates}");
                                // freeKickManager.MoveTokenToHex(freeKickManager.selectedToken, clickedDestinationHex);
                                StartCoroutine(freeKickManager.HandleSetupHexSelection(clickedDestinationHex));
                                // freeKickManager.selectedToken = null; // Reset the selected token
                            }
                            else
                            {
                                Debug.LogWarning($"Hex {clickedDestinationHex.coordinates} is occupied. Select an unoccupied Hex.");
                            }
                        }
                        else
                        {
                            Debug.LogWarning("Please click on a valid Hex to move the selected token.");
                        }
                        return;
                    }

                    PlayerToken clickedToken = hit.collider.GetComponent<PlayerToken>();
                    if (clickedToken != null)
                    {
                        Debug.Log($"Clicked token during free kick setup: {clickedToken.name}");
                        freeKickManager.HandleSetupTokenSelection(clickedToken);
                        return;
                    }

                    // Check if the clicked object is a HexCell
                    HexCell clickedHex = hit.collider.GetComponent<HexCell>();
                    if (clickedHex != null)
                    {
                        Debug.Log($"Clicked on hex: {clickedHex.coordinates}");

                        // If there's a token on the clicked hex, treat it as clicking the token
                        PlayerToken occupyingToken = clickedHex.GetOccupyingToken();
                        if (occupyingToken != null)
                        {
                            Debug.Log($"Hex {clickedHex.coordinates} is occupied by token: {occupyingToken.name}");
                            freeKickManager.HandleSetupTokenSelection(occupyingToken);
                            return;
                        }
                        else
                        {
                            Debug.LogWarning($"Hex {clickedHex.coordinates} is unoccupied. Please select a valid token.");
                            return;
                        }
                    }
                    // Check if the clicked object is the Ball
                    Ball clickedBall = hit.collider.GetComponent<Ball>();
                    if (clickedBall != null)
                    {
                        Debug.Log("Clicked on the ball.");

                        // Get the Hex where the ball is located
                        HexCell ballHex = clickedBall.GetCurrentHex();
                        if (ballHex != null)
                        {
                            PlayerToken ballToken = ballHex.GetOccupyingToken();
                            if (ballToken != null)
                            {
                                Debug.Log($"Ball is on hex {ballHex.coordinates}, occupied by token: {ballToken.name}");
                                freeKickManager.HandleSetupTokenSelection(ballToken);
                                return;
                            }
                            else
                            {
                                Debug.LogWarning($"Ball is on hex {ballHex.coordinates}, but no token is present. Input rejected.");
                                return;
                            }
                        }
                        else
                        {
                            Debug.LogError("Ball is not on a valid Hex. Input rejected.");
                            return;
                        }
                    }
                }
                else
                {
                    Debug.Log("Raycast did not hit any collider.");
                }
            }
            else if (Input.GetKeyDown(KeyCode.X))
            {
                Debug.Log("Player attempts to forfeit the remaining moves for this phase.");
                freeKickManager.selectedToken = null;  // Reset the selected token
                freeKickManager.AttemptToAdvanceToNextPhase();
                // return;
            }
        }
    }

}