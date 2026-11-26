using System;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace CounterAttack.Online
{
    [DefaultExecutionOrder(100)]
    public sealed class OnlineSetupNetworkPanel:MonoBehaviour
    {
        public CreateNewGameManager setup;
        public OnlineSetupView view;
        public GameObject panel;
        public Button hostButton,joinButton,leaveButton,copyButton;
        public TMP_InputField lobbyCode;
        public Toggle relayOnly;
        public TMP_Text connectionStatus;
        public OnlineFrontendSession sessionPrefab;
        private OnlineFrontendSession bound;
        private OnlineSetupClient seededClient;
        private GameSettings pendingSetupSelection;
        private bool preparingHostSetup;
        private bool online;
        private bool joining;
        private string startError;
        private bool watching;
        private JObject displayedPublication;
        private GameSettings lastValidEarlySetup;
        private void Start()
        {
            online=ApplicationManager.Instance?.SelectedRoomGameMode==ApplicationManager.OnlineGameMode;
            if(panel!=null)panel.SetActive(online);
            if(!online)return;
            if (OnlineFrontendSession.Current is { IsSpectator: true } spectator)
            {
                watching = true;
                bound = spectator;
                GuardLocalSave();
                setup.enabled = false; // Disable scene keyboard shortcuts after the authored controls initialize.
                LockSpectatorControls();
                foreach (var control in new Selectable[] { hostButton, joinButton, copyButton, lobbyCode, relayOnly, setup.createGameButton })
                    if (control != null) control.gameObject.SetActive(false);
                leaveButton.gameObject.SetActive(true);
                leaveButton.onClick.AddListener(spectator.LeaveSpectating);
                var label = leaveButton.GetComponentInChildren<TMP_Text>(true);
                if (label != null) label.text = "Leave";
                RefreshSpectator();
                return;
            }
            lobbyCode.characterLimit = OnlineSpectatorAdmission.CodeLength;
            if(string.IsNullOrWhiteSpace(setup.homeTeamInputField.text))setup.homeTeamInputField.SetTextWithoutNotify("Home");
            if(string.IsNullOrWhiteSpace(setup.awayTeamInputField.text))setup.awayTeamInputField.SetTextWithoutNotify("Away");
            GuardLocalSave();hostButton.onClick.AddListener(Host);joinButton.onClick.AddListener(Join);
            leaveButton.onClick.AddListener(Leave);copyButton.onClick.AddListener(CopyLobbyCode);
            if(OnlineFrontendSession.Current!=null)Bind(OnlineFrontendSession.Current);
            var joinCode = OnlineModeMenu.TakeJoinCode();
            joining = joinCode != null;
            hostButton.gameObject.SetActive(!joining);
            joinButton.gameObject.SetActive(false);
            lobbyCode.gameObject.SetActive(!joining);
            copyButton.gameObject.SetActive(!joining);
            var leaveLabel = leaveButton.GetComponentInChildren<TMP_Text>();
            if (joining && leaveLabel != null) leaveLabel.text = "Cancel / Back";
            if (!joining && OnlineFrontendSession.Current == null) Show("Host a session and share its lobby code.");
            if (joining && OnlineFrontendSession.Current == null)
            {
                lobbyCode.SetTextWithoutNotify(joinCode);
                Join();
            }
            Refresh();
        }
        private void GuardLocalSave()=>setup.BindOnlineSetupConfirmation(_=>Show("Start an online session first."));
        private void Show(string message){if(connectionStatus!=null)connectionStatus.text=message;}
        public void Host()
        {
            if(!online || OnlineFrontendSession.Current!=null)return;
            if(!setup.TryCaptureOnlineSetupSettings(out var settings,out var error)){Show(error);return;}
            startError=null;
            try{var session=Instantiate(sessionPrefab);Bind(session);session.Host(settings,relayOnly!=null && relayOnly.isOn);}
            catch(Exception connectionError){startError=connectionError.Message;Show(startError);bound?.Close();}
        }
        public void Join()
        {
            if(!online || OnlineFrontendSession.Current!=null)return;
            if (!OnlineFrontendSessionPolicy.TryNormalizeLobbyCode(lobbyCode.text, out var code))
            {
                Show(OnlineFrontendSessionPolicy.LobbyCodePrompt);
                return;
            }
            startError=null;
            // Menu-driven joins open setup with placeholder values.
            pendingSetupSelection=joining?null:setup.CaptureOnlineSetupDraft();
            try{var session=Instantiate(sessionPrefab);Bind(session);session.Join(code,relayOnly!=null && relayOnly.isOn);}
            catch(Exception connectionError){startError=connectionError.Message;Show(startError);bound?.Close();}
        }
        private void Bind(OnlineFrontendSession session)
        {
            if(bound!=null)bound.ClientAvailable-=Connected;
            bound=session;bound.ClientAvailable+=Connected;
            if(bound.Client!=null)Connected(bound.Client);
            else if(bound.CaptureHostedSetup() is { } settings)
            {
                setup.ApplyOnlineSetupSettings(settings, ownsHome: true);
                preparingHostSetup = true;
                lastValidEarlySetup = settings;
                bound.CaptureEarlySpectatorSetup = CaptureEarlySetup;
            }
        }
        private GameSettings CaptureEarlySetup()
        {
            if (setup.TryCaptureOnlineSetupSettings(out var settings, out _))
                lastValidEarlySetup = settings;
            return lastValidEarlySetup;
        }
        private void Connected(OnlineSetupClient client)
        {
            if (preparingHostSetup)
            {
                bound.CaptureEarlySpectatorSetup = null;
                pendingSetupSelection = setup.CaptureOnlineSetupDraft();
                preparingHostSetup = false;
            }
            view.Configure(client);
            if(pendingSetupSelection==null)return;
            seededClient=client;seededClient.Changed+=SeedSetupSelection;SeedSetupSelection();
        }
        private void SeedSetupSelection()
        {
            if(seededClient?.Capture()==null || pendingSetupSelection==null)return;
            var proposed=pendingSetupSelection;pendingSetupSelection=null;
            var client=seededClient;client.Changed-=SeedSetupSelection;seededClient=null;
            client.Edit(proposed);view.RefreshSelections();
        }
        public void Leave()
        {
            if (joining)
            {
                setup.BackToCreateLoadRoomMenu();
                return;
            }
            startError = null;
            if(bound!=null){bound.Close();view.Unbind();GuardLocalSave();}
        }
        public void CopyLobbyCode()
        {if(bound?.LobbyCode!=null)GUIUtility.systemCopyBuffer=bound.LobbyCode;}
        private void LateUpdate()
        {
            if (watching) RefreshSpectator();
            else if (online) Refresh();
        }
        private void RefreshSpectator()
        {
            Show(bound.Error ?? "Spectating match setup");
            var publication = bound.SpectatorPublication;
            if (publication == displayedPublication || (string)publication?["stage"] != "setup") return;
            if (!JToken.DeepEquals(displayedPublication?["settings"], publication["settings"])
                || !JToken.DeepEquals(displayedPublication?["teams"], publication["teams"]))
            {
                var settings = OnlineSpectatorPregameWire.ReadSettings(publication);
                if (settings != null)
                {
                    setup.ApplyOnlineSetupSettings(settings);
                    LockSpectatorControls();
                }
            }
            displayedPublication = publication;
            setup.createGameButton.interactable = false;
        }
        private void LockSpectatorControls()
        {
            foreach (var root in gameObject.scene.GetRootGameObjects())
            foreach (var control in root.GetComponentsInChildren<Selectable>(true))
                control.interactable = control == leaveButton;
        }
        private void Refresh()
        {
            var current=OnlineFrontendSession.Current;
            if(bound==null && current!=null)Bind(current);
            bool idle=current==null;
            hostButton.interactable=joinButton.interactable=idle && sessionPrefab!=null;
            leaveButton.interactable=joining || !idle;copyButton.interactable=current?.LobbyCode!=null;
            lobbyCode.readOnly=true;
            if(!joining)
            {
                var code=current?.LobbyCode??string.Empty;
                if(lobbyCode.text!=code)lobbyCode.SetTextWithoutNotify(code);
            }
            if(current?.Client==null)setup.createGameButton.interactable=false;
            if(current==null)
            {
                if(!ReferenceEquals(bound,null)){view.Unbind();bound=null;preparingHostSetup=false;GuardLocalSave();Show(startError ?? "Host a session and share its lobby code.");}
                return;
            }
            if (current.Error != null) { Show(current.Error); return; }
            if (startError != null) { Show(startError); return; }
            var latency=current.Latency;string role=current.Client==null?"":current.Client.Team==OnlineTeam.Home?" You manage Home.":" You manage Away.";
            Show(current.Status+role+(latency.samples>0?" Network "+latency.lastMilliseconds.ToString("F0")+" ms (p95 "+latency.p95Milliseconds.ToString("F0")+" ms).":""));
        }
        private void OnDestroy()
        {
            if (watching) return;
            if (bound != null && preparingHostSetup) bound.CaptureEarlySpectatorSetup = null;
            if(bound!=null)bound.ClientAvailable-=Connected;
            if(seededClient!=null)seededClient.Changed-=SeedSetupSelection;
            // Leaving setup before agreement cancels its native lifetime; agreed
            // sessions remain alive for the scene-transition coordinator.
            if(bound!=null && bound.AgreedSettings==null && !bound.LaunchingRoom)bound.Close();
        }
    }
}
