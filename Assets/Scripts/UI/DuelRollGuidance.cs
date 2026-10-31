/// <summary>Presentation of the existing ground-duel comparison; never rolls dice.</summary>
public static class DuelRollGuidance
{
    public static string Percent(float probability)
        => (probability * 100f).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "%";

    public static string RollTotal(int roll, int skill, int bonus, bool jackpot)
        => jackpot ? "Roll JACKPOT · Total JACKPOT"
            : $"Roll {roll} + {skill}{(bonus != 0 ? $" + {bonus}" : "")} = {roll + skill + bonus}";

    public static string ThresholdCheck(string attribute, int threshold, string bad, string good)
    {
        float badProbability = System.Math.Max(0f, System.Math.Min(1f, (7f - threshold) / 6f));
        int safeMax = System.Math.Max(0, System.Math.Min(6, threshold - 1));
        string safeRange = safeMax == 0 ? "None" : safeMax == 1 ? "1" : $"1–{safeMax}";
        return $"{attribute}: {threshold}\n{System.Math.Max(1, threshold)}+: {bad} ({Percent(badProbability)})\n"
            + $"{safeRange}: {good} ({Percent(1f - badProbability)})";
    }

    public static string LiveOutcomes(ExpectedStatsCalculator.GroundDuelExpectation chances)
        => $"Dribble: {Percent(chances.attackerWinProbability)} - Loose ball: {Percent(chances.tieProbability)}"
            + $" - Successful tackle: {Percent(chances.defenderWinProbability)}";

    public static string ForDribbler(int dribbling, int defenderTotal, bool defenderJackpot, bool foul, bool penalty = false)
    {
        if (foul) return penalty ? "Penalty awarded — dribbler roll still required" : "Foul committed — dribbler roll still required";
        if (defenderJackpot) return "JACKPOT to tie";
        int tie = defenderTotal - dribbling;
        int win = tie + 1;
        if (win <= 1) return "Any roll wins";
        string winText = win <= 6 ? $"{win}+ to win" : "JACKPOT to win";
        return tie >= 1 && tie <= 6 ? $"{winText}; {tie} ties" : winText;
    }
}
