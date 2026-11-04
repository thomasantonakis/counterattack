# Stadium crowd tuning

Percentages currently live in `Assets/Scripts/GamePlay/StadiumCrowdController.cs`, in `ApplyMatchColours`. They are not Inspector fields. Each section receives whole-person quotas, rounded to its population, then shuffled once at match startup. Each row below must total 100. No mesh rebuild is needed for percentage or palette changes.

## Club matches

Main = primary top / secondary trousers. Reversed = secondary top / primary trousers.

| Region | Home main | Home reversed | Away main | Away reversed | Neutral | Code array |
|---|---:|---:|---:|---:|---:|---|
| West end, both tiers | 95 | 5 | 0 | 0 | 0 | `{95, 5, 0}` |
| Three consecutive east away blocks, both tiers (six sections) | 0 | 0 | 80 | 15 | 5 | `{80, 15, 5}` |
| All remaining sections | 50 | 30 | 0 | 0 | 20 | `{50, 30, 20}` |

All seats use home primary, including seats in the away section. Fiorentina main is purple/white and reversed is white/purple. Real Madrid main is white/navy and reversed is navy/white.

## International matches

Main and alternate mean the nation's home and away jersey palettes, independent of the selected token kit. Each pair is top/trousers.

| Region | Home main | Home alternate | Away main | Away alternate | Neutral | Code array |
|---|---:|---:|---:|---:|---:|---|
| West end, both tiers | 80 | 15 | 0 | 0 | 5 | `{80, 15, 5}` |
| East end, both tiers | 0 | 0 | 80 | 15 | 5 | `{80, 15, 5}` |
| Both long stands, both tiers | 30 | 18 | 20 | 12 | 20 | `{30, 18, 20, 12, 20}` |

For Germany vs Netherlands, columns mean white/black, green/white, orange/white, white/orange, neutral. All stadium seats use one randomly selected colour from `RandomSeatColour`.

## Section numbering

Hierarchy names use 01–32, repeated under each tier. Code and the serialized `section` field use 0–31.

| Region | Hierarchy section numbers | Internal indices |
|---|---|---|
| North long stand, including adjacent half-corners | 32, 01–07 | 31, 0–6 |
| East end, including adjacent half-corners | 08–15 | 7–14 |
| South long stand, including adjacent half-corners | 16–23 | 15–22 |
| West end, including adjacent half-corners | 24–31 | 23–30 |
| Club away reference (position 1, excluded) | 11 | section=10 |
| Current club away allocation (positions 2–4 counterclockwise) | 10, 09, 08 in BOTH tiers | section=9, 8, 7 |

The `Club Away Section` Inspector field is now the reference block, counted as position 1 and excluded from the allocation. Valid reference values 8–13 correspond to hierarchy sections 09–14. `Club Away Section Count` defaults to 3: the next three blocks counterclockwise, in both tiers, making six sections total. Section IDs increase clockwise, so counterclockwise reduces the ID. The original reference block returns to the ordinary home-fan mix.

## Making areas more distinct

The stadium prefab root uses uniform scale `0.85`; its pitch apron compensates with inverse scale to preserve the opening around the playing surface. `RoomStadiumEditorTools.StadiumScale` preserves this size on rebuild. Bench tokens use `z = -12` in `PlayerTokenManager.GetBenchTokenPosition`, including restored bench players.

Increasing the main-colour share and decreasing reversed/neutral shares creates a stronger colour block. Examples, not applied: club away `{85,10,5}`; ordinary home `{70,20,10}`; international end `{80,15,5}`. Changes to a regional array affect every section selected by that branch.

Individual percentage overrides are not currently exposed. To special-case one section in code, add its condition before the broader region condition, with both `section.upperTier` and the zero-based `section.section`. Always keep `palettes` and the percentages array aligned; the last percentage is the neutral mix.

## Preview workflow

After editing percentages, let Unity compile. Select `StadiumCrowd`, open the StadiumCrowdController component's context menu, then choose `Preview Colours/Fiorentina vs Real Madrid` or `Preview Colours/Germany vs Netherlands`. These Edit Mode previews have a fixed seed for comparison and do not change match settings. The user's next Play session applies the actual match settings with a new random assignment.

`NationalKit` contains Germany/Netherlands clothing overrides; other nations use matching main/away entries from TokenKitCatalog. `RandomSeatColour` contains the international seating palette choices. `RoomStadiumEditorTools` controls physical row spacing, seat spacing and aisle width; its Realign Stadium Aisles menu updates mesh assets and refreshes native vertex buffers.
