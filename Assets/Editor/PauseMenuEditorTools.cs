using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CounterAttack.Editor
{
    public static class PauseMenuEditorTools
    {
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
            panel.SetActive(false);

            SerializedObject serializedManager = new(manager);
            serializedManager.FindProperty("editSettingsPanel").objectReferenceValue = panel;
            serializedManager.FindProperty("captureLogFileButton").objectReferenceValue = captureButton;
            serializedManager.FindProperty("editSettingsBackButton").objectReferenceValue = backButton;
            serializedManager.FindProperty("logFileStatusText").objectReferenceValue = statusText;
            serializedManager.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(panel);
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
