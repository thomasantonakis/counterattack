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

    // public static List<Vector3Int> HexRing(Vector3Int center, int radius)
    // {
    //     List<Vector3Int> results = new List<Vector3Int>();
    //     Vector3Int hex = center + hexDirections[4] * radius;
    //     for (int i = 0; i < 6; i++)
    //     {
    //         for (int j = 0; j < radius; j++)
    //         {
    //             results.Add(hex);
    //             hex = GetNeighbor(hex, i);
    //         }
    //     }
    //     return results;
    // }


}
