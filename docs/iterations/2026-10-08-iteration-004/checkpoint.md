# Iteration 4 checkpoint: recovered baseline and pending fixes

Started from `53bd47eb63f5b512c1981605341d64eee8d2cfe7` on
`rpg/iteration-004-combat-reliability`; stable remote master is
`2e8c154f219256454ad981d6ebf11fdea5865d7d`.

After the quota interruption, actual Git inspection found a clean working tree,
no fourth-round commit, no upstream and no remote fourth-round branch. No code or
regression test edits survived because none had been written. The temporary
focused reproducer and capability reports still existed; those are not delivered
game changes. Full session-handoff chain was already read and verified against
the candidate source. Historical user-only merge restrictions are superseded by
the current autonomous-merge authorization.

## Independently verified baseline

The first full check failed because six LFS files were still pointers in this
fresh checkout. `git lfs install --local` and `git lfs pull` restored every real
object; no asset source was changed. Fresh restored validation passes 134 project
checks and six distribution tests. Separate API-reference compilation passes for
16 production sources with 86 real Unity 2022.3.53f1 Engine/Editor/uGUI references;
Input System is a boundary substitute. Full JSON evidence accompanies this file.

The actual Editor version/Mono/compiler were probed. A single isolated temporary
project failed native licensing before import (`No valid Unity Editor license
found`). No native tests, physics, visuals, audio or player build passed. Existing
skills are usable guidance; CLI/MCP/desktop were not found. No installer, login,
license workaround, version upgrade or package change was attempted.

PR #1 is open/draft/unmerged, de06143 -> master; PR #2 is open/draft/unmerged,
53bd47e -> iteration 2. Both are currently mergeable. Previous exact-head CI is
green; this checkpoint will need its own hosted verification after push.

## Interrupted implementation to resume

1. Regression tests and production fixes are not yet complete. A focused actual-
   source recording-boundary reproducer proved that OnDisable hides objects but
   leaves the current attack and impact deadlines alive. The next RefreshViews
   brings strike/warning/impact back; a fresh target takes 18 damage without new
   input. Cancel transient actions/effect timers while preserving HP, rewards,
   cooldowns and immunity. Pause remains a freeze, not cancellation.
2. Reproduce a same-tick burst against several targets while one sentinel deals
   lethal damage. The single interleaved enemy loop can break before later player
   contacts. Resolve all admitted player contacts first, then remaining enemy
   attacks; check index permutations and lethal interruption.
3. Run red/green regression evidence, full project/distribution/API checks,
   inspect diffs, checkpoint commit/push and obtain hosted CI.
4. Review candidate PR integration. Master acceptance must be based on actual
   risk/evidence; native-unverified scene/physics still justify isolation.
5. Deliver a clean-commit complete source ZIP, verify CRC/hash/metadata/every
   source byte and real LFS, publish a persistent download, then save final handoff.

Current native barriers and unimplemented full-RPG features are not closed by
this checkpoint. No paid autonomous API or recurring job was enabled.

## Completed implementation stage

Both pending root causes above are now fixed in ClearingRules/ClearingRuntime.
Original red reproduction had 17 pass/6 fail. Final focused checks have 26
presentation and 36 pure-rule passes; full project validation has 150 passes,
distribution six passes, and real Unity API compilation still passes. Sources,
assets and bundle hashes pass; no native acceptance is claimed. The implementation
checkpoint commit contains this updated file and final JSON/log evidence.
Only hosted implementation CI, candidate integration, ZIP publication and final
handoff remain outstanding. Read the final handoff/publication when present;
these dated checkpoint sections describe their own stage, not current PR state.
