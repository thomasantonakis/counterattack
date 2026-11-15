using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CounterAttack.Online;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
public sealed class OnlineFullRoomSceneFixture
{
    private OnlineRoomSession session;private string directory;
    private OnlineSharedPause pause;
    private MatchManager previousMatch;
    private object previousBall;
    private bool previousIntro;
    private readonly Dictionary<FieldInfo,object> logValues=new();
    private readonly Dictionary<FieldInfo,DictionaryEntry[]> logMaps=new();
    private readonly Dictionary<FieldInfo,string[]> logSets=new();
    private static readonly FieldInfo BallField=typeof(PlayerToken).GetField("ball",BindingFlags.Static|BindingFlags.NonPublic);
    private static readonly FieldInfo IntroField=typeof(ApplicationManager).GetField("playIntroOnNextRoomLoad",BindingFlags.Instance|BindingFlags.NonPublic);
    private void CaptureGlobals()
    {
        previousMatch=MatchManager.Instance;previousBall=BallField.GetValue(null);previousIntro=(bool)IntroField.GetValue(app);
        foreach(var field in typeof(RoomLiveLogService).GetFields(BindingFlags.Static|BindingFlags.NonPublic).Where(f=>!f.IsLiteral))
        {
            var value=field.GetValue(null);logValues[field]=value;
            if(value is IDictionary map){var entries=new List<DictionaryEntry>();foreach(DictionaryEntry entry in map)entries.Add(entry);logMaps[field]=entries.ToArray();}
            else if(value is HashSet<string> set)logSets[field]=set.ToArray();
        }
    }
    private void RestoreGlobals()
    {
        MatchManager.Instance=previousMatch;BallField.SetValue(null,previousBall);IntroField.SetValue(app,previousIntro);
        foreach(var pair in logValues)
        {
            if(logMaps.TryGetValue(pair.Key,out var entries)){var map=(IDictionary)pair.Value;map.Clear();foreach(var entry in entries)map.Add(entry.Key,entry.Value);}
            else if(logSets.TryGetValue(pair.Key,out var entriesSet)){var set=(HashSet<string>)pair.Value;set.Clear();set.UnionWith(entriesSet);}
            else if(!pair.Key.IsInitOnly)pair.Key.SetValue(null,pair.Value);
        }
    }
    private readonly UnityEngine.Random.State random=UnityEngine.Random.state;
    private readonly float scale=Time.timeScale;
    private ApplicationManager app;private bool created,previousExplicit;private string previousPath,previousMode;
    private MatchManager.GameData Data(int size,bool mirror)
    {
        var v=new OnlineCompatibility(OnlineSessionWire.Version,"full-room-draft","catalog");var p=new OnlineMatchProtocol("full-room-scene","home","away","away",v);
        p.BindAuthenticatedAccount("home","h",v);p.BindAuthenticatedAccount("away","a",v);
        var settings=OnlineSetupAgreementEditorTests.Valid();settings.squadSize=size.ToString();int roll=0;
        var d=new OnlineDraftAuthority(p,settings,OnlineDraftCatalog.Load(),_=>0,()=>++roll%2==1?6:1);
        void Submit(OnlineTeam actor,OnlineDraftIntent intent,string card=null,int slot=0,CoinTossChoice choice=CoinTossChoice.Kickoff)
        {var s=d.Capture();Assert.That(d.Submit(actor==OnlineTeam.Home?"h":"a",new OnlineDraftRequest{matchId=s.matchId,sequence=(actor==OnlineTeam.Home?s.homeSequence:s.awaySequence)+1,revision=s.revision,intent=intent,cardId=card,slot=slot,choice=choice}).Status,Is.EqualTo(OnlineCommandStatus.Accepted));}
        Submit(OnlineTeam.Home,OnlineDraftIntent.Roll);Submit(OnlineTeam.Away,OnlineDraftIntent.Roll);
        while(d.Capture().phase=="pick"){var s=d.Capture();Submit(s.turn,OnlineDraftIntent.Pick,s.cards[0].id,Array.FindIndex(s.turn==OnlineTeam.Home?s.home:s.away,c=>c==null)+1);}
        Submit(OnlineTeam.Home,OnlineDraftIntent.Ready);Submit(OnlineTeam.Away,OnlineDraftIntent.Ready);Submit(OnlineTeam.Away,OnlineDraftIntent.Start);
        Submit(OnlineTeam.Home,OnlineDraftIntent.Roll);Submit(OnlineTeam.Away,OnlineDraftIntent.Roll);Submit(OnlineTeam.Home,OnlineDraftIntent.Choose,choice:mirror?CoinTossChoice.RightToLeft:CoinTossChoice.LeftToRight);
        return OnlineRoomLaunchData.Create(d.Capture());
    }
    private IEnumerator Load(MatchManager.GameData data,bool guest)
    {
        string path=Path.Combine(directory,guest?"replica.json":"authority.json");File.WriteAllText(path,JsonConvert.SerializeObject(data));
        session=new OnlineRoomSession("full-room-scene",guest,data,path);app.SetActiveSaveFilePath(path);
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode(OnlineFullRoomAuthoring.ScenePath,new LoadSceneParameters(LoadSceneMode.Single));yield return null;
    }
    public IEnumerator Run(bool guest,int size,bool mirror,string branch)
    {
        Assert.That(OnlineRoomSession.Current,Is.Null);Assert.That(OnlineFrontendSession.Current,Is.Null);
        app=ApplicationManager.Instance;created=app==null;if(created){ApplicationManager.EnsureInstanceExists();app=ApplicationManager.Instance;}
        CaptureGlobals();previousPath=app.LastSavedFileName;previousExplicit=app.HasExplicitSaveContext;previousMode=app.SelectedRoomGameMode;app.SetSelectedRoomGameMode(ApplicationManager.OnlineGameMode);Time.timeScale=1;
        directory=Path.Combine(Path.GetTempPath(),"CounterattackFullRoom",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        var data=Data(size,mirror);yield return Load(data,branch=="replica"?false:guest);
        var bootstrap=Object.FindAnyObjectByType<OnlineFullRoomBootstrap>();Assert.That(bootstrap.Ready,Is.True);var board=bootstrap.board;
        Assert.That(board.cells.Length,Is.EqualTo(1900));Assert.That(board.cells.All(c=>c.GetComponent<MeshFilter>()?.sharedMesh!=null),Is.True);
        Assert.That(board.grid.IsGridInitialized(),Is.True);Assert.That(board.tokens.Length,Is.EqualTo(size*2));
        Assert.That(board.match.playerTokenManager.allTokens.Count,Is.EqualTo(22));Assert.That(board.match.playerTokenManager.benchTokens.Count,Is.EqualTo((size-11)*2));
        Assert.That(board.tokens.All(t=>!t.enabled),Is.True);Assert.That(board.match.enabled,Is.False);
        Assert.That(board.match.gameData.events,Is.Empty,"Initial formation binding must not create gameplay history");
        foreach(var token in board.tokens)
        {
            var entry=(token.isHomeTeam?data.rosters.home:data.rosters.away)[token.jerseyNumber.ToString()];
            Assert.That(token.playerName,Is.EqualTo(entry.name));Assert.That(token.pace,Is.EqualTo(entry.pace));Assert.That(token.IsGoalKeeper,Is.EqualTo(token.jerseyNumber is 1 or 12));
            if(token.jerseyNumber<=11){Assert.That(token.GetCurrentHex()?.occupyingToken,Is.SameAs(token));Assert.That(token.GetCurrentHex().isAttackOccupied,Is.EqualTo(token.isAttacker));}
        }
        var version=new OnlineCompatibility("ground-wire-v7","ground-rules-v1",OnlineFullRoomBootstrap.Catalog);var protocol=new OnlineMatchProtocol("full-room-scene","home","away","away",version);
        protocol.BindAuthenticatedAccount("home","h",version);protocol.BindAuthenticatedAccount("away","a",version);
        if(!OnlineRoomSession.IsGuest)pause=new OnlineSharedPause(protocol,board.match);
        if(branch=="replica")
        {
            var authority=new OnlineGroundPassAuthority(board.match,protocol);var published=authority.Capture();
            Assert.That(published.tokens.Length,Is.EqualTo(size*2));
            published.homeStats.totalGoals=2;published.awayStats.totalGoals=1;published.regulationSeconds=123;published.half=1;
            var moved=published.tokens.First(t=>t.id=="home:2");moved.x=mirror?12:-12;moved.z=4;
            pause.Dispose();pause=null;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/MainMenu.unity",new LoadSceneParameters(LoadSceneMode.Single));session.Dispose();session=null;MatchManager.Instance=null;
            yield return Load(data,true);board=Object.FindAnyObjectByType<OnlineFullRoomBootstrap>().board;
            var replica=new OnlineGroundPassReplica("full-room-scene",board.tokens.Select(OnlineRollPublication.ActorId),OnlineFullRoomBootstrap.Catalog);
            Assert.That(replica.Apply(published),Is.True);board.Display(replica.State,"Full Room replica");
            var token=board.tokens.Single(t=>t.isHomeTeam && t.jerseyNumber==2);
            Assert.That(token.GetCurrentHex().coordinates,Is.EqualTo(new Vector3Int(moved.x,0,moved.z)));Assert.That(token.transform.position.x,Is.EqualTo(board.grid.GetHexCenterForCoordinates(token.GetCurrentHex().coordinates).x).Within(.001));
            Assert.That(board.match.gameData.stats.homeTeamStats.totalGoals,Is.EqualTo(2));Assert.That(board.match.OnlineRegulationSeconds,Is.EqualTo(123));
            Assert.That(board.liveScoreboard.homeScoreText.text,Is.EqualTo("2"));Assert.That(board.liveScoreboard.awayScoreText.text,Is.EqualTo("1"));
            Assert.That(board.authorityRoot.activeSelf,Is.False);Assert.That(board.match.gameData.events,Is.Empty);Assert.That(board.match.TimingRollSequence,Is.Zero);
            Assert.That(board.match.CaptureOnlineDecisionTiming(),Is.Null,"Replica projection must not create an authority clock");
            yield break;
        }
        Assert.That(board.authorityRoot.activeSelf,Is.EqualTo(!guest));
        if(guest){board.match.AdvanceOnlineAuthorityTime(5);Assert.That(board.match.OnlineRegulationSeconds,Is.Zero);Assert.That(board.match.TimingRollSequence,Is.Zero);Assert.That(board.match.CaptureOnlineDecisionTiming(),Is.Null);yield break;}
        var hostAuthority=new OnlineGroundPassAuthority(board.match,protocol);var state=hostAuthority.Capture();Assert.That(state.stage,Is.EqualTo("kickoff.setup"));Assert.That(state.tokens.Length,Is.EqualTo(size*2));
        if(branch=="kickoff")
        {
            long homeSeq=0,awaySeq=0;
            void Command(string action)
            {var current=hostAuthority.Capture();bool home=current.owner=="Home";var command=new OnlineMatchCommand(current.matchId,home?++homeSeq:++awaySeq,current.revision,current.decisionId,action);Assert.That(hostAuthority.Submit(home?"h":"a",command).Status,Is.EqualTo(OnlineCommandStatus.Accepted));}
            Command(OnlineGroundPassAuthority.KickoffConfirmAction);Command(OnlineGroundPassAuthority.KickoffConfirmAction);
            var takers=hostAuthority.Capture();Assert.That(takers.stage,Is.EqualTo("kickoff.taker"));Command(takers.legalActions.First(a=>a.StartsWith("kickoff.taker:")));
            float deadline=Time.realtimeSinceStartup+8;
            while(hostAuthority.Capture().stage.StartsWith("kickoff.") && Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(hostAuthority.Capture().passActive,Is.True);Assert.That(hostAuthority.Capture().awaitingTarget,Is.True,"Kickoff automatically commits its required standard pass");
            Assert.That(board.match.ball.GetCurrentHex().GetOccupyingToken(),Is.Not.Null);
        }
    }
    public IEnumerator Cleanup()
    {
        if(app==null)yield break;
        pause?.Dispose();pause=null;
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/MainMenu.unity",new LoadSceneParameters(LoadSceneMode.Single));session?.Dispose();session=null;
        if(previousExplicit)app.SetActiveSaveFilePath(previousPath);else{app.ClearActiveSaveContext();app.LastSavedFileName=previousPath;}app.SetSelectedRoomGameMode(previousMode);
        RestoreGlobals();if(created)Object.Destroy(app.gameObject);UnityEngine.Random.state=random;Time.timeScale=scale;
        if(directory!=null)
        {
            string full=Path.GetFullPath(directory),parent=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"CounterattackFullRoom"))+Path.DirectorySeparatorChar;
            if(!full.StartsWith(parent,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Full Room fixture cleanup escaped owned temporary root");Directory.Delete(full,true);
        }
        yield return null;
    }
}
