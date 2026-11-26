# Explicit guest Room startup

- [x] Explicit scoped online host/guest context; offline remains the default.
- [x] Early authored authority-root gate, outside designer Stats and personal cameras.
- [x] Guard match bootstrap, pass entry points, synthetic input and persistence.
- [x] Author references through live Editor and compile.
- [x] Execute focused category OnlineRoomStartup (EditMode and PlayMode).
- [x] Verify normal loaded guest lifecycle with personal presentation intact.
- [x] Connect the controlled roster/publications and manual/recovery exchange in the owned Room action proof.

Compilation separately clean. EditMode: **2 passed, 0 failed, 0 skipped**,
`TestResults/GameplayRegression/Runs/20261112T054524003Z/results.xml`.
Final normal-load PlayMode: **1 passed, 0 failed, 0 skipped**,
`TestResults/GameplayRegression/Runs/20261112T064316462Z/results.xml`.
The guest leaves all 28 explicit authority roots inactive, cannot bootstrap a
save or synthetic pass, and retains the personal camera and inspection object.
Persistence tests reject save/run-copy operations before path resolution.
The guest cannot instantiate or advance an authority timing service even when
the explicit authority-time API is called on its inactive MatchManager.
Host online startup also suppresses legacy local pause and intro handling;
the owned semantic controls route Escape through shared authority. Offline
has no online context and retains its existing behavior.

Room was saved through the live Editor. Only the new startup object, component,
transform and root reference (74 lines) are staged from Room.unity. Incidental
Unity UI prefab/layout serialization is retained in the worktree and excluded
from the commit; no loaded scene was rewritten on disk. The designer Stats
layout is not part of the authored gate or task changes.

Paired online Room acceptance remains external. The six-token Room action proof
does not claim complete online Room, full roster/draft or all mutation flows.
