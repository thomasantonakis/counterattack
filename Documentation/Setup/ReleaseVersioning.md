# Release versioning

`ProjectSettings/ProjectSettings.asset` stores Unity's `Application.version`, shown as the game
version in the main menu. GitHub Actions tags merged PRs automatically with the next patch label:

- `master` uses the stable `0.9.x` line, starting at `0.9.3`.
- `codex/online-foundation-personal-mac` uses the online development `0.10.x` line, starting at `0.10.20`.

Tags are calculated independently from the latest matching branch tag. GitHub Actions in this
repository cannot write protected branch files or create PRs. Therefore the tag is automated, but
updating `bundleVersion` in `ProjectSettings/ProjectSettings.asset` still needs a normal PR. The
menu displays `Application.version`; its text itself needs no per-commit edit.

Game version is informational. Lobby matching is governed by the handshake values shown beside
it: protocol, rules and catalog fingerprint. A protocol/rules change must still follow the online
compatibility PR declaration and bump procedure in `CONTRIBUTING.md`.
