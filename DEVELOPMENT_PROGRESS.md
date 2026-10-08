# Development progress

## Iteration 1 — 2026-10-08 UTC

Baseline: `26d29dbbf41e39d811f1d876ae45ddf22d11e50d`, `master`.
This record describes real edits to this repository's player prototype.

- Fixed `PlayerMovement`: capture input in Update and issue movement only in
  FixedUpdate; recheck death on the physics tick; clear cached direction, velocity
  and walking state on death/disable; retain last facing; dispose input resources.
- Fixed `PlayerHealth`: reject nonpositive/nonfinite damage, clamp HP at zero,
  trigger death only on the alive-to-dead transition, permit a later explicit reset.
- Fixed `PlayerMana`: reject invalid consumption and preserve insufficient-balance
  behavior. No combat or forest-gate features were invented or claimed repaired.
- Migrated 23 locked registry URLs and the default registry to the international
  service, preserving dependency versions and graph. Native resolution was not run.
- Added actual-source offline C# checks, asset/meta/scene/LFS integrity checks,
  complete project packaging, source/skill/license records and eight portable skills.
- Added project validation, complete source ZIP packaging and hosted Artifact delivery.

Observed validation: 12 movement and 30 resource behavior tests passed; all 9
production C# files compiled against boundary doubles. Five PNGs decoded, all six
LFS SHA-256 OIDs matched (five game PNGs and a vendored skill reference), 37 Unity GUIDs were unique, 142 project references resolved,
and the enabled scene matched its .meta. Earlier movement checks failed against
the old source; resource checks changed from 18 pass/12 fail to 30 pass/0 fail.

Real Unity engine/Editor API reference compilation was additionally exercised for
the movement source, with Input System still substituted. No native Unity import,
PlayMode, physics, rendering, audio, full gameplay or platform build passed.

Six packaging boundary tests passed. During iteration 1, cloud GitHub REST access
returned proxy Forbidden; its required domain was added to the environment draft.

Published game/skills commit: d2783214e752ddf35c5cf22f6332a5a3fa8ed0eb.
Published workflow commit: 1e00d30ad2df4371ee96fa8f81b048ace9b8aaef.
Actual first GitHub Actions run: https://github.com/jinze909/2D-RPG-Project/actions/runs/37739823460.
The hosted project validation passed, but delivery boundary tests
failed and ZIP creation was correctly skipped. Follow-up isolates GitHub step
output transports in fixtures, removes fixture dependence on LFS local-transfer
smudging, and retains failure logs/annotations. Local checks pass; the corrected
hosted run must be observed before reporting successful CI or ZIP publication.

Next: verify corrected CI and source ZIP delivery, then close collision/input/respawn
gaps with appropriate native evidence.

Hosted CI follow-up 37740874645 again passed actual-source validation while an
LFS resource-transfer check failed. Investigation found that legitimate Git LFS
cache hardlinks were being rejected. This delivery failure was distinct from
the game-source validation and was corrected before the subsequent successful
baseline CI run recorded below.

First recoverable source ZIP for a81a6ce3617abdabc44706415d7059882c9928ec was
published on rpg-deliveries and downloaded over HTTPS: 377919 bytes, SHA-256
31b02d356245c6ac0015644a4508e49f7386cf2318a4e9944f42d0d4938c020a.
It contains 188 source files plus metadata and has a clean source worktree. A
final delivery will follow the validated workflow correction.

## Iteration 2 — 2026-10-08 (America/Los_Angeles)

Starting baseline: `abc74f5bef86edf06835a785a9e7205725e80e75`.
`git fetch origin master` refreshed the stale tracking ref and exact remote master
matched this HEAD; there were no unmerged iteration branches. Local work remained
isolated on `rpg/iteration-002-clearing`. All six real LFS objects were restored
after initializing the repository's local LFS filter; no pointers are deliverable.

Actual development:

- Actor-owned PlayerStats clones, full valid resource initialization, coherent
  health/mana ownership, retry/revive, invalid configuration guards and cleanup.
  Pause/retry discard cached motion. Prototype P damage is disabled in gameplay.
