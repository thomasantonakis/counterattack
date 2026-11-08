# Fixture lighting verification

- [x] Author Room via `CounterAttack/Room/Author Fixture Lighting` in Edit Mode. Preserve stadium meshes/crowd overrides and the daylight baseline.
- [x] Compile through the live Editor; inspect compilation result and Editor.log.
- [x] Run focused EditMode fixture `FixtureLightingEditorTests` and PlayMode fixture `FixtureLightingPlayModeTests` / category `FixtureLighting` after saving and rechecking every loaded scene.
- [x] Keep native XML and progress under `TestResults/GameplayRegression/Runs/`.
- [x] Confirm setup and pause have scene-authored **Fixture lighting** dropdowns: Mid afternoon, Sunset, Late night. Inspect saved UI references and matching menu captures. Verify both settings representations round-trip with independent weather.
- [x] Preview all three in Edit Mode, repeatedly restore Mid afternoon, and test environment restoration and runtime event unsubscription. Protect the incoming scene's environment during scene transitions.
- [x] Inspect exactly 256 ordered tiles in the nested ribbon prefab: eight per section, .12 local height, 99.5% width, inward faces and barely visible gaps, including rounded corners. Top edge is at local Y 2.07, bottom 1.95, above lower-tier heads. No lights, colliders, shadow or baked GI contribution on ribbon.
- [x] Capture matching tabletop, corner, wide and low views for all presets; inspect pitch colours, markings, ribbon and stadium illumination. Both menus fit without overlapping rows.
- [ ] Final live-match review with player kits, moving shadows and target-device GPU profiling.

## Evidence and limits

Compilation is clean in the live Unity Editor. Runtime framework cases use a disposable scene and dormant in-memory MatchManager; they never load Room, use a live save, or start ordinary gameplay. They exercise the existing settings event while `Time.timeScale` is zero, old/unknown defaults, repeated switching, serialized game-data round trips and unsubscribe on disable.

EditMode: **16 passed / 0 failed / 0 skipped** at `TestResults/GameplayRegression/Runs/20261108T101104008Z/results.xml`.

PlayMode: **4 passed / 0 failed / 0 skipped** at `TestResults/GameplayRegression/Runs/20261108T101138550Z/results.xml`. Includes a real active-scene transition and confirms Room does not overwrite the incoming scene's ambient environment. The transition case was moved from EditMode to PlayMode because Unity's EditMode runner uses an unsaved scene, which disallows ordinary additive scene creation.

The first EditMode run found fixture setup/type/precision issues (9 passed, 5 failed); those were corrected without changing feature behavior. Subsequent visual checks added separate regressions for disabled layout groups and the fixed-height setup list. Final counts above cover all enabled feature cases.

The final serialized-scene audit retained all pre-existing objects and restored unrelated UI/layout serialization to its pre-task state. Only the new lighting objects, references, pause settings rows/card and setup-list spacing are changed.

Matching renders and menu captures: `TestResults/GameplayRegression/Runs/FixtureLightingVisuals/`.
Pitch previews temporarily instantiate the existing hex prefab and restore the grid and camera; pitch shading remains its existing unlit shader. Kit-dependent gameplay visuals were not exercised. No full gameplay suites were run, and no broader gameplay regressions are warranted for this visual/settings feature.

The offscreen capture's `render-stats.json` contains synchronous CPU submission samples, not gameplay frame times or GPU timings. Its UnityStats draw/triangle counters did not refresh between manual camera renders and must not be used for draw-call comparisons. Rendering all twelve views synchronously caused a graphics ring-buffer warning; target-device profiling remains unchecked above.

This is a visual/settings feature. Gameplay suites are outside this focused verification scope.

## Visual refinement after live review

- Raise the four persistent floodlights from Y 7.28 to Y 40 and aim them back at pitch center. At center pitch the horizontal-distance/height ratio is about .80, comparable to the daylight sun's `cot(50°) ≈ .84`; individual beam shadows retain their distance-dependent lengths.
- Sunset retains its low warm illumination with directional shadows disabled, allowing the raised floodlights to supply short token shadows. Daylight shadow mode is preserved and restored when switching back or leaving Room.
- Sunset ribbon changes to pale warm white `(1, .96, .89)` at brightness 1.05. Late-night ribbon colour and brightness remain unchanged.
- Ribbon height changes from .30 to .12, top aligned to the upper concrete edge; gaps shrink from 8% to .5%. Existing tile objects, ordered references and shared mesh/material are retained.
- Pause card height is 1296, with a 1056-high white settings area and a 180-high blue bottom footer. The 110-high Back button is centred in the blue footer with 35 units of blue padding above and below.

Matched real-token shadow comparisons on a disposable lit receiver, ribbon wide/close/corner views, and the pause layout are retained under `TestResults/GameplayRegression/Runs/FixtureLightingRefinements/`. Camera and temporary objects are restored/removed after captures. These are visual changes; no gameplay suite is required.

Refinement compilation: clean. Focused EditMode rerun: **16 passed / 0 failed / 0 skipped**, `TestResults/GameplayRegression/Runs/20261108T103625161Z/results.xml`. Existing assertions now verify the requested .12 height, .5% seams, concrete-edge alignment, and sun-shadow mode switching/restoration. Prior PlayMode results above were not rerun for this visual-only refinement.

Footer correction after screenshot review: Back belongs on the blue border, outside the white settings area. Shortened white background and centred the button in the thick blue footer; retained all lighting/ribbon refinements. Verified footer/button world bounds and visual capture; compilation clean. Gameplay tests are not required for this UI-only correction.
