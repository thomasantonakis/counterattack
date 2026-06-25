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
    public static class MovementPhaseMovedTokensPanelEditorTools
    {
        private const string RoomScenePath = "Assets/Scenes/Room.unity";
        private const string PanelName = "MovedTokensPanel";
        private const float SlotSize = 46f;
        private const float FaceSize = 38f;
        private const float PlainNumberFontSize = 24f;
        private const float VerticalNumberFontSize = 21f;

        [MenuItem("Tools/Counter Attack/Ensure Movement Phase Moved Tokens Panel In Room")]
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
            if (movementPhaseManager == null)
            {
                Debug.LogError("Could not find MovementPhaseManager in Room scene.");
                return;
            }

            RectTransform panel = EnsurePanel(canvas.transform);
            MovementPhaseMovedTokensPanel panelController = panel.GetComponent<MovementPhaseMovedTokensPanel>();
            CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();

            EnsureTitle(panel);
            MovementPhaseMovedTokenSlotView[] attackSlots = EnsureRow(panel, "AttMPRow", 4);
            MovementPhaseMovedTokenSlotView[] defenseSlots = EnsureRow(panel, "DefMPRow", 5);
            MovementPhaseMovedTokenSlotView[] twoForTwoSlots = EnsureRow(panel, "Att2f2Row", 2);

            SerializedObject serializedPanel = new(panelController);
            serializedPanel.FindProperty("movementPhaseManager").objectReferenceValue = movementPhaseManager;
            serializedPanel.FindProperty("canvasGroup").objectReferenceValue = canvasGroup;
            AssignSlotArray(serializedPanel.FindProperty("attackingMovementSlots"), attackSlots);
            AssignSlotArray(serializedPanel.FindProperty("defensiveMovementSlots"), defenseSlots);
            AssignSlotArray(serializedPanel.FindProperty("attackingTwoForTwoSlots"), twoForTwoSlots);
            serializedPanel.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(panelController);
            EditorUtility.SetDirty(panel);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("Ensured Movement Phase moved-token panel as an edit-mode object in Room scene.");
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
                    typeof(MovementPhaseMovedTokensPanel));
                panel = panelObject.GetComponent<RectTransform>();
            }

            EnsureComponent<CanvasGroup>(panel.gameObject);
            EnsureComponent<Image>(panel.gameObject);
            EnsureComponent<VerticalLayoutGroup>(panel.gameObject);
            EnsureComponent<MovementPhaseMovedTokensPanel>(panel.gameObject);

            panel.anchorMin = new Vector2(0f, 0.5f);
            panel.anchorMax = new Vector2(0f, 0.5f);
            panel.pivot = new Vector2(0f, 0.5f);
            panel.anchoredPosition = new Vector2(20f, 0f);
            panel.sizeDelta = new Vector2(300f, 210f);

            Image image = panel.GetComponent<Image>();
            image.color = new Color(0.04f, 0.05f, 0.06f, 0.72f);
            image.raycastTarget = false;

            CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            VerticalLayoutGroup layout = panel.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.UpperLeft;
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
            titleRect.sizeDelta = new Vector2(280f, 24f);
            titleRect.SetAsFirstSibling();

            LayoutElement layoutElement = title.GetComponent<LayoutElement>();
            layoutElement.preferredWidth = 280f;
            layoutElement.preferredHeight = 24f;
            layoutElement.minHeight = 24f;

            title.text = "Moved Tokens";
            title.alignment = TextAlignmentOptions.Left;
            title.fontSize = 18f;
            title.enableAutoSizing = false;
            title.color = Color.white;
            title.raycastTarget = false;
        }

        private static MovementPhaseMovedTokenSlotView[] EnsureRow(RectTransform panel, string rowName, int slotCount)
        {
            RectTransform row = EnsureRect(panel, rowName, typeof(HorizontalLayoutGroup));
            EnsureComponent<HorizontalLayoutGroup>(row.gameObject);
            row.sizeDelta = new Vector2(280f, SlotSize);

            HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

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
            slotImage.color = new Color(0.88f, 0.91f, 0.96f, 0.18f);
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
