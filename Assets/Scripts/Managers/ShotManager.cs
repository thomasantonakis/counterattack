using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine.Analytics;
using System;

public class ShotManager : MonoBehaviour
{
    [Header("Dependencies")]
    public MovementPhaseManager movementPhaseManager;
    public GameInputManager gameInputManager;
    public LooseBallManager looseBallManager;
    public HexGrid hexGrid;
    [Header("Flags")]
    public bool isShotInProgress = false;  // Tracks if a shot is active
    public bool isWaitingforBlockerSelection = false;  // Tracks if we are waiting to select a blocker
    public bool isWaitingforBlockerMovement = false;  // Tracks if we are waiting for the selected blocker to move
    public bool isWaitingForTargetSelection = false;  // Tracks if we are waiting for shot target selection
    public bool isWaitingForBlockDiceRoll = false;  // Tracks we are in the Blocking Phase
    public bool isWaitingForShotRoll = false;  // Tracks we are in the Blocking Phase
    public bool isWaitingForGKDiceRoll = false;
    public bool isWaitingforHandlingTest = false;
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

    void Update()
    {
        // Check if waiting for dice rolls and the R key is pressed
        if (isWaitingForBlockDiceRoll && !isWaitingForGKDiceRoll && !isWaitingForShotRoll && !isWaitingforHandlingTest && Input.GetKeyDown(KeyCode.R))
        {
            StartCoroutine(StartShotBlockRoll());  // Pass the stored list
        }
        if (!isWaitingForBlockDiceRoll && isWaitingForGKDiceRoll && !isWaitingForShotRoll && !isWaitingforHandlingTest && Input.GetKeyDown(KeyCode.R))
        {
            StartCoroutine(ResolveGKSavingAttempt(interceptors[0]));  // Pass the stored list
        }
        if (!isWaitingForBlockDiceRoll && !isWaitingForGKDiceRoll && isWaitingForShotRoll && !isWaitingforHandlingTest && Input.GetKeyDown(KeyCode.R))
        {
            StartShotRoll();  // Pass the stored list
        }
        if (!isWaitingForBlockDiceRoll && !isWaitingForGKDiceRoll && !isWaitingForShotRoll && isWaitingforHandlingTest && Input.GetKeyDown(KeyCode.R))
        {
            StartCoroutine(ResolveHandlingTest());
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
        isShotInProgress = true;

        if (shotType == "snapshot")
        {
            Debug.Log("Snapshot initiated. Allow one defender to move 2 hexes.");
            StartDefenderMovementPhase();
        }
        else
        {
            Debug.Log("Full Power Shot initiated. Proceeding to target selection.");
            if (shooter.GetCurrentHex().isInPenaltyBox == 0)
            {
                Debug.Log("Shooter is OUTSIDE the penalty box. -1 to shot power.");
                Debug.Log("Goalkeeper can move 1 hex");
                // TODO: Implement Goalkeeper movement
            }
            HandleTargetSelection();
        }
    }

    private void StartDefenderMovementPhase()
    {
        MatchManager.Instance.currentState = MatchManager.GameState.SnapshotPhase;
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
            canShootToHex.transform.position += Vector3.up * 0.5f; // Raise it above the plane
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
            canShootToHex.transform.position -= Vector3.up * 0.5f; // Raise it above the plane
        }
        trajectoryPath = shooterHex.ShootingPaths[targetHex];
        HighlightTrajectoryPath();
        isWaitingForTargetSelection = false;
        StartInterceptionPhase();
    }

    private void HighlightTrajectoryPath()
    {
        foreach (HexCell hex in trajectoryPath)
        {
            hex.HighlightHex("ballPath");
            hexGrid.highlightedHexes.Add(hex);
        }
    }

    private void StartInterceptionPhase()
    {
        Debug.Log("Starting interception phase.");
        interceptors = GatherInterceptors(trajectoryPath);

        if (interceptors.Count == 0)
        {
            Debug.Log($"No defenders to Deflect! The {shooter.name} may [R]oll! Good Luck!. Starting dice roll sequence..");
            isWaitingForShotRoll = true;
            return;
        }

        Debug.Log($"Defenders with interception chances: {interceptors.Count}");
        interceptors = interceptors.OrderBy(d =>
            HexGridUtils.GetHexDistance(shooter.GetCurrentHex().coordinates, d.defender.GetCurrentHex().coordinates)).ToList();
        OfferBlockRoll();
    }

