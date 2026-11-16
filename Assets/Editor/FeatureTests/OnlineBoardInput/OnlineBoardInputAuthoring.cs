using System;
using CounterAttack.Online;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public static class OnlineBoardInputAuthoring
{
    [CliCommand("author_online_board_input","Author direct online pitch input and its inspectable instruction label")]
    public static string Prepare()
    {
        string previous=SceneManager.GetActiveScene().path;
        if(EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().isDirty || SceneManager.sceneCount!=1)throw new InvalidOperationException("Expected one clean stopped scene");
        try
        {
            var scene=EditorSceneManager.OpenScene(OnlineFullRoomAuthoring.ScenePath,OpenSceneMode.Single);
            var board=UnityEngine.Object.FindAnyObjectByType<OnlineGroundPassBoard>();
            if(board.boardInput!=null)return "Online pitch input already authored";
            board.boardInput=board.gameObject.AddComponent<OnlineBoardInput>();board.boardInput.board=board;
            var obj=new GameObject("Online pitch input instructions",typeof(RectTransform));obj.transform.SetParent(board.sharedPausePanel.transform.parent,false);
            var rect=(RectTransform)obj.transform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,0);rect.pivot=new Vector2(.5f,0);rect.sizeDelta=new Vector2(1250,55);rect.anchoredPosition=new Vector2(0,6);
            var text=obj.AddComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=19;text.color=Color.white;text.raycastTarget=false;text.alignment=TextAnchor.LowerCenter;
            text.text="Click a player, then a legal hex. Click the target again or Enter to confirm.\nP pass | L long | C high | F first time | M move | S shoot | R roll | Enter confirm";
            EditorUtility.SetDirty(board);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return "Authored direct pitch input and key guide";
        }
        finally{if(!string.IsNullOrEmpty(previous))EditorSceneManager.OpenScene(previous,OpenSceneMode.Single);}
    }
}
