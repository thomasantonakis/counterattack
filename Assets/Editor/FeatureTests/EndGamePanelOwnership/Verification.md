# End Game panel ownership

- [x] Remove RoomIntroCanvas and RulesViewer copies through the live Editor.
- [x] Preserve MainCanvas/EndGamePanel/RecapPanel at 1400 × 1240 canvas units, centered.
- [x] Restrict runtime lookup and editor authoring to MainCanvas in the active scene.
- [x] Match the fallback prefab's recap dimensions to the designer-owned scene panel.
- [x] Compile in the live Editor: zero errors and warnings.
- [x] Run only EditMode fixture `EndGamePanelOwnershipEditorTests`: 3 passed, 0 failed, 0 skipped.

Native XML and per-case progress:
`TestResults/GameplayRegression/Runs/20261107T131139512Z/`.

This presentation change does not change match completion or statistics calculations.
Canvas scaling still converts the authored dimensions to screen pixels.
