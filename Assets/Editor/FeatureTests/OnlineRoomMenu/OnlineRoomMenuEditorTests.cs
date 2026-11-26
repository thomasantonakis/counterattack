using System;
using System.Collections;
using System.IO;
using System.Reflection;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;
[Category("OnlineRoomMenu")]
public sealed class OnlineRoomMenuEditorTests
{
    private sealed class Flow:IEnumerator,IDisposable
    {private readonly Action dispose;public Flow(Action action){dispose=action;}public object Current=>null;public bool MoveNext()=>false;public void Reset()=>throw new NotSupportedException();public void Dispose()=>dispose();}
    [Test] public void CloseRestoresContextBeforeSceneUnload([Values(false,true)]bool guest)
    {
        Assert.That(OnlineFrontendSession.Current,Is.Null);Assert.That(OnlineRoomSession.Current,Is.Null);
        var original=ApplicationManager.Instance;ApplicationManager.EnsureInstanceExists();var app=ApplicationManager.Instance;
        string directory=Path.Combine(Path.GetTempPath(),"CounterattackMenu",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        var outer=app.BeginOnlineRoomSaveContext(Path.Combine(directory,"prior-context.json"));var root=new GameObject("Owned menu teardown fixture");
        var frontend=root.AddComponent<OnlineFrontendSession>();OnlineRoomLaunchLifetime lifetime=null;bool released=false;
        const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
        try
        {
            lifetime=new OnlineRoomLaunchLifetime("menu",guest,new MatchManager.GameData(),directory);
            typeof(OnlineFrontendSession).GetField("roomLifetime",flags).SetValue(frontend,lifetime);
            typeof(OnlineFrontendSession).GetField("gameplay",flags).SetValue(frontend,new Flow(()=>released=true));
            typeof(OnlineFrontendSession).GetProperty("Busy").SetValue(frontend,true);
            frontend.Close();Assert.That(released,Is.True,"Controls may be destroyed immediately after Close returns");
            Assert.That(OnlineRoomSession.Current,Is.Null);Assert.That(app.LastSavedFileName,Is.EqualTo(Path.Combine(directory,"prior-context.json")));
            frontend.Close();Assert.That(OnlineRoomSession.Current,Is.Null);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);lifetime?.Dispose();outer.Dispose();if(original==null)UnityEngine.Object.DestroyImmediate(app.gameObject);
            string resolved=Path.GetFullPath(directory),parent=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"CounterattackMenu"))+Path.DirectorySeparatorChar;
            if(!resolved.StartsWith(parent,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Fixture cleanup escaped owned root");Directory.Delete(resolved,true);
        }
    }
}
