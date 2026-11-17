using System;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

[Category("PostMatchReport")]
public class PostMatchReportEditorTests
{
    private static MatchManager.PlayerStatsEntry Player(string key, string name, string side, MatchManager.PlayerStats stats) =>
        new() { playerKey = key, playerName = name, teamSide = side, jerseyNumber = 9, stats = stats };

    private static MatchManager.Stats Stats(params MatchManager.PlayerStatsEntry[] entries) =>
        new() { playerStats = entries.ToList() };

    [TestCase(PostMatchLeaderboard.Metric.Distance, 30, "30")]
    [TestCase(PostMatchLeaderboard.Metric.Recoveries, 4, "4 / 2.50")]
    [TestCase(PostMatchLeaderboard.Metric.Passes, 6, "6/10  ·  60%")]
    [TestCase(PostMatchLeaderboard.Metric.Duels, 5, "5/8  ·  62.5%")]
    [TestCase(PostMatchLeaderboard.Metric.Goals, 2, "2")]
    [TestCase(PostMatchLeaderboard.Metric.Assists, 3, "3")]
    public void UsesRecordedMetricsWithoutDoubleCountingRecoveries(PostMatchLeaderboard.Metric metric, double value, string display)
    {
        var entry = Player("home:9", "Player", "Home", new MatchManager.PlayerStats
        {
            pacesRan = 30, possessionWon = 4, xRecoveries = 2.5f, interceptionsMade = 2, tacklesMade = 1,
            passesCompleted = 4, passesAttempted = 6, aerialPassesCompleted = 2, aerialPassesAttempted = 4,
            groundDuelsWon = 3, groundDuelsInvolved = 5, aerialChallengesWon = 2, aerialChallengesInvolved = 3,
            goals = 2, assists = 3, attemptsSaved = 7, attemptsFaced = 9,
        });
        var data = Stats(entry);
        string before = JsonUtility.ToJson(data);
        var row = PostMatchLeaderboard.Build(data, metric).Single();
        Assert.That(row.Value, Is.EqualTo(value).Within(.0001));
        Assert.That(row.DisplayValue, Is.EqualTo(display));
        Assert.That(JsonUtility.ToJson(data), Is.EqualTo(before), "Reporting must never mutate recorded statistics.");
    }

    [TestCase(4, 4, true)]
    [TestCase(5, 5, true)]
    [TestCase(0, 0, false)]
    [TestCase(5, 0, true)]
    public void PassesShowAllRecordedCompletionsWithoutAnAccuracyEligibilityThreshold(int attempts, int completed, bool eligible)
    {
        var stats = Stats(Player("p", "P", "Home", new MatchManager.PlayerStats { passesAttempted = attempts, passesCompleted = completed }));
        Assert.That(PostMatchLeaderboard.Build(stats, PostMatchLeaderboard.Metric.Passes).Count, Is.EqualTo(eligible ? 1 : 0));
    }

    [Test]
    public void PassesRankByCompletedCountAndIncludeGroundAndAerialAccuracy()
    {
        var stats = Stats(
            Player("a", "A", "Home", new MatchManager.PlayerStats { passesAttempted = 5, passesCompleted = 4 }),
            Player("b", "B", "Home", new MatchManager.PlayerStats { passesAttempted = 5, passesCompleted = 4, aerialPassesAttempted = 5, aerialPassesCompleted = 4 }),
            Player("c", "C", "Away", new MatchManager.PlayerStats { passesAttempted = 5, passesCompleted = 5 }));
        var rows = PostMatchLeaderboard.Build(stats, PostMatchLeaderboard.Metric.Passes);
        Assert.That(rows.Select(r => r.PlayerKey), Is.EqualTo(new[] { "b", "c", "a" }));
        Assert.That(rows[0].DisplayValue, Is.EqualTo("8/10  ·  80%"));
    }

