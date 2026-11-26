using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace CounterAttack.Online
{
    // This is an independent wire contract. Never serialize a manager frame
    // directly or copy a nested manager object that carries choices or commands.
    public static class OnlineSpectatorRoomWire
    {
        public const int Version = 2;
        public const int MaxBytes = 65536;

        public static bool TryEncode(OnlineGroundPassState state, MatchManager.GameSettings settings,
            long sequence, out byte[] payload) =>
            TryEncode(state, settings, state?.matchId, sequence, out payload);

        internal static bool TryEncode(OnlineGroundPassState state, MatchManager.GameSettings settings,
            string spectatorMatchId, long sequence, out byte[] payload)
        {
            payload = null;
            if (state?.catalogId != OnlineFullRoomBootstrap.Catalog || !OnlineProtocolIds.IsValid(spectatorMatchId)
                || sequence < 1 || settings == null)
                return false;

            var publication = new
            {
                version = Version,
                matchId = spectatorMatchId,
                sequence,
                stage = "room",
                home = new { name = settings.homeTeamName, stats = state.homeStats, scorers = Goals(state.homeScorers) },
                away = new { name = settings.awayTeamName, stats = state.awayStats, scorers = Goals(state.awayScorers) },
                settings = new
                {
                    settings.homeKit, settings.awayKit, settings.homeGKKit, settings.awayGKKit,
                    settings.halfDuration, settings.numberOfHalfs, settings.referee, settings.matchType,
                    settings.playerAssistance, settings.weatherConditions, settings.fixtureLighting, settings.ballColor,
                    settings.tiebreaker
                },
                state.homeDirection, state.awayDirection, state.possessionTeam, state.attackHasPossession,
                clock = new { state.half, state.regulationSeconds, running = state.clockRunning,
                    state.halfExpired, state.stoppageDetermined, state.stoppageTotal, state.stoppageRemaining },
                paused = state.isSharedPaused,
                held = state.connectionHeld,
                state.matchComplete,
                ball = state.ballOnBoard ? new { x = state.ballX, z = state.ballZ } : null,
                pose = PublicMotion(state),
                players = (state.tokens ?? Array.Empty<OnlineGroundPassTokenState>())
                    .Where(token => token?.runtime != null)
                    .Select(token => new
                    {
                        token.id,
                        name = token.runtime.playerName,
                        team = token.runtime.teamSide,
                        jersey = token.runtime.jerseyNumber,
                        playing = token.runtime.isPlaying,
                        keeper = token.runtime.isGoalKeeper,
                        booked = token.runtime.isBooked,
                        sentOff = token.runtime.isSentOff,
                        injured = token.runtime.isInjured,
                        token.runtime.wasSubbedOn, token.runtime.wasSubbedOff,
                        token.visible,
                        token.hasRecordedPlayerStats,
                        position = token.onBoard && token.visible ? new { token.x, token.z } : null,
                        attributes = new
                        {
                            token.runtime.pace, token.runtime.dribbling, token.runtime.highPass,
                            token.runtime.resilience, token.runtime.heading, token.runtime.shooting,
                            token.runtime.tackling, token.runtime.aerial, token.runtime.saving, token.runtime.handling
                        },
                        stats = token.playerStats
                    }),
                coin = PublicCoin(state.coin),
                dice = PublicDice(state.rollPanel),
                announcements = state.banners?.events?.Where(entry => entry != null)
                    .Select(entry => new { entry.sequence, entry.kind, entry.detail }),
                shootout = state.shootout == null || state.shootout.selecting ? null : new
                {
                    state.shootout.homeScore, state.shootout.awayScore,
                    state.shootout.homeResults, state.shootout.awayResults, state.shootout.complete, state.shootout.active,
                    winnerHome = state.shootout.complete ? (bool?)state.shootout.winnerIsHome : null
                }
            };

            return TryEncodePublication(publication, out payload);
        }

        private static object PublicMotion(OnlineGroundPassState state)
        {
            var pose = state.roomPose;
            if (pose == null || !state.ShouldStreamPresentation()) return null;
            var visible = (state.tokens ?? Array.Empty<OnlineGroundPassTokenState>())
                .Where(token => token?.onBoard == true && token.visible).Select(token => token.id).ToHashSet();
            return new
            {
                ball = state.ballOnBoard && state.ballMoving ? new { x = pose.ballX, y = pose.ballY, z = pose.ballZ } : null,
                players = state.playerMoving ? pose.players?.Where(player => player != null && visible.Contains(player.id))
                    .Select(player => new { player.id, player.x, player.y, player.z }) : null
            };
        }

        internal static bool TryEncodePublication(object publication, out byte[] payload)
        {
            payload = null;
            var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(publication,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            if (bytes.Length > MaxBytes) return false;
            payload = bytes;
            return true;
        }

        private static object Goals(MatchManager.GoalEvent[] goals) =>
            (goals ?? Array.Empty<MatchManager.GoalEvent>()).Where(goal => goal != null)
                .Select(goal => new { goal.scorer, goal.minute, goal.minuteLabel, goal.isPenalty, goal.assist });

        private static object PublicCoin(OnlineCoinTossSnapshot coin) => coin?.Valid() != true ? null : new
        {
            coin.purpose, coin.stage, coin.homeRoll, coin.awayRoll, coin.choosingHome,
            winnerHome = coin.hasWinner ? (bool?)coin.winnerHome : null
        };

        private static object PublicDice(OnlineRollPanelFrame panel)
        {
            if (panel?.dice == null || panel.diceAnimating?.Length != panel.dice.Length) return null;
            return new
            {
                panel.panel,
                panel.active, panel.duelExpanded, panel.recoveryAttached, panel.attackColumnX,
                faces = panel.dice.Select((value, index) =>
                {
                    bool rolling = panel.diceAnimating[index];
                    return new { rolling, value = rolling ? 0 : value };
                })
            };
        }
    }
}
