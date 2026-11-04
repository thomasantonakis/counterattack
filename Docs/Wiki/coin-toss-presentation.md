# Coin Toss Flip

Start Game in both Draft and FreeDraft opens the scene-authored `MainCanvas/CoinTossUI` before saving rosters and loading Room. It reuses the first-pick panel's ordinary 1–6 rolls, jersey colors, animated active die and two-second result pause. Home rolls with R, then Away rolls with a fresh R. Jackpots are excluded. A tie waits two seconds, clears both dice and asks for two new rolls.

After a winner's two-second pause, the same panel retains both results and expands with three buttons. Directions always describe the choosing team's first-half attack:

1. First Half Kick Off: the winner keeps kickoff, and the losing manager chooses a direction. The kickoff button becomes disabled.
2. First Half Left to Right: the chooser starts on the left, and the opponent gets kickoff.
3. First Half Right to Left: the chooser starts on the right, and the opponent gets kickoff.

The panel identifies the manager currently choosing by team name. Single-player CPU managers always take kickoff when available. When forced to choose direction, a CPU selects its usual direction (Home left to right, Away right to left). Hot-seat managers make their own choices. Rolls continue using the same manual R input as the draft roll-off.

The final `openingSetup` object is stored alongside `rosters` in the draft's save. Home/Away identities, names, kits and rosters stay attached to their original teams. Room applies the chosen kickoff and opposite attack directions before creating tokens. Regular formation coordinates and attacker/defender occupancy flags use those choices; each goalkeeper starts in its own team's chosen half. A fresh draft clears an inherited runtime snapshot. Later saves keep `openingSetup`, but restoring a runtime snapshot restores the live directions and possession, including after halftime. Saves without opening choices retain their existing defaults.

`CounterAttack > Draft > Ensure Coin Toss Panels` authors or validates the panels in both scenes in edit mode. Visuals, labels, dice, button objects and modal pointer blockers are saved scene objects. Scripts only animate/update those existing objects and expand the existing panel.

`CoinTossPresentationEditorTests` covers both winners and all choice combinations, unavailable kickoff choices, CPU policies, saved team identity, formation halves, scene bindings, ties, result timing, retained dice and button interactions. `DraftOrderPresentationEditorTests` continues to cover first-pick behavior. The user runs Edit Mode tests and Play Mode manually.

For a quick manual check, finish either draft and press Start Game. Use Ctrl+Shift+1–6 for deterministic numeric rolls if needed. Try an Away win with Left to Right: Away tokens should start on the left, Home tokens on the right, Home should kick off, and both teams should keep their original names, kits and roster identities. Also check winner kickoff followed by the opponent's direction choice, a tie, and a single-player CPU winner/loser.

## Room: extra time

When a tied regulation match qualifies for extra time under its configured tiebreaker, Room opens its own saved `MainCanvas/CoinTossUI` before moving teams or starting extra time. The options are Extra Time First Half Kick Off, Extra Time First Half Left to Right, and Extra Time First Half Right to Left. The winner/loser choice rules match the draft toss. CPU managers take kickoff when available.

The chosen directions replace the automatic side switch at the start of extra time. Teams move to the matching regular formations with their original Home/Away identities. At extra-time halftime, teams switch sides normally and the team that did not kick off the first extra-time half takes the second-half kickoff. Regulation kickoff order is independent. Normal halftime still switches sides and alternates the regulation kickoff, without another toss. Extra-time eligibility, substitution credit, and the existing post-goal-celebration gate remain in place.

## Room: penalty shootout

When a tied final score qualifies for penalties (directly after regulation, or after extra time), Room opens a fresh toss before shootout order selection. Its options are Take the First Penalty, Left Side, and Right Side. Left/Right selects the fixed pitch end used by **both** teams throughout the shootout; it does not describe a team's attacking direction.

Choosing the first penalty leaves only the end choice for the opponent. Choosing an end gives the opponent the first penalty. CPU managers take the first penalty when available; a CPU forced to choose an end uses the left goal. After both choices, the usual nominee/order panel opens. Beginning the shootout uses the chosen first team, penalty spot (x = -14 or +14), goalkeeper position (x = -18 or +18), and goalkeeper resting positions at the selected end. All kicks and ball resets continue at that end; the teams' attacking directions update for each taker. Sudden death continues using the same alternation and end.

Room routes physical and synthetic R/numeric roll overrides through GameInputManager, consumes unrelated keys, blocks pitch clicks and automated gameplay, and stops the match clock during the toss. Dice retain jersey colors and ordinary 1–6 faces, including tied rounds. Opening choices are kept in `extraTimeOpeningSetup` and `penaltyShootoutSetup`, and manual save/save-as preserves them alongside live snapshots. Saving is blocked while a toss or half reset is unresolved. Fresh drafts clear inherited extra-time/shootout choices.

`CounterAttack > Room > Ensure Coin Toss Panel` authors or validates Room's saved panel. `RoomCoinTossEditorTests` covers choice combinations, tiebreaker eligibility, independent extra-time kickoff order, the extra-time side-switch exception, normal halftime, mirrored shootout positions, both shooting teams at either end, persistence, and Room modal input/clock behavior. Tests and Play Mode remain manual.

Manual regression: use Extra Time & Penalties with a tied regulation score. Keep the current directions when choosing extra-time sides and verify they do not automatically flip; verify the opposite kickoff and switched sides at extra-time halftime. Finish extra time tied, choose the right shootout end with Away shooting first, then check the ball, taker and defending goalkeeper on the right for both teams. Also check Penalties-only, Extra-Time-only, None, an untied final score, tied toss rolls, and CPU choices.

The post-toss nominee panel is resolved from the match scene, including inactive objects, and retains its designer-owned Canvas parent. A globally selected Canvas can be a runtime overlay or belong to another scene; it must not determine whether the shootout panel exists. Validate the panel and its references before entering order selection, and release the coin-toss transition gate in a `finally` block. Unused nominee rows accept null tokens so teams with fewer than 11 nominees can still display their order. `PenaltyShootoutPanelLookupEditorTests` covers competing Canvases, missing/incomplete references with retry, and a one-half/15-minute match's post-toss order UI. For manual regression, finish that match tied, complete the coin toss, verify the order panel opens, then start the first penalty using the selected team and end.
