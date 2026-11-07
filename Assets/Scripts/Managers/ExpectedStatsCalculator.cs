using System;
using System.Collections.Generic;

public static class ExpectedStatsCalculator
{
    public sealed class ShotResolutionInteraction
    {
        public PlayerToken token;
        public HexCell tokenHex;
        public HexCell contactHex;
        public bool goalkeeper;
        public int requiredRoll;
        public int? tackling;
        public int saveModifier;
    }

    public sealed class ShotReboundEvent
    {
        public PlayerToken token;
        public HexCell origin;
        public LooseBallSourceType source;
        public float probability;
    }

    public sealed class ShotResolutionProbabilities
    {
        public float goal;
        public float offTarget;
        public float held;
        public readonly List<ShotReboundEvent> rebounds = new();
    }

    /// <summary>Ordered shot resolution: blocks can precede the shooter's roll;
    /// a GK tie deflects, a strict save tests Handling, and only a failed save
    /// continues to later blockers. Natural six/jackpot share the same roll.</summary>
    public static ShotResolutionProbabilities CalculateShotResolutionProbabilities(PlayerToken shooter, int penalty,
        IEnumerable<ShotResolutionInteraction> interactions)
    {
        var result = new ShotResolutionProbabilities();
        if (shooter == null) return result;
        float unrolled = 1f;
        var rolled = new float[DuelDiceOutcomes.Length];
        foreach (var interaction in interactions ?? Array.Empty<ShotResolutionInteraction>())
        {
            if (interaction.token == null) continue;
            if (!interaction.goalkeeper)
            {
                float block = CalculateShotBlockProbability(interaction.token, interaction.requiredRoll, interaction.tackling);
                float mass = unrolled;
                foreach (float value in rolled) mass += value;
                result.rebounds.Add(new ShotReboundEvent { token = interaction.token, origin = interaction.tokenHex,
                    source = LooseBallSourceType.GroundDeflection, probability = mass * block });
                unrolled *= 1f - block;
                for (int i = 0; i < rolled.Length; i++) rolled[i] *= 1f - block;
                continue;
            }
            if (unrolled > 0f)
            {
                for (int i = 0; i < rolled.Length; i++)
                {
                    if (GetNaturalRoll(DuelDiceOutcomes[i]) == 1) result.offTarget += unrolled * DuelDiceOutcomes[i].probability;
                    else rolled[i] += unrolled * DuelDiceOutcomes[i].probability;
                }
                unrolled = 0f;
            }
            float tie = 0f, save = 0f;
            for (int i = 0; i < rolled.Length; i++)
            {
                int power = GetContestTotal(shooter.shooting - penalty, DuelDiceOutcomes[i].effectiveRoll);
                float failed = 0f;
                foreach (var outcome in DuelDiceOutcomes)
                {
                    int saving = GetContestTotal(interaction.token.saving + interaction.saveModifier, outcome.effectiveRoll);
                    float mass = rolled[i] * outcome.probability;
                    if (saving < power) failed += outcome.probability;
                    else if (saving == power) tie += mass;
                    else save += mass;
                }
                rolled[i] *= failed;
            }
            float holds = Math.Max(0f, Math.Min(1f, (interaction.token.handling - 1) / 6f));
            result.held += save * holds;
            result.rebounds.Add(new ShotReboundEvent { token = interaction.token, origin = interaction.contactHex,
                source = LooseBallSourceType.GroundDeflection, probability = tie });
            result.rebounds.Add(new ShotReboundEvent { token = interaction.token, origin = interaction.contactHex,
                source = LooseBallSourceType.GoalkeeperHandlingSpill, probability = save * (1f - holds) });
        }
        result.offTarget += unrolled / 6f;
        result.goal += unrolled * 5f / 6f;
        foreach (float value in rolled) result.goal += value;
        return result;
    }

    public readonly struct ShotBlockerExpectation
    {
        public readonly PlayerToken token;
        public readonly int requiredNaturalRoll;
        public readonly int? effectiveTackling;

        public ShotBlockerExpectation(PlayerToken token, int requiredNaturalRoll, int? effectiveTackling = null)
        {
            this.token = token;
            this.requiredNaturalRoll = requiredNaturalRoll;
            this.effectiveTackling = effectiveTackling;
        }
    }

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

        // Continuation forecasts keep foul branches separate from open-play outcomes.
        public float dribbleContinuationProbability => attackerWinProbability + (tieProbability / 6f);
        public float xDribbles => dribbleContinuationProbability + defenderFoulProbability;
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

