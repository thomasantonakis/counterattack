# Counterattack online PvP implementation plan

Prepared: 2026-11-09. Status: M0 investigation and isolated protocol foundation
started; Windows-host/macOS-guest standalone relay packet exchange passed on
separate networks. The first real ground-pass authority adapter passed focused
tests; full M0 acceptance and M1 cross-process gameplay remain pending.

## Implementation evidence — first M0 batch

- On 2026-11-11, standalone Windows-host/macOS-guest relay-only packet reports
  passed on separate networks with the family Mac on a hotspot. See
  `Assets/Editor/FeatureTests/EOSStandaloneProof/Verification.md` for exact
  report paths and packaging limitations.
- Added isolated `OnlineGroundPassAuthority` using real gameplay entry points,
  shared host/guest admission checks, read-only semantic snapshots and ordered
  coroutine outcome publications. Focused validation: 1 EditMode + 14 PlayMode
  passes, zero failures/skips; compilation separately clean. See
  `Assets/Editor/FeatureTests/OnlineGroundPass/Verification.md`. It is not wired
  into Room/EOS yet; guest startup, live physical input/clock authority, shared
  pause/recovery and the complete M1 paired proof remain pending.

- Added provider-independent OnlineMatchProtocol under Assets/Scripts/Online/.
  This is an unwired command-admission foundation, not transport/authentication
  or an implemented gameplay gateway.
- Live Editor compilation clean; 27 focused EditMode cases passed, zero failed
  or skipped. Native run: TestResults/GameplayRegression/Runs/20261109T202334308Z/.
  Details: Assets/Editor/FeatureTests/OnlineMatch/Verification.md.
- User created an EOS product and desktop client with the predefined PeerToPeer
  policy. [EOS setup](EOSSetup.md) records the portal configuration;
  local credentials are Git ignored and are not tracked.
- Pinned EOS Unity plugin 6.2.0; live Unity 6000.5.3f1 import and compilation
  succeeded. The focused protocol fixture passed 27/27 again after import:
  `TestResults/GameplayRegression/Runs/20261110T080134239Z/results.xml`.
  The user saved local config; an isolated Mac Editor probe returned native
  `init=Success`, `create=True`, `shutdown=Success`. Transport, identity,
  standalone builds and cross-platform connectivity remain gates.
- One focused live EOS Connect Device ID EditMode case passed after an Editor
  restart: 1 passed, 0 failed; a valid Product User ID was returned. Native XML:
  `TestResults/GameplayRegression/Runs/20261110T204345472Z/results.xml`.
  This proves one local development login, not a second user or P2P connection.
- A separate focused EOS private-lobby case passed 1/1 after a fresh Editor
  start: a two-seat invite-only lobby with join-by-ID was created and destroyed.
  Native XML: `TestResults/GameplayRegression/Runs/20261110T205106861Z/results.xml`.
  This case alone did not attempt a remote join.
- On 2026-11-11, separate focused Mac host and guest EditMode cases each passed
  1/1: the guest found an unadvertised lobby by exact ID, joined and left, and
  the host observed a distinct product user before destroying the lobby. Host
  XML: `TestResults/GameplayRegression/Runs/20261111T144217103Z/results.xml`.
  Guest completed status was reported from the family Mac; its native XML remains
  there at `TestResults/GameplayRegression/Runs/20261111T144726042Z/results.xml`.
  No P2P packet was sent.
- Two focused two-Mac P2P attempts delivered an exact guest hello packet and
  host acknowledgement through EOS reliable ordered P2P. The family Mac guest
  passed 1/1 each time; its latest XML is
  `TestResults/GameplayRegression/Runs/20261111T151712137Z/results.xml` on that
  Mac. The host reached lobby cleanup after verifying the hello, but failed
  0/1 when EOS returned `TimedOut`, then `NotFound` on retry. Latest host XML:
  `TestResults/GameplayRegression/Runs/20261111T151623401Z/results.xml`.
  The cleanup assertion was narrowed to accept `NotFound` only after a timed-out
  destroy request.
