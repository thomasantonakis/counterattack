using System.Collections.Generic;
using UnityEngine;

public class PitchLines : MonoBehaviour
{
    private const string GeneratedRootName = "GeneratedVisuals";
    private const string MarkingsRootName = "PitchMarkings";
    private const string DotsRootName = "PitchDots";
    private const string NetsRootName = "GoalNets";
    private const string BlockersRootName = "OutOfBoundsBlockers";

    private const float LineLift = 0.03f;
    private const float DotLift = 0.031f;
    private const float GoalNetLift = 0.06f;
    private const float OutOfBoundsLift = 0.05f;

    private const int FullCircleSegments = 96;
    private const int PenaltyArcSegments = 48;
    private const int CornerArcSegments = 24;

    [Header("Dependencies")]
    public HexGrid hexGrid;

    [Header("Generated Assets")]
    [SerializeField] private Material lineMaterial;
    [SerializeField] private Material netMaterial;
    [SerializeField] private Material blockerMaterial;
    [SerializeField] private Sprite dotSprite;
    [SerializeField] private Transform generatedRoot;

    // The Room board visuals are scene-owned. This builder recreates the serialized child
    // hierarchy in edit mode so play mode only consumes the authored result.
    public void ConfigureGeneratedAssets(Material lines, Material nets, Material blockers, Sprite dots)
    {
        lineMaterial = lines;
        netMaterial = nets;
        blockerMaterial = blockers;
        dotSprite = dots;
    }

    public void RebuildSceneVisuals()
    {
        if (hexGrid == null)
        {
            Debug.LogError("PitchBoardVisuals cannot rebuild without a HexGrid reference.");
            return;
        }

        if (lineMaterial == null || netMaterial == null || blockerMaterial == null || dotSprite == null)
        {
            Debug.LogError("PitchBoardVisuals is missing the shared assets needed to rebuild scene visuals.");
            return;
        }

        gameObject.name = "PitchBoardVisuals";
        generatedRoot = EnsureContainer(GeneratedRootName, transform);
        ClearChildren(generatedRoot);

        int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
        Transform markingsRoot = EnsureContainer(MarkingsRootName, generatedRoot);
        Transform dotsRoot = EnsureContainer(DotsRootName, generatedRoot);
        Transform netsRoot = EnsureContainer(NetsRootName, generatedRoot);
        Transform blockersRoot = EnsureContainer(BlockersRootName, generatedRoot);

        BuildPitchMarkings(markingsRoot, ignoreRaycastLayer);
        BuildPitchDots(dotsRoot, ignoreRaycastLayer);
        BuildGoalNets(netsRoot, ignoreRaycastLayer);
        BuildOutOfBoundsBlockers(blockersRoot);
    }

