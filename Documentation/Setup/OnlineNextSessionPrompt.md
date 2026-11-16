# Online next session: four steps to first Room integration

## Start here

Current checkpoint (2026-11-16): normal Host/Join, persistent native setup, all Draft
variants, opening coin toss and complete roster launch data are implemented.
The authored OnlineRoom now uses the actual 1,900-hex pitch, 32/36 drafted actors,
real kickoff managers and guest score/time/position projection. Sixteen focused
actual-scene cases passed, including final isolation verification. Native
frontend ? full Room handoff has 13 lifecycle and four encoded kickoff passes;
seven affected manual-control and 12 frontend-entry cases also passed. All three
Windows exports and packaged proof local/login checks succeeded.
Roster status contract v2 has 13 validation and four real-scene projection passes:
bookings/injuries, bench changes, converted keepers, directions and player stats.
Online pause/menu and safe Leave have eight focused cases. Authoritative
substitutions and normal goalkeeper swaps now have 18 passing cases, including
allowance/rebind boundaries. Bounded full-snapshot compression has 15 passes and
fixes a real 36-actor wire-size failure without dropping fields. Required keeper replacement/emergency nomination now has 33 focused passes,
including opponent-owned pauses and guest kit/state projection. Shootout ordering,
real kick outcomes and early/sudden-death completion now have 55 focused native
cases; see OnlineShootout/Verification.md for the current run/build evidence.
Authenticated Room tiebreaker/extra-time coin toss and half transitions now have
37 new native passes, plus 142 distinct affected passes. This also fixes null
optional snapshot objects becoming empty live state on guests. See
OnlineRoomCoinToss/Verification.md; all three Windows exports and local/login
checks refreshed. Regular-match conclusion now has 26 new native passes and 35
affected passes, including safe host/guest return and preserved scoreboard/Stats.
See OnlineMatchConclusion/Verification.md. Direct pitch input now adds 52 native
passes (44 EditMode, eight actual-scene), plus 11 affected passes. Player/hex clicks,
real collider raycasts and original action keys use the authenticated semantic
gateway. Host movement/kickoff and inert guest commands passed in both attacking
directions. See OnlineBoardInput/Verification.md; full contract is Room v9.
Next: shared settings commands and motion interpolation/transport tuning.
Controls no longer serialize the complete roster/stats every display update:
typed detached control decisions have 10 new focused passes and 19 selected
affected passes. See OnlineControlDecision/Verification.md for memory-measurement
limits and retained fixture corrections. Wire framing/full Room v9 are unchanged.
In-flight full snapshots
now retain actual ball/player poses, with 9 validation and 5 actual-scene passes. Full-match internet acceptance and measured
latency remain unverified. Paired Mac testing is deferred.
See Assets/Editor/FeatureTests/OnlineFullRoom/Verification.md.


Latest steering: the son's Mac verification is deferred until the user explicitly
requests it. The waiting supervisor was stopped; do not host or await reports
now. Continue implementing gameplay and normal Room integration. Maintain
OnlineMacTestManifest.json: each new test or parameterized case gets a separate
test_additions entry, and changed fixture definitions need refreshed hashes.
The verifier refuses unregistered fixtures or changed test definitions. Run
focused Windows tests independently; do not spend the task on Mac coordination.
Dribble continuation/pace forfeit and declined-nutmeg steals now have 12 focused
PlayMode passes plus 3 affected offline passes, compilation separately clean.
See OnlineDribble/Verification.md. All 12 cases have explicit Mac manifest
entries. Ground duels now have 36 focused passes and 13 affected passes; see
OnlineGroundDuels/Verification.md. Next:
fouls/cards/injuries, then remaining restarts and normal full Room lifecycle.
Exports precede this dribble batch; refresh before any future paired run.

