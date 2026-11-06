using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;

public sealed partial class GoalkeeperBoxEntryFixture
{
    private sealed class LegacyRosterDocument
    {
        public MatchManager.GameSettings gameSettings;
        public MatchManager.Rosters rosters;
        public Dictionary<string, Dictionary<string, int[]>> positions;
    }

    // Static football data only: never deserialize a runtime snapshot, event log,
    // stats or a path into ApplicationManager. Every test receives fresh state.
    private Dictionary<string, PlayerToken> LoadLegacyRoster(int side = 1)
    {
        Board(side);
        var data = JsonConvert.DeserializeObject<LegacyRosterDocument>(
            File.ReadAllText("Assets/Tests/Fixtures/LegacyMatchRoster.json"));
        match.gameData = new MatchManager.GameData { gameSettings = data.gameSettings, rosters = data.rosters };
        match.gameData.gameSettings.gameMode = ApplicationManager.HotSeatGameMode;
        match.refereeLeniency = int.Parse(data.gameSettings.referee[^1].ToString());
        var players = new Dictionary<string, PlayerToken>(System.StringComparer.Ordinal);
        foreach (bool home in new[] { true, false })
        {
            var roster = home ? data.rosters.home : data.rosters.away;
            foreach (var row in roster.OrderBy(r => int.Parse(r.Key)))
            {
                int jersey = int.Parse(row.Key);
                PlayerToken token = jersey == 1 ? home ? homeGK : awayGK
                    : jersey == 2 ? home ? attacker : defender
                    : Token(row.Value.name, home, jersey == 12, jersey);
                token.InitializeAttributesFromRoster(row.Value, jersey);
                token.name = token.playerName;
                token.isAttacker = home;
                if (jersey <= 11)
                {
                    int[] coordinates = data.positions[home ? "home" : "away"][row.Key];
                    Place(token, Hex(coordinates[0] * side, coordinates[1]));
                    token.MarkAsStarter();
                }
                else
                {
                    token.MarkAsBench();
                    match.playerTokenManager.benchTokens.Add(token);
                }
                players.Add(token.playerName, token);
            }
        }
        Assert.That(match.playerTokenManager.allTokens.Count, Is.EqualTo(32));
        Assert.That(match.playerTokenManager.allTokens.Count(p => p.isPlaying), Is.EqualTo(22));
        ball.PlaceAtCell(Hex(0));
        Invoke(grid, "LoadPrecomputedShootingPaths");
        Invoke(grid, "CalculateGoalDistanceAndDangerousTackleCaches");
        match.StartMatch();
        return players;
    }
}
