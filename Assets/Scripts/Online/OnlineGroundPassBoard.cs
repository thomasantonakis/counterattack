using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace CounterAttack.Online
{
    // Every object is authored in the proof scene. Guest authorityRoot stays inactive.
    public sealed class OnlineGroundPassBoard : MonoBehaviour
    {
        public GameObject authorityRoot;
        public MatchManager match;
        public HexGrid grid;
        public HexCell[] cells;
        public PlayerToken[] tokens;
        public Vector3Int[] initialCoordinates;
        public Transform[] displayedTokens;
        public Transform displayedBall;
        public Camera boardCamera;
        public Text statusText;
        public GameObject localStatsPanel;
        public GameObject sharedPausePanel, requesterPauseMessage, passivePauseMessage;
        public OnlineTeam localTeam = OnlineTeam.Home;
        public OnlineGroundPassControls controls;
        private MatchManager previousMatch;
        private bool ownsMatch;

        public void InitializeAuthority()
        {
            localTeam = OnlineTeam.Away;
            previousMatch = MatchManager.Instance;
            MatchManager.Instance = match; ownsMatch = true;
            match.gameData = new MatchManager.GameData();
            match.gameData.gameSettings.homeTeamName = "Online proof Home";
            match.gameData.gameSettings.awayTeamName = "Online proof Away";
            match.difficulty_level = 2;
            match.currentState = MatchManager.GameState.AnyOtherScenario;
            match.homeTeamDirection = MatchManager.TeamAttackingDirection.LeftToRight;
            match.awayTeamDirection = MatchManager.TeamAttackingDirection.RightToLeft;
            match.teamInAttack = MatchManager.TeamInAttack.Home;
            match.isClockRunning = true;
            grid.cells = new HexCell[grid.GridWidth, grid.GridHeight];
            foreach (var cell in cells)
            {
                grid.cells[cell.coordinates.x + grid.GridWidth / 2, cell.coordinates.z + grid.GridHeight / 2] = cell;
                cell.occupyingToken = null; cell.isAttackOccupied = cell.isDefenseOccupied = false;
            }
            PlayerToken.SetBallReference(match.ball);
            match.playerTokenManager.allTokens = tokens.ToList();
            for (var i = 0; i < tokens.Length; i++)
            {
                var hex = grid.GetHexCellAt(initialCoordinates[i]);
                tokens[i].isAttacker = tokens[i].isHomeTeam;
                if (tokens[i].jerseyNumber == 1)
                {
                    tokens[i].ConvertToGK();
                    tokens[i].saving = tokens[i].aerial = tokens[i].handling = 4;
                }
                tokens[i].SetCurrentHex(hex);
                tokens[i].transform.position = hex.GetHexCenter();
                hex.isAttackOccupied = tokens[i].isAttacker; hex.isDefenseOccupied = !tokens[i].isAttacker;
            }
            authorityRoot.SetActive(true);
            match.SetLastToken(tokens[0]); match.attackHasPossession = true;
            match.ball.PlaceAtCell(tokens[0].currentHex);
            // Real outcome entry point; managers remain disabled to suppress unrelated
            // startup/input/AI loops. Explicit ground-pass coroutines run on active hosts.
            match.BroadcastSafeEndofMovementPhase(countMovementAction: false, triggerFinalThird: false);
        }

        public void Display(OnlineGroundPassState state, string role)
        {
            if (controls != null) controls.Display(state);
            if (sharedPausePanel != null) sharedPausePanel.SetActive(state.isSharedPaused);
            var requester = state.isSharedPaused && state.pauseRequester == localTeam.ToString();
            if (requesterPauseMessage != null) requesterPauseMessage.SetActive(requester);
            if (passivePauseMessage != null) passivePauseMessage.SetActive(state.isSharedPaused && !requester);
            foreach (var token in state.tokens)
            {
                var index = Array.FindIndex(tokens, t => (t.isHomeTeam ? "home:" : "away:") + t.jerseyNumber == token.id);
                EOSProofCheck.Require(index >= 0, "Published roster must match the authored board");
                if (token.onBoard) displayedTokens[index].position = Center(token.x, token.z) + Vector3.up * .5f;
            }
            if (state.ballOnBoard) displayedBall.position = Center(state.ballX, state.ballZ) + Vector3.up * 1.2f;
            if (statusText != null) statusText.text = role + " | " + state.stage + " | " + state.owner
                + "\nState " + state.publicationSequence + " | Rolls " + state.rollSequence
                + " | Clock " + state.regulationSeconds.ToString("F1")
                + (state.connectionHeld ? "\nConnection interrupted - waiting for player" : state.isSharedPaused ? "\nPaused by " + state.pauseRequester : "")
                + "\n" + string.Join("  ", state.tokens.Where(t => t.passesAttempted > 0 || t.interceptionsAttempted > 0)
                    .Select(t => t.id + " passes " + t.passesCompleted + "/" + t.passesAttempted + " interceptions " + t.interceptionsMade));
        }

        private Vector3 Center(int x, int z) => grid.GetHexCenterForCoordinates(new Vector3Int(x, 0, z));

        public void DisplayPresentation(OnlineGroundPassPresentation frame)
        {
            // Trusted, validated presentation values only; completion never calls rules.
            displayedBall.position = new Vector3(frame.ballX, frame.ballY, frame.ballZ);
        }

        private void Update()
        {
            // Personal presentation only. No keys route to rule/input managers.
            if (boardCamera != null)
            {
                boardCamera.orthographicSize = Mathf.Clamp(boardCamera.orthographicSize - Input.mouseScrollDelta.y, 4f, 40f);
                if (Input.GetMouseButton(1)) boardCamera.transform.position += new Vector3(-Input.GetAxis("Mouse X"), 0, -Input.GetAxis("Mouse Y"));
            }
            if (localStatsPanel != null && Input.GetKeyDown(KeyCode.Tab)) localStatsPanel.SetActive(!localStatsPanel.activeSelf);
        }

        private void OnDestroy()
        {
            if (!ownsMatch) return;
            // The inactive MatchManager never owns a live save. Remove its singleton
            // before teardown so its OnDestroy cannot flush through ApplicationManager.
            MatchManager.Instance = previousMatch;
            PlayerToken.SetBallReference(previousMatch != null ? previousMatch.ball : null);
        }
    }
}
