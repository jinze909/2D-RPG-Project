# Known issues

## Confirmed current gaps

- Native Unity 2022.3.53f1 import/play/test is unavailable without valid licensing.
  Offline doubles do not validate real physics, Input System or visual playback.
- SampleScene's player currently has no Collider2D and no terrain/quest walls.
  Plan a tested collision/exploration loop; do not claim existing forest gates fixed.
- No attacks, casting, enemies, professions, HUD, inventory, quests or audio systems
  exist in this master. A separate Starfall handoff cannot substitute for its source.
- Gamepad uses the default digital-normalized 2DVector composite. Movement now
  preserves partial action values, but real analog hardware still requires a binding
  correction and actual Input System checks.
- PlayerStats is a shared ScriptableObject without a defined runtime reset/respawn
  lifecycle. Do not auto-reset or redesign it without a coherent save/session contract.
- Dead.anim is configured to loop. Native animation review is needed to establish
  the correct ending/respawn behavior before replacing it.
- International registry URLs are corrected with identical locked versions, but
  native package resolution/artifact downloads and editor import remain unverified.

## Automation prerequisites and limits

- GitHub Actions must allow the workflows and required write permissions. The
  default branch is master, confirmed via remote symbolic HEAD.
- codex-action needs a repository OPENAI_API_KEY Secret with independently billed
  API credit/model access. ChatGPT subscription credentials cannot replace it.
  GitHub secret presence is currently unknown because cloud API access is blocked.
- Current cloud GitHub REST requests return Forbidden. api.github.com is saved in
  the network draft; draft persistence has not applied the running network policy.
- Live scheduled execution, model development and Artifact upload are not yet
  verified. Never equate configuration/local unit tests with activation.
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
