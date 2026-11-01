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
- The panel's countdown starts after the final distance result. It dismisses before movement or goalkeeper input, retaining its history until LooseBallManager ends. An eligible outfield recovery reopens the same panel with an authored recovery section below the retained contestant and direction/distance results, using the existing Tackling rule and individual logged xRecovery. Each recovery result uses the same countdown/hold controls. No shared duel/single-roll panel opens for a header-owned recovery.
- If a goalkeeper free move interrupts movement to the deflector before scatter, the originating panel temporarily hides and releases input, then resumes with its contestant history intact when direction rolls begin. Later goalkeeper wall/Saving rolls, interceptions, save-and-hold decisions, and movement occur after the header panel has closed. They retain their existing manager inputs; no shot or Saving die is added here.
- Attack wins dismiss before headed-pass target selection or declared-shot processing. Outfield defense wins dismiss before possession change and headed-pass selection. Goalkeeper wins dismiss before save-and-hold input. A won aerial contest is not itself presented as a goal: the existing natural-roll on-target rule and shot handoffs remain authoritative.
- Half-boundary action completion and stoppage-action input follow the manager continuation, after header/scatter dismissal. The stoppage panel waits while HeaderChallengeUI is visible. Cancellation, manager disable and header cleanup/reset release panel ownership; a transferred header-deflection scatter belongs to LooseBallManager until its own completion/cancellation.

The designer-owned `Canvas/MatchStatsUI/Stats` layout is untouched.

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
