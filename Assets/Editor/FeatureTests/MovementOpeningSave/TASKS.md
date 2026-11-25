# Autocommitted Movement Phase opening saves

- [x] Reproduced the save refusal before the first move in both attacking directions.
- [x] Saved and reloaded both before and after a provisional token selection.
- [x] Restored the original board, selection, eligibility, match clock, and decision budget. Verified the paused balance and expired state also survive reload.
- [x] Verified that accepting a nutmeg, starting or completing a move, and forfeiting the opening all block saving.
- [x] Ran `MovementOpeningSavePlayModeTests` in Unity. Seven cases passed.
