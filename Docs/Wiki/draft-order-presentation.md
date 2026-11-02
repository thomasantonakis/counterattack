# Regular draft first-pick roll

`MainCanvas/DraftOrderUI` in `Assets/Scenes/Draft.unity` is authored in edit mode. Its root stays active; the `Visuals` child opens only for the regular draft roll-off. All labels, dice, background and pointer blocker are inspectable scene objects. FreeDraft keeps its existing first-pick flow; its only roll panel is the post-draft Coin Toss Flip. International prepopulated rosters still bypass outfield drafting.

After roster slots and goalkeepers are prepared, DraftManager opens the panel before dealing outfield cards. R rolls for the home manager, then a fresh R press rolls for the away manager. Only the active die animates; the first die stays resolved while the second rolls. Labels identify the actual team names, and dice use their jersey palettes. Each gameplay roll is an ordinary uniform 1–6 die. Neither animations nor results have jackpots; cosmetic animation consumes no gameplay randomness. Deterministic Ctrl+Shift number/numpad overrides follow the same input handler. Jackpot shortcuts are ignored, and legacy direct jackpot overrides normalize to a numeric six.

A completed pair remains visible for exactly two unscaled seconds. A winner then closes the panel and becomes the first batch starter; existing alternating batch rules continue normally. A tie clears both results after two seconds and waits for two new rolls. Keys during the result pause cannot skip it or consume additional dice. Cards, CPU picks and random-draft completion stay blocked until the roll-off finishes.

Regular draft turn and batch-start messages use team names rather than Home/Away labels. The first-batch message refers to the roll-off rather than a coin toss. `CounterAttack > Draft > Ensure First Pick Roll Panel` is the guarded authoring command and preserves an existing complete panel.

Edit Mode regression cases cover both winners, numeric ties, the two-second pause, fresh rolls after a tie, numeric-only animations/results, kit colors, synthetic overrides and saved-scene wiring. The user runs tests and Play Mode manually.
