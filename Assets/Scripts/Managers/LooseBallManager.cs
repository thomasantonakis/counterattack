using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public enum LooseBallSourceType
{
    GroundDeflection,
    HeaderDeflection,
    GoalkeeperHandlingSpill,
    InaccuratePass,
}

public class LooseBallManager : MonoBehaviour
{
    [Header("Dependencies")]
    public HexGrid hexGrid;
    public Ball ball;
    public OutOfBoundsManager outOfBoundsManager;
    public LongBallManager longBallManager;
    public GroundBallManager groundBallManager;
    public MovementPhaseManager movementPhaseManager;
    public HeaderManager headerManager;
    public FinalThirdManager finalThirdManager;
    public ShotManager shotManager;
    public GoalFlowManager goalFlowManager;
    public GoalKeeperManager goalKeeperManager;
    public HelperFunctions helperFunctions;
    [Header("Flags")]
    public bool isActivated = false;
    public bool isWaitingForDirectionRoll = false;
    public bool isWaitingForDistanceRoll = false;
    public bool isWaitingForInterceptionRoll = false;
    public int directionRoll = 240885;
    public int distanceRoll = 0;
    public int interceptionRoll = 0;
    [Header("Important Things")]
    public List<PlayerToken> defendersTriedToIntercept;
    public List<HexCell> path = new List<HexCell>();
    // public HexCell checkedHex = null;
    public PlayerToken causingDeflection;
    public PlayerToken ballHitThisToken;
    public PlayerToken potentialInterceptor;
    private bool nextLooseBallGoalIsPenalty;

    public struct InaccuracyTargetResult
    {
        public HexCell StartHex;
        public HexCell FinalHex;
        public int DirectionIndex;
        public int Distance;

        public bool IsValid => FinalHex != null;
        public bool IsOutOfBounds => FinalHex != null && FinalHex.isOutOfBounds;

        public InaccuracyTargetResult(HexCell startHex, HexCell finalHex, int directionIndex, int distance)
        {
            StartHex = startHex;
            FinalHex = finalHex;
            DirectionIndex = directionIndex;
            Distance = distance;
        }
    }

    private sealed class LooseBallGkCheckpointResult
    {
        public bool GkMoveOffered;
        public bool RecoveredByGoalkeeper;
    }

    private bool IsHeaderLooseBall(LooseBallSourceType sourceType)
    {
        return sourceType == LooseBallSourceType.HeaderDeflection;
    }

    private bool IsGoalkeeperHandlingSpill(LooseBallSourceType sourceType)
    {
        return sourceType == LooseBallSourceType.GoalkeeperHandlingSpill;
    }

    public void MarkNextLooseBallGoalAsPenalty()
    {
        nextLooseBallGoalIsPenalty = true;
    }

    private bool TryHandleLooseBallGoal(HexCell goalHex, PlayerToken deflectingToken)
    {
        if (goalHex == null || goalHex.isInGoal == 0)
        {
            return false;
        }

        PlayerToken scoringToken = MatchManager.Instance.LastTokenToTouchTheBallOnPurpose;
        if (scoringToken == null)
        {
            scoringToken = deflectingToken;
        }

        if (scoringToken == null)
        {
            Debug.LogError($"Loose ball entered goal at {goalHex.coordinates}, but no scoring token could be identified.");
            return false;
        }

        GoalFlowManager resolvedGoalFlowManager = goalFlowManager != null ? goalFlowManager : FindAnyObjectByType<GoalFlowManager>();
        if (resolvedGoalFlowManager == null)
        {
            Debug.LogError("Loose ball entered goal, but GoalFlowManager is not linked.");
            return false;
        }

        if (deflectingToken != null && deflectingToken != scoringToken)
        {
            Debug.Log($"Loose ball entered the goal at {goalHex.coordinates} after a deflection by {deflectingToken.name}. Awarding the goal to {scoringToken.name}.");
        }
        else
        {
            Debug.Log($"Loose ball entered the goal at {goalHex.coordinates}. Awarding the goal to {scoringToken.name}.");
        }

        if (nextLooseBallGoalIsPenalty)
        {
            MatchManager.Instance.MarkNextGoalAsPenalty();
            nextLooseBallGoalIsPenalty = false;
        }

        MatchManager.Instance.gameData.gameLog.LogEvent(scoringToken, MatchManager.ActionType.GoalScored);
        MatchManager.Instance.ClearHangingPass();
        MatchManager.Instance.SetLastToken(scoringToken);
        if (movementPhaseManager != null && movementPhaseManager.isActivated)
        {
            movementPhaseManager.EndMovementPhase(false);
            movementPhaseManager.EndMovementPhase(false);
        }

        resolvedGoalFlowManager.StartGoalFlow(scoringToken, goalHex);
        return true;
    }

    private static HexCell FindFirstGoalHexInPath(List<HexCell> path)
    {
        if (path == null)
        {
            return null;
        }

        foreach (HexCell hex in path)
        {
            if (hex != null && hex.isInGoal != 0)
            {
                return hex;
            }
        }

        return null;
    }

    private string ResolveOutOfBoundsSource(PlayerToken lastTouchToken)
    {
        if (lastTouchToken == null)
        {
            return "inaccuracy";
        }

        bool attackingTeamTouched = MatchManager.Instance.teamInAttack == MatchManager.TeamInAttack.Home
            ? lastTouchToken.isHomeTeam
            : !lastTouchToken.isHomeTeam;

        return attackingTeamTouched ? "inaccuracy" : "defendertouch";
    }

    private bool ShouldAllowTokenImpactOnHex(LooseBallSourceType sourceType, int pathIndex, int pathCount)
    {
        if (sourceType == LooseBallSourceType.InaccuratePass)
        {
            return false;
        }

        if (IsHeaderLooseBall(sourceType))
        {
            return pathIndex == pathCount - 1;
        }

        return true;
    }