    [Test]
    public void DuelsIncludeUnsuccessfulParticipantsAndRecoveriesStillRequireARecovery()
    {
        var stats = Stats(Player("zero", "Zero wins", "Home", new MatchManager.PlayerStats { groundDuelsInvolved = 4, xRecoveries = 2.5f }),
            Player("winner", "Winner", "Away", new MatchManager.PlayerStats { aerialChallengesInvolved = 3, aerialChallengesWon = 1, possessionWon = 1, xRecoveries = .75f }));
        Assert.That(PostMatchLeaderboard.Build(stats, PostMatchLeaderboard.Metric.Duels).Select(r => r.DisplayValue), Is.EqualTo(new[] { "1/3  ·  33.3%", "0/4  ·  0%" }));
        Assert.That(PostMatchLeaderboard.Build(stats, PostMatchLeaderboard.Metric.Recoveries).Single().DisplayValue, Is.EqualTo("1 / 0.75"));
    }

    [Test]
    public void GlobalFiltersUseTeamNamesAndValueHeadingTracksTheSelectedMetric()
    {
        WithView((root, view) =>
        {
            var data = Data();
            data.gameSettings.homeTeamName = "Aurora F.C.";
            data.gameSettings.awayTeamName = "Inverness CT";
            root.SetActive(true);
            view.ShowReport(data, true);
            var content = root.transform.Find(PostMatchReportEditorTools.ContentPath);
            Assert.That(content.Find("TeamFilters").GetSiblingIndex(), Is.EqualTo(content.Find("ReportTabs").GetSiblingIndex() + 1));
            Assert.That(Button(root, "HomeTeam").GetComponentInChildren<TMP_Text>().text, Is.EqualTo("Aurora F.C."));
            Assert.That(Button(root, "AwayTeam").GetComponentInChildren<TMP_Text>().text, Is.EqualTo("Inverness CT"));
            Assert.That(content.Find("TeamFilters").gameObject.activeSelf, Is.False);
            Click(root, "TopPerformersTab");
            Assert.That(content.Find("TeamFilters").gameObject.activeSelf, Is.True);
            Click(root, "HomeTeam");
            Assert.That(content.Find("StatsGrid/HomeStats").GetComponent<CanvasGroup>().alpha, Is.EqualTo(1));
            Assert.That(content.Find("StatsGrid/AwayStats").GetComponent<CanvasGroup>().alpha, Is.EqualTo(1));
            Click(root, "Metric_Passes");
            Assert.That(content.Find("TopPerformers/ColumnHeadings/Value").GetComponent<TMP_Text>().text, Is.EqualTo("Passes / Accuracy"));
            Click(root, "Metric_Recoveries");
            Assert.That(content.Find("TopPerformers/ColumnHeadings/Value").GetComponent<TMP_Text>().text, Is.EqualTo("Recoveries / xRecoveries"));
            Assert.That(root.transform.Find("RecapPanel/Paging"), Is.Not.Null);
            Assert.That(Button(root, "Previous").transform.parent, Is.EqualTo(root.transform.Find("RecapPanel/Paging")));
            Assert.That(root.GetComponentsInChildren<UnityEngine.UI.Button>(true).Any(b => b.name == "Metric_PassAccuracy" || b.name == "Metric_Saves"), Is.False);
            Click(root, "PitchTab");
            Assert.That(content.Find("TeamFilters").gameObject.activeSelf, Is.True);
            Click(root, "MatchStatsTab");
            Assert.That(content.Find("TeamFilters").gameObject.activeSelf, Is.False);
            Assert.That(content.Find("StatsGrid/HomeStats").GetComponent<CanvasGroup>().alpha, Is.EqualTo(1));
            Assert.That(content.Find("StatsGrid/AwayStats").GetComponent<CanvasGroup>().alpha, Is.EqualTo(1));
        });
    }

