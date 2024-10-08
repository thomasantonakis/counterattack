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
        // Initialize the open and closed lists
        List<HexCell> openList = new List<HexCell>();
        HashSet<HexCell> closedList = new HashSet<HexCell>();

        // Add the starting hex to the open list
        openList.Add(startHex);

        // Dictionary to store the cost from start to each hex
        Dictionary<HexCell, float> gCosts = new Dictionary<HexCell, float>();
        gCosts[startHex] = 0;

        // Dictionary to store the path (came from)
        Dictionary<HexCell, HexCell> cameFrom = new Dictionary<HexCell, HexCell>();

        while (openList.Count > 0)
        {
            // Sort the open list based on the cost (could use a priority queue here)
            openList.Sort((a, b) => gCosts[a].CompareTo(gCosts[b]));
            HexCell currentHex = openList[0];

            // If we reached the target hex, reconstruct the path
            if (currentHex == targetHex)
            {
                return ReconstructPath(cameFrom, currentHex);
            }

            // Remove the current hex from the open list and add it to the closed list
            openList.Remove(currentHex);
            closedList.Add(currentHex);

            // Loop through each neighbor
            foreach (HexCell neighbor in currentHex.GetNeighbors(hexGrid))
            {
                if (closedList.Contains(neighbor) || neighbor.isAttackOccupied || neighbor.isDefenseOccupied)
                {
                    // Skip occupied or already visited hexes
                    continue;
                }

                // Calculate the tentative gCost
                float tentativeGCost = gCosts[currentHex] + HexGridUtils.GetHexDistance(currentHex.coordinates, neighbor.coordinates);

                if (!openList.Contains(neighbor))
                {
                    // If it's a new hex, add it to the open list
                    openList.Add(neighbor);
                }

                // Update the gCost and path if this is a better path
                if (!gCosts.ContainsKey(neighbor) || tentativeGCost < gCosts[neighbor])
                {
                    gCosts[neighbor] = tentativeGCost;
                    cameFrom[neighbor] = currentHex;
                }
            }
        }

        // Return an empty path if no valid path is found
        return new List<HexCell>();
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

}
