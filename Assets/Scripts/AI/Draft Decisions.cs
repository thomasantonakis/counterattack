using System.Collections.Generic;
using System.Linq;

public static class DraftDecisions
{
    public enum DraftProfile
    {
        Greedy
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

    public static DraftAction ChooseOutfielder(DraftProfile profile, IEnumerable<Player> visiblePlayers, string team)
    {
        switch (profile)
        {
            case DraftProfile.Greedy:
                return ChooseGreedyOutfielder(visiblePlayers, team);
            default:
                return default;
        }
    }

    public static string DescribeGreedyOutfielderDecision(IEnumerable<Player> visiblePlayers, Player selectedPlayer)
    {
        string availablePlayers = string.Join(", ", (visiblePlayers ?? Enumerable.Empty<Player>())
            .Where(player => player != null)
            .Select(FormatGreedyOutfielderScore));
        string selectedName = selectedPlayer != null ? selectedPlayer.Name : "no player";

        return $"Available players: {availablePlayers}. Choosing {selectedName} due to greedy profile";
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

        string rosterPanelName = team == "Away" ? "AwayRoster" : "HomeRoster";
        return new DraftAction(DraftProfile.Greedy, selectedPlayer, team, rosterPanelName);
    }

    private static string FormatGreedyOutfielderScore(Player player)
    {
        return $"{player.Name}({player.Pace}+{player.Dribbling}+{player.Shooting}+{player.Heading}+{player.Tackling}={GetGreedyOutfielderScore(player)})";
    }

    private static int GetGreedyOutfielderScore(Player player)
    {
        return player.Pace +
               player.Dribbling +
               player.Shooting +
               player.Heading +
               player.Tackling;
    }
}
