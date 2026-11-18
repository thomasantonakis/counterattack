using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[Category("PostMatchRatings")]
public sealed class PostMatchRatingEditorTests
{
    private static MatchManager.PlayerStatsEntry Entry(string key, string name, MatchManager.PlayerStats stats, string side = "Home")
        => new() { playerKey = key, playerName = name, teamSide = side, jerseyNumber = 9, stats = stats };
    private static MatchManager.Stats Stats(params MatchManager.PlayerStatsEntry[] entries) => new() { playerStats = entries.ToList() };

    [Test]
    public void OneFormulaProducesAnExactInspectableBreakdown()
    {
        var stats = Stats(Entry("Home:9", "Player", new MatchManager.PlayerStats { goals=1, assists=1,
            passesAttempted=10, passesCompleted=8, aerialPassesAttempted=3, aerialPassesCompleted=2,
            dribblesMade=2, possessionLost=1, possessionWon=3, shotBlocksMade=1, attemptsSaved=2, yellowCards=1 }));
        var row = PostMatchRatingData.Build(stats).Single();
        Assert.That(row.Raw, Is.EqualTo(8.24).Within(1e-9)); Assert.That(row.DisplayRating, Is.EqualTo("8.2"));
        Assert.That(row.Contributions.Select(c=>c.Label), Is.EqualTo(new[] {"Attack","Passing","Control","Defence","Goalkeeping","Discipline"}));
        Assert.That(PostMatchRatingData.Base + row.Contributions.Sum(c=>c.Points), Is.EqualTo(row.Raw).Within(1e-9));
        Assert.That(row.Breakdown, Does.Contain("Attack +1.50").And.Contain("Passing +0.08").And.Contain("Raw 8.24"));
    }

    [TestCase(100,0,10)]
    [TestCase(0,100,1)]
    public void FinalRatingIsBoundedAndRawCalculationRemainsVisible(int goals, int redCards, int expected)
    {
        var row=PostMatchRatingData.Build(Stats(Entry("Home:9","Player",new MatchManager.PlayerStats {goals=goals,redCards=redCards}))).Single();
        Assert.That(row.Rating,Is.EqualTo(expected)); Assert.That(row.Raw,Is.Not.EqualTo(row.Rating));
    }

    [Test]
    public void TacklesInterceptionsAndDuelAliasesDoNotDuplicateRecoveryCredit()
    {
        var first=Entry("Home:1","First",new MatchManager.PlayerStats {possessionWon=2});
        var second=Entry("Away:1","Second",new MatchManager.PlayerStats {possessionWon=2,tacklesMade=2,interceptionsMade=2,
            groundDuelsWon=2,aerialChallengesWon=2,xRecoveries=1.5f},"Away");
        Assert.That(PostMatchRatingData.Build(Stats(first,second)).All(r=>System.Math.Abs(r.Rating.Value-6.24)<1e-9),Is.True);
    }

    [Test]
    public void DistanceInjuryAndExpectedStatsDoNotManufactureRatings()
    {
        var row=PostMatchRatingData.Build(Stats(Entry("Home:1","Untouched",new MatchManager.PlayerStats {pacesRan=1000,injuries=2,xGoals=2,xRecoveries=3}))).Single();
        Assert.That(row.Rating,Is.Null); Assert.That(row.DisplayRating,Is.EqualTo("NR")); Assert.That(row.Breakdown,Does.Contain("Not rated"));
    }

    [Test]
    public void ZeroNetPassingStillHasEvidenceAndLegacyCompletionsDoNotCreateNegativeFailures()
    {
        var even=Entry("Home:1","Even",new MatchManager.PlayerStats {passesAttempted=3,passesCompleted=2});
        var legacy=Entry("Away:1","Legacy",new MatchManager.PlayerStats {passesCompleted=3},"Away");
        var rows=PostMatchRatingData.Build(Stats(even,legacy));
        Assert.That(rows.Single(r=>r.Key=="Home:1").Rating,Is.EqualTo(6));
        Assert.That(rows.Single(r=>r.Key=="Away:1").Rating,Is.EqualTo(6.06).Within(1e-9));
    }

