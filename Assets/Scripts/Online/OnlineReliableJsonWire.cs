using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
namespace CounterAttack.Online
{
    /// <summary>Bounded framing for an already authenticated reliable ordered EOS peer.</summary>
    internal sealed class OnlineReliableJsonWire<T> where T:class
    {
        public const int PayloadLimit = 65536;
        public const int FragmentPayload = 900;
        public const int HeaderSize = 26;
        private readonly ushort magic;
        private readonly Func<T,bool> validate;
        internal OnlineReliableJsonWire(ushort magic,Func<T,bool> validate)
        {this.magic=magic;this.validate=validate??throw new ArgumentNullException(nameof(validate));}
        private readonly MemoryStream pending = new();
        private int expectedSize;
        private ushort expectedIndex, fragmentCount;
        private Guid expectedMessage;
        private readonly HashSet<Guid> retiredMessages = new();
        private readonly Queue<Guid> retiredOrder = new();

        public IEnumerable<byte[]> Encode(T message)
        {
            if(message==null)throw new ArgumentNullException(nameof(message));
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
                writer.Write(magic); writer.Write(index); writer.Write(count); writer.Write(payload.Length); writer.Write(identity);
                writer.Write(payload, index * FragmentPayload, Math.Min(FragmentPayload, payload.Length - index * FragmentPayload));
                yield return stream.ToArray();
            }
        }

        public T Receive(byte[] packet, int length, bool allowConnectionRestart = false)
        {
            try
            {
                if (packet == null || length < HeaderSize + 1 || length > FragmentPayload + HeaderSize || length > packet.Length)
                    throw new InvalidDataException("Gameplay fragment size");
                using var reader = new BinaryReader(new MemoryStream(packet, 0, length));
                if (reader.ReadUInt16() != magic) throw new InvalidDataException("Gameplay fragment version");
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
                var message = JsonConvert.DeserializeObject<T>(json);
                if(message==null || !validate(message))
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

}
