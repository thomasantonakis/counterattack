using System;
using UnityEngine;

/// <summary>Diagnostic-only XZ geometry. No occupancy, match state, or gameplay path changes.</summary>
public static class DiscPathGeometry
{
    private const float Epsilon = 0.000001f;

    public static bool CapsuleTouchesPolygon(Vector2 start, Vector2 end, float radius, Vector2[] polygon)
    {
        if (radius < 0f) throw new ArgumentOutOfRangeException(nameof(radius));
        if (polygon == null || polygon.Length < 3) return false;
        float minX = Mathf.Min(start.x, end.x) - radius - Epsilon;
        float maxX = Mathf.Max(start.x, end.x) + radius + Epsilon;
        float minY = Mathf.Min(start.y, end.y) - radius - Epsilon;
        float maxY = Mathf.Max(start.y, end.y) + radius + Epsilon;
        float polygonMinX = float.PositiveInfinity, polygonMaxX = float.NegativeInfinity;
        float polygonMinY = float.PositiveInfinity, polygonMaxY = float.NegativeInfinity;
        foreach (Vector2 vertex in polygon)
        {
            polygonMinX = Mathf.Min(polygonMinX, vertex.x); polygonMaxX = Mathf.Max(polygonMaxX, vertex.x);
            polygonMinY = Mathf.Min(polygonMinY, vertex.y); polygonMaxY = Mathf.Max(polygonMaxY, vertex.y);
        }
        if (polygonMaxX < minX || polygonMinX > maxX || polygonMaxY < minY || polygonMinY > maxY) return false;
        if (InsideConvex(start, polygon) || InsideConvex(end, polygon)) return true;
        float squaredRadius = (radius + Epsilon) * (radius + Epsilon);
        for (int i = 0; i < polygon.Length; i++)
            if (SegmentDistanceSquared(start, end, polygon[i], polygon[(i + 1) % polygon.Length]) <= squaredRadius) return true;
        return false;
    }

    private static bool InsideConvex(Vector2 point, Vector2[] polygon)
    {
        bool positive = false, negative = false;
        for (int i = 0; i < polygon.Length; i++)
        {
            float cross = Cross(polygon[(i + 1) % polygon.Length] - polygon[i], point - polygon[i]);
            positive |= cross > Epsilon;
            negative |= cross < -Epsilon;
            if (positive && negative) return false;
        }
        return true;
    }

    private static float SegmentDistanceSquared(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        Vector2 ab = b - a, cd = d - c;
        float denominator = Cross(ab, cd);
        if (Mathf.Abs(denominator) > Epsilon)
        {
            float t = Cross(c - a, cd) / denominator;
            float u = Cross(c - a, ab) / denominator;
            if (t >= 0f && t <= 1f && u >= 0f && u <= 1f) return 0f;
        }
        return Mathf.Min(Mathf.Min(PointSegmentDistanceSquared(a, c, d), PointSegmentDistanceSquared(b, c, d)),
            Mathf.Min(PointSegmentDistanceSquared(c, a, b), PointSegmentDistanceSquared(d, a, b)));
    }

    private static float PointSegmentDistanceSquared(Vector2 point, Vector2 a, Vector2 b)
    {
        Vector2 delta = b - a;
        float t = delta.sqrMagnitude > 0f ? Mathf.Clamp01(Vector2.Dot(point - a, delta) / delta.sqrMagnitude) : 0f;
        return (point - (a + t * delta)).sqrMagnitude;
    }

    private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
}