    private void BuildPitchMarkings(Transform root, int layer)
    {
        CreateLineMesh("Bottom Side Line", Corner(-18, -12, 5), Corner(18, -12, 0), 0.05f, LineLift, lineMaterial, root, layer);
        CreateLineMesh("Right Goal Line", Corner(18, -12, 0), Corner(18, 12, 2), 0.05f, LineLift, lineMaterial, root, layer);
        CreateLineMesh("Top Side Line", Corner(18, 12, 2), Corner(-18, 12, 3), 0.05f, LineLift, lineMaterial, root, layer);
        CreateLineMesh("Left Goal Line", Corner(-18, 12, 3), Corner(-18, -12, 5), 0.05f, LineLift, lineMaterial, root, layer);

        CreateLineMesh("Halfway Line", Midpoint(0, 12, 2), Midpoint(0, -12, 5), 0.05f, LineLift, lineMaterial, root, layer);

        CreateLineMesh("Left Penalty Box Side", Midpoint(-12, -7, 5), Midpoint(-12, 7, 2), 0.05f, LineLift, lineMaterial, root, layer);
        CreateLineMesh("Left Penalty Box Bottom", Corner(-18, -7, 5), Midpoint(-12, -7, 5), 0.05f, LineLift, lineMaterial, root, layer);
        CreateLineMesh("Left Penalty Box Top", Corner(-18, 7, 3), Midpoint(-12, 7, 2), 0.05f, LineLift, lineMaterial, root, layer);

        CreateLineMesh("Right Penalty Box Side", Midpoint(12, -7, 5), Midpoint(12, 7, 2), 0.05f, LineLift, lineMaterial, root, layer);
        CreateLineMesh("Right Penalty Box Bottom", Corner(18, -7, 0), Midpoint(12, -7, 5), 0.05f, LineLift, lineMaterial, root, layer);
        CreateLineMesh("Right Penalty Box Top", Corner(18, 7, 2), Midpoint(12, 7, 2), 0.05f, LineLift, lineMaterial, root, layer);

        CreateLineMesh("Left Six Yard Side", Center(-16, -5), Center(-16, 5), 0.05f, LineLift, lineMaterial, root, layer);
        CreateLineMesh("Left Six Yard Bottom", Center(-16, -5), Corner(-18, -5, 4), 0.05f, LineLift, lineMaterial, root, layer);
        CreateLineMesh("Left Six Yard Top", Center(-16, 5), Corner(-18, 5, 4), 0.05f, LineLift, lineMaterial, root, layer);

        CreateLineMesh("Right Six Yard Side", Center(16, -5), Center(16, 5), 0.05f, LineLift, lineMaterial, root, layer);
        CreateLineMesh("Right Six Yard Bottom", Center(16, -5), Corner(18, -5, 1), 0.05f, LineLift, lineMaterial, root, layer);
        CreateLineMesh("Right Six Yard Top", Center(16, 5), Corner(18, 5, 1), 0.05f, LineLift, lineMaterial, root, layer);

        CreateArcMesh("Center Circle", Center(0, 0), 3f, FullCircleSegments, 0.05f, LineLift, 0f, 360f, lineMaterial, root, layer, true);
        CreateArcMesh("Right Penalty Arc", Center(14, 0), 3f, PenaltyArcSegments, 0.05f, LineLift, 120f, 240f, lineMaterial, root, layer);
        CreateArcMesh("Left Penalty Arc", Center(-14, 0), 3f, PenaltyArcSegments, 0.05f, LineLift, 300f, 420f, lineMaterial, root, layer);

        CreateArcMesh("Bottom Left Corner", Corner(-18, -12, 5), 0.3f, CornerArcSegments, 0.03f, LineLift, 0f, 90f, lineMaterial, root, layer);
        CreateArcMesh("Top Left Corner", Corner(-18, 12, 3), 0.3f, CornerArcSegments, 0.03f, LineLift, 270f, 360f, lineMaterial, root, layer);
        CreateArcMesh("Bottom Right Corner", Corner(18, -12, 0), 0.3f, CornerArcSegments, 0.03f, LineLift, 90f, 180f, lineMaterial, root, layer);
        CreateArcMesh("Top Right Corner", Corner(18, 12, 2), 0.3f, CornerArcSegments, 0.03f, LineLift, 180f, 270f, lineMaterial, root, layer);
    }

    private void BuildPitchDots(Transform root, int layer)
    {
        CreateDot("Kick Off", Center(0, 0), 0.08f, root, layer);
        CreateDot("Right Penalty Spot", Center(14, 0), 0.08f, root, layer);
        CreateDot("Left Penalty Spot", Center(-14, 0), 0.08f, root, layer);
    }

    private void BuildGoalNets(Transform root, int layer)
    {
        CreateLineMesh("Left Goal Net", Corner(-20, 3, 2), Corner(-20, -3, 0), 0.05f, GoalNetLift, netMaterial, root, layer);
        CreateLineMesh("Left Goal Top Net", Corner(-18, 3, 3), Corner(-20, 3, 2), 0.05f, GoalNetLift, netMaterial, root, layer);
        CreateLineMesh("Left Goal Bottom Net", Corner(-18, -3, 5), Corner(-20, -3, 0), 0.05f, GoalNetLift, netMaterial, root, layer);

        CreateLineMesh("Right Goal Net", Corner(20, 3, 3), Corner(20, -3, 5), 0.05f, GoalNetLift, netMaterial, root, layer);
        CreateLineMesh("Right Goal Top Net", Corner(20, 3, 3), Corner(18, 3, 2), 0.05f, GoalNetLift, netMaterial, root, layer);
        CreateLineMesh("Right Goal Bottom Net", Corner(20, -3, 5), Corner(18, -3, 0), 0.05f, GoalNetLift, netMaterial, root, layer);

        CreateLineMesh("Left Goal Middle Net", Center(-19, 3), Center(-19, -4), 0.05f, GoalNetLift, netMaterial, root, layer);
        CreateLineMesh("Right Goal Middle Net", Center(19, 3), Center(19, -4), 0.05f, GoalNetLift, netMaterial, root, layer);
    }

