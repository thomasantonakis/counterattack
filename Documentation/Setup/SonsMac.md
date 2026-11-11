# Counterattack setup on the family Mac

Use this checklist on the separate family Mac for the two-Mac EOS connectivity
proof. Run shell blocks in that Mac's Terminal. Complete account sign-ins and
Unity Hub/Editor actions yourself. Keep all account credentials, local EOS
configuration and EOS device-identity storage on that Mac; do not copy them from
the company Mac or the personal host Mac.

The project branch is `codex/online-foundation-personal-mac`. It contains the
two-Mac test harness from commit `033ecea9f8504d5dbe3fdc13a5ea0a3c53d7d550`.
Use the branch tip after this guide is committed; it includes that commit.
Do not launch ordinary gameplay, run whole test suites, or start Unity from
Codex. The human opens Unity manually.

## 1. Check macOS and install terminal tools

```sh
sw_vers -productVersion
uname -m
xcode-select --install
```

Finish the macOS developer tools dialog. The current Homebrew recipe supports
macOS 15 or newer; ask for help before continuing on an older release. If
`brew --version` fails, install [Homebrew](https://brew.sh/):

```sh
/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
```

Follow its printed `brew shellenv` instructions, then open a new Terminal.

```sh
brew --version
brew install git git-lfs ripgrep jq python gh
git lfs install
```

## 2. Authenticate to GitHub and check out the branch

Use a GitHub account with access to the private repository. Complete the
browser sign-in yourself; do not copy GitHub tokens or SSH keys from another Mac.

```sh
gh auth login
gh auth status
mkdir -p "$HOME/Documents/Projects/Personal"
cd "$HOME/Documents/Projects/Personal"
ls -ld counterattack 2>/dev/null
```

If `counterattack` does **not** exist, clone the branch:

```sh
git clone --branch codex/online-foundation-personal-mac https://github.com/thomasantonakis/counterattack.git
cd counterattack
git lfs pull
git branch --show-current
git log -1 --oneline
git status --short
```

If a checkout already exists, inspect its local changes before switching:

```sh
cd "$HOME/Documents/Projects/Personal/counterattack"
git status --short --branch
git fetch origin codex/online-foundation-personal-mac
git switch codex/online-foundation-personal-mac
git pull --ff-only origin codex/online-foundation-personal-mac
git log -1 --oneline
```

If `git switch` or `git pull` refuses because of local edits, stop and preserve
those edits. Do not reset, clean or force the branch. The branch tip should
contain commit `033ecea` in its history:

```sh
git merge-base --is-ancestor 033ecea HEAD
```

Exit status `0` means that required test-harness commit is present. If the
command fails, ask for help before testing.

## 3. Install Codex, Unity Hub and Unity CLI

Use the [official Codex installer](https://learn.chatgpt.com/docs/codex/cli):

```sh
curl -fsSL https://chatgpt.com/codex/install.sh | sh
brew install --cask unity-hub
```

Open Unity Hub manually. Sign in to Unity, activate a suitable license and
install **Unity 6000.5.3f1** (the exact version in
`ProjectSettings/ProjectVersion.txt`). Choose the matching Mac architecture.
Add the cloned Counterattack folder in Hub, then open it manually. Let Unity
resolve packages, import assets and compile. The repository already pins the
EOS Unity plugin and Pipeline package; no separate EOS SDK download is needed.

Install the [Unity CLI](https://docs.unity.com/en-us/unity-cli/use-unity-cli),
then open a new Terminal:

```sh
curl -fsSL https://unity.com/install.sh | bash
cd "$HOME/Documents/Projects/Personal/counterattack"
codex --version
unity --version
unity setup codex
```

Complete Codex sign-in on this Mac. Start `codex` from the checkout when using
it so the portable `.codex/config.toml` applies to this project.

## 4. Install CodeGraph and check the live Editor

```sh
curl -fsSL https://raw.githubusercontent.com/colbymchenry/codegraph/main/install.sh | sh
```

Open a new Terminal. [CodeGraph's current guide](https://github.com/colbymchenry/codegraph/blob/main/site/src/content/docs/getting-started/quickstart.md)
says `init` builds the first index:

```sh
cd "$HOME/Documents/Projects/Personal/counterattack"
codegraph install
codegraph init
PROJECT_ROOT="$(git rev-parse --show-toplevel)"
unity pipeline editors --format json
unity status --project-path "$PROJECT_ROOT" --format json
unity command editor_status --project-path "$PROJECT_ROOT" --caller codex --skill unity-cli --format json
unity command list_open_scenes --project-path "$PROJECT_ROOT" --caller codex --skill unity-cli --format json
```

Choose Codex during CodeGraph integration. Confirm the Unity CLI reports this
exact checkout as `ready`. If Pipeline is absent, first allow imports to finish
and check for compile errors or Safe Mode. Do not upgrade Pipeline just to force
a reconnect.

## 5. Enter EOS settings locally

In Unity, open **EOS Plugin → EOS Configuration**. Enter the same EOS product,
Live sandbox, Release deployment, desktop Client ID and Client Secret used on
the personal host Mac. Set a nonempty Product Version, select that deployment
and client for macOS, then **Save All Changes**. The local files under
`Assets/StreamingAssets/EOS/` are Git ignored. Do not paste their values into
chat, commit them, or copy the other Mac's device identity storage. A separate
EOS SDK download is unnecessary for this plugin.

## 6. Verify compilation and only the focused tests

Read `AGENTS.md` and `Documentation/Testing/RunningTests.md`. With Play Mode
stopped and no test running, inspect loaded scenes. If any scene is dirty, save
through Unity and recheck that it is clean before starting tests.

```sh
cd "$HOME/Documents/Projects/Personal/counterattack"
PROJECT_ROOT="$(git rev-parse --show-toplevel)"
unity command recompile --project-path "$PROJECT_ROOT" --caller codex --skill unity-cli --format json
unity command recompile_status --project-path "$PROJECT_ROOT" --caller codex --skill unity-cli --format json
unity command editor_status --project-path "$PROJECT_ROOT" --caller codex --skill unity-cli --format json
unity command list_open_scenes --project-path "$PROJECT_ROOT" --caller codex --skill unity-cli --format json
```

After `recompile_status` is `up_to_date` without errors and all loaded scenes
are clean, run only the 27-case protocol fixture:

```sh
unity command run_tests --mode editor --filter OnlineMatchProtocolEditorTests --filter_type testName --async_tests true --project-path "$PROJECT_ROOT" --caller codex --skill unity-cli --format json
unity command test_status --project-path "$PROJECT_ROOT" --caller codex --skill unity-cli --format json
```

Poll `test_status` until `completed`; `running` is not a verdict. Expected
baseline: 27 passed, zero failed/skipped. Inspect the retained native XML under
`TestResults/GameplayRegression/Runs/`.

After checking compilation and scene cleanliness again, run only the isolated
EOS Connect login fixture. Its marker is consumed once:

```sh
touch Temp/OnlineEOSConnectivity.run
unity command run_tests --mode editor --filter EOSConnectLoginEditorTests.DeviceIdentityCanLogInThroughConnect --filter_type testName --async_tests true --project-path "$PROJECT_ROOT" --caller codex --skill unity-cli --format json
unity command test_status --project-path "$PROJECT_ROOT" --caller codex --skill unity-cli --format json
```

Poll to a completed result and inspect its native XML. The EOS login fixture
shuts down EOS in that Editor process. **Manually close and reopen Unity** before
the two-Mac lobby test. Prepare the guest marker after reopening, but do not
run the guest fixture or use a lobby ID until the host's fresh lobby is live.

Report the Unity readiness state, compilation result, both actual test counts
and XML paths to Codex. Do not claim a two-Mac join, P2P or Windows proof from
these single-Mac checks.

## 7. Prepare the two-Mac guest, then wait for the host

The test code is already on this branch. After the EOS login fixture has passed
and Unity has been reopened manually on this Mac, prepare the one-use guest
marker in the checkout. Check that the Editor is ready, Play Mode stopped, no
test running, compilation complete and every loaded scene clean. Save dirty
scenes through Unity before testing.

```sh
cd "$HOME/Documents/Projects/Personal/counterattack"
PROJECT_ROOT="$(git rev-parse --show-toplevel)"
git status --short --branch
git pull --ff-only origin codex/online-foundation-personal-mac
unity status --project-path "$PROJECT_ROOT" --format json
unity command editor_status --project-path "$PROJECT_ROOT" --caller codex --skill unity-cli --format json
unity command recompile_status --project-path "$PROJECT_ROOT" --caller codex --skill unity-cli --format json
unity command list_open_scenes --project-path "$PROJECT_ROOT" --caller codex --skill unity-cli --format json
unity command test_status --project-path "$PROJECT_ROOT" --caller codex --skill unity-cli --format json
touch Temp/OnlineEOSTwoMacGuest.run
ls -l Temp/OnlineEOSTwoMacGuest.run
```

If `git pull` refuses because of local edits, stop and preserve them. Do not
start the guest fixture until the host on the other Mac has created a new
temporary lobby ID. The host shares that one-use ID during coordination; old
IDs expire after each host test. In the same Terminal, enter the current ID
locally, then run the exact guest fixture:

```sh
printf 'Current host lobby ID: '
IFS= read -r EOS_LOBBY_ID
printf '%s' "$EOS_LOBBY_ID" > Temp/OnlineEOSJoinId.txt
unity command run_tests --mode editor --filter EOSTwoMacLobbyEditorTests.GuestSearchesAndJoinsHostByLobbyId --filter_type testName --async_tests true --project-path "$PROJECT_ROOT" --caller codex --skill unity-cli --format json
unity command test_status --project-path "$PROJECT_ROOT" --caller codex --skill unity-cli --format json
```

Run the `read` line by itself and type/paste the current ID when prompted;
do not paste the whole block into Terminal at once. Poll `test_status` to a
completed result, then report the actual counts and native XML path. The
temporary ID files and consumed marker stay out of Git.
