using System;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace CounterAttack.Online
{
    /// <summary>Authored personal controls. Only semantic intentions leave this view.</summary>
    public sealed class OnlineGroundPassControls : MonoBehaviour
    {
        public Button startButton, targetButton, rollButton, pauseButton, resumeButton;
        public InputField targetX, targetZ;
        public Text commandStatus;
        public Dropdown legalTargets;
        private string[] targetOptions = Array.Empty<string>();
        public event Action<OnlineGroundPassMessage> Requested;
        private OnlineGroundPassState state;
        private OnlineTeam team;
        private long sequence, controlSequence;
        private bool pending, connected;

        private void Awake()
        {
            startButton.onClick.AddListener(StartPass);
            targetButton.onClick.AddListener(Target);
            rollButton.onClick.AddListener(Roll);
            pauseButton.onClick.AddListener(Pause);
            resumeButton.onClick.AddListener(Resume);
            if (legalTargets != null) legalTargets.onValueChanged.AddListener(SelectLegalTarget);
        }

        public void Configure(OnlineTeam localTeam)
        { team = localTeam; connected = true; Refresh(); }

        public void Display(OnlineGroundPassState publication)
        {
            // Detach the displayed decision from transport/caller mutation.
            state = publication == null ? null : JsonUtility.FromJson<OnlineGroundPassState>(JsonUtility.ToJson(publication));
            if (state != null)
            {
                sequence = Math.Max(sequence, team == OnlineTeam.Home ? state.homeAcceptedSequence : state.awayAcceptedSequence);
                controlSequence = Math.Max(controlSequence, team == OnlineTeam.Home ? state.homePauseSequence : state.awayPauseSequence);
            }
            var choices = state?.legalActions?.Where(action => action.StartsWith("ground.target:", StringComparison.Ordinal)).ToArray() ?? Array.Empty<string>();
            if (legalTargets != null && !choices.SequenceEqual(targetOptions))
            {
                targetOptions = choices;
                legalTargets.ClearOptions();
                legalTargets.AddOptions(choices.Select(action => "Target (" + action.Substring("ground.target:".Length).Replace(':', ',') + ")").ToList());
                var selected = Array.IndexOf(choices, OnlineGroundPassAuthority.TargetAction(
                    int.TryParse(targetX.text, out var x) ? x : 0, int.TryParse(targetZ.text, out var z) ? z : 0));
                legalTargets.SetValueWithoutNotify(Math.Max(0, selected));
            }
            Refresh();
        }

        public void SetConnected(bool value)
        { connected = value; if (!value) { pending = false; commandStatus.text = "Connection unavailable"; } Refresh(); }

        public void Reply(OnlineGroundPassMessage reply)
        {
            pending = false;
            commandStatus.text = reply.status;
            Refresh();
        }

        private bool CanAct => connected && !pending && state != null && !state.connectionHeld && !state.isSharedPaused
            && state.owner == team.ToString();

        private void Refresh()
        {
            bool Legal(string id) => CanAct && state.legalActions?.Contains(id) == true;
            startButton.interactable = Legal(OnlineGroundPassAuthority.StartAction);
            rollButton.interactable = Legal(OnlineGroundPassAuthority.RollAction);
            targetButton.interactable = CanAct && state.stage == "target";
            if (legalTargets != null) legalTargets.interactable = connected && !pending && targetOptions.Length > 0;
            pauseButton.interactable = connected && !pending && state != null && !state.isSharedPaused;
            resumeButton.interactable = connected && !pending && state?.isSharedPaused == true && state.pauseRequester == team.ToString();
        }

        public void StartPass() => Gameplay(OnlineGroundPassAuthority.StartAction);
        private void SelectLegalTarget(int index)
        {
            if (index < 0 || index >= targetOptions.Length) return;
            var coordinates = targetOptions[index].Substring("ground.target:".Length).Split(':');
            if (coordinates.Length != 2 || !int.TryParse(coordinates[0], out _) || !int.TryParse(coordinates[1], out _)) return;
            targetX.SetTextWithoutNotify(coordinates[0]); targetZ.SetTextWithoutNotify(coordinates[1]);
        }
        public void Roll() => Gameplay(OnlineGroundPassAuthority.RollAction);
        public void Target()
        {
            if (!int.TryParse(targetX.text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var x)
                || !int.TryParse(targetZ.text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var z))
            { commandStatus.text = "Enter hex X and Z"; return; }
            Gameplay(OnlineGroundPassAuthority.TargetAction(x, z));
        }
        public void Pause() => Control(OnlineSharedPause.RequestAction);
        public void Resume() => Control(OnlineSharedPause.ResumeAction);
        public void Escape()
        { if (state?.isSharedPaused == true) Resume(); else Pause(); }
        private void Update()
        { if (Input.GetKeyDown(KeyCode.Escape)) Escape(); }

        private void Gameplay(string action)
        {
            if (!CanAct || state.legalActions?.Contains(action) != true)
            { commandStatus.text = "Action unavailable for this decision"; return; }
            Emit(OnlineGroundPassMessage.Command(state, checked(++sequence), action));
        }

        private void Control(string action)
        {
            if (!connected || pending || state == null || action == OnlineSharedPause.ResumeAction
                && (!state.isSharedPaused || state.pauseRequester != team.ToString())
                || action == OnlineSharedPause.RequestAction && state.isSharedPaused) return;
            Emit(new OnlineGroundPassMessage { kind = "control", matchId = state.matchId, sequence = checked(++controlSequence),
                revision = state.sharedPauseRevision, decisionId = "shared-pause", actionId = action });
        }

        private void Emit(OnlineGroundPassMessage message)
        {
            if (Requested == null) { commandStatus.text = "Connection unavailable"; return; }
            pending = true; commandStatus.text = "Pending " + message.actionId; Refresh(); Requested(message);
        }
    }
}
