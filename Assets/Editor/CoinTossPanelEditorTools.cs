using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CounterAttack.Editor
{
    public static class CoinTossPanelEditorTools
    {
        [MenuItem("CounterAttack/Draft/Ensure Coin Toss Panels")]
        public static void EnsurePanels()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Coin toss panel authoring requires edit mode.");
            var active = SceneManager.GetActiveScene();
            var draft = SceneManager.GetSceneByPath("Assets/Scenes/Draft.unity");
            bool closeDraft = !draft.isLoaded;
            if (closeDraft) draft = EditorSceneManager.OpenScene("Assets/Scenes/Draft.unity", OpenSceneMode.Additive);
            try
            {
                var template = draft.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<DraftOrderPanelController>(true))
                    .Single(panel => panel.name == "DraftOrderUI");
                EnsurePanel(draft, template);
                var free = SceneManager.GetSceneByPath("Assets/Scenes/FreeDraft.unity");
                bool closeFree = !free.isLoaded;
                if (closeFree) free = EditorSceneManager.OpenScene("Assets/Scenes/FreeDraft.unity", OpenSceneMode.Additive);
                try { EnsurePanel(free, template); }
                finally { if (closeFree) EditorSceneManager.CloseScene(free, true); }
            }
            finally
            {
                if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active);
                if (closeDraft) EditorSceneManager.CloseScene(draft, true);
            }
        }

        private static void EnsurePanel(Scene scene, DraftOrderPanelController template)
        {
            var ui = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<DraftUIManager>(true)).Single();
            var canvas = ui.homeTeamPanel.GetComponentInParent<Canvas>();
            if (canvas == null || canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
                throw new InvalidOperationException($"{scene.name} needs its canvas and raycaster.");
            int systems = scene.GetRootGameObjects()
                .SelectMany(go => go.GetComponentsInChildren<UnityEngine.EventSystems.EventSystem>(true)).Count();
            if (systems != 1) throw new InvalidOperationException($"{scene.name} requires exactly one EventSystem.");
            var existing = canvas.transform.Find("CoinTossUI");
            CoinTossPanelController controller;
            if (existing != null)
            {
                controller = existing.GetComponent<CoinTossPanelController>();
                if (controller == null || !controller.HasSceneBindings)
                    throw new InvalidOperationException($"Inspect {scene.name} CoinTossUI bindings.");
            }
            else
            {
                // Clone authored dice, typography, modal blocker and columns, with remapped scene references.
                var root = UnityEngine.Object.Instantiate(template.gameObject, canvas.transform);
                root.name = "CoinTossUI";
                root.transform.SetAsLastSibling();
                controller = root.AddComponent<CoinTossPanelController>();
                controller.dicePanel = root.GetComponent<DraftOrderPanelController>();
                var dice = controller.dicePanel;
                dice.title.text = "COIN TOSS FLIP";
                dice.outcome.text = "Highest roll chooses kickoff or direction";
                var options = Rect("Choices", dice.panel);
                options.anchorMin = Vector2.zero; options.anchorMax = Vector2.one;
                options.offsetMin = options.offsetMax = Vector2.zero;
                controller.choices = options.gameObject;
                controller.kickoffButton = Button("Kickoff", options, 635, "1. First Half Kick Off", dice.title.font);
                controller.leftToRightButton = Button("LeftToRight", options, 750, "2. First Half Left to Right", dice.title.font);
                controller.rightToLeftButton = Button("RightToLeft", options, 865, "3. First Half Right to Left", dice.title.font);
                options.gameObject.SetActive(false);
                dice.visuals.SetActive(false);
            }
            ui.coinTossPanel = controller;
            EditorUtility.SetDirty(ui);
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (!controller.HasSceneBindings) throw new InvalidOperationException($"{scene.name}: Coin toss bindings incomplete.");
        }

        private static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static UnityEngine.UI.Button Button(string name, Transform parent, float top, string label, TMP_FontAsset font)
        {
            var rect = Rect(name, parent);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, 1);
            rect.sizeDelta = new Vector2(1100, 90); rect.anchoredPosition = new Vector2(0, -top);
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(.16f, .29f, .36f); image.raycastTarget = true;
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image;
            var colors = button.colors; colors.disabledColor = new Color(.38f, .38f, .38f, .65f); button.colors = colors;
            var textRect = Rect("Label", rect);
            textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(20, 5); textRect.offsetMax = new Vector2(-20, -5);
            var text = textRect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font; text.text = label; text.fontSize = 34;
            text.enableAutoSizing = true; text.fontSizeMin = 24; text.fontSizeMax = 34;
            text.color = Color.white; text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
            return button;
        }
    }
}
