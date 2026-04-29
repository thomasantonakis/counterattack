using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using System.Text;

public class HighPassManager : MonoBehaviour
{
    [Header("Dependencies")]
    public Ball ball;
    public HexGrid hexGrid;
    public GroundBallManager groundBallManager;
    public GameInputManager gameInputManager;
    public MovementPhaseManager movementPhaseManager;
    public OutOfBoundsManager outOfBoundsManager;
    public HeaderManager headerManager;
    public FinalThirdManager finalThirdManager;
    public GoalKeeperManager goalKeeperManager;
    public LooseBallManager looseBallManager;
    public FreeKickManager freeKickManager;
    public HelperFunctions helperFunctions;
    [Header("Runtime")]
    public bool isAvailable = false;
    public bool isActivated = false;
    public bool isWaitingForConfirmation = false; // Prevents token selection during confirmation stage
    public bool isWaitingForAttackerSelection = false; // Flag to check for Distance roll
    public bool isWaitingForAttackerMove = false; // Flag to check for Distance roll
    public bool isWaitingForDefenderSelection = false; // Flag to check for Distance roll
    public bool isWaitingForDefenderMove = false; // Flag to check for Distance roll
    public bool isWaitingForAccuracyRoll = false; // Flag to check for accuracy roll
    public bool isWaitingForDirectionRoll = false; // Flag to check for Direction roll
    public bool isWaitingForDistanceRoll = false; // Flag to check for Distance roll
    public bool isAvailableTargetsReady = false;
    [Header("Basic Selections")]
    public PlayerToken lockedAttacker;  // The attacker who is locked on the target hex
    public HexCell currentTargetHex;
    public HexCell intendedTargetHex; // New variable to store the intended target hex
    public HexCell finalTargetHex; // Final targethex
    public PlayerToken selectedToken;  // To store the selected attacker or defender token
    public List<HexCell> gkReachableHexes = new List<HexCell>();
    public List<PlayerToken> eligibleAttackers = new List<PlayerToken>();
    public int directionIndex;
    [Header("Flags")]
    public bool didGKMoveInDefPhase = false;
    public bool gkRushedOut = false;
    public PlayerToken defGK = null;
    public bool isWaitingForDefGKChallengeDecision = false;
    public bool isCornerKick = false;
    [Header("Tuning")]
    [Min(0)]
    public int minPassDistance = 6;
    private const int MAX_PASS_DISTANCE = 15;
    private const int ATTACKER_MOVE_RANGE = 3;
    private const int DEFENDER_MOVE_RANGE = 3;
    private const int ACCURACY_THRESHOLD = 8;
    private const int TARGET_PRECOMPUTE_BATCH_SIZE = 16;
    private readonly List<HexCell> availableHighPassTargetHexes = new();
    private HexCell hoveredHighPassTargetHex;
    private Coroutine availableTargetPrecomputeRoutine;
    private int availableTargetPrecomputeVersion = 0;
    private bool pendingDifficultyOneTargetHighlightRefresh = false;

    private void OnEnable()
    {
        GameInputManager.OnClick += OnClickReceived;
        GameInputManager.OnKeyPress += OnKeyReceived;
        GameInputManager.OnHover += OnHoverReceived;
    }

    private void OnDisable()
    {
        GameInputManager.OnClick -= OnClickReceived;
        GameInputManager.OnKeyPress -= OnKeyReceived;
        GameInputManager.OnHover -= OnHoverReceived;
    }


    private void OnClickReceived(PlayerToken token, HexCell hex)
    {
        if (goalKeeperManager.isActivated) return;
        if (isActivated)
        {
            if (isWaitingForConfirmation)
            {
                HandleHighPassProcess(hex);
                return;
            }
            if (isWaitingForAttackerSelection)
            {
                if (!isWaitingForAttackerMove)
                {
                    if (token == null)
                    {
                        Debug.LogWarning($"No token was clicked.");
                        return;  // Skip if the clicked token is a defender
                    }
                    if (!token.isAttacker)
                    {
                        Debug.LogWarning($"{token.name} is not an attacker.");
                        return;  // Skip if the clicked token is a defender
                    }
                    if (lockedAttacker == null) // not on an attacker, someone close needs to be selected and moved.
                    {
                        if (!eligibleAttackers.Contains(token))
                        {
                            Debug.LogWarning($"Cannot select {token.name}, as they cannot reach the HP target.");
                            return;  // Skip if the clicked token is the locked attacker
                        }
                        else
                        {
                            selectedToken = token;
                            hexGrid.ClearHighlightedHexes();
                            StartCoroutine(MoveSelectedAttackerToHex(currentTargetHex));
                            return;
                        }
                    }
                    else // on an attacker, the lockedAttacker is not null
                    {
                        if (token == lockedAttacker)
                        {
                            Debug.LogWarning($"Cannot move the locked attacker {token.name}.");
                            return;  // Skip if the clicked token is the locked attacker
                        }   
                        if (selectedToken == null || selectedToken != token) // No previous selection of attacker
                        {
                            // First attacker click or switching attacker
                            Debug.Log($"Attacker {token.name} selected.");
                            selectedToken = token;
                            hexGrid.ClearHighlightedHexes();
                            movementPhaseManager.HighlightValidMovementHexes(selectedToken, ATTACKER_MOVE_RANGE);
                            isWaitingForAttackerMove = true;
                        }
                        else if (selectedToken == token)
                        {
                            Debug.Log($"Attacker {token.name} already selected. Please click on a Higlighted Hex to move them there!");
                        }
                    }
                }
                else // if (isWaitingForAttackerMove)
                {
                    if (token == null && hexGrid.highlightedHexes.Contains(hex))
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
                    if (token != null && lockedAttacker != null && token == lockedAttacker)
                    {
                        Debug.LogWarning("You cannot click the Locked Attacker, resetting selection");
                        hexGrid.ClearHighlightedHexes();
                        selectedToken = null;
                        isWaitingForAttackerMove = false;  // Stop waiting for attacker move
                        return;
                    }
                    if (selectedToken == null || selectedToken != token) // No previous selection of attacker
                    {
                        // First attacker click or switching attacker
                        Debug.Log($"Attacker {token.name} selected.");
                        selectedToken = token;
                        hexGrid.ClearHighlightedHexes();
                        movementPhaseManager.HighlightValidMovementHexes(selectedToken, ATTACKER_MOVE_RANGE);
                        isWaitingForAttackerMove = true;
                    }
                    else if (selectedToken == token)
                    {
                        Debug.Log($"Attacker {token.name} already selected. Please click on a Higlighted Hex to move them there!");
                    }
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
                    movementPhaseManager.HighlightValidMovementHexes(selectedToken, DEFENDER_MOVE_RANGE);
                    isWaitingForDefenderMove = true;
                }
                else if (selectedToken == token)
                {
                    Debug.Log($"Defender {token.name} already selected. Please click on a Higlighted Hex to move them there!");
                }
            }
            else if (isWaitingForDefGKChallengeDecision)
            {
                if (hex != null && gkReachableHexes.Contains(hex))
                {
                    hexGrid.ClearHighlightedHexes();
                    MoveGKForHP(hex);
                }
                else
                {
                    Debug.LogWarning($"Cannot move GK there");
                }
            }
        }
    }

