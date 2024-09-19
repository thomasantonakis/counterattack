using UnityEngine;
public static class HexGridUtils
{
    public static Vector3 AxialLerp(Vector3Int a, Vector3Int b, float t)
    {
        return new Vector3(
            Mathf.Lerp(a.x, b.x, t),
            Mathf.Lerp(a.y, b.y, t),
            Mathf.Lerp(a.z, b.z, t)
        );
    }

    public static Vector3Int AxialRound(Vector3 axial)
    {
        int rx = Mathf.RoundToInt(axial.x);
        int rz = Mathf.RoundToInt(axial.z);
        int ry = -rx - rz;

        return new Vector3Int(rx, ry, rz);
    }
}
