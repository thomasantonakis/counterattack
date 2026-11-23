# Counterattack Online handoff

The [Windows host and Mac guest walkthrough](#windows-host-and-mac-guest-walkthrough-2026-11-23) records the open findings after the Room migration. The earlier sections preserve the original migration handoff.

## Required outcome

Online must use **the actual `Assets/Scenes/Room.unity` scene**, with the same authored UI, token prefabs, materials, jersey text, cards, camera controls, dice panels, pause menu and match presentation as Hot Seat and Single Player. The normal path remains one Create New Game scene, one Draft scene and one Room scene. Use the existing Room objects and their configuration directly.

Keep Online-specific differences in EOS transport, host authority, guest state application, input ownership and session lifecycle. Keep gameplay commands authoritative on the host and state application read-only on the guest. Route the existing Room controls through those paths. The user wants zero extra on-pitch controls, debug/prototype panels, or dedicated Leave buttons. Use Room's existing Back to Main Menu and Quit actions for exit. Online save remains outside this request.

Treat Hot Seat and Single Player Room behaviour as the reference. Preserve the designer-owned `Canvas/MatchStatsUI/Stats` hierarchy and the unrelated working-tree edits. Read `AGENTS.md` before using the Editor or changing a scene.

## Verified source state

- Branch `codex/online-foundation-personal-mac` and GitHub both pointed to `efdd471842e14de9aeb3d836488c7115e045736d` when this handoff was written. The preceding presentation commits are `813da39` and `db89254`.
- `Assets/Scenes/Room.unity`, font assets and other files already have unrelated local edits. Review diffs and preserve them. The Windows build from the last pass is `Build/OnlineGame/WindowsPairedFollowUp/Counterattack.exe`.
- `Documentation/Setup/OnlineMultiplayerHandoff.md` contains build commands, earlier decisions and test evidence. It records the history of the copied scene. This handoff supersedes it on the target architecture and visual acceptance criteria.
- Prior Editor checks established isolated behaviours. They did not establish packaged visual parity. The latest paired screenshots supersede earlier claims that the displayed assets matched Room.

## Current implementation to replace in the normal Online route

- `Assets/Scripts/Online/OnlineFrontendSession.cs` loads `OnlineFullRoomBootstrap.SceneName` after the shared Draft. `OnlineFullRoomBootstrap.cs` points that constant at `OnlineRoom`, binds its preauthored cells and roster, and activates the host authority root. That worked as an isolated replica path. It is the source of duplicated scene presentation and setup.
- `Assets/Scripts/Online/OnlineRoomLaunchLifetime.cs`, `OnlineRoomLaunchData.cs`, `OnlineRoomSession.cs` and `Assets/Scripts/Managers/MatchManager.OnlineRoom.cs` carry the agreed match data and online lifetime. Inspect these when making the normal Room entry point accept the agreed roster, kits and opening choices.
- `Assets/Scripts/Managers/PlayerTokenManager.OnlineRoom.cs` binds copied tokens and applies a special jersey overlay. Hot Seat and Single Player use token spawning and the number setup in `PlayerTokenManager.cs`, with `JerseyNumberTextPrefab.prefab` and `PlayerTokenVisuals`. Use the regular Room token configuration on both roles. Remove the special overlay from the normal route.
- `OnlineRoomMenu*`, `OnlineGroundPassBoard.cs` and `OnlineGroundPassControls.cs` connect the current prototype/adapter controls and projections. Inventory their transport responsibilities separately from the controls they expose. The normal player should present the existing Room controls and route their actions into the authority layer.

The first architectural checkpoint is to open an Online match through Host → setup → Draft and establish that **both players are running `Room.unity`**. Exercise the native Room token and UI paths before doing another round of individual style adjustments. Keep the previous route available only as long as it is needed to verify the migration.

## Latest paired failures to reproduce

| Area | Latest observation | Acceptance |
| --- | --- | --- |
| Create New Game, International | Host selected France and Argentina. Guest displayed Argentina and Argentina. Earlier state-only tests passed while the real guest UI was wrong. | Verify the exact selected national teams in both authored setup views before confirmation. |
| Create New Game, return to Regular | Kit choices failed to follow the switch. Earlier walkthrough also reported regular jersey dropdowns limited to about two visible rows. | Both views restore the agreed Regular selections. The existing dropdowns use the appropriate list height for the mode. |
| Create New Game, Windows text input | Default team name text stays in the field and typed names do not appear. | Both owned team name fields support selection, deletion and typing normally. The other manager sees valid edits before confirmation. |
| Draft drag | Live drag and double-click are working. Only the top edge of the hovered slot is highlighted. | A dragged card stays above the roster, and the permitted target has a visible full border. Exercise all Draft modes that allow dragging. Keep offer positions fixed after a pick. |
| Draft opening toss | Countdown and choice rows are pale, low contrast bars. The final choice row extends onto the blue footer. | Reuse the regular Room opening coin-toss presentation and choices. Keep the five-second auto-roll, readable prompt and local action ownership through that UI. |
| Pitch tokens | Red and white patches surround the numbers. The blue numbers appear over separate yellow blocks. The result differs from regular Room tokens. | The same token prefab, decal, number font, placement, size, colour and render setup as Room, on both laptops. Compare pixels on rendered tokens in the packaged players. A mesh-exists assertion is insufficient. |
| Small and large player cards | Small card values or circles still render differently. Large cards were reported OK in the previous pass. Space over Lineups previously failed to open the large card Online. | Show the existing Room small and large card paths, layering and dimensions. Test outfielders and goalkeepers. Space on a hovered lineup name opens the existing large preview. |

## Behaviour to retain through the migration

- Live setup changes, live Draft drag, double-click quick-pick, fixed offer positions and both scorers were confirmed working in at least one paired walkthrough. Recheck after moving the entry point.
- Both managers see the native Room roll panels, including expanded foul and loose-ball recovery. The result is locally dismissible with Escape and advances once after both acknowledgements or the timeout. Check the rendered guest layout at the Mac viewport.
- Both managers see the native Movement Phase panel, timer, match events and Final Third flash. Both can use Stats with `E`, inspect players, and use their own camera. A goal celebration leaves each camera where it was.
- Skip Intro closes the intro for the manager who pressed it. The match begins when both managers have skipped or watched it.
- The ball uses the supplied football artwork as its top-face mask and both players see the same selected ball colour. Hex borders match the dark hex colour from startup.
- An explicit exit gives the remaining manager a clear message through existing Room UI. A connection interruption remains eligible for recovery.

## Verification

Run focused Editor checks through the live Unity CLI following `AGENTS.md`. Verify scene identity and that the real Room objects are driving presentation in all three modes. Use matching macOS and Windows builds for the paired walkthrough. Compare the same kits, camera preset, player cards, pause menu and roll actions with Hot Seat and Single Player on the same display. Test both host/guest assignments. Treat packaged screenshots and observed input as the acceptance check for visuals.

## Windows host and Mac guest walkthrough, 2026-11-23

The walkthrough followed the `Room.unity` migration at `2498e99` and the shared confirmations change at `a0a95d6`. The players reached the penalty shootout in the normal game. Before the run, the Mac's old player showed the legacy 32-digit prompt. Rebuilding the new player required saving the git-ignored macOS EOS configuration in that checkout. The eight-character code is the expected format for this revision.

| # | Area | Paired observation | Follow-up check |
| --- | --- | --- | --- |
| 1 | International setup | Switching the match to International put the same national team in both slots. The two screens agreed at first. Changing the guest's team did not update the host screen. | Inspect both rendered dropdown captions and the agreed settings after the mode switch and after each manager edits their own team. |
| 2 | Pre-Draft coin toss | The call to action needs review against the walkthrough photo. | Obtain the photo and compare the authored footer, font, contrast, and placement in both packaged players. |
| 3 | Draft averages | XI Averages and Team Averages were not visible or could not be found. | Check their location and visibility in the authored Draft and compare with the equivalent offline Draft. |
| 4 | Kickoff coin toss | The rolling team's die animation did not appear. | Trigger both teams' rolls and check that both viewers see the correct team's animation before the result. |
| 5 | Small player cards | Small-preview attribute circles are the wrong size. Large-preview circles look correct. | Compare the small outfielder and goalkeeper previews with their offline Room equivalents at the same resolution and UI scale. |
| 6 | Mouse pointers | The expected pointer icons and team colors were missing. | Check the cursor artwork and team tint on both players, including what each sees of the other manager's pointer. |
| 7 | Intermediate tackling | Tackling from behind did not show at Intermediate assistance. | Reproduce with a known tackle from behind and check the ruling and the player-facing indicator independently. |
| 8 | Shared substitutions | The sent-off player did not appear in the substitution screen. Once one manager confirmed a shared review, the confirmation button did not clearly show that it was waiting for the other manager. | Verify whether the roster should display sent-off players as unavailable while keeping them out of replacement choices. Show a waiting state after the first confirmation when both managers have orders. |
| 9 | Match Stats lineups | The walkthrough raised whether sent-off players should be struck through in Lineups. | Check the offline presentation and confirm the intended status style before changing it. Preserve the designer-owned Stats layout. |
| 10 | Shootout coin toss | The rolling team's animation did not appear, and the coin toss did not auto-roll. | Exercise both teams in the packaged players and watch the countdown, automatic roll, animation, and transition to order selection. |
| 11 | Shootout kick rolls | The Mac guest did not see the penalty shootout roll presentation. | Watch both a shooter roll and a goalkeeper save roll on the Mac while the host advances the match. Check that the guest can see its own roll prompt and the other team's roll and result. |
| 12 | Shared pause | The match stopped responding after a manager pressed Pause during the shootout. The requester and the exact UI state at the time are unconfirmed. | Reproduce with each manager as requester while waiting for shooter input, then while a result is visible. Capture both player logs and check pause visibility, ownership, resume, and any pending roll acknowledgements. |

Start with the pause lock and the missing guest shootout rolls. The Windows player log records the first Home kick and save attempt, an Away miss, then a third kick for Home. The last recorded message says the shooter must roll before the goalkeeper save attempt. That does not identify the pause requester or establish the cause of the lock. The matching Mac player log and the pre-Draft coin-toss photo are still needed. The prior focused Editor passes do not close these packaged-player findings.
