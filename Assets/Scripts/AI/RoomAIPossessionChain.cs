using System;
using System.Collections.Generic;

public enum RoomAIPossessionOutcomeKind
{
    Continue,
    Goal,
    AttackRetainsPossession,
    DefenseWinsPossession,
    AttackingRestart,
    DefendingRestart,
    Unresolved
}

/// <summary>
/// One probabilistic edge in a possession-chain decision tree. A branch is either
/// terminal (for example Goal or DefenseWinsPossession) or points to the next
/// decision/chance node. This model contains no Unity objects so future search can
/// evaluate it away from the live scene.
/// </summary>
public sealed class RoomAIPossessionBranch
{
    public string label;
    public float probability;
    public RoomAIPossessionOutcomeKind outcome;
    public RoomAIPossessionChain continuation;
}

/// <summary>
/// A bounded, pure probability tree for the rest of one possession. Missing branch
/// probability is intentionally classified as unresolved instead of silently being
/// treated as a turnover or zero xG.
/// </summary>
public sealed class RoomAIPossessionChain
{
    public string label;
    public readonly List<RoomAIPossessionBranch> branches = new();

    public RoomAIPossessionChain(string label = "")
    {
        this.label = label ?? string.Empty;
    }

    public RoomAIPossessionChain AddTerminal(
        string branchLabel,
        float probability,
        RoomAIPossessionOutcomeKind outcome)
    {
        if (outcome == RoomAIPossessionOutcomeKind.Continue)
        {
            throw new ArgumentException("A terminal possession branch cannot use Continue.", nameof(outcome));
        }

        branches.Add(new RoomAIPossessionBranch
        {
            label = branchLabel ?? string.Empty,
            probability = ClampProbability(probability),
            outcome = outcome
        });
        return this;
    }

    public RoomAIPossessionChain AddContinuation(
        string branchLabel,
        float probability,
        RoomAIPossessionChain continuation)
    {
        branches.Add(new RoomAIPossessionBranch
        {
            label = branchLabel ?? string.Empty,
            probability = ClampProbability(probability),
            outcome = continuation != null
                ? RoomAIPossessionOutcomeKind.Continue
                : RoomAIPossessionOutcomeKind.Unresolved,
            continuation = continuation
        });
        return this;
    }

    public RoomAIPossessionChainSummary Evaluate(int maximumDepth = 12)
    {
        RoomAIPossessionChainSummary summary = new();
        EvaluateInto(summary, 1f, Math.Max(0, maximumDepth), new HashSet<RoomAIPossessionChain>());
        summary.ClampRoundingError();
        return summary;
    }

    private void EvaluateInto(
        RoomAIPossessionChainSummary summary,
        float reachProbability,
        int remainingDepth,
        HashSet<RoomAIPossessionChain> activePath)
    {
        if (summary == null || reachProbability <= 0f)
        {
            return;
        }

        if (remainingDepth <= 0 || !activePath.Add(this))
        {
            summary.unresolvedProbability += reachProbability;
            return;
        }

        float declaredProbability = 0f;
        foreach (RoomAIPossessionBranch branch in branches)
        {
            if (branch == null || branch.probability <= 0f || declaredProbability >= 1f)
            {
                continue;
            }

            float availableProbability = 1f - declaredProbability;
            float branchProbability = Math.Min(branch.probability, availableProbability);
            declaredProbability += branchProbability;
            float weightedProbability = reachProbability * branchProbability;
            summary.evaluatedBranchCount++;

            if (branch.outcome == RoomAIPossessionOutcomeKind.Continue
                && branch.continuation != null)
            {
                branch.continuation.EvaluateInto(
                    summary,
                    weightedProbability,
                    remainingDepth - 1,
                    activePath);
                continue;
            }

            summary.AddTerminal(branch.outcome, weightedProbability);
        }

        if (declaredProbability < 1f)
        {
            summary.unresolvedProbability += reachProbability * (1f - declaredProbability);
        }

        activePath.Remove(this);
    }

    private static float ClampProbability(float probability)
    {
        if (float.IsNaN(probability) || probability <= 0f)
        {
            return 0f;
        }

        return Math.Min(1f, probability);
    }
}

[Serializable]
public sealed class RoomAIPossessionChainSummary
{
    public float goalProbability;
    public float attackRetainsPossessionProbability;
    public float defenseWinsPossessionProbability;
    public float attackingRestartProbability;
    public float defendingRestartProbability;
    public float unresolvedProbability;
    public int evaluatedBranchCount;

    public float resolvedProbability => Math.Max(0f, 1f - unresolvedProbability);
    public bool isComplete => unresolvedProbability <= 0.0001f;

    internal void AddTerminal(RoomAIPossessionOutcomeKind outcome, float probability)
    {
        switch (outcome)
        {
            case RoomAIPossessionOutcomeKind.Goal:
                goalProbability += probability;
                break;
            case RoomAIPossessionOutcomeKind.AttackRetainsPossession:
                attackRetainsPossessionProbability += probability;
                break;
            case RoomAIPossessionOutcomeKind.DefenseWinsPossession:
                defenseWinsPossessionProbability += probability;
                break;
            case RoomAIPossessionOutcomeKind.AttackingRestart:
                attackingRestartProbability += probability;
                break;
            case RoomAIPossessionOutcomeKind.DefendingRestart:
                defendingRestartProbability += probability;
                break;
            case RoomAIPossessionOutcomeKind.Continue:
            case RoomAIPossessionOutcomeKind.Unresolved:
            default:
                unresolvedProbability += probability;
                break;
        }
    }

    internal void ClampRoundingError()
    {
        goalProbability = Clamp(goalProbability);
        attackRetainsPossessionProbability = Clamp(attackRetainsPossessionProbability);
        defenseWinsPossessionProbability = Clamp(defenseWinsPossessionProbability);
        attackingRestartProbability = Clamp(attackingRestartProbability);
        defendingRestartProbability = Clamp(defendingRestartProbability);
        unresolvedProbability = Clamp(unresolvedProbability);
    }

    private static float Clamp(float value)
    {
        return Math.Max(0f, Math.Min(1f, value));
    }
}