- The final focused P2P run passed 1/1 on each Mac. Host XML:
  `TestResults/GameplayRegression/Runs/20261111T152710314Z/results.xml`.
  Guest XML on the family Mac:
  `TestResults/GameplayRegression/Runs/20261111T152822714Z/results.xml`.
  The host verified the guest's exact hello and the guest verified the host's
  exact acknowledgement. Both cases finished with zero failures or skips.
- A focused relay-only packet run then passed 1/1 on each Mac. Both peers set
  and read back EOS `ForceRelays` before connecting, and each received the
  expected packet. Host XML:
  `TestResults/GameplayRegression/Runs/20261111T161052518Z/results.xml`.
  Family Mac guest XML:
  `TestResults/GameplayRegression/Runs/20261111T161217944Z/results.xml`.
  This establishes packet exchange under relay-only settings on the current
  networks, but does not inspect a per-connection route.
- The same relay-only host and guest cases each passed 1/1 after the user put
  the family Mac on an iPhone hotspot and restarted both Editors. Host XML:
  `TestResults/GameplayRegression/Runs/20261111T161817260Z/results.xml`.
  Family Mac guest XML:
  `TestResults/GameplayRegression/Runs/20261111T161910290Z/results.xml`.
  This proves two-way packet delivery across the reported separate networks
  with relay-only settings in Editor processes.
- A second relay-only run passed 1/1 on each Mac after reversing the network
  roles: personal Mac on an iPhone hotspot, family Mac on home Wi-Fi. Host XML:
  `TestResults/GameplayRegression/Runs/20261111T163354297Z/results.xml`;
  guest XML on the family Mac:
  `TestResults/GameplayRegression/Runs/20261111T163544902Z/results.xml`.
  The host reused EOS after a separate focused login-reuse test in the same
  Editor process; that test passed 1/1 at
  `TestResults/GameplayRegression/Runs/20261111T163147731Z/results.xml`.
- No Windows/macOS, standalone-build, scene/input, pause or full-match
  synchronization proof is claimed. No existing gameplay/visual wiring changed.

Updated 2026-11-09: explore free, distributor-independent infrastructure first;
paid infrastructure and distributor commitment are deferred. Shared pause,
two-team substitutions and online settings permissions are confirmed below.

## Outcome and launch boundary

Two customers on Windows and/or macOS create a private match, agree settings,
choose their own teams and kits, draft, and complete the same match on separate
networks. Each controls only decisions assigned to their team. Cameras, mouse
pointers, inspection and personal panels remain independent.

Confirmed by the user: Windows/macOS cross-play; private friend matches;
one player's computer hosts the authoritative game at launch. Explore free
connectivity before selecting a distributor; more than one distributor may be
supported eventually. Dedicated compute and paid infrastructure are deferred.

This plan covers networking and its content-ownership integration points.
Store checkout, new sound assets, expert AI and World Cup implementation are
separate workstreams. Actual purchase verification must be integrated before
commercial launch; development entitlement fixtures cannot ship as verification.

No ranked matchmaking, public game browser, spectators, chat, shared pointers,
automatic CPU replacement, seamless host migration or asynchronous turns are
required for the initial online release. These are proposed scope limits.

## Confirmed content rules

| Item | Online rule |
| --- | --- |
| Base content | Regular matches, Tabletopia decks, starter jerseys, all visual weather/time choices |
| Ordinary jerseys | Each player chooses from their own account's available jerseys |
| International guest access | An owning host can offer their available international teams to a guest with none; borrowing lasts only for this match |
| Both own international teams | Each chooses among their own international teams |
| International kits | Kits follow the eligible selected international team, including the guest-access exception |
| Draft decks | Distinct union of both accounts' owned decks, restricted by the agreed CreateNewGame checkboxes |
| Local modes | Hotseat and PvC use the current account's content |
| All international teams owned | Future PvC World Cup access; no tournament implementation in this plan |

For international guest access, the content owner should create the match. A
non-owning host does not automatically gain borrowing rights from an owning
guest; offer a clear instruction to let the owner host rather than invent a
new sharing rule. Confirm a different policy if desired.

The existing different-international-teams restriction remains. If both owners
select the same team, show the conflict and ask one to change; do not silently
change someone else's team or jersey.

