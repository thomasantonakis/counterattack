# Online save and resume

Run the EditMode fixture `OnlineSaveResumeEditorTests` under category
`OnlineSaveResume`. All cases use temporary local files.

- [x] Save and read a validated checkpoint for each attacking side.
- [x] Load the same slot twice and preserve the event cutoff.
- [x] Verify replacement, rejected updates and incompatible metadata.
- [ ] Exercise live host capture, restore and readiness in a dedicated PlayMode fixture.
- [ ] Exercise both original EOS identities with the packaged macOS and Windows builds.

Unity 6000.5.3f1 compiled with zero errors. The focused EditMode fixture
passed 8 cases with zero failures and zero skips. Native XML and per-case
progress are under
`TestResults/GameplayRegression/Runs/20261124T203817485Z/`.
