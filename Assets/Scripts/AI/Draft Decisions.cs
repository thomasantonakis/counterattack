using System.Collections.Generic;
using System.Linq;
using System.Text;

public static class DraftDecisions
{
    private const string HomeRosterPanelName = "HomeRoster";
    private const string AwayRosterPanelName = "AwayRoster";
    private const float SophisticatedOverallQualityWeight = 0.1f;

    public enum DraftProfile
    {
        Greedy,
        Sophisticated
    }

    public enum StarterRole
    {
        Fullback,
        Centerback,
        CentralMidfielder,
        Winger,
        AttackingMidfielder,
        Striker
    }

    public sealed class DraftContext
    {
        public DraftContext(
            IEnumerable<Player> visiblePlayers,
            IEnumerable<Player> rosterPlayers,
            string team,
            int currentBatchNumber,
            int totalBatchCount,
            int picksLeftInBatch,
            int remainingUndealtPlayers)
        {
            VisiblePlayers = (visiblePlayers ?? Enumerable.Empty<Player>()).Where(player => player != null).ToList();
            RosterPlayers = (rosterPlayers ?? Enumerable.Empty<Player>()).Where(player => player != null).ToList();
            Team = team;
            CurrentBatchNumber = currentBatchNumber;
            TotalBatchCount = totalBatchCount;
            PicksLeftInBatch = picksLeftInBatch;
            RemainingUndealtPlayers = remainingUndealtPlayers;
        }

        public IReadOnlyList<Player> VisiblePlayers { get; }
        public IReadOnlyList<Player> RosterPlayers { get; }
        public string Team { get; }
        public int CurrentBatchNumber { get; }
        public int TotalBatchCount { get; }
        public int PicksLeftInBatch { get; }
        public int RemainingUndealtPlayers { get; }
    }

    public sealed class StarterAssignment
    {
        public StarterAssignment(Player player, StarterRole role, string roleName, int jerseyNumber, float roleScore)
        {
            Player = player;
            Role = role;
            RoleName = roleName;
            JerseyNumber = jerseyNumber;
            RoleScore = roleScore;
        }

        public Player Player { get; }
        public StarterRole Role { get; }
        public string RoleName { get; }
        public int JerseyNumber { get; }
        public float RoleScore { get; }
    }

    public sealed class SophisticatedCandidateScore
    {
        public SophisticatedCandidateScore(
            Player player,
            string projectedRoleName,
            float roleScore,
            float starterImpact,
            float gapOrBenchValue,
            float overallQualityAdjustment,
            float urgencyAdjustment,
            float finalScore,
            bool projectedStarter,
            string reason)
        {
            Player = player;
            ProjectedRoleName = projectedRoleName;
            RoleScore = roleScore;
            StarterImpact = starterImpact;
            GapOrBenchValue = gapOrBenchValue;
            OverallQualityAdjustment = overallQualityAdjustment;
            UrgencyAdjustment = urgencyAdjustment;
            FinalScore = finalScore;
            ProjectedStarter = projectedStarter;
            Reason = reason;
        }

        public Player Player { get; }
        public string ProjectedRoleName { get; }
        public float RoleScore { get; }
        public float StarterImpact { get; }
        public float GapOrBenchValue { get; }
        public float OverallQualityAdjustment { get; }
        public float UrgencyAdjustment { get; }
        public float FinalScore { get; }
        public bool ProjectedStarter { get; }
        public string Reason { get; }
    }

    public readonly struct DraftAction
    {
        public DraftAction(DraftProfile profile, Player selectedPlayer, string team, string rosterPanelName)
        {
            Profile = profile;
            SelectedPlayer = selectedPlayer;
            Team = team;
            RosterPanelName = rosterPanelName;
        }

        public DraftProfile Profile { get; }
        public Player SelectedPlayer { get; }
        public string Team { get; }
        public string RosterPanelName { get; }
        public bool IsValid => SelectedPlayer != null && !string.IsNullOrEmpty(RosterPanelName);
    }

    private readonly struct FormationSlot
    {
        public FormationSlot(int index, StarterRole role, string roleName, int jerseyNumber)
        {
            Index = index;
            Role = role;
            RoleName = roleName;
            JerseyNumber = jerseyNumber;
        }

        public int Index { get; }
        public StarterRole Role { get; }
        public string RoleName { get; }
        public int JerseyNumber { get; }
    }

