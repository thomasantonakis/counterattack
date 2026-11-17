# Shooting path inspection

Dedicated diagnostic scene: `Assets/Scenes/ShootingPathInspection.unity`.
Branch: `feature/shooting-path-inspector`.

## Use

Open the diagnostic scene normally, enter Play manually, and focus the Game view.
Press **F8**, hover a shooting origin, then click to lock it. Another click picks
and locks the newly hovered valid origin; **Esc** restores hover selection.
**1–8** (or keypad 1–8) selects a target, numbered north to south at either goal.
**WASD/arrows** pan, **scroll** zooms, **R** resets the overhead view.
F8 turns the overlays off and releases the lock.
**V** cycles `Differences` (default), `Stored`, and `PhysicalDisc` views.
In Differences: blue = both paths, magenta = stored only, cyan = physical only.
Stored shows the cached blue path; PhysicalDisc shows all physical hexes in cyan.
The status gives the three set counts for the selected origin/target.

The selected origin shows a visual-only copy of the red token prefab with the
actual ball mesh/scale on top at its normal dribbling height. A second ball of the
same visible size follows the locked shooting line as the mouse moves. The mouse
ray intersects the pitch plane; the preview takes the line point at the same
world-Z row (horizontal in the overhead camera), clamped to origin/target. For a
horizontal shot, mouse X determines progress instead. Unlocked hover does not
scrub the ball. Disabling inspection hides both comparison objects.

Measured visual ball diameter is **0.35** world units, giving physical radius
**0.175**. The old `ballRadius` field is an arbitrary center-distance cutoff of
**0.6474**, not a measured physical radius. The amber rectangle is now **0.35**
wide with an additional round-ended capsule outline. This follows the true ball
footprint instead of the earlier experimental rectangle width of 0.6474.

All eight raised goal targets are colored from the shipped shooting cache:
green = connection exists, red = absent. These colors continue to describe cached
shooting eligibility, not the experimental geometry. The physical path can be
viewed for rejected targets too; it does not determine shot range or goal legality.

`DiscPathGeometry` includes a hex if the centerline segment crosses/starts/ends
inside its polygon, or the minimum segment-to-edge distance is <= **0.175**.
This is capsule-versus-convex-polygon intersection, including tangency within
0.000001 world units and rounded endpoint coverage. It uses the existing
`HexCell.GetHexCorners()` polygon in XZ, excludes the origin for comparison with
the stored path, includes the target, and orders cells along travel direction.
It reads no occupancy, match state or save data. All scene hexes are candidates
with a bounding-box rejection, avoiding the legacy candidate-window assumption.
Pitch markings and goal outlines use a diagnostic-only overlay material with
depth testing disabled, so they remain visible over every path/rectangle marker.

## Isolation

Pending gameplay decision for discussion with Colin: replacing the shared
`CalculateThickPath` and regenerating shooting/heading caches is proposed, not
implemented. The intended goal-mouth rule remains center-based: the ball's center
must cross strictly between the post boundaries derived from hex corners, even
if its disc overlaps a post. Capsule/hex overlap determines path coverage only;
it must not introduce a full-ball-clearance requirement at the posts.

- No changes to gameplay scripts, Room, shooting JSON, build settings or live saves.
- No MatchManager, GameInputManager, or CameraController in the diagnostic scene.
- Reference Ball component stays disabled; only its configured radius is read.
- Comparison token is an unpacked scene instance with gameplay scripts/colliders
  removed. Both preview balls are passive mesh renderers authored in this scene.
  Original token prefab, ball assets, and Room are untouched.
- Grid, pitch, markers, lines, labels and instruction Canvas are authored in Edit
  Mode. Selecting paths creates no runtime GameObjects.
- Inspector input is enabled only in the named diagnostic scene.
- Blue paths/eligibility are read from the same resource loaded by HexGrid.
  Changes to that cache are reflected on the next Play session.
- Scene authoring utility copies a saved Room via the Editor and switches scenes
  normally; it refuses to overwrite an existing diagnostic scene.

## Verification checklist

- [x] Compile through the live Editor; no script errors.
- [x] Inspect rendered target colors, numbering, blue path and ball corridor.
- [x] Both goals: eight targets and stable ordering.
- [x] Hover, click lock, replace lock, invalid-origin rejection, off-state input.
- [x] Rejected target keeps its corridor and clears the blue path.
- [x] Visible ball diameter used as full rectangle width; rounded capsule ends.
- [x] Physical polygon inclusion: inside/crossing, edges/corners/tangency, finite
      endpoints, zero-length/zero-radius cases and reversed travel/winding.
- [x] Shared hex edges in both attacking directions and all three display sets.
- [x] Pitch markings and goal outlines render above diagnostic overlays.
- [x] Stored blue path matches the cache exactly; authored pool fits every path.
- [x] Inspect every stored origin/target without altering cells or cache.
- [x] Runtime startup, authored visual reuse, status, disable cleanup.
- [x] Actual ball/token scales, visual-only components and origin reference.
- [x] Mouse ray/row mapping in both directions, endpoint clamps, horizontal and
      degenerate shots, unlocked/off input and comparison cleanup.
- [ ] Manual keyboard/mouse check in the focused Game view (F8, Esc, 1–8,
      pan/zoom). Automated checks call selection entry points, not OS key events.

Focused filters: `DiscPathGeometryEditorTests`, `ShootingPathInspectorEditorTests` (EditMode) and
`ShootingPathInspectorPlayModeTests` (PlayMode, category `ShootingPathInspection`).
EditMode fixture opens the diagnostic scene normally if needed and restores the
previous clean scene setup. PlayMode uses only the isolated diagnostic scene;
the framework controls entry and exit. Run serially after saving loaded scenes.

Final physical-disc comparison verification: 35 EditMode (15 geometry + 20 view)
and 1 PlayMode passed; no failures/skips in the final native runs. Compilation
separately passed with zero errors/warnings on the final reload.
Native artifacts:

- `TestResults/GameplayRegression/Runs/20261117T141010709Z/results.xml` (15 geometry)
- `TestResults/GameplayRegression/Runs/20261117T141544541Z/results.xml` (20 view)
- `TestResults/GameplayRegression/Runs/20261117T141249836Z/results.xml` (1 runtime)
- `TestResults/GameplayRegression/Runs/shooting-inspection-runtime.png`

Compilation is separate from these test counts. No full gameplay suites ran.
The earlier view run `20261117T141037510Z` had 20 passing cases but a fixture
teardown error: Unity supplied an empty SceneSetup array. Cleanup now restores a
setup only if the fixture switched scenes and the setup is valid; a framework
temporary scene falls back to a new empty scene. The final view XML above passes
at both case and fixture levels. CLI case counts alone missed this cleanup error.
Broader gameplay verification is not warranted by this isolated diagnostic tool;
normal gameplay scripts and scenes have no dependency on it.
