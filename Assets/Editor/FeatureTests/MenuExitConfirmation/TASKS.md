# Menu exit confirmation

- [x] Run the `MenuExitConfirmationEditorTests` fixture. Both callback cases passed.
- [x] Run `MenuExitConfirmationPlayModeTests` to verify the authored footer buttons receive pointer clicks. The pointer case passed.
- [x] Verify both frontend scenes reference the shared Quit Game confirmation prefab.
- [x] Verify Room has one pause-menu Back to Main Menu action in the former Quit Game footer.
- [ ] In a standalone match, check direct return when saving is disabled. At a saveable point, check Back, Save and Return, and Return Without Saving.
- [ ] If the save fails, check that the message is visible and the match stays open.
