# Referee preview verification

- Run `RefereePreviewEditorTests` in EditMode. It checks scene placement, player-card sizing, all four referee faces and flags, Room double-click and Space, and draft hover enter/exit.
- In Room, check the yellow badge above Rules, then open the card with double-click and with Space. Compare its size to the full player inspection card.
- In Draft and FreeDraft, check the yellow badge beside the title and confirm that the card disappears on pointer exit.
- With online managers and a spectator connected, check the displayed name and value against the chosen referee. Open the card on each client in turn and confirm the other clients' views stay closed.

Unity 6000.5.3f1 compiled with zero errors. The focused run passed 8 cases with zero failures and zero skips. Its native results and the Room and Draft UI renders are under `TestResults/GameplayRegression/Runs/20261127T173711207Z/`. Live multi-client verification is pending.
