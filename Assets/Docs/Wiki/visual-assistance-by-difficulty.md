# Visual Assistance By Difficulty

This page explains how difficulty changes the visual help offered to the player for:

- Ground Ball Pass
- First-Time Pass
- High Pass
- Long Ball

The core rules of the actions do not change. Difficulty changes how much the interface reveals before the player commits.

## Shared Highlight Language

The pass managers use a common visual vocabulary:

- Yellow: available or valid target options.
- Orange: the currently hovered or selected target.
- Blue: a safe visible pass path.
- Purple: a dangerous visible pass path, meaning defenders may affect the pass.

Difficulty 1 gives the most information before commitment. Difficulty 2 confirms target choice but hides more path/risk information. Difficulty 3 expects the player to choose without target highlighting.

## Ground Ball Pass

Ground Ball Pass uses a grounded path. That path can be safe, dangerous, or invalid.

### Difficulty 1

Difficulty 1 shows the full target preview.

When the player hovers a valid target:

- the hovered target is orange
- the pass path is painted
- a safe path is blue
- a dangerous path is purple
- the instruction box can tell the player how many interception attempts the pass would create

When the player clicks a valid target once:

- the selected target stays orange
- the selected path remains painted
- the player must click the same orange target again to confirm

If the player hovers another valid target before confirming, that new target and its path are previewed. If the player hovers back over the already selected target, the selected target's path stays visible.

### Difficulty 2

Difficulty 2 confirms the target, but hides the pass-path preview.

When the player hovers a valid target:

- the hovered target is orange
- the pass path is not painted
- interception information is not shown in the instruction box

When the player clicks a valid target once:

- the selected target stays orange
- the pass path is still not painted
- the instruction box asks for a second click on the orange target

The pass only commits when the same orange target is clicked again. Interception checks and dangerous-pass information are resolved after confirmation, not during target preview.

### Difficulty 3

Difficulty 3 gives no target preview.

The player clicks one valid target. If it is valid, the pass commits immediately. There is no second confirmation click, no target highlight map, and no pre-commit path or interception preview.

## First-Time Pass

First-Time Pass starts like a short Ground Ball Pass target choice, then adds one optional 1-hex attacker movement and one optional 1-hex defender movement before the final interception check is recalculated.

### Difficulty 1

Difficulty 1 shows the full target preview before the FTP target is confirmed.

When the player hovers a valid target:

- the hovered target is orange
- the pass path is painted
- a safe path is blue
- a dangerous path is purple
- the instruction box can tell the player how many current interception attempts exist before the 1-hex moves

When the player clicks a valid target once:

- the selected target stays orange
- the selected path remains painted
- the player must click the same orange target again to confirm

After target confirmation, the attacker movement phase highlights valid 1-hex destinations. The player may switch attackers before choosing a destination, or press `X` to forfeit the attacker move.

During the defender movement phase, valid 1-hex destinations are highlighted by interception effect:

- Yellow: moving there gives no FTP interception chance.
- Blue: moving there gives a mild interception chance through zone influence.
- Purple: moving there gives the increased interception chance from standing in the pass path.

The defender may switch defenders before choosing a destination, or press `X` to forfeit the defender move.

### Difficulty 2

Difficulty 2 confirms the target like Ground Ball Pass difficulty 2.

When the player hovers a valid target:

- the hovered target is orange
- the pass path is not painted
- interception information is not shown in the instruction box

When the player clicks a valid target once:

- the selected target stays orange
- the pass path is still not painted
- the player must click the same orange target again to confirm

After target confirmation, movement destinations are not painted as a full map.

For both attacker and defender movement:

- the player selects a token
- the player may switch to another valid token before moving
- hovering a valid destination turns that destination orange
- clicking a valid destination moves the selected token immediately
- pressing `X` forfeits the movement

### Difficulty 3

Difficulty 3 gives no target preview and no movement map.

The player clicks one valid FTP target. If it is valid, the target is accepted immediately.

After target confirmation:

