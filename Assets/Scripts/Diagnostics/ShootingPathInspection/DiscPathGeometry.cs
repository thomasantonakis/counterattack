using System;
using UnityEngine;

/// <summary>XZ ball-disc geometry shared by ground paths and the diagnostic inspector.</summary>
public static class DiscPathGeometry
{
    private const float Epsilon = 0.000001f;
    private const int SemicircleSteps = 32;
    public const float MinimumPathOverlapArea = 0.0005f;

    /// <summary>CCW polygon approximating the capsule, with 64 steps around both round ends.</summary>
    public static Vector2[] BuildCapsulePolygon(Vector2 start, Vector2 end, float radius)
    {
        if (radius < 0f) throw new ArgumentOutOfRangeException(nameof(radius));
        Vector2 direction = (end - start).sqrMagnitude > Epsilon ? (end - start).normalized : Vector2.right;
        Vector2 left = new Vector2(-direction.y, direction.x);
        var outline = new Vector2[2 * SemicircleSteps + 2];
        int count = 0;
        outline[count++] = start - left * radius;
        outline[count++] = end - left * radius;
        for (int i = 1; i <= SemicircleSteps; i++)
        {
            float angle = -Mathf.PI * 0.5f + Mathf.PI * i / SemicircleSteps;
            outline[count++] = end + radius * (Mathf.Cos(angle) * direction + Mathf.Sin(angle) * left);
        }
        outline[count++] = start + left * radius;
        for (int i = 1; i < SemicircleSteps; i++)
        {
            float angle = Mathf.PI * 0.5f + Mathf.PI * i / SemicircleSteps;
            outline[count++] = start + radius * (Mathf.Cos(angle) * direction + Mathf.Sin(angle) * left);
        }
        return outline;
    }

    public static bool CapsuleCoversPathHex(Vector2 start, Vector2 end, float radius,
        Vector2[] hexPolygon, Vector2[] capsulePolygon)
    {
        if (!CapsuleTouchesPolygon(start, end, radius, hexPolygon)) return false;
        if (radius <= 0f) return true;
        return ConvexIntersectionArea(hexPolygon, capsulePolygon) >= MinimumPathOverlapArea;
    }

    public static float ConvexIntersectionArea(Vector2[] polygon, Vector2[] clipper)
    {
        if (polygon == null || polygon.Length < 3 || clipper == null || clipper.Length < 3) return 0f;
        var current = new System.Collections.Generic.List<Vector2>(polygon);
        var next = new System.Collections.Generic.List<Vector2>(polygon.Length + clipper.Length);
        for (int edge = 0; edge < clipper.Length; edge++)
        {
            if (current.Count == 0) return 0f;
            next.Clear();
            Vector2 a = clipper[edge], b = clipper[(edge + 1) % clipper.Length];
            Vector2 previous = current[current.Count - 1];
            float previousSide = Cross(b - a, previous - a);
            foreach (Vector2 point in current)
            {
                float side = Cross(b - a, point - a);
                if ((side >= 0f) != (previousSide >= 0f))
                {
                    float fraction = previousSide / (previousSide - side);
                    next.Add(previous + fraction * (point - previous));
                }
                if (side >= 0f) next.Add(point);
                previous = point;
                previousSide = side;
            }
            var swap = current;
            current = next;
            next = swap;
        }
        double twiceArea = 0d;
        for (int i = 0; i < current.Count; i++)
        {
            Vector2 a = current[i], b = current[(i + 1) % current.Count];
            twiceArea += (double)a.x * b.y - (double)a.y * b.x;
        }
        return (float)(Math.Abs(twiceArea) * 0.5d);
    }

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
