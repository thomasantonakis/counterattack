# Post-match report

## Stage 1 — authored tabs and player leaderboards (implemented)

The existing EndGamePanel score, scorers, match stats and main-menu action remain.
An optional report controller on that panel adds Match Stats / Top Performers /
Pitch tabs. Legacy panel instances without the component retain their original view.
Pending-shootout notices hide the tabs; completed reports reset to Match Stats.

Top Performers reads existing playerStats without creating/updating entries.
Metrics: Distance (in paces), recoveries, passes, duels won, goals and assists.
Passing accuracy is displayed with completed/attempted passes; recoveries display
actual/xRecoveries. Pass accuracy and Saves have no separate metric buttons.
Named team filters sit directly below the main tabs and appear only on Top
Performers and Pitch. Match Stats always shows both teams. Twelve-row paging remains fixed at
the bottom of the panel, outside the scroll content. All objects are authored.
Ground/aerial passing and duels combine their respective counters. Recoveries use
possessionWon alone, since interceptions already increment it. Percentages use all
recorded ground/aerial attempts with no minimum eligibility threshold. Completed
hanging passes feed those same existing completion counters. The passes leaderboard
sorts by completions descending, attempts ascending, then name ascending. Duels
uses wins descending, contests ascending, then name ascending, and displays success
percentage. Attempted passes and contested duels include players with zero
successes; other metrics require positive values. Unknown legacy team attribution stays Unassigned.
Stable identities keep same-named players separate. No rating is inferred.

Authoring: `Tools/Counter Attack/Add Post Match Report Tabs` upgrades the existing
prefab and loaded Room. It adds to the post-game view and never edits the live
`Canvas/MatchStatsUI/Stats`. All new objects exist before Play Mode.

Evidence: `Assets/Editor/FeatureTests/PostMatchReport/TASKS.md`.

The full-time report now sets the existing match pause flag and immediately pauses
the scene's GameplayAudioDirector without a pause transition cue. Stadium loops,
chants, reactions, whistle and match effects stop; existing pause music plays
according to the music preference. Repeated report display keeps the pause.

## Stage 2 — pitch and heatmaps (implemented)

The read-only projection uses existing `snapshot.action_boundary` token positions
and committed `token.move` paths. No recording hooks or save schema changes were
needed. Stationary players and players with no ball touches receive positions.
Setup/reset/bench/sent-off records are excluded. Direction is normalized through
opening choices and half changes, including the separate extra-time choice.

The authored Pitch view has 36 number markers, hover previews, click-to-pin,
three independent dropdowns in one row (period, possession, heatmap), and an empty state when
history is unavailable. Averages are weighted by recorded resolved actions rather
than wall time. Heat intensity is relative to the selected player's own samples.
Historical saves without opening choices use the historical default direction.
Old saves with incomplete event history cannot produce a complete match map.
The schematic uses the actual pitch markings' 27.5:21.65 proportions. Period
choices include each applicable regulation/extra-time half and aggregate views.
Possession offers All game / Attacking / Defending; heatmaps offer Position /
Movement. Each popup fits all its options without scrolling.

Evidence: `Assets/Editor/FeatureTests/PostMatchPitch/TASKS.md`.

## Later stages

1. Record pre-resolution probabilities and complete contest outcomes for luck:
   expected successes = sum(p), surplus = sum(actual - p). Count each contest once;
   respect tie/foul/jackpot branches and exclude debug overrides.
2. Calibrate transparent player contribution ratings using attacking, creation,
   defending, goalkeeping, errors and discipline. Avoid duplicate event rewards,
   account for roles/exposure and explain the score. Keep luck separately visible.
3. Extend with chance maps, passing connections, recovery-to-shot chains and
   factual manager insights when recording coverage supports them.

Each stage needs its own focused tests. Gameplay recording changes must add the
project's required gameplay coverage; presentation checks alone do not cover them.
