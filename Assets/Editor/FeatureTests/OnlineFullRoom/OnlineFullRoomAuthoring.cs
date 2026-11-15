using System;
using System.Linq;
using System.Reflection;
using CounterAttack.Online;
using TMPro;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class OnlineFullRoomAuthoring
{
    public const string ScenePath="Assets/Scenes/OnlineRoom.unity";
    [CliCommand("repair_online_full_room_controls","Bind authored shared pause messages and separate match/command status")]
    public static string RepairControls()
    {
        string previous=SceneManager.GetActiveScene().path;
        if(EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().isDirty || SceneManager.sceneCount!=1)throw new InvalidOperationException("Expected one clean stopped scene");
        try
        {
            var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);var board=UnityEngine.Object.FindAnyObjectByType<OnlineGroundPassBoard>();
            board.requesterPauseMessage=board.sharedPausePanel.transform.Find("Requester pause").gameObject;board.passivePauseMessage=board.sharedPausePanel.transform.Find("Passive pause").gameObject;
            board.liveScoreboard=UnityEngine.Object.FindAnyObjectByType<ScoreboardManager>();
            if(board.statusText==board.controls.commandStatus)
            {
                var status=new GameObject("Match connection and state",typeof(RectTransform),typeof(Text));status.transform.SetParent(board.controls.GetComponentInParent<Canvas>().transform,false);
                var text=status.GetComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=18;text.color=Color.white;text.raycastTarget=false;
                var rect=status.GetComponent<RectTransform>();rect.anchorMin=new Vector2(0,1);rect.anchorMax=new Vector2(1,1);rect.pivot=new Vector2(.5f,1);rect.sizeDelta=new Vector2(-40,100);rect.anchoredPosition=new Vector2(0,-10);board.statusText=text;board.localStatsPanel=status;
            }
            EditorUtility.SetDirty(board);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return "Bound full Room status and shared pause messages";
        }
        finally{if(!string.IsNullOrEmpty(previous))EditorSceneManager.OpenScene(previous,OpenSceneMode.Single);}
    }
    [CliCommand("author_online_full_room","Author a full roster online Room from normal Room with inspectable real pitch/tokens")]
    public static string Prepare()
    {
        var original=SceneManager.GetActiveScene();string previous=original.path;
        if(EditorApplication.isPlayingOrWillChangePlaymode || original.isDirty || SceneManager.sceneCount!=1)throw new InvalidOperationException("Expected one clean stopped scene");
        if(System.IO.File.Exists(ScenePath))throw new InvalidOperationException("Full online Room already exists; inspect before editing");
        const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
        try
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Room.unity",OpenSceneMode.Single);
            if(!EditorSceneManager.SaveScene(scene,ScenePath))throw new InvalidOperationException("Could not create owned online Room");
            var gate=UnityEngine.Object.FindAnyObjectByType<OnlineRoomStartup>();
            var owner=new GameObject("Full online Room");var bootstrap=owner.AddComponent<OnlineFullRoomBootstrap>();var board=owner.AddComponent<OnlineGroundPassBoard>();bootstrap.board=board;board.liveFullRoom=true;board.continuousManualSession=true;
            board.match=gate.authorityRoots.Single(r=>r.name=="MatchManager").GetComponent<MatchManager>();board.grid=board.match.hexGrid;
            var authority=new GameObject("Online rules - inactive on replica");authority.SetActive(false);board.authorityRoot=authority;
            foreach(var root in gate.authorityRoots)root.transform.SetParent(authority.transform,true);
            foreach(var component in authority.GetComponentsInChildren<MonoBehaviour>(true))component.enabled=false;
            var render=new GameObject("Authored full pitch and roster");
            board.grid.cells=new HexCell[board.grid.GridWidth,board.grid.GridHeight];
            typeof(HexGrid).GetMethod("ApplyPaletteForWeather",flags).Invoke(board.grid,new object[]{"Clear"});
            typeof(HexGrid).GetMethod("CreateGrid",flags).Invoke(board.grid,new object[]{null});
            board.cells=board.grid.cells.Cast<HexCell>().ToArray();
            foreach(var cell in board.cells){cell.transform.SetParent(render.transform,true);cell.enabled=false;}
            var manager=board.match.playerTokenManager;
            var prefab=(GameObject)typeof(PlayerTokenManager).GetMethod("GetTokenBasePrefab",flags).Invoke(manager,null);
            board.tokens=new PlayerToken[36];board.displayedTokens=new Transform[36];
            for(int team=0;team<2;team++)for(int slot=0;slot<18;slot++)
            {
                int index=team*18+slot,jersey=slot+1;bool home=team==0;
                var obj=(GameObject)PrefabUtility.InstantiatePrefab(prefab,render.transform);obj.name=(home?"Home ":"Away ")+jersey;
                obj.layer=LayerMask.NameToLayer("Token");obj.transform.position=new Vector3((home?-1:1)*(3+slot%9),.2f,-12-slot/9);
                var token=obj.GetComponent<PlayerToken>()??obj.AddComponent<PlayerToken>();token.enabled=false;token.isHomeTeam=home;token.jerseyNumber=jersey;
                var visuals=obj.GetComponent<PlayerTokenVisuals>()??obj.AddComponent<PlayerTokenVisuals>();var style=TokenKitCatalog.ResolveStyle(home?"088":"021");visuals.ApplyStyle(style);
                var number=(GameObject)PrefabUtility.InstantiatePrefab(manager.textPrefab,obj.transform);number.name="Authored jersey number";
                number.transform.localPosition=new Vector3(0,1.06f,0);number.transform.localRotation=Quaternion.Euler(90,0,0);
                var text=number.GetComponent<TextMeshPro>();text.text=jersey.ToString();visuals.ApplyNumberStyle(text,style);
                board.tokens[index]=token;board.displayedTokens[index]=obj.transform;
            }
            var ball=UnityEngine.Object.Instantiate(board.match.ball.gameObject,render.transform);ball.name="Replica ball";
            foreach(var script in ball.GetComponentsInChildren<MonoBehaviour>(true))script.enabled=false;
            foreach(var collider in ball.GetComponentsInChildren<Collider>(true))collider.enabled=false;
            bootstrap.replicaBallRenderer=ball.GetComponentInChildren<Renderer>(true);board.displayedBall=ball.transform;
            board.boardCamera=Camera.main;
            var canvas=new GameObject("Online match controls",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;canvas.GetComponent<Canvas>().sortingOrder=200;
            var controlsAsset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Settings/OnlineGroundPassProof/ManualControls.prefab");
            var controls=(GameObject)PrefabUtility.InstantiatePrefab(controlsAsset,canvas.transform);board.controls=controls.GetComponent<OnlineGroundPassControls>();
            var pauseAsset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Settings/OnlineGroundPassProof/SharedPause.prefab");
            var pause=(GameObject)PrefabUtility.InstantiatePrefab(pauseAsset,canvas.transform);board.sharedPausePanel=pause;
            board.requesterPauseMessage=pause.transform.Find("Requester pause")?.gameObject;board.passivePauseMessage=pause.transform.Find("Passive pause")?.gameObject;pause.SetActive(false);
            board.statusText=board.controls.commandStatus;
            EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("Could not save full online Room");
            if(!EditorBuildSettings.scenes.Any(s=>s.path==ScenePath))EditorBuildSettings.scenes=EditorBuildSettings.scenes.Concat(new[]{new EditorBuildSettingsScene(ScenePath,true)}).ToArray();
            AssetDatabase.SaveAssets();return "Authored real Room, "+board.cells.Length+" hexes, 36 roster slots and network controls";
        }
        finally{if(!string.IsNullOrEmpty(previous))EditorSceneManager.OpenScene(previous,OpenSceneMode.Single);}
    }
}
