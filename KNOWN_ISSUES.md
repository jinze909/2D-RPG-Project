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
- Runtime geometry/sentinel shapes are blockout content and generated tones have
  not been auditioned. Full art, level pacing and audio mix acceptance remain open.
- International registry URLs are corrected with identical locked versions, but
  native package resolution/artifact downloads and editor import remain unverified.

## Automation prerequisites and limits

- GitHub Actions must allow the workflows and required write permissions. The
  default branch is master, confirmed via remote symbolic HEAD.
- codex-action needs a repository OPENAI_API_KEY Secret with independently billed
  API credit/model access. ChatGPT subscription credentials cannot replace it.
  The user declined an API Key. RPG_AUTONOMOUS_ENABLED is opt-in and defaults to
  paused; no paid AI round is scheduled. GitHub secret presence remains unknown.
- Terminal GitHub REST requests still return proxy Forbidden. Connected GitHub
  APIs and public HTML work; use them for PR/status instead of bypassing the proxy.
- Prior baseline abc74f5 hosted validation was confirmed successful at run
  37741402547 with one source Artifact. A successful scheduled gate run had its
  generate/validate/publish jobs skipped; paid AI development is still paused.
  The new candidate needs its own CI evidence and native acceptance before merge.
- Standard Actions runners do not have the cloud's editor license, MCP connection
  or ChatGPT image-generation tools. Native/visual high-risk changes stay isolated.
- Cron is best effort, can be delayed, and may be disabled for prolonged inactivity.
  Missing keys, quota failures and protected branches must be reported accurately.

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
