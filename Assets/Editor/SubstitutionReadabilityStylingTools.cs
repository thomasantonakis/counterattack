using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore;
using Object = UnityEngine.Object;

namespace CounterAttack.Editor
{
    public static class SubstitutionReadabilityStylingTools
    {
        private static readonly Color Navy = new Color32(30, 69, 89, 255);

        [MenuItem("CounterAttack/Room/Update Substitution Readability")]
        public static void Apply()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != "Assets/Scenes/Room.unity"
                || UnityEngine.SceneManagement.SceneManager.sceneCount != 1)
                throw new InvalidOperationException("Open Room alone in edit mode.");
            ApplyTo(Object.FindAnyObjectByType<SubstitutionMenuView>(FindObjectsInactive.Include),
                Object.FindAnyObjectByType<PauseMenuManager>());
            AuthorLineupIcons();
            AuthorHoverReadability();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        public static void ApplyTo(SubstitutionMenuView view, PauseMenuManager pause)
        {
            var backgrounds = new List<SubstitutionMenuView.RosterRowBackground>();
            foreach (var text in view.homePlayingRows.Concat(view.homeBenchRows)
                .Concat(view.awayPlayingRows).Concat(view.awayBenchRows))
            {
                text.color = Color.black;
                text.fontSize = text.fontSizeMax = 28;
                text.fontSizeMin = 22;
                text.enableAutoSizing = true;
                var name = text.name + "StatusBackground";
                var old = text.transform.parent.Find(name);
                var background = old != null ? old.GetComponent<UnityEngine.UI.Image>()
                    : new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>();
                var rect = (RectTransform)background.transform;
                rect.SetParent(text.transform.parent, false);
                rect.SetAsFirstSibling();
                rect.gameObject.layer = text.gameObject.layer;
                rect.anchorMin = text.rectTransform.anchorMin; rect.anchorMax = text.rectTransform.anchorMax;
                rect.pivot = text.rectTransform.pivot; rect.sizeDelta = text.rectTransform.sizeDelta;
                rect.anchoredPosition = text.rectTransform.anchoredPosition;
                rect.sizeDelta += new Vector2(24, 0);
                background.sprite = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/UI/Cards/MenuCard.prefab")
                    .GetComponent<UnityEngine.UI.Image>().sprite;
                background.type = UnityEngine.UI.Image.Type.Sliced;
                background.color = Color.clear; background.raycastTarget = false;
                backgrounds.Add(new SubstitutionMenuView.RosterRowBackground { label = text, background = background });
            }
            view.rosterRowBackgrounds = backgrounds.ToArray();
            foreach (var row in view.homeRows.Concat(view.awayRows))
                foreach (var dropdown in new[] { row.outgoingDropdown, row.incomingDropdown })
                    CopySettingsDropdown(dropdown, pause.editBallColorDropdown);
            Footer(view.backButton, pause.editSettingsBackButton, false);
            Footer(view.confirmButton, pause.editSettingsBackButton, true);
            var footerFont = pause.editSettingsBackButton.transform.Find("Text (TMP)").GetComponent<TMP_Text>().font;
            foreach (var button in new[] { pause.saveAsCancelButton, pause.saveAsSaveButton, pause.overwriteCancelButton, pause.overwriteReplaceButton })
            {
                var label = button.GetComponentInChildren<TMP_Text>(true);
                label.font = footerFont;
                label.text = label.text.ToUpperInvariant();
                label.fontSize = label.fontSizeMax = 40;
                label.fontSizeMin = 24;
                label.enableAutoSizing = true;
            }
            EditorUtility.SetDirty(view);
        }

