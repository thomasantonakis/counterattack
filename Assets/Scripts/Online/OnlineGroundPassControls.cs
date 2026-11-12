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
        }

        public void Configure(OnlineTeam localTeam)
        { team = localTeam; connected = true; Refresh(); }

        public void Display(OnlineGroundPassState publication)
        {
            // Detach the displayed decision from transport/caller mutation.
            state = publication == null ? null : JsonUtility.FromJson<OnlineGroundPassState>(JsonUtility.ToJson(publication));
            Refresh();
        }

        public void SetConnected(bool value)
        { connected = value; if (!value) pending = false; Refresh(); }

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
            pauseButton.interactable = connected && !pending && state != null && !state.isSharedPaused;
            resumeButton.interactable = connected && !pending && state?.isSharedPaused == true && state.pauseRequester == team.ToString();
        }

        public void StartPass() => Gameplay(OnlineGroundPassAuthority.StartAction);
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
