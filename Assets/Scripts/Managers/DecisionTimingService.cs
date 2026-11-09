using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public sealed class DecisionTimingState
{
    public string id;
    public string key;
    public string owner;
    public DecisionKind kind;
    public float remaining;
    public float budget;
    public bool expired;
    public bool suspended;
    public bool inactive;
}

[Serializable]
public sealed class DecisionTimingSnapshot
{
    public List<DecisionTimingState> decisions = new();
    public string rollKey;
    public string rollId;
    public float rollRemaining;
}

/// <summary>Pure real-time accounting. The host supplies elapsed time and validated domain commands.</summary>
public sealed class DecisionTimingService
{
    private readonly Dictionary<string, DecisionTimingState> decisions = new();
    private readonly Dictionary<string, DecisionTimingState> unresolvedChoices = new();
    private readonly HashSet<string> suspended = new();
    private readonly HashSet<string> seen = new();
    private readonly HashSet<string> concurrent = new();
    private DecisionTimingSnapshot restoring;
    private Func<bool> rollReady;
    private Action rollCommand;
    private bool rollSubmitted;
    private string rollKey;
    public string RollId { get; private set; }
    public float RollRemaining { get; private set; }
    public bool Paused { get; set; }
    public bool BlockingResult { get; set; }
    public bool AutomaticResult { get; set; }
    public bool ContinuationInProgress { get; set; }
    public bool GameClockStopped { get; set; }
    public bool HasCommandAuthority { get; set; } = true;
    public DecisionTimingState Current { get; private set; }
    public string CurrentLabel { get; set; }
    public IEnumerable<DecisionTimingState> ConcurrentDecisions => concurrent.Where(decisions.ContainsKey).Select(k => decisions[k]);
    public DecisionTimingState ExpiredDecision => decisions.Values.FirstOrDefault(d => d.expired);
    public bool HasRollWait => rollKey != null;
    public bool HasPendingRoll => rollKey != null && !rollSubmitted;
    public bool HoldsMatchClock => BlockingResult || (!HasPendingRoll && !AutomaticResult
        && !ContinuationInProgress && decisions.Values.Any(d => d.expired));
    public event Action<string, string, string> Recorded;

    public DecisionTimingState BeginDecision(string key, DecisionKind kind, string owner, int difficulty)
    {
        if (string.IsNullOrEmpty(key)) throw new ArgumentException("A stable decision key is required.", nameof(key));
        if (!decisions.TryGetValue(key, out var state))
        {
            var saved = restoring?.decisions?.Find(d => d.key == key && d.kind == kind && d.owner == owner);
            if (saved == null) unresolvedChoices.TryGetValue(key, out saved);
            state = saved != null ? Copy(saved) : new DecisionTimingState
            { id = Guid.NewGuid().ToString("N"), key = key, kind = kind, owner = owner,
                remaining = DecisionTimingConfig.GetBudget(kind, difficulty), budget = DecisionTimingConfig.GetBudget(kind, difficulty) };
            if (state.budget <= 0) state.budget = DecisionTimingConfig.GetBudget(kind, difficulty);
            state.inactive = false;
            decisions.Add(key, state);
            if (state.suspended) suspended.Add(key);
        }
        else if (state.kind != kind || state.owner != owner)
            throw new InvalidOperationException("A logical decision cannot change kind or owner.");
        return state;
    }

    public void EndDecision(string key)
    {
        if (decisions.TryGetValue(key, out var state) && state.expired)
            Recorded?.Invoke("hold_released", state.owner, state.id);
        decisions.Remove(key);
        suspended.Remove(key);
        concurrent.Remove(key);
        if (Current?.key == key) Current = null;
    }
    public void Suspend(string key) { suspended.Add(key); if (decisions.TryGetValue(key, out var state)) state.suspended = true; }
    public void Resume(string key) { suspended.Remove(key); if (decisions.TryGetValue(key, out var state)) state.suspended = false; }
    public void ClearUnresolvedChoices() => unresolvedChoices.Clear();
    private static bool IsReopenable(DecisionKind kind) => kind == DecisionKind.SelectNextAction
        || kind == DecisionKind.StandardPassTarget || kind == DecisionKind.FirstTimePassTarget
        || kind == DecisionKind.HighPassTarget || kind == DecisionKind.LongBallTarget
        || kind == DecisionKind.ShotGoalLineTarget || kind == DecisionKind.SnapshotGoalLineTarget
        || kind == DecisionKind.GoalkeeperDistributionTarget || kind == DecisionKind.ThrowInTarget
        || kind == DecisionKind.GoalKickTarget || kind == DecisionKind.AttackingMovement;
    public void Select(string key) => Current = key != null && decisions.TryGetValue(key, out var state) ? state : null;

