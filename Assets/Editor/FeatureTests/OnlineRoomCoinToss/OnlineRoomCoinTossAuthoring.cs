using System;
using CounterAttack.Online;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public static class OnlineRoomCoinTossAuthoring
{
    [CliCommand("author_online_room_coin_toss","Author owned Room coin-toss rolls and choices")]
    public static string Prepare()
    {
        string previous=SceneManager.GetActiveScene().path;
        if(EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().isDirty || SceneManager.sceneCount!=1)throw new InvalidOperationException("Expected one clean stopped scene");
        try
        {
            var scene=EditorSceneManager.OpenScene(OnlineFullRoomAuthoring.ScenePath,OpenSceneMode.Single);var board=UnityEngine.Object.FindAnyObjectByType<OnlineGroundPassBoard>();
            if(board.coinView!=null)return "Online Room coin toss already authored";
            GameObject Box(string name,Transform parent,Vector2 size,Vector2 position)
            {
                var obj=new GameObject(name,typeof(RectTransform));obj.transform.SetParent(parent,false);var rect=(RectTransform)obj.transform;
                rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);rect.pivot=new Vector2(.5f,1);rect.sizeDelta=size;rect.anchoredPosition=position;return obj;
            }
            Text Label(string name,Transform parent,string value,Vector2 size,Vector2 position)
            {
                var obj=Box(name,parent,size,position);var text=obj.AddComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=22;text.color=Color.white;text.raycastTarget=false;text.text=value;text.alignment=TextAnchor.UpperCenter;return text;
            }
            var panel=Box("Online Room coin toss",board.sharedPausePanel.transform.parent,new Vector2(1000,600),new Vector2(0,-160));panel.AddComponent<Image>().color=new Color(.07f,.11f,.16f,.98f);
            panel.transform.SetSiblingIndex(board.sharedPausePanel.transform.GetSiblingIndex());var view=panel.AddComponent<OnlineCoinTossView>();view.board=board;board.coinView=view;
            view.title=Label("Coin toss title",panel.transform,"Room coin toss",new Vector2(900,50),new Vector2(0,-25));view.title.fontSize=30;
            view.homeName=Label("Home manager",panel.transform,"Home",new Vector2(400,50),new Vector2(-230,-100));view.awayName=Label("Away manager",panel.transform,"Away",new Vector2(400,50),new Vector2(230,-100));
            DuelDieView Die(DuelDieView source,string name,Vector2 position)
            {
                var obj=UnityEngine.Object.Instantiate(source.gameObject,panel.transform);obj.name=name;var rect=(RectTransform)obj.transform;
                rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);rect.pivot=new Vector2(.5f,1);rect.sizeDelta=new Vector2(150,150);rect.anchoredPosition=position;return obj.GetComponent<DuelDieView>();
            }
            view.homeDie=Die(board.match.coinTossPanel.dicePanel.homeDie,"Published home die",new Vector2(-230,-170));view.awayDie=Die(board.match.coinTossPanel.dicePanel.awayDie,"Published away die",new Vector2(230,-170));
            view.status=Label("Coin toss status",panel.transform,"",new Vector2(900,90),new Vector2(0,-345));
            Button Button(string name,string value,Vector2 position)
            {
                var obj=UnityEngine.Object.Instantiate(board.menu.resumeButton.gameObject,panel.transform);obj.name=name;var rect=(RectTransform)obj.transform;
                rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);rect.pivot=new Vector2(.5f,1);rect.sizeDelta=new Vector2(200,45);rect.anchoredPosition=position;
                obj.GetComponentInChildren<Text>(true).text=value;var button=obj.GetComponent<Button>();button.onClick=new Button.ButtonClickedEvent();return button;
            }
            view.roll=Button("Roll owned coin toss","Roll",new Vector2(0,-450));view.kickoff=Button("Choose first kick","Take first kick",new Vector2(-260,-515));
            view.left=Button("Choose left end","Left",new Vector2(0,-515));view.right=Button("Choose right end","Right",new Vector2(260,-515));
            view.pause=Button("Pause coin toss","Pause",new Vector2(-375,-25));view.leave=Button("Leave coin toss","Leave match",new Vector2(375,-25));
            panel.SetActive(false);EditorUtility.SetDirty(board);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return "Authored Room coin-toss dice and owned choices";
        }
        finally{if(!string.IsNullOrEmpty(previous))EditorSceneManager.OpenScene(previous,OpenSceneMode.Single);}
    }
}
