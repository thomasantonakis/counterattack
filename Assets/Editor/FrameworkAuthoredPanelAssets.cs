using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Explicit EditMode export of the production-authored resolution panels.
/// Tests never bootstrap Room or mutate its objects, layouts or live saves.</summary>
public static class FrameworkAuthoredPanelAssets
{
    public const string SourceScene = "Assets/Scenes/Room.unity";
    public const string PrefabPath = "Assets/Tests/Fixtures/RoomResolutionPanels.prefab";
    public const string HashPath = "Assets/Tests/Fixtures/RoomResolutionPanels.source.sha256.txt";

    public static string SourceHash()
    {
        using var hash = SHA256.Create();
        return BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(SourceScene))).Replace("-", "");
    }

    public static object Export()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Export test panels only in Edit Mode.");
        Scene original = SceneManager.GetActiveScene();
        bool originalDirty = original.isDirty;
        Scene preview = EditorSceneManager.OpenPreviewScene(SourceScene);
        Scene output = default;
        try
        {
            var panels = preview.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MonoBehaviour>(true))
                .Where(c => c is DuelResolutionPanelController || c is HeaderChallengePanelController
                    || c is StoppageActionsPanelController).ToArray();
            if (panels.Length != 3) throw new InvalidOperationException($"Expected three authored panel roots, found {panels.Length}.");
            var sourceCanvas = panels[0].GetComponentInParent<Canvas>(true);
            if (sourceCanvas == null || sourceCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                throw new InvalidOperationException("Expected Room's authored overlay canvas.");
            output = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(output);
            var root = new GameObject("Room authored resolution panels", typeof(RectTransform), typeof(Canvas));
            EditorUtility.CopySerialized(sourceCanvas, root.GetComponent<Canvas>());
            foreach (Type type in new[] { typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster) })
            {
                var source = sourceCanvas.GetComponent(type);
                if (source != null) EditorUtility.CopySerialized(source, root.AddComponent(type));
            }
            foreach (var source in panels)
            {
                var clone = Object.Instantiate(source.gameObject, root.transform, false);
                clone.name = source.gameObject.name;
                var stoppage = clone.GetComponent<StoppageActionsPanelController>();
                if (stoppage != null) stoppage.match = null; // Bound to the isolated match by each fixture.
            }
            // Refuse to serialize references back into the production scene.
            foreach (var component in root.GetComponentsInChildren<Component>(true).Where(c => c != null))
            {
                var property = new SerializedObject(component).GetIterator();
                while (property.Next(true))
                {
                    if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                    Object value = property.objectReferenceValue;
                    if (value == null || EditorUtility.IsPersistent(value)) continue;
                    Transform reference = value is Component c ? c.transform : value is GameObject g ? g.transform : null;
                    if (reference != null && !reference.IsChildOf(root.transform))
                        throw new InvalidOperationException($"External scene reference: {component.name}.{property.propertyPath}");
                }
            }
            if (!AssetDatabase.IsValidFolder("Assets/Tests/Fixtures")) AssetDatabase.CreateFolder("Assets/Tests", "Fixtures");
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            if (prefab == null) throw new InvalidOperationException("Authored test panel export failed.");
            File.WriteAllText(HashPath, SourceHash() + Environment.NewLine);
            AssetDatabase.ImportAsset(HashPath);
            return new { prefab = PrefabPath, panelRoots = panels.Select(p => p.gameObject.name).ToArray(),
                transforms = root.GetComponentsInChildren<Transform>(true).Length, sourceHash = SourceHash() };
        }
        finally
        {
            if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
            if (output.IsValid() && output.isLoaded) EditorSceneManager.CloseScene(output, true);
            EditorSceneManager.ClosePreviewScene(preview);
            if (original.isDirty != originalDirty)
                throw new InvalidOperationException("Export unexpectedly changed the original scene dirty state.");
        }
    }
}
