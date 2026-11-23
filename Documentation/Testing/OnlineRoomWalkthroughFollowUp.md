# Online Room walkthrough follow-up

The November 23, 2026 match ran in **Room.unity** with Online roles. The Windows player log (`winhost-player-2026-11-23.log`, lines 1–2) identifies `Build/OnlineGame/WindowsNativeRoom/Counterattack.exe`. The Mac session has both `room-bootstrap.json` and `room-replica.json` and reached the gameplay exchange that waits for `OnlineRoomStartup.Board` in Room. Its bootstrap records assistance level 2 (Intermediate). The paired build settings exclude `OnlineRoom.unity`. Draft contains a “Leave session” action. In Room, the authored pause menu contains Back to Main Menu and Quit Game. New builds log the role and active scene asset path when Room binds.

The host log (lines 5852–5885) records a goal on kick one, an off-target roll of 1 on kick two and the Home shooter waiting to roll on kick three. Immediately before kick three, the host logs that the outgoing shooter still referenced the penalty spot after it ceased to list him as occupant. The Mac player log at line 3233 records `Manual guest accepts ordered state`, the message emitted when a publication fails replica validation. The Windows `frontend-session-report.json` says the session closed and contains no pause requester or state revision. The logs establish a guest state rejection and a host occupancy inconsistency. They do not establish that the two were the same publication or that pressing Pause caused it.

| Item | Cause or evidence | Change and check |
| --- | --- | --- |
| 1. International setup | The offline national-team handler could move the opponent’s dropdown and re-pair both kits before the Online client submitted an owned edit. That could leave the submitted kit incompatible with the opponent’s actual selection. The authority already rejected duplicate team names. | Online edits keep the other manager’s selection fixed, choose a compatible kit for the edited side, and restore the agreed captions after a duplicate choice. The authored setup fixture covers International entry and return to Regular, the duplicate choice and live edits for both roles and both host sides. |
| 2. Pre-Draft roll prompt | The screenshot shows the first-pick roll panel. Its Online-only animated Roll graphic placed a pale label and surface across the edge of the panel. | The existing Draft footer now displays the countdown in white on the blue footer. The existing click target covers the prompt for the rolling manager. The Draft test checks the footer, hit target, turn and automatic rolls in the authored scene. |
| 3. Draft averages | Online hid the offline average containers and disabled the manager that created their rows. | Both Draft modes now use the same authored Draft containers and Player Slot prefab instances. Online calls the existing average calculation after projecting the roster. The Draft test checks both squad sizes and the XI pace value after an authoritative pick. |
| 4. Kickoff toss animation | The online presentation cleared the active die each time it received a snapshot with no result. | The authored toss die animates while that team is rolling and displays the published score once resolved. The native toss test checks both teams and both peers. |
| 5. Small cards | The small-card theme replaced the prefab’s circle sprite and sized it from the row’s current geometry. | The small card uses the prefab’s badge size, sprite and position and updates only the result color. The outfield and goalkeeper theme tests verify the authored circle sprite, diameter and position after rendering. The existing large inspection card stays on its own path. Paired pixel comparison is pending. |
| 6. Pointers | Room startup never bound an Online pointer overlay, so the exchange’s existing pointer lane had no view. | The authored pointer artwork from the legacy scene is a prefab placed in Room and bound for both Online roles. The native input fixture checks the remote grab texture and the other manager’s kit color in Room. |
| 7. Intermediate tackle | The Windows log (lines 4783–4802) records a defender roll of 2, a dangerous-position foul, a harsh leniency check and a second yellow. The rule therefore ran at Intermediate. The common Room rules show purple destination hints at Beginner; Intermediate has the from-behind wording at the tackle choice. | The existing assistance policy and tackle rules are shared across modes. Two deterministic tests check the warning, the absence of the Beginner guide, and the roll-of-2 foul with harsh leniency in both directions. A packaged capture of the Intermediate wording remains pending. |
| 8. Substitutions | The review filtered sent-off players out before building both the visible roster and the eligible choices. The first confirmation only changed interactability. | Sent-off players appear as unavailable in the roster and are excluded from both dropdowns. After confirmation, the local button says “Waiting for other manager” until review completes. The shared review fixture checks visibility, eligibility, confirmation and applying orders. |
| 9. Lineups | Room’s common lineup formatter already applies red and strikethrough to sent-off players in both its authored and fallback presentations. It reads the replicated sent-off flag on a guest. | The native substitution test verifies the sent-off name is struck through in the authored lineup on both roles. The designer-owned Stats hierarchy uses its existing layout. |
| 10. Shootout toss | The Room toss hid the active die in the same projection as kickoff and only advanced a completed result. It had no deadline for the roll itself. | Both dice use the authored animation. The host auto-rolls after five seconds for either team and freezes the countdown during holds. The native toss test checks the hold and both automatic rolls. |
| 11. Shootout kick rolls | A roll frame only carried the cosmetic face currently showing. Guest projection resolved it as a fixed result. | The publication explicitly carries animation state and jackpot availability. The guest restores the authored panel into the same sibling layer used by the host. The native shootout test checks shooter, save and result frames after wire decoding. |
| 12. Pause and continuation | The two player logs preserve the occupancy warning and guest rejection. Independent inspection found that a pending or locally dismissed roll could consume Escape, and that moving keepers can briefly be between staging hexes. | Shootout movement releases the source hex consistently before travel. The resume guard ignores temporary goalkeeper continuity gaps during shootout motion. The shared pause menu stays above any pending roll panel, and resuming retains result acknowledgements. The native shootout test exercises both requesters before rolling and while the result is visible, then checks publications through kick three. New builds log the accepted pause requester, phase and revision. Rejected publications log the phase and revisions. |
| 13. Moved Players numbers | Both roles already use the authored Room panel and slot prefab. The shared kit renderer applied a kit-specific vertical offset to the UI number. | The slot preserves its authored center while retaining the kit font and color. The prefab test places a two-digit number on an offset kit and checks the number and disc centers. |