    private bool ShouldAllowInterceptionOnHex(LooseBallSourceType sourceType, int pathIndex, int pathCount)
    {
        if (sourceType == LooseBallSourceType.InaccuratePass)
        {
            return false;
        }

        if (IsHeaderLooseBall(sourceType))
        {
            return pathIndex == pathCount - 1;
        }

        return true;
    }

    private bool ShouldExtendPastJumpedToken(LooseBallSourceType sourceType)
    {
        return IsHeaderLooseBall(sourceType);
    }

    private bool ShouldClearPreviousChainOnCollection(LooseBallSourceType sourceType)
    {
        return sourceType != LooseBallSourceType.InaccuratePass;
    }

    private static bool ShouldOfferShortGroundBallAfterAttackerPickup(LooseBallSourceType sourceType, bool allowGKBoxMove)
    {
        return sourceType == LooseBallSourceType.GoalkeeperHandlingSpill
            || (sourceType == LooseBallSourceType.GroundDeflection && !allowGKBoxMove);
    }

    private bool IsTokenUnavailableForLooseBall(PlayerToken token)
    {
        return token == null
            || (MatchManager.Instance != null && !MatchManager.Instance.CanTokenCollectHangingPass(token))
            || movementPhaseManager.stunnedTokens.Contains(token)
            || movementPhaseManager.stunnedforNext.Contains(token)
            || headerManager.defenderWillJump.Contains(token)
            || headerManager.attackerWillJump.Contains(token);
    }

    private void AssignLooseBallReceiver(PlayerToken token, LooseBallSourceType sourceType)
    {
        if (ShouldClearPreviousChainOnCollection(sourceType))
        {
            MatchManager.Instance.SetLastTokenFromLooseBall(token);
            return;
        }

        MatchManager.Instance.ApplyBallCollectionOwnership(token);
    }

    private IEnumerator MoveLooseBallToHex(HexCell targetHex, LooseBallSourceType sourceType, bool allowGKBoxMove)
    {
        yield return StartCoroutine(groundBallManager.HandleGroundBallMovement(targetHex, allowGKBoxMove: allowGKBoxMove));
    }

    private IEnumerator MoveLooseBallUntilGKBoxMove(HexCell targetHex)
    {
        if (ball == null)
        {
            Debug.LogError("LooseBallManager cannot move the ball to the defensive GK checkpoint because Ball is not linked.");
            yield break;
        }

        yield return StartCoroutine(ball.MoveToCell(targetHex, null, true, stopAfterGKBoxMove: true));
    }

    private bool IsOpponentPenaltyBoxForCurrentPurposefulTouch(HexCell hex)
    {
        if (hex == null || hex.isInPenaltyBox == 0 || MatchManager.Instance == null)
        {
            return false;
        }

        PlayerToken lastPurposefulTouch = MatchManager.Instance.LastTokenToTouchTheBallOnPurpose;
        if (lastPurposefulTouch == null || !lastPurposefulTouch.isAttacker)
        {
            return false;
        }

        MatchManager.TeamAttackingDirection attackingDirection = lastPurposefulTouch.isHomeTeam
            ? MatchManager.Instance.homeTeamDirection
            : MatchManager.Instance.awayTeamDirection;

        return (attackingDirection == MatchManager.TeamAttackingDirection.LeftToRight && hex.isInPenaltyBox == 1)
            || (attackingDirection == MatchManager.TeamAttackingDirection.RightToLeft && hex.isInPenaltyBox == -1);
    }

    private HexCell FindFirstOpponentPenaltyBoxHex(List<HexCell> movementPath)
    {
        if (movementPath == null)
        {
            return null;
        }

        return movementPath.FirstOrDefault(IsOpponentPenaltyBoxForCurrentPurposefulTouch);
    }

    private bool ShouldOfferLooseBallGKBoxMove(PlayerToken startingToken, LooseBallSourceType sourceType, List<HexCell> movementPath)
    {
        if (sourceType != LooseBallSourceType.GroundDeflection || startingToken == null || startingToken.IsGoalKeeper)
        {
            return false;
        }

        if (hexGrid == null || ball == null || hexGrid.CheckPenaltyBox(ball.transform.position) != 0)
        {
            return false;
        }

        return FindFirstOpponentPenaltyBoxHex(movementPath) != null;
    }

    private bool ShouldOfferPreRollGKBoxMove(PlayerToken startingToken, LooseBallSourceType sourceType, bool allowGKBoxMove)
    {
        if (!allowGKBoxMove)
        {
            return false;
        }

        if (sourceType != LooseBallSourceType.GroundDeflection || startingToken == null || startingToken.IsGoalKeeper)
        {
            return false;
        }

        if (hexGrid == null || ball == null || hexGrid.CheckPenaltyBox(ball.transform.position) != 0)
        {
            return false;
        }

        return IsOpponentPenaltyBoxForCurrentPurposefulTouch(startingToken.GetCurrentHex());
    }

    private List<HexCell> GetRemainingLooseBallPathFromCurrentBallHex(List<HexCell> movementPath)
    {
        if (movementPath == null)
        {
            return new List<HexCell>();
        }

        List<HexCell> validPath = movementPath.Where(hex => hex != null).ToList();
        HexCell currentBallHex = ball != null ? ball.GetCurrentHex() : null;
        int currentIndex = currentBallHex != null ? validPath.IndexOf(currentBallHex) : -1;
        return currentIndex >= 0
            ? validPath.Skip(currentIndex).ToList()
            : validPath;
    }

    private bool TryFindDefendingGkDirectPickupHex(List<HexCell> movementPath, PlayerToken defendingGk, out HexCell pickupHex)
    {
        pickupHex = null;
        HexCell gkHex = defendingGk != null ? defendingGk.GetCurrentHex() : null;
        if (gkHex == null || movementPath == null)
        {
            return false;
        }

        pickupHex = movementPath.FirstOrDefault(hex => hex == gkHex);
        return pickupHex != null;
    }

