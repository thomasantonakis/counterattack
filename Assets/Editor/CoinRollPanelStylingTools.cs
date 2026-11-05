using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CounterAttack.Editor
{
    public static class CoinRollPanelStylingTools
    {
        private static readonly Color Navy = new Color32(30, 69, 89, 255);
        private static readonly Color Teal = new Color32(17, 123, 113, 255);
        private static readonly Color Mint = new Color32(224, 242, 237, 255);

        [MenuItem("CounterAttack/UI/Style Coin Rolls In Open Scene")]
        public static void ApplyOpenScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Requires edit mode.");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (UnityEngine.SceneManagement.SceneManager.sceneCount != 1)
                throw new InvalidOperationException("Open the target scene alone.");
            var panels = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<DraftOrderPanelController>(true)).ToArray();
            if (panels.Length == 0) throw new InvalidOperationException("No authored coin-roll panels in this scene.");
            var menu = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/UI/Cards/MenuCard.prefab");
            var sprite = menu.GetComponent<Image>().sprite;
            var heading = menu.transform.Find("WhiteBkg/HeaderPanel/TitleText").GetComponent<TMP_Text>().font;
            var body = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/UI/Buttons/Button.prefab").GetComponentInChildren<TMP_Text>(true).font;
            foreach (var p in panels)
            {
                if (!p.HasSceneBindings) throw new InvalidOperationException(p.name + " has incomplete bindings.");
                Surface(p.panel, sprite, Navy, true);
                Label(p.title, heading, 46, Color.white);
                Label(p.outcome, body, 32, Color.white);
                Label(p.footer, body, 28, Color.white);
                Team(p.homeTeam, p.homeResult, p.homeDie, sprite, heading, body);
                Team(p.awayTeam, p.awayResult, p.awayDie, sprite, heading, body);
                var toss = p.GetComponent<CoinTossPanelController>();
                if (toss != null)
                {
                    var choices = toss.choices.transform;
                    var existing = choices.Find("ChoicesSurface");
                    var background = existing != null ? (RectTransform)existing
                        : (RectTransform)new GameObject("ChoicesSurface", typeof(RectTransform)).transform;
                    background.gameObject.layer = choices.gameObject.layer;
                    background.SetParent(choices, false); background.SetAsFirstSibling();
                    Top(background, 615, 1392, 360);
                    Surface(background, sprite, Color.white, false);
                    foreach (var button in new[] { toss.kickoffButton, toss.leftToRightButton, toss.rightToLeftButton })
                    {
                        Surface((RectTransform)button.transform, sprite, Mint, true);
                        ((RectTransform)button.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 1300);
                        Label(button.GetComponentInChildren<TMP_Text>(true), body, 32, Navy);
                        var colors = button.colors;
                        colors.normalColor = Color.white;
                        colors.highlightedColor = new Color32(177, 224, 213, 255);
                        colors.selectedColor = colors.highlightedColor;
                        colors.pressedColor = new Color32(129, 195, 182, 255);
                        colors.disabledColor = new Color32(179, 187, 190, 170);
                        button.colors = colors;
                        EditorUtility.SetDirty(button);
                    }
                    if (!toss.HasSceneBindings) throw new InvalidOperationException("Coin-toss choices lost bindings.");
                }
                EditorUtility.SetDirty(p);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void Team(TMP_Text name, TMP_Text result, DuelDieView die, Sprite sprite, TMP_FontAsset heading, TMP_FontAsset body)
        {
            var card = (RectTransform)name.transform.parent;
            Surface(card, sprite, Color.white, false);
            name.rectTransform.anchoredPosition = new Vector2(0, -20);
            Label(name, heading, 40, Navy);
            ((RectTransform)die.transform).anchoredPosition = new Vector2(0, -110);
            result.rectTransform.anchoredPosition = new Vector2(0, -310);
            Label(result, body, 32, Teal);
        }

        private static void Label(TMP_Text text, TMP_FontAsset font, int size, Color color)
        {
            text.font = font; text.fontSize = size; text.fontSizeMax = size; text.fontSizeMin = 20;
            text.enableAutoSizing = true; text.color = color;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.alignment = TextAlignmentOptions.Center;
        }

        private static void Top(RectTransform rect, float top, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, 1);
            rect.anchoredPosition = new Vector2(0, -top); rect.sizeDelta = new Vector2(width, height);
        }

        private static void Surface(RectTransform rect, Sprite sprite, Color color, bool raycast)
        {
            var image = rect.GetComponent<Image>();
            if (image == null) image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite; image.type = Image.Type.Sliced; image.color = color; image.raycastTarget = raycast;
        }
    }
}
