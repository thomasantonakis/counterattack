using UnityEngine;

public enum GameplayAudioCategory { Stadium, Game, Music }
public enum GameplayAudioPool
{
    Chatter, Chant, Cheer, Groan, Whistle, Dice, Invalid, Pause, Resume,
    MenuHover, StatsShow, StatsHide, PauseMusic,
    DuelWon, DuelLost, DefensiveShotRelief, OpponentError, FoulReaction,
    KickOff, HalfTime, FullTime, HomeSubstitution, AwaySubstitution,
    DoubleAwaySubstitution, InjurySubstitution, SnapshotAvailable, SetPiecePreparation,
    MenuSelect, MenuAdvance, MenuBack, DiceWiggle,
    None = -1
}

/// <summary>All mix/timing defaults. Clip choices are authored in GameplayAudioLibrary.</summary>
public static class GameplayAudioConfig
{
    public const float HomeShare = 0.7f;
    public const float AwayShare = 0.3f;
    public const float InternationalStandShare = 0.5f;
    public const float ChatterMinIntensity = 0.4f;
    public const float ChatterMaxIntensity = 0.5f;
    public const float ChatterIntensity = 0.45f;
    public const float ReactionIntensity = 0.5f;
    public const float SnapshotIntensity = 0.8f;
    public const float SnapshotClipGainDb = 6f;
    public const float SnapshotRepeatCooldownSeconds = 15f;
    public const float GoalIntensity = 1f;
    public const float ChantIntensity = 0.45f;
    public const float SetPieceIntensity = 0.5f;
    public const float ChatterCrossfadeSeconds = 2f;
    public const float ChatterFadeSeconds = 0.35f;
    public const float ChantFadeSeconds = 0.35f;
    // Uniform output trim preserves stand/layer ratios and leaves room for fade overlaps.
    public const float CrowdOutputGain = 0.75f;
    public const float ChantMinIntervalSeconds = 25f;
    public const float ChantMaxIntervalSeconds = 45f;
    public const int ChantPriority = 0;
    public const int IncidentPriority = 10;
    public const int SnapshotPriority = 5;
    public const int FoulPriority = 30;
    public const int ShotOutcomePriority = 40;
    public const int CardPriority = 50;
    public const int GoalPriority = 100;
    public const int RememberedEventCount = 512;
    public const double MenuHoverCooldownSeconds = 0.18;
    public const double MenuSelectCooldownSeconds = 0.08;
    public const double MenuNavigationCooldownSeconds = 0.1;

    // Player-facing settings are On/Off only. Adjust category mix levels here.
    public static float CategoryVolume(GameplayAudioCategory category) => category switch
    {
        GameplayAudioCategory.Stadium => 1f,
        GameplayAudioCategory.Game => 1f,
        GameplayAudioCategory.Music => 1f,
        _ => 0f
    };

    public readonly struct StandReaction
    {
        public readonly GameplayAudioPool Pool;
        public readonly float Intensity;
        public StandReaction(GameplayAudioPool pool, float intensity)
        {
            Pool = pool;
            Intensity = intensity;
        }
    }

    public readonly struct CrowdReaction
    {
        public readonly StandReaction Home;
        public readonly StandReaction Away;
        public readonly int Priority;
        public readonly bool FillRemaining;
        public CrowdReaction(StandReaction home, StandReaction away, int priority, bool fillRemaining = false)
        {
            Home = home;
            Away = away;
            Priority = priority;
            FillRemaining = fillRemaining;
        }
    }

    // Remaining incident rows deliberately await agreement and suitable clip pools.
    public static CrowdReaction Goal(bool homeScored) => new CrowdReaction(
        new StandReaction(homeScored ? GameplayAudioPool.Cheer : GameplayAudioPool.Groan,
            GoalIntensity),
        new StandReaction(homeScored ? GameplayAudioPool.Groan : GameplayAudioPool.Cheer,
            GoalIntensity), GoalPriority, fillRemaining: true);

    public static CrowdReaction PenaltyShootoutOutcome(bool shooterHome, bool scored)
    {
        if (scored) return Goal(shooterHome);
        GameplayAudioPool home = shooterHome ? GameplayAudioPool.Groan : GameplayAudioPool.Cheer;
        GameplayAudioPool away = shooterHome ? GameplayAudioPool.Cheer : GameplayAudioPool.Groan;
        return new CrowdReaction(new StandReaction(home, GoalIntensity),
            new StandReaction(away, GoalIntensity), GoalPriority, fillRemaining: true);
    }

