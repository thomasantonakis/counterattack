# Display resolution verification

- [x] Run the `DisplayResolutionEditorTests` fixture for the 16:9 viewport and fullscreen and windowed preset filtering.
- [x] Verify the authored dropdown wiring next to Audio in Main Menu and in Pause → Game Settings through the Unity Editor.
- [ ] On Windows and macOS standalone builds, verify first launch uses fullscreen and a chosen preset is remembered.
- [ ] Select Fullscreen at 1280 × 720 and check that the monitor stays covered while the player renders at the lower resolution.
- [ ] Drag a window to 16:10, ultrawide and portrait proportions. Check the pitch, menus and HUD stay inside the centered 16:9 area.
- [ ] Change the setting while paused, resume and check that mouse picks align with the visible pitch.
