public enum DecisionKind
{
    SelectNextAction,
    StandardPassTarget,
    FinalThirdAttackingMovement,
    FinalThirdDefensiveMovement,
    AttackingMovement,
    DefensiveMovement,
    AttackingTwoForTwo,
    SnapshotOrForfeitPace,
    TackleReposition,
    NutmegReposition,
    DefensiveBoxMovement,
    StandaloneTackleOrStand,
    StandaloneNutmegDecision,
    FirstTimePassTarget,
    FirstTimePassAttackerMovement,
    FirstTimePassDefenderMovement,
    HighPassTarget,
    HighPassAttackerMovement,
    HighPassDefenderMovement,
    HighPassAttackingJumpers,
    HighPassDefensiveJumpers,
    HighPassGoalkeeperRush,
    LongBallTarget,
    LongBallGoalkeeperRush,
    HeaderOrBallControl,
    HeadedPassOrHeaderAtGoal,
    HeadedPassTarget,
    ShotGoalLineTarget,
    SnapshotGoalLineTarget,
    SnapshotDefenderMovement,
    GoalkeeperPickupOrLeave,
    GoalkeeperMovement,
    QuickThrowOrDistribution,
    GoalkeeperDistributionTarget,
    ThrowInTakerSelection,
    ThrowInMovementOrThrow,
    ThrowInActionSelection,
    ThrowInTarget,
    FreeKickTakerSelection,
    FreeKickAttackingSetup,
    FreeKickDefensiveSetup,
    FreeKickGoalkeeperMovement,
    FreeKickFinalKickerSelection,
    FreeKickActionSelection,
    AdvantageOrFreeKick,
    CornerTakerSelection,
    CornerAttackingThreeMoves,
    CornerDefensiveThreeMoves,
    CornerActionSelection,
    GoalKickAttackingSetup,
    GoalKickDefensiveSetup,
    GoalKickActionSelection,
    GoalKickTarget,
    PenaltyTakerSelection,
    PenaltyAttackingSetup,
    PenaltyDefensiveSetup,
    ShootoutOrderSelection,
    KickoffFormationSetup,
    KickoffTakerSelection,
    KickoffConfirmation,
}

public static class DecisionTimingConfig
{
    public const float CountdownDisplaySpentFraction = 0.5f;
    public const float AutomaticRollSeconds = 5f;
    public const float CountdownWarningSeconds = 5f;

