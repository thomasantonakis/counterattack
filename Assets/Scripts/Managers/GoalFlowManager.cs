using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GoalFlowManager : MonoBehaviour
{
    private enum GoalInstructionPhase
    {
        None,
        Celebration,
        Reset
    }

    public HexGrid hexGrid;
    public PlayerTokenManager playerTokenManager;
    public MovementPhaseManager movementPhaseManager;
    public GroundBallManager groundBallManager;
    public LongBallManager longBallManager;
    public KickoffManager kickoffManager;
    public bool isActivated = false;
    public int LastCompletedGoalSide { get; private set; } = 0;
    public HexCell LastCompletedGoalHex { get; private set; }
    private int activeGoalSide = 0;
    private HexCell activeGoalHex;
    // Celebration hex lists
    private List<HexCell> celebrationTopLeft;
    private List<HexCell> celebrationTopRight;
    private List<HexCell> celebrationBottomLeft;
    private List<HexCell> celebrationBottomRight;
    // Reset formation lists
    private List<HexCell> resetFormationLeft;
    private List<HexCell> resetFormationRight;
    public bool defendersAreBack = false;
    public bool attackersAreBack = false;
    private GoalInstructionPhase instructionPhase = GoalInstructionPhase.None;
    private bool goalScoringTeamIsHome;
    private string goalScoringTeamName = string.Empty;
    private string goalScorerName = string.Empty;
    private string goalAssisterName = string.Empty;
    private int scorerGoalCount;
    private readonly Dictionary<PlayerToken, HexCell> plannedPostGoalResetHexes = new();
    private bool postGoalResetFinalized = false;
    private bool suppressPostGoalResetAfterCelebration = false;
    private Action postGoalResetSuppressedCallback;
    
    private void Start()
    {
        StartCoroutine(WaitUntilHexGridIsReady());
    }
    private IEnumerator WaitUntilHexGridIsReady()
    {
        HexGrid hexGriditem = this.hexGrid;
        yield return new WaitUntil(() => hexGriditem != null && hexGriditem.IsGridInitialized());  // Check if grid is ready
        // Initialize hex lists (Replace with actual predefined lists)
        celebrationTopLeft = GenerateTopLeftHexList();
        celebrationTopRight = GenerateTopRightHexList();
        celebrationBottomLeft = GenerateBottomLeftHexList();
        celebrationBottomRight = GenerateBottomRightHexList();
        resetFormationLeft = GenerateResetLeft();
        resetFormationRight = GenerateResetRight();
    }
    
    private List<HexCell> GenerateTopLeftHexList()
    {
        List<HexCell> list = new()
        {
            hexGrid.GetHexCellAt(new Vector3Int(-18, 0, 12)),
            hexGrid.GetHexCellAt(new Vector3Int(-18, 0, 11)),
            hexGrid.GetHexCellAt(new Vector3Int(-18, 0, 10)),
            hexGrid.GetHexCellAt(new Vector3Int(-18, 0, 9)),
            hexGrid.GetHexCellAt(new Vector3Int(-17, 0, 12)),
            hexGrid.GetHexCellAt(new Vector3Int(-17, 0, 11)),
            hexGrid.GetHexCellAt(new Vector3Int(-17, 0, 10)),
            hexGrid.GetHexCellAt(new Vector3Int(-17, 0, 9)),
            hexGrid.GetHexCellAt(new Vector3Int(-16, 0, 12)),
            hexGrid.GetHexCellAt(new Vector3Int(-16, 0, 11)),
            hexGrid.GetHexCellAt(new Vector3Int(-16, 0, 10))
        };
        return list;
    }
    
    private List<HexCell> GenerateTopRightHexList()
    {
        List<HexCell> list = new List<HexCell>
        {
            hexGrid.GetHexCellAt(new Vector3Int(18, 0, 12)),
            hexGrid.GetHexCellAt(new Vector3Int(18, 0, 10)),
            hexGrid.GetHexCellAt(new Vector3Int(18, 0, 8)),
            hexGrid.GetHexCellAt(new Vector3Int(18, 0, 6)),
            hexGrid.GetHexCellAt(new Vector3Int(16, 0, 12)),
            hexGrid.GetHexCellAt(new Vector3Int(16, 0, 10)),
            hexGrid.GetHexCellAt(new Vector3Int(16, 0, 8)),
            hexGrid.GetHexCellAt(new Vector3Int(16, 0, 6)),
            hexGrid.GetHexCellAt(new Vector3Int(14, 0, 12)),
            hexGrid.GetHexCellAt(new Vector3Int(14, 0, 10)),
            hexGrid.GetHexCellAt(new Vector3Int(14, 0, 8))
        };
        return list;
    }
    
    private List<HexCell> GenerateBottomLeftHexList()
    {
        List<HexCell> list = new List<HexCell>
        {
            hexGrid.GetHexCellAt(new Vector3Int(-18, 0, -12)),
            hexGrid.GetHexCellAt(new Vector3Int(-18, 0, -10)),
            hexGrid.GetHexCellAt(new Vector3Int(-18, 0, -8)),
            hexGrid.GetHexCellAt(new Vector3Int(-18, 0, -6)),
            hexGrid.GetHexCellAt(new Vector3Int(-16, 0, -12)),
            hexGrid.GetHexCellAt(new Vector3Int(-16, 0, -10)),
            hexGrid.GetHexCellAt(new Vector3Int(-16, 0, -8)),
            hexGrid.GetHexCellAt(new Vector3Int(-16, 0, -6)),
            hexGrid.GetHexCellAt(new Vector3Int(-14, 0, -12)),
            hexGrid.GetHexCellAt(new Vector3Int(-14, 0, -10)),
            hexGrid.GetHexCellAt(new Vector3Int(-14, 0, -8))
        };
        return list;
    }
    
    private List<HexCell> GenerateBottomRightHexList()
    {
        List<HexCell> list = new List<HexCell>
        {
            hexGrid.GetHexCellAt(new Vector3Int(18, 0, -12)),
            hexGrid.GetHexCellAt(new Vector3Int(18, 0, -10)),
            hexGrid.GetHexCellAt(new Vector3Int(18, 0, -8)),
            hexGrid.GetHexCellAt(new Vector3Int(18, 0, -6)),
            hexGrid.GetHexCellAt(new Vector3Int(16, 0, -12)),
            hexGrid.GetHexCellAt(new Vector3Int(16, 0, -10)),
            hexGrid.GetHexCellAt(new Vector3Int(16, 0, -8)),
            hexGrid.GetHexCellAt(new Vector3Int(16, 0, -6)),
            hexGrid.GetHexCellAt(new Vector3Int(14, 0, -12)),
            hexGrid.GetHexCellAt(new Vector3Int(14, 0, -10)),
            hexGrid.GetHexCellAt(new Vector3Int(14, 0, -8))
        };
        return list;
    }
    
    private List<HexCell> GenerateResetLeft()
    {
        List<HexCell> list = new List<HexCell>
        {
            hexGrid.GetHexCellAt(new Vector3Int(-16, 0, 0)), // 1
            hexGrid.GetHexCellAt(new Vector3Int(-10, 0, -8)), // 2
            hexGrid.GetHexCellAt(new Vector3Int(-10, 0, 8)), // 3
            hexGrid.GetHexCellAt(new Vector3Int(-10, 0, 4)), // 4
            hexGrid.GetHexCellAt(new Vector3Int(-10, 0, -4)), // 5
            hexGrid.GetHexCellAt(new Vector3Int(-4, 0, -4)), // 6
            hexGrid.GetHexCellAt(new Vector3Int(-4, 0, -8)), // 7 RM
            hexGrid.GetHexCellAt(new Vector3Int(-4, 0, 4)), // 8
            hexGrid.GetHexCellAt(new Vector3Int(-2, 0, 3)), // 9
            hexGrid.GetHexCellAt(new Vector3Int(-2, 0, -3)), // 10
            hexGrid.GetHexCellAt(new Vector3Int(-4, 0, 8)) // 11 LM
        };
        return list;
    }
    
    private List<HexCell> GenerateResetRight()
    {
        List<HexCell> list = new List<HexCell>
        {
            hexGrid.GetHexCellAt(new Vector3Int(16, 0, 0)), // 1
            hexGrid.GetHexCellAt(new Vector3Int(10, 0, 8)), // 2
            hexGrid.GetHexCellAt(new Vector3Int(10, 0, -8)), // 3
            hexGrid.GetHexCellAt(new Vector3Int(10, 0, -4)), // 4
            hexGrid.GetHexCellAt(new Vector3Int(10, 0, 4)), // 5
            hexGrid.GetHexCellAt(new Vector3Int(4, 0, 4)), // 6
            hexGrid.GetHexCellAt(new Vector3Int(4, 0, 8)), // 7 RM
            hexGrid.GetHexCellAt(new Vector3Int(4, 0, -4)), // 8
            hexGrid.GetHexCellAt(new Vector3Int(2, 0, 3)), // 9
            hexGrid.GetHexCellAt(new Vector3Int(2, 0, -3)), // 10
            hexGrid.GetHexCellAt(new Vector3Int(4, 0, -8)) // 11 LM
        };
        return list;
    }

    public IEnumerator MoveTeamsToHalfTimeResetFormation(bool attackingTeamIsHome)
    {
        EnsureResetFormationsReady();
        plannedPostGoalResetHexes.Clear();

        MatchManager matchManager = MatchManager.Instance;
        if (matchManager == null)
        {
            Debug.LogError("[GoalFlow] Cannot run half-time reset because MatchManager is missing.");
            yield break;
        }

        List<HexCell> homeResetHexes = GetResetFormationForDirection(matchManager.homeTeamDirection);
        List<HexCell> awayResetHexes = GetResetFormationForDirection(matchManager.awayTeamDirection);
        Coroutine homeMove = StartCoroutine(MovePlayersToHexes(GetAttackTokens(true), homeResetHexes, false, true, true));
        Coroutine awayMove = StartCoroutine(MovePlayersToHexes(GetAttackTokens(false), awayResetHexes, false, true, true));
        yield return homeMove;
        yield return awayMove;
        ReconcilePostGoalTokenHexes(attackingTeamIsHome);
    }

    private void EnsureResetFormationsReady()
    {
        if (resetFormationLeft == null || resetFormationLeft.Count == 0)
        {
            resetFormationLeft = GenerateResetLeft();
        }

        if (resetFormationRight == null || resetFormationRight.Count == 0)
        {
            resetFormationRight = GenerateResetRight();
        }
    }

    private List<HexCell> GetResetFormationForDirection(MatchManager.TeamAttackingDirection direction)
    {
        return direction == MatchManager.TeamAttackingDirection.LeftToRight
            ? resetFormationLeft
            : resetFormationRight;
    }
  
    public void StartGoalFlow(PlayerToken shooterToken)
    {
        StartGoalFlow(shooterToken, null);
    }

    public void StartGoalFlow(PlayerToken shooterToken, HexCell scoredGoalHex)
    {
        // TODO: This should clean up everything from all Managers.
        activeGoalHex = scoredGoalHex;
        activeGoalSide = ResolveScoredGoalSide(shooterToken, scoredGoalHex);
        LastCompletedGoalHex = scoredGoalHex;
        LastCompletedGoalSide = activeGoalSide;
        isActivated = true;
        attackersAreBack = false;
        defendersAreBack = false;
        plannedPostGoalResetHexes.Clear();
        postGoalResetFinalized = false;
        suppressPostGoalResetAfterCelebration = false;
        postGoalResetSuppressedCallback = null;
        CaptureGoalInstructionContext(shooterToken);
        instructionPhase = GoalInstructionPhase.Celebration;
        hexGrid.RemoveHighlightsFromAllHexes();
        string shooterName = shooterToken != null ? shooterToken.name : "Unknown scorer";
        Debug.Log($"GOAL! {shooterName} scores! Starting celebration on goal side {activeGoalSide}.");
        StartCoroutine(DefenseCelebrationFlow(shooterToken, activeGoalSide));
        StartCoroutine(AttackCelebrationFlow(shooterToken, activeGoalSide));
        
        
    }

    private IEnumerator AttackCelebrationFlow(PlayerToken shooterToken, int scoredGoalSide)
    {
        // 4️⃣ GK joins if after 85’ and team scored match-winner (basic logic here)
        // if (MatchManager.Instance.minutesPassed >= 85)
        // {
        //     PlayerToken gk = celebratingPlayers.FirstOrDefault(p => p.isGoalkeeper);
        //     if (gk != null)
        //     {
        //         Debug.Log($"🧤 GK {gk.name} joins the celebration!");
        //         yield return StartCoroutine(MovePlayersToHexes(new List<PlayerToken> { gk }, celebrationHexes));
        //     }
        // }

        // 1️⃣ Determine which corner flag the players should run to
        List<HexCell> celebrationHexes = GetCelebrationHexes(scoredGoalSide, activeGoalHex, shooterToken);
        List<HexCell> attackerResetHexes = scoredGoalSide > 0 ? resetFormationLeft : resetFormationRight;
        // 2️⃣ Get all attacking teammates
        List<PlayerToken> attackers = GetAttackTokens(shooterToken.isHomeTeam);
        // 3️⃣ Move all attackers to their celebration positions
        yield return StartCoroutine(MovePlayersToHexes(attackers, celebrationHexes, true, false));
        // 4️⃣ Wait a bit to celebrate

        yield return new WaitForSeconds(1); // Small pause for celebration
        if (suppressPostGoalResetAfterCelebration)
        {
            attackersAreBack = true;
            yield return new WaitUntil(() => defendersAreBack);
            CompleteGoalFlowWithoutPostGoalReset();
            yield break;
        }

        instructionPhase = GoalInstructionPhase.Reset;
        Debug.Log("Waited for 1 second, going back!");
        // 6️⃣ Move attackers back to their reset positions
        yield return StartCoroutine(MovePlayersToHexes(attackers, attackerResetHexes, false, false, true));
        attackersAreBack = true;
        yield return new WaitUntil(() => defendersAreBack);
        FinalizePostGoalReset(shooterToken);
    }

    private IEnumerator DefenseCelebrationFlow(PlayerToken shooterToken, int scoredGoalSide)
    {
        // 1️⃣ Get all defender Tokens
        List<PlayerToken> defenders = GetAttackTokens(!shooterToken.isHomeTeam);
        // 2️⃣ Get the hexes where they should reset
        List<HexCell> defenderResetHexes = scoredGoalSide > 0 ? resetFormationRight : resetFormationLeft;
        // 3️⃣ Wait and cry!
        yield return new WaitForSeconds(0); // Small pause for disappointment
        if (suppressPostGoalResetAfterCelebration)
        {
            defendersAreBack = true;
            yield break;
        }

        // 4️⃣ Move defenders to their reset positions
        // TeleportPlayersToHexes(defenders, defenderResetHexes);
        yield return StartCoroutine(MovePlayersToHexes(defenders, defenderResetHexes, false, true, true));
        defendersAreBack = true;
    }

    public void CompleteAfterCelebrationWithoutPostGoalReset(Action onComplete)
    {
        suppressPostGoalResetAfterCelebration = true;
        postGoalResetSuppressedCallback = onComplete;
    }

    private void CompleteGoalFlowWithoutPostGoalReset()
    {
        if (postGoalResetFinalized)
        {
            return;
        }

        postGoalResetFinalized = true;
        Action onComplete = postGoalResetSuppressedCallback;
        Debug.Log("[GoalFlow] Goal celebration complete. Post-goal kickoff reset suppressed because the half or match has ended.");
        CleanUpGoalFlow();
        onComplete?.Invoke();
    }

    private void FinalizePostGoalReset(PlayerToken scorerToken)
    {
        if (postGoalResetFinalized)
        {
            return;
        }

        postGoalResetFinalized = true;
        MatchManager matchManager = MatchManager.Instance;
        bool kickoffTeamIsHome = scorerToken != null ? !scorerToken.isHomeTeam : !goalScoringTeamIsHome;

        if (matchManager != null)
        {
            matchManager.ClearGoalKickRestartTaker();
            matchManager.teamInAttack = kickoffTeamIsHome
                ? MatchManager.TeamInAttack.Home
                : MatchManager.TeamInAttack.Away;
            matchManager.attackHasPossession = true;
            matchManager.ClearLastTokenChain();
        }

        ReconcilePostGoalTokenHexes(kickoffTeamIsHome);
        PlaceBallOnKickoffHex();

        if (matchManager != null)
        {
            matchManager.currentState = MatchManager.GameState.PostGoalKickOffSetup;
        }

        kickoffManager.StartPostGoalKickoffSetupPhase();
        Debug.Log($"[GoalFlow] Post-goal reset complete. {(kickoffTeamIsHome ? "Home" : "Away")} team is attacking for kickoff.");
        CleanUpGoalFlow();
    }

    private void ReconcilePostGoalTokenHexes(bool attackingTeamIsHome)
    {
        if (hexGrid == null || hexGrid.cells == null || playerTokenManager == null)
        {
            Debug.LogError("[GoalFlow] Cannot reconcile post-goal reset because grid or token manager is missing.");
            return;
        }

        ClearAllHexOccupancy();

        HashSet<HexCell> claimedHexes = new HashSet<HexCell>();
        foreach (PlayerToken token in playerTokenManager.allTokens)
        {
            if (token == null)
            {
                continue;
            }

            HexCell targetHex = ResolvePostGoalTargetHex(token, claimedHexes);
            if (targetHex == null)
            {
                Debug.LogError($"[GoalFlow] Could not resolve a post-goal hex for {token.name}.");
                continue;
            }

            claimedHexes.Add(targetHex);
            bool tokenIsAttacker = token.isHomeTeam == attackingTeamIsHome;
            token.isAttacker = tokenIsAttacker;
            targetHex.isAttackOccupied = tokenIsAttacker;
            targetHex.isDefenseOccupied = !tokenIsAttacker;
            token.SetCurrentHex(targetHex);
            targetHex.ResetHighlight();
            targetHex.HighlightHex(tokenIsAttacker ? "isAttackOccupied" : "isDefenseOccupied");
        }

        ValidatePostGoalReconciliation();
    }

    private void ClearAllHexOccupancy()
    {
        foreach (HexCell hex in hexGrid.cells)
        {
            if (hex == null)
            {
                continue;
            }

            hex.occupyingToken = null;
            hex.isAttackOccupied = false;
            hex.isDefenseOccupied = false;
            hex.ResetHighlight();
        }
    }

    private HexCell ResolvePostGoalTargetHex(PlayerToken token, HashSet<HexCell> claimedHexes)
    {
        if (plannedPostGoalResetHexes.TryGetValue(token, out HexCell plannedHex)
            && IsAvailablePostGoalHex(plannedHex, claimedHexes))
        {
            return plannedHex;
        }

        HexCell currentHex = token.GetCurrentHex();
        if (IsAvailablePostGoalHex(currentHex, claimedHexes))
        {
            return currentHex;
        }

        HexCell positionHex = GetHexAtTokenPosition(token);
        if (IsAvailablePostGoalHex(positionHex, claimedHexes))
        {
            return positionHex;
        }

        foreach (HexCell resetHex in GetResetHexesForTeamAfterGoal(token.isHomeTeam))
        {
            if (IsAvailablePostGoalHex(resetHex, claimedHexes))
            {
                return resetHex;
            }
        }

        return FindNearestAvailablePostGoalHex(token, claimedHexes);
    }

    private IEnumerable<HexCell> GetResetHexesForTeamAfterGoal(bool isHomeTeam)
    {
        List<HexCell> scoringTeamResetHexes = activeGoalSide > 0 ? resetFormationLeft : resetFormationRight;
        List<HexCell> concedingTeamResetHexes = activeGoalSide > 0 ? resetFormationRight : resetFormationLeft;
        return isHomeTeam == goalScoringTeamIsHome ? scoringTeamResetHexes : concedingTeamResetHexes;
    }

    private HexCell GetHexAtTokenPosition(PlayerToken token)
    {
        if (token == null || hexGrid == null)
        {
            return null;
        }

        Vector3Int coordinates = hexGrid.WorldToHexCoords(token.transform.position);
        return hexGrid.GetHexCellAt(coordinates);
    }

    private HexCell FindNearestAvailablePostGoalHex(PlayerToken token, HashSet<HexCell> claimedHexes)
    {
        HexCell nearestHex = null;
        float nearestDistance = float.PositiveInfinity;
        foreach (HexCell hex in hexGrid.cells)
        {
            if (!IsAvailablePostGoalHex(hex, claimedHexes))
            {
                continue;
            }

            float distance = Vector3.SqrMagnitude(hex.GetHexCenter() - token.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestHex = hex;
            }
        }

        return nearestHex;
    }

    private bool IsAvailablePostGoalHex(HexCell hex, HashSet<HexCell> claimedHexes)
    {
        return hex != null
            && !claimedHexes.Contains(hex)
            && !hex.isOutOfBounds
            && hex.isInGoal == 0;
    }

    private void ValidatePostGoalReconciliation()
    {
        int issueCount = 0;
        foreach (PlayerToken token in playerTokenManager.allTokens)
        {
            if (token == null)
            {
                continue;
            }

            HexCell tokenHex = token.GetCurrentHex();
            if (tokenHex == null)
            {
                issueCount++;
                Debug.LogError($"[GoalFlow] {token.name} has no current hex after post-goal reconciliation.");
                continue;
            }

            if (tokenHex.GetOccupyingToken() != token)
            {
                issueCount++;
                Debug.LogError($"[GoalFlow] {token.name} points to {tokenHex.coordinates}, but that hex does not point back to the token.");
            }
        }

        foreach (HexCell hex in hexGrid.cells)
        {
            if (hex == null || hex.GetOccupyingToken() == null)
            {
                continue;
            }

            if (hex.GetOccupyingToken().GetCurrentHex() != hex)
            {
                issueCount++;
                Debug.LogError($"[GoalFlow] Hex {hex.coordinates} points to {hex.GetOccupyingToken().name}, but the token points elsewhere.");
            }
        }

        if (issueCount == 0)
        {
            Debug.Log("[GoalFlow] Token/hex ownership reconciled cleanly after goal reset.");
        }
    }

    private void PlaceBallOnKickoffHex()
    {
        HexCell kickoffHex = hexGrid.GetHexCellAt(new Vector3Int(0, 0, 0));
        if (kickoffHex == null)
        {
            Debug.LogError("[GoalFlow] Cannot place ball on kickoff because hex (0, 0) was not found.");
            return;
        }

        Ball resetBall = groundBallManager != null ? groundBallManager.ball : MatchManager.Instance?.ball;
        if (resetBall == null)
        {
            Debug.LogError("[GoalFlow] Cannot place ball on kickoff because no ball reference is available.");
            return;
        }

        resetBall.PlaceAtCell(kickoffHex);
        MatchManager.Instance?.ClearGoalKickRestartTaker();
    }

    private void CleanUpGoalFlow()
    {
        isActivated = false;
        attackersAreBack = false;
        defendersAreBack = false;
        instructionPhase = GoalInstructionPhase.None;
        activeGoalSide = 0;
        activeGoalHex = null;
        goalScoringTeamName = string.Empty;
        goalScorerName = string.Empty;
        goalAssisterName = string.Empty;
        scorerGoalCount = 0;
        plannedPostGoalResetHexes.Clear();
        suppressPostGoalResetAfterCelebration = false;
        postGoalResetSuppressedCallback = null;
    }

    public string GetInstructions()
    {
        return instructionPhase switch
        {
            GoalInstructionPhase.Celebration => $"GOAL FOR {goalScoringTeamName}!!!",
            GoalInstructionPhase.Reset => BuildGoalResetInstruction(),
            _ => string.Empty,
        };
    }

    public bool? IsInstructionExpectingHomeTeam()
    {
        if (instructionPhase == GoalInstructionPhase.None)
        {
            return null;
        }

        return goalScoringTeamIsHome;
    }

    public bool ShouldFlashInstructionColors()
    {
        return instructionPhase == GoalInstructionPhase.Celebration;
    }

    private void CaptureGoalInstructionContext(PlayerToken shooterToken)
    {
        if (shooterToken == null)
        {
            goalScoringTeamIsHome = true;
            goalScoringTeamName = "Unknown Team";
            goalScorerName = "Unknown Scorer";
            goalAssisterName = string.Empty;
            scorerGoalCount = 1;
            return;
        }

        goalScoringTeamIsHome = shooterToken.isHomeTeam;
        goalScoringTeamName = GetTeamName(shooterToken.isHomeTeam);
        goalScorerName = string.IsNullOrWhiteSpace(shooterToken.playerName) ? shooterToken.name : shooterToken.playerName;
        goalAssisterName = ResolveAssisterName(shooterToken);
        scorerGoalCount = CountGoalsByScorer(goalScorerName, shooterToken.isHomeTeam);
    }

    private string BuildGoalResetInstruction()
    {
        string ordinalText = GetGoalOrdinalText(scorerGoalCount);
        string scorerText = string.IsNullOrWhiteSpace(ordinalText)
            ? $"Goal by {goalScorerName}!"
            : $"{ordinalText} Goal by {goalScorerName}!";
        string assistText = string.IsNullOrWhiteSpace(goalAssisterName)
            ? string.Empty
            : $" Assisted by {goalAssisterName}!";

        return $"{scorerText}{assistText} {GetCurrentScoreText()}";
    }

    private string GetGoalOrdinalText(int goalCount)
    {
        return goalCount switch
        {
            2 => $"{goalCount}) {GetBraceLabel()}",
            3 => $"{goalCount}) Hat trick",
            4 => $"{goalCount}) Poker",
            5 => $"{goalCount}) Re-Poker",
            6 => $"{goalCount}) Double Hat trick",
            > 6 => $"{goalCount}) Double Hat trick",
            _ => string.Empty,
        };
    }

    private string GetBraceLabel()
    {
        // Placeholder for a future nationality field on PlayerToken/RosterPlayer:
        // return scorerIsItalian ? "Doppietta" : "Brace";
        return "Brace";
    }

    private string ResolveAssisterName(PlayerToken shooterToken)
    {
        PlayerToken assistToken = MatchManager.Instance?.PreviousTokenToTouchTheBallOnPurpose;
        if (assistToken == null || assistToken == shooterToken || assistToken.isHomeTeam != shooterToken.isHomeTeam)
        {
            return string.Empty;
        }

        return string.IsNullOrWhiteSpace(assistToken.playerName) ? assistToken.name : assistToken.playerName;
    }

    private int CountGoalsByScorer(string scorerName, bool isHomeTeam)
    {
        List<MatchManager.GoalEvent> goals = isHomeTeam
            ? MatchManager.Instance?.homeScorers
            : MatchManager.Instance?.awayScorers;

        if (goals == null || string.IsNullOrWhiteSpace(scorerName))
        {
            return 1;
        }

        int count = goals.Count(goal => goal != null && goal.scorer == scorerName);
        return Mathf.Max(1, count);
    }

    private string GetTeamName(bool isHomeTeam)
    {
        MatchManager.GameSettings settings = MatchManager.Instance?.gameData?.gameSettings;
        if (settings == null)
        {
            return isHomeTeam ? "Home" : "Away";
        }

        string teamName = isHomeTeam ? settings.homeTeamName : settings.awayTeamName;
        return string.IsNullOrWhiteSpace(teamName) ? (isHomeTeam ? "Home" : "Away") : teamName;
    }

    private string GetCurrentScoreText()
    {
        MatchManager matchManager = MatchManager.Instance;
        if (matchManager?.gameData?.stats == null)
        {
            return string.Empty;
        }

        string homeTeamName = GetTeamName(true);
        string awayTeamName = GetTeamName(false);
        int homeGoals = matchManager.gameData.stats.homeTeamStats.totalGoals;
        int awayGoals = matchManager.gameData.stats.awayTeamStats.totalGoals;
        return $"{homeTeamName} {homeGoals} - {awayGoals} {awayTeamName}";
    }

    // Determines the celebration hex list based on scorer's position
    private int ResolveScoredGoalSide(PlayerToken scorer, HexCell scoredGoalHex)
    {
        if (scoredGoalHex != null && scoredGoalHex.isInGoal != 0)
        {
            return scoredGoalHex.isInGoal;
        }

        if (scorer == null)
        {
            return 0;
        }

        MatchManager.TeamAttackingDirection scorerDirection = scorer.isHomeTeam
            ? MatchManager.Instance.homeTeamDirection
            : MatchManager.Instance.awayTeamDirection;

        return scorerDirection == MatchManager.TeamAttackingDirection.LeftToRight ? 1 : -1;
    }

    private List<HexCell> GetCelebrationHexes(int scoredGoalSide, HexCell scoredGoalHex, PlayerToken scorer)
    {
        int z = scoredGoalHex != null ? scoredGoalHex.coordinates.z : scorer.GetCurrentHex().coordinates.z;
        if (z > 0)
            return scoredGoalSide > 0 ? celebrationTopRight : celebrationTopLeft;
        else
            return scoredGoalSide > 0 ? celebrationBottomRight : celebrationBottomLeft;
    }

    private void TeleportPlayersToHexes(List<PlayerToken> players, List<HexCell> targetHexes)
    {
        if (players.Count > targetHexes.Count)
        {
            Debug.LogWarning($"[GoalFlow] Not enough target hexes ({targetHexes.Count}) for all players ({players.Count})!");
        }

        Debug.Log($"[GoalFlow] Instantly moving {players.Count} players to {targetHexes.Count} hexes.");

        for (int i = 0; i < players.Count; i++)
        {
            PlayerToken player = players[i];

            if (player == null)
            {
                Debug.LogError($"[GoalFlow] Player at index {i} is NULL!");
                continue;
            }

            HexCell targetHex = targetHexes[Mathf.Min(i, targetHexes.Count - 1)];

            Debug.Log($"[GoalFlow] Teleporting {player.name} to Hex {targetHex.coordinates}");

            player.SetCurrentHex(targetHex);
            player.transform.position = targetHex.transform.position; // Instantly move the GameObject
        }
    }

    // Moves all players in a team to their target hexes
    private IEnumerator MovePlayersToHexes(
        List<PlayerToken> players,
        List<HexCell> targetHexes,
        bool isForward,
        bool isDefense,
        bool recordPostGoalResetHexes = false)
    {
        if (players.Count > targetHexes.Count)
        {
            Debug.LogWarning($"[GoalFlow] Not enough target hexes ({targetHexes.Count}) for all players ({players.Count})!");
        }
        // 🛠 Shuffle the hex list so it's randomized (avoids unnatural movement patterns)
        List<HexCell> shuffledHexes = targetHexes;
        if (isForward)
        {
            shuffledHexes = targetHexes.OrderBy(h => UnityEngine.Random.value).ToList();
        }

        // Store assigned hexes to prevent double assignments
        HashSet<HexCell> assignedHexes = new HashSet<HexCell>();
        List<Coroutine> movementCoroutines = new List<Coroutine>();
        for (int i = 0; i < players.Count; i++)
        {
            PlayerToken player = players[i];
            if (player == null)
            {
                Debug.LogError($"[GoalFlow] Player at index {i} is NULL!");
                continue;
            }
            // Find the first available hex that is not occupied
            HexCell targetHex = shuffledHexes.FirstOrDefault(h => !assignedHexes.Contains(h));

            if (targetHex == null)
            {
                Debug.LogError($"[GoalFlow] Target Hex at index {i} is NULL!");
                continue;
            }
            assignedHexes.Add(targetHex); // Mark this hex as taken
            if (recordPostGoalResetHexes)
            {
                plannedPostGoalResetHexes[player] = targetHex;
            }
            // Debug.Log($"[GoalFlow] Moving {players[i].name} to Hex {targetHexes[i].coordinates}");
            // Start moving everyone at the same time
            // Coroutine moveCoroutine = StartCoroutine(movementPhaseManager.MoveTokenToHex(targetHexes[i], players[i], false, false));
            Coroutine moveCoroutine = StartCoroutine(MoveTokenStraightToHex(player, targetHex, isForward, isDefense));
            movementCoroutines.Add(moveCoroutine);
        }
        // Wait for all coroutines to finish before moving forward
        foreach (var coroutine in movementCoroutines)
        {
            yield return coroutine;
        }
    }

    private IEnumerator MoveTokenStraightToHex(PlayerToken token, HexCell targetHex, bool isForward, bool isDefense)
    {
        Vector3 startPos = token.transform.position;
        Vector3 endPos = targetHex.transform.position;

        float fixedY = startPos.y; // Lock this value

        // Apply fixed Y to end position too
        endPos.y = fixedY;

        float distance = Vector3.Distance(startPos, endPos);
        float speed = 2f;

        if (!isDefense)
        {
            if (isForward)
            {
                speed *= (1 + (token.pace - 3) * 0.3f);
            }
        }
        else
        {
            speed *= 0.7f;
        }

        float duration = distance / speed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            Vector3 interpolated = Vector3.Lerp(startPos, endPos, t);
            interpolated.y = fixedY;  // Force Y again
            token.transform.position = interpolated;

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Final position with fixed Y
        Vector3 finalPosition = endPos;
        finalPosition.y = fixedY;
        token.transform.position = finalPosition;

        token.SetCurrentHex(targetHex);

        // Debug.Log($"✅ Token {token.name} moved to {finalPosition} (target hex: {targetHex.name})");
    }

    // Retrieves all tokens belonging to a specific team
    private List<PlayerToken> GetAttackTokens(bool teamID)
    {
        return playerTokenManager.allTokens.Where(token => token.isHomeTeam == teamID).ToList();
    }

    // private IEnumerator JumpPlayer(PlayerToken token, float height = 2f, float totalDuration = 0.6f)
    // {
    //     float halfDuration = totalDuration / 2f;
    //     float elapsed = 0f;
    //     Vector3 startPos = token.transform.position;

    //     // Jump up
    //     while (elapsed < halfDuration)
    //     {
    //         float progress = elapsed / halfDuration;
    //         float yOffset = Mathf.Lerp(0, height, progress);
    //         token.transform.position = new Vector3(startPos.x, startPos.y + yOffset, startPos.z);
    //         elapsed += Time.deltaTime;
    //         yield return null;
    //     }

    //     // Jump down
    //     elapsed = 0f;
    //     while (elapsed < halfDuration)
    //     {
    //         float progress = elapsed / halfDuration;
    //         float yOffset = Mathf.Lerp(height, 0, progress);
    //         token.transform.position = new Vector3(startPos.x, startPos.y + yOffset, startPos.z);
    //         elapsed += Time.deltaTime;
    //         yield return null;
    //     }

    //     // Ensure final position is exactly the start
    //     token.transform.position = startPos;
    // }
}
