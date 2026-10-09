# Next iteration

Iteration 5 is in progress on rpg/iteration-005-persistent-progression.
Read docs/iterations/2026-10-08-iteration-005/checkpoint.md and
 docs/game/CLEARING_PROGRESSION.md. Persistence foundation is tested; next
finish gameplay/input/HUD/actor integration and runtime regressions, push the
second checkpoint, verify hosted CI/stacked PR, then ZIP/download/handoff.
Keep PR #1 draft and master unchanged until licensed native acceptance.

Previous completed-round context follows; do not restart its implemented fixes.

Continue from the [fourth-round handoff](docs/iterations/2026-10-08-iteration-004/handoff.md)
and [publication record](docs/iterations/2026-10-08-iteration-004/publication.md).
Read the independent [delivery receipt](https://raw.githubusercontent.com/jinze909/2D-RPG-Project/rpg-deliveries/iteration-004-delivery.json)
for the final full source SHA, successful exact-head CI and verified ZIP identities.
Those identities are external to the packaged source to avoid self-reference.

1. Inspect actual Git/remote state, uncommitted files, PR #1 and latest receipt.
   PR #3 (77dd22b) merged the fourth round into iteration 3, then PR #2 (baffbfa)
   integrated it into iteration 2. Fetch every rpg/iteration-* branch before
   selecting a baseline. Master remains 2e8c154; PR #1 remains a draft because
   native acceptance is missing. Autonomous ordinary merges are authorized;
   quality evidence determines readiness. No force push or worktree creation.
2. Recheck international Unity 2022.3.53f1 license, real editor, CLI/MCP and test
   capabilities. Read applicable Unity / Unity Essentials skills; skill visibility
   does not establish a connected Editor. The last actual native probe failed
   before import because there was no valid license. Do not repeat that unchanged
   probe or reinstall packages merely to claim progress.
3. In a licensed environment, complete docs/game/COMBAT_CLEARING.md native
   acceptance: import/compile, EditMode/PlayMode, input, fixed-update contacts,
   all three lethal-attacker orderings, disable/resume, collisions/gate,
   beacon/win/death/retry/pause, real HUD/Animator screenshots and all six cues.
   Check FullRect pixel creation, sentinel feet/layering, warning progress/X,
   multi-target sound deduplication, stale effects/sounds after retry/disable,
   and edge-slot messages at narrow/portrait sizes. Produce a real player build
   only when its licensing and target support are available.
4. Fix observed native defects, then reevaluate PR #1 for master. Existing
   stone/moss raster art, blonde hero, PPU30, accepted frames and rejected ranger
   arrows are preserved. Walking foot drift, partial-speed cadence and proper
   hurt/death/attack character motion need visual review; do not replace existing
   art solely to increase the change count.
5. After the current loop is accepted, add one coherent exploration/interaction
   extension with checkpoint or persistent reward. Define save/version/reset
   contracts before inventory/equipment/classes; balance using actual play.

Fourth-round code and tests are complete: 150 project + 6 distribution passes,
16-source compilation against 86 real Unity references (Input System substituted).
Transient cancellation and two-pass player contacts must not be reimplemented.
Publication is resumable from the receipt if a quota interruption occurs before
its final verification. Earlier movement/resource guards and all eight vendored
skills remain preserved. Save and safely push each independently verified stage;
maintain actual completed and unfinished work in the five root records.
