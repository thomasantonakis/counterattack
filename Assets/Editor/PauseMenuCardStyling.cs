using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CounterAttack.Editor
{
    public static class PauseMenuCardStyling
    {
        private const string Cards = "Assets/Resources/UI/Cards/";
        private const string Buttons = "Assets/Resources/UI/Buttons/";

        [MenuItem("CounterAttack/Room/Style Pause Menu Cards")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new System.InvalidOperationException("Requires edit mode.");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != "Assets/Scenes/Room.unity" || UnityEngine.SceneManagement.SceneManager.sceneCount != 1)
                throw new System.InvalidOperationException("Open Room alone before styling.");
            var manager = Object.FindAnyObjectByType<PauseMenuManager>();
            var pauseContent = Card(manager.pausePanel, "MenuCard_Pause", "PAUSED", "TAKE A BREATHER. PLAN YOUR NEXT MOVE.", 656, 1080);
            var settingsContent = Card(manager.editSettingsPanel, "MenuCard_PauseSettings", "MATCH SETTINGS", "MAKE THE MATCH YOUR OWN.", 1120, 1100);
            var actions = new[] { manager.resumeButton, manager.addTacticalNoteButton, manager.substitutionsButton,
                manager.saveMatchButton, manager.saveMatchAsButton, manager.editSettings, manager.backToMainMenuButton, manager.quitButton };
            for (int i = 0; i < actions.Length; i++)
            {
                StyleButton(actions[i]);
                actions[i].transform.SetParent(pauseContent, false);
                Place((RectTransform)actions[i].transform, 0, -i * 76, 488, 68);
            }
            manager.pauseSaveStatusText.transform.SetParent(pauseContent, false);
            Place(manager.pauseSaveStatusText.rectTransform, 0, -620, 488, 64);
            StyleStatus(manager.pauseSaveStatusText);

            var dropdowns = new[] { manager.editTiebreakerDropdown, manager.editPlayerAssistanceDropdown,
                manager.editWeatherDropdown, manager.editBallColorDropdown, manager.editHomeKitDropdown,
                manager.editAwayKitDropdown, manager.editHomeGKKitDropdown, manager.editAwayGKKitDropdown };
            var labels = new[] { "Tiebreaker", "Player assistance", "Weather", "Ball colour", "Home kit", "Away kit", "Home GK kit", "Away GK kit" };
            var serialized = new SerializedObject(manager);
            var fields = new[] { "editTiebreakerDropdown", "editPlayerAssistanceDropdown", "editWeatherDropdown", "editBallColorDropdown",
                "editHomeKitDropdown", "editAwayKitDropdown", "editHomeGKKitDropdown", "editAwayGKKitDropdown" };
            for (int i = 0; i < dropdowns.Length; i++)
            {
                var old = dropdowns[i];
                var row = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Buttons + "DropDownButton.prefab"), settingsContent);
                row.name = old.name + "Row";
                Place((RectTransform)row.transform, 0, -i * 75, 936, 62);
                foreach (var layout in row.GetComponents<LayoutGroup>()) layout.enabled = false;
                var label = row.transform.Find("Label").GetComponent<TMP_Text>();
                label.text = labels[i]; label.fontSize = 30; label.color = Ink;
                Place(label.rectTransform, -310, 0, 310, 62);
                var replacement = row.GetComponentInChildren<TMP_Dropdown>(true);
                replacement.name = old.name;
                Place((RectTransform)replacement.transform, 168, 0, 600, 62);
                replacement.options = old.options;
                replacement.SetValueWithoutNotify(old.value);
                replacement.RefreshShownValue();
                replacement.captionText.fontSize = 28;
                replacement.itemText.fontSize = 28;
                replacement.template.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 300);
                serialized.FindProperty(fields[i]).objectReferenceValue = replacement;
                Object.DestroyImmediate(old.transform.parent.gameObject);
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            StyleButton(manager.captureLogFileButton);
            StyleButton(manager.editSettingsBackButton);
            manager.captureLogFileButton.transform.SetParent(settingsContent, false);
            manager.editSettingsBackButton.transform.SetParent(settingsContent, false);
            Place((RectTransform)manager.captureLogFileButton.transform, -240, -640, 456, 68);
            Place((RectTransform)manager.editSettingsBackButton.transform, 240, -640, 456, 68);
            manager.logFileStatusText.transform.SetParent(settingsContent, false);
            Place(manager.logFileStatusText.rectTransform, 0, -718, 936, 54);
            StyleStatus(manager.logFileStatusText);
            manager.pausePanel.transform.SetAsLastSibling();
            manager.editSettingsPanel.transform.SetAsLastSibling();
            manager.saveAsOverlay.transform.SetAsLastSibling();
            manager.tacticalNoteOverlay.transform.SetAsLastSibling();
            UpdateFooterButtons();
            manager.pausePanel.SetActive(false);
            manager.editSettingsPanel.SetActive(false);
            EditorUtility.SetDirty(manager);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        private static Color Ink => new Color32(30, 69, 89, 255);

        [MenuItem("CounterAttack/Room/Style Pause Card Footer Buttons")]
        public static void UpdateFooterButtons()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new System.InvalidOperationException("Requires edit mode.");
            var manager = Object.FindAnyObjectByType<PauseMenuManager>();
            foreach (string name in new[] { "MenuCard_Pause", "MenuCard_PauseSettings" })
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Cards + name + ".prefab");
                var copy = Object.Instantiate(asset);
                var white = (RectTransform)copy.transform.Find("WhiteBkg");
                white.offsetMin = new Vector2(white.offsetMin.x, 120);
                PrefabUtility.SaveAsPrefabAsset(copy, Cards + name + ".prefab");
                Object.DestroyImmediate(copy);
            }
            Footer(manager.quitButton, manager.pausePanel.transform.Find("MenuCard_Pause"), "Quit Game");
            Footer(manager.editSettingsBackButton, manager.editSettingsPanel.transform.Find("MenuCard_PauseSettings"), "Back");
            AlignCaptureLogfile();
            Place(manager.logFileStatusText.rectTransform, 0, -680, 936, 40);
            EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
        }

        public static void AlignCaptureLogfile()
        {
            var button = Object.FindAnyObjectByType<PauseMenuManager>().captureLogFileButton;
            Place((RectTransform)button.transform, 224, -620, 488, 68);
            button.GetComponent<HorizontalLayoutGroup>().enabled = false;
            var selector = (RectTransform)button.transform.Find("Selector");
            selector.gameObject.SetActive(true);
            selector.anchorMin = selector.anchorMax = selector.pivot = new Vector2(1, .5f);
            selector.anchoredPosition = Vector2.zero;
            selector.sizeDelta = new Vector2(64, 64);
            var mask = (RectTransform)button.transform.Find("MaskContainer");
            mask.anchorMin = Vector2.zero; mask.anchorMax = Vector2.one;
            mask.offsetMin = mask.offsetMax = Vector2.zero;
            mask.offsetMax = new Vector2(-88, 0);
            var text = mask.GetComponentInChildren<TMP_Text>();
            var fitter = text.GetComponent<ContentSizeFitter>();
            if (fitter != null) fitter.enabled = false;
            text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
            text.alignment = TextAlignmentOptions.MidlineRight;
            CopyHoverAnimation(button, AssetDatabase.LoadAssetAtPath<GameObject>(Buttons + "Button.prefab").GetComponent<Button>());
            EditorSceneManager.MarkSceneDirty(button.gameObject.scene);
        }

        private static void Footer(Button button, Transform card, string label)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Cards + "MenuCard.prefab").transform.Find("ExitButton");
            foreach (var layout in button.GetComponents<LayoutGroup>()) Object.DestroyImmediate(layout);
            foreach (Transform child in button.transform.Cast<Transform>().ToArray()) Object.DestroyImmediate(child.gameObject);
            foreach (Transform child in source) Object.Instantiate(child.gameObject, button.transform, false).name = child.name;
            var animator = button.GetComponent<Animator>();
            if (animator == null) animator = button.gameObject.AddComponent<Animator>();
            EditorUtility.CopySerialized(source.GetComponent<Animator>(), animator);
            button.transition = source.GetComponent<Button>().transition;
            button.animationTriggers = source.GetComponent<Button>().animationTriggers;
            button.transform.SetParent(card, false);
            var rect = (RectTransform)button.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, 0);
            rect.anchoredPosition = new Vector2(0, 4);
            rect.sizeDelta = new Vector2(360, 110);
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
            button.transform.Find("Text (TMP)").GetComponent<TMP_Text>().text = label;
            AlignFooterArrow(button);
        }

        public static void AlignQuitHoverArrow()
        {
            var manager = Object.FindAnyObjectByType<PauseMenuManager>();
            AlignFooterArrow(manager.quitButton);
            AlignFooterArrow(manager.editSettingsBackButton);
            AlignCaptureLogfile();
        }

        private static void AlignFooterArrow(Button button)
        {
            var text = button.transform.Find("Text (TMP)").GetComponent<TMP_Text>();
            text.ForceMeshUpdate(true);
            const float gap = 24;
            var arrow = (RectTransform)button.transform.Find("ArrowPanel");
            arrow.anchorMin = arrow.anchorMax = arrow.pivot = new Vector2(.5f, .5f);
            arrow.anchoredPosition = new Vector2(text.preferredWidth * .5f + gap + arrow.sizeDelta.x * .5f, 0);
            ((RectTransform)button.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                text.preferredWidth + 2 * (gap + arrow.sizeDelta.x));
            EditorSceneManager.MarkSceneDirty(button.gameObject.scene);
        }

        private static Transform Card(GameObject overlay, string name, string title, string subtitle, float width, float height)
        {
            if (overlay.transform.Find(name) != null) throw new System.InvalidOperationException("Card already authored.");
            foreach (var layout in overlay.GetComponents<LayoutGroup>()) layout.enabled = false;
            var overlayRect = (RectTransform)overlay.transform;
            overlayRect.anchorMin = Vector2.zero; overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = overlayRect.offsetMax = Vector2.zero;
            overlay.GetComponent<Image>().color = new Color(0.015f, .035f, .045f, .65f);
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Cards + "MenuCard.prefab");
            var card = Object.Instantiate(source);
            card.name = name;
            var rect = (RectTransform)card.transform;
            rect.localRotation = Quaternion.identity; rect.localScale = Vector3.one;
            Place(rect, 0, 0, width, height);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            foreach (var layout in card.GetComponentsInChildren<LayoutGroup>(true)) layout.enabled = false;
            foreach (var fitter in card.GetComponentsInChildren<ContentSizeFitter>(true)) fitter.enabled = false;
            var white = (RectTransform)card.transform.Find("WhiteBkg");
            white.anchorMin = Vector2.zero; white.anchorMax = Vector2.one;
            white.offsetMin = new Vector2(60, 60); white.offsetMax = new Vector2(-60, -60);
            var header = (RectTransform)white.Find("HeaderPanel");
            Place(header, 0, -20, width - 168, 140);
            var heading = header.Find("TitleText").GetComponent<TMP_Text>();
            heading.text = title; heading.fontSize = 62; heading.color = Ink;
            Place(heading.rectTransform, 0, 0, width - 168, 76);
            var copy = header.Find("SubtitleText").GetComponent<TMP_Text>();
            copy.text = subtitle; copy.fontSize = 30; copy.color = new Color32(75, 107, 123, 255);
            Place(copy.rectTransform, 0, -80, width - 168, 70);
            var content = white.Find("ButtonsPanel");
            foreach (Transform child in content.Cast<Transform>().ToArray()) Object.DestroyImmediate(child.gameObject);
            Place((RectTransform)content, 0, -190, width - 168, height - 330);
            Object.DestroyImmediate(card.transform.Find("ExitButton").gameObject);
            var prefab = PrefabUtility.SaveAsPrefabAsset(card, Cards + name + ".prefab");
            Object.DestroyImmediate(card);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, overlay.transform);
            return instance.transform.Find("WhiteBkg/ButtonsPanel");
        }

        private static void StyleButton(Button button)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Buttons + "Button.prefab");
            string label = button.GetComponentInChildren<TMP_Text>(true).text;
            foreach (Transform child in button.transform.Cast<Transform>().ToArray()) Object.DestroyImmediate(child.gameObject);
            foreach (Transform child in source.transform) Object.Instantiate(child.gameObject, button.transform, false).name = child.name;
            var image = button.GetComponent<Image>();
            if (image != null) { image.color = new Color(1, 1, 1, 0); image.raycastTarget = true; button.targetGraphic = image; }
            var layout = button.GetComponent<HorizontalLayoutGroup>() ?? button.gameObject.AddComponent<HorizontalLayoutGroup>();
            EditorUtility.CopySerialized(source.GetComponent<HorizontalLayoutGroup>(), layout);
            var text = button.transform.Find("MaskContainer").GetComponentInChildren<TMP_Text>(true);
            text.text = label; text.fontSize = 40; text.color = Ink;
            button.colors = source.GetComponent<Button>().colors;
            button.navigation = new Navigation { mode = Navigation.Mode.Automatic };
            CopyHoverAnimation(button, source.GetComponent<Button>());
        }

        public static void RestorePauseHover()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new System.InvalidOperationException("Requires edit mode.");
            var manager = Object.FindAnyObjectByType<PauseMenuManager>();
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Buttons + "Button.prefab").GetComponent<Button>();
            foreach (var button in new[] { manager.resumeButton, manager.addTacticalNoteButton,
                manager.substitutionsButton, manager.saveMatchButton, manager.saveMatchAsButton,
                manager.editSettings, manager.backToMainMenuButton })
            {
                CopyHoverAnimation(button, source);
            }
            EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
        }

        private static void CopyHoverAnimation(Button button, Button source)
        {
            var animator = button.GetComponent<Animator>();
            if (animator == null) animator = button.gameObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = source.GetComponent<Animator>().runtimeAnimatorController;
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            button.transition = source.transition;
            button.animationTriggers = source.animationTriggers;
            EditorUtility.SetDirty(button);
            EditorUtility.SetDirty(animator);
        }

        private static void StyleStatus(TMP_Text text)
        {
            text.color = Ink; text.fontSize = 24; text.enableAutoSizing = true;
            text.fontSizeMin = 18; text.fontSizeMax = 24; text.raycastTarget = false;
        }

        private static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1);
            rect.pivot = new Vector2(.5f, 1); rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height); rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }
    }
}
