# Release versioning

`ProjectSettings/ProjectSettings.asset` stores the version shown by Unity as `Application.version`.
The GitHub Actions workflow `.github/workflows/release-version.yml` advances the patch number
when a merge reaches either release branch and tags the version commit created by the workflow:

- `master` uses the stable `0.9.x` line, starting at `0.9.3`.
- `codex/online-foundation-personal-mac` uses the online development `0.10.x` line, starting at `0.10.20`.

The two lines advance independently. Each branch's first qualifying merge receives its listed
starting version; later merges increment its patch number. The workflow writes a matching
`vX.Y.Z` tag and ignores later pushes caused only by the project version file.

Game version is informational. Lobby matching is governed by the handshake values shown beside
it: protocol, rules and catalog fingerprint. A protocol/rules change must still follow the online
compatibility PR declaration and bump procedure in `CONTRIBUTING.md`.
