# Header challenge presentation

`MainCanvas/HeaderChallengeUI` in `Assets/Scenes/Room.unity` is an independent, scene-authored panel. All five contestant rows, eight dice, labels, scatter/recovery sections and the bottom footer exist in edit mode. `HeaderChallengePanelController` only changes authored layout, visibility, text and cosmetic dice. Its drag handle is independent of the duel and stoppage panels. No prefab or runtime visual construction is involved.

## Authority and input

HeaderManager retains nomination limits, ordering, penalties, dice overrides, tokenScores, best-player selection and outcomes. The panel opens only in branches that actually request header dice, after nomination confirmation. Ordinary unchallenged headers without rolls do not open it. An unchallenged declared header at goal still rolls its nominated attackers as before.

The two defensive outfield slots and dedicated goalkeeper slot display actual nominations only. Team summaries use the manager-selected highest individual total. Equal teammates retain the earlier roller; a tied defending goalkeeper becomes the selected defender without increasing the team score. Jackpot values appear only as `JACKPOT`.

The title identifies `HEADER PASS CHALLENGE` or `HEADER AT GOAL CHALLENGE` from the manager's declaration. Team-best summaries sit below the contestant rolls, above the final outcome. Each tile has an authored border and a centered `Roll … - Total: …` row. Attributes omit zero-modifier copy; a distance penalty is shown as `Heading: 2-1 = 1 (distance penalty)` (or Aerial for a goalkeeper).

Only the active contestant animates, using DuelDieView's unscaled cosmetic sequence without consuming gameplay randomness. Completed dice remain visible. There is no dismissal interval between contestants. After the final outcome, the five-second unscaled countdown supports Enter hold/resume, match-clock hold and Escape dismissal. Physical and synthetic input use the same routing. The opening Enter press remains owned by nomination confirmation.

All dice-resolution modals allow camera presets 1–4 on both the number row and numpad, including Shift to save a preset, while awaiting rolls or holding results. Ctrl+Shift roll overrides retain their existing routing. Hovering visible pitch tokens updates token previews and hover names without dispatching gameplay hover or selection actions; the visible modal panel still blocks hover through itself.

## Continuation ownership and limits

- An opposing-team header tie explicitly transfers presentation ownership from HeaderManager to LooseBallManager. Direction is left and distance is right in the authored continuation section. Contestant results and the tied maxima remain visible. Direction persists during distance. These scatter dice have no jackpots, attributes or probability display.
- `LooseBallPresentationContext` remains separate from presentation ownership. A declared header-at-goal tie captures and retains `ShotResolution` before header cleanup resets the declaration, while explicitly assigning this header-deflection scatter to HeaderChallengeUI. This requested presentation change also covers header-at-goal ties; saves/handling and other ShotManager spills do not acquire a header presentation owner.
- The header-owned panel's countdown starts after the final distance result. It dismisses before movement or goalkeeper input, retaining its history until LooseBallManager ends. An eligible recovery reopens the same panel with an authored recovery section below the retained contestant and direction/distance results. Outfield interceptions show Tackling; goalkeeper recoveries show Saving, with a distance penalty on GK-wall dives. Each recovery result uses the same countdown/hold controls. No shared duel/single-roll panel opens for a header-owned recovery.
- If a goalkeeper free move interrupts movement to the deflector before scatter, the originating panel temporarily hides and releases input, then resumes with its contestant history intact when direction rolls begin. Later recovery rolls reopen the appropriate presentation owner using the actual recovery branch's attribute, including GK-wall Saving rolls. They dismiss before further movement or save-and-hold decisions; gameplay still owns those continuations.
- Attack wins dismiss before headed-pass target selection or declared-shot processing. Outfield defense wins dismiss before possession change and headed-pass selection. Goalkeeper wins dismiss before save-and-hold input. A won aerial contest is not itself presented as a goal: the existing natural-roll on-target rule and shot handoffs remain authoritative.
- Half-boundary action completion and stoppage-action input follow the manager continuation, after header/scatter dismissal. The stoppage panel waits while HeaderChallengeUI is visible. Cancellation, manager disable and header cleanup/reset release panel ownership; a transferred header-deflection scatter belongs to LooseBallManager until its own completion/cancellation.

The designer-owned `Canvas/MatchStatsUI/Stats` layout is untouched.

## Non-jumping goalkeeper: Saving and Handling

