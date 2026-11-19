# Human online walkthrough review, 2026-11-19

Source: user-confirmed 8364215; matching Windows normal-game assembly SHA256
FCD57AF22FFDC8C5052F074C404C6C9242853BAF8D1E2566A885551E489C3C08.
Evidence: TestResults/GameplayRegression/Runs/20261119T132848333Z-HumanOnlineWindows.
Normal frontend session: ae9931ddaf7e4618ab7d8a1a912ef834 (Windows).
Mac/Home: Sons' Mac. Windows/Away: Windows Personal. Intermediate assistance (2).
This was a normal-game human run, not the prepared automatic probe scene.

## Verified by the human run

- Host/Join and team ownership; setup confirmations and host Start Draft.
- Shared Draft rolls, turn ownership, identical card offers and goalkeeper deals.
- Guest and host picks; drag/drop commits synchronize and own-roster rearrangement
  synchronizes even during the other manager's pick. Drag animation stays local.
- Both completed 16-player rosters and their confirmation/host Start match flow.
- Opening toss ownership: Away wins and chooses kickoff; Home chooses direction.
  Non-owner choice controls are visible but disabled. Room transition completes.
- Correct kits; Home then Away pre-kickoff repositioning and confirmations.
- Correct kickoff ownership/instruction colors, taker placement and automatic
  mandatory Standard Pass; target previews orange on both, including teammates.
- Kickoff pass and ball travel synchronize; legal continuation is offered.
- FTP target selection, attacking/defending one-hex moves and interception offer.
  The interception auto-rolled after five seconds, failed and continued to Movement.
- Movement selection/destination hints work on both screens, movement and selected
  player changes synchronize, and V pickup is accepted.

These are observed checks, not a claim that all gameplay families or a full match
passed. Shots, headers, set pieces, substitutions, actual reconnect and match
completion were not reached in this human run.

## Priority 1: long-session authentication and crash

Windows frontend-session-report.json says InvalidAuth at
2026-11-19T14:32:04.3255009Z, after login started at 13:32:04.0448638Z: exactly
60 minutes. The current standalone Connect session does not own an auth-expiry
renewal notification. The local EOS SDK documentation says the notification is
approximately one minute before expiry and requires another Connect.Login.
Implement/test renewal while preserving the authenticated product user identity,
notification/callback lifetime and clean shutdown; verify a real long session.

The Mac subsequently displayed a macOS unexpected-close alert. No quit/kill was
sent to either client; Windows remained open. Obtain the native Mac crash report
and player log before attributing its cause to authentication or shutdown.
Use CollectOnlineMacSessionReport.sh; it collects no account configuration/saves,
runs no Unity commands and pushes a report-only branch.

The X/N pickup/nutmeg issue must be reproduced while authenticated. The log reached
Littlewood with remaining dribbler Pace 2 and Fruin as the nutmeg option, but the
session also failed authentication. Do not bypass admission checks to mask a
transport failure. Connection failure must be clearly visible instead of leaving
an apparently active but unresponsive decision.

The retained frontend heartbeat RTT summary has 128 samples: p50 42.06 ms,
p95 166.55 ms, last 68.36 ms. This is application heartbeat RTT including scheduling,
not measured input-to-display latency or an internet-only wire measurement.

## Priority 2: confirmed action/presentation gaps

- FTP precommit M switch: Intermediate permits changing one's mind before target
  commitment; instructions advertise M but the online offer builder excludes
  ftp.target. Add meaningful focused tests and preserve committed/interrupted guards.
- Host highlights: setup eligible players/destinations, Standard Pass overview and
  FTP movement destinations show only on the guest. Host orange previews and normal
  Movement destinations DO work. Add fallback hints without destroying those paths.
- Show every rolling panel on both clients using original authored presentation;
  show the correct actor, die/threshold, countdown and result. Only the owning
  manager may trigger a manual roll. Observers see the panel read-only.
- Resolved-roll dismissal, ALL roll types: start a five-second shared result hold.
  Esc closes only that manager's own panel and acknowledges it. Continue on both
  acknowledgements OR at the five-second deadline; timeout closes remaining panels.
  One local dismissal alone cannot advance play. Keep this separate from pre-roll
  auto-roll. Consume this Esc without also opening pause.
- Movement panel was visible only on the attacking manager's screen. Distinguish
  interactive owner controls from the shared read-only phase/remaining-move view.

## Priority 3: restore authored normal-game UI behavior

- Five-second completed Draft roll-result hold before offering picks.
- Keep/expand the opening coin-toss panel for Kick off / direction choices,
  matching original behavior; avoid separate bottom-right options.
