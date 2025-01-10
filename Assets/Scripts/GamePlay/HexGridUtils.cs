using UnityEngine;
using System.Collections.Generic;

public static class HexGridUtils
{
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
        List<HexCell> openList = new List<HexCell>();
        HashSet<HexCell> closedList = new HashSet<HexCell>();

        Dictionary<HexCell, float> gCosts = new Dictionary<HexCell, float>();
        Dictionary<HexCell, HexCell> cameFrom = new Dictionary<HexCell, HexCell>();

        openList.Add(startHex);
        gCosts[startHex] = 0;

        while (openList.Count > 0)
        {
            // Sort openList by the lowest gCost (shortest distance so far)
            openList.Sort((a, b) => gCosts[a].CompareTo(gCosts[b]));
            HexCell currentHex = openList[0];

            // If we reached the target hex, reconstruct the path
            if (currentHex == targetHex)
            {
                return ReconstructPath(cameFrom, currentHex);
            }

            openList.Remove(currentHex);
            closedList.Add(currentHex);

            // Loop through neighbors of the current hex
            foreach (HexCell neighbor in currentHex.GetNeighbors(hexGrid))
            {
                if (neighbor == null || closedList.Contains(neighbor) || neighbor.isAttackOccupied || neighbor.isDefenseOccupied)
                {
                    continue;  // Skip null, occupied, or already processed hexes
                }

                // Check if stepping into a ZOI is required
                bool requiresZOI = false;
                foreach (HexCell neighborOfNeighbor in neighbor.GetNeighbors(hexGrid))
                {
                    if (neighborOfNeighbor != null && neighborOfNeighbor.isDefenseOccupied)
                    {
                        requiresZOI = true;
                        break;
                    }
                }

                // Calculate gCost based on ZOI
                float tentativeGCost = gCosts[currentHex] + HexGridUtils.GetHexDistance(currentHex.coordinates, neighbor.coordinates);

                // If this path requires stepping into a ZOI, increase the cost
                if (requiresZOI)
                {
                    tentativeGCost += 1.0f;  // Arbitrary extra cost for stepping into ZOI
                }

                // If the neighbor isn't in the open list or this path is cheaper, update path
                if (!openList.Contains(neighbor))
                {
                    openList.Add(neighbor);
                }
                if (!gCosts.ContainsKey(neighbor) || tentativeGCost < gCosts[neighbor])
                {
                    gCosts[neighbor] = tentativeGCost;
                    cameFrom[neighbor] = currentHex;
                }
            }
        }

        return new List<HexCell>();  // Return an empty list if no path is found
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

    public static (List<HexCell> reachableHexes, Dictionary<HexCell, (int distance, bool enteredZOI)>) GetReachableHexes(HexGrid hexGrid, HexCell startHex, int range)
    {
        if (startHex == null || hexGrid == null)
        {
            Debug.LogError("HexGrid or StartHex is null!");  // Additional debug for clarity
            return (new List<HexCell>(), new Dictionary<HexCell, (int, bool)>());  // Return empty collections
        }

        List<HexCell> reachableHexes = new List<HexCell>();
        Queue<HexCell> frontier = new Queue<HexCell>();  // Frontier for exploring hexes
        Dictionary<HexCell, (int distance, bool enteredZOI)> distance = new Dictionary<HexCell, (int, bool)>();  // Track distance and ZOI entry

        frontier.Enqueue(startHex);
        distance[startHex] = (0, false);  // Start hex has 0 distance and no ZOI entry

        while (frontier.Count > 0)
        {
            HexCell currentHex = frontier.Dequeue();
            var (currentDistance, enteredZOI) = distance[currentHex];

            // Add the hex to the reachable list if it's within range
            if (currentDistance <= range)
            {
                reachableHexes.Add(currentHex);
            }

            // Explore neighbors of the current hex
            foreach (HexCell neighbor in currentHex.GetNeighbors(hexGrid))
            {
                // Ensure neighbor is not null and has not been processed yet
                if (neighbor == null || neighbor.isAttackOccupied || distance.ContainsKey(neighbor))
                {
                    continue;  // Skip null hexes, occupied hexes, or already processed ones
                }

                // Check if the neighbor is blocked by other players (attackers or defenders)
                if (neighbor.isAttackOccupied || neighbor.isDefenseOccupied)
                {
                    continue;  // Skip hexes blocked by other players
                }

                // Check if this neighbor is within a ZOI (Zone of Influence)
                bool inDefenderZOI = false;
                foreach (HexCell neighborOfNeighbor in neighbor.GetNeighbors(hexGrid))
                {
                    if (neighborOfNeighbor != null && neighborOfNeighbor.isDefenseOccupied)
                    {
                        inDefenderZOI = true;  // This hex is within the ZOI of a defender
                        break;
                    }
                }

                // Determine if entering this neighbor would step into ZOI
                bool willEnterZOI = enteredZOI || inDefenderZOI;

                // If the current distance + 1 is within range, explore the neighbor
                if (currentDistance + 1 <= range)
                {
                    frontier.Enqueue(neighbor);
                    distance[neighbor] = (currentDistance + 1, willEnterZOI);
                }
            }
        }

        return (reachableHexes, distance);  // Return both the list of reachable hexes and the distance data
    }

}
