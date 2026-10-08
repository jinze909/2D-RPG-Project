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
production C# files compiled against boundary doubles. Five PNGs decoded, all six
LFS SHA-256 OIDs matched (five game PNGs and a vendored skill reference), 37 Unity GUIDs were unique, 142 project references resolved,
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
status remains unknown. User explicitly declined to provide an API Key, so paid
autonomous development is paused by default and requires a separate explicit
RPG_AUTONOMOUS_ENABLED=true opt-in before any future model call.

Published game/skills commit: d2783214e752ddf35c5cf22f6332a5a3fa8ed0eb.
Published workflow commit: 1e00d30ad2df4371ee96fa8f81b048ace9b8aaef.
Actual first GitHub Actions run: https://github.com/jinze909/2D-RPG-Project/actions/runs/37739823460.
The hosted project validation passed, but automation/distribution boundary tests
failed and ZIP creation was correctly skipped. Follow-up isolates GitHub step
output transports in fixtures, removes fixture dependence on LFS local-transfer
smudging, and retains failure logs/annotations. Local checks pass; the corrected
hosted run must be observed before reporting successful CI or ZIP publication.

Next: verify corrected CI and source ZIP delivery, then close collision/input/respawn
gaps with appropriate native evidence. No paid scheduled development or next AI
run is enabled while the user chooses not to configure independent API access.

Hosted CI follow-up 37740874645 again passed actual-source validation and 26/27
automation checks, with only the LFS patch-transfer test failing. The failure was
reproduced using a mandatory global LFS filter: Git LFS creates legitimate local
cache hardlinks, which strict untrusted-bundle checks incorrectly also rejected
for the native cache. The fix preinstalls hash-verified bundled objects, prevents
network smudging during patch application and accepts only hash-verified cache
hardlinks for read-only reuse. Untrusted bundle files still reject all hardlinks.
The regression now includes mandatory filters and a cache hardlink, and all 27
checks pass locally. Real hosted revalidation is required before green claims.

First recoverable source ZIP for a81a6ce3617abdabc44706415d7059882c9928ec was
published on rpg-deliveries and downloaded over HTTPS: 377919 bytes, SHA-256
31b02d356245c6ac0015644a4508e49f7386cf2318a4e9944f42d0d4938c020a.
It contains 188 source files plus metadata and has a clean source worktree. A
final delivery will follow the validated workflow correction.
