using System;
using System.Collections;
using System.IO;
using Epic.OnlineServices;
using Epic.OnlineServices.P2P;
using UnityEngine;

internal static class EOSWatchLobbyProof
{
    private const byte DuringWatch = 1;
    private const byte AfterWatch = 2;
    private const byte Finished = 3;

    internal static IEnumerator Host(string outputDirectory) =>
        EOSStandalonePacketProbe.HostProbe(outputDirectory, (session, manager, socket, _) =>
            WatchAlongsideManager(session, manager, socket, outputDirectory));

    internal static IEnumerator Manager(string lobbyId) =>
        EOSStandalonePacketProbe.GuestProbe(lobbyId, EchoManagerPackets);

    internal static IEnumerator Viewer(string lobbyId) =>
        EOSStandalonePacketProbe.GuestProbe(lobbyId, watchLobby: true);

    private static IEnumerator WatchAlongsideManager(EOSStandaloneSession session,
        ProductUserId manager, SocketId socket, string outputDirectory)
    {
        var connection = new ManagerProbeConnection(session, manager, socket);
        var watch = EOSStandalonePacketProbe.HostProbe(outputDirectory, watchOwner: session);
        var watchLobbyFile = Path.Combine(outputDirectory, EOSStandalonePacketProbe.WatchLobbyFileName);
        bool managerPingSent = false;
        bool managerResponded = false;
        try
        {
            while (watch.MoveNext())
            {
                if (!managerPingSent && File.Exists(watchLobbyFile))
                {
                    connection.Send(DuringWatch);
                    managerPingSent = true;
                }
                if (connection.TryReceive(out var response))
                {
                    EOSProofCheck.Require(response == DuringWatch, "Manager response during watch lobby");
                    managerResponded = true;
                }
                yield return watch.Current;
            }
        }
        finally
        {
            (watch as IDisposable)?.Dispose();
        }

        EOSProofCheck.Require(managerResponded, "Manager did not respond while the watch lobby proof was running");
        connection.Send(AfterWatch);
        var deadline = Time.realtimeSinceStartupAsDouble + 25;
        while (Time.realtimeSinceStartupAsDouble < deadline)
        {
            session.Tick();
            if (connection.TryReceive(out var response))
            {
                EOSProofCheck.Require(response == AfterWatch, "Manager response after watch lobby shutdown");
                connection.Send(Finished);
                yield break;
            }
            yield return null;
        }
        throw new EOSProofFailure("Manager did not respond after the watch lobby closed");
    }

    private static IEnumerator EchoManagerPackets(EOSStandaloneSession session,
        ProductUserId host, SocketId socket, string _)
    {
        var connection = new ManagerProbeConnection(session, host, socket);
        var expected = DuringWatch;
        var deadline = Time.realtimeSinceStartupAsDouble + 1320;
        while (Time.realtimeSinceStartupAsDouble < deadline)
        {
            session.Tick();
            if (connection.TryReceive(out var message))
            {
                EOSProofCheck.Require(message == expected, "Manager proof packet order");
                if (message == Finished)
                    yield break;
                connection.Send(message);
                expected++;
            }
            yield return null;
        }
        throw new EOSProofFailure("Watch lobby proof did not finish");
    }

    private sealed class ManagerProbeConnection
    {
        private readonly ProductUserId local;
        private readonly ProductUserId peer;
        private readonly SocketId socket;
        private readonly P2PInterface p2p;
        private readonly byte[] received = new byte[256];

        internal ManagerProbeConnection(EOSStandaloneSession session, ProductUserId peer, SocketId socket)
        {
            local = session.UserId;
            this.peer = peer;
            this.socket = socket;
            p2p = session.Platform.GetP2PInterface();
        }

        internal void Send(byte message)
        {
            var options = new SendPacketOptions
            {
                LocalUserId = local, RemoteUserId = peer, SocketId = socket,
                Channel = EOSStandalonePacketProbe.ManagerProbeChannel,
                Data = new ArraySegment<byte>(new[] { message }),
                AllowDelayedDelivery = true, Reliability = PacketReliability.ReliableOrdered
            };
            EOSProofCheck.Require(p2p.SendPacket(ref options) == Result.Success, "Sending manager proof packet");
        }

        internal bool TryReceive(out byte message)
        {
            message = 0;
            var options = new ReceivePacketOptions
            {
                LocalUserId = local, MaxDataSizeBytes = (uint)received.Length,
                RequestedChannel = EOSStandalonePacketProbe.ManagerProbeChannel
            };
            ProductUserId sender = null;
            var receivedSocket = new SocketId();
            var result = p2p.ReceivePacket(ref options, ref sender, ref receivedSocket,
                out var channel, new ArraySegment<byte>(received), out var count);
            if (result == Result.NotFound)
                return false;
            EOSProofCheck.Require(result == Result.Success && count == 1
                && channel == EOSStandalonePacketProbe.ManagerProbeChannel
                && sender?.ToString() == peer.ToString() && receivedSocket.SocketName == socket.SocketName,
                "Receiving manager proof packet from the connected manager");
            message = received[0];
            return true;
        }
    }
}