After an on-target header wins the aerial contest against outfield defenders (or is unchallenged), a goalkeeper who did not jump may save through `ShotManager.ProcessHeaderAtGoal`. The header panel dismisses first. `ShotManager` uses the existing authored `DuelResolutionPanelController` single-roll section for “Header at Goal · Goalkeeper Save”, displaying attacker/header power, goalkeeper Saving, save modifier, strict winning threshold and tie threshold. Jackpot totals display as `JACKPOT`; against a jackpot header, a goalkeeper jackpot ties rather than wins. This branch adds no shooter roll or outfield block roll.

Terminal Saving results use the shared five-second unscaled countdown, Enter hold/resume, Escape dismissal, camera presets, and physical/synthetic roll overrides. A winning save dismisses before movement and then opens “Header at Goal · Handling” with the existing strict numeric `roll < Handling` rule. Handling has no automatic jackpot success; its cosmetic die shows the numeric result without changing the logged gameplay roll. A held result dismisses before possession changes or save-and-hold choices. A tied save or failed Handling instead explicitly hands the open panel to LooseBallManager for the attached scatter below its retained result.

The goalkeeper Saving screen uses the shared panel's two authored player columns. The attacking column repeats the winning header player's name, team, Heading, modifier, frozen die and total; the defending column shows the non-jumping goalkeeper's pending Saving die, attribute, modifier and thresholds. HeaderManager passes the already-resolved roll and modifier before cleanup so the screen does not reconstruct or reroll the header. The goalkeeper result resolves only the defending die and shows its total beside the retained attacking total. No goalkeeper is retrospectively added to the aerial nominations.

Column placement follows the attacking team's current match direction, as in the aerial challenge: a left-to-right attacker appears on the left with the goalkeeper on the right; a right-to-left attacker appears on the right with the goalkeeper on the left. This uses the token's home/away identity and the current team directions, including after teams switch ends.

Ordinary ground shots and snapshots use the same two-sided layout when a goalkeeper save is next after preceding blocks. Both players are shown before the shooter rolls; only the shooter die animates, with the goalkeeper waiting. An on-target shot freezes its die and total, then immediately activates the goalkeeper's Saving die in place, without a separate result dismissal. Numeric penalty arithmetic and its reasons appear on each player's attribute line. A natural 1 ends the shot presentation without a Saving roll. Saves that become available after a GK decision display the already-resolved shot beside the new save, without consuming another gameplay roll. The existing final-result controls and loose-ball expansion then apply to the paired result.

Attacker panels show `xGoals` to two decimals. In the aerial header-at-goal challenge, each nominated attacker shows their probability next to Heading, calculated from all nominated contestants, the natural-roll on-target rule and (when the GK did not jump and has a save hex) Saving and its distance penalty. HeaderManager logs only the selected shooter's xG at the existing outcome point and passes that exact value to the later header-versus-Saving panel before cleanup. Shot/snapshot panels use the same `CalculateCurrentShotExpectedGoals()` function as their existing log event, including the captured original block attempts, shooting penalties and goalkeeper context. The labels can therefore be calculated before the displayed dice roll, while stat logging still occurs only at the original resolution points. Actual successful/failed dice do not replace attempt xG with a conditional chance or add another xG event; the value stays visible through the Saving and scatter continuation. A later GK positioning/hand-save decision can update the underlying goalkeeper context as before.

Tied Saving and failed Handling continue through the existing `ShotResolution` loose-ball context without a header presentation owner. Tied header saves expand the winning-header-versus-GK comparison; failed Handling expands the Handling check. Successful ground-shot defender blocks and tied ground-shot saves also retain their originating results. Scatter names, kit colors and logged roll actors come from `causingDeflection`, and its team owns direction/distance input; the last purposeful attacking toucher remains separate for gameplay attribution. Recovery rolls use the recovering token as their logged actor. A spill directly behind for a corner hides the Distance column, shows “Corner kick awarded”, and dismisses the direction result before movement without requesting another roll. Direction/distance and outfield recovery retain the existing authored expansion and history. Gameplay rules and header-owned scatter ownership are unchanged. Reset, save-and-hold entry, and disabling ShotManager cancel its own single-roll presentation, without cancelling a transferred loose-ball owner.

“Corner kick awarded” uses the scene-authored `ScatterOutcome` label at the bottom of the shared panel, directly above `CountdownFooter`. It never replaces the earlier save/Handling outcome. The label is hidden on the next presentation, and expanded height reserves room between the scatter dice and the result/footer. `CounterAttack > Room > Ensure Scatter Outcome Footer` authors this label in edit mode without rebuilding existing panel objects.

Goalkeeper loose-ball interceptions use Saving in both gameplay resolution and presentation; outfield players use Tackling. GK-wall dives additionally apply their existing distance penalty. Both the shared and header-owned panels show the numeric threshold, xRecovery, keeper identity and kit-colored die. The logged xRecovery for ordinary goalkeeper interceptions also uses Saving. A natural 6 still succeeds in either branch. Recovery results dismiss before collection, movement or save-and-hold decisions. Direct pickups have no dice check and do not open a recovery panel.

