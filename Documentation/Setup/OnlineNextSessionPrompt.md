# Online next session: four steps to first Room integration

## Start here

User resumed independent work after the shot-block break (2026-11-14).
The block extension has 10 focused passes; the preceding shot category has 30.
Goal/reset and typed kickoff integration is pushed as 4668d13: six focused
OnlineGoals PlayMode passes, zero failed/skipped, compilation separately clean.
See OnlineGoals/Verification.md for native evidence. General goalkeeper free
moves/stay and deferred pickup/feet now have ten focused PlayMode passes,
zero failed/skipped; compilation separately clean. Deferred offline kickoff
checks also passed (two cases). See OnlineGoalkeeper/Verification.md.
The Editor timeout was the low-disk-space dialog. The user closed it and freed
space; C: now has approximately 129 GiB free and Unity is ready again.
Windows export refresh is underway; consult the latest feature verification
before claiming packaged evidence. Next: First Time Pass,
dribbling/duels, set pieces and full-match lifecycle.
Current Windows exports precede the block extension and must rebuild before
packaged acceptance. Paired/manual sessions remain deferred. Exact Mac commands
are in Documentation/Setup/OnlineRoomProof.md.

For the son's Mac when testing resumes, follow PersonalMac.md first on a fresh
machine and manually open this checkout in Unity 6000.5.3f1. Then:

```sh
cd /Users/vasilisantonakis/Documents/Projects/Personal/counterattack
git pull --ff-only origin codex/online-foundation-personal-mac
bash Documentation/Setup/BuildEOSStandaloneMac.sh room
bash Documentation/Setup/RunEOSStandaloneMac.sh login room
bash Documentation/Setup/RunEOSStandaloneMac.sh local room
```

Only during an arranged paired session, after Windows publishes its host handoff:

```sh
bash Documentation/Setup/RunEOSStandaloneMac.sh guest room manual
```

High-pass integration now has 21 focused PlayMode passes (both directions, actual
Room, preparation, accuracy/scatter/control, keeper responses, holds, cancellation
and deadlines). See Assets/Editor/FeatureTests/OnlineHighPass/Verification.md.
Contested header nominations/results/targeting and headed-pass collection now
have focused coverage, including headed keeper back-passes and landing
interceptions (40 PlayMode + 10 EditMode passed). Goalkeeper distribution has
14 focused passes for quick throws and Final Third drop/kick targeting.
Loose-ball ties and keeper spills now have 18 focused passes, including recovery,
holds and replacement races. Headed shots now have 16 focused passing outcomes
through save/handling, distribution and completed rebounds. The shot category
now has 40 distinct passing cases including ordinary shots, snapshot defender
movement, post-pass choices, interrupted previews and outfield blocks. Continue goals,
general keeper moves and restarts. See OnlineShots/Verification.md.
See Assets/Editor/FeatureTests/OnlineHeaders/Verification.md.
Keep paired/manual sessions deferred while independent implementation continues.

Further Windows checkpoints add host dice results (5943f15), result holds
(987fd30), long passes/action controls (f4a209e), movement/player presentation
(09ddeed), and Final Third selection/continuation. See the OnlineRollResults,
OnlineResultHold, OnlineLongPass, OnlineMovement and OnlineFinalThird feature
Verification.md files for exact focused counts and XML. Current wire is v7.
The long-pass automatic-roll admission audit is verified; use its typed timing
registration for subsequent online adapters. Continue high passes, headers,
dribbles/duels, shots and restarts; full six-token proof/lifecycle limitations
still apply. Do not treat local builds as paired or full-match acceptance.

### Current Windows implementation update (2026-11-13)

User expanded scope to full internet gameplay: continue beyond passes into
headers, shots, movement, interactions, restarts and set pieces. Defer all
paired/manual acceptance sessions with the user while continuing focused
automated Windows work. Priorities and boundaries are tracked in
Documentation/Plans/OnlineGameplayIntegration.md. Do not stop at long passes.

The recovery and manual prepared-board batches are pushed as 64cd596 and
52d7bf7. The subsequent Room work adds explicit normal-scene guest startup
isolation and `Assets/Scenes/OnlineRoomProof.unity`, an authored deterministic
copy using Room's real serialized managers and a controlled six-token roster.
It reuses manual commands/recovery, exact catalog/roster checks, accepted
sequence watermarks, stats/timing snapshots and separate presentation frames.
Wire is now ground-wire-v7; old exports must be rebuilt. Subsequent batches
5943f15 and 987fd30 add immutable host roll observations and result continuation
holds. The current long-pass batch adds typed action selection, accuracy/scatter,
keeper Pace and landing recovery; see its feature verification. Continue into
movement and remaining high/header/shot/restart integration.

