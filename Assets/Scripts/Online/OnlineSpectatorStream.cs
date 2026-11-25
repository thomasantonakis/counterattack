using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CounterAttack.Online
{
    public sealed class OnlineSpectatorStream
    {
        private const ushort Magic = 0xCA42;
        private const int MaxCompressedBytes = 48000;
        private readonly string matchId;
        private readonly OnlineReliableJsonWire<Envelope> wire = new(Magic, Valid);

        private sealed class Envelope
        {
            public int version, expandedBytes;
            public byte[] compressed;
        }

        public OnlineSpectatorStream(string matchId)
        {
            OnlineProtocolIds.Require(matchId, nameof(matchId));
            this.matchId = matchId;
        }

        public long Sequence { get; private set; }
        public JObject Latest { get; private set; }

        public IEnumerable<byte[]> Encode(byte[] snapshot)
        {
            if (snapshot == null || snapshot.Length < 1 || snapshot.Length > OnlineSpectatorRoomWire.MaxBytes)
                throw new InvalidDataException("Spectator snapshot size");

            using var output = new MemoryStream();
            using (var gzip = new GZipStream(output, CompressionLevel.Fastest, true))
                gzip.Write(snapshot, 0, snapshot.Length);

            var envelope = new Envelope
            {
                version = OnlineSpectatorRoomWire.Version, expandedBytes = snapshot.Length,
                compressed = output.ToArray()
            };
            if (!Valid(envelope))
                throw new InvalidDataException("Compressed spectator snapshot size");
            return wire.Encode(envelope);
        }

        // Only call with packets from the authenticated host on the spectator lane.
        public bool Receive(byte[] packet, int length)
        {
            var envelope = wire.Receive(packet, length, allowConnectionRestart: true);
            if (envelope == null)
                return false;

            var snapshot = Expand(envelope);
            int stage = StageIndex(snapshot["stage"]?.Type == JTokenType.String ? snapshot.Value<string>("stage") : null);
            if (snapshot["version"]?.Type != JTokenType.Integer || snapshot.Value<long>("version") != OnlineSpectatorRoomWire.Version
                || snapshot["matchId"]?.Type != JTokenType.String || snapshot.Value<string>("matchId") != matchId
                || stage < 0 || snapshot["sequence"]?.Type != JTokenType.Integer)
                throw new InvalidDataException("Spectator snapshot identity");

            long sequence = snapshot.Value<long>("sequence");
            if (sequence <= Sequence || stage < StageIndex(Latest?.Value<string>("stage")))
                return false;
            Latest = snapshot;
            Sequence = sequence;
            return true;
        }

        private static bool Valid(Envelope envelope) =>
            envelope.version == OnlineSpectatorRoomWire.Version
            && envelope.expandedBytes is > 0 and <= OnlineSpectatorRoomWire.MaxBytes
            && envelope.compressed?.Length is > 0 and <= MaxCompressedBytes;

        private static int StageIndex(string stage) => stage switch
        {
            "lobby" => 0,
            "setup" => 1,
            "draft" => 2,
            "room" => 3,
            _ => -1
        };

        private static JObject Expand(Envelope envelope)
        {
            using var input = new MemoryStream(envelope.compressed);
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream(envelope.expandedBytes);
            var buffer = new byte[4096];
            int count;
            while ((count = gzip.Read(buffer, 0, buffer.Length)) > 0)
            {
                if (output.Length + count > envelope.expandedBytes)
                    throw new InvalidDataException("Expanded spectator snapshot exceeded its budget");
                output.Write(buffer, 0, count);
            }
            if (output.Length != envelope.expandedBytes)
                throw new InvalidDataException("Expanded spectator snapshot length");

            try { return JObject.Parse(new UTF8Encoding(false, true).GetString(output.ToArray())); }
            catch (JsonException error) { throw new InvalidDataException("Spectator snapshot JSON", error); }
            catch (DecoderFallbackException error) { throw new InvalidDataException("Spectator snapshot UTF-8", error); }
        }
    }
}