For shared shot loose balls, a successful block immediately continues ball movement to the blocker before the scatter section opens. After distance resolves there is no intermediate five-second countdown: the result remains visible and the recovery section expands as a recovery check is offered. External goalkeeper movement suspends the shared overlay and releases input, retaining its history for the following recovery. Terminal recovery and corner results retain countdown/hold controls. Shared recovery details and dice use equal-width columns aligned with the scatter dice; the recovery die starts at the top of its player-details group.

Tackling and nutmeg ties use `GroundDuelResolution` for the same continuous scatter/recovery flow. Neither the tied duel nor its distance result introduces a five-second wait. The completed duel remains visible while ownership transfers to LooseBallManager. If the authored handoff cannot be prepared, the obsolete duel closes immediately rather than delaying the loose ball. Decisive duel outcomes, recovery results and corner results keep their normal final-result controls; standalone pass scatter behavior is unchanged.

The `DuelResolutionUI` controller root must stay active in the saved Room scene so its `Awake` registers the singleton. Only its `Visuals` child is hidden between checks. Disabling the root suppresses all shared presentations, including High Pass accuracy. Ordinary ground shots and snapshots now use this authored panel for each blocker, shooter, Saving and Handling roll, dismissing each result before the next check, GK move or ball movement. The shooter rolls at the existing point in the interaction order; free-kick and penalty-specific roll paths remain separate.

The existing Scenario_038 header-at-goal branches cover non-rushing save loss/tie/win, hold/spill, direction-only corners, distance and recovery, plus rushing goalkeeper regressions. Their helpers now assert and dismiss completed presentations through synthetic input, including Enter hold and Escape. Focused Edit Mode tests cover thresholds, jackpot copy, authored modal input, cosmetic randomness and disable cancellation. Run these manually; Codex does not start tests.

## Edit-mode previews

Use `CounterAttack > Room > Preview Header Challenge`:

1. Two attackers vs two defenders and goalkeeper.
2. Partially completed.
3. Goalkeeper matches defensive best.
4. Jackpot team scores.
5. Tie with direction and distance.
6. Loose ball recovery with retained contest and scatter.
7. Header-at-goal tie scatter.

Previews populate existing scene objects without running gameplay. They are not tests. `HeaderChallengePanelEditorTools.EnsurePanel()` is the guarded, one-time edit-mode authoring entry point used through Unity CLI; it retains existing designer objects on subsequent calls.

## Changed files

- `Assets/Scenes/Room.unity`: authored HeaderChallengeUI, MainCanvas child link and HeaderManager controller reference.
- `Assets/Scripts/UI/HeaderChallengePanelController.cs` and `.meta`: contestant/scatter display, palettes, drag bounds, countdown and input ownership.
- `Assets/Scripts/UI/HeaderChallengePanelDragHandle.cs` and `.meta`: independent dragging.
- `Assets/Editor/HeaderChallengePanelEditorTools.cs` and `.meta`: guarded scene authoring and header, scatter and recovery edit-mode previews.
- `Assets/Editor/HeaderChallengePresentationEditorTests.cs` and `.meta`: focused Edit Mode coverage for held-modal camera presets, roll overrides, scatter ownership, recovery history, cosmetic dice and saved-scene bindings.
- `Assets/Scripts/Managers/HeaderManager.cs`: presentation hooks, authoritative guidance and cancellation guards; existing comparison and outcome rules remain intact.
- `Assets/Scripts/Managers/LooseBallManager.cs`: explicit header-deflection presentation handoff, retained recovery history, and release on loose-ball completion.
- `Assets/Scripts/Managers/GameInputManager.cs`, `MatchManager.cs`, `RoomDecisionManager.cs`: physical/synthetic input, match-clock hold and decision scheduling integration.
- `Assets/Scripts/Managers/CameraController.cs`: shared camera-preset handling with number-row and numpad mappings, including Shift to save presets during modal presentation.
- `Assets/Scripts/UI/GameDebugMonitor.cs`, `PauseMenuManager.cs`, `StoppageActionsPanelController.cs`: instruction suppression, exclusive Escape and stoppage-panel coordination.
- This document: ownership, preview instructions and presentation limits.

The scene's pre-existing local edits are preserved. Existing serialized objects remain byte-identical to the pre-task scene except for the MainCanvas child list and HeaderManager's new controller reference. Generated font-cache changes were removed, and the local dice-roll inventory remains excluded through `.git/info/exclude`.
