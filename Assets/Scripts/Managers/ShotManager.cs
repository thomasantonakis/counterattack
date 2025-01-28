using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine.Analytics;
using System;

public class ShotManager : MonoBehaviour
{
    public MovementPhaseManager movementPhaseManager;
    public GameInputManager gameInputManager;
    public LooseBallManager looseBallManager;
    public HexGrid hexGrid;
    public bool isShotInProgress = false;  // Tracks if a shot is active
    public bool isWaitingforBlockerSelection = false;  // Tracks if we are waiting to select a blocker
    public bool isWaitingforBlockerMovement = false;  // Tracks if we are waiting for the selected blocker to move
    public bool isWaitingForTargetSelection = false;  // Tracks if we are waiting for shot target selection
    public bool isWaitingForBlockDiceRoll = false;  // Tracks we are in the Blocking Phase
    public bool isWaitingForShotRoll = false;  // Tracks we are in the Blocking Phase
    public string shotType;               // "snapshot" or "fullPower"
    public PlayerToken shooter;          // The token that is shooting
    public PlayerToken tokenMoveforDeflection;          // The token that is shooting
    public HexCell targetHex;            // The CanShootTo hex selected by the attacker
    public HexCell currentDefenderBlockingHex; // The Hex of the defender currently attempting to intercept
    private List<HexCell> trajectoryPath; // The list of hexes the ball will travel through
    private List<(PlayerToken defender, bool isCausingInvalidity)> interceptors; // Defenders trying to intercept

    void Update()
    {
        // Check if waiting for dice rolls and the R key is pressed
        if (isWaitingForBlockDiceRoll && Input.GetKeyDown(KeyCode.R))
        {
            StartCoroutine(StartShotBlockRoll());  // Pass the stored list
        }
        if (isWaitingForShotRoll && !isWaitingForBlockDiceRoll && Input.GetKeyDown(KeyCode.R))
        {
            StartShotRoll();  // Pass the stored list
        }
    }
    public void StartShotProcess(PlayerToken shootingToken, string shotType)
    {
        hexGrid.ClearHighlightedHexes();
        if (!shootingToken.GetCurrentHex().CanShootFrom)
        {
            Debug.LogError($"Token {shootingToken.name} cannot shoot from this hex!");
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
        MatchManager.Instance.currentState = MatchManager.GameState.SnapshotDefenderMovement;
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
        }
    }

    private void StartInterceptionPhase()
    {
        Debug.Log("Starting interception phase.");
        interceptors = GatherInterceptors(trajectoryPath);

        if (interceptors.Count == 0)
        {
            Debug.Log("No defenders can intercept. Proceeding to shot roll.");
            isWaitingForShotRoll = true;
            return;
        }

        Debug.Log($"Defenders with interception chances: {interceptors.Count}");
        interceptors = interceptors.OrderBy(d =>
            HexGridUtils.GetHexDistance(shooter.GetCurrentHex().coordinates, d.defender.GetCurrentHex().coordinates)).ToList();

        currentDefenderBlockingHex = interceptors[0].defender.GetCurrentHex();
        Debug.Log($"Starting dice roll sequence with {interceptors[0].defender.name}... Press [R] key."); // TODO: Make this log the defender and the toll needed.
        isWaitingForBlockDiceRoll = true;
        
    }

    private List<(PlayerToken defender, bool isCausingInvalidity)> GatherInterceptors(List<HexCell> path)
    {
        List<(PlayerToken defender, bool isCausingInvalidity)> onPathDefenders = new List<(PlayerToken defender, bool isCausingInvalidity)>();
        PlayerToken invalidityCausingDefender = null;
        foreach (HexCell hex in path)
        {
            if (hex.isDefenseOccupied)
            {
                PlayerToken defenderOnPath = hex.GetOccupyingToken();
                // After movement: Defender on the path causes the pass to become dangerous
                onPathDefenders.Add((defenderOnPath, true));  // Add defender as blocking path
                invalidityCausingDefender = defenderOnPath;  // Keep track for later rolls
                Debug.Log($"Path blocked by defender at hex: {hex.coordinates}. Defender: {defenderOnPath.name}");
            }
        }
        // Step 4: Get defenders and their ZOI (neighbors)
        List<HexCell> defenderHexes = hexGrid.GetDefenderHexes();
        List<HexCell> defenderNeighbors = hexGrid.GetDefenderNeighbors(defenderHexes);
        foreach (HexCell hex in path)
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
        return onPathDefenders;


    }

