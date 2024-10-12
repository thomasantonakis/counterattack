using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;


public class GameInputManager : MonoBehaviour
{
    public CameraController cameraController;  
    public GroundBallManager groundBallManager;
    public LongBallManager longBallManager;
    public HighPassManager highPassManager;
    public MovementPhaseManager movementPhaseManager;
    public Ball ball;  
    public HexGrid hexGrid; 
    public MatchManager matchManager;

    public LayerMask tokenLayerMask;  // Layer for player tokens
    public LayerMask hexLayerMask;    // Layer for hex grid

    private Vector3 mouseDownPosition;  
    private bool isDragging = false;    
    public float dragThreshold = 10f;   

    void Start()
    {
        // TestHexConversions();
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
        if (Input.GetKeyDown(KeyCode.P))
        {
            hexGrid.ClearHighlightedHexes(); 
            MatchManager.Instance.TriggerStandardPass();
        }
        else if (Input.GetKeyDown(KeyCode.M))
        {
            MatchManager.Instance.TriggerMovement();
        }
        else if (Input.GetKeyDown(KeyCode.C))
        {
            hexGrid.ClearHighlightedHexes(); 
            MatchManager.Instance.TriggerHighPass();
        }
        else if (Input.GetKeyDown(KeyCode.L))
        {
            hexGrid.ClearHighlightedHexes(); 
            MatchManager.Instance.TriggerLongPass();
        }
        // MovementPhase input handling
        if
        (!movementPhaseManager.isPlayerMoving &&
            (
                MatchManager.Instance.currentState == MatchManager.GameState.MovementPhaseAttack || 
                MatchManager.Instance.currentState == MatchManager.GameState.MovementPhaseDef ||
                MatchManager.Instance.currentState == MatchManager.GameState.MovementPhase2f2
            )
        )
        {
            HandleMouseInputForMovement();
        }
        if (
                MatchManager.Instance.currentState == MatchManager.GameState.HighPassAttackerMovement || 
                MatchManager.Instance.currentState == MatchManager.GameState.HighPassDefenderMovement
        )
        {
            HandleMouseInputForHighPassMovement();
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
            groundBallManager.HandleGroundBallPath(hex);
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

    void HandleMouseInputForMovement()
    {
        if (Input.GetMouseButtonDown(0))  // Only respond to left mouse click (not every frame)
        {
            Debug.Log("HandleMouseInputForMovement called on click");

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                Debug.Log("Raycast hit something");

                // Check if the player token was clicked
                PlayerToken token = hit.collider.GetComponent<PlayerToken>();
                if (token != null)
                {
                    Debug.Log("Player token clicked");
                    movementPhaseManager.HandleTokenSelection(token);  // Select the token first
                }
                // Check if the ball was clicked
                Ball clickedBall = hit.collider.GetComponent<Ball>();
                if (clickedBall != null)
                {
                    Debug.Log("Ball clicked");
                    HexCell ballHex = clickedBall.GetCurrentHex();  // Get the hex the ball is on
                    PlayerToken ballToken = ballHex?.GetOccupyingToken();  // Check if a token occupies the hex where the ball is

                    if (ballToken != null)
                    {
                        Debug.Log("Selecting the token carrying the ball");
                        movementPhaseManager.HandleTokenSelection(ballToken);  // Select the token carrying the ball
                        return;  // Stop further checks if the ball was clicked and token found
                    }
                }
                // Check if a valid hex was clicked
                HexCell clickedHex = hit.collider.GetComponent<HexCell>();
                if (clickedHex != null)
                {
                    Debug.Log($"Hex clicked: {clickedHex.name}");

                    // Check if the hex is occupied by a token
                    PlayerToken occupyingToken = clickedHex.GetOccupyingToken();
                    if (occupyingToken != null)
                    {
                        Debug.Log("Hex is occupied by a token. Selecting the token instead.");
                        movementPhaseManager.HandleTokenSelection(occupyingToken);  // Select the token on the clicked hex
                        return;  // Stop further checks if the hex is occupied by a token
                    }

                    // If the hex is not occupied, check if it's valid for movement
                    if (movementPhaseManager.IsHexValidForMovement(clickedHex))
                    {
                        movementPhaseManager.MoveTokenToHex(clickedHex);  // Move the selected token to the hex
                    }
                }
            }
        }
    }

    public void HandleMouseInputForHighPassMovement()
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
                                return;  // Exit to avoid selecting a locked attacker
                            }
                            else
                            {
                                // Trying to move anyone BUT the locked Player, Accept, Highlight and wait for click on Hex
                                Debug.Log($"Selecting attacker {token.name}. Highlighting reachable hexes.");
                                highPassManager.selectedToken = token;  // Set selected token
                                movementPhaseManager.HighlightValidMovementHexes(token, 3);  // Highlight reachable hexes within 3 moves
                                return;
                            }
                        }
                        // ** Targeting a Hex Near One or more Players
                        // **Check if there are multiple eligible attackers**
                        if (highPassManager.eligibleAttackers != null && highPassManager.eligibleAttackers.Contains(token))
                        {
                            Debug.Log($"Eligible attacker {token.name} selected. Moving to the target hex.");
                            movementPhaseManager.MoveTokenToHex(highPassManager.currentTargetHex, token);  // Move attacker to target hex
                            highPassManager.StartDefenderMovementPhase();  // Transition to defender phase
                            return;  // Exit after attacker has moved
                        }
                        else if (highPassManager.eligibleAttackers != null && !highPassManager.eligibleAttackers.Contains(token))
                        {
                            Debug.LogWarning($"Ineligible attacker {token.name} clicked. Rejecting.");
                            hexGrid.ClearHighlightedHexes();
                            highPassManager.selectedToken = null;
                            return;  // Exit after rejecting the ineligible attacker
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
                        return;  // Ensure no further processing for this click
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
                            movementPhaseManager.MoveTokenToHex(clickedHex, highPassManager.selectedToken);  // Pass the selected token
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


    // public void TestHexConversions()
    // {
    //     Vector3Int cubeCoords = new Vector3Int(3, -3, 0); 
    //     Vector2Int offsetCoords = HexGridUtils.CubeToOffset(cubeCoords);
    //     Vector3Int convertedBackToCube = HexGridUtils.OffsetToCube(offsetCoords.x, offsetCoords.y);

    //     Debug.Log($"Cube: {cubeCoords}, Offset: {offsetCoords}, Converted Back to Cube: {convertedBackToCube}");
    // }
}