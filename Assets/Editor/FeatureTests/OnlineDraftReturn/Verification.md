# Online Draft return to setup

- [x] Author **Back to Match Setup** to the right of **Leave session**.
- [x] Let either connected manager return both views to the existing match setup. Keep the agreed settings and clear both confirmations.
- [x] Start a fresh draft after reconfirmation. Reject packets from the discarded draft and reject a return after the opening choices commit.

Run `OnlineDraftReturnEditorTests` in EditMode and `OnlineDraftReturnPlayModeTests` in PlayMode, or use the `OnlineDraftReturn` category. These tests use serialized manager channels and the authored Draft and Create New Game scenes. They do not require an EOS lobby.

The return command uses `session-wire-v7` and `frontend-rules-v13`. Rebuild both managers at the same commit for paired verification.

Unity compilation finished with zero errors. `20261126T165057715Z` recorded 3 EditMode passes and `20261126T165128898Z` recorded 2 PlayMode passes, with zero failures or skips. Native XML and per-case progress are under `TestResults/GameplayRegression/Runs/`.

For broader transport verification, `Assets/Editor/FeatureTests/OnlineDraft/` contains `OnlineDraftChannelEditorTests` and its existing draft reconnect and lost-reply cases.
