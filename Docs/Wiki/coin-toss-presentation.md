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
