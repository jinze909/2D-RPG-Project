# Character action frames for Unity 2022.3

Local project recipe added to the downloaded OpenAI imagegen skill. This recipe guides the available image tool; it is not a separate trained animation model and does not claim any generated output has been validated yet.

The user has now confirmed pixel art. For frame identity, anchor alignment and cycle continuity, also read [pixel-animation-production.md](pixel-animation-production.md). It records researched controls, precise editor operations, tool limitations and acceptance gates; it does not treat a prompt, common seed or repeated endpoint frame as a guarantee.

Repository adaptation, 2026-10-08: read the newest current repository handoff and design records when present, including the uploaded RPG-Codex-Handoff-2026-10-07.md source if retained. The historical Starfall Frontier design uses GameCore / FrontierRuntime / OnGUI, while the original 2D-RPG-Project checkpoint uses Player / Animator. Inspect the current code before integrating any asset; do not assume either architecture has replaced the other. Use actual actor registration, masks and bounds for an OnGUI production path, or preserve existing slices/clips/controllers for an Animator path. The original playable blonde hero remains the identity reference; rejected ranger north grip/arrow candidates remain rejected. Historical missing-checkpoint or LFS status must be rechecked at the current HEAD. Current game-development authorization permits scoped work, but no report alone proves art acceptance or Unity playback.

## Establish the asset contract

Use the user's design and existing character reference. Inspect local references with view_image first. Only ask for missing information that prevents useful output; infer provisional production choices from the current project's sprites and record them as provisional.

Record:

- Character reference and invariants: proportions, face, hair, costume, palette, equipment, handedness and silhouette.
- Perspective: top-down, side-view or isometric; 4 or 8 directions if actually requested.
- Required actions, frame count per action, looping policy and intended playback rate.
- Target cell dimensions, padding, foot/pivot alignment, row order and output naming.
- Transparency and whether the output is a prototype draft or a final candidate.

Common small prototype defaults may be proposed: idle 4 frames, walk 6 or 8, attack 4–6, hurt 2–3, death 6–8. They are not the user's confirmed design. Do not generate every action or every direction without a task scope that calls for them.

## Generate with the existing skill

Use the built-in image generation/editing tool by default. This built-in mode does not require a user API key when the tool is actually available. GitHub Actions must check availability rather than assume it. Do not substitute API scripts, Python painting/editing, or unconnected third-party services. Follow the current tool's reference and edit rules.

Start from the character reference, then produce one action and direction at a time when that makes consistency easier to inspect. If a complete sheet is requested, state an explicit row/column order, equal cells, padding and a fixed ground baseline. For transparent frames set transparent_background=true. Treat pixel dimensions and exact grids as output targets to verify, not as guaranteed tool arguments.

Example prompt skeleton:

```text
Asset type: transparent 2D RPG character animation frames for Unity
Primary request: [action], [direction], [frame count] frames in temporal order
Input images: [reference image] is the character identity and art-style reference
Style: match the supplied reference; [pixel art / hand-drawn as actually requested]
Layout: [columns] equal cells; consistent padding; fixed foot baseline and scale
Motion beats: [explicit pose sequence suited to the action]
Keep unchanged: face, proportions, palette, costume, equipment and handedness
Background: transparent; no scenery, labels, grid lines or text
Avoid: repeated poses, extra limbs, cropped weapons, scale drift and motion blur
```

Useful motion beats to adapt, not blindly apply:

- Idle: subtle breathing; stable foot contact; controlled secondary motion.
- Walk: contact, weight transfer, passing and opposite contact; check a complete loop.
- Attack: anticipation, wind-up, strike, follow-through and recovery; keep weapon orientation consistent.
- Hurt: impact and recovery; distinguish it from an attack pose.
- Death: loss of balance, fall and resting pose; normally non-looping.

## Inspect before calling it usable

View the output and check identity, frame order, pose differences, silhouette, direction, palette, limb/equipment continuity, transparency, clipping, padding, cell boundaries and baseline/scale consistency. Check the transition from the last frame to the first for looping actions. A still contact sheet cannot prove timing or a smooth loop; preview the animation in an available editor before claiming those qualities.

When a frame is wrong, change that frame or one invariant at a time while retaining the same reference. State unresolved defects plainly. Do not claim pixel-exact output, ready-to-ship animation or game integration based only on a generated PNG.

## Save and integrate when authorized

Copy selected generated outputs into the workspace using a new filename; preserve existing character assets and .meta GUID references. A generation task does not automatically authorize replacing a game's current hero.

For pixel-art imports, propose Sprite (2D and UI), appropriate Multiple/Single mode, Point filtering, compression off, a consistent Pixels Per Unit and a foot-aligned pivot. For hand-drawn assets choose sampling appropriate to that style. Use the actual Unity 2022.3 importer and installed package APIs.

If live Unity MCP tools are available, inspect existing slices/clips/controllers before editing. Verify tool schemas rather than assuming manage_sprite exists. For a verified Animator path, slice only the intended new sheet and create scoped clips/transitions; for an OnGUI path, follow its real atlas, mask and bounds contract instead. If editor licensing or the MCP connection is missing, deliver the image and explicit import metadata, and report integration as unexecuted.

Record action/direction, frame count/order, intended FPS, looping, cell/padding, PPU and pivot in a sidecar document. Record which dimensions and behavior were actually checked. Never write credentials or API tokens into asset metadata.