- WASD preserved; arrows, analog left stick and D-pad added with generated-wrapper
  JSON coherence. Hardware/package-native input tests remain pending.
- Replaced the old looping four-direction death spin with one existing front pose.
  Opposing death/revive requests are canceled; no new hero motion art was invented.
- Implemented CombatClearing with a collidable closed perimeter, three chasing /
  telegraphing sentinels, light strikes, mana burst, contact-window deduplication,
  cooldowns, immunity, mana regeneration, unique rewards, a fully sealed north
  objective region, gate unlock, beacon completion, defeat and full retry.
- Compact adaptive Canvas HUD, actionable status/goal/cooldown/mana feedback,
  stationary world warning/strike footprints, cached foot-Y actor depth and four
  original quiet synthesized cues with variation/mute. Blockout art and cue mix
  remain provisional; hero images/imports, original SampleScene and PPU30 preserved.
- Corrected defects found during implementation: wrongly typed scene bootstrap,
  enemy initiation outside its damage footprint, dead-player input admission,
  narrow HUD overlap, torso-aligned physics/contact, north camera head clipping
  and pause-disable cleanup. New loops were reviewed against actual code/assets.

Main files: new `Assets/Scenes/CombatClearing.unity`, five `Gameplay/Clearing*.cs`
with meta; Player/Health/Mana/Stats/Movement/Animations; input asset/wrapper;
Dead.anim; build entry; actual-source/contract/API runners and regression fixtures.
Five root records, a scene design/acceptance contract and persistent handoff updated.
Older rpg-by-ai source was read at b0aae3d329c5b0b200b30b1457d0f692257b9f06 for
reference; no foreign game source/assets/save schema was silently imported.

Recorded iteration 2 game/project validation: 14 movement, 40
resource/lifecycle, 32 pure combat rules, 9 input/animation serialized contracts,
15 scene/geometry/raster contracts and 6 distribution tests passed. These are
historical implementation results; follow-up changes require fresh verification.
All 14 production C# files compile against boundary doubles and separately against
86 real installed Unity 2022.3.53f1 engine/Editor/.NET/uGUI references. The latter
still substitutes Input System. Five PNGs decode, six LFS hashes match, 44 Unity
GUIDs are unique, 145 project references resolve, two enabled scenes resolve and
all 65 files in eight licensed skill bundles match recorded hashes.

Regression evidence: prior lifecycle source failed four new checks; original
input/death data failed four contracts. Scene tests failed on the wrong bootstrap
component before its fix. Memory mutations detect removed partition, excessive
enemy attack range, missing late input order, torso foot anchor and cropped camera.
The handoff's identical completed working copy validated 100/100, eight referenced
files present and no secrets; persistent version is under this iteration directory.

Native limit: a fresh empty-project license probe failed with `No valid Unity
Editor license found`. No native import/compile, EditMode/PlayMode, collision
simulation, rendering, audio audition, playthrough or platform build passed.
API compilation/offline doubles/floodfill do not establish those results. This
high-risk candidate will be delivered through an isolated draft PR, not merged
into stable master without required evidence. See COMBAT_CLEARING acceptance list.

Prior baseline hosted CI was verified: run 37741402547 validated, packaged and
uploaded its source artifact successfully. Current candidate commit, PR/CI/ZIP
receipts are recorded after publication and in ARCHIVE-INFO.json.

Implementation was normally pushed as
`f203cc1a0e4f6de50f5a8aaa73039298823cbec5`. Actual draft PR:
https://github.com/jinze909/2D-RPG-Project/pull/1, targeting master and not merged.
The final delivery-record commit follows this implementation; its exact source
SHA is stored by the packager in ARCHIVE-INFO.json. The directly downloadable ZIP
and current hosted CI receipts are included in the session's delivered manifest.

## Project guidance and CI maintenance — 2026-10-08

- Simplified root instructions, portable skill authorization, project documentation
  and handoff records; removed unused infrastructure and its dedicated evidence.
- Preserved game code, assets, Unity version, real resource files and ordinary
  source-validation/ZIP CI.
