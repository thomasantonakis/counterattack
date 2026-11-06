# Legacy generated branch inventory

Extracted from the committed pre-migration runner at commit `fb9a00b` on 2026-11-06. These are explicit data-driven branch definitions, additional to the distinct coroutine-method inventory. Runtime parameter matrices are listed separately below. No unexecuted branch is considered migrated.

## ShootingBranchDefinition (31)

- 01: Blocked by Paterson Picked up by Gilbert — retained
- 02: Blocked by Paterson missed by Gilbert — retained
- 03: Blocked by Paterson missed by Paterson — retained
- 04: Blocked by Paterson into the Box McNulty misses — retained
- 05: Blocked by Paterson into the Box McNulty picks — retained
- 06: Missed by Paterson Blocked by McNulty Paterson Picks — retained
- 07: Missed by Paterson Blocked by McNulty Paterson Misses — retained
- 08: Missed by Paterson Blocked by McNulty McNulty Picks — retained
- 09: Missed by Paterson Blocked by McNulty McNulty Misses — retained
- 10: Missed by Paterson Blocked by McNulty Soares Picks — retained
- 11: Missed by Paterson Blocked by McNulty Kuzmic Picks — retained
- 12: Missed by Paterson Blocked by McNulty Kuzmic Misses — retained
- 13: Missed by Paterson McNulty Blocked by Soares McNulty Picks — retained
- 14: Missed by Paterson McNulty Blocked by Soares McNulty Misses — retained
- 15: Missed by Paterson McNulty Blocked by Soares Kuzmic Picks — retained
- 16: Missed by Paterson McNulty Blocked by Soares Kuzmic Misses — retained
- 17: Missed by Paterson McNulty Blocked by Soares Kuzmic Misses Own Goal — retained
- 18: Saved by Kuzmic and held — retained
- 19: Saved by Kuzmic and Corner N — retained
- 20: Saved by Kuzmic and Corner S — retained
- 21: Saved by Kuzmic and Spill Soares Hits — retained
- 22: Saved by Kuzmic and Spill Soares Misses — retained
- 23: Loose Ball by Kuzmic Poulsen Misses — retained
- 24: Loose Ball by Kuzmic Poulsen Intercepts — retained
- 25: Loose Ball by Kuzmic Kalla Picks — retained
- 26: Loose Ball by Kuzmic Soares Picks — retained
- 27: Loose Ball by Kuzmic Soares Misses — retained
- 28: Poulsen Goal — retained
- 29: Poulsen Own Goal — retained
- 30: Poulsen Blocks Kuzmic Picks Up — retained
- 31: Poulsen Blocks Kuzmic Misses — retained

## FreeKickShotBranchDefinition (12)

- 01: Natural one missed blocks off target — passed: `Runs/20261106T170036296Z/results.xml`; FK01 corrected camera/restart/oracle passed `Runs/20261106T171139863Z/results.xml`
- 02: Noruega 5 failed blocks Kuzmic save roll 2 goal — passed: `Runs/20261106T170036296Z/results.xml`; FK01 corrected camera/restart/oracle passed `Runs/20261106T171139863Z/results.xml`
- 03: Noruega 5 failed blocks Kuzmic save roll 4 handling hold — passed: `Runs/20261106T170036296Z/results.xml`; FK01 corrected camera/restart/oracle passed `Runs/20261106T171139863Z/results.xml`
- 04: Noruega 5 failed blocks Kuzmic save roll 4 handling spill corner south — passed: `Runs/20261106T170036296Z/results.xml`; FK01 corrected camera/restart/oracle passed `Runs/20261106T171139863Z/results.xml`
- 05: Noruega 5 failed blocks Kuzmic save roll 4 spill Vladoiu hit — passed: `Runs/20261106T170036296Z/results.xml`; FK01 corrected camera/restart/oracle passed `Runs/20261106T171139863Z/results.xml`
- 06: Noruega 5 failed blocks Kuzmic save roll 4 spill Vladoiu intercepts — passed: `Runs/20261106T170036296Z/results.xml`; FK01 corrected camera/restart/oracle passed `Runs/20261106T171139863Z/results.xml`
- 07: Noruega 5 failed blocks Kuzmic save roll 4 spill Vladoiu misses — passed: `Runs/20261106T170036296Z/results.xml`; FK01 corrected camera/restart/oracle passed `Runs/20261106T171139863Z/results.xml`
- 08: Noruega 5 failed blocks Kuzmic tie loose direction 1 distance 1 Kuzmic misses — passed: `Runs/20261106T170036296Z/results.xml`; FK01 corrected camera/restart/oracle passed `Runs/20261106T171139863Z/results.xml`
- 09: Noruega 5 failed blocks Kuzmic tie loose direction 1 distance 2 movement — passed: `Runs/20261106T170036296Z/results.xml`; FK01 corrected camera/restart/oracle passed `Runs/20261106T171139863Z/results.xml`
- 10: Noruega 5 failed blocks Kuzmic tie loose direction 6 distance 4 goal — passed: `Runs/20261106T170036296Z/results.xml`; FK01 corrected camera/restart/oracle passed `Runs/20261106T171139863Z/results.xml`
- 11: Noruega 5 failed blocks Kuzmic tie loose Soares misses movement — passed: `Runs/20261106T170036296Z/results.xml`; FK01 corrected camera/restart/oracle passed `Runs/20261106T171139863Z/results.xml`
- 12: Noruega 6 perfect height skips blocks Kuzmic save roll 3 goal — passed: `Runs/20261106T170036296Z/results.xml`; FK01 corrected camera/restart/oracle passed `Runs/20261106T171139863Z/results.xml`

