#if UNITY_EDITOR
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class EndGamePanelPrefabEditorTools
{
    private const string PrefabPath = "Assets/Resources/UI/EndGamePanel.prefab";
    private const string SceneInstanceName = "EndGamePanel";
    private const float PanelWidth = 860f;
    private const float PanelHeight = 720f;
    private const float RecapHorizontalPadding = 34f;
    private const int StatsRowCount = 21;
    private static readonly string[] StatRowTitles =
    {
        "ATTACKING",
        "Total Shots / xG",
        "On Target / Corners",
        "Blocked / Off Target",
        "PASSING",
        "Assists",
        "Ground (Att./Made)",
        "Aerial(Att./Trg./Made)",
        "Game Play",
        "Distance Covered",
        "Possession",
        "DUELS",
        "Ground Att. / Won",
        "Air Att. / Won",
        "DISCIPLINE",
        "Yellow / Red Cards",
        "Injuries / Subs Used",
        "OPTA STATS",
        "xRecoveries / Made",
        "xDribbles / Made",
        "xTackles / Made",
    };

    [InitializeOnLoadMethod]
    private static void EnsurePrefabExistsOnEditorLoad()
    {
        if (!File.Exists(PrefabPath) || PrefabNeedsRebuild())
        {
            CreateEndGamePanelPrefab();
        }

        EnsureSceneInstanceInEditMode();
    }

    [MenuItem("Tools/Counter Attack/Rebuild End Game Panel Prefab")]
    public static void CreateEndGamePanelPrefab()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));

        GameObject root = CreateRect("EndGamePanel", null, typeof(Image), typeof(EndGamePanelManager));
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        Image overlayImage = root.GetComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0.55f);
        overlayImage.raycastTarget = true;

        GameObject panel = CreateRect("RecapPanel", root.transform, typeof(Image));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0.05f, 0.08f, 0.14f, 0.96f);
        panelImage.raycastTarget = true;

        TMP_Text titleText = CreateText("Title", panel.transform, "Full Time", 34f, TextAlignmentOptions.Center, new Color(0.96f, 0.91f, 0.72f, 1f));
        RectTransform titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -24f);
        titleRect.sizeDelta = new Vector2(-64f, 54f);

        GameObject scrollRoot = CreateRect("RecapScroll", panel.transform, typeof(ScrollRect));
        RectTransform scrollRectTransform = scrollRoot.GetComponent<RectTransform>();
        scrollRectTransform.anchorMin = Vector2.zero;
        scrollRectTransform.anchorMax = Vector2.one;
        scrollRectTransform.offsetMin = new Vector2(RecapHorizontalPadding, 96f);
        scrollRectTransform.offsetMax = new Vector2(-RecapHorizontalPadding, -90f);
        ScrollRect scroll = scrollRoot.GetComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        GameObject viewport = CreateRect("Viewport", scrollRoot.transform, typeof(RectMask2D));
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;

        GameObject content = CreateRect("Content", viewport.transform, typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 720f);
        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        VerticalLayoutGroup contentLayout = content.GetComponent<VerticalLayoutGroup>();
        contentLayout.spacing = 18f;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        GameObject scoreRecap = CreateRect("ScoreRecap", content.transform, typeof(VerticalLayoutGroup), typeof(LayoutElement));
        scoreRecap.GetComponent<LayoutElement>().preferredHeight = 116f;
        VerticalLayoutGroup scoreRecapLayout = scoreRecap.GetComponent<VerticalLayoutGroup>();
        scoreRecapLayout.spacing = 4f;
        scoreRecapLayout.childControlWidth = true;
        scoreRecapLayout.childControlHeight = true;
        scoreRecapLayout.childForceExpandWidth = true;
        scoreRecapLayout.childForceExpandHeight = false;

        TMP_Text recapText = CreateText("ScoreRecapText", scoreRecap.transform, string.Empty, 1f, TextAlignmentOptions.Center, Color.clear);
        recapText.richText = true;
        recapText.gameObject.AddComponent<LayoutElement>().preferredHeight = 0f;

        GameObject scoreRow = CreateRect("ScoreRow", scoreRecap.transform, typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        scoreRow.GetComponent<LayoutElement>().preferredHeight = 34f;
        HorizontalLayoutGroup scoreRowLayout = scoreRow.GetComponent<HorizontalLayoutGroup>();
        scoreRowLayout.spacing = 8f;
        scoreRowLayout.childControlWidth = true;
        scoreRowLayout.childControlHeight = true;
        scoreRowLayout.childForceExpandWidth = true;
        scoreRowLayout.childForceExpandHeight = true;

        TMP_Text homeTeamScoreText = CreateScoreColumnText("HomeTeamText", scoreRow.transform, "Home", 19f, TextAlignmentOptions.Right, 1.1f);
        TMP_Text centerScoreText = CreateScoreColumnText("CenterScoreText", scoreRow.transform, "0 - 0", 19f, TextAlignmentOptions.Center, 0.7f);
        TMP_Text awayTeamScoreText = CreateScoreColumnText("AwayTeamText", scoreRow.transform, "Away", 19f, TextAlignmentOptions.Left, 1.1f);

        GameObject scorersRow = CreateRect("ScorersRow", scoreRecap.transform, typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        scorersRow.GetComponent<LayoutElement>().preferredHeight = 74f;
        HorizontalLayoutGroup scorersRowLayout = scorersRow.GetComponent<HorizontalLayoutGroup>();
        scorersRowLayout.spacing = 8f;
        scorersRowLayout.childControlWidth = true;
        scorersRowLayout.childControlHeight = true;
        scorersRowLayout.childForceExpandWidth = true;
        scorersRowLayout.childForceExpandHeight = true;

        TMP_Text homeScorersText = CreateScoreColumnText("HomeScorersText", scorersRow.transform, "Home scorer 15'", 15f, TextAlignmentOptions.TopRight, 1.1f);
        CreateScoreColumnText("ScorersSpacer", scorersRow.transform, string.Empty, 15f, TextAlignmentOptions.Center, 0.7f);
        TMP_Text awayScorersText = CreateScoreColumnText("AwayScorersText", scorersRow.transform, "Away scorer 77'", 15f, TextAlignmentOptions.TopLeft, 1.1f);

        GameObject statsGrid = CreateRect("StatsGrid", content.transform, typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        statsGrid.GetComponent<LayoutElement>().preferredHeight = 560f;
        HorizontalLayoutGroup gridLayout = statsGrid.GetComponent<HorizontalLayoutGroup>();
        gridLayout.spacing = 10f;
        gridLayout.childControlWidth = true;
        gridLayout.childControlHeight = true;
        gridLayout.childForceExpandWidth = true;
        gridLayout.childForceExpandHeight = true;

        TMP_Text[] homeStatsTexts = CreateStatsColumn(statsGrid.transform, "HomeStats", "HomeStat_", string.Empty, TextAlignmentOptions.Right);
        TMP_Text[] statTitleTexts = CreateStatsColumn(statsGrid.transform, "StatTitles", "StatTitle_", null, TextAlignmentOptions.Center);
        TMP_Text[] awayStatsTexts = CreateStatsColumn(statsGrid.transform, "AwayStats", "AwayStat_", string.Empty, TextAlignmentOptions.Left);
        scroll.viewport = viewportRect;
        scroll.content = contentRect;

        Button mainMenuButton = CreateButton(panel.transform);
        root.GetComponent<EndGamePanelManager>().ConfigureReferences(
            root,
            titleText,
            recapText,
            homeTeamScoreText,
            centerScoreText,
            awayTeamScoreText,
            homeScorersText,
            awayScorersText,
            scroll,
            homeStatsTexts,
            statTitleTexts,
            awayStatsTexts,
            mainMenuButton);

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[EndGamePanelPrefab] Created {PrefabPath}");
    }

    [MenuItem("Tools/Counter Attack/Ensure End Game Panel In Scene")]
    public static void EnsureSceneInstanceInEditMode()
    {
        if (Application.isPlaying)
        {
            return;
        }

        Canvas canvas = ResolveMainCanvas();
        if (canvas == null)
        {
            return;
        }

        EndGamePanelManager existing = canvas.GetComponentsInChildren<EndGamePanelManager>(true)
            .FirstOrDefault(manager => manager != null && (manager.name == SceneInstanceName || manager.name == "EndGamePanelManager"));
        if (existing != null)
        {
            existing.gameObject.SetActive(false);
            return;
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            return;
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab, canvas.transform) as GameObject;
        if (instance == null)
        {
            return;
        }

        instance.name = SceneInstanceName;
        instance.SetActive(false);
        RectTransform rect = instance.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log($"[EndGamePanelPrefab] Added inactive scene instance under {canvas.name}.");
    }

    private static Button CreateButton(Transform parent)
    {
        GameObject buttonObject = CreateRect("BackToMainMenuButton", parent, typeof(Image), typeof(Button));
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.pivot = new Vector2(0.5f, 0f);
        buttonRect.anchoredPosition = new Vector2(0f, 26f);
        buttonRect.sizeDelta = new Vector2(300f, 54f);
        buttonObject.GetComponent<Image>().color = new Color(0.92f, 0.92f, 0.92f, 1f);

        TMP_Text buttonText = CreateText("Text", buttonObject.transform, "Back To Main Menu", 23f, TextAlignmentOptions.Center, new Color(0.2f, 0.2f, 0.2f, 1f));
        RectTransform textRect = buttonText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        buttonText.fontStyle = FontStyles.SmallCaps;
        return buttonObject.GetComponent<Button>();
    }

    private static TMP_Text CreateScoreColumnText(string name, Transform parent, string textValue, float fontSize, TextAlignmentOptions alignment, float flexibleWidth)
    {
        TMP_Text text = CreateText(name, parent, textValue, fontSize, alignment, Color.white);
        LayoutElement layout = text.gameObject.AddComponent<LayoutElement>();
        layout.flexibleWidth = flexibleWidth;
        layout.preferredHeight = name.Contains("Scorers") ? 74f : 34f;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
        return text;
    }

    private static TMP_Text[] CreateStatsColumn(Transform parent, string columnName, string rowPrefix, string valueText, TextAlignmentOptions alignment)
    {
        GameObject column = CreateRect(columnName, parent, typeof(VerticalLayoutGroup), typeof(LayoutElement));
        column.GetComponent<LayoutElement>().flexibleWidth = columnName == "StatTitles" ? 1.4f : 1f;
        VerticalLayoutGroup layout = column.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 4f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        TMP_Text[] texts = new TMP_Text[StatsRowCount];
        for (int i = 0; i < StatsRowCount; i++)
        {
            string textValue = valueText ?? StatRowTitles[i];
            TMP_Text text = CreateText($"{rowPrefix}{i + 1:00}", column.transform, textValue, 15f, alignment, Color.white);
            text.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;
            texts[i] = text;
        }

        return texts;
    }

    private static TMP_Text CreateText(string name, Transform parent, string text, float fontSize, TextAlignmentOptions alignment, Color color)
    {
        GameObject textObject = CreateRect(name, parent, typeof(TextMeshProUGUI));
        TMP_Text tmp = textObject.GetComponent<TMP_Text>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.raycastTarget = false;
        tmp.characterSpacing = 0f;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 10f;
        tmp.fontSizeMax = fontSize;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        AssignDefaultFont(tmp);
        return tmp;
    }

    private static void AssignDefaultFont(TMP_Text text)
    {
        if (text != null && TMP_Settings.defaultFontAsset != null)
        {
            text.font = TMP_Settings.defaultFontAsset;
        }
    }

    private static bool PrefabNeedsRebuild()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            return true;
        }

        EndGamePanelManager manager = prefab.GetComponent<EndGamePanelManager>();
        ScrollRect scroll = prefab.GetComponentInChildren<ScrollRect>(true);
        Button button = prefab.GetComponentsInChildren<Button>(true).FirstOrDefault(candidate => candidate != null && candidate.name == "BackToMainMenuButton");
        TMP_Text title = prefab.transform.Find("RecapPanel/Title")?.GetComponent<TMP_Text>();
        TMP_Text recap = prefab.transform.Find("RecapPanel/RecapScroll/Viewport/Content/ScoreRecap/ScoreRecapText")?.GetComponent<TMP_Text>();
        TMP_Text homeTeam = prefab.transform.Find("RecapPanel/RecapScroll/Viewport/Content/ScoreRecap/ScoreRow/HomeTeamText")?.GetComponent<TMP_Text>();
        TMP_Text centerScore = prefab.transform.Find("RecapPanel/RecapScroll/Viewport/Content/ScoreRecap/ScoreRow/CenterScoreText")?.GetComponent<TMP_Text>();
        TMP_Text awayTeam = prefab.transform.Find("RecapPanel/RecapScroll/Viewport/Content/ScoreRecap/ScoreRow/AwayTeamText")?.GetComponent<TMP_Text>();
        TMP_Text homeScorers = prefab.transform.Find("RecapPanel/RecapScroll/Viewport/Content/ScoreRecap/ScorersRow/HomeScorersText")?.GetComponent<TMP_Text>();
        TMP_Text awayScorers = prefab.transform.Find("RecapPanel/RecapScroll/Viewport/Content/ScoreRecap/ScorersRow/AwayScorersText")?.GetComponent<TMP_Text>();
        Transform homeStats = prefab.transform.Find("RecapPanel/RecapScroll/Viewport/Content/StatsGrid/HomeStats");
        Transform statTitles = prefab.transform.Find("RecapPanel/RecapScroll/Viewport/Content/StatsGrid/StatTitles");
        Transform awayStats = prefab.transform.Find("RecapPanel/RecapScroll/Viewport/Content/StatsGrid/AwayStats");
        return manager == null
            || title == null
            || recap == null
            || scroll == null
            || scroll.viewport == null
            || scroll.content == null
            || button == null
            || homeTeam == null
            || centerScore == null
            || awayTeam == null
            || homeScorers == null
            || awayScorers == null
            || CountDirectTextChildren(homeStats) != StatsRowCount
            || CountDirectTextChildren(statTitles) != StatsRowCount
            || CountDirectTextChildren(awayStats) != StatsRowCount;
    }

    private static int CountDirectTextChildren(Transform parent)
    {
        if (parent == null)
        {
            return 0;
        }

        return parent.Cast<Transform>().Count(child => child.GetComponent<TMP_Text>() != null);
    }

    private static Canvas ResolveMainCanvas()
    {
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
        Canvas namedCanvas = canvases.FirstOrDefault(candidate => candidate != null && candidate.name == "Canvas");
        if (namedCanvas != null)
        {
            return namedCanvas;
        }

        return canvases.FirstOrDefault(candidate =>
            candidate != null
            && candidate.isRootCanvas
            && candidate.name != "HoveredTokenNameCanvas");
    }

    private static GameObject CreateRect(string name, Transform parent, params System.Type[] components)
    {
        System.Type[] componentTypes = new System.Type[components.Length + 2];
        componentTypes[0] = typeof(RectTransform);
        componentTypes[1] = typeof(CanvasRenderer);
        for (int i = 0; i < components.Length; i++)
        {
            componentTypes[i + 2] = components[i];
        }

        GameObject gameObject = new(name, componentTypes);
        if (parent != null)
        {
            gameObject.transform.SetParent(parent, false);
        }

        return gameObject;
    }
}
#endif
