using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.VisualScripting;
using UnityEngine.Analytics;
using System;
using Unity.VisualScripting.ReorderableList.Element_Adder_Menu;
using System.Data.Common;
using System.Threading.Tasks;

public class ShotManager : MonoBehaviour
{
    [Header("Dependencies")]
    public MovementPhaseManager movementPhaseManager;
    public GameInputManager gameInputManager;
    public GroundBallManager groundBallManager;
    public LooseBallManager looseBallManager;
    public FinalThirdManager finalThirdManager;
    public LongBallManager longBallManager;
    public GoalKeeperManager goalKeeperManager;
    public GoalFlowManager goalFlowManager;
    public HelperFunctions helperFunctions;
    public HexGrid hexGrid;
    public Ball ball;
    [Header("Flags")]
    public bool isAvailable = false;
    public bool isActivated = false;  // Tracks if a shot is active
    public bool isWaitingforBlockerSelection = false;  // Tracks if we are waiting to select a blocker
    public bool isWaitingforBlockerMovement = false;  // Tracks if we are waiting for the selected blocker to move
    public bool isWaitingForTargetSelection = false;  // Tracks if we are waiting for shot target selection
    public bool isWaitingForBlockDiceRoll = false;  // Tracks we are in the Blocking Phase
    public bool isWaitingForShotRoll = false;  // Tracks we are in the Blocking Phase
    public bool isWaitingForGKDiceRoll = false;
    public bool isWaitingforHandlingTest = false;
    public bool isWaitingForSaveandHoldScenario = false;
    public bool gkWasOfferedMoveForBox = false;
    public string shotType; // "snapshot" or "fullPower"
    [Header("Important Runtime Items")]
    public PlayerToken shooter; // The token that is shooting
    public int totalShotPower;
    public int shooterRoll;
    public string boxPenalty;
    public string snapPenalty;
    public PlayerToken tokenMoveforDeflection; // The token that is shooting
    public HexCell targetHex; // The CanShootTo hex selected by the attacker
    public HexCell saveHex; // The hex (if any) where the GK will make the save on.
    public HexCell currentDefenderBlockingHex; // The Hex of the defender currently attempting to intercept
    private List<HexCell> trajectoryPath; // The list of hexes the ball will travel through
    private List<(PlayerToken defender, bool isCausingInvalidity, int? gkPenalty)> interceptors; // Defenders trying to intercept
    public List<PlayerToken> alreadyInterceptedDefs;

    void Update()
    {
        // Check if waiting for dice rolls and the R key is pressed
        if (isWaitingForBlockDiceRoll && !isWaitingForGKDiceRoll && !isWaitingForShotRoll && !isWaitingforHandlingTest && Input.GetKeyDown(KeyCode.R))
        {
            StartCoroutine(StartShotBlockRoll());  // Pass the stored list
        }
        if (!isWaitingForBlockDiceRoll && isWaitingForGKDiceRoll && !isWaitingForShotRoll && !isWaitingforHandlingTest && Input.GetKeyDown(KeyCode.R))
        {
            StartCoroutine(ResolveGKSavingAttempt(interceptors[0]));
        }
        if (!isWaitingForBlockDiceRoll && !isWaitingForGKDiceRoll && isWaitingForShotRoll && !isWaitingforHandlingTest && Input.GetKeyDown(KeyCode.R))
        {
            StartCoroutine(StartShotRoll());
        }
        if (!isWaitingForBlockDiceRoll && !isWaitingForGKDiceRoll && !isWaitingForShotRoll && isWaitingforHandlingTest && Input.GetKeyDown(KeyCode.R))
        {
            StartCoroutine(ResolveHandlingTest());
        }
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
        HandleClicksForSnapMovement(token, hex);
    }

    private void OnKeyReceived(KeyPressData keyData)
    {
        if (keyData.isConsumed) return;
        if (!isActivated) return;
        if (isWaitingForBlockDiceRoll && keyData.key == KeyCode.R)
        {
            keyData.isConsumed = true; // Consume the key event
            StartCoroutine(StartShotBlockRoll());  // Pass the stored list
            return;
        }
        else if (isWaitingForGKDiceRoll && keyData.key == KeyCode.R)
        {
            keyData.isConsumed = true; // Consume the key event
            StartCoroutine(ResolveGKSavingAttempt(interceptors[0]));
            return;
        }
        else if (isWaitingForShotRoll && keyData.key == KeyCode.R)
        {
            keyData.isConsumed = true; // Consume the key event
            StartCoroutine(StartShotRoll());
            return;
        }
        else if (isWaitingforHandlingTest && keyData.key == KeyCode.R)
        {
            keyData.isConsumed = true; // Consume the key event
            StartCoroutine(ResolveHandlingTest());
            return;
        }
        else if (isWaitingforBlockerSelection && keyData.key == KeyCode.X)
        {
            keyData.isConsumed = true; // Consume the key event
            StartDefenderMovementPhase();
            return;
        }
        else if (isWaitingForTargetSelection && keyData.key == KeyCode.X)
        {
            keyData.isConsumed = true; // Consume the key event
            CompleteDefenderMovement();
            return;
        }
        else if (isWaitingForSaveandHoldScenario)
        {
            if (keyData.key == KeyCode.Q)
            {
                keyData.isConsumed = true;
                QuickThrow();
            }
            if (keyData.key == KeyCode.K)
            {
                keyData.isConsumed = true;
                ActivateFinalThirds();
            }
        }

    }
    
