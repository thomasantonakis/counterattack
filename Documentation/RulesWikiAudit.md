# English wiki and game audit

## Follow-up decisions — 2026-11-07

Implemented and focused-verified: direct headed-back-pass choice/catch, landing-ZOI
wording, digital squad wording and period-specific stoppage cap. Compilation was
clean through the live Editor. Native results: HeadedBackPassEditorTests **11/0/0**
at `Runs/20261107T210952817Z/results.xml`, HeadedBackPassPlayModeTests **26/0/0**
at `Runs/20261107T211031868Z/results.xml`, ExtraTimeStoppageEditorTests **16/0/0**
at `Runs/20261107T210821974Z/results.xml`. These are focused counts, not full-suite
validation. The dedicated feature checklists name broader user-run fixtures.
Pickup after movement remains unimplemented and belongs to the separate prompt.

The user resolved the remaining rule questions after the approved scoring batch:

- WIKI-009: headed passes are airborne until the final hex, where the ball drops.
  Eligible defenders in that hex's ZOI may attempt an interception; jumpers,
  stunned players and other excluded players remain ineligible. Existing code
  already implements landing-only attempts; clarify the bundled wording.
- WIKI-010: allow teammate headers to the keeper after open play AND corners.
  Direct reception inside the keeper's own box offers hands [H] / feet [F]. Hands
  rolls an ordinary D6 with no attribute: 1 scatters from the keeper; 2–6 held.
  Feet needs no roll. Outside-box receptions always go to feet.
- WIKI-011: keep current from-behind discipline only. Do not add last-man logic.
- WIKI-012/013: pickup after movement is a separate-session feature. The choice
  occurs before Final Thirds; Quick Throw must bypass Final Thirds. Header-to-feet
  eligibility persists through subsequent Movement Phases while the GK retains
  possession, until pickup or passing. Teammate kicked passes never authorize
  hands; deliberate Drop Ball forbids a re-pickup. Full prompt:
  `Documentation/Testing/GoalkeeperPickupFutureSessionPrompt.md`.
- WIKI-002: digital squads stay 16 or 18, with exactly two goalkeepers in either.
  The old printed draft rules are superseded. No draft gameplay change requested.
- WIKI-003: extra-time stoppage cap is now 3 per half; regulation remains 7.
  This supersedes the earlier open discussion and previous all-halves cap 7.


Baseline wiki: `counterattackgame/wiki@e9c0f9fe0d575d61368f20cfd412bc9194ca2e71`.
Scope: common rules plus Advanced variants. Reckless tackles and dependent
references are excluded. The in-game viewer ships first; gameplay corrections
require a decision on each discrepancy. No gameplay rules were changed for the viewer.

## Approved decisions — 2026-11-07

**Shared scoring floor:** user explicitly extended the zero floor to all
attribute-based scores. Compute `max(current injured attribute + all signed
modifiers, 0) + numeric dice`. Accepted jackpots stay 50 without attributes;
natural-one misses/fouls remain. `GameplayScore` centralizes this for shooting,
saves/dives, headers/control, accuracy and their probability/presentation paths.
Injuries now also reduce GK Aerial/Saving/Handling, which were previously omitted.
Confirmed by the user: difficult-angle hexes remain in the calculation and the
combined shooting penalty remains capped at -2. This decision is resolved.
See `Assets/Editor/FeatureTests/AttributeScoreFloor/Verification.md` for focused
tests and artifact paths. Further wiki gaps remain deferred.

The user reviewed WIKI-003–008 and clarified the final contracts:

- WIKI-003: retain the game's 7-action cap for all halves. Keep the suggested
  extra-time cap of 3 open for later discussion; it is not an authorized code fix.
- WIKI-004: High Pass, goalkeeper-kick/corner accuracy, Long Pass accuracy and
  Ball Control **accept jackpots as automatic success**. This supersedes the
  earlier suggestion that accuracy should count STAR as 6. Eligible duel
  jackpots remain total 50 without attributes so two jackpots tie. Checks that
  do not accept jackpots use numeric 6.
