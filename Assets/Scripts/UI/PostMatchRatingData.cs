using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/// <summary>One deliberately simple, read-only prototype formula for every position.</summary>
public static class PostMatchRatingData
{
    public const double Base = 6;
    public const string FormulaText = "Base 6.0; goals +0.90, assists +0.60; completed passes +0.02, uncompleted passes -0.04; dribbles +0.10, possession losses -0.12; recoveries +0.12, shot blocks +0.12; saves +0.20; yellow cards -0.30, red cards -0.80. Final score limited to 1.0–10.0.";
    public sealed class Contribution
    {
        public string Label { get; }
        public double Points { get; }
        public Contribution(string label, double points) { Label = label; Points = points; }
    }
    public sealed class Row
    {
        public string Key, Name, Side;
        public int Jersey;
        public double? Rating;
        public double Raw;
        public IReadOnlyList<Contribution> Contributions;
        public string DisplayRating => Rating.HasValue ? Rating.Value.ToString("0.0", CultureInfo.InvariantCulture) : "NR";
        public string Breakdown => !Rating.HasValue ? "Not rated: no contributions covered by this prototype formula were recorded."
            : "Base 6.00\n" + string.Join(" · ", Contributions.Select(c => c.Label + " " + Signed(c.Points)))
                + "\nRaw " + Raw.ToString("0.00", CultureInfo.InvariantCulture) + " → " + DisplayRating + "/10";
    }
    private static double Count(int value) => Math.Max(0d, value);
    public static IReadOnlyList<Row> Build(MatchManager.Stats stats)
    {
        if (stats?.playerStats == null) return Array.Empty<Row>();
        return stats.playerStats.Where(e => e?.stats != null).Select(Project)
            .OrderByDescending(r => r.Rating.HasValue).ThenByDescending(r => r.Rating ?? 0)
            .ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase).ThenBy(r => r.Side, StringComparer.Ordinal)
            .ThenBy(r => r.Jersey).ThenBy(r => r.Key, StringComparer.Ordinal).ToArray();
    }
    private static Row Project(MatchManager.PlayerStatsEntry entry)
    {
        var s = entry.stats;
        double completed = Count(s.passesCompleted) + Count(s.aerialPassesCompleted);
        double uncompleted = Math.Max(0, Count(s.passesAttempted) - Count(s.passesCompleted))
            + Math.Max(0, Count(s.aerialPassesAttempted) - Count(s.aerialPassesCompleted));
        var contributions = new[] {
            new Contribution("Attack", .9 * Count(s.goals) + .6 * Count(s.assists)),
            new Contribution("Passing", .02 * completed - .04 * uncompleted),
            new Contribution("Control", .10 * Count(s.dribblesMade) - .12 * Count(s.possessionLost)),
            new Contribution("Defence", .12 * Count(s.possessionWon) + .12 * Count(s.shotBlocksMade)),
            new Contribution("Goalkeeping", .20 * Count(s.attemptsSaved)),
            new Contribution("Discipline", -.30 * Count(s.yellowCards) - .80 * Count(s.redCards)) };
        double evidence = completed + uncompleted + Count(s.goals) + Count(s.assists) + Count(s.dribblesMade)
            + Count(s.possessionLost) + Count(s.possessionWon) + Count(s.shotBlocksMade) + Count(s.attemptsSaved) + Count(s.yellowCards) + Count(s.redCards);
        double raw = Base + contributions.Sum(c => c.Points);
        return new Row { Key = entry.playerKey ?? "", Name = !string.IsNullOrWhiteSpace(entry.displayName) ? entry.displayName
                : !string.IsNullOrWhiteSpace(entry.playerName) ? entry.playerName : "Unknown player",
            Side = string.Equals(entry.teamSide, "Home", StringComparison.OrdinalIgnoreCase) ? "Home"
                : string.Equals(entry.teamSide, "Away", StringComparison.OrdinalIgnoreCase) ? "Away" : "Unassigned",
            Jersey = entry.jerseyNumber, Raw = raw, Rating = evidence > 0 ? Math.Max(1, Math.Min(10, raw)) : null, Contributions = contributions };
    }
    private static string Signed(double value) => (Math.Abs(value) < .000001 ? 0 : value).ToString("+0.00;-0.00;0.00", CultureInfo.InvariantCulture);
}
