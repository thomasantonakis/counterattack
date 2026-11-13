using System;

namespace CounterAttack.Online
{
    public enum OnlinePeerState { Connected, TransportLost, Left, Cancelled }

    /// <summary>
    /// Main-thread liveness for one authenticated remote participant. The caller
    /// supplies a monotonic unscaled clock and authenticates every connection
    /// before binding. Generations belong to the authority, never to payloads.
    /// A new connection remains held until its full snapshot is acknowledged.
    /// </summary>
    public sealed class OnlinePeerRecovery
    {
        private readonly OnlineMatchProtocol protocol;
        private readonly OnlineCompatibility compatibility;
        private readonly OnlineSharedPause pause;
        private readonly Func<double> clock;
        private readonly string account;
        private readonly double timeout;
        private string connection;
        private double lastSeen, lastClock;
        private long lastHeartbeat;
        private bool awaitingSnapshot;

        public long Generation { get; private set; }
        public OnlinePeerState State { get; private set; } = OnlinePeerState.TransportLost;
        public bool CanSubmit => State == OnlinePeerState.Connected && !awaitingSnapshot;

        public OnlinePeerRecovery(OnlineMatchProtocol protocol, OnlineCompatibility compatibility,
            OnlineSharedPause pause, string reservedAccount, Func<double> monotonicClock, double timeoutSeconds = 5)
        {
            this.protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));
            this.compatibility = compatibility ?? throw new ArgumentNullException(nameof(compatibility));
            this.pause = pause ?? throw new ArgumentNullException(nameof(pause));
            OnlineProtocolIds.Require(reservedAccount, nameof(reservedAccount));
            account = reservedAccount;
            clock = monotonicClock ?? throw new ArgumentNullException(nameof(monotonicClock));
            if (double.IsNaN(timeoutSeconds) || double.IsInfinity(timeoutSeconds) || timeoutSeconds <= 0 || timeoutSeconds > 60)
                throw new ArgumentOutOfRangeException(nameof(timeoutSeconds));
            timeout = timeoutSeconds;
            lastClock = Now();
            pause.SetConnectionHold(true);
        }

        private double Now()
        {
            var now = clock();
            if (double.IsNaN(now) || double.IsInfinity(now) || now < lastClock)
                throw new InvalidOperationException("Recovery requires a monotonic finite clock");
            return lastClock = now;
        }

        public bool BindAuthenticated(string authenticatedAccount, string newConnection, OnlineCompatibility version)
        {
            if (State == OnlinePeerState.Left || State == OnlinePeerState.Cancelled || authenticatedAccount != account
                || !compatibility.Matches(version) || newConnection == connection) return false;
            var now = Now();
            if (!protocol.BindAuthenticatedAccount(authenticatedAccount, newConnection, version)) return false;
            connection = newConnection; Generation = checked(Generation + 1);
            lastSeen = now; lastHeartbeat = 0; awaitingSnapshot = true;
            State = OnlinePeerState.Connected;
            protocol.WithdrawDecision();
            pause.SetConnectionHold(true);
            return true;
        }

        public bool Heartbeat(string authenticatedConnection, long generation, long sequence)
        {
            Tick();
            if (State != OnlinePeerState.Connected || connection != authenticatedConnection
                || generation != Generation || sequence <= lastHeartbeat) return false;
            lastHeartbeat = sequence; lastSeen = Now(); return true;
        }

        public bool AcknowledgeSnapshot(string authenticatedConnection, long generation)
        {
            Tick();
            if (State != OnlinePeerState.Connected || connection != authenticatedConnection || generation != Generation) return false;
            awaitingSnapshot = false; pause.SetConnectionHold(false); return true;
        }

        public void Tick()
        {
            if (State == OnlinePeerState.Connected && Now() - lastSeen >= timeout) Lose(OnlinePeerState.TransportLost);
        }

        public void TransportLost(string authenticatedConnection, long generation)
        {
            if (State == OnlinePeerState.Connected && authenticatedConnection == connection && generation == Generation)
                Lose(OnlinePeerState.TransportLost);
        }

        public void Leave(string authenticatedConnection, long generation)
        {
            if (State == OnlinePeerState.Connected && authenticatedConnection == connection && generation == Generation)
                Lose(OnlinePeerState.Left);
        }

        public void Cancel() => Lose(OnlinePeerState.Cancelled);

        private void Lose(OnlinePeerState reason)
        {
            protocol.Disconnect(connection); protocol.WithdrawDecision(); awaitingSnapshot = true;
            State = reason; pause.SetConnectionHold(true);
        }
    }
}
