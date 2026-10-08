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
- Added official Codex Action hourly wakeup/five-hour persisted gate, isolated
  generation/validation/publish jobs, safe branch fallback and ZIP Artifact delivery.

Observed validation: 12 movement and 30 resource behavior tests passed; all 9
production C# files compiled against boundary doubles. Five PNGs decoded, all five
LFS SHA-256 OIDs matched, 37 Unity GUIDs were unique, 142 project references resolved,
and the enabled scene matched its .meta. Earlier movement checks failed against
the old source; resource checks changed from 18 pass/12 fail to 30 pass/0 fail.

Real Unity engine/Editor API reference compilation was additionally exercised for
the movement source, with Input System still substituted. No native Unity import,
PlayMode, physics, rendering, audio, full gameplay or platform build passed.

Automation/distribution boundary checks: 27 automation tests and 6 packaging tests passed.
Automation helpers have local tests, including real temporary Git push/conflict
scenarios and mocked HTTP gate checks. These are not live scheduled-run evidence.
GitHub API access from this cloud environment currently returns proxy Forbidden;
the required domain has been added to the environment draft. Repository API-secret
status and live Actions execution have not yet been confirmed. Publication receipts
and exact commit/download links are appended after actual push/delivery.

Next: verify real Actions runs, configure required independent API credentials if
missing, then close collision/input/respawn gaps with appropriate native evidence.
