# Sophisticated Draft Persona

The Sophisticated draft persona is intended to behave like a manager building a CounterAttack squad, not like a raw attribute maximizer. It drafts with a 4-2-3-1 match shape in mind, then rearranges the roster so jersey slots 1-11 represent the projected starters and the remaining players become bench options.

## Core Mentality

Sophisticated values role fit, balance, and matchup coverage. It is willing to pass on a generally strong player if the current squad already has that strength and has a more urgent structural weakness elsewhere.

The default target shape is:

- 1 goalkeeper
- 2 fullbacks in slots 2 and 3
- 2 centerbacks in slots 4 and 5
- 2 central or defensive midfielders in slots 6 and 8
- 2 wingers in slots 7 and 11
- 1 striker in slot 9
- 1 attacking midfielder in slot 10

## Outfield Role Principles

Fullbacks are defenders first. They need pace to cover wide runners and tackling to avoid leaving the side exposed. High Pass and dribbling help them build play, but a fast attacker with poor tackling should not be parked at fullback just because they are useful in possession.

Centerbacks protect the middle. Tackling and heading are heavily valued, with pace still useful but not enough to move a weak defender into the center. Slow, strong tacklers get extra credit as central anchors, while slot 5 slightly prefers the faster centerback so the pair has a covering defender.

Central midfielders are all-rounders. They need enough high pass, pace, tackling, dribbling, heading, and resilience to connect the team and protect the defense. The persona accepts different midfield profiles, from holding defenders to mobile ball winners to balanced playmakers.

Wingers spend pace. If several attackers can dribble and pass, the faster ones should usually play wide because they can latch onto long balls, stay clear of tackles, break into the box, and create snapshot chances.

The attacking midfielder is the creator. Dribbling and shooting matter most, but when the squad has multiple good attacking dribblers, slot 10 can absorb the slower one so the team keeps pace on the wings.

The striker is the finisher. Shooting is the main threat, heading creates a second route to goal, and pace helps poachers create more shots. Tackling is not important for this role.

## Pace, Resilience, and Mismatch Rules

Pace is not treated as a generic bonus. It is especially valuable for wingers, strikers, fullbacks, and other players who can create or prevent breakaway situations.

Low resilience is position-sensitive. Fragile players are penalized more when they are placed in roles that are likely to absorb fouls and contact, especially attacking midfield and central midfield. A fragile shooting striker can still be valuable if the role lets them act clinically.

The persona penalizes obvious role mismatches. For example, a classic pace-and-shooting striker with poor tackling should be pushed toward winger or striker, not fullback.

## Draft Pick Evaluation

On each Regular draft decision, the persona evaluates all visible outfield cards by projecting the roster after adding each candidate. It then rebuilds the best 4-2-3-1 starter assignment and scores the candidate using:

- role score
- starter impact
- starter gap or bench value
- urgency from the remaining draft state
- matchup-driven adjustments where applicable

The final pick is the candidate with the strongest projected effect on the squad, not always the highest standalone player.

The logs explain:

- available players and attributes
- current team status
- projected role
- role score factors
- starter impact
- gap or bench value
- urgency
- final score
- why the selected player beat the alternatives

## Automatic Rearrangement

After every Sophisticated Regular draft pick, the roster is rearranged automatically. The current best outfield XI is placed into slots 2-11 and the remaining outfielders are placed on the bench.

The arrangement is dynamic. A later great player can push an earlier starter to the bench if the optimized XI improves.

The arrangement logs list each selected starter, their role score, slot-specific alternatives, and the bench players with their best role scores.

## Goalkeeper Handling Review

The initial goalkeeper deal stays unchanged: the higher-saving goalkeeper starts in slot 1 by default.

After the draft is complete, Sophisticated performs a goalkeeper review using the final starter context. This is designed for the case where a low-handling goalkeeper may concede more corners, making aerial defense more important.

For each team, it compares:

- our top four starter Heading average
- the opponent top four starter Heading average
- the current starting goalkeeper's Saving and Handling
- the bench goalkeeper's Saving and Handling

If the opponent top four starter headers are at least 2.0 higher than ours, the persona considers swapping in the bench goalkeeper. It swaps only when the bench goalkeeper improves Handling and does not lose 2 or more Saving.

Example mentality: Doxakis with Saving 4 and Handling 6 can start over Walker with Saving 5 and Handling 1 if the team failed to match the opponent's heading strength. The same swap should not happen if the bench goalkeeper's Saving drop is too large.

The GK review logs the heading comparison, both goalkeeper profiles, the Saving drop, the Handling gain, and the final keep-or-swap decision.

## Current Limitations

Sophisticated does not formally declare a formation to the match engine. It arranges the roster to express a practical 4-2-3-1 assumption.

It does not yet model exact opponent winger placement, man-marking assignments, or in-match substitution plans. Those can be added later once the roster shape and role scoring are stable.
