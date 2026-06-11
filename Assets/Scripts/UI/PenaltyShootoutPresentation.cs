using System;
using UnityEngine;

public readonly struct PenaltyShootoutDisplayState
{
    public readonly int homeBaseGoals;
    public readonly int awayBaseGoals;
    public readonly int homePenaltyGoals;
    public readonly int awayPenaltyGoals;
    public readonly string clockText;

    public PenaltyShootoutDisplayState(int homeBaseGoals, int awayBaseGoals, int homePenaltyGoals, int awayPenaltyGoals, string clockText)
    {
        this.homeBaseGoals = homeBaseGoals;
        this.awayBaseGoals = awayBaseGoals;
        this.homePenaltyGoals = homePenaltyGoals;
        this.awayPenaltyGoals = awayPenaltyGoals;
        this.clockText = clockText;
    }
}

public static class PenaltyShootoutPresentation
{
    private const int ExtraTimeMinutes = 30;

    public static bool TryGetDisplayState(MatchManager matchManager, out PenaltyShootoutDisplayState state)
    {
        state = default;

        PenaltyShootoutManager shootout = PenaltyShootoutManager.ActiveShootout ?? UnityEngine.Object.FindAnyObjectByType<PenaltyShootoutManager>();
        if (shootout == null || !shootout.IsOrderSelectionOrShootoutStarted)
        {
            return false;
        }

        MatchManager resolvedMatchManager = matchManager != null ? matchManager : shootout.matchManager ?? MatchManager.Instance;
        state = new PenaltyShootoutDisplayState(
            shootout.HomePreShootoutGoals,
            shootout.AwayPreShootoutGoals,
            shootout.homeShootoutScore,
            shootout.awayShootoutScore,
            BuildShootoutClockText(resolvedMatchManager));
        return true;
    }

    private static string BuildShootoutClockText(MatchManager matchManager)
    {
        MatchManager.GameSettings settings = matchManager?.gameData?.gameSettings;
        int numberOfHalfs = Mathf.Clamp(settings?.numberOfHalfs ?? 2, 1, 2);
        int halfDuration = Mathf.Clamp(settings?.halfDuration ?? 45, 15, 60);
        string tiebreaker = settings?.tiebreaker ?? string.Empty;
        int extraTimeMinutes = tiebreaker.IndexOf("Extra Time", StringComparison.OrdinalIgnoreCase) >= 0
            ? ExtraTimeMinutes
            : 0;

        return $"{(numberOfHalfs * halfDuration) + extraTimeMinutes}:00";
    }
}
