using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Original legacy branch inputs and expected outcomes, copied without gameplay execution.
public static class LegacyShotBranchCatalog
{
    public enum ShootingBranchStepType
    {
        GoalkeeperWallSaveRoll,
        ShotBlockRoll,
        ShooterRoll,
        GkSaveRoll,
        HandlingRoll,
        LooseDirectionRoll,
        LooseDistanceRoll,
        LooseInterceptionRoll,
        GoalkeeperMove,
        ExpectAnyOtherScenario,
        ExpectMovementToAttack,
        ExpectSaveAndHold,
        ExpectGoalkeeperRecoveryHold,
        ExpectCornerNorth,
        ExpectCornerSouth,
        ExpectGoal,
        ExpectOwnGoal,
        ExpectOffTarget
    }

    public sealed class ShootingBranchStep
    {
        public ShootingBranchStepType Type;
        public string PlayerName;
        public int Roll;
        public Vector2Int Coordinates;
    }

    public sealed class ShootingBranchDefinition
    {
        public ShootingBranchDefinition(string name, params ShootingBranchStep[] steps)
        {
            Name = name;
            Steps = steps.ToList();
        }

        public string Name { get; }
        public List<ShootingBranchStep> Steps { get; }
    }

    public sealed class FreeKickShotBranchDefinition
    {
        public FreeKickShotBranchDefinition(string name, params ShootingBranchStep[] steps)
        {
            Name = name;
            Steps = steps.ToList();
        }

        public string Name { get; }
        public List<ShootingBranchStep> Steps { get; }
    }

    private static ShootingBranchStep ShotBlock(string playerName, int roll)
    {
        return new ShootingBranchStep { Type = ShootingBranchStepType.ShotBlockRoll, PlayerName = playerName, Roll = roll };
    }

    private static ShootingBranchStep ShooterRoll(string playerName, int roll)
    {
        return new ShootingBranchStep { Type = ShootingBranchStepType.ShooterRoll, PlayerName = playerName, Roll = roll };
    }

    private static ShootingBranchStep GkSave(string playerName, int roll)
    {
        return new ShootingBranchStep { Type = ShootingBranchStepType.GkSaveRoll, PlayerName = playerName, Roll = roll };
    }

    private static ShootingBranchStep Handling(int roll)
    {
        return new ShootingBranchStep { Type = ShootingBranchStepType.HandlingRoll, Roll = roll };
    }

    private static ShootingBranchStep LooseDirection(int roll)
    {
        return new ShootingBranchStep { Type = ShootingBranchStepType.LooseDirectionRoll, Roll = roll };
    }

    private static ShootingBranchStep LooseDistance(int roll)
    {
        return new ShootingBranchStep { Type = ShootingBranchStepType.LooseDistanceRoll, Roll = roll };
    }

    private static ShootingBranchStep LooseInterception(string playerName, int roll)
    {
        return new ShootingBranchStep { Type = ShootingBranchStepType.LooseInterceptionRoll, PlayerName = playerName, Roll = roll };
    }

    private static ShootingBranchStep GkMove(int x, int z)
    {
        return new ShootingBranchStep { Type = ShootingBranchStepType.GoalkeeperMove, Coordinates = new Vector2Int(x, z) };
    }

    private static ShootingBranchStep ExpectAnyOther(string playerName = null)
    {
        return new ShootingBranchStep { Type = playerName == "Kuzmic" ? ShootingBranchStepType.ExpectGoalkeeperRecoveryHold : ShootingBranchStepType.ExpectAnyOtherScenario, PlayerName = playerName };
    }

    private static ShootingBranchStep ExpectMovementToAttack()
    {
        return new ShootingBranchStep { Type = ShootingBranchStepType.ExpectMovementToAttack };
    }

    private static ShootingBranchStep ExpectSaveAndHold()
    {
        return new ShootingBranchStep { Type = ShootingBranchStepType.ExpectSaveAndHold };
    }

    private static ShootingBranchStep ExpectCornerNorth()
    {
        return new ShootingBranchStep { Type = ShootingBranchStepType.ExpectCornerNorth };
    }

    private static ShootingBranchStep ExpectCornerSouth()
    {
        return new ShootingBranchStep { Type = ShootingBranchStepType.ExpectCornerSouth };
    }

