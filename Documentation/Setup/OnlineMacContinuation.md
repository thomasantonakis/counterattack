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
paired acceptance. Pause, substitutions and shootout ordering now have native
integration coverage; their cross-machine acceptance remains deferred. No actual
internet latency has been measured.

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
Control display now captures only detached input metadata and legal choices,
avoiding full-roster/statistics JSON copies each host frame. This adds ten focused
native cases and 19 selected affected passes; see OnlineControlDecision/Verification.md.
Its memory test reports coarse Mono heap accounting, not actual network latency.
Outside-box keeper shots now expose the defending team's D/hands and B/block
choices. Fourteen new native passes cover continuation, eligible/ineligible
blocks, goals, handball/red card, holds and cancellation in both directions;
19 affected shot/legacy cases passed. Full Room now requires **v10** on both
clients. See OnlineOutsideKeeperShot/Verification.md. Shared live-settings consent
is awaiting the user's answer; continue remaining gameplay and transport work.
Keeper movement choices add 72 native passes and nine selected affected passes,
including serialized decisions for both teams/directions and actual wall-dive
continuation. Full Room now requires **v11**; exports still precede that batch.
See OnlineKeeperGroundChoices/Verification.md. Movement snapshot offers/declines
and presentation/transport tuning remain independent next work.
Movement snapshot offers add 49 native passes and 12 selected affected passes;
full Room now requires **v12**. Both teams/directions and actual snapshot/save
continuations are covered. See OnlineMovementSnapshot/Verification.md. Exports
still precede v11/v12; rebuild before paired use. Mac execution remains deferred.
Collected loose-ball snapshot choice/reset adds ten new passes and two affected
legacy passes; current full Room is **v13**. See OnlineLooseSnapshotChoice/Verification.md.
Refresh exports before paired use; shared-setting consent remains unanswered.
Current full Room is **v14**, with 37 new compact-presentation cases and 28
affected wire/payload passes. Full poses are lossless single-packet unreliable
traffic; commands/outcomes/snapshots/recovery stay reliable. See
OnlineCompactPresentation/Verification.md. Both normal-game clients must rebuild.
Paired EOS loss/jitter/RTT acceptance remains unexecuted; the Mac run is deferred.
Windows exports now include v11–v14 (17:39–17:40 UTC); see the compact-presentation
checklist for exact evidence. The Mac manifest includes all 168 new cases from
this hour. Use the pull/build/run commands above when the paired run is arranged.
Preserve host-only RNG/rules, authenticated semantic commands, guest inactivity,
receipt/revision checks and reconnect holds. Compile, run feature-focused native
tests and build through the already-open Editor. Commit/push verified batches.
Preserve unrelated local scene/font/settings edits. Do not launch Unity,
batchmode, ordinary editor_play, GUI automation or additive scene loads.
Continue independent implementation whenever Mac actions are unavailable.

Latest independent gameplay checkpoint: Room v15 adds authenticated manual and
automatic stoppage-action rolls and matching guest added-action clock/counts.
There are 23 new Windows native passes; contextual keys add another 63. Read
OnlineStoppageActions/Verification.md and OnlineContextKeys/Verification.md.
Pull/rebuild both clients after the remaining batches; the latest exported
Windows binaries still contain v14. Do not run the paired script until the user
requests it after independent readiness work is complete.

Current contract is Room v17 / wire v7. Windows normal, Room and prepared exports
were refreshed with stoppage clocks, stationary defensive choices, pickup V and
header select-all. Read OnlineHeaderSelectAll/Verification.md for current evidence.
Use the normal game build/run commands above after pulling; do not run the paired
verification script until the user requests it. Further independent publication
latency and full-match audits are continuing on the same branch.

Latest contract is Room v18 / wire v7. Windows exports now include the pre-move
choice and authority-hold fixes; see OnlinePreMoveChoices/Verification.md. Continue
using the normal Mac game build/run commands after pulling the current branch.
Paired verification remains deferred while independent Draft/input work continues.

Authenticated normal-Draft drag/drop is implemented over existing Pick/Swap
requests, including own-slot rearrangement and stale/pending gesture rejection.
68 new native cases and 30 affected click/variant cases passed; see
Assets/Editor/FeatureTests/OnlineDraftDrag/Verification.md. All three Windows
exports and packaged local/login checks refreshed at 23:23-23:26 UTC November 16.
Room remains v18/wire v7. Each new case has a separate deferred Mac manifest entry.
Continue the remaining frontend/full-match audit and transport verification.
Paired testing and actual internet latency remain deferred.

