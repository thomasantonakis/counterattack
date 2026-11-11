using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace CounterAttack.Online
{
    [Serializable]
    public sealed class OnlineGroundPassMessage
    {
        public string version;
        public string kind, matchId, decisionId, actionId, status, digest;
        public long sequence, revision;
        public OnlineGroundPassState state;

        public OnlineMatchCommand ToCommand() => new(matchId, sequence, revision, decisionId, actionId);
        public static OnlineGroundPassMessage Command(OnlineGroundPassState state, long sequence, string action)
            => new() { kind = "command", matchId = state.matchId, sequence = sequence,
                revision = state.revision, decisionId = state.decisionId, actionId = action };
    }

    /// <summary>Bounded framing for an already authenticated reliable ordered EOS peer.</summary>
    public sealed class OnlineGroundPassWire
    {
        public const int PayloadLimit = 65536;
        public const int FragmentPayload = 900;
        private readonly MemoryStream pending = new();
        private int expectedSize;
        private ushort expectedIndex, fragmentCount;

        public static IEnumerable<byte[]> Encode(OnlineGroundPassMessage message)
        {
            message.version = "ground-wire-v1";
            var payload = Encoding.UTF8.GetBytes(JsonUtility.ToJson(message));
            if (payload.Length == 0 || payload.Length > PayloadLimit) throw new InvalidDataException("Gameplay message size");
            var count = (ushort)((payload.Length + FragmentPayload - 1) / FragmentPayload);
            for (ushort index = 0; index < count; index++)
            {
                using var stream = new MemoryStream();
                using var writer = new BinaryWriter(stream);
                writer.Write((ushort)0xCA01); writer.Write(index); writer.Write(count); writer.Write(payload.Length);
                writer.Write(payload, index * FragmentPayload, Math.Min(FragmentPayload, payload.Length - index * FragmentPayload));
                yield return stream.ToArray();
            }
        }

        public OnlineGroundPassMessage Receive(byte[] packet, int length)
        {
            try
            {
                if (length < 11 || length > FragmentPayload + 10 || length > packet.Length)
                    throw new InvalidDataException("Gameplay fragment size");
                using var reader = new BinaryReader(new MemoryStream(packet, 0, length));
                if (reader.ReadUInt16() != 0xCA01) throw new InvalidDataException("Gameplay fragment version");
                var index = reader.ReadUInt16(); var count = reader.ReadUInt16(); var size = reader.ReadInt32();
                if (size < 1 || size > PayloadLimit || count != (size + FragmentPayload - 1) / FragmentPayload
                    || index >= count || length - 10 != Math.Min(FragmentPayload, size - index * FragmentPayload))
                    throw new InvalidDataException("Gameplay fragment header");
                if (index == 0)
                {
                    if (expectedIndex != 0) throw new InvalidDataException("Interrupted gameplay message");
                    expectedSize = size; fragmentCount = count;
                }
                if (index != expectedIndex || count != fragmentCount || size != expectedSize)
                    throw new InvalidDataException("Out-of-order gameplay fragment");
                pending.Write(packet, 10, length - 10); expectedIndex++;
                if (expectedIndex != fragmentCount) return null;
                var json = new UTF8Encoding(false, true).GetString(pending.ToArray());
                Reset();
                var message = JsonUtility.FromJson<OnlineGroundPassMessage>(json);
                if (message == null || message.version != "ground-wire-v1" || !OnlineProtocolIds.IsValid(message.matchId)
                    || !(new[] { "hello", "command", "state", "reply", "done", "finish" }).Contains(message.kind))
                    throw new InvalidDataException("Gameplay message contract");
                return message;
            }
            catch { Reset(); throw; }
        }

        private void Reset() { pending.SetLength(0); pending.Position = 0; expectedIndex = fragmentCount = 0; expectedSize = 0; }
    }

    /// <summary>Guest value replica. It has no gameplay manager, dice or clock references.</summary>
    public sealed class OnlineGroundPassReplica
    {
        private readonly string matchId;
        private string currentJson;
        private long sequence = -1, revision = -1;
        public OnlineGroundPassReplica(string matchId) { OnlineProtocolIds.Require(matchId, nameof(matchId)); this.matchId = matchId; }
        public OnlineGroundPassState State => currentJson == null ? null : JsonUtility.FromJson<OnlineGroundPassState>(currentJson);

        public bool Apply(OnlineGroundPassState state)
        {
            if (state == null || state.matchId != matchId || state.publicationSequence < 1 || state.revision < 0
                || state.tokens == null || state.tokens.Length > 64 || state.tokens.Any(t => t == null || !OnlineProtocolIds.IsValid(t.id))
                || state.tokens.Select(t => t.id).Distinct().Count() != state.tokens.Length
                || state.legalActions == null || state.legalActions.Length > 4096
                || state.legalActions.Any(a => !OnlineProtocolIds.IsValid(a))) return false;
            var json = JsonUtility.ToJson(state);
            if (state.publicationSequence < sequence || state.revision < revision) return false;
            if (state.publicationSequence == sequence) return json == currentJson;
            currentJson = json; sequence = state.publicationSequence; revision = state.revision;
            return true;
        }
    }
}
