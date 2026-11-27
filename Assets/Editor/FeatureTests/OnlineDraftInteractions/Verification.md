# Online Draft interactions

- [x] Verify parity between Free keeper and outfield first-pick rolls, including ties and automatic rolls. Both Arcade stages open directly for picks.
- [x] Author the pool table, sort headers, filters, rows and drag preview in `Draft.unity`.
- [x] Gate Free keeper and Free Regular outfield controls on turn ownership. Validate the same empty roster destination for highlighting, drag/drop and click selection. Double-click chooses the next eligible slot.
- [x] Give each Arcade manager a separate filter and sort state. Keep candidates available after picks. Accept crossing picks when intervening commands only filled the other roster in the same stage.
- [x] Project the public stage, rolls and accepted rosters to spectators with the manager controls hidden.

Run `OnlineDraftInteractionRulesTests` in EditMode and `OnlineDraftInteractionsPlayModeTests` in PlayMode, or select category `OnlineDraftInteractions`. The authored scene tests use two serialized manager clients and a separate spectator case. See [Draft return to setup](../OnlineDraftReturn/Verification.md) for the current compatibility labels.

## Editor verification

Unity 6000.5.3f1 reported zero compile errors. Native XML and per-case progress are under `TestResults/GameplayRegression/Runs/`.

| Scope | Passed | Failed | Skipped | Run |
| --- | ---: | ---: | ---: | --- |
| EditMode rules and public draft projection | 6 | 0 | 0 | `20261126T161134673Z` |
| PlayMode manager controls and late spectator | 7 | 0 | 0 | `20261126T161020116Z` |
| PlayMode existing draft variants and off-page preview | 11 | 0 | 0 | `20261126T161152126Z` |

Broader affected coverage includes `OnlineDraftDragPlayModeTests` in `Assets/Tests/PlayMode/Features/OnlineDraftDrag/` for dealt-card and existing-roster gestures, and `OnlineDraftVariantEditorTests` in `Assets/Editor/FeatureTests/OnlineDraftVariants/` for the complete mode matrix. Packaged cross-platform EOS verification remains pending.

The presentation follow-up reproduced overlapping rows on the prior build in `20261126T162534994Z`. The table now uses the offline panel dimensions outside the dealt-card grid, with a vertical layout for rows and pagination below the viewport. Both manager views use the authored player card for pool drags. Empty averages show neutral badges and populated averages use the existing rating colors. Compilation finished with zero errors. The updated PlayMode interactions and layout fixture passed 9 cases in `20261126T163332913Z` and the scene variant fixture passed 11 in `20261126T163429411Z`, with zero failures or skips. The populated Editor capture is `TestResults/GameplayRegression/Runs/online-draft-layout-final.png`.

## Packaged verification

Use the same commit on both machines. Follow the platform setup guide and open Unity manually in each checkout before running the commands. Exercise Free GK followed by Free Regular, Arcade GK followed by Arcade outfield, and a spectator joining during the keeper roll.

**macOS**

```sh
git fetch origin &&
git switch feature/online-nonregular-draft-controls &&
git pull --ff-only origin feature/online-nonregular-draft-controls &&
git rev-parse HEAD &&
bash Documentation/Setup/BuildEOSStandaloneMac.sh game &&
open Build/OnlineGame/Mac/Counterattack.app
```

**Windows PowerShell**

```powershell
git fetch origin
if ($LASTEXITCODE) { throw 'Fetch failed' }
git switch feature/online-nonregular-draft-controls
if ($LASTEXITCODE) { throw 'Checkout failed' }
git pull --ff-only origin feature/online-nonregular-draft-controls
if ($LASTEXITCODE) { throw 'Fast-forward pull failed' }
git rev-parse HEAD
.\Documentation\Setup\BuildOnlineGameWindows.ps1 -Launch
```

The Windows build script reports success and launches `Build/OnlineGame/Windows/Counterattack.exe`.

## Lobby code placement

The online Draft lobby code uses the scene title and the same gap and padding as
Create New Game. The four `SpectatorCanCopyCodeAndReturnToOnlineMenu` PlayMode
cases passed for Setup, Regular, Free Regular, and Arcade with zero failures or
skips. Unity compiled with zero errors.
Native results are in `TestResults/GameplayRegression/Runs/20261127T171337364Z/results.xml`.