- WIKI-005: own-box goalkeeper fouls use no-card/yellow to avoid double jeopardy;
  second yellow still sends off. Outside-box fouls keep regular discipline, and
  outside-box hands retain their separate handball offense.
- WIKI-006: force foul stoppage only for straight red, second yellow or second
  injury. An existing booking with no new card must permit play-on.
- WIKI-007: retain goalkeeper priority on tied defensive team-best header scores.
  This intentional digital rule supersedes upstream first-roll priority.
- WIKI-008: allow keepers to compete outside their own penalty area with
  **Heading 0**, including after rushing there or from there. Use the keeper's
  final hex, not the ball's hex. Outside winners head away; only legal defensive
  own-box catches use Aerial and Save and Hold. Ordinary outside headers use the
  separate GK nomination in addition to two outfield players, regardless of
  position (user correction after the initial implementation). Zero Heading has
  no further distance penalty. Preserve listed Pace/Dribbling/High Pass
  attributes; zero applies to missing outfield skills, not every keeper attribute.

The earlier first-pass evidence below is retained as history. Its proposed
corrections and decision requests are superseded by these decisions. Current
implementation/verification is tracked in
`Assets/Editor/FeatureTests/ApprovedRuleDecisions/Verification.md`.
**WIKI-009–013 remain deferred:** do not implement or reopen the further gaps
until this approved batch is completed and the user resumes that discussion.

## Source review baseline — 2026-11-07, `dd87cbd`

The working tree was clean at review start. This pass compared the bundled English
text with current source and existing fixture assertions. No Editor operations,
compilation or tests were executed. These are source discrepancies, not newly
reproduced runtime failures. No gameplay or bundled-rule correction was applied.
Concurrent edits to `MatchManager.cs` and `MatchStatsUI.cs` appeared later in the
review and were left untouched. References here describe the inspected baseline;
the live working file's extra-time cap was rechecked and remained 7.
Advanced variants take precedence; reckless tackles remain excluded. Approved
penalty play-on and defensive own-goal attribution remain settled.

This is a first source-review pass, **not a completed rule-by-rule parity claim**.
Diagrams, every input continuation, both directions and scene wiring have not been
exhaustively verified. Paths below are relative to `Assets/Scripts/Managers/` unless
otherwise stated; line numbers refer to `dd87cbd`.

### Concrete source discrepancies awaiting a decision

For each item the proposed correction follows the pinned wiki. Keeping the game's
behavior instead requires an explicit digital-edition rule decision.

