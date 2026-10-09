# Unity project context

<!-- unity-onboarding:generated:start -->

Analyzed on 2026-10-08 (America/Los_Angeles), source
`53bd47eb63f5b512c1981605341d64eee8d2cfe7`, project root
`/workspace/2D-RPG-Project`. This is the existing candidate, not a new RPG.

## Confirmed environment and structure

- International Unity 2022.3.53f1 (ProjectVersion.txt). Built-in rendering:
  no SRP package or custom pipeline in manifest/QualitySettings.
- Both input backends are enabled (`activeInputHandler: 2`). Input System 1.11.2
  owns movement; ClearingRuntime uses legacy keyboard polling for combat.
- Cinemachine 2.10.3, uGUI 1.0.0, TMP 3.0.7, Test Framework 1.1.33 are configured
  registry dependencies, not evidence of successful native package import.
- Assets/Scripts/Player owns actor resources, movement and Animator control;
  Assets/Scripts/Gameplay owns clearing rules, runtime and presentation.
  Assets/Actions and Assets/Animations hold original hero/input resources.
- No first-party asmdef/asmref exists: runtime scripts use Unity's default
  assembly. Gameplay uses `Rpg.Gameplay`; original Player types are global.
  Private serialized fields, brace-on-next-line C#, short lifecycle comments.
- CombatClearing is the first enabled build scene; SampleScene remains second.
  Runtime Start builds the bounded clearing and calls ResetRun. No multi-scene
  progression, persistent save, inventory or profession system is implemented.

## Architecture and validation

Player clones the authoring PlayerStats; health/mana use that same actor-owned
snapshot. ClearingRun owns timing, action tokens, sentinel HP, run-local rewards
and objective gates; it never duplicates player HP/MP. ClearingRuntime bridges
input/positions/contacts; Visuals, PixelArt/Palette, Hud and Audio present it.
Pause freezes simulation time. Hero identity, PPU30, Point filtering and original
PNG/meta files are preservation contracts.

`tools/validate_project.py` compiles/executes actual C# with recording boundaries
and checks serialized input, geometry, assets, GUID/meta, LFS and skill provenance.
Distribution tests and `package_unity_project.py` produce recoverable source ZIPs.
CI is ordinary push/manual offline validation plus ZIP upload. There are no native
EditMode/PlayMode test assemblies in the source. A player-build target is unknown.

## Actual tooling and unknowns

Editor executable and Mono/Roslyn exist under
`/workspace/.cloud-tools/unity-onboarding/2022.3.53f1/Editor`. Version/compiler
probes and 16-source/86-reference Unity API compilation passed; Input System is
explicitly substituted. One isolated native probe exited 1 with
`No valid Unity Editor license found` before project import.

Unity and Unity Essentials skills are readable. No Unity CLI executable, live
Editor MCP tools, bridge package/configuration or interactive desktop was found.
CLI Pipeline requires Unity 6+ and must not be installed into this 2022.3 project.
Native import/compile, physics, hardware input, animation/rendering/HUD, audio
audition, playthrough and platform builds are blocked or unrun, not passed.

Sources inspected: AGENTS.md, all root records, the full iterations 2/3 handoff
chain, DESIGN_BASELINE/COMBAT_CLEARING, manifest/lock, ProjectVersion,
ProjectSettings/QualitySettings, EditorBuildSettings, Player and Clearing source,
offline fixtures, validation/package tools and the push workflow. Onboarding did
not modify scenes, resources, packages or settings.

<!-- unity-onboarding:generated:end -->

## Iteration 5 update (2026-10-08 local)

From verified fe8b1f7, implemented ClearingProgress (immutable bounded ledger),
ClearingProgressCodec (strict v1/checksum/future header protection), and desktop
FileClearingProgressStore (lock/expected snapshot/flushed atomic replacement).
ClearingRuntime banks only completed attempts, handles terminal input and applies
saved bonuses to actor clones only on reset. Hud displays bank/ranks/result costs
and truthful failure reasons; Audio reuses its accepted reward cue. No art, scene,
package or settings changes. 219 project + 6 distribution checks and 19 sources
against 86 actual references pass; InputSystem/native limits above still apply.

## Resumed delivery policy and integration (2026-10-09)

Latest user makes native acceptance optional for ordinary merging. PR4 merged
(a20cb5a) and PR1 merged into master(cb2d6fc) after exact green candidate CI and
cumulative review. Prior draft/master statements are historical. The actual
feature tree equals tested50d42ce; native/tooling limitations still apply.

## Iteration 6 tactical encounter update (2026-10-09 local)

From master d8c7048, immutable SentinelTactics supplies three distinct roles and
contact snapshots; Runtime explicitly assigns them at startup, controls spacing/
admission and uses the same footprint for warnings and hits. Visuals/Hud add cached
role silhouettes, constant-stroke warnings and edge counters. Existing progression
and original hero/scene/package/settings remain. Real API compilation:20sources,
86references, InputSystem substitute. Live native/editor evidence remains absent.
