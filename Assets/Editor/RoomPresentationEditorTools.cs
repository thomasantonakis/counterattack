using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>Explicit edit-mode authoring. Runtime controllers only use serialized objects.</summary>
public static class RoomPresentationEditorTools
{
    private const string RoomPath = "Assets/Scenes/Room.unity";
    private const string AssetFolder = "Assets/Resources/UI/RoomPresentation";

    [MenuItem("CounterAttack/Room/Author Presentation And Camera Controls")]
    public static void Author()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before authoring Room presentation.");
        var scene = SceneManager.GetActiveScene();
        if (scene.path != RoomPath) scene = EditorSceneManager.OpenScene(RoomPath, OpenSceneMode.Single);
        var stats = Object.FindAnyObjectByType<MatchStatsUI>(FindObjectsInactive.Include);
        var canvas = stats != null ? stats.GetComponentInParent<Canvas>().rootCanvas : null;
        var grid = Object.FindAnyObjectByType<HexGrid>();
        var camera = Object.FindAnyObjectByType<CameraController>();
        if (stats == null || canvas == null || grid == null || camera == null) throw new InvalidOperationException("Room dependencies are missing.");
        EnsureFolder(AssetFolder);
        var presentation = EnsureRoot("RoomPresentation");
        AuthorSlots(canvas);
        AuthorCard(presentation, canvas, stats, camera);
        AuthorTips(presentation, canvas);
        AuthorOverlay(presentation, grid);
        AuthorCamera(presentation, camera, stats, grid);
        AuthorIntro(presentation, canvas, camera);
        ExportTestViews(canvas, camera);
        ExportVisualStage(canvas, camera, presentation);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    private static void AuthorSlots(Canvas canvas)
    {
        foreach (var slot in canvas.GetComponentsInChildren<MovementPhaseMovedTokenSlotView>(true))
        {
            if (slot.GetComponentInParent<MovementPhaseMovedTokensPanel>() == null)
            {
                // This view is also used by the status panel; only movement rows get prompts.
                foreach (string ownedName in new[] { "ActiveDisc", "Forfeit", "FacePresentation" })
                {
                    var owned = slot.transform.Find(ownedName);
                    if (owned != null && owned.childCount == 0) Object.DestroyImmediate(owned.gameObject);
                }
                continue;
            }
            var face = slot.GetComponentInChildren<RawImage>(true);
            var text = slot.GetComponentInChildren<TMP_Text>(true);
            var faceRoot = slot.transform.Find("FacePresentation");
            if (faceRoot != null)
            {
                if (face.transform.parent == faceRoot) face.transform.SetParent(slot.transform, false);
                if (text.transform.parent == faceRoot) text.transform.SetParent(slot.transform, false);
                if (faceRoot.childCount == 0) Object.DestroyImmediate(faceRoot.gameObject);
            }
            var disc = Ensure<Image>(Rect("ActiveDisc", slot.transform).gameObject);
            disc.rectTransform.anchorMin = disc.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            disc.rectTransform.sizeDelta = new Vector2(34f, 34f);
            disc.rectTransform.anchoredPosition = Vector2.zero;
            disc.sprite = DiscSprite();
            disc.raycastTarget = false; disc.enabled = false;
            var forfeited = Label("Forfeit", slot.transform, "", 54f);
            Stretch(forfeited.rectTransform); forfeited.alignment = TextAlignmentOptions.Center; forfeited.fontStyle = FontStyles.Bold;
            var border = Label("ForfeitBorder", slot.transform, "", 60f);
            Stretch(border.rectTransform); border.alignment = TextAlignmentOptions.Center; border.fontStyle = FontStyles.Bold;
            border.transform.SetAsFirstSibling();
            Assign(slot, ("activeDisc", disc), ("forfeitGraphic", forfeited), ("forfeitOutline", border));
        }
    }

