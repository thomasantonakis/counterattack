using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/// <summary>Read-only projections of recorded match statistics. Never creates player-stat entries.</summary>
public static class PostMatchLeaderboard
{
    // Retain existing serialized IDs when removing metrics.
    public enum Metric { Distance = 0, Recoveries = 1, Passes = 2, Duels = 4, Goals = 5, Assists = 6 }
    public enum Team { Both, Home, Away }

    public sealed class Row
    {
        public string PlayerKey { get; }
        public string PlayerName { get; }
        public string TeamSide { get; }
        public int JerseyNumber { get; }
        public double Value { get; }
        public int Attempts { get; }
        public string DisplayValue { get; }

        internal Row(MatchManager.PlayerStatsEntry entry, double value, int attempts, string displayValue)
        {
            PlayerKey = entry.playerKey ?? string.Empty;
            PlayerName = !string.IsNullOrWhiteSpace(entry.displayName) ? entry.displayName
                : !string.IsNullOrWhiteSpace(entry.playerName) ? entry.playerName : "Unknown player";
            TeamSide = string.Equals(entry.teamSide, "Home", StringComparison.OrdinalIgnoreCase) ? "Home"
                : string.Equals(entry.teamSide, "Away", StringComparison.OrdinalIgnoreCase) ? "Away" : "Unassigned";
            JerseyNumber = entry.jerseyNumber;
            Value = value;
            Attempts = attempts;
            DisplayValue = displayValue;
        }
    }

    public static string Title(Metric metric) => metric switch
    {
        Metric.Distance => "Distance",
        Metric.Recoveries => "Possession recoveries",
        Metric.Passes => "Completed passes",
        Metric.Duels => "Duels won",
        Metric.Goals => "Goals",
        Metric.Assists => "Assists",
        _ => throw new ArgumentOutOfRangeException(nameof(metric)),
    };

    public static string Description(Metric metric) => metric switch
    {
        Metric.Distance => "Distance travelled in paces.",
        Metric.Recoveries => "Recoveries made / expected recoveries. Includes interceptions without counting them twice.",
        Metric.Passes => "Completed / attempted and accuracy. Ground and aerial passes combined.",
        Metric.Duels => "Won / contested and success rate. Ground and aerial duels combined.",
        Metric.Goals => "Recorded match goals. Penalty shootout scores are separate.",
        Metric.Assists => "Recorded assists.",
        _ => throw new ArgumentOutOfRangeException(nameof(metric)),
    };

    public static string ColumnTitle(Metric metric) => metric switch
    {
        Metric.Recoveries => "Recoveries / xRecoveries",
        Metric.Passes => "Passes / Accuracy",
        Metric.Duels => "Duels / Success",
        _ => Title(metric),
    };

    public static IReadOnlyList<Row> Build(MatchManager.Stats stats, Metric metric, Team team = Team.Both)
    {
        if (stats?.playerStats == null) return Array.Empty<Row>();
        return stats.playerStats
            .Where(entry => entry?.stats != null)
            .Select(entry => Project(entry, metric))
            .Where(row => (row.Value > 0 || (metric == Metric.Duels || metric == Metric.Passes) && row.Attempts > 0)
                && (team == Team.Both || row.TeamSide == team.ToString()))
            .OrderByDescending(row => row.Value)
            .ThenBy(row => metric == Metric.Duels || metric == Metric.Passes ? row.Attempts : 0)
            .ThenBy(row => row.PlayerName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.TeamSide, StringComparer.Ordinal)
            .ThenBy(row => row.JerseyNumber)
            .ThenBy(row => row.PlayerKey, StringComparer.Ordinal)
            .ToArray();
    }

    private static Row Project(MatchManager.PlayerStatsEntry entry, Metric metric)
    {
        var s = entry.stats;
        int passes = s.passesCompleted + s.aerialPassesCompleted;
        int attempts = s.passesAttempted + s.aerialPassesAttempted;
        int duelsWon = s.groundDuelsWon + s.aerialChallengesWon;
        int duels = s.groundDuelsInvolved + s.aerialChallengesInvolved;
        string accuracy = attempts > 0 ? ((double)passes * 100 / attempts).ToString("0.#", CultureInfo.InvariantCulture) + "%" : "—";
        return metric switch
        {
            Metric.Distance => Count(entry, s.pacesRan),
            Metric.Recoveries => new Row(entry, s.possessionWon, 0,
                $"{s.possessionWon} / {s.xRecoveries.ToString("0.00", CultureInfo.InvariantCulture)}"),
            Metric.Passes => new Row(entry, passes, attempts, $"{passes}/{attempts}  ·  {accuracy}"),
            Metric.Duels => new Row(entry, duelsWon, duels, $"{duelsWon}/{duels}  ·  {Percentage(duelsWon, duels)}"),
            Metric.Goals => Count(entry, s.goals),
            Metric.Assists => Count(entry, s.assists),
            _ => throw new ArgumentOutOfRangeException(nameof(metric)),
        };
    }

    private static Row Count(MatchManager.PlayerStatsEntry entry, int value) =>
        new(entry, value, 0, value.ToString(CultureInfo.InvariantCulture));

    private static string Percentage(int successes, int attempts) => attempts > 0
        ? ((double)successes * 100 / attempts).ToString("0.#", CultureInfo.InvariantCulture) + "%" : "—";
}
