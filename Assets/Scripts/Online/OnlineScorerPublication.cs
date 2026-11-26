using System;
using System.Collections.Generic;
using System.Linq;

namespace CounterAttack.Online
{
    internal static class OnlineScorerPublication
    {
        private const int GoalLimit = 256;
        private const int NameLimit = 256;
        private const int MinuteLabelLimit = 32;

        internal static MatchManager.GoalEvent[] Capture(IEnumerable<MatchManager.GoalEvent> goals)
        {
            if (goals == null) return Array.Empty<MatchManager.GoalEvent>();

            return goals.Where(goal => goal != null).Take(GoalLimit).Select(goal => new MatchManager.GoalEvent
            {
                scorer = Bound(goal.scorer, NameLimit),
                scorerKey = Bound(goal.scorerKey, NameLimit),
                minute = goal.minute,
                minuteLabel = Bound(goal.minuteLabel, MinuteLabelLimit),
                isPenalty = goal.isPenalty,
                assist = Bound(goal.assist, NameLimit),
                assistKey = Bound(goal.assistKey, NameLimit)
            }).ToArray();
        }

        internal static bool Valid(MatchManager.GoalEvent[] goals)
            => goals == null || goals.Length <= GoalLimit && goals.All(goal =>
                goal != null && goal.minute >= 0 && Fits(goal.scorer, NameLimit) && Fits(goal.assist, NameLimit)
                && Fits(goal.scorerKey, NameLimit) && Fits(goal.assistKey, NameLimit)
                && Fits(goal.minuteLabel, MinuteLabelLimit));

        private static bool Fits(string value, int limit) => value == null || value.Length <= limit;
        private static string Bound(string value, int limit) => Fits(value, limit) ? value : value.Substring(0, limit);
    }
}