    private bool TryFindDefendingGkInterceptionHex(List<HexCell> movementPath, PlayerToken defendingGk, out HexCell interceptionHex)
    {
        interceptionHex = null;
        HexCell gkHex = defendingGk != null ? defendingGk.GetCurrentHex() : null;
        if (gkHex == null || movementPath == null)
        {
            return false;
        }

        foreach (HexCell hex in movementPath)
        {
            if (hex == null || hex.isAttackOccupied || hex.isDefenseOccupied)
            {
                continue;
            }

            if (hex.GetNeighbors(hexGrid).Contains(gkHex))
            {
                interceptionHex = hex;
                return true;
            }
        }

        return false;
    }

    private void ResolveDefensiveLooseBallRecovery(PlayerToken recoveringToken, HexCell recoveryHex)
    {
        if (recoveringToken == null || recoveryHex == null)
        {
            Debug.LogError("Cannot resolve defensive loose-ball recovery with a null token or recovery hex.");
            return;
        }

        MatchManager.Instance.gameData.gameLog.LogEvent(
            recoveringToken,
            MatchManager.ActionType.BallRecovery,
            connectedToken: MatchManager.Instance.LastTokenToTouchTheBallOnPurpose,
            recoveryType: MatchManager.Instance.hangingPassType
        );
        MatchManager.Instance.ClearHangingPass();
        MatchManager.Instance.SetLastTokenFromLooseBall(recoveringToken);
        MatchManager.Instance.ChangePossession();
        MatchManager.Instance.UpdatePossessionAfterPass(recoveryHex);
        if (movementPhaseManager != null)
        {
            movementPhaseManager.EndMovementPhase(false);
        }
        MatchManager.Instance.BroadcastDefensiveRecoveryOutcome(recoveringToken, recoveryHex);
    }

