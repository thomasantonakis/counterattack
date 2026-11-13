using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CounterAttack.Online;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class OnlineRoomProofAuthoring
{
    public const string ScenePath = "Assets/Scenes/OnlineRoomProof.unity";
    [CliCommand("author_online_room_proof", "Author the deterministic online action proof using Room's real serialized managers")]
    public static string Prepare()
    {
        var original = SceneManager.GetActiveScene(); var previousPath = original.path;
        if (EditorApplication.isPlayingOrWillChangePlaymode || original.isDirty || SceneManager.sceneCount != 1)
            throw new InvalidOperationException("Expected one clean stopped scene");
        if (File.Exists(ScenePath)) return ScenePath;
        try
        {
            var prepared = EditorSceneManager.OpenScene(OnlineGroundPassSceneAuthoring.ScenePath, OpenSceneMode.Single);
            var source = UnityEngine.Object.FindAnyObjectByType<OnlineGroundPassBoard>();
            var coordinates = source.initialCoordinates.ToArray();
            var controlsPath = "Assets/Settings/OnlineGroundPassProof/ManualControls.prefab";
            PrefabUtility.SaveAsPrefabAsset(source.controls.gameObject, controlsPath);
            var pausePath = "Assets/Settings/OnlineGroundPassProof/SharedPause.prefab";
            PrefabUtility.SaveAsPrefabAsset(source.sharedPausePanel, pausePath);
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Room.unity", OpenSceneMode.Single);
            // Save as a dedicated owned Room proof. Never replace the designer Room.
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new InvalidOperationException("Could not create Room proof");
            var gate = UnityEngine.Object.FindAnyObjectByType<OnlineRoomStartup>();
            var controller = new GameObject("Online Room action proof");
            controller.AddComponent<OnlineRoomProofStartup>();
            var board = controller.AddComponent<OnlineGroundPassBoard>();
            controller.AddComponent<EOSStandaloneProof>().gameplayBoard = board;
            var authorityRoot = new GameObject("Room authority - inactive on replica"); authorityRoot.SetActive(false);
            board.authorityRoot = authorityRoot;
            board.match = gate.authorityRoots.Single(root => root.name == "MatchManager").GetComponent<MatchManager>();
            board.grid = gate.authorityRoots.Single(root => root.name == "HexGrid").GetComponent<HexGrid>();
            foreach (var root in gate.authorityRoots) root.transform.SetParent(authorityRoot.transform, true);
            foreach (var component in authorityRoot.GetComponentsInChildren<MonoBehaviour>(true)) component.enabled = false;
            board.match.gameObject.SetActive(false);
            var dark = AssetDatabase.LoadAssetAtPath<Material>("Assets/Settings/OnlineGroundPassProof/Pitch.mat");
            var home = AssetDatabase.LoadAssetAtPath<Material>("Assets/Settings/OnlineGroundPassProof/Home.mat");
            var away = AssetDatabase.LoadAssetAtPath<Material>("Assets/Settings/OnlineGroundPassProof/Away.mat");
            var white = AssetDatabase.LoadAssetAtPath<Material>("Assets/Settings/OnlineGroundPassProof/Ball.mat");
            var cells = new List<HexCell>();
            var features = typeof(HexGrid).GetMethod("AssignHexFeatures", BindingFlags.Instance | BindingFlags.NonPublic);
            for (int x = 0; x < board.grid.GridWidth; x++) for (int z = 0; z < board.grid.GridHeight; z++)
            {
                var obj = new GameObject("Authored Room hex " + x + "," + z); obj.transform.SetParent(authorityRoot.transform);
                var renderer = obj.AddComponent<MeshRenderer>(); renderer.sharedMaterial = dark;
                var cell = obj.AddComponent<HexCell>(); cell.enabled = false; cell.hexRenderer = renderer;
                cell.coordinates = new Vector3Int(x - board.grid.GridWidth / 2,0,z - board.grid.GridHeight / 2);
                cell.hexRadius = board.grid.HexRadius; obj.transform.position = board.grid.GetHexCenterForCoordinates(cell.coordinates);
                features.Invoke(board.grid, new object[] { cell }); cells.Add(cell);
            }
            board.cells = cells.ToArray(); board.initialCoordinates = coordinates;
            var jerseys = new[] { 9,8,1,1,10,6 }; var homes = new[] { true,false,true,false,true,false };
            board.tokens = new PlayerToken[6]; board.displayedTokens = new Transform[6];
            GameObject Display(string name, PrimitiveType primitive, Material material)
            {
                var obj = GameObject.CreatePrimitive(primitive); obj.name = name; obj.GetComponent<Renderer>().sharedMaterial = material;
                UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>()); return obj;
            }
            for (var i = 0; i < 6; i++)
            {
                var obj = new GameObject("Fixture " + (homes[i] ? "Home " : "Away ") + jerseys[i]); obj.transform.SetParent(authorityRoot.transform);
                var token = obj.AddComponent<PlayerToken>(); token.enabled = false; token.playerName = obj.name; token.jerseyNumber = jerseys[i];
                token.isHomeTeam = token.isAttacker = homes[i]; token.pace = 5; token.dribbling = token.tackling = token.saving = 4;
                board.tokens[i] = token;
                var display = Display("Published " + obj.name, PrimitiveType.Cylinder, homes[i] ? home : away);
                display.transform.position = board.grid.GetHexCenterForCoordinates(coordinates[i]) + Vector3.up * .5f;
                display.transform.localScale = new Vector3(.7f,.5f,.7f); board.displayedTokens[i] = display.transform;
            }
            board.displayedBall = Display("Published Room ball", PrimitiveType.Sphere, white).transform;
            board.displayedBall.localScale = Vector3.one * .4f; board.displayedBall.position = Vector3.up * 1.2f;
            board.boardCamera = Camera.main;
            var canvas = new GameObject("Owned online Room controls", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay; canvas.GetComponent<Canvas>().sortingOrder = 200;
            board.controls = ((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(controlsPath))).GetComponent<OnlineGroundPassControls>();
            board.controls.transform.SetParent(canvas.transform, false);
            board.sharedPausePanel = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(pausePath));
            board.sharedPausePanel.transform.SetParent(canvas.transform, false);
            board.requesterPauseMessage = board.sharedPausePanel.transform.Find("Requester pause").gameObject;
            board.passivePauseMessage = board.sharedPausePanel.transform.Find("Passive pause").gameObject;
            var label = new GameObject("Online authority status", typeof(RectTransform), typeof(Text)); label.transform.SetParent(canvas.transform, false);
            board.statusText = label.GetComponent<Text>(); board.statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            board.statusText.fontSize = 18; board.statusText.color = Color.white; board.localStatsPanel = label;
            var rect = label.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(0,1); rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0,1); rect.anchoredPosition = new Vector2(16,-16); rect.sizeDelta = new Vector2(-32,100);
            AssetDatabase.SaveAssets();
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save Room action proof");
            return ScenePath;
        }
        finally { EditorSceneManager.OpenScene(previousPath, OpenSceneMode.Single); }
    }
}
