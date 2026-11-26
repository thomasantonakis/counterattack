using System;

namespace CounterAttack.Online
{
    // Keeps the authoritative model alive during transport loss. Only the host
    // rotates the connection nonce; retries reuse it so delayed replies can land.
    public sealed class OnlineFrontendRecovery
    {
        public const double SilenceSeconds=15,RetrySeconds=2,RecoverySeconds=300;
        public bool Recovering {get;private set;}
        public bool Expired {get;private set;}
        private readonly OnlineSessionChannel channel;
        private readonly Func<double> clock;
        private readonly Action discardUnsent;
        private readonly Func<bool> queueEmpty;
        private double nextRetry,recoverUntil;
        public OnlineFrontendRecovery(OnlineSessionChannel channel,Func<double> clock,Action discardUnsent,Func<bool> queueEmpty)
        {
            this.channel=channel??throw new ArgumentNullException(nameof(channel));
            this.clock=clock??throw new ArgumentNullException(nameof(clock));
            this.discardUnsent=discardUnsent??throw new ArgumentNullException(nameof(discardUnsent));
            this.queueEmpty=queueEmpty??throw new ArgumentNullException(nameof(queueEmpty));nextRetry=clock()+RetrySeconds;
        }
        public void ConnectionLost()
        {
            if(Recovering || Expired || channel.PeerLeft || channel.Fault!=null)return;
            Recovering=true;recoverUntil=clock()+RecoverySeconds;
            channel.TransportLost();discardUnsent();
            if(channel is OnlineSetupHostChannel host)host.Begin();
            nextRetry=clock()+RetrySeconds;
        }
        public void Tick()
        {
            if(Expired || channel.PeerLeft || channel.Fault!=null)return;
            double now=clock();
            if(Recovering && channel.Ready && now-channel.LastReceived<SilenceSeconds)Recovering=false;
            if(!Recovering && now-channel.LastReceived>=SilenceSeconds)ConnectionLost();
            if(Recovering && now>=recoverUntil){Expired=true;channel.TransportLost();discardUnsent();return;}
            if(!channel.Ready && channel is OnlineSetupHostChannel host && now>=nextRetry && queueEmpty())
            {nextRetry=now+RetrySeconds;host.ResendHello();}
        }
    }
}