Focused Windows evidence: startup 2 EditMode + 1 PlayMode, Room action 6
EditMode + 6 PlayMode, manual/recovery exchange 7 PlayMode, affected wire 13
EditMode and pause 11 EditMode; all final results pass with zero failures/skips.
Compilation is separately clean. Earlier recovery 8 EditMode + 8 PlayMode and
pause 13 PlayMode evidence remains retained. Exact XML and limitations are in
the feature Verification.md files. No full suites ran. Windows Room export and
packaged local/login checks passed, including native liveness notification
registration. Both applications must rebuild for the current v7 framing.

Use Documentation/Setup/OnlineRoomProof.md for current Mac setup/build/local/
manual Room commands and prepared reverse-role support. Actual two-human
Windows/Mac Room operation and EOS interruption/rejoin still need both reports;
do not mark the four-step acceptance complete from the deterministic/local
checks. No remote Mac shell is available. Continue independent work before
requesting an external action. Normal offline Room remains the default.

Use **Windows as the development machine and authoritative host**, with the
family Mac as guest on its hotspot for paired tests. Switching the host to
macOS is not needed for these four steps. Reverse roles remain a later
acceptance check; do not claim them without an actual run.

Start Codex in this checkout with the settings in
`Documentation/Setup/CodexContinuousWork.md`, then send:

> Read Documentation/Setup/OnlineNextSessionPrompt.md. Implement and verify all
> four steps through the first real ground-pass/interception flow in Room.
> Continue across focused batches without asking whether to proceed. Preserve
> unrelated edits, execute focused tests, fix failures, commit/push task-related
> work and update this handoff. Ask only for genuinely missing design information
> or a necessary manual/external action that available tools cannot perform.

## Authorization and continuity

The user authorizes **all four steps below**, including code, authored
scenes/prefabs/UI, focused framework tests, live-Editor builds, standalone
probe launches and necessary task-related Git add/commit/fetch/pull/push.
Implement serial verified batches and continue to the next authorized step.
Do not stop after each batch to ask "shall I continue?"

Preserve unrelated local changes. Current Windows leftovers include dynamic
fonts, project/coverage/time settings, solution metadata and generated EOS
plugin resources/configuration. Inspect actual diffs; do not commit/discard
them just to clean the worktree. Never commit local credentials, exports or
device identity. No paid services are authorized.

This prompt cannot change an enforced sandbox/approval policy. If prompts
remain, report the active setting and how the user can change it. Full access
does not grant remote control of the family Mac, dismiss OS dialogs or remove
usage limits. If paired testing is unavailable, finish independent local work
first, then identify the exact external dependency.

## Read and connect

- Read AGENTS.md, Documentation/Setup/WindowsBootCamp.md,
  Documentation/Plans/OnlineMultiplayer.md, Documentation/Plans/EOSSetup.md,
  and Documentation/Testing/RunningTests.md.
- Read Verification.md under Assets/Editor/FeatureTests/OnlineGroundPass/,
  OnlineGroundPassNetwork/, OnlineSharedPause/ and EOSStandaloneProof/.
- Resolve the root with `git rev-parse --show-toplevel`; pass the actual absolute
  root explicitly on Unity CLI calls, with required caller/skill metadata.
- Follow the unity-cli skill. Check this project's ready connection,
  compilation/reload, Play Mode, dirty scenes and pending tests/builds first.
  Save through Unity as needed; preserve unsaved work.
- Never launch Unity, use batchmode/editor_play, edit a loaded scene on disk,
  open scenes additively or automate modal dismissal. Framework-controlled
  PlayMode tests are authorized. Preserve designer-owned MatchStats layout.

## Proven baseline — do not rerun by default

Branch: codex/online-foundation-personal-mac. Latest evidence commit before
this handoff rewrite: **5038c52**. Shared-pause feature uses wire/handoff **v2**.

- Windows/macOS standalone EOS login and relay-only packet exchange passed
  across separate networks, with the family Mac on the hotspot.
- Real prepared-board pass preview/confirm/interception, host-only roll,
  detached guest display and matching final state passed on both applications.