    private IEnumerator OfferDefendingGKMoveAndRecoveryAfterInterceptions(
        PlayerToken startingToken,
        LooseBallSourceType sourceType,
        List<HexCell> movementPath,
        bool gkMoveAlreadyOffered,
        LooseBallGkCheckpointResult result)
    {
        if (result == null)
        {
            yield break;
        }

        result.GkMoveOffered = gkMoveAlreadyOffered;
        result.RecoveredByGoalkeeper = false;

        if (gkMoveAlreadyOffered || !ShouldOfferLooseBallGKBoxMove(startingToken, sourceType, movementPath))
        {
            yield break;
        }

        HexCell firstBoxHex = FindFirstOpponentPenaltyBoxHex(movementPath);
        HexCell movementTargetHex = movementPath != null ? movementPath.LastOrDefault(hex => hex != null) : null;
        if (firstBoxHex == null || movementTargetHex == null)
        {
            yield break;
        }

        Debug.Log($"Loose ball movement enters the opponent penalty box at {firstBoxHex.coordinates}. Stopping on the line for the defending GK free move before continuing.");
        yield return StartCoroutine(MoveLooseBallUntilGKBoxMove(movementTargetHex));
        result.GkMoveOffered = true;

        PlayerToken defendingGk = hexGrid != null ? hexGrid.GetDefendingGK() : null;
        if (defendingGk == null)
        {
            yield break;
        }

        List<HexCell> remainingPath = GetRemainingLooseBallPathFromCurrentBallHex(movementPath);
        if (TryFindDefendingGkDirectPickupHex(remainingPath, defendingGk, out HexCell pickupHex))
        {
            Debug.Log($"{defendingGk.name} moved directly onto the loose-ball path at {pickupHex.coordinates} and picks it up.");
            yield return StartCoroutine(MoveLooseBallToHex(pickupHex, sourceType, allowGKBoxMove: false));
            ResolveDefensiveLooseBallRecovery(defendingGk, pickupHex);
            result.RecoveredByGoalkeeper = true;
            yield break;
        }

        GoalKeeperManager resolvedGoalKeeperManager = goalKeeperManager != null
            ? goalKeeperManager
            : FindAnyObjectByType<GoalKeeperManager>();
        if (resolvedGoalKeeperManager != null)
        {
            goalKeeperManager = resolvedGoalKeeperManager;
        }

        if (resolvedGoalKeeperManager != null
            && resolvedGoalKeeperManager.TryFindFirstGoalkeeperWallHexOnPath(
                remainingPath,
                defendingGk,
                includeGoalkeeperHex: false,
                out HexCell wallSaveHex,
                out _,
                out int wallSavingPenalty))
        {
            potentialInterceptor = defendingGk;
            Debug.Log($"{defendingGk.name} can dive from the GK Wall at {wallSaveHex.coordinates} after the GK free move. Press [R] to roll.");
            MatchManager.Instance.gameData.gameLog.LogEvent(defendingGk, MatchManager.ActionType.SaveAttempt);

            isWaitingForInterceptionRoll = true;
            while (isWaitingForInterceptionRoll)
            {
                yield return null;
            }

            int savingTotal = interceptionRoll + defendingGk.saving + wallSavingPenalty;
            if (interceptionRoll == 6 || savingTotal >= 10)
            {
                Debug.Log($"{defendingGk.name} successfully catches the loose ball from the GK Wall.");
                yield return StartCoroutine(MoveLooseBallToHex(wallSaveHex, sourceType, allowGKBoxMove: false));
                yield return StartCoroutine(resolvedGoalKeeperManager.ResolveGoalkeeperSaveAndHold(defendingGk, wallSaveHex, "gkWall"));
                result.RecoveredByGoalkeeper = true;
                yield break;
            }

            Debug.Log($"{defendingGk.name} failed the GK Wall loose-ball save.");
            defendersTriedToIntercept.Add(defendingGk);
        }

        if (!TryFindDefendingGkInterceptionHex(remainingPath, defendingGk, out HexCell interceptionHex))
        {
            yield break;
        }

        potentialInterceptor = defendingGk;
        Debug.Log($"{defendingGk.name} can intercept the loose ball after the GK free move near {interceptionHex.coordinates}.");
        MatchManager.Instance.gameData.gameLog.LogExpectedRecovery(
            defendingGk,
            ExpectedStatsCalculator.CalculateRecoveryProbability(defendingGk),
            connectedToken: MatchManager.Instance.LastTokenToTouchTheBallOnPurpose,
            recoveryType: MatchManager.Instance.hangingPassType);
        MatchManager.Instance.gameData.gameLog.LogEvent(defendingGk, MatchManager.ActionType.InterceptionAttempt);

        isWaitingForInterceptionRoll = true;
        while (isWaitingForInterceptionRoll)
        {
            yield return null;
        }

        if (interceptionRoll == 6 || defendingGk.tackling + interceptionRoll >= 10)
        {
            HexCell gkHex = defendingGk.GetCurrentHex();
            Debug.Log($"{defendingGk.name} successfully intercepted the loose ball after the GK free move.");
            yield return StartCoroutine(MoveLooseBallToHex(gkHex, sourceType, allowGKBoxMove: false));
            ResolveDefensiveLooseBallRecovery(defendingGk, gkHex);
            result.RecoveredByGoalkeeper = true;
        }
        else
        {
            Debug.Log($"{defendingGk.name} failed to intercept the loose ball after the GK free move.");
            defendersTriedToIntercept.Add(defendingGk);
        }
    }

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
    }

    private void OnKeyReceived(KeyPressData keyData)
    {
        if (keyData.isConsumed) return;
        if (!isActivated) return;
        bool hasRollOverride = RollInputOverride.TryParse(keyData, out RollInputOverride rollOverride);
        if (isWaitingForDirectionRoll && (keyData.key == KeyCode.R || hasRollOverride))
        {
            PerformDirectionRoll(hasRollOverride ? rollOverride : null);
            keyData.isConsumed = true;
            return;
        }
        if (isWaitingForDistanceRoll && (keyData.key == KeyCode.R || hasRollOverride))
        {
            PerformDistanceRoll(hasRollOverride ? rollOverride : null);
            keyData.isConsumed = true;
            return;
        }
        if (isWaitingForInterceptionRoll && (keyData.key == KeyCode.R || hasRollOverride))
        {
            PerformInterceptionRoll(hasRollOverride ? rollOverride : null);
            keyData.isConsumed = true;
            return;
        }
    }

    public string TranslateRollToDirection(int direction)
    {
        switch (direction)
        {
          case 0:
            return "South";
          case 1:
            return "SouthWest";
          case 2:
            return "NorthWest";
          case 3:
            return "North";
          case 4:
            return "NorthEast";
          case 5:
            return "SouthEast";
          default:
            return "Invalid direction";  // This should never Happen
        }
    }

    public InaccuracyTargetResult CalculateFinalInaccuracyTarget(HexCell startHex, int directionIndex, int distance)
    {
        HexCell finalHex = CalculateDirectionalTarget(startHex, directionIndex, distance);
        return new InaccuracyTargetResult(startHex, finalHex, directionIndex, distance);
    }

    public HexCell CalculateDirectionalTarget(HexCell startHex, int directionIndex, int distance)
    {
        if (startHex == null)
        {
            Debug.LogWarning("Cannot calculate inaccuracy target from a null start hex.");
            return null;
        }

        if (hexGrid == null)
        {
            Debug.LogError("LooseBallManager has no HexGrid linked.");
            return null;
        }

        if (directionIndex < 0 || directionIndex > 5)
        {
            Debug.LogWarning($"Invalid inaccuracy direction index: {directionIndex}");
            return null;
        }

        if (distance < 0)
        {
            Debug.LogWarning($"Invalid inaccuracy distance: {distance}");
            return null;
        }

        Vector3Int currentPosition = startHex.coordinates;
        for (int i = 0; i < distance; i++)
        {
            HexCell currentHex = hexGrid.GetHexCellAt(currentPosition);
            if (currentHex == null)
            {
                Debug.LogWarning($"Inaccuracy path left the known grid at {currentPosition}.");
                return null;
            }

            Vector2Int[] directionVectors = currentHex.GetDirectionVectors();
            Vector2Int direction2D = directionVectors[directionIndex];
            currentPosition = new Vector3Int(currentPosition.x + direction2D.x, 0, currentPosition.z + direction2D.y);
        }

        HexCell finalHex = hexGrid.GetHexCellAt(currentPosition);
        if (finalHex == null)
        {
            Debug.LogWarning($"Final inaccuracy target is outside the known grid at {currentPosition}.");
        }

        return finalHex;
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
        var (returnedRoll, returnedJackpot) = helperFunctions.DiceRoll();
        int roll = GetRollValueWithoutJackpot(rollOverride, returnedRoll);
        directionRoll = roll - 1;
        isWaitingForDirectionRoll = false;
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
        var (returnedRoll, returnedJackpot) = helperFunctions.DiceRoll();
        distanceRoll = GetRollValueWithoutJackpot(rollOverride, returnedRoll);
        isWaitingForDistanceRoll = false;
    }
    
    public void PerformInterceptionRoll(int? rigroll = null)
    {
        RollInputOverride? rollOverride = rigroll.HasValue
            ? new RollInputOverride
            {
                hasOverride = true,
                roll = rigroll.Value,
                isJackpot = false
            }
            : null;
        PerformInterceptionRoll(rollOverride);
    }

    public void PerformInterceptionRoll(RollInputOverride? rollOverride)
    {
        var (returnedRoll, returnedJackpot) = helperFunctions.DiceRoll();
        interceptionRoll = GetRollValueWithoutJackpot(rollOverride, returnedRoll);
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

    public IEnumerator ResolveLooseBall(PlayerToken startingToken, LooseBallSourceType sourceType, bool allowGKBoxMove = true)
    {
        isActivated = true;
        causingDeflection = startingToken; // TODO: I think this is redundant
        MatchManager.Instance.ClearPendingLooseBallCollectionReset();
        Debug.Log($"Loose Ball Resolution triggered by {startingToken.name} with resolution type: {sourceType}");
        path.Clear();
        PlayerToken lastPurposefulTouchBeforeLooseBall = MatchManager.Instance.LastTokenToTouchTheBallOnPurpose;
        if (IsGoalkeeperHandlingSpill(sourceType) && startingToken.IsGoalKeeper)
        {
            MatchManager.Instance.SetLastToken(startingToken);
        }

        bool allowGKBoxMoveForLooseBallMovement = allowGKBoxMove && !IsGoalkeeperHandlingSpill(sourceType);
        // Step 1: Move the ball to the starting token's hex
        HexCell defenderHex = startingToken.GetCurrentHex();
        bool gkBoxMoveOfferedForThisLooseBall = false;
        if (ShouldOfferPreRollGKBoxMove(startingToken, sourceType, allowGKBoxMove))
        {
            Debug.Log($"Loose ball source {startingToken.name} is on the opponent box line. Offering the defending GK free move before loose-ball direction and distance rolls.");
            yield return StartCoroutine(MoveLooseBallUntilGKBoxMove(defenderHex));
            gkBoxMoveOfferedForThisLooseBall = true;
            yield return StartCoroutine(MoveLooseBallToHex(defenderHex, sourceType, allowGKBoxMove: false));
        }
        else
        {
            yield return StartCoroutine(MoveLooseBallToHex(defenderHex, sourceType, allowGKBoxMoveForLooseBallMovement));
        }
        // ball.SetCurrentHex(defenderHex);
        List<int> spillDirections = new List<int>();
        if (IsGoalkeeperHandlingSpill(sourceType) && startingToken.IsGoalKeeper)
        {
            if (startingToken.currentHex.isInPenaltyBox == 0) Debug.LogError($"Something is wrong, {startingToken.name} does not seem to be in Penalty Area");
            else if( startingToken.currentHex.isInPenaltyBox == 1 )
            {
                // Right side of the pitch
                spillDirections = new List<int>{1,2};
            }
            else if( startingToken.currentHex.isInPenaltyBox == -1 )
            {
                // Left side of the pitch
                spillDirections = new List<int>{4,5};
            }
        }

        // Step 2: Roll for direction and distance
        // Wait for input to confirm the direction
        isWaitingForDirectionRoll = true;
        while (isWaitingForDirectionRoll)
        {
            yield return null;
        }

        if (IsGoalkeeperHandlingSpill(sourceType) && startingToken.IsGoalKeeper)
        {
            if (!spillDirections.Contains(directionRoll))
            {
                MatchManager.Instance.gameData.gameLog.LogEvent(
                    startingToken
                    , MatchManager.ActionType.SaveMade
                    , saveType: "corner"
                    , connectedToken: lastPurposefulTouchBeforeLooseBall
                );
                // HexCell lastInbound;
                // This should be a CornerKick, and we should break
                if (directionRoll == 2 || directionRoll == 3 || directionRoll == 4)
                {
                    // North Direction
                    if (startingToken.currentHex.isInPenaltyBox == 1)
                    {
                        // Right Side
                        Debug.Log($"Ball went out of bounds on the Right Side, starting a Corner Kick form the North corner");
                        yield return StartCoroutine(longBallManager.HandleLongBallMovement(hexGrid.GetHexCellAt(new Vector3Int(22, 0, 6)), true));
                        StartCoroutine(outOfBoundsManager.HandleGoalKickOrCorner(hexGrid.GetHexCellAt(new Vector3Int(18, 0, 6)), "RightGoalLine", "defendertouch", startingToken, lastPurposefulTouchBeforeLooseBall));
                    }
                    else
                    {
                        // Left Side
                        Debug.Log($"Ball went out of bounds on the Left Side, starting a Corner Kick form the North corner");
                        yield return StartCoroutine(longBallManager.HandleLongBallMovement(hexGrid.GetHexCellAt(new Vector3Int(-22, 0, 6)), true));
                        StartCoroutine(outOfBoundsManager.HandleGoalKickOrCorner(hexGrid.GetHexCellAt(new Vector3Int(-18, 0, 6)), "LeftGoalLine", "defendertouch", startingToken, lastPurposefulTouchBeforeLooseBall));
                    }
                }
                else
                {
                    // South Direction
                    if (startingToken.currentHex.isInPenaltyBox == 1)
                    {
                        // Right Side
                        Debug.Log($"Ball went out of bounds on the Right Side, starting a Corner Kick form the South corner");
                        yield return StartCoroutine(longBallManager.HandleLongBallMovement(hexGrid.GetHexCellAt(new Vector3Int(22, 0, -6)), true));
                        StartCoroutine(outOfBoundsManager.HandleGoalKickOrCorner(hexGrid.GetHexCellAt(new Vector3Int(18, 0, -6)), "RightGoalLine", "defendertouch", startingToken, lastPurposefulTouchBeforeLooseBall));
                    }
                    else
                    {
                        // Left Side
                        Debug.Log($"Ball went out of bounds on the Left Side, starting a Corner Kick form the South corner");
                        yield return StartCoroutine(longBallManager.HandleLongBallMovement(hexGrid.GetHexCellAt(new Vector3Int(-22, 0, -6)), true));
                        StartCoroutine(outOfBoundsManager.HandleGoalKickOrCorner(hexGrid.GetHexCellAt(new Vector3Int(-18, 0, -6)), "LeftGoalLine", "defendertouch", startingToken, lastPurposefulTouchBeforeLooseBall));
                    }
                }
                // Just decide where to put the ball and how to trigger the OutOfboundsManager to call the 
                EndLooseBallPhase();
                yield break;
            }
            else
            {
                MatchManager.Instance.gameData.gameLog.LogEvent(
                startingToken
                , MatchManager.ActionType.SaveMade
                , saveType: "loose"
            );
                MatchManager.Instance.SetHangingPass("shot");
            }
        }
        string direction = TranslateRollToDirection(directionRoll);
        Debug.Log($"Rolled Direction: {direction}");
        isWaitingForDistanceRoll = true;
        while (isWaitingForDistanceRoll)
        {
            yield return null;
        }

        Debug.Log($"Loose Ball Direction: {direction}, Distance: {distanceRoll}");

        // Step 3: Calculate the final target hex
        HexCell finalHex = CalculateFinalInaccuracyTarget(defenderHex, directionRoll, distanceRoll).FinalHex;

        if (finalHex == null)
        {
            Debug.LogWarning("Loose Ball target hex calculation failed.");
            EndLooseBallPhase();
            yield break;
        }

        Debug.Log($"Loose Ball target hex: {finalHex.coordinates}");

        // Step 4: Get all hexes in the path from the defender's hex to the final hex
        // HexCell currentHex = defenderHex;
        for (int i = 0; i < distanceRoll; i++)
        {
            HexCell nextHex = CalculateDirectionalTarget(defenderHex, directionRoll, i+1);
            if (nextHex == null)
            {
                Debug.LogWarning("Loose Ball path calculation failed before reaching the rolled distance.");
                EndLooseBallPhase();
                yield break;
            }
            // Debug.Log($"nextHex: {nextHex.coordinates}");
            path.Add(nextHex);
        }
        Debug.Log($"Path: {string.Join(" -> ", path.Select((hex, index) => $"({index}): {hex.coordinates}"))}");

        // Step 5: Check for pickups along the path
        PlayerToken closestToken = null;  // Track the closest token for fallback pickup
        for (int i = 0; i < path.Count; i++)
        {
            // Noone can stop a header, so if we are resolving a header and we are not at the last one,
            if (!ShouldAllowTokenImpactOnHex(sourceType, i, path.Count)) continue;
            // move to the next one until we reach the last one

            HexCell hex = path[i];
            Debug.Log($"Checking hex {hex.coordinates} for tokens...");

            // Step 5.1: Check if there is a token directly on this hex
            PlayerToken tokenOnHex = hex.GetOccupyingToken();
            if (tokenOnHex != null)
            {
                if (IsTokenUnavailableForLooseBall(tokenOnHex))
                {
                    if (ShouldExtendPastJumpedToken(sourceType) && i == path.Count - 1)
                    {
                        Debug.Log("Moving a Hex further, as ball (after a header LB) landed on a jumped token.");
                        HexCell additionalHex = CalculateDirectionalTarget(hex, directionRoll, 1);
                        if (additionalHex == null)
                        {
                            Debug.LogWarning("Could not extend loose ball path past jumped token.");
                            continue;
                        }
                        path.Add(additionalHex);
                    }
                    continue;
                }
                else
                {
                    // Store the closest token for fallback pickup
                    closestToken = tokenOnHex;
                    Debug.Log($"{tokenOnHex.name} encountered on hex {hex.coordinates}. Tracking as fallback for ball pickup.");
                    int indexOfClosestHex = path.IndexOf(hex);
                    // Debug.Log($"{indexOfClosestHex}");
                    if (indexOfClosestHex >= 0) // Ensure the hex exists in the path
                    {
                        path.RemoveRange(indexOfClosestHex, path.Count - indexOfClosestHex);
                    }
                    Debug.Log($"Path: {string.Join(" -> ", path.Select((hex, index) => $"({index}): {hex.coordinates}"))}");
                    break; // Stop processing further hexes, as the ball can't go further
                }
            }
        }

        // If the path is occupied by a jumped token, we need to expand it until we find a free Hex.
        // Up to here closestToken may or may not be null
        
        foreach (HexCell hexround2 in path)
        {
            int pathIndex = path.IndexOf(hexround2);
            if (!ShouldAllowInterceptionOnHex(sourceType, pathIndex, path.Count)) continue;
            // check for interceptions only on the last Hex of the path
            if (hexround2.isAttackOccupied || hexround2.isDefenseOccupied) continue;
            // Step 5.2: Check if there are defenders in ZOI of this hex
            foreach (HexCell neighbor in hexround2.GetNeighbors(hexGrid))
            {
                potentialInterceptor = neighbor?.GetOccupyingToken();
                if (potentialInterceptor != null && // a token is there
                    (potentialInterceptor != startingToken || distanceRoll == 1) && // the deflector can react only on a one-hex loose ball
                    potentialInterceptor != closestToken && // not the one who is the fallback hit
                    !potentialInterceptor.isAttacker && // exclude all attackers
                    !IsTokenUnavailableForLooseBall(potentialInterceptor) &&
                    !defendersTriedToIntercept.Contains(potentialInterceptor)) // Ensure the defender hasn't already tried
                {
                    Debug.Log($"{potentialInterceptor.name} is attempting to intercept the ball near {hexround2.coordinates}...");
                    MatchManager.Instance.gameData.gameLog.LogExpectedRecovery(
                        potentialInterceptor,
                        ExpectedStatsCalculator.CalculateRecoveryProbability(potentialInterceptor),
                        connectedToken: MatchManager.Instance.LastTokenToTouchTheBallOnPurpose,
                        recoveryType: MatchManager.Instance.hangingPassType);
                    MatchManager.Instance.gameData.gameLog.LogEvent(potentialInterceptor, MatchManager.ActionType.InterceptionAttempt);

                    isWaitingForInterceptionRoll = true;
                    while (isWaitingForInterceptionRoll)
                    {
                        yield return null;
                    }

                    // int interceptionRoll = 1; // Simulate dice roll
                    if (interceptionRoll == 6 || potentialInterceptor.tackling + interceptionRoll >= 10)
                    {
                        MatchManager.Instance.gameData.gameLog.LogEvent(
                            potentialInterceptor
                            , MatchManager.ActionType.BallRecovery
                            , connectedToken: MatchManager.Instance.LastTokenToTouchTheBallOnPurpose
                            , recoveryType: MatchManager.Instance.hangingPassType
                        );
                        Debug.Log($"{potentialInterceptor.name} successfully intercepted the ball!");
                        // Move the ball to the interceptor's hex
                        List<HexCell> interceptionMovementPath = path.Take(pathIndex + 1).Where(hex => hex != null).ToList();
                        if (!interceptionMovementPath.Contains(neighbor))
                        {
                            interceptionMovementPath.Add(neighbor);
                        }

                        LooseBallGkCheckpointResult gkCheckpointResult = new();
                        yield return StartCoroutine(OfferDefendingGKMoveAndRecoveryAfterInterceptions(
                            startingToken,
                            sourceType,
                            interceptionMovementPath,
                            gkBoxMoveOfferedForThisLooseBall,
                            gkCheckpointResult));

                        gkBoxMoveOfferedForThisLooseBall = gkCheckpointResult.GkMoveOffered;
                        if (gkCheckpointResult.RecoveredByGoalkeeper)
                        {
                            EndLooseBallPhase();
                            yield break;
                        }

                        bool allowGKBoxMoveForInterception = allowGKBoxMoveForLooseBallMovement && !gkBoxMoveOfferedForThisLooseBall;
                        yield return StartCoroutine(MoveLooseBallToHex(neighbor, sourceType, allowGKBoxMoveForInterception));
                        MatchManager.Instance.ClearHangingPass();
                        MatchManager.Instance.SetLastTokenFromLooseBall(potentialInterceptor);
                        // Change possession to the defending team
                        MatchManager.Instance.ChangePossession();  
                        MatchManager.Instance.UpdatePossessionAfterPass(neighbor);  // Update possession
                        movementPhaseManager.EndMovementPhase(false);
                        MatchManager.Instance.BroadcastDefensiveRecoveryOutcome(potentialInterceptor, neighbor);
                        EndLooseBallPhase();
                        yield break; // End ball movement
                    }
                    else
                    {
                        Debug.Log($"{potentialInterceptor.name} failed to intercept the ball.");
                        defendersTriedToIntercept.Add(potentialInterceptor); // Mark defender as having tried interception
                    }
                }
            }
        }
        Debug.Log($"No more interception chances, Moving Ball to the last Hex of the Path");
        if (closestToken != null) {path.Add(closestToken.GetCurrentHex());}

        LooseBallGkCheckpointResult finalGkCheckpointResult = new();
        yield return StartCoroutine(OfferDefendingGKMoveAndRecoveryAfterInterceptions(
            startingToken,
            sourceType,
            path,
            gkBoxMoveOfferedForThisLooseBall,
            finalGkCheckpointResult));

        gkBoxMoveOfferedForThisLooseBall = finalGkCheckpointResult.GkMoveOffered;
        if (finalGkCheckpointResult.RecoveredByGoalkeeper)
        {
            EndLooseBallPhase();
            yield break;
        }

        bool allowRemainingGKBoxMove = allowGKBoxMoveForLooseBallMovement && !gkBoxMoveOfferedForThisLooseBall;

        HexCell looseBallGoalHex = FindFirstGoalHexInPath(path);
        if (looseBallGoalHex != null)
        {
            Debug.Log($"Loose ball crossed the goal at {looseBallGoalHex.coordinates} after all interception attempts failed.");
            yield return StartCoroutine(MoveLooseBallToHex(looseBallGoalHex, sourceType, allowRemainingGKBoxMove));
            if (TryHandleLooseBallGoal(looseBallGoalHex, startingToken))
            {
                EndLooseBallPhase();
                yield break;
            }
        }

        // Step 5.3: If no interception succeeded move the ball to the last Hex of the Path
        yield return StartCoroutine(MoveLooseBallToHex(path.Last(), sourceType, allowRemainingGKBoxMove));
        HexCell looseBallRestingHex = path.Last();
        // Check what is going on with where the ball went.
        // Ball ended up on a Token
        if (closestToken != null)
        {
            ballHitThisToken = closestToken;
            // TODO: resolve based on what created the Loose Ball.
            // Token with Ball is an Attacker
            if (closestToken.isAttacker)
            {
                if (IsHeaderLooseBall(sourceType) && MatchManager.Instance.hangingPassType == "aerial")
                {
                    MatchManager.Instance.gameData.gameLog.LogEvent(
                        MatchManager.Instance.LastTokenToTouchTheBallOnPurpose,
                        MatchManager.ActionType.AerialPassCompleted
                    );
                }
                AssignLooseBallReceiver(closestToken, sourceType);
                if (IsHeaderLooseBall(sourceType))
                {
                    // TODO: ignore offside
                    finalThirdManager.TriggerFinalThirdPhase();
                    MatchManager.Instance.BroadcastHeaderCompleted();
                    Debug.Log("Available Options are: [M]ovement Phase, Short [P]ass, [L]ong Ball, [S]napshot");
                }
                // TODO: Check if the the Loose Ball is from HP OR LB or they handle themselves
                // TODO: Check if the Loose ball is from Shot or snapshot
                else if (!movementPhaseManager.isActivated) // TODO: check if there is no Movement Phase going on, Allow Attacker Selection
                {
                    Debug.LogWarning("There is no movement Phase going on, Attacker must choose what to do!");
                    finalThirdManager.TriggerFinalThirdPhase();
                    MatchManager.Instance.BroadcastAnyOtherScenario(ShouldOfferShortGroundBallAfterAttackerPickup(sourceType, allowGKBoxMoveForLooseBallMovement));
                    Debug.Log("Available Options are: [M]ovement Phase, Standard [P]ass, [L]ong Ball, [S]napshot");
                }
                else if (movementPhaseManager.isActivated)
                {
                    // There is a movement Phase going ON.
                    Debug.Log($"Ball hit {closestToken.name}, who is an attacker");
                    if (movementPhaseManager.isMovementPhaseDef)
                    {
                        // Attacker hit during Def MP
                        movementPhaseManager.AdvanceMovementPhase();
                    }
                    else
                    {
                        bool isSnapshotAvailable = movementPhaseManager.IsDribblerinOpponentPenaltyBox(closestToken);
                        if (isSnapshotAvailable)
                        {
                            Debug.Log($"{closestToken.name} found themselves with the ball in during MP the opposition penalty Box. Press [S] to take a snapshot!");
                            shotManager.isAvailable = true;
                            shotManager.isWaitingForSnapshotDecisionFromLoose = true;
                            // Shot Manager takes responsibility from here on
                        }
                    }
                }
                else
                {
                    Debug.LogError("Unknown Scenario");
                }
            }
            else // Ball Hit a defender
            {
                // TODO: Should we check what is going on first?
                Debug.Log($"Ball hit {closestToken.name}, who is a defender");
                MatchManager.Instance.gameData.gameLog.LogEvent(
                    closestToken
                    , MatchManager.ActionType.BallRecovery
                    , connectedToken: MatchManager.Instance.LastTokenToTouchTheBallOnPurpose
                    , recoveryType: MatchManager.Instance.hangingPassType
                ); // TODO: check what is being picked up
                AssignLooseBallReceiver(closestToken, sourceType);
                // Change possession to the defending team
                MatchManager.Instance.ChangePossession();  
                MatchManager.Instance.UpdatePossessionAfterPass(defenderHex);  // Update possession
                movementPhaseManager.EndMovementPhase(false);
                MatchManager.Instance.BroadcastDefensiveRecoveryOutcome(closestToken, defenderHex);
             }
        }
        else // ball hit no token and reached an empty Hex
        {
            if (TryHandleLooseBallGoal(looseBallRestingHex, startingToken))
            {
                EndLooseBallPhase();
                yield break;
            }

            if (!looseBallRestingHex.isOutOfBounds) // in bounds, still in play
            {
                Debug.Log($"Ball did not hit anyone");
                MatchManager.Instance.UpdatePossessionAfterPass(looseBallRestingHex);
                if (ShouldClearPreviousChainOnCollection(sourceType))
                {
                    MatchManager.Instance.MarkNextBallCollectionToClearPrevious();
                }
                else
                {
                    MatchManager.Instance.ClearPendingLooseBallCollectionReset();
                }

                if (IsHeaderLooseBall(sourceType))
                {
                    MatchManager.Instance.currentState = MatchManager.GameState.HeaderCompleted;
                    finalThirdManager.TriggerFinalThirdPhase();
                    Debug.Log($"Header Resolved to a Loose Ball, Ball is not in Possesssion. {MatchManager.Instance.teamInAttack} Starting a movement Phase");
                    movementPhaseManager.ActivateMovementPhase();
                    movementPhaseManager.CommitToAction();
                }
                else if (!movementPhaseManager.isActivated)
                {
                    finalThirdManager.TriggerFinalThirdPhase();
                    Debug.LogWarning($"Loose ball is not picked up by anyone.{MatchManager.Instance.teamInAttack} Starts a movement Phase");
                    movementPhaseManager.ActivateMovementPhase();
                    movementPhaseManager.CommitToAction();
                }
                else if (movementPhaseManager.isActivated)
                {
                    Debug.LogWarning($"Loose ball is not picked up by anyone. Current movement Phase continues.");
                    movementPhaseManager.AdvanceMovementPhase();
                }
                else
                {
                    Debug.LogError("Unknown Scenario");
                }
            }
            else
            {
                Debug.Log($"Ball Went out of Bounds");
                MatchManager.Instance.ClearLastTokenChain();
                MatchManager.Instance.ClearPendingLooseBallCollectionReset();
                if (movementPhaseManager.isActivated)
                {
                    movementPhaseManager.EndMovementPhase(false);
                }
                outOfBoundsManager.HandleOutOfBounds(startingToken.GetCurrentHex(), directionRoll, ResolveOutOfBoundsSource(startingToken), startingToken);
            }
        }
        EndLooseBallPhase();
    }

    public void EndLooseBallPhase()
    {
        isActivated = false;
        isWaitingForDirectionRoll = false;
        isWaitingForDistanceRoll = false;
        isWaitingForInterceptionRoll = false;
        directionRoll = 240885;
        distanceRoll = 0;
        interceptionRoll = 0;
        defendersTriedToIntercept.Clear();
        potentialInterceptor = null;
        causingDeflection = null;
        ballHitThisToken = null;
        nextLooseBallGoalIsPenalty = false;
        path.Clear();
    }

    public string GetDebugStatus()
    {
        StringBuilder sb = new();
        sb.Append("Loose: ");

        if (isActivated) sb.Append("isActivated, ");
        if (isWaitingForDirectionRoll) sb.Append("isWaitingForDirectionRoll, ");
        if (isWaitingForDistanceRoll) sb.Append("isWaitingForDistanceRoll, ");
        if (isWaitingForInterceptionRoll) sb.Append("isWaitingForInterceptionRoll, ");
        
        if (sb.Length >= 2 && sb[^2] == ',') sb.Length -= 2; // Trim trailing comma
        return sb.ToString();
    }

    public string GetInstructions()
    {
        StringBuilder sb = new();
        if (isActivated) sb.Append("Loose: ");
        if (isWaitingForDirectionRoll) sb.Append($"Press [R] to roll the Direction roll from {causingDeflection.playerName}, ");
        if (isWaitingForDistanceRoll) sb.Append($"Press [R] to roll the Distance roll from {causingDeflection.playerName}, ");
        if (isWaitingForInterceptionRoll) sb.Append($"Press [R] to roll an Interception roll from {potentialInterceptor.playerName}, ");

        if (sb.Length >= 2 && sb[^2] == ',') sb.Length -= 2; // Safely trim trailing comma + space
        return sb.ToString();
    }

    public bool? IsInstructionExpectingHomeTeam()
    {
        if (!isActivated)
        {
            return null;
        }

        if (isWaitingForInterceptionRoll && potentialInterceptor != null)
        {
            return potentialInterceptor.isHomeTeam;
        }

        if ((isWaitingForDirectionRoll || isWaitingForDistanceRoll) && causingDeflection != null)
        {
            return causingDeflection.isHomeTeam;
        }

        return null;
    }

}