        private static void CopySettingsDropdown(TMP_Dropdown dropdown, TMP_Dropdown source)
        {
            EditorUtility.CopySerialized(source.GetComponent<UnityEngine.UI.Image>(), dropdown.GetComponent<UnityEngine.UI.Image>());
            dropdown.colors = source.colors;
            dropdown.transition = UnityEngine.UI.Selectable.Transition.ColorTint;
            var caption = dropdown.captionText;
            caption.font = source.captionText.font;
            caption.fontSize = caption.fontSizeMax = 26; caption.fontSizeMin = 20; caption.enableAutoSizing = true;
            caption.color = source.captionText.color;
            caption.rectTransform.anchorMin = Vector2.zero; caption.rectTransform.anchorMax = Vector2.one;
            caption.rectTransform.offsetMin = new Vector2(20, 0); caption.rectTransform.offsetMax = new Vector2(-52, 0);
            var oldArrow = dropdown.transform.Find("Arrow");
            if (oldArrow != null) Object.DestroyImmediate(oldArrow.gameObject);
            var arrow = Object.Instantiate(source.transform.Find("Arrow").gameObject, dropdown.transform, false);
            arrow.name = "Arrow";
            var arrowImage = arrow.GetComponent<UnityEngine.UI.Image>();
            if (arrowImage != null) arrowImage.color = Color.white;
            var arrowRect = (RectTransform)arrow.transform;
            arrowRect.anchorMin = arrowRect.anchorMax = new Vector2(1, .5f);
            arrowRect.pivot = new Vector2(1, .5f); arrowRect.anchoredPosition = new Vector2(-12, 0); arrowRect.sizeDelta = new Vector2(24, 24);
            var oldTemplate = dropdown.template;
            var labelPath = AnimationUtility.CalculateTransformPath(source.itemText.transform, source.template);
            var template = Object.Instantiate(source.template, dropdown.transform, false);
            template.name = "Template";
            template.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 592);
            template.gameObject.SetActive(false);
            dropdown.template = template;
            dropdown.itemText = template.Find(labelPath).GetComponent<TMP_Text>();
            dropdown.itemText.color = Navy;
            dropdown.itemText.fontSize = dropdown.itemText.fontSizeMax = 26;
            dropdown.itemText.fontSizeMin = 20; dropdown.itemText.enableAutoSizing = true;
            dropdown.itemText.rectTransform.offsetMin = new Vector2(44, 0);
            dropdown.itemText.rectTransform.offsetMax = new Vector2(-12, 0);
            var item = dropdown.itemText.GetComponentInParent<UnityEngine.UI.Toggle>(true);
            var itemRect = (RectTransform)item.transform;
            itemRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 48);
            var element = item.GetComponent<UnityEngine.UI.LayoutElement>();
            if (element == null) element = item.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
            element.minHeight = element.preferredHeight = 48;
            var check = item.transform.Find("Item Checkmark") as RectTransform;
            if (check != null) { check.sizeDelta = new Vector2(28, 28); check.anchoredPosition = new Vector2(8, 0); }
            var scroll = template.GetComponent<UnityEngine.UI.ScrollRect>();
            scroll.vertical = scroll.horizontal = false;
            if (scroll.verticalScrollbar != null) scroll.verticalScrollbar.gameObject.SetActive(false);
            scroll.verticalScrollbar = null; scroll.horizontalScrollbar = null;
            scroll.viewport.offsetMin = scroll.viewport.offsetMax = Vector2.zero;
            var layout = scroll.content.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
            if (layout != null) { layout.spacing = 0; layout.padding = new RectOffset(); layout.childControlWidth = layout.childControlHeight = true; layout.childForceExpandWidth = true; layout.childForceExpandHeight = false; }
            if (oldTemplate != null) Object.DestroyImmediate(oldTemplate.gameObject);
            dropdown.RefreshShownValue();
            EditorUtility.SetDirty(dropdown);
        }

        private static void Footer(UnityEngine.UI.Button button, UnityEngine.UI.Button source, bool confirm)
        {
            var rect = (RectTransform)button.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(confirm ? 1 : 0, 0);
            rect.anchoredPosition = new Vector2(confirm ? -24 : 24, 0);
            rect.sizeDelta = new Vector2(510, 74);
            var image = button.GetComponent<UnityEngine.UI.Image>();
            image.color = confirm ? new Color32(25, 134, 109, 255) : Navy;
            var label = button.GetComponentInChildren<TMP_Text>(true);
            label.font = source.transform.Find("Text (TMP)").GetComponent<TMP_Text>().font;
            label.color = Color.white; label.fontSize = label.fontSizeMax = 40; label.fontSizeMin = 26; label.enableAutoSizing = true;
            label.text = confirm ? "CONFIRM SUBS" : "BACK";
            label.alignment = TextAlignmentOptions.Center;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color32(210, 242, 234, 255);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = new Color32(165, 213, 202, 255);
            colors.disabledColor = new Color32(115, 126, 133, 160);
            button.colors = colors;
            button.transition = UnityEngine.UI.Selectable.Transition.ColorTint;
        }

        public static void AuthorLineupIcons()
        {
            const string folder = "Assets/Resources/Sprite Assets";
            Directory.CreateDirectory(folder);
            const string path = folder + "/LineupStatusIcons.asset";
            var asset = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(path);
            if (asset != null)
            {
                var existingImporter = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(asset.spriteSheet));
                if (existingImporter.npotScale != TextureImporterNPOTScale.None)
                {
                    existingImporter.npotScale = TextureImporterNPOTScale.None;
                    existingImporter.SaveAndReimport();
                }
                return;
            }
            // Simple, crisp UI symbols are authored once as an atlas, never generated in Play Mode.
            var atlas = new Texture2D(192, 64, TextureFormat.RGBA32, false);
            var pixels = new Color32[192 * 64];
            var colors = new[] { new Color32(34, 190, 102, 255), new Color32(237, 62, 70, 255), new Color32(237, 62, 70, 255) };
            for (int icon = 0; icon < 3; icon++) for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
            {
                int yy = icon == 1 ? 63 - y : y;
                bool filled = icon == 2
                    ? x >= 23 && x < 41 && y >= 8 && y < 56 || y >= 23 && y < 41 && x >= 8 && x < 56
                    : yy >= 8 && yy < 35 && x >= 24 && x < 40 || yy >= 30 && yy < 56 && Mathf.Abs(x - 31.5f) <= 56 - yy;
                if (filled) pixels[y * 192 + icon * 64 + x] = colors[icon];
            }
            atlas.SetPixels32(pixels); atlas.Apply();
            string png = folder + "/LineupStatusIcons.png";
            File.WriteAllBytes(png, atlas.EncodeToPNG()); Object.DestroyImmediate(atlas);
            AssetDatabase.ImportAsset(png);
            var importer = (TextureImporter)AssetImporter.GetAtPath(png);
            importer.textureType = TextureImporterType.Default; importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.mipmapEnabled = false; importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(png);
            asset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
            asset.name = "LineupStatusIcons";
            var serializedAsset = new SerializedObject(asset);
            serializedAsset.FindProperty("m_Version").stringValue = "1.1.0";
            serializedAsset.ApplyModifiedPropertiesWithoutUndo();
            asset.hashCode = TMP_TextUtilities.GetSimpleHashCode(asset.name); asset.spriteSheet = texture;
            asset.spriteGlyphTable.Clear(); asset.spriteCharacterTable.Clear();
            string[] names = { "SubOn", "SubOff", "Injury" };
            for (int i = 0; i < 3; i++)
            {
                var glyph = new TMP_SpriteGlyph { index = (uint)i, metrics = new GlyphMetrics(64, 64, 0, 54, 68), glyphRect = new GlyphRect(i * 64, 0, 64, 64), scale = 1 };
                asset.spriteGlyphTable.Add(glyph);
                asset.spriteCharacterTable.Add(new TMP_SpriteCharacter(0xFFFE, asset, glyph) { name = names[i], scale = 1 });
            }
            AssetDatabase.CreateAsset(asset, path);
            var material = new Material(Shader.Find("TextMeshPro/Sprite")) { name = "LineupStatusIcons Material", mainTexture = texture };
            AssetDatabase.AddObjectToAsset(material, asset); asset.material = material;
            asset.UpdateLookupTables(); EditorUtility.SetDirty(asset); AssetDatabase.SaveAssets();
        }

        public static void AuthorHoverReadability()
        {
            var ui = Object.FindAnyObjectByType<MatchStatsUI>();
            var input = Object.FindAnyObjectByType<GameInputManager>();
            var stats = new SerializedObject(ui);
            var cards = (RectTransform)stats.FindProperty("hoverCardsRoot").objectReferenceValue;
            cards.localScale = Vector3.one * 1.5f;
            stats.FindProperty("lineupHoverInput").objectReferenceValue = input;
            stats.ApplyModifiedPropertiesWithoutUndo();
            var serializedInput = new SerializedObject(input);
            var canvas = serializedInput.FindProperty("hoverNameCanvas").objectReferenceValue as Canvas;
            if (canvas == null)
            {
                var root = new GameObject("HoveredTokenNameCanvas", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler));
                root.layer = LayerMask.NameToLayer("UI");
                canvas = root.GetComponent<Canvas>();
            }
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500; canvas.pixelPerfect = true;
            var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ConstantPixelSize; scaler.scaleFactor = 1;
            var found = canvas.transform.Find("HoveredTokenNameLabel");
            var label = found != null ? found.GetComponent<TextMeshProUGUI>()
                : new GameObject("HoveredTokenNameLabel", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            label.transform.SetParent(canvas.transform, false); label.gameObject.layer = canvas.gameObject.layer;
            var rect = label.rectTransform;
            rect.sizeDelta = new Vector2(156, 31.2f); rect.pivot = new Vector2(.5f, 0);
            label.font = TMP_Settings.defaultFontAsset; label.text = string.Empty;
            label.enableAutoSizing = false; label.fontSize = 18.2f; label.alignment = TextAlignmentOptions.Bottom;
            label.textWrappingMode = TextWrappingModes.NoWrap; label.overflowMode = TextOverflowModes.Overflow;
            label.outlineWidth = .1f; label.outlineColor = new Color(0, 0, 0, .85f);
            label.color = Color.white; label.raycastTarget = false;
            serializedInput.FindProperty("hoverNameCanvas").objectReferenceValue = canvas;
            serializedInput.FindProperty("hoverNameRect").objectReferenceValue = rect;
            serializedInput.FindProperty("hoverNameLabel").objectReferenceValue = label;
            serializedInput.FindProperty("hoverNameScale").floatValue = 1.3f;
            serializedInput.ApplyModifiedPropertiesWithoutUndo();
            canvas.gameObject.SetActive(false);
            EditorUtility.SetDirty(ui); EditorUtility.SetDirty(input);
        }
    }
}
