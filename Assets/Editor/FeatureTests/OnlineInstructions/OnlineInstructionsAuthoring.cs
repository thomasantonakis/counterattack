using System;
using System.Linq;
using CounterAttack.Online;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class OnlineInstructionsAuthoring
{
    [CliCommand("bind_online_instructions","Bind the existing authored instruction panel without enabling monitor updates")]
    public static string Bind()
    {
        var original=SceneManager.GetActiveScene();string previous=original.path;
        if(EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.sceneCount!=1 || original.isDirty)throw new InvalidOperationException("Expected one clean stopped scene");
        try
        {
            var scene=EditorSceneManager.OpenScene(OnlineFullRoomAuthoring.ScenePath,OpenSceneMode.Single);
            var board=UnityEngine.Object.FindAnyObjectByType<OnlineGroundPassBoard>();
            var monitor=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<GameDebugMonitor>(true)).Single();
            if(monitor.enabled || monitor.instructionText==null)throw new InvalidOperationException("Expected the disabled authored monitor and its instruction text");
            board.instructionMonitor=monitor;EditorUtility.SetDirty(board);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            return "Bound existing instruction panel; monitor remains disabled";
        }
        finally{if(!string.IsNullOrEmpty(previous))EditorSceneManager.OpenScene(previous,OpenSceneMode.Single);}
    }
}
