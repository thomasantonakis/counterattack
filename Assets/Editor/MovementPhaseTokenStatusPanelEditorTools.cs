#if UNITY_EDITOR
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CounterAttack.Editor
{
    public static class MovementPhaseTokenStatusPanelEditorTools
    {
        private const string RoomScenePath = "Assets/Scenes/Room.unity";
        private const string PanelName = "TokenStatusPanel";
        private const int MaxTokensPerSection = 11;
        private const float SlotSize = 46f;
        private const float FaceSize = 38f;
        private const float PlainNumberFontSize = 24f;
        private const float VerticalNumberFontSize = 21f;

        [MenuItem("Tools/Counter Attack/Ensure Movement Phase Token Status Panel In Room")]
        public static void EnsureSceneInstanceInEditMode()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("Cannot edit the Room scene while Unity is in Play Mode.");
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != RoomScenePath)
            {
                scene = EditorSceneManager.OpenScene(RoomScenePath, OpenSceneMode.Single);
            }

            Canvas canvas = ResolveMainCanvas();
            if (canvas == null)
            {
                Debug.LogError("Could not find the main Canvas in Room scene.");
                return;
            }

            MovementPhaseManager movementPhaseManager = Object.FindObjectsByType<MovementPhaseManager>(FindObjectsInactive.Include)
                .FirstOrDefault();
            HeaderManager headerManager = Object.FindObjectsByType<HeaderManager>(FindObjectsInactive.Include)
                .FirstOrDefault();
            if (movementPhaseManager == null)
            {
                Debug.LogError("Could not find MovementPhaseManager in Room scene.");
                return;
            }

            if (headerManager == null)
            {
                Debug.LogError("Could not find HeaderManager in Room scene.");
                return;
            }

            RectTransform panel = EnsurePanel(canvas.transform);
            MovementPhaseTokenStatusPanel panelController = panel.GetComponent<MovementPhaseTokenStatusPanel>();
            CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();

            EnsureTitle(panel);
            RectTransform stunnedSection = EnsureSection(panel, "StunnedSection", "Stunned Tokens");
            RectTransform jumpedSection = EnsureSection(panel, "JumpedSection", "Jumped Tokens");
            RectTransform stunnedNextSection = EnsureSection(panel, "StunnedNextSection", "Stunned Next MP");

            MovementPhaseMovedTokenSlotView[] stunnedSlots = EnsureSlots(stunnedSection, MaxTokensPerSection);
            MovementPhaseMovedTokenSlotView[] jumpedSlots = EnsureSlots(jumpedSection, MaxTokensPerSection);
            MovementPhaseMovedTokenSlotView[] stunnedNextSlots = EnsureSlots(stunnedNextSection, MaxTokensPerSection);

            SerializedObject serializedPanel = new(panelController);
            serializedPanel.FindProperty("movementPhaseManager").objectReferenceValue = movementPhaseManager;
            serializedPanel.FindProperty("headerManager").objectReferenceValue = headerManager;
            serializedPanel.FindProperty("canvasGroup").objectReferenceValue = canvasGroup;
            serializedPanel.FindProperty("stunnedSection").objectReferenceValue = stunnedSection;
            serializedPanel.FindProperty("jumpedSection").objectReferenceValue = jumpedSection;
            serializedPanel.FindProperty("stunnedNextSection").objectReferenceValue = stunnedNextSection;
            AssignSlotArray(serializedPanel.FindProperty("stunnedSlots"), stunnedSlots);
            AssignSlotArray(serializedPanel.FindProperty("jumpedSlots"), jumpedSlots);
            AssignSlotArray(serializedPanel.FindProperty("stunnedNextSlots"), stunnedNextSlots);
            serializedPanel.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(panelController);
            EditorUtility.SetDirty(panel);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("Ensured Movement Phase token-status panel as an edit-mode object in Room scene.");
        }

        private static RectTransform EnsurePanel(Transform canvasTransform)
        {
            Transform existing = canvasTransform.Find(PanelName);
            RectTransform panel = existing as RectTransform;
            if (panel == null)
            {
                GameObject panelObject = CreateRect(
                    PanelName,
                    canvasTransform,
                    typeof(CanvasGroup),
                    typeof(Image),
                    typeof(VerticalLayoutGroup),
                    typeof(MovementPhaseTokenStatusPanel));
                panel = panelObject.GetComponent<RectTransform>();
            }

            EnsureComponent<CanvasGroup>(panel.gameObject);
            EnsureComponent<Image>(panel.gameObject);
            EnsureComponent<VerticalLayoutGroup>(panel.gameObject);
            EnsureComponent<MovementPhaseTokenStatusPanel>(panel.gameObject);

            panel.anchorMin = new Vector2(0f, 0.5f);
            panel.anchorMax = new Vector2(0f, 0.5f);
            panel.pivot = new Vector2(0f, 0.5f);
            panel.anchoredPosition = new Vector2(20f, -220f);
            panel.sizeDelta = new Vector2(610f, 268f);

            Image image = panel.GetComponent<Image>();
            image.color = Color.clear;
            image.raycastTarget = false;

            CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            VerticalLayoutGroup layout = panel.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            return panel;
        }

        private static void EnsureTitle(RectTransform panel)
        {
            TextMeshProUGUI title = EnsureChild<TextMeshProUGUI>(panel, "Title", typeof(TextMeshProUGUI), typeof(LayoutElement));
            RectTransform titleRect = title.GetComponent<RectTransform>();
            titleRect.sizeDelta = new Vector2(590f, 24f);

            LayoutElement layoutElement = title.GetComponent<LayoutElement>();
            layoutElement.preferredWidth = 590f;
            layoutElement.preferredHeight = 24f;
            layoutElement.minHeight = 24f;

            title.text = "Token Status";
            title.alignment = TextAlignmentOptions.Left;
            title.fontSize = 18f;
            title.enableAutoSizing = false;
            title.color = Color.white;
            title.raycastTarget = false;
        }

        private static RectTransform EnsureSection(RectTransform panel, string sectionName, string labelText)
        {
            RectTransform section = EnsureRect(panel, sectionName, typeof(VerticalLayoutGroup), typeof(LayoutElement));
            section.sizeDelta = new Vector2(590f, 64f);

            LayoutElement sectionLayoutElement = section.GetComponent<LayoutElement>();
            sectionLayoutElement.preferredWidth = 590f;
            sectionLayoutElement.preferredHeight = 64f;
            sectionLayoutElement.minHeight = 64f;

            VerticalLayoutGroup sectionLayout = section.GetComponent<VerticalLayoutGroup>();
            sectionLayout.padding = new RectOffset(0, 0, 0, 0);
            sectionLayout.spacing = 4f;
            sectionLayout.childAlignment = TextAnchor.UpperCenter;
            sectionLayout.childControlWidth = false;
            sectionLayout.childControlHeight = false;
            sectionLayout.childForceExpandWidth = false;
            sectionLayout.childForceExpandHeight = false;

            TextMeshProUGUI label = EnsureChild<TextMeshProUGUI>(section, "Label", typeof(TextMeshProUGUI), typeof(LayoutElement));
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.sizeDelta = new Vector2(590f, 14f);

            LayoutElement labelLayoutElement = label.GetComponent<LayoutElement>();
            labelLayoutElement.preferredWidth = 590f;
            labelLayoutElement.preferredHeight = 14f;
            labelLayoutElement.minHeight = 14f;

            label.text = labelText;
            label.alignment = TextAlignmentOptions.Left;
            label.fontSize = 12f;
            label.enableAutoSizing = false;
            label.color = new Color(0.86f, 0.89f, 0.94f, 1f);
            label.raycastTarget = false;

            RectTransform row = EnsureRect(section, "Slots", typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.sizeDelta = new Vector2(590f, SlotSize);

            LayoutElement rowLayoutElement = row.GetComponent<LayoutElement>();
            rowLayoutElement.preferredWidth = 590f;
            rowLayoutElement.preferredHeight = SlotSize;
            rowLayoutElement.minHeight = SlotSize;

            HorizontalLayoutGroup rowLayout = row.GetComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 6f;
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            rowLayout.childControlWidth = false;
            rowLayout.childControlHeight = false;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;

            return section;
        }

        private static MovementPhaseMovedTokenSlotView[] EnsureSlots(RectTransform section, int slotCount)
        {
            RectTransform row = section.Find("Slots") as RectTransform;
            MovementPhaseMovedTokenSlotView[] slots = new MovementPhaseMovedTokenSlotView[slotCount];
            for (int i = 0; i < slotCount; i++)
            {
                slots[i] = EnsureSlot(row, $"Slot{i + 1:00}");
            }

            return slots;
        }

        private static MovementPhaseMovedTokenSlotView EnsureSlot(RectTransform row, string slotName)
        {
            RectTransform slot = EnsureRect(row, slotName, typeof(Image), typeof(MovementPhaseMovedTokenSlotView), typeof(LayoutElement));
            EnsureComponent<Image>(slot.gameObject);
            EnsureComponent<MovementPhaseMovedTokenSlotView>(slot.gameObject);
            EnsureComponent<LayoutElement>(slot.gameObject);
            slot.sizeDelta = new Vector2(SlotSize, SlotSize);

            LayoutElement layoutElement = slot.GetComponent<LayoutElement>();
            layoutElement.preferredWidth = SlotSize;
            layoutElement.preferredHeight = SlotSize;
            layoutElement.minWidth = SlotSize;
            layoutElement.minHeight = SlotSize;

            Image slotImage = slot.GetComponent<Image>();
            slotImage.color = Color.clear;
            slotImage.raycastTarget = false;

            RawImage faceImage = EnsureChild<RawImage>(slot, "Face", typeof(RawImage));
            RectTransform faceRect = faceImage.GetComponent<RectTransform>();
            faceRect.anchorMin = new Vector2(0.5f, 0.5f);
            faceRect.anchorMax = new Vector2(0.5f, 0.5f);
            faceRect.pivot = new Vector2(0.5f, 0.5f);
            faceRect.anchoredPosition = Vector2.zero;
            faceRect.sizeDelta = new Vector2(FaceSize, FaceSize);
            faceImage.raycastTarget = false;
            faceImage.color = Color.clear;

            TextMeshProUGUI numberText = EnsureChild<TextMeshProUGUI>(slot, "Number", typeof(TextMeshProUGUI));
            RectTransform numberRect = numberText.GetComponent<RectTransform>();
            numberRect.anchorMin = Vector2.zero;
            numberRect.anchorMax = Vector2.one;
            numberRect.offsetMin = Vector2.zero;
            numberRect.offsetMax = Vector2.zero;
            numberText.text = string.Empty;
            numberText.alignment = TextAlignmentOptions.Center;
            numberText.raycastTarget = false;
            numberText.enableAutoSizing = false;
            numberText.fontSize = PlainNumberFontSize;

            SerializedObject serializedSlot = new(slot.GetComponent<MovementPhaseMovedTokenSlotView>());
            serializedSlot.FindProperty("faceImage").objectReferenceValue = faceImage;
            serializedSlot.FindProperty("numberText").objectReferenceValue = numberText;
            serializedSlot.FindProperty("plainNumberFontSize").floatValue = PlainNumberFontSize;
            serializedSlot.FindProperty("verticalNumberFontSize").floatValue = VerticalNumberFontSize;
            serializedSlot.ApplyModifiedPropertiesWithoutUndo();

            return slot.GetComponent<MovementPhaseMovedTokenSlotView>();
        }

        private static RectTransform EnsureRect(RectTransform parent, string name, params System.Type[] components)
        {
            Transform existing = parent.Find(name);
            if (existing is RectTransform existingRect)
            {
                return existingRect;
            }

            return CreateRect(name, parent, components).GetComponent<RectTransform>();
        }

        private static T EnsureChild<T>(RectTransform parent, string name, params System.Type[] components) where T : Component
        {
            Transform existing = parent.Find(name);
            if (existing != null && existing.TryGetComponent(out T existingComponent))
            {
                return existingComponent;
            }

            return CreateRect(name, parent, components).GetComponent<T>();
        }

        private static GameObject CreateRect(string name, Transform parent, params System.Type[] components)
        {
            System.Type[] componentTypes = new System.Type[components.Length + 2];
            componentTypes[0] = typeof(RectTransform);
            componentTypes[1] = typeof(CanvasRenderer);
            for (int i = 0; i < components.Length; i++)
            {
                componentTypes[i + 2] = components[i];
            }

            GameObject gameObject = new(name, componentTypes);
            gameObject.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(gameObject, $"Create {name}");
            return gameObject;
        }

        private static T EnsureComponent<T>(GameObject gameObject) where T : Component
        {
            if (gameObject.TryGetComponent(out T component))
            {
                return component;
            }

            return gameObject.AddComponent<T>();
        }

        private static void AssignSlotArray(SerializedProperty property, MovementPhaseMovedTokenSlotView[] slots)
        {
            property.arraySize = slots.Length;
            for (int i = 0; i < slots.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
            }
        }

        private static Canvas ResolveMainCanvas()
        {
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            Canvas namedCanvas = canvases.FirstOrDefault(candidate => candidate != null && candidate.name == "Canvas");
            if (namedCanvas != null)
            {
                return namedCanvas;
            }

            return canvases.FirstOrDefault(candidate =>
                candidate != null
                && candidate.isRootCanvas
                && candidate.name != "HoveredTokenNameCanvas");
        }
    }
}
#endif