    private sealed class FormationPlan
    {
        public FormationPlan(float score, List<StarterAssignment> assignments)
        {
            Score = score;
            Assignments = assignments;
        }

        public float Score { get; }
        public List<StarterAssignment> Assignments { get; }
    }

    private static readonly FormationSlot[] FormationSlots =
    {
        new FormationSlot(0, StarterRole.Fullback, "FB", 2),
        new FormationSlot(1, StarterRole.Fullback, "FB", 3),
        new FormationSlot(2, StarterRole.Centerback, "CB", 4),
        new FormationSlot(3, StarterRole.Centerback, "CB", 5),
        new FormationSlot(4, StarterRole.CentralMidfielder, "CM/DM", 6),
        new FormationSlot(5, StarterRole.Winger, "Winger", 7),
        new FormationSlot(6, StarterRole.CentralMidfielder, "CM/DM", 8),
        new FormationSlot(7, StarterRole.Striker, "ST", 9),
        new FormationSlot(8, StarterRole.AttackingMidfielder, "AMC", 10),
        new FormationSlot(9, StarterRole.Winger, "Winger", 11)
    };

    public static DraftAction ChooseOutfielder(DraftProfile profile, IEnumerable<Player> visiblePlayers, string team)
    {
        switch (profile)
        {
            case DraftProfile.Sophisticated:
                return ChooseSophisticatedOutfielder(new DraftContext(visiblePlayers, null, team, 0, 0, 0, 0));
            case DraftProfile.Greedy:
                return ChooseGreedyOutfielder(visiblePlayers, team);
            default:
                return default;
        }
    }

    public static DraftAction ChooseOutfielder(DraftProfile profile, DraftContext context)
    {
        switch (profile)
        {
            case DraftProfile.Sophisticated:
                return ChooseSophisticatedOutfielder(context);
            case DraftProfile.Greedy:
                return ChooseGreedyOutfielder(context?.VisiblePlayers, context?.Team);
            default:
                return default;
        }
    }

    public static string DescribeOutfielderDecision(DraftProfile profile, DraftContext context, Player selectedPlayer)
    {
        switch (profile)
        {
            case DraftProfile.Sophisticated:
                return DescribeSophisticatedOutfielderDecision(context, selectedPlayer);
            case DraftProfile.Greedy:
                return DescribeGreedyOutfielderDecision(context?.VisiblePlayers, selectedPlayer);
            default:
                return "No draft profile description available.";
        }
    }

    public static string DescribeGreedyOutfielderDecision(IEnumerable<Player> visiblePlayers, Player selectedPlayer)
    {
        string availablePlayers = string.Join(", ", (visiblePlayers ?? Enumerable.Empty<Player>())
            .Where(player => player != null)
            .Select(FormatGreedyOutfielderScore));
        string selectedName = selectedPlayer != null ? selectedPlayer.Name : "no player";

        return $"Available players: {availablePlayers}. Choosing {selectedName} due to Greedy profile.";
    }

    public static List<StarterAssignment> BuildSophisticatedStarterAssignments(IEnumerable<Player> rosterPlayers)
    {
        List<Player> availablePlayers = (rosterPlayers ?? Enumerable.Empty<Player>())
            .Where(player => player != null)
            .ToList();
        Dictionary<string, FormationPlan> memo = new Dictionary<string, FormationPlan>();
        FormationPlan bestPlan = BuildBestFormationPlan(availablePlayers, 0, 0, memo);

        return bestPlan.Assignments
            .OrderBy(assignment => assignment.JerseyNumber)
            .ToList();
    }

    public static string DescribeSophisticatedStarterAssignments(IEnumerable<Player> rosterPlayers)
    {
        List<StarterAssignment> assignments = BuildSophisticatedStarterAssignments(rosterPlayers);
        if (assignments.Count == 0)
        {
            return "No Sophisticated starter assignment is available yet.";
        }

        return string.Join(", ", assignments.Select(assignment =>
            $"{assignment.JerseyNumber}:{assignment.RoleName}={assignment.Player.Name}({FormatScore(assignment.RoleScore)})"));
    }

