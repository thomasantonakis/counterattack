using System;

namespace CounterAttack.Online
{
    [Serializable]
    public sealed class OnlineRoomBannerEvent
    {
        public long sequence;
        public MatchBannerEvent kind;
        public string detail;
    }

    [Serializable]
    public sealed class OnlineRoomBannerPublication
    {
        internal const int EventLimit = 12;
        internal const int CaptionLimit = 128;
        internal const int DetailLimit = 512;

        public OnlineRoomBannerEvent[] events;
        public string timerCaption, timerDetail;
        public bool timerWarning;

        internal static bool Valid(OnlineRoomBannerPublication publication)
        {
            if (publication == null) return true;
            if (publication.events == null || publication.events.Length > EventLimit
                || publication.timerCaption?.Length > CaptionLimit
                || publication.timerDetail?.Length > DetailLimit) return false;

            long previous = 0;
            foreach (var entry in publication.events)
            {
                if (entry == null || entry.sequence <= previous || entry.detail?.Length > DetailLimit
                    || !Enum.IsDefined(typeof(MatchBannerEvent), entry.kind)) return false;
                previous = entry.sequence;
            }
            return true;
        }
    }
}
