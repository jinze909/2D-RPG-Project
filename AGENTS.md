# RPG development instructions

## Iteration 10 reviewed ordinary master integration

[PR #9](https://github.com/jinze909/2D-RPG-Project/pull/9) normally merged reviewed
head 7481f5b at a61fada. Exact implementation CI38031431351/job114153017263 passed
all 12 steps; nonexpired artifact11661974248 exists. Root fetched and verified both
merge parents (ninth ba4cd1d + reviewed7481f5b), exact merge-tree equality and clean
local fast-forward to the actual merge. No force push; all recovery history remains.
Merged-master CI38031515174/job114153259081 also passes all12 steps; nonexpired artifact11661899444 exists.
The Guardian HUD/notice/marks/cue fixes and 18 additional permanent regressions are
implemented: 385 project/distribution groups, API26/86 with Input substitute and
independent123presentation/19Boss/API review pass. Five static integrity envelopes
also pass; chained handoff validates100/100. No remaining blocker found in scope.
Preserve108HP/2damage/all timing/contact/costs/supplies/30coin-v1/hero contracts.
Final exact source CI, complete ZIP/CRC/member SHA/source bytes/LFS/immutable HTTPS
and receipt identities resolve externally in iteration-010-delivery.json plus
final-handoff/validation on rpg-deliveries after source publication. Verify that
actual receipt first; if complete do not redo tenth code or packaging, and if
missing finish publication before new development. No archive success is assumed
by this source record. Native Editor tests/play and real player playtests are unrun.

## Evidence and player-value rules — current user instruction, 2026-10-09 PDT

- Separate static source/asset analysis, offline actual-C# logic with recording
  boundaries, controlled simulation, Unity native tests and real player playtests.
  Passing offline checks never establishes difficulty, animation smoothness,
  visual clarity or game feel. Label unrun experience as **尚未进行 Unity 原生验收**.
- Check Boss state/contact/warning/phase/death/pause/retry/system integration first.
  Tune HP, damage or speed only when evidence supports that change; deterministic
  optimal-hit estimates are not a player difficulty measurement.
- Historical animation/UI/forest/archery/quest reports are experience references,
  not confirmed current defects. Inspect actual source/Scene/Prefab/Animator/assets
  before listing a defect. Full forest, archery and full quests are absent here;
  never claim their bugs fixed. Preserve regressions for already-fixed code.
- Do not repeat license/Editor/MCP probes when the known environment is unchanged.
  Plugin availability, Skill readability, CLI availability, Editor connectivity,
  native validation and player acceptance are distinct facts.
- Finish one valuable player loop. If no evidenced defect can be addressed, choose
  coherent exploration/interaction/content from actual current code; do not modify
  sound systems or stats merely to increase iteration count.
- Current KNOWN_ISSUES/NEXT_ITERATION categories must distinguish implemented and
  code-tested, native-validated, native-unrun experience, confirmed-unfixed defects,
  and absent future features. Current top sections supersede all historical lists.
  Ordinary reviewed/tested CI-green PR/master merges are authorized; native checks
  are optional. Never force push or overwrite uncommitted work.

## Historical iteration 9 reviewed master integration

PR8 normally merged reviewed/testedb4046999e1ec766f9550bbbc667b8e554e9b9228 at
134e09dc75fef799aa0f3ed4e7dabe1470c86364. ExactCI38015650991/job114105188884 passed
all12steps,artifact11656011193 exists; merge parents/tree match expected history.
367checks/26-source86-real-referenceAPIcompile/Inputsub/independentreview and
100/100handoff are recorded. Boss entry/combat/feedback/gate/reward loop is complete.
Final source/CI/archive/download identities resolve in the external ninth receipt
on rpg-deliveries after source publication. Inspect that actual receipt before
new development; if complete, do not repeat ninth implementation or packaging.
Native tests/play/physics/device/render/audio/balance/build remain optional/unrun.
All prior gameplay, original hero and v1ledger/recovery history remain preserved.

## Historical iteration 9 integration checkpoint — superseded

Eighth delivery is complete at source84177cd / receipt879693b. Ninth foundation
f8cebaa is safely pushed; exact CI38015115905 passed all12steps. Live Runtime now
requires three guards -> central altar E ->108HP two-pattern/two-phase Guardian
-> north seal -> beacon E -> existing30coin saved growth. Read the complete
BOSS_ENCOUNTER contract and ninth handoff. Default isolated ClearingRun callers
retain the legacy three-guard API; live Start explicitly opts in.
Fresh361project +6distribution =367checks pass;26production sources compile
against86real Unity2022.3engine/Editor/uGUI references, Input System substituted.
Independent actual-method/pure/scene review found no blocking issue. Cached Boss
art/telegraphs, HP/phase edge HUD, fifth actual-loss slot, accepted cue reuse and
pause/disable/retry/resource/reward lifecycle are integrated. PR/finalCI/ZIP/HTTPS
publication remains to be completed after the implementation checkpoint.
Preserve v1/30coins, player-first contacts, reserves and original blonde hero.

## Current architecture and clearing contract

- `SampleScene` retains the original player prototype. `CombatClearing` is the
  new, isolated candidate build entry: three sentinels, a post-guard Guardian, combat, a sealed beacon
  region, completion/retry and compact Canvas HUD. Its native acceptance is pending.
- `Player` clones the authoring `PlayerStats` in Awake. Health and mana resolve
  that same actor-owned snapshot. New session/retry fills valid maxima; the template
  is never a save file. Iteration 5 adds engine-independent clearing progression
  and versioned local storage, result-screen purchases and next-run actor bonuses.
  Inventory, equipment and classes remain absent.
- Iteration 6 wires Warden sweep, stationary Lancer lane and Seer spacing/sigil
  to the actual clearing. SentinelTactics captures immutable role geometry shared
  by contact and warning. Read docs/game/SENTINEL_TACTICS.md; preserve original
  caster LOS, fixed targets, phase movement locks and snapshot cleanup on reset.
- Iteration 7 DamageReadout formats accepted HP loss; ClearingDamageNumbers owns
  five fixed labels and cached glyphs (Boss extends slot4). Runtime feeds accepted before/after deltas.
  Preserve simulation-time pause, fixed hit origins, kill visibility and cleanup
  on defeat/completion/retry/disable. Read docs/game/DAMAGE_FEEDBACK.md.
- Iteration 8 ClearingSupplies owns two optional attempt-local discoveries/charges.
  Runtime uses the player's foot point and wall LOS for discovery/E interaction,
  then restores the same actor-owned HP/MP snapshot. Herbs supply at most 4 HP;
  the rune supplies at most 6 MP. Full/invalid/no-gain attempts preserve charges.
  Only a genuine new-run reset restocks; disable/resume preserves spent/discovered
  state. ClearingSupplyVisuals caches nonblocking props and simulation-time pulses;
  HUD hints reuse the existing edge slot. Beacon E and completed-save retry retain
  priority. Read docs/game/EXPLORATION_SUPPLIES.md before changing this contract.
- `Gameplay/ClearingRules` is engine-independent timing/admission/reward logic;
  `ClearingRuntime` owns scene positions, contacts, input and the resource bridge.
  `ClearingVisuals`, `ClearingHud` and `ClearingAudio` own presentation. Runtime
  art uses cached PPU30/Point code-native rasters and a shared 15-color palette.
  New stone/moss sentinels, walls and beacon are an integrated art candidate;
  native visual acceptance and synthesized cue audition remain pending.
- Preserve attack tokens, stationary warning footprints, once-only rewards,
  HP-death synchronization, pause cleanup and full gate-to-boundary coverage.
  Player attacks use independent world effects and retain the existing walk cycle.
  Resolve all player contacts before enemy retaliation on each physics tick.
  Disable cancels transient contacts/effect deadlines without refunding cooldowns
  or erasing progression; pause freezes those actions without canceling them.
- Read `docs/game/COMBAT_CLEARING.md` for controls and native acceptance steps.
  Read `docs/game/CLEARING_PROGRESSION.md` before changing the reward ledger:
  only a completed beacon run deposits coins; save-before-mutate, same-attempt
  idempotency, bounded ranks and next-run-only actor bonuses are required.
  Run `tools/validate_project.py` (movement/resources/input/animation/combat/scene,
  actual presentation behavior and managed-raster checks) and the distribution
  suite. When installed, also use
  `tools/compile_unity_api.py`: real engine/uGUI signatures, with Input System
  explicitly reported as a substitute if its compiled assembly is absent.
- PR #4 normally merged the fifth-round growth loop into iteration 2 (a20cb5a),
  then PR #1 normally merged the reviewed candidate into master (cb2d6fc).
  The user's 2026-10-09 resume instruction explicitly makes native validation
  optional for ordinary merges, superseding earlier draft/master restrictions.
  Master now includes CombatClearing, local growth and PR5 tactical enemy roles;
  do not restart the prototype or redo delivered roles. PR6 also normally merges
  verified pooled actual-loss feedback; read the iteration-007 receipt before
  treating any historical seventh checkpoint pending list as current.
  Check exact CI, code/resources and recovery history before later merges. Never
  infer native acceptance from offline tests; continue recording its actual limits.
- Warning progress/X motifs remain inside the original saved contact outline.
  Pooled hit/kill feedback uses simulation time and resets on retry/disable.
  HUD messages share the help edge slot and hide unavailable actions on pause/death.
  Preserve these contracts and original hero hashes; do not change hit geometry
  merely to align with decoration or claim native rendering from offline rasters.

## Historical iteration 8 reviewed master integration

PR7 normally merged reviewed/tested30588a2 into masterba41be1; merge tree equals
implementation exactly. ExactCI37996558184 passed all12steps/artifact11647800082.
Fresh322checks,24sources/86realAPIrefsInputsub and100/100handoff are recorded.
Exploration supplies are fully wired; do not resume historical helper-only lists.
Final source/CI/ZIP/download identities resolve externally in the eighth delivery
receipt after source publication. Check that receipt before starting another loop;
if complete, do not redo eighth development. Native acceptance remains optional
and unrun. Preserve per-attempt charges, beacon/save priority and mature systems.

## Historical iteration 8 interruption — resolved

Read docs/iterations/2026-10-09-iteration-008/handoff.md before new features.
Supply domain foundation is isolated on rpg/iteration-008-exploration-supplies;
Runtime/prop/HUD integration is unfinished. Recover actual execution connectivity
before local edits and preserve any interrupted branch/process/work. Never delete
index locks based only on a timeout. No new eighth world interaction is claimed.

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
python3 tools/run_progression_checks.py --project-root .
python3 tools/run_enemy_checks.py --project-root .
python3 tools/run_presentation_checks.py --project-root .
python3 tools/run_art_checks.py --project-root .
python3 tools/validate_project.py --root . --output /tmp/rpg-project-report.json
python3 -m unittest discover -s tests -p 'test_distribution.py'
```

The offline C# runners use system Mono, or discover a retained Unity installation.
RPG_MONO and RPG_CSC/RPG_MCS can select tools. Boundary doubles do not reproduce
native physics, the real Input System, Animator rendering or Game View. Native
import, tests and gameplay are valuable optional checks when the environment supports them.
Never reduce checks, fabricate counts or treat a zero-test run as success.

## Delivery and CI

Maintain all five root records with changes, real results, baseline/commit evidence,
remaining issues and a next priority. Update KNOWN_ISSUES rather than repeat a
completed fix merely to produce a commit. Complete and save tested work within the
available time; isolate incomplete or high-risk changes with their evidence.

The ordinary GitHub Actions workflow `rpg-project-validation.yml` checks pushed
code or a manual run, then packages the validated project and uploads its ZIP and
reports. Check the actual run and Artifact before claiming hosted success. Standard
runners do not establish licensed native import, playback or visual acceptance.
Keep failed changes on `rpg/iteration-*` branches. Use ordinary review/CI gates
for merges; native evidence is optional under the current user instruction.

Package with the trusted tools/package_unity_project.py. Include real LFS objects,
Assets, Packages, ProjectSettings and .meta; exclude caches and credentials.
Provide actual commit/artifact URLs after publication, not planned URLs presented
as successful uploads. If blocked, preserve local commits/ZIP and report the exact
access or credential requirement. Do not claim this long-term game is finished.
