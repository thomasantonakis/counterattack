# Main menu version panel verification

- Open `Assets/Scenes/MainMenu.unity` and confirm the panel stays anchored to the bottom-left across window sizes.
- Confirm the game version matches `Application.version` and that protocol, rules and catalog prefix match `OnlineFrontendSessionPolicy.Compatibility()`.
- Confirm the catalog text shows the first eight fingerprint characters for compact display.