- Fresh retained checks: 14 movement, 40 resource/lifecycle, 32 pure combat rules,
  9 input/animation contracts, 15 scene/geometry and 6 distribution checks: 116
  passed. Native Unity acceptance remains pending as previously recorded.
- All eight skill bundles still retain license/provenance metadata with updated
  local adaptation hashes. PR #1 remains a draft candidate, not a merged game.

## Iteration 3 — 2026-10-08: clearing presentation polish

Starting candidate: `de0614399bb878ae182f90f81074359824e5057b` on PR #1.
Fresh master: `2e8c154f219256454ad981d6ebf11fdea5865d7d`. Explicitly fetched the
candidate ref (the cloud's original fetch configuration covered master only),
then selected `rpg/iteration-003-clearing-polish` without changing either baseline.
PR #1 remains open/draft/unmerged; the user retains its merge decision.

Cleanup regression: candidate 116 retained checks and master 48 checks passed.
Exact hosted runs were independently read: master [37855895204](https://github.com/jinze909/2D-RPG-Project/actions/runs/37855895204)
and candidate [37855964779](https://github.com/jinze909/2D-RPG-Project/actions/runs/37855964779)
both succeeded through validation, ZIP packaging and artifact upload. Removed
checks were not presented as still present. No cleanup regression was found.

Selected one complete improvement: combat feedback and coherent clearing art,
with HUD safety, rather than unrelated professions/inventory/save systems.

- Added a shared 15-color palette and bounded deterministic managed raster
  builders. Ground, stepping paths, stone walls, moss/stone sentinels, plinth,
  crystal and rune now share PPU30/Point/no-mipmap art with explicit FullRect.
  Native textures/sprites are generated once, cached and disposed; original ten
  hero PNG/import files retain their exact candidate-baseline hashes.
- Preserved fixed warning boundaries and added phase-progress fill plus an X
  during the real contact window. Pooled hit/kill pulses survive hidden enemies
  briefly, freeze on pause and clear on death/retry/disable. Distinct contact/kill
  cues suppress repeated/multi-target spam; later kills can upgrade once.
- Fixed confirmed HUD defects: unavailable beacon instructions on pause/death
  and temporary feedback extending into the lower central combat region.
  Feedback now replaces help in its edge slot and terminal states hide stale text.
- Rules, damage/cooldowns, mana, rewards, scene/collider dimensions, actor anchors,
  hero animations and gate completion conditions were not rewritten.

Final local verification: 14 movement + 40 resource/lifecycle + 32 pure combat
rules + 9 input/animation contracts + 15 scene/geometry + 14 presentation behavior
+ 9 managed-raster behavior + 1 original-hero hash check + 6 distribution = 140
passing checks. Asset/GUID/build/package/LFS/skill integrity checks additionally
passed. All 16 production C# files compile against 86 real installed Unity
2022.3.53f1 engine/Editor/.NET/uGUI references; Input System is still substituted.

Regression evidence: the same presentation fixture on retained de06143 yields
2 passes / 12 failures, including reproduced HUD/action and contact-audio gaps.
New FX expectations also fail there because those features did not exist;
these are not described as twelve preexisting bugs. Raster mutation checks catch
removed size guards and colors outside the exact palette. An inspected contact
sheet contains exact production C# pixels, not a native screenshot.

A fresh editor probe again fails with `No valid Unity Editor license found`.
No native import/compile, EditMode/PlayMode, real physics, hardware input,
render/layout/font acceptance, sound audition, playthrough or platform build was
executed. Original gait/foot drift and new art/audio quality require that review.
Publication receipts, exact final SHA, hosted CI and verified ZIP follow in the
iteration directory / delivery manifest after actual publication.

Implementation normally pushed as `4ca71ef52d4918256ddf8e824a23911c0c37de4d`.
Actual stacked draft PR #2: https://github.com/jinze909/2D-RPG-Project/pull/2.
Exact implementation CI run 37858757979 completed successfully, with all
validation/packaging/upload steps and Artifact 11584913917 verified. See
docs/iterations/2026-10-08-iteration-003/publication.md for PR #1 readiness and
the final source/CI/ZIP receipt location. Neither draft has been merged to master.
