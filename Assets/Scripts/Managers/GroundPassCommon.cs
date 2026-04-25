using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum PassValidationFailureReason
{
    None,
    NullTarget,
    OutOfRange,
    BlockedByDefender,
    TargetOccupiedByDefender,
}

public readonly struct GroundPassValidationResult
{
    public readonly bool IsValid;
    public readonly bool IsDangerous;
    public readonly List<HexCell> PathHexes;
    public readonly PassValidationFailureReason FailureReason;

    public GroundPassValidationResult(bool isValid, bool isDangerous, List<HexCell> pathHexes, PassValidationFailureReason failureReason)
    {
        IsValid = isValid;
        IsDangerous = isDangerous;
        PathHexes = pathHexes;
        FailureReason = failureReason;
    }
}

public sealed class GroundInterceptionCandidate
{
    public HexCell DefenderHex;
    public PlayerToken DefenderToken;
    public HexCell ClosestInterceptionHex;
    public int ClosestInterceptionDistanceFromBall;
    public int DefenderDistanceFromBall;
    public bool IsBlockingPath;
}

public static class GroundPassCommon
{
    public static GroundPassValidationResult ValidateStandardPassPath(
        HexGrid hexGrid,
        Ball ball,
        HexCell targetHex,
        int maxDistance,
        bool isQuickThrow = false
    )
    {
        HexCell ballHex = ball != null ? ball.GetCurrentHex() : null;
        if (ballHex == null || targetHex == null)
        {
            Debug.LogError("Ball or target hex is null!");
            return new GroundPassValidationResult(false, false, null, PassValidationFailureReason.NullTarget);
        }

        List<HexCell> pathHexes = CalculateThickPath(hexGrid, ballHex, targetHex, ball.ballRadius);
        int distanceBetweenHexes = HexGridUtils.GetHexStepDistance(ballHex, targetHex);
        if (distanceBetweenHexes > maxDistance)
        {
            Debug.LogWarning($"Pass is out of range. Maximum steps allowed: {maxDistance}. Current steps: {distanceBetweenHexes}");
            return new GroundPassValidationResult(false, false, pathHexes, PassValidationFailureReason.OutOfRange);
        }

        if (isQuickThrow)
        {
            if (targetHex.isDefenseOccupied)
            {
                Debug.Log($"Quick throw target blocked by defender at hex: {targetHex.coordinates}");
                return new GroundPassValidationResult(false, false, pathHexes, PassValidationFailureReason.TargetOccupiedByDefender);
            }
        }
        else
        {
            foreach (HexCell hex in pathHexes)
            {
                if (hex != null && hex.isDefenseOccupied)
                {
                    Debug.Log($"Path blocked by defender at hex: {hex.coordinates}");
                    return new GroundPassValidationResult(false, false, pathHexes, PassValidationFailureReason.BlockedByDefender);
                }
            }
        }

        bool isDangerous = BuildOrderedInterceptionCandidates(
            hexGrid,
            ball,
            targetHex,
            isQuickThrow: isQuickThrow
        ).Count > 0;

        return new GroundPassValidationResult(true, isDangerous, pathHexes, PassValidationFailureReason.None);
    }

    public static string GetValidationFailureInstruction(PassValidationFailureReason failureReason)
    {
        return failureReason switch
        {
            PassValidationFailureReason.OutOfRange => "Pass invalid: out of range.",
            PassValidationFailureReason.BlockedByDefender => "Pass invalid: blocked by defender.",
            PassValidationFailureReason.TargetOccupiedByDefender => "Pass invalid: target occupied by defender.",
            PassValidationFailureReason.NullTarget => "Pass invalid: no valid target selected.",
            _ => string.Empty,
        };
    }