    // Most children suspend the parent. Explicit concurrent registrations charge both.
    public void BeginRegistration() { seen.Clear(); concurrent.Clear(); Current = null; }
    public void Register(string key, DecisionKind kind, string owner, int difficulty, bool ready, bool concurrentWithCurrent = false)
    {
        if (GameClockStopped && !decisions.ContainsKey(key)) return;
        seen.Add(key);
        var state = BeginDecision(key, kind, owner, difficulty);
        if (ready && concurrentWithCurrent && !GameClockStopped) concurrent.Add(key);
        Current = ready && !GameClockStopped ? state : null;
    }
    public void EndRegistration()
    {
        foreach (var key in decisions.Keys.Where(k => !seen.Contains(k)).ToArray())
        {
            if (IsReopenable(decisions[key].kind))
            {
                var copy = Copy(decisions[key]); copy.inactive = true; unresolvedChoices[key] = copy;
            }
            EndDecision(key);
        }
        restoring = null;
    }

    public void BeginRoll(string key, string owner, Func<bool> ready, Action command)
    {
        if (rollKey != key)
        {
            rollKey = key;
            RollId = restoring?.rollKey == key ? restoring.rollId : Guid.NewGuid().ToString("N");
            RollRemaining = restoring?.rollKey == key ? restoring.rollRemaining : DecisionTimingConfig.AutomaticRollSeconds;
            rollSubmitted = false;
        }
        rollReady = ready;
        rollCommand = command;
        RollOwner = owner;
    }
    public string RollOwner { get; private set; }
    public void EndRoll() { rollKey = null; RollId = null; rollReady = null; rollCommand = null; rollSubmitted = false; }

    public bool SubmitRoll(string id, string owner, Action manualCommand = null, bool automatic = false)
    {
        if (!HasCommandAuthority || Paused || BlockingResult || !HasPendingRoll || id != RollId
            || owner != RollOwner || rollReady?.Invoke() != true) return false;
        rollSubmitted = true; // Claim before invoking: continuation may synchronously offer another roll.
        if (automatic) Recorded?.Invoke("automatic_roll", owner, id);
        (manualCommand ?? rollCommand)?.Invoke();
        return true;
    }

    public void Advance(float seconds)
    {
        if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
        if (Paused || BlockingResult) return;
        // Roll waits and ordinary result displays consume the active owner's
        // allowance alongside live match time. A roll never invents a budget.
        var charging = ConcurrentDecisions.Concat(Current != null ? new[] { Current } : Array.Empty<DecisionTimingState>()).Distinct().ToArray();
        foreach (var state in charging)
        {
            bool ownsAllowance = !HasPendingRoll || RollOwner == state.owner || RollOwner == "Both teams";
            if (GameClockStopped || suspended.Contains(state.key) || state.expired || !ownsAllowance) continue;
            state.remaining = Math.Max(0, state.remaining - seconds);
            if (state.remaining <= 0)
            {
                state.expired = true;
                Recorded?.Invoke("expired", state.owner, state.id);
            }
        }
        if (HasPendingRoll)
        {
            if (rollReady?.Invoke() != true) return;
            RollRemaining = Math.Max(0, RollRemaining - seconds);
            if (RollRemaining <= 0) SubmitRoll(RollId, RollOwner, automatic: true);
        }
    }
    public DecisionTimingSnapshot Capture() => new()
    {
        decisions = decisions.Values.Concat(unresolvedChoices.Values.Where(s => !decisions.ContainsKey(s.key))).Select(Copy).ToList(), rollKey = rollKey,
        rollId = RollId, rollRemaining = RollRemaining
    };
    public void Restore(DecisionTimingSnapshot snapshot)
    {
        Clear(); restoring = snapshot;
        if (snapshot?.decisions != null) foreach (var state in snapshot.decisions)
            if (state.inactive && IsReopenable(state.kind)) unresolvedChoices[state.key] = Copy(state);
    }
    public void Clear() { decisions.Clear(); unresolvedChoices.Clear(); suspended.Clear(); concurrent.Clear(); Current = null; CurrentLabel = null; restoring = null; EndRoll(); }
    private static DecisionTimingState Copy(DecisionTimingState s) => new()
    { id = s.id, key = s.key, owner = s.owner, kind = s.kind, remaining = s.remaining, budget = s.budget, expired = s.expired, suspended = s.suspended, inactive = s.inactive };
}
