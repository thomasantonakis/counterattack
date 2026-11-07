using UnityEngine;

/// <summary>Authored from the bowl opening and blocker coverage, shared by every camera path.</summary>
public sealed class RoomCameraEnvelope : MonoBehaviour
{
    [SerializeField] private Bounds positionEnvelope = new(Vector3.up * 45f, new Vector3(28f, 88f, 24f));
    [SerializeField] private Bounds groundCoverage = new(Vector3.zero, new Vector3(100f, 0.1f, 48f));
    [SerializeField] private float groundHeight = 0.05f;
    [SerializeField, Min(0.1f)] private float edgeMargin = 0.75f;
    [SerializeField] private Vector2[] stadiumShellOutline = System.Array.Empty<Vector2>();
    [SerializeField] private float closedShellHeight;
    public Bounds PositionEnvelope => positionEnvelope;
    public Bounds GroundCoverage => groundCoverage;

    public void Configure(Bounds positions, Bounds coverage, float height)
    { positionEnvelope = positions; groundCoverage = coverage; groundHeight = height; }
    public void ConfigureShell(Vector2[] outline, float height) { stadiumShellOutline = outline; closedShellHeight = height; }

    public void Constrain(Camera camera, Vector3 target, ref Vector3 position, ref Quaternion rotation, float? horizontalShift = null, float minViewDistance = 0f)
    {
        if (camera == null) return;
        // Keep the requested rotation. The authored backdrop covers all ground
        // within the far clip; constraints translate or contract the pose instead.
        position = positionEnvelope.ClosestPoint(position);
        for (int step = 0; step < 64; step++)
        {
            if (ContainsFrustum(camera, position, rotation, horizontalShift)) return;
            position.y = Mathf.Lerp(position.y, positionEnvelope.min.y, 0.15f);
        }
    }

    private static Quaternion Look(Vector3 direction, Quaternion previous)
    {
        Vector3 up = Mathf.Abs(Vector3.Dot(direction.normalized, Vector3.up)) > 0.999f ? previous * Vector3.up : Vector3.up;
        return Quaternion.LookRotation(direction, up);
    }

    public bool ContainsFrustum(Camera camera, Vector3 position, Quaternion rotation, float? horizontalShift = null)
    {
        float tanV = Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f), tanH = tanV * camera.aspect;
        float shift = horizontalShift ?? camera.projectionMatrix[0, 2];
        for (int i = 0; i < 4; i++)
        {
            Vector3 direction = rotation * new Vector3((((i & 1) == 0 ? -1f : 1f) + shift) * tanH, (i & 2) == 0 ? -tanV : tanV, 1f);
            if (direction.y >= -0.0001f)
            {
                // From below the continuous outer wall, upward rays reveal stands/sky.
                continue;
            }
            float distance = (groundHeight - position.y) / direction.y;
            if (distance < camera.nearClipPlane || distance > camera.farClipPlane) continue;
            Vector3 point = position + direction * distance;
            if (point.x < groundCoverage.min.x + edgeMargin || point.x > groundCoverage.max.x - edgeMargin
                || point.z < groundCoverage.min.z + edgeMargin || point.z > groundCoverage.max.z - edgeMargin)
            {
                if (!IsOccludedByShell(position, direction)) return false;
            }
        }
        return true;
    }

    private bool IsOccludedByShell(Vector3 position, Vector3 direction)
    {
        float exit = float.PositiveInfinity;
        for (int i = 0; i < stadiumShellOutline.Length; i++)
        {
            Vector2 a = stadiumShellOutline[i], b = stadiumShellOutline[(i + 1) % stadiumShellOutline.Length];
            Vector2 edge = b - a;
            float denominator = edge.x * direction.z - edge.y * direction.x;
            if (denominator >= -0.00001f) continue; // CCW convex hull: this ray exits this half plane.
            float numerator = edge.x * (position.z - a.y) - edge.y * (position.x - a.x);
            exit = Mathf.Min(exit, -numerator / denominator);
        }
        if (float.IsInfinity(exit) || exit < 0f) return false;
        float height = position.y + direction.y * exit;
        return height >= groundHeight && height < closedShellHeight;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green; Gizmos.DrawWireCube(positionEnvelope.center, positionEnvelope.size);
        Gizmos.color = Color.cyan; Gizmos.DrawWireCube(groundCoverage.center, groundCoverage.size);
    }
}
