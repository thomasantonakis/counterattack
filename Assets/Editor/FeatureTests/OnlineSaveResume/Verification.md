# Online save and resume

Run EditMode fixture `OnlineSaveResumeEditorTests` under category
`OnlineSaveResume`. All cases use temporary local files.

- [x] Round-trip a checkpoint for each attacking side.
- [x] Load the same slot twice and preserve the event cutoff.
- [x] Verify replacement and preservation after a simulated disk publish failure.
- [x] Reject unsupported checkpoints, damaged files, incompatible catalogs, and unsettled rolls.
- [x] Reject an unauthenticated peer, an old session and a mismatched bootstrap acknowledgement.
- [x] Verify the guest bootstrap contains settings and rosters without host paths, runtime snapshots or event history.
- [ ] Confirm EOS identity rejection and rejoin on packaged macOS and Windows players.

Unity 6000.5.3f1 compiled with zero errors. The focused EditMode fixture
passed 11 cases with zero failures and zero skips. Native XML and per-case
progress are under
`TestResults/GameplayRegression/Runs/20261124T232444090Z/`.