The deck union is a shared draft pool: either participant can draft eligible
cards even if the other account supplied access. Match access grants no permanent
ownership or later offline access. Apply the existing outfield and goalkeeper
checkbox families separately. For example, A owns Tabletopia + pack X and B owns
Tabletopia + pack Y: with those categories agreed, the pool contains Tabletopia,
X and Y once, not two Tabletopia copies. Deduplicate by stable deck/card identity,
not player display name; preserve intentional distinct card editions.

Freeze a match content manifest after both players accept it. Never remove an
in-use player because an entitlement lookup temporarily fails. Recheck ownership
for a new match or resumed match under the store's verification policy; define
refund/revocation behavior before purchase integration is released.

## Project evidence and architectural consequences

Inspected source and CodeGraph on 2026-11-09; this is not a runtime audit.

| Existing seam | Finding and consequence |
| --- | --- |
| Unity 6000.5.3f1; Packages/manifest.json | EOS Unity plugin 6.2.0 is pinned and imports cleanly. Multiplayer Center exists; no gameplay transport is wired. Select and verify one in M0. |
| CreateNewGameManager and GameSettings | Existing settings, kit/team validation and six deck-category flags can be reused. Saving local JSON and loading Draft/FreeDraft currently drives progression; online needs an agreed manifest and coordinated transitions. |
| ApplicationManager | Persists local mode and save path. Add explicit online match/participant context; local paths must never be network identities. |
| DraftManager and DraftOrderPanelController | Draft pools, shuffles, first-pick rolls and scene flow currently run locally. The authority must own them; guest startup must not generate a second draft. |
| GameInputManager | Physical and synthetic clicks/keys invoke gameplay handlers. Add an authorized command boundary before mutation; synthetic execution is not permission verification. |
| RoomInputOwnership | Handles intro, inspection and rules presentation. It is not network account/team authorization. |
| RoomDecisionContext and RoomDecisionManager | Expose candidate actions, expected team, manager and valid targets. Useful for command validation, but audit completeness and remove dependency on debug UI for authority. |
| DecisionTimingService | Has HasCommandAuthority; automatic rolls have an existing claim/sequence seam. Extend authority to all clocks, rolls and continuation transitions. |
| HelperFunctions, MatchManager, draft panels | Gameplay randomness has multiple call sites. Audit all of them; one shared seed alone cannot make independent coroutine execution safe. |
| RoomSaveService and MatchManager.CanCreateRuntimeSnapshot | Saves reject moving tokens/ball, active actions and several transitions. Reuse stable checkpoints, but implement separate live replication and reconnect state. |
| PauseMenuManager and resolution panels | Local menus currently affect match timing/continuation. Split personal visibility from authoritative pause, roll and acknowledgement decisions. |

## Authority and synchronization contract

One authority resolves rules. Both host and guest submit through the same
authorization path. A host's physical click cannot bypass permissions.

Proposed components, with names subject to implementation:

- OnlineMatchContext: immutable match ID, authenticated account-to-team mapping,
  connection generation, build/protocol/catalog versions and mode.
- OnlineMatchCoordinator: connection, configuration revision, readiness, draft,
  scene readiness, playing, reconnecting, completion and leaving states.
- MatchCommandGateway: serial processing, authorization, legality validation,
  duplicate handling and execution through real gameplay entry points.
- MatchStatePublisher / MatchReplica: authoritative state updates and local
  presentation, including active decision, legal input, outcomes and recovery.
- MatchCheckpointStore: durable supported recovery boundaries and online metadata.
- ContentAccessPolicy: stable deck/card/team/kit IDs, account entitlements,
  international borrowing and per-match content manifests.

Commands contain match ID, command ID, connection generation, expected state
revision and decision ID, action kind, and stable actor/target IDs or hex
coordinates. Resolve the sender from the authenticated connection; never trust
a claimed team or account in the message. Reject unknown, malformed, oversized,
stale, unauthorized and illegal commands without changing gameplay state.
Duplicate retries return the original acknowledgement/result, without another roll.

Send semantic board actions, not screen coordinates or arbitrary method names.
Existing handlers may be adapted behind the gateway, but remote requests cannot
call arbitrary synthetic keys, test rigging or debug commands. Permission checks
cover button callbacks, drag/drop, modal controls and automatic transitions too.

