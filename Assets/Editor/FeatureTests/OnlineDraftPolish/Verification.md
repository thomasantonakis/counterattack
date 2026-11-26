# Online setup and Draft polish

- Match Hot Seat first-pick and opening rolls, including the wait wiggle, roll cue, five-second automatic roll and two-second result hold.
- Fill the authored pool viewport above pagination and display the goalkeeper catalog's dribbling ratings.
- Start regular online setup with blank team names and require both managers to provide valid names before Draft.
- Show a centered red connection notice at three times its normal scale when the other manager explicitly leaves before the match.

Run `OnlineDraftPolishEditorTests` in EditMode and `OnlineDraftPolishPlayModeTests` in PlayMode. The viewport geometry is also covered by `PoolRowsAndPageControlsFitBetweenTheRosters` in `OnlineDraftInteractionsPlayModeTests`.

Unity compilation finished with zero errors. The following runs recorded zero failures or skips. Native XML and progress are under `TestResults/GameplayRegression/Runs/`.

| Mode | Cases passed | Run |
| --- | ---: | --- |
| EditMode setup name validation | 1 | `20261126T170152979Z` |
| EditMode draft rules, including result hold parity | 6 | `20261126T170204364Z` |
| PlayMode dice audio, departure notice and keeper ratings | 5 | `20261126T170222903Z` |
| PlayMode full-height pool for both views | 2 | `20261126T170301423Z` |
| PlayMode draft variants and independent previews | 11 | `20261126T170328814Z` |

The full-height capture is `TestResults/GameplayRegression/Runs/online-draft-full-height.png`. The capture rerun passed in `20261126T203657087Z`.
