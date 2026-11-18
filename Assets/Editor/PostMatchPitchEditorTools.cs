using System.Linq;
using TMPro;
using UnityEngine;

public static class PostMatchPitchEditorTools
{
    public static void Author(GameObject root)
    {
        var content = root.transform.Find(PostMatchReportEditorTools.ContentPath);
        var existing = content.Find("Pitch");
        if (existing != null)
        {
            var map = existing.Find("PitchMap");
            if (map == null) map = existing.Find("PitchFrame/PitchMap");
            if (map.GetComponent<CanvasRenderer>() == null) map.gameObject.AddComponent<CanvasRenderer>();
            UpgradeFiltersAndProportions(root, existing.gameObject, map.gameObject);
            root.GetComponent<PostMatchReportController>().ConfigurePitch(content.Find("ReportTabs/PitchTab").GetComponent<UnityEngine.UI.Button>(), existing.GetComponent<PostMatchPitchView>());
            return;
        }
        var font = content.Find("StatsGrid").GetComponentsInChildren<TMP_Text>(true).First(t => t.font != null).font;
        var tab = PostMatchReportEditorTools.Button("PitchTab", content.Find("ReportTabs"), "PITCH", font);
        var page = PostMatchReportEditorTools.Group("Pitch", content, false);
        page.GetComponent<RectTransform>().pivot = new Vector2(.5f, 1);
        page.AddComponent<UnityEngine.UI.ContentSizeFitter>().verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
        var status = Label("PitchStatus", "Average positions · Home attacks → / Away ←", 38);
        var pitchObject = new GameObject("PitchMap", typeof(RectTransform), typeof(CanvasRenderer), typeof(PostMatchPitchGraphic), typeof(UnityEngine.UI.LayoutElement));
        pitchObject.transform.SetParent(page.transform, false);
        pitchObject.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 420;
        var graphic = pitchObject.GetComponent<PostMatchPitchGraphic>(); graphic.raycastTarget = false;
        var markers = new PostMatchPitchMarker[36];
        var sprite = UnityEditor.AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        for (int i = 0; i < markers.Length; i++)
        {
            var go = new GameObject($"PlayerMarker_{i + 1:00}", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(PostMatchPitchMarker));
            go.transform.SetParent(pitchObject.transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.1f + .08f * (i % 11), .2f + .25f * (i / 11));
            rect.sizeDelta = new Vector2(40, 40);
            var image = go.GetComponent<UnityEngine.UI.Image>(); image.sprite = sprite; image.raycastTarget = true;
            image.color = i < 18 ? new Color(.12f, .45f, .65f) : new Color(.78f, .36f, .12f);
            var number = PostMatchReportEditorTools.Text("Number", go.transform, (i % 18 + 1).ToString(), font, 22, 40);
            number.rectTransform.anchorMin = Vector2.zero; number.rectTransform.anchorMax = Vector2.one;
            number.rectTransform.offsetMin = number.rectTransform.offsetMax = Vector2.zero;
            number.alignment = TextAlignmentOptions.Center;
            markers[i] = go.GetComponent<PostMatchPitchMarker>(); markers[i].number = number; markers[i].disc = image;
            go.SetActive(false);
        }
        var detail = Label("PitchPlayerDetail", "Hover a number to see its heatmap. Click to pin.", 44);
        var note = Label("PitchMethod", "Positions sampled at resolved actions; movement shows committed paths. Heat intensity is relative to the selected player.", 44);
        note.fontSize = 17;
        var view = page.AddComponent<PostMatchPitchView>();
        UpgradeFiltersAndProportions(root, page, pitchObject);
        root.GetComponent<PostMatchReportController>().ConfigurePitch(tab, view);
        page.SetActive(false);

        TMP_Text Label(string name, string text, float height)
        {
            var label = PostMatchReportEditorTools.Text(name, page.transform, text, font, 20, height);
            label.color = new Color(.12f, .26f, .33f);
            label.textWrappingMode = TextWrappingModes.Normal;
            return label;
        }
    }

