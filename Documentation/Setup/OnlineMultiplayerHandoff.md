# Multiplayer continuation handoff — 2026-11-19

## Start with one bounded batch

Read AGENTS.md, this file, then only the sources needed for the selected batch.
Do not reread the entire conversation or historical handoff by default.
First session: implement batch A only, validate, commit/push, update this file
and stop with a concise handoff. Later batches require a new request.
Keep explanations short; avoid speculative refactoring and repeated verification.

## New user requirements

- Windows AND macOS builds must start fullscreen. Prefer native borderless/
  fullscreen-window behavior where supported; verify Unity's actual platform
  behavior rather than assuming Windows modes map identically to Mac.
- Windowed mode must remain available and resizable. Windows maximize/restore
  and Mac resize/zoom must work. Switching modes must preserve readable UI,
  pointer mapping, raycasts and game state. Startup remains fullscreen on later
  launches unless the user changes this policy.
- Mac hosting is unfinished/unaccepted. Deliver Mac host + Windows guest through
  normal UI. Generic host code and a Mac host shell script are not acceptance.
- Online Host/Join should resemble Singleplayer/Hotseat Create New Game / Load
  Game navigation: a dedicated authored screen/scene matching Main Menu, with
  Host, Join and Back. Inspect/reuse CreateLoadRoom styling/navigation patterns.
  Preserve lobby logic and setup/Draft/Room transitions. This request does not
  introduce online save/load support.

## Baseline and evidence

Branch: codex/online-foundation-personal-mac. Pre-handoff HEAD: 949dc8a.
Resolve the actual checkout with git rev-parse --show-toplevel.
Windows root: C:/Users/thomas/Documents/Projects/Personal/counterattack.
Git commit/push authorized; preserve unrelated working changes.

Normal Online flow loads Assets/Scenes/Room.unity after the shared Draft.
Both roles use the Room roster, ball, pitch and authored panels. The host owns
gameplay, and the guest applies published state to those same Room objects.
OnlineRoom and OnlineRoomProof remain available for historical test fixtures.
The normal player build uses Room. Catalog full-room-v25 / wire v7.
In the normal Online flow, the host manages Home and the guest manages Away on
both platforms. Historical prepared proof scenes retain their fixture roles.
Reassess compatibility when team ownership or payloads change.

Verified:

- b414d82: Connect renewal and native callback retirement before platform release.
  Mac crash report verification/sons-mac-20261119T144722Z-session-93237 matches
  first Windows match; native SIGSEGV starts in
  EOS_P2P_RemoveNotifyPeerConnectionInterrupted.
- Windows 3900-second auth soak PASSED with two refreshes and liveness registered:
  TestResults/GameplayRegression/Runs/20261119T150630795Z-WindowsAuthSoak/result.json.
  This is not paired hour-boundary or rebuilt Mac crash acceptance.
- 6722da1: Intermediate FTP preview can switch to movement with M; commitment,
  Experienced, wrong-team, stale and hold boundaries retained. Twenty new cases,
  14 existing FTP and 45 contextual-key cases passed.
- 9c3455b: eight real V pickup -> dribble -> encoded X/N cases passed for both
  teams/directions without production changes. Original stall coincided with
  InvalidAuth; no separate healthy-path key defect reproduced.
- 9a1342d: all three Windows exports succeeded at source 9c3455b; Room/gameplay
  packaged local checks passed. Known nonfatal vswhere.exe post-build diagnostic
  remains in reports. Nineteen verifier tooling cases passed.
- Normal Windows launcher Build/OnlineGame/Windows/Counterattack.exe retains a
  Nov 15 timestamp. Actual managed assembly rebuilt Nov 19 17:31 local, SHA256
  cc0b81dc6bad67b6a58d1f049f57d157dc805176c91c5a6c9947489629033b4c.
  Verify build reports/assembly hashes, not launcher timestamp.

Human coverage: Windows host/Mac guest completed setup/Draft/kickoff/FTP/movement;
later user played 33 match minutes including high/long passes and headers,
reporting presentation gaps. This is partial gameplay coverage, not full
acceptance. Second session/Mac revision not independently attributed from logs.
RigRolls is absent in packaged players, limiting forced outcome coverage.
Never combine first/second match evidence without session attribution.

No hosting watcher/lobby should be assumed active. Auth soak has finished.
Old terminals may be completed jobs or persistent Editor connections; inspect
live processes when relevant, never kill Unity.

## Batch A — fullscreen, resizing and Online navigation

