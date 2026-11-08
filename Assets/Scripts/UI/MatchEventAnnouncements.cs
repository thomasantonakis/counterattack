using System;

public enum MatchBannerEvent
{
    YellowCard, SecondYellow, RedCard, Injury, SecondInjury, Substitution,
    CornerKick, GoalKick, ThrowIn, Foul, StoppageTime, HalfTime, ExtraTime, Penalties
}

/// <summary>Live presentation notifications only. Save restoration does not replay announcements.</summary>
public static class MatchEventAnnouncements
{
    public static event Action<MatchBannerEvent, string> Published;

    public static void Publish(MatchBannerEvent kind, string detail = "")
    {
        Published?.Invoke(kind, detail ?? string.Empty);
    }

    public static string Caption(MatchBannerEvent kind) => kind switch
    {
        MatchBannerEvent.YellowCard => "YELLOW CARD",
        MatchBannerEvent.SecondYellow => "SECOND YELLOW · RED CARD",
        MatchBannerEvent.RedCard => "RED CARD",
        MatchBannerEvent.Injury => "INJURY",
        MatchBannerEvent.SecondInjury => "SECOND INJURY",
        MatchBannerEvent.Substitution => "SUBSTITUTION",
        MatchBannerEvent.CornerKick => "CORNER KICK",
        MatchBannerEvent.GoalKick => "GOAL KICK",
        MatchBannerEvent.ThrowIn => "THROW-IN",
        MatchBannerEvent.Foul => "FOUL",
        MatchBannerEvent.StoppageTime => "STOPPAGE TIME",
        MatchBannerEvent.HalfTime => "END OF HALF",
        MatchBannerEvent.ExtraTime => "AWAITING EXTRA TIME",
        MatchBannerEvent.Penalties => "PENALTY SHOOTOUT",
        _ => string.Empty
    };
}
