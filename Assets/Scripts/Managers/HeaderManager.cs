using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using UnityEngine;
using System.Threading.Tasks;
using UnityEditor.Experimental.GraphView;
using System.Data.Common;

public class HeaderManager : MonoBehaviour
{
    [Header("Dependencies")]
    public Ball ball;
    public HexGrid hexGrid;
    public HighPassManager highPassManager; // Reference to the HighPassManager
    public MovementPhaseManager movementPhaseManager; // Reference to the MovementPhaseManager
    public GroundBallManager groundBallManager;
    public GameInputManager gameInputManager;
    public LooseBallManager looseBallManager;
    public FinalThirdManager finalThirdManager;
    public GoalKeeperManager goalKeeperManager;
    public HelperFunctions helperFunctions;
    [Header("Header States")]
    public bool isAvailable = false;
    public bool isActivated = false;
    public bool hasEligibleAttackers = false;
    public bool hasEligibleDefenders = false;
    public bool isWaitingForAttackerSelection = false; // Flag to indicate waiting for attacker selection
    public bool isWaitingForDefenderSelection = false; // Flag to indicate waiting for attacker selection
    public bool isWaitingForHeaderRoll = false; // Flag to indicate waiting for header roll
    public bool isWaitingForSaveandHoldScenario = false;
    public bool iswaitingForChallengeWinnerSelection = false;
    public PlayerToken challengeWinner = null;
    public bool isWaitingForControlOrHeaderDecision = false;
    public bool isWaitingForControlOrHeaderDecisionDef = false;
    public bool isWaitingForControlRoll = false;
    public bool isWaitingForHeaderTargetSelection = false;
    public bool isWaitingForInterceptionRoll = false;
    public bool defenseWonFreeHeader = false;
    public bool defenseBallControl = false;
    public bool attackFreeHeader = false;
    public bool attackControlBall = false;
    public int threshold => attEligibleToHead.Count == 0 ? 1 : highPassManager.gkRushedOut ? 3 : 2 ;
    [Header("Lists")]
    public List<PlayerToken> attEligibleToHead = new List<PlayerToken>();
    public List<PlayerToken> defEligibleToHead = new List<PlayerToken>();
    public List<PlayerToken> attackerWillJump = new List<PlayerToken>();
    public List<PlayerToken> defenderWillJump = new List<PlayerToken>();
    public Dictionary<PlayerToken, (int roll, int totalScore)> tokenScores = new Dictionary<PlayerToken, (int, int)>();
    public PlayerToken tokenRolling;
    public HexCell tokenRollingHex => tokenRolling?.GetCurrentHex();
    public HexCell[] ballNeighbors => ball.GetCurrentHex().GetNeighbors(hexGrid);
    public bool hasHeadingPenalty => tokenRollingHex != ball.GetCurrentHex() && !ballNeighbors.Contains(tokenRollingHex);
    public string penaltyInfo => hasHeadingPenalty ? ", with penalty (-1)" : "";
    public string attordef => tokenRolling.isAttacker ? "Attacker" : "Defender";
    public int attribute => tokenRolling.IsGoalKeeper ? tokenRolling.aerial : tokenRolling.heading;
    public string attributelabel => tokenRolling.IsGoalKeeper ? "aerial ability" : "heading";
    [SerializeField]
    public List<HexCell> interceptingDefenders = new List<HexCell>();
    public PlayerToken interceptingDefender;
    public int interceptionDiceRoll;
    private const int HEADER_SELECTION_RANGE = 2;


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
        else
        {
            if (isWaitingForDefenderSelection)
            {
                if (token == null)
                {
                    Debug.LogWarning("You did not click on a token");
                }
                else
                {
                    if (token.isAttacker)
                    {
                        Debug.LogWarning($"{token.name} is not a defender! Rejecting input");
                    }
                    else if (!defEligibleToHead.Contains(token))
                    {
                        Debug.LogWarning($"{token.name} is not eligible to Head! Rejecting input");
                    }
                    else
                    {
                        HandleDefenderHeaderSelection(token);
                    }
                }
            }
            else if (isWaitingForAttackerSelection)
            {
                if (token == null)
                {
                    Debug.LogWarning("You did not click on a token");
                }
                else
                {
                    if (!token.isAttacker)
                    {
                        Debug.LogWarning($"{token.name} is not an Attacker! Rejecting input");
                    }
                    else if (!attEligibleToHead.Contains(token))
                    {
                        Debug.LogWarning($"{token.name} is not eligible to Head! Rejecting input");
                    }
                    else if (attackerWillJump.Contains(token))
                    {
                        Debug.LogWarning($"{token.name} has already declared to Jump for header. Rejecting input");
                        attackerWillJump.Remove(token);
                    }
                    else
                    {
                        HandleAttackerHeaderSelection(token);
                    }
                }

            }
            else if (isWaitingForHeaderTargetSelection)
            {
                if (hex == null)
                {
                    Debug.LogWarning("Invalid hex selected.");
                    return;
                }
                else if (hex.isDefenseOccupied)
                {
                    Debug.LogWarning("Please select a hex not occupied by the defense.");
                    return;
                }
                else if (hexGrid.highlightedHexes.Contains(hex))
                {
                    // TODO: Can I pass to a Jumped Token?
                    isWaitingForHeaderTargetSelection = false;
                    MoveHeaderToTargetSelection(hex);
                    return;
                }
            }
            else if (iswaitingForChallengeWinnerSelection)
            {
                DetermineChallengeWinner(token);
            }
        }
    }

    private void OnKeyReceived(KeyPressData keyData)
    {
        if (keyData.isConsumed) return;
        if (finalThirdManager.isActivated) return;
        if (goalKeeperManager.isActivated) return;
        if (!isActivated) return;
        else
        {
            if (isWaitingForInterceptionRoll && keyData.key == KeyCode.R)
            {
                keyData.isConsumed = true;
                PerformInterceptionRoll();
            }
            else if (isWaitingForDefenderSelection)
            {
                if (keyData.key == KeyCode.KeypadEnter)
                {
                    ConfirmDefenderHeaderSelection();
                }
                if (keyData.key == KeyCode.A && !hasEligibleAttackers)
                {
                    SelectAllAvailableDefenders();
                }
            }
            else if (isWaitingForAttackerSelection)
            {
                if (keyData.key == KeyCode.KeypadEnter)
                {
                    ConfirmAttackerHeaderSelection();
                }
                if (keyData.key == KeyCode.A)
                {
                    SelectAllAvailableAttackers();
                }
            }
            else if (isWaitingForControlOrHeaderDecision)
            {
                if (keyData.key == KeyCode.H)
                {
                    Debug.Log("Header option selected.");
                    attackFreeHeader = true;
                    StartAttackHeaderSelection();
                }
                else if (keyData.key == KeyCode.B)
                {
                    Debug.Log("Ball Control was chosen by Attack");
                    attackControlBall = true;
                    StartAttackControlSelection();
                }
            }
            else if (isWaitingForControlOrHeaderDecisionDef)
            {
                if (keyData.key == KeyCode.H)
                {
                    DefenseFreeHeader();
                }
                else if (keyData.key == KeyCode.B)
                {
                    DefenseContolBall();
                }
            }
            else if (isWaitingForSaveandHoldScenario)
            {
                if (keyData.key == KeyCode.Q)
                {
                    isWaitingForSaveandHoldScenario = false;
                    Debug.Log("QuickThrow Scenario chosen, NOBODY MOVES! Click Hex to select target for GK's throw");
                    MatchManager.Instance.currentState = MatchManager.GameState.QuickThrow;
                }
                else if (keyData.key == KeyCode.K)
                {
                    isWaitingForSaveandHoldScenario = false;  // Cancel the decision phase
                    Debug.Log("GK Decided to activate F3 Moves");
                    MatchManager.Instance.currentState = MatchManager.GameState.ActivateFinalThirdsAfterSave;
                    finalThirdManager.TriggerFinalThirdPhase(true);
                }
            }
            else if (isWaitingForHeaderRoll)
            {
                if (keyData.key == KeyCode.R)
                {
                    keyData.isConsumed = true;
                    PerformHeaderRoll();
                }
            }
            else if (isWaitingForControlRoll)
            {
                if (keyData.key == KeyCode.R)
                {
                    keyData.isConsumed = true;
                    PerformControlRoll();
                }
            }
        }
    }

    public IEnumerator FindEligibleHeaderTokens(HexCell landingHex)
    {
        isActivated = true;
        MatchManager.Instance.currentState = MatchManager.GameState.HeaderGeneric;
        hasEligibleAttackers = false;
        hasEligibleDefenders = false;
        if (highPassManager.gkRushedOut) 
        {
            defEligibleToHead.Add(hexGrid.GetDefendingGK());
            // highPassManager.gkRushedOut = false;
        }
        List<HexCell> nearbyHexes = HexGrid.GetHexesInRange(hexGrid, landingHex, HEADER_SELECTION_RANGE);

        foreach (HexCell hex in nearbyHexes)
        {
            PlayerToken token = hex.GetOccupyingToken();
            // TODO: Exclude kicker from the list of eligible attackers
            if (token != null)
            {
                Debug.Log($"Eligible to head: {token.name} at {hex.coordinates}");
                if (token.isAttacker)
                {
                    attEligibleToHead.Add(token);
                    hasEligibleAttackers = true;
                }
                else
                {
                    if (!defEligibleToHead.Contains(token)) defEligibleToHead.Add(token);
                    hasEligibleDefenders = true;
                }
            }
        }
        // Log the results for debugging
        Debug.Log($"hasEligibleAttackers: {hasEligibleAttackers}, hasEligibleDefenders: {hasEligibleDefenders}");

        if (!hasEligibleAttackers && !hasEligibleDefenders)
        {
            Debug.Log("No players eligible to head the ball. Ball drops to the ground.");
            movementPhaseManager.ResetMovementPhase();
            CleanUpHeader();
            movementPhaseManager.ActivateMovementPhase();
            movementPhaseManager.CommitToAction();
            // TODO: Log Appropriately
        }
        else if (!hasEligibleAttackers)
        {
            Debug.Log("No attackers eligible to head the ball. Defense wins the Possession automatically.");
            MatchManager.Instance.hangingPassType = null; // no chance of attack retaining possession
            // 🧤 If the GK rushed out, auto-select the GK
            if (highPassManager.gkRushedOut)
            {
                PlayerToken gk = hexGrid.GetDefendingGK();
                if (gk != null)
                {
                    if (!defenderWillJump.Contains(gk)) defenderWillJump.Add(gk); //maybe this is not needed (SetGKToRollLast)
                    Debug.Log($"🧤 GK {gk.name} auto-selected for the header.");
                }
                StartCoroutine(ResolveHeaderChallenge()); // TODO: Save and Hold without Resolve Header?
                yield break;
            }
            isWaitingForControlOrHeaderDecisionDef = true;
        }
        else if (!hasEligibleDefenders)
        {
            Debug.Log("No defenders eligible to head the ball. Offering option to header (H) or bring the ball down (B).");
            MatchManager.Instance.hangingPassType = null;
            isWaitingForControlOrHeaderDecision = true;
        }
        else
        {
            // Both attackers and defenders are eligible
            StartAttackHeaderSelection();
        }
    }

    private void DefenseFreeHeader()
    {
        Debug.Log("Header option selected from Defense");
        isWaitingForControlOrHeaderDecisionDef = false;
        defenseWonFreeHeader = true;
        if (defEligibleToHead.Count == 1)
        {
            defenderWillJump.Add(defEligibleToHead[0]);
            ConfirmDefenderHeaderSelection();
        }
        else iswaitingForChallengeWinnerSelection = true;
    }
    
    private void DefenseContolBall()
    {
        Debug.Log("Ball Control option selected from Defense");
        isWaitingForControlOrHeaderDecisionDef = false;
        defenseBallControl = true;
        if (defEligibleToHead.Count == 1)
        {
            challengeWinner = defEligibleToHead[0];
            HandleControlFlow();
        }
        else
        {
            iswaitingForChallengeWinnerSelection = true;
        }
    }

    private void DetermineChallengeWinner(PlayerToken token)
    {
        if (token == null)
        {
            Debug.LogWarning("No token was clicked");
            return;
        }
        else if(attackControlBall || attackFreeHeader)
        {
            if (!attEligibleToHead.Contains(token))
            {
                Debug.LogWarning($"{token} is not an attacker that can control or Head the HighPass");
            }
            else
            {
                if (attackFreeHeader)
                {
                    challengeWinner = token;
                    iswaitingForChallengeWinnerSelection = false;
                    StartCoroutine(ResolveHeaderChallenge());
                }
                else if (attackControlBall)
                {
                    challengeWinner = token;
                    iswaitingForChallengeWinnerSelection = false;
                    HandleControlFlow();
                }
                else
                {
                    Debug.LogError("Why are we here?");
                }
            }
        }
        else if(defenseBallControl || defenseWonFreeHeader)
        {
            if (!defEligibleToHead.Contains(token))
            {
                Debug.LogWarning($"{token} is not a defender that can control or Head the HighPass");
            }
            else
            {
                iswaitingForChallengeWinnerSelection = false;
                if (defenseWonFreeHeader)
                {
                    defenderWillJump.Add(token);
                    ConfirmDefenderHeaderSelection();
                }
                else if (defenseBallControl)
                {
                    MatchManager.Instance.ChangePossession();
                    challengeWinner = token;
                    HandleControlFlow();
                }
                else Debug.LogError("This should not happen");
            }
        }
        else
        {
            Debug.LogError("How did we end up here?");
        }
    }
    
    public void StartAttackControlSelection()
    {
        isWaitingForControlOrHeaderDecision = false;
        if (attackerWillJump.Count > 0)
        {
            // this means that Defense decided not to jump, after the attack declared a challenge
            if (attackerWillJump.Count == 1)
            {
                challengeWinner = attackerWillJump[0];
                Debug.Log($"Since {attackerWillJump[0].name} was the only one declared, they are autoselected to control the ball");
                HandleControlFlow();
            }
            else
            {
                Debug.Log($"More than one Attackers eligible to control the ball, Please select one of them");
                iswaitingForChallengeWinnerSelection = true;
            }
        }
        else
        {
            if (attEligibleToHead.Count == 1)
            {
                challengeWinner = attEligibleToHead[0];
                Debug.Log($"Since {attEligibleToHead[0].name} was the only one eligible, they are autoselected to control the ball");
                HandleControlFlow();
            }
            else
            {
                Debug.Log($"More than one Attackers eligible to control the ball, Please select one of them");
                iswaitingForChallengeWinnerSelection = true;
            }
        }
    }
    // Method to start the attacker's header selection
    public void StartAttackHeaderSelection()
    {
        // This case is called after defense's forfeit of aerial challenge
        // Since More than one attackers have declared that they want to jump
        // They will all jump and one will be selected for them 
        // to move forward and resolve the Free header
        if (isWaitingForControlOrHeaderDecision && attackerWillJump.Count > 0)
        {
            isWaitingForControlOrHeaderDecision = false;
            PlayerToken thomas = attackerWillJump
                  .OrderBy(token => HexGridUtils.GetHexDistance(
                      HexGridUtils.OffsetToCube(ball.GetCurrentHex().coordinates.x, ball.GetCurrentHex().coordinates.z)
                      , HexGridUtils.OffsetToCube(token.GetCurrentHex().coordinates.x, token.GetCurrentHex().coordinates.z))
                  )
                  // Closest token to the ball first
                  .ThenByDescending(token => token.heading)
                  // Break ties with the Heading
                  .First(); // random?
              challengeWinner = thomas;
              Debug.Log($"Closest Attacker or Best heading {challengeWinner.name} auto-selected for the header.");
              StartCoroutine(ResolveHeaderChallenge());
              return;
        }
        // This case is called when there are no eligible defenders
        // and Attackers have decided to Head
        if (attackFreeHeader && !hasEligibleDefenders && isWaitingForControlOrHeaderDecision)
        {
            if (attEligibleToHead.Count == 1)
            {
                attackerWillJump.Add(attEligibleToHead[0]);
                isWaitingForControlOrHeaderDecision = false;
                Debug.Log($"Automatically selected attacker: {attEligibleToHead[0].name}");
                StartCoroutine(ResolveHeaderChallenge());
                return;
            }
            else
            {
                Debug.Log($"More than one attackers are eligible to take the free Header, Please select 1");
                iswaitingForChallengeWinnerSelection = true;
                return;
            }
        }
        // This is a normally contested header with candidates from both
        if (hasEligibleAttackers && hasEligibleDefenders)
        {
            if (attEligibleToHead.Count == 1)
            {
                attackerWillJump.Add(attEligibleToHead[0]);
                Debug.Log($"Automatically selected attacker: {attEligibleToHead[0].name}");
                // Check if there are eligible defenders
                StartDefenseHeaderSelection();
                return;
            }
            else
            {
                Debug.Log($"Please 1-2 attackers to jump for the header. Press 'A' to select all available, or press 'X' to confirm selection.");
                isWaitingForAttackerSelection = true;
                return;
            }
        }
        // fallback
        Debug.LogError("Why are we here?");

    }

    public void HandleAttackerHeaderSelection(PlayerToken token)
    {
        if (attackerWillJump.Count < 2)
        {
            AddAttackerToHeaderSelection(token);
        }
    }

    public void AddAttackerToHeaderSelection(PlayerToken token)
    {
        if (!attackerWillJump.Contains(token))
        {
            attackerWillJump.Add(token);
            Debug.Log($"Attacker {token.name} selected to jump for the header.");
        }
    }

    public void ConfirmAttackerHeaderSelection()
    {
        if (attackerWillJump.Count > 0)
        {
            Debug.Log("Attack header selection confirmed.");
            isWaitingForAttackerSelection = false;
            // if (hasEligibleDefenders && !offeredControl)
            if (hasEligibleDefenders)
            {
                StartDefenseHeaderSelection();
            }
            else
            {
                StartCoroutine(ResolveHeaderChallenge());
            }
        }
        else
        {
            Debug.LogWarning("Attack cannot choose to not jump. Please select attackers and then press 'X' to confirm selection.");
        }
    }

    public void SelectAllAvailableAttackers()
    {
        if (attEligibleToHead.Count <= 2)
        {
            attackerWillJump.Clear();
            foreach (PlayerToken token in attEligibleToHead)
            {
                AddAttackerToHeaderSelection(token);
            }
            Debug.Log("All available attackers selected to jump for the header.");
            isWaitingForAttackerSelection = false;
            ConfirmAttackerHeaderSelection();
        }
        else
        {
            Debug.LogWarning("Too many attackers to select automatically. Please click on attackers to select them or press 'X' to confirm selection.");
        }
    }

    public void StartDefenseHeaderSelection()
    {
        defenderWillJump.Clear();
        if (defEligibleToHead.Count == 1)
        {
            defenderWillJump.Add(defEligibleToHead[0]);
            Debug.Log($"Single defender {defEligibleToHead[0].name} auto-selected for the header.");
            StartCoroutine(ResolveHeaderChallenge());
            return;
        }
        if (attEligibleToHead.Count == 0)
        {
            Debug.LogWarning("Is this ever called?");
            PlayerToken thomas = defEligibleToHead
                .OrderBy(token => HexGridUtils.GetHexDistance(
                    HexGridUtils.OffsetToCube(ball.GetCurrentHex().coordinates.x, ball.GetCurrentHex().coordinates.z)
                    , HexGridUtils.OffsetToCube(token.GetCurrentHex().coordinates.x, token.GetCurrentHex().coordinates.z))
                )
                // Closest token to the ball first
                .ThenByDescending(token => token.heading)
                // Break ties with the Heading
                .First(); // random?
            defenderWillJump.Add(thomas);
            challengeWinner = thomas;
            Debug.Log($"Closest defender or Best heading {challengeWinner.name} auto-selected for the header.");
            StartCoroutine(ResolveHeaderChallenge());
            return;
        }
        if (attEligibleToHead.Count != 0)
        {
            Debug.Log($"Please select 1-2 defenders to jump for the header. Press 'A' to select all available, and at any time press 'Enter' to confirm selection.");
            isWaitingForDefenderSelection = true;
        } 
    }

    // Coroutine for handling defender header selection
    public void HandleDefenderHeaderSelection(PlayerToken token)
    {
        if (!hasEligibleAttackers)
        {
            if (!defenderWillJump.Contains(token)) defenderWillJump.Add(token);
            challengeWinner = token;
            ConfirmDefenderHeaderSelection();
            return;
        }
        if (defenderWillJump.Contains(token))
        {
            Debug.LogWarning($"{token.name} has already declared to Jump for header. De-nominating token");
            // TODO: Maybe deselect?
            defenderWillJump.Remove(token);
            return;
        }
        if (defenderWillJump.Count < threshold)
        {
            defenderWillJump.Add(token);
            Debug.Log($"Defender {token.name} selected to jump for the header.");
        }
        else Debug.LogWarning("Defense Nominations are full, either deselect one, or press Enter to confirm");
    }

    public void ConfirmDefenderHeaderSelection()
    {
        Debug.Log("[Enter] was pressed. Defender header selection confirmed.");
        isWaitingForDefenderSelection = false;
        highPassManager.gkRushedOut = false;
        StartCoroutine(ResolveHeaderChallenge());
    }

    public void SelectAllAvailableDefenders()
    {
        if (defEligibleToHead.Count <= 2)
        {
            // defenderWillJump.Clear();
            foreach (PlayerToken token in defEligibleToHead)
            {
                defenderWillJump.Add(token);
            }
            Debug.Log("All available defenders selected to jump for the header.");
            ConfirmDefenderHeaderSelection();
        }
        else
        {
            Debug.LogWarning("Too many defenders to select automatically. Please click on defenders to select them or press 'X' to confirm selection.");
        }
    }

    public IEnumerator ResolveHeaderChallenge()
    {
        SetGKToRollLast(hexGrid.GetDefendingGK());
        // If both lists are empty, do nothing
        if (attackerWillJump.Count == 0 && defenderWillJump.Count == 0)
        {
            Debug.LogError("No attackers or defenders jumping. Ball drops to the ground. This should not happen.");
            yield break;
        }
        // **Scenario: Only Attackers are Jumping**
        if (attackerWillJump.Count > 0 && defenderWillJump.Count == 0)
        {
            MatchManager.Instance.hangingPassType = null;
            if (attackFreeHeader)
            {
                MatchManager.Instance.gameData.gameLog.LogEvent(MatchManager.Instance.LastTokenToTouchTheBallOnPurpose, MatchManager.ActionType.AerialPassCompleted);
                // MatchManager.Instance.SetLastToken(attackerWillJump[0]); // Pick the first attacker that was selected as the header winner and passer
                Debug.Log("Attackers win the header. Highlighting target hexes.");
                HighlightHexesForHeader(ball.GetCurrentHex(), 6);
                MatchManager.Instance.currentState = MatchManager.GameState.HeaderChallengeResolved;
                StartCoroutine(WaitForHeaderTargetSelection());
            }
            else
            {
                Debug.Log("No defenders wished to jump. We offer the option for a free header (H) or bring the ball down (B).");
                isWaitingForControlOrHeaderDecision = true;
            }
            
        }
        // **Scenario: Only Defenders are Jumping**
        if (attackerWillJump.Count == 0 && defenderWillJump.Count > 0)
        {
            Debug.Log("Only defenders are jumping. Defense wins the header automatically. Switching possession.");
            MatchManager.Instance.gameData.gameLog.LogEvent(
                defenderWillJump[0]
                , MatchManager.ActionType.BallRecovery
                , recoveryType: "freeheader"
                , connectedToken: MatchManager.Instance.LastTokenToTouchTheBallOnPurpose
            );
            MatchManager.Instance.SetLastToken(defenderWillJump[0]); // Add the first (and maybe only) Defender Token as the one that took the header.
            // TODO: Check if this is a Shot or a pass
            MatchManager.Instance.ChangePossession();
            // TODO: Check if this is the GK, to attract the ball and play save and hold
            // TODO: Check if this is is the GK, but the landing Hex is out of the box, so we continue with a header.
            MatchManager.Instance.gameData.gameLog.LogEvent(
                MatchManager.Instance.LastTokenToTouchTheBallOnPurpose
                , MatchManager.ActionType.PassAttempt
            );
            HighlightHexesForHeader(ball.GetCurrentHex(), 6);
            challengeWinner = defenderWillJump[0];
            StartCoroutine(WaitForHeaderTargetSelection());
        }
        // **Scenario: Both Attackers & Defenders are Jumping**
        if (attackerWillJump.Count > 0 && defenderWillJump.Count > 0)
        {
            // Both attackers and defenders are jumping
            Debug.Log("Header challenge started. Rolling for attackers and defenders.");
            // Dictionary<PlayerToken, (int roll, int totalScore)> tokenScores = new Dictionary<PlayerToken, (int, int)>();
            // Get the ball's current hex and its neighbors
            HexCell ballHex = ball.GetCurrentHex();
            HexCell[] ballNeighbors = ballHex.GetNeighbors(hexGrid);
            // Roll for attackers
            foreach (PlayerToken attacker in attackerWillJump)
            {
                tokenRolling = attacker;
                isWaitingForHeaderRoll = true;
                while (isWaitingForHeaderRoll) yield return null;
            }
            // Roll for defenders
            foreach (PlayerToken defender in defenderWillJump)
            {
                isWaitingForHeaderRoll = true;
                tokenRolling = defender;
                while (isWaitingForHeaderRoll) yield return null;
            }
            
            PlayerToken bestAttacker = attackerWillJump
                .OrderByDescending(token => tokenScores[token].totalScore) // Sort by total score
                .ThenByDescending(token => tokenScores[token].roll)        // Break ties with the roll
                .ThenBy(token => HexGridUtils.GetHexDistance(
                    HexGridUtils.OffsetToCube(ballHex.coordinates.x, ballHex.coordinates.z)
                    , HexGridUtils.OffsetToCube(token.GetCurrentHex().coordinates.x, token.GetCurrentHex().coordinates.z))
                ) // Closest token to the ball first
                .First(); // random?
            PlayerToken bestDefender = defenderWillJump
                .OrderByDescending(token => tokenScores[token].totalScore) // Highest total score first
                .ThenByDescending(token => token.IsGoalKeeper)             // Prefer goalkeepers (true before false)
                .ThenByDescending(token => tokenScores[token].roll)        // Break ties with the roll (higher roll first)
                .ThenBy(token => HexGridUtils.GetHexDistance(
                    HexGridUtils.OffsetToCube(ballHex.coordinates.x, ballHex.coordinates.z)
                    , HexGridUtils.OffsetToCube(token.GetCurrentHex().coordinates.x, token.GetCurrentHex().coordinates.z))
                ) // Closest token to the ball first
                .First(); // random?

            MatchManager.Instance.gameData.gameLog.LogEvent(bestAttacker, MatchManager.ActionType.AerialChallengeAttempt, connectedToken: bestDefender);

            int bestAttackerScore = tokenScores[bestAttacker].totalScore;
            int bestAttackerRoll = tokenScores[bestAttacker].roll;
            Debug.Log($"Best Attacker: {bestAttacker.name} with a score of {bestAttackerScore} (roll = {bestAttackerRoll})");
            int bestDefenderScore = tokenScores[bestDefender].totalScore;
            int bestDefenderRoll = tokenScores[bestDefender].roll;
            Debug.Log($"Best Defender: {bestDefender.name} with a score of {bestDefenderScore} (roll = {bestDefenderRoll})");

            if (bestAttackerScore > bestDefenderScore)
            {
                challengeWinner = bestAttacker;
                Debug.Log($"{bestAttacker.name} (Attack) wins the header. Highlighting target hexes.");
                HighlightHexesForHeader(ball.GetCurrentHex(), 6);
                MatchManager.Instance.gameData.gameLog.LogEvent(MatchManager.Instance.LastTokenToTouchTheBallOnPurpose, MatchManager.ActionType.AerialPassCompleted);
                MatchManager.Instance.gameData.gameLog.LogEvent(bestAttacker, MatchManager.ActionType.AerialChallengeWon, connectedToken: bestDefender);
                MatchManager.Instance.SetLastToken(bestAttacker);
                MatchManager.Instance.currentState = MatchManager.GameState.HeaderChallengeResolved;
                StartCoroutine(WaitForHeaderTargetSelection());
            }
            else if (bestDefenderScore > bestAttackerScore)
            {
                challengeWinner = bestDefender;
                MatchManager.Instance.gameData.gameLog.LogEvent(bestDefender, MatchManager.ActionType.AerialChallengeWon, connectedToken: bestAttacker);
                MatchManager.Instance.gameData.gameLog.LogEvent(bestDefender, MatchManager.ActionType.BallRecovery, recoveryType: "header", connectedToken: bestAttacker);
                MatchManager.Instance.SetLastToken(bestDefender);
                if (bestDefender.IsGoalKeeper) // TODO: in penalty area (their own)
                {
                    yield return StartCoroutine(groundBallManager.HandleGroundBallMovement(bestDefender.GetCurrentHex())); // Move the ball to the GK's Hex
                    MatchManager.Instance.ChangePossession();
                    yield return null;
                    Debug.Log($"GK {bestDefender.playerName} Wins the Aerial Challenge! Press [Q]uickThrow, or [K] to activate Final Thirds");
                    isWaitingForSaveandHoldScenario = true;
                    while (isWaitingForSaveandHoldScenario)
                    {
                        yield return null;  // Wait for the next frame
                    }
                }
                else
                {
                    Debug.Log($"{bestDefender.name} (Defense) wins the header. Switching possession. Click on a Highlighted Hex to play a Headed Pass.");
                    MatchManager.Instance.ChangePossession();
                    HighlightHexesForHeader(ball.GetCurrentHex(), 6);
                    StartCoroutine(WaitForHeaderTargetSelection());
                }
            }
            else if (bestDefenderScore == bestAttackerScore)
            {
                MatchManager.Instance.hangingPassType = "aerial";
                // MatchManager.Instance.gameData.gameLog.LogEvent(MatchManager.Instance.LastTokenToTouchTheBallOnPurpose, MatchManager.ActionType.AerialPassCompleted);
                StartCoroutine(looseBallManager.ResolveLooseBall(bestDefender, "header"));
                Debug.Log("Loose ball from header challenge.");
                CleanUpHeader();
            }
            else
            {
                Debug.LogError("What is going on here? Resolve header cannot decide what happened");
            }
        }
    }

    public void PerformHeaderRoll(int? rigRoll = null)
    {
        var (returnedRoll, returnedJackpot) = helperFunctions.DiceRoll();
        int roll = rigRoll ?? returnedRoll;
        int totalScore = rigRoll == null && returnedJackpot ? 50 : roll + tokenRolling.heading + (hasHeadingPenalty ? -1 : 0);
        // int totalScore = roll + attacker.heading + (hasHeadingPenalty ? -1 : 0);
        tokenScores[tokenRolling] = (roll, totalScore);
        if (returnedJackpot) Debug.Log($"{attordef} {tokenRolling.name} rolled A JACKPOT!!");
        else Debug.Log($"{attordef} {tokenRolling.name} rolled {roll} + {attributelabel}: {attribute}{penaltyInfo}= {totalScore}");
        isWaitingForHeaderRoll = false; // Proceed to the next token
    }

    public void SetGKToRollLast(PlayerToken gk)
    {
        if (defenderWillJump.Contains(gk))
        {
            Debug.Log($"{gk.name} is a goalkeeper, and will roll LAST for the defense");
            defenderWillJump.Remove(gk);
            defenderWillJump.Add(gk); // Moves to the last index
        }
    }

    private void HandleControlFlow()
    {
        HexCell ballHex = ball.GetCurrentHex();
        if (ballHex == null) Debug.LogWarning("ballHex is null");
        HexCell[] ballNeighbors = ballHex.GetNeighbors(hexGrid);
        if (ballNeighbors == null) Debug.LogWarning("ballNeighbors is null");
        bool hasHeadingPenalty = challengeWinner.GetCurrentHex() != ballHex && !ballNeighbors.Contains(challengeWinner.GetCurrentHex());
        string penaltyInfo = hasHeadingPenalty ? ", with penalty (-1)" : "";
        isWaitingForControlRoll = true;
        Debug.Log($"Press 'R' to attempt ball control: {challengeWinner.name} (dribbling: {challengeWinner.dribbling}{penaltyInfo}).");
    }

    public async void PerformControlRoll(int? rigRoll = null)
    {
        isWaitingForControlRoll = false;
        var (returnedRoll, returnedJackpot) = helperFunctions.DiceRoll();
        int roll = rigRoll ?? returnedRoll;
        int totalScore = roll + challengeWinner.dribbling + (hasHeadingPenalty ? -1 : 0);
        Debug.Log($"Attacker {challengeWinner.name} rolled {roll} + Dribbling {challengeWinner.dribbling}{penaltyInfo} = {totalScore}");
        if (totalScore >= 9)
        {
            Debug.Log($"You beauty! {challengeWinner.name} brings the ball down on their feet! Continue as if it were a SuccessfulTackle");
            await helperFunctions.StartCoroutineAndWait(groundBallManager.HandleGroundBallMovement(challengeWinner.GetCurrentHex()));
            MatchManager.Instance.gameData.gameLog.LogEvent(
                MatchManager.Instance.LastTokenToTouchTheBallOnPurpose
                , MatchManager.ActionType.AerialPassCompleted
            );
            MatchManager.Instance.SetLastToken(challengeWinner);
            MatchManager.Instance.BroadcastBallControl();
            CleanUpHeader();
        }
        else
        {
            Debug.Log($"{challengeWinner.name} failed to control the ball! Loose ball from {challengeWinner.name}");
            MatchManager.Instance.hangingPassType = "control";
            StartCoroutine(looseBallManager.ResolveLooseBall(challengeWinner, "ground"));
            CleanUpHeader();
        }
    }

    private void HighlightHexesForHeader(HexCell startHex, int range)
    {
        Debug.Log($"Highlighting hexes for header from {startHex.coordinates} with range {range}");
        List<HexCell> validHexes = HexGrid.GetHexesInRange(hexGrid, startHex, range)
            .Where(hex => !hex.isDefenseOccupied).ToList();

        foreach (HexCell hex in validHexes)
        {
            // TODO: Check if the hex is on a non jumped defender's ZOI
            hex.HighlightHex("ballPath");
            hexGrid.highlightedHexes.Add(hex);  // Track the highlighted hexes
        }
    }

    private IEnumerator WaitForHeaderTargetSelection()
    {
        isWaitingForHeaderTargetSelection = true;
        while (isWaitingForHeaderTargetSelection)
        {
            yield return null;
        }
    }

    private async void MoveHeaderToTargetSelection(HexCell clickedHex)
    {
        hexGrid.ClearHighlightedHexes();
        MatchManager.Instance.gameData.gameLog.LogEvent(
            MatchManager.Instance.LastTokenToTouchTheBallOnPurpose
            , MatchManager.ActionType.PassAttempt
        );
        await helperFunctions.StartCoroutineAndWait(ball.MoveToCell(clickedHex));
        Debug.Log($"Ball moved to {clickedHex.coordinates}");
        MatchManager.Instance.UpdatePossessionAfterPass(clickedHex);
        if (clickedHex.isAttackOccupied)
        {
            MatchManager.Instance.BroadcastHeaderCompleted();
            MatchManager.Instance.gameData.gameLog.LogEvent(
                MatchManager.Instance.LastTokenToTouchTheBallOnPurpose
                , MatchManager.ActionType.PassCompleted
            );
            MatchManager.Instance.SetLastToken(clickedHex.GetOccupyingToken());
            ball.AdjustBallHeightBasedOnOccupancy();
            finalThirdManager.TriggerFinalThirdPhase();
            CleanUpHeader();
        }
        else
        {
            MatchManager.Instance.hangingPassType = "ground";
            CheckForHeaderInterception(clickedHex);
        }
    }

    private void CheckForHeaderInterception(HexCell landingHex)
    {
        // Get all defenders and their ZOIs (neighbors)
        List<HexCell> defenderHexes = hexGrid.GetDefenderHexes();
        List<HexCell> filteredDefenderHexes = defenderHexes
            .Where(hex => !defenderWillJump.Any(defender => defender.GetCurrentHex() == hex))
            .ToList();
        List<HexCell> defenderNeighbors = hexGrid.GetDefenderNeighbors(filteredDefenderHexes);
        // Initialize the interceptingDefenders list to avoid null reference
        interceptingDefenders = new List<HexCell>();

        // Check if the landing hex is in any defender's ZOI (neighbors)
        if (defenderNeighbors.Contains(landingHex))
        {
            // Log for debugging: Confirm the landing hex and defender neighbors
            Debug.Log($"Landing hex {landingHex.coordinates} is in defender ZOI. Checking eligible defenders...");

            // Get defenders who have the landing hex in their ZOI
            foreach (HexCell defender in filteredDefenderHexes)
            {
                HexCell[] neighbors = defender.GetNeighbors(hexGrid);
                // Debug.Log($"Defender at {defender.coordinates} has neighbors: {string.Join(", ", neighbors.Select(n => n?.coordinates.ToString() ?? "null"))}");

                if (neighbors.Contains(landingHex))
                {
                    Debug.Log($"Defender at {defender.coordinates} can intercept at {landingHex.coordinates}");
                    interceptingDefenders.Add(defender);  // Add the eligible defender to the list
                }
            }
            // Check if there are any intercepting defenders
            if (interceptingDefenders.Count > 0)
            {
                Debug.Log($"Found {interceptingDefenders.Count} defender(s) eligible for interception. Please Press R key..");
                StartCoroutine(PerformInterceptionCheck());
            }
            else
            {
                Debug.LogError("No defenders eligible for interception. Header goes without interception. Should not appear");
            }
        }
        else
        {
            Debug.Log("Landing hex is not in any defender's ZOI. No interception needed.");
            CleanUpHeader();
            finalThirdManager.TriggerFinalThirdPhase();
            MatchManager.Instance.hangingPassType = "ground";
            MatchManager.Instance.BroadcastHeaderCompleted();
        }
    }

    public void PerformInterceptionRoll(int? rigRoll = null)
    {
        isWaitingForInterceptionRoll = false;
        var (returnedRoll, returnedJackpot) = helperFunctions.DiceRoll();
        interceptionDiceRoll = rigRoll ?? returnedRoll;
    }

    private IEnumerator PerformInterceptionCheck()
    {
        if (interceptingDefenders == null || interceptingDefenders.Count == 0)
        {
            Debug.Log("No defenders available for interception.");
            finalThirdManager.TriggerFinalThirdPhase();
            yield break;
        }

        foreach (HexCell defenderHex in interceptingDefenders)
        {
            isWaitingForInterceptionRoll = true;
            interceptingDefender = defenderHex.GetOccupyingToken();
            if (interceptingDefender == null)
            {
                Debug.LogWarning($"No valid token found at defender's hex {defenderHex.coordinates}.");
                continue;
            }
            while (isWaitingForInterceptionRoll) yield return null;

            Debug.Log($"Checking interception for defender at {defenderHex.coordinates}");
            Debug.Log($"Dice roll for defender {interceptingDefender.name} at {defenderHex.coordinates}: {interceptionDiceRoll}");
            int totalInterceptionScore = interceptionDiceRoll + interceptingDefender.tackling;
            Debug.Log($"Total interception score for defender {interceptingDefender.name}: {totalInterceptionScore}");
            MatchManager.Instance.gameData.gameLog.LogEvent(interceptingDefender, MatchManager.ActionType.InterceptionAttempt);

            if (interceptionDiceRoll == 6 || totalInterceptionScore >= 10)
            {
                Debug.Log($"Defender at {defenderHex.coordinates} successfully intercepted the ball!");
                MatchManager.Instance.gameData.gameLog.LogEvent(
                    interceptingDefender
                    , MatchManager.ActionType.InterceptionSuccess
                    , recoveryType: "headedpass"
                    , connectedToken: MatchManager.Instance.LastTokenToTouchTheBallOnPurpose
                );
                MatchManager.Instance.SetLastToken(interceptingDefender);
                // Move the ball to the defender's hex and change possession
                yield return StartCoroutine(ball.MoveToCell(defenderHex));
                MatchManager.Instance.ChangePossession();
                MatchManager.Instance.UpdatePossessionAfterPass(defenderHex);
                // ball.AdjustBallHeightBasedOnOccupancy();
                ball.PlaceAtCell(defenderHex);
                MatchManager.Instance.BroadcastAnyOtherScenario();
                finalThirdManager.TriggerFinalThirdPhase();
                yield break;  // Stop the sequence once an interception is successful
            }
            else
            {
                Debug.Log($"Defender at {defenderHex.coordinates} failed to intercept the ball.");
            }
        }

        // If no defender intercepts, the ball stays at the original hex
        Debug.Log("All defenders failed to intercept. Ball remains at the landing hex.");
        CleanUpHeader();
        finalThirdManager.TriggerFinalThirdPhase();
        MatchManager.Instance.hangingPassType = "ground";
        MatchManager.Instance.BroadcastHeaderCompleted();
    }

    private void CleanUpHeader()
    {
      attEligibleToHead.Clear();
      defEligibleToHead.Clear();
      hasEligibleAttackers = false;
      hasEligibleDefenders = false;
      isWaitingForControlOrHeaderDecision = false;
      isWaitingForControlOrHeaderDecisionDef = false;
      challengeWinner = null;
      tokenRolling = null;
      tokenScores = new Dictionary<PlayerToken, (int, int)>();
      interceptingDefenders.Clear();
      isActivated = false;
      interceptingDefender = null;
      interceptionDiceRoll = 0;
      defenseWonFreeHeader = false;
      defenseBallControl = false;
      attackFreeHeader = false;
      attackControlBall = false;
    }
    
    public void ResetHeader()
    {
      attEligibleToHead.Clear();
      defEligibleToHead.Clear();
      attackerWillJump.Clear();
      defenderWillJump.Clear();
      hasEligibleAttackers = false;
      hasEligibleDefenders = false;
    }

    public string GetDebugStatus()
    {
        StringBuilder sb = new();
        sb.Append("Head: ");

        if (isActivated) sb.Append("isActivated, ");
        if (isAvailable) sb.Append("isAvailable, ");
        if (hasEligibleAttackers) sb.Append($"attEligibleToHead ({string.Join(", ", attEligibleToHead.Select(t => t.name))}), ");
        if (hasEligibleDefenders) sb.Append($"defEligibleToHead ({string.Join(", ", defEligibleToHead.Select(t => t.name))}), ");
        if (isWaitingForAttackerSelection) sb.Append($"isWaitingForAttackerSelection ({string.Join(", ", attackerWillJump.Select(t => t.name))}), ");
        if (isWaitingForDefenderSelection) sb.Append($"isWaitingForDefenderSelection ({string.Join(", ", defenderWillJump.Select(t => t.name))}), ");
        if (isWaitingForHeaderRoll) sb.Append($"isWaitingForHeaderRoll, {tokenRolling.name} ");
        if (isWaitingForSaveandHoldScenario) sb.Append("isWaitingForSaveandHoldScenario, ");
        if (iswaitingForChallengeWinnerSelection) sb.Append("iswaitingForChallengeWinnerSelection, ");
        if (isWaitingForControlOrHeaderDecision) sb.Append("isWaitingForControlOrHeaderDecision, ");
        if (isWaitingForControlOrHeaderDecisionDef) sb.Append("isWaitingForControlOrHeaderDecisionDef, ");
        if (isWaitingForHeaderTargetSelection) sb.Append("isWaitingForHeaderTargetSelection, ");
        if (isWaitingForInterceptionRoll) sb.Append($"isWaitingForInterceptionRoll, {interceptingDefender} ");
        if (challengeWinner != null) sb.Append($"challengeWinner: {challengeWinner.playerName}, ");


        if (sb.Length >= 2 && sb[^2] == ',') sb.Length -= 2; // Trim trailing comma
        return sb.ToString();
    }


    public string GetInstructions()
    {
        StringBuilder sb = new();
        if (goalKeeperManager.isActivated) return "";
        if (finalThirdManager.isActivated) return "";
        if (isActivated) sb.Append("Head: ");
        if (isWaitingForAttackerSelection)
        {
            sb.Append($"Please 1-2 attackers among {string.Join(", ", attEligibleToHead.Select(t => t.name))} to jump for the header, ");
            if (attEligibleToHead.Count <= 2) sb.Append($"Press [A] to select all available and confirm, ");
            if (attackerWillJump.Count == 0) sb.Append($"Attack must choose a Token to jump, ");
            else sb.Append($"Press [Enter] to confirm current selection: {string.Join(", ", attackerWillJump.Select(t => t.name))}, ");
        }
        if (isWaitingForDefenderSelection)
        {
            sb.Append($"Please 1-2 attackers among {string.Join(", ", defEligibleToHead.Select(t => t.name))} to jump for the header, ");
            if (defEligibleToHead.Count <= 2) sb.Append($"Press [A] to select all available and confirm, ");
            if (defenderWillJump.Count == 0) sb.Append($"Press [Enter] to not challenge with anyone, ");
            else 
            {
              sb.Append($"Press [Enter] to confirm current selection: {string.Join(", ", defenderWillJump.Select(t => t.name))}, ");
              if (defenderWillJump.Count == threshold) sb.Append($"or deselect a nominated defender to nominate another, ");
            }
        }
        if (isWaitingForHeaderRoll) sb.Append($"Press 'R' to roll for {attordef}: {tokenRolling.name} ({attributelabel}: {tokenRolling.heading}{penaltyInfo})., ");
        if (isWaitingForHeaderTargetSelection) sb.Append($"{challengeWinner.name} won the header, Click on a Highlighted Hex to head the ball, ");
        if (isWaitingForInterceptionRoll) sb.Append($"Press [R] to roll for an interception with {interceptingDefender.name} (tackling: {interceptingDefender.tackling}), ");
        if (iswaitingForChallengeWinnerSelection) sb.Append($"Click on a Token to take the free Header or Control, ");
        if (isWaitingForControlOrHeaderDecision || isWaitingForControlOrHeaderDecisionDef) sb.Append($"Press [H] to take a free Header or [B] to attempt a ball control, ");
        if (isWaitingForControlRoll) sb.Append($"Press [R] to roll with {challengeWinner.name} to attempt to control the Ball, ");

        if (sb.Length >= 2 && sb[^2] == ',') sb.Length -= 2; // Trim trailing comma
        return sb.ToString();
    }
}
