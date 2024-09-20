using UnityEngine;
using System.Collections.Generic;

public static class HexGridUtils
{
    // Convert axial coordinates (x, z) to cube coordinates (x, y, z)
    // Remember that y = -x - z in cube coordinates
    public static Vector3Int AxialToCube(Vector3Int axialCoords)
    {
        int x = axialCoords.x;
        int z = axialCoords.z;
        int y = -x - z;
        return new Vector3Int(x, y, z);
    }

    // Function to calculate distance between two hexes in cube coordinates
    public static int GetHexDistance(Vector3Int cubeA, Vector3Int cubeB)
    {
        // Calculate the distance using cube coordinates
        return Mathf.Max(
            Mathf.Abs(cubeA.x - cubeB.x), 
            Mathf.Abs(cubeA.y - cubeB.y), 
            Mathf.Abs(cubeA.z - cubeB.z)
        );
    }

    public static Vector3Int[] directions = new Vector3Int[]
    {
        new Vector3Int(1, -1, 0),  // Right
        new Vector3Int(1, 0, -1),  // Top-right
        new Vector3Int(0, 1, -1),  // Top-left
        new Vector3Int(-1, 1, 0),  // Left
        new Vector3Int(-1, 0, 1),  // Bottom-left
        new Vector3Int(0, -1, 1)   // Bottom-right
    };

    public static Vector3Int GetDirection(int index)
    {
        return directions[index % 6];  // Ensure index is within bounds
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

}
