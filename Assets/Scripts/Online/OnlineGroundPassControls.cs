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
        public Dropdown legalCommands;
        public Button commandButton;
        private string[] commandOptions=Array.Empty<string>();
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
            if (commandButton != null) commandButton.onClick.AddListener(ExecuteSelectedCommand);
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

        private static string CommandLabel(string action) => action switch
        {
            "ground.start"=>"Start ground pass", "long.start"=>"Start long pass", "long.roll"=>"Roll",
            "long.keeper.decline"=>"Keep goalkeeper in position", "long.keeper.claim"=>"Claim long pass",
            "movement.start"=>"Start movement", "movement.forfeit"=>"Finish this team's movement",
            "high.start"=>"Start high pass", "high.roll"=>"Roll high pass", "high.skip"=>"Keep players in position",
            "high.keeper.stay"=>"Goalkeeper challenges from here", "high.keeper.decline"=>"Decline goalkeeper challenge",
            "header.control"=>"Bring the ball down", "header.control.roll"=>"Roll ball control",
            "header.free"=>"Take a free header", "header.confirm"=>"Confirm header nominations",
            "header.roll"=>"Roll header challenge", "header.pass"=>"Choose a headed pass",
            "header.backpass.hands"=>"Keeper catches headed back-pass", "header.backpass.feet"=>"Keeper receives at feet",
            "header.backpass.roll"=>"Roll keeper catch",
            "header.interception"=>"Roll headed-pass interception",
            "keeper.quick"=>"Choose quick goalkeeper throw", "keeper.quick.start"=>"Aim goalkeeper throw",
            "keeper.final-third"=>"Take Final Third moves before distribution",
            "loose.roll"=>"Roll loose ball",
            "shot.save"=>"Roll goalkeeper save", "shot.handling"=>"Roll goalkeeper handling",
            "shot.start"=>"Select shot / snapshot", "shot.confirm"=>"Commit shot", "shot.cancel"=>"Cancel shot preview", "shot.roll"=>"Roll shooter",
            "shot.block"=>"Roll shot block",
            "kickoff.confirm"=>"Confirm kick-off setup",
            "ftp.start"=>"Start First Time Pass", "ftp.skip"=>"Skip preparation move", "ftp.roll"=>"Roll First Time Pass interception",
            "keeper.stay"=>"Keep goalkeeper in position", "keeper.pickup.hands"=>"Pick up the ball", "keeper.pickup.feet"=>"Keep the ball at feet",
            "final-third.forfeit"=>"Finish this team's Final Third moves", "final-third.drop"=>"Drop the ball and move",
            "final-third.kick"=>"Take goalkeeper kick", "final-third.pass"=>"Take goal kick ground pass",
            _=>action.Replace("long.target:","Preview / confirm long pass to ")
                .Replace("long.keeper.move:","Move goalkeeper to ").Replace("ground.target:","Preview / confirm ground pass to ")
                .Replace("movement.target:","Move to ").Replace("movement.player:home:","Select Home #").Replace("movement.player:away:","Select Away #")
                .Replace("final-third.target:","Final Third move to ").Replace("final-third.player:home:","Select Home #").Replace("final-third.player:away:","Select Away #")
                .Replace("high.target:","Preview / confirm high pass to ").Replace("high.move:","High pass move to ")
                .Replace("high.player:home:","Select Home #").Replace("high.player:away:","Select Away #")
                .Replace("high.keeper.move:","Goalkeeper challenge at ").Replace("header.control.player:home:","Control with Home #")
                .Replace("header.control.player:away:","Control with Away #")
                .Replace("header.nominate:home:","Nominate Home #").Replace("header.nominate:away:","Nominate Away #")
                .Replace("header.remove:home:","Remove Home #").Replace("header.remove:away:","Remove Away #")
                .Replace("header.target:","Headed pass to ").Replace("header.goal:","Head at goal to ")
                .Replace("shot.target:","Aim at goal to ").Replace("shot.move:","Move snapshot defender to ")
                .Replace("shot.player:home:","Select Home defender #").Replace("shot.player:away:","Select Away defender #")
                .Replace("kickoff.player:home:","Arrange Home #").Replace("kickoff.player:away:","Arrange Away #")
                .Replace("kickoff.taker:home:","Kick off with Home #").Replace("kickoff.taker:away:","Kick off with Away #")
                .Replace("kickoff.move:","Kick-off setup move to ")
                .Replace("keeper.move:","Free goalkeeper move to ")
                .Replace("ftp.target:","Preview / confirm First Time Pass to ").Replace("ftp.move:","Preparation move to ")
                .Replace("ftp.player:home:","Select Home #").Replace("ftp.player:away:","Select Away #")
                .Replace("ground.roll","Interception roll").Replace(':',',')
        };
        public void ExecuteSelectedCommand()
        {
            if(legalCommands==null || legalCommands.value<0 || legalCommands.value>=commandOptions.Length) return;
            Gameplay(commandOptions[legalCommands.value]);
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
            var options=state?.legalActions ?? Array.Empty<string>();
            if(legalCommands!=null && !options.SequenceEqual(commandOptions))
            {
                string selected=legalCommands.value>=0 && legalCommands.value<commandOptions.Length?commandOptions[legalCommands.value]:null;
                commandOptions=options.ToArray();legalCommands.ClearOptions();
                legalCommands.AddOptions(commandOptions.Select(CommandLabel).ToList());
                legalCommands.SetValueWithoutNotify(Math.Max(0,Array.IndexOf(commandOptions,selected)));
            }
            if(commandButton!=null) commandButton.interactable=CanAct && commandOptions.Length>0;
            if(legalCommands!=null) legalCommands.interactable=CanAct && commandOptions.Length>0;
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
