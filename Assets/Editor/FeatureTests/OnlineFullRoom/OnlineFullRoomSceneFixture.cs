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
public sealed partial class OnlineFullRoomSceneFixture
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
        var data=Data(size,mirror);yield return Load(data,branch is "replica" or "status"?false:guest);
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
        if(branch.StartsWith("menu")){yield return RunMenu(board,guest,mirror,branch);yield break;}
        if(branch=="context-key-focus"){yield return RunContextKeyFocus(board);yield break;}
        var version=new OnlineCompatibility("ground-wire-v7","ground-rules-v1",OnlineFullRoomBootstrap.Catalog);var protocol=new OnlineMatchProtocol("full-room-scene","home","away","away",version);
        protocol.BindAuthenticatedAccount("home","h",version);protocol.BindAuthenticatedAccount("away","a",version);
        if(!OnlineRoomSession.IsGuest)pause=new OnlineSharedPause(protocol,board.match);
        if(branch is "replica" or "status")
        {
            if(branch=="status")PrepareRosterStatus(board);
            var authority=new OnlineGroundPassAuthority(board.match,protocol);var published=authority.Capture();
            Assert.That(published.tokens.Length,Is.EqualTo(size*2));
            published.homeStats.totalGoals=2;published.awayStats.totalGoals=1;published.regulationSeconds=123;published.half=1;
            var moved=published.tokens.First(t=>t.id=="home:2");moved.x=mirror?12:-12;moved.z=4;
            if(moved.runtime!=null){moved.runtime.currentHex.x=moved.x;moved.runtime.currentHex.z=moved.z;}
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
            if(branch=="status")
            {
                AssertRosterStatus(board,data,mirror);
                session.Dispose();session=null;
                var banner=Object.FindAnyObjectByType<MatchEventBannerController>();
                Assert.That(banner,Is.Not.Null);
                typeof(MatchEventBannerController).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(banner,null);
                Assert.That(board.match.CaptureOnlineDecisionTiming(),Is.Null,"Closing a replica must not turn its presentation into an authority clock");
            }
            yield break;
        }
        Assert.That(board.authorityRoot.activeSelf,Is.EqualTo(!guest));
        if(guest){board.match.AdvanceOnlineAuthorityTime(5);Assert.That(board.match.OnlineRegulationSeconds,Is.Zero);Assert.That(board.match.TimingRollSequence,Is.Zero);Assert.That(board.match.CaptureOnlineDecisionTiming(),Is.Null);yield break;}
        var hostAuthority=new OnlineGroundPassAuthority(board.match,protocol);var state=hostAuthority.Capture();Assert.That(state.stage,Is.EqualTo("kickoff.setup"));Assert.That(state.tokens.Length,Is.EqualTo(size*2));
        if(branch.StartsWith("compact-pose")){yield return RunCompactPresentation(board,protocol,data,branch=="compact-pose-boundary");yield break;}
        if(branch.StartsWith("board-input:")){yield return RunBoardInput(board,protocol,data,branch.Substring("board-input:".Length));yield break;}
        if(branch.StartsWith("conclusion:")){var parts=branch.Split(':');yield return RunConclusion(board,protocol,data,int.Parse(parts[1]),int.Parse(parts[2]),bool.Parse(parts[3]));yield break;}
        if(branch.StartsWith("ordinary-half:")){yield return RunNormalOnlineHalf(board,protocol,int.Parse(branch.Split(':')[1]));yield break;}
        if(branch.StartsWith("room-coin:")){var parts=branch.Split(':');yield return RunRoomCoin(board,protocol,data,bool.Parse(parts[1]),bool.Parse(parts[2]),bool.Parse(parts[3]),bool.Parse(parts[4]),bool.Parse(parts[5]),parts.Length>6 && bool.Parse(parts[6]));yield break;}
        if(branch.StartsWith("shootout:")){var parts=branch.Split(':');yield return RunShootout(board,protocol,data,bool.Parse(parts[1]),bool.Parse(parts[2]),parts.Length>3?parts[3]:null);yield break;}
        if(branch=="snapshot-pose"){yield return RunSnapshotPose(board,protocol,data);yield break;}
        if(branch=="transport"){yield return RunTransport(board,protocol,size);yield break;}
        if(branch.StartsWith("required-keeper:")){var parts=branch.Split(':');yield return RunRequiredKeeper(board,protocol,data,bool.Parse(parts[1]),parts[2],bool.Parse(parts[3]));yield break;}
        if(branch.StartsWith("substitution-limit-")){yield return RunSubstitutionLimit(board,protocol,data,branch.EndsWith("home"));yield break;}
        if(branch.StartsWith("substitution-")){yield return RunSubstitution(board,protocol,data,branch.EndsWith("home"),branch.Contains("-ui-"),branch.Contains("-keeper-"));yield break;}
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
    private IEnumerator RunSubstitutionLimit(OnlineGroundPassBoard board,OnlineMatchProtocol protocol,MatchManager.GameData data,bool home)
    {
        board.match.SetSubstitutionsAvailable(true,"Owned extra-time boundary fixture");string actor=home?"h":"a";long sequence=0;
        OnlineCommandReply Submit(string action)=>pause.Submit(actor,new OnlineMatchCommand("full-room-scene",++sequence,pause.Revision,"shared-pause",action));
        Assert.That(Submit(OnlineSharedPause.RequestAction).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
        int limit=MatchManager.MaxSubstitutionsPerTeam;
        for(int i=0;i<limit;i++)Assert.That(Submit(OnlineSharedPause.SubstitutionAction(2+i,13+i)).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
        var authority=new OnlineGroundPassAuthority(board.match,protocol);board.localTeam=home?OnlineTeam.Home:OnlineTeam.Away;board.controls.Configure(board.localTeam);board.Display(pause.Capture(authority),"Limit fixture");
        Assert.That(board.match.GetSubstitutionsRemaining(home),Is.Zero);Assert.That(board.menu.substitutionButton.interactable,Is.False,"The menu must honor the configured substitution limit");
        Assert.That(Submit(OnlineSharedPause.SubstitutionAction(2+limit,13+limit)).Status,Is.EqualTo(OnlineCommandStatus.IllegalAction));
        typeof(MatchManager).GetMethod("GrantExtraTimeSubstitutionCreditIfNeeded",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(board.match,null);
        board.Display(pause.Capture(authority),"Extra time");Assert.That(board.menu.substitutionButton.interactable,Is.True);Assert.That(board.match.GetSubstitutionsRemaining(home),Is.EqualTo(1));
        Assert.That(Submit(OnlineSharedPause.SubstitutionAction(2+limit,13+limit)).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
        var published=pause.Capture(authority);pause.Dispose();pause=null;
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/MainMenu.unity",new LoadSceneParameters(LoadSceneMode.Single));session.Dispose();session=null;MatchManager.Instance=null;
        yield return Load(data,true);board=Object.FindAnyObjectByType<OnlineFullRoomBootstrap>().board;
        var replica=new OnlineGroundPassReplica("full-room-scene",board.tokens.Select(OnlineRollPublication.ActorId),OnlineFullRoomBootstrap.Catalog);Assert.That(replica.Apply(published),Is.True);board.Display(replica.State,"Extra time replica");
        Assert.That(board.match.GetSubstitutionLimit(home),Is.EqualTo(MatchManager.ExtraTimeMaxSubstitutionsPerTeam));Assert.That(board.match.GetSubstitutionsUsed(home),Is.EqualTo(MatchManager.ExtraTimeMaxSubstitutionsPerTeam));Assert.That(board.match.GetSubstitutionsRemaining(home),Is.Zero);
        Assert.That(board.match.GetSubstitutionsUsed(!home),Is.Zero);Assert.That(board.match.GetSubstitutionsRemaining(!home),Is.EqualTo(MatchManager.ExtraTimeMaxSubstitutionsPerTeam));
        Assert.That(board.match.gameData.events,Is.Empty);Assert.That(board.match.CaptureOnlineDecisionTiming(),Is.Null);
    }
    private IEnumerator RunSubstitution(OnlineGroundPassBoard board,OnlineMatchProtocol protocol,MatchManager.GameData data,bool home,bool ui,bool keeper)
    {
        int offNumber=keeper?1:2,onNumber=keeper?12:13;
        var manager=board.match.playerTokenManager;var outgoing=board.tokens.Single(t=>t.isHomeTeam==home && t.jerseyNumber==offNumber);var incoming=board.tokens.Single(t=>t.isHomeTeam==home && t.jerseyNumber==onNumber);
        var destination=outgoing.GetCurrentHex().coordinates;string actor=home?"h":"a",other=home?"a":"h";
        board.match.SetSubstitutionsAvailable(true,"Owned stoppage fixture");
        OnlineMatchCommand Command(long sequence,string action,long? revision=null)=>new("full-room-scene",sequence,revision??pause.Revision,"shared-pause",action);
        Assert.That(pause.Submit(actor,Command(1,OnlineSharedPause.RequestAction)).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
        if(!home)board.match.ball.PlaceAtCell(outgoing.GetCurrentHex());
        var request=Command(2,OnlineSharedPause.SubstitutionAction(offNumber,onNumber));
        if(ui)
        {
            board.localTeam=home?OnlineTeam.Home:OnlineTeam.Away;board.controls.Configure(board.localTeam);
            var viewAuthority=new OnlineGroundPassAuthority(board.match,protocol);board.Display(pause.Capture(viewAuthority),"Substitution controls");
            OnlineCommandStatus? status=null;
            void Requested(OnlineGroundPassMessage message)
            {
                var wire=new OnlineGroundPassWire();OnlineGroundPassMessage decoded=null;foreach(var packet in OnlineGroundPassWire.Encode(message))decoded=wire.Receive(packet,packet.Length);
                Assert.That(decoded.kind,Is.EqualTo("control"));request=decoded.ToCommand();var reply=pause.Submit(actor,request);status=reply.Status;
                board.controls.Reply(new OnlineGroundPassMessage{status=reply.Status.ToString()});board.Display(pause.Capture(viewAuthority),"Substitution controls");
            }
            board.controls.Requested+=Requested;
            try
            {
                var menu=board.menu;Assert.That(menu.substitutionButton.interactable,Is.True);menu.substitutionButton.onClick.Invoke();Assert.That(menu.substitutionPanel.activeSelf,Is.True);
                board.controls.Escape();Assert.That(menu.substitutionPanel.activeSelf,Is.False);Assert.That(pause.IsPaused,Is.True);Assert.That(status,Is.Null,"Escape closes personal selection without resuming shared play");menu.substitutionButton.onClick.Invoke();
                menu.outgoingPlayer.value=menu.outgoingPlayer.options.FindIndex(o=>o.text.StartsWith(offNumber+". "));menu.incomingPlayer.value=menu.incomingPlayer.options.FindIndex(o=>o.text.StartsWith(onNumber+". "));
                Assert.That(menu.substitutionCommitButton.interactable,Is.True);menu.substitutionCommitButton.onClick.Invoke();Assert.That(status,Is.EqualTo(OnlineCommandStatus.Accepted));Assert.That(menu.substitutionPanel.activeSelf,Is.False);
            }
            finally{board.controls.Requested-=Requested;}
        }
        else Assert.That(pause.Submit(actor,request).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
        Assert.That(outgoing.wasSubbedOff,Is.True);Assert.That(incoming.wasSubbedOn,Is.True);Assert.That(incoming.GetCurrentHex().coordinates,Is.EqualTo(destination));Assert.That(incoming.GetCurrentHex().occupyingToken,Is.SameAs(incoming));
        Assert.That(board.match.GetSubstitutionsUsed(home),Is.EqualTo(1));Assert.That(board.match.GetSubstitutionsUsed(!home),Is.Zero);
        Assert.That(pause.Submit(actor,request).Status,Is.EqualTo(OnlineCommandStatus.Duplicate));
        Assert.That(pause.Submit(actor,Command(2,"roster.substitute:3:14")).Status,Is.EqualTo(OnlineCommandStatus.SequenceConflict));
        Assert.That(pause.Submit(other,Command(1,"roster.substitute:2:13")).Status,Is.EqualTo(OnlineCommandStatus.WrongTeam));
        foreach(string invalid in new[]{"roster.substitute:2:12","roster.substitute:2:19","roster.substitute:away:2:13","roster.substitute:02:14"})
            Assert.That(pause.Submit(actor,Command(3,invalid)).Status,Is.EqualTo(OnlineCommandStatus.IllegalAction));
        Assert.That(pause.Submit(actor,Command(3,OnlineSharedPause.SubstitutionAction(onNumber,offNumber))).Status,Is.EqualTo(OnlineCommandStatus.IllegalAction),"Subbed-off players cannot return");
        Assert.That(pause.Submit(actor,Command(3,"roster.substitute:3:14",pause.Revision-1)).Status,Is.EqualTo(OnlineCommandStatus.StaleRevision));
        pause.SetConnectionHold(true);Assert.That(pause.Submit(actor,Command(3,"roster.substitute:3:14")).Status,Is.EqualTo(OnlineCommandStatus.NotReady));pause.SetConnectionHold(false);
        string previousConnection=actor;protocol.Disconnect(actor);pause.SetConnectionHold(true);
        Assert.That(protocol.BindAuthenticatedAccount(home?"home":"away","returned",protocol.Compatibility),Is.True);actor="returned";
        Assert.That(pause.Submit(previousConnection,request).Status,Is.EqualTo(OnlineCommandStatus.UnknownConnection));
        Assert.That(pause.Submit(actor,request).Status,Is.EqualTo(OnlineCommandStatus.Duplicate));Assert.That(pause.Requester,Is.EqualTo(home?OnlineTeam.Home:OnlineTeam.Away));
        Assert.That(board.match.GetSubstitutionsUsed(home),Is.EqualTo(1));pause.SetConnectionHold(false);Assert.That(pause.IsPaused,Is.True);
        Assert.That(pause.Submit(actor,Command(3,OnlineSharedPause.ResumeAction)).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
        Assert.That(pause.Submit(actor,Command(4,"roster.substitute:3:14")).Status,Is.EqualTo(OnlineCommandStatus.NotReady));
        Assert.That(board.match.GetSubstitutionsUsed(home),Is.EqualTo(1));
        var authority=new OnlineGroundPassAuthority(board.match,protocol);var published=pause.Capture(authority);
        pause.Dispose();pause=null;
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/MainMenu.unity",new LoadSceneParameters(LoadSceneMode.Single));session.Dispose();session=null;MatchManager.Instance=null;
        yield return Load(data,true);board=Object.FindAnyObjectByType<OnlineFullRoomBootstrap>().board;
        var replica=new OnlineGroundPassReplica("full-room-scene",board.tokens.Select(OnlineRollPublication.ActorId),OnlineFullRoomBootstrap.Catalog);Assert.That(replica.Apply(published),Is.True);board.Display(replica.State,"Substitution replica");
        outgoing=board.tokens.Single(t=>t.isHomeTeam==home && t.jerseyNumber==offNumber);incoming=board.tokens.Single(t=>t.isHomeTeam==home && t.jerseyNumber==onNumber);
        Assert.That(outgoing.wasSubbedOff,Is.True);Assert.That(outgoing.isPlaying,Is.False);Assert.That(incoming.wasSubbedOn,Is.True);Assert.That(incoming.GetCurrentHex().coordinates,Is.EqualTo(destination));Assert.That(board.match.GetSubstitutionsUsed(home),Is.EqualTo(1));
        if(!home)Assert.That(board.match.ball.GetCurrentHex().GetOccupyingToken(),Is.SameAs(incoming));
        if(keeper)Assert.That(incoming.IsGoalKeeper,Is.True);
        Assert.That(board.match.gameData.events,Is.Empty);Assert.That(board.match.CaptureOnlineDecisionTiming(),Is.Null);
    }
    private IEnumerator RunMenu(OnlineGroundPassBoard board,bool guest,bool own,string branch)
    {
        Assert.That(board.menu,Is.Not.Null,"Menu controls must exist in the authored scene");var menu=board.menu;
        var scaler=menu.GetComponentInParent<UnityEngine.UI.CanvasScaler>();Assert.That(scaler.uiScaleMode,Is.EqualTo(UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize));
        if(branch=="menu-leave")
        {
            string path=app.GetLastSavedFilePath();var sentinel=File.ReadAllBytes(path);
            menu.leaveButton.onClick.Invoke();yield return null;
            Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo("MainMenu"));Assert.That(OnlineRoomSession.Current,Is.Null);
            Assert.That(File.ReadAllBytes(path),Is.EqualTo(sentinel),"Leaving an injected online Room cannot flush into the previous local save");yield break;
        }
        var compatibility=new OnlineCompatibility("ground-wire-v7","ground-rules-v1",OnlineFullRoomBootstrap.Catalog);
        var protocol=new OnlineMatchProtocol("full-room-scene","home","away","away",compatibility);
        protocol.BindAuthenticatedAccount("home","h",compatibility);protocol.BindAuthenticatedAccount("away","a",compatibility);
        var state=new OnlineGroundPassAuthority(board.match,protocol).Capture();var sent=new List<OnlineGroundPassMessage>();
        void Requested(OnlineGroundPassMessage message)=>sent.Add(message);
        board.controls.Configure(board.localTeam);board.controls.Requested+=Requested;
        try
        {
            board.Display(state,"Owned menu fixture");Assert.That(menu.pauseButton.interactable,Is.True);
            menu.pauseButton.onClick.Invoke();Assert.That(sent.Count,Is.EqualTo(1));Assert.That(sent[0].actionId,Is.EqualTo(OnlineSharedPause.RequestAction));
            board.controls.Reply(new OnlineGroundPassMessage{status="Accepted"});state.isSharedPaused=true;state.sharedPauseRevision=1;
            state.pauseRequester=(own?board.localTeam:board.localTeam==OnlineTeam.Home?OnlineTeam.Away:OnlineTeam.Home).ToString();
            board.Display(state,"Owned menu fixture");Assert.That(board.sharedPausePanel.activeSelf,Is.True);Assert.That(menu.resumeButton.interactable,Is.EqualTo(own));
            Assert.That(board.requesterPauseMessage.activeSelf,Is.EqualTo(own));Assert.That(board.passivePauseMessage.activeSelf,Is.EqualTo(!own));
            Assert.That(menu.settings.text,Does.Contain(board.match.gameData.gameSettings.homeTeamName));Assert.That(menu.settings.text,Does.Contain(board.match.gameData.gameSettings.weatherConditions));
            Canvas.ForceUpdateCanvases();Assert.That(menu.settings.preferredHeight,Is.LessThanOrEqualTo(menu.settings.rectTransform.rect.height),"Settings must fit their authored text area");
            menu.resumeButton.onClick.Invoke();Assert.That(sent.Count,Is.EqualTo(own?2:1));if(own)Assert.That(sent[1].actionId,Is.EqualTo(OnlineSharedPause.ResumeAction));
            board.controls.Reply(new OnlineGroundPassMessage{status="Accepted"});state.connectionHeld=true;board.Display(state,"Reconnecting");
            Assert.That(menu.resumeButton.interactable,Is.False);Assert.That(menu.pauseButton.interactable,Is.False);Assert.That(menu.leaveButton.interactable,Is.True);Assert.That(menu.status.text,Does.Contain("Reconnecting"));
            Assert.That(board.match.gameData.events,Is.Empty);Assert.That(board.match.TimingRollSequence,Is.Zero);
            if(guest)Assert.That(board.match.CaptureOnlineDecisionTiming(),Is.Null);
        }
        finally{board.controls.Requested-=Requested;}
        yield return null;
    }
    private static void PrepareRosterStatus(OnlineGroundPassBoard board)
    {
        PlayerToken Token(bool home,int number)=>board.tokens.Single(t=>t.isHomeTeam==home && t.jerseyNumber==number);
        Token(true,2).ReceiveYellowCard();Token(true,2).ReceiveInjury();Token(true,4).MarkSentOff();
        board.match.playerTokenManager.RemoveActiveToken(Token(true,4));Token(false,5).ConvertToGK();
        var outgoing=Token(true,6);var incoming=Token(true,13);var hex=outgoing.GetCurrentHex();var bench=incoming.transform.position;
        board.match.SetSubstitutionsAvailable(true,"Owned goal-boundary fixture");Assert.That(board.match.RegisterSubstitution(outgoing,incoming,out var reason),Is.True,reason);
        board.match.playerTokenManager.MoveActiveTokenToBenchSlot(outgoing,bench);board.match.playerTokenManager.MoveBenchTokenToActive(incoming);
        incoming.isAttacker=outgoing.isAttacker;hex.occupyingToken=null;hex.isAttackOccupied=incoming.isAttacker;hex.isDefenseOccupied=!incoming.isAttacker;incoming.SetCurrentHex(hex);incoming.transform.position=hex.GetHexCenter()+Vector3.up*.2f;
        var stats=board.match.gameData.stats.GetPlayerStats(Token(true,2));stats.goals=2;stats.shotsAttempted=3;stats.shotsOnTarget=2;stats.pacesRan=7;
        (board.match.homeTeamDirection,board.match.awayTeamDirection)=(board.match.awayTeamDirection,board.match.homeTeamDirection);
    }
    private static void AssertRosterStatus(OnlineGroundPassBoard board,MatchManager.GameData initial,bool mirror)
    {
        PlayerToken Token(bool home,int number)=>board.tokens.Single(t=>t.isHomeTeam==home && t.jerseyNumber==number);
        var injured=Token(true,2);Assert.That(injured.isBooked,Is.True);Assert.That(injured.isInjured,Is.True);
        Assert.That(injured.pace,Is.EqualTo(Mathf.Max(0,initial.rosters.home["2"].pace-1)));
        Assert.That(Token(true,4).isSentOff,Is.True);Assert.That(Token(true,4).gameObject.activeSelf,Is.False);
        Assert.That(Token(false,5).IsGoalKeeper,Is.True);Assert.That(Token(false,5).saving,Is.Zero);
        Assert.That(Token(true,6).wasSubbedOff,Is.True);Assert.That(Token(true,6).isPlaying,Is.False);Assert.That(Token(true,6).gameObject.activeSelf,Is.True,"An authored bench remains visible to both managers");
        Assert.That(Token(true,13).wasSubbedOn,Is.True);Assert.That(Token(true,13).isPlaying,Is.True);Assert.That(Token(true,13).GetCurrentHex().occupyingToken,Is.SameAs(Token(true,13)));
        Assert.That(board.match.playerTokenManager.allTokens.Contains(Token(true,13)),Is.True);Assert.That(board.match.playerTokenManager.benchTokens.Contains(Token(true,6)),Is.True);
        Assert.That(board.match.GetSubstitutionsUsed(true),Is.EqualTo(1));
        Assert.That(board.match.homeTeamDirection,Is.EqualTo(mirror?MatchManager.TeamAttackingDirection.LeftToRight:MatchManager.TeamAttackingDirection.RightToLeft));
        var stats=board.match.gameData.stats.GetPlayerStats(injured);Assert.That(stats.goals,Is.EqualTo(2));Assert.That(stats.shotsAttempted,Is.EqualTo(3));Assert.That(stats.shotsOnTarget,Is.EqualTo(2));Assert.That(stats.pacesRan,Is.EqualTo(7));
    }
    private IEnumerator RunTransport(OnlineGroundPassBoard board,OnlineMatchProtocol protocol,int size)
    {
        protocol.BindAuthenticatedAccount("home","guest-home",protocol.Compatibility);protocol.BindAuthenticatedAccount("away","host-away",protocol.Compatibility);
        var authority=new OnlineGroundPassAuthority(board.match,protocol);
        var type=typeof(OnlineGroundPassBoard).Assembly.GetType("CounterAttack.Online.EOSGroundPassExchange",true);
        const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
        var constructor=type.GetConstructors(flags).Single(c=>c.GetParameters().Length==7);
        var incoming=new Queue<OnlineGroundPassMessage>();var outgoing=new Queue<OnlineGroundPassMessage>();
        OnlineGroundPassMessage Copy(OnlineGroundPassMessage message)
        {
            message.matchId="full-room-scene";var wire=new OnlineGroundPassWire();OnlineGroundPassMessage decoded=null;
            foreach(var packet in OnlineGroundPassWire.Encode(message))decoded=wire.Receive(packet,packet.Length);
            Assert.That(decoded,Is.Not.Null);return decoded;
        }
        object exchange=null;double now=0;string nonce=Guid.NewGuid().ToString("N");long generation=0,sequence=0,heartbeat=0;
        exchange=constructor.Invoke(new object[]{board,directory,(Action<OnlineGroundPassMessage>)(m=>outgoing.Enqueue(Copy(m))),
            (Action)(()=>{var queue=(Queue<OnlineGroundPassMessage>)type.GetField("messages",flags).GetValue(exchange);while(incoming.Count>0)queue.Enqueue(incoming.Dequeue());}),"home","full-room-scene",(Func<double>)(()=>now)});
        string sessionId=board.match.gameData.playSessionId;
        type.GetMethod("PrepareHostBoard",flags).Invoke(exchange,null);
        Assert.That(board.match.gameData.playSessionId,Is.EqualTo(sessionId));
        Assert.That(board.tokens.Length,Is.EqualTo(size*2));
        Assert.That(type.GetProperty("SessionCatalog",flags).GetValue(exchange),Is.EqualTo(OnlineFullRoomBootstrap.Catalog));
        var flow=(IEnumerator)type.GetMethod("ManualHost",flags).Invoke(exchange,new object[]{authority,protocol,pause});
        var replica=new OnlineGroundPassReplica("full-room-scene",board.tokens.Select(OnlineRollPublication.ActorId),OnlineFullRoomBootstrap.Catalog);
        void Send(OnlineGroundPassMessage message){message.peerNonce=nonce;message.connectionGeneration=generation;incoming.Enqueue(Copy(message));}
        Send(new OnlineGroundPassMessage{kind="hello"});bool ready=false;int commands=0;string lastDecision=null;
        double deadline=Time.realtimeSinceStartupAsDouble+20;
        try
        {
            while(Time.realtimeSinceStartupAsDouble<deadline)
            {
                now+=.05;Assert.That(flow.MoveNext(),Is.True,"Full match must remain open across kickoff");
                while(outgoing.Count>0)
                {
                    var message=outgoing.Dequeue();generation=message.connectionGeneration;
                    if(message.kind=="state")
                    {
                        Assert.That(replica.Apply(message.state),Is.True);Assert.That(message.state.tokens.Length,Is.EqualTo(size*2));
                        if(message.status=="resync")Send(new OnlineGroundPassMessage{kind="hello",status="resync-ack",digest=(string)type.GetMethod("Digest",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{message.state})});
                        else ready=true;
                    }
                    if(message.kind=="reply")Assert.That(message.status,Is.EqualTo(OnlineCommandStatus.Accepted.ToString()));
                }
                Send(new OnlineGroundPassMessage{kind="hello",status="heartbeat",sequence=++heartbeat});
                var state=authority.Capture();
                if(ready && state.passActive && state.awaitingTarget){Assert.That(commands,Is.EqualTo(3));Assert.That(replica.State.tokens.Length,Is.EqualTo(size*2));yield break;}
                string action=state.stage=="kickoff.setup"?OnlineGroundPassAuthority.KickoffConfirmAction:state.stage=="kickoff.taker"?state.legalActions.FirstOrDefault(a=>a.StartsWith("kickoff.taker:")):null;
                string decision=state.decisionId+":"+state.revision;
                if(ready && action!=null && decision!=lastDecision)
                {
                    lastDecision=decision;commands++;
                    if(state.owner=="Home")Send(OnlineGroundPassMessage.Command(state,++sequence,action));
                    else
                    {
                        Assert.That(board.controls.commandButton.interactable,Is.True);board.controls.legalCommands.value=Array.IndexOf(state.legalActions,action);board.controls.commandButton.onClick.Invoke();
                    }
                }
                yield return null;
            }
            Assert.Fail("Encoded full Room kickoff did not reach its required pass");
        }
        finally{(flow as IDisposable)?.Dispose();}
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
