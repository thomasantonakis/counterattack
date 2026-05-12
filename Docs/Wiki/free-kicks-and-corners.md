# Free Kicks And Corners

This page documents how **Free Kicks** and **Corner Kicks** are currently handled by `FreeKickManager`.

It is written as implementation documentation: it describes both the intended rules flow and the current manager behavior.

## Shared Set-Piece Flow

Free Kicks and Corner Kicks both use the same manager and the same broad preparation structure:

1. The set-piece starts.
2. A potential taker is moved to the set-piece spot.
3. Both goalkeepers may be adjusted at specific points.
4. Attack and defense alternate setup moves.
5. A final set-piece taker is selected or auto-selected.
6. The attacking team chooses the execution action.

The selected final taker becomes `LastTokenToTouchTheBallOnPurpose`.

Before assigning that taker, the manager clears the previous purposeful-touch chain. This means the old `PreviousTokenToTouchTheBallOnPurpose` is cleared and the chosen taker becomes the only purposeful touch owner for the set piece.

## Potential Takers

A potential taker is any attacking token on the ball hex or touching the ball hex.

The manager recalculates potential takers by checking all hexes within range `1` of the ball:

- if the hex contains an attacking token, that token is a potential taker
- duplicates are ignored
- defenders are ignored
- null or invalid ball/grid state returns no takers

At the end of setup:

- if there is exactly one potential taker, they are auto-selected
- if there is more than one, the attack must click one of them
- if there are none, the manager logs an error and waits for taker selection

## Initial Free-Kick Taker Move

For a Free Kick, the clicked attacking token is moved to a free hex touching the fouled dribbler/ball.

The destination is chosen by `GetClosestAvailableHexToBall()`:

1. get the six neighboring hexes of the ball hex
2. keep only hexes that are:
   - not defense occupied
   - not attack occupied
   - not out of bounds
3. order the remaining hexes by hex-step distance to the pitch center `(0, 0, 0)`
4. choose the first result

So, if more than one adjacent free hex exists, the current tie-breaker is simply **closest to the center of the pitch**.

There is now an explicit TODO for the extreme edge case where every hex touching the fouled dribbler is occupied and no destination can be found.

## Initial Corner-Kick Taker Move

For a Corner Kick, the clicked attacking token is moved directly to the corner spot passed into `StartFreeKickPreparation(cornerKickSpot)`.

The manager currently assumes this spot can be occupied by the taker.

There is already an optimistic TODO in the code because corner spots may eventually need collision handling if:

- an attacker is already on the corner spot
- a defender is already on the corner spot
- the legal inbound adjacent choices are limited by the board edge

The same rare-surrounded-spot problem from Free Kicks is therefore more relevant for Corners.

## Goalkeeper Adjustment Timing

Both set pieces begin with two goalkeeper adjustment opportunities before the outfield setup sequence:

1. `FreeKickAttGK`
   The attacking goalkeeper may be adjusted first.

2. `FreeKickDefGK1`
   The defending goalkeeper may then be adjusted before the main setup moves.

After all normal alternating setup moves are complete:

3. `FreeKickDefGK2`
   The defending goalkeeper gets one more adjustment just before the kick sequence is finalized.

For Corner Kicks only, this second defending-GK adjustment is followed by the advanced 3-hex movement pair:

1. one attacker may move up to 3 hexes
2. one defender may move up to 3 hexes

## Free-Kick Setup Move Sequence

Free Kicks use this setup sequence:

1. Attack moves 2 players.
2. Defense moves 2 players.
3. Attack moves 2 players.
4. Defense moves 2 players.
5. Attack moves 3 players.
6. Defense moves 2 players.

The same token may be moved again in a later phase.

The manager does not lock a token after it has moved. This matches the rule that a moved token can move again in a later setup phase.

## Corner-Kick Setup Move Sequence

Corner Kicks use the same structure, except the final attacking setup phase is 2 players instead of 3:

1. Attack moves 2 players.
2. Defense moves 2 players.
3. Attack moves 2 players.
4. Defense moves 2 players.
5. Attack moves 2 players.
6. Defense moves 2 players.

Then the advanced corner movement runs:

1. Attack moves 1 player up to 3 hexes.
2. Defense moves 1 player up to 3 hexes.

This advanced movement is handled through the same movement-highlighting mechanism used elsewhere for 3-hex movement.

## Ball Must Not Be Left Alone

During setup, at least one attacker must remain on or touching the ball.

The manager protects this by recalculating potential takers after attacking moves. If there is exactly one potential taker and the attack tries to move that token during an attacking setup phase, the move is rejected.