    private static ShootingBranchStep ExpectGoal()
    {
        return new ShootingBranchStep { Type = ShootingBranchStepType.ExpectGoal };
    }

    private static ShootingBranchStep ExpectOwnGoal()
    {
        return new ShootingBranchStep { Type = ShootingBranchStepType.ExpectOwnGoal };
    }

    private static ShootingBranchStep ExpectOffTarget()
    {
        return new ShootingBranchStep { Type = ShootingBranchStepType.ExpectOffTarget };
    }

    public static List<ShootingBranchDefinition> BuildShootingBranchDefinitions()
    {
        ShootingBranchStep[] missedPatersonMissedMcNultySavedBase =
        {
            ShotBlock("Paterson", 4),
            GkMove(15, -1),
            ShotBlock("McNulty", 4),
            ShotBlock("Soares", 3),
            ShooterRoll("Yaneva", 4),
            GkSave("Kuzmic", 2)
        };

        ShootingBranchStep[] looseBallByKuzmicBase =
        {
            ShotBlock("Paterson", 4),
            GkMove(15, -1),
            ShotBlock("McNulty", 4),
            ShotBlock("Soares", 3),
            ShooterRoll("Yaneva", 5),
            GkSave("Kuzmic", 2)
        };

        ShootingBranchStep[] poulsenGoalLineBase =
        {
            ShotBlock("Paterson", 4),
            GkMove(15, -1),
            ShotBlock("McNulty", 4),
            ShotBlock("Soares", 3),
            ShooterRoll("Yaneva", 6),
            GkSave("Kuzmic", 2)
        };

        List<ShootingBranchDefinition> branches = new()
        {
            new ShootingBranchDefinition(
                "Blocked by Paterson Picked up by Gilbert",
                ShotBlock("Paterson", 6),
                LooseDirection(3),
                LooseDistance(6),
                LooseInterception("Gilbert", 6),
                ExpectAnyOther("Gilbert")),

            new ShootingBranchDefinition(
                "Blocked by Paterson missed by Gilbert",
                ShotBlock("Paterson", 6),
                LooseDirection(3),
                LooseDistance(6),
                LooseInterception("Gilbert", 3),
                ExpectMovementToAttack()),

            new ShootingBranchDefinition(
                "Blocked by Paterson missed by Paterson",
                ShotBlock("Paterson", 6),
                LooseDirection(1),
                LooseDistance(1),
                LooseInterception("Paterson", 3),
                ExpectMovementToAttack()),

            new ShootingBranchDefinition(
                "Blocked by Paterson into the Box McNulty misses",
                ShotBlock("Paterson", 6),
                LooseDirection(6),
                LooseDistance(5),
                LooseInterception("McNulty", 3),
                GkMove(15, -1),
                // Current GK-wall rules offer a save from the newly reached wall
                // before flight resumes. Preserve the original missed-recovery outcome.
                new ShootingBranchStep { Type = ShootingBranchStepType.GoalkeeperWallSaveRoll, PlayerName = "Kuzmic", Roll = 2 },
                ExpectMovementToAttack()),

            new ShootingBranchDefinition(
                "Blocked by Paterson into the Box McNulty picks",
                ShotBlock("Paterson", 6),
                LooseDirection(6),
                LooseDistance(5),
                LooseInterception("McNulty", 6),
                GkMove(15, -1),
                ExpectAnyOther("McNulty")),

            new ShootingBranchDefinition(
                "Missed by Paterson Blocked by McNulty Paterson Picks",
                ShotBlock("Paterson", 4),
                GkMove(15, -1),
                ShotBlock("McNulty", 6),
                LooseDirection(2),
                LooseDistance(6),
                LooseInterception("Paterson", 6),
                ExpectAnyOther("Paterson")),

            new ShootingBranchDefinition(
                "Missed by Paterson Blocked by McNulty Paterson Misses",
                ShotBlock("Paterson", 4),
                GkMove(15, -1),
                ShotBlock("McNulty", 6),
                LooseDirection(2),
                LooseDistance(6),
                LooseInterception("Paterson", 4),
                ExpectMovementToAttack()),

            new ShootingBranchDefinition(
                "Missed by Paterson Blocked by McNulty McNulty Picks",
                ShotBlock("Paterson", 4),
                GkMove(15, -1),
                ShotBlock("McNulty", 6),
                LooseDirection(1),
                LooseDistance(1),
                LooseInterception("McNulty", 6),
                ExpectAnyOther("McNulty")),

            new ShootingBranchDefinition(
                "Missed by Paterson Blocked by McNulty McNulty Misses",
                ShotBlock("Paterson", 4),
                GkMove(15, -1),
                ShotBlock("McNulty", 6),
                LooseDirection(1),
                LooseDistance(1),
                LooseInterception("McNulty", 2),
                ExpectMovementToAttack()),

            new ShootingBranchDefinition(
                "Missed by Paterson Blocked by McNulty Soares Picks",
                ShotBlock("Paterson", 4),
                GkMove(15, -1),
                ShotBlock("McNulty", 6),
                LooseDirection(6),
                LooseDistance(3),
                LooseInterception("Soares", 6),
                ExpectAnyOther("Soares")),

            new ShootingBranchDefinition(
                "Missed by Paterson Blocked by McNulty Kuzmic Picks",
                ShotBlock("Paterson", 4),
                GkMove(15, -1),
                ShotBlock("McNulty", 6),
                LooseDirection(6),
                LooseDistance(3),
                LooseInterception("Soares", 2),
                LooseInterception("Kuzmic", 6),
                ExpectAnyOther("Kuzmic")),

            new ShootingBranchDefinition(
                "Missed by Paterson Blocked by McNulty Kuzmic Misses",
                ShotBlock("Paterson", 4),
                GkMove(15, -1),
                ShotBlock("McNulty", 6),
                LooseDirection(6),
                LooseDistance(3),
                LooseInterception("Soares", 2),
                LooseInterception("Kuzmic", 2),
                ExpectMovementToAttack()),

            new ShootingBranchDefinition(
                "Missed by Paterson McNulty Blocked by Soares McNulty Picks",
                ShotBlock("Paterson", 4),
                GkMove(15, -1),
                ShotBlock("McNulty", 4),
                ShotBlock("Soares", 6),
                LooseDirection(2),
                LooseDistance(6),
                LooseInterception("McNulty", 6),
                ExpectAnyOther("McNulty")),

            new ShootingBranchDefinition(
                "Missed by Paterson McNulty Blocked by Soares McNulty Misses",
                ShotBlock("Paterson", 4),
                GkMove(15, -1),
                ShotBlock("McNulty", 4),
                ShotBlock("Soares", 6),
                LooseDirection(2),
                LooseDistance(6),
                LooseInterception("McNulty", 4),
                ExpectMovementToAttack()),

            new ShootingBranchDefinition(
                "Missed by Paterson McNulty Blocked by Soares Kuzmic Picks",
                ShotBlock("Paterson", 4),
                GkMove(15, -1),
                ShotBlock("McNulty", 4),
                ShotBlock("Soares", 6),
                LooseDirection(6),
                LooseDistance(6),
                ExpectAnyOther("Kuzmic")),

            new ShootingBranchDefinition(
                "Missed by Paterson McNulty Blocked by Soares Kuzmic Misses",
                ShotBlock("Paterson", 4),
                GkMove(15, -1),
                ShotBlock("McNulty", 4),
                ShotBlock("Soares", 6),
                LooseDirection(5),
                LooseDistance(4),
                LooseInterception("Kuzmic", 2),
                ExpectMovementToAttack()),

            new ShootingBranchDefinition(
                "Missed by Paterson McNulty Blocked by Soares Kuzmic Misses Own Goal",
                ShotBlock("Paterson", 4),
                GkMove(15, -1),
                ShotBlock("McNulty", 4),
                ShotBlock("Soares", 6),
                LooseDirection(5),
                LooseDistance(5),
                LooseInterception("Kuzmic", 2),
                ExpectOwnGoal()),

            new ShootingBranchDefinition(
                "Saved by Kuzmic and held",
                missedPatersonMissedMcNultySavedBase.Concat(new[]
                {
                    Handling(1),
                    ExpectSaveAndHold()
                }).ToArray()),

            new ShootingBranchDefinition(
                "Saved by Kuzmic and Corner N",
                missedPatersonMissedMcNultySavedBase.Concat(new[]
                {
                    Handling(6),
                    LooseDirection(4),
                    ExpectCornerNorth()
                }).ToArray()),

            new ShootingBranchDefinition(
                "Saved by Kuzmic and Corner S",
                missedPatersonMissedMcNultySavedBase.Concat(new[]
                {
                    Handling(6),
                    LooseDirection(6),
                    ExpectCornerSouth()
                }).ToArray()),

            new ShootingBranchDefinition(
                "Saved by Kuzmic and Spill Soares Hits",
                missedPatersonMissedMcNultySavedBase.Concat(new[]
                {
                    Handling(6),
                    LooseDirection(2),
                    LooseDistance(6),
                    ExpectAnyOther("Soares")
                }).ToArray()),

            new ShootingBranchDefinition(
                "Saved by Kuzmic and Spill Soares Misses",
                missedPatersonMissedMcNultySavedBase.Concat(new[]
                {
                    Handling(6),
                    LooseDirection(3),
                    LooseDistance(4),
                    LooseInterception("Soares", 3),
                    ExpectMovementToAttack()
                }).ToArray()),

            new ShootingBranchDefinition(
                "Loose Ball by Kuzmic Poulsen Misses",
                looseBallByKuzmicBase.Concat(new[]
                {
                    LooseDirection(6),
                    LooseDistance(6),
                    LooseInterception("Poulsen", 2),
                    ExpectGoal()
                }).ToArray()),

            new ShootingBranchDefinition(
                "Loose Ball by Kuzmic Poulsen Intercepts",
                looseBallByKuzmicBase.Concat(new[]
                {
                    LooseDirection(6),
                    LooseDistance(6),
                    LooseInterception("Poulsen", 6),
                    ExpectAnyOther("Poulsen")
                }).ToArray()),

            new ShootingBranchDefinition(
                "Loose Ball by Kuzmic Kalla Picks",
                looseBallByKuzmicBase.Concat(new[]
                {
                    LooseDirection(4),
                    LooseDistance(1),
                    ExpectAnyOther("Kalla")
                }).ToArray()),

            new ShootingBranchDefinition(
                "Loose Ball by Kuzmic Soares Picks",
                looseBallByKuzmicBase.Concat(new[]
                {
                    LooseDirection(3),
                    LooseDistance(4),
                    LooseInterception("Soares", 6),
                    ExpectAnyOther("Soares")
                }).ToArray()),

            new ShootingBranchDefinition(
                "Loose Ball by Kuzmic Soares Misses",
                looseBallByKuzmicBase.Concat(new[]
                {
                    LooseDirection(3),
                    LooseDistance(4),
                    LooseInterception("Soares", 2),
                    ExpectMovementToAttack()
                }).ToArray()),

            new ShootingBranchDefinition(
                "Poulsen Goal",
                poulsenGoalLineBase.Concat(new[]
                {
                    ShotBlock("Poulsen", 1),
                    ExpectGoal()
                }).ToArray()),

            new ShootingBranchDefinition(
                "Poulsen Own Goal",
                poulsenGoalLineBase.Concat(new[]
                {
                    ShotBlock("Poulsen", 6),
                    LooseDirection(6),
                    LooseDistance(2),
                    ExpectOwnGoal()
                }).ToArray()),

            new ShootingBranchDefinition(
                "Poulsen Blocks Kuzmic Picks Up",
                poulsenGoalLineBase.Concat(new[]
                {
                    ShotBlock("Poulsen", 6),
                    LooseDirection(2),
                    LooseDistance(6),
                    LooseInterception("Kuzmic", 6),
                    ExpectAnyOther("Kuzmic")
                }).ToArray()),

            new ShootingBranchDefinition(
                "Poulsen Blocks Kuzmic Misses",
                poulsenGoalLineBase.Concat(new[]
                {
                    ShotBlock("Poulsen", 6),
                    LooseDirection(2),
                    LooseDistance(6),
                    LooseInterception("Kuzmic", 2),
                    ExpectMovementToAttack()
                }).ToArray()),
        };

        return branches;
    }