| ID | Wiki contract | Current implementation and consequence | Proposed disposition / focused verification |
| --- | --- | --- | --- |
| WIKI-003 | Getting Started / Added time: at most **3** added actions per extra-time half, 7 in normal halves. | `MatchManager.cs:4799` caps every period at 7. Extra time exists (`IsExtraTimeHalf`, line 4236), so totals above 3 can produce 4–7 extra-time actions. `Assets/Scripts/UI/StoppageActionsPanelController.cs:79` also always displays 7. | Use the period-specific cap in calculation and displayed rule. Verify both extra-time halves, normal halves, low/high totals and the final-action penalty exception. |
| WIKI-004 | Getting Started / Jackpot STAR Roll: automatic wins apply to shot/save, tackle/dribble, feet-dive and header **duels**; other STAR rolls count as 6. | `LongBallManager.cs:675` and `HeaderManager.cs:1954` promote STAR to 50 for long-pass accuracy and ball control. High Pass 2 + STAR thus succeeds against 9/10 although 2 + 6 fails; Dribbling 2 + STAR succeeds against 9 although 8 fails. Regular high-pass accuracy already uses the numeric roll. | Count STAR as 6 on those checks, preserving duel jackpots. Include previews/probabilities/panel copy. Verify low attributes, distance-2 control penalties and real duel STAR behavior. |
| WIKI-005 | Movement / Booking and Goalkeeper / Committing a foul: own-box GK fouls use no-card/yellow even from behind or last man. Outside-box handling is a separate straight-red offense. | `MovementPhaseManager.cs:1661`, `3044` and `3740` carry the from-behind flag into harsh yellow/straight-red leniency without an own-box GK exception. An unbooked keeper fouling from behind in their box can receive straight red. Existing `LegacyKeeperFoulFixture` harsh cases explicitly use an **outside-box** ball position. | Separate positional foul threshold from card policy. Verify own-box GK fouls, second yellows and outside-box handling controls. Preserve the separate handball offense. |
| WIKI-006 | Movement / Foul and Booking: play-on remains available unless a sending-off or mandatory substitution forces a stop. | `MovementPhaseManager.cs:3831` sets `pendingAutomaticTakeFoulAfterInjury` when an already-booked defender's new leniency roll yields **no card**. `HandleFoulProcess` consumes it at line 3503 and forces the restart. This is the older unresolved booked-defender policy, now checked directly. | Decide whether to retain this digital exception or remove that forced stop. Verify booked/unbooked no-card fouls, second yellows, dangerous fouls and second injuries separately. Penalty play-on approval did not settle this. |
| WIKI-007 | Heading / Sequence step 10: first rolled player with the tied best score represents the team's best result. | `HeaderManager.cs:1364` prefers GK on defensive ties; resolution requests it at line 1695. Outfielder 9 first, GK 9 later, attack 8 gives GK Save and Hold instead of an outfield headed clearance. `Assets/Editor/HeaderChallengePresentationEditorTests.cs:204` explicitly preserves GK preference. | Decide first-roll priority versus GK priority. Test equal defensive maxima above/equal to attack. This is separate from the approved team-to-team tie/defensive own-goal policy. |
| WIKI-008 | Getting Started / Goalkeepers and Goalkeeper / Outfield player: missing GK attributes, including Heading, are 0. Aerial is used to catch crosses. | `HeaderManager.cs:1242` and the roll-time attribute at line 63 select Aerial for every GK. `RefreshHeaderEligibility` admits attacking keepers; no role/own-box guard separates an attacking headed attempt from a defensive catch. An attacking GK can use Aerial instead of Heading 0. | Define attacking GK and outside-own-box GK eligibility/attributes. If following the wiki, use Heading for offensive headers and reserve Aerial for legal catches. Cover previews and expected-goal calculations too. |

### Missing or ambiguous flows needing targeted follow-up

These are bounded source findings, not runtime-proven defects or claims that every
possible indirect route has been excluded.

| ID | Evidence / uncertainty | Pending action |
| --- | --- | --- |
| WIKI-009 | Heading forbids headed-pass interceptions. `HeaderManager.MoveHeaderToTargetSelection` sends an empty landing to `CheckForHeaderInterception`, offering adjacent non-jumpers a 6/10+ roll **after arrival**. It does not intercept the airborne path. | Clarify whether the prohibition includes immediate landing ZOI recovery or only flight, then decide game correction versus clearer wiki wording. |
| WIKI-010 | Heading back to GK permits an open-play header to hands with natural-one scatter risk, forbids it following a corner, and allows feet instead. The reviewed target route treats a friendly GK as an ordinary receiver; no hands/feet choice, risk roll or corner-origin restriction was found there. High-pass cleanup clears `isCornerKick` before header resolution. | Verify the real target/collection path and retain corner origin if implementing the dedicated back-to-hands flow. Define hands versus feet first. |
| WIKI-011 | Movement / Professional Fouls requires harsh leniency when no other defender can reach in line with/past the tackle hex in that phase. `CompareTackleRolls` derives harsh policy only from `IsCurrentTackleDangerous`, which checks the two from-behind hexes. No last-man reachability check was found in the reviewed managers. | Reproduce a front/side last-man foul. Define remaining movement, moved/stunned defenders and both directions. Keep the GK card exception separate. |
| WIKI-012 | GK text permits pickup after the phase bringing a keeper with an outside-box recovery back into their own box finishes. The inspected attacking movement path and `BroadcastSafeEndofMovementPhase` do not offer that handoff; the Save and Hold helper handles defensive recoveries instead. No persistent drop/back-pass handling eligibility check was found in these paths. | Exercise outside recovery → return → phase end, own-team kicked balls and deliberate Drop Ball. Define eligibility before reusing the generic recovery helper. Own-box possession alone does not establish legal handling. |
| WIKI-013 | GK chapter conflicts internally: Save and Hold section says own-box loose-ball pickup is immediate, but final pickup section says only when Movement Phase ends. Quick Guide lists Movement Phase directly under distribution while full chapter describes Final Thirds before Drop Ball. | Resolve/qualify source wording before changing timing. Existing defensive pickup ends movement then enters Save and Hold; that does not settle outside-recovery return timing. |