    public static string DescribeSophisticatedFormationDecision(IEnumerable<Player> rosterPlayers)
    {
        List<Player> players = (rosterPlayers ?? Enumerable.Empty<Player>())
            .Where(player => player != null)
            .OrderBy(player => player.Name)
            .ToList();
        List<StarterAssignment> assignments = BuildSophisticatedStarterAssignments(players);
        HashSet<Player> starterPlayers = new HashSet<Player>(assignments.Select(assignment => assignment.Player));
        float formationTotal = assignments.Sum(assignment => assignment.RoleScore);

        if (players.Count == 0)
        {
            return "Sophisticated XI arrangement unavailable: roster has no outfield players yet.";
        }

        StringBuilder builder = new StringBuilder();
        builder.Append("Sophisticated XI arrangement");
        builder.Append($" | rosterOutfielders={players.Count}");
        builder.Append($" | startersAssigned={assignments.Count}/10");
        builder.Append($" | formationTotal={FormatScore(formationTotal)}");
        builder.AppendLine();

        foreach (StarterAssignment assignment in assignments.OrderBy(assignment => assignment.JerseyNumber))
        {
            builder.Append("  ");
            builder.Append($"{assignment.JerseyNumber}:{assignment.RoleName} selected {assignment.Player.Name}");
            builder.Append($" roleScore={FormatScore(assignment.RoleScore)}");
            builder.Append(" alternatives=");
            builder.Append(FormatSlotAlternatives(players, assignment.JerseyNumber, assignment.Player));
            builder.AppendLine();
        }

        List<Player> benchPlayers = players
            .Where(player => !starterPlayers.Contains(player))
            .OrderByDescending(player => GetRoleScore(player, GetBestRole(player)))
            .ThenBy(player => player.Name)
            .ToList();

        if (benchPlayers.Count == 0)
        {
            builder.Append("  Bench: none yet.");
            return builder.ToString();
        }

        builder.Append("  Bench:");
        foreach (Player benchPlayer in benchPlayers)
        {
            StarterRole bestRole = GetBestRole(benchPlayer);
            builder.Append($" {benchPlayer.Name}");
            builder.Append($" bestRole={GetRoleName(bestRole)}");
            builder.Append($" bestRoleScore={FormatScore(GetRoleScore(benchPlayer, bestRole))}");
            builder.Append("; ");
        }

        builder.Append("benched because the optimized 4-2-3-1 XI had higher role scores in the filled starter slots.");
        return builder.ToString();
    }

    private static DraftAction ChooseGreedyOutfielder(IEnumerable<Player> visiblePlayers, string team)
    {
        Player selectedPlayer = (visiblePlayers ?? Enumerable.Empty<Player>())
            .Where(player => player != null)
            .OrderByDescending(GetGreedyOutfielderScore)
            .ThenBy(player => player.Name)
            .FirstOrDefault();

        if (selectedPlayer == null)
        {
            return default;
        }

        return new DraftAction(DraftProfile.Greedy, selectedPlayer, team, GetRosterPanelName(team));
    }

    private static DraftAction ChooseSophisticatedOutfielder(DraftContext context)
    {
        SophisticatedCandidateScore selectedScore = GetSophisticatedCandidateScores(context)
            .OrderByDescending(score => score.FinalScore)
            .ThenByDescending(score => score.StarterImpact)
            .ThenByDescending(score => score.RoleScore)
            .ThenBy(score => score.Player.Name)
            .FirstOrDefault();

        if (selectedScore == null)
        {
            return default;
        }

        return new DraftAction(DraftProfile.Sophisticated, selectedScore.Player, context.Team, GetRosterPanelName(context.Team));
    }

