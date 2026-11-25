# Shooting and heading path cache comparison

Compared the committed caches from `bde3631` with the final assets regenerated
on 2026-11-18 by `CounterAttack/Room/Rebuild Shooting Path Assets`. Coordinates
are `(x, y, z)`. Target numbers match the diagnostic scene's north-to-south
numbering: target 1 has `z = 3`, target 2 has `z = 2`, and target 7 has `z = -3`.

## Target eligibility

No origin gained or lost a target. All 432 `CanShootFrom` origins have the same
`CanShootTo` sets as before, and all 158 `CanHeadFrom` origins have the same
heading target sets. The shooting cache still has 2,408 routes; the heading
cache still has 764. The representative upper-post cases are:

| Origin | Target | Eligibility |
|---|---|---|
| `(16, 0, 8)` | `(19, 0, 2)` (2) | Rejected: centerline outside post |
| `(16, 0, 8)` | `(19, 0, 1)` (3) | Accepted: centerline inside posts |
| `(16, 0, 7)` | `(19, 0, 2)` (2) | Accepted: centerline on post |

The same centerline cases passed for the west goal.

## Hex coverage per route

Counts below compare path *membership*; ordering is discussed separately.

| Change to one path | Shooting routes | Heading routes |
|---|---:|---:|
| No hex added or removed | 2,248 | 764 |
| Two hexes removed | 160 | 0 |

No route gained hexes. The largest change to an individual route is two removed
hexes. Across all shooting routes, 320 hex memberships were removed; heading
path membership is identical to the baseline.

Examples of removed hexes:

| Origin | Target | Removed hexes (none added) |
|---|---|---|
| `(8, 0, 0)` | `(19, 0, -1)` (5) | `(13, 0, 0)`, `(14, 0, -1)` |
| `(8, 0, 0)` | `(19, 0, 0)` (4) | `(13, 0, -1)`, `(14, 0, 1)` |
| `(11, 0, 2)` | `(19, 0, 1)` (3) | `(13, 0, 1)`, `(17, 0, 2)` |
| `(-8, 0, 0)` | `(-19, 0, -1)` (5) | `(-13, 0, 0)`, `(-14, 0, -1)` |

The largest total change from one origin is four removed hex memberships across
two target routes, for example `(8, 0, 0)` and `(-8, 0, 0)`.

## Minimum overlap area

The visible ball remains 0.35 wide (radius 0.175). A hex now enters a ground
path only when its shared area with that capsule is at least **0.0005 square
world units**. The capsule's round ends are approximated by 32 segments per
semicircle for this area calculation. The contact test still handles tangency,
but tangent or tiny-area contact alone does not add a path hex.

Before this area rule, `(9, 0, 0) -> (19, 0, 3)` had four new corner hexes:
`(10, 0, 0)`, `(13, 0, 2)`, `(15, 0, 1)`, `(18, 0, 4)`.
Each overlapped by about 0.000254 square units and is now excluded. Across the
first physical-disc rebuild, all 600 newly added shooting memberships had areas
at or below 0.000260, while retained shooting contacts began at 0.000693.
The heading cache's 152 added memberships were also below 0.000260, while
retained heading contacts began at 0.002941. The 320 baseline-only shooting
memberships have zero physical overlap and remain excluded.

## Path ordering

The regenerated arrays are ordered along ball travel. Previously, 1,815
shooting and 537 heading arrays were not monotonic along their centerlines.
None of the new arrays has that issue. This explains why a simple ordered-list
comparison reports 1,858 shooting and 546 heading arrays changed, while only
160 shooting routes changed hex membership. Route order matters
for movement and interceptions, so it is intentional to keep the corrected
travel order.

## Validation

Unity compiled with zero errors and zero warnings. Focused native tests passed:
32 inspector EditMode, 15 geometry EditMode, 1 cache assignment EditMode, and
1 diagnostic PlayMode; none failed or skipped. The XML artifacts are listed in
`Assets/Editor/FeatureTests/ShootingPathInspection/TASKS.md`. No broader gameplay
suite was run.
