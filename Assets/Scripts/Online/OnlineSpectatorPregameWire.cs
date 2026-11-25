using System;
using System.Linq;

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
                draft = draft == null ? null : new
                {
                    draft.phase,
                    home = Roster(draft.home),
                    away = Roster(draft.away)
                }
            }, out payload);
        }

        private static object Roster(OnlineDraftCard[] cards) =>
            (cards ?? Array.Empty<OnlineDraftCard>()).Select(card => card == null ? null : new
            {
                card.id, card.name, card.country, card.goalkeeper
            });
    }
}
