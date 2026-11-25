using System;
using System.IO;
using Newtonsoft.Json;
namespace CounterAttack.Online
{
    // One owned lifetime spans scene loading, gameplay exchange and teardown.
    public sealed class OnlineRoomLaunchLifetime : IDisposable
    {
        public OnlineRoomSession Session {get;private set;}
        public string SavePath {get;}
        private IDisposable context;
        public OnlineRoomLaunchLifetime(string matchId,bool guest,MatchManager.GameData data,string directory)
            : this(matchId, guest, data, directory, false, null, null)
        {
        }

        internal static OnlineRoomLaunchLifetime Resume(string matchId, bool guest, MatchManager.GameData data,
            string directory, OnlineCheckpoint source, Guid? slot)
        {
            if (guest ? !OnlineRoomLaunchData.IsResumeBootstrap(data) : source == null || !slot.HasValue
                || data?.runtimeSnapshot == null)
                throw new ArgumentException("A validated resume checkpoint is required.");
            return new OnlineRoomLaunchLifetime(matchId, guest, data, directory, true, source, slot);
        }

        private OnlineRoomLaunchLifetime(string matchId,bool guest,MatchManager.GameData data,string directory,
            bool resume, OnlineCheckpoint source, Guid? slot)
        {
            if(data==null)throw new ArgumentNullException(nameof(data));
            if(ApplicationManager.Instance==null)throw new InvalidOperationException("Online Room requires the existing application context");
            if(!Path.IsPathFullyQualified(directory))throw new ArgumentException("An absolute online run directory is required",nameof(directory));
            SavePath=Path.Combine(directory,guest?"room-replica.json":"room-authority.json");
            Session=new OnlineRoomSession(matchId,guest,data,SavePath,showOpeningIntro:true,
                resume:resume,selectedSlot:slot,durableMatch:source?.matchId);
            try
            {
                Directory.CreateDirectory(directory);
                File.WriteAllText(SavePath,JsonConvert.SerializeObject(Session.CaptureInitialData(),Formatting.Indented));
                if (source != null)
                    File.WriteAllText(Path.Combine(directory, "checkpoint-provenance.json"),
                        JsonConvert.SerializeObject(new {source.matchId, source.checkpointId, source.savedUtc, slot}));
                context=ApplicationManager.Instance.BeginOnlineRoomSaveContext(SavePath);
            }
            catch{Session.Dispose();Session=null;throw;}
        }
        public void Dispose()
        {Session?.Dispose();Session=null;context?.Dispose();context=null;}
    }
}