Full-Room guest pose updates now interpolate over one 50 ms stream interval,
without extrapolation or a future-frame buffer. Reliable snapshots and holds
apply immediately; host movement and guest occupancy/rules remain unchanged.
Six new native cases and ten affected snapshot/compact cases passed. See
Assets/Editor/FeatureTests/OnlineMotionSmoothing/Verification.md. Windows exports
refreshed successfully November 17 at 05:32-05:33 UTC. Room stays v18/wire v7;
all six cases are separately registered for the deferred Mac run. Continue
published guest pitch highlights and the remaining frontend/full-match audit.
Actual paired acceptance and internet latency remain deferred.

Guests now see published legal-choice highlights on the authored pitch: eligible
players/destinations, selected player and selected target. Host source highlights
are preserved; holds, pauses and withdrawn choices clear hints. Four actual Room
and 14 isolated cases passed, all 18 individually registered for the deferred Mac
run. See OnlinePublishedHighlights/Verification.md. Room remains v18/wire v7;
Windows exports refreshed at 05:43-05:44 UTC November 17. Next audit: restore the
original authored match instruction panel using authority-published text without
enabling the disabled monitor's AI/logging/input loops. Paired testing deferred.

Room now uses v19 (wire v7) and publishes the authority's original match
instructions into the existing authored panel on both screens. The monitor's
Update, AI, logging and key loops stay disabled. Online kickoff wording now
correctly asks each manager to confirm once. Twenty-three new native cases and
19 focused clock/payload regressions passed; all new cases are separately
registered for the deferred Mac run. See OnlineInstructions/Verification.md.
All Windows profiles refreshed successfully at 06:03-06:04 UTC November 17.
Continue the original Room presentation audit: committed pass/shot paths and
risk previews, followed by remaining shared result/menu presentation. Preserve
the unanswered shared-settings consent and stationary outfield-X rule questions.
Paired testing and actual internet latency remain deferred until requested.

Room now uses v20 / wire v7 and carries bounded authority-computed path/risk
previews, shading and raised goal markers. Personal hover is excluded; medium
assistance does not reveal uncomputed risk. Source-held goal markers survive
logical target-field differences and clear on actual host/streamed guest ball
motion, without enabling Ball or other guest rule components. Thirty-eight new
native cases and 23 focused affected cases passed. Maximum tested state retains
36 actors, 1,900 previews and 8,192 instruction characters in 222,057 expanded /
15,639 wire bytes under unchanged limits. All new cases individually registered
for the deferred Mac run. See OnlineActionPreviews/Verification.md. Windows
exports refreshed at 07:20-07:22 UTC November 17. Next: audit off-target camera
shot animation for motion publication and pause/cancellation. High/long aerial
flight flags and goal-flow motion publication already exist; do not duplicate
them. Paired acceptance and actual internet latency stay deferred.


Online off-target camera misses now publish their real ball motion through the
existing Room v20 / wire v7 pose lanes and retire stale flight/restart work on
shot reset. Twenty new native cases passed (14 full-shot, six snapshot), plus ten
focused shot/possession regressions and 13 report-tool unit tests. Every new case
has a separate deferred Mac manifest entry. All three Windows exports succeeded
at 08:32-08:33 UTC November 17; packaged Room/local, Gameplay/local and Room/EOS
login checks passed at 08:36 UTC. Builds retain the known nonfatal vswhere
post-build diagnostic; compilation has zero errors. See
Assets/Editor/FeatureTests/OnlineOffTargetCamera/Verification.md.
Next audit: lob/wide off-target continuation cancellation. Paired testing and
measured internet latency remain deferred until requested.


Off-target lob continuation is now cancellation-safe: eight new full-shot and
snapshot cases passed in both directions after reproducing four stale-flight
failures. Four focused long-pass cases and 13 report-tool checks passed. All
eight additions are individually registered for the deferred Mac run. All three
Windows exports succeeded at 10:38-10:39 UTC November 17, retaining the known
nonfatal vswhere diagnostic and zero C# compilation errors. See
Assets/Editor/FeatureTests/OnlineOffTargetLob/Verification.md. Room stays v20 /
wire v7. Next: wide-miss ball-motion cancellation. Paired testing remains deferred.
