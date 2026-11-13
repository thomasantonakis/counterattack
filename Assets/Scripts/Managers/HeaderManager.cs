using System.Collections;
using System.Collections.Generic;
using System.Linq;
// using System.Text.RegularExpressions;
using System.Text;
using UnityEngine;
using System.Threading.Tasks;

public partial class HeaderManager : MonoBehaviour
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
    public bool isWaitingForHeadedBackPassChoice;
    public bool isWaitingForHeadedBackPassRoll;
    private PlayerToken headedBackPassKeeper;
    private bool headedBackPassRollInFlight;
    private int headedBackPassVersion;
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
    [System.NonSerialized] public Dictionary<PlayerToken, (int roll, int totalScore)> tokenScores = new Dictionary<PlayerToken, (int, int)>();
    public PlayerToken tokenRolling;
    public HexCell tokenRollingHex => tokenRolling?.GetCurrentHex();
    public HexCell[] ballNeighbors => ball.GetCurrentHex().GetNeighbors(hexGrid);
    public bool hasHeadingPenalty => GetHeaderPenalty(tokenRolling) < 0;
    public string penaltyInfo => hasHeadingPenalty ? ", with penalty (-1)" : "";
    public string attordef => tokenRolling.isAttacker ? "Attacker" : "Defender";
    public int attribute => GetHeaderAttribute(tokenRolling);
    public string attributelabel => UsesAerialAbility(tokenRolling) ? "aerial ability" : "heading";
    [SerializeField]
    public List<HexCell> interceptingDefenders = new List<HexCell>();
    public PlayerToken interceptingDefender;
    public int interceptionDiceRoll;
    private readonly Dictionary<HexCell, bool> headerTargetThreatByHex = new();
    private readonly List<HexCell> headerAtGoalTargetHexes = new();
    private readonly Dictionary<HexCell, float> headerAtGoalTargetOriginalHeights = new();
    private readonly HashSet<PlayerToken> headerInterceptionSuppressedTokens = new();
    private HexCell hoveredHeaderTargetHex;
    private HexCell hoveredHeaderAtGoalTargetHex;
    private bool headerStartedWithOffsideAssessment;
    private bool automaticDefensiveHeaderRecoveryLogged = false;
    [Header("Tuning")]
    public bool allowUnchallengedDefenseControl = true;
    public const int HEADER_SELECTION_RANGE = 2;
    public const int MAX_OUTFIELD_HEADER_DEFENDERS = 2;
    private const int CONTROL_TARGET = 9;
    private int controlPresentationVersion;
    private int challengePresentationVersion;
    public HeaderChallengePanelController headerChallengePanel;


    private void OnEnable()
    {
        GameInputManager.OnClick += OnClickReceived;
        GameInputManager.OnHover += OnHoverReceived;
        GameInputManager.OnKeyPress += OnKeyReceived;
    }

    private void OnDisable()
    {
        CancelHeadedBackPass();
        CancelHeaderChallengePresentation();
        CancelControlPresentation();
        GameInputManager.OnClick -= OnClickReceived;
        GameInputManager.OnHover -= OnHoverReceived;
        GameInputManager.OnKeyPress -= OnKeyReceived;
        hoveredHeaderTargetHex = null;
        headerTargetThreatByHex.Clear();
        ClearHeaderAtGoalTargets();
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
                _ = MoveHeaderToTargetSelection(hex);
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
        RefreshHeaderAtGoalHover(hex);
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

    private void RefreshHeaderAtGoalHover(HexCell hex)
    {
        HexCell target = isActivated && isWaitingForHeaderAtGoal && headerAtGoalTargetHexes.Contains(hex)
            ? hex : null;
        if (hoveredHeaderAtGoalTargetHex == target) return;

        if (hoveredHeaderAtGoalTargetHex != null)
        {
            hoveredHeaderAtGoalTargetHex.HighlightHex("CanShootFrom", 1);
        }
        hoveredHeaderAtGoalTargetHex = target;
        if (target != null) target.HighlightHex("ShotTargetHover");
    }

    private void OnKeyReceived(KeyPressData keyData)
    {
        if (MatchManager.Instance?.TryDispatchTimedRoll(this, keyData, OnKeyReceived) == true) return;
        if (keyData.isConsumed) return;
        if (finalThirdManager.isActivated) return;
        if (goalKeeperManager.isActivated) return;
        if (!isActivated) return;
        else
        {
            bool hasRollOverride = RollInputOverride.TryParse(keyData, out RollInputOverride rollOverride);
            if (isWaitingForHeadedBackPassChoice && (keyData.key == KeyCode.H || keyData.key == KeyCode.F))
            {
                keyData.Consume(nameof(HeaderManager));
                ChooseHeadedBackPassHands(keyData.key == KeyCode.H);
            }
            else if (isWaitingForHeadedBackPassRoll && (keyData.key == KeyCode.R || hasRollOverride))
            {
                keyData.Consume(nameof(HeaderManager));
                PerformHeadedBackPassRoll(hasRollOverride ? (RollInputOverride?)rollOverride : null);
            }
            else if (isWaitingForInterceptionRoll && (keyData.key == KeyCode.R || hasRollOverride))
            {
                keyData.Consume(nameof(HeaderManager));
                PerformInterceptionRoll(hasRollOverride ? (RollInputOverride?)rollOverride : null);
            }
            else if (isWaitingForDefenderSelection)
            {
                if (IsHeaderSelectionConfirmKey(keyData.key))
                {
                    keyData.Consume(nameof(HeaderManager));
                    ConfirmDefenderHeaderSelection();
                }
                if (keyData.key == KeyCode.A)
                {
                    keyData.Consume(nameof(HeaderManager));
                    SelectAllAvailableDefenders();
                }
            }
            else if (isWaitingForAttackerSelection)
            {
                if (IsHeaderSelectionConfirmKey(keyData.key))
                {
                    keyData.Consume(nameof(HeaderManager));
                    _ = ConfirmAttackerHeaderSelection();
                }
                if (keyData.key == KeyCode.A)
                {
                    keyData.Consume(nameof(HeaderManager));
                    SelectAllAvailableAttackers();
                }
            }
            else if (isWaitingForControlOrHeaderDecision)
            {
                if (keyData.key == KeyCode.H)
                {
                    keyData.Consume(nameof(HeaderManager));
                    Debug.Log("Header option selected.");
                    attackFreeHeader = true;
                    _ = StartAttackHeaderSelection();
                }
                else if (keyData.key == KeyCode.B)
                {
                    keyData.Consume(nameof(HeaderManager));
                    Debug.Log("Ball Control was chosen by Attack");
                    attackControlBall = true;
                    StartAttackControlSelection();
                }
            }
            else if (isWaitingForControlOrHeaderDecisionDef)
            {
                if (keyData.key == KeyCode.H)
                {
                    keyData.Consume(nameof(HeaderManager));
                    DefenseFreeHeader();
                }
                else if (keyData.key == KeyCode.B)
                {
                    keyData.Consume(nameof(HeaderManager));
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
                    keyData.Consume(nameof(HeaderManager));
                    PerformHeaderRoll(hasRollOverride ? (RollInputOverride?)rollOverride : null);
                }
            }
            else if (isWaitingForControlRoll)
            {
                if (keyData.key == KeyCode.R || hasRollOverride)
                {
                    keyData.Consume(nameof(HeaderManager));
                    _ = PerformControlRoll(hasRollOverride ? (RollInputOverride?)rollOverride : null); // No need to await this here
                }
            }
            else if (isWaitingForHeaderAtGoal)
            {
                if (keyData.key == KeyCode.H)
                {
                    keyData.Consume(nameof(HeaderManager));
                    isWaitingForHeaderAtGoal = false;
                    headerAtGoalDeclared = false;
                    ClearHeaderAtGoalTargets();
                }
            }
        }
    }

    private static bool IsHeaderSelectionConfirmKey(KeyCode key)
    {
        return key == KeyCode.Return || key == KeyCode.KeypadEnter;
    }

    private void RefreshHeaderEligibility(HexCell landingHex)
    {
        hasEligibleAttackers = false;
        hasEligibleDefenders = false;
        // Eligibility belongs to this landing, never the previous aerial ball.
        attEligibleToHead.Clear();
        defEligibleToHead.Clear();
        attackerWillJump.Clear();
        defenderWillJump.Clear();
        if (highPassManager.gkRushedOut) 
        {
            PlayerToken gk = hexGrid.GetDefendingGK();
            if (gk != null && gk.GetCurrentHex() != null
                && HexGridUtils.GetHexStepDistance(gk.GetCurrentHex(), landingHex) <= HEADER_SELECTION_RANGE)
            {
                defEligibleToHead.Add(gk);
                defenderWillJump.Add(gk);
                hasEligibleDefenders = true;
            }
            else highPassManager.gkRushedOut = false;
        }
        List<HexCell> nearbyHexes = HexGrid.GetHexesInRange(hexGrid, landingHex, HEADER_SELECTION_RANGE);

        foreach (HexCell hex in nearbyHexes)
        {
            PlayerToken token = hex.GetOccupyingToken();
            if (token != null)
            {
                if (MatchManager.Instance != null && !MatchManager.Instance.CanTokenCollectHangingPass(token))
                {
                    Debug.Log($"{token.name} is not eligible to head because they took the set piece or are excluded from collecting this ball.");
                    continue;
                }

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
    }

    public IEnumerator FindEligibleHeaderTokens(HexCell landingHex)
    {
        while (finalThirdManager.isActivated) yield return null;
        isActivated = true;
        headerStartedWithOffsideAssessment = MatchManager.Instance != null
            && MatchManager.Instance.offsideManager != null
            && MatchManager.Instance.offsideManager.HasStoredAssessment;
        MatchManager.Instance.currentState = MatchManager.GameState.HeaderGeneric;
        RefreshHeaderEligibility(landingHex);
        // Log the results for debugging
        Debug.Log($"hasEligibleAttackers: {hasEligibleAttackers}, hasEligibleDefenders: {hasEligibleDefenders}");

        if (!hasEligibleAttackers && !hasEligibleDefenders)
        {
            if (TryResolveUnchallengedInaccurateHighPassOnExcludedPasser(landingHex))
            {
                yield break;
            }

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

    private bool TryResolveUnchallengedInaccurateHighPassOnExcludedPasser(HexCell landingHex)
    {
        MatchManager matchManager = MatchManager.Instance;
        PlayerToken landingToken = landingHex != null ? landingHex.GetOccupyingToken() : null;
        if (matchManager == null
            || landingToken == null
            || !landingToken.isAttacker
            || (landingToken != matchManager.setPieceTakerExcludedFromNextTouch
                && landingToken != matchManager.hangingPassExcludedCollector))
        {
            return false;
        }

        Debug.Log($"{landingToken.name}'s inaccurate High Pass landed back on them unchallenged. Resolving as Any Other Scenario.");
        movementPhaseManager.ResetMovementPhase();
        matchManager.ClearSetPieceTakerNextTouchExclusion();
        matchManager.ClearHangingPass();
        ball.PlaceAtCell(landingHex);
        matchManager.UpdatePossessionAfterPass(landingHex);
        matchManager.ApplyBallCollectionOwnership(landingToken);
        CleanUpHeader();
        matchManager.BroadcastAnyOtherScenario();
        return true;
    }

    private void HandleHeaderAtGoalClick(HexCell hexcell)
    {
        if (hexcell == null)
        {
            return;
        }

        if (hexcell.isInGoal == 0)
        {
            return;
        }

        if (ball?.GetCurrentHex()?.HeadingPaths == null || !ball.GetCurrentHex().HeadingPaths.ContainsKey(hexcell))
        {
            Debug.LogWarning($"Header at goal target {hexcell.coordinates} is not valid from the current header hex.");
            return;
        }

        if (!headerAtGoalTargetHexes.Contains(hexcell))
        {
            Debug.LogWarning($"Header at goal target {hexcell.coordinates} is not currently selectable.");
            return;
        }

        headerAtGoalTarget = hexcell;
        headerAtGoalDeclared = true;
        isWaitingForHeaderAtGoal = false;
        ClearHeaderAtGoalTargets();
        ball.HoldGoalTargetUntilBallMoves(hexcell);
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
        MatchManager.Instance.ChangePossession();
        if (defEligibleToHead.Count == 1)
        {
            challengeWinner = defEligibleToHead[0];
            LogAutomaticDefensiveHeaderRecovery(challengeWinner, "freeheader");
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
                    if (TryCallOffsideForHeaderToken(token, "high_pass_free_header_nomination"))
                    {
                        return;
                    }

                    attackerWillJump.Clear();
                    attackerWillJump.Add(token);
                    challengeWinner = token;
                    iswaitingForChallengeWinnerSelection = false;
                    hexGrid.ClearHighlightedHexes();
                    StartCoroutine(ResolveHeaderChallenge());
                }
                else if (attackControlBall)
                {
                    if (TryCallOffsideForHeaderToken(token, "high_pass_control_nomination"))
                    {
                        return;
                    }

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
                    LogAutomaticDefensiveHeaderRecovery(token, "freeheader");
                    ConfirmDefenderHeaderSelection();
                }
                else if (defenseBallControl)
                {
                    challengeWinner = token;
                    LogAutomaticDefensiveHeaderRecovery(challengeWinner, "freeheader");
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
                if (TryCallOffsideForHeaderToken(challengeWinner, "high_pass_control_nomination"))
                {
                    return;
                }

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
                if (TryCallOffsideForHeaderToken(challengeWinner, "high_pass_control_nomination"))
                {
                    return;
                }

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
            // ClearHover does not emit OnHover when the pointer leaves the pitch or enters UI.
            if (gameInputManager != null) RefreshHeaderAtGoalHover(gameInputManager.hoveredHex);
            yield return null;
          }
        }
    }
    
    public void LiftHexesInGoal()
    {
        Debug.Log("Highlighting CanShootTo hexes for target selection.");
        ClearHeaderAtGoalTargets();
        HexCell shooterHex = ball.GetCurrentHex();
        if (shooterHex == null || shooterHex.HeadingPaths == null || shooterHex.HeadingPaths.Count == 0)
        {
            Debug.LogWarning("Cannot highlight header-at-goal targets because the current ball hex has no heading paths.");
            return;
        }

        Dictionary<HexCell, List<HexCell>> shootingPaths = shooterHex.HeadingPaths;

        // Highlight and raise all CanShootTo hexes
        foreach (var canShootToHex in shootingPaths.Keys)
        {
            if (canShootToHex == null)
            {
                continue;
            }

            canShootToHex.HighlightHex("CanShootFrom", 1);
            if (!headerAtGoalTargetHexes.Contains(canShootToHex))
            {
                headerAtGoalTargetHexes.Add(canShootToHex);
            }
            if (!headerAtGoalTargetOriginalHeights.ContainsKey(canShootToHex))
            {
                headerAtGoalTargetOriginalHeights[canShootToHex] = canShootToHex.transform.position.y;
            }
            if (canShootToHex.transform.position.y < 0.03f)
            {
                Vector3 position = canShootToHex.transform.position;
                position.y = 0.03f;
                canShootToHex.transform.position = position;
            }
        }
    }

    private void ClearHeaderAtGoalTargets()
    {
        hoveredHeaderAtGoalTargetHex = null;
        foreach (HexCell hex in headerAtGoalTargetHexes)
        {
            if (hex == null)
            {
                continue;
            }

            hex.ResetHighlight();
            if (headerAtGoalTargetOriginalHeights.TryGetValue(hex, out float originalHeight))
            {
                Vector3 position = hex.transform.position;
                position.y = originalHeight;
                hex.transform.position = position;
            }
        }

        headerAtGoalTargetHexes.Clear();
        headerAtGoalTargetOriginalHeights.Clear();
    }

    private bool IsHeaderAtGoalAvailable()
    {
        bool shouldShotBeAvailable = false;
        HexCell ballHex = ball != null ? ball.GetCurrentHex() : null;
        if (ballHex == null || !ballHex.CanHeadFrom || ballHex.HeadingPaths == null || ballHex.HeadingPaths.Count == 0)
        {
            return false;
        }

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
                && ballHex.coordinates.x > 0 // In Right Side of Pitch
            )
            ||
            (
                attackingDirection == MatchManager.TeamAttackingDirection.RightToLeft // Attackers shoot to the Left
                && ballHex.coordinates.x < 0 // In Left Side of Pitch
            )
        )
        {
          shouldShotBeAvailable = true;
        }
        return shouldShotBeAvailable;
    }

    private MatchManager.TeamAttackingDirection GetCurrentAttackingDirection()
    {
        return MatchManager.Instance.teamInAttack == MatchManager.TeamInAttack.Home
            ? MatchManager.Instance.homeTeamDirection
            : MatchManager.Instance.awayTeamDirection;
    }

    private int GetAttackingGoalSide()
    {
        return GetCurrentAttackingDirection() == MatchManager.TeamAttackingDirection.LeftToRight ? 1 : -1;
    }

    private string GetAttackingGoalLineSide()
    {
        return GetAttackingGoalSide() > 0 ? "RightGoalLine" : "LeftGoalLine";
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
        if (TryCallOffsideForHeaderToken(token, "high_pass_header_nomination"))
        {
            return;
        }

        if (!attackerWillJump.Contains(token))
        {
            attackerWillJump.Add(token);
            Debug.Log($"Attacker {token.name} selected to jump for the header.");
        }
    }

    private bool TryCallOffsideForHeaderToken(PlayerToken token, string source)
    {
        return token != null
            && token.isAttacker
            && MatchManager.Instance != null
            && MatchManager.Instance.TryHandleOffsideCollection(token, source, token.GetCurrentHex() ?? ball.GetCurrentHex());
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
            _ = ConfirmAttackerHeaderSelection();
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
            List<PlayerToken> outfieldDefenders = GetEligibleOutfieldHeaderDefenders().ToList();
            PlayerToken eligibleGoalkeeper = GetEligibleDefendingGoalkeeperForHeader();
            bool goalkeeperAlreadySelected = eligibleGoalkeeper != null && defenderWillJump.Contains(eligibleGoalkeeper);
            if (goalkeeperAlreadySelected && outfieldDefenders.Count == 0)
            {
                Debug.Log($"{eligibleGoalkeeper.name} is already challenging as GK and no outfield defenders can challenge. Confirming defender header selection automatically.");
                ConfirmDefenderHeaderSelection();
                return;
            }

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

    private void LogAutomaticDefensiveHeaderRecovery(PlayerToken defender, string recoveryType)
    {
        if (automaticDefensiveHeaderRecoveryLogged || defender == null || MatchManager.Instance == null)
        {
            return;
        }

        PlayerToken connectedToken = MatchManager.Instance.LastTokenToTouchTheBallOnPurpose;
        MatchManager.Instance.gameData.gameLog.LogExpectedRecovery(
            defender,
            1f,
            connectedToken: connectedToken,
            recoveryType: recoveryType);
        MatchManager.Instance.gameData.gameLog.LogEvent(
            defender,
            MatchManager.ActionType.BallRecovery,
            recoveryType: recoveryType,
            connectedToken: connectedToken);
        automaticDefensiveHeaderRecoveryLogged = true;
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

    private string BuildHeaderRollInstruction()
    {
        if (tokenRolling == null)
        {
            return "";
        }

        string abilityLabel = UsesAerialAbility(tokenRolling) ? "Aerial" : "Heading";
        int baseAttribute = GetHeaderAttribute(tokenRolling);
        int penalty = GetHeaderPenalty(tokenRolling);
        string penaltyInfo = penalty < 0 ? " -1 penalty" : "";

        StringBuilder sb = new();
        sb.Append($"Press [R] to roll with {FormatHeaderRollToken(tokenRolling)} ({abilityLabel}{baseAttribute}{penaltyInfo})");

        string scoreContext = BuildHeaderScoreContext();
        if (!string.IsNullOrEmpty(scoreContext))
        {
            sb.Append($". {scoreContext}");
        }

        string neededRoll = BuildHeaderNeededRollInstruction(tokenRolling, penalty);
        if (!string.IsNullOrEmpty(neededRoll))
        {
            sb.Append($" ({neededRoll})");
        }

        return sb.ToString();
    }

    private string BuildHeaderScoreContext()
    {
        List<string> parts = new();
        string bestAttackSummary = BuildBestHeaderScoreSummary(attackerWillJump, "Best Attack Score");
        if (!string.IsNullOrEmpty(bestAttackSummary))
        {
            parts.Add(bestAttackSummary);
        }

        string bestDefenseSummary = BuildBestHeaderScoreSummary(defenderWillJump, "Best Defense Score", preferGoalkeeperOnTie: true);
        if (!string.IsNullOrEmpty(bestDefenseSummary))
        {
            parts.Add(bestDefenseSummary);
        }

        return string.Join(", ", parts);
    }

    private string BuildBestHeaderScoreSummary(List<PlayerToken> candidates, string label, bool preferGoalkeeperOnTie = false)
    {
        PlayerToken best = GetBestHeaderCandidate(candidates, preferGoalkeeperOnTie);
        if (best == null)
        {
            return "";
        }

        int totalScore = tokenScores[best].totalScore;
        string scoreInfo = totalScore == 50 ? "Jackpot" : totalScore.ToString();
        return $"{label}: {FormatHeaderRollToken(best)} {scoreInfo}";
    }

    private string FormatHeaderRollToken(PlayerToken token)
    {
        if (token == null)
        {
            return "";
        }

        string displayName = string.IsNullOrWhiteSpace(token.playerName) ? token.name : token.playerName;
        return $"{token.jerseyNumber}. {displayName}";
    }

    private string BuildHeaderNeededRollInstruction(PlayerToken currentToken, int penalty)
        => GetHeaderRollGuidance(currentToken);

    // Guidance uses the same stored maxima and best-player selection as resolution.
    public string GetHeaderRollGuidance(PlayerToken current)
    {
        if (current == null) return "";
        bool attacking = attackerWillJump.Contains(current);
        var own = GetBestHeaderCandidate(attacking ? attackerWillJump : defenderWillJump, !attacking);
        var opposing = GetBestHeaderCandidate(attacking ? defenderWillJump : attackerWillJump, attacking);
        int effective = GetEffectiveHeaderAttribute(current);
        string side = attacking ? "Attack" : "Defense";
        var parts = new List<string>();
        if (own == null && opposing == null) return "Sets the attacking team score.";
        if (own != null)
        {
            int score = tokenScores[own].totalScore;
            if (score == 50)
                parts.Add(side + " already has JACKPOT; another jackpot matches it without increasing the team score.");
            else
            {
                parts.Add(HeaderTargetText(score + 1 - effective, "improves " + side + "’s best score of " + score));
                int match = score - effective;
                if (match >= 1 && match <= 6) parts.Add($"Roll {match} matches the team best without increasing it.");
            }
            if (!attacking && current.IsGoalKeeper && !own.IsGoalKeeper)
                parts.Add(score == 50 ? "A matching jackpot selects the goalkeeper as best defender; the team score stays JACKPOT."
                    : $"Matching Defense’s {score} selects the goalkeeper as best defender without increasing the team score.");
            else parts.Add("An equal teammate score retains the earlier roller as team best.");
        }
        if (opposing != null)
        {
            int target = tokenScores[opposing].totalScore;
            string other = attacking ? "Defense" : "Attack";
            if (target == 50) parts.Add("Opposing JACKPOT cannot be beaten; a jackpot creates a team tie.");
            else
            {
                if (own != null && tokenScores[own].totalScore > target)
                    parts.Insert(0, side + " currently leads " + HeaderChallengePanelController.Score(tokenScores[own].totalScore)
                        + "–" + target + ".");
                int match = target - effective;
                if (match >= 1 && match <= 6)
                    parts.Add($"Roll {match} matches {other}’s {target}" + (own == null || tokenScores[own].totalScore <= target
                        ? "; the team maxima tie if the team best is not higher." : "; the existing team best remains ahead."));
                else if (match > 6) parts.Add($"No numeric roll can match {other}’s {target}.");
                parts.Add(HeaderTargetText(target + 1 - effective, "beats " + other + "’s " + target));
            }
        }
        return string.Join(" ", parts);
    }

    private static string HeaderTargetText(int minimum, string effect)
        => minimum > 6 ? "Only JACKPOT " + effect + "."
            : $"Roll {Mathf.Max(1, minimum)}+ {effect}.";

    private string GetTokenSortName(PlayerToken token)
    {
        if (token == null) return "";
        return string.IsNullOrWhiteSpace(token.playerName) ? token.name : token.playerName;
    }

    public int GetHeaderAttribute(PlayerToken token)
        => HeaderAttributeAt(token, token != null ? token.GetCurrentHex() : null);

    // The keeper's position, not the ball's landing hex, determines legal use
    // of hands. Projected AI boards use these same rules at their projected hex.
    public static bool UsesAerialAbility(PlayerToken token, HexCell position = null)
    {
        if (token == null || !token.IsGoalKeeper || token.isAttacker || MatchManager.Instance == null) return false;
        position ??= token.GetCurrentHex();
        var direction = token.isHomeTeam ? MatchManager.Instance.homeTeamDirection : MatchManager.Instance.awayTeamDirection;
        int ownBox = direction == MatchManager.TeamAttackingDirection.LeftToRight ? -1 : 1;
        return position != null && position.isInPenaltyBox == ownBox;
    }

    public static int HeaderAttributeAt(PlayerToken token, HexCell position)
        => token == null ? 0 : UsesAerialAbility(token, position) ? token.aerial : token.IsGoalKeeper ? 0 : token.heading;

    public static int HeaderDistancePenaltyAt(PlayerToken token, HexCell position, HexCell landing)
        => token != null && position != null && landing != null && HeaderAttributeAt(token, position) > 0
            && HexGridUtils.GetHexStepDistance(position, landing) > 1 ? -1 : 0;

    public static int EffectiveHeaderAttributeAt(PlayerToken token, HexCell position, HexCell landing)
        => GameplayScore.EffectiveAttribute(HeaderAttributeAt(token, position), HeaderDistancePenaltyAt(token, position, landing));

    private int GetEffectiveHeaderAttribute(PlayerToken token)
    {
        if (token == null) return 0;
        return GameplayScore.EffectiveAttribute(GetHeaderAttribute(token), GetHeaderPenalty(token));
    }

    public int GetHeaderPenalty(PlayerToken token)
    {
        return HeaderDistancePenaltyAt(token, token != null ? token.GetCurrentHex() : null, ball.GetCurrentHex());
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

    public float CalculateHeaderExpectedGoals(PlayerToken shooter, bool requireNaturalRollAboveOne = true,
        bool includeGoalkeeperSave = true)
    {
        if (shooter == null || hexGrid == null || ball == null) return 0f;
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

        return ExpectedStatsCalculator.CalculateHeaderGoalProbability(
            shooterContestant,
            contestants,
            requireNaturalRollAboveOne,
            keeperSaving,
            keeperPenalty
        );
    }

    private float LogExpectedHeaderGoal(PlayerToken shooter, bool requireNaturalRollAboveOne, bool includeGoalkeeperSave)
    {
        float xGoals = CalculateHeaderExpectedGoals(shooter, requireNaturalRollAboveOne, includeGoalkeeperSave);
        MatchManager.Instance.gameData.gameLog.LogExpectedGoal(shooter, xGoals, "header");
        return xGoals;
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

    public PlayerToken GetBestHeaderCandidate(List<PlayerToken> orderedTokens, bool preferGoalkeeperOnTie = false)
    {
        if (orderedTokens == null || orderedTokens.Count == 0) return null;

        Dictionary<PlayerToken, int> rollOrder = orderedTokens
            .Select((token, index) => new { token, index })
            .ToDictionary(item => item.token, item => item.index);

        return orderedTokens
            .Where(token => tokenScores.ContainsKey(token))
            .OrderByDescending(token => tokenScores[token].totalScore)
            .ThenBy(token => preferGoalkeeperOnTie && token.IsGoalKeeper ? 0 : 1)
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

        int presentationVersion = challengePresentationVersion;
        headerChallengePanel?.Begin(this);
        Debug.Log("Header at goal is already declared. Defense is not challenging, so rolling nominated attackers for the headed shot.");
        foreach (PlayerToken attacker in attackerWillJump)
        {
            tokenRolling = attacker;
            isWaitingForHeaderRoll = true;
            headerChallengePanel?.Refresh();
            while (isWaitingForHeaderRoll && presentationVersion == challengePresentationVersion) yield return null;
            if (presentationVersion != challengePresentationVersion) yield break;
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
        float headerXGoals = LogExpectedHeaderGoal(bestAttacker, requireNaturalRollAboveOne: true, includeGoalkeeperSave: true);

        if (headerChallengePanel != null)
            yield return headerChallengePanel.Finish(this, bestAttacker,
                bestAttackerRoll > 1 ? "Unchallenged header at goal · On target · Goalkeeper resolution follows"
                    : "Unchallenged header at goal · Off target", false);
        if (presentationVersion != challengePresentationVersion) yield break;
        if (bestAttackerRoll > 1)
        {
            MatchManager.Instance.gameData.gameLog.LogEvent(bestAttacker, MatchManager.ActionType.ShotOnTarget);
            shotManager.ProcessHeaderAtGoal(bestAttacker, bestAttackerScore, headerAtGoalTarget,
                bestAttackerRoll, GetHeaderPenalty(bestAttacker), headerXGoals);
            CleanUpHeader();
        }
        else
        {
            MatchManager.Instance.gameData.gameLog.LogEvent(bestAttacker, MatchManager.ActionType.ShotOffTarget);
            yield return StartCoroutine(FailedLob());
            Debug.Log("Header at goal is off target.");
            StartCoroutine(outOfBoundsManager.HandleGoalKickOrCorner(bestAttacker.GetCurrentHex(), GetAttackingGoalLineSide(), "inaccuracy", bestAttacker));
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
        ClearHeaderAtGoalTargets();
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
        int presentationVersion = challengePresentationVersion;
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
                if (TryCallOffsideForHeaderToken(challengeWinner, "high_pass_free_header_nomination"))
                {
                    yield break;
                }

                MatchManager.Instance.gameData.gameLog.LogEvent(MatchManager.Instance.LastTokenToTouchTheBallOnPurpose, MatchManager.ActionType.AerialPassCompleted);
                MatchManager.Instance.SetLastToken(challengeWinner);
                MatchManager.Instance.ClearOffsideForLegalCollection(challengeWinner, "high_pass_free_header_won");
                ReassessOffsideAfterHeaderWinner("high_pass_free_header_won");
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
            LogAutomaticDefensiveHeaderRecovery(challengeWinner, "freeheader");
            MatchManager.Instance.SetLastToken(challengeWinner);
            MatchManager.Instance.ClearOffsideForLegalCollection(challengeWinner, "high_pass_defender_free_header_won");
            MatchManager.Instance.ChangePossession();
            ReassessOffsideAfterHeaderWinner("high_pass_defender_free_header_won");
            // Possession has already changed here; use the legal catch hex
            // rather than the newly updated attack/defense role.
            if (challengeWinner.IsGoalKeeper && goalKeeperManager.IsGoalkeeperOwnPenaltyHex(challengeWinner, challengeWinner.GetCurrentHex()))
            {
                yield return StartCoroutine(groundBallManager.HandleGroundBallMovement(challengeWinner.GetCurrentHex()));
                Debug.Log($"{challengeWinner.name} (DefenseGK) wins the free header. Switching possession. Save and hold scenario.");
                shotManager.isActivated = true;
                shotManager.isWaitingForSaveandHoldScenario = true;
                CleanUpHeader();
                yield break;
            }
            HighlightHexesForHeader(ball.GetCurrentHex(), 6);
            StartCoroutine(WaitForHeaderTargetSelection());
        }
        // **Scenario: Both Attackers & Defenders are Jumping**
        if (attackerWillJump.Count > 0 && defenderWillJump.Count > 0)
        {
            // Both attackers and defenders are jumping
            headerChallengePanel?.Begin(this);
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
                headerChallengePanel?.Refresh();
                while (isWaitingForHeaderRoll && presentationVersion == challengePresentationVersion) yield return null;
                if (presentationVersion != challengePresentationVersion) yield break;
            }
            // Roll for defenders
            foreach (PlayerToken defender in defenderWillJump)
            {
                isWaitingForHeaderRoll = true;
                tokenRolling = defender;
                headerChallengePanel?.Refresh();
                while (isWaitingForHeaderRoll && presentationVersion == challengePresentationVersion) yield return null;
                if (presentationVersion != challengePresentationVersion) yield break;
            }
            
            PlayerToken bestAttacker = GetBestHeaderCandidate(attackerWillJump);
            PlayerToken bestDefender = GetBestHeaderCandidate(defenderWillJump, preferGoalkeeperOnTie: true);
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

            if (headerAtGoalDeclared && bestDefenderScore >= bestAttackerScore)
            {
                ball.ClearCommittedGoalTarget();
                ClearHeaderAtGoalTargets();
                if (headerAtGoalTarget != null) headerAtGoalTarget.ResetHighlight();
                if (bestDefenderScore > bestAttackerScore)
                {
                    headerAtGoalTarget = null;
                    headerAtGoalDeclared = false;
                }
            }

            if (bestAttackerScore > bestDefenderScore)
            {
                challengeWinner = bestAttacker;
                if (TryCallOffsideForHeaderToken(bestAttacker, "high_pass_header_challenge_won"))
                {
                    yield break;
                }

                if (headerChallengePanel != null)
                    yield return headerChallengePanel.Finish(this, bestAttacker, headerAtGoalDeclared
                        ? (bestAttackerRoll > 1 ? "Attack wins aerial contest · On-target header · Shot continuation follows"
                            : "Attack wins aerial contest · Header off target")
                        : "Attack wins · Headed pass follows", false);
                if (presentationVersion != challengePresentationVersion) yield break;
                MatchManager.Instance.gameData.gameLog.LogEvent(MatchManager.Instance.LastTokenToTouchTheBallOnPurpose, MatchManager.ActionType.AerialPassCompleted);
                MatchManager.Instance.gameData.gameLog.LogEvent(bestAttacker, MatchManager.ActionType.AerialChallengeWon, connectedToken: bestDefender);
                LogExpectedHeaderRecoveriesForDefenders();
                MatchManager.Instance.SetLastToken(bestAttacker);
                MatchManager.Instance.ClearOffsideForLegalCollection(bestAttacker, "high_pass_header_challenge_won");
                ReassessOffsideAfterHeaderWinner("high_pass_header_challenge_won");
                if (headerAtGoalDeclared)
                {
                    float headerXGoals = LogExpectedHeaderGoal(bestAttacker, requireNaturalRollAboveOne: true, includeGoalkeeperSave: !defenderWillJump.Contains(hexGrid.GetDefendingGK()));
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
                            shotManager.ProcessHeaderAtGoal(bestAttacker, bestAttackerScore, headerAtGoalTarget,
                                bestAttackerRoll, GetHeaderPenalty(bestAttacker), headerXGoals);
                            CleanUpHeader();
                        }
                    }
                    else // Header At goal OFF Target
                    {
                        MatchManager.Instance.gameData.gameLog.LogEvent(bestAttacker, MatchManager.ActionType.ShotAttempt, shotType: "header");
                        MatchManager.Instance.gameData.gameLog.LogEvent(bestAttacker, MatchManager.ActionType.ShotOffTarget);
                        yield return StartCoroutine(FailedLob());
                        Debug.Log("off target");
                        StartCoroutine(outOfBoundsManager.HandleGoalKickOrCorner(bestAttacker.GetCurrentHex(), GetAttackingGoalLineSide(), "inaccuracy", bestAttacker));
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
                if (headerChallengePanel != null)
                    yield return headerChallengePanel.Finish(this, bestDefender, UsesAerialAbility(bestDefender)
                        ? "Goalkeeper wins · Save and hold follows" : "Defense wins · Possession changes · Headed pass follows", false);
                if (presentationVersion != challengePresentationVersion) yield break;
                MatchManager.Instance.gameData.gameLog.LogEvent(bestDefender, MatchManager.ActionType.AerialChallengeWon, connectedToken: bestAttacker);
                LogExpectedHeaderRecoveriesForDefenders();
                MatchManager.Instance.gameData.gameLog.LogEvent(bestDefender, MatchManager.ActionType.BallRecovery, recoveryType: "header", connectedToken: bestAttacker);
                MatchManager.Instance.SetLastToken(bestDefender);
                MatchManager.Instance.ClearOffsideForLegalCollection(bestDefender, "high_pass_header_defender_won");
                if (UsesAerialAbility(bestDefender))
                {
                    MatchManager.Instance.ChangePossession();
                    ReassessOffsideAfterHeaderWinner("high_pass_header_defender_won");
                    yield return StartCoroutine(groundBallManager.HandleGroundBallMovement(bestDefender.GetCurrentHex())); // Move the ball to the GK's Hex
                    Debug.Log($"{bestDefender.name} (DefenseGK) wins the Aerial Challenge. Switching possession. SaveandHoldScenario");
                    shotManager.isActivated = true;
                    shotManager.isWaitingForSaveandHoldScenario = true;
                    CleanUpHeader();
                }
                else
                {
                    Debug.Log($"{bestDefender.name} (Defense) wins the header. Switching possession. Click on a Highlighted Hex to play a Headed Pass.");
                    MatchManager.Instance.ChangePossession();
                    ReassessOffsideAfterHeaderWinner("high_pass_header_defender_won");
                    HighlightHexesForHeader(ball.GetCurrentHex(), 6);
                    StartCoroutine(WaitForHeaderTargetSelection());
                }
            }
            else if (bestDefenderScore == bestAttackerScore)
            {
                // Capture before cleanup clears the declaration; presentation never infers shot context.
                bool shotTie = headerAtGoalDeclared;
                var context = shotTie ? LooseBallPresentationContext.ShotResolution : LooseBallPresentationContext.Standalone;
                if (headerChallengePanel != null)
                    yield return headerChallengePanel.Finish(this, null, "TEAM TIE · Loose ball from "
                        + HeaderChallengePanelController.PlayerName(bestDefender), true);
                if (presentationVersion != challengePresentationVersion) yield break;
                headerChallengePanel?.TransferScatter(this, looseBallManager);
                if (headerAtGoalDeclared)
                {
                    MatchManager.Instance.SetLastToken(bestAttacker);
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
                StartCoroutine(looseBallManager.ResolveLooseBall(bestDefender, LooseBallSourceType.HeaderDeflection, allowGKBoxMove: !shotTie, presentationContext: context, presentationOwner: headerChallengePanel));
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
        GameplayDiceRollResult headerRoll = MatchManager.Instance.ResolveGameplayDiceRoll(
            "header_challenge",
            rollOverride,
            actor: tokenRolling,
            sourceHex: tokenRolling != null ? tokenRolling.GetCurrentHex() : null,
            targetHex: ball.GetCurrentHex(),
            details: new Dictionary<string, string>
            {
                ["side"] = attordef,
                ["attribute"] = attribute.ToString(),
                ["attributeLabel"] = attributelabel,
                ["hasHeadingPenalty"] = hasHeadingPenalty.ToString()
            });
        bool isJackpot = headerRoll.isJackpot;
        int roll = headerRoll.roll;
        int totalScore = GameplayScore.Total(attribute, roll, GetHeaderPenalty(tokenRolling), isJackpot);
        // int totalScore = roll + attacker.heading + (hasHeadingPenalty ? -1 : 0);
        tokenScores[tokenRolling] = (roll, totalScore);
        if (isJackpot) Debug.Log($"{attordef} {tokenRolling.name} rolled A JACKPOT!!");
        else Debug.Log($"{attordef} {tokenRolling.name} rolled {roll} + {attributelabel}: {attribute}{penaltyInfo}= {totalScore}");
        headerChallengePanel?.Refresh();
        isWaitingForHeaderRoll = false; // Proceed to the next token
    }
    
    private IEnumerator FailedLob()
    {
        HexCell shooterHex = ball.GetCurrentHex();
        int targetX = 22 * GetAttackingGoalSide();
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
        if (!defenseBallControl && TryCallOffsideForHeaderToken(challengeWinner, "high_pass_ball_control"))
        {
            return;
        }

        if (!defenseBallControl)
        {
            ReassessOffsideAfterHeaderWinner("high_pass_ball_control");
        }
        isWaitingForControlRoll = true;
        int modifier = GetControlPenalty(challengeWinner);
        SingleRollResolutionPresentation.BallControl(this, () => isActivated && isWaitingForControlRoll,
            challengeWinner, modifier, CONTROL_TARGET,
            ExpectedStatsCalculator.CalculateAttributeCheckSuccessProbability(challengeWinner.dribbling, modifier, CONTROL_TARGET));
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
        int version = controlPresentationVersion;
        PlayerToken controllingToken = challengeWinner;
        isWaitingForControlRoll = false;
        int controlPenalty = GetControlPenalty(controllingToken);
        GameplayDiceRollResult controlRoll = MatchManager.Instance.ResolveGameplayDiceRoll(
            "header_control",
            rollOverride,
            actor: controllingToken,
            sourceHex: controllingToken != null ? controllingToken.GetCurrentHex() : null,
            targetHex: ball.GetCurrentHex(),
            jackpotEnabled: true,
            details: new Dictionary<string, string>
            {
                ["dribbling"] = controllingToken != null ? controllingToken.dribbling.ToString() : string.Empty,
                ["controlPenalty"] = controlPenalty.ToString()
            });
        bool isJackpot = controlRoll.isJackpot;
        int roll = controlRoll.roll;
        int totalScore = GameplayScore.Total(controllingToken.dribbling, roll, controlPenalty, isJackpot);
        string controlPenaltyInfo = controlPenalty < 0 ? $", penalty ({controlPenalty})" : "";
        if (isJackpot) Debug.Log($"{controllingToken.name} rolled A JACKPOT for ball control!!");
        else Debug.Log($"Attacker {controllingToken.name} rolled {roll} + Dribbling {controllingToken.dribbling}{controlPenaltyInfo} = {totalScore}");
        bool controlled = totalScore >= CONTROL_TARGET;
        var panel = DuelResolutionPanelController.Instance;
        string controlResult = isJackpot ? "BALL CONTROLLED · JACKPOT"
            : controlled ? $"BALL CONTROLLED · Total {totalScore}" : $"CONTROL FAILED · Total {totalScore} — LOOSE BALL";
        await helperFunctions.StartCoroutineAndWait(SingleRollResolutionPresentation.Resolve(this, controlRoll,
            controlResult,
            continueWithScatter: !controlled, scatterOwner: looseBallManager));
        if (!IsOnlineControlContinuationCurrent(version)) return;
        if (controlled)
        {
            Debug.Log($"You beauty! {controllingToken.name} brings the ball down on their feet! Continue as if it were a SuccessfulTackle");
            await helperFunctions.StartCoroutineAndWait(groundBallManager.HandleGroundBallMovement(controllingToken.GetCurrentHex()));
            if (!IsOnlineControlContinuationCurrent(version)) return;
            MatchManager.Instance.gameData.gameLog.LogEvent(
                MatchManager.Instance.LastTokenToTouchTheBallOnPurpose
                , MatchManager.ActionType.AerialPassCompleted
            );
            MatchManager.Instance.SetLastToken(controllingToken);
            MatchManager.Instance.ClearOffsideForLegalCollection(controllingToken, "high_pass_ball_control_success");
            CleanUpHeader();
            MatchManager.Instance.ResolveActionBeforeFinalThird(
                MatchManager.MatchActionKind.BallControl,
                () => MatchManager.Instance.BroadcastBallControl());
        }
        else
        {
            PlayerToken failedControlToken = controllingToken;
            Debug.Log($"{failedControlToken.name} failed to control the ball! Loose ball from {failedControlToken.name}");
            bool scatterReserved = panel != null && panel.PrepareScatterContinuation(this, looseBallManager);
            CleanUpHeader(preserveControlScatter: scatterReserved);
            bool continuedImmediately = false;
            MatchManager.Instance.ResolveActionBeforeFinalThird(
                MatchManager.MatchActionKind.BallControl,
                () =>
                {
                    continuedImmediately = true;
                    MatchManager.Instance.SetHangingPass("control");
                    // This control branch is a deliberate collection attempt, never a shot spill.
                    StartCoroutine(looseBallManager.ResolveLooseBall(failedControlToken, LooseBallSourceType.GroundDeflection,
                        presentationContext: LooseBallPresentationContext.Standalone));
                });
            // A half-end/stoppage gate may queue or suppress the callback. Release the panel
            // so that gate can take input; its later loose ball opens a fresh presentation.
            if (!continuedImmediately && scatterReserved) panel.CancelSingle(this);
        }
    }

    private void HighlightHexesForHeader(HexCell startHex, int range)
    {
        Debug.Log($"Highlighting hexes for header from {startHex.coordinates} with range {range}");
        headerTargetThreatByHex.Clear();
        hoveredHeaderTargetHex = null;
        List<HexCell> validHexes = HexGrid.GetHexesInRange(hexGrid, startHex, range)
            .Where(hex => !hex.isOutOfBounds && !hex.isDefenseOccupied && CanHeaderTargetHexBeCollected(hex)).ToList();

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

    private bool CanHeaderTargetHexBeCollected(HexCell hex)
    {
        PlayerToken token = hex != null ? hex.GetOccupyingToken() : null;
        return token == null
            || MatchManager.Instance == null
            || MatchManager.Instance.CanTokenCollectHangingPass(token);
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
            && !headerInterceptionSuppressedTokens.Contains(token)
            && !attackerWillJump.Contains(token)
            && !defenderWillJump.Contains(token)
            && !movementPhaseManager.stunnedTokens.Contains(token)
            && !movementPhaseManager.stunnedforNext.Contains(token);
    }

    public void SuppressHeaderInterceptionForToken(PlayerToken token, string reason)
    {
        if (token == null)
        {
            return;
        }

        headerInterceptionSuppressedTokens.Add(token);
        Debug.Log($"Suppressing header interception for {token.name}: {reason}");
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

    private void ReassessOffsideAfterHeaderWinner(string context)
    {
        if (!headerStartedWithOffsideAssessment || MatchManager.Instance == null)
        {
            return;
        }

        MatchManager.Instance.EnsureOffsideManager()?.EvaluateAndStore(context, forceReassessment: true);
    }

    private IEnumerator MoveHeaderBallToTarget(HexCell targetHex)
    {
        yield return StartCoroutine(groundBallManager.HandleGroundBallMovement(targetHex));
    }

    private async Task MoveHeaderToTargetSelection(HexCell clickedHex)
    {
        MatchManager.Instance.BeginGoalkeeperBallDelivery(GoalkeeperDeliveryKind.Header,
            MatchManager.Instance.LastTokenToTouchTheBallOnPurpose);
        int version = headedBackPassVersion;
        hoveredHeaderTargetHex = null;
        headerTargetThreatByHex.Clear();
        hexGrid.ClearHighlightedHexes();
        MatchManager.Instance.gameData.gameLog.LogEvent(
            MatchManager.Instance.LastTokenToTouchTheBallOnPurpose
            , MatchManager.ActionType.PassAttempt
        );
        await helperFunctions.StartCoroutineAndWait(MoveHeaderBallToTarget(clickedHex));
        if (version != headedBackPassVersion || !isActivated) return;
        Debug.Log($"Ball moved to {clickedHex.coordinates}");
        MatchManager.Instance.UpdatePossessionAfterPass(clickedHex);
        if (clickedHex.isAttackOccupied)
        {
            PlayerToken receiver = clickedHex.GetOccupyingToken();
            if (TryCallOffsideForHeaderToken(receiver, "header_pass_collection"))
            {
                return;
            }

            MatchManager.Instance.gameData.gameLog.LogEvent(
                MatchManager.Instance.LastTokenToTouchTheBallOnPurpose
                , MatchManager.ActionType.PassCompleted
            );
            MatchManager.Instance.SetLastToken(receiver);
            MatchManager.Instance.ClearOffsideForLegalCollection(receiver, "header_pass_completed");
            ball.AdjustBallHeightBasedOnOccupancy();
            if (receiver.IsGoalKeeper && goalKeeperManager.IsGoalkeeperOwnPenaltyHex(receiver, clickedHex))
            {
                headedBackPassKeeper = receiver;
                isWaitingForHeadedBackPassChoice = true;
                while (isWaitingForHeadedBackPassChoice)
                {
                    DuelResolutionPanelController.Instance?.BeginSingleChoice(this,
                        () => isWaitingForHeadedBackPassChoice, "Headed Back-Pass · Hands or Feet", receiver,
                        "Hands: roll 2–6 to hold; 1 scatters from the keeper. Feet: no roll.",
                        "[H] Hands · [F] Feet", KeyCode.H, KeyCode.F);
                    await Task.Yield();
                }
                return;
            }
            CompleteHeadedPassAtFeet();
        }
        else
        {
            MatchManager.Instance.SetHangingPass("ground");
            CheckForHeaderInterception(clickedHex);
        }
    }

    private void CompleteHeadedPassAtFeet()
    {
            CleanUpHeader();
            MatchManager.Instance.ResolveActionBeforeFinalThird(
                MatchManager.MatchActionKind.Header,
                () =>
                {
                    finalThirdManager.TriggerFinalThirdPhase();
                    MatchManager.Instance.BroadcastHeaderCompleted();
                });
    }

    public void ChooseHeadedBackPassHands(bool hands)
    {
        if (!isWaitingForHeadedBackPassChoice || headedBackPassKeeper == null) return;
        isWaitingForHeadedBackPassChoice = false;
        DuelResolutionPanelController.Instance?.CancelSingle(this);
        if (!hands) { CompleteHeadedPassAtFeet(); return; }
        isWaitingForHeadedBackPassRoll = true;
        PresentHeadedBackPassRoll();
    }

    private void PresentHeadedBackPassRoll()
        => DuelResolutionPanelController.Instance?.BeginSingle(this, () => isWaitingForHeadedBackPassRoll,
            "Headed Back-Pass · Goalkeeper Catch", headedBackPassKeeper, null, "Catch 83.3%", false,
            "Roll 1: loose ball from goalkeeper · Rolls 2–6: hold · No attribute modifier");

    public void PerformHeadedBackPassRoll(RollInputOverride? rollOverride = null)
    {
        if (!isWaitingForHeadedBackPassRoll || headedBackPassRollInFlight) return;
        headedBackPassRollInFlight = true;
        StartCoroutine(ResolveHeadedBackPassCatch(rollOverride, headedBackPassVersion));
    }

    private IEnumerator ResolveHeadedBackPassCatch(RollInputOverride? rollOverride, int version)
    {
        PlayerToken receiver = headedBackPassKeeper;
        var result = MatchManager.Instance.ResolveGameplayDiceRoll("headed_back_pass_catch", rollOverride,
            actor: receiver, sourceHex: receiver.GetCurrentHex(), jackpotEnabled: false);
        bool caught = result.roll != 1;
        yield return SingleRollResolutionPresentation.Resolve(this, result,
            caught ? "HELD · Choose goalkeeper distribution" : "CATCH FAILED · Loose ball from goalkeeper",
            continueWithScatter: !caught, scatterOwner: looseBallManager);
        if (version != headedBackPassVersion || !isWaitingForHeadedBackPassRoll) yield break;
        var panel = DuelResolutionPanelController.Instance;
        bool scatterReserved = !caught && panel != null && panel.PrepareScatterContinuation(this, looseBallManager);
        CleanUpHeader(preserveControlScatter: scatterReserved);
        bool continuedImmediately = false;
        MatchManager.Instance.ResolveActionBeforeFinalThird(MatchManager.MatchActionKind.Header, () =>
        {
            continuedImmediately = true;
            if (caught)
            {
                MatchManager.Instance.MarkGoalkeeperHolding(receiver);
                MatchManager.Instance.BroadcastDefensiveRecoveryOutcome(receiver, receiver.GetCurrentHex(), false);
            }
            else
            {
                MatchManager.Instance.SetHangingPass("ground");
                StartCoroutine(looseBallManager.ResolveLooseBall(receiver, LooseBallSourceType.GroundDeflection,
                    presentationContext: LooseBallPresentationContext.Standalone));
            }
        });
        if (!continuedImmediately && scatterReserved) panel.CancelSingle(this);
    }

    private void CancelHeadedBackPass(bool preserveScatter = false)
    {
        headedBackPassVersion++;
        isWaitingForHeadedBackPassChoice = isWaitingForHeadedBackPassRoll = false;
        headedBackPassRollInFlight = false;
        headedBackPassKeeper = null;
        if (!preserveScatter) DuelResolutionPanelController.Instance?.CancelSingle(this);
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
            MatchManager.Instance.ResolveActionBeforeFinalThird(
                MatchManager.MatchActionKind.Header,
                () =>
                {
                    finalThirdManager.TriggerFinalThirdPhase();
                    MatchManager.Instance.SetHangingPass("ground");
                    MatchManager.Instance.BroadcastHeaderCompleted();
                });
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
        StartCoroutine(PresentHeaderInterception(rollOverride));
    }

    private bool HeaderInterceptionSucceeded()
        => interceptionDiceRoll == 6 || interceptionDiceRoll + interceptingDefender.tackling >= 10;

    private IEnumerator PresentHeaderInterception(RollInputOverride? rollOverride)
    {
        GameplayDiceRollResult diceResult = MatchManager.Instance.ResolveGameplayDiceRoll(
            "header_interception",
            rollOverride,
            actor: MatchManager.Instance.LastTokenToTouchTheBallOnPurpose,
            sourceHex: ball.GetCurrentHex());
        interceptionDiceRoll = diceResult.roll;
        yield return SingleRollResolutionPresentation.Resolve(this, diceResult,
            HeaderInterceptionSucceeded()
                ? "INTERCEPTED" : "INTERCEPTION FAILED");
        isWaitingForInterceptionRoll = false;
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

    /// <summary>
    /// Read-only preview used by the room AI. Keeping this calculation in the
    /// manager prevents the planner from becoming a second rules engine.
    /// </summary>
    public int GetControlPenaltyForPreview(PlayerToken controllingToken)
    {
        return GetControlPenalty(controllingToken);
    }

    /// <summary>
    /// Returns the defenders that would receive sequential interception rolls
    /// if a headed pass were sent to the supplied empty hex.
    /// </summary>
    public IReadOnlyList<PlayerToken> GetHeaderPassInterceptorsForPreview(HexCell targetHex)
    {
        if (targetHex == null || targetHex.isAttackOccupied || hexGrid == null)
        {
            return System.Array.Empty<PlayerToken>();
        }

        List<PlayerToken> interceptors = new List<PlayerToken>();
        foreach (HexCell defenderHex in GetHeaderInterceptionDefenderHexes())
        {
            if (defenderHex == null || !defenderHex.GetNeighbors(hexGrid).Contains(targetHex))
            {
                continue;
            }

            PlayerToken interceptor = defenderHex.GetOccupyingToken();
            if (interceptor != null)
            {
                interceptors.Add(interceptor);
            }
        }

        return interceptors;
    }

    /// <summary>
    /// Read-only equivalent of the six-hex free-header highlight. Interception is
    /// intentionally not checked along the flight: under the rules only the
    /// landing hex can produce a headed-pass interception.
    /// </summary>
    public IReadOnlyList<HexCell> GetLegalFreeHeaderTargetsForDecisionPreview()
    {
        HexCell startHex = ball != null ? ball.GetCurrentHex() : null;
        if (hexGrid == null || startHex == null)
        {
            return System.Array.Empty<HexCell>();
        }

        return HexGrid.GetHexesInRange(hexGrid, startHex, 6)
            .Where(hex => hex != null
                && !hex.isOutOfBounds
                && !hex.isDefenseOccupied
                && CanHeaderTargetHexBeCollected(hex))
            .ToList();
    }

    private int GetNeededControlRoll(PlayerToken controllingToken)
    {
        return CONTROL_TARGET - GameplayScore.EffectiveAttribute(controllingToken.dribbling, GetControlPenalty(controllingToken));
    }

    private string GetControlRollInstruction()
    {
        int neededRoll = GetNeededControlRoll(challengeWinner);
        string controlPenaltyInfo = GetControlPenalty(challengeWinner) < 0 ? ", with penalty (-1)" : "";
        if (challengeWinner.dribbling + GetControlPenalty(challengeWinner) < 0) controlPenaltyInfo += ", effective attribute 0 (minimum 0)";
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
            MatchManager.Instance.ResolveActionBeforeFinalThird(
                MatchManager.MatchActionKind.Header,
                () => finalThirdManager.TriggerFinalThirdPhase());
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
            SingleRollResolutionPresentation.Recovery(this, () => isWaitingForInterceptionRoll,
                interceptingDefender, "Landing Ball Interception");
            while (isWaitingForInterceptionRoll) yield return null;

            Debug.Log($"Checking interception for defender at {defenderHex.coordinates}");
            Debug.Log($"Dice roll for defender {interceptingDefender.name} at {defenderHex.coordinates}: {interceptionDiceRoll}");
            int totalInterceptionScore = interceptionDiceRoll + interceptingDefender.tackling;
            Debug.Log($"Total interception score for defender {interceptingDefender.name}: {totalInterceptionScore}");
            MatchManager.Instance.gameData.gameLog.LogEvent(interceptingDefender, MatchManager.ActionType.InterceptionAttempt);

            if (HeaderInterceptionSucceeded())
            {
                Debug.Log($"Defender at {defenderHex.coordinates} successfully intercepted the ball!");
                MatchManager.Instance.gameData.gameLog.LogEvent(
                    interceptingDefender
                    , MatchManager.ActionType.InterceptionSuccess
                    , recoveryType: "headedpass"
                    , connectedToken: MatchManager.Instance.LastTokenToTouchTheBallOnPurpose
                );
                MatchManager.Instance.SetLastToken(interceptingDefender);
                // Resolve the winning touch before evaluating box entry on the attraction.
                MatchManager.Instance.ChangePossession();
                yield return StartCoroutine(ball.MoveToCell(defenderHex));
                MatchManager.Instance.UpdatePossessionAfterPass(defenderHex);
                // ball.AdjustBallHeightBasedOnOccupancy();
                ball.PlaceAtCell(defenderHex);
                PlayerToken recoveringHeaderDefender = interceptingDefender;
                CleanUpHeader();
                MatchManager.Instance.ResolveActionBeforeFinalThird(
                    MatchManager.MatchActionKind.Header,
                    () => MatchManager.Instance.BroadcastDefensiveRecoveryOutcome(recoveringHeaderDefender, defenderHex));
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
        MatchManager.Instance.ResolveActionBeforeFinalThird(
            MatchManager.MatchActionKind.Header,
            () =>
            {
                finalThirdManager.TriggerFinalThirdPhase();
                MatchManager.Instance.SetHangingPass("ground");
                MatchManager.Instance.BroadcastHeaderCompleted();
            });
    }

    private void CancelControlPresentation(bool preserveControlScatter = false)
    {
        controlPresentationVersion++;
        isWaitingForControlRoll = false;
        if (!preserveControlScatter) DuelResolutionPanelController.Instance?.CancelSingle(this);
    }

    private void CancelHeaderChallengePresentation()
    {
        challengePresentationVersion++;
        isWaitingForHeaderRoll = false;
        headerChallengePanel?.Cancel(this);
    }

    public void CleanUpHeader(bool preserveControlScatter = false)
    {
        CancelHeadedBackPass(preserveControlScatter);
        CancelHeaderChallengePresentation();
        CancelControlPresentation(preserveControlScatter);
        attEligibleToHead.Clear();
        defEligibleToHead.Clear();
        hasEligibleAttackers = false;
        hasEligibleDefenders = false;
        isWaitingForControlOrHeaderDecision = false;
        isWaitingForControlOrHeaderDecisionDef = false;
        challengeWinner = null;
        headerStartedWithOffsideAssessment = false;
        tokenRolling = null;
        tokenScores = new Dictionary<PlayerToken, (int, int)>();
        interceptingDefenders.Clear();
        headerInterceptionSuppressedTokens.Clear();
        automaticDefensiveHeaderRecoveryLogged = false;
        isActivated = false;
        interceptingDefender = null;
        interceptionDiceRoll = 0;
        defenseWonFreeHeader = false;
        defenseBallControl = false;
        attackFreeHeader = false;
        attackControlBall = false;
        headerAtGoalDeclared = false;
        headerAtGoalTarget = null;
        ClearHeaderAtGoalTargets();
        hexGrid.ClearHighlightedHexes();
    }
    
    public void ResetHeader()
    {
        CancelHeadedBackPass();
        CancelHeaderChallengePresentation();
        CancelControlPresentation();
        attEligibleToHead.Clear();
        defEligibleToHead.Clear();
        attackerWillJump.Clear();
        defenderWillJump.Clear();
        headerInterceptionSuppressedTokens.Clear();
        automaticDefensiveHeaderRecoveryLogged = false;
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

    public void PopulateRoomDecisionContext(RoomDecisionContext context)
    {
        if (context == null || !isActivated)
        {
            return;
        }

        if (isWaitingForHeadedBackPassChoice)
        {
            context.AddKeyActionCandidate(nameof(HeaderManager), RoomActionType.Header,
                RoomDecisionStep.InterruptionChoice, "H", "Catch headed back-pass: roll 1 scatters; 2–6 held");
            context.AddKeyActionCandidate(nameof(HeaderManager), RoomActionType.Header,
                RoomDecisionStep.InterruptionChoice, "F", "Receive headed back-pass at feet without a roll");
            return;
        }
        if (isWaitingForHeadedBackPassRoll)
        {
            context.AddKeyActionCandidate(nameof(HeaderManager), RoomActionType.Roll,
                RoomDecisionStep.Roll, "R", "Roll headed back-pass catch");
            return;
        }
        if (isWaitingForAttackerSelection)
        {
            if (attEligibleToHead.Count <= 2)
            {
                context.AddKeyActionCandidate(
                    nameof(HeaderManager),
                    RoomActionType.Header,
                    RoomDecisionStep.Setup,
                    "A",
                    "Press [A] to select all available attackers");
            }

            if (IsAttackerHeaderNominationReady())
            {
                context.AddKeyActionCandidate(
                    nameof(HeaderManager),
                    RoomActionType.Header,
                    RoomDecisionStep.Confirm,
                    "Enter",
                    "Press [Enter] to confirm attacking challengers");
                context.AddActionSummary("Click a nominated attacker to remove them");
            }

            context.AddActionSummary("Click an eligible attacker to nominate for the header");
            AddHeaderActorCandidates(context, GetUnnominatedHeaderCandidates(attEligibleToHead, attackerWillJump), "Nominate");
            return;
        }

        if (isWaitingForDefenderSelection)
        {
            if (GetEligibleOutfieldHeaderDefenders().Count() <= MAX_OUTFIELD_HEADER_DEFENDERS)
            {
                context.AddKeyActionCandidate(
                    nameof(HeaderManager),
                    RoomActionType.Header,
                    RoomDecisionStep.Setup,
                    "A",
                    "Press [A] to select all available defenders");
            }

            if (IsDefenderHeaderNominationReady())
            {
                context.AddKeyActionCandidate(
                    nameof(HeaderManager),
                    RoomActionType.Header,
                    RoomDecisionStep.Confirm,
                    "Enter",
                    defenderWillJump.Count == 0
                        ? "Press [Enter] to decline the defensive challenge"
                        : "Press [Enter] to confirm defensive challengers");
            }

            if (defenderWillJump.Count > 0)
            {
                context.AddActionSummary("Click a nominated defender to remove them");
            }

            context.AddActionSummary("Click an eligible defender to nominate for the header");
            AddHeaderActorCandidates(context, GetUnnominatedHeaderCandidates(defEligibleToHead, defenderWillJump), "Nominate");
            return;
        }

        if (isWaitingForControlOrHeaderDecision || isWaitingForControlOrHeaderDecisionDef)
        {
            context.AddKeyActionCandidate(
                nameof(HeaderManager),
                RoomActionType.Header,
                RoomDecisionStep.InterruptionChoice,
                "H",
                "Press [H] to take a free Header");
            context.AddKeyActionCandidate(
                nameof(HeaderManager),
                RoomActionType.SelectToken,
                RoomDecisionStep.InterruptionChoice,
                "B",
                "Press [B] to attempt ball control");
            return;
        }

        if (iswaitingForChallengeWinnerSelection)
        {
            context.AddActionSummary("Click a valid header/control winner");
            AddHeaderActorCandidates(context, GetChallengeWinnerCandidates(), "Select");
            return;
        }

        if (isWaitingForHeaderAtGoal)
        {
            context.AddKeyActionCandidate(
                nameof(HeaderManager),
                RoomActionType.Header,
                RoomDecisionStep.ChooseActionType,
                "H",
                "Press [H] to make a headed pass");
            context.AddActionSummary("Click a highlighted in-goal hex");
            AddHeaderAtGoalTargets(context);
            return;
        }

        if (isWaitingForHeaderTargetSelection)
        {
            context.AddActionSummary("Click a highlighted headed-pass target");
            AddHeaderPassTargets(context);
            return;
        }

        if (isWaitingForHeaderRoll)
        {
            context.AddKeyActionCandidate(
                nameof(HeaderManager),
                RoomActionType.Roll,
                RoomDecisionStep.Roll,
                "R",
                "Press [R] to roll the header");
            return;
        }

        if (isWaitingForControlRoll)
        {
            context.AddKeyActionCandidate(
                nameof(HeaderManager),
                RoomActionType.Roll,
                RoomDecisionStep.Roll,
                "R",
                "Press [R] to roll ball control");
            return;
        }

        if (isWaitingForInterceptionRoll)
        {
            context.AddKeyActionCandidate(
                nameof(HeaderManager),
                RoomActionType.Roll,
                RoomDecisionStep.Roll,
                "R",
                "Press [R] to roll header interception");
        }
    }

    private static void AddHeaderActorCandidates(RoomDecisionContext context, IEnumerable<PlayerToken> tokens, string verb)
    {
        if (context == null || tokens == null)
        {
            return;
        }

        foreach (PlayerToken token in tokens)
        {
            if (token == null)
            {
                continue;
            }

            context.AddTokenActionCandidate(
                nameof(HeaderManager),
                RoomActionType.Header,
                RoomDecisionStep.ChooseActor,
                token,
                $"{verb} {FormatDecisionTokenName(token)} for the header");
        }
    }

    private bool IsAttackerHeaderNominationReady()
    {
        int desiredNominations = Mathf.Min(2, attEligibleToHead.Count(token => token != null));
        return desiredNominations > 0 && attackerWillJump.Count(token => token != null) >= desiredNominations;
    }

    private bool IsDefenderHeaderNominationReady()
    {
        int desiredNominations = Mathf.Min(2, defEligibleToHead.Count(token => token != null));
        return desiredNominations == 0 || defenderWillJump.Count(token => token != null) >= desiredNominations;
    }

    private static IEnumerable<PlayerToken> GetUnnominatedHeaderCandidates(
        IEnumerable<PlayerToken> eligibleTokens,
        IEnumerable<PlayerToken> nominatedTokens)
    {
        if (eligibleTokens == null)
        {
            return Enumerable.Empty<PlayerToken>();
        }

        HashSet<PlayerToken> nominated = nominatedTokens != null
            ? new HashSet<PlayerToken>(nominatedTokens.Where(token => token != null))
            : new HashSet<PlayerToken>();

        return eligibleTokens.Where(token => token != null && !nominated.Contains(token));
    }

    private void AddHeaderAtGoalTargets(RoomDecisionContext context)
    {
        foreach (HexCell hex in GetHeaderAtGoalDecisionTargets())
        {
            context.AddHexActionCandidate(
                nameof(HeaderManager),
                RoomActionType.Shot,
                RoomDecisionStep.ChooseTarget,
                hex,
                $"Head at goal to hex {hex.coordinates}");
        }
    }

    private IEnumerable<HexCell> GetHeaderAtGoalDecisionTargets()
    {
        if (headerAtGoalTargetHexes.Count > 0)
        {
            return headerAtGoalTargetHexes.Where(hex => hex != null);
        }

        HexCell headerHex = ball != null ? ball.GetCurrentHex() : null;
        if (headerHex?.HeadingPaths == null)
        {
            return Enumerable.Empty<HexCell>();
        }

        return headerHex.HeadingPaths.Keys
            .Where(hex => hex != null && hex.isInGoal != 0);
    }

    private void AddHeaderPassTargets(RoomDecisionContext context)
    {
        if (context == null || hexGrid == null)
        {
            return;
        }

        foreach (HexCell hex in hexGrid.highlightedHexes)
        {
            if (hex == null || !headerTargetThreatByHex.ContainsKey(hex))
            {
                continue;
            }

            PlayerToken targetToken = hex.GetOccupyingToken();
            if (targetToken != null && targetToken.isAttacker)
            {
                context.AddTargetTokenActionCandidate(
                    nameof(HeaderManager),
                    RoomActionType.Header,
                    RoomDecisionStep.ChooseTarget,
                    targetToken,
                    $"Headed pass to {FormatDecisionTokenName(targetToken)}");
                continue;
            }

            context.AddHexActionCandidate(
                nameof(HeaderManager),
                RoomActionType.Header,
                RoomDecisionStep.ChooseTarget,
                hex,
                $"Headed pass to hex {hex.coordinates}");
        }
    }

    private static string FormatDecisionTokenName(PlayerToken token)
    {
        if (token == null)
        {
            return "the selected player";
        }

        return !string.IsNullOrWhiteSpace(token.playerName) ? token.playerName : token.name;
    }


    public string GetInstructions()
    {
        StringBuilder sb = new();
        if (isWaitingForHeadedBackPassChoice) return "Headed back-pass: [H] hands (1 scatters, 2–6 held), [F] feet (no roll).";
        if (isWaitingForHeadedBackPassRoll) return "Press [R] for goalkeeper catch: 1 scatters from the keeper; 2–6 held. No attribute modifier.";
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
        if (isWaitingForHeaderRoll && tokenRolling != null) sb.Append($"{BuildHeaderRollInstruction()}, ");
        if (isWaitingForHeaderTargetSelection) sb.Append($"{challengeWinner.name} won the header, Click on a Highlighted Hex to head the ball, ");
        if (isWaitingForInterceptionRoll) sb.Append($"Press [R] to roll for an interception with {interceptingDefender.name} (tackling: {interceptingDefender.tackling}), ");
        if (iswaitingForChallengeWinnerSelection) sb.Append(GetChallengeWinnerInstruction());
        if (isWaitingForControlOrHeaderDecision || isWaitingForControlOrHeaderDecisionDef) sb.Append($"Press [H] to take a free Header or [B] to attempt a ball control, ");
        if (isWaitingForControlRoll) sb.Append(GetControlRollInstruction());
        if (isWaitingForHeaderAtGoal) sb.Append($"Click on a highlighted Hex in Goal to declare Goal attempt and target or Press [H] to declare a headed Pass, ");

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

        if (isWaitingForHeadedBackPassChoice || isWaitingForHeadedBackPassRoll)
            return headedBackPassKeeper?.isHomeTeam;

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

        if (isWaitingForHeaderAtGoal)
        {
            return attackingTeamIsHome;
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
