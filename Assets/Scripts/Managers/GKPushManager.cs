using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GKPushManager : MonoBehaviour
{
    [Header("Dependencies")]
    public HexGrid hexGrid;
    public Ball ball;

    private sealed class PushMove
    {
        public PlayerToken token;
        public HexCell originHex;
        public HexCell destinationHex;
        public bool wasAttacker;
        public int stepFromGoalkeeper;
    }

    public void Configure(HexGrid grid, Ball ballReference)
    {
        if (grid != null)
        {
            hexGrid = grid;
        }

        if (ballReference != null)
        {
            ball = ballReference;
        }
    }

    public IEnumerator ResolveGKPush(PlayerToken goalkeeper, HexCell saveHex, float? synchronizedDuration = null)
    {
        if (goalkeeper == null || saveHex == null)
        {
            Debug.LogError("GKPush failed: goalkeeper or save hex is null.");
            yield break;
        }

        EnsureDependencies();

        HexCell gkHex = goalkeeper.GetCurrentHex();
        if (gkHex == null)
        {
            Debug.LogError($"GKPush failed: {goalkeeper.name} has no current hex.");
            yield break;
        }

        if (gkHex == saveHex)
        {
            Debug.Log($"GKPush: {goalkeeper.name} is already on save hex {saveHex.coordinates}.");
            ball?.AdjustBallHeightBasedOnOccupancy();
            yield break;
        }

        if (gkHex.coordinates.x != saveHex.coordinates.x)
        {
            Debug.LogWarning($"GKPush v1 supports same-X saves only. GK at {gkHex.coordinates}, save hex {saveHex.coordinates}.");
            if (!IsOccupied(saveHex) || saveHex.GetOccupyingToken() == goalkeeper)
            {
                yield return StartCoroutine(MoveTokenDirect(goalkeeper, saveHex, goalkeeper.isAttacker, synchronizedDuration));
                ball?.AdjustBallHeightBasedOnOccupancy();
            }
            yield break;
        }

        int zDirection = saveHex.coordinates.z > gkHex.coordinates.z ? 1 : -1;
        int pushDistance = Mathf.Abs(saveHex.coordinates.z - gkHex.coordinates.z);
        List<PushMove> pushMoves = CollectPushMoves(goalkeeper, gkHex, pushDistance, zDirection);

        if (!AssignDestinations(pushMoves, saveHex, pushDistance, zDirection))
        {
            yield break;
        }

        if (synchronizedDuration.HasValue)
        {
            yield return StartCoroutine(MoveTokensTogether(pushMoves, goalkeeper, saveHex, synchronizedDuration.Value));
        }
        else
        {
            foreach (PushMove move in pushMoves)
            {
                Debug.Log($"GKPush: moving {move.token.name} from {move.originHex.coordinates} to {move.destinationHex.coordinates}.");
                yield return StartCoroutine(MoveTokenDirect(move.token, move.destinationHex, move.wasAttacker));
            }

            Debug.Log($"GKPush: moving {goalkeeper.name} from {gkHex.coordinates} to save hex {saveHex.coordinates}.");
            yield return StartCoroutine(MoveTokenDirect(goalkeeper, saveHex, goalkeeper.isAttacker));
        }
        ball?.AdjustBallHeightBasedOnOccupancy();
    }

    private void EnsureDependencies()
    {
        if (hexGrid == null)
        {
            hexGrid = Object.FindAnyObjectByType<HexGrid>();
        }

        if (ball == null)
        {
            ball = Object.FindAnyObjectByType<Ball>();
        }
    }

    private List<PushMove> CollectPushMoves(PlayerToken goalkeeper, HexCell gkHex, int pushDistance, int zDirection)
    {
        List<PushMove> sweptPathMoves = new();

        for (int step = 1; step <= pushDistance; step++)
        {
            PushMove move = TryCreatePushMoveAtStep(goalkeeper, gkHex, step, zDirection);
            if (move != null)
            {
                sweptPathMoves.Add(move);
            }
        }

        return sweptPathMoves
            .OrderBy(move => move.stepFromGoalkeeper)
            .ToList();
    }

    private PushMove TryCreatePushMoveAtStep(PlayerToken goalkeeper, HexCell gkHex, int step, int zDirection)
    {
        Vector3Int coordinates = new Vector3Int(
            gkHex.coordinates.x,
            0,
            gkHex.coordinates.z + (step * zDirection)
        );
        HexCell pathHex = TryGetCell(coordinates);
        if (pathHex == null)
        {
            return null;
        }

        PlayerToken occupant = pathHex.GetOccupyingToken();
        if (occupant == null)
        {
            if (pathHex.isAttackOccupied || pathHex.isDefenseOccupied)
            {
                Debug.LogWarning($"GKPush found occupied flags without a token at {pathHex.coordinates}; treating it as empty.");
            }

            return null;
        }

        if (occupant == goalkeeper)
        {
            return null;
        }

        return new PushMove
        {
            token = occupant,
            originHex = pathHex,
            wasAttacker = occupant.isAttacker,
            stepFromGoalkeeper = step
        };
    }

    private bool AssignDestinations(List<PushMove> pushMoves, HexCell saveHex, int pushDistance, int zDirection)
    {
        HashSet<HexCell> reservedDestinations = new() { saveHex };
        HashSet<PlayerToken> movingTokens = pushMoves
            .Where(move => move.token != null)
            .Select(move => move.token)
            .ToHashSet();
        HashSet<HexCell> movingOriginHexes = pushMoves
            .Where(move => move.originHex != null)
            .Select(move => move.originHex)
            .ToHashSet();

        int index = 0;
        while (index < pushMoves.Count)
        {
            PushMove move = pushMoves[index];
            Vector3Int desiredCoordinates = new Vector3Int(
                saveHex.coordinates.x,
                0,
                saveHex.coordinates.z + ((index + 1) * zDirection)
            );
            HexCell directDestination = TryGetCell(desiredCoordinates);
            if (directDestination == null || directDestination == move.originHex)
            {
                Debug.LogError($"GKPush failed: no valid same-column destination found for {move.token.name} pushed from {move.originHex.coordinates} toward {desiredCoordinates}.");
                return false;
            }

            PlayerToken destinationOccupant = directDestination.GetOccupyingToken();
            if (destinationOccupant != null && !movingTokens.Contains(destinationOccupant))
            {
                pushMoves.Add(new PushMove
                {
                    token = destinationOccupant,
                    originHex = directDestination,
                    wasAttacker = destinationOccupant.isAttacker,
                    stepFromGoalkeeper = pushDistance + pushMoves.Count + 1
                });
                movingTokens.Add(destinationOccupant);
                movingOriginHexes.Add(directDestination);
            }

            if (!IsLegalDestination(directDestination, reservedDestinations, movingTokens, movingOriginHexes))
            {
                Debug.LogError($"GKPush failed: same-column destination {directDestination.coordinates} is blocked for {move.token.name}.");
                return false;
            }

            move.destinationHex = directDestination;
            reservedDestinations.Add(move.destinationHex);
            index++;
        }

        return true;
    }

    private HexCell FindSameColumnDestination(PushMove move, Vector3Int desiredCoordinates, int zDirection, HashSet<HexCell> reservedDestinations, HashSet<PlayerToken> movingTokens, HashSet<HexCell> movingOriginHexes)
    {
        for (int offset = 1; offset <= hexGrid.GridHeight; offset++)
        {
            Vector3Int candidateCoordinates = new Vector3Int(
                desiredCoordinates.x,
                0,
                desiredCoordinates.z + ((offset - 1) * zDirection)
            );
            HexCell candidate = TryGetCell(candidateCoordinates);
            if (candidate == null)
            {
                return null;
            }

            if (candidate == move.originHex)
            {
                continue;
            }

            if (IsLegalDestination(candidate, reservedDestinations, movingTokens, movingOriginHexes))
            {
                return candidate;
            }
        }

        return null;
    }

    private HexCell FindFallbackDestination(PushMove move, Vector3Int desiredCoordinates, HashSet<HexCell> reservedDestinations, HashSet<PlayerToken> movingTokens)
    {
        List<HexCell> legalCandidates = GetLegalFallbackCandidates(reservedDestinations, movingTokens)
            .Where(candidate => candidate != move.originHex)
            .ToList();
        if (legalCandidates.Count == 0)
        {
            return null;
        }

        int defensiveXSign = GetDefensiveXSign(move.token);
        List<HexCell> defensewardCandidates = legalCandidates
            .Where(candidate => GetDefensewardProgress(move.originHex, candidate, defensiveXSign) > 0)
            .ToList();

        if (defensewardCandidates.Count > 0)
        {
            return SelectBestFallback(defensewardCandidates, move.originHex, desiredCoordinates, defensiveXSign);
        }

        List<HexCell> sameSideCandidates = legalCandidates
            .Where(candidate => GetDefensewardProgress(move.originHex, candidate, defensiveXSign) >= 0)
            .ToList();

        if (sameSideCandidates.Count > 0)
        {
            Debug.LogWarning($"GKPush fallback for {move.token.name}: no strictly defenseward cell was available.");
            return SelectBestFallback(sameSideCandidates, move.originHex, desiredCoordinates, defensiveXSign);
        }

        Debug.LogWarning($"GKPush fallback for {move.token.name}: using nearest legal cell outside defensive bias.");
        return SelectBestFallback(legalCandidates, move.originHex, desiredCoordinates, defensiveXSign);
    }

    private List<HexCell> GetLegalFallbackCandidates(HashSet<HexCell> reservedDestinations, HashSet<PlayerToken> movingTokens)
    {
        List<HexCell> candidates = new();
        if (hexGrid?.cells == null)
        {
            return candidates;
        }

        foreach (HexCell cell in hexGrid.cells)
        {
            if (IsLegalDestination(cell, reservedDestinations, movingTokens))
            {
                candidates.Add(cell);
            }
        }

        return candidates;
    }

    private HexCell SelectBestFallback(List<HexCell> candidates, HexCell originHex, Vector3Int desiredCoordinates, int defensiveXSign)
    {
        return candidates
            .OrderBy(candidate => HexGridUtils.GetHexStepDistance(desiredCoordinates, candidate.coordinates))
            .ThenByDescending(candidate => GetDefensewardProgress(originHex, candidate, defensiveXSign))
            .ThenBy(candidate => Mathf.Abs(candidate.coordinates.z))
            .ThenBy(candidate => candidate.coordinates.x)
            .ThenBy(candidate => candidate.coordinates.z)
            .FirstOrDefault();
    }

    private int GetDefensewardProgress(HexCell originHex, HexCell candidate, int defensiveXSign)
    {
        return (candidate.coordinates.x - originHex.coordinates.x) * defensiveXSign;
    }

    private int GetDefensiveXSign(PlayerToken token)
    {
        MatchManager.TeamAttackingDirection attackingDirection = MatchManager.TeamAttackingDirection.LeftToRight;
        if (MatchManager.Instance != null)
        {
            attackingDirection = token.isHomeTeam
                ? MatchManager.Instance.homeTeamDirection
                : MatchManager.Instance.awayTeamDirection;
        }
        else if (!token.isHomeTeam)
        {
            attackingDirection = MatchManager.TeamAttackingDirection.RightToLeft;
        }

        return attackingDirection == MatchManager.TeamAttackingDirection.LeftToRight ? -1 : 1;
    }

    private bool IsLegalDestination(HexCell cell, HashSet<HexCell> reservedDestinations, HashSet<PlayerToken> movingTokens, HashSet<HexCell> movingOriginHexes = null)
    {
        return cell != null
            && !reservedDestinations.Contains(cell)
            && !cell.isOutOfBounds
            && cell.isInGoal == 0
            && !IsOccupiedByNonMovingToken(cell, movingTokens, movingOriginHexes);
    }

    private bool IsOccupied(HexCell cell)
    {
        return cell != null
            && (cell.GetOccupyingToken() != null || cell.isAttackOccupied || cell.isDefenseOccupied);
    }

    private bool IsOccupiedByNonMovingToken(HexCell cell, HashSet<PlayerToken> movingTokens, HashSet<HexCell> movingOriginHexes = null)
    {
        if (cell == null)
        {
            return false;
        }

        if (movingOriginHexes != null && movingOriginHexes.Contains(cell))
        {
            return false;
        }

        PlayerToken occupant = cell.GetOccupyingToken();
        if (occupant != null)
        {
            return movingTokens == null || !movingTokens.Contains(occupant);
        }

        return cell.isAttackOccupied || cell.isDefenseOccupied;
    }

    private HexCell TryGetCell(Vector3Int coordinates)
    {
        if (hexGrid == null)
        {
            return null;
        }

        int minX = -hexGrid.GridWidth / 2;
        int maxX = (hexGrid.GridWidth / 2) - 1;
        int minZ = -hexGrid.GridHeight / 2;
        int maxZ = (hexGrid.GridHeight / 2) - 1;
        if (coordinates.x < minX || coordinates.x > maxX || coordinates.z < minZ || coordinates.z > maxZ)
        {
            return null;
        }

        return hexGrid.GetHexCellAt(coordinates);
    }

    private IEnumerator MoveTokensTogether(List<PushMove> pushMoves, PlayerToken goalkeeper, HexCell saveHex, float duration)
    {
        List<(PlayerToken token, HexCell destinationHex, bool wasAttacker)> moves = pushMoves
            .Select(move => (move.token, move.destinationHex, move.wasAttacker))
            .ToList();
        moves.Add((goalkeeper, saveHex, goalkeeper.isAttacker));

        foreach ((PlayerToken token, HexCell _, bool _) in moves)
        {
            HexCell originHex = token?.GetCurrentHex();
            if (originHex == null)
            {
                continue;
            }

            originHex.isAttackOccupied = false;
            originHex.isDefenseOccupied = false;
            if (originHex.occupyingToken == token)
            {
                originHex.occupyingToken = null;
            }
            originHex.ResetHighlight();
        }

        foreach ((PlayerToken _, HexCell destinationHex, bool wasAttacker) in moves)
        {
            if (destinationHex == null)
            {
                continue;
            }

            destinationHex.isAttackOccupied = wasAttacker;
            destinationHex.isDefenseOccupied = !wasAttacker;
        }

        int remainingMoves = moves.Count;
        foreach (PushMove move in pushMoves)
        {
            Debug.Log($"GKPush: moving {move.token.name} from {move.originHex.coordinates} to {move.destinationHex.coordinates}.");
            StartCoroutine(MoveTokenAlongGroundAndSignal(move.token, move.destinationHex, duration, () => remainingMoves--));
        }

        Debug.Log($"GKPush: moving {goalkeeper.name} from {goalkeeper.GetCurrentHex()?.coordinates} to save hex {saveHex.coordinates}.");
        StartCoroutine(MoveTokenAlongGroundAndSignal(goalkeeper, saveHex, duration, () => remainingMoves--));

        while (remainingMoves > 0)
        {
            yield return null;
        }
    }

    private IEnumerator MoveTokenAlongGroundAndSignal(PlayerToken token, HexCell targetHex, float duration, System.Action onComplete)
    {
        yield return StartCoroutine(MoveTokenAlongGround(token, targetHex, duration));
        token.SetCurrentHex(targetHex);
        targetHex.ResetHighlight();
        onComplete?.Invoke();
    }

    private IEnumerator MoveTokenDirect(PlayerToken token, HexCell targetHex, bool wasAttacker, float? duration = null)
    {
        if (token == null || targetHex == null)
        {
            yield break;
        }

        HexCell originHex = token.GetCurrentHex();
        if (originHex == targetHex)
        {
            yield break;
        }

        if (originHex != null)
        {
            originHex.isAttackOccupied = false;
            originHex.isDefenseOccupied = false;
            if (originHex.occupyingToken == token)
            {
                originHex.occupyingToken = null;
            }
            originHex.ResetHighlight();
        }

        if (wasAttacker)
        {
            targetHex.isAttackOccupied = true;
            targetHex.isDefenseOccupied = false;
        }
        else
        {
            targetHex.isAttackOccupied = false;
            targetHex.isDefenseOccupied = true;
        }

        yield return StartCoroutine(MoveTokenAlongGround(token, targetHex, duration ?? GetDefaultMoveDuration(originHex, targetHex)));
        token.SetCurrentHex(targetHex);
        targetHex.ResetHighlight();
    }

    private IEnumerator MoveTokenAlongGround(PlayerToken token, HexCell targetHex, float duration)
    {
        Vector3 startPosition = token.transform.position;
        Vector3 targetPosition = new(targetHex.GetHexCenter().x, startPosition.y, targetHex.GetHexCenter().z);
        if (duration <= 0f)
        {
            token.transform.position = targetPosition;
            yield break;
        }

        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / duration);
            token.transform.position = Vector3.Lerp(startPosition, targetPosition, progress);
            yield return null;
        }

        token.transform.position = targetPosition;
    }

    private float GetDefaultMoveDuration(HexCell originHex, HexCell targetHex)
    {
        int distance = originHex != null && targetHex != null
            ? Mathf.Max(1, HexGridUtils.GetHexStepDistance(originHex, targetHex))
            : 1;
        return distance * 0.3f;
    }
}
