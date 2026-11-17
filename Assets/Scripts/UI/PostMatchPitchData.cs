using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>Read-only action-boundary occupancy and committed-path projections of one saved match history.</summary>
public static class PostMatchPitchData
{
    public enum Half { All, First, Second, ExtraTime, FirstExtraTime, SecondExtraTime, Regulation }
    public enum Possession { All, InPossession, OutOfPossession }
    public const float PitchLength = 27.5f;
    public const float PitchWidth = 25f * .866f;
    public const float PitchAspectRatio = PitchLength / PitchWidth;

    public static IReadOnlyList<Half> AvailablePeriods(MatchManager.GameData data)
    {
        int configured = Mathf.Clamp(data?.gameSettings?.numberOfHalfs ?? 1, 1, 2);
        int latest = data?.events?.Where(e => e != null && e.currentHalf >= 1 && e.currentHalf <= 4)
            .Select(e => e.currentHalf).DefaultIfEmpty(configured).Max() ?? configured;
        int savedHalf = data?.runtimeSnapshot?.clock?.currentHalf ?? 0;
        if (savedHalf >= 1 && savedHalf <= 4) latest = Mathf.Max(latest, savedHalf);
        var result = new List<Half> { Half.All, Half.First };
        if (latest >= 2) result.Add(Half.Second);
        if (latest >= 3) result.Add(Half.FirstExtraTime);
        if (latest >= 4) result.Add(Half.SecondExtraTime);
        if (latest >= 3) { result.Add(Half.Regulation); result.Add(Half.ExtraTime); }
        return result;
    }

    public static string PeriodLabel(Half half) => half switch
    {
        Half.All => "Full game", Half.First => "1st half", Half.Second => "2nd half",
        Half.FirstExtraTime => "1st half of extra time", Half.SecondExtraTime => "2nd half of extra time",
        Half.Regulation => "Regulation", Half.ExtraTime => "Extra time",
        _ => throw new ArgumentOutOfRangeException(nameof(half)),
    };

    public sealed class Player
    {
        public string Key;
        public string Name;
        public string Side;
        public int Number;
        public readonly List<Vector2> Positions = new();
        public readonly List<Vector2> Movement = new();
        public Vector2 Average => Positions.Count == 0 ? Vector2.zero
            : Positions.Aggregate(Vector2.zero, (sum, position) => sum + position) / Positions.Count;
    }

    public static IReadOnlyList<Player> Build(MatchManager.GameData data, PostMatchLeaderboard.Team team = PostMatchLeaderboard.Team.Both,
        Half half = Half.All, Possession possession = Possession.All)
    {
        if (data?.events == null) return Array.Empty<Player>();
        var players = new Dictionary<string, Player>(StringComparer.Ordinal);
        var seen = new HashSet<int>();
        foreach (var entry in data.events.Where(e => e != null).OrderBy(e => e.sequenceNumber))
        {
            if (entry.sequenceNumber > 0 && !seen.Add(entry.sequenceNumber)) continue;
            int period = entry.currentHalf > 0 ? entry.currentHalf : entry.snapshot?.currentHalf ?? 1;
            if (half == Half.First && period != 1 || half == Half.Second && period != 2
                || half == Half.ExtraTime && period < 3 || half == Half.FirstExtraTime && period != 3
                || half == Half.SecondExtraTime && period != 4 || half == Half.Regulation && period > 2) continue;
            string state = entry.snapshot?.currentState ?? entry.currentGameState ?? string.Empty;
            bool setup = state.IndexOf("Setup", StringComparison.OrdinalIgnoreCase) >= 0
                || state.IndexOf("HalfTime", StringComparison.OrdinalIgnoreCase) >= 0
                || state.IndexOf("Shootout", StringComparison.OrdinalIgnoreCase) >= 0
                || state == "KickOffTakerSelection";
            if (!setup && entry.kind == "snapshot.action_boundary" && entry.snapshot?.tokens != null)
            {
                foreach (var token in entry.snapshot.tokens)
                {
                    if (token == null || !IsPlaying(token.status) || !OnPitch(token.hex)) continue;
                    var player = Get(token.tokenKey);
                    if (player != null && Included(player.Side, entry)) player.Positions.Add(Project(token.hex, data, period));
                }
            }
            else if (entry.kind == "token.move" && entry.movementPath?.countedForDistance == true && entry.movementPath.hexes != null)
            {
                var player = Get(entry.actorTokenKey);
                if (player == null || !Included(player.Side, entry)) continue;
                // The first path element is the origin, not a traversed step.
                foreach (var hex in entry.movementPath.hexes.Skip(1).Where(OnPitch)) player.Movement.Add(Project(hex, data, period));
            }
        }
        // Markers require a real average. Movement-only records remain unavailable rather than fabricating one.
        return players.Values.Where(p => p.Positions.Count > 0).OrderBy(p => p.Side).ThenBy(p => p.Number).ToArray();

        Player Get(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return null;
            var parts = key.Split(':');
            if (parts.Length != 2 || (parts[0] != "Home" && parts[0] != "Away") || !int.TryParse(parts[1], out int number)) return null;
            if (team != PostMatchLeaderboard.Team.Both && parts[0] != team.ToString()) return null;
            if (players.TryGetValue(key, out var existing)) return existing;
            var stats = data.stats?.playerStats?.FirstOrDefault(p => p != null && p.playerKey == key);
            string name = stats?.displayName;
            if (string.IsNullOrWhiteSpace(name)) name = stats?.playerName;
            var roster = parts[0] == "Home" ? data.rosters?.home : data.rosters?.away;
            if (string.IsNullOrWhiteSpace(name) && roster != null && roster.TryGetValue(parts[1], out var rosterPlayer)) name = rosterPlayer?.name;
            var result = new Player { Key = key, Side = parts[0], Number = number, Name = string.IsNullOrWhiteSpace(name) ? $"{parts[0]} #{number}" : name };
            players.Add(key, result);
            return result;
        }

        bool Included(string side, GameplayEvent entry) => possession == Possession.All || entry.attackHasPossession &&
            (possession == Possession.InPossession ? entry.teamInAttack == side : entry.teamInAttack != side);
    }

    private static bool IsPlaying(string status)
    {
        var values = (status ?? string.Empty).Split(',');
        return values.Contains("playing") && !values.Contains("sent_off") && !values.Contains("subbed_off");
    }

    private static bool OnPitch(RoomHexCoordinates hex) => hex != null && Mathf.Abs(hex.x) <= 18 &&
        Mathf.Abs(hex.z + (hex.x % 2 == 0 ? 0f : .5f)) <= 12.5f;

    private static Vector2 Project(RoomHexCoordinates hex, MatchManager.GameData data, int half)
    {
        var point = new Vector2((hex.x + 18f) / 36f, (hex.z + (hex.x % 2 == 0 ? 0f : .5f) + 12.5f) / 25f);
        bool homeRight = (half >= 3 ? data.extraTimeOpeningSetup : data.openingSetup)?.homeAttacksLeftToRight ?? true;
        if (half == 2 || half == 4) homeRight = !homeRight;
        return homeRight ? point : Vector2.one - point;
    }
}