    [Test]
    public void ProjectionIsReadOnlyKeepsIdentitiesAndUsesStableTiesWithUnratedPlayersLast()
    {
        var stats=Stats(Entry("Away:9","Same",new MatchManager.PlayerStats {goals=1},"Away"),
            Entry("Home:9","Same",new MatchManager.PlayerStats {goals=1}), Entry("Home:1","Alpha",new MatchManager.PlayerStats()));
        string before=JsonConvert.SerializeObject(stats);
        Assert.That(PostMatchRatingData.Build(stats).Select(r=>r.Key),Is.EqualTo(new[]{"Away:9","Home:9","Home:1"}));
        Assert.That(JsonConvert.SerializeObject(stats),Is.EqualTo(before)); Assert.That(PostMatchRatingData.Build(null),Is.Empty);
    }

    [Test]
    public void SameFormulaAppliesToEveryPlayerAndNegativeLegacyCountersAreIgnored()
    {
        var row=PostMatchRatingData.Build(Stats(Entry("Home:9","Legacy",new MatchManager.PlayerStats {goals=-2,passesAttempted=-10,yellowCards=-1}))).Single();
        Assert.That(row.Rating,Is.Null);
        var rows=PostMatchRatingData.Build(Stats(Entry("Home:1","Keeper",new MatchManager.PlayerStats {passesAttempted=3,passesCompleted=3}),
            Entry("Away:9","Forward",new MatchManager.PlayerStats {passesAttempted=3,passesCompleted=3},"Away")));
        Assert.That(rows[0].Rating,Is.EqualTo(rows[1].Rating));
    }

    [Test]
    public void AuthoredPrototypeSupportsSelectionPagingEmptyStateAndTabIsolationWithoutCreatingObjects()
    {
        var root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PostMatchReportEditorTools.PrefabPath));
        try
        {
            var data=new MatchManager.GameData(); data.gameSettings.homeTeamName="Inverness"; data.gameSettings.awayTeamName="Aurora";
            for(int i=0;i<12;i++) data.stats.playerStats.Add(Entry("Home:"+i,"Player "+i,new MatchManager.PlayerStats {goals=i}));
            var report=root.GetComponent<PostMatchReportController>(); int count=root.GetComponentsInChildren<Transform>(true).Length;
            report.ShowReport(data,true); var content=root.transform.Find(PostMatchReportEditorTools.ContentPath);
            content.Find("ReportTabs/RatingsTab").GetComponent<Button>().onClick.Invoke();
            Assert.That(report.ShowingRatings,Is.True); Assert.That(report.ShowingPlayers||report.ShowingPitch||report.ShowingLuck,Is.False);
            Assert.That(content.Find("TeamFilters").gameObject.activeSelf,Is.False);
            var page=content.Find("Ratings"); page.Find("RatingRow1").GetComponent<Button>().onClick.Invoke();
            Assert.That(page.Find("RatingBreakdown").GetComponent<TMP_Text>().text,Does.Contain("Base 6.00"));
            Assert.That(page.GetComponentsInChildren<PostMatchRatingView>(true).Single(), Is.Not.Null);
            Assert.That(page.Find("RatingRow10").gameObject.activeSelf, Is.True, "The first page shows eleven ratings.");
            page.Find("RatingPaging/RatingNext").GetComponent<Button>().onClick.Invoke();
            Assert.That(page.Find("RatingPaging/RatingPage").GetComponent<TMP_Text>().text,Does.Contain("Page 2/2"));
            Assert.That(page.Find("RatingRow0/Rank").GetComponent<TMP_Text>().text,Is.EqualTo("12"));
            Assert.That(page.Find("RatingFormula").GetComponent<TMP_Text>().text,Is.EqualTo(PostMatchRatingData.FormulaText));
            report.ShowReport(data,true); report.ShowRatings();
            page.Find("RatingPaging/RatingNext").GetComponent<Button>().onClick.Invoke();
            Assert.That(page.Find("RatingPaging/RatingPage").GetComponent<TMP_Text>().text,Does.Contain("Page 2/2"),"Reopening must not duplicate listeners.");
            Assert.That(root.GetComponentsInChildren<Transform>(true).Length,Is.EqualTo(count));
            report.ShowStats(); Assert.That(report.ShowingRatings,Is.False);
            report.ShowReport(new MatchManager.GameData(),true); report.ShowRatings();
            Assert.That(page.Find("RatingBreakdown").GetComponent<TMP_Text>().text,Does.Contain("No player statistics"));
            report.ShowReport(data,false); report.ShowRatings(); Assert.That(report.ShowingRatings,Is.False);
        }
        finally { Object.DestroyImmediate(root); }
    }
}