## Verification scope

Unity compilation completed with zero errors. Native Editor result files are listed below.

| Focused filter | Passed | Failed | Skipped | Evidence |
| --- | ---: | ---: | ---: | --- |
| `OnlineSetupViewEditorTests.AuthoredView` | 36 | 0 | 0 | [Authored setup suite](../../TestResults/GameplayRegression/Runs/20261123T211327292Z/results.xml) |
| `international-owned-team` | 4 | 0 | 0 | [Duplicate prevention and live owner edits](../../TestResults/GameplayRegression/Runs/20261123T211659473Z/results.xml) |
| `OnlineRoomWalkthroughEditorTests` | 5 | 0 | 0 | [Draft, badge and Movement Phase prefab checks](../../TestResults/GameplayRegression/Runs/20261123T212121115Z/results.xml) |
| `NativeTossAnimation` | 2 | 0 | 0 | [Room toss turns, animation and auto-roll](../../TestResults/GameplayRegression/Runs/20261123T211826384Z/results.xml) |
| `TeamColorsMatchCommandOwnershipAndLocalUiStaysLocal` | 1 | 0 | 0 | [Native Room pointers and input ownership](../../TestResults/GameplayRegression/Runs/20261123T211905808Z/results.xml) |
| `OnlineSharedSubstitutionReviewPlayModeTests` | 3 | 0 | 0 | [Both roles, visible sent-off player, first confirmation and lineup](../../TestResults/GameplayRegression/Runs/20261123T212138690Z/results.xml) |
| `IntermediateTackleFromBehind` | 2 | 0 | 0 | [Both attacking directions at Intermediate](../../TestResults/GameplayRegression/Runs/20261123T212257179Z/results.xml) |
| `ShootoutRollsAndPause` | 2 | 0 | 0 | [Real Room shooter and save frames, pause and third kick](../../TestResults/GameplayRegression/Runs/20261123T212311720Z/results.xml) |

The [initial shootout run](../../TestResults/GameplayRegression/Runs/20261123T210436359Z/results.xml) reproduced the guest dice animation failure for both cases.

The Editor runs use native Room and the real player-state codec with deterministic fixtures and temporary saves. Packaged Windows/macOS verification still uses a paired match. At kickoff and at the shootout toss, record both viewers while Home and Away roll. Compare both small cards, the Draft footer, pointers, and the Moved Players number placement at the same resolution and UI scale. During the shootout, let each manager initiate a pause at a roll prompt and while a result is visible. Retain the matching player logs.

## Matching packaged players

Unity built both players from the same working source with eight enabled scenes
and zero build errors. The player builds have not yet been exercised together.

| Platform | Player | Build report |
| --- | --- | --- |
| macOS | `Build/OnlineGame/RoomWalkthroughFollowUp/Mac/Counterattack.app` | [Succeeded, 9 warnings](../../TestResults/GameplayRegression/Runs/20261123-RoomWalkthroughFollowUp-Builds/mac-build-report.json) |
| Windows x64 | `Build/OnlineGame/RoomWalkthroughFollowUp/Windows/Counterattack.exe` | [Succeeded, 16 warnings](../../TestResults/GameplayRegression/Runs/20261123-RoomWalkthroughFollowUp-Builds/windows-build-report.json) |

`git diff --check` passed for source and documentation. Unity serialized four
blank YAML values with trailing spaces across the saved Room and Draft scenes.

## Small goalkeeper preview follow-up

The offline Room hover cards reference `Assets/Resources/UI/Cards/GKPlayerCard.prefab`.
The original badge test used the separate large-card asset. The actual small GK
prefab had no circles. It now uses the outfielder’s authored 45 × 45 Disc
behind each rating, with the shared low, medium and high colors. The [Room
binding tests](../../TestResults/GameplayRegression/Runs/20261123T214915365Z/results.xml)
compare all seven ratings at each color tier on both authored card instances.
