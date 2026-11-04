# Room professional stadium brief

## Implemented in Unity

`Assets/Scenes/Room.unity` now contains a saved `StadiumCrowd` instance of `Assets/Environment/RoomStadium/RoomStadium.prefab`.

Latest club away allocation: corrected to one section counterclockwise from the original six-block allocation, in both tiers. With reference index 10, these are zero-based sections 7–9, hierarchy sections 08–10 upper and lower (six sections). The reference block itself uses the normal home mix. The user's current away clothing mix is 80/15/5. The bowl is uniformly scaled to 85%, with the apron opening preserved, and bench players move from z=-14 to z=-12. See `StadiumCrowdTuning.md` for the current matrix and configurable reference/count.

- Two continuous tiers of 14 rows each, connected rounded corners and an elevated concourse.
- 19,324 static seated spectators after aisle realignment, with volumetric bodies, heads, limbs and seats; no perspective backdrop or billboarding. Simplified geometry is designed to read at the tactical camera's distance.
- 32 editable crowd sections per tier, shared vertex-colour material, concrete structure and handrails. Every visible object is authored before Play Mode.
- Dark perimeter apron; existing out-of-bounds material recoloured while retaining its colliders and scripts. No colliders on stadium geometry; Ignore Raycast layer.
- Builder: `Assets/Editor/RoomStadiumEditorTools.cs`, menu **CounterAttack > Room > Build Stadium**. Requires Room open in Edit Mode. Rebuilding replaces the generated prefab/meshes, so preserve custom edits in a separate variant before rebuilding.
- Current mesh totals after aisle realignment: 133 renderers, 3,629,072 vertices and 5,001,568 triangles. This first version has no LOD, crowd animation, roof, or measured frame-rate target. Generated serialized assets occupy approximately 251 MB; optimization remains a separate iteration. Renderer count is not a measured draw-call count, and serialized disk size is not GPU memory usage.
- Aisles corrected first at the user's request: fixed coordinates along straight stands, perpendicular to touchlines/goal lines; fixed radial angles through corners. Each side has six straight sections and two corner sections. Use **CounterAttack > Room > Realign Stadium Aisles** to update existing mesh assets without replacing the scene/prefab instance or saving unrelated scene edits.
- Compilation completed without errors or warnings; shader reports no errors; no missing mesh references. Inspected perspective and overhead Edit Mode renders and a capture of the user's running Game view. Codex did not start Play Mode or run tests.
- Actual Unity captures: `Visuals/RoomStadium-Unity-Perspective.png`, `Visuals/RoomStadium-Unity-Overhead.png`, and `Visuals/RoomStadium-Unity-Gameplay.png`. The existing pitch hex grid is initialized during Play Mode, so the Edit Mode captures show pitch markings without the green hex surface.

The generated concept images below remain art references, not screenshots of the implementation.

## Implemented supporter colour rules

The confirmed rules are implemented in `StadiumCrowdController`; see [Stadium crowd tuning](StadiumCrowdTuning.md) for the current matrix, section numbering, edit locations and previews. Club ordinary stands use reversed HOME colours for their 30% group. International ends use 60% own main jersey, 30% own alternate jersey and 10% neutral; long stands use 30% home main, 18% home alternate, 20% away main, 12% away alternate and 20% neutral. Germany clothing is white/black or green/white; Netherlands clothing is orange/white or white/orange.

The aisle update now has constant widths and uses native Mesh setters rather than CopySerialized, which had left the rendered buffers stale. Actual rendered previews confirmed both updated aisles and colours. Population after the fixed-width layout is 19,005; geometry is 3,570,612 vertices and 4,919,620 triangles across 133 renderers.

### Historical clarification notes (superseded)

Colour implementation awaits clarification. All stochastic clothing assignments should be made once at match startup, remain fixed during play/camera motion, and use the existing authored spectators. Physical west/east sections remain fixed when teams change ends.

Club matches: every seat uses home primary. West goal-end sections: 95% home primary top/secondary trousers, 5% reversed. Designate one upper east goal-end section as the away allocation: 60% away primary/secondary, 30% reversed, 10% neutral. Remaining stands: 50% home primary/secondary, 30% reversed AWAY colours as literally requested (user asked whether this was intended to be reversed HOME), 20% neutral.

International matches: one random stadium seat colour; use nations' main kit identities independently of the match's selected token kits. Entire west end home supporters, entire east end away supporters. Long-side stands split 60% home supporters / 40% away supporters. Clarify remaining goal-end 50%, and whether the 30% clothing group in long-side stands means reversed main colours or alternate kits; the remaining long-side 20% was unspecified and neutral has been proposed.

