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
        public TMP_Text hostLabel;
        public Button copyButton;
        public Button joinButton;
        public OnlineFrontendSession sessionPrefab;
        private OnlineFrontendSession hostedSession;
        private bool proceeding;

        private static string pendingJoinCode;

        public void Host()
        {
            if(hostedSession!=null)
            {
                if(hostedSession.LobbyCode==null)return;
                proceeding=true;
                OpenSetup(null);
                return;
            }
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

        public void CopyHostCode()
        {
            if(hostedSession?.LobbyCode is { } code)GUIUtility.systemCopyBuffer=code;
        }

        private void Update()
        {
            if(hostCode==null || copyButton==null || hostLabel==null)return;
            bool hosting=hostedSession!=null;
            var code=hosting?hostedSession.LobbyCode:null;
            hostCode.gameObject.SetActive(hosting);
            copyButton.gameObject.SetActive(hosting);
            lobbyCode.gameObject.SetActive(!hosting);
            if(joinButton!=null)joinButton.gameObject.SetActive(!hosting);
            copyButton.interactable=code!=null;
            hostLabel.text=code!=null?"Continue to Match Setup":"Host Match";
            if(!hosting)return;
            if(hostCode.text!=(code??string.Empty))hostCode.SetTextWithoutNotify(code??string.Empty);
            status.text=hostedSession.Error??hostedSession.Status;
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
            if (SessionIsClosing()) return;
            if (!TryReadLobbyCode(lobbyCode.text, out var code))
            {
                status.text = "Enter the 32-character lobby code shared by the host.";
                lobbyCode.ActivateInputField();
                return;
            }

            OpenSetup(code);
        }

        public void Back() => SceneManager.LoadScene("MainMenu");

        public void Exit() => Application.Quit();

        public static bool TryReadLobbyCode(string input, out string code)
        {
            code = input?.Trim();
            return OnlineFrontendSessionPolicy.ValidLobbyCode(code);
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