The host alone runs rules, gameplay random draws, clock accounting, automatic
rolls, decision expiry and gameplay coroutines. The guest renders published
state and submits intentions. Do not independently simulate the rules on both
computers, and do not treat matching transforms as complete synchronization.

The central M1 risk is exposing enough current manager state for a guest replica
without letting its normal Start/Update/event callbacks run a second match.
Audit that explicitly. Preserve current rules and owned regression fixtures;
avoid a speculative rewrite of every gameplay manager before the first proof.

State includes committed selections, pending decisions/owners, relevant manager
substates, timer allowances, pending rolls/results, continuation state, rosters,
positions, statistics and restart data. Separate live view state, reconnect state
and durable stable saves; do not assume one existing serializer covers all three.
Use ordered reliable messages for committed actions/results, versioned snapshots
for initial state/resync, and sequenced animation events. Ignore older updates.
Animation completion on a slow guest must not independently change the rules.

An opening intro or result panel must not make the host's personal UI a prerequisite
for executing the guest's authorized action. All gameplay holds must be explicit
shared state. Build the authority-facing API so a future headless server can use
it, while recognizing existing scene/UI dependencies will need further work.

## Shared game versus personal presentation

| Shared | Personal |
| --- | --- |
| Agreed settings and content manifest | Camera transform, zoom, presets and drag |
| Team identities, kits, rosters, draft picks | Pointer, hover, card inspection, draft sorting/filtering |
| Ball/token state, action outcomes, score/stats | MatchStats visibility and panel placement |
| Current decisions, roll results and continuation | Rules viewer, audio volume and graphics settings |
| Clock/budget truth, requested pauses and restart progression | Personal panel positions and non-gameplay preferences |

Weather and fixture lighting are free content but still agreed match settings.
Local display preferences cannot alter the authoritative clock. Preserve current
decision-budget and automatic-roll rules; networking is not permission to add
forfeits, change allowances or remove existing result holds.

### Confirmed shared pause and settings policy

- Escape requests an authoritative pause immediately, without an opponent vote.
  Pause the match for both participants, including gameplay timers, automatic
  rolls and continuations. Keep networking, rendering and UI processing active.
- Record the pause requester's account/team independently of host/Home/Away.
  The requester sees and operates the pause menu; the other sees a passive
  "Game paused" panel with no actionable pause-menu options. Camera/inspection
  behavior remains subject to the existing presentation rules.
- Only the requester can resume ordinary pause, open its subflows or change its
  shared options. Opponent Escape cannot resume, steal ownership or open another
  pause. Serialize simultaneous requests; the first accepted request owns pause.
- Opening substitutions makes that subflow available to both teams. Each can
  change only their own eligible players and each must click Confirm Subs once,
  including a team making zero substitutions. One account cannot confirm twice
  for both teams. Preserve legal windows, limits, injury and goalkeeper rules.
- Treat confirmations as team-specific, revision-bound readiness. A confirmed
  team is locked against further changes unless it explicitly withdraws its
  confirmation. Collect and validate both proposals before applying the combined
  result once; block resume while either confirmation or a mandatory change is
  outstanding. Disconnect/reconnect preserves pause ownership and accepted state.
- Preserve the current CloseToPauseMenu continuation after substitutions: once
  both confirm, return to the shared pause with its original requester, who can
  resume. This is the implementation interpretation of "two confirms to go on";
  automatic match resume after confirmation would be a separate clarification.
- In Game Settings each team may change only its OWN eligible jerseys. Both
  players get access to their own kit controls in this subflow; neither can
  overwrite the opponent's. Preserve entitlement and kit compatibility rules.
- Player assistance, weather, fixture time/lighting and ball choice are shared
  match settings. Publish one authoritative revision to both screens. The pause
  requester edits shared fields; the other can see them and edit only their kits.
  This editor assignment follows requester control of pause, not host privilege.
- Opening Game Settings does not open up unrelated gameplay settings. Preserve
  existing supported mid-match edits. Apply shared changes at a paused safe
  boundary; invalid choices cannot partially change either client's match.
- Saving or returning from a submenu must not implicitly bypass online pause
  ownership or the two-team substitution gate. Audit current local ResumeGame
  callbacks and forced-substitution entry points before adapting them.
