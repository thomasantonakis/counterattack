# Kit dropdown height

- [x] Keep regular home/away popup and viewport at the authored list-window height in both setup modes.
- [x] Preserve full-list behavior for goalkeeper and international selectors, template geometry and selected value.
- [x] Compile and run only KitDropdownHeightPlayModeTests (10 cases).

Visual presentation correction only; no gameplay suite. Uses the authored kit
dropdown prefab and TMP popup, reproduces a shortened generated list, then checks
both modes against the authored template height. No live saves are touched.

Open Editor compilation: clean. Focused result: 10 passed, zero failed/skipped.
Native XML and per-case progress retained under
TestResults/GameplayRegression/Runs/20261111T052608877Z/.
Both setup modes retain a 512-unit regular list window (eight 64-unit rows).
Goalkeeper and international lists still use their existing option-count height.
No scenes or prefabs changed; only existing generated popup sizing was corrected.
