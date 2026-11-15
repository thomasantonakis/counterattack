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

public static class OnlineDraftAuthoring
{
    [CliCommand("author_online_draft","Author online roster slots, four cards and owned controls in Draft")]
    public static string Prepare()
    {
        var previous=SceneManager.GetActiveScene();string original=previous.path;
        if(EditorApplication.isPlayingOrWillChangePlaymode || previous.isDirty || SceneManager.sceneCount!=1)throw new InvalidOperationException("Expected one clean stopped scene");
        try
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Draft.unity",OpenSceneMode.Single);
            var source=UnityEngine.Object.FindAnyObjectByType<DraftManager>();var ui=UnityEngine.Object.FindAnyObjectByType<DraftUIManager>();
            if(source.GetComponent<OnlineDraftView>()!=null)throw new InvalidOperationException("Online Draft already authored");
            var view=source.gameObject.AddComponent<OnlineDraftView>();view.source=source;view.sourceUi=ui;
            GameObject CloneEmpty(GameObject template,string name)
            {
                var clone=UnityEngine.Object.Instantiate(template,template.transform.parent);clone.name=name;
                foreach(Transform child in clone.transform.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
                return clone;
            }
            var home=CloneEmpty(source.homeTeamPanel,"OnlineHomeRoster");var away=CloneEmpty(source.awayTeamPanel,"OnlineAwayRoster");
            PlayerSlotDropHandler[] Slots(GameObject parent,OnlineTeam team)
            {
                var slots=new PlayerSlotDropHandler[18];
                for(int i=0;i<18;i++)
                {
                    var prefab=i is 0 or 11?Resources.Load<GameObject>("UI/List/GoalkeeperSlot"):source.playerSlotPrefab;
                    var row=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent.transform);row.name=team+"-"+(i+1);
                    row.transform.Find("ContentWrapper/Jersey#").GetComponent<TMP_Text>().text=(i+1).ToString();
                    slots[i]=row.GetComponent<PlayerSlotDropHandler>();slots[i].enabled=false;
                    foreach(var drag in row.GetComponentsInChildren<PlayerSlotDragHandler>(true))drag.enabled=false;
                    var target=row.AddComponent<OnlineDraftClickTarget>();target.view=view;target.index=i+1;target.team=team;
                }
                return slots;
            }
            view.homeSlots=Slots(home,OnlineTeam.Home);view.awaySlots=Slots(away,OnlineTeam.Away);
            var cardRoot=CloneEmpty(source.draftPanel,"OnlineDraftCards");view.cards=new PlayerCard[4];
            Canvas.ForceUpdateCanvases();var bounds=cardRoot.GetComponent<RectTransform>().rect;
            float height=Mathf.Max(200,(bounds.height-160)*.5f),width=height*.625f;
            for(int i=0;i<4;i++)
            {
                var go=(GameObject)PrefabUtility.InstantiatePrefab(source.playerCardPrefab,cardRoot.transform);go.name="OnlineCard"+(i+1);
                view.cards[i]=go.GetComponent<PlayerCard>();foreach(var drag in go.GetComponentsInChildren<PlayerCardDragHandler>(true))drag.enabled=false;
                var rect=go.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.sizeDelta=new Vector2(width,height);
                rect.anchoredPosition=new Vector2((i%2==0?-1:1)*(width+64)*.5f,(i<2?1:-1)*(height+64)*.5f);
                var target=go.AddComponent<OnlineDraftClickTarget>();target.view=view;target.card=true;target.index=i;
            }
            Button CloneButton(string name,string label,float x)
            {
                var button=UnityEngine.Object.Instantiate(ui.startGameButton,ui.startGameButton.transform.parent);button.name=name;button.onClick=new Button.ButtonClickedEvent();
                button.GetComponentInChildren<TMP_Text>(true).text=label;var rect=button.GetComponent<RectTransform>();rect.anchoredPosition+=new Vector2(x,0);return button;
            }
            view.actionButton=CloneButton("OnlineDraftAction","Roll",0);view.leaveButton=CloneButton("OnlineDraftLeave","Leave session",400);
            var localButtons=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Button>(true)).Where(b=>b!=view.actionButton && b!=view.leaveButton
                && Enumerable.Range(0,b.onClick.GetPersistentEventCount()).Any(i=>b.onClick.GetPersistentTarget(i) is DraftUIManager)).Select(b=>b.gameObject).ToList();
            localButtons.AddRange(new[]{source.homeTeamPanel,source.awayTeamPanel,source.homeAveragePanel,source.awayAveragePanel,source.draftPanel,ui.startGameButton.gameObject});
            if(ui.randomDraftButton!=null)localButtons.Add(ui.randomDraftButton.gameObject);
            view.offlineRoots=localButtons.Distinct().ToArray();view.onlineRoots=new[]{home,away,cardRoot,view.actionButton.gameObject,view.leaveButton.gameObject};
            foreach(var root in view.onlineRoots)root.SetActive(false);
            EditorUtility.SetDirty(view);EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("Could not save Draft");
            return "Authored 36 slots, four cards and online Roll/Confirm/Start/Leave controls";
        }
        finally{if(!string.IsNullOrEmpty(original))EditorSceneManager.OpenScene(original,OpenSceneMode.Single);}
    }
}
