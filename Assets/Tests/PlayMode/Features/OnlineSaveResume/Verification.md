# Online checkpoint capture

Run PlayMode fixture `OnlineSaveResumePlayModeTests` under category
`OnlineSaveResume`.

- [x] Finish a real kickoff pass with Home and Away in separate cases.
- [x] Reject save while the kickoff pass is unsettled.
- [x] Open the host's authored Save Match As dialog during a guest pause.
- [x] Save and update the slot while preserving the guest's resume rights.
- [ ] Verify the guest cannot write a checkpoint.
- [ ] Exercise Load Match, authenticated Room restore and both Ready confirmations.
- [ ] Run the packaged macOS and Windows walkthrough after the load flow is ready.

Unity 6000.5.3f1 compiled with zero errors. The focused PlayMode fixture passed
2 cases with no failures or skips. Native XML and progress are retained under
`TestResults/GameplayRegression/Runs/20261124T205350897Z/`. The earlier run at
`20261124T205300573Z` started target selection before the taker reached the ball.
