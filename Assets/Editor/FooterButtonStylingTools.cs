using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CounterAttack.Editor
{
    public static class FooterButtonStylingTools
    {
        [MenuItem("CounterAttack/Room/Use Main Menu Footer Arrows")]
        public static void Apply()
        {
            StyleOpenRoom();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        public static void StyleOpenRoom()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != "Assets/Scenes/Room.unity"
                || UnityEngine.SceneManagement.SceneManager.sceneCount != 1)
                throw new InvalidOperationException("Open Room alone in edit mode.");
            var p = Object.FindAnyObjectByType<PauseMenuManager>();
            var buttons = new List<Button> { p.saveAsSaveButton, p.saveAsCancelButton, p.overwriteCancelButton,
                p.overwriteReplaceButton, p.saveTacticalNoteButton, p.cancelTacticalNoteButton, p.quitButton, p.editSettingsBackButton };
            var subs = Object.FindAnyObjectByType<SubstitutionMenuView>(FindObjectsInactive.Include);
            buttons.Add(subs.backButton); buttons.Add(subs.confirmButton);
            buttons.Add(Object.FindAnyObjectByType<PenaltyShootoutOrderPanelView>(FindObjectsInactive.Include).startButton);
            buttons.Add(GameObject.Find("MainCanvas").transform.Find("EndGamePanel/RecapPanel/BackToMainMenuButton").GetComponent<Button>());
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/UI/Cards/MenuCard.prefab").transform.Find("ExitButton");
            foreach (var button in buttons) StyleArrowButton(button, source);
            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static void StyleArrowButton(Button button, Transform source)
        {
            foreach (var behaviour in button.GetComponents<MonoBehaviour>())
                if (behaviour != null && behaviour.GetType().Name == "FooterButtonFeedback") Object.DestroyImmediate(behaviour);
            var image = button.GetComponent<Image>();
            if (image == null) image = button.gameObject.AddComponent<Image>();
            foreach (var outline in image.GetComponents<Outline>()) Object.DestroyImmediate(outline);
            image.color = Color.clear; image.raycastTarget = true;
            button.targetGraphic = image;
            foreach (var layout in button.GetComponents<LayoutGroup>()) layout.enabled = false;

            var text = button.GetComponentInChildren<TMP_Text>(true);
            text.transform.SetParent(button.transform, false);
            text.name = "Text (TMP)";
            text.font = source.Find("Text (TMP)").GetComponent<TMP_Text>().font;
            text.text = text.text.ToUpperInvariant();
            text.color = Color.white; text.alignment = TextAlignmentOptions.Center;
            text.fontSize = text.fontSizeMax; text.fontSizeMin = Mathf.Min(text.fontSizeMin, text.fontSizeMax); text.enableAutoSizing = true;
            var fitter = text.GetComponent<ContentSizeFitter>();
            if (fitter != null) fitter.enabled = false;
            const float arrowWidth = 48, gap = 16;
            var label = text.rectTransform;
            label.anchorMin = Vector2.zero; label.anchorMax = Vector2.one;
            label.offsetMin = Vector2.zero; label.offsetMax = new Vector2(-(arrowWidth + gap), 0);
            Canvas.ForceUpdateCanvases(); text.ForceMeshUpdate(true, true);
            float renderedWidth = Mathf.Min(text.GetPreferredValues(text.text).x,
                Mathf.Max(1, ((RectTransform)button.transform).rect.width - arrowWidth - gap));
            var existing = button.transform.Find("ArrowPanel");
            var arrow = existing != null ? (RectTransform)existing
                : (RectTransform)Object.Instantiate(source.Find("ArrowPanel").gameObject, button.transform, false).transform;
            arrow.name = "ArrowPanel"; arrow.gameObject.SetActive(true);
            arrow.anchorMin = arrow.anchorMax = arrow.pivot = new Vector2(.5f, .5f);
            arrow.sizeDelta = new Vector2(arrowWidth, arrowWidth);
            arrow.anchoredPosition = new Vector2(-(arrowWidth + gap) * .5f + renderedWidth * .5f + gap + arrowWidth * .5f, 0);
            arrow.localScale = Vector3.zero;
            foreach (var graphic in arrow.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
            var animator = button.GetComponent<Animator>();
            if (animator == null) animator = button.gameObject.AddComponent<Animator>();
            EditorUtility.CopySerialized(source.GetComponent<Animator>(), animator);
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            button.transition = Selectable.Transition.Animation;
            button.animationTriggers = source.GetComponent<Button>().animationTriggers;
            EditorUtility.SetDirty(button);
        }

        [MenuItem("CounterAttack/Room/Match Kit Dropdown Scrolling")]
        public static void SynchronizeKitScrolling()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Requires edit mode.");
            var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (active.isDirty) throw new InvalidOperationException("Save the current scene first.");
            var values = new List<(float speed, bool inertia, float deceleration, float elasticity)>();
            try
            {
                EditorSceneManager.OpenScene("Assets/Scenes/CreateNewGameScene.unity", OpenSceneMode.Single);
                var source = Object.FindAnyObjectByType<CreateNewGameManager>();
                foreach (var dropdown in new[] { source.homeKitDropdown, source.awayKitDropdown, source.homeGKKitDropdown, source.awayGKKitDropdown })
                {
                    var scroll = dropdown.template.GetComponent<ScrollRect>();
                    values.Add((CreateNewGameManager.KitDropdownScrollSensitivity, scroll.inertia, scroll.decelerationRate, scroll.elasticity));
                }
            }
            finally { EditorSceneManager.OpenScene("Assets/Scenes/Room.unity", OpenSceneMode.Single); }
            var pause = Object.FindAnyObjectByType<PauseMenuManager>();
            var targets = new[] { pause.editHomeKitDropdown, pause.editAwayKitDropdown, pause.editHomeGKKitDropdown, pause.editAwayGKKitDropdown };
            for (int i = 0; i < targets.Length; i++)
            {
                var scroll = targets[i].template.GetComponent<ScrollRect>();
                scroll.scrollSensitivity = values[i].speed; scroll.inertia = values[i].inertia;
                scroll.decelerationRate = values[i].deceleration; scroll.elasticity = values[i].elasticity;
                EditorUtility.SetDirty(scroll);
            }
            var room = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(room); EditorSceneManager.SaveScene(room);
        }
    }
}