- If the requester disconnects, keep a separate connection hold; do not silently
  transfer control or let the passive player resume. Both clients must have a
  safe way to abandon a disconnected match even when ordinary controls are locked.

Result holds/acknowledgements belong to the relevant decision owner; system-wide
transitions use explicit both-ready gates where needed.
Do not gate every animation on both players pressing a key. Record any ambiguous
gameplay ownership in Documentation/Testing/GameplayReview.md for clarification.

## Milestones and acceptance gates

Implement serially in reviewable batches; complete a milestone's focused tests
before expanding its coverage. M0/M1 produce evidence for an effort estimate.

### M0 — Decisions, inventory and network foundation (medium)

- Compare free distributor-independent connectivity using the options below.
  No paid plan or distributor integration is required to begin the investigation.
- Verify current package/native-binding compatibility with this exact Unity version,
  Windows, macOS and supported CPU architectures. Pin tested versions.
- Establish account identity distinct from transport connection ID; reserve team
  assignments across reconnects. Host role is separate from Home/Away role.
- Audit all mutation entry points, random draws, timer paths, scene startup effects
  and transient state. Create a gameplay-branch/decision-ownership coverage matrix.
- Define protocol, content IDs, compatibility handshake and authority contracts.
- Create development-only entitlement fixtures behind an interface; record the
  production verification dependency and reject unavailable/unverified purchases.
- Set service project/environment configuration and diagnostic limits; no secrets
  in clients. Keep SDK/transport integration isolated from gameplay policy.

Gate: compatible clients connect privately, identify accounts, reject a third
player and incompatible versions, and exchange an acknowledged state message.
Record tested dependencies, connectivity choice and remaining costs/constraints.

### M1 — Online match working: one real action (high)

- Use an owned deterministic fixture to load the same prepared roster/board on
  host and guest, coordinated through normal scene loading (never additive).
- Implement one real ground-pass selection/confirmation and its continuation.
  Include an interception/roll branch to prove authority and ownership switching.
- Introduce guest replica startup, authorized command routing, published decision
  state, outcome sequencing and a full state refresh.
- Demonstrate independent cameras, pointers and MatchStats visibility.
- Include disconnect/reconnect to this supported decision state now.
- Prove requester-owned shared pause, including a guest-requested pause, and
  ensure rejected opponent menu input cannot affect match progression.

Gate: Windows-to-macOS and macOS-to-Windows host/guest runs on different networks
produce the same outcome and next decision. Wrong-team input, duplicate confirms,
stale target clicks and repeated roll requests cannot mutate the match twice.
Inspection/menu state on the host cannot obstruct the guest. Reconnect restores
the same accepted roll/outcome, team assignment and decision.

This is the first online milestone; it is not complete online gameplay.

### M2 — Shared CreateNewGame and content agreement (medium–high)

- Add scene-authored private-create/join UI, connection feedback and retry/leave.
- Reuse CreateNewGameScene. Host proposes shared rules; each participant edits
  only their own team/name/kit and marks the current configuration ready.
- Host changes increment a revision and invalidate readiness. Both accept the
  same settings/content digest before the authority starts the draft.
- Apply confirmed international guest access, personal jerseys and distinct deck
  union; show which available deck categories both players are agreeing to use.
- Keep current kit compatibility rules, but request an owner's correction instead
  of silently changing the other account's selected jersey.
- Validate legal pool/squad size and handle missing assets, ownership failures,
  wrong/full/expired codes and a player leaving during setup.

Gate: all ownership combinations produce the exact agreed pool/team choices.
Unready or obsolete settings cannot start. No local settings JSON can override
the accepted manifest or the other player's team selection.

### M3 — Complete online drafting and Room entry (high)

- Support the existing launch draft/goalkeeper choices: regular dealing,
  applicable FreeDraft/Arcade paths and international roster selection. Inventory
  exact valid combinations in M0; do not silently exclude an advertised mode.
- Authority owns first-pick rolls, shuffled order, offers, picks and rosters.
- Validate turn, offered card ID and roster destination for clicks and drag/drop.
  Keep table sorting/filtering/inspection local.
