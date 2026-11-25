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

The paired feedback changes advance compatibility to `frontend-rules-v11` and `session-wire-v5`. Stable v9 and v10 checkpoints still load with the same player catalog. Older saved dice records use the same fallback calculation as the offline report.

## Verification checklist

- [x] Add focused `OnlineFullTimeReport` EditMode tests for host/guest report parity, stable player IDs, authoritative roll classification, realistic long-match data, and invalid or partial transfers.
- [x] Verify native Room reports for a resumed regulation win, a draw, extra time and penalties. Compare score and scorers, report tabs, player rows, ratings, Pitch and Luck. Exercise independent navigation, late or missing history, host departure and the guest's return to the main menu.
- [x] Compile and test through the live Unity CLI under [AGENTS.md](../../AGENTS.md). Keep native results under `TestResults/GameplayRegression/Runs/` and record fixture filters in the [test guide](../Testing/RunningTests.md).
- [ ] Complete a paired Windows–macOS match through the normal menu. Verify both managers can browse the same complete report, including Pitch and Luck, then return to the main menu.

For the paired Full Time check, host a saved match close to the final whistle on the original host. Let the original guest join using the new code, compare the score and player stats, then both select Ready. After Full Time, compare the Players and Ratings tabs, and choose different tabs or filters at the same time. Check that Pitch and Luck include play from before and after the save. Have one manager return to the main menu while the other continues browsing.

## Automated results

Unity 6000.5.3f1 compiled with zero errors. The focused runs passed 132 EditMode and 31 PlayMode cases with zero failures or skips on the final runs. They include the online save fixtures, regulation and extra-time conclusions, a complete shootout, the offline report and save browser, and Draft and Full Time audio. Native results are under `TestResults/GameplayRegression/Runs/`.

| Fixture | Passed | Native run directory |
| --- | ---: | --- |
| `OnlineFullTimeReportEditorTests` | 4 | `20261125T002804707Z` |
| `OnlineFullTimeReportPlayModeTests` | 5 | `20261125T002834095Z` |
| `PostMatchAudioPlayModeTests` | 2 | `20261125T002819421Z` |
