using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CounterAttack.Editor
{
    public static class Thomas7StatsEditorTools
    {
        public static void RepairStatsPresentation()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before authoring Room UI.");
            var ui = UnityEngine.Object.FindObjectsByType<MatchStatsUI>(FindObjectsInactive.Include)
                .First(item => item.gameObject.scene.path == "Assets/Scenes/Room.unity");
            var stats = ui.transform.Find("Stats");
            var teams = stats.Find("TeamsRow");
            var grid = stats.Find("StatsGrid");
            if (teams == null || grid == null) throw new InvalidOperationException("Expected existing designer rows.");
            Undo.RecordObject(ui, "Repair stats references");
            ui.homeTeamNameText = teams.Find("HomeTeamName").GetComponent<TMP_Text>();
            ui.awayTeamNameText = teams.Find("AwayTeamName").GetComponent<TMP_Text>();
            ui.scoreText = teams.Find("VersusText").GetComponent<TMP_Text>();

            // The authored roster layout shares space between scorers and lineups.
            // StatsGrid stays fixed for both 16- and 18-player squads.
            var existing = ui.transform.Find("ScorerPanel");
            var panel = existing != null ? existing.GetComponent<RectTransform>()
                : new GameObject("ScorerPanel", typeof(RectTransform)).GetComponent<RectTransform>();
            panel.SetParent(ui.transform, false);
            panel.gameObject.layer = ui.gameObject.layer;
            panel.anchorMin = new Vector2(0f, 1f);
            panel.anchorMax = new Vector2(1f, 1f);
            panel.pivot = new Vector2(.5f, 1f);
            panel.anchoredPosition = new Vector2(0f, -76f);
            panel.sizeDelta = new Vector2(-64f, 128f);
            ui.homeScorersText = EnsureScorerText(panel, "HomeScorers", ui.homeTeamNameText, 0f, .44f);
            ui.awayScorersText = EnsureScorerText(panel, "AwayScorers", ui.awayTeamNameText, .56f, 1f);
            var rosterLayout = ui.transform.Find("Lineups").GetComponent<MatchStatsLineupLayout>();
            if (rosterLayout != null) rosterLayout.ApplyRosterSize(18, true);

            var serialized = new SerializedObject(ui);
            foreach (var column in new[] { ("homeStatsTexts", "HomeStat"), ("statTitleTexts", "StatTitle"), ("awayStatsTexts", "AwayStat") })
            {
                var array = serialized.FindProperty(column.Item1);
                array.arraySize = 21;
                for (int i = 0; i < 21; i++)
                    array.GetArrayElementAtIndex(i).objectReferenceValue = grid.Find($"StatRow ({i + 1})/{column.Item2}").GetComponent<TMP_Text>();
            }
            serialized.ApplyModifiedProperties();
            // Text values can be authored without altering any Stats layout.
            var methods = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            typeof(MatchStatsUI).GetMethod("LoadStatsTemplate", methods).Invoke(ui, null);
            typeof(MatchStatsUI).GetMethod("UpdateDesignerStatsFields", methods).Invoke(ui,
                new object[] { "Home", "Away", new MatchManager.TeamStats(), new MatchManager.TeamStats() });
            ui.homeTeamNameText.text = "Home";
            ui.awayTeamNameText.text = "Away";
            ui.scoreText.text = "0-0";
            foreach (var text in stats.GetComponentsInChildren<TMP_Text>(true)) EditorUtility.SetDirty(text);
            EditorUtility.SetDirty(ui);
            EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);
            EditorSceneManager.SaveScene(ui.gameObject.scene);
        }

        private static TMP_Text EnsureScorerText(RectTransform parent, string name, TMP_Text teamText, float left, float right)
        {
            var existing = parent.Find(name);
            var text = existing != null ? existing.GetComponent<TextMeshProUGUI>()
                : new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI))
                    .GetComponent<TextMeshProUGUI>();
            text.transform.SetParent(parent, false);
            text.gameObject.layer = parent.gameObject.layer;
            text.rectTransform.anchorMin = new Vector2(left, 0f);
            text.rectTransform.anchorMax = new Vector2(right, 1f);
            text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
            text.font = teamText.font;
            text.fontSize = 18f;
            text.enableAutoSizing = true;
            text.fontSizeMin = 10f;
            text.fontSizeMax = 18f;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.color = teamText.color;
            text.alignment = TextAlignmentOptions.Top;
            text.margin = new Vector4(0f, 4f, 0f, 4f);
            text.raycastTarget = false;
            text.text = string.Empty;
            return text;
        }
    }
}
