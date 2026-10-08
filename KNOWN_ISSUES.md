# Known issues

## Confirmed current gaps

- Native Unity 2022.3.53f1 import/play/test is unavailable without valid licensing.
  Offline doubles do not validate real physics, Input System or visual playback.
- The new CombatClearing candidate implements collision, attacks, enemies, sealed
  objective, HUD and cues on an isolated branch. Their real engine/visual behavior
  is not yet accepted. Original SampleScene remains the bare prototype; the old
  forest-gate report describes a different source and was not claimed recovered.
- No professions, inventory, persistent equipment/progression/save, full story,
  large world or boss system exists. Run-local coins are feedback, not a saved economy.
- Arrow/analog/D-pad bindings are corrected and serialized/wrapper-coherent;
  actual Input System package behavior and hardware deadzones require native tests.
- Existing walking slices have heights 66–69 with center pivots and a duplicated
  closing frame; foot drift and gait cadence at partial speed need visual review.
- Defeat holds an existing front pose. Proper hurt/death/attack character motion
  art remains absent; new world-space attack effects do not claim to supply it.
- Iteration 3 replaces the flat clearing blockout with a shared 15-color stone/moss
  pixel-art candidate and pooled warning/hit/kill feedback. Native texture creation,
  FullRect rendering, pixel scaling, actor layering, text readability and synthesized
  cue output have not been accepted. Level pacing and audio mix need actual play.
- International registry URLs are corrected with identical locked versions, but
  native package resolution/artifact downloads and editor import remain unverified.

## CI, access and validation limits

- GitHub Actions must allow the workflows and required write permissions. The
  default branch is master, confirmed via remote symbolic HEAD.
- Terminal GitHub REST requests still return proxy Forbidden. Connected GitHub
  APIs and public HTML work; use them for PR/status instead of bypassing the proxy.
- Cleanup baselines were freshly verified green at runs 37855895204 (master
  2e8c154) and 37855964779 (PR #1 candidate de06143), including ZIP uploads.
  New changes need exact-head CI evidence; native acceptance remains outstanding.
- PR #1 must remain unmerged in this round by explicit user instruction. The
  polish branch depends on its candidate and uses an independent stacked draft PR.
- Standard Actions runners do not have the cloud's editor license, MCP connection
  or ChatGPT image-generation tools. Native/visual high-risk changes stay isolated.
- Protected branches and unavailable GitHub access must be reported accurately.

## Fixed in iteration 1

Render-frame-dependent physics requests; movement after death; stale disabled input,
velocity and walk flags; undisposed input actions; full-speed treatment of partial
action values; negative/nonfinite damage; negative/nonfinite mana consumption;
negative overkill HP and repeated death triggers. Scope and verification limits are
recorded in DEVELOPMENT_PROGRESS.md and tests/offline/README.md.

## Fixed / implemented in iteration 2 candidate

Actor-owned resource snapshots, new session/retry reset, pending death/revive
trigger conflicts, missing arrows, digital-only stick binding, looping directional
spin on death, cached pause/retry movement and prototype self-damage in real gameplay.
The clearing additionally covers unique hits/rewards, telegraph timing, invulnerability,
gate/objective admission and sealed-boundary geometry. A regression caught and fixed
the new bootstrap's wrong typed Player reference before publication. New scene
implementation does not replace missing native physics/playtesting evidence.

## Fixed / implemented in iteration 3 candidate

HUD no longer offers disabled beacon actions while paused/dead, and transient
messages share the bottom-right help slot instead of extending across the lower
central combat band. Accepted hits/kills now give pooled world/audio confirmation;
progress and active-window motifs distinguish warning phases inside unchanged
contact bounds. Retry/disable clears effects and sounds. Regressions execute real
production methods with recording boundaries, not native playback.
