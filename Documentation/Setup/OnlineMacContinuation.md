# Continue online gameplay development on Mac

Follow `PersonalMac.md` first on a fresh Mac. Use the existing Mac-local Unity,
GitHub and ignored EOS configuration; do not copy credentials from Windows.
Manually open this checkout in Unity 6000.5.3f1 before Editor operations.

## Bring over the checkpoint

```sh
cd "$HOME/Documents/Projects/Personal/counterattack"
git status --short
git fetch origin
git switch codex/online-foundation-personal-mac
git pull --ff-only origin codex/online-foundation-personal-mac
git log -3 --oneline
```

Preserve any existing Mac edits. Required gameplay checkpoint: `61e3e38`
(goalkeeper free moves and deferred pickup), preceded by `4668d13`
(goals/reset/kickoff). The next documentation commit records refreshed exports.
Windows GitHub HTTPS was temporarily unreachable during this handoff. Verify
that `git merge-base --is-ancestor 61e3e38 HEAD` succeeds before resuming; a
successful pull of an older remote branch is insufficient.

If GitHub still lacks that checkpoint, copy the prepared
`Temp/OnlineMacContinuation.bundle` from Windows to the Mac, then use its actual
location below. The bundle requires the already published base `95caf71`.

```sh
git fetch "$HOME/Downloads/OnlineMacContinuation.bundle" codex/online-foundation-personal-mac
git merge --ff-only FETCH_HEAD
git merge-base --is-ancestor 61e3e38 HEAD
```

## Build and run on Mac

With Unity manually open, stopped and scenes saved:

```sh
bash Documentation/Setup/BuildEOSStandaloneMac.sh room
bash Documentation/Setup/RunEOSStandaloneMac.sh login room
bash Documentation/Setup/RunEOSStandaloneMac.sh local room
```

For an arranged paired session, choose one role:

```sh
# Mac hosts; start the other machine's guest after the host publishes its lobby.
bash Documentation/Setup/RunEOSStandaloneMacHost.sh room manual

# Mac joins an already-published Windows/Mac host lobby.
bash Documentation/Setup/RunEOSStandaloneMac.sh guest room manual
```

Hosting is not PowerShell-only. The Mac host helper is separate from the
login/local/guest helper. Reverse-role helpers are prepared; actual paired
Windows/Mac acceptance has not run. Local reports do not prove internet play.

## Paste into the Mac Codex session

> Continue the online gameplay task from Documentation/Setup/OnlineMacContinuation.md
> and Documentation/Setup/OnlineNextSessionPrompt.md. Follow AGENTS.md and the
> unity-cli skill, resolving this Mac checkout with git rev-parse --show-toplevel.
> Verify that checkpoint 61e3e38 is present. Goalkeeper integration has ten focused
> PlayMode passes, offline kickoff has two, and goal/kickoff has six. Read the feature
> Verification.md files and retained XML; do not rerun broad suites by default.
> First Time Pass is next: only source inspection has been done, with no partial
> implementation. Add typed selection, target confirmation, attacker/defender
> preparation moves, skip, interceptions and deadlines using the real manager
> entry points. Create a dedicated OnlineFirstTimePass folder/checklist and
> meaningful focused Unity coverage; reproduce gaps before fixing when practical.
> Preserve mandatory keeper/Final Third/loose-ball priority, receipt/revision
> validation, host-only RNG and guest inactivity. Then continue dribbling/duels,
> restarts/set pieces and full-match roster/lifecycle. Keep paired/manual sessions
> deferred until I have time. Compile/test/build through the open Editor, commit
> and push verified batches. Never launch Unity, batchmode, ordinary editor_play,
> GUI automation or additive scenes. Preserve unrelated local scene/font/settings
> edits. The current Room proof is still six tokens and ends after the first
> completed ground action; it is not a complete online match. Continue independent
> work when a second-machine action is unavailable.

First Time Pass source navigation: `FirstTimePassManager.cs` already provides
`GetLegalFirstTimePassTargetsForDecisionPreview()` and
`GetValidFtpMovementDestinationsForDecision(...)`. Its real path uses
`MatchManager.TriggerFTP()`, `HandleFTPBallPath(...)`, attacker/defender selection
and movement, `SkipAttackerMovementPhase()` / `SkipDefenderMovementPhase()`, and
`PerformFTPInterceptionRolls(...)`. Match decision timing needs a typed roll
registration consistent with the integrated long/high/header/shot adapters.
Inspect current signatures/access and continuation guards before implementation.
