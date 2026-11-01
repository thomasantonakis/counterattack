using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CounterAttack.Editor
{
    /// <summary>Invoked through Unity CLI in edit mode. Authors scene objects only; no prefab.</summary>
    public static class HeaderChallengePanelEditorTools
    {
        private static TMP_FontAsset font;
        private static DuelDieView template;
        private static readonly Color Ink = new(.94f, .95f, .98f);

        public static string EnsurePanel()
        {
            RequireEditMode();
            var room = SceneManager.GetSceneByPath("Assets/Scenes/Room.unity");
            if (!room.isLoaded) throw new InvalidOperationException("Open Room first.");
            Transform canvas = null;
            HeaderManager manager = null;
            foreach (var root in room.GetRootGameObjects())
            {
                if (root.name == "MainCanvas") canvas = root.transform;
                if (manager == null) manager = root.GetComponentInChildren<HeaderManager>(true);
            }
            if (canvas == null || manager == null) throw new InvalidOperationException("Room MainCanvas or HeaderManager missing.");
            var existing = canvas.Find("HeaderChallengeUI");
            if (existing != null)
            {
                var bound = existing.GetComponent<HeaderChallengePanelController>();
                if (bound == null || !bound.HasSceneBindings) throw new InvalidOperationException("Inspect existing HeaderChallengeUI bindings.");
                manager.headerChallengePanel = bound;
                EditorUtility.SetDirty(manager);
                return "Existing HeaderChallengeUI retained.";
            }
            var duel = canvas.GetComponentInChildren<DuelResolutionPanelController>(true);
            if (duel == null || !duel.HasSceneBindings) throw new InvalidOperationException("Authored duel dice/font required.");
            font = duel.title.font;
            template = duel.leftPlayer.die;
            var rootRect = Rect("HeaderChallengeUI", canvas);
            Stretch(rootRect);
            var ui = rootRect.gameObject.AddComponent<HeaderChallengePanelController>();
            var visuals = Rect("Visuals", rootRect); Stretch(visuals); ui.visuals = visuals.gameObject;
            var blocker = Rect("InputBlocker", visuals); Stretch(blocker);
            Image(blocker, Color.clear, true);
            var consumer = blocker.gameObject.AddComponent<GameplayInputConsumer>();
            consumer.ConsumesPointerOverSelf = true; consumer.BlocksGameplayWhileActive = false;
            var panel = Rect("Panel", visuals);
            panel.anchorMin = panel.anchorMax = new Vector2(.5f, .5f); panel.pivot = new Vector2(.5f, 1);
            panel.anchoredPosition = new Vector2(0, 648);
            Image(panel, new Color(.055f, .065f, .085f, 1f), true);
            ui.panel = panel;
            panel.gameObject.AddComponent<HeaderChallengePanelDragHandle>().owner = ui;
            ui.title = Label("Title", panel, 16, 56, "HEADER PASS CHALLENGE", 38);
            var attack = Column("AttackingTeam", panel, true);
            var defense = Column("DefendingTeam", panel, false);
            ui.leftColumn = attack; ui.rightColumn = defense;
            ui.attackBest = Label("BestAttack", attack, 794, 80, "Best Attack: —\n—", 28);
            ui.defenseBest = Label("BestDefense", defense, 794, 80, "Best Defense: —\n—", 28);
            ui.attackTeam = Label("TeamRole", attack, 84, 70, "ATTACKING TEAM", 27);
            ui.defenseTeam = Label("TeamRole", defense, 84, 70, "DEFENDING TEAM", 27);
            for (int i = 0; i < 2; i++) ui.attackers[i] = Row("Attacker" + (i + 1), attack, 160 + i * 168);
            for (int i = 0; i < 3; i++) ui.defenders[i] = Row(i == 2 ? "EligibleGoalkeeper" : "Defender" + (i + 1), defense, 160 + i * 168);
            ui.guidance = Label("CurrentRollerGuidance", panel, 676, 108, "Sets the attacking team score.", 25);
            ui.guidance.enableAutoSizing = true; ui.guidance.fontSizeMin = 19; ui.guidance.fontSizeMax = 25;
            ui.outcome = Label("FinalChallengeResult", panel, 884, 60, "", 29);
            var scatter = Top("LooseBallContinuation", panel, 964, 208); ui.scatter = scatter.gameObject;
            Label("ContinuationTitle", scatter, 0, 40, "LOOSE BALL CONTINUATION", 27);
            var dir = Column("Direction", scatter, true);
            var dist = Column("Distance", scatter, false);
            ui.direction = Check(dir, "DIRECTION"); ui.distance = Check(dist, "DISTANCE");
            var footer = Rect("ControlsCountdownFooter", panel);
            footer.anchorMin = new Vector2(0, 0); footer.anchorMax = new Vector2(1, 0);
            footer.pivot = new Vector2(.5f, 0); footer.anchoredPosition = new Vector2(0, 12);
            footer.sizeDelta = new Vector2(-48, 46);
            ui.footer = Text(footer, "[R] roll", 24);
            manager.headerChallengePanel = ui;
            EnsureRecoveryExpansion(ui);
            Preview(ui, 0);
            EditorUtility.SetDirty(manager); EditorUtility.SetDirty(ui);
            EditorSceneManager.MarkSceneDirty(room);
            EditorSceneManager.SaveScene(room);
            return "HeaderChallengeUI authored under MainCanvas with five contestant rows and two scatter dice.";
        }

        [MenuItem("CounterAttack/Room/Update Header Challenge Layout")]
        public static void UpdateLayout()
        {
            var ui = Find();
            ui.title = ui.panel.Find("Title").GetComponent<TMP_Text>();
            ui.compactHeight = 1020; ui.expandedHeight = 1280;
            SetTop(ui.attackTeam.rectTransform, 84);
            SetTop(ui.defenseTeam.rectTransform, 84);
            SetTop(ui.attackBest.rectTransform, 794);
            SetTop(ui.defenseBest.rectTransform, 794);
            SetTop(ui.guidance.rectTransform, 676);
            SetTop(ui.outcome.rectTransform, 884);
            SetTop((RectTransform)ui.scatter.transform, 964);
            foreach (var rows in new[] { ui.attackers, ui.defenders })
                for (int i = 0; i < rows.Length; i++)
                {
                    var row = rows[i];
                    SetTop((RectTransform)row.root.transform, 160 + i * 168);
                    var result = row.result.rectTransform;
                    result.anchorMin = new Vector2(0, 1); result.anchorMax = new Vector2(1, 1);
                    result.pivot = new Vector2(.5f, 1);
                    result.sizeDelta = new Vector2(-40, 38);
                    result.anchoredPosition = new Vector2(0, -82);
                    row.result.alignment = TextAlignmentOptions.Center;
                    EnsureTileBorder((RectTransform)row.root.transform);
                }
            Preview(ui, 2);
            EditorUtility.SetDirty(ui);
            EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);
        }
        [MenuItem("CounterAttack/Room/Ensure Header Loose Ball Expansion")]
        public static void EnsureLooseBallExpansion()
        {
            var ui = Find();
            EnsureRecoveryExpansion(ui);
            Preview(ui, 5);
            EditorUtility.SetDirty(ui);
            EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);
        }
        private static void EnsureRecoveryExpansion(HeaderChallengePanelController ui)
        {
            if (ui.recoveryContent != null) return;
            font = ui.title.font;
            template = ui.attackers[0].die;
            ui.recoveryHeight = 1610;
            var content = Top("LooseBallRecovery", ui.panel, 1188, 320);
            ui.recoveryContent = content.gameObject;
            Label("RecoveryTitle", content, 0, 42, "LOOSE BALL RECOVERY", 28);
            var body = Top("RecoveryTile", content, 52, 260);
            EnsureTileBorder(body);
            var details = Column("Details", body, true);
            details.anchorMax = new Vector2(.7f, 1);
            var rollColumn = Column("Roll", body, false);
            rollColumn.anchorMin = new Vector2(.7f, 1);
            ui.recoveryPlayer = new DuelPlayerPanelFields
            {
                playerName = Label("RecoveryPlayer", details, 6, 44, "#5 Recovering defender", 29),
                teamName = Label("RecoveryTeam", details, 50, 38, "Defending team", 25),
                attribute = Label("RecoveryAttribute", details, 92, 40, "Tackling: 5", 26),
                modifier = Label("ExpectedRecovery", details, 136, 40, "xRecovery 0.00", 25),
                guidance = Label("RecoveryGuidance", details, 182, 65, "Success: roll 5–6", 24),
                die = Die(rollColumn, new Vector2(145, -26), 80),
                total = Label("RecoveryResult", rollColumn, 125, 100, "Awaiting roll", 25)
            };
            var dieRect = (RectTransform)ui.recoveryPlayer.die.transform;
            dieRect.anchorMin = dieRect.anchorMax = dieRect.pivot = new Vector2(.5f, 1);
            dieRect.anchoredPosition = new Vector2(0, -26);
            content.gameObject.SetActive(false);
        }
        [MenuItem("CounterAttack/Room/Preview Header Challenge/Loose ball recovery")]
        public static void PreviewRecovery() => Preview(Find(), 5);
        [MenuItem("CounterAttack/Room/Preview Header Challenge/Header at goal tie scatter")]
        public static void PreviewAtGoalScatter() => Preview(Find(), 6);

        private static void SetTop(RectTransform rect, float top)
            => rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, -top);

        private static void EnsureTileBorder(RectTransform tile)
        {
            if (tile.Find("Border") != null) return;
            var border = Rect("Border", tile); Stretch(border);
            BorderEdge("Top", border, new Vector2(0, 1), Vector2.one, new Vector2(0, 2));
            BorderEdge("Bottom", border, Vector2.zero, new Vector2(1, 0), new Vector2(0, 2));
            BorderEdge("Left", border, Vector2.zero, new Vector2(0, 1), new Vector2(2, 0));
            BorderEdge("Right", border, new Vector2(1, 0), Vector2.one, new Vector2(2, 0));
        }
        private static void BorderEdge(string name, RectTransform parent, Vector2 min, Vector2 max, Vector2 size)
        {
            var edge = Rect(name, parent); edge.anchorMin = min; edge.anchorMax = max;
            edge.anchoredPosition = Vector2.zero; edge.sizeDelta = size;
            Image(edge, new Color(.65f, .7f, .78f, .8f));
        }

        private static HeaderContestantFields Row(string name, RectTransform parent, float top)
        {
            var row = Top(name, parent, top, 156);
            var highlight = Image(row, Color.clear);
            var fields = new HeaderContestantFields { root = row.gameObject, highlight = highlight };
            fields.player = Label("PlayerNameNumber", row, 0, 42, "#9 Player", 29);
            fields.player.enableAutoSizing = true; fields.player.fontSizeMin = 20; fields.player.fontSizeMax = 29;
            fields.player.textWrappingMode = TextWrappingModes.NoWrap;
            fields.attribute = Label("AttributeAndModifier", row, 44, 36, "Heading: 5", 24);
            fields.die = Die(row, new Vector2(30, -80), 64);
            var result = Top("RollAndTotal", row, 82, 38);
            fields.result = Text(result, "Roll — - Total: —", 26);
            var status = Top("ContestantStatus", row, 120, 34);
            fields.status = Text(status, "Pending", 23);
            EnsureTileBorder(row);
            return fields;
        }
        private static DuelCheckPanelFields Check(RectTransform column, string heading)
        {
            var fields = new DuelCheckPanelFields { column = column };
            fields.rule = Label("RollLabel", column, 44, 36, heading, 24);
            fields.die = Die(column, new Vector2(30, -92), 64);
            var result = Top("RollResult", column, 94, 70);
            result.offsetMin = new Vector2(116, result.offsetMin.y); result.offsetMax = new Vector2(-24, result.offsetMax.y);
            fields.result = Text(result, "Pending", 25);
            return fields;
        }
        private static DuelDieView Die(RectTransform parent, Vector2 position, float size)
        {
            var rect = Rect("Die", parent);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = position; rect.sizeDelta = Vector2.one * size;
            var die = rect.gameObject.AddComponent<DuelDieView>();
            die.background = Image(rect, new Color(.18f, .35f, .65f));
            die.border = rect.gameObject.AddComponent<UnityEngine.UI.Outline>();
            die.border.effectDistance = new Vector2(2, -2); die.border.useGraphicAlpha = false;
            var face = Rect("Face", rect); Stretch(face); face.offsetMin = Vector2.one * 5; face.offsetMax = Vector2.one * -5;
            die.face = Image(face, Color.white);
            var fallback = Rect("FallbackSymbol", rect); Stretch(fallback); die.fallbackText = Text(fallback, "—", 36);
            die.numericFaces = (Sprite[])template.numericFaces.Clone(); die.jackpotFace = template.jackpotFace;
            die.secondsPerFace = template.secondsPerFace;
            die.Clear(); return die;
        }
        private static RectTransform Rect(string name, Transform parent)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false); return (RectTransform)obj.transform;
        }
        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
        private static RectTransform Top(string name, Transform parent, float top, float height)
        {
            var rect = Rect(name, parent);
            rect.anchorMin = new Vector2(0, 1); rect.anchorMax = new Vector2(1, 1); rect.pivot = new Vector2(.5f, 1);
            rect.anchoredPosition = new Vector2(0, -top); rect.sizeDelta = new Vector2(-40, height); return rect;
        }
        private static RectTransform Column(string name, Transform parent, bool left)
        {
            var rect = Rect(name, parent);
            rect.anchorMin = new Vector2(left ? 0 : .5f, 1); rect.anchorMax = new Vector2(left ? .5f : 1, 1);
            rect.pivot = new Vector2(.5f, 1); rect.sizeDelta = new Vector2(0, 0); return rect;
        }
        private static UnityEngine.UI.Image Image(RectTransform rect, Color color, bool raycast = false)
        {
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color = color; image.raycastTarget = raycast; return image;
        }
        private static TMP_Text Label(string name, Transform parent, float top, float height, string value, int size)
            => Text(Top(name, parent, top, height), value, size);
        private static TMP_Text Text(RectTransform rect, string value, int size)
        {
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>(); text.font = font; text.text = value;
            text.fontSize = size; text.color = Ink; text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.Normal; text.raycastTarget = false;
            return text;
        }
        private static void RequireEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Header authoring/previews require edit mode.");
        }
        private static HeaderChallengePanelController Find()
        {
            RequireEditMode();
            var manager = UnityEngine.Object.FindAnyObjectByType<HeaderManager>();
            if (manager == null || manager.headerChallengePanel == null) throw new InvalidOperationException("Author HeaderChallengeUI first.");
            return manager.headerChallengePanel;
        }
        [MenuItem("CounterAttack/Room/Preview Header Challenge/Two attackers vs two defenders and goalkeeper")]
        public static void PreviewFull() => Preview(Find(), 0);
        [MenuItem("CounterAttack/Room/Preview Header Challenge/Partially completed")]
        public static void PreviewPartial() => Preview(Find(), 1);
        [MenuItem("CounterAttack/Room/Preview Header Challenge/Goalkeeper matches defensive best")]
        public static void PreviewGoalkeeperTie() => Preview(Find(), 2);
        [MenuItem("CounterAttack/Room/Preview Header Challenge/Jackpot team scores")]
        public static void PreviewJackpots() => Preview(Find(), 3);
        [MenuItem("CounterAttack/Room/Preview Header Challenge/Tie with direction and distance")]
        public static void PreviewScatter() => Preview(Find(), 4);
        public static void Preview(HeaderChallengePanelController ui, int mode)
        {
            RequireEditMode();
            ui.title.text = HeaderChallengePanelController.ChallengeTitle(mode == 2 || mode == 6);
            ui.visuals.SetActive(true); ui.scatter.SetActive(mode >= 4);
            if (ui.recoveryContent != null) ui.recoveryContent.SetActive(mode == 5);
            ui.attackTeam.text = "ATTACKING TEAM\nAttacking team"; ui.defenseTeam.text = "DEFENDING TEAM\nDefending team";
            ui.attackBest.text = "Best Attack: 10\n#9 Attacker"; ui.defenseBest.text = "Best Defense: 8\n#4 Defender";
            ui.guidance.text = "Current roller · #9 Attacker\nSets the attacking team score.";
            ui.outcome.text = ""; ui.footer.text = "[R] roll · Current contestant";
            for (int side = 0; side < 2; side++)
            {
                var rows = side == 0 ? ui.attackers : ui.defenders;
                for (int i = 0; i < rows.Length; i++)
                {
                    var row = rows[i]; row.root.SetActive(true); row.die.Clear();
                    row.player.text = side == 0 ? $"#{9 + i} Attacker {i + 1}" : i == 2 ? "#1 Goalkeeper" : $"#{4 + i} Defender {i + 1}";
                    row.attribute.text = HeaderChallengePanelController.AttributeText(i == 2, i == 2 ? 4 : 5, i == 1 ? -1 : 0);
                    row.result.text = "Roll — - Total: —"; row.status.text = "Pending"; row.highlight.color = Color.clear;
                    row.die.SetTeamColors(side == 0 ? new Color(.1f, .3f, .65f) : new Color(.65f, .12f, .15f), Color.white);
                    bool rolled = mode >= 2 || mode == 1 && (side == 0 || i == 0);
                    if (rolled)
                    {
                        row.die.Resolve(side == 0 ? 5 : i == 2 ? 4 : 3, mode == 3);
                        int roll = side == 0 ? 5 : i == 2 ? 4 : 3;
                        int total = roll + (i == 2 ? 4 : 5) - (i == 1 ? 1 : 0);
                        row.result.text = mode == 3 ? "Roll JACKPOT - Total: JACKPOT" : $"Roll {roll} - Total: {total}";
                        row.status.text = i == 0 ? "Rolled · TEAM BEST" : "Rolled";
                    }
                }
            }
            if (mode == 0) { ui.attackers[0].status.text = "ROLLING"; ui.attackBest.text = "Best Attack: —\n—"; ui.defenseBest.text = "Best Defense: —\n—"; }
            if (mode == 1) { ui.defenders[1].status.text = "ROLLING"; ui.guidance.text = "#5 Defender · Heading 5, distance −1. Roll 6 matches Attack’s 10; only JACKPOT puts Defense ahead."; }
            if (mode == 2)
            {
                ui.attackBest.text = "Best Attack: 7\n#9 Attacker"; ui.defenseBest.text = "Best Defense: 8\n#1 Goalkeeper";
                ui.defenders[2].status.text = "WINNER · TEAM BEST"; ui.defenders[0].status.text = "Rolled";
                for (int i = 0; i < ui.attackers.Length; i++) { ui.attackers[i].die.Resolve(2, false); ui.attackers[i].result.text = $"Roll 2 - Total: {7 - i}"; }
                ui.guidance.text = "Goalkeeper matches the defensive best of 8 and becomes the selected defender. Team score stays 8.";
                ui.outcome.text = "Goalkeeper wins · Save and hold follows";
            }
            if (mode == 3)
            {
                ui.attackBest.text = "Best Attack: JACKPOT\n#9 Attacker"; ui.defenseBest.text = "Best Defense: JACKPOT\n#1 Goalkeeper";
                ui.defenders[0].status.text = "Rolled"; ui.defenders[2].status.text = "Rolled · TEAM BEST";
                ui.guidance.text = "A jackpot cannot beat an opposing jackpot. Matching teammate jackpots does not raise the team score.";
                ui.outcome.text = "TEAM TIE · Loose ball from #1 Goalkeeper";
            }
            if (mode >= 4)
            {
                ui.defenseBest.text = "Best Defense: 10\n#4 Defender";
                for (int i = 0; i < ui.defenders.Length; i++)
                {
                    int roll = i == 1 ? 6 : 5;
                    ui.defenders[i].die.Resolve(roll, false);
                    ui.defenders[i].result.text = $"Roll {roll} - Total: {(i == 2 ? 9 : 10)}";
                }
                ui.guidance.text = ""; ui.outcome.text = "TEAM TIE · Loose ball from #4 Defender";
                ui.direction.die.Resolve(4, false); ui.direction.result.text = "Roll 4 · North";
                ui.distance.die.Clear(); ui.distance.result.text = "Pending"; ui.footer.text = "[R] distance roll";
            }
            if (mode == 5 && ui.recoveryContent != null)
            {
                ui.distance.die.Resolve(3, false); ui.distance.result.text = "Roll 3 · 3 hexes";
                ui.recoveryPlayer.playerName.text = "#5 Recovering defender";
                ui.recoveryPlayer.teamName.text = "Defending team";
                ui.recoveryPlayer.attribute.text = "Tackling: 5";
                ui.recoveryPlayer.modifier.text = "xRecovery —";
                ui.recoveryPlayer.guidance.text = SingleRollResolutionPresentation.RecoveryGuidance(5, 6, 0);
                ui.recoveryPlayer.total.text = "Awaiting roll";
                ui.recoveryPlayer.die.Clear();
                ui.recoveryPlayer.die.SetTeamColors(new Color(.65f, .12f, .15f), Color.white);
                ui.footer.text = "[R] loose-ball recovery roll";
            }
            foreach (var rows in new[] { ui.attackers, ui.defenders }) foreach (var row in rows)
                row.highlight.color = row.status.text == "ROLLING" ? new Color(.95f, .74f, .25f, .22f)
                    : row.status.text.Contains("TEAM BEST") ? new Color(.3f, .85f, .65f, .16f) : Color.clear;
            ui.direction.die.SetTeamColors(new Color(.65f, .12f, .15f), Color.white);
            ui.distance.die.SetTeamColors(new Color(.65f, .12f, .15f), Color.white);
            ui.FitAndClamp();
            Canvas.ForceUpdateCanvases();
            Selection.activeGameObject = ui.panel.gameObject;
            EditorUtility.SetDirty(ui);
            EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);
            EditorApplication.QueuePlayerLoopUpdate();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }
    }
}
