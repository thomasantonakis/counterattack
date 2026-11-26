# Counterattack on your personal Mac

Prepared 2026-11-09; verified on the personal Mac 2026-11-10. Follow in order on the PERSONAL Mac. These are installation
instructions, not a script to run blindly on the company laptop. No company
network bypass, company credentials or machine-wide configuration transfer is
needed. Keep working on `codex/online-foundation-personal-mac`, not `master`.

## 1. Check the Mac and install command-line tools

```sh
sw_vers -productVersion
uname -m
xcode-select --install
```

Finish the macOS installation dialog; if the tools are already installed, continue.
The current Unity CLI documents macOS 14+; current Homebrew documents macOS 15+
support. Use a supported macOS release for this recipe. For Apple Silicon use
native arm64 Terminal and Editor; do not start by adding Rosetta or Intel tooling.
An Intel Mac needs the matching Intel packages. Confirm the exact Editor supports
the destination OS/hardware in Unity Hub before investing in the rest of setup.

Install Homebrew if `brew --version` does not work:

```sh
/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
```

Follow the installer's **Next steps**, including its `brew shellenv` instructions
for your shell profile. Homebrew normally lives at `/opt/homebrew` on Apple Silicon
and `/usr/local` on Intel. Open a NEW terminal, then install the everyday tools:

```sh
brew --version
brew install git ripgrep jq python gh
git --version
rg --version
jq --version
python3 --version
gh --version
```

These cover Git, fast source search, JSON, local utility scripts and GitHub login.
Node/npm and a standalone .NET SDK are not needed by the installation route below.
Unity performs C# compilation; do not use `dotnet build Assembly-CSharp.csproj`.

## 2. Authenticate to GitHub and clone the correct branch

Use your own GitHub account that has access to `thomasantonakis/counterattack`.
HTTPS avoids having to copy this laptop's SSH private key:

```sh
gh auth login
gh auth status
```

Choose GitHub.com, HTTPS and browser sign-in; allow GitHub CLI to authenticate Git
when prompted. Set your own commit name/email locally or globally as you prefer.

For a fresh checkout:

```sh
mkdir -p "$HOME/Documents/Projects/Personal"
cd "$HOME/Documents/Projects/Personal"
git clone --branch codex/online-foundation-personal-mac https://github.com/thomasantonakis/counterattack.git
cd counterattack
git branch --show-current
git status -sb
```

If you already cloned the project, preserve any existing edits before changing
branches. In that checkout, use `git fetch origin`, then switch to the branch;
do not clone into an existing project directory or use a destructive reset.

The migration snapshot has no tracked `.gitattributes` or Git LFS pointers, and
Git LFS was not installed on the source machine. Therefore LFS is optional here.
If later commits add LFS-managed assets, install it BEFORE pulling those assets:

```sh
brew install git-lfs
git lfs install
git lfs pull
```

Git carries Assets, Packages, ProjectSettings, tests and instructions. Unity
rebuilds Library, Temp and other generated directories. Do not copy them from
the company machine. Local saves/screenshots and most historical test artifacts
are not included; copy only your personal game data separately if needed.
The focused 27-case online protocol run is included explicitly in this branch.

## 3. Install Codex and sign in again

Use the current official standalone installer:

```sh
curl -fsSL https://chatgpt.com/codex/install.sh | sh
```

Open a new terminal and check:

```sh
codex --version
```

You can also install the desktop application from the official Codex/ChatGPT
download page and add this checkout as a project. For terminal use, run `codex`
inside the repository and complete its ChatGPT sign-in flow. Trust this personal
checkout when prompted so project instructions/configuration can load. Sign in
fresh; do not copy `~/.codex/auth.json`, browser sessions or corporate settings.

## 4. Install Unity Hub and the project's exact Editor

```sh
brew install --cask unity-hub
```

Open Unity Hub yourself. Sign into your Unity account and activate an appropriate
license through Hub. Install **6000.5.3f1**, matching
`ProjectSettings/ProjectVersion.txt`. The previously pinned 6000.5.2f1 could
not start the existing Pipeline server; the personal-Mac setup therefore moved
to the next 6000.5 patch release. Do not upgrade to a newer Editor without
checking the project version and Pipeline compatibility.
If Hub does not list it, use the official Unity download archive's Hub link for
that exact version.

