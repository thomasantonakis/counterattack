# Authenticated Draft drag and drop

- [x] Drag an available card into the local roster through existing Pick requests.
- [x] Reorder owned roster slots through existing Swap requests, including review confirmation reset.
- [x] Support both host assignments, both clients, all draft modes and 18-player squads.
- [x] Reject opponent slots, incompatible player kinds, empty/same sources, stale connection state and duplicate pending gestures.
- [x] Restore authored transforms, hierarchy, alpha and raycast state after drop/cancel, including the final pool card becoming hidden.
- [x] Keep offline drag handlers disabled and avoid runtime visual objects.
- [x] Register every new native case separately for the deferred Mac run.
- [x] Compile, run focused native checks and refresh all Windows profiles.

Room remains v18/wire v7. This adds a client gesture over existing authenticated
Pick/Swap intentions; authority and gameplay rules are unchanged. Click selection
continues to work. Connection revision and displayed card identity invalidate stale
gestures; one pending request cannot submit another drag. The existing authored
objects move temporarily and restore their original position without reparenting.

Live Editor compilation clean, zero C# errors. OnlineDraftDragPlayModeTests has
68 distinct passed cases, zero remaining failed/skipped. Initial red reproduction
at Runs/20261116T230446877Z had 64 missing-BeginDrag failures. The implementation
run at 230954240Z passed 62/64; two disconnect fixture expectations compared
connection revisions before real rejoin. Corrected fixture reruns at 231414933Z
and 231433442Z passed; production was unchanged for that fixture correction.
Last-card cases passed at 231451816Z and 231512655Z; pending guest cases passed
at 231842371Z. Full paths share TestResults/GameplayRegression/Runs/ and contain
native results.xml plus progress. Failed diagnostic runs are retained.

Affected authored click-flow OnlineDraftScenePlayModeTests: 20 passed at
20261116T231632733Z. OnlineDraftVariantScenePlayModeTests: ten passed at
20261116T231902243Z. The 13 report-tool unit tests passed; manifest inventory and
all 68 separate additions validated. No paired or whole-suite run occurred.
For later broader user verification, run Assets/Tests/PlayMode/Features/OnlineDraft
and OnlineDraftVariants because they share the frontend presentation and setup.

Windows builds all Succeeded: normal build_1e220c6c030a (232337Z), Room
build_fca069236467 (232416Z), prepared build_82f0759de67c (232437Z), on
2026-11-16 UTC. Build-summary artifacts are under the corresponding timestamped
Runs profile folders. Zero C# errors; nine normal/two proof warnings and the known
nonfatal EOS missing-vswhere post-build diagnostic (one reported error/profile).
Packaged checks passed: 20261116T232546001Z-Room-local,
20261116T232559733Z-Gameplay-local, 20261116T232607319Z-Room-login.
Login registered liveness successfully. Local proof is not paired transport or
fresh-guest evidence. Actual internet latency and paired Mac acceptance remain
deferred until the user requests them.
