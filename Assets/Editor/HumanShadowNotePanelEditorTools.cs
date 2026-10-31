using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CounterAttack.Editor
{
    public static class HumanShadowNotePanelEditorTools
    {
        private const string RoomScenePath = "Assets/Scenes/Room.unity";
        private static readonly Color PanelColor = new(0.035f, 0.09f, 0.19f, 0.98f);
        private static readonly Color FieldColor = new(0.94f, 0.95f, 0.98f, 1f);
        private static readonly Color DarkTextColor = new(0.07f, 0.09f, 0.13f, 1f);

        [MenuItem("CounterAttack/Room/Ensure Human Shadow Note Panel")]
        public static void EnsureHumanShadowNotePanel()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Stop Play Mode before authoring the Human Shadow note panel.");
                return;
            }

            if (EditorSceneManager.GetActiveScene().path != RoomScenePath)
            {
                EditorSceneManager.OpenScene(RoomScenePath);
            }

            PauseMenuManager manager = Object.FindAnyObjectByType<PauseMenuManager>();
            if (manager == null || manager.pausePanel == null || manager.resumeButton == null)
            {
                Debug.LogError("Room PauseMenuManager, PausePanel, or ResumeButton is missing.");
                return;
            }

            TMP_FontAsset font = manager.resumeButton.GetComponentInChildren<TMP_Text>(true)?.font
                ?? TMP_Settings.defaultFontAsset;
            Button openButton = EnsureOpenButton(manager);
            GameObject panel = EnsurePanel(manager, font);

            SerializedObject serializedManager = new(manager);
            Assign(serializedManager, "addTacticalNoteButton", openButton);
            Assign(serializedManager, "tacticalNoteOverlay", panel);
            Assign(serializedManager, "tacticalNoteInput", panel.transform.Find("TacticalNoteInput")?.GetComponent<TMP_InputField>());
            Assign(serializedManager, "tacticalNoteDecisionText", panel.transform.Find("DecisionSummaryText")?.GetComponent<TextMeshProUGUI>());
            Assign(serializedManager, "tacticalNoteStatusText", panel.transform.Find("TacticalNoteStatusText")?.GetComponent<TextMeshProUGUI>());
            Assign(serializedManager, "saveTacticalNoteButton", panel.transform.Find("Buttons/SaveTacticalNoteButton")?.GetComponent<Button>());
            Assign(serializedManager, "cancelTacticalNoteButton", panel.transform.Find("Buttons/CancelTacticalNoteButton")?.GetComponent<Button>());
            serializedManager.ApplyModifiedPropertiesWithoutUndo();

            panel.SetActive(false);
            EditorUtility.SetDirty(manager);
            SetDirtyRecursive(openButton.gameObject);
            SetDirtyRecursive(panel);
            EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
            EditorSceneManager.SaveScene(manager.gameObject.scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Human Shadow note controls are authored in the Room scene and assigned.");
        }

        private static Button EnsureOpenButton(PauseMenuManager manager)
        {
            Button existing = FindChild<Button>(manager.pausePanel.transform, "AddTacticalNoteButton");
            Button button = existing != null
                ? existing
                : Object.Instantiate(manager.resumeButton, manager.resumeButton.transform.parent);
            button.name = "AddTacticalNoteButton";
            button.onClick.RemoveAllListeners();
            button.transform.SetSiblingIndex(manager.resumeButton.transform.GetSiblingIndex() + 1);
            SetButtonText(button, "Add Tactical Note");
            return button;
        }

        private static GameObject EnsurePanel(PauseMenuManager manager, TMP_FontAsset font)
        {
            Transform parent = manager.pausePanel.transform.parent != null
                ? manager.pausePanel.transform.parent
                : manager.transform;
            Transform existing = parent.Find("HumanShadowNotePanel");
            GameObject panel = existing != null
                ? existing.gameObject
                : new GameObject(
                    "HumanShadowNotePanel",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(VerticalLayoutGroup));
            panel.transform.SetParent(parent, false);
            SetUiLayer(panel);

            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(900f, 780f);
            panel.GetComponent<Image>().color = PanelColor;

            VerticalLayoutGroup layout = panel.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(42, 42, 28, 28);
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            EnsureText(panel.transform, "Title", "Tactical Note", 34f, 52f, Color.white, font);
            EnsureText(
                panel.transform,
                "DecisionSummaryText",
                "The latest five decisions will appear here when Add Tactical Note is pressed.",
                18f,
                176f,
                new Color(0.76f, 0.84f, 0.96f, 1f),
                font);
            EnsureInput(panel.transform, font);
            EnsureText(
                panel.transform,
                "TacticalNoteStatusText",
                "Reference a decision number and explain your tactical observation.",
                18f,
                42f,
                new Color(0.7f, 0.74f, 0.82f, 1f),
                font);
            EnsureButtons(panel.transform, manager.resumeButton);
            return panel;
        }

        private static void EnsureInput(Transform parent, TMP_FontAsset font)
        {
            Transform existing = parent.Find("TacticalNoteInput");
            GameObject root = existing != null
                ? existing.gameObject
                : new GameObject(
                    "TacticalNoteInput",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(TMP_InputField));
            root.transform.SetParent(parent, false);
            SetUiLayer(root);
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(800f, 210f);
            root.GetComponent<Image>().color = FieldColor;

            RectTransform viewport = EnsureRect(root.transform, "Text Area");
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(18f, 12f);
            viewport.offsetMax = new Vector2(-18f, -12f);

            TextMeshProUGUI placeholder = EnsureText(
                viewport,
                "Placeholder",
                "Example: #123 — this move left the striker unmarked.",
                19f,
                0f,
                new Color(0.35f, 0.39f, 0.46f, 0.8f),
                font);
            Stretch(placeholder.rectTransform);
            placeholder.fontStyle = FontStyles.Italic;

            TextMeshProUGUI text = EnsureText(viewport, "Text", string.Empty, 20f, 0f, DarkTextColor, font);
            Stretch(text.rectTransform);

            TMP_InputField input = root.GetComponent<TMP_InputField>();
            input.textViewport = viewport;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.lineType = TMP_InputField.LineType.MultiLineNewline;
            input.characterLimit = 600;
            input.targetGraphic = root.GetComponent<Image>();
        }

        private static void EnsureButtons(Transform parent, Button template)
        {
            RectTransform row = EnsureRect(parent, "Buttons");
            row.sizeDelta = new Vector2(800f, 58f);
            HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>()
                ?? row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 24f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            EnsureButton(row, template, "SaveTacticalNoteButton", "Save Note & Resume");
            EnsureButton(row, template, "CancelTacticalNoteButton", "Back");
        }

        private static Button EnsureButton(Transform parent, Button template, string name, string label)
        {
            Button button = FindChild<Button>(parent, name);
            if (button == null)
            {
                button = Object.Instantiate(template, parent);
                button.name = name;
            }

            button.onClick.RemoveAllListeners();
            button.GetComponent<RectTransform>().sizeDelta = new Vector2(330f, 56f);
            SetButtonText(button, label);
            return button;
        }

        private static TextMeshProUGUI EnsureText(
            Transform parent,
            string name,
            string value,
            float fontSize,
            float height,
            Color color,
            TMP_FontAsset font)
        {
            TextMeshProUGUI text = FindChild<TextMeshProUGUI>(parent, name);
            if (text == null)
            {
                GameObject textObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                textObject.transform.SetParent(parent, false);
                SetUiLayer(textObject);
                text = textObject.GetComponent<TextMeshProUGUI>();
            }

            text.text = value;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.Normal;
            if (font != null)
            {
                text.font = font;
            }

            if (height > 0f)
            {
                text.rectTransform.sizeDelta = new Vector2(800f, height);
            }
            return text;
        }

        private static RectTransform EnsureRect(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            GameObject gameObject = existing != null
                ? existing.gameObject
                : new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            SetUiLayer(gameObject);
            return gameObject.GetComponent<RectTransform>();
        }

        private static T FindChild<T>(Transform parent, string name) where T : Component
        {
            Transform child = parent != null ? parent.Find(name) : null;
            return child != null ? child.GetComponent<T>() : null;
        }

        private static void SetButtonText(Button button, string label)
        {
            TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
            {
                text.text = label;
            }
        }

        private static void Assign(SerializedObject target, string propertyName, Object value)
        {
            SerializedProperty property = target.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
            }
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetUiLayer(GameObject gameObject)
        {
            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer >= 0)
            {
                gameObject.layer = uiLayer;
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
    }
}
