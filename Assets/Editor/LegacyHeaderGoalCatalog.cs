using System.Collections.Generic;
using UnityEngine;

public static class LegacyHeaderGoalCatalog
{
    public enum HeaderAtGoalOutcomeExpectation
    {
        Goal,
        CornerKick,
        MovementPhase,
        SaveAndHold
    }

    public sealed class HeaderAtGoalBranchDefinition
    {
        public HeaderAtGoalBranchDefinition(
            string name,
            bool gkRush,
            int attackerHeaderRoll,
            int defenderHeaderRoll,
            int? goalkeeperHeaderOrAerialRoll,
            int? handlingRoll,
            int? looseDirectionRoll,
            int? looseDistanceRoll,
            HeaderAtGoalOutcomeExpectation expectedOutcome,
            Vector2Int? expectedBallHex = null,
            string looseInterceptionPlayerName = null,
            int? looseInterceptionRoll = null,
            string forbiddenLooseInterceptorName = null)
        {
            Name = name;
            GkRush = gkRush;
            AttackerHeaderRoll = attackerHeaderRoll;
            DefenderHeaderRoll = defenderHeaderRoll;
            GoalkeeperHeaderOrAerialRoll = goalkeeperHeaderOrAerialRoll;
            HandlingRoll = handlingRoll;
            LooseDirectionRoll = looseDirectionRoll;
            LooseDistanceRoll = looseDistanceRoll;
            ExpectedOutcome = expectedOutcome;
            ExpectedBallHex = expectedBallHex;
            LooseInterceptionPlayerName = looseInterceptionPlayerName;
            LooseInterceptionRoll = looseInterceptionRoll;
            ForbiddenLooseInterceptorName = forbiddenLooseInterceptorName;
        }

        public string Name { get; }
        public bool GkRush { get; }
        public int AttackerHeaderRoll { get; }
        public int DefenderHeaderRoll { get; }
        public int? GoalkeeperHeaderOrAerialRoll { get; }
        public int? HandlingRoll { get; }
        public int? LooseDirectionRoll { get; }
        public int? LooseDistanceRoll { get; }
        public HeaderAtGoalOutcomeExpectation ExpectedOutcome { get; }
        public Vector2Int? ExpectedBallHex { get; }
        public string LooseInterceptionPlayerName { get; }
        public int? LooseInterceptionRoll { get; }
        public string ForbiddenLooseInterceptorName { get; }
    }

    public static List<HeaderAtGoalBranchDefinition> BuildHeaderAtGoalBranchDefinitions()
    {
        return new List<HeaderAtGoalBranchDefinition>
        {
            new HeaderAtGoalBranchDefinition("No rush save roll 2 goal", false, 6, 4, 2, null, null, null, HeaderAtGoalOutcomeExpectation.Goal),
            new HeaderAtGoalBranchDefinition("No rush save tie direction 5 distance 5 goal", false, 6, 4, 4, null, 5, 5, HeaderAtGoalOutcomeExpectation.Goal),
            new HeaderAtGoalBranchDefinition("No rush save tie direction 6 distance 5 goal", false, 6, 4, 4, null, 6, 5, HeaderAtGoalOutcomeExpectation.Goal),
            new HeaderAtGoalBranchDefinition("No rush save handling spill direction 5 corner north", false, 6, 4, 6, 6, 5, null, HeaderAtGoalOutcomeExpectation.CornerKick, new Vector2Int(18, 12)),
            new HeaderAtGoalBranchDefinition("No rush save handling spill direction 1 corner south", false, 6, 4, 6, 6, 1, null, HeaderAtGoalOutcomeExpectation.CornerKick, new Vector2Int(18, -12)),
            new HeaderAtGoalBranchDefinition("No rush save handling spill direction 2 distance 1 Kuzmic misses movement", false, 6, 4, 6, 6, 2, 1, HeaderAtGoalOutcomeExpectation.MovementPhase, looseInterceptionPlayerName: "Kuzmic", looseInterceptionRoll: 2),
            new HeaderAtGoalBranchDefinition("No rush save handling hold", false, 6, 4, 6, 1, null, null, HeaderAtGoalOutcomeExpectation.SaveAndHold, new Vector2Int(16, 0)),
            new HeaderAtGoalBranchDefinition("Rush aerial 6 GK wins challenge save and hold no handling", true, 6, 4, 6, null, null, null, HeaderAtGoalOutcomeExpectation.SaveAndHold, new Vector2Int(15, 2)),
            new HeaderAtGoalBranchDefinition("Rush aerial 5 direction 6 distance 6 goal", true, 6, 4, 5, null, 6, 6, HeaderAtGoalOutcomeExpectation.Goal),
            new HeaderAtGoalBranchDefinition("Rush aerial 5 direction 5 distance 6 corner north", true, 6, 4, 5, null, 5, 6, HeaderAtGoalOutcomeExpectation.CornerKick, new Vector2Int(18, 12)),
            new HeaderAtGoalBranchDefinition("Rush aerial 5 direction 2 distance 1 movement no Kuzmic interception", true, 6, 4, 5, null, 2, 1, HeaderAtGoalOutcomeExpectation.MovementPhase, forbiddenLooseInterceptorName: "Kuzmic"),
            new HeaderAtGoalBranchDefinition("Rush aerial 5 direction 2 distance 2 movement", true, 6, 4, 5, null, 2, 2, HeaderAtGoalOutcomeExpectation.MovementPhase, new Vector2Int(12, 1)),
            new HeaderAtGoalBranchDefinition("Rush aerial 4 goal", true, 6, 4, 4, null, null, null, HeaderAtGoalOutcomeExpectation.Goal),
        };
    }

}
