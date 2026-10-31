# Single-roll resolution presentation

`SingleRollResolutionPresentation` adapts gameplay managers to the existing
`DuelResolutionPanelController`. The overlay has mutually exclusive, scene-authored
`PlayerColumns` (duel) and `SingleRollContent` (single roll) layouts. Sharing one
controller preserves drag/clamp behavior, input ownership, the five-second unscaled
countdown, Enter hold/resume, Escape dismissal/pause reservation, and match-clock hold.
Single rolls use a narrower authored width (1040 versus the duel's 1680), at the
same display scale. Their guidance lists successful numeric rolls, any running-dribbler
foul roll, and jackpot-only accuracy success where applicable. There is no separate
jackpot-enabled label. Movement ZOI checks are labelled “Recovery Attempt”.
No runtime UI objects are created. `Ensure Duel Resolution Panel` authors the new
layout; `Preview Duel Panel/Single Roll` exposes it in edit mode.

| Flow | Wait/input | Presentation and continuation | Probability/jackpot |
| --- | --- | --- | --- |
| Ground pass | `isWaitingForDiceRoll`, R or existing override | Outfield path interaction only; result dismissal precedes success or next interceptor | Same `CalculateRecoveryProbability(token)` as expected recovery log; numeric six even for jackpot |
| First Time Pass | `isWaitingForDiceRoll`, R or existing override | Outfield path interaction only; goalkeeper branches bypass the single layout | Same calculator and natural-success threshold (5 for path blocker, otherwise 6) as FTP expected recovery log |
| Landing headed pass | `isWaitingForInterceptionRoll`, R or existing override | One defender at a time; the wait flag remains raised until dismissal, then the existing interception-check coroutine resumes | Standard recovery calculator; numeric six for jackpot. Existing headed-pass flow has no expected recovery logging call |
| Long Ball landing | `isWaitingForInterceptionRoll`, R or existing override | Existing ordered batch retains its roll calls and override behavior; show each defender result before proceeding | Same recovery calculator as long-ball expected recovery log; numeric six for jackpot |
| Dribbler steal | `isWaitingForInterceptionDiceRoll`, R or existing override | Common interception routine covers pre-nutmeg and post-tackle attempts; fouls expand directly into leniency/injury checks; other results dismiss before recovery or the next defender | Jackpot disabled. xRecovery intentionally matches existing logging, including its unadjusted recovery model when a running dribbler can draw a foul |
| High Pass accuracy | `isWaitingForAccuracyRoll`, R or existing override | Accurate results dismiss before movement; inaccurate results remain above the expanded direction/distance continuation | Existing manager preview calculator and threshold 8. Jackpot is displayed but gameplay uses numeric six |
| Long Ball accuracy | `isWaitingForAccuracyRoll`, R or existing override | Accurate results dismiss before movement; inaccurate results remain above the expanded direction/distance continuation | Threshold 9 or dangerous-target 10; jackpot totals 50. Natural six splits into ordinary six and jackpot with probability 1/12 each when the dice helper is available |
| Standalone loose ball | Direction/distance wait flags, R or existing override | Direction resolves on the left and advances directly to the distance wait on the right; only the final distance result waits for dismissal | No attribute or success probability. Jackpots disabled; numeric direction/distance only |

All random results still come from `ResolveGameplayDiceRoll`; presentation animation
never consumes random state. Overrides keep their existing public entry points.
The legacy HUD prompt is hidden while the overlay is visible, while instruction
snapshots remain available to logging and deterministic/AI input.

## Loose-ball provenance

`LooseBallPresentationContext` is passed explicitly into `ResolveLooseBall` and
captured for that resolution. Every `ShotManager` caller supplies `ShotResolution`.
Header challenge deflections supply it when `headerAtGoalDeclared` is true, before
header cleanup clears that flag. Tackle ties and ordinary header/control deflections
use the default `Standalone`. This distinction does not depend on source type alone:
`GroundDeflection` is used by both shots and tackle ties. It also does not depend on
team identity, die results, panel visibility or whether ShotManager remains active.
Shot-context loose-ball rolls bypass this overlay and retain their existing flow.

Shot blocks, shooter rolls, goalkeeper rolls, aerial duels, and other multi-roll
flows are excluded. Existing tackling leniency/injury checks remain in the duel
layout. Nothing under `Canvas/MatchStatsUI/Stats` is modified.

## Recovery-foul continuation

When a running dribbler draws a foul during a ZOI recovery attempt, the resolved
recovery die stays visible and the same panel expands into the authored leniency
and injury checks. The movement manager adopts that presentation before entering
its existing `HandleFoulProcess`. Check input and result callbacks use the existing
duel ownership; the final injury result starts the usual dismissal countdown,
then the existing advantage/free-kick/penalty flow resumes. The expanded panel
uses the duel width so both check columns remain readable. Standalone recovery
results keep the narrower width. `Preview Duel Panel/Recovery Foul Checks` previews
this layout without entering Play Mode or resolving any gameplay.

## Dice borders, controls and penalties

All five authored dice have a UI Outline bound to `DuelDieView.border`. Runtime
color updates use the kit catalogue primary/secondary palette already used by
the HUD. Face pips, jackpot symbols and fallback numbers use the same secondary
color as the border. Leniency inherits the defender palette and injury the fouled attacker
palette; rolls without a team use the neutral die with a white border.

The controls/countdown footer anchors to the panel bottom in every layout. The
authoring tool reserves the footer height and a gap below the final foul checks
when calculating the expanded height. Penalty-producing foul rolls say “penalty”
in the pre-roll guidance and “PENALTY!” in the resolved outcome.

## Designer editing

All items are saved in Room under `MainCanvas/DuelResolutionUI/Visuals/Panel`.
Use `CounterAttack/Room/Preview Duel Panel` to expose Compact, Foul Checks,
Single Roll, or Recovery Foul Checks without Play Mode. Edit child RectTransforms,
fonts, sizes, sprites and Outline thickness directly in the hierarchy/Inspector.
Set overall panel dimensions on `DuelResolutionPanelController` (Preferred Width,
Single Roll Width, Normal Height, Expanded Height), because runtime fitting uses
those settings. Gameplay fills live text values, visibility and team colors.

## Paired standalone loose-ball rolls

`LooseBallContent` is a scene-authored, two-column layout. Direction is on the
left and distance on the right. The panel remains open between the rolls, keeping
the direction die and result intact while distance waits and resolves. Only after
distance resolves does the shared countdown/Enter/Escape dismissal begin. The
manager retains its existing dice calls, overrides and movement calculations.
Ending the loose-ball phase also cancels an interrupted presentation. Shot-context
loose balls continue to bypass this panel. Use `Preview Duel Panel/Loose Ball Rolls`
to inspect the distance-wait layout in edit mode.

## Scatter continuations and jackpot policy

All six direction/distance dice calls (standalone loose ball, High Pass
inaccuracy, Long Ball inaccuracy) explicitly disable jackpots. The dice animate
numeric faces only and always show numeric results. A supplied jackpot override
normalizes to six through the existing resolver; it has no special scatter effect.
Accuracy checks and tackling dice retain their own existing jackpot rules.

A failed HP/LB accuracy check explicitly reserves the presentation for its own
direction/distance states. A tied outfield tackle explicitly hands its open
presentation to LooseBallManager. Both expand the scene-authored
`ScatterContinuation` below the original dice/result, preserving those fields
while direction resolves on the left and distance waits/resolves on the right.
Only the final distance result starts dismissal, then existing movement resumes.
Shot-context loose balls keep their existing presentation and do not attach here.

Edit-mode previews: `Accuracy Inaccuracy Rolls` and `Tied Duel Loose Ball Rolls`
under `CounterAttack/Room/Preview Duel Panel`.