- [x] Inspected PlayerSettings, runtime overrides and normal build scene lists.
- [x] Enabled resizable windows and added F11 switching for Windows and macOS.
      Normal launches request Unity's FullScreenWindow mode. Explicit window
      flags used by standalone proofs still apply.
- [x] Authored OnlineModeMenu with Main Menu styling, Host, Join, lobby-code
      entry and Back. Host opens match setup. Join validates the code and
      opens the existing setup with connection status, errors and cancellation.
- [x] Windows Editor compiled with zero errors. The focused EditMode fixture
      passed 3, failed 0, skipped 0. It checked the authored navigation bindings,
      invalid and pasted lobby codes, and control bounds at wide, 4:3 and
      portrait aspect ratios.
- [x] Normal Windows player built successfully and passed standalone checks for
      borderless fullscreen on startup, F11 both directions, resizing to
      980 x 720, maximize/restore, and fullscreen relaunch after exiting windowed.
- [x] Visually checked the Windows Online menu fullscreen and windowed.
      Pointer clicks showed the invalid-code error, kept that message through
      F11, and opened Host setup in windowed mode. Setup stayed open after
      switching back to fullscreen.
- [ ] Verify native macOS fullscreen startup, F11, resize/zoom and fullscreen
      relaunch. Use the Mac build and launch commands below.
- [ ] Verify pointer mapping and state continuity in a live match on both
      platforms. Paired EOS remains outside the Batch A verification run.

Windows output: `Build/OnlineGame/Windows/Counterattack.exe`. Build
`build_8c69a68a2141` succeeded with the known nonfatal `vswhere.exe`
diagnostic. The build used the local checkout with its pre-existing asset
edits. The build report, assembly SHA256 and native window observations are in
`TestResults/GameplayRegression/Runs/20261119T190928221Z-BatchA-Windows/`.
Focused native test XML and progress are in
`TestResults/GameplayRegression/Runs/20261119T190911294Z/`.

### Online menu style follow-up

The Online screen now uses the authored Hot Seat card prefab and its tilted
frame, typography and Exit button. The lobby-code field and inline feedback
sit inside the card with Host Match, Join Match and Back to Main Menu.
The focused layout and navigation fixture passed 3, failed 0, skipped 0
in `TestResults/GameplayRegression/Runs/20261119T200746769Z/`.

Room presentation is the active priority. Paired laptop testing and any further
Online menu redesign are deferred. The requested Room visuals are still tracked
in batches C through E and the second walkthrough review.

## Batch B — Mac hosting and both roles

- [x] Audited normal entry, native ownership, renewal, lobby publication,
      joins, cancellation and teardown. The successful initialization of an
      owned EOS SDK lasts for the process. Each session still releases its
      platform and callback registrations. Windows players borrow the native
      plugin platform. macOS players shut down the owned SDK on application exit.
- [x] Traced role selection through setup and Room using the earlier Away-host
      assignment. OS detection selects the EOS platform adapter.
- [ ] Prove Mac host + Windows guest through setup -> Draft -> kickoff -> Room,
      with both-origin actions; preserve Windows host + Mac guest.
- [ ] Add focused role-swapped native coverage and teardown/error cases.
- [x] Built the normal Windows player and verified Host, cancel, and Host again
      through the authored UI in one process. Two distinct EOS lobby codes were
      observed, and both sessions closed.
- [ ] Build the normal macOS player and verify the native retry fixture on a
      fresh Editor. The Mac was unavailable for this run.

Windows Editor compilation finished with zero errors. The six focused menu
and offline navigation cases passed. The two native Editor cases failed at EOS
platform creation in the existing process and need a fresh Editor rerun.
The standalone Host retry succeeded in a fresh Windows player. Build
`build_16ae34a5ae22` succeeded with the known nonfatal `vswhere.exe`
diagnostic. The build used the local checkout with its pre-existing asset edits.
Evidence is under `TestResults/GameplayRegression/Runs/`.

- `20261119T192109832Z/results.xml` holds the six navigation passes.
- `20261119T191959956Z/results.xml` holds the two native Editor failures.
- `20261119T193927506Z-BatchB-Windows/` holds the standalone observations,
  build report and assembly SHA256.

For the paired check, build both normal players from the same revision with
separate machine-local EOS identities. Launch the macOS player and use
Multiplayer > Host > Host in setup. On Windows use Multiplayer > Join with
the code shared privately by the host. Complete setup and Draft through
kickoff, perform actions from both authority roles, then verify clean exit
and the reverse Windows-host/Mac-guest pairing. Do not include lobby codes
in retained evidence. Build and launch commands for Mac are below.

## Online Room presentation update

