using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Probability-adjusted, equally weighted resolved checks. Does not infer player ratings.</summary>
public static class PostMatchLuckData
{
    public static readonly string[] Categories = { "Passing", "Recoveries", "Duels & control", "Shooting", "Goalkeeping", "Discipline & fitness" };
    public sealed class Check
    {
        public string Category, Label;
        public int Rolls;
        public double ExpectedHome, ActualHome;
    }
    public sealed class Report
    {
        public int TotalRolls, ForcedRolls, AssessedRolls, HomeRolls, AwayRolls;
        public int UnassessedRolls => TotalRolls - ForcedRolls - AssessedRolls;
        public readonly List<Check> Checks = new();
        public double ExpectedHome => Checks.Sum(c => c.ExpectedHome);
        public double ActualHome => Checks.Sum(c => c.ActualHome);
        public double HomeSurplus => ActualHome - ExpectedHome;
        public double HomeLuckPercentagePoints => Checks.Count == 0 ? 0 : 100 * HomeSurplus / Checks.Count;
    }
    private readonly struct Face
    {
        public readonly int Roll;
        public readonly bool Jackpot;
        public readonly double Probability;
        public Face(int roll, bool jackpot, double probability) { Roll = roll; Jackpot = jackpot; Probability = probability; }
    }
    private static IEnumerable<Face> Faces(bool jackpot)
    {
        for (int i = 1; i <= 5; i++) yield return new Face(i, false, 1d / 6);
        yield return new Face(6, false, jackpot ? 1d / 12 : 1d / 6);
        if (jackpot) yield return new Face(6, true, 1d / 12);
    }
    private static IEnumerable<Face> Faces(GameplayDiceEvent die) => Faces(die.luckJackpotEnabled ?? die.jackpotEnabled);

    public static Report Build(MatchManager.GameData data)
    {
        var report = new Report();
        var ids = new HashSet<int>();
        var rolls = (data?.events ?? new List<GameplayEvent>()).Where(e => e?.kind == "dice.roll" && e.dice != null
            && (e.sequenceNumber <= 0 || ids.Add(e.sequenceNumber))).ToList();
        report.TotalRolls = rolls.Count;
        report.ForcedRolls = rolls.Count(e => e.dice.overrideUsed);
        report.HomeRolls = rolls.Count(e => e.actorTokenKey?.StartsWith("Home:", StringComparison.Ordinal) == true);
        report.AwayRolls = rolls.Count(e => e.actorTokenKey?.StartsWith("Away:", StringComparison.Ordinal) == true);
        var groups = new Dictionary<string, List<GameplayEvent>>();
        foreach (var entry in rolls)
        {
            var model = Model(entry);
            if (model == null || model.version != 1 || model.side is not ("Home" or "Away")) continue;
            if (model.rule is "ground_duel" or "aerial_contest")
            {
                if (string.IsNullOrWhiteSpace(model.contestId)) continue;
                string key = model.rule + ":" + model.contestId;
                if (!groups.TryGetValue(key, out var group)) groups[key] = group = new();
                group.Add(entry);
                continue;
            }
            if (entry.dice.overrideUsed || entry.dice.roll < 1 || entry.dice.roll > 6) continue;
            double actual = Value(model, entry.dice.roll, entry.dice.isJackpot);
            if (double.IsNaN(actual)) continue;
            double expected = Faces(entry.dice).Sum(f => f.Probability * Value(model, f.Roll, f.Jackpot));
            Add(report, model, actual, expected, 1);
        }
        foreach (var group in groups.Values)
        {
            var model = Model(group[0]);
            if (group.Any(e => e.dice.overrideUsed || e.dice.roll < 1 || e.dice.roll > 6 || Model(e)?.side != model.side)) continue;
            if (model.rule == "ground_duel")
            {
                if (group.Count != 2) continue;
                var defense = group.FirstOrDefault(e => e.dice.context == "movement_tackle_defender");
                var attack = group.FirstOrDefault(e => e.dice.context == "movement_tackle_attacker");
                if (defense == null || attack == null) continue;
                double expected = 0;
                foreach (var d in Faces(defense.dice))
                    foreach (var a in Faces(attack.dice))
                        expected += d.Probability * a.Probability * DuelValue(model, d.Roll, d.Jackpot, a.Roll, a.Jackpot);
                Add(report, model, DuelValue(model, defense.dice.roll, defense.dice.isJackpot, attack.dice.roll, attack.dice.isJackpot), expected, 2);
            }
            else
            {
                var contestants = model.contenders;
                if (contestants == null || contestants.Count == 0 || contestants.All(c => !c.attacking)
                    || contestants.Select(c => c.tokenKey).Distinct().Count() != contestants.Count
                    || group.Count != contestants.Count || group.Select(e => e.actorTokenKey).Distinct().Count() != contestants.Count
                    || contestants.Any(c => group.All(e => e.actorTokenKey != c.tokenKey))) continue;
                var byToken = group.ToDictionary(e => e.actorTokenKey);
                var attackDistribution = BestDistribution(contestants.Where(c => c.attacking), byToken);
                var defenseDistribution = BestDistribution(contestants.Where(c => !c.attacking), byToken);
                double expected = 0;
                foreach (var a in attackDistribution)
                    foreach (var d in defenseDistribution)
                        expected += a.Value * d.Value * AerialValue(model, a.Key.score, a.Key.onTarget, d.Key.score);
                var actualAttack = BestActual(contestants.Where(c => c.attacking), byToken);
                var actualDefense = BestActual(contestants.Where(c => !c.attacking), byToken);
                Add(report, model, AerialValue(model, actualAttack.score, actualAttack.onTarget, actualDefense.score), expected, group.Count);
            }
        }
        return report;
    }