    private static string DescribeSophisticatedOutfielderDecision(DraftContext context, Player selectedPlayer)
    {
        if (context == null)
        {
            return "Sophisticated draft scoring unavailable: draft context was not provided.";
        }

        List<SophisticatedCandidateScore> scores = GetSophisticatedCandidateScores(context)
            .OrderByDescending(score => score.FinalScore)
            .ThenByDescending(score => score.StarterImpact)
            .ThenByDescending(score => score.RoleScore)
            .ThenBy(score => score.Player.Name)
            .ToList();

        SophisticatedCandidateScore selectedScore = scores.FirstOrDefault(score => score.Player == selectedPlayer)
            ?? scores.FirstOrDefault(score => selectedPlayer != null && score.Player.Name == selectedPlayer.Name);

        StringBuilder builder = new StringBuilder();
        builder.Append("Sophisticated draft scoring");
        builder.Append($" | team={context.Team}");
        builder.Append($" | batch={context.CurrentBatchNumber}/{context.TotalBatchCount}");
        builder.Append($" | picksLeftInBatch={context.PicksLeftInBatch}");
        builder.Append($" | undealt={context.RemainingUndealtPlayers}");
        builder.AppendLine();
        builder.AppendLine(DescribeSophisticatedTeamStatus(context.RosterPlayers));
        builder.Append("Available players: ");
        builder.AppendLine(string.Join(", ", context.VisiblePlayers.Select(FormatPlayerAttributes)));

        foreach (SophisticatedCandidateScore score in scores)
        {
            builder.Append("  ");
            builder.Append(FormatPlayerAttributes(score.Player));
            builder.Append($" role={score.ProjectedRoleName}");
            builder.Append($" roleScore={FormatScore(score.RoleScore)}");
            builder.Append($" scoreFactors={FormatRoleScoreFactors(score.Player, score.ProjectedRoleName)}");
            builder.Append($" starterImpact={FormatScore(score.StarterImpact)}");
            builder.Append($" gapOrBench={FormatScore(score.GapOrBenchValue)}");
            builder.Append($" overallQuality={FormatScore(score.OverallQualityAdjustment)}");
            builder.Append($" urgency={FormatScore(score.UrgencyAdjustment)}");
            builder.Append($" final={FormatScore(score.FinalScore)}");
            builder.Append($" reason={score.Reason}");
            builder.AppendLine();
        }

        if (selectedScore != null)
        {
            builder.Append($"Sophisticated selected {selectedScore.Player.Name}: {selectedScore.Reason}; highest final score {FormatScore(selectedScore.FinalScore)}.");
            List<SophisticatedCandidateScore> alternatives = scores
                .Where(score => score.Player != selectedScore.Player)
                .Take(3)
                .ToList();
            if (alternatives.Count > 0)
            {
                builder.AppendLine();
                builder.Append("Selected over: ");
                builder.Append(string.Join("; ", alternatives.Select(score =>
                    $"{score.Player.Name} by +{FormatScore(selectedScore.FinalScore - score.FinalScore)} final score ({selectedScore.ProjectedRoleName} {FormatScore(selectedScore.RoleScore)} vs {score.ProjectedRoleName} {FormatScore(score.RoleScore)})")));
                builder.Append(".");
            }
        }
        else
        {
            builder.Append("Sophisticated could not select a player.");
        }

        return builder.ToString();
    }

    private static List<SophisticatedCandidateScore> GetSophisticatedCandidateScores(DraftContext context)
    {
        List<SophisticatedCandidateScore> scores = new List<SophisticatedCandidateScore>();
        if (context == null)
        {
            return scores;
        }

        List<Player> currentRoster = context.RosterPlayers.ToList();
        float currentStarterTotal = GetFormationTotal(currentRoster);

        foreach (Player candidate in context.VisiblePlayers)
        {
            List<Player> projectedRoster = currentRoster.Concat(new[] { candidate }).ToList();
            List<StarterAssignment> projectedAssignments = BuildSophisticatedStarterAssignments(projectedRoster);
            StarterAssignment candidateAssignment = projectedAssignments.FirstOrDefault(assignment => assignment.Player == candidate);
            bool projectedStarter = candidateAssignment != null;
            StarterRole bestRole = projectedStarter ? candidateAssignment.Role : GetBestRole(candidate);
            string roleName = projectedStarter ? candidateAssignment.RoleName : GetRoleName(bestRole);
            float roleScore = projectedStarter ? candidateAssignment.RoleScore : GetRoleScore(candidate, bestRole);
            float projectedStarterTotal = projectedAssignments.Sum(assignment => assignment.RoleScore);
            float starterImpact = projectedStarterTotal - currentStarterTotal;
            float gapOrBenchValue = GetGapOrBenchValue(context, projectedStarter, roleScore);
            float overallQualityAdjustment = GetOverallQualityAdjustment(candidate);
            float urgencyAdjustment = GetUrgencyAdjustment(context, currentRoster.Count, projectedStarter, starterImpact);
            float finalScore = starterImpact + gapOrBenchValue + overallQualityAdjustment + urgencyAdjustment;
            string reason = GetSophisticatedReason(projectedStarter, starterImpact, roleName, currentRoster.Count);

            scores.Add(new SophisticatedCandidateScore(
                candidate,
                roleName,
                roleScore,
                starterImpact,
                gapOrBenchValue,
                overallQualityAdjustment,
                urgencyAdjustment,
                finalScore,
                projectedStarter,
                reason));
        }

        return scores;
    }

