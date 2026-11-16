# Continue online development on Mac

Follow `PersonalMac.md` first on a fresh Mac. Use Mac-local Unity, GitHub and
ignored EOS configuration. Keep credentials on their original machines.
Manually open this checkout in Unity 6000.5.3f1 before Editor operations.

## Update the checkout

```sh
cd "$HOME/Documents/Projects/Personal/counterattack"
git status --short
git fetch origin
git switch codex/online-foundation-personal-mac
git pull --ff-only origin codex/online-foundation-personal-mac
git log -3 --oneline
repo_root="$(git rev-parse --show-toplevel)"
unity status --project-path "$repo_root" --format json
```

Preserve any existing local edits. Full Room scene checkpoint: `c787232`.
It contains the actual 1,900-hex pitch, 32/36 drafted actors, real kickoff and
inert guest score/time/position projection, with 16 focused Windows passes.
Subsequent native handoff work stays on the same branch; read the current
`OnlineNextSessionPrompt.md` and feature Verification.md files for evidence.

## Build and run the normal game

With the project open in the Editor, stopped and scenes saved:

```sh
bash Documentation/Setup/BuildEOSStandaloneMac.sh game
open Build/OnlineGame/Mac/Counterattack.app
```

The build includes normal menus, Create New Game, Draft and OnlineRoom.
Use the authored Host/Join controls and share the private lobby code. Both
managers choose their own identity/kit, agree settings, draft and complete the
opening coin toss before entering the full Room. Native handoff still requires
paired acceptance; normal pause/substitutions/shootout menus remain integration
work. No actual internet latency has been measured.

## One-command son's Mac verification

**Deferred until the user requests it.** Do not start a host or wait for reports
as part of independent development. Once arranged, use:

```sh
bash Documentation/Setup/VerifyOnlineMac.sh
```

This reads `OnlineMacTestManifest.json`, executes registered online fixtures,
builds all three profiles, performs available local/login/paired checks and
publishes one JSON report with stage outcomes on its verification branch.
The Windows supervisor must be explicitly arranged for paired stages.
Each new native test case needs its own manifest entry; changed fixture source
needs a refreshed definition hash. The verifier rejects stale registrations.

## Isolated proof commands

These existing helpers exercise the prepared Room proof profile:

```sh
bash Documentation/Setup/BuildEOSStandaloneMac.sh room
bash Documentation/Setup/RunEOSStandaloneMac.sh login room
bash Documentation/Setup/RunEOSStandaloneMac.sh local room
# Only during an arranged paired run:
bash Documentation/Setup/RunEOSStandaloneMac.sh guest room manual
# Or reverse roles:
bash Documentation/Setup/RunEOSStandaloneMacHost.sh room manual
```

The prepared proof uses its own scene/catalog; its reports do not establish
full normal-game internet acceptance.

## Continuation instructions

Continue from `OnlineNextSessionPrompt.md`, following AGENTS.md and unity-cli.
Full Room native handoff, pause/menu/substitutions, required keeper decisions
and in-flight snapshot poses are implemented with focused Windows evidence.
Shootout orders, owned rolls/outcomes and early/sudden-death endings now have
focused real-scene coverage. Room tiebreaker/extra-time coin toss and half transitions now have 37 focused
new native passes and 142 affected passes; full contract is Room v7.
Regular-match conclusion now adds 26 native passes and 35 affected passes;
rebuild both full-game clients for Room v8.
Direct board input adds 52 focused native passes: player/hex clicks, real physics
raycasts, original action keys, kickoff movement and inert guest intentions.
Rebuild both full-game clients for Room v9; wire framing remains v7. All 52 cases
are separate deferred entries in the Mac manifest. See OnlineBoardInput/Verification.md.
Prioritize shared settings, then presentation interpolation/transport tuning and
deferred paired acceptance. Physical two-machine input and latency remain untested.
Preserve host-only RNG/rules, authenticated semantic commands, guest inactivity,
receipt/revision checks and reconnect holds. Compile, run feature-focused native
tests and build through the already-open Editor. Commit/push verified batches.
Preserve unrelated local scene/font/settings edits. Do not launch Unity,
batchmode, ordinary editor_play, GUI automation or additive scene loads.
Continue independent implementation whenever Mac actions are unavailable.