    public static CrowdReaction Incident(GameplayAudioEvents.IncidentKind kind, bool homeTeam)
    {
        GameplayAudioPool team = GameplayAudioPool.None, opponent = GameplayAudioPool.None;
        switch (kind)
        {
            case GameplayAudioEvents.IncidentKind.DuelWon:
                team = GameplayAudioPool.DuelWon; opponent = GameplayAudioPool.DuelLost; break;
            case GameplayAudioEvents.IncidentKind.DuelLost:
                team = GameplayAudioPool.DuelLost; opponent = GameplayAudioPool.DuelWon; break;
            case GameplayAudioEvents.IncidentKind.OffTarget:
                team = GameplayAudioPool.Groan; opponent = GameplayAudioPool.OpponentError; break;
            case GameplayAudioEvents.IncidentKind.BlockedShot:
                team = GameplayAudioPool.Groan; opponent = GameplayAudioPool.DefensiveShotRelief; break;
            case GameplayAudioEvents.IncidentKind.Save:
                team = GameplayAudioPool.DefensiveShotRelief; opponent = GameplayAudioPool.Groan; break;
            case GameplayAudioEvents.IncidentKind.Foul:
                team = GameplayAudioPool.FoulReaction; break;
            case GameplayAudioEvents.IncidentKind.OpponentError:
            case GameplayAudioEvents.IncidentKind.CardShown:
                team = GameplayAudioPool.OpponentError; break;
            case GameplayAudioEvents.IncidentKind.SnapshotAvailable:
                team = GameplayAudioPool.SnapshotAvailable; break;
            case GameplayAudioEvents.IncidentKind.InjurySubstitution:
                team = GameplayAudioPool.InjurySubstitution; break;
        }
        int priority = kind == GameplayAudioEvents.IncidentKind.SnapshotAvailable ? SnapshotPriority
            : kind == GameplayAudioEvents.IncidentKind.CardShown ? CardPriority
            : kind == GameplayAudioEvents.IncidentKind.Foul ? FoulPriority
            : kind == GameplayAudioEvents.IncidentKind.OffTarget || kind == GameplayAudioEvents.IncidentKind.BlockedShot
                || kind == GameplayAudioEvents.IncidentKind.Save ? ShotOutcomePriority : IncidentPriority;
        float intensity = kind == GameplayAudioEvents.IncidentKind.SnapshotAvailable ? SnapshotIntensity : ReactionIntensity;
        return new CrowdReaction(new StandReaction(homeTeam ? team : opponent, intensity),
            new StandReaction(homeTeam ? opponent : team, intensity), priority);
    }

    public static float StandShare(bool home, string matchType = null) =>
        string.Equals(matchType?.Trim(), "International", System.StringComparison.OrdinalIgnoreCase)
            ? InternationalStandShare : home ? HomeShare : AwayShare;

    public static float StandGain(bool home, float intensity, float categoryGain, string matchType = null) =>
        StandShare(home, matchType) * Mathf.Clamp01(intensity) * Mathf.Clamp01(categoryGain);

    public static float PoolGain(GameplayAudioPool pool) => pool switch
    {
        GameplayAudioPool.SnapshotAvailable => Mathf.Pow(10f, SnapshotClipGainDb / 20f),
        GameplayAudioPool.MenuHover => 0.18f,
        GameplayAudioPool.MenuSelect => 0.35f,
        GameplayAudioPool.MenuAdvance => 0.45f,
        GameplayAudioPool.MenuBack => 0.4f,
        GameplayAudioPool.DiceWiggle => 0.9f,
        GameplayAudioPool.Invalid => 0.5f,
        GameplayAudioPool.Dice => 0.95f,
        GameplayAudioPool.StatsShow or GameplayAudioPool.StatsHide => 0.45f,
        GameplayAudioPool.Pause or GameplayAudioPool.Resume => 0.5f,
        GameplayAudioPool.PauseMusic => 0.5f,
        _ => 1f
    };

    public static float Cooldown(GameplayAudioPool pool) => pool switch
    {
        GameplayAudioPool.MenuHover => 0.12f,
        GameplayAudioPool.Invalid => 0.2f,
        GameplayAudioPool.Dice => 0.1f,
        GameplayAudioPool.DiceWiggle => 0f,
        _ => 0.08f
    };
}
