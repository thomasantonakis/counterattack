using System.Collections;
using System.Collections.Generic;
using System.Linq;
// using System.Text.RegularExpressions;
using System.Text;
using UnityEngine;
using System.Threading.Tasks;

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
    public GoalFlowManager goalFlowManager;
    public LongBallManager longBallManager;
    public ShotManager shotManager;
    public OutOfBoundsManager outOfBoundsManager;
    public HelperFunctions helperFunctions;
    [Header("Header States")]
    public bool isAvailable = false;
    public bool isActivated = false;
    public bool hasEligibleAttackers = false;
    public bool hasEligibleDefenders = false;
    public bool isWaitingForAttackerSelection = false; // Flag to indicate waiting for attacker selection
    public bool isWaitingForHeaderAtGoal = false; // Flag to indicate waiting for attacker selection
    public bool headerAtGoalDeclared = false; // Flag to indicate waiting for attacker selection
    public HexCell headerAtGoalTarget = null; // Flag to indicate waiting for attacker selection
    public bool isWaitingForDefenderSelection = false; // Flag to indicate waiting for attacker selection
    public bool isWaitingForHeaderRoll = false; // Flag to indicate waiting for header roll
    // public bool isWaitingForSaveandHoldScenario = false;
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
    // public int threshold => attEligibleToHead.Count == 0 ? 1 : highPassManager.gkRushedOut ? 3 : 2 ;
    public int threshold => attEligibleToHead.Count == 0 ? 1 : GetMaxDefenderHeaderNominations();
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
    private readonly Dictionary<HexCell, bool> headerTargetThreatByHex = new();
    private HexCell hoveredHeaderTargetHex;
    [Header("Tuning")]
    public bool allowUnchallengedDefenseControl = true;
    private const int HEADER_SELECTION_RANGE = 2;
    private const int MAX_OUTFIELD_HEADER_DEFENDERS = 2;


    private void OnEnable()
    {
        GameInputManager.OnClick += OnClickReceived;
        GameInputManager.OnHover += OnHoverReceived;
        GameInputManager.OnKeyPress += OnKeyReceived;
    }

    private void OnDisable()
    {
        GameInputManager.OnClick -= OnClickReceived;
        GameInputManager.OnHover -= OnHoverReceived;
        GameInputManager.OnKeyPress -= OnKeyReceived;
        hoveredHeaderTargetHex = null;
        headerTargetThreatByHex.Clear();
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
                  Debug.LogWarning($"{token.name} has already declared to Jump for header. De-nominating token");
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
            if (isWaitingForHeaderAtGoal)
            {
                HandleHeaderAtGoalClick(hex);
            }
        }
    }

    private void OnHoverReceived(PlayerToken token, HexCell hex)
    {
        if (!isActivated || !isWaitingForHeaderTargetSelection)
        {
            if (hoveredHeaderTargetHex != null)
            {
                hoveredHeaderTargetHex = null;
                RefreshHeaderTargetHighlights();
            }

            return;
        }

        HexCell targetHex = hexGrid.highlightedHexes.Contains(hex) ? hex : null;
        if (hoveredHeaderTargetHex == targetHex)
        {
            return;
        }

        hoveredHeaderTargetHex = targetHex;
        RefreshHeaderTargetHighlights();
    }

    private void OnKeyReceived(KeyPressData keyData)
    {
        if (keyData.isConsumed) return;
        if (finalThirdManager.isActivated) return;
        if (goalKeeperManager.isActivated) return;
        if (!isActivated) return;
        else
        {
            bool hasRollOverride = RollInputOverride.TryParse(keyData, out RollInputOverride rollOverride);
            if (isWaitingForInterceptionRoll && (keyData.key == KeyCode.R || hasRollOverride))
            {
                keyData.isConsumed = true;
                PerformInterceptionRoll(hasRollOverride ? (RollInputOverride?)rollOverride : null);
            }
            else if (isWaitingForDefenderSelection)
            {
                if (IsHeaderSelectionConfirmKey(keyData.key))
                {
                    keyData.isConsumed = true;
                    ConfirmDefenderHeaderSelection();
                }
                if (keyData.key == KeyCode.A)
                {
                    keyData.isConsumed = true;
                    SelectAllAvailableDefenders();
                }
            }
            else if (isWaitingForAttackerSelection)
            {
                if (IsHeaderSelectionConfirmKey(keyData.key))
                {
                    keyData.isConsumed = true;
                    ConfirmAttackerHeaderSelection();
                }
                if (keyData.key == KeyCode.A)
                {
                    keyData.isConsumed = true;
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
            // else if (isWaitingForSaveandHoldScenario)
            // {
            //     if (keyData.key == KeyCode.Q)
            //     {
            //         isWaitingForSaveandHoldScenario = false;
            //         Debug.Log("QuickThrow Scenario chosen, NOBODY MOVES! Click Hex to select target for GK's throw");
            //         MatchManager.Instance.currentState = MatchManager.GameState.QuickThrow;
            //     }
            //     else if (keyData.key == KeyCode.K)
            //     {
            //         isWaitingForSaveandHoldScenario = false;  // Cancel the decision phase
            //         Debug.Log("GK Decided to activate F3 Moves");
            //         MatchManager.Instance.currentState = MatchManager.GameState.ActivateFinalThirdsAfterSave;
            //         finalThirdManager.TriggerFinalThirdPhase(true);
            //     }
            // }
            else if (isWaitingForHeaderRoll)
            {
                if (keyData.key == KeyCode.R || hasRollOverride)
                {
                    keyData.isConsumed = true;
                    PerformHeaderRoll(hasRollOverride ? (RollInputOverride?)rollOverride : null);
                }
            }
            else if (isWaitingForControlRoll)
            {
                if (keyData.key == KeyCode.R || hasRollOverride)
                {
                    keyData.isConsumed = true;
                    _ = PerformControlRoll(hasRollOverride ? (RollInputOverride?)rollOverride : null); // No need to await this here
                }
            }
            else if (isWaitingForHeaderAtGoal)
            {
                if (keyData.key == KeyCode.H)
                {
                    keyData.isConsumed = true;
                    isWaitingForHeaderAtGoal = false;
                    headerAtGoalDeclared = false;
                }
            }
        }
    }

    private static bool IsHeaderSelectionConfirmKey(KeyCode key)
    {
        return key == KeyCode.Return || key == KeyCode.KeypadEnter;
    }

    public IEnumerator FindEligibleHeaderTokens(HexCell landingHex)
    {
        while (finalThirdManager.isActivated) yield return null;
        isActivated = true;
        MatchManager.Instance.currentState = MatchManager.GameState.HeaderGeneric;
        hasEligibleAttackers = false;
        hasEligibleDefenders = false;
        if (highPassManager.gkRushedOut) 
        {
            PlayerToken gk = hexGrid.GetDefendingGK();
            if (gk != null && !defEligibleToHead.Contains(gk))
            {
                defEligibleToHead.Add(gk);
                hasEligibleDefenders = true;
            }
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
            MatchManager.Instance.ClearHangingPass(); // no chance of attack retaining possession
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
            if (allowUnchallengedDefenseControl)
            {
                isWaitingForControlOrHeaderDecisionDef = true;
            }
            else
            {
                DefenseFreeHeader();
            }
        }
        else if (!hasEligibleDefenders)
        {
            Debug.Log("No defenders eligible to head the ball. Offering option to header (H) or bring the ball down (B).");
            MatchManager.Instance.ClearHangingPass();
            isWaitingForControlOrHeaderDecision = true;
        }
        else
        {
            // Both attackers and defenders are eligible
            _ = StartAttackHeaderSelection(); // No need to await this
        }
    }

    private void HandleHeaderAtGoalClick(HexCell hexcell)
    {
        if (hexcell.isInGoal == 0) return;
        else
        {
          headerAtGoalTarget = hexcell;
          headerAtGoalDeclared = true;
          isWaitingForHeaderAtGoal = false;
          hexGrid.ClearHighlightedHexes();
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
        else
        {
            iswaitingForChallengeWinnerSelection = true;
            HighlightCandidateTokenHexes(GetChallengeWinnerCandidates());
        }
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
            HighlightCandidateTokenHexes(GetChallengeWinnerCandidates());
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
                    attackerWillJump.Clear();
                    attackerWillJump.Add(token);
                    challengeWinner = token;
                    iswaitingForChallengeWinnerSelection = false;
                    hexGrid.ClearHighlightedHexes();
                    StartCoroutine(ResolveHeaderChallenge());
                }
                else if (attackControlBall)
                {
                    challengeWinner = token;
                    iswaitingForChallengeWinnerSelection = false;
                    hexGrid.ClearHighlightedHexes();
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
                hexGrid.ClearHighlightedHexes();
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
                HighlightCandidateTokenHexes(GetChallengeWinnerCandidates());
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
                HighlightCandidateTokenHexes(GetChallengeWinnerCandidates());
            }
        }
    }
    
    // Method to start the attacker's header selection
    public async Task StartAttackHeaderSelection()
    {
        // This case can be reached after defense declines an aerial challenge.
        // Clear previous contested nominations so the unchallenged choice starts fresh.
        if (isWaitingForControlOrHeaderDecision && attackerWillJump.Count > 0)
        {
            attackerWillJump.Clear();
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
                await helperFunctions.StartCoroutineAndWait(WaitForHeadAtGoalDecision());
                StartCoroutine(ResolveHeaderChallenge());
                return;
            }
            else
            {
                isWaitingForControlOrHeaderDecision = false;
                Debug.Log($"More than one attackers are eligible to take the free Header. Please select exactly one attacker.");
                iswaitingForChallengeWinnerSelection = true;
                HighlightCandidateTokenHexes(attEligibleToHead);
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
                await helperFunctions.StartCoroutineAndWait(WaitForHeadAtGoalDecision());
                // Check if there are eligible defenders
                StartDefenseHeaderSelection();
                return;
            }
            else
            {
                Debug.Log($"Please 1-2 attackers to jump for the header. Press 'A' to select all available, or press 'Enter' to confirm selection.");
                isWaitingForAttackerSelection = true;
                HighlightCandidateTokenHexes(attEligibleToHead);
                return;
            }
        }
        // fallback
        Debug.LogError("Why are we here?");
    }

    private IEnumerator WaitForHeadAtGoalDecision()
    {
        if (!IsHeaderAtGoalAvailable())
        {
            yield break;
        }
        else
        {
          isWaitingForHeaderAtGoal = true;
          LiftHexesInGoal();
          while (isWaitingForHeaderAtGoal)
          {
            yield return null;
          }
        }
    }
    
    public void LiftHexesInGoal()
    {
        Debug.Log("Highlighting CanShootTo hexes for target selection.");
        HexCell shooterHex = ball.GetCurrentHex();
        Dictionary<HexCell, List<HexCell>> shootingPaths = shooterHex.HeadingPaths;

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
    }

    private bool IsHeaderAtGoalAvailable()
    {
        bool shouldShotBeAvailable = false;
        MatchManager.TeamAttackingDirection attackingDirection;
        if (MatchManager.Instance.teamInAttack == MatchManager.TeamInAttack.Home)
        {
            attackingDirection = MatchManager.Instance.homeTeamDirection;
        }
        else
        {
            attackingDirection = MatchManager.Instance.awayTeamDirection;
        }
        // Debug.Log($"attackingDirection: {attackingDirection}, CanHeadfrom: {ball.GetCurrentHex().CanHeadFrom}, side: {ball.GetCurrentHex().coordinates.x}");
        if (
            (
                attackingDirection == MatchManager.TeamAttackingDirection.LeftToRight // Attackers shoot to the Right
                && ball.GetCurrentHex().CanHeadFrom // Is in heading distance
                && ball.GetCurrentHex().coordinates.x > 0 // In Right Side of Pitch
            )
            ||
            (
                attackingDirection == MatchManager.TeamAttackingDirection.RightToLeft // Attackers shoot to the Left
                && ball.GetCurrentHex().CanHeadFrom // Is in heading distance
                && ball.GetCurrentHex().coordinates.x < 0 // In Left Side of Pitch
            )
        )
        {
          shouldShotBeAvailable = true;
        }
        return shouldShotBeAvailable;
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

    public async Task ConfirmAttackerHeaderSelection()
    {
        if (attackerWillJump.Count > 0)
        {
            Debug.Log("Attack header selection confirmed.");
            isWaitingForAttackerSelection = false;
            hexGrid.ClearHighlightedHexes();
            await helperFunctions.StartCoroutineAndWait(WaitForHeadAtGoalDecision());
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
            // await Task.Delay(0);
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
        // defenderWillJump.Clear();
        // if (defEligibleToHead.Count == 1)
        // {
        //     defenderWillJump.Add(defEligibleToHead[0]);
        //     Debug.Log($"Single defender {defEligibleToHead[0].name} auto-selected for the header.");
        //     StartCoroutine(ResolveHeaderChallenge());
        //     return;
        // }
        if (attEligibleToHead.Count == 0)
        {
            Debug.LogWarning("Is this ever called?");
            PlayerToken thomas = defEligibleToHead
                .OrderBy(token => HexGridUtils.GetHexStepDistance(ball.GetCurrentHex(), token.GetCurrentHex()))
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
            Debug.Log("Please select up to two outfield defenders plus the defending GK if eligible. Press 'A' to select all available when allowed, and at any time press 'Enter' to confirm selection.");
            isWaitingForDefenderSelection = true;
            HighlightCandidateTokenHexes(defEligibleToHead);
        } 
    }

    private List<PlayerToken> GetChallengeWinnerCandidates()
    {
        if (attackControlBall || attackFreeHeader)
        {
            return attackerWillJump.Count > 0 ? attackerWillJump : attEligibleToHead;
        }

        if (defenseBallControl || defenseWonFreeHeader)
        {
            return defEligibleToHead;
        }

        return new List<PlayerToken>();
    }

    private void HighlightCandidateTokenHexes(IEnumerable<PlayerToken> candidates)
    {
        hexGrid.ClearHighlightedHexes();
        foreach (PlayerToken candidate in candidates.Where(token => token != null))
        {
            HexCell candidateHex = candidate.GetCurrentHex();
            if (candidateHex == null) continue;

            candidateHex.HighlightHex("ReachOverlayAttacker");
            if (!hexGrid.highlightedHexes.Contains(candidateHex))
            {
                hexGrid.highlightedHexes.Add(candidateHex);
            }
        }
    }

    private string FormatTokenCandidateList(IEnumerable<PlayerToken> candidates)
    {
        return string.Join(", ", candidates
            .Where(token => token != null)
            .Select(token => $"{token.jerseyNumber}. {token.playerName}"));
    }

    private string GetDefenderHeaderSelectionInstruction()
    {
        List<PlayerToken> outfieldDefenders = GetEligibleOutfieldHeaderDefenders().ToList();
        PlayerToken eligibleGoalkeeper = GetEligibleDefendingGoalkeeperForHeader();
        int maxOutfield = GetMaxOutfieldHeaderDefenderNominations();
        bool goalkeeperAlreadySelected = eligibleGoalkeeper != null && defenderWillJump.Contains(eligibleGoalkeeper);

        StringBuilder sb = new();
        if (goalkeeperAlreadySelected)
        {
            sb.Append($"{eligibleGoalkeeper.name} is already challenging as GK. ");
            sb.Append($"Please select 0-{maxOutfield} outfield defenders");
        }
        else
        {
            sb.Append($"Please select up to {maxOutfield} outfield defenders");
            if (eligibleGoalkeeper != null)
            {
                sb.Append($" plus optional GK {eligibleGoalkeeper.name}");
            }
        }

        if (outfieldDefenders.Count > 0)
        {
            sb.Append($" among {string.Join(", ", outfieldDefenders.Select(t => t.name))}");
        }

        sb.Append(" to jump for the header, ");
        if (outfieldDefenders.Count <= MAX_OUTFIELD_HEADER_DEFENDERS)
        {
            sb.Append("Press [A] to select all available and confirm, ");
        }
        if (defenderWillJump.Count == 0)
        {
            sb.Append("Press [Enter] to not challenge with anyone, ");
        }
        else
        {
            sb.Append($"Press [Enter] to confirm current selection: {string.Join(", ", defenderWillJump.Select(t => t.name))}, ");
            bool outfieldFull = GetSelectedOutfieldHeaderDefenderCount() == maxOutfield;
            bool goalkeeperAvailable = eligibleGoalkeeper != null && !goalkeeperAlreadySelected;
            if (outfieldFull && !goalkeeperAvailable)
            {
                sb.Append("or deselect a nominated outfield defender to nominate another, ");
            }
            else if (outfieldFull)
            {
                sb.Append("or select the GK too, ");
            }
        }

        return sb.ToString();
    }

    private string GetChallengeWinnerInstruction()
    {
        string actionLabel = attackControlBall || defenseBallControl
            ? "attempt the ball control"
            : "take the free Header";

        return $"Click on a Token among: ({FormatTokenCandidateList(GetChallengeWinnerCandidates())}) to {actionLabel}, ";
    }

    private string GetTokenSortName(PlayerToken token)
    {
        if (token == null) return "";
        return string.IsNullOrWhiteSpace(token.playerName) ? token.name : token.playerName;
    }

    private int GetHeaderAttribute(PlayerToken token)
    {
        if (token == null) return 0;
        return token.IsGoalKeeper ? token.aerial : token.heading;
    }

    private int GetEffectiveHeaderAttribute(PlayerToken token)
    {
        if (token == null) return 0;
        HexCell ballHex = ball.GetCurrentHex();
        HexCell tokenHex = token.GetCurrentHex();
        int penalty = 0;
        if (ballHex != null && tokenHex != null && tokenHex != ballHex && !ballHex.GetNeighbors(hexGrid).Contains(tokenHex))
        {
            penalty = -1;
        }

        return GetHeaderAttribute(token) + penalty;
    }

    private ExpectedStatsCalculator.AerialContestant[] BuildAerialContestants()
    {
        return attackerWillJump
            .Concat(defenderWillJump)
            .Where(token => token != null)
            .Distinct()
            .Select(token => new ExpectedStatsCalculator.AerialContestant(token, GetEffectiveHeaderAttribute(token)))
            .ToArray();
    }

    private void LogExpectedHeaderRecoveriesForDefenders()
    {
        ExpectedStatsCalculator.AerialContestant[] contestants = BuildAerialContestants();
        foreach (PlayerToken defender in defenderWillJump.Where(token => token != null).Distinct())
        {
            ExpectedStatsCalculator.AerialContestant defenderContestant = contestants.FirstOrDefault(contestant => contestant.token == defender);
            float xRecovery = ExpectedStatsCalculator.CalculateAerialWinProbability(defenderContestant, contestants);
            MatchManager.Instance.gameData.gameLog.LogExpectedRecovery(
                defender,
                xRecovery,
                connectedToken: MatchManager.Instance.LastTokenToTouchTheBallOnPurpose,
                recoveryType: "header"
            );
        }
    }

    private void LogExpectedHeaderGoal(PlayerToken shooter, bool requireNaturalRollAboveOne, bool includeGoalkeeperSave)
    {
        ExpectedStatsCalculator.AerialContestant[] contestants = BuildAerialContestants();
        ExpectedStatsCalculator.AerialContestant shooterContestant = contestants.FirstOrDefault(contestant => contestant.token == shooter);
        PlayerToken defendingGK = hexGrid.GetDefendingGK();
        int? keeperSaving = null;
        int keeperPenalty = 0;

        if (includeGoalkeeperSave && defendingGK != null && headerAtGoalTarget != null)
        {
            HexCell gkHex = defendingGK.GetCurrentHex();
            HexCell ballHex = ball.GetCurrentHex();
            if (gkHex != null
                && ballHex != null
                && ballHex.HeadingPaths.TryGetValue(headerAtGoalTarget, out List<HexCell> path))
            {
                List<HexCell> saveableHexes = hexGrid.GetSavableHexes();
                HexCell previewSaveHex = path
                    .Where(hex => saveableHexes.Contains(hex))
                    .OrderBy(hex => HexGridUtils.GetHexStepDistance(gkHex, hex))
                    .FirstOrDefault();
                if (previewSaveHex != null)
                {
                    keeperSaving = defendingGK.saving;
                    int saveDistance = HexGridUtils.GetHexStepDistance(gkHex, previewSaveHex);
                    if (saveDistance == 3) keeperPenalty = -1;
                }
            }
        }

        float xGoals = ExpectedStatsCalculator.CalculateHeaderGoalProbability(
            shooterContestant,
            contestants,
            requireNaturalRollAboveOne,
            keeperSaving,
            keeperPenalty
        );
        MatchManager.Instance.gameData.gameLog.LogExpectedGoal(shooter, xGoals, "header");
    }

    private List<PlayerToken> GetOrderedHeaderTokens(IEnumerable<PlayerToken> tokens, bool keepGoalkeeperLast)
    {
        HexCell ballHex = ball.GetCurrentHex();
        return tokens
            .Where(token => token != null)
            .Distinct()
            .OrderBy(token => keepGoalkeeperLast && token.IsGoalKeeper ? 1 : 0)
            .ThenBy(token => HexGridUtils.GetHexStepDistance(ballHex, token.GetCurrentHex()))
            .ThenByDescending(GetHeaderAttribute)
            .ThenBy(GetTokenSortName)
            .ToList();
    }

    private PlayerToken GetBestHeaderCandidate(List<PlayerToken> orderedTokens)
    {
        if (orderedTokens == null || orderedTokens.Count == 0) return null;

        Dictionary<PlayerToken, int> rollOrder = orderedTokens
            .Select((token, index) => new { token, index })
            .ToDictionary(item => item.token, item => item.index);

        return orderedTokens
            .Where(token => tokenScores.ContainsKey(token))
            .OrderByDescending(token => tokenScores[token].totalScore)
            .ThenBy(token => rollOrder[token])
            .FirstOrDefault();
    }

    private IEnumerable<PlayerToken> GetEligibleOutfieldHeaderDefenders()
    {
        return defEligibleToHead.Where(token => token != null && !token.IsGoalKeeper);
    }

    private PlayerToken GetEligibleDefendingGoalkeeperForHeader()
    {
        return defEligibleToHead.FirstOrDefault(token => token != null && token.IsGoalKeeper);
    }

    private int GetMaxOutfieldHeaderDefenderNominations()
    {
        return Mathf.Min(MAX_OUTFIELD_HEADER_DEFENDERS, GetEligibleOutfieldHeaderDefenders().Count());
    }

    private int GetSelectedOutfieldHeaderDefenderCount()
    {
        return defenderWillJump.Count(token => token != null && !token.IsGoalKeeper);
    }

    private int GetMaxDefenderHeaderNominations()
    {
        int goalkeeperCount = GetEligibleDefendingGoalkeeperForHeader() == null ? 0 : 1;
        return GetMaxOutfieldHeaderDefenderNominations() + goalkeeperCount;
    }

    private bool IsLockedGoalkeeperHeaderNomination(PlayerToken token)
    {
        return token != null && highPassManager.gkRushedOut && token == hexGrid.GetDefendingGK();
    }

    private PlayerToken GetFreeHeaderWinner(List<PlayerToken> candidates)
    {
        return GetOrderedHeaderTokens(candidates, true).FirstOrDefault();
    }

    private IEnumerator ResolveUnchallengedHeaderAtGoal()
    {
        MatchManager.Instance.ClearHangingPass();
        attackerWillJump = GetOrderedHeaderTokens(attackerWillJump, false);
        if (attackerWillJump.Count == 0)
        {
            Debug.LogError("Header at goal was declared, but no attackers were nominated to head.");
            yield break;
        }

        Debug.Log("Header at goal is already declared. Defense is not challenging, so rolling nominated attackers for the headed shot.");
        foreach (PlayerToken attacker in attackerWillJump)
        {
            tokenRolling = attacker;
            isWaitingForHeaderRoll = true;
            while (isWaitingForHeaderRoll) yield return null;
        }

        PlayerToken bestAttacker = GetBestHeaderCandidate(attackerWillJump);
        if (bestAttacker == null)
        {
            Debug.LogError("Header at goal could not determine the best attacking header.");
            yield break;
        }

        challengeWinner = bestAttacker;
        int bestAttackerScore = tokenScores[bestAttacker].totalScore;
        int bestAttackerRoll = tokenScores[bestAttacker].roll;
        Debug.Log($"Best headed shot attacker: {bestAttacker.name} with a score of {bestAttackerScore} (roll = {bestAttackerRoll})");
        MatchManager.Instance.gameData.gameLog.LogEvent(
            MatchManager.Instance.LastTokenToTouchTheBallOnPurpose,
            MatchManager.ActionType.AerialPassCompleted
        );
        MatchManager.Instance.SetLastToken(bestAttacker);
        MatchManager.Instance.gameData.gameLog.LogEvent(bestAttacker, MatchManager.ActionType.ShotAttempt, shotType: "header");
        LogExpectedHeaderGoal(bestAttacker, requireNaturalRollAboveOne: true, includeGoalkeeperSave: true);

        if (bestAttackerRoll > 1)
        {
            MatchManager.Instance.gameData.gameLog.LogEvent(bestAttacker, MatchManager.ActionType.ShotOnTarget);
            shotManager.ProcessHeaderAtGoal(bestAttacker, bestAttackerScore, headerAtGoalTarget);
            CleanUpHeader();
        }
        else
        {
            MatchManager.Instance.gameData.gameLog.LogEvent(bestAttacker, MatchManager.ActionType.ShotOffTarget);
            yield return StartCoroutine(FailedLob());
            Debug.Log("Header at goal is off target.");
            string side = bestAttacker.GetCurrentHex().coordinates.z > 0 ? "RightGoalLine" : "LeftGoalLine";
            StartCoroutine(outOfBoundsManager.HandleGoalKickOrCorner(bestAttacker.GetCurrentHex(), side, "inaccuracy"));
            CleanUpHeader();
            ResetHeader();
        }
    }

    private void OfferUnchallengedAttackChoiceAfterDefenseForfeit()
    {
        if (headerAtGoalDeclared)
        {
            Debug.Log("Defense declined to challenge, but attack has already declared a header at goal. Proceeding to headed shot resolution.");
            isWaitingForDefenderSelection = false;
            isWaitingForControlOrHeaderDecision = false;
            isWaitingForControlOrHeaderDecisionDef = false;
            isWaitingForAttackerSelection = false;
            defenderWillJump.Clear();
            hasEligibleDefenders = false;
            defEligibleToHead.Clear();
            highPassManager.gkRushedOut = false;
            hexGrid.ClearHighlightedHexes();
            StartCoroutine(ResolveHeaderChallenge());
            return;
        }

        Debug.Log("Defense declined to challenge the header. Attack is now unchallenged and may choose Header or Control again.");
        isWaitingForDefenderSelection = false;
        isWaitingForControlOrHeaderDecision = true;
        isWaitingForControlOrHeaderDecisionDef = false;
        isWaitingForAttackerSelection = false;
        isWaitingForHeaderAtGoal = false;
        headerAtGoalDeclared = false;
        headerAtGoalTarget = null;
        attackerWillJump.Clear();
        defenderWillJump.Clear();
        hasEligibleDefenders = false;
        defEligibleToHead.Clear();
        attackFreeHeader = false;
        attackControlBall = false;
        highPassManager.gkRushedOut = false;
        hexGrid.ClearHighlightedHexes();
    }

    // Coroutine for handling defender header selection
    public void HandleDefenderHeaderSelection(PlayerToken token)
    {
        Debug.Log($"threshold: {threshold}");
        if (token == null) return;

        if (!hasEligibleAttackers)
        {
            if (!defenderWillJump.Contains(token)) defenderWillJump.Add(token);
            challengeWinner = token;
            ConfirmDefenderHeaderSelection();
            return;
        }
        if (IsLockedGoalkeeperHeaderNomination(token))
        {
            Debug.LogWarning($"{token.name} is the GK and has already declared to be challenging, cannot de nominate");
            return;
        }
        if (defenderWillJump.Contains(token))
        {
            Debug.LogWarning($"{token.name} has already declared to Jump for header. De-nominating token");
            // TODO: Maybe deselect?
            defenderWillJump.Remove(token);
            return;
        }

        if (token.IsGoalKeeper)
        {
            PlayerToken eligibleGoalkeeper = GetEligibleDefendingGoalkeeperForHeader();
            if (token == eligibleGoalkeeper)
            {
                defenderWillJump.Add(token);
                Debug.Log($"Defending GK {token.name} selected to jump for the header.");
            }
            else
            {
                Debug.LogWarning($"{token.name} is not eligible to challenge this header.");
            }
            return;
        }

        if (GetSelectedOutfieldHeaderDefenderCount() < GetMaxOutfieldHeaderDefenderNominations())
        {
            defenderWillJump.Add(token);
            Debug.Log($"Defender {token.name} selected to jump for the header.");
        }
        else Debug.LogWarning("Outfield defender nominations are full, either deselect one, select the eligible GK if available, or press Enter to confirm");
    }

    public void ConfirmDefenderHeaderSelection()
    {
        Debug.Log("[Enter] was pressed. Defender header selection confirmed.");
        isWaitingForDefenderSelection = false;
        if (defenderWillJump.Count == 0)
        {
            OfferUnchallengedAttackChoiceAfterDefenseForfeit();
            return;
        }
        hexGrid.ClearHighlightedHexes();
        highPassManager.gkRushedOut = false;
        StartCoroutine(ResolveHeaderChallenge());
    }

    public void SelectAllAvailableDefenders()
    {
        List<PlayerToken> outfieldDefenders = GetEligibleOutfieldHeaderDefenders().ToList();
        if (outfieldDefenders.Count <= MAX_OUTFIELD_HEADER_DEFENDERS)
        {
            // defenderWillJump.Clear();
            foreach (PlayerToken token in outfieldDefenders)
            {
                if (!defenderWillJump.Contains(token)) defenderWillJump.Add(token);
            }
            PlayerToken eligibleGoalkeeper = GetEligibleDefendingGoalkeeperForHeader();
            if (eligibleGoalkeeper != null && !defenderWillJump.Contains(eligibleGoalkeeper))
            {
                defenderWillJump.Add(eligibleGoalkeeper);
            }
            Debug.Log("All available defenders selected to jump for the header.");
            ConfirmDefenderHeaderSelection();
        }
        else
        {
            Debug.LogWarning("Too many outfield defenders to select automatically. Please click up to two outfield defenders, plus the GK if eligible, or press Enter to confirm selection.");
        }
    }

    public IEnumerator ResolveHeaderChallenge()
    {
        attackerWillJump = GetOrderedHeaderTokens(attackerWillJump, false);
        defenderWillJump = GetOrderedHeaderTokens(defenderWillJump, true);
        // If both lists are empty, do nothing
        if (attackerWillJump.Count == 0 && defenderWillJump.Count == 0)
        {
            Debug.LogError("No attackers or defenders jumping. Ball drops to the ground. This should not happen.");
            yield break;
        }
        // **Scenario: Only Attackers are Jumping**
        if (attackerWillJump.Count > 0 && defenderWillJump.Count == 0)
        {
            if (headerAtGoalDeclared)
            {
                yield return StartCoroutine(ResolveUnchallengedHeaderAtGoal());
                yield break;
            }

            MatchManager.Instance.ClearHangingPass();
            if (attackFreeHeader)
            {
                challengeWinner = GetFreeHeaderWinner(attackerWillJump);
                if (challengeWinner == null)
                {
                    Debug.LogError("Attack free header could not find a nominated header winner.");
                    yield break;
                }
                MatchManager.Instance.gameData.gameLog.LogEvent(MatchManager.Instance.LastTokenToTouchTheBallOnPurpose, MatchManager.ActionType.AerialPassCompleted);
                MatchManager.Instance.SetLastToken(challengeWinner);
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
            challengeWinner = defenderWillJump[0];
            MatchManager.Instance.gameData.gameLog.LogEvent(
                challengeWinner
                , MatchManager.ActionType.BallRecovery
                , recoveryType: "freeheader"
                , connectedToken: MatchManager.Instance.LastTokenToTouchTheBallOnPurpose
            );
            MatchManager.Instance.SetLastToken(challengeWinner);
            MatchManager.Instance.ChangePossession();
            if (challengeWinner.IsGoalKeeper)
            {
                yield return StartCoroutine(groundBallManager.HandleGroundBallMovement(challengeWinner.GetCurrentHex()));
                Debug.Log($"{challengeWinner.name} (DefenseGK) wins the free header. Switching possession. Save and hold scenario.");
                shotManager.isActivated = true;
                shotManager.isWaitingForSaveandHoldScenario = true;
                CleanUpHeader();
                yield break;
            }
            MatchManager.Instance.gameData.gameLog.LogEvent(
                MatchManager.Instance.LastTokenToTouchTheBallOnPurpose
                , MatchManager.ActionType.PassAttempt
            );
            HighlightHexesForHeader(ball.GetCurrentHex(), 6);
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
            
            PlayerToken bestAttacker = GetBestHeaderCandidate(attackerWillJump);
            PlayerToken bestDefender = GetBestHeaderCandidate(defenderWillJump);
            if (bestAttacker == null || bestDefender == null)
            {
                Debug.LogError("Header challenge could not determine best scoring tokens.");
                yield break;
            }

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
                MatchManager.Instance.gameData.gameLog.LogEvent(MatchManager.Instance.LastTokenToTouchTheBallOnPurpose, MatchManager.ActionType.AerialPassCompleted);
                MatchManager.Instance.gameData.gameLog.LogEvent(bestAttacker, MatchManager.ActionType.AerialChallengeWon, connectedToken: bestDefender);
                LogExpectedHeaderRecoveriesForDefenders();
                MatchManager.Instance.SetLastToken(bestAttacker);
                if (headerAtGoalDeclared)
                {
                    LogExpectedHeaderGoal(bestAttacker, requireNaturalRollAboveOne: true, includeGoalkeeperSave: !defenderWillJump.Contains(hexGrid.GetDefendingGK()));
                    if (bestAttackerRoll > 1) // Header at goal ON target
                    {
                        if (defenderWillJump.Contains(hexGrid.GetDefendingGK())) // GK is in the challenge and was defeated
                        {
                            Debug.Log($"{bestAttacker.name} (Attack) wins the header and scores a GOAL!.");
                            MatchManager.Instance.gameData.gameLog.LogEvent(bestAttacker, MatchManager.ActionType.ShotAttempt, shotType: "header");
                            MatchManager.Instance.gameData.gameLog.LogEvent(bestAttacker, MatchManager.ActionType.ShotOnTarget);
                            MatchManager.Instance.gameData.gameLog.LogEvent(bestAttacker, MatchManager.ActionType.GoalScored);
                            yield return StartCoroutine(groundBallManager.HandleGroundBallMovement(headerAtGoalTarget, bestAttackerRoll));
                            goalFlowManager.StartGoalFlow(bestAttacker);
                            CleanUpHeader();
                            ResetHeader();
                        }
                        else // TODO: GK was not in the challenge and can attempt to save
                        {
                            MatchManager.Instance.gameData.gameLog.LogEvent(bestAttacker, MatchManager.ActionType.ShotAttempt, shotType: "header");
                            MatchManager.Instance.gameData.gameLog.LogEvent(bestAttacker, MatchManager.ActionType.ShotOnTarget);
                            shotManager.ProcessHeaderAtGoal(bestAttacker, bestAttackerScore, headerAtGoalTarget);
                            CleanUpHeader();
                        }
                    }
                    else // Header At goal OFF Target
                    {
                        MatchManager.Instance.gameData.gameLog.LogEvent(bestAttacker, MatchManager.ActionType.ShotAttempt, shotType: "header");
                        MatchManager.Instance.gameData.gameLog.LogEvent(bestAttacker, MatchManager.ActionType.ShotOffTarget);
                        yield return StartCoroutine(FailedLob());
                        Debug.Log("off target");
                        string side = bestAttacker.GetCurrentHex().coordinates.z > 0 ? "RightGoalLine" : "LeftGoalLine";
                        StartCoroutine(outOfBoundsManager.HandleGoalKickOrCorner(bestAttacker.GetCurrentHex(), side, "inaccuracy"));
                        CleanUpHeader();
                        ResetHeader();
                    }
                }
                else // Headed Pass (regardless of headedShot range)
                {
                    HighlightHexesForHeader(ball.GetCurrentHex(), 6);
                    // Do not log the pass attempt here, as it is logged in the next state
                    // MatchManager.Instance.gameData.gameLog.LogEvent(bestAttacker, MatchManager.ActionType.PassAttempt);
                    Debug.Log($"{bestAttacker.name} (Attack) wins the header. Highlighting target hexes.");
                    MatchManager.Instance.currentState = MatchManager.GameState.HeaderChallengeResolved;
                    StartCoroutine(WaitForHeaderTargetSelection());
                }
            }
            else if (bestDefenderScore > bestAttackerScore)
            {
                // TODO: if headerAtGoalDeclared log a header at goal?
                challengeWinner = bestDefender;
                MatchManager.Instance.gameData.gameLog.LogEvent(bestDefender, MatchManager.ActionType.AerialChallengeWon, connectedToken: bestAttacker);
                LogExpectedHeaderRecoveriesForDefenders();
                MatchManager.Instance.gameData.gameLog.LogEvent(bestDefender, MatchManager.ActionType.BallRecovery, recoveryType: "header", connectedToken: bestAttacker);
                MatchManager.Instance.SetLastToken(bestDefender);
                if (bestDefender.IsGoalKeeper) // TODO: in penalty area (their own)
                {
                    yield return StartCoroutine(groundBallManager.HandleGroundBallMovement(bestDefender.GetCurrentHex())); // Move the ball to the GK's Hex
                    MatchManager.Instance.ChangePossession();
                    Debug.Log($"{bestDefender.name} (DefenseGK) wins the Aerial Challenge. Switching possession. SaveandHoldScenario");
                    shotManager.isActivated = true;
                    shotManager.isWaitingForSaveandHoldScenario = true;
                    CleanUpHeader();
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
                if (headerAtGoalDeclared)
                {
                    MatchManager.Instance.gameData.gameLog.LogEvent(bestAttacker, MatchManager.ActionType.ShotAttempt, shotType: "header");
                    MatchManager.Instance.gameData.gameLog.LogEvent(
                        bestAttacker,
                        MatchManager.ActionType.ShotBlocked,
                        connectedToken: bestDefender
                    );
                    LogExpectedHeaderGoal(bestAttacker, requireNaturalRollAboveOne: true, includeGoalkeeperSave: false);
                }
                MatchManager.Instance.SetHangingPass("aerial");
                // MatchManager.Instance.gameData.gameLog.LogEvent(MatchManager.Instance.LastTokenToTouchTheBallOnPurpose, MatchManager.ActionType.AerialPassCompleted);
                StartCoroutine(looseBallManager.ResolveLooseBall(bestDefender, LooseBallSourceType.HeaderDeflection));
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
        RollInputOverride? rollOverride = rigRoll.HasValue
            ? new RollInputOverride
            {
                hasOverride = true,
                roll = rigRoll.Value,
                isJackpot = false
            }
            : null;
        PerformHeaderRoll(rollOverride);
    }

    public void PerformHeaderRoll(RollInputOverride? rollOverride)
    {
        var (returnedRoll, returnedJackpot) = helperFunctions.DiceRoll();
        bool isRiggedJackpot = rollOverride.HasValue && rollOverride.Value.hasOverride && rollOverride.Value.isJackpot;
        bool isJackpot = isRiggedJackpot || (!rollOverride.HasValue && returnedJackpot);
        int roll = rollOverride.HasValue && rollOverride.Value.hasOverride
            ? rollOverride.Value.roll
            : returnedRoll;
        int totalScore = isJackpot ? 50 : roll + attribute + (hasHeadingPenalty ? -1 : 0);
        // int totalScore = roll + attacker.heading + (hasHeadingPenalty ? -1 : 0);
        tokenScores[tokenRolling] = (roll, totalScore);
        if (isJackpot) Debug.Log($"{attordef} {tokenRolling.name} rolled A JACKPOT!!");
        else Debug.Log($"{attordef} {tokenRolling.name} rolled {roll} + {attributelabel}: {attribute}{penaltyInfo}= {totalScore}");
        isWaitingForHeaderRoll = false; // Proceed to the next token
    }
    
    private IEnumerator FailedLob()
    {
        HexCell shooterHex = ball.GetCurrentHex();
        int targetX = 22 * (shooterHex.coordinates.x > 0 ? 1 : -1);
        float slope = (float)(headerAtGoalTarget.coordinates.z - shooterHex.coordinates.z) /
                  (headerAtGoalTarget.coordinates.x - shooterHex.coordinates.x);
        int intercept = headerAtGoalTarget.coordinates.z - Mathf.RoundToInt(slope * headerAtGoalTarget.coordinates.x);
        int intersectionZ = Mathf.RoundToInt(slope * targetX + intercept);
        yield return StartCoroutine(longBallManager.HandleLongBallMovement(hexGrid.GetHexCellAt(new Vector3Int(targetX, 0, intersectionZ)), true));
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
        isWaitingForControlRoll = true;
        Debug.Log(GetControlRollInstruction());
    }

    public async Task PerformControlRoll(int? rigRoll = null)
    {
        RollInputOverride? rollOverride = rigRoll.HasValue
            ? new RollInputOverride
            {
                hasOverride = true,
                roll = rigRoll.Value,
                isJackpot = false
            }
            : null;
        await PerformControlRoll(rollOverride);
    }

    public async Task PerformControlRoll(RollInputOverride? rollOverride)
    {
        isWaitingForControlRoll = false;
        var (returnedRoll, returnedJackpot) = helperFunctions.DiceRoll();
        bool isRiggedJackpot = rollOverride.HasValue && rollOverride.Value.hasOverride && rollOverride.Value.isJackpot;
        bool isJackpot = isRiggedJackpot || (!rollOverride.HasValue && returnedJackpot);
        int roll = GetRollValueWithoutJackpot(rollOverride, returnedRoll);
        int controlPenalty = GetControlPenalty(challengeWinner);
        int totalScore = isJackpot ? 50 : roll + challengeWinner.dribbling + controlPenalty;
        string controlPenaltyInfo = controlPenalty < 0 ? $", penalty ({controlPenalty})" : "";
        if (isJackpot) Debug.Log($"{challengeWinner.name} rolled A JACKPOT for ball control!!");
        else Debug.Log($"Attacker {challengeWinner.name} rolled {roll} + Dribbling {challengeWinner.dribbling}{controlPenaltyInfo} = {totalScore}");
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
            MatchManager.Instance.SetHangingPass("control");
            StartCoroutine(looseBallManager.ResolveLooseBall(challengeWinner, LooseBallSourceType.GroundDeflection));
            CleanUpHeader();
        }
    }

    private void HighlightHexesForHeader(HexCell startHex, int range)
    {
        Debug.Log($"Highlighting hexes for header from {startHex.coordinates} with range {range}");
        headerTargetThreatByHex.Clear();
        hoveredHeaderTargetHex = null;
        List<HexCell> validHexes = HexGrid.GetHexesInRange(hexGrid, startHex, range)
            .Where(hex => !hex.isOutOfBounds && !hex.isDefenseOccupied).ToList();

        List<HexCell> interceptingDefenderHexes = GetHeaderInterceptionDefenderHexes();
        foreach (HexCell hex in validHexes)
        {
            headerTargetThreatByHex[hex] = MatchManager.Instance.difficulty_level == 1
                && CanHeaderTargetBeIntercepted(hex, interceptingDefenderHexes);
            hexGrid.highlightedHexes.Add(hex);  // Track the highlighted hexes
        }

        RefreshHeaderTargetHighlights();
    }

    private void RefreshHeaderTargetHighlights()
    {
        foreach (HexCell hex in hexGrid.highlightedHexes)
        {
            if (hex == null || !headerTargetThreatByHex.ContainsKey(hex)) continue;

            string highlightReason = hoveredHeaderTargetHex == hex
                ? "HeaderTargetHover"
                : headerTargetThreatByHex[hex] ? "HeaderTargetRisk" : "HeaderTargetFree";
            hex.HighlightHex(highlightReason);
        }
    }

    private List<HexCell> GetHeaderInterceptionDefenderHexes()
    {
        return hexGrid.GetDefenderHexes()
            .Where(IsHexEligibleForHeaderInterception)
            .ToList();
    }

    private bool IsHexEligibleForHeaderInterception(HexCell defenderHex)
    {
        PlayerToken token = defenderHex?.GetOccupyingToken();
        return token != null
            && !attackerWillJump.Contains(token)
            && !defenderWillJump.Contains(token)
            && !movementPhaseManager.stunnedTokens.Contains(token)
            && !movementPhaseManager.stunnedforNext.Contains(token);
    }

    private bool CanHeaderTargetBeIntercepted(HexCell targetHex, List<HexCell> defenderHexes)
    {
        if (targetHex == null || targetHex.isAttackOccupied)
        {
            return false;
        }

        return defenderHexes.Any(defenderHex => defenderHex.GetNeighbors(hexGrid).Contains(targetHex));
    }

    private IEnumerator WaitForHeaderTargetSelection()
    {
        isWaitingForHeaderTargetSelection = true;
        while (isWaitingForHeaderTargetSelection)
        {
            yield return null;
        }
    }

    private IEnumerator MoveHeaderBallToTarget(HexCell targetHex)
    {
        yield return StartCoroutine(groundBallManager.HandleGroundBallMovement(targetHex));
    }

    private async Task MoveHeaderToTargetSelection(HexCell clickedHex)
    {
        hoveredHeaderTargetHex = null;
        headerTargetThreatByHex.Clear();
        hexGrid.ClearHighlightedHexes();
        MatchManager.Instance.gameData.gameLog.LogEvent(
            MatchManager.Instance.LastTokenToTouchTheBallOnPurpose
            , MatchManager.ActionType.PassAttempt
        );
        await helperFunctions.StartCoroutineAndWait(MoveHeaderBallToTarget(clickedHex));
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
            MatchManager.Instance.SetHangingPass("ground");
            CheckForHeaderInterception(clickedHex);
        }
    }

    private void CheckForHeaderInterception(HexCell landingHex)
    {
        // Get all defenders and their ZOIs (neighbors)
        List<HexCell> defenderHexes = hexGrid.GetDefenderHexes();
        List<HexCell> filteredDefenderHexes = defenderHexes
            .Where(IsHexEligibleForHeaderInterception)
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
            MatchManager.Instance.SetHangingPass("ground");
            MatchManager.Instance.BroadcastHeaderCompleted();
        }
    }

    public void PerformInterceptionRoll(int? rigRoll = null)
    {
        RollInputOverride? rollOverride = rigRoll.HasValue
            ? new RollInputOverride
            {
                hasOverride = true,
                roll = rigRoll.Value,
                isJackpot = false
            }
            : null;
        PerformInterceptionRoll(rollOverride);
    }

    public void PerformInterceptionRoll(RollInputOverride? rollOverride)
    {
        isWaitingForInterceptionRoll = false;
        var (returnedRoll, returnedJackpot) = helperFunctions.DiceRoll();
        interceptionDiceRoll = GetRollValueWithoutJackpot(rollOverride, returnedRoll);
    }

    private int GetRollValueWithoutJackpot(RollInputOverride? rollOverride, int returnedRoll)
    {
        if (!rollOverride.HasValue || !rollOverride.Value.hasOverride)
        {
            return returnedRoll;
        }

        return rollOverride.Value.isJackpot ? 6 : rollOverride.Value.roll;
    }

    private int GetControlPenalty(PlayerToken controllingToken)
    {
        HexCell ballHex = ball.GetCurrentHex();
        if (ballHex == null || controllingToken == null) return 0;

        HexCell[] controlBallNeighbors = ballHex.GetNeighbors(hexGrid);
        if (controlBallNeighbors == null) return 0;

        HexCell controllingTokenHex = controllingToken.GetCurrentHex();
        bool hasControlPenalty = controllingTokenHex != ballHex && !controlBallNeighbors.Contains(controllingTokenHex);
        return hasControlPenalty ? -1 : 0;
    }

    private int GetNeededControlRoll(PlayerToken controllingToken)
    {
        const int controlTarget = 9;
        return controlTarget - controllingToken.dribbling - GetControlPenalty(controllingToken);
    }

    private string GetControlRollInstruction()
    {
        int neededRoll = GetNeededControlRoll(challengeWinner);
        string controlPenaltyInfo = GetControlPenalty(challengeWinner) < 0 ? ", with penalty (-1)" : "";
        string neededRollInfo = neededRoll <= 6
            ? $"a roll of {Mathf.Max(1, neededRoll)}+ is needed"
            : "a Jackpot is needed";

        return $"Press [R] to roll with {challengeWinner.name} to attempt to control the Ball (dribbling: {challengeWinner.dribbling}{controlPenaltyInfo}); {neededRollInfo}, ";
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
        MatchManager.Instance.SetHangingPass("ground");
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
        headerAtGoalDeclared = false;
        headerAtGoalTarget = null;
        hexGrid.ClearHighlightedHexes();
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
        // if (isWaitingForSaveandHoldScenario) sb.Append("isWaitingForSaveandHoldScenario, ");
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
            sb.Append(GetDefenderHeaderSelectionInstruction());
        }
        if (isWaitingForHeaderRoll && tokenRolling != null) sb.Append($"Press 'R' to roll for {attordef}: {tokenRolling.name} ({attributelabel}: {attribute}{penaltyInfo})., ");
        if (isWaitingForHeaderTargetSelection) sb.Append($"{challengeWinner.name} won the header, Click on a Highlighted Hex to head the ball, ");
        if (isWaitingForInterceptionRoll) sb.Append($"Press [R] to roll for an interception with {interceptingDefender.name} (tackling: {interceptingDefender.tackling}), ");
        if (iswaitingForChallengeWinnerSelection) sb.Append(GetChallengeWinnerInstruction());
        if (isWaitingForControlOrHeaderDecision || isWaitingForControlOrHeaderDecisionDef) sb.Append($"Press [H] to take a free Header or [B] to attempt a ball control, ");
        if (isWaitingForControlRoll) sb.Append(GetControlRollInstruction());
        if (isWaitingForHeaderAtGoal) sb.Append($"Click on a Highlghted Hex In Goal to declare Goal attempt and target or Press [H] to declare a headed Pass, ");

        if (sb.Length >= 2 && sb[^2] == ',') sb.Length -= 2; // Trim trailing comma
        return sb.ToString();
    }

    public bool? IsInstructionExpectingHomeTeam()
    {
        if (!isActivated || MatchManager.Instance == null)
        {
            return null;
        }

        bool attackingTeamIsHome = MatchManager.Instance.teamInAttack == MatchManager.TeamInAttack.Home;

        if (isWaitingForAttackerSelection)
        {
            return attackingTeamIsHome;
        }

        if (isWaitingForDefenderSelection)
        {
            return !attackingTeamIsHome;
        }

        if (isWaitingForHeaderRoll && tokenRolling != null)
        {
            return tokenRolling.isHomeTeam;
        }

        if (isWaitingForInterceptionRoll && interceptingDefender != null)
        {
            return interceptingDefender.isHomeTeam;
        }

        if ((isWaitingForHeaderTargetSelection
            || iswaitingForChallengeWinnerSelection
            || isWaitingForControlOrHeaderDecision
            || isWaitingForControlOrHeaderDecisionDef
            || isWaitingForControlRoll
            || isWaitingForHeaderAtGoal)
            && challengeWinner != null)
        {
            return challengeWinner.isHomeTeam;
        }

        if (isWaitingForControlOrHeaderDecisionDef || defenseWonFreeHeader || defenseBallControl)
        {
            return !attackingTeamIsHome;
        }

        if (isWaitingForControlOrHeaderDecision || attackFreeHeader || attackControlBall)
        {
            return attackingTeamIsHome;
        }

        return null;
    }
}
