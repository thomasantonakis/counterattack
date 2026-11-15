using System;
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
        private GameSettings pendingGuestSelection;
        private bool online;
        private void Start()
        {
            online=ApplicationManager.Instance?.SelectedRoomGameMode==ApplicationManager.OnlineGameMode;
            if(panel!=null)panel.SetActive(online);
            if(!online)return;
            if(string.IsNullOrWhiteSpace(setup.homeTeamInputField.text))setup.homeTeamInputField.SetTextWithoutNotify("Home");
            if(string.IsNullOrWhiteSpace(setup.awayTeamInputField.text))setup.awayTeamInputField.SetTextWithoutNotify("Away");
            GuardLocalSave();hostButton.onClick.AddListener(Host);joinButton.onClick.AddListener(Join);
            leaveButton.onClick.AddListener(Leave);copyButton.onClick.AddListener(CopyLobbyCode);
            if(OnlineFrontendSession.Current!=null)Bind(OnlineFrontendSession.Current);
            Refresh();
        }
        private void GuardLocalSave()=>setup.BindOnlineSetupConfirmation(_=>Show("Host or join an online session first."));
        private void Show(string message){if(connectionStatus!=null)connectionStatus.text=message;}
        public void Host()
        {
            if(!online || OnlineFrontendSession.Current!=null)return;
            if(!setup.TryCaptureOnlineSetupSettings(out var settings,out var error)){Show(error);return;}
            try{var session=Instantiate(sessionPrefab);Bind(session);session.Host(settings,relayOnly!=null && relayOnly.isOn);}
            catch(Exception connectionError){Show(connectionError.Message);bound?.Close();}
        }
        public void Join()
        {
            if(!online || OnlineFrontendSession.Current!=null)return;
            string code=(lobbyCode.text??"").Trim();
            if(!OnlineFrontendSessionPolicy.ValidLobbyCode(code)){Show("Enter the 32-character lobby code shared by the host.");return;}
            pendingGuestSelection=setup.CaptureOnlineSetupDraft();
            try{var session=Instantiate(sessionPrefab);Bind(session);session.Join(code,relayOnly!=null && relayOnly.isOn);}
            catch(Exception connectionError){Show(connectionError.Message);bound?.Close();}
        }
        private void Bind(OnlineFrontendSession session)
        {
            if(bound!=null)bound.ClientAvailable-=Connected;
            bound=session;bound.ClientAvailable+=Connected;
            if(bound.Client!=null)Connected(bound.Client);
        }
        private void Connected(OnlineSetupClient client)
        {
            view.Configure(client);
            if(pendingGuestSelection==null)return;
            seededClient=client;seededClient.Changed+=SeedGuestSelection;SeedGuestSelection();
        }
        private void SeedGuestSelection()
        {
            if(seededClient?.Capture()==null || pendingGuestSelection==null)return;
            var proposed=pendingGuestSelection;pendingGuestSelection=null;
            var client=seededClient;client.Changed-=SeedGuestSelection;seededClient=null;
            client.Edit(proposed);view.RefreshSelections();
        }
        public void Leave()
        {
            if(bound!=null){bound.Close();view.Unbind();GuardLocalSave();}
        }
        public void CopyLobbyCode()
        {if(bound?.LobbyCode!=null)GUIUtility.systemCopyBuffer=bound.LobbyCode;}
        private void LateUpdate(){if(online)Refresh();}
        private void Refresh()
        {
            var current=OnlineFrontendSession.Current;
            if(bound==null && current!=null)Bind(current);
            bool idle=current==null;
            hostButton.interactable=joinButton.interactable=idle && sessionPrefab!=null;
            leaveButton.interactable=!idle;copyButton.interactable=current?.LobbyCode!=null;
            lobbyCode.readOnly=!idle;
            if(current?.LobbyCode!=null && lobbyCode.text!=current.LobbyCode)lobbyCode.SetTextWithoutNotify(current.LobbyCode);
            if(current?.Client==null)setup.createGameButton.interactable=false;
            if(current==null)
            {
                if(!ReferenceEquals(bound,null)){view.Unbind();bound=null;GuardLocalSave();Show("Host a private session, or enter a lobby code to join.");}
                return;
            }
            var latency=current.Latency;string role=current.Client==null?"":current.Client.Team==OnlineTeam.Home?" You manage Home.":" You manage Away.";
            Show(current.Status+role+(latency.samples>0?" Network "+latency.lastMilliseconds.ToString("F0")+" ms (p95 "+latency.p95Milliseconds.ToString("F0")+" ms).":""));
        }
        private void OnDestroy()
        {
            if(bound!=null)bound.ClientAvailable-=Connected;
            if(seededClient!=null)seededClient.Changed-=SeedGuestSelection;
            // Leaving setup before agreement cancels its native lifetime; agreed
            // sessions remain alive for the scene-transition coordinator.
            if(bound!=null && bound.AgreedSettings==null)bound.Close();
        }
    }
}