    private List<(PlayerToken defender, bool isCausingInvalidity, int? gkPenalty)> GatherInterceptors(List<HexCell> path)
    {
        List<(PlayerToken defender, bool isCausingInvalidity, int? gkPenalty)> onPathDefenders = new List<(PlayerToken defender, bool isCausingInvalidity, int? gkPenalty)>();

        PlayerToken invalidityCausingDefender = null;
        
        // **Step 1: Find the defending Goalkeeper**
        PlayerToken defendingGK = hexGrid.GetDefendingGK();
        HexCell gkHex = defendingGK.GetCurrentHex();
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

        // Step 4.1: Get defenders On the Path
        foreach (HexCell hex in path)
        {
            if (hex.isDefenseOccupied && hex!= gkHex) // Skip the GK Hex
            {
                PlayerToken defenderOnPath = hex.GetOccupyingToken();
                // After movement: Defender on the path causes the pass to become dangerous
                onPathDefenders.Add((defenderOnPath, true, null));  // Add defender as blocking path
                invalidityCausingDefender = defenderOnPath;  // Keep track for later rolls
                Debug.Log($"Path blocked by defender at hex: {hex.coordinates}. Defender: {defenderOnPath.name}");
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
        return onPathDefenders;
    }

    private void OfferBlockRoll()
    {
        // Calculate the roll needed, ensuring GK penalty is handled safely
        int gkPenalty = interceptors[0].gkPenalty ?? 0;  // Default to 0 if null
        int rollNeeded = 10 - (interceptors[0].defender.tackling + gkPenalty);
        int howIsDefBlocking = interceptors[0].isCausingInvalidity ? 5 : 6;
        int finalRollNeeded = Math.Min(rollNeeded, howIsDefBlocking);

        currentDefenderBlockingHex = interceptors[0].defender.GetCurrentHex();
        // Log based on whether the interceptor is a GK or a defender
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

    private IEnumerator StartShotBlockRoll()
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
                int diceRoll = UnityEngine.Random.Range(1, 7);
                if (defenderName == "11. Poulsen")
                {
                    diceRoll = 6;
                }
                else
                {
                    diceRoll = 2;
                }
                
                Debug.Log($"Dice roll by {defenderName} at {currentDefenderBlockingHex.coordinates}: {diceRoll}");
                isWaitingForBlockDiceRoll = false;
                // Calculate interception conditions
                bool isCausingInvalidity = currentDefenderEntry.isCausingInvalidity;
                int requiredRoll = isCausingInvalidity ? 5 : 6; // Base roll requirement
                bool successfulInterception = diceRoll >= requiredRoll || diceRoll + tackling >= 10;
                if (successfulInterception)
                {
                    hexGrid.ClearHighlightedHexes();
                    Debug.Log($"Shot blocked by {defenderName}! Loose Ball from {currentDefenderBlockingHex.coordinates}!");
                    StartCoroutine(looseBallManager.ResolveLooseBall(defenderToken, "ground"));
                    ResetShotProcess();
                }
                else
                {
                    Debug.Log($"{defenderName} at {currentDefenderBlockingHex.coordinates} failed to block.");
                    // Remove this defender and move to the next
                    interceptors.Remove(currentDefenderEntry);

                    if (interceptors.Count > 0)
                    {
                        // There are more defenders (maybe the GK too) that can block
                        OfferBlockRoll();
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
                                // TODO: Implement GoalKick
                                ResetShotProcess();
                            }
                            else
                            {
                                Debug.Log($"{shooter.name} Shot roll: {shooterRoll}, that's a GOAL!!");
                                // MatchManager.Instance.ScoreGoal(shooter);
                                // TODO: Implement Goal scoring
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

    private void StartShotRoll()
    {
        Debug.Log("Hello from the StartShotRoll");
        // shooterRoll = UnityEngine.Random.Range(1, 7);
        shooterRoll = 4;
        isWaitingForShotRoll = false;
        totalShotPower = shooterRoll + shooter.shooting;
        boxPenalty = shooter.GetCurrentHex().isInPenaltyBox == 0 ? ", -1 outside the Penalty Box" : "";
        snapPenalty = shotType == "snapshot" ? ", -1 for taking a Snapshot" : "";
        if (shotType == "snapshot") totalShotPower -= 1; 
        if (shooter.GetCurrentHex().isInPenaltyBox == 0) totalShotPower -= 1;

        if (interceptors.Count > 0 && interceptors[0].gkPenalty != null) // Check if the GK is next
        {
            Debug.Log($"Goalkeeper {interceptors[0].defender.name} now attempts a save.");
            isWaitingForGKDiceRoll = true;
        }
        else // There are GOALKEEPER or more defenders! Shooter is attempting to put it on target. 
        {
            if (shooterRoll == 1)
            {
                Debug.Log($"{shooter.name} rolls 1! Shot is off target. GoalKick awarded.");
                // TODO: Implement GoalKick
            }
            else
            {
                Debug.Log($"{shooter.name} Shot roll: {shooterRoll} + Shooting: {shooter.shooting}{snapPenalty}{boxPenalty}= {totalShotPower}");
                Debug.Log($"Get IN!! {shooter.name}, buries it to the top corner! Goal!!!");
                // MatchManager.Instance.ScoreGoal(shooter);
                // TODO: Implement Goal scoring
                ResetShotProcess();
            }
        } 
    }

    private IEnumerator ResolveGKSavingAttempt((PlayerToken defender, bool isCausingInvalidity, int? gkPenalty) gkEntry)
    {
        isWaitingForGKDiceRoll = false;
        yield return null;
        PlayerToken gkToken = gkEntry.defender;
        // int gkRoll = UnityEngine.Random.Range(1, 7);
        int gkRoll = 6;
        int gkPenalty = gkEntry.gkPenalty ?? 0;
        int totalSavingPower = gkRoll + gkToken.saving + gkPenalty;
        // int totalSavingPower = 1;

        Debug.Log($"GK {gkToken.name} rolls {gkRoll} + Saving: {gkToken.saving} + Penalty: {gkPenalty} = {totalSavingPower}");

        if (totalSavingPower == totalShotPower)
        {
            yield return null;
            // hexGrid.ClearHighlightedHexes();
            Debug.Log($"{gkToken.name} ties the attacker's roll!! Loose Ball situation initiated.");
            yield return StartCoroutine(movementPhaseManager.MoveTokenToHex(saveHex, gkToken, false));  // Pass the selected token
            StartCoroutine(looseBallManager.ResolveLooseBall(gkToken, "ground"));
            ResetShotProcess();
        }
        else if (totalSavingPower > totalShotPower)
        {
            // TODO: Push everyone in line.
            yield return StartCoroutine(movementPhaseManager.MoveTokenToHex(saveHex, gkToken, false));
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
                OfferBlockRoll();
            }
            else
            {
                if (shooterRoll == 1)
                {
                    Debug.Log($"{shooter.name} rolls 1! Shot is off target. GoalKick awarded.");
                    // TODO: Implement GoalKick
                }
                else
                {
                    Debug.Log($"{shooter.name} Shot roll: {shooterRoll} + Shooting: {shooter.shooting}{snapPenalty}{boxPenalty} = {totalShotPower}");
                    Debug.Log($"Get IN!! {shooter.name}, buries it to the top corner! Goal!!!");
                    // MatchManager.Instance.ScoreGoal(shooter);
                    // TODO: Implement Goal scoring
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
      // int gkRoll = UnityEngine.Random.Range(1, 7);
      int gkRoll = 6;
      isWaitingforHandlingTest = false;
      // Handling Test
      if (gkRoll < gkToken.handling)
      {
          Debug.Log($"{gkToken.name} rolled {gkRoll} and holds the ball! Save and Hold Scenario");
          // TODO: Save and Hold Scenario
      }
      else 
      {
          Debug.Log($"{gkToken.name} rolled {gkRoll} and can't hold it!");
          StartCoroutine(looseBallManager.ResolveLooseBall(gkToken, "handling"));
      }
      ResetShotProcess();
    }

    private void ResetShotProcess()
    {
        isShotInProgress = false;
        isWaitingforBlockerSelection = false;
        isWaitingForBlockDiceRoll = false;
        isWaitingForShotRoll = false;
        isWaitingforBlockerMovement = false;
        isWaitingForTargetSelection = false;
        shooter = null;
        targetHex = null;
        trajectoryPath = null;
        interceptors.Clear();
    }
}
