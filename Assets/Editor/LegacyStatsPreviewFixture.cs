using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public void RunLegacyStatsPreview()
    {
        var p = LoadLegacyRoster(); var log = match.gameData.gameLog;
        var statsHost = Child("Inactive scorer display dependency");
        match.matchStatsUI = statsHost.AddComponent<MatchStatsUI>(); match.matchStatsUI.enabled = false;
        var yaneva = p["Yaneva"]; var cafferata = p["Cafferata"]; var kalla = p["Kalla"]; var delgado = p["Delgado"];
        log.LogEvent(yaneva, MatchManager.ActionType.GoalScored); log.LogEvent(yaneva, MatchManager.ActionType.GoalScored);
        log.LogEvent(yaneva, MatchManager.ActionType.AssistProvided);
        cafferata.ReceiveYellowCard(); cafferata.ReceiveInjury();
        log.LogEvent(cafferata, MatchManager.ActionType.YellowCardShown, connectedToken: delgado);
        log.LogEvent(cafferata, MatchManager.ActionType.Injured, connectedToken: delgado);
        match.RecordSubstitutionEvent(null, cafferata.playerName); match.RecordSubstitutionEvent(cafferata.playerName, kalla.playerName);
        match.gameData.stats.homeTeamStats.totalSubstiutions += 2;
        log.LogEvent(delgado, MatchManager.ActionType.AssistProvided); log.LogEvent(delgado, MatchManager.ActionType.AssistProvided);
        p["Soares"].ReceiveYellowCard(); log.LogEvent(p["Soares"], MatchManager.ActionType.YellowCardShown, connectedToken: yaneva);
        log.LogEvent(p["Soares"], MatchManager.ActionType.GoalScored); log.LogEvent(p["McNulty"], MatchManager.ActionType.GoalScored);
        match.PreviousTokenToTouchTheBallOnPurpose = null; match.LastTokenToTouchTheBallOnPurpose = yaneva;
        Assert.That(match.gameData.stats.homeTeamStats.totalGoals, Is.EqualTo(2)); Assert.That(match.gameData.stats.awayTeamStats.totalGoals, Is.EqualTo(2));
        Assert.That(match.gameData.stats.GetPlayerStats(yaneva).goals, Is.EqualTo(2)); Assert.That(match.gameData.stats.GetPlayerStats(yaneva).assists, Is.EqualTo(1));
        Assert.That(match.gameData.stats.GetPlayerStats(delgado).assists, Is.EqualTo(2));
        var preview = EditorSceneManager.OpenPreviewScene(FrameworkAuthoredPanelAssets.SourceScene);
        try
        {
            var ui = preview.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MatchStatsUI>(true)).Single();
            var statsRoot = ui.transform.Find("Stats"); Assert.That(statsRoot, Is.Not.Null, "Use the production-authored stats layout in an isolated preview scene.");
            var transforms = statsRoot.GetComponentsInChildren<RectTransform>(true);
            var geometry = transforms.ToDictionary(t => t, t => new object[] { t.localPosition, t.localRotation, t.localScale,
                t.anchorMin, t.anchorMax, t.anchoredPosition, t.sizeDelta, t.pivot, t.gameObject.activeSelf, t.GetSiblingIndex(), t.GetComponents<Component>().Select(c => c.GetType().FullName).ToArray() });
            Invoke(ui, "LoadStatsTemplate"); Invoke(ui, "RefreshTeamColors"); Invoke(ui, "AutoBindDesignerStatsFields");
            Invoke(ui, "UpdateDesignerStatsFields", match.gameData.gameSettings.homeTeamName, match.gameData.gameSettings.awayTeamName,
                match.gameData.stats.homeTeamStats, match.gameData.stats.awayTeamStats);
            var titles = statsRoot.GetComponentsInChildren<TMP_Text>(true).Select(t => t.text).ToArray();
            Assert.That(titles, Does.Contain("Assists"));
            var labels = (TMP_Text[])typeof(MatchStatsUI).GetField("statTitleTexts", Private).GetValue(ui);
            var homeValues = (TMP_Text[])typeof(MatchStatsUI).GetField("homeStatsTexts", Private).GetValue(ui);
            var awayValues = (TMP_Text[])typeof(MatchStatsUI).GetField("awayStatsTexts", Private).GetValue(ui);
            int assistsRow = System.Array.FindIndex(labels, t => t != null && t.text == "Assists");
            Assert.That(assistsRow, Is.GreaterThanOrEqualTo(0));
            Assert.That(homeValues[assistsRow].text, Does.Contain("1")); Assert.That(awayValues[assistsRow].text, Does.Contain("2"));
            var rows = ((IEnumerable)Invoke(ui, "BuildLineupRows", true)).Cast<object>().ToList();
            object Row(string name) => rows.Single(r => (string)r.GetType().GetField("displayName").GetValue(r) == name);
            object Value(object row, string field) => row.GetType().GetField(field).GetValue(row);
            Assert.That(Value(Row("Yaneva"), "goals"), Is.EqualTo(2)); Assert.That(Value(Row("Yaneva"), "assists"), Is.EqualTo(1));
            Assert.That(Value(Row("Cafferata"), "isBooked"), Is.EqualTo(true)); Assert.That(Value(Row("Cafferata"), "isInjured"), Is.EqualTo(true));
            Assert.That(Value(Row("Cafferata"), "subOns"), Is.EqualTo(1)); Assert.That(Value(Row("Cafferata"), "subOffs"), Is.EqualTo(1));
            Assert.That(Value(Row("Kalla"), "subOns"), Is.EqualTo(1));
            var format = typeof(MatchStatsUI).GetMethod("BuildDesignerLineupSideText", BindingFlags.NonPublic | BindingFlags.Static);
            string formatted = (string)format.Invoke(null, new[] { Row("Cafferata"), (object)true });
            foreach (var marker in new[] { "SubOn", "SubOff", "Injury", "<s>Cafferata</s>" }) Assert.That(formatted, Does.Contain(marker));
            Assert.That(statsRoot.GetComponentsInChildren<RectTransform>(true).Length, Is.EqualTo(transforms.Length));
            foreach (var t in transforms)
                Assert.That(new object[] { t.localPosition, t.localRotation, t.localScale, t.anchorMin, t.anchorMax, t.anchoredPosition, t.sizeDelta, t.pivot,
                    t.gameObject.activeSelf, t.GetSiblingIndex(), t.GetComponents<Component>().Select(c => c.GetType().FullName).ToArray() }, Is.EqualTo(geometry[t]), t.name);
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }
    }
}
