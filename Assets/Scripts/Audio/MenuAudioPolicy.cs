using System.Collections.Generic;

/// <summary>Unscaled UI cue throttling, independent of gameplay random state.</summary>
public sealed class MenuAudioPolicy
{
    private readonly Dictionary<GameplayAudioPool, double> last = new Dictionary<GameplayAudioPool, double>();
    private double lastNavigation = double.NegativeInfinity;
    public static bool IsMenuCue(GameplayAudioPool cue) => cue == GameplayAudioPool.MenuHover
        || cue == GameplayAudioPool.MenuSelect || cue == GameplayAudioPool.MenuAdvance || cue == GameplayAudioPool.MenuBack;
    public static int Priority(GameplayAudioPool cue) => cue == GameplayAudioPool.MenuHover ? 0
        : cue == GameplayAudioPool.MenuSelect ? 1 : 2;

    public bool Accept(GameplayAudioPool cue, double now, bool busy, GameplayAudioPool current)
    {
        if (!IsMenuCue(cue) || double.IsNaN(now) || double.IsInfinity(now)) return false;
        if (busy && Priority(cue) < Priority(current)) return false;
        double cooldown = cue == GameplayAudioPool.MenuHover ? GameplayAudioConfig.MenuHoverCooldownSeconds : GameplayAudioConfig.MenuSelectCooldownSeconds;
        if (last.TryGetValue(cue, out double previous) && now - previous < cooldown) return false;
        if (Priority(cue) == 2 && now - lastNavigation < GameplayAudioConfig.MenuNavigationCooldownSeconds) return false;
        last[cue] = now;
        if (Priority(cue) == 2) lastNavigation = now;
        return true;
    }
}
