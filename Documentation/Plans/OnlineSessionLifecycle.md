# Online session lifecycle

User scope clarified 2026-11-15: full online play includes the existing setup,
draft and Room menus. Continue implementing independently; Mac/manual acceptance
stays deferred. Each new test/parameter case needs an explicit Mac manifest row.

## Existing flow and gaps

- `CreateNewGameManager` owns team names, outfield/GK jerseys, duration, halves,
  tiebreaker, assistance, squad/draft/deck, referee, weather/lighting and ball.
  Its create button currently writes a local save and loads Draft/FreeDraft.
- `DraftManager` owns shuffled pools, goalkeeper assignment, draft-order rolls,
  alternating card batches, roster slots and transition to Room. Current UI and
  drag handlers assume local control; guest cannot independently shuffle or save.
- `PauseMenuManager` explicitly disables itself for online sessions. The proof
  exposes shared pause through separate authored controls, not the normal menu.
- `SubstitutionMenuManager` currently selects/confirms local rows for both teams.
  Preserve existing eligibility, mandatory injury/keeper replacement and limits.
- `PenaltyShootoutOrderPanelController` currently edits both columns and starts
  from one button. Each team must own its order; both confirm the same state.

## Implementation sequence and acceptance

1. Add authenticated two-seat setup agreement: own team names/outfield and GK
   kits; shared match settings; authoritative validation; immutable snapshots;
   either edit invalidates readiness; both agree before Draft. Preserve settings
   and resolve kit/name conflicts using existing validation and catalog assets.
2. Wire guest joining and authored setup controls to persistent session transport.
   Preserve identity/compatibility/recovery across scene transitions. Do not let
   local save paths, client random pools or a local scene button establish authority.
3. Integrate real Draft rolls, authoritative deck/batches and turn-owned picks,
   preferred slot rearrangement, confirmations and agreed transition to Room.
   Preserve regular/free/international modes; report any unimplemented mode clearly.
4. Replace fixed six-token initialization with the actual drafted rosters,
   settings, formation, reserves and real Room lifecycle. Guest rule/startup/save
   loops remain inactive; render the host roster and personalized UI.
5. Connect normal pause/menu presentation to requester-owned shared pause;
   reconnect holds remain independent. Classify personal presentation settings
   versus shared rules/appearance, and synchronize shared changes explicitly.
6. Add team-owned substitutions, mandatory injury/keeper replacement and emergency
   nominations through the existing rule APIs; snapshot roster/status/attributes.
7. Add team-owned shootout orders and both-team readiness, real coin toss/order,
   shootout rolls/results/continuation and final match completion.

Focused feature fixtures must exercise actual source entry points and scene
wiring where needed. Keep native evidence; do not claim complete internet play
from isolated protocol, local login/build or six-token Room proofs.

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

## Latency and state synchronization acceptance

User explicitly targets the full multiplayer game with low latency. Production
adapters already send authoritative token/ball hexes, stats, score, clock/half,
team in attack, possession, last touch, rolls and legal decisions; guest rules
remain inactive. Current proof sends boundary changes immediately, clock/budget
updates at up to 4 Hz and movement poses at 20 Hz. Pose application is currently
direct and reliable ordered; interpolation/queue prioritization need work.

Target confirmed guest actions near measured RTT plus processing/rendering,
without artificial decision delays. Add monotonic-clock RTT instrumentation and
report p50/p95, snapshot bytes, pose age and queue pressure under injected delay,
jitter/loss and real Windows/Mac relay sessions. Do not infer real latency from
local or deterministic tests. Separate reliable rules/control delivery from
superseded visual frames and avoid head-of-line waiting for obsolete movement.
Preserve state sequence, nonce/generation and roster validation while improving
presentation; never predict dice outcomes or run a second guest rules simulation.

Frontend framing now passed 22 focused cases plus 13 affected gameplay-wire cases;
see OnlineSessionTransport/Verification.md. Persistent actual session/host/join
remains next, with separate frontend and gameplay protocol identities.

Frontend authenticated channels and RTT telemetry now have 33 additional own-
feature passes (55 distinct including framing). Real serialized messages cover
peer identity, compatibility, nonce/rejoin, agreement and monotonic RTT; no actual
internet RTT is claimed. Native persistent EOS lifetime and host/join controls
remain next. See OnlineSessionTransport/Verification.md. All cases registered
for deferred Mac execution; keep full setup/Draft/Room and low latency as target.
