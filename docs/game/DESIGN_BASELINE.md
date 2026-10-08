# Design baseline and source boundary

The active target is jinze909/2D-RPG-Project master, international Unity
2022.3.53f1, 2D pixel art. The user now authorizes autonomous quality improvements,
coherent new content and refactoring. Preserve explicit accepted core design and
working functionality; quality, playability and stability outweigh feature count.

The original playable blonde Viola sprite is the identity reference. It is already
present as five real LFS PNG resources. Existing textures use Point filtering,
no compression and PPU 30. Do not replace the protagonist, change pixel scale or
modify all poses just to use a generator. Current gameplay uses a Rigidbody2D and
Animator in SampleScene, not the separate Starfall GameCore/OnGUI pipeline.

Previously supplied Starfall Frontier context described three equipment/profession
variants of that same blonde person, nine regions plus an inn, contact-timed combat,
save compatibility and a polish-focused direction. jinze909/rpg-by-ai contains an
older full source snapshot; its final 2026-10-07 rebuild/decision ledger checkpoint
is still not received. This master does not yet contain those game systems. Treat
that context as evidence to reconcile when source is available, not an invitation
to recreate missing assets and call them recovered.

Carry forward explicit rejection decisions: ranger north attacks with incorrect
grip or bent arrows were rejected; no accepted complete north attack set exists.
Future four-direction art must use stable identity, canvas, root/foot anchor,
handedness and a reviewed complete motion cycle. Internal registration counts or
offline checks never imply user acceptance or Unity gameplay acceptance.

Iteration 1 focuses on existing control/death/resource reliability. Subsequent
iterations should form playable exploration, interaction, combat and reward loops
in tested increments. Respect actual repository contents, preserve save/session
contracts when introduced, and isolate native-unverified scene or media changes.
