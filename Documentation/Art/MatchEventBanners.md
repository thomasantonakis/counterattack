# Match event banner artwork

Original AI-generated PNG illustrations created with the built-in ImageGen tool. The supplied screenshots were subject references; no stock images or watermarked source files are included. These are generated assets, not an externally sourced CC0 collection.

## Asset map

Assets live in `Assets/Art/UI/MatchEvents/`.

| File | Occasion | Suggested caption |
| --- | --- | --- |
| yellow-card.png | First caution | YELLOW CARD |
| second-yellow.png | Second caution and dismissal | SECOND YELLOW |
| red-card.png | Straight dismissal | RED CARD |
| injury.png | First injury | INJURY |
| second-injury.png | Second injury | SECOND INJURY |
| substitution.png | Substitution being made | SUBSTITUTION |
| corner-kick.png | Corner awarded | CORNER KICK |
| goal-kick.png | Goal kick awarded | GOAL KICK |
| throw-in.png | Throw-in awarded | THROW-IN |
| foul.png | Foul ruled | FOUL |
| stoppage-time.png | Added time decided | ADDED TIME |
| half-time.png | End of half | HALF-TIME |
| extra-time.png | Awaiting extra time | EXTRA TIME |
| penalties.png | Going to a shootout | PENALTY SHOOTOUT |

## Presentation specification

Use a designer-editable square panel below the scoreboard, with a large image and a short caption. Default event duration: four seconds, configurable between three and five seconds. A short fade/scale entrance can animate the PNG without requiring animated sprite frames. Queue simultaneous announcements so an injury does not replace a card before it can be read. Caption text should supply variable information such as the player name or actual added minutes, rather than baking it into the artwork.

The Room scene now includes `MainCanvas/ScoreBoardManager/MatchEventBanner`, a 320 × 320 square with a four-second FIFO queue and fade/scale animation. `MatchEventBannerController` exposes all sprite assignments and the display duration in the Inspector. Select its `Preview Event` field and use the component context menu's `Preview selected event in Edit Mode` action to inspect alternative PNGs without starting gameplay. Use `Hide preview / clear queue` afterward.

The panel listens to live presentation notifications from card, injury, substitution, restart, foul, stoppage-action and half/tiebreaker entry points. It does not replay historical save events or defer gameplay. Stoppage detail reports added **actions**, matching the game's existing clock rules. PNG selections are provisional.

## Verification checklist

- [x] All 14 event PNGs are present.
- [x] Each PNG has real transparent pixels and visible opaque content (alpha range 0–255).
- [x] Each PNG imports as a single Unity UI sprite with mipmaps disabled.
- [ ] The HTML preview loads each icon at banner size.
- [x] Rendered the authored scoreboard and banner against a dark green background; PNG selections remain provisional.

## Generation prompts

Exact shared style and per-icon subjects are retained in `MatchEventBannerPrompts.json` beside this document.
