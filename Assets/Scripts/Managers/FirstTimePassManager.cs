using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Unity.VisualScripting;

public class FirstTimePassManager : MonoBehaviour
{
    [Header("Dependencies")]
    public Ball ball;
    public HexGrid hexGrid;
    public MovementPhaseManager movementPhaseManager;
    public GameInputManager gameInputManager;
    [Header("Runtime Items")]
    public bool isAvailable = false;
    public bool isActivated = false;        // To check if the script is activated
    public bool isAwaitingTargetSelection = false; // To check if we are waiting for target selection
    public bool isWaitingForAttackerSelection = false; // To check if we are waiting for attacker selection
    public bool isWaitingForDefenderSelection = false; // To check if we are waiting for defender selection
    public bool isWaitingForAttackerMove = false; // To check if we are waiting for attacker selection
    public bool isWaitingForDefenderMove = false; // To check if we are waiting for defender selection
    public bool isWaitingForDiceRoll = false; // To check if we are waiting for dice rolls
    [Header("Others")]
    [SerializeField]
    private HexCell currentTargetHex = null;   // The currently selected target hex
    public PlayerToken selectedToken;  // To store the selected attacker or defender token
    private HexCell lastClickedHex = null;     // The last hex that was clicked
    private bool isWaitingForConfirmation = false;
    private HexCell currentDefenderHex = null; // The defender hex currently rolling the dice
    // private int diceRollsPending = 0; // Number of pending dice rolls
    private bool isDangerous = false;
    private List<(PlayerToken defender, bool isCausingInvalidity)> onPathDefendersList;

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
            if (isAwaitingTargetSelection)
            {
                HandleFTPBallPath(hex);
            }
            else if (isWaitingForAttackerSelection)
            {
                if (isWaitingForAttackerMove && hexGrid.highlightedHexes.Contains(hex))
                {
                    Debug.Log($"Valid Hex to move the Attacker.");
                    StartCoroutine(MoveSelectedAttackerToHex(hex));
                    return;
                }
                if (
                    token == null // empty hex Clicked
                    || !token.isAttacker // Hex of a non attacker was clicked
                )
                {
                    // Rejection case — either nothing was clicked, or it was a defender or invalid hex
                    Debug.LogWarning("Invalid token or Not an attacker clicked. Please click on an attacker.");
                    hexGrid.ClearHighlightedHexes();
                    selectedToken = null;
                    isWaitingForAttackerMove = false;  // Stop waiting for attacker move
                    return;
                }
                // Clicked on an attacker token
                if (selectedToken == null || selectedToken != token) // No previous selection of attacker
                {
                    // First attacker click or switching attacker
                    Debug.Log($"Attacker {token.name} selected.");
                    selectedToken = token;
                    hexGrid.ClearHighlightedHexes();
                    movementPhaseManager.HighlightValidMovementHexes(selectedToken, 1);
                    isWaitingForAttackerMove = true;
                }
                else if (selectedToken == token)
                {
                    Debug.Log($"Attacker {token.name} already selected. Please click on a Higlighted Hex to move them there!");
                }
            }
            else if (isWaitingForDefenderSelection)
            {
                if (isWaitingForDefenderMove && hexGrid.highlightedHexes.Contains(hex))
                {
                    Debug.Log($"Valid Hex to move the Defender.");
                    StartCoroutine(MoveSelectedDefenderToHex(hex));
                    return;
                }
                if (
                    token == null // empty hex Clicked
                    || token.isAttacker // Hex of a non attacker was clicked
                )
                {
                    // Rejection case — either nothing was clicked, or it was a defender or invalid hex
                    Debug.LogWarning("Invalid token or Not a Defender clicked. Please click on a Defender.");
                    hexGrid.ClearHighlightedHexes();
                    selectedToken = null;
                    isWaitingForDefenderMove = false;  // Stop waiting for attacker move
                    return;
                }
                // Clicked on an attacker token
                if (selectedToken == null || selectedToken != token) // No previous selection of attacker
                {
                    // First attacker click or switching attacker
                    Debug.Log($"Defender {token.name} selected.");
                    selectedToken = token;
                    hexGrid.ClearHighlightedHexes();
                    movementPhaseManager.HighlightValidMovementHexes(selectedToken, 1);
                    isWaitingForDefenderMove = true;
                }
                else if (selectedToken == token)
                {
                    Debug.Log($"Defender {token.name} already selected. Please click on a Higlighted Hex to move them there!");
                }
            }
        }
    }

    private void OnKeyReceived(KeyPressData keyData)
    {
        // return;
        if (isAvailable && !isActivated && keyData.key == KeyCode.F)
        {
            ActivateFTP();
        }
        if (isActivated)
        {
            if (isWaitingForDiceRoll && keyData.key == KeyCode.R)
            {
                // Check if waiting for dice rolls and the R key is pressed
                PerformFTPInterceptionRolls();  // Trigger the dice roll when R is pressed
            }
        }
    }

    private void ActivateFTP()
    {
        MatchManager.Instance.TriggerFTP();
        ball.SelectBall();
        Debug.Log("First Time pass attempt mode activated.");
        isActivated = true;
        isAvailable = false;  // Make it non available to avoid restarting this action again.
        // if (MatchManager.Instance.difficulty_level == 3) CommitToThisAction();
        isAwaitingTargetSelection = true;
        Debug.Log("FirstTimePassManager activated. Waiting for target selection...");
    }

    public void HandleFTPBallPath(HexCell clickedHex)
    {
        if (clickedHex != null)
        {
            // Debug.Log($"FTP Path: Clicked hex: {clickedHex.coordinates}");
            HexCell ballHex = ball.GetCurrentHex();
            if (ballHex == null)
            {
                Debug.LogError("Ball's current hex is null! Ensure the ball has been placed on the grid.");
                return;
            }
            else
            {
                // Now handle the pass based on difficulty
                HandleFTPBasedOnDifficulty(clickedHex);
            }   
        }
    }

    void HandleFTPBasedOnDifficulty(HexCell clickedHex)
    {
        // Debug.Log("Hello from FTP based on Diff");
        int difficulty = MatchManager.Instance.difficulty_level;  // Get current difficulty
        // Centralized path validation and danger assessment
        var (isValid, isDangerous, pathHexes, onPathDefenders) = ValidateFTPPath(clickedHex, false);  // Before moves
        // TODO populate the below only when the above is called with True
        onPathDefendersList = onPathDefenders;
        if (!isValid)
        {
            // Debug.LogWarning("Invalid pass. Path rejected.");
            currentTargetHex = null;  // Assign the current target hex
            return; // Reject invalid paths
        }
        currentTargetHex = clickedHex;  // Assign the current target hex
        // TODO Hard Mode in FTP
        // Handle each difficulty's behavior
        // if (difficulty == 3) // Hard Mode
        // {
        //     PopulateGroundPathInterceptions(clickedHex);
        //     if (passIsDangerous)
        //     {
        //         diceRollsPending = defendingHexes.Count; // is this relevant here?
        //         Debug.Log($"Dangerous pass detected. Waiting for {diceRollsPending} dice rolls...");
        //         StartGroundPassInterceptionDiceRollSequence();
        //     }
        //     else
        //     {
        //         Debug.Log("Pass is not dangerous, moving ball.");
        //         StartCoroutine(HandleGroundBallMovement(clickedHex)); // Execute pass
        //         MatchManager.Instance.UpdatePossessionAfterPass(clickedHex);
        //         if (clickedHex.isAttackOccupied)
        //         {
        //             MatchManager.Instance.currentState = MatchManager.GameState.StandardPassCompletedToPlayer;
        //         }
        //         else {
        //             MatchManager.Instance.currentState = MatchManager.GameState.StandardPassCompletedToSpace;
        //         }
        //     }
        //     ball.DeselectBall();
        // }
        // else
        if (difficulty == 2)
        {
            hexGrid.ClearHighlightedHexes();
            // Second click: Confirm the target and proceed to the movement phase
            if (clickedHex == currentTargetHex && clickedHex == lastClickedHex)
            {
                Debug.Log("First-Time Pass target confirmed. Waiting for movement phases.");
                isAwaitingTargetSelection = false;  // Stop waiting for target selection
                MatchManager.Instance.gameData.gameLog.LogEvent(MatchManager.Instance.LastTokenToTouchTheBallOnPurpose, MatchManager.ActionType.PassAttempt); // Log CompletedPass
                MatchManager.Instance.currentState = MatchManager.GameState.FirstTimePassAttackerMovement;
                StartAttackerMovementPhase();  // Allow attacker to move 1 hex
            }
            // First click: Highlight and wait
            else
            {
                hexGrid.ClearHighlightedHexes();
                HighlightValidFTPPath(pathHexes, isDangerous);
                currentTargetHex = clickedHex;
                lastClickedHex = clickedHex;  // Set for confirmation click
                Debug.Log($"First click registered. Click again to confirm the First-Time Pass. Path is {(isDangerous ? "dangerous" : "safe")}.");
            }
        }
        // TODO Easy Mode in FTP
        // else if (difficulty == 1) // Easy Mode: Handle hover and clicks with immediate highlights
        // {
        //     PopulateGroundPathInterceptions(clickedHex);
        //     diceRollsPending = defendingHexes.Count; // is this relevant here?
        //     Debug.Log($"Dangerous pass detected. If you confirm there will be {diceRollsPending} dice rolls...");
        //     if (clickedHex == currentTargetHex && clickedHex == lastClickedHex)
        //     {
        //         // Second click on the same hex: confirm the pass
        //         Debug.Log("Second click detected, confirming pass...");
        //         PopulateGroundPathInterceptions(clickedHex);
        //         if (passIsDangerous)
        //         {
        //             diceRollsPending = defendingHexes.Count; // is this relevant here?
        //             Debug.Log($"Dangerous pass detected. Waiting for {diceRollsPending} dice rolls...");
        //             StartGroundPassInterceptionDiceRollSequence();
        //         }
        //         else
        //         {
        //             Debug.Log("Pass is not dangerous, moving ball.");
        //             StartCoroutine(HandleGroundBallMovement(clickedHex)); // Execute pass
        //             MatchManager.Instance.UpdatePossessionAfterPass(clickedHex);
        //             if (clickedHex.isAttackOccupied)
        //             {
        //                 MatchManager.Instance.currentState = MatchManager.GameState.StandardPassCompletedToPlayer;
        //             }
        //             else {
        //                 MatchManager.Instance.currentState = MatchManager.GameState.StandardPassCompletedToSpace;
        //             }
        //         }
        //         ball.DeselectBall();
        //     }
        //     else
        //     {
        //         hexGrid.ClearHighlightedHexes();
        //         HighlightValidFTPPath(pathHexes, isDangerous);
        //         currentTargetHex = clickedHex; // Set this as the current target hex
        //         lastClickedHex = clickedHex; // Track the last clicked hex
        //     }
        // }
    }

    public (
        bool isValid
        , bool isDangerous
        , List<HexCell> pathHexes
        , List<(PlayerToken defender, bool isCausingInvalidity)> onPathDefenders
    ) ValidateFTPPath(HexCell targetHex, bool isAfterMovement = false)
    {
        hexGrid.ClearHighlightedHexes();
        HexCell ballHex = ball.GetCurrentHex();
        List<(PlayerToken defender, bool isCausingInvalidity)> onPathDefenders = new List<(PlayerToken defender, bool isCausingInvalidity)>();
        // HashSet<PlayerToken> processedDefenders = new HashSet<PlayerToken>();  // Track defenders already processed

        // Step 1: Ensure the ballHex and targetHex are valid
        if (ballHex == null || targetHex == null)
        {
            Debug.LogError("Ball or target hex is null!");
            return (false, false, null, onPathDefenders);
        }

        // Step 2: Calculate the path between the ball and the target hex
        List<HexCell> pathHexes = CalculateThickPath(ballHex, targetHex, ball.ballRadius);
        Vector3Int ballCubeCoords = HexGridUtils.OffsetToCube(ballHex.coordinates.x, ballHex.coordinates.z);
        Vector3Int targetCubeCoords = HexGridUtils.OffsetToCube(targetHex.coordinates.x, targetHex.coordinates.z);
        int distance = HexGridUtils.GetHexDistance(ballCubeCoords, targetCubeCoords);
        // Check the distance limit (FTP limit should be 6 hexes)
        if (distance > 6)
        {
            Debug.LogWarning($"First-Time Pass is out of range. Maximum steps allowed: 6. Current steps: {distance}");
            return (false, false, pathHexes, onPathDefenders);
        }
        // Step 3: If checking after movement, verify that no defender is directly on the path
        bool isValid = true;
        PlayerToken invalidityCausingDefender = null;  // Track the defender causing invalidity
        foreach (HexCell hex in pathHexes)
        {
            if (hex.isDefenseOccupied)
            {
                PlayerToken defenderOnPath = hex.GetOccupyingToken();
                if (!isAfterMovement)
                {
                    Debug.Log($"FTP Not valid. Path blocked by defender at hex: {hex.coordinates}. Defender: {defenderOnPath.name}");
                    return (false, false, pathHexes, onPathDefenders);  // Reject the path
                }
                else
                {
                    // After movement: Defender on the path causes the pass to become dangerous
                    onPathDefenders.Add((defenderOnPath, true));  // Add defender as blocking path
                    invalidityCausingDefender = defenderOnPath;  // Keep track for later rolls
                    Debug.Log($"Path blocked by defender at hex: {hex.coordinates}. Defender: {defenderOnPath.name}");
                    isValid = false;
                    // processedDefenders.Add(defenderOnPath);  // Mark defender as processed
                }
            }
        }
        // Step 4: Get defenders and their ZOI (neighbors)
        List<HexCell> defenderHexes = hexGrid.GetDefenderHexes();
        List<HexCell> defenderNeighbors = hexGrid.GetDefenderNeighbors(defenderHexes);
        // foreach (HexCell thomas in defenderHexes){thomas.HighlightHex("DefenderZOI");}
        // Step 5: Determine if the path is dangerous by checking if it passes through any defender's ZOI
        foreach (HexCell hex in pathHexes)
        {
            foreach (HexCell neighbor in hex.GetNeighbors(hexGrid))
            {
                if (defenderNeighbors.Contains(hex) && !neighbor.isAttackOccupied)  // Ignore attack-occupied hexes
                {
                    // Check if a defender is already processed as causing invalidity
                    PlayerToken defenderInZOI = neighbor.GetOccupyingToken();
                    if (defenderInZOI != null) // Avoid adding the same defender twice)
                    {
                        bool isCausingInvalidity = defenderInZOI == invalidityCausingDefender;
                        if (!onPathDefenders.Exists(d => d.defender == defenderInZOI))
                        {
                            onPathDefenders.Add((defenderInZOI, isCausingInvalidity));  // Add as a potential interceptor
                            Debug.Log($"Defender {defenderInZOI.name} can intercept through ZOI at hex: {hex.coordinates}");
                        }
                        else
                        {
                            Debug.Log($"Skipping already processed defender: {defenderInZOI.name}");
                        }
                    }
                }
            }
        }
        // Debug.Log($"isValid: {isValid}, isDangerous: {isDangerous}, pathHexes count: {pathHexes.Count}, onPathDefenders count: {onPathDefenders.Count}");
        // Debug log for output comparison
        Debug.Log($"[ValidateFTPPath] isValid: {isValid}, isDangerous: {isDangerous}, onPathDefendersList: [{string.Join(", ", onPathDefenders.Select(d => $"{d.defender.name} (Blocking: {d.isCausingInvalidity})"))}]");
        return (isValid, isDangerous, pathHexes, onPathDefenders);
    }

    public void HighlightValidFTPPath(List<HexCell> pathHexes, bool isDangerous)
    {
        foreach (HexCell hex in pathHexes)
        {
            if (hex == null) continue; // to next hex (loop)
            hex.HighlightHex(isDangerous ? "dangerousPass" : "ballPath");
            hexGrid.highlightedHexes.Add(hex);  // Track the highlighted hexes
        }
    }

    private void StartAttackerMovementPhase()
    {
        Debug.Log("Attacker movement phase started. Move one attacker 1 hex.");
        isWaitingForAttackerSelection = true;
        isWaitingForConfirmation = false;  // Now allow token selection since confirmation is done
        selectedToken = null;  // Ensure no token is auto-selected
        // Set game state to reflect we are in the attacker’s movement phase
        MatchManager.Instance.currentState = MatchManager.GameState.FirstTimePassAttackerMovement;
    }

    public void StartDefenderMovementPhase()
    {
        Debug.Log("Defender movement phase started. Move one defender 1 hex.");
        isWaitingForConfirmation = false;  // Now allow token selection since confirmation is done
        isWaitingForDefenderSelection = true;
        selectedToken = null;  // Ensure no token is auto-selected
        // Set game state to reflect we are in the defender’s movement phase
        MatchManager.Instance.currentState = MatchManager.GameState.FirstTimePassDefenderMovement;
    }

    public void CompleteDefenderMovementPhase()
    {
        // Step 1: Validate the path after defender movements
        var (isValid, isDangerous, pathHexes, onPathDefenders) = ValidateFTPPath(currentTargetHex, true);
        onPathDefendersList = onPathDefenders; // Store defenders from Validate
        // Step 2: Check for interception chances or dangerous paths
        if (onPathDefendersList.Count > 0)
        {
            Debug.Log($"Interception chance: {onPathDefendersList.Count} defenders can intercept the pass.");
            // Start dice roll sequence for interceptions
            StartFTPInterceptionDiceRollSequence();
        }
        else
        {
            Debug.Log("No interception chance. Moving ball to target hex.");
            StartCoroutine(MovePassNotIntercepted(currentTargetHex));
        }
    }

    private IEnumerator MoveSelectedAttackerToHex(HexCell hex)
    {
        hexGrid.ClearHighlightedHexes();
        isWaitingForAttackerMove = false;  // Stop waiting for attacker move
        isWaitingForAttackerSelection = false;  // Stop waiting for attacker selection
        Debug.Log($"Moving {selectedToken.name} to hex {hex.coordinates}");
        yield return StartCoroutine(movementPhaseManager.MoveTokenToHex(hex, selectedToken, false));  // Pass the selected token
        movementPhaseManager.isMovementPhaseInProgress = false;
        selectedToken = null;
        StartDefenderMovementPhase();
    }
    
    private IEnumerator MoveSelectedDefenderToHex(HexCell hex)
    {
        hexGrid.ClearHighlightedHexes();
        isWaitingForDefenderMove = false;  // Stop waiting for attacker move
        isWaitingForDefenderSelection = false;  // Stop waiting for attacker selection
        Debug.Log($"Moving {selectedToken.name} to hex {hex.coordinates}");
        yield return StartCoroutine(movementPhaseManager.MoveTokenToHex(hex, selectedToken, false));  // Pass the selected token
        movementPhaseManager.isMovementPhaseInProgress = false;
        selectedToken = null;
        CompleteDefenderMovementPhase();
    }

    private IEnumerator MovePassNotIntercepted(HexCell hex)
    {
        if (currentTargetHex.isAttackOccupied)
        {
            MatchManager.Instance.gameData.gameLog.LogEvent(MatchManager.Instance.LastTokenToTouchTheBallOnPurpose, MatchManager.ActionType.PassCompleted);
            MatchManager.Instance.SetLastToken(currentTargetHex.GetOccupyingToken());
        }
        else
        {
            MatchManager.Instance.hangingPassType = "ground";
        }
        yield return StartCoroutine(HandleGroundBallMovement(currentTargetHex));
        MatchManager.Instance.UpdatePossessionAfterPass(currentTargetHex);
        MatchManager.Instance.currentState = MatchManager.GameState.FTPCompleted;
        CleanUpFTP();
    }

    private void CleanUpFTP()
    {
        // Clear all highlighted hexes
        hexGrid.ClearHighlightedHexes();
        isAvailable = false;
        // Reset all relevant variables
        isActivated = false;
        isAwaitingTargetSelection = false;
        isWaitingForAttackerSelection = false;
        isWaitingForDefenderSelection = false;
        isWaitingForAttackerMove = false;
        isWaitingForDefenderMove = false;
        isWaitingForConfirmation = false;
        selectedToken = null;  // Reset selected token
        currentTargetHex = null;  // Reset current target hex
    }
    
    void StartFTPInterceptionDiceRollSequence()
    {
        Debug.Log($"Defenders with interception chances: {onPathDefendersList.Count}");
        if (onPathDefendersList.Count == 0)
        {
            Debug.LogWarning("No defenders available for interception rolls.");
            return;
        }
        if (onPathDefendersList.Count > 0)
        {
            // Start the dice roll process for each defender
            Debug.Log("Starting dice roll sequence... Press R key.");
            // Sort defendingHexes by distance from ballHex
            onPathDefendersList = onPathDefendersList.OrderBy(d => 
            HexGridUtils.GetHexDistance(ball.GetCurrentHex().coordinates, d.defender.GetCurrentHex().coordinates)).ToList(); // TODO check if we need cubes first
            currentDefenderHex = onPathDefendersList[0].defender.GetCurrentHex();  // Start with the closest defender
            isWaitingForDiceRoll = true;
        }
        else
        {
            Debug.LogError("No defenders in ZOI. This should never appear because the path should have been clear.");
            StartCoroutine(MovePassNotIntercepted(currentTargetHex));
        }
    }
    
    void PerformFTPInterceptionRolls()
    {
        if (currentDefenderHex != null)
        {
            // Find the current defender's entry in the list of defenders
            var currentDefenderEntry = onPathDefendersList
                .Find(d => d.defender.GetCurrentHex() == currentDefenderHex);

            if (currentDefenderEntry.defender != null)
            {
                // Retrieve defender attributes
                PlayerToken defenderToken = currentDefenderEntry.defender;
                int tackling = defenderToken.tackling;
                // string defenderName = defenderToken.playerName;
                // int jerseyNumber = defenderToken.jerseyNumber;

                // Roll the dice
                var (returnedRoll, returnedJackpot) = MatchManager.Instance.DiceRoll();
                // int diceRoll = returnedRoll;
                int diceRoll = 4;
                Debug.Log($"Dice roll by {defenderToken.name} at {currentDefenderHex.coordinates}: {diceRoll}");

                // Calculate interception conditions
                bool isCausingInvalidity = currentDefenderEntry.isCausingInvalidity;
                int requiredRoll = isCausingInvalidity ? 5 : 6; // Base roll requirement
                bool successfulInterception = diceRoll >= requiredRoll || diceRoll + tackling >= 10;

                if (successfulInterception)
                {
                    Debug.Log($"Pass intercepted by {defenderToken.name} at {currentDefenderHex.coordinates}!");
                    MatchManager.Instance.gameData.gameLog.LogEvent(
                        defenderToken
                        , MatchManager.ActionType.InterceptionSuccess
                        , recoveryType: "ftp"
                        , connectedToken: MatchManager.Instance.LastTokenToTouchTheBallOnPurpose
                    );
                    MatchManager.Instance.SetLastToken(defenderToken);
                    StartCoroutine(HandleBallInterception(currentDefenderHex));
                    ResetFTPInterceptionDiceRolls(); // Reset interception process
                    CleanUpFTP();
                }
                else
                {
                    Debug.Log($"{defenderToken.name} at {currentDefenderHex.coordinates} failed to intercept.");

                    // Remove this defender and move to the next
                    onPathDefendersList.Remove(currentDefenderEntry);

                    if (onPathDefendersList.Count > 0)
                    {
                        // Move to the next defender
                        currentDefenderHex = onPathDefendersList[0].defender.GetCurrentHex();
                        Debug.Log("Starting next dice roll sequence... Press R key.");
                        isWaitingForDiceRoll = true; // Wait for the next roll
                    }
                    else
                    {
                        // No more defenders, pass is successful
                        Debug.Log("Pass successful! No more defenders to roll.");
                        StartCoroutine(MovePassNotIntercepted(currentTargetHex));
                    }
                }
            }
            else
            {
                Debug.LogError("No matching defender found for interception rolls.");
            }
        }
    }

    private IEnumerator HandleBallInterception(HexCell defenderHex)
    {
        yield return StartCoroutine(HandleGroundBallMovement(defenderHex));  // Move the ball to the defender's hex

        // Call UpdatePossessionAfterPass after the ball has moved to the defender's hex
        MatchManager.Instance.ChangePossession();  // Possession is now changed to the other team
        MatchManager.Instance.currentState = MatchManager.GameState.LooseBallPickedUp;
        MatchManager.Instance.UpdatePossessionAfterPass(defenderHex);  // Update possession after the ball has reached the defender's hex
    }

    void ResetFTPInterceptionDiceRolls()
    {
        onPathDefendersList.Clear();
        currentDefenderHex = null;
    }

    public IEnumerator HandleGroundBallMovement(HexCell targetHex)
    {
        // Ensure the ball and targetHex are valid
        if (ball == null)
        {
            Debug.LogError("Ball reference is null in HandleGroundBallMovement!");
            yield break;
        }
        if (targetHex == null)
        {
            Debug.LogError("Target Hex is null in HandleGroundBallMovement!");
            Debug.LogError($"currentTargetHex: {currentTargetHex}, isWaitingForDiceRoll: {isWaitingForDiceRoll}");
            yield break;
        }
        // Set thegame status to StandardPassMoving
        // MatchManager.Instance.currentState = MatchManager.GameState.StandardPassMoving;
        // Wait for the ball movement to complete
        yield return StartCoroutine(ball.MoveToCell(targetHex));
        // Adjust the ball's height based on occupancy (after movement is completed)
        ball.AdjustBallHeightBasedOnOccupancy();  // Ensure this method is public in Ball.cs
        // Now clear the highlights after the movement
        hexGrid.ClearHighlightedHexes();
        Debug.Log("Highlights cleared after ball movement.");
    }

    // TODO use the same one from GroundBallManager
    public List<HexCell> CalculateThickPath(HexCell startHex, HexCell endHex, float ballRadius)
    {
        List<HexCell> path = new List<HexCell>();
        string logContent = $"Ball Radius: {ballRadius}\n";
        string startHexCoordinates = $"({startHex.coordinates.x}, {startHex.coordinates.z})";
        string endHexCoordinates = $"({endHex.coordinates.x}, {endHex.coordinates.z})";
        logContent += $"Starting Hex: {startHexCoordinates}, Target Hex: {endHexCoordinates}\n";

        // Get world positions of the start and end hex centers
        Vector3 startPos = startHex.GetHexCenter();
        Vector3 endPos = endHex.GetHexCenter();

        // Step 2: Get a list of candidate hexes based on the bounding box
        List<HexCell> candidateHexes = GetCandidateGroundPathHexes(startHex, endHex, ballRadius);

        // Step 3: Loop through the candidate hexes and check distances to the parallel lines
        foreach (HexCell candidateHex in candidateHexes)
        {
            Vector3 candidatePos = candidateHex.GetHexCenter();

            // Check the distance from the candidate hex to the main line
            float distanceToLine = DistanceFromPointToLine(candidatePos, startPos, endPos);

            if (distanceToLine <= ballRadius)
            {
                // Hex is within the thick path
                if (!path.Contains(candidateHex))
                {
                    path.Add(candidateHex);
                    logContent += $"Added Hex: ({candidateHex.coordinates.x}, {candidateHex.coordinates.z}), Distance to Line: {distanceToLine}, Radius: {ballRadius}\n";
                }
            }
            else
            {
                logContent += $"Not Added: ({candidateHex.coordinates.x}, {candidateHex.coordinates.z}), Distance: {distanceToLine} exceeds Ball Radius: {ballRadius}\n";
            }
        }
        path.Remove(startHex);
        // Log the final highlighted path to the file
        string highlightedPath = "Highlighted Path: ";
        foreach (HexCell hex in path)
        {
            highlightedPath += $"({hex.coordinates.x}, {hex.coordinates.z}), ";
        }
        highlightedPath = highlightedPath.TrimEnd(new char[] { ',', ' ' });
        logContent += highlightedPath;

        // Save the log to a file
        SaveLogToFile(logContent, startHexCoordinates, endHexCoordinates);

        return path;
    }

    // TODO use the same one from GroundBallManager
    public List<HexCell> GetCandidateGroundPathHexes(HexCell startHex, HexCell endHex, float ballRadius)
    {
        List<HexCell> candidates = new List<HexCell>();
        // Get the axial coordinates of the start and end hexes
        Vector3Int startCoords = startHex.coordinates;
        Vector3Int endCoords = endHex.coordinates;

        // Determine the bounds (min and max x and z)
        int minX = Mathf.Min(startCoords.x, endCoords.x) - 1;
        int maxX = Mathf.Max(startCoords.x, endCoords.x) + 1;
        int minZ = Mathf.Min(startCoords.z, endCoords.z) - 1;
        int maxZ = Mathf.Max(startCoords.z, endCoords.z) + 1;

        // Loop through all hexes in the bounding box
        for (int x = minX; x <= maxX; x++)
        {
            for (int z = minZ; z <= maxZ; z++)
            {
                Vector3Int coords = new Vector3Int(x, 0, z);
                HexCell hex = hexGrid.GetHexCellAt(coords);

                if (hex != null)
                {
                    candidates.Add(hex);
                }
            }
        }
        return candidates;
    }
    
    // TODO use the same one from GroundBallManager
    float DistanceFromPointToLine(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
    {
        Vector3 lineDirection = lineEnd - lineStart;
        float lineLength = lineDirection.magnitude;
        lineDirection.Normalize();

        // Project the point onto the line, clamping between the start and end of the line
        float projectedLength = Mathf.Clamp(Vector3.Dot(point - lineStart, lineDirection), 0, lineLength);

        // Calculate the closest point on the line
        Vector3 closestPoint = lineStart + lineDirection * projectedLength;

        // Return the distance from the point to the closest point on the line
        return Vector3.Distance(point, closestPoint);
    }

    void SaveLogToFile(string logText, string startHex, string endHex)
    {
        // // Define the file path (you can customize this path)
        // string filePath = Application.dataPath + $"/Logs/HexPath_{startHex}_to_{endHex}.txt";

        // // Ensure the directory exists
        // Directory.CreateDirectory(Path.GetDirectoryName(filePath));

        // // Write the log text to the file (overwrite mode)
        // using (StreamWriter writer = new StreamWriter(filePath))
        // {
        //     writer.WriteLine(logText);
        // }

        // Debug.Log($"Log saved to: {filePath}");
    }

}