    [TestCase(PostMatchLeaderboard.Metric.Duels)]
    [TestCase(PostMatchLeaderboard.Metric.Passes)]
    public void ParticipantsSortBySuccessDescendingAttemptsAscendingThenName(PostMatchLeaderboard.Metric metric)
    {
        MatchManager.PlayerStatsEntry Entry(string key, string name, int won, int attempted) => Player(key, name, "Home",
            new MatchManager.PlayerStats { groundDuelsWon = won, groundDuelsInvolved = attempted, passesCompleted = won, passesAttempted = attempted });
        var stats = Stats(Entry("larger", "A larger sample", 2, 5), Entry("smallBeta", "Beta", 2, 4),
            Entry("none", "No success", 0, 3), Entry("highest", "Highest", 8, 12), Entry("smallAlpha", "Alpha", 2, 4),
            Entry("uninvolved", "Uninvolved", 0, 0));
        var rows = PostMatchLeaderboard.Build(stats, metric);
        Assert.That(rows.Select(r => r.PlayerKey), Is.EqualTo(new[] { "highest", "smallAlpha", "smallBeta", "larger", "none" }));
        Assert.That(rows.Last().DisplayValue, Is.EqualTo("0/3  ·  0%"));
        Assert.That(PostMatchLeaderboard.Description(PostMatchLeaderboard.Metric.Passes), Does.Not.Contain("hanging"));
    }

    [TestCase(PostMatchLeaderboard.Team.Both, 3)]
    [TestCase(PostMatchLeaderboard.Team.Home, 1)]
    [TestCase(PostMatchLeaderboard.Team.Away, 1)]
    public void TeamFiltersPreserveSameNamedPlayersAndDoNotGuessLegacyTeams(PostMatchLeaderboard.Team team, int count)
    {
        var stats = Stats(
            Player("h", "Same name", "home", new MatchManager.PlayerStats { pacesRan = 12 }),
            Player("a", "Same name", "Away", new MatchManager.PlayerStats { pacesRan = 20 }),
            Player(null, "Legacy name", null, new MatchManager.PlayerStats { pacesRan = 10 }));
        var rows = PostMatchLeaderboard.Build(stats, PostMatchLeaderboard.Metric.Distance, team);
        Assert.That(rows.Count, Is.EqualTo(count));
        if (team == PostMatchLeaderboard.Team.Both)
        {
            Assert.That(rows.Select(r => r.PlayerKey), Is.EqualTo(new[] { "a", "h", "" }));
            Assert.That(rows[2].TeamSide, Is.EqualTo("Unassigned"));
        }
    }

    [Test]
    public void EmptyAndLegacyDataAreSafeAndZeroActivityDoesNotBecomeAnAward()
    {
        Assert.That(PostMatchLeaderboard.Build(null, PostMatchLeaderboard.Metric.Distance), Is.Empty);
        Assert.That(PostMatchLeaderboard.Build(new MatchManager.Stats { playerStats = null }, PostMatchLeaderboard.Metric.Distance), Is.Empty);
        var stats = Stats(null, Player("missing", "Missing", "Home", null),
            Player("zero", "Zero", "Home", new MatchManager.PlayerStats()),
            Player("sub", "Substitute", "Away", new MatchManager.PlayerStats { goals = 1 }));
        Assert.That(PostMatchLeaderboard.Build(stats, PostMatchLeaderboard.Metric.Distance), Is.Empty);
        Assert.That(PostMatchLeaderboard.Build(stats, PostMatchLeaderboard.Metric.Goals).Single().PlayerName, Is.EqualTo("Substitute"));
    }

    [Test]
    public void TiesHaveStableOrderingAndDisplayNameIsRespected()
    {
        var a = Player("a", "Zed", "Home", new MatchManager.PlayerStats { pacesRan = 3 });
        a.displayName = "Alpha";
        var b = Player("b", "Beta", "Away", new MatchManager.PlayerStats { pacesRan = 3 });
        Assert.That(PostMatchLeaderboard.Build(Stats(b, a), PostMatchLeaderboard.Metric.Distance).Select(r => r.PlayerName), Is.EqualTo(new[] { "Alpha", "Beta" }));
    }

