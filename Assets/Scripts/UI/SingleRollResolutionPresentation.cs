using System;
using System.Collections;
using UnityEngine;

/// <summary>Presentation adapter only. Callers retain dice overrides, rule evaluation and continuation.</summary>
public static class SingleRollResolutionPresentation
{
    public static void Recovery(MonoBehaviour owner, Func<bool> waiting, PlayerToken token,
        string situation, bool jackpot = true, int minimumNaturalRoll = 6, int foulThreshold = 0, bool foulIsPenalty = false)
    {
        if (token == null || token.IsGoalKeeper) return;
        DuelResolutionPanelController.Instance?.BeginSingle(owner, waiting, situation, token,
            $"Tackling {token.tackling}",
            $"xRecovery {ExpectedStatsCalculator.CalculateRecoveryProbability(token, minimumNaturalRoll):0.00}", jackpot,
            RecoveryGuidance(token.tackling, minimumNaturalRoll, foulThreshold, foulIsPenalty));
    }

    public static void Accuracy(MonoBehaviour owner, Func<bool> waiting, PlayerToken token,
        string situation, float probability, int threshold, bool jackpotAlwaysSucceeds = false)
    {
        if (token == null) return;
        DuelResolutionPanelController.Instance?.BeginSingle(owner, waiting, situation, token,
            $"High Pass {token.highPass} · Target {threshold}", $"Accuracy {probability:P1}", true,
            AccuracyGuidance(token.highPass, threshold, jackpotAlwaysSucceeds));
    }

    // Explain the same thresholds supplied by the owning manager; no outcomes are resolved here.
    private static string SuccessRange(int minimum)
        => minimum > 6 ? "No successful numeric rolls"
            : minimum == 6 ? "Success: roll 6" : $"Success: roll {Mathf.Max(1, minimum)}–6";

    public static string RecoveryGuidance(int tackling, int minimumNaturalRoll, int foulThreshold, bool foulIsPenalty = false)
    {
        int minimum = Mathf.Max(foulThreshold + 1, Mathf.Min(minimumNaturalRoll, 10 - tackling));
        string success = SuccessRange(minimum);
        string foulResult = foulIsPenalty ? "penalty" : "foul";
        return foulThreshold <= 0 ? success : success + (foulThreshold == 1
            ? $" · Roll 1: {foulResult}" : $" · Rolls 1–{foulThreshold}: {foulResult}");
    }

    public static string AccuracyGuidance(int highPass, int threshold, bool jackpotAlwaysSucceeds)
    {
        int minimum = threshold - highPass;
        if (minimum > 6 && jackpotAlwaysSucceeds) return "Success: jackpot only";
        return SuccessRange(minimum);
    }

    public static void LooseBall(MonoBehaviour owner, Func<bool> waiting, PlayerToken token, bool direction)
        => DuelResolutionPanelController.Instance?.BeginLooseBall(owner, waiting, token, direction);

    public static void Inaccuracy(MonoBehaviour owner, Func<bool> waiting, PlayerToken token, bool direction)
        => DuelResolutionPanelController.Instance?.BeginLooseBall(owner, waiting, token, direction, true);

    public static IEnumerator ResolveLooseBall(MonoBehaviour owner, GameplayDiceRollResult roll,
        string result, bool direction)
    {
        var panel = DuelResolutionPanelController.Instance;
        return panel != null ? panel.ResolveLooseBallRoll(owner, roll, result, direction) : Empty();
    }

    public static IEnumerator Resolve(MonoBehaviour owner, GameplayDiceRollResult roll, string result, bool continueWithFoulChecks = false, bool continueWithScatter = false)
    {
        var panel = DuelResolutionPanelController.Instance;
        return panel != null ? panel.ResolveSingle(owner, roll, result, continueWithFoulChecks, continueWithScatter) : Empty();
    }

    private static IEnumerator Empty() { yield break; }
}
