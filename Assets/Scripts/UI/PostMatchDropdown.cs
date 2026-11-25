using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Each authored filter opens at its own option count, without a scrolling list.</summary>
public sealed class PostMatchDropdown : TMP_Dropdown
{
    public const float RowHeight = 38f;
    public const float Padding = 8f;

    public void FitOptions()
    {
        if (template == null) return;
        template.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(1, options.Count) * RowHeight + Padding);
        var scroll = template.GetComponent<UnityEngine.UI.ScrollRect>();
        if (scroll != null) { scroll.horizontal = false; scroll.vertical = false; scroll.inertia = false; }
    }

    public new void Show() { FitOptions(); base.Show(); }
    public override void OnPointerClick(PointerEventData eventData) { FitOptions(); base.OnPointerClick(eventData); }
    public override void OnSubmit(BaseEventData eventData) { FitOptions(); base.OnSubmit(eventData); }
}