The normal Online Room uses the authored Room roll, pause, Settings and
substitution panels. Either authenticated manager can edit shared Settings
during a shared pause. Only the original requester can resume. Substitutions
still submit one eligible pair at a time for the local manager's team.

The guest receives both scorer lists, the decision timer, match-event banners,
and the active Final Third label. Jersey numbers use Room's render order.
Each manager has their own camera controls and sees team-coloured pointers
over the pitch. Goal-flow positions initialize on the host, and celebrations
use the existing movement stream.

The roll projection uses the authored duel/single-roll, header, and stoppage
panels. A completed result reserves Escape for local dismissal and holds new
gameplay actions until both acknowledgements or the existing result deadline.
The active player still owns the manual roll. Coin toss retains its existing
dedicated online projection.

The Online Multiplayer entry uses the Hot Seat card, stadium backdrop and
typography, with Host Match, lobby code, Join Match and Back inside the card.
The Windows screenshot is retained in
`TestResults/GameplayRegression/Runs/20261119T200804724Z-OnlineCard-Windows/`.

Windows Editor compilation completed with zero errors. The focused runs passed
21 cases with zero failures or skips. The test manifest records per-case native
XML evidence. The four shared roll cases are in `20261122T083702138Z/results.xml`
and the existing automatic stoppage case is in `20261122T083846969Z/results.xml`
under `TestResults/GameplayRegression/Runs/`.

Windows player build `build_45b34b34ff85` succeeded from the local working tree
at `Build/OnlineGame/Windows/Counterattack.exe`. Unity reported the known
nonfatal `vswhere.exe` diagnostic and nine warnings. The build report and
assembly SHA256 are in
`TestResults/GameplayRegression/Runs/20261122T064115Z-FinalOnlineRoom-Windows/`.

Paired testing and additional Online menu design remain deferred.
The local tests cover state and view behaviour. Full visual parity across an
entire match is still open, especially reconnects during roll review and other
roll-family walkthroughs.

## Batch C — shared rolls and match information

- [ ] All original roll panels visible on BOTH screens; matching presentation,
      authorized manager alone can manually roll. Preserve automatic roll policy.
- [ ] Resolved result hold begins AFTER resolution, at most five seconds.
      Esc closes only that manager's panel and sends acknowledgment. Continue
      after BOTH acknowledgments OR timeout; timeout closes remaining panels.
      One early Esc cannot advance gameplay or also open Pause.
- [ ] Cover all roll families, holds, disconnect/recovery, late/duplicate acks.
- [ ] Guest decision timer, match events, scorers and F3 feedback. Distinguish
      missing scorer data from missing display. Shared phase presentation must
      remain visible even when only one manager has active controls.

## Batch D — identity, aiming and local inspection

- [ ] Jersey numbers and token hover tags.
- [ ] Small player cards on token hover, not only Stats lineups.
- [ ] Independent local large player cards for each manager.
- [ ] Orange hover for CanShootTo/inGoal (same goal-line target set).
- [ ] Host kickoff/pass/FTP eligible/target hints; preserve working native
      movement/risk/orange previews. Don't erase host source highlights.
- [ ] Mouse-up selection with real click/drag/cancel distinctions.
- [ ] Both live manager pointers in respective team primary colors. Remote
      pointer never executes local input. Bound update rate and verify mapping
      across cameras, aspect ratios, fullscreen and resizing.
- [ ] Review white hex borders against intended offline presentation; no
      replacement color has been specified by user.

## Batch E — complete Room and Draft presentation

- [ ] Local camera controls and appropriate shared event-camera cues; don't
      synchronize one manager's discretionary camera onto the other.
- [ ] Room intro and goal celebrations; synchronized presentation continuation
      rather than guest ball reset with no celebration.
- [ ] Proper authored substitution/pause UI replacing prototype, preserving
      eligible choices, ownership, ordering and replicated outcomes.
- [ ] Draft five-second completed roll display; team-colored dice; original
      expanding opening-choice panel; no redundant bottom-right Roll.
- [ ] Leave bottom-left, Confirm/Start bottom-right, no overlap.
- [ ] Card typography/layout; stable four offer positions after picks.
- [ ] Authenticated double-click quick-pick to next eligible own slot; retain
      drag/drop and own-slot rearrangements. Drops sync; live drags don't yet.
- [ ] Appropriate populated setup defaults for names/jerseys/settings.
- [ ] Audit pause/settings/substitution/shootout ordering. Preserve unresolved
      gameplay rules recorded in GameplayReview.md.

## Local presentation and sync follow-up

