using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace CounterAttack.Online
{
    public sealed class OnlineSpectatorAdmission
    {
        public const int ViewerLimit = 2;
        public const int CodeLength = OnlineFrontendSessionPolicy.LobbyCodeLength;
        internal const string AwayReservationAttribute = "COUNTERATTACK_AWAY_RESERVATION";

        private readonly HashSet<string> viewers = new(StringComparer.Ordinal);
        private readonly byte[] expectedCode;
        private string awayIdentity;

        public OnlineSpectatorAdmission(string savedAwayIdentity = null)
        {
            MatchCode = OnlineFrontendSessionPolicy.GenerateLobbyCode();
            expectedCode = Encoding.ASCII.GetBytes(MatchCode);
            awayIdentity = savedAwayIdentity;
        }

        public string MatchCode { get; }
        public bool ViewingEnabled { get; private set; } = true;
        public int ViewerCount => viewers.Count;

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
        public bool TryAdmitAway(string authenticatedPeer, string code) =>
            TryAdmitAway(authenticatedPeer, code, out _);

        public bool TryAdmitAway(string authenticatedPeer, string code, out bool waitingForViewerLeave)
        {
            waitingForViewerLeave = false;
            if (!ValidRequest(authenticatedPeer, code) || awayIdentity != null && awayIdentity != authenticatedPeer)
                return false;
            waitingForViewerLeave = viewers.Contains(authenticatedPeer);
            if (waitingForViewerLeave)
                return false;
            awayIdentity ??= authenticatedPeer;
            return true;
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
