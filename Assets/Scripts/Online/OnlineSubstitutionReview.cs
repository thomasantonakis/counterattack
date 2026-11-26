using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CounterAttack.Online
{
    [Serializable]
    public struct OnlineSubstitutionOrder
    {
        public int outgoing;
        public int incoming;
        public bool HasSelection => outgoing != 0;
        public bool IsComplete => outgoing != 0 && incoming != 0;
    }

    [Serializable]
    public sealed class OnlineSubstitutionReview
    {
        internal const string Prefix = "roster.review:";
        internal const string OpenAction = Prefix + "open";
        internal const int Capacity = MatchManager.ExtraTimeMaxSubstitutionsPerTeam;
        public long id;
        public OnlineSubstitutionOrder[] home = new OnlineSubstitutionOrder[Capacity];
        public OnlineSubstitutionOrder[] away = new OnlineSubstitutionOrder[Capacity];
        public bool homeConfirmed;
        public bool awayConfirmed;

        public OnlineSubstitutionOrder[] Orders(bool homeTeam) => homeTeam ? home : away;
        public bool Confirmed(bool homeTeam) => homeTeam ? homeConfirmed : awayConfirmed;
        public bool HasOrders(bool homeTeam) => Orders(homeTeam).Any(order => order.HasSelection);

        internal OnlineSubstitutionReview Capture() => new()
        {
            id = id,
            home = (OnlineSubstitutionOrder[])home.Clone(),
            away = (OnlineSubstitutionOrder[])away.Clone(),
            homeConfirmed = homeConfirmed,
            awayConfirmed = awayConfirmed
        };

        internal bool IsValid() => id > 0 && IsValidTeam(home, homeConfirmed) && IsValidTeam(away, awayConfirmed);

        private static bool IsValidTeam(OnlineSubstitutionOrder[] orders, bool confirmed)
        {
            if (orders?.Length != Capacity) return false;
            var outgoing = new HashSet<int>();
            var incoming = new HashSet<int>();
            foreach (var order in orders)
            {
                if (order.outgoing is < 0 or > 18 || order.incoming is < 0 or > 18
                    || !order.HasSelection && order.incoming != 0
                    || order.HasSelection && (!outgoing.Add(order.outgoing) || confirmed && !order.IsComplete)
                    || order.incoming != 0 && (!incoming.Add(order.incoming) || order.outgoing == order.incoming))
                    return false;
            }
            return !confirmed || outgoing.Count > 0;
        }

        internal static string PickAction(long reviewId, int row, int outgoing, int incoming)
            => FormattableString.Invariant($"{Prefix}pick:{reviewId}:{row}:{outgoing}:{incoming}");

        internal static string ReviewAction(long reviewId, bool confirm)
            => FormattableString.Invariant($"{Prefix}{(confirm ? "confirm" : "back")}:{reviewId}");

        internal static OnlineCommandStatus Apply(MatchManager match, OnlineTeam team, string action,
            long nextRevision, ref OnlineSubstitutionReview review)
        {
            bool homeTeam = team == OnlineTeam.Home;
            if (action == OpenAction)
            {
                if (review != null || match.GetSubstitutionsRemaining(homeTeam) == 0) return OnlineCommandStatus.NotReady;
                review = new OnlineSubstitutionReview { id = nextRevision };
                return OnlineCommandStatus.Accepted;
            }

            var parts = action.Split(':');
            if (parts.Length < 3 || !long.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out long reviewId))
                return OnlineCommandStatus.InvalidCommand;
            if (review == null || review.id != reviewId) return OnlineCommandStatus.StaleRevision;
            if (review.Confirmed(homeTeam)) return OnlineCommandStatus.NotReady;

            if (parts[1] == "pick" && parts.Length == 6
                && ParseInt(parts[3], out int row) && ParseInt(parts[4], out int outgoing) && ParseInt(parts[5], out int incoming)
                && action == PickAction(reviewId, row, outgoing, incoming))
                return review.Pick(match, homeTeam, row, outgoing, incoming);

            bool confirm = parts[1] == "confirm";
            if (action != ReviewAction(reviewId, confirm)) return OnlineCommandStatus.InvalidCommand;
            var own = review.Orders(homeTeam);
            if (confirm && (!review.HasOrders(homeTeam) || own.Any(order => order.HasSelection && !order.IsComplete)))
                return OnlineCommandStatus.IllegalAction;
            if (!confirm) Array.Clear(own, 0, own.Length);
            if (homeTeam) review.homeConfirmed = confirm;
            else review.awayConfirmed = confirm;

            if (review.HasOrders(true) && !review.homeConfirmed || review.HasOrders(false) && !review.awayConfirmed)
                return OnlineCommandStatus.Accepted;

            // Validate the complete disjoint batch before moving any player. Teams
            // have separate roster slots and substitution allowances.
            var changes = new List<(PlayerToken outgoing, PlayerToken incoming)>();
            foreach (bool side in new[] { true, false })
            {
                var orders = review.Orders(side).Where(order => order.HasSelection).ToArray();
                if (orders.Length > match.GetSubstitutionsRemaining(side)) throw new InvalidOperationException("Changed substitution allowance during review");
                foreach (var order in orders)
                {
                    var playerOff = FindToken(match, side, order.outgoing);
                    var playerOn = FindToken(match, side, order.incoming);
                    if (!match.CanRegisterSubstitution(playerOff, playerOn, out string error)) throw new InvalidOperationException(error);
                    changes.Add((playerOff, playerOn));
                }
            }
            foreach (var change in changes)
                if (!match.TryApplyOnlineSubstitution(change.outgoing, change.incoming, out string error))
                    throw new InvalidOperationException(error);
            review = null;
            return OnlineCommandStatus.Accepted;
        }

        private OnlineCommandStatus Pick(MatchManager match, bool homeTeam, int row, int outgoing, int incoming)
        {
            if (row < 0 || row >= Capacity || row >= match.GetSubstitutionsRemaining(homeTeam)
                || outgoing is < 0 or > 18 || incoming is < 0 or > 18 || outgoing == 0 && incoming != 0)
                return OnlineCommandStatus.IllegalAction;
            var playerOff = FindToken(match, homeTeam, outgoing);
            var playerOn = FindToken(match, homeTeam, incoming);
            if (outgoing != 0 && (playerOff == null || !playerOff.isPlaying || playerOff.isSentOff || playerOff.GetCurrentHex() == null)
                || incoming != 0 && (playerOn == null || playerOn.isSentOff || !match.CanRegisterSubstitution(playerOff, playerOn, out _)))
                return OnlineCommandStatus.IllegalAction;
            var orders = Orders(homeTeam);
            for (int i = 0; i < orders.Length; i++)
                if (i != row && (outgoing != 0 && orders[i].outgoing == outgoing || incoming != 0 && orders[i].incoming == incoming))
                    return OnlineCommandStatus.IllegalAction;
            orders[row] = new OnlineSubstitutionOrder { outgoing = outgoing, incoming = incoming };
            return OnlineCommandStatus.Accepted;
        }

        private static PlayerToken FindToken(MatchManager match, bool homeTeam, int jersey)
            => match.playerTokenManager.AuthoredOnlineRoster.FirstOrDefault(token => token.isHomeTeam == homeTeam && token.jerseyNumber == jersey);

        private static bool ParseInt(string value, out int number)
            => int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out number);
    }
}
