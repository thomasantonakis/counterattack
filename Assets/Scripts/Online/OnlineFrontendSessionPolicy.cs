using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Epic.OnlineServices.P2P;
using UnityEngine;
namespace CounterAttack.Online
{
    public static class OnlineFrontendSessionPolicy
    {
        public const byte Channel=9;
        public const int MaxQueuedPackets=256;
        public const double PeerTimeoutSeconds=15;
        public const int LobbyCodeLength = 8;
        public const OnlineTeam HostTeam = OnlineTeam.Home;
        public const OnlineTeam GuestTeam = OnlineTeam.Away;

        public static OnlineMatchProtocol CreateMatchProtocol(string matchId, string hostAccount, string guestAccount,
            OnlineCompatibility compatibility)
            => new(matchId, hostAccount, guestAccount, hostAccount, compatibility);

        public static RelayControl Route(bool relayOnly)=>relayOnly?RelayControl.ForceRelays:RelayControl.AllowRelays;
        public const string LobbyCodePrompt = "Enter the 8-character Match Code (0–9, A–F) shared by the host.";
        public static bool IsMatchCodeReady(bool viewingEnabled, bool watchLobbyAdvertised) =>
            !viewingEnabled || watchLobbyAdvertised;
        internal const string LobbyCodeAttribute = "COUNTERATTACK_JOIN_CODE";

        public static bool ValidLobbyCode(string code) =>
            code != null && code.Length == LobbyCodeLength &&
            code.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F');

        public static bool TryNormalizeLobbyCode(string input, out string code)
        {
            code = input?.Trim();
            if (!ValidLobbyCode(code))
            {
                code = null;
                return false;
            }

            code = code.ToUpperInvariant();
            return true;
        }

        public static string GenerateLobbyCode()
        {
            var bytes = new byte[LobbyCodeLength / 2];
            using var random = RandomNumberGenerator.Create();
            random.GetBytes(bytes);
            return BitConverter.ToString(bytes).Replace("-", "");
        }

        public static void RequireUniqueLobbyCodeMatch(uint matches)
        {
            if (matches == 0)
                throw new InvalidOperationException("No active lobby found for that code. Check the code and try again.");
            if (matches > 1)
                throw new InvalidOperationException("That code matches multiple lobbies. Ask the host to create a new lobby.");
        }
        public static OnlineCompatibility Compatibility()
        {
            using var hash=SHA256.Create();var text=new StringBuilder();
            text.Append("Room contract:").Append(OnlineFullRoomBootstrap.Catalog).Append('\n');
            foreach(string resource in new[]{"outfield_players","goalkeepers","Kits/kit-picker-v1-11-supported"})
            {
                var asset=Resources.Load<TextAsset>(resource);
                if(asset==null)throw new InvalidOperationException("Required online catalog asset is missing: "+resource);
                text.Append(resource).Append('\n').Append(asset.text.Replace("\r\n","\n").TrimStart('\uFEFF')).Append('\n');
            }
            string digest=BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(text.ToString()))).Replace("-","");
            return new OnlineCompatibility(OnlineSessionWire.Version,"frontend-rules-v14","catalog-"+digest);
        }
    }
}
