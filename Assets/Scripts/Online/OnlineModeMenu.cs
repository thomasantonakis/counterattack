using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Linq;

namespace CounterAttack.Online
{
    public sealed class OnlineModeMenu : MonoBehaviour
    {
        public const string SceneName = "OnlineModeMenu";
        public TMP_InputField lobbyCode;
        public TMP_Text status;
        public TMP_InputField hostCode;
        public Button copyButton;
        public Button continueButton;
        public Button joinButton;
        public OnlineFrontendSession sessionPrefab;
        private OnlineFrontendSession hostedSession;
        private bool proceeding;
        private string lastHostStatus;

        private static string pendingJoinCode;

        private void OnEnable()
        {
            lobbyCode.onValueChanged.AddListener(UpdateJoinAvailability);
            UpdateJoinAvailability(lobbyCode.text);
        }

        private void OnDisable()
        {
            lobbyCode.onValueChanged.RemoveListener(UpdateJoinAvailability);
        }

        public void Host()
        {
            if (hostedSession != null) return;
            if (SessionIsClosing()) return;
            if(sessionPrefab==null)return;
            try
            {
                var defaults=HostDefaults();
                hostedSession=Instantiate(sessionPrefab);
                hostedSession.Host(defaults);
            }
            catch(System.Exception error)
            {
                hostedSession?.Close();
                status.text=error.Message;
            }
        }

        public void ContinueToMatchSetup()
        {
            if (hostedSession?.LobbyCode == null || proceeding) return;
            proceeding = true;
            OpenSetup(null);
        }

        public void CopyHostCode()
        {
            if(hostedSession?.LobbyCode is { } code)GUIUtility.systemCopyBuffer=code;
        }

        private void Update()
        {
            bool hosting=hostedSession!=null;
            var code=hosting?hostedSession.LobbyCode:null;
            hostCode.gameObject.SetActive(hosting);
            copyButton.gameObject.SetActive(hosting);
            continueButton.gameObject.SetActive(hosting);
            copyButton.interactable=code!=null;
            continueButton.interactable=code!=null;
            if(!hosting)return;
            if(hostCode.text!=(code??string.Empty))hostCode.SetTextWithoutNotify(code??string.Empty);
            var hostStatus = hostedSession.Error ?? hostedSession.Status;
            if (hostStatus != lastHostStatus)
            {
                lastHostStatus = hostStatus;
                status.text = hostStatus;
            }
        }

        private void UpdateJoinAvailability(string input)
        {
            joinButton.interactable = !string.IsNullOrWhiteSpace(input);
        }

        private void OnDestroy()
        {
            if(!proceeding && hostedSession!=null)hostedSession.Close();
        }

        public static GameSettings HostDefaults()
        {
            var settings=new GameSettings
            {
                halfDuration=45,numberOfHalfs=2,tiebreaker="Extra Time & Penalties",playerAssistance=2,
                matchType="Regular",squadSize="16",draft="Regular",gkDraft="Deal",referee="Random",
                weatherConditions="Clear",fixtureLighting="MidAfternoon",ballColor="White",
                includeTabletopia=true,includeTabletopiaGK=true,homeTeamName="Home",awayTeamName="Away"
            };
            var kits=TokenKitCatalog.GetAllPresets();
            var players=kits.Where(kit=>!kit.Id.StartsWith("gk")).OrderBy(kit=>kit.Id=="088"?0:kit.Id=="021"?1:2).ToArray();
            var keepers=kits.Where(kit=>kit.Id.StartsWith("gk")).ToArray();
            foreach(var home in players)
            foreach(var away in players.Where(kit=>kit!=home))
            foreach(var homeKeeper in keepers)
            foreach(var awayKeeper in keepers.Where(kit=>kit!=homeKeeper))
            {
                var similarity=TokenKitCatalog.GetSimilarityBreakdown(home.Id,away.Id);
                if(!string.IsNullOrEmpty(CreateNewGameManager.ValidateSelectedKits(home,away,homeKeeper,awayKeeper,similarity)))continue;
                settings.homeKit=home.Id;settings.awayKit=away.Id;
                settings.homeGKKit=homeKeeper.Id;settings.awayGKKit=awayKeeper.Id;
                return OnlineSetupRules.Normalize(settings);
            }
            throw new System.InvalidOperationException("No compatible starting kits are available.");
        }

        public void Join()
        {
            if (!TryReadLobbyCode(lobbyCode.text, out var code))
            {
                status.text = OnlineFrontendSessionPolicy.LobbyCodePrompt;
                lobbyCode.ActivateInputField();
                return;
            }

            if (hostedSession != null)
            {
                status.text = "Return to the main menu to close your hosted lobby before joining.";
                return;
            }
            if (SessionIsClosing()) return;

            OpenSetup(code);
        }

        public void Back() => SceneManager.LoadScene("MainMenu");

        public void Exit() => Application.Quit();

        public static bool TryReadLobbyCode(string input, out string code)
        {
            return OnlineFrontendSessionPolicy.TryNormalizeLobbyCode(input, out code);
        }

        public static string TakeJoinCode()
        {
            var code = pendingJoinCode;
            pendingJoinCode = null;
            return code;
        }

        private bool SessionIsClosing()
        {
            if (OnlineFrontendSession.Current == null) return false;
            status.text = "The previous session is closing. Try again in a moment.";
            return true;
        }

        private static void OpenSetup(string code)
        {
            ApplicationManager.EnsureInstanceExists();
            ApplicationManager.Instance.SetSelectedRoomGameMode(ApplicationManager.OnlineGameMode);
            pendingJoinCode = code;
            SceneManager.LoadScene("CreateNewGameScene");
        }
    }
}
