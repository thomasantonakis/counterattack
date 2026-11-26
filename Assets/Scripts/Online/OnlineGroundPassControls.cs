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
        public Func<bool> LocalEscapeHandler {get;set;}
        internal Func<bool> RollEscapeHandler { get; set; }
        internal bool AcknowledgeRoll(long result)
        {
            if (!connected || pending || state == null || state.connectionHeld || state.isSharedPaused || Requested == null)
                return false;
            Control(OnlineRoomRollPanels.AcknowledgePrefix + result.ToString(CultureInfo.InvariantCulture));
            return true;
        }
        internal bool CanEditSettings => connected && !pending && state?.isSharedPaused == true && !state.connectionHeld;
        internal void ChangeSetting(OnlineRoomSettings.Field field, string value)
        { if (CanEditSettings) Control(OnlineRoomSettings.Action(field, value)); }
        public bool CanManageOwnPause=>connected && !pending && state?.isSharedPaused==true && !state.connectionHeld && state.pauseRequester==team.ToString();
        private bool OwnRequiredKeeper=>state!=null && (team==OnlineTeam.Home
            ?state.homeKeeperReplacement || state.homeForcedKeeperSubstitution || state.homeEmergencyKeeper
            :state.awayKeeperReplacement || state.awayForcedKeeperSubstitution || state.awayEmergencyKeeper);
        public bool CanManageRequiredKeeper=>connected && !pending && state?.isSharedPaused==true && !state.connectionHeld && OwnRequiredKeeper;
        public bool CanManageSubstitutions=>state?.keeperActionBlocked==true?CanManageRequiredKeeper && !(team==OnlineTeam.Home?state.homeEmergencyKeeper:state.awayEmergencyKeeper):CanManageOwnPause;
        public bool CanSubmitAction(string action)=>CanAct && state.legalActions?.Contains(action)==true;
        public void SubmitAction(string action)=>Gameplay(action);
        public bool TrySubmitAction(string action)
        {
            if(!CanSubmitAction(action) || Requested==null)return false;
            Gameplay(action);return true;
        }
        public bool CanManageShootout=>connected && !pending && state?.shootoutSelecting==true && !state.connectionHeld && !state.isSharedPaused;
        public void SwapShootout(int first,int second)=>Control("shootout.swap:"+first+":"+second);
        public void ConfirmShootout()=>Control("shootout.ready");
        public bool CanNominateKeeper=>CanManageRequiredKeeper && (team==OnlineTeam.Home?state.homeEmergencyKeeper:state.awayEmergencyKeeper);
        private OnlineControlDecision state;
        private OnlineTeam team;
        private long sequence, controlSequence;
        private bool pending, connected;

        private void Awake()
        {
            startButton?.onClick.AddListener(StartPass);
            targetButton?.onClick.AddListener(Target);
            rollButton?.onClick.AddListener(Roll);
            pauseButton?.onClick.AddListener(Pause);
            resumeButton?.onClick.AddListener(Resume);
            if (legalTargets != null) legalTargets.onValueChanged.AddListener(SelectLegalTarget);
            if (commandButton != null) commandButton.onClick.AddListener(ExecuteSelectedCommand);
        }

        public void Configure(OnlineTeam localTeam)
        { team = localTeam; connected = true; Refresh(); }

        public void Display(OnlineGroundPassState publication)
        {
            // Detach the displayed decision from transport/caller mutation.
            state = OnlineControlDecision.Capture(publication);
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
            "coin.roll"=>"Roll coin toss", "coin.kickoff"=>"Take first kick", "coin.left"=>"Choose left end / left-to-right", "coin.right"=>"Choose right end / right-to-left",
            "stoppage.roll"=>"R: Roll stoppage actions",
            "ground.start"=>"Start ground pass", "long.start"=>"Start long pass", "long.roll"=>"Roll",
            "long.keeper.decline"=>"Keep goalkeeper in position", "long.keeper.claim"=>"Claim long pass",
            "movement.start"=>"Start movement", "movement.forfeit"=>"Finish this team's movement",
            "movement.snapshot"=>"S: Take offered snapshot", "movement.snapshot.decline"=>"X: Continue without shooting",
            "high.start"=>"Start high pass", "high.roll"=>"Roll high pass", "high.skip"=>"Keep players in position",
            "high.keeper.stay"=>"Goalkeeper challenges from here", "high.keeper.decline"=>"Decline goalkeeper challenge",
            "header.control"=>"Bring the ball down", "header.control.roll"=>"Roll ball control",
            "header.free"=>"Take a free header", "header.confirm"=>"Confirm header nominations",
            "header.all"=>"A: Select all eligible jumpers and confirm",
            "header.roll"=>"Roll header challenge", "header.pass"=>"Choose a headed pass",
            "header.backpass.hands"=>"Keeper catches headed back-pass", "header.backpass.feet"=>"Keeper receives at feet",
            "header.backpass.roll"=>"Roll keeper catch",
            "header.interception"=>"Roll headed-pass interception",
            "keeper.quick"=>"Choose quick goalkeeper throw", "keeper.quick.start"=>"Aim goalkeeper throw",
            "keeper.final-third"=>"Take Final Third moves before distribution",
            "loose.roll"=>"Roll loose ball",
            "shot.save"=>"Roll goalkeeper save", "shot.handling"=>"Roll goalkeeper handling",
            "shot.keeper.hands"=>"D: Dive with hands outside box (handball if stopped)", "shot.keeper.block"=>"B: Block as an outfielder",
            "shot.decline"=>"X: Continue without the collected-ball snapshot",
            "shot.start"=>"Select shot / snapshot", "shot.confirm"=>"Commit shot", "shot.cancel"=>"Cancel shot preview", "shot.roll"=>"Roll shooter",
            "shot.block"=>"Roll shot block",
            "kickoff.confirm"=>"Confirm kick-off setup",
            "ftp.start"=>"Start First Time Pass", "ftp.skip"=>"Skip preparation move", "ftp.roll"=>"Roll First Time Pass interception",
            "dribble.finish"=>"Finish this player's dribble", "dribble.decline-nutmeg"=>"Decline nutmeg and face steals", "dribble.roll"=>"Roll ball recovery",
            "duel.nutmeg"=>"Attempt nutmeg", "duel.tackle"=>"Attempt tackle", "duel.stand"=>"Stand without tackling", "duel.roll"=>"Roll ground duel", "duel.stay"=>"Stay after ground duel",
            "duel.dive"=>"D: Goalkeeper dive with hands",
            "keeper.stay"=>"Keep goalkeeper in position", "keeper.pickup.hands"=>"Pick up the ball", "keeper.pickup.feet"=>"Keep the ball at feet",
            "throw.movement"=>"Take optional movement", "throw.now"=>"Throw now",
            "throw.feet"=>"Throw to feet", "throw.head"=>"Throw to head",
            "penalty.confirm"=>"Confirm penalty setup",
            "set-piece.skip"=>"Finish this setup phase", "set-piece.ground"=>"Set-piece ground pass",
            "set-piece.high"=>"Set-piece cross", "set-piece.long"=>"Set-piece long pass", "set-piece.shot"=>"Set-piece shot / confirm",
            "foul.leniency"=>"Roll for booking", "foul.injury"=>"Roll for injury",
            "foul.advantage"=>"Play advantage", "foul.take"=>"Take awarded restart",
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
                .Replace("throw.player:home:","Throw in with Home #").Replace("throw.player:away:","Throw in with Away #").Replace("throw.target:","Throw to head at ")
                .Replace("penalty.player:home:","Select Home #").Replace("penalty.player:away:","Select Away #").Replace("penalty.target:","Penalty setup move to ")
                .Replace("set-piece.player:home:","Select Home #").Replace("set-piece.player:away:","Select Away #").Replace("set-piece.target:","Set-piece move to ")
                .Replace("duel.target:","Reposition to ").Replace("duel.victim:home:","Nutmeg Home #").Replace("duel.victim:away:","Nutmeg Away #")
                .Replace("ground.roll","Interception roll").Replace(':',',')
        };
        public void ExecuteSelectedCommand()
        {
            if(legalCommands==null || legalCommands.value<0 || legalCommands.value>=commandOptions.Length) return;
            Gameplay(commandOptions[legalCommands.value]);
        }

        public void SetConnected(bool value)
        { connected = value; if (!value) { pending = false; SetStatus("Connection unavailable"); } Refresh(); }

        public void Reply(OnlineGroundPassMessage reply)
        {
            pending = false;
            SetStatus(reply.status);
            Refresh();
        }

        private bool CanAct => connected && !pending && state != null && !state.connectionHeld && !state.isSharedPaused
            && state.owner == team.ToString();

        internal bool CanResume => CanManageOwnPause && !state.keeperActionBlocked && state.substitutionReviewId==0;
        internal bool CanOpenSubstitutionReview => connected && !pending && state?.isSharedPaused==true
            && !state.connectionHeld && state.substitutionsAvailable && !state.keeperActionBlocked && state.substitutionReviewId==0;
        internal bool CanEditSubstitutionReview => connected && !pending && state?.isSharedPaused==true
            && !state.connectionHeld && state.substitutionReviewId>0
            && !(team==OnlineTeam.Home ? state.homeSubstitutionConfirmed : state.awaySubstitutionConfirmed);

        internal void OpenSubstitutionReview()
        {
            if (CanOpenSubstitutionReview) Control(OnlineSubstitutionReview.OpenAction);
        }

        internal void SetSubstitutionOrder(int row, int outgoing, int incoming)
        {
            if (CanEditSubstitutionReview) Control(OnlineSubstitutionReview.PickAction(state.substitutionReviewId, row, outgoing, incoming));
        }

        internal void FinishSubstitutionReview(bool confirm)
        {
            if (CanEditSubstitutionReview) Control(OnlineSubstitutionReview.ReviewAction(state.substitutionReviewId, confirm));
        }

        private void SetStatus(string message)
        {
            if (commandStatus != null) commandStatus.text = message;
        }

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
            if (startButton != null) startButton.interactable = Legal(OnlineGroundPassAuthority.StartAction);
            if (rollButton != null) rollButton.interactable = Legal(OnlineGroundPassAuthority.RollAction);
            if (targetButton != null) targetButton.interactable = CanAct && state.stage == "target";
            if (legalTargets != null) legalTargets.interactable = connected && !pending && targetOptions.Length > 0;
            if (pauseButton != null) pauseButton.interactable = connected && !pending && state != null && !state.isSharedPaused;
            if (resumeButton != null) resumeButton.interactable = CanResume;
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
        internal bool CanConfirmResume => connected && !pending && state?.resumePending == true
            && !state.connectionHeld && !(team == OnlineTeam.Home ? state.homeResumeReady : state.awayResumeReady);
        public void Resume()
        {
            if (state?.resumePending == true)
            {
                if (CanConfirmResume) Control(OnlineSharedPause.ResumeReadyAction);
                return;
            }
            Control(OnlineSharedPause.ResumeAction);
        }
        public void Substitute(int outgoing,int incoming)
        {
            if(outgoing is >=1 and <=18 && incoming is >=1 and <=18 && outgoing!=incoming)
                Control(OnlineSharedPause.SubstitutionAction(outgoing,incoming));
        }
        public void NominateKeeper(int jersey)
        {if(jersey is >=1 and <=18)Control(OnlineSharedPause.EmergencyKeeperAction(jersey));}
        public void Escape()
        {
            if(RoomInputOwnership.ReservePauseEscape)
            {
                if(RoomInputOwnership.IntroActive)RoomMatchIntro.Instance.Skip();
                return;
            }
            if(RoomInputOwnership.CardOpen){RoomInspectionCard.Instance.Close();return;}
            if(state?.introPending==true || state?.resumePending==true)return;
            if(RollEscapeHandler?.Invoke()==true || LocalEscapeHandler?.Invoke()==true)return;
            if (state?.isSharedPaused == true) Resume(); else Pause();
        }
        private double nextIntroReceipt;
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) Escape();
            if(state?.introPending!=true || RoomMatchIntro.Instance==null || RoomInputOwnership.IntroActive
                || (team==OnlineTeam.Home?state.homeIntroReady:state.awayIntroReady)
                || Time.realtimeSinceStartupAsDouble<nextIntroReceipt)return;
            nextIntroReceipt=Time.realtimeSinceStartupAsDouble+1;
            Control("intro.ready");
        }

        private void Gameplay(string action)
        {
            if (!CanAct || state.legalActions?.Contains(action) != true)
            { SetStatus("Action unavailable for this decision"); return; }
            Emit(state.Command(checked(++sequence), action));
        }

        private void Control(string action)
        {
            if (!connected || pending || state == null || action == OnlineSharedPause.ResumeAction
                && (!state.isSharedPaused || state.pauseRequester != team.ToString())
                || action == OnlineSharedPause.RequestAction && state.isSharedPaused) return;
            bool shootout=action.StartsWith("shootout.",StringComparison.Ordinal);
            if(shootout && !CanManageShootout)return;
            if(action.StartsWith("roster.substitute:",StringComparison.Ordinal)
                && (!CanManageSubstitutions || !state.substitutionsAvailable))return;
            if(action.StartsWith("keeper.nominate:",StringComparison.Ordinal) && !CanNominateKeeper)return;
            if(action==OnlineSharedPause.ResumeAction && !CanResume)return;
            if (Requested != null)
            {
                if (action is OnlineSharedPause.RequestAction or OnlineSharedPause.ResumeReadyAction or OnlineSubstitutionReview.OpenAction)
                    MenuAudioPlayer.Play(GameplayAudioPool.MenuAdvance);
                else if (action == OnlineSharedPause.ResumeAction)
                    MenuAudioPlayer.Play(GameplayAudioPool.MenuBack);
            }
            Emit(new OnlineGroundPassMessage { kind = "control", matchId = state.matchId, sequence = checked(++controlSequence),
                revision = state.sharedPauseRevision,
                decisionId = action == OnlineSharedPause.ResumeReadyAction ? "resume-ready" : shootout?"shootout-order":"shared-pause",
                actionId = action });
        }

        private void Emit(OnlineGroundPassMessage message)
        {
            if (Requested == null) { SetStatus("Connection unavailable"); return; }
            pending = true; SetStatus("Pending " + message.actionId); Refresh(); Requested(message);
        }
    }
}