    private void OnHoverReceived(PlayerToken token, HexCell hex)
    {
        if (!ShouldShowDifficultyOneTargetHighlights() || !isAvailableTargetsReady)
        {
            hoveredHighPassTargetHex = null;
            return;
        }

        HexCell nextHoveredHex = availableHighPassTargetHexes.Contains(hex) ? hex : null;
        if (hoveredHighPassTargetHex == nextHoveredHex)
        {
            return;
        }

        hoveredHighPassTargetHex = nextHoveredHex;
        RefreshDifficultyOneHighPassTargetHighlights();
    }

    private void OnKeyReceived(KeyPressData keyData)
    {
        if (keyData.isConsumed) return;
        if (isAvailable && !isActivated && !freeKickManager.isWaitingForExecution && keyData.key == KeyCode.C)
        {
            MatchManager.Instance.TriggerHighPass();
            keyData.isConsumed = true;
            return;
        }
        if (isActivated)
        {
            bool hasRollOverride = RollInputOverride.TryParse(keyData, out RollInputOverride rollOverride);
            if (isWaitingForAttackerSelection && lockedAttacker != null && keyData.key == KeyCode.X)
            {
                ForfeitAttackerHighPassMove();
                keyData.isConsumed = true;
                return;
            }
            else if (isWaitingForDefenderSelection && keyData.key == KeyCode.X)
            {
                ForfeitDefenderHighPassMove();
                keyData.isConsumed = true;
                return;
            }
            else if (isWaitingForAccuracyRoll && (keyData.key == KeyCode.R || hasRollOverride))
            {
                PerformAccuracyRoll(hasRollOverride ? rollOverride : null); // Handle accuracy roll
                keyData.isConsumed = true;
                return;
            }
            else if (isWaitingForDirectionRoll && (keyData.key == KeyCode.R || hasRollOverride))
            {
                PerformDirectionRoll(hasRollOverride ? rollOverride : null); // Handle direction roll
                keyData.isConsumed = true;
                return;
            }
            else if (isWaitingForDistanceRoll && (keyData.key == KeyCode.R || hasRollOverride))
            {
                PerformDistanceRoll(hasRollOverride ? rollOverride : null); // Handle distance roll
                keyData.isConsumed = true;
                return;
            }
            else if (isWaitingForDefGKChallengeDecision && keyData.key == KeyCode.X)
            {
                hexGrid.ClearHighlightedHexes();
                Debug.Log($"GK chooses to not rush out for the High Pass, moving on!");
                isWaitingForDefGKChallengeDecision = false;
                keyData.isConsumed = true;
            }
        }
    }

    public void ActivateHighPass()
    {
        isActivated = true;
        isAvailable = false;  // Make it non available to avoid restarting this action again.
        isWaitingForConfirmation = true;
        if (MatchManager.Instance.difficulty_level == 3)
        {
            // TODO: Before offering [C], preflight that at least one valid High Pass target exists after a successful tackle or safe Movement Phase.
            CommitToThisAction();
            Debug.Log("High Pass committed on [C]. Select a valid target to continue.");
        }
        else if (MatchManager.Instance.difficulty_level == 1 && !isCornerKick)
        {
            HighlightAllValidHighPassTargets();
        }
        Debug.Log("HighPassManager activated. Waiting for target selection...");
    }

    public void BeginAvailableTargetPrecompute()
    {
        if (!CanPrecomputeAvailableTargets())
        {
            ResetAvailableTargetPrecompute();
            return;
        }

        if (isAvailableTargetsReady || availableTargetPrecomputeRoutine != null)
        {
            return;
        }

        int version = ++availableTargetPrecomputeVersion;
        availableTargetPrecomputeRoutine = StartCoroutine(PrecomputeAvailableHighPassTargets(version));
    }

    public void ResetAvailableTargetPrecompute()
    {
        availableTargetPrecomputeVersion++;
        if (availableTargetPrecomputeRoutine != null)
        {
            StopCoroutine(availableTargetPrecomputeRoutine);
            availableTargetPrecomputeRoutine = null;
        }

        isAvailableTargetsReady = false;
        availableHighPassTargetHexes.Clear();
        hoveredHighPassTargetHex = null;
        pendingDifficultyOneTargetHighlightRefresh = false;
    }

    private IEnumerator PrecomputeAvailableHighPassTargets(int version)
    {
        isAvailableTargetsReady = false;
        availableHighPassTargetHexes.Clear();
        hoveredHighPassTargetHex = null;

        int processed = 0;
        foreach (HexCell hex in hexGrid.cells)
        {
            if (version != availableTargetPrecomputeVersion)
            {
                availableTargetPrecomputeRoutine = null;
                yield break;
            }

            if (hex != null && !hex.isOutOfBounds && IsHighPassTargetAvailableForPreview(hex))
            {
                availableHighPassTargetHexes.Add(hex);
            }

            processed++;
            if (processed % TARGET_PRECOMPUTE_BATCH_SIZE == 0)
            {
                yield return null;
            }
        }

        if (version == availableTargetPrecomputeVersion)
        {
            isAvailableTargetsReady = true;
            if (pendingDifficultyOneTargetHighlightRefresh || ShouldShowDifficultyOneTargetHighlights())
            {
                pendingDifficultyOneTargetHighlightRefresh = false;
                RefreshDifficultyOneHighPassTargetHighlights();
                Debug.Log($"Successfully highlighted {availableHighPassTargetHexes.Count} precomputed valid hexes for High Pass.");
            }
        }

        availableTargetPrecomputeRoutine = null;
    }

    private bool EnsureAvailableTargetPrecomputeReady()
    {
        if (isAvailableTargetsReady)
        {
            return true;
        }

        pendingDifficultyOneTargetHighlightRefresh = ShouldShowDifficultyOneTargetHighlights();

        if (availableTargetPrecomputeRoutine == null && CanPrecomputeAvailableTargets())
        {
            int version = ++availableTargetPrecomputeVersion;
            availableTargetPrecomputeRoutine = StartCoroutine(PrecomputeAvailableHighPassTargets(version));
        }

        return false;
    }

    private bool CanPrecomputeAvailableTargets()
    {
        return MatchManager.Instance != null
            && MatchManager.Instance.difficulty_level == 1
            && !isCornerKick
            && (isAvailable || ShouldShowDifficultyOneTargetHighlights())
            && ball != null
            && hexGrid != null;
    }

