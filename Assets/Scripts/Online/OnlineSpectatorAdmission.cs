using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace CounterAttack.Online
{
    public sealed class OnlineSpectatorAdmission
    {
        public const int ViewerLimit = 2;
        public const int CodeLength = 8;
        internal const string AwayReservationAttribute = "COUNTERATTACK_AWAY_RESERVATION";

        private readonly HashSet<string> viewers = new(StringComparer.Ordinal);
        private readonly byte[] expectedCode;
        private string awayIdentity;

        public OnlineSpectatorAdmission(string savedAwayIdentity = null)
        {
            MatchCode = RandomNumberGenerator.GetInt32(100_000_000).ToString("D8", CultureInfo.InvariantCulture);
            expectedCode = Encoding.ASCII.GetBytes(MatchCode);
            awayIdentity = savedAwayIdentity;
        }

        public string MatchCode { get; }
        public bool ViewingEnabled { get; private set; } = true;
        public int ViewerCount => viewers.Count;

        public static bool TryNormalizeCode(string input, out string code)
        {
            code = input?.Trim();
            if (code?.Length == CodeLength)
            {
                foreach (char value in code)
                {
                    if (value is >= '0' and <= '9')
                        continue;
                    code = null;
                    return false;
                }
                return true;
            }
            code = null;
            return false;
        }

        internal static string Lookup(string code)
        {
            using var hash = SHA256.Create();
            return BitConverter.ToString(hash.ComputeHash(Encoding.ASCII.GetBytes("counterattack-match-v1:" + code)))
                .Replace("-", "");
        }
        internal static string MatchIdentity(string code) => "spectator-" + Lookup(code);
        internal static string AwayReservation(string code, string identity) => Lookup(code + ":away:" + identity);

        internal static string ViewerCompatibilitySuffix(OnlineCompatibility compatibility) =>
            ":" + OnlineSpectatorRoomWire.Version + ":" + compatibility.Protocol + ":" + compatibility.Rules + ":" + compatibility.Catalog;

        // Call on the session thread with the identity supplied by the authenticated transport.
        public bool TryAdmitAway(string authenticatedPeer, string code)
        {
            if (!ValidRequest(authenticatedPeer, code) || viewers.Contains(authenticatedPeer))
                return false;
            awayIdentity ??= authenticatedPeer;
            return string.Equals(awayIdentity, authenticatedPeer, StringComparison.Ordinal);
        }

        public bool TryAdmitViewer(string authenticatedPeer, string code)
        {
            return ViewingEnabled && viewers.Count < ViewerLimit && ValidRequest(authenticatedPeer, code)
                && !string.Equals(authenticatedPeer, awayIdentity, StringComparison.Ordinal)
                && viewers.Add(authenticatedPeer);
        }

        internal bool CanResumeViewer(string authenticatedPeer, string code) =>
            ViewingEnabled && viewers.Contains(authenticatedPeer) && ValidRequest(authenticatedPeer, code);

        public void ViewerLeft(string authenticatedPeer) => viewers.Remove(authenticatedPeer);

        public void CloseViewing()
        {
            ViewingEnabled = false;
            viewers.Clear();
        }

        private bool ValidRequest(string peer, string code)
        {
            return !string.IsNullOrEmpty(peer) && code?.Length == CodeLength
                && CryptographicOperations.FixedTimeEquals(expectedCode, Encoding.UTF8.GetBytes(code));
        }
    }
}
