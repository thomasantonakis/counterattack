using UnityEngine;

/// <summary>Shared pre-roll floor. Attributes supplied here already include injuries.</summary>
public static class GameplayScore
{
    public static int EffectiveAttribute(int currentAttribute, int modifier = 0)
        => Mathf.Max(0, currentAttribute + modifier);

    public static int Total(int currentAttribute, int roll, int modifier = 0, bool jackpot = false)
        => jackpot || roll == 50 ? 50 : EffectiveAttribute(currentAttribute, modifier) + roll;
}