    private void CommitToThisAction()
    {
        MatchManager.Instance.currentState = MatchManager.GameState.HighPass;  // Update game state
        MatchManager.Instance.CommitToAction();
    }

    public void HandleHighPassProcess(HexCell clickedHex, bool isGK = false)
    {
        if (clickedHex != null)
        { 
            Debug.Log($"Clicked hex: {clickedHex.coordinates}");
            HexCell ballHex = ball.GetCurrentHex();
            if (ballHex == null)
            {
                Debug.LogError("Ball's current hex is null! Ensure the ball has been placed on the grid.");
                return;
            }
            else
            {
                // Now handle the pass based on difficulty
                HandleHighPassBasedOnDifficulty(clickedHex, isGK);
            }   
        }
    }

    private void HandleHighPassBasedOnDifficulty(HexCell clickedHex, bool isGK = false)
    {
        int difficulty = MatchManager.Instance.difficulty_level;  // Get current difficulty
        // Centralized target validation
        bool isValid = ValidateHighPassTarget(clickedHex, isGK);
        // If the clicked hex is not valid, reset everything and reject the click
        if (!isValid)
        {
            // Debug.LogWarning("High Pass target is invalid.");
            // Reset the previous target and clicked hex
            currentTargetHex = null;
            // Clear the selected token and highlights
            selectedToken = null;
            lockedAttacker = null;  // Make sure no attacker is locked
            hoveredHighPassTargetHex = null;
            if (ShouldShowDifficultyOneTargetHighlights())
            {
                HighlightAllValidHighPassTargets();
            }
            else
            {
                hexGrid.ClearHighlightedHexes();
            }
            return;  // Reject invalid targets
        }
        // Difficulty-based handling
        if (difficulty == 3) // Hard Mode: Immediate action
        {
            Debug.Log("High Pass target confirmed. Difficulty 3 was already committed on [C].");
            ConfirmHighPassTargetSelection(clickedHex, false);
        }
        else if (difficulty == 2 || difficulty == 1)  // Medium/Easy Mode: Require confirmation with a second click
        {
            if (clickedHex == currentTargetHex)
            {
                Debug.Log("High Pass confirmed by second click.");
                ConfirmHighPassTargetSelection(clickedHex, true);
            }
            else
            {
                currentTargetHex = clickedHex;
                selectedToken = null;
                lockedAttacker = null;
                hexGrid.ClearHighlightedHexes();

                if (!isCornerKick)
                {
                    if (difficulty == 1)
                    {
                        HighlightAllValidHighPassTargets();
                        eligibleAttackers = GetAttackersWithinRangeOfHex(currentTargetHex, ATTACKER_MOVE_RANGE);
                    }
                    else
                    {
                        HighlightHighPassArea(clickedHex);
                    }
                }

                HighlightCommittedHighPassTarget();
                Debug.Log("First click registered. Click again to confirm the High Pass.");
            }
        }
    }

    private void ConfirmHighPassTargetSelection(HexCell clickedHex, bool commitNow)
    {
        currentTargetHex = clickedHex;
        intendedTargetHex = clickedHex;
        isWaitingForConfirmation = false;
        selectedToken = null;
        ResetAvailableTargetPrecompute();
        hexGrid.ClearHighlightedHexes();
        HighlightCommittedHighPassTarget();

        if (commitNow)
        {
            CommitToThisAction();
        }

        MatchManager.Instance.gameData.gameLog.LogEvent(
            MatchManager.Instance.LastTokenToTouchTheBallOnPurpose,
            MatchManager.ActionType.AerialPassAttempt
        );

        if (isCornerKick)
        {
            lockedAttacker = null;
            isWaitingForAccuracyRoll = true;
            Debug.Log("Waiting for accuracy roll... Please Press R key.");
            return;
        }

        if (clickedHex.isAttackOccupied)
        {
            lockedAttacker = clickedHex.GetOccupyingToken();
            Debug.Log($"Attacker {lockedAttacker.name} is locked on the target hex and cannot move.");
        }
        else
        {
            lockedAttacker = null;
        }

        StartCoroutine(StartAttackerMovementPhase());
    }

    private bool ValidateHighPassTarget(HexCell targetHex, bool isGK = false)
    {
        return TryValidateHighPassTarget(targetHex, isGK, updateEligibleAttackers: true, logWarnings: true);
    }

    private bool IsHighPassTargetAvailableForPreview(HexCell targetHex)
    {
        return TryValidateHighPassTarget(targetHex, isGK: false, updateEligibleAttackers: false, logWarnings: false);
    }

