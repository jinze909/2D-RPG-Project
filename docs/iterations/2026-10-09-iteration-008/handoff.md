# Handoff: eighth-round exploration supplies foundation

## Current State Summary

Seventh round fully delivered; actual master 3f3fbd41d2c6a4d3536001c817ae88a1489ebd21, PR6 merged,
completed receipt d0e59185db40cf83eebca8015b16402425673505. Eighth ordinary isolated
branch rpg/iteration-008-exploration-supplies starts there. Selected feature is two
optional discoverable per-run health/mana reserves. Pure rule foundation and
behavior tests are authored remotely; actual CI identities/results follow in the
checkpoint confirmation. Runtime/visual/HUD integration is not implemented.

## Handoff Chain

Continues docs/iterations/2026-10-09-iteration-007/handoff.md and completed
rpg-deliveries iteration-007-final-handoff.md/receipt atd0e5918. Prior master,
all checkpoints, original hero/profile/tactics/feedback and archives are preserved.

## Important Context

During initial Git checks, status calls blocked in kernel opening
/workspace/2D-RPG-Project/.git/index.lock with O_CREAT|O_EXCL. Then exec_command
failed before command creation: exec-server transport disconnected/recovery timeout.
Recoverywait reportedready but shell remained unresponsive; managed status offline.
No lock removed, secret read, source overwritten or force push performed. Local
branch-create command was queued behind blocked status and is not confirmed.
Remote branch creation/checkpoints use GitHub connector with exact parent lease.
Do not assume local dirty state or new branch after recovery; inspect actual state.

## Architecture Overview

ClearingSupplies owns only discovered/claimed flags and admission/capped delta.
Runtime must continue owning actor HP/MP, positions, LOS and playable admission.
Two charges restock only on new-run Reset, never component disable/enable.
Existing beacon/objective/save/gate authority remains untouched.

## Critical Files

- Assets/Scripts/Gameplay/ClearingSupplies.cs and its unique .meta.
- tests/exploration/ClearingSuppliesChecks.cs, tools/run_supply_checks.py.
- tools/validate_project.py: actual pure rule suite in current project validation.
- Assets/Scripts/Gameplay/ClearingRuntime.cs: upcoming input/resources/lifecycle.
- Assets/Scripts/Gameplay/ClearingHud.cs: upcoming existing edge-slot selection.
- docs/game/EXPLORATION_SUPPLIES.md: exact contract and coordinates.

## Files Modified

Foundation helper/meta, behavior fixture/runner/validator hook, five root records,
contract and this handoff. Existing Runtime/Visuals/Hud/scenes/hero/save are not
rewritten at this stage. All implementation still must be truthfully verified.

## Decisions Made

Supply exploration completes a missing loop while supporting existing tactical
combat. Fixed4HP/6MP reserves create timing/detour choices; full resources never
waste a charge. Per-run state avoids save schema/world-state expansion. Native
acceptance remains optional under user authority, but actual CI/review is required.

## Assumptions Made

Unity2022.3.53f1/Built-in/PPU30 and original hero stayfixed. Discovery2.4/use0.8
plus wall LOS. Herbs(-5.6,-2.35), rune(5.6,1.15) reachable on offline footprint
geometry while gate closed. This does not establish native collision or balance.

## Potential Gotchas

E near beacon and terminal save retry must retain priority. Invalid/dead/full/no
actual gain must not consume supplies. Disability cannot restock reserves; retry
must. Upcoming rendering/LOS/resource/animation tests must execute actual methods,
not claim the pure helper or recording doubles are native play. Input System
real-API baseline uses a substitute. Fresh baseline22/86 API and6distribution
checks passed before outage; rootbaseline validator exited0. New counts onlyfromCI.

## Immediate Next Steps

1. Verify remote checkpoint/CI and local executor state after recovery. Preserve
   original local files/locks; compare local branch/HEAD/dirty/unpushed state.
2. Finish actual E/LOS/foot-point/resource integration, cached props and edge HUD.
   Keep mature saved growth/tactics/damage feedback and terminal admission.
3. Run meaningful fresh actual-source domain/runtime/regression/API checks and
   independent review, saving ordinary checkpoints after verified stages.
4. Only then normally merge complete green PR to master and publish complete
   source ZIP with CRC/source/member hashes/HTTPS plus validated final handoff.
