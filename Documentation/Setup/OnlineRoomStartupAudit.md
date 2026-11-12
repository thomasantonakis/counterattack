# Room authority/replica startup audit

Inspected from this Windows checkout through live Pipeline hierarchy and source.
This is an implementation boundary record, not guest Room acceptance evidence.
Room has not been changed by the recovery/manual prepared-board batches.

## Serialized scene boundaries

The live clean Room hierarchy has 39 roots. Rules are separate roots:
MatchManager, GameInputManager, Player Tokens (PlayerTokenManager), HexGrid,
MatchBall (Ball), MovementPhaseManager, GroundBallManager, FirstTimePassManager,
HighPassManager, HeaderManager, LongBallManager, LooseBallManager,
OutOfBoundsManager, FreeKickManager, ShotManager, FinalThirdManager,
GoalFlowManager, KickoffManager, GoalKeeperManager, ThrowInManager, GKPushManager,
OOBPushManager, OffsideManager, PenaltyKickManager and PenaltyShootoutManager.
HelperFunctions, RoomDecisionManager and GameDebugMonitor are separate roots.

MainCanvas also owns PauseMenuManager. Disabling that entire canvas would hide
designer-owned match statistics and personal presentation. RoomPresentation
contains RoomInspectionCard, RoomTips, RoomHexEligibilityOverlay and RoomMatchIntro.
Main Camera owns CameraController. RulesViewer is separate. Treat these mixed
presentation/interaction objects individually; do not disable every scene root.

## Active mutation and persistence routes

- MatchManager.Awake assigns the singleton and ensures penalty/offside managers.
  Its OnEnable subscribes to GameInputManager events. Start resets trace/log
  services, loads local JSON, resolves opening possession/directions, waits for
  rosters/grid and starts restore or standalone restart behavior.
- MatchManager.Update advances decision timing with unscaled delta, updates
  the match clock and flushes live logs. Setting timeScale=0 alone cannot gate
  all of these routes; the tested connection hold also sets the match blocker.
- MatchManager.OnApplicationPause/OnApplicationQuit/OnDestroy can flush logs.
  Disabling Update alone cannot protect guest saves or callbacks.
- MatchManager.LoadGameSettingsFromJson ensures ApplicationManager exists,
  inspects explicit save context and can fall back to PlayerPrefs/newest save
  or the disposable Editor Room fixture. A guest must enter before this path.
- ApplicationManager.GetSaveFolderPath creates a directory; setting active
  save context mutates a persistent singleton. Leave offline context intact.
- RoomSaveService.CreateLoadedRunCopy, SaveInPlace, SaveAs and SaveToPath are
  persistence entry points. RoomLiveLogService appends event segments, writes
  active-save metadata and writes its event index. Guard these even against a
  stale button or direct callback; an inert guest must not acquire a live path.
- GameInputManager dispatches physical OnClick/OnKeyPress and independent
  synthetic token/hex/key entry points. Host input needs the same semantic
  gateway as the guest. Disabling a physical key handler is insufficient.
- GroundBallManager.OnEnable subscribes mutation handlers. Other rule managers
  have their own subscriptions, callbacks and coroutines. Authored disabled
  behaviours still need an Awake audit; disabling MonoBehaviour is not proof
  that Awake or public button methods cannot mutate.
- PlayerTokenManager.Start creates the live roster. Guest roster/catalog IDs
  must come from a validated publication before rendering authored replicas.

## Remaining implementation

Introduce explicit online context before normal scene activation. Author the
owned Room guest configuration through Pipeline; preserve offline Room and
Canvas/MatchStatsUI/Stats. Prove normal guest load, inert authority callbacks,
no random draws/clock advancement/save writes and usable personal inspection
with dedicated lifecycle tests. Then connect the existing semantic pass gateway,
manual controls and authenticated recovery to real Room host state and publish
validated roster, timing, outcome and continuation values to that inert guest.
Neither these audit findings nor the prepared-board tests complete steps 3–4.