    private bool TryValidateHighPassTarget(HexCell targetHex, bool isGK, bool updateEligibleAttackers, bool logWarnings)
    {
        HexCell ballHex = ball.GetCurrentHex();
        // Step 1: Ensure the ballHex and targetHex are valid
        if (ballHex == null || targetHex == null)
        {
            if (logWarnings) Debug.LogError("Ball or target hex is null!");
            ClearValidatedHighPassAttackers(updateEligibleAttackers);
            return false;
        }
        if (targetHex.isOutOfBounds)
        {
            if (logWarnings) Debug.LogWarning("High Pass target must be inbounds.");
            ClearValidatedHighPassAttackers(updateEligibleAttackers);
            return false;
        }
        if (targetHex.isDefenseOccupied)
        {
            if (logWarnings) Debug.LogWarning("High Pass cannot target a defender-occupied hex.");
            ClearValidatedHighPassAttackers(updateEligibleAttackers);
            return false;
        }
        if (isGK)
        {
            // Specific HP from GK after a save and hold or GoalKick
            // reject only targets in the opposite final thirds.
            if (ballHex.isInFinalThird * targetHex.isInFinalThird == -1)
            {
                if (logWarnings) Debug.LogWarning($"GK High Pass cannot be targeted in the opposite Final Third");
                ClearValidatedHighPassAttackers(updateEligibleAttackers);
                return false;
            }
        }
        else if (isCornerKick)
        {
            int distance = HexGridUtils.GetHexStepDistance(ballHex, targetHex);
            // Check the distance limit
            if (
                !targetHex.isAttackOccupied // Target is not attack occupied
                || (
                    ballHex.isInFinalThird * targetHex.isInPenaltyBox != 1 // the target is in the same final third with the ball and in the box
                    && distance > MAX_PASS_DISTANCE // Or it is below the allowed distance.
                )
            )
            {
                if (logWarnings)
                {
                    if (!targetHex.isAttackOccupied) Debug.LogWarning("CornerKick High Pass should target an attacker, please click one within 15 or in the box!");
                    else Debug.LogWarning($"Corner Kick High Pass is out of range or not in the box. Maximum steps allowed: {MAX_PASS_DISTANCE}. Current steps: {distance}");
                }
                ClearValidatedHighPassAttackers(updateEligibleAttackers);
                return false;
            }
        }
        else
        {
            // Regular HP
            // Alternative Step 4
            int distance = HexGridUtils.GetHexStepDistance(ballHex, targetHex);
            // Check the distance limit
            if (distance > MAX_PASS_DISTANCE)
            {
                if (logWarnings) Debug.LogWarning($"High Pass is out of range. Maximum steps allowed: {MAX_PASS_DISTANCE}. Current steps: {distance}");
                ClearValidatedHighPassAttackers(updateEligibleAttackers);
                return false;
            }
            if (distance < minPassDistance)
            {
                if (logWarnings) Debug.LogWarning($"High Pass is too close. Minimum steps allowed: {minPassDistance}. Current steps: {distance}");
                ClearValidatedHighPassAttackers(updateEligibleAttackers);
                return false;
            }
        }
        // Step 2: Calculate the path between the ball and the target hex
        List<HexCell> pathHexes = groundBallManager.CalculateThickPath(ballHex, targetHex, ball.ballRadius);
        // Step 3: Check if the path is valid by ensuring no defense-occupied hexes touching the kicker block the path
        foreach (HexCell hex in pathHexes)
        {
            if (hex.isDefenseOccupied && ballHex.GetNeighbors(hexGrid).Contains(hex))
            {
                if (logWarnings) Debug.LogWarning($"Path blocked by defender at hex: {hex.coordinates}");
                ClearValidatedHighPassAttackers(updateEligibleAttackers);
                return false; // Invalid path
            }
        }
        List<PlayerToken> attackersWithinRange = GetAttackersWithinRangeOfHex(targetHex, ATTACKER_MOVE_RANGE);
        // Step 5: Check if the target hex is occupied by an attacker
        if (targetHex.isAttackOccupied)
        {
            // Store these attackers for movement phase
            SetValidatedHighPassAttackers(attackersWithinRange, updateEligibleAttackers);
            return true;  // If occupied by an attacker, the target is valid
        }
        // Step 6: If the target is not occupied, check if any attacker can reach it within 3 moves
        if (attackersWithinRange.Count > 0)
        {
            if (logWarnings) Debug.Log("Empty hex is valid for High Pass, at least one attacker can reach it.");
            // Store these attackers for movement phase
            SetValidatedHighPassAttackers(attackersWithinRange, updateEligibleAttackers);
            return true;
        }
        else
        {
            if (logWarnings) Debug.LogWarning("No attackers can reach the target hex. High Pass is invalid.");
            ClearValidatedHighPassAttackers(updateEligibleAttackers);
            return false;
        }
    }

    private void ClearValidatedHighPassAttackers(bool updateEligibleAttackers)
    {
        if (!updateEligibleAttackers)
        {
            return;
        }

        eligibleAttackers.Clear();
    }

    private void SetValidatedHighPassAttackers(List<PlayerToken> attackers, bool updateEligibleAttackers)
    {
        if (!updateEligibleAttackers)
        {
            return;
        }

        eligibleAttackers.Clear();
        eligibleAttackers.AddRange(attackers);
    }
    
    private List<PlayerToken> GetAttackersWithinRangeOfHex(HexCell targetHex, int range)
    {
        List<PlayerToken> eligibleAttackers = new List<PlayerToken>();
        List<HexCell> reachableHexes;

        // Get all attackers currently on the field
        List<HexCell> attackerHexes = hexGrid.GetAttackerHexes();

        foreach (HexCell attackerHex in attackerHexes)
        {
            PlayerToken attackerToken = attackerHex.GetOccupyingToken();  // Get the token occupying the attacker hex

            if (attackerToken != null)
            {
                // Calculate reachable hexes for this attacker
                reachableHexes = HexGridUtils.GetReachableHexes(hexGrid, attackerHex, range).Item1;

                // If the target hex is within their reachable hexes, add them to the eligible list
                if (reachableHexes.Contains(targetHex))
                {
                    eligibleAttackers.Add(attackerToken);
                }
            }
        }

        return eligibleAttackers;
    }

    public void PerformAccuracyRoll(int? rigroll = null)
    {
        RollInputOverride? rollOverride = rigroll.HasValue
            ? new RollInputOverride
            {
                hasOverride = true,
                roll = rigroll.Value,
                isJackpot = false
            }
            : null;
        PerformAccuracyRoll(rollOverride);
    }

    public void PerformAccuracyRoll(RollInputOverride? rollOverride)
    {
        // TODO: Refine order and logs
        lockedAttacker = null;
        // Placeholder for dice roll logic (will be expanded in later steps)
        Debug.Log("Performing accuracy roll for High Pass. Please Press R key.");
        // Roll the dice (1 to 6)
        var (returnedRoll, returnedJackpot) = helperFunctions.DiceRoll();
        int diceRoll = GetRollValueWithoutJackpot(rollOverride, returnedRoll);
        // int diceRoll = 6; // Melina Mode
        isWaitingForAccuracyRoll = false;
        PlayerToken attackerToken = MatchManager.Instance.LastTokenToTouchTheBallOnPurpose;
        if (attackerToken == null)
        {
            Debug.LogError("Error: No attacker token found on the ball's hex!");
            return;
        }

        int highPassAttribute = attackerToken.highPass;
        Debug.Log($"Passer: {attackerToken.name}, HighPass: {highPassAttribute}");
        // Adjust threshold based on difficulty
        if (diceRoll + highPassAttribute >= ACCURACY_THRESHOLD)
        {
            Debug.Log($"High Pass is accurate, passer roll: {diceRoll}");
            // Move the ball to the intended target
            finalTargetHex = intendedTargetHex;
            MatchManager.Instance.gameData.gameLog.LogEvent(MatchManager.Instance.LastTokenToTouchTheBallOnPurpose, MatchManager.ActionType.AerialPassTargeted);
            StartCoroutine(HandleHighPassMovement(finalTargetHex));
            // await helperFunctions.StartCoroutineAndWait(HandleHighPassMovement(finalTargetHex));
            MatchManager.Instance.currentState = MatchManager.GameState.HighPassCompleted;
            ResetHighPassRolls();  // Reset flags to finish long pass
        }
        else
        {
            Debug.Log($"High Pass is NOT accurate, passer roll: {diceRoll}");
            isWaitingForDirectionRoll = true;
            Debug.Log("Waiting for Direction roll... Please Press R key.");
        }
    }

    public void PerformDirectionRoll(int? rigroll = null)
    {
        RollInputOverride? rollOverride = rigroll.HasValue
            ? new RollInputOverride
            {
                hasOverride = true,
                roll = rigroll.Value,
                isJackpot = false
            }
            : null;
        PerformDirectionRoll(rollOverride);
    }

