# Online native Room

- Run `OnlineNativeRoomPlayModeTests` in PlayMode. The nine cases use agreed Draft data and load Room in single-scene mode.
- Check rendered pitch numbers, both card previews, the authored pause menu, Hot Seat and Single Player startup, halftime, the initial and selected Movement Phase jerseys, and the duel, header and stoppage roll projections.
- For packaged acceptance, use matching Windows and macOS players and swap the host and guest assignments for the second pass.

Windows Editor compilation completed with zero errors. The focused Room fixture
passed 9, failed 0, and skipped 0 on 2026-11-23. Native XML and per-case progress
are in `TestResults/GameplayRegression/Runs/20261123T174518335Z/`.

Setup passed 32/32 in `20261123T172303561Z`, the Draft opening toss and footer
passed 4/4 in `20261123T174059152Z`, and Draft drag and rating circles passed
68/68 in `20261123T174746266Z`, under the same results directory.
The packaged Windows/macOS walkthrough is deferred.