    private static GameplayLuckModel Model(GameplayEvent e)
    {
        string side = e.actorTokenKey?.StartsWith("Home:", StringComparison.Ordinal) == true ? "Home"
            : e.actorTokenKey?.StartsWith("Away:", StringComparison.Ordinal) == true ? "Away" : null;
        return e.dice.luck ?? GameplayLuckModel.Capture(e.dice.context, side, null, e.dice.modifiers);
    }
    private static void Add(Report report, GameplayLuckModel model, double actual, double expected, int rolls)
    {
        if (double.IsNaN(expected) || double.IsInfinity(expected) || expected < 0 || expected > 1 + 1e-9) return;
        report.AssessedRolls += rolls;
        report.Checks.Add(new Check { Category = model.category, Label = model.label, Rolls = rolls,
            ActualHome = model.side == "Home" ? actual : 1 - actual,
            ExpectedHome = model.side == "Home" ? expected : 1 - expected });
    }
    private static double Compare(int score, int target) => score > target ? 1 : score == target ? .5 : 0;
    private static double Value(GameplayLuckModel m, int roll, bool jackpot)
    {
        int total = GameplayScore.Total(m.skill, roll, m.modifier, jackpot);
        return m.rule switch
        {
            "threshold" => total >= m.threshold ? 1 : 0,
            "above" => roll > m.threshold ? 1 : 0,
            "below" => roll < m.threshold ? 1 : 0,
            "comparison" => m.outsideHands ? 0 : Compare(total, m.threshold),
            "recovery" => roll <= m.foulThreshold ? 0 : roll >= m.naturalSuccess
                || (m.rawModifier ? roll + m.skill + m.modifier : GameplayScore.Total(m.skill, roll, m.modifier)) >= m.threshold ? 1 : 0,
            _ => double.NaN
        };
    }
    private static double DuelValue(GameplayLuckModel m, int defenderRoll, bool defenderJackpot, int attackerRoll, bool attackerJackpot)
        => m.outsideHands || !defenderJackpot && defenderRoll <= m.foulThreshold ? 1
            : Compare(GameplayScore.Total(m.attackerSkill, attackerRoll, jackpot: attackerJackpot),
                GameplayScore.Total(m.defenderSkill, defenderRoll, m.defenderModifier, defenderJackpot));
    private static double AerialValue(GameplayLuckModel m, int attackScore, bool onTarget, int defenseScore)
        => attackScore > defenseScore ? m.atGoal && !onTarget ? 0 : 1 : attackScore == defenseScore ? .5 : 0;

    private static Dictionary<(int score, bool onTarget), double> BestDistribution(IEnumerable<GameplayLuckModel.Contender> contestants, Dictionary<string, GameplayEvent> rolls)
    {
        var distribution = new Dictionary<(int, bool), double> { [(-1, false)] = 1 };
        foreach (var c in contestants)
        {
            var next = new Dictionary<(int, bool), double>();
            foreach (var previous in distribution)
                foreach (var face in Faces(rolls[c.tokenKey].dice))
                {
                    int score = GameplayScore.Total(c.skill, face.Roll, c.modifier, face.Jackpot);
                    var key = score > previous.Key.Item1 ? (score, face.Roll > 1) : previous.Key;
                    next[key] = next.TryGetValue(key, out double value) ? value + previous.Value * face.Probability : previous.Value * face.Probability;
                }
            distribution = next;
        }
        return distribution;
    }
    private static (int score, bool onTarget) BestActual(IEnumerable<GameplayLuckModel.Contender> contestants, Dictionary<string, GameplayEvent> rolls)
    {
        var best = (score: -1, onTarget: false);
        foreach (var c in contestants)
        {
            var roll = rolls[c.tokenKey].dice;
            int score = GameplayScore.Total(c.skill, roll.roll, c.modifier, roll.isJackpot);
            if (score > best.score) best = (score, roll.roll > 1);
        }
        return best;
    }
}