    public void PerformDirectionRoll(RollInputOverride? rollOverride)
    {
        // directionRoll = 0; // S  : PerformDirectionRoll(1)
        // directionRoll = 1; // SW : PerformDirectionRoll(2)
        // directionRoll = 2; // NW : PerformDirectionRoll(3)
        // directionRoll = 3; // N  : PerformDirectionRoll(4)
        // directionRoll = 4; // NE : PerformDirectionRoll(5)
        // directionRoll = 5; // SE : PerformDirectionRoll(6)
        // Debug.Log("Performing Direction roll to find Long Pass destination.");
        var (returnedRoll, returnedJackpot) = helperFunctions.DiceRoll();
        int diceRoll = GetRollValueWithoutJackpot(rollOverride, returnedRoll) - 1;
        // int diceRoll = 0; // South Mode
        directionIndex = diceRoll;  // Set the direction index for future use
        int diceRollLabel = diceRoll + 1;
        string rolledDirection = looseBallManager.TranslateRollToDirection(diceRoll);
        Debug.Log($"Rolled {diceRollLabel}: Moving in {rolledDirection} direction");
        isWaitingForDirectionRoll = false;
        isWaitingForDistanceRoll = true;
        Debug.Log("Waiting for Distance roll... Please Press R key.");
    }

    public void PerformDistanceRoll(int? rigroll = null)
    {
        RollInputOverride? rollOverride = rigroll.HasValue
            ? new RollInputOverride
            {
                hasOverride = true,
                roll = rigroll.Value,
                isJackpot = false
            }
            : null;
        PerformDistanceRoll(rollOverride);
    }

    public void PerformDistanceRoll(RollInputOverride? rollOverride)
    {
        Debug.Log("Performing Direction roll to find Long Pass destination.");
        var (returnedRoll, returnedJackpot) = helperFunctions.DiceRoll();
        int distanceRoll = GetRollValueWithoutJackpot(rollOverride, returnedRoll);
        // int distanceRoll = 5; // Melina Mode
        isWaitingForDistanceRoll = false;
        Debug.Log($"Distance Roll: {distanceRoll} hexes away from target.");
        // Calculate the final target hex based on the direction and distance
        LooseBallManager.InaccuracyTargetResult result = looseBallManager.CalculateFinalInaccuracyTarget(currentTargetHex, directionIndex, distanceRoll);
        finalTargetHex = result.FinalHex;
        // Check if the final hex is valid (not out of bounds or blocked)
        if (finalTargetHex != null)
        {
            if (result.IsOutOfBounds)
            {
                Debug.Log("LooseBallManager resolved this inaccurate High Pass out of bounds.");
            }
            // Move the ball to the inaccurate final hex
            // yield return StartCoroutine(HandleHighPassMovement(finalTargetHex));           
            // await helperFunctions.StartCoroutineAndWait(HandleHighPassMovement(finalTargetHex)); 
            StartCoroutine(HandleHighPassMovement(finalTargetHex));
        }
        else
        {
           Debug.LogWarning("Final hex calculation failed.");
        }
        ResetHighPassRolls();  // Reset flags to finish long pass
    }

    private int GetRollValueWithoutJackpot(RollInputOverride? rollOverride, int returnedRoll)
    {
        if (!rollOverride.HasValue || !rollOverride.Value.hasOverride)
        {
            return returnedRoll;
        }

        return rollOverride.Value.isJackpot ? 6 : rollOverride.Value.roll;
    }

    private void ResetHighPassRolls()
    {
        isWaitingForAccuracyRoll = false;
        isWaitingForDirectionRoll = false;
        isWaitingForDistanceRoll = false;
        lockedAttacker = null;  // Unlock the attacker after the HP is done
    }

    private IEnumerator HandleHighPassMovement(HexCell targetHex)
    {
        HexCell positionOfpasser = ball.GetCurrentHex();
        if (targetHex == null)
        {
            Debug.LogError("Target Hex is null in HandleHighPassMovement!");
            yield break;
        }
        Vector3 startPosition = ball.transform.position;
        Vector3 targetPosition = targetHex.GetHexCenter();
        float height = 10f;
        int steps = 90;
        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            Vector3 flatPosition = Vector3.Lerp(startPosition, targetPosition, t);
            flatPosition.y += height * Mathf.Sin(Mathf.PI * t);
            if (
                float.IsNaN(flatPosition.x) || float.IsInfinity(flatPosition.x)
                || float.IsNaN(flatPosition.y) || float.IsInfinity(flatPosition.y)
                || float.IsNaN(flatPosition.z) || float.IsInfinity(flatPosition.z)
            )
            {
                Debug.LogError($"❌ Invalid flatPosition at i={i}: {flatPosition}");
                yield break;
            }
            ball.transform.position = flatPosition;
            yield return null;
        }
        ball.PlaceAtCell(targetHex);
        Debug.Log($"Ball has reached its destination: {targetHex.coordinates}");

        if (
            goalKeeperManager.ShouldGKMove(targetHex)
            && positionOfpasser.isInPenaltyBox * targetHex.isInPenaltyBox != 1
        )
        {
            yield return StartCoroutine(goalKeeperManager.HandleGKFreeMove());
        }