    private static void UpgradeFiltersAndProportions(GameObject root, GameObject page, GameObject map)
    {
        foreach (string obsolete in new[] { "HalfFilters", "PossessionFilters", "HeatmapMode" })
        {
            var old = page.transform.Find(obsolete);
            if (old != null) Object.DestroyImmediate(old.gameObject);
        }
        var font = page.GetComponentsInChildren<TMP_Text>(true).First(t => t.font != null).font;
        foreach (var marker in map.GetComponentsInChildren<PostMatchPitchMarker>(true))
        {
            var border = marker.GetComponent<UnityEngine.UI.Outline>();
            if (border == null) border = marker.gameObject.AddComponent<UnityEngine.UI.Outline>();
            border.effectDistance = new Vector2(2f, 2f);
            border.useGraphicAlpha = false;
            marker.border = border;
        }
        var row = page.transform.Find("PitchFilters");
        if (row == null)
        {
            row = PostMatchReportEditorTools.Group("PitchFilters", page.transform, true, 70).transform;
            var titles = new[] { "Period", "Possession", "Heatmap" };
            var captions = new[] { "Full game", "All game", "Position" };
            for (int i = 0; i < titles.Length; i++)
            {
                var column = PostMatchReportEditorTools.Group(titles[i] + "Filter", row, false, 70);
                column.GetComponent<UnityEngine.UI.LayoutElement>().flexibleWidth = 1;
                var title = PostMatchReportEditorTools.Text("Title", column.transform, titles[i], font, 17, 20);
                title.color = new Color(.40f, .46f, .51f);
                PostMatchDropdownEditorTools.Create(titles[i] + "Dropdown", column.transform, captions[i], font);
            }
        }
        row.SetSiblingIndex(1);
        var frame = page.transform.Find("PitchFrame");
        if (frame == null)
        {
            var go = new GameObject("PitchFrame", typeof(RectTransform), typeof(UnityEngine.UI.LayoutElement));
            go.transform.SetParent(page.transform, false); frame = go.transform;
        }
        frame.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 460;
        frame.SetSiblingIndex(2);
        map.transform.SetParent(frame, false);
        var rect = map.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.anchoredPosition = Vector2.zero; rect.sizeDelta = new Vector2(460 * PostMatchPitchData.PitchAspectRatio, 460);
        var aspect = map.GetComponent<UnityEngine.UI.AspectRatioFitter>();
        if (aspect == null) aspect = map.AddComponent<UnityEngine.UI.AspectRatioFitter>();
        aspect.aspectRatio = PostMatchPitchData.PitchAspectRatio; aspect.aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent;
        page.transform.Find("PitchMethod").GetComponent<TMP_Text>().text = "Halves are aligned before averaging. Positions use resolved actions; movement uses committed paths. Heat intensity is relative to the player.";
        var dropdowns = row.GetComponentsInChildren<PostMatchDropdown>(true);
        foreach (var dropdown in dropdowns)
        {
            dropdown.transform.Find("Arrow").GetComponent<TMP_Text>().text = "v";
            dropdown.template.GetComponent<UnityEngine.UI.ScrollRect>().content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                PostMatchDropdown.RowHeight + PostMatchDropdown.Padding);
            var viewport = dropdown.template.GetComponent<UnityEngine.UI.ScrollRect>().viewport;
            viewport.offsetMin = new Vector2(4, 0); viewport.offsetMax = new Vector2(-4, 0);
        }
        page.GetComponent<PostMatchPitchView>().Configure(map.GetComponent<PostMatchPitchGraphic>(), map.GetComponentsInChildren<PostMatchPitchMarker>(true),
            page.transform.Find("PitchStatus").GetComponent<TMP_Text>(), page.transform.Find("PitchPlayerDetail").GetComponent<TMP_Text>(),
            dropdowns.Single(d => d.name == "PeriodDropdown"), dropdowns.Single(d => d.name == "PossessionDropdown"), dropdowns.Single(d => d.name == "HeatmapDropdown"));
    }
}