Resolved project palettes (primary/secondary): Fiorentina `502854/E6E6E6`; Real Madrid `E6E6E6/1D285F`; Germany main `E6E6E6/F6C64D`; Germany alternate `4CA76F/1A1A18`; Netherlands main `EF782E/2B2718`; Netherlands alternate `E6E6E6/EF782E`. The existing instruction-palette resolver selects Germany's gold stripe as secondary; user was asked whether stadium clothing should instead use white/black.

## Approved direction and reference

The user requests a professional stadium resembling their reference photo, with much smaller spectators and consistent stands. A hybrid of 3D structures and crowd images is acceptable. The earlier small-ground concept is rejected.

Current concept: `Visuals/RoomCrowdConcept-v3-ContinuousTiers.png`, revised with the built-in image generation tool from v2. It is an environment concept, not a gameplay screenshot. Generated token numbers, markings and framing are approximate; the real Unity board must remain intact.

The user requires a continuous lower seating bowl around the entire pitch, with an upper bowl above and set back from it everywhere. Both connect around the corners. A consistent elevated concourse separates them. The far lower tier must exist rather than having the upper stand descend directly to pitch level. Actual Unity modules must enforce identical tier profiles and row counts; the image is an approximate visual reference.

V3 edit prompt: Correct the stadium architecture into two separate continuous seating bowls. Insert the missing far-side lower tier between pitch apron and elevated concourse. Every stadium cross-section must read apron, lower seating tier, elevated concourse, upper seating tier, roof. Maintain consistent tier profiles through corners and a continuous horizontal division, retaining the tiny crowd scale, concrete, steel roof, daylight, and existing board. Permit foreground cropping for pitch visibility. No annotations.

## Implementation prompt

Create a coherent professional stadium around the existing Counter Attack Room pitch. Use the project Unity CLI workflow and respect AGENTS.md. Inspect live state before edits. Never start Play Mode. Author all visible geometry and placements as editable scene objects or prefab assets.

Preserve the existing hex pitch, tokens, markings, goal target geometry, labels, camera controls, substitute positions and gameplay input. The pitch is approximately 27.5 units long along X. Treat that length as a nominal 105-metre visual scale reference: approximately 0.262 Unity units per metre. An adult 1.7–1.8 metres tall is therefore approximately 0.45–0.47 units tall. The board's width is abstracted; do not reshape the gameplay grid to match a real pitch's aspect ratio. Do not scale spectators from the enlarged playing tokens.

Build a continuous stadium bowl with consistent tier profiles on all four sides, connected chamfered or curved corners, regular aisles and a concourse separating lower and upper tiers. Start with a representative stand module, establish consistent seat spacing and row rise, then repeat it around the pitch. Use roughly 25–35 rows as the concept starting point, adapting depth and height to preserve camera sightlines. The bowl may extend outside the default camera frame. Maintain a clear pitch perimeter and a technical apron for both substitute groups.

Use lightweight 3D geometry for terraces, walls, aisles, railings and roof structures. Populate distant seating with repeated crowd texture atlases on stepped or layered geometry, with several variants to suppress obvious repetition. Use small 3D spectator/seat forms for nearby rows and overhead visibility where needed. This is a crowd mass at realistic stadium scale, not a collection of large foreground characters. A single flat stadium photograph is insufficient for the existing orbit and overhead cameras. Evaluate layered textures from those angles; introduce geometry or view-dependent representations if they flatten out.

Create dark steel canopy/truss modules and restrained floodlighting only after the bowl and crowd scale read correctly. Roofs and foreground structures must not block the playing surface in tactical views; provide editable visibility controls or a suitable cutaway behavior if necessary. Keep materials compatible with the live Built-in render pipeline.

Use shared materials and reusable meshes. Establish performance with a representative section and the target camera views; do not claim measured performance without measurements. Optional crowd motion should be subtle and asynchronous. Every visible object must exist in edit mode, with runtime code limited to animation or visibility behavior.

Keep decorative objects from intercepting gameplay raycasts. Preserve the existing out-of-bounds click blockers while replacing their blue visual appearance with the stadium apron. Preserve designer-owned UI layout. Use a separate StadiumCrowd root and reusable prefabs grouped by side/tier, with consistent scales and clear naming.

## Image generation prompt used for the revision

Edit the original Room screenshot; use the supplied stadium photo only for architecture, density, atmosphere and scale. Preserve the green hex pitch, token layout, flat goal outlines, red touchline strips and substitute groups. Replace blue surroundings with a professional stadium bowl. Treat the pitch length as 105 metres and adults as approximately 1/60 of that length. Show thousands of tiny spectators in consistent rows, a continuous rectangular bowl with connected corners, 25–35 rows, regular stair aisles, a middle concourse, narrow pitch walkways, dark steel upper roof structures and modest floodlights. Keep the elevated tactical camera and clear view of the board and substitutes. Use realistic muted materials, daylight and soft shadows. Avoid oversized people, isolated mismatched bleachers, large empty concrete plazas, advertising, new text and logos. This is a concept for hybrid 3D geometry and textured crowds.
