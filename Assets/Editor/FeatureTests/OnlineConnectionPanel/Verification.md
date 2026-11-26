# Online connection panel

- [x] Match Setup and Draft use the bottom-center prefab. Room places the 200 × 64 connection panel below the matching Rules button, in the gap between the instructions and left player-card preview. Offline Room uses its original Rules button.
- [x] The manager panel presents peer round-trip time and a Good, Average or Poor rating. It updates both every second from the existing heartbeat. Offline players and spectators do not see the panel.
- [x] The five-second median drives the initial 180 ms and 350 ms bands. Two replies at least 100 ms above the median can lower the rating to Average. Reply gaps of 2.5 and 3.5 seconds trigger Average and Poor respectively. The ping clears after five seconds without a reply and whenever the session enters recovery.
- [x] Unity 6000.5.3f1 compiled with zero errors. The focused `OnlineConnectionPanelEditorTests` run passed 11, failed 0 and skipped 0. Native XML and per-case progress are in `TestResults/GameplayRegression/Runs/20261126T091514919Z/`.
- [ ] On two separate internet connections, compare the commit printed on both machines. Confirm both managers see the panel through Match Setup, Draft and Room, and check readability at 16:9 and on a taller display. Interrupt one network link and confirm the rating responds, recovery clears stale ping, and a fresh reading appears after reconnect. Compare the rating with the responsiveness of draft picks and Room actions.

## Paired standalone test

Open the project in Unity manually on each machine and leave its Pipeline ready. Run the commands from each checkout's root.

### macOS

```bash
git fetch origin &&
git switch feature/online-connection-panel &&
git pull --ff-only origin feature/online-connection-panel &&
git rev-parse HEAD &&
bash Documentation/Setup/BuildEOSStandaloneMac.sh game &&
open Build/OnlineGame/Mac/Counterattack.app
```

### Windows PowerShell

```powershell
git fetch origin
if ($LASTEXITCODE -ne 0) { throw 'Fetch failed' }
git switch feature/online-connection-panel
if ($LASTEXITCODE -ne 0) { throw 'Branch switch failed' }
git pull --ff-only origin feature/online-connection-panel
if ($LASTEXITCODE -ne 0) { throw 'Pull failed' }
git rev-parse HEAD
if ($LASTEXITCODE -ne 0) { throw 'Could not read the commit' }
& .\Documentation\Setup\BuildOnlineGameWindows.ps1 -Launch
```

The Mac script checks the Editor build report for success before the command launches the app. The Windows script checks the report and launches `Build/OnlineGame/Windows/Counterattack.exe` on success.