    private IEnumerator StartShotBlockRoll()
    {
        if (currentDefenderBlockingHex != null)
        {
            // Find the current defender's entry in the list of defenders
            var currentDefenderEntry = interceptors.Find(d => d.defender.GetCurrentHex() == currentDefenderBlockingHex);
            if (currentDefenderEntry.defender != null)
            {
                // Retrieve defender attributes
                PlayerToken defenderToken = currentDefenderEntry.defender;
                int tackling = defenderToken.tackling;
                string defenderName = defenderToken.playerName;
                int jerseyNumber = defenderToken.jerseyNumber;

                // Roll the dice
                // int diceRoll = Random.Range(1, 7);
                int diceRoll = 4;
                Debug.Log($"Dice roll by {jerseyNumber}. {defenderName} at {currentDefenderBlockingHex.coordinates}: {diceRoll}");
                isWaitingForBlockDiceRoll = false;
                // Calculate interception conditions
                bool isCausingInvalidity = currentDefenderEntry.isCausingInvalidity;
                int requiredRoll = isCausingInvalidity ? 5 : 6; // Base roll requirement
                bool successfulInterception = diceRoll >= requiredRoll || diceRoll + tackling >= 10;
                if (successfulInterception)
                {
                    Debug.Log($"Shot blocked by {jerseyNumber}. {defenderName} at {currentDefenderBlockingHex.coordinates}! Loose Ball!");
                    // TODO: Trigger LooseBall Event. from defenderToken ground
                    ResetDiceRolls(); // Reset interception process
                }
                else
                {
                    Debug.Log($"{jerseyNumber}. {defenderName} at {currentDefenderBlockingHex.coordinates} failed to block.");

                    // Remove this defender and move to the next
                    interceptors.Remove(currentDefenderEntry);

                    if (interceptors.Count > 0)
                    {
                        // Move to the next defender
                        currentDefenderBlockingHex = interceptors[0].defender.GetCurrentHex();
                        Debug.Log($"Next up: {interceptors[0].defender.name} needs to roll to block... Press R key."); // TODO: Make this log the defender and the toll needed.
                        yield return null;
                        isWaitingForBlockDiceRoll = true; // Wait for the next roll
                    }
                    else
                    {
                        // No more defenders, pass is successful
                        Debug.Log($"No more defenders to Deflect! The {shooter.name} may [R]oll! Good Luck!.");
                        yield return null;
                        isWaitingForShotRoll = true;
                    }
                }

            }
        }
    }

    private void StartShotRoll()
    {
        int roll = UnityEngine.Random.Range(1, 7);
        isWaitingForShotRoll = false;
        if (roll == 1)
        {
            Debug.Log("Shot is off target. GoalKick awarded.");
            // TODO: Implement GoalKick
        }
        else
        {
            int totalShotPower = roll + shooter.shooting;
            string boxPenalty = shooter.GetCurrentHex().isInPenaltyBox == 0 ? ", -1 outside the Penalty Box" : "";
            string snapPenalty = shotType == "snapshot" ? ", -1 for taking a Snapshot" : "";
            if (shotType == "snapshot") totalShotPower -= 1; 
            if (shooter.GetCurrentHex().isInPenaltyBox == 0) totalShotPower -= 1;
            Debug.Log($"{shooter.name} Shot roll: {roll} + Shooting: {shooter.shooting}{snapPenalty}{boxPenalty} = {totalShotPower}");
            Debug.Log($"Get IN!! {shooter.name}, buries it to the top corner! Goal!!!");
            // MatchManager.Instance.ScoreGoal(shooter);
            // TODO: Implement Goal scoring
        }

        // ResetShotProcess();
    }

    private void ResetDiceRolls()
    {

    }

    private void ResetShotProcess()
    {
        isShotInProgress = false;
        shooter = null;
        targetHex = null;
        trajectoryPath = null;
        interceptors.Clear();
    }
}