    private static string DescribeSophisticatedTeamStatus(IReadOnlyList<Player> rosterPlayers)
    {
        List<Player> players = (rosterPlayers ?? new List<Player>())
            .Where(player => player != null)
            .ToList();
        List<StarterAssignment> assignments = BuildSophisticatedStarterAssignments(players);
        HashSet<Player> starterPlayers = new HashSet<Player>(assignments.Select(assignment => assignment.Player));
        List<Player> benchPlayers = players
            .Where(player => !starterPlayers.Contains(player))
            .OrderBy(player => player.Name)
            .ToList();

        StringBuilder builder = new StringBuilder();
        builder.Append("Team status before pick");
        builder.Append($" | rosterOutfielders={players.Count}");
        builder.Append($" | startersAssigned={assignments.Count}/10");
        builder.Append($" | starterGaps={UnityEngine.Mathf.Max(0, FormationSlots.Length - assignments.Count)}");
        builder.Append($" | currentFormationTotal={FormatScore(assignments.Sum(assignment => assignment.RoleScore))}");
        builder.AppendLine();

        if (assignments.Count == 0)
        {
            builder.Append("  Current XI: none yet.");
        }
        else
        {
            builder.Append("  Current XI: ");
            builder.Append(string.Join(", ", assignments
                .OrderBy(assignment => assignment.JerseyNumber)
                .Select(assignment => $"{assignment.JerseyNumber}:{assignment.RoleName}={assignment.Player.Name}({FormatScore(assignment.RoleScore)})")));
        }

        builder.AppendLine();
        if (benchPlayers.Count == 0)
        {
            builder.Append("  Current bench outfielders: none.");
        }
        else
        {
            builder.Append("  Current bench outfielders: ");
            builder.Append(string.Join(", ", benchPlayers.Select(player =>
            {
                StarterRole bestRole = GetBestRole(player);
                return $"{player.Name} best={GetRoleName(bestRole)}({FormatScore(GetRoleScore(player, bestRole))})";
            })));
        }

        return builder.ToString();
    }

    private static FormationPlan BuildBestFormationPlan(List<Player> players, int playerIndex, int usedSlotMask, Dictionary<string, FormationPlan> memo)
    {
        if (playerIndex >= players.Count || usedSlotMask == (1 << FormationSlots.Length) - 1)
        {
            return new FormationPlan(0f, new List<StarterAssignment>());
        }

        string memoKey = $"{playerIndex}:{usedSlotMask}";
        if (memo.TryGetValue(memoKey, out FormationPlan cachedPlan))
        {
            return cachedPlan;
        }

        Player player = players[playerIndex];
        FormationPlan bestPlan = BuildBestFormationPlan(players, playerIndex + 1, usedSlotMask, memo);

        foreach (FormationSlot slot in FormationSlots)
        {
            int slotBit = 1 << slot.Index;
            if ((usedSlotMask & slotBit) != 0)
            {
                continue;
            }

            float roleScore = GetSlotScore(player, slot);
            FormationPlan tailPlan = BuildBestFormationPlan(players, playerIndex + 1, usedSlotMask | slotBit, memo);
            float totalScore = roleScore + tailPlan.Score;
            if (totalScore <= bestPlan.Score)
            {
                continue;
            }

            List<StarterAssignment> assignments = new List<StarterAssignment>
            {
                new StarterAssignment(player, slot.Role, slot.RoleName, slot.JerseyNumber, roleScore)
            };
            assignments.AddRange(tailPlan.Assignments);
            bestPlan = new FormationPlan(totalScore, assignments);
        }

        memo[memoKey] = bestPlan;
        return bestPlan;
    }

    private static string FormatSlotAlternatives(List<Player> players, int jerseyNumber, Player selectedPlayer)
    {
        FormationSlot slot = FormationSlots.FirstOrDefault(formationSlot => formationSlot.JerseyNumber == jerseyNumber);
        IEnumerable<string> alternatives = players
            .OrderByDescending(player => GetSlotScore(player, slot))
            .ThenBy(player => player.Name)
            .Take(4)
            .Select(player =>
            {
                string selectedMarker = player == selectedPlayer ? "*" : string.Empty;
                return $"{player.Name}{selectedMarker}:{FormatScore(GetSlotScore(player, slot))}";
            });

        return string.Join(", ", alternatives);
    }

