#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CounterAttack.Editor
{
    public static class CreateNewGameSceneEditorTools
    {
        private const string ScenePath = "Assets/Scenes/CreateNewHSGameScene.unity";
        private const float PanelHorizontalPadding = 16f;
        private const float PairGap = 12f;
        private const float DropdownPreviewGap = 8f;
        private const float PreviewSize = 60f;
        private const float DropdownTemplateHeight = 220f;
        private const float SimilarityY = -430f;
        private const float SimilarityHeight = 24f;
        private const float ValidationY = -456f;
        private const float ValidationHeight = 26f;

        [MenuItem("CounterAttack/Create New Game/Setup Kit Preview UI")]
        public static void SetupKitPreviewUi()
        {
            Scene targetScene = SceneManager.GetSceneByPath(ScenePath);
            bool openedAdditively = !targetScene.isLoaded;

            if (openedAdditively)
            {
                targetScene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            }

            try
            {
                CreateNewGameManager manager = FindComponentInScene<CreateNewGameManager>(targetScene);
                if (manager == null)
                {
                    throw new UnityException($"Could not find {nameof(CreateNewGameManager)} in {ScenePath}.");
                }

                RectTransform homeDropdown = manager.homeKitDropdown.GetComponent<RectTransform>();
                RectTransform awayDropdown = manager.awayKitDropdown.GetComponent<RectTransform>();
                RectTransform commonParent = homeDropdown.parent as RectTransform;
                TMP_FontAsset fontAsset = manager.homeKitDropdown.captionText != null
                    ? manager.homeKitDropdown.captionText.font
                    : TMP_Settings.defaultFontAsset;

                LayoutKitRow(homeDropdown, awayDropdown, commonParent);
                ResizeDropdownTemplate(manager.homeKitDropdown);
                ResizeDropdownTemplate(manager.awayKitDropdown);

                PreviewWidgetRefs homePreview = EnsurePreviewWidget(commonParent, "Home Kit Preview", GetPreviewPosition(homeDropdown, commonParent), fontAsset);
                PreviewWidgetRefs awayPreview = EnsurePreviewWidget(commonParent, "Away Kit Preview", GetPreviewPosition(awayDropdown, commonParent), fontAsset);
                TMP_Text similarityText = EnsureSimilarityLabel(commonParent, fontAsset, commonParent.sizeDelta.x);
                TMP_Text validationText = EnsureValidationLabel(commonParent, fontAsset, commonParent.sizeDelta.x);

                manager.homeKitPreviewImage = homePreview.Image;
                manager.homeKitPreviewNumberText = homePreview.NumberText;
                manager.awayKitPreviewImage = awayPreview.Image;
                manager.awayKitPreviewNumberText = awayPreview.NumberText;
                manager.kitSimilarityText = similarityText;
                manager.kitValidationText = validationText;

                EditorUtility.SetDirty(manager);
                EditorSceneManager.MarkSceneDirty(targetScene);
                EditorSceneManager.SaveScene(targetScene);
            }
            finally
            {
                if (openedAdditively && targetScene.isLoaded)
                {
                    EditorSceneManager.CloseScene(targetScene, true);
                }
            }
        }

        private static void LayoutKitRow(RectTransform homeDropdown, RectTransform awayDropdown, RectTransform panel)
        {
            float panelWidth = panel.sizeDelta.x;
            float pairWidth = (panelWidth - (PanelHorizontalPadding * 2f) - PairGap) / 2f;
            float dropdownWidth = pairWidth - DropdownPreviewGap - PreviewSize;

            float homePairStartX = PanelHorizontalPadding;
            float awayPairStartX = PanelHorizontalPadding + pairWidth + PairGap;
            float rowY = homeDropdown.anchoredPosition.y;

            homeDropdown.sizeDelta = new Vector2(dropdownWidth, homeDropdown.sizeDelta.y);
            homeDropdown.anchoredPosition = new Vector2(homePairStartX + (dropdownWidth / 2f), rowY);

            awayDropdown.sizeDelta = new Vector2(dropdownWidth, awayDropdown.sizeDelta.y);
            awayDropdown.anchoredPosition = new Vector2(awayPairStartX + (dropdownWidth / 2f), rowY);

            EditorUtility.SetDirty(homeDropdown);
            EditorUtility.SetDirty(awayDropdown);
        }

        private static Vector2 GetPreviewPosition(RectTransform dropdownTransform, RectTransform panel)
        {
            float panelWidth = panel.sizeDelta.x;
            float pairWidth = (panelWidth - (PanelHorizontalPadding * 2f) - PairGap) / 2f;
            float dropdownWidth = pairWidth - DropdownPreviewGap - PreviewSize;
            float previewCenterX = dropdownTransform.anchoredPosition.x + (dropdownWidth / 2f) + DropdownPreviewGap + (PreviewSize / 2f);
            return new Vector2(previewCenterX, dropdownTransform.anchoredPosition.y);
        }

        private static void ResizeDropdownTemplate(TMP_Dropdown dropdown)
        {
            if (dropdown == null || dropdown.template == null)
            {
                return;
            }

            RectTransform templateRect = dropdown.template;
            templateRect.sizeDelta = new Vector2(templateRect.sizeDelta.x, DropdownTemplateHeight);

            ScrollRect scrollRect = dropdown.template.GetComponent<ScrollRect>();
            if (scrollRect != null)
            {
                scrollRect.vertical = true;
                scrollRect.scrollSensitivity = 20f;
            }

            EditorUtility.SetDirty(templateRect);
            if (scrollRect != null)
            {
                EditorUtility.SetDirty(scrollRect);
            }
        }

        private static PreviewWidgetRefs EnsurePreviewWidget(RectTransform parent, string widgetName, Vector2 anchoredPosition, TMP_FontAsset fontAsset)
        {
            RectTransform root = EnsureRectTransform(parent, widgetName);
            root.anchorMin = new Vector2(0f, 1f);
            root.anchorMax = new Vector2(0f, 1f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.anchoredPosition = anchoredPosition;
            root.sizeDelta = new Vector2(PreviewSize, PreviewSize);

            RawImage image = EnsureRawImage(root, "Face");
            RectTransform imageRect = image.rectTransform;
            imageRect.anchorMin = Vector2.zero;
            imageRect.anchorMax = Vector2.one;
            imageRect.offsetMin = Vector2.zero;
            imageRect.offsetMax = Vector2.zero;
            image.color = Color.white;
            image.raycastTarget = false;

            TextMeshProUGUI numberText = EnsurePreviewNumber(root, fontAsset);
            numberText.rectTransform.anchorMin = Vector2.zero;
            numberText.rectTransform.anchorMax = Vector2.one;
            numberText.rectTransform.offsetMin = Vector2.zero;
            numberText.rectTransform.offsetMax = Vector2.zero;
            numberText.alignment = TextAlignmentOptions.Center;
            numberText.textWrappingMode = TextWrappingModes.NoWrap;
            numberText.raycastTarget = false;
            numberText.text = string.Empty;

            return new PreviewWidgetRefs
            {
                Root = root,
                Image = image,
                NumberText = numberText
            };
        }

        private static TMP_Text EnsureValidationLabel(RectTransform parent, TMP_FontAsset fontAsset, float panelWidth)
        {
            RectTransform root = EnsureRectTransform(parent, "Kit Clash Validation");
            root.anchorMin = new Vector2(0f, 1f);
            root.anchorMax = new Vector2(0f, 1f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.anchoredPosition = new Vector2(panelWidth / 2f, ValidationY);
            root.sizeDelta = new Vector2(panelWidth - (PanelHorizontalPadding * 2f), ValidationHeight);

            TextMeshProUGUI text = root.GetComponent<TextMeshProUGUI>();
            if (text == null)
            {
                text = root.gameObject.AddComponent<TextMeshProUGUI>();
            }

            text.font = fontAsset;
            text.fontSize = 20f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.68f, 0.12f, 0.12f, 1f);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            text.text = string.Empty;
            root.gameObject.SetActive(false);
            return text;
        }

        private static TMP_Text EnsureSimilarityLabel(RectTransform parent, TMP_FontAsset fontAsset, float panelWidth)
        {
            RectTransform root = EnsureRectTransform(parent, "Kit Similarity Index");
            root.anchorMin = new Vector2(0f, 1f);
            root.anchorMax = new Vector2(0f, 1f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.anchoredPosition = new Vector2(panelWidth / 2f, SimilarityY);
            root.sizeDelta = new Vector2(panelWidth - (PanelHorizontalPadding * 2f), SimilarityHeight);

            TextMeshProUGUI text = root.GetComponent<TextMeshProUGUI>();
            if (text == null)
            {
                text = root.gameObject.AddComponent<TextMeshProUGUI>();
            }

            text.font = fontAsset;
            text.fontSize = 20f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.16f, 0.16f, 0.16f, 1f);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            text.text = "Kit similarity index: 0%";
            root.gameObject.SetActive(true);
            return text;
        }

        private static RawImage EnsureRawImage(RectTransform parent, string childName)
        {
            RectTransform child = EnsureRectTransform(parent, childName);
            RawImage image = child.GetComponent<RawImage>();
            if (image == null)
            {
                image = child.gameObject.AddComponent<RawImage>();
            }
            return image;
        }

        private static TextMeshProUGUI EnsurePreviewNumber(RectTransform parent, TMP_FontAsset fontAsset)
        {
            RectTransform child = EnsureRectTransform(parent, "Number");
            TextMeshProUGUI text = child.GetComponent<TextMeshProUGUI>();
            if (text == null)
            {
                text = child.gameObject.AddComponent<TextMeshProUGUI>();
            }

            text.font = fontAsset;
            return text;
        }

        private static RectTransform EnsureRectTransform(RectTransform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                return existing as RectTransform;
            }

            GameObject child = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(child, $"Create {name}");
            RectTransform rectTransform = child.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            return rectTransform;
        }

        private static T FindComponentInScene<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T match = root.GetComponentInChildren<T>(true);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private sealed class PreviewWidgetRefs
        {
            public RectTransform Root;
            public RawImage Image;
            public TextMeshProUGUI NumberText;
        }
    }
}
#endif