Room and Online Room share the ball top-face material. The transparent SVG
is the source for the pattern texture, and the existing match ball colour
fills the open patches. A top-down Windows Editor render confirmed black
pattern pixels and yellow patches on the actual ball mesh.

Online Room uses three original transparent pointer textures. Hover uses a
pointing hand, left-button hold uses a grabbing hand, and right-button hold
uses a camera with four arrows. Both views tint the textures by team kit.

Create New Game publishes valid owned edits before confirmation. Ready and
Start remain explicit. The setup client fixture passed 26 cases, including
concurrent unconfirmed edits with the host on either team
(`20261122T093451726Z/results.xml`).

Draft sends a visual drag preview in session wire v2. Pick and swap still
commit through the normal authoritative request on drop. The transport
fixture passed 18 cases including preview and cancellation for both roles
(`20261122T093507100Z/results.xml`). Normal Draft preserves empty offer
positions until the next batch. The shared Draft card prefab has the Room
disc sprite and rating colours. The native scene case verified the positions,
badge colour, double-click quick-pick and remote preview
(`20261122T095441518Z/results.xml`). Free Draft uses an authored preview
card when dragging, including when the other manager is on a different
catalogue page (`20261122T094040439Z/results.xml`).

Online Room feeds token hovers into the existing local name/card display.
Space opens or closes the large local card, and Escape closes the card
before it can request a pause. Both role cases passed
(`20261122T094221656Z/results.xml`). The pointer states passed on both
roles (`20261122T093706136Z/results.xml`). Fresh Online Room launches use
the authored intro. Escape skips the local intro and is reserved for that
input (`20261122T094853777Z/results.xml`).

Board clicks submit on mouse-up after the drag threshold and UI checks.
The local shot-target hover uses an orange fill without replacing the
underlying highlight. The host fallback supplies missing legal hints while
native highlights retain priority. The host/guest hint case passed
(`20261122T095723287Z/results.xml`).

The host clock publishes both completed Draft rolls for five seconds before
picks or opening choices. The Draft roll panel uses the two team palettes,
contains Roll and the opening choices, and has no duplicate footer Roll.
Leave is anchored bottom-left and Confirm/Start bottom-right. The Draft
channel fixture passed 19 cases, including both result holds
(`20261122T095227461Z/results.xml`).

The Join paste field on the Online card and the read-only Host code field in
Create New Game use light backgrounds and blue outlines. The host copies the
code where EOS creates the lobby. Guests paste it on the entry card.
The menu fixture passed three cases (`20261122T100254507Z/results.xml`).

The shared Room instructions show the active movement section and remaining
moves. The authored Room display case passed (`20261122T100424682Z/results.xml`).
The goal celebration publication cues each camera and restores its own pose
afterward. Both scoring sides passed (`20261122T100314953Z/results.xml`).

Paired acceptance still needs the full roll-family and reconnect walkthroughs,
including reconnect during a resolved roll, and confirmation of the observer's
movement summary and event framing over EOS. Compare the white hex borders
against Room and review setup defaults across draft modes in the walkthrough.
Both players need a build from the same source with frontend session wire v2.
Windows build `build_924e636a4ab0` succeeded from the local working tree.
The output is `Build/OnlineGame/Windows/Counterattack.exe`. Its report and
managed assembly SHA256 are in
`TestResults/GameplayRegression/Runs/20261122T122935Z-OnlinePresentation-Windows/`.

## Cost and verification discipline

### Corrections after the 2026-11-23 paired walkthrough

- Host Match creates the lobby on the Online entry card. Copy the code there,
  then use Continue to Match Setup. The setup mode should read Online.
- International setup publishes a valid pair of national teams when the
  host switches modes. Each manager can edit their own team afterward.
- Unattended Draft and opening rolls use a five-second host deadline.
  Completed results keep their separate hold. Opening choices expand into a
  vertical list and roster footer labels use a single fitted line.
- Dragging raises the online card or roster slot above the roster. Draft
  drop targets are tinted while eligible. Player-card badge dimensions use
  the authored Room value of 45 by 45.
- Room and Online Room share the ball-colour resolver. Online jersey text
  uses Room's font size and alignment. The camera-drag pointer rotates 45
  degrees clockwise.
- The host's local monitor preserves the published movement summary. Goal
  celebrations retain each manager's active camera.

Windows Editor compilation completed without errors. Focused evidence is
under `TestResults/GameplayRegression/Runs/`: menu (3) in
`20261123T110646530Z`, automatic roll (1) in `20261123T110703384Z`,
International switching (2) in `20261123T110912900Z`, Draft position and
drag sorting (1) in `20261123T110933412Z`, movement summary (1) in
`20261123T111003442Z`, goal/camera (2) in `20261123T111051055Z`,
pointers (2) in `20261123T111144708Z`, and ball/jersey bindings (2) in
`20261123T111340008Z`. Retest the corrections in packaged Windows and
macOS players. The paired screenshots report the original issues.

