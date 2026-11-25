using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ShootingPathInspectionAuthoring
{
    public const string ScenePath = "Assets/Scenes/ShootingPathInspection.unity";
    private const string AssetsPath = "Assets/Diagnostics/ShootingPathInspection/";

    [MenuItem("Tools/Diagnostics/Author Shooting Path Inspection Scene")]
    public static void Author()
    {
        Scene room = SceneManager.GetActiveScene();
        if (Application.isPlaying || room.path != "Assets/Scenes/Room.unity" || room.isDirty)
            throw new InvalidOperationException("Author from the saved Room scene, outside Play Mode.");
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            throw new InvalidOperationException("Inspection scene already exists; open it to adjust its authored objects.");

        // Copy through the Editor, then switch normally. Room is never modified.
        if (!EditorSceneManager.SaveScene(room, ScenePath, true)) throw new InvalidOperationException("Could not copy Room.");
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.name != "HexGrid" && root.name != "MatchBall" && root.name != "PitchBoardVisuals")
                UnityEngine.Object.DestroyImmediate(root);

        HexGrid grid = UnityEngine.Object.FindAnyObjectByType<HexGrid>();
        Ball ball = UnityEngine.Object.FindAnyObjectByType<Ball>();
        ball.enabled = false; // Retain the exact configured radius, without Ball.Start or save/match hooks.
        ball.transform.position = new Vector3(0, 0.25f, 0);
        grid.cells = new HexCell[grid.GridWidth, grid.GridHeight];
        typeof(HexGrid).GetMethod("ApplyPaletteForWeather", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(grid, new object[] { "Clear" });
        typeof(HexGrid).GetMethod("CreateGrid", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(grid, new object[] { null });
        HexCell[] cells = grid.GetComponentsInChildren<HexCell>();
        // Only passive authored renderers, cells, and the disabled reference ball survive.
        foreach (var script in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
            if (!(script is HexCell) && !(script is Ball) && !(script is TMP_Text)) UnityEngine.Object.DestroyImmediate(script);
        var labels = GameObject.Find("PitchBoardVisuals/GeneratedVisuals/PitchActionLabels");
        if (labels != null) UnityEngine.Object.DestroyImmediate(labels);

        var camera = new GameObject("Inspection Camera", typeof(Camera)).GetComponent<Camera>();
        camera.tag = "MainCamera";
        camera.transform.SetPositionAndRotation(new Vector3(0, 40, 0), Quaternion.Euler(90, 0, 0));
        camera.orthographic = true;
        camera.orthographicSize = 15f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.05f, 0.07f, 0.09f);
        var light = new GameObject("Inspection Light", typeof(Light)).GetComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        light.transform.rotation = Quaternion.Euler(60, -30, 0);

        var rootTool = new GameObject("Shooting Path Inspector");
        var tool = rootTool.AddComponent<ShootingPathInspector>();
        tool.inspectionCamera = camera;
        tool.referenceBall = ball;
        tool.cells = cells;
        tool.shootingCache = Resources.Load<TextAsset>("shootingpaths/shootingPaths");
        tool.validMaterial = Material("Valid Target", new Color(0.12f, 0.95f, 0.24f));
        tool.invalidMaterial = Material("Rejected Target", new Color(0.95f, 0.08f, 0.12f));
        Material blue = Material("Stored Path", new Color(0.05f, 0.2f, 1f));
        Material amber = Material("Corridor Outline", new Color(1f, 0.68f, 0.06f));
        Material fill = Material("Ball Corridor", new Color(1f, 0.68f, 0.06f, 0.22f), true);
        Mesh hex = MakeHex();
        AssetDatabase.CreateAsset(hex, AssetsPath + "InspectionHex.asset");
        Mesh rectangle = new Mesh { name = "Unit XZ rectangle" };
        rectangle.vertices = new[] { new Vector3(-0.5f, 0, -0.5f), new Vector3(-0.5f, 0, 0.5f), new Vector3(0.5f, 0, 0.5f), new Vector3(0.5f, 0, -0.5f) };
        rectangle.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        rectangle.RecalculateNormals();
        rectangle.RecalculateBounds();
        AssetDatabase.CreateAsset(rectangle, AssetsPath + "UnitCorridor.asset");
        var targetsRoot = new GameObject("Raised Target Markers (1–8)").transform;
        targetsRoot.SetParent(rootTool.transform);
        tool.targetMarkers = new Transform[8];
        tool.targetLabels = new TMP_Text[8];
        for (int i = 0; i < 8; i++)
        {
            Transform marker = Marker($"Target {i + 1}", hex, tool.validMaterial, targetsRoot);
            marker.localScale = Vector3.one * 0.95f;
            var text = new GameObject("Number", typeof(TextMeshPro)).GetComponent<TextMeshPro>();
            text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            text.transform.SetParent(marker, false);
            text.transform.localPosition = new Vector3(0, 0.03f, 0);
            text.transform.localRotation = Quaternion.Euler(90, 0, 0);
            text.rectTransform.sizeDelta = Vector2.one;
            text.fontSize = 5;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.black;
            text.text = (i + 1).ToString();
            tool.targetMarkers[i] = marker;
            tool.targetLabels[i] = text;
        }
        var cache = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, List<string>>>>(tool.shootingCache.text);
        int maxPath = cache.Values.SelectMany(v => v.Values).Max(p => p.Count);
        var pathRoot = new GameObject("Blue Stored Path Markers").transform;
        pathRoot.SetParent(rootTool.transform);
        tool.pathMarkers = Enumerable.Range(0, maxPath).Select(i => Marker($"Path {i}", hex, blue, pathRoot)).ToArray();
        tool.originMarker = Marker("Selected Origin", hex, amber, rootTool.transform);
        tool.corridor = Marker("Ball Width Corridor", rectangle, fill, rootTool.transform);
        tool.corridorOutline = Line("Corridor Edges", amber, 0.025f, true, rootTool.transform);
        tool.centerLine = Line("Center To Center", amber, 0.018f, false, rootTool.transform);

        var canvas = new GameObject("Inspector Instructions", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler)).GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600, 1000);
        var panel = new GameObject("Instructions Background", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        panel.transform.SetParent(canvas.transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1); rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(12, -12); rect.sizeDelta = new Vector2(820, 166);
        panel.GetComponent<UnityEngine.UI.Image>().color = new Color(0.025f, 0.035f, 0.05f, 0.9f);
        panel.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
        var instructions = new GameObject("Status", typeof(RectTransform), typeof(TextMeshProUGUI));
        instructions.transform.SetParent(panel.transform, false);
        var textRect = instructions.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(12, 8); textRect.offsetMax = new Vector2(-12, -8);
        tool.status = instructions.GetComponent<TextMeshProUGUI>();
        tool.status.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        tool.status.fontSize = 19; tool.status.raycastTarget = false;
        tool.Initialize();
        ConfigurePitchLineOverlay();
        ConfigureBallComparison();
        ConfigurePhysicalComparison();
        // Assets serialized from existing generated pitch meshes must remain persistent.
        PersistTransientMeshes(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = rootTool;
    }

    public static void ConfigurePitchLineOverlay()
    {
        if (SceneManager.GetActiveScene().path != ScenePath)
            throw new InvalidOperationException("Pitch overlay is restricted to the diagnostic scene.");
        const string materialPath = AssetsPath + "PitchLinesOnTop.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            var shader = Shader.Find("Diagnostics/ShootingInspection/PitchLinesOnTop");
            if (shader == null) throw new InvalidOperationException("Import the diagnostic pitch-line shader first.");
            material = new Material(shader) { name = "Diagnostic Pitch Lines On Top", color = Color.white };
            AssetDatabase.CreateAsset(material, materialPath);
        }
        foreach (string group in new[] { "PitchMarkings", "GoalNets" })
        {
            var root = GameObject.Find("PitchBoardVisuals/GeneratedVisuals/" + group);
            if (root == null) throw new InvalidOperationException("Missing authored pitch group: " + group);
            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true)) renderer.sharedMaterial = material;
        }
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    public static void ConfigureBallComparison()
    {
        if (Application.isPlaying || SceneManager.GetActiveScene().path != ScenePath)
            throw new InvalidOperationException("Author comparison objects only in the diagnostic scene outside Play Mode.");
        var tool = UnityEngine.Object.FindAnyObjectByType<ShootingPathInspector>();
        if (tool.dribblingReference != null || tool.movingBall != null)
            throw new InvalidOperationException("Comparison objects already exist; edit them in the scene.");
        var sourceBall = tool.referenceBall;
        var reference = new GameObject("Dribbling Scale Reference").transform;
        reference.SetParent(tool.transform, false);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/RedKitPrefab.prefab");
        var token = (GameObject)PrefabUtility.InstantiatePrefab(prefab, reference);
        PrefabUtility.UnpackPrefabInstance(token, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        token.name = "Reference Player Token (visual only)";
        // Strip gameplay components from this scene instance, never from the prefab asset.
        foreach (var component in token.GetComponentsInChildren<MonoBehaviour>(true)) UnityEngine.Object.DestroyImmediate(component);
        foreach (var component in token.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(component);
        token.transform.localPosition = new Vector3(0, 0.2f, 0);
        CopyBallVisual("Dribbling Ball (actual scale)", reference, sourceBall);
        tool.dribblingReference = reference;
        tool.movingBall = CopyBallVisual("Mouse Shot Preview Ball (actual scale)", tool.transform, sourceBall);
        tool.visibleBallDiameter = sourceBall.GetComponent<MeshFilter>().sharedMesh.bounds.size.x * Mathf.Abs(sourceBall.transform.lossyScale.x);
        sourceBall.GetComponent<Renderer>().enabled = false;
        // Seven readable lines, all authored and editable in the diagnostic scene.
        var background = tool.status.transform.parent.GetComponent<RectTransform>();
        background.sizeDelta = new Vector2(900, 190);
        tool.Initialize();
        EditorSceneManager.MarkSceneDirty(tool.gameObject.scene);
    }

    public static void ConfigurePhysicalComparison()
    {
        if (Application.isPlaying || SceneManager.GetActiveScene().path != ScenePath)
            throw new InvalidOperationException("Author physical comparison only in the diagnostic scene outside Play Mode.");
        var tool = UnityEngine.Object.FindAnyObjectByType<ShootingPathInspector>();
        if (tool.physicalMarkers != null && tool.physicalMarkers.Length > 0)
            throw new InvalidOperationException("Physical comparison already exists; edit its scene objects.");
        tool.storedPathMaterial = AssetDatabase.LoadAssetAtPath<Material>(AssetsPath + "StoredPath.mat");
        tool.storedOnlyMaterial = Material("Stored Only", new Color(1f, 0.05f, 0.8f));
        tool.physicalPathMaterial = Material("Physical Disc", new Color(0.05f, 0.9f, 1f));
        var root = new GameObject("Physical Disc Path Markers").transform;
        root.SetParent(tool.transform, false);
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(AssetsPath + "InspectionHex.asset");
        tool.physicalMarkers = Enumerable.Range(0, 128).Select(i => Marker($"Physical {i}", mesh, tool.physicalPathMaterial, root)).ToArray();
        tool.capsuleOutline = Line("Physical Ball Capsule", tool.corridorOutline.sharedMaterial, 0.022f, true, tool.transform);
        tool.status.transform.parent.GetComponent<RectTransform>().sizeDelta = new Vector2(1100, 220);
        tool.Initialize();
        EditorSceneManager.MarkSceneDirty(tool.gameObject.scene);
    }

    private static Transform CopyBallVisual(string name, Transform parent, Ball source)
    {
        var visual = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        visual.transform.SetParent(parent, false);
        visual.transform.localScale = source.transform.lossyScale;
        visual.transform.localRotation = source.transform.rotation;
        visual.transform.localPosition = Vector3.up * source.playerHeightOffset;
        visual.GetComponent<MeshFilter>().sharedMesh = source.GetComponent<MeshFilter>().sharedMesh;
        visual.GetComponent<MeshRenderer>().sharedMaterials = source.GetComponent<MeshRenderer>().sharedMaterials;
        return visual.transform;
    }

    private static Material Material(string name, Color color, bool transparent = false)
    {
        var material = new Material(Shader.Find("Sprites/Default")) { name = name, color = color };
        material.SetFloat("_Cull", 0);
        if (transparent)
        {
            material.SetFloat("_Surface", 1);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = 3000;
        }
        AssetDatabase.CreateAsset(material, AssetsPath + name.Replace(" ", "") + ".mat");
        return material;
    }

    private static Mesh MakeHex()
    {
        var mesh = new Mesh { name = "Inspection hex" };
        var vertices = new Vector3[7];
        for (int i = 0; i < 6; i++) vertices[i + 1] = new Vector3(Mathf.Cos(i * Mathf.PI / 3) * 0.47f, 0, Mathf.Sin(i * Mathf.PI / 3) * 0.47f);
        mesh.vertices = vertices;
        mesh.triangles = Enumerable.Range(0, 6).SelectMany(i => new[] { 0, (i + 1) % 6 + 1, i + 1 }).ToArray();
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        return mesh;
    }

    private static Transform Marker(string name, Mesh mesh, Material material, Transform parent)
    {
        var marker = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        marker.transform.SetParent(parent, false);
        marker.GetComponent<MeshFilter>().sharedMesh = mesh;
        marker.GetComponent<MeshRenderer>().sharedMaterial = material;
        return marker.transform;
    }

    private static LineRenderer Line(string name, Material material, float width, bool loop, Transform parent)
    {
        var line = new GameObject(name, typeof(LineRenderer)).GetComponent<LineRenderer>();
        line.transform.SetParent(parent, false); line.sharedMaterial = material;
        line.useWorldSpace = true; line.loop = loop; line.widthMultiplier = width;
        line.positionCount = 0;
        return line;
    }

    private static void PersistTransientMeshes(Scene scene)
    {
        int index = 0;
        foreach (var filter in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MeshFilter>(true)))
        {
            Mesh mesh = filter.sharedMesh;
            if (mesh == null || EditorUtility.IsPersistent(mesh) || mesh.hideFlags != HideFlags.None) continue;
            AssetDatabase.CreateAsset(mesh, AssetsPath + $"AuthoredMesh{index++}.asset");
        }
    }
}
