#if UNITY_EDITOR
using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CounterAttack.Editor
{
    /// <summary>Dedicated additive authoring operation; never rebuilds stadium/crowd meshes.</summary>
    public static class RoomFixtureLightingAuthoring
    {
        public const string Folder = "Assets/Environment/RoomStadium/FixtureLighting";
        public const string RibbonPath = Folder + "/StadiumRibbon.prefab";
        private const string StadiumPath = "Assets/Environment/RoomStadium/RoomStadium.prefab";

        [MenuItem("CounterAttack/Room/Author Fixture Lighting")]
        public static void Author()
        {
            Scene room = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || room.path != "Assets/Scenes/Room.unity")
                throw new InvalidOperationException("Open Room in Edit Mode to author fixture lighting.");
            if (Find<RoomLightingController>(room) != null)
                throw new InvalidOperationException("Fixture lighting is already authored. Adjust its existing objects and presets in the Inspector.");
            Light sun = RenderSettings.sun;
            if (sun == null) throw new InvalidOperationException("Room must have its daylight sun assigned.");
            EnsureFolder(Folder);
            RoomLightingPreset day = Daylight(sun);
            RoomLightingPreset sunset = Evening(false);
            RoomLightingPreset night = Evening(true);
            AuthorRibbon();
            var stadium = Find<StadiumCrowdController>(room);
            if (stadium == null) throw new InvalidOperationException("Room stadium not found.");
            var ribbon = stadium.GetComponentInChildren<StadiumRibbonController>(true);
            // Existing prefab instances inherit the new nested ribbon after the prefab save.
            if (ribbon == null) throw new InvalidOperationException("Nested ribbon has not propagated to Room.");

            var root = new GameObject("Fixture Lighting");
            Undo.RegisterCreatedObjectUndo(root, "Author fixture lighting");
            var controller = root.AddComponent<RoomLightingController>();
            controller.sun = sun; controller.midAfternoon = day; controller.sunset = sunset;
            controller.lateNight = night; controller.ribbon = ribbon;
            int index = 0;
            foreach (int x in new[] { -1, 1 }) foreach (int z in new[] { -1, 1 })
            {
                var go = new GameObject($"Floodlight {(x < 0 ? "West" : "East")} {(z < 0 ? "South" : "North")}");
                go.transform.SetParent(root.transform, false);
                go.transform.position = new Vector3(x * 24.19f, 40f, z * 21.08f);
                go.transform.rotation = Quaternion.LookRotation(new Vector3(0, .05f, 0) - go.transform.position);
                Light light = go.AddComponent<Light>();
                light.type = LightType.Spot; light.range = 75; light.spotAngle = 92; light.innerSpotAngle = 65;
                light.renderMode = LightRenderMode.ForcePixel; light.shadows = LightShadows.Soft;
                light.shadowStrength = .65f; light.shadowBias = .025f; light.shadowNormalBias = .12f;
                light.shadowNearPlane = .2f; light.bounceIntensity = 0; light.enabled = false;
                controller.floodlights[index++] = light;
            }
            controller.Apply(FixtureLighting.MidAfternoon);
            var pause = Find<PauseMenuManager>(room);
            if (pause == null) throw new InvalidOperationException("Room pause settings not found.");
            pause.editFixtureLightingDropdown = CloneDropdown(pause.editWeatherDropdown, "EditFixtureLightingDropdown");
            FitPauseSettingsBackground(pause);
            EditorUtility.SetDirty(pause); EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(room);
            AuthorSetupDropdown();
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(room);
            Debug.Log("Fixture lighting authored: three presets, four editable spotlights, nested 256-tile ribbon, setup and pause dropdowns.");
        }

        private static RoomLightingPreset Daylight(Light sun)
        {
            var p = ScriptableObject.CreateInstance<RoomLightingPreset>();
            p.sky = RenderSettings.skybox; p.ambientMode = RenderSettings.ambientMode;
            p.ambientSky = RenderSettings.ambientSkyColor; p.ambientEquator = RenderSettings.ambientEquatorColor;
            p.ambientGround = RenderSettings.ambientGroundColor; p.ambientIntensity = RenderSettings.ambientIntensity;
            p.reflectionMode = RenderSettings.defaultReflectionMode;
            p.reflection = RenderSettings.customReflectionTexture as Cubemap;
            p.reflectionIntensity = RenderSettings.reflectionIntensity;
            p.sunlight = sun.enabled; p.sunColor = sun.color; p.sunIntensity = sun.intensity;
            p.sunShadows = sun.shadows;
            p.sunEuler = sun.transform.localEulerAngles;
            AssetDatabase.CreateAsset(p, Folder + "/MidAfternoon.asset"); return p;
        }

        private static RoomLightingPreset Evening(bool night)
        {
            string name = night ? "LateNight" : "Sunset";
            var p = ScriptableObject.CreateInstance<RoomLightingPreset>();
            Color zenith = night ? new Color(.012f,.026f,.075f) : new Color(.13f,.21f,.39f);
            Color horizon = night ? new Color(.055f,.075f,.13f) : new Color(.39f,.28f,.36f);
            Color ground = night ? new Color(.008f,.012f,.025f) : new Color(.065f,.055f,.08f);
            var sky = new Material(Shader.Find("CounterAttack/Room Fixture Sky"));
            sky.SetColor("_Zenith", zenith); sky.SetColor("_Horizon", horizon); sky.SetColor("_Ground", ground);
            sky.SetColor("_Glow", new Color(.65f,.29f,.10f)); sky.SetFloat("_GlowStrength", night ? .025f : .22f);
            sky.SetFloat("_Stars", night ? 1 : 0); AssetDatabase.CreateAsset(sky, Folder + "/" + name + "Sky.mat");
            p.sky = sky; p.ambientMode = AmbientMode.Trilight;
            p.ambientSky = night ? new Color(.24f,.29f,.40f) : new Color(.34f,.36f,.44f);
            p.ambientEquator = night ? new Color(.19f,.22f,.29f) : new Color(.30f,.26f,.28f);
            p.ambientGround = new Color(.10f,.12f,.15f);
            p.sunlight = !night; p.sunEuler = new Vector3(12, -30, 0);
            p.sunColor = new Color(1,.66f,.40f); p.sunIntensity = .38f;
            // Keep the low warm sun's illumination; raised floodlights own the short token shadows.
            p.sunShadows = LightShadows.None;
            p.floodlights = true; p.floodlightColor = new Color(.94f,.96f,1);
            p.floodlightIntensity = night ? 3.4f : 2.9f;
            p.ribbonColor = night ? new Color(.94f,.97f,1) : new Color(1,.96f,.89f);
            p.ribbonBrightness = night ? 1.15f : 1.05f; p.reflectionIntensity = night ? .35f : .55f;
            p.reflection = Reflection(name, zenith, horizon, ground);
            AssetDatabase.CreateAsset(p, Folder + "/" + name + ".asset"); return p;
        }

        private static Cubemap Reflection(string name, Color top, Color horizon, Color ground)
        {
            // Persistent, mipmapped environment approximation of the authored sky gradient.
            const int size = 64;
            var cube = new Cubemap(size, TextureFormat.RGBAHalf, true);
            for (int face = 0; face < 6; face++)
            {
                var colors = new Color[size * size];
                for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
                {
                    float u = 2f * (x + .5f) / size - 1, v = 2f * (y + .5f) / size - 1;
                    Vector3 d = face switch {
                        0 => new Vector3(1,-v,-u), 1 => new Vector3(-1,-v,u),
                        2 => new Vector3(u,1,v), 3 => new Vector3(u,-1,-v),
                        4 => new Vector3(u,-v,1), _ => new Vector3(-u,-v,-1)
                    };
                    float height = d.normalized.y;
                    colors[y * size + x] = height > 0 ? Color.Lerp(horizon, top, Mathf.Pow(height,.55f))
                        : Color.Lerp(horizon, ground, Mathf.Clamp01(-height * 5));
                }
                cube.SetPixels(colors, (CubemapFace)face);
            }
            cube.Apply(true, false); AssetDatabase.CreateAsset(cube, Folder + "/" + name + "Reflection.asset");
            return cube;
        }

        [MenuItem("CounterAttack/Room/Author Segmented Ribbon Only")]
        public static void AuthorRibbon()
        {
            EnsureFolder(Folder);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(RibbonPath) != null)
                throw new InvalidOperationException("Ribbon exists; edit the authored prefab instead of replacing designer changes.");
            var material = new Material(Shader.Find("Unlit/Color"));
            material.color = Color.white; material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            AssetDatabase.CreateAsset(material, Folder + "/Ribbon.mat");
            var mesh = new Mesh { name = "Shared ribbon tile quad" };
            mesh.vertices = new[] { new Vector3(-.5f,-.5f,0), new Vector3(.5f,-.5f,0), new Vector3(.5f,.5f,0), new Vector3(-.5f,.5f,0) };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 }; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, Folder + "/RibbonTile.asset");
            var root = new GameObject("StadiumRibbon");
            try
            {
                var tiles = new Renderer[256];
                for (int i = 0; i < tiles.Length; i++)
                {
                    // Match the existing 32-section/256-sample bowl wall exactly.
                    Vector3 a = RibbonPoint(i / 256f), b = RibbonPoint((i + 1f) / 256f);
                    Vector3 tangent = (b - a).normalized;
                    Vector3 inward = Vector3.Cross(Vector3.up, tangent);
                    var tile = new GameObject($"Section_{i / 8 + 1:00}_Tile_{i % 8 + 1:00}");
                    tile.transform.SetParent(root.transform, false);
                    tile.transform.localPosition = (a + b) * .5f + inward * .012f;
                    // Quad front is -Z. Keep local +X tangent, local +Y up.
                    tile.transform.localRotation = Quaternion.LookRotation(-inward, Vector3.up);
                    tile.transform.localScale = new Vector3(Vector3.Distance(a,b) * .995f, .12f, 1);
                    tile.AddComponent<MeshFilter>().sharedMesh = mesh;
                    var renderer = tile.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
                    renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
                    renderer.lightProbeUsage = LightProbeUsage.Off; renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                    GameObjectUtility.SetStaticEditorFlags(tile, 0); renderer.enabled = false; tiles[i] = renderer;
                }
                root.AddComponent<StadiumRibbonController>().Configure(tiles);
                PrefabUtility.SaveAsPrefabAsset(root, RibbonPath);
            }
            finally { Object.DestroyImmediate(root); }
            var stadium = PrefabUtility.LoadPrefabContents(StadiumPath);
            try
            {
                if (stadium.GetComponentInChildren<StadiumRibbonController>(true) != null)
                    throw new InvalidOperationException("Stadium already contains a ribbon.");
                PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RibbonPath), stadium.transform);
                PrefabUtility.SaveAsPrefabAsset(stadium, StadiumPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(stadium); }
        }

        public static Vector3 RibbonPoint(float t)
        {
            // Same constants as RoomStadiumEditorTools: lower tier depth = 14 * .24.
            const float radius = 4.5f + 14 * .24f;
            // Top of the strip meets the upper concrete edge; bottom clears lower-tier heads.
            const float height = .12f + 14 * .105f + .48f - .06f;
            float section = Mathf.Repeat(t,1) * 32;
            int side = Mathf.FloorToInt(section / 8); float within = section - side * 8;
            Vector2 p;
            if (within < 6)
            {
                float f = within / 6;
                p = side switch {
                    0 => new Vector2(Mathf.Lerp(-13,13,f),11.3f+radius),
                    1 => new Vector2(13+radius,Mathf.Lerp(11.3f,-11.3f,f)),
                    2 => new Vector2(Mathf.Lerp(13,-13,f),-11.3f-radius),
                    _ => new Vector2(-13-radius,Mathf.Lerp(-11.3f,11.3f,f))
                };
            }
            else
            {
                float angle = (90 - side * 90 - (within - 6) * 45) * Mathf.Deg2Rad;
                Vector2 center = side switch {
                    0 => new Vector2(13,11.3f), 1 => new Vector2(13,-11.3f),
                    2 => new Vector2(-13,-11.3f), _ => new Vector2(-13,11.3f)
                };
                p = center + new Vector2(Mathf.Cos(angle),Mathf.Sin(angle)) * radius;
            }
            return new Vector3(p.x,height,p.y);
        }

        private static void AuthorSetupDropdown()
        {
            const string path = "Assets/Scenes/CreateNewGameScene.unity";
            var scene = SceneManager.GetSceneByPath(path); bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                var manager = Find<CreateNewGameManager>(scene);
                if (manager == null) throw new InvalidOperationException("Match setup manager not found.");
                if (manager.fixtureLightingDropdown == null)
                    manager.fixtureLightingDropdown = CloneDropdown(manager.weatherDropdown, "FixtureLightingDropdown");
                EditorUtility.SetDirty(manager); EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene,true); }
        }

        private static TMP_Dropdown CloneDropdown(TMP_Dropdown source, string name)
        {
            if (source == null) throw new InvalidOperationException("Existing weather dropdown is unassigned.");
            // This project's DropDownButton prefab has a labelled row around the actual dropdown.
            RectTransform sourceRow = (RectTransform)source.transform.parent;
            Transform container = sourceRow.parent;
            var existing = container.Find(name + "Row");
            if (existing != null) return existing.GetComponentInChildren<TMP_Dropdown>(true);
            var go = Object.Instantiate(sourceRow.gameObject, container, false); go.name = name + "Row";
            var dropdown = go.GetComponentInChildren<TMP_Dropdown>(true);
            dropdown.name = name;
            dropdown.onValueChanged = new TMP_Dropdown.DropdownEvent();
            dropdown.ClearOptions(); dropdown.AddOptions(FixtureLightingOptions.Labels); dropdown.SetValueWithoutNotify(0);
            foreach (var text in go.GetComponentsInChildren<TMP_Text>(true))
                if (text != dropdown.captionText && text != dropdown.itemText && text.text.IndexOf("Weather",StringComparison.OrdinalIgnoreCase) >= 0)
                    text.text = "Fixture lighting";
            var rect = (RectTransform)go.transform;
            rect.SetSiblingIndex(sourceRow.GetSiblingIndex() + 1);
            LayoutDropdownRow(source, dropdown);
            return dropdown;
        }

        public static void LayoutDropdownRow(TMP_Dropdown source, TMP_Dropdown dropdown)
        {
            var sourceRow = (RectTransform)source.transform.parent;
            var rect = (RectTransform)dropdown.transform.parent;
            Transform container = sourceRow.parent;
            // Existing menu fields use authored rows. Insert a row and move following siblings down.
            var layout = container.GetComponent<UnityEngine.UI.LayoutGroup>();
            if (layout is UnityEngine.UI.VerticalLayoutGroup vertical && vertical.enabled)
            {
                // The setup list has fixed-height rows inside a fixed panel. Use its spare
                // spacing for the extra row, keeping the neighbouring Player Controls panel clear.
                var rows = container.OfType<RectTransform>().Where(t => t.gameObject.activeSelf).ToArray();
                float totalHeight = rows.Sum(t => Mathf.Max(t.rect.height, UnityEngine.UI.LayoutUtility.GetPreferredHeight(t)));
                float available = ((RectTransform)container).rect.height - vertical.padding.vertical;
                if (rows.Length > 1 && available >= totalHeight)
                    vertical.spacing = Mathf.Min(vertical.spacing, (available - totalHeight) / (rows.Length - 1));
                EditorUtility.SetDirty(vertical);
            }
            if ((layout == null || !layout.enabled) && Mathf.Abs(rect.anchoredPosition.y - sourceRow.anchoredPosition.y) < 1)
            {
                float step = Mathf.Max(76, sourceRow.rect.height + 12);
                rect.anchoredPosition = sourceRow.anchoredPosition + Vector2.down * step;
                foreach (RectTransform sibling in container.OfType<RectTransform>())
                    if (sibling != rect && sibling != sourceRow && sibling.anchoredPosition.y < sourceRow.anchoredPosition.y - 1)
                        sibling.anchoredPosition += Vector2.down * step;
                var parent = container as RectTransform;
                if (parent != null) parent.sizeDelta += new Vector2(0,step);
                // Pause settings have a fixed card background and footer around the manually placed rows.
                if (dropdown.name == "EditFixtureLightingDropdown")
                    for (Transform t = container.parent; t != null; t = t.parent)
                        if (t.name == "WhiteBkg" || t.name == "MenuCard_PauseSettings")
                            ((RectTransform)t).sizeDelta += new Vector2(0,step);
            }
        }

        public static void FitPauseSettingsBackground(PauseMenuManager pause)
        {
            var card = (RectTransform)pause.editSettingsBackButton.transform.parent;
            var white = (RectTransform)card.Find("WhiteBkg");
            Undo.RecordObjects(new Object[] { card, white, pause.editSettingsBackButton.transform }, "Fit settings footer background");
            float height = Mathf.Max(card.rect.height, 1296f);
            card.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            const float footerHeight = 180f;
            white.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height - 60f - footerHeight);
            var back = (RectTransform)pause.editSettingsBackButton.transform;
            back.anchoredPosition = new Vector2(back.anchoredPosition.x, (footerHeight - back.rect.height) * .5f);
        }

        private static T Find<T>(Scene scene) where T : Component => scene.GetRootGameObjects()
            .SelectMany(go => go.GetComponentsInChildren<T>(true)).FirstOrDefault();

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/'); EnsureFolder(path.Substring(0,slash));
            AssetDatabase.CreateFolder(path.Substring(0,slash),path.Substring(slash+1));
        }
    }
}
#endif