    public readonly struct MovementInterceptionExpectation
    {
        public readonly float recoveryProbability;
        public readonly float foulProbability;
        public readonly float continuationProbability;

        public MovementInterceptionExpectation(
            float recoveryProbability,
            float foulProbability,
            float continuationProbability)
        {
            this.recoveryProbability = recoveryProbability;
            this.foulProbability = foulProbability;
            this.continuationProbability = continuationProbability;
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

    /// <summary>Bounded two-player control estimate for tactical positioning.
    /// Uses the actual exploding-six distribution; split unresolved ties rather
    /// than pretending either side has already won a future header.</summary>
    public static float CalculateHeadToHeadAerialControlProbability(int attackingHeading, int defendingHeading)
    {
        float probability = 0f;
        foreach (var attack in DuelDiceOutcomes)
            foreach (var defense in DuelDiceOutcomes)
            {
                int difference = GetContestTotal(attackingHeading, attack.effectiveRoll)
                    - GetContestTotal(defendingHeading, defense.effectiveRoll);
                probability += attack.probability * defense.probability * (difference > 0 ? 1f : difference == 0 ? .5f : 0f);
            }
        return probability;
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
        return GameplayScore.Total(attribute, effectiveRoll);
    }

    public static float CalculateKeeperConcedeProbability(int shotPower, int keeperSaving, int keeperPenalty)
    {
        float probability = 0f;
        foreach (DiceOutcome outcome in DuelDiceOutcomes)
        {
            int savingPower = GameplayScore.Total(keeperSaving, outcome.effectiveRoll, keeperPenalty);
            if (savingPower < shotPower)
            {
                probability += outcome.probability;
            }
        }

        return probability;
    }

    public static float CalculateShotGoalProbability(
        PlayerToken shooter,
        int shootingPenalty,
        IEnumerable<ShotBlockerExpectation> blockAttempts,
        PlayerToken goalkeeper = null,
        int goalkeeperPenalty = 0,
        int? knownShooterRoll = null)
    {
        if (shooter == null)
        {
            return 0f;
        }

        float noBlockProbability = 1f;
        if (blockAttempts != null)
        {
            foreach (ShotBlockerExpectation blockAttempt in blockAttempts)
            {
                noBlockProbability *= 1f - CalculateShotBlockProbability(
                    blockAttempt.token,
                    blockAttempt.requiredNaturalRoll,
                    blockAttempt.effectiveTackling);
            }
        }

        if (knownShooterRoll.HasValue)
        {
            int roll = knownShooterRoll.Value;
            if (roll <= 1) return 0f;
            int power = GameplayScore.Total(shooter.shooting, roll, -shootingPenalty);
            return noBlockProbability * (goalkeeper != null
                ? CalculateKeeperConcedeProbability(power, goalkeeper.saving, goalkeeperPenalty) : 1f);
        }
        float goalAfterNoBlockProbability = 0f;
        foreach (DiceOutcome shooterOutcome in DuelDiceOutcomes)
        {
            int naturalRoll = GetNaturalRoll(shooterOutcome);
            if (naturalRoll <= 1)
            {
                continue;
            }

            int shotPower = shooterOutcome.effectiveRoll == 50
                ? 50
                : GameplayScore.Total(shooter.shooting, naturalRoll, -shootingPenalty);

            float concedeProbability = goalkeeper != null
                ? CalculateKeeperConcedeProbability(shotPower, goalkeeper.saving, goalkeeperPenalty)
                : 1f;

            goalAfterNoBlockProbability += shooterOutcome.probability * concedeProbability;
        }

        return noBlockProbability * goalAfterNoBlockProbability;
    }

    public static float CalculateShotBlockProbability(PlayerToken blocker, int requiredNaturalRoll, int? effectiveTackling = null)
    {
        if (blocker == null)
        {
            return 0f;
        }

        int tackling = effectiveTackling ?? blocker.tackling;
        float probability = 0f;
        foreach (DiceOutcome outcome in DuelDiceOutcomes)
        {
            int naturalRoll = GetNaturalRoll(outcome);
            bool succeeds = outcome.effectiveRoll == 50
                || naturalRoll >= requiredNaturalRoll
                || naturalRoll + tackling >= 10;

            if (succeeds)
            {
                probability += outcome.probability;
            }
        }

        return probability;
    }

    private static int GetNaturalRoll(DiceOutcome outcome)
    {
        return outcome.effectiveRoll == 50 ? 6 : outcome.effectiveRoll;
    }

    public static float CalculateAttributeCheckSuccessProbability(
        int attribute,
        int modifier = 0,
        int target = 9)
    {
        float successProbability = 0f;
        foreach (DiceOutcome outcome in DuelDiceOutcomes)
        {
            int total = outcome.effectiveRoll == 50
                ? 50
                : GameplayScore.Total(attribute, GetNaturalRoll(outcome), modifier);
            if (total >= target)
            {
                successProbability += outcome.probability;
            }
        }

        return successProbability;
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

        return CalculateRecoveryProbability(defender.tackling, 0, minimumNaturalRollForSuccess);
    }

    public static float CalculateRecoveryProbability(int attribute, int modifier = 0, int minimumNaturalRollForSuccess = 6)
    {
        int successfulRolls = 0;

        for (int roll = 1; roll <= 6; roll++)
        {
            if (roll >= minimumNaturalRollForSuccess || GameplayScore.Total(attribute, roll, modifier) >= 10)
            {
                successfulRolls++;
            }
        }

        return successfulRolls / 6f;
    }

    public static float CalculateAtLeastOneRecoveryProbability(
        IEnumerable<PlayerToken> defenders,
        int minimumNaturalRollForSuccess = 6)
    {
        if (defenders == null)
        {
            return 0f;
        }

        float allAttemptsFailProbability = 1f;
        foreach (PlayerToken defender in defenders)
        {
            if (defender == null)
            {
                continue;
            }

            allAttemptsFailProbability *= 1f
                - CalculateRecoveryProbability(defender, minimumNaturalRollForSuccess);
        }

        return 1f - allAttemptsFailProbability;
    }

    public static MovementInterceptionExpectation CalculateMovementInterceptionExpectation(
        PlayerToken defender,
        int foulThreshold = 1,
        int interceptionThreshold = 10)
    {
        if (defender == null)
        {
            return new MovementInterceptionExpectation(0f, 0f, 1f);
        }

        int recoveryOutcomes = 0;
        int foulOutcomes = 0;
        int continuationOutcomes = 0;
        for (int roll = 1; roll <= 6; roll++)
        {
            if (roll <= foulThreshold)
            {
                foulOutcomes++;
            }
            else if (roll == 6 || roll + defender.tackling >= interceptionThreshold)
            {
                recoveryOutcomes++;
            }
            else
            {
                continuationOutcomes++;
            }
        }

        return new MovementInterceptionExpectation(
            recoveryOutcomes / 6f,
            foulOutcomes / 6f,
            continuationOutcomes / 6f);
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

        return CalculateGroundDuelExpectationWithDefenderSkill(
            attacker,
            defender,
            defender.tackling,
            defenderBonusMalus);
    }

    public static GroundDuelExpectation CalculateGroundDuelExpectationWithDefenderSkill(
        PlayerToken attacker,
        PlayerToken defender,
        int defenderEffectiveSkill,
        int defenderBonusMalus = 0,
        int defenderFoulThreshold = 1)
    {
        if (attacker == null || defender == null)
        {
            return new GroundDuelExpectation(0f, 0f, 0f, 0f);
        }

        int attackerDribbling = attacker.dribbling;
        int defenderTackling = defenderEffectiveSkill + defenderBonusMalus;

        float attackerWins = 0f;
        float defenderWins = 0f;
        float ties = 0f;
        float defenderFouls = 0f;

        foreach (DiceOutcome defenderOutcome in DuelDiceOutcomes)
        {
            foreach (DiceOutcome attackerOutcome in DuelDiceOutcomes)
            {
                float branchProbability = defenderOutcome.probability * attackerOutcome.probability;

                if (defenderOutcome.effectiveRoll <= defenderFoulThreshold)
                {
                    defenderFouls += branchProbability;
                    continue;
                }

                int defenderTotal = defenderOutcome.effectiveRoll == 50
                    ? 50
                    : GameplayScore.Total(defenderTackling, defenderOutcome.effectiveRoll);
                int attackerTotal = attackerOutcome.effectiveRoll == 50
                    ? 50
                    : GameplayScore.Total(attackerDribbling, attackerOutcome.effectiveRoll);

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

    /// <summary>Conditional outcome chances after the defender roll; no xDr/xTk recovery weighting.</summary>
    public static GroundDuelExpectation CalculateGroundDuelAfterDefenderRoll(
        int attackerDribbling, int defenderTotal, bool defenderFouled)
    {
        if (defenderFouled) return new GroundDuelExpectation(0f, 0f, 0f, 1f);
        float dribble = 0f, tackle = 0f, loose = 0f;
        foreach (DiceOutcome roll in DuelDiceOutcomes)
        {
            int attackerTotal = GameplayScore.Total(attackerDribbling, roll.effectiveRoll);
            if (attackerTotal > defenderTotal) dribble += roll.probability;
            else if (attackerTotal < defenderTotal) tackle += roll.probability;
            else loose += roll.probability;
        }
        return new GroundDuelExpectation(dribble, tackle, loose, 0f);
    }
}