### Source matches and remaining boundaries checked in this pass

| Rule group | Source evidence | Remaining limit |
| --- | --- | --- |
| Draft | Two GK slots and configurable squad size. | WIKI-002 digital wording and full roster lifecycle pending. |
| Penalty play-on | Choice exists unless a forced-stop flag applies. | WIKI-001 stays resolved; WIKI-006 is separate. |
| Long pass | Adjacent path blockers, teammate radius 5, defender/ZOI restrictions, 9/10 accuracy, GK Pace movement and terminal ZOI responses exist. | WIKI-004; direct-defender landings bypass Pace response, whose intended ordering needs review. |
| High pass | Regular max 15/min 3; accuracy 8+; reachable teammate targets; 3-hex team moves, scatter and GK rush nomination. | Exact “3 clear hexes” interpretation, diagram geometry, GK ordering and interruption/input paths unverified. |
| Control | Target 9+, -1 at distance 2; failure scatters from controller. | WIKI-004 and continuation execution pending. |
| Heading | Range 2, -1 at distance 2; two outfield defenders plus GK; target distances from ball. | WIKI-007–010, attacking GK count and outside-box catches pending. |
| Shooting/saving | Outside-box/snapshot/difficult-angle modifiers; third-hex save -1; moved snapshot GK -1/-2 at hex 2/3. | End-to-end blocker order, natural ones, geometry and ownership not re-executed. |
| Defensive own goals | `LooseBallManager.ResolveOwnGoalToken` prefers opposing deflector; goal path calls `LogOwnGoal`. | Policy settled; branch-specific scorer/event/reset assertions and execution pending. |
| Throw-in | Mandatory movement excludes added-action consumption; optional movement includes it; feet distance 6; head routes to header selection. | Full clock/callback lifecycle, recipient restrictions and mirrored inputs unverified. |
| Free kick/corner | Free-kick 2/2/2/2/3/2; corner's final attack group 2; Advanced 3-hex moves; corner high pass goes directly to accuracy. | Setup legality, repeated movers and GK reposition decisions need individual review. |
| Penalty | Box/arc setup exclusions; saving modifier -2. | Input lifecycle, taker eligibility, rebounds and last-action exception need focused proof. |

### Refreshed pending actions

1. Review WIKI-003–008 with the user and record game-versus-wiki decisions before
   implementation. Do not change gameplay just to satisfy existing tests.
2. Resolve WIKI-009–013 and digital squad wording (WIKI-002). Exercise real entry
   paths before promoting gap candidates to runtime-confirmed defects.
3. Expand this to individual clauses and all eight diagrams, including offside
   timing, Final Third sequencing, injuries, nutmeg stuns/interruptions, reception
   geometry and every restart continuation. Import completeness is not parity.
4. Each approved gameplay fix needs a dedicated feature test folder/checklist,
   independently runnable cases, reproduction where practical and focused live
   Editor execution. Broader checks remain user-run unless explicitly requested.
5. Separate migration follow-up: repair premature `ExpectOffTarget` readiness;
   add explicit scorer/event attribution for approved own-goal branches; run
   affected fixtures; only then retire legacy 034/038.
6. Keep synthetic FTP acceptance and opening defensive kickoff front-four policy
   on the decision list. Align `AuthorTips` with the saved RoomTip placement
   before broad presentation authoring.

Historical XML was inspected: 747/0/0 EditMode and 362/0/0 PlayMode at the recorded
20261106 checkpoints. Newer focused local results exist through
`20261107T134929742Z/results.xml` (2/0/0); they concern later features, not the
discrepancies above. Do not sum overlapping runs or claim those old suites validate
`dd87cbd`. No new test results were generated by this review.

