#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CounterAttack.Editor
{
    public static class RoomBoardEditorTools
    {
        private const string RoomScenePath = "Assets/Scenes/Room.unity";
        private const string MaterialsFolder = "Assets/Materials";
        private const string GeneratedMaterialsFolder = "Assets/Materials/Generated";
        private const string LineMaterialPath = GeneratedMaterialsFolder + "/PitchLine.mat";
        private const string NetMaterialPath = GeneratedMaterialsFolder + "/GoalNet.mat";
        private const string BlockerMaterialPath = GeneratedMaterialsFolder + "/OutOfBoundsBlocker.mat";
        private const string DotSpritePath = "Assets/Resources/circle.png";

        [MenuItem("CounterAttack/Room/Rebuild Pitch Board")]
        public static void RebuildPitchBoard()
        {
            var scene = EditorSceneManager.OpenScene(RoomScenePath, OpenSceneMode.Single);
            HexGrid hexGrid = Object.FindFirstObjectByType<HexGrid>();
            if (hexGrid == null)
            {
                throw new System.InvalidOperationException("Room scene does not contain a HexGrid.");
            }

            PitchLines pitchBoardVisuals = Object.FindFirstObjectByType<PitchLines>();
            if (pitchBoardVisuals == null)
            {
                GameObject boardRoot = new GameObject("PitchBoardVisuals");
                pitchBoardVisuals = boardRoot.AddComponent<PitchLines>();
            }

            EnsureFolder(MaterialsFolder);
            EnsureFolder(GeneratedMaterialsFolder);

            Material lineMaterial = LoadOrCreateColorMaterial(LineMaterialPath, Color.white);
            Material netMaterial = LoadOrCreateColorMaterial(NetMaterialPath, new Color(0.92f, 0.92f, 0.92f, 1f));
            Material blockerMaterial = LoadOrCreateColorMaterial(BlockerMaterialPath, new Color(0.11f, 0.35f, 0.85f, 1f));
            Sprite dotSprite = AssetDatabase.LoadAssetAtPath<Sprite>(DotSpritePath);

            if (dotSprite == null)
            {
                throw new System.InvalidOperationException($"Could not load the pitch dot sprite at {DotSpritePath}.");
            }

            pitchBoardVisuals.hexGrid = hexGrid;
            pitchBoardVisuals.ConfigureGeneratedAssets(lineMaterial, netMaterial, blockerMaterial, dotSprite);
            pitchBoardVisuals.RebuildSceneVisuals();

            EditorUtility.SetDirty(pitchBoardVisuals);
            EditorUtility.SetDirty(hexGrid);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("Room pitch board visuals rebuilt and saved into Room.scene.");
        }

        [MenuItem("CounterAttack/Room/Rebuild Shooting Path Assets")]
        public static void RebuildShootingPathAssets()
        {
            EditorSceneManager.OpenScene(RoomScenePath, OpenSceneMode.Single);
            HexGrid hexGrid = Object.FindFirstObjectByType<HexGrid>();
            if (hexGrid == null)
            {
                throw new System.InvalidOperationException("Room scene does not contain a HexGrid.");
            }

            hexGrid.RebuildShootingPathAssetsInEditor();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Room shooting and heading path assets rebuilt.");
        }

        [MenuItem("CounterAttack/Room/Rebuild Pitch Board And Path Assets")]
        public static void RebuildPitchBoardAndPathAssets()
        {
            RebuildPitchBoard();
            RebuildShootingPathAssets();
        }

        private static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
            {
                return;
            }

            string[] parts = assetPath.Split('/');
            string currentPath = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string nextPath = $"{currentPath}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, parts[i]);
                }
                currentPath = nextPath;
            }
        }

        private static Material LoadOrCreateColorMaterial(string assetPath, Color color)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            Shader shader = Shader.Find("Unlit/Color");
            if (shader == null)
            {
                throw new System.InvalidOperationException("Could not find the Unlit/Color shader.");
            }

            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, assetPath);
            }

            material.shader = shader;
            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
#endif
