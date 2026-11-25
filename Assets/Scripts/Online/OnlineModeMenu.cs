using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Linq;
using System;
using System.Collections;

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
        public CanvasGroup mainCard;
        public GameObject roleCard;
        public Button awayRoleButton, spectatorRoleButton;
        public TMP_Text roleStatus;
        public Button loadMatchButton;
        private static System.Guid? pendingCheckpoint;
        private OnlineFrontendSession hostedSession;
        private OnlineFrontendSession joiningSession;
        private IEnumerator roleLookup;
        private Coroutine findingRoles;
        private OnlineMatchRoleOptions roleOptions;
        private string roleCode;
        private bool joiningAway;
        private bool proceeding;
        private string lastHostStatus;

        private static string pendingJoinCode;
        private static string roomFailure;

        internal static void ReturnFromFailedRoom(string message)
        {
            roomFailure = message;
            SceneManager.LoadScene(SceneName);
        }

        private void OnEnable()
        {
            lobbyCode.characterLimit = OnlineSpectatorAdmission.CodeLength;
            lobbyCode.onValueChanged.AddListener(UpdateJoinAvailability);
            UpdateJoinAvailability(lobbyCode.text);
            if (roomFailure != null)
            {
                status.text = roomFailure;
                roomFailure = null;
            }
            loadMatchButton?.onClick.AddListener(OpenLoadMatch);
        }

        private void Start()
        {
            var selected = pendingCheckpoint;
            pendingCheckpoint = null;
            if (selected.HasValue) HostSelectedCheckpoint(selected.Value);
        }

        public static void OpenCheckpointLobby(System.Guid slot)
        {
            pendingCheckpoint = slot;
            SceneManager.LoadScene(SceneName);
        }

        private void OnDisable()
        {
            lobbyCode.onValueChanged.RemoveListener(UpdateJoinAvailability);
            loadMatchButton?.onClick.RemoveListener(OpenLoadMatch);
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
            if (hostedSession?.Client?.Connected != true || proceeding || hostedSession.HostingResume) return;
            proceeding = true;
            OpenSetup(null);
        }

        public void CopyHostCode()
        {
            if(hostedSession?.LobbyCode is { } code)GUIUtility.systemCopyBuffer=code;
        }

        private void Update()
        {
            if (joiningSession != null)
            {
                if (joiningAway && joiningSession.Client?.Connected == true)
                {
                    proceeding = true;
                    OpenSetup(roleCode);
                    return;
                }
                var stage = (string)joiningSession.SpectatorPublication?["stage"];
                roleStatus.text = joiningSession.Error ?? (stage switch
                {
                    "lobby" => "Spectating the lobby",
                    "setup" => "Spectating match setup",
                    "draft" => "Spectating Draft",
                    "room" => "Spectating the match",
                    _ => joiningSession.Status
                });
            }
            bool hosting=hostedSession!=null;
            var code=hosting?hostedSession.LobbyCode:null;
            hostCode.gameObject.SetActive(hosting);
            copyButton.gameObject.SetActive(hosting);
            continueButton.gameObject.SetActive(hosting && !hostedSession.HostingResume);
            if (loadMatchButton != null)
            {
                loadMatchButton.interactable = !hosting || hostedSession.HostingResume;
                var label = loadMatchButton.GetComponentInChildren<TMP_Text>(true);
                if (label != null) label.text = hosting && hostedSession.HostingResume ? "Cancel Load" : "Load Match";
            }
            copyButton.interactable=code!=null;
            continueButton.interactable=hostedSession?.Client?.Connected==true;
            if(!hosting)return;
            if(hostCode.text!=(code??string.Empty))hostCode.SetTextWithoutNotify(code??string.Empty);
            var hostStatus = hostedSession.Error ?? hostedSession.Status;
            if (hostedSession.Error == null && hostedSession.SpectatorError != null)
                hostStatus += "\nSpectate unavailable. " + hostedSession.SpectatorError;
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
            DisposeRoleLookup();
            if(!proceeding && joiningSession!=null && !joiningSession.LaunchingRoom)joiningSession.Close();
            if(!proceeding && hostedSession!=null && !hostedSession.LaunchingRoom)hostedSession.Close();
        }

        private void OpenLoadMatch()
        {
            if (hostedSession != null && hostedSession.HostingResume)
            {
                hostedSession.Close();
                lastHostStatus = null;
                status.text = "Closing the resume lobby…";
                return;
            }
            if (hostedSession != null || SessionIsClosing()) return;
            CreateLoadRoomManager.OpenOnlineBrowser();
        }

        private void HostSelectedCheckpoint(System.Guid slot)
        {
            if (hostedSession != null || SessionIsClosing()) return;
            try
            {
                hostedSession = Instantiate(sessionPrefab);
                hostedSession.HostCheckpoint(slot);
            }
            catch (System.Exception error) when (error is System.IO.IOException or System.UnauthorizedAccessException
                or Newtonsoft.Json.JsonException or System.ArgumentException or System.InvalidOperationException
                or System.FormatException or System.OverflowException)
            {
                hostedSession?.Close();
                status.text = "Could not open this online save. It may have been removed, damaged or made by an incompatible build.";
            }
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
            if (roleCard != null && roleCard.activeSelf) return;
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

            if (roleCard == null || !OnlineSpectatorAdmission.TryNormalizeCode(code, out _))
            {
                OpenSetup(code);
                return;
            }
            roleCode = code;
            SetRoleCardVisible(true);
            SetRoleOptions(null);
            roleStatus.text = "Checking Match Code…";
            findingRoles = StartCoroutine(FindRoles(code));
        }

        private IEnumerator FindRoles(string code)
        {
            yield return null;
            roleLookup = OnlineMatchRoleLookup.Find(code, SetRoleOptions);
            string error = null;
            while (true)
            {
                bool advance = false;
                try { advance = roleLookup.MoveNext(); }
                catch (Exception failure) { error = failure.Message; }
                if (!advance) break;
                yield return roleLookup.Current;
            }
            DisposeRoleLookup();
            findingRoles = null;
            if (error != null) roleStatus.text = error;
        }

        private void SetRoleOptions(OnlineMatchRoleOptions options)
        {
            roleOptions = options;
            awayRoleButton.gameObject.SetActive(options?.CanJoinAway == true);
            spectatorRoleButton.gameObject.SetActive(options?.CanSpectate == true);
            if (options == null) return;
            roleStatus.text = !options.Found ? "No active match found for this code."
                : !options.CanJoinAway && !options.CanSpectate ? "This match has no open roles."
                : !options.CanSpectate ? "Spectating is unavailable for this match." : string.Empty;
        }

        public void SelectAway() => SelectRole(away: true);
        public void SelectSpectator() => SelectRole(away: false);

        private void SelectRole(bool away)
        {
            if (joiningSession != null || roleOptions == null || sessionPrefab == null
                || (away ? !roleOptions.CanJoinAway : !roleOptions.CanSpectate)) return;
            try
            {
                joiningAway = away;
                joiningSession = Instantiate(sessionPrefab);
                if (away) joiningSession.Join(roleCode);
                else joiningSession.Watch(roleCode);
                SetRoleOptions(null);
            }
            catch (Exception failure)
            {
                roleStatus.text = failure.Message;
                joiningSession?.Close();
                joiningSession = null;
            }
        }

        public void CancelRoleChoice()
        {
            if (findingRoles != null) StopCoroutine(findingRoles);
            findingRoles = null;
            DisposeRoleLookup();
            joiningSession?.Close();
            joiningSession = null;
            roleCode = null;
            SetRoleCardVisible(false);
        }

        private void DisposeRoleLookup()
        {
            (roleLookup as IDisposable)?.Dispose();
            roleLookup = null;
        }

        private void SetRoleCardVisible(bool visible)
        {
            roleCard.SetActive(visible);
            mainCard.alpha = visible ? 0 : 1;
            mainCard.interactable = mainCard.blocksRaycasts = !visible;
        }

        public void Back()
        {
            if (roleCard != null && roleCard.activeSelf) CancelRoleChoice();
            else SceneManager.LoadScene("MainMenu");
        }

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