    // Budget table                                   Diff 1  Diff 2  Diff 3
    public static readonly System.Collections.Generic.Dictionary<
        DecisionKind, (float Diff1, float Diff2, float Diff3)> Budgets = new()
    {
        // Open play
        [DecisionKind.SelectNextAction]              = (30f,   20f,   15f),
        [DecisionKind.StandardPassTarget]            = (20f,   15f,   10f),
        [DecisionKind.FinalThirdAttackingMovement]   = (30f,   20f,   15f),
        [DecisionKind.FinalThirdDefensiveMovement]   = (30f,   20f,   15f),

        // Movement phases
        [DecisionKind.AttackingMovement]             = (90f,   80f,   60f),
        [DecisionKind.DefensiveMovement]             = (120f, 100f,   90f),
        [DecisionKind.AttackingTwoForTwo]            = (30f,   20f,   10f),
        [DecisionKind.SnapshotOrForfeitPace]          = (10f,   10f,    5f),
        [DecisionKind.TackleReposition]              = (15f,   10f,    5f),
        [DecisionKind.NutmegReposition]              = (15f,   10f,    5f),
        [DecisionKind.DefensiveBoxMovement]          = (10f,   10f,    8f),

        // Choice allowance; also charge the deciding team's MP when it is active.
        [DecisionKind.StandaloneTackleOrStand]       = (10f,   10f,    5f),
        [DecisionKind.StandaloneNutmegDecision]      = (10f,   10f,    5f),

        // First Time Pass
        [DecisionKind.FirstTimePassTarget]           = (20f,   15f,   10f),
        [DecisionKind.FirstTimePassAttackerMovement] = (15f,   15f,   10f),
        [DecisionKind.FirstTimePassDefenderMovement] = (15f,   15f,   10f),

        // High Pass
        [DecisionKind.HighPassTarget]                = (20f,   15f,   10f),
        [DecisionKind.HighPassAttackerMovement]      = (15f,   15f,   10f),
        [DecisionKind.HighPassDefenderMovement]      = (15f,   15f,   10f),
        [DecisionKind.HighPassAttackingJumpers]      = (15f,   15f,   10f),
        [DecisionKind.HighPassDefensiveJumpers]      = (15f,   15f,   10f),
        [DecisionKind.HighPassGoalkeeperRush]        = (10f,   10f,    5f),

        // Long Ball
        [DecisionKind.LongBallTarget]                = (20f,   15f,   10f),
        [DecisionKind.LongBallGoalkeeperRush]        = (10f,   10f,    5f),

        // Headers and ball control
        [DecisionKind.HeaderOrBallControl]           = (10f,   10f,    5f),
        [DecisionKind.HeadedPassOrHeaderAtGoal]      = (10f,   10f,    5f),
        [DecisionKind.HeadedPassTarget]              = (20f,   15f,   10f),

        // Shots
        [DecisionKind.ShotGoalLineTarget]            = (10f,   10f,    5f),
        [DecisionKind.SnapshotGoalLineTarget]        = (10f,   10f,    5f),
        [DecisionKind.SnapshotDefenderMovement]      = (15f,   15f,   10f),

        // Goalkeeper
        [DecisionKind.GoalkeeperPickupOrLeave]       = (10f,   10f,    5f),
        [DecisionKind.GoalkeeperMovement]            = (15f,   15f,   10f),
        [DecisionKind.QuickThrowOrDistribution]      = (10f,   10f,    5f),
        [DecisionKind.GoalkeeperDistributionTarget]  = (20f,   15f,   10f),

        // Throw-in
        [DecisionKind.ThrowInTakerSelection]         = (20f,   15f,   10f),
        [DecisionKind.ThrowInMovementOrThrow]        = (20f,   15f,   10f),
        [DecisionKind.ThrowInActionSelection]        = (20f,   15f,   10f),
        [DecisionKind.ThrowInTarget]                 = (20f,   15f,   10f),

        // Free kick
        [DecisionKind.FreeKickTakerSelection]        = (20f,   15f,   10f),
        [DecisionKind.FreeKickAttackingSetup]        = (30f,   20f,   15f),
        [DecisionKind.FreeKickDefensiveSetup]        = (30f,   20f,   15f),
        [DecisionKind.FreeKickGoalkeeperMovement]    = (15f,   15f,   10f),
        [DecisionKind.FreeKickFinalKickerSelection]  = (20f,   15f,   10f),
        [DecisionKind.FreeKickActionSelection]       = (20f,   15f,   10f),
        [DecisionKind.AdvantageOrFreeKick]           = (10f,   10f,    5f),

        // Corner kick
        [DecisionKind.CornerTakerSelection]          = (20f,   15f,   10f),
        [DecisionKind.CornerAttackingThreeMoves]     = (20f,   15f,   10f),
        [DecisionKind.CornerDefensiveThreeMoves]     = (20f,   15f,   10f),
        [DecisionKind.CornerActionSelection]         = (20f,   15f,   10f),

        // Goal kick
        [DecisionKind.GoalKickAttackingSetup]        = (30f,   20f,   15f),
        [DecisionKind.GoalKickDefensiveSetup]        = (30f,   20f,   15f),
        [DecisionKind.GoalKickActionSelection]       = (20f,   15f,   10f),
        [DecisionKind.GoalKickTarget]                = (20f,   15f,   10f),

        // Penalties and shootouts
        [DecisionKind.PenaltyTakerSelection]         = (20f,   15f,   10f),
        [DecisionKind.PenaltyAttackingSetup]         = (30f,   20f,   15f),
        [DecisionKind.PenaltyDefensiveSetup]         = (30f,   20f,   15f),
        [DecisionKind.ShootoutOrderSelection]        = (90f,   80f,   60f),

        // Kickoff
        [DecisionKind.KickoffFormationSetup]         = (90f,   80f,   60f),
        [DecisionKind.KickoffTakerSelection]          = (20f,   15f,   10f),
        [DecisionKind.KickoffConfirmation]           = (20f,   15f,   10f),

    };

    public static float GetBudget(DecisionKind kind, int difficulty)
    {
        var budget = Budgets[kind];
        return difficulty switch
        {
            1 => budget.Diff1,
            2 => budget.Diff2,
            3 => budget.Diff3,
            _ => throw new System.ArgumentOutOfRangeException(
                nameof(difficulty), difficulty, "Expected difficulty 1, 2, or 3.")
        };
    }
}
