using System.Collections;
using System.Linq;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CounterAttack.Online
{
    [DefaultExecutionOrder(-32000)]
    public sealed class OnlineRoomStartup : MonoBehaviour
    {
        public const string SceneName = "Room";
        [Tooltip("Explicit scene-owned authority roots. Never include designer-owned Stats or personal cameras.")]
        public GameObject[] authorityRoots;
        [SerializeField] private OnlineRoomPointers teamPointers;
        public bool GuestConfigured { get; private set; }
        public OnlineGroundPassBoard Board { get; private set; }
        public string Failure { get; private set; }
        private bool nativeOnline;

        private void Awake()
        {
            nativeOnline = gameObject.scene.name == SceneName && OnlineRoomSession.Current?.HasInitialData == true;
            if (!OnlineRoomSession.IsGuest) return;
            var match = GetComponentInRoom<MatchManager>();
            var input = GetComponentInRoom<GameInputManager>();
            var instructions = GetComponentInRoom<GameDebugMonitor>();
            foreach (var root in authorityRoots)
            {
                if (nativeOnline && (root == match.gameObject || root == match.hexGrid.gameObject
                    || root == match.playerTokenManager.gameObject || root == match.ball.gameObject
                    || root == input.gameObject || root == instructions.gameObject))
                    continue;
                if (root != null) root.SetActive(false);
            }
            GuestConfigured = true;
        }

        private IEnumerator WatchRoom(OnlineGroundPassBoard board, PauseMenuManager menu)
        {
            var session = OnlineFrontendSession.Current;
            foreach (var button in menu.pausePanel.GetComponentsInChildren<Button>(true))
                if (button != menu.resumeButton && button != menu.backToMainMenuButton
                    && button.GetComponent<RulesViewerOpener>() == null)
                    button.gameObject.SetActive(false);
            menu.backToMainMenuButton.onClick.AddListener(session.LeaveSpectating);
            menu.backToMainMenuButton.GetComponentInChildren<TMP_Text>(true).text = "Leave";
            bool localMenu = false;
            menu.resumeButton.onClick.AddListener(() => localMenu = false);
            menu.resumeButton.GetComponentInChildren<TMP_Text>(true).text = "Back to match";
            OnlineGroundPassState state = null;
            JObject displayed = null;
            while (session != null)
            {
                var publication = session.SpectatorPublication;
                if (publication != displayed && (string)publication?["stage"] == "room")
                {
                    state = OnlineSpectatorRoomProjection.Read(publication, board.match.playerTokenManager);
                    board.DisplaySpectator(state, publication);
                    displayed = publication;
                }
                if (Input.GetKeyUp(KeyCode.Escape) && !RoomInputOwnership.ReservePauseEscape)
                {
                    if (RoomInputOwnership.CardOpen) RoomInspectionCard.Instance.Close();
                    else localMenu = !localMenu;
                }
                bool paused = state?.isSharedPaused == true;
                bool held = state?.connectionHeld == true;
                bool shared = paused || held;
                bool show = !EndGamePanelManager.IsVisible && (shared || localMenu || session.Error != null);
                if (show && !menu.pausePanel.activeSelf)
                    menu.pausePanel.transform.SetAsLastSibling();
                menu.pausePanel.SetActive(show);
                menu.resumeButton.interactable = !shared && session.Error == null;
                menu.pauseSaveStatusText.gameObject.SetActive(show);
                menu.pauseSaveStatusText.text = session.Error ?? (held ? "Waiting for the managers to reconnect"
                    : paused ? "The managers paused the match" : "Spectating the match");
                yield return null;
            }
        }

        private IEnumerator Start()
        {
            if (!nativeOnline) yield break;

            var match = GetComponentInRoom<MatchManager>();
            bool restoring = OnlineRoomSession.Current.IsResuming && !OnlineRoomSession.IsGuest;
            bool Initialized() => match != null && match.hexGrid.IsGridInitialized() && match.ball.GetCurrentHex() != null
                && (restoring ? match.RuntimeSnapshotRestored : match.playerTokenManager.allTokens.Count == 22
                    && match.currentState == MatchManager.GameState.KickOffSetup);
            double deadline = Time.realtimeSinceStartupAsDouble + 30;
            while (!Initialized())
            {
                if (match?.RuntimeSnapshotRestoreError != null || Time.realtimeSinceStartupAsDouble >= deadline)
                {
                    Failure = match?.RuntimeSnapshotRestoreError ?? "Room did not finish initializing.";
                    yield break;
                }
                yield return null;
            }

            var board = gameObject.AddComponent<OnlineGroundPassBoard>();
            board.match = match;
            board.grid = match.hexGrid;
            board.cells = board.grid.cells.Cast<HexCell>().ToArray();
            board.tokens = match.playerTokenManager.CaptureOnlineRoomRoster();
            board.displayedTokens = board.tokens.Select(token => token.transform).ToArray();
            board.displayedBall = match.ball.transform;
            board.liveFullRoom = true;
            board.continuousManualSession = true;
            board.usesNativeRoom = true;
            board.localTeam = OnlineRoomSession.IsGuest ? OnlineFrontendSessionPolicy.GuestTeam : OnlineFrontendSessionPolicy.HostTeam;
            board.boardCamera = Camera.main;
            if (teamPointers != null)
            {
                board.pointers = teamPointers;
                teamPointers.Bind(board);
                teamPointers.gameObject.SetActive(true);
            }
            board.liveScoreboard = GetComponentInRoom<ScoreboardManager>();
            board.instructionMonitor = GetComponentInRoom<GameDebugMonitor>();
            match.OnlineInstructionMonitor = board.instructionMonitor;
            match.OnlineEventBanner = GetComponentInRoom<MatchEventBannerController>();
            match.OnlinePitchLines = GetComponentInRoom<PitchLines>();
            board.boardInput = gameObject.AddComponent<OnlineBoardInput>();
            board.boardInput.board = board;
            board.rollPanels = gameObject.AddComponent<OnlineRoomRollPanels>();
            if (OnlineRoomSession.IsSpectator)
            {
                board.rollPanels.BindRoom(board, GetComponentInRoom<DuelResolutionPanelController>(),
                    GetComponentInRoom<HeaderChallengePanelController>(), GetComponentInRoom<StoppageActionsPanelController>());
                Board = board;
                yield return WatchRoom(board, GetComponentInRoom<PauseMenuManager>());
                yield break;
            }
            board.controls = gameObject.AddComponent<OnlineGroundPassControls>();
            board.rollPanels.BindRoom(board, GetComponentInRoom<DuelResolutionPanelController>(),
                GetComponentInRoom<HeaderChallengePanelController>(), GetComponentInRoom<StoppageActionsPanelController>());
            match.OnlineRollPanels = board.rollPanels;
            board.nativePause = gameObject.AddComponent<OnlineNativeRoomPause>();
            board.nativePause.Bind(board, GetComponentInRoom<PauseMenuManager>(), GetComponentInRoom<SubstitutionMenuView>());
            board.nativeShootout = GetComponentInRoom<PenaltyShootoutOrderPanelController>();
            match.coinTossPanel?.BindNativeOnlineControls(board.controls);
            Board = board;
            Debug.Log($"[Online Room] Bound {(OnlineRoomSession.IsGuest ? "guest" : "host")} in {gameObject.scene.path}");
        }

        internal void StopRules()
        {
            foreach (var root in authorityRoots)
            {
                if (root == null) continue;
                foreach (var behaviour in root.GetComponents<MonoBehaviour>())
                {
                    behaviour.StopAllCoroutines();
                    behaviour.enabled = false;
                }
            }
        }

        private T GetComponentInRoom<T>() where T : Component => gameObject.scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<T>(true)).FirstOrDefault();
    }
}
