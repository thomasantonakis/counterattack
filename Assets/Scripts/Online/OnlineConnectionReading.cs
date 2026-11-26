using System.Collections.Generic;
using System.Linq;

namespace CounterAttack.Online
{
    public enum OnlineConnectionQuality
    {
        Waiting,
        Measuring,
        Good,
        Average,
        Poor,
        Reconnecting,
        Disconnected
    }

    public readonly struct OnlineConnectionReading
    {
        public OnlineConnectionQuality Quality { get; }
        public double? PingMilliseconds { get; }

        private OnlineConnectionReading(OnlineConnectionQuality quality, double? pingMilliseconds)
        {
            Quality = quality;
            PingMilliseconds = pingMilliseconds;
        }

        internal static OnlineConnectionReading Rate(IEnumerable<double> recentReplies, double silenceSeconds)
        {
            var samples = recentReplies.OrderBy(milliseconds => milliseconds).ToArray();
            const double poorSilenceSeconds = 3.5;
            if (samples.Length == 0)
            {
                var quality = silenceSeconds >= poorSilenceSeconds
                    ? OnlineConnectionQuality.Poor : OnlineConnectionQuality.Measuring;
                return new OnlineConnectionReading(quality, null);
            }

            double median = (samples[(samples.Length - 1) / 2] + samples[samples.Length / 2]) / 2;
            if (median > 350 || silenceSeconds >= poorSilenceSeconds)
                return new OnlineConnectionReading(OnlineConnectionQuality.Poor, median);

            bool repeatedSpikes = samples.Count(milliseconds => milliseconds >= median + 100) >= 2;
            bool average = median > 180 || silenceSeconds >= 2.5 || repeatedSpikes;
            return new OnlineConnectionReading(average ? OnlineConnectionQuality.Average : OnlineConnectionQuality.Good, median);
        }
    }
}
