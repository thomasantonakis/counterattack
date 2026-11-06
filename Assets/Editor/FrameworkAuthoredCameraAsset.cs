using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class FrameworkAuthoredCameraAsset
{
    public const string PrefabPath = "Assets/Tests/Fixtures/RoomTestCamera.prefab";
    public static object Export()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Export test camera only in Edit Mode.");
        var original = SceneManager.GetActiveScene(); bool dirty = original.isDirty;
        var source = EditorSceneManager.OpenPreviewScene(FrameworkAuthoredPanelAssets.SourceScene);
        Scene output = default;
        try
        {
            var camera = source.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Camera>(true)).Single(c => c.CompareTag("MainCamera"));
            output = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(output);
            var root = new GameObject("Room test camera", typeof(Camera)); root.tag = "MainCamera";
            root.transform.SetPositionAndRotation(camera.transform.position, camera.transform.rotation);
            EditorUtility.CopySerialized(camera, root.GetComponent<Camera>());
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            if (prefab == null) throw new InvalidOperationException("Test camera export failed.");
            return new { prefab = PrefabPath, sourceHash = FrameworkAuthoredPanelAssets.SourceHash() };
        }
        finally
        {
            if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
            if (output.IsValid() && output.isLoaded) EditorSceneManager.CloseScene(output, true);
            EditorSceneManager.ClosePreviewScene(source);
            if (original.isDirty != dirty) throw new InvalidOperationException("Camera export changed the original scene dirty state.");
        }
    }
}