### Follow-up from the next paired walkthrough

- The ball face uses the supplied black-and-white image as an opacity mask.
  White pixels reveal the selected colour. The five Online pause actions
  have equal spacing, with unused save rows hidden.
- Guest Room projects the authored moved-token panel, including moved
  players, active slot and forfeited sections. Expanded Duel frames carry
  the expansion and attached recovery layout so each viewport can fit it.
- Fresh Online matches wait for both intro acknowledgements. Skip closes
  the local intro. Acknowledgement follows either Skip or natural completion.
  An explicit Leave presents a terminal message to the other manager.
- The pitch border retains the selected dark hex colour through startup.
  Jersey text uses the overlay shader and is checked using camera pixels.
- Draft offers keep rating badges behind the values. Coin-toss actions have
  readable button contrast, and the roll prompt shows the remaining time.
  Setup reapplies national teams and publishes both kits on a match-type switch.

Use matching full-room-v25 builds for the next paired check. The desktop
High preset has anti-aliasing disabled. Resolution controls are a separate
local graphics option and are not part of this pass.

Focused Editor evidence is under `TestResults/GameplayRegression/Runs/`:
guest movement panel `20261123T115800733Z`, two-party intro
`20261123T115907749Z`, expanded roll layouts `20261123T120110800Z`,
Draft badge order and sorting `20261123T120230308Z`, jersey camera pixels,
ball colours and startup borders `20261123T120701902Z`, explicit departure
presentation `20261123T120737424Z`, and International publication
`20261123T120804207Z`.

One batch per session and concise outcome. Do not run full suites, repeat the
historical Mac mega-run, build unchanged outputs repeatedly, idle-poll terminals
or start another hour soak without a concrete reason. Reuse retained evidence.
Compile per justified edit group; run only explicit new/affected fixtures.
A running response is not a verdict. New gameplay/input/timing/data behavior
needs meaningful independent native coverage, dedicated folder/checklist, and
separate entries in Documentation/Setup/OnlineMacTestManifest.json.
Retain XML/progress under TestResults/GameplayRegression/Runs; selectively
force-add ignored evidence, never raw credential-bearing logs.
Pure visuals require compilation and relevant visual checks, not unrelated
gameplay suites. Broader regression is user-run per AGENTS. Do not claim visual
or paired acceptance from state/control tests.

Unity: read unity-cli skill and WindowsBootCamp.md (or PersonalMac.md) before
Editor operations. Actual root and caller/skill metadata on CLI calls. Live
Pipeline only; no Unity launch/batchmode/editor_play/GUI automation. Before tests:
resolve pending run, ready/stopped, check/save dirty scenes and recheck.
Never edit loaded scene YAML. UI authored in edit-mode scene/prefab objects.
Designer Canvas/MatchStatsUI/Stats: update text/status colors only.
Preserve dirty Room, fonts and ProjectSettings; inspect diffs before scoped
edits/staging, never reset them. No delegation unless explicitly requested.

## Mac commands when requested

Start Terminal in the checkout:

```bash
cd "$(git rev-parse --show-toplevel)"
git pull --ff-only
```

Manually open this project in Unity and wait for ready/clean scripts:

```bash
bash Documentation/Setup/BuildEOSStandaloneMac.sh game
open Build/OnlineGame/Mac/Counterattack.app
```

Focused automation: VerifyOnlineMac.sh --fixture EXACT_FIXTURE --no-host. Select
only this batch's fixtures. The verifier rebuilds all three players, checks
login/local playback and commits/pushes one JSON report; --no-host skips paired
waits. Pair only when both laptops/host are ready. If a crash occurs, collect
before relaunch:

```bash
bash Documentation/Setup/CollectOnlineMacSessionReport.sh
```

Retrieve returned verification branch without merging into source.
All human findings: Documentation/Testing/OnlineHumanWalkthroughReview.md.
Historical implementation evidence only if needed: OnlineNextSessionPrompt.md.

## Model recommendation

Start GPT-6.2 Sol, Medium reasoning, for bounded implementation. This is practical
advice, not a price quote or guaranteed speed claim. Use High reasoning or a short
GPT-6.2 Astra session for a demonstrated difficult native lifetime/protocol/ack
bug, carrying precise failing tests/evidence; return to bounded batches afterward.
Official guidance: https://developers.openai.com/api/docs/guides/model-selection
