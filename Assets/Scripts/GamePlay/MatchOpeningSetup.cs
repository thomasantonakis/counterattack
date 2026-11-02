using System;
using Newtonsoft.Json;

/// <summary>First-half choices, independent of Home/Away roster identity and later runtime snapshots.</summary>
[Serializable]
public sealed class MatchOpeningSetup
{
    public bool coinTossWinnerIsHome;
    public bool homeHasFirstHalfKickoff;
    public bool homeAttacksLeftToRight = true;

    public MatchManager.TeamAttackingDirection DirectionFor(bool home) => home == homeAttacksLeftToRight
        ? MatchManager.TeamAttackingDirection.LeftToRight : MatchManager.TeamAttackingDirection.RightToLeft;

    [JsonIgnore] public MatchManager.TeamInAttack KickoffTeam => homeHasFirstHalfKickoff
        ? MatchManager.TeamInAttack.Home : MatchManager.TeamInAttack.Away;
}

public enum CoinTossChoice { Kickoff, LeftToRight, RightToLeft }

/// <summary>A direction always describes the choosing team's attack, never its Home/Away identity.</summary>
public sealed class CoinTossDecision
{
    public bool WinnerIsHome { get; }
    public bool ChoosingHome { get; private set; }
    public bool CanChooseKickoff { get; private set; } = true;
    public MatchOpeningSetup Result { get; private set; }

    public CoinTossDecision(bool winnerIsHome) => WinnerIsHome = ChoosingHome = winnerIsHome;

    public bool Choose(CoinTossChoice choice)
    {
        if (Result != null) return false;
        if (choice == CoinTossChoice.Kickoff)
        {
            if (!CanChooseKickoff) return false;
            CanChooseKickoff = false;
            ChoosingHome = !WinnerIsHome;
            return true;
        }
        if (choice != CoinTossChoice.LeftToRight && choice != CoinTossChoice.RightToLeft) return false;
        Result = new MatchOpeningSetup
        {
            coinTossWinnerIsHome = WinnerIsHome,
            homeHasFirstHalfKickoff = CanChooseKickoff ? !WinnerIsHome : WinnerIsHome,
            homeAttacksLeftToRight = ChoosingHome == (choice == CoinTossChoice.LeftToRight)
        };
        return true;
    }

    public CoinTossChoice CpuChoice => CanChooseKickoff ? CoinTossChoice.Kickoff
        : ChoosingHome ? CoinTossChoice.LeftToRight : CoinTossChoice.RightToLeft;

    public static bool IsCpuManager(GameSettings settings, bool home)
    {
        if (!string.Equals(settings?.gameMode, "Single Player", StringComparison.OrdinalIgnoreCase)) return false;
        string control = home ? settings.homeTeamControl : settings.awayTeamControl;
        if (string.Equals(control, "Human", StringComparison.OrdinalIgnoreCase)) return false;
        if (!string.IsNullOrWhiteSpace(control) && control.StartsWith("CPU", StringComparison.OrdinalIgnoreCase)) return true;
        string persona = home ? settings.homeRoomPersona : settings.awayRoomPersona;
        return AIManager.IsCodexTacticianPersonaName(persona)
            || string.Equals(persona, "Random", StringComparison.OrdinalIgnoreCase)
            || string.Equals(persona, "1step", StringComparison.OrdinalIgnoreCase)
            || string.Equals(persona, "one step", StringComparison.OrdinalIgnoreCase)
            || (!string.IsNullOrWhiteSpace(persona) && (persona.StartsWith("1 step", StringComparison.OrdinalIgnoreCase)
                || persona.StartsWith("CPU-", StringComparison.OrdinalIgnoreCase)))
            || (!home && string.IsNullOrWhiteSpace(control) && string.IsNullOrWhiteSpace(persona));
    }
}
