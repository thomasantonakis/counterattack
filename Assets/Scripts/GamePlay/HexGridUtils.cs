using UnityEngine;
using System.Collections.Generic;

public static class HexGridUtils
{
    public static string FormatHexCoordinates(HexCell hex)
    {
        return hex == null ? "unknown" : FormatHexCoordinates(hex.coordinates);
    }

    public static string FormatHexCoordinates(Vector3Int coordinates)
    {
        return $"({coordinates.x}, {coordinates.z})";
    }

    public static Vector3Int AxialToCube(Vector3Int axialCoords)
    {
        int x = axialCoords.x;
        int z = axialCoords.z;
        int y = -x - z;
        return new Vector3Int(x, y, z);
    }

    public static int GetHexDistance(Vector3Int cubeA, Vector3Int cubeB)
    // Function to calculate distance between two hexes in cube coordinates
    {
        // Calculate the distance using cube coordinates
        return Mathf.Max(
            Mathf.Abs(cubeA.x - cubeB.x), 
            Mathf.Abs(cubeA.y - cubeB.y), 
            Mathf.Abs(cubeA.z - cubeB.z)
        );
    }

    public static int GetHexStepDistance(Vector3Int offsetA, Vector3Int offsetB)
    {
        Vector3Int cubeA = OffsetToCube(offsetA.x, offsetA.z);
        Vector3Int cubeB = OffsetToCube(offsetB.x, offsetB.z);
        return GetHexDistance(cubeA, cubeB);
    }

    public static int GetHexStepDistance(HexCell fromHex, HexCell toHex)
    {
        if (fromHex == null || toHex == null)
        {
            Debug.LogError("GetHexStepDistance called with a null hex.");
            return int.MaxValue;
        }

        return GetHexStepDistance(fromHex.coordinates, toHex.coordinates);
    }

    public static Vector3Int OffsetToCube(int col, int row)
    {
        int x = col;
        int z = row - (col - (col & 1)) / 2;
        int y = -x - z;
        return new Vector3Int(x, y, z);  // This is the cube (q, r, s) coordinate
    }

    public static Vector2Int CubeToOffset(Vector3Int cubeCoords)
    {
        int col = cubeCoords.x;
        int row = cubeCoords.z + (cubeCoords.x - (cubeCoords.x & 1)) / 2;
        return new Vector2Int(col, row);  // This converts cube (q, r, s) back to even-q offset
    }

    public static List<HexCell> FindPath(HexCell startHex, HexCell targetHex, HexGrid hexGrid)
    {
        return FindPath(startHex, targetHex, hexGrid, null);
    }

    public static List<HexCell> FindPath(
        HexCell startHex,
        HexCell targetHex,
        HexGrid hexGrid,
        ISet<HexCell> temporarilyVacatedHexes)
    {
        if (startHex == null || targetHex == null || hexGrid == null)
        {
            Debug.LogError("StartHex, TargetHex, or HexGrid is null!");
            return new List<HexCell>();
        }
        if (!CanUseAsMovementDestination(targetHex, temporarilyVacatedHexes))
        {
            return new List<HexCell>();
        }

        Queue<HexCell> frontier = new Queue<HexCell>();
        HashSet<HexCell> visited = new HashSet<HexCell>();
        Dictionary<HexCell, HexCell> cameFrom = new Dictionary<HexCell, HexCell>();

        frontier.Enqueue(startHex);
        visited.Add(startHex);

        while (frontier.Count > 0)
        {
            HexCell currentHex = frontier.Dequeue();

            // If we reached the target, reconstruct the path
            if (currentHex == targetHex)
            {
                return ReconstructPath(cameFrom, currentHex);
            }

            foreach (HexCell neighbor in currentHex.GetNeighbors(hexGrid))
            {
                if (neighbor == null
                    || visited.Contains(neighbor)
                    || !CanUseForMovementPath(neighbor, targetHex, temporarilyVacatedHexes))
                {
                    continue;
                }

                frontier.Enqueue(neighbor);
                visited.Add(neighbor);
                cameFrom[neighbor] = currentHex;
            }
        }

        return new List<HexCell>(); // Return an empty list if no path is found
    }

