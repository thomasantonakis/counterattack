using System;
using System.IO;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class EOSStandaloneProofAuthoring
{
    public const string ScenePath = "Assets/Scenes/EOSStandaloneProof.unity";

    [CliCommand("prepare_eos_standalone_proof", "Author the isolated EOS proof scene and restore the current scene")]
    public static string Prepare()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            throw new InvalidOperationException("Wait for stopped Play Mode and clean compilation");
        if (SceneManager.sceneCount != 1)
            throw new InvalidOperationException("Expected one clean saved scene before authoring");
        var previous = SceneManager.GetActiveScene();
        if (previous.isDirty || string.IsNullOrEmpty(previous.path))
            throw new InvalidOperationException("Save the current scene before authoring");
        if (File.Exists(ScenePath)) return ScenePath;
        var previousPath = previous.path;
        try
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("EOS Standalone Proof", typeof(EOSStandaloneProof));
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException("Could not save the proof scene");
        }
        finally { EditorSceneManager.OpenScene(previousPath, OpenSceneMode.Single); }
        return ScenePath;
    }
}
