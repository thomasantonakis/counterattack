using System;
namespace CounterAttack.Online
{
    // Receives SDK expiry notices; native Login completion never throws through
    // the SDK callback. The owner checks Fault from its regular pump instead.
    public sealed class OnlineAuthRenewal:IDisposable
    {
        private readonly string user;
        private readonly Func<double> clock;
        private readonly Action<Action<bool,string,string>> login;
        private bool disposed,requested,pending;
        private int version;
        private double expires,callbackDeadline,retryAt;
        private string lastFailure;
        public int SuccessfulRefreshes {get;private set;}
        public bool Pending=>requested;
        public string Fault {get;private set;}
        public OnlineAuthRenewal(string user,Func<double> clock,Action<Action<bool,string,string>> login)
        {
            if(string.IsNullOrWhiteSpace(user))throw new ArgumentException("Authenticated identity is required",nameof(user));
            this.user=user;this.clock=clock??throw new ArgumentNullException(nameof(clock));this.login=login??throw new ArgumentNullException(nameof(login));
        }
        public bool NotifyExpiring(string identity)
        {
            if(disposed || Fault!=null || requested || identity!=user)return false;
            requested=true;expires=clock()+50;retryAt=clock();lastFailure=null;return true;
        }
        public void Tick()
        {
            if(disposed || !requested || Fault!=null)return;
            double now=clock();
            if(now>=expires){Fail("EOS Connect authentication renewal timed out"+(lastFailure==null?"":": "+lastFailure));return;}
            if(pending)
            {
                if(now<callbackDeadline)return;
                pending=false;version++;lastFailure="Login callback timed out";retryAt=now+5;
            }
            if(now<retryAt)return;
            pending=true;int attempt=++version;callbackDeadline=Math.Min(expires,now+20);
            try{login((success,identity,result)=>Complete(attempt,success,identity,result));}
            catch(Exception error)
            {
                if(disposed || attempt!=version || !pending)return;
                pending=false;lastFailure=error.GetType().Name;retryAt=clock()+5;
            }
        }
        private void Complete(int attempt,bool success,string identity,string result)
        {
            if(disposed || attempt!=version || !pending)return;
            pending=false;
            if(success && identity==user){requested=false;SuccessfulRefreshes++;lastFailure=null;return;}
            if(success){Fail("EOS Connect authentication renewal changed the authenticated identity");return;}
            lastFailure=result;retryAt=clock()+5;
        }
        private void Fail(string message){Fault=message;requested=pending=false;version++;}
        public void Dispose(){if(disposed)return;disposed=true;requested=pending=false;version++;}
    }
}