    public static List<GroundInterceptionCandidate> BuildOrderedInterceptionCandidates(
        HexGrid hexGrid,
        Ball ball,
        HexCell targetHex,
        IEnumerable<HexCell> candidateDefenders = null,
        bool isQuickThrow = false,
        IEnumerable<HexCell> blockingDefenderHexes = null
    )
    {
        HexCell ballHex = ball != null ? ball.GetCurrentHex() : null;
        if (ballHex == null || targetHex == null)
        {
            return new List<GroundInterceptionCandidate>();
        }

        List<HexCell> pathHexes = CalculateThickPath(hexGrid, ballHex, targetHex, ball.ballRadius);
        List<HexCell> relevantInterceptionHexes = GetRelevantInterceptionHexes(pathHexes, targetHex, isQuickThrow);
        HashSet<HexCell> relevantInterceptionHexSet = new HashSet<HexCell>(relevantInterceptionHexes);
        HashSet<HexCell> blockingHexSet = blockingDefenderHexes != null
            ? new HashSet<HexCell>(blockingDefenderHexes.Where(hex => hex != null))
            : new HashSet<HexCell>();

        IEnumerable<HexCell> defendersToEvaluate = candidateDefenders ?? hexGrid.GetDefenderHexes();
        List<GroundInterceptionCandidate> orderedCandidates = new List<GroundInterceptionCandidate>();

        foreach (HexCell defenderHex in defendersToEvaluate.Where(hex => hex != null))
        {
            PlayerToken defenderToken = defenderHex.occupyingToken;
            if (defenderToken == null)
            {
                continue;
            }

            bool isBlockingPath = blockingHexSet.Contains(defenderHex);
            List<HexCell> influencedHexes = defenderHex
                .GetNeighbors(hexGrid)
                .Where(hex => hex != null && relevantInterceptionHexSet.Contains(hex))
                .ToList();

            if (isBlockingPath)
            {
                influencedHexes.Add(defenderHex);
            }

            if (influencedHexes.Count == 0)
            {
                continue;
            }

            HexCell closestInterceptionHex = influencedHexes
                .Distinct()
                .OrderBy(hex => HexGridUtils.GetHexStepDistance(ballHex, hex))
                .ThenBy(hex => hex.coordinates.x)
                .ThenBy(hex => hex.coordinates.z)
                .First();

            orderedCandidates.Add(new GroundInterceptionCandidate
            {
                DefenderHex = defenderHex,
                DefenderToken = defenderToken,
                ClosestInterceptionHex = closestInterceptionHex,
                ClosestInterceptionDistanceFromBall = HexGridUtils.GetHexStepDistance(ballHex, closestInterceptionHex),
                DefenderDistanceFromBall = HexGridUtils.GetHexStepDistance(ballHex, defenderHex),
                IsBlockingPath = isBlockingPath,
            });
        }

        return orderedCandidates
            .OrderBy(candidate => candidate.ClosestInterceptionDistanceFromBall)
            .ThenBy(candidate => candidate.DefenderDistanceFromBall)
            .ThenBy(candidate => candidate.DefenderToken.tackling)
            .ThenBy(candidate => candidate.DefenderToken.playerName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static List<HexCell> GetRelevantInterceptionHexes(List<HexCell> pathHexes, HexCell targetHex, bool isQuickThrow = false)
    {
        if (pathHexes == null)
        {
            return new List<HexCell>();
        }

        if (isQuickThrow)
        {
            return pathHexes.Where(hex => hex == targetHex).ToList();
        }

        return pathHexes
            .Where(hex => hex != null && !hex.isAttackOccupied)
            .ToList();
    }

    public static List<HexCell> CalculateThickPath(HexGrid hexGrid, HexCell startHex, HexCell endHex, float ballRadius)
    {
        if (hexGrid == null || startHex == null || endHex == null)
        {
            return new List<HexCell>();
        }

        List<HexCell> path = new List<HexCell>();
        Vector3 startPos = startHex.GetHexCenter();
        Vector3 endPos = endHex.GetHexCenter();
        List<HexCell> candidateHexes = GetCandidateGroundPathHexes(hexGrid, startHex, endHex);

        foreach (HexCell candidateHex in candidateHexes)
        {
            Vector3 candidatePos = candidateHex.GetHexCenter();
            float distanceToLine = DistanceFromPointToLine(candidatePos, startPos, endPos);
            if (distanceToLine <= ballRadius && !path.Contains(candidateHex))
            {
                path.Add(candidateHex);
            }
        }

        path.Remove(startHex);
        return path;
    }

    public static List<HexCell> GetCandidateGroundPathHexes(HexGrid hexGrid, HexCell startHex, HexCell endHex)
    {
        List<HexCell> candidates = new List<HexCell>();
        Vector3Int startCoords = startHex.coordinates;
        Vector3Int endCoords = endHex.coordinates;

        int minX = Mathf.Min(startCoords.x, endCoords.x) - 1;
        int maxX = Mathf.Max(startCoords.x, endCoords.x) + 1;
        int minZ = Mathf.Min(startCoords.z, endCoords.z) - 1;
        int maxZ = Mathf.Max(startCoords.z, endCoords.z) + 1;

        for (int x = minX; x <= maxX; x++)
        {
            for (int z = minZ; z <= maxZ; z++)
            {
                Vector3Int coords = new Vector3Int(x, 0, z);
                HexCell hex = hexGrid.GetHexCellAt(coords);
                if (hex != null)
                {
                    candidates.Add(hex);
                }
            }
        }

        return candidates;
    }

    private static float DistanceFromPointToLine(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
    {
        Vector3 lineDirection = lineEnd - lineStart;
        float lineLength = lineDirection.magnitude;
        lineDirection.Normalize();

        float projectedLength = Mathf.Clamp(Vector3.Dot(point - lineStart, lineDirection), 0, lineLength);
        Vector3 closestPoint = lineStart + lineDirection * projectedLength;
        return Vector3.Distance(point, closestPoint);
    }
}
