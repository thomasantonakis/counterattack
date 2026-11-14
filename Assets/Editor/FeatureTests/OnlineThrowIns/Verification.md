# Online throw-ins

- [x] Owned taker, mandatory/optional protected movement and execution choices.
- [x] Completed six-hex feet pass and header handoff, preview/confirmation.
- [x] Holds, role/guest/stale/repeated-selection boundaries.
- [x] Actual serialized Room managers for feet and head in both directions.
- [x] Individual deferred Mac case entries.

Compilation separately clean. Category `OnlineThrowIns`:
`OnlineThrowInPlayModeTests`: **10 passed**, zero failed/skipped, at
`Runs/20261114T224107950Z/results.xml`.
`OnlineRoomThrowInPlayModeTests`: **4 passed**, zero failed/skipped, at
`Runs/20261114T224255780Z/results.xml`.
Affected `LegacyThrowInPlayModeTests`: **5 passed**, zero failed/skipped, at
`Runs/20261114T224402832Z/results.xml`.

Real taker movement, mandatory/optional source movement phases and protected
taker exclusion precede the execution choice. Feet throws complete to the real
receiver and restore the normal pass range. Head throws reach the real header
manager, with automatic single-target or two-click multi-target selection.
The source keyboard and online head choice share one semantic method. Selection
in flight cannot be repeated, and movement-to-choice transitions expose no
ordinary actions early. Source scene/prefab visuals and designer Stats layout
were preserved. No raw-key remote input.

The Editor was closed during preparation, so missing-command reproduction could
not run before implementation. Startup compile errors in the new fixture's local
manager references were fixed; live compilation and all focused cases then passed.
No assertions weakened. Header completion and occupied-spot push choices remain
separate coverage, not claimed from this handoff test. Broader user-run coverage:
`Assets/Tests/PlayMode/Features/OnlineMovement/` category `OnlineMovement` and
`Assets/Tests/PlayMode/Features/OnlineHeaders/` category `OnlineHeaders`.

Mac execution, paired acceptance, full normal Room and full suites remain deferred.
The latest Windows exports precede penalties/cache and throw-in changes.
