# Future session: goalkeeper pickup after movement

Implement the user-approved goalkeeper pickup feature in Counter Attack. Read AGENTS.md,
SessionHandoff.md, GameplayReview.md and this prompt first. Use the live Unity CLI,
owned deterministic framework fixtures and focused filters. Do not run whole suites.
Do not re-open the approved rules below.

## Approved behavior

- A teammate outfielder's kicked pass (Ground Pass, FTP, High Pass or Long Ball)
  is to the keeper's feet and never authorizes handling, even inside their own box.
- After recovering an opponent's pass outside their own box, the keeper gets
  Any Other Scenario. They may carry the ball back during a Movement Phase.
- A teammate header received outside the box also goes to feet; the keeper may
  use their remaining movement allowance to return inside. No handling outside.
- A header to feet, whether received inside or outside the box, grants eligibility
  for the deferred pickup choice. Headers originating from corners are allowed too.
- When a qualifying Movement Phase completes successfully with the keeper still
  in possession inside their own penalty area, offer pickup OR continue at feet
  BEFORE broadcasting Final Thirds. No mid-phase pickup.
- Choosing pickup permits Quick Throw [Q] or the normal goalkeeper distribution
  route [K]. Quick Throw must not trigger Final Thirds; the normal route retains
  its proper Final Thirds sequencing. Reuse existing Save and Hold controls.
- Choosing feet retains the regular movement/pass/high-pass/long-ball actions.
  Eligibility survives consecutive and subsequent Movement Phases while that
  keeper retains possession, until they pick up or pass. Outside-box phase ends
  defer the choice rather than destroying eligibility.
- A header played near the keeper may be collected during movement; only after
  the entire collection phase finishes can pickup be offered, if otherwise legal.
- A deliberate goalkeeper Drop Ball prohibits picking up again. Own-box location
  alone does not authorize hands. Do not infer eligibility from generic last-touch
  labels, stale hanging-pass strings or a recovery boolean.

## Existing direct-header behavior

The current task adds direct teammate-header reception inside the own box:
[H] hands / [F] feet. Hands uses a dedicated ordinary D6 check with no attribute:
1 scatters from the keeper, 2–6 held. Feet needs no roll. This applies to corners
and open play. Outside receptions always remain at feet. Do not add a second
catch roll when the deferred pickup is chosen; the user has authorized a pickup
choice, not an extra risk roll.

## Implementation and verification checklist

- Trace all real arrival, interception, recovery, movement-end and Final Thirds
  entry points before editing. Include headed-pass landings and loose balls.
- Introduce explicit ball-handling provenance/eligibility and clear ownership.
  Define transitions on a teammate kick, opponent touch, headed delivery,
  collection, interception, possession loss, passing, pickup and deliberate drop.
  Preserve it through temporary movement pauses and save/load where supported.
- Source provenance must survive headed-pass landing and collection; currently
  generic hanging-pass strings do not constitute a complete eligibility model.
- Make the choice a visible edit-mode-authored/reused panel with exclusive input,
  correct team ownership, AI legal actions and clean interruption/reset handling.
- Gate the normal successful phase completion before Final Thirds. Ensure forced
  stoppages, half/match ends, goals, fouls, lost possession and canceled phases
  cannot trigger a stale pickup prompt or double-complete an action.
- Add a dedicated test folder/checklist. Reproduce missing behavior; test both
  attacking directions, every kicked-pass exclusion, opponent recovery, header
  reception/nearby collection, inside/outside boundaries, repeated phases with
  feet selected, pickup then Q/K, Drop Ball lockout, reset and save round trip.
- Assert actual ball ownership, action options, phase completion count, statistics
  and Final Thirds ordering. Do not mutate live saves or designer-owned layouts.
- Compile through the live Editor, run only this feature's focused fixtures,
  retain native XML under TestResults/GameplayRegression/Runs, update rulebook
  source and its existing prefab text, and report unexecuted broader coverage.

Current deferred scope: implementation of the above provenance, persistent pickup
eligibility and phase-end choice. Do not claim these are already implemented by
the direct-header catch feature.
