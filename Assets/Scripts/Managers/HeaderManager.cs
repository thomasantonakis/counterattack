using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

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
    [Header("Header States")]
    public bool isWaitingForHeaderRoll = false; // Flag to indicate waiting for header roll
    public List<PlayerToken> attEligibleToHead = new List<PlayerToken>();
    public List<PlayerToken> defEligibleToHead = new List<PlayerToken>();
    public List<PlayerToken> attackerWillJump = new List<PlayerToken>();
    public List<PlayerToken> defenderWillJump = new List<PlayerToken>();
    public PlayerToken challengeWinner = null;
    private bool hasEligibleAttackers = false;
    private bool hasEligibleDefenders = false;
    private bool challengeWinnerIsSelected = false;
    private bool isWaitingForControlRoll = false;
    private bool isWaitingForSaveandHoldScenario = false;
    private const int HEADER_SELECTION_RANGE = 2;
    private bool offeredControl = false;
    List<HexCell> interceptingDefenders = new List<HexCell>();
    private bool isWaitingForInterceptionRoll = false;

    public void Update()
    {
        if (isWaitingForInterceptionRoll)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                StartCoroutine(PerformInterceptionCheck(ball.GetCurrentHex()));
            }
        }
    }
    
    public void FindEligibleHeaderTokens(HexCell landingHex)
    {
        MatchManager.Instance.currentState = MatchManager.GameState.HeaderGeneric;
        hasEligibleAttackers = false;
        hasEligibleDefenders = false;
        if (highPassManager.didGKMoveInDefPhase) 
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
                    if (!token.IsGoalKeeper) defEligibleToHead.Add(token);
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
            MatchManager.Instance.currentState = MatchManager.GameState.MovementPhaseAttack;
            // TODO: Log Appropriately
        }
        else if (!hasEligibleAttackers)
        {
            Debug.Log("No attackers eligible to head the ball. Defense wins the header automatically.");
            MatchManager.Instance.hangingPassType = null;
            StartDefenseHeaderSelection();
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
                challengeWinnerIsSelected = false;
                while (!challengeWinnerIsSelected)
                {
                    if (Input.GetMouseButtonDown(0))
                    {
                        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                        if (Physics.Raycast(ray, out RaycastHit hit))
                        {
                            var (inferredTokenFromClick, inferredHexCellFromClick) =  gameInputManager.DetectTokenOrHexClicked(hit);
                            if (inferredTokenFromClick == null)
                            {
                                Debug.LogWarning("No token was clicked");
                                continue;
                            }
                            else if(!attEligibleToHead.Contains(inferredTokenFromClick))
                            {
                                Debug.LogWarning($"{inferredTokenFromClick} is not an attacker that can challenge for the HighPass");
                                continue;
                            }
                            else
                            { 
                                challengeWinner = inferredTokenFromClick;
                                challengeWinnerIsSelected = true;
                            }
                        }
                    }
                }
            }
            // yield return StartCoroutine(WaitForHeaderOrControlInput());
            StartCoroutine(WaitForHeaderOrControlInput());
        }
        else
        {
            // Both attackers and defenders are eligible
            StartAttackHeaderSelection();
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
            ConfirmAttackerHeaderSelection();
        }
        else
        {
            Debug.LogWarning("Too many attackers to select automatically. Please click on attackers to select them or press 'X' to confirm selection.");
        }
    }

    // Method to start the defender's header selection
    public void StartDefenseHeaderSelection()
    {
        // defenderWillJump.Clear();
        MatchManager.Instance.currentState = MatchManager.GameState.HeaderDefenderSelection;
        Debug.Log($"Please select 1-2 defenders to jump for the header. Press 'A' to select all available, or press 'X' to confirm selection.");
    }

    // Coroutine for handling defender header selection
    public IEnumerator HandleDefenderHeaderSelection(PlayerToken token)
    {
        // TODO: if attEligibleTohead.Count == 0 threshold = 1
        int threshold = highPassManager.didGKMoveInDefPhase ? 3 : 2 ;
        highPassManager.didGKMoveInDefPhase = false;
        if (defenderWillJump.Count < threshold)
        {
            AddDefenderToHeaderSelection(token);
            if (defenderWillJump.Count == threshold)
            {
                ConfirmDefenderHeaderSelection();
            }
            yield return null; // Yield to wait for input handled by GIM
        }
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

    public void ConfirmDefenderHeaderSelection()
    {
        Debug.Log("Defender header selection confirmed.");
        StartCoroutine(ResolveHeaderChallenge());
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

    // Coroutine to resolve the header challenge
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
            MatchManager.Instance.gameData.gameLog.LogEvent(MatchManager.Instance.LastTokenToTouchTheBallOnPurpose, MatchManager.ActionType.AerialPassCompleted);
            if (offeredControl) 
            {
                MatchManager.Instance.SetLastToken(attackerWillJump[0]); // Pick the first attacker that was selected as the header winner and passer
                Debug.Log("Attackers win the header. Highlighting target hexes.");
                HighlightHexesForHeader(ball.GetCurrentHex(), 6);
                MatchManager.Instance.currentState = MatchManager.GameState.HeaderChallengeResolved;
                // TODO: Check if this is a Shot or a pass
                if (true)
                {
                    MatchManager.Instance.gameData.gameLog.LogEvent(
                        MatchManager.Instance.LastTokenToTouchTheBallOnPurpose
                        , MatchManager.ActionType.PassAttempt
                    );
                }
                else
                {
                    MatchManager.Instance.gameData.gameLog.LogEvent(
                        MatchManager.Instance.LastTokenToTouchTheBallOnPurpose
                        , MatchManager.ActionType.ShotAttempt
                    );
                }
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
                    MatchManager.Instance.SetLastToken(challengeWinner);
                }
                else
                {
                    yield return StartCoroutine(WaitForValidAttackerSelection());
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
            MatchManager.Instance.gameData.gameLog.LogEvent(
                MatchManager.Instance.LastTokenToTouchTheBallOnPurpose
                , MatchManager.ActionType.PassAttempt
            );
            matchManager.ChangePossession();
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
                        int roll = Random.Range(1, 7);
                        // int roll = 4;
                        int totalScore = roll + attacker.heading + (hasHeadingPenalty ? -1 : 0);
                        tokenScores[attacker] = (roll, totalScore);
                        Debug.Log($"Attacker {attacker.name} rolled {roll} + heading {attacker.heading}{penaltyInfo} = {totalScore}");
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
                        // int roll = Random.Range(1, 7);
                        int roll = 4;
                        int totalScore = roll + attribute + (hasHeadingPenalty ? -1 : 0);
                        tokenScores[defender] = (roll, totalScore);
                        Debug.Log($"Defender {defender.name} rolled {roll} + {attributelabel} {attribute}{penaltyInfo} = {totalScore}");
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
                        if (Input.GetKeyDown(KeyCode.Q))
                        {
                            isWaitingForSaveandHoldScenario = false;
                            Debug.Log("QuickThrow Scenario chosen, NOBODY MOVES! Click Hex to select target for GK's throw");
                            MatchManager.Instance.currentState = MatchManager.GameState.QuickThrow;
                            yield break;
                        }
                        else if (Input.GetKeyDown(KeyCode.K))
                        {
                            isWaitingForSaveandHoldScenario = false;  // Cancel the decision phase
                            Debug.Log("GK Decided to activate F3 Moves");
                            MatchManager.Instance.currentState = MatchManager.GameState.ActivateFinalThirdsAfterSave;
                            finalThirdManager.TriggerFinalThirdPhase(true);
                            yield break;
                        }
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

    private IEnumerator WaitForValidAttackerSelection()
    {
        challengeWinnerIsSelected = false;
        while (!challengeWinnerIsSelected)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    var (inferredTokenFromClick, inferredHexCellFromClick) =  gameInputManager.DetectTokenOrHexClicked(hit);
                    if (inferredTokenFromClick == null)
                    {
                        Debug.LogWarning("No token was clicked");
                        continue;
                    }
                    else if(!attEligibleToHead.Contains(inferredTokenFromClick))
                    {
                        Debug.LogWarning($"{inferredTokenFromClick.name} is not an attacker that can challenge for the HighPass");
                        continue;
                    }
                    else
                    { 
                        challengeWinner = inferredTokenFromClick;
                        MatchManager.Instance.SetLastToken(challengeWinner);
                        challengeWinnerIsSelected = true;
                        Debug.Log($"{challengeWinner.name} has been selected as the header challenge winner.");
                    }
                }
            }
        }
        yield return null;
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
        bool inputReceived = false;

        while (!inputReceived)
        {
            if (Input.GetKeyDown(KeyCode.H))
            {
                Debug.Log("Header option selected.");
                inputReceived = true;
                attackerWillJump.Add(challengeWinner);
                StartAttackHeaderSelection();
            }
            else if (Input.GetKeyDown(KeyCode.B))
            {
                Debug.Log($"Attempting to bring the ball down.");
                inputReceived = true;
                // isWaitingForControlRoll = true;
                HexCell ballHex = ball.GetCurrentHex();
                if (ballHex == null) Debug.LogWarning("ballHex is null");
                HexCell[] ballNeighbors = ballHex.GetNeighbors(hexGrid);
                if (ballNeighbors == null) Debug.LogWarning("ballNeighbors is null");
                bool hasHeadingPenalty = challengeWinner.GetCurrentHex() != ballHex && !ballNeighbors.Contains(challengeWinner.GetCurrentHex());
                string penaltyInfo = hasHeadingPenalty ? ", with penalty (-1)" : "";
                Debug.Log($"Press 'R' to attempt ball control: {challengeWinner.name} (dribbling: {challengeWinner.dribbling}{penaltyInfo}).");
                yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.R));
                // isWaitingForControlRoll = false;
                int roll = 4;
                // int roll = Random.Range(1, 7);
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

            yield return null;
        }
    }

    // Method to highlight hexes for header
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

    // Coroutine to wait for header target selection
    private IEnumerator WaitForHeaderTargetSelection()
    {
        bool targetSelected = false;

        while (!targetSelected)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    HexCell clickedHex = hit.collider.GetComponent<HexCell>();
                    // TODO: Exclude from valid targets the Hexes of Jumped Tokens
                    // TODO: Handle Click On Token or Ball Properly to infer the Hex
                    if (clickedHex != null && !clickedHex.isDefenseOccupied)
                    {
                        // TODO: BUG THIS IS CALLED 
                        if (true)
                        {
                            MatchManager.Instance.gameData.gameLog.LogEvent(
                                MatchManager.Instance.LastTokenToTouchTheBallOnPurpose
                                , MatchManager.ActionType.PassAttempt
                            );
                        }
                        else
                        {
                            MatchManager.Instance.gameData.gameLog.LogEvent(
                                MatchManager.Instance.LastTokenToTouchTheBallOnPurpose
                                , MatchManager.ActionType.ShotAttempt
                            );
                        }
                        yield return StartCoroutine(ball.MoveToCell(clickedHex));
                        Debug.Log($"Ball moved to {clickedHex.coordinates}");
                        hexGrid.ClearHighlightedHexes();
                        if (clickedHex.isAttackOccupied)
                        {
                            MatchManager.Instance.currentState = MatchManager.GameState.HeaderCompletedToPlayer;
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
                            CheckForHeaderInterception(clickedHex); // calling check for interceptions here
                        }
                        targetSelected = true;
                    }
                    else
                    {
                        Debug.LogWarning("Invalid hex selected. Please select a hex not occupied by the defense.");
                    }
                }
            }

            yield return null;
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
            int diceRoll = 6; // Ensure proper range (1-6)
            // int diceRoll = Random.Range(1, 7); // Ensure proper range (1-6)
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