- The paired v2 pause proof also passed: guest-owned pause/resume, duplicate
  pause, rejected host resume, stale-confirm rejection and one authority roll.
- Windows host report, directly inspected:
  TestResults/GameplayRegression/Runs/20261111T221104422Z-Gameplay-host/result.json.
- Mac guest report, user supplied and retained on that Mac:
  TestResults/GameplayRegression/Runs/20261111T221131Z-gameplay-guest-68731/result.json.
- Both recorded sharedPauseRevision=2, publicationSequence=9, rollSequence=1 and
  digest BD30B1171DCA79D53321D313092B72836719DB5A44DA84190D787773C969CED3.
  guestRulesInactive=true on the guest. Remote handoff removal was verified;
  no paired execution is pending.
- OnlineSharedPause: **11 EditMode + 13 PlayMode passed**, zero failures/skips,
  compilation separately clean. Native XML under TestResults/GameplayRegression/:
  Runs/20261111T220310248Z/results.xml and Runs/20261111T220358201Z/results.xml.
  Earlier gateway/network evidence remains in the feature verification files.
- Windows exports report Succeeded with a missing Visual Studio vswhere.exe
  diagnostic and existing warnings; the exported applications ran. The latest
  Mac export reported Succeeded, zero errors and six warnings.

The probe scene is **Assets/Scenes/OnlineGroundPassProof.unity**. Its guest
rules hierarchy stays inactive. **Room is not yet an online match.** Device ID
identities prove this development connection, not production account or
entitlement verification. Do not repeat completed connectivity work instead
of implementing the remaining steps.

## 1. Disconnect detection and reconnect recovery

Implementation in progress: `OnlinePeerRecovery` and independent connection
holds have focused Windows evidence (8 EditMode + 8 PlayMode passes, zero
failures/skips, compilation separately clean). See
`Assets/Editor/FeatureTests/OnlineRecovery/Verification.md`. This covers
injected-clock timeout, reserved-account rebind, retired generations/receipts,
and live preview/movement/roll/paused authority recovery in both directions.
Manual-exchange heartbeat/rejoin/resync integration now has deterministic
encoded interrupted-transport evidence in both directions (part of the five
passing OnlineManualControls PlayMode cases). A bounded Windows packaged local
recovery scenario also passed. Actual EOS interruption/rejoin is unverified;
do not treat these tests as completion of this step. Step 2 has authored controls
and a manual host/guest exchange in OnlineGroundPassProof; its paired human run
is pending. Steps 3–4 now have local startup/action implementations and focused
evidence described at the top of this handoff. The live hierarchy/startup audit is
recorded in Documentation/Setup/OnlineRoomStartupAudit.md. Continue independent
Room implementation; lack of Mac access does not block that work.

- Add authenticated peer liveness, bounded heartbeat/timeouts and connection
  generations; distinguish transport loss from voluntary leave.
- Hold authority gameplay, rolls, timers and active movement/coroutines when
  loss is detected. Continue networking/presentation on unscaled time. Keep a
  connection hold separate from requester-owned shared pause; reconnect must
  not resume somebody else's pause or transfer ownership.
- Stop guest submission immediately on local loss. Reauthenticate/rejoin to
  the reserved team, retire the old connection and reject its queued packets.
- Resynchronize the current decision, accepted receipts, publication/outcome/
  roll sequences, pause requester and remaining timing. Resume a still-live
  authority resolution without another pass, roll or stats mutation.
- Cover loss at preview, confirm/roll boundaries, mid-movement and while paused;
  late packets, duplicate retries, wrong accounts, stale generations, timeout/
  cancel and return. Use injected clocks/transport interruption for deterministic
  focused tests, followed by a bounded standalone recovery scenario.
- Do not invent defeat/AI takeover, seamless host migration or arbitrary
  mid-action crash restore as shortcuts.

Acceptance: returning guest retains its team and accepted outcome; no duplicate
stats/draws, unauthorized resume or stale-peer mutation occurs.

## 2. Manual online controls in the owned scene

- Author inspectable target/input and pause/resume controls in edit mode.
  Replace scripted actions with human-selectable start, target preview/confirm,
  defending roll and requester-only pause/resume.
- Route **host physical input and guest intentions through the same gateway**.
  Derive sender/team from authenticated connections. Transmit semantic IDs/hex
  coordinates, never arbitrary methods, synthetic keys or screen coordinates.