- Publish draft progress and recover a reconnecting participant without a new
  shuffle, duplicate card, changed first pick or lost selection.
- Transfer final rosters/settings as authoritative data; local files are optional
  caches. Both report the expected scene/catalog ready before setup progresses.

Gate: every supported draft path reaches Room with identical valid rosters and
stable token IDs, including reconnect during a pick and different loading speeds.

### M4 — All gameplay decisions and transitions (very high)

Expand the ownership/replication matrix in focused batches:

1. Opening formation, toss, direction/kickoff choices, setup and restarts.
2. Movement, tackles/nutmegs, possession recovery, fouls and advantage.
3. Ground/first-time/long/high passes, interceptions, scatter and loose balls.
4. Headers, goalkeeper choices, shooting, blocks, saves and goals.
5. Throw-ins, corners, free kicks, penalties and forced interruptions.
6. Substitutions, cards, injuries, forced/emergency goalkeeper replacements.
7. Clock/decision budgets, automatic rolls, result holds, halftime/direction
   switches, extra time, shootouts, final results and agreed rematch.

The substitutions/settings batch must cover requester-only pause controls,
two distinct team confirmations (including zero-change confirmation), simultaneous
pause requests, reconnect while paused, own-team kit editing, shared assistance/
weather/time/ball revisions, and prevention of local menu callbacks resuming early.

For each row: define allowed sender(s), accepted commands, random source,
published state, next owner, disconnect state and tests. Include both attacking
directions and legal defensive decisions while the opponent has possession.

Gate: complete regular and international matches with all advertised rule and
draft combinations covered by focused evidence. All decisions, stats, clocks,
rolls and final results agree; no hidden local callback advances the game.

### M5 — Recovery and launch hardening (high; started in M1)

- Freeze new gameplay commands/timing on authority-detected disconnect; preserve
  in-progress resolution state. Define an atomic boundary for a command racing
  disconnect and expose the last accepted revision to both players.
- Stop personal gameplay submission immediately when a client loses connection;
  render waiting state until authority confirms recovery. Account for detection
  delay; neither client invents its own authoritative pause time.
- Authenticate returning accounts; restore their reserved team, current state,
  remaining budgets, pending results and command acknowledgements.
- If service membership expires, allow authorized rejoin or a replacement private
  connection to the still-live match. A reconnect API alone is not save recovery.
- Persist online checkpoints at validated draft and gameplay boundaries. Reuse
  local stable-save machinery only where its actual restore guarantees hold.
- Host application crash recovery is a separate proposed policy: restore the last
  supported durable checkpoint with both players informed and agreeing. Exact
  mid-action crash restoration and seamless transfer to another host are not
  promised. Confirm checkpoint rollback as acceptable before declaring launch
  recovery complete; otherwise budget fuller persistence in M0.
- Never silently reroll, replay unproven event logs, merge different play-throughs,
  or claim checkpoints recover unsupported active coroutines.
- Test bad connectivity, sleep/resume, app focus loss, service failure, delayed
  scene loading, clean leave, cancel, corrupt checkpoint and version mismatch.
- Support safe return to menus, agreed completion and idempotent final-result
  saving. No automatic defeat/AI takeover unless separately approved.
- Retain bounded per-match diagnostics: match/run ID, pseudonymous participant
  role, command/decision/revision IDs, roll/result IDs, state digest and errors.

Gate: run and document the agreed recovery matrix in standalone builds with both
host operating systems. No duplicate outcomes, cross-team control, unexplained
rollback or overwritten local saves. Both players can safely leave an unrecoverable
match with clear status and the last supported checkpoint retained.

## Free connectivity investigation and later hosting

Explore first, select one connection provider after a bounded compatibility proof,
and do not implement several complete stacks. Preserve small account, ownership
and transport adapters. Paid infrastructure commitments are deferred; the earlier
content purchase design remains a separate workstream, not a current prerequisite
for the online proof.

