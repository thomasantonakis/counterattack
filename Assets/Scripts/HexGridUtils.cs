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
    public static int CalculateHexDistance(Vector3Int hexA, Vector3Int hexB)
    {
        // The distance between two hexes is the maximum of the differences in their cube coordinates
        return Mathf.Max(Mathf.Abs(hexA.x - hexB.x), Mathf.Abs(hexA.y - hexB.y), Mathf.Abs(hexA.z - hexB.z));
    }

    // Function to get all hexes within a certain number of steps from a start hex
    public static List<HexCell> GetHexesInRange(HexGrid hexGrid, HexCell startHex, int range)
    {
        List<HexCell> hexesInRange = new List<HexCell>();
        Vector3Int startCoords = startHex.coordinates;

        for (int dx = -range; dx <= range; dx++)
        {
            for (int dy = Mathf.Max(-range, -dx - range); dy <= Mathf.Min(range, -dx + range); dy++)
            {
                int dz = -dx - dy;
                Vector3Int currentCoords = new Vector3Int(startCoords.x + dx, startCoords.y + dy, startCoords.z + dz);

                HexCell hex = hexGrid.GetHexCellAt(currentCoords);
                if (hex != null)
                {
                    hexesInRange.Add(hex);
                }
            }
        }

        return hexesInRange;
    }

    // Function to calculate the number of steps (hexes) between two hexes
    public static int GetStepsBetweenHexes(HexCell startHex, HexCell targetHex)
    {
        Vector3Int startCoords = startHex.coordinates;
        Vector3Int targetCoords = targetHex.coordinates;
        return CalculateHexDistance(startCoords, targetCoords);
    }
}