- Display authoritative legal choices, owner, pending/rejected commands,
  shared pause and connection holds. Keep the opponent pause view passive.
- Keep camera/pointer/inspection/statistics visibility personal; host personal
  UI must not obstruct the guest's authorized action.
- Test real clicks/buttons/keys, stale targets, double confirms, roll retries,
  wrong-team input and pause/disconnect races in both attacking directions.

Acceptance: two humans can perform the prepared pass/interception flow through
the gateway with the same once-only outcomes already proven automatically.

## 3. Safe guest startup for Room

- Audit Room's serialized references and Awake/OnEnable/Start/Update/events,
  UI buttons and synthetic-input mutation routes before enabling a guest.
  Include ApplicationManager/save selection, MatchManager, input, AI, clocks,
  automatic rolls, movement and scene/bootstrap effects.
- Introduce explicit host-authority versus guest-replica startup. Guest must
  not load/run a second match, shuffle/draw, advance timers, resolve rules,
  subscribe mutation handlers or write the user's live saves.
- Preserve offline Room behavior. Prefer an explicit online context and owned
  fixture/scene configuration over speculative rewrites of every manager.
- Make scene changes through the live Editor; preserve unsaved work. Never
  edit loaded Room YAML on disk or create visuals only at runtime.
  Canvas/MatchStatsUI/Stats remains designer-owned; only permitted text values
  and identity/status colors may change through scripts.
- Add focused lifecycle/input/startup tests proving inert guest authority
  callbacks and no save writes, while presentation/inspection remain usable.

Acceptance: normally loaded guest Room is a replica with no independent rules;
offline behavior remains intact at affected, tested seams.

## 4. Publish and render the first complete Room action

- Extend publications to the real Room pass/interception boundary: stable
  roster/token IDs, ball/token positions, selection/legal choices, decision
  owner, pending interactions, accepted roll/outcome IDs, statistics, timing/
  holds and the next decision/continuation.
- Separate reliable committed outcomes, animation presentation and full resync
  snapshots. Reject old/conflicting state and incompatible roster/catalogs.
  Guest animation completion must never advance rules.
- Connect manual commands and recovery from steps 1–2 to host Room; apply
  authoritative values/presentation on the inert guest from step 3.
- Use owned deterministic board/roster fixtures and disposable save paths.
  Prove safe passes, successful/failed interceptions, pause and reconnect with
  real gameplay entry points, fixture-rigged dice and both directions.
- Build the focused Room proof and execute Windows host/macOS guest across
  separate networks. Record both completed reports and matching outcomes/next
  decisions. Prepare reverse-role support without claiming an unexecuted run.

Acceptance: one human-controlled real Room pass/interception and continuation
agrees across both processes, including ownership, roll, stats and recovery.
This does not claim every movement/shot/restart/draft flow is online.

## Verification and continuity

Create dedicated feature test folders/checklists per batch. Run serial explicit
fixture/category filters through live CLI/Pipeline after clean compilation and
scene checks. Retain native XML/progress under TestResults/GameplayRegression/Runs/.
Diagnose/fix failures yourself. Report compile status separately from actual
passed/failed/skipped counts. Follow AGENTS.md: broader affected-folder/full-suite
verification is handed to the user; this task does not request whole suites.

Commit/push verified task-related batches and update this handoff with actual
results and remaining steps. Continue while meaningful authorized work remains.
If a manual/remote action is unavailable, finish independent implementation and
local tests before handing over that dependency. Do not idle waiting for a
second machine when local work can continue.

## Machines and unavoidable manual dependencies

Windows root: resolve locally; current checkout is
C:/Users/thomas/Documents/Projects/Personal/counterattack.
Family Mac: /Users/vasilisantonakis/Documents/Projects/Personal/counterattack.
Extend Documentation/Setup/OnlineGroundPassNetwork.md's current helpers for
recovery/manual Room work instead of repeating the completed old probes.

No remote shell/control connection to the family Mac is currently available.
Full access on Windows does not create one. Give concise Mac pull/build/run
commands when needed and continue independent work meanwhile. The user must
open Unity manually, change physical networks and handle blocking Editor/OS
dialogs. macOS may request EOS Connect login-keychain access after rebuilds;
never request passwords/tokens in chat. Stable signing and first-install/
relaunch/update keychain UX remain release validation work.
