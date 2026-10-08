# Unity project deliveries

Source commit: [a81a6ce3617abdabc44706415d7059882c9928ec](https://github.com/jinze909/2D-RPG-Project/commit/a81a6ce3617abdabc44706415d7059882c9928ec).
Unity: international 2022.3.53f1.

[Download the full source project ZIP](https://github.com/jinze909/2D-RPG-Project/raw/refs/heads/rpg-deliveries/2D-RPG-Project-a81a6ce3617abdabc44706415d7059882c9928ec.zip).
SHA-256: `31b02d356245c6ac0015644a4508e49f7386cf2318a4e9944f42d0d4938c020a`.

188 source files plus archive metadata; actual LFS resources and Unity .meta files included. Library/Temp/Logs and credentials excluded. Archive CRC and SHA-256 verified. The packaged source worktree was clean.

42 actual-source C# offline behavior checks and asset/scene/skill integrity passed. 27 automation and 6 distribution boundary tests passed locally. These checks do not constitute native Unity import, physics, rendering, PlayMode or a platform build.

Paid scheduled AI development is paused at the user's request. See master/docs/AUTOMATION.md. Hosted CI proof is recorded separately after observing its real result.

## Iteration 2 — Combat Clearing candidate

Source: [090507d5648ad6e18a0bc92bb7c0ec53c02d8457](https://github.com/jinze909/2D-RPG-Project/commit/090507d5648ad6e18a0bc92bb7c0ec53c02d8457).
[Draft PR #1](https://github.com/jinze909/2D-RPG-Project/pull/1), not merged to master.

[Download complete candidate project ZIP](https://github.com/jinze909/2D-RPG-Project/raw/refs/heads/rpg-deliveries/2D-RPG-Project-iteration-002-090507d.zip).
SHA-256: `a8f2594016a3ac6aee8954ba1081311c15c999fd34953baa07a529d89a1149aa`.

218 source files plus archive metadata; original real LFS bitmaps, both scenes,
all meta, source, packages, settings, portable skills and records included.
Clean source worktree, ZIP CRC verified, caches/credentials/LFS pointers excluded.
143 local offline checks passed; 14 production files also compiled against real
Unity engine/Editor/uGUI API references (Input System substituted).
Native editor import/physics/rendering/PlayMode/audio/playthrough remain unverified.
New map/enemies are provisional code-native blockout, not a finished full RPG.

[Current source CI run](https://github.com/jinze909/2D-RPG-Project/actions/runs/37852926710);
see iteration-002-delivery.json for observed receipt. Paid AI automation stays paused.

Current source hosted CI completed successfully: actual project checks, boundary tests, ZIP packaging and upload all passed. Native Unity acceptance remains pending.