Prepared tooling: one-command son's Mac verification with one giant JSON report
committed/pushed for inspection, and a Windows host supervisor that starts/retires
automatic paired sessions and retrieves reports. Use OnlineMacVerification.md.
The command is `bash Documentation/Setup/VerifyOnlineMac.sh`; the Windows helper
is HostOnlineMacVerification.ps1. Both need the latest branch pulled first.
The verifier selects implemented online fixtures, then builds/login/local/paired
checks. It records failed/skipped stages and native evidence, publishes a separate
verification branch, and preserves source branches/local edits. Host retries are
bounded and every failed attempt stays in the report. Actual Mac execution is
still external; do not claim it from Windows unit or syntax checks.
The earlier Windows watcher was stopped when the user deferred the Mac run.
Check Temp/MacVerificationHost.process.json and actual process state before
starting another watcher, only when requested. Actual GitHub request/rejection
and 3 MiB report-download smokes passed; see Documentation/Setup/tests/Verification.md.

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
Windows Room and prepared exports succeeded; both packaged local checks passed.
See OnlineGoalkeeper/Verification.md for build IDs and retained native reports.
For moving development/hosting to Mac, use OnlineMacContinuation.md; it includes
exact commands, a pasteable development prompt and an offline Git bundle fallback.
First Time Pass now has typed targeting, preparation moves/skip and interceptions,
with 16 focused passing cases (14 isolated + 2 actual Room serialized cases),
10 affected offline passes and separately clean compilation. See
Assets/Editor/FeatureTests/OnlineFirstTimePass/Verification.md for exact XML.
Continuous-session boundary now has 8 EditMode and 2 PlayMode passes; affected
manual transport has 7 passes. The default proof still ends after one action;
an opt-in continuous profile remains open until MatchEnded, with its own catalog.
See OnlineContinuousSession/Verification.md. Both Windows exports and local
checks have refreshed for FTP/continuous code, and native login passed.
Next gameplay: dribbling/duels, set pieces and normal full Room roster/lifecycle.
Fixed six-token startup replacement remains; this is not yet a normal online Room.
Current Windows exports include blocks, goals/kickoff and goalkeeper choices.
Paired/manual sessions remain deferred. Exact Mac commands
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

Online fouls: 16 focused passes (`20261114T152353072Z`) and 5 affected
offline passes (`20261114T152445990Z`), zero failures/skips. Compilation clean.
See OnlineFouls/Verification.md. Each new case registered in the deferred Mac
manifest. Next: set-piece preparation and execution, normal Room lifecycle.

Online set pieces: 20 isolated and 2 actual serialized Room-proof passes, plus
1 affected offline setup pass, zero failed/skipped; compilation separately clean.
See OnlineSetPieces/Verification.md. Typed setup, restrictions and execution
choices now connect to existing pass/cross/long/shot managers. Each new case
has a deferred Mac manifest entry. Full normal Room remains future work.

Latest: online set-piece continuation audit reproduced and fixed disabled-Update
restart ownership leak. All 24 own-feature cases passed at
20261114T154013072Z; roster replica visibility 4 passed at153651364Z.
Both exports refreshed successfully; packaged local checks and native Room
login passed. See OnlineSetPieces/Verification.md for 15:43 UTC evidence.
Next penalties/throw-ins, substitutions and normal Room.

Online penalties now have 12 isolated and 2 actual serialized Room-proof passes,
plus 2 real in-box steal-foul award cases, zero failed/skipped. Compilation clean.
See OnlinePenalties/Verification.md for native evidence and resolved missing grid
cache initialization. Mac cases registered individually. Exports at84d016e
precede penalties/caches; refresh again before paired execution. Next throw-ins,
remaining substitutions/keeper replacement and normal full Room lifecycle.

Online throw-ins: 10 isolated + 4 actual Room-proof passes and 5 affected offline
passes, zero failed/skipped; compilation clean. See OnlineThrowIns/Verification.md.
New user priority: complete Create New Game guest joining/team names/jerseys/both
ready, Draft owned rolls/picks/slot moves/start, and Room pause/settings,
substitutions and shootout ordering. See OnlineSessionLifecycle.md. Paired Mac
execution remains deferred; each new case still requires its own manifest row.

