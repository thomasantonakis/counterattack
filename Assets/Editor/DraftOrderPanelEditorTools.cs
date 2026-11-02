using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CounterAttack.Editor
{
    public static class DraftOrderPanelEditorTools
    {
        [MenuItem("CounterAttack/Draft/Ensure First Pick Roll Panel")]
        public static void EnsurePanel()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Draft panel authoring requires edit mode.");
            var draft = SceneManager.GetSceneByPath("Assets/Scenes/Draft.unity");
            if (!draft.isLoaded) throw new InvalidOperationException("Open Draft.unity first.");
            var manager = draft.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<DraftManager>(true)).Single();
            var canvas = manager.homeTeamPanel.GetComponentInParent<Canvas>();
            if (canvas == null || canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
                throw new InvalidOperationException("Draft requires its authored canvas and raycaster.");
            var existing = canvas.transform.Find("DraftOrderUI");
            if (existing != null)
            {
                var current = existing.GetComponent<DraftOrderPanelController>();
                if (current == null || !current.HasSceneBindings)
                    throw new InvalidOperationException("Inspect existing DraftOrderUI bindings.");
                manager.draftOrderPanel = current;
                EditorUtility.SetDirty(manager);
                EditorSceneManager.MarkSceneDirty(draft);
                EditorSceneManager.SaveScene(draft);
                return;
            }

            // Reuse the Room's authored dice artwork and font; never generate a runtime visual.
            var room = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Room.unity");
            try
            {
                var source = room.GetRootGameObjects()
                    .SelectMany(go => go.GetComponentsInChildren<DuelResolutionPanelController>(true)).Single();
                var root = Rect("DraftOrderUI", canvas.transform); Stretch(root);
                var controller = root.gameObject.AddComponent<DraftOrderPanelController>();
                var visuals = Rect("Visuals", root); Stretch(visuals); controller.visuals = visuals.gameObject;
                Background(visuals, new Color(0, 0, 0, .25f));
                var panel = Rect("Panel", visuals);
                panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(.5f, .5f);
                panel.sizeDelta = new Vector2(1440, 720);
                Background(panel, new Color(.055f, .065f, .085f, .98f));
                controller.panel = panel;
                controller.title = Label("Title", panel, 28, 70, "ROLL FOR FIRST DRAFT PICK", 42, source.title.font);
                var home = Column("HomeManager", panel, false);
                var away = Column("AwayManager", panel, true);
                controller.homeTeam = Label("TeamName", home, 0, 70, "Home team", 38, source.title.font);
                controller.awayTeam = Label("TeamName", away, 0, 70, "Away team", 38, source.title.font);
                controller.homeDie = Die("Die", home, source.leftPlayer.die);
                controller.awayDie = Die("Die", away, source.leftPlayer.die);
                controller.homeResult = Label("RollResult", home, 300, 60, "", 34, source.title.font);
                controller.awayResult = Label("RollResult", away, 300, 60, "", 34, source.title.font);
                controller.outcome = Label("Outcome", panel, 535, 65, "Highest roll drafts first", 34, source.title.font);
                controller.footer = Label("Controls", panel, 625, 55, "Press [R] to roll", 28, source.title.font);
                manager.draftOrderPanel = controller;
                visuals.gameObject.SetActive(false);
                root.SetAsLastSibling();
                if (!controller.HasSceneBindings) throw new InvalidOperationException("Draft roll panel bindings incomplete.");
                EditorUtility.SetDirty(manager);
                EditorUtility.SetDirty(controller);
                EditorSceneManager.MarkSceneDirty(draft);
                EditorSceneManager.SaveScene(draft);
            }
            finally { EditorSceneManager.ClosePreviewScene(room); }
        }

        private static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static void Background(RectTransform rect, Color color)
        {
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = color; image.raycastTarget = true;
        }

        private static RectTransform Column(string name, Transform parent, bool right)
        {
            var rect = Rect(name, parent);
            rect.anchorMin = new Vector2(right ? .5f : 0, 1);
            rect.anchorMax = new Vector2(right ? 1 : .5f, 1);
            rect.pivot = new Vector2(.5f, 1);
            rect.anchoredPosition = new Vector2(0, -125);
            rect.sizeDelta = new Vector2(-48, 380);
            return rect;
        }

        private static TMP_Text Label(string name, Transform parent, float top, float height,
            string value, int size, TMP_FontAsset font)
        {
            var rect = Rect(name, parent);
            rect.anchorMin = new Vector2(0, 1); rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(.5f, 1);
            rect.anchoredPosition = new Vector2(0, -top); rect.sizeDelta = new Vector2(-48, height);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font; text.fontSize = size; text.enableAutoSizing = true;
            text.fontSizeMin = size * .6f; text.fontSizeMax = size;
            text.color = new Color(.94f, .95f, .98f); text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false; text.text = value;
            return text;
        }

        private static DuelDieView Die(string name, Transform parent, DuelDieView source)
        {
            var go = UnityEngine.Object.Instantiate(source.gameObject, parent);
            go.name = name;
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, 1);
            rect.anchoredPosition = new Vector2(0, -95);
            rect.sizeDelta = Vector2.one * 180;
            rect.localScale = Vector3.one;
            go.SetActive(true);
            var die = go.GetComponent<DuelDieView>(); die.Clear();
            return die;
        }
    }
}
