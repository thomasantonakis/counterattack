using System.Reflection;
using Newtonsoft.Json;
using NUnit.Framework;

public sealed class PossessionTimeEditorTests
{
    private static string Display(MatchManager.TeamStats team, MatchManager.TeamStats opponent)
        => (string)typeof(MatchStatsUI).GetMethod("FormatPossession", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, new object[] { team, opponent });

    [Test]
    public void PercentageUsesTimeEvenWhenRecoveryCountsFavorOpponent()
    {
        var home = new MatchManager.TeamStats { totalPossessionSeconds = 90, totalPossessionWon = 1 };
        var away = new MatchManager.TeamStats { totalPossessionSeconds = 30, totalPossessionWon = 9 };
        Assert.That(Display(home, away), Is.EqualTo("75%"));
        Assert.That(Display(away, home), Is.EqualTo("25%"));
    }

    [Test]
    public void OldSaveStartsWithoutInventedTime()
    {
        var stats = JsonConvert.DeserializeObject<MatchManager.Stats>(
            "{\"homeTeamStats\":{\"totalPossessionWon\":6},\"awayTeamStats\":{\"totalPossessionWon\":4}}");
        Assert.That(stats.homeTeamStats.totalPossessionSeconds, Is.Zero);
        Assert.That(Display(stats.homeTeamStats, stats.awayTeamStats), Is.EqualTo("50%"));
    }

    [Test]
    public void SaveRoundTripAndPlayerTotalsRebuildPreserveTeamTime()
    {
        var stats = new MatchManager.Stats();
        stats.homeTeamStats.totalPossessionSeconds = 123.5f;
        stats.awayTeamStats.totalPossessionSeconds = 45.25f;
        stats.playerStats.Add(new MatchManager.PlayerStatsEntry
        {
            teamSide = "Home", stats = new MatchManager.PlayerStats { possessionWon = 2 }
        });
        var restored = JsonConvert.DeserializeObject<MatchManager.Stats>(JsonConvert.SerializeObject(stats));
        restored.UpdateTeamStats(true);
        Assert.That(restored.homeTeamStats.totalPossessionSeconds, Is.EqualTo(123.5f));
        Assert.That(restored.awayTeamStats.totalPossessionSeconds, Is.EqualTo(45.25f));
        Assert.That(restored.homeTeamStats.totalPossessionWon, Is.EqualTo(2));
        restored.homeTeamStats.Reset();
        Assert.That(restored.homeTeamStats.totalPossessionSeconds, Is.Zero);
    }

    [Test]
    public void RuntimeSnapshotPreservesRegulationTeamAndExtraActionStarter()
    {
        using var fixture = new GoalkeeperBoxEntryFixture();
        var match = fixture.match;
        match.teamInAttack = MatchManager.TeamInAttack.Home;
        match.CommitToAction(MatchManager.MatchActionKind.StandardPass);
        match.ChangePossession();
        var snapshot = JsonConvert.DeserializeObject<RoomRuntimeSnapshot>(
            JsonConvert.SerializeObject(match.CreateRuntimeSnapshot()));
        match.CommitToAction(MatchManager.MatchActionKind.MovementPhase);
        GoalkeeperBoxEntryFixture.Invoke(match, "RestoreSnapshotStateFields", snapshot);
        GoalkeeperBoxEntryFixture.Invoke(match, "AdvanceMatchTime", 10f);
        Assert.That(match.gameData.stats.homeTeamStats.totalPossessionSeconds, Is.Zero);
        Assert.That(match.gameData.stats.awayTeamStats.totalPossessionSeconds, Is.EqualTo(10f));
        match.isHalfExpired = true;
        match.extraActionsDetermined = true;
        match.extraActionsTotal = match.extraActionsRemaining = 1;
        match.RecordResolvedAction(MatchManager.MatchActionKind.StandardPass);
        Assert.That(match.gameData.stats.homeTeamStats.totalPossessionSeconds, Is.EqualTo(60f), "The restored action starter keeps its extra-action allocation.");
        Assert.That(match.gameData.stats.awayTeamStats.totalPossessionSeconds, Is.EqualTo(10f));
    }
}
