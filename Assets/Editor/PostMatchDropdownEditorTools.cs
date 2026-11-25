using TMPro;
using UnityEngine;

public static class PostMatchDropdownEditorTools
{
    public static PostMatchDropdown Create(string name, Transform parent, string caption, TMP_FontAsset font)
    {
        var root = Rect(name, parent, typeof(UnityEngine.UI.Image), typeof(PostMatchDropdown), typeof(UnityEngine.UI.LayoutElement));
        var dropdown = root.GetComponent<PostMatchDropdown>();
        root.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 42;
        root.GetComponent<UnityEngine.UI.LayoutElement>().flexibleWidth = 1;
        var background = root.GetComponent<UnityEngine.UI.Image>(); background.color = new Color(.10f, .15f, .22f);
        dropdown.targetGraphic = background;
        var label = PostMatchReportEditorTools.Text("Caption", root.transform, caption, font, 22, 42);
        Fill(label.rectTransform, new Vector2(12, 2), new Vector2(-32, -2));
        var arrow = PostMatchReportEditorTools.Text("Arrow", root.transform, "v", font, 22, 30);
        arrow.rectTransform.anchorMin = arrow.rectTransform.anchorMax = new Vector2(1, .5f);
        arrow.rectTransform.sizeDelta = new Vector2(24, 30); arrow.rectTransform.anchoredPosition = new Vector2(-16, 0);
        arrow.alignment = TextAlignmentOptions.Center;

        var template = Rect("Template", root.transform, typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.ScrollRect));
        var templateRect = template.GetComponent<RectTransform>();
        templateRect.anchorMin = new Vector2(0, 0); templateRect.anchorMax = new Vector2(1, 0);
        templateRect.pivot = new Vector2(.5f, 1); templateRect.sizeDelta = new Vector2(0, 84); templateRect.anchoredPosition = new Vector2(0, -2);
        template.GetComponent<UnityEngine.UI.Image>().color = Color.white;
        var viewport = Rect("Viewport", template.transform, typeof(UnityEngine.UI.RectMask2D));
        Fill(viewport.GetComponent<RectTransform>(), new Vector2(4, 0), new Vector2(-4, 0));
        var content = Rect("Content", viewport.transform);
        var contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1); contentRect.anchorMax = Vector2.one;
        contentRect.pivot = new Vector2(.5f, 1); contentRect.sizeDelta = new Vector2(0, PostMatchDropdown.RowHeight + PostMatchDropdown.Padding);
        var item = Rect("Item", content.transform, typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Toggle));
        var itemRect = item.GetComponent<RectTransform>();
        itemRect.anchorMin = new Vector2(0, .5f); itemRect.anchorMax = new Vector2(1, .5f);
        itemRect.sizeDelta = new Vector2(0, PostMatchDropdown.RowHeight);
        item.GetComponent<UnityEngine.UI.Image>().color = new Color(.94f, .96f, .98f);
        var toggle = item.GetComponent<UnityEngine.UI.Toggle>(); toggle.targetGraphic = item.GetComponent<UnityEngine.UI.Image>();
        var check = Rect("Checkmark", item.transform, typeof(UnityEngine.UI.Image));
        var checkRect = check.GetComponent<RectTransform>(); checkRect.anchorMin = checkRect.anchorMax = new Vector2(0, .5f);
        checkRect.sizeDelta = new Vector2(8, 8); checkRect.anchoredPosition = new Vector2(10, 0);
        check.GetComponent<UnityEngine.UI.Image>().color = new Color(.19f, .39f, .48f); toggle.graphic = check.GetComponent<UnityEngine.UI.Image>();
        var itemLabel = PostMatchReportEditorTools.Text("ItemLabel", item.transform, caption, font, 20, PostMatchDropdown.RowHeight);
        Fill(itemLabel.rectTransform, new Vector2(24, 0), new Vector2(-8, 0)); itemLabel.color = new Color(.12f, .26f, .33f);
        var scroll = template.GetComponent<UnityEngine.UI.ScrollRect>();
        scroll.content = contentRect; scroll.viewport = viewport.GetComponent<RectTransform>();
        scroll.horizontal = scroll.vertical = scroll.inertia = false;
        dropdown.captionText = label; dropdown.itemText = itemLabel; dropdown.template = templateRect;
        dropdown.options.Add(new TMP_Dropdown.OptionData(caption));
        dropdown.FitOptions(); template.SetActive(false);
        return dropdown;
    }

    private static GameObject Rect(string name, Transform parent, params System.Type[] components)
    {
        var types = new System.Collections.Generic.List<System.Type> { typeof(RectTransform), typeof(CanvasRenderer) }; types.AddRange(components);
        var go = new GameObject(name, types.ToArray()); go.transform.SetParent(parent, false); return go;
    }
    private static void Fill(RectTransform rect, Vector2 min, Vector2 max)
    { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = min; rect.offsetMax = max; }
}