    [Test]
    public void AuthoredPrefabHasCompleteTabControlsAndPreservesExistingStats()
    {
        WithView((root, view) =>
        {
            var stats = root.transform.Find(PostMatchReportEditorTools.ContentPath + "/StatsGrid");
            Assert.That(stats.GetComponentsInChildren<TMP_Text>(true).Length, Is.EqualTo(63));
            Assert.That(root.transform.Find("RecapPanel").GetComponent<RectTransform>().sizeDelta, Is.EqualTo(new Vector2(1400, 1240)));
            var serialized = new SerializedObject(view);
            foreach (string field in new[] { "statsPage", "playersPage", "tabBar", "statsTab", "playersTab", "heading", "description", "emptyMessage", "pageLabel", "previousPage", "nextPage" })
                Assert.That(serialized.FindProperty(field).objectReferenceValue, Is.Not.Null, field);
            Assert.That(serialized.FindProperty("metricButtons").arraySize, Is.EqualTo(6));
            Assert.That(serialized.FindProperty("teamButtons").arraySize, Is.EqualTo(3));
            Assert.That(serialized.FindProperty("rows").arraySize, Is.EqualTo(PostMatchReportEditorTools.PageSize));
            Assert.That(root.GetComponentInChildren<UnityEngine.UI.ScrollRect>(true).viewport.GetComponent<UnityEngine.UI.Image>().raycastTarget, Is.True);
            int count = root.GetComponentsInChildren<Transform>(true).Length;
            PostMatchReportEditorTools.Author(root);
            Assert.That(root.GetComponentsInChildren<Transform>(true).Length, Is.EqualTo(count), "Repeated authoring must preserve designer objects.");
        });
    }

    [Test]
    public void LeaderboardFitsItsContentWithoutChangingTheDesignerStatsLayout()
    {
        WithView((root, view) =>
        {
            var content = root.transform.Find(PostMatchReportEditorTools.ContentPath).GetComponent<RectTransform>();
            var stats = content.Find("StatsGrid").GetComponent<RectTransform>();
            var originalStatsSize = stats.sizeDelta;
            root.SetActive(true);
            view.ShowReport(Data(), true);
            view.ShowPlayers();
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            var players = content.Find("TopPerformers").GetComponent<RectTransform>();
            Assert.That(players.rect.height, Is.GreaterThan(300));
            Assert.That(players.Find("Heading").GetComponent<RectTransform>().rect.height, Is.GreaterThan(30));
            Assert.That(players.Find("PlayerRows/PlayerRow_01").GetComponent<RectTransform>().rect.height, Is.GreaterThan(30));
            Assert.That(players.pivot.y, Is.EqualTo(1f), "Fitted page must expand downwards, away from the score and tabs.");
            var pageCorners = new Vector3[4];
            var tabCorners = new Vector3[4];
            players.GetWorldCorners(pageCorners);
            content.Find("ReportTabs").GetComponent<RectTransform>().GetWorldCorners(tabCorners);
            Assert.That(pageCorners[1].y, Is.LessThanOrEqualTo(tabCorners[0].y), "The player page must not overlap the tabs or score.");
            Assert.That(stats.sizeDelta, Is.EqualTo(originalStatsSize));
        });
    }

    [Test]
    public void RealButtonsSwitchTabsMetricsAndTeamsWithoutRebuildingOrChangingStatsText()
    {
        WithView((root, view) =>
        {
            var grid = root.transform.Find(PostMatchReportEditorTools.ContentPath + "/StatsGrid");
            var originalTexts = grid.GetComponentsInChildren<TMP_Text>(true);
            originalTexts[0].text = "Existing match value";
            int originalCount = root.GetComponentsInChildren<Transform>(true).Length;
            var data = Data();
            view.ShowReport(data, true);
            root.SetActive(true);
            Click(root, "TopPerformersTab");
            Assert.That(view.ShowingPlayers, Is.True);
            Assert.That(grid.gameObject.activeSelf, Is.False);
            Assert.That(Label(root, "Heading"), Is.EqualTo("Distance"));
            Assert.That(PlayerName(root, 1), Does.Contain("Away player"));
            Click(root, "HomeTeam");
            Assert.That(PlayerName(root, 1), Does.Contain("Home player"));
            Click(root, "Metric_Goals");
            Assert.That(Label(root, "Heading"), Is.EqualTo("Goals"));
            Click(root, "MatchStatsTab");
            Assert.That(view.ShowingPlayers, Is.False);
            Assert.That(grid.gameObject.activeSelf, Is.True);
            Assert.That(originalTexts[0].text, Is.EqualTo("Existing match value"));
            Assert.That(root.GetComponentsInChildren<Transform>(true).Length, Is.EqualTo(originalCount));
            Assert.That(grid.GetComponentsInChildren<TMP_Text>(true), Is.EqualTo(originalTexts));
        });
    }

