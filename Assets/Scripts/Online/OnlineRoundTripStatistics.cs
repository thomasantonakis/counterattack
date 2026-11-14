using System;
using System.Collections.Generic;
using System.Linq;
namespace CounterAttack.Online
{
    [Serializable] public sealed class OnlineLatencySummary { public int samples;public double lastMilliseconds,p50Milliseconds,p95Milliseconds; }
    public sealed class OnlineRoundTripStatistics
    {
        private readonly Func<double> clock;
        private readonly Dictionary<long,double> pending=new();
        private readonly Queue<long> sent=new();
        private readonly Queue<double> samples=new();
        public OnlineRoundTripStatistics(Func<double> monotonicSeconds)=>clock=monotonicSeconds??throw new ArgumentNullException(nameof(monotonicSeconds));
        public bool Sent(long sequence)
        {
            if(sequence<1 || pending.ContainsKey(sequence))return false;
            var now=clock();if(double.IsNaN(now) || double.IsInfinity(now))return false;
            pending.Add(sequence,now);sent.Enqueue(sequence);
            while(sent.Count>64)pending.Remove(sent.Dequeue());return true;
        }
        public bool Replied(long sequence)
        {
            if(!pending.TryGetValue(sequence,out var start))return false;
            pending.Remove(sequence);double milliseconds=(clock()-start)*1000;
            if(double.IsNaN(milliseconds) || double.IsInfinity(milliseconds) || milliseconds<0)return false;
            samples.Enqueue(milliseconds);while(samples.Count>128)samples.Dequeue();return true;
        }
        public OnlineLatencySummary Capture()
        {
            var values=samples.OrderBy(v=>v).ToArray();
            double Percentile(double p)
            {
                if(values.Length==0)return 0;double index=(values.Length-1)*p;int low=(int)index;
                return values[low]+(values[Math.Min(low+1,values.Length-1)]-values[low])*(index-low);
            }
            return new OnlineLatencySummary{samples=values.Length,lastMilliseconds=samples.Count==0?0:samples.Last(),p50Milliseconds=Percentile(.5),p95Milliseconds=Percentile(.95)};
        }
    }
}