    private void BuildOutOfBoundsBlockers(Transform root)
    {
        float horizontalOffset = hexGrid.GridWidth * hexGrid.HexRadius;
        float verticalOffset = hexGrid.GridHeight * hexGrid.HexRadius;

        CreateOutOfBoundsBlocker(
            "Left Out-of-Bounds",
            new Vector3(Corner(-18, 12, 5).x - horizontalOffset, OutOfBoundsLift, 0f),
            new Vector3(horizontalOffset * 2f, horizontalOffset * 2f, 1f),
            root
        );
        CreateOutOfBoundsBlocker(
            "Right Out-of-Bounds",
            new Vector3(Corner(18, -12, 2).x + horizontalOffset, OutOfBoundsLift, 0f),
            new Vector3(horizontalOffset * 2f, horizontalOffset * 2f, 1f),
            root
        );
        CreateOutOfBoundsBlocker(
            "Top Out-of-Bounds",
            new Vector3(0f, OutOfBoundsLift, Corner(-18, 12, 2).z + verticalOffset),
            new Vector3(verticalOffset * 2f, verticalOffset * 2f, 1f),
            root
        );
        CreateOutOfBoundsBlocker(
            "Bottom Out-of-Bounds",
            new Vector3(0f, OutOfBoundsLift, Corner(18, -12, 5).z - verticalOffset),
            new Vector3(verticalOffset * 2f, verticalOffset * 2f, 1f),
            root
        );
    }

    private void CreateLineMesh(string name, Vector3 start, Vector3 end, float thickness, float lift, Material material, Transform parent, int layer)
    {
        CreateStripMeshObject(name, new List<Vector3> { start, end }, thickness, lift, false, material, parent, layer);
    }

    private void CreateArcMesh(
        string name,
        Vector3 center,
        float radius,
        int segmentCount,
        float thickness,
        float lift,
        float startAngle,
        float endAngle,
        Material material,
        Transform parent,
        int layer,
        bool closedLoop = false
    )
    {
        List<Vector3> points = GetArcPoints(center, radius, segmentCount, startAngle, endAngle, closedLoop);
        CreateStripMeshObject(name, points, thickness, lift, closedLoop, material, parent, layer);
    }

    private void CreateStripMeshObject(
        string name,
        IReadOnlyList<Vector3> points,
        float thickness,
        float lift,
        bool closedLoop,
        Material material,
        Transform parent,
        int layer
    )
    {
        if (points == null || points.Count < 2)
        {
            return;
        }

        GameObject stripObject = new GameObject(name);
        stripObject.transform.SetParent(parent, false);
        stripObject.layer = layer;

        MeshFilter meshFilter = stripObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = stripObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = material;

        Mesh mesh = BuildStripMesh(name, points, thickness, lift, closedLoop);
        meshFilter.sharedMesh = mesh;
    }

