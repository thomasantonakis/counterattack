#if UNITY_EDITOR
using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Explicit, additive authoring upgrade. Preserves existing score/stats objects and designer layout.</summary>
public static class PostMatchReportEditorTools
{
    public const string PrefabPath = "Assets/Resources/UI/EndGamePanel.prefab";
    public const string ContentPath = "RecapPanel/RecapScroll/Viewport/Content";
    public const int PageSize = 12;

    [MenuItem("Tools/Counter Attack/Add Post Match Report Tabs")]
    public static void UpgradePrefabAndLoadedRoom()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Author post-match tabs in Edit Mode only.");
        var scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/Room.unity")
            throw new InvalidOperationException("Open Room normally before upgrading its existing panel.");
        var asset = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            Author(asset);
            PrefabUtility.SaveAsPrefabAsset(asset, PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(asset); }

        var canvas = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "MainCanvas");
        var panel = canvas != null ? canvas.transform.Find("EndGamePanel") : null;
        if (panel == null) throw new InvalidOperationException("Room's MainCanvas/EndGamePanel is missing.");
        Author(panel.gameObject);
        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
    }

    public static void Author(GameObject root)
    {
        // Once authored, subsequent calls preserve all designer adjustments.
        if (root.GetComponent<PostMatchReportController>() != null)
        {
            UpgradeControls(root);
            PostMatchPitchEditorTools.Author(root);
            PostMatchLuckEditorTools.Author(root);
            FitReportPages(root);
            return;
        }
        var content = root.transform.Find(ContentPath);
        var stats = content != null ? content.Find("StatsGrid") : null;
        if (stats == null) throw new InvalidOperationException("Expected the existing post-game stats grid.");
        var font = stats.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t => t.font != null)?.font;

        var tabs = Group("ReportTabs", content, true, 48);
        tabs.transform.SetSiblingIndex(stats.GetSiblingIndex());
        var statsTab = Button("MatchStatsTab", tabs.transform, "MATCH STATS", font);
        var playersTab = Button("TopPerformersTab", tabs.transform, "TOP PERFORMERS", font);

        var players = Group("TopPerformers", content, false);
        var layout = players.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
        layout.spacing = 12;
        var heading = Text("Heading", players.transform, "Distance", font, 28, 42);
        heading.fontStyle = FontStyles.Bold;
        var description = Text("Description", players.transform,
            "Compare recorded player performances across both teams.", font, 19, 48);
        description.textWrappingMode = TextWrappingModes.Normal;
        description.color = new Color(.72f, .78f, .84f);

        string[] metricLabels = { "Distance", "Recoveries", "Passes", "Duels", "Goals", "Assists" };
        var metricKinds = (PostMatchLeaderboard.Metric[])Enum.GetValues(typeof(PostMatchLeaderboard.Metric));
        var metricButtons = new UnityEngine.UI.Button[metricLabels.Length];
        for (int group = 0; group < 2; group++)
        {
            var bar = Group($"Metrics{group + 1}", players.transform, true, 42);
            for (int i = 0; i < 3; i++)
            {
                int index = group * 3 + i;
                metricButtons[index] = Button($"Metric_{metricKinds[index]}", bar.transform, metricLabels[index], font);
            }
        }
        var filters = Group("TeamFilters", content, true, 42);
        filters.transform.SetSiblingIndex(tabs.transform.GetSiblingIndex() + 1);
        var teamButtons = new[]
        {
            Button("BothTeams", filters.transform, "Both teams", font),
            Button("HomeTeam", filters.transform, "Home", font),
            Button("AwayTeam", filters.transform, "Away", font),
        };

        var header = Group("ColumnHeadings", players.transform, true, 30);
        Cell("Rank", header.transform, "#", font, 60, false);
        Cell("Player", header.transform, "PLAYER", font, 4, true);
        Cell("Team", header.transform, "TEAM", font, 3, true);
        var valueHeading = Cell("Value", header.transform, "Distance", font, 3, true);
        valueHeading.alignment = TextAlignmentOptions.Right;
        var table = Group("PlayerRows", players.transform, false);
        table.GetComponent<UnityEngine.UI.VerticalLayoutGroup>().spacing = 2;
        var rows = new PostMatchReportController.PlayerRow[PageSize];
        for (int i = 0; i < rows.Length; i++)
        {
            var row = Group($"PlayerRow_{i + 1:00}", table.transform, true, 36);
            var image = row.AddComponent<UnityEngine.UI.Image>();
            image.color = i % 2 == 0 ? new Color(.92f, .95f, .97f, 1f) : new Color(.97f, .98f, .99f, 1f);
            image.raycastTarget = false;
            rows[i] = new PostMatchReportController.PlayerRow
            {
                root = row,
                rank = Cell("Rank", row.transform, (i + 1).ToString(), font, 60, false),
                player = Cell("Player", row.transform, "Player name", font, 4, true),
                team = Cell("Team", row.transform, "Team name", font, 3, true),
                value = Cell("Value", row.transform, "—", font, 3, true),
            };
            rows[i].value.alignment = TextAlignmentOptions.Right;
        }
        var empty = Text("EmptyMessage", players.transform, "No recorded performances for this selection.", font, 22, 56);
        empty.textWrappingMode = TextWrappingModes.Normal;
        var footer = Group("Paging", players.transform, true, 42);
        var previous = Button("Previous", footer.transform, "Previous", font);
        var pageLabel = Text("PageLabel", footer.transform, "0 players", font, 19, 40);
        pageLabel.alignment = TextAlignmentOptions.Center;
        pageLabel.GetComponent<UnityEngine.UI.LayoutElement>().flexibleWidth = 2;
        var next = Button("Next", footer.transform, "Next", font);

        var controller = root.AddComponent<PostMatchReportController>();
        controller.Configure(stats.gameObject, players, tabs, statsTab, playersTab,
            metricButtons, teamButtons, heading, description, empty, pageLabel, previous, next, rows,
            metricKinds, valueHeading, ColumnGroup(stats, "HomeStats"), ColumnGroup(stats, "AwayStats"));
        // The scroll viewport needs a raycast surface for mouse-wheel/drag input over empty space.
        var viewport = root.transform.Find("RecapPanel/RecapScroll/Viewport");
        if (viewport.GetComponent<UnityEngine.UI.Image>() == null)
        {
            var image = viewport.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = Color.clear;
            image.raycastTarget = true;
        }
        players.SetActive(false);
        UpgradeControls(root);
        PostMatchPitchEditorTools.Author(root);
        PostMatchLuckEditorTools.Author(root);
        FitReportPages(root);
        EditorUtility.SetDirty(root);
    }

    private static CanvasGroup ColumnGroup(Transform stats, string name)
    {
        var column = stats.Find(name);
        var group = column.GetComponent<CanvasGroup>();
        return group != null ? group : column.gameObject.AddComponent<CanvasGroup>();
    }

    private static void UpgradeControls(GameObject root)
    {
        var content = root.transform.Find(ContentPath);
        var players = content.Find("TopPerformers");
        var filters = players.Find("TeamFilters") ?? content.Find("TeamFilters");
        filters.SetParent(content, false);
        filters.SetSiblingIndex(content.Find("ReportTabs").GetSiblingIndex() + 1);
        var filtersRect = filters.GetComponent<RectTransform>();
        filtersRect.sizeDelta = new Vector2(filtersRect.sizeDelta.x, 42);
        foreach (string obsolete in new[] { "Metric_PassAccuracy", "Metric_Saves" })
        {
            var old = players.GetComponentsInChildren<UnityEngine.UI.Button>(true).FirstOrDefault(b => b.name == obsolete);
            if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
        }
        var distance = players.GetComponentsInChildren<UnityEngine.UI.Button>(true).FirstOrDefault(b => b.name == "Metric_Paces");
        if (distance != null) distance.name = "Metric_Distance";
        var kinds = (PostMatchLeaderboard.Metric[])Enum.GetValues(typeof(PostMatchLeaderboard.Metric));
        var buttons = kinds.Select(k => players.GetComponentsInChildren<UnityEngine.UI.Button>(true).Single(b => b.name == $"Metric_{k}")).ToArray();
        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].transform.SetParent(players.Find($"Metrics{i / 3 + 1}"), false);
            buttons[i].GetComponentInChildren<TMP_Text>(true).text = kinds[i].ToString();
        }
        var footer = players.Find("Paging") ?? root.transform.Find("RecapPanel/Paging");
        footer.SetParent(root.transform.Find("RecapPanel"), false);
        var footerRect = footer.GetComponent<RectTransform>();
        footerRect.anchorMin = new Vector2(0, 0);
        footerRect.anchorMax = new Vector2(1, 0);
        footerRect.pivot = new Vector2(.5f, 0);
        footerRect.sizeDelta = new Vector2(-96, 42);
        footerRect.anchoredPosition = new Vector2(0, 130);
        var scrollRect = root.transform.Find("RecapPanel/RecapScroll").GetComponent<RectTransform>();
        scrollRect.offsetMin = new Vector2(scrollRect.offsetMin.x, 180);
        footer.gameObject.SetActive(false);
        var controller = root.GetComponent<PostMatchReportController>();
        var rowRoots = players.Find("PlayerRows").Cast<Transform>().ToArray();
        var rows = rowRoots.Select(r => new PostMatchReportController.PlayerRow
        {
            root = r.gameObject, rank = r.Find("Rank").GetComponent<TMP_Text>(), player = r.Find("Player").GetComponent<TMP_Text>(),
            team = r.Find("Team").GetComponent<TMP_Text>(), value = r.Find("Value").GetComponent<TMP_Text>()
        }).ToArray();
        var valueHeading = players.Find("ColumnHeadings/Value").GetComponent<TMP_Text>();
        valueHeading.text = "Distance";
        foreach (var text in rows.Select(r => r.value).Append(valueHeading))
            text.GetComponent<UnityEngine.UI.LayoutElement>().flexibleWidth = 3;
        controller.Configure(content.Find("StatsGrid").gameObject, players.gameObject, content.Find("ReportTabs").gameObject,
            content.Find("ReportTabs/MatchStatsTab").GetComponent<UnityEngine.UI.Button>(), content.Find("ReportTabs/TopPerformersTab").GetComponent<UnityEngine.UI.Button>(),
            buttons, new[] { filters.Find("BothTeams"), filters.Find("HomeTeam"), filters.Find("AwayTeam") }.Select(t => t.GetComponent<UnityEngine.UI.Button>()).ToArray(),
            players.Find("Heading").GetComponent<TMP_Text>(), players.Find("Description").GetComponent<TMP_Text>(), players.Find("EmptyMessage").GetComponent<TMP_Text>(),
            footer.Find("PageLabel").GetComponent<TMP_Text>(), footer.Find("Previous").GetComponent<UnityEngine.UI.Button>(), footer.Find("Next").GetComponent<UnityEngine.UI.Button>(), rows,
            kinds, valueHeading, ColumnGroup(content.Find("StatsGrid"), "HomeStats"), ColumnGroup(content.Find("StatsGrid"), "AwayStats"));
    }

    private static void FitReportPages(GameObject root)
    {
        var content = root.transform.Find(ContentPath);
        var players = content.Find("TopPerformers");
        players.GetComponent<RectTransform>().pivot = new Vector2(.5f, 1f);
        var tabs = content.Find("ReportTabs").GetComponent<RectTransform>();
        // The designer's existing content layout intentionally does not control child height.
        // Fit only our new page, preserving the original stats grid's explicit dimensions.
        var fitter = players.GetComponent<UnityEngine.UI.ContentSizeFitter>();
        if (fitter == null) fitter = players.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
        fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
        tabs.sizeDelta = new Vector2(tabs.sizeDelta.x, 48);
        var font = content.Find("StatsGrid").GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t => t.font != null)?.font;
        foreach (var text in players.GetComponentsInChildren<TMP_Text>(true))
        {
            if (font != null) text.font = font;
            bool darkSurface = text.transform.parent.GetComponent<UnityEngine.UI.Button>() != null;
            text.color = darkSurface ? Color.white : new Color(.12f, .26f, .33f);
        }
        var rowImages = players.Find("PlayerRows").GetComponentsInChildren<UnityEngine.UI.Image>(true);
        for (int i = 0; i < rowImages.Length; i++)
        {
            rowImages[i].color = i % 2 == 0 ? new Color(.92f, .95f, .97f, 1f) : new Color(.97f, .98f, .99f, 1f);
            rowImages[i].GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 32;
        }
        foreach (var text in tabs.GetComponentsInChildren<TMP_Text>(true)) if (font != null) text.font = font;
        var paging = root.transform.Find("RecapPanel/Paging");
        if (paging != null) paging.Find("PageLabel").GetComponent<TMP_Text>().color = new Color(.40f, .46f, .51f);
        players.Find("Description").GetComponent<TMP_Text>().color = new Color(.40f, .46f, .51f);
    }

    internal static GameObject Group(string name, Transform parent, bool horizontal, float height = -1)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        UnityEngine.UI.HorizontalOrVerticalLayoutGroup layout = horizontal
            ? go.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>()
            : go.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        layout.spacing = 8;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        if (height >= 0)
        {
            go.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight = height;
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(100, height);
        }
        return go;
    }

    internal static UnityEngine.UI.Button Button(string name, Transform parent, string label, TMP_FontAsset font)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button), typeof(UnityEngine.UI.LayoutElement));
        go.transform.SetParent(parent, false);
        go.GetComponent<UnityEngine.UI.LayoutElement>().flexibleWidth = 1;
        go.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 42;
        var image = go.GetComponent<UnityEngine.UI.Image>();
        image.color = new Color(.10f, .15f, .22f, 1);
        var button = go.GetComponent<UnityEngine.UI.Button>();
        button.targetGraphic = image;
        button.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
        var text = Text("Label", go.transform, label, font, 22, 40);
        text.alignment = TextAlignmentOptions.Center;
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = new Vector2(8, 2);
        text.rectTransform.offsetMax = new Vector2(-8, -2);
        return button;
    }

    private static TMP_Text Cell(string name, Transform parent, string label, TMP_FontAsset font, float width, bool flexible)
    {
        var text = Text(name, parent, label, font, 20, 34);
        var element = text.GetComponent<UnityEngine.UI.LayoutElement>();
        if (flexible) { element.minWidth = 0; element.preferredWidth = 0; element.flexibleWidth = width; }
        else { element.minWidth = width; element.preferredWidth = width; element.flexibleWidth = 0; }
        return text;
    }

    internal static TMP_Text Text(string name, Transform parent, string label, TMP_FontAsset font, float fontSize, float height)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(UnityEngine.UI.LayoutElement));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<TMP_Text>();
        text.font = font != null ? font : TMP_Settings.defaultFontAsset;
        text.text = label;
        text.richText = false;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Left;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        go.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = height;
        return text;
    }
}
#endif
