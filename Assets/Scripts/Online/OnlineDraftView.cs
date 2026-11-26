using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
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
        private double rollPromptDeadline;
        private bool openingControlsBound;
        public Button previousPageButton,nextPageButton;
        public Button kickoffChoiceButton,leftChoiceButton,rightChoiceButton;
        private OnlineDraftClient client;
        private OnlineFrontendSession session;
        private bool online;
        private bool watching;
        private JObject displayedPublication;
        private string selected;
        private int selectedSlot;
        private int page;
        private string poolSelection;
        private Dictionary<string,OnlineDraftCard> localCatalog;
        public OnlineDraftClient Client=>client;
        private void Awake()
        {
            watching = OnlineFrontendSession.Current?.IsSpectator == true;
            online=watching || ApplicationManager.Instance?.SelectedRoomGameMode==ApplicationManager.OnlineGameMode;
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
            if (sourceUi.coinTossPanel != null && !watching && !openingControlsBound)
            {
                var coin = sourceUi.coinTossPanel;
                coin.kickoffButton.onClick.AddListener(() => Choose(CoinTossChoice.Kickoff));
                coin.leftToRightButton.onClick.AddListener(() => Choose(CoinTossChoice.LeftToRight));
                coin.rightToLeftButton.onClick.AddListener(() => Choose(CoinTossChoice.RightToLeft));
                openingControlsBound = true;
            }
            foreach (var root in offlineRoots)
                if (root != null && root != source.homeAveragePanel && root != source.awayAveragePanel)
                    root.SetActive(false);
        }
        private void Start()
        {
            if(!online)return;
            session = OnlineFrontendSession.Current;
            if (watching)
            {
                StartSpectating();
                return;
            }
            actionButton.onClick.AddListener(Act);leaveButton.onClick.AddListener(Leave);
            if(rollPanelButton!=null)rollPanelButton.onClick.AddListener(Act);
            if(previousPageButton!=null)previousPageButton.onClick.AddListener(()=>ChangePage(-1));
            if(nextPageButton!=null)nextPageButton.onClick.AddListener(()=>ChangePage(1));
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
        public void Leave()
        {
            if (watching) { session.LeaveSpectating(); return; }
            OnlineFrontendSession.Current?.Close();
            SceneManager.LoadScene("MainMenu");
        }
        private void LateUpdate()
        {
            if(!online)return;
            if (watching)
            {
                RenderSpectating();
                return;
            }
            if(Input.GetKeyDown(KeyCode.R) && client?.Capture()?.phase is "roll" or "opening-roll")Act();
            var dice = rendered?.phase switch
            {
                "roll" => source.draftOrderPanel,
                "opening-roll" => sourceUi.coinTossPanel?.dicePanel,
                _ => null
            };
            if (dice == null) return;
            int remaining = Mathf.Max(0, Mathf.CeilToInt((float)(rollPromptDeadline - Time.realtimeSinceStartupAsDouble)));
            string team = rendered.turn == OnlineTeam.Home ? rendered.settings.homeTeamName : rendered.settings.awayTeamName;
            dice.footer.text = !client.Connected ? "Reconnecting" : rendered.turn == client.Team
                ? $"Press [R] to roll · Auto-roll in {remaining}s" : $"{team} rolls in {remaining}s";
        }
        private void Render()
        {
            if(!online)return;
            var s=client?.Capture();
            if(s!=null && (rendered==null || s.phase!=rendered.phase || s.turn!=rendered.turn
                || s.rollRound!=rendered.rollRound || s.connectionRevision!=rendered.connectionRevision))
                rollPromptDeadline=Time.realtimeSinceStartupAsDouble+5;
            rendered=s;
            if(s==null || !client.Connected || s.revision!=remoteDragRevision)ClearRemoteDrag();
            actionButton.interactable=false;
            if(s==null){sourceUi.draftTurnText.text="Connecting to Draft...";sourceUi.draftMetaText.text="Waiting for the shared draft state.";return;}
            if(poolSelection!=s.selection){poolSelection=s.selection;page=0;selected=null;selectedSlot=0;}
            RenderRosters(s);
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
                if(button!=null)button.gameObject.SetActive(false);
            }
            RenderRolls(s, client.Connected && !client.Pending && s.turn == client.Team);
            string team=s.turn==OnlineTeam.Home?s.settings.homeTeamName:s.settings.awayTeamName;
            sourceUi.draftTurnText.text=s.phase is "roll" or "opening-roll"?team+" rolls":s.phase=="opening-choice"?team+" chooses":s.phase=="pick"?s.repeatable?"Choose your "+s.selection:team+" picks":"Draft complete";
            sourceUi.draftBatchText.text=s.freeSelection? s.selection+" · Page "+(page+1)+" / "+Mathf.Max(1,(s.available.Length+3)/4):"Batch "+s.batch+" / "+s.totalBatches+" · Home roll "+s.homeRoll+" · Away roll "+s.awayRoll;
            bool localReady=client.Team==OnlineTeam.Home?s.homeReady:s.awayReady;
            string label=s.phase is "roll" or "opening-roll"?"Roll":client.IsHost && s.homeReady && s.awayReady?"Start match":localReady?"Waiting for player":"Confirm roster";
            actionButton.GetComponentInChildren<TMP_Text>(true).text=label;
            actionButton.interactable=client.Connected && !client.Pending && ((s.phase is "roll" or "opening-roll") && s.turn==client.Team || s.phase=="review" && (!localReady || client.IsHost && s.homeReady && s.awayReady));
            if(rollPanelButton!=null)
            {
                rollPanelButton.gameObject.SetActive(s.phase == "roll" && actionButton.interactable);
                actionButton.gameObject.SetActive(s.phase=="review");
            }
            sourceUi.draftMetaText.text=!client.Connected?"Connection interrupted. Draft is held.":s.phase=="room-ready"?"Opening choices agreed. Preparing match...":client.Pending?"Sending selection...":s.phase=="opening-choice"?s.turn==client.Team?"Choose kickoff or your attacking direction.":"Waiting for the other manager's opening choice."
                :activeDrag!=null?"Drop onto one of your own roster slots.":selected!=null?"Select a destination slot in your roster.":selectedSlot>0?"Select another own slot to swap with number "+selectedSlot+"."
                :s.phase=="pick"?"Select a card, then your preferred slot. Select two own slots to swap.":s.phase=="review"?"Review your roster and confirm. Changes clear both confirmations.":"Each manager rolls for their own team.";
        }
        private void StartSpectating()
        {
            foreach (var root in gameObject.scene.GetRootGameObjects())
            {
                foreach (var control in root.GetComponentsInChildren<Selectable>(true))
                    control.interactable = control == leaveButton;
                foreach (var drag in root.GetComponentsInChildren<PlayerSlotDragHandler>(true)) drag.enabled = false;
                foreach (var drag in root.GetComponentsInChildren<PlayerCardDragHandler>(true)) drag.enabled = false;
                foreach (var target in root.GetComponentsInChildren<OnlineDraftClickTarget>(true)) target.enabled = false;
                foreach (var scroll in root.GetComponentsInChildren<ScrollRect>(true)) scroll.enabled = false;
            }
            foreach (var button in new[] { actionButton, rollPanelButton, previousPageButton, nextPageButton,
                kickoffChoiceButton, leftChoiceButton, rightChoiceButton })
                if (button != null) button.gameObject.SetActive(false);
            foreach (var card in cards) card.gameObject.SetActive(false);
            leaveButton.onClick.AddListener(Leave);
            RenderSpectating();
        }

        private void RenderSpectating()
        {
            sourceUi.draftMetaText.text = session.Error ?? "Spectating Draft";
            var publication = session.SpectatorPublication;
            if (publication == displayedPublication || (string)publication?["stage"] != "draft") return;
            var previous = displayedPublication;
            displayedPublication = publication;
            if (JToken.DeepEquals(previous?["draft"], publication["draft"])
                && JToken.DeepEquals(previous?["settings"], publication["settings"])
                && JToken.DeepEquals(previous?["teams"], publication["teams"])) return;

            var state = publication["draft"]?.ToObject<OnlineDraftState>();
            if (state?.home == null || state.away == null) return;
            state.settings = OnlineSpectatorPregameWire.ReadSettings(publication);
            if (state.settings == null) return;
            RenderRosters(state);
            RenderRolls(state, ownsRoll: false);

            string team = state.turn == OnlineTeam.Home ? state.settings.homeTeamName : state.settings.awayTeamName;
            sourceUi.draftTurnText.text = state.phase switch
            {
                "roll" or "opening-roll" => team + " rolls",
                "opening-choice" => team + " chooses",
                "roll-result" or "opening-result" => "Roll results",
                "pick" => team + " picks",
                _ => "Draft complete"
            };
            sourceUi.draftBatchText.text = state.totalBatches > 0 && state.phase == "pick"
                ? $"Batch {state.batch} / {state.totalBatches}" : string.Empty;
        }

        private void RenderRosters(OnlineDraftState s)
        {
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
            source.UpdateTeamAverages(homeSlots[0].transform.parent, source.homeAveragePanel.transform, s.home.Length);
            source.UpdateTeamAverages(awaySlots[0].transform.parent, source.awayAveragePanel.transform, s.away.Length);
        }

        private void RenderRolls(OnlineDraftState s, bool ownsRoll)
        {
            bool rolling=s.phase=="roll";
            bool rollResult=s.phase=="roll-result";
            bool opening=s.phase is "opening-roll" or "opening-result" or "opening-choice";
            var coin = sourceUi.coinTossPanel;
            if (opening && coin != null)
                coin.PresentPublishedToss(s.settings.homeTeamName, s.settings.awayTeamName, s.settings.homeKit, s.settings.awayKit,
                    new OnlineCoinTossSnapshot
                    {
                        purpose = nameof(CoinTossPurpose.FirstHalf),
                        stage = s.phase == "opening-choice" ? "choice" : s.phase == "opening-result" ? "result"
                            : s.turn == OnlineTeam.Home ? "home" : "away",
                        homeRoll = s.homeRoll, awayRoll = s.awayRoll, winnerHome = s.openingWinnerIsHome,
                        choosingHome = s.turn == OnlineTeam.Home, canChooseKickoff = s.openingCanChooseKickoff
                    }, ownsRoll, viewOnly: watching);
            else coin?.HidePublishedToss();
            var dice=source.draftOrderPanel;
            if(dice!=null)
            {
                dice.visuals.SetActive(rolling || rollResult);
                if(rolling || rollResult)
                {
                    if(rollPanelHeight<=0)rollPanelHeight=dice.panel.rect.height;
                    dice.panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,rollPanelHeight);
                    dice.title.text="ROLL FOR FIRST DRAFT PICK";
                    dice.homeTeam.text=s.settings.homeTeamName;dice.awayTeam.text=s.settings.awayTeamName;
                    var homePalette=TokenKitCatalog.GetInstructionPalette(TokenKitCatalog.ResolveStyle(s.settings.homeKit));
                    var awayPalette=TokenKitCatalog.GetInstructionPalette(TokenKitCatalog.ResolveStyle(s.settings.awayKit));
                    dice.homeDie.SetTeamColors(homePalette.Primary,homePalette.Secondary);
                    dice.awayDie.SetTeamColors(awayPalette.Primary,awayPalette.Secondary);
                    dice.homeResult.text=s.homeRoll==0?"Waiting for roll":"Roll "+s.homeRoll;
                    dice.awayResult.text=s.awayRoll==0?"Waiting for roll":"Roll "+s.awayRoll;
                    string winner=s.homeRoll==s.awayRoll?"Tie. Roll again.":(s.homeRoll>s.awayRoll?s.settings.homeTeamName:s.settings.awayTeamName)+" wins the roll";
                    dice.outcome.text=rollResult?winner:"Round "+s.rollRound+" - Highest roll drafts first";
                    dice.footer.text=string.Empty;
                    CoinTossPanelController.PresentDie(dice.homeDie, s.homeRoll, rolling && s.turn == OnlineTeam.Home);
                    CoinTossPanelController.PresentDie(dice.awayDie, s.awayRoll, rolling && s.turn == OnlineTeam.Away);
                }
            }
        }

        private static Dictionary<string,string> Fields(OnlineDraftCard c)=>new()
        {{"Name",c.name},{"Nationality",c.country},{"Type",c.type},{"sqno",c.squadNumber.ToString()},{"Pace",c.pace.ToString()},{"Dribbling",c.dribbling.ToString()},{"Heading",(c.goalkeeper?c.aerial:c.heading).ToString()},
            {"HighPass",c.highPass.ToString()},{"Resilience",c.resilience.ToString()},{"Shooting",(c.goalkeeper?c.saving:c.shooting).ToString()},{"Tackling",(c.goalkeeper?c.handling:c.tackling).ToString()},{"Aerial",c.aerial.ToString()},{"Saving",c.saving.ToString()},{"Handling",c.handling.ToString()}};
        private static Player Player(OnlineDraftCard c)=>new(Fields(c));
        private static Goalkeeper Goalkeeper(OnlineDraftCard c)=>new(Fields(c));
        private void OnDestroy(){if(client!=null){client.Changed-=Render;client.DragReceived-=RenderRemoteDrag;}if(session!=null)session.DraftAvailable-=Configure;}
    }
}