    private Mesh BuildStripMesh(string meshName, IReadOnlyList<Vector3> points, float thickness, float lift, bool closedLoop)
    {
        Mesh mesh = new Mesh
        {
            name = $"{meshName} Mesh"
        };

        int segmentCount = closedLoop ? points.Count : points.Count - 1;
        List<Vector3> vertices = new List<Vector3>(segmentCount * 4);
        List<int> triangles = new List<int>(segmentCount * 12);
        List<Vector2> uvs = new List<Vector2>(segmentCount * 4);

        for (int i = 0; i < segmentCount; i++)
        {
            Vector3 start = points[i];
            Vector3 end = points[(i + 1) % points.Count];
            start.y = lift;
            end.y = lift;

            Vector3 direction = (end - start).normalized;
            Vector3 perpendicular = Vector3.Cross(Vector3.up, direction) * (thickness * 0.5f);
            int baseIndex = vertices.Count;

            vertices.Add(start - perpendicular);
            vertices.Add(start + perpendicular);
            vertices.Add(end - perpendicular);
            vertices.Add(end + perpendicular);

            uvs.Add(new Vector2(0f, 0f));
            uvs.Add(new Vector2(0f, 1f));
            uvs.Add(new Vector2(1f, 0f));
            uvs.Add(new Vector2(1f, 1f));

            triangles.Add(baseIndex);
            triangles.Add(baseIndex + 1);
            triangles.Add(baseIndex + 2);
            triangles.Add(baseIndex + 2);
            triangles.Add(baseIndex + 1);
            triangles.Add(baseIndex + 3);

            triangles.Add(baseIndex);
            triangles.Add(baseIndex + 2);
            triangles.Add(baseIndex + 1);
            triangles.Add(baseIndex + 2);
            triangles.Add(baseIndex + 3);
            triangles.Add(baseIndex + 1);
        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private List<Vector3> GetArcPoints(Vector3 center, float radius, int segmentCount, float startAngle, float endAngle, bool closedLoop)
    {
        int pointCount = closedLoop ? segmentCount : segmentCount + 1;
        List<Vector3> points = new List<Vector3>(pointCount);

        for (int i = 0; i < pointCount; i++)
        {
            float t = i / (float)segmentCount;
            float currentAngle = Mathf.Lerp(startAngle, endAngle, t);
            float currentAngleRad = Mathf.Deg2Rad * currentAngle;
            points.Add(new Vector3(
                center.x + radius * Mathf.Cos(currentAngleRad),
                center.y,
                center.z + radius * Mathf.Sin(currentAngleRad)
            ));
        }

        return points;
    }

    private void CreateDot(string name, Vector3 position, float radius, Transform parent, int layer)
    {
        GameObject dot = new GameObject(name);
        dot.transform.SetParent(parent, false);
        dot.transform.position = new Vector3(position.x, DotLift, position.z);
        dot.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        dot.transform.localScale = new Vector3(radius, radius, 1f);
        dot.layer = layer;

        SpriteRenderer spriteRenderer = dot.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = dotSprite;
        spriteRenderer.color = Color.white;
    }

    private void CreateOutOfBoundsBlocker(string name, Vector3 position, Vector3 scale, Transform parent)
    {
        GameObject blocker = GameObject.CreatePrimitive(PrimitiveType.Quad);
        blocker.name = name;
        blocker.transform.SetParent(parent, false);
        blocker.transform.position = position;
        blocker.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        blocker.transform.localScale = scale;
        blocker.GetComponent<Renderer>().sharedMaterial = blockerMaterial;

        if (blocker.GetComponent<OutOfBoundsClickBlocker>() == null)
        {
            blocker.AddComponent<OutOfBoundsClickBlocker>();
        }
    }

    private Transform EnsureContainer(string name, Transform parent)
    {
        Transform existingChild = parent.Find(name);
        if (existingChild != null)
        {
            existingChild.localPosition = Vector3.zero;
            existingChild.localRotation = Quaternion.identity;
            existingChild.localScale = Vector3.one;
            return existingChild;
        }

        GameObject container = new GameObject(name);
        container.transform.SetParent(parent, false);
        return container.transform;
    }

    private void ClearChildren(Transform parent)
    {
        List<GameObject> toDestroy = new List<GameObject>();
        foreach (Transform child in parent)
        {
            toDestroy.Add(child.gameObject);
        }

        foreach (GameObject child in toDestroy)
        {
            if (Application.isPlaying)
            {
                Destroy(child);
            }
            else
            {
                DestroyImmediate(child);
            }
        }
    }

    private Vector3 Center(int x, int z)
    {
        return hexGrid.GetHexCenterForCoordinates(new Vector3Int(x, 0, z));
    }

    private Vector3 Corner(int x, int z, int index)
    {
        return hexGrid.GetHexCornersForCoordinates(new Vector3Int(x, 0, z))[index];
    }

    private Vector3 Midpoint(int x, int z, int index)
    {
        return hexGrid.GetHexEdgeMidpointsForCoordinates(new Vector3Int(x, 0, z))[index];
    }
}
