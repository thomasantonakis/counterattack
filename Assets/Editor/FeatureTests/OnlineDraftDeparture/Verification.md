# Online departures, Arcade returns and lobby codes

- [x] Keep the pre-match red notice centered after a departure by either manager.
- [x] Freeze the online Room on a departure. Stack the red notice directly above the horizontally centered Pause card and keep the dialogs accessible. A host can save at a safe checkpoint.
- [x] Reuse the same selectable, read-only lobby code in connected setup and both managers' Room Pause menus.
- [x] Accept a drop onto the Arcade pool from an owned slot in the current draft stage. Keep browsing available through the first confirmation and clear confirmations if a manager returns a player. Both keeper confirmations advance to the outfield stage.
- [ ] Verify paired EOS departure, Room layout and clipboard behavior on packaged macOS and Windows.

Use category `OnlineDraftDeparture` in EditMode for the return and confirmation rules and terminal transport state. Use the same category in PlayMode for both Room roles, checkpoint writes to an isolated temporary store, and the native TMP copy handler. `OnlineDraftPolishPlayModeTests.ExplicitDepartureCentersTheConnectionPanel` exercises both managers leaving from setup and Draft. `OnlineDraftInteractionsPlayModeTests` covers real authored drag targets, browsing and per-manager tables, and `OnlineDraftVariantEditorTests` covers the draft mode matrix.

The online session uses wire v8 and rules v15. Compare the printed commits before building both players. Platform build and launch commands are in the [paired draft verification](../OnlineDraftInteractions/Verification.md#packaged-verification).

## Editor verification

Unity 6000.5.3f1 compiled with zero errors. The completed runs below had zero failures or skips. Native XML and per-case progress are in `TestResults/GameplayRegression/Runs/`.

| Scope | Passed | Run |
| --- | ---: | --- |
| PlayMode Room departure, layout, lobby code, save and exit | 2 | `20261126T212910803Z` |
| PlayMode Draft and setup departure | 4 | `20261126T213503798Z` |
| PlayMode draft interactions and spectator view | 9 | `20261126T213025646Z` |
| EditMode draft interaction rules | 6 | `20261126T213119202Z` |
| EditMode draft variant matrix | 38 | `20261126T213220907Z` |
| EditMode Arcade returns and terminal transport | 4 | `20261126T213445138Z` |
| EditMode frontend recovery | 16 | `20261126T213432085Z` |
