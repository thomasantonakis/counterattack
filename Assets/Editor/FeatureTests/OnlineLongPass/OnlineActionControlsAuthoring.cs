using System;
using CounterAttack.Online;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class OnlineActionControlsAuthoring
{
    [CliCommand("author_online_action_choices","Author semantic action controls in owned proof scenes and prefab")]
    public static string Prepare()
    {
        var previous=SceneManager.GetActiveScene();
        if(EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.sceneCount!=1 || previous.isDirty)
            throw new InvalidOperationException("Expected one clean stopped scene");
        string path=previous.path;
        try
        {
            foreach(var scenePath in new[]{OnlineGroundPassSceneAuthoring.ScenePath,OnlineRoomProofAuthoring.ScenePath})
            {
                var scene=EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Single);
                var controls=UnityEngine.Object.FindAnyObjectByType<OnlineGroundPassBoard>().controls;
                if(controls.legalCommands==null)
                {
                    var obj=DefaultControls.CreateDropdown(new DefaultControls.Resources());obj.name="Authoritative action choices";
                    obj.transform.SetParent(controls.transform,false);
                    var rect=obj.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.zero;
                    rect.anchoredPosition=new Vector2(0,120);rect.sizeDelta=new Vector2(455,28);
                    foreach(var text in obj.GetComponentsInChildren<Text>(true)) text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    controls.legalCommands=obj.GetComponent<Dropdown>();controls.legalCommands.ClearOptions();
                    var button=DefaultControls.CreateButton(new DefaultControls.Resources());button.name="Execute selected action";
                    button.transform.SetParent(controls.transform,false);
                    rect=button.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.zero;
                    rect.anchoredPosition=new Vector2(460,120);rect.sizeDelta=new Vector2(110,28);
                    var label=button.GetComponentInChildren<Text>();label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");label.text="Execute";
                    controls.commandButton=button.GetComponent<Button>();
                    EditorSceneManager.MarkSceneDirty(scene);
                    if(!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save action controls");
                }
                if(scenePath==OnlineGroundPassSceneAuthoring.ScenePath)
                    PrefabUtility.SaveAsPrefabAsset(controls.gameObject,"Assets/Settings/OnlineGroundPassProof/ManualControls.prefab");
            }
            return "Authored action choices in both proof scenes";
        }
        finally{EditorSceneManager.OpenScene(path,OpenSceneMode.Single);}
    }
}
