# Normal-game Windows/Mac walkthrough

Use this with Codex guiding each checkpoint. The two automatic EOS relay probes
have passed; this walkthrough checks the human UI and normal scene flow.
It is not the full verification script. No full test suite is needed here.

## 1. Update the Mac

Save any unsaved Unity work before updating. In the Mac Terminal:

```sh
cd "$HOME/Documents/Projects/Personal/counterattack"
git status --short
git fetch origin
git switch codex/online-foundation-personal-mac
git pull --ff-only origin codex/online-foundation-personal-mac
git log -1 --oneline
```

Preserve local edits. If Git reports a conflict or refuses to pull, stop and
share its message; do not reset, clean or discard files.

## 2. Build and open the normal game

Manually open this checkout in Unity 6000.5.3f1. Wait for compilation, save dirty
scenes and leave Play Mode stopped. Then run:

```sh
bash Documentation/Setup/BuildEOSStandaloneMac.sh game
open Build/OnlineGame/Mac/Counterattack.app
```

Tell Codex when the Mac is at the main menu. Codex prepares the Windows normal
game and captures its log; the human operates the visible menus on both machines.
The applications are Counterattack, rather than either automated proof player.
Both machines need internet; sharing one Wi-Fi network is fine initially.
Keep both awake and the Windows laptop lid open throughout the paired window.

## 3. Connect and agree the setup

1. On both machines choose Multiplayer in the normal game's main menu.
2. On Windows use Host. Wait for a lobby code and share it with the Mac.
3. On the Mac enter that code and use Join.
4. Compare connection messages and the displayed team ownership on both screens.
5. Each manager changes their own team name, outfield kit and goalkeeper kit.
   Windows controls the shared match settings. Confirm that the other screen
   receives each change. Follow the displayed ownership; do not use offline saves.
6. Both managers use Confirm selections. After both agree, Windows uses Start Draft.

Checkpoint: both screens have entered the same Draft, with matching teams and
settings. Tell Codex before continuing. If a button is disabled, share the status
message; do not attempt to bypass ownership or confirmation.

## 4. Complete Draft and enter Room

Follow the displayed active team to roll and make the turn's card selections.
Arrange your own slots using the authored controls/drag and compare the result
on the other screen. Both confirm their rosters; Windows starts the match once
both are ready. Complete any opening roll/choice when the UI requests it.

Checkpoint: both clients enter the full Room. Compare team identities, token
positions, ball hex, score, clock, team in attack and possession before any play.

## 5. Play one short sequence at a time

Codex will guide these in order, checking logs and both screens at each boundary:

- Kickoff and one movement action, then a safe ground pass.
- A contested pass with the defending manager's roll.
- A shot and its save/goal/restart continuation.
- High/long delivery and a header when available under the current rules.
- Shared pause/resume and an available substitution.

Only the team named by the current decision submits that action. Confirm
published outcomes and the next available decision on both machines; personal
camera/selection changes do not establish synchronized gameplay.

Actual network interruption/recovery is a separate later checkpoint, arranged
with Codex before disconnecting anything. Do not close either game or disable
network mid-action without arranging that test. Full-match completion, other
set pieces and measured latency remain further acceptance work.

## Evidence and problems

Keep the games open at the first discrepancy and give Codex the last action,
which machine performed it and the two visible status messages. A screenshot
can help with UI/layout problems. Codex inspects Windows logs directly. The normal
frontend keeps run-specific evidence below Application.persistentDataPath /
OnlineSessions; these are separate from the automatic proof reports. Preserve
that run and any source-save identity if diagnosing a specific match.

The original automatic report and its recovered paired passes are documented in
[OnlinePairingRecovery.md](../Testing/OnlinePairingRecovery.md). A successful
proof does not by itself establish this complete normal-game walkthrough.
