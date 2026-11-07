# Substitution panel ownership

- [x] Remove the RoomIntroCanvas and RulesViewer copies through Unity CLI.
- [x] Verify MainCanvas panel RectTransforms are unchanged during removal.
- [x] Restrict runtime lookup and editor authoring to MainCanvas; remove runtime reparenting.
- [x] Compile in the live Editor: completed with no errors.
- [x] Run only EditMode fixture `SubstitutionPanelOwnershipEditorTests`: 3 passed, 0 failed, 0 skipped.

Native results and per-case progress: `TestResults/GameplayRegression/Runs/20261107T134024852Z/`.

Presentation-only change; substitution rules and player selection are unchanged.
