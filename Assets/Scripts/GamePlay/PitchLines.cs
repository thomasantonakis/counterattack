using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PitchLines : MonoBehaviour
{
    private const string GeneratedRootName = "GeneratedVisuals";
    private const string MarkingsRootName = "PitchMarkings";
    private const string DotsRootName = "PitchDots";
    private const string NetsRootName = "GoalNets";
    private const string BlockersRootName = "OutOfBoundsBlockers";
    private const string ActionLabelsRootName = "PitchActionLabels";

    private const float LineLift = 0.03f;
    private const float DotLift = 0.031f;
    private const float GoalNetLift = 0.06f;
    private const float OutOfBoundsLift = 0.05f;
    private const int FullCircleSegments = 96;
    private const int PenaltyArcSegments = 48;
    private const int CornerArcSegments = 24;

    // Change these four world positions/scales to move the editable pitch labels.
    private static readonly PitchActionLabelSpec[] ActionLabelSpecs =
    {
        new("NorthWest F3", "FINAL THIRD", -1, new Vector3(-9.63f, 0.06f, 11f), new Vector3(8.18f, 1f, 0.33f)),
        new("SouthWest F3", "FINAL THIRD", -1, new Vector3(-9.63f, 0.06f, -11f), new Vector3(8.18f, 1f, 0.33f)),
        new("NorthEast F3", "FINAL THIRD", 1, new Vector3(9.63f, 0.06f, 11f), new Vector3(8.18f, 1f, 0.33f)),
        new("SouthEast F3", "FINAL THIRD", 1, new Vector3(9.63f, 0.06f, -11f), new Vector3(8.18f, 1f, 0.33f)),
    };

    private static readonly Color DefaultActionLabelBackgroundColor = new Color(0.78f, 0.05f, 0.05f, 0.92f);
    private static readonly Color DefaultActionLabelTextColor = Color.white;
    private static readonly Color FallbackHomeBackgroundColor = new Color(0.05f, 0.22f, 0.62f, 0.92f);
    private static readonly Color FallbackAwayBackgroundColor = new Color(0.78f, 0.36f, 0.08f, 0.92f);
    private static readonly Vector2 DefaultActionLabelTextAreaSize = new Vector2(1.76f, 0.31f);
    private const float DefaultActionLabelFontSize = 1.5f;

    [Header("Dependencies")]
    public HexGrid hexGrid;
    [SerializeField] private FinalThirdManager finalThirdManager;

    [Header("Generated Assets")]
    [SerializeField] private Material lineMaterial;
    [SerializeField] private Material netMaterial;
    [SerializeField] private Material blockerMaterial;
    [SerializeField] private Material actionLabelBackgroundMaterial;
    [SerializeField] private Sprite dotSprite;
    [SerializeField] private Transform generatedRoot;

    [Header("Pitch Action Labels")]
    [SerializeField] private Vector2 actionLabelTextAreaSize = DefaultActionLabelTextAreaSize;
    [SerializeField] private float actionLabelFontSize = DefaultActionLabelFontSize;
    [SerializeField] private List<PitchActionLabelItem> actionLabels = new();

    private MaterialPropertyBlock actionLabelPropertyBlock;
    private string cachedHomeKit = string.Empty;
    private string cachedAwayKit = string.Empty;
    private TokenKitInstructionPalette homeInstructionPalette;
    private TokenKitInstructionPalette awayInstructionPalette;

    // The Room board visuals are scene-owned. This builder recreates the serialized child
    // hierarchy in edit mode so play mode only consumes the authored result.
    public void ConfigureGeneratedAssets(Material lines, Material nets, Material blockers, Material actionLabelBackground, Sprite dots)
    {
        lineMaterial = lines;
        netMaterial = nets;
        blockerMaterial = blockers;
        actionLabelBackgroundMaterial = actionLabelBackground;
        dotSprite = dots;
        actionLabelTextAreaSize = DefaultActionLabelTextAreaSize;
        actionLabelFontSize = DefaultActionLabelFontSize;
    }

    private void Start()
    {
        ResolveRuntimeDependencies();
        CacheActionLabelsFromScene();
        RefreshInstructionPalettes(force: true);
        UpdateActionLabelColors();
    }

    private void Update()
    {
        UpdateActionLabelColors();
    }

    public void RebuildSceneVisuals()
    {
        if (hexGrid == null)
        {
            Debug.LogError("PitchBoardVisuals cannot rebuild without a HexGrid reference.");
            return;
        }

        if (lineMaterial == null || netMaterial == null || blockerMaterial == null || actionLabelBackgroundMaterial == null || dotSprite == null)
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
        Transform actionLabelsRoot = EnsureContainer(ActionLabelsRootName, generatedRoot);

        BuildPitchMarkings(markingsRoot, ignoreRaycastLayer);
        BuildPitchDots(dotsRoot, ignoreRaycastLayer);
        BuildGoalNets(netsRoot, ignoreRaycastLayer);
        BuildOutOfBoundsBlockers(blockersRoot);
        BuildActionLabels(actionLabelsRoot, ignoreRaycastLayer);
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

    private void BuildActionLabels(Transform root, int layer)
    {
        actionLabels.Clear();

        foreach (PitchActionLabelSpec spec in ActionLabelSpecs)
        {
            GameObject labelRoot = new GameObject(spec.Name);
            labelRoot.transform.SetParent(root, false);
            labelRoot.transform.position = spec.Position;
            labelRoot.transform.localScale = spec.Scale;
            labelRoot.layer = layer;

            GameObject backgroundObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            backgroundObject.name = "Background";
            backgroundObject.transform.SetParent(labelRoot.transform, false);
            backgroundObject.transform.localPosition = Vector3.zero;
            backgroundObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            backgroundObject.transform.localScale = Vector3.one;
            backgroundObject.layer = layer;

            MeshRenderer backgroundRenderer = backgroundObject.GetComponent<MeshRenderer>();
            backgroundRenderer.sharedMaterial = actionLabelBackgroundMaterial;

            Collider backgroundCollider = backgroundObject.GetComponent<Collider>();
            if (backgroundCollider != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(backgroundCollider);
                }
                else
                {
                    DestroyImmediate(backgroundCollider);
                }
            }

            GameObject textObject = new GameObject("Text");
            textObject.transform.SetParent(labelRoot.transform, false);
            textObject.transform.localPosition = new Vector3(0f, 0.012f, 0f);
            textObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            textObject.transform.localScale = GetInverseHorizontalLabelScale(spec.Scale);
            textObject.layer = layer;

            TextMeshPro text = textObject.AddComponent<TextMeshPro>();
            text.text = spec.Text;
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = actionLabelFontSize;
            text.fontStyle = FontStyles.Bold;
            text.color = DefaultActionLabelTextColor;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.rectTransform.sizeDelta = actionLabelTextAreaSize;

            actionLabels.Add(new PitchActionLabelItem(spec.Side, text, backgroundRenderer));
        }

        UpdateActionLabelColors();
    }

    private void ResolveRuntimeDependencies()
    {
        if (finalThirdManager == null)
        {
            finalThirdManager = FindAnyObjectByType<FinalThirdManager>();
        }
    }

    private void CacheActionLabelsFromScene()
    {
        actionLabels.RemoveAll(label => label == null || label.Text == null || label.Background == null);
    }

    private void UpdateActionLabelColors()
    {
        if (actionLabels == null || actionLabels.Count == 0)
        {
            return;
        }

        ResolveRuntimeDependencies();
        RefreshInstructionPalettes(force: false);

        int activeSide = 0;
        bool expectingHomeTeam = false;
        bool hasActiveFinalThirdLabel = finalThirdManager != null
            && finalThirdManager.TryGetPitchActionLabelState(out activeSide, out expectingHomeTeam);

        TokenKitInstructionPalette activePalette = expectingHomeTeam
            ? homeInstructionPalette
            : awayInstructionPalette;
        bool useSwappedColors = hasActiveFinalThirdLabel && Mathf.FloorToInt(Time.unscaledTime) % 2 == 1;

        foreach (PitchActionLabelItem label in actionLabels)
        {
            bool shouldHighlight = hasActiveFinalThirdLabel && label.Side == activeSide;
            Color backgroundColor = DefaultActionLabelBackgroundColor;
            Color textColor = DefaultActionLabelTextColor;
            if (shouldHighlight)
            {
                backgroundColor = useSwappedColors ? activePalette.Secondary : activePalette.Primary;
                textColor = useSwappedColors ? activePalette.Primary : activePalette.Secondary;
            }
            ApplyActionLabelColors(label, backgroundColor, textColor);
        }
    }

    private void RefreshInstructionPalettes(bool force)
    {
        if (MatchManager.Instance?.gameData?.gameSettings == null)
        {
            cachedHomeKit = string.Empty;
            cachedAwayKit = string.Empty;
            homeInstructionPalette = new TokenKitInstructionPalette(FallbackHomeBackgroundColor, Color.white);
            awayInstructionPalette = new TokenKitInstructionPalette(FallbackAwayBackgroundColor, Color.white);
            return;
        }

        string homeKit = MatchManager.Instance.gameData.gameSettings.homeKit ?? string.Empty;
        string awayKit = MatchManager.Instance.gameData.gameSettings.awayKit ?? string.Empty;
        if (!force && homeKit == cachedHomeKit && awayKit == cachedAwayKit)
        {
            return;
        }

        homeInstructionPalette = TokenKitCatalog.ResolveInstructionPalette(homeKit, FallbackHomeBackgroundColor, Color.white);
        awayInstructionPalette = TokenKitCatalog.ResolveInstructionPalette(awayKit, FallbackAwayBackgroundColor, Color.white);
        cachedHomeKit = homeKit;
        cachedAwayKit = awayKit;
    }

    private void ApplyActionLabelColors(PitchActionLabelItem label, Color backgroundColor, Color textColor)
    {
        if (label.Text != null)
        {
            label.Text.color = textColor;
        }

        if (label.Background == null)
        {
            return;
        }

        actionLabelPropertyBlock ??= new MaterialPropertyBlock();
        label.Background.GetPropertyBlock(actionLabelPropertyBlock);
        actionLabelPropertyBlock.SetColor("_Color", backgroundColor);
        actionLabelPropertyBlock.SetColor("_BaseColor", backgroundColor);
        label.Background.SetPropertyBlock(actionLabelPropertyBlock);
    }

    private static Vector3 GetInverseHorizontalLabelScale(Vector3 parentScale)
    {
        float inverseX = !Mathf.Approximately(parentScale.x, 0f) ? 1f / parentScale.x : 1f;
        float inverseY = !Mathf.Approximately(parentScale.z, 0f) ? 1f / parentScale.z : 1f;
        return new Vector3(inverseX, inverseY, 1f);
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

    private readonly struct PitchActionLabelSpec
    {
        public string Name { get; }
        public string Text { get; }
        public int Side { get; }
        public Vector3 Position { get; }
        public Vector3 Scale { get; }

        public PitchActionLabelSpec(string name, string text, int side, Vector3 position, Vector3 scale)
        {
            Name = name;
            Text = text;
            Side = side;
            Position = position;
            Scale = scale;
        }
    }

    [System.Serializable]
    private sealed class PitchActionLabelItem
    {
        [SerializeField] private int side;
        [SerializeField] private TextMeshPro text;
        [SerializeField] private MeshRenderer background;

        public int Side => side;
        public TextMeshPro Text => text;
        public MeshRenderer Background => background;

        public PitchActionLabelItem(int side, TextMeshPro text, MeshRenderer background)
        {
            this.side = side;
            this.text = text;
            this.background = background;
        }
    }
}
