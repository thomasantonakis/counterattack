using System;
using System.Collections;
using UnityEngine;

/// <summary>Presentation adapter only. Callers retain dice overrides, rule evaluation and continuation.</summary>
public static class SingleRollResolutionPresentation
{
    public static string LooseRecoveryAttribute(PlayerToken token, bool useSaving, int modifier)
        => !useSaving ? $"Tackling {token.tackling}"
            : modifier == 0 ? $"Saving {token.saving}"
            : $"Saving {token.saving} {modifier:+0;-0} = {token.saving + modifier} (Distance Penalty)";

    public static string HeaderSaveGuidance(int saving, int modifier, int headerPower)
    {
        if (headerPower == 50) return "JACKPOT: tied save / loose ball · Numeric rolls: goal";
        int tie = headerPower - saving - modifier;
        return SuccessRange(tie + 1) + " to save · "
            + (tie >= 1 && tie <= 6 ? $"Roll {tie}: loose ball" : "No numeric tie")
            + " · JACKPOT: save";
    }

    // Handling uses the manager's strict numeric comparison, even when its dice service
    // supplies a jackpot flag. There is no automatic jackpot success for this check.
    public static string HandlingGuidance(int handling)
        => handling <= 1 ? "No numeric rolls hold the ball"
            : handling > 6 ? "Rolls 1–6: hold the ball"
            : $"Rolls 1–{handling - 1}: hold · Rolls {handling}–6: spill";

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
        string situation, float probability, int threshold, bool jackpotAlwaysSucceeds = false,
        bool oppositeFinalThirds = false)
    {
        // An originating result can still be closing when the next accuracy check starts.
        // The manager retries while waiting; never replace or overlap that result.
        if (token == null || HeaderChallengePanelController.IsVisible
            || StoppageActionsPanelController.IsVisible || CoinTossPanelController.IsRoomVisible) return;
        DuelResolutionPanelController.Instance?.BeginSingle(owner, waiting, situation, token,
            $"High Pass {token.highPass} · Target {threshold}"
                + (oppositeFinalThirds ? " · Opposite final thirds" : ""), $"Accuracy {probability:P1}", true,
            AccuracyGuidance(token.highPass, threshold, jackpotAlwaysSucceeds));
    }

    public static void BallControl(MonoBehaviour owner, Func<bool> waiting, PlayerToken token,
        int modifier, int target, float probability)
    {
        if (token == null) return;
        DuelResolutionPanelController.Instance?.BeginSingle(owner, waiting, "Ball Control", token,
            BallControlAttribute(token.dribbling, modifier, target), $"Control {probability:P1}", true,
            AccuracyGuidance(token.dribbling + modifier, target, true));
    }

    public static string BallControlAttribute(int dribbling, int modifier, int target)
        => $"Dribbling {dribbling}{(modifier == 0 ? "" : $" · Control modifier {modifier:+0;-0}")} · Target {target}";

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
        string result, bool direction, bool continueWithMovement = false)
    {
        var panel = DuelResolutionPanelController.Instance;
        return panel != null ? panel.ResolveLooseBallRoll(owner, roll, result, direction, continueWithMovement) : Empty();
    }

    public static IEnumerator Resolve(MonoBehaviour owner, GameplayDiceRollResult roll, string result,
        bool continueWithFoulChecks = false, bool continueWithScatter = false, MonoBehaviour scatterOwner = null,
        int? resolvedTotal = null, bool continueWithNextRoll = false, bool continueWhileResultVisible = false)
    {
        var panel = DuelResolutionPanelController.Instance;
        return panel != null ? panel.ResolveSingle(owner, roll, result, continueWithFoulChecks, continueWithScatter, scatterOwner, resolvedTotal, continueWithNextRoll, continueWhileResultVisible) : Empty();
    }

    private static IEnumerator Empty() { yield break; }
}