    private static float GetGapOrBenchValue(DraftContext context, bool projectedStarter, float roleScore)
    {
        int currentRosterCount = context.RosterPlayers.Count;
        if (projectedStarter && currentRosterCount < FormationSlots.Length)
        {
            return 10f + ((FormationSlots.Length - currentRosterCount) * 0.5f);
        }

        if (projectedStarter)
        {
            return 4f;
        }

        return roleScore * 0.25f;
    }

    private static float GetUrgencyAdjustment(DraftContext context, int currentRosterCount, bool projectedStarter, float starterImpact)
    {
        int starterGaps = UnityEngine.Mathf.Max(0, FormationSlots.Length - currentRosterCount);
        int remainingBatches = UnityEngine.Mathf.Max(0, context.TotalBatchCount - context.CurrentBatchNumber);
        float urgency = 0f;

        if (projectedStarter && starterGaps > 0)
        {
            urgency += UnityEngine.Mathf.Min(6f, starterGaps * 0.75f);
        }

        if (projectedStarter && starterImpact > 0f && remainingBatches <= 2)
        {
            urgency += 2f;
        }

        if (context.PicksLeftInBatch <= 1 && projectedStarter)
        {
            urgency += 1f;
        }

        return urgency;
    }

    private static float GetOverallQualityAdjustment(Player player)
    {
        return GetOverallOutfielderScore(player) * SophisticatedOverallQualityWeight;
    }

    private static string GetSophisticatedReason(bool projectedStarter, float starterImpact, string roleName, int currentRosterCount)
    {
        if (projectedStarter && currentRosterCount < FormationSlots.Length)
        {
            return $"fills starter structure as {roleName}";
        }

        if (projectedStarter && starterImpact > 0f)
        {
            return $"upgrades projected XI as {roleName}";
        }

        if (projectedStarter)
        {
            return $"preserves formation balance as {roleName}";
        }

        return "adds bench depth after current XI";
    }

    private static float GetFormationTotal(IEnumerable<Player> players)
    {
        return BuildSophisticatedStarterAssignments(players).Sum(assignment => assignment.RoleScore);
    }

    private static StarterRole GetBestRole(Player player)
    {
        return new[]
            {
                StarterRole.Fullback,
                StarterRole.Centerback,
                StarterRole.CentralMidfielder,
                StarterRole.Winger,
                StarterRole.AttackingMidfielder,
                StarterRole.Striker
            }
            .OrderByDescending(role => GetRoleScore(player, role))
            .ThenBy(role => role.ToString())
            .First();
    }

    private static string GetRoleName(StarterRole role)
    {
        switch (role)
        {
            case StarterRole.Fullback:
                return "FB";
            case StarterRole.Centerback:
                return "CB";
            case StarterRole.CentralMidfielder:
                return "CM/DM";
            case StarterRole.Winger:
                return "Winger";
            case StarterRole.AttackingMidfielder:
                return "AMC";
            case StarterRole.Striker:
                return "ST";
            default:
                return role.ToString();
        }
    }

    private static float GetRoleScore(Player player, StarterRole role)
    {
        if (player == null)
        {
            return 0f;
        }

        switch (role)
        {
            case StarterRole.Centerback:
                return (player.Tackling * 3.2f) + (player.Heading * 2.8f) + (player.Pace * 1.4f) + (player.Resilience * 0.9f) + (player.HighPass * 0.4f) + GetCentralDefenderAnchorBonus(player) - GetFragilityPenalty(player, role);
            case StarterRole.Fullback:
                return (player.Pace * 3.0f) + (player.Tackling * 2.8f) + (player.HighPass * 1.8f) + (player.Dribbling * 1.2f) + (player.Resilience * 0.6f) + GetBreakawayPaceBonus(player, role) - GetFragilityPenalty(player, role) - GetDefensiveMismatchPenalty(player, role) - GetFullbackPaceMismatchPenalty(player);
            case StarterRole.CentralMidfielder:
                return (player.HighPass * 2.0f) + (player.Pace * 1.7f) + (player.Tackling * 1.7f) + (player.Dribbling * 1.5f) + (player.Heading * 1.2f) + (player.Resilience * 1.2f) + (player.Shooting * 0.5f) - GetFragilityPenalty(player, role);
            case StarterRole.Winger:
                return (player.Pace * 3.1f) + (player.Dribbling * 2.7f) + (player.HighPass * 2.0f) + (player.Shooting * 0.9f) + (player.Resilience * 0.5f) + GetBreakawayPaceBonus(player, role) + GetAttackingRunnerBonus(player, role) - GetFragilityPenalty(player, role);
            case StarterRole.AttackingMidfielder:
                return (player.Dribbling * 3.3f) + (player.Shooting * 2.0f) + (player.Pace * 1.7f) + (player.HighPass * 1.5f) + (player.Heading * 0.7f) + (player.Resilience * 0.6f) + GetBreakawayPaceBonus(player, role) + GetAttackingRunnerBonus(player, role) - GetFragilityPenalty(player, role);
            case StarterRole.Striker:
                return (player.Shooting * 3.4f) + (player.Heading * 2.5f) + (player.Pace * 1.1f) + (player.Resilience * 1.0f) + (player.Dribbling * 0.7f) + GetBreakawayPaceBonus(player, role) + GetAttackingRunnerBonus(player, role) + GetPoacherBonus(player) - GetFragilityPenalty(player, role);
            default:
                return GetOverallOutfielderScore(player);
        }
    }

