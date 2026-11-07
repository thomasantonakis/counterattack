# English wiki and game audit

Baseline wiki: `counterattackgame/wiki@e9c0f9fe0d575d61368f20cfd412bc9194ca2e71`.
Scope: common rules plus Advanced variants. Reckless tackles and dependent
references are excluded. The in-game viewer ships first; gameplay corrections
require a decision on each discrepancy. No gameplay rules were changed for the viewer.

## Coverage state

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
