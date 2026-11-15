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
        public static RelayControl Route(bool relayOnly)=>relayOnly?RelayControl.ForceRelays:RelayControl.AllowRelays;
        public static bool ValidLobbyCode(string code)=>code!=null && code.Length==32 && code.All(c=>c is >= '0' and <= '9' or >= 'a' and <= 'z' or >= 'A' and <= 'Z');
        public static OnlineCompatibility Compatibility()
        {
            using var hash=SHA256.Create();var text=new StringBuilder();
            foreach(string resource in new[]{"outfield_players","goalkeepers","Kits/kit-picker-v1-11-supported"})
            {
                var asset=Resources.Load<TextAsset>(resource);
                if(asset==null)throw new InvalidOperationException("Required online catalog asset is missing: "+resource);
                text.Append(resource).Append('\n').Append(asset.text.Replace("\r\n","\n").TrimStart('\uFEFF')).Append('\n');
            }
            string digest=BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(text.ToString()))).Replace("-","");
            return new OnlineCompatibility(OnlineSessionWire.Version,"frontend-rules-v4","catalog-"+digest);
        }
    }
}