This prevents the attack from leaving the ball with no attacking token on or around it.

The instruction box now shows the current live set-piece check:

- which attackers are on or touching the ball
- whether there are no attackers on or touching the ball

## Defenders Within Two Hexes

At set-piece start, the manager records defenders within 2 hexes of the ball as defenders that should be moved away.

During defensive setup phases:

- a defender cannot be moved to a destination within 2 hexes of the ball
- if the number of defenders still needing to move is greater than or equal to the remaining defender moves, the defense must select one of those defenders
- the defense cannot forfeit a setup phase if doing so would leave too few remaining defender moves to move all required defenders
- after a required defender is moved, they are removed from the required-move list

The instruction box now also shows a live check of defenders currently within 2 hexes of the ball. This is intentionally recalculated from board state, so it remains visible even if the original required-move list and the current board state drift apart.

## Final Taker Selection

After setup is complete, potential takers are recalculated.

If there is one:

- they are auto-selected
- `PreviousTokenToTouchTheBallOnPurpose` is cleared
- `LastTokenToTouchTheBallOnPurpose` is set to that taker
- execution options are shown

If there are multiple:

- the instruction box asks the attack to select the taker from attackers on or touching the ball
- clicking a valid potential taker commits that taker

## Free-Kick Execution Options

After a Free Kick setup completes, the manager enables:

- `P` Standard Pass
- `C` High Pass
- `L` Long Ball
- `S` Shot, if the placed ball hex is in shooting range

During `FreeKickExecution`, `FreeKickManager` owns the action keys so the instruction box does not also show the normal Long Ball or Shot prompts.

For difficulty `< 3`, pressing `P`, `C`, `L`, or available `S` selects that option but does not immediately close the Free Kick chooser. The attack can press another valid option key to change the selected execution before the action is finally committed. Pass options commit when their normal target confirmation commits. Shot uses a Free Kick-level pre-commit: first `S` previews/selects the shot, and pressing `S` again commits it.

For difficulty `3`, a valid execution key auto-commits the selected action immediately.

## Corner-Kick Execution Options

After a Corner Kick setup completes, the manager enables:

- `P` Short Standard Pass
- `C` Corner High Pass

Corner Short Pass is a Standard Pass with an imposed distance of `6`.

Corner High Pass is handled by `HighPassManager` with `isCornerKick = true`. `MatchManager.TriggerHighPass(isCornerKick: true)` sets the corner flag before activation so the corner-specific target rules are active immediately. The current validation requires the target to be attack occupied and either:

- within normal high-pass distance, or
- in the defending penalty box in the same final third as the corner

Long Ball and Shot are not enabled from Corner Kick execution.

## Free-Kick Shot Flow

The current `FreeKickManager` recognizes `S` during Free Kick execution. On difficulty `< 3`, first `S` enters a shot pre-commit state and second `S` commits, matching the normal Shot Manager commit behavior. On difficulty `3`, `S` commits immediately.

The committed action uses a dedicated Free Kick shot branch in `ShotManager`:

1. Highlight valid `CanShootTo` target hexes from the placed ball hex.
2. Confirm the target.
3. Roll for the shooter.
4. Apply the outside-box shooting penalty when relevant.
5. If shot power is `>= 9`, skip all outfield defender blocks.
6. Once the ball enters the box, offer the defending goalkeeper box move.
7. Determine the save hex and saving penalty.
8. Roll the defending goalkeeper save.
9. Compare shot power and saving power.

If shot power is `< 9`:

1. resolve outfield defender block attempts in normal shot order
2. defenders on the shot path can block on `5+`
3. defenders using ZOI can block on `6+`
4. defenders may also block with `Tackling + roll >= 10`
5. a successful block starts a Loose Ball from that defender
6. if no defender blocks, continue to goalkeeper save resolution

A natural shooter roll of `1` means the shot is off target, but outfield block attempts still happen first. If no outfield block succeeds, the shot resolves as off target.

Final shot resolution should follow normal shot outcomes:

- shot power greater than saving power: goal by the set-piece taker, with no assist
- shot power equal to saving power: goalkeeper moves to the save hex, possible GK push, then Loose Ball
- shot power less than saving power: save made, then Handling test

## Known Implementation Gaps

The main known gaps are:

- Corner spot occupancy needs a real resolution method.
- The rare case where no adjacent free hex exists for the Free-Kick taker still needs a rule decision.
- Corner High Pass should keep scenario coverage proving the corner flag is set before activation.
- There should be scenario coverage for final taker auto-selection, defender-clearance enforcement, and the instruction-box obligation text.
