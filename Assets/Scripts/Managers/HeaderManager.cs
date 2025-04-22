using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using System.Threading.Tasks;

public class HeaderManager : MonoBehaviour
{
    [Header("Dependencies")]
    public Ball ball;
    public HexGrid hexGrid;
    public MatchManager matchManager;
    public HighPassManager highPassManager; // Reference to the HighPassManager
    public MovementPhaseManager movementPhaseManager; // Reference to the MovementPhaseManager
    public GroundBallManager groundBallManager;
    public GameInputManager gameInputManager;
    public LooseBallManager looseBallManager;
    public FinalThirdManager finalThirdManager;
    public HelperFunctions helperFunctions;
    [Header("Header States")]
    public bool isAvailable = false;
    public bool isActivated = false;
    [SerializeField]
    private bool hasEligibleAttackers = false;
    [SerializeField]
    private bool hasEligibleDefenders = false;
    [SerializeField]
    private bool isWaitingForAttackerSelection = false; // Flag to indicate waiting for attacker selection
    [SerializeField]
    private bool isWaitingForDefenderSelection = false; // Flag to indicate waiting for attacker selection
    public bool isWaitingForHeaderRoll = false; // Flag to indicate waiting for header roll
    [SerializeField]
    private bool isWaitingForSaveandHoldScenario = false;
    [SerializeField]
    private bool iswaitingForChallengeWinnerSelection = false;
    [SerializeField]
    public PlayerToken challengeWinner = null;
    private bool offeredControl = false;
    [SerializeField]
    private bool isWaitingForControlOrHeaderDecision = false;
    [SerializeField]
    private bool isWaitingForHeaderTargetSelection = false;
    [SerializeField]
    private bool isWaitingForInterceptionRoll = false;
    [Header("Lists")]
    public List<PlayerToken> attEligibleToHead = new List<PlayerToken>();
    public List<PlayerToken> defEligibleToHead = new List<PlayerToken>();
    public List<PlayerToken> attackerWillJump = new List<PlayerToken>();
    public List<PlayerToken> defenderWillJump = new List<PlayerToken>();
    [SerializeField]
    List<HexCell> interceptingDefenders = new List<HexCell>();
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
                    else if (!hasEligibleAttackers)
                    {
                        if (!defenderWillJump.Contains(token)) defenderWillJump.Add(token);
                        // TODO: Check if this needs to be saved in the challengeWinner
                        ConfirmDefenderHeaderSelection();
                        return;
                    }
                    else if (defenderWillJump.Contains(token))
                    {
                        Debug.LogWarning($"{token.name} has already declared to Jump for header. Rejecting input");
                        // TODO: Maybe deselect?
                    }
                    else
                    {
                        StartCoroutine(HandleDefenderHeaderSelection(token));
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
                        // TODO: Maybe deselect?
                    }
                    else
                    {
                        StartCoroutine(HandleAttackerHeaderSelection(token));
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
                else
                {
                    isWaitingForHeaderTargetSelection = false;
                    MoveHeaderToTargetSelection(hex);
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
        if (!isActivated) return;
        else
        {
            if (isWaitingForInterceptionRoll && keyData.key == KeyCode.X)
            {
                StartCoroutine(PerformInterceptionCheck(ball.GetCurrentHex()));
            }
            else if (isWaitingForDefenderSelection)
            {
                if (keyData.key == KeyCode.X)
                {
                    ConfirmDefenderHeaderSelection();
                }
                if (keyData.key == KeyCode.A)
                {
                    SelectAllAvailableDefenders();
                }
            }
            else if (isWaitingForAttackerSelection)
            {
                if (keyData.key == KeyCode.X)
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
                    isWaitingForControlOrHeaderDecision = false;
                    attackerWillJump.Add(challengeWinner);
                    StartAttackHeaderSelection();
                }
                else if (keyData.key == KeyCode.B)
                {
                    StartCoroutine(HandleControlFlow());
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
        }
    }
    
    public void FindEligibleHeaderTokens(HexCell landingHex)
    {
        isActivated = true;
        MatchManager.Instance.currentState = MatchManager.GameState.HeaderGeneric;
        hasEligibleAttackers = false;
        hasEligibleDefenders = false;
        if (highPassManager.gkRushedOut) 
        {
            defEligibleToHead.Add(hexGrid.GetDefendingGK());
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
                    // Is this check needed here? 
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
            isActivated = false;
            MatchManager.Instance.TriggerStandardPass();
            MatchManager.Instance.currentState = MatchManager.GameState.MovementPhaseAttack;
            // TODO: Log Appropriately
        }
        else if (!hasEligibleAttackers)
        {
            Debug.Log("No attackers eligible to head the ball. Defense wins the header automatically.");
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
                StartCoroutine(ResolveHeaderChallenge());
                return;
            }
            // 👤 Only one defender? Auto-select them
            if (defEligibleToHead.Count == 1)
            {
                PlayerToken soloDefender = defEligibleToHead[0];
                defenderWillJump.Add(soloDefender);
                Debug.Log($"Single defender {soloDefender.name} auto-selected for the header.");
                StartCoroutine(ResolveHeaderChallenge());
                return;
            }
            // 👥 More than one defender: wait for player input
            Debug.Log("Multiple defenders eligible. Please click one to jump for the header.");
            StartCoroutine(DetermineDefensiveChallengeWinner());
        }
        else if (!hasEligibleDefenders)
        {
            Debug.Log("No defenders eligible to head the ball. Offering option to header (H) or bring the ball down (B).");
            MatchManager.Instance.hangingPassType = null;
            if (attEligibleToHead.Count == 1)
            {
                challengeWinner = attEligibleToHead[0];
            }
            else
            {
                isWaitingForControlOrHeaderDecision = true;
                while (isWaitingForControlOrHeaderDecision)
                {
                    continue;
                }
            }
            // yield return StartCoroutine(WaitForHeaderOrControlInput());
            // TODO: Check if we indeed need to and how to wait for the below
            StartCoroutine(WaitForHeaderOrControlInput());
        }
        else
        {
            // Both attackers and defenders are eligible
            StartAttackHeaderSelection();
        }
    }

    private void DetermineChallengeWinner(PlayerToken token)
    {
        if (token == null)
        {
            Debug.LogWarning("No token was clicked");
            return;
        }
        else if(!attEligibleToHead.Contains(token))
        {
            Debug.LogWarning($"{token} is not an attacker that can challenge for the HighPass");
            return;
        }
        else
        { 
            challengeWinner = token;
            iswaitingForChallengeWinnerSelection = false;
        }
    }
    
    // Method to start the attacker's header selection
    public void StartAttackHeaderSelection()
    {
        attackerWillJump.Clear();
        if (attEligibleToHead.Count == 1)
        {
            attackerWillJump.Add(attEligibleToHead[0]);
            Debug.Log($"Automatically selected attacker: {attEligibleToHead[0].name}");
            // Check if there are eligible defenders
            if (hasEligibleDefenders && !offeredControl)
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
            Debug.Log($"Please 1-2 attackers to jump for the header. Press 'A' to select all available, or press 'X' to confirm selection.");
            isWaitingForAttackerSelection = true;
            MatchManager.Instance.currentState = MatchManager.GameState.HeaderAttackerSelection;
        }
    }

    public IEnumerator HandleAttackerHeaderSelection(PlayerToken token)
    {
        if (attackerWillJump.Count < 2)
        {
            AddAttackerToHeaderSelection(token);
            // Check if there are eligible defenders
            if (attackerWillJump.Count == 2)
            {
                ConfirmAttackerHeaderSelection();
            }
            yield return null; // Yield to wait for input handled by GIM
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
            if (hasEligibleDefenders && !offeredControl)
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
        // defenderWillJump.Clear();
        isWaitingForDefenderSelection = true;
        Debug.Log($"Please select 1-2 defenders to jump for the header. Press 'A' to select all available, or press 'X' to confirm selection.");
    }

    private IEnumerator DetermineDefensiveChallengeWinner()
    {
        isWaitingForDefenderSelection = true;
        Debug.Log($"Please select 1 of the available defenders to jump for the header");
        while (isWaitingForDefenderSelection)
        {
            yield return null;  // Let GIM drive the input detection and call ConfirmDefenderHeaderSelection()
        }
    }

    // Coroutine for handling defender header selection
    public IEnumerator HandleDefenderHeaderSelection(PlayerToken token)
    {
        // TODO: if attEligibleTohead.Count == 0 threshold = 1
        int threshold = highPassManager.gkRushedOut ? 3 : 2 ;
        highPassManager.gkRushedOut = false;
        if (defenderWillJump.Count < threshold) // TODO: this needs to be turned on a WHILE LOOP
        {
            AddDefenderToHeaderSelection(token);
            if
            (
                defenderWillJump.Count == threshold // reached the allowed theshold
                || defEligibleToHead.All(item => defenderWillJump.Contains(item)) // there are no more eligible defenders.
            )
            {
                ConfirmDefenderHeaderSelection();
                yield break;
            }
            Debug.LogWarning("HandleDefenderHeaderSelection is running!");
            yield return null; // Yield to wait for input handled by GIM
        }
    }

    public void ConfirmDefenderHeaderSelection()
    {
        Debug.Log("[X] was pressed. Defender header selection confirmed.");
        isWaitingForDefenderSelection = false;
        StartCoroutine(ResolveHeaderChallenge());
    }

    public void AddDefenderToHeaderSelection(PlayerToken token)
    {
        if (!defenderWillJump.Contains(token))
        {
            defenderWillJump.Add(token);
            Debug.Log($"Defender {token.name} selected to jump for the header.");
            Debug.Log("Click on more or Press [X] to confirm");
        }
        else Debug.LogWarning($"{token.name} is already selected, rejecting click!");
    }

    public void SelectAllAvailableDefenders()
    {
        if (defEligibleToHead.Count <= 2)
        {
            // defenderWillJump.Clear();
            foreach (PlayerToken token in defEligibleToHead)
            {
                AddDefenderToHeaderSelection(token);
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
            if (offeredControl) // TODO: Check if this is needed
            {
                MatchManager.Instance.gameData.gameLog.LogEvent(MatchManager.Instance.LastTokenToTouchTheBallOnPurpose, MatchManager.ActionType.AerialPassCompleted);
                // MatchManager.Instance.SetLastToken(attackerWillJump[0]); // Pick the first attacker that was selected as the header winner and passer
                Debug.Log("Attackers win the header. Highlighting target hexes.");
                HighlightHexesForHeader(ball.GetCurrentHex(), 6);
                MatchManager.Instance.currentState = MatchManager.GameState.HeaderChallengeResolved;
                yield return WaitForHeaderTargetSelection();
            }
            else
            {
                
                Debug.Log("No defenders wished to jump. Click on one of the eligible Attackers again and then we'll offer again the option for a free header (H) or bring the ball down (B).");
                // wait for a click on one of the attackers in attEligibleToHead and passthem as the token who WON the challenge
                attackerWillJump.Clear();
                if (attEligibleToHead.Count == 1)
                {
                    challengeWinner = attEligibleToHead[0];
                    // MatchManager.Instance.SetLastToken(challengeWinner);
                }
                else
                {
                    iswaitingForChallengeWinnerSelection = true;
                    while (iswaitingForChallengeWinnerSelection)
                    {
                        yield return null;  // Let GIM drive the input detection and call ConfirmDefenderHeaderSelection()
                    }
                    // yield return StartCoroutine(WaitForValidAttackerSelection());
                }
                yield return StartCoroutine(WaitForHeaderOrControlInput());
                yield break;
            }
            
        }
        // **Scenario: Only Defenders are Jumping**
        if (attackerWillJump.Count == 0 && defenderWillJump.Count > 0)
        {
            Debug.Log("Only defenders are jumping. Defense wins the header automatically. Switching possession.");
            // TODO: Sort defenderWillJump by desc header, and then by distance from the ball asc
            MatchManager.Instance.gameData.gameLog.LogEvent(
                defenderWillJump[0]
                , MatchManager.ActionType.BallRecovery
                , recoveryType: "freeheader"
                , connectedToken: MatchManager.Instance.LastTokenToTouchTheBallOnPurpose
            );
            MatchManager.Instance.SetLastToken(defenderWillJump[0]); // Add the first (and maybe only) Defender Token as the one that took the header.
            // TODO: Check if this is a Shot or a pass
            matchManager.ChangePossession();
            // TODO: Check if this is the GK, to attract the ball and play save and hold
            // TODO: Check if this is is the GK, but the landing Hex is out of the box, so we continue with a header.
            MatchManager.Instance.gameData.gameLog.LogEvent(
                MatchManager.Instance.LastTokenToTouchTheBallOnPurpose
                , MatchManager.ActionType.PassAttempt
            );
            HighlightHexesForHeader(ball.GetCurrentHex(), 6);
            yield return WaitForHeaderTargetSelection();
            yield break;
        }
        // **Scenario: Both Attackers & Defenders are Jumping**
        if (attackerWillJump.Count > 0 && defenderWillJump.Count > 0)
        {
            // Both attackers and defenders are jumping
            Debug.Log("Header challenge started. Rolling for attackers and defenders.");
            Dictionary<PlayerToken, (int roll, int totalScore)> tokenScores = new Dictionary<PlayerToken, (int, int)>();
            // Get the ball's current hex and its neighbors
            HexCell ballHex = ball.GetCurrentHex();
            HexCell[] ballNeighbors = ballHex.GetNeighbors(hexGrid);
            // Roll for attackers
            foreach (PlayerToken attacker in attackerWillJump)
            {
                isWaitingForHeaderRoll = true;
                // Check if the attacker is on the ball hex or a neighboring hex
                HexCell attackerHex = attacker.GetCurrentHex();
                bool hasHeadingPenalty = attackerHex != ballHex && !ballNeighbors.Contains(attackerHex);
                string penaltyInfo = hasHeadingPenalty ? ", with penalty (-1)" : "";
                Debug.Log($"Press 'R' to roll for attacker: {attacker.name} (heading: {attacker.heading}{penaltyInfo}).");

                while (isWaitingForHeaderRoll)
                {
                    if (Input.GetKeyDown(KeyCode.R))
                    {
                        var (returnedRoll, returnedJackpot) = helperFunctions.DiceRoll();
                        int roll = returnedRoll;
                        int totalScore = returnedJackpot ? 50 : roll + attacker.heading + (hasHeadingPenalty ? -1 : 0);
                        // int totalScore = roll + attacker.heading + (hasHeadingPenalty ? -1 : 0);
                        tokenScores[attacker] = (roll, totalScore);
                        if (returnedJackpot) Debug.Log($"Attacker {attacker.name} rolled A JACKPOT!!");
                        else Debug.Log($"Attacker {attacker.name} rolled {roll} + heading {attacker.heading}{penaltyInfo} = {totalScore}");
                        isWaitingForHeaderRoll = false; // Proceed to the next token
                    }
                    yield return null;
                }
            }
            // Roll for defenders
            foreach (PlayerToken defender in defenderWillJump)
            {
                isWaitingForHeaderRoll = true;
                // Check if the attacker is on the ball hex or a neighboring hex
                HexCell defenderHex = defender.GetCurrentHex();
                bool hasHeadingPenalty = defenderHex != ballHex && !ballNeighbors.Contains(defenderHex);
                string penaltyInfo = hasHeadingPenalty ? ", with penalty (-1)" : "";
                int attribute = defender.IsGoalKeeper ? defender.aerial : defender.heading;
                string attributelabel = defender.IsGoalKeeper ? "aerial ability" : "heading";
                Debug.Log($"Press 'R' to roll for defender: {defender.name} ({attributelabel}: {attribute}{penaltyInfo}).");

                while (isWaitingForHeaderRoll)
                {
                    if (Input.GetKeyDown(KeyCode.R))
                    {
                        var (returnedRoll, returnedJackpot) = helperFunctions.DiceRoll();
                        int roll = returnedRoll;
                        // int roll = 4;
                        int totalScore = returnedJackpot ? 50 : roll + attribute + (hasHeadingPenalty ? -1 : 0);
                        // int totalScore = roll + attribute + (hasHeadingPenalty ? -1 : 0);
                        tokenScores[defender] = (roll, totalScore);
                        if (returnedJackpot) Debug.Log($"Attacker {defender.name} rolled A JACKPOT!!");
                        else Debug.Log($"Defender {defender.name} rolled {roll} + {attributelabel} {attribute}{penaltyInfo} = {totalScore}");
                        isWaitingForHeaderRoll = false; // Proceed to the next token
                    }
                    yield return null;
                }
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
                Debug.Log($"{bestAttacker.name} (Attack) wins the header. Highlighting target hexes.");
                HighlightHexesForHeader(ball.GetCurrentHex(), 6);
                MatchManager.Instance.gameData.gameLog.LogEvent(MatchManager.Instance.LastTokenToTouchTheBallOnPurpose, MatchManager.ActionType.AerialPassCompleted);
                MatchManager.Instance.gameData.gameLog.LogEvent(bestAttacker, MatchManager.ActionType.AerialChallengeWon, connectedToken: bestDefender);
                MatchManager.Instance.SetLastToken(bestAttacker);
                MatchManager.Instance.currentState = MatchManager.GameState.HeaderChallengeResolved;
                yield return WaitForHeaderTargetSelection();
            }
            else if (bestDefenderScore > bestAttackerScore)
            {
                MatchManager.Instance.gameData.gameLog.LogEvent(bestDefender, MatchManager.ActionType.AerialChallengeWon, connectedToken: bestAttacker);
                MatchManager.Instance.gameData.gameLog.LogEvent(bestDefender, MatchManager.ActionType.BallRecovery, recoveryType: "header", connectedToken: bestAttacker);
                MatchManager.Instance.SetLastToken(bestDefender);
                if (!bestDefender.IsGoalKeeper)
                {
                    Debug.Log($"{bestDefender.name} (Defense) wins the header. Switching possession. Click on a Highlighted Hex to play a Headed Pass.");
                    MatchManager.Instance.currentState = MatchManager.GameState.HeaderChallengeResolved;
                    matchManager.ChangePossession();
                    HighlightHexesForHeader(ball.GetCurrentHex(), 6);
                    yield return WaitForHeaderTargetSelection();
                }
                else
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
            }
            else if (bestDefenderScore == bestAttackerScore)
            {
                MatchManager.Instance.hangingPassType = "aerial";
                // MatchManager.Instance.gameData.gameLog.LogEvent(MatchManager.Instance.LastTokenToTouchTheBallOnPurpose, MatchManager.ActionType.AerialPassCompleted);
                StartCoroutine(looseBallManager.ResolveLooseBall(bestDefender, "header"));
                Debug.Log("Loose ball from header challenge.");
            }
            else
            {
                Debug.LogError("What is going on here? Resolve header cannot decide what happened");
            }
        }
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

    private IEnumerator WaitForHeaderOrControlInput()
    {
        Debug.Log("Press 'H' to head the ball or 'B' to attempt to bring it down.");
        offeredControl = true;
        isWaitingForControlOrHeaderDecision = true;

        while (isWaitingForControlOrHeaderDecision)
        {
            yield return null;
        }
    }

    private IEnumerator HandleControlFlow()
    {
        Debug.Log($"Attempting to bring the ball down.");
        isWaitingForControlOrHeaderDecision = false;
        HexCell ballHex = ball.GetCurrentHex();
        if (ballHex == null) Debug.LogWarning("ballHex is null");
        HexCell[] ballNeighbors = ballHex.GetNeighbors(hexGrid);
        if (ballNeighbors == null) Debug.LogWarning("ballNeighbors is null");
        bool hasHeadingPenalty = challengeWinner.GetCurrentHex() != ballHex && !ballNeighbors.Contains(challengeWinner.GetCurrentHex());
        string penaltyInfo = hasHeadingPenalty ? ", with penalty (-1)" : "";
        Debug.Log($"Press 'R' to attempt ball control: {challengeWinner.name} (dribbling: {challengeWinner.dribbling}{penaltyInfo}).");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.R));
        var (returnedRoll, returnedJackpot) = helperFunctions.DiceRoll();
        int roll = returnedRoll;
        int totalScore = roll + challengeWinner.dribbling + (hasHeadingPenalty ? -1 : 0);
        Debug.Log($"Attacker {challengeWinner.name} rolled {roll} + Dribbling {challengeWinner.dribbling}{penaltyInfo} = {totalScore}");
        if (totalScore >= 9)
        {
            Debug.Log($"You beauty! {challengeWinner.name} brings the ball down on their feet! Continue as if it were a SuccessfulTackle");
            yield return StartCoroutine(groundBallManager.HandleGroundBallMovement(challengeWinner.GetCurrentHex()));
            MatchManager.Instance.gameData.gameLog.LogEvent(
                MatchManager.Instance.LastTokenToTouchTheBallOnPurpose
                , MatchManager.ActionType.AerialPassCompleted
            );
            MatchManager.Instance.SetLastToken(challengeWinner);
            MatchManager.Instance.currentState = MatchManager.GameState.SuccessfulTackle;
        }
        else
        {
            Debug.Log($"{challengeWinner.name} failed to control the ball! Loose ball from {challengeWinner.name}");
            MatchManager.Instance.currentState = MatchManager.GameState.HeaderGeneric;
            MatchManager.Instance.hangingPassType = "control";
            StartCoroutine(looseBallManager.ResolveLooseBall(challengeWinner, "ground"));
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
        MatchManager.Instance.gameData.gameLog.LogEvent(
            MatchManager.Instance.LastTokenToTouchTheBallOnPurpose
            , MatchManager.ActionType.PassAttempt
        );
        await helperFunctions.StartCoroutineAndWait(ball.MoveToCell(clickedHex));
        Debug.Log($"Ball moved to {clickedHex.coordinates}");
        hexGrid.ClearHighlightedHexes();
        if (clickedHex.isAttackOccupied)
        {
            MatchManager.Instance.currentState = MatchManager.GameState.HeaderCompletedToPlayer;
            // TODO: broadcast this better
            MatchManager.Instance.gameData.gameLog.LogEvent(
                MatchManager.Instance.LastTokenToTouchTheBallOnPurpose
                , MatchManager.ActionType.PassCompleted
            );
            MatchManager.Instance.SetLastToken(clickedHex.GetOccupyingToken());
            matchManager.UpdatePossessionAfterPass(clickedHex);
            ball.AdjustBallHeightBasedOnOccupancy();
            finalThirdManager.TriggerFinalThirdPhase();
        }
        else
        {
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
                isWaitingForInterceptionRoll = true;
            }
            else
            {
                Debug.LogError("No defenders eligible for interception. Header goes without interception. Should not appear");
                MatchManager.Instance.currentState = MatchManager.GameState.HeaderCompletedToSpace;
                CleanUpHeader();
            }
        }
        else
        {
            Debug.Log("Landing hex is not in any defender's ZOI. No interception needed.");
            CleanUpHeader();
            MatchManager.Instance.hangingPassType = "ground";
            MatchManager.Instance.currentState = MatchManager.GameState.HeaderCompletedToSpace;
        }
    }

    private IEnumerator PerformInterceptionCheck(HexCell landingHex)
    {
        if (interceptingDefenders == null || interceptingDefenders.Count == 0)
        {
            Debug.Log("No defenders available for interception.");
            finalThirdManager.TriggerFinalThirdPhase();
            yield break;
        }

        foreach (HexCell defenderHex in interceptingDefenders)
        {
            PlayerToken defenderToken = defenderHex.GetOccupyingToken();
            if (defenderToken == null)
            {
                Debug.LogWarning($"No valid token found at defender's hex {defenderHex.coordinates}.");
                continue;
            }
            Debug.Log($"Checking interception for defender at {defenderHex.coordinates}");
            // Roll the dice (1 to 6)
            var (returnedRoll, returnedJackpot) = helperFunctions.DiceRoll();
            int diceRoll = returnedRoll;
            // int diceRoll = 6; // Ensure proper range (1-6)
            Debug.Log($"Dice roll for defender {defenderToken.name} at {defenderHex.coordinates}: {diceRoll}");
            int totalInterceptionScore = diceRoll + defenderToken.tackling;
            Debug.Log($"Total interception score for defender {defenderToken.name}: {totalInterceptionScore}");
            MatchManager.Instance.gameData.gameLog.LogEvent(defenderToken, MatchManager.ActionType.InterceptionAttempt);

            if (diceRoll == 6 || totalInterceptionScore >= 10)
            {
                Debug.Log($"Defender at {defenderHex.coordinates} successfully intercepted the ball!");
                isWaitingForInterceptionRoll = false;
                MatchManager.Instance.gameData.gameLog.LogEvent(
                    defenderToken
                    , MatchManager.ActionType.InterceptionSuccess
                    , recoveryType: "headedpass"
                    , connectedToken: MatchManager.Instance.LastTokenToTouchTheBallOnPurpose
                );
                MatchManager.Instance.SetLastToken(defenderToken);
                // Move the ball to the defender's hex and change possession
                yield return StartCoroutine(ball.MoveToCell(defenderHex));
                MatchManager.Instance.ChangePossession();
                MatchManager.Instance.UpdatePossessionAfterPass(defenderHex);
                // ball.AdjustBallHeightBasedOnOccupancy();
                ball.PlaceAtCell(defenderHex);
                MatchManager.Instance.currentState = MatchManager.GameState.LooseBallPickedUp;
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
        MatchManager.Instance.hangingPassType = "ground";
        MatchManager.Instance.currentState = MatchManager.GameState.HeaderCompletedToSpace;
        finalThirdManager.TriggerFinalThirdPhase();
    }

    private void CleanUpHeader()
    {
      attEligibleToHead.Clear();
      defEligibleToHead.Clear();
      hasEligibleAttackers = false;
      hasEligibleDefenders = false;
      offeredControl = false;
      challengeWinner = null;
    }
    
    public void ResetHeader()
    {
      attEligibleToHead.Clear();
      defEligibleToHead.Clear();
      attackerWillJump.Clear();
      defenderWillJump.Clear();
      hasEligibleAttackers = false;
      hasEligibleDefenders = false;
      offeredControl = false;
    }
}
