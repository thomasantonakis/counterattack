# Release versioning

`ProjectSettings/ProjectSettings.asset` stores Unity's `Application.version`, shown as the game
version in the main menu. GitHub Actions tags merged PRs automatically with the next patch label:

- `master` uses the stable `0.9.x` line, starting at `0.9.3`.
- `codex/online-foundation-personal-mac` uses the online development `0.10.x` line, starting at `0.10.20`.

Tags are calculated independently from the latest matching branch tag. Before preparing a
release or product PR to either branch, ask the user whether the game version should advance. When
they say yes, include the `bundleVersion` update in that PR and keep it aligned with the next tag.
GitHub Actions in this repository cannot write protected branch files, so version-file updates need
a normal PR. The menu displays `Application.version`; its text itself needs no per-commit edit.

Game version is informational. Lobby matching is governed by the handshake values shown beside
it: protocol, rules and catalog fingerprint. A protocol/rules change must still follow the online
compatibility PR declaration and bump procedure in `CONTRIBUTING.md`.
