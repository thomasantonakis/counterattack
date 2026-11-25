# Online Full Time report

Run `OnlineFullTimeReportEditorTests` and `OnlineFullTimeReportPlayModeTests`
under category `OnlineFullTimeReport`. The PlayMode fixture loads the normal
Room for each role and drives the encoded gameplay exchange with an owned
transport. The save and resume case uses a temporary checkpoint.

- [x] Compare score, scorers, player stats, ratings, Pitch and Luck in the authored report.
- [x] Cover regulation draw and resumed win, extra time, and penalty completion.
- [x] Preserve inherited and new event history, including forced-roll classification.
- [x] Preserve local navigation while history loads and after the other manager leaves.
- [x] Reject stale or corrupt parts, accept duplicates and handle unavailable history.
- [ ] Verify two real EOS players in the packaged macOS and Windows apps.

See the [Full Time plan](../../../../Documentation/Plans/OnlineFullTimeReport.md)
for native results and the paired walkthrough.
