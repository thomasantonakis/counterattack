# Continue online gameplay development on Mac

Follow `PersonalMac.md` first on a fresh Mac. Use the existing Mac-local Unity,
GitHub and ignored EOS configuration; do not copy credentials from Windows.
Manually open this checkout in Unity 6000.5.3f1 before Editor operations.

## Bring over the checkpoint

For the son's verification run, use `OnlineMacVerification.md` and the single
`VerifyOnlineMac.sh` command. It handles native online fixtures, builds, local/
login checks, Windows-host requests and a committed JSON report automatically.
The individual manual commands below remain available for development.

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
> First Time Pass is implemented in 90a0af2 with 16 focused passes and 10 affected
> offline passes. The continuous-session boundary has 8 EditMode and 2 PlayMode
> passes plus 7 affected manual transport passes. Read the latest feature
> Verification.md files. Prioritize the one-command Mac verifier and host
> supervisor until the paired run is collected; keep their source/binaries stable.
> Then implement the remaining movement/dribbling/duel decisions with dedicated
> native coverage, followed by set pieces and normal full-Room roster/lifecycle.
> Preserve mandatory keeper/Final Third/loose-ball priority, receipt/revision
> validation, host-only RNG and guest inactivity. Then continue dribbling/duels,
> restarts/set pieces and full-match roster/lifecycle. Keep paired/manual sessions
> deferred until I have time. Compile/test/build through the open Editor, commit
> and push verified batches. Never launch Unity, batchmode, ordinary editor_play,
> GUI automation or additive scenes. Preserve unrelated local scene/font/settings
> edits. Room's current online startup still uses six prepared tokens. Default
> proof mode ends after the first action; opt-in continuous mode removes that
> boundary but does not make it a complete normal Room match. Continue independent
> work when a second-machine action is unavailable.

Implemented First Time Pass source navigation: `FirstTimePassManager.cs` provides
`GetLegalFirstTimePassTargetsForDecisionPreview()` and
`GetValidFtpMovementDestinationsForDecision(...)`. Its real path uses
`MatchManager.TriggerFTP()`, `HandleFTPBallPath(...)`, attacker/defender selection
and movement, `SkipAttackerMovementPhase()` / `SkipDefenderMovementPhase()`, and
`PerformFTPInterceptionRolls(...)`. `FirstTimePassManager.Online.cs`, the authority
partial and Match decision timing now use a typed roll entry point and cancelled
result guard. Extend these patterns for movement/duel choices.
