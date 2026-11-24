using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;

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
        public Button loadMatchButton;
        public GameObject loadMatchPanel;
        public TMP_Text checkpointSummary;
        public Button previousCheckpoint, nextCheckpoint, hostCheckpoint, cancelCheckpoint;
        private IReadOnlyList<OnlineCheckpointSummary> checkpoints;
        private OnlineCheckpointStore checkpointStore;
        private int selectedCheckpoint, skippedCheckpoints;
        private OnlineFrontendSession hostedSession;
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
            lobbyCode.onValueChanged.AddListener(UpdateJoinAvailability);
            UpdateJoinAvailability(lobbyCode.text);
            if (roomFailure != null)
            {
                status.text = roomFailure;
                roomFailure = null;
            }
            loadMatchButton?.onClick.AddListener(OpenLoadMatch);
            previousCheckpoint?.onClick.AddListener(PreviousCheckpoint);
            nextCheckpoint?.onClick.AddListener(NextCheckpoint);
            hostCheckpoint?.onClick.AddListener(HostSelectedCheckpoint);
            cancelCheckpoint?.onClick.AddListener(CloseLoadMatch);
        }

        private void OnDisable()
        {
            lobbyCode.onValueChanged.RemoveListener(UpdateJoinAvailability);
            loadMatchButton?.onClick.RemoveListener(OpenLoadMatch);
            previousCheckpoint?.onClick.RemoveListener(PreviousCheckpoint);
            nextCheckpoint?.onClick.RemoveListener(NextCheckpoint);
            hostCheckpoint?.onClick.RemoveListener(HostSelectedCheckpoint);
            cancelCheckpoint?.onClick.RemoveListener(CloseLoadMatch);
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
            if (hostedSession?.LobbyCode == null || proceeding || hostedSession.HostingResume) return;
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
            continueButton.gameObject.SetActive(hosting && !hostedSession.HostingResume);
            if (loadMatchButton != null)
            {
                loadMatchButton.interactable = !hosting || hostedSession.HostingResume;
                var label = loadMatchButton.GetComponentInChildren<TMP_Text>(true);
                if (label != null) label.text = hosting && hostedSession.HostingResume ? "Cancel Load" : "Load Match";
            }
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
            if (hostedSession != null || SessionIsClosing() || loadMatchPanel == null) return;
            try
            {
                checkpointStore ??= new OnlineCheckpointStore();
                checkpoints = checkpointStore.Browse(out skippedCheckpoints);
                selectedCheckpoint = 0;
                loadMatchPanel.SetActive(true);
                RefreshCheckpoint();
            }
            catch (System.Exception error) when (error is System.IO.IOException or System.UnauthorizedAccessException)
            {
                status.text = "Could not read online saves on this device.";
            }
        }

        private void PreviousCheckpoint() { selectedCheckpoint--; RefreshCheckpoint(); }
        private void NextCheckpoint() { selectedCheckpoint++; RefreshCheckpoint(); }
        private void CloseLoadMatch() => loadMatchPanel.SetActive(false);

        private void RefreshCheckpoint()
        {
            selectedCheckpoint = Mathf.Clamp(selectedCheckpoint, 0, Mathf.Max(0, checkpoints.Count - 1));
            previousCheckpoint.interactable = selectedCheckpoint > 0;
            nextCheckpoint.interactable = selectedCheckpoint + 1 < checkpoints.Count;
            hostCheckpoint.interactable = checkpoints.Count > 0;
            checkpointSummary.text = checkpoints.Count == 0 ? "No compatible online saves were found."
                : $"{selectedCheckpoint + 1} of {checkpoints.Count}\n{checkpoints[selectedCheckpoint].Description}";
            if (skippedCheckpoints > 0)
                checkpointSummary.text += $"\n{skippedCheckpoints} unsupported or damaged save(s).";
        }

        private void HostSelectedCheckpoint()
        {
            if (checkpoints == null || checkpoints.Count == 0 || hostedSession != null || SessionIsClosing()) return;
            try
            {
                hostedSession = Instantiate(sessionPrefab);
                hostedSession.HostCheckpoint(checkpoints[selectedCheckpoint].Slot);
                CloseLoadMatch();
            }
            catch (System.Exception error) when (error is System.IO.IOException or System.UnauthorizedAccessException
                or Newtonsoft.Json.JsonException or System.ArgumentException or System.InvalidOperationException
                or System.FormatException or System.OverflowException)
            {
                hostedSession?.Close();
                checkpointSummary.text = "Could not open this online save. It may have been removed, damaged or made by an incompatible build.";
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
