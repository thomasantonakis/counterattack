# Online session lifecycle

Current checkpoint (2026-11-16 local): full drafted Room, shared pause, normal
and required keeper substitutions, emergency nomination and in-flight snapshot
poses are implemented. Shootout orders now use authenticated team-owned swaps,
both confirmations, roster reconciliation, real shot/save/deflection adapters,
results and early/sudden-death completion. The new feature has 55 native cases;
see OnlineShootout/Verification.md for actual scope/evidence and final UI checks.
Room tiebreaker/extra-time coin toss and real half transitions now have 37 own
native passes and 142 affected passes, with null-preserving replica copies and
explicit opening-choice projection. See OnlineRoomCoinToss/Verification.md.
Regular-match conclusion, shared settings and direct board interaction remain;
local fixtures do not establish complete normal-match internet acceptance. Earlier
progress paragraphs below are historical checkpoints. Mac remains deferred.

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
