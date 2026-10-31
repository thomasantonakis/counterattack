using System;
using System.Collections.Generic;

public enum RoomAIFormationRole
{
    Unknown,
    Goalkeeper,
    Fullback,
    CentreBack,
    CentralMidfielder,
    Winger,
    Striker,
    AttackingMidfielder
}

public enum RoomAIAttackingIdentity
{
    Balanced,
    ShooterLed,
    DribblerLed,
    AerialPrimary,
    ShootForCorners
}

[Serializable]
public sealed class RoomAITacticalProfileThresholds
{
    public int elite = 6;
    public int veryGood = 5;
    public int scarcityFallback = 4;
    public int poorGoalkeeperHandling = 3;
}

/// <summary>
/// A role-aware summary of an XI. This is deliberately factual: it identifies
/// the manager's available specialists without deciding a legal game action.
/// </summary>
public sealed class RoomAITeamTacticalProfile
{
    public string team;
    public string formation = "4-2-3-1";
    public RoomAITokenSnapshot goalkeeper;
    public RoomAITokenSnapshot primaryBallCarrier;
    public RoomAITokenSnapshot primaryShooter;
    public RoomAITokenSnapshot primaryHeader;
    public RoomAITokenSnapshot primaryHighPasser;
    public RoomAITokenSnapshot primaryTackler;
    public int effectiveShootingThreshold;
    public int effectiveDribblingThreshold;
    public readonly List<RoomAITokenSnapshot> outfieldPlayers = new();
    public readonly List<RoomAITokenSnapshot> shooters = new();
    public readonly List<RoomAITokenSnapshot> dribblers = new();
    public readonly List<RoomAITokenSnapshot> headers = new();
    public readonly List<RoomAITokenSnapshot> highPassers = new();
    public readonly List<RoomAITokenSnapshot> tacklers = new();
    public readonly List<RoomAITokenSnapshot> fastPlayers = new();
}

/// <summary>
/// The manager's pre-decision matchup notes. These signals are inputs to later
/// multi-action search; they are not a replacement for the gameplay rules.
/// </summary>
public sealed class RoomAIMatchupTacticalProfile
{
    public RoomAITeamTacticalProfile ownTeam;
    public RoomAITeamTacticalProfile opponent;
    public RoomAIAttackingIdentity suggestedAttackingIdentity;
    public bool opponentGoalkeeperHasPoorHandling;
    public bool hasHeadingAdvantageAgainstCentralDefenders;
    public bool hasCornerHeadingDepthAdvantage;
    public int ownTopSixHeadingTotal;
    public int opponentTopSixHeadingTotal;
    public int favorableCornerHeadingMatchups;
    public int bestCornerHeadingAdvantage;
    public RoomAITokenSnapshot bestCornerTarget;
    public readonly List<RoomAICornerHeadingMatchup> cornerHeadingMatchups = new();
    public bool opponentHasEliteDribbler;
    public bool opponentHasEliteHeader;
    public bool opponentHasFastVeryGoodShooter;
    public RoomAITokenSnapshot fastestScoringThreat;
    public RoomAITokenSnapshot primaryShootingThreat;
    public RoomAITokenSnapshot bestRecoveryDefender;
    public int minimumMaterialShootingThreat;
    public int defensiveRunBuffer;
    public int recommendedDefensiveLineAbsoluteX = -1;
    public int recommendedDefensiveLineX;
    public int secondDeepestDefenderLineX;
    public int currentOffsideLineX;
    public bool ballDefinesOffsideLine;
    public RoomAITokenSnapshot deepestDefendingPlayer;
    public RoomAITokenSnapshot offsideLineDefiningPlayer;
    public bool defensiveLineIsDeeperThanRecommended;
    public bool requiresRoleReassignment;
    public readonly List<string> attackingReasons = new();
    public readonly List<string> defensiveThreats = new();
}

public sealed class RoomAICornerHeadingMatchup
{
    public int rank;
    public RoomAITokenSnapshot attacker;
    public RoomAITokenSnapshot defender;
    public int headingAdvantage;
    public bool isFavorable => headingAdvantage > 0;
}

