# Online Room launch data

- [x] Transfer every drafted player/GK attribute and bench slot into real MatchManager.GameData.
- [x] Host resolves random referee once and publishes room session identity/start metadata.
- [x] Preserve opening direction/kickoff and create fresh score/statistics/event state.
- [x] Load authenticated bootstrap before local save discovery, including replica data.
- [x] Route host live logs to session storage and block accidental local-slot saves.
- [x] Execute focused tests against real Room load/persistence APIs.
- [ ] Actual full Room scene/transport/roster startup and menu integration.

The native frontend prepares a detached room-bootstrap.json after the opening.
It does not yet load Room automatically. Full Room runtime synchronization and
session-owned resumable persistence are still separate work; paired Mac deferred.

Live compilation clean. 36 focused EditMode passes at `20261115T065225277Z`,
zero failed/skipped. An earlier 32-case run also passed at `20261115T064515129Z`;
four additional stats-link cases verify that the real GameLog updates the new
runtime statistics rather than its detached bootstrap copy. Persistence cases
check local-save contents AND companion paths, and restore singleton, application
save reference and static live-log cache state even after failure. No live save
is used as a mutable fixture. All 36 cases have deferred Mac entries.

Affected existing startup: 2 EditMode passes `20261115T065323792Z`, 1 actual
normally loaded guest Room pass `20261115T065338839Z`; existing opening20 passes
at `20261115T065351807Z` and actual opening4 at `20261115T065406570Z`.
Native XML/progress is retained under TestResults/GameplayRegression/Runs.
Host referee is resolved once at Draft construction, room session ID/UTC once at
opening completion. Referee, identity and directions survive rebind. The native
frontend caches detached real GameData and a private bootstrap file after consent.
Full sessions must provide their own absolute host log path; ordinary save APIs
cannot escape into a previous local slot. Existing two-argument proof session
construction remains compatible. Actual full Room runtime and resumable online
save support are still next; no full-match or paired acceptance is claimed.
Frontend rules v5; all older frontend exports need refreshing.
Broader user-run existing checks if desired: `Assets/Editor/RoomSaveBrowserEditorTests.cs`
fixture `RoomSaveBrowserEditorTests` for offline save UI; no full suites were run.