        StartCoroutine(PostBallMovementHandling());
    }

    // private IEnumerator HandleHighPassMovement(HexCell targetHex)
    // {
    //     if (targetHex == null)
    //     {
    //         Debug.LogError("Target Hex is null in HandleHighPassMovement!");
    //         yield break;
    //     }
    //     HexCell positionOfpasser = ball.GetCurrentHex();
    //     Vector3 startPosition = ball.transform.position;
    //     Vector3 targetPosition = targetHex.GetHexCenter();
    //     float travelDuration = 2.0f;  // Duration of the ball's flight
    //     float elapsedTime = 0;
    //     float height = 10f;// Height of the arc for the aerial trajectory
    //     // Debug.Log($"HandleHighPassMovement → start: {startPosition}, target: {targetPosition}, duration: {travelDuration}");
    //     int safetyCounter = 0;
    //     const int maxFrames = 300;
    //     // Debug.Log($"Time.timeScale = {Time.timeScale}");

    //     // isMoving = true;

    //     while (elapsedTime < travelDuration && safetyCounter++ < maxFrames)
    //     {
    //         if (safetyCounter == 10) Debug.Break();
    //         elapsedTime += Time.deltaTime;
    //         // float progress = elapsedTime / travelDuration;
    //         float progress = Mathf.Clamp01(elapsedTime / travelDuration);
    //         // Lerp position along the straight line
    //         Vector3 flatPosition = Vector3.Lerp(startPosition, targetPosition, progress);
    //         // // Add the arc (use a sine curve to create the arc)
    //         flatPosition.y += height * Mathf.Sin(Mathf.PI * progress);
    //         // // Combine the flat position with the height offset to create the arc
    //         ball.transform.position = flatPosition;
    //         // Debug.Log($"[HP Move] progress: {progress:F3}, elapsedTime: {elapsedTime:F3}, pos: {ball.transform.position}");
    //         if (Time.deltaTime == 0) Debug.LogWarning("⚠ Time.deltaTime == 0");
    //         if (float.IsNaN(flatPosition.x) || float.IsNaN(flatPosition.y) || float.IsNaN(flatPosition.z))
    //         {
    //             Debug.LogError($"NaN detected at progress: {progress}, elapsedTime: {elapsedTime}");
    //             yield break;
    //         }
    //         if (float.IsNaN(ball.transform.position.y))
    //         {
    //             Debug.LogError("Ball Y is NaN! Breaking early.");
    //             yield break;
    //         }
    //         if (++safetyCounter > maxFrames)
    //         {
    //             Debug.LogWarning("⚠️ Ball movement exceeded max frame count! Breaking out of animation loop.");
    //             break;
    //         }
    //         yield return null;  // Wait for the next frame
    //     }
    //     // Debug.Log($"Time.timeScale = {Time.timeScale}");
    //     // Debug.Break();
    //     // isMoving = false;  // Stop the movement
    //     // Ensure the ball ends exactly on the target hex
    //     ball.PlaceAtCell(targetHex);
    //     // Debug.Log($"Ball has reached its destination: {targetHex.coordinates}.");
        
    //     if (
    //         goalKeeperManager.ShouldGKMove(targetHex)
    //         && positionOfpasser.isInPenaltyBox * targetHex.isInPenaltyBox != 1 // The HP was played from anywhere except the same box
    //     )
    //     {
    //         yield return StartCoroutine(goalKeeperManager.HandleGKFreeMove());
    //     }
    //     StartCoroutine(PostBallMovementHandling());
    // }

    private IEnumerator PostBallMovementHandling()
    {
        // Debug.Log("[PostBallMovementHandling] Entered coroutine");
        // After movement completes, check if the ball is out of bounds
        if (finalTargetHex.isOutOfBounds)
        {
            Debug.Log("Ball landed out of bounds!");
            Debug.Log($"Passing targetHex to HandleOutOfBounds: {currentTargetHex.coordinates}");
            outOfBoundsManager.HandleOutOfBounds(currentTargetHex, directionIndex, "inaccuracy", MatchManager.Instance.LastTokenToTouchTheBallOnPurpose);
        }
        else
        {
            MatchManager.Instance.SetHangingPass("aerial");
            Debug.Log("Ball landed within bounds.");
            // Check if the defending GK can challenge
            gkReachableHexes = CanDefendingGKChallenge();
            // Debug.Log($"gkReachableHexes.Count: {gkReachableHexes.Count}");
            if (gkReachableHexes.Count > 0)
            {
                NotifyForGkRushAvailability();
                while (isWaitingForDefGKChallengeDecision)
                {
                    yield return null;
                }
            }
            else
            {
                Debug.Log("GK cannot rush out to challenge.");
            }
            finalThirdManager.TriggerFinalThirdPhase();
            StartCoroutine(headerManager.FindEligibleHeaderTokens(finalTargetHex));
        }
        CleanUpHighPass();
    }

    private void NotifyForGkRushAvailability()
    {
        defGK = hexGrid.GetDefendingGK();
        Debug.Log($"Defending GK {defGK.name} is to move closer and Challenge! Press [X] to forfeit or Click on a highlighted Hex to go and Jump wiith GK...");
        isWaitingForDefGKChallengeDecision = true;
        hexGrid.ClearHighlightedHexes();
        foreach (HexCell hex in gkReachableHexes)
        {
            hex.HighlightHex("ballPath");
            hexGrid.highlightedHexes.Add(hex);  // Track the highlighted hexes
        }
    }

    private List<HexCell> CanDefendingGKChallenge()
    {
        if (didGKMoveInDefPhase) return new List<HexCell>(); // GK already used their movement
        if (hexGrid.GetDefendingGK() == null) return new List<HexCell>(); // No defending GK exists
        if (finalTargetHex.isInPenaltyBox == 0) return new List<HexCell>(); // Better not rush to Handle a HP outside the box :)
        HexCell gkHex = hexGrid.GetDefendingGK().GetCurrentHex();
        List<HexCell> reachableHexes = HexGridUtils.GetReachableHexes(hexGrid, gkHex, 3).Item1;
        List<HexCell> challengeHexes = HexGrid.GetHexesInRange(hexGrid, finalTargetHex, 2);
        // Find intersection of reachable hexes & valid challenge spots
        return reachableHexes.Intersect(challengeHexes).ToList();
    }

    private void HighlightCommittedHighPassTarget()
    {
        if (currentTargetHex == null)
        {
            return;
        }

        currentTargetHex.HighlightHex("passTargetCommitted");
        if (!hexGrid.highlightedHexes.Contains(currentTargetHex))
        {
            hexGrid.highlightedHexes.Add(currentTargetHex);
        }
    }

    private void HighlightHighPassArea(HexCell targetHex)
    {
        hexGrid.ClearHighlightedHexes();
        if (targetHex == null)
        {
            Debug.LogError("Target hex is null in HighlightHighPassArea!");
            return;
        }
        // Initialize highlightedHexes to ensure it's ready for use
        hexGrid.highlightedHexes = new List<HexCell>();
        // Get hexes within a radius (e.g., 6 hexes) around the targetHex
        List<HexCell> hexesInRange = HexGrid.GetHexesInRange(hexGrid, targetHex, 3);
        if (hexesInRange == null || hexesInRange.Count == 0)
        {
            Debug.LogError("No hexes found in range for highlighting.");
            return;
        }

        // Loop through the hexes and highlight each one
        foreach (HexCell hex in hexesInRange)
        {
            if (hex == null)
            {
                // Debug.LogWarning("Encountered a null hex while highlighting, skipping this hex.");
                continue;  // Skip null hexes
            }

            if (hex.isOutOfBounds || hex.isDefenseOccupied)
            {
                // Debug.LogWarning($"Hex {hex.coordinates} is out of bounds, skipping highlight.");
                continue;  // Skip out of bounds hexes
            }
            if (hex == targetHex)
            {
                // Highlight hexes (use a specific color for Long Pass)
                hex.HighlightHex("passTarget");  // Assuming HexHighlightReason.LongPass is defined for long pass highlights
            }
            else
            {
                // Highlight hexes (use a specific color for Long Pass)
                hex.HighlightHex("highPass");  // Assuming HexHighlightReason.LongPass is defined for long pass highlights
            }
            hexGrid.highlightedHexes.Add(hex);  // Track the highlighted hexes for later clearing
            // Debug.Log($"Highlighted Hex at coordinates: ({hex.coordinates.x}, {hex.coordinates.z})");
        }

        // Log the highlighted hexes if needed (optional)
        // Debug.Log($"Highlighted {hexesInRange.Count} hexes around the target for a Long Pass.");
    }

    private void HighlightAllValidHighPassTargets()
    {
        if (!EnsureAvailableTargetPrecomputeReady())
        {
            hexGrid.ClearHighlightedHexes();
            Debug.Log("High Pass target map is still calculating. Highlights will draw when ready.");
            return;
        }

        RefreshDifficultyOneHighPassTargetHighlights();
        Debug.Log($"Successfully highlighted {availableHighPassTargetHexes.Count} valid hexes for High Pass.");
    }

    private bool ShouldShowDifficultyOneTargetHighlights()
    {
        return isActivated
            && isWaitingForConfirmation
            && MatchManager.Instance != null
            && MatchManager.Instance.difficulty_level == 1
            && !isCornerKick;
    }

    private void RefreshDifficultyOneHighPassTargetHighlights()
    {
        hexGrid.ClearHighlightedHexes();

        foreach (HexCell hex in availableHighPassTargetHexes)
        {
            if (hex == null)
            {
                continue;
            }

            string highlightReason = hex == currentTargetHex || hex == hoveredHighPassTargetHex
                ? "passTargetCommitted"
                : "PaceAvailable";
            hex.HighlightHex(highlightReason);

            if (!hexGrid.highlightedHexes.Contains(hex))
            {
                hexGrid.highlightedHexes.Add(hex);
            }
        }
    }

    private IEnumerator StartAttackerMovementPhase()
    {
        Debug.Log("Attacker HP movement phase started. Move one attacker up to 3 hexes.");
        isWaitingForAttackerSelection = true;  // Now allow attacker selection
        selectedToken = null;  // Ensure no token is auto-selected
        // Allow attackers to move one token up to 3 hexes
        // Check if the target hex is unoccupied, and find attackers that can reach it
        if (!currentTargetHex.isAttackOccupied)
        {
            eligibleAttackers = GetAttackersWithinRangeOfHex(currentTargetHex, ATTACKER_MOVE_RANGE);

            if (eligibleAttackers.Count == 0)
            {
                Debug.LogError("No attackers can reach the target hex.");
                // Handle case where no attackers can move to the target (potentially cancel the High Pass or retry)
                yield break;
            }
            else if (eligibleAttackers.Count == 1)
            {
                // **Automatic move for single eligible attacker**
                selectedToken = eligibleAttackers[0];
                isWaitingForAttackerSelection = false;
                hexGrid.ClearHighlightedHexes();
                Debug.Log($"Automatically moving attacker {selectedToken.name} to target hex.");
                // Automatically move the attacker to the target hex
                StartCoroutine(MoveSelectedAttackerToHex(currentTargetHex));
                yield break;  // Skip further input handling
            }
            else
            {
                // Multiple eligible attackers: highlight their current hexes and wait for one direct token click.
                Debug.Log($"Found {eligibleAttackers.Count} attackers who can reach the target hex.");
                HighlightEligibleAttackerHexes();
            }
        }
        else
        {
            Debug.Log("Initial target already has an attacker. Press [X] to forfeit Attacker HP movement or move another attacker up to 3 hexes.");
        }
        // Wait for player to move an attacker
    }

    private void HighlightEligibleAttackerHexes()
    {
        hexGrid.ClearHighlightedHexes();
        foreach (PlayerToken attacker in eligibleAttackers)
        {
            HexCell attackerHex = attacker != null ? attacker.GetCurrentHex() : null;
            if (attackerHex == null)
            {
                continue;
            }

            attackerHex.HighlightHex("ReachOverlayAttacker");
            if (!hexGrid.highlightedHexes.Contains(attackerHex))
            {
                hexGrid.highlightedHexes.Add(attackerHex);
            }
        }
    }

    private void ForfeitAttackerHighPassMove()
    {
        if (lockedAttacker == null)
        {
            Debug.LogWarning("Attacker HP movement cannot be forfeited because an attacker must move onto the initial target.");
            return;
        }

        Debug.Log("Attacker HP movement forfeited. Proceeding to Defender HP movement.");
        hexGrid.ClearHighlightedHexes();
        selectedToken = null;
        isWaitingForAttackerMove = false;
        isWaitingForAttackerSelection = false;
        StartDefenderMovementPhase();
    }

    private void ForfeitDefenderHighPassMove()
    {
        Debug.Log("Defender HP movement forfeited. Waiting for accuracy roll. Please Press R key.");
        hexGrid.ClearHighlightedHexes();
        selectedToken = null;
        isWaitingForDefenderMove = false;
        isWaitingForDefenderSelection = false;
        isWaitingForAccuracyRoll = true;
    }

    private IEnumerator MoveSelectedAttackerToHex(HexCell hex)
    {
        hexGrid.ClearHighlightedHexes();
        isWaitingForAttackerMove = false;  // Stop waiting for attacker move
        isWaitingForAttackerSelection = false;  // Stop waiting for attacker selection
        Debug.Log($"Moving {selectedToken.name} to hex {hex.coordinates}");
        // yield return StartCoroutine(movementPhaseManager.MoveTokenToHex(hex, selectedToken, false));  // Pass the selected token
        yield return StartCoroutine(movementPhaseManager.MoveTokenToHex(targetHex: hex, token: selectedToken, isCalledDuringMovement: false, shouldCountForDistance: true, shouldCarryBall: false));  // Pass the selected token
        movementPhaseManager.isActivated = false;
        selectedToken = null;
        StartDefenderMovementPhase();
    }

    private void StartDefenderMovementPhase()
    {
        Debug.Log("Defender HP movement phase started. Move one defender up to 3 hexes, or press [X] to forfeit.");
        isWaitingForDefenderSelection = true;  // Now allow defender selection
        selectedToken = null;  // Ensure no token is auto-selected
        // Find the defending goalkeeper and intialize the flag to false
        didGKMoveInDefPhase = false;
    }

    private IEnumerator MoveSelectedDefenderToHex(HexCell hex)
    {
        hexGrid.ClearHighlightedHexes();
        if (selectedToken == hexGrid.GetDefendingGK()) didGKMoveInDefPhase = true;
        isWaitingForDefenderMove = false;  // Stop waiting for attacker move
        isWaitingForDefenderSelection = false;  // Stop waiting for attacker selection
        Debug.Log($"Moving {selectedToken.name} to hex {hex.coordinates}");
        yield return StartCoroutine(movementPhaseManager.MoveTokenToHex(targetHex: hex, token: selectedToken, isCalledDuringMovement: false, shouldCountForDistance: true));  // Pass the selected token
        movementPhaseManager.isActivated = false;
        movementPhaseManager.isBallPickable = false;
        selectedToken = null;
        isWaitingForAccuracyRoll = true;
        Debug.Log("Waiting for accuracy roll... Please Press R key.");
        // CompleteDefenderMovementPhase();
    }

    private async Task MoveGKForHP(HexCell hex)
    {
        hexGrid.ClearHighlightedHexes();
        Debug.Log($"🧤 {defGK.name} moving to {hex.name}");
        await helperFunctions.StartCoroutineAndWait(movementPhaseManager.MoveTokenToHex(hex, defGK, false));
        Debug.Log($"🧤 {defGK.name} moved to {hex.name}");
        gkRushedOut = true;
        headerManager.defenderWillJump.Add(defGK);
        isWaitingForDefGKChallengeDecision = false;
    }

    public void CleanUpHighPass(bool preserveTargetPrecompute = false)
    {
        selectedToken = null;
        currentTargetHex = null;
        intendedTargetHex = null;
        finalTargetHex = null;
        hoveredHighPassTargetHex = null;
        pendingDifficultyOneTargetHighlightRefresh = false;
        isCornerKick = false;
        directionIndex = 240885; // Something implausible
        eligibleAttackers.Clear();
        if (!preserveTargetPrecompute)
        {
            ResetAvailableTargetPrecompute();
        }
        gkReachableHexes.Clear();
        isActivated = false;
        isWaitingForConfirmation = false;
        isWaitingForAttackerSelection = false;
        isWaitingForAttackerMove = false;
        isWaitingForDefenderSelection = false;
        isWaitingForDefenderMove = false;
        isWaitingForAccuracyRoll = false;
        isWaitingForDirectionRoll = false;
        isWaitingForDistanceRoll = false;
        isWaitingForDefGKChallengeDecision = false;
        // didGKMoveInDefPhase = false; // Reset in headerManager.FindEligibleHeaderTokens()
    }

    public string GetDebugStatus()
    {
        StringBuilder sb = new();
        sb.Append("HP: ");

        if (isActivated) sb.Append("isActivated, ");
        if (isAvailable) sb.Append("isAvailable, ");
        if (isWaitingForConfirmation) sb.Append("isAwaitingTargetSelection, ");
        if (isWaitingForAttackerSelection) sb.Append("isWaitingForAttackerSelection, ");
        if (isWaitingForAttackerMove) sb.Append("isWaitingForAttackerMove, ");
        if (isWaitingForDefenderSelection) sb.Append("isWaitingForDefenderSelection, ");
        if (isWaitingForDefenderMove) sb.Append("isWaitingForDefenderMove, ");
        if (isWaitingForAccuracyRoll) sb.Append("isWaitingForAccuracyRoll, ");
        if (isWaitingForDirectionRoll) sb.Append("isWaitingForDirectionRoll, ");
        if (isWaitingForDistanceRoll) sb.Append("isWaitingForDistanceRoll, ");
        if (isWaitingForDefGKChallengeDecision) sb.Append("isWaitingForDefGKChallengeDecision, ");
        if (gkRushedOut) sb.Append("gkRushedOut, ");
        if (didGKMoveInDefPhase) sb.Append("didGKMoveInDefPhase, ");
        if (currentTargetHex != null) sb.Append($"currentTargetHex: {currentTargetHex.coordinates}, ");
        if (lockedAttacker != null) sb.Append($"lockedAttacker: {lockedAttacker.name}, ");

        if (sb.Length >= 2 && sb[^2] == ',') sb.Length -= 2; // Trim trailing comma
        return sb.ToString();
    }

    public string GetInstructions()
    {
        StringBuilder sb = new();
        PlayerToken passer = MatchManager.Instance.LastTokenToTouchTheBallOnPurpose;
        string passerName = passer != null ? passer.name : "the passer";
        if (goalKeeperManager.isActivated) return "";
        if (finalThirdManager.isActivated) return "";
        if (freeKickManager.isWaitingForExecution) return "";
        if (isAvailable) sb.Append("Press [C] to Play a High Pass, ");
        if (isActivated) sb.Append("HP: ");
        if (isWaitingForConfirmation)
        {
            sb.Append($"Click on an inbounds Hex {minPassDistance}-{MAX_PASS_DISTANCE} Hexes from {passerName}, on or within 3 reachable Hexes of an attacker, ");
            if (MatchManager.Instance.difficulty_level == 3) sb.Append("this High Pass is already committed, ");
        }
        if (isWaitingForConfirmation && currentTargetHex != null) sb.Append($"or click the orange Hex again to confirm target, ");
        if (isWaitingForAttackerSelection && lockedAttacker == null) sb.Append($"Click an eligible attacker ({string.Join(", ", eligibleAttackers.Select(t => t.name))}) to move them to the target, ");
        if (isWaitingForAttackerSelection && lockedAttacker != null)
        {
            if (selectedToken == null) sb.Append($"Click on an Attacker (not {lockedAttacker.name}) to show the range, or Press [X] to forfeit Attacker HP movement, ");
            else sb.Append($"Click on highlighted Hex to move {selectedToken.name}, click another attacker to switch player, or Press [X] to forfeit Attacker HP movement, ");
        }
        if (isWaitingForDefenderSelection)
        {
            if (!isWaitingForDefenderMove) sb.Append($"Click on a Defender to show the moveable range, or Press [X] to forfeit Defender HP movement, ");
            else sb.Append($"Click on highlighted Hex to move {selectedToken.name}, click another defender to switch player, or Press [X] to forfeit Defender HP movement, ");
        }
        if (isWaitingForAccuracyRoll && passer != null) {sb.Append($"Press [R] to roll the accuracy check with {passer.name}, a roll of {8 - passer.highPass}+ is needed, ");}
        if (isWaitingForDirectionRoll) {sb.Append($"Press [R] to roll for Inacuracy Direction, ");}
        if (isWaitingForDistanceRoll) {sb.Append($"Press [R] to roll for Inacuracy Distance, ");}
        if (isWaitingForDefGKChallengeDecision) {sb.Append($"{defGK.name} can rush out to challenge, click a highlighted hex to rush there, or Press [X] to not rush out, ");}

        if (sb.Length >= 2 && sb[^2] == ',') sb.Length -= 2; // Trim trailing comma
        return sb.ToString();
    }

    public bool? IsInstructionExpectingHomeTeam()
    {
        if (MatchManager.Instance == null || (!isActivated && !isAvailable))
        {
            return null;
        }

        bool attackingTeamIsHome = MatchManager.Instance.teamInAttack == MatchManager.TeamInAttack.Home;
        if (!isActivated)
        {
            return attackingTeamIsHome;
        }

        if (isWaitingForDefenderSelection || isWaitingForDefenderMove || isWaitingForDefGKChallengeDecision)
        {
            return !attackingTeamIsHome;
        }

        return attackingTeamIsHome;
    }
}
