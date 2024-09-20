using UnityEngine;
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

    // Convert cube coordinates (x, y, z) to axial coordinates (x, z)
    // We ignore the y coordinate since it's derived from x and z
    public static Vector3Int CubeToAxial(Vector3Int cubeCoords)
    {
        int x = cubeCoords.x;
        int z = cubeCoords.z;
        return new Vector3Int(x, 0, z); // Return only axial coordinates
    }

    // Linear interpolation between two axial points
    public static Vector3 AxialLerp(Vector3Int startCoords, Vector3Int endCoords, float t)
    {
        Vector3 startCube = AxialToCube(startCoords);
        Vector3 endCube = AxialToCube(endCoords);

        float x = Mathf.Lerp(startCube.x, endCube.x, t);
        float y = Mathf.Lerp(startCube.y, endCube.y, t);
        float z = Mathf.Lerp(startCube.z, endCube.z, t);

        return new Vector3(x, y, z);
    }

    // Rounds floating-point cube coordinates to integer cube coordinates
    public static Vector3Int AxialRound(Vector3 axialCoords)
    {
        int rx = Mathf.RoundToInt(axialCoords.x);
        int ry = Mathf.RoundToInt(axialCoords.y);
        int rz = Mathf.RoundToInt(axialCoords.z);

        float xDiff = Mathf.Abs(rx - axialCoords.x);
        float yDiff = Mathf.Abs(ry - axialCoords.y);
        float zDiff = Mathf.Abs(rz - axialCoords.z);

        // Correct rounding errors by adjusting the coordinate with the largest difference
        if (xDiff > yDiff && xDiff > zDiff)
        {
            rx = -ry - rz;
        }
        else if (yDiff > zDiff)
        {
            ry = -rx - rz;
        }
        else
        {
            rz = -rx - ry;
        }

        return new Vector3Int(rx, ry, rz);
    }

    // Gets the distance between two hex cells using cube coordinates
    public static int CubeDistance(Vector3Int a, Vector3Int b)
    {
        return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y), Mathf.Abs(a.z - b.z));
    }
}
