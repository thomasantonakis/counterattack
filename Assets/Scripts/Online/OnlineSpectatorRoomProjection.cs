using System;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace CounterAttack.Online
{
    internal static class OnlineSpectatorRoomProjection
    {
        internal static MatchManager.GameData Bootstrap(JObject snapshot)
        {
            var settings = ReadSettings(snapshot);
            if (settings == null || !OnlineRoomSettings.Valid(OnlineRoomSettings.Capture(settings))) return null;

            var data = new MatchManager.GameData
            {
                gameSettings = settings,
                openingSetup = new MatchOpeningSetup
                {
                    homeAttacksLeftToRight = (string)snapshot["homeDirection"] != "RightToLeft",
                    homeHasFirstHalfKickoff = (string)snapshot["possessionTeam"] != "Away"
                },
                playSessionId = Guid.NewGuid().ToString("N"),
                playSessionStartedUtc = DateTime.UtcNow.ToString("O")
            };
            foreach (var player in ReadPlayers(snapshot))
            {
                var roster = player.runtime.teamSide == "Home" ? data.rosters.home : data.rosters.away;
                var attributes = player.runtime;
                var jersey = attributes.jerseyNumber.ToString(CultureInfo.InvariantCulture);
                roster.Add(jersey, new MatchManager.RosterPlayer
                {
                    name = attributes.playerName, pace = attributes.pace, dribbling = attributes.dribbling,
                    highPass = attributes.highPass, resilience = attributes.resilience, heading = attributes.heading,
                    shooting = attributes.shooting, tackling = attributes.tackling, aerial = attributes.aerial,
                    saving = attributes.saving, handling = attributes.handling
                });
            }
            int size = data.rosters.home.Count;
            if (size is not (16 or 18) || data.rosters.away.Count != size) return null;
            for (int jersey = 1; jersey <= size; jersey++)
            {
                var key = jersey.ToString(CultureInfo.InvariantCulture);
                if (!data.rosters.home.ContainsKey(key) || !data.rosters.away.ContainsKey(key)) return null;
            }
            settings.squadSize = size;
            TeamNameAbbreviator.EnsureAbbreviations(settings);
            return data;
        }

        private static MatchManager.GameSettings ReadSettings(JObject snapshot)
        {
            var settings = snapshot["settings"]?.ToObject<MatchManager.GameSettings>();
            if (settings == null) return null;
            settings.gameMode = ApplicationManager.OnlineGameMode;
            settings.homeTeamControl = settings.awayTeamControl = "Human";
            settings.homeTeamName = (string)snapshot["home"]?["name"];
            settings.awayTeamName = (string)snapshot["away"]?["name"];
            if (settings.referee is not ("Webster - 2" or "Castolo - 3" or "Bakker - 4" or "Read - 5"))
                settings.referee = "Webster - 2";
            return settings;
        }

        internal static OnlineGroundPassState Read(JObject snapshot, PlayerTokenManager tokens)
        {
            var home = snapshot["home"];
            var away = snapshot["away"];
            var clock = snapshot["clock"];
            var ball = snapshot["ball"];
            var settings = ReadSettings(snapshot);
            var state = new OnlineGroundPassState
            {
                stage = "spectating", catalogId = OnlineFullRoomBootstrap.Catalog,
                matchId = (string)snapshot["matchId"], publicationSequence = (long)snapshot["sequence"],
                isSharedPaused = (bool?)snapshot["paused"] == true, connectionHeld = (bool?)snapshot["held"] == true,
                matchComplete = (bool?)snapshot["matchComplete"] == true,
                half = (int?)clock?["half"] ?? 1, regulationSeconds = (float?)clock?["regulationSeconds"] ?? 0,
                clockRunning = (bool?)clock?["running"] == true, halfExpired = (bool?)clock?["halfExpired"] == true,
                stoppageDetermined = (bool?)clock?["stoppageDetermined"] == true,
                stoppageTotal = (int?)clock?["stoppageTotal"] ?? 0, stoppageRemaining = (int?)clock?["stoppageRemaining"] ?? 0,
                homeDirection = (string)snapshot["homeDirection"] == "RightToLeft" ? "RightToLeft" : "LeftToRight",
                awayDirection = (string)snapshot["homeDirection"] == "RightToLeft" ? "LeftToRight" : "RightToLeft",
                possessionTeam = (string)snapshot["possessionTeam"],
                attackHasPossession = (bool?)snapshot["attackHasPossession"] == true,
                homeStats = home?["stats"]?.ToObject<MatchManager.TeamStats>() ?? new(),
                awayStats = away?["stats"]?.ToObject<MatchManager.TeamStats>() ?? new(),
                homeScorers = home?["scorers"]?.ToObject<MatchManager.GoalEvent[]>() ?? Array.Empty<MatchManager.GoalEvent>(),
                awayScorers = away?["scorers"]?.ToObject<MatchManager.GoalEvent[]>() ?? Array.Empty<MatchManager.GoalEvent>(),
                ballOnBoard = ball != null, ballX = (int?)ball?["x"] ?? 0, ballZ = (int?)ball?["z"] ?? 0,
                legalActions = Array.Empty<string>(),
                tokens = ReadPlayers(snapshot),
                roomSettings = settings == null ? null : OnlineRoomSettings.Capture(settings),
                banners = new OnlineRoomBannerPublication
                {
                    events = snapshot["announcements"]?.ToObject<OnlineRoomBannerEvent[]>() ?? Array.Empty<OnlineRoomBannerEvent>()
                },
                coin = snapshot["coin"]?.ToObject<OnlineCoinTossSnapshot>(),
                shootout = ReadShootout(snapshot["shootout"])
            };
            if (state.matchComplete)
                state.gameState = MatchManager.GameState.MatchEnded.ToString();
            foreach (var player in state.tokens)
            {
                if (player.onBoard || tokens == null) continue;
                bool homePlayer = player.runtime.teamSide == "Home";
                var bench = tokens.GetBenchTokenPositionForRestore(homePlayer, Mathf.Max(0, player.runtime.jerseyNumber - 12));
                player.inactiveX = bench.x; player.inactiveY = bench.y; player.inactiveZ = bench.z;
            }
            return state;
        }

        internal static OnlineGroundPassPresentation ReadMotion(JObject snapshot, Vector3 ballPosition)
        {
            var pose = snapshot["pose"];
            if (pose == null) return null;
            var ball = pose["ball"];
            return new OnlineGroundPassPresentation
            {
                frameSequence = (long)snapshot["sequence"],
                publicationSequence = (long)snapshot["sequence"],
                ballX = (float?)ball?["x"] ?? ballPosition.x, ballY = (float?)ball?["y"] ?? ballPosition.y,
                ballZ = (float?)ball?["z"] ?? ballPosition.z,
                players = pose["players"]?.ToObject<OnlinePlayerPresentation[]>()
            };
        }

        private static OnlineShootoutSnapshot ReadShootout(JToken payload)
        {
            if (payload == null) return null;
            var state = payload.ToObject<OnlineShootoutSnapshot>();
            state.homeOrder = state.awayOrder = Array.Empty<string>();
            state.homeResults ??= Array.Empty<bool>();
            state.awayResults ??= Array.Empty<bool>();
            state.homeAttempts = state.homeResults.Length;
            state.awayAttempts = state.awayResults.Length;
            state.winnerIsHome = (bool?)payload["winnerHome"] == true;
            return state;
        }

        private static OnlineGroundPassTokenState[] ReadPlayers(JObject snapshot) =>
            (snapshot["players"] as JArray ?? new JArray()).Select(player =>
            {
                var position = player["position"];
                var runtime = player["attributes"]?.ToObject<RoomTokenSnapshot>() ?? new();
                runtime.playerName = (string)player["name"];
                runtime.teamSide = (string)player["team"];
                runtime.jerseyNumber = (int)player["jersey"];
                runtime.tokenKey = $"{runtime.teamSide}:{runtime.jerseyNumber}";
                runtime.isPlaying = (bool)player["playing"];
                runtime.isGoalKeeper = (bool)player["keeper"];
                runtime.isBooked = (bool)player["booked"];
                runtime.isSentOff = (bool)player["sentOff"];
                runtime.isInjured = (bool)player["injured"];
                runtime.wasSubbedOn = (bool?)player["wasSubbedOn"] == true;
                runtime.wasSubbedOff = (bool?)player["wasSubbedOff"] == true;
                runtime.isAttacker = runtime.teamSide == (string)snapshot["possessionTeam"];
                return new OnlineGroundPassTokenState
                {
                    id = (string)player["id"], runtime = runtime, visible = (bool?)player["visible"] == true,
                    onBoard = position != null, x = (int?)position?["x"] ?? 0, z = (int?)position?["z"] ?? 0,
                    hasRecordedPlayerStats = (bool?)player["hasRecordedPlayerStats"] == true,
                    playerStats = player["stats"]?.ToObject<MatchManager.PlayerStats>() ?? new()
                };
            }).ToArray();
    }
}
