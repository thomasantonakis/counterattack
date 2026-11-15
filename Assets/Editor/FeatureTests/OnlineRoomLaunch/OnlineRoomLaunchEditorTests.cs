using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CounterAttack.Online;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;

[Category("OnlineRoomLaunch")]
public sealed class OnlineRoomLaunchEditorTests
{
    private OnlineMatchProtocol protocol;private OnlineDraftAuthority authority;
    private readonly Queue<int> rolls=new();
    private OnlineDraftState Create(bool hostAway,int size)
    {
        Assert.That(OnlineRoomSession.Current,Is.Null,"Do not replace an active Room session");rolls.Clear();
        var v=new OnlineCompatibility(OnlineSessionWire.Version,"room-launch","catalog");protocol=new("room-launch","home","away",hostAway?"away":"home",v);
        protocol.BindAuthenticatedAccount("home","h",v);protocol.BindAuthenticatedAccount("away","a",v);
        var s=OnlineSetupAgreementEditorTests.Valid();s.squadSize=size.ToString();
        authority=new(protocol,s,OnlineDraftCatalog.Load(),_=>0,()=>rolls.Dequeue());
        rolls.Enqueue(6);rolls.Enqueue(1);Submit(OnlineTeam.Home,OnlineDraftIntent.Roll);Submit(OnlineTeam.Away,OnlineDraftIntent.Roll);
        for(int i=0;i<40 && authority.Capture().phase=="pick";i++)
        {var state=authority.Capture();var roster=state.turn==OnlineTeam.Home?state.home:state.away;Submit(state.turn,OnlineDraftIntent.Pick,state.cards[0].id,Array.FindIndex(roster,c=>c==null)+1);}
        Submit(OnlineTeam.Home,OnlineDraftIntent.Ready);Submit(OnlineTeam.Away,OnlineDraftIntent.Ready);Submit(protocol.HostTeam,OnlineDraftIntent.Start);
        rolls.Enqueue(1);rolls.Enqueue(6);Submit(OnlineTeam.Home,OnlineDraftIntent.Roll);Submit(OnlineTeam.Away,OnlineDraftIntent.Roll);
        Submit(OnlineTeam.Away,OnlineDraftIntent.Choose,choice:CoinTossChoice.Kickoff);Submit(OnlineTeam.Home,OnlineDraftIntent.Choose,choice:CoinTossChoice.LeftToRight);
        return authority.Capture();
    }
    private void Submit(OnlineTeam actor,OnlineDraftIntent intent,string card=null,int slot=0,CoinTossChoice choice=CoinTossChoice.Kickoff)
    {
        var s=authority.Capture();var r=new OnlineDraftRequest{matchId=s.matchId,revision=s.revision,sequence=(actor==OnlineTeam.Home?s.homeSequence:s.awaySequence)+1,intent=intent,cardId=card,slot=slot,choice=choice};
        Assert.That(authority.Submit(actor==OnlineTeam.Home?"h":"a",r).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
    }
    [Test] public void CompleteDraftBuildsRealMatchData([Values(false,true)]bool hostAway,[Values(16,18)]int size,[Values("attributes","opening","detached","rebind","reject","host-load","guest-load","save-isolation","stats-link")]string branch)
    {
        var ready=Create(hostAway,size);var data=OnlineRoomLaunchData.Create(ready);
        Assert.That(data.rosters.home.Count,Is.EqualTo(size));Assert.That(data.rosters.away.Count,Is.EqualTo(size));
        Assert.That(data.gameSettings.referee,Is.EqualTo("Webster - 2"));Assert.That(data.gameSettings.gameMode,Is.EqualTo("Online"));
        Assert.That(data.playSessionId,Is.EqualTo(ready.roomSessionId));Assert.That(data.runtimeSnapshot,Is.Null);Assert.That(data.sourceSavePath,Is.Null);
        if(branch=="attributes")
        {
            foreach(var pair in new[]{(ready.home,data.rosters.home),(ready.away,data.rosters.away)})for(int i=0;i<size;i++)
            {
                var source=pair.Item1[i];var player=pair.Item2[(i+1).ToString()];Assert.That(player.name,Is.EqualTo(source.name));Assert.That(player.pace,Is.EqualTo(source.pace));
                Assert.That(player.heading,Is.EqualTo(source.heading));Assert.That(player.shooting,Is.EqualTo(source.shooting));Assert.That(player.saving,Is.EqualTo(source.saving));Assert.That(player.handling,Is.EqualTo(source.handling));
            }
            Assert.That(data.stats.homeTeamStats.totalGoals+data.stats.awayTeamStats.totalGoals,Is.Zero);Assert.That(data.events,Is.Empty);return;
        }
        if(branch=="opening")
        {Assert.That(data.openingSetup.KickoffTeam,Is.EqualTo(MatchManager.TeamInAttack.Away));Assert.That(data.openingSetup.DirectionFor(true),Is.EqualTo(MatchManager.TeamAttackingDirection.LeftToRight));return;}
        if(branch=="detached")
        {
            data.rosters.home["1"].saving=999;data.gameSettings.homeTeamName="Changed";data.openingSetup.homeHasFirstHalfKickoff=true;
            var again=OnlineRoomLaunchData.Create(ready);Assert.That(again.rosters.home["1"].saving,Is.Not.EqualTo(999));Assert.That(again.gameSettings.homeTeamName,Is.EqualTo(ready.settings.homeTeamName));Assert.That(again.openingSetup.homeHasFirstHalfKickoff,Is.False);return;
        }
        if(branch=="rebind")
        {
            protocol.Disconnect("a");protocol.BindAuthenticatedAccount("away","a2",protocol.Compatibility);var returned=authority.Capture();
            Assert.That(returned.roomSessionId,Is.EqualTo(ready.roomSessionId));Assert.That(returned.roomStartedUtc,Is.EqualTo(ready.roomStartedUtc));
            Assert.That(OnlineRoomLaunchData.Create(returned).gameSettings.referee,Is.EqualTo(data.gameSettings.referee));return;
        }
        if(branch=="reject")
        {
            var invalid=OnlineDraftAuthority.Copy(ready);invalid.phase="review";Assert.Throws<ArgumentException>(()=>OnlineRoomLaunchData.Create(invalid));
            invalid=OnlineDraftAuthority.Copy(ready);invalid.connected=false;Assert.Throws<ArgumentException>(()=>OnlineRoomLaunchData.Create(invalid));
            invalid=OnlineDraftAuthority.Copy(ready);invalid.home[2]=null;Assert.Throws<ArgumentException>(()=>OnlineRoomLaunchData.Create(invalid));
            invalid=OnlineDraftAuthority.Copy(ready);invalid.roomSessionId="bad";Assert.Throws<ArgumentException>(()=>OnlineRoomLaunchData.Create(invalid));
            invalid=OnlineDraftAuthority.Copy(ready);invalid.settings.referee="Random";Assert.Throws<ArgumentException>(()=>OnlineRoomLaunchData.Create(invalid));return;
        }
        ExerciseRealLoadAndPersistence(data,branch);
    }
    private static void ExerciseRealLoadAndPersistence(MatchManager.GameData data,string branch)
    {
        var oldApp=ApplicationManager.Instance;ApplicationManager.EnsureInstanceExists();var app=ApplicationManager.Instance;
        string previousPath=app.LastSavedFileName;bool explicitContext=app.HasExplicitSaveContext;var previousMatch=MatchManager.Instance;
        string directory=Path.Combine(Path.GetTempPath(),"CounterattackOnlineLaunch",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        string sentinel=Path.Combine(directory,"previous-local-slot.json"),owned=Path.Combine(directory,"online-session.json");
        File.WriteAllText(sentinel,"must-never-be-read-or-written");File.WriteAllText(owned,JsonConvert.SerializeObject(data));app.LastSavedFileName=sentinel;
        var root=new GameObject("Online Room bootstrap verification");OnlineRoomSession session=null;
        const BindingFlags flags=BindingFlags.Static|BindingFlags.NonPublic;
        var logFields=typeof(RoomLiveLogService).GetFields(flags).Where(f=>!f.IsLiteral).ToArray();
        var values=logFields.ToDictionary(f=>f,f=>f.GetValue(null));
        DictionaryEntry[] Entries(IDictionary map){var entries=new List<DictionaryEntry>();var enumerator=map.GetEnumerator();while(enumerator.MoveNext())entries.Add(enumerator.Entry);return entries.ToArray();}
        var maps=values.Where(p=>p.Value is IDictionary).ToDictionary(p=>p.Key,p=>Entries((IDictionary)p.Value));
        var sets=values.Where(p=>p.Value is HashSet<string>).ToDictionary(p=>p.Key,p=>((HashSet<string>)p.Value).ToArray());
        try
        {
            bool guest=branch=="guest-load";session=new OnlineRoomSession("room-launch",guest,data,guest?null:owned);
            var match=root.AddComponent<MatchManager>();match.LoadGameSettingsFromJson();
            Assert.That(match.gameData.rosters.home.Count,Is.EqualTo(data.rosters.home.Count));Assert.That(match.gameData.playSessionId,Is.EqualTo(data.playSessionId));
            Assert.That(app.LastSavedFileName,Is.EqualTo(sentinel));Assert.That(app.HasExplicitSaveContext,Is.EqualTo(explicitContext));
            match.gameData.rosters.home["1"].name="Mutable runtime copy";Assert.That(session.CaptureInitialData().rosters.home["1"].name,Is.Not.EqualTo("Mutable runtime copy"));
            if(branch=="stats-link")
            {
                MatchManager.Instance=match;
                var tokenObject=new GameObject("Online home nine");tokenObject.transform.SetParent(root.transform);var token=tokenObject.AddComponent<PlayerToken>();token.isHomeTeam=true;token.jerseyNumber=9;token.playerName=match.gameData.rosters.home["9"].name;
                match.gameData.gameLog.LogEvent(token,MatchManager.ActionType.PassAttempt);
                Assert.That(match.gameData.stats.homeTeamStats.totalPassesAttempted,Is.EqualTo(1));
                Assert.That(match.gameData.stats.GetPlayerStats(token).passesAttempted,Is.EqualTo(1));Assert.That(session.CaptureInitialData().stats.homeTeamStats.totalPassesAttempted,Is.Zero);
            }
            if(branch=="save-isolation")
            {
                Assert.That(RoomSaveService.SaveInPlace(match,out _,out string reason),Is.False);Assert.That(reason,Does.Contain("session"));
                Assert.That(RoomSaveService.SaveAs(match,"online-must-not-escape",true,out _,out _),Is.False);
                Assert.That(RoomSaveService.SaveToPath(match,sentinel,true,out _,out _),Is.False);
                Assert.That(RoomLiveLogService.AppendActiveLiveEvents(match,out _,out _),Is.True);
                Assert.That(File.Exists(Path.Combine(directory,"online-session__Live Events Index.json")),Is.True);
                Assert.That(Directory.GetFiles(directory).Any(p=>Path.GetFileName(p).StartsWith("previous-local-slot__")),Is.False,"Companions must also stay out of the old slot");
            }
            Assert.That(File.ReadAllText(sentinel),Is.EqualTo("must-never-be-read-or-written"));
        }
        finally
        {
            Object.DestroyImmediate(root);session?.Dispose();app.LastSavedFileName=previousPath;MatchManager.Instance=previousMatch;
            foreach(var pair in values)
            {
                if(maps.TryGetValue(pair.Key,out var entries)){var target=(IDictionary)pair.Value;target.Clear();foreach(var entry in entries)target.Add(entry.Key,entry.Value);}
                else if(sets.TryGetValue(pair.Key,out var members)){var target=(HashSet<string>)pair.Value;target.Clear();target.UnionWith(members);}
                else if(!pair.Key.IsInitOnly)pair.Key.SetValue(null,pair.Value);
            }
            if(oldApp==null)Object.DestroyImmediate(app.gameObject);
            string resolved=Path.GetFullPath(directory),parent=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"CounterattackOnlineLaunch"))+Path.DirectorySeparatorChar;
            if(!resolved.StartsWith(parent,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Temporary fixture cleanup escaped its owned directory");
            Directory.Delete(resolved,true);
        }
    }
}
