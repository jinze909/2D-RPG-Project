# Iteration 4 integration and delivery

The resumed fourth round implements two reproduced reliability fixes: disable
invalidates unfinished contacts/effect deadlines while retaining earned progress
and costs; every admitted player hit resolves before surviving enemies retaliate.
No hero, scene, collider, package, Unity version or accepted art was replaced.

## Actual checkpoints and integration

| Stage | Actual commit / result |
| --- | --- |
| Recovery checkpoint | b1fac682bda6da227dbf25e22fced7ad39088d2d; pushed, CI 37883493542 succeeded |
| Game fix and regression checkpoint | [1e7eb01c359eeba55b42c038652701f0a54bbb38](https://github.com/jinze909/2D-RPG-Project/commit/1e7eb01c359eeba55b42c038652701f0a54bbb38) |
| PR #3 | [Merged](https://github.com/jinze909/2D-RPG-Project/pull/3) into iteration 3, merge 77dd22b1e08292a0e7d2d9ef0a6b085e3c449eea |
| PR #2 | [Merged](https://github.com/jinze909/2D-RPG-Project/pull/2) into iteration 2, merge baffbfa5a8962233585d98ebdd20ff3c522d1e07 |
| PR #1 | [Open draft](https://github.com/jinze909/2D-RPG-Project/pull/1), current candidate contains all merged work; master unchanged at 2e8c154f219256454ad981d6ebf11fdea5865d7d |

Implementation CI [37883929552](https://github.com/jinze909/2D-RPG-Project/actions/runs/37883929552)
completed successfully at exact head 1e7eb01. All 12 job steps succeeded, including
project validation, six distribution tests, package creation and upload; Artifact
11595835740 existed and was unexpired when inspected (427734 bytes).

PR #3 merge CI [37884061641](https://github.com/jinze909/2D-RPG-Project/actions/runs/37884061641)
also completed successfully at exact head 77dd22b. All 12 steps succeeded and
Artifact 11595163607 existed/unexpired (427734 bytes). The iteration-2 merge tree
matches both verified implementation and PR #3 trees exactly. Candidate-merge CI
[37884336027](https://github.com/jinze909/2D-RPG-Project/actions/runs/37884336027)
completed successfully at exact head baffbfa; all 12 steps and Artifact
11595597061 succeeded (427732 bytes, unexpired at inspection). Compact evidence
is in integration-ci.json. Final documentation-commit CI is checked separately
in the delivery receipt.

The user explicitly authorizes autonomous review, fixes and quality-gated normal
merges, overriding older user-only vetoes. PR #1 remains draft because its new
scene/input/physics has no native acceptance evidence. Its clean Git merge graph
and green offline CI do not resolve that gap. No force push or master merge occurred.

## Real validation and limits

Actual-source regression started at 17 presentation passes / six failures.
Final checks: 150 project passes (14 movement, 40 resources, 9 input/animation,
36 pure rules, 15 scene contracts, 26 presentation, 10 raster/hero) and six
distribution passes. Independent review reran the 26 presentation / 36 rules
checks and verified current source hashes. All 16 production sources compile
against 86 real Unity 2022.3.53f1 Engine/Editor/uGUI references; Input System is
explicitly substituted. JSON/log evidence is saved beside this record.

The installed Editor reports 2022.3.53f1. One temporary-project native probe
failed before import with no valid Unity license. Native import, real package
resolution/input/physics, EditMode/PlayMode, Animator/Game View/HUD, audio output,
playthrough and platform builds remain unrun. No connected Unity MCP or CLI was
found; plugin guidance alone is not an operating service.

## Complete project ZIP and persistent receipt

The final source includes this record and the validated [handoff](handoff.md).
Its own future source SHA, CI and ZIP hash are recorded externally in the independent
[iteration-4 delivery receipt](https://raw.githubusercontent.com/jinze909/2D-RPG-Project/rpg-deliveries/iteration-004-delivery.json)
only. ARCHIVE-INFO.json inside the ZIP records the source SHA, Unity version,
file count and clean-source/native-validation metadata. Read the receipt for actual publication
completion after any interruption; this source record does not claim that a
future upload or check has already run.

The trusted packager includes real LFS Assets, Packages, ProjectSettings, every
.meta, source, docs, tests/tools, workflow and eight licensed .agents skills.
It excludes Library/Temp/logs/build caches, Git history and credentials. The final
delivery phase verifies CRC, commit/clean metadata, exact selected file set,
every source byte/SHA, LFS materialization, and an independently downloaded HTTPS
copy before committing its receipt. Earlier iteration-2/3 deliveries are retained.
Hosted Artifact metadata and the persistent direct ZIP are distinct archives;
use the direct ZIP SHA-256 for its download, not the Actions container digest.

Next priority is licensed native acceptance of the current loop. Do not redo the
fourth-round fixes or choose unrelated development merely because master is old.
