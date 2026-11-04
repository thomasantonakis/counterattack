#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace CounterAttack.Editor
{
    /// <summary>Authors persistent stadium geometry. No runtime construction or camera-facing images.</summary>
    public static class RoomStadiumEditorTools
    {
        private const string Folder = "Assets/Environment/RoomStadium";
        private const string PrefabPath = Folder + "/RoomStadium.prefab";
        private const int Rows = 14;
        private const int Sections = 32;
        private const int RingSegments = 256;
        private const float RowDepth = 0.24f;
        private const float RowRise = 0.105f;
        private const float CoreX = 13f;
        private const float CoreZ = 11.3f;
        private const float Radius = 4.5f;
        private const float StadiumScale = 0.85f;
        private static readonly Color Concrete = new Color(0.37f, 0.39f, 0.41f);
        private static readonly Color Riser = new Color(0.24f, 0.26f, 0.28f);
        private static readonly Color SeatBlue = new Color(0.045f, 0.12f, 0.23f);
        private static readonly Color Metal = new Color(0.24f, 0.29f, 0.33f);
        private static readonly Color[] Clothes = {
            new Color(.035f,.055f,.08f), new Color(.10f,.15f,.22f),
            new Color(.16f,.24f,.35f), new Color(.32f,.36f,.40f),
            new Color(.62f,.64f,.62f), new Color(.74f,.73f,.68f),
            new Color(.16f,.18f,.13f), new Color(.32f,.13f,.10f),
            new Color(.14f,.25f,.43f), new Color(.40f,.30f,.17f)
        };
        private static readonly Color[] Skin = {
            new Color(.67f,.46f,.32f), new Color(.84f,.65f,.48f),
            new Color(.40f,.26f,.18f), new Color(.57f,.36f,.24f)
        };

        [MenuItem("CounterAttack/Room/Realign Stadium Aisles")]
        public static void RealignAisles()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Realign stadium aisles in Edit Mode.");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/Stadium.mat");
            if (material == null || AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
                throw new InvalidOperationException("Build the stadium before realigning its aisles.");

            // Update the existing mesh assets in place. Preserve the scene, prefab
            // overrides, other authoring work, and all mesh GUID references.
            var temporary = new GameObject("Temporary Stadium Aisle Authoring");
            temporary.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                var metadata = new List<StadiumCrowdSection>();
                BuildTier(Container(temporary.transform, "Lower Tier - 14 Rows"), 0f, .12f,
                    material, 1024, out int lowerCount, metadata);
                BuildTier(Container(temporary.transform, "Upper Tier - 14 Rows"),
                    Rows * RowDepth + .85f, .12f + Rows * RowRise + .48f,
                    material, 2048, out int upperCount, metadata);
                foreach (var filter in temporary.GetComponentsInChildren<MeshFilter>())
                    AssetDatabase.SaveAssetIfDirty(filter.sharedMesh);
                var contents = PrefabUtility.LoadPrefabContents(PrefabPath);
                try
                {
                    ConfigureController(contents, metadata);
                    PrefabUtility.SaveAsPrefabAsset(contents, PrefabPath);
                }
                finally { PrefabUtility.UnloadPrefabContents(contents); }
                Debug.Log($"Stadium aisles realigned: {lowerCount + upperCount:N0} spectators. Straight aisles perpendicular to pitch edges; corner aisles radial. Scene changes preserved.");
            }
            finally
            {
                Object.DestroyImmediate(temporary);
            }
        }

        [MenuItem("CounterAttack/Room/Build Stadium")]
        public static void Build()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != "Assets/Scenes/Room.unity")
                throw new InvalidOperationException("Open Room in Edit Mode before building its stadium.");
            Shader shader = Shader.Find("CounterAttack/Stadium Vertex Color");
            if (shader == null) throw new InvalidOperationException("Stadium shader has not imported.");
            EnsureFolder(Folder + "/Meshes");
            var existing = GameObject.Find("StadiumCrowd");
            if (existing != null)
            {
                if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(existing) != PrefabPath)
                    throw new InvalidOperationException("StadiumCrowd already exists and is not the generated stadium prefab.");
                Undo.DestroyObjectImmediate(existing);
            }

            Material shared = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/Stadium.mat");
            if (shared == null)
            {
                shared = new Material(shader);
                AssetDatabase.CreateAsset(shared, Folder + "/Stadium.mat");
            }
            shared.shader = shader;
            shared.color = Color.white;
            EditorUtility.SetDirty(shared);
            var root = new GameObject("StadiumCrowd");
            root.layer = 2;
            Undo.RegisterCreatedObjectUndo(root, "Build Room stadium");
            var architecture = Container(root.transform, "Architecture");
            var lower = Container(root.transform, "Lower Tier - 14 Rows");
            var upper = Container(root.transform, "Upper Tier - 14 Rows");
            var trim = new Geometry();
            var metadata = new List<StadiumCrowdSection>();

            // Every tier uses exactly the same closed perimeter, row depth and rise.
            BuildTier(lower, 0f, .12f, shared, 1024, out int lowerCount, metadata);
            float upperDepth = Rows * RowDepth + .85f;
            float upperHeight = .12f + Rows * RowRise + .48f;
            BuildTier(upper, upperDepth, upperHeight, shared, 2048, out int upperCount, metadata);
            float outerDepth = upperDepth + Rows * RowDepth;
            float outerHeight = upperHeight + Rows * RowRise;

            var ring = new Geometry();
            RingSurface(ring, Rows * RowDepth, upperDepth, .12f + Rows * RowRise, Concrete);
            RingWall(ring, Rows * RowDepth, .12f + Rows * RowRise, upperHeight, new Color(.13f,.16f,.19f));
            RingWall(ring, outerDepth, .05f, outerHeight + .32f, Riser);
            RingSurface(ring, outerDepth, outerDepth + .25f, outerHeight + .32f, Metal);
            RingWall(ring, -.08f, .06f, .30f, new Color(.13f,.17f,.21f));
            SaveGeometry("Concourse and Bowl Walls", ring, architecture, shared);

            // Rings of handrails and regularly spaced upright supports.
            for (int i = 0; i < RingSegments; i++)
            {
                float t = i / (float)RingSegments, next = (i + 1f) / RingSegments;
                trim.Beam(Point(t, -.06f, .42f), Point(next, -.06f, .42f), .018f, Metal);
                trim.Beam(Point(t, Rows * RowDepth + .03f, upperHeight + .10f),
                    Point(next, Rows * RowDepth + .03f, upperHeight + .10f), .018f, Metal);
                if (i % 2 == 0)
                {
                    trim.Beam(Point(t,-.06f,.12f), Point(t,-.06f,.42f), .017f, Metal);
                    trim.Beam(Point(t,Rows*RowDepth+.03f,upperHeight-.35f),
                        Point(t,Rows*RowDepth+.03f,upperHeight+.10f), .017f, Metal);
                }
            }
            SaveGeometry("Perimeter and Concourse Railings", trim, architecture, shared);

            var apron = new Geometry();
            // Four disjoint rectangles leave pitch geometry and red action labels exposed.
            Color asphalt = new Color(.16f,.18f,.20f);
            apron.Box(new Vector3(0,-.025f,14.1f), new Vector3(27.5f,.14f,5.9f), asphalt);
            apron.Box(new Vector3(0,-.025f,-14.1f), new Vector3(27.5f,.14f,5.9f), asphalt);
            apron.Box(new Vector3(-15.7f,-.025f,0), new Vector3(3.9f,.14f,34.1f), asphalt);
            apron.Box(new Vector3(15.7f,-.025f,0), new Vector3(3.9f,.14f,34.1f), asphalt);
            var apronRenderer = SaveGeometry("Pitch Perimeter Apron", apron, architecture, shared);
            // Tighten the bowl while preserving the apron opening around the pitch.
            root.transform.localScale = Vector3.one * StadiumScale;
            apronRenderer.transform.localScale = Vector3.one / StadiumScale;

            // Recolour only the existing blocker material; keep colliders/scripts untouched.
            Material blocker = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Generated/OutOfBoundsBlocker.mat");
            if (blocker != null)
            {
                Undo.RecordObject(blocker, "Stadium apron colour");
                blocker.color = asphalt;
                EditorUtility.SetDirty(blocker);
            }
            ConfigureController(root, metadata);
            PrefabUtility.SaveAsPrefabAssetAndConnect(root, PrefabPath, InteractionMode.AutomatedAction);
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"Room stadium authored: {lowerCount + upperCount:N0} seated spectators, two continuous {Rows}-row tiers, {Sections} sections per tier. All geometry saved in {PrefabPath}.");
        }

        private static void ConfigureController(GameObject root, List<StadiumCrowdSection> metadata)
        {
            foreach (var section in metadata)
            {
                string seatPath = section.seats.transform.parent.name + "/" + section.seats.name;
                string crowdPath = section.spectators.transform.parent.name + "/" + section.spectators.name;
                section.seats = root.transform.Find(seatPath).GetComponent<Renderer>();
                section.spectators = root.transform.Find(crowdPath).GetComponent<Renderer>();
            }
            var controller = root.GetComponent<StadiumCrowdController>();
            if (controller == null) controller = root.AddComponent<StadiumCrowdController>();
            controller.Configure(metadata.ToArray());
            EditorUtility.SetDirty(controller);
        }

        private static void BuildTier(Transform parent, float depth, float height, Material mat, int seed, out int population,
            List<StadiumCrowdSection> metadata = null)
        {
            var structure = new Geometry();
            for (int row = 0; row < Rows; row++)
            {
                float d = depth + row * RowDepth, h = height + row * RowRise;
                RingSurface(structure, d, d + RowDepth, h, Concrete);
                RingWall(structure, d + RowDepth, h, h + RowRise, Riser);
            }
            SaveGeometry(parent.name + " Concrete", structure, parent, mat);
            population = 0;
            var rng = new System.Random(seed);
            for (int section = 0; section < Sections; section++)
            {
                var people = new Geometry();
                var seats = new Geometry();
                var tops = new List<Color32>();
                var trousers = new List<Color32>();
                seats.PartData = new Vector2(0, 3);
                for (int row = 0; row < Rows; row++)
                {
                    float d = depth + (row + .55f) * RowDepth;
                    float h = height + row * RowRise;
                    // Six straight sections and two corner sections per side. Their
                    // boundaries stay at fixed coordinates/angles through both tiers.
                    float sectionLength = SectionLength(section, d);
                    const float aisleWidth = .38f;
                    float usableLength = sectionLength - aisleWidth;
                    int slots = Mathf.Max(1, Mathf.FloorToInt(usableLength / .17f));
                    for (int slot = 0; slot < slots; slot++)
                    {
                        // Equal half-aisles on each boundary, identical on every
                        // straight row, rather than dropping unequal end seats.
                        float fraction = (aisleWidth * .5f + (slot + .5f) * usableLength / slots) / sectionLength;
                        float t = (section + fraction) / Sections;
                        Vector3 p = Point(t,d,h);
                        Vector3 inward = -Outward(t,d);
                        Quaternion q = Quaternion.LookRotation(inward,Vector3.up);
                        seats.Box(p + Vector3.up * .10f, new Vector3(.12f,.025f,.12f), SeatBlue, q);
                        seats.Box(p + Vector3.up * .18f - inward*.052f,new Vector3(.12f,.14f,.025f),SeatBlue,q);
                        if (rng.NextDouble() > .86) continue;
                        Spectator(people, p, q, rng, tops, trousers);
                        population++;
                    }
                }
                var seatRenderer = SaveGeometry($"Seats {section+1:00}", seats, parent, mat);
                var crowdRenderer = SaveGeometry($"Spectators {section+1:00}", people, parent, mat, false);
                metadata?.Add(new StadiumCrowdSection {
                    section = section, upperTier = depth > 0,
                    seats = seatRenderer, spectators = crowdRenderer,
                    neutralTops = tops.ToArray(), neutralTrousers = trousers.ToArray()
                });
            }
        }

        private static void Spectator(Geometry mesh, Vector3 p, Quaternion q, System.Random rng,
            List<Color32> tops, List<Color32> pants)
        {
            float scale = .90f + (float)rng.NextDouble() * .18f;
            Color shirt = Clothes[rng.Next(Clothes.Length)];
            Color skin = Skin[rng.Next(Skin.Length)];
            Color trousers = Clothes[rng.Next(4)];
            int id = tops.Count;
            tops.Add(shirt); pants.Add(trousers);
            // A seated body is about .35 units tall; a standing adult would be .45–.47.
            Vector3 P(float x,float y,float z) => p + q * (new Vector3(x,y,z) * scale);
            mesh.PartData = new Vector2(id, 1);
            mesh.Ellipsoid(P(0,.225f,0), new Vector3(.061f,.092f,.039f)*scale,shirt,q);
            mesh.PartData = new Vector2(id, 0);
            mesh.Ellipsoid(P(0,.335f,.006f),new Vector3(.028f,.035f,.029f)*scale,skin,q);
            Color hair = rng.NextDouble() < .20 ? new Color(.43f,.40f,.34f) : new Color(.055f,.043f,.035f);
            mesh.Ellipsoid(P(0,.355f,.002f),new Vector3(.029f,.019f,.029f)*scale,hair,q);
            foreach (float side in new[] {-1f,1f})
            {
                mesh.PartData = new Vector2(id, 2);
                mesh.Beam(P(side*.03f,.145f,.015f),P(side*.035f,.13f,.095f),.042f*scale,trousers);
                mesh.Beam(P(side*.035f,.13f,.095f),P(side*.035f,.035f,.11f),.032f*scale,trousers);
                mesh.PartData = new Vector2(id, 0);
                mesh.Box(P(side*.035f,.022f,.125f),new Vector3(.035f,.025f,.055f)*scale,new Color(.035f,.035f,.037f),q);
                mesh.PartData = new Vector2(id, 1);
                mesh.Beam(P(side*.052f,.265f,.002f),P(side*.068f,.19f,.045f),.026f*scale,shirt);
                mesh.Beam(P(side*.068f,.19f,.045f),P(side*.037f,.165f,.082f),.023f*scale,shirt);
            }
        }

        // Anchor each straight aisle to a fixed X/Z coordinate, not a fraction of
        // each row's growing circumference (which made the old aisles diagonal).
        // Rounded corners use fixed radial angles and share their end normals
        // with the adjoining straight stands.
        private static Vector3 Point(float t,float depth,float y)
        {
            float r = Radius + depth;
            float section = Mathf.Repeat(t, 1f) * Sections;
            int side = Mathf.FloorToInt(section / 8f);
            float withinSide = section - side * 8f;
            Vector2 result;
            if (withinSide < 6f)
            {
                float fraction = withinSide / 6f;
                result = side switch {
                    0 => new Vector2(Mathf.Lerp(-CoreX,CoreX,fraction),CoreZ+r),
                    1 => new Vector2(CoreX+r,Mathf.Lerp(CoreZ,-CoreZ,fraction)),
                    2 => new Vector2(Mathf.Lerp(CoreX,-CoreX,fraction),-CoreZ-r),
                    _ => new Vector2(-CoreX-r,Mathf.Lerp(-CoreZ,CoreZ,fraction))
                };
            }
            else
            {
                float angle = (90f - side * 90f - (withinSide - 6f) * 45f) * Mathf.Deg2Rad;
                Vector2 center = side switch {
                    0=>new Vector2(CoreX,CoreZ),1=>new Vector2(CoreX,-CoreZ),
                    2=>new Vector2(-CoreX,-CoreZ),_=>new Vector2(-CoreX,CoreZ)
                };
                result = center + new Vector2(Mathf.Cos(angle),Mathf.Sin(angle)) * r;
            }
            return new Vector3(result.x,y,result.y);
        }

        private static float SectionLength(int section, float depth)
        {
            int side = section / 8;
            return section % 8 < 6
                ? (side % 2 == 0 ? CoreX * 2f : CoreZ * 2f) / 6f
                : (Radius + depth) * Mathf.PI * .25f;
        }

        private static Vector3 Outward(float t,float depth)
        {
            Vector3 p=Point(t,depth,0);
            return new Vector3(p.x-Mathf.Clamp(p.x,-CoreX,CoreX),0,p.z-Mathf.Clamp(p.z,-CoreZ,CoreZ)).normalized;
        }

        private static void RingSurface(Geometry mesh,float a,float b,float y,Color color)
        {
            for(int i=0;i<RingSegments;i++)
            {
                float t=i/(float)RingSegments, n=(i+1f)/RingSegments;
                mesh.Quad(Point(t,a,y),Point(t,b,y),Point(n,b,y),Point(n,a,y),color);
            }
        }
        private static void RingWall(Geometry mesh,float d,float a,float b,Color color)
        {
            for(int i=0;i<RingSegments;i++)
            {
                float t=i/(float)RingSegments,n=(i+1f)/RingSegments;
                mesh.Quad(Point(t,d,a),Point(n,d,a),Point(n,d,b),Point(t,d,b),color);
                mesh.Quad(Point(t,d,b),Point(n,d,b),Point(n,d,a),Point(t,d,a),color);
            }
        }
        private static Transform Container(Transform parent,string name)
        {
            var go=new GameObject(name); go.layer=2; go.transform.SetParent(parent,false); return go.transform;
        }
        private static MeshRenderer SaveGeometry(string name,Geometry data,Transform parent,Material material,bool shadows=true)
        {
            string key=parent.name.Replace(" ","_")+"_"+name.Replace(" ","_");
            string path=Folder+"/Meshes/"+key+".asset";
            Mesh created=data.ToMesh(key);
            Mesh asset=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(asset==null) { AssetDatabase.CreateAsset(created,path); asset=created; }
            else
            {
                // CopySerialized updates CPU serialization but can leave the live
                // native vertex/index streams stale. Use the Mesh API so existing
                // prefab instances immediately render new geometry and UV roles.
                asset.Clear();
                asset.name = key;
                asset.indexFormat = created.indexFormat;
                asset.vertices = created.vertices;
                asset.normals = created.normals;
                asset.colors32 = created.colors32;
                asset.uv = created.uv;
                asset.triangles = created.triangles;
                asset.bounds = created.bounds;
                asset.UploadMeshData(false);
                Object.DestroyImmediate(created);
                EditorUtility.SetDirty(asset);
            }
            var go=Container(parent,name).gameObject;
            go.AddComponent<MeshFilter>().sharedMesh=asset;
            var renderer=go.AddComponent<MeshRenderer>(); renderer.sharedMaterial=material;
            renderer.shadowCastingMode=shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            renderer.receiveShadows=true;
            return renderer;
        }
        private static void EnsureFolder(string path)
        {
            if(AssetDatabase.IsValidFolder(path)) return;
            int split=path.LastIndexOf('/'); string parent=path.Substring(0,split);
            EnsureFolder(parent); AssetDatabase.CreateFolder(parent,path.Substring(split+1));
        }

        private sealed class Geometry
        {
            private readonly List<Vector3> vertices=new();
            private readonly List<int> triangles=new();
            private readonly List<Color32> colors=new();
            private readonly List<Vector2> partData=new();
            public Vector2 PartData { get; set; }
            public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Color color)
            {
                int i=vertices.Count; vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
                for(int j=0;j<4;j++) { colors.Add(color); partData.Add(PartData); }
                triangles.AddRange(new[]{i,i+1,i+2,i,i+2,i+3});
            }
            public void Box(Vector3 p,Vector3 size,Color color,Quaternion? rotation=null)
            {
                Quaternion q=rotation??Quaternion.identity; Vector3 h=size*.5f;
                Vector3 V(float x,float y,float z)=>p+q*Vector3.Scale(h,new Vector3(x,y,z));
                int start=vertices.Count;
                vertices.AddRange(new[]{V(-1,-1,-1),V(1,-1,-1),V(1,-1,1),V(-1,-1,1),
                    V(-1,1,-1),V(1,1,-1),V(1,1,1),V(-1,1,1)});
                for(int j=0;j<8;j++) { colors.Add(color); partData.Add(PartData); }
                int[] faces={4,7,6,4,6,5,3,0,1,3,1,2,0,4,5,0,5,1,2,6,7,2,7,3,3,7,4,3,4,0,1,5,6,1,6,2};
                foreach(int i in faces) triangles.Add(start+i);
            }
            public void Beam(Vector3 a,Vector3 b,float width,Color color)
            {
                Vector3 d=b-a;
                Box((a+b)*.5f,new Vector3(width,d.magnitude,width),color,Quaternion.FromToRotation(Vector3.up,d));
            }
            public void Ellipsoid(Vector3 p,Vector3 scale,Color color,Quaternion rotation)
            {
                const int slices=6, rings=3;
                Vector3 V(int x,int y)
                {
                    float a=x*Mathf.PI*2/slices,b=-Mathf.PI*.5f+y*Mathf.PI/rings;
                    return p+rotation*Vector3.Scale(scale,new Vector3(Mathf.Cos(a)*Mathf.Cos(b),Mathf.Sin(b),Mathf.Sin(a)*Mathf.Cos(b)));
                }
                int start=vertices.Count;
                for(int y=0;y<=rings;y++) for(int x=0;x<=slices;x++)
                { vertices.Add(V(x,y)); colors.Add(color); partData.Add(PartData); }
                for(int y=0;y<rings;y++) for(int x=0;x<slices;x++)
                {
                    int a=start+y*(slices+1)+x,b=a+slices+1;
                    triangles.AddRange(new[]{a,b,b+1,a,b+1,a+1});
                }
            }
            public Mesh ToMesh(string name)
            {
                var mesh=new Mesh {name=name,indexFormat=vertices.Count>65535 ? IndexFormat.UInt32:IndexFormat.UInt16};
                mesh.SetVertices(vertices); mesh.SetColors(colors); mesh.SetUVs(0,partData); mesh.SetTriangles(triangles,0);
                mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
            }
        }
    }
}
#endif
