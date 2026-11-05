using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CounterAttack.Editor
{
    public static class RemainingMatchPanelStylingTools
    {
        private static readonly Color Navy = new Color32(30, 69, 89, 255);
        private static readonly Color Teal = new Color32(17, 123, 113, 255);
        private static readonly Color Mint = new Color32(224, 242, 237, 255);
        private static readonly Color Pale = new Color32(236, 242, 244, 255);
        private static Sprite rounded;
        private static TMP_FontAsset heading;

        [MenuItem("CounterAttack/Room/Finish Match Panel Styling")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Requires edit mode.");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != "Assets/Scenes/Room.unity" || UnityEngine.SceneManagement.SceneManager.sceneCount != 1)
                throw new InvalidOperationException("Open Room alone.");
            var menu = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/UI/Cards/MenuCard.prefab");
            rounded = menu.GetComponent<Image>().sprite;
            heading = menu.transform.Find("WhiteBkg/HeaderPanel/TitleText").GetComponent<TMP_Text>().font;
            StyleDuel(Object.FindAnyObjectByType<DuelResolutionPanelController>());
            StyleHeader(Object.FindAnyObjectByType<HeaderChallengePanelController>());
            StyleStoppage(Object.FindAnyObjectByType<StoppageActionsPanelController>());
            StyleOrder(Object.FindAnyObjectByType<PenaltyShootoutOrderPanelView>(FindObjectsInactive.Include));
            StyleSubstitutions(Object.FindAnyObjectByType<SubstitutionMenuView>(FindObjectsInactive.Include));
            StyleEndGame(GameObject.Find("MainCanvas").transform.Find("EndGamePanel/RecapPanel") as RectTransform);
            StylePauseOverlays(Object.FindAnyObjectByType<PauseMenuManager>());
            SubstitutionReadabilityStylingTools.ApplyTo(Object.FindAnyObjectByType<SubstitutionMenuView>(FindObjectsInactive.Include),
                Object.FindAnyObjectByType<PauseMenuManager>());
            SubstitutionReadabilityStylingTools.AuthorLineupIcons();
            SubstitutionReadabilityStylingTools.AuthorHoverReadability();
            EnsureDiceOutlines();
            FooterButtonStylingTools.StyleOpenRoom();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void StyleDuel(DuelResolutionPanelController p)
        {
            var recoveryTitle = p.looseRecoveryContent.transform.Find("Situation")?.GetComponent<TMP_Text>();
            if (recoveryTitle != null) Title(recoveryTitle, 30);
            foreach (var player in new[] { p.leftPlayer, p.rightPlayer })
            {
                var column = player.playerName.transform.parent;
                var stats = Box("AttributeCard", column, Pale);
                Top(stats, 150, 86, -48);
                Inset(player.attribute, stats, 28, Navy);
                var chance = Box("ProbabilityCard", column, Mint);
                Top(chance, 240, 42, -48);
                Inset(player.modifier, chance, 23, Teal);
                player.modifierBackground = chance.GetComponent<Image>();
                player.modifierBackground.enabled = !string.IsNullOrWhiteSpace(player.modifier.text);
                var roll = Box("RollCard", column, Pale);
                Top(roll, 288, 104, -48);
                player.die.transform.SetParent(roll, false);
                Place((RectTransform)player.die.transform, .25f, -12, 80, 80);
                player.total.transform.SetParent(roll, false);
                Place(player.total.rectTransform, .67f, -20, 310, 64);
                Label(player.total, 27, Navy);
                var guidance = Box("GuidanceCard", column, Mint);
                Top(guidance, 406, 72, -48);
                Inset(player.guidance, guidance, 23, Teal);
                player.playerName.font = heading;
                Label(player.playerName, 36, Navy);
            }
            EditorUtility.SetDirty(p);
        }

        public static void EnsureDiceOutlines()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Requires edit mode.");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            foreach (var die in scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<DuelDieView>(true)))
            {
                if (die.background == null) continue;
                if (die.border == null)
                {
                    die.border = die.background.GetComponent<Outline>();
                    if (die.border == null) die.border = die.background.gameObject.AddComponent<Outline>();
                }
                die.border.effectDistance = new Vector2(3, -3);
                die.border.useGraphicAlpha = false;
                die.RefreshBorderContrast();
                EditorUtility.SetDirty(die);
            }
            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static void StylePauseOverlays(PauseMenuManager m)
        {
            var save = (RectTransform)m.saveAsOverlay.transform.Find("SaveAsPanel");
            Center(save, 1200, 700); DisableLayout(save); Frame(save, 122);
            var title = save.Find("Title").GetComponent<TMP_Text>();
            Top(title.rectTransform, 40, 80, -96); Title(title, 54);
            Top((RectTransform)m.saveNameInput.transform, 150, 90, -96); Input(m.saveNameInput, 30);
            Top(m.savePreviewText.rectTransform, 260, 64, -96); Label(m.savePreviewText, 25, Navy);
            Top(m.saveModalStatusText.rectTransform, 350, 84, -96); Label(m.saveModalStatusText, 25, Navy);
            Bottom((RectTransform)m.saveAsCancelButton.transform, 24, 74, 320);
            m.saveAsCancelButton.GetComponent<RectTransform>().anchoredPosition += new Vector2(-180, 0);
            Bottom((RectTransform)m.saveAsSaveButton.transform, 24, 74, 320);
            m.saveAsSaveButton.GetComponent<RectTransform>().anchoredPosition += new Vector2(180, 0);
            Button(m.saveAsCancelButton, true); Button(m.saveAsSaveButton, true);
            var confirm = (RectTransform)m.overwriteConfirmPanel.transform;
            DisableLayout(confirm); Bottom(confirm, 24, 220, 1104);
            var confirmImage = confirm.GetComponent<Image>(); if (confirmImage != null) confirmImage.color = Color.clear;
            var question = confirm.Find("Label").GetComponent<TMP_Text>();
            Top(question.rectTransform, 0, 64, 0); Label(question, 28, Navy);
            Bottom((RectTransform)m.overwriteCancelButton.transform, 0, 74, 320);
            m.overwriteCancelButton.GetComponent<RectTransform>().anchoredPosition += new Vector2(-180, 0);
            Bottom((RectTransform)m.overwriteReplaceButton.transform, 0, 74, 320);
            m.overwriteReplaceButton.GetComponent<RectTransform>().anchoredPosition += new Vector2(180, 0);
            Button(m.overwriteCancelButton, true); Button(m.overwriteReplaceButton, true);
            var note = (RectTransform)m.tacticalNoteOverlay.transform;
            Center(note, 1500, 1220); DisableLayout(note); Frame(note, 122);
            var noteTitle = note.Find("Title").GetComponent<TMP_Text>();
            Top(noteTitle.rectTransform, 40, 80, -96); Title(noteTitle, 54);
            Top(m.tacticalNoteDecisionText.rectTransform, 150, 380, -96); Label(m.tacticalNoteDecisionText, 26, Navy);
            m.tacticalNoteDecisionText.alignment = TextAlignmentOptions.TopLeft;
            Top((RectTransform)m.tacticalNoteInput.transform, 570, 320, -96); Input(m.tacticalNoteInput, 28);
            Top(m.tacticalNoteStatusText.rectTransform, 930, 110, -96); Label(m.tacticalNoteStatusText, 25, Navy);
            var buttons = (RectTransform)note.Find("Buttons"); DisableLayout(buttons); Bottom(buttons, 24, 74, 1404);
            Bottom((RectTransform)m.cancelTacticalNoteButton.transform, 0, 74, 400);
            m.cancelTacticalNoteButton.GetComponent<RectTransform>().anchoredPosition += new Vector2(-220, 0);
            Bottom((RectTransform)m.saveTacticalNoteButton.transform, 0, 74, 400);
            m.saveTacticalNoteButton.GetComponent<RectTransform>().anchoredPosition += new Vector2(220, 0);
            Button(m.cancelTacticalNoteButton, true); Button(m.saveTacticalNoteButton, true);
            EditorUtility.SetDirty(m);
        }

        private static void Input(TMP_InputField input, int fontSize)
        {
            Surface(input.GetComponent<Image>(), Pale);
            if (input.textComponent != null)
            {
                input.textComponent.color = Navy; input.textComponent.fontSize = fontSize;
                input.textComponent.enableAutoSizing = false;
                input.textComponent.alignment = input.lineType == TMP_InputField.LineType.SingleLine
                    ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.TopLeft;
            }
            var placeholder = input.placeholder as TMP_Text;
            if (placeholder != null) Label(placeholder, fontSize, new Color32(75, 107, 123, 255));
            if (placeholder != null) placeholder.alignment = input.lineType == TMP_InputField.LineType.SingleLine
                ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.TopLeft;
            input.customCaretColor = true; input.caretColor = Navy; input.selectionColor = Mint;
            input.textViewport.offsetMin = new Vector2(24, 16);
            input.textViewport.offsetMax = new Vector2(-24, -16);
        }

        private static void StyleHeader(HeaderChallengePanelController p)
        {
            Frame(p.panel, 82);
            Top(p.title.rectTransform, 32, 56, -80); Title(p.title, 44);
            foreach (var text in new[] { p.attackTeam, p.defenseTeam })
            { Top(text.rectTransform, 100, 44, -48); Label(text, 27, Navy); }
            foreach (var row in p.attackers.Concat(p.defenders))
            {
                var rect = (RectTransform)row.root.transform;
                rect.sizeDelta = new Vector2(-80, rect.sizeDelta.y);
                row.highlight.sprite = rounded; row.highlight.type = Image.Type.Sliced;
                row.highlight.color = Pale;
                var oldBorder = rect.Find("Border"); if (oldBorder != null) oldBorder.gameObject.SetActive(false);
                Top(row.player.rectTransform, 4, 38, -32); row.player.font = heading; Label(row.player, 28, Navy);
                Top(row.attribute.rectTransform, 44, 46, -32); Label(row.attribute, 22, Navy);
                HeaderChallengePanelEditorTools.ConfigureContestantDice(row);
                Top(row.result.rectTransform, 94, 28, -160); row.result.rectTransform.anchoredPosition += new Vector2(60, 0);
                Label(row.result, 22, Navy);
                Top(row.status.rectTransform, 124, 28, -160); row.status.rectTransform.anchoredPosition += new Vector2(60, 0);
                Label(row.status, 19, Teal);
            }
            Top(p.guidance.rectTransform, 668, 96, -80); Label(p.guidance, 25, Navy);
            foreach (var summary in new[] { p.attackBest, p.defenseBest })
            {
                var column = summary == p.attackBest ? p.leftColumn : p.rightColumn;
                var tile = Box("BestScoreCard", column, Mint);
                Top(tile, 776, 72, -80);
                Inset(summary, tile, 26, Teal);
            }
            Top(p.outcome.rectTransform, 864, 64, -80); Label(p.outcome, 30, Navy);
            Label(p.footer, 25, Color.white);
            foreach (var text in p.scatter.GetComponentsInChildren<TMP_Text>(true))
                if (text.GetComponentInParent<DuelDieView>() == null) Label(text, 24, Navy);
            var recovery = p.recoveryPlayer;
            foreach (var text in new[] { recovery.playerName, recovery.teamName, recovery.attribute,
                recovery.modifier, recovery.total, recovery.guidance }) Label(text, 25, Navy);
            recovery.playerName.font = heading;
            var recoveryTile = recovery.playerName.transform.parent.parent.GetComponent<Image>();
            if (recoveryTile != null) { recoveryTile.sprite = rounded; recoveryTile.type = Image.Type.Sliced; recoveryTile.color = Pale; }
            var border = recovery.playerName.transform.parent.parent.Find("Border");
            if (border != null) border.gameObject.SetActive(false);
            var recoveryTitle = p.recoveryContent.transform.Find("RecoveryTitle")?.GetComponent<TMP_Text>();
            if (recoveryTitle != null) Title(recoveryTitle, 28);
            EditorUtility.SetDirty(p);
        }

        private static void StyleStoppage(StoppageActionsPanelController p)
        {
            Frame(p.panel, 122);
            Top(p.title.rectTransform, 40, 64, -80); Title(p.title, 44);
            Top(p.situation.rectTransform, 114, 48, -80); Label(p.situation, 28, Navy);
            Label(p.rollingTeam, 30, Navy); Label(p.rule, 28, Navy); Label(p.result, 30, Teal);
            Label(p.footer, 26, Color.white);
            EditorUtility.SetDirty(p);
        }

        private static void StyleOrder(PenaltyShootoutOrderPanelView v)
        {
            var root = (RectTransform)v.transform;
            Center(root, 1800, 1440); DisableLayout(root); Frame(root, 122);
            Top(v.titleText.rectTransform, 40, 80, -96); Title(v.titleText, 56);
            var columns = (RectTransform)v.homeRowsContainer.parent.parent;
            DisableLayout(columns); Top(columns, 150, 1130, -96);
            for (int side = 0; side < 2; side++)
            {
                var container = (RectTransform)(side == 0 ? v.homeRowsContainer : v.awayRowsContainer);
                var column = (RectTransform)container.parent;
                DisableLayout(column); Half(column, side, 0, 1130); Surface(column.GetComponent<Image>(), Color.white);
                var header = side == 0 ? v.homeHeaderText : v.awayHeaderText;
                Top(header.rectTransform, 0, 80, -32); Title(header, 38);
                DisableLayout(container); Top(container, 96, 1012, -32);
                var rows = v.GetRows(side == 0);
                for (int i = 0; i < rows.Length; i++)
                {
                    var row = rows[i]; DisableLayout(row.transform); Top((RectTransform)row.transform, i * 84, 76, 0);
                    Surface(row.GetComponent<Image>(), i % 2 == 0 ? Pale : Mint);
                    Label(row.nameText, 28, Navy); Label(row.shootingText, 26, Teal);
                }
            }
            if (v.rowTemplate != null) v.rowTemplate.gameObject.SetActive(false);
            Bottom((RectTransform)v.startButton.transform, 24, 74, 620);
            Button(v.startButton, true);
            EditorUtility.SetDirty(v);
        }

        private static void StyleSubstitutions(SubstitutionMenuView v)
        {
            var root = (RectTransform)v.transform; Center(root, 2260, 1480); Frame(root, 122);
            Top(v.titleText.rectTransform, 40, 80, -96); Title(v.titleText, 56);
            var body = (RectTransform)root.Find("Body"); DisableLayout(body); Top(body, 146, 1180, -96);
            for (int side = 0; side < 2; side++)
            {
                var title = side == 0 ? v.homeTeamTitleText : v.awayTeamTitleText;
                var column = (RectTransform)title.transform.parent;
                Half(column, side, 0, 1180); DisableLayout(column); Surface(column.GetComponent<Image>(), Color.white);
                Top(title.rectTransform, 0, 64, -24); Title(title, 34);
                var roster = column.Find("RosterArea") as RectTransform; DisableLayout(roster); Top(roster, 80, 680, -24);
                for (int list = 0; list < 2; list++)
                {
                    var content = (RectTransform)(side == 0 ? (list == 0 ? v.homePlayingListContent : v.homeBenchListContent)
                        : (list == 0 ? v.awayPlayingListContent : v.awayBenchListContent));
                    var listRoot = (RectTransform)content.parent; Half(listRoot, list, 0, 680); DisableLayout(listRoot);
                    var header = listRoot.GetComponentsInChildren<TMP_Text>(true).First(text => text.transform.parent == listRoot);
                    Top(header.rectTransform, 0, 48, -20); Label(header, 26, Teal);
                    Top(content, 58, 600, -20); DisableLayout(content);
                    var rows = side == 0 ? (list == 0 ? v.homePlayingRows : v.homeBenchRows)
                        : (list == 0 ? v.awayPlayingRows : v.awayBenchRows);
                    for (int i = 0; i < rows.Length; i++) { Top(rows[i].rectTransform, i * 50, 46, 0); Label(rows[i], 23, Navy); }
                }
                var remaining = side == 0 ? v.homeSubsRemainingText : v.awaySubsRemainingText;
                Top(remaining.rectTransform, 776, 44, -24); Label(remaining, 26, Teal);
                var rowRoot = side == 0 ? v.homeSubstitutionRowsContainer : v.awaySubstitutionRowsContainer;
                Top((RectTransform)rowRoot, 838, 296, -24); DisableLayout(rowRoot);
                var subs = v.GetRows(side == 0);
                for (int i = 0; i < subs.Length; i++)
                {
                    Top((RectTransform)subs[i].transform, i * 74, 66, 0);
                    var layout = subs[i].GetComponent<HorizontalLayoutGroup>();
                    if (layout != null) { layout.spacing = 12; layout.childControlWidth = layout.childControlHeight = true; layout.childForceExpandWidth = layout.childForceExpandHeight = true; }
                    foreach (var dropdown in new[] { subs[i].outgoingDropdown, subs[i].incomingDropdown })
                    {
                        Surface(dropdown.GetComponent<Image>(), Pale);
                        ((RectTransform)dropdown.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 62);
                        var element = dropdown.GetComponent<LayoutElement>(); if (element == null) element = dropdown.gameObject.AddComponent<LayoutElement>();
                        element.minHeight = element.preferredHeight = 62;
                        Label(dropdown.captionText, 24, Navy); Label(dropdown.itemText, 20, Navy);
                        var arrow = dropdown.transform.Find("Arrow")?.GetComponent<Image>();
                        if (arrow != null) arrow.color = Teal;
                        var arrowText = dropdown.transform.Find("Arrow")?.GetComponent<TMP_Text>();
                        if (arrowText != null) Label(arrowText, 24, Teal);
                        Surface(dropdown.template.GetComponent<Image>(), Color.white);
                        var option = dropdown.itemText.GetComponentInParent<Toggle>(true);
                        if (option != null)
                        {
                            Surface(option.GetComponent<Image>(), Pale);
                            var colors = option.colors;
                            colors.normalColor = Color.white;
                            colors.highlightedColor = Mint;
                            colors.selectedColor = Mint;
                            colors.pressedColor = new Color32(177, 224, 213, 255);
                            option.colors = colors;
                            var optionHeight = option.GetComponent<LayoutElement>();
                            if (optionHeight != null) optionHeight.minHeight = optionHeight.preferredHeight = 42;
                            ((RectTransform)option.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 42);
                        }
                    }
                }
            }
            var footer = (RectTransform)root.Find("Footer"); DisableLayout(footer);
            Bottom(footer, 24, 74, 2164);
            Bottom((RectTransform)v.backButton.transform, 0, 74, 320); v.backButton.GetComponent<RectTransform>().anchoredPosition += new Vector2(-180, 0);
            Bottom((RectTransform)v.confirmButton.transform, 0, 74, 320); v.confirmButton.GetComponent<RectTransform>().anchoredPosition += new Vector2(180, 0);
            Button(v.backButton, true); Button(v.confirmButton, true);
            EditorUtility.SetDirty(v);
        }

        private static void StyleEndGame(RectTransform panel)
        {
            Center(panel, 1400, 1240);
            Frame(panel, 122);
            foreach (var text in panel.GetComponentsInChildren<TMP_Text>(true)) text.color = Navy;
            var title = panel.Find("Title").GetComponent<TMP_Text>(); Top(title.rectTransform, 40, 64, -80); Title(title, 54);
            var scroll = (RectTransform)panel.Find("RecapScroll"); scroll.offsetMin = new Vector2(48, 146); scroll.offsetMax = new Vector2(-48, -110);
            foreach (var image in scroll.GetComponentsInChildren<Image>(true))
                if (image.color.a > .1f) Surface(image, Pale);
            var content = scroll.GetComponent<ScrollRect>().content;
            var stats = content.Find("StatsGrid");
            int count = 0;
            foreach (Transform column in stats)
            {
                count = Mathf.Max(count, column.childCount);
                foreach (var text in column.GetComponentsInChildren<TMP_Text>(true))
                {
                    Label(text, 24, Navy);
                    var element = text.GetComponent<LayoutElement>();
                    if (element != null) element.minHeight = element.preferredHeight = 34;
                    text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 34);
                    if (new[] { "ATTACKING", "PASSING", "DUELS", "DISCIPLINE", "OPTA STATS" }.Contains(text.text)) Title(text, 24);
                }
            }
            stats.GetComponent<LayoutElement>().minHeight = stats.GetComponent<LayoutElement>().preferredHeight = count * 36;
            ((RectTransform)stats).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, count * 36);
            foreach (var text in content.Find("ScoreRecap").GetComponentsInChildren<TMP_Text>(true)) Label(text, 28, Navy);
            var button = panel.Find("BackToMainMenuButton").GetComponent<Button>();
            Bottom((RectTransform)button.transform, 24, 74, 500); Button(button, true);
        }

        private static RectTransform Box(string name, Transform parent, Color color)
        {
            var found = parent.Find(name);
            var rect = found != null ? (RectTransform)found : (RectTransform)new GameObject(name, typeof(RectTransform), typeof(Image)).transform;
            rect.SetParent(parent, false); rect.SetAsFirstSibling(); rect.gameObject.layer = parent.gameObject.layer;
            Surface(rect.GetComponent<Image>(), color); return rect;
        }

        private static void Frame(RectTransform root, float footer)
        {
            var image = root.GetComponent<Image>(); if (image == null) image = root.gameObject.AddComponent<Image>();
            Surface(image, Navy);
            var white = Box("WhiteSurface", root, Color.white); white.anchorMin = Vector2.zero; white.anchorMax = Vector2.one;
            white.offsetMin = new Vector2(24, footer); white.offsetMax = new Vector2(-24, -24);
            white.GetComponent<Image>().raycastTarget = false;
        }

        private static void Surface(Image image, Color color)
        { if (image == null) return; image.sprite = rounded; image.type = Image.Type.Sliced; image.color = color; }
        private static void Title(TMP_Text text, int size) { text.font = heading; Label(text, size, Navy); }
        private static void Label(TMP_Text text, int size, Color color)
        { if (text == null) return; text.color = color; text.fontSize = text.fontSizeMax = size; text.fontSizeMin = 16; text.enableAutoSizing = true; text.textWrappingMode = TextWrappingModes.Normal; }
        private static void Inset(TMP_Text text, RectTransform parent, int size, Color color)
        { text.transform.SetParent(parent, false); text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one; text.rectTransform.offsetMin = new Vector2(16, 8); text.rectTransform.offsetMax = new Vector2(-16, -8); Label(text, size, color); }
        private static void Top(RectTransform rect, float top, float height, float width)
        { rect.anchorMin = new Vector2(0, 1); rect.anchorMax = Vector2.one; rect.pivot = new Vector2(.5f, 1); rect.anchoredPosition = new Vector2(0, -top); rect.sizeDelta = new Vector2(width, height); }
        private static void Place(RectTransform rect, float x, float top, float width, float height)
        { rect.anchorMin = rect.anchorMax = new Vector2(x, 1); rect.pivot = new Vector2(.5f, 1); rect.anchoredPosition = new Vector2(0, top); rect.sizeDelta = new Vector2(width, height); }
        private static void Half(RectTransform rect, int side, float top, float height)
        { rect.anchorMin = new Vector2(side * .5f, 1); rect.anchorMax = new Vector2((side + 1) * .5f, 1); rect.pivot = new Vector2(.5f, 1); rect.anchoredPosition = new Vector2(0, -top); rect.sizeDelta = new Vector2(-24, height); }
        private static void Bottom(RectTransform rect, float bottom, float height, float width)
        { rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, 0); rect.anchoredPosition = new Vector2(0, bottom); rect.sizeDelta = new Vector2(width, height); }
        private static void Center(RectTransform rect, float width, float height)
        { rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f); rect.anchoredPosition = Vector2.zero; rect.sizeDelta = new Vector2(width, height); }
        private static void DisableLayout(Transform target)
        { foreach (var layout in target.GetComponents<LayoutGroup>()) layout.enabled = false; foreach (var fitter in target.GetComponents<ContentSizeFitter>()) fitter.enabled = false; }
        private static void Button(Button button, bool footer)
        { Surface(button.GetComponent<Image>(), footer ? Navy : Mint); Label(button.GetComponentInChildren<TMP_Text>(true), 28, footer ? Color.white : Navy); }

        public static void RenderPreview(RectTransform panel, string path, int width = 1600, int height = 1200)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Requires edit mode.");
            var canvas = panel.GetComponentInParent<Canvas>();
            var scaler = canvas.GetComponent<CanvasScaler>();
            var roots = canvas.transform.Cast<Transform>().ToArray();
            var states = roots.Select(t => t.gameObject.activeSelf).ToArray();
            var view = panel.transform; while (view.parent != canvas.transform) view = view.parent;
            var mode = canvas.renderMode; var oldCamera = canvas.worldCamera; var distance = canvas.planeDistance;
            var resolution = scaler.referenceResolution; var geometry = EditorJsonUtility.ToJson(panel);
            var oldTarget = RenderTexture.active;
            var cameraObject = new GameObject("TemporaryPanelPreview", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>(); camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.055f, .09f, .115f); camera.cullingMask = 1 << canvas.gameObject.layer;
            var target = new RenderTexture(width, height, 24);
            try
            {
                foreach (var root in roots) root.gameObject.SetActive(root == view);
                scaler.referenceResolution = new Vector2(width, height);
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
                camera.targetTexture = target;
                panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(.5f, .5f);
                panel.anchoredPosition = Vector2.zero;
                panel.localScale = Vector3.one * Mathf.Min(1, Mathf.Min((width - 100f) / panel.rect.width, (height - 100f) / panel.rect.height));
                Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;
                var texture = new Texture2D(width, height);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0); texture.Apply();
                System.IO.File.WriteAllBytes(path, ImageConversion.EncodeToPNG(texture)); Object.DestroyImmediate(texture);
            }
            finally
            {
                for (int i = 0; i < roots.Length; i++) roots[i].gameObject.SetActive(states[i]);
                scaler.referenceResolution = resolution; canvas.renderMode = mode; canvas.worldCamera = oldCamera;
                canvas.planeDistance = distance; EditorJsonUtility.FromJsonOverwrite(geometry, panel);
                RenderTexture.active = oldTarget; Object.DestroyImmediate(cameraObject); Object.DestroyImmediate(target);
                Canvas.ForceUpdateCanvases();
            }
        }
    }
}