    private static float GetSlotScore(Player player, FormationSlot slot)
    {
        float score = GetRoleScore(player, slot.Role);
        if (slot.Role == StarterRole.Centerback)
        {
            // Keep the faster centerback in the cover slot when the same CB pair is selected.
            score += slot.JerseyNumber == 5 ? player.Pace * 0.25f : player.Pace * -0.25f;
        }

        if (slot.Role == StarterRole.Winger)
        {
            score += player.Pace * 0.35f;
        }

        if (slot.Role == StarterRole.AttackingMidfielder)
        {
            // When several attackers can dribble, spend pace wide and let the #10 be the slower creator.
            score -= player.Pace * 0.15f;
        }

        return score;
    }

    private static float GetCentralDefenderAnchorBonus(Player player)
    {
        float bonus = 0f;
        if (player.Tackling >= 5 && player.Pace <= 4)
        {
            bonus += 3f;
        }

        if (player.Tackling >= 5 && player.HighPass >= 5)
        {
            bonus += 1f;
        }

        return bonus;
    }

    private static float GetBreakawayPaceBonus(Player player, StarterRole role)
    {
        if (player.Pace < 6)
        {
            return 0f;
        }

        switch (role)
        {
            case StarterRole.Winger:
                return 5f + (player.Shooting >= 4 ? 4f : 0f) + (player.HighPass >= 4 ? 1f : 0f);
            case StarterRole.Striker:
                return 4f + (player.Shooting >= 4 ? 2.5f : 0f);
            case StarterRole.Fullback:
                return 2f;
            case StarterRole.AttackingMidfielder:
                return player.Shooting >= 4 ? 2f : 0f;
            default:
                return 0f;
        }
    }

    private static float GetAttackingRunnerBonus(Player player, StarterRole role)
    {
        if (player.Pace < 5 || player.Shooting < 5)
        {
            return 0f;
        }

        switch (role)
        {
            case StarterRole.Winger:
                return 4.5f + (player.HighPass >= 5 ? 1f : 0f);
            case StarterRole.Striker:
                return 4f;
            case StarterRole.AttackingMidfielder:
                return 1.5f;
            default:
                return 0f;
        }
    }

    private static float GetDefensiveMismatchPenalty(Player player, StarterRole role)
    {
        if (role != StarterRole.Fullback)
        {
            return 0f;
        }

        float penalty = 0f;
        if (player.Tackling <= 1)
        {
            penalty += 12f;
        }
        else if (player.Tackling == 2)
        {
            penalty += 8f;
        }
        else if (player.Tackling == 3)
        {
            penalty += 2f;
        }

        if (player.Shooting >= 5 && player.Tackling <= 2)
        {
            penalty += 5f;
        }

        return penalty;
    }

    private static float GetFullbackPaceMismatchPenalty(Player player)
    {
        if (player.Pace <= 4)
        {
            return 7f;
        }

        return 0f;
    }

    private static float GetFragilityPenalty(Player player, StarterRole role)
    {
        if (player.Resilience >= 3)
        {
            return 0f;
        }

        bool extremelyFragile = player.Resilience <= 1;
        switch (role)
        {
            case StarterRole.AttackingMidfielder:
                return extremelyFragile ? 8f : 3f;
            case StarterRole.Winger:
                return extremelyFragile ? 5f : 1.5f;
            case StarterRole.CentralMidfielder:
                return extremelyFragile ? 5f : 2f;
            case StarterRole.Fullback:
                return extremelyFragile ? 2f : 0.5f;
            case StarterRole.Centerback:
                return extremelyFragile ? 1f : 0f;
            case StarterRole.Striker:
                return extremelyFragile ? 0.5f : 0f;
            default:
                return 0f;
        }
    }

