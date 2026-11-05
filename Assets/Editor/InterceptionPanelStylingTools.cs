using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CounterAttack.Editor
{
    public static class InterceptionPanelStylingTools
    {
        private static readonly Color Navy = new Color32(30, 69, 89, 255);
        private static readonly Color Teal = new Color32(17, 123, 113, 255);

        [MenuItem("CounterAttack/Room/Style Existing Single Roll Panel")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new System.InvalidOperationException("Requires edit mode.");
            var p = Object.FindAnyObjectByType<DuelResolutionPanelController>();
            if (p == null || p.gameObject.scene.path != "Assets/Scenes/Room.unity")
                throw new System.InvalidOperationException("Open Room first.");
            var menu = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/UI/Cards/MenuCard.prefab");
            var sprite = menu.GetComponent<Image>().sprite;
            var heading = menu.transform.Find("WhiteBkg/HeaderPanel/TitleText").GetComponent<TMP_Text>().font;
            var background = p.panel.GetComponent<Image>();
            background.sprite = sprite; background.type = Image.Type.Sliced; background.color = Navy;
            p.title.font = heading;
            Text(p.title, 44, Color.white);
            Text(p.outcome, 32, Color.white);
            var content = (RectTransform)p.singleRollContent.transform;
            var surface = content.GetComponent<Image>();
            if (surface == null) surface = content.gameObject.AddComponent<Image>();
            surface.sprite = sprite; surface.type = Image.Type.Sliced; surface.color = Color.white;
            surface.raycastTarget = false;
            var player = p.singlePlayer;
            var column = (RectTransform)player.playerName.transform.parent;
            column.anchorMin = Vector2.zero; column.anchorMax = Vector2.one;
            column.offsetMin = column.offsetMax = Vector2.zero;
            Place(player.playerName.rectTransform, -170, -26, 560, 88);
            player.playerName.font = heading;
            Text(player.playerName, 42, Navy);
            Place(player.teamName.rectTransform, -170, -120, 560, 52);
            Text(player.teamName, 28, new Color32(75, 107, 123, 255));
            Place(player.role.rectTransform, -170, -174, 560, 32);
            Text(player.role, 22, Navy);
            var attribute = Box("AttributeTile", column, -310, -220, 256, 112, new Color32(236, 242, 244, 255), sprite);
            var probability = Box("RecoveryTile", column, -28, -220, 260, 112, new Color32(224, 242, 237, 255), sprite);
            Inset(player.attribute, attribute, 30, Navy);
            Inset(player.modifier, probability, 28, Teal);
            var statsRow = column.Find("StatsRow") as RectTransform;
            if (statsRow == null)
            {
                statsRow = (RectTransform)new GameObject("StatsRow", typeof(RectTransform)).transform;
                statsRow.gameObject.layer = column.gameObject.layer;
                statsRow.SetParent(column, false);
            }
            Place(statsRow, -170, -220, 560, 112);
            var statsLayout = statsRow.GetComponent<HorizontalLayoutGroup>();
            if (statsLayout == null) statsLayout = statsRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            statsLayout.spacing = 24;
            statsLayout.childControlWidth = statsLayout.childControlHeight = true;
            statsLayout.childForceExpandWidth = statsLayout.childForceExpandHeight = true;
            foreach (var tile in new[] { attribute, probability })
            {
                tile.SetParent(statsRow, false);
                var element = tile.GetComponent<LayoutElement>();
                if (element == null) element = tile.gameObject.AddComponent<LayoutElement>();
                element.minWidth = 0; element.preferredWidth = 0; element.flexibleWidth = 1;
                tile.gameObject.SetActive(true);
            }
            p.singleAttributeTile = attribute.gameObject;
            p.singleProbabilityTile = probability.gameObject;
            var roll = Box("RollTile", column, 310, -26, 296, 306, new Color32(236, 242, 244, 255), sprite);
            player.die.transform.SetParent(roll, false);
            Place((RectTransform)player.die.transform, 0, -24, 156, 156);
            player.total.transform.SetParent(roll, false);
            Place(player.total.rectTransform, 0, -202, 264, 80);
            Text(player.total, 28, Navy);
            player.total.alignment = TextAlignmentOptions.Center;
            var guidance = Box("GuidanceTile", column, 0, -360, 896, 108, new Color32(224, 242, 237, 255), sprite);
            Inset(player.guidance, guidance, 28, Teal);
            StyleExpandedSections();
            StyleComparisonSurface();
            EditorUtility.SetDirty(p);
            EditorSceneManager.MarkSceneDirty(p.gameObject.scene);
            EditorSceneManager.SaveScene(p.gameObject.scene);
        }

        [MenuItem("CounterAttack/Room/Style Shooting Comparison Surface")]
        public static void StyleComparisonSurface()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new System.InvalidOperationException("Requires edit mode.");
            var p = Object.FindAnyObjectByType<DuelResolutionPanelController>();
            var sprite = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/UI/Cards/MenuCard.prefab").GetComponent<Image>().sprite;
            var surface = p.playerColumns.GetComponent<Image>();
            if (surface == null) surface = p.playerColumns.AddComponent<Image>();
            surface.sprite = sprite; surface.type = Image.Type.Sliced;
            surface.color = Color.white; surface.raycastTarget = false;
            foreach (var player in new[] { p.leftPlayer, p.rightPlayer })
            {
                StretchTop(player.playerName.rectTransform, 10, 60); Text(player.playerName, 36, Navy);
                StretchTop(player.teamName.rectTransform, 74, 36); Text(player.teamName, 26, Navy);
                StretchTop(player.role.rectTransform, 112, 32); Text(player.role, 22, Teal);
                StretchTop(player.attribute.rectTransform, 150, 60); Text(player.attribute, 30, Navy);
                StretchTop(player.modifier.rectTransform, 210, 38); Text(player.modifier, 24, Teal);
                Place((RectTransform)player.die.transform, 0, -258, 96, 96);
                StretchTop(player.total.rectTransform, 362, 44); Text(player.total, 28, Navy);
                StretchTop(player.guidance.rectTransform, 410, 64); Text(player.guidance, 24, Navy);
            }
            EditorUtility.SetDirty(p);
            EditorSceneManager.MarkSceneDirty(p.gameObject.scene);
        }

        [MenuItem("CounterAttack/Room/Style Single Roll Expansion Sections")]
        public static void StyleExpandedSections()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new System.InvalidOperationException("Requires edit mode.");
            var p = Object.FindAnyObjectByType<DuelResolutionPanelController>();
            var sprite = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/UI/Cards/MenuCard.prefab").GetComponent<Image>().sprite;
            foreach (var section in new[] { p.scatterContinuationContent, p.foulChecks })
            {
                var rect = (RectTransform)section.transform;
                rect.anchoredPosition = new Vector2(0, -704);
                rect.sizeDelta = new Vector2(-48, 336);
                var image = section.GetComponent<Image>();
                if (image == null) image = section.AddComponent<Image>();
                image.sprite = sprite; image.type = Image.Type.Sliced;
                image.color = Color.white; image.raycastTarget = false;
            }
            var caption = p.scatterCaption;
            StretchTop(caption.rectTransform, 12, 42);
            Text(caption, 26, Navy);
            var columns = (RectTransform)p.continuationDirection.column.parent;
            StretchTop(columns, 60, 264);
            foreach (var check in new[] { p.continuationDirection, p.continuationDistance })
            {
                check.column.anchoredPosition = Vector2.zero;
                check.column.sizeDelta = new Vector2(-24, 264);
                var heading = check.column.Find("Heading").GetComponent<TMP_Text>();
                StretchTop(heading.rectTransform, 0, 38); Text(heading, 26, Navy);
                Place((RectTransform)check.die.transform, 0, -50, 100, 100);
                StretchTop(check.result.rectTransform, 174, 76); Text(check.result, 24, Navy);
            }
            foreach (var check in new[] { p.leniency, p.injury })
            {
                check.column.anchoredPosition = new Vector2(0, -12);
                check.column.sizeDelta = new Vector2(-24, 312);
                var title = check.column.Find("Title").GetComponent<TMP_Text>();
                StretchTop(title.rectTransform, 0, 38); Text(title, 26, Navy);
                StretchTop(check.rule.rectTransform, 44, 100); Text(check.rule, 22, Navy);
                Place((RectTransform)check.die.transform, 0, -152, 80, 80);
                StretchTop(check.result.rectTransform, 236, 70); Text(check.result, 22, Navy);
            }
            p.FitAndClamp();
            EditorUtility.SetDirty(p);
            EditorSceneManager.MarkSceneDirty(p.gameObject.scene);
        }

        private static void StretchTop(RectTransform rect, float top, float height)
        {
            rect.anchorMin = new Vector2(0, 1); rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(.5f, 1); rect.anchoredPosition = new Vector2(0, -top);
            rect.sizeDelta = new Vector2(-48, height); rect.localScale = Vector3.one;
        }

        private static RectTransform Box(string name, Transform parent, float x, float y, float width, float height, Color color, Sprite sprite)
        {
            var found = parent.Find(name) ?? parent.Find("StatsRow/" + name);
            var rect = found != null ? (RectTransform)found : (RectTransform)new GameObject(name, typeof(RectTransform), typeof(Image)).transform;
            rect.gameObject.layer = parent.gameObject.layer;
            rect.SetParent(parent, false);
            rect.SetAsFirstSibling();
            Place(rect, x, y, width, height);
            var image = rect.GetComponent<Image>();
            image.sprite = sprite; image.type = Image.Type.Sliced; image.color = color; image.raycastTarget = false;
            return rect;
        }

        private static void Inset(TMP_Text text, RectTransform parent, int size, Color color)
        {
            text.transform.SetParent(parent, false);
            text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = new Vector2(16, 12); text.rectTransform.offsetMax = new Vector2(-16, -12);
            Text(text, size, color);
            text.alignment = TextAlignmentOptions.Center;
        }

        private static void Text(TMP_Text text, int size, Color color)
        {
            text.fontSize = size; text.fontSizeMax = size; text.fontSizeMin = 18;
            text.enableAutoSizing = true; text.color = color;
            text.textWrappingMode = TextWrappingModes.Normal;
        }

        private static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1);
            rect.pivot = new Vector2(.5f, 1); rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height); rect.localScale = Vector3.one;
        }
    }
}
