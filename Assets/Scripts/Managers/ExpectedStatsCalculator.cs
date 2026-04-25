using System;

public static class ExpectedStatsCalculator
{
    public readonly struct GroundDuelExpectation
    {
        public readonly float attackerWinProbability;
        public readonly float defenderWinProbability;
        public readonly float tieProbability;
        public readonly float defenderFoulProbability;

        public float xDribbles => attackerWinProbability + (tieProbability / 6f);
        public float xTackles => defenderWinProbability + ((tieProbability * 5f) / 6f);

        public GroundDuelExpectation(
            float attackerWinProbability,
            float defenderWinProbability,
            float tieProbability,
            float defenderFoulProbability)
        {
            this.attackerWinProbability = attackerWinProbability;
            this.defenderWinProbability = defenderWinProbability;
            this.tieProbability = tieProbability;
            this.defenderFoulProbability = defenderFoulProbability;
        }
    }

    private readonly struct DiceOutcome
    {
        public readonly int effectiveRoll;
        public readonly float probability;

        public DiceOutcome(int effectiveRoll, float probability)
        {
            this.effectiveRoll = effectiveRoll;
            this.probability = probability;
        }
    }

    private static readonly DiceOutcome[] DuelDiceOutcomes =
    {
        new DiceOutcome(1, 1f / 6f),
        new DiceOutcome(2, 1f / 6f),
        new DiceOutcome(3, 1f / 6f),
        new DiceOutcome(4, 1f / 6f),
        new DiceOutcome(5, 1f / 6f),
        new DiceOutcome(6, 1f / 12f),
        new DiceOutcome(50, 1f / 12f),
    };

    public static float CalculateRecoveryProbability(PlayerToken defender)
    {
        return CalculateRecoveryProbability(defender, 6);
    }

    public static float CalculateRecoveryProbability(PlayerToken defender, int minimumNaturalRollForSuccess)
    {
        if (defender == null)
        {
            return 0f;
        }

        int tackling = defender.tackling;
        int successfulRolls = 0;

        for (int roll = 1; roll <= 6; roll++)
        {
            if (roll >= minimumNaturalRollForSuccess || roll + tackling >= 10)
            {
                successfulRolls++;
            }
        }

        return successfulRolls / 6f;
    }

    public static GroundDuelExpectation CalculateGroundDuelExpectation(
        PlayerToken attacker,
        PlayerToken defender,
        int defenderBonusMalus = 0)
    {
        if (attacker == null || defender == null)
        {
            return new GroundDuelExpectation(0f, 0f, 0f, 0f);
        }

        int attackerDribbling = attacker.dribbling;
        int defenderTackling = defender.tackling + defenderBonusMalus;

        float attackerWins = 0f;
        float defenderWins = 0f;
        float ties = 0f;
        float defenderFouls = 0f;

        foreach (DiceOutcome defenderOutcome in DuelDiceOutcomes)
        {
            foreach (DiceOutcome attackerOutcome in DuelDiceOutcomes)
            {
                float branchProbability = defenderOutcome.probability * attackerOutcome.probability;

                if (defenderOutcome.effectiveRoll <= 1)
                {
                    defenderFouls += branchProbability;
                    continue;
                }

                int defenderTotal = defenderOutcome.effectiveRoll == 50
                    ? 50
                    : defenderTackling + defenderOutcome.effectiveRoll;
                int attackerTotal = attackerOutcome.effectiveRoll == 50
                    ? 50
                    : attackerDribbling + attackerOutcome.effectiveRoll;

                if (defenderTotal > attackerTotal)
                {
                    defenderWins += branchProbability;
                }
                else if (defenderTotal < attackerTotal)
                {
                    attackerWins += branchProbability;
                }
                else
                {
                    ties += branchProbability;
                }
            }
        }

        return new GroundDuelExpectation(attackerWins, defenderWins, ties, defenderFouls);
    }
}