Choose the correct Mac architecture. Include the Mac build support offered for
your intended backend and Windows build support when available for that Editor.
Windows packaging/testing remains a separate check; installing the Mac Editor
does not prove that every Windows build module or a Windows test machine exists.
Full Xcode/iOS/Android/WebGL tooling is not needed for the first desktop proof.

Add the cloned folder through Hub and open it MANUALLY. Let package resolution,
initial imports and compilation finish. Do not open two copies of this checkout.
Codex must not launch Unity, batchmode or ordinary gameplay for this project.

## 5. Install Unity CLI

Use the official installer (one installation route only):

```sh
curl -fsSL https://unity.com/install.sh | bash
```

Open a new terminal and run:

```sh
command -v unity
unity --version
unity self-update --check --format json
```

If an update is available, update with `unity self-update` as the machine owner.
Follow installer PATH instructions if the command is missing. Do not assume the
source laptop's `/Users/t.antonakis/.unity/bin/unity` exists on this machine.
Avoid installing a second Unity CLI through Homebrew over the standalone one.

## 6. Verify the existing Pipeline connection

From the cloned repository, in each new terminal/session:

```sh
PROJECT_ROOT="$(git rev-parse --show-toplevel)"
unity pipeline editors --format json
unity status --project-path "$PROJECT_ROOT" --format json
unity command editor_status --project-path "$PROJECT_ROOT" --caller codex --skill unity-cli --format json
unity command list_open_scenes --project-path "$PROJECT_ROOT" --caller codex --skill unity-cli --format json
```

Success means the instance points at this checkout, is `ready`, and reports no
compilation/reload in progress. `editor_status` should say Play Mode is stopped
before automated tests. This branch already pins `com.unity.pipeline` in Packages;
allow Unity Package Manager to restore it. Do NOT reinstall/upgrade Pipeline
merely because the connection is absent. The source machine used Pipeline
0.8.0-exp.1 and CLI 1.0.0-beta.17; current CLI may suggest newer command names.

`unity pipeline editors` is the current discovery command; older instructions
may mention the removed `unity pipeline list`. Use `unity command --group-by flat`
with the project/metadata flags above to inspect this Editor's command schemas.

If no server connects, first inspect compilation/Safe Mode, allow imports to
finish, and manually start/restart the Pipeline server using its installed Editor
UI. Then recheck. A running Editor process alone is not a working connection.
If an Editor command times out while discovery responds, inspect logs for a modal;
resolve dialogs manually, preserving unsaved work. Never use GUI automation,
kill Unity or overwrite a loaded scene on disk.

## 7. Restore Unity agent skills and optional MCP tools

```sh
unity setup codex
```

This installs/configures Unity's agent plugin/skills; review its prompts. Restart
Codex afterward. The repo already contains `.agents/skills/unity-cli/SKILL.md`.
If plugin installation is unavailable and the tool offers a skills-only route,
use its supported fallback or `unity skill install codex`.

`.codex/config.toml` now uses `command = "unity"` and `args = ["mcp"]`, rather
than this machine's username/path. Start Codex in the project root; keep `unity`
on the PATH visible to that process. For the desktop app, fully restart it after
installation. If its MCP process cannot resolve PATH, configure your actual
absolute CLI path locally; do not commit a username-specific replacement.
Shell-based Editor control uses CLI directly and does not depend on MCP startup.

Avoid adding a duplicate global Unity MCP entry for the same project. Inspect
`unity mcp configure codex --dry-run` if a client still needs configuration.
Other connected apps/plugins require fresh sign-in on the new device; Git does
not transfer their permissions, sessions or integration credentials.

Only migrate personal game-specific custom skills if desired. They lived under
`~/.codex/skills` on the source machine and are separate from this Git repository;
copy only the relevant skill folders after inspecting their supporting files.
Do not copy the whole Codex home or unrelated company skills. Unity's official
plugin restores the standard Unity skills without copying its old plugin cache.

## 8. Install CodeGraph for equivalent code navigation

```sh
curl -fsSL https://raw.githubusercontent.com/colbymchenry/codegraph/main/install.sh | sh
```

Open a new terminal. From the project root:

```sh
codegraph --version
codegraph install
codegraph init
codegraph index
codegraph query GameInputManager
```

