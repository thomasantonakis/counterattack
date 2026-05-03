using System;

public static class ExpectedStatsCalculator
{
    public readonly struct AerialContestant
    {
        public readonly PlayerToken token;
        public readonly int attribute;

        public AerialContestant(PlayerToken token, int attribute)
        {
            this.token = token;
            this.attribute = attribute;
        }
    }

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

    public static float CalculateAerialWinProbability(
        AerialContestant winner,
        AerialContestant[] contestants,
        bool winnerNaturalRollMustBeGreaterThanOne = false)
    {
        if (winner.token == null || contestants == null || contestants.Length == 0)
        {
            return 0f;
        }

        int winnerIndex = Array.FindIndex(contestants, contestant => contestant.token == winner.token);
        if (winnerIndex < 0)
        {
            return 0f;
        }

        return CalculateAerialBranchProbability(
            contestants,
            winnerIndex,
            winnerNaturalRollMustBeGreaterThanOne,
            requireGoalAgainstKeeper: false,
            keeperSaving: 0,
            keeperPenalty: 0);
    }

    public static float CalculateHeaderGoalProbability(
        AerialContestant shooter,
        AerialContestant[] contestants,
        bool shooterNaturalRollMustBeGreaterThanOne,
        int? keeperSaving = null,
        int keeperPenalty = 0)
    {
        if (shooter.token == null || contestants == null || contestants.Length == 0)
        {
            return 0f;
        }

        int shooterIndex = Array.FindIndex(contestants, contestant => contestant.token == shooter.token);
        if (shooterIndex < 0)
        {
            return 0f;
        }

        return CalculateAerialBranchProbability(
            contestants,
            shooterIndex,
            shooterNaturalRollMustBeGreaterThanOne,
            keeperSaving.HasValue,
            keeperSaving.GetValueOrDefault(),
            keeperPenalty);
    }

    private static float CalculateAerialBranchProbability(
        AerialContestant[] contestants,
        int selectedIndex,
        bool selectedNaturalRollMustBeGreaterThanOne,
        bool requireGoalAgainstKeeper,
        int keeperSaving,
        int keeperPenalty)
    {
        float probability = 0f;
        int[] effectiveRolls = new int[contestants.Length];
        int[] naturalRolls = new int[contestants.Length];

        void Enumerate(int index, float branchProbability)
        {
            if (index == contestants.Length)
            {
                int selectedNaturalRoll = naturalRolls[selectedIndex];
                if (selectedNaturalRollMustBeGreaterThanOne && selectedNaturalRoll <= 1)
                {
                    return;
                }

                int selectedScore = GetContestTotal(contestants[selectedIndex].attribute, effectiveRolls[selectedIndex]);
                for (int i = 0; i < contestants.Length; i++)
                {
                    if (i == selectedIndex)
                    {
                        continue;
                    }

                    int otherScore = GetContestTotal(contestants[i].attribute, effectiveRolls[i]);
                    if (otherScore >= selectedScore)
                    {
                        return;
                    }
                }

                probability += requireGoalAgainstKeeper
                    ? branchProbability * CalculateKeeperConcedeProbability(selectedScore, keeperSaving, keeperPenalty)
                    : branchProbability;
                return;
            }

            foreach (DiceOutcome outcome in DuelDiceOutcomes)
            {
                effectiveRolls[index] = outcome.effectiveRoll;
                naturalRolls[index] = outcome.effectiveRoll == 50 ? 6 : outcome.effectiveRoll;
                Enumerate(index + 1, branchProbability * outcome.probability);
            }
        }

        Enumerate(0, 1f);
        return probability;
    }

    private static int GetContestTotal(int attribute, int effectiveRoll)
    {
        return effectiveRoll == 50 ? 50 : attribute + effectiveRoll;
    }

    public static float CalculateKeeperConcedeProbability(int shotPower, int keeperSaving, int keeperPenalty)
    {
        float probability = 0f;
        foreach (DiceOutcome outcome in DuelDiceOutcomes)
        {
            int savingPower = outcome.effectiveRoll == 50 ? 50 : keeperSaving + keeperPenalty + outcome.effectiveRoll;
            if (savingPower < shotPower)
            {
                probability += outcome.probability;
            }
        }

        return probability;
    }

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
