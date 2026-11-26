using System;
using CounterAttack.Online;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public static class OnlineRoomMenuAuthoring
{
    [CliCommand("scale_online_room_menu","Scale the authored online controls to fit desktop and smaller windows")]
    public static string Scale()
    {
        string previous=SceneManager.GetActiveScene().path;
        if(EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().isDirty || SceneManager.sceneCount!=1)throw new InvalidOperationException("Expected one clean stopped scene");
        try
        {
            var scene=EditorSceneManager.OpenScene(OnlineFullRoomAuthoring.ScenePath,OpenSceneMode.Single);
            var board=UnityEngine.Object.FindAnyObjectByType<OnlineGroundPassBoard>();var scaler=board.controls.GetComponentInParent<CanvasScaler>();
            scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            EditorUtility.SetDirty(scaler);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return "Scaled online controls around a 1920x1080 reference";
        }
        finally{if(!string.IsNullOrEmpty(previous))EditorSceneManager.OpenScene(previous,OpenSceneMode.Single);}
    }
    [CliCommand("author_online_room_menu","Author online pause, settings summary and safe leave controls in the owned full Room")]
    public static string Prepare()
    {
        var original=SceneManager.GetActiveScene();string previous=original.path;
        if(EditorApplication.isPlayingOrWillChangePlaymode || original.isDirty || SceneManager.sceneCount!=1)throw new InvalidOperationException("Expected one clean stopped scene");
        try
        {
            var scene=EditorSceneManager.OpenScene(OnlineFullRoomAuthoring.ScenePath,OpenSceneMode.Single);
            var board=UnityEngine.Object.FindAnyObjectByType<OnlineGroundPassBoard>();if(board.menu!=null)return "Online match menu already authored";
            var canvas=board.controls.GetComponentInParent<Canvas>();
            var owner=new GameObject("Online match menu",typeof(RectTransform),typeof(OnlineRoomMenu));owner.transform.SetParent(canvas.transform,false);
            var ownerRect=(RectTransform)owner.transform;ownerRect.anchorMin=Vector2.zero;ownerRect.anchorMax=Vector2.one;ownerRect.offsetMin=ownerRect.offsetMax=Vector2.zero;
            var menu=owner.GetComponent<OnlineRoomMenu>();menu.board=board;board.menu=menu;
            GameObject Box(string name,Transform parent,Vector2 size,Vector2 position)
            {
                var obj=new GameObject(name,typeof(RectTransform));obj.transform.SetParent(parent,false);var rect=(RectTransform)obj.transform;
                rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);rect.pivot=new Vector2(.5f,1);rect.sizeDelta=size;rect.anchoredPosition=position;return obj;
            }
            Text Label(string name,Transform parent,string value,Vector2 size,Vector2 position)
            {
                var obj=Box(name,parent,size,position);var text=obj.AddComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=20;text.color=Color.white;text.raycastTarget=false;text.text=value;text.alignment=TextAnchor.UpperCenter;return text;
            }
            Button Button(string name,Transform parent,string value,Vector2 size,Vector2 position)
            {
                var obj=UnityEngine.Object.Instantiate(board.controls.pauseButton.gameObject,parent);obj.name=name;var rect=(RectTransform)obj.transform;
                rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);rect.pivot=new Vector2(.5f,1);rect.sizeDelta=size;rect.anchoredPosition=position;
                obj.GetComponentInChildren<Text>(true).text=value;var button=obj.GetComponent<Button>();button.onClick=new Button.ButtonClickedEvent();return button;
            }
            var panel=Box("Shared online match menu",owner.transform,new Vector2(640,480),new Vector2(0,-170));var image=panel.AddComponent<Image>();image.color=new Color(.07f,.11f,.16f,.98f);
            var title=Label("Match menu title",panel.transform,"Match menu",new Vector2(580,40),new Vector2(0,-20));title.fontSize=28;
            var requester=Label("Your pause",panel.transform,"You paused the match",new Vector2(580,35),new Vector2(0,-65));
            var passive=Label("Other manager pause",panel.transform,"The other manager paused the match",new Vector2(580,35),new Vector2(0,-65));
            menu.settings=Label("Agreed match settings",panel.transform,"Agreed match settings",new Vector2(580,210),new Vector2(0,-110));
            menu.status=Label("Pause and connection status",panel.transform,"",new Vector2(580,65),new Vector2(0,-330));
            menu.resumeButton=Button("Resume own pause",panel.transform,"Resume match",new Vector2(240,48),new Vector2(0,-410));
            menu.pauseButton=Button("Open online match menu",owner.transform,"Pause / menu",new Vector2(175,44),new Vector2(-290,-20));
            menu.leaveButton=Button("Leave online match",owner.transform,"Leave match",new Vector2(175,44),new Vector2(-100,-20));
            foreach(var button in new[]{menu.pauseButton,menu.leaveButton}){var rect=(RectTransform)button.transform;rect.anchorMin=rect.anchorMax=new Vector2(1,1);}
            board.sharedPausePanel.SetActive(false);board.sharedPausePanel=panel;board.requesterPauseMessage=requester.gameObject;board.passivePauseMessage=passive.gameObject;
            if(board.statusText!=null){var rect=board.statusText.rectTransform;rect.sizeDelta=new Vector2(-480,100);rect.anchoredPosition=new Vector2(-200,-10);}
            panel.SetActive(false);requester.gameObject.SetActive(false);passive.gameObject.SetActive(false);
            EditorUtility.SetDirty(board);EditorUtility.SetDirty(menu);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return "Authored online match menu and persistent leave control";
        }
        finally{if(!string.IsNullOrEmpty(previous))EditorSceneManager.OpenScene(previous,OpenSceneMode.Single);}
    }
}