public static class RoomAITacticalProfileBuilder
{
    public static RoomAIMatchupTacticalProfile Build(
        RoomAIDecisionSnapshot snapshot,
        RoomAITacticalProfileThresholds thresholds = null)
    {
        thresholds ??= new RoomAITacticalProfileThresholds();
        if (snapshot == null || string.IsNullOrWhiteSpace(snapshot.expectedTeam))
        {
            return null;
        }

        string opponentTeam = ResolveOpponentTeam(snapshot, snapshot.expectedTeam);
        RoomAIMatchupTacticalProfile matchup = new()
        {
            ownTeam = BuildTeam(snapshot.tokens, snapshot.expectedTeam, thresholds),
            opponent = BuildTeam(snapshot.tokens, opponentTeam, thresholds)
        };

        AssessMatchup(snapshot, matchup, thresholds);
        return matchup;
    }

    public static RoomAIFormationRole ResolveRole(RoomAITokenSnapshot token)
    {
        if (token == null)
        {
            return RoomAIFormationRole.Unknown;
        }

        if (token.isGoalKeeper || token.jerseyNumber == 1)
        {
            return RoomAIFormationRole.Goalkeeper;
        }

        return token.jerseyNumber switch
        {
            2 or 3 => RoomAIFormationRole.Fullback,
            4 or 5 => RoomAIFormationRole.CentreBack,
            6 or 8 => RoomAIFormationRole.CentralMidfielder,
            7 or 11 => RoomAIFormationRole.Winger,
            9 => RoomAIFormationRole.Striker,
            10 => RoomAIFormationRole.AttackingMidfielder,
            _ => RoomAIFormationRole.Unknown
        };
    }

