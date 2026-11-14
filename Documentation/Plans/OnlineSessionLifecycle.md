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
