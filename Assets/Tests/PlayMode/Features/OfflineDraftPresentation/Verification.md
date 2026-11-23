# Offline Draft presentation and first-pick roll

The fixture enters the real Draft scene with a temporary save, deals cards
through the normal first-pick panel and restores the application’s save context
after each case. It covers Hot Seat and Single Player.

- [x] Reproduce rating text behind the circles. The old offline styling pass
  moved the circle above the number when the shared prefab had already placed
  it underneath. Both paths now use the authored card and its rating renderer.
- [x] Reproduce the missing four-sided hover. The shared slot had a full border
  but only the Online path activated it. Both drag paths now track the raycast
  target and share the border with the mode’s existing eligibility.
- [x] Reproduce the first-pick text wrapping. The authored footer used centered
  anchors with a negative width. The label and Online hit area now stretch
  across the panel at the existing footer position.
- [x] Reproduce the missing countdown. The offline first-pick path now starts a
  five-second timer per team. R can still roll immediately. The Room coin toss
  uses its existing controller options.

The [baseline XML](../../../../../TestResults/GameplayRegression/Runs/20261123T213713586Z/results.xml)
records all eight failures at the affected assertions.

## Focused results

Unity compilation completed with zero errors.

| Run | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: |
| [Offline Draft PlayMode](../../../../../TestResults/GameplayRegression/Runs/20261123T213848629Z/results.xml) | 9 | 0 | 0 |
| [Shared Online Draft footer and averages](../../../../../TestResults/GameplayRegression/Runs/20261123T213936912Z/results.xml) | 2 | 0 | 0 |
| [Online card drag](../../../../../TestResults/GameplayRegression/Runs/20261123T213951510Z/results.xml) | 1 | 0 | 0 |

The shared scene edit was saved through the Editor. The existing Draft
serialization contains two blank YAML values with trailing spaces.

## Kickoff parity follow-up

The previous width assertion missed the footer's vertical position. The first-pick
footer and its click area now use the authored kickoff footer anchors, pivot,
and offset. The fixture compares that placement and asserts that the full prompt
sits below the outcome. The first-half kickoff panel also uses the five-second
turn timer. The offline cases exercise both teams in Hot Seat and Single Player.

Compilation completed with zero errors. The [offline fixture rerun](../../../../../TestResults/GameplayRegression/Runs/20261123T214933110Z/results.xml)
passed 11 cases. The [native Online toss rerun](../../../../../TestResults/GameplayRegression/Runs/20261123T215033414Z/results.xml)
passed 2 cases, covering auto-roll, holds, and guest display for both kickoff
and penalty shootout. An additional Editor check advanced the Online Draft
opening authority through Home at five seconds, Away at ten seconds, and the
kickoff choice at fifteen seconds without roll input. All four assertions passed.
