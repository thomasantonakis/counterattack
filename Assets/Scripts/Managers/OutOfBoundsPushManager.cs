using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class OutOfBoundsPushManager : MonoBehaviour
{
    [Header("Dependencies")]
    public HexGrid hexGrid;
    public Ball ball;

    private const int MaxPushDistance = 3;

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

    public IEnumerator ResolveOutOfBoundsPush(HexCell reentryHex)
    {
        if (reentryHex == null)
        {
            Debug.LogError("OutOfBoundsPush failed: reentry hex is null.");
            yield break;
        }

        EnsureDependencies();

        PlayerToken occupyingToken = reentryHex.GetOccupyingToken();
        if (occupyingToken == null)
        {
            if (reentryHex.isDefenseOccupied)
            {
                Debug.LogWarning($"OutOfBoundsPush found defender occupancy on {reentryHex.coordinates} without a token.");
            }

            yield break;
        }

        if (occupyingToken.isAttacker)
        {
            Debug.Log($"OutOfBoundsPush: {reentryHex.coordinates} is occupied by attacker {occupyingToken.name}; no push needed.");
            yield break;
        }

        HexCell destinationHex = GetOutOfBoundsPushHex(occupyingToken, reentryHex);
        if (destinationHex == null)
        {
            Debug.LogError($"OutOfBoundsPush failed: no legal destination found for {occupyingToken.name} on {reentryHex.coordinates}.");
            yield break;
        }

        Debug.Log($"OutOfBoundsPush: moving {occupyingToken.name} from {reentryHex.coordinates} to {destinationHex.coordinates}.");
        yield return StartCoroutine(MoveTokenDirect(occupyingToken, destinationHex));
        ball?.AdjustBallHeightBasedOnOccupancy();
    }

    public HexCell GetDefensiveGoalHex(PlayerToken token)
    {
        if (token == null)
        {
            return null;
        }

        EnsureDependencies();

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

        int defensiveGoalX = attackingDirection == MatchManager.TeamAttackingDirection.LeftToRight ? -18 : 18;
        return hexGrid != null ? hexGrid.GetHexCellAt(new Vector3Int(defensiveGoalX, 0, 0)) : null;
    }

    public HexCell GetOutOfBoundsPushHex(PlayerToken token, HexCell tokenHex)
    {
        HexCell defensiveGoalHex = GetDefensiveGoalHex(token);
        return GetOutOfBoundsPushHex(tokenHex, defensiveGoalHex);
    }

    public HexCell GetOutOfBoundsPushHex(HexCell tokenHex, HexCell defensiveGoalHex)
    {
        if (tokenHex == null || defensiveGoalHex == null)
        {
            return null;
        }

        EnsureDependencies();
        if (hexGrid == null)
        {
            return null;
        }

        int originDistanceToGoal = HexGridUtils.GetHexStepDistance(tokenHex, defensiveGoalHex);
        HashSet<HexCell> visited = new() { tokenHex };
        List<HexCell> frontier = new() { tokenHex };

        for (int distance = 1; distance <= MaxPushDistance; distance++)
        {
            List<HexCell> nextFrontier = new();
            foreach (HexCell frontierHex in frontier)
            {
                foreach (HexCell neighbor in frontierHex.GetNeighbors(hexGrid))
                {
                    if (neighbor == null || visited.Contains(neighbor))
                    {
                        continue;
                    }

                    visited.Add(neighbor);
                    nextFrontier.Add(neighbor);
                }
            }

            HexCell bestAtDistance = nextFrontier
                .Where(IsLegalDestination)
                .Where(candidate => HexGridUtils.GetHexStepDistance(candidate, defensiveGoalHex) < originDistanceToGoal)
                .OrderBy(candidate => HexGridUtils.GetHexStepDistance(candidate, defensiveGoalHex))
                .ThenBy(candidate => Mathf.Abs(candidate.coordinates.z - defensiveGoalHex.coordinates.z))
                .ThenBy(candidate => candidate.coordinates.x)
                .ThenBy(candidate => candidate.coordinates.z)
                .FirstOrDefault();

            if (bestAtDistance != null)
            {
                return bestAtDistance;
            }

            frontier = nextFrontier;
        }

        return null;
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

    private bool IsLegalDestination(HexCell cell)
    {
        return cell != null
            && !cell.isOutOfBounds
            && cell.isInGoal == 0
            && cell.GetOccupyingToken() == null
            && !cell.isAttackOccupied
            && !cell.isDefenseOccupied;
    }

    private IEnumerator MoveTokenDirect(PlayerToken token, HexCell targetHex)
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

        targetHex.isAttackOccupied = token.isAttacker;
        targetHex.isDefenseOccupied = !token.isAttacker;

        yield return StartCoroutine(token.JumpToHex(targetHex));
        targetHex.ResetHighlight();
    }
}
