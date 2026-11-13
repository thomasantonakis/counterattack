using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using Newtonsoft.Json;

namespace CounterAttack.Online
{
    [Serializable]
    public sealed class OnlineGroundPassPresentation
    {
        public string matchId;
        public long publicationSequence, frameSequence;
        public float ballX, ballY, ballZ;
    }
    [Serializable]
    public sealed class OnlineGroundPassMessage
    {
        public string version;
        public string kind, matchId, decisionId, actionId, status, digest;
        public long sequence, revision;
        // Used only by the explicitly negotiated manual recovery handoff.
        public string peerNonce;
        public long connectionGeneration;
        public OnlineGroundPassState state;
        public OnlineGroundPassPresentation presentation;

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
        public const int HeaderSize = 26;
        private readonly MemoryStream pending = new();
        private int expectedSize;
        private ushort expectedIndex, fragmentCount;
        private Guid expectedMessage;
        private readonly HashSet<Guid> retiredMessages = new();
        private readonly Queue<Guid> retiredOrder = new();

        public static IEnumerable<byte[]> Encode(OnlineGroundPassMessage message)
        {
            message.version = "ground-wire-v5";
            // Unity serializes null inline DTO fields as fully expanded default
            // objects. Keep heartbeats/controls small instead of embedding an
            // unused full roster/statistics/timing payload in every message.
            var payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            if (payload.Length == 0 || payload.Length > PayloadLimit) throw new InvalidDataException("Gameplay message size");
            var count = (ushort)((payload.Length + FragmentPayload - 1) / FragmentPayload);
            var identity = Guid.NewGuid().ToByteArray();
            for (ushort index = 0; index < count; index++)
            {
                using var stream = new MemoryStream();
                using var writer = new BinaryWriter(stream);
                writer.Write((ushort)0xCA05); writer.Write(index); writer.Write(count); writer.Write(payload.Length); writer.Write(identity);
                writer.Write(payload, index * FragmentPayload, Math.Min(FragmentPayload, payload.Length - index * FragmentPayload));
                yield return stream.ToArray();
            }
        }

        public OnlineGroundPassMessage Receive(byte[] packet, int length, bool allowConnectionRestart = false)
        {
            try
            {
                if (packet == null || length < HeaderSize + 1 || length > FragmentPayload + HeaderSize || length > packet.Length)
                    throw new InvalidDataException("Gameplay fragment size");
                using var reader = new BinaryReader(new MemoryStream(packet, 0, length));
                if (reader.ReadUInt16() != 0xCA05) throw new InvalidDataException("Gameplay fragment version");
                var index = reader.ReadUInt16(); var count = reader.ReadUInt16(); var size = reader.ReadInt32();
                var identity = new Guid(reader.ReadBytes(16));
                if (size < 1 || size > PayloadLimit || count != (size + FragmentPayload - 1) / FragmentPayload
                    || identity == Guid.Empty || index >= count || length - HeaderSize != Math.Min(FragmentPayload, size - index * FragmentPayload))
                    throw new InvalidDataException("Gameplay fragment header");
                if (allowConnectionRestart && (retiredMessages.Contains(identity) || index > 0 && identity != expectedMessage)) return null;
                if (index == 0)
                {
                    if (expectedIndex != 0)
                    {
                        if (!allowConnectionRestart) throw new InvalidDataException("Interrupted gameplay message");
                        if (identity == expectedMessage) return null;
                        Reset(true);
                    }
                    expectedSize = size; fragmentCount = count; expectedMessage = identity;
                }
                if (identity != expectedMessage || index != expectedIndex || count != fragmentCount || size != expectedSize)
                    throw new InvalidDataException("Out-of-order gameplay fragment");
                pending.Write(packet, HeaderSize, length - HeaderSize); expectedIndex++;
                if (expectedIndex != fragmentCount) return null;
                var json = new UTF8Encoding(false, true).GetString(pending.ToArray());
                Reset();
                var message = JsonConvert.DeserializeObject<OnlineGroundPassMessage>(json);
                if (message == null || message.version != "ground-wire-v5" || !OnlineProtocolIds.IsValid(message.matchId)
                    || !(new[] { "hello", "command", "state", "presentation", "reply", "control", "control-reply", "done", "finish" }).Contains(message.kind))
                    throw new InvalidDataException("Gameplay message contract");
                return message;
            }
            catch (JsonException error) { Reset(true); throw new InvalidDataException("Gameplay JSON contract", error); }
            catch (DecoderFallbackException error) { Reset(true); throw new InvalidDataException("Gameplay UTF8 contract", error); }
            catch { Reset(true); throw; }
        }