## Earlier coverage state

All ten English pages and eight diagrams are bundled and all imported internal
hyperlinks validate. This verifies document coverage, **not gameplay equivalence**.
The full behavioral audit remains in progress. Existing scenario results remain
separate from rule-by-rule verification.

| Chapter | Implementation entry points | Behavioral audit |
| --- | --- | --- |
| Index | MainMenuManager, RulesViewer | Viewer and navigation covered by focused tests |
| Getting Started | DraftManager, MatchManager | Setup variants identified; review pending |
| Movement Phase | MovementPhaseManager, FinalThirdManager | Penalty-advantage conflict identified below; remaining rules pending |
| Passing | GroundBallManager, FirstTimePassManager, HighPassManager, LongBallManager | Advanced long-pass threshold inspected; complete action flows pending |
| Shooting | ShotManager, HexGrid | Pending |
| Heading | HeaderManager | Pending |
| Goalkeepers | GoalKeeperManager, GroundPassCommon, MovementPhaseManager | Pending |
| Loose Ball | LooseBallManager, OutOfBoundsManager | Pending |
| Set Pieces | FreeKickManager, ThrowInManager, PenaltyKickManager | Penalty-advantage conflict identified below; remaining rules pending |
| Quick Guide | All action managers | Cross-check against full chapters pending |

## Findings and decisions

### WIKI-001: advantage after a penalty-area foul

- Movement Phase / Foul says the attacker cannot decline a penalty.
- Set Pieces / Penalty says the attacker can take the penalty or play on.
- The game follows the Set Pieces variant: `MovementPhaseManager.HandleFoulProcess`
  offers A/F for penalty fouls. `ShouldForceFreeKickAfterFoul` forces a stoppage
  for a mandatory substitution or sent-off defender, rather than for every penalty.
- **Approved by the user:** the game is correct; the wiki must allow play-on for
  penalty-area fouls. The bundled Movement Phase page is corrected. An upstream
  patch is prepared in `Documentation/WikiPatches/penalty-advantage.patch`.
- Approved wiki correction: replace the mandatory
  penalty sentence with “For a foul inside the penalty area, the attacker may
  take the penalty or play on. Play must stop if the foul requires a sending-off
  or a mandatory substitution.” This keeps the existing game behavior.
- No gameplay change is required.
- Evidence: source inspection; no new foul/penalty runtime test executed in this batch.
- The existing booked-defender branch can also set
  `pendingAutomaticTakeFoulAfterInjury`. Its policy remains an unresolved item in
  GameplayReview.md. Approval of penalty play-on does not authorize changing that
  separate rule or imply advantage is available after every possible foul outcome.

### WIKI-002: squad selection variants

- Getting Started describes both one keeper plus fifteen outfielders and two
  keepers plus fourteen outfielders. These are distinct drafting variants.
- `DraftManager` reserves two keeper positions and uses the configured squad size
  (fallback sixteen), rather than supporting every tabletop draft procedure.
- Proposed game-edition wording, **pending user decision**: describe the configured
  squad size and two keeper slots as the digital setup; move tabletop drafting to
  an explicitly labeled background section. No gameplay change proposed.
- Evidence: `DraftManager` settings loading, pool size and jersey-slot allocation;
  full draft lifecycle remains to be audited.

## Advanced long-pass threshold: source evidence

`LongBallManager.AccuracyThresholdFor` uses ten for opposite final thirds and nine
otherwise, matching Passing / Long Pass's Advanced threshold. This is a source-level
match only; target eligibility, inaccuracy, goalkeeper moves and continuation still
need separate verification before the long-pass rule group is marked complete.

## Next audit batches

1. Finish setup and movement rules, including injuries, bookings, nutmegs and phase transitions.
2. Audit all pass types and their interceptions, offside timing and goalkeeper interruptions.
3. Audit aerial challenges, control, shooting, saving and loose-ball recovery together.
4. Audit set pieces and reconcile every Quick Guide transition with the full chapters.

For each rule, record source section, current code path, focused coverage, actual
test result and decision before proposing or applying gameplay/wiki corrections.