    private static void AuthorCard(GameObject presentation, Canvas canvas, MatchStatsUI stats, CameraController camera)
    {
        var area = Rect("InspectionCard", canvas.transform);
        area.anchorMin = new Vector2(0.5f, 0.2f); area.anchorMax = new Vector2(0.5f, 0.8f);
        area.pivot = new Vector2(0.5f, 0.5f); area.sizeDelta = new Vector2(330f, 0f); area.anchoredPosition = Vector2.zero;
        var group = Ensure<CanvasGroup>(area.gameObject); group.blocksRaycasts = false; group.interactable = false;
        var cardCanvas = Ensure<Canvas>(area.gameObject);
        var hoverCanvas = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include).FirstOrDefault(c => c.name == "HoveredTokenNameCanvas");
        cardCanvas.overrideSorting = true;
        cardCanvas.sortingLayerID = hoverCanvas != null ? hoverCanvas.sortingLayerID : canvas.sortingLayerID;
        cardCanvas.sortingOrder = (hoverCanvas != null ? hoverCanvas.sortingOrder : canvas.sortingOrder) + 1;
        var serialized = new SerializedObject(stats);
        var playerSource = (PlayerCard)serialized.FindProperty("homeHoverCard").objectReferenceValue;
        var keeperSource = (GoalkeeperCard)serialized.FindProperty("homeGoalkeeperHoverCard").objectReferenceValue;
        if (playerSource == null || keeperSource == null) throw new InvalidOperationException("Authored stats cards are required as inspection sources.");
        var player = CopyCard("OutfieldCard", playerSource, area);
        var keeper = CopyCard("GoalkeeperCard", keeperSource, area);
        var controller = Ensure<RoomInspectionCard>(presentation);
        Assign(controller, ("outfieldCard", player), ("goalkeeperCard", keeper), ("cardArea", area), ("statsUI", stats), ("cameraController", camera));
        AuthorAttributeBadges(controller, "outfieldBadges", player);
        AuthorAttributeBadges(controller, "goalkeeperBadges", keeper);
    }

    private static void AuthorAttributeBadges(RoomInspectionCard controller, string field, Component card)
    {
        Canvas.ForceUpdateCanvases();
        var values = card.GetComponentsInChildren<TMP_Text>(true).Where(t => t.name.EndsWith("Value", StringComparison.Ordinal)).ToArray();
        var serialized = new SerializedObject(controller); var entries = serialized.FindProperty(field); entries.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
        {
            var value = values[i]; var valueRect = value.rectTransform;
            var rect = Rect(value.name + "Badge", value.transform.parent);
            Ensure<LayoutElement>(rect.gameObject).ignoreLayout = true;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.position = valueRect.TransformPoint(valueRect.rect.center);
            // The tight disc has no transparent padding: size it around the glyph,
            // leaving the same breathing room between rows as the small previews.
            float diameter = value.fontSize * 1.25f;
            rect.sizeDelta = new Vector2(diameter, diameter);
            rect.SetAsFirstSibling();
            var background = Ensure<Image>(rect.gameObject);
            background.sprite = DiscSprite();
            background.preserveAspect = true; background.raycastTarget = false;
            background.color = MatchStatsUI.GetInspectionAttributeColor(int.TryParse(value.text, out int number) ? number : 4);
            entries.GetArrayElementAtIndex(i).FindPropertyRelative("value").objectReferenceValue = value;
            entries.GetArrayElementAtIndex(i).FindPropertyRelative("background").objectReferenceValue = background;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static T CopyCard<T>(string name, T source, RectTransform parent) where T : Component
    {
        var existing = parent.Find(name);
        T card = existing != null ? existing.GetComponent<T>() : Object.Instantiate(source, parent);
        card.name = name;
        var rect = (RectTransform)card.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;
        foreach (Graphic graphic in card.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
        card.gameObject.SetActive(false);
        return card;
    }

    private static void AuthorTips(GameObject presentation, Canvas canvas)
    {
        Transform box = canvas.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(t => t.name == "InstructionsPanel");
        if (box == null) throw new InvalidOperationException("InstructionsPanel is missing.");
        var label = Label("RoomTip", box, "Tip: Press Space over a player to inspect their card.", 24f);
        var rect = label.rectTransform;
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.right; rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -8f); rect.sizeDelta = new Vector2(-12f, 48f);
        label.alignment = TextAlignmentOptions.TopLeft; label.color = Color.white;
        label.outlineColor = new Color(0f, 0f, 0f, 0.95f); label.outlineWidth = 0.25f;
        Assign(Ensure<RoomTips>(presentation), ("tipText", label));
    }

    private static void AuthorOverlay(GameObject presentation, HexGrid grid)
    {
        var root = Child(presentation.transform, "EligibilityIcons");
        var shooting = Coordinates("shootingpaths/shootingPaths");
        var heading = Coordinates("shootingpaths/headingPaths");
        var all = new HashSet<Vector3Int>(shooting); all.UnionWith(heading);
        Sprite boot = PngSprite("SoccerBoot");
        Sprite head = PngSprite("HeaderBust");
        var controller = Ensure<RoomHexEligibilityOverlay>(presentation);
        var serialized = new SerializedObject(controller);
        serialized.FindProperty("grid").objectReferenceValue = grid;
        var icons = serialized.FindProperty("icons"); icons.arraySize = all.Count;
        int index = 0;
        foreach (var coordinate in all.OrderBy(c => c.x).ThenBy(c => c.z))
        {
            var entry = Child(root, $"Hex_{coordinate.x}_{coordinate.z}");
            entry.position = grid.GetHexCenterForCoordinates(coordinate);
            SpriteRenderer shot = Icon(entry, "Shooting", boot, shooting.Contains(coordinate));
            SpriteRenderer aerial = Icon(entry, "Heading", head, heading.Contains(coordinate));
            var element = icons.GetArrayElementAtIndex(index++);
            element.FindPropertyRelative("coordinates").vector3IntValue = coordinate;
            element.FindPropertyRelative("shooting").objectReferenceValue = shot;
            element.FindPropertyRelative("heading").objectReferenceValue = aerial;
            element.FindPropertyRelative("shootingOutline").objectReferenceValue = shot != null ? shot.transform.Find("ContrastOutline").GetComponent<SpriteRenderer>() : null;
            element.FindPropertyRelative("headingOutline").objectReferenceValue = aerial != null ? aerial.transform.Find("ContrastOutline").GetComponent<SpriteRenderer>() : null;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static SpriteRenderer Icon(Transform parent, string name, Sprite sprite, bool eligible)
    {
        var root = Child(parent, name); root.gameObject.layer = 2;
        root.localPosition = Vector3.up * 0.12f; root.localScale = Vector3.one * 0.7f;
        var renderer = Ensure<SpriteRenderer>(root.gameObject);
        renderer.sprite = sprite; renderer.color = Color.white; renderer.enabled = false; renderer.sortingOrder = 20;
        var border = Child(root, "ContrastOutline"); border.gameObject.layer = 2;
        border.localPosition = new Vector3(0f, 0f, 0.015f); border.localScale = Vector3.one * 1.16f;
        var outline = Ensure<SpriteRenderer>(border.gameObject);
        outline.sprite = sprite; outline.color = Color.black; outline.enabled = false; outline.sortingOrder = 19;
        return eligible ? renderer : null;
    }

    private static HashSet<Vector3Int> Coordinates(string resource)
    {
        var json = JObject.Parse(Resources.Load<TextAsset>(resource).text);
        return new HashSet<Vector3Int>(json.Properties().Select(p =>
        {
            var parts = p.Name.Trim('(', ')').Split(',');
            return new Vector3Int(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]));
        }));
    }

    private static Sprite Glyph(string name, Vector2[] polygon)
    {
        string path = AssetFolder + "/" + name + ".asset";
        Sprite existing = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
        if (existing != null) return existing;
        const int size = 128;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = name, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
        {
            // Supersample the code-authored polygon to keep silhouette edges smooth.
            int hits = 0;
            for (int sy = 0; sy < 4; sy++) for (int sx = 0; sx < 4; sx++)
            {
                Vector2 point = new((x + (sx + .5f) / 4f) / size, (y + (sy + .5f) / 4f) / size);
                bool inside = false;
                for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
                    if ((polygon[i].y > point.y) != (polygon[j].y > point.y)
                        && point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x) inside = !inside;
                if (inside) hits++;
            }
            pixels[y * size + x] = new Color(1f, 1f, 1f, hits / 16f);
        }
        texture.SetPixels(pixels); texture.Apply();
        AssetDatabase.CreateAsset(texture, path);
        var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), size);
        sprite.name = name; AssetDatabase.AddObjectToAsset(sprite, texture);
        return sprite;
    }

    private static Sprite DiscSprite() => Glyph("Disc", Enumerable.Range(0, 96).Select(i =>
        new Vector2(.5f + .48f * Mathf.Cos(i * Mathf.PI / 48f), .5f + .48f * Mathf.Sin(i * Mathf.PI / 48f))).ToArray());

    private static Sprite PngSprite(string name)
    {
        string path = AssetFolder + "/" + name + ".png";
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 256; importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void AuthorCamera(GameObject presentation, CameraController camera, MatchStatsUI stats, HexGrid grid)
    {
        var controls = Child(presentation.transform, "CameraControls");
        var envelope = Ensure<RoomCameraEnvelope>(controls.gameObject);
        var markings = Object.FindAnyObjectByType<PitchLines>().transform.Find("GeneratedVisuals/PitchMarkings");
        Bounds pitch = BoundsOf(markings.GetComponentsInChildren<Renderer>());
        var blockers = Object.FindObjectsByType<OutOfBoundsClickBlocker>().Select(b => b.GetComponent<Renderer>()).ToArray();
        Bounds coverage = BoundsOf(blockers);
        // Close the cross-shaped gaps beyond the stands. This makes the authored
        // rectangle truly covered even for a full-pitch view beside a wide panel.
        foreach (Renderer blocker in blockers.Where(r => r.name.StartsWith("Top") || r.name.StartsWith("Bottom")))
        {
            Vector3 scale = blocker.transform.localScale;
            scale.x = coverage.size.x / blocker.transform.parent.lossyScale.x;
            blocker.transform.localScale = scale;
        }
        coverage = BoundsOf(blockers);
        var backdrop = Object.FindAnyObjectByType<PitchLines>().transform.Find("GeneratedVisuals/StadiumGroundBackdrop");
        if (backdrop == null)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Quad); ground.name = "StadiumGroundBackdrop";
            ground.transform.SetParent(Object.FindAnyObjectByType<PitchLines>().transform.Find("GeneratedVisuals"), false);
            backdrop = ground.transform; Object.DestroyImmediate(ground.GetComponent<Collider>());
        }
        backdrop.gameObject.layer = 2; backdrop.localPosition = new Vector3(0f, -.03f, 0f);
        backdrop.localRotation = Quaternion.Euler(90f, 0f, 0f); backdrop.localScale = new Vector3(4000f, 4000f, 1f);
        backdrop.GetComponent<Renderer>().sharedMaterial = blockers[0].sharedMaterial;
        coverage = backdrop.GetComponent<Renderer>().bounds;
        var stadium = Object.FindAnyObjectByType<StadiumCrowdController>();
        if (stadium == null) throw new InvalidOperationException("Stadium geometry is required for camera authoring.");
        Bounds bowl = BoundsOf(stadium.GetComponentsInChildren<Renderer>());
        Vector3 half = new(Mathf.Min(pitch.extents.x - .4f, bowl.extents.x * .65f), 44f, Mathf.Min(pitch.extents.z - .4f, bowl.extents.z * .65f));
        envelope.Configure(new Bounds(new Vector3(pitch.center.x, 50.5f, pitch.center.z), new Vector3(240f, 100f, 240f)), coverage, coverage.center.y);
        var shell = stadium.GetComponentsInChildren<MeshFilter>().Single(m => m.name == "Concourse and Bowl Walls");
        var vertices = shell.sharedMesh.vertices.Select(v => shell.transform.TransformPoint(v)).ToArray();
        float rimHeight = vertices.Max(v => v.y);
        envelope.ConfigureShell(ConvexHull(vertices.Where(v => v.y > rimHeight - .01f).Select(v => new Vector2(v.x, v.z))), rimHeight - .2f);
        Assign(camera, ("safeEnvelope", envelope), ("sidePanel", stats.panel));
        // Wider lens permits the full pitch while keeping the camera inside the bowl.
        camera.GetComponent<Camera>().fieldOfView = 50f;
        var positionHandle = Child(controls, "SafeCameraPositionEnvelope");
        positionHandle.position = envelope.PositionEnvelope.center; positionHandle.localScale = envelope.PositionEnvelope.size;
        var groundHandle = Child(controls, "SafeGroundCoverage");
        groundHandle.position = coverage.center; groundHandle.localScale = coverage.size;
    }

    private static void AuthorIntro(GameObject presentation, Canvas hud, CameraController camera)
    {
        var introCanvasObject = EnsureRoot("RoomIntroCanvas");
        introCanvasObject.layer = 5;
        var canvas = Ensure<Canvas>(introCanvasObject); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
        var scaler = Ensure<CanvasScaler>(introCanvasObject); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        var hudScaler = hud.GetComponent<CanvasScaler>();
        scaler.referenceResolution = hudScaler != null ? hudScaler.referenceResolution : new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = .5f; Ensure<GraphicRaycaster>(introCanvasObject);
        var content = Rect("IntroUI", canvas.transform); Stretch(content);
        var blocker = Ensure<Image>(content.gameObject); blocker.color = Color.clear; blocker.raycastTarget = true;
        var names = Label("TeamNames", content, "Home  v  Away", 76f);
        names.gameObject.SetActive(false);
        names.alignment = TextAlignmentOptions.Center; names.outlineColor = Color.black; names.outlineWidth = .2f;
        names.rectTransform.anchorMin = new Vector2(.08f,.78f); names.rectTransform.anchorMax = new Vector2(.92f,.93f);
        names.rectTransform.offsetMin = names.rectTransform.offsetMax = Vector2.zero;
        var scoreboardSource = Object.FindObjectsByType<ScoreboardManager>(FindObjectsInactive.Include)
            .FirstOrDefault(s => s.gameObject.scene == hud.gameObject.scene && !s.transform.IsChildOf(introCanvasObject.transform));
        if (scoreboardSource != null)
        {
            var existing = content.Find("IntroScoreboard");
            var scoreboard = existing != null ? existing.GetComponent<ScoreboardManager>() : Object.Instantiate(scoreboardSource, content);
            scoreboard.name = "IntroScoreboard";
            typeof(ScoreboardManager).GetMethod("EnsureLayout", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(scoreboard, null);
            if (scoreboard.timeText != null) scoreboard.timeText.transform.parent.gameObject.SetActive(false);
            foreach (Transform child in scoreboard.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 5;
            scoreboard.gameObject.SetActive(true);
        }
        var skipRect = Rect("SkipIntro", content);
        skipRect.anchorMin = skipRect.anchorMax = skipRect.pivot = Vector2.right;
        skipRect.anchoredPosition = new Vector2(-72f, 72f); skipRect.sizeDelta = new Vector2(360f, 96f);
        var background = Ensure<Image>(skipRect.gameObject); background.color = new Color(.025f,.035f,.05f,.9f);
        var button = Ensure<Button>(skipRect.gameObject); button.targetGraphic = background;
        var buttonText = Label("Label", skipRect, "Skip Intro", 44f); Stretch(buttonText.rectTransform); buttonText.alignment = TextAlignmentOptions.Center;
        var path = Child(presentation.transform, "IntroPathControls");
        var controls = new Transform[3];
        Vector3[] positions = { new(-12.5f, 1.8f, -8f), new(-11.5f, 2.2f, -9.5f), new(-4f, 32f, -10f) };
        Vector3[] targets = { new(4f, 0f, 2f), new(4f, 0f, 2f), Vector3.zero };
        for (int i = 0; i < controls.Length; i++)
        {
            controls[i] = Child(path, new[] { "Opening_0s", "Reveal_4s", "General_10s" }[i]);
            controls[i].SetPositionAndRotation(positions[i], Quaternion.LookRotation(targets[i] - positions[i], Vector3.up));
        }
        var intro = Ensure<RoomMatchIntro>(presentation);
        Assign(intro, ("cameraController", camera), ("introUI", content.gameObject), ("teamNames", names), ("skipButton", button));
        var serialized = new SerializedObject(intro);
        var normalHud = serialized.FindProperty("normalHud"); normalHud.arraySize = 1;
        normalHud.GetArrayElementAtIndex(0).objectReferenceValue = Ensure<CanvasGroup>(hud.gameObject);
        var paths = serialized.FindProperty("pathControls"); paths.arraySize = controls.Length;
        for (int i = 0; i < controls.Length; i++) paths.GetArrayElementAtIndex(i).objectReferenceValue = controls[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
        content.gameObject.SetActive(false);
    }

    private static Bounds BoundsOf(IEnumerable<Renderer> renderers)
    {
        var items = renderers.Where(r => r != null).ToArray();
        if (items.Length == 0) throw new InvalidOperationException("Required geometry has no renderers.");
        Bounds result = items[0].bounds;
        foreach (var renderer in items.Skip(1)) result.Encapsulate(renderer.bounds);
        return result;
    }

    public const string TestViewsPath = "Assets/Tests/Fixtures/RoomPresentationViews.prefab";
    public const string VisualStagePath = "Assets/Tests/Fixtures/RoomPresentationVisualStage.prefab";

    private static void ExportVisualStage(Canvas source, CameraController sourceCamera, GameObject presentation)
    {
        var preview = EditorSceneManager.NewPreviewScene();
        var root = new GameObject("Room presentation visual validation stage");
        root.SetActive(false); SceneManager.MoveGameObjectToScene(root, preview);
        try
        {
            var views = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(TestViewsPath), root.transform);
            views.SetActive(true);
            var hud = views.GetComponent<Canvas>();
            var camera = views.GetComponentInChildren<CameraController>(true);
            // A ScreenSpaceCamera Canvas must not parent the camera it follows.
            camera.transform.SetParent(root.transform, true);
            camera.tag = "MainCamera";
            var uiCameraObject = new GameObject("PresentationUICamera", typeof(Camera)); uiCameraObject.transform.SetParent(root.transform, false);
            uiCameraObject.transform.localPosition = new Vector3(0f, 0f, -10f);
            var uiCamera = uiCameraObject.GetComponent<Camera>(); uiCamera.enabled = false;
            uiCamera.orthographic = true; uiCamera.orthographicSize = 5f; uiCamera.clearFlags = CameraClearFlags.Depth; uiCamera.cullingMask = 1 << 5;
            camera.GetComponent<Camera>().cullingMask &= ~(1 << 5);
            hud.renderMode = RenderMode.ScreenSpaceCamera; hud.worldCamera = uiCamera; hud.planeDistance = 1f;
            var sourceStats = source.GetComponentInChildren<MatchStatsUI>(true);
            var panel = Object.Instantiate(sourceStats.panel, views.transform); panel.name = "AuthoredSidePanel";
            foreach (MonoBehaviour behaviour in panel.GetComponentsInChildren<MonoBehaviour>(true))
                if (behaviour.GetType().Assembly == typeof(MatchStatsUI).Assembly) Object.DestroyImmediate(behaviour);
            Assign(camera, ("sidePanel", panel));
            var directors = Object.Instantiate(presentation, root.transform);
            Object.DestroyImmediate(directors.GetComponent<RoomInspectionCard>());
            var instructions = source.GetComponentsInChildren<RectTransform>(true).Single(t => t.name == "InstructionsPanel");
            var instructionCopy = Object.Instantiate(instructions, views.transform);
            Assign(directors.GetComponent<RoomTips>(), ("tipText", instructionCopy.GetComponentsInChildren<TMP_Text>(true).Single(t => t.name == "RoomTip")));
            var introCanvas = Object.Instantiate(GameObject.Find("RoomIntroCanvas"), root.transform).GetComponent<Canvas>();
            introCanvas.renderMode = RenderMode.ScreenSpaceCamera; introCanvas.worldCamera = uiCamera; introCanvas.planeDistance = .8f;
            var introUI = introCanvas.transform.Find("IntroUI").gameObject;
            var intro = directors.GetComponent<RoomMatchIntro>();
            Assign(intro, ("cameraController", camera), ("introUI", introUI), ("teamNames", introUI.GetComponentsInChildren<TMP_Text>(true).Single(t => t.name == "TeamNames")),
                ("skipButton", introUI.GetComponentInChildren<Button>(true)));
            var serialized = new SerializedObject(intro);
            var groups = serialized.FindProperty("normalHud"); groups.arraySize = 1;
            groups.GetArrayElementAtIndex(0).objectReferenceValue = Ensure<CanvasGroup>(views);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var pitch = Object.Instantiate(Object.FindAnyObjectByType<PitchLines>().gameObject, root.transform);
            PersistFixtureMeshes(pitch);
            Object.Instantiate(Object.FindAnyObjectByType<StadiumCrowdController>().gameObject, root.transform);
            foreach (Light light in Object.FindObjectsByType<Light>())
                if (light.gameObject.scene == source.gameObject.scene) Object.Instantiate(light.gameObject, root.transform);
            var manager = Object.FindAnyObjectByType<PlayerTokenManager>();
            var token = Object.Instantiate(manager.redKitPrefab, root.transform); token.name = "AuthoredTokenVisualTemplate"; token.SetActive(false);
            foreach (MonoBehaviour behaviour in token.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(behaviour);
            var decal = GameObject.CreatePrimitive(PrimitiveType.Quad); decal.name = "TopDecal"; decal.transform.SetParent(token.transform, false);
            decal.transform.localPosition = new Vector3(0f, 1.01f, 0f); decal.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); decal.transform.localScale = Vector3.one * .9f;
            Object.DestroyImmediate(decal.GetComponent<Collider>());
            Ensure<PlayerTokenVisuals>(token);
            var number = Object.Instantiate(manager.textPrefab, token.transform);
            number.name = "JerseyNumber"; number.transform.localPosition = new Vector3(0f, 1.06f, 0f); number.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var sourceBall = Object.FindAnyObjectByType<Ball>();
            var ball = Object.Instantiate(sourceBall.gameObject, root.transform); ball.name = "AuthoredBallVisualTemplate"; ball.SetActive(false);
            foreach (MonoBehaviour behaviour in ball.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(behaviour);
            PrefabUtility.SaveAsPrefabAsset(root, VisualStagePath);
        }
        finally { Object.DestroyImmediate(root); EditorSceneManager.ClosePreviewScene(preview); }
    }

    private static void PersistFixtureMeshes(GameObject root)
    {
        const string path = "Assets/Tests/Fixtures/RoomPresentationPitchMeshes.asset";
        var saved = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().ToDictionary(m => m.name);
        foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
        {
            Mesh source = filter.sharedMesh;
            if (source == null || EditorUtility.IsPersistent(source)) continue;
            string key = filter.name;
            if (!saved.TryGetValue(key, out Mesh mesh))
            {
                mesh = Object.Instantiate(source); mesh.name = key;
                if (saved.Count == 0) AssetDatabase.CreateAsset(mesh, path); else AssetDatabase.AddObjectToAsset(mesh, path);
                saved.Add(key, mesh);
            }
            else { EditorUtility.CopySerialized(source, mesh); mesh.name = key; EditorUtility.SetDirty(mesh); }
            filter.sharedMesh = mesh;
        }
    }
    private static void ExportTestViews(Canvas source, CameraController camera)
    {
        var preview = EditorSceneManager.NewPreviewScene();
        var root = new GameObject("Room presentation test views", typeof(RectTransform));
        root.SetActive(false); SceneManager.MoveGameObjectToScene(root, preview);
        try
        {
            root.layer = 5;
            var canvas = root.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            EditorUtility.CopySerialized(source.GetComponent<CanvasScaler>(), scaler);
            var area = Object.Instantiate((RectTransform)source.transform.Find("InspectionCard"), root.transform);
            var card = root.AddComponent<RoomInspectionCard>();
            var cameraObject = Object.Instantiate(camera.gameObject, root.transform);
            cameraObject.tag = "Untagged";
            var listener = cameraObject.GetComponent<AudioListener>(); if (listener != null) Object.DestroyImmediate(listener);
            var copyCamera = cameraObject.GetComponent<CameraController>();
            var sourceEnvelope = (RoomCameraEnvelope)new SerializedObject(camera).FindProperty("safeEnvelope").objectReferenceValue;
            var envelope = cameraObject.AddComponent<RoomCameraEnvelope>(); EditorUtility.CopySerialized(sourceEnvelope, envelope);
            Assign(copyCamera, ("safeEnvelope", envelope), ("sidePanel", null), ("gameInputManager", null));
            Assign(card, ("outfieldCard", area.GetComponentInChildren<PlayerCard>(true)), ("goalkeeperCard", area.GetComponentInChildren<GoalkeeperCard>(true)),
                ("cardArea", area), ("cameraController", copyCamera));
            AuthorAttributeBadges(card, "outfieldBadges", area.GetComponentInChildren<PlayerCard>(true));
            AuthorAttributeBadges(card, "goalkeeperBadges", area.GetComponentInChildren<GoalkeeperCard>(true));
            var panelSource = source.GetComponentInChildren<MovementPhaseMovedTokensPanel>(true);
            var panel = Object.Instantiate(panelSource, root.transform);
            Assign(panel, ("movementPhaseManager", null));
            PrefabUtility.SaveAsPrefabAsset(root, TestViewsPath);
        }
        finally { Object.DestroyImmediate(root); EditorSceneManager.ClosePreviewScene(preview); }
    }
    private static Vector2[] ConvexHull(IEnumerable<Vector2> points)
    {
        var sorted = points.Distinct().OrderBy(p => p.x).ThenBy(p => p.y).ToArray();
        var hull = new List<Vector2>();
        float Cross(Vector2 a, Vector2 b, Vector2 c) => (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
        foreach (Vector2 point in sorted)
        {
            while (hull.Count > 1 && Cross(hull[hull.Count - 2], hull[hull.Count - 1], point) <= 0f) hull.RemoveAt(hull.Count - 1);
            hull.Add(point);
        }
        int lower = hull.Count;
        foreach (Vector2 point in sorted.Reverse().Skip(1))
        {
            while (hull.Count > lower && Cross(hull[hull.Count - 2], hull[hull.Count - 1], point) <= 0f) hull.RemoveAt(hull.Count - 1);
            hull.Add(point);
        }
        if (hull.Count > 1) hull.RemoveAt(hull.Count - 1);
        return hull.ToArray();
    }
    private static void Assign(Object target, params (string field, Object value)[] fields)
    {
        var serialized = new SerializedObject(target);
        foreach (var field in fields) serialized.FindProperty(field.field).objectReferenceValue = field.value;
        serialized.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(target);
    }
    private static GameObject EnsureRoot(string name) => GameObject.Find(name) ?? new GameObject(name);
    private static T Ensure<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }
    private static Transform Child(Transform parent, string name)
    {
        var existing = parent.Find(name); if (existing != null) return existing;
        var child = new GameObject(name); child.transform.SetParent(parent, false); return child.transform;
    }
    private static RectTransform Rect(string name, Transform parent)
    {
        var existing = parent.Find(name);
        if (existing != null) { existing.gameObject.layer = parent.gameObject.layer; return (RectTransform)existing; }
        var child = new GameObject(name, typeof(RectTransform)); child.layer = parent.gameObject.layer;
        child.transform.SetParent(parent, false); return (RectTransform)child.transform;
    }
    private static void Stretch(RectTransform rect)
    { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
    private static TextMeshProUGUI Label(string name, Transform parent, string text, float size)
    {
        var label = Ensure<TextMeshProUGUI>(Rect(name, parent).gameObject);
        label.text = text; label.fontSize = size; label.color = Color.white; label.raycastTarget = false;
        return label;
    }
    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/'); EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
    }
}