    public static List<FreeKickShotBranchDefinition> BuildFreeKickShotBranchDefinitions()
    {
        ShootingBranchStep[] failedBlocksBase =
        {
            ShooterRoll("Noruega", 5),
            GkMove(17, -1),
            ShotBlock("Soares", 1)
        };

        return new List<FreeKickShotBranchDefinition>
        {
            new FreeKickShotBranchDefinition(
                "Natural one missed blocks off target",
                ShooterRoll("Noruega", 1),
                GkMove(17, -1),
                ShotBlock("Soares", 1),
                ExpectOffTarget()),

            new FreeKickShotBranchDefinition(
                "Noruega 5 failed blocks Kuzmic save roll 2 goal",
                failedBlocksBase.Concat(new[]
                {
                    GkSave("Kuzmic", 2),
                    ExpectGoal()
                }).ToArray()),

            new FreeKickShotBranchDefinition(
                "Noruega 5 failed blocks Kuzmic save roll 4 handling hold",
                failedBlocksBase.Concat(new[]
                {
                    GkSave("Kuzmic", 4),
                    Handling(1),
                    ExpectSaveAndHold()
                }).ToArray()),

            new FreeKickShotBranchDefinition(
                "Noruega 5 failed blocks Kuzmic save roll 4 handling spill corner south",
                failedBlocksBase.Concat(new[]
                {
                    GkSave("Kuzmic", 4),
                    Handling(6),
                    LooseDirection(1),
                    ExpectCornerSouth()
                }).ToArray()),

            new FreeKickShotBranchDefinition(
                "Noruega 5 failed blocks Kuzmic save roll 4 spill Vladoiu hit",
                failedBlocksBase.Concat(new[]
                {
                    GkSave("Kuzmic", 4),
                    Handling(6),
                    LooseDirection(2),
                    LooseDistance(5),
                    ExpectAnyOther("Vladoiu")
                }).ToArray()),

            new FreeKickShotBranchDefinition(
                "Noruega 5 failed blocks Kuzmic save roll 4 spill Vladoiu intercepts",
                failedBlocksBase.Concat(new[]
                {
                    GkSave("Kuzmic", 4),
                    Handling(6),
                    LooseDirection(2),
                    LooseDistance(2),
                    LooseInterception("Vladoiu", 6),
                    ExpectAnyOther("Vladoiu")
                }).ToArray()),

            new FreeKickShotBranchDefinition(
                "Noruega 5 failed blocks Kuzmic save roll 4 spill Vladoiu misses",
                failedBlocksBase.Concat(new[]
                {
                    GkSave("Kuzmic", 4),
                    Handling(6),
                    LooseDirection(2),
                    LooseDistance(2),
                    LooseInterception("Vladoiu", 5),
                    ExpectMovementToAttack()
                }).ToArray()),

            new FreeKickShotBranchDefinition(
                "Noruega 5 failed blocks Kuzmic tie loose direction 1 distance 1 Kuzmic misses",
                failedBlocksBase.Concat(new[]
                {
                    GkSave("Kuzmic", 3),
                    LooseDirection(1),
                    LooseDistance(1),
                    LooseInterception("Kuzmic", 1),
                    ExpectMovementToAttack()
                }).ToArray()),

            new FreeKickShotBranchDefinition(
                "Noruega 5 failed blocks Kuzmic tie loose direction 1 distance 2 movement",
                failedBlocksBase.Concat(new[]
                {
                    GkSave("Kuzmic", 3),
                    LooseDirection(1),
                    LooseDistance(2),
                    ExpectMovementToAttack()
                }).ToArray()),

            new FreeKickShotBranchDefinition(
                "Noruega 5 failed blocks Kuzmic tie loose direction 6 distance 4 goal",
                failedBlocksBase.Concat(new[]
                {
                    GkSave("Kuzmic", 3),
                    LooseDirection(6),
                    LooseDistance(4),
                    ExpectGoal()
                }).ToArray()),

            new FreeKickShotBranchDefinition(
                "Noruega 5 failed blocks Kuzmic tie loose Soares misses movement",
                failedBlocksBase.Concat(new[]
                {
                    GkSave("Kuzmic", 3),
                    LooseDirection(3),
                    LooseDistance(6),
                    LooseInterception("Soares", 4),
                    ExpectMovementToAttack()
                }).ToArray()),

            new FreeKickShotBranchDefinition(
                "Noruega 6 perfect height skips blocks Kuzmic save roll 3 goal",
                ShooterRoll("Noruega", 6),
                GkMove(17, -1),
                GkSave("Kuzmic", 3),
                ExpectGoal())
        };
    }

}