| Route | Appropriate use | Cost/operational consequence |
| --- | --- | --- |
| Epic Online Services (EOS), investigate first | Published as free across engines/stores/platforms, with peer connections and relay fallback | Requires SDK/Unity transport integration, product setup and an identity/reconnect design. Verify service limits, current terms, Windows/macOS native packaging and Unity 6000.5 compatibility. This is connectivity, not hosted game compute. |
| Unity free allowance, fallback candidate | Official pricing lists first 50 average monthly connected users and traffic allowance of 3 GiB per average CCU, capped at 150 GiB/month | Average CCU is not a peak cap; traffic and ancillary service limits also apply. Without billing, exceeding free limits can block services. Evaluate via Multiplayer Services SDK + Netcode for GameObjects + Unity Transport if selected. |
| Photon Fusion free launch allowance, comparison candidate | Official pricing lists 100 simultaneous users, one app/customer, 0.3 TB monthly traffic | A hard 100-user cap means at most 50 full two-player matches, fewer with connected users waiting in setup. The 20-user development plan is a different plan. Verify traffic-overage behavior before using a strictly zero-spend deployment. Fusion adoption has its own gameplay integration cost. |
| Direct connection / private VPN, internal tests only | No managed game-service subscription is inherently required | Router/NAT setup or another network tool may be required; home/mobile networks may prevent direct connection. Self-hosted coordination/fallback is not automatically free or operationally reliable. Not the preferred consumer experience. |
| Steam-specific connectivity, deferred | Possible optional integration after distributor selection | Not the cross-store foundation while distribution remains open. |
| Dedicated compute, deferred | Later fairness/continuity need | No researched connectivity offer supplies a free dedicated Counterattack process. Hosting needs separate compute, operations, persistence and budget. |

Recommendation: retain player-host authority and investigate EOS first against
Unity's free allowance. Epic services do not require distributing through the
Epic Games Store. This is a research recommendation, not a selected production
SDK. Free connectivity still requires implementation; do not assume EOS sends
gameplay state or offers a drop-in compatible Unity transport.

The bounded proof must cover Windows/macOS both host directions; different
networks including forced fallback routing; identity without dependence on a
selected storefront; private join codes mapped to the same match; reconnect;
SDK maintenance; traffic/limits and failure behavior. Decide the player sign-in
experience explicitly. EOS game-service identity can use supported external
identity providers; optional Epic social features have separate account needs.
Do not promise account-free cross-device recovery before testing the identity
path. Record any connector's license/maintenance and exact compatibility evidence.

Measure bytes per match and connected time during the proof. No paid service
activation is needed to compare the published offers. Verify current dashboard
requirements and quota enforcement before selecting a zero-spend deployment;
do not silently activate billing or assume free service is unlimited.

Steam provides networking and game-server APIs, but its documentation describes
developer/community hosting and explicitly warns that a hosted-server networking
mode does not necessarily mean Valve hosts the machine. Do not budget automatic
Valve-provided dedicated hosting as part of Steam distribution.

Before later dedicated hosting, measure actual server CPU/memory per match,
match length, concurrent matches, regions, bandwidth, idle capacity, logs/storage
and orchestration. No monthly price estimate is credible from the current audit.
Host authority prevents invalid guest commands but does not protect players from
a modified hosting client; dedicated trusted authority is a separate fairness step.

## Verification and implementation workflow

- Add dedicated folders as work lands: Assets/Editor/FeatureTests/OnlineMatch/
  and Assets/Tests/PlayMode/Features/OnlineMatch/, each with TASKS.md or
  Verification.md. Use proposed fixture/category names OnlineMatchProtocol,
  OnlineMatchContent, OnlineMatchSetup, OnlineMatchDraft, OnlineMatchAuthority,
  OnlineMatchGameplay and OnlineMatchRecovery; these do not exist yet.
- EditMode: permissions, compatibility, revision/readiness rules, identity and
  deduplication, deck/team/kit access, checkpoints and message validation.
- PlayMode: real input/manager entry points, scene readiness, coroutines,
  interruptions, clock/roll ownership, presentation independence, shared pause,
  two-team substitution confirmation, settings permissions and recovery.
- Include independently runnable new tests for every gameplay change, reproducing
  defects before fixes when practical. Reuse deterministic boards/rigged dice
  only in owned test fixtures; isolate singleton/event/time/save state.