## OobLongBallBranchDefinition (6)

- 01: Left bottom direction 1 throw in away — migrated and passed: `Runs/20261106T041600573Z/results.xml`
- 02: Left bottom direction 2 corner away — migrated and passed: `Runs/20261106T041600573Z/results.xml`
- 03: Left lower box direction 3 corner away — migrated and passed: `Runs/20261106T041600573Z/results.xml`
- 04: Left upper touchline direction 3 corner away — migrated and passed: `Runs/20261106T041600573Z/results.xml`
- 05: Right bottom direction 1 throw in away — migrated and passed: `Runs/20261106T041600573Z/results.xml`
- 06: Right bottom direction 6 goal kick away — migrated and passed: `Runs/20261106T041600573Z/results.xml`

## HeaderAtGoalBranchDefinition (13)

- 01: No rush save roll 2 goal — retained
- 02: No rush save tie direction 5 distance 5 goal — retained
- 03: No rush save tie direction 6 distance 5 goal — retained
- 04: No rush save handling spill direction 5 corner north — retained
- 05: No rush save handling spill direction 1 corner south — retained
- 06: No rush save handling spill direction 2 distance 1 Kuzmic misses movement — retained
- 07: No rush save handling hold — retained
- 08: Rush aerial 6 GK wins challenge save and hold no handling — retained
- 09: Rush aerial 5 direction 6 distance 6 goal — retained
- 10: Rush aerial 5 direction 5 distance 6 corner north — retained
- 11: Rush aerial 5 direction 2 distance 1 movement no Kuzmic interception — retained
- 12: Rush aerial 5 direction 2 distance 2 movement — retained
- 13: Rush aerial 4 goal — retained

## Runtime matrices

- Header save jumpers: handling spill false/true × occupied landing hexes 0/1/2 = 6. All passed `Runs/20261106T165113194Z/results.xml`; retired.
- Stationary nutmeg: difficulties 1/2/3 × keyboard/defender click = 6. All passed `Runs/20261106T041752093Z/results.xml`; retired.
- Nutmeg steal/foul audit: miss, foul, injury, post, recover, red = 6. Five passed `Runs/20261106T053532210Z/results.xml`, corrected post setup passed `Runs/20261106T054158602Z/results.xml`; retired.
- Recovery box move: standard, FTP, steal, long, long_direct, header, tackle_stay, tackle_reposition, loose_recovery, loose, same_box × normal/mirrored = 22. Migrated and retired after native GK coverage passed.
- Loose-ball box entry: tackle/header/shot × normal/mirrored = 6. Migrated and retired after native GK coverage passed.
- Outside-GK attacker-win box reposition: foot/dive × normal/mirrored = 4. Gameplay and authored duel presentation passed `Runs/20261106T035302342Z/results.xml`; retired.
- Outside-GK positional foul variants include stale last touch, forced second injury, hands/feet, behind/normal, mirrored direction and leniency rolls; retain their named audit entries until equivalent full flow/presentation passes.
- Additional explicit call parameters remain indexed by the method inventory; this list is not a claim that every combination of all optional parameters was a released legacy case.