    private static float GetPoacherBonus(Player player)
    {
        if (player.Shooting < 5)
        {
            return 0f;
        }

        float bonus = 0f;
        if (player.Pace >= 5)
        {
            bonus += 3f;
        }

        if (player.Resilience <= 1)
        {
            bonus += 1.5f;
        }

        return bonus;
    }

    private static string FormatRoleScoreFactors(Player player, string roleName)
    {
        StarterRole role = GetRoleFromName(roleName);
        List<string> factors = new List<string>();
        float paceBonus = GetBreakawayPaceBonus(player, role);
        float attackingRunnerBonus = GetAttackingRunnerBonus(player, role);
        float defensiveMismatchPenalty = GetDefensiveMismatchPenalty(player, role);
        float fullbackPaceMismatchPenalty = role == StarterRole.Fullback ? GetFullbackPaceMismatchPenalty(player) : 0f;
        float centralDefenderAnchorBonus = role == StarterRole.Centerback ? GetCentralDefenderAnchorBonus(player) : 0f;
        float fragilityPenalty = GetFragilityPenalty(player, role);
        float poacherBonus = role == StarterRole.Striker ? GetPoacherBonus(player) : 0f;

        if (paceBonus > 0f)
        {
            factors.Add($"paceBonus+{FormatScore(paceBonus)}");
        }

        if (poacherBonus > 0f)
        {
            factors.Add($"poacherBonus+{FormatScore(poacherBonus)}");
        }

        if (attackingRunnerBonus > 0f)
        {
            factors.Add($"attackingRunner+{FormatScore(attackingRunnerBonus)}");
        }

        if (centralDefenderAnchorBonus > 0f)
        {
            factors.Add($"centralAnchor+{FormatScore(centralDefenderAnchorBonus)}");
        }

        if (defensiveMismatchPenalty > 0f)
        {
            factors.Add($"defensiveMismatch-{FormatScore(defensiveMismatchPenalty)}");
        }

        if (fullbackPaceMismatchPenalty > 0f)
        {
            factors.Add($"widePaceMismatch-{FormatScore(fullbackPaceMismatchPenalty)}");
        }

        if (fragilityPenalty > 0f)
        {
            factors.Add($"fragility-{FormatScore(fragilityPenalty)}");
        }

        return factors.Count == 0 ? "base" : string.Join(",", factors);
    }

    private static StarterRole GetRoleFromName(string roleName)
    {
        switch (roleName)
        {
            case "FB":
                return StarterRole.Fullback;
            case "CB":
                return StarterRole.Centerback;
            case "CM/DM":
                return StarterRole.CentralMidfielder;
            case "Winger":
                return StarterRole.Winger;
            case "AMC":
                return StarterRole.AttackingMidfielder;
            case "ST":
                return StarterRole.Striker;
            default:
                return StarterRole.CentralMidfielder;
        }
    }

    private static string FormatGreedyOutfielderScore(Player player)
    {
        return $"{player.Name}({player.Pace}+{player.Dribbling}+{player.Shooting}+{player.Heading}+{player.Tackling}={GetGreedyOutfielderScore(player)})";
    }

    private static string FormatPlayerAttributes(Player player)
    {
        if (player == null)
        {
            return "null";
        }

        return $"{player.Name}[P{player.Pace} D{player.Dribbling} H{player.Heading} HP{player.HighPass} R{player.Resilience} S{player.Shooting} T{player.Tackling}]";
    }

    private static int GetGreedyOutfielderScore(Player player)
    {
        return player.Pace +
               player.Dribbling +
               player.Shooting +
               player.Heading +
               player.Tackling;
    }

    private static int GetOverallOutfielderScore(Player player)
    {
        return player.Pace +
               player.Dribbling +
               player.Shooting +
               player.Heading +
               player.Tackling +
               player.HighPass +
               player.Resilience;
    }

    private static string GetRosterPanelName(string team)
    {
        return team == "Away" ? AwayRosterPanelName : HomeRosterPanelName;
    }

    private static string FormatScore(float score)
    {
        return score.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
    }
}
