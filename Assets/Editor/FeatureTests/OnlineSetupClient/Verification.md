# Online setup client and authored view

- [x] Atomic owned confirmation and host start only after both agree.
- [x] Preserve pending/local edits over remote updates and rejected replies.
- [x] Reconnection, stale/malformed snapshots, duplicate replies, no echo loops.
- [x] Author scene-owned view/status and test existing Create New Game controls.
- [x] Focused native verification and individual deferred Mac cases.

Compilation separately clean. **48 distinct cases passed**:
- `OnlineSetupClientEditorTests` / category `OnlineSetupClient`: 24 distinct
  cases. Initial 23/1/0 at `Runs/20261114T230934367Z/results.xml`; both start
  cases then 2/0/0 at `Runs/20261114T231044922Z/results.xml`. One retained fixture
  counter leaked between parameter cases; Create now resets counters/queue.
  Production behavior and assertions were preserved.
- `OnlineSetupViewEditorTests` / category `OnlineSetupView`: 24/0/0 at
  `Runs/20261114T231851799Z/results.xml`, both host assignments and both local roles.
- Affected agreement cases: 24/0/0 at `Runs/20261114T231112299Z/results.xml`.
- Affected authored setup capture/save guards: 5/0/0 at
  `Runs/20261114T231944951Z/results.xml`.
Do not add overlapping reruns. No full suites ran.

Client edits stay local until confirmation. Changes after readiness send unready;
changes while a request is pending survive its reply and revoke that earlier
confirmation. Guest edits cannot alter opponent identity or shared settings.
Malformed/old states cannot clear a pending request. Exactly one agreed-Draft
notification follows the accepted host start, even with duplicate state delivery.

Atomic Confirm merges owned team fields and host-only shared settings. An actual
configuration edit clears the other agreement; confirming unchanged settings
preserves it. This lets both managers confirm the same configuration without
continually revoking each other's readiness.

`OnlineSetupView` and its status object are authored in CreateNewGameScene via
`author_online_setup_view`, saved through the Editor and available in Edit Mode.
Status participates in the existing Header layout and stays hidden offline.
The view binds the existing confirmation button, locks fields by authenticated
role, preserves unsent input on remote updates and displays ready/wait/start.
Online capture reads controls without refreshing dropdowns or resolving randomness;
local offline save still finalizes its existing dropdown/random-referee behavior.
Normal Room was restored clean; unrelated Room/font/settings edits are excluded.

**Boundary:** the persistent authenticated lobby transport still needs to supply
this view's client. Main Menu multiplayer entry, host/join controls, scene handoff,
Draft authority and normal Room menu integration remain next. This is not an EOS
paired frontend acceptance claim. Latest exports precede this frontend batch.
Every case is registered for the explicitly deferred son's Mac run.
