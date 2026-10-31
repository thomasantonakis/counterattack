using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CounterAttack.Editor
{
    public static class RoomAIThinkingPanelEditorTools
    {
        [MenuItem("CounterAttack/Room/Ensure AI Thinking Panel")]
        public static void EnsureThinkingPanel()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new System.InvalidOperationException("Stop Play Mode before authoring Room UI.");
            var pause = Object.FindAnyObjectByType<PauseMenuManager>();
            var decision = Object.FindAnyObjectByType<RoomDecisionManager>();
            if (pause?.pausePanel == null || decision == null)
                throw new System.InvalidOperationException("Open the Room scene with its existing Canvas and managers.");
            Transform parent = pause.pausePanel.transform.parent;
            var existing = parent.Find("ThinkingPanel");
            var panel = existing != null ? existing.gameObject : new GameObject("ThinkingPanel",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image), typeof(CanvasGroup));
            panel.transform.SetParent(parent, false);
            panel.layer = parent.gameObject.layer;
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -82f);
            rect.sizeDelta = new Vector2(248f, 56f);
            var background = panel.GetComponent<UnityEngine.UI.Image>();
            background.color = new Color(.035f, .09f, .19f, .94f);
            background.raycastTarget = false;
            var group = panel.GetComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;
            var labelTransform = panel.transform.Find("ThinkingText");
            var label = labelTransform != null ? labelTransform.GetComponent<TextMeshProUGUI>()
                : new GameObject("ThinkingText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI))
                    .GetComponent<TextMeshProUGUI>();
            label.transform.SetParent(panel.transform, false);
            label.gameObject.layer = panel.layer;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(12f, 4f);
            label.rectTransform.offsetMax = new Vector2(-12f, -4f);
            label.font = pause.resumeButton.GetComponentInChildren<TMP_Text>(true)?.font ?? TMP_Settings.defaultFontAsset;
            label.text = "Thinking...";
            label.fontSize = 24f;
            label.color = new Color(.72f, .94f, .9f, 1f);
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            Undo.RecordObject(decision, "Assign AI Thinking panel");
            decision.thinkingPanel = panel;
            panel.SetActive(false);
            EditorUtility.SetDirty(decision);
            EditorSceneManager.MarkSceneDirty(panel.scene);
            EditorSceneManager.SaveScene(panel.scene);
            AssetDatabase.SaveAssets();
        }
    }
}
