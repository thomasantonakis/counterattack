using UnityEngine;
using System;
using System.Text;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using Unity.VisualScripting;
using System.Runtime.CompilerServices;

public class KeyPressData
    {
        public KeyCode key;
        public bool shift;
        public bool ctrl;
        public bool alt;
        public bool isConsumed;

        public KeyPressData(KeyCode key, bool shift, bool ctrl, bool alt)
        {
            this.key = key;
            this.shift = shift;
            this.ctrl = ctrl;
            this.alt = alt;
            this.isConsumed = false; // set default here
        }

        public override string ToString()
        {
            return $"{(ctrl ? "Ctrl+" : "")}{(alt ? "Alt+" : "")}{(shift ? "Shift+" : "")}{key}, , Consumed: {isConsumed}";
        }
    }

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
    public FinalThirdManager finalThirdManager;
    public KickoffManager kickoffManager;
    public GoalKeeperManager goalKeeperManager;
    public Ball ball;  
    public HexGrid hexGrid; 
    public MatchManager matchManager;
    public static event Action<PlayerToken, HexCell> OnClick;
    public static event Action<PlayerToken, HexCell> OnHover;
    // public static event Action<KeyCode> OnKeyPress;
    public static event Action<KeyPressData> OnKeyPress;

    [Header("Layers")]
    public LayerMask tokenLayerMask;  // Layer for player tokens
    public LayerMask hexLayerMask;    // Layer for hex grid

    [Header("Hovers")]
    public PlayerToken hoveredToken = null;
    public HexCell hoveredHex = null;
    [Header("Clicks")]
    public PlayerToken clickedToken = null;
    public HexCell clickedHex = null;
    private Vector3 mouseDownPosition;  
    public bool isDragging = false;    
    [SerializeField]
    private bool logIsOn = false;    
    public float dragThreshold = 10f;

    void Start()
    {
    }

    void Update()
    {
        // cameraController.HandleCameraInput();
        ProcessInputs();
        // HandleMouseInput();
        // HandleSpecialInputs();
    }

    private void ProcessInputs()
    {
      HandleMouseHover();
      DetectMouseDrag();     // Drag overrides click
      HandleKeyPresses();
    }

    private void HandleMouseHover()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hover))
        {
            ClearHover();
            return;
        }
        
        var (newHoveredToken, newHoveredHex, isOOBClick) = DetectTokenOrHexClicked(hover);

        if (isOOBClick)
        {
            ClearHover();
            return;
        }

        // ✅ Only update hover state when it changes
        if (newHoveredToken != hoveredToken || newHoveredHex != hoveredHex)
        {
            hoveredToken = newHoveredToken;
            hoveredHex = newHoveredHex;
            // HandleHover(hoveredToken, hoveredHex);
            OnHover?.Invoke(hoveredToken, hoveredHex);  // 📣 Broadcast hover updates
        }
    }

    private void DetectMouseDrag()
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
                if (logIsOn) Debug.Log("🖱️ No drag → treat as click.");
                HandleMouseClick();
            }
            else
            {
                if (logIsOn) Debug.Log("🖱️ Drag ended.");
            }
            isDragging = false;
        }
    }

    private void HandleMouseClick()
    {
        if (logIsOn) Debug.Log("HandleMouseClick called!");
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit click))
        {
            return;
        }
        else
        {
            if (logIsOn) Debug.Log("Raycast was successful");
            var (newClickedToken, newClickedHex, isOOBClick) = DetectTokenOrHexClicked(click);
            if (isOOBClick)
            {
                clickedToken = null;
                clickedHex = null;
                Debug.LogWarning("Out Of Bounds Plane hit, Sending empty Notification");
            }
            else
            {
                if (logIsOn) Debug.Log("Updating Clicked Items");
                // ✅ Only update clicked items when they change
                if (newClickedToken != clickedToken || newClickedHex != clickedHex)
                {
                    clickedToken = newClickedToken;
                    clickedHex = newClickedHex;
                }
            }
            OnClick?.Invoke(clickedToken, clickedHex);  // 📣 Broadcast the click event
        }
    }
    
    // private void HandleHover(PlayerToken token, HexCell hex)
    // {
    //     if (logIsOn)
    //     {
    //         if (token != null)
    //         {
    //             Debug.Log($"👀 Hovering over Token: {token.name}");
    //             // Highlight token, show stats, etc.
    //         }
    //         else if (hex != null)
    //         {
    //             Debug.Log($"👀 Hovering over Hex: {hex.name}");
    //             // Show movement paths, highlight, etc.
    //         }
    //         else
    //         {
    //             Debug.Log("❓ Hovering over empty space.");
    //         }
    //     }
    // }

    private void ClearHover()
    {
        if (hoveredToken != null || hoveredHex != null)
        {
            Debug.Log("🏁 Hover cleared.");
        }
        hoveredToken = null;
        hoveredHex = null;
    }
    
    private void HandleKeyPresses()
    {
        if (!Input.anyKeyDown) return;
        foreach (KeyCode kcode in Enum.GetValues(typeof(KeyCode)))
        {
            if (!Input.GetKeyDown(kcode)) continue;
            if (kcode is
                KeyCode.LeftShift or KeyCode.RightShift or 
                KeyCode.LeftControl or KeyCode.RightControl or 
                KeyCode.LeftAlt or KeyCode.RightAlt or
                KeyCode.LeftCommand or KeyCode.RightCommand
            )
            {
                if (logIsOn) Debug.Log($"🔒 Modifier key ignored: {kcode}");
                continue;
            }
            KeyPressData data = new KeyPressData
            (
                kcode,
                Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift),
                Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand),
                Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)
            );
            if (logIsOn) Debug.Log($"📢 KeyPress: {data}");
            OnKeyPress?.Invoke(data);
            break; // Stop after first match

            // if (!Input.anyKeyDown) 
            // {
            //     if (Input.GetKeyDown(kcode))
            //     {
            //         if (logIsOn) Debug.Log($"🔑 Key pressed: {kcode}");
            //         OnKeyPress?.Invoke(kcode);  // 📣 Broadcast the key press
            //     }
            // }
        }
    }

    // private void HandleKeyAction(KeyCode key)
    // {
    //     // switch (key)
    //     // {
    //     //     case KeyCode.R:
    //     //         Debug.Log("🔁 Reload or Roll logic here.");
    //     //         break;

    //     //     case KeyCode.Space:
    //     //         Debug.Log("⏸ Pause logic here.");
    //     //         break;

    //     //     case KeyCode.Escape:
    //     //         Debug.Log("❌ Cancel logic here.");
    //     //         break;

    //     //     case KeyCode.Alpha1:
    //     //         Debug.Log("1️⃣ Select action 1.");
    //     //         break;

    //     //     case KeyCode.Alpha2:
    //     //         Debug.Log("2️⃣ Select action 2.");
    //     //         break;

    //     //     default:
    //     //         Debug.Log($"🪓 No action mapped for {key}");
    //     //         break;
    //     // }
    // }

    // private void HandleClick(PlayerToken token, HexCell hex)
    // {
    //     if (token != null)
    //     {
    //         Debug.Log($"🎯 Clicked on Token: {token.name}");
    //         // HandleTokenClick(token);
    //     }
    //     else if (hex != null)
    //     {
    //         Debug.Log($"🎯 Clicked on Hex: {hex.name}");
    //         HandleHexClick(hex);
    //     }
    //     else
    //     {
    //         Debug.LogWarning("❓ Click did not hit a valid game object.");
    //     }
    // }

    void HandleSpecialInputs()
    {
        // Handle Next Action Selection
        if (
            Input.GetKeyDown(KeyCode.P) && !freeKickManager.isCornerKick
            // && MatchManager.Instance.currentState == MatchManager.GameState.SuccessfulTackle
        )
        {
            hexGrid.ClearHighlightedHexes(); 
            MatchManager.Instance.TriggerStandardPass();
            // groundBallManager.imposedDistance = 11;
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
        // else if (
        //     Input.GetKeyDown(KeyCode.C) && !freeKickManager.isCornerKick
        //     // && MatchManager.Instance.currentState == MatchManager.GameState.SuccessfulTackle
        // )
        // {
        //     hexGrid.ClearHighlightedHexes(); 
        //     MatchManager.Instance.TriggerHighPass();
        // }
        // else if (Input.GetKeyDown(KeyCode.L))
        // {
        //     hexGrid.ClearHighlightedHexes(); 
        //     MatchManager.Instance.TriggerLongPass();
        // }
        // else if (Input.GetKeyDown(KeyCode.F))
        // {
        //     hexGrid.ClearHighlightedHexes(); 
        //     MatchManager.Instance.TriggerFTP();
        // }
        // Final Thirds Handling
        // if (finalThirdManager.isActivated)
        // {
        //     HandleMouseInputForF3();
        //     return;
        // }
        if (shotManager.isActivated)
        {
            if (movementPhaseManager.isWaitingForSnapshotDecision)
            {
                if (Input.GetKeyDown(KeyCode.S))
                {
                    movementPhaseManager.isWaitingForSnapshotDecision = false;
                    Debug.Log($"{looseBallManager.ballHitThisToken.name} decides to Snapshot!!!!");
                    shotManager.StartShotProcess(looseBallManager.ballHitThisToken, "snapshot");
                    looseBallManager.EndLooseBallPhase();
                }
                if (Input.GetKeyDown(KeyCode.X))
                {
                    movementPhaseManager.isWaitingForSnapshotDecision = false;
                    Debug.Log($"Attacker decides not to shoot..");
                    if (movementPhaseManager.isActivated) movementPhaseManager.AdvanceMovementPhase();
                }
            }
            // if (shotManager.isWaitingforBlockerSelection)
            // {
            //     if (Input.GetKeyDown(KeyCode.X))
            //     {
            //         shotManager.CompleteDefenderMovement();
            //     }
            // }
            // StartCoroutine(HandleMouseInputForSnapMovement());
        }
        else
        {
            // // MovementPhase input handling
            // if
            // (
            //     !movementPhaseManager.isPlayerMoving &&
            //     !movementPhaseManager.isWaitingForTackleRoll &&
            //     !movementPhaseManager.isWaitingForTackleDecision &&
            //     !movementPhaseManager.isWaitingForInterceptionDiceRoll &&
            //     !movementPhaseManager.isWaitingForReposition &&
            //     !movementPhaseManager.isWaitingForInjuryRoll &&
            //     !movementPhaseManager.isWaitingForYellowCardRoll &&
            //     !movementPhaseManager.isWaitingForFoulDecision &&
            //     !goalKeeperManager.isActivated &&
            //     (
            //         MatchManager.Instance.currentState == MatchManager.GameState.MovementPhaseAttack || 
            //         MatchManager.Instance.currentState == MatchManager.GameState.MovementPhaseDef ||
            //         MatchManager.Instance.currentState == MatchManager.GameState.MovementPhase2f2
            //     )
            // )
            // {
            //     // if (Input.GetKeyDown(KeyCode.X))
            //     // {
            //     //     if (!movementPhaseManager.isWaitingForNutmegDecision 
            //     //         && !movementPhaseManager.isWaitingForNutmegDecisionWithoutMoving
            //     //     )
            //     //     {
            //     //         movementPhaseManager.ForfeitTeamMovementPhase();
            //     //     }
            //     // }
            //     // StartCoroutine(HandleMouseInputForMovement());
            // }
            // if (
            //     (
            //         MatchManager.Instance.currentState == MatchManager.GameState.HighPassAttackerMovement || 
            //         MatchManager.Instance.currentState == MatchManager.GameState.HighPassDefenderMovement
            //     ) && !highPassManager.isWaitingForDefGKChallengeDecision
            // )
            // {
            //     StartCoroutine(HandleMouseInputForHighPassMovement());
            // }
            // if (
            //     MatchManager.Instance.currentState == MatchManager.GameState.HighPassCompleted
            //     && highPassManager.isWaitingForDefGKChallengeDecision
            // )
            // {
            //     StartCoroutine(HandleMouseInputForHPGKRush());
            // }
            // if (
            //     goalKeeperManager.isActivated
            // )
            // {
            //     StartCoroutine(HandleMouseInputForGKBoxMovement());
            // }
            // if (
            //     MatchManager.Instance.currentState == MatchManager.GameState.LongBallAttempt
            //     && longBallManager.isWaitingForDefLBMove
            // )
            // {
            //     StartCoroutine(HandleMouseInputForGKLongBallMovement());
            // }
            // if (
            //         MatchManager.Instance.currentState == MatchManager.GameState.FirstTimePassAttackerMovement ||
            //         MatchManager.Instance.currentState == MatchManager.GameState.FirstTimePassDefenderMovement
            // )
            // {
            //     StartCoroutine(HandleMouseInputForFTPMovement());
            // }
            // if (MatchManager.Instance.currentState == MatchManager.GameState.HeaderAttackerSelection)
            // {
            //     HandleAttackerHeaderSelectionInput();
            // }
            // else if (MatchManager.Instance.currentState == MatchManager.GameState.HeaderDefenderSelection)
            // {
            //     HandleDefenderHeaderSelectionInput();
            // }
            if (MatchManager.Instance.currentState.ToString().StartsWith("FreeKick"))
            {
                if (freeKickManager.isWaitingForKickerSelection)
                {
                    // HandleFreeKickKickerSelection();
                }
                else if (freeKickManager.isWaitingForSetupPhase)
                {
                    // HandleFreeKickSetupPhaseInput();
                }
                else if (freeKickManager.isWaitingForFinalKickerSelection)
                {
                    // HandleFreeKickFinalKicker();
                }
                else if (freeKickManager.isWaitingForExecution)
                {
                //     if (freeKickManager.isCornerKick)
                //     {
                //         if (Input.GetKeyDown(KeyCode.C))
                //         {
                //             hexGrid.ClearHighlightedHexes(); 
                //             MatchManager.Instance.TriggerHighPass();
                //             highPassManager.isCornerKick = true;
                //             freeKickManager.isWaitingForExecution = false;
                //             freeKickManager.isCornerKick = false;
                //         }
                //         else if (Input.GetKeyDown(KeyCode.P))
                //         {
                //             hexGrid.ClearHighlightedHexes(); 
                //             MatchManager.Instance.TriggerStandardPass();
                //             groundBallManager.imposedDistance = 6;
                //             freeKickManager.isWaitingForExecution = false;
                //             freeKickManager.isCornerKick = false;
                //         }
                //     }
                //     else
                //     {
                //         if (Input.GetKeyDown(KeyCode.L))
                //         {
                //             hexGrid.ClearHighlightedHexes(); 
                //             MatchManager.Instance.TriggerLongPass();
                //             freeKickManager.isWaitingForExecution = false;
                //             freeKickManager.isCornerKick = false;
                //         }
                //         else if (Input.GetKeyDown(KeyCode.C))
                //         {
                //             hexGrid.ClearHighlightedHexes(); 
                //             MatchManager.Instance.TriggerHighPass();
                //             freeKickManager.isWaitingForExecution = false;
                //             freeKickManager.isCornerKick = false;
                //         }
                //         else if (Input.GetKeyDown(KeyCode.P))
                //         {
                //             hexGrid.ClearHighlightedHexes(); 
                //             MatchManager.Instance.TriggerStandardPass();
                //             freeKickManager.isWaitingForExecution = false;
                //             freeKickManager.isCornerKick = false;
                //         }
                //         else if (Input.GetKeyDown(KeyCode.S))
                //         {
                //             hexGrid.ClearHighlightedHexes();
                //             Debug.Log("Free Kick Shoot triggered."); 
                //             freeKickManager.isWaitingForExecution = false;
                //             freeKickManager.isCornerKick = false;
                //             // TODO: Implement Free Kick Shoot
                //         }
                //     }
                }
            }
        }
    }


    private void HandleHexClick(HexCell hex)
    {
        Debug.LogError($"LEGACY: Hex clicked: {hex.name}");
        // TODO: Remove this altogether and change the logic
        // if (ball.IsBallSelected() && MatchManager.Instance.currentState == MatchManager.GameState.StandardPassAttempt)
        // {
        //     groundBallManager.HandleGroundBallPath(hex); // Normal Standard Pass
        // }
        if (MatchManager.Instance.currentState == MatchManager.GameState.QuickThrow)
        {
            groundBallManager.HandleGroundBallPath(hex, true); // QuickThrow
        }
        // else if (ball.IsBallSelected() && MatchManager.Instance.currentState == MatchManager.GameState.FirstTimePassAttempt)
        // {
        //     firstTimePassManager.HandleFTPBallPath(hex);
        // }
        // else if (ball.IsBallSelected() && MatchManager.Instance.currentState == MatchManager.GameState.LongBallAttempt && !longBallManager.isWaitingForDefLBMove)
        // {
        //     longBallManager.HandleLongBallProcess(hex);
        // }
        // else if (ball.IsBallSelected() && MatchManager.Instance.currentState == MatchManager.GameState.HighPassAttempt)
        // {
        //     highPassManager.HandleHighPassProcess(hex, false);
        // }
        else if (ball.IsBallSelected() && MatchManager.Instance.currentState == MatchManager.GameState.GoalKick)
        {
            highPassManager.HandleHighPassProcess(hex, true);
        }
    }

    public IEnumerator HandleMouseInputForMovement()
    {
        yield return null;
        // if (Input.GetMouseButtonDown(0))  // Only respond to left mouse click (not every frame)
        // {
        //     Debug.Log("HandleMouseInputForMovement called on click");

        //     Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        //     RaycastHit hit;

        //     if (Physics.Raycast(ray, out hit))
        //     {
        //         Debug.Log("HandleMouseInputForMovement: Raycast hit something");
        //         var (inferredTokenFromClick, inferredHexCellFromClick, isOOBClicked) =  DetectTokenOrHexClicked(hit);
        //         if (isOOBClicked)
        //         {
        //            Debug.LogWarning("Out Of Bounds Plane hit, rejecting click");
        //            yield break;
        //         }

        //         // When do we need a expect a Token Selection?
        //         if (
        //             inferredTokenFromClick != null // A token was indeed inferred from the click
        //             && !movementPhaseManager.isPlayerMoving // Wait for animations to stop
        //             && !movementPhaseManager.isDribblerRunning // The Dribbler has not started moving
        //             && (
        //                 movementPhaseManager.selectedToken == null // MovementPhase does not have a selected Token.
        //                 || !movementPhaseManager.isDribblerRunning //  We Should not be able to reset the selected Token while the Dribbler is running.
        //             )
        //             && !movementPhaseManager.lookingForNutmegVictim // Do not handle a Token when looking for a victim
        //             && !movementPhaseManager.isWaitingForNutmegDecision // Do not handle a Token when waiting for Nutmeg Decision
        //             && !movementPhaseManager.isWaitingForNutmegDecisionWithoutMoving // Do not handle a Token when waiting for Nutmeg Decision without moving
        //         )
        //         {
        //             Debug.Log($"Passing {inferredTokenFromClick.name} to HandleTokenSelection");
        //             movementPhaseManager.HandleTokenSelection(inferredTokenFromClick);  // Select the token first
        //             yield return null;
        //         }
        //         else if (
        //             // While either we are waiting to nutmeg without moving
        //             movementPhaseManager.isWaitingForNutmegDecisionWithoutMoving
        //             // Or we are waiting to nutmeg while the dribbler has already started moving
        //             || (movementPhaseManager.isDribblerRunning && movementPhaseManager.isWaitingForNutmegDecision)
        //         )
        //         {
        //             // We clicked on a Nutmeggable Defender
        //             if (movementPhaseManager.nutmeggableDefenders.Contains(inferredTokenFromClick))
        //             {
        //                 // Start the Nutmeg with the Selected nutmeggable Token
        //                 Debug.LogWarning("While waiting for a Nutmeg Decision, a nutmeggable Defender was clicked");
        //                 movementPhaseManager.isWaitingForSnapshotDecision = false;
        //                 movementPhaseManager.isWaitingForNutmegDecision = false;
        //                 movementPhaseManager.isWaitingForNutmegDecisionWithoutMoving = false;
        //                 movementPhaseManager.nutmegVictim = inferredTokenFromClick;
        //                 movementPhaseManager.isDribblerRunning = true;
        //                 hexGrid.ClearHighlightedHexes();
        //                 Debug.Log($"Selected {inferredTokenFromClick.name} to nutmeg. Proceeding with nutmeg.");
        //                 movementPhaseManager.lookingForNutmegVictim = false;
        //                 movementPhaseManager.StartNutmegProcess();
        //                 yield return null;
        //             }
        //             else
        //             {
        //                 Debug.LogWarning("While waiting for a Nutmeg Decision, a nutmeggable Defender was not clicked");
        //                 if (movementPhaseManager.IsHexValidForMovement(inferredHexCellFromClick))
        //                 {
        //                     // Turning off wait for Nutmeg decision flags.
        //                     if (movementPhaseManager.isWaitingForTackleDecisionWithoutMoving)
        //                     {
        //                         movementPhaseManager.isWaitingForTackleDecisionWithoutMoving = false;
        //                     }
        //                     if (movementPhaseManager.isWaitingForNutmegDecisionWithoutMoving)
        //                     {
        //                         movementPhaseManager.isWaitingForNutmegDecisionWithoutMoving = false;
        //                     }
        //                     if (movementPhaseManager.isWaitingForSnapshotDecision)
        //                     {
        //                         movementPhaseManager.isWaitingForSnapshotDecision = false;
        //                     }
        //                     Debug.Log($"Passing {inferredHexCellFromClick.name} to MoveTokenToHex");
        //                     yield return StartCoroutine(movementPhaseManager.MoveTokenToHex(inferredHexCellFromClick));  // Move the selected token to the hex
        //                 }
        //             }
        //         }
        //         else if (movementPhaseManager.lookingForNutmegVictim)
        //         {
        //             // Nutmeg was selected with the Keyboard, and more than one nutmeggable Defender exists
        //             Debug.Log($"Passing {inferredTokenFromClick.name} to HandleNutmegVictimSelection");
        //             movementPhaseManager.HandleNutmegVictimSelection(inferredTokenFromClick);
        //             yield return null;
        //         }
        //         else
        //         // We did not infer a Token (clicked on an Hex (or the ball on it) where there is no Token)
        //         // Clicked on a NOT OCCUPIED HEX
        //         {
        //             if (!movementPhaseManager.isWaitingForNutmegDecision && !movementPhaseManager.isWaitingForNutmegDecisionWithoutMoving)
        //             {
        //                 // If the hex is not occupied, check if it's valid for movement
        //                 if (movementPhaseManager.IsHexValidForMovement(inferredHexCellFromClick))
        //                 {
        //                     movementPhaseManager.isWaitingForSnapshotDecision = false;
        //                     bool temp_check = ball.GetCurrentHex() == inferredHexCellFromClick && movementPhaseManager.selectedToken != null;
        //                     if (temp_check)
        //                     {
        //                         movementPhaseManager.tokenPickedUpBall = true;
        //                     }
        //                     Debug.Log($"Passing {inferredHexCellFromClick.name} to MoveTokenToHex");
        //                     yield return StartCoroutine(movementPhaseManager.MoveTokenToHex(inferredHexCellFromClick));  // Move the selected token to the hex
        //                     if (temp_check)
        //                     {
        //                         movementPhaseManager.isBallPickable = false;
        //                     }
        //                 }
        //             }
        //         }
        //     }
        // }
        // else if (movementPhaseManager.isBallPickable && Input.GetKeyDown(KeyCode.V))
        // {
        //     movementPhaseManager.tokenPickedUpBall = true;
        //     movementPhaseManager.remainingDribblerPace = movementPhaseManager.selectedToken.pace;
        //     yield return StartCoroutine(movementPhaseManager.MoveTokenToHex(ball.GetCurrentHex()));
        //     movementPhaseManager.isBallPickable = false;
        // }
        // else if (movementPhaseManager.isWaitingForNutmegDecision)
        // {
        //     if (Input.GetKeyDown(KeyCode.N))
        //     {
        //         movementPhaseManager.isWaitingForNutmegDecision = false;
        //         movementPhaseManager.isWaitingForSnapshotDecision = false;
        //         Debug.Log($"Starting Nutmeg Process.");
        //         movementPhaseManager.StartNutmegVictimIdentification();
        //     }
        //     else if (Input.GetKeyDown(KeyCode.X))
        //     {
        //         movementPhaseManager.isWaitingForNutmegDecision = false;
        //         Debug.Log($"Reject Nutmeg. Check for interceptions.");
        //         movementPhaseManager.ContinueFromRejectedNutmeg();
        //     }
        // }
        // else if (movementPhaseManager.isWaitingForNutmegDecisionWithoutMoving)
        // {
        //     if (Input.GetKeyDown(KeyCode.N))
        //     {
        //         movementPhaseManager.isWaitingForNutmegDecisionWithoutMoving = false;
        //         movementPhaseManager.isWaitingForSnapshotDecision = false;
        //         Debug.Log($"Starting Nutmeg Process.");
        //         movementPhaseManager.StartNutmegVictimIdentification();
        //     }
        // }
        // else if (movementPhaseManager.isWaitingForSnapshotDecision)
        // {
        //     if (Input.GetKeyDown(KeyCode.S))
        //     {
        //         movementPhaseManager.isWaitingForSnapshotDecision = false;
        //         Debug.Log($"{movementPhaseManager.selectedToken.name} decides to Snapshot!!!!");
        //         shotManager.StartShotProcess(movementPhaseManager.selectedToken, "snapshot");
        //     }
        //     if (Input.GetKeyDown(KeyCode.X))
        //     {
        //         movementPhaseManager.isWaitingForSnapshotDecision = false;
        //         Debug.Log($"Attacker decides not to shoot..");
        //     }
        // }
    }

    // public void HandleMouseInputForF3()
    // {
    //     if (Input.GetKeyDown(KeyCode.X))
    //     {
    //         finalThirdManager.ForfeitTurn();
    //     }
    //     if (finalThirdManager.isWaitingForWhatToDo)
    //     {
    //         if (Input.GetKeyDown(KeyCode.D))
    //         {
    //             finalThirdManager.DropBall();
    //         }
    //         if (Input.GetKeyDown(KeyCode.K))
    //         {
    //             finalThirdManager.GKKick();
    //         }
    //     }
    //     if (Input.GetMouseButtonDown(0)) // Left Click
    //     {
    //         Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
    //         if (Physics.Raycast(ray, out RaycastHit hit))
    //         {
    //             Debug.Log("HandleMouseInputForF3: Click Detected!");
    //             var (inferredTokenFromClick, inferredHexCellFromClick, isOOBClicked) = DetectTokenOrHexClicked(hit);
    //             if (isOOBClicked)
    //             {
    //                Debug.LogWarning("Out Of Bounds Plane hit, rejecting click");
    //                return;
    //             }
    //             StartCoroutine(finalThirdManager.HandleMouseInput(inferredTokenFromClick, inferredHexCellFromClick));
    //         }
    //     }
    // }
    

    // public IEnumerator HandleMouseInputForHPGKRush()
    // {
    //     if (Input.GetKeyDown(KeyCode.X))
    //     {
    //         hexGrid.ClearHighlightedHexes();
    //         Debug.Log($"GK chooses to not rush out for the High Pass, moving on!");
    //         highPassManager.isWaitingForDefGKChallengeDecision = false;
    //         yield break;  
    //     }
    //     if (Input.GetMouseButtonDown(0))  // Only respond to left mouse click (not every frame)
    //     {
    //         Debug.Log("HandleMouseInputForHPGKRush called on click");

    //         Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
    //         if (Physics.Raycast(ray, out RaycastHit hit))
    //         {
    //             var (inferredTokenFromClick, inferredHexCellFromClick, isOOBClicked) =  DetectTokenOrHexClicked(hit);
    //             if (isOOBClicked)
    //             {
    //                Debug.LogWarning("Out Of Bounds Plane hit, rejecting click");
    //                yield break;
    //             }
    //             // Check if the ray hit a PlayerToken directly
    //             Debug.Log($"Inferred Clicked Token: {inferredTokenFromClick?.name}");
    //             Debug.Log($"Inferred Clicked Hex: {inferredHexCellFromClick.name}");
    //             if (inferredHexCellFromClick != null && highPassManager.gkReachableHexes.Contains(inferredHexCellFromClick))
    //             {
    //                 hexGrid.ClearHighlightedHexes();
    //                 yield return StartCoroutine(movementPhaseManager.MoveTokenToHex(inferredHexCellFromClick, hexGrid.GetDefendingGK(), false));
    //                 highPassManager.isWaitingForDefGKChallengeDecision = false;
    //                 highPassManager.gkRushedOut = true;
    //                 headerManager.defenderWillJump.Add(hexGrid.GetDefendingGK());
    //             }
    //             else
    //             {
    //                 Debug.LogWarning($"Cannot move GK there");
    //             }
    //         }
    //         else {
    //             Debug.Log("Raycast did not hit any collider.");
    //             yield break;
    //         }
    //     }
    // }
    
    // public IEnumerator HandleMouseInputForGKBoxMovement()
    // {
    //     if (Input.GetKeyDown(KeyCode.X))
    //     {
    //         hexGrid.ClearHighlightedHexes();
    //         Debug.Log($"GK chooses to not move for the ball entering the box, moving on!");
    //         goalKeeperManager.isActivated = false;
    //         yield break;  
    //     }
    //     if (Input.GetMouseButtonDown(0))  // Only respond to left mouse click (not every frame)
    //     {
    //         Debug.Log("HandleMouseInputForGKBoxMovement called on click");

    //         Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
    //         if (Physics.Raycast(ray, out RaycastHit hit))
    //         {
    //             var (inferredTokenFromClick, inferredHexCellFromClick, isOOBClicked) =  DetectTokenOrHexClicked(hit);
    //             if (isOOBClicked)
    //             {
    //                Debug.LogWarning("Out Of Bounds Plane hit, rejecting click");
    //                yield break;
    //             }

    //             if (inferredTokenFromClick == null && inferredHexCellFromClick != null && hexGrid.highlightedHexes.Contains(inferredHexCellFromClick))
    //             {
    //                 hexGrid.ClearHighlightedHexes();
    //                 yield return StartCoroutine(movementPhaseManager.MoveTokenToHex(inferredHexCellFromClick, hexGrid.GetDefendingGK(), false));
    //                 goalKeeperManager.isActivated = false;
    //                 Debug.Log($"🧤 {hexGrid.GetDefendingGK().name} moved to {inferredHexCellFromClick.name}");
    //             }
    //             else
    //             {
    //                 Debug.LogWarning($"Cannot move GK there");
    //             }
    //         }
    //         else {
    //             Debug.Log("Raycast did not hit any collider.");
    //             yield break;
    //         }
    //     }
    // }
    
    // public IEnumerator HandleMouseInputForGKLongBallMovement()
    // {
    //     if (Input.GetKeyDown(KeyCode.X))
    //     {
    //         hexGrid.ClearHighlightedHexes();
    //         Debug.Log($"GK chooses to not move for the ball entering the box, moving on!");
    //         longBallManager.isWaitingForDefLBMove = false;
    //         yield break;  
    //     }
    //     if (Input.GetMouseButtonDown(0))  // Only respond to left mouse click (not every frame)
    //     {
    //         Debug.Log("HandleMouseInputForGKLongBallMovement called on click");

    //         Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
    //         if (Physics.Raycast(ray, out RaycastHit hit))
    //         {
    //             var (inferredTokenFromClick, inferredHexCellFromClick, isOOBClicked) =  DetectTokenOrHexClicked(hit);
    //             if (isOOBClicked)
    //             {
    //                Debug.LogWarning("Out Of Bounds Plane hit, rejecting click");
    //                yield break;
    //             }

    //             if (inferredTokenFromClick == null && inferredHexCellFromClick != null && hexGrid.highlightedHexes.Contains(inferredHexCellFromClick))
    //             {
    //                 hexGrid.ClearHighlightedHexes();
    //                 yield return StartCoroutine(movementPhaseManager.MoveTokenToHex(inferredHexCellFromClick, hexGrid.GetDefendingGK(), false));
    //                 longBallManager.isWaitingForDefLBMove = false;
    //                 Debug.Log($"🧤 {hexGrid.GetDefendingGK().name} moved to {inferredHexCellFromClick.name}");
    //             }
    //             else
    //             {
    //                 Debug.LogWarning($"Cannot move GK there");
    //             }
    //         }
    //         else {
    //             Debug.Log("Raycast did not hit any collider.");
    //             yield break;
    //         }
    //     }
    // }

    // public IEnumerator HandleMouseInputForFTPMovement()
    // {
    //     if (Input.GetMouseButtonDown(0))  // Only respond to left mouse click (not every frame)
    //     {
    //         Debug.Log("HandleMouseInputForFTPMovement called on click");

    //         Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
    //         if (Physics.Raycast(ray, out RaycastHit hit))
    //         {
    //             Debug.Log("Raycast hit something");
    //             var (inferredTokenFromClick, inferredHexCellFromClick, isOOBClicked) = DetectTokenOrHexClicked(hit);
    //             if (isOOBClicked)
    //             {
    //                Debug.LogWarning("Out Of Bounds Plane hit, rejecting click");
    //                yield break;
    //             }

    //             // Check if a player token was clicked
    //             PlayerToken token = hit.collider.GetComponent<PlayerToken>();
    //             if (token != null)
    //             {
    //                 Debug.Log($"PlayerToken {token.name} clicked, for FTP");

    //                 // Attacker Phase: Ensure the token is an attacker
    //                 if (MatchManager.Instance.currentState == MatchManager.GameState.FirstTimePassAttackerMovement && token.isAttacker)
    //                 {
    //                     // Trying to move an Attacker: Accept, Highlight and wait for click on Hex
    //                     if (firstTimePassManager.selectedToken != null && firstTimePassManager.selectedToken != token)
    //                     {
    //                         Debug.Log($"Switching Attacker selection to {token.name}. Clearing previous highlights.");
    //                         hexGrid.ClearHighlightedHexes();  // Clear the previous highlights
    //                     }

    //                     Debug.Log($"Selecting attacker {token.name}. Highlighting reachable hexes.");
    //                     firstTimePassManager.selectedToken = token;  // Set selected token
    //                     movementPhaseManager.HighlightValidMovementHexes(token, 1);  // Highlight reachable hexes within 3 moves
    //                     yield return null;
    //                 }
    //                 // Defender Phase: Ensure the token is a defender
    //                 else if (MatchManager.Instance.currentState == MatchManager.GameState.FirstTimePassDefenderMovement && !token.isAttacker)
    //                 {
    //                     if (firstTimePassManager.selectedToken != null && firstTimePassManager.selectedToken != token)
    //                     {
    //                         Debug.Log($"Switching defender selection to {token.name}. Clearing previous highlights.");
    //                         hexGrid.ClearHighlightedHexes();  // Clear the previous highlights
    //                     }

    //                     firstTimePassManager.selectedToken = token;  // Set the selected defender token
    //                     movementPhaseManager.HighlightValidMovementHexes(token, 1);  // Highlight reachable hexes within 3 moves
    //                     firstTimePassManager.CompleteDefenderMovementPhase();
    //                     yield return null;  // Ensure no further processing for this click
    //                 }
    //             }

    //             // Check if a valid hex was clicked (for movement)
    //             HexCell clickedHex = hit.collider.GetComponent<HexCell>();
    //             if (clickedHex != null)
    //             {
    //                 Debug.Log($"Hex clicked: {clickedHex.name}");

    //                 // Ensure the hex is within the highlighted valid movement hexes
    //                 if (hexGrid.highlightedHexes.Contains(clickedHex))
    //                 {
    //                     if (firstTimePassManager.selectedToken != null)
    //                     {
    //                         Debug.Log($"Moving {firstTimePassManager.selectedToken.name} to hex {clickedHex.coordinates}");

    //                         // Move the selected token to the valid hex (use the highPassManager's selectedToken)
    //                         yield return StartCoroutine(movementPhaseManager.MoveTokenToHex(clickedHex, firstTimePassManager.selectedToken, false));  // Pass the selected token
    //                         firstTimePassManager.selectedToken = null;  // Reset after movement

    //                         if (MatchManager.Instance.currentState == MatchManager.GameState.FirstTimePassAttackerMovement)
    //                         {
    //                             firstTimePassManager.StartDefenderMovementPhase();  // Transition to defender movement after attacker moves
    //                         }
    //                         else if (MatchManager.Instance.currentState == MatchManager.GameState.FirstTimePassDefenderMovement)
    //                         {
    //                             firstTimePassManager.CompleteDefenderMovementPhase();
    //                         }
    //                     }
    //                     else
    //                     {
    //                         Debug.LogWarning("No token selected to move.");
    //                     }
    //                 }
    //                 else
    //                 {
    //                     Debug.LogWarning("Clicked hex is not a valid movement target.");
    //                 }
    //             }
    //             else
    //             {
    //                 Debug.LogWarning("No valid hex or token clicked.");
    //             }
    //         }
    //     }
    // }
    
    // public IEnumerator HandleMouseInputForSnapMovement()
    // {
    //     if (Input.GetMouseButtonDown(0))  // Only respond to left mouse click (not every frame)
    //     {
    //         Debug.Log("HandleMouseInputForSnapMovement called on click");

    //         Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
    //         RaycastHit hit;

    //         if (Physics.Raycast(ray, out hit))
    //         {
    //             Debug.Log("Raycast hit something");
    //             var (inferredTokenFromClick, inferredHexCellFromClick, isOOBClicked) = DetectTokenOrHexClicked(hit);
    //             if (isOOBClicked)
    //             {
    //                Debug.LogWarning("Out Of Bounds Plane hit, rejecting click");
    //                yield break;
    //             }

    //             // // Check if a player token was clicked
    //             // PlayerToken token = hit.collider.GetComponent<PlayerToken>();
    //             // HexCell clickedHex = hit.collider.GetComponent<HexCell>();
    //             // PlayerToken tokenOnHex = clickedHex?.GetOccupyingToken();
    //             // PlayerToken inferredToken = token ?? tokenOnHex ?? null;

    //             if (inferredTokenFromClick != null && shotManager.isWaitingforBlockerSelection)
    //             {
    //                 Debug.Log($"PlayerToken {inferredTokenFromClick.name} clicked, for Snapshot");

    //                 // Attacker Phase: Ensure the token is an attacker
    //                 if (!inferredTokenFromClick.isAttacker)
    //                 {
    //                     // Trying to move an Attacker: Accept, Highlight and wait for click on Hex
    //                     if (shotManager.tokenMoveforDeflection != null && shotManager.tokenMoveforDeflection != inferredTokenFromClick)
    //                     {
    //                         Debug.Log($"Switching Defender selection to {inferredTokenFromClick.name}. Clearing previous highlights.");
    //                         hexGrid.ClearHighlightedHexes();  // Clear the previous highlights
    //                     }

    //                     Debug.Log($"Selecting defender {inferredTokenFromClick.name}. Highlighting reachable hexes.");
    //                     shotManager.tokenMoveforDeflection = inferredTokenFromClick;  // Set selected token
    //                     movementPhaseManager.HighlightValidMovementHexes(inferredTokenFromClick, 2);  // Highlight reachable hexes within 3 moves
    //                     shotManager.isWaitingforBlockerSelection = false;
    //                     shotManager.isWaitingforBlockerMovement = true;
    //                     yield return null;
    //                 }
    //                 else {
    //                     Debug.LogWarning("Attacker clicked, while waiting for a defender to select.");
    //                 }
    //             }

    //             if (inferredHexCellFromClick != null && shotManager.isWaitingforBlockerMovement)
    //             {
    //                 Debug.Log($"Hex clicked: {inferredHexCellFromClick.name}");

    //                 // Ensure the hex is within the highlighted valid movement hexes
    //                 if (
    //                     hexGrid.highlightedHexes.Contains(inferredHexCellFromClick)
    //                     && !inferredHexCellFromClick.isAttackOccupied
    //                     && !inferredHexCellFromClick.isDefenseOccupied
    //                     && !inferredHexCellFromClick.isOutOfBounds
    //                 )
    //                 {
    //                     if (shotManager.tokenMoveforDeflection != null)
    //                     {
    //                         Debug.Log($"Moving {shotManager.tokenMoveforDeflection.name} to hex {inferredHexCellFromClick.coordinates}");

    //                         // Move the selected token to the valid hex (use the highPassManager's selectedToken)
    //                         yield return StartCoroutine(movementPhaseManager.MoveTokenToHex(inferredHexCellFromClick, shotManager.tokenMoveforDeflection, false));  // Pass the selected token
    //                         shotManager.CompleteDefenderMovement();
    //                     }
    //                     else
    //                     {
    //                         Debug.LogWarning("No token selected to move.");
    //                     }
    //                 }
    //                 else
    //                 {
    //                     Debug.LogWarning("Clicked hex is not a valid movement target.");
    //                 }
    //             }
    //             else if (
    //                 inferredHexCellFromClick != null // we clicked a Hex
    //                 && shotManager.isWaitingForTargetSelection // We are indeed waiting for a targetSelection
    //                 && hexGrid.highlightedHexes.Contains(inferredHexCellFromClick) // one of the target Hexes
    //             )
    //             {
    //                 Debug.Log($"Valid selected Target Hex: {inferredHexCellFromClick.name}");
    //                 shotManager.HandleTargetClick(inferredHexCellFromClick);                    
    //             }
    //             else
    //             {
    //                 Debug.LogWarning("No valid hex or token clicked.");
    //             }
    //         }
    //     }
    // }

    // private void HandleAttackerHeaderSelectionInput()
    // {
    //     if (Input.GetMouseButtonDown(0))
    //     {
    //         Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
    //         if (Physics.Raycast(ray, out RaycastHit hit))
    //         {
    //             PlayerToken token = hit.collider.GetComponent<PlayerToken>();
    //             if (token != null && token.isAttacker && headerManager.attEligibleToHead.Contains(token) && !headerManager.attackerWillJump.Contains(token))
    //             {
    //                 StartCoroutine(headerManager.HandleAttackerHeaderSelection(token));
    //             }
    //             else
    //             {
    //                 Debug.Log("Invalid attacker selected for header challenge.");
    //             }
    //         }
    //     }

    //     if (Input.GetKeyDown(KeyCode.X))
    //     {
    //         headerManager.ConfirmAttackerHeaderSelection();
    //     }
    //     if (Input.GetKeyDown(KeyCode.A))
    //     {
    //         headerManager.SelectAllAvailableAttackers();
    //     }
    // }

    // private void HandleDefenderHeaderSelectionInput()
    // {
    //     if (Input.GetMouseButtonDown(0))
    //     {
    //         Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
    //         if (Physics.Raycast(ray, out RaycastHit hit))
    //         {
    //             PlayerToken token = hit.collider.GetComponent<PlayerToken>();
    //             if (token == null)
    //             {
    //                 Debug.LogWarning("You did not click on a token");
    //                 return;
    //             }
    //             else
    //             {
    //                 if (token.isAttacker)
    //                 {
    //                     Debug.LogWarning($"{token.name} is not a defender! Rejecting input");
    //                 }
    //                 else if (!headerManager.defEligibleToHead.Contains(token))
    //                 {
    //                     Debug.LogWarning($"{token.name} is not eligible to Head! Rejecting input");
    //                 }
    //                 else if (headerManager.defenderWillJump.Contains(token))
    //                 {
    //                     Debug.LogWarning($"{token.name} has already declared to Jump for header. Rejecting input");
    //                     // TODO: Maybe deselect?
    //                 }
    //                 else
    //                 {
    //                     StartCoroutine(headerManager.HandleDefenderHeaderSelection(token));
    //                 }
    //             }
    //         }
    //         else Debug.LogWarning("Raycast did not hit any collider");
    //     }

    //     if (Input.GetKeyDown(KeyCode.X))
    //     {
    //         headerManager.ConfirmDefenderHeaderSelection();
    //     }
    //     if (Input.GetKeyDown(KeyCode.A))
    //     {
    //         headerManager.SelectAllAvailableDefenders();
    //     }
    // }

    // private void HandleFreeKickKickerSelection()
    // {
    //     if (MatchManager.Instance.currentState == MatchManager.GameState.FreeKickKickerSelect
    //         && freeKickManager.isWaitingForKickerSelection)
    //     {
    //         if (Input.GetMouseButtonDown(0))
    //         {
    //             Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
    //             if (Physics.Raycast(ray, out RaycastHit hit))
    //             {
    //                 if (hit.collider == null)
    //                 {
    //                     Debug.Log("Raycast did not hit any collider.");
    //                     return;
    //                 }
    //                 var (inferredTokenFromClick, inferredHexCellFromClick, isOOBClicked) =  DetectTokenOrHexClicked(hit);
    //                 if (isOOBClicked)
    //                 {
    //                   Debug.LogWarning("Out Of Bounds Plane hit, rejecting click");
    //                   return;
    //                 }
    //                 // Check if the ray hit a PlayerToken directly
    //                 Debug.Log($"Inferred Clicked Token: {inferredTokenFromClick?.name}");
    //                 Debug.Log($"Inferred Clicked Hex: {inferredHexCellFromClick.name}");
    //                 if (inferredTokenFromClick != null) StartCoroutine(freeKickManager.HandleKickerSelection(inferredTokenFromClick));
    //                 else Debug.Log($"There is no Token on {inferredHexCellFromClick.name}. Doing nothing!");
    //             }
    //         }
    //         else if (Input.GetKeyDown(KeyCode.X))
    //         {
    //             Debug.Log("Player pressed X to skip kicker selection.");
    //             StartCoroutine(freeKickManager.HandleKickerSelection());  // Pass no token to skip
    //         }
    //     }
    // }

    // private void HandleFreeKickSetupPhaseInput()
    // {
    //     if (freeKickManager.isWaitingForSetupPhase)
    //     {
    //         if (Input.GetMouseButtonDown(0))
    //         {
    //             Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
    //             if (Physics.Raycast(ray, out RaycastHit hit))
    //             {
    //                 if (hit.collider == null)
    //                 {
    //                     Debug.Log("Raycast did not hit any collider.");
    //                     return;
    //                 }
    //                 var (inferredTokenFromClick, inferredHexCellFromClick, isOOBClicked) =  DetectTokenOrHexClicked(hit);
    //                 if (isOOBClicked)
    //                 {
    //                   Debug.LogWarning("Out Of Bounds Plane hit, rejecting click");
    //                   return;
    //                 }
    //                 // Check if the ray hit a PlayerToken directly
    //                 Debug.Log($"Inferred Clicked Token: {inferredTokenFromClick?.name}");
    //                 Debug.Log($"Inferred Clicked Hex: {inferredHexCellFromClick.name}");
    //                 if (freeKickManager.isWaitingForFinalKickerSelection)
    //                 {
    //                     freeKickManager.selectedKicker = inferredTokenFromClick;
    //                     freeKickManager.AdvanceToNextPhase(MatchManager.GameState.FreeKickKickerSelect);
    //                     return;
    //                 }
    //                 if (freeKickManager.selectedToken != null)
    //                 {
    //                     if (inferredTokenFromClick != null && inferredTokenFromClick != freeKickManager.selectedToken)
    //                     {
    //                       Debug.Log($"New Clicked token during free kick setup: {inferredTokenFromClick.name}");
    //                       freeKickManager.HandleSetupTokenSelection(inferredTokenFromClick);
    //                       return;
    //                     }
    //                     if (inferredHexCellFromClick != null)
    //                     {
    //                         if (!inferredHexCellFromClick.isDefenseOccupied && !inferredHexCellFromClick.isAttackOccupied)
    //                         {
    //                             Debug.Log($"Token {freeKickManager.selectedToken.name} moving to Hex {inferredHexCellFromClick.coordinates}");
    //                             StartCoroutine(freeKickManager.HandleSetupHexSelection(inferredHexCellFromClick));
    //                         }
    //                         else
    //                         {
    //                             Debug.LogWarning($"Hex {inferredHexCellFromClick.coordinates} is occupied. Select an unoccupied Hex.");
    //                         }
    //                     }
    //                     else
    //                     {
    //                         Debug.LogWarning("Please click on a valid Hex to move the selected token.");
    //                     }
    //                     return;
    //                 }
    //                 else if (inferredTokenFromClick != null)
    //                 {
    //                     {
    //                         Debug.Log($"Clicked token during free kick setup: {inferredTokenFromClick.name}");
    //                         freeKickManager.HandleSetupTokenSelection(inferredTokenFromClick);
    //                         return;
    //                     }
    //                 }
    //                 else
    //                 {
    //                     if (freeKickManager.isWaitingforMovement3)
    //                     {
    //                         Debug.Log($"Clicked token during free kick setup: {inferredTokenFromClick.name}");
    //                         StartCoroutine(movementPhaseManager.MoveTokenToHex(inferredHexCellFromClick, freeKickManager.selectedToken, false));
    //                         // yield return StartCoroutine(movementPhaseManager.MoveTokenToHex(inferredHexCellFromClick));
    //                         return;
    //                     }
    //                     else
    //                     Debug.LogWarning($"Hex {inferredHexCellFromClick.name} is unoccupied. Please select a valid token.");
    //                     return;
    //                 }            
    //             }
    //         }
    //         else if (Input.GetKeyDown(KeyCode.X))
    //         {
    //             Debug.Log("Player attempts to forfeit the remaining moves for this phase.");
    //             freeKickManager.selectedToken = null;  // Reset the selected token
    //             freeKickManager.AttemptToAdvanceToNextPhase();
    //         }
    //     }
    // }

    // private void HandleFreeKickFinalKicker()
    // {
    //     if (Input.GetMouseButtonDown(0))
    //     {
    //         Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
    //         if (Physics.Raycast(ray, out RaycastHit hit))
    //         {
    //             if (hit.collider == null)
    //             {
    //                 Debug.Log("Raycast did not hit any collider.");
    //                 return;
    //             }
    //             var (inferredTokenFromClick, inferredHexCellFromClick, isOOBClicked) =  DetectTokenOrHexClicked(hit);
    //             if (isOOBClicked)
    //             {
    //                Debug.LogWarning("Out Of Bounds Plane hit, rejecting click");
    //                return;
    //             }
    //             // Check if the ray hit a PlayerToken directly
    //             Debug.Log($"Inferred Clicked Token: {inferredTokenFromClick?.name}");
    //             Debug.Log($"Inferred Clicked Hex: {inferredHexCellFromClick.name}");
    //             if (freeKickManager.isWaitingForFinalKickerSelection)
    //             {
    //                 freeKickManager.selectedKicker = inferredTokenFromClick;
    //                 freeKickManager.AdvanceToNextPhase(MatchManager.GameState.FreeKickDefineKicker);
    //                 return;
    //             }
    //         }
    //     }
    // }

    // private void HandleKickOffClicks()
    // {
    //     if (Input.GetMouseButtonDown(0))
    //     {
    //         Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
    //         if (Physics.Raycast(ray, out RaycastHit hit))
    //         {
    //             if (hit.collider == null)
    //             {
    //                 Debug.Log("Raycast did not hit any collider.");
    //                 return;
    //             }
    //             var (inferredTokenFromClick, inferredHexCellFromClick, isOOBClicked) = DetectTokenOrHexClicked(hit);
    //             if (isOOBClicked)
    //             {
    //                Debug.LogWarning("Out Of Bounds Plane hit, rejecting click");
    //                return;
    //             }
    //             // Check if the ray hit a PlayerToken directly
    //             Debug.Log($"Inferred Clicked Token: {inferredTokenFromClick?.name}");
    //             Debug.Log($"Inferred Clicked Hex: {inferredHexCellFromClick.name}");
    //             if (inferredTokenFromClick != null && inferredTokenFromClick != kickoffManager.selectedToken)
    //             {
    //                 kickoffManager.SelectToken(inferredTokenFromClick);
    //             }
    //             else if (inferredHexCellFromClick != null)
    //             {
    //                 StartCoroutine(kickoffManager.TryMoveToken(inferredHexCellFromClick));
    //             }
    //         }
    //     }
    //     if (Input.GetKeyDown(KeyCode.Space))
    //     {
    //         Debug.Log("Player pressed Sapce to try and start the match.");
    //         kickoffManager.ConfirmSetup();
    //     }
    // }
    
    public (PlayerToken inferredTokenFromClick, HexCell inferredHexCellFromClick, bool isOutOfBoundsClick) DetectTokenOrHexClicked(RaycastHit hit)
    {
        // Detect if we hit an Out-of-Bounds Plane
        if (hit.collider.name.Contains("Out-of-Bounds"))
        {
            if (logIsOn) Debug.Log($"🚫 Clicked on {hit.collider.name} - Out of Bounds area.");
            return (null, null, true);  // ✅ Indicate this was an OOB click
        }
        Ball clickedBall = hit.collider.GetComponent<Ball>();
        HexCell clickedBallHex = null;
        PlayerToken clickedBallToken = null;
        if (clickedBall != null)
        {
            clickedBallHex = clickedBall.GetCurrentHex(); 
            clickedBallToken = clickedBallHex?.GetOccupyingToken();
            if (logIsOn)
            {
                if (clickedBallToken != null) {Debug.Log($"Raycast hit the Ball, and {clickedBallToken.name} controls it, on {clickedBallHex.name}");}
                else {Debug.Log($"Raycast hit the Ball, on {clickedBallHex.name}");}
            }
        }
        PlayerToken clickedToken = hit.collider.GetComponent<PlayerToken>();
        HexCell clickedTokenHex = null;
        if (clickedToken != null)
        {
            clickedTokenHex = clickedToken.GetCurrentHex();
            if (logIsOn) Debug.Log($"Raycast hit {clickedToken.name} on {clickedTokenHex.name}.");
        }
        HexCell clickedHex = hit.collider.GetComponent<HexCell>();
        PlayerToken tokenOnClickedHex = null;
        if (clickedHex != null)
        {
            tokenOnClickedHex = clickedHex.GetOccupyingToken();
            if (logIsOn)
            {
                if (tokenOnClickedHex != null)
                {
                    Debug.Log($"Raycast hit {clickedHex.name}, where {tokenOnClickedHex.name} is on");
                }
                else 
                {
                    Debug.Log($"Raycast hit {clickedHex.name}, which is not occupied ");
                }
            }
        }
        PlayerToken inferredToken = clickedBallToken ?? clickedToken ?? tokenOnClickedHex ?? null;
        HexCell inferredHexCell = clickedBallHex ?? clickedTokenHex ?? clickedHex ?? null;
        if (logIsOn)
        {
            Debug.Log($"Inferred Clicked Token: {inferredToken?.name}");
            Debug.Log($"Inferred Clicked Hex: {inferredHexCell.name}"); 
        }
        return (inferredToken, inferredHexCell, false);      
    }

    public void SimulateClickAt(Vector2Int hexCoords)
    {
        Vector3Int coords = new Vector3Int(hexCoords.x, 0, hexCoords.y);
        HexCell hex = hexGrid.GetHexCellAt(coords);
        if (hex == null)
        {
            Debug.LogWarning($"No HexCell found at {coords}");
            return;
        }

        PlayerToken token = hex.GetOccupyingToken();

        Debug.Log($"🧪 Simulated click at {coords}. Hex: {hex.name}, Token: {(token != null ? token.name : "None")}");
        OnClick?.Invoke(token, hex);
    }

    public void SimulateKeyDataPress(KeyCode key, bool shift = false, bool ctrl = false, bool alt = false)
    {
        var keyData = new KeyPressData(key,shift,ctrl,alt);
        OnKeyPress?.Invoke(keyData);
    }

    public IEnumerator DelayedClick(Vector2Int pos, float delay)
    {
        yield return new WaitForSeconds(delay);
        SimulateClickAt(pos);
    }

    public IEnumerator DelayedKeyDataPress(KeyCode key, float delay, bool shift = false, bool ctrl = false, bool alt = false)
    {
        yield return new WaitForSeconds(delay);
        SimulateKeyDataPress(key);
    }

    public string GetDebugStatus()
    {
        StringBuilder sb = new();
        sb.Append("GIM: ");

        if (clickedToken != null) sb.Append($"clickedToken: {clickedToken.name}, ");
        if (clickedHex != null) sb.Append($"clickedHex: {clickedHex.name}, ");
        if (hoveredToken != null) sb.Append($"hoveredToken: {hoveredToken.name}, ");
        if (hoveredHex != null) sb.Append($"hoveredHex: {hoveredHex.name}, ");

        if (sb.Length >= 2 && sb[^2] == ',') sb.Length -= 2; // Trim trailing comma
        return sb.ToString();
    }
}