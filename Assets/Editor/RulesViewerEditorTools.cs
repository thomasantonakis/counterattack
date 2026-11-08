using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class RulesViewerEditorTools
{
    public const string PrefabPath = "Assets/Prefabs/RulesViewer.prefab";
    public static readonly string[] Pages = { "index", "getting_started", "movement_phase", "passing", "shooting", "heading", "goalkeeper", "loose_ball", "set_pieces", "quick_guide" };
    private static readonly Color Background = new Color32(9, 23, 48, 255);
    private static readonly Color Card = new Color32(20, 46, 68, 255);
    private static TMP_FontAsset font;

    public static List<RulesDocument.Block> LoadDocument()
    {
        var blocks = new List<RulesDocument.Block>();
        foreach (string page in Pages)
            blocks.AddRange(RulesDocument.Parse(page, File.ReadAllText("Assets/Docs/Wiki/English/" + page + ".md")));
        RulesDocument.ValidateLinks(blocks);
        return blocks;
    }

    [MenuItem("CounterAttack/Rules/Refresh Existing Article Text")]
    public static void RefreshArticleText()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Refresh Rules in Edit Mode.");
        var blocks = LoadDocument();
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var viewer = root.GetComponent<RulesViewer>();
            if (viewer.entries.Length != blocks.Count)
                throw new InvalidOperationException("Rulebook structure changed; use the full builder after saving scenes.");
            // Validate the entire document before changing any serialized text.
            for (int i = 0; i < blocks.Count; i++)
                if (viewer.entries[i].text == null || (viewer.entries[i].anchor ?? "") != (blocks[i].anchor ?? "")
                    || viewer.entries[i].section != blocks[i].section || viewer.entries[i].heading != (blocks[i].level > 0))
                    throw new InvalidOperationException("Rulebook anchors changed; use the full builder after saving scenes.");
            for (int i = 0; i < blocks.Count; i++) viewer.entries[i].text.text = blocks[i].text;
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    [MenuItem("CounterAttack/Rules/Rebuild Viewer and Scene Entries")]
    public static void Build()
        => BuildViewer(true);

    [MenuItem("CounterAttack/Rules/Rebuild Viewer Prefab Only")]
    public static void BuildPrefab()
        => BuildViewer(false);

    private static void BuildViewer(bool installSceneEntries)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Author Rules in Edit Mode.");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        if (installSceneEntries && Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
            throw new InvalidOperationException("Save open scenes before rebuilding Rules.");
        List<RulesDocument.Block> blocks = LoadDocument();
        var original = SceneManager.GetActiveScene();
        var staging = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        try
        {
            SceneManager.SetActiveScene(staging);
            font = TMP_Settings.defaultFontAsset;
            var room = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Room.unity");
            try
            {
                var pause = room.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<PauseMenuManager>(true)).First();
                font = pause.resumeButton.GetComponentInChildren<TMP_Text>(true).font ?? font;
            }
            finally { EditorSceneManager.ClosePreviewScene(room); }
            // A rulebook contains many more glyphs than the HUD. Give it its own atlas
            // so authoring/searching never changes the designer's shared panel font.
            const string rulesFontPath = "Assets/Docs/Wiki/RulesFont.asset";
            var rulesFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(rulesFontPath);
            if (rulesFont == null)
            {
                rulesFont = TMP_FontAsset.CreateFontAsset(font.sourceFontFile, 60, 6,
                    UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
                AssetDatabase.CreateAsset(rulesFont, rulesFontPath);
                AssetDatabase.AddObjectToAsset(rulesFont.material, rulesFont);
                foreach (var atlas in rulesFont.atlasTextures) AssetDatabase.AddObjectToAsset(atlas, rulesFont);
            }
            font = rulesFont;
            var root = Node("RulesViewer", null);
            var canvas = root.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 300;
            var scaler = root.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900); scaler.matchWidthOrHeight = .5f;
            root.AddComponent<GraphicRaycaster>();
            var viewer = root.AddComponent<RulesViewer>();
            var surface = Panel("RulesSurface", root.transform, Background); Stretch(surface, 0, 0, 0, 0);
            var consumer = surface.AddComponent<GameplayInputConsumer>(); consumer.BlocksGameplayWhileActive = true;
            viewer.surface = surface;
            var header = Node("Header", surface.transform);
            var headerRect = (RectTransform)header.transform; headerRect.anchorMin = new Vector2(0,1); headerRect.anchorMax = Vector2.one;
            headerRect.offsetMin = new Vector2(20,-68); headerRect.offsetMax = new Vector2(-20,-14);
            var row = header.AddComponent<HorizontalLayoutGroup>(); row.spacing = 10; row.childControlWidth = true; row.childControlHeight = true; row.childForceExpandWidth = false;
            Label("Title", header.transform, "RULES", 28, 120);
            viewer.backButton = Button("Back", header.transform, "Back", 90);
            viewer.forwardButton = Button("Forward", header.transform, "Forward", 105);
            viewer.search = Input(header.transform);
            viewer.previousButton = Button("Previous", header.transform, "Prev", 65);
            viewer.nextButton = Button("Next", header.transform, "Next", 65);
            viewer.clearButton = Button("Clear", header.transform, "Clear", 70);
            viewer.closeButton = Button("Close", header.transform, "Close  ×", 105);
            var status = Label("MatchCount", surface.transform, "Search the rules", 17); Place(status.gameObject, 340, 76, 700, 26); viewer.matchCount = status;
            var sidebar = Panel("Sidebar", surface.transform, new Color32(12, 31, 51, 255)); Stretch(sidebar, 20, 110, 1280, 24);
            var sidebarRect = (RectTransform)sidebar.transform; sidebarRect.anchorMax = new Vector2(0,1); sidebarRect.offsetMax = new Vector2(320,-110);
            var sidebarLayout = sidebar.AddComponent<VerticalLayoutGroup>(); sidebarLayout.padding = new RectOffset(10,10,10,10); sidebarLayout.spacing = 10;
            sidebarLayout.childControlWidth = sidebarLayout.childControlHeight = true; sidebarLayout.childForceExpandHeight = false;
            Label("ContentsTitle", sidebar.transform, "CONTENTS", 19).gameObject.AddComponent<LayoutElement>().preferredHeight = 28;
            ScrollRect contents = Scroll("Contents", sidebar.transform); contents.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
            viewer.resultsScroll = Scroll("SearchResults", sidebar.transform); viewer.resultsScroll.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
            viewer.article = Scroll("Article", surface.transform); Stretch(viewer.article.gameObject, 340, 110, 20, 24);
            viewer.article.content.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(24, 30, 16, 32);
            var entries = new List<RulesViewer.Entry>();
            foreach (RulesDocument.Block block in blocks)
            {
                var entry = new RulesViewer.Entry { anchor = block.anchor, section = block.section, heading = block.level > 0 };
                if (!string.IsNullOrEmpty(block.image))
                {
                    string path = "Assets/Docs/Wiki/Images/" + block.image;
                    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    if (texture == null) throw new InvalidOperationException("Missing wiki diagram " + path);
                    var imageObject = Node("Diagram " + block.image, viewer.article.content);
                    var image = imageObject.AddComponent<RawImage>(); image.texture = texture; image.raycastTarget = false;
                    var aspect = imageObject.AddComponent<AspectRatioFitter>(); aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent; aspect.aspectRatio = (float)texture.width / texture.height;
                    // Put aspect fitting inside a reserved layout slot.
                    var slot = Node("DiagramFrame", viewer.article.content); slot.transform.SetSiblingIndex(imageObject.transform.GetSiblingIndex());
                    slot.AddComponent<LayoutElement>().preferredHeight = Mathf.Min(420f, 1060f * texture.height / texture.width);
                    imageObject.transform.SetParent(slot.transform, false);
                }
                entry.text = Label(block.anchor ?? "Paragraph", viewer.article.content, block.text, block.level > 0 ? (block.anchor.Contains("#") ? 25 : 34) : 21);
                entry.text.richText = true; entry.text.raycastTarget = true; entry.text.textWrappingMode = TextWrappingModes.Normal;
                entry.rect = entry.text.rectTransform;
                var spacing = entry.text.gameObject.AddComponent<LayoutElement>(); spacing.minHeight = block.level > 0 ? 50 : 30;
                if (block.level > 0)
                {
                    entry.text.fontStyle = FontStyles.Bold;
                    entry.text.color = new Color32(170, 222, 242, 255);
                    entry.contentsButton = Button("Go " + block.anchor, contents.content, (block.anchor.Contains("#") ? "   " : "") + block.plain);
                    var label = entry.contentsButton.GetComponentInChildren<TMP_Text>(); label.fontSize = block.anchor.Contains("#") ? 16 : 19;
                    label.alignment = TextAlignmentOptions.MidlineLeft;
                    entry.contentsButton.GetComponent<LayoutElement>().preferredHeight = 48;
                }
                entry.resultButton = Button("Result " + entries.Count, viewer.resultsScroll.content, "");
                var resultLabel = entry.resultButton.GetComponentInChildren<TMP_Text>(); resultLabel.fontSize = 16; resultLabel.richText = false; resultLabel.alignment = TextAlignmentOptions.MidlineLeft;
                entry.resultButton.GetComponent<LayoutElement>().preferredHeight = 110;
                entry.resultButton.gameObject.SetActive(false);
                entries.Add(entry);
            }
            viewer.entries = entries.ToArray();
            viewer.resultsScroll.gameObject.SetActive(false);
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(viewer.article.content);
            surface.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            EditorSceneManager.CloseScene(staging, true);
            SceneManager.SetActiveScene(original);
            if (installSceneEntries)
            {
                Install("Assets/Scenes/MainMenu.unity");
                Install("Assets/Scenes/Room.unity");
            }
            AssetDatabase.SaveAssets();
        }
        finally
        {
            if (staging.IsValid() && staging.isLoaded) EditorSceneManager.CloseScene(staging, true);
            if (installSceneEntries) EditorSceneManager.RestoreSceneManagerSetup(setup);
        }
        Debug.Log("Rules viewer authored: " + blocks.Count + " searchable blocks. All internal links validated.");
    }

    private static void Install(string scenePath)
    {
        var scene = SceneManager.GetSceneByPath(scenePath);
        bool alreadyLoaded = scene.IsValid() && scene.isLoaded;
        if (!alreadyLoaded) scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
        try
        {
            SceneManager.SetActiveScene(scene);
            var existing = scene.GetRootGameObjects().FirstOrDefault(r => r.name == "RulesViewer");
            if (existing == null) PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), scene);
            else PrefabUtility.RevertPrefabInstance(existing, InteractionMode.AutomatedAction);
            var pause = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<PauseMenuManager>(true)).FirstOrDefault();
            if (pause != null)
            {
                var parent = pause.resumeButton.transform.parent;
                var button = parent.Find("RulesButton")?.GetComponent<Button>();
                if (button == null)
                {
                    button = Object.Instantiate(pause.resumeButton, parent); button.name = "RulesButton";
                }
                ConfigureOpen(button);
                var back = (RectTransform)pause.backToMainMenuButton.transform;
                var ruleRect = (RectTransform)button.transform;
                ruleRect.anchorMin = back.anchorMin; ruleRect.anchorMax = back.anchorMax; ruleRect.pivot = back.pivot;
                ruleRect.sizeDelta = back.sizeDelta; ruleRect.localScale = back.localScale; ruleRect.localRotation = back.localRotation;
                float gap = pause.addTacticalNoteButton != null
                    ? Mathf.Abs(((RectTransform)pause.resumeButton.transform).anchoredPosition.y - ((RectTransform)pause.addTacticalNoteButton.transform).anchoredPosition.y)
                    : back.rect.height + 8f;
                ruleRect.anchoredPosition = new Vector2(back.anchoredPosition.x, ((RectTransform)pause.editSettings.transform).anchoredPosition.y - gap);
                back.anchoredPosition = ruleRect.anchoredPosition + Vector2.down * gap;
                button.transform.SetSiblingIndex(back.GetSiblingIndex() - (button.transform.GetSiblingIndex() < back.GetSiblingIndex() ? 1 : 0));
                var canvas = pause.pausePanel.GetComponentInParent<Canvas>().transform;
                var direct = canvas.Find("RoomRulesButton")?.GetComponent<Button>();
                if (direct == null) direct = Button("RoomRulesButton", canvas, "Rules");
                var monitor = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<GameDebugMonitor>(true)).First();
                var instructions = (RectTransform)monitor.instructionText.transform.parent;
                var rect = (RectTransform)direct.transform;
                rect.SetParent(instructions.parent, false);
                rect.anchorMin = instructions.anchorMin; rect.anchorMax = instructions.anchorMin; rect.pivot = Vector2.zero;
                rect.sizeDelta = new Vector2(200,64);
                rect.anchoredPosition = instructions.anchoredPosition + new Vector2(-instructions.pivot.x * instructions.rect.width, (1f - instructions.pivot.y) * instructions.rect.height + 12f);
                direct.navigation = new Navigation { mode = Navigation.Mode.None };
                ConfigureOpen(direct);
                direct.GetComponentInChildren<TMP_Text>(true).fontSize = 32;
            }
            EditorSceneManager.SaveScene(scene);
        }
        finally { if (!alreadyLoaded) EditorSceneManager.CloseScene(scene, true); }
    }

    private static void ConfigureOpen(Button button)
    {
        button.onClick = new Button.ButtonClickedEvent();
        // Instance callback works with persistent UnityEvent serialization.
        var opener = button.GetComponent<RulesViewerOpener>() ?? button.gameObject.AddComponent<RulesViewerOpener>();
        UnityEventTools.AddPersistentListener(button.onClick, opener.OpenRules);
        var label = button.GetComponentInChildren<TMP_Text>(true); if (label != null) label.text = "Rules";
    }

    private static GameObject Node(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform)); go.layer = LayerMask.NameToLayer("UI");
        if (parent != null) go.transform.SetParent(parent, false);
        return go;
    }

    private static GameObject Panel(string name, Transform parent, Color color)
    {
        var go = Node(name, parent); go.AddComponent<Image>().color = color; return go;
    }

    private static void Stretch(GameObject go, float left, float top, float right, float bottom)
    {
        var rect = (RectTransform)go.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom); rect.offsetMax = new Vector2(-right, -top);
    }

    private static void Place(GameObject go, float left, float top, float width, float height)
    {
        var rect = (RectTransform)go.transform; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0,1);
        rect.anchoredPosition = new Vector2(left, -top); rect.sizeDelta = new Vector2(width, height);
    }

    private static TMP_Text Label(string name, Transform parent, string value, float size, float width = -1)
    {
        var go = Node(name, parent); var label = go.AddComponent<TextMeshProUGUI>(); label.font = font;
        label.text = value; label.fontSize = size; label.color = new Color32(223, 232, 243, 255); label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.Normal;
        if (width > 0) go.AddComponent<LayoutElement>().preferredWidth = width;
        return label;
    }

    private static Button Button(string name, Transform parent, string title, float width = -1)
    {
        var go = Panel(name, parent, Card); var button = go.AddComponent<Button>();
        var layout = go.AddComponent<LayoutElement>(); layout.preferredHeight = 44; if (width > 0) layout.minWidth = layout.preferredWidth = width;
        TMP_Text label = Label("Label", go.transform, title, 19); label.alignment = TextAlignmentOptions.Center;
        if (width > 0) { label.enableAutoSizing = true; label.fontSizeMin = 13; label.fontSizeMax = 19; label.textWrappingMode = TextWrappingModes.NoWrap; }
        Stretch(label.gameObject, 10, 5, 10, 5);
        return button;
    }

    private static TMP_InputField Input(Transform parent)
    {
        var go = Panel("Search", parent, Card); var layout = go.AddComponent<LayoutElement>(); layout.flexibleWidth = 1; layout.minWidth = 150;
        var input = go.AddComponent<TMP_InputField>();
        var viewport = Node("Viewport", go.transform); Stretch(viewport, 12, 5, 12, 5); viewport.AddComponent<RectMask2D>();
        TMP_Text text = Label("Text", viewport.transform, "", 21); Stretch(text.gameObject, 0,0,0,0);
        TMP_Text placeholder = Label("Placeholder", viewport.transform, "Search all rules…", 21); Stretch(placeholder.gameObject, 0,0,0,0); placeholder.color = new Color32(150,170,190,255);
        input.textViewport = (RectTransform)viewport.transform; input.textComponent = (TextMeshProUGUI)text; input.placeholder = placeholder;
        input.lineType = TMP_InputField.LineType.SingleLine;
        return input;
    }

    private static ScrollRect Scroll(string name, Transform parent)
    {
        var go = Panel(name, parent, new Color32(12, 31, 51, 255));
        var scroll = go.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 45;
        var viewport = Panel("Viewport", go.transform, Color.white); Stretch(viewport, 0,0,12,0); viewport.AddComponent<Mask>().showMaskGraphic = false;
        var content = Node("Content", viewport.transform); var rect = (RectTransform)content.transform;
        rect.anchorMin = new Vector2(0,1); rect.anchorMax = Vector2.one; rect.pivot = new Vector2(.5f,1); rect.sizeDelta = Vector2.zero;
        var layout = content.AddComponent<VerticalLayoutGroup>(); layout.spacing = 12; layout.padding = new RectOffset(4,4,4,4);
        layout.childControlWidth = layout.childControlHeight = true; layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var bar = Panel("Scrollbar", go.transform, Background); var barRect = (RectTransform)bar.transform;
        barRect.anchorMin = new Vector2(1,0); barRect.anchorMax = Vector2.one; barRect.pivot = Vector2.one; barRect.sizeDelta = new Vector2(8,0);
        var scrollbar = bar.AddComponent<Scrollbar>(); scrollbar.direction = Scrollbar.Direction.BottomToTop;
        var handle = Panel("Handle", bar.transform, new Color32(69,126,151,255)); Stretch(handle,0,0,0,0); scrollbar.handleRect = (RectTransform)handle.transform; scrollbar.targetGraphic = handle.GetComponent<Image>();
        scroll.content = rect; scroll.viewport = (RectTransform)viewport.transform; scroll.verticalScrollbar = scrollbar;
        return scroll;
    }
}