    [Test]
    public void PagingAndRepeatedReportsDoNotDuplicateListenersOrRetainOldSelections()
    {
        WithView((root, view) =>
        {
            var data = Data();
            data.stats.playerStats = Enumerable.Range(1, 30).Select(i => Player(i.ToString(), $"Player {i}", "Home", new MatchManager.PlayerStats { pacesRan = i })).ToList();
            root.SetActive(true);
            view.ShowReport(data, true);
            for (int i = 0; i < 3; i++) { root.SetActive(false); root.SetActive(true); view.ShowReport(data, true); }
            Click(root, "TopPerformersTab");
            Click(root, "Next");
            Assert.That(Label(root, "PageLabel"), Does.Contain("Page 2/3"), "One click must advance exactly one page after repeated report initialization.");
            Assert.That(PlayerName(root, 1), Does.Contain("Player 18"));
            Click(root, "Next");
            Assert.That(Label(root, "PageLabel"), Does.Contain("Page 3/3"));
            Assert.That(Button(root, "Next").interactable, Is.False);
            Assert.That(root.GetComponentsInChildren<Transform>(true).Single(t => t.name == "PlayerRow_07").gameObject.activeSelf, Is.False);
            Click(root, "AwayTeam");
            Assert.That(Label(root, "PageLabel"), Is.EqualTo("0 players"));
            view.ShowReport(Data(), true);
            Assert.That(view.ShowingPlayers, Is.False);
            Click(root, "TopPerformersTab");
            Assert.That(PlayerName(root, 1), Does.Contain("Away player"));
        });
    }

    [Test]
    public void PendingShootoutKeepsReportUnavailableAndCompletedReportRestoresTabs()
    {
        WithView((root, view) =>
        {
            view.ShowReport(Data(), false);
            root.SetActive(true);
            Click(root, "TopPerformersTab");
            Assert.That(view.ShowingPlayers, Is.False);
            Assert.That(root.transform.Find(PostMatchReportEditorTools.ContentPath + "/ReportTabs").gameObject.activeSelf, Is.False);
            view.ShowReport(Data(), true);
            Click(root, "TopPerformersTab");
            Assert.That(view.ShowingPlayers, Is.True);
        });
    }

    private static MatchManager.GameData Data() => new()
    {
        stats = Stats(Player("h", "Home player", "Home", new MatchManager.PlayerStats { pacesRan = 10, goals = 1 }),
            Player("a", "Away player", "Away", new MatchManager.PlayerStats { pacesRan = 20, goals = 2 })),
    };

    private static void WithView(Action<GameObject, PostMatchReportController> test)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PostMatchReportEditorTools.PrefabPath);
        Assert.That(prefab, Is.Not.Null);
        // Own only a disposable prefab clone; no scene switch, live singleton or live saves.
        var root = Object.Instantiate(prefab);
        root.hideFlags = HideFlags.HideAndDontSave;
        root.SetActive(false);
        try { test(root, root.GetComponent<PostMatchReportController>()); }
        finally { Object.DestroyImmediate(root); }
    }

    private static UnityEngine.UI.Button Button(GameObject root, string name) =>
        root.GetComponentsInChildren<UnityEngine.UI.Button>(true).Single(b => b.name == name);
    private static void Click(GameObject root, string name) => Button(root, name).onClick.Invoke();
    private static string Label(GameObject root, string name) => root.GetComponentsInChildren<TMP_Text>(true).Single(t => t.name == name).text;
    private static string PlayerName(GameObject root, int row) => root.transform.Find(PostMatchReportEditorTools.ContentPath + $"/TopPerformers/PlayerRows/PlayerRow_{row:00}/Player").GetComponent<TMP_Text>().text;
}
