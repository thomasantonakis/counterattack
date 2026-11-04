# Room crowd creation brief

**Superseded direction:** The user rejected the small-ground concept because the spectators were too large and the stands uneven. Use `RoomStadiumVisualsPrompt.md` and `Visuals/RoomCrowdConcept-v2-Stadium.png` for the current professional-stadium direction. The small terraces and spectator sizes below are historical only.

Status: proposal, pending atmosphere and animation preferences. User screenshot reviewed and live scene settings inspected through Pipeline. No scene or gameplay changes have been made.

## First concept

Generated with the built-in image generation tool from the user's Room screenshot. Saved at `Docs/Visuals/RoomCrowdConcept-v1.png`. The concept establishes concrete aprons, a larger far terrace, compact goal-end terraces, and separated low foreground sections around the substitutes. It is an environment reference, not a pixel-exact gameplay capture: the generated image altered some token numbers, labels, and pitch details. Preserve the actual Unity board when implementing. Realistic 3D character meshes still need to be sourced or authored separately.

Generation prompt: Edit the supplied screenshot into a realistic Unity environment concept while preserving the green hex pitch, camera composition, tokens, goal outlines and red labels. Replace the blue surroundings with a compact football ground: 4–5 rows on the far terrace, smaller goal-end terraces, low separated foreground sections, clear technical bays around both substitute groups, concrete walkways, galvanized railings, varied realistic spectators in muted everyday clothes with occasional blue and white scarves, soft daylight and restrained weathering. Keep the playing surface unobstructed, with open corners and no roofs, advertising or new text.

User screenshot reviewed: the pitch is surrounded by a broad blue surface, with substitute tokens in two groups below the near touchline. Preserve a clear technical/substitution area there. Concentrate the initial crowd section on the far touchline; extend around the goal ends and use a lower, interrupted near-side terrace. The user reported restarting Pipeline, but a subsequent CLI check still returned `STATUS_PIPELINE_LOAD_PENDING` and an unreachable server.

## Findings from the saved project

- Scene: `Assets/Scenes/Room.unity`; pitch visuals live under `PitchBoardVisuals`, with `HexGrid` at the origin.
- Playing rectangle is approximately 27.5 units along X and 21.65 along Z. Goals extend beyond the X ends. These are board units, not metres.
- FINAL THIRD labels sit at Z = +/-11. They need an unobstructed margin.
- Four large blue out-of-bounds quads include colliders and `OutOfBoundsClickBlocker`. Their input function must survive any visual redesign.
- The perspective camera has a saved vertical field of view of 23 degrees. `CameraController` supplies a 45-degree overview, two overhead presets, a closer oblique preset, and orbit/zoom controls (pitch 20–90 degrees; distance 12–90 units).
- No obvious spectator character or stadium asset appeared in the existing model/prefab inventory. Realistic spectator meshes are a separate asset requirement; image generation can provide visual concepts and textures, but not a finished rigged 3D crowd.
- Pipeline connection subsequently restored on port 7800. Live inspection confirms Room is loaded, saved without pending changes, Play Mode stopped, and compilation idle. The active render pipeline is Built-in. The edit-mode camera is at (0, 0, -10), rotated (45, 0, 0), with FOV 23; gameplay framing is set by CameraController on Start. Use the user's gameplay screenshot as the visual reference rather than assuming the edit-mode camera represents the gameplay view.

## Proposed direction

A compact football ground around the existing board: low concrete terraces, simple railings, varied spectators in everyday clothes, muted materials, scattered scarves in team colours, and clear access gaps at corners. Keep the near edge low enough to preserve the view of the board from the default camera. Use three-dimensional people so overhead and orbit views retain believable volume.

Provisional layout: terrace fronts near Z = +/-14 and X = +/-18, leaving clear space around labels and goals. Begin with roughly 3–5 rows and adjust after viewing all camera presets. Establish person scale visually against the pitch and tokens rather than treating a Unity unit as a real metre. Determine density and performance limits with a representative section before filling the perimeter.

## Prompt to execute after direction is agreed

Create realistic crowd surroundings for Counter Attack's existing `Assets/Scenes/Room.unity`, using the findings above and the user's latest preferences.

First inspect the live Room scene through the Unity CLI, capture the current view, and confirm camera framing, render pipeline, existing materials, pitch bounds, and scene state. Respect all project AGENTS.md rules. Never start Play Mode. If Pipeline is unavailable, report the missing connection and continue only work that does not depend on live Editor access.

Art direction: an intimate football ground with believable spectators watching the pitch. Use human proportions, varied silhouettes, natural clothing folds and colours, different ages and skin tones, occasional scarves, small groups and gaps, and irregular poses. Keep the pitch and tokens visually dominant. Match the scene's lighting; favour subdued concrete, metal railings, and restrained colour accents. Preserve the current pitch geometry, markings, tokens, gameplay labels, UI, and controls.

Build a representative terrace section first, with 3–5 low rows and a clear walkway between the pitch and spectators. Place it beyond the touchline labels and goal footprint. Evaluate its scale and sightlines in the default overview, overhead views, close oblique view, and low orbit angles before extending it to all four sides. Use low terraces and open corners to keep the pitch readable. Address any occlusion caused by foreground spectators.

Start this representative section along the far touchline visible in the user's screenshot. Reserve a clear technical area for the two substitute-token groups along the near side, with terrace breaks and access paths. Retain the substitute tokens' visibility and clickability. Give the surrounding blue surface a coherent stadium-apron treatment while preserving its existing input role.

Use proper 3D spectator meshes for the nearby and middle rows. Reuse shared meshes and materials with variations in clothes, scale, pose, and placement. Source suitable licensed assets already available to the project or clearly identify any missing mesh/rig requirement before committing to realism. Avoid treating simple capsule figures as the finished realistic result. Image generation may supply a concept reference and supporting texture assets. Any distant image-based crowd representation must also work from the overhead camera or switch to suitable geometry.

If movement is selected, add restrained idle, head-turn and occasional clap loops with varied timing. Keep animation parameters editable and establish a sensible cost with a representative section. Add goal reactions only if requested. Author every terrace, railing and spectator placement as editable scene objects or prefabs before Play Mode; runtime code may animate existing objects.

Create a separate `StadiumCrowd` root with clearly named side groups and reusable section prefabs. Expose density, occupancy, colour palette and motion settings. Use decorative layers and avoid crowd colliders that intercept gameplay input. Preserve the out-of-bounds quads' click-blocking behavior; any replacement surface must avoid depth overlap and remain compatible with that behavior. Do not modify `Canvas/MatchStatsUI/Stats` layout.

Deliver the editable scene/prefab assets, supporting materials/textures, a short explanation of new dependencies, and comparison captures from the relevant camera views where available. Use Unity CLI for Editor operations and compilation checks; the user runs Play Mode and tests. Describe measured performance only after it has actually been measured.

## Optional image concept prompt

Using an actual screenshot of Counter Attack's Room pitch as the composition reference, illustrate the proposed surroundings. Preserve the exact board shape, green hex pitch, white markings, flat player tokens, goals and gameplay labels. Add a compact realistic football ground outside the board: low concrete terraces on all four sides, three to five rows of varied spectators in everyday clothing, occasional team scarves, metal barriers, perimeter walkways and open corner entrances. Show natural crowd spacing and human proportions, soft daylight matching the reference, subdued surfaces, and subtle clothing colour variation. Keep the pitch as the visual focus and the foreground terrace low. Match the supplied camera perspective exactly. This is an art-direction concept for subsequent editable Unity assets.