Setup agreement/capture slice: 44 distinct EditMode passes plus 27 affected
protocol passes, compilation separately clean. See OnlineSetupAgreement/Verification.md.
Team-owned names/kits, shared configuration, both ready, edit/rebind invalidation
and host start are implemented. The existing Create New Game button can bind
confirmation to its session; unbound online guests cannot save/load Draft locally.
Actual lobby UI/transport, session persistence, Draft and Room menu integration
remain next. Do not claim the frontend is connected yet. All Mac cases registered.

Online setup client/view: 48 distinct focused EditMode cases passed, plus 24
affected agreement and 5 authored capture cases. See OnlineSetupClient/Verification.md.
CreateNewGameScene now owns OnlineSetupView and a Header status object, hidden
offline; bindings enforce own names/kits, host shared settings and agreed start.
Unsaved edits survive remote snapshots and pending replies. The persistent EOS
lobby/session must still supply that client; Main Menu multiplayer is still a
placeholder. Continue actual host/join transport, scene handoff, Draft, and Room
menus/substitutions/shootout ordering. Mac manual execution remains deferred.

Frontend wire: 22 focused passes and 13 affected gameplay-wire passes. Shared
framing preserves gameplay v7, frontend uses session-wire-v1/CA11. Persistent
EOS lobby/peer dispatch and authored host/join controls remain next. Full low-
latency multiplayer is the explicit target; see OnlineSessionLifecycle latency
acceptance notes. No actual RTT or complete frontend connection is claimed.

Frontend authenticated channels and RTT telemetry now have 33 additional own-
feature passes (55 distinct including framing). Real serialized messages cover
peer identity, compatibility, nonce/rejoin, agreement and monotonic RTT; no actual
internet RTT is claimed. Native persistent EOS lifetime and host/join controls
remain next. See OnlineSessionTransport/Verification.md. All cases registered
for deferred Mac execution; keep full setup/Draft/Room and low latency as target.

Normal multiplayer entry now opens authored Host/Join/Copy/Leave controls in
CreateNewGameScene and owns a persistent native EOS session prefab. Direct-first
routing falls back to relay; proof defaults remain forced-relay. Focused native
coverage: 18 distinct own-feature passes (12 EditMode, 6 PlayMode), plus one
existing offline countdown case; compilation separately clean. Initial offline
fixture expectation was corrected without changing gameplay. See
Assets/Editor/FeatureTests/OnlineFrontendEntry/Verification.md. Every case has
its own deferred Mac manifest row. Actual paired Host/Join/RTT remains untested.
DraftAgreed currently captures agreed settings but does not yet launch Draft:
continue authoritative Draft bootstrap/rolls/decks/picks/slots and full Room,
then normal pause/substitution/shootout UI. Native frontend currently fails after
15 seconds without packets; reconnect preservation still needs implementation.
Exports predate this slice; refresh before paired use. Normal Room restored clean.

Frontend recovery supersedes the previous 15-second fatal disconnect: a silence
or EOS closure now holds state and blocks interaction, rotates the host nonce
once, retries bounded hellos and expires after five minutes. Same reserved peer
must still belong to the lobby before native reacceptance. Own-feature 16 passes,
affected channels 26 passes and entry scenes 6 passes; compilation separately
clean. See OnlineFrontendRecovery/Verification.md. Actual native reconnect/RTT
requires the deferred paired run. Continue authoritative Draft then full Room.

