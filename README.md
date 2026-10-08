# Unity project deliveries

Source commit: [a81a6ce3617abdabc44706415d7059882c9928ec](https://github.com/jinze909/2D-RPG-Project/commit/a81a6ce3617abdabc44706415d7059882c9928ec).
Unity: international 2022.3.53f1.

[Download the full source project ZIP](https://github.com/jinze909/2D-RPG-Project/raw/refs/heads/rpg-deliveries/2D-RPG-Project-a81a6ce3617abdabc44706415d7059882c9928ec.zip).
SHA-256: `31b02d356245c6ac0015644a4508e49f7386cf2318a4e9944f42d0d4938c020a`.

188 source files plus archive metadata; actual LFS resources and Unity .meta files included. Library/Temp/Logs and credentials excluded. Archive CRC and SHA-256 verified. The packaged source worktree was clean.

42 actual-source C# offline behavior checks and asset/scene/skill integrity passed. Six distribution boundary tests passed locally. These checks do not constitute native Unity import, physics, rendering, PlayMode or a platform build.

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
see iteration-002-delivery.json for observed receipt.

Current source hosted CI completed successfully: actual project checks, boundary tests, ZIP packaging and upload all passed. Native Unity acceptance remains pending.

## Current candidate project

Source: [de0614399bb878ae182f90f81074359824e5057b](https://github.com/jinze909/2D-RPG-Project/commit/de0614399bb878ae182f90f81074359824e5057b).
[Download current complete project ZIP](2D-RPG-Project-iteration-002-de06143.zip).
SHA-256: `a57c2c807b17b2ad171a88311507197f5b27892ef6db9ce238ef41fa747d718c`.

211 source files plus archive metadata; clean source checkout, real LFS resources,
all Unity project directories and metadata included. CRC and SHA-256 verified.
116 retained offline game/resource/scene/distribution checks passed.
Ordinary source validation and ZIP CI retained. Native Unity acceptance remains pending.
Draft PR #1 remains unmerged; master contains only documentation/infrastructure cleanup.

## Iteration 3 — Clearing pixel art and combat feedback

Source: [1bedd3296a31398db62dc27d07c466c125bd896e](https://github.com/jinze909/2D-RPG-Project/commit/1bedd3296a31398db62dc27d07c466c125bd896e).
[Stacked draft PR #2](https://github.com/jinze909/2D-RPG-Project/pull/2), not merged; PR #1 and master are unchanged.

[Download the complete source ZIP](2D-RPG-Project-iteration-003-1bedd32.zip).
SHA-256: `18394e7efad8a69899d99bce090012954836881e9ee13386dee8ad4473f67e6e`.

228 source files plus ARCHIVE-INFO; clean source checkout, both scenes, original real LFS images, all meta, scripts, packages, settings, licensed skills and records included. CRC and all source bytes checked; caches/credentials/LFS pointers excluded.

140 local offline checks and 16-source real Unity API-reference compilation passed (Input System substituted). Native import, physics, rendering, HUD/font layout, EditMode/PlayMode, audio and playthrough remain unverified.

[Exact-source offline pixel preview](iteration-003-offline-pixel-preview.png); not a Unity screenshot.
See [final source/CI/download receipt](iteration-003-delivery.json) for observed delivery evidence.

Final exact-source [CI run 37858940651](https://github.com/jinze909/2D-RPG-Project/actions/runs/37858940651) is green; all validation, packaging and upload steps succeeded.
The published immutable ZIP was downloaded over HTTPS: bytes, SHA-256, CRC, metadata and all 228 source files matched. See the receipt for direct immutable ZIP/checksum URLs and Artifact retention.
