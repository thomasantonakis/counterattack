using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CounterAttack.Editor
{
    /// <summary>One-time scene authoring. Never creates a prefab or builds UI in Play Mode.</summary>
    public static class DuelResolutionPanelEditorTools
    {
        private static readonly Color Background = new(.055f, .065f, .085f, .98f);
        private static readonly Color TextColor = new(.94f, .95f, .98f);
        private static TMP_FontAsset font;
        private static Sprite[] faces;

        [MenuItem("CounterAttack/Room/Preview Duel Panel/Compact")]
        public static void PreviewCompact()
        {
            var panel = FindPanelForPreview();
            panel.visuals.SetActive(true);
            if (panel.playerColumns != null) panel.playerColumns.SetActive(true);
            if (panel.singleRollContent != null) panel.singleRollContent.SetActive(false);
            if (panel.looseBallContent != null) panel.looseBallContent.SetActive(false);
            if (panel.scatterContinuationContent != null) panel.scatterContinuationContent.SetActive(false);
            panel.SetExpandedLayout(false);
            FinishPreview(panel);
        }

        [MenuItem("CounterAttack/Room/Preview Duel Panel/Foul Checks")]
        public static void PreviewFoulChecks()
        {
            var panel = FindPanelForPreview();
            panel.visuals.SetActive(true);
            if (panel.playerColumns != null) panel.playerColumns.SetActive(true);
            if (panel.singleRollContent != null) panel.singleRollContent.SetActive(false);
            if (panel.looseBallContent != null) panel.looseBallContent.SetActive(false);
            if (panel.scatterContinuationContent != null) panel.scatterContinuationContent.SetActive(false);
            panel.SetExpandedLayout(true);
            FinishPreview(panel);
        }

        private static DuelResolutionPanelController FindPanelForPreview()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Use duel previews in edit mode only.");
            var manager = UnityEngine.Object.FindAnyObjectByType<MovementPhaseManager>();
            if (manager == null || manager.duelResolutionPanel == null)
                throw new InvalidOperationException("Open Room and author the duel panel first.");
            return manager.duelResolutionPanel;
        }

        private static void FinishPreview(DuelResolutionPanelController panel)
        {
            Selection.activeGameObject = panel.panel.gameObject;
            UnityEngine.Canvas.ForceUpdateCanvases();
            EditorUtility.SetDirty(panel.panel);
            EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);
            EditorApplication.QueuePlayerLoopUpdate();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        [MenuItem("CounterAttack/Room/Ensure Duel Resolution Panel")]
        public static void EnsurePanel()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before authoring the duel panel.");
            Scene room = SceneManager.GetSceneByPath("Assets/Scenes/Room.unity");
            if (!room.isLoaded) throw new InvalidOperationException("Open Assets/Scenes/Room.unity first.");
            MovementPhaseManager manager = null;
            Transform canvas = null;
            foreach (GameObject root in room.GetRootGameObjects())
            {
                if (root.name == "MainCanvas") canvas = root.transform;
                if (manager == null) manager = root.GetComponentInChildren<MovementPhaseManager>(true);
            }
            if (canvas == null || manager == null) throw new InvalidOperationException("Room Canvas/managers are missing.");
            Transform existing = canvas.Find("DuelResolutionUI");
            if (existing != null)
            {
                manager.duelResolutionPanel = existing.GetComponent<DuelResolutionPanelController>();
                UpdatePanelDetails(manager.duelResolutionPanel);
                if (manager.duelResolutionPanel == null || !manager.duelResolutionPanel.HasSceneBindings)
                    throw new InvalidOperationException("Existing duel panel has incomplete references; inspect it before changing designer objects.");
                EditorUtility.SetDirty(manager);
                EditorSceneManager.MarkSceneDirty(room);
                EditorSceneManager.SaveScene(room);
                return;
            }
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Resources/Fonts/Poppins/Poppins-Medium SDF.asset")
                ?? TMP_Settings.defaultFontAsset;
            faces = CreateFaces();
            RectTransform rootRect = Rect("DuelResolutionUI", canvas);
            Stretch(rootRect);
            var controller = rootRect.gameObject.AddComponent<DuelResolutionPanelController>();
            RectTransform visuals = Rect("Visuals", rootRect);
            Stretch(visuals);
            controller.visuals = visuals.gameObject;
            RectTransform blocker = Rect("InputBlocker", visuals);
            Stretch(blocker);
            Image(blocker, new Color(0, 0, 0, .02f), true);
            var consumer = blocker.gameObject.AddComponent<GameplayInputConsumer>();
            consumer.ConsumesPointerOverSelf = true;
            consumer.BlocksGameplayWhileActive = false;
            RectTransform panel = Rect("Panel", visuals);
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(.5f, .5f);
            panel.sizeDelta = new Vector2(controller.preferredWidth, controller.normalHeight);
            Image(panel, Background, true);
            controller.panel = panel;
            var drag = panel.gameObject.AddComponent<DuelPanelDragHandle>();
            drag.owner = controller;
            controller.title = Label("Title", panel, 25, 64, "TACKLE DUEL", 44);
            RectTransform columns = Top("PlayerColumns", panel, 110, 490);
            controller.leftPlayer = Player("LeftPlayer", columns, false);
            controller.rightPlayer = Player("RightPlayer", columns, true);
            controller.outcome = Label("DuelOutcome", panel, 615, 70, "", 36);
            RectTransform checks = Top("FoulChecks", panel, 700, 320);
            controller.foulChecks = checks.gameObject;
            controller.leniency = Check("LeniencyCheck", checks, false, "LENIENCY");
            controller.injury = Check("InjuryCheck", checks, true, "INJURY");
            checks.gameObject.SetActive(false);
            RectTransform footer = Rect("CountdownFooter", panel);
            footer.anchorMin = new Vector2(0, 0);
            footer.anchorMax = new Vector2(1, 0);
            footer.pivot = new Vector2(.5f, 0);
            footer.anchoredPosition = new Vector2(0, 24);
            footer.sizeDelta = new Vector2(-60, 74);
            controller.footer = Text(footer, "Press [R] — tackler rolls", 26);
            UpdatePanelDetails(controller);
            manager.duelResolutionPanel = controller;
            // Keep authoring defaults visible in edit mode. Awake hides this overlay in a running game.
            EditorUtility.SetDirty(manager);
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(room);
            EditorSceneManager.SaveScene(room);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = panel.gameObject;
            Debug.Log("Duel panel authored under MainCanvas/DuelResolutionUI. No prefab created.");
        }

        private static DuelPlayerPanelFields Player(string name, RectTransform parent, bool right)
        {
            RectTransform column = Column(name, parent, right, 490);
            return new DuelPlayerPanelFields
            {
                playerName = Label("PlayerName", column, 0, 88, right ? "#4  Tackler" : "#9  Dribbler", 40),
                teamName = Label("TeamName", column, 90, 45, "Team name", 28),
                role = Label("Role", column, 138, 38, "", 25),
                attribute = Label("Attribute", column, 182, 46, right ? "Tackling  6" : "Dribbling  5", 34),
                modifier = Label("Modifier", column, 232, 40, "", 27),
                die = Die("Die", column, new Vector2(0, -280), true),
                total = Label("Total", column, 402, 48, "Total  —", 32),
                guidance = Label("RollGuidance", column, 454, 64, "Waiting for roll", 27)
            };
        }

        private static DuelCheckPanelFields Check(string name, RectTransform parent, bool right, string heading)
        {
            RectTransform column = Column(name, parent, right, 270);
            Label("Title", column, 0, 45, heading, 32);
            var fields = new DuelCheckPanelFields
            {
                column = column,
                rule = Label("Rule", column, 48, 100, "Check threshold", 27),
                result = Label("Result", column, 156, 106, "Pending", 29),
                die = Die("Die", column, new Vector2(24, -162), false)
            };
            var resultRect = fields.result.rectTransform;
            resultRect.offsetMin = new Vector2(150, resultRect.offsetMin.y);
            return fields;
        }

        private static void UpdatePanelDetails(DuelResolutionPanelController panel)
        {
            if (panel == null) return;
            EnsureSingleRollContent(panel);
            EnsureLooseBallContent(panel);
            EnsureScatterContinuation(panel);
            foreach (var die in panel.GetComponentsInChildren<DuelDieView>(true)) EnsureDieBorder(die);
            if (panel.leftPlayer.role != null)
            {
                panel.leftPlayer.role.text = "";
                panel.leftPlayer.role.gameObject.SetActive(false);
            }
            if (panel.rightPlayer.role != null)
            {
                panel.rightPlayer.role.text = "";
                panel.rightPlayer.role.gameObject.SetActive(false);
            }
            panel.leniency.column = panel.foulChecks.transform.Find("LeniencyCheck") as RectTransform;
            panel.injury.column = panel.foulChecks.transform.Find("InjuryCheck") as RectTransform;
            panel.ArrangeFoulChecks(true);
            panel.outcome.text = "";
            AuthorDownwardExpansion(panel);
            EditorUtility.SetDirty(panel);
        }

        // Authored once in edit mode; runtime only changes text, color and visibility.
        private static void EnsureSingleRollContent(DuelResolutionPanelController controller)
        {
            controller.playerColumns = controller.panel.Find("PlayerColumns").gameObject;
            if (controller.singleRollContent != null) return;
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Resources/Fonts/Poppins/Poppins-Medium SDF.asset")
                ?? TMP_Settings.defaultFontAsset;
            faces = new Sprite[7];
            Array.Copy(controller.leftPlayer.die.numericFaces, faces, 6);
            faces[6] = controller.leftPlayer.die.jackpotFace;
            RectTransform content = Top("SingleRollContent", controller.panel, 110, 490);
            controller.singleRollContent = content.gameObject;
            controller.singlePlayer = Player("RollingPlayer", content, false);
            RectTransform column = (RectTransform)controller.singlePlayer.playerName.transform.parent;
            column.anchorMax = new Vector2(1, 1);
            controller.singlePlayer.playerName.text = "#4  Rolling player";
            controller.singlePlayer.attribute.text = "Tackling 6";
            controller.singlePlayer.modifier.text = "xRecovery 0.50";
            controller.singlePlayer.role.gameObject.SetActive(false);
            content.gameObject.SetActive(false);
        }

        private static void EnsureLooseBallContent(DuelResolutionPanelController controller)
        {
            if (controller.looseBallContent != null) return;
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Resources/Fonts/Poppins/Poppins-Medium SDF.asset")
                ?? TMP_Settings.defaultFontAsset;
            faces = new Sprite[7];
            Array.Copy(controller.leftPlayer.die.numericFaces, faces, 6);
            faces[6] = controller.leftPlayer.die.jackpotFace;
            RectTransform content = Top("LooseBallContent", controller.panel, 110, 490);
            controller.looseBallContent = content.gameObject;
            controller.looseBallPlayer = Label("RollingPlayer", content, 0, 88, "#4  Rolling player", 40);
            RectTransform columns = Top("RollColumns", content, 150, 340);
            controller.looseDirection = LooseBallColumn("Direction", columns, false);
            controller.looseDistance = LooseBallColumn("Distance", columns, true);
            content.gameObject.SetActive(false);
        }

        private static void EnsureScatterContinuation(DuelResolutionPanelController controller)
        {
            if (controller.scatterContinuationContent != null) return;
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Resources/Fonts/Poppins/Poppins-Medium SDF.asset")
                ?? TMP_Settings.defaultFontAsset;
            faces = new Sprite[7];
            Array.Copy(controller.leftPlayer.die.numericFaces, faces, 6);
            faces[6] = controller.leftPlayer.die.jackpotFace;
            RectTransform content = Top("ScatterContinuation", controller.panel, 744, 400);
            controller.scatterContinuationContent = content.gameObject;
            controller.scatterCaption = Label("Situation", content, 0, 48, "Loose Ball", 34);
            RectTransform columns = Top("RollColumns", content, 60, 340);
            controller.continuationDirection = LooseBallColumn("Direction", columns, false);
            controller.continuationDistance = LooseBallColumn("Distance", columns, true);
            content.gameObject.SetActive(false);
        }

        [MenuItem("CounterAttack/Room/Preview Duel Panel/Accuracy Inaccuracy Rolls")]
        public static void PreviewAccuracyScatter()
        {
            PreviewSingleRoll();
            var controller = FindPanelForPreview();
            controller.title.text = "High Pass Accuracy";
            controller.singlePlayer.attribute.text = "High Pass 6 · Target 8";
            controller.singlePlayer.modifier.text = "Accuracy 83.3%";
            controller.singlePlayer.total.text = "Roll 1";
            controller.singlePlayer.guidance.text = "Success: roll 2–6";
            controller.singlePlayer.die.Resolve(1, false);
            controller.outcome.text = "INACCURATE";
            PreviewScatterAttachment(controller, "Inaccuracy");
        }

        [MenuItem("CounterAttack/Room/Preview Duel Panel/Tied Duel Loose Ball Rolls")]
        public static void PreviewTiedDuelScatter()
        {
            PreviewCompact();
            var controller = FindPanelForPreview();
            controller.leftPlayer.die.Resolve(3, false);
            controller.rightPlayer.die.Resolve(3, false);
            controller.leftPlayer.total.text = "Roll 3 + 5 = 8";
            controller.rightPlayer.total.text = "Roll 3 + 5 = 8";
            controller.outcome.text = "TIE — LOOSE BALL";
            PreviewScatterAttachment(controller, "Loose Ball");
        }

        private static void PreviewScatterAttachment(DuelResolutionPanelController controller, string caption)
        {
            EnsureScatterContinuation(controller);
            controller.scatterCaption.text = caption;
            controller.continuationDirection.die.Resolve(2, false);
            controller.continuationDirection.result.text = "Roll 2\nSouthWest";
            controller.continuationDistance.die.Clear();
            controller.continuationDistance.result.text = "Awaiting roll";
            controller.footer.text = "Press [R] — distance roll";
            controller.SetScatterExpandedLayout();
            FinishPreview(controller);
        }

        private static DuelCheckPanelFields LooseBallColumn(string heading, RectTransform parent, bool right)
        {
            RectTransform column = Column(heading + "Roll", parent, right, 340);
            return new DuelCheckPanelFields
            {
                column = column,
                rule = Label("Heading", column, 0, 50, heading, 34),
                die = Die("Die", column, new Vector2(0, -75), true),
                result = Label("Result", column, 220, 110, "Pending", 30)
            };
        }

        [MenuItem("CounterAttack/Room/Preview Duel Panel/Loose Ball Rolls")]
        public static void PreviewLooseBallRolls()
        {
            var controller = FindPanelForPreview();
            EnsureLooseBallContent(controller);
            controller.playerColumns.SetActive(false);
            controller.singleRollContent.SetActive(false);
            controller.looseBallContent.SetActive(true);
            controller.title.text = "Loose Ball Distance";
            controller.outcome.text = "";
            controller.looseDirection.die.Resolve(2, false);
            controller.looseDirection.result.text = "Roll 2\nSouthWest";
            controller.looseDistance.die.Clear();
            controller.looseDistance.result.text = "Awaiting roll";
            controller.footer.text = "Press [R] — distance roll";
            controller.SetExpandedLayout(false);
            controller.visuals.SetActive(true);
            FinishPreview(controller);
        }

        [MenuItem("CounterAttack/Room/Preview Duel Panel/Single Roll")]
        public static void PreviewSingleRoll()
        {
            var controller = FindPanelForPreview();
            EnsureSingleRollContent(controller);
            controller.playerColumns.SetActive(false);
            if (controller.looseBallContent != null) controller.looseBallContent.SetActive(false);
            controller.singleRollContent.SetActive(true);
            controller.title.text = "Ground Pass Interception";
            controller.singlePlayer.playerName.text = "#4  Rolling player";
            controller.singlePlayer.attribute.text = "Tackling 6";
            controller.singlePlayer.modifier.text = "xRecovery 0.50";
            controller.singlePlayer.guidance.text = SingleRollResolutionPresentation.RecoveryGuidance(6, 6, 0);
            controller.SetExpandedLayout(false);
            controller.visuals.SetActive(true);
            FinishPreview(controller);
        }

        [MenuItem("CounterAttack/Room/Preview Duel Panel/Recovery Foul Checks")]
        public static void PreviewRecoveryFoulChecks()
        {
            PreviewSingleRoll();
            var controller = FindPanelForPreview();
            controller.title.text = "Recovery Attempt";
            controller.singlePlayer.total.text = "Roll 1";
            controller.singlePlayer.guidance.text = "Fouled: #9  Dribbler";
            controller.outcome.text = "FOUL";
            controller.leniency.rule.text = DuelRollGuidance.ThresholdCheck("Referee leniency", 4, "yellow card", "no card");
            controller.injury.rule.text = DuelRollGuidance.ThresholdCheck("Resilience", 4, "injury", "avoids injury");
            controller.leniency.result.text = "Awaiting roll";
            controller.injury.result.text = "Pending";
            controller.footer.text = "Press [R] — leniency check";
            controller.SetExpandedLayout(true);
            FinishPreview(controller);
        }

        private static void AuthorDownwardExpansion(DuelResolutionPanelController controller)
        {
            RectTransform panel = controller.panel;
            bool needsMigration = panel.pivot.y != 1f;
            panel.anchorMin = panel.anchorMax = new Vector2(.5f, .5f);
            panel.pivot = new Vector2(.5f, 1f);

            // The controls follow the panel bottom in compact and expanded layouts.
            RectTransform footer = controller.footer.rectTransform;
            footer.anchorMin = Vector2.zero;
            footer.anchorMax = new Vector2(1f, 0f);
            footer.pivot = new Vector2(.5f, 0f);
            footer.anchoredPosition = new Vector2(0f, 24f);
            EditorUtility.SetDirty(footer);

            if (needsMigration)
            {
                RectTransform checks = (RectTransform)controller.foulChecks.transform;
                checks.anchoredPosition = new Vector2(checks.anchoredPosition.x, -controller.normalHeight - 24f);
                controller.expandedHeight = Mathf.Max(controller.expandedHeight,
                    controller.normalHeight + checks.rect.height + 48f);
                EditorUtility.SetDirty(checks);
            }
            // Leave a gap after the final checks and reserve the footer's full height plus bottom inset.
            var checksRect = (RectTransform)controller.foulChecks.transform;
            controller.expandedHeight = Mathf.Max(controller.expandedHeight,
                -checksRect.anchoredPosition.y + checksRect.rect.height + 24f + footer.rect.height + 24f);
            if (controller.scatterContinuationContent != null)
            {
                var scatter = (RectTransform)controller.scatterContinuationContent.transform;
                controller.expandedHeight = Mathf.Max(controller.expandedHeight,
                    -scatter.anchoredPosition.y + scatter.rect.height + 24f + footer.rect.height + 24f);
            }
            controller.SetExpandedLayout(false);
            if (needsMigration) controller.ResetPanelPosition();
            EditorUtility.SetDirty(panel);
        }

        private static void EnsureDieBorder(DuelDieView die)
        {
            // Authored in edit mode on the existing die image; never added at runtime.
            if (die.background == null) return;
            var border = die.background.GetComponent<UnityEngine.UI.Outline>();
            if (border == null)
            {
                border = die.background.gameObject.AddComponent<UnityEngine.UI.Outline>();
                border.effectColor = Color.white;
            }
            border.effectDistance = new Vector2(4f, 4f);
            border.useGraphicAlpha = true;
            die.border = border;
            EditorUtility.SetDirty(border);
            EditorUtility.SetDirty(die);
        }

        private static DuelDieView Die(string name, RectTransform parent, Vector2 position, bool centered)
        {
            RectTransform rect = Rect(name, parent);
            rect.anchorMin = rect.anchorMax = new Vector2(centered ? .5f : 0, 1);
            rect.pivot = new Vector2(centered ? .5f : 0, 1);
            rect.anchoredPosition = position;
            rect.sizeDelta = Vector2.one * (centered ? 112 : 92);
            var die = rect.gameObject.AddComponent<DuelDieView>();
            die.background = Image(rect, new Color(.13f, .35f, .63f), false);
            EnsureDieBorder(die);
            RectTransform face = Rect("Face", rect);
            Stretch(face, 14);
            die.face = Image(face, Color.white, false);
            die.face.sprite = faces[0];
            die.numericFaces = new Sprite[6];
            Array.Copy(faces, die.numericFaces, 6);
            die.jackpotFace = faces[6];
            RectTransform fallback = Rect("FallbackText", rect);
            Stretch(fallback, 5);
            die.fallbackText = Text(fallback, "1", 64);
            die.fallbackText.enabled = false;
            return die;
        }

        private static RectTransform Rect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.gameObject.layer = parent.gameObject.layer;
            return rect;
        }

        private static void Stretch(RectTransform rect, float margin = 0)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.one * margin;
            rect.offsetMax = Vector2.one * -margin;
        }

        private static RectTransform Top(string name, Transform parent, float y, float height)
        {
            RectTransform rect = Rect(name, parent);
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(.5f, 1);
            rect.anchoredPosition = new Vector2(0, -y);
            rect.sizeDelta = new Vector2(-48, height);
            return rect;
        }

        private static RectTransform Column(string name, Transform parent, bool right, float height)
        {
            RectTransform rect = Rect(name, parent);
            rect.anchorMin = new Vector2(right ? .5f : 0, 1);
            rect.anchorMax = new Vector2(right ? 1 : .5f, 1);
            rect.pivot = new Vector2(.5f, 1);
            rect.sizeDelta = new Vector2(-36, height);
            return rect;
        }

        private static TMP_Text Label(string name, Transform parent, float top, float height, string value, float size)
            => Text(Top(name, parent, top, height), value, size);

        private static TMP_Text Text(RectTransform rect, string value, float size)
        {
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = size;
            text.enableAutoSizing = true;
            text.fontSizeMin = size * .7f;
            text.fontSizeMax = size;
            text.text = value;
            text.color = TextColor;
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

        private static Sprite[] CreateFaces()
        {
            const string folder = "Assets/Textures/UI/DuelDice";
            string currentFolder = "Assets";
            foreach (string name in new[] { "Textures", "UI", "DuelDice" })
            {
                string child = currentFolder + "/" + name;
                if (!AssetDatabase.IsValidFolder(child)) AssetDatabase.CreateFolder(currentFolder, name);
                currentFolder = child;
            }
            var result = new Sprite[7];
            for (int face = 1; face <= 7; face++)
            {
                string path = $"{folder}/DieFace{(face == 7 ? "Star" : face.ToString())}.png";
                if (!File.Exists(path))
                {
                    var texture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
                    var pixels = new Color32[128 * 128];
                    for (int y = 0; y < 128; y++)
                        for (int x = 0; x < 128; x++)
                        {
                            bool fill = face == 7 ? InStar(x - 64, y - 64) : InPip(x, y, face);
                            pixels[y * 128 + x] = fill ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
                        }
                    texture.SetPixels32(pixels);
                    texture.Apply();
                    File.WriteAllBytes(path, texture.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(texture);
                }
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
                result[face - 1] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            return result;
        }

        private static bool InPip(int x, int y, int number)
        {
            if (number % 2 == 1 && Circle(x, y, 64, 64)) return true;
            if (number >= 2 && (Circle(x, y, 28, 28) || Circle(x, y, 100, 100))) return true;
            if (number >= 4 && (Circle(x, y, 28, 100) || Circle(x, y, 100, 28))) return true;
            return number == 6 && (Circle(x, y, 28, 64) || Circle(x, y, 100, 64));
        }

        private static bool Circle(int x, int y, int cx, int cy) => (x - cx) * (x - cx) + (y - cy) * (y - cy) <= 100;

        private static bool InStar(float x, float y)
        {
            bool inside = false;
            Vector2 previous = StarPoint(9);
            for (int i = 0; i < 10; i++)
            {
                Vector2 point = StarPoint(i);
                if ((point.y > y) != (previous.y > y)
                    && x < (previous.x - point.x) * (y - point.y) / (previous.y - point.y) + point.x)
                    inside = !inside;
                previous = point;
            }
            return inside;
        }

        private static Vector2 StarPoint(int i)
        {
            float angle = (90 + i * 36) * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (i % 2 == 0 ? 56 : 25);
        }
    }
}
