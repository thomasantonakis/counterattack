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
    private bool? previousFrameworkFlag;
    private bool? previousFormationFlag;
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
    private MatchManager.GameData Data(int size,bool mirror,bool useFrontendSeats=false)
    {
        var v=new OnlineCompatibility(OnlineSessionWire.Version,"full-room-draft","catalog");
        var p=useFrontendSeats?OnlineFrontendSessionPolicy.CreateMatchProtocol("full-room-scene","home","away",v)
            :new OnlineMatchProtocol("full-room-scene","home","away","away",v);
        p.BindAuthenticatedAccount("home","h",v);p.BindAuthenticatedAccount("away","a",v);
        var settings=OnlineSetupAgreementEditorTests.Valid();settings.squadSize=size.ToString();int roll=0;
        var d=new OnlineDraftAuthority(p,settings,OnlineDraftCatalog.Load(),_=>0,()=>++roll%2==1?6:1);
        void Submit(OnlineTeam actor,OnlineDraftIntent intent,string card=null,int slot=0,CoinTossChoice choice=CoinTossChoice.Kickoff)
        {var s=d.Capture();Assert.That(d.Submit(actor==OnlineTeam.Home?"h":"a",new OnlineDraftRequest{matchId=s.matchId,sequence=(actor==OnlineTeam.Home?s.homeSequence:s.awaySequence)+1,revision=s.revision,intent=intent,cardId=card,slot=slot,choice=choice}).Status,Is.EqualTo(OnlineCommandStatus.Accepted));}
        Submit(OnlineTeam.Home,OnlineDraftIntent.Roll);Submit(OnlineTeam.Away,OnlineDraftIntent.Roll);
        while(d.Capture().phase=="pick"){var s=d.Capture();Submit(s.turn,OnlineDraftIntent.Pick,s.cards.First(card=>card!=null).id,Array.FindIndex(s.turn==OnlineTeam.Home?s.home:s.away,c=>c==null)+1);}
        Submit(OnlineTeam.Home,OnlineDraftIntent.Ready);Submit(OnlineTeam.Away,OnlineDraftIntent.Ready);Submit(p.HostTeam,OnlineDraftIntent.Start);
        Submit(OnlineTeam.Home,OnlineDraftIntent.Roll);Submit(OnlineTeam.Away,OnlineDraftIntent.Roll);Submit(OnlineTeam.Home,OnlineDraftIntent.Choose,choice:mirror?CoinTossChoice.RightToLeft:CoinTossChoice.LeftToRight);
        return OnlineRoomLaunchData.Create(d.Capture());
    }
    private IEnumerator Load(MatchManager.GameData data,bool guest,bool showIntro=false)
    {
        string path=Path.Combine(directory,guest?"replica.json":"authority.json");File.WriteAllText(path,JsonConvert.SerializeObject(data));
        session=new OnlineRoomSession("full-room-scene",guest,data,path,showOpeningIntro:showIntro);app.SetActiveSaveFilePath(path);
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode(OnlineFullRoomAuthoring.ScenePath,new LoadSceneParameters(LoadSceneMode.Single));yield return null;
    }
    private void InitializeFixture()
    {
        Assert.That(OnlineRoomSession.Current,Is.Null);Assert.That(OnlineFrontendSession.Current,Is.Null);
        app=ApplicationManager.Instance;created=app==null;if(created){ApplicationManager.EnsureInstanceExists();app=ApplicationManager.Instance;}
        CaptureGlobals();previousPath=app.LastSavedFileName;previousExplicit=app.HasExplicitSaveContext;previousMode=app.SelectedRoomGameMode;app.SetSelectedRoomGameMode(ApplicationManager.OnlineGameMode);Time.timeScale=1;
        directory=Path.Combine(Path.GetTempPath(),"CounterattackFullRoom",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
    }
    public IEnumerator RunNativePeerHover(bool guest)
    {
        InitializeFixture();
        SuppressLegacyRoomRunner();
        yield return LoadNativeRoom(Data(16, false, useFrontendSeats: true), guest, "full-room-scene");
        var board = Object.FindAnyObjectByType<OnlineRoomStartup>().Board;
        var input = board.boardInput;
        input.samplePhysicalInput = false;
        var token = board.tokens.First(candidate => candidate.currentHex != null);
        Physics.SyncTransforms();
        var ray = new Ray(token.transform.position + Vector3.up * 10, Vector3.down);
        Assert.That(input.PresentHover(ray, true), Is.SameAs(token));
        var presentation = Object.FindAnyObjectByType<GameInputManager>(FindObjectsInactive.Include);
        Assert.That(presentation.InspectionHoverToken, Is.SameAs(token));
        const System.Reflection.BindingFlags fields = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        var localPointer = new OnlineRoomPointer { visible = true };
        typeof(OnlineBoardInput).GetMethod("CapturePointerHover", fields).Invoke(input, new object[] { localPointer });
        Assert.That(localPointer.hoveredToken, Is.EqualTo(OnlineRollPublication.ActorId(token)));
        var peer = board.tokens.First(candidate => candidate.isHomeTeam != token.isHomeTeam && candidate.currentHex != null);
        var stats = Object.FindAnyObjectByType<MatchStatsUI>();
        var localSlot = typeof(MatchStatsUI).GetField(token.isHomeTeam ? "lastHoveredHomeToken" : "lastHoveredAwayToken", fields);
        Assert.That(localSlot.GetValue(stats), Is.SameAs(token));
        var cardSlot = typeof(MatchStatsUI).GetField(peer.isHomeTeam ? "lastHoveredHomeToken" : "lastHoveredAwayToken", fields);
        var previousCard = cardSlot.GetValue(stats);
        var peerLabel = (TMPro.TMP_Text)typeof(GameInputManager).GetField("peerHoverNameLabel", fields).GetValue(presentation);
        var localLabel = (TMPro.TMP_Text)typeof(GameInputManager).GetField("hoverNameLabel", fields).GetValue(presentation);
        Assert.That(peerLabel, Is.Not.Null, "Room has a peer hover label");
        var wire = new OnlineGroundPassWire();
        OnlineGroundPassMessage received = null;
        foreach (var fragment in OnlineGroundPassWire.Encode(new OnlineGroundPassMessage
        {
            kind = "pointer", matchId = "full-room-scene", sequence = 3, connectionGeneration = 1,
            peerNonce = System.Guid.NewGuid().ToString("N"),
            pointer = new OnlineRoomPointer { visible = true, hoveredToken = OnlineRollPublication.ActorId(peer) }
        }))
            received = wire.Receive(fragment, fragment.Length) ?? received;
        board.pointers.ShowRemote(received.pointer, received.sequence);
        yield return null;
        yield return null;
        Assert.That(localLabel.gameObject.activeInHierarchy && peerLabel.gameObject.activeInHierarchy, Is.True);
        Assert.That(localLabel.text, Is.EqualTo(token.playerName));
        Assert.That(peerLabel.text, Is.EqualTo(peer.playerName));
        Assert.That(cardSlot.GetValue(stats), Is.SameAs(previousCard));
        Assert.That(presentation.InspectionHoverToken, Is.SameAs(token));
        board.pointers.ShowRemote(new OnlineRoomPointer(), 4);
        yield return null;
        yield return null;
        Assert.That(peerLabel.gameObject.activeInHierarchy, Is.False);
        Assert.That(localLabel.gameObject.activeInHierarchy, Is.True);
    }

    public IEnumerator Run(bool guest,int size,bool mirror,string branch)
    {
        InitializeFixture();
        var data=Data(size,mirror);
        if(branch.StartsWith("action-preview:"))data.gameSettings.playerAssistance=branch.EndsWith(":medium")?2:1;
        yield return Load(data,branch is "replica" or "status" or "scorers" or "banners" or "f3-label" or "settings"?false:guest,branch=="room-intro");
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
        if (branch == "room-intro")
        {
            Assert.That(RoomInputOwnership.IntroActive,Is.True);
            board.controls.Escape();
            Assert.That(RoomInputOwnership.IntroActive,Is.False);
            Assert.That(RoomInputOwnership.ReservePauseEscape,Is.True);
            Assert.That(session.IntroPending,Is.True,"A local Skip must not release the match");
            if(!guest)
            {
                var introVersion=new OnlineCompatibility("ground-wire-v7","ground-rules-v1",OnlineFullRoomBootstrap.Catalog);
                var introProtocol=new OnlineMatchProtocol("full-room-scene","home","away","away",introVersion);
                introProtocol.BindAuthenticatedAccount("home","h",introVersion);
                introProtocol.BindAuthenticatedAccount("away","a",introVersion);
                using var introPause=new OnlineSharedPause(introProtocol,board.match);
                Assert.That(introPause.Submit("a",new OnlineMatchCommand("full-room-scene",1,introPause.Revision,"shared-pause","intro.ready")).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
                Assert.That(session.IntroPending,Is.True);
                Assert.That(board.match.IsGameplayInputBlocked,Is.True);
                Assert.That(introPause.Submit("h",new OnlineMatchCommand("full-room-scene",1,introPause.Revision,"shared-pause","intro.ready")).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
                Assert.That(session.IntroPending,Is.False);
            }
            Assert.That(board.match.gameData.events,Is.Empty);
            yield break;
        }
        if (branch == "team-hover")
        {
            var input=board.boardInput;
            var token=board.match.playerTokenManager.allTokens.First(t=>t.currentHex!=null);
            Physics.SyncTransforms();
            var ray=new Ray(token.transform.position+Vector3.up*10,Vector3.down);
            var hovered=input.PresentHover(ray,true);
            Assert.That(hovered,Is.SameAs(token));
            var presentation=Object.FindAnyObjectByType<GameInputManager>(FindObjectsInactive.Include);
            Assert.That(presentation.InspectionHoverToken,Is.SameAs(token));
            Assert.That(RoomInspectionCard.Instance,Is.Not.Null);
            RoomInspectionCard.Instance.Toggle(hovered);
            Assert.That(RoomInspectionCard.Instance.LockedPlayer,Is.SameAs(token));
            board.controls.Escape();
            Assert.That(RoomInspectionCard.Instance.IsOpen,Is.False);
            input.PresentHover(ray,false);
            Assert.That(presentation.InspectionHoverToken,Is.Null);
            Assert.That(board.match.gameData.events,Is.Empty);
            yield break;
        }
        if (branch == "team-pointers")
        {
            var pointers = board.pointers;
            Assert.That(pointers, Is.Not.Null);
            Assert.That(pointers.localMarker.raycastTarget || pointers.remoteMarker.raycastTarget, Is.False);
            var camera = board.boardCamera;
            var framing = camera.GetComponent<CameraController>();
            var point = new Vector3(2, 0, -1);
            var bounds = Rect.MinMaxRect(-25, -15, 25, 15);
            foreach (int preset in new[] { 1, 2 })
            {
                framing.SetCameraToPreset(preset);
                var sample = OnlineRoomPointers.Project(camera, bounds, camera.WorldToScreenPoint(point));
                Assert.That(sample.visible, Is.True);
                Assert.That(sample.x, Is.EqualTo(point.x).Within(0.001f));
                Assert.That(sample.z, Is.EqualTo(point.z).Within(0.001f));
            }
            Assert.That(OnlineRoomPointers.Project(camera, bounds, new Vector2(-10, -10)).visible, Is.False);
            var wire = new OnlineGroundPassWire();
            OnlineGroundPassMessage received = null;
            foreach (var packet in OnlineGroundPassWire.Encode(new OnlineGroundPassMessage
                { kind = "pointer", matchId = "full-room-scene", sequence = 2, connectionGeneration = 1,
                    peerNonce = System.Guid.NewGuid().ToString("N"), pointer = new OnlineRoomPointer { visible = true, x = point.x, z = point.z, icon = 2 } }))
                received = wire.Receive(packet, packet.Length) ?? received;
            Assert.That(received?.pointer, Is.Not.Null);
            pointers.ShowRemote(received.pointer, received.sequence);
            pointers.ShowRemote(new OnlineRoomPointer(), 1);
            yield return null;
            Assert.That(pointers.remoteMarker.gameObject.activeSelf, Is.True);
            Assert.That(pointers.remoteIcon.texture, Is.EqualTo(pointers.cameraDragIcon));
            Assert.That(pointers.remoteIcon.raycastTarget, Is.False);
            pointers.ShowRemote(new OnlineRoomPointer { visible = true, x = point.x, z = point.z, icon = 1 }, 3);
            yield return null;
            Assert.That(pointers.remoteIcon.texture, Is.EqualTo(pointers.grabIcon));
            pointers.ShowRemote(new OnlineRoomPointer { visible = true, x = point.x, z = point.z }, 4);
            yield return null;
            Assert.That(pointers.remoteIcon.texture, Is.EqualTo(pointers.hoverIcon));
            var settings = board.match.gameData.gameSettings;
            var remoteKit = guest ? settings.awayKit : settings.homeKit;
            Assert.That(pointers.remoteMarker.color, Is.EqualTo(TokenKitCatalog.ResolveInstructionPalette(remoteKit, Color.white, Color.black).Primary));
            pointers.SetHold(true);
            yield return null;
            Assert.That(pointers.remoteMarker.gameObject.activeSelf, Is.False);
            Assert.That(board.match.gameData.events, Is.Empty);
            yield break;
        }
        if (branch == "peer-left")
        {
            board.menu.PresentPeerDeparture();
            Assert.That(board.sharedPausePanel.activeInHierarchy,Is.True);
            Assert.That(board.menu.status.text,Does.Contain("other manager left the match"));
            Assert.That(board.menu.leaveButton.interactable,Is.True);
            Assert.That(board.menu.pauseButton.interactable,Is.False);
            yield break;
        }
        if (branch == "ball-jerseys")
        {
            var darkHex=board.cells.First(cell=>cell.isDark && !cell.isOutOfBounds);
            var borderMaterial=darkHex.GetComponent<MeshRenderer>().material;
            var border=borderMaterial.HasProperty("_BorderColor")?borderMaterial.GetColor("_BorderColor"):borderMaterial.color;
            Assert.That(Vector4.Distance(border,darkHex.originalColor),Is.LessThan(.001f),"Startup border must match dark fill before any settings edit");
            var renderer=(guest?board.displayedBall:board.match.ball.transform).GetComponent<MeshRenderer>();
            var block=new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            Color expected=new Color32(244,246,250,255);
            Assert.That(Vector4.Distance(block.GetColor("_BaseColor"),expected),Is.LessThan(.001f));
            foreach(var token in board.match.playerTokenManager.allTokens)
            {
                var number=token.GetComponentInChildren<TMPro.TextMeshPro>();
                Assert.That(number,Is.Not.Null,token.name);
                Assert.That(number.mesh.vertexCount,Is.GreaterThan(0),token.name);
                Assert.That(board.boardCamera.cullingMask & (1<<number.gameObject.layer),Is.Not.Zero,token.name);
            }
            var camera=board.boardCamera;
            var sample=board.match.playerTokenManager.allTokens.First(token=>token.jerseyNumber==9)
                .GetComponentInChildren<TMPro.TextMeshPro>();
            var numberRenderer=sample.GetComponent<MeshRenderer>();
            Assert.That(numberRenderer.sharedMaterial.shader.name,Is.EqualTo("TextMeshPro/Distance Field Overlay"));
            AssertVisibleJerseyPixels(camera, sample, "Temp/OnlineJersey"+(guest?"Guest":"Host")+".png");
            yield break;
        }
        if (branch == "movement-summary")
        {
            var movementVersion=new OnlineCompatibility("ground-wire-v7","ground-rules-v1",OnlineFullRoomBootstrap.Catalog);
            var movementProtocol=new OnlineMatchProtocol("full-room-scene","home","away","away",movementVersion);
            movementProtocol.BindAuthenticatedAccount("home","h",movementVersion);
            movementProtocol.BindAuthenticatedAccount("away","a",movementVersion);
            var authority=new OnlineGroundPassAuthority(board.match,movementProtocol);
            var movement=board.match.movementPhaseManager;
            movement.ActivateMovementPhase();
            var publication=authority.Capture();
            string expected=$"Attacking movement · {movement.AttackingMoveLimit} moves remaining";
            Assert.That(publication.instruction.text,Does.StartWith(expected));
            board.Display(publication,"Movement observer");
            yield return null;
            Assert.That(board.instructionMonitor.instructionText.text,Does.StartWith(expected));
            Assert.That(board.match.gameData.events,Is.Empty);
            Assert.That(publication.movementPanel,Is.Not.Null);
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/MainMenu.unity",new LoadSceneParameters(LoadSceneMode.Single));
            session.Dispose();session=null;MatchManager.Instance=null;
            yield return Load(data,true);
            var guestBoard=Object.FindAnyObjectByType<OnlineFullRoomBootstrap>().board;
            var movedPanel=Object.FindAnyObjectByType<MovementPhaseMovedTokensPanel>();
            guestBoard.Display(publication,"Guest movement panel");
            yield return null;
            Assert.That(movedPanel.GetComponent<CanvasGroup>().alpha,Is.EqualTo(1f));
            publication.movementPanel=null;
            guestBoard.Display(publication,"Movement complete");
            Assert.That(movedPanel.GetComponent<CanvasGroup>().alpha,Is.Zero);
            yield break;
        }
        if (branch == "goal-celebration")
        {
            var versionForGoal = new OnlineCompatibility("ground-wire-v7", "ground-rules-v1", OnlineFullRoomBootstrap.Catalog);
            var goalProtocol = new OnlineMatchProtocol("full-room-scene", "home", "away", "away", versionForGoal);
            goalProtocol.BindAuthenticatedAccount("home", "h", versionForGoal);
            goalProtocol.BindAuthenticatedAccount("away", "a", versionForGoal);
            var authority = new OnlineGroundPassAuthority(board.match, goalProtocol);
            var scorer = board.tokens.First(token => token.isHomeTeam == mirror && token.jerseyNumber == 9);
            var goal = board.cells.First(cell => cell.isInGoal == (mirror ? -1 : 1));
            var opening = scorer.transform.position;
            board.match.goalFlowManager.StartGoalFlow(scorer, goal);
            float deadline = Time.realtimeSinceStartup + 5f;
            while (Vector3.Distance(scorer.transform.position, opening) <= 0.01f && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(Vector3.Distance(scorer.transform.position, opening), Is.GreaterThan(0.01f));
            var goalState = authority.Capture();
            Assert.That(goalState.stage, Is.EqualTo("goal.resolving"));
            Assert.That(goalState.instruction.text, Does.Contain("GOAL FOR"));
            Assert.That(goalState.ShouldStreamPresentation(), Is.True);
            var pose = goalState.roomPose.players.Single(player => player.id == OnlineRollPublication.ActorId(scorer));
            Assert.That(new Vector3(pose.x, pose.y, pose.z), Is.EqualTo(scorer.transform.position));
            var initialCameraPosition=board.boardCamera.transform.position;
            var initialCameraRotation=board.boardCamera.transform.rotation;
            board.Display(goalState,"Goal camera check");
            Assert.That(board.boardCamera.transform.position,Is.EqualTo(initialCameraPosition));
            Assert.That(board.boardCamera.transform.rotation,Is.EqualTo(initialCameraRotation));
            goalState.goalMotion=false;
            board.Display(goalState,"Restore local camera");
            Assert.That(board.boardCamera.transform.position,Is.EqualTo(initialCameraPosition));
            Assert.That(board.boardCamera.transform.rotation,Is.EqualTo(initialCameraRotation));
            yield break;
        }
        if (branch == "local-camera")
        {
            var camera = board.boardCamera.GetComponent<CameraController>();
            Assert.That(camera.IsReady, Is.True);
            foreach (var token in board.tokens)
            {
                var number = token.transform.Find("Authored jersey number").GetComponent<TMPro.TextMeshPro>();
                var renderer = number.GetComponent<MeshRenderer>();
                number.ForceMeshUpdate();
                Assert.That(renderer.enabled, Is.True);
                Assert.That(renderer.sortingOrder, Is.EqualTo(10));
                Assert.That(number.GetParsedText(), Is.EqualTo(token.jerseyNumber.ToString()));
            }
            if (guest) Assert.That(board.match.IsGameplayInputBlocked, Is.True);
            var opening = camera.transform.position;
            var change = new KeyPressData(KeyCode.Alpha3, false, false, false);
            Assert.That(camera.TryHandlePresetKey(change), Is.True);
            Assert.That(change.isConsumed, Is.True);
            Assert.That(Vector3.Distance(camera.transform.position, opening), Is.GreaterThan(0.1f));
            var changed = camera.transform.position;
            camera.SetOnlinePresentationHold(true);
            Assert.That(camera.TryHandlePresetKey(new KeyPressData(KeyCode.Alpha4, false, false, false)), Is.False);
            Assert.That(camera.transform.position, Is.EqualTo(changed));
            camera.SetOnlinePresentationHold(false);
            Assert.That(camera.TryHandlePresetKey(new KeyPressData(KeyCode.Alpha4, false, false, false)), Is.True);
            Assert.That(board.match.gameData.events, Is.Empty);
            yield break;
        }

        var version=new OnlineCompatibility("ground-wire-v7","ground-rules-v1",OnlineFullRoomBootstrap.Catalog);var protocol=new OnlineMatchProtocol("full-room-scene","home","away","away",version);
        protocol.BindAuthenticatedAccount("home","h",version);protocol.BindAuthenticatedAccount("away","a",version);
        if(!OnlineRoomSession.IsGuest)pause=new OnlineSharedPause(protocol,board.match);
        if(branch is "replica" or "status" or "scorers" or "banners" or "f3-label" or "settings")
        {
            if(branch=="status")PrepareRosterStatus(board);
            if (branch == "scorers")
            {
                board.match.AddGoal("Home striker", true, 12, false, "Home winger");
                board.match.AddGoal("Home striker", true, 45, true, minuteLabel: "45+2'");
                board.match.AddGoal("Away striker", false, 32, false);
                Assert.That(board.match.matchStatsUI.homeScorersText.text, Does.Contain("Home striker"));
                Assert.That(board.match.matchStatsUI.awayScorersText.text, Does.Contain("Away striker"));
            }
            if (branch == "banners")
            {
                var timing = new DecisionTimingService();
                timing.BeginDecision("next-action", DecisionKind.SelectNextAction, "Home", 1);
                timing.Select("next-action");
                timing.Advance(DecisionTimingConfig.GetBudget(DecisionKind.SelectNextAction, 1) * 0.75f);
                MatchEventAnnouncements.Publish(MatchBannerEvent.YellowCard, "Home defender");
                MatchEventAnnouncements.Publish(MatchBannerEvent.Foul, "Away free kick");
                board.match.OnlineEventBanner.SetTimingStatus(timing);
                Assert.That(board.match.OnlineEventBanner.IsTimingVisible, Is.True);
            }
            if (branch == "settings")
            {
                OnlineCommandStatus Control(string connection, long sequence, long revision, string action) => pause.Submit(connection,
                    new OnlineMatchCommand("full-room-scene", sequence, revision, "shared-pause", action)).Status;
                Assert.That(Control("h", 1, pause.Revision, "settings:2:Snow"), Is.EqualTo(OnlineCommandStatus.NotReady));
                Assert.That(Control("h", 1, pause.Revision, OnlineSharedPause.RequestAction), Is.EqualTo(OnlineCommandStatus.Accepted));

                board.controls.Configure(OnlineTeam.Away);
                var view = Object.FindAnyObjectByType<PauseMenuManager>(FindObjectsInactive.Include);
                var settingsAuthority = new OnlineGroundPassAuthority(board.match, protocol);
                board.Display(pause.Capture(settingsAuthority), "Pause settings");
                Assert.That(view.pausePanel.activeInHierarchy, Is.True);
                view.editSettings.onClick.Invoke();
                Assert.That(view.editSettingsPanel.activeInHierarchy, Is.True);
                Assert.That(view.editWeatherDropdown.interactable, Is.True);
                board.controls.Requested += message =>
                    Assert.That(pause.Submit("a", message.ToCommand()).Status, Is.EqualTo(OnlineCommandStatus.Accepted));
                view.editWeatherDropdown.value = view.editWeatherDropdown.options.FindIndex(option => option.text == "Snow");
                Assert.That(board.match.gameData.gameSettings.weatherConditions, Is.EqualTo("Snow"));
                Assert.That(board.match.gameData.gameSettings.ballColor, Is.EqualTo("Orange"));
                Assert.That(Control("h", 2, pause.Revision - 1, "settings:1:3"), Is.EqualTo(OnlineCommandStatus.StaleRevision));
                Assert.That(Control("h", 2, pause.Revision, "settings:1:3"), Is.EqualTo(OnlineCommandStatus.Accepted));
                Assert.That(pause.Requester, Is.EqualTo(OnlineTeam.Home));
                Assert.That(Control("a", 2, pause.Revision, OnlineSharedPause.ResumeAction), Is.EqualTo(OnlineCommandStatus.WrongTeam));
            }
            int labelSide = mirror ? 1 : -1;
            if (branch == "f3-label")
            {
                var origin = board.cells.First(cell => cell.isInFinalThird == -labelSide
                    && cell.isInGoal == 0 && !cell.isOutOfBounds && cell.occupyingToken == null);
                board.match.ball.PlaceAtCell(origin);
                board.match.finalThirdManager.TriggerFinalThirdPhase();
                yield return null;
            }
            var authority=new OnlineGroundPassAuthority(board.match,protocol);var published=branch == "settings" ? pause.Capture(authority) : authority.Capture();
            if (branch == "f3-label")
                Assert.That(published.finalThirdLabelSide, Is.EqualTo(labelSide));
            if (branch is "scorers" or "banners" or "f3-label" or "settings")
            {
                var wire = new OnlineGroundPassWire();
                OnlineGroundPassMessage received = null;
                foreach (var packet in OnlineGroundPassWire.Encode(new OnlineGroundPassMessage
                    { kind = "state", matchId = published.matchId, state = published }))
                    received = wire.Receive(packet, packet.Length) ?? received;
                Assert.That(received?.state, Is.Not.Null);
                published = received.state;
            }
            Assert.That(published.tokens.Length,Is.EqualTo(size*2));
            published.homeStats.totalGoals=2;published.awayStats.totalGoals=1;published.regulationSeconds=123;published.half=1;
            var moved=published.tokens.First(t=>t.id=="home:2");moved.x=mirror?12:-12;moved.z=4;
            if(moved.runtime!=null){moved.runtime.currentHex.x=moved.x;moved.runtime.currentHex.z=moved.z;}
            pause.Dispose();pause=null;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/MainMenu.unity",new LoadSceneParameters(LoadSceneMode.Single));session.Dispose();session=null;MatchManager.Instance=null;
            yield return Load(data,true);board=Object.FindAnyObjectByType<OnlineFullRoomBootstrap>().board;
            var replica=new OnlineGroundPassReplica("full-room-scene",board.tokens.Select(OnlineRollPublication.ActorId),OnlineFullRoomBootstrap.Catalog);
            Assert.That(replica.Apply(published),Is.True);board.Display(replica.State,"Full Room replica");
            if (branch == "settings")
            {
                var view = Object.FindAnyObjectByType<PauseMenuManager>(FindObjectsInactive.Include);
                Assert.That(view.pausePanel.activeInHierarchy, Is.True);
                view.editSettings.onClick.Invoke();
                Assert.That(view.editSettingsPanel.activeInHierarchy, Is.True);
                Assert.That(board.match.gameData.gameSettings.weatherConditions, Is.EqualTo("Snow"));
                Assert.That(board.match.gameData.gameSettings.ballColor, Is.EqualTo("Orange"));
                Assert.That(board.match.gameData.gameSettings.playerAssistance, Is.EqualTo(3));
                Assert.That(view.editWeatherDropdown.captionText.text, Is.EqualTo("Snow"));
                Assert.That(view.editBallColorDropdown.captionText.text, Is.EqualTo("Orange"));
                Assert.That(view.editPlayerAssistanceDropdown.captionText.text, Is.EqualTo("Experienced"));
                board.controls.Escape();
                Assert.That(view.editSettingsPanel.activeInHierarchy, Is.False);
                Assert.That(view.pausePanel.activeInHierarchy, Is.True);
            }
            var token=board.tokens.Single(t=>t.isHomeTeam && t.jerseyNumber==2);
            Assert.That(token.GetCurrentHex().coordinates,Is.EqualTo(new Vector3Int(moved.x,0,moved.z)));Assert.That(token.transform.position.x,Is.EqualTo(board.grid.GetHexCenterForCoordinates(token.GetCurrentHex().coordinates).x).Within(.001));
            Assert.That(board.match.gameData.stats.homeTeamStats.totalGoals,Is.EqualTo(2));Assert.That(board.match.OnlineRegulationSeconds,Is.EqualTo(123));
            Assert.That(board.liveScoreboard.homeScoreText.text,Is.EqualTo("2"));Assert.That(board.liveScoreboard.awayScoreText.text,Is.EqualTo("1"));
            Assert.That(board.authorityRoot.activeSelf,Is.False);Assert.That(board.match.gameData.events,Is.Empty);Assert.That(board.match.TimingRollSequence,Is.Zero);
            Assert.That(board.match.CaptureOnlineDecisionTiming(),Is.Null,"Replica projection must not create an authority clock");
            if (branch == "scorers")
            {
                Assert.That(board.match.homeScorers.Select(goal => goal.scorer), Is.EqualTo(new[] { "Home striker", "Home striker" }));
                Assert.That(board.match.homeScorers[0].assist, Is.EqualTo("Home winger"));
                Assert.That(board.match.homeScorers[1].isPenalty, Is.True);
                Assert.That(board.match.homeScorers[1].minuteLabel, Is.EqualTo("45+2'"));
                Assert.That(board.match.awayScorers.Single().scorer, Is.EqualTo("Away striker"));
                Assert.That(board.match.matchStatsUI.homeScorersText.text, Does.Contain("Home striker"));
                Assert.That(board.match.matchStatsUI.homeScorersText.text, Does.Contain("45+2"));
                Assert.That(board.match.matchStatsUI.awayScorersText.text, Does.Contain("Away striker"));
            }
            if (branch == "f3-label")
            {
                var pitch = board.match.OnlinePitchLines;
                MeshRenderer Background(string name) => pitch.GetComponentsInChildren<MeshRenderer>(true)
                    .Single(renderer => renderer.name == "Background" && renderer.transform.parent.name == name);
                var active = Background(labelSide > 0 ? "NorthEast F3" : "NorthWest F3");
                var inactive = Background(labelSide > 0 ? "NorthWest F3" : "NorthEast F3");
                var block = new MaterialPropertyBlock();
                Color Read(MeshRenderer renderer)
                {
                    renderer.GetPropertyBlock(block);
                    return block.GetColor("_BaseColor");
                }
                Color before = Read(active);
                Assert.That(before, Is.Not.EqualTo(Read(inactive)));
                float stop = Time.realtimeSinceStartup + 2f;
                while (Read(active) == before && Time.realtimeSinceStartup < stop) yield return null;
                Assert.That(Read(active), Is.Not.EqualTo(before), "The replicated label alternates team colors");
            }
            if (branch == "banners")
            {
                var banner = board.match.OnlineEventBanner;
                Assert.That(banner.IsTimingVisible, Is.True);
                Assert.That(banner.timingCaption.text, Does.Contain("remaining"));
                Assert.That(banner.timingDetail.text, Does.Contain("Home"));
                Assert.That(banner.CurrentEvent, Is.EqualTo(MatchBannerEvent.YellowCard));
                Assert.That(banner.detail.text, Is.EqualTo("Home defender"));
                Assert.That(banner.PendingCount, Is.EqualTo(1));
                board.Display(replica.State, "Repeated publication");
                Assert.That(banner.PendingCount, Is.EqualTo(1));
                banner.Advance(5f);
                Assert.That(banner.CurrentEvent, Is.EqualTo(MatchBannerEvent.Foul));
                Assert.That(banner.IsTimingVisible, Is.True);
            }
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
        if (branch is "shared-duel-panel" or "shared-header-panel")
        {
            yield return RunSharedDuelOrHeader(board, protocol, data, branch == "shared-header-panel");
            yield break;
        }
        if (branch.StartsWith("shared-roll:"))
        {
            yield return RunSharedRollPresentation(board, protocol, data, branch.EndsWith(":timeout"));
            yield break;
        }
        if(branch.StartsWith("stoppage:")){var parts=branch.Split(':');yield return RunStoppage(board,protocol,data,bool.Parse(parts[1]),bool.Parse(parts[2]),int.Parse(parts[3]),parts.Length>4);yield break;}
        if(branch.StartsWith("ordinary-half:")){yield return RunNormalOnlineHalf(board,protocol,int.Parse(branch.Split(':')[1]));yield break;}
        if(branch.StartsWith("room-coin:")){var parts=branch.Split(':');yield return RunRoomCoin(board,protocol,data,bool.Parse(parts[1]),bool.Parse(parts[2]),bool.Parse(parts[3]),bool.Parse(parts[4]),bool.Parse(parts[5]),parts.Length>6 && bool.Parse(parts[6]));yield break;}
        if(branch.StartsWith("shootout:")){var parts=branch.Split(':');yield return RunShootout(board,protocol,data,bool.Parse(parts[1]),bool.Parse(parts[2]),parts.Length>3?parts[3]:null);yield break;}
        if(branch.StartsWith("action-preview:")){yield return RunActionPreview(board,protocol,data,branch.Split(':')[1]);yield break;}
        if(branch.StartsWith("instructions:")){yield return RunOnlineInstructions(board,protocol,data,bool.Parse(branch.Split(':')[1]),branch.EndsWith(":live"));yield break;}
        if(branch=="published-highlights"){yield return RunPublishedHighlights(board,protocol,data);yield break;}
        if(branch.StartsWith("motion-smoothing")){yield return RunMotionSmoothing(board,protocol,data,branch.EndsWith("-auto"));yield break;}
        if(branch=="snapshot-pose"){yield return RunSnapshotPose(board,protocol,data);yield break;}
        if(branch.StartsWith("publication:")){yield return RunPublicationCadence(board,protocol,branch.Split(':')[1]);yield break;}
        if(branch=="transport"){yield return RunTransport(board,protocol,size);yield break;}
        if(branch.StartsWith("required-keeper:")){var parts=branch.Split(':');yield return RunRequiredKeeper(board,protocol,data,bool.Parse(parts[1]),parts[2],bool.Parse(parts[3]));yield break;}
        if(branch.StartsWith("substitution-limit-")){yield return RunSubstitutionLimit(board,protocol,data,branch.EndsWith("home"));yield break;}
        if(branch.StartsWith("substitution-")){yield return RunSubstitution(board,protocol,data,branch.EndsWith("home"),branch.Contains("-ui-") || branch.Contains("-authored-"),branch.Contains("-keeper-"),branch.Contains("-authored-"));yield break;}
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
    private IEnumerator RunSubstitution(OnlineGroundPassBoard board,OnlineMatchProtocol protocol,MatchManager.GameData data,bool home,bool ui,bool keeper,bool authored)
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
                if (authored)
                {
                    yield return null;
                    var roomView = Object.FindAnyObjectByType<SubstitutionMenuView>();
                    Assert.That(roomView, Is.Not.Null);
                    var ownRow = roomView.GetRows(home).Single(row => row.gameObject.activeInHierarchy);
                    Assert.That(roomView.GetRows(!home).Any(row => row.gameObject.activeInHierarchy), Is.False);
                    Assert.That(roomView.GetPlayingRows(home).Any(row => row.text.Contains(outgoing.playerName)), Is.True);
                    ownRow.outgoingDropdown.value = ownRow.outgoingDropdown.options.FindIndex(option => option.text.StartsWith(offNumber + ". "));
                    yield return null;
                    ownRow.incomingDropdown.value = ownRow.incomingDropdown.options.FindIndex(option => option.text.StartsWith(onNumber + ". "));
                    Assert.That(roomView.confirmButton.interactable, Is.True);
                    roomView.confirmButton.onClick.Invoke();
                    yield return null;
                    Assert.That(roomView.gameObject.activeSelf, Is.False);
                }
                else
                {
                    menu.outgoingPlayer.value=menu.outgoingPlayer.options.FindIndex(o=>o.text.StartsWith(offNumber+". "));
                    menu.incomingPlayer.value=menu.incomingPlayer.options.FindIndex(o=>o.text.StartsWith(onNumber+". "));
                    Assert.That(menu.substitutionCommitButton.interactable,Is.True);
                    menu.substitutionCommitButton.onClick.Invoke();
                }
                Assert.That(status,Is.EqualTo(OnlineCommandStatus.Accepted));
                Assert.That(menu.substitutionPanel.activeSelf,Is.False);
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
    public IEnumerator RunNativeRoom(bool guest)
    {
        InitializeFixture();
        SuppressLegacyRoomRunner();
        var data = Data(16, guest);
        yield return LoadNativeRoom(data, guest, "native-room-scene");
        Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo("Assets/Scenes/Room.unity"));
        var board = Object.FindAnyObjectByType<OnlineRoomStartup>().Board;
        Assert.That(board.match, Is.SameAs(MatchManager.Instance));
        Assert.That(board.displayedBall, Is.SameAs(board.match.ball.transform));
        Assert.That(board.tokens, Has.Length.EqualTo(32));
        Assert.That(board.tokens.All(token => token.GetComponentInChildren<TMPro.TextMeshPro>(true) != null), Is.True);
        var number = board.match.playerTokenManager.allTokens.First(token => token.isHomeTeam && token.jerseyNumber == 9)
            .GetComponentInChildren<TMPro.TextMeshPro>();
        yield return null;
        AssertVisibleJerseyPixels(board.boardCamera, number);
        Assert.That(board.controls.startButton, Is.Null);
        Assert.That(board.rollPanels.stoppage, Is.SameAs(board.match.stoppageActionsPanel));
        Assert.That(board.match.homeTeamDirection, Is.EqualTo(data.openingSetup.DirectionFor(true)));
        Assert.That(board.match.kickoffManager.gameObject.activeSelf, Is.EqualTo(!guest));
        var input = Object.FindAnyObjectByType<GameInputManager>();
        var inspection = RoomInspectionCard.Instance;
        var stats = Object.FindAnyObjectByType<MatchStatsUI>();
        Assert.That(inspection, Is.Not.Null);
        Assert.That(stats, Is.Not.Null);
        Physics.SyncTransforms();
        foreach (int jersey in new[] { 1, 2 })
        {
            var token = board.tokens.Single(player => player.isHomeTeam && player.jerseyNumber == jersey);
            var ray = new Ray(token.transform.position + Vector3.up * 10f, Vector3.down);
            Assert.That(board.boardInput.PresentHover(ray, true), Is.SameAs(token));
            if (token.IsGoalKeeper)
            {
                var smallCard = stats.GetComponentsInChildren<GoalkeeperCard>(true).Single(card => card.DisplayedToken == token);
                Assert.That(smallCard.gameObject.activeInHierarchy, Is.True);
                AssertAttributeBadge(smallCard.savingValueText, token.saving);
            }
            else
            {
                var smallCard = stats.GetComponentsInChildren<PlayerCard>(true).Single(card => card.DisplayedToken == token);
                Assert.That(smallCard.gameObject.activeInHierarchy, Is.True);
                AssertAttributeBadge(smallCard.paceValueText, token.pace);
            }
            input.SetLineupHoverToken(token);
            Assert.That(board.boardInput.TryKey(KeyCode.Space), Is.True);
            Assert.That(inspection.LockedPlayer, Is.SameAs(token));
            board.controls.Escape();
            Assert.That(inspection.IsOpen, Is.False);
        }
        input.SetLineupHoverToken(null);
        if (guest)
        {
            var before = board.match.CaptureOnlineDecisionTiming();
            board.match.AdvanceOnlineAuthorityTime(5f);
            Assert.That(board.match.CaptureOnlineDecisionTiming(), Is.EqualTo(before));
        }
        else
        {
            var version = new OnlineCompatibility("ground-wire-v7", "ground-rules-v1", OnlineFullRoomBootstrap.Catalog);
            var protocol = OnlineFrontendSessionPolicy.CreateMatchProtocol("native-room-scene", "home", "away", version);
            protocol.BindAuthenticatedAccount("home", "h", version);
            protocol.BindAuthenticatedAccount("away", "a", version);
            var authority = new OnlineGroundPassAuthority(board.match, protocol);
            using var nativePause = new OnlineSharedPause(protocol, board.match);
            var initial = nativePause.Capture(authority);
            Assert.That(initial.stage, Is.EqualTo("kickoff.setup"));
            Assert.That(initial.tokens, Has.Length.EqualTo(32));
            var pauseView = Object.FindAnyObjectByType<PauseMenuManager>(FindObjectsInactive.Include);
            board.controls.Configure(board.localTeam);
            board.controls.Requested += message =>
            {
                var receipt = nativePause.Submit("h", message.ToCommand());
                Assert.That(receipt.Status, Is.EqualTo(OnlineCommandStatus.Accepted));
                board.controls.Reply(new OnlineGroundPassMessage { status = receipt.Status.ToString() });
                board.Display(nativePause.Capture(authority), "host");
            };
            board.Display(initial, "host");
            board.controls.Escape();
            Assert.That(pauseView.pausePanel.activeInHierarchy, Is.True);
            Assert.That(pauseView.resumeButton.interactable, Is.True);
            pauseView.resumeButton.onClick.Invoke();
            Assert.That(pauseView.pausePanel.activeSelf, Is.False);
            Assert.That(nativePause.IsPaused, Is.False);
        }
    }

    private static void AssertAttributeBadge(TMPro.TMP_Text value, int expected)
    {
        Assert.That(value.text, Is.EqualTo(expected.ToString()));
        var badge = value.transform.parent.Find(value.name + "Badge");
        Assert.That(badge, Is.Not.Null);
        var image = badge.GetComponent<UnityEngine.UI.Image>();
        Assert.That(image.sprite, Is.Not.Null);
        Assert.That(image.color, Is.EqualTo(MatchStatsUI.GetInspectionAttributeColor(expected)));
        Assert.That(badge.GetSiblingIndex(), Is.LessThan(value.transform.GetSiblingIndex()));
    }

    private static void AssertVisibleJerseyPixels(Camera camera, TMPro.TextMeshPro sample, string artifactPath = null)
    {
        var numberRenderer = sample.GetComponent<MeshRenderer>();
        var previousTarget = camera.targetTexture;
        var previousActive = RenderTexture.active;
        var target = new RenderTexture(1600, 900, 24);
        var crop = new Texture2D(40, 40, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = target;
            var center = camera.WorldToScreenPoint(sample.transform.position);
            var rect = new Rect(Mathf.Clamp(center.x - 20, 0, 1560), Mathf.Clamp(center.y - 20, 0, 860), 40, 40);
            Color32[] Render()
            {
                camera.Render();
                RenderTexture.active = target;
                crop.ReadPixels(rect, 0, 0);
                crop.Apply();
                return crop.GetPixels32();
            }
            var withNumber = Render();
            if (artifactPath != null) File.WriteAllBytes(artifactPath, crop.EncodeToPNG());
            numberRenderer.enabled = false;
            var withoutNumber = Render();
            Assert.That(withNumber.Where((pixel, i) => !pixel.Equals(withoutNumber[i])).Count(), Is.GreaterThan(8));
        }
        finally
        {
            numberRenderer.enabled = true;
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            Object.Destroy(target);
            Object.Destroy(crop);
        }
    }

    public IEnumerator RunOfflineNativeRoom(string mode)
    {
        InitializeFixture();
        SuppressLegacyRoomRunner();
        var data = Data(16, false);
        data.gameSettings.gameMode = mode;
        string path = Path.Combine(directory, "offline-room.json");
        File.WriteAllText(path, JsonConvert.SerializeObject(data));
        app.SetSelectedRoomGameMode(mode);
        app.SetActiveSaveFilePath(path);
        previousFormationFlag = app.UseRegularFormationOnNextRoomLoad;
        app.PrepareRoomLaunchWithRegularFormation();
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Room.unity", new LoadSceneParameters(LoadSceneMode.Single));
        var startup = Object.FindAnyObjectByType<OnlineRoomStartup>();
        Assert.That(startup, Is.Not.Null);
        var match = MatchManager.Instance;
        Assert.That(match, Is.Not.Null);
        float deadline = Time.realtimeSinceStartup + 15f;
        while (match.ball.GetCurrentHex() == null && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(match.ball.GetCurrentHex(), Is.Not.Null);
        Assert.That(match.gameData.gameSettings.gameMode, Is.EqualTo(mode));
        Assert.That(match.playerTokenManager.allTokens, Has.Count.EqualTo(22));
        Assert.That(startup.Board, Is.Null);
        Assert.That(Object.FindAnyObjectByType<RoomInspectionCard>(), Is.Not.Null);
    }

    private void SuppressLegacyRoomRunner()
    {
        previousFrameworkFlag = UnityEditor.SessionState.GetBool("CounterAttack.FrameworkTests", false);
        UnityEditor.SessionState.SetBool("CounterAttack.FrameworkTests", true);
    }

    public IEnumerator Cleanup()
    {
        if(app==null)yield break;
        if (departureFrontend != null) departureFrontend.Close();
        pause?.Dispose();pause=null;
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/MainMenu.unity",new LoadSceneParameters(LoadSceneMode.Single));session?.Dispose();session=null;
        if(previousExplicit)app.SetActiveSaveFilePath(previousPath);else{app.ClearActiveSaveContext();app.LastSavedFileName=previousPath;}app.SetSelectedRoomGameMode(previousMode);
        RestoreGlobals();if(created)Object.Destroy(app.gameObject);UnityEngine.Random.state=random;Time.timeScale=scale;
        if (previousFormationFlag.HasValue)
        {
            app.ConsumeRegularFormationRoomLaunch();
            if (previousFormationFlag.Value) app.PrepareRoomLaunchWithRegularFormation();
            previousFormationFlag = null;
        }
        if (previousFrameworkFlag.HasValue)
        {
            UnityEditor.SessionState.SetBool("CounterAttack.FrameworkTests", previousFrameworkFlag.Value);
            previousFrameworkFlag = null;
        }
        if(directory!=null)
        {
            string full=Path.GetFullPath(directory),parent=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"CounterattackFullRoom"))+Path.DirectorySeparatorChar;
            if(!full.StartsWith(parent,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Full Room fixture cleanup escaped owned temporary root");Directory.Delete(full,true);
        }
        yield return null;
    }
}
