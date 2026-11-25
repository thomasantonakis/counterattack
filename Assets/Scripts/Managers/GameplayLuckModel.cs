using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/// <summary>Inputs captured before resolution. Optional, additive save evidence; never changes a roll.</summary>
[Serializable]
public sealed class GameplayLuckModel
{
    public int version = 1;
    public string rule, category, label, side, contestId;
    public int skill, modifier, threshold, naturalSuccess = 7, foulThreshold;
    public bool rawModifier, outsideHands, atGoal;
    public int attackerSkill, defenderSkill, defenderModifier;
    public List<Contender> contenders;

    [Serializable]
    public sealed class Contender
    {
        public string tokenKey;
        public bool attacking;
        public int skill, modifier;
    }

    public static GameplayLuckModel Capture(string context, string side, PlayerToken actor, Dictionary<string, string> details)
    {
        int? Read(string key, int? fallback = null) => details != null && details.TryGetValue(key, out string text)
            && int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : fallback;
        bool Flag(string key) => details != null && details.TryGetValue(key, out string text) && bool.TryParse(text, out bool value) && value;
        GameplayLuckModel Make(string rule, string category, string label, int? skill = 0, int modifier = 0, int threshold = 0)
            => skill.HasValue ? new GameplayLuckModel { rule = rule, category = category, label = label, side = side,
                skill = skill.Value, modifier = modifier, threshold = threshold } : null;
        GameplayLuckModel Recovery(int? skill, int modifier = 0, int natural = 6, int foul = 0)
        {
            var model = Make("recovery", "Recoveries", "Recovery", skill, modifier, 10);
            if (model != null) { model.naturalSuccess = natural; model.foulThreshold = foul; model.rawModifier = true; }
            return model;
        }
        switch (context)
        {
            case "pass_interception": case "long_ball_interception": case "header_interception":
                return Recovery(Read("defenderTackling", actor?.tackling));
            case "ftp_interception":
                return Recovery(Read("defenderTackling", actor?.tackling), natural: Flag("isBlockingPath") ? 5 : 6);
            case "movement_ball_interception":
                return Recovery(Read("defenderTackling", actor?.tackling), foul: Flag("isDribblerRunning") ? 1 : 0);
            case "loose_ball_interception":
                return Recovery(Read("recoveryAttribute"), Read("recoveryModifier") ?? 0);
            case "pass_gk_wall_save": case "ftp_gk_wall_save":
                var wall = Recovery(Read("goalkeeperSaving", actor?.saving), Read("gkPenalty") ?? 0);
                if (wall != null) { wall.rawModifier = false; wall.category = "Goalkeeping"; wall.label = "Pass save"; }
                return wall;
            case "high_pass_accuracy": case "long_ball_accuracy":
                return Read("threshold") is int target ? Make("threshold", "Passing", "Pass accuracy",
                    Read("highPassAttribute", actor?.highPass), threshold: target) : null;
            case "header_control":
                return Make("threshold", "Duels & control", "Ball control", Read("dribbling", actor?.dribbling), Read("controlPenalty") ?? 0, 9);
            case "headed_back_pass_catch":
                return Make("above", "Goalkeeping", "Back-pass catch", threshold: 1);
            case "goalkeeper_handling":
                return Read("goalkeeperHandling", actor?.handling) is int handling
                    ? Make("below", "Goalkeeping", "Handling", threshold: handling) : null;
            case "movement_injury":
                return Read("attackerResilience", actor?.resilience) is int resilience
                    ? Make("below", "Discipline & fitness", "Avoid injury", threshold: resilience) : null;
            case "movement_foul_card_leniency":
                return Read("refereeLeniency") is int leniency
                    ? Make("below", "Discipline & fitness", "Avoid card", threshold: leniency) : null;
            case "shot_block":
                var block = Recovery(Read("defenderTackling", actor?.tackling), natural: Read("requiredNaturalRoll") ?? 6);
                if (block != null) { block.category = "Shooting"; block.label = "Shot block"; }
                return block;
            case "shot_roll":
                return Make("above", "Shooting", "Shot on target", threshold: 1);
            case "header_at_goal_gk_save": case "shot_gk_save":
                int? power = Read(context == "shot_gk_save" ? "totalShotPower" : "attackerTotalScore");
                var save = power.HasValue ? Make("comparison", "Goalkeeping", "Shot save", actor?.saving,
                    Read("gkPenalty") ?? 0, power.Value) : null;
                if (save != null) save.outsideHands = Flag("outsideHands");
                return save;
            default: return null; // Direction, distance and stoppage have no inherently favourable face.
        }
    }

    public static GameplayLuckModel GroundDuel(string id, PlayerToken attacker, int defenderSkill, int defenderModifier,
        int foulThreshold, bool outsideHands) => new()
    {
        rule = "ground_duel", category = "Duels & control", label = "Ground duel", contestId = id,
        side = attacker != null && attacker.isHomeTeam ? "Home" : "Away", attackerSkill = attacker?.dribbling ?? 0,
        defenderSkill = defenderSkill, defenderModifier = defenderModifier, foulThreshold = foulThreshold, outsideHands = outsideHands
    };

    public static GameplayLuckModel AerialContest(string id, string attackingSide, bool atGoal, IEnumerable<Contender> contenders) => new()
    {
        rule = "aerial_contest", category = "Duels & control", label = "Aerial contest", contestId = id,
        side = attackingSide, atGoal = atGoal,
        contenders = contenders.Select(c => new Contender { tokenKey = c.tokenKey, attacking = c.attacking, skill = c.skill, modifier = c.modifier }).ToList()
    };
}