    private static List<HexCell> ReconstructPath(Dictionary<HexCell, HexCell> cameFrom, HexCell currentHex)
    {
        List<HexCell> totalPath = new List<HexCell>();
        while (cameFrom.ContainsKey(currentHex))
        {
            totalPath.Add(currentHex);
            currentHex = cameFrom[currentHex];
        }
        totalPath.Reverse();  // Reverse the path to get it from start to target
        return totalPath;
    }

    private static bool CanUseAsMovementDestination(HexCell hex, ISet<HexCell> temporarilyVacatedHexes = null)
    {
        return hex != null
            && (hex.isInGoal != 0 || !hex.isOutOfBounds)
            && (temporarilyVacatedHexes?.Contains(hex) == true
                || (!hex.isAttackOccupied && !hex.isDefenseOccupied));
    }

    private static bool CanUseForMovementPath(
        HexCell hex,
        HexCell targetHex,
        ISet<HexCell> temporarilyVacatedHexes = null)
    {
        if (hex == null
            || ((hex.isAttackOccupied || hex.isDefenseOccupied)
                && temporarilyVacatedHexes?.Contains(hex) != true))
        {
            return false;
        }

        return !hex.isOutOfBounds
            || (hex == targetHex && hex.isInGoal != 0);
    }

    private static bool CanUseForReachability(HexCell hex, ISet<HexCell> temporarilyVacatedHexes = null)
    {
        return hex != null
            && !hex.isOutOfBounds
            && (temporarilyVacatedHexes?.Contains(hex) == true
                || (!hex.isAttackOccupied && !hex.isDefenseOccupied));
    }

    public static (List<HexCell> reachableHexes, Dictionary<HexCell, (int distance, bool enteredZOI)>) GetReachableHexes(HexGrid hexGrid, HexCell startHex, int range)
    {
        return GetReachableHexes(hexGrid, startHex, range, null);
    }

    public static (List<HexCell> reachableHexes, Dictionary<HexCell, (int distance, bool enteredZOI)>) GetReachableHexes(
        HexGrid hexGrid,
        HexCell startHex,
        int range,
        ISet<HexCell> temporarilyVacatedHexes)
    {
        if (startHex == null || hexGrid == null)
        {
            Debug.LogError("HexGrid or StartHex is null!");
            return (new List<HexCell>(), new Dictionary<HexCell, (int, bool)>());
        }

        List<HexCell> reachableHexes = new List<HexCell>();
        Queue<HexCell> frontier = new Queue<HexCell>();
        Dictionary<HexCell, int> distance = new Dictionary<HexCell, int>();

        frontier.Enqueue(startHex);
        distance[startHex] = 0;

        while (frontier.Count > 0)
        {
            HexCell currentHex = frontier.Dequeue();
            int currentDistance = distance[currentHex];

            if (currentDistance <= range)
            {
                reachableHexes.Add(currentHex);
            }

            // A boundary cell cannot contribute another reachable step. Most
            // cells in a small search are on this boundary.
            if (currentDistance >= range) continue;

            foreach (HexCell neighbor in currentHex.GetNeighbors(hexGrid))
            {
                if (neighbor == null
                    || !CanUseForReachability(neighbor, temporarilyVacatedHexes)
                    || distance.ContainsKey(neighbor))
                {
                    continue;
                }

                int neighborDistance = currentDistance + 1;

                if (neighborDistance <= range)
                {
                    frontier.Enqueue(neighbor);
                    distance[neighbor] = neighborDistance;
                }
            }
        }

        // Convert distance to include a dummy `enteredZOI` flag for compatibility
        Dictionary<HexCell, (int distance, bool enteredZOI)> finalDistance = new Dictionary<HexCell, (int, bool)>();
        foreach (var pair in distance)
        {
            finalDistance[pair.Key] = (pair.Value, false);
        }

        return (reachableHexes, finalDistance);
    }

}