Choose Codex in the interactive integration setup. This tool bundles its runtime;
the shell installer avoids a separate Node/npm installation. The source machine
used CodeGraph 0.9.5; inspect the installed version's help if names change.
The `.codegraph/` index is intentionally ignored by Git and must be rebuilt here.
Inspect any changes the installer proposes to AGENTS.md or client configuration.
Missing CodeGraph should not prevent the Unity connection; `rg` remains available.

## 9. Verify compilation and the focused foundation tests

Read AGENTS.md and `Documentation/Testing/RunningTests.md` first. With the Editor
open, Play Mode stopped and no pending tests:

```sh
PROJECT_ROOT="$(git rev-parse --show-toplevel)"
unity command recompile --project-path "$PROJECT_ROOT" --caller codex --skill unity-cli --format json
unity command recompile_status --project-path "$PROJECT_ROOT" --caller codex --skill unity-cli --format json
unity command editor_status --project-path "$PROJECT_ROOT" --caller codex --skill unity-cli --format json
unity command list_open_scenes --project-path "$PROJECT_ROOT" --caller codex --skill unity-cli --format json
```

With the pinned Pipeline 0.8.0-exp.1, the top-level `unity recompile` CLI may
look for an unavailable `script recompile` name. The direct `unity command
recompile` above succeeded on this Mac; verify `recompile_status` is
`up_to_date` with no errors before tests.

If any loaded scene is dirty, save through the Editor, then recheck it is clean:

```sh
unity command save_all --project-path "$PROJECT_ROOT" --caller codex --skill unity-cli --format json
unity command list_open_scenes --project-path "$PROJECT_ROOT" --caller codex --skill unity-cli --format json
```

After compilation/reload completes and scenes are clean, run ONLY this fixture:

```sh
unity command run_tests --mode editor --filter OnlineMatchProtocolEditorTests --filter_type testName --async_tests true --project-path "$PROJECT_ROOT" --caller codex --skill unity-cli --format json
unity command test_status --project-path "$PROJECT_ROOT" --caller codex --skill unity-cli --format json
```

Poll status until completed; `running` is not a verdict. Expected baseline is
**27 passed, zero failed/skipped**. Native XML/per-case evidence is written under
`TestResults/GameplayRegression/Runs/`. Most new runs remain local/ignored.
No full suite is needed for environment setup. Inspect compile errors and test
failures directly; never ask Codex to start ordinary gameplay as a substitute.

## 10. Resume online development

The EOS product/client are ready, and EOS Unity plugin 6.2.0 imports and compiles
in Unity 6000.5.3f1. See `Documentation/Plans/EOSSetup.md` for the recorded proof.
One local EOS Connect Device ID login passed; no second-identity, relay or
Windows/macOS internet proof is complete yet.
Do not paste credentials in chat. Current gameplay remains Hotseat/PvC; the new
online code is an isolated protocol foundation, not a playable online mode.

Start Codex in the repository and send:

> This is Counterattack on my personal Mac, branch
> codex/online-foundation-personal-mac. Read AGENTS.md,
> Documentation/Setup/PersonalMac.md, Documentation/Plans/OnlineMultiplayer.md,
> Documentation/Plans/EOSSetup.md and
> Assets/Editor/FeatureTests/OnlineMatch/Verification.md. Verify the live Unity
> connection and continue the M0 free-connectivity proof. Do not start Unity,
> ordinary gameplay or full test suites. My EOS portal setup is [ready/not ready].

Conversations and credentials do not travel with Git. The committed documents
are the handoff, including shared Escape pause, two-team substitution confirms,
own-team jerseys and synchronized assistance/weather/time/ball choices.

## Sources and maintenance

Installation instructions checked 2026-11-09; network services and installers can
change. Use their current official instructions if a command differs:

- [Homebrew](https://brew.sh/)
- [GitHub CLI authentication for Git](https://docs.github.com/en/get-started/git-basics/caching-your-github-credentials-in-git)
- [Codex CLI installation and login](https://learn.chatgpt.com/docs/codex/cli)
- [Unity CLI installation and supported OS](https://docs.unity.com/en-us/unity-cli/use-unity-cli)
- [Unity download archive](https://unity.com/releases/editor/archive)
- [CodeGraph installation](https://github.com/colbymchenry/codegraph)
