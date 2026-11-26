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

public static class OnlineGroundPassSceneAuthoring
{
    public const string ScenePath = "Assets/Scenes/OnlineGroundPassProof.unity";
    private const string AssetFolder = "Assets/Settings/OnlineGroundPassProof";

    [CliCommand("prepare_online_ground_pass_proof", "Author the separate authority/replica ground-pass proof scene")]
    public static string Prepare()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || SceneManager.sceneCount != 1)
            throw new InvalidOperationException("Expected one clean saved scene and stopped Editor");
        var previous = SceneManager.GetActiveScene();
        if (previous.isDirty || string.IsNullOrEmpty(previous.path)) throw new InvalidOperationException("Save the current scene first");
        if (File.Exists(ScenePath)) return ScenePath;
        var previousPath = previous.path;
        try
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Directory.CreateDirectory(AssetFolder); AssetDatabase.Refresh();
            var controller = new GameObject("Online Ground Pass Proof");
            var board = controller.AddComponent<OnlineGroundPassBoard>();
            controller.AddComponent<EOSStandaloneProof>().gameplayBoard = board;
            var root = new GameObject("Authority only - inactive on guest"); root.SetActive(false); board.authorityRoot = root;
            var components = new List<MonoBehaviour>();
            T Add<T>(bool active = true) where T : MonoBehaviour
            {
                var obj = new GameObject(typeof(T).Name); obj.SetActive(false); obj.transform.SetParent(root.transform);
                var component = obj.AddComponent<T>(); component.enabled = false; components.Add(component);
                obj.SetActive(active); return component;
            }
            board.match = Add<MatchManager>(false); board.grid = Add<HexGrid>();
            Add<Ball>(); Add<PlayerTokenManager>(); Add<MovementPhaseManager>(); Add<GroundBallManager>();
            Add<FirstTimePassManager>(); Add<HighPassManager>(); Add<LongBallManager>();
            Add<LooseBallManager>(); Add<GoalKeeperManager>(); Add<ShotManager>(); Add<HeaderManager>();
            Add<FinalThirdManager>(); Add<FreeKickManager>(); Add<HelperFunctions>();
            foreach (var owner in components)
                foreach (var field in owner.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
                    if (typeof(MonoBehaviour).IsAssignableFrom(field.FieldType) && field.FieldType != typeof(HexCell)
                        && field.FieldType != typeof(PlayerToken))
                    {
                        var dependency = components.FirstOrDefault(c => field.FieldType.IsInstanceOfType(c));
                        if (dependency != null) field.SetValue(owner, dependency);
                    }
            var dark = Material("Pitch", new Color(.05f, .22f, .13f));
            var home = Material("Home", new Color(.15f, .5f, 1));
            var away = Material("Away", new Color(1, .25f, .15f));
            var white = Material("Ball", Color.white);
            var cells = new List<HexCell>();
            var assignFeatures = typeof(HexGrid).GetMethod("AssignHexFeatures", BindingFlags.Instance | BindingFlags.NonPublic);
            for (var x = 0; x < board.grid.GridWidth; x++)
                for (var z = 0; z < board.grid.GridHeight; z++)
                {
                    var obj = new GameObject("Hex " + x + "," + z); obj.SetActive(false); obj.transform.SetParent(root.transform);
                    var renderer = obj.AddComponent<MeshRenderer>(); renderer.sharedMaterial = dark;
                    var hex = obj.AddComponent<HexCell>(); hex.enabled = false; hex.hexRenderer = renderer;
                    hex.coordinates = new Vector3Int(x - board.grid.GridWidth / 2, 0, z - board.grid.GridHeight / 2);
                    hex.hexRadius = board.grid.HexRadius; obj.transform.position = board.grid.GetHexCenterForCoordinates(hex.coordinates);
                    assignFeatures.Invoke(board.grid, new object[] { hex }); obj.SetActive(true); cells.Add(hex);
                }
            board.cells = cells.ToArray();
            // Same central interception geometry as the focused owned fixture.
            board.initialCoordinates = new[] { new Vector3Int(0,0,0), new Vector3Int(1,0,3),
                new Vector3Int(-16,0,0), new Vector3Int(16,0,0), new Vector3Int(0,0,6), new Vector3Int(8,0,8) };
            var names = new[] { "Home carrier", "Away interceptor", "Home keeper", "Away keeper", "Home receiver", "Away backline" };
            var jerseys = new[] { 9, 8, 1, 1, 10, 6 };
            var isHome = new[] { true, false, true, false, true, false };
            board.tokens = new PlayerToken[names.Length]; board.displayedTokens = new Transform[names.Length];
            for (var i = 0; i < names.Length; i++)
            {
                var token = Add<PlayerToken>(); token.name = token.playerName = names[i]; token.jerseyNumber = jerseys[i];
                token.isHomeTeam = token.isAttacker = isHome[i]; token.pace = 5; token.dribbling = token.tackling = token.saving = 4;
                board.tokens[i] = token;
                var display = Primitive(names[i] + " display", PrimitiveType.Cylinder, isHome[i] ? home : away);
                display.transform.position = board.grid.GetHexCenterForCoordinates(board.initialCoordinates[i]) + Vector3.up * .5f;
                display.transform.localScale = new Vector3(.7f, .5f, .7f); board.displayedTokens[i] = display.transform;
            }
            var ballDisplay = Primitive("Published ball", PrimitiveType.Sphere, white);
            ballDisplay.transform.position = Vector3.up * 1.2f; ballDisplay.transform.localScale = Vector3.one * .4f;
            board.displayedBall = ballDisplay.transform;
            var pitch = Primitive("Prepared pitch", PrimitiveType.Cube, dark);
            pitch.transform.position = new Vector3(0,-.2f,0); pitch.transform.localScale = new Vector3(38,.2f,28);
            var camera = new GameObject("Personal Camera", typeof(Camera)).GetComponent<Camera>();
            camera.transform.position = new Vector3(0,30,0); camera.transform.rotation = Quaternion.Euler(90,0,0);
            camera.orthographic = true; camera.orthographicSize = 16; camera.backgroundColor = new Color(.025f,.04f,.06f);
            camera.clearFlags = CameraClearFlags.SolidColor; board.boardCamera = camera;
            var canvas = new GameObject("Proof status", typeof(Canvas), typeof(CanvasScaler));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var textObj = new GameObject("Local statistics - Tab to hide", typeof(RectTransform), typeof(Text)); textObj.transform.SetParent(canvas.transform, false);
            board.statusText = textObj.GetComponent<Text>(); board.localStatsPanel = textObj;
            board.statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); board.statusText.fontSize = 18;
            board.statusText.color = Color.white; board.statusText.text = "Waiting for EOS gameplay proof\nRight-drag: pan | Wheel: zoom | Tab: local stats";
            var rect = textObj.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(0,1); rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0,1); rect.anchoredPosition = new Vector2(16,-16); rect.sizeDelta = new Vector2(-32,130);
            AssetDatabase.SaveAssets();
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new InvalidOperationException("Could not save proof scene");
        }
        finally { EditorSceneManager.OpenScene(previousPath, OpenSceneMode.Single); }
        return ScenePath;
    }

    private static Material Material(string name, Color color)
    {
        var path = AssetFolder + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) { material = new Material(Shader.Find("Sprites/Default")) { color = color }; AssetDatabase.CreateAsset(material, path); }
        return material;
    }
    private static GameObject Primitive(string name, PrimitiveType type, Material material)
    {
        var obj = GameObject.CreatePrimitive(type); obj.name = name;
        obj.GetComponent<Renderer>().sharedMaterial = material;
        UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());
        return obj;
    }
}
