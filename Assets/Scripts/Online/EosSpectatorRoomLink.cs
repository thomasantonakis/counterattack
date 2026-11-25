using System;
using System.Collections.Generic;
using System.IO;
using Epic.OnlineServices;
using Epic.OnlineServices.P2P;

namespace CounterAttack.Online
{
    // Owned by one authenticated viewer connection. The host uses only the send
    // side; spectator packets never feed the two-manager dispatch loop.
    internal sealed class EosSpectatorRoomLink : IDisposable
    {
        private const byte Channel = 42;
        private const int PacketsPerTick = 4;
        private readonly ProductUserId local, remote;
        private readonly string remoteIdentity;
        private readonly SocketId socket;
        private readonly P2PInterface p2p;
        private readonly byte[] incoming = new byte[1170];
        private IEnumerator<byte[]> sending;
        private byte[] pending;

        internal EosSpectatorRoomLink(EOSStandaloneSession session, ProductUserId remote, SocketId socket, string matchId)
        {
            local = session.UserId;
            this.remote = remote;
            remoteIdentity = remote.ToString();
            this.socket = socket;
            p2p = session.Platform.GetP2PInterface();
            Stream = new OnlineSpectatorStream(matchId);
        }

        internal OnlineSpectatorStream Stream { get; }
        internal bool HasOutgoing => sending != null || pending != null;
        internal void QueueLatest(byte[] snapshot) => pending = snapshot;

        internal void Flush()
        {
            for (int i = 0; i < PacketsPerTick; i++)
            {
                if (sending == null)
                {
                    if (pending == null) return;
                    sending = Stream.Encode(pending).GetEnumerator();
                    pending = null;
                }
                if (!sending.MoveNext())
                {
                    sending.Dispose();
                    sending = null;
                    continue;
                }

                var send = new SendPacketOptions
                {
                    LocalUserId = local, RemoteUserId = remote, SocketId = socket, Channel = Channel,
                    Data = new ArraySegment<byte>(sending.Current), Reliability = PacketReliability.ReliableOrdered
                };
                var result = p2p.SendPacket(ref send);
                if (result != Result.Success)
                    throw new IOException("Sending spectator packet: " + result);
            }
        }

        internal int PollHost()
        {
            var receive = new ReceivePacketOptions
            {
                LocalUserId = local, MaxDataSizeBytes = (uint)incoming.Length, RequestedChannel = Channel
            };
            int applied = 0;
            for (int i = 0; i < PacketsPerTick; i++)
            {
                ProductUserId sender = null;
                var actualSocket = new SocketId();
                var result = p2p.ReceivePacket(ref receive, ref sender, ref actualSocket, out var channel,
                    new ArraySegment<byte>(incoming), out var size);
                if (result == Result.NotFound) break;
                if (result != Result.Success)
                    throw new IOException("Receiving spectator packet: " + result);
                if (channel == Channel && sender?.ToString() == remoteIdentity && actualSocket.SocketName == socket.SocketName
                    && Stream.Receive(incoming, (int)size))
                    applied++;
            }
            return applied;
        }

        public void Dispose()
        {
            sending?.Dispose();
            sending = null;
            pending = null;
        }
    }
}
