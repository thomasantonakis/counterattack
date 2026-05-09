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

    public IEnumerator ResolveGKPush(PlayerToken goalkeeper, HexCell saveHex)
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
                yield return StartCoroutine(MoveTokenDirect(goalkeeper, saveHex, goalkeeper.isAttacker));
                ball?.AdjustBallHeightBasedOnOccupancy();
            }
            yield break;
        }

        int zDirection = saveHex.coordinates.z > gkHex.coordinates.z ? 1 : -1;
        int pushDistance = Mathf.Abs(saveHex.coordinates.z - gkHex.coordinates.z);
        List<PushMove> pushMoves = CollectPushMoves(goalkeeper, gkHex, pushDistance, zDirection);
        pushMoves.Sort((left, right) => right.stepFromGoalkeeper.CompareTo(left.stepFromGoalkeeper));

        if (!AssignDestinations(pushMoves, saveHex, pushDistance, zDirection))
        {
            yield break;
        }

        foreach (PushMove move in pushMoves)
        {
            Debug.Log($"GKPush: moving {move.token.name} from {move.originHex.coordinates} to {move.destinationHex.coordinates}.");
            yield return StartCoroutine(MoveTokenDirect(move.token, move.destinationHex, move.wasAttacker));
        }

        Debug.Log($"GKPush: moving {goalkeeper.name} from {gkHex.coordinates} to save hex {saveHex.coordinates}.");
        yield return StartCoroutine(MoveTokenDirect(goalkeeper, saveHex, goalkeeper.isAttacker));
        ball?.AdjustBallHeightBasedOnOccupancy();
    }

    private void EnsureDependencies()
    {
        if (hexGrid == null)
        {
            hexGrid = UnityEngine.Object.FindFirstObjectByType<HexGrid>();
        }

        if (ball == null)
        {
            ball = UnityEngine.Object.FindFirstObjectByType<Ball>();
        }
    }

    private List<PushMove> CollectPushMoves(PlayerToken goalkeeper, HexCell gkHex, int pushDistance, int zDirection)
    {
        List<PushMove> pushMoves = new();

        for (int step = 1; step <= pushDistance; step++)
        {
            Vector3Int coordinates = new Vector3Int(
                gkHex.coordinates.x,
                0,
                gkHex.coordinates.z + (step * zDirection)
            );
            HexCell pathHex = TryGetCell(coordinates);
            if (pathHex == null)
            {
                continue;
            }

            PlayerToken occupant = pathHex.GetOccupyingToken();
            if (occupant == null)
            {
                if (pathHex.isAttackOccupied || pathHex.isDefenseOccupied)
                {
                    Debug.LogWarning($"GKPush found occupied flags without a token at {pathHex.coordinates}; treating it as empty.");
                }

                continue;
            }

            if (occupant == goalkeeper)
            {
                continue;
            }

            pushMoves.Add(new PushMove
            {
                token = occupant,
                originHex = pathHex,
                wasAttacker = occupant.isAttacker,
                stepFromGoalkeeper = step
            });
        }

        return pushMoves;
    }

    private bool AssignDestinations(List<PushMove> pushMoves, HexCell saveHex, int pushDistance, int zDirection)
    {
        HashSet<HexCell> reservedDestinations = new() { saveHex };

        foreach (PushMove move in pushMoves)
        {
            Vector3Int directCoordinates = new Vector3Int(
                move.originHex.coordinates.x,
                0,
                move.originHex.coordinates.z + (pushDistance * zDirection)
            );
            HexCell directDestination = TryGetCell(directCoordinates);

            if (IsLegalDestination(directDestination, reservedDestinations))
            {
                move.destinationHex = directDestination;
            }
            else
            {
                move.destinationHex = FindFallbackDestination(move, directCoordinates, reservedDestinations);
                if (move.destinationHex == null)
                {
                    Debug.LogError($"GKPush failed: no legal destination found for {move.token.name} pushed from {move.originHex.coordinates}.");
                    return false;
                }

                Debug.LogWarning($"GKPush overflow: {move.token.name} cannot move to {directCoordinates}; using {move.destinationHex.coordinates}.");
            }

            reservedDestinations.Add(move.destinationHex);
        }

        return true;
    }

    private HexCell FindFallbackDestination(PushMove move, Vector3Int desiredCoordinates, HashSet<HexCell> reservedDestinations)
    {
        List<HexCell> legalCandidates = GetLegalFallbackCandidates(reservedDestinations);
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

    private List<HexCell> GetLegalFallbackCandidates(HashSet<HexCell> reservedDestinations)
    {
        List<HexCell> candidates = new();
        if (hexGrid?.cells == null)
        {
            return candidates;
        }

        foreach (HexCell cell in hexGrid.cells)
        {
            if (IsLegalDestination(cell, reservedDestinations))
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

    private bool IsLegalDestination(HexCell cell, HashSet<HexCell> reservedDestinations)
    {
        return cell != null
            && !reservedDestinations.Contains(cell)
            && !cell.isOutOfBounds
            && cell.isInGoal == 0
            && !IsOccupied(cell);
    }

    private bool IsOccupied(HexCell cell)
    {
        return cell != null
            && (cell.GetOccupyingToken() != null || cell.isAttackOccupied || cell.isDefenseOccupied);
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

    private IEnumerator MoveTokenDirect(PlayerToken token, HexCell targetHex, bool wasAttacker)
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

        yield return StartCoroutine(token.JumpToHex(targetHex));
        targetHex.ResetHighlight();
    }
}