        private void Reset(bool retire = false)
        {
            if (retire && expectedMessage != Guid.Empty && retiredMessages.Add(expectedMessage))
            {
                retiredOrder.Enqueue(expectedMessage);
                if (retiredOrder.Count > 64) retiredMessages.Remove(retiredOrder.Dequeue());
            }
            pending.SetLength(0); pending.Position = 0; expectedIndex = fragmentCount = 0; expectedSize = 0; expectedMessage = Guid.Empty;
        }
    }

    /// <summary>Guest value replica. It has no gameplay manager, dice or clock references.</summary>
    public sealed class OnlineGroundPassReplica
    {
        private readonly string matchId;
        private readonly HashSet<string> roster;
        private readonly string catalog;
        private string currentJson;
        private long sequence = -1, revision = -1, rollSequence = -1;
        private string presentationJson;
        private long presentationFrame = -1;
        public OnlineGroundPassReplica(string matchId, IEnumerable<string> expectedRoster = null, string expectedCatalog = null)
        {
            OnlineProtocolIds.Require(matchId, nameof(matchId)); this.matchId = matchId;
            if (expectedCatalog != null) OnlineProtocolIds.Require(expectedCatalog, nameof(expectedCatalog));
            catalog = expectedCatalog;
            if (expectedRoster != null)
            {
                var ids = expectedRoster.ToArray();
                if (ids.Length == 0 || ids.Length > 64 || ids.Any(id => !OnlineProtocolIds.IsValid(id)) || ids.Distinct().Count() != ids.Length)
                    throw new ArgumentException("A distinct compatible roster is required", nameof(expectedRoster));
                roster = new HashSet<string>(ids, StringComparer.Ordinal);
            }
        }
        public OnlineGroundPassState State => currentJson == null ? null : JsonUtility.FromJson<OnlineGroundPassState>(currentJson);

        public bool ApplyPresentation(OnlineGroundPassPresentation frame)
        {
            bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && Math.Abs(value) <= 100;
            if (frame == null || frame.matchId != matchId || frame.publicationSequence != sequence || frame.frameSequence < 1
                || frame.frameSequence < presentationFrame || !Finite(frame.ballX) || !Finite(frame.ballY) || !Finite(frame.ballZ)) return false;
            var json = JsonUtility.ToJson(frame);
            if (frame.frameSequence == presentationFrame) return json == presentationJson;
            presentationFrame = frame.frameSequence; presentationJson = json; return true;
        }

        public bool Apply(OnlineGroundPassState state)
        {
            if (state == null || state.matchId != matchId || state.publicationSequence < 1 || state.revision < 0
                || state.tokens == null || state.tokens.Length > 64 || state.tokens.Any(t => t == null || !OnlineProtocolIds.IsValid(t.id))
                || state.tokens.Select(t => t.id).Distinct().Count() != state.tokens.Length
                || state.legalActions == null || state.legalActions.Length > 4096
                || state.legalActions.Any(a => !OnlineProtocolIds.IsValid(a))) return false;
            if (catalog != null && state.catalogId != catalog || roster != null && !roster.SetEquals(state.tokens.Select(token => token.id))) return false;
            if (state.rollSequence < rollSequence || state.rollSequence < 0 || !ValidRolls(state)) return false;
            var prior = State;
            if (prior?.rolls != null && state.rolls != null)
                foreach (var result in state.rolls)
                {
                    var observed = prior.rolls.FirstOrDefault(item => item.sequence == result.sequence);
                    if (observed != null && JsonUtility.ToJson(observed) != JsonUtility.ToJson(result)) return false;
                }
            var json = JsonUtility.ToJson(state);
            if (state.publicationSequence < sequence || state.revision < revision) return false;
            if (state.publicationSequence == sequence) return json == currentJson;
            currentJson = json; sequence = state.publicationSequence; revision = state.revision; rollSequence = state.rollSequence;
            return true;
        }

        private static bool ValidRolls(OnlineGroundPassState state)
        {
            if (state.rolls == null) return true;
            if (state.rolls.Length > 64) return false;
            long previous = 0;
            foreach (var result in state.rolls)
            {
                if (result == null || result.sequence <= previous || result.sequence > state.rollSequence
                    || result.roll < 1 || result.roll > 6 || string.IsNullOrEmpty(result.context)
                    || result.isJackpot && (!result.jackpotEnabled || result.roll != 6)) return false;
                if (result.actorId != null && (!state.tokens.Any(token => token.id == result.actorId)
                    || result.team != (result.actorId.StartsWith("home:", StringComparison.Ordinal) ? "Home" : "Away"))) return false;
                previous = result.sequence;
            }
            return true;
        }
    }
}