    public void StartShotProcess(PlayerToken shootingToken, string shotType)
    {
        hexGrid.ClearHighlightedHexes();
        if (shootingToken == null)
        {
            Debug.LogError("Shooting token is NULL! Cannot proceed with shot.");
            return;
        }

        HexCell shooterHex = shootingToken.GetCurrentHex();
        if (shooterHex == null)
        {
            Debug.LogError($"Shooting token {shootingToken.name} is not on any hex! Cannot proceed with shot.");
            return;
        }
        if (!shooterHex.CanShootFrom)
        {
            Debug.LogError($"Token {shootingToken.name} is on hex {shooterHex.coordinates}, but this hex is not a valid shooting hex!");
            return;
        }

        shooter = shootingToken;
        this.shotType = shotType;
        isActivated = true;

        if (shotType == "snapshot")
        {
            Debug.Log("Snapshot initiated. Allow one defender to move 2 hexes.");
            if (shooterHex.isInPenaltyBox == 0) MatchManager.Instance.gameData.gameLog.LogEvent(shooter, MatchManager.ActionType.ShotAttempt, shotType: "snapO");
            else MatchManager.Instance.gameData.gameLog.LogEvent(shooter, MatchManager.ActionType.ShotAttempt, shotType: "snap");
            StartDefenderMovementPhase();
        }
        else
        {
            Debug.Log("Full Power Shot initiated. Proceeding to target selection.");
            if (shooterHex.isInPenaltyBox == 0)
            {
                Debug.Log("Shooter is OUTSIDE the penalty box. -1 to shot power.");
                MatchManager.Instance.gameData.gameLog.LogEvent(shooter, MatchManager.ActionType.ShotAttempt, shotType: "shot");
                Debug.Log("Goalkeeper can move 1 hex");
                // TODO: Implement Goalkeeper movement for the Box in case of a Shot from outside the box
            }
            else
            {
                MatchManager.Instance.gameData.gameLog.LogEvent(shooter, MatchManager.ActionType.ShotAttempt, shotType: "shotO");
            }
            HandleTargetSelection();
        }
    }

