# Iteration 5 checkpoint: persistence foundation

Baseline: fe8b1f72841bade09ed5a1c39dbf1df5996c78df, clean checkout,
from the verified iteration-2 candidate. Prior combat/presentation work retained.
PR #2 and #3 are merged; PR #1 remains draft and master remains 2e8c154.

Completed: versioned bounded reward/upgrade ledger, strict checksum codec,
exclusive-lock and expected-snapshot writes, flushed atomic replacement,
verified backup recovery and protection of corrupt/future-schema files.
Design: docs/game/CLEARING_PROGRESSION.md. No player integration yet.

Fresh evidence: 196 project checks (including 46 actual pure-C#/real filesystem
progression cases) and 6 distribution checks pass. 19 production sources compile
against 86 real Unity 2022.3.53f1 references; Input System is a boundary double.
Reports in this directory. No native import/play/render/audio/build ran.

Next: result-screen/input integration, actor-owned next-run bonuses, real-method
runtime/HUD regression fixtures, checkpoint push, CI/stacked PR, source ZIP,
verified hosted download and final handoff. This checkpoint is not final delivery.

## Integration checkpoint (supersedes foundation-only pending list)

Implemented complete E-bank / 1-or-2-upgrade / R-next-run flow, actor-owned
bonuses, saved bank/rank HUD, failure/retry/readonly notices and reward cue reuse.
Oversized future-version downgrade reproduced and fixed (five red/green cases).
Fresh 219 project + 6 distribution checks and 19-source real-API compile pass.
Final source reports and before/after evidence are alongside this file.
Pending: hosted CI/stacked PR merge into isolated candidate, final documentation
commit, complete source ZIP, immutable download verification and delivery receipt.
PR #1 remains draft; native acceptance and master merge remain deferred.
