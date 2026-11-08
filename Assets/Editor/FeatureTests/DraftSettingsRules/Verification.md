# Draft settings in Rules

- [x] Add all six draft settings as a two-column table under Getting Started.
- [x] Explain the independent phases and fixed International settings.
- [x] Rebuild the shared RulesViewer prefab without reinstalling scene UI.
- [x] Compile and run only DraftSettingsRulesEditorTests in EditMode.
- [x] Inspect table wrapping and the displayed International captions.

Compilation: completed without errors. Focused EditMode result: 3 passed,
0 failed, 0 skipped. Native XML, per-case progress and the inspected 1600x900
table capture are retained under
`TestResults/GameplayRegression/Runs/20261108T141746795Z/`.

The existing International selector behavior was verified with the real setup
scene's two dropdown references; no setup behavior changes were needed.

Internal-link follow-up: bundled English wiki chapter URLs now use local targets
directly. All authored chapter destinations exist in the same document; only the
GitHub source attribution remains external. Compilation is clean and all 4
focused tests passed (0 failed, 0 skipped). Native results and progress:
`TestResults/GameplayRegression/Runs/20261108T142223703Z/`.

This is documentation/UI presentation only; no gameplay tests are required.
