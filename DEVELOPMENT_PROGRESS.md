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
- Added project verification, ZIP packaging and Artifact delivery workflow.

Observed validation: 12 movement and 30 resource behavior tests passed; all 9
production C# files compiled against boundary doubles. Five PNGs decoded, all six
LFS SHA-256 OIDs matched (five game PNGs and a vendored skill reference), 37 Unity GUIDs were unique, 142 project references resolved,
and the enabled scene matched its .meta. Earlier movement checks failed against
the old source; resource checks changed from 18 pass/12 fail to 30 pass/0 fail.

Real Unity engine/Editor API reference compilation was additionally exercised for
the movement source, with Input System still substituted. No native Unity import,
PlayMode, physics, rendering, audio, full gameplay or platform build passed.

Distribution boundary checks: 6 packaging tests passed. These checks verify
source archive content, cache exclusions, credential-file rejection, LFS pointers,
symlinks and required Unity directories. They do not prove native Unity gameplay.

Published game/skills commit: d2783214e752ddf35c5cf22f6332a5a3fa8ed0eb.
Published CI workflow commit: 1e00d30ad2df4371ee96fa8f81b048ace9b8aaef.
Actual first GitHub Actions run: https://github.com/jinze909/2D-RPG-Project/actions/runs/37739823460.
The hosted project validation passed, but another check failed and ZIP creation
was correctly skipped. Hosted CI follow-up 37740874645 also passed actual-source
validation. Fresh hosted revalidation and source ZIP delivery remain required
before reporting success.

Next: verify project CI and source ZIP delivery, then close collision/input/respawn
gaps with appropriate native evidence.

First recoverable source ZIP for a81a6ce3617abdabc44706415d7059882c9928ec was
published on rpg-deliveries and downloaded over HTTPS: 377919 bytes, SHA-256
31b02d356245c6ac0015644a4508e49f7386cf2318a4e9944f42d0d4938c020a.
It contains 188 source files plus metadata and has a clean source worktree. A
final delivery will follow the validated workflow correction.

## Repository maintenance — 2026-10-08

Baseline: `abc74f5bef86edf06835a785a9e7205725e80e75`, fresh `origin/master`.
Updated project guidance, skill authorization wording and the project verification /
ZIP delivery workflow. Unity source, assets, package versions and project settings
remain unchanged. Upstream skill provenance and license files are preserved; all
eight local adaptation records and manifest checksums match the retained files.

Fresh verification: 12 movement and 30 resource boundary behavior checks, 6
distribution checks and all project integrity checks passed. Native Unity import,
PlayMode, physics and visual acceptance were not executed.
