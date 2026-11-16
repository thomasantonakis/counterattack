# Online goalkeeper movement choices

- [x] Reproduce omitted moving/stationary outside-box keeper choices and GK-wall choice.
- [x] Publish dive/foot/stand with the actual defending keeper as decision owner.
- [x] Reuse existing saving/zero-Tackling duel rules, receipts and continuation.
- [x] Admit a wall child decision while its dribble parent is suspended at the reached hex.
- [x] Preserve guest inactivity, wrong-team rejection, duplicate receipts and overlapping holds.
- [x] Verify both attacking teams, both pitch directions and actual win/lose/tie continuations.
- [x] Round-trip publications and commands through the wire codec and detached replica.
- [x] Register each parameterized case separately in the deferred Mac manifest.

Full Room is now **v11**, wire remains v7. The published ground-duel adapter now
offers the source game's goalkeeper hand dive, and stationary outside-box keeper
choices register that player once before resolving. A wall dive suspends its
parent movement coroutine after reaching a hex. Only that specific suspended
parent is exempt from the ordinary movement-busy guard; it cannot hide the child
choice/roll. Other movement, reposition, modal, pause and connection gates remain.
Mandatory throw-in restrictions still suppress an unavailable stationary foot tackle.

Compilation: clean through the live Editor, zero C# errors. Own-feature results:
**72 distinct passed, zero failed/skipped**:

- Four `OnlineKeeperGroundChoicesEditorTests` at `20261116T164829156Z/results.xml`.
- 68 `OnlineKeeperGroundChoicesPlayModeTests` at `20261116T165422719Z/results.xml`.

Native tests use real movement/selection, both authenticated team identities,
rigged dice and existing authored duel panels. Outcomes include straight red and
free-kick handball, attacker reposition, wall recovery/distribution, tied loose
ball and release of the dribble parent. They assert actor, ownership, possession,
statistics and draw counts. These are isolated deterministic gameplay boards;
physical paired full-Room acceptance and internet latency are unexecuted.

Affected verification: **nine passed, zero failed/skipped**:
outside-box shot key mapping two (`164954158Z`), moved-defender standing two
(`165009010Z`), legacy keeper wall one (`165027000Z`), and four exact tackle/nutmeg
outcome/hold cases (`165045999Z`, `165104329Z`, `165122755Z`, `165140895Z`).
All timestamps have prefix `20261116T` and are under
`TestResults/GameplayRegression/Runs/`, with native XML and progress retained.

The 16-case reproducer at `20261116T080729521Z` has genuine missing-choice
failures. Its polling response failed once; the completed run was retrieved,
not restarted. Earlier 16/34 passing iterations are retained without double
counting. The new Editor was opened by the user after an earlier native crash;
Codex did not launch, kill or restart Unity. No pending run survived that gap.
The manifest has all 72 individual additions and validated definition hashes;
13 verifier tooling unit tests passed.

Windows exports currently precede this batch (v10). Refresh all three profiles
before paired use; the normal full-game clients must both rebuild for v11.
For future broader user-run verification: `Assets/Tests/PlayMode/Features/OnlineGroundDuels`
fixture `OnlineGroundDuelPlayModeTests` / category `OnlineGroundDuels`, since
these decisions share the ground-duel adapter. No complete suite was run.
