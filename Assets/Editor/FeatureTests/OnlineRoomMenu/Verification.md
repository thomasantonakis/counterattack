# Online full Room menu

- [x] Author inspectable pause/menu, settings summary, requester resume and leave controls.
- [x] Route pause/resume through existing semantic controls and ownership checks.
- [x] Close native gameplay subscriptions synchronously before a menu unloads Room.
- [x] Verify host/guest owner/passive views, hold state and actual leave navigation.
- [x] Register each native case for deferred Mac verification and refresh exports.

Settings are read-only here. Substitution, settings agreement and shootout-order
commands remain separate integration work. No paired internet acceptance claim.

Native evidence under `TestResults/GameplayRegression/Runs/`:
- `20261115T184750954Z/results.xml`: two close/context cases passed.
- `20261115T185132333Z/results.xml`: six actual scene menu cases passed,
  covering host/guest owner/passive views, connection holds, settings text fit,
  responsive Canvas configuration and safe MainMenu navigation.
- `20261115T185223853Z/results.xml`: four affected encoded kickoff cases passed.
- `20261115T185303132Z/results.xml`: 13 affected handoff lifecycle cases passed.
Compilation was separately clean. Final runs have zero failures/skips.
`20261115T184805915Z` retains the first six-case pass before responsive scaling.
All eight new native cases are individually registered for deferred Mac testing.

The new menu is authored in OnlineRoom; original Room and designer-owned Stats
layout are unchanged. The online Canvas now scales from 1920x1080. Pause/resume
use existing authenticated semantic controls. Passive managers cannot resume
another manager's pause; connection-held controls remain disabled, while Leave
remains reachable. Closing disposes Room gameplay synchronously before unload.
A fallback injected Room session also disposes without flushing its save.

The four encoded kickoff regressions exercised gameplay with the new menu and
hold presentation present. No whole suite or paired internet run was executed.

Normal Windows game refresh succeeded as `build_0a84c3ce640f`; report:
`TestResults/GameplayRegression/Runs/20261115T185539Z-normal-Windows-build/build-summary.json`.
It includes all eight normal scenes and the authored menu. Nine warnings and the
existing nonfatal EOS `vswhere.exe` diagnostic remain; no C# compilation errors.
The isolated proof exports/local/login results from roster contract v2 remain
applicable: their scenes do not contain this full Room menu/frontend lifecycle.
Thirteen verifier tooling tests passed; native Mac execution remains deferred.
