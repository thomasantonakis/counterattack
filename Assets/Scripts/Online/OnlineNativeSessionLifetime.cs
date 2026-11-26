using System;
using System.Collections.Generic;
namespace CounterAttack.Online
{
    // Native callback handles must be retired before their platform is released.
    public sealed class OnlineNativeSessionLifetime : IDisposable
    {
        private readonly Action releaseNative;
        private readonly List<Lease> leases=new();
        public bool IsDisposed {get;private set;}
        public int OwnedCount=>leases.Count;
        public OnlineNativeSessionLifetime(Action releaseNative)
            =>this.releaseNative=releaseNative??throw new ArgumentNullException(nameof(releaseNative));
        public IDisposable Own(IDisposable resource)
        {
            if(resource==null)throw new ArgumentNullException(nameof(resource));
            if(IsDisposed)throw new ObjectDisposedException(nameof(OnlineNativeSessionLifetime));
            var lease=new Lease(this,resource);leases.Add(lease);return lease;
        }
        public IDisposable Own(Action release)=>Own(new ReleaseAction(release));
        public void Dispose()
        {
            if(IsDisposed)return;IsDisposed=true;Exception failure=null;
            var pending=leases.ToArray();
            for(int i=pending.Length-1;i>=0;i--)
                try{pending[i].Dispose();}catch(Exception error){failure??=error;}
            leases.Clear();
            try{releaseNative();}catch(Exception error){failure=failure==null?error:new AggregateException(failure,error);}
            if(failure!=null)throw failure;
        }
        private sealed class Lease:IDisposable
        {
            private readonly OnlineNativeSessionLifetime owner;
            private IDisposable resource;
            public Lease(OnlineNativeSessionLifetime owner,IDisposable resource){this.owner=owner;this.resource=resource;}
            public void Dispose(){var value=resource;if(value==null)return;resource=null;owner.leases.Remove(this);value.Dispose();}
        }
        private sealed class ReleaseAction:IDisposable
        {
            private Action release;
            public ReleaseAction(Action release)=>this.release=release??throw new ArgumentNullException(nameof(release));
            public void Dispose(){var value=release;release=null;value?.Invoke();}
        }
    }
}
