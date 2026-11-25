# Online spectators verification

The standalone transport proof uses the EOS packet proof scene. Start `watch-host` first and give its `lobby-id.txt` to `watch-manager`. Once the host writes `watch-lobby-id.txt`, give that file to `watch-viewer`. Each handoff contains the same Match Code alongside that role's internal lobby ID. Keep the files and player logs private. Use distinct EOS user identities on Windows and macOS, with a third identity for the viewer.

- [ ] The host creates both lobbies under one authenticated EOS user. The viewer joins after the manager transport is established.
- [ ] The viewer exchanges packets on a separate socket and channel, then leaves. The manager responds during the watch lobby's lifetime and again after it is destroyed.
- [ ] All three players exit with passing `result.json` reports. Confirm that both host handoff files were removed.
- [ ] Verify the shared Match Code through both roles on packaged clients. Repeat with an altered code and confirm the host rejects the request before starting that role's exchange.
- [ ] Verify the two-viewer transport capacity and public stage and Room replication after the packaged proof passes.

Run `OnlineSpectatorProofEditorTests` and `OnlineSpectatorAdmissionEditorTests` with explicit EditMode fixture filters. They verify the command-line entry points, role admission, and the host's two-viewer limit. The three-player run verifies native EOS behavior.

Run `OnlineSpectatorRoomWireEditorTests` to verify the separate Room publication, including its version, size limit, scoreboard, clock, stats, visible player cards and the exclusion of manager-only fields and unrevealed dice. This fixture does not exercise EOS delivery or the spectator view.

Run `OnlineSpectatorRoomStreamEditorTests` to verify fragmented delivery, rejection of manager commands and mismatched matches, and recovery from interrupted or stale snapshots. The packaged spectator proof sends two Room samples over the separate viewer channel and verifies score, clock and shared pause on the viewer.
