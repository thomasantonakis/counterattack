using TMPro;
using UnityEngine;

namespace CounterAttack.Online
{
    public sealed class OnlineLobbyCodeTitle : MonoBehaviour
    {
        public TMP_Text title;

        private string lastTitle;
        private Rect lastTitleRect;
        private bool needsLayout = true;
        public static readonly Vector2 Padding = new Vector2(10, 6);

        private void OnEnable() => needsLayout = true;

        private void LateUpdate()
        {
            var titleRect = title.rectTransform.rect;
            if (!needsLayout && lastTitle == title.text && lastTitleRect == titleRect) return;

            needsLayout = false;
            lastTitle = title.text;
            lastTitleRect = titleRect;
            title.ForceMeshUpdate();
            var glyphs = title.textBounds;
            transform.localPosition = new Vector3(glyphs.max.x + 16f, glyphs.center.y);
        }

        internal static void Show(TMP_InputField field, string code)
        {
            if (field == null) return;
            bool visible = !string.IsNullOrEmpty(code);
            field.gameObject.SetActive(visible);
            if (!visible || field.text == code) return;

            var glyphs = field.textComponent.GetPreferredValues(code);
            ((RectTransform)field.transform).sizeDelta = new Vector2(Mathf.Ceil(glyphs.x), Mathf.Ceil(glyphs.y)) + 2 * Padding;
            field.SetTextWithoutNotify(code);
        }
    }
}
