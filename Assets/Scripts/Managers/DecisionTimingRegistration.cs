using System;
using UnityEngine;

/// <summary>Typed registrations owned by gameplay managers; no instruction text or AI dependency.</summary>
public sealed class DecisionTimingRegistration
{
    private readonly MatchManager match;
    private readonly DecisionTimingService timing;
    private string scope;
    private bool? owner;
    private string pendingRollKey, pendingRollOwner;
    private Func<bool> pendingRollReady;
    private Action pendingRollCommand;
    public bool HasRoll { get; private set; }
    public UnityEngine.Object RollManager { get; private set; }
    public DecisionTimingRegistration(MatchManager match, DecisionTimingService timing) { this.match = match; this.timing = timing; }
    public bool AttackHome => match.teamInAttack == MatchManager.TeamInAttack.Home;
    public void Scope(string name, bool? home) { scope = name; owner = home; }
    public void Decision(DecisionKind kind, bool waiting, bool ready = true, string phase = "", bool? home = null)
    {
        if (!waiting) return;
        string team = (home ?? owner) is bool h ? h ? "Home" : "Away" : "Both teams";
        timing.Register($"{match.totalCompletedActions}/{scope}/{phase}/{kind}/{team}", kind, team, match.difficulty_level, ready);
    }
    public void Roll(UnityEngine.Object manager, string stage, bool waiting, Func<bool> ready, Action<KeyPressData> command)
    {
        if (!waiting) return;
        HasRoll = true;
        RollManager = manager;
        string team = owner is bool h ? h ? "Home" : "Away" : "Both teams";
        pendingRollKey = $"{scope}/{stage}/{team}/{match.TimingRollSequence}";
        pendingRollOwner = team;
        pendingRollReady = () => manager != null && !match.IsPauseMenuOpen && !MatchManager.TimingResultBlocked && ready();
        pendingRollCommand = () => match.ExecuteTimingRoll(() => command(new KeyPressData(KeyCode.R, false, false, false)));

    }
    public void ApplyRoll()
    {
        // Resolve priority before registering. A suspended outer roll must not reset
        // the active child roll's identity and deadline on every frame.
        if (HasRoll) timing.BeginRoll(pendingRollKey, pendingRollOwner, pendingRollReady, pendingRollCommand);
        else timing.EndRoll();
    }
}
