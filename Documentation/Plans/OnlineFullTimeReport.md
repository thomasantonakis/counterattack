# Online Full Time report implementation plan

Show the same complete Full Time report to both online managers in the normal `Room` scene. Reuse the authored panel from `master`, including Match Stats, Players, Pitch, Luck and Ratings. The host owns the final result and report data. Each manager can browse independently and return to the main menu.

## Integrate the authored report

- Integrate the report from `master` through a feature or integration branch. Resolve the `Room.unity` and `EndGamePanelManager` changes while preserving the online Room bootstrap and offline Full Time behavior. Reuse the authored panel, prefab and `PostMatchReportController`. Make loaded-scene changes through Unity CLI and preserve the designer-owned live Stats layout.
- Use the terminal publication in `OnlineGroundPassBoard` and its call to `EndGamePanelManager.ShowPublishedOnlineConclusion`. Open the report once the host commits the result. Cover regulation, extra time and completed penalties. Preserve the regulation score, any extra-time and penalty tallies, scorer lines and **Back To Main Menu**. Keep the shootout controls available through the final kick's resolution.
- Keep tabs and filters local. Block new match commands and timer progression after completion. If one manager leaves immediately, let the other browse any completed report already received. Complete or acknowledge report delivery before normal peer teardown where possible, with a clear fallback if the host disconnects during transfer.

## Deliver complete report data

- Inventory the inputs to `PostMatchLeaderboard`, `PostMatchPitchData`, `PostMatchLuckData` and `PostMatchRatingData` on `master`. Reuse already published team/player stats, scorer lists, roster identities and directions where sufficient. Pitch derives positions and periods from match events. Luck needs authoritative dice events, including forced-roll classification. Provide report inputs separately from the guest's live event log.
- Capture the report after the host finalizes stats and records the last gameplay event. Share the report calculations across host, guest and offline paths. Use a versioned payload containing compact report-ready data or the narrow event projections those calculations consume. Carry stable player IDs through substitutions and keep account IDs, save paths, private selections and diagnostics out of the payload. When save/resume is available, include the checkpoint's inherited history exactly once.
- Bind the report to the match ID and final publication revision. Send it reliably with bounded transfer and payload sizes, separate from recurring gameplay snapshots. Validate completeness and handle duplicate, stale, interrupted and oversized transfers. Show the confirmed score and basic recap during transfer, and explain which remaining tabs are unavailable if it fails. Check mixed-build behavior under [CONTRIBUTING.md](../../CONTRIBUTING.md) and bump the protocol if older builds cannot handle required messages.

## Verification checklist

- [ ] Add focused `OnlineFullTimeReport` EditMode tests for host/guest report parity, stable player IDs, authoritative roll classification, realistic long-match data, and invalid or partial transfers.
- [ ] Add PlayMode coverage in the authored Room for a regulation win and draw, extra time and a penalty shootout. Open all tabs on both roles, compare scorers, leaderboard rows, heatmap filters, luck and ratings, and verify independent navigation and return to menu. Cover terminal state arriving before report data, duplicate transfers and either manager leaving early.
- [ ] Use the live Unity CLI under [AGENTS.md](../../AGENTS.md). Report compilation separately from test counts and retain native results under `TestResults/GameplayRegression/Runs/`. Identify existing offline `PostMatchReport`, `PostMatchPitch` and online conclusion coverage for broader verification.
- [ ] Complete a paired Windows–macOS match through the normal menu. Verify both managers can browse the same complete report, including Pitch and Luck, then return to the main menu.
