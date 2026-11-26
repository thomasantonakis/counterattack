using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;
[Category("OnlineRoomHandoff")]
public sealed class OnlineRoomHandoffEditorTests
{
    [Test] public void SessionOwnsAndRestoresSaveContext([Values(false,true)]bool guest,[Values(false,true)]bool explicitContext,[Values(false,true)]bool intro)
    {
        Assert.That(OnlineRoomSession.Current,Is.Null);
        var original=ApplicationManager.Instance;ApplicationManager.EnsureInstanceExists();var app=ApplicationManager.Instance;
        string directory=Path.Combine(Path.GetTempPath(),"CounterattackHandoff",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        var restore=app.BeginOnlineRoomSaveContext(Path.Combine(directory,"outer.json"));OnlineRoomLaunchLifetime lifetime=null;
        try
        {
            string sentinel=Path.Combine(directory,"local-slot.json");File.WriteAllText(sentinel,"untouched");
            if(explicitContext)app.SetActiveSaveFilePath(sentinel);else{app.ClearActiveSaveContext();app.LastSavedFileName=sentinel;}
            app.SetSelectedRoomGameMode(ApplicationManager.SinglePlayerGameMode);if(intro)app.PrepareDraftRoomIntro();
            var data=new MatchManager.GameData();data.gameSettings.gameMode=ApplicationManager.OnlineGameMode;data.playSessionId=Guid.NewGuid().ToString("N");
            lifetime=new OnlineRoomLaunchLifetime("handoff",guest,data,directory);
            Assert.That(app.GetLastSavedFilePath(),Is.EqualTo(lifetime.SavePath));Assert.That(app.HasExplicitSaveContext,Is.True);
            Assert.That(app.SelectedRoomGameMode,Is.EqualTo(ApplicationManager.OnlineGameMode));Assert.That(File.ReadAllText(lifetime.SavePath),Does.Contain(data.playSessionId));
            Assert.That(lifetime.Session.GuestReplica,Is.EqualTo(guest));Assert.That(lifetime.Session.AuthoritySavePath,Is.EqualTo(guest?null:lifetime.SavePath));
            data.gameSettings.homeTeamName="changed after handoff";Assert.That(lifetime.Session.CaptureInitialData().gameSettings.homeTeamName,Is.Not.EqualTo(data.gameSettings.homeTeamName));
            lifetime.Dispose();lifetime.Dispose();Assert.That(OnlineRoomSession.Current,Is.Null);
            Assert.That(app.LastSavedFileName,Is.EqualTo(sentinel));Assert.That(app.HasExplicitSaveContext,Is.EqualTo(explicitContext));
            Assert.That(app.SelectedRoomGameMode,Is.EqualTo(ApplicationManager.SinglePlayerGameMode));Assert.That(app.ConsumeDraftRoomIntro(),Is.EqualTo(intro));
            Assert.That(File.ReadAllText(sentinel),Is.EqualTo("untouched"));
        }
        finally
        {
            lifetime?.Dispose();restore.Dispose();if(original==null)UnityEngine.Object.DestroyImmediate(app.gameObject);
            string resolved=Path.GetFullPath(directory),parent=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"CounterattackHandoff"))+Path.DirectorySeparatorChar;
            if(!resolved.StartsWith(parent,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Fixture cleanup escaped owned root");Directory.Delete(resolved,true);
        }
    }
    [Test] public void FailedLaunchLeavesNoActiveSession([Values(false,true)]bool guest)
    {
        Assert.That(OnlineRoomSession.Current,Is.Null);var original=ApplicationManager.Instance;ApplicationManager.EnsureInstanceExists();var app=ApplicationManager.Instance;
        string path=Path.GetTempFileName(),prior=app.LastSavedFileName;bool explicitContext=app.HasExplicitSaveContext;
        try
        {
            Assert.Throws<ArgumentNullException>(()=>new OnlineRoomLaunchLifetime("handoff",guest,null,path));
            Assert.Throws<IOException>(()=>new OnlineRoomLaunchLifetime("handoff",guest,new MatchManager.GameData(),path));
            Assert.That(OnlineRoomSession.Current,Is.Null);Assert.That(app.LastSavedFileName,Is.EqualTo(prior));Assert.That(app.HasExplicitSaveContext,Is.EqualTo(explicitContext));
            Assert.Throws<ArgumentException>(()=>new OnlineRoomLaunchLifetime("handoff",guest,new MatchManager.GameData(),"relative"));Assert.That(OnlineRoomSession.Current,Is.Null);
        }
        finally{File.Delete(path);if(original==null)UnityEngine.Object.DestroyImmediate(app.gameObject);}
    }
    private sealed class TraceFlow:IEnumerator,IDisposable
    {
        private readonly Action dispose;public TraceFlow(Action action){dispose=action;}
        public object Current=>null;public bool MoveNext()=>false;public void Reset()=>throw new NotSupportedException();public void Dispose()=>dispose();
    }
    [Test] public void RejectsFrontendWithoutFullRoomHandoff()
    {
        var current=OnlineFrontendSessionPolicy.Compatibility();
        var previous=new OnlineCompatibility(current.Protocol,"frontend-rules-v5",current.Catalog);
        var protocol=new OnlineMatchProtocol("handoff-version","home","away","away",current);
        Assert.That(current.Rules,Is.Not.EqualTo(previous.Rules));
        Assert.That(protocol.BindAuthenticatedAccount("home","old-guest",previous),Is.False);
        Assert.That(protocol.BindAuthenticatedAccount("home","new-guest",current),Is.True);
    }
    [Test] public void NativeTeardownDisposesGameplayBeforeProbe([Values(false,true)]bool failingGameplayCleanup)
    {
        Assert.That(OnlineFrontendSession.Current,Is.Null);var trace=new List<string>();
        var root=new GameObject("Owned native teardown fixture");var frontend=root.AddComponent<OnlineFrontendSession>();
        const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
        typeof(OnlineFrontendSession).GetField("probe",flags).SetValue(frontend,new TraceFlow(()=>trace.Add("probe")));
        typeof(OnlineFrontendSession).GetField("gameplay",flags).SetValue(frontend,new TraceFlow(()=>{trace.Add("gameplay");if(failingGameplayCleanup)throw new InvalidOperationException("Owned cleanup failure");}));
        try
        {
            var run=(IEnumerator)typeof(OnlineFrontendSession).GetMethod("Run",flags).Invoke(frontend,null);
            Assert.DoesNotThrow(()=>Assert.That(run.MoveNext(),Is.False));
            Assert.That(trace,Is.EqualTo(new[]{"gameplay","probe"}),"Gameplay subscriptions must be released while their EOS probe is alive");
            Assert.That(typeof(OnlineFrontendSession).GetField("gameplay",flags).GetValue(frontend),Is.Null);
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
}
