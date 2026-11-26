# Single-packet full-Room presentation

- [x] Encode lossless ball/player floats, match ID, nonce, generation and ordering metadata.
- [x] Fit 36 actors in one EOS packet; preserve reliable framing for commands/outcomes.
- [x] Send full-Room poses UnreliableUnordered with delayed delivery disabled.
- [x] Drop transient pose-send failures without creating a control/recovery hold.
- [x] Reject malformed/nonfinite/unknown/duplicate actor data and stale/conflicting replica frames.
- [x] Verify interleaving poses with reliable fragment assembly and fresh actual Room guests.
- [x] Register every new case separately in the deferred Mac manifest.

Full Room is **v14**. Reliable gameplay framing remains v7; the opt-in pose format
has its own CA18/version-one prefix. Prepared proof profiles keep their existing
reliable JSON poses. Full-Room commands, outcomes, snapshots and recovery messages
remain reliable/ordered. Only replaceable motion uses the compact unreliable lane.
Its 36-actor layout is at most 669 bytes with a 128-byte match ID, below EOS's
1,170-byte packet buffer. Floats are not quantized. Peer/socket authentication
still precedes decoding; nonce/generation/publication/frame checks are preserved.

Transient NoConnection/TimedOut/LimitExceeded on a pose drops that pose. Critical
traffic retains its previous recovery behavior. Full-Room presentation coordinates
use the existing full-snapshot ±1,000 bound, including hidden off-board actors;
prepared replica limits remain ±100. Poses never advance rules or authorize input.

Compilation: clean, zero C# errors. **37 distinct new passed, zero failed/skipped**:
32 `OnlineCompactPresentationEditorTests` at `20261116T173656502Z`, four real
32/36-actor guest cases at `173349781Z`, and the full off-board boundary case at
`173710523Z` (prefix `20261116T`). Earlier 30-case Editor results are retained
without double counting. Native XML/progress are under
`TestResults/GameplayRegression/Runs/`.

Affected: `OnlineGroundPassWireEditorTests` 13 passed at `20261116T173730942Z`,
`OnlineFullRoomPayloadEditorTests` 15 passed at `20261116T173738655Z`, zero failed/skipped.
All 37 Mac additions are separate entries; hashes/inventory and 13 verifier unit
tests pass. No full suite ran. Actual paired EOS unreliable delivery, loss/jitter
behavior and end-to-end latency remain unexecuted; these tests establish codec,
policy, ordering and real-scene guest projection, not an internet RTT measurement.

For future broader user-run verification, use
`Assets/Tests/PlayMode/Features/OnlineFullRoom` / `OnlineFullRoomPlayModeTests`,
and `Assets/Tests/PlayMode/Features/OnlineManualControls` /
`OnlineManualControlsPlayModeTests`, because transport lifetime and recovery are shared.

All Windows profiles refreshed successfully with the hour's v11–v14 changes:
normal `build_d6956fa2437e` (`20261116T173944Z-normal-Windows-build`), Room
`build_1a9ff141da7e` (`20261116T174019Z-Room-Windows-build`), prepared
`build_29de338abcb2` (`20261116T174040Z-Gameplay-Windows-build`). Known nonfatal
EOS vswhere diagnostic and nine normal/two proof warnings remain; zero C# errors.
Packaged checks passed: `20261116T174103853Z-Room-local`, `20261116T174123678Z-Gameplay-local`, `20261116T174138388Z-Room-login` (liveness notifications registered). These proof profiles retain their reliable pose format; paired full-Room unreliable delivery is still unexecuted.
