# Standard Pass

The **Standard Pass** is the basic grounded pass action.

In rules terms, this is a **Ground Ball Pass**:

- the ball travels along a ground path
- the path can be safe or dangerous
- defenders may intercept if the pass enters their zone of influence

## When It Is Used

A Standard Pass is usually available when the attacking team has the ball and is choosing its next action.

At kickoff, the first attacking action is a Standard Pass.

## How To Play It

1. Start the action.
   Press `P` to begin a Standard Pass.

2. Choose a target hex.
   Click a hex up to the maximum allowed distance from the ball.

3. Confirm the target.
   If the target is valid, click the same target again to confirm the pass.

4. Resolve the pass.
   - If the pass is **safe**, the ball moves immediately.
   - If the pass is **dangerous**, interception attempts are resolved first.

## Maximum Distance

The normal Standard Pass range is **11 hexes**.

Distance is measured in **hex steps**, not by straight-line world distance.

## Target Validation

A Standard Pass target is valid only if all of the following are true:

- the target is within the allowed range
- the pass path is valid
- the thick path of the ball does **not** contain a defender-occupied hex

If the target is invalid, the pass cannot be confirmed.

## Thick Path

The Standard Pass uses a **thick path**, not a single thin line.

This matters because:

- a defender standing inside that path blocks the pass completely
- a defender whose **zone of influence** touches the path may be able to intercept

## Safe Pass Or Dangerous Pass

After choosing a valid target, the pass is classified as either:

- **Safe**
- **Dangerous**

A pass is **dangerous** if at least one defender has a valid interception chance on the path.

A pass is **safe** if no defender can affect the path.

## Interceptions

If the pass is dangerous, eligible defenders try to intercept one at a time.

### Who Gets A Chance

Each interceptor gets **exactly one** interception attempt.

A defender is considered an interceptor if at least one hex in that defender's zone of influence affects the relevant pass path.

### Interceptor Order

Interceptors are resolved in this order:

1. the defender whose **closest zone-of-influence hex** is nearest to the ball
2. if still tied, the defender who is closest to the ball
3. if still tied, the defender with the lower `Tackling`
4. if still tied, alphabetical order

All of these distances are measured in **hex steps**.

### Interception Result

- If an interceptor succeeds, the ball is pulled to that defender and the pass is stopped.
- If all interceptors fail, the pass continues to the original target.

## What Happens If The Pass Is Intercepted

If a defender intercepts:

- the ball moves to the interceptor
- possession changes
- that player becomes the new attacking player on the ball
- play continues from that new situation

The new attacking side can then choose from the actions currently available in that position, typically:

- `P` Standard Pass
- `L` Long Ball
- `M` Movement Phase
- `S` Snapshot, if a shot is available

## What Happens If The Pass Is Not Intercepted

If the pass reaches its target:

- the ball moves to the selected hex
- if the target hex contains an attacker, the pass is completed to a player
- if the target hex is empty, the ball is played into space

After resolution, the next available actions depend on:

- whether the attacking team still has possession
- whether the ball position allows a shot

Typical next actions are:

- `FTP` First-Time Pass
- `M` Movement Phase
- `S` Snapshot, if available

## Pass To Space

A Standard Pass may be played into an empty hex.

In that case:

- the pass is left **hanging**
- it can later be completed if an attacker picks it up during a Movement Phase

## Statistics

The Standard Pass affects stats as follows:

- once the target is confirmed, the passer receives a **Pass Attempt**
- defenders receive **Interception Attempt** only when they actually roll
- if the pass is intercepted, the passer does **not** get a completed pass
- if the pass reaches an attacker, the passer gets a **Pass Completed**
- if the ball is played into space, completion is delayed until the ball is picked up

## Quick Throw Relationship

A **Quick Throw** uses the same Ground Ball Pass logic with special restrictions:

- it is still range-limited
- intermediate path hexes do not care about defender occupation or defender ZOI
- the **target hex** still matters:
  - you cannot target a defender
  - defenders influencing the target hex may still get interception attempts

## Short Pass Note

A **Short Pass** is conceptually a Standard Pass with a shorter maximum range.

The intended short-pass range is **6 hexes**.

This will be formalized as its own explicit pass mode.
