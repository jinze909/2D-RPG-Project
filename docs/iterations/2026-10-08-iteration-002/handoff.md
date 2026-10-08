# Handoff: Combat Clearing and player lifecycle

## Current State Summary

Iteration 2 starts from abc74f5bef86edf06835a785a9e7205725e80e75, fetched and
verified against GitHub master. Work is isolated on rpg/iteration-002-clearing.
The original moving-player prototype now has actor-owned resources, reset/revive
and corrected input/death presentation. CombatClearing implements the first
bounded combat/reward/beacon/retry loop. Source and offline verification are real;
licensed native import/play/visual/audio acceptance is pending. Publication receipts
and the final ZIP commit/digest are recorded in the delivered manifest/final summary.

## Architecture Overview

Player owns a runtime clone of the authoring PlayerStats. Health and mana share
that clone; new sessions/retry fill valid maxima, not a shared asset or saved game.
ClearingRules is pure C# timing/contact/reward state without duplicate HP/MP.
ClearingRuntime bridges physics/facing/input/resources and scene objects.
ClearingVisuals/Hud/Audio own provisional code-native presentation. CombatClearing
is the first candidate build scene; SampleScene remains unchanged on disk.

## Important Context

This source is not the recovered full Starfall game. Older rpg-by-ai reference
at b0aae3d329c5b0b200b30b1457d0f692257b9f06 informed foot clearance/contact/UI
ideas; its assets, scale, OnGUI and save architecture were not transplanted.
Preserve the blonde Viola sprite, Point/no compression/PPU30, licenses and GUIDs.
Native-unverified scene/media changes stay isolated. No paid API automation or
Gemini Spark schedule was enabled; a successful prior scheduled gate skipped AI jobs.

## Critical Files

| File | Role |
| --- | --- |
| Assets/Scenes/CombatClearing.unity | Candidate scene with typed player/camera bootstrap |
| Assets/Scripts/Gameplay/ClearingRules.cs | Pure combat/telegraph/cooldown/reward/run state |
| Assets/Scripts/Gameplay/ClearingRuntime.cs | Physics/resource/input/lifecycle bridge |
| Assets/Scripts/Gameplay/ClearingVisuals.cs | Closed terrain/gate and shared foot anchor |
| Assets/Scripts/Player/Player.cs | Actor-owned resource lifetime and retry |
| docs/game/COMBAT_CLEARING.md | Controls, gameplay contract and native acceptance |
| tools/validate_project.py | Offline source/resource/rules/scene checks |
| tools/compile_unity_api.py | Optional real installed-engine/uGUI API signatures |

## Files Modified

Player/Health/Mana/Stats/Movement/Animations, serialized input and generated wrapper,
Dead.anim, new clearing scene/five gameplay scripts and meta, build entry, validation
fixtures/runners and five root records. Original SampleScene/hero bitmap/import
files, Unity version, dependency versions and paid automation settings are preserved.

## Decisions Made

Finish one small loop before classes/inventory/large world. Use world-space strike
effects while preserving real locomotion; do not pretend new hero attack art exists.
Use stationary warnings and matching footprints, unique action tokens/rewards,
closed walls and both gate/proximity objective checks. Initial wrong bootstrap
PlayerHealth reference was caught by a test and corrected. Raster review corrected
torso-based foot collision and north camera clipping. Death holds a provisional
existing front pose until proper art can be authored and reviewed.

## Immediate Next Steps

Fetch current master and unmerged iteration branches, read PR/CI/ZIP receipts,
then run the fresh offline suites. Execute the native checklist with licensed
international Unity 2022.3.53f1 before approving this high-risk scene for master.
Review real movement/attack/foot animation, gate collisions, UI at different
resolutions and cue balance. Add one coherent exploration/progression extension
only after the current loop is accepted; do not reimplement it from scratch.

## Assumptions Made

The authoring stats are new-run configuration, not persistent saves; no save
contract existed in the baseline. Clear rewards remain run-local. Geometry and
enemy shapes are readable blockout candidates. Approximate foot anchor derives
from the current 35×68 PPU30 frames and needs real engine visual acceptance.

## Potential Gotchas

The serialized Player is component 990178526, not PlayerHealth 990178524. Input
device/Animator tests remain unrun despite real engine API reference compilation.
The latter substitutes Input System when no compiled package assembly exists.
Walk slice height/center-pivot variation and closing duplicate key still need
native review. DefaultExecutionOrder100 ensures current-frame movement facing
before attack admission. Runtime must clean up time scale, cached motion and
actor velocities around pause/death/retry. Do not equate geometry floodfill with
native collision simulation or API compilation with editor import.

## Environment State

The fresh managed container and terminal work. Git fetch/push use normal history;
local LFS was initialized and all six original objects were materialized.
Retained international editor is 2022.3.53f1, with Mono/Roslyn and real engine/uGUI
references; a fresh license probe failed with No valid Unity Editor license found.
Terminal REST api.github.com is proxy-blocked, but connected GitHub tools and public
HTML work. Credentials were not inspected or printed. No live Unity MCP is connected.