Normal-scene Windows export `build_06972049607d` succeeded at
`20261115T053740Z-normal-Windows-build/build-summary.json` (9 warnings,
one known nonfatal EOS post-build vswhere.exe diagnostic, zero C# errors).
Output: `Build/OnlineGame/Windows/Counterattack.exe`; no ordinary match was
launched and no full multiplayer acceptance is claimed. The Mac one-command
verifier now also builds the normal game scenes through
`BuildEOSStandaloneMac.sh game`. Its 13 tooling tests and Bash syntax check
passed on Windows; actual macOS export remains deferred.

Real production Host-button acceptance: 1 native PlayMode pass, 0 failed/skipped
at `20261115T054507321Z`. The existing scene controls captured valid selections,
created an EOS private lobby with AllowRelays, then cancelled without a guest.
The test verified transport cleanup, background setting restoration, guarded
local save and the retained redacted session report. This is not a paired join,
remote recovery or RTT measurement. The feature now has 19 distinct passed cases.
Its dedicated fixture/category is `OnlineFrontendNativeLobbyPlayModeTests` /
`OnlineFrontendNativeLobby`; it requires this machine's local EOS configuration.
It is separately registered for the deferred Mac run. Native result/report are
retained beside the XML in the run directory.

Latest proof exports also succeeded: Room `build_6c6089c7a517` (05:42:43 UTC),
prepared `build_92273852a34c` (05:43:03 UTC), each 2 warnings and the known
nonfatal vswhere.exe post-build diagnostic. Packaged checks passed:
`20261115T054303870Z-Room-local`, `20261115T054325787Z-Gameplay-local`,
`20261115T054339522Z-Room-login` (liveness notifications registered).
Build summaries and native evidence are under TestResults/GameplayRegression/Runs.
The normal-scene export and these proofs contain the same production C# changes;
the subsequent added native fixture is Editor/test-only. Full Draft/Room remains.

Regular online Draft is integrated with the authenticated native frontend and
normal Draft scene: host deck/GK deal, owned rolls/picks/reordering, detached
stats/rosters, lost-reply/rejoin continuity and both confirm. It uses 36 authored
slots/four cards and suppresses local Draft save/random startup online. 76 new
focused cases passed (40 authority,16 serialized,20 actual-scene), plus26 affected
channels and16 recovery; compilation separately clean. Every case registered for
Mac. See OnlineDraft/Verification.md. Frontend rules version is now v2.
Next: Free/international variants, opening coin toss, full Room roster/save-free
handoff and menus/substitutions/shootouts. Regular Draft currently ends at agreed
room-ready state; no full match start is claimed. Exports predate this slice.

Online Draft variants now support all nine Regular/Free Regular/Arcade and
Deal/Free/Arcade combinations, plus international prefilled rosters, in the normal
authored Draft scene. 50 new cases passed (38 mode/national,10 actual UI including
full mixed drafts,2 comparisons with legacy CSV loaders); affected regular40,
transport16 and scene20 also passed. Compilation separately clean. Mac manifest
has separate entries and refreshed hashes. See OnlineDraftVariants/Verification.md.
Frontend rules v3. Next opening coin toss, full Room initialization/roster and
normal menus; online Arcade returns remain outstanding. Exports predate Draft.

Opening coin toss now follows roster confirmation: owned host-resolved rolls,
re-roll ties, real CoinTossDecision kickoff/direction choices and opponent
continuation, frozen rosters and recovery preservation. 24 new cases passed
(20 rules,4 actual scene); affected Draft40/transport16/variants38/scene20 passed.
Compilation separately clean. Mac cases registered individually; older definition
hashes/evidence refreshed for changed Start expectations. Frontend rules v4.
See OnlineOpeningSetup/Verification.md. Next full Room initialization/roster and
runtime transport, normal pause/substitution/shootout UI; Arcade returns remain.
No full remote match start is claimed. Windows exports predate Draft/opening.

Online Room launch preparation now produces real MatchManager.GameData from all
16/18 drafted slots per side, goalkeeper attributes, one host-resolved referee,
opening choices and host session ID/UTC. Real loading prioritizes authenticated
bootstrap instead of local save discovery. Host live logs use session storage;
manual local save-slot APIs are blocked for a full online session. 36 new focused
passes plus affected startup2+1 and opening20+4; compilation separately clean.
See OnlineRoomLaunch/Verification.md. Each new case registered for deferred Mac.
Native frontend caches room-bootstrap.json but does not yet load full Room.
Next actual full Scene startup/roster/transport and normal menus. Rules v5;
exports predate Draft/opening/launch and need refresh before paired execution.
