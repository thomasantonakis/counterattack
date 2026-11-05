using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CounterAttack.Editor
{
    public static class RollPanelSurfaceStylingTools
    {
        private static readonly Color Navy = new Color32(30, 69, 89, 255);
        private const float Border = 24f;

        [MenuItem("CounterAttack/UI/Unify Roll Panel Surfaces In Open Scene")]
        public static void ApplyOpenScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Requires edit mode.");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (UnityEngine.SceneManagement.SceneManager.sceneCount != 1)
                throw new InvalidOperationException("Open the target scene alone.");
            var sprite = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/UI/Cards/MenuCard.prefab").GetComponent<Image>().sprite;
            foreach (var p in scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<DuelResolutionPanelController>(true)))
            {
                Backdrop(p.panel, sprite, 122);
                Heading(p.title);
                p.outcome.color = Navy;
                if (p.scatterOutcome != null)
                {
                    p.scatterOutcome.color = Navy;
                    p.scatterOutcome.rectTransform.anchoredPosition = new Vector2(0,
                        p.footer.rectTransform.anchoredPosition.y + p.footer.rectTransform.rect.height + 48);
                }
                foreach (var content in new[] { p.looseBallContent, p.looseRecoveryContent })
                {
                    if (content == null) continue;
                    var image = content.GetComponent<Image>();
                    if (image == null) image = content.AddComponent<Image>();
                    image.sprite = sprite; image.type = Image.Type.Sliced; image.color = Color.white; image.raycastTarget = false;
                }
                if (p.looseBallPlayer != null) p.looseBallPlayer.color = Navy;
                foreach (var check in new[] { p.looseDirection, p.looseDistance })
                {
                    if (check.rule != null) check.rule.color = Navy;
                    if (check.result != null) check.result.color = Navy;
                    var heading = check.column != null ? check.column.Find("Heading")?.GetComponent<TMP_Text>() : null;
                    if (heading != null) heading.color = Navy;
                }
                var recovery = p.looseRecoveryPlayer;
                foreach (var text in new[] { recovery.playerName, recovery.teamName, recovery.role,
                    recovery.attribute, recovery.modifier, recovery.total, recovery.guidance })
                    if (text != null) text.color = Navy;
                EditorUtility.SetDirty(p);
            }
            foreach (var p in scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<DraftOrderPanelController>(true)))
            {
                Backdrop(p.panel, sprite, 104);
                Heading(p.title);
                p.outcome.color = Navy;
                EditorUtility.SetDirty(p);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void Backdrop(RectTransform panel, Sprite sprite, float footerHeight)
        {
            var found = panel.Find("WhiteSurface");
            var rect = found != null ? (RectTransform)found
                : (RectTransform)new GameObject("WhiteSurface", typeof(RectTransform), typeof(Image)).transform;
            rect.SetParent(panel, false); rect.SetAsFirstSibling();
            rect.gameObject.layer = panel.gameObject.layer;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(Border, footerHeight);
            rect.offsetMax = new Vector2(-Border, -Border);
            var image = rect.GetComponent<Image>();
            image.sprite = sprite; image.type = Image.Type.Sliced; image.color = Color.white; image.raycastTarget = false;
            panel.GetComponent<Image>().color = Navy;
        }

        private static void Heading(TMP_Text title)
        {
            title.color = Navy;
            title.rectTransform.anchoredPosition = new Vector2(0, -40);
            title.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 64);
        }
    }
}
