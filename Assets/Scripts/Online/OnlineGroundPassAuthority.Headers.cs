using System;
using System.Collections.Generic;
using System.Linq;

namespace CounterAttack.Online
{
    public sealed partial class OnlineGroundPassAuthority
    {
        private OnlineHeaderObservation CaptureHeaderObservation()
        {
            var manager=HeaderRules;
            if(manager?.isActivated!=true) return null;
            return new OnlineHeaderObservation
            {
                attackers=manager.attackerWillJump.Where(t=>t!=null).Select(TokenId).ToArray(),
                defenders=manager.defenderWillJump.Where(t=>t!=null).Select(TokenId).ToArray(),
                roller=TokenId(manager.isWaitingForHeaderRoll?manager.tokenRolling:null),
                winner=TokenId(manager.challengeWinner),
                scores=manager.tokenScores.OrderBy(pair=>TokenId(pair.Key),StringComparer.Ordinal)
                    .Select(pair=>new OnlineHeaderScore{actorId=TokenId(pair.Key),roll=pair.Value.roll,total=pair.Value.totalScore}).ToArray()
            };
        }
        public const string HeaderFreeAction = "header.free", HeaderConfirmAction = "header.confirm",
            HeaderRollAction = "header.roll", HeaderPassAction = "header.pass",
            HeaderKeeperHandsAction="header.backpass.hands",HeaderKeeperFeetAction="header.backpass.feet",HeaderKeeperRollAction="header.backpass.roll";
        public const string HeaderInterceptionAction="header.interception";
        public static string HeaderPlayerAction(string id, bool nominated = false) => (nominated ? "header.remove:" : "header.nominate:") + id;
        public static string HeaderTargetAction(int x, int z) => "header.target:" + x + ":" + z;
        public static string HeaderGoalAction(int x, int z) => "header.goal:" + x + ":" + z;

        private void ReadHeaderDecision(OnlineGroundPassState state, List<string> actions, HeaderManager manager)
        {
            var phase = manager.OnlineHeaderStage;
            if (phase == null)
            {
                ReadHeaderControlDecision(state, actions, manager);
                if (manager.OnlineControlStage == "choice") AddHeaderCommand(actions, manager, HeaderFreeAction, OnlineHeaderIntent.FreeHeader);
                return;
            }
            state.stage = "header." + phase;
            var home = manager.IsInstructionExpectingHomeTeam();
            state.owner = home == null ? "None" : home.Value ? "Home" : "Away";
            state.awaitingRoll = phase is "roll" or "backpass.roll" or "interception";
            state.awaitingTarget = phase is "target" or "goal-choice";
            state.selectedPlayer = TokenId(manager.OnlineHeaderActor);
            if (home == null) return;
            if(phase=="interception")AddHeaderCommand(actions,manager,HeaderInterceptionAction,OnlineHeaderIntent.Interception);
            else if(phase=="interception.pending")return;
            else if(phase=="backpass.choice")
            {
                AddHeaderCommand(actions,manager,HeaderKeeperHandsAction,OnlineHeaderIntent.KeeperHands);
                AddHeaderCommand(actions,manager,HeaderKeeperFeetAction,OnlineHeaderIntent.KeeperFeet);
            }
            else if(phase=="backpass.roll")AddHeaderCommand(actions,manager,HeaderKeeperRollAction,OnlineHeaderIntent.KeeperCatch);
            else if(phase=="backpass.pending")return;
            else if (phase is "attackers" or "defenders")
            {
                foreach (var player in manager.OnlineHeaderPlayers)
                    AddHeaderCommand(actions, manager, HeaderPlayerAction(TokenId(player), manager.OnlineHeaderNominated(player)), OnlineHeaderIntent.Nominate, player);
                if (phase == "defenders" || manager.attackerWillJump.Count > 0)
                    AddHeaderCommand(actions, manager, HeaderConfirmAction, OnlineHeaderIntent.Confirm);
            }
            else if (phase == "roll") AddHeaderCommand(actions, manager, HeaderRollAction, OnlineHeaderIntent.Roll);
            else
            {
                if (phase == "goal-choice") AddHeaderCommand(actions, manager, HeaderPassAction, OnlineHeaderIntent.HeadedPass);
                foreach (var hex in manager.OnlineHeaderTargets)
                    AddHeaderCommand(actions, manager, phase == "goal-choice" ? HeaderGoalAction(hex.coordinates.x, hex.coordinates.z)
                        : HeaderTargetAction(hex.coordinates.x, hex.coordinates.z), phase == "goal-choice" ? OnlineHeaderIntent.GoalTarget : OnlineHeaderIntent.PassTarget, target: hex);
            }
        }
        private void AddHeaderCommand(List<string> actions, HeaderManager manager, string id, OnlineHeaderIntent intent, PlayerToken player = null, HexCell target = null)
            => AddAdditional(actions, id, () =>
            {
                if (!manager.TryExecuteOnlineHeader(intent, player, target)) throw new InvalidOperationException("Accepted header decision became unavailable");
            });
    }
}