- In-process tests cannot prove separate-process or internet behavior. Maintain
  standalone Windows-host/macOS-guest, macOS-host/Windows-guest, Windows/Windows
  and macOS/macOS scenarios with logs and explicit observed verdicts. Confirm
  macOS CPU/minimum OS and Windows minimum OS during M0 packaging checks.
- Proposed network test envelope: baseline plus 100–300 ms RTT, up to 100 ms
  jitter, 1–5% packet loss and brief/long disconnects. Tune and record the exact
  supported envelope before beta; do not present it as an existing guarantee.
- Use only live Unity CLI/Pipeline for Editor control; never launch Unity,
  batchmode or ordinary gameplay. Framework-controlled PlayMode tests remain
  authorized. Separate-process/manual external validation needs running builds
  and available Windows/macOS machines; record this dependency explicitly.
- Before each focused framework run, wait for compilation, inspect all loaded
  scenes, save dirty scenes through the Editor and recheck clean state. Do not
  edit loaded scenes on disk. Never mutate Canvas/MatchStatsUI/Stats layout.
- Run serial, explicit feature fixture/category filters; retain native XML and
  per-case results under TestResults/GameplayRegression/Runs/. Report compilation
  separately from passed/failed/skipped counts and name unexecuted coverage.
- At handoff, identify affected existing fixtures for the user to run (not an
  unsolicited broad suite): DraftSettingsRulesEditorTests,
  SubstitutionPanelOwnershipEditorTests, EndGamePanelOwnershipEditorTests,
  DecisionTimingPlayModeTests, DecisionTimingLiveClockPlayModeTests,
  DecisionTimingReportedIssuesPlayModeTests and affected legacy action fixtures.
  Match the exact changed branch to existing fixtures before requesting them.
- Update Documentation/Testing/RunningTests.md when commands change, migration
  evidence when equivalence changes, and GameplayReview.md for ambiguous rules.
  Never weaken rules/assertions to make the network tests pass.

## Immediate implementation backlog

1. Compare free connectivity/identity candidates and run the M0 compatibility
   proof before selecting the connection provider; confirm crash-recovery policy.
2. Complete M0 mutation/state/ownership inventory, including confirmed pause,
   substitution and settings permissions.
3. Add online context, content policy and command contracts with focused tests.
4. Prove the M1 real ground-pass/interception flow on independent processes.
5. Re-estimate remaining work using M1 evidence; proceed M2 through M5.

The first protocol batch changed no existing gameplay/scenes. The subsequent M0
compatibility check installed the EOS Unity plugin, without credentials or a
paid service.

## Primary references checked 2026-11-09

- [Unity connection/reconnection behavior](https://docs.unity.com/en-us/mps-sdk/join-session): reconnect depends on membership; removed players need rejoining.
- [Unity host migration](https://docs.unity.com/en-us/mps-sdk/session-host-migration): host election does not migrate gameplay state.
- [Steam networking](https://partner.steamgames.com/doc/features/multiplayer/networking): APIs and relayed connectivity.
- [Steam game servers](https://partner.steamgames.com/doc/features/multiplayer/game_servers): developer/community hosting and server integration.
- [Steam dedicated connectivity](https://partner.steamgames.com/doc/features/multiplayer/steamdatagramrelay): networking does not imply Valve-provided server compute.
- [Amazon GameLift Servers Unity integration](https://docs.aws.amazon.com/gameliftservers/latest/developerguide/unity-plug-in.html): managed hosting integration option; compatibility not yet verified for this project.
- [Epic Online Services](https://onlineservices.epicgames.com/): free services across stores and engines.
- [EOS relay behavior](https://dev.epicgames.com/docs/en-US/api-ref/enums/eos-e-relay-control): direct attempts and fallback routing; relay-only mode is available for testing.
- [EOS cross-store identity](https://dev.epicgames.com/docs/epic-online-services/accounts-and-social/crossplay/crossplay-introduction): game-service versus Epic-account authentication.
- [Unity service pricing](https://unity.com/products/gaming-services/pricing): current free connected-user and traffic allowances.
- [Unity quota/billing behavior](https://docs.unity.com/en-us/services/pricing-and-billing): exceeding free limits without billing can block access.
- [Photon Fusion pricing](https://www.photonengine.com/fusion/pricing): separate free development and launch allowances.
