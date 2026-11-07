# Desktop text navigation

- [x] Compile in the live Editor: zero errors and warnings.
- [x] Run EditMode fixture `DesktopTextNavigationEditorTests` only: 28 passed, 0 failed, 0 skipped.
- [x] Verify Save As and Tactical Notes retain serialized manager references.
- [ ] Manual macOS check: Cmd+Left/Right goes to line boundaries; Option or Ctrl+Left/Right moves by word; Cmd+Up/Down goes to text boundaries. Shift extends selections. Ctrl+A/E goes to line boundaries.
- [ ] Manual Windows check: Ctrl+Left/Right moves by word; Home/End goes to line boundaries; Ctrl+Home/End goes to text boundaries. Shift extends selections.
- [ ] Manual check: typing, clipboard shortcuts, Return, Escape, and IME remain functional.

Native results: `TestResults/GameplayRegression/Runs/20261107T125343397Z/results.xml`;
per-case progress is retained alongside it. Earlier failed runs remain available.
The automated cases simulate both platform conventions in the macOS Editor;
they do not establish physical Windows keyboard behavior. No gameplay suites were run.
