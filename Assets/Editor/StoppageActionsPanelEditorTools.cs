using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CounterAttack.Editor
{
    /// <summary>Authors an independent panel under MainCanvas; never creates runtime visuals.</summary>
    public static class StoppageActionsPanelEditorTools
    {
        private static TMP_FontAsset font;

        [MenuItem("CounterAttack/Room/Ensure Stoppage Actions Panel")]
        public static void EnsurePanel()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before authoring the stoppage panel.");
            Scene room = SceneManager.GetSceneByPath("Assets/Scenes/Room.unity");
            if (!room.isLoaded) throw new InvalidOperationException("Open Room first.");
            Transform canvas = null;
            MatchManager manager = null;
            foreach (var root in room.GetRootGameObjects())
            {
                if (root.name == "MainCanvas") canvas = root.transform;
                if (manager == null) manager = root.GetComponentInChildren<MatchManager>(true);
            }
            if (canvas == null || manager == null) throw new InvalidOperationException("Room MainCanvas/MatchManager missing.");
            var existing = canvas.Find("StoppageActionsUI");
            if (existing != null)
            {
                var bound = existing.GetComponent<StoppageActionsPanelController>();
                if (bound == null || !bound.HasSceneBindings) throw new InvalidOperationException("Inspect incomplete stoppage-panel bindings.");
                manager.stoppageActionsPanel = bound;
                bound.match = manager;
                EditorUtility.SetDirty(manager);
                EditorUtility.SetDirty(bound);
                EditorSceneManager.MarkSceneDirty(room);
                return;
            }
            var template = canvas.GetComponentInChildren<DuelResolutionPanelController>(true);
            if (template == null) throw new InvalidOperationException("Existing dice assets are required.");
            font = template.title.font;
            var rootRect = Rect("StoppageActionsUI", canvas);
            Stretch(rootRect);
            var controller = rootRect.gameObject.AddComponent<StoppageActionsPanelController>();
            controller.match = manager;
            var visuals = Rect("Visuals", rootRect);
            Stretch(visuals);
            controller.visuals = visuals.gameObject;
            var blocker = Rect("InputBlocker", visuals);
            Stretch(blocker);
            Image(blocker, new Color(0f, 0f, 0f, .02f), true);
            var consumer = blocker.gameObject.AddComponent<GameplayInputConsumer>();
            consumer.ConsumesPointerOverSelf = true;
            consumer.BlocksGameplayWhileActive = false;
            var panel = Rect("Panel", visuals);
            panel.anchorMin = panel.anchorMax = new Vector2(.5f, .5f);
            panel.pivot = new Vector2(.5f, 1f);
            controller.panel = panel;
            Image(panel, new Color(.055f, .065f, .085f, .98f), true);
            panel.gameObject.AddComponent<StoppageActionsPanelDragHandle>().owner = controller;
            controller.title = Label("Title", panel, 24, 64, "STOPPAGE ACTIONS", 44);
            controller.situation = Label("Situation", panel, 100, 60, StoppageActionsPanelController.SituationCopy("First half"), 32);
            controller.rollingTeam = Label("RollingTeam", panel, 170, 60, "Attacking team", 34);
            controller.rule = Label("Rule", panel, 240, 100, "Referee leniency 4 + D6\nMaximum: 7 stoppage actions", 29);
            var dieRect = Rect("Die", panel);
            dieRect.anchorMin = dieRect.anchorMax = new Vector2(.5f, 1f);
            dieRect.pivot = new Vector2(.5f, 1f);
            dieRect.anchoredPosition = new Vector2(0f, -360f);
            dieRect.sizeDelta = Vector2.one * 112f;
            controller.die = dieRect.gameObject.AddComponent<DuelDieView>();
            controller.die.background = Image(dieRect, new Color(.13f, .35f, .63f), false);
            controller.die.border = dieRect.gameObject.AddComponent<UnityEngine.UI.Outline>();
            controller.die.border.effectDistance = new Vector2(4f, 4f);
            controller.die.border.effectColor = Color.white;
            var face = Rect("Face", dieRect);
            Stretch(face, 14);
            controller.die.face = Image(face, Color.white, false);
            controller.die.numericFaces = (Sprite[])template.leftPlayer.die.numericFaces.Clone();
            controller.die.jackpotFace = template.leftPlayer.die.jackpotFace;
            controller.die.face.sprite = controller.die.numericFaces[0];
            var fallback = Rect("FallbackText", dieRect);
            Stretch(fallback, 5);
            controller.die.fallbackText = Text(fallback, "1", 64);
            controller.die.fallbackText.enabled = false;
            controller.result = Label("Result", panel, 490, 110, "", 32);
            var footer = Rect("CountdownFooter", panel);
            footer.anchorMin = Vector2.zero;
            footer.anchorMax = new Vector2(1f, 0f);
            footer.pivot = new Vector2(.5f, 0f);
            footer.anchoredPosition = new Vector2(0f, 24f);
            footer.sizeDelta = new Vector2(-60, 74);
            controller.footer = Text(footer, "Press [R] to roll", 26);
            controller.FitAndClamp();
            if (panel.parent is RectTransform bounds)
                panel.anchoredPosition = new Vector2(0f, bounds.rect.height * (.5f - controller.initialTopInsetFraction));
            controller.FitAndClamp();
            manager.stoppageActionsPanel = controller;
            EditorUtility.SetDirty(manager);
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(room);
        }

        [MenuItem("CounterAttack/Room/Preview Stoppage Actions/Awaiting Roll")]
        public static void PreviewAwaiting()
        {
            EnsurePanel();
            var p = UnityEngine.Object.FindAnyObjectByType<StoppageActionsPanelController>();
            p.visuals.SetActive(true);
            p.situation.text = StoppageActionsPanelController.SituationCopy("First half");
            p.result.text = "";
            p.die.Clear();
            p.footer.text = "Press [R] to roll";
            FinishPreview(p);
        }

        [MenuItem("CounterAttack/Room/Preview Stoppage Actions/Capped Result")]
        public static void PreviewResult()
        {
            PreviewAwaiting();
            var p = UnityEngine.Object.FindAnyObjectByType<StoppageActionsPanelController>();
            p.die.Resolve(6, false);
            p.rule.text = "Referee leniency 4 + D6\nMaximum: 7 stoppage actions";
            p.result.text = "Referee leniency 4 + roll 6 = 10\n7 stoppage actions (maximum 7)";
            p.footer.text = "Closes automatically in 5 seconds — [Enter] hold · [Esc] close now";
            FinishPreview(p);
        }

        [MenuItem("CounterAttack/Room/Preview Stoppage Actions/Second Half")]
        public static void PreviewSecondHalf() => PreviewPeriod("Second half");
        [MenuItem("CounterAttack/Room/Preview Stoppage Actions/Extra Time First Half")]
        public static void PreviewExtraTimeFirstHalf() => PreviewPeriod("Extra time — first half");
        [MenuItem("CounterAttack/Room/Preview Stoppage Actions/Extra Time Second Half")]
        public static void PreviewExtraTimeSecondHalf() => PreviewPeriod("Extra time — second half");

        private static void PreviewPeriod(string label)
        {
            PreviewAwaiting();
            var p = UnityEngine.Object.FindAnyObjectByType<StoppageActionsPanelController>();
            p.situation.text = StoppageActionsPanelController.SituationCopy(label);
            FinishPreview(p);
        }

        private static void FinishPreview(StoppageActionsPanelController panel)
        {
            panel.FitAndClamp();
            UnityEngine.Canvas.ForceUpdateCanvases();
            Selection.activeGameObject = panel.panel.gameObject;
            EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);
            EditorApplication.QueuePlayerLoopUpdate();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        private static RectTransform Rect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.gameObject.layer = parent.gameObject.layer;
            return rect;
        }
        private static void Stretch(RectTransform rect, float margin = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.one * margin;
            rect.offsetMax = Vector2.one * -margin;
        }
        private static TMP_Text Label(string name, Transform parent, float top, float height, string value, float size)
        {
            var rect = Rect(name, parent);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -top);
            rect.sizeDelta = new Vector2(-48f, height);
            return Text(rect, value, size);
        }
        private static TMP_Text Text(RectTransform rect, string value, float size)
        {
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = size;
            text.enableAutoSizing = true;
            text.fontSizeMin = size * .7f;
            text.fontSizeMax = size;
            text.text = value;
            text.color = new Color(.94f, .95f, .98f);
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }
        private static UnityEngine.UI.Image Image(RectTransform rect, Color color, bool raycast)
        {
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }
    }
}
