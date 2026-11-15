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
        {
            if(data==null)throw new ArgumentNullException(nameof(data));
            if(ApplicationManager.Instance==null)throw new InvalidOperationException("Online Room requires the existing application context");
            if(!Path.IsPathFullyQualified(directory))throw new ArgumentException("An absolute online run directory is required",nameof(directory));
            SavePath=Path.Combine(directory,guest?"room-replica.json":"room-authority.json");
            Session=new OnlineRoomSession(matchId,guest,data,SavePath);
            try
            {
                Directory.CreateDirectory(directory);
                File.WriteAllText(SavePath,JsonConvert.SerializeObject(Session.CaptureInitialData(),Formatting.Indented));
                context=ApplicationManager.Instance.BeginOnlineRoomSaveContext(SavePath);
            }
            catch{Session.Dispose();Session=null;throw;}
        }
        public void Dispose()
        {Session?.Dispose();Session=null;context?.Dispose();context=null;}
    }
}