- Hide the extra bottom-right Roll control while that panel is active.
- Put Draft Leave at bottom-left, away from Confirm roster / Start match.
- Preserve offered card positions until the next batch; leave holes after picks.
- Match original card layout/typography, and apply team-appropriate dice colors
  (the two teams currently show blue/white dice).
- Implement double-click quick-pick through authenticated Pick and the next eligible
  own empty slot. Current double-click selects only; click+slot and drag/drop work.
- Restore the normal Room intro and visible token jersey numbers.
- Compare white hex borders to intended/offline appearance; no replacement color
  has been specified. Existing Stats layout remains designer-owned.
- Setup already has kit/match defaults and Home/Away name fallbacks; editing them
  in this run was a synchronization check. Remember-last-choice policy is not specified.
- Draft no-auto-roll was observed, but an automatic Draft-roll rule was not requested;
  preserve current rules unless clarified.

## Corrections and verification plan

The apparent first-pick divergence was the user viewing an earlier screenshot;
no Draft synchronization defect was established. The apparent missing kickoff
repositioning was corrected when the user found the active setup highlights.
No whole suites or repeated Mac batch should be launched by default.

For each fix, reproduce using dedicated native tests, retain filtered XML/progress,
validate clean Editor compilation and relevant affected cases, then refresh exports.
Append each new test case separately to OnlineMacTestManifest.json. Prioritize the
blocking session lifecycle over cosmetics; preserve native scene/prefab authoring
and avoid designer-owned Stats changes. Restart the focused human walkthrough
only after the required fixes are verified. Full-match acceptance remains open.


## Independent lifecycle fix checkpoint

Connect expiry renewal and owned native notification retirement are implemented.
The exact Mac crash report was retrieved; its Room bootstrap is identical to this
Windows match, and its SIGSEGV begins at EOS_P2P_RemoveNotifyPeerConnectionInterrupted.
35 focused native lifecycle/argument cases passed, nine affected proof-argument
cases and one real native host/cancel case passed. A fresh isolated Windows auth
export passed an actual re-login preserving identity (authenticationRefreshes=1).

The 65-minute native auth soak is running from the isolated Build/OnlineAuth
export, output TestResults/GameplayRegression/Runs/20261119T150630795Z-WindowsAuthSoak.
It must finish with passed and authenticationRefreshes >= 2; the first refresh
is deliberately forced, the additional one must come from native expiry notice.
Do not overwrite that isolated export or start another real EOS-login probe on
Windows until it finishes. Other focused non-network native tests/builds can run.
Do not claim an hour-boundary or rebuilt Mac pass until actual evidence exists.
Normal/Room/gameplay Windows exports still require refresh before next pairing.

## FTP preview switch checkpoint

Intermediate FTP target previews now publish movement.start, so M uses the same
authenticated switch as other previews. Movement availability still blocks it
after commitment, including immediate Experienced commitment. Twenty dedicated
feature cases, 14 existing FTP cases and 45 contextual-key cases passed.
See Assets/Editor/FeatureTests/OnlineFtpPreviewSwitch/Verification.md for native
artifacts and the before-fix reproduction. Normal exports still need refresh;
this is not paired human acceptance.

## Pickup/nutmeg reproduction checkpoint

Eight native cases now exercise actual V pickup, continued dribble and encoded
X/N commands for both teams and directions, with wrong-team and hold guards.
All passed without production changes: X offers defender steal; N proceeds into
nutmeg. The original InvalidAuth interruption remains the supported explanation
for that run's stalled input, while rebuilt paired acceptance is still pending.
See Assets/Editor/FeatureTests/OnlinePickupNutmeg/Verification.md.

## Next Mac verification commands (prepared; do not run until requested)

Keep the Mac free while the Windows expiry soak finishes and the remaining
walkthrough fixes are prepared. When requested, close the old game normally,
open Terminal in this checkout and update it:

```bash
cd "$(git rev-parse --show-toplevel)"
git pull --ff-only
git merge-base --is-ancestor 9c3455b HEAD
```

Manually open this same project in Unity, wait for the Pipeline server and clean
scripts, then run the focused verifier. It rebuilds all three Mac players, checks
login/local playback and publishes its result branch; it does not wait for a host.
It runs 63 new lifecycle/FTP/pickup cases rather than the full historical batch.

```bash
bash Documentation/Setup/VerifyOnlineMac.sh --no-host \
  --fixture OnlineAuthRenewalEditorTests \
  --fixture OnlineNativeLifetimeEditorTests \
  --fixture OnlineAuthSoakArgumentsEditorTests \
  --fixture OnlineFtpPreviewSwitchPlayModeTests \
  --fixture OnlinePickupNutmegPlayModeTests
```

