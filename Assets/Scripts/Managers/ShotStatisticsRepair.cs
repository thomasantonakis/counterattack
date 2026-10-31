using System;
using System.Collections.Generic;
using System.Linq;

public static class ShotStatisticsRepair
{
    // Old shot-goal branches omitted ShotOnTarget. Recover those missing values
    // from the saved event stream without adding them again on each reload.
    public static void RepairSavedGoals(MatchManager.GameData data)
    {
        if (data?.stats?.playerStats == null || data.events == null) return;
        var recorded = new Dictionary<string, int>();
        var missing = new Dictionary<string, int>();
        var attempts = new Dictionary<string, int>();
        var goals = new Dictionary<string, int>();
        var onTargetInAttempt = new HashSet<string>();
        var events = data.events.Where(item => item != null).OrderBy(item => item.sequenceNumber).ToList();
        // A restarted match can retain the earlier event history in the same save.
        // Its goals must never be added to the new match's counters.
        int start = 0;
        for (int i = 1; i < events.Count; i++)
            if (events[i].currentHalf == 1 && events[i].currentGameState == nameof(MatchManager.GameState.KickOffSetup)
                && events[i].matchClockSeconds + 1f < events[i - 1].matchClockSeconds)
                start = i;
        foreach (var item in events.Skip(start))
        {
            if (item.kind != "stats.action" || string.IsNullOrEmpty(item.actorTokenKey)
                || item.result?.details == null || !item.result.details.TryGetValue("actionType", out string action)) continue;
            string key = item.actorTokenKey;
            int value = item.result.details.TryGetValue("value", out string text) && int.TryParse(text, out int parsed) ? parsed : 1;
            if (action == nameof(MatchManager.ActionType.ShotAttempt))
            {
                attempts[key] = attempts.GetValueOrDefault(key) + value;
                onTargetInAttempt.Remove(key);
            }
            if (action == nameof(MatchManager.ActionType.ShotOnTarget))
            {
                recorded[key] = recorded.GetValueOrDefault(key) + value;
                onTargetInAttempt.Add(key);
            }
            if (action == nameof(MatchManager.ActionType.GoalScored))
            {
                goals[key] = goals.GetValueOrDefault(key) + value;
                if (!onTargetInAttempt.Contains(key)) missing[key] = missing.GetValueOrDefault(key) + value;
                onTargetInAttempt.Remove(key);
            }
        }
        int homeAdded = 0, awayAdded = 0;
        foreach (var player in data.stats.playerStats.Where(player => player?.stats != null))
        {
            if (string.IsNullOrEmpty(player.playerKey)
                || attempts.GetValueOrDefault(player.playerKey) != player.stats.shotsAttempted
                || goals.GetValueOrDefault(player.playerKey) != player.stats.goals) continue;
            int expected = Math.Min(player.stats.shotsAttempted,
                recorded.GetValueOrDefault(player.playerKey) + missing.GetValueOrDefault(player.playerKey));
            int added = expected - player.stats.shotsOnTarget;
            player.stats.shotsOnTarget += added;
            if (player.teamSide == "Home") homeAdded += added;
            else if (player.teamSide == "Away") awayAdded += added;
        }
        if (data.stats.homeTeamStats != null) data.stats.homeTeamStats.totalShotsOnTarget = Math.Max(0, data.stats.homeTeamStats.totalShotsOnTarget + homeAdded);
        if (data.stats.awayTeamStats != null) data.stats.awayTeamStats.totalShotsOnTarget = Math.Max(0, data.stats.awayTeamStats.totalShotsOnTarget + awayAdded);
    }
}
