using System;
using System.Collections.Generic;
using System.Linq;
namespace CounterAttack.Online
{
    [Serializable] public sealed class OnlineSessionMessage
    {
        public string version,kind,matchId,status,protocol,rules,catalog;
        public long sequence;
        public OnlineSetupRequest request;
        public OnlineSetupState state;
    }
    // Dedicated frontend control channel; it cannot be decoded as a gameplay packet.
    public sealed class OnlineSessionWire
    {
        public const string Version="session-wire-v1";
        public const int FragmentPayload=OnlineReliableJsonWire<OnlineSessionMessage>.FragmentPayload;
        public const int HeaderSize=OnlineReliableJsonWire<OnlineSessionMessage>.HeaderSize;
        public const int PayloadLimit=OnlineReliableJsonWire<OnlineSessionMessage>.PayloadLimit;
        private readonly OnlineReliableJsonWire<OnlineSessionMessage> framing=new(0xCA11,Valid);
        private static bool Valid(OnlineSessionMessage m)=>m.version==Version && OnlineProtocolIds.IsValid(m.matchId)
            && new[]{"hello","setup-request","setup-state","setup-reply","heartbeat","leave","phase-ready","phase-ack"}.Contains(m.kind);
        public static IEnumerable<byte[]> Encode(OnlineSessionMessage message)
        {
            message.version=Version;
            foreach(var fragment in new OnlineReliableJsonWire<OnlineSessionMessage>(0xCA11,Valid).Encode(message))yield return fragment;
        }
        public OnlineSessionMessage Receive(byte[] packet,int length,bool allowConnectionRestart=false)=>framing.Receive(packet,length,allowConnectionRestart);
    }
}