    private static RoomAITeamTacticalProfile BuildTeam(
        IReadOnlyList<RoomAITokenSnapshot> tokens,
        string team,
        RoomAITacticalProfileThresholds thresholds)
    {
        RoomAITeamTacticalProfile profile = new() { team = team ?? string.Empty };
        if (tokens == null || string.IsNullOrWhiteSpace(team))
        {
            return profile;
        }

        foreach (RoomAITokenSnapshot token in tokens)
        {
            if (token == null
                || !token.isPlaying
                || !string.Equals(token.teamSide, team, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (token.isGoalKeeper || ResolveRole(token) == RoomAIFormationRole.Goalkeeper)
            {
                profile.goalkeeper = token;
                continue;
            }

            profile.outfieldPlayers.Add(token);
        }

        profile.effectiveShootingThreshold = ResolveScarcityThreshold(
            profile.outfieldPlayers,
            token => token.shooting,
            thresholds);
        profile.effectiveDribblingThreshold = ResolveScarcityThreshold(
            profile.outfieldPlayers,
            token => token.dribbling,
            thresholds);

        AddQualifiedSorted(
            profile.outfieldPlayers,
            profile.shooters,
            token => token.shooting,
            profile.effectiveShootingThreshold);
        AddQualifiedSorted(
            profile.outfieldPlayers,
            profile.dribblers,
            token => token.dribbling,
            profile.effectiveDribblingThreshold);
        AddQualifiedSorted(profile.outfieldPlayers, profile.headers, token => token.heading, thresholds.veryGood);
        AddQualifiedSorted(profile.outfieldPlayers, profile.highPassers, token => token.highPass, thresholds.veryGood);
        AddQualifiedSorted(profile.outfieldPlayers, profile.tacklers, token => token.tackling, thresholds.veryGood);
        AddQualifiedSorted(profile.outfieldPlayers, profile.fastPlayers, token => token.pace, thresholds.veryGood);

        profile.primaryBallCarrier = FirstOrNull(profile.dribblers);
        profile.primaryShooter = FirstOrNull(profile.shooters);
        profile.primaryHeader = FirstOrNull(profile.headers);
        profile.primaryHighPasser = FirstOrNull(profile.highPassers);
        profile.primaryTackler = FirstOrNull(profile.tacklers);
        return profile;
    }

    private static void AssessMatchup(
        RoomAIDecisionSnapshot snapshot,
        RoomAIMatchupTacticalProfile matchup,
        RoomAITacticalProfileThresholds thresholds)
    {
        RoomAITeamTacticalProfile own = matchup.ownTeam;
        RoomAITeamTacticalProfile opponent = matchup.opponent;
        matchup.opponentGoalkeeperHasPoorHandling = opponent?.goalkeeper != null
            && opponent.goalkeeper.handling <= thresholds.poorGoalkeeperHandling;
        matchup.opponentHasEliteDribbler = HasAttribute(
            opponent?.outfieldPlayers,
            token => token.dribbling >= thresholds.elite);
        matchup.opponentHasEliteHeader = HasAttribute(
            opponent?.outfieldPlayers,
            token => token.heading >= thresholds.elite);
        matchup.opponentHasFastVeryGoodShooter = HasAttribute(
            opponent?.outfieldPlayers,
            token => token.pace >= thresholds.veryGood && token.shooting >= thresholds.veryGood);
        matchup.minimumMaterialShootingThreat = Math.Max(
            thresholds.scarcityFallback,
            own?.goalkeeper?.saving ?? thresholds.scarcityFallback);
        matchup.primaryShootingThreat = BestPlayer(
            opponent?.outfieldPlayers,
            token => token.shooting >= thresholds.scarcityFallback,
            token => token.shooting,
            token => token.pace);
        matchup.fastestScoringThreat = BestPlayer(
            opponent?.outfieldPlayers,
            token => token.shooting >= matchup.minimumMaterialShootingThreat,
            token => token.pace,
            token => token.shooting);
        matchup.bestRecoveryDefender = BestPlayer(
            own?.outfieldPlayers,
            token => IsDefensiveRole(token) || token.tackling >= thresholds.veryGood,
            token => token.pace,
            token => token.tackling);
        matchup.requiresRoleReassignment = HasAttribute(
            own?.outfieldPlayers,
            token => token.wasSubbedOn
                || token.isInjured
                || token.requiresSubstitution
                || ResolveRole(token) == RoomAIFormationRole.Unknown)
            || (own?.outfieldPlayers.Count ?? 0) < 10;
        CalculateDefensiveLine(snapshot, matchup);

        int ownBestHeader = MaximumAttribute(own?.headers, token => token.heading);
        int opponentBestCentralHeader = MaximumAttribute(
            opponent?.outfieldPlayers,
            token => ResolveRole(token) == RoomAIFormationRole.CentreBack ? token.heading : 0);
        matchup.hasHeadingAdvantageAgainstCentralDefenders = ownBestHeader > opponentBestCentralHeader;
        matchup.ownTopSixHeadingTotal = SumTopAttributes(
            own?.outfieldPlayers,
            token => token.heading,
            6);
        matchup.opponentTopSixHeadingTotal = SumTopAttributes(
            opponent?.outfieldPlayers,
            token => token.heading,
            6);
        BuildCornerHeadingMatchups(matchup);

        bool hasVeryGoodShooter = HasAttribute(
            own?.outfieldPlayers,
            token => token.shooting >= thresholds.veryGood);
        bool hasGoodEnoughShooter = HasAttribute(
            own?.outfieldPlayers,
            token => token.shooting >= thresholds.scarcityFallback);
        bool hasVeryGoodDribbler = HasAttribute(
            own?.outfieldPlayers,
            token => token.dribbling >= thresholds.veryGood);
        bool hasAerialTools = own?.primaryHeader != null && own.primaryHighPasser != null;

        if (!hasGoodEnoughShooter && hasAerialTools)
        {
            matchup.suggestedAttackingIdentity = RoomAIAttackingIdentity.AerialPrimary;
            matchup.attackingReasons.Add("no shooter rated 4+; use the high-pass and heading route as the primary finish");
        }
        else if (matchup.opponentGoalkeeperHasPoorHandling
            && matchup.hasCornerHeadingDepthAdvantage)
        {
            matchup.suggestedAttackingIdentity = RoomAIAttackingIdentity.ShootForCorners;
            matchup.attackingReasons.Add(
                $"opponent goalkeeper handling is 3 or lower and attack has "
                + $"{matchup.favorableCornerHeadingMatchups} favorable top-six Heading matchup(s); "
                + $"best edge is +{matchup.bestCornerHeadingAdvantage} for {matchup.bestCornerTarget?.playerName ?? "the target"}");
        }
        else if (hasVeryGoodShooter)
        {
            matchup.suggestedAttackingIdentity = RoomAIAttackingIdentity.ShooterLed;
            matchup.attackingReasons.Add("at least one shooter is rated 5+");
        }
        else if (hasVeryGoodDribbler)
        {
            matchup.suggestedAttackingIdentity = RoomAIAttackingIdentity.DribblerLed;
            matchup.attackingReasons.Add("no 5+ shooter is available, but a 5+ dribbler can carry progression");
        }
        else
        {
            matchup.suggestedAttackingIdentity = RoomAIAttackingIdentity.Balanced;
            matchup.attackingReasons.Add("no specialist route clearly dominates the matchup");
        }

        if (matchup.hasHeadingAdvantageAgainstCentralDefenders && hasAerialTools)
        {
            matchup.attackingReasons.Add("best header has an attribute advantage over the opponent's central defenders");
        }

        if (matchup.opponentHasFastVeryGoodShooter)
        {
            matchup.defensiveThreats.Add("deny service to fast 5+ shooters and keep the box beyond their uninterrupted run");
        }

        if (matchup.recommendedDefensiveLineAbsoluteX >= 0)
        {
            matchup.defensiveThreats.Add(
                $"target a coordinated line at |x|<={matchup.recommendedDefensiveLineAbsoluteX} "
                + $"to keep the box beyond {matchup.fastestScoringThreat.playerName}'s Pace {matchup.fastestScoringThreat.pace} run");
        }

        if (matchup.requiresRoleReassignment)
        {
            matchup.defensiveThreats.Add("a substitute or non-standard jersey is active; rebuild roles instead of inheriting the departed player's job");
        }

        if (matchup.opponentHasEliteDribbler)
        {
            matchup.defensiveThreats.Add("elite dribbler requires an equivalent tackler adjacent and secondary tackling cover nearby");
        }

        if (matchup.opponentHasEliteHeader)
        {
            matchup.defensiveThreats.Add("elite header requires close aerial marking, offside pressure, and coverage of headed-pass targets");
        }
    }

    private static void BuildCornerHeadingMatchups(RoomAIMatchupTacticalProfile matchup)
    {
        matchup.cornerHeadingMatchups.Clear();
        matchup.favorableCornerHeadingMatchups = 0;
        matchup.bestCornerHeadingAdvantage = 0;
        matchup.bestCornerTarget = null;

        List<RoomAITokenSnapshot> attackers = TopPlayersByAttribute(
            matchup.ownTeam?.outfieldPlayers,
            token => token.heading,
            6);
        List<RoomAITokenSnapshot> defenders = TopPlayersByAttribute(
            matchup.opponent?.outfieldPlayers,
            token => token.heading,
            6);
        int pairCount = Math.Min(6, Math.Max(attackers.Count, defenders.Count));
        for (int index = 0; index < pairCount; index++)
        {
            RoomAITokenSnapshot attacker = index < attackers.Count ? attackers[index] : null;
            RoomAITokenSnapshot defender = index < defenders.Count ? defenders[index] : null;
            int advantage = (attacker?.heading ?? 0) - (defender?.heading ?? 0);
            matchup.cornerHeadingMatchups.Add(new RoomAICornerHeadingMatchup
            {
                rank = index + 1,
                attacker = attacker,
                defender = defender,
                headingAdvantage = advantage
            });

            if (attacker == null || advantage <= 0)
            {
                continue;
            }

            matchup.favorableCornerHeadingMatchups++;
            if (matchup.bestCornerTarget == null || advantage > matchup.bestCornerHeadingAdvantage)
            {
                matchup.bestCornerHeadingAdvantage = advantage;
                matchup.bestCornerTarget = attacker;
            }
        }

        matchup.hasCornerHeadingDepthAdvantage = matchup.favorableCornerHeadingMatchups > 0;
    }

    private static string ResolveOpponentTeam(RoomAIDecisionSnapshot snapshot, string ownTeam)
    {
        foreach (RoomAITokenSnapshot token in snapshot.tokens)
        {
            if (token != null
                && token.isPlaying
                && !string.IsNullOrWhiteSpace(token.teamSide)
                && !string.Equals(token.teamSide, ownTeam, StringComparison.OrdinalIgnoreCase))
            {
                return token.teamSide;
            }
        }

        return string.Equals(ownTeam, "Home", StringComparison.OrdinalIgnoreCase) ? "Away" : "Home";
    }

    private static void CalculateDefensiveLine(
        RoomAIDecisionSnapshot snapshot,
        RoomAIMatchupTacticalProfile matchup)
    {
        int ownGoalSide = ResolveOwnGoalSide(snapshot);
        List<RoomAITokenSnapshot> defendingPlayers = new List<RoomAITokenSnapshot>();
        if (matchup?.ownTeam?.goalkeeper != null && matchup.ownTeam.goalkeeper.currentHex != null)
        {
            defendingPlayers.Add(matchup.ownTeam.goalkeeper);
        }

        if (matchup?.ownTeam?.outfieldPlayers != null)
        {
            foreach (RoomAITokenSnapshot player in matchup.ownTeam.outfieldPlayers)
            {
                if (player?.currentHex != null)
                {
                    defendingPlayers.Add(player);
                }
            }
        }

        defendingPlayers.Sort((left, right) =>
        {
            int leftDepth = ownGoalSide * left.currentHex.x;
            int rightDepth = ownGoalSide * right.currentHex.x;
            return rightDepth.CompareTo(leftDepth);
        });
        matchup.deepestDefendingPlayer = defendingPlayers.Count > 0 ? defendingPlayers[0] : null;
        matchup.offsideLineDefiningPlayer = defendingPlayers.Count > 1 ? defendingPlayers[1] : null;
        matchup.secondDeepestDefenderLineX = matchup.offsideLineDefiningPlayer?.currentHex?.x ?? 0;
        matchup.currentOffsideLineX = CalculateEffectiveOffsideLineX(
            ownGoalSide,
            matchup.secondDeepestDefenderLineX,
            snapshot?.ball?.x ?? 0);
        matchup.ballDefinesOffsideLine = snapshot?.ball != null
            && matchup.currentOffsideLineX == snapshot.ball.x
            && matchup.currentOffsideLineX != matchup.secondDeepestDefenderLineX;

        if (matchup?.fastestScoringThreat == null)
        {
            return;
        }

        int recoveryPace = matchup.bestRecoveryDefender?.pace ?? 0;
        matchup.defensiveRunBuffer = recoveryPace > matchup.fastestScoringThreat.pace ? 1 : 2;
        const int penaltyBoxOuterColumn = 12;
        matchup.recommendedDefensiveLineAbsoluteX = Math.Max(
            0,
            penaltyBoxOuterColumn
            - matchup.fastestScoringThreat.pace
            - matchup.defensiveRunBuffer);
        matchup.recommendedDefensiveLineX = ownGoalSide * matchup.recommendedDefensiveLineAbsoluteX;
        matchup.defensiveLineIsDeeperThanRecommended = ownGoalSide != 0
            && ownGoalSide * matchup.secondDeepestDefenderLineX > matchup.recommendedDefensiveLineAbsoluteX;
    }

    private static int CalculateEffectiveOffsideLineX(
        int defendingTeamOwnGoalSide,
        int secondDeepestDefenderX,
        int ballX)
    {
        // Mirror OffsideManager exactly. The opponent attacks toward this team's
        // own goal, and the line can never extend into the attacker's own half.
        return defendingTeamOwnGoalSide < 0
            ? Math.Min(0, Math.Min(ballX, secondDeepestDefenderX))
            : defendingTeamOwnGoalSide > 0
                ? Math.Max(0, Math.Max(ballX, secondDeepestDefenderX))
                : 0;
    }

    private static int ResolveOwnGoalSide(RoomAIDecisionSnapshot snapshot)
    {
        if (snapshot == null)
        {
            return 0;
        }

        string direction = string.Equals(snapshot.expectedTeam, "Home", StringComparison.OrdinalIgnoreCase)
            ? snapshot.homeTeamDirection
            : string.Equals(snapshot.expectedTeam, "Away", StringComparison.OrdinalIgnoreCase)
                ? snapshot.awayTeamDirection
                : string.Empty;
        if (string.Equals(direction, "LeftToRight", StringComparison.OrdinalIgnoreCase))
        {
            return -1;
        }

        return string.Equals(direction, "RightToLeft", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
    }

    private static bool IsDefensiveRole(RoomAITokenSnapshot token)
    {
        RoomAIFormationRole role = ResolveRole(token);
        return role == RoomAIFormationRole.Fullback || role == RoomAIFormationRole.CentreBack;
    }

    private static int ResolveScarcityThreshold(
        IReadOnlyList<RoomAITokenSnapshot> players,
        Func<RoomAITokenSnapshot, int> selector,
        RoomAITacticalProfileThresholds thresholds)
    {
        int maximum = MaximumAttribute(players, selector);
        if (maximum >= thresholds.veryGood)
        {
            return thresholds.veryGood;
        }

        return maximum >= thresholds.scarcityFallback
            ? thresholds.scarcityFallback
            : maximum;
    }

    private static void AddQualifiedSorted(
        IReadOnlyList<RoomAITokenSnapshot> players,
        List<RoomAITokenSnapshot> destination,
        Func<RoomAITokenSnapshot, int> selector,
        int threshold)
    {
        if (players == null || destination == null || selector == null || threshold <= 0)
        {
            return;
        }

        foreach (RoomAITokenSnapshot player in players)
        {
            if (player != null && selector(player) >= threshold)
            {
                destination.Add(player);
            }
        }

        destination.Sort((left, right) =>
        {
            int attributeComparison = selector(right).CompareTo(selector(left));
            return attributeComparison != 0
                ? attributeComparison
                : left.jerseyNumber.CompareTo(right.jerseyNumber);
        });
    }

    private static RoomAITokenSnapshot FirstOrNull(List<RoomAITokenSnapshot> players)
    {
        return players != null && players.Count > 0 ? players[0] : null;
    }

    private static RoomAITokenSnapshot BestPlayer(
        IReadOnlyList<RoomAITokenSnapshot> players,
        Predicate<RoomAITokenSnapshot> qualifies,
        Func<RoomAITokenSnapshot, int> primary,
        Func<RoomAITokenSnapshot, int> secondary)
    {
        RoomAITokenSnapshot best = null;
        if (players == null || qualifies == null || primary == null || secondary == null)
        {
            return null;
        }

        foreach (RoomAITokenSnapshot player in players)
        {
            if (player == null || !qualifies(player))
            {
                continue;
            }

            if (best == null
                || primary(player) > primary(best)
                || (primary(player) == primary(best) && secondary(player) > secondary(best))
                || (primary(player) == primary(best)
                    && secondary(player) == secondary(best)
                    && player.jerseyNumber < best.jerseyNumber))
            {
                best = player;
            }
        }

        return best;
    }

    private static bool HasAttribute(
        IReadOnlyList<RoomAITokenSnapshot> players,
        Predicate<RoomAITokenSnapshot> predicate)
    {
        if (players == null || predicate == null)
        {
            return false;
        }

        foreach (RoomAITokenSnapshot player in players)
        {
            if (player != null && predicate(player))
            {
                return true;
            }
        }

        return false;
    }

    private static int MaximumAttribute(
        IReadOnlyList<RoomAITokenSnapshot> players,
        Func<RoomAITokenSnapshot, int> selector)
    {
        int maximum = 0;
        if (players == null || selector == null)
        {
            return maximum;
        }

        foreach (RoomAITokenSnapshot player in players)
        {
            if (player != null)
            {
                maximum = Math.Max(maximum, selector(player));
            }
        }

        return maximum;
    }

    private static int SumTopAttributes(
        IReadOnlyList<RoomAITokenSnapshot> players,
        Func<RoomAITokenSnapshot, int> selector,
        int count)
    {
        if (players == null || selector == null || count <= 0)
        {
            return 0;
        }

        List<int> values = new List<int>();
        foreach (RoomAITokenSnapshot player in players)
        {
            if (player != null)
            {
                values.Add(selector(player));
            }
        }

        values.Sort((left, right) => right.CompareTo(left));
        int total = 0;
        for (int index = 0; index < Math.Min(count, values.Count); index++)
        {
            total += values[index];
        }

        return total;
    }

    private static List<RoomAITokenSnapshot> TopPlayersByAttribute(
        IReadOnlyList<RoomAITokenSnapshot> players,
        Func<RoomAITokenSnapshot, int> selector,
        int count)
    {
        List<RoomAITokenSnapshot> result = new List<RoomAITokenSnapshot>();
        if (players == null || selector == null || count <= 0)
        {
            return result;
        }

        foreach (RoomAITokenSnapshot player in players)
        {
            if (player != null)
            {
                result.Add(player);
            }
        }

        result.Sort((left, right) =>
        {
            int attributeComparison = selector(right).CompareTo(selector(left));
            return attributeComparison != 0
                ? attributeComparison
                : left.jerseyNumber.CompareTo(right.jerseyNumber);
        });
        if (result.Count > count)
        {
            result.RemoveRange(count, result.Count - count);
        }

        return result;
    }
}