- the player selects a single attacker, then clicks one reachable destination or presses `X`
- the player selects a single defender, then clicks one reachable destination or presses `X`
- reachable destinations are not highlighted
- token switching is not offered after a token is selected

## High Pass

High Pass targets are landing hexes that must satisfy the high-pass target rules. A selected target is later resolved through the high-pass accuracy and contest flow.

### Difficulty 1

Difficulty 1 paints all valid High Pass targets.

Before target selection:

- every valid target is shown
- the hovered target is orange
- a clicked target remains orange

After the first click, the player must click the same orange target again to confirm.

For normal High Passes, this means the player can scan the available landing options before committing. For target-related attacker movement steps, valid movement ranges are also highlighted when the current phase allows it.

### Difficulty 2

Difficulty 2 calculates valid High Pass targets, but does not paint all of them.

Before target selection:

- valid targets are known internally
- unselected valid targets are not painted yellow
- hovering a valid target turns it orange
- clicking a valid target keeps it orange

The player must click the same orange target again to confirm. The player may still move the pointer to another valid target and select that instead before confirmation.

### Difficulty 3

Difficulty 3 does not paint High Pass targets and does not use the two-click target confirmation.

The action is committed when High Pass is chosen. The player then clicks one valid target. If the clicked target is valid, the target is accepted immediately.

During later High Pass movement decisions, reachable hexes are not highlighted. The instruction box asks for a reachable hex rather than a highlighted hex. Defender-switching prompts are suppressed in difficulty 3 where the phase is meant to require a committed choice.

## Long Ball

Long Ball targets are distant landing hexes. The selected target then goes through the Long Ball accuracy flow.

### Difficulty 1

Difficulty 1 paints all valid Long Ball targets.

Before target selection:

- every valid target is shown
- the hovered target is orange
- a clicked target remains orange

The player must click the same orange target again to confirm. This gives a complete map of possible Long Ball destinations before commitment.

### Difficulty 2

Difficulty 2 calculates valid Long Ball targets, but does not paint all of them.

Before target selection:

- valid targets are known internally
- unselected valid targets are not painted yellow
- hovering a valid target turns it orange
- clicking a valid target keeps it orange

The player must click the same orange target again to confirm. The player can still switch to another valid target before confirming.

### Difficulty 3

Difficulty 3 does not paint Long Ball targets and does not use target confirmation.

The player clicks one valid target. If it is valid, the target is accepted immediately.

After the accuracy check, if the defending goalkeeper may rush, reachable rush hexes are not highlighted. The instruction box asks the player to click a reachable hex to rush there, or press `X` to skip the rush.

## Summary Table

| Action | Difficulty 1 | Difficulty 2 | Difficulty 3 |
| --- | --- | --- | --- |
| Ground Ball Pass | Shows target, path, and danger preview. Requires second click. | Shows orange target only. Hides path and interception info. Requires second click. | No preview. One valid click commits. |
| First-Time Pass | Shows target, path, and danger preview. Attacker move highlights valid destinations. Defender move colors destinations by interception effect. | Shows orange target only, then orange hover-only movement destinations. Requires target second click. | No target or movement highlights. One valid target click; one token and one destination per move phase. |
| High Pass | Paints all valid targets. Hover/selected target is orange. Requires second click. | Does not paint all targets. Hover/selected valid target is orange. Requires second click. | No target highlights. One valid click accepts target. |
| Long Ball | Paints all valid targets. Hover/selected target is orange. Requires second click. | Does not paint all targets. Hover/selected valid target is orange. Requires second click. | No target highlights. One valid click accepts target. |

## Design Intent

Difficulty 1 is explicit training mode: it shows the player where they can play and what the likely risk shape is.

Difficulty 2 keeps target confirmation and hover feedback, but removes the stronger predictive help. The player still knows when a target is valid, but does not get full path/risk assistance before committing.

Difficulty 3 is rules-first play. The game validates the player's choice, but does not reveal the available target map in advance.
