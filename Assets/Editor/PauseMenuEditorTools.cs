using TMPro;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CounterAttack.Editor
{
    public static class PauseMenuEditorTools
    {
        private const float SettingsRowWidth = 620f;
        private const float SettingsRowHeight = 42f;
        private const float SettingsLabelWidth = 210f;
        private const float SettingsDropdownWidth = 360f;
        private const float SettingsControlHeight = 36f;
        private const float SettingsRowGap = 12f;
        private const float DropdownTemplateHeight = 220f;

        [MenuItem("CounterAttack/Room/Ensure Pause Menu Substitutions Button")]
        public static void EnsureSubstitutionsButton()
        {
            PauseMenuManager manager = Object.FindAnyObjectByType<PauseMenuManager>();
            if (manager == null)
            {
                Debug.LogError("PauseMenuManager not found in the open scene.");
                return;
            }

            if (manager.pausePanel == null || manager.resumeButton == null)
            {
                Debug.LogError("PauseMenuManager must have pausePanel and resumeButton assigned before adding the Substitutions button.");
                return;
            }

            Button button = manager.substitutionsButton != null
                ? manager.substitutionsButton
                : FindExistingButton(manager.pausePanel.transform);

            if (button == null)
            {
                button = Object.Instantiate(manager.resumeButton, manager.resumeButton.transform.parent);
                button.name = "SubstitutionsButton";
                button.transform.SetSiblingIndex(manager.resumeButton.transform.GetSiblingIndex() + 1);
                RectTransform rect = button.transform as RectTransform;
                RectTransform resumeRect = manager.resumeButton.transform as RectTransform;
                if (rect != null && resumeRect != null)
                {
                    rect.anchoredPosition = resumeRect.anchoredPosition + new Vector2(0f, -60f);
                    rect.sizeDelta = resumeRect.sizeDelta;
                }
            }

            SetButtonText(button, "Substitutions");
            button.onClick.RemoveAllListeners();

            SerializedObject serializedManager = new(manager);
            serializedManager.FindProperty("substitutionsButton").objectReferenceValue = button;
            serializedManager.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(button);
            EditorUtility.SetDirty(manager);
            EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
            EditorSceneManager.SaveScene(manager.gameObject.scene);
            Debug.Log("Pause menu Substitutions button is present and assigned.");
        }

        [MenuItem("CounterAttack/Room/Ensure Pause Menu Edit Settings Panel")]
        public static void EnsureEditSettingsPanel()
        {
            PauseMenuManager manager = Object.FindAnyObjectByType<PauseMenuManager>();
            if (manager == null)
            {
                Debug.LogError("PauseMenuManager not found in the open scene.");
                return;
            }

            if (manager.pausePanel == null || manager.resumeButton == null)
            {
                Debug.LogError("PauseMenuManager must have pausePanel and resumeButton assigned before adding the Edit Settings panel.");
                return;
            }

            Transform parent = manager.pausePanel.transform.parent != null
                ? manager.pausePanel.transform.parent
                : manager.transform;

            GameObject panel = manager.editSettingsPanel != null
                ? manager.editSettingsPanel
                : FindChildObject(parent, "EditSettingsPanel");
            if (panel == null)
            {
                panel = CreatePanel(parent);
            }

            TMP_FontAsset fontAsset = ResolveFontAsset(manager);
            TMP_Dropdown tiebreakerDropdown = EnsureSettingsDropdownRow(panel.transform, "EditTiebreakerDropdown", "Tiebreaker", 0, fontAsset);
            TMP_Dropdown playerAssistanceDropdown = EnsureSettingsDropdownRow(panel.transform, "EditPlayerAssistanceDropdown", "Player Assistance", 1, fontAsset);
            TMP_Dropdown weatherDropdown = EnsureSettingsDropdownRow(panel.transform, "EditWeatherDropdown", "Weather Conditions", 2, fontAsset);
            TMP_Dropdown ballColorDropdown = EnsureSettingsDropdownRow(panel.transform, "EditBallColorDropdown", "Ball Color", 3, fontAsset);
            TMP_Dropdown homeKitDropdown = EnsureSettingsDropdownRow(panel.transform, "EditHomeKitDropdown", "Home Kit", 4, fontAsset);
            TMP_Dropdown awayKitDropdown = EnsureSettingsDropdownRow(panel.transform, "EditAwayKitDropdown", "Away Kit", 5, fontAsset);
            TMP_Dropdown homeGKKitDropdown = EnsureSettingsDropdownRow(panel.transform, "EditHomeGKKitDropdown", "Home GK Kit", 6, fontAsset);
            TMP_Dropdown awayGKKitDropdown = EnsureSettingsDropdownRow(panel.transform, "EditAwayGKKitDropdown", "Away GK Kit", 7, fontAsset);

            Button captureButton = manager.captureLogFileButton != null
                ? manager.captureLogFileButton
                : FindChildComponent<Button>(panel.transform, "CaptureLogFileButton");
            if (captureButton == null)
            {
                captureButton = CreateButton(manager.resumeButton, panel.transform, "CaptureLogFileButton", "Capture Logfile");
            }

            Button backButton = manager.editSettingsBackButton != null
                ? manager.editSettingsBackButton
                : FindChildComponent<Button>(panel.transform, "EditSettingsBackButton");
            if (backButton == null)
            {
                backButton = CreateButton(manager.resumeButton, panel.transform, "EditSettingsBackButton", "Back");
            }

            TextMeshProUGUI statusText = manager.logFileStatusText != null
                ? manager.logFileStatusText
                : FindChildComponent<TextMeshProUGUI>(panel.transform, "LogFileStatusText");
            if (statusText == null)
            {
                statusText = CreateStatusText(panel.transform);
            }

            SetButtonText(captureButton, "Capture Logfile");
            SetButtonText(backButton, "Back");
            captureButton.onClick.RemoveAllListeners();
            backButton.onClick.RemoveAllListeners();
            statusText.text = string.Empty;
            captureButton.transform.SetSiblingIndex(8);
            backButton.transform.SetSiblingIndex(9);
            statusText.transform.SetSiblingIndex(10);
            panel.SetActive(false);

            SerializedObject serializedManager = new(manager);
            serializedManager.FindProperty("editSettingsPanel").objectReferenceValue = panel;
            serializedManager.FindProperty("captureLogFileButton").objectReferenceValue = captureButton;
            serializedManager.FindProperty("editSettingsBackButton").objectReferenceValue = backButton;
            serializedManager.FindProperty("logFileStatusText").objectReferenceValue = statusText;
            serializedManager.FindProperty("editTiebreakerDropdown").objectReferenceValue = tiebreakerDropdown;
            serializedManager.FindProperty("editPlayerAssistanceDropdown").objectReferenceValue = playerAssistanceDropdown;
            serializedManager.FindProperty("editWeatherDropdown").objectReferenceValue = weatherDropdown;
            serializedManager.FindProperty("editBallColorDropdown").objectReferenceValue = ballColorDropdown;
            serializedManager.FindProperty("editHomeKitDropdown").objectReferenceValue = homeKitDropdown;
            serializedManager.FindProperty("editAwayKitDropdown").objectReferenceValue = awayKitDropdown;
            serializedManager.FindProperty("editHomeGKKitDropdown").objectReferenceValue = homeGKKitDropdown;
            serializedManager.FindProperty("editAwayGKKitDropdown").objectReferenceValue = awayGKKitDropdown;
            serializedManager.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(panel);
            SetDirtyRecursive(panel);
            EditorUtility.SetDirty(captureButton);
            EditorUtility.SetDirty(backButton);
            EditorUtility.SetDirty(statusText);
            EditorUtility.SetDirty(manager);
            EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
            EditorSceneManager.SaveScene(manager.gameObject.scene);
            Debug.Log("Pause menu Edit Settings panel is present and assigned.");
        }

        private static Button FindExistingButton(Transform pausePanel)
        {
            foreach (Button button in pausePanel.GetComponentsInChildren<Button>(true))
            {
                if (button.name == "SubstitutionsButton")
                {
                    return button;
                }
            }

            return null;
        }

        private static GameObject FindChildObject(Transform root, string childName)
        {
            if (root == null)
            {
                return null;
            }

            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == childName)
                {
                    return child.gameObject;
                }
            }

            return null;
        }

        private static T FindChildComponent<T>(Transform root, string childName) where T : Component
        {
            if (root == null)
            {
                return null;
            }

            foreach (T component in root.GetComponentsInChildren<T>(true))
            {
                if (component.name == childName)
                {
                    return component;
                }
            }

            return null;
        }

        private static GameObject CreatePanel(Transform parent)
        {
            GameObject panel = new("EditSettingsPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(VerticalLayoutGroup));
            panel.transform.SetParent(parent, false);

            RectTransform rect = panel.transform as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = Vector2.zero;
            }

            Image image = panel.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.55f);
            image.raycastTarget = true;

            VerticalLayoutGroup layout = panel.GetComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 30f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            return panel;
        }

        private static Button CreateButton(Button template, Transform parent, string buttonName, string label)
        {
            Button button = Object.Instantiate(template, parent);
            button.name = buttonName;
            button.onClick.RemoveAllListeners();

            RectTransform rect = button.transform as RectTransform;
            if (rect != null)
            {
                rect.sizeDelta = new Vector2(300f, 56f);
            }

            SetButtonText(button, label);
            return button;
        }

        private static TextMeshProUGUI CreateStatusText(Transform parent)
        {
            GameObject textObject = new("LogFileStatusText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);

            RectTransform rect = textObject.transform as RectTransform;
            if (rect != null)
            {
                rect.sizeDelta = new Vector2(1000f, 120f);
            }

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 20f;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            text.color = new Color(0.7f, 0.74f, 0.82f, 1f);
            if (TMP_Settings.defaultFontAsset != null)
            {
                text.font = TMP_Settings.defaultFontAsset;
            }

            return text;
        }

        private static TMP_Dropdown EnsureSettingsDropdownRow(
            Transform panel,
            string dropdownName,
            string label,
            int siblingIndex,
            TMP_FontAsset fontAsset)
        {
            TMP_Dropdown dropdown = FindChildComponent<TMP_Dropdown>(panel, dropdownName);
            if (dropdown == null)
            {
                dropdown = CreateSettingsDropdownRow(panel, dropdownName, label, fontAsset);
            }

            Transform row = dropdown.transform.parent != null && dropdown.transform.parent != panel
                ? dropdown.transform.parent
                : dropdown.transform;
            row.SetSiblingIndex(siblingIndex);

            ConfigureSettingsRow(row);
            ConfigureSettingsLabel(row, label, fontAsset);
            ConfigureSettingsDropdown(dropdown, fontAsset);
            return dropdown;
        }

        private static TMP_Dropdown CreateSettingsDropdownRow(
            Transform parent,
            string dropdownName,
            string label,
            TMP_FontAsset fontAsset)
        {
            GameObject row = new($"{dropdownName}Row", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            Undo.RegisterCreatedObjectUndo(row, $"Create {dropdownName} row");
            row.transform.SetParent(parent, false);
            SetLayerRecursively(row, LayerMask.NameToLayer("UI"));
            ConfigureSettingsRow(row.transform);

            TextMeshProUGUI labelText = CreateDropdownText(row.transform, $"{label} Label", TextAlignmentOptions.MidlineRight, Color.white, fontAsset);
            RectTransform labelRect = labelText.transform as RectTransform;
            if (labelRect != null)
            {
                labelRect.sizeDelta = new Vector2(SettingsLabelWidth, SettingsControlHeight);
            }

            TMP_Dropdown dropdown = CreateSettingsDropdown(row.transform, dropdownName, fontAsset);
            RectTransform dropdownRect = dropdown.transform as RectTransform;
            if (dropdownRect != null)
            {
                dropdownRect.sizeDelta = new Vector2(SettingsDropdownWidth, SettingsControlHeight);
            }

            return dropdown;
        }

        private static void ConfigureSettingsRow(Transform row)
        {
            RectTransform rowRect = row as RectTransform;
            if (rowRect != null)
            {
                rowRect.sizeDelta = new Vector2(SettingsRowWidth, SettingsRowHeight);
            }

            HorizontalLayoutGroup rowLayout = row.GetComponent<HorizontalLayoutGroup>();
            if (rowLayout == null)
            {
                rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            }

            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.spacing = SettingsRowGap;
            rowLayout.childControlWidth = false;
            rowLayout.childControlHeight = false;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;
            EditorUtility.SetDirty(row);
            EditorUtility.SetDirty(rowLayout);
        }

        private static void ConfigureSettingsLabel(Transform row, string label, TMP_FontAsset fontAsset)
        {
            TextMeshProUGUI labelText = null;
            foreach (TextMeshProUGUI text in row.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (text.name == $"{label} Label")
                {
                    labelText = text;
                    break;
                }
            }

            if (labelText == null)
            {
                labelText = CreateDropdownText(row, $"{label} Label", TextAlignmentOptions.MidlineRight, Color.white, fontAsset);
                labelText.transform.SetSiblingIndex(0);
            }

            labelText.text = label;
            labelText.fontSize = 20f;
            labelText.alignment = TextAlignmentOptions.MidlineRight;
            labelText.color = Color.white;
            if (fontAsset != null)
            {
                labelText.font = fontAsset;
            }

            RectTransform labelRect = labelText.transform as RectTransform;
            if (labelRect != null)
            {
                labelRect.sizeDelta = new Vector2(SettingsLabelWidth, SettingsControlHeight);
            }

            EditorUtility.SetDirty(labelText);
        }

        private static TMP_Dropdown CreateSettingsDropdown(Transform parent, string dropdownName, TMP_FontAsset fontAsset)
        {
            GameObject dropdownObject = new(dropdownName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_Dropdown));
            Undo.RegisterCreatedObjectUndo(dropdownObject, $"Create {dropdownName}");
            dropdownObject.transform.SetParent(parent, false);
            SetLayerRecursively(dropdownObject, LayerMask.NameToLayer("UI"));

            TMP_Dropdown dropdown = dropdownObject.GetComponent<TMP_Dropdown>();
            ConfigureSettingsDropdown(dropdown, fontAsset);
            return dropdown;
        }

        private static void ConfigureSettingsDropdown(TMP_Dropdown dropdown, TMP_FontAsset fontAsset)
        {
            Image dropdownImage = dropdown.GetComponent<Image>();
            if (dropdownImage == null)
            {
                dropdownImage = dropdown.gameObject.AddComponent<Image>();
            }

            dropdownImage.color = new Color(0.94f, 0.94f, 0.94f, 1f);
            dropdownImage.raycastTarget = true;

            TextMeshProUGUI caption = FindDirectChildComponent<TextMeshProUGUI>(dropdown.transform, "Label")
                ?? CreateDropdownText(dropdown.transform, "Label", TextAlignmentOptions.MidlineLeft, new Color(0.08f, 0.08f, 0.08f, 1f), fontAsset);
            RectTransform captionRect = caption.transform as RectTransform;
            if (captionRect != null)
            {
                captionRect.anchorMin = Vector2.zero;
                captionRect.anchorMax = Vector2.one;
                captionRect.offsetMin = new Vector2(12f, 2f);
                captionRect.offsetMax = new Vector2(-28f, -2f);
            }

            TextMeshProUGUI arrow = FindDirectChildComponent<TextMeshProUGUI>(dropdown.transform, "Arrow")
                ?? CreateDropdownText(dropdown.transform, "Arrow", TextAlignmentOptions.Center, new Color(0.08f, 0.08f, 0.08f, 1f), fontAsset);
            arrow.text = "v";
            RectTransform arrowRect = arrow.transform as RectTransform;
            if (arrowRect != null)
            {
                arrowRect.anchorMin = new Vector2(1f, 0f);
                arrowRect.anchorMax = new Vector2(1f, 1f);
                arrowRect.pivot = new Vector2(1f, 0.5f);
                arrowRect.sizeDelta = new Vector2(26f, 0f);
                arrowRect.anchoredPosition = Vector2.zero;
            }

            RectTransform template = dropdown.transform.Find("Template") as RectTransform;
            if (template == null)
            {
                template = CreateDropdownTemplate(dropdown.transform, fontAsset);
            }

            dropdown.captionText = caption;
            dropdown.template = template;
            dropdown.itemText = template.Find("Viewport/Content/Item/Item Label")?.GetComponent<TextMeshProUGUI>();
            dropdown.targetGraphic = dropdownImage;
            dropdown.options = new List<TMP_Dropdown.OptionData>();
            dropdown.RefreshShownValue();

            RectTransform dropdownRect = dropdown.transform as RectTransform;
            if (dropdownRect != null)
            {
                dropdownRect.sizeDelta = new Vector2(SettingsDropdownWidth, SettingsControlHeight);
            }

            EditorUtility.SetDirty(dropdown);
            EditorUtility.SetDirty(dropdownImage);
            EditorUtility.SetDirty(caption);
            EditorUtility.SetDirty(arrow);
            EditorUtility.SetDirty(template);
        }

        private static RectTransform CreateDropdownTemplate(Transform parent, TMP_FontAsset fontAsset)
        {
            GameObject templateObject = new("Template", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect));
            Undo.RegisterCreatedObjectUndo(templateObject, "Create dropdown template");
            templateObject.transform.SetParent(parent, false);
            templateObject.SetActive(false);
            SetLayerRecursively(templateObject, LayerMask.NameToLayer("UI"));

            RectTransform templateRect = templateObject.transform as RectTransform;
            if (templateRect != null)
            {
                templateRect.anchorMin = new Vector2(0f, 0f);
                templateRect.anchorMax = new Vector2(1f, 0f);
                templateRect.pivot = new Vector2(0.5f, 1f);
                templateRect.anchoredPosition = new Vector2(0f, 2f);
                templateRect.sizeDelta = new Vector2(0f, DropdownTemplateHeight);
            }

            Image templateImage = templateObject.GetComponent<Image>();
            templateImage.color = new Color(0.96f, 0.96f, 0.96f, 1f);

            GameObject viewportObject = new("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
            viewportObject.transform.SetParent(templateObject.transform, false);
            RectTransform viewportRect = viewportObject.transform as RectTransform;
            if (viewportRect != null)
            {
                viewportRect.anchorMin = Vector2.zero;
                viewportRect.anchorMax = Vector2.one;
                viewportRect.sizeDelta = Vector2.zero;
            }

            Image viewportImage = viewportObject.GetComponent<Image>();
            viewportImage.color = Color.white;
            viewportImage.raycastTarget = true;
            viewportObject.GetComponent<Mask>().showMaskGraphic = false;

            GameObject contentObject = new("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentObject.transform.SetParent(viewportObject.transform, false);
            RectTransform contentRect = contentObject.transform as RectTransform;
            if (contentRect != null)
            {
                contentRect.anchorMin = new Vector2(0f, 1f);
                contentRect.anchorMax = new Vector2(1f, 1f);
                contentRect.pivot = new Vector2(0.5f, 1f);
                contentRect.sizeDelta = new Vector2(0f, 28f);
            }

            VerticalLayoutGroup contentLayout = contentObject.GetComponent<VerticalLayoutGroup>();
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = false;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Toggle item = CreateDropdownItem(contentObject.transform, fontAsset);
            ScrollRect scrollRect = templateObject.GetComponent<ScrollRect>();
            scrollRect.content = contentRect;
            scrollRect.viewport = viewportRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 55f;

            EditorUtility.SetDirty(templateObject);
            EditorUtility.SetDirty(viewportObject);
            EditorUtility.SetDirty(contentObject);
            EditorUtility.SetDirty(item);
            return templateRect;
        }

        private static Toggle CreateDropdownItem(Transform parent, TMP_FontAsset fontAsset)
        {
            GameObject itemObject = new("Item", typeof(RectTransform), typeof(Toggle));
            Undo.RegisterCreatedObjectUndo(itemObject, "Create dropdown item");
            itemObject.transform.SetParent(parent, false);
            RectTransform itemRect = itemObject.transform as RectTransform;
            if (itemRect != null)
            {
                itemRect.anchorMin = new Vector2(0f, 0.5f);
                itemRect.anchorMax = new Vector2(1f, 0.5f);
                itemRect.sizeDelta = new Vector2(0f, 30f);
            }

            GameObject backgroundObject = new("Item Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            backgroundObject.transform.SetParent(itemObject.transform, false);
            RectTransform backgroundRect = backgroundObject.transform as RectTransform;
            if (backgroundRect != null)
            {
                backgroundRect.anchorMin = Vector2.zero;
                backgroundRect.anchorMax = Vector2.one;
                backgroundRect.sizeDelta = Vector2.zero;
            }

            Image backgroundImage = backgroundObject.GetComponent<Image>();
            backgroundImage.color = new Color(0.82f, 0.86f, 0.93f, 1f);

            TextMeshProUGUI itemLabel = CreateDropdownText(itemObject.transform, "Item Label", TextAlignmentOptions.MidlineLeft, new Color(0.08f, 0.08f, 0.08f, 1f), fontAsset);
            RectTransform labelRect = itemLabel.transform as RectTransform;
            if (labelRect != null)
            {
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(12f, 1f);
                labelRect.offsetMax = new Vector2(-8f, -1f);
            }

            Toggle toggle = itemObject.GetComponent<Toggle>();
            toggle.targetGraphic = backgroundImage;
            toggle.graphic = backgroundImage;
            return toggle;
        }

        private static TextMeshProUGUI CreateDropdownText(
            Transform parent,
            string name,
            TextAlignmentOptions alignment,
            Color color,
            TMP_FontAsset fontAsset)
        {
            GameObject textObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(textObject, $"Create {name}");
            textObject.transform.SetParent(parent, false);
            SetLayerRecursively(textObject, LayerMask.NameToLayer("UI"));

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.fontSize = 18f;
            text.alignment = alignment;
            text.color = color;
            if (fontAsset != null)
            {
                text.font = fontAsset;
            }

            return text;
        }

        private static T FindDirectChildComponent<T>(Transform parent, string childName) where T : Component
        {
            Transform child = parent.Find(childName);
            return child != null ? child.GetComponent<T>() : null;
        }

        private static TMP_FontAsset ResolveFontAsset(PauseMenuManager manager)
        {
            TMP_Text label = manager.resumeButton != null
                ? manager.resumeButton.GetComponentInChildren<TMP_Text>(true)
                : null;
            return label != null && label.font != null ? label.font : TMP_Settings.defaultFontAsset;
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            if (root == null || layer < 0)
            {
                return;
            }

            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.layer = layer;
            }
        }

        private static void SetDirtyRecursive(GameObject root)
        {
            foreach (Component component in root.GetComponentsInChildren<Component>(true))
            {
                if (component != null)
                {
                    EditorUtility.SetDirty(component);
                }
            }
        }

        private static void SetButtonText(Button button, string text)
        {
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = text;
            }
        }
    }
}