    private async void HandleClicksForSnapMovement(PlayerToken token, HexCell hex)
    {
        if (token != null && isWaitingforBlockerSelection)
        {
            Debug.Log($"PlayerToken {token.name} clicked, for Snapshot");

            // Attacker Phase: Ensure the token is an attacker
            if (!token.isAttacker)
            {
                // Trying to move an Attacker: Accept, Highlight and wait for click on Hex
                if (tokenMoveforDeflection != null && tokenMoveforDeflection != token)
                {
                    Debug.Log($"Switching Defender selection to {token.name}. Clearing previous highlights.");
                    hexGrid.ClearHighlightedHexes();  // Clear the previous highlights
                }

                Debug.Log($"Selecting defender {token.name}. Highlighting reachable hexes.");
                tokenMoveforDeflection = token;  // Set selected token
                movementPhaseManager.HighlightValidMovementHexes(token, 2);  // Highlight reachable hexes within 3 moves
                isWaitingforBlockerSelection = false;
                isWaitingforBlockerMovement = true;
            }
            else {
                Debug.LogWarning("Attacker clicked, while waiting for a defender to select.");
            }
        }

        if (hex != null && isWaitingforBlockerMovement)
        {
            Debug.Log($"Hex clicked: {hex.name}");

            // Ensure the hex is within the highlighted valid movement hexes
            if (
                hexGrid.highlightedHexes.Contains(hex)
                && !hex.isAttackOccupied
                && !hex.isDefenseOccupied
                && !hex.isOutOfBounds
            )
            {
                if (tokenMoveforDeflection != null)
                {
                    Debug.Log($"Moving {tokenMoveforDeflection.name} to hex {hex.coordinates}");

                    // Move the selected token to the valid hex (use the highPassManager's selectedToken)
                    await helperFunctions.StartCoroutineAndWait(movementPhaseManager.MoveTokenToHex(hex, tokenMoveforDeflection, false));  // Pass the selected token
                    CompleteDefenderMovement();
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
            hex != null // we clicked a Hex
            && isWaitingForTargetSelection // We are indeed waiting for a targetSelection
            && hexGrid.highlightedHexes.Contains(hex) // one of the target Hexes
        )
        {
            Debug.Log($"Valid selected Target Hex: {hex.name}");
            HandleTargetClick(hex);                    
        }
        else
        {
            Debug.LogWarning("No valid hex or token clicked.");
        }
    }

    private void StartDefenderMovementPhase()
    {
        isWaitingforBlockerSelection = true;
    }

    public void CompleteDefenderMovement()
    {
        Debug.Log("Defender movement phase complete. Proceeding to target selection.");
        isWaitingforBlockerMovement = false;
        HandleTargetSelection();
    }

    public void HandleTargetSelection()
    {
        Debug.Log("Highlighting CanShootTo hexes for target selection.");
        HexCell shooterHex = shooter.GetCurrentHex();
        Dictionary<HexCell, List<HexCell>> shootingPaths = shooterHex.ShootingPaths;

        // Highlight and raise all CanShootTo hexes
        foreach (var canShootToHex in shootingPaths.Keys)
        {
            canShootToHex.HighlightHex("CanShootFrom", 1);
            hexGrid.highlightedHexes.Add(canShootToHex);
            if (canShootToHex.transform.position.y == 0)
            {
                canShootToHex.transform.position += Vector3.up * 0.03f; // Raise it above the plane
            }
        }

        Debug.Log("Waiting for target selection...");
        isWaitingForTargetSelection = true;
    }

    public void HandleTargetClick(HexCell clickedTargethex)
    {
        targetHex = clickedTargethex;
        Debug.Log($"Target hex {targetHex.coordinates} selected. Preparing trajectory.");
        HexCell shooterHex = shooter.GetCurrentHex();
        Dictionary<HexCell, List<HexCell>> shootingPaths = shooterHex.ShootingPaths;
        foreach (var canShootToHex in shootingPaths.Keys)
        {
            canShootToHex.ResetHighlight();
            if (canShootToHex.transform.position.y >= 0)
            {
              canShootToHex.transform.position -= Vector3.up * 0.03f; // Sink it below the plane
            }
        }
        trajectoryPath = shooterHex.ShootingPaths[targetHex];
        HighlightTrajectoryPath();
        isWaitingForTargetSelection = false;
        StartCoroutine(StartInterceptionPhase());
    }

    private void HighlightTrajectoryPath()
    {
        foreach (HexCell hex in trajectoryPath)
        {
            hex.HighlightHex("ballPath");
            hexGrid.highlightedHexes.Add(hex);
        }
    }

    private IEnumerator StartInterceptionPhase()
    {
        Debug.Log("Starting interception phase.");
        interceptors = GatherInterceptors(trajectoryPath);

        if (interceptors.Count == 0)
        {
            if (shooter.GetCurrentHex().isInPenaltyBox != 0)
            {
                Debug.Log($"No defenders to Deflect! The {shooter.name} may [R]oll! Good Luck! Starting dice roll sequence..");
                isWaitingForShotRoll = true;
                yield break;
            }
            else // Shot is from outside the box
            {
                gkWasOfferedMoveForBox = true;
                goalKeeperManager.isActivated = true;
                yield return StartCoroutine(goalKeeperManager.HandleGKFreeMove());
                interceptors = GatherInterceptors(trajectoryPath);
                if (interceptors.Count == 0)
                {
                    Debug.Log($"No defenders to Deflect! The {shooter.name} may [R]oll! Good Luck!. Starting dice roll sequence..");
                    isWaitingForShotRoll = true;
                    yield break;
                }
            }
        }
        Debug.Log($"Defenders with interception chances: {interceptors.Count}");
        interceptors = interceptors.OrderBy(d =>
            HexGridUtils.GetHexDistance(shooter.GetCurrentHex().coordinates, d.defender.GetCurrentHex().coordinates)).ToList();
        StartCoroutine(OfferBlockRoll());

    }

    private List<(PlayerToken defender, bool isCausingInvalidity, int? gkPenalty)> GatherInterceptors(List<HexCell> path)
    {
        List<(PlayerToken defender, bool isCausingInvalidity, int? gkPenalty)> onPathDefenders = new List<(PlayerToken defender, bool isCausingInvalidity, int? gkPenalty)>();

        PlayerToken invalidityCausingDefender = null;
        
        // **Step 1: Find the defending Goalkeeper**
        PlayerToken defendingGK = hexGrid.GetDefendingGK();
        HexCell gkHex = defendingGK.GetCurrentHex();
        if (!alreadyInterceptedDefs.Contains(defendingGK))
        {
            int gkPenalty = 0;

            // **Step 2: Calculate Saveable Hexes**
            List<HexCell> saveableHexes = hexGrid.GetSavableHexes();

            // **Step 3: Find all SaveHexes in the shot path**
            List<HexCell> validSaveHexes = path
                .Where(hex => saveableHexes.Contains(hex))
                .OrderBy(hex => HexGridUtils.GetHexDistance(gkHex.coordinates, hex.coordinates))
                .ToList();
            // Get the closest one (if any)
            saveHex = validSaveHexes.FirstOrDefault();

            if (saveHex != null && gkHex != null)
            {
                int saveDistance = HexGridUtils.GetHexDistance(gkHex.coordinates, saveHex.coordinates); // turn to Cube
                if (saveDistance == 3) gkPenalty = -1;
                if (saveDistance == 2 && tokenMoveforDeflection == defendingGK) gkPenalty = -1;
                if (saveDistance == 3 && tokenMoveforDeflection == defendingGK) gkPenalty = -2;
                
                Debug.Log($"Goalkeeper {defendingGK.name} can attempt a save at {saveHex.coordinates} with penalty {gkPenalty}");
                onPathDefenders.Add((defendingGK, false, gkPenalty));
            }
        }

        // Step 4.1: Get defenders On the Path
        foreach (HexCell hex in path)
        {
            if (hex.isDefenseOccupied && hex!= gkHex) // Skip the GK Hex
            {
                PlayerToken defenderOnPath = hex.GetOccupyingToken();
                if (!alreadyInterceptedDefs.Contains(defenderOnPath))
                {
                    // After movement: Defender on the path causes the pass to become dangerous
                    onPathDefenders.Add((defenderOnPath, true, null));  // Add defender as blocking path
                    invalidityCausingDefender = defenderOnPath;  // Keep track for later rolls
                    Debug.Log($"Path blocked by defender at hex: {hex.coordinates}. Defender: {defenderOnPath.name}");
                }
            }
        }
        
        // Step 4.2: Get defenders and their ZOI (neighbors)
        List<HexCell> defenderHexes = hexGrid.GetDefenderHexes();
        defenderHexes.Remove(gkHex); // Exclude the GK Hex
        List<HexCell> defenderNeighbors = hexGrid.GetDefenderNeighbors(defenderHexes);
        // Add null check
        if (defenderNeighbors == null)
        {
            Debug.LogError("defenderNeighbors is null! Make sure GetDefenderNeighbors() is returning a valid list.");
            defenderNeighbors = new List<HexCell>(); // Initialize an empty list to avoid crashes
        }
        foreach (HexCell hex in path)
        {
            foreach (HexCell neighbor in hex.GetNeighbors(hexGrid))
            {
                if (neighbor == null)
                {
                    // Debug.LogWarning($"A neighbor of {hex.coordinates} is null.");
                    continue;  // Skip this iteration if the neighbor neighbor of the path is out of bounds
                }
                if (defenderNeighbors.Contains(hex) && !neighbor.isAttackOccupied)  // Ignore attack-occupied hexes
                {
                    // Check if a defender is already processed as causing invalidity
                    // Check if a defender is already processed as causing invalidity
                    PlayerToken defenderInZOI = neighbor.GetOccupyingToken();
                    if (!alreadyInterceptedDefs.Contains(defenderInZOI))
                    {
                        if (defenderInZOI != null) // Avoid adding the same defender twice)
                        {
                            bool isCausingInvalidity = defenderInZOI == invalidityCausingDefender;
                            if (!onPathDefenders.Exists(d => d.defender == defenderInZOI))
                            {
                                onPathDefenders.Add((defenderInZOI, isCausingInvalidity, null));  // Add as a potential interceptor
                                Debug.Log($"Defender {defenderInZOI.name} can intercept through ZOI at hex: {hex.coordinates}");
                            }
                            else
                            {
                                // Debug.Log($"Skipping already processed defender: {defenderInZOI.name}");
                            }
                        }
                    }
                }
            }
        }
        return onPathDefenders;
    }

    private IEnumerator OfferBlockRoll()
    {
        currentDefenderBlockingHex = interceptors[0].defender.GetCurrentHex();
        // 🛑 If shot is from OUTSIDE the box & first in-box defender is blocking, allow GK to move
        if (
            shooter.GetCurrentHex().isInPenaltyBox == 0 // Shot is from outside the box
            && currentDefenderBlockingHex.isInPenaltyBox != 0 // defender is in the box
            && !gkWasOfferedMoveForBox // GK as not already moved for the box.
        )
        {
            // this is the first defender in the box while shooting from outside the box
            gkWasOfferedMoveForBox = true;
            yield return StartCoroutine(goalKeeperManager.HandleGKFreeMove());
            interceptors = GatherInterceptors(trajectoryPath);
            StartCoroutine(OfferBlockRoll());
            yield break;
        }
        
        /// 🏆 Calculate roll needed
        int gkPenalty = interceptors[0].gkPenalty ?? 0;  // Default to 0 if null
        int rollNeeded = 10 - (interceptors[0].defender.tackling + gkPenalty);
        int howIsDefBlocking = interceptors[0].isCausingInvalidity ? 5 : 6;
        int finalRollNeeded = Math.Min(rollNeeded, howIsDefBlocking);

        // Log based on whether the interceptor is a GK or a defender
        // 🎲 If GK is next, trigger shot roll instead
        if (interceptors[0].gkPenalty != null)
        {
            Debug.Log("The GK is next up. Shooter must Roll to shoot. Setting isWaitingForShotRoll to true.");
            isWaitingForShotRoll = true;
        }
        else
        {
            Debug.Log($"{interceptors[0].defender.name} attempts to block the shot, needs a {finalRollNeeded}+ to deflect. [R]oll!");
            isWaitingForBlockDiceRoll = true;
        }
    }

    private IEnumerator StartShotBlockRoll(int? rigRoll = null)
    {
        yield return null; // Wait for next frame
        if (currentDefenderBlockingHex != null)
        {
            // Find the current defender's entry in the list of defenders
            var currentDefenderEntry = interceptors.Find(d => d.defender.GetCurrentHex() == currentDefenderBlockingHex);
            if (currentDefenderEntry.defender != null)
            {
                // Retrieve defender attributes
                PlayerToken defenderToken = currentDefenderEntry.defender;
                int tackling = defenderToken.tackling;
                string defenderName = defenderToken.name;

                if (currentDefenderEntry.gkPenalty != null) // This is the GK
                {
                    Debug.Log($"GK {defenderName} is attempting a save at {currentDefenderBlockingHex.coordinates}.");
                    Debug.Log("Shooter must roll first! Press [R] to roll for the shot.");
                    isWaitingForBlockDiceRoll = false;
                    isWaitingForShotRoll = true;  // Wait for the shooter to roll
                    yield break;  // Exit the coroutine here to wait for the shot roll
                }

                // Roll the dice
                var (returnedRoll, returnedJackpot) = helperFunctions.DiceRoll();
                int diceRoll = rigRoll ?? returnedRoll;
                
                Debug.Log($"Dice roll by {defenderName} at {currentDefenderBlockingHex.coordinates}: {diceRoll}");
                MatchManager.Instance.gameData.gameLog.LogEvent(defenderToken, MatchManager.ActionType.InterceptionAttempt);
                isWaitingForBlockDiceRoll = false;
                // Calculate interception conditions
                bool isCausingInvalidity = currentDefenderEntry.isCausingInvalidity;
                int requiredRoll = isCausingInvalidity ? 5 : 6; // Base roll requirement
                bool successfulInterception = diceRoll >= requiredRoll || diceRoll + tackling >= 10;
                if (successfulInterception)
                {
                    hexGrid.ClearHighlightedHexes();
                    Debug.Log($"Shot blocked by {defenderName}! Loose Ball from {currentDefenderBlockingHex.coordinates}!");
                    MatchManager.Instance.gameData.gameLog.LogEvent(
                        MatchManager.Instance.LastTokenToTouchTheBallOnPurpose
                        , MatchManager.ActionType.ShotBlocked
                        , connectedToken: defenderToken
                    );
                    MatchManager.Instance.hangingPassType = "shot"; // TODO: WHY?
                    StartCoroutine(looseBallManager.ResolveLooseBall(defenderToken, "ground"));
                    ResetShotProcess();
                }
                else
                {
                    Debug.Log($"{defenderName} at {currentDefenderBlockingHex.coordinates} failed to block.");
                    // Remove this defender and move to the next
                    interceptors.Remove(currentDefenderEntry);
                    alreadyInterceptedDefs.Add(currentDefenderEntry.defender);

                    if (interceptors.Count > 0)
                    {
                        // There are more defenders (maybe the GK too) that can block
                        StartCoroutine(OfferBlockRoll());
                    }
                    else
                    {
                        // If the Shooter has already Shot
                        if (totalShotPower > 0)
                        {
                            // If the Shooter had shot OFF TARGET
                            if (shooterRoll == 1)
                            {
                                Debug.Log($"{shooter.name} rolled a {shooterRoll}, this means the Shot is OFF target! GoalKick awarded.");
                                MatchManager.Instance.gameData.gameLog.LogEvent(
                                    MatchManager.Instance.LastTokenToTouchTheBallOnPurpose
                                    , MatchManager.ActionType.ShotOffTarget
                                );
                                yield return StartCoroutine(ShootOffTargetRandomizer());
                                ResetShotProcess();
                                if (movementPhaseManager.isActivated)
                                {
                                    movementPhaseManager.EndMovementPhase(false);
                                    movementPhaseManager.stunnedTokens.Clear();
                                }
                                // TODO: Implement GoalKick
                            }
                            else
                            {
                                Debug.Log($"{shooter.name} Shot roll: {shooterRoll}, that's a GOAL!!");
                                MatchManager.Instance.gameData.gameLog.LogEvent(
                                    MatchManager.Instance.LastTokenToTouchTheBallOnPurpose
                                    , MatchManager.ActionType.GoalScored
                                );
                                MatchManager.Instance.gameData.gameLog.LogEvent(
                                    MatchManager.Instance.PreviousTokenToTouchTheBallOnPurpose
                                    , MatchManager.ActionType.AssistProvided
                                );
                                yield return StartCoroutine(groundBallManager.HandleGroundBallMovement(targetHex, shooterRoll));
                                if (movementPhaseManager.isActivated)
                                {
                                    movementPhaseManager.EndMovementPhase(false);
                                    movementPhaseManager.stunnedTokens.Clear();
                                }
                                goalFlowManager.StartGoalFlow(shooter);
                                ResetShotProcess();
                            }
                        }
                        else 
                        {
                            // No more defenders, shooter can shoot!
                            Debug.Log($"No more defenders to Deflect! The {shooter.name} may [R]oll! Good Luck!.");
                            yield return null;
                            isWaitingForShotRoll = true;
                        }
                    }
                }
            }
        }
        yield return null;
    }

    public IEnumerator StartShotRoll(int? rigRoll = null)
    {
        Debug.Log("Hello from the StartShotRoll");
        
        var (returnedRoll, returnedJackpot) = helperFunctions.DiceRoll();
        shooterRoll = rigRoll ?? returnedRoll;
        // shooterRoll = 2;
        isWaitingForShotRoll = false;
        totalShotPower = returnedJackpot ? 50 : shooterRoll + shooter.shooting;
        boxPenalty = shooter.GetCurrentHex().isInPenaltyBox == 0 ? ", -1 outside the Penalty Box" : "";
        // TODO: dificult shooting position penalty
        snapPenalty = shotType == "snapshot" ? ", -1 for taking a Snapshot" : "";
        if (shotType == "snapshot") totalShotPower -= 1; 
        if (shooter.GetCurrentHex().isInPenaltyBox == 0) totalShotPower -= 1;
        // totalShotPower = 6;
        if (interceptors.Count > 0 && interceptors[0].gkPenalty != null) // Check if the GK is next
        {
            Debug.Log($"Goalkeeper {interceptors[0].defender.name} now attempts a save. Press [R] to roll");
            isWaitingForGKDiceRoll = true;
        }
        else // There's NO GOALKEEPER or more defenders! Shooter is attempting to put it on target. 
        {
            if (shooterRoll == 1)
            {
                Debug.Log($"{shooter.name} rolls 1! Shot is off target. GoalKick awarded.");
                MatchManager.Instance.gameData.gameLog.LogEvent(
                    MatchManager.Instance.LastTokenToTouchTheBallOnPurpose
                    , MatchManager.ActionType.ShotOffTarget
                );
                yield return StartCoroutine(ShootOffTargetRandomizer());
                // TODO: Implement GoalKick
            }
            else
            {
                Debug.Log($"{shooter.name} Shot roll: {shooterRoll} + Shooting: {shooter.shooting}{snapPenalty}{boxPenalty}= {totalShotPower}");
                Debug.Log($"Get IN!! {shooter.name}, buries it to the top corner! Goal!!!");
                MatchManager.Instance.gameData.gameLog.LogEvent(
                        MatchManager.Instance.LastTokenToTouchTheBallOnPurpose
                        , MatchManager.ActionType.GoalScored
                    );
                if (MatchManager.Instance.PreviousTokenToTouchTheBallOnPurpose != null)
                {
                    MatchManager.Instance.gameData.gameLog.LogEvent(
                        MatchManager.Instance.PreviousTokenToTouchTheBallOnPurpose
                        , MatchManager.ActionType.AssistProvided
                    );
                }
                if (movementPhaseManager.isActivated)
                {
                    movementPhaseManager.EndMovementPhase(false);
                    movementPhaseManager.stunnedTokens.Clear();
                }
                yield return StartCoroutine(groundBallManager.HandleGroundBallMovement(targetHex, shooterRoll));
                goalFlowManager.StartGoalFlow(shooter);
                ResetShotProcess();
            }
        } 
    }

    private IEnumerator ResolveGKSavingAttempt((PlayerToken defender, bool isCausingInvalidity, int? gkPenalty) gkEntry, int? rigRoll = null)
    {
        isWaitingForGKDiceRoll = false;
        yield return null;
        PlayerToken gkToken = gkEntry.defender;
        var (returnedRoll, returnedJackpot) = helperFunctions.DiceRoll();
        int gkRoll = rigRoll ?? returnedRoll;
        // int gkRoll = 6;
        int gkPenalty = gkEntry.gkPenalty ?? 0;
        int totalSavingPower = returnedJackpot ? 50 : gkRoll + gkToken.saving + gkPenalty;
        // int totalSavingPower = gkRoll + gkToken.saving + gkPenalty;
        // int totalSavingPower = 6;
        // if (returnedJackpot)
        // {
        //     Debug.Log($"GK {gkToken.name} rolls A JACKPOT!!!");
        // }
        // else
        Debug.Log($"GK {gkToken.name} rolls {gkRoll} + Saving: {gkToken.saving} + Penalty: {gkPenalty} = {totalSavingPower}");
        MatchManager.Instance.gameData.gameLog.LogEvent(
                gkToken
                , MatchManager.ActionType.SaveAttempt
            );

        hexGrid.ClearHighlightedHexes();
        alreadyInterceptedDefs.Add(gkToken);

        if (totalSavingPower == totalShotPower)
        {
            yield return null;
            Debug.Log($"{gkToken.name} ties the attacker's roll!! Loose Ball situation initiated.");
            MatchManager.Instance.gameData.gameLog.LogEvent(
                MatchManager.Instance.LastTokenToTouchTheBallOnPurpose
                , MatchManager.ActionType.ShotOnTarget
            );
            MatchManager.Instance.gameData.gameLog.LogEvent(
                gkToken
                , MatchManager.ActionType.SaveMade
                , saveType: "loose"
            );
            MatchManager.Instance.hangingPassType = "shot";
            yield return StartCoroutine(groundBallManager.HandleGroundBallMovement(saveHex, shooterRoll));
            yield return StartCoroutine(movementPhaseManager.MoveTokenToHex(saveHex, gkToken, false)); // Maybe this is redundant
            StartCoroutine(looseBallManager.ResolveLooseBall(gkToken, "ground"));
            ResetShotProcess();
        }
        else if (totalSavingPower > totalShotPower)
        {
            // TODO: Push everyone in line.
            MatchManager.Instance.gameData.gameLog.LogEvent(
                MatchManager.Instance.LastTokenToTouchTheBallOnPurpose
                , MatchManager.ActionType.ShotOnTarget
            );
            yield return StartCoroutine(groundBallManager.HandleGroundBallMovement(saveHex, shooterRoll));
            yield return StartCoroutine(movementPhaseManager.MoveTokenToHex(saveHex, gkToken, false));
            if (movementPhaseManager.isActivated)
            {
                movementPhaseManager.EndMovementPhase(false);
                // movementPhaseManager.stunnedTokens.Clear();
            }
            MatchManager.Instance.ChangePossession();
            yield return null;
            Debug.Log($"{gkToken.name} saves the shot! Will they hold the ball? {gkToken} needs to roll lower than {gkToken.handling} to hold the ball. Press [R] to roll for Handling Test!");
            isWaitingforHandlingTest = true;
            yield break;
        }
        else if (totalSavingPower < totalShotPower)
        {
            interceptors.Remove(gkEntry);
            if (interceptors.Count > 0)
            {
                // There are more defenders to block, Run through them
                StartCoroutine(OfferBlockRoll());
            }
            else
            {
                if (shooterRoll == 1)
                {
                    Debug.Log($"{shooter.name} rolls 1! Shot is off target. GoalKick awarded.");
                    if (movementPhaseManager.isActivated)
                    {
                        movementPhaseManager.EndMovementPhase(false);
                        movementPhaseManager.stunnedTokens.Clear();
                    }
                    MatchManager.Instance.gameData.gameLog.LogEvent(
                        MatchManager.Instance.LastTokenToTouchTheBallOnPurpose
                        , MatchManager.ActionType.ShotOffTarget
                    );
                    // TODO: Implement GoalKick
                }
                else
                {
                    Debug.Log($"{shooter.name} Shot roll: {shooterRoll} + Shooting: {shooter.shooting}{snapPenalty}{boxPenalty} = {totalShotPower}");
                    Debug.Log($"Get IN!! {shooter.name}, buries it to the top corner! Goal!!!");
                    MatchManager.Instance.gameData.gameLog.LogEvent(
                        MatchManager.Instance.LastTokenToTouchTheBallOnPurpose
                        , MatchManager.ActionType.GoalScored
                    );
                    MatchManager.Instance.gameData.gameLog.LogEvent(
                        MatchManager.Instance.PreviousTokenToTouchTheBallOnPurpose
                        , MatchManager.ActionType.AssistProvided
                    );
                    yield return StartCoroutine(groundBallManager.HandleGroundBallMovement(targetHex, shooterRoll));
                    if (movementPhaseManager.isActivated)
                    {
                        movementPhaseManager.EndMovementPhase(false);
                        movementPhaseManager.stunnedTokens.Clear();
                    }
                    goalFlowManager.StartGoalFlow(shooter);
                    ResetShotProcess();
                }
            }
        }
        yield return null;
    }

    private IEnumerator ResolveHandlingTest()
    {
      yield return null;
      PlayerToken gkToken = interceptors[0].defender;
      var (returnedRoll, returnedJackpot) = helperFunctions.DiceRoll();
      // int gkRoll = returnedRoll;
      int gkRoll = 1;
      isWaitingforHandlingTest = false;
      // Handling Test
      if (gkRoll < gkToken.handling)
      {
          MatchManager.Instance.gameData.gameLog.LogEvent(
              gkToken
              , MatchManager.ActionType.SaveMade
              , saveType: "held"
          );
          MatchManager.Instance.SetLastToken(gkToken);
          Debug.Log($"{gkToken.name} rolled {gkRoll} and holds the ball! Press [Q]uickThrow, or [K] to activate Final Thirds");
          if (movementPhaseManager.isActivated)
          {
              movementPhaseManager.EndMovementPhase(false);
              // movementPhaseManager.stunnedTokens.Clear();
          }
          isWaitingForSaveandHoldScenario = true;
          while (isWaitingForSaveandHoldScenario)
          {
              yield return null;  // Wait for the next frame
          }
      }
      else 
      {
          Debug.Log($"{gkToken.name} rolled {gkRoll} and can't hold it!");
          StartCoroutine(looseBallManager.ResolveLooseBall(gkToken, "handling"));
      }
      ResetShotProcess();
    }

    private void QuickThrow()
    {
        isWaitingForSaveandHoldScenario = false;
        Debug.Log("QuickThrow Scenario chosen, NOBODY MOVES! Click Hex to select target for GK's throw");
        MatchManager.Instance.BroadcastQuickThrow();
    }
    private void ActivateFinalThirds()
    {
        isWaitingForSaveandHoldScenario = false;  // Cancel the decision phase
        Debug.Log("GK Decided to activate F3 Moves");
        MatchManager.Instance.BroadcastActivateFinalThirdsAfterSave();
        finalThirdManager.TriggerFinalThirdPhase(true);
    }

    private void ResetShotProcess()
    {
        isActivated = false;
        isWaitingforBlockerSelection = false;
        isWaitingForBlockDiceRoll = false;
        isWaitingForShotRoll = false;
        isWaitingforBlockerMovement = false;
        isWaitingForTargetSelection = false;
        shooter = null;
        targetHex = null;
        trajectoryPath = null;
        interceptors.Clear();
        alreadyInterceptedDefs.Clear();
        totalShotPower = 0;
        shooterRoll = 0;
        snapPenalty = "";
        tokenMoveforDeflection = null;
        saveHex = null;
        currentDefenderBlockingHex = null;
        shotType = null;
    }

    private IEnumerator ShootOffTargetRandomizer()
    {
        var (returnedRoll, returnedJackpot) = helperFunctions.DiceRoll();
        int diceRoll = returnedRoll;
        // int diceRoll = 6;
        switch (diceRoll)
        {
            case 1:
            case 2:
            case 3:
                yield return StartCoroutine(FailedLob());
                break;
            case 4:
            case 5:
            case 6:
                Debug.Log("Camera");
                yield return StartCoroutine(oGamosTouKaragkiozi());
                break;
            default:
                yield return StartCoroutine(NextToBar());
                Debug.Log("Value is something else");
                break;
        }
    }

    private IEnumerator FailedLob()
    {
        HexCell shooterHex = shooter.GetCurrentHex();
        int targetX = 22 * (shooterHex.coordinates.x > 0 ? 1 : -1);
        float slope = (float)(targetHex.coordinates.z - shooterHex.coordinates.z) /
                  (targetHex.coordinates.x - shooterHex.coordinates.x);
        int intercept = targetHex.coordinates.z - Mathf.RoundToInt(slope * targetHex.coordinates.x);
        int intersectionZ = Mathf.RoundToInt(slope * targetX + intercept);
        yield return StartCoroutine(longBallManager.HandleLongBallMovement(hexGrid.GetHexCellAt(new Vector3Int(targetX, 0, intersectionZ)), true));
    }
    
    private IEnumerator NextToBar()
    {
        // TODO: Shoot right next to the bar
        HexCell shooterHex = shooter.GetCurrentHex();
        int targetX = 20 * (shooterHex.coordinates.x > 0 ? 1 : -1);
        int shooterz = shooterHex.coordinates.z;
        float slope = (float)(targetHex.coordinates.z - shooterHex.coordinates.z) /
                  (targetHex.coordinates.x - shooterHex.coordinates.x);
        int intercept = targetHex.coordinates.z - Mathf.RoundToInt(slope * targetHex.coordinates.x);
        int intersectionZ = Mathf.RoundToInt(slope * targetX + intercept);
        yield return StartCoroutine(longBallManager.HandleLongBallMovement(hexGrid.GetHexCellAt(new Vector3Int(targetX, 0, intersectionZ)), true));      
    }

    private IEnumerator oGamosTouKaragkiozi()
    {
        hexGrid.ClearHighlightedHexes();
        HexCell shooterHex = shooter.GetCurrentHex();
        // Step 1: Get Camera Position & Forward Direction
        Transform camTransform = Camera.main.transform;
        Vector3 cameraPosition = camTransform.position;
        Vector3 cameraForward = camTransform.forward.normalized;

        // Step 2: Define Close-Up Target Position (In Front of Camera)
        float closeUpDistance = 1f; // Distance in front of the camera where the ball will stop
        Vector3 closeUpPosition = cameraPosition + (cameraForward * closeUpDistance);

        // Step 3: Move the Ball Towards the Camera (Fast)
        float moveDuration = 0.4f; // Ball flies toward the camera in 0.4 seconds
        float elapsedTime = 0f;
        Vector3 startPos = shooterHex.GetHexCenter();

        while (elapsedTime < moveDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / moveDuration;
            ball.transform.position = Vector3.Lerp(startPos, closeUpPosition, progress);
            yield return null; // Wait for the next frame
        }

        // Step 4: Hold Ball Near Camera for Dramatic Pause
        yield return new WaitForSeconds(2f);

        // Step 5: Move Ball to Final Hex Based on Shooter's X
        int finalX = shooterHex.coordinates.x > 0 ? 22 : -22;
        HexCell finalHex = hexGrid.GetHexCellAt(new Vector3Int(finalX, 0, 0));

        if (finalHex != null)
        {
            ball.transform.position = finalHex.GetHexCenter();
            ball.PlaceAtCell(finalHex);
        }
        else
        {
            Debug.LogWarning($"Final hex at ({finalX}, 0, 0) is null!");
        }
    }

    public string GetDebugStatus()
    {
        StringBuilder sb = new();
        sb.Append("Shot: ");

        if (isActivated) sb.Append("isActivated, ");
        if (isAvailable) sb.Append("isAvailable, ");
        if (isWaitingforBlockerSelection) sb.Append("isWaitingforBlockerSelection, ");
        if (isWaitingforBlockerMovement) sb.Append("isWaitingforBlockerMovement, ");
        if (isWaitingForTargetSelection) sb.Append("isWaitingForTargetSelection, ");
        if (isWaitingForBlockDiceRoll) sb.Append("isWaitingForBlockDiceRoll, ");
        if (isWaitingForShotRoll) sb.Append("isWaitingForShotRoll, ");
        if (isWaitingForGKDiceRoll) sb.Append("isWaitingForGKDiceRoll, ");
        if (isWaitingforHandlingTest) sb.Append("isWaitingforHandlingTest, ");
        if (isWaitingForSaveandHoldScenario) sb.Append("isWaitingForSaveandHoldScenario, ");
        if (gkWasOfferedMoveForBox) sb.Append("gkWasOfferedMoveForBox, ");
        if (shotType != "") sb.Append($"shotType: {shotType}, ");
        if (shooter != null) sb.Append($"shooter: {shooter.name}, ");
        if (targetHex != null) sb.Append($"targetHex: {targetHex.name}, ");
        if (saveHex != null) sb.Append($"saveHex: {saveHex.name}, ");

        if (sb.Length >= 2 && sb[^2] == ',') sb.Length -= 2; // Trim trailing comma
        return sb.ToString();
    }

    public string GetInstructions()
    {
        StringBuilder sb = new();
        if (finalThirdManager.isActivated) return "";
        if (goalKeeperManager.isActivated) return "";
        if (isAvailable) sb.Append("Press [S] to Shoot");
        if (isActivated) sb.Append("Shot: ");
        if (isWaitingforBlockerSelection) sb.Append($"Click on a defender to move 2 Hexes in an attempt to block the Snapshot, ");
        if (isWaitingforBlockerMovement) sb.Append($"Click on a Highlighted Hex to move the blocker there, ");
        if (isWaitingForTargetSelection) sb.Append($"Click on a Hex in the Goal to target the Shot there, ");
        if (isWaitingForBlockDiceRoll) sb.Append($"Press [R] to roll for the block of the shot, ");
        if (isWaitingForShotRoll) sb.Append($"Press [R] to roll with {shooter} for the shot, ");
        if (isWaitingforHandlingTest) sb.Append($"Press [R] to roll for the Handling Test, ");
        if (isWaitingForSaveandHoldScenario) sb.Append($"Press [Q]uick Throw or [K] to Activate Final thirds, ");

        if (sb.Length >= 2 && sb[^2] == ',') sb.Length -= 2; // Safely trim trailing comma + space
        return sb.ToString();
    }
}
