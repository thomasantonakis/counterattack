using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace CounterAttack.Online
{
    // Only the events used by Pitch and Luck travel here. Stats and player
    // identity already arrive in the authenticated terminal Room publication.
    public static class OnlineFullTimeReport
    {
        public const string Part = "report-part";
        public const string Unavailable = "report-unavailable";
        private const int Version = 1;
        private const int ChunkBytes = 12000;
        private const int MaxParts = 96;
        private const int MaxExpanded = 12 * 1024 * 1024;
        private const int MaxEvents = 20000;
        private static readonly UTF8Encoding Utf8 = new(false, true);
        private static readonly JsonSerializerSettings Settings = new() { NullValueHandling = NullValueHandling.Ignore, MaxDepth = 32 };

        private sealed class Payload
        {
            public int version;
            public List<GameplayEvent> events;
        }

        public static OnlineGroundPassMessage[] Encode(string match, long terminalPublication, IEnumerable<GameplayEvent> history)
        {
            if (!OnlineProtocolIds.IsValid(match) || terminalPublication <= 0)
                throw new ArgumentException("A terminal publication is required.");
            var events = history.Where(IsReportEvent).Take(MaxEvents + 1).Select(Project).ToList();
            if (events.Count > MaxEvents || !ValidEvents(events))
                throw new InvalidDataException("Full Time report history exceeded its limits.");
            byte[] bytes = Utf8.GetBytes(JsonConvert.SerializeObject(new Payload { version = Version, events = events }, Settings));
            if (bytes.Length > MaxExpanded) throw new InvalidDataException("Full Time report exceeded its expansion budget.");
            using var output = new MemoryStream();
            using (var zip = new GZipStream(output, CompressionLevel.Fastest, leaveOpen: true))
                zip.Write(bytes, 0, bytes.Length);
            byte[] compressed = output.ToArray();
            int count = (compressed.Length + ChunkBytes - 1) / ChunkBytes;
            if (count > MaxParts) throw new InvalidDataException("Full Time report exceeded its transfer budget.");
            string digest = Digest(compressed);
            var parts = new OnlineGroundPassMessage[count];
            for (int index = 0; index < count; index++)
            {
                int offset = index * ChunkBytes;
                parts[index] = new OnlineGroundPassMessage
                {
                    kind = Part, matchId = match, revision = terminalPublication, sequence = index + 1,
                    reportPartCount = count, reportExpandedBytes = bytes.Length, digest = digest,
                    reportChunk = Convert.ToBase64String(compressed, offset, Math.Min(ChunkBytes, compressed.Length - offset))
                };
            }
            return parts;
        }

        internal static bool ValidEnvelope(OnlineGroundPassMessage message)
        {
            if (message.kind == Unavailable)
                return message.revision > 0 && message.reportChunk == null
                    && message.reportPartCount == 0 && message.reportExpandedBytes == 0;
            if (message.kind != Part)
                return message.reportChunk == null && message.reportPartCount == 0 && message.reportExpandedBytes == 0;
            return message.revision > 0 && message.reportPartCount is >= 1 and <= MaxParts
                && message.sequence > 0 && message.sequence <= message.reportPartCount
                && message.reportChunk?.Length is > 0 and <= ChunkBytes / 3 * 4
                && message.reportExpandedBytes is > 0 and <= MaxExpanded && message.digest?.Length == 64;
        }

        public sealed class Receiver
        {
            private readonly string match;
            private readonly long publication;
            private string[] chunks;
            private string digest;
            private int expectedBytes, received;
            public IReadOnlyList<GameplayEvent> Events { get; private set; }
            public bool Failed { get; private set; }

            public Receiver(string match, long publication)
            {
                this.match = match;
                this.publication = publication;
            }

            public bool Accept(OnlineGroundPassMessage message)
            {
                if (message == null || Failed || message.kind != Part || message.matchId != match
                    || message.revision != publication || !ValidEnvelope(message)) return false;
                if (chunks == null)
                {
                    chunks = new string[message.reportPartCount];
                    digest = message.digest;
                    expectedBytes = message.reportExpandedBytes;
                }
                if (chunks.Length != message.reportPartCount || digest != message.digest
                    || expectedBytes != message.reportExpandedBytes) return false;
                int index = (int)message.sequence - 1;
                if (chunks[index] != null) return chunks[index] == message.reportChunk;
                chunks[index] = message.reportChunk;
                if (++received != chunks.Length) return true;
                try
                {
                    using var input = new MemoryStream();
                    foreach (string chunk in chunks)
                    {
                        byte[] bytes = Convert.FromBase64String(chunk);
                        input.Write(bytes, 0, bytes.Length);
                    }
                    if (Digest(input.ToArray()) != digest) throw new InvalidDataException();
                    input.Position = 0;
                    using var zip = new GZipStream(input, CompressionMode.Decompress);
                    using var output = new MemoryStream();
                    var buffer = new byte[8192];
                    int read;
                    while ((read = zip.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        if (output.Length + read > expectedBytes) throw new InvalidDataException();
                        output.Write(buffer, 0, read);
                    }
                    if (output.Length != expectedBytes) throw new InvalidDataException();
                    var payload = JsonConvert.DeserializeObject<Payload>(Utf8.GetString(output.ToArray()), Settings);
                    if (payload?.version != Version || !ValidEvents(payload.events)) throw new InvalidDataException();
                    Events = payload.events;
                    return true;
                }
                catch (Exception error) when (error is IOException or InvalidDataException or FormatException or JsonException or DecoderFallbackException)
                {
                    Failed = true;
                    return false;
                }
            }
        }

        private static string Digest(byte[] bytes)
        {
            using var hash = SHA256.Create();
            return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "");
        }

        private static bool IsReportEvent(GameplayEvent entry) => entry?.kind switch
        {
            "dice.roll" => entry.dice != null,
            "snapshot.action_boundary" => entry.snapshot?.tokens != null,
            "token.move" => entry.movementPath?.countedForDistance == true,
            _ => false
        };

        private static GameplayEvent Project(GameplayEvent entry)
        {
            var dice = entry.dice;
            return new GameplayEvent
            {
                sequenceNumber = entry.sequenceNumber, kind = entry.kind, currentHalf = entry.currentHalf,
                currentGameState = entry.currentGameState, teamInAttack = entry.teamInAttack,
                attackHasPossession = entry.attackHasPossession, actorTokenKey = entry.actorTokenKey,
                snapshot = entry.kind == "snapshot.action_boundary" ? new GameplaySnapshotSummary
                {
                    currentState = entry.snapshot.currentState, currentHalf = entry.snapshot.currentHalf,
                    tokens = entry.snapshot.tokens
                } : null,
                movementPath = entry.kind == "token.move" ? new GameplayMovementPath
                {
                    countedForDistance = true, hexes = entry.movementPath.hexes
                } : null,
                dice = entry.kind == "dice.roll" ? new GameplayDiceEvent
                {
                    context = dice.context, luck = PostMatchLuckData.Model(entry), luckJackpotEnabled = dice.luckJackpotEnabled,
                    jackpotEnabled = dice.jackpotEnabled, isJackpot = dice.isJackpot, roll = dice.roll,
                    overrideUsed = dice.overrideUsed, modifiers = null
                } : null
            };
        }

        private static bool ValidEvents(List<GameplayEvent> events)
        {
            if (events == null || events.Count > MaxEvents) return false;
            int previous = 0;
            foreach (var entry in events)
            {
                if (!IsReportEvent(entry) || entry.sequenceNumber <= previous || entry.currentHalf is < 0 or > 4
                    || entry.dice != null && (entry.dice.roll is < 1 or > 6 || entry.dice.luck?.contenders?.Count > 36
                        || entry.dice.luck?.contenders?.Any(contender => contender == null) == true)
                    || entry.snapshot?.tokens?.Count > 36 || entry.movementPath?.hexes?.Count > 256) return false;
                previous = entry.sequenceNumber;
            }
            return true;
        }
    }
}
