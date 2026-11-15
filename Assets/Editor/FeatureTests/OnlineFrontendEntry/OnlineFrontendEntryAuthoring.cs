using System;
using System.Linq;
using CounterAttack.Online;
using TMPro;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class OnlineFrontendEntryAuthoring
{
    public const string PrefabPath="Assets/Settings/OnlineSession/FrontendSession.prefab";
    [CliCommand("author_online_frontend_entry","Author persistent online session prefab and Create New Game host/join controls")]
    public static string Prepare()
    {
        var original=SceneManager.GetActiveScene();string previous=original.path;
        if(EditorApplication.isPlayingOrWillChangePlaymode || original.isDirty || SceneManager.sceneCount!=1)
            throw new InvalidOperationException("Expected one clean stopped scene");
        try
        {
            if(!AssetDatabase.IsValidFolder("Assets/Settings/OnlineSession"))AssetDatabase.CreateFolder("Assets/Settings","OnlineSession");
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if(prefab==null)
            {
                var source=new GameObject("Online Frontend Session",typeof(OnlineFrontendSession));
                try{prefab=PrefabUtility.SaveAsPrefabAsset(source,PrefabPath);}finally{UnityEngine.Object.DestroyImmediate(source);}
            }
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/CreateNewGameScene.unity",OpenSceneMode.Single);
            var setup=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<CreateNewGameManager>(true)).Single();
            var controller=setup.GetComponent<OnlineSetupNetworkPanel>()??setup.gameObject.AddComponent<OnlineSetupNetworkPanel>();
            controller.setup=setup;controller.view=setup.GetComponent<OnlineSetupView>();controller.sessionPrefab=prefab.GetComponent<OnlineFrontendSession>();
            if(controller.panel!=null)throw new InvalidOperationException("Online controls already authored; inspect before changing layout");
            var header=setup.createGameButton.GetComponentInParent<Canvas>().transform.Find("Header");
            if(header==null)throw new InvalidOperationException("Missing Create New Game header");
            var panel=new GameObject("OnlineConnection",typeof(RectTransform),typeof(LayoutElement));panel.transform.SetParent(header,false);
            var size=panel.GetComponent<LayoutElement>();size.minHeight=96;size.preferredHeight=96;
            var rect=panel.GetComponent<RectTransform>();rect.sizeDelta=new Vector2(1200,96);
            controller.panel=panel;
            var row=new GameObject("Controls",typeof(RectTransform),typeof(HorizontalLayoutGroup));row.transform.SetParent(panel.transform,false);
            var rowRect=row.GetComponent<RectTransform>();rowRect.anchorMin=new Vector2(0,1);rowRect.anchorMax=Vector2.one;rowRect.pivot=new Vector2(.5f,1);rowRect.sizeDelta=new Vector2(0,40);rowRect.anchoredPosition=new Vector2(0,-16);
            var layout=row.GetComponent<HorizontalLayoutGroup>();layout.spacing=8;layout.childControlWidth=true;layout.childControlHeight=true;layout.childForceExpandWidth=false;layout.childForceExpandHeight=true;layout.childAlignment=TextAnchor.MiddleCenter;
            Button Button(string name,string label,float width)
            {
                var go=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(Button),typeof(LayoutElement));go.transform.SetParent(row.transform,false);
                go.GetComponent<LayoutElement>().preferredWidth=width;go.GetComponent<Image>().color=new Color(.13f,.19f,.26f);
                var button=go.GetComponent<Button>();button.targetGraphic=go.GetComponent<Image>();
                var text=Label(go.transform,"Label",label);var rt=text.rectTransform;rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.offsetMin=rt.offsetMax=Vector2.zero;
                return button;
            }
            TMP_Text Label(Transform parent,string name,string text)
            {
                var go=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);
                var label=go.GetComponent<TextMeshProUGUI>();label.font=setup.homeTeamInputField.textComponent.font;label.fontSize=20;label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;label.text=text;return label;
            }
            controller.hostButton=Button("Host","Host",90);
            controller.lobbyCode=UnityEngine.Object.Instantiate(setup.homeTeamInputField,row.transform);controller.lobbyCode.name="LobbyCode";
            controller.lobbyCode.onValueChanged=new TMP_InputField.OnChangeEvent();controller.lobbyCode.onEndEdit=new TMP_InputField.SubmitEvent();
            controller.lobbyCode.text="";controller.lobbyCode.characterLimit=32;controller.lobbyCode.contentType=TMP_InputField.ContentType.Alphanumeric;
            controller.lobbyCode.textComponent.fontSize=20;
            if(controller.lobbyCode.placeholder is TMP_Text placeholder){placeholder.text="Host lobby code";placeholder.fontSize=20;}
            var codeLayout=controller.lobbyCode.GetComponent<LayoutElement>()??controller.lobbyCode.gameObject.AddComponent<LayoutElement>();codeLayout.preferredWidth=430;
            controller.joinButton=Button("Join","Join",90);controller.copyButton=Button("Copy","Copy code",120);controller.leaveButton=Button("Leave","Leave",90);
            // Direct-first routing is the normal production policy. Relay-only remains
            // available to automation without exposing implementation detail in the menu.
            controller.relayOnly=null;
            controller.connectionStatus=Label(panel.transform,"ConnectionStatus","Host a private session, or enter a lobby code to join.");
            var status=controller.connectionStatus.rectTransform;status.anchorMin=new Vector2(0,0);status.anchorMax=new Vector2(1,0);status.pivot=new Vector2(.5f,0);status.anchoredPosition=Vector2.zero;status.sizeDelta=new Vector2(0,30);
            panel.SetActive(false);EditorUtility.SetDirty(controller);EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("Could not save authored controls");AssetDatabase.SaveAssets();
            return "Authored Host/Join/Copy/Leave controls and persistent session prefab";
        }
        finally{if(!string.IsNullOrEmpty(previous))EditorSceneManager.OpenScene(previous,OpenSceneMode.Single);}
    }
}
