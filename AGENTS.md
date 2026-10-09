# RPG development instructions

The user authorizes game development, commits, normal pushes to master,
and source-project deliveries. Preserve accepted designs, existing user work,
working systems, save compatibility, and the original blonde hero identity.
Use international Unity **2022.3.53f1** and pixel art; do not use China Unity or
China package services. Preserve the specified editor version.

## Current architecture and clearing contract

- `SampleScene` retains the original player prototype. `CombatClearing` is the
  new, isolated candidate build entry: three sentinels, combat, a sealed beacon
  region, completion/retry and compact Canvas HUD. Its native acceptance is pending.
- `Player` clones the authoring `PlayerStats` in Awake. Health and mana resolve
  that same actor-owned snapshot. New session/retry fills valid maxima; the template
  is never a save file. No persistent save, inventory or class system exists yet.
- `Gameplay/ClearingRules` is engine-independent timing/admission/reward logic;
  `ClearingRuntime` owns scene positions, contacts, input and the resource bridge.
  `ClearingVisuals`, `ClearingHud` and `ClearingAudio` own presentation. Runtime
  art uses cached PPU30/Point code-native rasters and a shared 15-color palette.
  New stone/moss sentinels, walls and beacon are an integrated art candidate;
  native visual acceptance and synthesized cue audition remain pending.
- Preserve attack tokens, stationary warning footprints, once-only rewards,
  HP-death synchronization, pause cleanup and full gate-to-boundary coverage.
  Player attacks use independent world effects and retain the existing walk cycle.
- Read `docs/game/COMBAT_CLEARING.md` for controls and native acceptance steps.
  Run `tools/validate_project.py` (movement/resources/input/animation/combat/scene,
  actual presentation behavior and managed-raster checks) and the distribution
  suite. When installed, also use
  `tools/compile_unity_api.py`: real engine/uGUI signatures, with Input System
  explicitly reported as a substitute if its compiled assembly is absent.
- Native-unverified iteration 2 stays on `rpg/iteration-002-clearing` / draft PR.
  Iteration 3 is stacked on that candidate at `rpg/iteration-003-clearing-polish`.
  Fetch both before selecting work; do not recreate them. The current user has
  explicitly authorized autonomous review, ordinary PR merges and delivery,
  superseding historical user-only merge restrictions. Assess real CI and risk;
  keep native-unverified high-risk changes isolated until adequate evidence exists.
- Warning progress/X motifs remain inside the original saved contact outline.
  Pooled hit/kill feedback uses simulation time and resets on retry/disable.
  HUD messages share the help edge slot and hide unavailable actions on pause/death.
  Preserve these contracts and original hero hashes; do not change hit geometry
  merely to align with decoration or claim native rendering from offline rasters.

## Start every iteration

1. Read this file, README.md, DEVELOPMENT_PROGRESS.md, KNOWN_ISSUES.md,
   NEXT_ITERATION.md, SKILLS_USAGE.md, and docs/game/DESIGN_BASELINE.md completely.
2. Verify the actual checkout, branch, full HEAD, remote master, dirty files and
   unmerged rpg/iteration-* branches. Fetch current history before selecting work.
   Never overwrite uncommitted user files, reset their history, or force push.
3. This task is already isolated. Do not create a Git worktree unless the user
   explicitly requests one. Risky work may use an ordinary isolated branch.
4. Enumerate every .agents/skills/*/SKILL.md. Read all applicable instructions in
   full and the references needed for this iteration. These files are vendored
   instructions, not evidence of a connected Unity MCP or image service.
5. Recheck actual capabilities: compiler, editor/license, native tests, image
   tools, audio tools, service connections and credentials by name/presence only.
   Never print environment values, authentication files, private keys or licenses.
6. Save and normally push a checkpoint after each independently verified stage.
   Record unfinished work; resume that checkpoint after a quota interruption.

## Apply the skills to real work

- session-handoff: verify and preserve continuity. Working handoffs in .claude/
  are ignored; validate and copy a useful final handoff into docs/iterations/ when
  it should persist on GitHub. Root iteration records always persist.
- systematic-debugging: reproduce a real failure, trace its cause, fix that
  cause, and rerun a meaningful regression check.
- verification-before-completion: report fresh checks, counts and failures;
  distinguish static/offline compilation from native play and visual acceptance.
- game-design: choose a coherent improvement to the player's actual experience;
  finish one loop rather than add unrelated incomplete systems.
- game-art: preserve the established identity, pixel scale, palette and frame
  anchor; inspect animation timing and real import settings before changing art.
- game-audio: apply feedback/mix/variation principles when audio work is relevant;
  absent audio assets are an identified gap, not an implemented sound system.
- unity-mcp-orchestrator: verify APIs and editor/test workflow compatibility.
  Live operation requires an actual compatible connected editor and server.
- imagegen: use available built-in image tools when a real bitmap task calls for
  them. GitHub Actions does not inherit ChatGPT image tools. Do not invent calls,
  invent unavailable capabilities, or redesign the existing hero merely to use a skill.

Record exact skill usage, affected files, evidence and unavailable capabilities in
SKILLS_USAGE.md. All eight skills must be rediscovered for each fresh task; only
actually applicable skills need execute a modification in that round. Source and
license records are in docs/skills-source-manifest.json.

## Select and finish a valuable change

Prioritize run/compile failures, serious gameplay bugs, movement and animation
continuity, combat/level problems, art/UI, coherent content, then maintenance.
Check actual files before claiming a feature exists. The initial master contained
only SampleScene and a player prototype: no attacks, forest, quest gates, enemies,
HUD or full Starfall game. A separate repository's handoff is design background,
not evidence those systems are implemented here. Do not replace this game with an
unrelated project or call reconstructed guesses a recovered checkpoint.

Investigate unfamiliar APIs using official sources compatible with Unity 2022.3;
inspect external code before using it. Do not execute unreviewed downloaded scripts.
Use meaningful tests when changing behavior. Existing actual-source checks are:

```bash
python3 tools/run_player_checks.py --project-root .
python3 tools/run_resource_checks.py --project-root .
python3 tools/run_presentation_checks.py --project-root .
python3 tools/run_art_checks.py --project-root .
python3 tools/validate_project.py --root . --output /tmp/rpg-project-report.json
python3 -m unittest discover -s tests -p 'test_distribution.py'
```

The offline C# runners use system Mono, or discover a retained Unity installation.
RPG_MONO and RPG_CSC/RPG_MCS can select tools. Boundary doubles do not reproduce
native physics, the real Input System, Animator rendering or Game View. Native
import, tests and gameplay remain required when the environment supports them.
Never reduce checks, fabricate counts or treat a zero-test run as success.

## Delivery and CI

Maintain all four root records with changes, real results, baseline/commit evidence,
remaining issues and a next priority. Update KNOWN_ISSUES rather than repeat a
completed fix merely to produce a commit. Complete and save tested work within the
available time; isolate incomplete or high-risk changes with their evidence.

The ordinary GitHub Actions workflow `rpg-project-validation.yml` checks pushed
code or a manual run, then packages the validated project and uploads its ZIP and
reports. Check the actual run and Artifact before claiming hosted success. Standard
runners do not establish licensed native import, playback or visual acceptance.
Keep failed or native-unverified high-risk changes on `rpg/iteration-*` branches.

Package with the trusted tools/package_unity_project.py. Include real LFS objects,
Assets, Packages, ProjectSettings and .meta; exclude caches and credentials.
Provide actual commit/artifact URLs after publication, not planned URLs presented
as successful uploads. If blocked, preserve local commits/ZIP and report the exact
access or credential requirement. Do not claim this long-term game is finished.
