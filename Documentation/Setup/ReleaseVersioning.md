# Release versioning

`ProjectSettings/ProjectSettings.asset` stores the version shown by Unity as `Application.version`.
The GitHub Actions workflow `.github/workflows/release-version.yml` advances the patch number
when a merge reaches either release branch. It updates the Unity bundle version and creates a matching Git tag:

- `master` uses the stable `0.9.x` line, starting at `0.9.3`.
- `codex/online-foundation-personal-mac` uses the online development `0.10.x` line, starting at `0.10.20`.

The two lines advance independently. Each branch's first qualifying merge receives its listed
starting version; later merges increment its patch number. The menu reads `Application.version`,
so the label displays the built game's `bundleVersion` without per-commit text edits. The workflow
ignores pushes caused only by its own version-file update.

Game version is informational. Lobby matching is governed by the handshake values shown beside
it: protocol, rules and catalog fingerprint. A protocol/rules change must still follow the online
compatibility PR declaration and bump procedure in `CONTRIBUTING.md`.
