using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HeaderManager : MonoBehaviour
{
    [Header("Dependencies")]
    public Ball ball;
    public HexGrid hexGrid;
    public MatchManager matchManager;
    public HighPassManager highPassManager; // Reference to the HighPassManager
    public bool isWaitingForHeaderRoll = false; // Flag to indicate waiting for header roll

    [Header("Header States")]
    public List<PlayerToken> attEligibleToHead = new List<PlayerToken>();
    public List<PlayerToken> defEligibleToHead = new List<PlayerToken>();
    public List<PlayerToken> attackerWillJump = new List<PlayerToken>();
    public List<PlayerToken> defenderWillJump = new List<PlayerToken>();

    private bool hasEligibleAttackers = false;
    private bool hasEligibleDefenders = false;
    private const int HEADER_SELECTION_RANGE = 2;
    private bool offeredControl = false;

    public void FindEligibleHeaderTokens(HexCell landingHex)
    {
        MatchManager.Instance.currentState = MatchManager.GameState.HeaderGeneric;
        attEligibleToHead.Clear();
        defEligibleToHead.Clear();
        hasEligibleAttackers = false;
        hasEligibleDefenders = false;
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
                    defEligibleToHead.Add(token);
                    hasEligibleDefenders = true;
                }
            }
        }
        // Log the results for debugging
        Debug.Log($"hasEligibleAttackers: {hasEligibleAttackers}, hasEligibleDefenders: {hasEligibleDefenders}");

        if (!hasEligibleAttackers && !hasEligibleDefenders)
        {
            Debug.Log("No players eligible to head the ball. Ball drops to the ground.");
            MatchManager.Instance.currentState = MatchManager.GameState.MovementPhaseAttack;
            // TODO: start movement phase
        }
        else if (!hasEligibleAttackers)
        {
            Debug.Log("No attackers eligible to head the ball. Defense wins the header automatically.");
            // TODO: switch possession
            StartDefenseHeaderSelection();
        }
        else if (!hasEligibleDefenders)
        {
            Debug.Log("No defenders eligible to head the ball. Offering option to header (H) or bring the ball down (B).");
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
        defenderWillJump.Clear();
        MatchManager.Instance.currentState = MatchManager.GameState.HeaderDefenderSelection;
        Debug.Log($"Please 1-2 defenders to jump for the header. Press 'A' to select all available, or press 'X' to confirm selection.");
    }

    // Coroutine for handling defender header selection
    public IEnumerator HandleDefenderHeaderSelection(PlayerToken token)
    {
        if (defenderWillJump.Count < 2)
        {
            AddDefenderToHeaderSelection(token);
            if (defenderWillJump.Count == 2)
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
        }
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
            defenderWillJump.Clear();
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
        // If both lists are empty, do nothing
        if (attackerWillJump.Count == 0 && defenderWillJump.Count == 0)
        {
            Debug.Log("No attackers or defenders jumping. Ball drops to the ground. This should not happen.");
            yield break;
        }
        // **Scenario: Only Attackers are Jumping**
        if (attackerWillJump.Count > 0 && defenderWillJump.Count == 0)
        {
            if (offeredControl) 
            {
                Debug.Log("Attackers win the header. Highlighting target hexes.");
                HighlightHexesForHeader(ball.GetCurrentHex(), 6);
                MatchManager.Instance.currentState = MatchManager.GameState.HeaderChallengeResolved;
                yield return WaitForHeaderTargetSelection(false);
            }
            else
            {
                Debug.Log("No defenders wished to jump. Offering again option to header (H) or bring the ball down (B).");
                attackerWillJump.Clear();
                yield return StartCoroutine(WaitForHeaderOrControlInput());
                yield break;
            }
            
        }
        // **Scenario: Only Defenders are Jumping**
        if (attackerWillJump.Count == 0 && defenderWillJump.Count > 0)
        {
            Debug.Log("Only defenders are jumping. Defense wins the header automatically. Switching possession.");
            matchManager.ChangePossession();
            HighlightHexesForHeader(ball.GetCurrentHex(), 6);
            yield return WaitForHeaderTargetSelection(true);
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
                Debug.Log($"Press 'R' to roll for attacker: {defender.name} (heading: {defender.heading}{penaltyInfo}).");

                while (isWaitingForHeaderRoll)
                {
                    if (Input.GetKeyDown(KeyCode.R))
                    {
                        int roll = Random.Range(1, 7);
                        int totalScore = roll + defender.heading + (hasHeadingPenalty ? -1 : 0);
                        tokenScores[defender] = (roll, totalScore);
                        Debug.Log($"Defender {defender.name} rolled {roll} + heading {defender.heading}{penaltyInfo} = {totalScore}");
                        isWaitingForHeaderRoll = false; // Proceed to the next token
                    }
                    yield return null;
                }
            }
            
            PlayerToken bestAttacker = attackerWillJump
                .OrderByDescending(token => tokenScores[token].totalScore) // Sort by total score
                .ThenByDescending(token => tokenScores[token].roll)        // Break ties with the roll
                .First();
            PlayerToken bestDefender = defenderWillJump
                .OrderByDescending(token => tokenScores[token].totalScore) // Sort by total score
                .ThenByDescending(token => tokenScores[token].roll)        // Break ties with the roll
                .First();

            int bestAttackerScore = tokenScores[bestAttacker].totalScore;
            int bestAttackerRoll = tokenScores[bestAttacker].roll;
            Debug.Log($"Best Attacker: {bestAttacker.name} with a score of {bestAttackerScore} (roll = {bestAttackerRoll})");
            int bestDefenderScore = tokenScores[bestDefender].totalScore;
            int bestDefenderRoll = tokenScores[bestDefender].roll;
            Debug.Log($"Best Defender: {bestDefender.name} with a score of {bestDefenderScore} (roll = {bestDefenderRoll})");

            if (bestAttackerScore > bestDefenderScore)
            {
                // TODO: Check if it is a header at goal and if it is a roll of 1
                Debug.Log("Attack wins the header. Highlighting target hexes.");
                HighlightHexesForHeader(ball.GetCurrentHex(), 6);
                MatchManager.Instance.currentState = MatchManager.GameState.HeaderChallengeResolved;
                yield return WaitForHeaderTargetSelection(false);
            }
            else if (bestDefenderScore > bestAttackerScore)
            {
                Debug.Log("Defense wins the header. Switching possession.");
                MatchManager.Instance.currentState = MatchManager.GameState.HeaderChallengeResolved;
                matchManager.ChangePossession();
                HighlightHexesForHeader(ball.GetCurrentHex(), 6);
                yield return WaitForHeaderTargetSelection(true);
            }
            else
            {
                Debug.Log("Loose ball from header challenge.");
            }
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
                StartAttackHeaderSelection();
            }
            else if (Input.GetKeyDown(KeyCode.B))
            {
                Debug.Log("Attempting to bring the ball down.");
                inputReceived = true;
                // TODO: Implement the logic for bringing the ball down
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
    private IEnumerator WaitForHeaderTargetSelection(bool defensePossession)
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
                    if (clickedHex != null && !clickedHex.isDefenseOccupied)
                    {
                        yield return StartCoroutine(ball.MoveToCell(clickedHex));
                        MatchManager.Instance.currentState = MatchManager.GameState.HeaderCompleted;
                        Debug.Log($"Ball moved to {clickedHex.coordinates}");
                        ball.AdjustBallHeightBasedOnOccupancy();
                        matchManager.UpdatePossessionAfterPass(clickedHex);
                        hexGrid.ClearHighlightedHexes();
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
