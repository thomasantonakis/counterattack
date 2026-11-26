# Online spectators verification

The standalone transport proof uses the EOS packet proof scene. Start `watch-host` first and give its `lobby-id.txt` to `watch-manager`. Once the host writes `watch-lobby-id.txt`, give that file to `watch-viewer`. Each handoff contains the same Match Code alongside that role's internal lobby ID. Keep the files and player logs private. Use distinct EOS user identities on Windows and macOS, with a third identity for the viewer.

- [ ] The host creates both lobbies under one authenticated EOS user. The viewer joins after the manager transport is established.
- [ ] The viewer exchanges packets on a separate socket and channel, then leaves. The manager responds during the watch lobby's lifetime and again after it is destroyed.
- [ ] All three players exit with passing `result.json` reports. Confirm that both host handoff files were removed.
- [ ] Verify the shared Match Code through both roles on packaged clients. Repeat with an altered code and confirm the host rejects the request before starting that role's exchange.
- [ ] Verify the two-viewer transport capacity and public stage and Room replication after the packaged proof passes.

Run `OnlineSpectatorProofEditorTests` and `OnlineSpectatorAdmissionEditorTests` with explicit EditMode fixture filters. They verify the command-line entry points, role admission, and the host's two-viewer limit. The three-player run verifies native EOS behavior.

Run `OnlineSpectatorRoomWireEditorTests` to verify the separate Room publication, including its version, size limit, scoreboard, clock, stats, visible player cards and the exclusion of manager-only fields and unrevealed dice. This fixture does not exercise EOS delivery or the spectator view.

Run `OnlineSpectatorStreamEditorTests` to verify fragmented delivery, rejection of manager commands and mismatched matches, and recovery from interrupted or stale snapshots. The fixture also follows lobby, setup, Draft and Room publications under one spectator match identity and checks that private draft offers stay out of the stream. The packaged spectator proof sends two Room samples over the separate viewer channel and verifies score, clock and shared pause on the viewer.

Run `OnlineModeNavigationEditorTests` for the authored Join Match Card. It verifies the actions, that only available roles are shown, and that Back restores the code entry. The live lookup and role transfer still need the packaged client run.

Run `OnlineSpectatorViewPlayModeTests` for the authored spectator views. The fixture delivers public snapshots through the spectator stream and checks early and late viewer paths.

- [x] Follow Match Setup and Draft automatically. Apply changes to public setup and show committed picks with manager controls disabled.
- [x] Bootstrap Room from a live host snapshot alone. Apply score, scorers and clock updates without a save context.
- [x] Preview a player with Space. Reject token selection, roll input and shared resume.
- [x] Animate a published die, apply its revealed face and show the public coin toss.
- [x] Show the shared pause, open Rules locally and retain updated stats across local panel toggles.
- [ ] Verify EOS delivery, camera and white pointer appearance on three packaged clients. Repeat with a viewer joining during Draft and with the viewer capacity filled.

The Unity Editor compiled with zero errors. The focused run passed 2 PlayMode cases and 9 EditMode cases with zero failures or skips. Native XML and progress are under `TestResults/GameplayRegression/Runs/` in `20261126T055216840Z` (view), `20261126T055246142Z` (stream) and `20261126T055256545Z` (Room wire).
