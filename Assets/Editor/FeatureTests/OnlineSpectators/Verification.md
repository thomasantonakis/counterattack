# Online spectators verification

The standalone transport proof uses the EOS packet proof scene. Start `watch-host` first and give its `lobby-id.txt` to `watch-manager`. Once the host writes `watch-lobby-id.txt`, give that file to `watch-viewer`. Keep the IDs and player logs local. Use distinct EOS user identities on Windows and macOS, with a third identity for the viewer.

- [ ] The host creates both lobbies under one authenticated EOS user. The viewer joins after the manager transport is established.
- [ ] The viewer exchanges packets on a separate socket and channel, then leaves. The manager responds during the watch lobby's lifetime and again after it is destroyed.
- [ ] All three players exit with passing `result.json` reports. Confirm that both host handoff files were removed.
- [ ] Implement and verify watch-code admission, the two-viewer cap, and public Room replication after the packaged proof passes.

Run `OnlineSpectatorProofEditorTests` with an explicit EditMode fixture filter. The fixture verifies command-line entry points. The three-player run verifies native EOS behavior.