Only when a new human walkthrough is agreed, launch the freshly rebuilt normal
game (the verification command above already builds it):

```bash
open Build/OnlineGame/Mac/Counterattack.app
```

For a build-only refresh through the manually opened Editor, use
`bash Documentation/Setup/BuildEOSStandaloneMac.sh game` before that open command.
Paste the verifier's Report pushed branch for retrieval. A focused verifier pass
is not an hour-boundary paired crash/renewal acceptance pass.

## Refreshed Windows packaged checks

All three Windows exports at source 9c3455b succeeded (normal, Room proof,
gameplay proof), retaining the known nonfatal vswhere diagnostic. Their managed
assembly hashes match. Packaged local Room and gameplay checks both passed at
20261119T153323938Z-Room-local and 20261119T153415149Z-Gameplay-local.
These local checks do not exercise EOS or establish paired guest isolation.
The independent 65-minute EOS expiry soak is still running; no expiry-window
pass is claimed. Nineteen verifier tooling tests also passed.

## Second human walkthrough: 33 match minutes (user report)

User reports reaching 33 match minutes and exercising high passes, headers,
long passes and other actions without a reported gameplay defect. This is
partial human coverage, not complete gameplay acceptance. The exact new session
and Mac build have not been attributed from artifacts; do not mix these notes
with the first run's logs. RigRolls is absent in packaged players, so specific
outcomes could not be forced.

New and repeated findings, retained individually:

- [ ] F3 feedback/flash does not appear on guest (Sons' Mac).
- [ ] Token jersey numbers do not render.
- [ ] Hovering tokens does not show player tags.
- [ ] Cameras do not move as expected; distinguish local camera controls from
      shared event-driven camera presentation during implementation.
- [ ] Roll panels do not appear on guest (repeats first walkthrough).
- [ ] Esc always opens Pause instead of dismissing a resolved roll panel.
      Preserve the already agreed per-manager close/dual-ack/5-second limit.
- [ ] Decision timer panel is missing on guest.
- [ ] Match events are missing on guest.
- [ ] Both managers' mouse pointers must be visible, updating live and colored
      using their respective teams' primary colors. Replicate pointer position
      as presentation only; never turn remote movement into local clicks.
- [ ] Hex selection occurs on mouse-down rather than mouse-up; preserve click
      versus drag semantics and cancellation. This is input behavior coverage.
- [ ] Hovering CanShootTo/inGoal targets does not turn them orange, preventing
      the manager from checking the aimed goal-line target.
- [ ] Substitution menu still appears as a prototype; restore normal Room UI.
- [ ] Large player card previews are missing. Each manager must independently
      open/dismiss their own local player card without changing the other screen.
- [ ] Small card previews work from Stats lineups but not token hover.
- [ ] Goal celebrations do not render; ball resets as though they completed.
- [ ] Scorers are not updated on guest. Distinguish scorer data from its display.
- [ ] Windows normal player cannot maximize: current PlayerSettings has
      resizableWindow=0; allowFullscreenSwitch=1. Alt+Enter is a temporary
      fullscreen option; a resizable-window build fix remains pending.

Scene clarification verified from source: normal online frontend deliberately
loads Assets/Scenes/OnlineRoom.unity (OnlineFullRoomBootstrap.SceneName), not
Assets/Scenes/Room.unity. OnlineRoom was authored by copying Room, retaining its
real pitch and real gameplay managers, and binding an authored full roster.
Host runs real authority; guest applies state without duplicate gameplay/RNG.
This is the normal multiplayer path, distinct from OnlineRoomProof, but it does
not yet reproduce all offline Room presentation. Earlier broad Room-parity
claims must not be read as full visual/UI acceptance.

Prioritize shared rolls/acknowledgment and match information, then token identity,
hover/cards/aim feedback, cameras/celebrations, and full substitution presentation.
Local hover/card/camera preferences stay local; shared match facts and event
presentation need explicit guest projection. Existing state/control tests do not
establish visual parity. Add dedicated coverage for input/timing/data changes;
pure styling/layout work needs compilation and visual validation.

## Authentication soak final result

The previously pending isolated native Windows run completed successfully:
20261119T150630795Z-WindowsAuthSoak/result.json. Started 15:06:34Z,
completed 16:11:35Z, 3900 seconds, status passed, authenticationRefreshes=2,
livenessNotificationRegistration=true. This includes the forced initial refresh
and a further native expiry-window refresh. It is not a paired one-hour match
or a rebuilt Mac crash acceptance result. The soak controller has finished;
no host lobby was involved.
