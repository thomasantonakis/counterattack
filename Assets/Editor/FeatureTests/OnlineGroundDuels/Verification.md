# Online ground duels

- [x] Reproduce missing nutmeg/tackle commands through real movement.
- [x] Typed nutmeg attempt, defender tackle/stand, both-team rolls and reposition.
- [x] Attacker/defender wins, tied loose balls, deadlines, holds and cancellation.
- [x] Focused native tests, compilation and retained artifacts.
- [x] Append all new cases to the deferred son's Mac manifest.

Compilation separately clean. Category `OnlineGroundDuels`, fixture
`OnlineGroundDuelPlayModeTests`: **36 passed**, zero failed/skipped, in
`TestResults/GameplayRegression/Runs/20261114T150325167Z/results.xml`.
Initial 26 missing-command/stage reproductions are retained in
`20261114T145349365Z/results.xml`; the first implementation passed all 26 in
`20261114T145813737Z/results.xml`. The final extension adds optional reposition
forfeits, mandatory nutmeg reposition rejection, occupied target/guest rejection,
and multiple candidates with the unselected defender's real pre-nutmeg steal.
Both directions cover both duel winners, owned roll order/actors, automatic
deadlines, held results, cancellation, actual reposition/possession and completed
tied loose-ball scatter. Actor selection is a server decision, not a remote team
claim or raw key. Reposition animation uses the existing presentation-frame lane.

Affected online dribble category: **12 passed**, zero failed/skipped, in
`20261114T150922447Z/results.xml`. No full suites ran. Each of the 36 cases has
a separate entry in OnlineMacTestManifest.json; Mac execution stays deferred.

Fouls/cards/injuries and goalkeeper challenge variants
remain separate follow-up coverage; no full match or paired acceptance is implied.

Affected offline tackle/reposition regression: 1 passed, zero failed/skipped,
`20261114T151340606Z/results.xml`.
