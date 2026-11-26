# EOS ground-pass command/state proof

Room/v3 follow-up: explicit catalog/roster checks, receipt watermarks and
separate animation payloads now require ground-wire-v3. The final affected
EditMode wire fixture passed 13/13 with zero failures/skips:
`TestResults/GameplayRegression/Runs/20261112T070127603Z/results.xml`.
V3 uses message identities on every fragment and a 26-byte header, still below
the EOS packet limit. Strict framing and recovery restart behavior have
separate checks; the historical v1/v2 header sizes below describe those runs.
Both applications must rebuild. Paired v1/v2 evidence below remains historical;
use Documentation/Setup/OnlineRoomProof.md for the current focused Room proof.

Shared-pause v2 follow-up: see
`Assets/Editor/FeatureTests/OnlineSharedPause/Verification.md`. The completed
paired evidence below is the earlier v1 run; it does not prove the new pause
exchange. Both applications need rebuilding for v2.

## Checklist and scope

- [x] Add bounded versioned message framing below EOS's packet-size limit.
- [x] Validate EOS peer/socket/channel identity on every received fragment.
- [x] Bind the authenticated guest to Home and the host to Away; host role does
  not override team ownership. Route both through the tested authority gateway.
- [x] Send real start, preview and confirm commands; return duplicate receipts
  and reject the guest's defending-team roll request without mutating rules.
- [x] Let the authority alone draw/resolve the real interception roll and publish
  ordered detached state. Retried defender roll must return Duplicate.
- [x] Author a separate scene with all board/display/camera/UI objects in edit mode.
- [x] Keep the guest's complete rules/input/clock/AI hierarchy inactive. Update
  separate authored display objects from the published state, without rerolling.
- [x] Add focused framing/replica/real-scene tests and execute them serially.
- [x] Build Windows and run packaged local gameplay and EOS login checks.
- [x] Build Mac and run packaged login/local gameplay checks there.
- [x] Execute the separate-network Windows-host/Mac-guest gameplay exchange;
  retain both final-state digests, reports and handoff-cleanup evidence.

This automated prepared-board probe is not an interactive Room match or full
M1 completion. Shared pause ownership, active-resolution disconnect/recovery,
physical command submission, reverse host roles, clock synchronization, full
animation replication and production roster/catalog compatibility remain open.
No local save, Room scene, designer-owned Stats layout or existing gameplay
rule was modified. Camera pan/zoom and Tab visibility are personal controls
on this new proof scene, never rule commands.

## Validation evidence

Live Windows Editor recompilation completed cleanly, zero script errors/warnings.
Final focused EditMode category `OnlineGroundPassNetwork`: **13 passed,
0 failed, 0 skipped**, native XML:
`TestResults/GameplayRegression/Runs/20261111T213927358Z/results.xml`.
PlayMode: **2 passed, 0 failed, 0 skipped**, native XML:
`TestResults/GameplayRegression/Runs/20261111T213735511Z/results.xml`.
Earlier scene-loading setup failed 0/2 because SceneManager could not load a
scene outside the shared build list. Native failure:
`TestResults/GameplayRegression/Runs/20261111T213700031Z/results.xml`.
The fixture now uses EditorSceneManager.LoadSceneAsyncInPlayMode with Single
loading, without modifying the user's build scene list or additive loading.

The scene tests exercise normal scene replacement, the actual authored manager
dependencies, real pass/interception execution and encoded/fragmented command
roundtrips. A fresh guest receives the outcome without executing rules or
rerolling. Framework gating suppresses the legacy runner; fixture cleanup
restores random/time and clears its owned singleton/ball reference. The Editor
returns to the clean Room scene on framework completion.

Windows build `build_b67c0a325e3c` reported Succeeded with 7 warnings and one
diagnostic about missing Visual Studio vswhere.exe. The application launched:

- Packaged real gameplay **passed**:
  `TestResults/GameplayRegression/Runs/20261111T214055519Z-Gameplay-local/result.json`.
  Its `gameplay-evidence.json` records rollSequence=1; final-state.json and
  Player.log retain the real outcome. Local evidence is not a two-peer pass.
- Packaged EOS login **passed**:
  `TestResults/GameplayRegression/Runs/20261111T214249833Z-Gameplay-login/result.json`.

The host/guest exchange uses reliable ordered EOS channel 8 after the existing
relay-only lobby/P2P handshake. Only the exact joined peer is accepted. Payloads
are capped at 64 KiB; fragments have 900 data bytes plus a 10-byte header. A
single bounded assembler rejects bad sizes, ordering and versions before
dispatch. Published states have their own monotonic sequence; guest replicas
reject old/conflicting publications and copy all exposed values. The paired
proof requires final SHA-256 state agreement before the guest leaves.

## Run / broader affected checks

The paired standalone run passed on 2026-11-11 with the family Mac on the
hotspot. Mac export reported Succeeded, zero errors and six warnings. The
user-supplied Mac login/local reports passed at
`Runs/20261111T214718Z-gameplay-login-67110/result.json` and
`Runs/20261111T214803Z-gameplay-local-67286/result.json`, beneath
`TestResults/GameplayRegression/` on that Mac.

Windows host **passed**, verified directly in result.json, evidence and launcher
exit 0: `TestResults/GameplayRegression/Runs/20261111T214927469Z-Gameplay-host/result.json`.
Mac guest **passed**, per its supplied completed report on that Mac:
`TestResults/GameplayRegression/Runs/20261111T215035Z-gameplay-guest-67466/result.json`.
Both reported ForceRelays, publicationSequence=7, rollSequence=1 and identical
digest `29742BBEA911125326BE16797FD90561A540D60E59111A5ADEE72F03584B98BB`.
Guest evidence reported guestRulesInactive=true. The host completed its
duplicate-confirmation, unauthorized guest-roll and duplicate-defender-roll
checks before acknowledging final-state agreement. Host final-state.json
records one completed Home pass, one failed Away interception attempt, ball
at (0,6), last touch home:10 and Home possession at continuation.
The helper removed the temporary handoff; remote directory listing confirmed
ActiveEOSGameplayLobby.json was absent. Full M1 remains incomplete for the
limitations above. No further suites ran.

Instructions: `Documentation/Setup/OnlineGroundPassNetwork.md`.
Feature-only tests: EditMode `OnlineGroundPassWireEditorTests` under this folder;
PlayMode `OnlineGroundPassNetworkPlayModeTests` under
`Assets/Tests/PlayMode/Features/OnlineGroundPassNetwork/`. Both use explicit
category `OnlineGroundPassNetwork`; no full suites were run.

Broader user-run checks, not executed in this batch:
`Assets/Tests/PlayMode/Features/OnlineGroundPass/` fixture/category
`OnlineGroundPassPlayModeTests` / `OnlineGroundPass` (existing gateway behavior),
and `Assets/Editor/FeatureTests/EOSStandaloneProof/` fixture
`EOSStandaloneProofEditorTests` (shared launcher argument parsing). The prior
packet-only host/guest remains selectable through the default packets mode;
rerun only if investigating changes to the shared transport lifetime.
