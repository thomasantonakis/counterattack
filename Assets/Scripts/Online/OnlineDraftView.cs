using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CounterAttack.Online
{
    [DefaultExecutionOrder(-500)]
    public sealed partial class OnlineDraftView:MonoBehaviour
    {
        public DraftManager source;
        public DraftUIManager sourceUi;
        public GameObject[] onlineRoots,offlineRoots;
        public PlayerSlotDropHandler[] homeSlots,awaySlots;
        public PlayerCard[] cards;
        public Button actionButton,leaveButton;
        public Button rollPanelButton;
        private float rollPanelHeight;
        public Button previousPageButton,nextPageButton;
        public Button kickoffChoiceButton,leftChoiceButton,rightChoiceButton;
        private OnlineDraftClient client;
        private OnlineFrontendSession session;
        private bool online;
        private string selected;
        private int selectedSlot;
        private int page;
        private string poolSelection;
        private Dictionary<string,OnlineDraftCard> localCatalog;
        public OnlineDraftClient Client=>client;
        private void Awake()
        {
            online=ApplicationManager.Instance?.SelectedRoomGameMode==ApplicationManager.OnlineGameMode;
            OwnScene();
        }
        private void OwnScene()
        {
            foreach(var root in onlineRoots)if(root!=null)root.SetActive(online);
            if(!online)
            {
                foreach(var button in new[]{rollPanelButton,kickoffChoiceButton,leftChoiceButton,rightChoiceButton})
                    if(button!=null)button.gameObject.SetActive(false);
                return;
            }
            source.enabled=false;sourceUi.enabled=false;
            if(source.draftOrderPanel!=null){source.draftOrderPanel.externalInput=true;source.draftOrderPanel.enabled=false;}
            foreach(var root in offlineRoots)if(root!=null)root.SetActive(false);
        }
        private void Start()
        {
            if(!online)return;
            actionButton.onClick.AddListener(Act);leaveButton.onClick.AddListener(Leave);
            if(rollPanelButton!=null)rollPanelButton.onClick.AddListener(Act);
            if(previousPageButton!=null)previousPageButton.onClick.AddListener(()=>ChangePage(-1));
            if(nextPageButton!=null)nextPageButton.onClick.AddListener(()=>ChangePage(1));
            if(kickoffChoiceButton!=null)kickoffChoiceButton.onClick.AddListener(()=>Choose(CoinTossChoice.Kickoff));
            if(leftChoiceButton!=null)leftChoiceButton.onClick.AddListener(()=>Choose(CoinTossChoice.LeftToRight));
            if(rightChoiceButton!=null)rightChoiceButton.onClick.AddListener(()=>Choose(CoinTossChoice.RightToLeft));
            session=OnlineFrontendSession.Current;
            if(session!=null){session.DraftAvailable+=Configure;if(session.Draft!=null)Configure(session.Draft);}
            Render();
        }
        public void Configure(OnlineDraftClient owner)
        {
            if(client!=null){client.Changed-=Render;client.DragReceived-=RenderRemoteDrag;}
            ClearRemoteDrag();
            online=true;OwnScene();client=owner??throw new ArgumentNullException(nameof(owner));client.Changed+=Render;client.DragReceived+=RenderRemoteDrag;Render();
        }
        public void SelectCard(int index,bool quickPick=false)
        {
            var s=client?.Capture();if(activeDrag!=null || s?.phase!="pick" || !client.Connected || client.Pending || !s.repeatable && s.turn!=client.Team)return;
            var displayed=DisplayedCards(s);if(index<0 || index>=displayed.Length || displayed[index]==null)return;
            var offer=displayed[index];
            selected=offer.id;selectedSlot=0;
            if(quickPick)
            {
                var roster=client.Team==OnlineTeam.Home?s.home:s.away;
                for(int slot=0;slot<roster.Length;slot++)
                {
                    if(roster[slot]!=null || GoalkeeperSlot(slot+1)!=offer.goalkeeper)continue;
                    if(client.Request(OnlineDraftIntent.Pick,offer.id,slot+1))return;
                    break;
                }
            }
            Render();
        }
        public void ChangePage(int delta){ClearRemoteDrag();page=Mathf.Max(0,page+delta);selected=null;Render();}
        private OnlineDraftCard[] DisplayedCards(OnlineDraftState s)
        {
            if(!s.freeSelection)return s.cards;
            if(s.phase!="pick")return Array.Empty<OnlineDraftCard>();
            localCatalog??=OnlineDraftCatalog.Load().ToDictionary(c=>c.id);
            page=Mathf.Clamp(page,0,Mathf.Max(0,(s.available.Length-1)/4));
            return s.available.Skip(page*4).Take(4).Where(localCatalog.ContainsKey).Select(id=>localCatalog[id]).ToArray();
        }
        public void SelectSlot(OnlineTeam team,int slot)
        {
            if(activeDrag!=null || client==null || team!=client.Team || !client.Connected || client.Pending)return;
            var s=client.Capture();if(s==null || s.phase is not ("pick" or "review"))return;
            if(selected!=null)
            {
                if(client.Request(OnlineDraftIntent.Pick,selected,slot))selected=null;
            }
            else if(selectedSlot==0)selectedSlot=slot;
            else{int previous=selectedSlot;selectedSlot=0;client.Request(OnlineDraftIntent.Swap,null,previous,slot);}
            Render();
        }
        public void Act()
        {
            var s=client?.Capture();if(s==null)return;
            if(s.phase is "roll" or "opening-roll" && s.turn==client.Team)client.Request(OnlineDraftIntent.Roll);
            else if(s.phase=="review")
            {
                bool ready=client.Team==OnlineTeam.Home?s.homeReady:s.awayReady;
                if(!ready)client.Request(OnlineDraftIntent.Ready);
                else if(client.IsHost && s.homeReady && s.awayReady)client.Request(OnlineDraftIntent.Start);
            }
        }
        public void Choose(CoinTossChoice choice)
        {var s=client?.Capture();if(s?.phase=="opening-choice" && s.turn==client.Team)client.Request(OnlineDraftIntent.Choose,choice:choice);}
        public void Leave(){OnlineFrontendSession.Current?.Close();SceneManager.LoadScene("MainMenu");}
        private void LateUpdate()
        {
            if(!online)return;
            if(Input.GetKeyDown(KeyCode.R) && client?.Capture()?.phase is "roll" or "opening-roll")Act();
        }
        private void Render()
        {
            if(!online)return;
            var s=client?.Capture();rendered=s;
            if(s==null || !client.Connected || s.revision!=remoteDragRevision)ClearRemoteDrag();
            actionButton.interactable=false;
            if(s==null){sourceUi.draftTurnText.text="Connecting to Draft...";sourceUi.draftMetaText.text="Waiting for the shared draft state.";return;}
            if(poolSelection!=s.selection){poolSelection=s.selection;page=0;selected=null;selectedSlot=0;}
            source.currentSettings=OnlineSetupRules.Copy(s.settings);
            source.homeTeamName.text=s.settings.homeTeamName;source.awayTeamName.text=s.settings.awayTeamName;source.refereeText.text=s.settings.referee;
            void Roster(PlayerSlotDropHandler[] slots,OnlineDraftCard[] roster)
            {
                for(int i=0;i<slots.Length;i++)
                {
                    slots[i].gameObject.SetActive(i<roster.Length);if(i>=roster.Length)continue;
                    var c=roster[i];if(c==null)slots[i].ClearSlot();
                    else if(c.goalkeeper)slots[i].UpdateGoalkeeperSlot(Goalkeeper(c));else slots[i].UpdatePlayerSlot(Player(c));
                }
            }
            Roster(homeSlots,s.home);Roster(awaySlots,s.away);
            var displayedCards=DisplayedCards(s);
            for(int i=0;i<cards.Length;i++)
            {
                var offer=i<displayedCards.Length?displayedCards[i]:null;
                var card=cards[i];
                var visibility=card.GetComponent<CanvasGroup>();
                card.gameObject.SetActive(s.phase=="pick" && (offer!=null || visibility!=null));
                if(visibility!=null && (activeDrag==null || !activeDrag.card || activeDrag.index!=i))
                {
                    visibility.alpha=offer==null?0:1;
                    visibility.blocksRaycasts=offer!=null;
                }
                if(offer==null)continue;
                card.UpdatePlayerCard(Player(offer));
                if(offer.goalkeeper)card.playerNameText.text="GK · "+offer.name;
            }
            if(previousPageButton!=null){previousPageButton.gameObject.SetActive(s.freeSelection && s.phase=="pick");previousPageButton.interactable=page>0;}
            if(nextPageButton!=null){nextPageButton.gameObject.SetActive(s.freeSelection && s.phase=="pick");nextPageButton.interactable=(page+1)*4<s.available.Length;}
            foreach(var button in new[]{kickoffChoiceButton,leftChoiceButton,rightChoiceButton})
            {
                if(button==null)continue;button.gameObject.SetActive(s.phase=="opening-choice" && (button!=kickoffChoiceButton || s.openingCanChooseKickoff));
                button.interactable=client.Connected && !client.Pending && s.turn==client.Team;
            }
            bool rolling=s.phase is "roll" or "opening-roll";
            bool rollResult=s.phase is "roll-result" or "opening-result";
            bool opening=s.phase is "opening-roll" or "opening-result" or "opening-choice";
            bool choosing=s.phase=="opening-choice";
            var dice=source.draftOrderPanel;
            if(dice!=null)
            {
                dice.visuals.SetActive(rolling || rollResult || choosing);
                if(rolling || rollResult || choosing)
                {
                    if(rollPanelHeight<=0)rollPanelHeight=dice.panel.rect.height;
                    dice.panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,rollPanelHeight+(choosing?270:0));
                    dice.title.text=opening?"OPENING COIN TOSS":"ROLL FOR FIRST DRAFT PICK";
                    dice.homeTeam.text=s.settings.homeTeamName;dice.awayTeam.text=s.settings.awayTeamName;
                    var homePalette=TokenKitCatalog.GetInstructionPalette(TokenKitCatalog.ResolveStyle(s.settings.homeKit));
                    var awayPalette=TokenKitCatalog.GetInstructionPalette(TokenKitCatalog.ResolveStyle(s.settings.awayKit));
                    dice.homeDie.SetTeamColors(homePalette.Primary,homePalette.Secondary);
                    dice.awayDie.SetTeamColors(awayPalette.Primary,awayPalette.Secondary);
                    dice.homeResult.text=s.homeRoll==0?"Waiting for roll":"Roll "+s.homeRoll;
                    dice.awayResult.text=s.awayRoll==0?"Waiting for roll":"Roll "+s.awayRoll;
                    string winner=s.homeRoll==s.awayRoll?"Tie. Roll again.":(s.homeRoll>s.awayRoll?s.settings.homeTeamName:s.settings.awayTeamName)+" wins the roll";
                    dice.outcome.text=rollResult || choosing?winner:"Round "+s.rollRound+(opening?" - Highest roll chooses kickoff or direction":" - Highest roll drafts first");
                    dice.footer.text=string.Empty;
                    if(s.homeRoll>0)dice.homeDie.Resolve(s.homeRoll,false);else if(s.turn==OnlineTeam.Home)dice.homeDie.Begin(false);else dice.homeDie.Clear();
                    if(s.awayRoll>0)dice.awayDie.Resolve(s.awayRoll,false);else if(s.turn==OnlineTeam.Away)dice.awayDie.Begin(false);else dice.awayDie.Clear();
                }
            }
            string team=s.turn==OnlineTeam.Home?s.settings.homeTeamName:s.settings.awayTeamName;
            sourceUi.draftTurnText.text=s.phase is "roll" or "opening-roll"?team+" rolls":s.phase=="opening-choice"?team+" chooses":s.phase=="pick"?s.repeatable?"Choose your "+s.selection:team+" picks":"Draft complete";
            sourceUi.draftBatchText.text=s.freeSelection? s.selection+" · Page "+(page+1)+" / "+Mathf.Max(1,(s.available.Length+3)/4):"Batch "+s.batch+" / "+s.totalBatches+" · Home roll "+s.homeRoll+" · Away roll "+s.awayRoll;
            bool localReady=client.Team==OnlineTeam.Home?s.homeReady:s.awayReady;
            string label=s.phase is "roll" or "opening-roll"?"Roll":client.IsHost && s.homeReady && s.awayReady?"Start match":localReady?"Waiting for player":"Confirm roster";
            actionButton.GetComponentInChildren<TMP_Text>(true).text=label;
            actionButton.interactable=client.Connected && !client.Pending && ((s.phase is "roll" or "opening-roll") && s.turn==client.Team || s.phase=="review" && (!localReady || client.IsHost && s.homeReady && s.awayReady));
            if(rollPanelButton!=null)
            {
                rollPanelButton.gameObject.SetActive(rolling);
                rollPanelButton.interactable=rolling && actionButton.interactable;
                actionButton.gameObject.SetActive(s.phase=="review");
            }
            sourceUi.draftMetaText.text=!client.Connected?"Connection interrupted. Draft is held.":s.phase=="room-ready"?"Opening choices agreed. Preparing match...":client.Pending?"Sending selection...":s.phase=="opening-choice"?s.turn==client.Team?"Choose kickoff or your attacking direction.":"Waiting for the other manager's opening choice."
                :activeDrag!=null?"Drop onto one of your own roster slots.":selected!=null?"Select a destination slot in your roster.":selectedSlot>0?"Select another own slot to swap with number "+selectedSlot+"."
                :s.phase=="pick"?"Select a card, then your preferred slot. Select two own slots to swap.":s.phase=="review"?"Review your roster and confirm. Changes clear both confirmations.":"Each manager rolls for their own team.";
        }
        private static Dictionary<string,string> Fields(OnlineDraftCard c)=>new()
        {{"Name",c.name},{"Nationality",c.country},{"Type",c.type},{"sqno",c.squadNumber.ToString()},{"Pace",c.pace.ToString()},{"Dribbling",c.dribbling.ToString()},{"Heading",(c.goalkeeper?c.aerial:c.heading).ToString()},
            {"HighPass",c.highPass.ToString()},{"Resilience",c.resilience.ToString()},{"Shooting",(c.goalkeeper?c.saving:c.shooting).ToString()},{"Tackling",(c.goalkeeper?c.handling:c.tackling).ToString()},{"Aerial",c.aerial.ToString()},{"Saving",c.saving.ToString()},{"Handling",c.handling.ToString()}};
        private static Player Player(OnlineDraftCard c)=>new(Fields(c));
        private static Goalkeeper Goalkeeper(OnlineDraftCard c)=>new(Fields(c));
        private void OnDestroy(){if(client!=null){client.Changed-=Render;client.DragReceived-=RenderRemoteDrag;}if(session!=null)session.DraftAvailable-=Configure;}
    }
}
