using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace CounterAttack.Online
{
    public static class OnlineSpectatorPregameWire
    {
        public static bool TryEncode(string matchId, long sequence, GameSettings initial, OnlineSetupState setup,
            OnlineDraftState draft, out byte[] payload)
        {
            payload = null;
            if (!OnlineProtocolIds.IsValid(matchId) || sequence < 1)
                return false;
            var settings = draft?.settings ?? setup?.settings ?? initial;
            if (settings == null)
                return false;

            return OnlineSpectatorRoomWire.TryEncodePublication(new
            {
                version = OnlineSpectatorRoomWire.Version,
                matchId,
                sequence,
                stage = draft != null ? "draft" : setup != null ? "setup" : "lobby",
                teams = new
                {
                    home = new { name = settings.homeTeamName, kit = settings.homeKit, keeperKit = settings.homeGKKit },
                    away = new { name = settings.awayTeamName, kit = settings.awayKit, keeperKit = settings.awayGKKit }
                },
                settings = setup != null || draft != null ? new
                {
                    settings.halfDuration, settings.numberOfHalfs, settings.tiebreaker, settings.playerAssistance,
                    settings.matchType, settings.squadSize, settings.draft, settings.gkDraft, settings.referee,
                    settings.weatherConditions, settings.fixtureLighting, settings.ballColor,
                    settings.includeTabletopia, settings.includeNonTabletopia, settings.includeInternationals,
                    settings.includeTabletopiaGK, settings.includeNonTabletopiaGK, settings.includeInternationalsGK
                } : null,
                draft = draft == null ? null : new
                {
                    draft.phase,
                    draft.turn, draft.homeRoll, draft.awayRoll, draft.rollRound, draft.batch, draft.totalBatches,
                    draft.openingWinnerIsHome,
                    home = Roster(draft.home),
                    away = Roster(draft.away)
                }
            }, out payload);
        }

        internal static GameSettings ReadSettings(JObject publication)
        {
            var settings = publication?["settings"]?.ToObject<GameSettings>();
            if (settings == null) return null;
            settings.gameMode = ApplicationManager.OnlineGameMode;
            settings.homeTeamControl = settings.awayTeamControl = "Human";
            settings.homeTeamName = (string)publication["teams"]?["home"]?["name"];
            settings.awayTeamName = (string)publication["teams"]?["away"]?["name"];
            settings.homeKit = (string)publication["teams"]?["home"]?["kit"];
            settings.awayKit = (string)publication["teams"]?["away"]?["kit"];
            settings.homeGKKit = (string)publication["teams"]?["home"]?["keeperKit"];
            settings.awayGKKit = (string)publication["teams"]?["away"]?["keeperKit"];
            return settings;
        }

        internal static string SceneForStage(string stage) => stage switch
        {
            "setup" => "CreateNewGameScene",
            "draft" => "Draft",
            _ => null
        };

        private static object Roster(OnlineDraftCard[] cards) =>
            (cards ?? Array.Empty<OnlineDraftCard>()).Select(card => card == null ? null : new
            {
                card.id, card.name, card.country, card.goalkeeper, card.type, card.squadNumber,
                card.pace, card.dribbling, card.heading, card.highPass, card.resilience,
                card.shooting, card.tackling, card.saving, card.handling, card.aerial
            });
    }
}
